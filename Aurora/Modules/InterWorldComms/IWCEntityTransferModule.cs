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
using OpenSim.Server.Base;

using GridRegion = OpenSim.Services.Interfaces.GridRegion;

using OpenMetaverse;
using log4net;
using Nini.Config;
using Aurora.Modules;
using Aurora.DataManager;
using Aurora.Framework;

namespace OpenSim.Region.CoreModules.Framework.EntityTransfer
{
    public class IWCEntityTransferModule : EntityTransferModule, ISharedRegionModule, IEntityTransferModule, IUserAgentVerificationModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Initialized = false;

        private InterWorldComms IWC;

        #region ISharedRegionModule

        public override string Name
        {
            get { return "IWCEntityTransferModule"; }
        }

        public override void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("EntityTransferModule", "");
                if (name == Name)
                {
                    m_agentsInTransit = new List<UUID>();
                    
                    m_Enabled = true;
                    m_log.InfoFormat("[IWC MODULE]: {0} enabled.", Name);
                }
            }
        }

        public override void AddRegion(Scene scene)
        {
            base.AddRegion(scene);
            if (m_Enabled)
            {
                scene.RegisterModuleInterface<IUserAgentVerificationModule>(this);
            }
        }

        protected override void OnNewClient(IClientAPI client)
        {
            client.OnTeleportHomeRequest += TeleportHome;
            client.OnConnectionClosed += new Action<IClientAPI>(OnConnectionClosed);
        }


        public override void RegionLoaded(Scene scene)
        {
            base.RegionLoaded(scene);
            if (m_Enabled)
                if (!m_Initialized)
                {
                    IWC = scene.RequestModuleInterface<InterWorldComms>();
                    m_Initialized = true;
                }

        }
        public override void RemoveRegion(Scene scene)
        {
            base.AddRegion(scene);
            if (m_Enabled)
            {
                scene.UnregisterModuleInterface<IUserAgentVerificationModule>(this);
            }
        }


        #endregion

        #region IWC overrides of IEntityTransferModule

        protected override GridRegion GetFinalDestination(GridRegion region)
        {
            GridRegionFlags flags = IWC.GetRegionFlags(region.RegionID);
            m_log.DebugFormat("[IWC MODULE]: region {0} flags: {1}", region.RegionID, flags);
            if (flags.IsIWCConnected)
            {
                m_log.DebugFormat("[IWC MODULE]: RegionFound {0}", region.RegionID);
                return region;
            }
            return region;
        }

        protected override bool NeedsClosing(uint oldRegionX, uint newRegionX, uint oldRegionY, uint newRegionY, GridRegion reg)
        {
            if (base.NeedsClosing(oldRegionX, newRegionX, oldRegionY, newRegionY, reg))
                return true;

            GridRegionFlags flags = IWC.GetRegionFlags(reg.RegionID);
            if (flags.IsIWCConnected)
                return true;

            return false;
        }

        protected bool CreateAgent(ScenePresence sp, GridRegion reg, GridRegion finalDestination, AgentCircuitData agentCircuit, uint teleportFlags, out string reason, out bool IsIWC)
        {
            reason = string.Empty;
            IsIWC = false;
            GridRegionFlags flags = IWC.GetRegionFlags(reg.RegionID);
            if (flags.IsIWCConnected)
            {
                IsIWC = true;
                bool success = IWC.FireNewIWCUser(agentCircuit, reg, finalDestination, out reason);
                if (success)
                {
                    // Log them out of this grid
                    //m_aScene.PresenceService.LogoutAgent(agentCircuit.SessionID, sp.AbsolutePosition, sp.Lookat);
                    
                }
                try
                {
                    return success;
                }
                finally
                {
                    //sp.Scene.IncomingCloseAgent(sp.UUID);
                }
            }
            return m_aScene.SimulationService.CreateAgent(reg, agentCircuit, teleportFlags, out reason);
        }

        public override void TeleportHome(UUID id, IClientAPI client)
        {
            m_log.DebugFormat("[IWC MODULE]: Request to teleport {0} {1} home", client.FirstName, client.LastName);

            // Let's find out if this is a foreign user or a local user
            UserAccount account = m_aScene.UserAccountService.GetUserAccount(m_aScene.RegionInfo.ScopeID, id);
            if (account != null)
            {
                // local grid user
                m_log.DebugFormat("[IWC MODULE]: User is local");
                base.TeleportHome(id, client);
                return;
            }

            // Foreign user wants to go home
            // 
            AgentCircuitData aCircuit = ((Scene)(client.Scene)).AuthenticateHandler.GetAgentCircuitData(client.CircuitCode);
            if (aCircuit == null || (aCircuit != null && !aCircuit.ServiceURLs.ContainsKey("HomeURI")))
            {
                client.SendTeleportFailed("Your information has been lost");
                m_log.DebugFormat("[IWC MODULE]: Unable to locate agent's gateway information");
                return;
            }

            ScenePresence sp = ((Scene)(client.Scene)).GetScenePresence(client.AgentId);
            if (sp == null)
            {
                client.SendTeleportFailed("Internal error");
                m_log.DebugFormat("[IWC MODULE]: Agent not found in the scene where it is supposed to be");
                return;
            }
            Vector3 position = Vector3.Zero;
            Vector3 lookAt = Vector3.Zero;
            IEventQueue eq = sp.Scene.RequestModuleInterface<IEventQueue>();
            GridRegion home = IWC.GetDefaultRegion(aCircuit, out position, out lookAt);

            m_log.DebugFormat("[IWC MODULE]: teleporting user {0} {1} home to {2} via {3}:{4}:{5}",
                aCircuit.firstname, aCircuit.lastname, home.RegionName, home.ExternalHostName, home.HttpPort, home.RegionName);

            DoTeleport(sp, home, home, position, lookAt, (uint)(Constants.TeleportFlags.SetLastToTarget | Constants.TeleportFlags.ViaHome), eq);
        }

        public override void Teleport(ScenePresence sp, ulong regionHandle, Vector3 position, Vector3 lookAt, uint teleportFlags)
        {
            if (!sp.Scene.Permissions.CanTeleport(sp.UUID))
                return;

            IEventQueue eq = sp.Scene.RequestModuleInterface<IEventQueue>();

            // Reset animations; the viewer does that in teleports.
            sp.Animator.ResetAnimations();

            try
            {
                if (regionHandle == sp.Scene.RegionInfo.RegionHandle)
                {
                    m_log.DebugFormat(
                        "[ENTITY TRANSFER MODULE]: RequestTeleportToLocation {0} within {1}",
                        position, sp.Scene.RegionInfo.RegionName);

                    // Teleport within the same region
                    if (IsOutsideRegion(sp.Scene, position) || position.Z < 0)
                    {
                        Vector3 emergencyPos = new Vector3(128, 128, 128);

                        m_log.WarnFormat(
                            "[ENTITY TRANSFER MODULE]: RequestTeleportToLocation() was given an illegal position of {0} for avatar {1}, {2}.  Substituting {3}",
                            position, sp.Name, sp.UUID, emergencyPos);
                        position = emergencyPos;
                    }

                    // TODO: Get proper AVG Height
                    float localAVHeight = 1.56f;
                    float posZLimit = 22;

                    // TODO: Check other Scene HeightField
                    if (position.X > 0 && position.X <= (int)Constants.RegionSize && position.Y > 0 && position.Y <= (int)Constants.RegionSize)
                    {
                        posZLimit = (float)sp.Scene.Heightmap[(int)position.X, (int)position.Y];
                    }

                    float newPosZ = posZLimit + localAVHeight;
                    if (posZLimit >= (position.Z - (localAVHeight / 2)) && !(Single.IsInfinity(newPosZ) || Single.IsNaN(newPosZ)))
                    {
                        position.Z = newPosZ;
                    }

                    // Only send this if the event queue is null
                    if (eq == null)
                        sp.ControllingClient.SendTeleportLocationStart();

                    sp.ControllingClient.SendLocalTeleport(position, lookAt, teleportFlags);
                    sp.Teleport(position);
                }
                else // Another region possibly in another simulator
                {
                    uint x = 0, y = 0;
                    OpenMetaverse.Utils.LongToUInts(regionHandle, out x, out y);
                    GridRegion reg = m_aScene.GridService.GetRegionByPosition(sp.Scene.RegionInfo.ScopeID, (int)x, (int)y);

                    if (reg != null)
                    {
                        GridRegion finalDestination = GetFinalDestination(reg);
                        if (finalDestination == null)
                        {
                            m_log.DebugFormat("[ENTITY TRANSFER MODULE]: Final destination is having problems. Unable to teleport agent.");
                            sp.ControllingClient.SendTeleportFailed("Problem at destination");
                            return;
                        }
                        m_log.DebugFormat("[ENTITY TRANSFER MODULE]: Final destination is x={0} y={1} uuid={2}",
                            finalDestination.RegionLocX / Constants.RegionSize, finalDestination.RegionLocY / Constants.RegionSize, finalDestination.RegionID);

                        //
                        // This is it
                        //
                        DoTeleport(sp, reg, finalDestination, position, lookAt, teleportFlags, eq);

                        return;
                    }
                    reg = IWC.TryGetRegion((int)x, (int)y);
                    if (reg != null)
                    {
                        GridRegion finalDestination = GetFinalDestination(reg);
                        if (finalDestination == null)
                        {
                            m_log.DebugFormat("[ENTITY TRANSFER MODULE]: Final destination is having problems. Unable to teleport agent.");
                            sp.ControllingClient.SendTeleportFailed("Problem at destination");
                            return;
                        }
                        m_log.DebugFormat("[ENTITY TRANSFER MODULE]: Final destination is x={0} y={1} uuid={2}",
                            finalDestination.RegionLocX / Constants.RegionSize, finalDestination.RegionLocY / Constants.RegionSize, finalDestination.RegionID);

                        //
                        // This is it
                        //
                        DoTeleport(sp, reg, finalDestination, position, lookAt, teleportFlags, eq);

                    }
                    else
                    {
                        // TP to a place that doesn't exist (anymore)
                        // Inform the viewer about that
                        sp.ControllingClient.SendTeleportFailed("The region you tried to teleport to doesn't exist anymore");

                        // and set the map-tile to '(Offline)'
                        uint regX, regY;
                        OpenMetaverse.Utils.LongToUInts(regionHandle, out regX, out regY);

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
                m_log.DebugFormat("[ENTITY TRANSFER MODULE]: Exception on teleport: {0}\n{1}", e.Message, e.StackTrace);
                sp.ControllingClient.SendTeleportFailed("Internal error");
            }
        }
        
        public override void DoTeleport(ScenePresence sp, GridRegion reg, GridRegion finalDestination, Vector3 position, Vector3 lookAt, uint teleportFlags, IEventQueue eq)
        {
            if (reg == null || finalDestination == null)
            {
                sp.ControllingClient.SendTeleportFailed("Unable to locate destination");
                return;
            }

            m_log.DebugFormat(
                "[ENTITY TRANSFER MODULE]: Request Teleport to {0}:{1}:{2}/{3}",
                reg.ExternalHostName, reg.HttpPort, finalDestination.RegionName, position);

            uint newRegionX = (uint)(reg.RegionHandle >> 40);
            uint newRegionY = (((uint)(reg.RegionHandle)) >> 8);
            uint oldRegionX = (uint)(sp.Scene.RegionInfo.RegionHandle >> 40);
            uint oldRegionY = (((uint)(sp.Scene.RegionInfo.RegionHandle)) >> 8);

            ulong destinationHandle = finalDestination.RegionHandle;

            if (eq == null)
                sp.ControllingClient.SendTeleportLocationStart();

            // Let's do DNS resolution only once in this process, please!
            // This may be a costly operation. The reg.ExternalEndPoint field is not a passive field,
            // it's actually doing a lot of work.
            IPEndPoint endPoint = finalDestination.ExternalEndPoint;
            if (endPoint.Address != null)
            {
                // Fixing a bug where teleporting while sitting results in the avatar ending up removed from
                // both regions
                if (sp.ParentID != (uint)0)
                    sp.StandUp();

                if (!sp.ValidateAttachments())
                {
                    sp.ControllingClient.SendTeleportFailed("Inconsistent attachment state");
                    return;
                }

                // the avatar.Close below will clear the child region list. We need this below for (possibly)
                // closing the child agents, so save it here (we need a copy as it is Clear()-ed).
                //List<ulong> childRegions = new List<ulong>(avatar.GetKnownRegionList());
                // Compared to ScenePresence.CrossToNewRegion(), there's no obvious code to handle a teleport
                // failure at this point (unlike a border crossing failure).  So perhaps this can never fail
                // once we reach here...
                //avatar.Scene.RemoveCapsHandler(avatar.UUID);

                string capsPath = String.Empty;

                AgentCircuitData currentAgentCircuit = sp.Scene.AuthenticateHandler.GetAgentCircuitData(sp.ControllingClient.CircuitCode);
                AgentCircuitData agentCircuit = sp.ControllingClient.RequestClientInfo();
                agentCircuit.startpos = position;
                agentCircuit.child = true;
                agentCircuit.Appearance = sp.Appearance;
                if (currentAgentCircuit != null)
                    agentCircuit.ServiceURLs = currentAgentCircuit.ServiceURLs;

                if (NeedsNewAgent(oldRegionX, newRegionX, oldRegionY, newRegionY))
                {
                    // brand new agent, let's create a new caps seed
                    agentCircuit.CapsPath = CapsUtil.GetRandomCapsObjectPath();
                }

                string reason = String.Empty;
                bool isIWC = false;
                // Let's create an agent there if one doesn't exist yet. 
                if (!CreateAgent(sp, reg, finalDestination, agentCircuit, teleportFlags, out reason, out isIWC))
                {
                    sp.ControllingClient.SendTeleportFailed(String.Format("Destination refused: {0}",
                                                                              reason));
                    return;
                }

                // OK, it got this agent. Let's close some child agents
                sp.CloseChildAgents(newRegionX, newRegionY);

                if (NeedsNewAgent(oldRegionX, newRegionX, oldRegionY, newRegionY))
                {
                    #region IP Translation for NAT
                    IClientIPEndpoint ipepClient;
                    if (sp.ClientView.TryGet(out ipepClient))
                    {
                        capsPath
                            = "http://"
                              + NetworkUtil.GetHostFor(ipepClient.EndPoint, finalDestination.ExternalHostName)
                              + ":"
                              + finalDestination.HttpPort
                              + CapsUtil.GetCapsSeedPath(agentCircuit.CapsPath);
                    }
                    else
                    {
                        capsPath
                            = "http://"
                              + finalDestination.ExternalHostName
                              + ":"
                              + finalDestination.HttpPort
                              + CapsUtil.GetCapsSeedPath(agentCircuit.CapsPath);
                    }
                    #endregion

                    if (eq != null)
                    {
                        #region IP Translation for NAT
                        // Uses ipepClient above
                        if (sp.ClientView.TryGet(out ipepClient))
                        {
                            endPoint.Address = NetworkUtil.GetIPFor(ipepClient.EndPoint, endPoint.Address);
                        }
                        #endregion

                        eq.EnableSimulator(destinationHandle, endPoint, sp.UUID);

                        // ES makes the client send a UseCircuitCode message to the destination, 
                        // which triggers a bunch of things there.
                        // So let's wait
                        Thread.Sleep(200);

                        eq.EstablishAgentCommunication(sp.UUID, endPoint, capsPath);

                    }
                    else
                    {
                        sp.ControllingClient.InformClientOfNeighbour(destinationHandle, endPoint);
                    }
                }
                else
                {
                    agentCircuit.CapsPath = sp.Scene.CapsModule.GetChildSeed(sp.UUID, reg.RegionHandle);
                    capsPath = "http://" + finalDestination.ExternalHostName + ":" + finalDestination.HttpPort
                                + "/CAPS/" + agentCircuit.CapsPath + "0000/";
                }

                // Expect avatar crossing is a heavy-duty function at the destination.
                // That is where MakeRoot is called, which fetches appearance and inventory.
                // Plus triggers OnMakeRoot, which spawns a series of asynchronous updates.
                //m_commsProvider.InterRegion.ExpectAvatarCrossing(reg.RegionHandle, avatar.ControllingClient.AgentId,
                //                                                      position, false);

                //{
                //    avatar.ControllingClient.SendTeleportFailed("Problem with destination.");
                //    // We should close that agent we just created over at destination...
                //    List<ulong> lst = new List<ulong>();
                //    lst.Add(reg.RegionHandle);
                //    SendCloseChildAgentAsync(avatar.UUID, lst);
                //    return;
                //}

                SetInTransit(sp.UUID);

                // Let's send a full update of the agent. This is a synchronous call.
                AgentData agent = new AgentData();
                sp.CopyTo(agent);
                agent.Position = position;
                SetCallbackURL(agent, sp.Scene.RegionInfo);

                UpdateAgent(reg, finalDestination, agent);

                m_log.DebugFormat(
                    "[ENTITY TRANSFER MODULE]: Sending new CAPS seed url {0} to client {1}", capsPath, sp.UUID);


                if (eq != null)
                {
                    eq.TeleportFinishEvent(destinationHandle, 13, endPoint,
                                           0, teleportFlags, capsPath, sp.UUID);
                }
                else
                {
                    sp.ControllingClient.SendRegionTeleport(destinationHandle, 13, endPoint, 4,
                                                                teleportFlags, capsPath);
                }
                if (!isIWC)
                {
                    // TeleportFinish makes the client send CompleteMovementIntoRegion (at the destination), which
                    // trigers a whole shebang of things there, including MakeRoot. So let's wait for confirmation
                    // that the client contacted the destination before we send the attachments and close things here.
                    if (!WaitForCallback(sp.UUID))
                    {
                        // Client never contacted destination. Let's restore everything back
                        sp.ControllingClient.SendTeleportFailed("Problems connecting to destination.");

                        ResetFromTransit(sp.UUID);

                        // Yikes! We should just have a ref to scene here.
                        //sp.Scene.InformClientOfNeighbours(sp);
                        EnableChildAgents(sp);

                        // Finally, kill the agent we just created at the destination.
                        m_aScene.SimulationService.CloseAgent(finalDestination, sp.UUID);

                        return;
                    }
                }

                // CrossAttachmentsIntoNewRegion is a synchronous call. We shouldn't need to wait after it
                CrossAttachmentsIntoNewRegion(finalDestination, sp, true);

                KillEntity(sp.Scene, sp.LocalId);

                sp.MakeChildAgent();
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


                // REFACTORING PROBLEM. Well, not a problem, but this method is HORRIBLE!
                if (sp.Scene.NeedSceneCacheClear(sp.UUID))
                {
                    m_log.DebugFormat(
                        "[ENTITY TRANSFER MODULE]: User {0} is going to another region, profile cache removed",
                        sp.UUID);
                }
            }
            else
            {
                sp.ControllingClient.SendTeleportFailed("Remote Region appears to be down");
            }
        }

        #endregion

        #region IUserAgentVerificationModule

        public bool VerifyClient(AgentCircuitData aCircuit, string token)
        {
            /*if (aCircuit.ServiceURLs.ContainsKey("HomeURI"))
            {
                string url = aCircuit.ServiceURLs["HomeURI"].ToString();
                IUserAgentService security = new UserAgentServiceConnector(url);
                return security.VerifyClient(aCircuit.SessionID, token);
            }*/

            return true;
        }

        void OnConnectionClosed(IClientAPI obj)
        {
            if (IWC.IsForeignAgent(obj.AgentId) || IWC.IsLocalAgent(obj.AgentId))
            {
                AgentCircuitData aCircuit = ((Scene)(obj.Scene)).AuthenticateHandler.GetAgentCircuitData(obj.CircuitCode);
                m_aScene.PresenceService.LogoutAgent(aCircuit.SessionID, new Vector3(), new Vector3());
            }
            else if (obj.IsLoggingOut)
            {
                AgentCircuitData aCircuit = ((Scene)(obj.Scene)).AuthenticateHandler.GetAgentCircuitData(obj.CircuitCode);
                string reason = "";
                m_log.Info("[IWC Module]: Logging out user " + aCircuit.firstname + " " + aCircuit.lastname + ".");
                bool Sent = IWC.FireLogOutIWCUser(aCircuit, out reason);
                if (!Sent)
                    m_log.Error("[IWC Module]: Was not able to remove presence from foreign world, error: " + reason);
            }
        }

        #endregion
    }
}
