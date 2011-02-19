using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;

namespace OpenSim.Services.GridService
{
    public class GridRegistrationService : IService, IGridRegistrationService
    {
        #region Declares

        protected Dictionary<string, IGridRegistrationUrlModule> m_modules = new Dictionary<string, IGridRegistrationUrlModule>();
        protected string m_hostName = "";
        protected IGenericsConnector m_genericsConnector;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterModuleInterface<IGridRegistrationService>(this);
            m_hostName = config.Configs["Configuration"].GetString("HostName", m_hostName);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_genericsConnector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
        }

        public void FinishedStartup()
        {
            LoadFromDatabase();
        }

        protected void LoadFromDatabase()
        {
            List<GridRegistrationURLs> urls = m_genericsConnector.GetGenerics<GridRegistrationURLs>(
                UUID.Zero, "GridRegistrationUrls", new GridRegistrationURLs());

            foreach (GridRegistrationURLs url in urls)
            {
                foreach (IGridRegistrationUrlModule module in m_modules.Values)
                {
                    module.AddExistingUrlForClient(url.SessionID, url.RegionHandle, url.URLS[module.UrlName]);
                }
            }
        }

        #endregion

        #region IGridRegistrationService Members

        public OSDMap GetUrlForRegisteringClient(UUID SessionID, ulong RegionHandle)
        {
            OSDMap databaseSave = new OSDMap();
            //Get the URLs from all the modules that have registered with us
            foreach (IGridRegistrationUrlModule module in m_modules.Values)
            {
                //Build the URL
                databaseSave[module.UrlName] = module.GetUrlForRegisteringClient(SessionID, RegionHandle);
            }
            OSDMap retVal = new OSDMap();
            foreach (KeyValuePair<string, OSD> module in databaseSave)
            {
                //Build the URL
                retVal[module.Key] = m_hostName + ":" + m_modules[module.Key].Port + module.Value.AsString();
            }

            //Save into the database so that we can rebuild later if the server goes offline
            GridRegistrationURLs urls = new GridRegistrationURLs();
            urls.URLS = databaseSave;
            urls.RegionHandle = RegionHandle;
            urls.SessionID = SessionID;
            m_genericsConnector.AddGeneric(UUID.Zero, "GridRegistrationUrls", RegionHandle.ToString(), urls.ToOSD());

            return retVal;
        }

        public void RegisterModule(IGridRegistrationUrlModule module)
        {
            //Add the module to our list
            m_modules.Add(module.UrlName, module);
        }

        #endregion

        #region Classes

        public class GridRegistrationURLs : IDataTransferable
        {
            public OSDMap URLS;
            public UUID SessionID;
            public ulong RegionHandle;

            public override OSDMap ToOSD()
            {
                OSDMap retVal = new OSDMap();
                retVal["URLS"] = URLS;
                retVal["SessionID"] = SessionID;
                retVal["RegionHandle"] = RegionHandle;
                return retVal;
            }

            public override void FromOSD(OSDMap retVal)
            {
                URLS = (OSDMap)retVal["URLS"];
                SessionID = retVal["SessionID"].AsUUID();
                RegionHandle = retVal["RegionHandle"].AsULong();
            }

            public override IDataTransferable Duplicate()
            {
                GridRegistrationURLs url = new GridRegistrationURLs();
                url.FromOSD(ToOSD());
                return url;
            }
        }

        #endregion
    }
}
