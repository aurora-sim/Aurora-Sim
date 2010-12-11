using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using log4net;
using Aurora.Services.DataService;

namespace OpenSim.ApplicationPlugins.RegionLoaderPlugin
{
    public class AuroraDataStartupPlugin : IApplicationPlugin
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        ISimulationBase OpenSimBase;
        public void Initialize(ISimulationBase openSim)
        {
            m_log.Info("[AURORADATA]: Setting up the data service");
            OpenSimBase = openSim;
            LocalDataService service = new LocalDataService();
            service.Initialise(openSim.ConfigSource);
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "AuroraDataStartupPlugin"; }
        }

        public void Dispose()
        {
        }
    }
}
