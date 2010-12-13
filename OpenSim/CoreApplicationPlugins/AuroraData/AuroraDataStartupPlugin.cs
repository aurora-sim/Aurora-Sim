using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using log4net;
using Aurora.Services.DataService;

namespace OpenSim.CoreApplicationPlugins
{
    public class AuroraDataStartupPlugin : IApplicationPlugin
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        ISimulationBase OpenSimBase;
        public void Initialize(ISimulationBase openSim)
        {
            OpenSimBase = openSim;
        }

        public void PostInitialise()
        {
            m_log.Debug("[AURORADATA]: Setting up the data service");
            LocalDataService service = new LocalDataService();
            service.Initialise(OpenSimBase);
        }

        public void Start()
        {
        }

        public void PostStart()
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
