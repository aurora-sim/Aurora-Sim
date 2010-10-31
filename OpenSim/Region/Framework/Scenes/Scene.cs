/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyrightD
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using System.Xml;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenMetaverse.Imaging;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes.Serialization;
using OpenSim.Region.Physics.Manager;
using Timer=System.Timers.Timer;
using TPFlags = OpenSim.Framework.Constants.TeleportFlags;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes.Animation;

namespace OpenSim.Region.Framework.Scenes
{
    public partial class Scene : SceneBase
    {
        private const long DEFAULT_MIN_TIME_FOR_PERSISTENCE = 60L;
        private const long DEFAULT_MAX_TIME_FOR_PERSISTENCE = 600L;

        public delegate void SynchronizeSceneHandler(Scene scene);

        #region Fields

        public SynchronizeSceneHandler SynchronizeScene;
        public SimStatsReporter StatsReporter;
        private AvatarAnimations m_defaultAnimations = null;

        protected List<RegionInfo> m_regionRestartNotifyList = new List<RegionInfo>();
        protected List<RegionInfo> m_neighbours = new List<RegionInfo>();

        protected List<UUID> m_needsDeleted = new List<UUID>();
        public List<SceneObjectGroup> PhysicsReturns = new List<SceneObjectGroup>();

        private volatile int m_bordersLocked = 0;

        public List<Border> NorthBorders = new List<Border>();
        public List<Border> EastBorders = new List<Border>();
        public List<Border> SouthBorders = new List<Border>();
        public List<Border> WestBorders = new List<Border>();

        /// <value>
        /// The scene graph for this scene
        /// </value>
        private SceneGraph m_sceneGraph;

        public bool m_seeIntoRegionFromNeighbor;
        public bool m_strictAccessControl = true;
        public int MaxUndoCount = 5;
        private int m_RestartTimerCounter;
        private readonly Timer m_restartTimer = new Timer(15000); // Wait before firing
        private int m_incrementsof15seconds;
        private volatile bool m_backingup;
        private Dictionary<UUID, ReturnInfo> m_returns = new Dictionary<UUID, ReturnInfo>();
        private Dictionary<UUID, SceneObjectGroup> m_groupsWithTargets = new Dictionary<UUID, SceneObjectGroup>();
        private Object m_heartbeatLock = new Object();
        protected IConfigSource m_config;
        

        protected AgentCircuitManager m_authenticateHandler;

        protected UUID d = UUID.Zero;


        protected SceneCommunicationService m_sceneGridService;
        public bool LoginsDisabled = true;

        protected ISimulationDataService m_SimulationDataService;
        protected IEstateDataService m_EstateDataService;
        protected IAssetService m_AssetService;
        protected IAuthorizationService m_AuthorizationService;
        protected IInventoryService m_InventoryService;
        protected IGridService m_GridService;
        protected ILibraryService m_LibraryService;
        protected ISimulationService m_simulationService;
        protected IAuthenticationService m_AuthenticationService;
        protected IPresenceService m_PresenceService;
        protected IUserAccountService m_UserAccountService;
        protected IAvatarService m_AvatarService;
        protected IGridUserService m_GridUserService;


        protected IXMLRPC m_xmlrpcModule;
        protected IWorldComm m_worldCommModule;
        protected IAvatarFactory m_AvatarFactory;
        protected IRegionSerialiserModule m_serialiser;
        protected IDialogModule m_dialogModule;
        protected IEntityTransferModule m_teleportModule;
        protected ICapabilitiesModule m_capsModule;
        public IXfer XferManager;

        
        public IAttachmentsModule AttachmentsModule = null;
        /// <summary>
        /// Holds the non-viewer statistics collection object for this service/server
        /// </summary>
        protected IStatsCollector m_stats;
        

        // Central Update Loop

        protected int m_fps = 10;
        protected uint m_frame;
        protected float m_timespan = 0.089f;
        protected DateTime m_lastupdate = DateTime.UtcNow;

        private int m_update_physics = 1;
        private int m_update_entitymovement = 1;
        private int m_update_objects = 1; // Update objects which have scheduled themselves for updates
        private int m_update_presences = 1; // Update scene presence movements
        private int m_update_events = 1;
        private int m_update_backup = 50;
        private int m_update_terrain = 50;
        private int m_update_land = 1;
        private int m_update_coarse_locations = 50;

        private int frameMS;
        private int physicsMS2;
        private int physicsMS;
        private int otherMS;
        private int tempOnRezMS;
        private int eventMS;
        private int backupMS;
        private int terrainMS;
        private int landMS;
        private int lastCompletedFrame;

        public int MonitorFrameTime { get { return frameMS; } }
        public int MonitorPhysicsUpdateTime { get { return physicsMS; } }
        public int MonitorPhysicsSyncTime { get { return physicsMS2; } }
        public int MonitorOtherTime { get { return otherMS; } }
        public int MonitorTempOnRezTime { get { return tempOnRezMS; } }
        public int MonitorEventTime { get { return eventMS; } } // This may need to be divided into each event?
        public int MonitorBackupTime { get { return backupMS; } }
        public int MonitorTerrainTime { get { return terrainMS; } }
        public int MonitorLandTime { get { return landMS; } }
        public int MonitorLastFrameTick { get { return lastCompletedFrame; } }

        private string m_defaultScriptEngine;
        private int m_LastLogin;
        private Thread HeartbeatThread;
        private static volatile bool shuttingdown = false;

        private int m_lastUpdate;

        private object m_deleting_scene_object = new object();

        // the minimum time that must elapse before a changed object will be considered for persisted
        public long m_dontPersistBefore = DEFAULT_MIN_TIME_FOR_PERSISTENCE * 10000000L;
        // the maximum time that must elapse before a changed object will be considered for persisted
        public long m_persistAfter = DEFAULT_MAX_TIME_FOR_PERSISTENCE * 10000000L;

        private UpdatePrioritizationSchemes m_priorityScheme = UpdatePrioritizationSchemes.Time;
        private bool m_reprioritizationEnabled = true;
        private double m_reprioritizationInterval = 5000.0;
        private double m_rootReprioritizationDistance = 10.0;
        private double m_childReprioritizationDistance = 20.0;

        private int m_ObjectCapacity = 45000;
        private bool EnableFakeRaycasting = false;
        private bool m_UseSelectionParticles = true;
        public bool LoadingPrims = false;
        public bool CheckForObjectCulling = false;
        public bool[,] DirectionsToBlockChildAgents;
        private string m_DefaultObjectName = "Primitive";
        private bool RunScriptsInAttachments = false;
        /// <summary>
        /// Are we applying physics to any of the prims in this scene?
        /// </summary>
        public bool m_physicalPrim;
        public bool m_trustBinaries;
        public bool m_allowScriptCrossings;
        public bool m_usePreJump = true;
        public bool m_UseNewStyleMovement = true;
        public bool m_useSplatAnimation = true;
        public bool NewCoarseLocations = false;
        public float MaxLowValue = -1000;

        #endregion

        #region Properties

        public UpdatePrioritizationSchemes UpdatePrioritizationScheme { get { return m_priorityScheme; } }
        public bool IsReprioritizationEnabled { get { return m_reprioritizationEnabled; } }
        public double ReprioritizationInterval { get { return m_reprioritizationInterval; } }
        public double RootReprioritizationDistance { get { return m_rootReprioritizationDistance; } }
        public double ChildReprioritizationDistance { get { return m_childReprioritizationDistance; } }
        protected string m_simulatorVersion = "OpenSimulator Server";
        protected Timer m_restartWaitTimer = new Timer();

        public AgentCircuitManager AuthenticateHandler
        {
            get { return m_authenticateHandler; }
        }

        protected ISimulationDataService SimulationDataService
        {
            get { return m_SimulationDataService; }
        }

        public bool ShuttingDown
        {
            get { return shuttingdown; }
        }

        // an instance to the physics plugin's Scene object.
        public PhysicsScene PhysicsScene
        {
            get { return m_sceneGraph.PhysicsScene; }
            set
            {
                // If we're not doing the initial set
                // Then we've got to remove the previous
                // event handler
                if (PhysicsScene != null && PhysicsScene.SupportsNINJAJoints)
                {
                    PhysicsScene.OnJointMoved -= jointMoved;
                    PhysicsScene.OnJointDeactivated -= jointDeactivated;
                    PhysicsScene.OnJointErrorMessage -= jointErrorMessage;
                }

                m_sceneGraph.PhysicsScene = value;

                if (PhysicsScene != null && m_sceneGraph.PhysicsScene.SupportsNINJAJoints)
                {
                    // register event handlers to respond to joint movement/deactivation
                    PhysicsScene.OnJointMoved += jointMoved;
                    PhysicsScene.OnJointDeactivated += jointDeactivated;
                    PhysicsScene.OnJointErrorMessage += jointErrorMessage;
                }
            }
        }

        // This gets locked so things stay thread safe.
        public object SyncRoot
        {
            get { return m_sceneGraph.m_syncRoot; }
        }

        /// <summary>
        /// This is for llGetRegionFPS
        /// </summary>
        public float SimulatorFPS
        {
            get { return StatsReporter.getLastReportedSimFPS(); }
        }
        
        public float[] SimulatorStats
        {
            get { return StatsReporter.getLastReportedSimStats(); }
        }

        public string DefaultScriptEngine
        {
            get { return m_defaultScriptEngine; }
        }

        public EntityManager Entities
        {
            get { return m_sceneGraph.Entities; }
        }

        public override string GetSimulatorVersion()
        {
            return m_simulatorVersion;
        }

        protected override IConfigSource GetConfig()
        {
            return m_config;
        }

        public SceneCommunicationService SceneGridService
        {
            get { return m_sceneGridService; }
        }

        public AvatarAnimations DefaultAnimations
        {
            get
            {
                if (m_defaultAnimations == null)
                    m_defaultAnimations = new AvatarAnimations();
                return m_defaultAnimations;
            }
        }
        public string DefaultObjectName
        {
            get { return m_DefaultObjectName; }
        }

        public IStatsCollector Stats
        {
            get { return m_stats; }
        }

        public bool UseSelectionParticles
        {
            get { return m_UseSelectionParticles; }
        }

        public override bool AllowScriptCrossings
        {
            get { return m_allowScriptCrossings; }
        }

        public new float TimeDilation
        {
            get { return m_sceneGraph.PhysicsScene.TimeDilation; }
        }

        public bool BordersLocked
        {
            get { return m_bordersLocked == 1; }
            set
            {
                if (value == true)
                    m_bordersLocked = 1;
                else
                    m_bordersLocked = 0;
            }
        }

        public IAssetService AssetService
        {
            get
            {
                if (m_AssetService == null)
                {
                    m_AssetService = RequestModuleInterface<IAssetService>();

                    if (m_AssetService == null)
                    {
                        throw new Exception("No IAssetService available.");
                    }
                }

                return m_AssetService;
            }
        }

        public IAuthenticationService AuthenticationService
        {
            get
            {
                if (m_AuthenticationService == null)
                    m_AuthenticationService = RequestModuleInterface<IAuthenticationService>();
                return m_AuthenticationService;
            }
        }

        public IAvatarService AvatarService
        {
            get
            {
                if (m_AvatarService == null)
                    m_AvatarService = RequestModuleInterface<IAvatarService>();
                return m_AvatarService;
            }
        }

        public IAuthorizationService AuthorizationService
        {
            get
            {
                if (m_AuthorizationService == null)
                {
                    m_AuthorizationService = RequestModuleInterface<IAuthorizationService>();
                }

                return m_AuthorizationService;
            }
        }

        protected IEstateConnector m_EstateService;
        public IEstateConnector EstateService
        {
            get
            {
                if (m_EstateService == null)
                {
                    m_EstateService = DataManager.RequestPlugin<IEstateConnector>();
                }
                return m_EstateService;
            }
        }

        public IGridService GridService
        {
            get
            {
                if (m_GridService == null)
                {
                    m_GridService = RequestModuleInterface<IGridService>();

                    if (m_GridService == null)
                    {
                        throw new Exception("No IGridService available. This could happen if the config_include folder doesn't exist or if the OpenSim.ini [Architecture] section isn't set.  Please also check that you have the correct version of your inventory service dll.  Sometimes old versions of this dll will still exist.  Do a clean checkout and re-create the opensim.ini from the opensim.ini.example.");
                    }
                }

                return m_GridService;
            }
        }

        public IGridUserService GridUserService
        {
            get
            {
                if (m_GridUserService == null)
                    m_GridUserService = RequestModuleInterface<IGridUserService>();
                return m_GridUserService;
            }
        }

        public IInventoryService InventoryService
        {
            get
            {
                if (m_InventoryService == null)
                {
                    m_InventoryService = RequestModuleInterface<IInventoryService>();

                    if (m_InventoryService == null)
                    {
                        throw new Exception("No IInventoryService available. This could happen if the config_include folder doesn't exist or if the OpenSim.ini [Architecture] section isn't set.  Please also check that you have the correct version of your inventory service dll.  Sometimes old versions of this dll will still exist.  Do a clean checkout and re-create the opensim.ini from the opensim.ini.example.");
                    }
                }

                return m_InventoryService;
            }
        }

        public ILibraryService LibraryService
        {
            get
            {
                if (m_LibraryService == null)
                    m_LibraryService = RequestModuleInterface<ILibraryService>();

                return m_LibraryService;
            }
        }

        public IPresenceService PresenceService
        {
            get
            {
                if (m_PresenceService == null)
                    m_PresenceService = RequestModuleInterface<IPresenceService>();
                return m_PresenceService;
            }
        }

        public ISimulationService SimulationService
        {
            get
            {
                if (m_simulationService == null)
                    m_simulationService = RequestModuleInterface<ISimulationService>();
                return m_simulationService;
            }
        }

        public IUserAccountService UserAccountService
        {
            get
            {
                if (m_UserAccountService == null)
                    m_UserAccountService = RequestModuleInterface<IUserAccountService>();
                return m_UserAccountService;
            }
        }

        public IAvatarFactory AvatarFactory
        {
            get { return m_AvatarFactory; }
        }

        public ICapabilitiesModule CapsModule
        {
            get { return m_capsModule; }
        }

        public int ObjectCapacity
        {
            get { return m_ObjectCapacity; }
        }
        
        #endregion

        #region Constructors

        public Scene(RegionInfo regInfo, AgentCircuitManager authen,
                     SceneCommunicationService sceneGridService,
            IConfigSource config, string simulatorVersion, ISimulationDataService simDataService, IStatsCollector stats)
        {
            m_stats = stats;
            m_config = config;
            Random random = new Random();
            m_lastAllocatedLocalId = (uint)(random.NextDouble() * (double)(uint.MaxValue / 2)) + (uint)(uint.MaxValue / 4);
            m_authenticateHandler = authen;
            m_sceneGridService = sceneGridService;
            m_regInfo = regInfo;
            m_lastUpdate = Util.EnvironmentTickCount();

            
            BordersLocked = true;

            Border northBorder = new Border();
            northBorder.BorderLine = new Vector3(float.MinValue, float.MaxValue, (int)Constants.RegionSize);  //<---
            northBorder.CrossDirection = Cardinals.N;
            NorthBorders.Add(northBorder);

            Border southBorder = new Border();
            southBorder.BorderLine = new Vector3(float.MinValue, float.MaxValue, 0);    //--->
            southBorder.CrossDirection = Cardinals.S;
            SouthBorders.Add(southBorder);

            Border eastBorder = new Border();
            eastBorder.BorderLine = new Vector3(float.MinValue, float.MaxValue, (int)Constants.RegionSize);   //<---
            eastBorder.CrossDirection = Cardinals.E;
            EastBorders.Add(eastBorder);

            Border westBorder = new Border();
            westBorder.BorderLine = new Vector3(float.MinValue, float.MaxValue, 0);     //--->
            westBorder.CrossDirection = Cardinals.W;
            WestBorders.Add(westBorder);

            BordersLocked = false;

            AuroraEventManager = new AuroraEventManager();
            m_eventManager = new EventManager();
            m_permissions = new ScenePermissions(this);

            m_asyncSceneObjectDeleter = new AsyncSceneObjectGroupDeleter(this);
            m_asyncSceneObjectDeleter.Enabled = true;

            m_SimulationDataService = simDataService;

            // Load region settings
            m_regInfo.RegionSettings = m_SimulationDataService.LoadRegionSettings(m_regInfo.RegionID);
            FindEstateInfo();

            //Bind Storage Manager functions to some land manager functions for this scene
            IParcelServiceConnector conn = DataManager.RequestPlugin<IParcelServiceConnector>();
            if(conn != null)
            {
                EventManager.OnLandObjectAdded +=
                    new EventManager.LandObjectAdded(conn.StoreLandObject);
                EventManager.OnLandObjectRemoved +=
                    new EventManager.LandObjectRemoved(conn.RemoveLandObject);
            }
            else
            {
                EventManager.OnLandObjectAdded +=
                    new EventManager.LandObjectAdded(SimulationDataService.StoreLandObject);
                EventManager.OnLandObjectRemoved +=
                    new EventManager.LandObjectRemoved(SimulationDataService.RemoveLandObject);
            }

            m_sceneGraph = new SceneGraph(this, m_regInfo);

            StatsReporter = new SimStatsReporter(this);
            StatsReporter.OnSendStatsResult += SendSimStatsPackets;
            StatsReporter.OnStatsIncorrect += m_sceneGraph.RecalculateStats;

            m_simulatorVersion = simulatorVersion + " (" + Util.GetRuntimeInformation() + ")";

            #region Region Config

            try
            {
                DirectionsToBlockChildAgents = new bool[3,3];
                DirectionsToBlockChildAgents.Initialize();
                IConfig aurorastartupConfig = m_config.Configs["AuroraStartup"];
                if (aurorastartupConfig != null)
                {
                    RunScriptsInAttachments = aurorastartupConfig.GetBoolean("AllowRunningOfScriptsInAttachments", false);
                    m_UseSelectionParticles = aurorastartupConfig.GetBoolean("UseSelectionParticles", true);
                    EnableFakeRaycasting = aurorastartupConfig.GetBoolean("EnableFakeRaycasting", false);
                    MaxLowValue = aurorastartupConfig.GetFloat("MaxLowValue", -1000);
                    Util.RegionViewSize = aurorastartupConfig.GetInt("RegionSightSize", 1);
                    Util.CloseLocalRegions = aurorastartupConfig.GetBoolean("CloseLocalAgents", true);
                    m_DefaultObjectName = aurorastartupConfig.GetString("DefaultObjectName", m_DefaultObjectName);
                    CheckForObjectCulling = aurorastartupConfig.GetBoolean("CheckForObjectCulling", CheckForObjectCulling);
                    SetObjectCapacity(aurorastartupConfig.GetInt("ObjectCapacity", ObjectCapacity));
                }

                IConfig regionConfig = m_config.Configs[this.RegionInfo.RegionName];
                if (regionConfig != null)
                {
                    #region Block Child Agents config

                    //   [{0,2}, {1, 2}, {2,2}]
                    //   [{0,1}, {1, 1}, {2,1}]  1,1 is the current region
                    //   [{0,0}, {1, 0}, {2,0}]

                    //SouthWest
                    DirectionsToBlockChildAgents[0, 0] = regionConfig.GetBoolean("BlockChildAgentsSouthWest", false);
                    //South
                    DirectionsToBlockChildAgents[1, 0] = regionConfig.GetBoolean("BlockChildAgentsSouth", false);
                    //SouthEast
                    DirectionsToBlockChildAgents[2, 0] = regionConfig.GetBoolean("BlockChildAgentsSouthEast", false);


                    //West
                    DirectionsToBlockChildAgents[0, 1] = regionConfig.GetBoolean("BlockChildAgentsWest", false);
                    //East
                    DirectionsToBlockChildAgents[2, 1] = regionConfig.GetBoolean("BlockChildAgentsEast", false);


                    //NorthWest
                    DirectionsToBlockChildAgents[0, 2] = regionConfig.GetBoolean("BlockChildAgentsNorthWest", false);
                    //North
                    DirectionsToBlockChildAgents[1, 2] = regionConfig.GetBoolean("BlockChildAgentsNorth", false);
                    //NorthEast
                    DirectionsToBlockChildAgents[2, 2] = regionConfig.GetBoolean("BlockChildAgentsNorthEast", false);

                    #endregion
                }
                // Region config overrides global config
                //
                IConfig startupConfig = m_config.Configs["Startup"];

                //Animation states
                IConfig animationConfig = m_config.Configs["Animations"];
                if (animationConfig != null)
                {
                    m_UseNewStyleMovement = animationConfig.GetBoolean("enableNewMovement", m_UseNewStyleMovement);
                    m_usePreJump = animationConfig.GetBoolean("enableprejump", m_usePreJump);
                    m_useSplatAnimation = animationConfig.GetBoolean("enableSplatAnimation", m_useSplatAnimation);
                }
                m_seeIntoRegionFromNeighbor = RegionInfo.SeeIntoThisSimFromNeighbor;
                m_trustBinaries = RegionInfo.TrustBinariesFromForeignSims;
                m_allowScriptCrossings = RegionInfo.AllowScriptCrossing;

                IConfig persistanceConfig = m_config.Configs["Persistance"];
                if (persistanceConfig != null)
                {
                    m_dontPersistBefore =
                        persistanceConfig.GetLong("MinimumTimeBeforePersistenceConsidered", DEFAULT_MIN_TIME_FOR_PERSISTENCE);
                    m_dontPersistBefore *= 10000000;

                    m_persistAfter =
                        persistanceConfig.GetLong("MaximumTimeBeforePersistenceConsidered", DEFAULT_MAX_TIME_FOR_PERSISTENCE);
                    m_persistAfter *= 10000000;
                }
                else
                {
                    m_dontPersistBefore = DEFAULT_MIN_TIME_FOR_PERSISTENCE;
                    m_dontPersistBefore *= 10000000;
                    m_persistAfter = DEFAULT_MAX_TIME_FOR_PERSISTENCE;
                    m_persistAfter *= 10000000;
                }
                IConfig scriptEngineConfig = m_config.Configs["ScriptEngines"];
                if (scriptEngineConfig != null)
                    m_defaultScriptEngine = scriptEngineConfig.GetString("DefaultScriptEngine", "AuroraDotNetEngine");

                IConfig packetConfig = m_config.Configs["PacketPool"];
                if (packetConfig != null)
                {
                    PacketPool.Instance.RecyclePackets = packetConfig.GetBoolean("RecyclePackets", true);
                    PacketPool.Instance.RecycleDataBlocks = packetConfig.GetBoolean("RecycleDataBlocks", true);
                }

                m_strictAccessControl = startupConfig.GetBoolean("StrictAccessControl", m_strictAccessControl);
            }
            catch
            {
                m_log.Warn("[SCENE]: Failed to load StartupConfig");
            }

            #endregion Region Config

            #region Interest Management

            if (m_config != null)
            {
                IConfig interestConfig = m_config.Configs["InterestManagement"];
                if (interestConfig != null)
                {
                    string update_prioritization_scheme = interestConfig.GetString("UpdatePrioritizationScheme", "Time").Trim().ToLower();

                    try
                    {
                        m_priorityScheme = (UpdatePrioritizationSchemes)Enum.Parse(typeof(UpdatePrioritizationSchemes), update_prioritization_scheme, true);
                    }
                    catch (Exception)
                    {
                        m_log.Warn("[PRIORITIZER]: UpdatePrioritizationScheme was not recognized, setting to default prioritizer Time");
                        m_priorityScheme = UpdatePrioritizationSchemes.Time;
                    }

                    m_reprioritizationEnabled = interestConfig.GetBoolean("ReprioritizationEnabled", true);
                    m_reprioritizationInterval = interestConfig.GetDouble("ReprioritizationInterval", 5000.0);
                    m_rootReprioritizationDistance = interestConfig.GetDouble("RootReprioritizationDistance", 10.0);
                    m_childReprioritizationDistance = interestConfig.GetDouble("ChildReprioritizationDistance", 20.0);
                }
            }

            //m_log.Info("[SCENE]: Using the " + m_priorityScheme + " prioritization scheme");

            #endregion Interest Management

            #region Startup Complete config

            EventManager.OnAddToStartupQueue += AddToStartupQueue;
            EventManager.OnFinishedStartup += FinishedStartup;
            EventManager.OnStartupComplete += StartupComplete;

            AddToStartupQueue("Startup");

            #endregion

            LoadWorldMap();

            //Add stats handlers
            MainServer.Instance.AddStreamHandler(new RegionStatsHandler(RegionInfo));
        }

