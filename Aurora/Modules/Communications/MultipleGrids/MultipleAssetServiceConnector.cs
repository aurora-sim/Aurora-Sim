using System;
using System.Collections.Generic;
using Nini.Config;
using log4net;
using System.Reflection;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Region.CoreModules.ServiceConnectorsOut;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Asset;

namespace Aurora.Modules.Communications.MultipleGrids
{
    public class MultipleAssetServicesConnector : ISharedRegionModule, IAssetService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Enabled = false;
        private List<IAssetService> AllServices = new List<IAssetService>();

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "MultipleAssetServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("AssetServices", "");
                if (name == Name)
                {
                    IConfig multipleConfig = source.Configs["MultipleGridsModule"];
                    if (multipleConfig != null)
                    {
                        IConfig UAS = source.Configs["AssetService"];
                        if (UAS != null)
                        {
                            string[] Grids = multipleConfig.GetString("AssetServerURIs", "").Split(',');
                            //Set it so that it works for them
                            moduleConfig.Set("AssetServices", "RemoteAssetServicesConnector");
                            foreach (string gridURL in Grids)
                            {
                                //Set their gridURL
                                UAS.Set("AssetServerURI", gridURL);
                                //Start it up
                                RemoteAssetServicesConnector connector = new RemoteAssetServicesConnector();
                                connector.Initialise(source);
                                AllServices.Add(connector);
                                m_log.Info("[ASSET CONNECTOR]: Multiple asset services enabled for " + gridURL);
                            }
                        }
                    }
                    //Reset the name
                    moduleConfig.Set("AssetServices", Name);
                    m_Enabled = true;
                }
            }
        }

        public void PostInitialise()
        {
            if (!m_Enabled)
                return;
        }

        public void Close()
        {
            if (!m_Enabled)
                return;
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            scene.RegisterModuleInterface<IAssetService>(this);
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;
        }

        #region IAssetService Members

        public OpenSim.Framework.AssetBase Get(string id)
        {
            OpenSim.Framework.AssetBase r = null;
            foreach (IAssetService service in AllServices)
            {
                r = service.Get(id);
                if (r != null)
                    return r;
            }
            return r;
        }

        public OpenSim.Framework.AssetMetadata GetMetadata(string id)
        {
            OpenSim.Framework.AssetMetadata r = null;
            foreach (IAssetService service in AllServices)
            {
                r = service.GetMetadata(id);
                if (r != null)
                    return r;
            }
            return r;
        }

        public byte[] GetData(string id)
        {
            byte[] r = null;
            foreach (IAssetService service in AllServices)
            {
                r = service.GetData(id);
                if (r != null && r != Utils.EmptyBytes)
                    return r;
            }
            return r;
        }

        public OpenSim.Framework.AssetBase GetCached(string id)
        {
            OpenSim.Framework.AssetBase r = null;
            foreach (IAssetService service in AllServices)
            {
                r = service.GetCached(id);
                if (r != null)
                    return r;
            }
            return r;
        }

        public bool Get(string id, object sender, AssetRetrieved handler)
        {
            bool r = false;
            foreach (IAssetService service in AllServices)
            {
                r = service.Get(id, sender, handler);
                if (r)
                    return r;
            }
            return r;
        }

        public string Store(OpenSim.Framework.AssetBase asset)
        {
            string r = "";
            foreach (IAssetService service in AllServices)
            {
                r += service.Store(asset);
            }
            return r;
        }

        public bool UpdateContent(string id, byte[] data)
        {
            bool r = false;
            foreach (IAssetService service in AllServices)
            {
                r = service.UpdateContent(id, data);
                if (r)
                    return r;
            }
            return r;
        }

        public bool Delete(string id)
        {
            bool r = false;
            foreach (IAssetService service in AllServices)
            {
                r = service.Delete(id);
                if (r)
                    return r;
            }
            return r;
        }

        #endregion
    }
}
