using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using log4net;

namespace OpenSim.Region.CoreModules
{
    public class ServicesInitializer : ISharedRegionStartupModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<IService> serviceConnectors;
        private List<IServiceConnector> connectors;
        private RegistryCore m_serviceRegistry = null;
        
        public void Initialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            if (m_serviceRegistry == null)
            {
                m_serviceRegistry = new RegistryCore();
                m_serviceRegistry.RegisterInterface<ISimulationBase>(openSimBase);
                scene.RegisterInterface<ISimulationBase>(openSimBase);
                serviceConnectors = AuroraModuleLoader.PickupModules<IService>();
                connectors = AuroraModuleLoader.PickupModules<IServiceConnector>();
                foreach (IService connector in serviceConnectors)
                {
                    try
                    {
                        connector.Initialize(source, m_serviceRegistry);
                    }
                    catch
                    {
                    }
                }
            }

            scene.AddModuleInterfaces(m_serviceRegistry.GetInterfaces());
        }

        public void PostInitialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            foreach (IService connector in serviceConnectors)
            {
                try
                {
                    connector.PostInitialize(scene);
                }
                catch
                {
                }
            }
            foreach (IServiceConnector connector in connectors)
            {
                try
                {
                    connector.Initialize(source, openSimBase, "", scene);
                }
                catch
                {
                }
            }
        }
    }
}
