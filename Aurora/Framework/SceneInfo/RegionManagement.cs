using System;
using System.Collections.Generic;
using System.IO;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class RegionManagement : ConnectorBase, IRegionManagement, IApplicationPlugin
    {
        private ISceneManager _sceneManager;
        private IRegionInfoConnector _regionInfoConnector;
        private string _url = "";

        public RegionManagement() { }
        public RegionManagement(string url, string password)
        {
            _url = url;
            Init(null, GetType().Name, password);
            SetDoRemoteCalls(true);
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        public void PreStartup(ISimulationBase simBase)
        {
            m_registry = simBase.ApplicationRegistry;
            m_registry.RegisterModuleInterface<IRegionManagement>(this);
            IConfig config = simBase.ConfigSource.Configs["AuroraStartup"];
            if (config != null)
            {
                string password = config.GetString("RemoteAccessPassword", "");
                if (password != "")
                {
                    Init(m_registry, Name, password);
                    SetDoRemoteCalls(false);
                }
            }
        }

        public void Initialize(ISimulationBase simBase)
        {
            _regionInfoConnector = Aurora.DataManager.DataManager.RequestPlugin<IRegionInfoConnector>();
            _sceneManager = m_registry.RequestModuleInterface<ISceneManager>();
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
        }

        public void PostStart()
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(0);
            server.AddStreamHandler(new ServerHandler("/regionmanagement", "", m_registry));
        }

        public void Close()
        {
        }

        public void ReloadConfiguration(IConfigSource m_config)
        {
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword=true)]
        public bool GetWhetherRegionIsOnline(UUID regionID)
        {
            object remoteValue = InternalDoRemote(regionID);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            IScene scene;
            return _sceneManager.TryGetScene(regionID, out scene);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public void StartNewRegion(RegionInfo region)
        {
            InternalDoRemote(region);
            if (m_doRemoteOnly)
                return;

            _sceneManager.AllRegions++;
            region.NewRegion = true;
            Util.FireAndForget(delegate(object o)
            {
                _sceneManager.StartNewRegion(region);
            });
            region.NewRegion = false;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public void StartRegion(RegionInfo region)
        {
            InternalDoRemote(region);
            if (m_doRemoteOnly)
                return;

            _sceneManager.AllRegions++;
            Util.FireAndForget(delegate(object o)
            {
                _sceneManager.StartNewRegion(region);
            });
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public bool StopRegion(UUID regionID)
        {
            object remoteValue = InternalDoRemote(regionID);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            IScene scene;
            if (_sceneManager.TryGetScene(regionID, out scene))
            {
                _sceneManager.AllRegions--;
                _sceneManager.CloseRegion(scene, ShutdownType.Immediate, 0);
                return true;
            }
            return false;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public void ResetRegion(UUID regionID)
        {
            InternalDoRemote(regionID);
            if (m_doRemoteOnly)
                return;

            IScene scene;
            if (_sceneManager.TryGetScene(regionID, out scene))
                _sceneManager.ResetRegion(scene);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public void DeleteRegion(UUID regionID)
        {
            InternalDoRemote(regionID);
            if (m_doRemoteOnly)
                return;

            _regionInfoConnector.Delete(GetRegionInfo(regionID));
            _sceneManager.DeleteRegion(regionID);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public List<RegionInfo> GetRegionInfos(bool nonDisabledOnly)
        {
            object remoteValue = InternalDoRemote(nonDisabledOnly);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<RegionInfo>)remoteValue;

            return new List<RegionInfo>(_regionInfoConnector.GetRegionInfos(nonDisabledOnly));
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public void UpdateRegionInfo(RegionInfo region)
        {
            InternalDoRemote(region);
            if (m_doRemoteOnly)
                return;

            _regionInfoConnector.UpdateRegionInfo(region);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true, RenamedMethod="GetRegionInfoByName")]
        public RegionInfo GetRegionInfo(string regionName)
        {
            object remoteValue = InternalDoRemote(regionName);
            if (remoteValue != null || m_doRemoteOnly)
                return (RegionInfo)remoteValue;

            return _regionInfoConnector.GetRegionInfo(regionName);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true, RenamedMethod = "GetRegionInfoByUUID")]
        public RegionInfo GetRegionInfo(UUID regionID)
        {
            object remoteValue = InternalDoRemote(regionID);
            if (remoteValue != null || m_doRemoteOnly)
                return (RegionInfo)remoteValue;

            return _regionInfoConnector.GetRegionInfo(regionID);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public string GetOpenRegionSettingsHTMLPage(UUID regionID)
        {
            object remoteValue = InternalDoRemote(regionID);
            if (remoteValue != null || m_doRemoteOnly)
                return (string)remoteValue;

            IOpenRegionSettingsConnector orsc = DataManager.DataManager.RequestPlugin<IOpenRegionSettingsConnector>();
            if (orsc != null)
                return orsc.AddOpenRegionSettingsHTMLPage(regionID);
            return "";
        }

        private string _defaultRegionsLocation = "DefaultRegions";
        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public List<string> GetDefaultRegionNames()
        {
            object remoteValue = InternalDoRemote();
            if (remoteValue != null || m_doRemoteOnly)
                return (List<string>)remoteValue;
            IConfig config = _sceneManager.ConfigSource.Configs["RegionManager"];
            if (config != null)
                _defaultRegionsLocation = config.GetString("DefaultRegionsLocation", _defaultRegionsLocation);

            if (!Directory.Exists(_defaultRegionsLocation))
                return new List<string>();

            return new List<string>(Directory.GetFiles(_defaultRegionsLocation, "*.abackup"));
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public System.Drawing.Image GetDefaultRegionImage(string name)
        {
            object remoteValue = InternalDoRemote(name);
            if (remoteValue != null || m_doRemoteOnly)
                return (System.Drawing.Image)remoteValue;

            IConfig config = _sceneManager.ConfigSource.Configs["RegionManager"];
            if (config != null)
                _defaultRegionsLocation = config.GetString("DefaultRegionsLocation", _defaultRegionsLocation);

            System.Drawing.Image b = null;
            if (File.Exists(Path.Combine(_defaultRegionsLocation, name + ".png")))
                b = System.Drawing.Image.FromFile(Path.Combine(_defaultRegionsLocation, name + ".png"));
            else if (File.Exists(Path.Combine(_defaultRegionsLocation, name + ".jpg")))
                b = System.Drawing.Image.FromFile(Path.Combine(_defaultRegionsLocation, name + ".jpg"));
            else if (File.Exists(Path.Combine(_defaultRegionsLocation, name + ".jpeg")))
                b = System.Drawing.Image.FromFile(Path.Combine(_defaultRegionsLocation, name + ".jpeg"));

            return b;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public bool MoveDefaultRegion(string regionName, string fileName, bool forced)
        {
            object remoteValue = InternalDoRemote(regionName, fileName, forced);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            string name = Path.Combine(_defaultRegionsLocation, fileName + ".abackup");//Full name
            if (!File.Exists(name))
                return true;//None selected, it can't be done, just don't do anything about it

            string loadAppenedFileName = "";
            string newFilePath = "";
            IConfig simData = _sceneManager.ConfigSource.Configs["FileBasedSimulationData"];
            if (simData != null)
            {
                loadAppenedFileName = simData.GetString("ApendedLoadFileName", loadAppenedFileName);
                newFilePath = simData.GetString("LoadBackupDirectory", newFilePath);
            }
            string newFileName = newFilePath == "" || newFilePath == "/" ?
                regionName + loadAppenedFileName + ".abackup" :
                Path.Combine(newFilePath, regionName + loadAppenedFileName + ".abackup");
            if (File.Exists(newFileName))
            {
                if (!forced)
                    return false;
                else
                    File.Delete(newFileName);
            }
            File.Copy(name, newFileName);
            return true;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public bool ConnectionIsWorking()
        {
            object remoteValue = InternalDoRemote();
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            return true;
        }

        [CanBeReflected(NotReflectableLookUpAnotherTrace=true)]
        private object InternalDoRemote(params object[] o)
        {
            if (_url == "")
                return DoRemote(o);
            else
                return DoRemoteByHTTP(_url, o);
        }
    }
}
