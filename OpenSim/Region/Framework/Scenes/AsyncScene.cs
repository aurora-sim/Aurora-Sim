/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
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
using Nini.Config;
using Aurora.Framework;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;

namespace OpenSim.Region.Framework.Scenes
{
    public class AsyncScene : RegistryCore, IScene
    {
        #region Fields

        private readonly List<ISceneEntity> m_PhysicsReturns = new List<ISceneEntity> ();
        public List<ISceneEntity> PhysicsReturns
        {
            get { return m_PhysicsReturns; }
        }

        /// <value>
        /// The scene graph for this scene
        /// </value>
        private SceneGraph m_sceneGraph;

        protected readonly ClientManager m_clientManager = new ClientManager();

        public ClientManager ClientManager
        {
            get { return m_clientManager; }
        }

        protected RegionInfo m_regInfo;
        protected List<IClientNetworkServer> m_clientServers;

        protected ThreadMonitor monitor = new ThreadMonitor();
        protected ThreadMonitor physmonitor = new ThreadMonitor();
            
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

        private ISceneManager m_sceneManager;

        public ISceneManager SceneManager
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

        private const int m_update_physics = 1; //Trigger the physics update
        private const int m_update_entities = 5; // Send prim updates for clients
        private const int m_update_events = 1; //Trigger the OnFrame event and tell any modules about the new frame
        private const int m_update_coarse_locations = 30; //Trigger the sending of coarse location updates (minimap updates)

        private volatile bool shuttingdown = false;

        private bool m_ShouldRunHeartbeat = true;
        public bool ShouldRunHeartbeat
        {
            get { return m_ShouldRunHeartbeat; }
            set { m_ShouldRunHeartbeat = value; }
        }

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

