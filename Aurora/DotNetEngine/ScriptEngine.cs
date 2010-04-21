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
using OpenSim.Region.ScriptEngine.Shared.Api;

namespace OpenSim.Region.ScriptEngine.DotNetEngine
{
    [Serializable]
    public class ScriptEngine : INonSharedRegionModule, IScriptEngine, IScriptModule
    {
        #region Declares 

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_Scene;
        public Scene World
        {
            get { return m_Scene; }
        }

        // Handles and queues incoming events from OpenSim
        public EventManager m_EventManager;

        // Executes events, handles script threads
        public EventQueueManager m_EventQueueManager;

        // Load, unload and execute scripts
        public ScriptManager m_ScriptManager;

        // Handles loading/unloading of scripts into AppDomains
        public AppDomainManager m_AppDomainManager;

        //Handles saving of states.
        public StateSaverQueue m_StateQueue;

        // Thread that does different kinds of maintenance,
        // for example refreshing config and killing scripts
        // that has been running too long
        public static MaintenanceThread m_MaintenanceThread;

        private  IConfigSource m_ConfigSource;
        public IConfig ScriptConfigSource;
        private bool m_enabled = false;

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
            get { return "Aurora.DotNetEngine"; }
        }
        
        public IScriptModule ScriptModule
        {
            get { return this; }
        }

        public event ScriptRemoved OnScriptRemoved;
        public event ObjectRemoved OnObjectRemoved;
        private IXmlRpcRouter m_XmlRpcRouter;
        public IScriptProtectionModule ScriptProtection;
        #endregion

        #region Constructor and Shutdown
        
        public void Shutdown()
        {
            // We are shutting down
            m_ScriptManager.Stop();
        }

        #endregion

        #region INonSharedRegionModule

        public void Initialise(IConfigSource config)
        {
            m_ConfigSource = config;
        }

        public void AddRegion(Scene scene)
        {
        	m_log.Info("[" + ScriptEngineName + "]: ScriptEngine initializing");

            m_Scene = scene;

            // Make sure we have config
            if (ConfigSource.Configs[ScriptEngineName] == null)
                ConfigSource.AddConfig(ScriptEngineName);

            ScriptConfigSource = ConfigSource.Configs[ScriptEngineName];

            m_enabled = ScriptConfigSource.GetBoolean("Enabled", true);
            if (!m_enabled)
                return;

            // Create all objects we'll be using
            ScriptProtection = (IScriptProtectionModule)new ScriptProtectionModule(m_ConfigSource, this);
            
            m_EventQueueManager = new EventQueueManager(this, scene);
            m_EventManager = new EventManager(this, true);
            m_StateQueue = new StateSaverQueue(this);

            // We need to start it
            m_ScriptManager = new ScriptManager(this);
            
            m_AppDomainManager = new AppDomainManager(this);
            
            if (m_MaintenanceThread == null)
                m_MaintenanceThread = new MaintenanceThread(this);

            m_log.Info("[" + ScriptEngineName + "]: Reading configuration "+
                    "from config section \"" + ScriptEngineName + "\"");

            m_Scene.StackModuleInterface<IScriptModule>(this);

            m_XmlRpcRouter = m_Scene.RequestModuleInterface<IXmlRpcRouter>();
            if (m_XmlRpcRouter != null)
            {
                OnScriptRemoved += m_XmlRpcRouter.ScriptRemoved;
                OnObjectRemoved += m_XmlRpcRouter.ObjectRemoved;
            }
        }

        public void RemoveRegion(Scene scene)
        {
            m_Scene.EventManager.OnScriptReset -= OnScriptReset;
            m_Scene.EventManager.OnGetScriptRunning -= OnGetScriptRunning;
            m_Scene.EventManager.OnStartScript -= OnStartScript;
            m_Scene.EventManager.OnStopScript -= OnStopScript;
            
            if (m_XmlRpcRouter != null)
            {
                OnScriptRemoved -= m_XmlRpcRouter.ScriptRemoved;
                OnObjectRemoved -= m_XmlRpcRouter.ObjectRemoved;
            }

            m_Scene.UnregisterModuleInterface<IScriptModule>(this);
            
			m_ScriptManager.Stop();
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_enabled)
                return;

            m_EventManager.HookUpEvents();

            m_Scene.EventManager.OnScriptReset += OnScriptReset;
            m_Scene.EventManager.OnGetScriptRunning += OnGetScriptRunning;
            m_Scene.EventManager.OnStartScript += OnStartScript;
            m_Scene.EventManager.OnStopScript += OnStopScript;

