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

    public class ScriptData : IScriptData
    {
        #region Constructor

        public ScriptData(ScriptEngine engine)
        {
            m_ScriptEngine = engine;
            ScriptFrontend = Aurora.DataManager.DataManager.IScriptDataConnector;
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
        public bool Compiling = false;
        public bool Suspended = false;
        public bool Loading = true;
        public string Source;
        public string ClassSource;
        public int StartParam;
        public StateSource stateSource;
        public AppDomain AppDomain;
        public Dictionary<string, IScriptApi> Apis = new Dictionary<string, IScriptApi>();
        public Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> LineMap;
        public ScriptSponsor ScriptSponsor;
        public bool TimerQueued = false;
        public bool CollisionInQueue = false;
        public int LastControlLevel = 0;
        public int ControlEventsInQueue = 0;

        public SceneObjectPart part;

        public long EventDelayTicks = 0;
        public long NextEventTimeTicks = 0;
        public UUID AssetID;
        public string AssemblyName;
        //This is the UUID of the actual script.
        public UUID ItemID;
        public UUID UserInventoryItemID;
        //This is the localUUID of the object the script is in.
        public uint localID;
        public string ClassID;
        public bool PostOnRez;
        public TaskInventoryItem InventoryItem;
        public ScenePresence presence;
        public DetectParams[] LastDetectParams;
        public bool m_startedFromSavedState = false;
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
            try
            {
                if (!Silent)
                {
                    if (Script != null)
                    {
                        //Fire this directly so its not closed before its fired
                        SetEventParams(new DetectParams[0]);
                        Script.ExecuteEvent(State,
                                            "state_exit",
                                            new object[0], 0);
                    }
                }
                ReleaseControls();
                // Tell script not to accept new requests
                //These are fine to set as the state wont be saved again
                Running = false;
                Disabled = true;

                // Remove from internal structure
                m_ScriptEngine.RemoveScript(this);

                m_ScriptEngine.RemoveFromEventQueue(ItemID, localID);
                if (m_ScriptEngine.Errors.ContainsKey(ItemID))
                    m_ScriptEngine.Errors.Remove(ItemID);

                #region Clean out script parts
                /* 
            part.RemoveParticleSystem();
            part.SetText("");
            Primitive.TextureAnimation tani= new Primitive.TextureAnimation();
            tani.Flags = Primitive.TextureAnimMode.ANIM_OFF;
            tani.Face = 255;
            tani.Length = 0;
            tani.Rate = 0;
            tani.SizeX = 0;
            tani.SizeY = 0;
            tani.Start = 0;
            part.AddTextureAnimation(tani);
            part.AngularVelocity = Vector3.Zero;


            part.ScheduleTerseUpdate();
            part.SendTerseUpdateToAllClients();
            part.ParentGroup.HasGroupChanged = true;
            part.ParentGroup.ScheduleGroupForFullUpdate();
            */
                #endregion

                if (Script != null)
                {
                    // Stop long command on script
                    AsyncCommandManager.RemoveScript(m_ScriptEngine, localID, ItemID);
                    Script.Close();
                    Script.Dispose();
                    Script = null;
                    ScriptSponsor.Close();
                    ILease lease = (ILease)RemotingServices.GetLifetimeService(Script as ScriptBaseClass);
                    lease.Unregister(ScriptSponsor);
                }

                try
                {
                    if (AppDomain == null)
                        return;

                    try
                    {
                        // Tell AppDomain that we have stopped script
                        ScriptEngine.AppDomainManager.UnloadScriptAppDomain(AppDomain);
                        AppDomain = null;
                    }
                    //Legit: If the script had an error, this can happen... really shouldn't, but it does.
                    catch (Exception) { }
                }
                catch (Exception e)
                {
                    m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: Exception stopping script localID: " + localID + " LLUID: " + ItemID.ToString() + ": " + e.ToString());
                }
            }
            catch { }
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
            if (Script == null)
                return;
            //Release controls over people.
            ReleaseControls();
            //Must be posted immediately, otherwise the next line will delete it.
            m_ScriptEngine.PostObjectEvent(localID, new EventParams(
                    "state_exit", new object[0] { },
                    new DetectParams[0]));
            //Remove items from the queue.
            m_ScriptEngine.RemoveFromEventQueue(ItemID, localID);
            //Reset the state to default
            State = "default";
            //Tell the SOP about the change.
            part.SetScriptEvents(ItemID,
                                 (int)Script.GetStateEventFlags(State));
            //Reset all variables back to their original values.
            Script.ResetVars();
            //Fire state_entry
            if (ScriptEngine.NeedsRemoved.ContainsKey(ItemID))
                ScriptEngine.NeedsRemoved.Remove(ItemID);
            m_ScriptEngine.AddToScriptQueue(this, "state_entry", new DetectParams[0], new object[] { });
            m_log.InfoFormat("[{0}]: Reset Script {1}", m_ScriptEngine.ScriptEngineName, ItemID);
        }
        #endregion

        #region Helpers

        //Makes ToString look nicer
        public override string ToString()
        {
            return "localID: " + localID + ", itemID: " + ItemID;
        }

        /// <summary>
        /// Sets up the APIs for the script
        /// </summary>
        internal void SetApis()
        {
            Apis = new Dictionary<string, IScriptApi>();

            ApiManager am = new ApiManager();
            foreach (string api in am.GetApis())
            {
                Apis[api] = am.CreateApi(api);
                Apis[api].Initialize(m_ScriptEngine, part, localID, ItemID, m_ScriptEngine.ScriptProtection);
            }
            foreach (KeyValuePair<string, IScriptApi> kv in Apis)
            {
                Script.InitApi(kv.Key, kv.Value);
            }
        }

        public void ShowError(Exception e, int stage, bool reupload)
        {
            if (presence != null && (!PostOnRez))
                presence.ControllingClient.SendAgentAlertMessage("Script saved with errors, check debug window!", false);

            if (reupload)
                m_ScriptEngine.Errors[ItemID] = new String[] { e.Message.ToString() };
            
            try
            {
                // DISPLAY ERROR INWORLD
                string consoletext = "Error compiling script in stage " + stage + ":\n" + e.Message.ToString() + " itemID: " + ItemID + ", localID" + localID + ", CompiledFile: " + AssemblyName;
                //m_log.Error(consoletext);
                string inworldtext = "Error compiling script: " + e;
                if (inworldtext.Length > 1100)
                    inworldtext = inworldtext.Substring(0, 1099);

                World.SimChat(OpenMetaverse.Utils.StringToBytes(inworldtext), ChatTypeEnum.DebugChannel, 2147483647, part.AbsolutePosition, part.Name, part.UUID, false);
                // LEGIT: User Scripting
            }
            catch (Exception e2)
            {
                m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: Error displaying error in-world: " + e2.ToString());
                m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: " + "Errormessage: Error compiling script:\r\n" + e2.Message.ToString());
            }
            throw e;
        }

        #endregion

        #region Start Script

        /// <summary>
        /// Fires the events after the compiling has occured
        /// </summary>
        public void FireEvents()
        {
            m_ScriptEngine.AddToScriptQueue(this, "state_entry", new DetectParams[0], new object[] { });
            if (PostOnRez)
                m_ScriptEngine.AddToScriptQueue(this, "on_rez", new DetectParams[0], new object[] { new LSL_Types.LSLInteger(StartParam) });

            if (stateSource == StateSource.AttachedRez)
                m_ScriptEngine.AddToScriptQueue(this, "attach", new DetectParams[0], new object[] { new LSL_Types.LSLString(part.AttachedAvatar.ToString()) });
            else if (stateSource == StateSource.NewRez)
                m_ScriptEngine.AddToScriptQueue(this, "changed", new DetectParams[0], new Object[] { new LSL_Types.LSLInteger(256) });
            else if (stateSource == StateSource.PrimCrossing)
                // CHANGED_REGION
                m_ScriptEngine.AddToScriptQueue(this, "changed", new DetectParams[0], new Object[] { new LSL_Types.LSLInteger(512) });
        }

        /// <summary>
        /// This starts the script and sets up the variables.
        /// </summary>
        /// <returns></returns>
        public void Start(bool reupload)
        {
            //Clear out the removing of events for this script.
            if (ScriptEngine.NeedsRemoved.ContainsKey(ItemID))
                ScriptEngine.NeedsRemoved.Remove(ItemID);

            Compiling = true;

            //Remove any script errors that might be waiting.
            if (m_ScriptEngine.Errors.ContainsKey(ItemID))
                m_ScriptEngine.Errors.Remove(ItemID);


            DateTime StartTime = DateTime.Now.ToUniversalTime();

            part = World.GetSceneObjectPart(localID);

            //No SOP, no compile.
            if (null == part)
            {
                m_log.ErrorFormat("[{0}]: Could not find scene object part corresponding " + "to localID {1} to start script", m_ScriptEngine.ScriptEngineName, localID);
                throw new NullReferenceException();
            }

            //Find the asset ID
            if (part.TaskInventory.TryGetValue(ItemID, out InventoryItem))
                AssetID = InventoryItem.AssetID;
            //m_log.Warn(InventoryItem.Name);
            //Try to see if this was rezzed from someone's inventory
            UserInventoryItemID = part.FromUserInventoryItemID;
            //Try to find the avatar who started this.
            presence = World.GetScenePresence(InventoryItem.OwnerID);

            //Set the thread culture and etc.
            CultureInfo USCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = USCulture;

            //Attempt to find previously compiled assemblys
            string FilePrefix = "CommonCompiler";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                FilePrefix = FilePrefix.Replace(c, '_');
            }

            AssemblyName = Path.Combine("ScriptEngines", Path.Combine(
                    m_ScriptEngine.World.RegionInfo.RegionID.ToString(),
                    FilePrefix + "_compiled_" + ItemID.ToString() + ".dll"));
         
            //Macrothreading
            #region Class and interface reader

            string Inherited = "";
            string ClassName = "";

            if (m_ScriptEngine.ScriptProtection.AllowMacroScripting)
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
                    string webSite = ReadExternalWebsite(URL);
                    m_ScriptEngine.ScriptProtection.AddNewClassSource(URL, webSite, null);
                    m_ScriptEngine.ScriptProtection.AddWantedSRC(ItemID, URL);
                }
                if (Source.Contains("#Include "))
                {
                    string WantedClass = "";
                    int line = Source.IndexOf("#Include ");
                    WantedClass = Source.Split('\n')[line];
                    WantedClass = WantedClass.Replace("#Include ", "");
                    Source = Source.Replace("#Include " + WantedClass, "");
                    m_ScriptEngine.ScriptProtection.AddWantedSRC(ItemID, WantedClass);
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
                    string webSite = ReadExternalWebsite(URL);
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
                    m_ScriptEngine.ScriptProtection.AddWantedSRC(ItemID, WantedClass);
                    WantedClass = "";
                }
            }

            #endregion

            bool NeedsToCreateNewAppDomain = true;

            if (reupload)
            {
                //Null everything and don't fire any events
                CloseAndDispose(true);
                m_ScriptEngine.ScriptProtection.RemovePreviouslyCompiled(Source);

                AssemblyName = Path.Combine("ScriptEngines", Path.Combine(
                    m_ScriptEngine.World.RegionInfo.RegionID.ToString(),
                    FilePrefix + "_compiled_" + ItemID.ToString() + ".dll"));
                AssemblyName = AssemblyName.Remove(AssemblyName.Length - 4);
                AssemblyName += "A.dll";
                LineMap = new Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>>();
                Running = true;
                Disabled = false;
                //Clear out the removing of events for this script.
                if (ScriptEngine.NeedsRemoved.ContainsKey(ItemID))
                    ScriptEngine.NeedsRemoved.Remove(ItemID);
            }
            //Try to find a previously compiled script in this instance
            ScriptData PreviouslyCompiledID = (ScriptData)m_ScriptEngine.ScriptProtection.TryGetPreviouslyCompiledScript(Source);

            //If the previous compile is there, retrive that
            if (PreviouslyCompiledID != null)
            {
                ClassID = PreviouslyCompiledID.ClassID;
                LineMap = PreviouslyCompiledID.LineMap;
                AssemblyName = PreviouslyCompiledID.AssemblyName;
                if (!File.Exists(AssemblyName))
                {
                    ClassID = "";
                    LineMap = new Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>>();
                    //We can reuse the assembly name
                }
            }
            //Otherwise, compile the script.
            else
            {
                try
                {
                    ScriptEngine.LSLCompiler.PerformScriptCompile(Source, AssetID, InventoryItem.OwnerID, ItemID, Inherited, ClassName, m_ScriptEngine.ScriptProtection, localID, AssemblyName, this, out AssemblyName,
                                                                         out LineMap, out ClassID);
                    #region Warnings

                    string[] compilewarnings = ScriptEngine.LSLCompiler.GetWarnings();

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

                            World.SimChat(OpenMetaverse.Utils.StringToBytes(text), ChatTypeEnum.DebugChannel, 2147483647, part.AbsolutePosition, part.Name, part.UUID, false);
                        }
                    }

                    #endregion
                }
                catch (Exception ex)
                {
                    ShowError(ex, 1, reupload);
                }
            }

            bool useDebug = false;
            if (useDebug)
            {
                TimeSpan t = (DateTime.Now.ToUniversalTime() - StartTime);
                m_log.Debug("Stage 1: " + t.TotalSeconds);
            }

            //Create the app domain if needed.
            if (NeedsToCreateNewAppDomain)
            {
                try
                {
                    if (ClassName != "")
                        Script = ScriptEngine.AppDomainManager.LoadScript(AssemblyName, "Script." + ClassName, out AppDomain);
                    else
                        Script = ScriptEngine.AppDomainManager.LoadScript(AssemblyName, "Script." + ClassID, out AppDomain);
                    m_ScriptEngine.ScriptProtection.AddPreviouslyCompiled(Source, this);
                }
                catch (Exception ex)
                {
                    ShowError(ex, 2, reupload);
                }
            }

            ScriptSponsor = new ScriptSponsor();
            ILease lease = (ILease)RemotingServices.GetLifetimeService(Script as ScriptBaseClass);
            lease.Register(ScriptSponsor);
            //If its a reupload, an avatar is waiting for the script errors
            if (reupload)
                m_ScriptEngine.Errors[ItemID] = new String[] { "SUCCESSFULL" };

            if (useDebug)
            {
                TimeSpan t = (DateTime.Now.ToUniversalTime() - StartTime);
                m_log.Debug("Stage 2: " + t.TotalSeconds);
            }

            //ALWAYS reset up APIs, otherwise m_host doesn't get updated and LSL thinks its in another prim.
            SetApis();

            // Add it to our script memstruct so it can be found by other scripts
            m_ScriptEngine.UpdateScriptInstanceData(this);

            //Set the event flags
            int eventFlags = Script.GetStateEventFlags(State);
            part.SetScriptEvents(ItemID, eventFlags);

            FireEvents();

            //All done, compiled successfully
            Compiling = false;
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