        protected ISimulationDataStore m_simDataStore;
        public ISimulationDataStore SimulationDataService
        {
            get { return m_simDataStore; }
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
            set { m_regInfo = value; }
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

        public void Initialize (RegionInfo regionInfo, AgentCircuitManager authen, List<IClientNetworkServer> clientServers)
        {
            Initialize (regionInfo);

            //Set up the clientServer
            m_clientServers = clientServers;
            foreach (IClientNetworkServer clientServer in clientServers)
            {
                clientServer.AddScene (this);
            }

            m_sceneManager = RequestModuleInterface<ISceneManager> ();
            m_simDataStore = m_sceneManager.GetNewSimulationDataStore ();

            m_config = m_sceneManager.ConfigSource;
            m_authenticateHandler = authen;

            m_AuroraEventManager = new AuroraEventManager();
            m_eventManager = new EventManager();
            m_permissions = new ScenePermissions(this);

            // Load region settings
            m_regInfo.RegionSettings = Aurora.DataManager.DataManager.RequestPlugin<IRegionInfoConnector> ().LoadRegionSettings (m_regInfo.RegionID);

            m_sceneGraph = new SceneGraph(this, m_regInfo) { Entities = new AsyncEntityManager() };

            #region Region Config

            IConfig aurorastartupConfig = m_config.Configs["AuroraStartup"];
            if (aurorastartupConfig != null)
            {
                //Region specific is still honored here, the RegionInfo checks for it, and if it is 0, it didn't set it
                if(RegionInfo.ObjectCapacity == 0)
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

            m_updatetimespan = 1000 / m_basesimfps;
            m_physicstimespan = 1000 / m_basesimphysfps;

            #region Startup Complete config

            EventManager.OnAddToStartupQueue += AddToStartupQueue;
            EventManager.OnModuleFinishedStartup += FinishedStartup;
            //EventManager.OnStartupComplete += StartupComplete;

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
            MainConsole.Instance.InfoFormat ("[Scene]: Closing down the single simulator: {0}", RegionInfo.RegionName);

            SimulationDataService.Shutdown ();

            // Kick all ROOT agents with the message, 'The simulator is going down'
            ForEachScenePresence (delegate (IScenePresence avatar)
            {
                if (!avatar.IsChildAgent)
                    avatar.ControllingClient.Kick("The simulator is going down.");
            });

            //Let things process and get sent for a bit
            Thread.Sleep (1000);

            IEntityTransferModule transferModule = RequestModuleInterface<IEntityTransferModule> ();
            if (transferModule != null)
            {
                foreach (IScenePresence avatar in new List<IScenePresence>(GetScenePresences ()))
                {
                    transferModule.IncomingCloseAgent (this, avatar.UUID);
                }
            }
            m_ShouldRunHeartbeat = false; //Stop the heartbeat
            //Now close the tracker
            monitor.Stop();

            if (m_sceneGraph.PhysicsScene != null)
                m_sceneGraph.PhysicsScene.Dispose ();

            // Stop updating the scene objects and agents.
            shuttingdown = true;

            m_sceneGraph.Close ();
            foreach (IClientNetworkServer clientServer in m_clientServers)
            {
                clientServer.Stop ();
            }
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

            foreach (IClientNetworkServer clientServer in m_clientServers)
            {
                clientServer.Start ();
            }

            //Give it the heartbeat delegate with an infinite timeout
            monitor.StartTrackingThread(0, Update);
            physmonitor.StartTrackingThread(0, PhysUpdate);
            //Then start the thread for it with an infinite loop time and no 
            //  sleep overall as the Update delete does it on it's own
            monitor.StartMonitor(0, 0);
            physmonitor.StartMonitor(0, 0);
        }

        #endregion

        #region Scene Heartbeat Methods

        private bool m_lastPhysicsChange = false;
        private bool PhysUpdate ()
        {
            IPhysicsFrameMonitor physicsFrameMonitor = (IPhysicsFrameMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), MonitorModuleHelper.TotalPhysicsFrameTime);
            ITimeMonitor physicsFrameTimeMonitor = (ITimeMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), MonitorModuleHelper.PhysicsUpdateFrameTime);
            IPhysicsMonitor monitor2 = RequestModuleInterface<IPhysicsMonitor>();
            while(true)
            {
                if(!ShouldRunHeartbeat) //If we arn't supposed to be running, kill ourselves
                    return false;
                int maintc = Util.EnvironmentTickCount();
                int BeginningFrameTime = maintc;

                if(PhysicsReturns.Count != 0)
                {
                    lock(PhysicsReturns)
                    {
                        ILLClientInventory inventoryModule = RequestModuleInterface<ILLClientInventory>();
                        if(inventoryModule != null)
                            inventoryModule.ReturnObjects(PhysicsReturns.ToArray(), UUID.Zero);
                        PhysicsReturns.Clear();
                    }
                }

                int PhysicsUpdateTime = Util.EnvironmentTickCount();

                if(m_frame % m_update_physics == 0)
                {
                    TimeSpan SinceLastFrame = DateTime.UtcNow - m_lastphysupdate;
                    if(!RegionInfo.RegionSettings.DisablePhysics && ApproxEquals((float)SinceLastFrame.TotalMilliseconds, 
                        m_updatetimespan, 3))
                    {
                        m_sceneGraph.UpdatePreparePhysics();
                        m_sceneGraph.UpdatePhysics(SinceLastFrame.TotalSeconds);
                        m_lastphysupdate = DateTime.UtcNow;
                        int MonitorPhysicsUpdateTime = Util.EnvironmentTickCountSubtract(PhysicsUpdateTime);

                        if(MonitorPhysicsUpdateTime != 0)
                        {
                            if(physicsFrameTimeMonitor != null)
                                physicsFrameTimeMonitor.AddTime(MonitorPhysicsUpdateTime);
                            if(monitor2 != null)
                                monitor2.AddPhysicsStats(RegionInfo.RegionID, PhysicsScene);
                            if(m_lastPhysicsChange != RegionInfo.RegionSettings.DisablePhysics)
                                StartPhysicsScene();
                        }
                        if(physicsFrameMonitor != null)
                            physicsFrameMonitor.AddFPS(1);
                    }
                    else if(m_lastPhysicsChange != RegionInfo.RegionSettings.DisablePhysics)
                        StopPhysicsScene();
                    m_lastPhysicsChange = RegionInfo.RegionSettings.DisablePhysics;
                }

                //Get the time between beginning and end
                maintc = Util.EnvironmentTickCountSubtract(BeginningFrameTime);
                if(maintc == 0)
                    continue;
                int getSleepTime = GetHeartbeatSleepTime(maintc, true);
                if(getSleepTime > 0)
                    Thread.Sleep(getSleepTime);
            }
        }

