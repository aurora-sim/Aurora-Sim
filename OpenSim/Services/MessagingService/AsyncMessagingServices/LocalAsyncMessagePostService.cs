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
    public class LocalAsyncMessagePostService : IService, IAsyncMessagePostService
    {
        protected IAsyncMessageRecievedService m_asyncReceiverService;
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
            m_asyncReceiverService.FireMessageReceived(RegionHandle.ToString(), request);
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AsyncMessagePostServiceHandler", "") != Name)
                return;

            m_registry = registry;
            registry.RegisterModuleInterface<IAsyncMessagePostService>(this);
            m_asyncReceiverService = registry.RequestModuleInterface<IAsyncMessageRecievedService>();
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}