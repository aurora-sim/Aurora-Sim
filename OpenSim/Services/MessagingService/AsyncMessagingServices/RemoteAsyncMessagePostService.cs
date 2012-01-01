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

using System.Collections.Generic;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.MessagingService
{
    /// <summary>
    ///   This class deals with putting async messages into the regions 'queues' and sending them to them
    ///   when they request them. This is used for Aurora.Server
    /// </summary>
    public class RemoteAsyncMessagePostService : IService, IAsyncMessagePostService
    {
        protected IAsyncMessageRecievedService m_asyncReceiverService;

        protected Dictionary<ulong, OSDArray> m_regionMessages = new Dictionary<ulong, OSDArray>();
        protected IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IAsyncMessagePostService Members

        /// <summary>
        ///   Post a new message to the given region by region handle
        /// </summary>
        /// <param name = "RegionHandle"></param>
        /// <param name = "request"></param>
        public void Post(ulong RegionHandle, OSDMap request)
        {
            if (!m_regionMessages.ContainsKey(RegionHandle))
                m_regionMessages.Add(RegionHandle, new OSDArray());

            m_regionMessages[RegionHandle].Add(request);
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AsyncMessagePostServiceHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IAsyncMessagePostService>(this);
            m_asyncReceiverService = registry.RequestModuleInterface<IAsyncMessageRecievedService>();

            //Read any messages received to see whether they are for the async service
            m_asyncReceiverService.OnMessageReceived += OnMessageReceived;
        }

        public void FinishedStartup()
        {
        }

        #endregion

        protected OSDMap OnMessageReceived(OSDMap message)
        {
            //If it is an async message request, make sure that the request is valid and check it
            if (message["Method"] == "AsyncMessageRequest")
            {
                try
                {
                    ICapsService service = m_registry.RequestModuleInterface<ICapsService>();
                    OSDMap response = new OSDMap();
                    OSDMap mapresponse = new OSDMap();

                    if (message.ContainsKey("RegionHandles"))
                    {
                        OSDArray handles = (OSDArray) message["RegionHandles"];

                        for (int i = 0; i < handles.Count; i += 2)
                        {
                            ulong regionHandle = handles[i].AsULong();
                            IRegionCapsService region = service.GetCapsForRegion(regionHandle);
                            if (region != null)
                            {
                                bool verified = (region.Region.SessionID == handles[i + 1].AsUUID());
                                if (verified)
                                {
                                    if (m_regionMessages.ContainsKey(regionHandle))
                                    {
                                        //Get the array, then remove it
                                        OSDArray array = m_regionMessages[regionHandle];
                                        m_regionMessages.Remove(regionHandle);
                                        mapresponse[regionHandle.ToString()] = array;
                                    }
                                }
                            }
                        }
                    }

                    response["Messages"] = mapresponse;
                    return response;
                }
                catch
                {
                }
            }
            return null;
        }
    }
}