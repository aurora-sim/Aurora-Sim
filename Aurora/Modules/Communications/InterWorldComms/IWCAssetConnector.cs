using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Services.Connectors;
using OpenSim.Services.AssetService;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules 
{
    public class IWCAssetConnector : IAssetService, IService
    {
        protected AssetService m_localService;
        protected AssetServicesConnector m_remoteService;
        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public IAssetService InnerService
        {
            get { return m_localService; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AssetHandler", "") != Name)
                return;

            m_localService = new AssetService();
            m_localService.Configure(config, registry);
            m_remoteService = new AssetServicesConnector();
            m_remoteService.Initialize(config, registry);
            registry.RegisterModuleInterface<IAssetService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_localService != null)
                m_localService.Start(config, registry);
        }

        public void FinishedStartup()
        {
            if (m_localService != null)
                m_localService.FinishedStartup();
        }

        #endregion

        #region IAssetService Members

        public AssetBase Get(string id)
        {
            AssetBase asset = m_localService.Get(id);
            if (asset == null)
                asset = m_remoteService.Get(id);
            return asset;
        }

        public AssetMetadata GetMetadata(string id)
        {
            AssetMetadata asset = m_localService.GetMetadata(id);
            if (asset == null)
                asset = m_remoteService.GetMetadata(id);
            return asset;
        }

        public bool GetExists(string id)
        {
            bool exists = m_localService.GetExists(id);
            if (!exists)
                exists = m_remoteService.GetExists(id);
            return exists;
        }

        public byte[] GetData(string id)
        {
            byte[] asset = m_localService.GetData(id);
            if (asset == null)
                asset = m_remoteService.GetData(id);
            return asset;
        }

        public AssetBase GetCached(string id)
        {
            AssetBase asset = m_localService.GetCached(id);
            if (asset == null)
                asset = m_remoteService.GetCached(id);
            return asset;
        }

        public bool Get(string id, object sender, AssetRetrieved handler)
        {
            bool asset = m_localService.Get(id, sender, handler);
            if (!asset)
                asset = m_remoteService.Get(id, sender, handler);
            return asset;
        }

        public string Store(AssetBase asset)
        {
            string retVal = m_localService.Store(asset);
            //m_remoteService.Store(asset);
            return retVal;
        }

        public bool UpdateContent(string id, byte[] data)
        {
            bool asset = m_localService.UpdateContent(id, data);
            if (!asset)
                asset = m_remoteService.UpdateContent(id, data);
            return asset;
        }

        public bool Delete(string id)
        {
            bool asset = m_localService.Delete(id);
            if (!asset)
                asset = m_remoteService.Delete(id);
            return asset;
        }

        #endregion
    }
}
