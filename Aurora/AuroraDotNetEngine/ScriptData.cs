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
using System.Xml;
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
using Aurora.Framework;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    #region ScriptData

    public class ScriptData
    {
        #region Constructor

        public ScriptData(ScriptEngine engine)
        {
            m_ScriptEngine = engine;
            ScriptFrontend = Aurora.DataManager.DataManager.RequestPlugin<IScriptDataConnector>("IScriptDataConnector");
        }

        #endregion

        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ScriptEngine m_ScriptEngine;
        public Scene World;
        public IScript Script;
        public string State;
        public bool Running = true;
        public bool Disabled = false;
        public bool Suspended = false;
        public bool Loading = true;
        public string Source;
        public int StartParam;
        public StateSource stateSource;
        public AppDomain AppDomain;
        public Dictionary<string, IScriptApi> Apis = new Dictionary<string, IScriptApi>();
        public bool TimerQueued = false;
        public bool CollisionInQueue = false;
        public bool TouchInQueue = false;
        public bool LandCollisionInQueue = false;
        public List<Changed> ChangedInQueue = new List<Changed>();
        public int LastControlLevel = 0;
        public int ControlEventsInQueue = 0;
        public bool StartedFromSavedState = false;
        public UUID RezzedFrom = UUID.Zero; // If rezzed from llRezObject, this is not Zero
        public int VersionID = 0;

        public SceneObjectPart part;

        public long EventDelayTicks = 0;
        public long NextEventTimeTicks = 0;
        public string AssemblyName;
        //This is the UUID of the actual script.
        public UUID ItemID;
        public UUID UserInventoryItemID;
        public bool PostOnRez;
        public TaskInventoryItem InventoryItem;
        public ScenePresence presence = null;
        public DetectParams[] LastDetectParams = null;
        public Object[] PluginData = new Object[0];
        private StateSave LastStateSave = null;
        private IScriptDataConnector ScriptFrontend;
        
        #endregion

        #region Close Script

        /// <summary>
        /// This closes the scrpit, removes it from any known spots, and disposes of itself.
        /// </summary>
        /// <returns></returns>
        public void CloseAndDispose(bool Silent)
        {
            if (ScriptEngine.NeedsRemoved.ContainsKey(ItemID))
                ScriptEngine.NeedsRemoved.Remove(ItemID);

            if (!Silent)
            {
                if (Script != null)
                {
                    //Save the state
                    //Must be called directly or it wont be processed in time
                    SerializeDatabase();
                    //Fire this directly so its not closed before its fired
                    SetEventParams(new DetectParams[0]);
                    EventQueue.ProcessQIS(new QueueItemStruct()
                    {
                        ID = this,
                        CurrentlyAt = Guid.Empty,
                        functionName = "state_exit",
                        param = new object[0],
                        llDetectParams = new DetectParams[0],
                        VersionID = VersionID
                    });
                }
            }
            VersionID++;
            ScriptEngine.NeedsRemoved.Add(ItemID, VersionID);
            ReleaseControls();
            // Tell script not to accept new requests
            //These are fine to set as the state wont be saved again
            Running = false;
            Disabled = true;

            // Remove from internal structure
            ScriptEngine.ScriptProtection.RemoveScript(this);

            //ScriptEngine.NeedsRemoved.Add(ItemID, VersionID);

            if (m_ScriptEngine.Errors.ContainsKey(ItemID))
                m_ScriptEngine.Errors.Remove(ItemID);

            #region Clean out script parts
            part.AngularVelocity = Vector3.Zero; // Removed in SL
            part.ScheduleFullUpdate(); // Send changes to client.
            #endregion

            if (Script != null)
            {
                // Stop long command on script
                AsyncCommandManager.RemoveScript(m_ScriptEngine, World, part.LocalId, ItemID);
                ILease lease = (ILease)RemotingServices.GetLifetimeService(Script as MarshalByRefObject);
                if(lease != null)
                    lease.Unregister(Script.Sponsor);
                try
                {
                    Script.Close();
                    Script.Dispose();
                    Script = null;
                }
                catch
                {
                }
            }
            try
            {
                if (AppDomain == null)
                    return;

                try
                {
                    // Tell AppDomain that we have stopped script
                    m_ScriptEngine.AppDomainManager.UnloadScriptAppDomain(AppDomain);
                    AppDomain = null;
                }
                //Legit: If the script had an error, this can happen... really shouldn't, but it does.
                catch (Exception) { }
            }
            catch (Exception e)
            {
                m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: Exception stopping script UUID: " + part.UUID + " LLUID: " + ItemID.ToString() + ": " + e.ToString());
            }
            m_log.DebugFormat("[{0}]: Closed Script {1} in " + part.Name, m_ScriptEngine.ScriptEngineName, InventoryItem.Name);
        }

        /// <summary>
        /// Removes any permissions the script may have on other avatars.
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="itemID"></param>
        private void ReleaseControls()
        {
            if (part != null)
            {
                int permsMask = InventoryItem.PermsMask;
                UUID permsGranter = InventoryItem.PermsGranter;


                if ((permsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                {
                    if (presence != null)
                        presence.UnRegisterControlEventsToScript(part.LocalId, ItemID);
                }
            }

            InventoryItem.PermsMask = 0;
            InventoryItem.PermsGranter = UUID.Zero;
        }

        #endregion

        #region Reset Script

        /// <summary>
        /// This resets the script back to its default state.
        /// </summary>
        internal void Reset()
        {
            if (Script == null)
                return;
            //Release controls over people.
            ReleaseControls();
            //Must be posted immediately, otherwise the next line will delete it.
            EventQueue.ProcessQIS(new QueueItemStruct()
            {
                ID = this,
                CurrentlyAt = Guid.Empty,
                functionName = "state_exit",
                llDetectParams = new DetectParams[0],
                param = new object[0],
                VersionID = VersionID
            });
            //Remove other items from the queue.
            if (ScriptEngine.NeedsRemoved.ContainsKey(ItemID))
                ScriptEngine.NeedsRemoved.Remove(ItemID);
            ScriptEngine.NeedsRemoved.Add(ItemID, VersionID);
            VersionID++;
            //Reset the state to default
            State = "default";
            //Tell the SOP about the change.
            part.SetScriptEvents(ItemID,
                                 (int)Script.GetStateEventFlags(State));
            //Reset all variables back to their original values.
            Script.ResetVars();

            //Fire state_entry
            m_ScriptEngine.AddToScriptQueue(this, "state_entry", new DetectParams[0], VersionID, new object[] { });
            m_log.InfoFormat("[{0}]: Reset Script {1}", m_ScriptEngine.ScriptEngineName, ItemID);
        }
        #endregion

        #region Helpers

        //Makes ToString look nicer
        public override string ToString()
        {
            return "UUID: " + part.UUID + ", itemID: " + ItemID;
        }

        /// <summary>
        /// Sets up the APIs for the script
        /// </summary>
        internal void SetApis()
        {
            Apis = new Dictionary<string, IScriptApi>();

            foreach (IScriptApi api in m_ScriptEngine.GetAPIs())
            {
                Apis[api.Name] = api;
                Apis[api.Name].Initialize(m_ScriptEngine, part, part.LocalId, ItemID, ScriptEngine.ScriptProtection);
            }
            foreach (KeyValuePair<string, IScriptApi> kv in Apis)
            {
                Script.InitApi(kv.Key, kv.Value);
            }
        }

        public void DisplayUserNotification(string message, string stage, bool postScriptCAPSError, bool IsError)
        {
            if (presence != null && (!PostOnRez))
                presence.ControllingClient.SendAgentAlertMessage("Script saved with errors, check debug window!", false);

            if (postScriptCAPSError)
                m_ScriptEngine.Errors[ItemID] = new String[] { message };

            // DISPLAY ERROR ON CONSOLE
            if (m_ScriptEngine.DisplayErrorsOnConsole)
            {
                string consoletext = IsError ? "Error " : "Warning ";
                consoletext += stage + " script:\n" + message + " itemID: " + ItemID + ", CompiledFile: " + AssemblyName;
                m_log.Error(consoletext);
            }

            // DISPLAY ERROR INWORLD
            string inworldtext = IsError ? "Error " : "Warning ";
            inworldtext += stage + " script: " + message;
            if (inworldtext.Length > 1100)
                inworldtext = inworldtext.Substring(0, 1099);

            World.SimChat(OpenMetaverse.Utils.StringToBytes(inworldtext), ChatTypeEnum.DebugChannel, 2147483647, part.AbsolutePosition, part.Name, part.UUID, false);

            m_ScriptEngine.ScriptFailCount++;
            m_ScriptEngine.ScriptErrorMessages += inworldtext;
        }

        #endregion

        #region Start Script

        /// <summary>
        /// Fires the events after the compiling has occured
        /// </summary>
        public void FireEvents()
        {
            if (RezzedFrom != UUID.Zero)
            {
                //Post the event for the prim that rezzed us
                m_ScriptEngine.PostObjectEvent(RezzedFrom,"object_rez", new object[] {
                            part.ParentGroup.RootPart.UUID });
                RezzedFrom = UUID.Zero;
            }
            if (StartedFromSavedState)
            {
                if (PostOnRez)
                    m_ScriptEngine.AddToScriptQueue(this, "on_rez", new DetectParams[0], VersionID, new object[] { new LSL_Types.LSLInteger(StartParam) });

                if (stateSource == StateSource.AttachedRez)
                    m_ScriptEngine.AddToScriptQueue(this, "attach", new DetectParams[0], VersionID, new object[] { new LSL_Types.LSLString(part.AttachedAvatar.ToString()) });
                else if (stateSource == StateSource.NewRez)
                    m_ScriptEngine.AddToScriptQueue(this, "changed", new DetectParams[0], VersionID, new Object[] { new LSL_Types.LSLInteger(256) });
                else if (stateSource == StateSource.PrimCrossing)
                    // CHANGED_REGION
                    m_ScriptEngine.AddToScriptQueue(this, "changed", new DetectParams[0], VersionID, new Object[] { new LSL_Types.LSLInteger(512) });
            }
            else
            {
                if (PostOnRez)
                    m_ScriptEngine.AddToScriptQueue(this, "on_rez", new DetectParams[0], VersionID, new object[] { new LSL_Types.LSLInteger(StartParam) });

                if (stateSource == StateSource.AttachedRez)
                    m_ScriptEngine.AddToScriptQueue(this, "attach", new DetectParams[0], VersionID, new object[] { new LSL_Types.LSLString(part.AttachedAvatar.ToString()) });
                m_ScriptEngine.AddToScriptQueue(this, "state_entry", new DetectParams[0], VersionID, new object[0]);
            }
        }

        /// <summary>
        /// This starts the script and sets up the variables.
        /// </summary>
        /// <returns></returns>
        public void Start(bool reupload)
        {
            //Clear out the removing of events for this script.
            VersionID++;

            //Remove any script errors that might be waiting.
            if (m_ScriptEngine.Errors.ContainsKey(ItemID))
                m_ScriptEngine.Errors.Remove(ItemID);

            DateTime StartTime = DateTime.Now.ToUniversalTime();

            //Find the inventory item
            part.TaskInventory.TryGetValue(ItemID, out InventoryItem);

            //Try to see if this was rezzed from someone's inventory
            UserInventoryItemID = part.FromUserInventoryItemID;

            //Try to find the avatar who started this.
            presence = World.GetScenePresence(part.OwnerID);

            #region HTML Reader

            if (ScriptEngine.ScriptProtection.AllowHTMLLinking)
            {
                //Read the URL and load it.
                if (Source.Contains("#IncludeHTML "))
                {
                    string URL = "";
                    int line = Source.IndexOf("#IncludeHTML ");
                    URL = Source.Remove(0, line);
                    URL = URL.Replace("#IncludeHTML ", "");
                    URL = URL.Split('\n')[0];
                    string webSite = ReadExternalWebsite(URL);
                    Source = Source.Replace("#IncludeHTML " + URL, webSite);
                }
            }
            else
            {
                //Remove the line then
                if (Source.Contains("#IncludeHTML "))
                {
                    string URL = "";
                    int line = Source.IndexOf("#IncludeHTML ");
                    URL = Source.Remove(0, line);
                    URL = URL.Replace("#IncludeHTML ", "");
                    URL = URL.Split('\n')[0];
                    Source = Source.Replace("#IncludeHTML " + URL, "");
                }
            }

            #endregion

            // Attempt to find a state save
            LastStateSave = ScriptFrontend.GetStateSave(ItemID, UserInventoryItemID);

            if (!reupload && Loading && LastStateSave != null
                && File.Exists(Path.Combine("ScriptEngines", Path.Combine(
                    "Script",
                    LastStateSave.AssemblyName))))
            {
                //Retrive the previous assembly
                AssemblyName = Path.Combine("ScriptEngines", Path.Combine(
                    "Script",
                    LastStateSave.AssemblyName));
            }
            else
            {
                LastStateSave = null;

                //Try to find a previously compiled script in this instance
                ScriptData PreviouslyCompiledID = ScriptEngine.ScriptProtection.TryGetPreviouslyCompiledScript(Source);
                if (reupload)
                {
                    //Close the previous script
                    CloseAndDispose(true);
                    ScriptEngine.ScriptProtection.RemovePreviouslyCompiled(Source);

                    Running = true;
                    Disabled = false;
                    VersionID++;
                }
                if (PreviouslyCompiledID != null)
                {
                    AssemblyName = PreviouslyCompiledID.AssemblyName;
                }
                else
                {
                    try
                    {
                        m_ScriptEngine.Compiler.PerformScriptCompile(Source, ItemID, part.OwnerID, out AssemblyName);
                        #region Errors and Warnings

                        #region Errors

                        string[] compileerrors = m_ScriptEngine.Compiler.GetErrors();

                        if (compileerrors != null && compileerrors.Length != 0)
                        {
                            string error = string.Empty;
                            foreach(string compileerror in compileerrors)
                            {
                                error += compileerror;
                            }
                            DisplayUserNotification(error, "compiling", reupload, true);
                            return;
                        }

                        #endregion

                        #region Warnings

                        if (m_ScriptEngine.ShowWarnings)
                        {
                            string[] compilewarnings = m_ScriptEngine.Compiler.GetWarnings();

                            if (compilewarnings != null && compilewarnings.Length != 0)
                            {
                                string error = string.Empty;
                                foreach(string compileerror in compileerrors)
                                {
                                    error += compileerror;
                                }
                                DisplayUserNotification(error, "compiling", reupload, false);
                                return;
                            }
                        }

                        #endregion

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        DisplayUserNotification(ex.Message, "compiling", reupload, true);
                        return;
                    }
                }
            }

            bool useDebug = false;
            if (useDebug)
            {
                TimeSpan t = (DateTime.Now.ToUniversalTime() - StartTime);
                m_log.Debug("[" + m_ScriptEngine.ScriptEngineName + "]: Stage 1 compile: " + t.TotalSeconds);
            }

            //Create the app domain if needed.
            try
            {
                Script = m_ScriptEngine.AppDomainManager.LoadScript(AssemblyName, "Script.ScriptClass", out AppDomain);
                ScriptEngine.ScriptProtection.AddPreviouslyCompiled(Source, this);
            }
            catch (System.IO.FileNotFoundException) // Not valid!!!
            {
                m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: File not found in app domain creation. Corrupt state save! " + AssemblyName);
                ScriptEngine.ScriptProtection.RemovePreviouslyCompiled(Source);
                ScriptFrontend.DeleteStateSave(AssemblyName);
                Start(reupload); // Lets restart the script if this happens
                return;
            }
            catch (Exception ex)
            {
                DisplayUserNotification(ex.Message, "app domain creation", reupload, true);
                return;
            }

            ILease lease = (ILease)RemotingServices.GetLifetimeService(Script as MarshalByRefObject);
            if (lease != null)
                lease.Register(Script.Sponsor);

            //If its a reupload, an avatar is waiting for the script errors
            if (reupload)
                m_ScriptEngine.Errors[ItemID] = new String[] { "SUCCESSFULL" };

            if (useDebug)
            {
                TimeSpan t = (DateTime.Now.ToUniversalTime() - StartTime);
                m_log.Debug("[" + m_ScriptEngine.ScriptEngineName + "]: Stage 2 compile: " + t.TotalSeconds);
            }

            //ALWAYS reset up APIs, otherwise m_host doesn't get updated and APIs could think that they are in another prim.
            SetApis();

            //Set the event flags
            int eventFlags = Script.GetStateEventFlags(State);
            part.SetScriptEvents(ItemID, eventFlags);

            //Now do the full state save finding now that we have an app domain.
            if (LastStateSave != null)
            {
                DeserializeDatabase();

                AsyncCommandManager.CreateFromData(m_ScriptEngine, part.ParentGroup.Scene,
                    part.LocalId, ItemID, part.UUID,
                    PluginData);

                // we get new rez events on sim restart, too
                // but if there is state, then we fire the change
                // event
                StartedFromSavedState = true;

                // We loaded state, don't force a re-save
            }
            else
            {
                // Add it to our script memstruct so it can be found by other scripts
                m_ScriptEngine.UpdateScriptInstanceData(this);

                //Make a new state save now
                m_ScriptEngine.AddToStateSaverQueue(this, true);
            }

            //All done, compiled successfully
            Loading = false;

            TimeSpan time = (DateTime.Now.ToUniversalTime() - StartTime);
            if (presence != null)
                m_log.DebugFormat("[{0}]: Started Script {1} in object {2} by avatar {3} in {4} seconds.", m_ScriptEngine.ScriptEngineName, InventoryItem.Name, part.Name, presence.Name, time.TotalSeconds);
            else
                m_log.DebugFormat("[{0}]: Started Script {1} in object {2} in {3} seconds.", m_ScriptEngine.ScriptEngineName, InventoryItem.Name, part.Name, time.TotalSeconds);
        }

        #endregion

        #region Event Processing

        public void SetEventParams(DetectParams[] qParams)
        {
            if (!Running || Disabled)
                return;

            if (qParams.Length > 0)
                LastDetectParams = qParams;

            if (EventDelayTicks != 0)
            {
                if (DateTime.Now.Ticks < NextEventTimeTicks)
                    throw new Exception();

                NextEventTimeTicks = DateTime.Now.Ticks + EventDelayTicks;
            }
        }

        #endregion

        #region Serialize

        private void DeserializeDatabase()
        {
            Dictionary<string, object> vars = LastStateSave.Variables as Dictionary<string,object>;
            State = LastStateSave.State;
            Running = LastStateSave.Running;

            if (vars != null && vars.Count != 0)
                Script.SetVars(vars);

            PluginData = (object[])LastStateSave.Plugins;
            if (LastStateSave.Permissions != " " && LastStateSave.Permissions != "")
            {
                InventoryItem.PermsGranter = new UUID(LastStateSave.Permissions.Split(',')[0]);
                InventoryItem.PermsMask = int.Parse(LastStateSave.Permissions.Split(',')[1], NumberStyles.Integer, Culture.NumberFormatInfo);
                //m_ScriptEngine.PostScriptEvent(ItemID, new EventParams(
                //            "run_time_permissions", new Object[] {
                //            new LSL_Types.LSLInteger(InventoryItem.PermsMask) },
                //            new DetectParams[0]));
            }
            EventDelayTicks = (long)LastStateSave.MinEventDelay;
            AssemblyName = LastStateSave.AssemblyName;
            Disabled = LastStateSave.Disabled;
            UserInventoryItemID = LastStateSave.UserInventoryID;
            // Add it to our script memstruct
            m_ScriptEngine.UpdateScriptInstanceData(this);
        }

        /// <summary>
        /// This saves the script to a database so that it can be reloaded in exactly the same state it was before it was closed.
        /// </summary>
        public void SerializeDatabase()
        {
            StateSave Insert = new StateSave();
            Insert.State = State;
            Insert.ItemID = ItemID;
            string source = Source.Replace("\n", " ");
            Insert.Source = source.Replace("'", " ");
            Insert.Running = Running;
            //Vars
            Dictionary<string, Object> vars = new Dictionary<string,object>();
            if (Script != null)
                vars = Script.GetVars();
            string varsmap = "";
            foreach (KeyValuePair<string, Object> var in vars)
            {
                varsmap += var.Key + "," + var.Value + "\n";
            }
            Insert.Variables = varsmap;
            //Plugins
            object[] Plugins = AsyncCommandManager.GetSerializationData(m_ScriptEngine, part.ParentGroup.Scene, ItemID);
            string plugins = "";
            foreach (object plugin in Plugins)
                plugins += plugin + ",";
            Insert.Plugins = plugins;

            //perms
            string perms = "";
            if (InventoryItem != null)
            {
                if (InventoryItem.PermsMask != 0 && InventoryItem.PermsGranter != UUID.Zero)
                {
                    perms += InventoryItem.PermsGranter.ToString() + "," + InventoryItem.PermsMask.ToString();

                }
            }
            Insert.Permissions = perms;
            
            Insert.MinEventDelay = EventDelayTicks;
            try
            {
                Insert.AssemblyName = AssemblyName.Split('\\')[2];
            }
            catch
            {
                Insert.AssemblyName = AssemblyName;
            }
            Insert.Disabled = Disabled;
            Insert.UserInventoryID = UserInventoryItemID;
            ScriptFrontend.SaveStateSave(Insert);
        }

        #region Helpers

        private LSL_Types.list ReadList(XmlNode parent)
        {
            List<Object> olist = new List<Object>();

            XmlNodeList itemL = parent.ChildNodes;
            foreach (XmlNode item in itemL)
                olist.Add(ReadTypedValue(item));

            return new LSL_Types.list(olist.ToArray());
        }

        private object ReadTypedValue(XmlNode tag, out string name)
        {
            name = tag.Attributes.GetNamedItem("name").Value;

            return ReadTypedValue(tag);
        }

        private object ReadTypedValue(XmlNode tag)
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

                assembly = itemType + ", OpenSim.Region.ScriptEngine.Shared";
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

        #endregion

        #endregion

        #region Other

        public string ReadExternalWebsite(string URL)
        {
            // External IP Address (get your external IP locally)
            String externalIp = "";
            UTF8Encoding utf8 = new UTF8Encoding();

            WebClient webClient = new WebClient();
            try
            {
                externalIp = utf8.GetString(webClient.DownloadData(URL));
            }
            catch (Exception)
            {
            }
            return externalIp;
        }
        #endregion
    }

    #endregion
}