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
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.ScriptEngine.Shared;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;
using OpenSim.Region.ScriptEngine.Shared.CodeTools;
using OpenSim.Region.ScriptEngine.Shared.Api;
using OpenSim.Framework.Console;
using Amib.Threading;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    [Serializable]
    public class ScriptEngine : ISharedRegionModule, IScriptEngine, IScriptModule
    {
        #region Declares 

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public List<IScene> Worlds
        {
            get { return m_Scenes.ConvertAll<IScene>(ConvertToIScene); }
        }

        public static IScene ConvertToIScene(Scene scene)
        {
            return scene;
        }

        private List<Scene> m_Scenes = new List<Scene>();

        // Handles and queues incoming events from OpenSim
        public EventManager EventManager;

        // Handles loading/unloading of scripts into AppDomains
        public AppDomainManager AppDomainManager;

        //The compiler for all scripts
        public Compiler LSLCompiler;

        //Queue that handles the loading and unloading of scripts
        public OpenSim.Framework.LockFreeQueue<LUStruct[]> LUQueue = new OpenSim.Framework.LockFreeQueue<LUStruct[]>();

        public MaintenanceThread m_MaintenanceThread;

        private IConfigSource m_ConfigSource;
        public IConfig ScriptConfigSource;
        private bool m_enabled = false;
        public bool DisplayErrorsOnConsole = false;

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
        private IXmlRpcRouter m_XmlRpcRouter;
        public static IScriptProtectionModule ScriptProtection;
        
        /// <summary>
        /// Removes the script from the event queue so it does not fire anymore events.
        /// </summary>
        public static List<UUID> NeedsRemoved = new List<UUID>();

        public enum EventPriority : int
        {
            FirstStart = 0,
            Suspended = 1,
            Continued = 2
        }

        public class Priority : OpenSim.Framework.IPriorityConverter<EventPriority>
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
        public static OpenSim.Framework.LimitedPriorityQueue<QueueItemStruct, EventPriority> EventQueue = new OpenSim.Framework.LimitedPriorityQueue<QueueItemStruct, EventPriority>(new Priority());

        /// <summary>
        /// Queue containing scripts that need to have states saved or deleted.
        /// </summary>
        public static OpenSim.Framework.LockFreeQueue<StateQueueItem> StateQueue = new OpenSim.Framework.LockFreeQueue<StateQueueItem>();
        
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

        public bool FirstStartup = true;

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
            {
                scene.AddCommand(this, "DotNet restart all scripts", "DotNet restart all scripts", "Restarts all scripts in the sim", RestartAllScripts);
                scene.AddCommand(this, "DotNet stop all scripts", "DotNet stop all scripts", "Stops all scripts in the sim", StopAllScripts);
                scene.AddCommand(this, "DotNet start all scripts", "DotNet start all scripts", "Restarts all scripts in the sim", StartAllScripts);
            }
            FirstStartup = false;

            ScriptConfigSource = ConfigSource.Configs[ScriptEngineName];

        	//m_log.Info("[" + ScriptEngineName + "]: ScriptEngine initializing");
            m_Scenes.Add(scene);

            // Create all objects we'll be using
            if(ScriptProtection == null)
                ScriptProtection = (IScriptProtectionModule)new ScriptProtectionModule(m_ConfigSource, this);
            
            EventManager = new EventManager(this, true);
            
            // We need to start it
            if (LSLCompiler == null)
                LSLCompiler = new Compiler(this);

            if(AppDomainManager == null)
                AppDomainManager = new AppDomainManager(this);
            
            if(m_MaintenanceThread == null)
                m_MaintenanceThread = new MaintenanceThread(this);

            
            scene.StackModuleInterface<IScriptModule>(this);

            m_XmlRpcRouter = m_Scenes[0].RequestModuleInterface<IXmlRpcRouter>();
            if (m_XmlRpcRouter != null)
            {
                OnScriptRemoved += m_XmlRpcRouter.ScriptRemoved;
                OnObjectRemoved += m_XmlRpcRouter.ObjectRemoved;
            }

            scene.EventManager.OnRezScript += OnRezScript;
            scene.EventManager.OnRezScripts += OnRezScripts;
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_enabled)
                return;

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

            if (m_XmlRpcRouter != null)
            {
                OnScriptRemoved -= m_XmlRpcRouter.ScriptRemoved;
                OnObjectRemoved -= m_XmlRpcRouter.ObjectRemoved;
            }

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

        protected void RestartAllScripts(string module, string[] cmdparams)
        {
            string go = MainConsole.Instance.CmdPrompt("Are you sure you want to restart all scripts? (This also wipes the script state saves database, which could cause loss of information in your scripts)", "no");
            if (go == "yes" || go == "Yes")
            {
                foreach (ScriptData ID in ScriptProtection.GetAllScripts())
                {
                    try
                    {
                        ScriptProtection.RemovePreviouslyCompiled(ID.Source);
                        ID.Start(false);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            else
            {
                m_log.Info("Not restarting all scripts");
            }
        }

        protected void StartAllScripts(string module, string[] cmdparams)
        {
            string go = MainConsole.Instance.CmdPrompt("Are you sure you want to restart all scripts?", "no");
            if (go == "yes" || go == "Yes")
            {
                foreach (ScriptData ID in ScriptProtection.GetAllScripts())
                {
                    try
                    {
                        ID.Start(true);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            else
            {
                m_log.Info("Not restarting all scripts");
            }
        }

        protected void StopAllScripts(string module, string[] cmdparams)
        {
            string go = MainConsole.Instance.CmdPrompt("Are you sure you want to stop all scripts?", "no");
            if (go.Contains("yes") || go.Contains("Yes"))
            {
                StopAllScripts();
            }
            else
            {
                m_log.Info("Not restarting all scripts");
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
                    ILease lease = (ILease)RemotingServices.GetLifetimeService(script.Script as ScriptBaseClass);
                    lease.Renew(DateTime.Now.AddMinutes(10) - DateTime.Now);
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
                    startParam, postOnRez, (StateSource)stateSource);
            if(itemToQueue != null)
                LUQueue.Enqueue(new LUStruct[]{itemToQueue});
        }

        public void OnRezScripts(SceneObjectPart part, TaskInventoryItem[] items,
                int startParam, bool postOnRez, string engine, int stateSource)
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
                string script = Utils.BytesToString(asset.Data);

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
                string script = Utils.BytesToString(asset.Data);

                LUStruct itemToQueue = StartScript(part, item.ItemID, script,
                        startParam, postOnRez, (StateSource)stateSource);
                if (itemToQueue != null)
                    ItemsToQueue.Add(itemToQueue);
            }
            LUQueue.Enqueue(ItemsToQueue.ToArray());

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
                    p.DetectParams, p.Params);
        }

        public bool PostScriptEvent(UUID itemID, EventParams p)
        {
            ScriptData ID = GetScriptByItemID(itemID);
            return AddToScriptQueue(ID,
                    p.EventName, p.DetectParams, p.Params);
        }

        public bool PostScriptEvent(UUID itemID, string name, Object[] p)
        {
            Object[] lsl_p = new Object[p.Length];
            for (int i = 0; i < p.Length ; i++)
            {
                if (p[i] is int)
                    lsl_p[i] = new LSL_Types.LSLInteger((int)p[i]);
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

            return PostScriptEvent(itemID, new EventParams(name, lsl_p, new DetectParams[0]));
        }

        public bool PostObjectEvent(UUID itemID, string name, Object[] p)
        {
            SceneObjectPart part = findPrimsScene(itemID).GetSceneObjectPart(itemID);
            if (part == null)
                return false;

            Object[] lsl_p = new Object[p.Length];
            for (int i = 0; i < p.Length ; i++)
            {
                if (p[i] is int)
                    lsl_p[i] = new LSL_Types.LSLInteger((int)p[i]);
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

        public DetectParams GetDetectParams(UUID itemID, int number)
        {
            ScriptData id = GetScriptByItemID(itemID);

            if (id == null)
                return null;

            DetectParams[] det = id.LastDetectParams;

            if (number < 0 || number >= det.Length)
                return null;

            return det[number];
        }

        #endregion
        
        #region Get/Set Start Parameter and Min Event Delay
        
        public int GetStartParameter(UUID itemID)
        {
            ScriptData id = GetScriptByItemID(itemID);

            if (id == null)
                return 0;

            return id.StartParam;
        }

        public void SetMinEventDelay(UUID itemID, double delay)
        {
            ScriptData ID = GetScriptByItemID(itemID);
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
                    new DetectParams[0], new object[0] { });
                id.State = state;
                int eventFlags = id.Script.GetStateEventFlags(id.State);

                id.part.SetScriptEvents(itemID, eventFlags);
                UpdateScriptInstanceData(id);

                AddToObjectQueue(id.part.UUID, "state_entry",
                    new DetectParams[0], new object[0] { });
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
            ScriptData ID = (ScriptData)ScriptProtection.GetScript(itemID);
            if (ID == null)
                return;

            ID.Reset();
        }
        #endregion

        #region Start/End/Suspend Scripts

        public void OnStartScript(uint localID, UUID itemID)
        {
            ScriptData id = (ScriptData)ScriptProtection.GetScript(itemID);
            if (id == null)
                return;        

            if (!id.Disabled)
                id.Running = true;

            LUStruct item = StartScript(id.part, itemID, id.Source, id.StartParam, true, id.stateSource);
            if (item != null)
                LUQueue.Enqueue(new LUStruct[] { item });
        }

        public void OnStopScript(uint localID, UUID itemID)
        {
            ScriptData ID = (ScriptData)ScriptProtection.GetScript(itemID);
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

        #region xEngine only

        /// <summary>
        /// Unneeded for DotNet. Only for xEngine.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public IScriptWorkItem QueueEventHandler(Object o)
        {
            return null;
        }

        #endregion

        #region XML Serialization

        public string GetXMLState(UUID itemID)
        {
            return "";
        }

        public bool SetXMLState(UUID itemID, string xml)
        {
            return true;
        }
        #endregion

        /// <summary>
        /// Posts event to all objects in the group.
        /// </summary>
        /// <param name="localID">Region object ID</param>
        /// <param name="FunctionName">Name of the function, will be state + "_event_" + FunctionName</param>
        /// <param name="param">Array of parameters to match event mask</param>
        public bool AddToObjectQueue(UUID partID, string FunctionName, DetectParams[] qParams, params object[] param)
        {
            // Determine all scripts in Object and add to their queue
            IScriptData[] datas = ScriptProtection.GetScripts(partID);

            if (datas == null)
                //No scripts to post to... so it is firing all the events it needs to
                return true;

            foreach (IScriptData ID in datas)
            {
                // Add to each script in that object
                AddToScriptQueue((ScriptData)ID, FunctionName, qParams, param);
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
        public bool AddToScriptQueue(ScriptData ID, string FunctionName, DetectParams[] qParams, params object[] param)
        {
            #region Should be fired checks

            if (ID == null)
                return false;

            //Disabled or not running scripts dont get events fired as well as events that should be removed
            if (ID.Disabled || !ID.Running || NeedsRemoved.Contains(ID.part.UUID))
                return true;

            if (!ID.World.PipeEventsForScript(
                ID.part.LocalId))
                return true;

            #endregion
            
            // Create a structure and add data
            QueueItemStruct QIS = new QueueItemStruct();
            QIS.ID = ID;
            QIS.functionName = FunctionName;
            QIS.llDetectParams = qParams;
            QIS.param = param;

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
                Changed changed = (Changed)(new LSL_Types.LSLInteger(param[0].ToString()).value);
                if (QIS.ID.ChangedInQueue.Contains(changed))
                    return true;
                QIS.ID.ChangedInQueue.Add(changed);
            }
            else if (FunctionName == "link_message")
            {
            }
            else
            {
                AuroraDotNetEngine.EventQueue.ProcessQIS(QIS);
                return true;
            }

            EventQueue.Enqueue(QIS, EventPriority.FirstStart);
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
        public LUStruct StartScript(SceneObjectPart part, UUID itemID, string Script, int startParam, bool postOnRez, StateSource statesource)
        {
            ScriptData id = null;
            id = GetScript(part.UUID, itemID);
            
            LUStruct ls = new LUStruct();
            //Its a change of the script source, needs to be recompiled and such.
            if (id != null)
            {
                //Ignore prims that have crossed regions, they are already started and working
                if ((statesource & StateSource.PrimCrossing) != 0)
                {
                    //Post the changed event though
                    AddToScriptQueue(id, "changed", new DetectParams[0], new Object[] { new LSL_Types.LSLInteger(512) });
                    return null;
                }
                else
                {
                    //Restart other scripts
                    id = new ScriptData(this);
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
                LUQueue.Enqueue(new LUStruct[] { ls });
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
                LUQueue.Enqueue(new LUStruct[] { ls });
            }
        }

        public Scene findPrimsScene(UUID objectID)
        {
            lock (m_Scenes)
            {
                foreach (Scene s in m_Scenes)
                {
                    SceneObjectPart part = s.GetSceneObjectPart(objectID);
                    if (part != null)
                    {
                        return s;
                    }
                }
            }
            return null;
        }

        public Scene findPrimsScene(uint localID)
        {
            lock (m_Scenes)
            {
                foreach (Scene s in m_Scenes)
                {
                    SceneObjectPart part = s.GetSceneObjectPart(localID);
                    if (part != null)
                    {
                        return s;
                    }
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
            NeedsRemoved.Add(data.part.UUID);
            LUQueue.Enqueue(new LUStruct[]{ls});
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
            return (ScriptData)ScriptProtection.GetScript(itemID);
        }

        /// <summary>
        /// Gets the InstanceData by the prims local and itemID.
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public ScriptData GetScript(UUID primID, UUID itemID)
        {
            return (ScriptData)ScriptProtection.GetScript(primID, itemID);
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
                string script = Utils.BytesToString(asset.Data);
                try
                {
                    string assembly;
                    LSLCompiler.PerformScriptCompile(script, itemID, UUID.Zero, out assembly);
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

        public void SaveStateSave(UUID itemID)
        {
            IScriptData script = ScriptProtection.GetScript(itemID);
            if(script != null)
                ((ScriptData)script).SerializeDatabase();
        }

        public void UpdateScriptToNewObject(UUID olditemID, TaskInventoryItem newItem, SceneObjectPart newPart)
        {
            try
            {
                Aurora.DataManager.DataManager.IScriptDataConnector.DeleteStateSave(olditemID);
                if (newPart.ParentGroup.Scene != null)
                {
                    ScriptData SD = GetScriptByItemID(olditemID);
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

                    object[] Plugins = AsyncCommandManager.GetSerializationData(this, SD.part.ParentGroup.Scene, SD.ItemID);
                    AsyncCommandManager.RemoveScript(this, SD.part.ParentGroup.Scene, SD.part.LocalId, SD.ItemID);
                    AsyncCommandManager.CreateFromData(this, SD.part.ParentGroup.Scene, SD.part.LocalId, SD.ItemID, SD.part.UUID, Plugins);
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
        public Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>>
                LineMap;
        public OpenMetaverse.UUID CurrentlyAt = OpenMetaverse.UUID.Zero;
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
}
