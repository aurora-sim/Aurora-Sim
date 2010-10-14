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
using Mono.Addins;

namespace Aurora.OpenRegionSettingsModule
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class OpenRegionSettingsModule : INonSharedRegionModule, IOpenRegionSettingsModule
    {
        #region Declares

        private IConfigSource m_source;
        private Scene m_scene;

        private Vector3 m_MinimumPosition = new Vector3(0, 0, 0);
        private Vector3 m_MaximumPosition = new Vector3(100000, 100000, 4096);

        private float m_MaxDragDistance = 0;
        private float m_DefaultDrawDistance = 0;

        private float m_MaximumPrimScale = 256;
        private float m_MinimumPrimScale = 0.001F;
        private float m_MaximumPhysPrimScale = 10;

        private float m_MaximumHollowSize = 100;
        private float m_MinimumHoleSize = 0.01f;

        private int m_MaximumLinkCount = 0;
        private int m_MaximumLinkCountPhys = 0;

        private float m_WhisperDistance = 10;
        private float m_SayDistance = 30;
        private float m_ShoutDistance = 100;

        private OSDArray m_LSLCommands = new OSDArray();

        private int m_MaximumInventoryItemsTransfer = 0;
        private bool m_DisplayMinimap = true;
        private bool m_RenderWater = true;
        private bool m_AllowPhysicalPrims = true;
        private bool m_ClampPrimSizes = true;
        private bool m_ClampPrimPositions = true;
        private bool m_ForceDrawDistance = true;
        private bool m_OffsetOfUTCDST = false;
        private string m_OffsetOfUTC = "SLT";
        private bool m_EnableTeenMode = false;
        private UUID m_DefaultUnderpants = UUID.Zero;
        private UUID m_DefaultUndershirt = UUID.Zero;
        private int m_ShowTags = 2; //Show always
        private int m_MaxGroups = -1;
        private bool m_AllowParcelWindLight = true;
        private bool m_SetTeenMode = false;

        #endregion

        #region Public properties

        public Vector3 MinimumPosition
        {
            get
            {
                return m_MinimumPosition;
            }
            set { m_MinimumPosition = value; }
        }

        public Vector3 MaximumPosition
        {
            get
            {
                return m_MaximumPosition;
            }
            set { m_MaximumPosition = value; }
        }

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
                return m_MaximumPrimScale;
            }
            set { m_MaximumPrimScale = value; }
        }

        public float MinimumPrimScale
        {
            get 
            {
                return m_MinimumPrimScale;
            }
            set { m_MinimumPrimScale = value; }
        }

        public float MaximumPhysPrimScale
        {
            get 
            {
                return m_MaximumPhysPrimScale;
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
            ReadConfig(scene);
            scene.EventManager.OnMakeRootAgent += OnNewClient;
            scene.EventManager.OnRegisterCaps += OnRegisterCaps;
            scene.RegisterModuleInterface<IOpenRegionSettingsModule>(this);
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
                WhisperDistance = chatmodule.WhisperDistance;
                SayDistance = chatmodule.SayDistance;
                ShoutDistance = chatmodule.ShoutDistance;
            }
            IScriptModule scriptmodule = scene.RequestModuleInterface<IScriptModule>();
            if (scriptmodule != null)
            {
                List<string> FunctionNames = scriptmodule.GetAllFunctionNames();
                foreach (string FunctionName in FunctionNames)
                {
                    LSLCommands.Add(OSD.FromString(FunctionName));
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

        public void OnRegisterCaps(UUID agentID, Caps caps)
        {
            UUID capuuid = UUID.Random();

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

            DefaultDrawDistance = rm["draw_distance"].AsInteger();
            ForceDrawDistance = rm["force_draw_distance"].AsBoolean();
            DisplayMinimap = rm["allow_minimap"].AsBoolean();
            AllowPhysicalPrims = rm["allow_physical_prims"].AsBoolean();
            MaxDragDistance = (float)rm["max_drag_distance"].AsReal();
            MinimumHoleSize = (float)rm["min_hole_size"].AsReal();
            MaximumHollowSize = (float)rm["max_hollow_size"].AsReal();
            MaximumInventoryItemsTransfer = rm["max_inventory_items_transfer"].AsInteger();
            MaximumLinkCount = (int)rm["max_link_count"].AsReal();
            MaximumLinkCountPhys = (int)rm["max_link_count_phys"].AsReal();
            MaximumPhysPrimScale = (float)rm["max_phys_prim_scale"].AsReal();
            MaximumPrimScale = (float)rm["max_prim_scale"].AsReal();
            MinimumPrimScale = (float)rm["min_prim_scale"].AsReal();
            RenderWater = rm["render_water"].AsBoolean();
            ShowTags = (int)rm["show_tags"].AsReal();
            MaxGroups = (int)rm["max_groups"].AsReal();
            AllowParcelWindLight = rm["allow_parcel_windlight"].AsBoolean();
            EnableTeenMode = rm["enable_teen_mode"].AsBoolean();
            ClampPrimSizes = rm["enforce_max_build"].AsBoolean();
            
            //TODO Save this

            //Update all clients about changes
            SendToAllClients();

            return responsedata;
        }

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
            m_MinimumPosition.X = instanceSettings.GetFloat("MinimumPositionX", MinimumPosition.X);
            m_MinimumPosition.Y = instanceSettings.GetFloat("MinimumPositionY", MinimumPosition.Y);
            m_MinimumPosition.Z = instanceSettings.GetFloat("MinimumPositionZ", MinimumPosition.Z);


            m_MaximumPosition.X = instanceSettings.GetFloat("MaximumPositionX", MaximumPosition.X);
            m_MaximumPosition.Y = instanceSettings.GetFloat("MaximumPositionY", MaximumPosition.Y);
            m_MaximumPosition.Z = instanceSettings.GetFloat("MaximumPositionZ", MaximumPosition.Z);

            m_MaxDragDistance = instanceSettings.GetFloat("MaxDragDistance", MaxDragDistance);
            m_DefaultDrawDistance = instanceSettings.GetFloat("DefaultDrawDistance", DefaultDrawDistance);


            m_MaximumPrimScale = instanceSettings.GetFloat("MaximumPrimScale", MaximumPrimScale);
            m_MinimumPrimScale = instanceSettings.GetFloat("MinimumPrimScale", MinimumPrimScale);
            m_MaximumPhysPrimScale = instanceSettings.GetFloat("MaximumPhysPrimScale", MaximumPhysPrimScale);


            m_MaximumHollowSize = instanceSettings.GetFloat("MaximumHollowSize", MaximumHollowSize);
            m_MinimumHoleSize = instanceSettings.GetFloat("MinimumHoleSize", MinimumHoleSize);


            m_MaximumLinkCount = instanceSettings.GetInt("MaximumLinkCount", MaximumLinkCount);
            m_MaximumLinkCountPhys = instanceSettings.GetInt("MaximumLinkCountPhys", MaximumLinkCountPhys);


            m_RenderWater = instanceSettings.GetBoolean("RenderWater", RenderWater);
            m_MaximumInventoryItemsTransfer = instanceSettings.GetInt("MaximumInventoryItemsTransfer", MaximumInventoryItemsTransfer);
            m_DisplayMinimap = instanceSettings.GetBoolean("DisplayMinimap", DisplayMinimap);
            m_AllowPhysicalPrims = instanceSettings.GetBoolean("AllowPhysicalPrims", AllowPhysicalPrims);
            m_ClampPrimSizes = instanceSettings.GetBoolean("ClampPrimSizes", m_ClampPrimSizes);
            m_ClampPrimPositions = instanceSettings.GetBoolean("ClampPrimPositions", m_ClampPrimPositions);
            m_ForceDrawDistance = instanceSettings.GetBoolean("ForceDrawDistance", m_ForceDrawDistance);

            m_OffsetOfUTC = instanceSettings.GetString("OffsetOfUTC", OffsetOfUTC);
            m_OffsetOfUTCDST = instanceSettings.GetBoolean("OffsetOfUTCDST", OffsetOfUTCDST);
            m_EnableTeenMode = instanceSettings.GetBoolean("EnableTeenMode", m_EnableTeenMode);
            string defaultunderpants = instanceSettings.GetString("DefaultUnderpants", DefaultUnderpants.ToString());
            UUID.TryParse(defaultunderpants, out m_DefaultUnderpants);
            string defaultundershirt = instanceSettings.GetString("DefaultUndershirt", DefaultUndershirt.ToString());
            UUID.TryParse(defaultundershirt, out m_DefaultUndershirt);
            m_ShowTags = instanceSettings.GetInt("ShowTags", ShowTags);
            m_MaxGroups = instanceSettings.GetInt("MaxGroups", MaxGroups);
            m_AllowParcelWindLight = instanceSettings.GetBoolean("AllowParcelWindLight", AllowParcelWindLight);
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
            body.Add("MinPosX", OSD.FromReal(MinimumPosition.X));
            body.Add("MinPosY", OSD.FromReal(MinimumPosition.Y));
            body.Add("MinPosZ", OSD.FromReal(MinimumPosition.Z));
            body.Add("MaxPosX", OSD.FromReal(MaximumPosition.X));
            body.Add("MaxPosY", OSD.FromReal(MaximumPosition.Y));
            body.Add("MaxPosZ", OSD.FromReal(MaximumPosition.Z));

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

            body.Add("WhisperDistance", OSD.FromReal(WhisperDistance));
            body.Add("SayDistance", OSD.FromReal(WhisperDistance));
            body.Add("ShoutDistance", OSD.FromReal(WhisperDistance));

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

            map.Add("body", body);
            map.Add("message", OSD.FromString("OpenRegionInfo"));
            return map;
        }

        #endregion
    }
}
