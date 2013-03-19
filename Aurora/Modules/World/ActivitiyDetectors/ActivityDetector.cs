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

using Aurora.Framework;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Other;
using Nini.Config;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Timers;

namespace Aurora.Modules.ActivityDetectors
{
    public class ActivityDetector : INonSharedRegionModule
    {
        private IScene m_scene;
        private readonly List<UUID> m_zombieAgents = new List<UUID>();
        private Timer m_presenceUpdateTimer;

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
        }

        public void Close()
        {
        }

        public void AddRegion(IScene scene)
        {
            scene.AuroraEventManager.RegisterEventHandler("AgentIsAZombie", OnGenericEvent);
            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
        }

        public void RemoveRegion(IScene scene)
        {
            ISyncMessagePosterService syncMessage = scene.RequestModuleInterface<ISyncMessagePosterService>();
            if (syncMessage != null)
                syncMessage.PostToServer(SyncMessageHelper.LogoutRegionAgents(scene.RegionInfo.RegionID));
            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
            m_scene = null;
        }

        public void RegionLoaded(IScene scene)
        {
            scene.EventManager.OnStartupFullyComplete += EventManager_OnStartupFullyComplete;
            m_scene = scene;
            if (m_presenceUpdateTimer == null)
            {
                m_presenceUpdateTimer = new Timer {Interval = 1000*60*28};
                //As agents move around, they could get to regions that won't update them in time, so we cut 
                // the time in half, and then a bit less so that they are updated consistantly
                m_presenceUpdateTimer.Elapsed += m_presenceUpdateTimer_Elapsed;
                m_presenceUpdateTimer.Start();
            }
        }

        public string Name
        {
            get { return "ActivityDetector"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        private void m_presenceUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            IAgentInfoService service = m_scene.RequestModuleInterface<IAgentInfoService>();
            if (service == null)
                return;
            foreach (IScenePresence sp in m_scene.GetScenePresences())
            {
                //This causes the last pos to be updated in the database, along with the last seen time
                sp.AddChildAgentUpdateTaint(1);
            }
        }

        private void EventManager_OnStartupFullyComplete(IScene scene, List<string> data)
        {
            //Just send the RegionIsOnline message, it will log out all the agents for the region as well
            ISyncMessagePosterService syncMessage = scene.RequestModuleInterface<ISyncMessagePosterService>();
            if (syncMessage != null)
                syncMessage.PostToServer(SyncMessageHelper.RegionIsOnline(scene.RegionInfo.RegionID));
        }

        public void OnNewClient(IClientAPI client)
        {
            client.OnConnectionClosed += OnConnectionClose;
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnConnectionClosed -= OnConnectionClose;
        }

        public object OnGenericEvent(string functionName, object parameters)
        {
            m_zombieAgents.Add((UUID) parameters);
            return null;
        }

        public void OnConnectionClose(IClientAPI client)
        {
            IScenePresence sp = null;
            client.Scene.TryGetScenePresence(client.AgentId, out sp);
            if (client.IsLoggingOut && sp != null & !sp.IsChildAgent)
            {
                MainConsole.Instance.InfoFormat("[ActivityDetector]: Detected logout of user {0} in region {1}",
                                                client.Name,
                                                client.Scene.RegionInfo.RegionName);

                //Inform the grid service about it

                if (m_zombieAgents.Contains(client.AgentId))
                {
                    m_zombieAgents.Remove(client.AgentId);
                    return; //They are a known zombie, just clear them out and go on with life!
                }
                AgentPosition agentpos = new AgentPosition
                                             {
                                                 AgentID = sp.UUID,
                                                 AtAxis = sp.CameraAtAxis,
                                                 Center = sp.CameraPosition,
                                                 Far = sp.DrawDistance,
                                                 LeftAxis = Vector3.Zero,
                                                 Position = sp.AbsolutePosition
                                             };
                if (agentpos.Position.X > sp.Scene.RegionInfo.RegionSizeX)
                    agentpos.Position.X = sp.Scene.RegionInfo.RegionSizeX;
                if (agentpos.Position.Y > sp.Scene.RegionInfo.RegionSizeY)
                    agentpos.Position.Y = sp.Scene.RegionInfo.RegionSizeY;
                if (agentpos.Position.Z > sp.Scene.RegionInfo.RegionSizeZ)
                    agentpos.Position.Z = sp.Scene.RegionInfo.RegionSizeZ;
                if (agentpos.Position.X < 0)
                    agentpos.Position.X = 0;
                if (agentpos.Position.Y < 0)
                    agentpos.Position.Y = 0;
                if (agentpos.Position.Z < 0)
                    agentpos.Position.Z = 0;
                agentpos.RegionHandle = sp.Scene.RegionInfo.RegionHandle;
                agentpos.Size = sp.PhysicsActor != null ? sp.PhysicsActor.Size : new Vector3(0, 0, 1.8f);
                agentpos.UpAxis = Vector3.Zero;
                agentpos.Velocity = sp.Velocity;
                agentpos.UserGoingOffline = true; //Don't attempt to add us into other regions

                //Send the child agent data update
                ISyncMessagePosterService syncPoster = sp.Scene.RequestModuleInterface<ISyncMessagePosterService>();
                if (syncPoster != null)
                    syncPoster.PostToServer(SyncMessageHelper.AgentLoggedOut(client.AgentId,
                                                                             client.Scene.RegionInfo.RegionID, agentpos));
            }
        }
    }
}