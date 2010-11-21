using System;
using System.Collections;
using System.Collections.Generic;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Caps = OpenSim.Framework.Capabilities.Caps;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OSDArray = OpenMetaverse.StructuredData.OSDArray;
using OSDMap = OpenMetaverse.StructuredData.OSDMap;
using Aurora.Framework;

namespace Aurora.OpenRegionSettingsModule
{
    /// <summary>
    /// This module sends Aurora-specific settings to the viewer to tell it about different settings for the region
    /// </summary>
    #region Settings

    public class OpenRegionSettings : IDataTransferable
    {
        #region Declares

        private float m_MaxDragDistance = -1;
        private float m_DefaultDrawDistance = -1;

        private float m_MaximumPrimScale = -1;
        private float m_MinimumPrimScale = -1;
        private float m_MaximumPhysPrimScale = -1;

        private float m_MaximumHollowSize = -1;
        private float m_MinimumHoleSize = -1;

        private int m_MaximumLinkCount = -1;
        private int m_MaximumLinkCountPhys = -1;

        private float m_WhisperDistance = 10;
        private float m_SayDistance = 30;
        private float m_ShoutDistance = 100;

        private OSDArray m_LSLCommands = new OSDArray();

        private int m_MaximumInventoryItemsTransfer = -1;
        private bool m_DisplayMinimap = true;
        private bool m_RenderWater = true;
        private bool m_AllowPhysicalPrims = true;
        private bool m_ClampPrimSizes = true;
        private bool m_ForceDrawDistance = true;
        private bool m_OffsetOfUTCDST = false;
        private string m_OffsetOfUTC = "SLT";
        private bool m_EnableTeenMode = false;
        public UUID m_DefaultUnderpants = UUID.Zero;
        public UUID m_DefaultUndershirt = UUID.Zero;
        private int m_ShowTags = 2; //Show always
        private int m_MaxGroups = -1;
        private bool m_AllowParcelWindLight = true;
        private bool m_SetTeenMode = false;

        #endregion

        #region Public properties

        public float MaxDragDistance
        {
            get
            {
                return m_MaxDragDistance;
            }
            set { m_MaxDragDistance = value; }
        }

        public float DefaultDrawDistance
        {
            get { return m_DefaultDrawDistance; }
            set { m_DefaultDrawDistance = value; }
        }

        public float MaximumPrimScale
        {
            get
            {
                if (m_ClampPrimSizes)
                    return m_MaximumPrimScale;
                else
                    return float.MaxValue;
            }
            set { m_MaximumPrimScale = value; }
        }

        public float MinimumPrimScale
        {
            get
            {
                if (m_ClampPrimSizes)
                    return m_MinimumPrimScale;
                else
                    return 0;
            }
            set { m_MinimumPrimScale = value; }
        }

        public float MaximumPhysPrimScale
        {
            get
            {
                if (m_ClampPrimSizes)
                    return m_MaximumPhysPrimScale;
                else
                    return float.MaxValue;
            }
            set { m_MaximumPhysPrimScale = value; }
        }

        public float MaximumHollowSize
        {
            get { return m_MaximumHollowSize; }
            set { m_MaximumHollowSize = value; }
        }

        public float MinimumHoleSize
        {
            get { return m_MinimumHoleSize; }
            set { m_MinimumHoleSize = value; }
        }

        public int MaximumLinkCount
        {
            get
            {
                return m_MaximumLinkCount;
            }
            set { m_MaximumLinkCount = value; }
        }

        public int MaximumLinkCountPhys
        {
            get
            {
                return m_MaximumLinkCountPhys;
            }
            set { m_MaximumLinkCountPhys = value; }
        }

        public OSDArray LSLCommands
        {
            get { return m_LSLCommands; }
            set { m_LSLCommands = value; }
        }

        public float WhisperDistance
        {
            get { return m_WhisperDistance; }
            set { m_WhisperDistance = value; }
        }

        public float SayDistance
        {
            get { return m_SayDistance; }
            set { m_SayDistance = value; }
        }

