using System.Collections.Generic;
using Aurora.Framework;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Services;
using Nini.Config;

namespace Aurora.Region
{
    public class SceneLoader : ISceneLoader, IApplicationPlugin
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

            bool enabled = true;
            if (m_openSimBase.ConfigSource.Configs["SceneLoader"] != null)
                enabled = m_openSimBase.ConfigSource.Configs["SceneLoader"].GetBoolean("SceneLoader", true);

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
            get { return "SceneLoader"; }
        }

        public IScene CreateScene(RegionInfo regionInfo)
        {
            return SetupScene(regionInfo, m_configSource);
        }

        #endregion

        /// <summary>
        ///     Create a scene and its initial base structures.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="configSource"></param>
        /// <returns></returns>
        protected IScene SetupScene(RegionInfo regionInfo, IConfigSource configSource)
        {
            AgentCircuitManager circuitManager = new AgentCircuitManager();
            List<IClientNetworkServer> clientServers = AuroraModuleLoader.PickupModules<IClientNetworkServer>();
            List<IClientNetworkServer> allClientServers = new List<IClientNetworkServer>();
            foreach (IClientNetworkServer clientServer in clientServers)
            {
                clientServer.Initialise(regionInfo.InternalEndPoint.Port, m_configSource, circuitManager);
                allClientServers.Add(clientServer);
            }

            Scene scene = new Scene();
            scene.AddModuleInterfaces(m_openSimBase.ApplicationRegistry.GetInterfaces());
            scene.Initialize(regionInfo, circuitManager, allClientServers);

            return scene;
        }
    }
}