using System.Collections.Generic;
using Aurora.Framework;
using Nini.Config;

namespace Aurora.Region
{
    public class AsyncSceneLoader : ISceneLoader, IApplicationPlugin
    {
        private IConfigSource m_configSource;
        private ISimulationBase m_openSimBase;

        #region IApplicationPlugin Members

        public void PreStartup(ISimulationBase simBase)
        {
        }

        public void Initialize(ISimulationBase openSim)
        {
            m_openSimBase = openSim;
            m_configSource = openSim.ConfigSource;

            bool enabled = false;
            if (m_openSimBase.ConfigSource.Configs["SceneLoader"] != null)
                enabled = m_openSimBase.ConfigSource.Configs["SceneLoader"].GetBoolean("AsyncSceneLoader", false);

            if (enabled)
                m_openSimBase.ApplicationRegistry.RegisterModuleInterface<ISceneLoader>(this);
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

        #endregion

        #region ISceneLoader Members

        public string Name
        {
            get { return "AsyncSceneLoader"; }
        }

        public IScene CreateScene(RegionInfo regionInfo)
        {
            return SetupScene(regionInfo, m_configSource);
        }

        #endregion

        /// <summary>
        ///   Create a scene and its initial base structures.
        /// </summary>
        /// <param name = "regionInfo"></param>
        /// <param name = "proxyOffset"></param>
        /// <param name = "configSource"></param>
        /// <param name = "clientServer"> </param>
        /// <returns></returns>
        protected IScene SetupScene(RegionInfo regionInfo, IConfigSource configSource)
        {
            AgentCircuitManager circuitManager = new AgentCircuitManager();
            List<IClientNetworkServer> clientServers = AuroraModuleLoader.PickupModules<IClientNetworkServer>();
            List<IClientNetworkServer> allClientServers = new List<IClientNetworkServer>();
            foreach (IClientNetworkServer clientServer in clientServers)
            {
                foreach (int port in regionInfo.UDPPorts)
                {
                    IClientNetworkServer copy = clientServer.Copy();
                    copy.Initialise(port, m_configSource, circuitManager);
                    allClientServers.Add(copy);
                }
            }

            AsyncScene scene = new AsyncScene();
            scene.AddModuleInterfaces(m_openSimBase.ApplicationRegistry.GetInterfaces());
            scene.Initialize(regionInfo, circuitManager, allClientServers);

            return scene;
        }
    }
}