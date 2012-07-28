/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
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
using Aurora.Framework;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class ScriptData
    {
        #region Constructor

        public ScriptData(ScriptEngine engine)
        {
            m_ScriptEngine = engine;

            NextEventDelay.Add("at_rot_target", 0);
            NextEventDelay.Add("at_target", 0);
            NextEventDelay.Add("attach", 0);
            NextEventDelay.Add("changed", 0);
            NextEventDelay.Add("collision", 0);
            NextEventDelay.Add("collision_end", 0);
            NextEventDelay.Add("collision_start", 0);
            NextEventDelay.Add("control", 0);
            NextEventDelay.Add("dataserver", 0);
            NextEventDelay.Add("email", 0);
            NextEventDelay.Add("http_response", 0);
            NextEventDelay.Add("http_request", 0);
            NextEventDelay.Add("land_collision", 0);
            NextEventDelay.Add("land_collision_end", 0);
            NextEventDelay.Add("land_collision_start", 0);
            //Don't limit link_message, too important!
            //NextEventDelay.Add("link_message", 0);
            NextEventDelay.Add("listen", 0);
            NextEventDelay.Add("money", 0);
            NextEventDelay.Add("moving_end", 0);
            NextEventDelay.Add("moving_start", 0);
            NextEventDelay.Add("no_sensor", 0);
            NextEventDelay.Add("not_at_rot_target", 0);
            NextEventDelay.Add("not_at_target", 0);
            NextEventDelay.Add("object_rez", 0);
            NextEventDelay.Add("on_rez", 0);
            NextEventDelay.Add("remote_data", 0);
            NextEventDelay.Add("run_time_permissions", 0);
            NextEventDelay.Add("sensor", 0);
            NextEventDelay.Add("state_entry", 0);
            NextEventDelay.Add("state_exit", 0);
            NextEventDelay.Add("timer", 0);
            NextEventDelay.Add("touch", 0);
            NextEventDelay.Add("touch_end", 0);
            NextEventDelay.Add("touch_start", 0);
        }

        #endregion

        #region Declares

        private readonly Dictionary<string, long> NextEventDelay = new Dictionary<string, long>();

        //This is the UUID of the actual script.
        private readonly ScriptEngine m_ScriptEngine;
        public Dictionary<string, IScriptApi> Apis = new Dictionary<string, IScriptApi>();
        public AppDomain AppDomain;
        public string AssemblyName;
        public List<Changed> ChangedInQueue = new List<Changed>();
        private double CollisionEventDelayTicks = 0.13;
        public bool CollisionInQueue;
        public bool TimerInQueue;
        public bool TouchInQueue;
        public bool SensorInQueue;
        public bool NoSensorInQueue;
        public bool AtTargetInQueue;
        public bool NotAtTargetInQueue;
        public bool AtRotTargetInQueue;
        public bool NotAtRotTargetInQueue;
        public bool MovingInQueue;
        public bool LandCollisionInQueue;
        public bool RemoveCollisionEvents;
        public bool RemoveLandCollisionEvents;
        public bool RemoveTouchEvents;
        public bool Compiled;
        public int ControlEventsInQueue;
        private double DefaultEventDelayTicks = 0.05;
        public string DefaultState = "";
        public bool Disabled;


        public long EventDelayTicks;
        public bool IgnoreNew;
        public TaskInventoryItem InventoryItem;
        public UUID ItemID;
        public int LastControlLevel;
        public DetectParams[] LastDetectParams;
        public bool Loading = true;
        public long NextEventTimeTicks;
        public ISceneChildEntity Part;
        public OSDMap PluginData = new OSDMap();
        public bool PostOnRez;
        public UUID RezzedFrom = UUID.Zero; // If rezzed from llRezObject, this is not Zero
        public bool Running = true;
        public IScript Script;
        public object ScriptEventLock = new object();
        public int ScriptScore;
        public string Source = "";
        public int StartParam;
        public bool StartedFromSavedState;
        public string State;
        public bool Suspended;
        public bool TargetOmegaWasSet;
        private double TimerEventDelayTicks = 0.01;
        private double TouchEventDelayTicks = 0.1;
        private const long TicksPerMillisecond = 1000;
        public UUID UserInventoryItemID;

        /// <summary>
        ///   This helps make sure that we clear out previous versions so that we don't have overlapping script versions running
        /// </summary>
        public long VersionID;

        public IScene World;
        public StateSource stateSource;

        #endregion

        #region Close/Suspend Script

        public void Suspend()
        {
        }

        /// <summary>
        ///   This closes the scrpit, removes it from any known spots, and disposes of itself.
        /// </summary>
        /// <param name = "shouldbackup">Should we back up this script and fire state_exit?</param>
        public void CloseAndDispose(bool shouldbackup)
        {
            m_ScriptEngine.MaintenanceThread.RemoveFromEventSchQueue(this, true);

            if (shouldbackup)
            {
                if (Script != null)
                {
                    /*
                    //Fire this directly so its not closed before its fired
                    SetEventParams("state_exit", new DetectParams[0]);

                    m_ScriptEngine.MaintenanceThread.ProcessQIS(new QueueItemStruct()
                    {
                        ID = this,
                        CurrentlyAt = null,
                        functionName = "state_exit",
                        param = new object[0],
                        llDetectParams = new DetectParams[0],
                        VersionID = VersionID
                    });
                    */
                    // dont think we should fire state_exit here
                    //                    m_ScriptEngine.MaintenanceThread.DoAndWaitEventSch(this, "state_exit",
                    //                        new DetectParams[0], VersionID, EventPriority.FirstStart, new object[0]);
                    m_ScriptEngine.StateSave.SaveStateTo(this);
                }
            }
            if (Script != null)
            {
                //Fire the exit event for scripts that support it
                Exception ex;
                EnumeratorInfo info = null;
                while ((info = Script.ExecuteEvent(State, "exit", new object[0], info, out ex))
                       != null)
                {
                }
            }
            m_ScriptEngine.MaintenanceThread.SetEventSchSetIgnoreNew(this, false);

            //Give the user back any controls we took
            ReleaseControls();

            // Tell script not to accept new requests
            //These are fine to set as the state wont be saved again
            if (shouldbackup)
            {
                Running = false;
                Disabled = true;
            }

            // Remove from internal structure
            ScriptEngine.ScriptProtection.RemoveScript(this);
            //            if (!Silent) //Don't remove on a recompile because we'll make it under a different assembly
            //                ScriptEngine.ScriptProtection.RemovePreviouslyCompiled(Source);

            //Remove any errors that might be sitting around
            m_ScriptEngine.ScriptErrorReporter.RemoveError(ItemID);

            #region Clean out script parts

            //Only if this script changed target omega do we reset it
            if (TargetOmegaWasSet)
            {
                Part.AngularVelocity = Vector3.Zero; // Removed in SL
                Part.ScheduleUpdate(PrimUpdateFlags.AngularVelocity); // Send changes to client.
            }

            #endregion

            if (Script != null)
            {
                // Stop long command on script
                m_ScriptEngine.RemoveScriptFromPlugins(Part.UUID, ItemID);

                //Release the script and destroy it
                ILease lease = (ILease)RemotingServices.GetLifetimeService(Script as MarshalByRefObject);
                if (lease != null)
                    lease.Unregister(Script.Sponsor);

                Script.Close();
                Script = null;
            }

            if(InventoryItem != null && Part != null)
                MainConsole.Instance.Debug("[" + m_ScriptEngine.ScriptEngineName + "]: Closed Script " + InventoryItem.Name + " in " +
                            Part.Name);
            if (AppDomain == null)
                return;

            // Tell AppDomain that we have stopped script
            m_ScriptEngine.AppDomainManager.UnloadScriptAppDomain(AppDomain);
            AppDomain = null;
        }

        /// <summary>
        ///   Removes any permissions the script may have on other avatars.
        /// </summary>
        /// <param name = "localID"></param>
        /// <param name = "itemID"></param>
        private void ReleaseControls()
        {
            if (InventoryItem != null)
            {
                if (Part != null)
                {
                    int permsMask = InventoryItem.PermsMask;
                    UUID permsGranter = InventoryItem.PermsGranter;

                    IScenePresence sp = World.GetScenePresence(permsGranter);
                    if ((permsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        if (sp != null)
                        {
                            IScriptControllerModule m = sp.RequestModuleInterface<IScriptControllerModule>();
                            if (m != null)
                                m.UnRegisterControlEventsToScript(Part.LocalId, ItemID);
                        }
                    }
                }

                InventoryItem.PermsMask = 0;
                InventoryItem.PermsGranter = UUID.Zero;
            }
        }

        #endregion

        #region Reset Script and State Change

        public void ResetEvents()
        {
            RemoveCollisionEvents = false;
            RemoveTouchEvents = false;
            RemoveLandCollisionEvents = false;
            TouchInQueue = false;
            LandCollisionInQueue = false;
            SensorInQueue = false;
            NoSensorInQueue = false;
            TimerInQueue = false;
            AtTargetInQueue = false;
            NotAtTargetInQueue = false;
            AtRotTargetInQueue = false;
            NotAtRotTargetInQueue = false;
            ChangedInQueue.Clear();
            LastControlLevel = 0;
            ControlEventsInQueue = 0;
        }

        /// <summary>
        ///   This resets the script back to its default state.
        /// </summary>
        internal void Reset()
        {
            if (Script == null)
                return;
            //Unset the events that may still be firing after the change.
            m_ScriptEngine.RemoveScriptFromPlugins(Part.UUID, ItemID);
            //Remove other items from the queue.
            m_ScriptEngine.MaintenanceThread.RemoveFromEventSchQueue(this, false);
            // let current InExec finish or lsl reset fails

            //Release controls over people.
            ReleaseControls();
            //Reset the state to default
            State = DefaultState;
            //Reset all variables back to their original values.
            Script.ResetVars();
            //Tell the SOP about the change.
            Part.SetScriptEvents(ItemID, !Running ? 0 : Script.GetStateEventFlags(State));

            //Remove MinEventDelay
            EventDelayTicks = 0;
            //Remove events that may be fired again after the user stops touching the prim, etc
            ResetEvents();
            // These will be removed after the next ***_start event, and will remove ones that are not finished yet
            RemoveLandCollisionEvents = true;
            RemoveCollisionEvents = true;
            RemoveTouchEvents = true;

            //Fire state_entry
            m_ScriptEngine.StateSave.SaveStateTo(this, true);
            m_ScriptEngine.MaintenanceThread.SetEventSchSetIgnoreNew(this, false); // accept new events
            m_ScriptEngine.AddToScriptQueue(this, "state_entry", new DetectParams[0], EventPriority.FirstStart,
                                            new object[] { });

            MainConsole.Instance.Debug("[" + m_ScriptEngine.ScriptEngineName + "]: Reset Script " + ItemID);
        }

        internal void ChangeState(string state)
        {
            if (State != state)
            {
                //Technically, we should do this,
                //but we would need to remove timer/listen/sensor events as well to keep compat with SL style lsl
                //m_ScriptEngine.MaintenanceThread.RemoveFromEventSchQueue (this, false);
                //m_ScriptEngine.MaintenanceThread.SetEventSchSetIgnoreNew (this, false); // accept new events
                
                //Remove us from timer, listen, and sensor events
                m_ScriptEngine.RemoveScriptFromChangedStatePlugins(this);

                //Fire state_exist after we switch over all the removing of events so that it gets the new versionID
                m_ScriptEngine.MaintenanceThread.AddEventSchQueue(this, "state_exit",
                                                                  new DetectParams[0], EventPriority.FirstStart,
                                                                  new object[0] { });

                State = state;

                //Remove events that may be fired again after the user stops touching the prim, etc
                // These will be removed after the next ***_start event
                RemoveLandCollisionEvents = true;
                RemoveCollisionEvents = true;
                RemoveTouchEvents = true;

                //Tell the SOP about the change.
                Part.SetScriptEvents(ItemID, Script.GetStateEventFlags(state));
                ScriptEngine.ScriptProtection.AddNewScript(this);

                m_ScriptEngine.MaintenanceThread.AddEventSchQueue(this, "state_entry",
                                                                  new DetectParams[0], EventPriority.FirstStart,
                                                                  new object[0] { });
                //Save a state save after a state change, its a large change in the script's function
                m_ScriptEngine.StateSave.SaveStateTo(this, true);
            }
        }

        #endregion

        #region Helpers

        //Makes ToString look nicer
        public override string ToString()
        {
            return "UUID: " + Part.UUID + ", itemID: " + ItemID;
        }

        /// <summary>
        ///   Sets up the APIs for the script
        /// </summary>
        /// <param name = "setInitialResetValues">Get the initial reset values needed to reset the script</param>
        internal void SetApis()
        {
            Apis = new Dictionary<string, IScriptApi>();

            foreach (IScriptApi api in m_ScriptEngine.GetAPIs())
            {
                Apis[api.Name] = api;
                Apis[api.Name].Initialize(m_ScriptEngine, Part, Part.LocalId, ItemID, ScriptEngine.ScriptProtection);
                Script.InitApi(api);
            }
            //We must always do this, as reset doesn't care whether there is a state save or not, we must have the defaults
            Script.UpdateInitialValues();
        }

        public void DisplayUserNotification(string message, string stage, bool postScriptCAPSError, bool IsError)
        {
            if (message == "")
                return; //No blank messages

            IScenePresence presence = World.GetScenePresence(Part.OwnerID);
            if (presence != null && (!PostOnRez) && postScriptCAPSError)
                presence.ControllingClient.SendAgentAlertMessage(
                    m_ScriptEngine.ChatCompileErrorsToDebugChannel
                        ? "Script saved with errors, check debug window!"
                        : "Script saved with errors!",
                    false);

            if (postScriptCAPSError)
                m_ScriptEngine.ScriptErrorReporter.AddError(ItemID, new ArrayList(message.Split('\n')));

            // DISPLAY ERROR ON CONSOLE
            if (m_ScriptEngine.DisplayErrorsOnConsole)
            {
                string consoletext = IsError ? "Error " : "Warning ";
                consoletext += stage + " script:\n" + message + " prim name: " + Part.Name + "@ " +
                    Part.AbsolutePosition + (InventoryItem != null ? ("item name: " + InventoryItem.Name) : (" itemID: " + ItemID)) + ", CompiledFile: " + AssemblyName;
                MainConsole.Instance.Error(consoletext);
            }

            // DISPLAY ERROR INWORLD
            string inworldtext = IsError ? "Error " : "Warning ";
            inworldtext += stage + " script: " + message;
            if (inworldtext.Length > 1100)
                inworldtext = inworldtext.Substring(0, 1099);

            if (m_ScriptEngine.ChatCompileErrorsToDebugChannel)
            {
                IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
                if (chatModule != null)
                    chatModule.SimChat(inworldtext, ChatTypeEnum.DebugChannel,
                                       2147483647, Part.AbsolutePosition, Part.Name, Part.UUID, false, World);
            }
            m_ScriptEngine.ScriptFailCount++;
            m_ScriptEngine.ScriptErrorMessages += inworldtext;
        }

        #endregion

        #region Start Script

        /// <summary>
        ///   Fires the events after the compiling has occured
        /// </summary>
        public void FireEvents()
        {
            if (RezzedFrom != UUID.Zero)
            {
                //Post the event for the prim that rezzed us
                m_ScriptEngine.AddToObjectQueue(RezzedFrom, "object_rez", new DetectParams[0],
                                                new object[] { (LSL_Types.LSLString)Part.ParentEntity.RootChild.UUID.ToString() });
                RezzedFrom = UUID.Zero;
            }
            if (StartedFromSavedState)
            {
                if (PostOnRez)
                    m_ScriptEngine.AddToScriptQueue(this, "on_rez", new DetectParams[0], EventPriority.FirstStart,
                                                    new object[] { new LSL_Types.LSLInteger(StartParam) });

                if (stateSource == StateSource.AttachedRez)
                    m_ScriptEngine.AddToScriptQueue(this, "attach", new DetectParams[0], EventPriority.FirstStart,
                                                    new object[] { new LSL_Types.LSLString(Part.AttachedAvatar.ToString()) });
                else if (stateSource == StateSource.RegionStart)
                    // CHANGED_REGION_START
                    m_ScriptEngine.AddToScriptQueue(this, "changed", new DetectParams[0], EventPriority.FirstStart,
                                                    new Object[] { new LSL_Types.LSLInteger((int)Changed.REGION_RESTART) });
                else if (stateSource == StateSource.PrimCrossing)
                    // CHANGED_REGION
                    // note: CHANGED_TELEPORT should occur on any teleport of an attachment within a region too and is taken care of elsewhere
                    m_ScriptEngine.AddToScriptQueue(this, "changed", new DetectParams[0], EventPriority.FirstStart,
                                                    new Object[] { new LSL_Types.LSLInteger((int)Changed.REGION) });
                // note: StateSource.NewRez doesn't do anything (PostOnRez controls on_rez)
            }
            else
            {
                m_ScriptEngine.AddToScriptQueue(this, "state_entry", new DetectParams[0], EventPriority.FirstStart,
                                                new object[0]);

                if (PostOnRez)
                    m_ScriptEngine.AddToScriptQueue(this, "on_rez", new DetectParams[0], EventPriority.FirstStart,
                                                    new object[] { new LSL_Types.LSLInteger(StartParam) });

                if (stateSource == StateSource.AttachedRez)
                    m_ScriptEngine.AddToScriptQueue(this, "attach", new DetectParams[0], EventPriority.FirstStart,
                                                    new object[] { new LSL_Types.LSLString(Part.AttachedAvatar.ToString()) });
            }
        }

        /// <summary>
        ///   This starts the script and sets up the variables.
        /// </summary>
        /// <returns></returns>
        public bool Start(LUStruct startInfo)
        {
            bool reupload = startInfo.Action == LUType.Reupload;
            DateTime StartTime = DateTime.Now.ToUniversalTime();
            Running = true;
            Suspended = false;

            //Clear out the removing of events for this script.
            IgnoreNew = false;
            Interlocked.Increment(ref VersionID);

            //Reset this
            StartedFromSavedState = false;

            //Clear out previous errors if they were not cleaned up
            m_ScriptEngine.ScriptErrorReporter.RemoveError(ItemID);

            //Find the inventory item
            Part.TaskInventory.TryGetValue(ItemID, out InventoryItem);

            if (InventoryItem == null)
            {
                MainConsole.Instance.Warn("[ADNE]: Could not find inventory item for script " + ItemID + ", part" + Part.Name + "@" +
                           Part.AbsolutePosition);
                return false;
            }

            //Try to see if this was rezzed from someone's inventory
            UserInventoryItemID = Part.FromUserInventoryItemID;

            //Try to find the avatar who started this.
            IScenePresence presence = World.GetScenePresence(Part.OwnerID);


            if (startInfo.ClearStateSaves)
                m_ScriptEngine.StateSave.DeleteFrom(this);
            //Now that the initial loading is complete,
            // we need to find the state save and start loading the info from it
            StateSave LastStateSave = m_ScriptEngine.StateSave.FindScriptStateSave(this);
            if (!reupload && Loading && LastStateSave != null)
            {
                //Deserialize the most important pieces first
                Source = LastStateSave.Source;
            }
            if (string.IsNullOrEmpty(Source))
            {
                AssetBase asset = Part.ParentEntity.Scene.AssetService.Get(InventoryItem.AssetID.ToString());
                if (null == asset)
                {
                    MainConsole.Instance.ErrorFormat(
                        "[ScriptData]: " +
                        "Couldn't start script {0}, {1} at {2} in {3} since asset ID {4} could not be found",
                        InventoryItem.Name, InventoryItem.ItemID, Part.AbsolutePosition,
                        Part.ParentEntity.Scene.RegionInfo.RegionName, InventoryItem.AssetID);
                    ScriptEngine.ScriptProtection.RemoveScript(this);
                    return false;
                }
                Source = Utils.BytesToString(asset.Data);
            }
            if (string.IsNullOrEmpty(Source))
            {
                MainConsole.Instance.ErrorFormat(
                    "[ScriptData]: " +
                    "Couldn't start script {0}, {1} at {2} in {3} since asset ID {4} could not be found",
                    InventoryItem.Name, InventoryItem.ItemID, Part.AbsolutePosition,
                    Part.ParentEntity.Scene.RegionInfo.RegionName, InventoryItem.AssetID);
                ScriptEngine.ScriptProtection.RemoveScript(this);
                return false;
            }

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
                    string webSite = Utilities.ReadExternalWebsite(URL);
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

            //Find the default state save
            DefaultState = m_ScriptEngine.Compiler.FindDefaultStateForScript(Source);
            State = DefaultState;

            //If the saved state exists, if it isn't a reupload (something changed), and if the assembly exists, load the state save
            if (!reupload && Loading && LastStateSave != null
                && File.Exists(LastStateSave.AssemblyName))
            {
                //Retrive the previous assembly
                AssemblyName = LastStateSave.AssemblyName;
            }
            else
            {
                Compiled = false;
                //if (!reupload && Loading && LastStateSave != null && !LastStateSave.Compiled)
                //    return false;//If we're trying to start up and we failed before, just give up
                if (reupload)
                {
                    LastStateSave = null;
                    //Close the previous script
                    CloseAndDispose(false); //We don't want to back it up
                    Interlocked.Increment(ref VersionID);
                    m_ScriptEngine.MaintenanceThread.SetEventSchSetIgnoreNew(this, false); // accept new events
                }

                //Try to find a previously compiled script in this instance
                string PreviouslyCompiledAssemblyName =
                    ScriptEngine.ScriptProtection.TryGetPreviouslyCompiledScript(Source);
                if (PreviouslyCompiledAssemblyName != null)
                    //Already exists in this instance, so we do not need to check whether it exists
                    AssemblyName = PreviouslyCompiledAssemblyName;
                else
                {
                    try
                    {
                        m_ScriptEngine.Compiler.PerformScriptCompile(Source, ItemID, Part.OwnerID, out AssemblyName);

                        #region Errors and Warnings

                        #region Errors

                        string[] compileerrors = m_ScriptEngine.Compiler.GetErrors();

                        if (compileerrors.Length != 0)
                        {
                            string error = string.Empty;
                            foreach (string compileerror in compileerrors)
                            {
                                error += compileerror;
                            }
                            DisplayUserNotification(error, "compiling", reupload, true);
                            //It might have failed, but we still need to add it so that we can reuse this script data class later
                            ScriptEngine.ScriptProtection.AddNewScript(this);
                            m_ScriptEngine.StateSave.SaveStateTo(this, true);
                            return false;
                        }

                        #endregion

                        #region Warnings

                        if (m_ScriptEngine.ShowWarnings)
                        {
                            string[] compilewarnings = m_ScriptEngine.Compiler.GetWarnings();

                            if (compilewarnings != null && compilewarnings.Length != 0)
                            {
                                string error = string.Empty;
                                foreach (string compileerror in compilewarnings)
                                {
                                    error += compileerror;
                                }
                                DisplayUserNotification(error, "compiling", reupload, false);
                                //It might have failed, but we still need to add it so that we can reuse this script data class later
                                ScriptEngine.ScriptProtection.AddNewScript(this);
                                return false;
                            }
                        }

                        #endregion

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        //LEAVE IT AS ToString() SO THAT WE GET THE STACK TRACE TOO
                        DisplayUserNotification(ex.ToString(), "(exception) compiling", reupload, true);
                        //It might have failed, but we still need to add it so that we can reuse this script data class later
                        ScriptEngine.ScriptProtection.AddNewScript(this);
                        return false;
                    }
                }
            }

            bool useDebug = false;
            if (useDebug)
                MainConsole.Instance.Debug("[" + m_ScriptEngine.ScriptEngineName + "]: Stage 1 compile: " +
                            (DateTime.Now.ToUniversalTime() - StartTime).TotalSeconds);

            //Create the app domain if needed.
            try
            {
                Script = m_ScriptEngine.AppDomainManager.LoadScript(AssemblyName, "Script.ScriptClass", out AppDomain);
                m_ScriptEngine.Compiler.FinishCompile(this, Script);
                //Add now so that we don't add it too early and give it the possibility to fail
                ScriptEngine.ScriptProtection.AddPreviouslyCompiled(Source, this);
            }
            catch (FileNotFoundException) // Not valid!!!
            {
                MainConsole.Instance.Error("[" + m_ScriptEngine.ScriptEngineName +
                            "]: File not found in app domain creation. Corrupt state save! " + AssemblyName);
                ScriptEngine.ScriptProtection.RemovePreviouslyCompiled(Source);
                return Start(startInfo); // Lets restart the script if this happens
            }
            catch (Exception ex)
            {
                DisplayUserNotification(ex.ToString(), "app domain creation", reupload, true);
                //It might have failed, but we still need to add it so that we can reuse this script data class later
                ScriptEngine.ScriptProtection.AddNewScript(this);
                return false;
            }
            Source = null; //Don't keep it in memory, we don't need it anymore
            Compiled = true; //We compiled successfully

            //ILease lease = (ILease)RemotingServices.GetLifetimeService(Script as MarshalByRefObject);
            //if (lease != null) //Its null if it is all running in the same app domain
            //    lease.Register(Script.Sponsor);

            //If its a reupload, an avatar is waiting for the script errors
            if (reupload)
                m_ScriptEngine.ScriptErrorReporter.AddError(ItemID, new ArrayList(new[] { "SUCCESSFULL" }));

            if (useDebug)
                MainConsole.Instance.Debug("[" + m_ScriptEngine.ScriptEngineName + "]: Stage 2 compile: " +
                            (DateTime.Now.ToUniversalTime() - StartTime).TotalSeconds);

            SetApis();

            //Now do the full state save finding now that we have an app domain.
            if (LastStateSave != null)
            {
                string assy = AssemblyName;
                // don't restore the assembly name, the one we have is right (if re-compiled or not)
                m_ScriptEngine.StateSave.Deserialize(this, LastStateSave);
                AssemblyName = assy;
                if (this.State == "" && DefaultState != this.State)
                //Sometimes, "" is a valid state for other script languages
                {
                    MainConsole.Instance.Warn("BROKEN STATE SAVE!!! - " + this.Part.Name + " @ " + this.Part.AbsolutePosition);
                    this.State = DefaultState;
                    m_ScriptEngine.StateSave.SaveStateTo(this, true);
                }
                // we get new rez events on sim restart, too
                // but if there is state, then we fire the change
                // event
                StartedFromSavedState = true;

                // ItemID changes sometimes (not sure why, but observed it)
                // If so we want to clear out the old save state,
                // which would otherwise have hung around in the object forever
                if (LastStateSave.ItemID != ItemID)
                {
                    m_ScriptEngine.StateSave.DeleteFrom(Part, LastStateSave.ItemID);
                    m_ScriptEngine.StateSave.SaveStateTo(this, true);
                }
            }
            else
            {
                //Make a new state save now
                m_ScriptEngine.StateSave.SaveStateTo(this, true);
            }

            //Set the event flags
            Part.SetScriptEvents(ItemID, Script.GetStateEventFlags(State));

            // Add it to our script memstruct so it can be found by other scripts
            ScriptEngine.ScriptProtection.AddNewScript(this);

            //All done, compiled successfully
            Loading = false;

            if (MainConsole.Instance.IsDebugEnabled)
            {
                TimeSpan time = (DateTime.Now.ToUniversalTime() - StartTime);

                MainConsole.Instance.Debug("[" + m_ScriptEngine.ScriptEngineName +
                            "]: Started Script " + InventoryItem.Name +
                            " in object " + Part.Name + "@" + Part.ParentEntity.RootChild.AbsolutePosition +
                            (presence != null ? " by " + presence.Name : "") +
                            " in region " + Part.ParentEntity.Scene.RegionInfo.RegionName +
                            " in " + time.TotalSeconds + " seconds.");
            }
            return true;
        }

        #endregion

        #region Event Processing

        public bool SetEventParams(string functionName, DetectParams[] qParams)
        {
            if (Suspended || !Running)
                return false; //No suspended scripts...
            if (qParams.Length > 0)
                LastDetectParams = qParams;

            if ( /*functionName == "control" || */functionName == "state_entry" || functionName == "on_rez" ||
                                                  functionName == "link_message")
            {
                //For vehicles, otherwise breaks them. DO NOT REMOVE UNLESS YOU FIND A BETTER WAY TO FIX
                return true;
            }

            long NowTicks = Util.EnvironmentTickCount();

            if (EventDelayTicks != 0)
            {
                if (NowTicks < NextEventTimeTicks)
                    return false;
                NextEventTimeTicks = NowTicks + EventDelayTicks;
            }
            switch (functionName)
            {
                //Times pulled from http://wiki.secondlife.com/wiki/LSL_Delay
                case "touch": //Limits for 0.1 seconds
                case "touch_start":
                case "touch_end":
                    if (NowTicks < NextEventDelay[functionName])
                        return false;
                    NextEventDelay[functionName] = NowTicks + (long)(TouchEventDelayTicks * TicksPerMillisecond);
                    break;
                case "timer": //Settable timer limiter
                    if (NowTicks < NextEventDelay[functionName])
                        return false;
                    NextEventDelay[functionName] = NowTicks + (long)(TimerEventDelayTicks * TicksPerMillisecond);
                    break;
                case "collision": //Collision limiters taken off of reporting from WhiteStar in mantis 0004513
                case "collision_start":
                case "collision_end":
                case "land_collision":
                case "land_collision_start":
                case "land_collision_end":
                    if (NowTicks < NextEventDelay[functionName])
                        return false;
                    NextEventDelay[functionName] = NowTicks + (long)(CollisionEventDelayTicks * TicksPerMillisecond);
                    break;
                case "control":
                    if (NowTicks < NextEventDelay[functionName])
                        return false;
                    NextEventDelay[functionName] = NowTicks + (long)(0.5f * TicksPerMillisecond);
                    break;
                default: //Default is 0.05 seconds for event limiting
                    if (!NextEventDelay.ContainsKey(functionName))
                        break; //If it doesn't exist, we don't limit it
                    if (NowTicks < NextEventDelay[functionName])
                        return false;
                    NextEventDelay[functionName] = NowTicks + (long)(DefaultEventDelayTicks * TicksPerMillisecond);
                    break;
            }
            //Add the event to the stats
            ScriptScore++;
            m_ScriptEngine.ScriptEPS++;
            return true;
        }

        #endregion
    }
}