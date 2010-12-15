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
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes.Serialization;
using OpenSim.Region.Physics.Manager;
using Timer = System.Timers.Timer;
using TPFlags = OpenSim.Framework.Constants.TeleportFlags;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes.Animation;

namespace OpenSim.Region.Framework.Scenes
{
    public partial class Scene : IScene, IRegistryCore
    {
        #region Fields

        protected List<UUID> m_needsDeleted = new List<UUID>();
        public List<SceneObjectGroup> PhysicsReturns = new List<SceneObjectGroup>();

        private volatile int m_bordersLocked = 0;

        /// <value>
        /// The scene graph for this scene
        /// </value>
        private SceneGraph m_sceneGraph;

        protected readonly ClientManager m_clientManager = new ClientManager();

        protected RegionInfo m_regInfo;

        public ITerrainChannel Heightmap;

        /// <value>
        /// Allows retrieval of land information for this scene.
        /// </value>
        public ILandChannel LandChannel;

        protected AuroraEventManager m_AuroraEventManager = null;
        protected EventManager m_eventManager;
        /// <value>
        /// Manage events that occur in this scene (avatar movement, script rez, etc.).  Commonly used by region modules
        /// to subscribe to scene events.
        /// </value>
        public EventManager EventManager
        {
            get { return m_eventManager; }
        }
        /// <summary>
        /// Generic manager to send and recieve events. Used mainly by region modules
        /// </summary>
        public AuroraEventManager AuroraEventManager
        {
            get { return m_AuroraEventManager; }
        }

        private SceneManager m_sceneManager;

        protected ScenePermissions m_permissions;
        /// <summary>
        /// Controls permissions for the Scene
        /// </summary>
        public ScenePermissions Permissions
        {
            get { return m_permissions; }
        }

        public volatile bool m_backingup = false;
        protected DateTime m_lastRanBackupInHeartbeat = DateTime.MinValue;

        protected Dictionary<UUID, ReturnInfo> m_returns = new Dictionary<UUID, ReturnInfo>();
        protected Dictionary<UUID, SceneObjectGroup> m_groupsWithTargets = new Dictionary<UUID, SceneObjectGroup>();

        protected IConfigSource m_config;


        protected AgentCircuitManager m_authenticateHandler;

        public bool LoginsDisabled = true;

        protected ISimulationDataStore m_SimulationDataService;
        protected IAssetService m_AssetService;
        protected IInventoryService m_InventoryService;
        protected IGridService m_GridService;
        protected ILibraryService m_LibraryService;
        protected ISimulationService m_simulationService;
        protected IAuthenticationService m_AuthenticationService;
        protected IPresenceService m_PresenceService;
        protected IUserAccountService m_UserAccountService;
        protected IAvatarService m_AvatarService;
        protected IGridUserService m_GridUserService;

        // Central Update Loop

        protected uint m_frame;
        protected float m_timespan = 0.089f;
        protected DateTime m_lastupdate = DateTime.UtcNow;

        private int m_update_physics = 1; //Trigger the physics update
        private int m_update_entitymovement = 1; //Update the movement of scene presences
        private int m_update_objects = 1; // Update objects which have scheduled themselves for updates
        private int m_update_presences = 1; // Update scene presences which have scheduled updates
        private int m_update_events = 1; //Trigger the OnFrame event and tell any modules about the new frame
        private int m_update_backup = 50; //Trigger backup
        private int m_update_terrain = 50; //Trigger the updating of the terrain mesh in the physics engine
        private int m_update_land = 10; //Check whether we need to rebuild the parcel prim count and other land related functions
        private int m_update_coarse_locations = 30; //Trigger the sending of coarse location updates (minimap updates)

        private string m_defaultScriptEngine;
        private static volatile bool shuttingdown = false;

        private object m_cleaningAttachments = new object();

        // the minimum time that must elapse before a changed object will be considered for persisted
        public long m_dontPersistBefore = 60;
        // the maximum time that must elapse before a changed object will be considered for persisted
        public long m_persistAfter = 600;

        private UpdatePrioritizationSchemes m_priorityScheme = UpdatePrioritizationSchemes.Time;
        private bool m_reprioritizationEnabled = true;
        private double m_reprioritizationInterval = 5000.0;
        private double m_rootReprioritizationDistance = 10.0;
        private double m_childReprioritizationDistance = 20.0;

        private bool EnableFakeRaycasting = false;
        private bool m_UseSelectionParticles = true;
        public bool LoadingPrims = false;
        public bool CheckForObjectCulling = false;
        public bool[,] DirectionsToBlockChildAgents;
        private string m_DefaultObjectName = "Primitive";
        public bool RunScriptsInAttachments = false;
        public bool m_usePreJump = true;
        public bool m_UseNewStyleMovement = true;
        public bool m_useSplatAnimation = true;
        public float MaxLowValue = -1000;

        #endregion

        #region Properties

        public UpdatePrioritizationSchemes UpdatePrioritizationScheme { get { return m_priorityScheme; } }
        public bool IsReprioritizationEnabled { get { return m_reprioritizationEnabled; } }
        public double ReprioritizationInterval { get { return m_reprioritizationInterval; } }
        public double RootReprioritizationDistance { get { return m_rootReprioritizationDistance; } }
        public double ChildReprioritizationDistance { get { return m_childReprioritizationDistance; } }
        protected Timer m_restartWaitTimer = new Timer();
        protected static int m_timeToSlowTheHeartbeat = 3;
        protected static int m_timeToSlowThePhysHeartbeat = 2;

        public AgentCircuitManager AuthenticateHandler
        {
            get { return m_authenticateHandler; }
        }

        public ISimulationDataStore SimulationDataService
        {
            get { return m_SimulationDataService; }
        }

        public bool ShuttingDown
        {
            get { return shuttingdown; }
        }

        public SceneGraph SceneGraph
        {
            get { return m_sceneGraph; }
        }