        private bool ApproxEquals (float a, float b, int approx)
        {
            return (a - b + approx) > 0;
        }

        private readonly List<BlankHandler> m_events = new List<BlankHandler>();
        public List<BlankHandler> Events
        {
            get { return m_events; }
        }

        private bool Update()
        {
            ISimFrameMonitor simFrameMonitor = (ISimFrameMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), MonitorModuleHelper.SimFrameStats);
            ITotalFrameTimeMonitor totalFrameMonitor = (ITotalFrameTimeMonitor)RequestModuleInterface<IMonitorModule> ().GetMonitor (RegionInfo.RegionID.ToString (), MonitorModuleHelper.TotalFrameTime);
            ISetMonitor lastFrameMonitor = (ISetMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), MonitorModuleHelper.LastCompletedFrameAt);
            ITimeMonitor otherFrameMonitor = (ITimeMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), MonitorModuleHelper.OtherFrameTime);
            ITimeMonitor sleepFrameMonitor = (ITimeMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), MonitorModuleHelper.SleepFrameTime);
            while (true)
            {
                if (!ShouldRunHeartbeat) //If we arn't supposed to be running, kill ourselves
                    return false;

                int maintc = Util.EnvironmentTickCount();
                int BeginningFrameTime = maintc;
                // Increment the frame counter
                ++m_frame;

                try
                {
                    int OtherFrameTime = Util.EnvironmentTickCount();
                    if(m_frame % m_update_coarse_locations == 0)
                    {
                        List<Vector3> coarseLocations;
                        List<UUID> avatarUUIDs;
                        if(SceneGraph.GetCoarseLocations(out coarseLocations, out avatarUUIDs, 60))
                        {
                            // Send coarse locations to clients 
                            foreach(IScenePresence presence in GetScenePresences())
                            {
                                presence.SendCoarseLocations(coarseLocations, avatarUUIDs);
                            }
                        }
                    }
                    
                    if(m_frame % m_update_entities == 0)
                        m_sceneGraph.UpdateEntities();

                    BlankHandler[] events;
                    lock(m_events)
                    {
                        events = new BlankHandler[m_events.Count];
                        m_events.CopyTo(events);
                        m_events.Clear();
                    }
                    foreach(BlankHandler h in events)
                        try { h(); }
                        catch { }

                    if(m_frame % m_update_events == 0)
                        m_sceneGraph.PhysicsScene.UpdatesLoop();

                    if (m_frame % m_update_events == 0)
                        m_eventManager.TriggerOnFrame();

                    //Now fix the sim stats
                    int MonitorOtherFrameTime = Util.EnvironmentTickCountSubtract(OtherFrameTime);
                    int MonitorLastCompletedFrame = Util.EnvironmentTickCount();

                    if(simFrameMonitor != null)
                    {
                        simFrameMonitor.AddFPS(1);
                        lastFrameMonitor.SetValue(MonitorLastCompletedFrame);
                        otherFrameMonitor.AddTime(MonitorOtherFrameTime);
                    }
                    else
                    {
                        simFrameMonitor = (ISimFrameMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), MonitorModuleHelper.SimFrameStats);
                        totalFrameMonitor = (ITotalFrameTimeMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), MonitorModuleHelper.TotalFrameTime);
                        lastFrameMonitor = (ISetMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), MonitorModuleHelper.LastCompletedFrameAt);
                        otherFrameMonitor = (ITimeMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), MonitorModuleHelper.OtherFrameTime);
                        sleepFrameMonitor = (ITimeMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor(RegionInfo.RegionID.ToString(), MonitorModuleHelper.SleepFrameTime);
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[REGION]: Failed with exception " + e + " in region: " + RegionInfo.RegionName);
                    return true;
                }

                //Get the time between beginning and end
                maintc = Util.EnvironmentTickCountSubtract (BeginningFrameTime);
                //Beginning + (time between beginning and end) = end
                int MonitorEndFrameTime = BeginningFrameTime + maintc;

                int getSleepTime = GetHeartbeatSleepTime (maintc, false);
                if (getSleepTime > 0)
                    Thread.Sleep (getSleepTime);

                if(sleepFrameMonitor != null)
                {
                    sleepFrameMonitor.AddTime(maintc);
                    totalFrameMonitor.AddFrameTime(MonitorEndFrameTime);
                }
            }
        }

        /// <summary>
        /// Reload the last saved physics state to the Physics Scene
        /// </summary>
        public void StartPhysicsScene ()
        {
            //Save how all the prims are moving so that we can resume it when we turn it back on
            IPhysicsStateModule physicsState = RequestModuleInterface<IPhysicsStateModule> ();
            if (physicsState != null)
                physicsState.ResetToLastSavedState ();
        }

        /// <summary>
        /// Takes a state save of the Physics Scene, then clears all velocity from it so that objects stop moving
        /// </summary>
        public void StopPhysicsScene ()
        {
            //Save how all the prims are moving so that we can resume it when we turn it back on
            IPhysicsStateModule physicsState = RequestModuleInterface<IPhysicsStateModule> ();
            if (physicsState != null)
                physicsState.SavePhysicsState ();

            //Then clear all the velocity and stuff on objects
            foreach (PhysicsObject o in PhysicsScene.ActiveObjects)
            {
                o.ClearVelocity ();
                o.RequestPhysicsterseUpdate ();
            }
            foreach (IScenePresence sp in GetScenePresences ())
            {
                sp.PhysicsActor.ForceSetVelocity (Vector3.Zero);
                sp.SendTerseUpdateToAllClients ();
            }
        }

        private readonly AveragingClass m_heartbeatList = new AveragingClass(50);
        private readonly AveragingClass m_physheartbeatList = new AveragingClass(50);
        private int GetHeartbeatSleepTime (int timeBeatTook, bool phys)
        {
            if(phys)
            {
                //Add it to the list of the last 50 heartbeats
                if(timeBeatTook != 0)
                    m_physheartbeatList.Add(timeBeatTook);
                int avgHeartBeat = (int)m_physheartbeatList.GetAverage();

                //The heartbeat sleep time if time dilation is 1
                float normalHeartBeatSleepTime = m_physicstimespan;
                if(avgHeartBeat > normalHeartBeatSleepTime)//Fudge a bit
                    return 0;//It doesn't get any sleep
                int newAvgSleepTime = (int)(normalHeartBeatSleepTime - avgHeartBeat);
                return newAvgSleepTime;//Fudge a bit
            }
            else
            {
                //Add it to the list of the last 50 heartbeats
                m_heartbeatList.Add(timeBeatTook);
                int avgHeartBeat = (int)m_heartbeatList.GetAverage();

                //The heartbeat sleep time if time dilation is 1
                int normalHeartBeatSleepTime = (int)m_updatetimespan;
                if(avgHeartBeat > normalHeartBeatSleepTime)//Fudge a bit
                    return 0;//It doesn't get any sleep
                int newAvgSleepTime = normalHeartBeatSleepTime - avgHeartBeat;
                //Console.WriteLine (newAvgSleepTime);
                return newAvgSleepTime;
            }
        }

        #endregion

        #region Add/Remove Avatar Methods

        /// <summary>
        /// Adding a New Client and Create a Presence for it.
        /// Called by the LLClientView when the UseCircuitCode packet comes in
        /// Used by NPCs to add themselves to the Scene
        /// </summary>
        /// <param name="client"></param>
        /// <param name="completed"></param>
        public void AddNewClient (IClientAPI client, BlankHandler completed)
        {
            lock(m_events)
                m_events.Add(delegate
                                 {
                    try
                    {
                        System.Net.IPEndPoint ep = (System.Net.IPEndPoint)client.GetClientEP();
                        AgentCircuitData aCircuit = AuthenticateHandler.AuthenticateSession(client.SessionId, client.AgentId, client.CircuitCode, ep);

                        if(aCircuit == null) // no good, didn't pass NewUserConnection successfully
                        {
                            completed();
                            return;
                        }

                        m_clientManager.Add(client);

                        //Create the scenepresence
                        IScenePresence sp = CreateAndAddChildScenePresence(client);
                        sp.IsChildAgent = aCircuit.child;
                        sp.RootAgentHandle = aCircuit.roothandle;
                        sp.DrawDistance = aCircuit.DrawDistance;

                        //Trigger events
                        m_eventManager.TriggerOnNewPresence(sp);

                        //Make sure the appearanace is updated
                        IAvatarAppearanceModule appearance = sp.RequestModuleInterface<IAvatarAppearanceModule>();
                        if (appearance != null)
                        {
                            appearance.Appearance = aCircuit.Appearance ?? sp.Scene.AvatarService.GetAppearance(sp.UUID);
                            if (appearance.Appearance == null)
                            {
                                MainConsole.Instance.Error("[AsyncScene]: NO AVATAR APPEARANCE FOUND FOR " + sp.Name);
                                appearance.Appearance = new AvatarAppearance(sp.UUID);
                            }
                        }

                        if(GetScenePresence(client.AgentId) != null)
                        {
                            EventManager.TriggerOnNewClient(client);
                            if((aCircuit.teleportFlags & (uint)TeleportFlags.ViaLogin) != 0)
                                EventManager.TriggerOnClientLogin(client);
                        }

                        //Add the client to login stats
                        ILoginMonitor monitor3 = (ILoginMonitor)RequestModuleInterface<IMonitorModule>().GetMonitor("", MonitorModuleHelper.LoginMonitor);
                        if((aCircuit.teleportFlags & (uint)TeleportFlags.ViaLogin) != 0 && monitor3 != null)
                            monitor3.AddSuccessfulLogin();

                        if(sp.IsChildAgent)//If we're a child, trigger this so that we get updated in the modules
                            sp.TriggerSignificantClientMovement();
                        completed();
                    }
                    catch(Exception ex)
                    {
                        MainConsole.Instance.Warn("[Scene]: Error in AddNewClient: " + ex);
                    }
                });
        }

        protected internal IScenePresence CreateAndAddChildScenePresence (IClientAPI client)
        {
            AsyncScenePresence newAvatar = new AsyncScenePresence(client, this) {IsChildAgent = true};

            m_sceneGraph.AddScenePresence(newAvatar);

            return newAvatar;
        }

        /// <summary>
        /// Tell a single agent to disconnect from the region.
        /// Does not send the DisableSimulator EQM or close child agents
        /// </summary>
        /// <param name="?"></param>
        /// <param name="presence"></param>
        /// <param name="forceClose"></param>
        /// <returns></returns>
        public bool RemoveAgent (IScenePresence presence, bool forceClose)
        {
            lock(m_events)
                m_events.Add(delegate
                                 {
                    presence.ControllingClient.Close(forceClose);
                    foreach(IClientNetworkServer cns in m_clientServers)
                        cns.RemoveClient(presence.ControllingClient);

                    if(presence.ParentID != UUID.Zero)
                        presence.StandUp();

                    EventManager.TriggerOnClosingClient(presence.ControllingClient);
                    EventManager.TriggerOnRemovePresence(presence);

                    ForEachClient(
                        delegate(IClientAPI client)
                        {
                            if(client.AgentId != presence.UUID)
                            {
                                //We can safely ignore null reference exceptions.  It means the avatar is dead and cleaned up anyway
                                try { client.SendKillObject(presence.Scene.RegionInfo.RegionHandle, new IEntity[] { presence }); }
                                catch(NullReferenceException) { }
                            }
                        });

                    // Remove the avatar from the scene
                    m_sceneGraph.RemoveScenePresence(presence);
                    m_clientManager.Remove(presence.UUID);

                    try
                    {
                        presence.Close();
                    }
                    catch(Exception e)
                    {
                        MainConsole.Instance.Error("[SCENE] Scene.cs:RemoveClient:Presence.Close exception: " + e);
                    }

                    //Remove any interfaces it might have stored
                    presence.RemoveAllInterfaces();

                    AuthenticateHandler.RemoveCircuit(presence.ControllingClient.CircuitCode);
                    //MainConsole.Instance.InfoFormat("[SCENE] Memory pre  GC {0}", System.GC.GetTotalMemory(false));
                    //MainConsole.Instance.InfoFormat("[SCENE] Memory post GC {0}", System.GC.GetTotalMemory(true));
                });
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
            lock(Events)
            {
                Events.Add(delegate
                               {
                    if(m_sceneGraph != null)
                    {
                        m_sceneGraph.ForEachScenePresence(action);
                    }
                });
            }
        }

        public List<IScenePresence> GetScenePresences ()
        {
            return m_sceneGraph.GetScenePresences ();
        }

        public int GetScenePresenceCount()
        {
            return Entities.GetPresenceCount();
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
            lock(Events)
            {
#if (!ISWIN)
                Events.Add(delegate()
                {
                    m_clientManager.ForEachSync(action);
                });
#else
                Events.Add(() => m_clientManager.ForEachSync(action));
#endif
            }
        }

        public bool TryGetClient(UUID avatarID, out IClientAPI client)
        {
            return m_clientManager.TryGetValue(avatarID, out client);
        }

        public bool TryGetClient(System.Net.IPEndPoint remoteEndPoint, out IClientAPI client)
        {
            return m_clientManager.TryGetValue(remoteEndPoint, out client);
        }

        public void ForEachSceneEntity (Action<ISceneEntity> action)
        {
            lock(Events)
            {
#if (!ISWIN)
                Events.Add(delegate()
                {
                    m_sceneGraph.ForEachSceneEntity(action);
                });
#else
                Events.Add(() => m_sceneGraph.ForEachSceneEntity(action));
#endif
            }
        }

        #endregion

        #region Startup Complete

        private readonly List<string> StartupCallbacks = new List<string>();
        private readonly List<string> StartupData = new List<string>();

        /// <summary>
        /// Add a module to the startup queue
        /// </summary>
        /// <param name="name"></param>
        public void AddToStartupQueue(string name)
        {
            IConfig startupConfig = m_config.Configs["Startup"];
            bool add = startupConfig.GetBoolean("CompleteStartupAfterAllModulesLoad", true);
            if ((add) ||
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
                    List<string> NewData = new List<string>(data.Count + 2) {name, data.Count.ToString()}; //Fixed size to reduce memory
                    NewData.AddRange(data);
                    StartupData.AddRange(NewData);
                }
                if (StartupCallbacks.Count == 0)
                {
                    //All callbacks are done, trigger startup complete
                    EventManager.TriggerStartupComplete(this, StartupData);
                    StartupComplete (this, StartupData);
                }
            }
        }

        /// <summary>
        /// Startup is complete, trigger the modules and allow logins
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="data"></param>
        public void StartupComplete(IScene scene, List<string> data)
        {
            //Tell the SceneManager about it
            m_sceneManager.HandleStartupComplete(this, data);
        }

        #endregion
    }
}
