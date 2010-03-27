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
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static List<ScriptEngine> ScriptEngines =
                new List<ScriptEngine>();

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

        // How many seconds between re-reading config-file.
        // 0 = never. ScriptEngine will try to adjust to new config changes.
        public int RefreshConfigFileSeconds {
            get { return (int)(RefreshConfigFilens / 10000000); }
            set { RefreshConfigFilens = value * 10000000; }
        }

        public long RefreshConfigFilens;

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

        public ScriptEngine()
        {
            lock (ScriptEngines)
            {
                // Keep a list of ScriptEngines for shared threads
                // to process all instances
                ScriptEngines.Add(this);
            }
        }

        public void Initialise(IConfigSource config)
        {
            m_ConfigSource = config;
        }

        public void AddRegion(Scene Sceneworld)
        {
            m_log.Info("[" + ScriptEngineName + "]: ScriptEngine initializing");

            m_Scene = Sceneworld;

            // Make sure we have config
            if (ConfigSource.Configs[ScriptEngineName] == null)
                ConfigSource.AddConfig(ScriptEngineName);

            ScriptConfigSource = ConfigSource.Configs[ScriptEngineName];

            m_enabled = ScriptConfigSource.GetBoolean("Enabled", true);
            if (!m_enabled)
                return;

            // Create all objects we'll be using
            m_EventQueueManager = new EventQueueManager(this, Sceneworld);
            m_EventManager = new EventManager(this, true);

            // We need to start it
            m_ScriptManager = new ScriptManager(this);
            m_ScriptManager.Setup();
            m_AppDomainManager = new AppDomainManager(this);
            if (m_MaintenanceThread == null)
                m_MaintenanceThread = new MaintenanceThread();

            m_log.Info("[" + ScriptEngineName + "]: Reading configuration "+
                    "from config section \"" + ScriptEngineName + "\"");

            ReadConfig();

            m_Scene.StackModuleInterface<IScriptModule>(this);
        }

        public void RemoveRegion(Scene scene)
        {
            m_Scene.EventManager.OnScriptReset -= OnScriptReset;
            m_Scene.EventManager.OnGetScriptRunning -= OnGetScriptRunning;
            m_Scene.EventManager.OnStartScript -= OnStartScript;
            m_Scene.EventManager.OnStopScript -= OnStopScript;

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

        public void Shutdown()
        {
            // We are shutting down
            lock (ScriptEngines)
            {
                ScriptEngines.Remove(this);
                m_ScriptManager.Stop();
            }
        }

        public void ReadConfig()
        {
            RefreshConfigFileSeconds = ScriptConfigSource.GetInt("RefreshConfig", 0);

            if (m_EventQueueManager != null) m_EventQueueManager.ReadConfig();
            if (m_ScriptManager != null) m_ScriptManager.ReadConfig();
            if (m_AppDomainManager != null) m_AppDomainManager.ReadConfig();
            if (m_MaintenanceThread != null) m_MaintenanceThread.ReadConfig();
        }

        #region IRegionModule

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

        public bool IsSharedModule
        {
            get { return false; }
        }

        public bool PostObjectEvent(uint localID, EventParams p)
        {
            return m_EventQueueManager.AddToObjectQueue(localID, p.EventName,
                    p.DetectParams, p.Params);
        }

        public bool PostScriptEvent(UUID itemID, EventParams p)
        {
            uint localID = m_ScriptManager.GetLocalID(itemID);
            return m_EventQueueManager.AddToScriptQueue(localID, itemID,
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
            uint localID = m_ScriptManager.GetLocalID(itemID);
            if (localID == 0)
                return null;

            InstanceData id = m_ScriptManager.GetScript(localID, itemID);

            if (id == null)
                return null;

            DetectParams[] det = m_ScriptManager.GetDetectParams(id);

            if (number < 0 || number >= det.Length)
                return null;

            return det[number];
        }

        public int GetStartParameter(UUID itemID)
        {
            return m_ScriptManager.GetStartParameter(itemID);
        }

        public void SetMinEventDelay(UUID itemID, double delay)
        {
            InstanceData ID = null;
            m_ScriptManager.RunningScripts.TryGetValue(itemID, out ID);
            if(ID == null)
            {
                m_log.ErrorFormat("[{0}]: SetMinEventDelay found no InstanceData for script {1}.",ScriptEngineName,itemID.ToString());
            }
            ID.EventDelayTicks = (long)delay;
        }

        #endregion

        public void SetState(UUID itemID, string state)
        {
            uint localID = m_ScriptManager.GetLocalID(itemID);
            if (localID == 0)
                return;

            InstanceData id = m_ScriptManager.GetScript(localID, itemID);

            if (id == null)
                return;

            string currentState = id.State;

            if (currentState != state)
            {
                try
                {
                    m_EventManager.state_exit(localID);

                }
                catch (AppDomainUnloadedException)
                {
                    m_log.Error("[SCRIPT]: state change called when "+
                            "script was unloaded.  Nothing to worry about, "+
                            "but noting the occurance");
                }

                id.State = state;

                try
                {
                    int eventFlags = m_ScriptManager.GetStateEventFlags(localID,
                            itemID);

                    SceneObjectPart part = m_Scene.GetSceneObjectPart(localID);
                    if (part != null)
                        part.SetScriptEvents(itemID, eventFlags);

                    m_EventManager.state_entry(localID);
                }
                catch (AppDomainUnloadedException)
                {
                    m_log.Error("[SCRIPT]: state change called when "+
                    "script was unloaded.  Nothing to worry about, but "+
                    "noting the occurance");
                }
            }
        }

        public bool GetScriptState(UUID itemID)
        {
            uint localID = m_ScriptManager.GetLocalID(itemID);
            if (localID == 0)
                return false;

            InstanceData id = m_ScriptManager.GetScript(localID, itemID);
            if (id == null)
                return false;

            return id.Running;
        }

        public void SetScriptState(UUID itemID, bool state)
        {
            uint localID = m_ScriptManager.GetLocalID(itemID);
            if (localID == 0)
                return;

            InstanceData id = m_ScriptManager.GetScript(localID, itemID);
            if (id == null)
                return;

            if (!id.Disabled)
                id.Running = state;
        }

        public void ApiResetScript(UUID itemID)
        {
            uint localID = m_ScriptManager.GetLocalID(itemID);
            if (localID == 0)
                return;

            m_ScriptManager.ResetScript(localID, itemID);
        }

        public void ResetScript(UUID itemID)
        {
            uint localID = m_ScriptManager.GetLocalID(itemID);
            if (localID == 0)
                return;

            m_ScriptManager.ResetScript(localID, itemID);
        }

        public void OnScriptReset(uint localID, UUID itemID)
        {
            ResetScript(itemID);
        }

        public void OnStartScript(uint localID, UUID itemID)
        {
            InstanceData id = m_ScriptManager.GetScript(localID, itemID);
            if (id == null)
                return;        

            if (!id.Disabled)
                id.Running = true;
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
            uint localID = m_ScriptManager.GetLocalID(itemID);
            if (localID == 0)
                return;

            InstanceData id = m_ScriptManager.GetScript(localID, itemID);
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

        public IScriptApi GetApi(UUID itemID, string name)
        {
            return m_ScriptManager.GetApi(itemID, name);
        }

        public IScriptWorkItem QueueEventHandler(Object o)
        {
            return null;
        }

        public string GetXMLState(UUID itemID)
        {
            InstanceData instance = m_ScriptManager.GetScript(m_ScriptManager.GetLocalID(itemID), itemID);
            //if (instance == null)
                return "";
            string xml = Serialize(instance);

            XmlDocument sdoc = new XmlDocument();
            sdoc.LoadXml(xml);
            XmlNodeList rootL = sdoc.GetElementsByTagName("ScriptState");
            XmlNode rootNode = rootL[0];

            // Create <State UUID="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx">
            XmlDocument doc = new XmlDocument();
            XmlElement stateData = doc.CreateElement("", "State", "");
            XmlAttribute stateID = doc.CreateAttribute("", "UUID", "");
            stateID.Value = itemID.ToString();
            stateData.Attributes.Append(stateID);
            XmlAttribute assetID = doc.CreateAttribute("", "Asset", "");
            assetID.Value = instance.AssetID.ToString();
            stateData.Attributes.Append(assetID);
            XmlAttribute engineName = doc.CreateAttribute("", "Engine", "");
            engineName.Value = ScriptEngineName;
            stateData.Attributes.Append(engineName);
            doc.AppendChild(stateData);

            // Add <ScriptState>...</ScriptState>
            XmlNode xmlstate = doc.ImportNode(rootNode, true);
            stateData.AppendChild(xmlstate);

            string assemName = instance.AssemblyName;

            string fn = Path.GetFileName(assemName);

            string assem = String.Empty;

            if (File.Exists(assemName + ".text"))
            {
                FileInfo tfi = new FileInfo(assemName + ".text");

                if (tfi != null)
                {
                    Byte[] tdata = new Byte[tfi.Length];

                    try
                    {
                        FileStream tfs = File.Open(assemName + ".text",
                                FileMode.Open, FileAccess.Read);
                        tfs.Read(tdata, 0, tdata.Length);
                        tfs.Close();

                        assem = new System.Text.ASCIIEncoding().GetString(tdata);
                    }
                    catch (Exception e)
                    {
                        m_log.DebugFormat("[XEngine]: Unable to open script textfile {0}, reason: {1}", assemName + ".text", e.Message);
                    }
                }
            }
            else
            {
                FileInfo fi = new FileInfo(assemName);

                if (fi != null)
                {
                    Byte[] data = new Byte[fi.Length];

                    try
                    {
                        FileStream fs = File.Open(assemName, FileMode.Open, FileAccess.Read);
                        fs.Read(data, 0, data.Length);
                        fs.Close();

                        assem = System.Convert.ToBase64String(data);
                    }
                    catch (Exception e)
                    {
                        m_log.DebugFormat("[XEngine]: Unable to open script assembly {0}, reason: {1}", assemName, e.Message);
                    }

                }
            }

            string map = String.Empty;

            if (File.Exists(fn + ".map"))
            {
                FileStream mfs = File.Open(fn + ".map", FileMode.Open, FileAccess.Read);
                StreamReader msr = new StreamReader(mfs);

                map = msr.ReadToEnd();

                msr.Close();
                mfs.Close();
            }

            XmlElement assemblyData = doc.CreateElement("", "Assembly", "");
            XmlAttribute assemblyName = doc.CreateAttribute("", "Filename", "");

            assemblyName.Value = fn;
            assemblyData.Attributes.Append(assemblyName);

            assemblyData.InnerText = assem;

            stateData.AppendChild(assemblyData);

            XmlElement mapData = doc.CreateElement("", "LineMap", "");
            XmlAttribute mapName = doc.CreateAttribute("", "Filename", "");

            mapName.Value = fn + ".map";
            mapData.Attributes.Append(mapName);

            mapData.InnerText = map;

            stateData.AppendChild(mapData);
            return doc.InnerXml;
        }

        public bool CanBeDeleted(UUID itemID)
        {
            return true;
        }

        public ArrayList GetScriptErrors(UUID itemID)
        {
            return new ArrayList(m_ScriptManager.GetErrors(itemID));
        }

        public bool SetXMLState(UUID itemID, string xml)
        {
            //if (xml == String.Empty)
                return false;

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.LoadXml(xml);
            }
            catch (Exception)
            {
                m_log.Error("[XEngine]: Exception decoding XML data from region transfer");
                return false;
            }

            XmlNodeList rootL = doc.GetElementsByTagName("State");
            if (rootL.Count < 1)
                return false;

            XmlElement rootE = (XmlElement)rootL[0];

            if (rootE.GetAttribute("Engine") != ScriptEngineName)
                return false;

            //          On rez from inventory, that ID will have changed. It was only
            //          advisory anyway. So we don't check it anymore.
            //
            //            if (rootE.GetAttribute("UUID") != itemID.ToString())
            //                return;

            XmlNodeList stateL = rootE.GetElementsByTagName("ScriptState");

            if (stateL.Count != 1)
                return false;

            XmlElement stateE = (XmlElement)stateL[0];

            if (World.m_trustBinaries)
            {
                XmlNodeList assemL = rootE.GetElementsByTagName("Assembly");

                if (assemL.Count != 1)
                    return false;

                XmlElement assemE = (XmlElement)assemL[0];

                string fn = assemE.GetAttribute("Filename");
                string base64 = assemE.InnerText;

                string path = Path.Combine("ScriptEngines", World.RegionInfo.RegionID.ToString());
                path = Path.Combine(path, fn);

                if (!File.Exists(path))
                {
                    Byte[] filedata = Convert.FromBase64String(base64);

                    FileStream fs = File.Create(path);
                    fs.Write(filedata, 0, filedata.Length);
                    fs.Close();

                    fs = File.Create(path + ".text");
                    StreamWriter sw = new StreamWriter(fs);

                    sw.Write(base64);

                    sw.Close();
                    fs.Close();
                }
            }

            string statepath = Path.Combine("ScriptEngines", World.RegionInfo.RegionID.ToString());
            statepath = Path.Combine(statepath, itemID.ToString() + ".state");

            FileStream sfs = File.Create(statepath);
            StreamWriter ssw = new StreamWriter(sfs);

            ssw.Write(stateE.OuterXml);

            ssw.Close();
            sfs.Close();

            XmlNodeList mapL = rootE.GetElementsByTagName("LineMap");
            if (mapL.Count > 0)
            {
                XmlElement mapE = (XmlElement)mapL[0];

                string mappath = Path.Combine("ScriptEngines", World.RegionInfo.RegionID.ToString());
                mappath = Path.Combine(mappath, mapE.GetAttribute("Filename"));

                FileStream mfs = File.Create(mappath);
                StreamWriter msw = new StreamWriter(mfs);

                msw.Write(mapE.InnerText);

                msw.Close();
                mfs.Close();
            }
            return true;
        }

        public string Serialize(InstanceData instance)
        {
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

            XmlElement queue = xmldoc.CreateElement("", "Queue", "");

            int count = m_EventQueueManager.eventQueue.CountOf(instance.ItemID);

            while (count > 0)
            {
                EventQueueManager.QueueItemStruct QIS = m_EventQueueManager.eventQueue.Dequeue(instance.ItemID);
                EventParams ep = new EventParams(QIS.functionName, QIS.param, QIS.llDetectParams);
                m_EventQueueManager.eventQueue.Enqueue(QIS, QIS.itemID);
                count--;

                XmlElement item = xmldoc.CreateElement("", "Item", "");
                XmlAttribute itemEvent = xmldoc.CreateAttribute("", "event",
                                                                "");
                itemEvent.Value = ep.EventName;
                item.Attributes.Append(itemEvent);

                XmlElement parms = xmldoc.CreateElement("", "Params", "");

                foreach (Object o in ep.Params)
                    WriteTypedValue(xmldoc, parms, "Param", String.Empty, o);

                item.AppendChild(parms);

                XmlElement detect = xmldoc.CreateElement("", "Detected", "");

                foreach (DetectParams det in ep.DetectParams)
                {
                    XmlElement objectElem = xmldoc.CreateElement("", "Object",
                                                                 "");
                    XmlAttribute pos = xmldoc.CreateAttribute("", "pos", "");
                    pos.Value = det.OffsetPos.ToString();
                    objectElem.Attributes.Append(pos);

                    XmlAttribute d_linkNum = xmldoc.CreateAttribute("",
                            "linkNum", "");
                    d_linkNum.Value = det.LinkNum.ToString();
                    objectElem.Attributes.Append(d_linkNum);

                    XmlAttribute d_group = xmldoc.CreateAttribute("",
                            "group", "");
                    d_group.Value = det.Group.ToString();
                    objectElem.Attributes.Append(d_group);

                    XmlAttribute d_name = xmldoc.CreateAttribute("",
                            "name", "");
                    d_name.Value = det.Name.ToString();
                    objectElem.Attributes.Append(d_name);

                    XmlAttribute d_owner = xmldoc.CreateAttribute("",
                            "owner", "");
                    d_owner.Value = det.Owner.ToString();
                    objectElem.Attributes.Append(d_owner);

                    XmlAttribute d_position = xmldoc.CreateAttribute("",
                            "position", "");
                    d_position.Value = det.Position.ToString();
                    objectElem.Attributes.Append(d_position);

                    XmlAttribute d_rotation = xmldoc.CreateAttribute("",
                            "rotation", "");
                    d_rotation.Value = det.Rotation.ToString();
                    objectElem.Attributes.Append(d_rotation);

                    XmlAttribute d_type = xmldoc.CreateAttribute("",
                            "type", "");
                    d_type.Value = det.Type.ToString();
                    objectElem.Attributes.Append(d_type);

                    XmlAttribute d_velocity = xmldoc.CreateAttribute("",
                            "velocity", "");
                    d_velocity.Value = det.Velocity.ToString();
                    objectElem.Attributes.Append(d_velocity);

                    objectElem.AppendChild(
                        xmldoc.CreateTextNode(det.Key.ToString()));

                    detect.AppendChild(objectElem);
                }

                item.AppendChild(detect);

                queue.AppendChild(item);
            }

            rootElement.AppendChild(queue);

            XmlNode plugins = xmldoc.CreateElement("", "Plugins", "");
            DumpList(xmldoc, plugins,
                     new LSL_Types.list(AsyncCommandManager.GetSerializationData(this, instance.ItemID)));

            rootElement.AppendChild(plugins);

            /*if (instance.ScriptTask != null)
            {
                if (instance.ScriptTask.PermsMask != 0 && instance.ScriptTask.PermsGranter != UUID.Zero)
                {
                    XmlNode permissions = xmldoc.CreateElement("", "Permissions", "");
                    XmlAttribute granter = xmldoc.CreateAttribute("", "granter", "");
                    granter.Value = instance.ScriptTask.PermsGranter.ToString();
                    permissions.Attributes.Append(granter);
                    XmlAttribute mask = xmldoc.CreateAttribute("", "mask", "");
                    mask.Value = instance.ScriptTask.PermsMask.ToString();
                    permissions.Attributes.Append(mask);
                    rootElement.AppendChild(permissions);
                }
            }*/

            if (instance.EventDelayTicks > 0.0)
            {
                XmlElement eventDelay = xmldoc.CreateElement("", "MinEventDelay", "");
                eventDelay.AppendChild(xmldoc.CreateTextNode(instance.EventDelayTicks.ToString()));
                rootElement.AppendChild(eventDelay);
            }

            return xmldoc.InnerXml;
        }
        private void DumpList(XmlDocument doc, XmlNode parent,
                LSL_Types.list l)
        {
            foreach (Object o in l.Data)
                WriteTypedValue(doc, parent, "ListItem", "", o);
        }

        private void WriteTypedValue(XmlDocument doc, XmlNode parent,
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
    }
}