        public RegionInfo RegionInfo
        {
            get { return m_regInfo; }
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

        public string DefaultScriptEngine
        {
            get { return m_defaultScriptEngine; }
        }

        public EntityManager Entities
        {
            get { return m_sceneGraph.Entities; }
        }

        public string GetSimulatorVersion()
        {
            return m_sceneManager.GetSimulatorVersion();
        }

        public IConfigSource Config
        {
            get { return m_config; }
        }

        public string DefaultObjectName
        {
            get { return m_DefaultObjectName; }
        }

        public bool UseSelectionParticles
        {
            get { return m_UseSelectionParticles; }
        }

        public float TimeDilation
        {
            get { return m_sceneGraph.PhysicsScene.TimeDilation; }
            set { m_sceneGraph.PhysicsScene.TimeDilation = value; }
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

        #region Services

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

        #endregion

        #endregion

        #region Constructors

        public Scene(RegionInfo regInfo, AgentCircuitManager authen, SceneManager manager)
        {
            //THIS NEEDS RESET TO FIX RESTARTS
            shuttingdown = false;

            m_sceneManager = manager;

            //Register to regInfo events
            regInfo.OnRegionUp += new RegionInfo.TriggerOnRegionUp(regInfo_OnRegionUp);

            m_config = manager.ConfigSource;
            m_authenticateHandler = authen;
            m_regInfo = regInfo;


            BordersLocked = true;

            Border northBorder = new Border();
            northBorder.BorderLine = new Vector3(float.MinValue, float.MaxValue, (int)Constants.RegionSize);  //<---
            northBorder.CrossDirection = Cardinals.N;
            RegionInfo.NorthBorders.Add(northBorder);

            Border southBorder = new Border();
            southBorder.BorderLine = new Vector3(float.MinValue, float.MaxValue, 0);    //--->
            southBorder.CrossDirection = Cardinals.S;
            RegionInfo.SouthBorders.Add(southBorder);

            Border eastBorder = new Border();
            eastBorder.BorderLine = new Vector3(float.MinValue, float.MaxValue, (int)Constants.RegionSize);   //<---
            eastBorder.CrossDirection = Cardinals.E;
            RegionInfo.EastBorders.Add(eastBorder);

            Border westBorder = new Border();
            westBorder.BorderLine = new Vector3(float.MinValue, float.MaxValue, 0);     //--->
            westBorder.CrossDirection = Cardinals.W;
            RegionInfo.WestBorders.Add(westBorder);

            BordersLocked = false;

            m_AuroraEventManager = new AuroraEventManager();
            m_eventManager = new EventManager();
            m_permissions = new ScenePermissions(this);

            m_SimulationDataService = manager.SimulationDataService;

            // Load region settings
            m_regInfo.RegionSettings = m_SimulationDataService.LoadRegionSettings(m_regInfo.RegionID);

            //Bind Storage Manager functions to some land manager functions for this scene
            IParcelServiceConnector conn = DataManager.RequestPlugin<IParcelServiceConnector>();
            if (conn != null)
            {
                EventManager.OnLandObjectAdded +=
                    conn.StoreLandObject;
                EventManager.OnLandObjectRemoved +=
                    conn.RemoveLandObject;
            }

            EventManager.OnClosingClient += UnSubscribeToClientEvents;

            m_sceneGraph = new SceneGraph(this, m_regInfo);

            #region Region Config

            try
            {
                DirectionsToBlockChildAgents = new bool[3, 3];
                DirectionsToBlockChildAgents.Initialize();
                IConfig aurorastartupConfig = m_config.Configs["AuroraStartup"];
                if (aurorastartupConfig != null)
                {
                    UseTracker = aurorastartupConfig.GetBoolean("RunWithMultipleHeartbeats", true);
                    RunScriptsInAttachments = aurorastartupConfig.GetBoolean("AllowRunningOfScriptsInAttachments", false);
                    m_UseSelectionParticles = aurorastartupConfig.GetBoolean("UseSelectionParticles", true);
                    EnableFakeRaycasting = aurorastartupConfig.GetBoolean("EnableFakeRaycasting", false);
                    MaxLowValue = aurorastartupConfig.GetFloat("MaxLowValue", -1000);
                    Util.VariableRegionSight = aurorastartupConfig.GetBoolean("UseVariableRegionSightDistance", Util.VariableRegionSight);
                    m_DefaultObjectName = aurorastartupConfig.GetString("DefaultObjectName", m_DefaultObjectName);
                    CheckForObjectCulling = aurorastartupConfig.GetBoolean("CheckForObjectCulling", CheckForObjectCulling);
                    SetObjectCapacity(aurorastartupConfig.GetInt("ObjectCapacity", 80000));
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

                //Animation states
                IConfig animationConfig = m_config.Configs["Animations"];
                if (animationConfig != null)
                {
                    m_UseNewStyleMovement = animationConfig.GetBoolean("enableNewMovement", m_UseNewStyleMovement);
                    m_usePreJump = animationConfig.GetBoolean("enableprejump", m_usePreJump);
                    m_useSplatAnimation = animationConfig.GetBoolean("enableSplatAnimation", m_useSplatAnimation);
                }

                IConfig persistanceConfig = m_config.Configs["Persistance"];
                if (persistanceConfig != null)
                {
                    m_dontPersistBefore =
                        persistanceConfig.GetLong("MinimumTimeBeforePersistenceConsidered", m_dontPersistBefore);

                    m_persistAfter =
                        persistanceConfig.GetLong("MaximumTimeBeforePersistenceConsidered", m_persistAfter);
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
            EventManager.OnStartupFullyComplete += StartupComplete;

            AddToStartupQueue("Startup");

            #endregion

            //Add stats handlers
            MainServer.Instance.AddStreamHandler(new RegionStatsHandler(RegionInfo));
        }

        #endregion Constructors

        #region Startup / Close Methods

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
        public void OtherRegionUp(GridRegion otherRegion)
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

                    try
                    {
                        ForEachScenePresence(delegate(ScenePresence agent)
                        {
                            // If agent is a root agent.
                            if (!agent.IsChildAgent)
                            {
                                //Fix its neighbor settings and add this new region
                                List<ulong> old = new List<ulong>();
                                old.Add(otherRegion.RegionHandle);
                                agent.DropOldNeighbours(old);
                                //Now add the agent to the reigon that is coming up
                                IEntityTransferModule transferModule = RequestModuleInterface<IEntityTransferModule>();
                                if (transferModule != null)
                                    transferModule.EnableChildAgent(agent, otherRegion);
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

        private void regInfo_OnRegionUp(object otherRegion)
        {
            EventManager.TriggerOnRegionUp((GridRegion)otherRegion);
        }

        // Alias IncomingHelloNeighbour OtherRegionUp, for now
        public void IncomingHelloNeighbour(RegionInfo neighbour)
        {
            OtherRegionUp(new GridRegion(neighbour));
        }

        public void IncomingClosingNeighbour(RegionInfo neighbour)
        {
            //OtherRegionUp(new GridRegion(neighbour));
            //return new GridRegion(RegionInfo);
        }

        /// <summary>
        /// This causes the region to restart immediatley.
        /// </summary>
        public void Restart()
        {
            IConfig startupConfig = m_config.Configs["Startup"];
            if (startupConfig != null)
            {
                if (startupConfig.GetBoolean("InworldRestartShutsDown", false))
                {
                    //This will kill it asyncly
                    MainConsole.Instance.EndConsoleProcessing();
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

            restart handlerPhysicsCrash = OnRestart;
            if (handlerPhysicsCrash != null)
                handlerPhysicsCrash(RegionInfo);
        }

        /// <summary>
        /// Update the grid server with new info about this region
        /// </summary>
        public void UpdateGridRegion()
        {
            GridService.UpdateMap(RegionInfo.ScopeID, new GridRegion(RegionInfo), RegionInfo.GridSecureSessionID);
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

        /// <summary>
        /// This is the method that shuts down the scene.
        /// </summary>
        public void Close()
        {
            m_log.InfoFormat("[SCENE]: Closing down the single simulator: {0}", RegionInfo.RegionName);

            // Kick all ROOT agents with the message, 'The simulator is going down'
            ForEachScenePresence(delegate(ScenePresence avatar)
            {
                if (!avatar.IsChildAgent)
                    avatar.ControllingClient.Kick("The simulator is going down.");

                avatar.ControllingClient.SendShutdownConnectionNotice();
            });

            // Wait here, or the kick messages won't actually get to the agents before the scene terminates.
            Thread.Sleep(500);

            // Stop all client threads.
            ForEachScenePresence(delegate(ScenePresence avatar) { avatar.ControllingClient.Close(); });

            if (UseTracker)
            {
                if (tracker != null)
                {
                    tracker.OnNeedToAddThread -= NeedsNewThread;
                    tracker.Close();
                    tracker = null;
                }
            }

            //Tell the neighbors that this region is now down
            INeighbourService service = RequestModuleInterface<INeighbourService>();
            if (service != null)
                service.InformNeighborsThatRegionIsDown(RegionInfo);

            // Stop updating the scene objects and agents.
            //m_heartbeatTimer.Close();
            shuttingdown = true;

            m_log.Info("[SCENE]: Persisting changed objects...");

            //Backup uses the new taints system
            m_backingup = true; //Clear out all other threads
            ProcessPrimBackupTaints(true, false);

            m_sceneGraph.Close();

            m_log.InfoFormat("[SCENE]: Deregistering region {0} from the grid...", m_regInfo.RegionName);

            //Deregister from the grid server
            if (!GridService.DeregisterRegion(m_regInfo.RegionID, RegionInfo.GridSecureSessionID))
                m_log.WarnFormat("[SCENE]: Deregister from grid failed for region {0}", m_regInfo.RegionName);

            //Trigger the last event
            try
            {
                EventManager.TriggerShutdown();
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[SCENE]: Close() - Failed with exception ", e);
            }
        }

        #region Tracker

        public AuroraThreadTracker tracker = null;
        private bool UseTracker = true;
        /// <summary>
        /// Start the timer which triggers regular scene updates
        /// </summary>
        public void StartTimer()
        {
            if (tracker == null)
                tracker = new AuroraThreadTracker();
            if (UseTracker)
            {
                ScenePhysicsHeartbeat shb = new ScenePhysicsHeartbeat(this);
                SceneUpdateHeartbeat suhb = new SceneUpdateHeartbeat(this);
                tracker.AddSceneHeartbeat(suhb);
                tracker.AddSceneHeartbeat(shb);
            }
            else
            {
                tracker.AddSceneHeartbeat(new SceneHeartbeat(this));
            }
            //Start this after the threads are started.
            tracker.Init(this);
            tracker.OnNeedToAddThread += NeedsNewThread;
        }

        protected void NeedsNewThread(string type)
        {
            if (type == "SceneUpdateHeartbeat")
                tracker.AddSceneHeartbeat(new SceneUpdateHeartbeat(this));
            else if (type == "ScenePhysicsHeartbeat")
                tracker.AddSceneHeartbeat(new ScenePhysicsHeartbeat(this));
        }

        #endregion

        #endregion

        #region Update Methods

        #region Scene Heartbeat parts

        protected class SceneUpdateHeartbeat : IThread
        {
            #region Constructor and IThread

            public SceneUpdateHeartbeat(Scene scene)
            {
                type = "SceneBackupHeartbeat";
                m_scene = scene;
            }

            public override void Restart()
            {
                ShouldExit = true;
                SceneUpdateHeartbeat heartbeat = new SceneUpdateHeartbeat(m_scene);
                m_scene.tracker.AddSceneHeartbeat(heartbeat);
            }

            /// <summary>
            /// Performs per-frame updates regularly
            /// </summary>
            public override void Start()
            {
                try
                {
                    Update(true);
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    m_log.Error("[Scene]: Failed with " + ex);
                }
                FireThreadClosing(this);
                Thread.CurrentThread.Abort();
            }

            private void CheckExit()
            {
                LastUpdate = DateTime.Now;
                if (!ShouldExit && !shuttingdown)
                    return;
                //Lets kill this thing
                throw new Exception("Closing");
            }

            #endregion

            #region Update

            public void Update(bool shouldSleep)
            {
                ISimFrameMonitor simFrameMonitor = (ISimFrameMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "SimFrameStats");
                ISetMonitor totalFrameMonitor = (ISetMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Total Frame Time");
                ISetMonitor lastFrameMonitor = (ISetMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Last Completed Frame At");
                ISetMonitor otherFrameMonitor = (ISetMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Other Frame Time");
                ISetMonitor sleepFrameMonitor = (ISetMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Sleep Frame");
                int maintc;

                while (!ShouldExit)
                {
                    maintc = Util.EnvironmentTickCount();
                    int BeginningFrameTime = maintc;
                    
                    // Increment the frame counter
                    ++m_scene.m_frame;

                    try
                    {
                        int OtherFrameTime = Util.EnvironmentTickCount();
                        // Delete temp-on-rez stuff
                        if (m_scene.m_frame % m_scene.m_update_backup == 0)
                        {
                            m_scene.CleanTempObjects();
                            m_scene.CheckParcelReturns();
                        }
                        if (m_scene.PhysicsReturns.Count != 0)
                        {
                            lock (m_scene.PhysicsReturns)
                            {
                                m_scene.returnObjects(m_scene.PhysicsReturns.ToArray(), UUID.Zero);
                                m_scene.PhysicsReturns.Clear();
                            }
                        }
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
                        if (m_scene.m_frame % m_scene.m_update_events == 0)
                            m_scene.UpdateEvents();

                        if (m_scene.m_frame % m_scene.m_update_backup == 0)
                            m_scene.UpdateStorageBackup();

                        if (m_scene.m_frame % m_scene.m_update_terrain == 0)
                            m_scene.UpdateTerrain();

                        if (m_scene.m_frame % m_scene.m_update_land == 0)
                            m_scene.UpdateLand();

                        // Check if any objects have reached their targets
                        m_scene.CheckAtTargets();

                        int MonitorOtherFrameTime = Util.EnvironmentTickCountSubtract(OtherFrameTime);

                        maintc = Util.EnvironmentTickCountSubtract(maintc);
                        maintc = ((int)(m_scene.m_timespan * 1000) - maintc) / Scene.m_timeToSlowTheHeartbeat;

                        int MonitorSleepFrameTime = maintc;

                        int MonitorLastCompletedFrame = Util.EnvironmentTickCount();
                        int MonitorFrameTime = Util.EnvironmentTickCountSubtract(BeginningFrameTime);

                        //Now fix the stats
                        simFrameMonitor.AddFPS(1);
                        totalFrameMonitor.SetValue(1);
                        lastFrameMonitor.SetValue(MonitorLastCompletedFrame);
                        sleepFrameMonitor.SetValue(MonitorSleepFrameTime);
                        otherFrameMonitor.SetValue(MonitorOtherFrameTime);

                        CheckExit();
                    }
                    catch (Exception e)
                    {
                        if (e.Message != "Closing")
                            m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                        break;
                    }

                    if (maintc > 0 && shouldSleep)
                        Thread.Sleep(maintc);
                }
            }

            #endregion
        }

        protected class ScenePhysicsHeartbeat : IThread
        {
            #region Constructor and IThread

            public ScenePhysicsHeartbeat(Scene scene)
            {
                type = "ScenePhysicsHeartbeat";
                m_scene = scene;
            }

            public override void Restart()
            {
                ShouldExit = true;
                ScenePhysicsHeartbeat heartbeat = new ScenePhysicsHeartbeat(m_scene);
                m_scene.tracker.AddSceneHeartbeat(heartbeat);
            }

            /// <summary>
            /// Performs per-frame updates regularly
            /// </summary>
            public override void Start()
            {
                try
                {
                    Update(true);
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    m_log.Error("[Scene]: Failed with " + ex);
                }
                FireThreadClosing(this);
                Thread.CurrentThread.Abort();
            }

            private void CheckExit()
            {
                LastUpdate = DateTime.Now;
                if (!ShouldExit && !shuttingdown)
                    return;
                //Lets kill this thing
                throw new Exception("Closing");
            }

            #endregion

            #region Update

            public void Update(bool shouldSleep)
            {
                IPhysicsFrameMonitor physicsFrameMonitor = (IPhysicsFrameMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Total Physics Frame Time");
                ISetMonitor physicsSyncFrameMonitor = (ISetMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Physics Update Frame Time");
                int maintc;

                while (!ShouldExit)
                {
                    TimeSpan SinceLastFrame = DateTime.UtcNow - m_scene.m_lastupdate;

                    maintc = Util.EnvironmentTickCount();
                    
                    try
                    {
                        int PhysicsSyncTime = Util.EnvironmentTickCount();

                        // Update SceneObjectGroups that have scheduled themselves for updates
                        // Objects queue their updates onto all scene presences
                        if (m_scene.m_frame % m_scene.m_update_objects == 0)
                            m_scene.m_sceneGraph.UpdateObjectGroups();

                        // Run through all ScenePresences looking for updates
                        // Presence updates and queued object updates for each presence are sent to clients
                        if (m_scene.m_frame % m_scene.m_update_presences == 0)
                            m_scene.m_sceneGraph.UpdatePresences();

                        if ((m_scene.m_frame % m_scene.m_update_physics == 0) && !m_scene.RegionInfo.RegionSettings.DisablePhysics)
                            m_scene.m_sceneGraph.UpdatePreparePhysics();
                        
                        if (m_scene.m_frame % m_scene.m_update_entitymovement == 0)
                            m_scene.m_sceneGraph.UpdateScenePresenceMovement();

                        int MonitorPhysicsSyncTime = Util.EnvironmentTickCountSubtract(PhysicsSyncTime);

                        int PhysicsUpdateTime = Util.EnvironmentTickCount();

                        if (m_scene.m_frame % m_scene.m_update_physics == 0)
                        {
                            if (!m_scene.RegionInfo.RegionSettings.DisablePhysics)
                                m_scene.m_sceneGraph.UpdatePhysics(Math.Max(SinceLastFrame.TotalSeconds, m_scene.m_timespan));
                        }

                        int MonitorPhysicsUpdateTime = Util.EnvironmentTickCountSubtract(PhysicsUpdateTime) + MonitorPhysicsSyncTime;

                        physicsFrameMonitor.AddFPS(1);
                        physicsSyncFrameMonitor.SetValue(MonitorPhysicsSyncTime);
                        
                        CheckExit();
                    }
                    catch (Exception e)
                    {
                        if (e.Message != "Closing")
                            m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                        break;
                    }
                    finally
                    {
                        m_scene.m_lastupdate = DateTime.UtcNow;
                    }

                    maintc = Util.EnvironmentTickCountSubtract(maintc);
                    maintc = (int)((m_scene.m_timespan * 1000) - maintc) / Scene.m_timeToSlowThePhysHeartbeat;

                    if (maintc > 0 && shouldSleep)
                        Thread.Sleep(maintc);
                }
            }

            #endregion
        }

        protected class SceneHeartbeat : IThread
        {
            #region Constructor and IThread

            ScenePhysicsHeartbeat physH;
            SceneUpdateHeartbeat updateH;

            public SceneHeartbeat(Scene scene)
            {
                physH = new ScenePhysicsHeartbeat(scene);
                updateH = new SceneUpdateHeartbeat(scene);
                type = "SceneHeartbeat";
                m_scene = scene;
            }

            public override void Restart()
            {
                ShouldExit = true;
                SceneHeartbeat heartbeat = new SceneHeartbeat(m_scene);
                m_scene.tracker.AddSceneHeartbeat(heartbeat);
            }

            /// <summary>
            /// Performs per-frame updates regularly
            /// </summary>
            public override void Start()
            {
                try
                {
                    Update();
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    m_log.Error("[Scene]: Failed with " + ex);
                }
                FireThreadClosing(this);
                Thread.CurrentThread.Abort();
            }

            private void CheckExit()
            {
                LastUpdate = DateTime.Now;
                if (!ShouldExit && !shuttingdown)
                    return;
                throw new Exception("Closing");
            }

            #endregion

            #region Update

            public void Update()
            {
                int maintc;

                while (!ShouldExit)
                {
                    maintc = Util.EnvironmentTickCount();
                    //Update all of the threads without sleeping, then sleep down at the bottom
                    physH.Update(false);
                    updateH.Update(false);
                    maintc = Util.EnvironmentTickCountSubtract(maintc);
                    maintc = (int)(m_scene.m_timespan * 1000) - maintc;

                    if (maintc > 0)
                        Thread.Sleep(maintc);
                }
            }

            #endregion
        }

        #endregion

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
            //Check every min persistant times as well except when it is set to 0
            if (!m_backingup || (m_lastRanBackupInHeartbeat.Ticks > DateTime.Now.Ticks
                && m_dontPersistBefore != 0))
            {
                //Add the time now plus minimum persistance time so that we can force a run if it goes wrong
                m_lastRanBackupInHeartbeat = DateTime.Now.AddMinutes((m_dontPersistBefore / 10000000L));
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
            ProcessPrimBackupTaints(forced, false);
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
                bool shouldReaddToLoop;
                bool shouldReaddToLoopNow;
                group.ProcessBackup(SimulationDataService, true, out shouldReaddToLoop, out shouldReaddToLoopNow);
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
        /// Register this region with a grid service
        /// </summary>
        /// <exception cref="System.Exception">Thrown if registration of the region itself fails.</exception>
        public string RegisterRegionWithGrid()
        {
            // These two 'commands' *must be* next to each other or sim rebooting fails.
            //m_sceneGridService.RegisterRegion(m_interregionCommsOut, RegionInfo);

            GridRegion region = new GridRegion(RegionInfo);

            IGenericsConnector g = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            GridSessionID s = null;
            if (g != null) //Get the sessionID from the database if possible
                s = g.GetGeneric<GridSessionID>(RegionInfo.RegionID, "GridSessionID", GridService.GridServiceURL, new GridSessionID());

            if (s == null)
            {
                s = new GridSessionID();
                //Set it from the regionInfo if it knows anything
                s.SessionID = RegionInfo.GridSecureSessionID;
            }

            string error = GridService.RegisterRegion(RegionInfo.ScopeID, region, s.SessionID, out s.SessionID);
            if (error != String.Empty)
                return error;
            RegionInfo.GridSecureSessionID = s.SessionID;

            //Save the new SessionID to the database
            g.AddGeneric(RegionInfo.RegionID, "GridSessionID", GridService.GridServiceURL, s.ToOSD());

            INeighbourService service = this.RequestModuleInterface<INeighbourService>();
            if (service != null)
                service.InformNeighborsThatRegionIsUp(RegionInfo);
            return "";
        }

        public class GridSessionID : IDataTransferable
        {
            public UUID SessionID;
            public override void FromOSD(OSDMap map)
            {
                SessionID = map["SessionID"].AsUUID();
            }

            public override OSDMap ToOSD()
            {
                OSDMap map = new OSDMap();
                map.Add("SessionID", SessionID);
                return map;
            }

            public override Dictionary<string, object> ToKeyValuePairs()
            {
                return Util.OSDToDictionary(ToOSD());
            }

            public override void FromKVP(Dictionary<string, object> KVP)
            {
                FromOSD(Util.DictionaryToOSD(KVP));
            }

            public override IDataTransferable Duplicate()
            {
                GridSessionID m = new GridSessionID();
                m.FromOSD(ToOSD());
                return m;
            }
        }

        #endregion

        #region Load Land

        /// <summary>
        /// Loads all Parcel data from the datastore for region identified by regionID
        /// </summary>
        /// <param name="regionID">Unique Identifier of the Region to load parcel data for</param>
        public void loadAllLandObjectsFromStorage(UUID regionID)
        {
            m_log.Info("[SCENE]: Loading Land Objects from database... ");
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
                SceneGraph.CheckAllocationOfLocalIds(group);
                if (group.IsAttachment || (group.RootPart.Shape != null && (group.RootPart.Shape.State != 0 &&
                    (group.RootPart.Shape.PCode == (byte)PCode.None ||
                    group.RootPart.Shape.PCode == (byte)PCode.Prim ||
                    group.RootPart.Shape.PCode == (byte)PCode.Avatar))))
                {
                    m_log.Warn("[SCENE]: Broken state for object " + group.Name + " while loading objects, removing it from the database.");
                    //WTF went wrong here? Remove it and then pass it by on loading
                    SimulationDataService.RemoveObject(group.UUID, this.RegionInfo.RegionID);
                    continue;
                }
                group.Scene = this;
                EventManager.TriggerOnSceneObjectLoaded(group);

                if (group.RootPart == null)
                {
                    m_log.ErrorFormat("[SCENE] Found a SceneObjectGroup with m_rootPart == null and {0} children",
                                      group.ChildrenList.Count);
                    continue;
                }
                RestorePrimToScene(group);
                SceneObjectPart rootPart = group.GetChildPart(group.UUID);
                rootPart.Flags &= ~PrimFlags.Scripted;
                rootPart.TrimPermissions();
                group.CheckSculptAndLoad();
            }
            LoadingPrims = false;
            m_log.Info("[SCENE]: Loaded " + PrimsFromDB.Count.ToString() + " SceneObject(s)");
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
                    //m_log.Info("[RAYTRACERESULTS]: Hit:" + ei.HitTF.ToString() + " Point: " + ei.ipoint.ToString() + " Normal: " + ei.normal.ToString());

                    if (ei.HitTF)
                    {
                        pos = new Vector3(ei.ipoint.X, ei.ipoint.Y, ei.ipoint.Z);
                    }
                    else
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

                AddPrimToScene(sceneObject);
                sceneObject.ScheduleGroupForFullUpdate(PrimUpdateFlags.FullUpdate);
                sceneObject.SetGroup(groupID, null);
            }


            return sceneObject;
        }

        public bool AddPrimToScene(SceneObjectGroup sceneObject)
        {
            return m_sceneGraph.AddPrimToScene(sceneObject);
        }

        public bool RestorePrimToScene(SceneObjectGroup sceneObject)
        {
            return m_sceneGraph.RestorePrimToScene(sceneObject);
        }

        public void PrepPrimForAdditionToScene(SceneObjectGroup sceneObject)
        {
            m_sceneGraph.PrepPrimForAdditionToScene(sceneObject);
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

                        group.RemoveScriptInstances(true);

                        foreach (SceneObjectPart part in group.ChildrenList)
                        {
                            if (part.IsJoint() && ((part.Flags & PrimFlags.Physics) != 0))
                            {
                                PhysicsScene.RequestJointDeletion(part.Name); // FIXME: what if the name changed?
                            }
                            else if (part.PhysActor != null)
                            {
                                PhysicsScene.RemovePrim(part.PhysActor);
                                part.PhysActor = null;
                            }
                        }

                        m_sceneGraph.DeleteEntity(group);
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
                            SP.StandUp(false);
                    }
                }
            }

            if (DeleteScripts)
            {
                group.RemoveScriptInstances(true);
            }
            foreach (SceneObjectPart part in group.ChildrenList)
            {
                if (part.IsJoint() && ((part.Flags & PrimFlags.Physics) != 0))
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
            if (m_sceneGraph.DeleteEntity(so))
            {
                if (!softDelete)
                {
                    DeleteFromStorage(so.UUID);
                    // We need to keep track of this state in case this group is still queued for further backup.
                    so.IsDeleted = true;
                }
                return true;
            }

            return false;
        }

        public void CleanDroppedAttachments()
        {
            List<SceneObjectGroup> objectsToDelete =
                    new List<SceneObjectGroup>();

            lock (m_cleaningAttachments)
            {
                ForEachSOG(delegate(SceneObjectGroup grp)
                {
                    if (grp.RootPart.Shape.PCode == 0 && grp.RootPart.Shape.State != 0 && (!objectsToDelete.Contains(grp)))
                    {
                        UUID agentID = grp.OwnerID;
                        if (agentID == UUID.Zero)
                        {
                            objectsToDelete.Add(grp);
                            return;
                        }

                        ScenePresence sp = GetScenePresence(agentID);
                        if (sp == null)
                        {
                            objectsToDelete.Add(grp);
                            return;
                        }
                    }
                });
            }

            foreach (SceneObjectGroup grp in objectsToDelete)
            {
                m_log.InfoFormat("[SCENE]: Deleting dropped attachment {0} of user {1}", grp.UUID, grp.OwnerID);
                DeleteSceneObject(grp, true, true);
            }
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
            IEntityTransferModule transferModule = RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                transferModule.Cross(grp, attemptedPosition, silent);
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

        public ISceneObject DeserializeObject(string representation)
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
                        IDialogModule module = RequestModuleInterface<IDialogModule>();
                        if (module != null)
                            module.SendAlertToUser(remoteClient, "This item doesn't appear to be for sale");
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
                        IDialogModule module = RequestModuleInterface<IDialogModule>();
                        if (module != null)
                            module.SendAlertToUser(remoteClient, "This item doesn't appear to be for sale");
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
                        IDialogModule module = RequestModuleInterface<IDialogModule>();
                        if (module != null)
                            module.SendAlertToUser(remoteClient, "Cannot buy now. Your inventory is unavailable");
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
                        IDialogModule module = RequestModuleInterface<IDialogModule>();
                        if (module != null)
                            module.SendAlertToUser(
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
                        lock (RegionInfo.NorthBorders)
                        {
                            foreach (Border b in RegionInfo.NorthBorders)
                            {
                                if (b.TestCross(position))
                                    return b;
                            }
                        }
                        break;
                    case Cardinals.S:
                        lock (RegionInfo.SouthBorders)
                        {
                            foreach (Border b in RegionInfo.SouthBorders)
                            {
                                if (b.TestCross(position))
                                    return b;
                            }
                        }

                        break;
                    case Cardinals.E:
                        lock (RegionInfo.EastBorders)
                        {
                            foreach (Border b in RegionInfo.EastBorders)
                            {
                                if (b.TestCross(position))
                                    return b;
                            }
                        }

                        break;
                    case Cardinals.W:

                        lock (RegionInfo.WestBorders)
                        {
                            foreach (Border b in RegionInfo.WestBorders)
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
                        foreach (Border b in RegionInfo.NorthBorders)
                        {
                            if (b.TestCross(position))
                                return b;
                        }

                        break;
                    case Cardinals.S:
                        foreach (Border b in RegionInfo.SouthBorders)
                        {
                            if (b.TestCross(position))
                                return b;
                        }
                        break;
                    case Cardinals.E:
                        foreach (Border b in RegionInfo.EastBorders)
                        {
                            if (b.TestCross(position))
                                return b;
                        }

                        break;
                    case Cardinals.W:
                        foreach (Border b in RegionInfo.WestBorders)
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
                        lock (RegionInfo.NorthBorders)
                        {
                            foreach (Border b in RegionInfo.NorthBorders)
                            {
                                if (b.TestCross(position))
                                    return true;
                            }
                        }
                        break;
                    case Cardinals.E:
                        lock (RegionInfo.EastBorders)
                        {
                            foreach (Border b in RegionInfo.EastBorders)
                            {
                                if (b.TestCross(position))
                                    return true;
                            }
                        }
                        break;
                    case Cardinals.S:
                        lock (RegionInfo.SouthBorders)
                        {
                            foreach (Border b in RegionInfo.SouthBorders)
                            {
                                if (b.TestCross(position))
                                    return true;
                            }
                        }
                        break;
                    case Cardinals.W:
                        lock (RegionInfo.WestBorders)
                        {
                            foreach (Border b in RegionInfo.WestBorders)
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
                        foreach (Border b in RegionInfo.NorthBorders)
                        {
                            if (b.TestCross(position))
                                return true;
                        }
                        break;
                    case Cardinals.E:
                        foreach (Border b in RegionInfo.EastBorders)
                        {
                            if (b.TestCross(position))
                                return true;
                        }
                        break;
                    case Cardinals.S:
                        foreach (Border b in RegionInfo.SouthBorders)
                        {
                            if (b.TestCross(position))
                                return true;
                        }
                        break;
                    case Cardinals.W:
                        foreach (Border b in RegionInfo.WestBorders)
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
            IAttachmentsModule attachMod = RequestModuleInterface<IAttachmentsModule>();
            if (sp != null && attachMod != null)
            {
                int attPt = sp.Appearance.GetAttachpoint(itemID);
                attachMod.RezSingleAttachmentFromInventory(sp.ControllingClient, itemID, attPt);
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

            if (sceneObject.IsAttachmentCheckFull()) // Attachment
            {
                sceneObject.RootPart.AddFlag(PrimFlags.TemporaryOnRez);
                sceneObject.RootPart.AddFlag(PrimFlags.Phantom);
                AddPrimToScene(sceneObject);

                // Fix up attachment Parent Local ID
                ScenePresence sp = GetScenePresence(sceneObject.OwnerID);

                if (sp != null)
                {
                    m_log.DebugFormat(
                        "[ATTACHMENT]: Received attachment {0}, inworld asset id {1}", sceneObject.GetFromItemID(), sceneObject.UUID);
                    m_log.DebugFormat(
                        "[ATTACHMENT]: Attach to avatar {0} at position {1}", sp.UUID, sceneObject.AbsolutePosition);

                    IAttachmentsModule attachModule = RequestModuleInterface<IAttachmentsModule>();
                    if (attachModule != null)
                        attachModule.AttachObject(sp.ControllingClient, sceneObject.LocalId, 0, false);

                    sceneObject.RootPart.RemFlag(PrimFlags.TemporaryOnRez);
                }
                else
                {
                    sceneObject.RootPart.RemFlag(PrimFlags.TemporaryOnRez);
                    sceneObject.RootPart.AddFlag(PrimFlags.TemporaryOnRez);
                }
            }
            else
            {
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
                AddPrimToScene(sceneObject);
            }
            sceneObject.SendGroupFullUpdate(PrimUpdateFlags.FullUpdate);

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
        public void AddNewClient(IClientAPI client)
        {
            AgentCircuitData aCircuit = m_authenticateHandler.GetAgentCircuitData(client.CircuitCode);
            bool vialogin = false;

            if (aCircuit == null) // no good, didn't pass NewUserConnection successfully
                return;

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
            if (!aCircuit.child)
            {
                sp.IsChildAgent = false;
                Util.FireAndForget(delegate(object o) { sp.RezAttachments(); });
            }

            if (GetScenePresence(client.AgentId) != null)
            {
                EventManager.TriggerOnNewClient(client);
                if (vialogin)
                    EventManager.TriggerOnClientLogin(client);
            }

            //Leave these below so we don't allow clients to be able to get fake entrance into the sim as this DOES set up the checking for IPs
            m_clientManager.Add(client);
            SubscribeToClientEvents(client);

            ILoginMonitor monitor = (ILoginMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor("", "LoginMonitor");
            if (!sp.IsChildAgent && monitor != null)
            {
                monitor.AddSuccessfulLogin();
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
                //m_log.DebugFormat("[SCENE]: Incoming client {0} {1} in region {2} via regular login. Client IP verification not performed.",
                //    aCircuit.firstname, aCircuit.lastname, RegionInfo.RegionName);
                vialogin = true;
            }

            return true;
        }

        // Called by Caps, on the first HTTP contact from the client
        public bool CheckClient(UUID agentID, System.Net.IPEndPoint ep)
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
                        if (sp != null)
                        {
                            m_log.Warn("[Scene]: Could not verify client " + sp.Name + " in region " + RegionInfo.RegionName + ", logging them out of the grid");
                            PresenceService.LogoutAgent(sp.ControllingClient.SessionId);
                            sp.ControllingClient.Close();
                        }

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
            client.OnParcelDisableObjectsRequest += LandChannel.DisableObjectsInParcel;
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
            client.OnViewerEffect += ProcessViewerEffect;
        }

        /// <summary>
        /// Unsubscribe the client from events.
        /// </summary>
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
            client.OnParcelDisableObjectsRequest -= LandChannel.DisableObjectsInParcel;
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
            client.OnViewerEffect -= ProcessViewerEffect;
        }

        /// <summary>
        /// Teleport an avatar to their home region
        /// </summary>
        /// <param name="agentId">The avatar's Unique ID</param>
        /// <param name="client">The IClientAPI for the client</param>
        public virtual void TeleportClientHome(UUID agentId, IClientAPI client)
        {
            IEntityTransferModule transferModule = RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                transferModule.TeleportHome(agentId, client);
            else
            {
                m_log.DebugFormat("[SCENE]: Unable to teleport user home: no AgentTransferModule is active");
                client.SendTeleportFailed("Unable to perform teleports on this simulator.");
            }
        }

        /// <summary>
        /// Send the region heightmap to the client
        /// </summary>
        /// <param name="RemoteClient">Client to send to</param>
        public void SendLayerData(IClientAPI RemoteClient)
        {
            RemoteClient.SendLayerData(Heightmap.GetFloatsSerialised(this));
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
                            m_sceneGraph.DuplicateObject(localID, pos, target.GetEffectiveObjectFlags(), AgentID, GroupID, Quaternion.Identity);
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
            IDialogModule module = RequestModuleInterface<IDialogModule>();
            if (Permissions.CanSetHome(SP.UUID))
            {
                position.Z += SP.Appearance.AvatarHeight / 2;
                if (GridUserService != null &&
                    GridUserService.SetHome(remoteClient.AgentId.ToString(), RegionInfo.RegionID, position, lookAt) &&
                    module != null) //Do this last so it doesn't screw up the rest
                {
                    // FUBAR ALERT: this needs to be "Home position set." so the viewer saves a home-screenshot.
                    module.SendAlertToUser(remoteClient, "Home position set.");
                }
                else if (module != null)
                    module.SendAlertToUser(remoteClient, "Set Home request failed.");
            }
            else if (module != null)
                module.SendAlertToUser(remoteClient, "Set Home request failed: Permissions do not allow the setting of home here.");
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
        public void RemoveClient(UUID agentID)
        {
            bool childagentYN = false;
            ScenePresence avatar = GetScenePresence(agentID);
            if (avatar != null)
            {
                childagentYN = avatar.IsChildAgent;

                if (avatar.ParentID != UUID.Zero)
                {
                    avatar.StandUp(true);
                }

                try
                {
                    if (!childagentYN)
                        m_log.DebugFormat(
                            "[SCENE]: Removing {0} agent {1} from region {2}",
                            (childagentYN ? "child" : "root"), agentID, RegionInfo.RegionName);

                    m_sceneGraph.removeUserCount(!childagentYN);
                    ICapabilitiesModule module = RequestModuleInterface<ICapabilitiesModule>();
                    if (module != null)
                        module.RemoveCapsHandler(agentID);

                    if (!avatar.IsChildAgent)
                    {
                        INeighbourService service = RequestModuleInterface<INeighbourService>();
                        if (service != null)
                            service.CloseAllNeighborAgents(agentID, RegionInfo.RegionID);
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
                        try { client.SendKillObject(avatar.RegionHandle, new ISceneEntity[] { avatar }); }
                        catch (NullReferenceException) { }
                    });

                CleanDroppedAttachments();

                IAgentAssetTransactions agentTransactions = this.RequestModuleInterface<IAgentAssetTransactions>();
                if (agentTransactions != null)
                {
                    agentTransactions.RemoveAgentAssetTransactions(agentID);
                }

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

                // Remove the avatar from the scene
                m_sceneGraph.RemoveScenePresence(agentID);
                m_clientManager.Remove(agentID);

                m_authenticateHandler.RemoveCircuit(avatar.ControllingClient.CircuitCode);
                //m_log.InfoFormat("[SCENE] Memory pre  GC {0}", System.GC.GetTotalMemory(false));
                //m_log.InfoFormat("[SCENE] Memory post GC {0}", System.GC.GetTotalMemory(true));
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
            bool retVal = NewUserConnection(agent, teleportFlags, out reason, true);
            if (!retVal && reason != string.Empty)
                m_log.Warn("[Scene]: NewUserConnection failed with reason " + reason);
            return retVal;
        }

        /// <summary>
        /// Do the work necessary to initiate a new user connection for a particular scene.
        /// At the moment, this consists of setting up the caps infrastructure
        /// The return bool should allow for connections to be refused, but as not all calling paths
        /// take proper notice of it let, we allowed banned users in still.
        /// </summary>
        /// <param name="agent">CircuitData of the agent who is connecting</param>
        /// <param name="reason">Outputs the reason for the false response on this string</param>
        /// <param name="requirePresenceLookup">True for normal presence. False for NPC
        /// or other applications where a full grid/Hypergrid presence may not be required.</param>
        /// <returns>True if the region accepts this agent.  False if it does not.  False will 
        /// also return a reason.</returns>
        public bool NewUserConnection(AgentCircuitData agent, uint teleportFlags, out string reason, bool requirePresenceLookup)
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
            if (!agent.child)
                m_log.DebugFormat(
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
            if (!Permissions.CanTeleport(agent.AgentID, agent.startpos, agent, out agent.startpos, out reason))
                return false;

            if (!agent.child)
                m_log.InfoFormat(
                    "[CONNECTION BEGIN]: Region {0} authenticated and authorized incoming {1} agent {2} {3} {4} (circuit code {5})",
                    RegionInfo.RegionName, (agent.child ? "child" : "root"), agent.firstname, agent.lastname,
                    agent.AgentID, agent.circuitcode);

            ICapabilitiesModule module = RequestModuleInterface<ICapabilitiesModule>();
            if (module != null)
                module.NewUserConnection(agent);

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

                if (module != null)
                    module.AddCapsHandler(agent.AgentID);
            }
            else
            {
                if (sp.IsChildAgent)
                {
                    //m_log.DebugFormat(
                    //    "[SCENE]: Adjusting known seeds for existing agent {0} in {1}",
                    //    agent.AgentID, RegionInfo.RegionName);

                    sp.AdjustKnownSeeds();
                }
            }


            // In all cases, add or update the circuit data with the new agent circuit data and teleport flags
            agent.teleportFlags = teleportFlags;
            m_authenticateHandler.AddNewCircuit(agent.circuitcode, agent);

            if (vialogin)
            {
                CleanDroppedAttachments();
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
                    lock (RegionInfo.EastBorders)
                    {
                        if (agent.startpos.X > RegionInfo.EastBorders[0].BorderLine.Z)
                        {
                            m_log.Warn("FIX AGENT POSITION");
                            agent.startpos.X = RegionInfo.EastBorders[0].BorderLine.Z * 0.5f;
                            if (agent.startpos.Z > 720)
                                agent.startpos.Z = 720;
                        }
                    }
                    lock (RegionInfo.NorthBorders)
                    {
                        if (agent.startpos.Y > RegionInfo.NorthBorders[0].BorderLine.Z)
                        {
                            m_log.Warn("FIX Agent POSITION");
                            agent.startpos.Y = RegionInfo.NorthBorders[0].BorderLine.Z * 0.5f;
                            if (agent.startpos.Z > 720)
                                agent.startpos.Z = 720;
                        }
                    }
                }
                else
                {
                    if (agent.startpos.X > RegionInfo.EastBorders[0].BorderLine.Z)
                    {
                        m_log.Warn("FIX AGENT POSITION");
                        agent.startpos.X = RegionInfo.EastBorders[0].BorderLine.Z * 0.5f;
                        if (agent.startpos.Z > 720)
                            agent.startpos.Z = 720;
                    }
                    if (agent.startpos.Y > RegionInfo.NorthBorders[0].BorderLine.Z)
                    {
                        m_log.Warn("FIX Agent POSITION");
                        agent.startpos.Y = RegionInfo.NorthBorders[0].BorderLine.Z * 0.5f;
                        if (agent.startpos.Z > 720)
                            agent.startpos.Z = 720;
                    }
                }
                //Keep users from being underground
                if (agent.startpos.Z < GetNormalizedGroundHeight(agent.startpos.X, agent.startpos.Y))
                {
                    agent.startpos.Z = GetNormalizedGroundHeight(agent.startpos.X, agent.startpos.Y) + 1;
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

            IAuthorizationService AuthorizationService = RequestModuleInterface<IAuthorizationService>();
            if (AuthorizationService != null)
            {
                if (!AuthorizationService.IsAuthorizedForRegion(agent.AgentID.ToString(), RegionInfo.RegionID.ToString(), out reason))
                {
                    if (Permissions.IsGod(agent.AgentID)) return true;

                    m_log.WarnFormat("[CONNECTION BEGIN]: Denied access to: {0} ({1} {2}) at {3} because the user does not have access to the region",
                                     agent.AgentID, agent.firstname, agent.lastname, RegionInfo.RegionName);
                    reason = String.Format("You do not have access to the region {0}",RegionInfo.RegionName);
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
                    IEventQueueService eq = RequestModuleInterface<IEventQueueService>();
                    if (eq != null)
                    {
                        eq.DisableSimulator(RegionInfo.RegionHandle, agentID, RegionInfo.RegionHandle);
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

                int shiftx = (int)regionX - (int)m_regInfo.RegionLocX * (int)Constants.RegionSize;
                int shifty = (int)regionY - (int)m_regInfo.RegionLocY * (int)Constants.RegionSize;

                position.X += shiftx;
                position.Y += shifty;

                bool result = false;

                if (TestBorderCross(position, Cardinals.N))
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

                IEntityTransferModule transferModule = RequestModuleInterface<IEntityTransferModule>();
                if (transferModule != null)
                {
                    transferModule.Teleport(sp, regionHandle, position, lookAt, teleportFlags);
                }
                else
                {
                    m_log.DebugFormat("[SCENE]: Unable to perform teleports: no AgentTransferModule is active");
                    sp.ControllingClient.SendTeleportFailed("Unable to perform teleports on this simulator.");
                }
            }
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
            IEntityTransferModule transferModule = RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                transferModule.CancelTeleport(client.AgentId);
        }

        public void CrossAgentToNewRegion(ScenePresence agent, bool isFlying)
        {
            IEntityTransferModule transferModule = RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                transferModule.Cross(agent, isFlying);
            else
            {
                m_log.DebugFormat("[SCENE]: Unable to cross agent to neighbouring region, because there is no AgentTransferModule");
            }
        }

        public void SendOutChildAgentUpdates(AgentPosition cadu, ScenePresence presence)
        {
            INeighbourService service = RequestModuleInterface<INeighbourService>();
            if (service != null)
                service.SendChildAgentUpdate(cadu, presence.Scene.RegionInfo.RegionID);
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
            if (RegionInfo.ObjectCapacity == 0)
                RegionInfo.ObjectCapacity = objects;
        }

        #endregion

        #region SceneGraph wrapper methods

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

        public bool PresenceChildStatus(UUID avatarID)
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
        /// Do NOT use this method ever! This is only kept around so that NINJA physics is not broken
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private SceneObjectPart GetSceneObjectPart(string name)
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
            ISceneEntity entity;
            m_sceneGraph.TryGetPart(localID, out entity);
            return entity as SceneObjectPart;
        }

        /// <summary>
        /// Get a prim via its UUID
        /// </summary>
        /// <param name="fullID"></param>
        /// <returns></returns>
        public SceneObjectPart GetSceneObjectPart(UUID ObjectID)
        {
            ISceneEntity entity;
            m_sceneGraph.TryGetPart(ObjectID, out entity);
            return entity as SceneObjectPart;
        }

        /// <summary>
        /// Get a scene object group that contains the prim with the given local id
        /// </summary>
        /// <param name="localID"></param>
        /// <returns>null if no scene object group containing that prim is found</returns>
        public SceneObjectGroup GetGroupByPrim(uint localID)
        {
            SceneObjectPart part = GetSceneObjectPart(localID);
            if (part != null)
                return part.ParentGroup;
            return null;
        }

        public bool TryGetScenePresence(UUID agentID, out IScenePresence scenePresence)
        {
            scenePresence = null;
            ScenePresence sp = null;
            if (TryGetScenePresence(agentID, out sp))
            {
                scenePresence = sp;
                return true;
            }

            return false;
        }

        public bool TryGetScenePresence(UUID avatarId, out ScenePresence avatar)
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

        protected internal void jointCreate(SceneObjectPart part)
        {
            // by turning a joint proxy object physical, we cause creation of a joint in the ODE scene.
            // note that, as a special case, joints have no bodies or geoms in the physics scene, even though they are physical.

            PhysicsJointType jointType;
            if (part.IsHingeJoint())
            {
                jointType = PhysicsJointType.Hinge;
            }
            else if (part.IsBallJoint())
            {
                jointType = PhysicsJointType.Ball;
            }
            else
            {
                jointType = PhysicsJointType.Ball;
            }

            List<string> bodyNames = new List<string>();
            string RawParams = part.Description;
            string[] jointParams = RawParams.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
            string trackedBodyName = null;
            if (jointParams.Length >= 2)
            {
                for (int iBodyName = 0; iBodyName < 2; iBodyName++)
                {
                    string bodyName = jointParams[iBodyName];
                    bodyNames.Add(bodyName);
                    if (bodyName != "NULL")
                    {
                        if (trackedBodyName == null)
                        {
                            trackedBodyName = bodyName;
                        }
                    }
                }
            }

            SceneObjectPart trackedBody = GetSceneObjectPart(trackedBodyName); // FIXME: causes a sequential lookup
            Quaternion localRotation = Quaternion.Identity;
            if (trackedBody != null)
            {
                localRotation = Quaternion.Inverse(trackedBody.RotationOffset) * part.RotationOffset;
            }
            else
            {
                // error, output it below
            }

            PhysicsJoint joint;

            joint = PhysicsScene.RequestJointCreation(part.Name, jointType,
                part.AbsolutePosition,
                part.RotationOffset,
                part.Description,
                bodyNames,
                trackedBodyName,
                localRotation);

            if (trackedBody == null)
            {
                jointErrorMessage(joint, "warning: tracked body name not found! joint location will not be updated properly. joint: " + part.Name);
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
                    ((SceneObjectGroup)ent).SendGroupFullUpdate(PrimUpdateFlags.FullUpdate);
                }
            }
        }

        public void Show(string[] showParams)
        {
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

        #region Ground

        /// <summary>
        /// Gets the average height of the area +2 in both the X and Y directions from the given position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float GetNormalizedGroundHeight(float x, float y)
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

        #endregion

        #region Backup

        private Dictionary<UUID, SceneObjectGroup> m_backupTaintedPrims = new Dictionary<UUID, SceneObjectGroup>();
        private Dictionary<UUID, SceneObjectGroup> m_secondaryBackupTaintedPrims = new Dictionary<UUID, SceneObjectGroup>();
        private DateTime runSecondaryBackup = DateTime.Now;

        public void AddPrimBackupTaint(SceneObjectGroup sceneObjectGroup)
        {
            lock (m_backupTaintedPrims)
            {
                if (!m_backupTaintedPrims.ContainsKey(sceneObjectGroup.UUID))
                    m_backupTaintedPrims.Add(sceneObjectGroup.UUID, sceneObjectGroup);
            }
        }

        /// <summary>
        /// This is the new backup processor, it only deals with prims that 
        /// have been 'tainted' so that it does not waste time
        /// running through as large of a backup loop
        /// </summary>
        public void ProcessPrimBackupTaints(bool forced, bool backupAll)
        {
            HashSet<SceneObjectGroup> backupPrims = new HashSet<SceneObjectGroup>();
            //Add all
            if (backupAll)
            {
                EntityBase[] entities = Entities.GetEntities();
                foreach (EntityBase entity in entities)
                {
                    if (entity is SceneObjectGroup)
                        backupPrims.Add(entity as SceneObjectGroup);
                }
            }
            else if (forced)
            {
                lock (m_backupTaintedPrims)
                {
                    //Add all these to the backup
                    backupPrims = new HashSet<SceneObjectGroup>(m_backupTaintedPrims.Values);
                    m_backupTaintedPrims.Clear();
                    //Reset the timer
                    runSecondaryBackup = DateTime.Now.AddMinutes((m_dontPersistBefore / 10000000L));

                    if (m_secondaryBackupTaintedPrims.Count != 0)
                    {
                        //Check this set
                        foreach (SceneObjectGroup grp in m_secondaryBackupTaintedPrims.Values)
                        {
                            backupPrims.Add(grp);
                        }
                    }
                    m_secondaryBackupTaintedPrims.Clear();
                }
            }
            else
            {
                lock (m_backupTaintedPrims)
                {
                    if (m_backupTaintedPrims.Count != 0)
                    {
                        backupPrims = new HashSet<SceneObjectGroup>(m_backupTaintedPrims.Values);
                        m_backupTaintedPrims.Clear();
                    }
                }
                //The seconary backup storage is so that we do not check every time and kill checking for updates that are not ready to persist yet
                // So it runs every X minutes depending on how long the minimum persistance time is
                if (runSecondaryBackup.Ticks < DateTime.Now.Ticks)
                {
                    //Add the min persistance time to now to get the new time
                    runSecondaryBackup = DateTime.Now.AddMinutes((m_dontPersistBefore / 10000000L));
                    lock (m_secondaryBackupTaintedPrims)
                    {
                        if (m_secondaryBackupTaintedPrims.Count != 0)
                        {
                            //Check this set
                            foreach (SceneObjectGroup grp in m_secondaryBackupTaintedPrims.Values)
                            {
                                backupPrims.Add(grp);
                            }
                        }
                        m_secondaryBackupTaintedPrims.Clear();
                    }
                    //Add the min persistance time to now to get the new time
                    runSecondaryBackup = DateTime.Now.AddMinutes((m_dontPersistBefore / 10000000L));
                }
            }
            int PrimsBackedUp = 0;
            foreach (SceneObjectGroup grp in backupPrims)
            {
                //Check this prim
                bool shouldReaddToLoop;
                bool shouldReaddToLoopNow;
                if (!grp.ProcessBackup(SimulationDataService, forced, out shouldReaddToLoop, out shouldReaddToLoopNow))
                {
                    if (shouldReaddToLoop)
                    {
                        //Readd it into the seconary backup loop then as its not time for it to backup yet
                        lock (m_secondaryBackupTaintedPrims)
                            lock (m_backupTaintedPrims)
                                //Make sure its not in either so that we don't duplicate checking
                                if (!m_secondaryBackupTaintedPrims.ContainsKey(grp.UUID) &&
                                    !m_backupTaintedPrims.ContainsKey(grp.UUID))
                                    m_secondaryBackupTaintedPrims.Add(grp.UUID, grp);
                    }
                    if (shouldReaddToLoopNow)
                    {
                        //Readd it into the seconary backup loop then as its not time for it to backup yet
                        lock (m_backupTaintedPrims)
                            //Make sure its not in either so that we don't duplicate checking
                            if (!m_backupTaintedPrims.ContainsKey(grp.UUID))
                                m_backupTaintedPrims.Add(grp.UUID, grp);
                    }
                }
                else
                    PrimsBackedUp++;
            }
            if (PrimsBackedUp != 0)
                m_log.Info("[Scene]: Processed backup of " + PrimsBackedUp + " prims");
            //Now make sure that we delete any prims sitting around
            // Bit ironic that backup deals with deleting of objects too eh? 
            lock (m_needsDeleted)
            {
                if (m_needsDeleted.Count != 0)
                {
                    //Removes all objects in one call
                    SimulationDataService.RemoveObjects(m_needsDeleted);
                    m_needsDeleted.Clear();
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

                    if (ret.Value.Groups.Count > 1)
                        m_log.InfoFormat("[SCENE]: Returning {0} objects due to parcel auto return.", ret.Value.Groups.Count);
                    else
                        m_log.Info("[SCENE]: Returning 1 object due to parcel auto return.");

                    AsyncSceneObjectGroupDeleter async = RequestModuleInterface<AsyncSceneObjectGroupDeleter>();
                    if (async != null)
                    {
                        async.DeleteToInventory(
                                DeRezAction.Return, ret.Value.Groups[0].RootPart.OwnerID, ret.Value.Groups, ret.Value.Groups[0].RootPart.OwnerID,
                                true, true);
                    }
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

            //Tell the SceneManager about it
            if (OnStartupComplete != null)
                OnStartupComplete(this, data);
        }

        #endregion

        #region Module Methods

        /// <value>
        /// The module interfaces available from this scene.
        /// </value>
        protected Dictionary<Type, List<object>> ModuleInterfaces = new Dictionary<Type, List<object>>();

        /// <value>
        /// The module commanders available from this scene
        /// </value>
        protected Dictionary<string, ICommander> m_moduleCommanders = new Dictionary<string, ICommander>();

        /// <value>
        /// Registered classes that are capable of creating entities.
        /// </value>
        protected Dictionary<PCode, IEntityCreator> m_entityCreators = new Dictionary<PCode, IEntityCreator>();
        
        /// <summary>
        /// Register a module commander.
        /// </summary>
        /// <param name="commander"></param>
        public void RegisterModuleCommander(ICommander commander)
        {
            lock (m_moduleCommanders)
            {
                m_moduleCommanders.Add(commander.Name, commander);
            }
        }

        /// <summary>
        /// Unregister a module commander and all its commands
        /// </summary>
        /// <param name="name"></param>
        public void UnregisterModuleCommander(string name)
        {
            lock (m_moduleCommanders)
            {
                ICommander commander;
                if (m_moduleCommanders.TryGetValue(name, out commander))
                    m_moduleCommanders.Remove(name);
            }
        }

        /// <summary>
        /// Get a module commander
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The module commander, null if no module commander with that name was found</returns>
        public ICommander GetCommander(string name)
        {
            lock (m_moduleCommanders)
            {
                if (m_moduleCommanders.ContainsKey(name))
                    return m_moduleCommanders[name];
            }

            return null;
        }

        public Dictionary<string, ICommander> GetCommanders()
        {
            return m_moduleCommanders;
        }

        /// <summary>
        /// Register an interface to a region module.  This allows module methods to be called directly as
        /// well as via events.  If there is already a module registered for this interface, it is not replaced
        /// (is this the best behaviour?)
        /// </summary>
        /// <param name="mod"></param>
        public void RegisterModuleInterface<M>(M mod)
        {
            //            m_log.DebugFormat("[SCENE BASE]: Registering interface {0}", typeof(M));

            List<Object> l = null;
            if (!ModuleInterfaces.TryGetValue(typeof(M), out l))
            {
                l = new List<Object>();
                ModuleInterfaces.Add(typeof(M), l);
            }

            if (l.Count > 0)
                l.Clear();

            l.Add(mod);

            if (mod is IEntityCreator)
            {
                IEntityCreator entityCreator = (IEntityCreator)mod;
                foreach (PCode pcode in entityCreator.CreationCapabilities)
                {
                    m_entityCreators[pcode] = entityCreator;
                }
            }
        }

        public void AddModuleInterfaces(Dictionary<Type, object> dictionary)
        {
            foreach (KeyValuePair<Type, object> kvp in dictionary)
            {
                List<Object> l = null;
                if (!ModuleInterfaces.TryGetValue(kvp.Key, out l))
                {
                    l = new List<Object>();
                    ModuleInterfaces.Add(kvp.Key, l);
                }

                if (l.Count > 0)
                    l.Clear();

                l.Add(kvp.Value);
            }
        }

        public void UnregisterModuleInterface<M>(M mod)
        {
            List<Object> l;
            if (ModuleInterfaces.TryGetValue(typeof(M), out l))
            {
                if (l.Remove(mod))
                {
                    if (mod is IEntityCreator)
                    {
                        IEntityCreator entityCreator = (IEntityCreator)mod;
                        foreach (PCode pcode in entityCreator.CreationCapabilities)
                        {
                            m_entityCreators[pcode] = null;
                        }
                    }
                }
            }
        }

        public void RegisterInterface<M>(M mod)
        {
            RegisterModuleInterface<M>(mod);
        }

        public void StackModuleInterface<M>(M mod)
        {
            List<Object> l;
            if (ModuleInterfaces.ContainsKey(typeof(M)))
                l = ModuleInterfaces[typeof(M)];
            else
                l = new List<Object>();

            if (l.Contains(mod))
                return;

            l.Add(mod);

            if (mod is IEntityCreator)
            {
                IEntityCreator entityCreator = (IEntityCreator)mod;
                foreach (PCode pcode in entityCreator.CreationCapabilities)
                {
                    m_entityCreators[pcode] = entityCreator;
                }
            }

            ModuleInterfaces[typeof(M)] = l;
        }

        /// <summary>
        /// For the given interface, retrieve the region module which implements it.
        /// </summary>
        /// <returns>null if there is no registered module implementing that interface</returns>
        public T RequestModuleInterface<T>()
        {
            if (ModuleInterfaces.ContainsKey(typeof(T)) &&
                    (ModuleInterfaces[typeof(T)].Count > 0))
                return (T)ModuleInterfaces[typeof(T)][0];
            else
                return default(T);
        }

        public T Get<T>()
        {
            if (ModuleInterfaces.ContainsKey(typeof(T)) &&
                    (ModuleInterfaces[typeof(T)].Count > 0))
                return (T)ModuleInterfaces[typeof(T)][0];
            else
                return default(T);
        }

        public bool TryGet<T>(out T iface)
        {
            iface = default(T);
            if (ModuleInterfaces.ContainsKey(typeof(T)) &&
                    (ModuleInterfaces[typeof(T)].Count > 0))
            {
                iface = (T)ModuleInterfaces[typeof(T)][0];
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// For the given interface, retrieve an array of region modules that implement it.
        /// </summary>
        /// <returns>an empty array if there are no registered modules implementing that interface</returns>
        public T[] RequestModuleInterfaces<T>()
        {
            if (ModuleInterfaces.ContainsKey(typeof(T)))
            {
                List<T> ret = new List<T>();

                foreach (Object o in ModuleInterfaces[typeof(T)])
                    ret.Add((T)o);
                return ret.ToArray();
            }
            else
            {
                return new T[] { default(T) };
            }
        }

        /// <summary>
        /// We don't support this in the Scene...
        /// </summary>
        /// <returns></returns>
        public Dictionary<Type, object> GetInterfaces()
        {
            return new Dictionary<Type, object>();
        }

        #endregion

        #region Console Commander

        public void AddCommand(object mod, string command, string shorthelp, string longhelp, CommandDelegate callback)
        {
            AddCommand(mod, command, shorthelp, longhelp, string.Empty, callback);
        }

        /// <summary>
        /// Call this from a region module to add a command to the OpenSim console.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="command"></param>
        /// <param name="shorthelp"></param>
        /// <param name="longhelp"></param>
        /// <param name="descriptivehelp"></param>
        /// <param name="callback"></param>
        public void AddCommand(
            object mod, string command, string shorthelp, string longhelp, string descriptivehelp, CommandDelegate callback)
        {
            if (MainConsole.Instance == null)
                return;

            string modulename = String.Empty;
            bool shared = false;

            if (mod != null)
            {
                if (mod is IRegionModuleBase)
                {
                    IRegionModuleBase module = (IRegionModuleBase)mod;
                    modulename = module.Name;
                    shared = mod is ISharedRegionModule;
                }
                else throw new Exception("AddCommand module parameter must be IRegionModule or IRegionModuleBase");
            }

            MainConsole.Instance.Commands.AddCommand(
                modulename, shared, command, shorthelp, longhelp, descriptivehelp, callback);
        }

        #endregion

        #region Events

        public event restart OnRestart;
        public event startupComplete OnStartupComplete;

        #endregion
    }
}