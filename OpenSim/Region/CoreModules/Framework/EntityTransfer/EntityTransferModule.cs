/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;

using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Client;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

using GridRegion = OpenSim.Services.Interfaces.GridRegion;

using OpenMetaverse;
using OpenMetaverse.StructuredData;
using log4net;
using Nini.Config;

namespace OpenSim.Region.CoreModules.Framework.EntityTransfer
{
    public class EntityTransferModule : ISharedRegionModule, IEntityTransferModule
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected bool m_Enabled = false;
        protected List<Scene> m_scenes = new List<Scene> ();
        private Dictionary<IScene, Dictionary<UUID, AgentData>> m_incomingChildAgentData = new Dictionary<IScene, Dictionary<UUID, AgentData>> ();
        
        #endregion

        #region ISharedRegionModule

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public virtual string Name
        {
            get { return "BasicEntityTransferModule"; }
        }

        public virtual void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("EntityTransferModule", "");
                if (name == Name)
                {
                    m_Enabled = true;
                    //m_log.InfoFormat("[ENTITY TRANSFER MODULE]: {0} enabled.", Name);
                }
            }
        }

        public virtual void PostInitialise()
        {
        }

        public virtual void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            m_scenes.Add(scene);

            scene.RegisterModuleInterface<IEntityTransferModule>(this);
            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnNewPresence += EventManager_OnNewPresence;
            scene.EventManager.OnClosingClient += OnClosingClient;
        }

        void EventManager_OnNewPresence (IScenePresence sp)
        {
            lock (m_incomingChildAgentData)
            {
                Dictionary<UUID, AgentData> childAgentUpdates;
                if (m_incomingChildAgentData.TryGetValue (sp.Scene, out childAgentUpdates))
                {
                    if (childAgentUpdates.ContainsKey (sp.UUID))
                    {
                        //Found info, update the agent then remove it
                        sp.ChildAgentDataUpdate (childAgentUpdates[sp.UUID]);
                        childAgentUpdates.Remove (sp.UUID);
                        m_incomingChildAgentData[sp.Scene] = childAgentUpdates;
                    }
                }
            }   
        }

        public virtual void Close()
        {
        }

        public virtual void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;
            m_scenes.Remove(scene);

            scene.UnregisterModuleInterface<IEntityTransferModule>(this);
            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnNewPresence -= EventManager_OnNewPresence;
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public virtual void RegionLoaded(Scene scene)
        {
        }


        #endregion

        #region Agent Teleports

        public virtual void Teleport(IScenePresence sp, ulong regionHandle, Vector3 position, Vector3 lookAt, uint teleportFlags)
        {
            int x = 0, y = 0;
            Util.UlongToInts(regionHandle, out x, out y);

            GridRegion reg = sp.Scene.GridService.GetRegionByPosition (sp.Scene.RegionInfo.ScopeID, x, y);
            
            if (reg == null)
            {
                // TP to a place that doesn't exist (anymore)
                // Inform the viewer about that
                sp.ControllingClient.SendTeleportFailed("The region you tried to teleport to doesn't exist anymore");

                // and set the map-tile to '(Offline)'
                int regX, regY;
                Util.UlongToInts(regionHandle, out regX, out regY);

                MapBlockData block = new MapBlockData();
                block.X = (ushort)(regX / Constants.RegionSize);
                block.Y = (ushort)(regY / Constants.RegionSize);
                block.Access = 254; // == not there

                List<MapBlockData> blocks = new List<MapBlockData>();
                blocks.Add(block);
                sp.ControllingClient.SendMapBlock(blocks, 0);
                return;
            }
            Teleport(sp, reg, position, lookAt, teleportFlags);
        }

        public virtual void Teleport(IScenePresence sp, GridRegion finalDestination, Vector3 position, Vector3 lookAt, uint teleportFlags)
        {
            string reason = "";

            sp.ControllingClient.SendTeleportStart(teleportFlags);
            sp.ControllingClient.SendTeleportProgress(teleportFlags, "requesting");

            // Reset animations; the viewer does that in teleports.
            if(sp.Animator != null)
                sp.Animator.ResetAnimations();

            try
            {
                if (finalDestination.RegionHandle == sp.Scene.RegionInfo.RegionHandle)
                {
                    //First check whether the user is allowed to move at all
                    if (!sp.Scene.Permissions.AllowedOutgoingLocalTeleport(sp.UUID, out reason))
                    {
                        sp.ControllingClient.SendTeleportFailed(reason);
                        return;
                    }
                    //Now respect things like parcel bans with this
                    if (!sp.Scene.Permissions.AllowedIncomingTeleport(sp.UUID, position, out position, out reason))
                    {
                        sp.ControllingClient.SendTeleportFailed(reason);
                        return;
                    }
                    m_log.DebugFormat(
                        "[ENTITY TRANSFER MODULE]: RequestTeleportToLocation {0} within {1}",
                        position, sp.Scene.RegionInfo.RegionName);

                    sp.ControllingClient.SendLocalTeleport(position, lookAt, teleportFlags);
                    sp.Teleport(position);
                }
                else // Another region possibly in another simulator
                {
                    // Make sure the user is allowed to leave this region
                    if (!sp.Scene.Permissions.AllowedOutgoingRemoteTeleport(sp.UUID, out reason))
                    {
                        sp.ControllingClient.SendTeleportFailed(reason);
                        return;
                    }
                    //m_log.DebugFormat("[ENTITY TRANSFER MODULE]: Final destination is x={0} y={1} uuid={2}",
                    //    finalDestination.RegionLocX / Constants.RegionSize, finalDestination.RegionLocY / Constants.RegionSize, finalDestination.RegionID);

                    // Check that these are not the same coordinates
                    if (finalDestination.RegionLocX == sp.Scene.RegionInfo.RegionLocX &&
                        finalDestination.RegionLocY == sp.Scene.RegionInfo.RegionLocY)
                    {
                        // Can't do. Viewer crashes
                        sp.ControllingClient.SendTeleportFailed("Space warp! You would crash. Move to a different region and try again.");
                        return;
                    }

                    //
                    // This is it
                    //
                    DoTeleport(sp, finalDestination, position, lookAt, teleportFlags);
                    //
                    //
                    //
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[ENTITY TRANSFER MODULE]: Exception on teleport: {0}\n{1}", e.Message, e.StackTrace);
                sp.ControllingClient.SendTeleportFailed("Internal error");
            }
        }

        public virtual void DoTeleport(IScenePresence sp, GridRegion finalDestination, Vector3 position, Vector3 lookAt, uint teleportFlags)
        {
            sp.ControllingClient.SendTeleportProgress(teleportFlags, "sending_dest");
            if (finalDestination == null)
            {
                sp.ControllingClient.SendTeleportFailed("Unable to locate destination");
                return;
            }

            m_log.DebugFormat(
                "[ENTITY TRANSFER MODULE]: Request Teleport to {0}:{1}/{2}",
                finalDestination.ServerURI, finalDestination.RegionName, position);

            sp.ControllingClient.SendTeleportProgress(teleportFlags, "arriving");

            // Fixing a bug where teleporting while sitting results in the avatar ending up removed from
            // both regions
            if (sp.ParentID != UUID.Zero)
                sp.StandUp();

            //Make sure that all attachments are ready for the teleport
            IAttachmentsModule attModule = sp.Scene.RequestModuleInterface<IAttachmentsModule>();
            if (attModule != null)
                attModule.ValidateAttachments(sp.UUID);

            AgentCircuitData agentCircuit = sp.ControllingClient.RequestClientInfo();
            agentCircuit.startpos = position;
            //The agent will be a root agent
            agentCircuit.child = false;
            //Make sure the appearnace is right
            IAvatarAppearanceModule appearance = sp.RequestModuleInterface<IAvatarAppearanceModule> ();
            agentCircuit.Appearance = appearance.Appearance;

            AgentData agent = new AgentData();
            sp.CopyTo(agent);
            //Fix the position
            agent.Position = position;

            IEventQueueService eq = sp.Scene.RequestModuleInterface<IEventQueueService>();
            if (eq != null)
            {
                ISyncMessagePosterService syncPoster = sp.Scene.RequestModuleInterface<ISyncMessagePosterService>();
                if (syncPoster != null)
                {
                    //This does CreateAgent and sends the EnableSimulator/EstablishAgentCommunication/TeleportFinish
                    //  messages if they need to be called and deals with the callback
                    OSDMap map = syncPoster.Get(SyncMessageHelper.TeleportAgent((int)sp.DrawDistance,
                        agentCircuit, agent, teleportFlags, finalDestination, sp.Scene.RegionInfo.RegionHandle), 
                        sp.Scene.RegionInfo.RegionHandle);
                    bool result =  map["Success"].AsBoolean();
                    if (!result)
                    {
                        // Fix the agent status
                        sp.IsChildAgent = false;
                        sp.ControllingClient.SendTeleportFailed(map["Reason"].AsString());
                        return;
                    }
                }
            }

            //Kill the groups here, otherwise they will become ghost attachments 
            //  and stay in the sim, they'll get readded below into the new sim
            KillAttachments(sp);

            // Well, this is it. The agent is over there.
            KillEntity(sp.Scene, sp);

            //Make it a child agent for now... the grid will kill us later if we need to close
            sp.MakeChildAgent();
        }

        protected void KillEntity (IScene scene, IEntity entity)
        {
            scene.ForEachClient(delegate(IClientAPI client)
            {
                client.SendKillObject (scene.RegionInfo.RegionHandle, new IEntity[] { entity });
            });
        }

        protected void KillEntities (IScene scene, IEntity[] grp)
        {
            scene.ForEachClient(delegate(IClientAPI client)
            {
                client.SendKillObject(scene.RegionInfo.RegionHandle, grp);
            });
        }

        #endregion

        #region Client Events

        protected virtual void OnNewClient(IClientAPI client)
        {
            client.OnTeleportHomeRequest += TeleportHome;
            client.OnTeleportCancel += RequestTeleportCancel;
            client.OnTeleportLocationRequest += RequestTeleportLocation;
            client.OnTeleportLandmarkRequest += RequestTeleportLandmark;
        }

        protected virtual void OnClosingClient(IClientAPI client)
        {
            client.OnTeleportHomeRequest -= TeleportHome;
            client.OnTeleportCancel -= RequestTeleportCancel;
            client.OnTeleportLocationRequest -= RequestTeleportLocation;
            client.OnTeleportLandmarkRequest -= RequestTeleportLandmark;
        }

        public void RequestTeleportCancel(IClientAPI client)
        {
            CancelTeleport(client.AgentId, client.Scene.RegionInfo.RegionHandle);
        }

        /// <summary>
        /// Tries to teleport agent to other region.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="regionName"></param>
        /// <param name="position"></param>
        /// <param name="lookAt"></param>
        /// <param name="teleportFlags"></param>
        public void RequestTeleportLocation(IClientAPI remoteClient, string regionName, Vector3 position,
                                            Vector3 lookat, uint teleportFlags)
        {
            GridRegion regionInfo = remoteClient.Scene.RequestModuleInterface<IGridService>().GetRegionByName(UUID.Zero, regionName);
            if (regionInfo == null)
            {
                // can't find the region: Tell viewer and abort
                remoteClient.SendTeleportFailed("The region '" + regionName + "' could not be found.");
                return;
            }

            RequestTeleportLocation(remoteClient, regionInfo, position, lookat, teleportFlags);
        }

        /// <summary>
        /// Tries to teleport agent to other region.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="regionHandle"></param>
        /// <param name="position"></param>
        /// <param name="lookAt"></param>
        /// <param name="teleportFlags"></param>
        public void RequestTeleportLocation(IClientAPI remoteClient, ulong regionHandle, Vector3 position,
                                            Vector3 lookAt, uint teleportFlags)
        {
            Scene scene = (Scene)remoteClient.Scene;
            IScenePresence sp = scene.GetScenePresence(remoteClient.AgentId);
            if (sp != null)
            {
                Teleport(sp, regionHandle, position, lookAt, teleportFlags);
            }
        }

        /// <summary>
        /// Tries to teleport agent to other region.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="regionHandle"></param>
        /// <param name="position"></param>
        /// <param name="lookAt"></param>
        /// <param name="teleportFlags"></param>
        public void RequestTeleportLocation(IClientAPI remoteClient, GridRegion reg, Vector3 position,
                                            Vector3 lookAt, uint teleportFlags)
        {
            Scene scene = (Scene)remoteClient.Scene;
            IScenePresence sp = scene.GetScenePresence(remoteClient.AgentId);
            if (sp != null)
            {
                Teleport(sp, reg, position, lookAt, teleportFlags);
            }
        }

        /// <summary>
        /// Tries to teleport agent to landmark.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="regionHandle"></param>
        /// <param name="position"></param>
        public void RequestTeleportLandmark(IClientAPI remoteClient, UUID regionID, Vector3 position)
        {
            GridRegion info = null;
            try
            {
                info = remoteClient.Scene.RequestModuleInterface<IGridService>().GetRegionByUUID(UUID.Zero, regionID);
            }
            catch( Exception ex)
            {
                m_log.Warn("[EntityTransferModule]: Error finding landmark's region for user " + remoteClient.Name + ", " + ex.ToString());
            }
            if (info == null)
            {
                // can't find the region: Tell viewer and abort
                remoteClient.SendTeleportFailed("The teleport destination could not be found.");
                return;
            }

            RequestTeleportLocation(remoteClient, info, position, Vector3.Zero, (uint)(TeleportFlags.SetLastToTarget | TeleportFlags.ViaLandmark));
        }

        #endregion

        #region Teleport Home

        public virtual void TeleportHome(UUID id, IClientAPI client)
        {
            //m_log.DebugFormat("[ENTITY TRANSFER MODULE]: Request to teleport {0} {1} home", client.FirstName, client.LastName);

            //OpenSim.Services.Interfaces.PresenceInfo pinfo = m_aScene.PresenceService.GetAgent(client.SessionId);
            UserInfo uinfo = client.Scene.RequestModuleInterface<IAgentInfoService>().GetUserInfo(client.AgentId.ToString());

            if (uinfo != null)
            {
                GridRegion regionInfo = client.Scene.GridService.GetRegionByUUID(UUID.Zero, uinfo.HomeRegionID);
                if (regionInfo == null)
                {
                    // can't find the Home region: Tell viewer and abort
                    client.SendTeleportFailed("Your home region could not be found.");
                    return;
                }
                m_log.DebugFormat("[ENTITY TRANSFER MODULE]: User's home region is {0} {1} ({2}-{3})",
                    regionInfo.RegionName, regionInfo.RegionID, regionInfo.RegionLocX / Constants.RegionSize, regionInfo.RegionLocY / Constants.RegionSize);

                RequestTeleportLocation(
                    client, regionInfo, uinfo.HomePosition, uinfo.HomeLookAt,
                    (uint)(TeleportFlags.SetLastToTarget | TeleportFlags.ViaHome));
            }
            else
            {
                //Default region time...
                List<GridRegion> Regions = client.Scene.GridService.GetDefaultRegions(UUID.Zero);
                if (Regions.Count != 0)
                {
                    m_log.DebugFormat("[ENTITY TRANSFER MODULE]: User's home region was not found, using {0} {1} ({2}-{3})",
                        Regions[0].RegionName, Regions[0].RegionID, Regions[0].RegionLocX / Constants.RegionSize, Regions[0].RegionLocY / Constants.RegionSize);

                    RequestTeleportLocation(
                        client, Regions[0], new Vector3(128, 128, 25), new Vector3(128, 128, 128),
                        (uint)(TeleportFlags.SetLastToTarget | TeleportFlags.ViaHome));
                }
            }
        }

        #endregion

        #region Agent Crossings

        public virtual void Cross(IScenePresence agent, bool isFlying, GridRegion crossingRegion)
        {
            Vector3 newposition = new Vector3(agent.AbsolutePosition.X, agent.AbsolutePosition.Y, agent.AbsolutePosition.Z);;

            CrossAgentToNewRegionDelegate d = CrossAgentToNewRegionAsync;
            d.BeginInvoke(agent, newposition, crossingRegion, isFlying, CrossAgentToNewRegionCompleted, d);
        }

        public delegate IScenePresence CrossAgentToNewRegionDelegate(IScenePresence agent, Vector3 pos, GridRegion crossingRegion, bool isFlying);

        /// <summary>
        /// This Closes child agents on neighboring regions
        /// Calls an asynchronous method to do so..  so it doesn't lag the sim.
        /// </summary>
        protected IScenePresence CrossAgentToNewRegionAsync(IScenePresence agent, Vector3 pos,
            GridRegion crossingRegion, bool isFlying)
        {
            m_log.DebugFormat("[EntityTransferModule]: Crossing agent {0} to region {1}", agent.Name, crossingRegion.RegionName);

            IScene m_scene = agent.Scene;

            if (crossingRegion != null)
            {
                //Make sure that all attachments are ready for the teleport
                IAttachmentsModule attModule = agent.Scene.RequestModuleInterface<IAttachmentsModule>();
                if (attModule != null)
                    attModule.ValidateAttachments(agent.UUID);

                int xOffset = crossingRegion.RegionLocX - m_scene.RegionInfo.RegionLocX;
                int yOffset = crossingRegion.RegionLocY - m_scene.RegionInfo.RegionLocY;

                if (xOffset < 0)
                    pos.X += m_scene.RegionInfo.RegionSizeX;
                else if (xOffset > 0)
                    pos.X -= Constants.RegionSize;

                if (yOffset < 0)
                    pos.Y += m_scene.RegionInfo.RegionSizeY;
                else if (yOffset > 0)
                    pos.Y -= Constants.RegionSize;

                //Make sure that they are within bounds (velocity can push it out of bounds)
                if (pos.X < 0)
                    pos.X = 1;
                if (pos.Y < 0)
                    pos.Y = 1;

                if (pos.X > crossingRegion.RegionSizeX)
                    pos.X = crossingRegion.RegionSizeX - 1;
                if (pos.Y > crossingRegion.RegionSizeY)
                    pos.Y = crossingRegion.RegionSizeY - 1;

                AgentData cAgent = new AgentData();
                agent.CopyTo(cAgent);
                cAgent.Position = pos;
                if (isFlying)
                    cAgent.ControlFlags |= (uint)AgentManager.ControlFlags.AGENT_CONTROL_FLY;

                AgentCircuitData agentCircuit = agent.ControllingClient.RequestClientInfo();
                agentCircuit.startpos = pos;
                agentCircuit.child = false;
                IAvatarAppearanceModule appearance = agent.RequestModuleInterface<IAvatarAppearanceModule> ();
                agentCircuit.Appearance = appearance.Appearance;

                IEventQueueService eq = agent.Scene.RequestModuleInterface<IEventQueueService>();
                if (eq != null)
                {
                    //This does UpdateAgent and closing of child agents
                    //  messages if they need to be called
                    ISyncMessagePosterService syncPoster = agent.Scene.RequestModuleInterface<ISyncMessagePosterService>();
                    if (syncPoster != null)
                    {
                        OSDMap map = syncPoster.Get(SyncMessageHelper.CrossAgent(crossingRegion, pos,
                            agent.Velocity, agentCircuit, cAgent, agent.Scene.RegionInfo.RegionHandle),
                            agent.Scene.RegionInfo.RegionHandle);
                        bool result =  map["Success"].AsBoolean();
                        if (!result)
                        {
                            agent.ControllingClient.SendTeleportFailed(map["Reason"].AsString());
                            return agent;
                        }
                    }
                }
                
                agent.MakeChildAgent();
                //Revolution- We already were in this region... we don't need updates about the avatars we already know about, right?
                // now we have a child agent in this region. Request and send all interesting data about (root) agents in the sim
                //agent.SendOtherAgentsAvatarDataToMe();
                //agent.SendOtherAgentsAppearanceToMe();

                //Kill the groups here, otherwise they will become ghost attachments 
                //  and stay in the sim, they'll get readded below into the new sim
                KillAttachments(agent);
            }
            return agent;
        }

        private void KillAttachments(IScenePresence agent)
        {
            IAttachmentsModule attModule = agent.Scene.RequestModuleInterface<IAttachmentsModule>();
            if (attModule != null)
            {
                ISceneEntity[] attachments = attModule.GetAttachmentsForAvatar (agent.UUID);
                foreach (ISceneEntity grp in attachments)
                {
                    //Kill in all clients as it will be readded in the other region
                    KillEntities(agent.Scene, grp.ChildrenEntities().ToArray());
                    //Now remove it from the Scene so that it will not come back
                    agent.Scene.SceneGraph.DeleteEntity(grp);
                    //And from storage as well
                    IBackupModule backup = agent.Scene.RequestModuleInterface<IBackupModule>();
                    if (backup != null)
                        backup.DeleteFromStorage(grp.UUID);
                }
            }
        }

        protected void CrossAgentToNewRegionCompleted(IAsyncResult iar)
        {
            CrossAgentToNewRegionDelegate icon = (CrossAgentToNewRegionDelegate)iar.AsyncState;
            IScenePresence agent = icon.EndInvoke(iar);

            // If the cross was successful, this agent is a child agent
            // Otherwise, put them back in the scene
            if (!agent.IsChildAgent)
            {
                bool m_flying = ((agent.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_FLY) != 0);
                agent.AddToPhysicalScene(m_flying, false);
            }

            // In any case
            agent.NotInTransit();

            //m_log.DebugFormat("[ENTITY TRANSFER MODULE]: Crossing agent {0} {1} completed.", agent.Firstname, agent.Lastname);
        }

        #endregion

        #region Object Transfers

        /// <summary>
        /// Move the given scene object into a new region depending on which region its absolute position has moved
        /// into.
        ///
        /// This method locates the new region handle and offsets the prim position for the new region
        /// </summary>
        /// <param name="attemptedPosition">the attempted out of region position of the scene object</param>
        /// <param name="grp">the scene object that we're crossing</param>
        public bool CrossGroupToNewRegion(SceneObjectGroup grp, Vector3 attemptedPosition, GridRegion destination)
        {
            if (grp == null)
                return false;
            if (grp.IsDeleted)
                return false;

            if (grp.Scene == null)
                return false;
            if (grp.RootPart.DIE_AT_EDGE)
            {
                // We remove the object here
                try
                {
                    IBackupModule backup = grp.Scene.RequestModuleInterface<IBackupModule>();
                    if (backup != null)
                        return backup.DeleteSceneObjects(new SceneObjectGroup[1] { grp }, true);
                }
                catch (Exception)
                {
                    m_log.Warn("[DATABASE]: exception when trying to remove the prim that crossed the border.");
                }
                return false;
            }

            if (grp.RootPart.RETURN_AT_EDGE)
            {
                // We remove the object here
                try
                {
                    List<ISceneEntity> objects = new List<ISceneEntity> () { grp };
                    ILLClientInventory inventoryModule = grp.Scene.RequestModuleInterface<ILLClientInventory>();
                    if (inventoryModule != null)
                        return inventoryModule.ReturnObjects(objects.ToArray(), UUID.Zero);
                }
                catch (Exception)
                {
                    m_log.Warn("[SCENE]: exception when trying to return the prim that crossed the border.");
                }
                return false;
            }

            Vector3 oldGroupPosition = grp.RootPart.GroupPosition;
            // If we fail to cross the border, then reset the position of the scene object on that border.
            if (destination != null && !CrossPrimGroupIntoNewRegion(destination, grp, attemptedPosition))
            {
                grp.OffsetForNewRegion(oldGroupPosition);
                grp.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Move the given scene object into a new region
        /// </summary>
        /// <param name="newRegionHandle"></param>
        /// <param name="grp">Scene Object Group that we're crossing</param>
        /// <returns>
        /// true if the crossing itself was successful, false on failure
        /// </returns>
        protected bool CrossPrimGroupIntoNewRegion(GridRegion destination, SceneObjectGroup grp, Vector3 attemptedPos)
        {
            bool successYN = false;
            grp.RootPart.ClearUpdateScheduleOnce();
            if (destination != null)
            {
                if (grp.RootPart.SitTargetAvatar.Count != 0)
                {
                    lock (grp.RootPart.SitTargetAvatar)
                    {
                        foreach (UUID avID in grp.RootPart.SitTargetAvatar)
                        {
                            IScenePresence SP = grp.Scene.GetScenePresence(avID);
                            CrossAgentToNewRegionAsync(SP, grp.AbsolutePosition, destination, false);
                        }
                    }
                }

                SceneObjectGroup copiedGroup = (SceneObjectGroup)grp.Copy(false);
                copiedGroup.SetAbsolutePosition(true, attemptedPos);
                if (grp.Scene != null && grp.Scene.SimulationService != null)
                    successYN = grp.Scene.SimulationService.CreateObject(destination, copiedGroup);

                if (successYN)
                {
                    // We remove the object here
                    try
                    {
                        foreach (SceneObjectPart part in grp.ChildrenList)
                        {
                            lock (part.SitTargetAvatar)
                            {
                                part.SitTargetAvatar.Clear();
                            }
                        }
                        IBackupModule backup = grp.Scene.RequestModuleInterface<IBackupModule>();
                        if (backup != null)
                            return backup.DeleteSceneObjects(new SceneObjectGroup[1] { grp }, false);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat(
                            "[ENTITY TRANSFER MODULE]: Exception deleting the old object left behind on a border crossing for {0}, {1}",
                            grp, e);
                    }
                }
                else
                {
                    if (!grp.IsDeleted)
                    {
                        if (grp.RootPart.PhysActor != null)
                        {
                            grp.RootPart.PhysActor.CrossingFailure();
                        }
                    }

                    m_log.ErrorFormat("[ENTITY TRANSFER MODULE]: Prim crossing failed for {0}", grp);
                }
            }
            else
            {
                m_log.Error("[ENTITY TRANSFER MODULE]: destination was unexpectedly null in Scene.CrossPrimGroupIntoNewRegion()");
            }

            return successYN;
        }

        #endregion

        #region Incoming Object Transfers

        /// <summary>
        /// Attachment rezzing
        /// </summary>
        /// <param name="userID">Agent Unique ID</param>
        /// <param name="itemID">Inventory Item ID to rez</param>
        /// <returns>False</returns>
        public virtual bool IncomingCreateObject(UUID regionID, UUID userID, UUID itemID)
        {
            /*//m_log.DebugFormat(" >>> IncomingCreateObject(userID, itemID) <<< {0} {1}", userID, itemID);
            Scene scene = GetScene(regionID);
            if (scene == null)
                return false;
            ScenePresence sp = scene.GetScenePresence(userID);
            IAttachmentsModule attachMod = scene.RequestModuleInterface<IAttachmentsModule>();
            if (sp != null && attachMod != null)
            {
                m_log.DebugFormat(
                        "[EntityTransferModule]: Received attachment via new attachment method {0} for agent {1}", itemID, sp.Name);
                int attPt = sp.Appearance.GetAttachpoint(itemID);
                attachMod.RezSingleAttachmentFromInventory(sp.ControllingClient, itemID, attPt, true);
                return true;
            }*/

            return false;
        }

        /// <summary>
        /// Called when objects or attachments cross the border, or teleport, between regions.
        /// </summary>
        /// <param name="sog"></param>
        /// <returns></returns>
        public virtual bool IncomingCreateObject(UUID regionID, ISceneObject sog)
        {
            Scene scene = GetScene(regionID);
            if (scene == null)
                return false;
            
            //m_log.Debug(" >>> IncomingCreateObject(sog) <<< " + ((SceneObjectGroup)sog).AbsolutePosition + " deleted? " + ((SceneObjectGroup)sog).IsDeleted);
            SceneObjectGroup newObject;
            try
            {
                newObject = (SceneObjectGroup)sog;
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[EntityTransferModule]: Problem casting object: {0}", e.Message);
                return false;
            }

            if (!AddSceneObject(scene, newObject))
            {
                m_log.WarnFormat("[EntityTransferModule]: Problem adding scene object {0} in {1} ", sog.UUID, scene.RegionInfo.RegionName);
                return false;
            }

            newObject.RootPart.ParentGroup.CreateScriptInstances(0, false, 1, UUID.Zero);
            newObject.RootPart.ParentGroup.ResumeScripts();

            if (newObject.RootPart.SitTargetAvatar.Count != 0)
            {
                lock (newObject.RootPart.SitTargetAvatar)
                {
                    foreach (UUID avID in newObject.RootPart.SitTargetAvatar)
                    {
                        IScenePresence SP = scene.GetScenePresence(avID);
                        while (SP == null)
                        {
                            Thread.Sleep(20);
                        }
                        SP.AbsolutePosition = newObject.AbsolutePosition;
                        SP.CrossSittingAgent(SP.ControllingClient, newObject.RootPart.UUID);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Adds a Scene Object group to the Scene.
        /// Verifies that the creator of the object is not banned from the simulator.
        /// Checks if the item is an Attachment
        /// </summary>
        /// <param name="sceneObject"></param>
        /// <returns>True if the SceneObjectGroup was added, False if it was not</returns>
        public bool AddSceneObject(Scene scene, SceneObjectGroup sceneObject)
        {
            // If the user is banned, we won't let any of their objects
            // enter. Period.
            //
            if (scene.RegionInfo.EstateSettings.IsBanned(sceneObject.OwnerID))
            {
                m_log.Info("[EntityTransferModule]: Denied prim crossing for banned avatar");

                return false;
            }

            if (!sceneObject.IsAttachmentCheckFull()) // Not Attachment
            {
                if (!scene.Permissions.CanObjectEntry(sceneObject.UUID,
                        true, sceneObject.AbsolutePosition, sceneObject.OwnerID))
                {
                    // Deny non attachments based on parcel settings
                    //
                    m_log.Info("[EntityTransferModule]: Denied prim crossing " +
                            "because of parcel settings");

                    IBackupModule backup = scene.RequestModuleInterface<IBackupModule>();
                    if (backup != null)
                        backup.DeleteSceneObjects(new SceneObjectGroup[1] { sceneObject }, true);

                    return false;
                }
                if (scene.SceneGraph.AddPrimToScene(sceneObject))
                {
                    if(sceneObject.RootPart.IsSelected)
                        sceneObject.RootPart.CreateSelected = true;
                    sceneObject.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Misc

        public void CancelTeleport(UUID AgentID, ulong RegionHandle)
        {
            ISyncMessagePosterService syncPoster = m_scenes[0].RequestModuleInterface<ISyncMessagePosterService>();
            if (syncPoster != null)
            {
                syncPoster.Post(SyncMessageHelper.CancelTeleport(AgentID, RegionHandle), RegionHandle);
            }
        }

        /// <summary>
        /// This 'can' return null, so be careful
        /// </summary>
        /// <param name="RegionID"></param>
        /// <returns></returns>
        public Scene GetScene(UUID RegionID)
        {
            foreach (Scene scene in m_scenes)
            {
                if (scene.RegionInfo.RegionID == RegionID)
                    return scene;
            }
            return null;
        }

        #endregion

        #region RegionComms

        /// <summary>
        /// Do the work necessary to initiate a new user connection for a particular scene.
        /// At the moment, this consists of setting up the caps infrastructure
        /// The return bool should allow for connections to be refused, but as not all calling paths
        /// take proper notice of it let, we allowed banned users in still.
        /// </summary>
        /// <param name="agent">CircuitData of the agent who is connecting</param>
        /// <param name="reason">Outputs the reason for the false response on this string,
        /// If the agent was accepted, this will be the Caps SEED for the region</param>
        /// <param name="requirePresenceLookup">True for normal presence. False for NPC
        /// or other applications where a full grid/Hypergrid presence may not be required.</param>
        /// <returns>True if the region accepts this agent.  False if it does not.  False will 
        /// also return a reason.</returns>
        public bool NewUserConnection (IScene scene, AgentCircuitData agent, uint teleportFlags, out string reason)
        {
            reason = String.Empty;

            // Don't disable this log message - it's too helpful
            m_log.DebugFormat (
                "[ConnectionBegin]: Region {0} told of incoming {1} agent {2} (circuit code {3}, teleportflags {4})",
                scene.RegionInfo.RegionName, (agent.child ? "child" : "root"), agent.AgentID,
                agent.circuitcode, teleportFlags);

            if (!AuthorizeUser (scene, agent, out reason))
            {
                OSDMap map = new OSDMap ();
                map["Reason"] = reason;
                map["Success"] = false;
                reason = OSDParser.SerializeJsonString (map);
                return false;
            }

            IScenePresence sp = scene.GetScenePresence (agent.AgentID);

            if (sp != null && !sp.IsChildAgent)
            {
                // We have a zombie from a crashed session. 
                // Or the same user is trying to be root twice here, won't work.
                // Kill it.
                m_log.InfoFormat ("[Scene]: Zombie scene presence detected for {0} in {1}", agent.AgentID, scene.RegionInfo.RegionName);
                scene.RemoveAgent (sp);
                sp = null;
            }

            //Add possible Urls for the given agent
            IConfigurationService configService = scene.RequestModuleInterface<IConfigurationService> ();
            if (configService != null && agent.OtherInformation.ContainsKey ("UserUrls"))
            {
                configService.AddNewUser (agent.AgentID.ToString (), (OSDMap)agent.OtherInformation["UserUrls"]);
            }

            OSDMap responseMap = new OSDMap ();
            responseMap["CapsUrls"] = scene.EventManager.TriggerOnRegisterCaps (agent.AgentID);

            // In all cases, add or update the circuit data with the new agent circuit data and teleport flags
            agent.teleportFlags = teleportFlags;

            //Add the circuit at the end
            scene.AuthenticateHandler.AddNewCircuit (agent.circuitcode, agent);

            responseMap["Agent"] = agent.PackAgentCircuitData ();

            scene.AuroraEventManager.FireGenericEventHandler ("NewUserConnection", responseMap);

            m_log.InfoFormat (
                "[ConnectionBegin]: Region {0} authenticated and authorized incoming {1} agent {2} (circuit code {3})",
                scene.RegionInfo.RegionName, (agent.child ? "child" : "root"), agent.AgentID,
                agent.circuitcode);

            responseMap["Success"] = true;
            reason = OSDParser.SerializeJsonString (responseMap);
            return true;
        }

        /// <summary>
        /// Verify if the user can connect to this region.  Checks the banlist and ensures that the region is set for public access
        /// </summary>
        /// <param name="agent">The circuit data for the agent</param>
        /// <param name="reason">outputs the reason to this string</param>
        /// <returns>True if the region accepts this agent.  False if it does not.  False will 
        /// also return a reason.</returns>
        protected bool AuthorizeUser (IScene scene, AgentCircuitData agent, out string reason)
        {
            reason = String.Empty;

            IAuthorizationService AuthorizationService = scene.RequestModuleInterface<IAuthorizationService> ();
            if (AuthorizationService != null)
            {
                GridRegion ourRegion = new GridRegion (scene.RegionInfo);
                if (!AuthorizationService.IsAuthorizedForRegion (ourRegion, agent, !agent.child, out reason))
                {
                    m_log.WarnFormat ("[ConnectionBegin]: Denied access to {0} at {1} because the user does not have access to the region, reason: {2}",
                                     agent.AgentID, scene.RegionInfo.RegionName, reason);
                    reason = String.Format ("You do not have access to the region {0}, reason: {1}", scene.RegionInfo.RegionName, reason);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// We've got an update about an agent that sees into this region, 
        /// send it to ScenePresence for processing  It's the full data.
        /// </summary>
        /// <param name="cAgentData">Agent that contains all of the relevant things about an agent.
        /// Appearance, animations, position, etc.</param>
        /// <returns>true if we handled it.</returns>
        public virtual bool IncomingChildAgentDataUpdate (IScene scene, AgentData cAgentData)
        {
            //m_log.DebugFormat(
            //    "[SCENE]: Incoming child agent update for {0} in {1}", cAgentData.AgentID, RegionInfo.RegionName);

            //No null updates!
            if (cAgentData == null)
                return false;

            // We have to wait until the viewer contacts this region after receiving EAC.
            // That calls AddNewClient, which finally creates the ScenePresence and then this gets set up
            // So if the client isn't here yet, save the update for them when they get into the region fully
            IScenePresence SP = scene.GetScenePresence (cAgentData.AgentID);
            if (SP != null)
                SP.ChildAgentDataUpdate (cAgentData);
            else
                lock (m_incomingChildAgentData)
                {
                    if (!m_incomingChildAgentData.ContainsKey (scene))
                        m_incomingChildAgentData.Add (scene, new Dictionary<UUID, AgentData> ());
                    m_incomingChildAgentData[scene][cAgentData.AgentID] = cAgentData;
                }
            return true;
        }

        /// <summary>
        /// We've got an update about an agent that sees into this region, 
        /// send it to ScenePresence for processing  It's only positional data
        /// </summary>
        /// <param name="cAgentData">AgentPosition that contains agent positional data so we can know what to send</param>
        /// <returns>true if we handled it.</returns>
        public virtual bool IncomingChildAgentDataUpdate (IScene scene, AgentPosition cAgentData)
        {
            //m_log.Debug(" XXX Scene IncomingChildAgentDataUpdate POSITION in " + RegionInfo.RegionName);
            IScenePresence presence = scene.GetScenePresence (cAgentData.AgentID);
            if (presence != null)
            {
                // I can't imagine *yet* why we would get an update if the agent is a root agent..
                // however to avoid a race condition crossing borders..
                if (presence.IsChildAgent)
                {
                    uint rRegionX = 0;
                    uint rRegionY = 0;
                    //In meters
                    Utils.LongToUInts (cAgentData.RegionHandle, out rRegionX, out rRegionY);
                    //In meters
                    int tRegionX = scene.RegionInfo.RegionLocX;
                    int tRegionY = scene.RegionInfo.RegionLocY;
                    //Send Data to ScenePresence
                    presence.ChildAgentDataUpdate (cAgentData, tRegionX, tRegionY, (int)rRegionX, (int)rRegionY);
                }

                return true;
            }

            return false;
        }

        public virtual bool IncomingRetrieveRootAgent (IScene scene, UUID id, out IAgentData agent)
        {
            agent = null;
            IScenePresence sp = scene.GetScenePresence (id);
            if ((sp != null) && (!sp.IsChildAgent))
            {
                AgentData data = new AgentData ();
                sp.CopyTo (data);
                agent = data;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tell a single agent to disconnect from the region.
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="agentID"></param>
        public bool IncomingCloseAgent (IScene scene, UUID agentID)
        {
            //m_log.DebugFormat("[SCENE]: Processing incoming close agent for {0}", agentID);

            IScenePresence presence = scene.GetScenePresence (agentID);
            if (presence != null)
            {
                bool RetVal = scene.RemoveAgent (presence);

                ISyncMessagePosterService syncPoster = scene.RequestModuleInterface<ISyncMessagePosterService> ();
                if (syncPoster != null)
                {
                    //Tell the grid that we are logged out
                    syncPoster.Post (SyncMessageHelper.DisableSimulator (presence.UUID, scene.RegionInfo.RegionHandle), scene.RegionInfo.RegionHandle);
                }

                return RetVal;
            }
            return false;
        }

        #endregion
    }
}
