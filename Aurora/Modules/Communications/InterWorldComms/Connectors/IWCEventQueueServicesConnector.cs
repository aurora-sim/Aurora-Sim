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
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Services.CapsService;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules
{
    public class IWCEventQueueServicesConnector : EventQueueMasterService, IEventQueueService, IService
    {
        protected EventQueueMasterService m_localService;
        protected IRegistryCore m_registry;
        protected EventQueueServicesConnector m_remoteService;

        public override string Name
        {
            get { return GetType().Name; }
        }

        #region IEventQueueService Members

        public override IEventQueueService InnerService
        {
            get
            {
                //If we are getting URls for an IWC connection, we don't want to be calling other things, as they are calling us about only our info
                //If we arn't, its ar region we are serving, so give it everything we know
                if (m_registry.RequestModuleInterface<InterWorldCommunications>().IsGettingUrlsForIWCConnection)
                    return m_localService;
                else
                    return this;
            }
        }

        public override bool Enqueue(OSD o, UUID avatarID, ulong RegionHandle)
        {
            if (!base.Enqueue(o, avatarID, RegionHandle))
                if (!m_remoteService.Enqueue(o, avatarID, RegionHandle))
                    return false;
            return true;
        }

        public override bool TryEnqueue(OSD ev, UUID avatarID, ulong RegionHandle)
        {
            if (!base.TryEnqueue(ev, avatarID, RegionHandle))
                if (!m_remoteService.TryEnqueue(ev, avatarID, RegionHandle))
                    return false;
            return true;
        }

        #endregion

        #region IService Members

        public override void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueHandler", "") != Name)
                return;

            base.Initialize(config, registry);
            m_localService = new EventQueueMasterService();
            m_localService.Initialize(config, registry);
            m_remoteService = new EventQueueServicesConnector();
            m_remoteService.Initialize(config, registry);
            registry.RegisterModuleInterface<IEventQueueService>(this);
            m_registry = registry;
        }

        public override void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_remoteService != null)
            {
                base.Start(config, registry);
                m_localService.Start(config, registry);
                m_remoteService.Start(config, registry);
            }
        }

        public override void FinishedStartup()
        {
        }

        #endregion
    }
}