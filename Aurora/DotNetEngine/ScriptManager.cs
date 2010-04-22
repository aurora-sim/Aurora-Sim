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

namespace OpenSim.Region.ScriptEngine.DotNetEngine
{
	#region InstanceData

	public class InstanceData : IInstanceData
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
		public bool Running = true;
		public bool Disabled = false;
        public bool Compiling = false;
        public bool Suspended = true;
		public string Source;
		public string ClassSource;
		public int StartParam;
		public StateSource stateSource;
		public AppDomain AppDomain;
		public Dictionary<string, IScriptApi> Apis = new Dictionary<string,IScriptApi>();
		public Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> LineMap;
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
		public ScenePresence presence;
		public DetectParams[] LastDetectParams;
		public bool m_startedFromSavedState = false;
        public Object[] PluginData = new Object[0];
        public string CurrentStateXML = "";

		#endregion

		#region Close Script

		/// <summary>
		/// This closes the scrpit, removes it from any known spots, and disposes of itself.
		/// This function is microthreaded.
		/// </summary>
		/// <returns></returns>
		public IEnumerable CloseAndDispose()
		{
            m_ScriptManager.m_scriptEngine.m_EventQueueManager.RemoveFromQueue(ItemID);
            if (m_ScriptManager.Errors.ContainsKey(ItemID))
                m_ScriptManager.Errors.Remove(ItemID);
            ReleaseControls();
            // Stop long command on script
            AsyncCommandManager.RemoveScript(m_ScriptManager.m_scriptEngine, localID, ItemID);
            m_ScriptManager.m_scriptEngine.m_EventManager.state_exit(localID);
            //m_scriptEngine.m_StateQueue.AddToQueue(this, false);

            yield return null;

            try 
            {
				// Tell script not to accept new requests
				Running = false;
				Disabled = true;

                // Remove from internal structure
                m_ScriptManager.RemoveScript(this);

                m_log.DebugFormat("[{0}]: Closed Script in " + part.Name, m_ScriptManager.m_scriptEngine.ScriptEngineName);
                
                if (AppDomain == null || Script == null)
				    yield break;
                
                try
                {
                    // Tell AppDomain that we have stopped script
                    m_ScriptManager.m_scriptEngine.m_AppDomainManager.UnloadScriptAppDomain(AppDomain);
                }
                //Legit: If the script had an error, this can happen... really shouldn't, but it does.
                catch (AppDomainUnloadedException) { }
			} 
			catch (Exception e)
			{
				m_log.Error("[" + m_ScriptManager.m_scriptEngine.ScriptEngineName + "]: Exception stopping script localID: " + localID + " LLUID: " + ItemID.ToString() + ": " + e.ToString());
			}
		}