        private void FindEstateInfo()
        {
            if (EstateService != null)
            {
                EstateSettings ES = EstateService.LoadEstateSettings(m_regInfo.RegionID);
                if (ES != null && ES.EstateID == 0) // No record at all, new estate required
                {
                    m_log.Warn("Your region " + m_regInfo.RegionName + " is not part of an estate.");
                    ES = CreateEstateInfo();
                }
                else if (ES == null) //Cannot connect to the estate service
                {
                    m_log.Warn("The connection to the estate service was broken, please try again soon.");
                    while (true)
                    {
                        MainConsole.Instance.CmdPrompt("Press enter to try again.");
                        ES = EstateService.LoadEstateSettings(m_regInfo.RegionID);
                        if (ES != null && ES.EstateID == 0)
                            ES = CreateEstateInfo();
                        else if (ES == null)
                            continue;
                        break;
                    }
                }
                //This sets the password back so we can use it again to make changes to the estate settings later
                if (ES.EstatePass == "" && m_regInfo.EstateSettings.EstatePass != "")
                    ES.EstatePass = m_regInfo.EstateSettings.EstatePass;
                m_regInfo.EstateSettings = ES;
                m_regInfo.WriteNiniConfig();
            }
            else
            {
                IConfig dbConfig = Config.Configs["DatabaseService"];
                IConfig esConfig = Config.Configs["EstateService"];
                if (dbConfig != null)
                {
                    string StorageDLL = dbConfig.GetString("StorageProvider", String.Empty);
                    string StorageConnectionString = dbConfig.GetString("ConnectionString", String.Empty);
                    if (esConfig != null)
                    {
                        StorageDLL = esConfig.GetString("StorageProvider", StorageDLL);
                        StorageConnectionString = esConfig.GetString("ConnectionString", StorageConnectionString);
                    }
                    if (StorageDLL != "")
                    {
                        IEstateDataStore EDS = AuroraModuleLoader.LoadPlugin<IEstateDataStore>(StorageDLL, "IEstateDataStore");
                        EDS.Initialise(StorageConnectionString);
                        if (EDS != null)
                        {
                            m_regInfo.EstateSettings = EDS.LoadEstateSettings(RegionInfo.RegionID, true);
                        }
                    }
                }
            }
        }

        private EstateSettings CreateEstateInfo()
        {
            EstateSettings ES = null;
            while (true)
            {
                string response = MainConsole.Instance.CmdPrompt("Do you wish to join an existing estate for " + m_regInfo.RegionName + "? (Options are {yes, no, find})", "no", new List<string>() { "yes", "no", "find" });
                if (response == "no")
                {
                    // Create a new estate
                    ES = new EstateSettings();
                    ES.EstateName = MainConsole.Instance.CmdPrompt("New estate name", m_regInfo.EstateSettings.EstateName);
                    string Password = Util.Md5Hash(Util.Md5Hash(MainConsole.Instance.CmdPrompt("New estate password (to keep others from joining your estate, blank to have no pass)", ES.EstatePass)));
                    ES.EstatePass = Password;
                    ES = EstateService.CreateEstate(ES, RegionInfo.RegionID);
                    if (ES == null)
                    {
                        m_log.Warn("The connection to the server was broken, please try again soon.");
                        continue;
                    }
                    else if (ES.EstateID == 0)
                    {
                        m_log.Warn("There was an error in creating this estate: " + ES.EstateName); //EstateName holds the error. See LocalEstateConnector for more info.
                        continue;
                    }
                    //We set this back if there wasn't an error because the EstateService will NOT send it back
                    ES.EstatePass = Password;
                    break;
                }
                else if (response == "yes")
                {
                    response = MainConsole.Instance.CmdPrompt("Estate name to join", "None");
                    if (response == "None")
                        continue;

                    List<int> estateIDs = EstateService.GetEstates(response);
                    if (estateIDs == null)
                    {
                        m_log.Warn("The connection to the server was broken, please try again soon.");
                        continue;
                    }
                    if (estateIDs.Count < 1)
                    {
                        m_log.Warn("The name you have entered matches no known estate. Please try again");
                        continue;
                    }

                    int estateID = estateIDs[0];

                    string Password = Util.Md5Hash(Util.Md5Hash(MainConsole.Instance.CmdPrompt("Password for the estate", "")));
                    //We save the Password because we have to reset it after we tell the EstateService about it, as it clears it for security reasons
                    if (EstateService.LinkRegion(m_regInfo.RegionID, estateID, Password))
                    {
                        ES = EstateService.LoadEstateSettings(m_regInfo.RegionID); //We could do by EstateID now, but we need to completely make sure that it fully is set up
                        if (ES == null)
                        {
                            m_log.Warn("The connection to the server was broken, please try again soon.");
                            continue;
                        }
                        break;
                    }

                    m_log.Warn("Joining the estate failed. Please try again.");
                    continue;
                }
                else if (response == "find")
                {
                    ES = EstateService.LoadEstateSettings(m_regInfo.RegionID);
                    if (ES == null)
                    {
                        m_log.Warn("The connection to the estate service was broken, please try again soon.");
                        continue;
                    }
                    break;
                }
            }
            return ES;
        }

        /// <summary>
        /// Mock constructor for scene group persistency unit tests.
        /// SceneObjectGroup RegionId property is delegated to Scene.
        /// </summary>
        /// <param name="regInfo"></param>
        public Scene(RegionInfo regInfo)
        {
            BordersLocked = true;
            Border northBorder = new Border();
            northBorder.BorderLine = new Vector3(float.MinValue, float.MaxValue, (int)Constants.RegionSize - 1);  //<---
            northBorder.CrossDirection = Cardinals.N;
            NorthBorders.Add(northBorder);

            Border southBorder = new Border();
            southBorder.BorderLine = new Vector3(float.MinValue, float.MaxValue,1);    //--->
            southBorder.CrossDirection = Cardinals.S;
            SouthBorders.Add(southBorder);

            Border eastBorder = new Border();
            eastBorder.BorderLine = new Vector3(float.MinValue, float.MaxValue, (int)Constants.RegionSize - 1);   //<---
            eastBorder.CrossDirection = Cardinals.E;
            EastBorders.Add(eastBorder);

            Border westBorder = new Border();
            westBorder.BorderLine = new Vector3(float.MinValue, float.MaxValue,1);     //--->
            westBorder.CrossDirection = Cardinals.W;
            WestBorders.Add(westBorder);
            BordersLocked = false;

            m_regInfo = regInfo;
            m_eventManager = new EventManager();
            AuroraEventManager = new AuroraEventManager();
            

            m_lastUpdate = Util.EnvironmentTickCount();
        }

        #endregion Constructors

        #region Startup / Close Methods

        public string[] GetUserNames(UUID uuid)
        {
            string[] returnstring = new string[0];

            UserAccount account = UserAccountService.GetUserAccount(RegionInfo.ScopeID, uuid);

            if (account != null)
            {
                returnstring = new string[2];
                returnstring[0] = account.FirstName;
                returnstring[1] = account.LastName;
            }

            return returnstring;
        }

        public string GetUserName(UUID uuid)
        {
            string[] names = GetUserNames(uuid);
            if (names.Length == 2)
            {
                string firstname = names[0];
                string lastname = names[1];

                return firstname + " " + lastname;

            }
            return "(hippos)";
        }

        /// <summary>
        /// Another region is up. 
        ///
        /// We only add it to the neighbor list if it's within 1 region from here.
        /// Agents may have draw distance values that cross two regions though, so
        /// we add it to the notify list regardless of distance. We'll check
        /// the agent's draw distance before notifying them though.
        /// </summary>
        /// <param name="otherRegion">RegionInfo handle for the new region.</param>
        /// <returns>True after all operations complete, throws exceptions otherwise.</returns>
        public override void OtherRegionUp(GridRegion otherRegion)
        {
            uint xcell = (uint)((int)otherRegion.RegionLocX / (int)Constants.RegionSize);
            uint ycell = (uint)((int)otherRegion.RegionLocY / (int)Constants.RegionSize);
            //m_log.InfoFormat("[SCENE]: (on region {0}): Region {1} up in coords {2}-{3}", 
            //    RegionInfo.RegionName, otherRegion.RegionName, xcell, ycell);

            if (RegionInfo.RegionHandle != otherRegion.RegionHandle)
            {

                // If these are cast to INT because long + negative values + abs returns invalid data
                int resultX = Math.Abs((int)xcell - (int)RegionInfo.RegionLocX);
                int resultY = Math.Abs((int)ycell - (int)RegionInfo.RegionLocY);
                if (resultX <= 1 && resultY <= 1)
                {
                    // Let the grid service module know, so this can be cached
                    m_eventManager.TriggerOnRegionUp(otherRegion);

                    //This updates us about new neighbors in the cache
                    GridService.GetNeighbours(UUID.Zero, this.RegionInfo.RegionID);
                    try
                    {
                        ForEachScenePresence(delegate(ScenePresence agent)
                                             {
                                                 // If agent is a root agent.
                                                 if (!agent.IsChildAgent)
                                                 {
                                                     //agent.ControllingClient.new
                                                     //this.CommsManager.InterRegion.InformRegionOfChildAgent(otherRegion.RegionHandle, agent.ControllingClient.RequestClientInfo());

                                                     List<ulong> old = new List<ulong>();
                                                     old.Add(otherRegion.RegionHandle);
                                                     agent.DropOldNeighbours(old);
                                                     if (m_teleportModule != null)
                                                         m_teleportModule.EnableChildAgent(agent, otherRegion);
                                                 }
                                             }
                            );
                    }
                    catch (NullReferenceException)
                    {
                        // This means that we're not booted up completely yet.
                        // This shouldn't happen too often anymore.
                        m_log.Error("[SCENE]: Couldn't inform client of regionup because we got a null reference exception");
                    }

                }
                else
                {
                    m_log.Info("[INTERGRID]: Got notice about far away Region: " + otherRegion.RegionName.ToString() +
                               " at  (" + otherRegion.RegionLocX.ToString() + ", " +
                               otherRegion.RegionLocY.ToString() + ")");
                }
            }
        }

        public void AddNeighborRegion(RegionInfo region)
        {
            lock (m_neighbours)
            {
                if (!CheckNeighborRegion(region))
                {
                    m_neighbours.Add(region);
                }
            }
        }

        public bool CheckNeighborRegion(RegionInfo region)
        {
            bool found = false;
            lock (m_neighbours)
            {
                foreach (RegionInfo reg in m_neighbours)
                {
                    if (reg.RegionHandle == region.RegionHandle)
                    {
                        found = true;
                        break;
                    }
                }
            }
            return found;
        }

        // Alias IncomingHelloNeighbour OtherRegionUp, for now
        public GridRegion IncomingHelloNeighbour(RegionInfo neighbour)
        {
            OtherRegionUp(new GridRegion(neighbour));
            return new GridRegion(RegionInfo);
        }

        /// <summary>
        /// Given float seconds, this will restart the region.
        /// </summary>
        /// <param name="seconds">float indicating duration before restart.</param>
        public virtual void Restart(float seconds)
        {
            // notifications are done in 15 second increments
            // so ..   if the number of seconds is less then 15 seconds, it's not really a restart request
            // It's a 'Cancel restart' request.

            // RestartNow() does immediate restarting.
            if (seconds < 15)
            {
                m_restartTimer.Stop();
                m_dialogModule.SendGeneralAlert("Restart Aborted");
            }
            else
            {
                // Now we figure out what to set the timer to that does the notifications and calls, RestartNow()
                m_restartTimer.Interval = 15000;
                m_incrementsof15seconds = (int)seconds / 15;
                m_RestartTimerCounter = 0;
                m_restartTimer.AutoReset = true;
                m_restartTimer.Elapsed += new ElapsedEventHandler(RestartTimer_Elapsed);
                m_log.Info("[REGION]: Restarting Region in " + (seconds / 60) + " minutes");
                m_restartTimer.Start();
                m_dialogModule.SendNotificationToUsersInRegion(
                    UUID.Random(), String.Empty, RegionInfo.RegionName + String.Format(": Restarting in {0} Minutes", (int)(seconds / 60.0)));
            }
        }

        // The Restart timer has occured.
        // We have to figure out if this is a notification or if the number of seconds specified in Restart
        // have elapsed.
        // If they have elapsed, call RestartNow()
        public void RestartTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            m_RestartTimerCounter++;
            if (m_RestartTimerCounter <= m_incrementsof15seconds)
            {
                if (m_RestartTimerCounter == 4 || m_RestartTimerCounter == 6 || m_RestartTimerCounter == 7)
                    m_dialogModule.SendNotificationToUsersInRegion(
                        UUID.Random(),
                        String.Empty,
                        RegionInfo.RegionName + ": Restarting in " + ((8 - m_RestartTimerCounter) * 15) + " seconds");
            }
            else
            {
                m_restartTimer.Stop();
                m_restartTimer.AutoReset = false;
                RestartNow();
            }
        }

        // This causes the region to restart immediatley.
        public void RestartNow()
        {
            IConfig startupConfig = m_config.Configs["Startup"];
            if (startupConfig != null)
            {
                if (startupConfig.GetBoolean("InworldRestartShutsDown", false))
                {
                    MainConsole.Instance.RunCommand("shutdown");
                    return;
                }
            }
            if (UseTracker)
            {
                tracker.OnNeedToAddThread -= NeedsNewThread;
                tracker.Close();
                tracker = null;
            }

            if (PhysicsScene != null)
            {
                PhysicsScene.Dispose();
            }

            m_log.Error("[REGION]: Closing");
            Close();

            m_log.Error("[REGION]: Firing Region Restart Message");
            base.Restart(0);
        }

        // This is a helper function that notifies root agents in this region that a new sim near them has come up
        // This is in the form of a timer because when an instance of OpenSim.exe is started,
        // Even though the sims initialize, they don't listen until 'all of the sims are initialized'
        // If we tell an agent about a sim that's not listening yet, the agent will not be able to connect to it.
        // subsequently the agent will never see the region come back online.
        public void RestartNotifyWaitElapsed(object sender, ElapsedEventArgs e)
        {
            m_restartWaitTimer.Stop();
            lock (m_regionRestartNotifyList)
            {
                foreach (RegionInfo region in m_regionRestartNotifyList)
                {
                    GridRegion r = new GridRegion(region);
                    try
                    {
                        ForEachScenePresence(delegate(ScenePresence agent)
                                             {
                                                 // If agent is a root agent.
                                                 if (!agent.IsChildAgent)
                                                 {
                                                     if (m_teleportModule != null)
                                                         m_teleportModule.EnableChildAgent(agent, r);
                                                 }
                                             }
                            );
                    }
                    catch (NullReferenceException)
                    {
                        // This means that we're not booted up completely yet.
                        // This shouldn't happen too often anymore.
                    }
                }

                // Reset list to nothing.
                m_regionRestartNotifyList.Clear();
            }
        }

        public void UpdateGridRegion()
        {
            GridService.UpdateMap(RegionInfo.ScopeID, new GridRegion(RegionInfo), RegionInfo.RegionSettings.TerrainImageID, RegionInfo.RegionSettings.TerrainMapImageID, RegionInfo.GridSecureSessionID);
        }

        public void SetSceneCoreDebug(bool ScriptEngine, bool CollisionEvents, bool PhysicsEngine)
        {
            if (RegionInfo.RegionSettings.DisableScripts == !ScriptEngine)
            {
                if (ScriptEngine)
                {
                    m_log.Info("[SCENEDEBUG]: Stopping all Scripts in Scene");
                    IScriptModule mod = RequestModuleInterface<IScriptModule>();
                    mod.StopAllScripts();
                }
                else
                {
                    m_log.Info("[SCENEDEBUG]: Starting all Scripts in Scene");

                    EntityBase[] entities = Entities.GetEntities();
                    foreach (EntityBase ent in entities)
                    {
                        if (ent is SceneObjectGroup)
                        {
                            if (ent is SceneObjectGroup)
                            {
                                ((SceneObjectGroup)ent).CreateScriptInstances(0, false, DefaultScriptEngine, 0, UUID.Zero);
                                ((SceneObjectGroup)ent).ResumeScripts();
                            }
                        }
                    }
                }
                RegionInfo.RegionSettings.DisableScripts = !ScriptEngine;
            }

            if (RegionInfo.RegionSettings.DisablePhysics == !PhysicsEngine)
            {
                RegionInfo.RegionSettings.DisablePhysics = !PhysicsEngine;
            }

            if (RegionInfo.RegionSettings.DisableCollisions == !CollisionEvents)
            {
                RegionInfo.RegionSettings.DisableCollisions = !CollisionEvents;
                PhysicsScene.DisableCollisions = RegionInfo.RegionSettings.DisableCollisions;
            }
            RegionInfo.RegionSettings.Save();
        }

        public int GetInaccurateNeighborCount()
        {
            return m_neighbours.Count;
        }

        // This is the method that shuts down the scene.
        public override void Close()
        {
            m_log.InfoFormat("[SCENE]: Closing down the single simulator: {0}", RegionInfo.RegionName);

            m_restartTimer.Stop();
            m_restartTimer.Close();

            // Kick all ROOT agents with the message, 'The simulator is going down'
            ForEachScenePresence(delegate(ScenePresence avatar)
                                 {
                                     if (avatar.KnownChildRegionHandles.Contains(RegionInfo.RegionHandle))
                                         avatar.KnownChildRegionHandles.Remove(RegionInfo.RegionHandle);

                                     if (!avatar.IsChildAgent)
                                         avatar.ControllingClient.Kick("The simulator is going down.");

                                     avatar.ControllingClient.SendShutdownConnectionNotice();
                                 });

            // Wait here, or the kick messages won't actually get to the agents before the scene terminates.
            Thread.Sleep(500);

            // Stop all client threads.
            ForEachScenePresence(delegate(ScenePresence avatar) { avatar.ControllingClient.Close(); });

            // Stop updating the scene objects and agents.
            //m_heartbeatTimer.Close();
            shuttingdown = true;

            m_log.Debug("[SCENE]: Persisting changed objects");

            //Backup uses the new taints system
            ProcessPrimBackupTaints(false);

            //Replaced by the taints system as above
            /*List<EntityBase> entities = GetEntities();
            foreach (EntityBase entity in entities)
            {
                if (!entity.IsDeleted && entity is SceneObjectGroup && ((SceneObjectGroup)entity).HasGroupChanged)
                {
                    ((SceneObjectGroup)entity).ProcessBackup(DataStore, false);
                }
            }*/

            m_sceneGraph.Close();

            //Deregister from the grid server
            if (!GridService.DeregisterRegion(m_regInfo.RegionID, RegionInfo.GridSecureSessionID))
                m_log.WarnFormat("[SCENE]: Deregister from grid failed for region {0}", m_regInfo.RegionName);

            // call the base class Close method.
            base.Close();
        }

        public AuroraThreadTracker tracker = null;
        private bool UseTracker = true;
        /// <summary>
        /// Start the timer which triggers regular scene updates
        /// </summary>
        public void StartTimer()
        {
            m_lastUpdate = Util.EnvironmentTickCount();
            if (UseTracker)
            {
                if (tracker == null)
                    tracker = new AuroraThreadTracker();
                ScenePhysicsHeartbeat shb = new ScenePhysicsHeartbeat(this);
                SceneBackupHeartbeat sbhb = new SceneBackupHeartbeat(this);
                SceneUpdateHeartbeat suhb = new SceneUpdateHeartbeat(this);
                tracker.AddSceneHeartbeat(suhb, out HeartbeatThread);
                tracker.AddSceneHeartbeat(shb, out HeartbeatThread);
                tracker.AddSceneHeartbeat(sbhb, out HeartbeatThread);
                //tracker.AddSceneHeartbeat(new SceneHeartbeat(this), out HeartbeatThread);
                //Start this after the threads are started.
                tracker.Init(this);
                tracker.OnNeedToAddThread += NeedsNewThread;
            }
            else
                HeartbeatThread = Watchdog.StartThread(Update, "Heartbeat for region " + RegionInfo.RegionName, ThreadPriority.Normal, false);
        }

        public void NeedsNewThread(string type)
        {
            System.Threading.Thread thread;
            if(type == "SceneUpdateHeartbeat")
                tracker.AddSceneHeartbeat(new Scene.SceneUpdateHeartbeat(this), out thread);
            if (type == "ScenePhysicsHeartbeat")
                tracker.AddSceneHeartbeat(new Scene.ScenePhysicsHeartbeat(this), out thread);
            if (type == "SceneBackupHeartbeat")
                tracker.AddSceneHeartbeat(new Scene.SceneBackupHeartbeat(this), out thread);
        }

        /// <summary>
        /// Sets up references to modules required by the scene
        /// </summary>
        public void SetModuleInterfaces()
        {
            m_xmlrpcModule = RequestModuleInterface<IXMLRPC>();
            m_worldCommModule = RequestModuleInterface<IWorldComm>();
            XferManager = RequestModuleInterface<IXfer>();
            m_AvatarFactory = RequestModuleInterface<IAvatarFactory>();
            AttachmentsModule = RequestModuleInterface<IAttachmentsModule>();
            m_serialiser = RequestModuleInterface<IRegionSerialiserModule>();
            m_dialogModule = RequestModuleInterface<IDialogModule>();
            m_capsModule = RequestModuleInterface<ICapabilitiesModule>();
            m_teleportModule = RequestModuleInterface<IEntityTransferModule>();

            // Shoving this in here for now, because we have the needed
            // interfaces at this point
            //
            // TODO: Find a better place for this
            //
            while (m_regInfo.EstateSettings.EstateOwner == UUID.Zero && MainConsole.Instance != null)
            {
                MainConsole.Instance.Output("The current estate " + m_regInfo.EstateSettings.EstateName + " has no owner set.");
                List<char> excluded = new List<char>(new char[1] { ' ' });
                string first = MainConsole.Instance.CmdPrompt("Estate owner first name", "Test", excluded);
                string last = MainConsole.Instance.CmdPrompt("Estate owner last name", "User", excluded);

                UserAccount account = UserAccountService.GetUserAccount(m_regInfo.ScopeID, first, last);

                if (account == null)
                {
                    // Create a new account
                    account = new UserAccount(m_regInfo.ScopeID, first, last, String.Empty);
                    if (account.ServiceURLs == null || (account.ServiceURLs != null && account.ServiceURLs.Count == 0))
                    {
                        account.ServiceURLs = new Dictionary<string, object>();
                        account.ServiceURLs["HomeURI"] = string.Empty;
                        account.ServiceURLs["GatekeeperURI"] = string.Empty;
                        account.ServiceURLs["InventoryServerURI"] = string.Empty;
                        account.ServiceURLs["AssetServerURI"] = string.Empty;
                    }

                    if (UserAccountService.StoreUserAccount(account))
                    {
                        string password = MainConsole.Instance.PasswdPrompt("Password");
                        string email = MainConsole.Instance.CmdPrompt("Email", "");

                        account.Email = email;
                        UserAccountService.StoreUserAccount(account);

                        bool success = false;
                        success = AuthenticationService.SetPassword(account.PrincipalID, password);
                        if (!success)
                            m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to set password for account {0} {1}.",
                               first, last);

                        GridRegion home = null;
                        if (GridService != null)
                        {
                            List<GridRegion> defaultRegions = GridService.GetDefaultRegions(UUID.Zero);
                            if (defaultRegions != null && defaultRegions.Count >= 1)
                                home = defaultRegions[0];

                            if (GridUserService != null && home != null)
                                GridUserService.SetHome(account.PrincipalID.ToString(), home.RegionID, new Vector3(128, 128, 0), new Vector3(0, 1, 0));
                            else
                                m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to set home for account {0} {1}.",
                                   first, last);

                        }
                        else
                            m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to retrieve home region for account {0} {1}.",
                               first, last);

                        if (InventoryService != null)
                            success = InventoryService.CreateUserInventory(account.PrincipalID);
                        if (!success)
                            m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to create inventory for account {0} {1}.",
                               first, last);


                        m_log.InfoFormat("[USER ACCOUNT SERVICE]: Account {0} {1} created successfully", first, last);

