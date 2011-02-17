using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using OpenMetaverse;

namespace OpenSim.Services.GridService
{
    public class GridRegistrationService : IService, IGridRegistrationService
    {
        #region Declares

        protected List<IGridRegistrationUrlModule> m_modules = new List<IGridRegistrationUrlModule>();
        protected string m_hostName = "";

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterModuleInterface<IGridRegistrationService>(this);
            m_hostName = config.Configs["Configuration"].GetString("HostName", m_hostName);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        #endregion

        #region IGridRegistrationService Members

        public Dictionary<string, string> GetUrlForRegisteringClient(UUID SessionID)
        {
            Dictionary<string, string> retVal = new Dictionary<string, string>();
            //Get the URLs from all the modules that have registered with us
            foreach (IGridRegistrationUrlModule module in m_modules)
            {
                //Build the URL
                retVal[module.UrlName] = m_hostName + ":" + module.Port + module.GetUrlForRegisteringClient(SessionID);
            }
            return retVal;
        }

        public void RegisterModule(IGridRegistrationUrlModule module)
        {
            //Add the module to our list
            m_modules.Add(module);
        }

        #endregion
    }
}
