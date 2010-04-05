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
using System.Net;
using System.Reflection;
using System.Globalization;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared;
using OpenSim.Region.ScriptEngine.Shared.Api;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using OpenSim.Region.ScriptEngine.Shared.Api.Runtime;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;
using OpenSim.Region.ScriptEngine.Shared.CodeTools;

namespace OpenSim.Region.ScriptEngine.DotNetEngine
{

    #region InstancesData
    
	public class InstancesData
	{
        public string AssemblyName = "";
        public uint localID = 0;
        public List<InstanceData> Instances = new List<InstanceData>();
        public List<UUID> ItemIDs = new List<UUID>();
	}
	
	#endregion
	
	#region InstanceData
	
    public class InstanceData
    {
    	#region Constructor
    	
    	public InstanceData(ScriptManager engine)
    	{
            m_ScriptManager = engine;
            m_scriptEngine = m_ScriptManager.m_scriptEngine;
            World = m_scriptEngine.World;
    	}
    	
    	#endregion
    	
    	#region Declares
    	
    	private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private ScriptManager m_ScriptManager;
        private ScriptEngine m_scriptEngine;
        private Scene World;
        public IScript Script;
        public string State;
        public bool Running;
        public bool Disabled;
        public string Source;
        public string ClassSource;
        public int StartParam;
        public StateSource stateSource;
        public AppDomain AppDomain;
        public Dictionary<string, IScriptApi> Apis;
        public Dictionary<KeyValuePair<int,int>, KeyValuePair<int,int>>
                LineMap;
        public ISponsor ScriptSponsor;

        public SceneObjectPart part;

        public long EventDelayTicks = 0;
        public long NextEventTimeTicks = 0;
        public UUID AssetID;
        public string AssemblyName;
        //This is the UUID of the actual script.
        public UUID ItemID;
        //This is the localUUID of the object the script is in.
        public uint localID;
        public string ClassID;
        public bool PostOnRez;
        public TaskInventoryItem InventoryItem;
        public InstancesData MacroData;
        public ScenePresence presence;
        public DetectParams[] LastDetectParams;
        
        #endregion
        
        #region Close Script
        
        /// <summary>
        /// This closes the scrpit, removes it from any known spots, and disposes of itself.
        /// This function is microthreaded.
        /// </summary>
        /// <returns></returns>
        public IEnumerable CloseAndDispose()
        {
        	ReleaseControls(localID, ItemID);
            // Stop long command on script
            AsyncCommandManager.RemoveScript(m_ScriptManager.m_scriptEngine, localID, ItemID);
            m_ScriptManager.m_scriptEngine.m_EventManager.state_exit(localID);
            m_ScriptManager.m_scriptEngine.m_EventQueueManager.RemoveFromQueue(ItemID);
           
            
            yield return null;

            try
            {
                // Get AppDomain
                // Tell script not to accept new requests
                Running = false;
                Disabled = true;
                AppDomain ad = AppDomain;
                Script.Close();
                // Tell AppDomain that we have stopped script
                m_ScriptManager.m_scriptEngine.m_AppDomainManager.UnloadScriptAppDomain(ad);
                // Remove from internal structure
            	m_ScriptManager.RemoveScript(this);
            }
            catch (Exception e) // LEGIT: User Scripting
            {
                m_log.Error("[" +
                            m_ScriptManager.m_scriptEngine.ScriptEngineName +
                            "]: Exception stopping script localID: " +
                            localID + " LLUID: " + ItemID.ToString() +
                            ": " + e.ToString());
            }
            m_log.DebugFormat("[{0}]: Closed Script in " + part.Name, m_ScriptManager.m_scriptEngine.ScriptEngineName);
        }
        