		/// <summary>
		/// Removes any permissions the script may have on other avatars.
		/// </summary>
		/// <param name="localID"></param>
		/// <param name="itemID"></param>
		private void ReleaseControls()
		{
			if (part != null) {
                int permsMask = InventoryItem.PermsMask;
				UUID permsGranter= InventoryItem.PermsGranter;
                

				if ((permsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0) {
					if (presence != null)
						presence.UnRegisterControlEventsToScript(localID, ItemID);
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
			if(Script == null)
				return;
            //Release controls over people.
            ReleaseControls();
            //Remove state saves.
            m_scriptEngine.m_StateQueue.AddToQueue(this, false);
			//Must be posted immediately, otherwise the next line will delete it.
			m_ScriptManager.m_scriptEngine.m_EventManager.state_exit(localID);
            //Remove items from the queue.
			m_ScriptManager.m_scriptEngine.m_EventQueueManager.RemoveFromQueue(ItemID);
            //Reset the state to default
            State = "default";
            //Tell the SOP about the change.
            part.SetScriptEvents(ItemID,
                                 (int)Script.GetStateEventFlags(State));
            //Reset all variables back to their original values.
            Script.ResetVars();
            //Make sure the new changes are processed.
            m_scriptEngine.m_StateQueue.AddToQueue(this, true);
            //Fire state_entry
            m_ScriptManager.m_scriptEngine.m_EventQueueManager.AddToScriptQueue(this, "state_entry", new DetectParams[0], new object[] {});
			m_log.InfoFormat("[{0}]: Reset Script {1}", m_ScriptManager.m_scriptEngine.ScriptEngineName, ItemID);
		}
		#endregion

		#region Helpers

		public override string ToString()
		{
			return "localID: " + localID + ", itemID: " + ItemID;
		}

		internal void SetApis()
		{
			if (part == null)
				part = m_ScriptManager.m_scriptEngine.World.GetSceneObjectPart(localID);
			Apis = new Dictionary<string, IScriptApi>();

			ApiManager am = new ApiManager();
			foreach (string api in am.GetApis()) {
				Apis[api] = am.CreateApi(api);
				Apis[api].Initialize(m_ScriptManager.m_scriptEngine, part, localID, ItemID, m_scriptEngine.ScriptProtection);
			}
			foreach (KeyValuePair<string, IScriptApi> kv in Apis) {
				Script.InitApi(kv.Key, kv.Value);
			}
		}

		public void ShowError(Exception e, int stage)
		{
			if (presence != null && (!PostOnRez))
				presence.ControllingClient.SendAgentAlertMessage("Script saved with errors, check debug window!", false);

            if (presence != null)
                m_ScriptManager.Errors[ItemID] = new String[] { e.Message.ToString() };
            m_ScriptManager.UpdateScriptInstanceData(this);

			try {
				// DISPLAY ERROR INWORLD
				string consoletext = "Error compiling script in stage " + stage + ":\n" + e.Message.ToString() + " itemID: " + ItemID + ", localID" + localID + ", CompiledFile: " + AssemblyName;
				//m_log.Error(consoletext);
				string inworldtext = "Error compiling script: " + e;
				if (inworldtext.Length > 1100)
					inworldtext = inworldtext.Substring(0, 1099);

				World.SimChat(Utils.StringToBytes(inworldtext), ChatTypeEnum.DebugChannel, 2147483647, part.AbsolutePosition, part.Name, part.UUID, false);
			// LEGIT: User Scripting
			} catch (Exception e2) {
				m_log.Error("[" + m_scriptEngine.ScriptEngineName + "]: Error displaying error in-world: " + e2.ToString());
				m_log.Error("[" + m_scriptEngine.ScriptEngineName + "]: " + "Errormessage: Error compiling script:\r\n" + e2.Message.ToString());
			}
			throw e;
		}

		#endregion

		#region Start Script

        public void FireEvents()
        {
            if (m_startedFromSavedState)
            {
                if (PostOnRez)
                    m_scriptEngine.m_EventQueueManager.AddToScriptQueue(this, "on_rez", new DetectParams[0], new object[] { new LSL_Types.LSLInteger(StartParam) });

                if (stateSource == StateSource.AttachedRez)
                    m_scriptEngine.m_EventQueueManager.AddToScriptQueue(this, "attach", new DetectParams[0], new object[] { new LSL_Types.LSLString(part.AttachedAvatar.ToString()) });
                else if (stateSource == StateSource.NewRez)
                    m_scriptEngine.m_EventQueueManager.AddToScriptQueue(this, "changed", new DetectParams[0], new Object[] { new LSL_Types.LSLInteger(256) });
                else if (stateSource == StateSource.PrimCrossing)
                    // CHANGED_REGION
                    m_scriptEngine.m_EventQueueManager.AddToScriptQueue(this, "changed", new DetectParams[0], new Object[] { new LSL_Types.LSLInteger(512) });
            }
            else
            {
                m_scriptEngine.m_EventQueueManager.AddToScriptQueue(this, "state_entry", new DetectParams[0], new object[] { });
                if (PostOnRez)
                {
                    m_scriptEngine.m_EventQueueManager.AddToScriptQueue(this, "on_rez", new DetectParams[0], new object[] { new LSL_Types.LSLInteger(StartParam) });
                }

                if (stateSource == StateSource.AttachedRez)
                {
                    m_scriptEngine.m_EventQueueManager.AddToScriptQueue(this, "attach", new DetectParams[0], new object[] { new LSL_Types.LSLString(part.AttachedAvatar.ToString()) });
                }
            }
        }
		/// <summary>
		/// This starts the script and sets up the variables.
		/// This function is microthreaded.
		/// </summary>
		/// <returns></returns>
		public IEnumerator Start()
		{
            Compiling = true;
            if (m_ScriptManager.Errors.ContainsKey(ItemID))
                m_ScriptManager.Errors.Remove(ItemID);
			DateTime Start = DateTime.Now.ToUniversalTime();

			// We will initialize and start the script.
			// It will be up to the script itself to hook up the correct events.
			part = World.GetSceneObjectPart(localID);

			if (null == part) 
            {
				m_log.ErrorFormat("[{0}]: Could not find scene object part corresponding " + "to localID {1} to start script", m_scriptEngine.ScriptEngineName, localID);

				throw new NullReferenceException();
			}


			if (part.TaskInventory.TryGetValue(ItemID, out InventoryItem))
				AssetID = InventoryItem.AssetID;

			presence = World.GetScenePresence(InventoryItem.OwnerID);
			if (presence != null)
                m_log.DebugFormat("[{0}]: Starting Script {1} in object {2} by avatar {3}.", m_scriptEngine.ScriptEngineName, InventoryItem.Name, part.Name, presence.Name);
            else
                m_log.DebugFormat("[{0}]: Starting Script {1} in object {2}.", m_scriptEngine.ScriptEngineName, InventoryItem.Name, part.Name);

			CultureInfo USCulture = new CultureInfo("en-US");
			Thread.CurrentThread.CurrentCulture = USCulture;
            string FilePrefix = "CommonCompiler";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                FilePrefix = FilePrefix.Replace(c, '_');
            }
            AssemblyName = Path.Combine("ScriptEngines", Path.Combine(
                    m_scriptEngine.World.RegionInfo.RegionID.ToString(),
                    FilePrefix + "_compiled_" + ItemID.ToString() + ".dll"));
            string savedState = Path.Combine(Path.GetDirectoryName(AssemblyName),
                    ItemID.ToString() + ".state");
            
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

            bool previouslyCompiled = false;
            if (File.Exists(AssemblyName) && File.Exists(savedState) && File.Exists(AssemblyName + ".map"))
            {
                //Find the linemap
                LineMap = OpenSim.Region.ScriptEngine.Shared.CodeTools.Compiler.ReadMapFile(AssemblyName + ".map");
                //Find the classID
                string xml = String.Empty;
                try
                {
                    FileInfo fi = new FileInfo(savedState);
                    int size = (int)fi.Length;
                    if (size < 512000)
                    {
                        using (FileStream fs = File.Open(savedState,
                                                         FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            System.Text.UTF8Encoding enc =
                                new System.Text.UTF8Encoding();

                            Byte[] data = new Byte[size];
                            fs.Read(data, 0, size);

                            xml = enc.GetString(data);

                            FindClassID(xml);
                        }
                    }
                }
                catch (Exception) { }
                //Dont set to previously compiled, otherwise we wont have the script loaded into the app domain.
                previouslyCompiled = false;
            }
            else
            {
                try
                {
                    InstanceData PreviouslyCompiledID = (InstanceData)m_scriptEngine.ScriptProtection.TryGetPreviouslyCompiledScript(Source);
                    if (PreviouslyCompiledID != null)
                    {
                        if (!File.Exists(PreviouslyCompiledID.AssemblyName))
                        {
                            FileInfo fi = new FileInfo(PreviouslyCompiledID.AssemblyName);
                            FileStream stream = fi.OpenRead();
                            Byte[] data = new Byte[fi.Length];
                            stream.Read(data, 0, data.Length);
                            FileStream sfs = File.Create(AssemblyName);
                            sfs.Write(data, 0, data.Length);
                            sfs.Close();
                            stream.Close();
                        }
                        if (!File.Exists(PreviouslyCompiledID.AssemblyName + ".map"))
                        {
                            FileInfo fi = new FileInfo(PreviouslyCompiledID.AssemblyName + ".map");
                            FileStream stream = fi.OpenRead();
                            Byte[] data = new Byte[fi.Length];
                            stream.Read(data, 0, data.Length);
                            FileStream sfs = File.Create(AssemblyName + ".map");
                            sfs.Write(data, 0, data.Length);
                            sfs.Close();
                            stream.Close();
                        }
                        if (!File.Exists(PreviouslyCompiledID.AssemblyName + ".text"))
                        {
                            FileInfo fi = new FileInfo(PreviouslyCompiledID.AssemblyName + ".text");
                            FileStream stream = fi.OpenRead();
                            Byte[] data = new Byte[fi.Length];
                            stream.Read(data, 0, data.Length);
                            FileStream sfs = File.Create(AssemblyName + ".text");
                            sfs.Write(data, 0, data.Length);
                            sfs.Close();
                            stream.Close();
                        }

                        LineMap = PreviouslyCompiledID.LineMap;
                        ClassID = PreviouslyCompiledID.ClassID;
                        AppDomain = PreviouslyCompiledID.AppDomain;
                        Script = PreviouslyCompiledID.Script;
                        previouslyCompiled = true;
                    }
                    else
                    {
                        // Compile (We assume LSL)
                        m_ScriptManager.LSLCompiler.PerformScriptCompile(Source, AssetID, InventoryItem.OwnerID, ItemID, Inherited, ClassName, m_scriptEngine.ScriptProtection, localID, this, out AssemblyName,
                                                                         out LineMap, out ClassID);
                        m_scriptEngine.ScriptProtection.AddPreviouslyCompiled(Source, this);
                    }

                    #region Warnings

                    string[] compilewarnings = m_ScriptManager.LSLCompiler.GetWarnings();

                    if (compilewarnings != null && compilewarnings.Length != 0)
                    {
                        if (presence != null && (!PostOnRez))
                            presence.ControllingClient.SendAgentAlertMessage("Script saved with warnings, check debug window!", false);

                        foreach (string warning in compilewarnings)
                        {
                            // DISPLAY WARNING INWORLD
                            string text = "Warning:\n" + warning;
                            if (text.Length > 1100)
                                text = text.Substring(0, 1099);

                            World.SimChat(Utils.StringToBytes(text), ChatTypeEnum.DebugChannel, 2147483647, part.AbsolutePosition, part.Name, part.UUID, false);
                        }
                    }

                    #endregion
                }
                catch (Exception ex)
                {
                    ShowError(ex, 1);
                }
            }
            if(presence != null)
			    m_ScriptManager.Errors[ItemID] = new String[] { "SUCCESSFULL" };
            

			bool useDebug = false;
			if (useDebug) 
			{
				TimeSpan t = (DateTime.Now.ToUniversalTime() - Start);
				m_log.Debug("Stage 1: " + t.TotalSeconds);
			}

			yield return null;
            if (!previouslyCompiled)
            {
                try
                {
                    if (ClassName != "")
                        Script = m_scriptEngine.m_AppDomainManager.LoadScript(AssemblyName, "Script." + ClassName, out AppDomain);
                    else
                        Script = m_scriptEngine.m_AppDomainManager.LoadScript(AssemblyName, "Script." + ClassID, out AppDomain);
                }
                catch (Exception ex)
                {
                    ShowError(ex, 2);
                }
            }

			if (useDebug) 
			{
				TimeSpan t = (DateTime.Now.ToUniversalTime() - Start);
				m_log.Debug("Stage 2: " + t.TotalSeconds);
			}

			State = "default";
			Running = true;
			Disabled = false;

			// Add it to our script memstruct
			m_ScriptManager.UpdateScriptInstanceData(this);

            //ALWAYS reset up APIs, otherwise m_host doesn't get updated and LSL thinks its in another prim.
            SetApis();

            if (File.Exists(savedState))
            {
                yield return null;
                string xml = String.Empty;

                try
                {
                    FileInfo fi = new FileInfo(savedState);
                    int size = (int)fi.Length;
                    if (size < 512000)
                    {
                        using (FileStream fs = File.Open(savedState,
                                                         FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            System.Text.UTF8Encoding enc =
                                new System.Text.UTF8Encoding();

                            Byte[] data = new Byte[size];
                            fs.Read(data, 0, size);

                            xml = enc.GetString(data);

                            Deserialize(xml);

                            AsyncCommandManager.CreateFromData(m_scriptEngine,
                                localID, ItemID, part.UUID,
                                PluginData);

                            // we get new rez events on sim restart, too
                            // but if there is state, then we fire the change
                            // event

                            // We loaded state, don't force a re-save
                            m_startedFromSavedState = true;
                        }
                    }
                }
                catch (Exception)
                {
                    // m_log.ErrorFormat("[Script] Unable to load script state from xml: {0}\n"+e.ToString(), xml);
                }
            }
            else
            {
                m_scriptEngine.m_StateQueue.AddToQueue(this, true);
            }
            // Fire the first start-event
            int eventFlags = Script.GetStateEventFlags(State);
            part.SetScriptEvents(ItemID, eventFlags);

			Compiling = false;
            //if (m_ScriptManager.Errors.ContainsKey(ItemID))
            //   m_ScriptManager.Errors.Remove(ItemID);
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

        public void Deserialize(string xml)
        {
            XmlDocument doc = new XmlDocument();

            Dictionary<string, object> vars = Script.GetVars();

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
                            State = part.InnerText;
                            break;
                        case "Running":
                            Running = bool.Parse(part.InnerText);
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
                            Script.SetVars(vars);
                            break;
                        case "Plugins":
                            PluginData = ReadList(part).Data;
                            break;
                        case "ClassID":
                            ClassID = part.InnerText;
                            break;
                        case "Queue":
                            XmlNodeList itemL = part.ChildNodes;
                            foreach (XmlNode item in itemL)
                            {
                                List<Object> parms = new List<Object>();
                                List<DetectParams> detected =
                                        new List<DetectParams>();

                                string eventName =
                                        item.Attributes.GetNamedItem("event").Value;
                                XmlNodeList eventL = item.ChildNodes;
                                foreach (XmlNode evt in eventL)
                                {
                                    switch (evt.Name)
                                    {
                                        case "Params":
                                            XmlNodeList prms = evt.ChildNodes;
                                            foreach (XmlNode pm in prms)
                                                parms.Add(ReadTypedValue(pm));

                                            break;
                                        case "Detected":
                                            XmlNodeList detL = evt.ChildNodes;
                                            foreach (XmlNode det in detL)
                                            {
                                                string vect =
                                                        det.Attributes.GetNamedItem(
                                                        "pos").Value;
                                                LSL_Types.Vector3 v =
                                                        new LSL_Types.Vector3(vect);

                                                int d_linkNum = 0;
                                                UUID d_group = UUID.Zero;
                                                string d_name = String.Empty;
                                                UUID d_owner = UUID.Zero;
                                                LSL_Types.Vector3 d_position =
                                                    new LSL_Types.Vector3();
                                                LSL_Types.Quaternion d_rotation =
                                                    new LSL_Types.Quaternion();
                                                int d_type = 0;
                                                LSL_Types.Vector3 d_velocity =
                                                    new LSL_Types.Vector3();

                                                try
                                                {
                                                    string tmp;

                                                    tmp = det.Attributes.GetNamedItem(
                                                            "linkNum").Value;
                                                    int.TryParse(tmp, out d_linkNum);

                                                    tmp = det.Attributes.GetNamedItem(
                                                            "group").Value;
                                                    UUID.TryParse(tmp, out d_group);

                                                    d_name = det.Attributes.GetNamedItem(
                                                            "name").Value;

                                                    tmp = det.Attributes.GetNamedItem(
                                                            "owner").Value;
                                                    UUID.TryParse(tmp, out d_owner);

                                                    tmp = det.Attributes.GetNamedItem(
                                                            "position").Value;
                                                    d_position =
                                                        new LSL_Types.Vector3(tmp);

                                                    tmp = det.Attributes.GetNamedItem(
                                                            "rotation").Value;
                                                    d_rotation =
                                                        new LSL_Types.Quaternion(tmp);

                                                    tmp = det.Attributes.GetNamedItem(
                                                            "type").Value;
                                                    int.TryParse(tmp, out d_type);

                                                    tmp = det.Attributes.GetNamedItem(
                                                            "velocity").Value;
                                                    d_velocity =
                                                        new LSL_Types.Vector3(tmp);

                                                }
                                                catch (Exception) // Old version XML
                                                {
                                                }

                                                UUID uuid = new UUID();
                                                UUID.TryParse(det.InnerText,
                                                        out uuid);

                                                DetectParams d = new DetectParams();
                                                d.Key = uuid;
                                                d.OffsetPos = v;
                                                d.LinkNum = d_linkNum;
                                                d.Group = d_group;
                                                d.Name = d_name;
                                                d.Owner = d_owner;
                                                d.Position = d_position;
                                                d.Rotation = d_rotation;
                                                d.Type = d_type;
                                                d.Velocity = d_velocity;

                                                detected.Add(d);
                                            }
                                            break;
                                    }
                                }
                                EventParams ep = new EventParams(
                                        eventName, parms.ToArray(),
                                        detected.ToArray());
                                
                                m_scriptEngine.PostScriptEvent(ItemID, ep);
                            }
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
                                            InventoryItem.PermsMask = mask;
                                            InventoryItem.PermsGranter = granter;
                                        }
                                    }
                                }
                            }
                            break;
                        case "MinEventDelay":
                            double minEventDelay = 0.0;
                            double.TryParse(part.InnerText, NumberStyles.Float, Culture.NumberFormatInfo, out minEventDelay);
                            EventDelayTicks = (long)minEventDelay;
                            break;
                    }
                }
            }
        }

        public void FindClassID(string xml)
        {
            XmlDocument doc = new XmlDocument();

            doc.LoadXml(xml);

            XmlNodeList rootL = doc.GetElementsByTagName("ScriptState");
            if (rootL.Count != 1)
            {
                return;
            }
            XmlNode rootNode = rootL[0];

            if (rootNode != null)
            {
                XmlNodeList partL = rootNode.ChildNodes;

                foreach (XmlNode part in partL)
                {
                    switch (part.Name)
                    {
                        case "ClassID":
                            ClassID = part.InnerText;
                            break;
                    }
                }
            }
        }

        public IEnumerator Serialize()
        {
            //Update PluginData
            PluginData = AsyncCommandManager.GetSerializationData(m_scriptEngine, ItemID);

            bool running = Running;

            XmlDocument xmldoc = new XmlDocument();

            XmlNode xmlnode = xmldoc.CreateNode(XmlNodeType.XmlDeclaration,
                                                "", "");
            xmldoc.AppendChild(xmlnode);

            XmlElement rootElement = xmldoc.CreateElement("", "ScriptState",
                                                          "");
            xmldoc.AppendChild(rootElement);

            XmlElement state = xmldoc.CreateElement("", "State", "");
            state.AppendChild(xmldoc.CreateTextNode(State));

            rootElement.AppendChild(state);

            XmlElement run = xmldoc.CreateElement("", "Running", "");
            run.AppendChild(xmldoc.CreateTextNode(
                    running.ToString()));

            rootElement.AppendChild(run);

            XmlElement classID = xmldoc.CreateElement("", "ClassID", "");
            classID.AppendChild(xmldoc.CreateTextNode(
                    ClassID.ToString()));

            rootElement.AppendChild(classID);

            Dictionary<string, Object> vars = Script.GetVars();

            XmlElement variables = xmldoc.CreateElement("", "Variables", "");

            foreach (KeyValuePair<string, Object> var in vars)
                WriteTypedValue(xmldoc, variables, "Variable", var.Key,
                                var.Value);

            rootElement.AppendChild(variables);

            yield return null;
            #region Queue

            XmlElement queue = xmldoc.CreateElement("", "Queue", "");

            QueueItemStruct[] tempQueue = new List<QueueItemStruct>().ToArray();
            m_scriptEngine.m_EventQueueManager.EventQueue.CopyTo(tempQueue, 0);
            int count = tempQueue.Length;
            int i = 0;
            while (i < count)
            {
                QueueItemStruct QIS = tempQueue[i];
                if (QIS.ID.ItemID == ItemID)
                {
                    EventParams ep = new EventParams(QIS.functionName, QIS.param, QIS.llDetectParams);
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
                i++;
            }

            rootElement.AppendChild(queue);

            #endregion

            XmlNode plugins = xmldoc.CreateElement("", "Plugins", "");
            DumpList(xmldoc, plugins,
                     new LSL_Types.list(AsyncCommandManager.GetSerializationData(m_scriptEngine, ItemID)));

            rootElement.AppendChild(plugins);

            if (InventoryItem != null)
            {
                if (InventoryItem.PermsMask != 0 && InventoryItem.PermsGranter != UUID.Zero)
                {
                    XmlNode permissions = xmldoc.CreateElement("", "Permissions", "");
                    XmlAttribute granter = xmldoc.CreateAttribute("", "granter", "");
                    granter.Value = InventoryItem.PermsGranter.ToString();
                    permissions.Attributes.Append(granter);
                    XmlAttribute mask = xmldoc.CreateAttribute("", "mask", "");
                    mask.Value = InventoryItem.PermsMask.ToString();
                    permissions.Attributes.Append(mask);
                    rootElement.AppendChild(permissions);
                }
            }

            if (EventDelayTicks > 0.0)
            {
                XmlElement eventDelay = xmldoc.CreateElement("", "MinEventDelay", "");
                eventDelay.AppendChild(xmldoc.CreateTextNode(EventDelayTicks.ToString()));
                rootElement.AppendChild(eventDelay);
            }

            Type type = Script.GetType();
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
            stateID.Value = ItemID.ToString();
            stateData.Attributes.Append(stateID);
            XmlAttribute assetID = doc.CreateAttribute("", "Asset", "");
            assetID.Value = AssetID.ToString();
            stateData.Attributes.Append(assetID);
            XmlAttribute engineName = doc.CreateAttribute("", "Engine", "");
            engineName.Value = m_scriptEngine.ScriptEngineName;
            stateData.Attributes.Append(engineName);
            doc.AppendChild(stateData);

            // Add <ScriptState>...</ScriptState>
            XmlNode xmlstate = doc.ImportNode(rootNode, true);
            stateData.AppendChild(xmlstate);

            string assemName = AssemblyName;

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
                        m_log.DebugFormat("[{0}]: Unable to open script textfile {1}, reason: {2}", m_scriptEngine.ScriptEngineName, assemName + ".text", e.Message);
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
                        m_log.DebugFormat("[{0}]: Unable to open script assembly {1}, reason: {2}", m_scriptEngine.ScriptEngineName, assemName, e.Message);
                    }

                }
            }

            yield return null;
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

            yield return null;
            FileStream fcs = File.Create(Path.Combine(Path.GetDirectoryName(AssemblyName), ItemID.ToString() + ".state"));
            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            Byte[] buf = enc.GetBytes(doc.InnerXml);
            CurrentStateXML = doc.InnerXml;
            fcs.Write(buf, 0, buf.Length);
            fcs.Close();
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
		private bool m_started = false;
		private Dictionary<InstanceData, DetectParams[]> detparms = new Dictionary<InstanceData, DetectParams[]>();
		public Dictionary<UUID, string[]> Errors = new Dictionary<UUID, string[]>();
        private int SleepTime = 250;

		// Load/Unload structure
		private struct LUStruct
		{
			public InstanceData ID;
			public LUType Action;
		}

		private enum LUType
		{
			Unknown = 0,
			Load = 1,
			Unload = 2
		}

		public Compiler LSLCompiler;

		public Scene World {
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
			while (!Errors.ContainsKey(ItemID)) 
			{
				Thread.Sleep(250);
			}
			lock(Errors)
			{
				string[] Error = Errors[ItemID];
				Errors.Remove(ItemID);
				if (Error[0] == "SUCCESSFULL")
					return new string[0];
				return Error;
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
			LoadUnloadMaxQueueSize = m_scriptEngine.ScriptConfigSource.GetInt("LoadUnloadMaxQueueSize", 100);
			SleepTime = m_scriptEngine.ScriptConfigSource.GetInt("SleepTimeBetweenLoops", 250);
		}

		public void Start()
		{
			m_started = true;

			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

			//
			// CREATE THREAD
			// Private or shared
			//
			if (PrivateThread) {
				// Assign one thread per region
				//scriptLoadUnloadThread = StartScriptLoadUnloadThread();
			} else {
				// Shared thread - make sure one exist, then assign it to the private
				if (staticScriptLoadUnloadThread == null) {
					//staticScriptLoadUnloadThread =
					//        StartScriptLoadUnloadThread();
				}
				scriptLoadUnloadThread = staticScriptLoadUnloadThread;
			}
		}

		internal void Stop()
		{
			foreach (InstanceData ID in m_scriptEngine.ScriptProtection.GetAllScripts())
					StopScript(ID.localID, ID.ItemID);
		}

		~ScriptManager()
		{
			// Abort load/unload thread
			try {
				if (scriptLoadUnloadThread != null && scriptLoadUnloadThread.IsAlive == true) {
					scriptLoadUnloadThread.Abort();
					//scriptLoadUnloadThread.Join();
				}
			} catch {
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
            List<InstanceData> FireEvents = new List<InstanceData>();
			lock (LUQueue) {
				if (LUQueue.Count > 0) {
					int i = 0;
					while (i < LUQueue.Count)
                    {
						LUStruct item = LUQueue.Dequeue();

						if (item.Action == LUType.Unload)
                        {
							StopParts.Add(item.ID.CloseAndDispose().GetEnumerator());
						} 
                        else if (item.Action == LUType.Load)
                        {
                            FireEvents.Add(item.ID);
							StartParts.Add(item.ID.Start());
						}
						i++;
					}
				}
			}
			lock (StopParts) {
				int i = 0;
				while (StopParts.Count > 0 && i < 1000) {
					i++;

					bool running = false;
					try {
						running = StopParts[i % StopParts.Count].MoveNext();
					} catch (Exception) {
					}

					if (!running)
						StopParts.Remove(StopParts[i % StopParts.Count]);
				}
			}
			lock (StartParts) {
				int i = 0;
				while (StartParts.Count > 0 && i < 1000) {
					i++;

					bool running = false;
					try {
						running = StartParts[i % StartParts.Count].MoveNext();
					} catch (Exception) {
					}

					if (!running)
                    {
						StartParts.Remove(StartParts[i % StartParts.Count]);
                    }
				}
			}
            foreach (InstanceData ID in FireEvents)
            {
                ID.FireEvents();
            }
            FireEvents.Clear();
		}

		#endregion

		#region Helper functions

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			return Assembly.GetExecutingAssembly().FullName == args.Name ? Assembly.GetExecutingAssembly() : null;
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
			InstanceData id = null;
			lock (LUQueue) 
            {
				if ((LUQueue.Count >= LoadUnloadMaxQueueSize) && m_started)
                {
					m_log.Error("[" + m_scriptEngine.ScriptEngineName + "]: ERROR: Load/unload queue item count is at " + LUQueue.Count + ". Config variable \"LoadUnloadMaxQueueSize\" " + "is set to " + LoadUnloadMaxQueueSize + ", so ignoring new script.");
					return;
				}
				id = new InstanceData(this);
				id.ItemID = itemID;
				id.localID = localID;
				id.PostOnRez = postOnRez;
				id.StartParam = startParam;
				id.stateSource = statesource;
				id.State = "default";
				id.Running = true;
				id.Disabled = false;
                id.Source = Script;
				id.PostOnRez = postOnRez;
				LUStruct ls = new LUStruct();
				ls.Action = LUType.Load;
				ls.ID = id;
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
			if (data == null)
				return;
			if (data.Disabled)
				return;
			LUStruct ls = new LUStruct();
			ls.ID = data;
			ls.Action = LUType.Unload;
			m_scriptEngine.m_EventQueueManager.RemoveFromQueue(itemID);
			lock (LUQueue) {
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
			List<UUID> UUIDs = new List<UUID>();
			foreach(InstanceData ID in m_scriptEngine.ScriptProtection.GetScript(localID) as InstanceData[])
				UUIDs.Add(ID.ItemID);
			return UUIDs;
		}

		/// <summary>
		/// Gets the script by itemID.
		/// </summary>
		/// <param name="itemID"></param>
		/// <returns></returns>
		public InstanceData GetScriptByItemID(UUID itemID)
		{
			return (InstanceData)m_scriptEngine.ScriptProtection.GetScript(itemID);
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
			return (InstanceData)m_scriptEngine.ScriptProtection.GetScript(localID, itemID);
		}

		/// <summary>
		/// Updates or adds the given InstanceData to the list of known scripts.
		/// </summary>
		/// <param name="id"></param>
		public void UpdateScriptInstanceData(InstanceData id)
		{
			m_scriptEngine.ScriptProtection.AddNewScript(id);
		}

		/// <summary>
		/// Removes the given InstanceData from all known scripts.
		/// </summary>
		/// <param name="id"></param>
		public void RemoveScript(InstanceData id)
		{
			m_scriptEngine.ScriptProtection.RemoveScript(id);
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
			try {
				externalIp = utf8.GetString(webClient.DownloadData(URL));
			} catch (Exception) {
			}
			return externalIp;
		}
		#endregion
	}

	#endregion

    public class StateSaverQueue
    {
        public class StateQueueItem
        {
            public InstanceData ID;
            public bool Create;
        }
        public Queue<StateQueueItem> StateQueue = new Queue<StateQueueItem>();
        Thread thread = null;
        IScriptEngine m_ScriptEngine = null;
        private int SleepTime = 250;
        
        public StateSaverQueue(ScriptEngine engine)
        {
            m_ScriptEngine = engine;
            SleepTime = engine.ScriptConfigSource.GetInt("SleepTimeBetweenLoops", 250);
            thread = Watchdog.StartThread(RunLoop, "StateQueueThread", ThreadPriority.BelowNormal, true);
        }

        public void Stop()
        {
            Watchdog.RemoveThread();
            if (thread != null && thread.IsAlive)
            {
                try
                {
                    thread.Abort(); // Send abort
                }
                catch (Exception)
                {
                }
            }
        }

        public void RunLoop()
        {
            while(true)
            {
                DoQueue();
            }
        }

        public void DoQueue()
        {
            Thread.Sleep(SleepTime);
            List<IEnumerator> Parts = new List<IEnumerator>();
            while (StateQueue.Count != 0)
            {
                StateQueueItem item = StateQueue.Dequeue();
                if (item.Create)
                    Parts.Add(item.ID.Serialize());
                else
                    RemoveState(item.ID);
                lock (Parts)
                {
                    int i = 0;
                    while (Parts.Count > 0 && i < 1000)
                    {
                        i++;

                        bool running = false;
                        try
                        {
                            running = Parts[i % Parts.Count].MoveNext();
                        }
                        catch (Exception)
                        {
                        }

                        if (!running)
                            Parts.Remove(Parts[i % Parts.Count]);
                    }
                }
            }

        }

        public void RemoveState(InstanceData ID)
        {
            string savedState = Path.Combine(Path.GetDirectoryName(ID.AssemblyName),
                    ID.ItemID.ToString() + ".state");
            try
            {
                if (File.Exists(savedState))
                {
                    File.Delete(savedState);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Adds the given item to the queue.
        /// </summary>
        /// <param name="ID">InstanceData that needs to be state saved</param>
        /// <param name="create">true: create a new state. false: remove the state.</param>
        public void AddToQueue(InstanceData ID, bool create)
        {
            StateQueueItem SQ = new StateQueueItem();
            SQ.ID = ID;
            SQ.Create = create;
            StateQueue.Enqueue(SQ);
        }
    }
}