        public float ShoutDistance
        {
            get { return m_ShoutDistance; }
            set { m_ShoutDistance = value; }
        }

        public bool RenderWater
        {
            get { return m_RenderWater; }
            set { m_RenderWater = value; }
        }

        public int MaximumInventoryItemsTransfer
        {
            get
            {
                return m_MaximumInventoryItemsTransfer;
            }
            set { m_MaximumInventoryItemsTransfer = value; }
        }

        public bool DisplayMinimap
        {
            get { return m_DisplayMinimap; }
            set { m_DisplayMinimap = value; }
        }

        public bool AllowPhysicalPrims
        {
            get { return m_AllowPhysicalPrims; }
            set { m_AllowPhysicalPrims = value; }
        }

        public string OffsetOfUTC
        {
            get { return m_OffsetOfUTC; }
            set { m_OffsetOfUTC = value; }
        }

        public bool OffsetOfUTCDST
        {
            get { return m_OffsetOfUTCDST; }
            set { m_OffsetOfUTCDST = value; }
        }

        public bool EnableTeenMode
        {
            get { return m_EnableTeenMode; }
            set { m_EnableTeenMode = value; }
        }

        public bool SetTeenMode
        {
            get { return m_SetTeenMode; }
            set { m_SetTeenMode = value; }
        }

        public UUID DefaultUnderpants
        {
            get { return m_DefaultUnderpants; }
            set { m_DefaultUnderpants = value; }
        }

        public UUID DefaultUndershirt
        {
            get { return m_DefaultUndershirt; }
            set { m_DefaultUndershirt = value; }
        }

        public bool ClampPrimSizes
        {
            get { return m_ClampPrimSizes; }
            set { m_ClampPrimSizes = value; }
        }

        public bool ForceDrawDistance
        {
            get { return m_ForceDrawDistance; }
            set { m_ForceDrawDistance = value; }
        }

        public int ShowTags
        {
            get { return m_ShowTags; }
            set { m_ShowTags = value; }
        }

        public int MaxGroups
        {
            get { return m_MaxGroups; }
            set { m_MaxGroups = value; }
        }

        public bool AllowParcelWindLight
        {
            get { return m_AllowParcelWindLight; }
            set { m_AllowParcelWindLight = value; }
        }

        #endregion

        #region IDataTransferable

        public override void FromOSD(OSDMap rm)
        {
            MaxDragDistance = (float)rm["MaxDragDistance"].AsReal();
            ForceDrawDistance = rm["ForceDrawDistance"].AsInteger() == 1;
            MaximumPrimScale = (float)rm["MaxPrimScale"].AsReal();
            MinimumPrimScale = (float)rm["MinPrimScale"].AsReal();
            MaximumPhysPrimScale = (float)rm["MaxPhysPrimScale"].AsReal();
            MaximumHollowSize = (float)rm["MaxHollowSize"].AsReal();
            MinimumHoleSize = (float)rm["MinHoleSize"].AsReal();
            ClampPrimSizes = rm["EnforceMaxBuild"].AsInteger() == 1;
            MaximumLinkCount = rm["MaxLinkCount"].AsInteger();
            MaximumLinkCountPhys = rm["MaxLinkCountPhys"].AsInteger();
            MaxDragDistance = (float)rm["MaxDragDistance"].AsReal();
            RenderWater = rm["RenderWater"].AsInteger() == 1;
            MaximumInventoryItemsTransfer = rm["MaxInventoryItemsTransfer"].AsInteger();
            DisplayMinimap = rm["AllowMinimap"].AsInteger() == 1;
            AllowPhysicalPrims = rm["AllowPhysicalPrims"].AsInteger() == 1;
            OffsetOfUTC = rm["OffsetOfUTC"].AsString();
            OffsetOfUTCDST = rm["OffsetOfUTCDST"].AsInteger() == 1;
            EnableTeenMode = rm["ToggleTeenMode"].AsInteger() == 1;
            SetTeenMode = rm["SetTeenMode"].AsInteger() == 1;
            ShowTags = rm["ShowTags"].AsInteger();
            MaxGroups = rm["MaxGroups"].AsInteger();
            AllowParcelWindLight = rm["AllowParcelWindLight"].AsInteger() == 1;
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            return Util.OSDToDictionary(ToOSD());
        }

