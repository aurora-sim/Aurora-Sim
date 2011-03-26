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
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Messages.Linden;
using OpenMetaverse.Packets;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using BlockingLLSDQueue = OpenSim.Framework.BlockingQueue<OpenMetaverse.StructuredData.OSD>;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Capabilities;

namespace OpenSim.Services.CapsService
{
    public class EventQueueServicesConnector : EventQueueMasterService, IEventQueueService, IService
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IRegistryCore m_registry;

        #endregion

        #region IService Members

        public override string Name
        {
            get { return GetType().Name; }
        }

        public override IEventQueueService InnerService
        {
            get { return this; }
        }

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

        #region IEventQueue Members

        /// <summary>
        /// Add an EQ message into the queue on the remote EventQueueService 
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="avatarID"></param>
        /// <param name="regionHandle"></param>
        /// <returns></returns>
        public override bool Enqueue(OSD ev, UUID avatarID, ulong regionHandle)
        {
            //Do this async so that we don't kill the sim while waiting for this to be sent
            AddToQueue(ev, avatarID, regionHandle, true);

            return true;
        }

        private bool AddToQueue(OSD ev, UUID avatarID, ulong regionHandle, bool async)
        {
            //m_log.DebugFormat("[EVENTQUEUE]: Enqueuing event for {0} in region {1}", avatarID, m_scene.RegionInfo.RegionName);
            
            if (ev == null)
                return false;
            try
            {
                OSDMap request = new OSDMap();
                request.Add("AgentID", avatarID);
                request.Add("RegionHandle", regionHandle);
                OSDArray events = new OSDArray();
                //Note: we HAVE to convert it to xml, otherwise things like byte[] arrays will not be passed through correctly!
                events.Add(OSDParser.SerializeLLSDXmlString(ev)); //Add this event

                request.Add("Events", events);

                List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(avatarID.ToString(), regionHandle.ToString(), "EventQueueServiceURI");
                foreach (string serverURI in serverURIs)
                {
                    if (async)
                    {
                        /*AsynchronousRestObjectRequester.MakeRequest("POST", serverURI + "/CAPS/EQMPOSTER",
                            OSDParser.SerializeJsonString(request),
                            delegate(string resp)
                            {
                                return RequestHandler(resp, events, avatarID, regionHandle);
                            });

                        return true;*/
                        string resp = SynchronousRestFormsRequester.MakeRequest("POST", serverURI + "/CAPS/EQMPOSTER",
                            OSDParser.SerializeJsonString(request));
                        return RequestHandler(resp, events, avatarID, regionHandle);
                    }
                    else
                    {
                        string resp = SynchronousRestFormsRequester.MakeRequest("POST", serverURI + "/CAPS/EQMPOSTER",
                            OSDParser.SerializeJsonString(request));
                        return RequestHandler(resp, events, avatarID, regionHandle);
                    }
                }
            }
            catch (Exception e)
            {
                m_log.Error("[EVENTQUEUE] Caught exception: " + e.ToString());
            }

            return false;
        }

        public bool RequestHandler(string response, OSDArray events, UUID avatarID, ulong RegionHandle)
        {
            OSD r = OSDParser.DeserializeJson(response);
            if (r.Type == OSDType.Map)
            {
                OSDMap result = (OSDMap)r;
                if (result != null)
                {
                    bool success = result["success"].AsBoolean();
                    if (!success)
                    {
                        m_log.Warn("[EventQueueServicesConnector]: Failed to post EQMessage for user " + avatarID);
                    }
                    else
                        return success;
                }
            }
            return false;
        }

        /// <summary>
        /// Add an EQ message into the queue on the remote EventQueueService 
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="avatarID"></param>
        /// <param name="regionHandle"></param>
        /// <returns></returns>
        public override bool TryEnqueue(OSD ev, UUID avatarID, ulong regionHandle)
        {
            return AddToQueue(ev, avatarID, regionHandle, false);
        }

        #endregion
    }
}
