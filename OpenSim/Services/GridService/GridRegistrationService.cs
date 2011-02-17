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

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterModuleInterface<IGridRegistrationService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        #endregion

        #region IGridRegistrationService Members

        public Dictionary<string, string> GetUrlForRegisteringClient(UUID SessionID)
        {
            Dictionary<string, string> retVal = new Dictionary<string, string>();
            foreach (IGridRegistrationUrlModule module in m_modules)
            {
                retVal[module.Name] = module.GetUrlForRegisteringClient(SessionID);
            }
            return retVal;
        }

        public void RegisterModule(IGridRegistrationUrlModule module)
        {
            m_modules.Add(module);
        }

        #endregion
    }
}
