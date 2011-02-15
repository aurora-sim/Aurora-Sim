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
        public void Initialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            scene.AddModuleInterfaces(openSimBase.ApplicationRegistry.GetInterfaces());
        }

        public void PostInitialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void FinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void PostFinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void Close(Scene scene)
        {
        }

        public void StartupComplete()
        {
        }
    }
}
