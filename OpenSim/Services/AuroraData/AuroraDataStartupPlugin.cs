using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using log4net;
using Nini.Config;
using Aurora.Services.DataService;
using Aurora.Simulation.Base;

namespace OpenSim.CoreApplicationPlugins
{
    public class AuroraDataStartupPlugin : IService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            LocalDataService service = new LocalDataService();
            service.Initialise(config, registry);
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}