        public override IDataTransferable Duplicate() { return new OpenRegionSettings(); }

        public override OSDMap ToOSD()
        {
            OSDMap body = new OSDMap();
            body.Add("MaxDragDistance", OSD.FromReal(MaxDragDistance));

            body.Add("DrawDistance", OSD.FromReal(DefaultDrawDistance));
            body.Add("ForceDrawDistance", OSD.FromInteger(ForceDrawDistance ? 1 : 0));

            body.Add("MaxPrimScale", OSD.FromReal(MaximumPrimScale));
            body.Add("MinPrimScale", OSD.FromReal(MinimumPrimScale));
            body.Add("MaxPhysPrimScale", OSD.FromReal(MaximumPhysPrimScale));

            body.Add("MaxHollowSize", OSD.FromReal(MaximumHollowSize));
            body.Add("MinHoleSize", OSD.FromReal(MinimumHoleSize));
            body.Add("EnforceMaxBuild", OSD.FromInteger(ClampPrimSizes ? 1 : 0));

            body.Add("MaxLinkCount", OSD.FromInteger(MaximumLinkCount));
            body.Add("MaxLinkCountPhys", OSD.FromInteger(MaximumLinkCountPhys));

            body.Add("LSLFunctions", LSLCommands);

            body.Add("RenderWater", OSD.FromInteger(RenderWater ? 1 : 0));

            body.Add("MaxInventoryItemsTransfer", OSD.FromInteger(MaximumInventoryItemsTransfer));

            body.Add("AllowMinimap", OSD.FromInteger(DisplayMinimap ? 1 : 0));
            body.Add("AllowPhysicalPrims", OSD.FromInteger(AllowPhysicalPrims ? 1 : 0));
            body.Add("OffsetOfUTC", OSD.FromString(OffsetOfUTC));
            body.Add("OffsetOfUTCDST", OSD.FromInteger(OffsetOfUTCDST ? 1 : 0));
            body.Add("ToggleTeenMode", OSD.FromInteger(EnableTeenMode ? 1 : 0));
            body.Add("SetTeenMode", OSD.FromInteger(SetTeenMode ? 1 : 0));

            body.Add("ShowTags", OSD.FromInteger(ShowTags));
            body.Add("MaxGroups", OSD.FromInteger(MaxGroups));
            body.Add("AllowParcelWindLight", OSD.FromInteger(AllowParcelWindLight ? 1 : 0));

            return body;
        }

        #endregion
    }

    #endregion

    #region Module

    public class OpenRegionSettingsModule : INonSharedRegionModule, IOpenRegionSettingsModule
    {
        #region IOpenRegionSettingsModule

        public float MaxDragDistance
        {
            get
            {
                return m_settings.MaxDragDistance;
            }
            set { m_settings.MaxDragDistance = value; }
        }

        public float DefaultDrawDistance
        {
            get { return m_settings.DefaultDrawDistance; }
            set { m_settings.DefaultDrawDistance = value; }
        }

        public float MaximumPrimScale
        {
            get
            {
                return m_settings.MaximumPrimScale;
            }
            set { m_settings.MaximumPrimScale = value; }
        }

        public float MinimumPrimScale
        {
            get
            {
                return m_settings.MinimumPrimScale;
            }
            set { m_settings.MinimumPrimScale = value; }
        }

        public float MaximumPhysPrimScale
        {
            get
            {
                return m_settings.MaximumPhysPrimScale;
            }
            set { m_settings.MaximumPhysPrimScale = value; }
        }

        public float MaximumHollowSize
        {
            get { return m_settings.MaximumHollowSize; }
            set { m_settings.MaximumHollowSize = value; }
        }

        public float MinimumHoleSize
        {
            get { return m_settings.MinimumHoleSize; }
            set { m_settings.MinimumHoleSize = value; }
        }

