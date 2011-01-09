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
using log4net;
using Nini.Config;

namespace OpenSim.Region.CoreModules.Framework.EntityTransfer
{
    public class EntityTransferModule : ISharedRegionModule, IEntityTransferModule
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected bool m_Enabled = false;
        protected List<Scene> m_scenes = new List<Scene>();
        protected List<UUID> m_agentsInTransit;
        protected List<UUID> m_cancelingAgents;

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
                    m_agentsInTransit = new List<UUID>();
                    m_cancelingAgents = new List<UUID>();
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
            scene.EventManager.OnClosingClient += OnClosingClient;
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
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public virtual void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;

        }


        #endregion

        #region Agent Teleports

        public virtual void Teleport(ScenePresence sp, ulong regionHandle, Vector3 position, Vector3 lookAt, uint teleportFlags)
        {
            string reason = "";
            if (!sp.Scene.Permissions.CanTeleport(sp.UUID, position, sp.Scene.AuthenticateHandler.GetAgentCircuitData(sp.UUID), out position, out reason))
            {
                sp.ControllingClient.SendTeleportFailed(reason);
                return;
            }

            sp.ControllingClient.SendTeleportStart(teleportFlags);
            sp.ControllingClient.SendTeleportProgress(teleportFlags, "requesting");
            //sp.ControllingClient.SendTeleportProgress("resolving");

            // Reset animations; the viewer does that in teleports.
            if(sp.Animator != null)
                sp.Animator.ResetAnimations();

            try
            {
                uint x = 0, y = 0;
                Utils.LongToUInts(regionHandle, out x, out y);

                GridRegion reg = sp.Scene.GridService.GetRegionByPosition(sp.Scene.RegionInfo.ScopeID, (int)x, (int)y);

                long XShift = (reg.RegionLocX - sp.Scene.RegionInfo.RegionLocX);
                long YShift = (reg.RegionLocY - sp.Scene.RegionInfo.RegionLocY);
                if (regionHandle == sp.Scene.RegionInfo.RegionHandle || //Take region size into account as well
                    (XShift < sp.Scene.RegionInfo.RegionSizeX && YShift < sp.Scene.RegionInfo.RegionSizeY &&
                    XShift > 0 && YShift > 0 && //Can't have negatively sized regions
                    sp.Scene.RegionInfo.RegionSizeX != float.PositiveInfinity && sp.Scene.RegionInfo.RegionSizeY != float.PositiveInfinity))
                {
                    // m_log.DebugFormat(
                    //    "[ENTITY TRANSFER MODULE]: RequestTeleportToLocation {0} within {1}",
                    //    position, sp.Scene.RegionInfo.RegionName);

                    //We have to add the shift as it is brought into this as well in regions that have larger RegionSizes
                    position.X += XShift;
                    position.Y += YShift;

                    // Teleport within the same region
                    ITerrainChannel channel = sp.Scene.RequestModuleInterface<ITerrainChannel>();
                    float groundHeight = channel.GetNormalizedGroundHeight(position.X, position.Y);
                    if (position.X < 0f || position.Y < 0f || position.Z < groundHeight ||
                        position.X > sp.Scene.RegionInfo.RegionSizeX || position.Y > sp.Scene.RegionInfo.RegionSizeY)
                    {
                        m_log.WarnFormat(
                            "[EntityTransferModule]: Given an illegal position of {0} for avatar {1}. Clamping",
                            position, sp.Name);

                        if (position.X < 0f) position.X = 0f;
                        if (position.Y < 0f) position.Y = 0f;

                        if (position.X > sp.Scene.RegionInfo.RegionSizeX) position.X = sp.Scene.RegionInfo.RegionSizeX;
                        if (position.Y > sp.Scene.RegionInfo.RegionSizeY) position.Y = sp.Scene.RegionInfo.RegionSizeY;

                        //Keep users from being underground
                        // Not good to get it twice, but we need to, because the X,Y might have changed
                        groundHeight = channel.GetNormalizedGroundHeight(position.X, position.Y);
                        if (position.Z < groundHeight)
                        {
                            position.Z = groundHeight;
                        }
                    }

                    sp.ControllingClient.SendTeleportStart(teleportFlags);

                    sp.ControllingClient.SendLocalTeleport(position, lookAt, teleportFlags);
                    sp.Teleport(position);
                }
                else // Another region possibly in another simulator
                {
                    if (reg != null)
                    {
                        GridRegion finalDestination = GetFinalDestination(reg);
                        if (finalDestination == null)
                        {
                            m_log.DebugFormat("[ENTITY TRANSFER MODULE]: Final destination is having problems. Unable to teleport agent.");
                            sp.ControllingClient.SendTeleportFailed("Problem at destination");
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
                        DoTeleport(sp, reg, finalDestination, position, lookAt, teleportFlags);
                        //
                        //
                        //
                    }
                    else
                    {
                        // TP to a place that doesn't exist (anymore)
                        // Inform the viewer about that
                        sp.ControllingClient.SendTeleportFailed("The region you tried to teleport to doesn't exist anymore");

                        // and set the map-tile to '(Offline)'
                        uint regX, regY;
                        Utils.LongToUInts(regionHandle, out regX, out regY);

                        MapBlockData block = new MapBlockData();
                        block.X = (ushort)(regX / Constants.RegionSize);
                        block.Y = (ushort)(regY / Constants.RegionSize);
                        block.Access = 254; // == not there

                        List<MapBlockData> blocks = new List<MapBlockData>();
                        blocks.Add(block);
                        sp.ControllingClient.SendMapBlock(blocks, 0);
                    }
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[ENTITY TRANSFER MODULE]: Exception on teleport: {0}\n{1}", e.Message, e.StackTrace);
                sp.ControllingClient.SendTeleportFailed("Internal error");
            }
        }

        public virtual void DoTeleport(ScenePresence sp, GridRegion reg, GridRegion finalDestination, Vector3 position, Vector3 lookAt, uint teleportFlags)
        {
            sp.ControllingClient.SendTeleportProgress(teleportFlags, "sending_dest");
            if (reg == null || finalDestination == null)
            {
                sp.ControllingClient.SendTeleportFailed("Unable to locate destination");
                return;
            }

            m_log.DebugFormat(
                "[ENTITY TRANSFER MODULE]: Request Teleport to {0}:{1}:{2}/{3}",
                reg.ExternalHostName, reg.HttpPort, finalDestination.RegionName, position);

            int newRegionX = (int)(reg.RegionHandle >> 40) * Constants.RegionSize;
            int newRegionY = (((int)(reg.RegionHandle)) >> 8) * Constants.RegionSize;
            int oldRegionX = (int)(sp.Scene.RegionInfo.RegionHandle >> 40) * Constants.RegionSize;
            int oldRegionY = (((int)(sp.Scene.RegionInfo.RegionHandle)) >> 8) * Constants.RegionSize;

            ulong destinationHandle = finalDestination.RegionHandle;

            // Let's do DNS resolution only once in this process, please!
            // This may be a costly operation. The reg.ExternalEndPoint field is not a passive field,
            // it's actually doing a lot of work.
            IPEndPoint endPoint = finalDestination.ExternalEndPoint;
            if (endPoint.Address != null)
            {
                sp.ControllingClient.SendTeleportProgress(teleportFlags, "arriving");

                if (CheckForCancelingAgent(sp.UUID))
                {
                    Cancel(sp);
                    return;
                }
                
                // Fixing a bug where teleporting while sitting results in the avatar ending up removed from
                // both regions
                if (sp.ParentID != UUID.Zero)
                    sp.StandUp(true);

                if (!sp.ValidateAttachments())
                {
                    sp.ControllingClient.SendTeleportProgress(teleportFlags, "missing_attach_tport");
                    sp.ControllingClient.SendTeleportFailed("Inconsistent attachment state");
                    return;
                }

                AgentCircuitData agentCircuit = sp.ControllingClient.RequestClientInfo();
                agentCircuit.startpos = position;
                agentCircuit.child = false;
                agentCircuit.Appearance = sp.Appearance;

                IEventQueueService eq = sp.Scene.RequestModuleInterface<IEventQueueService>();

                AgentData agent = new AgentData();
                sp.CopyTo(agent);
                agent.Position = position;
                SetCallbackURL(agent, sp.Scene.RegionInfo);
                
                if (eq != null)
                {
                    //This does CreateAgent and sends the EnableSimulator/EstablishAgentCommunication 
                    //  messages if they need to be called
                    if(!eq.TryEnableChildAgents(sp.UUID, sp.Scene.RegionInfo.RegionHandle, (int)sp.DrawDistance,
                        finalDestination, agentCircuit, agent, teleportFlags, endPoint.Address.GetAddressBytes(), endPoint.Port))
                    {
                        sp.ControllingClient.SendTeleportFailed("Destination refused");
                        return;
                    }
                }

                if (CheckForCancelingAgent(sp.UUID))
                {
                    Cancel(sp);
                    return;
                }

                //Set the agent in transit before we send the event
                SetInTransit(sp.UUID);

                if (eq != null)
                {
                    eq.TeleportFinishEvent(destinationHandle, 13, endPoint,
                                           4, teleportFlags, sp.UUID, teleportFlags, sp.Scene.RegionInfo.RegionHandle);
                }

                // Let's set this to true tentatively. This does not trigger OnChildAgent
                sp.IsChildAgent = true;

                // TeleportFinish makes the client send CompleteMovementIntoRegion (at the destination), which
                // trigers a whole shebang of things there, including MakeRoot. So let's wait for confirmation
                // that the client contacted the destination before we send the attachments and close things here.

                bool callWasCanceled = false;
                if (!WaitForCallback(sp.UUID, out callWasCanceled))
                {
                    if (!callWasCanceled)
                    {
                        m_log.Warn("[EntityTransferModule]: Callback never came for teleporting agent " + sp.Name + ". Resetting.");
                        Fail(sp, finalDestination);
                    }
                    else
                        Cancel(sp);
                    return;
                }

                // OK, it got this agent. Let's close some child agents
                INeighborService neighborService = sp.Scene.RequestModuleInterface<INeighborService>();
                if (neighborService != null)
                    neighborService.CloseNeighborAgents(newRegionX, newRegionY, sp.UUID, sp.Scene.RegionInfo.RegionID);

                // CrossAttachmentsIntoNewRegion is a synchronous call. We shouldn't need to wait after it
                CrossAttachmentsIntoNewRegion(finalDestination, sp);

                // Well, this is it. The agent is over there.

                KillEntity(sp.Scene, sp);

                // May need to logout or other cleanup
                AgentHasMovedAway(sp.ControllingClient.SessionId, false);

                // Now let's make it officially a child agent
                sp.MakeChildAgent();

                sp.Scene.CleanDroppedAttachments();

                // Finally, let's close this previously-known-as-root agent, when the jump is outside the view zone

                if (NeedsClosing(oldRegionX, newRegionX, oldRegionY, newRegionY, reg))
                {
                    Thread.Sleep(5000);
                    sp.Close();
                    sp.Scene.IncomingCloseAgent(sp.UUID);
                }
                else
                    // now we have a child agent in this region. 
                    sp.Reset();
            }
            else
            {
                sp.ControllingClient.SendTeleportFailed("Remote Region appears to be down");
            }
        }

        private void Cancel(ScenePresence sp)
        {
            // Client never contacted destination. Let's restore everything back
            sp.ControllingClient.SendTeleportFailed("You canceled the tp.");

            // Fail. Reset it back
            sp.IsChildAgent = false;

            ResetFromTransit(sp.UUID);

            EnableChildAgents(sp);
        }

        private void Fail(ScenePresence sp, GridRegion finalDestination)
        {
            // Client never contacted destination. Let's restore everything back
            sp.ControllingClient.SendTeleportFailed("Problems connecting to destination.");

            // Fail. Reset it back
            sp.IsChildAgent = false;

            ResetFromTransit(sp.UUID);

            EnableChildAgents(sp);

            // Finally, kill the agent we just created at the destination.
            sp.Scene.SimulationService.CloseAgent(finalDestination, sp.UUID);
        }

        protected virtual void SetCallbackURL(AgentData agent, RegionInfo region)
        {
            agent.CallbackURI = "http://" + region.ExternalHostName + ":" + region.HttpPort +
                "/agent/" + agent.AgentID.ToString() + "/" + region.RegionID.ToString() + "/release/";

        }

        protected virtual void AgentHasMovedAway(UUID sessionID, bool logout)
        {
        }

        protected void KillEntity(Scene scene, ISceneEntity entity)
        {
            scene.ForEachClient(delegate(IClientAPI client)
            {
                client.SendKillObject(scene.RegionInfo.RegionHandle, new ISceneEntity[] { entity });
            });
        }

        protected void KillEntities(Scene scene, ISceneEntity[] grp)
        {
            scene.ForEachClient(delegate(IClientAPI client)
            {
                client.SendKillObject(scene.RegionInfo.RegionHandle, grp);
            });
        }

        protected virtual GridRegion GetFinalDestination(GridRegion region)
        {
            return region;
        }

        protected virtual bool NeedsClosing(int oldRegionX, int newRegionX, int oldRegionY, int newRegionY, GridRegion reg)
        {
            INeighborService neighborService = m_scenes[0].RequestModuleInterface<INeighborService>();
            if (neighborService != null)
                return neighborService.IsOutsideView(oldRegionX, newRegionX, oldRegionY, newRegionY);
            return false;
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
            CancelTeleport(client.AgentId);
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

            RequestTeleportLocation(remoteClient, regionInfo.RegionHandle, position, lookat, teleportFlags);
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
            ScenePresence sp = scene.GetScenePresence(remoteClient.AgentId);
            if (sp != null)
            {
                Teleport(sp, regionHandle, position, lookAt, teleportFlags);
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
            GridRegion info = remoteClient.Scene.RequestModuleInterface<IGridService>().GetRegionByUUID(UUID.Zero, regionID);

            if (info == null)
            {
                // can't find the region: Tell viewer and abort
                remoteClient.SendTeleportFailed("The teleport destination could not be found.");
                return;
            }

            RequestTeleportLocation(remoteClient, info.RegionHandle, position, Vector3.Zero, (uint)(TeleportFlags.SetLastToTarget | TeleportFlags.ViaLandmark));
        }

        #endregion

        #region Teleport Home

        public virtual void TeleportHome(UUID id, IClientAPI client)
        {
            //m_log.DebugFormat("[ENTITY TRANSFER MODULE]: Request to teleport {0} {1} home", client.FirstName, client.LastName);

            //OpenSim.Services.Interfaces.PresenceInfo pinfo = m_aScene.PresenceService.GetAgent(client.SessionId);
            GridUserInfo uinfo = GetScene(client.Scene.RegionInfo.RegionID).GridUserService.GetGridUserInfo(client.AgentId.ToString());

            if (uinfo != null)
            {
                GridRegion regionInfo = GetScene(client.Scene.RegionInfo.RegionID).GridService.GetRegionByUUID(UUID.Zero, uinfo.HomeRegionID);
                if (regionInfo == null)
                {
                    // can't find the Home region: Tell viewer and abort
                    client.SendTeleportFailed("Your home region could not be found.");
                    return;
                }
                m_log.DebugFormat("[ENTITY TRANSFER MODULE]: User's home region is {0} {1} ({2}-{3})",
                    regionInfo.RegionName, regionInfo.RegionID, regionInfo.RegionLocX / Constants.RegionSize, regionInfo.RegionLocY / Constants.RegionSize);

                RequestTeleportLocation(
                    client, regionInfo.RegionHandle, uinfo.HomePosition, uinfo.HomeLookAt,
                    (uint)(Constants.TeleportFlags.SetLastToTarget | Constants.TeleportFlags.ViaHome));
            }
            else
            {
                //Default region time...
                List<GridRegion> Regions = GetScene(client.Scene.RegionInfo.RegionID).GridService.GetDefaultRegions(UUID.Zero);
                if (Regions.Count != 0)
                {
                    m_log.DebugFormat("[ENTITY TRANSFER MODULE]: User's home region was not found, using {0} {1} ({2}-{3})",
                        Regions[0].RegionName, Regions[0].RegionID, Regions[0].RegionLocX / Constants.RegionSize, Regions[0].RegionLocY / Constants.RegionSize);

                    RequestTeleportLocation(
                        client, Regions[0].RegionHandle, new Vector3(128, 128, 25), new Vector3(128, 128, 128),
                        (uint)(Constants.TeleportFlags.SetLastToTarget | Constants.TeleportFlags.ViaHome));
                }
            }
        }

        #endregion

        #region Agent Crossings

        public delegate void InformClientToInitateTeleportToLocationDelegate(ScenePresence agent, uint regionX, uint regionY,
                                                            Vector3 position,
                                                            Scene initiatingScene);

        protected void InformClientToInitateTeleportToLocation(ScenePresence agent, uint regionX, uint regionY, Vector3 position, Scene initiatingScene)
        {

            // This assumes that we know what our neighbors are.

            InformClientToInitateTeleportToLocationDelegate d = InformClientToInitiateTeleportToLocationAsync;
            d.BeginInvoke(agent, regionX, regionY, position, initiatingScene,
                          InformClientToInitiateTeleportToLocationCompleted,
                          d);
        }

        public void InformClientToInitiateTeleportToLocationAsync(ScenePresence agent, uint regionX, uint regionY, Vector3 position,
            Scene initiatingScene)
        {
            Thread.Sleep(10000);
            IMessageTransferModule im = initiatingScene.RequestModuleInterface<IMessageTransferModule>();
            if (im != null)
            {
                UUID gotoLocation = Util.BuildFakeParcelID(
                    Util.UIntsToLong(
                                              (regionX *
                                               (uint)Constants.RegionSize),
                                              (regionY *
                                               (uint)Constants.RegionSize)),
                    (uint)(int)position.X,
                    (uint)(int)position.Y,
                    (uint)(int)position.Z);
                GridInstantMessage m = new GridInstantMessage(initiatingScene, UUID.Zero,
                "Region", agent.UUID,
                (byte)InstantMessageDialog.GodLikeRequestTeleport, false,
                "", gotoLocation, false, new Vector3(127, 0, 0),
                new Byte[0]);
                im.SendInstantMessage(m, delegate(bool success)
                {
                    //m_log.DebugFormat("[ENTITY TRANSFER MODULE]: Client Initiating Teleport sending IM success = {0}", success);
                });

            }
        }

        protected void InformClientToInitiateTeleportToLocationCompleted(IAsyncResult iar)
        {
            InformClientToInitateTeleportToLocationDelegate icon =
                (InformClientToInitateTeleportToLocationDelegate)iar.AsyncState;
            icon.EndInvoke(iar);
        }

        public virtual void Cross(ScenePresence agent, bool isFlying, GridRegion crossingRegion)
        {
            Scene scene = agent.Scene;
            //Add the offset as its needed
            Vector3 newposition = new Vector3(agent.AbsolutePosition.X, agent.AbsolutePosition.Y, agent.AbsolutePosition.Z);
            newposition.X += scene.RegionInfo.RegionLocX - crossingRegion.RegionLocX;
            newposition.Y += scene.RegionInfo.RegionLocY - crossingRegion.RegionLocY;

            CrossAgentToNewRegionDelegate d = CrossAgentToNewRegionAsync;
            d.BeginInvoke(agent, newposition, crossingRegion, isFlying, CrossAgentToNewRegionCompleted, d);
        }

        public delegate ScenePresence CrossAgentToNewRegionDelegate(ScenePresence agent, Vector3 pos, GridRegion crossingRegion, bool isFlying);

        /// <summary>
        /// This Closes child agents on neighboring regions
        /// Calls an asynchronous method to do so..  so it doesn't lag the sim.
        /// </summary>
        protected ScenePresence CrossAgentToNewRegionAsync(ScenePresence agent, Vector3 pos, GridRegion crossingRegion, bool isFlying)
        {
            m_log.DebugFormat("[EntityTransferModule]: Crossing agent {0} {1} to region {2}", agent.Firstname, agent.Lastname, crossingRegion.RegionName);

            Scene m_scene = agent.Scene;

            if (crossingRegion != null && agent.ValidateAttachments())
            {
                pos = pos + (agent.Velocity);

                SetInTransit(agent.UUID);
                AgentData cAgent = new AgentData();
                agent.CopyTo(cAgent);
                cAgent.Position = pos;
                if (isFlying)
                    cAgent.ControlFlags |= (uint)AgentManager.ControlFlags.AGENT_CONTROL_FLY;
                SetCallbackURL(cAgent, m_scene.RegionInfo);
                
                if (!m_scene.SimulationService.UpdateAgent(crossingRegion, cAgent))
                {
                    // region doesn't take it
                    ResetFromTransit(agent.UUID);
                    return agent;
                }

                IEventQueueService eq = agent.Scene.RequestModuleInterface<IEventQueueService>();
                if (eq != null)
                {
                    eq.CrossRegion(crossingRegion.RegionHandle, pos, agent.Velocity, crossingRegion.ExternalEndPoint,
                                   agent.UUID, agent.ControllingClient.SessionId, agent.Scene.RegionInfo.RegionHandle);
                }

                bool callWasCanceled = false;
                if (!WaitForCallback(agent.UUID, callWasCanceled))
                {
                    m_log.Warn("[EntityTransferModule]: Callback never came in crossing agent " + agent.Name + ". Resetting.");
                    ResetFromTransit(agent.UUID);

                    // Yikes! We should just have a ref to scene here.
                    //agent.Scene.InformClientOfNeighbours(agent);
                    EnableChildAgents(agent);

                    return agent;
                }

                // Next, let's close the child agent connections that are too far away.
                INeighborService neighborService = agent.Scene.RequestModuleInterface<INeighborService>();
                if (neighborService != null)
                    neighborService.CloseNeighborAgents(crossingRegion.RegionLocX, crossingRegion.RegionLocY, agent.UUID, agent.Scene.RegionInfo.RegionID);

                agent.MakeChildAgent();
                // now we have a child agent in this region. Request and send all interesting data about (root) agents in the sim
                agent.SendOtherAgentsAvatarDataToMe();
                agent.SendOtherAgentsAppearanceToMe();

                CrossAttachmentsIntoNewRegion(crossingRegion, agent);
            }
            return agent;
        }

        private void KillAttachments(ScenePresence agent)
        {
            foreach (SceneObjectGroup grp in agent.Attachments)
            {
                //Kill in all clients as it will be readded in the other region
                KillEntities(agent.Scene, grp.ChildrenEntities().ToArray());
                //Now remove it from the Scene so that it will not come back
                agent.Scene.SceneGraph.DeleteEntity(grp);
                //And from storage as well
                IBackupModule backup = agent.Scene.RequestModuleInterface<IBackupModule>();
                if(backup != null)
                    backup.DeleteFromStorage(grp.UUID);
            }
        }

        /// <summary>
        /// This Closes child agents on neighboring regions
        /// Calls an asynchronous method to do so..  so it doesn't lag the sim.
        /// </summary>
        protected ScenePresence CrossAgentSittingToNewRegionAsync(ScenePresence agent, GridRegion neighbourRegion, SceneObjectGroup grp)
        {
            Scene m_scene = agent.Scene;
            
            if (agent.ValidateAttachments())
            {
                AgentData cAgent = new AgentData();
                agent.CopyTo(cAgent);
                cAgent.Position = grp.AbsolutePosition;
                    

                cAgent.CallbackURI = "http://" + m_scene.RegionInfo.ExternalHostName + ":" + m_scene.RegionInfo.HttpPort +
                    "/agent/" + agent.UUID.ToString() + "/" + m_scene.RegionInfo.RegionID.ToString() + "/release/";

                if (!m_scene.SimulationService.UpdateAgent(neighbourRegion, cAgent))
                {
                    // region doesn't take it
                    ResetFromTransit(agent.UUID);
                    return agent;
                }

                // Next, let's close the child agent connections that are too far away.
                INeighborService neighborService = agent.Scene.RequestModuleInterface<INeighborService>();
                if (neighborService != null)
                    neighborService.CloseNeighborAgents(neighbourRegion.RegionLocX, neighbourRegion.RegionLocY, agent.UUID, agent.Scene.RegionInfo.RegionID);

                IEventQueueService eq = agent.Scene.RequestModuleInterface<IEventQueueService>();
                if (eq != null)
                {
                    eq.CrossRegion(neighbourRegion.RegionHandle, agent.AbsolutePosition, agent.Velocity, neighbourRegion.ExternalEndPoint,
                                   agent.UUID, agent.ControllingClient.SessionId, agent.Scene.RegionInfo.RegionHandle);
                }

                agent.MakeChildAgent();
                // now we have a child agent in this region. Request all interesting data about other (root) agents
                agent.SendOtherAgentsAvatarDataToMe();
                agent.SendOtherAgentsAppearanceToMe();

                CrossAttachmentsIntoNewRegion(neighbourRegion, agent);
            }
            return agent;
        }

        protected void CrossAgentToNewRegionCompleted(IAsyncResult iar)
        {
            CrossAgentToNewRegionDelegate icon = (CrossAgentToNewRegionDelegate)iar.AsyncState;
            ScenePresence agent = icon.EndInvoke(iar);

            // If the cross was successful, this agent is a child agent
            if (agent.IsChildAgent)
                agent.Reset();
            else // Not successful
                agent.RestoreInCurrentScene();

            // In any case
            agent.NotInTransit();

            //m_log.DebugFormat("[ENTITY TRANSFER MODULE]: Crossing agent {0} {1} completed.", agent.Firstname, agent.Lastname);
        }

        #endregion

        #region Enable Child Agent

        /// <summary>
        /// This informs a single neighboring region about agent "avatar".
        /// Calls an asynchronous method to do so..  so it doesn't lag the sim.
        /// </summary>
        public void EnableChildAgent(ScenePresence sp, GridRegion region)
        {
            m_log.DebugFormat("[ENTITY TRANSFER]: Enabling child agent in new neighour {0}", region.RegionName);

            AgentCircuitData agent = sp.ControllingClient.RequestClientInfo();
            agent.startpos = new Vector3(128, 128, 70);
            agent.child = true;
            agent.Appearance = sp.Appearance;

            IEventQueueService eq = sp.Scene.RequestModuleInterface<IEventQueueService>();
            if (eq != null)
            {
                eq.EnableChildAgentsReply(agent.AgentID, sp.Scene.RegionInfo.RegionHandle,
                    (int)sp.DrawDistance, new GridRegion[1] { region }, agent, null,
                    (uint)TeleportFlags.Default);
                return;
            }
        }
        #endregion

        #region Enable Child Agents

        /// <summary>
        /// This informs all neighboring regions about agent "avatar".
        /// Calls an asynchronous method to do so..  so it doesn't lag the sim.
        /// </summary>
        public virtual void EnableChildAgents(ScenePresence sp)
        {
            List<GridRegion> neighbors = new List<GridRegion>();
            RegionInfo m_regionInfo = sp.Scene.RegionInfo;

            if (Util.VariableRegionSight && sp.DrawDistance != 0)
            {
                int xMin = (int)(m_regionInfo.RegionLocX) - (int)(sp.DrawDistance * Constants.RegionSize);
                int xMax = (int)(m_regionInfo.RegionLocX) + (int)(sp.DrawDistance * Constants.RegionSize);
                int yMin = (int)(m_regionInfo.RegionLocX) - (int)(sp.DrawDistance * Constants.RegionSize);
                int yMax = (int)(m_regionInfo.RegionLocX) + (int)(sp.DrawDistance * Constants.RegionSize);

                neighbors = sp.Scene.GridService.GetRegionRange(m_regionInfo.ScopeID,
                    xMin, xMax, yMin, yMax);
            }
            else
            {
                INeighborService service = sp.Scene.RequestModuleInterface<INeighborService>();
                if (service != null)
                {
                    neighbors = service.GetNeighbors(sp.Scene.RegionInfo);
                }
            }
            
            AgentCircuitData agent = sp.ControllingClient.RequestClientInfo();
            agent.startpos = new Vector3(128, 128, 70);
            agent.child = true;
            agent.Appearance = sp.Appearance;

            IEventQueueService eq = sp.Scene.RequestModuleInterface<IEventQueueService>();
            if (eq != null)
            {
                eq.EnableChildAgentsReply(agent.AgentID, sp.Scene.RegionInfo.RegionHandle,
                    (int)sp.DrawDistance, neighbors.ToArray(), agent, null, (uint)TeleportFlags.Default);
                return;
            }
        }

        private List<ulong> NewNeighbours(List<ulong> currentNeighbours, List<ulong> previousNeighbours)
        {
            return currentNeighbours.FindAll(delegate(ulong handle) { return !previousNeighbours.Contains(handle); });
        }

        private List<ulong> OldNeighbours(List<ulong> currentNeighbours, List<ulong> previousNeighbours)
        {
            return previousNeighbours.FindAll(delegate(ulong handle) { return !currentNeighbours.Contains(handle); });
        }

        private List<ulong> NeighbourHandles(Scene currentScene, List<GridRegion> neighbours)
        {
            List<ulong> handles = new List<ulong>();
            foreach (GridRegion reg in neighbours)
            {
                int x = currentScene.RegionInfo.RegionLocX - reg.RegionLocX;
                int y = currentScene.RegionInfo.RegionLocY - reg.RegionLocY;
                if ((x < 2 && x > -2) && (x < 2 && x > -2)) //We only check in the nearby regions for now
                {
                    //Offset for the array so that it fixes
                    x += 2;
                    y += 2;
                    if (!currentScene.DirectionsToBlockChildAgents[x, y]) //Not blocked, so false
                        handles.Add(reg.RegionHandle);
                }
                else
                    handles.Add(reg.RegionHandle);
            }
            return handles;
        }

        #endregion

        #region Agent Arrived

        public void AgentArrivedAtDestination(UUID id)
        {
            //m_log.Debug(" >>> ReleaseAgent called <<< ");
            ResetFromTransit(id);
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
        public void CrossGroupToNewRegion(SceneObjectGroup grp, Vector3 attemptedPosition, GridRegion destination)
        {
            if (grp == null)
                return;
            if (grp.IsDeleted)
                return;

            if (grp.Scene == null)
                return;
            if (grp.RootPart.DIE_AT_EDGE)
            {
                // We remove the object here
                try
                {
                    grp.Scene.DeleteSceneObject(grp, true);
                }
                catch (Exception)
                {
                    m_log.Warn("[DATABASE]: exception when trying to remove the prim that crossed the border.");
                }
                return;
            }

            if (grp.RootPart.RETURN_AT_EDGE)
            {
                // We remove the object here
                try
                {
                    List<SceneObjectGroup> objects = new List<SceneObjectGroup>() { grp };
                    grp.Scene.returnObjects(objects.ToArray(), UUID.Zero);
                }
                catch (Exception)
                {
                    m_log.Warn("[SCENE]: exception when trying to return the prim that crossed the border.");
                }
                return;
            }

            Vector3 pos = attemptedPosition;

            //TODO: Fix up group transfer as its probably broken

            // Offset the positions for the new region across the border
            Vector3 oldGroupPosition = grp.RootPart.GroupPosition;
            grp.OffsetForNewRegion(pos);

            // If we fail to cross the border, then reset the position of the scene object on that border.
            if (destination != null && !CrossPrimGroupIntoNewRegion(destination, grp))
            {
                grp.OffsetForNewRegion(oldGroupPosition);
                grp.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
            }
        }

        /// <summary>
        /// Move the given scene object into a new region
        /// </summary>
        /// <param name="newRegionHandle"></param>
        /// <param name="grp">Scene Object Group that we're crossing</param>
        /// <returns>
        /// true if the crossing itself was successful, false on failure
        /// </returns>
        protected bool CrossAttachmentIntoNewRegion(GridRegion destination, SceneObjectGroup grp, UUID userID, UUID ItemID)
        {
            bool successYN = false;
            grp.RootPart.ClearUpdateScheduleOnce();
            if (destination != null)
            {
                if (grp.Scene != null && grp.Scene.SimulationService != null)
                    successYN = grp.Scene.SimulationService.CreateObject(destination, userID, ItemID);

                if (successYN)
                {
                    // We remove the object here
                    try
                    {
                        grp.Scene.DeleteSceneObject(grp, false);
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

        /// <summary>
        /// Move the given scene object into a new region
        /// </summary>
        /// <param name="newRegionHandle"></param>
        /// <param name="grp">Scene Object Group that we're crossing</param>
        /// <returns>
        /// true if the crossing itself was successful, false on failure
        /// </returns>
        protected bool CrossPrimGroupIntoNewRegion(GridRegion destination, SceneObjectGroup grp)
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
                            ScenePresence SP = grp.Scene.GetScenePresence(avID);
                            CrossAgentSittingToNewRegionAsync(SP, destination, grp);
                        }
                    }
                }

                SceneObjectGroup copiedGroup = (SceneObjectGroup)grp.Copy();
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
                        return grp.Scene.DeleteSceneObject(grp, false);
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

        protected bool CrossAttachmentsIntoNewRegion(GridRegion destination, ScenePresence sp)
        {
            List<SceneObjectGroup> m_attachments = sp.Attachments;
            lock (m_attachments)
            {
                //Kill the groups here, otherwise they will become ghost attachments 
                //  and stay in the sim, they'll get readded below into the new sim
                KillAttachments(sp);

                foreach (SceneObjectGroup gobj in m_attachments)
                {
                    // If the prim group is null then something must have happened to it!
                    if (gobj != null && gobj.RootPart != null)
                    {
                        // Set the parent localID to 0 so it transfers over properly.
                        gobj.RootPart.SetParentLocalId(0);
                        gobj.AbsolutePosition = gobj.RootPart.AttachedPos;
                        gobj.RootPart.IsAttachment = false;
                        //gobj.RootPart.LastOwnerID = gobj.GetFromAssetID();
                        m_log.InfoFormat("[ENTITY TRANSFER MODULE]: Sending attachment {0} to region {1}", gobj.UUID, destination.RegionName);
                        CrossAttachmentIntoNewRegion(destination, gobj, sp.UUID, gobj.RootPart.FromItemID);
                    }
                }
                m_attachments.Clear();

                return true;
            }
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
            //m_log.DebugFormat(" >>> IncomingCreateObject(userID, itemID) <<< {0} {1}", userID, itemID);
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
                attachMod.RezSingleAttachmentFromInventory(sp.ControllingClient, itemID, attPt);
                return true;
            }

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

            newObject.RootPart.ParentGroup.CreateScriptInstances(0, false, scene.DefaultScriptEngine, 1, UUID.Zero);
            newObject.RootPart.ParentGroup.ResumeScripts();

            // Do this as late as possible so that listeners have full access to the incoming object
            scene.EventManager.TriggerOnIncomingSceneObject(newObject);

            if (newObject.RootPart.SitTargetAvatar.Count != 0)
            {
                lock (newObject.RootPart.SitTargetAvatar)
                {
                    foreach (UUID avID in newObject.RootPart.SitTargetAvatar)
                    {
                        ScenePresence SP = scene.GetScenePresence(avID);
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

            if (sceneObject.IsAttachmentCheckFull()) // Attachment
            {
                sceneObject.RootPart.AddFlag(PrimFlags.TemporaryOnRez);
                sceneObject.RootPart.AddFlag(PrimFlags.Phantom);

                // Fix up attachment Parent Local ID
                ScenePresence sp = scene.GetScenePresence(sceneObject.OwnerID);
                if (sp != null)
                {
                    scene.SceneGraph.AddPrimToScene(sceneObject);

                    m_log.DebugFormat(
                        "[EntityTransferModule]: Received attachment {0}, inworld asset id {1}", sceneObject.GetFromItemID(), sceneObject.UUID);
                    m_log.DebugFormat(
                        "[EntityTransferModule]: Attach to avatar {0} at position {1}", sp.UUID, sceneObject.AbsolutePosition);

                    IAttachmentsModule attachModule = scene.RequestModuleInterface<IAttachmentsModule>();
                    if (attachModule != null)
                        attachModule.AttachObject(sp.ControllingClient, sceneObject.LocalId, 0, false);

                    sceneObject.RootPart.RemFlag(PrimFlags.TemporaryOnRez);
                    sceneObject.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
                    return true;
                }
            }
            else
            {
                if (!scene.Permissions.CanObjectEntry(sceneObject.UUID,
                        true, sceneObject.AbsolutePosition, sceneObject.OwnerID))
                {
                    // Deny non attachments based on parcel settings
                    //
                    m_log.Info("[EntityTransferModule]: Denied prim crossing " +
                            "because of parcel settings");

                    scene.DeleteSceneObject(sceneObject, true);

                    return false;
                }
                if (scene.SceneGraph.AddPrimToScene(sceneObject))
                {
                    sceneObject.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Misc

        protected bool WaitForCallback(UUID id, out bool callWasCanceled)
        {
            int count = 200;
            while (m_agentsInTransit.Contains(id) && count-- > 0)
            {
                //m_log.Debug("  >>> Waiting... " + count);
                if (CheckForCancelingAgent(id))
                {
                    //If the call was canceled, we need to break here 
                    //   now and tell the code that called us about it
                    callWasCanceled = true;
                    return true;
                }
                Thread.Sleep(100);
            }
            //If we made it through the whole loop, we havn't been canceled,
            //    as we either have timed out or made it, so no checks are needed
            callWasCanceled = false;
            if (count > 0)
                return true;
            else
                return false;
        }

        protected void SetInTransit(UUID id)
        {
            lock (m_agentsInTransit)
            {
                if (!m_agentsInTransit.Contains(id))
                    m_agentsInTransit.Add(id);
            }
        }

        protected bool ResetFromTransit(UUID id)
        {
            RemoveCancelingAgent(id);
            lock (m_agentsInTransit)
            {
                if (m_agentsInTransit.Contains(id))
                {
                    m_agentsInTransit.Remove(id);
                    return true;
                }
            }
            return false;
        }

        public void CancelTeleport(UUID AgentID)
        {
            AddCancelingAgent(AgentID);
        }

        private bool CheckForCancelingAgent(UUID AgentID)
        {
            lock (m_cancelingAgents)
            {
                if (m_cancelingAgents.Contains(AgentID))
                {
                    return true;
                }
            }
            return false;
        }

        private void RemoveCancelingAgent(UUID AgentID)
        {
            lock (m_cancelingAgents)
            {
                m_cancelingAgents.Remove(AgentID);
            }
        }

        private void AddCancelingAgent(UUID AgentID)
        {
            lock (m_cancelingAgents)
            {
                if (!m_cancelingAgents.Contains(AgentID))
                    m_cancelingAgents.Add(AgentID);
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

        /// <summary>
        /// This is pretty much guaranteed to return a Scene
        ///   as it will return the first scene if it cannot find the scene
        /// </summary>
        /// <param name="RegionID"></param>
        /// <returns></returns>
        public Scene TryGetScene(UUID RegionID)
        {
            foreach (Scene scene in m_scenes)
            {
                if (scene.RegionInfo.RegionID == RegionID)
                    return scene;
            }
            return m_scenes[0];
        }

        #endregion
    }
}
