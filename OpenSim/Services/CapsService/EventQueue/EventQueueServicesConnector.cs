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
        
        private string m_serverURL = "";
        
        #endregion

        #region IService Members

        public override string Name
        {
            get { return GetType().Name; }
        }

        public override void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public override void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IEventQueueService>(this);
        }

        public override void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public override void PostStart(IConfigSource config, IRegistryCore registry)
        {
            string url = registry.RequestModuleInterface<IAutoConfigurationService>().FindValueOf("EventQueueServiceURI", "EventQueueService");
            //Clean it up a bit
            url = url.EndsWith("/") ? url.Remove(url.Length - 1) : url;
            m_serverURL = url + "/CAPS/EQMPOSTER";
            m_service = registry.RequestModuleInterface<ICapsService>();
        }

        public override void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IEventQueueService>(this);
        }

        #endregion

        #region Find EQM Password

        /// <summary>
        /// Find the password of the user that we may have recieved in the CAPS seed request.
        /// </summary>
        /// <param name="agentID"></param>
        private bool FindAndPopulateEQMPassword(UUID agentID, ulong RegionHandle, out UUID Password)
        {
            if (m_service != null)
            {
                IClientCapsService clientCaps = m_service.GetClientCapsService(agentID);
                if (clientCaps != null)
                {
                    IRegionClientCapsService regionClientCaps = clientCaps.GetCapsService(RegionHandle);
                    if (regionClientCaps != null)
                    {
                        if (regionClientCaps.RequestMap.ContainsKey("EventQueuePass"))
                        {
                            Password = regionClientCaps.RequestMap["EventQueuePass"].AsUUID();
                            return true;
                        }
                    }
                }
            }
            Password = UUID.Zero;
            return false;
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
            try
            {
                //Do this async so that we don't kill the sim while waiting for this to be sent
                //TODO: Maybe have a thread that runs through this and sends them off instead of doing fire and forget?
                Util.FireAndForget(delegate(object o)
                {
                    TryEnqueue(ev, avatarID, regionHandle);
                });
            } 
            catch(Exception e)
            {
                m_log.Error("[EVENTQUEUE] Caught exception: " + e);
                return false;
            }
            
            return true;
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
            //m_log.DebugFormat("[EVENTQUEUE]: Enqueuing event for {0} in region {1}", avatarID, m_scene.RegionInfo.RegionName);
            try
            {
                UUID Password;
                if (!FindAndPopulateEQMPassword(avatarID, regionHandle, out Password))
                    return false;

                OSDMap request = new OSDMap();
                request.Add("AgentID", avatarID);
                request.Add("RegionHandle", regionHandle);
                request.Add("Password", Password);
                //Note: we HAVE to convert it to xml, otherwise things like byte[] arrays will not be passed through correctly!
                request.Add("Event", OSDParser.SerializeLLSDXmlString(ev));

                OSDMap reply = WebUtils.PostToService(m_serverURL, request);

                if (reply != "")
                {
                    if (reply["_Result"] is OSDMap)
                    {
                        OSDMap result = (OSDMap)reply["_Result"];
                        if (result["success"].AsBoolean())
                            return true;
                    }
                }
            }
            catch (Exception e)
            {
                m_log.Error("[EVENTQUEUE] Caught exception: " + e);
                return false;
            }

            return false;
        }

        #endregion

        #region Overrides

        public override void DisableSimulator(ulong handle, UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.DisableSimulator(handle);
            Enqueue(item, avatarID, RegionHandle);
            //ALSO, do the base.Enqueue so the region Caps get the kill request as well
            base.Enqueue(item, avatarID, RegionHandle);
        }

        #endregion
    }
}
