/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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
using System.Reflection;
using System.Timers;

using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

using OpenMetaverse;
using log4net;
using Nini.Config;

namespace OpenSim.Region.CoreModules
{
    public class ActivityDetector : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Timer m_presenceUpdateTimer = null;
        private List<Scene> m_scenes = new List<Scene> ();
        private List<UUID> m_zombieAgents = new List<UUID> ();
        
        public void Initialise(IConfigSource source)
        {
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            scene.AuroraEventManager.RegisterEventHandler ("AgentIsAZombie", OnGenericEvent);
            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
        }

        public void RemoveRegion(Scene scene)
        {
            ISyncMessagePosterService syncMessage = scene.RequestModuleInterface<ISyncMessagePosterService>();
            if (syncMessage != null)
                syncMessage.Post(SyncMessageHelper.LogoutRegionAgents(scene.RegionInfo.RegionHandle), scene.RegionInfo.RegionHandle);
            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
            m_scenes.Remove (scene);
        }

        public void RegionLoaded(Scene scene)
        {
            scene.EventManager.OnStartupFullyComplete += EventManager_OnStartupFullyComplete;
            m_scenes.Add (scene);
            if (m_presenceUpdateTimer == null)
            {
                m_presenceUpdateTimer = new Timer ();
                m_presenceUpdateTimer.Interval = 1000 * 60 * 58; //Bit less than an hour so that we have 2 minute to send all the updates and lag
                m_presenceUpdateTimer.Elapsed += m_presenceUpdateTimer_Elapsed;
            }
        }

        void m_presenceUpdateTimer_Elapsed (object sender, ElapsedEventArgs e)
        {
            IAgentInfoService service = m_scenes[0].RequestModuleInterface<IAgentInfoService> ();
            if (service == null)
                return;
            foreach (Scene scene in m_scenes)
            {
                foreach (IScenePresence sp in scene.GetScenePresences ())
                {
                    //Setting the last position updates the 1 hour presence timer, so send this ~ every hour so that the agent does not get logged out
                    service.SetLastPosition (sp.UUID.ToString (), scene.RegionInfo.RegionID, sp.AbsolutePosition, sp.Lookat);
                }
            }
        }

        void EventManager_OnStartupFullyComplete(IScene scene, List<string> data)
        {
            //Just send the RegionIsOnline message, it will log out all the agents for the region as well
            ISyncMessagePosterService syncMessage = scene.RequestModuleInterface<ISyncMessagePosterService>();
            if (syncMessage != null)
                syncMessage.Post(SyncMessageHelper.RegionIsOnline(scene.RegionInfo.RegionHandle), scene.RegionInfo.RegionHandle);
        }

        public string Name
        {
            get { return "ActivityDetector"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void OnNewClient(IClientAPI client)
        {
            client.OnConnectionClosed += OnConnectionClose;
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnConnectionClosed -= OnConnectionClose;
        }

        public object OnGenericEvent (string functionName, object parameters)
        {
            m_zombieAgents.Add ((UUID)parameters);
            return null;
        }

        public void OnConnectionClose(IClientAPI client)
        {
            IScenePresence sp = null;
            client.Scene.TryGetScenePresence(client.AgentId, out sp);
            if (client.IsLoggingOut && sp != null & !sp.IsChildAgent)
            {
                m_log.InfoFormat("[ActivityDetector]: Detected client logout {0} in {1}", client.AgentId, client.Scene.RegionInfo.RegionName);
            
                //Inform the grid service about it

                if (m_zombieAgents.Contains (client.AgentId))
                {
                    m_zombieAgents.Remove (client.AgentId);
                    return; //They are a known zombie, just clear them out and go on with life!
                }
                client.Scene.RequestModuleInterface<ISyncMessagePosterService>().Get(SyncMessageHelper.AgentLoggedOut(client.AgentId, client.Scene.RegionInfo.RegionHandle), client.Scene.RegionInfo.RegionHandle);
            }
        }
    }
}
