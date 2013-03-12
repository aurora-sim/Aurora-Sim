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
            server.AddStreamHandler(new ServerHandler("/regionmanagement", m_registry, this));
        }

        public void Close()
        {
        }

        public void ReloadConfiguration(IConfigSource m_config)
        {
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword=true)]
        public bool GetWhetherRegionIsOnline(UUID regionID)
        {
            object remoteValue = InternalDoRemote(regionID);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            return true;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
        public void StartNewRegion(RegionInfo region)
        {
            InternalDoRemote(region);
            if (m_doRemoteOnly)
                return;

            region.NewRegion = true;
            Util.FireAndForget(delegate(object o)
            {
                _sceneManager.StartNewRegion(region);
            });
            region.NewRegion = false;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
        public void StartRegion(RegionInfo region)
        {
            InternalDoRemote(region);
            if (m_doRemoteOnly)
                return;

            Util.FireAndForget(delegate(object o)
            {
                _sceneManager.StartNewRegion(region);
            });
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
        public bool StopRegion(UUID regionID, int secondsBeforeShutdown)
        {
            object remoteValue = InternalDoRemote(regionID, secondsBeforeShutdown);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            _sceneManager.CloseRegion(secondsBeforeShutdown == 0 ? ShutdownType.Immediate : ShutdownType.Delayed, secondsBeforeShutdown);
            return true;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
        public void ResetRegion(UUID regionID)
        {
            InternalDoRemote(regionID);
            if (m_doRemoteOnly)
                return;

            _sceneManager.ResetRegion();
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
        public void DeleteRegion(UUID regionID)
        {
            InternalDoRemote(regionID);
            if (m_doRemoteOnly)
                return;

            _sceneManager.RemoveRegion(true);//Deletes the .abackup file, all prims in the region, and the info from all region loaders
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true, RenamedMethod = "GetRegionInfoByUUID")]
        public RegionInfo GetRegionInfo(UUID regionID)
        {
            object remoteValue = InternalDoRemote(regionID);
            if (remoteValue != null || m_doRemoteOnly)
                return (RegionInfo)remoteValue;

            return _sceneManager.Scene.RegionInfo;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
        public string GetOpenRegionSettingsHTMLPage(UUID regionID)
        {
            object remoteValue = InternalDoRemote(regionID);
            if (remoteValue != null || m_doRemoteOnly)
                return (string)remoteValue;

            return AddOpenRegionSettingsHTMLPage(_sceneManager.Scene);
        }

        #region ORS HTML

        public string AddOpenRegionSettingsHTMLPage(IScene scene)
        {
            Dictionary<string, object> vars = new Dictionary<string, object>();
            OpenRegionSettings settings = scene.RegionInfo.OpenRegionSettings;
            vars.Add("Default Draw Distance", settings.DefaultDrawDistance.ToString());
            vars.Add("Force Draw Distance", settings.ForceDrawDistance ? "checked" : "");
            vars.Add("Max Drag Distance", settings.MaxDragDistance.ToString());
            vars.Add("Max Prim Scale", settings.MaximumPrimScale.ToString());
            vars.Add("Min Prim Scale", settings.MinimumPrimScale.ToString());
            vars.Add("Max Physical Prim Scale", settings.MaximumPhysPrimScale.ToString());
            vars.Add("Max Hollow Size", settings.MaximumHollowSize.ToString());
            vars.Add("Min Hole Size", settings.MinimumHoleSize.ToString());
            vars.Add("Max Link Count", settings.MaximumLinkCount.ToString());
            vars.Add("Max Link Count Phys", settings.MaximumLinkCountPhys.ToString());
            vars.Add("Max Inventory Items To Transfer", settings.MaximumInventoryItemsTransfer.ToString());
            vars.Add("Terrain Scale", settings.TerrainDetailScale.ToString());
            vars.Add("Show Tags", settings.ShowTags.ToString());
            vars.Add("Render Water", settings.RenderWater ? "checked" : "");
            vars.Add("Allow Minimap", settings.DisplayMinimap ? "checked" : "");
            vars.Add("Allow Physical Prims", settings.AllowPhysicalPrims ? "checked" : "");
            vars.Add("Enable Teen Mode", settings.EnableTeenMode ? "checked" : "");
            vars.Add("Enforce Max Build Constraints", settings.ClampPrimSizes ? "checked" : "");
            string HTMLPage = "";
            string path = Util.BasePathCombine(System.IO.Path.Combine("data", "OpenRegionSettingsPage.html"));
            if (System.IO.File.Exists(path))
                HTMLPage = System.IO.File.ReadAllText(path);
            return CSHTMLCreator.AddHTMLPage(HTMLPage, "", "OpenRegionSettings", vars, (newVars) =>
            {
                ParseUpdatedList(scene, newVars);
                return AddOpenRegionSettingsHTMLPage(scene);
            });
        }

        private void ParseUpdatedList(IScene scene, Dictionary<string, string> vars)
        {
            OpenRegionSettings settings = scene.RegionInfo.OpenRegionSettings;
            settings.DefaultDrawDistance = floatParse(vars["Default Draw Distance"]);
            settings.ForceDrawDistance = vars["Force Draw Distance"] != null;
            settings.MaxDragDistance = floatParse(vars["Max Drag Distance"]);
            settings.MaximumPrimScale = floatParse(vars["Max Prim Scale"]);
            settings.MinimumPrimScale = floatParse(vars["Min Prim Scale"]);
            settings.MaximumPhysPrimScale = floatParse(vars["Max Physical Prim Scale"]);
            settings.MaximumHollowSize = floatParse(vars["Max Hollow Size"]);
            settings.MinimumHoleSize = floatParse(vars["Min Hole Size"]);
            settings.MaximumLinkCount = (int)floatParse(vars["Max Link Count"]);
            settings.MaximumLinkCountPhys = (int)floatParse(vars["Max Link Count Phys"]);
            settings.MaximumInventoryItemsTransfer = (int)floatParse(vars["Max Inventory Items To Transfer"]);
            settings.TerrainDetailScale = floatParse(vars["Terrain Scale"]);
            settings.ShowTags = (int)floatParse(vars["Show Tags"]);
            settings.RenderWater = vars["Render Water"] != null;
            settings.DisplayMinimap = vars["Allow Minimap"] != null;
            settings.AllowPhysicalPrims = vars["Allow Physical Prims"] != null;
            settings.EnableTeenMode = vars["Enable Teen Mode"] != null;
            settings.ClampPrimSizes = vars["Enforce Max Build Constraints"] != null;
            scene.RegionInfo.OpenRegionSettings = settings;
        }

        private float floatParse(string p)
        {
            float d = 0;
            if (!float.TryParse(p, out d))
                d = 0;
            return d;
        }

        #endregion

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
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

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
        public List<UserAccount> GetUserAccounts(string name)
        {
            object remoteValue = InternalDoRemote(name);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<UserAccount>)remoteValue;

            return m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccounts(null, name);
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
        public void CreateUser(string name, string password, string email, UUID userID, UUID scopeID)
        {
            InternalDoRemote(name);
            if (m_doRemoteOnly)
                return;

            m_registry.RequestModuleInterface<IUserAccountService>().CreateUser(userID, 
                scopeID, name, Util.Md5Hash(password), email);
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
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

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true, RenamedMethod="CreateNewEstateWithInformation")]
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

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
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

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
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

        [CanBeReflected(ThreatLevel = ThreatLevel.None, UsePassword = true)]
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
                return DoRemote(_url, o);
        }
    }
}
