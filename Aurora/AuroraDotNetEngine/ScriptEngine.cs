/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Xml;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.CoreModules.Framework.EventQueue;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework.Console;
using Aurora.Framework;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public enum EventPriority : int
    {
        FirstStart = 0,
        Suspended = 1,
        Continued = 2
    }
    [Serializable]
    public class ScriptEngine : ISharedRegionModule, IScriptModule
    {
        #region Declares 

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public List<Scene> Worlds
        {
            get { return m_Scenes; }
        }

        private List<Scene> m_Scenes = new List<Scene>();

        // Handles and queues incoming events from OpenSim
        public EventManager EventManager;

        // Handles loading/unloading of scripts into AppDomains
        public AppDomainManager AppDomainManager;

        //The compiler for all scripts
        public Compiler Compiler;

        public enum LoadPriority : int
        {
            FirstStart = 0,
            Restart = 1,
            Stop = 2
        }

        public class StartPerformanceQueue
        {
            Queue FirstStartQueue = new Queue(10000);
            Queue SuspendedQueue = new Queue(100); //Smaller, we don't get this very often
            Queue ContinuedQueue = new Queue(10000);
            public bool GetNext(out object Item)
            {
                Item = null;
                if (FirstStartQueue.Count != 0)
                {
                    lock (FirstStartQueue)
                        Item = FirstStartQueue.Dequeue();
                    return true;
                }
                if (SuspendedQueue.Count != 0)
                {
                    lock (SuspendedQueue)
                        Item = SuspendedQueue.Dequeue();
                    return true;
                }
                if (ContinuedQueue.Count != 0)
                {
                    lock (ContinuedQueue)
                        Item = ContinuedQueue.Dequeue();
                    return true;
                }
                return false;
            }

            public void Add(object item, LoadPriority priority)
            {
                if (priority == LoadPriority.FirstStart)
                    lock (FirstStartQueue)
                        FirstStartQueue.Enqueue(item);
                if (priority == LoadPriority.Restart)
                    lock (SuspendedQueue)
                        SuspendedQueue.Enqueue(item);
                if (priority == LoadPriority.Stop)
                    lock (ContinuedQueue)
                        ContinuedQueue.Enqueue(item);
            }
        }

        //Queue that handles the loading and unloading of scripts
        public StartPerformanceQueue LUQueue = new StartPerformanceQueue();
        //public OpenSim.Framework.LimitedPriorityQueue<LUStruct[], LoadPriority> LUQueue = new OpenSim.Framework.LimitedPriorityQueue<LUStruct[], LoadPriority>(new ScriptLoadingPrioritizer());

        public MaintenanceThread m_MaintenanceThread;

        private IConfigSource m_ConfigSource;
        public IConfig ScriptConfigSource;
        private bool m_enabled = false;
        public bool DisplayErrorsOnConsole = false;
        public bool ShowWarnings = false;

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

        private IXmlRpcRouter m_XmlRpcRouter;
        public static ScriptProtectionModule ScriptProtection;
        
        /// <summary>
        /// Removes the script from the event queue so it does not fire anymore events.
        /// </summary>
        public static Dictionary<UUID, int> NeedsRemoved = new Dictionary<UUID, int>();

        public class ScriptPrioritizer : OpenSim.Framework.IPriorityConverter<EventPriority>
        {
            public int Convert(EventPriority priority)
            {
                return (int)priority;
            }

            public int PriorityCount
            {
                get { return 3; }
            }
        }

        /// <summary>
        /// Queue containing events waiting to be executed.
        /// </summary>
        public static PerformanceQueue EventPerformanceTestQueue = new PerformanceQueue();
        /// <summary>
        /// Queue containing scripts that need to have states saved or deleted.
        /// </summary>
        public static Queue StateQueue = new Queue();
        
        /// <summary>
        /// Time for each of the smart thread pool threads to sleep before doing the queues.
        /// </summary>
        public int SleepTime;

        /// <summary>
        /// Number of threads that should be running  to deal with starting and stopping scripts
        /// </summary>
        public int NumberOfStartStopThreads;

        /// <summary>
        /// Number of Event Queue threads that should be running
        /// </summary>
        public int NumberOfEventQueueThreads;

        /// <summary>
        /// Priority of the threads.
        /// </summary>
        public ThreadPriority ThreadPriority;

        public System.Timers.Timer UpdateLeasesTimer = null;

        public delegate void ScriptRemoved(UUID ItemID);
        public delegate void ObjectRemoved(UUID ObjectID);
        public bool FirstStartup = true; 
        public event ScriptRemoved OnScriptRemoved;
        public event ObjectRemoved OnObjectRemoved;


        /// <summary>
        /// Number of scripts that have failed in this run of the Maintenance Thread
        /// </summary>
        public int ScriptFailCount = 0;

        /// <summary>
        /// Errors of scripts that have failed in this run of the Maintenance Thread
        /// </summary>
        public string ScriptErrorMessages = "";

        #endregion

        #region Constructor and Shutdown
        
        public void Shutdown()
        {
            // We are shutting down
            foreach (ScriptData ID in ScriptProtection.GetAllScripts())
            {
                try
                {
                    ID.CloseAndDispose(false);
                }
                catch (Exception) { }
            }
        }

        #endregion

        #region ISharedRegionModule

        public void Initialise(IConfigSource config)
        {
            m_ConfigSource = config;
            ScriptConfigSource = config.Configs[ScriptEngineName];
            if (ScriptConfigSource == null)
                return;

            NumberOfEventQueueThreads = ScriptConfigSource.GetInt("NumberOfEventQueueThreads", 5);
            NumberOfStartStopThreads = ScriptConfigSource.GetInt("NumberOfStartStopThreads", 1);
            SleepTime = ScriptConfigSource.GetInt("SleepTime", 50);
            ShowWarnings = ScriptConfigSource.GetBoolean("ShowWarnings", false);
            DisplayErrorsOnConsole = ScriptConfigSource.GetBoolean("DisplayErrorsOnConsole", false);
        }

        public void PostInitialise()
        { 
        }

        public void AddRegion(Scene scene)
        {
            // Make sure we have config
            if (ConfigSource.Configs[ScriptEngineName] == null)
                ConfigSource.AddConfig(ScriptEngineName);

            m_enabled = ScriptConfigSource.GetBoolean("Enabled", true);
            if (!m_enabled)
                return;

            //Register the console commands
            if (FirstStartup)
                scene.AddCommand(this, "ADNE", "ADNE", "Subcommands for Aurora DotNet Engine", AuroraDotNetConsoleCommands);
            
            FirstStartup = false;

            ScriptConfigSource = ConfigSource.Configs[ScriptEngineName];

        	//m_log.Info("[" + ScriptEngineName + "]: ScriptEngine initializing");
            m_Scenes.Add(scene);

            // Create all objects we'll be using
            if(ScriptProtection == null)
                ScriptProtection = new ScriptProtectionModule(m_ConfigSource, this);
            
            EventManager = new EventManager(this, true);
            
            // We need to start it
            if (Compiler == null)
                Compiler = new Compiler(this);

            if(AppDomainManager == null)
                AppDomainManager = new AppDomainManager(this);

            scene.StackModuleInterface<IScriptModule>(this);

            scene.EventManager.OnRezScript += OnRezScript;
            scene.EventManager.OnRezScripts += OnRezScripts;
            //Fire this once to make sure that the APIs are found later... bad Mono.Addins...
            GetAPIs();
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_enabled)
                return;

            m_XmlRpcRouter = m_Scenes[0].RequestModuleInterface<IXmlRpcRouter>();
            if (m_XmlRpcRouter != null)
            {
                OnScriptRemoved += m_XmlRpcRouter.ScriptRemoved;
                OnObjectRemoved += m_XmlRpcRouter.ObjectRemoved;
            }

            SetUpCommandManager();

            if (m_MaintenanceThread == null)
            {
                m_MaintenanceThread = new MaintenanceThread(this);
                scene.EventManager.OnScriptLoadingComplete += m_MaintenanceThread.OnScriptsLoadingComplete;
            }

            EventManager.HookUpRegionEvents(scene);

            scene.EventManager.OnScriptReset += OnScriptReset;
            scene.EventManager.OnGetScriptRunning += OnGetScriptRunning;
            scene.EventManager.OnStartScript += OnStartScript;
            scene.EventManager.OnStopScript += OnStopScript;
            UpdateLeasesTimer = new System.Timers.Timer(9.5 * 1000 * 60 /*9.5 minutes*/);
            UpdateLeasesTimer.Enabled = true;
            UpdateLeasesTimer.Elapsed += UpdateAllLeases;
            UpdateLeasesTimer.Start();
        }

        public void RemoveRegion(Scene scene)
        {
            scene.EventManager.OnScriptReset -= OnScriptReset;
            scene.EventManager.OnGetScriptRunning -= OnGetScriptRunning;
            scene.EventManager.OnStartScript -= OnStartScript;
            scene.EventManager.OnStopScript -= OnStopScript;

            scene.UnregisterModuleInterface<IScriptModule>(this);
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

        #endregion

        #region Console Commands

        protected void AuroraDotNetConsoleCommands(string module, string[] cmdparams)
        {
            if (cmdparams[1] == "restart")
            {
                string go = MainConsole.Instance.CmdPrompt("Are you sure you want to restart all scripts? (This also wipes the script state saves database, which could cause loss of information in your scripts)", "no");
                if (go == "yes" || go == "Yes")
                {
                    //Delete all assemblies
                    Compiler.RecreateDirectory();
                    foreach (ScriptData ID in ScriptProtection.GetAllScripts())
                    {
                        try
                        {
                            //Remove the state save, remove the previously compiled referance
                            Aurora.DataManager.DataManager.RequestPlugin<Aurora.Framework.IScriptDataConnector>("IScriptDataConnector").DeleteStateSave(ID.ItemID);
                            ScriptProtection.RemovePreviouslyCompiled(ID.Source);
                            ID.Start(false);
                        }
                        catch (Exception) { }
                    }
                }
                else
                {
                    m_log.Debug("Not restarting all scripts");
                }
            }
            if (cmdparams[1] == "stop")
            {
                string go = MainConsole.Instance.CmdPrompt("Are you sure you want to stop all scripts?", "no");
                if (go.Contains("yes") || go.Contains("Yes"))
                {
                    StopAllScripts();
                }
                else
                {
                    m_log.Debug("Not restarting all scripts");
                }
            }
            if (cmdparams[1] == "stats")
            {
                m_log.Info("Aurora DotNet Script Engine Stats:"
                    + "\nNumber of scripts compiled: " + Compiler.ScriptCompileCounter
                    + "\nMax allowed threat level: " + ScriptProtection.GetThreatLevel().ToString()
                    + "\nNumber of scripts running now: " + ScriptProtection.GetAllScripts().Length
                    + "\nNumber of app domains: " + AppDomainManager.NumberOfAppDomains
                    + "\nPermission level of app domains: " + AppDomainManager.PermissionLevel);
            }
            if (cmdparams[1] == "help")
            {
                m_log.Info("Aurora DotNet Commands : \n" +
                    " ADNE restart - Restarts all scripts \n" +
                    " ADNE stop - Stops all scripts \n" +
                    " ADNE stats - Tells stats about the script engine");
            }
        }

        public void StopAllScripts()
        {
            foreach (ScriptData ID in ScriptProtection.GetAllScripts())
            {
                try
                {
                    ScriptProtection.RemovePreviouslyCompiled(ID.Source);
                    ID.CloseAndDispose(false);
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion

        public void UpdateAllLeases(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (ScriptData script in ScriptProtection.GetAllScripts())
            {
                if (script.Running == false || script.Disabled == true || script.Script == null)
                    return;

                try
                {
                    script.Script.UpdateLease(DateTime.Now.AddMinutes(10) - DateTime.Now);
                }
                catch (Exception ex)
                {
                    m_log.Error("Lease found dead!" + script.ItemID);
                }
            }
        }

        public void OnRezScript(SceneObjectPart part, UUID itemID, string script,
                int startParam, bool postOnRez, string engine, int stateSource)
        {
            if (script.StartsWith("//MRM:"))
                return;

            List<IScriptModule> engines =
                new List<IScriptModule>(
                m_Scenes[0].RequestModuleInterfaces<IScriptModule>());

            List<string> names = new List<string>();
            foreach (IScriptModule m in engines)
                names.Add(m.ScriptEngineName);

            int lineEnd = script.IndexOf('\n');

            if (lineEnd > 1)
            {
                string firstline = script.Substring(0, lineEnd).Trim();

                int colon = firstline.IndexOf(':');
                if (firstline.Length > 2 &&
                    firstline.Substring(0, 2) == "//" && colon != -1)
                {
                    string engineName = firstline.Substring(2, colon - 2);

                    if (names.Contains(engineName))
                    {
                        engine = engineName;
                        script = "//" + script.Substring(script.IndexOf(':') + 1);
                    }
                    else
                    {
                        if (engine == ScriptEngineName)
                        {
                            TaskInventoryItem item =
                                    part.Inventory.GetInventoryItem(itemID);

                            ScenePresence presence =
                                    part.ParentGroup.Scene.GetScenePresence(
                                    item.OwnerID);

                            if (presence != null)
                            {
                                presence.ControllingClient.SendAgentAlertMessage(
                                         "Selected engine unavailable. " +
                                         "Running script on " +
                                         ScriptEngineName,
                                         false);
                            }
                        }
                    }
                }
            }

            if (engine != ScriptEngineName)
                return;

            LUStruct itemToQueue = StartScript(part, itemID, script,
                    startParam, postOnRez, (StateSource)stateSource, UUID.Zero);
            if(itemToQueue != null)
                AddScriptChange(new LUStruct[] { itemToQueue }, LoadPriority.FirstStart);
        }

        public void OnRezScripts(SceneObjectPart part, TaskInventoryItem[] items,
                int startParam, bool postOnRez, string engine, int stateSource, UUID RezzedFrom)
        {
            List<TaskInventoryItem> ItemsToStart = new List<TaskInventoryItem>();
            foreach (TaskInventoryItem item in items)
            {
                AssetBase asset = m_Scenes[0].AssetService.Get(item.AssetID.ToString());
                if (null == asset)
                {
                    m_log.ErrorFormat(
                        "[PRIM INVENTORY]: " +
                        "Couldn't start script {0}, {1} since asset ID {4} could not be found",
                        item.Name, item.ItemID, item.AssetID);
                    continue;
                }
                string script = OpenMetaverse.Utils.BytesToString(asset.Data);

                if (script.StartsWith("//MRM:"))
                    return;

                List<IScriptModule> engines =
                    new List<IScriptModule>(
                    m_Scenes[0].RequestModuleInterfaces<IScriptModule>());

                List<string> names = new List<string>();
                foreach (IScriptModule m in engines)
                    names.Add(m.ScriptEngineName);

                int lineEnd = script.IndexOf('\n');

                if (lineEnd > 1)
                {
                    string firstline = script.Substring(0, lineEnd).Trim();

                    int colon = firstline.IndexOf(':');
                    if (firstline.Length > 2 &&
                        firstline.Substring(0, 2) == "//" && colon != -1)
                    {
                        string engineName = firstline.Substring(2, colon - 2);

                        if (names.Contains(engineName))
                        {
                            engine = engineName;
                            script = "//" + script.Substring(script.IndexOf(':') + 1);
                        }
                        else
                        {
                            if (engine == ScriptEngineName)
                            {
                                ScenePresence presence =
                                        part.ParentGroup.Scene.GetScenePresence(
                                        item.OwnerID);

                                if (presence != null)
                                {
                                    presence.ControllingClient.SendAgentAlertMessage(
                                             "Selected engine unavailable. " +
                                             "Running script on " +
                                             ScriptEngineName,
                                             false);
                                }
                            }
                        }
                    }
                }

                if (engine != ScriptEngineName)
                    return;
                ItemsToStart.Add(item);
            }

            List<LUStruct> ItemsToQueue = new List<LUStruct>();
            foreach (TaskInventoryItem item in ItemsToStart)
            {
                AssetBase asset = m_Scenes[0].AssetService.Get(item.AssetID.ToString());
                if (null == asset)
                {
                    m_log.ErrorFormat(
                        "[PRIM INVENTORY]: " +
                        "Couldn't start script {0}, {1} since asset ID {4} could not be found",
                        item.Name, item.ItemID, item.AssetID);
                    continue;
                }
                string script = OpenMetaverse.Utils.BytesToString(asset.Data);

                LUStruct itemToQueue = StartScript(part, item.ItemID, script,
                        startParam, postOnRez, (StateSource)stateSource, RezzedFrom);
                if (itemToQueue != null)
                    ItemsToQueue.Add(itemToQueue);
            }
            if(ItemsToQueue.Count != 0)
                AddScriptChange(ItemsToQueue.ToArray(), LoadPriority.FirstStart);

        }

		#region Post Object Events

        public bool PostObjectEvent(uint localID, EventParams p)
        {
            Scene scene = findPrimsScene(localID);
            if (scene == null)
                return false;
            SceneObjectPart part = scene.GetSceneObjectPart(localID);
            if (part == null)
                return false;
            return AddToObjectQueue(part.UUID, p.EventName,
                    p.DetectParams, -1, p.Params);
        }

        public bool PostObjectEvent(UUID primID, EventParams p)
        {
            Scene scene = findPrimsScene(primID);
            if (scene == null)
                return false;
            SceneObjectPart part = scene.GetSceneObjectPart(primID);
            if (part == null)
                return false;
            return AddToObjectQueue(part.UUID, p.EventName,
                    p.DetectParams, -1, p.Params);
        }

        public bool PostScriptEvent(UUID itemID, UUID primID, EventParams p, EventPriority priority)
        {
            ScriptData ID = GetScript(primID, itemID);
            if (ID == null)
                return false;
            return AddToScriptQueue(ID,
                    p.EventName, p.DetectParams, ID.VersionID, priority, p.Params);
        }

        public bool PostScriptEvent(UUID itemID, UUID primID, string name, Object[] p)
        {
            Object[] lsl_p = new Object[p.Length];
            for (int i = 0; i < p.Length ; i++)
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
                    lsl_p[i] = new LSL_Types.Quaternion(((Quaternion)p[i]).X, ((Quaternion)p[i]).Y, ((Quaternion)p[i]).Z, ((Quaternion)p[i]).W);
                else if (p[i] is float)
                    lsl_p[i] = new LSL_Types.LSLFloat((float)p[i]);
                else
                    lsl_p[i] = p[i];
            }

            return PostScriptEvent(itemID, primID, new EventParams(name, lsl_p, new DetectParams[0]), EventPriority.FirstStart);
        }

        public bool PostObjectEvent(UUID primID, string name, Object[] p)
        {
            SceneObjectPart part = findPrimsScene(primID).GetSceneObjectPart(primID);
            if (part == null)
                return false;

            Object[] lsl_p = new Object[p.Length];
            for (int i = 0; i < p.Length ; i++)
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
                    lsl_p[i] = new LSL_Types.Quaternion(((Quaternion)p[i]).X, ((Quaternion)p[i]).Y, ((Quaternion)p[i]).Z, ((Quaternion)p[i]).W);
                else if (p[i] is float)
                    lsl_p[i] = new LSL_Types.LSLFloat((float)p[i]);
                else
                    lsl_p[i] = p[i];
            }

            return PostObjectEvent(part.LocalId, new EventParams(name, lsl_p, new DetectParams[0]));
        }

        public DetectParams GetDetectParams(UUID primID, UUID itemID, int number)
        {
            ScriptData id = GetScript(primID, itemID);

            if (id == null)
                return null;

            DetectParams[] det = id.LastDetectParams;

            if (number < 0 || number >= det.Length)
                return null;

            return det[number];
        }

        #endregion
        
        #region Get/Set Start Parameter and Min Event Delay
        
        public int GetStartParameter(UUID itemID, UUID primID)
        {
            ScriptData id = GetScript(primID, itemID);

            if (id == null)
                return 0;

            return id.StartParam;
        }

        public void SetMinEventDelay(UUID itemID, UUID primID, double delay)
        {
            ScriptData ID = GetScript(primID, itemID);
            if(ID == null)
            {
                m_log.ErrorFormat("[{0}]: SetMinEventDelay found no InstanceData for script {1}.",ScriptEngineName,itemID.ToString());
                return;
            }
            ID.EventDelayTicks = (long)delay;
        }

        #endregion

        #region Get/Set Script States/Running

        public void SetState(UUID itemID, string state)
        {
            ScriptData id = GetScriptByItemID(itemID);

            if (id == null)
                return;

            if (id.State != state)
            {
                AddToObjectQueue(id.part.UUID, "state_exit",
                    new DetectParams[0], id.VersionID, new object[0] { });
                id.State = state;
                int eventFlags = (int)id.Script.GetStateEventFlags(id.State);

                id.part.SetScriptEvents(itemID, eventFlags);
                UpdateScriptInstanceData(id);

                AddToObjectQueue(id.part.UUID, "state_entry",
                    new DetectParams[0], id.VersionID, new object[0] { });
            }
        }

        public bool GetScriptState(UUID itemID)
        {
            ScriptData id = GetScriptByItemID(itemID);
            if (id == null)
                return false;

            return id.Running;
        }

        public void SetScriptState(UUID itemID, bool state)
        {
            ScriptData id = GetScriptByItemID(itemID);
            if (id == null)
                return;

            if (!id.Disabled)
                id.Running = state;
        }

        #endregion

        #region Reset

        public void ApiResetScript(UUID itemID)
        {
            ScriptData ID = GetScriptByItemID(itemID);
            if (ID == null)
                return;

            ID.Reset();
        }

        public void ResetScript(UUID itemID)
        {
            ScriptData ID = GetScriptByItemID(itemID);
            if (ID == null)
                return;

            ID.Reset();
        }

        public void OnScriptReset(uint localID, UUID itemID)
        {
            ScriptData ID = ScriptProtection.GetScript(itemID);
            if (ID == null)
                return;

            ID.Reset();
        }
        #endregion

        #region Start/End/Suspend Scripts

        public void OnStartScript(uint localID, UUID itemID)
        {
            ScriptData id = ScriptProtection.GetScript(itemID);
            if (id == null)
                return;        

            if (!id.Disabled)
                id.Running = true;

            LUStruct item = StartScript(id.part, itemID, id.Source, id.StartParam, true, id.stateSource, UUID.Zero);
            if (item != null)
                AddScriptChange(new LUStruct[] { item }, LoadPriority.Restart);
        }

        public void OnStopScript(uint localID, UUID itemID)
        {
            ScriptData ID = ScriptProtection.GetScript(itemID);
            if (ID == null)
                return;        
            
            StopScript(localID, itemID);
        }

        public void OnGetScriptRunning(IClientAPI controllingClient,
                UUID objectID, UUID itemID)
        {
            ScriptData id = GetScriptByItemID(itemID);
            if (id == null)
                return;        

            IEventQueue eq = id.World.RequestModuleInterface<IEventQueue>();
            if (eq == null)
            {
                controllingClient.SendScriptRunningReply(objectID, itemID,
                        id.Running);
            }
            else
            {
                eq.Enqueue(EventQueueHelper.ScriptRunningReplyEvent(objectID, itemID, id.Running, true),
                           controllingClient.AgentId);
            }
        }

        public void SuspendScript(UUID itemID)
        {
            ScriptData ID = GetScriptByItemID(itemID);
            if (ID != null)
                ID.Suspended = true;
        }

        public void ResumeScript(UUID itemID)
        {
            ScriptData ID = GetScriptByItemID(itemID);
            if (ID != null)
                ID.Suspended = false;
        }

        #endregion

        #region GetScriptAPI

        public IScriptApi GetApi(UUID itemID, string name)
        {
            ScriptData id = GetScriptByItemID(itemID);
            if (id == null)
                return null;

            if (id.Apis.ContainsKey(name))
                return id.Apis[name];

            return null;
        }

        #endregion

        #region XML Serialization

        public string GetXMLState(UUID itemID)
        {
            ScriptData data = GetScriptByItemID(itemID);
            if(data == null)
                return "";
            return ScriptDataXMLSerializer.GetXMLState(data, this);
        }

        public bool SetXMLState(UUID itemID, string xml)
        {
            ScriptData data = GetScriptByItemID(itemID);
            if (data == null)
                return false;
            ScriptDataXMLSerializer.SetXMLState(xml, data, this);
            return true;
        }
        #endregion

        /// <summary>
        /// Posts event to all objects in the group.
        /// </summary>
        /// <param name="localID">Region object ID</param>
        /// <param name="FunctionName">Name of the function, will be state + "_event_" + FunctionName</param>
        /// <param name="param">Array of parameters to match event mask</param>
        public bool AddToObjectQueue(UUID partID, string FunctionName, DetectParams[] qParams, int VersionID, params object[] param)
        {
            // Determine all scripts in Object and add to their queue
            ScriptData[] datas = ScriptProtection.GetScripts(partID);

            if (datas == null)
                //No scripts to post to... so it is firing all the events it needs to
                return true;

            foreach (ScriptData ID in datas)
            {
                if (VersionID == -1)
                    VersionID = ID.VersionID;
                // Add to each script in that object
                AddToScriptQueue(ID, FunctionName, qParams, VersionID, EventPriority.FirstStart, param);
            }
            return true;
        }

        /// <summary>
        /// Posts the event to the given object.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="FunctionName"></param>
        /// <param name="qParams"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool AddToScriptQueue(ScriptData ID, string FunctionName, DetectParams[] qParams, int VersionID, EventPriority priority, params object[] param)
        {
            #region Checks

            //Disabled or not running scripts dont get events fired as well as events that should be removed
            if (ID.Disabled || !ID.Running)
                return true;

            int Version = 0;
            if (NeedsRemoved.TryGetValue(ID.ItemID, out Version))
            {
                if (Version >= VersionID)
                    return true;
            }

            if (!ID.World.PipeEventsForScript(
                ID.part))
                return true;

            #endregion
            
            // Create a structure and add data
            QueueItemStruct QIS = new QueueItemStruct();
            QIS.ID = ID;
            QIS.functionName = FunctionName;
            QIS.llDetectParams = qParams;
            QIS.param = param;
            QIS.VersionID = VersionID;

            if (FunctionName == "timer")
            {
                if (ID.TimerQueued)
                    return true;
                ID.TimerQueued = true;
            }
            else if (FunctionName == "control")
            {
                int held = ((LSL_Types.LSLInteger)param[1]).value;
                // int changed = ((LSL_Types.LSLInteger)data.Params[2]).value;

                // If the last message was a 0 (nothing held)
                // and this one is also nothing held, drop it
                //
                if (ID.LastControlLevel == held && held == 0)
                    return true;

                // If there is one or more queued, then queue
                // only changed ones, else queue unconditionally
                //
                if (ID.ControlEventsInQueue > 0)
                {
                    if (ID.LastControlLevel == held)
                        return true;
                }
            }
            else if (FunctionName == "collision")
            {
                if (ID.CollisionInQueue)
                    return true;
                if (qParams == null)
                    return true;

                ID.CollisionInQueue = true;
            }
            else if (FunctionName == "touch")
            {
                if (ID.TouchInQueue)
                    return true;
                if (qParams == null)
                    return true;

                ID.TouchInQueue = true;
            }
            else if (FunctionName == "land_collision")
            {
                if (ID.LandCollisionInQueue)
                    return true;
                if (qParams == null)
                    return true;

                ID.LandCollisionInQueue = true;
            }
            else if (FunctionName == "changed")
            {
                Changed changed = (Changed)(((LSL_Types.LSLInteger)param[0]).value);
                if (QIS.ID.ChangedInQueue.Contains(changed))
                    return true;
                QIS.ID.ChangedInQueue.Add(changed);
            }
            EventPerformanceTestQueue.Add(QIS, priority);
            if (!m_MaintenanceThread.EventProcessorIsRunning)
                m_MaintenanceThread.StartThread("Event");
            return true;
        }

        /// <summary>
        /// Adds the given item to the queue.
        /// </summary>
        /// <param name="ID">InstanceData that needs to be state saved</param>
        /// <param name="create">true: create a new state. false: remove the state.</param>
        public void AddToStateSaverQueue(ScriptData ID, bool create)
        {
            StateQueueItem SQ = new StateQueueItem();
            SQ.ID = ID;
            SQ.Create = create;
            StateQueue.Enqueue(SQ);
            if (!m_MaintenanceThread.StateSaveIsRunning)
                m_MaintenanceThread.StartThread("State");
        }

        public ArrayList GetScriptErrors(UUID itemID)
        {
            return new ArrayList(GetErrors(itemID));
        }

        public Dictionary<UUID, string[]> Errors = new Dictionary<UUID, string[]>();
        /// <summary>
        /// Gets compile errors for the given itemID.
        /// </summary>
        /// <param name="ItemID"></param>
        /// <returns></returns>
        public string[] GetErrors(UUID ItemID)
        {
            while (!Errors.ContainsKey(ItemID))
            {
                Thread.Sleep(250);
            }
            lock (Errors)
            {
                string[] Error = Errors[ItemID];
                Errors.Remove(ItemID);
                if (Error[0] == "SUCCESSFULL")
                    return new string[0];
                return Error;
            }
        }

        /// <summary>
        /// Fetches, loads and hooks up a script to an objects events
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="localID"></param>
        public LUStruct StartScript(SceneObjectPart part, UUID itemID, string Script, int startParam, bool postOnRez, StateSource statesource, UUID RezzedFrom)
        {
            ScriptData id = null;
            ScriptData findID = GetScript(part.UUID, itemID);
            
            LUStruct ls = new LUStruct();
            //Its a change of the script source, needs to be recompiled and such.
            if (findID != null)
            {
                //Ignore prims that have crossed regions, they are already started and working
                if ((statesource & StateSource.PrimCrossing) != 0)
                {
                    //Post the changed event though
                    AddToScriptQueue(id, "changed", new DetectParams[0], id.VersionID, EventPriority.FirstStart, new Object[] { new LSL_Types.LSLInteger(512) });
                    return null;
                }
                else
                {
                    //Restart other scripts
                    ls.Action = LUType.Load;
                }
            }
            else
                ls.Action = LUType.Load;
            if (id == null)
                id = new ScriptData(this);
            id.ItemID = itemID;
            id.PostOnRez = postOnRez;
            id.StartParam = startParam;
            id.stateSource = statesource;
            id.State = "default";
            id.Running = true;
            id.Disabled = false;
            id.Source = Script;
            id.part = part;
            id.World = part.ParentGroup.Scene;
            id.RezzedFrom = RezzedFrom;
            ScriptProtection.RemovePreviouslyCompiled(id.Source);
            ls.ID = id;
            return ls;
        }

        public void UpdateScript(UUID partID, UUID itemID, string script, int startParam, bool postOnRez, int stateSource)
        {
            ScriptData id = null;
            id = GetScript(partID, itemID);
            //Its a change of the script source, needs to be recompiled and such.
            if (id != null)
            {
                LUStruct ls = new LUStruct();
                //Its a change of the script source, needs to be recompiled and such.
                ls.Action = LUType.Reupload;
                id.PostOnRez = postOnRez;
                id.StartParam = startParam;
                id.stateSource = (StateSource)stateSource;
                id.State = "default";
                id.Running = true;
                id.Disabled = false;
                id.Source = script;
                id.World = findPrimsScene(partID);
                id.part = findPrimsScene(partID).GetSceneObjectPart(partID);

                //No SOP, no compile.
                if (id.part == null)
                {
                    m_log.ErrorFormat("[{0}]: Could not find scene object part corresponding " + "to localID {1} to start script", ScriptEngineName, partID);
                    return;
                }
                ls.ID = id;
                AddScriptChange(new LUStruct[] { ls }, LoadPriority.Restart);
            }
            else
            {
                LUStruct ls = new LUStruct();
                //Its a change of the script source, needs to be recompiled and such.
                ls.Action = LUType.Reupload;
                id = new ScriptData(this);
                id.ItemID = itemID;
                id.StartParam = startParam;
                id.stateSource = (StateSource)stateSource;
                id.State = "default";
                id.Running = true;
                id.Disabled = false;
                id.Source = script;
                id.PostOnRez = postOnRez;
                id.World = findPrimsScene(partID);
                id.part = findPrimsScene(partID).GetSceneObjectPart(partID);

                //No SOP, no compile.
                if (id.part == null)
                {
                    m_log.ErrorFormat("[{0}]: Could not find scene object part corresponding " + "to localID {1} to start script", ScriptEngineName, partID);
                    return;
                }
                ls.ID = id;
                AddScriptChange(new LUStruct[] { ls }, LoadPriority.Restart);
            }
        }

        public Scene findPrimsScene(UUID objectID)
        {
            foreach (Scene s in m_Scenes)
            {
                SceneObjectPart part = s.GetSceneObjectPart(objectID);
                if (part != null)
                {
                    return s;
                }
            }
            return null;
        }

        public Scene findPrimsScene(uint localID)
        {
            foreach (Scene s in m_Scenes)
            {
                SceneObjectPart part = s.GetSceneObjectPart(localID);
                if (part != null)
                {
                    return s;
                }
            }
            return null;
        }

        /// <summary>
        /// Disables and unloads a script
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="itemID"></param>
        public void StopScript(uint localID, UUID itemID)
        {
            ScriptData data = GetScriptByItemID(itemID);
            if (data == null)
                return;
            LUStruct ls = new LUStruct();
            ScriptProtection.RemovePreviouslyCompiled(data.Source);
            ls.ID = data;
            ls.Action = LUType.Unload;
            NeedsRemoved[data.ItemID] = data.VersionID;
            AddScriptChange(new LUStruct[] { ls }, LoadPriority.Stop);

            ObjectRemoved handlerObjectRemoved = OnObjectRemoved;
            if (handlerObjectRemoved != null)
            {
                handlerObjectRemoved(ls.ID.part.UUID);
            }

            ScriptRemoved handlerScriptRemoved = OnScriptRemoved;
            if (handlerScriptRemoved != null)
                handlerScriptRemoved(itemID);
        }

        /// <summary>
        /// Gets all itemID's of scripts in the given localID.
        /// </summary>
        /// <param name="localID"></param>
        /// <returns></returns>
        public List<UUID> GetScriptKeys(uint localID)
        {
            List<UUID> UUIDs = new List<UUID>();
            foreach (ScriptData ID in ScriptProtection.GetScripts(findPrimsScene(localID).GetSceneObjectPart(localID).UUID) as ScriptData[])
                UUIDs.Add(ID.ItemID);
            return UUIDs;
        }

        /// <summary>
        /// Gets the script by itemID.
        /// </summary>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public ScriptData GetScriptByItemID(UUID itemID)
        {
            return ScriptProtection.GetScript(itemID);
        }

        /// <summary>
        /// Gets the InstanceData by the prims local and itemID.
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public ScriptData GetScript(UUID primID, UUID itemID)
        {
            return ScriptProtection.GetScript(primID, itemID);
        }

        /// <summary>
        /// Updates or adds the given InstanceData to the list of known scripts.
        /// </summary>
        /// <param name="id"></param>
        public void UpdateScriptInstanceData(ScriptData id)
        {
            ScriptProtection.AddNewScript(id);
        }

        /// <summary>
        /// Removes the given InstanceData from all known scripts.
        /// </summary>
        /// <param name="id"></param>
        public void RemoveScript(ScriptData id)
        {
            ScriptProtection.RemovePreviouslyCompiled(id.Source);
            ScriptProtection.RemoveScript(id);
        }

        public string TestCompileScript(UUID assetID, UUID itemID)
        {
            AssetBase asset = m_Scenes[0].AssetService.Get(assetID.ToString());
            if (null == asset)
            {
                return "Could not find script.";
            }
            else
            {
                string script = OpenMetaverse.Utils.BytesToString(asset.Data);
                try
                {
                    string assembly;
                    Compiler.PerformScriptCompile(script, itemID, UUID.Zero, out assembly);
                }
                catch (Exception e)
                {
                    string error = "Error compiling script: " + e;
                    if (error.Length > 255)
                        error = error.Substring(0, 255);
                    return error;
                }
                return "";
            }
        }

        public void AddScriptChange(LUStruct[] Struct, LoadPriority priority)
        {
            LUQueue.Add(Struct, priority);
            if (!m_MaintenanceThread.ScriptChangeIsRunning)
                m_MaintenanceThread.StartThread("Change");
        }

        public void SaveStateSave(UUID itemID)
        {
            ScriptData script = ScriptProtection.GetScript(itemID);
            if(script != null)
                script.SerializeDatabase();
        }

        public void UpdateScriptToNewObject(UUID olditemID, TaskInventoryItem newItem, SceneObjectPart newPart)
        {
            try
            {
                Aurora.DataManager.DataManager.RequestPlugin<IScriptDataConnector>("IScriptDataConnector").DeleteStateSave(olditemID);
                if (newPart.ParentGroup.Scene != null)
                {
                    ScriptData SD = GetScriptByItemID(olditemID);
                    if (SD == null)
                        return;
                    SD.presence = SD.World.GetScenePresence(SD.presence.UUID);
                    ScriptControllers SC = SD.presence.GetScriptControler(SD.ItemID);
                    if ((newItem.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        SD.presence.UnRegisterControlEventsToScript(SD.part.LocalId, SD.ItemID);
                    }

                    SD.part = newPart;
                    SD.ItemID = newItem.ItemID;
                    //Find the asset ID
                    SD.InventoryItem = newItem;
                    //Try to see if this was rezzed from someone's inventory
                    SD.UserInventoryItemID = SD.part.FromUserInventoryItemID;

                    object[] Plugins = GetSerializationData(this, SD.part.ParentGroup.Scene, SD.ItemID, SD.part.UUID);
                    RemoveScript(this, SD.part.ParentGroup.Scene, SD.part.UUID, SD.ItemID);
                    CreateFromData(this, SD.part.ParentGroup.Scene, SD.part.UUID, SD.ItemID, SD.part.UUID, Plugins);
                    SD.World = newPart.ParentGroup.Scene;
                    SD.SetApis();

                    if ((newItem.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        SC.itemID = newItem.ItemID;
                        SC.objID = SD.part.LocalId;
                        SD.presence.RegisterScriptController(SC);
                    }

                    UpdateScriptInstanceData(SD);
                    SD.SerializeDatabase();
                }
            }
            catch
            {
            }
        }

        #region API Manager

        public IScriptApi[] GetAPIs()
        {
            return Aurora.Framework.AuroraModuleLoader.LoadPlugins<IScriptApi>("/OpenSim/ScriptPlugins", new PluginInitialiserBase()).ToArray();
        }

        #endregion

        #region NewAsyncCommandManager

        private Dataserver m_Dataserver;
        private Plugins.Timer m_Timer;
        private Dictionary<Scene, Listener> m_Listeners = new Dictionary<Scene, Listener>();
        private HttpRequest m_HttpRequest;
        private SensorRepeat m_SensorRepeat;
        private XmlRequest m_XmlRequest;

        public Dataserver DataserverPlugin
        {
            get { return m_Dataserver; }
        }

        public Plugins.Timer TimerPlugin
        {
            get { return m_Timer; }
        }

        public HttpRequest HttpRequestPlugin
        {
            get { return m_HttpRequest; }
        }

        public SensorRepeat SensorRepeatPlugin
        {
            get { return m_SensorRepeat; }
        }

        public XmlRequest XmlRequestPlugin
        {
            get { return m_XmlRequest; }
        }

        private void SetUpCommandManager()
        {
            m_HttpRequest = new HttpRequest(this, Worlds[0]);
            m_XmlRequest = new XmlRequest(this, Worlds[0]);
            m_Dataserver = new Dataserver(this);
            m_Timer = new Plugins.Timer(this);
            foreach (Scene scene in Worlds)
            {
                IWorldComm comm = scene.RequestModuleInterface<IWorldComm>();
                m_Listeners[scene] = new Listener(this, comm);
            }
            m_SensorRepeat = new SensorRepeat(this);
        }

        public void DoOneCmdHandlerPass()
        {
            if (m_HttpRequest == null)
                return;
            // Check HttpRequests
            m_HttpRequest.CheckHttpRequests();

            // Check XMLRPCRequests
            m_XmlRequest.CheckXMLRPCRequests();

            foreach (Listener listener in m_Listeners.Values)
            {
                // Check Listeners
                listener.CheckListeners();
            }

            // Check timers
            m_Timer.CheckTimerEvents();

            // Check Sensors
            m_SensorRepeat.CheckSenseRepeaterEvents();

            // Check dataserver
            m_Dataserver.CheckAndExpireRequests();
        }

        /// <summary>
        /// Remove a specific script (and all its pending commands)
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="itemID"></param>
        public void RemoveScript(ScriptEngine engine, Scene scene, UUID primID, UUID itemID)
        {
            // Remove a specific script

            // Remove dataserver events
            m_Dataserver.RemoveEvents(primID, itemID);

            // Remove from: Timers
            m_Timer.UnSetTimerEvents(primID, itemID);

            // Remove from: HttpRequest
            IHttpRequestModule iHttpReq =
                scene.RequestModuleInterface<IHttpRequestModule>();
            iHttpReq.StopHttpRequest(primID, itemID);

            IWorldComm comms = scene.RequestModuleInterface<IWorldComm>();
            if (comms != null)
                comms.DeleteListener(itemID);

            IXMLRPC xmlrpc = scene.RequestModuleInterface<IXMLRPC>();
            xmlrpc.DeleteChannels(itemID);
            xmlrpc.CancelSRDRequests(itemID);

            // Remove Sensors
            m_SensorRepeat.UnSetSenseRepeaterEvents(primID, itemID);

        }

        public Object[] GetSerializationData(ScriptEngine engine, Scene scene, UUID itemID, UUID primID)
        {
            List<Object> data = new List<Object>();

            Object[] listeners = m_Listeners[scene].GetSerializationData(itemID);
            if (listeners.Length > 0)
            {
                data.Add("listener");
                data.Add(listeners.Length);
                data.AddRange(listeners);
            }

            Object[] timers = m_Timer.GetSerializationData(itemID, primID);
            if (timers.Length > 0)
            {
                data.Add("timer");
                data.Add(timers.Length);
                data.AddRange(timers);
            }

            Object[] sensors = m_SensorRepeat.GetSerializationData(itemID);
            if (sensors.Length > 0)
            {
                data.Add("sensor");
                data.Add(sensors.Length);
                data.AddRange(sensors);
            }

            return data.ToArray();
        }

        public void CreateFromData(ScriptEngine engine, Scene scene, UUID primID,
                UUID itemID, UUID hostID, Object[] data)
        {
            int idx = 0;
            int len;

            while (idx < data.Length - 1)
            {
                string type = data[idx].ToString();
                len = Convert.ToInt32(data[idx + 1]);
                idx += 2;

                if (len > 0)
                {
                    Object[] item = new Object[len];
                    Array.Copy(data, idx, item, 0, len);

                    idx += len;

                    switch (type)
                    {
                        case "listener":
                            m_Listeners[scene].CreateFromData(itemID,
                                                        hostID, item);
                            break;
                        case "timer":
                            m_Timer.CreateFromData(itemID,
                                                        hostID, item);
                            break;
                        case "sensor":
                            m_SensorRepeat.CreateFromData(itemID, hostID, item);
                            break;
                    }
                }
            }
        }

        #endregion
    }
    /// <summary>
    /// Queue item structure
    /// </summary>
    public class QueueItemStruct
    {
        public ScriptData ID;
        public string functionName;
        public DetectParams[] llDetectParams;
        public object[] param;
        public Guid CurrentlyAt = Guid.Empty;
        public int VersionID = 0;
    }
    public class StateQueueItem
    {
        public ScriptData ID;
        public bool Create;
    }
    // Load/Unload structure
    public class LUStruct
    {
        public ScriptData ID;
        public LUType Action;
    }

    public enum LUType
    {
        Unknown = 0,
        Load = 1,
        Unload = 2,
        Reupload = 3
    }

    public class ScriptDataXMLSerializer
    {
        public static string GetXMLState(ScriptData instance, ScriptEngine engine)
        {
            //Update PluginData
            instance.PluginData = engine.GetSerializationData(engine, instance.World, instance.ItemID, instance.part.UUID);

            bool running = instance.Running;

            XmlDocument xmldoc = new XmlDocument();

            XmlNode xmlnode = xmldoc.CreateNode(XmlNodeType.XmlDeclaration,
                                                "", "");
            xmldoc.AppendChild(xmlnode);

            XmlElement rootElement = xmldoc.CreateElement("", "ScriptState",
                                                          "");
            xmldoc.AppendChild(rootElement);

            XmlElement state = xmldoc.CreateElement("", "State", "");
            state.AppendChild(xmldoc.CreateTextNode(instance.State));

            rootElement.AppendChild(state);

            XmlElement run = xmldoc.CreateElement("", "Running", "");
            run.AppendChild(xmldoc.CreateTextNode(
                    running.ToString()));

            rootElement.AppendChild(run);

            Dictionary<string, Object> vars = instance.Script.GetVars();

            XmlElement variables = xmldoc.CreateElement("", "Variables", "");

            foreach (KeyValuePair<string, Object> var in vars)
                WriteTypedValue(xmldoc, variables, "Variable", var.Key,
                                var.Value);

            rootElement.AppendChild(variables);

            #region Queue

            //We don't do queue...
            XmlElement queue = xmldoc.CreateElement("", "Queue", "");
            rootElement.AppendChild(queue);

            #endregion

            XmlNode plugins = xmldoc.CreateElement("", "Plugins", "");
            DumpList(xmldoc, plugins,
                     new LSL_Types.list(instance.PluginData));

            rootElement.AppendChild(plugins);

            if (instance.InventoryItem != null)
            {
                if (instance.InventoryItem.PermsMask != 0 && instance.InventoryItem.PermsGranter != UUID.Zero)
                {
                    XmlNode permissions = xmldoc.CreateElement("", "Permissions", "");
                    XmlAttribute granter = xmldoc.CreateAttribute("", "granter", "");
                    granter.Value = instance.InventoryItem.PermsGranter.ToString();
                    permissions.Attributes.Append(granter);
                    XmlAttribute mask = xmldoc.CreateAttribute("", "mask", "");
                    mask.Value = instance.InventoryItem.PermsMask.ToString();
                    permissions.Attributes.Append(mask);
                    rootElement.AppendChild(permissions);
                }
            }

            if (instance.EventDelayTicks > 0.0)
            {
                XmlElement eventDelay = xmldoc.CreateElement("", "MinEventDelay", "");
                eventDelay.AppendChild(xmldoc.CreateTextNode(instance.EventDelayTicks.ToString()));
                rootElement.AppendChild(eventDelay);
            }

            Type type = instance.Script.GetType();
            FieldInfo[] mi = type.GetFields();
            string xml = xmldoc.InnerXml;

            XmlDocument sdoc = new XmlDocument();
            sdoc.LoadXml(xml);
            XmlNodeList rootL = sdoc.GetElementsByTagName("ScriptState");
            XmlNode rootNode = rootL[0];

            // Create <State UUID="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx">
            XmlDocument doc = new XmlDocument();
            XmlElement stateData = doc.CreateElement("", "State", "");
            XmlAttribute stateID = doc.CreateAttribute("", "UUID", "");
            stateID.Value = instance.ItemID.ToString();
            stateData.Attributes.Append(stateID);
            XmlAttribute assetID = doc.CreateAttribute("", "Asset", "");
            assetID.Value = instance.InventoryItem.AssetID.ToString();
            stateData.Attributes.Append(assetID);
            XmlAttribute engineName = doc.CreateAttribute("", "Engine", "");
            engineName.Value = engine.ScriptEngineName;
            stateData.Attributes.Append(engineName);
            doc.AppendChild(stateData);

            // Add <ScriptState>...</ScriptState>
            XmlNode xmlstate = doc.ImportNode(rootNode, true);
            stateData.AppendChild(xmlstate);

            string assemName = instance.AssemblyName;

            XmlElement assemblyData = doc.CreateElement("", "Assembly", "");
            XmlAttribute assemblyName = doc.CreateAttribute("", "Filename", "");

            assemblyName.Value = assemName;
            assemblyData.Attributes.Append(assemblyName);

            assemblyData.InnerText = assemName;

            stateData.AppendChild(assemblyData);

            XmlElement mapData = doc.CreateElement("", "LineMap", "");
            XmlAttribute mapName = doc.CreateAttribute("", "Filename", "");

            mapName.Value = assemName + ".map";
            mapData.Attributes.Append(mapName);

            mapData.InnerText = assemName;

            stateData.AppendChild(mapData);

            return doc.InnerXml;
        }

        public static void SetXMLState(string xml, ScriptData instance, ScriptEngine engine)
        {
            XmlDocument doc = new XmlDocument();

            Dictionary<string, object> vars = instance.Script.GetVars();

            doc.LoadXml(xml);

            XmlNodeList rootL = doc.GetElementsByTagName("ScriptState");
            if (rootL.Count != 1)
            {
                return;
            }
            XmlNode rootNode = rootL[0];

            if (rootNode != null)
            {
                object varValue;
                XmlNodeList partL = rootNode.ChildNodes;

                foreach (XmlNode part in partL)
                {
                    switch (part.Name)
                    {
                        case "State":
                            instance.State = part.InnerText;
                            break;
                        case "Running":
                            instance.Running = bool.Parse(part.InnerText);
                            break;
                        case "Variables":
                            XmlNodeList varL = part.ChildNodes;
                            foreach (XmlNode var in varL)
                            {
                                string varName;
                                varValue = ReadTypedValue(var, out varName);

                                if (vars.ContainsKey(varName))
                                    vars[varName] = varValue;
                            }
                            instance.Script.SetVars(vars);
                            break;
                        case "Plugins":
                            instance.PluginData = ReadList(part).Data;
                            break;
                        case "Permissions":
                            string tmpPerm;
                            int mask = 0;
                            tmpPerm = part.Attributes.GetNamedItem("mask").Value;
                            if (tmpPerm != null)
                            {
                                int.TryParse(tmpPerm, out mask);
                                if (mask != 0)
                                {
                                    tmpPerm = part.Attributes.GetNamedItem("granter").Value;
                                    if (tmpPerm != null)
                                    {
                                        UUID granter = new UUID();
                                        UUID.TryParse(tmpPerm, out granter);
                                        if (granter != UUID.Zero)
                                        {
                                            instance.InventoryItem.PermsMask = mask;
                                            instance.InventoryItem.PermsGranter = granter;
                                        }
                                    }
                                }
                            }
                            break;
                        case "MinEventDelay":
                            double minEventDelay = 0.0;
                            double.TryParse(part.InnerText, NumberStyles.Float, Culture.NumberFormatInfo, out minEventDelay);
                            instance.EventDelayTicks = (long)minEventDelay;
                            break;
                    }
                }
            }
        }

        #region Helpers

        private static LSL_Types.list ReadList(XmlNode parent)
        {
            List<Object> olist = new List<Object>();

            XmlNodeList itemL = parent.ChildNodes;
            foreach (XmlNode item in itemL)
                olist.Add(ReadTypedValue(item));

            return new LSL_Types.list(olist.ToArray());
        }

        private static object ReadTypedValue(XmlNode tag, out string name)
        {
            name = tag.Attributes.GetNamedItem("name").Value;

            return ReadTypedValue(tag);
        }

        private static object ReadTypedValue(XmlNode tag)
        {
            Object varValue;
            string assembly;

            string itemType = tag.Attributes.GetNamedItem("type").Value;

            if (itemType == "list")
                return ReadList(tag);

            if (itemType == "OpenMetaverse.UUID")
            {
                UUID val = new UUID();
                UUID.TryParse(tag.InnerText, out val);

                return val;
            }

            Type itemT = Type.GetType(itemType);
            if (itemT == null)
            {
                Object[] args =
                    new Object[] { tag.InnerText };

                assembly = itemType + ", Aurora.ScriptEngine.AuroraDotNetEngine";
                itemT = Type.GetType(assembly);
                if (itemT == null)
                    return null;

                varValue = Activator.CreateInstance(itemT, args);

                if (varValue == null)
                    return null;
            }
            else
            {
                varValue = Convert.ChangeType(tag.InnerText, itemT);
            }
            return varValue;
        }

        private static void DumpList(XmlDocument doc, XmlNode parent,
                LSL_Types.list l)
        {
            foreach (Object o in l.Data)
                WriteTypedValue(doc, parent, "ListItem", "", o);
        }

        private static void WriteTypedValue(XmlDocument doc, XmlNode parent,
                string tag, string name, object value)
        {
            Type t = value.GetType();
            XmlAttribute typ = doc.CreateAttribute("", "type", "");
            XmlNode n = doc.CreateElement("", tag, "");

            if (value is LSL_Types.list)
            {
                typ.Value = "list";
                n.Attributes.Append(typ);

                DumpList(doc, n, (LSL_Types.list)value);

                if (name != String.Empty)
                {
                    XmlAttribute nam = doc.CreateAttribute("", "name", "");
                    nam.Value = name;
                    n.Attributes.Append(nam);
                }

                parent.AppendChild(n);
                return;
            }

            n.AppendChild(doc.CreateTextNode(value.ToString()));

            typ.Value = t.ToString();
            n.Attributes.Append(typ);
            if (name != String.Empty)
            {
                XmlAttribute nam = doc.CreateAttribute("", "name", "");
                nam.Value = name;
                n.Attributes.Append(nam);
            }

            parent.AppendChild(n);
        }

        #endregion
    }
}
