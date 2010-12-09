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
        public void Initialize(ISimulationBase openSim)
        {
            IConfig handlerConfig = openSim.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("ServicesLoader", "") != Name)
                return; 
            
            List<IService> serviceConnectors = AuroraModuleLoader.PickupModules<IService>();
            foreach (IService connector in serviceConnectors)
            {
                try
                {
                    connector.Initialize(openSim.ConfigSource, openSim.ApplicationRegistry);
                }
                catch
                {
                }
            }
            foreach (IService connector in serviceConnectors)
            {
                try
                {
                    connector.PostInitialize(openSim.ApplicationRegistry);
                }
                catch
                {
                }
            }

            List<IServiceConnector> connectors = AuroraModuleLoader.PickupModules<IServiceConnector>();
            foreach (IServiceConnector connector in connectors)
            {
                try
                {
                    connector.Initialize(openSim.ConfigSource, openSim, "", openSim.ApplicationRegistry);
                }
                catch
                {
                }
            }
        }

        public void PostInitialise()
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
