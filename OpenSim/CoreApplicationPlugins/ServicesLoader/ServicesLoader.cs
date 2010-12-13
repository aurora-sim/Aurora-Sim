using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Framework;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;

namespace OpenSim.CoreApplicationPlugins
{
    public class ServicesLoader : IApplicationPlugin
    {
        private ISimulationBase m_openSim;
        public void Initialize(ISimulationBase openSim)
        {
            m_openSim = openSim;
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
            IConfig handlerConfig = m_openSim.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("ServicesLoader", "") != Name)
                return;

            List<IService> serviceConnectors = AuroraModuleLoader.PickupModules<IService>();
            foreach (IService connector in serviceConnectors)
            {
                try
                {
                    connector.Initialize(m_openSim.ConfigSource, m_openSim.ApplicationRegistry);
                }
                catch
                {
                }
            }
            foreach (IService connector in serviceConnectors)
            {
                try
                {
                    connector.PostInitialize(m_openSim.ConfigSource, m_openSim.ApplicationRegistry);
                }
                catch
                {
                }
            }
            foreach (IService connector in serviceConnectors)
            {
                try
                {
                    connector.Start(m_openSim.ConfigSource, m_openSim.ApplicationRegistry);
                }
                catch
                {
                }
            }
            foreach (IService connector in serviceConnectors)
            {
                try
                {
                    connector.PostStart(m_openSim.ConfigSource, m_openSim.ApplicationRegistry);
                }
                catch
                {
                }
            }
        }

        public void PostStart()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "ServicesLoader"; }
        }

        public void Dispose()
        {
        }
    }
}
