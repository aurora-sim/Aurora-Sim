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
    #region InstanceData
	public class InstancesData
	{
		public string Source = "";
        public string AssemblyName = "";
        public uint localID = 0;
        public List<InstanceData> Instances = new List<InstanceData>();
	}
    public class InstanceData
    {
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

        public long EventDelayTicks = 0;
        public long NextEventTimeTicks = 0;
        public UUID AssetID;
        public string AssemblyName;
        public UUID ItemID;
        public uint localID;
        public string ClassID;
    }

    #endregion

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

        public Dictionary<uint, Dictionary<UUID, InstanceData>> Scripts =
            new Dictionary<uint, Dictionary<UUID, InstanceData>>();

        public Compiler LSLCompiler;

        public Scene World
        {
            get { return m_scriptEngine.World; }
        }

        #endregion

        #region Start/End Scripts

        public IEnumerator Microthread_StartScript(uint localID, UUID itemID, string Script,
                int startParam, bool postOnRez, StateSource stateSource)
        {
            // We will initialize and start the script.
            // It will be up to the script itself to hook up the correct events.
            string CompiledScriptFile = String.Empty;

            SceneObjectPart m_host = World.GetSceneObjectPart(localID);

            if (null == m_host)
            {
                m_log.ErrorFormat(
                    "[{0}]: Could not find scene object part corresponding " +
                    "to localID {1} to start script",
                    m_scriptEngine.ScriptEngineName, localID);

                throw new NullReferenceException();
            }


            UUID assetID = UUID.Zero;
            TaskInventoryItem taskInventoryItem = new TaskInventoryItem();
            if (m_host.TaskInventory.TryGetValue(itemID, out taskInventoryItem))
                assetID = taskInventoryItem.AssetID;

            ScenePresence presence =
                    World.GetScenePresence(taskInventoryItem.OwnerID);
            if (presence != null)
            {
                m_log.DebugFormat(
                    "[{0}]: Starting Script {1} in object {2} by avatar {3}.",
                    m_scriptEngine.ScriptEngineName, m_host.Inventory.GetInventoryItem(itemID).Name, m_host.Name, presence.Name);
            }
            else
            {
                m_log.DebugFormat(
                    "[{0}]: Starting Script {1} in object {2}.",
                    m_scriptEngine.ScriptEngineName, m_host.Inventory.GetInventoryItem(itemID).Name, m_host.Name);
            }
            
            CultureInfo USCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = USCulture;

            InstancesData MacroData = GetMacroScript(localID);
            InstanceData id = new InstanceData();
            
            string FormerScript = "";
            if(MacroData != null)
            	FormerScript = MacroData.Source;
            else
                MacroData = new InstancesData();
            
            try
            {
                // Compile (We assume LSL)
                LSLCompiler.PerformScriptCompile(Script,
                        assetID, taskInventoryItem.OwnerID, FormerScript, out CompiledScriptFile, out id.LineMap, out MacroData.Source, out id.ClassID);
            }
            catch (Exception ex)
            {
                ShowError(presence, m_host, postOnRez, itemID, ex, 1);
            }

            yield return null;
            IScript CompiledScript = null;
            try
            {
                CompiledScript =
                        m_scriptEngine.m_AppDomainManager.LoadScript(
                        CompiledScriptFile, "SecondLife."+id.ClassID, out id.AppDomain);
            }
            catch (Exception ex)
            {
                ShowError(presence, m_host, postOnRez, itemID, ex, 2);
            }
            //Register the sponsor
            //ISponsor scriptSponsor = new ScriptSponsor();
            //ILease lease = (ILease)RemotingServices.GetLifetimeService(CompiledScript as MarshalByRefObject);
            //lease.Register(scriptSponsor);
            //id.ScriptSponsor = scriptSponsor;

            id.Script = CompiledScript;
            id.StartParam = startParam;
            id.State = "default";
            id.Running = true;
            id.Disabled = false;
            id.AssetID = assetID;
            id.ItemID = itemID;
            
            MacroData.AssemblyName = CompiledScriptFile;
            MacroData.localID = localID;
            MacroData.Instances.Add(id);

            // Add it to our script memstruct
            SetScript(localID, itemID, id);
            SetMacroScript(MacroData);
            
            id.Apis = new Dictionary<string, IScriptApi>();

            ApiManager am = new ApiManager();

            foreach (string api in am.GetApis())
            {
                id.Apis[api] = am.CreateApi(api);
                id.Apis[api].Initialize(m_scriptEngine, m_host,
                        localID, itemID);
            }

            foreach (KeyValuePair<string, IScriptApi> kv in id.Apis)
            {
                CompiledScript.InitApi(kv.Key, kv.Value);
            }

            // Fire the first start-event
            int eventFlags =
                    m_scriptEngine.m_ScriptManager.GetStateEventFlags(
                    localID, itemID);

            m_host.SetScriptEvents(itemID, eventFlags);

            m_scriptEngine.m_EventQueueManager.AddToScriptQueue(
                    localID, itemID, "state_entry", new DetectParams[0],
                    new object[] { });

            if (postOnRez)
            {
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(
                    localID, itemID, "on_rez", new DetectParams[0],
                    new object[] { new LSL_Types.LSLInteger(startParam) });
            }

            if (stateSource == StateSource.AttachedRez)
            {
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(localID, itemID, "attach", new DetectParams[0],
                    new object[] { new LSL_Types.LSLString(m_host.AttachedAvatar.ToString()) });
            }
            else if (stateSource == StateSource.NewRez)
            {
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(localID, itemID, "changed", new DetectParams[0],
                                          new Object[] { new LSL_Types.LSLInteger(256) });
            }
            else if (stateSource == StateSource.PrimCrossing)
            {
                // CHANGED_REGION
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(localID, itemID, "changed", new DetectParams[0],
                                          new Object[] { new LSL_Types.LSLInteger(512) });
            }

            string[] warnings = LSLCompiler.GetWarnings();

            #region Warnings

            if (warnings != null && warnings.Length != 0)
            {
                if (presence != null && (!postOnRez))
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
                                m_host.AbsolutePosition, m_host.Name, m_host.UUID,
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
                if (presence != null && (!postOnRez))
                {
                    presence.ControllingClient.SendAgentAlertMessage(
                            "Compile successful", false);
                }
                if (presence != null)
                {
                    m_log.DebugFormat(
                        "[{0}]: Started Script {1} in object {2} by avatar {3} successfully.",
                        m_scriptEngine.ScriptEngineName, m_host.Inventory.GetInventoryItem(itemID).Name, m_host.Name, presence.Name);
                }
                else
                {
                    m_log.DebugFormat(
                        "[{0}]: Started Script {1} in object {2} successfully.",
                        m_scriptEngine.ScriptEngineName, m_host.Inventory.GetInventoryItem(itemID).Name, m_host.Name);
                }
            }

            #endregion
        }

        public void _StartScript(uint localID, UUID itemID, string Script,
                int startParam, bool postOnRez, StateSource stateSource)
        {
            // We will initialize and start the script.
            // It will be up to the script itself to hook up the correct events.
            string CompiledScriptFile = String.Empty;

            SceneObjectPart m_host = World.GetSceneObjectPart(localID);

            if (null == m_host)
            {
                m_log.ErrorFormat(
                    "[{0}]: Could not find scene object part corresponding " +
                    "to localID {1} to start script",
                    m_scriptEngine.ScriptEngineName, localID);

                throw new NullReferenceException();
            }


            UUID assetID = UUID.Zero;
            TaskInventoryItem taskInventoryItem = new TaskInventoryItem();
            if (m_host.TaskInventory.TryGetValue(itemID, out taskInventoryItem))
                assetID = taskInventoryItem.AssetID;

            ScenePresence presence =
                    World.GetScenePresence(taskInventoryItem.OwnerID);
            if (presence != null)
            {
                m_log.DebugFormat(
                    "[{0}]: Starting Script {1} in object {2} by avatar {3}.",
                    m_scriptEngine.ScriptEngineName, m_host.Inventory.GetInventoryItem(itemID).Name, m_host.Name, presence.Name);
            }
            else
            {
                m_log.DebugFormat(
                    "[{0}]: Starting Script {1} in object {2}.",
                    m_scriptEngine.ScriptEngineName, m_host.Inventory.GetInventoryItem(itemID).Name, m_host.Name);
            }

            CultureInfo USCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = USCulture;

            InstancesData MacroData = GetMacroScript(localID);
            InstanceData id = new InstanceData();
            
            string FormerScript = "";
            if(MacroData != null)
            	FormerScript = MacroData.Source;
            else
                MacroData = new InstancesData();
            
            try
            {
                // Compile (We assume LSL)
                LSLCompiler.PerformScriptCompile(Script,
                        assetID, taskInventoryItem.OwnerID, FormerScript, out CompiledScriptFile, out id.LineMap, out MacroData.Source, out id.ClassID);
            }
            catch (Exception ex)
            {
                ShowError(presence, m_host, postOnRez, itemID, ex, 1);
            }

            IScript CompiledScript = null;
            try
            {
                CompiledScript =
                        m_scriptEngine.m_AppDomainManager.LoadScript(
                        CompiledScriptFile, "SecondLife."+id.ClassID, out id.AppDomain);
            }
            catch (Exception ex)
            {
                ShowError(presence, m_host, postOnRez, itemID, ex, 2);
            }
            //Register the sponsor
            //ISponsor scriptSponsor = new ScriptSponsor();
            //ILease lease = (ILease)RemotingServices.GetLifetimeService(CompiledScript as MarshalByRefObject);
            //lease.Register(scriptSponsor);
            //id.ScriptSponsor = scriptSponsor;

            id.Script = CompiledScript;
            id.StartParam = startParam;
            id.State = "default";
            id.Running = true;
            id.Disabled = false;
            id.AssetID = assetID;
            id.ItemID = itemID;

            MacroData.AssemblyName = CompiledScriptFile;
            MacroData.localID = localID;
            MacroData.Instances.Add(id);

            // Add it to our script memstruct
            SetScript(localID, itemID, id);
            SetMacroScript(MacroData);

            id.Apis = new Dictionary<string, IScriptApi>();

            ApiManager am = new ApiManager();

            foreach (string api in am.GetApis())
            {
                id.Apis[api] = am.CreateApi(api);
                id.Apis[api].Initialize(m_scriptEngine, m_host,
                        localID, itemID);
            }

            foreach (KeyValuePair<string, IScriptApi> kv in id.Apis)
            {
                CompiledScript.InitApi(kv.Key, kv.Value);
            }

            // Fire the first start-event
            int eventFlags =
                    m_scriptEngine.m_ScriptManager.GetStateEventFlags(
                    localID, itemID);

            m_host.SetScriptEvents(itemID, eventFlags);

            m_scriptEngine.m_EventQueueManager.AddToScriptQueue(
                    localID, itemID, "state_entry", new DetectParams[0],
                    new object[] { });

            
            if (postOnRez)
            {
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(
                    localID, itemID, "on_rez", new DetectParams[0],
                    new object[] { new LSL_Types.LSLInteger(startParam) });
            }

            if (stateSource == StateSource.AttachedRez)
            {
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(localID, itemID, "attach", new DetectParams[0],
                    new object[] { new LSL_Types.LSLString(m_host.AttachedAvatar.ToString()) });
            }
            else if (stateSource == StateSource.NewRez)
            {
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(localID, itemID, "changed", new DetectParams[0],
                                          new Object[] { new LSL_Types.LSLInteger(256) });
            }
            else if (stateSource == StateSource.PrimCrossing)
            {
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(localID, itemID, "changed", new DetectParams[0],
                                          new Object[] { new LSL_Types.LSLInteger(512) });
            }

            string[] warnings = LSLCompiler.GetWarnings();

            #region Warnings

            if (warnings != null && warnings.Length != 0)
            {
                if (presence != null && (!postOnRez))
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
                                m_host.AbsolutePosition, m_host.Name, m_host.UUID,
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
                if (presence != null && (!postOnRez))
                {
                    presence.ControllingClient.SendAgentAlertMessage(
                            "Compile successful", false);
                }
                if (presence != null)
                {
                    m_log.DebugFormat(
                        "[{0}]: Started Script {1} in object {2} by avatar {3} successfully.",
                        m_scriptEngine.ScriptEngineName, m_host.Inventory.GetInventoryItem(itemID).Name, m_host.Name, presence.Name);
                }
                else
                {
                    m_log.DebugFormat(
                        "[{0}]: Started Script {1} in object {2} successfully.",
                        m_scriptEngine.ScriptEngineName, m_host.Inventory.GetInventoryItem(itemID).Name, m_host.Name);
                }
            }

            #endregion
        }

        public IEnumerator Microthread_StopScript(uint localID, UUID itemID)
        {
            InstanceData id = GetScript(localID, itemID);
            if (id == null)
                throw new NullReferenceException();

            ReleaseControls(localID, itemID);
            m_scriptEngine.m_EventManager.state_exit(localID);
            m_scriptEngine.m_EventQueueManager.RemoveFromQueue(itemID);
           
            // Stop long command on script
            AsyncCommandManager.RemoveScript(m_scriptEngine, localID, itemID);
            yield return null;

            try
            {
                // Get AppDomain
                // Tell script not to accept new requests
                id.Running = false;
                id.Disabled = true;
                AppDomain ad = id.AppDomain;
                id.Script.Close();
                
                // Remove from internal structure
                RemoveScript(localID, itemID);

                // Tell AppDomain that we have stopped script
                m_scriptEngine.m_AppDomainManager.StopScript(ad);
            }
            catch (Exception e) // LEGIT: User Scripting
            {
                m_log.Error("[" +
                            m_scriptEngine.ScriptEngineName +
                            "]: Exception stopping script localID: " +
                            localID + " LLUID: " + itemID.ToString() +
                            ": " + e.ToString());
            }
        }

        public void _StopScript(uint localID, UUID itemID)
        {
            InstanceData id = GetScript(localID, itemID);
            if (id == null)
                throw new NullReferenceException();

            ReleaseControls(localID, itemID);
            m_scriptEngine.m_EventManager.state_exit(localID);
            m_scriptEngine.m_EventQueueManager.RemoveFromQueue(itemID);

            // Stop long command on script
            AsyncCommandManager.RemoveScript(m_scriptEngine, localID, itemID);
            
            try
            {
                // Get AppDomain
                // Tell script not to accept new requests
                id.Running = false;
                id.Disabled = true;
                AppDomain ad = id.AppDomain;
                id.Script.Close();

                // Remove from internal structure
                RemoveScript(localID, itemID);

                // Tell AppDomain that we have stopped script
                m_scriptEngine.m_AppDomainManager.StopScript(ad);
            }
            catch (Exception e) // LEGIT: User Scripting
            {
                m_log.Error("[" +
                            m_scriptEngine.ScriptEngineName +
                            "]: Exception stopping script localID: " +
                            localID + " LLUID: " + itemID.ToString() +
                            ": " + e.ToString());
            }
        }

        private void ShowError(ScenePresence presence, SceneObjectPart m_host, bool postOnRez, UUID itemID, Exception e, int stage)
        {
            if (presence != null && (!postOnRez))
                presence.ControllingClient.SendAgentAlertMessage(
                        "Script saved with errors, check debug window!",
                        false);
            AddError(itemID, e.Message.ToString());

            try
            {
                // DISPLAY ERROR INWORLD
                string text = "Error compiling script in stage "+stage+":\n" +
                        e.Message.ToString();
                m_log.Error(text);
                if (text.Length > 1100)
                    text = text.Substring(0, 1099);

                World.SimChat(Utils.StringToBytes(text),
                        ChatTypeEnum.DebugChannel, 2147483647,
                        m_host.AbsolutePosition, m_host.Name, m_host.UUID,
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

        private void ReleaseControls(uint localID, UUID itemID)
        {
            SceneObjectPart part = m_scriptEngine.World.GetSceneObjectPart(itemID);

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
                    ScenePresence presence = m_scriptEngine.World.GetScenePresence(permsGranter);
                    if (presence != null)
                        presence.UnRegisterControlEventsToScript(localID, itemID);
                }
            }
        }

        #endregion

        #region Error Reporting
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

        private Dictionary<UUID, List<string>> m_Errors = new Dictionary<UUID, List<string>>();
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
        }

        public void Initialize()
        {
            // Create our compiler
            LSLCompiler = new Compiler(m_scriptEngine);
        }

        #region Config Reader

        public void ReadConfig()
        {
            // TODO: Requires sharing of all ScriptManagers to single thread
            PrivateThread = true;
            LoadUnloadMaxQueueSize = m_scriptEngine.ScriptConfigSource.GetInt(
                    "LoadUnloadMaxQueueSize", 100);
            MinMicrothreadScriptThreshold = m_scriptEngine.ScriptConfigSource.GetInt(
                    "LoadUnloadMaxQueueSizeBeforeMicrothreading", 100);
        }

        #endregion

        public void Setup()
        {
            ReadConfig();
            Initialize();
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
            foreach (KeyValuePair<uint, Dictionary<UUID, InstanceData>> script in Scripts)
            {
                foreach (KeyValuePair<UUID, InstanceData> innerscript in script.Value)
                {
                    StopScript(script.Key, innerscript.Key);
                }
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

        public void DoScriptsLoadUnload()
        {
            if (!m_started)
                return;

            List<IEnumerator> parts = new List<IEnumerator>();
            lock (LUQueue)
            {
                if (LUQueue.Count > 0)
                {
                    if (LUQueue.Count > MinMicrothreadScriptThreshold)
                    {
                        int i = 0;
                        while (i < LUQueue.Count)
                        {
                            LUStruct item = LUQueue.Dequeue();

                            if (item.Action == LUType.Unload)
                            {
                                parts.Add(Microthread_StopScript(item.localID, item.itemID));
                                Scripts.Remove(item.localID);
                            }
                            else if (item.Action == LUType.Load)
                            {
                                if (GetScript(item.localID, item.itemID) != null)
                                {
                                    parts.Add(Microthread_StopScript(item.localID, item.itemID));
                                    Scripts.Remove(item.localID);
                                }
                                parts.Add(Microthread_StartScript(item.localID, item.itemID, item.script,
                                             item.startParam, item.postOnRez, item.stateSource));
                            }
                            i++;
                        }
                    }
                    else
                    {
                        int i = 0;
                        while (i < LUQueue.Count)
                        {
                            LUStruct item = LUQueue.Dequeue();

                            if (item.Action == LUType.Unload)
                            {
                                _StopScript(item.localID, item.itemID);
                                Scripts.Remove(item.localID);
                            }
                            else if (item.Action == LUType.Load)
                            {
                                if (GetScript(item.localID, item.itemID) != null)
                                {
                                    _StopScript(item.localID, item.itemID);
                                    Scripts.Remove(item.localID);
                                }
                                _StartScript(item.localID, item.itemID, item.script,
                                             item.startParam, item.postOnRez, item.stateSource);
                            }
                            i++;
                        }
                    }
                }
            }
            lock (parts)
            {
                int i = 0;
                while (parts.Count > 0 && i < 1000)
                {
                    i++;

                    bool running = false;
                    try
                    {
                        running = parts[i % parts.Count].MoveNext();
                    }
                    catch (Exception ex)
                    {
                    }

                    if (!running)
                        parts.Remove(parts[i % parts.Count]);
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

                InstanceData data = GetScript(localID, itemID);
                if (data != null)
                {
                    if (!data.Running || data.Disabled)
                    {
                        m_log.Info("Script !Running or Disabled");
                        return;
                    }
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

        #region Perform event execution in script

        // Execute a LL-event-function in Script
        internal IEnumerable ExecuteEvent(uint localID, UUID itemID,
                string FunctionName, DetectParams[] qParams, object[] args)
        {
            int Stage = 0;
            InstanceData id = null;
            //;^) Ewe Loon,fix 

            try
            {
                Stage = 1;
                id = GetScript(localID, itemID);
            }
            catch (Exception e)
            {
                SceneObjectPart ob = m_scriptEngine.World.GetSceneObjectPart(localID);
                m_log.InfoFormat("[Script Error] ,{0},{1},@{2},{3},{4},{5}", ob.Name, FunctionName, Stage, e.Message, qParams.Length, detparms.Count);
                throw e;
            }

            if (id == null)
                throw new NullReferenceException();

            if (!id.Running)
                throw new NotSupportedException();

            try
            {
                Stage = 2;
                if (qParams.Length > 0)
                    detparms[id] = qParams;
            }
            catch (Exception e)
            {
                SceneObjectPart ob = m_scriptEngine.World.GetSceneObjectPart(localID);
                m_log.InfoFormat("[Script Error] ,{0},{1},@{2},{3},{4},{5}", ob.Name, FunctionName, Stage, e.Message, qParams.Length, detparms.Count);
                throw e;
            }

            Stage = 3;

            if (id.EventDelayTicks != 0)
            {
                if (DateTime.Now.Ticks < id.NextEventTimeTicks)
                    throw new Exception();

                id.NextEventTimeTicks = DateTime.Now.Ticks + id.EventDelayTicks;
            }


            yield return null;

            try
            {
                id.Script.ExecuteEvent(id.State, FunctionName, args);
            }
            catch (SelfDeleteException) // Must delete SOG
            {
                SceneObjectPart part =
                    m_scriptEngine.World.GetSceneObjectPart(localID);
                if (part != null && part.ParentGroup != null)
                    m_scriptEngine.World.DeleteSceneObject(
                        part.ParentGroup, false);
            }
            catch (ScriptDeleteException) // Must delete item
            {
                SceneObjectPart part =
                    m_scriptEngine.World.GetSceneObjectPart(
                        localID);
                if (part != null && part.ParentGroup != null)
                    part.Inventory.RemoveInventoryItem(itemID);
            }
            catch (Exception e)
            {
                if (qParams.Length > 0)
                    detparms.Remove(id);
                SceneObjectPart ob = m_scriptEngine.World.GetSceneObjectPart(localID);
                m_log.InfoFormat("[Script Error] ,{0},{1},@{2},{3},{4},{5}", ob.Name, FunctionName, Stage, e.Message, qParams.Length, detparms.Count);
                throw e;
            }

            try
            {
                Stage = 4;

                if (qParams.Length > 0)
                    detparms.Remove(id);
            }
            catch (Exception e)
            {
                SceneObjectPart ob = m_scriptEngine.World.GetSceneObjectPart(localID);
                m_log.InfoFormat("[Script Error] ,{0},{1},@{2},{3},{4},{5}", ob.Name, FunctionName, Stage, e.Message, qParams.Length, detparms.Count);
                throw e;
            }
        }

        public uint GetLocalID(UUID itemID)
        {
            foreach (KeyValuePair<uint, Dictionary<UUID, InstanceData> > k
                    in Scripts)
            {
                if (k.Value.ContainsKey(itemID))
                    return k.Key;
            }
            return 0;
        }

        public int GetStateEventFlags(uint localID, UUID itemID)
        {
            try
            {
                InstanceData id = GetScript(localID, itemID);
                if (id == null)
                {
                    return 0;
                }
                int evflags = id.Script.GetStateEventFlags(id.State);

                return (int)evflags;
            }
            catch (Exception)
            {
            }

            return 0;
        }

        #endregion

        #region Internal functions to keep track of script

        public List<UUID> GetScriptKeys(uint localID)
        {
            if (Scripts.ContainsKey(localID) == false)
                return new List<UUID>();

            Dictionary<UUID, InstanceData> Obj;
            Scripts.TryGetValue(localID, out Obj);

            return new List<UUID>(Obj.Keys);
        }

        public InstanceData GetScript(uint localID, UUID itemID)
        {
            lock (scriptLock)
            {
                InstanceData id = null;

                if (Scripts.ContainsKey(localID) == false)
                    return null;

                Dictionary<UUID, InstanceData> Obj;
                Scripts.TryGetValue(localID, out Obj);
                if (Obj==null) return null;
                if (Obj.ContainsKey(itemID) == false)
                    return null;

                // Get script
                Obj.TryGetValue(itemID, out id);
                return id;
            }
        }
        Dictionary<uint, InstancesData> MacroScripts = new Dictionary<uint, InstancesData>();
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

        public void SetMacroScript(InstancesData id)
        {
            lock (scriptLock)
            {
                if (MacroScripts.ContainsKey(id.localID))
                	MacroScripts.Remove(id.localID);

                MacroScripts.Add(id.localID, id);
            }
        }

        public void SetScript(uint localID, UUID itemID, InstanceData id)
        {
            lock (scriptLock)
            {
                // Create object if it doesn't exist
                if (Scripts.ContainsKey(localID) == false)
                {
                    Scripts.Add(localID, new Dictionary<UUID, InstanceData>());
                }

                // Delete script if it exists
                Dictionary<UUID, InstanceData> Obj;
                Scripts.TryGetValue(localID, out Obj);
                if (Obj.ContainsKey(itemID) == true)
                {
                    InstanceData ID;
                    Obj.TryGetValue(itemID, out ID);
                    ID.Disabled = true;
                    ID.Running = false;
                    ID.Script.Close();
                    ID.Script = null;
                    ID = null;
                    Obj.Remove(itemID);
                }

                Scripts.Remove(localID);
                // Add to object
                Obj.Add(itemID, id);
                Scripts.Add(localID, Obj);
            }
        }

        internal void SetScriptInstanceData(InstanceData ID)
        {
            lock (scriptLock)
            {
                // Create object if it doesn't exist
                if (Scripts.ContainsKey(ID.localID) == false)
                {
                    Scripts.Add(ID.localID, new Dictionary<UUID, InstanceData>());
                }

                // Delete script if it exists
                Dictionary<UUID, InstanceData> Obj = new Dictionary<UUID, InstanceData>();
                Scripts.TryGetValue(ID.localID, out Obj);
                if (Obj.ContainsKey(ID.ItemID) == true)
                {
                    Obj.Remove(ID.ItemID);
                }
                Scripts.Remove(ID.localID);
                // Add to object
                Obj.Add(ID.ItemID, ID);
                Scripts.Add(ID.localID, Obj);
            }
        }

        public void RemoveScript(uint localID, UUID itemID)
        {
            if (localID == 0)
                localID = GetLocalID(itemID);

            // Don't have that object?
            if (Scripts.ContainsKey(localID) == false)
                return;
            // Delete script if it exists
            Dictionary<UUID, InstanceData> Obj;
            Scripts.TryGetValue(localID, out Obj);
            if(Obj == null)
                return;
            if (Obj.ContainsKey(itemID) == true)
            {
                Obj.Remove(itemID);
            }
        }

        internal void SetMinEventDelay(InstanceData ID, double delay)
        {
            ID.EventDelayTicks = (long)delay;
            SetScriptInstanceData(ID);
        }

        #endregion

        #region Get/Set DetectParams and start parameters

        public DetectParams[] GetDetectParams(InstanceData id)
        {
            if (detparms.ContainsKey(id))
                return detparms[id];

            return null;
        }

        public int GetStartParameter(UUID itemID)
        {
            uint localID = GetLocalID(itemID);
            InstanceData id = GetScript(localID, itemID);

            if (id == null)
                return 0;

            return id.StartParam;
        }

        public IScriptApi GetApi(UUID itemID, string name)
        {
            uint localID = GetLocalID(itemID);

            InstanceData id = GetScript(localID, itemID);
            if (id == null)
                return null;

            if (id.Apis.ContainsKey(name))
                return id.Apis[name];

            return null;
        }
        #endregion

        #region Reset
        public void ResetScript(uint localID, UUID itemID)
        {
            InstanceData id = GetScript(localID, itemID);
            ReleaseControls(localID, itemID);
            m_scriptEngine.m_EventManager.state_exit(localID);
            m_scriptEngine.m_EventQueueManager.RemoveFromQueue(itemID);
            m_scriptEngine.m_ScriptManager.RemoveScript(localID, itemID);

            id.Running = true;
            id.State = "default";
            SetScript(localID, itemID, id);

            m_scriptEngine.m_EventQueueManager.AddToScriptQueue(
                    localID, itemID, "state_entry", new DetectParams[0],
                    new object[] { });
        }
        #endregion
    }
}