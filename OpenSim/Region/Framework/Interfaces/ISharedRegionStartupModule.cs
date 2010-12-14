using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface ISharedRegionStartupModule
    {
        /// <summary>
        /// Initialise and load the configs of the module
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="source"></param>
        /// <param name="openSimBase"></param>
        void Initialise(Scene scene, IConfigSource source, ISimulationBase openSimBase);

        /// <summary>
        /// PostInitialise the module
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="source"></param>
        /// <param name="openSimBase"></param>
        void PostInitialise(Scene scene, IConfigSource source, ISimulationBase openSimBase);

        /// <summary>
        /// Do the functions of the module and set up any necessary functions
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="source"></param>
        /// <param name="openSimBase"></param>
        void FinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase);

        /// <summary>
        /// Close the module and remove all references to it
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="m_config"></param>
        /// <param name="m_OpenSimBase"></param>
        void Close(Scene scene);
    }
}
