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
    public class AuroraDataStartupPlugin : IApplicationPlugin
    {
        #region IApplicationPlugin Members

        public void Initialize(ISimulationBase openSim)
        {
            LocalDataService service = new LocalDataService();
            service.Initialise(openSim.ConfigSource, openSim.ApplicationRegistry);
        }

        public void PostInitialise()
        {
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

        public void ReloadConfiguration(IConfigSource m_config)
        {
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
