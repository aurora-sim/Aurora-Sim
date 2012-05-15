/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using Aurora.Framework;
using Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using Timer = System.Timers.Timer;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    [Serializable]
    public class ScriptEngine : ISharedRegionModule, IScriptModulePlugin
    {
        #region Declares

        #region Delegates

        public delegate void ObjectRemoved(UUID ObjectID);

        public delegate void ScriptRemoved(UUID ItemID);

        #endregion

        public static ScriptProtectionModule ScriptProtection;

        private readonly List<IScene> m_Scenes = new List<IScene>();

        // Handles and queues incoming events from OpenSim

        // Handles loading/unloading of scripts into AppDomains
        public AppDomainManager AppDomainManager;
        public AssemblyResolver AssemblyResolver;
        public bool ChatCompileErrorsToDebugChannel = true;

        //The compiler for all scripts
        public Compiler Compiler;
        public bool DisplayErrorsOnConsole = true;
        public EventManager EventManager;
        public bool FirstStartup = true;

        //This deals with all state saving for scripts

        //Handles the queues
        public MaintenanceThread MaintenanceThread;

        //Handles script errors

        public bool RunScriptsInAttachments;
        public IConfig ScriptConfigSource;

        /// <summary>
        ///   Script events per second, used by stats
        /// </summary>
        public int ScriptEPS;

        /// <summary>
        ///   Path to the script binaries.
        /// </summary>
        public string ScriptEnginesPath = "ScriptEngines";

        /// <summary>
        ///   Errors of scripts that have failed in this run of the Maintenance Thread
        /// </summary>
        public string ScriptErrorMessages = "";

        public ScriptErrorReporter ScriptErrorReporter;

        /// <summary>
        ///   Number of scripts that have failed in this run of the Maintenance Thread
        /// </summary>
        public int ScriptFailCount;

        public bool ShowWarnings;
        public ScriptStateSave StateSave;
        public Timer UpdateLeasesTimer;
        private IScriptApi[] m_APIs = new IScriptApi[0];
        private IConfigSource m_ConfigSource;
        private IXmlRpcRouter m_XmlRpcRouter;

        private bool m_consoleDisabled;
        private bool m_disabled;
        private bool m_enabled;

        public List<IScene> Worlds
        {
            get { return m_Scenes; }
        }

        /// <summary>
        ///   Disabled from the command line, takes presidence over normal Disabled
        /// </summary>
        public bool ConsoleDisabled
        {
            get { return m_consoleDisabled; }
            set
            {
                m_consoleDisabled = value;
                if (!value)
                {
                    //Poke the threads to make sure they run
                    MaintenanceThread.PokeThreads(UUID.Zero);
                }
                else
                    MaintenanceThread.DisableThreads();
            }
        }

        /// <summary>
        ///   Temperary disable by things like OAR loading so that we don't kill loading
        /// </summary>
        public bool Disabled
        {
            get { return m_disabled; }
            set
            {
                m_disabled = value;
                if (!value)
                {
                    //Poke the threads to make sure they run
                    MaintenanceThread.PokeThreads(UUID.Zero);
                }
                else
                    MaintenanceThread.DisableThreads();
            }
        }

        public IConfig Config
        {
            get { return ScriptConfigSource; }
        }

        public IConfigSource ConfigSource
        {
            get { return m_ConfigSource; }
        }

        public string ScriptEngineName
        {
            get { return "AuroraDotNetEngine"; }
        }

        public IScriptModule ScriptModule
        {
            get { return this; }
        }

        public event ScriptRemoved OnScriptRemoved;

        public event ObjectRemoved OnObjectRemoved;

        #endregion

        #region Constructor and Shutdown

        public void Shutdown()
        {
            // We are shutting down
            foreach (ScriptData ID in ScriptProtection.GetAllScripts())
            {
                ID.CloseAndDispose(true);
            }
        }

        #endregion

        #region ISharedRegionModule

        private int AmountOfStartupsLeft;

        public void Initialise(IConfigSource config)
        {
            m_ConfigSource = config;
            ScriptConfigSource = config.Configs[ScriptEngineName];
            if (ScriptConfigSource == null)
                return;

            m_enabled = ScriptConfigSource.GetBoolean("Enabled", false);

            RunScriptsInAttachments = ScriptConfigSource.GetBoolean("AllowRunningOfScriptsInAttachments", false);
            ScriptEnginesPath = ScriptConfigSource.GetString("PathToLoadScriptsFrom", ScriptEnginesPath);
            ShowWarnings = ScriptConfigSource.GetBoolean("ShowWarnings", ShowWarnings);
            DisplayErrorsOnConsole = ScriptConfigSource.GetBoolean("DisplayErrorsOnConsole", DisplayErrorsOnConsole);
            ChatCompileErrorsToDebugChannel = ScriptConfigSource.GetBoolean("ChatCompileErrorsToDebugChannel",
                                                                            ChatCompileErrorsToDebugChannel);

            if (Compiler != null)
                Compiler.ReadConfig();

            if (ScriptProtection != null)
                ScriptProtection.Initialize(this, Config);
        }

        public void PostInitialise()
        {
        }

        public void AddRegion(IScene scene)
        {
            if (!m_enabled)
                return;

            //Register the console commands
            if (FirstStartup)
            {
                if (MainConsole.Instance != null)
                {
                    MainConsole.Instance.Commands.AddCommand("ADNE restart", "ADNE restart",
                                                             "Restarts all scripts and clears all script caches",
                                                             AuroraDotNetRestart);
                    MainConsole.Instance.Commands.AddCommand("ADNE stop", "ADNE stop", "Stops all scripts",
                                                             AuroraDotNetStop);
                    MainConsole.Instance.Commands.AddCommand("ADNE stats", "ADNE stats",
                                                             "Tells stats about the script engine", AuroraDotNetStats);
                    MainConsole.Instance.Commands.AddCommand("ADNE disable", "ADNE disable",
                                                             "Disables the script engine temperarily",
                                                             AuroraDotNetDisable);
                    MainConsole.Instance.Commands.AddCommand("ADNE enable", "ADNE enable", "Reenables the script engine",
                                                             AuroraDotNetEnable);
                }

                // Create all objects we'll be using
                ScriptProtection = new ScriptProtectionModule(this, Config);
                ScriptProtection.Initialize(this, Config);

                EventManager = new EventManager(this);

                Compiler = new Compiler(this);

                AppDomainManager = new AppDomainManager(this);

                ScriptErrorReporter = new ScriptErrorReporter(Config);

                AssemblyResolver = new AssemblyResolver(ScriptEnginesPath);
            }

            FirstStartup = false;

            scene.StackModuleInterface<IScriptModule>(this);
        }

        public void RegionLoaded(IScene scene)
        {
            if (!m_enabled)
                return;

            m_Scenes.Add(scene);

            //Must come AFTER the script plugins setup! Otherwise you'll get weird errors from the plugins
            if (MaintenanceThread == null)
            {
                //Only needs created once
                MaintenanceThread = new MaintenanceThread(this);

                //Still must come before the maintenance thread start
                StartSharedScriptPlugins(); //This only gets called once

                m_XmlRpcRouter = m_Scenes[0].RequestModuleInterface<IXmlRpcRouter>();
                if (m_XmlRpcRouter != null)
                {
                    OnScriptRemoved += m_XmlRpcRouter.ScriptRemoved;
                    OnObjectRemoved += m_XmlRpcRouter.ObjectRemoved;
                }

                StateSave = new ScriptStateSave();
                StateSave.Initialize(this);

                FindDefaultLSLScript();
            }

            AddRegionToScriptModules(scene);
            StateSave.AddScene(scene);

            scene.EventManager.OnStartupComplete += EventManager_OnStartupComplete;
            EventManager.HookUpRegionEvents(scene);

            //Hook up to client events
            scene.EventManager.OnNewClient += EventManager_OnNewClient;
            scene.EventManager.OnClosingClient += EventManager_OnClosingClient;

            scene.EventManager.OnRemoveScript += StopScript;

            UpdateLeasesTimer = new Timer(9.5 * 1000 * 60 /*9.5 minutes*/) { Enabled = true };
            UpdateLeasesTimer.Elapsed += UpdateAllLeases;
            UpdateLeasesTimer.Start();
        }

        public void RemoveRegion(IScene scene)
        {
            if (!m_enabled)
                return;

            scene.EventManager.OnRemoveScript -= StopScript;

            scene.UnregisterModuleInterface<IScriptModule>(this);
            m_Scenes.Remove(scene);
            UpdateLeasesTimer.Enabled = false;
            UpdateLeasesTimer.Elapsed -= UpdateAllLeases;
            UpdateLeasesTimer.Stop();

            Shutdown();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return ScriptEngineName; }
        }

        public void Close()
        {
        }

        private void EventManager_OnStartupComplete(IScene scene, List<string> data)
        {
            AmountOfStartupsLeft++;
            SceneManager m = scene.RequestModuleInterface<SceneManager>();
            if (AmountOfStartupsLeft >= m.AllRegions)
            {
                //All done!
                MaintenanceThread.Started = true;
            }
        }

        #endregion

        #region Client Events

        private void EventManager_OnNewClient(IClientAPI client)
        {
            client.OnScriptReset += ProcessScriptReset;
            client.OnGetScriptRunning += OnGetScriptRunning;
            client.OnSetScriptRunning += SetScriptRunning;
        }

        private void EventManager_OnClosingClient(IClientAPI client)
        {
            client.OnScriptReset -= ProcessScriptReset;
            client.OnGetScriptRunning -= OnGetScriptRunning;
            client.OnSetScriptRunning -= SetScriptRunning;
        }

        #endregion

        #region Console Commands

        public void StopAllScripts()
        {
            foreach (ScriptData ID in ScriptProtection.GetAllScripts())
            {
                ID.CloseAndDispose(true);
            }
        }

        private void FindDefaultLSLScript()
        {
            if (!Directory.Exists(ScriptEnginesPath))
            {
                try
                {
                    Directory.CreateDirectory(ScriptEnginesPath);
                }
                catch (Exception)
                {
                }
            }
            string Dir = Path.Combine(Path.Combine(Environment.CurrentDirectory, ScriptEnginesPath), "default.lsl");
            if (File.Exists(Dir))
            {
                string defaultScript = File.ReadAllText(Dir);
#if (!ISWIN)
                foreach (IScene scene in m_Scenes)
                {
                    ILLClientInventory inventoryModule = scene.RequestModuleInterface<ILLClientInventory>();
                    if (inventoryModule != null)
                    {
                        inventoryModule.DefaultLSLScript = defaultScript;
                    }
                }
#else
                foreach (ILLClientInventory inventoryModule in m_Scenes.Select(scene => scene.RequestModuleInterface<ILLClientInventory>()).Where(inventoryModule => inventoryModule != null))
                {
                    inventoryModule.DefaultLSLScript = defaultScript;
                }
#endif
            }
        }

        protected void AuroraDotNetRestart(string[] cmdparams)
        {
            string go =
                MainConsole.Instance.Prompt(
                    "Are you sure you want to restart all scripts? (This also wipes the script state saves database, which could cause loss of information in your scripts)",
                    "no");
            if (go.Equals("yes", StringComparison.CurrentCultureIgnoreCase))
            {
                //Clear out all of the data on the threads that we have just to make sure everything is all clean
                this.MaintenanceThread.DisableThreads();
                this.MaintenanceThread.PokeThreads(UUID.Zero);
                ScriptData[] scripts = ScriptProtection.GetAllScripts();
                ScriptProtection.Reset(true);
                foreach (ScriptData ID in scripts)
                {
                    ID.CloseAndDispose(false); //We don't want to backup
                    //Remove the state save
                    StateSave.DeleteFrom(ID);
                }

                ScriptProtection.Reset(true);
                //Delete all assemblies
                Compiler.RecreateDirectory();
#if (!ISWIN)
                List<LUStruct> list = new List<LUStruct>();
                foreach (ScriptData id in scripts)
                    list.Add(new LUStruct { Action = LUType.Load, ID = id });
                MaintenanceThread.StartScripts(list.ToArray());
#else
                MaintenanceThread.StartScripts(scripts.Select(ID => new LUStruct { Action = LUType.Load, ID = ID }).ToArray());
#endif
                MainConsole.Instance.Warn("[ADNE]: All scripts have been restarted.");
            }
            else
            {
                MainConsole.Instance.Info("Not restarting all scripts");
            }
        }

        protected void AuroraDotNetStop(string[] cmdparams)
        {
            string go = MainConsole.Instance.Prompt("Are you sure you want to stop all scripts?", "no");
            if (go.Contains("yes") || go.Contains("Yes"))
            {
                StopAllScripts();
                MaintenanceThread.Stop();
                MainConsole.Instance.Warn("[ADNE]: All scripts have been stopped.");
            }
            else
            {
                MainConsole.Instance.Info("Not restarting all scripts");
            }
        }

        protected void AuroraDotNetStats(string[] cmdparams)
        {
            MainConsole.Instance.Info("Aurora DotNet Script Engine Stats:"
                       + "\nNumber of scripts compiled: " + Compiler.ScriptCompileCounter
                       + "\nMax allowed threat level: " + ScriptProtection.GetThreatLevel()
                       + "\nNumber of scripts running now: " + ScriptProtection.GetAllScripts().Length
                       + "\nNumber of app domains: " + AppDomainManager.NumberOfAppDomains
                       + "\nPermission level of app domains: " + AppDomainManager.PermissionLevel
                       + "\nNumber Script Event threads: " +
                       (MaintenanceThread.scriptThreadpool == null ? 0 : MaintenanceThread.scriptThreadpool.nthreads).
                           ToString()
                       + "/" +
                       (MaintenanceThread.scriptThreadpool == null
                            ? 0
                            : MaintenanceThread.scriptThreadpool.nSleepingthreads).ToString());
            MaintenanceThread.Stats();
        }

        protected void AuroraDotNetDisable(string[] cmdparams)
        {
            ConsoleDisabled = true;
            MainConsole.Instance.Warn("[ADNE]: ADNE has been disabled.");
        }

        protected void AuroraDotNetEnable(string[] cmdparams)
        {
            ConsoleDisabled = false;
            MaintenanceThread.Started = true;
            MainConsole.Instance.Warn("[ADNE]: ADNE has been enabled.");
        }

        #endregion

        #region Update Leases [Unused]

        public void UpdateAllLeases(object sender, ElapsedEventArgs e)
        {
            foreach (ScriptData script in ScriptProtection.GetAllScripts())
            {
                if (script.Running == false || script.Disabled || script.Script == null)
                    return;

                try
                {
                    script.Script.UpdateLease(DateTime.Now.AddMinutes(10) - DateTime.Now);
                }
                catch (Exception)
                {
                    MainConsole.Instance.Error("Lease found dead!" + script.ItemID);
                }
            }
        }

        #endregion

        #region Post Object Events

        public bool PostScriptEvent(UUID itemID, UUID primID, EventParams p, EventPriority priority)
        {
            ScriptData ID = ScriptProtection.GetScript(primID, itemID);
            if (ID == null || !ID.Running)
                return false;
            return AddToScriptQueue(ID,
                                    p.EventName, p.DetectParams, priority, p.Params);
        }

        public bool PostScriptEvent(UUID itemID, UUID primID, string name, Object[] p)
        {
            Object[] lsl_p = new Object[p.Length];
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i] is int)
                    lsl_p[i] = new LSL_Types.LSLInteger((int)p[i]);
                else if (p[i] is UUID)
                    lsl_p[i] = new LSL_Types.LSLString(UUID.Parse(p[i].ToString()).ToString());
                else if (p[i] is string)
                    lsl_p[i] = new LSL_Types.LSLString((string)p[i]);
                else if (p[i] is Vector3)
                    lsl_p[i] = new LSL_Types.Vector3(((Vector3)p[i]).X, ((Vector3)p[i]).Y, ((Vector3)p[i]).Z);
                else if (p[i] is Quaternion)
                    lsl_p[i] = new LSL_Types.Quaternion(((Quaternion)p[i]).X, ((Quaternion)p[i]).Y,
                                                        ((Quaternion)p[i]).Z, ((Quaternion)p[i]).W);
                else if (p[i] is float)
                    lsl_p[i] = new LSL_Types.LSLFloat((float)p[i]);
                else
                    lsl_p[i] = p[i];
            }

            return PostScriptEvent(itemID, primID, new EventParams(name, lsl_p, new DetectParams[0]),
                                   EventPriority.FirstStart);
        }

        public bool PostObjectEvent(UUID primID, string name, Object[] p)
        {
            Object[] lsl_p = new Object[p.Length];
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i] is int)
                    lsl_p[i] = new LSL_Types.LSLInteger((int)p[i]);
                else if (p[i] is UUID)
                    lsl_p[i] = new LSL_Types.LSLString(UUID.Parse(p[i].ToString()).ToString());
                else if (p[i] is string)
                    lsl_p[i] = new LSL_Types.LSLString((string)p[i]);
                else if (p[i] is Vector3)
                    lsl_p[i] = new LSL_Types.Vector3(((Vector3)p[i]).X, ((Vector3)p[i]).Y, ((Vector3)p[i]).Z);
                else if (p[i] is Quaternion)
                    lsl_p[i] = new LSL_Types.Quaternion(((Quaternion)p[i]).X, ((Quaternion)p[i]).Y,
                                                        ((Quaternion)p[i]).Z, ((Quaternion)p[i]).W);
                else if (p[i] is float)
                    lsl_p[i] = new LSL_Types.LSLFloat((float)p[i]);
                else if (p[i] is Changed)
                {
                    Changed c = (Changed)p[i];
                    lsl_p[i] = new LSL_Types.LSLInteger((int)c);
                }
                else
                    lsl_p[i] = p[i];
            }

            return AddToObjectQueue(primID, name, new DetectParams[0], lsl_p);
        }

        public DetectParams GetDetectParams(UUID primID, UUID itemID, int number)
        {
            ScriptData id = ScriptProtection.GetScript(primID, itemID);

            if (id == null)
                return null;

            DetectParams[] det = id.LastDetectParams;

            if (det == null || number < 0 || number >= det.Length)
                return null;

            return det[number];
        }

        /// <summary>
        ///   Posts event to all objects in the group.
        /// </summary>
        /// <param name = "localID">Region object ID</param>
        /// <param name = "FunctionName">Name of the function, will be state + "_event_" + FunctionName</param>
        /// <param name = "VersionID">Version ID of the script. Note: If it is -1, the version ID will be detected automatically</param>
        /// <param name = "param">Array of parameters to match event mask</param>
        public bool AddToObjectQueue(UUID partID, string FunctionName, DetectParams[] qParams, object[] param)
        {
            // Determine all scripts in Object and add to their queue
            ScriptData[] datas = ScriptProtection.GetScripts(partID);

            if (datas == null)
                //No scripts to post to... so it is firing all the events it needs to
                return true;

            foreach (ScriptData ID in datas)
            {
                // Add to each script in that object
                AddToScriptQueue(ID, FunctionName, qParams, EventPriority.FirstStart, param);
            }
            return true;
        }

        /// <summary>
        ///   Posts the event to the given object.
        /// </summary>
        /// <param name = "ID"></param>
        /// <param name = "FunctionName"></param>
        /// <param name = "qParams"></param>
        /// <param name = "param"></param>
        /// <returns></returns>
        public bool AddToScriptQueue(ScriptData ID, string FunctionName, DetectParams[] qParams, EventPriority priority,
                                     object[] param)
        {
            // Create a structure and add data
            QueueItemStruct QIS = new QueueItemStruct
                                      {
                                          ID = ID,
                                          EventsProcData = new ScriptEventsProcData(),
                                          functionName = FunctionName,
                                          llDetectParams = qParams,
                                          param = param,
                                          VersionID = Interlocked.Read(ref ID.VersionID),
                                          State = ID.State
                                      };

            return MaintenanceThread.AddEventSchQIS(QIS, priority);
        }

        #endregion

        #region Get/Set Start Parameter and Min Event Delay

        public int GetStartParameter(UUID itemID, UUID primID)
        {
            ScriptData id = ScriptProtection.GetScript(primID, itemID);

            if (id == null)
                return 0;

            return id.StartParam;
        }

        public void SetMinEventDelay(UUID itemID, UUID primID, double delay)
        {
            ScriptData ID = ScriptProtection.GetScript(primID, itemID);
            if (ID == null)
            {
                MainConsole.Instance.ErrorFormat("[{0}]: SetMinEventDelay found no InstanceData for script {1}.", ScriptEngineName,
                                  itemID.ToString());
                return;
            }
            if (delay > 0.001)
                ID.EventDelayTicks = (long)delay;
            else
                ID.EventDelayTicks = 0;
            ID.EventDelayTicks = (long)(delay * 10000000L);
        }

        #endregion

        #region Get/Set Script States/Running

        public void SetState(UUID itemID, string state)
        {
            ScriptData id = ScriptProtection.GetScript(itemID);

            if (id == null)
                return;

            id.ChangeState(state);
        }

        public bool GetScriptRunningState(UUID itemID)
        {
            ScriptData id = ScriptProtection.GetScript(itemID);
            if (id == null)
                return false;

            return id.Running;
        }

        public void SetScriptRunningState(UUID itemID, bool state)
        {
            ScriptData id = ScriptProtection.GetScript(itemID);
            if (id == null)
                return;

            if (!id.Disabled)
                id.Running = state;
        }

        /// <summary>
        ///   Get the total number of active (running) scripts on the object or avatar
        /// </summary>
        /// <param name = "obj"></param>
        /// <returns></returns>
        public int GetActiveScripts(IEntity obj)
        {
            int activeScripts = 0;
            if (obj is IScenePresence)
            {
                //Get all the scripts in the attachments and run through the loop
                IAttachmentsModule attModule =
                    (obj as IScenePresence).Scene.RequestModuleInterface<IAttachmentsModule>();
                if (attModule != null)
                {
                    ISceneEntity[] attachments = attModule.GetAttachmentsForAvatar(obj.UUID);
                    foreach (ISceneEntity grp in attachments)
                    {
                        activeScripts += GetActiveScripts(grp);
                    }
                }
            }
            else //Ask the protection module how many Scripts there are
            {
                ScriptData[] scripts = ScriptProtection.GetScripts(obj.UUID);
                foreach (ScriptData script in scripts)
                {
                    if (script.Running) activeScripts++;
                }
            }
            return activeScripts;
        }

        /// <summary>
        ///   Get the total (running and non-running) scripts on the object or avatar
        /// </summary>
        /// <param name = "obj"></param>
        /// <returns></returns>
        public int GetTotalScripts(IEntity obj)
        {
            int totalScripts = 0;
            if (obj is IScenePresence)
            {
                //Get all the scripts in the attachments
                IAttachmentsModule attModule = ((IScenePresence)obj).Scene.RequestModuleInterface<IAttachmentsModule>();
                if (attModule != null)
                {
                    ISceneEntity[] attachments = attModule.GetAttachmentsForAvatar(obj.UUID);
                    foreach (ISceneEntity grp in attachments)
                    {
                        totalScripts += GetTotalScripts(grp);
                    }
                }
            }
            else //Ask the protection module how many Scripts there are
                totalScripts += ScriptProtection.GetScripts(obj.UUID).Length;
            return totalScripts;
        }

        public int GetScriptTime(UUID itemID)
        {
            return ScriptProtection.GetScript(itemID).ScriptScore;
        }

        public void SetScriptRunning(IClientAPI controllingClient, UUID objectID, UUID itemID, bool running)
        {
            ISceneChildEntity part = findPrim(objectID);
            if (part == null)
                return;

            if (running)
                OnStartScript(part.LocalId, itemID);
            else
                OnStopScript(part.LocalId, itemID);
        }

        #endregion

        #region Reset

        public void ResetScript(UUID primID, UUID itemID, bool EndEvent)
        {
            ScriptData ID = ScriptProtection.GetScript(itemID);
            if (ID == null)
                return;

            ID.Reset();

            if (EndEvent)
                throw new EventAbortException();
        }

        public void ProcessScriptReset(IClientAPI remoteClient, UUID objectID,
                                       UUID itemID)
        {
            ISceneChildEntity part = findPrim(objectID);
            if (part == null)
                return;

            if (part.ParentEntity.Scene.Permissions.CanResetScript(objectID, itemID, remoteClient.AgentId))
            {
                ScriptData ID = ScriptProtection.GetScript(itemID);
                if (ID == null)
                    return;

                ID.Reset();
            }
        }

        #endregion

        #region Start/End/Suspend Scripts

        /// <summary>
        ///   Not from the client, only from other parts of the simulator
        /// </summary>
        /// <param name = "itemID"></param>
        public void SuspendScript(UUID itemID)
        {
            ScriptData ID = ScriptProtection.GetScript(itemID);
            if (ID != null)
                ID.Suspended = true;
        }

        /// <summary>
        ///   Not from the client, only from other parts of the simulator
        /// </summary>
        /// <param name = "itemID"></param>
        public void ResumeScript(UUID itemID)
        {
            ScriptData ID = ScriptProtection.GetScript(itemID);
            if (ID != null)
                ID.Suspended = false;
        }

        public void OnStartScript(uint localID, UUID itemID)
        {
            ScriptData id = ScriptProtection.GetScript(itemID);
            if (id == null)
                return;

            id.Running = true;
            id.Suspended = false; //Set this too, it gets stuck sometimes...
            id.Part.SetScriptEvents(itemID, id.Script.GetStateEventFlags(id.State));
            id.Part.ScheduleUpdate(PrimUpdateFlags.FindBest);
        }

        public void OnStopScript(uint localID, UUID itemID)
        {
            ScriptData ID = ScriptProtection.GetScript(itemID);
            if (ID == null)
                return;

            ID.Running = false;
            ID.Part.SetScriptEvents(itemID, 0);
            ID.Part.ScheduleUpdate(PrimUpdateFlags.FindBest);
        }

        public void OnGetScriptRunning(IClientAPI controllingClient,
                                       UUID objectID, UUID itemID)
        {
            ScriptData id = ScriptProtection.GetScript(objectID, itemID);
            if (id == null)
                return;

            IEventQueueService eq = id.World.RequestModuleInterface<IEventQueueService>();
            if (eq == null)
            {
                controllingClient.SendScriptRunningReply(objectID, itemID,
                                                         id.Running);
            }
            else
            {
                eq.ScriptRunningReply(objectID, itemID, id.Running, true,
                                      controllingClient.AgentId, controllingClient.Scene.RegionInfo.RegionHandle);
            }
        }

        #endregion

        #region Error reporting

        public ArrayList GetScriptErrors(UUID itemID)
        {
            return ScriptErrorReporter.FindErrors(itemID);
        }

        #endregion

        #region Starting, Updating, and Stopping scripts

        public void UpdateScript(UUID partID, UUID itemID, string script, int startParam, bool postOnRez,
                                 StateSource stateSource)
        {
            ScriptData id = ScriptProtection.GetScript(partID, itemID);
            LUStruct ls = new LUStruct();
            //Its a change of the script source, needs to be recompiled and such.
            if (id == null)
            {
                MaintenanceThread.AddScriptChange(new LUStruct[1]
                                                      {
                                                          StartScript(findPrim(partID), itemID, startParam, postOnRez,
                                                                      stateSource, UUID.Zero)
                                                      }, LoadPriority.Restart);
                return;
            }
            ls.Action = LUType.Reupload;
            id.PostOnRez = postOnRez;
            id.StartParam = startParam;
            id.stateSource = stateSource;
            id.Source = script;
            id.EventDelayTicks = 0;
            id.Part = findPrim(partID);
            id.ItemID = itemID;
            id.EventDelayTicks = 0;

            //No SOP, no compile.
            if (id.Part == null)
            {
                MainConsole.Instance.ErrorFormat(
                    "[{0}]: Could not find scene object part corresponding " + "to localID {1} to start script",
                    ScriptEngineName, partID);
                return;
            }
            id.World = id.Part.ParentEntity.Scene;
            ls.ID = id;

            // Stop long command on script
            RemoveScriptFromPlugins(partID, itemID);

            MaintenanceThread.AddScriptChange(new[] { ls }, LoadPriority.Restart);
        }

        public void SaveStateSave(UUID ItemID, UUID PrimID)
        {
            ScriptData id = ScriptProtection.GetScript(PrimID, ItemID);
            if (id == null)
                return;
            StateSave.SaveStateTo(id);
        }

        public void SaveStateSaves()
        {
            foreach (ScriptData id in ScriptProtection.GetAllScripts())
            {
                if (id == null)
                    return;
                StateSave.SaveStateTo(id, true, false);
            }
        }

        public void UpdateScriptToNewObject(UUID olditemID, TaskInventoryItem newItem, ISceneChildEntity newPart)
        {
            try
            {
                if (newPart.ParentEntity.Scene != null)
                {
                    ScriptData SD = ScriptProtection.GetScript(olditemID);
                    if (SD == null)
                        return;

                    IScenePresence presence = SD.World.GetScenePresence(SD.Part.OwnerID);

                    ScriptControllers SC = new ScriptControllers();
                    if (presence != null)
                    {
                        IScriptControllerModule m = presence.RequestModuleInterface<IScriptControllerModule>();
                        if (m != null)
                        {
                            SC = m.GetScriptControler(SD.ItemID);
                            if ((newItem.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                            {
                                m.UnRegisterControlEventsToScript(SD.Part.LocalId, SD.ItemID);
                            }
                        }
                    }
                    OSDMap Plugins = GetSerializationData(SD.ItemID, SD.Part.UUID);
                    RemoveScriptFromPlugins(SD.Part.UUID, SD.ItemID);

                    MaintenanceThread.SetEventSchSetIgnoreNew(SD, true);

                    ScriptProtection.RemoveScript(SD);

                    SD.Part = newPart;
                    SD.ItemID = newItem.ItemID;
                    //Find the asset ID
                    SD.InventoryItem = newItem;
                    //Try to see if this was rezzed from someone's inventory
                    SD.UserInventoryItemID = SD.Part.FromUserInventoryItemID;

                    CreateFromData(SD.Part.UUID, SD.ItemID, SD.Part.UUID, Plugins);

                    SD.World = newPart.ParentEntity.Scene;
                    SD.SetApis();

                    MaintenanceThread.SetEventSchSetIgnoreNew(SD, false);


                    if (presence != null && (newItem.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        SC.itemID = newItem.ItemID;
                        SC.part = SD.Part;
                        IScriptControllerModule m = presence.RequestModuleInterface<IScriptControllerModule>();
                        if (m != null)
                            m.RegisterScriptController(SC);
                    }

                    ScriptProtection.AddNewScript(SD);

                    StateSave.SaveStateTo(SD, true);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        ///   Fetches, loads and hooks up a script to an objects events
        /// </summary>
        /// <param name = "itemID"></param>
        /// <param name = "localID"></param>
        public LUStruct StartScript(ISceneChildEntity part, UUID itemID, int startParam, bool postOnRez,
                                    StateSource statesource, UUID RezzedFrom)
        {
            ScriptData id = ScriptProtection.GetScript(part.UUID, itemID);

            LUStruct ls = new LUStruct();
            //Its a change of the script source, needs to be recompiled and such.
            if (id != null)
            {
                //Ignore prims that have crossed regions, they are already started and working
                if ((statesource & StateSource.PrimCrossing) != 0)
                {
                    //Post the changed event though
                    AddToScriptQueue(id, "changed", new DetectParams[0], EventPriority.FirstStart,
                                     new Object[] { new LSL_Types.LSLInteger(512) });
                    return new LUStruct { Action = LUType.Unknown };
                }
                else
                {
                    //Restart other scripts
                    ls.Action = LUType.Load;
                }
                id.EventDelayTicks = 0;
                ScriptProtection.RemovePreviouslyCompiled(id.Source);
            }
            else
                ls.Action = LUType.Load;
            if (id == null)
                id = new ScriptData(this);
            id.ItemID = itemID;
            id.PostOnRez = postOnRez;
            id.StartParam = startParam;
            id.stateSource = statesource;
            id.Part = part;
            id.World = part.ParentEntity.Scene;
            id.RezzedFrom = RezzedFrom;
            ls.ID = id;
            //WE MUST ADD THIS HERE, even though it hasn't compiled yet... 
            //we need to add it so that if things go trying to add events before it fully compiles, we don't fail completely
            ScriptProtection.AddNewScript(id);
            return ls;
        }

        public void SaveStateSaves(UUID PrimID)
        {
            ScriptData[] ids = ScriptProtection.GetScripts(PrimID);
            if (ids == null)
                return;
            foreach (ScriptData id in ids)
            {
                StateSave.SaveStateTo(id, false);
            }
        }

        /// <summary>
        ///   Disables and unloads a script
        /// </summary>
        /// <param name = "localID"></param>
        /// <param name = "itemID"></param>
        public void StopScript(uint localID, UUID itemID)
        {
            ScriptData data = ScriptProtection.GetScript(itemID);
            if (data == null)
                return;

            LUStruct ls = new LUStruct { ID = data, Action = LUType.Unload };


            MaintenanceThread.AddScriptChange(new[] { ls }, LoadPriority.Stop);

            //Disconnect from other modules
            ObjectRemoved handlerObjectRemoved = OnObjectRemoved;
            if (handlerObjectRemoved != null)
                handlerObjectRemoved(ls.ID.Part.UUID);

            ScriptRemoved handlerScriptRemoved = OnScriptRemoved;
            if (handlerScriptRemoved != null)
                handlerScriptRemoved(itemID);
        }

        #endregion

        #region Test Compiling Scripts

        public string TestCompileScript(UUID assetID, UUID itemID)
        {
            AssetBase asset = m_Scenes[0].AssetService.Get(assetID.ToString());
            if (null == asset)
                return "Could not find script.";
            else
            {
                string script = Utils.BytesToString(asset.Data);
                try
                {
                    Compiler.PerformInMemoryScriptCompile(script, itemID);
                }
                catch (Exception e)
                {
                    string error = "Error compiling script: " + e;
                    if (error.Length > 255)
                        error = error.Substring(0, 255);
                    return error;
                }
                if (Compiler.GetErrors().Length != 0)
                {
                    string error = "Error compiling script: ";
                    foreach (string comperror in Compiler.GetErrors())
                    {
                        error += comperror;
                    }
                    error += ".";
                    return error;
                }
                return "";
            }
        }

        #endregion

        #region API Manager

        private static Dictionary<string, IScriptApi> m_apiFunctionNamesCache = new Dictionary<string, IScriptApi>();

        public IScriptApi GetApi(UUID itemID, string name)
        {
            ScriptData id = ScriptProtection.GetScript(itemID);
            if (id == null)
                return null;

            return id.Apis[name];
        }

        public List<string> GetAllFunctionNames()
        {
            List<string> FunctionNames = new List<string>();

            IScriptApi[] apis = GetAPIs();
            foreach (IScriptApi api in apis)
            {
                FunctionNames.AddRange(GetFunctionNames(api));
            }

            return FunctionNames;
        }

        public IScriptApi[] GetAPIs()
        {
            if (m_APIs.Length == 0)
            {
                m_APIs = AuroraModuleLoader.PickupModules<IScriptApi>().ToArray();
                //Only add Apis that are considered safe
#if (!ISWIN)
                List<IScriptApi> list = new List<IScriptApi>();
                foreach (IScriptApi api in m_APIs)
                {
                    if (ScriptProtection.CheckAPI(api.Name)) list.Add(api);
                }
                m_APIs = list.ToArray();
#else
                m_APIs = m_APIs.Where(api => ScriptProtection.CheckAPI(api.Name)).ToArray();
#endif
            }
            IScriptApi[] apis = new IScriptApi[m_APIs.Length];
            int i = 0;
            foreach (IScriptApi api in m_APIs)
            {
                apis[i] = api.Copy();
                i++;
            }
            return apis;
        }

        public Dictionary<string, IScriptApi> GetAllFunctionNamesAPIs()
        {
            if (m_apiFunctionNamesCache.Count > 0)
                return m_apiFunctionNamesCache;
            Dictionary<string, IScriptApi> FunctionNames = new Dictionary<string, IScriptApi>();

            IScriptApi[] apis = m_APIs.Length == 0 ? GetAPIs() : m_APIs;
            foreach (IScriptApi api in apis)
            {
#if (!ISWIN)
                foreach (string functionName in GetFunctionNames(api))
                {
                    if (!FunctionNames.ContainsKey(functionName))
                    {
                        FunctionNames.Add(functionName, api);
                    }
                }
#else
                foreach (string functionName in GetFunctionNames(api).Where(functionName => !FunctionNames.ContainsKey(functionName)))
                {
                    FunctionNames.Add(functionName, api);
                }
#endif
            }
            m_apiFunctionNamesCache = FunctionNames;
            return FunctionNames;
        }

        public List<string> GetFunctionNames(IScriptApi api)
        {
            MemberInfo[] members = api.GetType().GetMembers();
            List<string> FunctionNames = new List<string>();
            foreach (MemberInfo member in members)
            {
                if (member.Name.StartsWith(api.Name, StringComparison.CurrentCultureIgnoreCase))
                    FunctionNames.Add(member.Name);
            }
            members = null;
            return FunctionNames;
        }

        #endregion

        #region Script Plugin Manager

        private readonly List<IScriptPlugin> ScriptPlugins = new List<IScriptPlugin>();

        public IScriptPlugin GetScriptPlugin(string Name)
        {
            foreach (IScriptPlugin plugin in ScriptPlugins)
            {
                if (plugin.Name == Name)
                    return plugin;
            }
            return null;
        }

        /// <summary>
        ///   Make sure that the threads are running
        /// </summary>
        public void PokeThreads(UUID itemID)
        {
            MaintenanceThread.PokeThreads(itemID);
        }

        /// <summary>
        ///   Starts all non shared script plugins
        /// </summary>
        /// <param name = "scene"></param>
        private void AddRegionToScriptModules(IScene scene)
        {
            foreach (IScriptPlugin plugin in ScriptPlugins)
            {
                plugin.AddRegion(scene);
            }
        }

        /// <summary>
        ///   Starts all shared script plugins
        /// </summary>
        public void StartSharedScriptPlugins()
        {
            List<IScriptPlugin> sharedPlugins = AuroraModuleLoader.PickupModules<IScriptPlugin>();
            foreach (IScriptPlugin plugin in sharedPlugins)
            {
                plugin.Initialize(this);
            }
            ScriptPlugins.AddRange(sharedPlugins.ToArray());
        }

        public bool DoOneScriptPluginPass()
        {
            bool didAnything = false;
            foreach (IScriptPlugin plugin in ScriptPlugins)
            {
                if (didAnything)
                    plugin.Check();
                else
                    didAnything = plugin.Check();
            }
            return didAnything;
        }

        /// <summary>
        ///   Removes a script from all Script Plugins
        /// </summary>
        /// <param name = "localID"></param>
        /// <param name = "itemID"></param>
        public void RemoveScriptFromPlugins(UUID primID, UUID itemID)
        {
            foreach (IScriptPlugin plugin in ScriptPlugins)
            {
                plugin.RemoveScript(primID, itemID);
            }
        }

        public OSDMap GetSerializationData(UUID itemID, UUID primID)
        {
            OSDMap data = new OSDMap();

            foreach (IScriptPlugin plugin in ScriptPlugins)
            {
                try
                {
                    data.Add(plugin.Name, plugin.GetSerializationData(itemID, primID));
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.Warn("[" + Name + "]: Error attempting to get serialization data, " + ex);
                }
            }

            return data;
        }

        public void CreateFromData(UUID primID,
                                   UUID itemID, UUID hostID, OSDMap data)
        {
            foreach (KeyValuePair<string, OSD> kvp in data)
            {
                IScriptPlugin plugin = GetScriptPlugin(kvp.Key);
                if (plugin != null)
                    plugin.CreateFromData(itemID, hostID, kvp.Value);
            }
        }

        #endregion

        #region Helpers

        public bool PipeEventsForScript(ISceneChildEntity part, Vector3 position)
        {
            // Changed so that child prims of attachments return ScriptDanger for their parent, so that
            //  their scripts will actually run.
            //      -- Leaf, Tue Aug 12 14:17:05 EDT 2008
            ISceneChildEntity parent = part.ParentEntity.RootChild;
            if (parent != null && parent.IsAttachment)
                return ScriptDanger(parent, position);
            else
                return ScriptDanger(part, position);
        }

        public ISceneChildEntity findPrim(UUID objectID)
        {
#if (!ISWIN)
            foreach (IScene s in m_Scenes)
            {
                ISceneChildEntity part = s.GetSceneObjectPart(objectID);
                if (part != null) return part;
            }
            return null;
#else
            return m_Scenes.Select(s => s.GetSceneObjectPart(objectID)).FirstOrDefault(part => part != null);
#endif
        }

        public ISceneChildEntity findPrim(uint localID)
        {
#if (!ISWIN)
            foreach (IScene s in m_Scenes)
            {
                ISceneChildEntity part = s.GetSceneObjectPart(localID);
                if (part != null) return part;
            }
            return null;
#else
            return m_Scenes.Select(s => s.GetSceneObjectPart(localID)).FirstOrDefault(part => part != null);
#endif
        }

        public IScene findPrimsScene(uint localID)
        {
            foreach (IScene s in m_Scenes)
            {
                ISceneChildEntity part = s.GetSceneObjectPart(localID);
                if (part != null)
                    return s;
            }
            return null;
        }

        private bool ScriptDanger(ISceneChildEntity part, Vector3 pos)
        {
            IScene scene = part.ParentEntity.Scene;
            if (part.IsAttachment && RunScriptsInAttachments)
                return true; //Always run as in SL

            IParcelManagementModule parcelManagement = scene.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject parcel = parcelManagement.GetLandObject(pos.X, pos.Y);
                if (parcel != null)
                {
                    if ((parcel.LandData.Flags & (uint)ParcelFlags.AllowOtherScripts) != 0)
                        return true;
                    else if ((parcel.LandData.Flags & (uint)ParcelFlags.AllowGroupScripts) != 0)
                    {
                        if (part.OwnerID == parcel.LandData.OwnerID
                            || (parcel.LandData.IsGroupOwned && part.GroupID == parcel.LandData.GroupID)
                            || scene.Permissions.IsGod(part.OwnerID))
                            return true;
                        else
                            return false;
                    }
                    else
                    {
                        //Gods should be able to run scripts. 
                        // -- Revolution
                        if (part.OwnerID == parcel.LandData.OwnerID || scene.Permissions.IsGod(part.OwnerID))
                            return true;
                        else
                            return false;
                    }
                }
                else
                {
                    if (pos.X > 0f && pos.X < scene.RegionInfo.RegionSizeX && pos.Y > 0f &&
                        pos.Y < scene.RegionInfo.RegionSizeY)
                        // The only time parcel != null when an object is inside a region is when
                        // there is nothing behind the landchannel.  IE, no land plugin loaded.
                        return true;
                    else
                        // The object is outside of this region.  Stop piping events to it.
                        return false;
                }
            }
            return true;
        }

        public bool PipeEventsForScript(ISceneChildEntity part)
        {
            // Changed so that child prims of attachments return ScriptDanger for their parent, so that
            //  their scripts will actually run.
            //      -- Leaf, Tue Aug 12 14:17:05 EDT 2008
            ISceneChildEntity parent = part.ParentEntity.RootChild;
            if (parent != null && parent.IsAttachment)
                return PipeEventsForScript(parent, parent.AbsolutePosition);
            else
                return PipeEventsForScript(part, part.AbsolutePosition);
        }

        #endregion

        #region Stats

        /// <summary>
        ///   Get the current number of events being fired per second
        /// </summary>
        /// <returns></returns>
        public int GetScriptEPS()
        {
            int EPS = ScriptEPS;
            //Return it to 0 now that we've sent it.
            ScriptEPS = 0;
            return EPS;
        }

        /// <summary>
        ///   Get the number of active scripts in this instance
        /// </summary>
        /// <returns></returns>
        public int GetActiveScripts()
        {
            //Get all the scripts
            ScriptData[] data = ScriptProtection.GetAllScripts();
            int activeScripts = 0;
            foreach (ScriptData script in data)
            {
                //Only if the script is running do we include it
                if (script.Running) activeScripts++;
            }
            return activeScripts;
        }

        /// <summary>
        ///   Get the top scripts in this instance
        /// </summary>
        /// <returns></returns>
        public Dictionary<uint, float> GetTopScripts(UUID RegionID)
        {
            List<ScriptData> data = new List<ScriptData>(ScriptProtection.GetAllScripts());
            data.RemoveAll(delegate(ScriptData script)
                               {
                                   //Remove the scripts that are in a different region
                                   if (script.World.RegionInfo.RegionID != RegionID)
                                       return true;
                                   else
                                       return false;
                               });
            //Now sort and put the top scripts in the correct order
            data.Sort(ScriptScoreSorter);
            if (data.Count > 100)
            {
                //We only take the top 100
                data.RemoveRange(100, data.Count - 100);
            }
            Dictionary<uint, float> topScripts = new Dictionary<uint, float>();
            foreach (ScriptData script in data)
            {
                if (!topScripts.ContainsKey(script.Part.ParentEntity.LocalId))
                    topScripts.Add(script.Part.ParentEntity.LocalId, script.ScriptScore);
                else
                    topScripts[script.Part.ParentEntity.LocalId] += script.ScriptScore;
                script.ScriptScore = 0;
            }
            return topScripts;
        }

        private int ScriptScoreSorter(ScriptData scriptA, ScriptData scriptB)
        {
            return scriptA.ScriptScore.CompareTo(scriptB.ScriptScore);
        }

        #endregion

        #region Registry pieces

        private readonly Dictionary<Type, object> m_extensions = new Dictionary<Type, object>();

        public Dictionary<Type, object> Extensions
        {
            get { return m_extensions; }
        }

        public void RegisterExtension<T>(T instance)
        {
            m_extensions[typeof(T)] = instance;
        }

        #endregion
    }

    public class ScriptErrorReporter
    {
        //Errors that have been thrown while compiling
        private readonly Dictionary<UUID, ArrayList> Errors = new Dictionary<UUID, ArrayList>();
        private readonly int Timeout = 5000; // 5 seconds

        public ScriptErrorReporter(IConfig config)
        {
            Timeout = (config.GetInt("ScriptErrorFindingTimeOut", 5) * 1000);
        }

        /// <summary>
        ///   Add a new error for the client thread to find
        /// </summary>
        /// <param name = "ItemID"></param>
        /// <param name = "errors"></param>
        public void AddError(UUID ItemID, ArrayList errors)
        {
            lock (Errors)
            {
                Errors[ItemID] = errors;
            }
        }

        /// <summary>
        ///   Find the errors that the script may have produced while compiling
        /// </summary>
        /// <param name = "ItemID"></param>
        /// <returns></returns>
        public ArrayList FindErrors(UUID ItemID)
        {
            ArrayList Error = new ArrayList();

            if (!TryFindError(ItemID, out Error))
                return new ArrayList(new[] { "Compile not finished." });
            //Not there, but need to return something so the user knows

            RemoveError(ItemID);

            if ((string)Error[0] == "SUCCESSFULL")
                return new ArrayList();

            return Error;
        }

        /// <summary>
        ///   Wait while the script is processed
        /// </summary>
        /// <param name = "ItemID"></param>
        /// <param name = "error"></param>
        /// <returns></returns>
        private bool TryFindError(UUID ItemID, out ArrayList error)
        {
            error = null;
            lock (Errors)
            {
                if (!Errors.ContainsKey(ItemID))
                    Errors[ItemID] = null; //Add it so that it does not error out with no key
            }

            int i = 0;
            while ((error = Errors[ItemID]) == null && i < Timeout)
            {
                Thread.Sleep(50);
                i += 50;
            }
            if (i < 5000)
                return true;
            else
                return false; //Cut off
        }

        /// <summary>
        ///   Clear this item's errors
        /// </summary>
        /// <param name = "ItemID"></param>
        public void RemoveError(UUID ItemID)
        {
            if (Errors.ContainsKey(ItemID))
            {
                lock (Errors)
                {
                    Errors[ItemID] = null;
                }
            }
        }
    }
}