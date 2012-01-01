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
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using BlockingLLSDQueue = Aurora.Framework.BlockingQueue<OpenMetaverse.StructuredData.OSD>;

namespace OpenSim.Services.CapsService
{
    public class EventQueueServicesConnector : EventQueueMasterService, IEventQueueService, IService
    {
        #region Declares

        private IRegistryCore m_registry;

        #endregion

        public override string Name
        {
            get { return GetType().Name; }
        }

        #region IEventQueueService Members

        public override IEventQueueService InnerService
        {
            get { return this; }
        }

        /// <summary>
        ///   Add an EQ message into the queue on the remote EventQueueService
        /// </summary>
        /// <param name = "ev"></param>
        /// <param name = "avatarID"></param>
        /// <param name = "regionHandle"></param>
        /// <returns></returns>
        public override bool Enqueue(OSD ev, UUID avatarID, ulong regionHandle)
        {
            //Do this async so that we don't kill the sim while waiting for this to be sent
            return AddToQueue(ev, avatarID, regionHandle, true);
        }

        /// <summary>
        ///   Add an EQ message into the queue on the remote EventQueueService
        /// </summary>
        /// <param name = "ev"></param>
        /// <param name = "avatarID"></param>
        /// <param name = "regionHandle"></param>
        /// <returns></returns>
        public override bool TryEnqueue(OSD ev, UUID avatarID, ulong regionHandle)
        {
            return AddToQueue(ev, avatarID, regionHandle, false);
        }

        #endregion

        #region IService Members

        public override void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IEventQueueService>(this);
        }

        public override void Start(IConfigSource config, IRegistryCore registry)
        {
            m_service = registry.RequestModuleInterface<ICapsService>();
        }

        #endregion

        private bool AddToQueue(OSD ev, UUID avatarID, ulong regionHandle, bool runasync)
        {
            //MainConsole.Instance.DebugFormat("[EVENTQUEUE]: Enqueuing event for {0} in region {1}", avatarID, m_scene.RegionInfo.RegionName);

            if (ev == null)
                return false;
            try
            {
                OSDMap request = new OSDMap {{"AgentID", avatarID}, {"RegionHandle", regionHandle}};
                OSDArray events = new OSDArray {OSDParser.SerializeLLSDXmlString(ev)};
                //Note: we HAVE to convert it to xml, otherwise things like byte[] arrays will not be passed through correctly!

                request.Add("Events", events);

                IConfigurationService configService = m_registry.RequestModuleInterface<IConfigurationService>();
                List<string> serverURIs = configService.FindValueOf(avatarID.ToString(), regionHandle.ToString(),
                                                                    "EventQueueServiceURI");
                foreach (string serverURI in serverURIs)
                {
                    if (serverURI != "")
                    {
                        if (runasync)
                        {
                            /*AsynchronousRestObjectRequester.MakeRequest("POST", serverURI + "/CAPS/EQMPOSTER",
                            OSDParser.SerializeJsonString(request),
                            delegate(string resp)
                            {
                                return RequestHandler(resp, events, avatarID, regionHandle);
                            });

                        return true;*/
                            string resp = SynchronousRestFormsRequester.MakeRequest("POST", serverURI + "/CAPS/EQMPOSTER", OSDParser.SerializeJsonString(request));
                            return RequestHandler(resp, events, avatarID, regionHandle);
                        }
                        else
                        {
                            string resp = SynchronousRestFormsRequester.MakeRequest("POST", serverURI + "/CAPS/EQMPOSTER", OSDParser.SerializeJsonString(request));
                            return RequestHandler(resp, events, avatarID, regionHandle);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[EVENTQUEUE] Caught exception: " + e);
            }

            return false;
        }

        public bool RequestHandler(string response, OSDArray events, UUID avatarID, ulong RegionHandle)
        {
            OSD r = OSDParser.DeserializeJson(response);
            if (r.Type == OSDType.Map)
            {
                OSDMap result = (OSDMap) r;
                if (result != null)
                {
                    bool success = result["success"].AsBoolean();
                    if (!success)
                        MainConsole.Instance.Warn("[EventQueueServicesConnector]: Failed to post EQMessage for user " + avatarID);
                    else
                        return success;
                }
            }
            return false;
        }
    }
}