        public int MaximumLinkCount
        {
            get
            {
                return m_settings.MaximumLinkCount;
            }
            set { m_settings.MaximumLinkCount = value; }
        }

        public int MaximumLinkCountPhys
        {
            get
            {
                return m_settings.MaximumLinkCountPhys;
            }
            set { m_settings.MaximumLinkCountPhys = value; }
        }

        public OSDArray LSLCommands
        {
            get { return m_settings.LSLCommands; }
            set { m_settings.LSLCommands = value; }
        }

        public float WhisperDistance
        {
            get { return m_settings.WhisperDistance; }
            set { m_settings.WhisperDistance = value; }
        }

        public float SayDistance
        {
            get { return m_settings.SayDistance; }
            set { m_settings.SayDistance = value; }
        }

        public float ShoutDistance
        {
            get { return m_settings.ShoutDistance; }
            set { m_settings.ShoutDistance = value; }
        }

        public bool RenderWater
        {
            get { return m_settings.RenderWater; }
            set { m_settings.RenderWater = value; }
        }

        public int MaximumInventoryItemsTransfer
        {
            get
            {
                return m_settings.MaximumInventoryItemsTransfer;
            }
            set { m_settings.MaximumInventoryItemsTransfer = value; }
        }

        public bool DisplayMinimap
        {
            get { return m_settings.DisplayMinimap; }
            set { m_settings.DisplayMinimap = value; }
        }

        public bool AllowPhysicalPrims
        {
            get { return m_settings.AllowPhysicalPrims; }
            set { m_settings.AllowPhysicalPrims = value; }
        }

        public string OffsetOfUTC
        {
            get { return m_settings.OffsetOfUTC; }
            set { m_settings.OffsetOfUTC = value; }
        }

        public bool OffsetOfUTCDST
        {
            get { return m_settings.OffsetOfUTCDST; }
            set { m_settings.OffsetOfUTCDST = value; }
        }

        public bool EnableTeenMode
        {
            get { return m_settings.EnableTeenMode; }
            set { m_settings.EnableTeenMode = value; }
        }

        public bool SetTeenMode
        {
            get { return m_settings.SetTeenMode; }
            set { m_settings.SetTeenMode = value; }
        }

        public UUID DefaultUnderpants
        {
            get { return m_settings.DefaultUnderpants; }
            set { m_settings.DefaultUnderpants = value; }
        }

        public UUID DefaultUndershirt
        {
            get { return m_settings.DefaultUndershirt; }
            set { m_settings.DefaultUndershirt = value; }
        }

        public bool ClampPrimSizes
        {
            get { return m_settings.ClampPrimSizes; }
            set { m_settings.ClampPrimSizes = value; }
        }

        public bool ForceDrawDistance
        {
            get { return m_settings.ForceDrawDistance; }
            set { m_settings.ForceDrawDistance = value; }
        }

        public int ShowTags
        {
            get { return m_settings.ShowTags; }
            set { m_settings.ShowTags = value; }
        }

        public int MaxGroups
        {
            get { return m_settings.MaxGroups; }
            set { m_settings.MaxGroups = value; }
        }

        public bool AllowParcelWindLight
        {
            get { return m_settings.AllowParcelWindLight; }
            set { m_settings.AllowParcelWindLight = value; }
        }

        public void RegisterGenericValue(string key, string value)
        {
            additionalKVPs.Add(key, value);
        }

        #endregion

        #region Declares

        private IConfigSource m_source;
        private Scene m_scene;
        private OpenRegionSettings m_settings = null;

        //Generic KVP's to send
        private Dictionary<string, string> additionalKVPs = new Dictionary<string, string>();

        #endregion

        #region INonSharedRegionModule

