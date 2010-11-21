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
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Authentication;

namespace Aurora.Modules.Communications.MultipleGrids
{
    public class MultipleAuthenticationConnectorModule : ISharedRegionModule, IAuthenticationService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Enabled = false;
        private List<IAuthenticationService> AllServices = new List<IAuthenticationService>();

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "MultipleAuthenticationServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("AuthenticationServices", "");
                if (name == Name)
                {
                    IConfig multipleConfig = source.Configs["MultipleGridsModule"];
                    if (multipleConfig != null)
                    {
                        IConfig UAS = source.Configs["AuthenticationService"];
                        if (UAS != null)
                        {
                            string[] Grids = multipleConfig.GetString("AuthenticationServerURIs", "").Split(',');
                            //Set it so that it works for them
                            moduleConfig.Set("AuthenticationServices", "RemoteAuthenticationServicesConnector");
                            foreach (string gridURL in Grids)
                            {
                                //Set their gridURL
                                UAS.Set("AuthenticationServerURI", gridURL);
                                //Start it up
                                RemoteAuthenticationServicesConnector connector = new RemoteAuthenticationServicesConnector();
                                connector.Initialise(source);
                                AllServices.Add(connector);
                                m_log.Info("[AUTHENTICATION CONNECTOR]: Multiple authentication services enabled for " + gridURL);
                            }
                        }
                    }
                    //Reset the name
                    moduleConfig.Set("AuthenticationServices", Name);
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

            scene.RegisterModuleInterface<IAuthenticationService>(this);
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

        #region IAuthenticationService Members

        public string Authenticate(UUID principalID, string password, int lifetime)
        {
            string ret = "";
            foreach (IAuthenticationService service in AllServices)
            {
                ret = service.Authenticate(principalID, password, lifetime);
                UUID i;
                if (ret != "" && UUID.TryParse(ret, out i))
                    return ret;
            }
            return ret;
        }

        public bool Verify(UUID principalID, string token, int lifetime)
        {
            bool ret = false;
            foreach (IAuthenticationService service in AllServices)
            {
                ret = service.Verify(principalID, token, lifetime);
                if (ret)
                    return ret;
            }
            return ret;
        }

        public bool Release(UUID principalID, string token)
        {
            bool ret = true;
            foreach (IAuthenticationService service in AllServices)
            {
                ret = service.Release(principalID, token);
                if (!ret)
                    return ret;
            }
            return ret;
        }

        public bool SetPassword(UUID principalID, string passwd)
        {
            bool ret = false;
            foreach (IAuthenticationService service in AllServices)
            {
                ret = service.SetPassword(principalID, passwd);
                if (ret)
                    return ret;
            }
            return ret;
        }

        public bool SetPasswordHashed(UUID UUID, string passwd)
        {
            bool ret = false;
            foreach (IAuthenticationService service in AllServices)
            {
                ret = service.SetPasswordHashed(UUID, passwd);
                if (ret)
                    return ret;
            }
            return ret;
        }

        #endregion
    }
}
