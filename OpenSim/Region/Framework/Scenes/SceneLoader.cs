using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using OpenSim.Framework;
using Nini.Config;
using Aurora.Framework;

namespace OpenSim.Region.Framework.Scenes
{
    public class SceneLoader : ISceneLoader, IApplicationPlugin
    {
        private IConfigSource m_configSource;
        private ISimulationBase m_openSimBase;

        public string Name
        {
            get { return "SceneLoader"; }
        }

        public void Initialize (ISimulationBase openSim)
        {
            m_openSimBase = openSim;
            m_configSource = openSim.ConfigSource;

            bool enabled = true;
            if (m_openSimBase.ConfigSource.Configs["SceneLoader"] != null)
                enabled = m_openSimBase.ConfigSource.Configs["SceneLoader"].GetBoolean ("Enabled", true);

            if (enabled)
                m_openSimBase.ApplicationRegistry.RegisterModuleInterface<ISceneLoader> (this);
        }

        public void PostInitialise ()
        {
        }

        public void Start ()
        {
        }

        public void PostStart ()
        {
        }

        public void Close ()
        {
        }

        public void ReloadConfiguration (IConfigSource m_config)
        {
        }

        public IScene CreateScene (RegionInfo regionInfo)
        {
            return SetupScene (regionInfo, m_configSource);
        }

        /// <summary>
        /// Create a scene and its initial base structures.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="proxyOffset"></param>
        /// <param name="configSource"></param>
        /// <param name="clientServer"> </param>
        /// <returns></returns>
        protected Scene SetupScene (RegionInfo regionInfo, IConfigSource configSource)
        {
            AgentCircuitManager circuitManager = new AgentCircuitManager ();
            List<IClientNetworkServer> clientServers = AuroraModuleLoader.PickupModules<IClientNetworkServer> ();
            List<IClientNetworkServer> allClientServers = new List<IClientNetworkServer> ();
            foreach (IClientNetworkServer clientServer in clientServers)
            {
                foreach (int port in regionInfo.UDPPorts)
                {
                    IClientNetworkServer copy = clientServer.Copy ();
                    copy.Initialise (port, m_configSource, circuitManager);
                    allClientServers.Add (copy);
                }
            }

            Scene scene = new Scene ();
            scene.AddModuleInterfaces (m_openSimBase.ApplicationRegistry.GetInterfaces ());
            scene.Initialize (regionInfo, circuitManager, allClientServers);

            return scene;
        }
    }
}
