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
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Presence;

namespace Aurora.Modules.Communications.MultipleGrids
{
    public class MultiplePresenceServicesConnector : ISharedRegionModule, IPresenceService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Enabled = false;
        private List<IPresenceService> AllServices = new List<IPresenceService>();

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "MultiplePresenceServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("PresenceServices", "");
                if (name == Name)
                {
                    IConfig multipleConfig = source.Configs["MultipleGridsModule"];
                    if (multipleConfig != null)
                    {
                        IConfig UAS = source.Configs["PresenceService"];
                        if (UAS != null)
                        {
                            string[] Grids = multipleConfig.GetString("PresenceServerURIs", "").Split(',');
                            //Set it so that it works for them
                            moduleConfig.Set("PresenceServices", "RemotePresenceServicesConnector");
                            foreach (string gridURL in Grids)
                            {
                                //Set their gridURL
                                UAS.Set("PresenceServerURI", gridURL);
                                //Start it up
                                RemotePresenceServicesConnector connector = new RemotePresenceServicesConnector();
                                connector.Initialise(source);
                                AllServices.Add(connector);
                                m_log.Info("[PRESENCE CONNECTOR]: Multiple presence services enabled for " + gridURL);
                            }
                        }
                    }
                    //Reset the name
                    moduleConfig.Set("PresenceServices", Name);
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

            scene.RegisterModuleInterface<IPresenceService>(this);
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

        #region IPresenceService Members

        public bool LoginAgent(string userID, UUID sessionID, UUID secureSessionID)
        {
            bool RetVal = false;
            foreach (IPresenceService service in AllServices)
            {
                if (!RetVal)
                    RetVal = service.LoginAgent(userID, sessionID, secureSessionID);
                else
                    service.LoginAgent(userID, sessionID, secureSessionID);
            }
            return true;
        }

        public bool LogoutAgent(UUID sessionID)
        {
            bool RetVal = false;
            foreach (IPresenceService service in AllServices)
            {
                if (!RetVal)
                    RetVal = service.LogoutAgent(sessionID);
                else
                    service.LogoutAgent(sessionID);
            }
            return true;
        }

        public bool LogoutRegionAgents(UUID regionID)
        {
            bool RetVal = false;
            foreach (IPresenceService service in AllServices)
            {
                if (!RetVal)
                    RetVal = service.LogoutRegionAgents(regionID);
                else
                    service.LogoutRegionAgents(regionID);
            }
            return true;
        }

        public void ReportAgent(UUID sessionID, UUID regionID)
        {
            foreach (IPresenceService service in AllServices)
            {
                service.ReportAgent(sessionID, regionID);
            }
        }

        public OpenSim.Services.Interfaces.PresenceInfo GetAgent(UUID sessionID)
        {
            OpenSim.Services.Interfaces.PresenceInfo RetVal = null;
            foreach (IPresenceService service in AllServices)
            {
                RetVal = service.GetAgent(sessionID);
                if (RetVal != null)
                    return RetVal;
            }
            return null;
        }

        public OpenSim.Services.Interfaces.PresenceInfo[] GetAgents(string[] userIDs)
        {
            List<OpenSim.Services.Interfaces.PresenceInfo> r = new List<OpenSim.Services.Interfaces.PresenceInfo>();
            foreach (IPresenceService service in AllServices)
            {
                r.AddRange(service.GetAgents(userIDs));
            }
            return r.ToArray();
        }

        public string[] GetAgentsLocations(string[] userIDs)
        {
            List<string> r = new List<string>();
            foreach (IPresenceService service in AllServices)
            {
                r.AddRange(service.GetAgentsLocations(userIDs));
            }
            return r.ToArray();
        }

        #endregion
    }
}
