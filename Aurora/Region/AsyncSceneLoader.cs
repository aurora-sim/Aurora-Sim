using System.Collections.Generic;
using Aurora.Framework;
using Aurora.Framework.ModuleLoader;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Services;
using Nini.Config;
using Aurora.Framework.Servers;

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

        /// <summary>
        ///     Create a scene and its initial base structures.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="configSource"></param>
        /// <returns></returns>
        public IScene CreateScene(ISimulationDataStore dataStore, RegionInfo regionInfo)
        {
            AgentCircuitManager circuitManager = new AgentCircuitManager();
            List<IClientNetworkServer> clientServers = AuroraModuleLoader.PickupModules<IClientNetworkServer>();
            List<IClientNetworkServer> allClientServers = new List<IClientNetworkServer>();
            foreach (IClientNetworkServer clientServer in clientServers)
            {
                clientServer.Initialise((uint)regionInfo.RegionPort, m_configSource, circuitManager);
                allClientServers.Add(clientServer);
            }

            AsyncScene scene = new AsyncScene();
            scene.AddModuleInterfaces(m_openSimBase.ApplicationRegistry.GetInterfaces());
            scene.Initialize(regionInfo, dataStore, circuitManager, allClientServers);

            return scene;
        }

        #endregion
    }
}