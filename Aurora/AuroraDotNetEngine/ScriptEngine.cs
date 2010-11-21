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
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    [Serializable]
    public class ScriptEngine : ISharedRegionModule, IScriptModule, ICommandableModule
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public List<Scene> Worlds
        {
            get { return m_Scenes; }
        }

        private readonly Commander m_commander = new Commander("ADNE");

        private List<Scene> m_Scenes = new List<Scene>();

        // Handles and queues incoming events from OpenSim
        public EventManager EventManager;

        // Handles loading/unloading of scripts into AppDomains
        public AppDomainManager AppDomainManager;

        //The compiler for all scripts
        public Compiler Compiler;
        
        //Handles the queues
        public MaintenanceThread MaintenanceThread;

        //Handles script errors
        public ScriptErrorReporter ScriptErrorReporter;

        //Handles checking of threat levels and if scripts have been compiled before
        public static ScriptProtectionModule ScriptProtection;

        public AssemblyResolver AssemblyResolver;

        private IConfigSource m_ConfigSource;

        public IConfig ScriptConfigSource;

        private bool m_enabled = false;

        public bool DisplayErrorsOnConsole = false;

        public bool ShowWarnings = false;

        private bool m_consoleDisabled = false;
        private bool m_disabled = false;

        /// <summary>
        /// Disabled from the command line, takes presidence over normal Disabled
        /// </summary>
        public bool ConsoleDisabled
        {
            get { return m_consoleDisabled; }
            set 
            { 
                m_consoleDisabled = value; 
                //Poke the threads to make sure they run
                MaintenanceThread.PokeThreads();
            }
        }

        /// <summary>
        /// Temperary disable by things like OAR loading so that we don't kill loading
        /// </summary>
        public bool Disabled
        {
            get { return m_disabled; }
            set
            {
                m_disabled = value;
                //Poke the threads to make sure they run
                MaintenanceThread.PokeThreads();
            }
        }

        public ICommander CommandInterface
        {
            get { return m_commander; }
        }

        private IXmlRpcRouter m_XmlRpcRouter;

        public bool FirstStartup = true;

        /// <summary>
        /// Number of scripts that have failed in this run of the Maintenance Thread
        /// </summary>
        public int ScriptFailCount = 0;

        /// <summary>
        /// Errors of scripts that have failed in this run of the Maintenance Thread
        /// </summary>
        public string ScriptErrorMessages = "";

        /// <summary>
        /// Path to the script binaries.
        /// </summary>
        public string ScriptEnginesPath = "ScriptEngines";

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
        
        public System.Timers.Timer UpdateLeasesTimer = null;

        public delegate void ScriptRemoved(UUID ItemID);

        public delegate void ObjectRemoved(UUID ObjectID);

        public event ScriptRemoved OnScriptRemoved;

        public event ObjectRemoved OnObjectRemoved;

        #endregion

        #region Constructor and Shutdown
        
        public void Shutdown()
        {
            // We are shutting down
            foreach (ScriptData ID in ScriptProtection.GetAllScripts())
            {
                 ID.CloseAndDispose(false);
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

            m_enabled = ScriptConfigSource.GetBoolean("Enabled", false);

            ScriptEnginesPath = ScriptConfigSource.GetString("PathToLoadScriptsFrom", ScriptEnginesPath);
            ShowWarnings = ScriptConfigSource.GetBoolean("ShowWarnings", false);
            DisplayErrorsOnConsole = ScriptConfigSource.GetBoolean("DisplayErrorsOnConsole", false);
        }

        public void PostInitialise()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            //Register the console commands
            if (FirstStartup)
            {
                scene.AddCommand(this, "ADNE", "ADNE", "Subcommands for Aurora DotNet Engine", AuroraDotNetConsoleCommands);
                scene.AddCommand(this, "help ADNE", "help ADNE", "Brings up the help for ADNE", AuroraDotNetConsoleHelp);

                //Fire this once to make sure that the APIs are found later...
                GetAPIs();

                // Create all objects we'll be using
                ScriptProtection = new ScriptProtectionModule(Config);

                EventManager = new EventManager(this);

                Compiler = new Compiler(this);

                AppDomainManager = new AppDomainManager(this);

                ScriptErrorReporter = new ScriptErrorReporter(Config);

                AssemblyResolver = new AssemblyResolver(ScriptEnginesPath);
            }
            
            FirstStartup = false;

        	m_Scenes.Add(scene);

            scene.StackModuleInterface<IScriptModule>(this);
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_enabled)
                return;

            StartNonSharedScriptPlugins(scene);

            //Must come AFTER the script plugins setup! Otherwise you'll get weird errors from the plugins
            if (MaintenanceThread == null)
            {
                //Still must come before the maintenance thread start
                StartSharedScriptPlugins(); //This only gets called once

                m_XmlRpcRouter = m_Scenes[0].RequestModuleInterface<IXmlRpcRouter>();
                if (m_XmlRpcRouter != null)
                {
                    OnScriptRemoved += m_XmlRpcRouter.ScriptRemoved;
                    OnObjectRemoved += m_XmlRpcRouter.ObjectRemoved;
                }

                //Only needs created once
                MaintenanceThread = new MaintenanceThread(this);

                FindDefaultLSLScript();
            }

            scene.EventManager.OnStartupComplete += new OpenSim.Region.Framework.Scenes.EventManager.StartupComplete(EventManager_OnStartupComplete);
            scene.EventManager.TriggerAddToStartupQueue("ScriptEngine");
            EventManager.HookUpRegionEvents(scene);

            scene.EventManager.OnRemoveScript += StopScript;
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
            if (!m_enabled)
                return;

            scene.EventManager.OnRemoveScript -= StopScript;
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

        private int AmountOfStartupsLeft = 0;

        void EventManager_OnStartupComplete(List<string> data)
        {
            AmountOfStartupsLeft++;
            if (AmountOfStartupsLeft == Util.NumberofScenes)
            {
                //All done!
                MaintenanceThread.Started = true;
            }
        }

        #endregion

        #region Console Commands

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
                foreach (Scene scene in m_Scenes)
                {
                    scene.DefaultLSLScript = File.ReadAllText(Dir);
                }
            }
        }

        protected void AuroraDotNetConsoleHelp(string module, string[] cmdparams)
        {
            m_log.Info("Aurora DotNet Commands : \n" +
                " ADNE restart - Restarts all scripts \n" +
                " ADNE stop - Stops all scripts \n" +
                " ADNE stats - Tells stats about the script engine \n" +
                " ADNE disable - Disables the script engine temperarily \n" +
                " ADNE enable - Reenables the script engine");
        }

        protected void AuroraDotNetConsoleCommands(string module, string[] cmdparams)
        {
            if (cmdparams.Length == 1)
            {
                AuroraDotNetConsoleHelp(module, cmdparams);
                return;
            }
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
                            Aurora.DataManager.DataManager.RequestPlugin<Aurora.Framework.IScriptDataConnector>().DeleteStateSave(ID.ItemID);
                            ScriptProtection.RemovePreviouslyCompiled(ID.Source);
                            ID.Start(false);
                        }
                        catch (Exception) { }
                    }
                    m_log.Warn("[ADNE]: All scripts have been restarted.");
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
                    m_log.Warn("[ADNE]: All scripts have been stopped.");
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
                + "\nPermission level of app domains: " + AppDomainManager.PermissionLevel
            + "\nNumber Engine threads/sleeping: " + (MaintenanceThread.threadpool == null ? 0 : MaintenanceThread.threadpool.nthreads).ToString()
            + "/" + (MaintenanceThread.threadpool == null ? 0 : MaintenanceThread.threadpool.nSleepingthreads).ToString()
            + "\nNumber Script threads: " + (MaintenanceThread.threadpool == null ? 0 : MaintenanceThread.Scriptthreadpool.nthreads).ToString()
            + "/" + (MaintenanceThread.threadpool == null ? 0 : MaintenanceThread.Scriptthreadpool.nSleepingthreads).ToString());

            }
            if (cmdparams[1] == "disable")
            {
                ConsoleDisabled = true;
                m_log.Warn("[ADNE]: ADNE has been disabled.");
            }
            if (cmdparams[1] == "enable")
            {
                ConsoleDisabled = false;
                MaintenanceThread.Started = true;
                m_log.Warn("[ADNE]: ADNE has been enabled.");
            }
            if (cmdparams[1] == "help")
            {
                AuroraDotNetConsoleHelp(module, cmdparams);
            }
        }

        public void StopAllScripts()
        {
            foreach (ScriptData ID in ScriptProtection.GetAllScripts())
            {
                ID.CloseAndDispose(false);
            }
        }

        #endregion

        #region Update Leases [Unused]

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
                catch (Exception)
                {
                    m_log.Error("Lease found dead!" + script.ItemID);
                }
            }
        }

        #endregion

        #region Post Object Events

        public bool PostScriptEvent(UUID itemID, UUID primID, EventParams p, EventPriority priority)
        {
            ScriptData ID = ScriptProtection.GetScript(primID, itemID);
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

            return AddToObjectQueue(primID, name, new DetectParams[0], -1, lsl_p);
        }

        /// <summary>
        /// Posts event to all objects in the group.
        /// </summary>
        /// <param name="localID">Region object ID</param>
        /// <param name="FunctionName">Name of the function, will be state + "_event_" + FunctionName</param>
        /// <param name="VersionID">Version ID of the script. Note: If it is -1, the version ID will be detected automatically</param>
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
            // Create a structure and add data
            QueueItemStruct QIS = new QueueItemStruct();
            QIS.ID = ID;
            QIS.functionName = FunctionName;
            QIS.llDetectParams = qParams;
            QIS.param = param;
            QIS.VersionID = VersionID;
            QIS.State = ID.State;

            MaintenanceThread.AddEvent(QIS, priority);
            return true;
        }

        public DetectParams GetDetectParams(UUID primID, UUID itemID, int number)
        {
            ScriptData id = ScriptProtection.GetScript(primID, itemID);

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
            ScriptData id = ScriptProtection.GetScript(primID, itemID);

            if (id == null)
                return 0;

            return id.StartParam;
        }

        public void SetMinEventDelay(UUID itemID, UUID primID, double delay)
        {
            ScriptData ID = ScriptProtection.GetScript(primID, itemID);
            if(ID == null)
            {
                m_log.ErrorFormat("[{0}]: SetMinEventDelay found no InstanceData for script {1}.",ScriptEngineName,itemID.ToString());
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

        #endregion

        #region Reset

        public void ResetScript(UUID primID, UUID itemID, bool EndEvent)
        {
            ScriptData ID = ScriptProtection.GetScript(itemID);
            if (ID == null)
                return;

            ID.Reset();

            if(EndEvent)
                throw new EventAbortException();
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

            id.Running = true;
            id.part.SetScriptEvents(itemID, id.Script.GetStateEventFlags(id.State));
            id.part.ScheduleFullUpdate(PrimUpdateFlags.FindBest);
        }

        public void OnStopScript(uint localID, UUID itemID)
        {
            ScriptData ID = ScriptProtection.GetScript(itemID);
            if (ID == null)
                return;

            ID.Running = false;
            ID.part.SetScriptEvents(itemID, 0);
            ID.part.ScheduleFullUpdate(PrimUpdateFlags.FindBest);
        }

        public void OnGetScriptRunning(IClientAPI controllingClient,
                UUID objectID, UUID itemID)
        {
            ScriptData id = ScriptProtection.GetScript(objectID, itemID);
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

        /// <summary>
        /// Not from the client, only from other parts of the simulator
        /// </summary>
        /// <param name="itemID"></param>
        public void SuspendScript(UUID itemID)
        {
            ScriptData ID = ScriptProtection.GetScript(itemID);
            if (ID != null)
                ID.Suspended = true;
        }

        /// <summary>
        /// Not from the client, only from other parts of the simulator
        /// </summary>
        /// <param name="itemID"></param>
        public void ResumeScript(UUID itemID)
        {
            ScriptData ID = ScriptProtection.GetScript(itemID);
            if (ID != null)
                ID.Suspended = false;
        }

        #endregion

        #region XML Serialization

        public string GetXMLState(UUID itemID)
        {
            ScriptData data = ScriptProtection.GetScript(itemID);
            if(data == null)
                return "";
            return ScriptDataXMLSerializer.GetXMLState(data, this);
        }

        public bool SetXMLState(UUID itemID, string xml)
        {
            ScriptData data = ScriptProtection.GetScript(itemID);
            if (data == null)
                return false;
            ScriptDataXMLSerializer.SetXMLState(xml, data, this);
            return true;
        }
        #endregion

        #region Error reporting

        public ArrayList GetScriptErrors(UUID itemID)
        {
            return ScriptErrorReporter.FindErrors(itemID);
        }

        #endregion

        #region Starting, Updating, and Stopping scripts

        /// <summary>
        /// Fetches, loads and hooks up a script to an objects events
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="localID"></param>
        public LUStruct StartScript(SceneObjectPart part, UUID itemID, string Script, int startParam, bool postOnRez, StateSource statesource, UUID RezzedFrom)
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
                    AddToScriptQueue(id, "changed", new DetectParams[0], id.VersionID, EventPriority.FirstStart, new Object[] { new LSL_Types.LSLInteger(512) });
                    return new LUStruct() { Action = LUType.Unknown };
                }
                else
                {
                    //Restart other scripts
                    ls.Action = LUType.Load;
                }
                id.EventDelayTicks = 0;
                ScriptEngine.ScriptProtection.RemovePreviouslyCompiled(id.Source);
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
            id.Source = Script;
            id.part = part;
            id.World = part.ParentGroup.Scene;
            id.RezzedFrom = RezzedFrom;
            ls.ID = id;
            return ls;
        }

        public void UpdateScript(UUID partID, UUID itemID, string script, int startParam, bool postOnRez, int stateSource)
        {
            ScriptData id = ScriptProtection.GetScript(partID, itemID);
            LUStruct ls = new LUStruct();
            //Its a change of the script source, needs to be recompiled and such.
            if (id == null)
                id = new ScriptData(this);
            ls.Action = LUType.Reupload;
            id.PostOnRez = postOnRez;
            id.StartParam = startParam;
            id.stateSource = (StateSource)stateSource;
            id.State = "default";
            id.Source = script;
            id.EventDelayTicks = 0;
            id.part = findPrim(partID);
            id.ItemID = itemID;
            id.EventDelayTicks = 0;

            //No SOP, no compile.
            if (id.part == null)
            {
                m_log.ErrorFormat("[{0}]: Could not find scene object part corresponding " + "to localID {1} to start script", ScriptEngineName, partID);
                return;
            }
            id.World = id.part.ParentGroup.Scene;
            ls.ID = id;
            MaintenanceThread.AddScriptChange(new LUStruct[] { ls }, LoadPriority.Restart);
        }

        public void SaveStateSave(UUID ItemID, UUID PrimID)
        {
            ScriptData id = ScriptProtection.GetScript(PrimID, ItemID);
            if (id == null)
                return;
            MaintenanceThread.AddToStateSaverQueue(id, true);
        }

        /// <summary>
        /// Disables and unloads a script
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="itemID"></param>
        public void StopScript(uint localID, UUID itemID)
        {
            ScriptData data = ScriptProtection.GetScript(itemID);
            if (data == null)
                return;

            LUStruct ls = new LUStruct();

            ls.ID = data;
            ls.Action = LUType.Unload;

            MaintenanceThread.AddScriptChange(new LUStruct[] { ls }, LoadPriority.Stop);

            //Disconnect from other modules
            ObjectRemoved handlerObjectRemoved = OnObjectRemoved;
            if (handlerObjectRemoved != null)
                handlerObjectRemoved(ls.ID.part.UUID);

            ScriptRemoved handlerScriptRemoved = OnScriptRemoved;
            if (handlerScriptRemoved != null)
                handlerScriptRemoved(itemID);
        }

        public void UpdateScriptToNewObject(UUID olditemID, TaskInventoryItem newItem, SceneObjectPart newPart)
        {
            try
            {
                Aurora.DataManager.DataManager.RequestPlugin<IScriptDataConnector>().DeleteStateSave(olditemID);
                if (newPart.ParentGroup.Scene != null)
                {
                    ScriptData SD = ScriptProtection.GetScript(olditemID);
                    if (SD == null)
                        return;

                    SD.presence = SD.World.GetScenePresence(SD.part.OwnerID);

                    ScriptControllers SC = new ScriptControllers();
                    if (SD.presence != null)
                    {
                        SC = SD.presence.GetScriptControler(SD.ItemID);
                        if ((newItem.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                        {
                            SD.presence.UnRegisterControlEventsToScript(SD.part.LocalId, SD.ItemID);
                        }
                    }
                    object[] Plugins = GetSerializationData(SD.ItemID, SD.part.UUID);
                    RemoveScript(SD.part.UUID, SD.ItemID);

                    MaintenanceThread.SetEventSchSetIgnoreNew(SD, true);

                    ScriptProtection.RemoveScript(SD);

                    SD.part = newPart;
                    SD.ItemID = newItem.ItemID;
                    //Find the asset ID
                    SD.InventoryItem = newItem;
                    //Try to see if this was rezzed from someone's inventory
                    SD.UserInventoryItemID = SD.part.FromUserInventoryItemID;

                    CreateFromData(SD.part.UUID, SD.ItemID, SD.part.UUID, Plugins);

                    SD.World = newPart.ParentGroup.Scene;
                    SD.SetApis();

                    MaintenanceThread.SetEventSchSetIgnoreNew(SD, false);


                    if (SD.presence != null && (newItem.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        SC.itemID = newItem.ItemID;
                        SC.part = SD.part;
                        SD.presence.RegisterScriptController(SC);
                    }

                    ScriptProtection.AddNewScript(SD);


                    ScriptDataSQLSerializer.SaveState(SD, this);
                }
            }
            catch
            {
            }
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
                string script = OpenMetaverse.Utils.BytesToString(asset.Data);
                try
                {
                    string assembly;
                    Compiler.PerformScriptCompile(script, itemID, UUID.Zero, 0, out assembly);
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

        public IScriptApi GetApi(UUID itemID, string name)
        {
            ScriptData id = ScriptProtection.GetScript(itemID);
            if (id == null)
                return null;

            return id.Apis[name];
        }

        public IScriptApi[] GetAPIs()
        {
            return AuroraModuleLoader.PickupModules<IScriptApi>().ToArray();
        }

        public List<string> GetAllFunctionNames()
        {
            List<string> FunctionNames = new List<string>();

            IScriptApi[] apis = GetAPIs();
            foreach (IScriptApi api in apis)
            {
                MemberInfo[] members = api.GetType().GetMembers();
                string APIName = api.Name;
                if (APIName == "LSL")
                    continue;
                else if (APIName == "OSSL")
                    APIName = "os";
                foreach (MemberInfo member in members)
                {
                    if (member.Name.StartsWith(APIName, StringComparison.CurrentCultureIgnoreCase))
                        FunctionNames.Add(member.Name);
                }
            }

            return FunctionNames;
        }

        #endregion

        #region Script Plugin Manager

        private List<IScriptPlugin> ScriptPlugins = new List<IScriptPlugin>();

        public IScriptPlugin GetScriptPlugin(string Name)
        {
            foreach (IScriptPlugin plugin in ScriptPlugins)
            {
                if (plugin.Name == Name)
                    return plugin;
            }
            return null;
        }

        #region Plugin Initializers

        public class NonSharedScriptPluginInitialiser : PluginInitialiserBase
        {
            ScriptEngine m_engine;
            Scene m_scene;
            public NonSharedScriptPluginInitialiser(ScriptEngine engine, Scene scene)
            {
                m_scene = scene;
                m_engine = engine;
            }

            public override void Initialise(IPlugin plugin)
            {
                INonSharedScriptPlugin nonSharedPlugin = (INonSharedScriptPlugin)plugin;
                nonSharedPlugin.Initialize(m_engine, m_scene);
            }
        }

        public class SharedScriptPluginInitialiser : PluginInitialiserBase
        {
            ScriptEngine m_engine;
            public SharedScriptPluginInitialiser(ScriptEngine engine)
            {
                m_engine = engine;
            }

            public override void Initialise(IPlugin plugin)
            {
                ISharedScriptPlugin sharedPlugin = (ISharedScriptPlugin)plugin;
                sharedPlugin.Initialize(m_engine);
            }
        }

        #endregion

        /// <summary>
        /// Starts all non shared script plugins
        /// </summary>
        /// <param name="scene"></param>
        private void StartNonSharedScriptPlugins(Scene scene)
        {
            List<INonSharedScriptPlugin> nonSharedPlugins = AuroraModuleLoader.PickupModules<INonSharedScriptPlugin>();
            foreach (INonSharedScriptPlugin plugin in nonSharedPlugins)
            {
                plugin.Initialize(this, scene);
            }
            ScriptPlugins.AddRange(nonSharedPlugins.ToArray());
        }

        /// <summary>
        /// Starts all shared script plugins
        /// </summary>
        public void StartSharedScriptPlugins()
        {
            List<ISharedScriptPlugin> sharedPlugins = AuroraModuleLoader.PickupModules<ISharedScriptPlugin>();
            foreach (ISharedScriptPlugin plugin in sharedPlugins)
            {
                plugin.Initialize(this);
            }
            ScriptPlugins.AddRange(sharedPlugins.ToArray());
        }

        public void DoOneScriptPluginPass()
        {
            foreach (IScriptPlugin plugin in ScriptPlugins)
            {
                plugin.Check();
            }
        }

        /// <summary>
        /// Removes a script from all Script Plugins
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="itemID"></param>
        public void RemoveScript(UUID primID, UUID itemID)
        {
            foreach (IScriptPlugin plugin in ScriptPlugins)
            {
                plugin.RemoveScript(primID, itemID);
            }
        }

        public Object[] GetSerializationData(UUID itemID, UUID primID)
        {
            List<Object> data = new List<Object>();

            foreach (IScriptPlugin plugin in ScriptPlugins)
            {
                try
                {
                    data.AddRange(plugin.GetSerializationData(itemID, primID));
                }
                catch(Exception ex)
                {
                    m_log.Warn("[" + Name + "]: Error attempting to get serialization data, " + ex.ToString());
                }
            }

            return data.ToArray();
        }

        public void CreateFromData(UUID primID,
                UUID itemID, UUID hostID, Object[] data)
        {
            try
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

                        IScriptPlugin plugin = GetScriptPlugin(type);
                        if (plugin != null)
                        {
                            plugin.CreateFromData(itemID, hostID, item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.Warn("[" + Name + "]: Error attempting to CreateFromData, " + ex.ToString());
            }
        }

        #endregion

        #region Helpers

        public SceneObjectPart findPrim(UUID objectID)
        {
            foreach (Scene s in m_Scenes)
            {
                SceneObjectPart part = s.GetSceneObjectPart(objectID);
                if (part != null)
                    return part;
            }
            return null;
        }

        public SceneObjectPart findPrim(uint localID)
        {
            foreach (Scene s in m_Scenes)
            {
                SceneObjectPart part = s.GetSceneObjectPart(localID);
                if (part != null)
                    return part;
            }
            return null;
        }

        public Scene findPrimsScene(uint localID)
        {
            foreach (Scene s in m_Scenes)
            {
                SceneObjectPart part = s.GetSceneObjectPart(localID);
                if (part != null)
                    return s;
            }
            return null;
        }

        private bool ScriptDanger(SceneObjectPart part, Vector3 pos)
        {
            Scene scene = part.ParentGroup.Scene;
            if (part.IsAttachment && scene.RunScriptsInAttachments)
                return true; //Always run as in SL
            ILandObject parcel = scene.LandChannel.GetLandObject(pos.X, pos.Y);
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
                if (pos.X > 0f && pos.X < Constants.RegionSize && pos.Y > 0f && pos.Y < Constants.RegionSize)
                    // The only time parcel != null when an object is inside a region is when
                    // there is nothing behind the landchannel.  IE, no land plugin loaded.
                    return true;
                else
                    // The object is outside of this region.  Stop piping events to it.
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
                return PipeEventsForScript(parent, parent.AbsolutePosition);
            else
                return PipeEventsForScript(part, part.AbsolutePosition);
        }

        public bool PipeEventsForScript(SceneObjectPart part, Vector3 position)
        {
            // Changed so that child prims of attachments return ScriptDanger for their parent, so that
            //  their scripts will actually run.
            //      -- Leaf, Tue Aug 12 14:17:05 EDT 2008
            SceneObjectPart parent = part.ParentGroup.RootPart;
            if (parent != null && parent.IsAttachment)
                return ScriptDanger(parent, position);
            else
                return ScriptDanger(part, position);
        }

        #endregion
    }

    public class ScriptErrorReporter
    {
        //Errors that have been thrown while compiling
        private Dictionary<UUID, ArrayList> Errors = new Dictionary<UUID, ArrayList>();
        private int Timeout = 5000; // 5 seconds

        public ScriptErrorReporter(IConfig config)
        {
            Timeout = (config.GetInt("ScriptErrorFindingTimeOut", 5) * 1000);
        }

        /// <summary>
        /// Add a new error for the client thread to find
        /// </summary>
        /// <param name="ItemID"></param>
        /// <param name="errors"></param>
        public void AddError(UUID ItemID, ArrayList errors)
        {
            if (!Errors.ContainsKey(ItemID))
                Errors.Add(ItemID, errors);
            else
                Errors[ItemID] = errors;
        }

        /// <summary>
        /// Find the errors that the script may have produced while compiling
        /// </summary>
        /// <param name="ItemID"></param>
        /// <returns></returns>
        public ArrayList FindErrors(UUID ItemID)
        {
            ArrayList Error = new ArrayList();

            if(!TryFindError(ItemID, out Error))
                return new ArrayList(new string[]{"Compile not finished."}); //Not there, but need to return something so the user knows
            
            RemoveError(ItemID);

            if ((string)Error[0] == "SUCCESSFULL")
                return new ArrayList();

            return Error;
        }

        /// <summary>
        /// Wait while the script is processed
        /// </summary>
        /// <param name="ItemID"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryFindError(UUID ItemID, out ArrayList error)
        {
            error = null;
            if (!Errors.ContainsKey(ItemID))
                Errors.Add(ItemID, null); //Add it so that it does not error out with no key

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
        /// Clear this item's errors
        /// </summary>
        /// <param name="ItemID"></param>
        public void RemoveError(UUID ItemID)
        {
            Errors.Remove(ItemID);
        }
    }
}