        public void Initialise(IConfigSource source)
        {
            m_source = source;
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;
            scene.EventManager.OnMakeRootAgent += OnNewClient;
            scene.EventManager.OnRegisterCaps += OnRegisterCaps;
            scene.RegisterModuleInterface<IOpenRegionSettingsModule>(this);
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            if (connector != null)
            {
                m_settings = connector.GetGeneric<OpenRegionSettings>(scene.RegionInfo.RegionID, "OpenRegionSettings", "OpenRegionSettings", new OpenRegionSettings());
                if (m_settings == null)
                    m_settings = new OpenRegionSettings();
            }
            ReadConfig(scene);
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
            IChatModule chatmodule = scene.RequestModuleInterface<IChatModule>();
            if (chatmodule != null)
            {
                //Set default chat ranges
                m_settings.WhisperDistance = chatmodule.WhisperDistance;
                m_settings.SayDistance = chatmodule.SayDistance;
                m_settings.ShoutDistance = chatmodule.ShoutDistance;
            }
            IScriptModule scriptmodule = scene.RequestModuleInterface<IScriptModule>();
            if (scriptmodule != null)
            {
                List<string> FunctionNames = scriptmodule.GetAllFunctionNames();
                foreach (string FunctionName in FunctionNames)
                {
                    m_settings.LSLCommands.Add(OSD.FromString(FunctionName));
                }
            }
        }

        public string Name
        {
            get { return "WorldSettingsModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        #region CAPS

        public void OnRegisterCaps(UUID agentID, Caps caps)
        {
            UUID capuuid = UUID.Random();
            
            //Sets the OpenRegionSettings
            caps.RegisterHandler("DispatchOpenRegionSettings",
                                new RestHTTPHandler("POST", "/CAPS/" + capuuid + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return DispatchOpenRegionSettings(m_dhttpMethod, capuuid, agentID);
                                                      }));
        }

        private Hashtable DispatchOpenRegionSettings(Hashtable m_dhttpMethod, UUID capuuid, UUID agentID)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";

            ScenePresence SP = m_scene.GetScenePresence(agentID);
            if (SP == null)
                return responsedata; //They don't exist

            if (!SP.Scene.Permissions.CanIssueEstateCommand(SP.UUID, false))
                return responsedata; // No permissions

            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)m_dhttpMethod["requestbody"]);

            m_settings.DefaultDrawDistance = rm["draw_distance"].AsInteger();
            m_settings.ForceDrawDistance = rm["force_draw_distance"].AsBoolean();
            m_settings.DisplayMinimap = rm["allow_minimap"].AsBoolean();
            m_settings.AllowPhysicalPrims = rm["allow_physical_prims"].AsBoolean();
            m_settings.MaxDragDistance = (float)rm["max_drag_distance"].AsReal();
            m_settings.MinimumHoleSize = (float)rm["min_hole_size"].AsReal();
            m_settings.MaximumHollowSize = (float)rm["max_hollow_size"].AsReal();
            m_settings.MaximumInventoryItemsTransfer = rm["max_inventory_items_transfer"].AsInteger();
            m_settings.MaximumLinkCount = (int)rm["max_link_count"].AsReal();
            m_settings.MaximumLinkCountPhys = (int)rm["max_link_count_phys"].AsReal();
            m_settings.MaximumPhysPrimScale = (float)rm["max_phys_prim_scale"].AsReal();
            m_settings.MaximumPrimScale = (float)rm["max_prim_scale"].AsReal();
            m_settings.MinimumPrimScale = (float)rm["min_prim_scale"].AsReal();
            m_settings.RenderWater = rm["render_water"].AsBoolean();
            m_settings.ShowTags = (int)rm["show_tags"].AsReal();
            m_settings.MaxGroups = (int)rm["max_groups"].AsReal();
            m_settings.AllowParcelWindLight = rm["allow_parcel_windlight"].AsBoolean();
            m_settings.EnableTeenMode = rm["enable_teen_mode"].AsBoolean();
            m_settings.ClampPrimSizes = rm["enforce_max_build"].AsBoolean();

            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            //Update the database
            if (connector != null)
            {
                connector.AddGeneric(SP.Scene.RegionInfo.RegionID, "OpenRegionSettings", "OpenRegionSettings", m_settings.ToOSD());
            }

            //Update all clients about changes
            SendToAllClients();

