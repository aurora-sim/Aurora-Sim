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
using System.Threading;
using System.Reflection;
using log4net;
using Nini.Config;
using Aurora.DataManager;
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Region.Framework.Scenes
{
    public class Scene : RegistryCore, IScene
    {
        #region Fields

        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private List<ISceneEntity> m_PhysicsReturns = new List<ISceneEntity> ();
        public List<ISceneEntity> PhysicsReturns
        {
            get { return m_PhysicsReturns; }
        }

        /// <value>
        /// The scene graph for this scene
        /// </value>
        private SceneGraph m_sceneGraph;

        protected readonly ClientManager m_clientManager = new ClientManager();

        protected RegionInfo m_regInfo;
        protected IClientNetworkServer m_clientServer;

        protected ThreadMonitor monitor = new ThreadMonitor();
            
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

        protected IConfigSource m_config;

        protected AgentCircuitManager m_authenticateHandler;

        // Central Update Loop

        protected uint m_frame;
        /// <summary>
        /// The current frame #
        /// </summary>
        public uint Frame
        {
            get { return m_frame; }
        }

        private float m_basesimfps = 45f;
        private float m_basesimphysfps = 45f;

        protected float m_updatetimespan = 0.022f;
        protected float m_physicstimespan = 0.022f;
        protected DateTime m_lastphysupdate = DateTime.UtcNow;

        private int m_update_physics = 1; //Trigger the physics update
        private int m_update_entities = 5; // Send prim updates for clients
        private int m_update_events = 1; //Trigger the OnFrame event and tell any modules about the new frame
        private int m_update_coarse_locations = 30; //Trigger the sending of coarse location updates (minimap updates)

        private volatile bool shuttingdown = false;

        public bool ShouldRunHeartbeat = true;

        #endregion

        #region Properties

        public PhysicsScene PhysicsScene
        {
            get { return m_sceneGraph.PhysicsScene; }
            set { m_sceneGraph.PhysicsScene = value; }
        }

        public float BaseSimFPS
        {
            get { return m_basesimfps; }
        }

        public float BaseSimPhysFPS
        {
            get { return m_basesimphysfps; }
        }

        public AgentCircuitManager AuthenticateHandler
        {
            get { return m_authenticateHandler; }
        }

        public ISimulationDataStore SimulationDataService
        {
            get { return m_sceneManager.SimulationDataService; }
        }

        public bool ShuttingDown
        {
            get { return shuttingdown; }
        }

        public ISceneGraph SceneGraph
        {
            get { return m_sceneGraph; }
        }

        public RegionInfo RegionInfo
        {
            get { return m_regInfo; }
        }

        // This gets locked so things stay thread safe.
        public object SyncRoot
        {
            get { return m_sceneGraph.m_syncRoot; }
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

        public float TimeDilation
        {
            get { return m_sceneGraph.PhysicsScene.TimeDilation; }
            set { m_sceneGraph.PhysicsScene.TimeDilation = value; }
        }

        public override string ToString()
        {
            return "Name: " + m_regInfo.RegionName + ", Loc: " +
                m_regInfo.RegionLocX / Constants.RegionSize + "," +
                m_regInfo.RegionLocY / Constants.RegionSize + ", Size: " +
                m_regInfo.RegionSizeX + "," +
                m_regInfo.RegionSizeY + 
                ", Port: " + m_regInfo.InternalEndPoint.Port;
        }

        #region Services

        public IAssetService AssetService
        {
            get
            {
                return RequestModuleInterface<IAssetService>();
            }
        }

        public IAuthenticationService AuthenticationService
        {
            get
            {
                return RequestModuleInterface<IAuthenticationService>();
            }
        }

        public IAvatarService AvatarService
        {
            get
            {
                return RequestModuleInterface<IAvatarService>();
            }
        }

        public IGridService GridService
        {
            get
            {
                return RequestModuleInterface<IGridService>();
            }
        }

        public IInventoryService InventoryService
        {
            get
            {
                return RequestModuleInterface<IInventoryService>();
            }
        }

        public ISimulationService SimulationService
        {
            get
            {
                return RequestModuleInterface<ISimulationService>();
            }
        }

        public IUserAccountService UserAccountService
        {
            get
            {
                return RequestModuleInterface<IUserAccountService>();
            }
        }

        #endregion

        #endregion

        #region Constructors

        public void Initialize (RegionInfo regionInfo)
        {
            m_regInfo = regionInfo;
        }

        public void Initialize (RegionInfo regionInfo, AgentCircuitManager authen, IClientNetworkServer clientServer)
        {
            Initialize (regionInfo);

            //Set up the clientServer
            m_clientServer = clientServer;
            clientServer.AddScene (this);

            m_sceneManager = RequestModuleInterface<SceneManager>();

            m_config = m_sceneManager.ConfigSource;
            m_authenticateHandler = authen;

            m_AuroraEventManager = new AuroraEventManager();
            m_eventManager = new EventManager();
            m_permissions = new ScenePermissions(this);

            // Load region settings
            m_regInfo.RegionSettings = m_sceneManager.SimulationDataService.LoadRegionSettings(m_regInfo.RegionID);

            m_sceneGraph = new SceneGraph(this, m_regInfo);

            #region Region Config

            IConfig aurorastartupConfig = m_config.Configs["AuroraStartup"];
            if (aurorastartupConfig != null)
            {
                //Region specific is still honored here, the RegionInfo checks for it
                RegionInfo.ObjectCapacity = aurorastartupConfig.GetInt("ObjectCapacity", 80000);
            }

            IConfig packetConfig = m_config.Configs["PacketPool"];
            if (packetConfig != null)
            {
                PacketPool.Instance.RecyclePackets = packetConfig.GetBoolean("RecyclePackets", true);
                PacketPool.Instance.RecycleDataBlocks = packetConfig.GetBoolean("RecycleDataBlocks", true);
            }

            #endregion Region Config

            m_basesimfps = 45f;
            m_basesimphysfps = 45f;

            m_basesimphysfps = Config.Configs["Physics"].GetFloat("BasePhysicsFPS", 45f);
            if (m_basesimphysfps > 45f)
                m_basesimphysfps = 45f;

            m_basesimfps = Config.Configs["Protection"].GetFloat("BaseRateFramesPerSecond", 45f);
            if (m_basesimfps > 45f)
                m_basesimfps = 45f;

            if (m_basesimphysfps > m_basesimfps)
                m_basesimphysfps = m_basesimfps;

            m_updatetimespan = 1 / m_basesimfps;
            m_physicstimespan = 1 / m_basesimphysfps;

            #region Startup Complete config

            EventManager.OnAddToStartupQueue += AddToStartupQueue;
            EventManager.OnModuleFinishedStartup += FinishedStartup;
            EventManager.OnStartupComplete += StartupComplete;

            AddToStartupQueue("Startup");

            #endregion
        }

        #endregion Constructors

        #region Close

        /// <summary>
        /// This is the method that shuts down the scene.
        /// </summary>
        public void Close()
        {
            m_log.InfoFormat("[Scene]: Closing down the single simulator: {0}", RegionInfo.RegionName);

            // Kick all ROOT agents with the message, 'The simulator is going down'
            ForEachScenePresence (delegate (IScenePresence avatar)
            {
                if (!avatar.IsChildAgent)
                    avatar.ControllingClient.Kick("The simulator is going down.");
            });

            IEntityTransferModule transferModule = RequestModuleInterface<IEntityTransferModule> ();
            if (transferModule != null)
            {
                foreach (IScenePresence avatar in GetScenePresences ())
                {
                    transferModule.IncomingCloseAgent (this, avatar.UUID);
                }
            }
            //Stop the heartbeat
            monitor.Stop();

            if (m_sceneGraph.PhysicsScene != null)
                m_sceneGraph.PhysicsScene.Dispose ();

            //Tell the neighbors that this region is now down
            INeighborService service = RequestModuleInterface<INeighborService>();
            if (service != null)
                service.InformNeighborsThatRegionIsDown(RegionInfo);

            // Stop updating the scene objects and agents.
            shuttingdown = true;

            m_sceneGraph.Close();
        }

        #endregion

        #region Tracker

        /// <summary>
        /// Start the heartbeat which triggers regular scene updates
        /// </summary>
        public void StartHeartbeat()
        {
            if (!ShouldRunHeartbeat) //Allow for the heartbeat to not be used
                return;

            m_clientServer.Start ();

            //Give it the heartbeat delegate with an infinite timeout
            monitor.StartTrackingThread(0, Update);
            //Then start the thread for it with an infinite loop time and no 
            //  sleep overall as the Update delete does it on it's own
            monitor.StartMonitor(0, 0);
        }

        #endregion

        #region Scene Heartbeat Methods

        private bool Update()
        {
            if (!ShouldRunHeartbeat) //If we arn't supposed to be running, kill ourselves
                return false;

            ISimFrameMonitor simFrameMonitor = (ISimFrameMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), "SimFrameStats");
            ITotalFrameTimeMonitor totalFrameMonitor = (ITotalFrameTimeMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), "Total Frame Time");
            ISetMonitor lastFrameMonitor = (ISetMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), "Last Completed Frame At");
            ITimeMonitor otherFrameMonitor = (ITimeMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), "Other Frame Time");
            ITimeMonitor sleepFrameMonitor = (ITimeMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), "Sleep Frame Time");
            IPhysicsFrameMonitor physicsFrameMonitor = (IPhysicsFrameMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), "Total Physics Frame Time");
            ITimeMonitor physicsSyncFrameMonitor = (ITimeMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), "Physics Sync Frame Time");
            ITimeMonitor physicsFrameTimeMonitor = (ITimeMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), "Physics Update Frame Time");
            int maintc = Util.EnvironmentTickCount();
            int BeginningFrameTime = maintc;

            // Increment the frame counter
            ++m_frame;

            try
            {
                int OtherFrameTime = Util.EnvironmentTickCount();
                if (PhysicsReturns.Count != 0)
                {
                    lock (PhysicsReturns)
                    {
                        ILLClientInventory inventoryModule = RequestModuleInterface<ILLClientInventory>();
                        if (inventoryModule != null)
                            inventoryModule.ReturnObjects(PhysicsReturns.ToArray(), UUID.Zero);
                        PhysicsReturns.Clear();
                    }
                }
                if (m_frame % m_update_coarse_locations == 0)
                {
                    List<Vector3> coarseLocations;
                    List<UUID> avatarUUIDs;
                    SceneGraph.GetCoarseLocations(out coarseLocations, out avatarUUIDs, 60);
                    // Send coarse locations to clients 
                    foreach (IScenePresence presence in GetScenePresences ())
                    {
                        presence.SendCoarseLocations(coarseLocations, avatarUUIDs);
                    }
                }

                if (m_frame % m_update_entities == 0)
                    m_sceneGraph.UpdateEntities();

                if (m_frame % m_update_events == 0)
                    m_eventManager.TriggerOnFrame();

                int PhysicsSyncTime = Util.EnvironmentTickCount();
                TimeSpan SinceLastFrame = DateTime.UtcNow - m_lastphysupdate;

                if ((m_frame % m_update_physics == 0) && !RegionInfo.RegionSettings.DisablePhysics)
                    m_sceneGraph.UpdatePreparePhysics();

                int MonitorPhysicsSyncTime = Util.EnvironmentTickCountSubtract(PhysicsSyncTime);

                int PhysicsUpdateTime = Util.EnvironmentTickCount();

                if (m_frame % m_update_physics == 0)
                {
                    if (!RegionInfo.RegionSettings.DisablePhysics && SinceLastFrame.TotalSeconds > m_physicstimespan)
                    {
                        m_sceneGraph.UpdatePhysics(SinceLastFrame.TotalSeconds);
                        m_lastphysupdate = DateTime.UtcNow;
                    }
                }

                int MonitorPhysicsUpdateTime = Util.EnvironmentTickCountSubtract(PhysicsUpdateTime) + MonitorPhysicsSyncTime;

                physicsFrameTimeMonitor.AddTime(MonitorPhysicsUpdateTime);
                physicsFrameMonitor.AddFPS(1);
                physicsSyncFrameMonitor.AddTime(MonitorPhysicsSyncTime);

                IPhysicsMonitor monitor = RequestModuleInterface<IPhysicsMonitor>();
                if (monitor != null)
                    monitor.AddPhysicsStats(RegionInfo.RegionID, PhysicsScene);

                //Now fix the sim stats
                int MonitorOtherFrameTime = Util.EnvironmentTickCountSubtract(OtherFrameTime);
                int MonitorLastCompletedFrame = Util.EnvironmentTickCount();

                simFrameMonitor.AddFPS(1);
                lastFrameMonitor.SetValue(MonitorLastCompletedFrame);
                otherFrameMonitor.AddTime(MonitorOtherFrameTime);

                maintc = Util.EnvironmentTickCountSubtract(maintc);
                maintc = (int)(m_updatetimespan * 1000) - maintc;
            }
            catch (Exception e)
            {
                m_log.Error ("[REGION]: Failed with exception " + e.ToString () + " in region: " + RegionInfo.RegionName);
                return true;
            }

            int MonitorEndFrameTime = Util.EnvironmentTickCountSubtract(BeginningFrameTime) + maintc;

            if (maintc > 0)
                Thread.Sleep(maintc);

            sleepFrameMonitor.AddTime(maintc);

            totalFrameMonitor.AddFrameTime (MonitorEndFrameTime);
            return true;
        }

        #endregion

        #region Add/Remove Avatar Methods

        /// <summary>
        /// Adding a New Client and Create a Presence for it.
        /// Called by the LLClientView when the UseCircuitCode packet comes in
        /// Used by NPCs to add themselves to the Scene
        /// </summary>
        /// <param name="client"></param>
        public void AddNewClient(IClientAPI client)
        {
            try
            {
                System.Net.IPEndPoint ep = (System.Net.IPEndPoint)client.GetClientEP();
                AgentCircuitData aCircuit = AuthenticateHandler.AuthenticateSession(client.SessionId, client.AgentId, client.CircuitCode, ep);

                if (aCircuit == null) // no good, didn't pass NewUserConnection successfully
                    return;

                m_clientManager.Add (client);

                //Create the scenepresence
                IScenePresence sp = m_sceneGraph.CreateAndAddChildScenePresence (client);
                sp.IsChildAgent = aCircuit.child;


                //Trigger events
                m_eventManager.TriggerOnNewPresence (sp);

                //Make sure the appearanace is updated
                if (aCircuit != null)
                {
                    IAvatarAppearanceModule appearance = sp.RequestModuleInterface<IAvatarAppearanceModule> ();
                    if (appearance != null)
                        appearance.Appearance = aCircuit.Appearance;
                }

                if (GetScenePresence(client.AgentId) != null)
                {
                    EventManager.TriggerOnNewClient(client);
                    if ((aCircuit.teleportFlags & (uint)TeleportFlags.ViaLogin) != 0)
                        EventManager.TriggerOnClientLogin(client);
                }

                //Add the client to login stats
                ILoginMonitor monitor = (ILoginMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor("", "LoginMonitor");
                if ((aCircuit.teleportFlags & (uint)TeleportFlags.ViaLogin) != 0 && monitor != null)
                {
                    monitor.AddSuccessfulLogin();
                }
            }
            catch(Exception ex)
            {
                m_log.Warn("[Scene]: Error in AddNewClient: " + ex.ToString());
            }
        }

        /// <summary>
        /// Tell a single agent to disconnect from the region.
        /// Does not send the DisableSimulator EQM or close child agents
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public bool RemoveAgent (IScenePresence presence)
        {
            presence.ControllingClient.Close ();
            if (presence.ParentID != UUID.Zero)
            {
                presence.StandUp ();
            }

            EventManager.TriggerClientClosed (presence.UUID, this);
            EventManager.TriggerOnClosingClient (presence.ControllingClient);
            EventManager.TriggerOnRemovePresence (presence);

            ForEachClient (
                delegate (IClientAPI client)
                {
                    //We can safely ignore null reference exceptions.  It means the avatar is dead and cleaned up anyway
                    try { client.SendKillObject (presence.Scene.RegionInfo.RegionHandle, new IEntity[] { presence }); }
                    catch (NullReferenceException) { }
                });

            try
            {
                presence.Close ();
            }
            catch (Exception e)
            {
                m_log.Error ("[SCENE] Scene.cs:RemoveClient exception: " + e.ToString ());
            }

            //Remove any interfaces it might have stored
            presence.RemoveAllInterfaces ();

            // Remove the avatar from the scene
            m_sceneGraph.RemoveScenePresence (presence);
            m_clientManager.Remove (presence.UUID);

            AuthenticateHandler.RemoveCircuit (presence.ControllingClient.CircuitCode);
            //m_log.InfoFormat("[SCENE] Memory pre  GC {0}", System.GC.GetTotalMemory(false));
            //m_log.InfoFormat("[SCENE] Memory post GC {0}", System.GC.GetTotalMemory(true));
            return true;
        }

        #endregion

        #region SceneGraph wrapper methods

        /// <summary>
        /// Request a scene presence by UUID. Fast, indexed lookup.
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns>null if the presence was not found</returns>
        public IScenePresence GetScenePresence (UUID agentID)
        {
            return m_sceneGraph.GetScenePresence (agentID);
        }

        public IScenePresence GetScenePresence (uint agentID)
        {
            return m_sceneGraph.GetScenePresence (agentID);
        }

        /// <summary>
        /// Performs action on all scene presences.
        /// </summary>
        /// <param name="action"></param>
        public void ForEachScenePresence (Action<IScenePresence> action)
        {
            if (m_sceneGraph != null)
            {
                m_sceneGraph.ForEachScenePresence(action);
            }
        }

        public List<IScenePresence> GetScenePresences ()
        {
            return new List<IScenePresence> (m_sceneGraph.GetScenePresences ());
        }

        /// <summary>
        /// Get a prim via its local id
        /// </summary>
        /// <param name="localID"></param>
        /// <returns></returns>
        public ISceneChildEntity GetSceneObjectPart (uint localID)
        {
            ISceneChildEntity entity;
            m_sceneGraph.TryGetPart(localID, out entity);
            return entity;
        }

        /// <summary>
        /// Get a prim via its UUID
        /// </summary>
        /// <param name="fullID"></param>
        /// <returns></returns>
        public ISceneChildEntity GetSceneObjectPart (UUID ObjectID)
        {
            ISceneChildEntity entity;
            m_sceneGraph.TryGetPart(ObjectID, out entity);
            return entity as SceneObjectPart;
        }

        public bool TryGetPart (UUID objectUUID, out ISceneChildEntity SensedObject)
        {
            return m_sceneGraph.TryGetPart (objectUUID, out SensedObject);
        }

        /// <summary>
        /// Get a scene object group that contains the prim with the given local id
        /// </summary>
        /// <param name="localID"></param>
        /// <returns>null if no scene object group containing that prim is found</returns>
        public ISceneEntity GetGroupByPrim (uint localID)
        {
            ISceneChildEntity part = GetSceneObjectPart (localID);
            if (part != null)
                return part.ParentEntity;
            return null;
        }

        public bool TryGetScenePresence (UUID avatarId, out IScenePresence avatar)
        {
            return m_sceneGraph.TryGetScenePresence(avatarId, out avatar);
        }

        public bool TryGetAvatarByName (string avatarName, out IScenePresence avatar)
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
                    EventManager.TriggerStartupComplete(this, StartupData);
                }
            }
        }

        /// <summary>
        /// Startup is complete, trigger the modules and allow logins
        /// </summary>
        /// <param name="data"></param>
        public void StartupComplete(IScene scene, List<string> data)
        {
            // In 99.9% of cases it is a bad idea to manually force garbage collection. However,
            // this is a rare case where we know we have just went through a long cycle of heap
            // allocations, and there is no more work to be done until someone logs in
            GC.Collect();

            m_log.Info("[Region]: Startup Complete in region " + RegionInfo.RegionName);
            
            //Tell the SceneManager about it
            m_sceneManager.HandleStartupComplete(this, data);
        }

        #endregion
    }
}