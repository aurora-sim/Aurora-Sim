/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
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
using System.Linq;
using System.Net;
using System.Reflection;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

using GridRegion = OpenSim.Services.Interfaces.GridRegion;

using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Nini.Config;

namespace Aurora.Modules.EntityTransfer
{
    public class EntityTransferModule : ISharedRegionModule, IEntityTransferModule
    {
        #region Declares

        protected bool m_Enabled = false;
        protected List<IScene> m_scenes = new List<IScene> ();
        private readonly Dictionary<IScene, Dictionary<UUID, AgentData>> m_incomingChildAgentData = new Dictionary<IScene, Dictionary<UUID, AgentData>> ();
        
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
                    //MainConsole.Instance.InfoFormat("[ENTITY TRANSFER MODULE]: {0} enabled.", Name);
                }
            }
        }

        public virtual void PostInitialise()
        {
        }

        public virtual void AddRegion (IScene scene)
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

        public virtual void RemoveRegion (IScene scene)
        {
            if (!m_Enabled)
                return;
            m_scenes.Remove(scene);

            scene.UnregisterModuleInterface<IEntityTransferModule>(this);
            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnNewPresence -= EventManager_OnNewPresence;
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public virtual void RegionLoaded (IScene scene)
        {
        }


        #endregion

        #region Agent Teleports

        public virtual void Teleport(IScenePresence sp, ulong regionHandle, Vector3 position, Vector3 lookAt, uint teleportFlags)
        {
            int x = 0, y = 0;
            Util.UlongToInts(regionHandle, out x, out y);

            GridRegion reg = sp.Scene.GridService.GetRegionByPosition (sp.ControllingClient.AllScopeIDs, x, y);
            
            if (reg == null)
            {
                List<GridRegion> regions = sp.Scene.GridService.GetRegionRange(sp.ControllingClient.AllScopeIDs, x - (sp.Scene.GridService.GetRegionViewSize() * sp.Scene.RegionInfo.RegionSizeX),
                    x + (sp.Scene.GridService.GetRegionViewSize() * sp.Scene.RegionInfo.RegionSizeX),
                    y - (sp.Scene.GridService.GetRegionViewSize() * sp.Scene.RegionInfo.RegionSizeY),
                    y + (sp.Scene.GridService.GetRegionViewSize() * sp.Scene.RegionInfo.RegionSizeY));
                foreach (GridRegion r in regions)
                {
                    if (r.RegionLocX <= x && r.RegionLocX + r.RegionSizeX > x &&
                        r.RegionLocY <= y && r.RegionLocY + r.RegionSizeY > y)
                    {
                        reg = r;
                        position.X += x - reg.RegionLocX;
                        position.Y += y - reg.RegionLocY;
                        break;
                    }
                }
                if (reg == null)
                {
                    // TP to a place that doesn't exist (anymore)
                    // Inform the viewer about that
                    sp.ControllingClient.SendTeleportFailed ("The region you tried to teleport to doesn't exist anymore");

                    // and set the map-tile to '(Offline)'
                    int regX, regY;
                    Util.UlongToInts (regionHandle, out regX, out regY);

                    MapBlockData block = new MapBlockData
                                             {
                                                 X = (ushort) (regX/Constants.RegionSize),
                                                 Y = (ushort) (regY/Constants.RegionSize),
                                                 Access = 254
                                             };
                    // == not there

                    List<MapBlockData> blocks = new List<MapBlockData> {block};
                    sp.ControllingClient.SendMapBlock (blocks, 0);
                    return;
                }
            }
            Teleport(sp, reg, position, lookAt, teleportFlags);
        }

        public virtual void Teleport(IScenePresence sp, GridRegion finalDestination, Vector3 position, Vector3 lookAt, uint teleportFlags)
        {
            sp.ControllingClient.SendTeleportStart(teleportFlags);
            sp.ControllingClient.SendTeleportProgress(teleportFlags, "requesting");

            // Reset animations; the viewer does that in teleports.
            if(sp.Animator != null)
                sp.Animator.ResetAnimations();

            try
            {
                string reason = "";
                if (finalDestination.RegionHandle == sp.Scene.RegionInfo.RegionHandle)
                {
                    //First check whether the user is allowed to move at all
                    if (!sp.Scene.Permissions.AllowedOutgoingLocalTeleport(sp.UUID, out reason))
                    {
                        sp.ControllingClient.SendTeleportFailed(reason);
                        return;
                    }
                    //Now respect things like parcel bans with this
                    if (!sp.Scene.Permissions.AllowedIncomingTeleport(sp.UUID, position, teleportFlags, out position, out reason))
                    {
                        sp.ControllingClient.SendTeleportFailed(reason);
                        return;
                    }
                    MainConsole.Instance.DebugFormat(
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
                    //MainConsole.Instance.DebugFormat("[ENTITY TRANSFER MODULE]: Final destination is x={0} y={1} uuid={2}",
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
                MainConsole.Instance.ErrorFormat("[ENTITY TRANSFER MODULE]: Exception on teleport: {0}\n{1}", e.Message, e.StackTrace);
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

            MainConsole.Instance.DebugFormat(
                "[ENTITY TRANSFER MODULE]: Request Teleport to {0}:{1}/{2}",
                finalDestination.ServerURI, finalDestination.RegionName, position);

            sp.ControllingClient.SendTeleportProgress(teleportFlags, "arriving");
            sp.SetAgentLeaving(finalDestination);

            // Fixing a bug where teleporting while sitting results in the avatar ending up removed from
            // both regions
            if (sp.ParentID != UUID.Zero)
                sp.StandUp();

            AgentCircuitData agentCircuit = BuildCircuitDataForPresence(sp, position);

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
                    syncPoster.Get(SyncMessageHelper.TeleportAgent((int)sp.DrawDistance,
                        agentCircuit, agent, teleportFlags, finalDestination, sp.Scene.RegionInfo.RegionHandle),
                        sp.UUID, sp.Scene.RegionInfo.RegionHandle, (map) =>
                        {
                            if (map == null || !map["success"].AsBoolean())
                            {
                                // Fix the agent status
                                sp.IsChildAgent = false;
                                //Tell modules about it
                                sp.AgentFailedToLeave();
                                sp.ControllingClient.SendTeleportFailed(map != null
                                                                            ? map["Reason"].AsString()
                                                                            : "Teleport Failed");
                                return;
                            }
                            //Get the new destintation, it may have changed
                            if (map.ContainsKey("Destination"))
                            {
                                finalDestination = new GridRegion();
                                finalDestination.FromOSD((OSDMap)map["Destination"]);
                            }
                            MakeChildAgent(sp, finalDestination, false);
                        });
                }
            }

        }

        private AgentCircuitData BuildCircuitDataForPresence(IScenePresence sp, Vector3 position)
        {
            AgentCircuitData agentCircuit = sp.ControllingClient.RequestClientInfo();
            agentCircuit.startpos = position;
            //The agent will be a root agent
            agentCircuit.child = false;
            //Make sure the appearnace is right
            IAvatarAppearanceModule appearance = sp.RequestModuleInterface<IAvatarAppearanceModule>();
            if (appearance != null && appearance.Appearance != null)
                agentCircuit.Appearance = appearance.Appearance;
            else
                MainConsole.Instance.Error("[EntityTransferModule]: No appearance is being packed as we could not find the appearance ? " + appearance == null);
            AgentCircuitData oldCircuit = sp.Scene.AuthenticateHandler.GetAgentCircuitData(sp.UUID);
            agentCircuit.ServiceURLs = oldCircuit.ServiceURLs;
            agentCircuit.firstname = oldCircuit.firstname;
            agentCircuit.lastname = oldCircuit.lastname;
            agentCircuit.ServiceSessionID = oldCircuit.ServiceSessionID;
            return agentCircuit;
        }

        public void MakeChildAgent (IScenePresence sp, GridRegion finalDestination, bool markAgentAsLeaving)
        {
            if(sp == null)
                return;

            if(markAgentAsLeaving)
                sp.SetAgentLeaving(null);
            sp.Scene.AuroraEventManager.FireGenericEventHandler("SendingAttachments", new object[] { finalDestination, sp });

            //Kill the groups here, otherwise they will become ghost attachments 
            //  and stay in the sim, they'll get readded below into the new sim
            //KillAttachments(sp);

            // Well, this is it. The agent is over there.
            KillEntity(sp.Scene, sp);

            //Make it a child agent for now... the grid will kill us later if we need to close
            sp.MakeChildAgent(finalDestination);
        }

        protected void KillEntity (IScene scene, IEntity entity)
        {
            scene.ForEachClient(delegate(IClientAPI client)
            {
                if(client.AgentId != entity.UUID)
                    client.SendKillObject (scene.RegionInfo.RegionHandle, new[] { entity });
            });
        }

        protected void KillEntities (IScenePresence sp, IEntity[] grp)
        {
            sp.Scene.ForEachClient(delegate(IClientAPI client)
            {
                if(sp.UUID != client.AgentId)//Don't send kill requests to us, it'll just look jerky
                    client.SendKillObject(sp.Scene.RegionInfo.RegionHandle, grp);
            });
        }

        #endregion

        #region Client Events

        protected virtual void OnNewClient(IClientAPI client)
        {
            client.OnTeleportHomeRequest += ClientTeleportHome;
            client.OnTeleportCancel += RequestTeleportCancel;
            client.OnTeleportLocationRequest += RequestTeleportLocation;
            client.OnTeleportLandmarkRequest += RequestTeleportLandmark;
        }

        protected virtual void OnClosingClient(IClientAPI client)
        {
            client.OnTeleportHomeRequest -= ClientTeleportHome;
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
        /// <param name="lookat"></param>
        /// <param name="teleportFlags"></param>
        public void RequestTeleportLocation(IClientAPI remoteClient, string regionName, Vector3 position,
                                            Vector3 lookat, uint teleportFlags)
        {
            GridRegion regionInfo = remoteClient.Scene.RequestModuleInterface<IGridService>().GetRegionByName(remoteClient.AllScopeIDs, regionName);
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
            IScenePresence sp = remoteClient.Scene.GetScenePresence(remoteClient.AgentId);
            if (sp != null)
            {
                Teleport(sp, regionHandle, position, lookAt, teleportFlags);
            }
        }

        /// <summary>
        /// Tries to teleport agent to other region.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="reg"></param>
        /// <param name="position"></param>
        /// <param name="lookAt"></param>
        /// <param name="teleportFlags"></param>
        public void RequestTeleportLocation(IClientAPI remoteClient, GridRegion reg, Vector3 position,
                                            Vector3 lookAt, uint teleportFlags)
        {
            IScenePresence sp = remoteClient.Scene.GetScenePresence(remoteClient.AgentId);
            if (sp != null)
            {
                Teleport(sp, reg, position, lookAt, teleportFlags);
            }
        }

        /// <summary>
        /// Tries to teleport agent to landmark.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="gatekeeperURL"></param>
        /// <param name="position"></param>
        /// <param name="regionID"></param>
        public void RequestTeleportLandmark (IClientAPI remoteClient, UUID regionID, string gatekeeperURL, Vector3 position)
        {
            GridRegion info = null;
            try
            {
                info = remoteClient.Scene.RequestModuleInterface<IGridService>().GetRegionByUUID(remoteClient.AllScopeIDs, regionID);
            }
            catch( Exception ex)
            {
                MainConsole.Instance.Warn("[EntityTransferModule]: Error finding landmark's region for user " + remoteClient.Name + ", " + ex);
            }
            if (info == null)
            {
                if (!string.IsNullOrEmpty(gatekeeperURL))
                {
                    info = new GridRegion
                               {
                                   ServerURI = gatekeeperURL,
                                   RegionID = regionID,
                                   Flags =
                                       (int)
                                       (Aurora.Framework.RegionFlags.Foreign | Aurora.Framework.RegionFlags.Hyperlink)
                               };
                }
                else
                {
                    // can't find the region: Tell viewer and abort
                    remoteClient.SendTeleportFailed ("The teleport destination could not be found.");
                    return;
                }
            }

            RequestTeleportLocation(remoteClient, info, position, Vector3.Zero, (uint)(TeleportFlags.SetLastToTarget | TeleportFlags.ViaLandmark));
        }

        #endregion

        #region Teleport Home

        public void ClientTeleportHome (UUID id, IClientAPI client)
        {
            TeleportHome(id, client);
        }

        public virtual bool TeleportHome(UUID id, IClientAPI client)
        {
            //MainConsole.Instance.DebugFormat("[ENTITY TRANSFER MODULE]: Request to teleport {0} {1} home", client.FirstName, client.LastName);

            //OpenSim.Services.Interfaces.PresenceInfo pinfo = m_aScene.PresenceService.GetAgent(client.SessionId);
            UserInfo uinfo = client.Scene.RequestModuleInterface<IAgentInfoService>().GetUserInfo(client.AgentId.ToString());
            IUserAgentService uas = client.Scene.RequestModuleInterface<IUserAgentService> ();
                
            if (uinfo != null)
            {
                GridRegion regionInfo = client.Scene.GridService.GetRegionByUUID(client.AllScopeIDs, uinfo.HomeRegionID);
                if (regionInfo == null)
                {
                    Vector3 position = Vector3.Zero, lookAt = Vector3.Zero;
                    if (uas != null)
                        regionInfo = uas.GetHomeRegion(client.Scene.AuthenticateHandler.GetAgentCircuitData(client.AgentId), out position, out lookAt);
                    if (regionInfo == null)
                    {
                        //can't find the Home region: Tell viewer and abort
                        client.SendTeleportFailed ("Your home region could not be found.");
                        return false;
                    }
                }
                MainConsole.Instance.DebugFormat("[ENTITY TRANSFER MODULE]: User's home region is {0} {1} ({2}-{3})",
                    regionInfo.RegionName, regionInfo.RegionID, regionInfo.RegionLocX / Constants.RegionSize, regionInfo.RegionLocY / Constants.RegionSize);

                RequestTeleportLocation(
                    client, regionInfo, uinfo.HomePosition, uinfo.HomeLookAt,
                    (uint)(TeleportFlags.SetLastToTarget | TeleportFlags.ViaHome));
            }
            else
            {
                //Default region time...
                List<GridRegion> Regions = client.Scene.GridService.GetDefaultRegions(client.AllScopeIDs);
                if(Regions.Count != 0)
                {
                    MainConsole.Instance.DebugFormat("[ENTITY TRANSFER MODULE]: User's home region was not found, using {0} {1} ({2}-{3})",
                        Regions[0].RegionName, Regions[0].RegionID, Regions[0].RegionLocX / Constants.RegionSize, Regions[0].RegionLocY / Constants.RegionSize);

                    RequestTeleportLocation(
                        client, Regions[0], new Vector3(128, 128, 25), new Vector3(128, 128, 128),
                        (uint)(TeleportFlags.SetLastToTarget | TeleportFlags.ViaHome));
                }
                else
                    return false;
            }
            return true;
        }

        #endregion

        #region Agent Crossings

        public virtual void Cross(IScenePresence agent, bool isFlying, GridRegion crossingRegion)
        {
            Vector3 pos = new Vector3(agent.AbsolutePosition.X, agent.AbsolutePosition.Y, agent.AbsolutePosition.Z);
            pos.X = (agent.Scene.RegionInfo.RegionLocX + pos.X) - crossingRegion.RegionLocX;
            pos.Y = (agent.Scene.RegionInfo.RegionLocY + pos.Y) - crossingRegion.RegionLocY;

            //Make sure that they are within bounds (velocity can push it out of bounds)
            if (pos.X < 0)
                pos.X = 1;
            if (pos.Y < 0)
                pos.Y = 1;

            if (pos.X > crossingRegion.RegionSizeX)
                pos.X = crossingRegion.RegionSizeX - 1;
            if (pos.Y > crossingRegion.RegionSizeY)
                pos.Y = crossingRegion.RegionSizeY - 1;
            InternalCross(agent, pos, isFlying, crossingRegion);
        }

        public virtual void InternalCross(IScenePresence agent, Vector3 attemptedPos, bool isFlying, GridRegion crossingRegion)
        {
            if(agent.PhysicsActor != null)
                agent.PhysicsActor.IsPhysical = false;
            
            MainConsole.Instance.DebugFormat("[EntityTransferModule]: Crossing agent {0} to region {1}", agent.Name, crossingRegion.RegionName);

            try
            {
                agent.SetAgentLeaving(crossingRegion);

                AgentData cAgent = new AgentData();
                agent.CopyTo(cAgent);
                cAgent.Position = attemptedPos;
                if (isFlying)
                    cAgent.ControlFlags |= (uint)AgentManager.ControlFlags.AGENT_CONTROL_FLY;

                AgentCircuitData agentCircuit = BuildCircuitDataForPresence(agent, attemptedPos);
                agentCircuit.teleportFlags = (uint)TeleportFlags.ViaRegionID;

                IEventQueueService eq = agent.Scene.RequestModuleInterface<IEventQueueService>();
                if (eq != null)
                {
                    //This does UpdateAgent and closing of child agents
                    //  messages if they need to be called
                    ISyncMessagePosterService syncPoster =
                        agent.Scene.RequestModuleInterface<ISyncMessagePosterService>();
                    if (syncPoster != null)
                    {
                        syncPoster.Get(SyncMessageHelper.CrossAgent(crossingRegion, attemptedPos,
                            agent.Velocity, agentCircuit, cAgent,
                            agent.Scene.RegionInfo.RegionHandle),
                            agent.UUID, agent.Scene.RegionInfo.RegionHandle,
                            (map) =>
                            {
                                if (map == null || !map["success"].AsBoolean())
                                {
                                    //Tell modules that we have failed
                                    agent.AgentFailedToLeave();
                                    if (map != null)
                                    {
                                        if (map.ContainsKey("Note") && !map["Note"].AsBoolean())
                                            return;
                                        agent.ControllingClient.SendTeleportFailed(map["Reason"].AsString());
                                    }
                                    else
                                        agent.ControllingClient.SendTeleportFailed("TP Failed");
                                    if (agent.PhysicsActor != null)
                                        agent.PhysicsActor.IsPhysical = true; //Fix the setting that we set earlier
                                    // In any case
                                    agent.FailedCrossingTransit(crossingRegion);
                                    return;
                                }
                                //We're killing the animator and the physics actor, so we don't need to worry about agent.PhysicsActor.IsPhysical
                                agent.MakeChildAgent(crossingRegion);
                                //Revolution- We already were in this region... we don't need updates about the avatars we already know about, right?
                                // OLD: now we have a child agent in this region. Request and send all interesting data about (root) agents in the sim
                                //agent.SendOtherAgentsAvatarDataToMe();
                                //agent.SendOtherAgentsAppearanceToMe();

                                //Kill the groups here, otherwise they will become ghost attachments 
                                //  and stay in the sim, they'll get readded below into the new sim
                                //KillAttachments(agent);
                                // In any case
                                agent.SuccessfulCrossingTransit(crossingRegion);
                            });
                    }
                }

            }
            catch(Exception ex)
            {
                MainConsole.Instance.Warn("[EntityTransferModule]: Exception in crossing: " + ex);
            }
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
                    KillEntities(agent, grp.ChildrenEntities().ToArray());
                    //Now remove it from the Scene so that it will not come back
                    agent.Scene.SceneGraph.DeleteEntity(grp);
                }
            }
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
        ///<param name="destination"></param>
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
                        return backup.DeleteSceneObjects(new[] { grp }, true, true);
                }
                catch (Exception)
                {
                    MainConsole.Instance.Warn("[DATABASE]: exception when trying to remove the prim that crossed the border.");
                }
                return false;
            }

            if (grp.RootPart.RETURN_AT_EDGE)
            {
                // We remove the object here
                try
                {
                    List<ISceneEntity> objects = new List<ISceneEntity> { grp };
                    ILLClientInventory inventoryModule = grp.Scene.RequestModuleInterface<ILLClientInventory>();
                    if (inventoryModule != null)
                        return inventoryModule.ReturnObjects(objects.ToArray(), UUID.Zero);
                }
                catch (Exception)
                {
                    MainConsole.Instance.Warn("[SCENE]: exception when trying to return the prim that crossed the border.");
                }
                return false;
            }

            Vector3 oldGroupPosition = grp.RootPart.GroupPosition;
            // If we fail to cross the border, then reset the position of the scene object on that border.
            if (destination != null && !CrossPrimGroupIntoNewRegion(destination, grp, attemptedPosition))
            {
                grp.OffsetForNewRegion(oldGroupPosition);
                grp.ScheduleGroupUpdate (PrimUpdateFlags.ForcedFullUpdate);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Move the given scene object into a new region
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="grp">Scene Object Group that we're crossing</param>
        /// <param name="attemptedPos"></param>
        /// <returns>
        /// true if the crossing itself was successful, false on failure
        /// </returns>
        protected bool CrossPrimGroupIntoNewRegion(GridRegion destination, SceneObjectGroup grp, Vector3 attemptedPos)
        {
            bool successYN = false;
            if (destination != null)
            {
                if(grp.SitTargetAvatar.Count != 0)
                {
                    bool success = false;
                    foreach (UUID avID in grp.SitTargetAvatar)
                    {
                        IScenePresence SP = grp.Scene.GetScenePresence(avID);
                        SP.Velocity = grp.RootChild.PhysActor.Velocity;
                        InternalCross(SP, attemptedPos, false, destination);
                        success = grp.Scene.GetScenePresence(avID).IsChildAgent;
                    }
                    if(success)
                    {
                        foreach(ISceneChildEntity part in grp.ChildrenEntities())
                            part.SitTargetAvatar.Clear();

                        IBackupModule backup = grp.Scene.RequestModuleInterface<IBackupModule>();
                        if(backup != null)
                            return backup.DeleteSceneObjects(new[] { grp }, false, false);
                        return true;//They do all the work adding the prim in the other region
                    }
                    return false;
                }

                SceneObjectGroup copiedGroup = (SceneObjectGroup)grp.Copy(false);
                copiedGroup.SetAbsolutePosition(true, attemptedPos);
                if (grp.Scene != null)
                    successYN = grp.Scene.RequestModuleInterface<ISimulationService>().CreateObject(destination, copiedGroup);

                if (successYN)
                {
                    // We remove the object here
                    try
                    {
                        foreach (ISceneChildEntity part in grp.ChildrenEntities())
                            part.SitTargetAvatar.Clear();

                        IBackupModule backup = grp.Scene.RequestModuleInterface<IBackupModule>();
                        if (backup != null)
                            return backup.DeleteSceneObjects(new[] { grp }, false, true);
                    }
                    catch (Exception e)
                    {
                        MainConsole.Instance.ErrorFormat(
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

                    MainConsole.Instance.ErrorFormat("[ENTITY TRANSFER MODULE]: Prim crossing failed for {0}", grp);
                }
            }
            else
            {
                MainConsole.Instance.Error("[ENTITY TRANSFER MODULE]: destination was unexpectedly null in Scene.CrossPrimGroupIntoNewRegion()");
            }

            return successYN;
        }

        #endregion

        #region Incoming Object Transfers

        /// <summary>
        /// Attachment rezzing
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="userID">Agent Unique ID</param>
        /// <param name="itemID">Inventory Item ID to rez</param>
        /// <returns>False</returns>
        public virtual bool IncomingCreateObject(UUID regionID, UUID userID, UUID itemID)
        {
            /*//MainConsole.Instance.DebugFormat(" >>> IncomingCreateObject(userID, itemID) <<< {0} {1}", userID, itemID);
            Scene scene = GetScene(regionID);
            if (scene == null)
                return false;
            ScenePresence sp = scene.GetScenePresence(userID);
            IAttachmentsModule attachMod = scene.RequestModuleInterface<IAttachmentsModule>();
            if (sp != null && attachMod != null)
            {
                MainConsole.Instance.DebugFormat(
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
        /// <param name="regionID"></param>
        /// <param name="sog"></param>
        /// <returns></returns>
        public virtual bool IncomingCreateObject(UUID regionID, ISceneObject sog)
        {
            IScene scene = GetScene(regionID);
            if (scene == null)
                return false;
            
            //MainConsole.Instance.Debug(" >>> IncomingCreateObject(sog) <<< " + ((SceneObjectGroup)sog).AbsolutePosition + " deleted? " + ((SceneObjectGroup)sog).IsDeleted);
            SceneObjectGroup newObject;
            try
            {
                newObject = (SceneObjectGroup)sog;
            }
            catch (Exception e)
            {
                MainConsole.Instance.WarnFormat("[EntityTransferModule]: Problem casting object: {0}", e.Message);
                return false;
            }

            if (!AddSceneObject(scene, newObject))
            {
                MainConsole.Instance.WarnFormat("[EntityTransferModule]: Problem adding scene object {0} in {1} ", sog.UUID, scene.RegionInfo.RegionName);
                return false;
            }

            newObject.RootPart.ParentGroup.CreateScriptInstances(0, false, StateSource.PrimCrossing, UUID.Zero, false);

            return true;
        }

        /// <summary>
        /// Adds a Scene Object group to the Scene.
        /// Verifies that the creator of the object is not banned from the simulator.
        /// Checks if the item is an Attachment
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="sceneObject"></param>
        /// <returns>True if the SceneObjectGroup was added, False if it was not</returns>
        public bool AddSceneObject(IScene scene, SceneObjectGroup sceneObject)
        {
            // If the user is banned, we won't let any of their objects
            // enter. Period.
            //
            if (scene.RegionInfo.EstateSettings.IsBanned(sceneObject.OwnerID))
            {
                MainConsole.Instance.Info("[EntityTransferModule]: Denied prim crossing for banned avatar");

                return false;
            }

            if (!sceneObject.IsAttachmentCheckFull()) // Not Attachment
            {
                if (!scene.Permissions.CanObjectEntry(sceneObject.UUID,
                        true, sceneObject.AbsolutePosition, sceneObject.OwnerID))
                {
                    // Deny non attachments based on parcel settings
                    //
                    MainConsole.Instance.Info("[EntityTransferModule]: Denied prim crossing " +
                            "because of parcel settings");

                    IBackupModule backup = scene.RequestModuleInterface<IBackupModule>();
                    if (backup != null)
                        backup.DeleteSceneObjects(new[] { sceneObject }, true, true);

                    return false;
                }
                if (scene.SceneGraph.AddPrimToScene(sceneObject))
                {
                    if(sceneObject.RootPart.IsSelected)
                        sceneObject.RootPart.CreateSelected = true;
                    sceneObject.ScheduleGroupUpdate (PrimUpdateFlags.ForcedFullUpdate);
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
        public IScene GetScene(UUID RegionID)
        {
#if (!ISWIN)
            foreach (IScene scene in m_scenes)
            {
                if (scene.RegionInfo.RegionID == RegionID) return scene;
            }
            return null;
#else
            return m_scenes.FirstOrDefault(scene => scene.RegionInfo.RegionID == RegionID);
#endif
        }

        #endregion

        #region RegionComms

        /// <summary>
        /// Do the work necessary to initiate a new user connection for a particular scene.
        /// At the moment, this consists of setting up the caps infrastructure
        /// The return bool should allow for connections to be refused, but as not all calling paths
        /// take proper notice of it let, we allowed banned users in still.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="agent">CircuitData of the agent who is connecting</param>
        /// <param name="UDPPort"></param>
        /// <param name="reason">Outputs the reason for the false response on this string,
        /// If the agent was accepted, this will be the Caps SEED for the region</param>
        /// <param name="teleportFlags"></param>
        /// <returns>True if the region accepts this agent.  False if it does not.  False will 
        /// also return a reason.</returns>
        public bool NewUserConnection (IScene scene, AgentCircuitData agent, uint teleportFlags, out int UDPPort, out string reason)
        {
            reason = String.Empty;
            UDPPort = GetUDPPort(scene);
            IScenePresence sp = scene.GetScenePresence(agent.AgentID);

            // Don't disable this log message - it's too helpful
            MainConsole.Instance.TraceFormat (
                "[ConnectionBegin]: Region {0} told of incoming {1} agent {2} (circuit code {3}, teleportflags {4})",
                scene.RegionInfo.RegionName, (agent.child ? "child" : "root"), agent.AgentID,
                agent.circuitcode, teleportFlags);

            if (!AuthorizeUser (scene, agent, out reason))
            {
                OSDMap map = new OSDMap ();
                map["Reason"] = reason;
                map["success"] = false;
                reason = OSDParser.SerializeJsonString (map);
                return false;
            }

            CacheUserInfo(scene, agent.OtherInformation);

            if (sp != null && !sp.IsChildAgent)
            {
                // We have a zombie from a crashed session. 
                // Or the same user is trying to be root twice here, won't work.
                // Kill it.
                MainConsole.Instance.InfoFormat ("[Scene]: Zombie scene presence detected for {0} in {1}", agent.AgentID, scene.RegionInfo.RegionName);
                //Tell everyone about it
                scene.AuroraEventManager.FireGenericEventHandler ("AgentIsAZombie", sp.UUID);
                //Send the killing message (DisableSimulator)
                scene.RemoveAgent (sp, true);
                sp = null;
            }

            OSDMap responseMap = new OSDMap ();
            responseMap["CapsUrls"] = scene.EventManager.TriggerOnRegisterCaps (agent.AgentID);
            responseMap["OurIPForClient"] = MainServer.Instance.HostName;

            // In all cases, add or update the circuit data with the new agent circuit data and teleport flags
            agent.teleportFlags = teleportFlags;

            responseMap["Agent"] = agent.PackAgentCircuitData ();

            object[] obj = new object[2];
            obj[0] = responseMap;
            obj[1] = agent;
            scene.AuroraEventManager.FireGenericEventHandler ("NewUserConnection", obj);

            //Add the circuit at the end
            scene.AuthenticateHandler.AddNewCircuit (agent.circuitcode, agent);

            MainConsole.Instance.InfoFormat (
                "[ConnectionBegin]: Region {0} authenticated and authorized incoming {1} agent {2} (circuit code {3})",
                scene.RegionInfo.RegionName, (agent.child ? "child" : "root"), agent.AgentID,
                agent.circuitcode);

            responseMap["success"] = true;
            reason = OSDParser.SerializeJsonString (responseMap);
            return true;
        }

        private void CacheUserInfo(IScene scene, OSDMap map)
        {
			if(map.ContainsKey("CachedUserInfo"))
			{
				//OpenSim does not contain this, only check if it is there
				// We do this caching so that we don't pull down all of this info as the user is trying to login, which causes major lag, and slows down the sim for people already in the sim
	            CachedUserInfo cache = new CachedUserInfo();
	            cache.FromOSD((OSDMap)map["CachedUserInfo"]);
	            IAgentConnector conn = Aurora.DataManager.DataManager.RequestPlugin<IAgentConnector>();
	            if (conn != null)
	                conn.CacheAgent(cache.AgentInfo);
	            scene.UserAccountService.CacheAccount(cache.UserAccount);
	
	            IGroupsModule groupsMod = scene.RequestModuleInterface<IGroupsModule>();
	            if (groupsMod != null)
	                groupsMod.UpdateCachedData(cache.UserAccount.PrincipalID, cache);
			}
        }

        private readonly Dictionary<IScene, int> m_lastUsedPort = new Dictionary<IScene, int> ();
        /// <summary>
        /// This method provides ports for the region to accept requests from clients on as given by the region info
        ///   and goes through them one by one
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        private int GetUDPPort (IScene scene)
        {
            if (scene.RegionInfo.UDPPorts.Count == 0)
                return scene.RegionInfo.InternalEndPoint.Port;
            lock (m_lastUsedPort)
            {
                if (!m_lastUsedPort.ContainsKey (scene))
                    m_lastUsedPort.Add (scene, 0);
                int port = scene.RegionInfo.UDPPorts[m_lastUsedPort[scene]];
                m_lastUsedPort[scene]++;
                if (m_lastUsedPort[scene] == scene.RegionInfo.UDPPorts.Count)
                    m_lastUsedPort[scene] = 0;
                return port;
            }
        }

        /// <summary>
        /// Verify if the user can connect to this region.  Checks the banlist and ensures that the region is set for public access
        /// </summary>
        /// <param name="scene"></param>
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
                    MainConsole.Instance.WarnFormat ("[ConnectionBegin]: Denied access to {0} at {1} because the user does not have access to the region, reason: {2}",
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
        /// <param name="scene"></param>
        /// <param name="cAgentData">Agent that contains all of the relevant things about an agent.
        /// Appearance, animations, position, etc.</param>
        /// <returns>true if we handled it.</returns>
        public virtual bool IncomingChildAgentDataUpdate (IScene scene, AgentData cAgentData)
        {
            MainConsole.Instance.DebugFormat(
                "[SCENE]: Incoming child agent update for {0} in {1}", cAgentData.AgentID, scene.RegionInfo.RegionName);

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
                    return false;//The agent doesn't exist
                }
            return true;
        }

        /// <summary>
        /// We've got an update about an agent that sees into this region, 
        /// send it to ScenePresence for processing  It's only positional data
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="cAgentData">AgentPosition that contains agent positional data so we can know what to send</param>
        /// <returns>true if we handled it.</returns>
        public virtual bool IncomingChildAgentDataUpdate (IScene scene, AgentPosition cAgentData)
        {
            //MainConsole.Instance.Debug(" XXX Scene IncomingChildAgentDataUpdate POSITION in " + RegionInfo.RegionName);
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

        public virtual bool IncomingRetrieveRootAgent(IScene scene, UUID id, bool agentIsLeaving, out AgentData agent, out AgentCircuitData circuitData)
        {
            agent = null;
            circuitData = null;
            IScenePresence sp = scene.GetScenePresence (id);
            if ((sp != null) && (!sp.IsChildAgent))
            {
                AgentData data = new AgentData ();
                sp.CopyTo (data);
                agent = data;
                circuitData = BuildCircuitDataForPresence(sp, sp.AbsolutePosition);
                //if (agentIsLeaving)
                //    sp.SetAgentLeaving(null);//We arn't sure where they are going
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tell a single agent to disconnect from the region.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="agentID"></param>
        public bool IncomingCloseAgent (IScene scene, UUID agentID)
        {
            //MainConsole.Instance.DebugFormat("[SCENE]: Processing incoming close agent for {0}", agentID);

            IScenePresence presence = scene.GetScenePresence (agentID);
            if (presence != null)
            {
                bool RetVal = scene.RemoveAgent (presence, true);

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