            m_ScriptManager.Start();
        }

        public void Close()
        {
        }

        public Type ReplaceableInterface 
        {
            get { return null; }
        }

        public string Name
        {
            get { return ScriptEngineName; }
        }
        
		#endregion

		#region Post Object Events
		
        public bool PostObjectEvent(uint localID, EventParams p)
        {
            return m_EventQueueManager.AddToObjectQueue(localID, p.EventName,
                    p.DetectParams, p.Params);
        }

        public bool PostScriptEvent(UUID itemID, EventParams p)
        {
            InstanceData ID = m_ScriptManager.GetScriptByItemID(itemID);
            return m_EventQueueManager.AddToScriptQueue(ID,
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
            SceneObjectPart part = m_Scene.GetSceneObjectPart(itemID);
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
            InstanceData id = m_ScriptManager.GetScriptByItemID(itemID);

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
            InstanceData id = m_ScriptManager.GetScriptByItemID(itemID);

            if (id == null)
                return 0;

            return id.StartParam;
        }

        public void SetMinEventDelay(UUID itemID, double delay)
        {
            InstanceData ID = m_ScriptManager.GetScriptByItemID(itemID);
            if(ID == null)
            {
                m_log.ErrorFormat("[{0}]: SetMinEventDelay found no InstanceData for script {1}.",ScriptEngineName,itemID.ToString());
                return;
            }
            m_ScriptManager.SetMinEventDelay(ID, delay);
        }

        #endregion

        #region Get/Set Script States/Running

        public void SetState(UUID itemID, string state)
        {
            InstanceData id = m_ScriptManager.GetScriptByItemID(itemID);

            if (id == null)
                return;

            if (id.State != state)
            {
            	m_EventManager.state_exit(id.localID);
            	id.State = state;
            	int eventFlags = id.Script.GetStateEventFlags(id.State);

            	id.part.SetScriptEvents(itemID, eventFlags);

            	m_EventManager.state_entry(id.localID);
            	m_ScriptManager.UpdateScriptInstanceData(id);
            }
        }

        public bool GetScriptState(UUID itemID)
        {
            InstanceData id = m_ScriptManager.GetScriptByItemID(itemID);
            if (id == null)
                return false;

            return id.Running;
        }

        public void SetScriptState(UUID itemID, bool state)
        {
            InstanceData id = m_ScriptManager.GetScriptByItemID(itemID);
            if (id == null)
                return;

            if (!id.Disabled)
                id.Running = state;
        }

        #endregion

        #region Reset

        public void ApiResetScript(UUID itemID)
        {
            InstanceData ID = m_ScriptManager.GetScriptByItemID(itemID);
            if (ID == null)
                return;

            ID.Reset();
        }

        public void ResetScript(UUID itemID)
        {
            InstanceData ID = m_ScriptManager.GetScriptByItemID(itemID);
            if (ID == null)
                return;

            ID.Reset();
        }

        public void OnScriptReset(uint localID, UUID itemID)
        {
            InstanceData ID = m_ScriptManager.GetScript(localID, itemID);
            if (ID == null)
                return;

            ID.Reset();
        }
        #endregion

        #region Start/End/Suspend Scripts

        public void OnStartScript(uint localID, UUID itemID)
        {
            InstanceData id = m_ScriptManager.GetScript(localID, itemID);
            if (id == null)
                return;        

            if (!id.Disabled)
                id.Running = true;
            m_ScriptManager.StartScript(localID, itemID, id.Source, id.StartParam, true, id.stateSource);
        }

        public void OnStopScript(uint localID, UUID itemID)
        {
            InstanceData id = m_ScriptManager.GetScript(localID, itemID);
            if (id == null)
                return;        
            
            id.Running = false;
            m_ScriptManager.StopScript(localID, itemID);
        }

        public void OnGetScriptRunning(IClientAPI controllingClient,
                UUID objectID, UUID itemID)
        {
            InstanceData id = m_ScriptManager.GetScriptByItemID(itemID);
            if (id == null)
                return;        

            IEventQueue eq = World.RequestModuleInterface<IEventQueue>();
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
            InstanceData ID = m_ScriptManager.GetScriptByItemID(itemID);
            if (ID == null)
                return;
            ID.Suspended = true;
        }

        public void ResumeScript(UUID itemID)
        {
            InstanceData ID = m_ScriptManager.GetScriptByItemID(itemID);
            if (ID == null)
                return;
            ID.Suspended = false;
        }

        #endregion

        #region GetScriptAPI

        public IScriptApi GetApi(UUID itemID, string name)
        {
            InstanceData id = m_ScriptManager.GetScriptByItemID(itemID);
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
            InstanceData instance = m_ScriptManager.GetScriptByItemID(itemID);
            IEnumerator enumerator = instance.Serialize();
            bool running = true;
            while (running)
            {
                try
                {
                    running = enumerator.MoveNext();
                }
                catch (Exception) { }
            }
            return instance.CurrentStateXML;
        }

        public ArrayList GetScriptErrors(UUID itemID)
        {
            return new ArrayList(m_ScriptManager.GetErrors(itemID));
        }

        public bool SetXMLState(UUID itemID, string xml)
        {
            InstanceData instance = m_ScriptManager.GetScriptByItemID(itemID);
            instance.Deserialize(xml);
            return true;
        }
        #endregion
    }
}
