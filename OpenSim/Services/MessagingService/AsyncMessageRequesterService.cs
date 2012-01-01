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
using System.Linq;
using System.Timers;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.MessagingService
{
    /// <summary>
    ///   This module is run on Aurora.exe when it is being run in grid mode as it requests the
    ///   AsyncMessagePostService for any async messages that might have been queued to be sent to us
    /// </summary>
    public class AsyncMessageRequesterService : ISharedRegionModule
    {
        #region Declares

        protected volatile bool m_locked;
        protected List<IScene> m_scenes = new List<IScene>();
        protected Timer m_timer;

        #endregion

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
        }

        public void PostInitialise()
        {
        }

        public void AddRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            IConfig handlerConfig = scene.Config.Configs["Handlers"];
            if (handlerConfig.GetString("AsyncMessageRequesterServiceHandler", "") != Name)
                return;

            m_scenes.Add(scene);

            if (m_timer == null)
            {
                m_timer = new Timer();
                //Start the request timer
                m_timer.Elapsed += requestAsyncMessages;
                m_timer.Interval = 60*1000; //60 secs
                m_timer.Start();
            }
        }

        public void RemoveRegion(IScene scene)
        {
            m_scenes.Remove(scene);
        }

        public void Close()
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        #endregion

        #region Async Requester

        private void requestAsyncMessages(object sender, ElapsedEventArgs e)
        {
            if (m_locked || m_scenes.Count == 0)
                return;
            m_locked = true;
            OSDMap message = CreateWebRequest();
            List<string> serverURIs =
                m_scenes[0].RequestModuleInterface<IConfigurationService>().FindValueOf("MessagingServerURI");
            foreach (string host in serverURIs)
            {
                OSDMap response = WebUtils.PostToService(host, message, true, false, true);
                if (response is OSDMap)
                {
                    if (response["Messages"].Type == OSDType.Map)
                    {
                        OSDMap messages = (OSDMap) response["Messages"];
                        foreach (KeyValuePair<string, OSD> kvp in messages)
                        {
                            OSDArray array = (OSDArray) kvp.Value;
                            IAsyncMessageRecievedService service =
                                GetScene(ulong.Parse(kvp.Key)).RequestModuleInterface<IAsyncMessageRecievedService>();
                            foreach (OSD asyncMessage in array)
                            {
                                service.FireMessageReceived(kvp.Key, (OSDMap) asyncMessage);
                            }
                        }
                    }
                }
            }
            m_locked = false;
        }

        private IScene GetScene(ulong regionHandle)
        {
#if (!ISWIN)
            foreach (IScene scene in m_scenes)
            {
                if (scene.RegionInfo.RegionHandle == regionHandle) return scene;
            }
            return null;
#else
            return m_scenes.FirstOrDefault(scene => scene.RegionInfo.RegionHandle == regionHandle);
#endif
        }

        #region Helpers

        private OSDMap CreateWebRequest()
        {
            OSDMap message = new OSDMap();
            message["Method"] = "AsyncMessageRequest";
            OSDMap request = new OSDMap();
            request["Method"] = "AsyncMessageRequest";
            OSDArray array = new OSDArray();
            foreach (IScene scene in m_scenes)
            {
                array.Add(scene.RegionInfo.RegionHandle);
                array.Add(scene.RegionInfo.GridSecureSessionID);
            }
            request["RegionHandles"] = array;
            message["Message"] = request;
            return message;
        }

        private OSDMap CreateWebResponse(OSDMap request)
        {
            OSDMap message = new OSDMap();
            message["Response"] = OSDParser.DeserializeJson(request["_RawResult"]);
            return message;
        }

        #endregion

        #endregion
    }
}