        /// <summary>
        /// Removes any permissions the script may have on other avatars.
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="itemID"></param>
        private void ReleaseControls(uint localID, UUID itemID)
        {
            if (part != null)
            {
                int permsMask;
                UUID permsGranter;
                lock (part.TaskInventory)
                {
                    if (!part.TaskInventory.ContainsKey(itemID))
                        return;

                    permsGranter = part.TaskInventory[itemID].PermsGranter;
                    permsMask = part.TaskInventory[itemID].PermsMask;
                }

                if ((permsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                {
                    if (presence != null)
                        presence.UnRegisterControlEventsToScript(localID, itemID);
                }
            }
        }
        
        #endregion
        
        #region Reset Script
        
		/// <summary>
		/// This resets the script back to its default state.
		/// </summary>
        internal void Reset()
        {
            ReleaseControls(localID, ItemID);
            //Must be posted immediately, otherwise the next line will delete it.
            m_ScriptManager.m_scriptEngine.m_EventManager.state_exit(localID);
            m_ScriptManager.m_scriptEngine.m_EventQueueManager.RemoveFromQueue(ItemID);
            m_ScriptManager.UpdateScriptInstanceData(this);
            m_ScriptManager.m_scriptEngine.m_EventQueueManager.AddToScriptQueue(this, "state_entry", new DetectParams[0],new object[] { });
            m_log.InfoFormat("[{0}]: Reset Script {1}", m_ScriptManager.m_scriptEngine.ScriptEngineName, ItemID);
        }
        #endregion
        
        #region Helpers
        
        public override string ToString()
        {
        	return "localID: "+ localID
        		+", itemID: "+
        		ItemID;
        }

        internal void SetApis()
        {
        	if(part == null)
        		part = m_ScriptManager.m_scriptEngine.World.GetSceneObjectPart(localID);
        	Apis = new Dictionary<string, IScriptApi>();

            ApiManager am = new ApiManager();
            foreach (string api in am.GetApis())
            {
                Apis[api] = am.CreateApi(api);
                Apis[api].Initialize(m_ScriptManager.m_scriptEngine, part,
                        localID, ItemID);
            }
			foreach (KeyValuePair<string, IScriptApi> kv in Apis)
            {
                Script.InitApi(kv.Key, kv.Value);
            }
        }
        
        public void ShowError(Exception e, int stage)
        {
            if (presence != null && (!PostOnRez))
                presence.ControllingClient.SendAgentAlertMessage(
                        "Script saved with errors, check debug window!",
                        false);
            m_ScriptManager.AddError(ItemID, e.Message.ToString());

            try
            {
                // DISPLAY ERROR INWORLD
                string text = "Error compiling script in stage "+stage+":\n" +
                    e.Message.ToString() + " itemID: " + ItemID + ", localID" + localID + ", CompiledFile: " + AssemblyName;
                m_log.Error(text);
                if (text.Length > 1100)
                    text = text.Substring(0, 1099);

                World.SimChat(Utils.StringToBytes(text),
                        ChatTypeEnum.DebugChannel, 2147483647,
                        part.AbsolutePosition, part.Name, part.UUID,
                        false);
            }
            catch (Exception e2) // LEGIT: User Scripting
            {
                m_log.Error("[" +
                            m_scriptEngine.ScriptEngineName +
                            "]: Error displaying error in-world: " +
                            e2.ToString());
                m_log.Error("[" +
                            m_scriptEngine.ScriptEngineName + "]: " +
                            "Errormessage: Error compiling script:\r\n" +
                            e2.Message.ToString());
            }
            throw e;
        }
        
        #endregion
        
        #region Start Script
        
        /// <summary>
        /// This starts the script and sets up the variables.
        /// This function is microthreaded.
        /// </summary>
        /// <returns></returns>
        public IEnumerator Start()
        {
        	DateTime Start = DateTime.Now.ToUniversalTime();
        	
        	// We will initialize and start the script.
            // It will be up to the script itself to hook up the correct events.
            part = World.GetSceneObjectPart(localID);
            
            if (null == part)
            {
                m_log.ErrorFormat(
                    "[{0}]: Could not find scene object part corresponding " +
                    "to localID {1} to start script",
                    m_scriptEngine.ScriptEngineName, localID);

                throw new NullReferenceException();
            }


            if (part.TaskInventory.TryGetValue(ItemID, out InventoryItem))
                AssetID = InventoryItem.AssetID;

            presence =
                    World.GetScenePresence(InventoryItem.OwnerID);
            if (presence != null)
            {
                m_log.DebugFormat(
                    "[{0}]: Starting Script {1} in object {2} by avatar {3}.",
                    m_scriptEngine.ScriptEngineName, part.Inventory.GetInventoryItem(ItemID).Name, part.Name, presence.Name);
            }
            else
            {
                m_log.DebugFormat(
                    "[{0}]: Starting Script {1} in object {2}.",
                    m_scriptEngine.ScriptEngineName, part.Inventory.GetInventoryItem(ItemID).Name, part.Name);
            }
            
            CultureInfo USCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = USCulture;

            MacroData = m_ScriptManager.GetMacroScript(localID);
            
            if (MacroData == null)
                MacroData = new InstancesData();

            #region Class and interface reader

            string Inherited = "";
            string ClassName = "";

            if (m_scriptEngine.ScriptProtection.AllowMacroScripting)
            {
                if (Source.Contains("#Inherited"))
                {
                    int line = Source.IndexOf("#Inherited ");
                    Inherited = Source.Split('\n')[line];
                    Inherited = Inherited.Replace("#Inherited ", "");
                    Source = Source.Replace("#Inherited " + Inherited, "");
                }
                if (Source.Contains("#ClassName "))
                {
                    int line = Source.IndexOf("#ClassName ");
                    ClassName = Source.Split('\n')[line];
                    ClassName = ClassName.Replace("#ClassName ", "");
                    Source = Source.Replace("#ClassName " + ClassName, "");
                }
                if (Source.Contains("#IncludeHTML "))
                {
                    string URL = "";
                    int line = Source.IndexOf("#IncludeHTML ");
                    URL = Source.Split('\n')[line];
                    URL = URL.Replace("#IncludeHTML ", "");
                    Source = Source.Replace("#IncludeHTML " + URL, "");
                    string webSite = ScriptManager.ReadExternalWebsite(URL);
                    m_scriptEngine.ScriptProtection.AddNewClassSource(URL, webSite, null);
                    m_scriptEngine.ScriptProtection.AddWantedSRC(ItemID, URL);
                }
                if (Source.Contains("#Include "))
                {
                    string WantedClass = "";
                    int line = Source.IndexOf("#Include ");
                    WantedClass = Source.Split('\n')[line];
                    WantedClass = WantedClass.Replace("#Include ", "");
                    Source = Source.Replace("#Include " + WantedClass, "");
                    m_scriptEngine.ScriptProtection.AddWantedSRC(ItemID, WantedClass);
                }
            }
            else
            {
            	if (Source.Contains("#Inherited"))
                {
                    int line = Source.IndexOf("#Inherited ");
                    Inherited = Source.Split('\n')[line];
                    Inherited = Inherited.Replace("#Inherited ", "");
                    Source = Source.Replace("#Inherited " + Inherited, "");
                    Inherited = "";
                }
                if (Source.Contains("#ClassName "))
                {
                    int line = Source.IndexOf("#ClassName ");
                    ClassName = Source.Split('\n')[line];
                    ClassName = ClassName.Replace("#ClassName ", "");
                    Source = Source.Replace("#ClassName " + ClassName, "");
                    ClassName = "";
                }
                if (Source.Contains("#IncludeHTML "))
                {
                    string URL = "";
                    int line = Source.IndexOf("#IncludeHTML ");
                    URL = Source.Split('\n')[line];
                    URL = URL.Replace("#IncludeHTML ", "");
                    Source = Source.Replace("#IncludeHTML " + URL, "");
                    string webSite = ScriptManager.ReadExternalWebsite(URL);
                    URL = "";
                    webSite = "";
                }
                if (Source.Contains("#Include "))
                {
                    string WantedClass = "";
                    int line = Source.IndexOf("#Include ");
                    WantedClass = Source.Split('\n')[line];
                    WantedClass = WantedClass.Replace("#Include ", "");
                    Source = Source.Replace("#Include " + WantedClass, "");
                    m_scriptEngine.ScriptProtection.AddWantedSRC(ItemID, WantedClass);
                    WantedClass = "";
                }
            }
            
            #endregion

            try
            {
                // Compile (We assume LSL)
                m_ScriptManager.LSLCompiler.PerformScriptCompile(Source,
                        AssetID, InventoryItem.OwnerID, ItemID, Inherited, ClassName, m_scriptEngine.ScriptProtection,localID, this,
                        out AssemblyName, out LineMap, out ClassID);
            }
            catch (Exception ex)
            {
                ShowError(ex, 1);
            }
            
			bool useDebug = false;
			if(useDebug)
			{
            	t = (DateTime.Now.ToUniversalTime() - Start);
        		m_log.Debug("Stage 1: " + t.TotalSeconds);
			}
			
            yield return null;
            
            try
            {
                if (ClassName != "")
                {
                    Script =
                            m_scriptEngine.m_AppDomainManager.LoadScript(
                            AssemblyName, "SecondLife." + ClassName, out AppDomain);
                }
                else
                {
                    Script =
                            m_scriptEngine.m_AppDomainManager.LoadScript(
                            AssemblyName, "SecondLife." + ClassID, out AppDomain);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex, 2);
            }
            
            if(useDebug)
			{
            	t = (DateTime.Now.ToUniversalTime() - Start);
        		m_log.Debug("Stage 2: " + t.TotalSeconds);
            }
            
            //Register the sponsor
            //ISponsor scriptSponsor = new ScriptSponsor();
            //ILease lease = (ILease)RemotingServices.GetLifetimeService(CompiledScript as MarshalByRefObject);
            //lease.Register(scriptSponsor);
            //id.ScriptSponsor = scriptSponsor;

            State = "default";
            Running = true;
            Disabled = false;
            
            MacroData.ItemIDs.Add(ItemID);
            MacroData.AssemblyName = AssemblyName;
            MacroData.localID = localID;
            MacroData.Instances.Add(this);

            m_ScriptManager.UpdateMacroScript(MacroData);
            
            // Add it to our script memstruct
            m_ScriptManager.UpdateScriptInstanceData(this);
            
            SetApis();

            #region Post Script Events

            // Fire the first start-event
            int eventFlags = Script.GetStateEventFlags(State);
            part.SetScriptEvents(ItemID, eventFlags);

            m_scriptEngine.m_EventQueueManager.AddToScriptQueue(
                    this, "state_entry", new DetectParams[0],
                    new object[] { });

            if (PostOnRez)
            {
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(
                    this, "on_rez", new DetectParams[0],
                    new object[] { new LSL_Types.LSLInteger(StartParam) });
            }

            if (stateSource == StateSource.AttachedRez)
            {
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(this, "attach", new DetectParams[0],
                    new object[] { new LSL_Types.LSLString(part.AttachedAvatar.ToString()) });
            }
            else if (stateSource == StateSource.NewRez)
            {
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(this, "changed", new DetectParams[0],
                                          new Object[] { new LSL_Types.LSLInteger(256) });
            }
            else if (stateSource == StateSource.PrimCrossing)
            {
                // CHANGED_REGION
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(this, "changed", new DetectParams[0],
                                          new Object[] { new LSL_Types.LSLInteger(512) });
            }
            
            #endregion

            #region Warnings

            string[] warnings = m_ScriptManager.LSLCompiler.GetWarnings();

            if (warnings != null && warnings.Length != 0)
            {
                if (presence != null && (!PostOnRez))
                    presence.ControllingClient.SendAgentAlertMessage(
                            "Script saved with warnings, check debug window!",
                            false);

                foreach (string warning in warnings)
                {
                    try
                    {
                        // DISPLAY WARNING INWORLD
                        string text = "Warning:\n" + warning;
                        if (text.Length > 1100)
                            text = text.Substring(0, 1099);

                        World.SimChat(Utils.StringToBytes(text),
                                ChatTypeEnum.DebugChannel, 2147483647,
                                part.AbsolutePosition, part.Name, part.UUID,
                                false);
                    }
                    catch (Exception e2) // LEGIT: User Scripting
                    {
                        m_log.Error("[" +
                                m_scriptEngine.ScriptEngineName +
                                "]: Error displaying warning in-world: " +
                                e2.ToString());
                        m_log.Error("[" +
                                m_scriptEngine.ScriptEngineName + "]: " +
                                "Warning:\r\n" +
                                warning);
                    }
                }
            }
            else
            {
                if (presence != null && (!PostOnRez))
                {
                    presence.ControllingClient.SendAgentAlertMessage(
                            "Compile successful", false);
                }
                if (presence != null)
                {
                    m_log.DebugFormat(
                        "[{0}]: Started Script {1} in object {2} by avatar {3} successfully.",
                        m_scriptEngine.ScriptEngineName, part.Inventory.GetInventoryItem(ItemID).Name, part.Name, presence.Name);
                }
                else
                {
                    m_log.DebugFormat(
                        "[{0}]: Started Script {1} in object {2} successfully.",
                        m_scriptEngine.ScriptEngineName, InventoryItem.Name, part.Name);
                }
            }

            #endregion
        }
        
        #endregion
        
        #region Event Processing
        
        /// <summary>
        /// Executes the given event 'FunctionName'  with parameters 'args'.
        /// This event is microthreaded.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="FunctionName"></param>
        /// <param name="qParams"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal IEnumerator ExecuteEvent(string FunctionName, DetectParams[] qParams, object[] args)
        {
        	//;^) Ewe Loon,fix 
            
            
            if (!Running || Disabled)
                throw new NotSupportedException();
			
            if (qParams.Length > 0)
                LastDetectParams = qParams;

            if (EventDelayTicks != 0)
            {
                if (DateTime.Now.Ticks < NextEventTimeTicks)
                    throw new Exception();

                NextEventTimeTicks = DateTime.Now.Ticks + EventDelayTicks;
            }
            
            yield return null;
            
            try
            {
                Script.ExecuteEvent(State, FunctionName, args);
            }
            catch (SelfDeleteException) // Must delete SOG
            {
                if (part != null && part.ParentGroup != null)
                    m_scriptEngine.World.DeleteSceneObject(
                        part.ParentGroup, false);
            }
            catch (ScriptDeleteException) // Must delete item
            {
                if (part != null && part.ParentGroup != null)
                    part.Inventory.RemoveInventoryItem(ItemID);
            }
            catch (Exception e)
            {
                LastDetectParams = null;
                m_log.InfoFormat("[Script Event Error] ,{0},{1},{2},{3},{4}", part.Name, FunctionName, e.Message, qParams.Length, LastDetectParams.Length);
                throw e;
            }
            LastDetectParams = null;
        }
        
        #endregion
    }

    #endregion
	
    #region Script Manager
    
    public class ScriptManager
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Thread scriptLoadUnloadThread;
        private static Thread staticScriptLoadUnloadThread = null;
        private Queue<LUStruct> LUQueue = new Queue<LUStruct>();
        private static bool PrivateThread;
        private int LoadUnloadMaxQueueSize;
        private int MinMicrothreadScriptThreshold;
        private Object scriptLock = new Object();
        private bool m_started = false;
        private Dictionary<InstanceData, DetectParams[]> detparms =
                new Dictionary<InstanceData, DetectParams[]>();
		private Dictionary<UUID, List<string>> m_Errors = new Dictionary<UUID, List<string>>();
        
        // Load/Unload structure
        private struct LUStruct
        {
            public uint localID;
            public UUID itemID;
            public string script;
            public LUType Action;
            public int startParam;
            public bool postOnRez;
            public StateSource stateSource;
        }

        private enum LUType
        {
            Unknown = 0,
            Load = 1,
            Unload = 2
        }

        public Dictionary<uint, InstancesData> MacroScripts = new Dictionary<uint, InstancesData>();
        public Compiler LSLCompiler;

        public Scene World
        {
            get { return m_scriptEngine.World; }
        }

        #endregion

        #region Error Reporting
        
        /// <summary>
        /// Gets compile errors for the given itemID.
        /// </summary>
        /// <param name="ItemID"></param>
        /// <returns></returns>
        public string[] GetErrors(UUID ItemID)
        {
            Thread.Sleep(1000);
            if (m_Errors.ContainsKey(ItemID))
            {
                string[] errors = m_Errors[ItemID].ToArray();
                m_Errors.Remove(ItemID);
                return errors;
            }
            else
            {
                return new string[0];
            }
        }
		
        /// <summary>
        /// Adds the given error to the list of known errors.
        /// </summary>
        /// <param name="ItemID"></param>
        /// <param name="Error"></param>
        public void AddError(UUID ItemID, string Error)
        {
            List<string> Errors = new List<string>();
            if (!m_Errors.ContainsKey(ItemID))
            {
                Errors.Add(Error);
                m_Errors.Add(ItemID, Errors);
            }
            else
            {
                m_Errors.TryGetValue(ItemID, out Errors);
                Errors.Add(Error);
                m_Errors.Remove(ItemID);
                m_Errors.Add(ItemID, Errors);
            }
        }

        #endregion

        #region Object init/shutdown

        public ScriptEngine m_scriptEngine;

        public ScriptManager(ScriptEngine scriptEngine)
        {
            m_scriptEngine = scriptEngine;
            ReadConfig();
            LSLCompiler = new Compiler(m_scriptEngine);
        }
        
        public void ReadConfig()
        {
            // TODO: Requires sharing of all ScriptManagers to single thread
            PrivateThread = true;
            LoadUnloadMaxQueueSize = m_scriptEngine.ScriptConfigSource.GetInt(
                    "LoadUnloadMaxQueueSize", 100);
            MinMicrothreadScriptThreshold = m_scriptEngine.ScriptConfigSource.GetInt(
                    "LoadUnloadMaxQueueSizeBeforeMicrothreading", 100);
        }

        public void Start()
        {
            m_started = true;

            AppDomain.CurrentDomain.AssemblyResolve +=
                    new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            //
            // CREATE THREAD
            // Private or shared
            //
            if (PrivateThread)
            {
                // Assign one thread per region
                //scriptLoadUnloadThread = StartScriptLoadUnloadThread();
            }
            else
            {
                // Shared thread - make sure one exist, then assign it to the private
                if (staticScriptLoadUnloadThread == null)
                {
                    //staticScriptLoadUnloadThread =
                    //        StartScriptLoadUnloadThread();
                }
                scriptLoadUnloadThread = staticScriptLoadUnloadThread;
            }
        }

        internal void Stop()
        {
            foreach (InstancesData script in MacroScripts.Values)
            {
                foreach(InstanceData ID in script.Instances)
                    StopScript(ID.localID,ID.ItemID);
            }
        }

        ~ScriptManager()
        {
            // Abort load/unload thread
            try
            {
                if (scriptLoadUnloadThread != null &&
                        scriptLoadUnloadThread.IsAlive == true)
                {
                    scriptLoadUnloadThread.Abort();
                    //scriptLoadUnloadThread.Join();
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Load / Unload scripts (Thread loop)

        /// <summary>
        /// Main Loop that starts/stops all scripts in the LUQueue.
        /// </summary>
        public void DoScriptsLoadUnload()
        {
            if (!m_started)
                return;

            List<IEnumerator> StartParts = new List<IEnumerator>();
            List<IEnumerator> StopParts = new List<IEnumerator>();
            lock (LUQueue)
            {
            	if (LUQueue.Count > 0)
            	{
            		int i = 0;
            		while (i < LUQueue.Count)
            		{
            			LUStruct item = LUQueue.Dequeue();

            			if (item.Action == LUType.Unload)
            			{
            				InstanceData id = GetScript(item.localID, item.itemID);
            				if (id != null)
            					StopParts.Add(id.CloseAndDispose().GetEnumerator());
            			}
            			else if (item.Action == LUType.Load)
            			{
            				InstanceData id = GetScript(item.localID, item.itemID);
            				if (id != null)
            					StopParts.Add(id.CloseAndDispose().GetEnumerator());
            				id = new InstanceData(this);
            				id.ItemID = item.itemID;
            				id.localID = item.localID;
            				id.PostOnRez = item.postOnRez;
            				id.StartParam = item.startParam;
            				id.stateSource = item.stateSource;
            				id.State = "default";
            				id.Running = true;
            				id.Disabled = false;
            				id.Source = item.script;
            				id.PostOnRez = item.postOnRez;
            				StartParts.Add(id.Start());
            			}
            			i++;
            		}
            	}
            }
            lock (StopParts)
            {
                int i = 0;
                while (StopParts.Count > 0 && i < 1000)
                {
                    i++;

                    bool running = false;
                    try
                    {
                        running = StopParts[i % StopParts.Count].MoveNext();
                    }
                    catch (Exception)
                    {
                    }

                    if (!running)
                        StopParts.Remove(StopParts[i % StopParts.Count]);
                }
            }
            lock (StartParts)
            {
                int i = 0;
                while (StartParts.Count > 0 && i < 1000)
                {
                    i++;

                    bool running = false;
                    try
                    {
                        running = StartParts[i % StartParts.Count].MoveNext();
                    }
                    catch (Exception)
                    {
                    }

                    if (!running)
                        StartParts.Remove(StartParts[i % StartParts.Count]);
                }
            }
        }

        #endregion

        #region Helper functions

        private static Assembly CurrentDomain_AssemblyResolve(
                object sender, ResolveEventArgs args)
        {
            return Assembly.GetExecutingAssembly().FullName == args.Name ?
                    Assembly.GetExecutingAssembly() : null;
        }

        #endregion

        #region Start/Stop script queue

        /// <summary>
        /// Fetches, loads and hooks up a script to an objects events
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="localID"></param>
        public void StartScript(uint localID, UUID itemID, string Script, int startParam, bool postOnRez, StateSource statesource)
        {
            lock (LUQueue)
            {
                if ((LUQueue.Count >= LoadUnloadMaxQueueSize) && m_started)
                {
                    m_log.Error("[" +
                                m_scriptEngine.ScriptEngineName +
                                "]: ERROR: Load/unload queue item count is at " +
                                LUQueue.Count +
                                ". Config variable \"LoadUnloadMaxQueueSize\" "+
                                "is set to " + LoadUnloadMaxQueueSize +
                                ", so ignoring new script.");

                    return;
                }

                LUStruct ls = new LUStruct();
                ls.localID = localID;
                ls.itemID = itemID;
                ls.script = Script;
                ls.Action = LUType.Load;
                ls.startParam = startParam;
                ls.postOnRez = postOnRez;
                ls.stateSource = statesource;
                LUQueue.Enqueue(ls);
            }
        }

        /// <summary>
        /// Disables and unloads a script
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="itemID"></param>
        public void StopScript(uint localID, UUID itemID)
        {
            InstanceData data = GetScript(localID, itemID);
            if(data == null)
            	return;
            if (data.Disabled)
                return; 

            LUStruct ls = new LUStruct();
            ls.localID = localID;
            ls.itemID = itemID;
            ls.Action = LUType.Unload;
            ls.startParam = 0;
            ls.postOnRez = false;
            ls.stateSource = 0;
            lock (LUQueue)
            {
                LUQueue.Enqueue(ls);
            }
        }

        #endregion

        #region Internal functions to keep track of script

        /// <summary>
        /// Gets all itemID's of scripts in the given localID.
        /// </summary>
        /// <param name="localID"></param>
        /// <returns></returns>
        public List<UUID> GetScriptKeys(uint localID)
        {
        	if(!MacroScripts.ContainsKey(localID))
        		return new List<UUID>();
            return MacroScripts[localID].ItemIDs;
        }

        /// <summary>
        /// Gets the localID of an object from its ItemID.
        /// </summary>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public InstanceData GetScriptByLocalID(UUID itemID)
        {
            foreach(KeyValuePair<uint, InstancesData> IDs in MacroScripts)
            {
                if (IDs.Value.ItemIDs.Contains(itemID))
                {
                    for (int i = 0; i < IDs.Value.Instances.Count; i++)
                    {
                        if (IDs.Value.Instances[i].ItemID == itemID)
                        {
                            return IDs.Value.Instances[i];
                        }
                    }
                }
            }
            return null;
        }

        #region InstanceData
        
        /// <summary>
		/// Gets the InstanceData by the prims local and itemID.
		/// </summary>
		/// <param name="localID"></param>
		/// <param name="itemID"></param>
		/// <returns></returns>
        public InstanceData GetScript(uint localID, UUID itemID)
        {
            lock (scriptLock)
            {
                if (!MacroScripts.ContainsKey(localID))
                    return null;
                for (int i = 0; i < MacroScripts[localID].Instances.Count; i++)
                {
                    if (MacroScripts[localID].Instances[i].ItemID == itemID)
                    {
                        return MacroScripts[localID].Instances[i];
                    }
                }
                return null;
            }
        }
        
		/// <summary>
		/// Updates or adds the given InstanceData to the list of known scripts.
		/// </summary>
		/// <param name="id"></param>
        public void UpdateScriptInstanceData(InstanceData id)
        {
            lock (scriptLock)
            {
                InstancesData IDs = null;
                if (MacroScripts.ContainsKey(id.localID))
                {
                    MacroScripts.TryGetValue(id.localID, out IDs);
                    IDs.Instances.Remove(id);
                    MacroScripts.Remove(id.localID);
                }
                else
                {
                    IDs = new InstancesData();
                    IDs.localID = id.localID;
                    IDs.AssemblyName = id.AssemblyName;
                }
                IDs.ItemIDs.Add(id.ItemID);
                IDs.Instances.Add(id);
                MacroScripts.Add(id.localID, IDs);
            }
        }

        /// <summary>
        /// Removes the given InstanceData from all known scripts.
        /// </summary>
        /// <param name="id"></param>
        public void RemoveScript(InstanceData id)
        {
            lock (scriptLock)
            {
                InstancesData IDs = null;
                if (MacroScripts.ContainsKey(id.localID))
                {
                    MacroScripts.TryGetValue(id.localID, out IDs);
                    IDs.Instances.Remove(id);
                    IDs.ItemIDs.Remove(id.ItemID);
                    MacroScripts.Remove(id.localID);
                    MacroScripts.Add(id.localID, IDs);
                }
            }
        }

        #endregion
        
		#region InstancesData
        
		public InstancesData GetMacroScript(uint localID)
        {
            lock (scriptLock)
            {
                InstancesData id = null;
                if (!MacroScripts.ContainsKey(localID))
                    return null;

                MacroScripts.TryGetValue(localID, out id);
                return id;
            }
        }

        public void UpdateMacroScript(InstancesData MacroData)
        {
            lock (scriptLock)
            {
                if (MacroScripts.ContainsKey(MacroData.localID))
                    MacroScripts.Remove(MacroData.localID);
                MacroScripts.Add(MacroData.localID, MacroData);
            }
        }

        #endregion
		
        internal void SetMinEventDelay(InstanceData ID, double delay)
        {
            ID.EventDelayTicks = (long)delay;
            UpdateScriptInstanceData(ID);
        }

        #endregion
		
        #region Other
        public static string ReadExternalWebsite(string URL)
        {
            // External IP Address (get your external IP locally)
            String externalIp = "";
            UTF8Encoding utf8 = new UTF8Encoding();

            WebClient webClient = new WebClient();
            try
            {
                externalIp = utf8.GetString(webClient.DownloadData(URL));
            }
            catch (Exception) { }
            return externalIp;
        }
        #endregion
    }
    
    #endregion
}