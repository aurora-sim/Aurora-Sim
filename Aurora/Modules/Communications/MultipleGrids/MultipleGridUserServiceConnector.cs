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
using OpenSim.Region.CoreModules.ServiceConnectorsOut.GridUser;

namespace Aurora.Modules.Communications.MultipleGrids
{
    public class MultipleGridUserServicesConnector : ISharedRegionModule, IGridUserService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Enabled = false;
        private List<IGridUserService> AllServices = new List<IGridUserService>();

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "MultipleGridUserServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("GridUserServices", "");
                if (name == Name)
                {
                    IConfig multipleConfig = source.Configs["MultipleGridsModule"];
                    if (multipleConfig != null)
                    {
                        IConfig UAS = source.Configs["GridUserService"];
                        if (UAS != null)
                        {
                            string[] Grids = multipleConfig.GetString("GridUserServerURIs", "").Split(',');
                            //Set it so that it works for them
                            moduleConfig.Set("GridUserServices", "RemoteGridUserServicesConnector");
                            foreach (string gridURL in Grids)
                            {
                                //Set their gridURL
                                UAS.Set("GridUserServerURI", gridURL);
                                //Start it up
                                RemoteGridUserServicesConnector connector = new RemoteGridUserServicesConnector();
                                connector.Initialise(source);
                                AllServices.Add(connector);
                                m_log.Info("[GRID USER CONNECTOR]: Multiple grid user services enabled for " + gridURL);
                            }
                        }
                    }
                    //Reset the name
                    moduleConfig.Set("GridUserServices", Name);
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

            scene.RegisterModuleInterface<IGridUserService>(this);
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

        #region IGridUserService Members

        public GridUserInfo LoggedIn(string userID)
        {
            GridUserInfo info = null;
            foreach (IGridUserService service in AllServices)
            {
                info = service.LoggedIn(userID);
                if (info != null)
                    return info;
            }
            return info;
        }

        public bool LoggedOut(string userID, UUID sessionID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            bool r = false;
            foreach (IGridUserService service in AllServices)
            {
                r = service.LoggedOut(userID, sessionID, regionID, lastPosition, lastLookAt);
                if (r)
                    return r;
            }
            return r;
        }

        public bool SetHome(string userID, UUID homeID, Vector3 homePosition, Vector3 homeLookAt)
        {
            bool r = false;
            foreach (IGridUserService service in AllServices)
            {
                r = service.SetHome(userID, homeID, homePosition, homeLookAt);
                if (r)
                    return r;
            }
            return r;
        }

        public void SetLastPosition(string userID, UUID sessionID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            foreach (IGridUserService service in AllServices)
            {
                service.SetLastPosition(userID, sessionID, regionID, lastPosition, lastLookAt);
            }
        }

        public GridUserInfo GetGridUserInfo(string userID)
        {
            GridUserInfo info = null;
            foreach (IGridUserService service in AllServices)
            {
                info = service.GetGridUserInfo(userID);
                if (info != null)
                    return info;
            }
            return info;
        }

        #endregion
    }
}
