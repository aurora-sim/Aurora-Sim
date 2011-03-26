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
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules 
{
    public class IWCSyncMessagePosterConnector : ISyncMessagePosterService, IService
    {
        protected LocalSyncMessagePosterService m_localService;
        protected RemoteSyncMessagePosterService m_remoteService;
        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public virtual ISyncMessagePosterService InnerService
        {
            get { return m_localService; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("SyncMessagePosterServiceHandler", "") != Name)
                return;

            m_localService = new LocalSyncMessagePosterService();
            m_localService.Initialize(config, registry);
            m_remoteService = new RemoteSyncMessagePosterService();
            m_remoteService.Initialize(config, registry);
            registry.RegisterModuleInterface<ISyncMessagePosterService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region ISyncMessagePosterService Members

        public void Post(OSDMap request, ulong RegionHandle)
        {
            m_localService.Post(request, RegionHandle);
            m_remoteService.Post(request, RegionHandle);
        }

        public OSDMap Get(OSDMap request, ulong RegionHandle)
        {
            OSDMap get = m_localService.Get(request, RegionHandle);
            if (get == null)
                get = m_remoteService.Get(request, RegionHandle);
            return get;
        }

        #endregion
    }
}
