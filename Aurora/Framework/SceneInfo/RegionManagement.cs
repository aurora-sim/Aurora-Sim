using System;
using System.Collections.Generic;
using System.IO;
using Aurora.Framework;
using OpenSim.Services.Interfaces;
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
        private bool m_enabled = false;

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
                    m_enabled = true;
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
            if (!m_enabled) return;
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

            IScene scene;
            if (_sceneManager.TryGetScene(regionID, out scene))
                _sceneManager.RemoveRegion(scene, true);//Deletes the .abackup file, all prims in the region, and the info from all region loaders
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
        public List<string> GetEstatesForUser(string name)
        {
            object remoteValue = InternalDoRemote(name);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<string>)remoteValue;

            List<string> estateItems = new List<string>();
            IEstateConnector conn = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector>();
            if (conn != null)
            {
                UserAccount account = m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(null, name);
                if (account != null)
                {
                    List<EstateSettings> estates = conn.GetEstates(account.PrincipalID);
                    foreach (var es in estates)
                        estateItems.Add(es.EstateName);
                }
            }
            return estateItems;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public List<UserAccount> GetUserAccounts(string name)
        {
            object remoteValue = InternalDoRemote(name);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<UserAccount>)remoteValue;

            return m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccounts(null, name);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public void CreateUser(string name, string password, string email, UUID userID, UUID scopeID)
        {
            InternalDoRemote(name);
            if (m_doRemoteOnly)
                return;

            m_registry.RequestModuleInterface<IUserAccountService>().CreateUser(userID, 
                scopeID, name, Util.Md5Hash(password), email);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public void ChangeEstate(string ownerName, string estateToJoin, UUID regionID)
        {
            InternalDoRemote(ownerName, estateToJoin, regionID);
            if (m_doRemoteOnly)
                return;

            IEstateConnector conn = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector>();
            if (conn != null)
            {
                conn.DelinkRegion(regionID);
                UserAccount account = m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(null, ownerName);
                conn.LinkRegion(regionID, conn.GetEstate(account.PrincipalID, estateToJoin));
            }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true, RenamedMethod="CreateNewEstateWithInformation")]
        public bool CreateNewEstate(UUID regionID, string estateName, string ownerName)
        {
            object remoteValue = InternalDoRemote(regionID, estateName, ownerName);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            IEstateConnector conn = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector>();
            if (conn != null)
            {
                var account = m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(null, ownerName);
                if (account != null)
                {
                    UUID userID = account.PrincipalID;
                    conn.DelinkRegion(regionID);

                    return conn.CreateNewEstate(new EstateSettings() { EstateName = estateName, EstateOwner = userID }, regionID) != 0;
                }
            }
            return false;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public string GetCurrentEstate(UUID regionID)
        {
            object remoteValue = InternalDoRemote(regionID);
            if (remoteValue != null || m_doRemoteOnly)
                return (string)remoteValue;

            IEstateConnector conn = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector>();
            if (conn != null)
            {
                EstateSettings es = conn.GetEstateSettings(regionID);
                if (es == null || es.EstateID == 0)
                    return "";
                else
                    return es.EstateName;
            }
            return "";
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public string GetEstateOwnerName(UUID regionID)
        {
            object remoteValue = InternalDoRemote(regionID);
            if (remoteValue != null || m_doRemoteOnly)
                return (string)remoteValue;

            IEstateConnector conn = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector>();
            if (conn != null)
            {
                EstateSettings es = conn.GetEstateSettings(regionID);
                if (es == null || es.EstateID == 0)
                    return "";
                else
                    return m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(null, es.EstateOwner).Name;
            }
            return "";
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
