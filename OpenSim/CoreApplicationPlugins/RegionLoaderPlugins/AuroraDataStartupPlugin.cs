using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using log4net;

namespace OpenSim.ApplicationPlugins.RegionLoaderPlugin
{
    public class AuroraDataStartupPlugin : IApplicationPlugin
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        OpenSim.Framework.ISimulationBase OpenSimBase;
        public void Initialize(OpenSim.Framework.ISimulationBase openSim)
        {
            m_log.Info("[AURORADATA]: Setting up the data service");
            OpenSimBase = openSim;
            Aurora.Services.DataService.LocalDataService service = new Aurora.Services.DataService.LocalDataService();
            service.Initialise(openSim.ConfigSource);
        }

        public void PostInitialise()
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