            return responsedata;
        }

        #endregion

        #region Setup

        private void ReadConfig(Scene scene)
        {
            //Set up the instance first
            IConfig instanceSettings = m_source.Configs["InstanceSettings"];
            if (instanceSettings != null)
            {
                ReadSettings(instanceSettings);
            }
            IConfig regionSettings = m_source.Configs[scene.RegionInfo.RegionName];
            if (regionSettings != null)
            {
                ReadSettings(regionSettings);
            }
        }

        private void ReadSettings(IConfig instanceSettings)
        {
            m_settings.MaxDragDistance = instanceSettings.GetFloat("MaxDragDistance", MaxDragDistance);
            m_settings.DefaultDrawDistance = instanceSettings.GetFloat("DefaultDrawDistance", DefaultDrawDistance);


            m_settings.MaximumPrimScale = instanceSettings.GetFloat("MaximumPrimScale", MaximumPrimScale);
            m_settings.MinimumPrimScale = instanceSettings.GetFloat("MinimumPrimScale", MinimumPrimScale);
            m_settings.MaximumPhysPrimScale = instanceSettings.GetFloat("MaximumPhysPrimScale", MaximumPhysPrimScale);


            m_settings.MaximumHollowSize = instanceSettings.GetFloat("MaximumHollowSize", MaximumHollowSize);
            m_settings.MinimumHoleSize = instanceSettings.GetFloat("MinimumHoleSize", MinimumHoleSize);


            m_settings.MaximumLinkCount = instanceSettings.GetInt("MaximumLinkCount", MaximumLinkCount);
            m_settings.MaximumLinkCountPhys = instanceSettings.GetInt("MaximumLinkCountPhys", MaximumLinkCountPhys);


            m_settings.RenderWater = instanceSettings.GetBoolean("RenderWater", RenderWater);
            m_settings.MaximumInventoryItemsTransfer = instanceSettings.GetInt("MaximumInventoryItemsTransfer", MaximumInventoryItemsTransfer);
            m_settings.DisplayMinimap = instanceSettings.GetBoolean("DisplayMinimap", DisplayMinimap);
            m_settings.AllowPhysicalPrims = instanceSettings.GetBoolean("AllowPhysicalPrims", AllowPhysicalPrims);
            m_settings.ClampPrimSizes = instanceSettings.GetBoolean("ClampPrimSizes", ClampPrimSizes);
            m_settings.ForceDrawDistance = instanceSettings.GetBoolean("ForceDrawDistance", ForceDrawDistance);

            m_settings.OffsetOfUTC = instanceSettings.GetString("OffsetOfUTC", OffsetOfUTC);
            m_settings.OffsetOfUTCDST = instanceSettings.GetBoolean("OffsetOfUTCDST", OffsetOfUTCDST);
            m_settings.EnableTeenMode = instanceSettings.GetBoolean("EnableTeenMode", EnableTeenMode);
            m_settings.ShowTags = instanceSettings.GetInt("ShowTags", ShowTags);
            m_settings.MaxGroups = instanceSettings.GetInt("MaxGroups", MaxGroups);
            m_settings.AllowParcelWindLight = instanceSettings.GetBoolean("AllowParcelWindLight", AllowParcelWindLight);
            
            string defaultunderpants = instanceSettings.GetString("DefaultUnderpants", m_settings.DefaultUnderpants.ToString());
                UUID.TryParse(defaultunderpants, out m_settings.m_DefaultUnderpants);
            string defaultundershirt = instanceSettings.GetString("DefaultUndershirt", m_settings.DefaultUndershirt.ToString());
                UUID.TryParse(defaultundershirt, out m_settings.m_DefaultUndershirt);
        }

        #endregion

        #region Client and Event Queue

        public void OnNewClient(ScenePresence presence)
        {
            OpenRegionInfo(presence);
        }

        public void SendToAllClients()
        {
            m_scene.ForEachScenePresence(delegate(ScenePresence SP)
            {
                OpenRegionInfo(SP);
            });
        }

        public void OpenRegionInfo(ScenePresence presence)
        {
            OSD item = OpenRegionInfo();
            IEventQueue eq = presence.Scene.RequestModuleInterface<IEventQueue>();
            if (eq != null)
                eq.Enqueue(item, presence.UUID);
        }

        public OSD OpenRegionInfo()
        {
            OSDMap map = new OSDMap();

            OSDMap body = new OSDMap();

            if (m_settings.MaxDragDistance != -1)
                body.Add("MaxDragDistance", OSD.FromReal(m_settings.MaxDragDistance));

            if (m_settings.DefaultDrawDistance != -1)
            {
                body.Add("DrawDistance", OSD.FromReal(m_settings.DefaultDrawDistance));
                body.Add("ForceDrawDistance", OSD.FromInteger(m_settings.ForceDrawDistance ? 1 : 0));
            }

            if (m_settings.MaximumPrimScale != -1)
                body.Add("MaxPrimScale", OSD.FromReal(m_settings.MaximumPrimScale));
            if (m_settings.MinimumPrimScale != -1)
                body.Add("MinPrimScale", OSD.FromReal(m_settings.MinimumPrimScale));
            if (m_settings.MaximumPhysPrimScale != -1)
                body.Add("MaxPhysPrimScale", OSD.FromReal(m_settings.MaximumPhysPrimScale));

            if (m_settings.MaximumHollowSize != -1)
                body.Add("MaxHollowSize", OSD.FromReal(m_settings.MaximumHollowSize));
            if (m_settings.MinimumHoleSize != -1)
                body.Add("MinHoleSize", OSD.FromReal(m_settings.MinimumHoleSize));
            body.Add("EnforceMaxBuild", OSD.FromInteger(m_settings.ClampPrimSizes ? 1 : 0));

            if (m_settings.MaximumLinkCount != -1)
                body.Add("MaxLinkCount", OSD.FromInteger(m_settings.MaximumLinkCount));
            if (m_settings.MaximumLinkCountPhys != -1)
                body.Add("MaxLinkCountPhys", OSD.FromInteger(m_settings.MaximumLinkCountPhys));

            body.Add("LSLFunctions", m_settings.LSLCommands);

            body.Add("WhisperDistance", OSD.FromReal(m_settings.WhisperDistance));
            body.Add("SayDistance", OSD.FromReal(m_settings.WhisperDistance));
            body.Add("ShoutDistance", OSD.FromReal(m_settings.WhisperDistance));

            body.Add("RenderWater", OSD.FromInteger(m_settings.RenderWater ? 1 : 0));

            if (m_settings.MaximumInventoryItemsTransfer != -1)
                body.Add("MaxInventoryItemsTransfer", OSD.FromInteger(m_settings.MaximumInventoryItemsTransfer));

            body.Add("AllowMinimap", OSD.FromInteger(m_settings.DisplayMinimap ? 1 : 0));
            body.Add("AllowPhysicalPrims", OSD.FromInteger(m_settings.AllowPhysicalPrims ? 1 : 0));
            body.Add("OffsetOfUTC", OSD.FromString(m_settings.OffsetOfUTC));
            body.Add("OffsetOfUTCDST", OSD.FromInteger(m_settings.OffsetOfUTCDST ? 1 : 0));
            body.Add("ToggleTeenMode", OSD.FromInteger(m_settings.EnableTeenMode ? 1 : 0));
            body.Add("SetTeenMode", OSD.FromInteger(m_settings.SetTeenMode ? 1 : 0));

            body.Add("ShowTags", OSD.FromInteger(m_settings.ShowTags));
            if (m_settings.MaxGroups != -1)
                body.Add("MaxGroups", OSD.FromInteger(m_settings.MaxGroups));
            body.Add("AllowParcelWindLight", OSD.FromInteger(m_settings.AllowParcelWindLight ? 1 : 0));

            //Add all the generic ones
            foreach (KeyValuePair<string, string> KVP in additionalKVPs)
            {
                body.Add(KVP.Key, OSD.FromString(KVP.Value));
            }

            map.Add("body", body);
            map.Add("message", OSD.FromString("OpenRegionInfo"));
            return map;
        }

        #endregion
    }

    #endregion
}