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
        private bool m_loaded = false;
        public void Initialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            if (serviceConnectors == null)
            {
                serviceConnectors = AuroraModuleLoader.PickupModules<IService>();
                foreach (IService connector in serviceConnectors)
                {
                    try
                    {
                        connector.Initialize(openSimBase.ConfigSource, openSimBase.ApplicationRegistry);
                    }
                    catch
                    {
                    }
                }
                foreach (IService connector in serviceConnectors)
                {
                    try
                    {
                        connector.PostInitialize(openSimBase.ConfigSource, openSimBase.ApplicationRegistry);
                    }
                    catch
                    {
                    }
                }
            }
            scene.AddModuleInterfaces(openSimBase.ApplicationRegistry.GetInterfaces());
        }

        public void PostInitialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            if (!m_loaded)
            {
                m_loaded = true;
                foreach (IService connector in serviceConnectors)
                {
                    try
                    {
                        connector.Start(openSimBase.ConfigSource, scene);
                    }
                    catch
                    {
                    }
                }
            }
        }

        public void FinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }
    }
}
