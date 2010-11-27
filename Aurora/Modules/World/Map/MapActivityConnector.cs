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
using System.Reflection;

using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

using OpenMetaverse;
using log4net;

namespace Aurora.Modules
{
    public class MapActivityDetector
    {
        //private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_aScene;
        private IGridService m_GridService;
        public MapActivityDetector(Scene scene)
        {
            m_GridService = scene.GridService;
            //m_log.DebugFormat("[MAP ACTIVITY DETECTOR]: starting ");
            // For now the only events we listen to are these
            // But we could trigger the position update more often
            scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;

            //scene.EventManager.OnAvatarEnteringNewParcel += OnEnteringNewParcel;

            if (m_aScene == null)
                m_aScene = scene;
        }

        public void OnNewClient(IClientAPI client)
        {
            client.OnConnectionClosed += OnConnectionClose;
            client.OnLogout += client_OnLogout;
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnConnectionClosed -= OnConnectionClose;
            client.OnLogout -= client_OnLogout;
        }

        public void client_OnLogout(IClientAPI client)
        {
            m_GridService.RemoveAgent(client.Scene.RegionInfo.RegionID,
                    client.AgentId);
        }

        public void OnMakeRootAgent(ScenePresence sp)
        {
            m_GridService.AddAgent(sp.Scene.RegionInfo.RegionID,
                sp.UUID, sp.AbsolutePosition);
        }

        public void OnConnectionClose(IClientAPI client)
        {
            m_GridService.RemoveAgent(client.Scene.RegionInfo.RegionID,
                    client.AgentId);
        }

        public void OnEnteringNewParcel(ScenePresence sp, int localLandID, UUID regionID)
        {
            //We would do this here... if it wouldn't lag movement into new parcels badly
        }
    }
}
