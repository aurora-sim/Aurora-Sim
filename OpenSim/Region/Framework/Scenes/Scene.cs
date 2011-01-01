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
    public partial class Scene : RegistryCore, IScene
    {
        #region Fields

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

        public SceneManager SceneManager
        {
            get { return m_sceneManager; }
        }

        protected ScenePermissions m_permissions;
        /// <summary>
        /// Controls permissions for the Scene
        /// </summary>
        public ScenePermissions Permissions
        {
            get { return m_permissions; }
        }

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
        public uint Frame
        {
            get { return m_frame; }
        }
        protected float m_updatetimespan = 0.069f;
        protected float m_physicstimespan = 0.049f;
        protected DateTime m_lastupdate = DateTime.UtcNow;

        private int m_update_physics = 1; //Trigger the physics update
        private int m_update_presences = 5; // Send prim updates for clients
        private int m_update_events = 1; //Trigger the OnFrame event and tell any modules about the new frame
        private int m_update_coarse_locations = 30; //Trigger the sending of coarse location updates (minimap updates)

        private string m_defaultScriptEngine;
        private static volatile bool shuttingdown = false;

        private object m_cleaningAttachments = new object();

        private double m_rootReprioritizationDistance = 10.0;
        private double m_childReprioritizationDistance = 20.0;

        private bool EnableFakeRaycasting = false;
        private bool m_UseSelectionParticles = true;
        public bool CheckForObjectCulling = false;
        public bool[,] DirectionsToBlockChildAgents;
        private string m_DefaultObjectName = "Primitive";
        public bool RunScriptsInAttachments = false;
        public bool m_usePreJump = true;
        public bool m_useSplatAnimation = true;
        public float MaxLowValue = -1000;
        private Dictionary<UUID, AgentData> m_incomingChildAgentData = new Dictionary<UUID, AgentData>();

        #endregion

        #region Properties

        public double RootReprioritizationDistance { get { return m_rootReprioritizationDistance; } }
        public double ChildReprioritizationDistance { get { return m_childReprioritizationDistance; } }
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

        public IConfigSource Config
        {
            get { return m_config; }
            set { m_config = value; }
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

        public Scene(RegionInfo regInfo)
        {
            m_regInfo = regInfo;

            //Register to regInfo events
            m_regInfo.OnRegionUp += new RegionInfo.TriggerOnRegionUp(regInfo_OnRegionUp);
        }

        public Scene(RegionInfo regInfo, AgentCircuitManager authen, SceneManager manager) : this(regInfo)
        {
            //THIS NEEDS RESET TO FIX RESTARTS
            shuttingdown = false;

            m_sceneManager = manager;

            m_config = manager.ConfigSource;
            m_authenticateHandler = authen;


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
                    UseOneHeartbeat = aurorastartupConfig.GetBoolean("RunWithMultipleHeartbeats", true);
                    RunScriptsInAttachments = aurorastartupConfig.GetBoolean("AllowRunningOfScriptsInAttachments", false);
                    m_UseSelectionParticles = aurorastartupConfig.GetBoolean("UseSelectionParticles", true);
                    EnableFakeRaycasting = aurorastartupConfig.GetBoolean("EnableFakeRaycasting", false);
                    MaxLowValue = aurorastartupConfig.GetFloat("MaxLowValue", -1000);
                    Util.VariableRegionSight = aurorastartupConfig.GetBoolean("UseVariableRegionSightDistance", Util.VariableRegionSight);
                    m_DefaultObjectName = aurorastartupConfig.GetString("DefaultObjectName", m_DefaultObjectName);
                    CheckForObjectCulling = aurorastartupConfig.GetBoolean("CheckForObjectCulling", CheckForObjectCulling);
                    //Region specific is still honored here, the RegionInfo checks for it
                    RegionInfo.ObjectCapacity = aurorastartupConfig.GetInt("ObjectCapacity", 80000);
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
                    m_usePreJump = animationConfig.GetBoolean("enableprejump", m_usePreJump);
                    m_useSplatAnimation = animationConfig.GetBoolean("enableSplatAnimation", m_useSplatAnimation);
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
                                agent.DropOldNeighbors(old);
                                //Now add the agent to the reigon that is coming up
                                IEntityTransferModule transferModule = RequestModuleInterface<IEntityTransferModule>();
                                if (transferModule != null)
                                    transferModule.EnableChildAgent(agent, otherRegion);
                            }
                        });
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

        // Alias IncomingHelloNeighbor OtherRegionUp, for now
        public void IncomingHelloNeighbor(RegionInfo neighbor)
        {
            OtherRegionUp(new GridRegion(neighbor));
        }

        public void IncomingClosingNeighbor(RegionInfo neighbor)
        {
            //OtherRegionUp(new GridRegion(neighbor));
            //return new GridRegion(RegionInfo);
        }

        /// <summary>
        /// This is the method that shuts down the scene.
        /// </summary>
        public void Close()
        {
            m_log.InfoFormat("[Scene]: Closing down the single simulator: {0}", RegionInfo.RegionName);

            // Kick all ROOT agents with the message, 'The simulator is going down'
            ForEachScenePresence(delegate(ScenePresence avatar)
            {
                if (!avatar.IsChildAgent)
                    avatar.ControllingClient.Kick("The simulator is going down.");

                IEventQueueService eq = RequestModuleInterface<IEventQueueService>();
                if (eq != null)
                {
                    eq.DisableSimulator(RegionInfo.RegionHandle, avatar.UUID, RegionInfo.RegionHandle);
                }
                else
                    avatar.ControllingClient.SendShutdownConnectionNotice();
            });

            // Wait here, or the kick messages won't actually get to the agents before the scene terminates.
            Thread.Sleep(500);

            // Stop all client threads.
            ForEachScenePresence(delegate(ScenePresence avatar) { avatar.ControllingClient.Close(); });

            if (tracker != null)
            {
                tracker.OnNeedToAddThread -= NeedsNewThread;
                tracker.Close();
                tracker = null;
            }

            if (PhysicsScene != null)
            {
                PhysicsScene.Dispose();
            }

            //Tell the neighbors that this region is now down
            INeighborService service = RequestModuleInterface<INeighborService>();
            if (service != null)
                service.InformNeighborsThatRegionIsDown(RegionInfo);

            // Stop updating the scene objects and agents.
            shuttingdown = true;

            m_sceneGraph.Close();

            //Trigger the last event
            EventManager.TriggerShutdown();
        }

        #region Tracker

        public AuroraThreadTracker tracker = null;
        private bool UseOneHeartbeat = true;
        /// <summary>
        /// Start the timer which triggers regular scene updates
        /// </summary>
        public void StartTimer()
        {
            if (tracker == null)
                tracker = new AuroraThreadTracker();
            if (UseOneHeartbeat)
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
                ITotalFrameTimeMonitor totalFrameMonitor = (ITotalFrameTimeMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Total Frame Time");
                ISetMonitor lastFrameMonitor = (ISetMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Last Completed Frame At");
                ITimeMonitor otherFrameMonitor = (ITimeMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Other Frame Time");
                ITimeMonitor sleepFrameMonitor = (ITimeMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Sleep Frame Time");
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
                            m_scene.SceneGraph.GetCoarseLocations(out coarseLocations, out avatarUUIDs, 60);
                            // Send coarse locations to clients 
                            foreach(ScenePresence presence in m_scene.ScenePresences)
                            {
                                presence.SendCoarseLocations(coarseLocations, avatarUUIDs);
                            }
                        }

                        if (m_scene.m_frame % m_scene.m_update_presences == 0)
                            m_scene.m_sceneGraph.UpdatePresences();

                        if (m_scene.m_frame % m_scene.m_update_events == 0)
                            m_scene.UpdateEvents();

                        // Check if any objects have reached their targets
                        m_scene.CheckAtTargets();

                        int MonitorOtherFrameTime = Util.EnvironmentTickCountSubtract(OtherFrameTime);

                        maintc = Util.EnvironmentTickCountSubtract(maintc);
                        maintc = ((int)(m_scene.m_updatetimespan * 1000) - maintc) / Scene.m_timeToSlowTheHeartbeat;

                        int MonitorLastCompletedFrame = Util.EnvironmentTickCount();
                        
                        //Now fix the stats
                        simFrameMonitor.AddFPS(1);
                        lastFrameMonitor.SetValue(MonitorLastCompletedFrame);
                        otherFrameMonitor.AddTime(MonitorOtherFrameTime);

                        CheckExit();
                    }
                    catch (Exception e)
                    {
                        if (e.Message != "Closing")
                            m_log.Error("[REGION]: Failed with exception " + e.ToString() + " On Region: " + m_scene.RegionInfo.RegionName);
                        break;
                    }

                    int MonitorEndFrameTime = Util.EnvironmentTickCountSubtract(BeginningFrameTime) + maintc;

                    if (maintc > 0 && shouldSleep)
                        Thread.Sleep(maintc);

                    int MonitorSleepFrameTime = maintc;
                    if(shouldSleep)
                        sleepFrameMonitor.AddTime(MonitorSleepFrameTime);
                    
                    totalFrameMonitor.AddFrameTime(MonitorEndFrameTime);
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
                ITotalFrameTimeMonitor totalFrameMonitor = (ITotalFrameTimeMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Total Frame Time");
                ITimeMonitor physicsSyncFrameMonitor = (ITimeMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Physics Sync Frame Time");
                ITimeMonitor physicsFrameTimeMonitor = (ITimeMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Physics Update Frame Time");
                ITimeMonitor sleepFrameMonitor = (ITimeMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Sleep Frame Time");
                int maintc;

                while (!ShouldExit)
                {
                    TimeSpan SinceLastFrame = DateTime.UtcNow - m_scene.m_lastupdate;

                    maintc = Util.EnvironmentTickCount();
                    int BeginningFrameTime = maintc;
                    
                    try
                    {
                        int PhysicsSyncTime = Util.EnvironmentTickCount();

                        if ((m_scene.m_frame % m_scene.m_update_physics == 0) && !m_scene.RegionInfo.RegionSettings.DisablePhysics)
                            m_scene.m_sceneGraph.UpdatePreparePhysics();

                        int MonitorPhysicsSyncTime = Util.EnvironmentTickCountSubtract(PhysicsSyncTime);

                        int PhysicsUpdateTime = Util.EnvironmentTickCount();

                        if (m_scene.m_frame % m_scene.m_update_physics == 0)
                        {
                            if (!m_scene.RegionInfo.RegionSettings.DisablePhysics)
                                m_scene.m_sceneGraph.UpdatePhysics(Math.Max(SinceLastFrame.TotalSeconds, m_scene.m_physicstimespan));
                        }

                        int MonitorPhysicsUpdateTime = Util.EnvironmentTickCountSubtract(PhysicsUpdateTime) + MonitorPhysicsSyncTime;

                        physicsFrameTimeMonitor.AddTime(MonitorPhysicsUpdateTime);
                        physicsFrameMonitor.AddFPS(1);
                        physicsSyncFrameMonitor.AddTime(MonitorPhysicsSyncTime);

                        IPhysicsMonitor monitor = m_scene.RequestModuleInterface<IPhysicsMonitor>();
                        if(monitor != null)
                            monitor.AddPhysicsStats(m_scene.RegionInfo.RegionID, m_scene.m_sceneGraph.PhysicsScene);
                        
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
                    maintc = (int)((m_scene.m_physicstimespan * 1000) - maintc) / Scene.m_timeToSlowThePhysHeartbeat;

                    int MonitorEndFrameTime = Util.EnvironmentTickCountSubtract(BeginningFrameTime) + maintc;

                    if (maintc > 0 && shouldSleep)
                        Thread.Sleep(maintc);

                    int MonitorSleepFrameTime = maintc;
                    if(shouldSleep)
                        sleepFrameMonitor.AddTime(MonitorSleepFrameTime);
                    
                    totalFrameMonitor.AddFrameTime(MonitorEndFrameTime);
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
                ITimeMonitor sleepFrameMonitor = (ITimeMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "Sleep Frame Time");
                int maintc;

                while (!ShouldExit)
                {
                    maintc = Util.EnvironmentTickCount();
                    //Update all of the threads without sleeping, then sleep down at the bottom
                    updateH.Update(false);
                    physH.Update(false);
                    maintc = Util.EnvironmentTickCountSubtract(maintc);
                    maintc = (int)(0.086 * 1000) - maintc;

                    if (maintc > 0)
                        Thread.Sleep(maintc);
                    int MonitorSleepFrameTime = maintc;
                    sleepFrameMonitor.AddTime(MonitorSleepFrameTime);
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
            Dictionary<UUID, SceneObjectGroup>.ValueCollection objs;
            lock (m_groupsWithTargets)
                objs = m_groupsWithTargets.Values;
            foreach (SceneObjectGroup entry in objs)
                entry.checkAtTargets();
        }

        /// <summary>
        /// Sends out the OnFrame event to the modules
        /// </summary>
        private void UpdateEvents()
        {
            m_eventManager.TriggerOnFrame();
        }

        #endregion

        #region Primitives Methods

        /// <summary>
        /// Synchronously delete the given object from the scene.
        /// </summary>
        /// <param name="group">Object Id</param>
        /// <param name="silent">Suppress broadcasting changes to other clients.</param>
        public bool DeleteSceneObject(SceneObjectGroup group, bool DeleteScripts)
        {
            //            m_log.DebugFormat("[SCENE]: Deleting scene object {0} {1}", group.Name, group.UUID);

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
                return true;
            }
            return false;
            //m_log.DebugFormat("[SCENE]: Exit DeleteSceneObject() for {0} {1}", group.Name, group.UUID);
        }

        /// <summary>
        /// Unlink the given object from the scene.  Unlike delete, this just removes the record of the object - the
        /// object itself is not destroyed.
        /// </summary>
        /// <param name="so">The scene object.</param>
        /// <param name="softDelete">If true, only deletes from scene, but keeps the object in the database.</param>
        /// <returns>true if the object was in the scene, false if it was not</returns>
        protected bool UnlinkSceneObject(SceneObjectGroup so, bool softDelete)
        {
            if (m_sceneGraph.DeleteEntity(so))
            {
                if (!softDelete)
                {
                    IBackupModule backup = RequestModuleInterface<IBackupModule>();
                    if (backup != null)
                        backup.DeleteFromStorage(so.UUID);

                    // We need to keep track of this state in case this group is still queued for backup.
                    so.IsDeleted = true;
                    //Clear the update schedule HERE so that IsDeleted will not have to fire as well
                    lock (so.ChildrenListLock)
                    {
                        foreach (SceneObjectPart part in so.ChildrenList)
                        {
                            //Make sure it isn't going to be updated again
                            part.ClearUpdateSchedule();
                            //If it is the root part, kill the object in the client
                            if (part == so.RootPart)
                            {
                                ForEachScenePresence(delegate(ScenePresence avatar)
                                {
                                    avatar.ControllingClient.SendKillObject(RegionInfo.RegionHandle, new ISceneEntity[] { part });
                                });
                            }
                        }
                    }
                }
                EventManager.TriggerParcelPrimCountTainted();
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
                m_log.WarnFormat("[SCENE]: Deleting dropped attachment {0} of user {1}", grp.UUID, grp.OwnerID);
                DeleteSceneObject(grp, true);
            }
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

        #region Subscribing and Unsubscribing to client events

        /// <summary>
        /// Register for events from the client
        /// </summary>
        /// <param name="client">The IClientAPI of the connected client</param>
        public virtual void SubscribeToClientEvents(IClientAPI client)
        {
            SubscribeToClientPrimEvents(client);
            SubscribeToClientPrimRezEvents(client);
            SubscribeToClientInventoryEvents(client);
            SubscribeToClientGridEvents(client);
            SubscribeToClientNetworkEvents(client);
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
            client.OnObjectPermissions += m_sceneGraph.HandleObjectPermissionsUpdate;
            client.OnGrabObject += ProcessObjectGrab;
            client.OnGrabUpdate += ProcessObjectGrabUpdate;
            client.OnDeGrabObject += ProcessObjectDeGrab;
            client.OnUndo += m_sceneGraph.HandleUndo;
            client.OnRedo += m_sceneGraph.HandleRedo;
            client.OnObjectDescription += m_sceneGraph.PrimDescription;
            client.OnObjectDrop += m_sceneGraph.DropObject;
            client.OnObjectIncludeInSearch += m_sceneGraph.MakeObjectSearchable;
            client.OnObjectOwner += ObjectOwner;
            client.OnObjectGroupRequest += m_sceneGraph.HandleObjectGroupUpdate;
        }

        public virtual void SubscribeToClientPrimRezEvents(IClientAPI client)
        {
            client.OnAddPrim += m_sceneGraph.AddNewPrim;
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

        public virtual void SubscribeToClientGridEvents(IClientAPI client)
        {
            client.OnNameFromUUIDRequest += HandleUUIDNameRequest;
            client.OnMoneyTransferRequest += ProcessMoneyTransferRequest;
            client.OnAvatarPickerRequest += ProcessAvatarPickerRequest;
            client.OnSetStartLocationRequest += SetHomeRezPoint;
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
            UnSubscribeToClientPrimEvents(client);
            UnSubscribeToClientPrimRezEvents(client);
            UnSubscribeToClientInventoryEvents(client);
            UnSubscribeToClientGridEvents(client);
            UnSubscribeToClientNetworkEvents(client);
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
            client.OnObjectPermissions -= m_sceneGraph.HandleObjectPermissionsUpdate;
            client.OnGrabObject -= ProcessObjectGrab;
            client.OnDeGrabObject -= ProcessObjectDeGrab;
            client.OnUndo -= m_sceneGraph.HandleUndo;
            client.OnRedo -= m_sceneGraph.HandleRedo;
            client.OnObjectDescription -= m_sceneGraph.PrimDescription;
            client.OnObjectDrop -= m_sceneGraph.DropObject;
            client.OnObjectIncludeInSearch -= m_sceneGraph.MakeObjectSearchable;
            client.OnObjectOwner -= ObjectOwner;
            client.OnObjectGroupRequest -= m_sceneGraph.HandleObjectGroupUpdate;
        }

        public virtual void UnSubscribeToClientPrimRezEvents(IClientAPI client)
        {
            client.OnAddPrim -= m_sceneGraph.AddNewPrim;
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

        public virtual void UnSubscribeToClientGridEvents(IClientAPI client)
        {
            client.OnNameFromUUIDRequest -= HandleUUIDNameRequest;
            client.OnMoneyTransferRequest -= ProcessMoneyTransferRequest;
            client.OnAvatarPickerRequest -= ProcessAvatarPickerRequest;
            client.OnSetStartLocationRequest -= SetHomeRezPoint;
        }

        public virtual void UnSubscribeToClientNetworkEvents(IClientAPI client)
        {
            client.OnViewerEffect -= ProcessViewerEffect;
        }

        #endregion

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
            AgentCircuitData aCircuit = m_authenticateHandler.GetAgentCircuitData(client.CircuitCode);

            if (aCircuit == null)
            {
                m_log.ErrorFormat("[APPEARANCE] Client did not supply a circuit. Non-Linden? Creating default appearance.");
                appearance = new AvatarAppearance(client.AgentId);
            }

            appearance = aCircuit.Appearance;
            if (appearance == null)
            {
                m_log.ErrorFormat("[APPEARANCE]: Appearance not found in {0}, returning default", RegionInfo.RegionName);
                appearance = new AvatarAppearance(client.AgentId);
            }

            ScenePresence avatar = m_sceneGraph.CreateAndAddChildScenePresence(client, appearance);
            if (m_incomingChildAgentData.ContainsKey(avatar.UUID))
            {
                avatar.ChildAgentDataUpdate(m_incomingChildAgentData[avatar.UUID]);
                m_incomingChildAgentData.Remove(avatar.UUID);
            }

            m_eventManager.TriggerOnNewPresence(avatar);

            return avatar;
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
                        INeighborService service = RequestModuleInterface<INeighborService>();
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

            ScenePresence sp = GetScenePresence(agent.AgentID);

            if (sp != null && !sp.IsChildAgent)
            {
                // We have a zombie from a crashed session. 
                // Or the same user is trying to be root twice here, won't work.
                // Kill it.
                m_log.InfoFormat("[Scene]: Zombie scene presence detected for {0} in {1}", agent.AgentID, RegionInfo.RegionName);
                sp.ControllingClient.Close();
                sp = null;
            }

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
                if (module != null)
                    module.AddCapsHandler(agent.AgentID);
            }
            else
            {
                if (sp.IsChildAgent)
                {
                    m_log.DebugFormat(
                        "[Scene]: Adjusting known seeds for existing agent {0} in {1}",
                        agent.AgentID, RegionInfo.RegionName);

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
                if (agent.startpos.Z < LandChannel.GetNormalizedGroundHeight(agent.startpos.X, agent.startpos.Y))
                {
                    agent.startpos.Z = LandChannel.GetNormalizedGroundHeight(agent.startpos.X, agent.startpos.Y) + 1;
                }
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

            //Can we teleport into this region?
            // Note: this takes care of practically every check possible, banned from estate, banned from parcels, parcel landing locations, etc
            if (!Permissions.CanTeleport(agent.AgentID, agent.startpos, agent, out agent.startpos, out reason))
                return false;

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
            // That calls AddNewClient, which finally creates the ScenePresence and then this gets set up

            ScenePresence SP = GetScenePresence(cAgentData.AgentID);
            if (SP != null)
                SP.ChildAgentDataUpdate(cAgentData);
            else
                m_incomingChildAgentData[cAgentData.AgentID] = cAgentData;
            return true;
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

        public void SendOutChildAgentUpdates(AgentPosition cadu, ScenePresence presence)
        {
            INeighborService service = RequestModuleInterface<INeighborService>();
            if (service != null)
                service.SendChildAgentUpdate(cadu, presence.Scene.RegionInfo.RegionID);
        }

        #endregion

        #region SceneGraph wrapper methods

        public int GetRootAgentCount()
        {
            return m_sceneGraph.GetRootAgentCount();
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

        #endregion

        #region Startup Complete

        private List<string> StartupCallbacks = new List<string>();
        private List<string> StartupData = new List<string>();

        /// <summary>
        /// Add a module to the startup queue
        /// </summary>
        /// <param name="name"></param>
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

        /// <summary>
        /// This module finished startup and is giving a list of data about its startup
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
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

        /// <summary>
        /// Startup is complete, trigger the modules and allow logins
        /// </summary>
        /// <param name="data"></param>
        public void StartupComplete(List<string> data)
        {
            // In 99.9% of cases it is a bad idea to manually force garbage collection. However,
            // this is a rare case where we know we have just went through a long cycle of heap
            // allocations, and there is no more work to be done until someone logs in
            GC.Collect();

            m_log.Info("[Region]: Startup Complete in region " + RegionInfo.RegionName);
            IConfig startupConfig = m_config.Configs["Startup"];
            if (startupConfig == null || !startupConfig.GetBoolean("StartDisabled", false))
            {
                m_log.DebugFormat("[Region]: Enabling logins for {0}", RegionInfo.RegionName);
                LoginsDisabled = false;
            }

            //Tell the SceneManager about it
            m_sceneManager.HandleStartupComplete(this, data);
        }

        #endregion
    }
}