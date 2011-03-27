using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Services.Connectors;
using OpenSim.Services.MessagingService;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using OpenSim.Services.CapsService;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules 
{
    public class IWCEventQueueServicesConnector : EventQueueMasterService, IEventQueueService, IService
    {
        protected EventQueueServicesConnector m_remoteService;
        #region IService Members

        public override string Name
        {
            get { return GetType().Name; }
        }

        public override IEventQueueService InnerService
        {
            get { return this; }
        }

        public override void Initialize (IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString ("EventQueueHandler", "") != Name)
                return;

            base.Initialize(config, registry);
            m_remoteService = new EventQueueServicesConnector ();
            m_remoteService.Initialize(config, registry);
            registry.RegisterModuleInterface<IEventQueueService> (this);
        }

        public override void Start (IConfigSource config, IRegistryCore registry)
        {
            if (m_remoteService != null)
            {
                base.Start (config, registry);
                m_remoteService.Start (config, registry);
            }
        }

        public override void FinishedStartup()
        {
        }

        #endregion

        #region IEventQueueService Members

        public override bool Enqueue (OSD o, UUID avatarID, ulong RegionHandle)
        {
            if (!base.Enqueue (o, avatarID, RegionHandle))
                if (!m_remoteService.Enqueue (o, avatarID, RegionHandle))
                    return false;
            return true;
        }

        public override bool TryEnqueue (OSD ev, UUID avatarID, ulong RegionHandle)
        {
            if (!base.TryEnqueue (ev, avatarID, RegionHandle))
                if (!m_remoteService.TryEnqueue (ev, avatarID, RegionHandle))
                    return false;
            return true;
        }

        #endregion
    }
}