                        m_regInfo.EstateSettings.EstateOwner = account.PrincipalID;
                        m_regInfo.EstateSettings.Save();
                    }
                    else
                        m_log.ErrorFormat("[SCENE]: Unable to store account. If this simulator is connected to a grid, you must create the estate owner account first.");
                }
                else
                {
                    m_regInfo.EstateSettings.EstateOwner = account.PrincipalID;
                    m_regInfo.EstateSettings.Save();
                }
            }
        }

        #endregion

        #region Update Methods

        #region Scene Heartbeat parts

        public bool firstRun = true;

        public class SceneBackupHeartbeat : IThread
        {
            public SceneBackupHeartbeat(Scene scene)
            {
                type = "SceneBackupHeartbeat";
                m_scene = scene;
            }

            public override void Restart()
            {
                ShouldExit = true;
                SceneBackupHeartbeat heartbeat = new SceneBackupHeartbeat(m_scene);
                m_scene.tracker.AddSceneHeartbeat(heartbeat, out m_scene.HeartbeatThread);
            }

            /// <summary>
            /// Performs per-frame updates regularly
            /// </summary>
            public override void Start()
            {
                try
                {
                    Update();

                    m_scene.m_lastUpdate = Util.EnvironmentTickCount();
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    m_log.Error("[Scene]: Failed with " + ex);
                }
                FireThreadClosing(this);
            }

            private void CheckExit()
            {
                if (!ShouldExit && !shuttingdown)
                    return;
                //Lets kill this thing
                throw new Exception("Closing");
            }

            private void Update()
            {
                int maintc;

                while (!ShouldExit)
                {
                    TimeSpan SinceLastFrame = DateTime.UtcNow - m_scene.m_lastupdate;
                    
                    maintc = Util.EnvironmentTickCount();
                    int tmpFrameMS = maintc;
                    m_scene.tempOnRezMS = m_scene.eventMS = m_scene.backupMS = m_scene.terrainMS = m_scene.landMS = 0;

                    // Increment the frame counter
                    ++m_scene.m_frame;

                    try
                    {
                        if (m_scene.m_frame % m_scene.m_update_events == 0)
                            {
                                int evMS = Util.EnvironmentTickCount();
                                m_scene.UpdateEvents();
                                m_scene.eventMS = Util.EnvironmentTickCountSubtract(evMS); ;
                            }
                            CheckExit();
                            if (m_scene.m_frame % m_scene.m_update_backup == 0)
                            {
                                int backMS = Util.EnvironmentTickCount();
                                m_scene.UpdateStorageBackup();
                                m_scene.backupMS = Util.EnvironmentTickCountSubtract(backMS);
                            }
                            CheckExit();
                            
                            if (m_scene.m_frame % m_scene.m_update_terrain == 0)
                            {
                                int terMS = Util.EnvironmentTickCount();
                                m_scene.UpdateTerrain();
                                m_scene.terrainMS = Util.EnvironmentTickCountSubtract(terMS);
                            }
                            CheckExit();
                            
                            if (m_scene.m_frame % m_scene.m_update_land == 0)
                            {
                                int ldMS = Util.EnvironmentTickCount();
                                m_scene.UpdateLand();
                                m_scene.landMS = Util.EnvironmentTickCountSubtract(ldMS);
                            }
                            CheckExit();
                            lock (m_scene.m_needsDeleted)
                            {
                                if (m_scene.m_needsDeleted.Count != 0)
                                {
                                    //Removes all objects in one SQL query
                                    foreach (UUID id in m_scene.m_needsDeleted)
                                    {
                                        m_scene.SimulationDataService.RemoveObject(id, m_scene.RegionInfo.RegionID);
                                    }
                                    //m_scene.DataStore.RemoveObjects(m_scene.m_needsDeleted);
                                    m_scene.m_needsDeleted.Clear();
                                }
                            }

                            if (m_scene.PhysicsReturns.Count != 0)
                            {
                                lock (m_scene.PhysicsReturns)
                                {
                                    m_scene.returnObjects(m_scene.PhysicsReturns.ToArray(), UUID.Zero);
                                    m_scene.PhysicsReturns.Clear();
                                }
                            }
                            
                            m_scene.frameMS = Util.EnvironmentTickCountSubtract(tmpFrameMS);
                            m_scene.otherMS = m_scene.tempOnRezMS + m_scene.eventMS + m_scene.backupMS + m_scene.terrainMS + m_scene.landMS;
                            m_scene.lastCompletedFrame = Util.EnvironmentTickCount();

                            m_scene.StatsReporter.AddTimeDilation(m_scene.TimeDilation);
                            m_scene.StatsReporter.AddFPS(1);
                            m_scene.StatsReporter.SetRootAgents(m_scene.m_sceneGraph.GetRootAgentCount());
                            m_scene.StatsReporter.SetChildAgents(m_scene.m_sceneGraph.GetChildAgentCount());
                            m_scene.StatsReporter.SetObjects(m_scene.m_sceneGraph.GetTotalObjectsCount());
                            m_scene.StatsReporter.SetActiveObjects(m_scene.m_sceneGraph.GetActiveObjectsCount());
                            m_scene.StatsReporter.addFrameMS(m_scene.frameMS);
                            m_scene.StatsReporter.addPhysicsMS(m_scene.physicsMS + m_scene.physicsMS2);
                            m_scene.StatsReporter.addOtherMS(m_scene.otherMS);
                            m_scene.StatsReporter.SetActiveScripts(m_scene.m_sceneGraph.GetActiveScriptsCount());
                            m_scene.StatsReporter.addScriptEvents(m_scene.m_sceneGraph.GetScriptEPS());
                            m_scene.StatsReporter.SetPhysicsOther(m_scene.physicsMS2);
                            m_scene.StatsReporter.SetPhysicsStep((int)Math.Max(SinceLastFrame.TotalSeconds, m_scene.m_timespan));

                        CheckExit();
                    }
                    catch (NotImplementedException)
                    {
                        throw;
                    }
                    catch (AccessViolationException e)
                    {
                        m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                    }
                    //catch (NullReferenceException e)
                    //{
                    //   m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + RegionInfo.RegionName);
                    //}
                    catch (InvalidOperationException e)
                    {
                        m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                    }
                    catch (Exception e)
                    {
                        if (e.Message != "Closing")
                            m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                        break;
                    }
                    finally
                    {
                    }

                    maintc = Util.EnvironmentTickCountSubtract(maintc);
                    maintc = (int)(m_scene.m_timespan * 1000) - maintc;
                    m_scene.StatsReporter.SetSleepMS(maintc);

                    if (maintc > 0)
                        Thread.Sleep(maintc / 3);
                    try
                    {
                        CheckExit();
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        }

        public class ScenePhysicsHeartbeat : IThread
        {
            public ScenePhysicsHeartbeat(Scene scene)
            {
                type = "ScenePhysicsHeartbeat";
                m_scene = scene;
            }

            public override void Restart()
            {
                ShouldExit = true;
                ScenePhysicsHeartbeat heartbeat = new ScenePhysicsHeartbeat(m_scene);
                m_scene.tracker.AddSceneHeartbeat(heartbeat, out m_scene.HeartbeatThread);
            }

            /// <summary>
            /// Performs per-frame updates regularly
            /// </summary>
            public override void Start()
            {
                try
                {
                    Update();

                    m_scene.m_lastUpdate = Util.EnvironmentTickCount();
                }
                catch (ThreadAbortException)
                {
                }
                catch(Exception ex)
                {
                    m_log.Error("[Scene]: Failed with " + ex);
                }
                FireThreadClosing(this);
            }

            private void CheckExit()
            {
                if (!ShouldExit && !shuttingdown)
                    return;
                //Lets kill this thing
                throw new Exception("Closing");
            }

            private void Update()
            {
                float physicsFPS;
                int maintc;

                while (!ShouldExit)
                {
                    TimeSpan SinceLastFrame = DateTime.UtcNow - m_scene.m_lastupdate;
                    physicsFPS = 0f;

                    maintc = Util.EnvironmentTickCount();
                    int tmpFrameMS = maintc;
                    m_scene.tempOnRezMS = m_scene.eventMS = m_scene.backupMS = m_scene.terrainMS = m_scene.landMS = 0;

                    // Increment the frame counter
                    ++m_scene.m_frame;

                    try
                    {
                        CheckExit();
                        int tmpPhysicsMS2 = Util.EnvironmentTickCount();
                        if ((m_scene.m_frame % m_scene.m_update_physics == 0) && !m_scene.RegionInfo.RegionSettings.DisablePhysics)
                            m_scene.m_sceneGraph.UpdatePreparePhysics();
                        m_scene.physicsMS2 = Util.EnvironmentTickCountSubtract(tmpPhysicsMS2);
                        CheckExit();
                            
                        if (m_scene.m_frame % m_scene.m_update_entitymovement == 0)
                            m_scene.m_sceneGraph.UpdateScenePresenceMovement();
                        CheckExit();
                            
                        int tmpPhysicsMS = Util.EnvironmentTickCount();
                        if (m_scene.m_frame % m_scene.m_update_physics == 0)
                        {
                            if (!m_scene.RegionInfo.RegionSettings.DisablePhysics)
                                physicsFPS = m_scene.m_sceneGraph.UpdatePhysics(Math.Max(SinceLastFrame.TotalSeconds, m_scene.m_timespan));
                        }
                        m_scene.StatsReporter.AddPhysicsFPS(physicsFPS);
                        CheckExit();
                            
                        m_scene.physicsMS = Util.EnvironmentTickCountSubtract(tmpPhysicsMS);
                    }
                    catch (NotImplementedException)
                    {
                        throw;
                    }
                    catch (AccessViolationException e)
                    {
                        if (e.Message != "Closing")
                            m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                    }
                    //catch (NullReferenceException e)
                    //{
                    //   m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + RegionInfo.RegionName);
                    //}
                    catch (InvalidOperationException e)
                    {
                        if (e.Message != "Closing")
                            m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                    }
                    catch (Exception e)
                    {
                        if (e.Message != "Closing")
                            m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                        break;
                    }
                    finally
                    {
                        LastUpdate = DateTime.UtcNow;
                        m_scene.m_lastupdate = DateTime.UtcNow;
                    }

                    maintc = Util.EnvironmentTickCountSubtract(maintc);
                    maintc = (int)(m_scene.m_timespan * 1000) - maintc;

                    if (maintc > 0)
                        Thread.Sleep(maintc / 2);
                    try
                    {
                        CheckExit();
                    }
                    catch 
                    {
                        break;
                    }
                }
            }
        }

        public class SceneUpdateHeartbeat : IThread
        {
            public SceneUpdateHeartbeat(Scene scene)
            {
                type = "SceneUpdateHeartbeat";
                m_scene = scene;
            }

            public override void Restart()
            {
                ShouldExit = true;
                SceneUpdateHeartbeat heartbeat = new SceneUpdateHeartbeat(m_scene);
                m_scene.tracker.AddSceneHeartbeat(heartbeat, out m_scene.HeartbeatThread);
            }

            /// <summary>
            /// Performs per-frame updates regularly
            /// </summary>
            public override void Start()
            {
                try
                {
                    Update();

                    m_scene.m_lastUpdate = Util.EnvironmentTickCount();
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    m_log.Error("[Scene]: Failed with " + ex);
                }
                FireThreadClosing(this);
            }

            private void CheckExit()
            {
                if (!ShouldExit && !shuttingdown)
                    return;
                throw new Exception("Closing");
            }

            private void Update()
            {
                int maintc;

                while (!ShouldExit)
                {
                    TimeSpan SinceLastFrame = DateTime.UtcNow - m_scene.m_lastupdate;

                    maintc = Util.EnvironmentTickCount();
                    int tmpFrameMS = maintc;
                    m_scene.tempOnRezMS = m_scene.eventMS = m_scene.backupMS = m_scene.terrainMS = m_scene.landMS = 0;

                    // Increment the frame counter
                    ++m_scene.m_frame;

                    try
                    {
                        // Check if any objects have reached their targets
                        m_scene.CheckAtTargets();
                        CheckExit();

                        // Update SceneObjectGroups that have scheduled themselves for updates
                        // Objects queue their updates onto all scene presences
                        if (m_scene.m_frame % m_scene.m_update_objects == 0)
                            m_scene.m_sceneGraph.UpdateObjectGroups();
                        CheckExit();

                        // Run through all ScenePresences looking for updates
                        // Presence updates and queued object updates for each presence are sent to clients
                        if (m_scene.m_frame % m_scene.m_update_presences == 0)
                            m_scene.m_sceneGraph.UpdatePresences();
                        CheckExit();

                        if (m_scene.m_frame % m_scene.m_update_coarse_locations == 0)
                        {
                            List<Vector3> coarseLocations;
                            List<UUID> avatarUUIDs;
                            m_scene.GetCoarseLocations(out coarseLocations, out avatarUUIDs, 60);
                            // Send coarse locations to clients 
                            m_scene.ForEachScenePresence(delegate(ScenePresence presence)
                            {
                                presence.SendCoarseLocations(coarseLocations, avatarUUIDs);
                            });
                        }

                        // Delete temp-on-rez stuff
                        if (m_scene.m_frame % m_scene.m_update_backup == 0)
                        {
                            int tmpTempOnRezMS = Util.EnvironmentTickCount();
                            m_scene.CleanTempObjects();
                            m_scene.tempOnRezMS = Util.EnvironmentTickCountSubtract(tmpTempOnRezMS);
                            m_scene.CheckParcelReturns();
                        }
                    }
                    catch (NotImplementedException)
                    {
                        throw;
                    }
                    catch (AccessViolationException e)
                    {
                        m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                    }
                    //catch (NullReferenceException e)
                    //{
                    //   m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + RegionInfo.RegionName);
                    //}
                    catch (InvalidOperationException e)
                    {
                        m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                    }
                    catch (Exception e)
                    {
                        if (e.Message != "Closing")
                            m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                        break;
                    }

                    maintc = Util.EnvironmentTickCountSubtract(maintc);
                    maintc = (int)(m_scene.m_timespan * 1000) - maintc;

                    if (maintc > 0)
                        Thread.Sleep(maintc / 3);
                    try
                    {
                        CheckExit();
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
        }

        public class SceneHeartbeat : IThread
        {
            public SceneHeartbeat(Scene scene)
            {
                type = "SceneHeartbeat";
                m_scene = scene;
            }

            public override void Restart()
            {
                ShouldExit = true;
                SceneHeartbeat heartbeat = new SceneHeartbeat(m_scene);
                m_scene.tracker.AddSceneHeartbeat(heartbeat, out m_scene.HeartbeatThread);
            }

            /// <summary>
            /// Performs per-frame updates regularly
            /// </summary>
            public override void Start()
            {
                try
                {
                    Update();

                    m_scene.m_lastUpdate = Util.EnvironmentTickCount();
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    m_log.Error("[Scene]: Failed with " + ex);
                }
                FireThreadClosing(this);
            }

            private void CheckExit()
            {
                if (!ShouldExit && !shuttingdown)
                    return;
                throw new Exception("Closing");
            }

            private void Update()
            {
                float physicsFPS;
                int maintc;

                while (!ShouldExit)
                {
                    LastUpdate = DateTime.Now;
                    TimeSpan SinceLastFrame = DateTime.UtcNow - m_scene.m_lastupdate;
                    physicsFPS = 0f;

                    maintc = Util.EnvironmentTickCount();
                    int tmpFrameMS = maintc;
                    m_scene.tempOnRezMS = m_scene.eventMS = m_scene.backupMS = m_scene.terrainMS = m_scene.landMS = 0;

                    // Increment the frame counter
                    ++m_scene.m_frame;

                    try
                    {
                        // Check if any objects have reached their targets
                        m_scene.CheckAtTargets();
                        CheckExit();
                        // Update SceneObjectGroups that have scheduled themselves for updates
                        // Objects queue their updates onto all scene presences
                        if (m_scene.m_frame % m_scene.m_update_objects == 0)
                            m_scene.m_sceneGraph.UpdateObjectGroups();
                        CheckExit();
                        // Run through all ScenePresences looking for updates
                        // Presence updates and queued object updates for each presence are sent to clients
                        if (m_scene.m_frame % m_scene.m_update_presences == 0)
                            m_scene.m_sceneGraph.UpdatePresences();
                        CheckExit();
                        int tmpPhysicsMS2 = Util.EnvironmentTickCount();
                        if ((m_scene.m_frame % m_scene.m_update_physics == 0) && !m_scene.RegionInfo.RegionSettings.DisablePhysics)
                            m_scene.m_sceneGraph.UpdatePreparePhysics();
                        m_scene.physicsMS2 = Util.EnvironmentTickCountSubtract(tmpPhysicsMS2);
                        CheckExit();
                        if (m_scene.m_frame % m_scene.m_update_entitymovement == 0)
                            m_scene.m_sceneGraph.UpdateScenePresenceMovement();

                        int tmpPhysicsMS = Util.EnvironmentTickCount();
                        if (m_scene.m_frame % m_scene.m_update_physics == 0)
                        {
                            if (!m_scene.RegionInfo.RegionSettings.DisablePhysics)
                                physicsFPS = m_scene.m_sceneGraph.UpdatePhysics(Math.Max(SinceLastFrame.TotalSeconds, m_scene.m_timespan));
                        }
                        m_scene.physicsMS = Util.EnvironmentTickCountSubtract(tmpPhysicsMS);
                        CheckExit();
                        // Delete temp-on-rez stuff
                        if (m_scene.m_frame % m_scene.m_update_backup == 0)
                        {
                            int tmpTempOnRezMS = Util.EnvironmentTickCount();
                            m_scene.CleanTempObjects();
                            m_scene.tempOnRezMS = Util.EnvironmentTickCountSubtract(tmpTempOnRezMS);
                            m_scene.CheckParcelReturns();
                        }
                        CheckExit();
                        if (m_scene.m_frame % m_scene.m_update_events == 0)
                            {
                                int evMS = Util.EnvironmentTickCount();
                                m_scene.UpdateEvents();
                                m_scene.eventMS = Util.EnvironmentTickCountSubtract(evMS); ;
                            }

                            if (m_scene.m_frame % m_scene.m_update_backup == 0)
                            {
                                int backMS = Util.EnvironmentTickCount();
                                m_scene.UpdateStorageBackup();
                                m_scene.backupMS = Util.EnvironmentTickCountSubtract(backMS);
                            }

                            if (m_scene.m_frame % m_scene.m_update_terrain == 0)
                            {
                                int terMS = Util.EnvironmentTickCount();
                                m_scene.UpdateTerrain();
                                m_scene.terrainMS = Util.EnvironmentTickCountSubtract(terMS);
                            }

                            if (m_scene.m_frame % m_scene.m_update_land == 0)
                            {
                                int ldMS = Util.EnvironmentTickCount();
                                m_scene.UpdateLand();
                                m_scene.landMS = Util.EnvironmentTickCountSubtract(ldMS);
                            }

                            m_scene.frameMS = Util.EnvironmentTickCountSubtract(tmpFrameMS);
                            m_scene.otherMS = m_scene.tempOnRezMS + m_scene.eventMS + m_scene.backupMS + m_scene.terrainMS + m_scene.landMS;
                            m_scene.lastCompletedFrame = Util.EnvironmentTickCount();

                            m_scene.StatsReporter.AddPhysicsFPS(physicsFPS);
                            m_scene.StatsReporter.AddTimeDilation(m_scene.TimeDilation);
                            m_scene.StatsReporter.AddFPS(1);
                            m_scene.StatsReporter.SetRootAgents(m_scene.m_sceneGraph.GetRootAgentCount());
                            m_scene.StatsReporter.SetChildAgents(m_scene.m_sceneGraph.GetChildAgentCount());
                            m_scene.StatsReporter.SetObjects(m_scene.m_sceneGraph.GetTotalObjectsCount());
                            m_scene.StatsReporter.SetActiveObjects(m_scene.m_sceneGraph.GetActiveObjectsCount());
                            m_scene.StatsReporter.addFrameMS(m_scene.frameMS);
                            m_scene.StatsReporter.addPhysicsMS(m_scene.physicsMS + m_scene.physicsMS2);
                            m_scene.StatsReporter.addOtherMS(m_scene.otherMS);
                            m_scene.StatsReporter.SetActiveScripts(m_scene.m_sceneGraph.GetActiveScriptsCount());
                            m_scene.StatsReporter.addScriptEvents(m_scene.m_sceneGraph.GetScriptEPS());
                        CheckExit();
                    }
                    catch (NotImplementedException)
                    {
                        throw;
                    }
                    catch (AccessViolationException e)
                    {
                        m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                    }
                    //catch (NullReferenceException e)
                    //{
                    //   m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + RegionInfo.RegionName);
                    //}
                    catch (InvalidOperationException e)
                    {
                        m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                    }
                    catch (Exception e)
                    {
                        m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                    }
                    finally
                    {
                        m_scene.m_lastupdate = DateTime.UtcNow;
                    }

                    maintc = Util.EnvironmentTickCountSubtract(maintc);
                    maintc = (int)(m_scene.m_timespan * 1000) - maintc;

                    if (maintc > 0)
                        Thread.Sleep(maintc);
                    try
                    {
                        CheckExit();
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Performs per-frame updates on the scene, this should be the central scene loop
        /// </summary>
        public override void Update()
        {
            float physicsFPS;
            int maintc;

            while (!shuttingdown)
            {
                TimeSpan SinceLastFrame = DateTime.UtcNow - m_lastupdate;
                physicsFPS = 0f;

                maintc = Util.EnvironmentTickCount();
                int tmpFrameMS = maintc;
                tempOnRezMS = eventMS = backupMS = terrainMS = landMS = 0;

                // Increment the frame counter
                ++m_frame;

                try
                {
                    // Check if any objects have reached their targets
                    CheckAtTargets();

                    // Update SceneObjectGroups that have scheduled themselves for updates
                    // Objects queue their updates onto all scene presences
                    if (m_frame % m_update_objects == 0)
                        m_sceneGraph.UpdateObjectGroups();

                    // Run through all ScenePresences looking for updates
                    // Presence updates and queued object updates for each presence are sent to clients
                    if (m_frame % m_update_presences == 0)
                        m_sceneGraph.UpdatePresences();

                    // Coarse locations relate to positions of green dots on the mini-map (on a SecondLife client)
                    if (m_frame % m_update_coarse_locations == 0)
                    {
                        List<Vector3> coarseLocations;
                        List<UUID> avatarUUIDs;
                        GetCoarseLocations(out coarseLocations, out avatarUUIDs, 60);
                        // Send coarse locations to clients 
                        ForEachScenePresence(delegate(ScenePresence presence)
                        {
                            presence.SendCoarseLocations(coarseLocations, avatarUUIDs);
                        });
                    }

                    int tmpPhysicsMS2 = Util.EnvironmentTickCount();
                    if ((m_frame % m_update_physics == 0) && !RegionInfo.RegionSettings.DisablePhysics)
                        m_sceneGraph.UpdatePreparePhysics();
                    physicsMS2 = Util.EnvironmentTickCountSubtract(tmpPhysicsMS2);

                    // Apply any pending avatar force input to the avatar's velocity
                    if (m_frame % m_update_entitymovement == 0)
                        m_sceneGraph.UpdateScenePresenceMovement();

                    // Perform the main physics update.  This will do the actual work of moving objects and avatars according to their
                    // velocity
                    int tmpPhysicsMS = Util.EnvironmentTickCount();
                    if (m_frame % m_update_physics == 0)
                    {
                        if (!RegionInfo.RegionSettings.DisablePhysics)
                            physicsFPS = m_sceneGraph.UpdatePhysics(Math.Max(SinceLastFrame.TotalSeconds, m_timespan));
                        if (SynchronizeScene != null)
                            SynchronizeScene(this);
                    }
                    physicsMS = Util.EnvironmentTickCountSubtract(tmpPhysicsMS);

                    // Delete temp-on-rez stuff
                    if (m_frame % m_update_backup == 0)
                    {
                        int tmpTempOnRezMS = Util.EnvironmentTickCount();
                        CleanTempObjects();
                        tempOnRezMS = Util.EnvironmentTickCountSubtract(tmpTempOnRezMS);
                        CheckParcelReturns();
                    }

                    if (m_frame % m_update_events == 0)
                        {
                            int evMS = Util.EnvironmentTickCount();
                            UpdateEvents();
                            eventMS = Util.EnvironmentTickCountSubtract(evMS); ;
                        }

                        if (m_frame % m_update_backup == 0)
                        {
                            int backMS = Util.EnvironmentTickCount();
                            UpdateStorageBackup();
                            backupMS = Util.EnvironmentTickCountSubtract(backMS);
                        }

                        if (m_frame % m_update_terrain == 0)
                        {
                            int terMS = Util.EnvironmentTickCount();
                            UpdateTerrain();
                            terrainMS = Util.EnvironmentTickCountSubtract(terMS);
                        }

                        if (m_frame % m_update_land == 0)
                        {
                            int ldMS = Util.EnvironmentTickCount();
                            UpdateLand();
                            landMS = Util.EnvironmentTickCountSubtract(ldMS);
                        }

                        frameMS = Util.EnvironmentTickCountSubtract(tmpFrameMS);
                        otherMS = tempOnRezMS + eventMS + backupMS + terrainMS + landMS;
                        lastCompletedFrame = Util.EnvironmentTickCount();

                        StatsReporter.AddPhysicsFPS(physicsFPS);
                        StatsReporter.AddTimeDilation(TimeDilation);
                        StatsReporter.AddFPS(1);
                        StatsReporter.SetRootAgents(m_sceneGraph.GetRootAgentCount());
                        StatsReporter.SetChildAgents(m_sceneGraph.GetChildAgentCount());
                        StatsReporter.SetObjects(m_sceneGraph.GetTotalObjectsCount());
                        StatsReporter.SetActiveObjects(m_sceneGraph.GetActiveObjectsCount());
                        StatsReporter.addFrameMS(frameMS);
                        StatsReporter.addPhysicsMS(physicsMS + physicsMS2);
                        StatsReporter.addOtherMS(otherMS);
                        StatsReporter.SetActiveScripts(m_sceneGraph.GetActiveScriptsCount());
                        StatsReporter.addScriptEvents(m_sceneGraph.GetScriptEPS());
                }
                catch (NotImplementedException)
                {
                    throw;
                }
                catch (AccessViolationException e)
                {
                    m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + RegionInfo.RegionName);
                }
                //catch (NullReferenceException e)
                //{
                //   m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + RegionInfo.RegionName);
                //}
                catch (InvalidOperationException e)
                {
                    m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + RegionInfo.RegionName);
                }
                catch (Exception e)
                {
                    m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + RegionInfo.RegionName);
                }
                finally
                {
                    m_lastupdate = DateTime.UtcNow;
                }

                maintc = Util.EnvironmentTickCountSubtract(maintc);
                maintc = (int)(m_timespan * 1000) - maintc;

                if (maintc > 0)
                    Thread.Sleep(maintc);

                // Tell the watchdog that this thread is still alive
                Watchdog.UpdateThread();
            }
        }

        public void AddGroupTarget(SceneObjectGroup grp)
        {
            lock (m_groupsWithTargets)
                m_groupsWithTargets[grp.UUID] = grp;
        }

        public void RemoveGroupTarget(SceneObjectGroup grp)
        {
            lock (m_groupsWithTargets)
                m_groupsWithTargets.Remove(grp.UUID);
        }

        private void CheckAtTargets()
        {
            lock (m_groupsWithTargets)
            {
                foreach (SceneObjectGroup entry in m_groupsWithTargets.Values)
                {
                    entry.checkAtTargets();
                }
            }
        }


        /// <summary>
        /// Send out simstats data to all clients
        /// </summary>
        /// <param name="stats">Stats on the Simulator's performance</param>
        private void SendSimStatsPackets(SimStats stats)
        {
            ForEachScenePresence(
                delegate(ScenePresence agent)
                {
                    if (!agent.IsChildAgent)
                        agent.ControllingClient.SendSimStats(stats);
                }
            );
        }

        /// <summary>
        /// Recount SceneObjectPart in parcel aabb
        /// </summary>
        private void UpdateLand()
        {
            if (LandChannel != null)
            {
                if (LandChannel.IsLandPrimCountTainted())
                {
                    EventManager.TriggerParcelPrimCountUpdate();
                }
            }
        }

        /// <summary>
        /// Update the terrain if it needs to be updated.
        /// </summary>
        private void UpdateTerrain()
        {
            EventManager.TriggerTerrainTick();
        }

        /// <summary>
        /// Back up queued up changes
        /// </summary>
        private void UpdateStorageBackup()
        {
            if (!m_backingup)
            {
                m_backingup = true;
                Util.FireAndForget(BackupWaitCallback);
            }
        }

        /// <summary>
        /// Sends out the OnFrame event to the modules
        /// </summary>
        private void UpdateEvents()
        {
            m_eventManager.TriggerOnFrame();
        }

        /// <summary>
        /// Wrapper for Backup() that can be called with Util.FireAndForget()
        /// </summary>
        private void BackupWaitCallback(object o)
        {
            Backup(false);
        }
        
        /// <summary>
        /// Backup the scene.  This acts as the main method of the backup thread.
        /// </summary>
        /// <param name="forced">
        /// If true, then any changes that have not yet been persisted are persisted.  If false,
        /// then the persistence decision is left to the backup code (in some situations, such as object persistence,
        /// it's much more efficient to backup multiple changes at once rather than every single one).
        /// <returns></returns>
        public void Backup(bool forced)
        {
            //EventManager.TriggerOnBackup(DataStore);
            ProcessPrimBackupTaints(false);
            m_backingup = false;
        }

        /// <summary>
        /// Synchronous force backup.  For deletes and links/unlinks
        /// </summary>
        /// <param name="group">Object to be backed up</param>
        public void ForceSceneObjectBackup(SceneObjectGroup group)
        {
            if (group != null)
            {
                group.ProcessBackup(SimulationDataService, true);
            }
        }

        /// <summary>
        /// Return object to avatar Message
        /// </summary>
        /// <param name="agentID">Avatar Unique Id</param>
        /// <param name="objectName">Name of object returned</param>
        /// <param name="location">Location of object returned</param>
        /// <param name="reason">Reasion for object return</param>
        public void AddReturn(UUID agentID, string objectName, Vector3 location, string reason, SceneObjectGroup group)
        {
            lock (m_returns)
            {
                if (m_returns.ContainsKey(agentID))
                {
                    ReturnInfo info = m_returns[agentID];
                    info.count++;
                    info.Groups.Add(group);
                    m_returns[agentID] = info;
                }
                else
                {
                    ReturnInfo info = new ReturnInfo();
                    info.count = 1;
                    info.objectName = objectName;
                    info.location = location;
                    info.reason = reason;
                    info.Groups = new List<SceneObjectGroup>();
                    info.Groups.Add(group);
                    m_returns[agentID] = info;
                }
            }
        }

        /// <summary>
        /// Return object to avatar Message
        /// </summary>
        /// <param name="agentID">Avatar Unique Id</param>
        /// <param name="objectName">Name of object returned</param>
        /// <param name="location">Location of object returned</param>
        /// <param name="reason">Reasion for object return</param>
        public void AddReturns(UUID agentID, string objectName, int Count, Vector3 location, string reason, List<SceneObjectGroup> Groups)
        {
            lock (m_returns)
            {
                if (m_returns.ContainsKey(agentID))
                {
                    ReturnInfo info = m_returns[agentID];
                    info.count += Count;
                    info.Groups.AddRange(Groups);
                    m_returns[agentID] = info;
                }
                else
                {
                    ReturnInfo info = new ReturnInfo();
                    info.count = Count;
                    info.objectName = objectName;
                    info.location = location;
                    info.reason = reason;
                    info.Groups = Groups;
                    m_returns[agentID] = info;
                }
            }
        }

        #endregion

        #region Load Terrain

        /// <summary>
        /// Store the terrain in the persistant data store
        /// </summary>
        public void SaveTerrain()
        {
            SimulationDataService.StoreTerrain(Heightmap.GetDoubles(), RegionInfo.RegionID, false);
        }

        /// <summary>
        /// Store the revert terrain in the persistant data store
        /// </summary>
        public void SaveRevertTerrain(ITerrainChannel channel)
        {
            SimulationDataService.StoreTerrain(channel.GetDoubles(), RegionInfo.RegionID, true);
        }

        /// <summary>
        /// Loads the World Revert heightmap
        /// </summary>
        public ITerrainChannel LoadRevertMap()
        {
            try
            {
                double[,] map = SimulationDataService.LoadTerrain(RegionInfo.RegionID, true);
                if (map == null)
                {
                    map = Heightmap.GetDoubles();
                    TerrainChannel channel = new TerrainChannel(map);
                    SaveRevertTerrain(channel);
                    return channel;
                }
                return new TerrainChannel(map);
            }
            catch (Exception e)
            {
                m_log.Warn("[TERRAIN]: Scene.cs: LoadRevertMap() - Failed with exception " + e.ToString());
            }
            return Heightmap;
        }

        /// <summary>
        /// Loads the World heightmap
        /// </summary>
        public override void LoadWorldMap()
        {
            try
            {
                double[,] map = SimulationDataService.LoadTerrain(RegionInfo.RegionID, false);
                if (map == null)
                {
                    m_log.Info("[TERRAIN]: No default terrain. Generating a new terrain.");
                    Heightmap = new TerrainChannel();

                    SimulationDataService.StoreTerrain(Heightmap.GetDoubles(), RegionInfo.RegionID, false);
                }
                else
                {
                    Heightmap = new TerrainChannel(map);
                }
            }
            catch (IOException e)
            {
                m_log.Warn("[TERRAIN]: Scene.cs: LoadWorldMap() - Failed with exception " + e.ToString() + " Regenerating");
                
                // Non standard region size.    If there's an old terrain in the database, it might read past the buffer
                #pragma warning disable 0162
                if ((int)Constants.RegionSize != 256)
                {
                    Heightmap = new TerrainChannel();

                    SimulationDataService.StoreTerrain(Heightmap.GetDoubles(), RegionInfo.RegionID, false);
                }
            }
            catch (Exception e)
            {
                m_log.Warn("[TERRAIN]: Scene.cs: LoadWorldMap() - Failed with exception " + e.ToString());
            }
        }

        /// <summary>
        /// Register this region with a grid service
        /// </summary>
        /// <exception cref="System.Exception">Thrown if registration of the region itself fails.</exception>
        public string RegisterRegionWithGrid()
        {
            m_sceneGridService.SetScene(this);

            // These two 'commands' *must be* next to each other or sim rebooting fails.
            //m_sceneGridService.RegisterRegion(m_interregionCommsOut, RegionInfo);

            GridRegion region = new GridRegion(RegionInfo);
            UUID newSessionID = UUID.Zero;
            string error = GridService.RegisterRegion(RegionInfo.ScopeID, region, RegionInfo.GridSecureSessionID, out newSessionID);
            if (error != String.Empty)
                return error;
            RegionInfo.GridSecureSessionID = newSessionID;
            RegionInfo.WriteNiniConfig();

            m_sceneGridService.SetScene(this);
            m_sceneGridService.InformNeighborsThatRegionisUp(RequestModuleInterface<INeighbourService>(), RegionInfo);
            return "";
        }

        #endregion

        #region Load Land

        /// <summary>
        /// Loads all Parcel data from the datastore for region identified by regionID
        /// </summary>
        /// <param name="regionID">Unique Identifier of the Region to load parcel data for</param>
        public void loadAllLandObjectsFromStorage(UUID regionID)
        {
            m_log.Debug("[SCENE]: Loading Land Objects from database... ");
            IParcelServiceConnector conn = DataManager.RequestPlugin<IParcelServiceConnector>();
            List<LandData> LandObjects = SimulationDataService.LoadLandObjects(regionID);
            if (conn != null)
            {
                if (LandObjects.Count != 0)
                {
                    foreach (LandData land in LandObjects)
                    {
                        //Store it in the new database
                        conn.StoreLandObject(land);
                        //Remove it from the old
                        SimulationDataService.RemoveLandObject(this.RegionInfo.RegionID, land.GlobalID);
                    }
                }
                EventManager.TriggerIncomingLandDataFromStorage(conn.LoadLandObjects(regionID));
            }
            else
                EventManager.TriggerIncomingLandDataFromStorage(LandObjects);
        }

        #endregion

        #region Primitives Methods

        /// <summary>
        /// Loads the World's objects
        /// </summary>
        public virtual void LoadPrimsFromStorage(UUID regionID)
        {
            LoadingPrims = true;
            m_log.Info("[SCENE]: Loading objects from datastore");

            List<SceneObjectGroup> PrimsFromDB = SimulationDataService.LoadObjects(regionID, this);

            foreach (SceneObjectGroup group in PrimsFromDB)
            {
                group.Scene = this;
                EventManager.TriggerOnSceneObjectLoaded(group);
                
                if (group.RootPart == null)
                {
                    m_log.ErrorFormat("[SCENE] Found a SceneObjectGroup with m_rootPart == null and {0} children",
                                      group.ChildrenList.Count);
                    continue;
                }
                AddRestoredSceneObject(group, true, true, true);
                SceneObjectPart rootPart = group.GetChildPart(group.UUID);
                rootPart.Flags &= ~PrimFlags.Scripted;
                rootPart.TrimPermissions();
                group.CheckSculptAndLoad();
                //rootPart.DoPhysicsPropertyUpdate(UsePhysics, true);
            }
            LoadingPrims = false;
            EntityBase[] e = m_sceneGraph.GetEntities();
            m_log.Info("[SCENE]: Loaded " + e.Length.ToString() + " SceneObject(s)");
        }


        /// <summary>
        /// Gets a new rez location based on the raycast and the size of the object that is being rezzed.
        /// </summary>
        /// <param name="RayStart"></param>
        /// <param name="RayEnd"></param>
        /// <param name="RayTargetID"></param>
        /// <param name="rot"></param>
        /// <param name="bypassRayCast"></param>
        /// <param name="RayEndIsIntersection"></param>
        /// <param name="frontFacesOnly"></param>
        /// <param name="scale"></param>
        /// <param name="FaceCenter"></param>
        /// <returns></returns>
        public Vector3 GetNewRezLocation(Vector3 RayStart, Vector3 RayEnd, UUID RayTargetID, Quaternion rot, byte bypassRayCast, byte RayEndIsIntersection, bool frontFacesOnly, Vector3 scale, bool FaceCenter)
        {
            Vector3 pos = Vector3.Zero;
            if (RayEndIsIntersection == (byte)1)
            {
                pos = RayEnd;
                return pos;
            }

            if (RayTargetID != UUID.Zero)
            {
                SceneObjectPart target = GetSceneObjectPart(RayTargetID);

                Vector3 direction = Vector3.Normalize(RayEnd - RayStart);
                Vector3 AXOrigin = new Vector3(RayStart.X, RayStart.Y, RayStart.Z);
                Vector3 AXdirection = new Vector3(direction.X, direction.Y, direction.Z);

                if (target != null)
                {
                    pos = target.AbsolutePosition;
                    //m_log.Info("[OBJECT_REZ]: TargetPos: " + pos.ToString() + ", RayStart: " + RayStart.ToString() + ", RayEnd: " + RayEnd.ToString() + ", Volume: " + Util.GetDistanceTo(RayStart,RayEnd).ToString() + ", mag1: " + Util.GetMagnitude(RayStart).ToString() + ", mag2: " + Util.GetMagnitude(RayEnd).ToString());

                    // TODO: Raytrace better here

                    //EntityIntersection ei = m_sceneGraph.GetClosestIntersectingPrim(new Ray(AXOrigin, AXdirection));
                    Ray NewRay = new Ray(AXOrigin, AXdirection);

                    // Ray Trace against target here
                    EntityIntersection ei = target.TestIntersectionOBB(NewRay, Quaternion.Identity, frontFacesOnly, FaceCenter);

                    // Un-comment out the following line to Get Raytrace results printed to the console.
                    //m_log.Info("[RAYTRACERESULTS]: Hit:" + ei.HitTF.ToString() + " Point: " + ei.ipoint.ToString() + " Normal: " + ei.normal.ToString());
                    float ScaleOffset = 0.5f;

                    // If we hit something
                    if (ei.HitTF)
                    {
                        Vector3 scaleComponent = new Vector3(ei.AAfaceNormal.X, ei.AAfaceNormal.Y, ei.AAfaceNormal.Z);
                        if (scaleComponent.X != 0) ScaleOffset = scale.X;
                        if (scaleComponent.Y != 0) ScaleOffset = scale.Y;
                        if (scaleComponent.Z != 0) ScaleOffset = scale.Z;
                        ScaleOffset = Math.Abs(ScaleOffset);
                        Vector3 intersectionpoint = new Vector3(ei.ipoint.X, ei.ipoint.Y, ei.ipoint.Z);
                        Vector3 normal = new Vector3(ei.normal.X, ei.normal.Y, ei.normal.Z);
                        // Set the position to the intersection point
                        Vector3 offset = (normal * (ScaleOffset / 2f));
                        pos = (intersectionpoint + offset);

                        //Seems to make no sense to do this as this call is used for rezzing from inventory as well, and with inventory items their size is not always 0.5f
                        //And in cases when we weren't rezzing from inventory we were re-adding the 0.25 straight after calling this method
                        // Un-offset the prim (it gets offset later by the consumer method)
                        //pos.Z -= 0.25F; 
                       
                    }

                    return pos;
                }
                else
                {
                    // We don't have a target here, so we're going to raytrace all the objects in the scene.

                    EntityIntersection ei = m_sceneGraph.GetClosestIntersectingPrim(new Ray(AXOrigin, AXdirection), true, false);

                    // Un-comment the following line to print the raytrace results to the console.
                    m_log.Info("[RAYTRACERESULTS]: Hit:" + ei.HitTF.ToString() + " Point: " + ei.ipoint.ToString() + " Normal: " + ei.normal.ToString());

                    if (ei.HitTF)
                    {
                        pos = new Vector3(ei.ipoint.X, ei.ipoint.Y, ei.ipoint.Z);
                    } else
                    {
                        // fall back to our stupid functionality
                        pos = RayEnd;
                    }

                    return pos;
                }
            }
            else
            {
                // fall back to our stupid functionality
                pos = RayEnd;

                //increase height so its above the ground.
                //should be getting the normal of the ground at the rez point and using that?
                pos.Z += scale.Z / 2f;
                return pos;
            }
        }


        /// <summary>
        /// Create a New SceneObjectGroup/Part by raycasting
        /// </summary>
        /// <param name="ownerID"></param>
        /// <param name="groupID"></param>
        /// <param name="RayEnd"></param>
        /// <param name="rot"></param>
        /// <param name="shape"></param>
        /// <param name="bypassRaycast"></param>
        /// <param name="RayStart"></param>
        /// <param name="RayTargetID"></param>
        /// <param name="RayEndIsIntersection"></param>
        public virtual void AddNewPrim(UUID ownerID, UUID groupID, Vector3 RayEnd, Quaternion rot, PrimitiveBaseShape shape,
                                       byte bypassRaycast, Vector3 RayStart, UUID RayTargetID,
                                       byte RayEndIsIntersection)
        {
            Vector3 pos = GetNewRezLocation(RayStart, RayEnd, RayTargetID, rot, bypassRaycast, RayEndIsIntersection, true, new Vector3(0.5f, 0.5f, 0.5f), false);

            string reason; 
            if (Permissions.CanRezObject(1, ownerID, pos, out reason))
            {
                AddNewPrim(ownerID, groupID, pos, rot, shape);
            }
            else
            {
                GetScenePresence(ownerID).ControllingClient.SendAlertMessage("You do not have permission to rez objects here: " + reason);
            }
        }

        public virtual SceneObjectGroup AddNewPrim(
            UUID ownerID, UUID groupID, Vector3 pos, Quaternion rot, PrimitiveBaseShape shape)
        {
            //m_log.DebugFormat(
            //    "[SCENE]: Scene.AddNewPrim() pcode {0} called for {1} in {2}", shape.PCode, ownerID, RegionInfo.RegionName);

            SceneObjectGroup sceneObject = null;
            
            // If an entity creator has been registered for this prim type then use that
            if (m_entityCreators.ContainsKey((PCode)shape.PCode))
            {
                sceneObject = m_entityCreators[(PCode)shape.PCode].CreateEntity(ownerID, groupID, pos, rot, shape);
            }
            else
            {
                // Otherwise, use this default creation code;
                sceneObject = new SceneObjectGroup(ownerID, pos, rot, shape, this);
                //This has to be set, otherwise it will break things like rezzing objects in an area where crossing is disabled, but rez isn't
                sceneObject.m_lastSignificantPosition = pos;

                AddNewSceneObject(sceneObject, true);
                sceneObject.SetGroup(groupID, null);
            }


            return sceneObject;
        }

        /// <summary>
        /// Add an object into the scene that has come from storage
        /// </summary>
        ///
        /// <param name="sceneObject"></param>
        /// <param name="attachToBackup">
        /// If true, changes to the object will be reflected in its persisted data
        /// If false, the persisted data will not be changed even if the object in the scene is changed
        /// </param>
        /// <param name="alreadyPersisted">
        /// If true, we won't persist this object until it changes
        /// If false, we'll persist this object immediately
        /// </param>
        /// <returns>
        /// true if the object was added, false if an object with the same uuid was already in the scene
        /// </returns>
        public bool AddRestoredSceneObject(
            SceneObjectGroup sceneObject, bool attachToBackup, bool alreadyPersisted, bool sendClientUpdates)
        {
            return m_sceneGraph.AddRestoredSceneObject(sceneObject, attachToBackup, alreadyPersisted, sendClientUpdates);
        }

        /// <summary>
        /// Add a newly created object to the scene.  Updates are also sent to viewers.
        /// </summary>
        /// <param name="sceneObject"></param>
        /// <param name="attachToBackup">
        /// If true, the object is made persistent into the scene.
        /// If false, the object will not persist over server restarts
        /// </param>
        public bool AddNewSceneObject(SceneObjectGroup sceneObject, bool attachToBackup)
        {
            return AddNewSceneObject(sceneObject, attachToBackup, true);
        }
        
        /// <summary>
        /// Add a newly created object to the scene
        /// </summary>
        /// <param name="sceneObject"></param>
        /// <param name="attachToBackup">
        /// If true, the object is made persistent into the scene.
        /// If false, the object will not persist over server restarts
        /// </param>
        /// <param name="sendClientUpdates">
        /// If true, updates for the new scene object are sent to all viewers in range.
        /// If false, it is left to the caller to schedule the update
        /// </param>
        public bool AddNewSceneObject(SceneObjectGroup sceneObject, bool attachToBackup, bool sendClientUpdates)
        {
            return m_sceneGraph.AddNewSceneObject(sceneObject, attachToBackup, sendClientUpdates);
        }
        
        /// <summary>
        /// Add a newly created object to the scene.
        /// </summary>
        /// 
        /// This method does not send updates to the client - callers need to handle this themselves.
        /// <param name="sceneObject"></param>
        /// <param name="attachToBackup"></param>
        /// <param name="pos">Position of the object</param>
        /// <param name="rot">Rotation of the object</param>
        /// <param name="vel">Velocity of the object.  This parameter only has an effect if the object is physical</param>
        /// <returns></returns>
        public bool AddNewSceneObject(
            SceneObjectGroup sceneObject, bool attachToBackup, Vector3 pos, Quaternion rot, Vector3 vel)
        {
            return m_sceneGraph.AddNewSceneObject(sceneObject, attachToBackup, pos, rot, vel);
        }

        /// <summary>
        /// Delete every object from the scene.  This does not include attachments worn by avatars.
        /// </summary>
        public void DeleteAllSceneObjects()
        {
            lock (Entities)
            {
                EntityBase[] entities = Entities.GetEntities();
                List<ISceneEntity> ObjectsToDelete = new List<ISceneEntity>();
                foreach (EntityBase e in entities)
                {
                    if (e is SceneObjectGroup)
                    {
                        SceneObjectGroup group = (SceneObjectGroup)e;
                        if (group.IsAttachment)
							continue;

						lock (m_deleting_scene_object)
                        {
                            group.RemoveScriptInstances(true);
                        }

                        foreach (SceneObjectPart part in group.ChildrenList)
                        {
                            if (part.IsJoint() && ((part.ObjectFlags & (uint)PrimFlags.Physics) != 0))
                            {
                                PhysicsScene.RequestJointDeletion(part.Name); // FIXME: what if the name changed?
                            }
                            else if (part.PhysActor != null)
                            {
                                PhysicsScene.RemovePrim(part.PhysActor);
                                part.PhysActor = null;
                            }
                        }
                        
                        m_sceneGraph.DeleteSceneObject(group.UUID, false);
                        EventManager.TriggerObjectBeingRemovedFromScene(group);

                        //Silent true so taht we don't send needless killObjects
                        group.DeleteGroup(true);
                        ObjectsToDelete.Add(group.RootPart);
                    }
                }
                ForEachScenePresence(delegate(ScenePresence avatar)
                {
                    avatar.ControllingClient.SendKillObject(RegionInfo.RegionHandle, ObjectsToDelete.ToArray());
                });

                SimulationDataService.RemoveRegion(m_regInfo.RegionID);
                EventManager.TriggerParcelPrimCountTainted();
            }
        }

        /// <summary>
        /// Synchronously delete the given object from the scene.
        /// </summary>
        /// <param name="group">Object Id</param>
        /// <param name="silent">Suppress broadcasting changes to other clients.</param>
        public void DeleteSceneObject(SceneObjectGroup group, bool silent, bool DeleteScripts)
        {
//            m_log.DebugFormat("[SCENE]: Deleting scene object {0} {1}", group.Name, group.UUID);

            //SceneObjectPart rootPart = group.GetChildPart(group.UUID);

            // Serialise calls to RemoveScriptInstances to avoid
            // deadlocking on m_parts inside SceneObjectGroup
            lock (group.RootPart.SitTargetAvatar)
            {
                if (group.RootPart.SitTargetAvatar.Count != 0)
                {
                    foreach (UUID avID in group.RootPart.SitTargetAvatar)
                    {
                        ScenePresence SP = GetScenePresence(avID);
                        if (SP != null)
                            SP.StandUp();
                    }
                }
            }

            if (DeleteScripts)
            {
                lock (m_deleting_scene_object)
                {
                    group.RemoveScriptInstances(true);
                }
            }
            foreach (SceneObjectPart part in group.ChildrenList)
            {
                if (part.IsJoint() && ((part.ObjectFlags&(uint)PrimFlags.Physics) != 0))
                {
                    PhysicsScene.RequestJointDeletion(part.Name); // FIXME: what if the name changed?
                }
                else if (part.PhysActor != null)
                {
                    PhysicsScene.RemovePrim(part.PhysActor);
                    part.PhysActor = null;
                }
            }

            if (UnlinkSceneObject(group, false))
            {
                EventManager.TriggerObjectBeingRemovedFromScene(group);
                EventManager.TriggerParcelPrimCountTainted();
            }

            group.DeleteGroup(silent);

            //m_log.DebugFormat("[SCENE]: Exit DeleteSceneObject() for {0} {1}", group.Name, group.UUID);
        }

        /// <summary>
        /// Unlink the given object from the scene.  Unlike delete, this just removes the record of the object - the
        /// object itself is not destroyed.
        /// </summary>
        /// <param name="so">The scene object.</param>
        /// <param name="softDelete">If true, only deletes from scene, but keeps the object in the database.</param>
        /// <returns>true if the object was in the scene, false if it was not</returns>
        public bool UnlinkSceneObject(SceneObjectGroup so, bool softDelete)
        {
            DateTime StartTime = DateTime.Now.ToUniversalTime();

            if (m_sceneGraph.DeleteSceneObject(so.UUID, softDelete))
            {
                if (!softDelete)
                {
                    lock (m_needsDeleted)
                    {
                        if (!m_needsDeleted.Contains(so.UUID))
                            m_needsDeleted.Add(so.UUID);
                    }
					// We need to keep track of this state in case this group is still queued for further backup.
                	so.IsDeleted = true;
                    Entities.Remove(so.UUID);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Move the given scene object into a new region depending on which region its absolute position has moved
        /// into.
        ///
        /// </summary>
        /// <param name="attemptedPosition">the attempted out of region position of the scene object</param>
        /// <param name="grp">the scene object that we're crossing</param>
        public void CrossPrimGroupIntoNewRegion(Vector3 attemptedPosition, SceneObjectGroup grp, bool silent)
        {
            if (grp == null)
                return;
            if (grp.IsDeleted)
                return;

            if (grp.RootPart.DIE_AT_EDGE)
            {
                // We remove the object here
                try
                {
                    DeleteSceneObject(grp, false, true);
                }
                catch (Exception)
                {
                    m_log.Warn("[SCENE]: exception when trying to remove the prim that crossed the border.");
                }
                return;
            }

            if (grp.RootPart.RETURN_AT_EDGE)
            {
                // We remove the object here
                try
                {
                    List<SceneObjectGroup> objects = new List<SceneObjectGroup>();
                    objects.Add(grp);
                    SceneObjectGroup[] objectsArray = objects.ToArray();
                    returnObjects(objectsArray, UUID.Zero);
                }
                catch (Exception)
                {
                    m_log.Warn("[SCENE]: exception when trying to return the prim that crossed the border.");
                }
                return;
            }

            if (m_teleportModule != null)
                m_teleportModule.Cross(grp, attemptedPosition, silent);
        }

        public void HandleObjectPermissionsUpdate(IClientAPI controller, UUID agentID, UUID sessionID, byte field, uint localId, uint mask, byte set)
        {
            // Check for spoofing..  since this is permissions we're talking about here!
            if ((controller.SessionId == sessionID) && (controller.AgentId == agentID))
            {
                // Tell the object to do permission update
                if (localId != 0)
                {
                    SceneObjectGroup chObjectGroup = GetGroupByPrim(localId);
                    if (chObjectGroup != null)
                    {
                        if (Permissions.CanEditObject(chObjectGroup.UUID, controller.AgentId))
                            chObjectGroup.UpdatePermissions(agentID, field, localId, mask, set);
                    }
                }
            }
        }

        public Vector3[] GetCombinedBoundingBox(List<SceneObjectGroup> objects, out float minX, out float maxX, out float minY, out float maxY, out float minZ, out float maxZ)
        {
            minX = 256;
            maxX = -256;
            minY = 256;
            maxY = -256;
            minZ = 8192;
            maxZ = -256;

            List<Vector3> offsets = new List<Vector3>();

            foreach (SceneObjectGroup g in objects)
            {
                float ominX, ominY, ominZ, omaxX, omaxY, omaxZ;

                g.GetAxisAlignedBoundingBoxRaw(out ominX, out omaxX, out ominY, out omaxY, out ominZ, out omaxZ);

                if (minX > ominX)
                    minX = ominX;
                if (minY > ominY)
                    minY = ominY;
                if (minZ > ominZ)
                    minZ = ominZ;
                if (maxX < omaxX)
                    maxX = omaxX;
                if (maxY < omaxY)
                    maxY = omaxY;
                if (maxZ < omaxZ)
                    maxZ = omaxZ;
            }

            foreach (SceneObjectGroup g in objects)
            {
                Vector3 vec = g.AbsolutePosition;
                vec.X -= minX;
                vec.Y -= minY;
                vec.Z -= minZ;

                offsets.Add(vec);
            }

            return offsets.ToArray();
        }

        public override ISceneObject DeserializeObject(string representation)
        {
            return SceneObjectSerializer.FromXml2Format(representation, this);
        }

        public void CleanTempObjects()
        {
            EntityBase[] objs = GetEntities();

            foreach (EntityBase obj in objs)
            {
                if (obj is SceneObjectGroup)
                {
                    SceneObjectGroup grp = (SceneObjectGroup)obj;

                    if (!grp.IsDeleted)
                    {
                        if ((grp.RootPart.Flags & PrimFlags.TemporaryOnRez) != 0)
                        {
                            if (grp.RootPart.Expires <= DateTime.Now)
                                DeleteSceneObject(grp, false, true);
                        }
                    }
                }
            }
        }

        public void DeleteFromStorage(UUID uuid)
        {
            lock (m_needsDeleted)
            {
                if (!m_needsDeleted.Contains(uuid))
                    m_needsDeleted.Add(uuid);
            }
        }
        public void ObjectSaleInfo(IClientAPI client, UUID agentID, UUID sessionID, uint localID, byte saleType, int salePrice)
        {
            SceneObjectPart part = GetSceneObjectPart(localID);
            if (part == null || part.ParentGroup == null)
                return;

            if (part.ParentGroup.IsDeleted)
                return;

            if (!Permissions.CanEditObject(part.UUID, agentID))
            {
                m_log.Warn("[Scene]: " + agentID + " attempted to set an object for sale without having the correct permissions.");
                return;
            }

            part = part.ParentGroup.RootPart;

            part.ObjectSaleType = saleType;
            part.SalePrice = salePrice;

            part.ParentGroup.HasGroupChanged = true;

            part.GetProperties(client);
        }

        public bool PerformObjectBuy(IClientAPI remoteClient, UUID categoryID,
                uint localID, byte saleType)
        {
            SceneObjectPart part = GetSceneObjectPart(localID);

            if (part == null)
                return false;

            if (part.ParentGroup == null)
                return false;

            SceneObjectGroup group = part.ParentGroup;

            switch (part.ObjectSaleType)
            {
                case 1: // Sell as original (in-place sale)
                    uint effectivePerms = group.GetEffectivePermissions();

                    if ((effectivePerms & (uint)PermissionMask.Transfer) == 0)
                    {
                        m_dialogModule.SendAlertToUser(remoteClient, "This item doesn't appear to be for sale");
                        return false;
                    }

                    group.SetOwnerId(remoteClient.AgentId);
                    group.SetRootPartOwner(part, remoteClient.AgentId,
                            remoteClient.ActiveGroupId);

                    List<SceneObjectPart> partList =
                        new List<SceneObjectPart>(group.ChildrenList);

                    if (Permissions.PropagatePermissions())
                    {
                        foreach (SceneObjectPart child in partList)
                        {
                            child.Inventory.ChangeInventoryOwner(remoteClient.AgentId);
                            child.TriggerScriptChangedEvent(Changed.OWNER);
                            child.ApplyNextOwnerPermissions();
                        }
                    }

                    part.ObjectSaleType = 0;
                    part.SalePrice = 10;

                    group.HasGroupChanged = true;
                    part.GetProperties(remoteClient);
                    part.TriggerScriptChangedEvent(Changed.OWNER);
                    group.ResumeScripts();
                    part.ScheduleFullUpdate(PrimUpdateFlags.FullUpdate);

                    break;

                case 2: // Sell a copy


                    Vector3 inventoryStoredPosition = new Vector3
                           (((group.AbsolutePosition.X > (int)Constants.RegionSize)
                                 ? 250
                                 : group.AbsolutePosition.X)
                            ,
                            (group.AbsolutePosition.X > (int)Constants.RegionSize)
                                ? 250
                                : group.AbsolutePosition.X,
                            group.AbsolutePosition.Z);

                    Vector3 originalPosition = group.AbsolutePosition;

                    group.AbsolutePosition = inventoryStoredPosition;

                    string sceneObjectXml = SceneObjectSerializer.ToOriginalXmlFormat(group);
                    group.AbsolutePosition = originalPosition;

                    uint perms = group.GetEffectivePermissions();

                    if ((perms & (uint)PermissionMask.Transfer) == 0)
                    {
                        m_dialogModule.SendAlertToUser(remoteClient, "This item doesn't appear to be for sale");
                        return false;
                    }

                    AssetBase asset = CreateAsset(
                        group.GetPartName(localID),
                        group.GetPartDescription(localID),
                        (sbyte)AssetType.Object,
                        Utils.StringToBytes(sceneObjectXml),
                        group.OwnerID);
                    AssetService.Store(asset);

                    InventoryItemBase item = new InventoryItemBase();
                    item.CreatorId = part.CreatorID.ToString();

                    item.ID = UUID.Random();
                    item.Owner = remoteClient.AgentId;
                    item.AssetID = asset.FullID;
                    item.Description = asset.Description;
                    item.Name = asset.Name;
                    item.AssetType = asset.Type;
                    item.InvType = (int)InventoryType.Object;
                    item.Folder = categoryID;

                    uint nextPerms = (perms & 7) << 13;
                    if ((nextPerms & (uint)PermissionMask.Copy) == 0)
                        perms &= ~(uint)PermissionMask.Copy;
                    if ((nextPerms & (uint)PermissionMask.Transfer) == 0)
                        perms &= ~(uint)PermissionMask.Transfer;
                    if ((nextPerms & (uint)PermissionMask.Modify) == 0)
                        perms &= ~(uint)PermissionMask.Modify;

                    item.BasePermissions = perms & part.NextOwnerMask;
                    item.CurrentPermissions = perms & part.NextOwnerMask;
                    item.NextPermissions = part.NextOwnerMask;
                    item.EveryOnePermissions = part.EveryoneMask &
                                               part.NextOwnerMask;
                    item.GroupPermissions = part.GroupMask &
                                               part.NextOwnerMask;
                    item.CurrentPermissions |= 8; // Slam!
                    item.CreationDate = Util.UnixTimeSinceEpoch();

                    if (InventoryService.AddItem(item))
                        remoteClient.SendInventoryItemCreateUpdate(item, 0);
                    else
                    {
                        m_dialogModule.SendAlertToUser(remoteClient, "Cannot buy now. Your inventory is unavailable");
                        return false;
                    }
                    break;

                case 3: // Sell contents
                    List<UUID> invList = part.Inventory.GetInventoryList();

                    bool okToSell = true;

                    foreach (UUID invID in invList)
                    {
                        TaskInventoryItem item1 = part.Inventory.GetInventoryItem(invID);
                        if ((item1.CurrentPermissions &
                                (uint)PermissionMask.Transfer) == 0)
                        {
                            okToSell = false;
                            break;
                        }
                    }

                    if (!okToSell)
                    {
                        m_dialogModule.SendAlertToUser(
                            remoteClient, "This item's inventory doesn't appear to be for sale");
                        return false;
                    }

                    if (invList.Count > 0)
                        MoveTaskInventoryItems(remoteClient.AgentId, part.Name,
                                part, invList);
                    break;
            }

            return true;
        }

        internal void DeleteGroups(List<SceneObjectGroup> objectGroups)
        {
            List<ISceneEntity> DeleteGroups = new List<ISceneEntity>();
            foreach (SceneObjectGroup g in objectGroups)
            {
                DeleteGroups.Add(g.RootPart);
                g.DeleteGroup(true); //WE do the deleting of the prims on the client
            }
            ForEachScenePresence(delegate(ScenePresence avatar)
            {
                avatar.ControllingClient.SendKillObject(RegionInfo.RegionHandle, DeleteGroups.ToArray());
            });
        }

        public Border GetCrossedBorder(Vector3 position, Cardinals gridline)
        {
            if (BordersLocked)
            {
                switch (gridline)
                {
                    case Cardinals.N:
                        lock (NorthBorders)
                        {
                            foreach (Border b in NorthBorders)
                            {
                                if (b.TestCross(position))
                                    return b;
                            }
                        }
                        break;
                    case Cardinals.S:
                        lock (SouthBorders)
                        {
                            foreach (Border b in SouthBorders)
                            {
                                if (b.TestCross(position))
                                    return b;
                            }
                        }

                        break;
                    case Cardinals.E:
                        lock (EastBorders)
                        {
                            foreach (Border b in EastBorders)
                            {
                                if (b.TestCross(position))
                                    return b;
                            }
                        }

                        break;
                    case Cardinals.W:

                        lock (WestBorders)
                        {
                            foreach (Border b in WestBorders)
                            {
                                if (b.TestCross(position))
                                    return b;
                            }
                        }
                        break;

                }
            }
            else
            {
                switch (gridline)
                {
                    case Cardinals.N:
                        foreach (Border b in NorthBorders)
                        {
                            if (b.TestCross(position))
                                return b;
                        }
                       
                        break;
                    case Cardinals.S:
                        foreach (Border b in SouthBorders)
                        {
                            if (b.TestCross(position))
                                return b;
                        }
                        break;
                    case Cardinals.E:
                        foreach (Border b in EastBorders)
                        {
                            if (b.TestCross(position))
                                return b;
                        }

                        break;
                    case Cardinals.W:
                        foreach (Border b in WestBorders)
                        {
                            if (b.TestCross(position))
                                return b;
                        }
                        break;

                }
            }
            

            return null;
        }

        public bool TestBorderCross(Vector3 position, Cardinals border)
        {
            if (BordersLocked)
            {
                switch (border)
                {
                    case Cardinals.N:
                        lock (NorthBorders)
                        {
                            foreach (Border b in NorthBorders)
                            {
                                if (b.TestCross(position))
                                    return true;
                            }
                        }
                        break;
                    case Cardinals.E:
                        lock (EastBorders)
                        {
                            foreach (Border b in EastBorders)
                            {
                                if (b.TestCross(position))
                                    return true;
                            }
                        }
                        break;
                    case Cardinals.S:
                        lock (SouthBorders)
                        {
                            foreach (Border b in SouthBorders)
                            {
                                if (b.TestCross(position))
                                    return true;
                            }
                        }
                        break;
                    case Cardinals.W:
                        lock (WestBorders)
                        {
                            foreach (Border b in WestBorders)
                            {
                                if (b.TestCross(position))
                                    return true;
                            }
                        }
                        break;
                }
            }
            else
            {
                switch (border)
                {
                    case Cardinals.N:
                        foreach (Border b in NorthBorders)
                        {
                            if (b.TestCross(position))
                                return true;
                        }
                        break;
                    case Cardinals.E:
                        foreach (Border b in EastBorders)
                        {
                            if (b.TestCross(position))
                                return true;
                        }
                        break;
                    case Cardinals.S:
                        foreach (Border b in SouthBorders)
                        {
                            if (b.TestCross(position))
                                return true;
                        }
                        break;
                    case Cardinals.W:
                        foreach (Border b in WestBorders)
                        {
                            if (b.TestCross(position))
                                return true;
                        }
                        break;
                }
            }
            return false;
        }


        /// <summary>
        /// Called when objects or attachments cross the border, or teleport, between regions.
        /// </summary>
        /// <param name="sog"></param>
        /// <returns></returns>
        public bool IncomingCreateObject(ISceneObject sog)
        {
            //m_log.Debug(" >>> IncomingCreateObject(sog) <<< " + ((SceneObjectGroup)sog).AbsolutePosition + " deleted? " + ((SceneObjectGroup)sog).IsDeleted);
            SceneObjectGroup newObject;
            try
            {
                newObject = (SceneObjectGroup)sog;
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[SCENE]: Problem casting object: {0}", e.Message);
                return false;
            }
            if (newObject.LocalId == 0)
            {
                newObject.ResetIDs();
                newObject.AttachToScene(this);
            }
            if (!AddSceneObject(newObject))
            {
                m_log.DebugFormat("[SCENE]: Problem adding scene object {0} in {1} ", sog.UUID, RegionInfo.RegionName);
                return false;
            }
            
            newObject.RootPart.ParentGroup.CreateScriptInstances(0, false, DefaultScriptEngine, 1, UUID.Zero);
            newObject.RootPart.ParentGroup.ResumeScripts();

            // Do this as late as possible so that listeners have full access to the incoming object
            EventManager.TriggerOnIncomingSceneObject(newObject);

            TriggerChangedTeleport(newObject);

            if (newObject.RootPart.SitTargetAvatar.Count != 0)
            {
                lock (newObject.RootPart.SitTargetAvatar)
                {
                    foreach (UUID avID in newObject.RootPart.SitTargetAvatar)
                    {
                        ScenePresence SP = GetScenePresence(avID);
                        while (SP == null)
                        {
                            Thread.Sleep(20);
                        }
                        SP.AbsolutePosition = newObject.AbsolutePosition;
                        SP.CrossSittingAgent(SP.ControllingClient, newObject.RootPart.UUID);
                    }
                }
            }
            
            return true;
        }

        /// <summary>
        /// Called when objects or attachments cross the border, or teleport, between regions.
        /// </summary>
        /// <param name="sog"></param>
        /// <returns></returns>
        public bool IncomingCreateObject(ISceneObject sog, ISceneObject oldsog)
        {
            //m_log.Debug(" >>> IncomingCreateObject(sog) <<< " + ((SceneObjectGroup)sog).AbsolutePosition + " deleted? " + ((SceneObjectGroup)sog).IsDeleted);
            SceneObjectGroup newObject;
            SceneObjectGroup oldObject = (SceneObjectGroup)oldsog;
            try
            {
                newObject = (SceneObjectGroup)sog;
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[SCENE]: Problem casting object: {0}", e.Message);
                return false;
            }
            if (!AddSceneObject(newObject))
            {
                m_log.DebugFormat("[SCENE]: Problem adding scene object {0} in {1} ", sog.UUID, RegionInfo.RegionName);
                return false;
            }

            newObject.RootPart.ParentGroup.CreateScriptInstances(0, false, DefaultScriptEngine, 1, UUID.Zero);
            newObject.RootPart.ParentGroup.ResumeScripts();

            // Do this as late as possible so that listeners have full access to the incoming object
            EventManager.TriggerOnIncomingSceneObject(newObject);

            TriggerChangedTeleport(newObject);

            if (newObject.RootPart.SitTargetAvatar.Count != 0)
            {
                lock (newObject.RootPart.SitTargetAvatar)
                {
                    foreach (UUID avID in newObject.RootPart.SitTargetAvatar)
                    {
                        ScenePresence SP = GetScenePresence(avID);
                        while (SP == null)
                        {
                            Thread.Sleep(20);
                        }
                        SP.AbsolutePosition = newObject.AbsolutePosition;
                        SP.CrossSittingAgent(SP.ControllingClient, newObject.RootPart.UUID);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Attachment rezzing
        /// </summary>
        /// <param name="userID">Agent Unique ID</param>
        /// <param name="itemID">Object ID</param>
        /// <returns>False</returns>
        public virtual bool IncomingCreateObject(UUID userID, UUID itemID)
        {
            //m_log.DebugFormat(" >>> IncomingCreateObject(userID, itemID) <<< {0} {1}", userID, itemID);
            
            ScenePresence sp = GetScenePresence(userID);
            if (sp != null && AttachmentsModule != null)
            {
                int attPt = sp.Appearance.GetAttachpoint(itemID);
                AttachmentsModule.RezSingleAttachmentFromInventory(sp.ControllingClient, itemID, attPt);
            }

            return false;
        }

        /// <summary>
        /// Adds a Scene Object group to the Scene.
        /// Verifies that the creator of the object is not banned from the simulator.
        /// Checks if the item is an Attachment
        /// </summary>
        /// <param name="sceneObject"></param>
        /// <returns>True if the SceneObjectGroup was added, False if it was not</returns>
        public bool AddSceneObject(SceneObjectGroup sceneObject)
        {
            // If the user is banned, we won't let any of their objects
            // enter. Period.
            //
            if (m_regInfo.EstateSettings.IsBanned(sceneObject.OwnerID))
            {
                m_log.Info("[INTERREGION]: Denied prim crossing for " +
                        "banned avatar");

                return false;
            }

            // Force allocation of new LocalId
            //
            foreach (SceneObjectPart p in sceneObject.ChildrenList)
                p.LocalId = 0;

            if (sceneObject.IsAttachmentCheckFull()) // Attachment
            {
                sceneObject.RootPart.AddFlag(PrimFlags.TemporaryOnRez);
                sceneObject.RootPart.AddFlag(PrimFlags.Phantom);
                AddRestoredSceneObject(sceneObject, false, false, true);

                // Handle attachment special case
                SceneObjectPart RootPrim = sceneObject.RootPart;

                // Fix up attachment Parent Local ID
                ScenePresence sp = GetScenePresence(sceneObject.OwnerID);

                if (sp != null)
                {
                    SceneObjectGroup grp = sceneObject;

                    m_log.DebugFormat(
                        "[ATTACHMENT]: Received attachment {0}, inworld asset id {1}", grp.GetFromItemID(), grp.UUID);
                    m_log.DebugFormat(
                        "[ATTACHMENT]: Attach to avatar {0} at position {1}", sp.UUID, grp.AbsolutePosition);

                    if (AttachmentsModule != null)
                        AttachmentsModule.AttachObject(sp.ControllingClient, grp.LocalId, 0, false);
                
                    RootPrim.RemFlag(PrimFlags.TemporaryOnRez);
                    grp.SendGroupFullUpdate(PrimUpdateFlags.FullUpdate);
                }
                else
                {
                    RootPrim.RemFlag(PrimFlags.TemporaryOnRez);
                    RootPrim.AddFlag(PrimFlags.TemporaryOnRez);
                }
            }
            else
            {
                AddRestoredSceneObject(sceneObject, true, false, true);

                if (!Permissions.CanObjectEntry(sceneObject.UUID,
                        true, sceneObject.AbsolutePosition))
                {
                    // Deny non attachments based on parcel settings
                    //
                    m_log.Info("[INTERREGION]: Denied prim crossing " +
                            "because of parcel settings");

                    DeleteSceneObject(sceneObject, false, true);

                    return false;
                }
            }

            return true;
        }

        private void TriggerChangedTeleport(SceneObjectGroup sog)
        {
            ScenePresence sp = GetScenePresence(sog.OwnerID);

            if (sp != null)
            {
                AgentCircuitData aCircuit = m_authenticateHandler.GetAgentCircuitData(sp.UUID);

                if (aCircuit != null && (aCircuit.teleportFlags != (uint)TeleportFlags.Default))
                {
                    // This will get your attention
                    //m_log.Error("[XXX] Triggering ");

                    // Trigger CHANGED_TELEPORT
                    foreach (SceneObjectPart part in sog.ChildrenList)
                    {
                        sp.Scene.EventManager.TriggerOnScriptChangedEvent(part, (uint)Changed.TELEPORT);
                    }
                }

            }
        }

        #endregion

        #region Add/Remove Avatar Methods

        /// <summary>
        /// Adding a New Client and Create a Presence for it.
        /// </summary>
        /// <param name="client"></param>
        public override void AddNewClient(IClientAPI client)
        {
            AgentCircuitData aCircuit = m_authenticateHandler.GetAgentCircuitData(client.CircuitCode);
            bool vialogin = false;

            if (aCircuit == null) // no good, didn't pass NewUserConnection successfully
                return;

            m_clientManager.Add(client);
            SubscribeToClientEvents(client);

            // Do the verification here
            System.Net.IPEndPoint ep = (System.Net.IPEndPoint)client.GetClientEP();
            if (!VerifyClient(aCircuit, ep, out vialogin))
            {
                // uh-oh, this is fishy
                m_log.WarnFormat("[Scene]: Agent {0} with session {1} connecting with unidentified end point {2}. Refusing service.",
                    client.AgentId, client.SessionId, ep.ToString());
                try
                {
                    client.Close();
                }
                catch (Exception e)
                {
                    m_log.DebugFormat("[Scene]: Exception while closing aborted client: {0}", e.StackTrace);
                }
                return;
            }

            //m_log.Debug("[Scene] Adding new agent " + client.Name + " to scene " + RegionInfo.RegionName);

            ScenePresence sp = CreateAndAddScenePresence(client);
            if (aCircuit != null)
                sp.Appearance = aCircuit.Appearance;

            // HERE!!! Do the initial attachments right here
            // first agent upon login is a root agent by design.
            // All other AddNewClient calls find aCircuit.child to be true
            if (aCircuit.child == false)
            {
                sp.IsChildAgent = false;
                Util.FireAndForget(delegate(object o) { sp.RezAttachments(); });
            }

            if (GetScenePresence(client.AgentId) != null)
            {
                m_LastLogin = Util.EnvironmentTickCount();
                EventManager.TriggerOnNewClient(client);
                if (vialogin)
                    EventManager.TriggerOnClientLogin(client);
            }

            if (!sp.IsChildAgent && Stats is OpenSim.Framework.Statistics.SimExtraStatsCollector)
            {
                OpenSim.Framework.Statistics.SimExtraStatsCollector stats = Stats as OpenSim.Framework.Statistics.SimExtraStatsCollector;
                stats.AddSuccessfulLogin();
            }
        }

        private bool VerifyClient(AgentCircuitData aCircuit, System.Net.IPEndPoint ep, out bool vialogin)
        {
            vialogin = false;
            
            // Do the verification here
            if ((aCircuit.teleportFlags & (uint)Constants.TeleportFlags.ViaHGLogin) != 0)
            {
                m_log.DebugFormat("[SCENE]: Incoming client {0} {1} in region {2} via HG login", aCircuit.firstname, aCircuit.lastname, RegionInfo.RegionName);
                vialogin = true;
                IUserAgentVerificationModule userVerification = RequestModuleInterface<IUserAgentVerificationModule>();
                if (userVerification != null && ep != null)
                {
                    if (!userVerification.VerifyClient(aCircuit, ep.Address.ToString()))
                    {
                        // uh-oh, this is fishy
                        m_log.DebugFormat("[SCENE]: User Client Verification for {0} {1} in {2} returned false", aCircuit.firstname, aCircuit.lastname, RegionInfo.RegionName);
                        return false;
                    }
                    else
                        m_log.DebugFormat("[SCENE]: User Client Verification for {0} {1} in {2} returned true", aCircuit.firstname, aCircuit.lastname, RegionInfo.RegionName);
                }
            }

            else if ((aCircuit.teleportFlags & (uint)Constants.TeleportFlags.ViaLogin) != 0)
            {
                m_log.DebugFormat("[SCENE]: Incoming client {0} {1} in region {2} via regular login. Client IP verification not performed.",
                    aCircuit.firstname, aCircuit.lastname, RegionInfo.RegionName);
                vialogin = true;
            }

            return true;
        }

        // Called by Caps, on the first HTTP contact from the client
        public override bool CheckClient(UUID agentID, System.Net.IPEndPoint ep)
        {
            AgentCircuitData aCircuit = m_authenticateHandler.GetAgentCircuitData(agentID);
            if (aCircuit != null)
            {
                bool vialogin = false;
                if (!VerifyClient(aCircuit, ep, out vialogin))
                {
                    // if it doesn't pass, we remove the agentcircuitdata altogether
                    // and the scene presence and the client, if they exist
                    try
                    {
                        ScenePresence sp = GetScenePresence(agentID);
                        PresenceService.LogoutAgent(sp.ControllingClient.SessionId);
                        
                        if (sp != null)
                            sp.ControllingClient.Close();

                        // BANG! SLASH!
                        m_authenticateHandler.RemoveCircuit(agentID);

                        return false;
                    }
                    catch (Exception e)
                    {
                        m_log.DebugFormat("[SCENE]: Exception while closing aborted client: {0}", e.StackTrace);
                    }
                }
                else
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Register for events from the client
        /// </summary>
        /// <param name="client">The IClientAPI of the connected client</param>
        public virtual void SubscribeToClientEvents(IClientAPI client)
        {
            SubscribeToClientTerrainEvents(client);
            SubscribeToClientPrimEvents(client);
            SubscribeToClientPrimRezEvents(client);
            SubscribeToClientInventoryEvents(client);
            SubscribeToClientTeleportEvents(client);
            SubscribeToClientScriptEvents(client);
            SubscribeToClientParcelEvents(client);
            SubscribeToClientGridEvents(client);
            SubscribeToClientNetworkEvents(client);
        }

        public virtual void SubscribeToClientTerrainEvents(IClientAPI client)
        {
            client.OnRegionHandShakeReply += SendLayerData;
        }
        
        public virtual void SubscribeToClientPrimEvents(IClientAPI client)
        {
            client.OnUpdatePrimGroupPosition += m_sceneGraph.UpdatePrimPosition;
            client.OnUpdatePrimSinglePosition += m_sceneGraph.UpdatePrimSinglePosition;
            client.OnUpdatePrimGroupRotation += m_sceneGraph.UpdatePrimRotation;
            client.OnUpdatePrimGroupMouseRotation += m_sceneGraph.UpdatePrimRotation;
            client.OnUpdatePrimSingleRotation += m_sceneGraph.UpdatePrimSingleRotation;
            client.OnUpdatePrimSingleRotationPosition += m_sceneGraph.UpdatePrimSingleRotationPosition;
            client.OnUpdatePrimScale += m_sceneGraph.UpdatePrimScale;
            client.OnUpdatePrimGroupScale += m_sceneGraph.UpdatePrimGroupScale;
            client.OnUpdateExtraParams += m_sceneGraph.UpdateExtraParam;
            client.OnUpdatePrimShape += m_sceneGraph.UpdatePrimShape;
            client.OnUpdatePrimTexture += m_sceneGraph.UpdatePrimTexture;
            client.OnObjectRequest += RequestPrim;
            client.OnObjectSelect += SelectPrim;
            client.OnObjectDeselect += DeselectPrim;
            client.OnGrabUpdate += m_sceneGraph.MoveObject;
            client.OnSpinStart += m_sceneGraph.SpinStart;
            client.OnSpinUpdate += m_sceneGraph.SpinObject;
            client.OnDeRezObject += DeRezObjects;
            
            client.OnObjectName += m_sceneGraph.PrimName;
            client.OnObjectClickAction += m_sceneGraph.PrimClickAction;
            client.OnObjectMaterial += m_sceneGraph.PrimMaterial;
            client.OnLinkObjects += LinkObjects;
            client.OnDelinkObjects += DelinkObjects;
            client.OnObjectDuplicate += m_sceneGraph.DuplicateObject;
            client.OnUpdatePrimFlags += m_sceneGraph.UpdatePrimFlags;
            client.OnRequestObjectPropertiesFamily += m_sceneGraph.RequestObjectPropertiesFamily;
            client.OnObjectPermissions += HandleObjectPermissionsUpdate;
            client.OnGrabObject += ProcessObjectGrab;
            client.OnGrabUpdate += ProcessObjectGrabUpdate; 
            client.OnDeGrabObject += ProcessObjectDeGrab;
            client.OnUndo += m_sceneGraph.HandleUndo;
            client.OnRedo += m_sceneGraph.HandleRedo;
            client.OnObjectDescription += m_sceneGraph.PrimDescription;
            client.OnObjectDrop += m_sceneGraph.DropObject;
            client.OnObjectIncludeInSearch += m_sceneGraph.MakeObjectSearchable;
            client.OnObjectOwner += ObjectOwner;
        }

        public virtual void SubscribeToClientPrimRezEvents(IClientAPI client)
        {
            client.OnAddPrim += AddNewPrim;
            client.OnRezObject += RezObject;
            client.OnObjectDuplicateOnRay += doObjectDuplicateOnRay;
        }

        public virtual void SubscribeToClientInventoryEvents(IClientAPI client)
        {
            client.OnCreateNewInventoryItem += CreateNewInventoryItem;
            client.OnLinkInventoryItem += HandleLinkInventoryItem;
            client.OnCreateNewInventoryFolder += HandleCreateInventoryFolder;
            client.OnUpdateInventoryFolder += HandleUpdateInventoryFolder;
            client.OnMoveInventoryFolder += HandleMoveInventoryFolder; // 2; //!!
            client.OnFetchInventoryDescendents += HandleFetchInventoryDescendents;
            client.OnPurgeInventoryDescendents += HandlePurgeInventoryDescendents; // 2; //!!
            client.OnFetchInventory += HandleFetchInventory;
            client.OnUpdateInventoryItem += UpdateInventoryItemAsset;
            client.OnChangeInventoryItemFlags += ChangeInventoryItemFlags;
            client.OnCopyInventoryItem += CopyInventoryItem;
            client.OnMoveInventoryItem += MoveInventoryItem;
            client.OnRemoveInventoryItem += RemoveInventoryItem;
            client.OnRemoveInventoryFolder += RemoveInventoryFolder;
            client.OnRezScript += RezScript;
            client.OnRequestTaskInventory += RequestTaskInventory;
            client.OnRemoveTaskItem += RemoveTaskInventory;
            client.OnUpdateTaskInventory += UpdateTaskInventory;
            client.OnMoveTaskItem += ClientMoveTaskInventoryItem;
        }

        public virtual void SubscribeToClientTeleportEvents(IClientAPI client)
        {
            client.OnTeleportLocationRequest += RequestTeleportLocation;
            client.OnTeleportLandmarkRequest += RequestTeleportLandmark;
            client.OnTeleportCancel += new TeleportCancel(client_OnTeleportCancel);
        }

        public virtual void SubscribeToClientScriptEvents(IClientAPI client)
        {
            client.OnScriptReset += ProcessScriptReset;
            client.OnGetScriptRunning += GetScriptRunning;
            client.OnSetScriptRunning += SetScriptRunning;
        }

        public virtual void SubscribeToClientParcelEvents(IClientAPI client)
        {
            client.OnObjectGroupRequest += m_sceneGraph.HandleObjectGroupUpdate;
            client.OnParcelReturnObjectsRequest += LandChannel.ReturnObjectsInParcel;
            client.OnParcelSetOtherCleanTime += LandChannel.SetParcelOtherCleanTime;
            client.OnParcelBuy += ProcessParcelBuy;
        }

        public virtual void SubscribeToClientGridEvents(IClientAPI client)
        {
            client.OnNameFromUUIDRequest += HandleUUIDNameRequest;
            client.OnMoneyTransferRequest += ProcessMoneyTransferRequest;
            client.OnAvatarPickerRequest += ProcessAvatarPickerRequest;
            client.OnSetStartLocationRequest += SetHomeRezPoint;
            client.OnRegionHandleRequest += RegionHandleRequest;
        }
        
        public virtual void SubscribeToClientNetworkEvents(IClientAPI client)
        {
            client.OnNetworkStatsUpdate += StatsReporter.AddPacketsStats;
            client.OnViewerEffect += ProcessViewerEffect;
        }

        /// <summary>
        /// Unsubscribe the client from events.
        /// </summary>
        /// FIXME: Not called anywhere!
        /// <param name="client">The IClientAPI of the client</param>
        public virtual void UnSubscribeToClientEvents(IClientAPI client)
        {
            UnSubscribeToClientTerrainEvents(client);
            UnSubscribeToClientPrimEvents(client);
            UnSubscribeToClientPrimRezEvents(client);
            UnSubscribeToClientInventoryEvents(client);
            UnSubscribeToClientTeleportEvents(client);
            UnSubscribeToClientScriptEvents(client);
            UnSubscribeToClientParcelEvents(client);
            UnSubscribeToClientGridEvents(client);
            UnSubscribeToClientNetworkEvents(client);
        }

        public virtual void UnSubscribeToClientTerrainEvents(IClientAPI client)
        {
            client.OnRegionHandShakeReply -= SendLayerData;
        }

        public virtual void UnSubscribeToClientPrimEvents(IClientAPI client)
        {
            client.OnUpdatePrimGroupPosition -= m_sceneGraph.UpdatePrimPosition;
            client.OnUpdatePrimSinglePosition -= m_sceneGraph.UpdatePrimSinglePosition;
            client.OnUpdatePrimGroupRotation -= m_sceneGraph.UpdatePrimRotation;
            client.OnUpdatePrimGroupMouseRotation -= m_sceneGraph.UpdatePrimRotation;
            client.OnUpdatePrimSingleRotation -= m_sceneGraph.UpdatePrimSingleRotation;
            client.OnUpdatePrimSingleRotationPosition -= m_sceneGraph.UpdatePrimSingleRotationPosition;
            client.OnUpdatePrimScale -= m_sceneGraph.UpdatePrimScale;
            client.OnUpdatePrimGroupScale -= m_sceneGraph.UpdatePrimGroupScale;
            client.OnUpdateExtraParams -= m_sceneGraph.UpdateExtraParam;
            client.OnUpdatePrimShape -= m_sceneGraph.UpdatePrimShape;
            client.OnUpdatePrimTexture -= m_sceneGraph.UpdatePrimTexture;
            client.OnObjectRequest -= RequestPrim;
            client.OnObjectSelect -= SelectPrim;
            client.OnObjectDeselect -= DeselectPrim;
            client.OnGrabUpdate -= m_sceneGraph.MoveObject;
            client.OnSpinStart -= m_sceneGraph.SpinStart;
            client.OnSpinUpdate -= m_sceneGraph.SpinObject;
            client.OnDeRezObject -= DeRezObjects;
            client.OnObjectName -= m_sceneGraph.PrimName;
            client.OnObjectClickAction -= m_sceneGraph.PrimClickAction;
            client.OnObjectMaterial -= m_sceneGraph.PrimMaterial;
            client.OnLinkObjects -= LinkObjects;
            client.OnDelinkObjects -= DelinkObjects;
            client.OnObjectDuplicate -= m_sceneGraph.DuplicateObject;
            client.OnUpdatePrimFlags -= m_sceneGraph.UpdatePrimFlags;
            client.OnRequestObjectPropertiesFamily -= m_sceneGraph.RequestObjectPropertiesFamily;
            client.OnObjectPermissions -= HandleObjectPermissionsUpdate;
            client.OnGrabObject -= ProcessObjectGrab;
            client.OnDeGrabObject -= ProcessObjectDeGrab;
            client.OnUndo -= m_sceneGraph.HandleUndo;
            client.OnRedo -= m_sceneGraph.HandleRedo;
            client.OnObjectDescription -= m_sceneGraph.PrimDescription;
            client.OnObjectDrop -= m_sceneGraph.DropObject;
            client.OnObjectIncludeInSearch -= m_sceneGraph.MakeObjectSearchable;
            client.OnObjectOwner -= ObjectOwner;
        }

        public virtual void UnSubscribeToClientPrimRezEvents(IClientAPI client)
        {
            client.OnAddPrim -= AddNewPrim;
            client.OnRezObject -= RezObject;
            client.OnObjectDuplicateOnRay -= doObjectDuplicateOnRay;
        }

        public virtual void UnSubscribeToClientInventoryEvents(IClientAPI client)
        {
            client.OnCreateNewInventoryItem -= CreateNewInventoryItem;
            client.OnCreateNewInventoryFolder -= HandleCreateInventoryFolder;
            client.OnUpdateInventoryFolder -= HandleUpdateInventoryFolder;
            client.OnMoveInventoryFolder -= HandleMoveInventoryFolder; // 2; //!!
            client.OnFetchInventoryDescendents -= HandleFetchInventoryDescendents;
            client.OnPurgeInventoryDescendents -= HandlePurgeInventoryDescendents; // 2; //!!
            client.OnFetchInventory -= HandleFetchInventory;
            client.OnUpdateInventoryItem -= UpdateInventoryItemAsset;
            client.OnCopyInventoryItem -= CopyInventoryItem;
            client.OnMoveInventoryItem -= MoveInventoryItem;
            client.OnRemoveInventoryItem -= RemoveInventoryItem;
            client.OnRemoveInventoryFolder -= RemoveInventoryFolder;
            client.OnRezScript -= RezScript;
            client.OnRequestTaskInventory -= RequestTaskInventory;
            client.OnRemoveTaskItem -= RemoveTaskInventory;
            client.OnUpdateTaskInventory -= UpdateTaskInventory;
            client.OnMoveTaskItem -= ClientMoveTaskInventoryItem;
        }

        public virtual void UnSubscribeToClientTeleportEvents(IClientAPI client)
        {
            client.OnTeleportLocationRequest -= RequestTeleportLocation;
            client.OnTeleportLandmarkRequest -= RequestTeleportLandmark;
            //client.OnTeleportHomeRequest -= TeleportClientHome;
        }

        public virtual void UnSubscribeToClientScriptEvents(IClientAPI client)
        {
            client.OnScriptReset -= ProcessScriptReset;
            client.OnGetScriptRunning -= GetScriptRunning;
            client.OnSetScriptRunning -= SetScriptRunning;
        }

        public virtual void UnSubscribeToClientParcelEvents(IClientAPI client)
        {
            client.OnObjectGroupRequest -= m_sceneGraph.HandleObjectGroupUpdate;
            client.OnParcelReturnObjectsRequest -= LandChannel.ReturnObjectsInParcel;
            client.OnParcelSetOtherCleanTime -= LandChannel.SetParcelOtherCleanTime;
            client.OnParcelBuy -= ProcessParcelBuy;
        }

        public virtual void UnSubscribeToClientGridEvents(IClientAPI client)
        {
            client.OnNameFromUUIDRequest -= HandleUUIDNameRequest;
            client.OnMoneyTransferRequest -= ProcessMoneyTransferRequest;
            client.OnAvatarPickerRequest -= ProcessAvatarPickerRequest;
            client.OnSetStartLocationRequest -= SetHomeRezPoint;
            client.OnRegionHandleRequest -= RegionHandleRequest;
        }

        public virtual void UnSubscribeToClientNetworkEvents(IClientAPI client)
        {
            client.OnNetworkStatsUpdate -= StatsReporter.AddPacketsStats;
            client.OnViewerEffect -= ProcessViewerEffect;
        }

        /// <summary>
        /// Teleport an avatar to their home region
        /// </summary>
        /// <param name="agentId">The avatar's Unique ID</param>
        /// <param name="client">The IClientAPI for the client</param>
        public virtual void TeleportClientHome(UUID agentId, IClientAPI client)
        {
            if (m_teleportModule != null)
                m_teleportModule.TeleportHome(agentId, client);
            else
            {
                m_log.DebugFormat("[SCENE]: Unable to teleport user home: no AgentTransferModule is active");
                client.SendTeleportFailed("Unable to perform teleports on this simulator.");
            }
        }

        /// <summary>
        /// Duplicates object specified by localID at position raycasted against RayTargetObject using 
        /// RayEnd and RayStart to determine what the angle of the ray is
        /// </summary>
        /// <param name="localID">ID of object to duplicate</param>
        /// <param name="dupeFlags"></param>
        /// <param name="AgentID">Agent doing the duplication</param>
        /// <param name="GroupID">Group of new object</param>
        /// <param name="RayTargetObj">The target of the Ray</param>
        /// <param name="RayEnd">The ending of the ray (farthest away point)</param>
        /// <param name="RayStart">The Beginning of the ray (closest point)</param>
        /// <param name="BypassRaycast">Bool to bypass raycasting</param>
        /// <param name="RayEndIsIntersection">The End specified is the place to add the object</param>
        /// <param name="CopyCenters">Position the object at the center of the face that it's colliding with</param>
        /// <param name="CopyRotates">Rotate the object the same as the localID object</param>
        public void doObjectDuplicateOnRay(uint localID, uint dupeFlags, UUID AgentID, UUID GroupID,
                                           UUID RayTargetObj, Vector3 RayEnd, Vector3 RayStart,
                                           bool BypassRaycast, bool RayEndIsIntersection, bool CopyCenters, bool CopyRotates)
        {
            Vector3 pos;
            const bool frontFacesOnly = true;
            //m_log.Info("HITTARGET: " + RayTargetObj.ToString() + ", COPYTARGET: " + localID.ToString());
            SceneObjectPart target = GetSceneObjectPart(localID);
            SceneObjectPart target2 = GetSceneObjectPart(RayTargetObj);
            ScenePresence Sp = GetScenePresence(AgentID);
            if (target != null && target2 != null)
            {
                if (EnableFakeRaycasting)
                {
                    RayStart = Sp.CameraPosition;
                    RayEnd = pos = target2.AbsolutePosition;
                }
                Vector3 direction = Vector3.Normalize(RayEnd - RayStart);
                Vector3 AXOrigin = new Vector3(RayStart.X, RayStart.Y, RayStart.Z);
                Vector3 AXdirection = new Vector3(direction.X, direction.Y, direction.Z);

                if (target2.ParentGroup != null)
                {
                    pos = target2.AbsolutePosition;
                    //m_log.Info("[OBJECTREZ]: TargetPos: " + pos.ToString() + ", RayStart: " + RayStart.ToString() + ", RayEnd: " + RayEnd.ToString() + ", Volume: " + Util.GetDistanceTo(RayStart,RayEnd).ToString() + ", mag1: " + Util.GetMagnitude(RayStart).ToString() + ", mag2: " + Util.GetMagnitude(RayEnd).ToString());
                    //m_log.Info("[OBJECTREZ]: AXOrigin: " + AXOrigin.ToString() + "AXdirection: " + AXdirection.ToString());
                    // TODO: Raytrace better here
                    
                    //EntityIntersection ei = m_sceneGraph.GetClosestIntersectingPrim(new Ray(AXOrigin, AXdirection), false, false);
                    Ray NewRay = new Ray(AXOrigin, AXdirection);

                    // Ray Trace against target here
                    EntityIntersection ei = target2.TestIntersectionOBB(NewRay, Quaternion.Identity, frontFacesOnly, CopyCenters);

                    // Un-comment out the following line to Get Raytrace results printed to the console.
                    //m_log.Info("[RAYTRACERESULTS]: Hit:" + ei.HitTF.ToString() + " Point: " + ei.ipoint.ToString() + " Normal: " + ei.normal.ToString());
                    float ScaleOffset = 0.5f;

                    // If we hit something
                    if (ei.HitTF)
                    {
                        Vector3 scale = target.Scale;
                        Vector3 scaleComponent = new Vector3(ei.AAfaceNormal.X, ei.AAfaceNormal.Y, ei.AAfaceNormal.Z);
                        if (scaleComponent.X != 0) ScaleOffset = scale.X;
                        if (scaleComponent.Y != 0) ScaleOffset = scale.Y;
                        if (scaleComponent.Z != 0) ScaleOffset = scale.Z;
                        ScaleOffset = Math.Abs(ScaleOffset);
                        Vector3 intersectionpoint = new Vector3(ei.ipoint.X, ei.ipoint.Y, ei.ipoint.Z);
                        Vector3 normal = new Vector3(ei.normal.X, ei.normal.Y, ei.normal.Z);
                        Vector3 offset = normal * (ScaleOffset / 2f);
                        pos = intersectionpoint + offset;

                        // stick in offset format from the original prim
                        pos = pos - target.ParentGroup.AbsolutePosition;
                        if (CopyRotates)
                        {
                            Quaternion worldRot = target2.GetWorldRotation();

                            // SceneObjectGroup obj = m_sceneGraph.DuplicateObject(localID, pos, target.GetEffectiveObjectFlags(), AgentID, GroupID, worldRot);
                            m_sceneGraph.DuplicateObject(localID, pos, target.GetEffectiveObjectFlags(), AgentID, GroupID, worldRot);
                            //obj.Rotation = worldRot;
                            //obj.UpdateGroupRotationR(worldRot);
                        }
                        else
                        {
                            m_sceneGraph.DuplicateObject(localID, pos, target.GetEffectiveObjectFlags(), AgentID, GroupID);
                        }
                    }

                    return;
                }

                return;
            }
        }



        /// <summary>
        /// Sets the Home Point.   The LoginService uses this to know where to put a user when they log-in
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="regionHandle"></param>
        /// <param name="position"></param>
        /// <param name="lookAt"></param>
        /// <param name="flags"></param>
        public virtual void SetHomeRezPoint(IClientAPI remoteClient, ulong regionHandle, Vector3 position, Vector3 lookAt, uint flags)
        {
            ScenePresence SP = GetScenePresence(remoteClient.AgentId);
            if (Permissions.CanSetHome(SP.UUID))
            {
                position.Z += SP.Appearance.AvatarHeight / 2;
                if (GridUserService != null && GridUserService.SetHome(remoteClient.AgentId.ToString(), RegionInfo.RegionID, position, lookAt))
                    // FUBAR ALERT: this needs to be "Home position set." so the viewer saves a home-screenshot.
                    m_dialogModule.SendAlertToUser(remoteClient, "Home position set.");
                else
                    m_dialogModule.SendAlertToUser(remoteClient, "Set Home request failed.");
            }
            else
                m_dialogModule.SendAlertToUser(remoteClient, "Set Home request failed: Permissions do not allow the setting of home here.");
        }

        /// <summary>
        /// Create a child agent scene presence and add it to this scene.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        protected virtual ScenePresence CreateAndAddScenePresence(IClientAPI client)
        {
            AvatarAppearance appearance = null;
            GetAvatarAppearance(client, out appearance);

            ScenePresence avatar = m_sceneGraph.CreateAndAddChildScenePresence(client, appearance);
            //avatar.KnownRegions = GetChildrenSeeds(avatar.UUID);

            m_eventManager.TriggerOnNewPresence(avatar);

            return avatar;
        }

        /// <summary>
        /// Get the avatar apperance for the given client.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="appearance"></param>
        public void GetAvatarAppearance(IClientAPI client, out AvatarAppearance appearance)
        {
            AgentCircuitData aCircuit = m_authenticateHandler.GetAgentCircuitData(client.CircuitCode);

            if (aCircuit == null)
            {
                m_log.DebugFormat("[APPEARANCE] Client did not supply a circuit. Non-Linden? Creating default appearance.");
                appearance = new AvatarAppearance(client.AgentId);
                return;
            }

            appearance = aCircuit.Appearance;
            if (appearance == null)
            {
                m_log.DebugFormat("[APPEARANCE]: Appearance not found in {0}, returning default", RegionInfo.RegionName);
                appearance = new AvatarAppearance(client.AgentId);
            }
        }

        /// <summary>
        /// Remove the given client from the scene.
        /// </summary>
        /// <param name="agentID"></param>
        public override void RemoveClient(UUID agentID)
        {
            bool childagentYN = false;
            ScenePresence avatar = GetScenePresence(agentID);
            if (avatar != null)
            {
                childagentYN = avatar.IsChildAgent;

                if (avatar.ParentID != UUID.Zero)
                {
                    avatar.StandUp();
                }

                try
                {
                    if(!childagentYN)
                        m_log.DebugFormat(
                            "[SCENE]: Removing {0} agent {1} from region {2}",
                            (childagentYN ? "child" : "root"), agentID, RegionInfo.RegionName);

                    m_sceneGraph.removeUserCount(!childagentYN);
                    CapsModule.RemoveCapsHandler(agentID);

                    if (!avatar.IsChildAgent)
                    {
                        //List<ulong> childknownRegions = new List<ulong>();
                        //List<ulong> ckn = avatar.KnownChildRegionHandles;
                        //for (int i = 0; i < ckn.Count; i++)
                        //{
                        //    childknownRegions.Add(ckn[i]);
                        //}
                        List<ulong> regions = new List<ulong>(avatar.KnownChildRegionHandles);
                        regions.Remove(RegionInfo.RegionHandle);
                        m_sceneGridService.SendCloseChildAgentConnections(agentID, regions);

                    }
                    m_eventManager.TriggerClientClosed(agentID, this);
                    m_eventManager.TriggerOnClosingClient(avatar.ControllingClient);
                }
                catch (NullReferenceException)
                {
                    // We don't know which count to remove it from
                    // Avatar is already disposed :/
                }

                m_eventManager.TriggerOnRemovePresence(agentID);
                ForEachClient(
                    delegate(IClientAPI client)
                    {
                        //We can safely ignore null reference exceptions.  It means the avatar is dead and cleaned up anyway
                        try { client.SendKillObject(avatar.RegionHandle, new ISceneEntity[] {avatar}); }
                        catch (NullReferenceException) { }
                    });

                IAgentAssetTransactions agentTransactions = this.RequestModuleInterface<IAgentAssetTransactions>();
                if (agentTransactions != null)
                {
                    agentTransactions.RemoveAgentAssetTransactions(agentID);
                }

                // Remove the avatar from the scene
                m_sceneGraph.RemoveScenePresence(agentID);
                m_clientManager.Remove(agentID);

                try
                {
                    avatar.Close();
                }
                catch (NullReferenceException)
                {
                    //We can safely ignore null reference exceptions.  It means the avatar are dead and cleaned up anyway.
                }
                catch (Exception e)
                {
                    m_log.Error("[SCENE] Scene.cs:RemoveClient exception: " + e.ToString());
                }

                m_authenticateHandler.RemoveCircuit(avatar.ControllingClient.CircuitCode);
                //m_log.InfoFormat("[SCENE] Memory pre  GC {0}", System.GC.GetTotalMemory(false));
                //m_log.InfoFormat("[SCENE] Memory post GC {0}", System.GC.GetTotalMemory(true));
            }
        }

        /// <summary>
        /// Removes region from an avatar's known region list.  This coincides with child agents.  For each child agent, there will be a known region entry.
        /// 
        /// </summary>
        /// <param name="avatarID"></param>
        /// <param name="regionslst"></param>
        public void HandleRemoveKnownRegionsFromAvatar(UUID avatarID, List<ulong> regionslst)
        {
            ScenePresence av = GetScenePresence(avatarID);
            if (av != null)
            {
                lock (av)
                {
                    for (int i = 0; i < regionslst.Count; i++)
                    {
                        av.KnownChildRegionHandles.Remove(regionslst[i]);
                    }
                }
            }
        }

        #endregion

        #region Entities

        public void SendKillObject(uint localID)
        {
            ISceneEntity entity = null;
            SceneObjectPart part = GetSceneObjectPart(localID);
            if (part != null) // It is a prim
            {
                if (part.ParentGroup != null && !part.ParentGroup.IsDeleted) // Valid
                {
                    if (part.ParentGroup.RootPart != part) // Child part
                        return;
                }
                entity = part;
            }
            else
            {
                ScenePresence SP = GetScenePresence(localID);
                if (SP == null)
                    return;
                entity = SP;
            }
            ForEachClient(delegate(IClientAPI client) { client.SendKillObject(m_regInfo.RegionHandle, new ISceneEntity[] { entity }); });
        }

        #endregion

        #region RegionComms

        /// <summary>
        /// Do the work necessary to initiate a new user connection for a particular scene.
        /// At the moment, this consists of setting up the caps infrastructure
        /// The return bool should allow for connections to be refused, but as not all calling paths
        /// take proper notice of it let, we allowed banned users in still.
        /// </summary>
        /// <param name="agent">CircuitData of the agent who is connecting</param>
        /// <param name="reason">Outputs the reason for the false response on this string</param>
        /// <returns>True if the region accepts this agent.  False if it does not.  False will 
        /// also return a reason.</returns>
        public bool NewUserConnection(AgentCircuitData agent, uint teleportFlags, out string reason)
        {
            bool vialogin = ((teleportFlags & (uint)Constants.TeleportFlags.ViaLogin) != 0 ||
                             (teleportFlags & (uint)Constants.TeleportFlags.ViaHGLogin) != 0);
            reason = String.Empty;

            //Teleport flags:
            //
            // TeleportFlags.ViaGodlikeLure - God teleport (forced teleport)
            // TeleportFlags.ViaLogin - Login
            // TeleportFlags.TeleportFlags.ViaLure - Teleport request sent by another user
            // TeleportFlags.ViaLandmark | TeleportFlags.ViaLocation | TeleportFlags.ViaLandmark | TeleportFlags.Default - Regular Teleport

            // Don't disable this log message - it's too helpful
            if(!agent.child)
                m_log.InfoFormat(
                    "[CONNECTION BEGIN]: Region {0} told of incoming {1} agent {2} {3} {4} (circuit code {5}, teleportflags {6})",
                    RegionInfo.RegionName, (agent.child ? "child" : "root"), agent.firstname, agent.lastname,
                    agent.AgentID, agent.circuitcode, teleportFlags);

            if (LoginsDisabled)
            {
                reason = "Logins Disabled";
                return false;
            }

            ScenePresence sp = GetScenePresence(agent.AgentID);

            if (sp != null && !sp.IsChildAgent)
            {
                // We have a zombie from a crashed session. 
                // Or the same user is trying to be root twice here, won't work.
                // Kill it.
                m_log.DebugFormat("[SCENE]: Zombie scene presence detected for {0} in {1}", agent.AgentID, RegionInfo.RegionName);
                sp.ControllingClient.Close();
                sp = null;
            }

            //Can we teleport into this region?
            // Note: this takes care of practically every check possible, banned from estate, banned from parcels, parcel landing locations, etc
            if (!Permissions.CanTeleport(agent.AgentID, agent.startpos, agent.IPAddress, out agent.startpos, out reason))
                return false;

            if (!agent.child) 
                m_log.InfoFormat(
                    "[CONNECTION BEGIN]: Region {0} authenticated and authorized incoming {1} agent {2} {3} {4} (circuit code {5})",
                    RegionInfo.RegionName, (agent.child ? "child" : "root"), agent.firstname, agent.lastname,
                    agent.AgentID, agent.circuitcode);

            CapsModule.NewUserConnection(agent);

            if (sp == null) // We don't have an [child] agent here already
            {
                try
                {
                    if (!VerifyUserPresence(agent, out reason))
                        return false;
                }
                catch (Exception e)
                {
                    m_log.DebugFormat("[CONNECTION BEGIN]: Exception verifying presence {0}", e.Message);
                    return false;
                }

                try
                {
                    if (!AuthorizeUser(agent, out reason))
                        return false;
                }
                catch (Exception e)
                {
                    m_log.DebugFormat("[CONNECTION BEGIN]: Exception authorizing user {0}", e.Message);
                    return false;
                }

                CapsModule.NewUserConnection(agent);
                CapsModule.AddCapsHandler(agent.AgentID);
            }
            else
            {
                if (sp.IsChildAgent)
                {
                    //m_log.DebugFormat(
                    //    "[SCENE]: Adjusting known seeds for existing agent {0} in {1}",
                    //    agent.AgentID, RegionInfo.RegionName);

                    sp.AdjustKnownSeeds();
                    CapsModule.NewUserConnection(agent);
                }
            }


            // In all cases, add or update the circuit data with the new agent circuit data and teleport flags
            agent.teleportFlags = teleportFlags;
            m_authenticateHandler.AddNewCircuit(agent.circuitcode, agent);

            if (vialogin) 
            {
                if (TestBorderCross(agent.startpos, Cardinals.E))
                {
                    Border crossedBorder = GetCrossedBorder(agent.startpos, Cardinals.E);
                    agent.startpos.X = crossedBorder.BorderLine.Z - 1;
                }

                if (TestBorderCross(agent.startpos, Cardinals.N))
                {
                    Border crossedBorder = GetCrossedBorder(agent.startpos, Cardinals.N);
                    agent.startpos.Y = crossedBorder.BorderLine.Z - 1;
                }

                //Mitigate http://opensimulator.org/mantis/view.php?id=3522
                // Check if start position is outside of region
                // If it is, check the Z start position also..   if not, leave it alone.
                if (BordersLocked)
                {
                    lock (EastBorders)
                    {
                        if (agent.startpos.X > EastBorders[0].BorderLine.Z)
                        {
                            m_log.Warn("FIX AGENT POSITION");
                            agent.startpos.X = EastBorders[0].BorderLine.Z * 0.5f;
                            if (agent.startpos.Z > 720)
                                agent.startpos.Z = 720;
                        }
                    }
                    lock (NorthBorders)
                    {
                        if (agent.startpos.Y > NorthBorders[0].BorderLine.Z)
                        {
                            m_log.Warn("FIX Agent POSITION");
                            agent.startpos.Y = NorthBorders[0].BorderLine.Z * 0.5f;
                            if (agent.startpos.Z > 720)
                                agent.startpos.Z = 720;
                        }
                    }
                }
                else
                {
                    if (agent.startpos.X > EastBorders[0].BorderLine.Z)
                    {
                        m_log.Warn("FIX AGENT POSITION");
                        agent.startpos.X = EastBorders[0].BorderLine.Z * 0.5f;
                        if (agent.startpos.Z > 720)
                            agent.startpos.Z = 720;
                    }
                    if (agent.startpos.Y > NorthBorders[0].BorderLine.Z)
                    {
                        m_log.Warn("FIX Agent POSITION");
                        agent.startpos.Y = NorthBorders[0].BorderLine.Z * 0.5f;
                        if (agent.startpos.Z > 720)
                            agent.startpos.Z = 720;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Verifies that the user has a presence on the Grid
        /// </summary>
        /// <param name="agent">Circuit Data of the Agent we're verifying</param>
        /// <param name="reason">Outputs the reason for the false response on this string</param>
        /// <returns>True if the user has a session on the grid.  False if it does not.  False will 
        /// also return a reason.</returns>
        public virtual bool VerifyUserPresence(AgentCircuitData agent, out string reason)
        {
            reason = String.Empty;

            IPresenceService presence = RequestModuleInterface<IPresenceService>();
            if (presence == null)
            {
                reason = String.Format("Failed to verify user presence in the grid for {0} {1} in region {2}. Presence service does not exist.", agent.firstname, agent.lastname, RegionInfo.RegionName);
                return false;
            }

            OpenSim.Services.Interfaces.PresenceInfo pinfo = presence.GetAgent(agent.SessionID);

            if (pinfo == null)
            {
                reason = String.Format("Failed to verify user presence in the grid for {0} {1}, access denied to region {2}.", agent.firstname, agent.lastname, RegionInfo.RegionName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verify if the user can connect to this region.  Checks the banlist and ensures that the region is set for public access
        /// </summary>
        /// <param name="agent">The circuit data for the agent</param>
        /// <param name="reason">outputs the reason to this string</param>
        /// <returns>True if the region accepts this agent.  False if it does not.  False will 
        /// also return a reason.</returns>
        protected virtual bool AuthorizeUser(AgentCircuitData agent, out string reason)
        {
            reason = String.Empty;

            if (!m_strictAccessControl) return true; //No checking if we don't do access control
            if (Permissions.IsGod(agent.AgentID)) return true;
                      
            if (AuthorizationService != null)
            {
                if (!AuthorizationService.IsAuthorizedForRegion(agent.AgentID.ToString(), RegionInfo.RegionID.ToString(),out reason))
                {
                    m_log.WarnFormat("[CONNECTION BEGIN]: Denied access to: {0} ({1} {2}) at {3} because the user does not have access to the region",
                                     agent.AgentID, agent.firstname, agent.lastname, RegionInfo.RegionName);
                    //reason = String.Format("You are not currently on the access list for {0}",RegionInfo.RegionName);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// We've got an update about an agent that sees into this region, 
        /// send it to ScenePresence for processing  It's the full data.
        /// </summary>
        /// <param name="cAgentData">Agent that contains all of the relevant things about an agent.
        /// Appearance, animations, position, etc.</param>
        /// <returns>true if we handled it.</returns>
        public virtual bool IncomingChildAgentDataUpdate(AgentData cAgentData)
        {
            //m_log.DebugFormat(
            //    "[SCENE]: Incoming child agent update for {0} in {1}", cAgentData.AgentID, RegionInfo.RegionName);

            // XPTO: if this agent is not allowed here as root, always return false

            // We have to wait until the viewer contacts this region after receiving EAC.
            // That calls AddNewClient, which finally creates the ScenePresence
            ScenePresence childAgentUpdate = WaitGetScenePresence(cAgentData.AgentID);
            if (childAgentUpdate != null)
            {
                childAgentUpdate.ChildAgentDataUpdate(cAgentData);
                return true;
            }

            return false;
        }

        /// <summary>
        /// We've got an update about an agent that sees into this region, 
        /// send it to ScenePresence for processing  It's only positional data
        /// </summary>
        /// <param name="cAgentData">AgentPosition that contains agent positional data so we can know what to send</param>
        /// <returns>true if we handled it.</returns>
        public virtual bool IncomingChildAgentDataUpdate(AgentPosition cAgentData)
        {
            //m_log.Debug(" XXX Scene IncomingChildAgentDataUpdate POSITION in " + RegionInfo.RegionName);
            ScenePresence childAgentUpdate = GetScenePresence(cAgentData.AgentID);
            if (childAgentUpdate != null)
            {
                // I can't imagine *yet* why we would get an update if the agent is a root agent..
                // however to avoid a race condition crossing borders..
                if (childAgentUpdate.IsChildAgent)
                {
                    uint rRegionX = (uint)(cAgentData.RegionHandle >> 40);
                    uint rRegionY = (((uint)(cAgentData.RegionHandle)) >> 8);
                    uint tRegionX = RegionInfo.RegionLocX;
                    uint tRegionY = RegionInfo.RegionLocY;
                    //Send Data to ScenePresence
                    childAgentUpdate.ChildAgentDataUpdate(cAgentData, tRegionX, tRegionY, rRegionX, rRegionY);
                }

                return true;
            }

            return false;
        }

        protected virtual ScenePresence WaitGetScenePresence(UUID agentID)
        {
            int ntimes = 10;
            ScenePresence childAgentUpdate = null;
            while ((childAgentUpdate = GetScenePresence(agentID)) == null && (ntimes-- > 0))
                Thread.Sleep(1000);
            return childAgentUpdate;

        }

        public virtual bool IncomingRetrieveRootAgent(UUID id, out IAgentData agent)
        {
            agent = null;
            ScenePresence sp = GetScenePresence(id);
            if ((sp != null) && (!sp.IsChildAgent))
            {
                sp.IsChildAgent = true;
                return sp.CopyAgent(out agent);
            }

            return false;
        }

        /// <summary>
        /// Tell a single agent to disconnect from the region.
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="agentID"></param>
        public bool IncomingCloseAgent(UUID agentID)
        {
            //m_log.DebugFormat("[SCENE]: Processing incoming close agent for {0}", agentID);

            ScenePresence presence = m_sceneGraph.GetScenePresence(agentID);
            if (presence != null)
            {
                // Nothing is removed here, so down count it as such
                if (presence.IsChildAgent)
                {
                   m_sceneGraph.removeUserCount(false);
                }
                else
                {
                   m_sceneGraph.removeUserCount(true);
                }

                // Don't do this to root agents on logout, it's not nice for the viewer
                if (presence.IsChildAgent)
                {
                    // Tell a single agent to disconnect from the region.
                    IEventQueue eq = RequestModuleInterface<IEventQueue>();
                    if (eq != null)
                    {
                        eq.DisableSimulator(RegionInfo.RegionHandle, agentID);
                    }
                    else
                        presence.ControllingClient.SendShutdownConnectionNotice();
                }

                presence.ControllingClient.Close();
                return true;
            }

            // Agent not here
            return false;
        }

        /// <summary>
        /// Tries to teleport agent to other region.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="regionName"></param>
        /// <param name="position"></param>
        /// <param name="lookAt"></param>
        /// <param name="teleportFlags"></param>
        public void RequestTeleportLocation(IClientAPI remoteClient, string regionName, Vector3 position,
                                            Vector3 lookat, uint teleportFlags)
        {
            GridRegion regionInfo = GridService.GetRegionByName(UUID.Zero, regionName);
            if (regionInfo == null)
            {
                // can't find the region: Tell viewer and abort
                remoteClient.SendTeleportFailed("The region '" + regionName + "' could not be found.");
                return;
            }

            RequestTeleportLocation(remoteClient, regionInfo.RegionHandle, position, lookat, teleportFlags);
        }

        /// <summary>
        /// Tries to teleport agent to other region.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="regionHandle"></param>
        /// <param name="position"></param>
        /// <param name="lookAt"></param>
        /// <param name="teleportFlags"></param>
        public void RequestTeleportLocation(IClientAPI remoteClient, ulong regionHandle, Vector3 position,
                                            Vector3 lookAt, uint teleportFlags)
        {
            ScenePresence sp = GetScenePresence(remoteClient.AgentId);
            if (sp != null)
            {
                uint regionX = m_regInfo.RegionLocX;
                uint regionY = m_regInfo.RegionLocY;

                Utils.LongToUInts(regionHandle, out regionX, out regionY);

                int shiftx = (int) regionX - (int) m_regInfo.RegionLocX * (int)Constants.RegionSize;
                int shifty = (int) regionY - (int) m_regInfo.RegionLocY * (int)Constants.RegionSize;

                position.X += shiftx;
                position.Y += shifty;

                bool result = false;

                if (TestBorderCross(position,Cardinals.N))
                    result = true;

                if (TestBorderCross(position, Cardinals.S))
                    result = true;

                if (TestBorderCross(position, Cardinals.E))
                    result = true;

                if (TestBorderCross(position, Cardinals.W))
                    result = true;

                // bordercross if position is outside of region

                if (!result)
                    regionHandle = m_regInfo.RegionHandle;
                else
                {
                    // not in this region, undo the shift!
                    position.X -= shiftx;
                    position.Y -= shifty;
                }

                if (m_teleportModule != null)
                {
                    object[] request = new object[5];
                    request[0] = sp;
                    request[1] = regionHandle;
                    request[2] = position;
                    request[3] = lookAt;
                    request[4] = teleportFlags;
                    Util.FireAndForget(FireTeleportAsync, request);
                }
                else
                {
                    m_log.DebugFormat("[SCENE]: Unable to perform teleports: no AgentTransferModule is active");
                    sp.ControllingClient.SendTeleportFailed("Unable to perform teleports on this simulator.");
                }
            }
        }

        private void FireTeleportAsync(object val)
        {
            object[] request = (object[])val;
            ScenePresence sp = (ScenePresence)request[0];
            ulong regionHandle = (ulong)request[1];
            Vector3 position = (Vector3)request[2];
            Vector3 lookAt = (Vector3)request[3];
            uint teleportFlags = (uint)request[4];
            m_teleportModule.Teleport(sp, regionHandle, position, lookAt, teleportFlags);
        }

        /// <summary>
        /// Tries to teleport agent to landmark.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="regionHandle"></param>
        /// <param name="position"></param>
        public void RequestTeleportLandmark(IClientAPI remoteClient, UUID regionID, Vector3 position)
        {
            GridRegion info = GridService.GetRegionByUUID(UUID.Zero, regionID);

            if (info == null)
            {
                // can't find the region: Tell viewer and abort
                remoteClient.SendTeleportFailed("The teleport destination could not be found.");
                return;
            }

            RequestTeleportLocation(remoteClient, info.RegionHandle, position, Vector3.Zero, (uint)(TPFlags.SetLastToTarget | TPFlags.ViaLandmark));
        }

        public void client_OnTeleportCancel(IClientAPI client)
        {
            m_teleportModule.CancelTeleport(client.AgentId);
        }

        public void CrossAgentToNewRegion(ScenePresence agent, bool isFlying)
        {
            if (m_teleportModule != null)
                m_teleportModule.Cross(agent, isFlying);
            else
            {
                m_log.DebugFormat("[SCENE]: Unable to cross agent to neighbouring region, because there is no AgentTransferModule");
            }
        }

        public void SendOutChildAgentUpdates(AgentPosition cadu, ScenePresence presence)
        {
            m_sceneGridService.SendChildAgentDataUpdate(cadu, presence);
        }

        public void RegionHandleRequest(IClientAPI client, UUID regionID)
        {
            ulong handle = 0;
            if (regionID == RegionInfo.RegionID)
                handle = RegionInfo.RegionHandle;
            else
            {
                GridRegion r = GridService.GetRegionByUUID(UUID.Zero, regionID);
                if (r != null)
                    handle = r.RegionHandle;
            }

            if (handle != 0)
                client.SendRegionHandle(regionID, handle);
        }

        #endregion

        #region Other Methods

        public void SetObjectCapacity(int objects)
        {
            // Region specific config overrides global
            //
            if (RegionInfo.ObjectCapacity != 0)
                objects = RegionInfo.ObjectCapacity;

            if (StatsReporter != null)
                StatsReporter.SetObjectCapacity(objects);

            m_ObjectCapacity = objects;
        }

        #endregion

        #region Script Handling Methods

        /// <summary>
        /// Console command handler to send script command to script engine.
        /// </summary>
        /// <param name="args"></param>
        public void SendCommandToPlugins(string[] args)
        {
            m_eventManager.TriggerOnPluginConsole(args);
        }

        public LandData GetLandData(float x, float y)
        {
            return LandChannel.GetLandObject(x, y).LandData;
        }


        #endregion

        #region Script Engine

        private bool ScriptDanger(SceneObjectPart part, Vector3 pos)
        {
            if (part.IsAttachment && RunScriptsInAttachments)
                return true; //Always run as in SL
            ILandObject parcel = LandChannel.GetLandObject(pos.X, pos.Y);
            if (parcel != null)
            {
                if ((parcel.LandData.Flags & (uint)ParcelFlags.AllowOtherScripts) != 0)
                    return true;
                else if ((parcel.LandData.Flags & (uint)ParcelFlags.AllowGroupScripts) != 0)
                {
                    if (part.OwnerID == parcel.LandData.OwnerID
                        || (parcel.LandData.IsGroupOwned && part.GroupID == parcel.LandData.GroupID)
                        || Permissions.IsGod(part.OwnerID))
                        return true;
                    else
                        return false;
                }
                else
                {
                    //Gods should be able to run scripts. 
                    // -- Revolution
                    if (part.OwnerID == parcel.LandData.OwnerID || Permissions.IsGod(part.OwnerID))
                        return true;
                    else
                        return false;
                }
            }
            else
            {
                if (pos.X > 0f && pos.X < Constants.RegionSize && pos.Y > 0f && pos.Y < Constants.RegionSize)
                    // The only time parcel != null when an object is inside a region is when
                    // there is nothing behind the landchannel.  IE, no land plugin loaded.
                    return true;
                else
                    // The object is outside of this region.  Stop piping events to it.
                    return false;
            }
        }

        public bool ScriptDanger(uint localID, Vector3 pos)
        {
            SceneObjectPart part = GetSceneObjectPart(localID);
            if (part != null)
            {
                return ScriptDanger(part, pos);
            }
            else
            {
                return false;
            }
        }

        public bool PipeEventsForScript(SceneObjectPart part)
        {
            // Changed so that child prims of attachments return ScriptDanger for their parent, so that
            //  their scripts will actually run.
            //      -- Leaf, Tue Aug 12 14:17:05 EDT 2008
            SceneObjectPart parent = part.ParentGroup.RootPart;
            if (parent != null && parent.IsAttachment)
                return ScriptDanger(parent, parent.AbsolutePosition);
            else
                return ScriptDanger(part, part.AbsolutePosition);
        }

        #endregion

        #region SceneGraph wrapper methods

        public UUID ConvertLocalIDToFullID(uint localID)
        {
            return m_sceneGraph.ConvertLocalIDToFullID(localID);
        }

        public void SwapRootAgentCount(bool rootChildChildRootTF)
        {
            m_sceneGraph.SwapRootChildAgent(rootChildChildRootTF);
        }

        public void AddPhysicalPrim(int num)
        {
            m_sceneGraph.AddPhysicalPrim(num);
        }

        public void RemovePhysicalPrim(int num)
        {
            m_sceneGraph.RemovePhysicalPrim(num);
        }

        public int GetRootAgentCount()
        {
            return m_sceneGraph.GetRootAgentCount();
        }

        public int GetChildAgentCount()
        {
            return m_sceneGraph.GetChildAgentCount();
        }

        public int GetTotalObjectsCount()
        {
            return m_sceneGraph.GetTotalObjectsCount();
        }

        public Dictionary<uint, float> GetTopScripts()
        {
            return m_sceneGraph.GetTopScripts();
        }

        public void AddToScriptEPS(int count)
        {
            m_sceneGraph.AddToScriptEPS(count);
        }

        public void AddActiveScripts(int count)
        {
            m_sceneGraph.AddActiveScripts(count);
        }

        public void AddToUpdateList(SceneObjectGroup g)
        {
            m_sceneGraph.AddToUpdateList(g);
        }

        /// <summary>
        /// Request a scene presence by UUID. Fast, indexed lookup.
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns>null if the presence was not found</returns>
        public ScenePresence GetScenePresence(UUID agentID)
        {
            return m_sceneGraph.GetScenePresence(agentID);
        }

        /// <summary>
        /// Request the scene presence by name.
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <returns>null if the presence was not found</returns>
        public ScenePresence GetScenePresence(string firstName, string lastName)
        {
            return m_sceneGraph.GetScenePresence(firstName, lastName);
        }

        /// <summary>
        /// Request the scene presence by localID.
        /// </summary>
        /// <param name="localID"></param>
        /// <returns>null if the presence was not found</returns>
        public ScenePresence GetScenePresence(uint localID)
        {
            return m_sceneGraph.GetScenePresence(localID);
        }

        public override bool PresenceChildStatus(UUID avatarID)
        {
            ScenePresence cp = GetScenePresence(avatarID);

            return cp.IsChildAgent;
        }

        /// <summary>
        /// Performs action on all scene presences.
        /// </summary>
        /// <param name="action"></param>
        public void ForEachScenePresence(Action<ScenePresence> action)
        {
            if (m_sceneGraph != null)
            {
                m_sceneGraph.ForEachScenePresence(action);
            }
        }

        public List<ScenePresence> ScenePresences
        {
            get { return m_sceneGraph.ScenePresences; }
        }

        /// <summary>
        /// Get a named prim contained in this scene (will return the first
        /// found, if there are more than one prim with the same name)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SceneObjectPart GetSceneObjectPart(string name)
        {
            return m_sceneGraph.GetSceneObjectPart(name);
        }

        /// <summary>
        /// Get a prim via its local id
        /// </summary>
        /// <param name="localID"></param>
        /// <returns></returns>
        public SceneObjectPart GetSceneObjectPart(uint localID)
        {
            return m_sceneGraph.GetSceneObjectPart(localID);
        }

        /// <summary>
        /// Get a prim via its UUID
        /// </summary>
        /// <param name="fullID"></param>
        /// <returns></returns>
        public SceneObjectPart GetSceneObjectPart(UUID fullID)
        {
            return m_sceneGraph.GetSceneObjectPart(fullID);
        }

        /// <summary>
        /// Get a scene object group that contains the prim with the given local id
        /// </summary>
        /// <param name="localID"></param>
        /// <returns>null if no scene object group containing that prim is found</returns>
        public SceneObjectGroup GetGroupByPrim(uint localID)
        {
            return m_sceneGraph.GetGroupByPrim(localID);
        }

        public override bool TryGetScenePresence(UUID avatarId, out ScenePresence avatar)
        {
            return m_sceneGraph.TryGetScenePresence(avatarId, out avatar);
        }

        public bool TryGetAvatarByName(string avatarName, out ScenePresence avatar)
        {
            return m_sceneGraph.TryGetAvatarByName(avatarName, out avatar);
        }

        public void ForEachClient(Action<IClientAPI> action)
        {
            m_clientManager.ForEachSync(action);
        }

        public bool TryGetClient(UUID avatarID, out IClientAPI client)
        {
            return m_clientManager.TryGetValue(avatarID, out client);
        }

        public bool TryGetClient(System.Net.IPEndPoint remoteEndPoint, out IClientAPI client)
        {
            return m_clientManager.TryGetValue(remoteEndPoint, out client);
        }

        public void ForEachSOG(Action<SceneObjectGroup> action)
        {
            m_sceneGraph.ForEachSOG(action);
        }

        public void removeUserCount(bool typeRCTF)
        {
            m_sceneGraph.removeUserCount(typeRCTF);
        }

        public void GetCoarseLocations(out List<Vector3> coarseLocations, out List<UUID> avatarUUIDs, uint maxLocations)
        {
            m_sceneGraph.GetCoarseLocations(out coarseLocations, out avatarUUIDs, maxLocations);
        }

        public IClientAPI GetControllingClient(UUID agentId)
        {
            return m_sceneGraph.GetControllingClient(agentId);
        }

        /// <summary>
        /// Returns a list of the entities in the scene.  This is a new list so operations perform on the list itself
        /// will not affect the original list of objects in the scene.
        /// </summary>
        /// <returns></returns>
        public EntityBase[] GetEntities()
        {
            return m_sceneGraph.GetEntities();
        }

        #endregion

        #region Joints

        // This callback allows the PhysicsScene to call back to its caller (the SceneGraph) and
        // update non-physical objects like the joint proxy objects that represent the position
        // of the joints in the scene.

        // This routine is normally called from within a lock (OdeLock) from within the OdePhysicsScene
        // WARNING: be careful of deadlocks here if you manipulate the scene. Remember you are being called
        // from within the OdePhysicsScene.

        protected internal void jointMoved(PhysicsJoint joint)
        {
            // m_parentScene.PhysicsScene.DumpJointInfo(); // non-thread-locked version; we should already be in a lock (OdeLock) when this callback is invoked
            SceneObjectPart jointProxyObject = GetSceneObjectPart(joint.ObjectNameInScene);
            if (jointProxyObject == null)
            {
                jointErrorMessage(joint, "WARNING, joint proxy not found, name " + joint.ObjectNameInScene);
                return;
            }

            // now update the joint proxy object in the scene to have the position of the joint as returned by the physics engine
            SceneObjectPart trackedBody = GetSceneObjectPart(joint.TrackedBodyName); // FIXME: causes a sequential lookup
            if (trackedBody == null) return; // the actor may have been deleted but the joint still lingers around a few frames waiting for deletion. during this time, trackedBody is NULL to prevent further motion of the joint proxy.
            jointProxyObject.Velocity = trackedBody.Velocity;
            jointProxyObject.AngularVelocity = trackedBody.AngularVelocity;
            switch (joint.Type)
            {
                case PhysicsJointType.Ball:
                    {
                        Vector3 jointAnchor = PhysicsScene.GetJointAnchor(joint);
                        Vector3 proxyPos = new Vector3(jointAnchor.X, jointAnchor.Y, jointAnchor.Z);
                        jointProxyObject.ParentGroup.UpdateGroupPosition(proxyPos, true); // schedules the entire group for a terse update
                    }
                    break;

                case PhysicsJointType.Hinge:
                    {
                        Vector3 jointAnchor = PhysicsScene.GetJointAnchor(joint);

                        // Normally, we would just ask the physics scene to return the axis for the joint.
                        // Unfortunately, ODE sometimes returns <0,0,0> for the joint axis, which should
                        // never occur. Therefore we cannot rely on ODE to always return a correct joint axis.
                        // Therefore the following call does not always work:
                        //PhysicsVector phyJointAxis = _PhyScene.GetJointAxis(joint);

                        // instead we compute the joint orientation by saving the original joint orientation
                        // relative to one of the jointed bodies, and applying this transformation
                        // to the current position of the jointed bodies (the tracked body) to compute the
                        // current joint orientation.

                        if (joint.TrackedBodyName == null)
                        {
                            jointErrorMessage(joint, "joint.TrackedBodyName is null, joint " + joint.ObjectNameInScene);
                        }

                        Vector3 proxyPos = new Vector3(jointAnchor.X, jointAnchor.Y, jointAnchor.Z);
                        Quaternion q = trackedBody.RotationOffset * joint.LocalRotation;

                        jointProxyObject.ParentGroup.UpdateGroupPosition(proxyPos, true); // schedules the entire group for a terse update
                        jointProxyObject.ParentGroup.UpdateGroupRotationR(q); // schedules the entire group for a terse update
                    }
                    break;
            }
        }

        // This callback allows the PhysicsScene to call back to its caller (the SceneGraph) and
        // update non-physical objects like the joint proxy objects that represent the position
        // of the joints in the scene.

        // This routine is normally called from within a lock (OdeLock) from within the OdePhysicsScene
        // WARNING: be careful of deadlocks here if you manipulate the scene. Remember you are being called
        // from within the OdePhysicsScene.
        protected internal void jointDeactivated(PhysicsJoint joint)
        {
            //m_log.Debug("[NINJA] SceneGraph.jointDeactivated, joint:" + joint.ObjectNameInScene);
            SceneObjectPart jointProxyObject = GetSceneObjectPart(joint.ObjectNameInScene);
            if (jointProxyObject == null)
            {
                jointErrorMessage(joint, "WARNING, trying to deactivate (stop interpolation of) joint proxy, but not found, name " + joint.ObjectNameInScene);
                return;
            }

            // turn the proxy non-physical, which also stops its client-side interpolation
            bool wasUsingPhysics = ((jointProxyObject.Flags & PrimFlags.Physics) != 0);
            if (wasUsingPhysics)
            {
                jointProxyObject.UpdatePrimFlags(false, false, true, false); // FIXME: possible deadlock here; check to make sure all the scene alterations set into motion here won't deadlock
            }
        }

        // This callback allows the PhysicsScene to call back to its caller (the SceneGraph) and
        // alert the user of errors by using the debug channel in the same way that scripts alert
        // the user of compile errors.

        // This routine is normally called from within a lock (OdeLock) from within the OdePhysicsScene
        // WARNING: be careful of deadlocks here if you manipulate the scene. Remember you are being called
        // from within the OdePhysicsScene.
        public void jointErrorMessage(PhysicsJoint joint, string message)
        {
            if (joint != null)
            {
                if (joint.ErrorMessageCount > PhysicsJoint.maxErrorMessages)
                    return;

                SceneObjectPart jointProxyObject = GetSceneObjectPart(joint.ObjectNameInScene);
                if (jointProxyObject != null)
                {
                    SimChat("[NINJA]: " + message,
                        ChatTypeEnum.DebugChannel,
                        2147483647,
                        jointProxyObject.AbsolutePosition,
                        jointProxyObject.Name,
                        jointProxyObject.UUID,
                        false);

                    joint.ErrorMessageCount++;

                    if (joint.ErrorMessageCount > PhysicsJoint.maxErrorMessages)
                    {
                        SimChat("[NINJA]: Too many messages for this joint, suppressing further messages.",
                            ChatTypeEnum.DebugChannel,
                            2147483647,
                            jointProxyObject.AbsolutePosition,
                            jointProxyObject.Name,
                            jointProxyObject.UUID,
                            false);
                    }
                }
                else
                {
                    // couldn't find the joint proxy object; the error message is silently suppressed
                }
            }
        }

        #endregion

        #region Console

        public Scene ConsoleScene()
        {
            if (MainConsole.Instance == null)
                return null;
            if (MainConsole.Instance.ConsoleScene is Scene)
                return (Scene)MainConsole.Instance.ConsoleScene;
            return null;
        }

        /// <summary>
        /// Causes all clients to get a full object update on all of the objects in the scene.
        /// </summary>
        public void ForceClientUpdate()
        {
            EntityBase[] EntityList = GetEntities();

            foreach (EntityBase ent in EntityList)
            {
                if (ent is SceneObjectGroup)
                {
                    ((SceneObjectGroup)ent).ScheduleGroupForFullUpdate(PrimUpdateFlags.FullUpdate);
                }
            }
        }

        public override void Show(string[] showParams)
        {
            base.Show(showParams);

            switch (showParams[0])
            {
                case "users":
                    m_log.Error("Current Region: " + RegionInfo.RegionName);
                    m_log.ErrorFormat("{0,-16}{1,-16}{2,-25}{3,-25}{4,-16}{5,-16}{6,-16}", "Firstname", "Lastname",
                                      "Agent ID", "Session ID", "Circuit", "IP", "World");

                    ForEachScenePresence(delegate(ScenePresence sp)
                    {
                        m_log.ErrorFormat("{0,-16}{1,-16}{2,-25}{3,-25}{4,-16},{5,-16}{6,-16}",
                                          sp.Firstname,
                                          sp.Lastname,
                                          sp.UUID,
                                          sp.ControllingClient.AgentId,
                                          "Unknown",
                                          "Unknown",
                                          RegionInfo.RegionName);
                    });

                    break;
            }
        }

        #endregion

        #region Ground/Sun

        public float GetGroundHeight(float x, float y)
        {
            if (x < 0)
                x = 0;
            if (x >= Heightmap.Width)
                x = Heightmap.Width - 1;
            if (y < 0)
                y = 0;
            if (y >= Heightmap.Height)
                y = Heightmap.Height - 1;

            Vector3 p0 = new Vector3(x, y, (float)Heightmap[(int)x, (int)y]);
            Vector3 p1 = new Vector3(p0);
            Vector3 p2 = new Vector3(p0);

            p1.X += 1.0f;
            if (p1.X < Heightmap.Width)
                p1.Z = (float)Heightmap[(int)p1.X, (int)p1.Y];

            p2.Y += 1.0f;
            if (p2.Y < Heightmap.Height)
                p2.Z = (float)Heightmap[(int)p2.X, (int)p2.Y];

            Vector3 v0 = new Vector3(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3 v1 = new Vector3(p2.X - p0.X, p2.Y - p0.Y, p2.Z - p0.Z);

            v0.Normalize();
            v1.Normalize();

            Vector3 vsn = new Vector3();
            vsn.X = (v0.Y * v1.Z) - (v0.Z * v1.Y);
            vsn.Y = (v0.Z * v1.X) - (v0.X * v1.Z);
            vsn.Z = (v0.X * v1.Y) - (v0.Y * v1.X);
            vsn.Normalize();

            float xdiff = x - (float)((int)x);
            float ydiff = y - (float)((int)y);

            return (((vsn.X * xdiff) + (vsn.Y * ydiff)) / (-1 * vsn.Z)) + p0.Z;
        }

        public void TerrainUnAcked(IClientAPI client, int patchX, int patchY)
        {
            //m_log.Debug("Terrain packet unacked, resending patch: " + patchX + " , " + patchY);
            client.SendLayerData(patchX, patchY, Heightmap.GetFloatsSerialised());
        }

        public void TriggerEstateSunUpdate()
        {
            float sun;
            if (RegionInfo.RegionSettings.UseEstateSun)
            {
                sun = (float)RegionInfo.EstateSettings.SunPosition;
                if (RegionInfo.EstateSettings.UseGlobalTime)
                {
                    sun = EventManager.GetCurrentTimeAsSunLindenHour() - 6.0f;
                }

                // 
                EventManager.TriggerEstateToolsSunUpdate(
                        RegionInfo.RegionHandle,
                        RegionInfo.EstateSettings.FixedSun,
                        RegionInfo.RegionSettings.UseEstateSun,
                        sun);
            }
            else
            {
                // Use the Sun Position from the Region Settings
                sun = (float)RegionInfo.RegionSettings.SunPosition - 6.0f;

                EventManager.TriggerEstateToolsSunUpdate(
                        RegionInfo.RegionHandle,
                        RegionInfo.RegionSettings.FixedSun,
                        RegionInfo.RegionSettings.UseEstateSun,
                        sun);
            }
        }

        #endregion

        #region Backup

        private HashSet<UUID> m_backupTaintedPrims = new HashSet<UUID>();

        public void AddPrimBackupTaint(SceneObjectGroup sceneObjectGroup)
        {
            lock (m_backupTaintedPrims)
            {
                if (!m_backupTaintedPrims.Contains(sceneObjectGroup.UUID))
                    m_backupTaintedPrims.Add(sceneObjectGroup.UUID);
            }
        }

        /// <summary>
        /// This is the new backup processor, it only deals with prims that 
        /// have been 'tainted' so that it does not waste time
        /// running through as large of a backup loop
        /// </summary>
        public void ProcessPrimBackupTaints(bool forced)
        {
            HashSet<UUID> backupPrims;
            lock (m_backupTaintedPrims)
            {
                if (m_backupTaintedPrims.Count == 0)
                    return;
                backupPrims = new HashSet<UUID>(m_backupTaintedPrims);
                m_backupTaintedPrims.Clear();
            }
            backupPrims.Clear();
            foreach (UUID grpUUID in backupPrims)
            {
                EntityBase entity = Entities[grpUUID];
                if(!(entity is SceneObjectGroup))
                    continue;
                SceneObjectGroup grp = entity as SceneObjectGroup;
                if (!grp.ProcessBackup(SimulationDataService, forced))
                {
                    //Readd it then as its not time for it to backup yet
                    lock (m_backupTaintedPrims)
                        if(!m_backupTaintedPrims.Contains(grp.UUID))
                            m_backupTaintedPrims.Add(grp.UUID);
                }
            }
        }

        #endregion

        #region Parcel Returns

        /// <summary>
        /// This deals with sending the return IMs as well as actually returning the objects
        /// </summary>
        protected internal void CheckParcelReturns()
        {
            // Go through all updates
            m_sceneGraph.CheckParcelReturns();
            lock (m_returns)
            {
                foreach (KeyValuePair<UUID, ReturnInfo> ret in m_returns)
                {
                    UUID transaction = UUID.Random();

                    GridInstantMessage msg = new GridInstantMessage();
                    msg.fromAgentID = new Guid(UUID.Zero.ToString()); // From server
                    msg.toAgentID = new Guid(ret.Key.ToString());
                    msg.imSessionID = new Guid(transaction.ToString());
                    msg.timestamp = (uint)Util.UnixTimeSinceEpoch();
                    msg.fromAgentName = "Server";
                    msg.dialog = (byte)19; // Object msg
                    msg.fromGroup = false;
                    msg.offline = (byte)1;
                    msg.ParentEstateID = RegionInfo.EstateSettings.ParentEstateID;
                    msg.Position = Vector3.Zero;
                    msg.RegionID = RegionInfo.RegionID.Guid;
                    msg.binaryBucket = new byte[0];
                    if (ret.Value.count > 1)
                        msg.message = string.Format("Your {0} objects were returned from {1} in region {2} due to {3}", ret.Value.count, ret.Value.location.ToString(), RegionInfo.RegionName, ret.Value.reason);
                    else
                        msg.message = string.Format("Your object {0} was returned from {1} in region {2} due to {3}", ret.Value.objectName, ret.Value.location.ToString(), RegionInfo.RegionName, ret.Value.reason);

                    IMessageTransferModule tr = RequestModuleInterface<IMessageTransferModule>();
                    if (tr != null)
                        tr.SendInstantMessage(msg);

                    if(ret.Value.Groups.Count > 1)
                        m_log.InfoFormat("[SCENE]: Returning {0} objects due to parcel auto return.", ret.Value.Groups.Count);
                    else
                        m_log.Info("[SCENE]: Returning 1 object due to parcel auto return.");

                    m_asyncSceneObjectDeleter.DeleteToInventory(
                        DeRezAction.Return, ret.Value.Groups[0].RootPart.OwnerID, ret.Value.Groups, ret.Value.Groups[0].RootPart.OwnerID,
                        true, true);
                    EventManager.TriggerParcelPrimCountTainted();
                }
                m_returns.Clear();
            }
        }

        #endregion

        #region Startup Complete

        private List<string> StartupCallbacks = new List<string>();
        private List<string> StartupData = new List<string>();

        public void AddToStartupQueue(string name)
        {
            IConfig startupConfig = m_config.Configs["Startup"];
            if ((startupConfig != null &&
                !startupConfig.GetBoolean("CompleteStartupAfterAllModulesLoad", true)) ||
                name == "Startup") //We allow startup through to allow for normal starting up, even if all module loading is disabled
            {
                StartupCallbacks.Add(name);
            }
        }

        public void FinishedStartup(string name, List<string> data)
        {
            if (StartupCallbacks.Contains(name))
            {
                StartupCallbacks.Remove(name);
                if (data.Count != 0)
                {
                    List<string> NewData = new List<string>(data.Count + 2); //Fixed size to reduce memory
                    NewData.Add(name);
                    NewData.Add(data.Count.ToString());
                    NewData.AddRange(data);
                    StartupData.AddRange(NewData);
                }
                if (StartupCallbacks.Count == 0)
                {
                    //All callbacks are done, trigger startup complete
                    EventManager.TriggerStartupComplete(StartupData);
                }
            }
        }

        public void StartupComplete(List<string> data)
        {
            // In 99.9% of cases it is a bad idea to manually force garbage collection. However,
            // this is a rare case where we know we have just went through a long cycle of heap
            // allocations, and there is no more work to be done until someone logs in
            GC.Collect();

            m_log.Info("[REGION] - Startup Complete in region " + RegionInfo.RegionName);
            IConfig startupConfig = m_config.Configs["Startup"];
            if (startupConfig == null || !startupConfig.GetBoolean("StartDisabled", false))
            {
                m_log.DebugFormat("[REGION]: Enabling logins for {0}", RegionInfo.RegionName);
                LoginsDisabled = false;
            }

            base.StartupComplete(this, data);
        }

        #endregion
    }
}