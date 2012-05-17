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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class ScriptProtectionModule
    {
        #region Declares

        //List of all enabled APIs for scripts
        private List<string> EnabledAPIs = new List<string>();
        //Keeps track of whether the source has been compiled before
        public Dictionary<string, string> PreviouslyCompiled = new Dictionary<string, string>();

        public Dictionary<UUID, Dictionary<UUID, ScriptData>> Scripts =
            new Dictionary<UUID, Dictionary<UUID, ScriptData>>();

        public Dictionary<UUID, UUID> ScriptsItems = new Dictionary<UUID, UUID>();
        private bool allowHTMLLinking = true;

        //Threat Level for scripts.
        private ThreatLevel m_MaxThreatLevel = 0;
        private bool m_allowFunctionLimiting;
        private IConfig m_config;
        private ScriptEngine m_scriptEngine;
        public ThreatLevelDefinition m_threatLevelHigh;

        public ThreatLevelDefinition m_threatLevelLow;
        public ThreatLevelDefinition m_threatLevelModerate;
        public ThreatLevelDefinition m_threatLevelNone;
        public ThreatLevelDefinition m_threatLevelNuisance;
        public ThreatLevelDefinition m_threatLevelSevere;
        public ThreatLevelDefinition m_threatLevelNoAccess;
        public ThreatLevelDefinition m_threatLevelVeryHigh;
        public ThreatLevelDefinition m_threatLevelVeryLow;

        #region Limiting of functions

        private readonly Dictionary<UUID, Dictionary<string, LimitReq>> m_functionLimiting =
            new Dictionary<UUID, Dictionary<string, LimitReq>>();

        private readonly Dictionary<string, LimitDef> m_functionsToLimit = new Dictionary<string, LimitDef>();

        #region Nested type: LimitAction

        private enum LimitAction
        {
            None,
            Drop,
            Delay, //Not Implemented
            TerminateScript,
            TerminateEvent
        }

        #endregion

        #region Nested type: LimitAlert

        private enum LimitAlert
        {
            None,
            Console,
            Inworld,
            ConsoleAndInworld
        }

        #endregion

        #region Nested type: LimitDef

        private class LimitDef
        {
            /// <summary>
            ///   What action will be taken
            /// </summary>
            public LimitAction Action = LimitAction.None;

            /// <summary>
            ///   What alert will be triggered by the limitor if it does limit
            /// </summary>
            public LimitAlert Alert = LimitAlert.None;

            /// <summary>
            ///   The number of times a function can be fired over the timescale
            /// </summary>
            public int FunctionsOverTimeScale;

            /// <summary>
            ///   The max number of times a function can be fired (in total, does not decrease)
            /// </summary>
            public int MaxNumberOfTimes;

            /// <summary>
            ///   Time (in ms) that a function has to wait before being fired again
            /// </summary>
            public int TimeScale;

            /// <summary>
            ///   The group that will be limited by this function
            /// </summary>
            public LimitType Type = LimitType.None;
        }

        #endregion

        #region Nested type: LimitReq

        private class LimitReq
        {
            public int FunctionsSinceLastFired;
            public int LastFired;
            public int NumberOfTimesFired;
        }

        #endregion

        #region Nested type: LimitType

        private enum LimitType
        {
            None,
            Script,
            Owner,
            Group,
            Prim
        }

        #endregion

        #endregion

        public bool AllowHTMLLinking
        {
            get { return allowHTMLLinking; }
        }

        #endregion

        #region Constructor

        public ScriptProtectionModule(ScriptEngine engine, IConfig config)
        {
        }

        public void Initialize(ScriptEngine engine, IConfig config)
        {
            m_config = config;
            m_scriptEngine = engine;
            EnabledAPIs = new List<string>(config.GetString("AllowedAPIs", "LSL").ToLower().Split(','));

            allowHTMLLinking = config.GetBoolean("AllowHTMLLinking", true);

            #region Limitation configs

            m_allowFunctionLimiting = config.GetBoolean("AllowFunctionLimiting", false);

            foreach (string kvp in config.GetKeys())
            {
                if (kvp.EndsWith("_Limit"))
                {
                    string functionName = kvp.Remove(kvp.Length - 6);
                    LimitDef limitDef = new LimitDef();
                    string limitType = config.GetString(functionName + "_LimitType", "None");
                    string limitAlert = config.GetString(functionName + "_LimitAlert", "None");
                    string limitAction = config.GetString(functionName + "_LimitAction", "None");
                    int limitTimeScale = config.GetInt(functionName + "_LimitTimeScale", 0);
                    int limitMaxNumberOfTimes = config.GetInt(functionName + "_LimitMaxNumberOfTimes", 0);
                    int limitFunctionsOverTimeScale = config.GetInt(functionName + "_LimitFunctionsOverTimeScale", 0);

                    try
                    {
                        limitDef.Type = (LimitType)Enum.Parse(typeof(LimitType), limitType, true);
                    }
                    catch
                    {
                    }
                    try
                    {
                        limitDef.Alert = (LimitAlert)Enum.Parse(typeof(LimitAlert), limitAlert, true);
                    }
                    catch
                    {
                    }
                    try
                    {
                        limitDef.Action = (LimitAction)Enum.Parse(typeof(LimitAction), limitAction, true);
                    }
                    catch
                    {
                    }

                    limitDef.TimeScale = limitTimeScale;
                    limitDef.MaxNumberOfTimes = limitMaxNumberOfTimes;
                    limitDef.FunctionsOverTimeScale = limitFunctionsOverTimeScale;
                    m_functionsToLimit[functionName] = limitDef;
                }
            }

            #endregion

            m_threatLevelNone = new ThreatLevelDefinition(ThreatLevel.None,
                                                          UserSetHelpers.ParseUserSetConfigSetting(config, "NoneUserSet",
                                                                                                   UserSet.None), this);
            m_threatLevelNuisance = new ThreatLevelDefinition(ThreatLevel.Nuisance,
                                                              UserSetHelpers.ParseUserSetConfigSetting(config,
                                                                                                       "NuisanceUserSet",
                                                                                                       UserSet.None),
                                                              this);
            m_threatLevelVeryLow = new ThreatLevelDefinition(ThreatLevel.VeryLow,
                                                             UserSetHelpers.ParseUserSetConfigSetting(config,
                                                                                                      "VeryLowUserSet",
                                                                                                      UserSet.None),
                                                             this);
            m_threatLevelLow = new ThreatLevelDefinition(ThreatLevel.Low,
                                                         UserSetHelpers.ParseUserSetConfigSetting(config, "LowUserSet",
                                                                                                  UserSet.None), this);
            m_threatLevelModerate = new ThreatLevelDefinition(ThreatLevel.Moderate,
                                                              UserSetHelpers.ParseUserSetConfigSetting(config,
                                                                                                       "ModerateUserSet",
                                                                                                       UserSet.None),
                                                              this);
            m_threatLevelHigh = new ThreatLevelDefinition(ThreatLevel.High,
                                                          UserSetHelpers.ParseUserSetConfigSetting(config, "HighUserSet",
                                                                                                   UserSet.None), this);
            m_threatLevelVeryHigh = new ThreatLevelDefinition(ThreatLevel.VeryHigh,
                                                              UserSetHelpers.ParseUserSetConfigSetting(config,
                                                                                                       "VeryHighUserSet",
                                                                                                       UserSet.None),
                                                              this);
            m_threatLevelSevere = new ThreatLevelDefinition(ThreatLevel.Severe,
                                                            UserSetHelpers.ParseUserSetConfigSetting(config,
                                                                                                     "SevereUserSet",
                                                                                                     UserSet.None), this);
            m_threatLevelNoAccess = new ThreatLevelDefinition(ThreatLevel.NoAccess,
                                                            UserSetHelpers.ParseUserSetConfigSetting(config,
                                                                                                     "NoAccessUserSet",
                                                                                                     UserSet.None), this);
        }

        #endregion

        #region ThreatLevels

        public ThreatLevelDefinition GetThreatLevel()
        {
            if (m_MaxThreatLevel != 0)
                return GetDefinition(m_MaxThreatLevel);
            string risk = m_config.GetString("FunctionThreatLevel", "VeryLow");
            switch (risk)
            {
                case "NoAccess":
                    m_MaxThreatLevel = ThreatLevel.NoAccess;
                    break;
                case "None":
                    m_MaxThreatLevel = ThreatLevel.None;
                    break;
                case "VeryLow":
                    m_MaxThreatLevel = ThreatLevel.VeryLow;
                    break;
                case "Low":
                    m_MaxThreatLevel = ThreatLevel.Low;
                    break;
                case "Moderate":
                    m_MaxThreatLevel = ThreatLevel.Moderate;
                    break;
                case "High":
                    m_MaxThreatLevel = ThreatLevel.High;
                    break;
                case "VeryHigh":
                    m_MaxThreatLevel = ThreatLevel.VeryHigh;
                    break;
                case "Severe":
                    m_MaxThreatLevel = ThreatLevel.Severe;
                    break;
                default:
                    break;
            }
            return GetDefinition(m_MaxThreatLevel);
        }

        public bool CheckAPI(string Name)
        {
            if (!EnabledAPIs.Contains(Name.ToLower()))
                return false;
            return true;
        }

        public bool CheckThreatLevel(ThreatLevel level, string function, ISceneChildEntity m_host, string API,
                                     UUID itemID)
        {
            GetDefinition(level).CheckThreatLevel(function, m_host, API);
            return CheckFunctionLimits(function, m_host, API, itemID);
        }

        public ThreatLevelDefinition GetDefinition(ThreatLevel level)
        {
            switch (level)
            {
                case ThreatLevel.None:
                    return m_threatLevelNone;
                case ThreatLevel.Nuisance:
                    return m_threatLevelNuisance;
                case ThreatLevel.VeryLow:
                    return m_threatLevelVeryLow;
                case ThreatLevel.Low:
                    return m_threatLevelLow;
                case ThreatLevel.Moderate:
                    return m_threatLevelModerate;
                case ThreatLevel.High:
                    return m_threatLevelHigh;
                case ThreatLevel.VeryHigh:
                    return m_threatLevelVeryHigh;
                case ThreatLevel.Severe:
                    return m_threatLevelSevere;
                case ThreatLevel.NoAccess:
                    return m_threatLevelNoAccess;
            }
            return null;
        }

        internal void Error(string surMessage, string msg)
        {
            throw new Exception(surMessage + msg);
        }

        #region Limitation Functions

        private bool CheckFunctionLimits(string function, ISceneChildEntity m_host, string API, UUID itemID)
        {
            LimitDef d = null;
            bool isAPI = m_functionsToLimit.TryGetValue(API, out d);
            bool isFunction = m_functionsToLimit.TryGetValue(function, out d); //Function overrides API
            if (m_allowFunctionLimiting && (isAPI || isFunction))
            {
                //Get the list for the given type
                Dictionary<string, LimitReq> functions;
                bool doInsert = false;
                if (d.Type == LimitType.Owner)
                {
                    if (!m_functionLimiting.TryGetValue(m_host.OwnerID, out functions))
                    {
                        doInsert = true;
                        functions = new Dictionary<string, LimitReq>();
                    }
                }
                else if (d.Type == LimitType.Script)
                {
                    if (!m_functionLimiting.TryGetValue(itemID, out functions))
                    {
                        doInsert = true;
                        functions = new Dictionary<string, LimitReq>();
                    }
                }
                else if (d.Type == LimitType.Group)
                {
                    if (!m_functionLimiting.TryGetValue(m_host.ParentEntity.UUID, out functions))
                    {
                        doInsert = true;
                        functions = new Dictionary<string, LimitReq>();
                    }
                }
                else if (d.Type == LimitType.Prim)
                {
                    if (!m_functionLimiting.TryGetValue(m_host.UUID, out functions))
                    {
                        doInsert = true;
                        functions = new Dictionary<string, LimitReq>();
                    }
                }
                else
                    return true;

                LimitReq r;
                if (!functions.TryGetValue(function, out r))
                    r = new LimitReq();

                if (d.MaxNumberOfTimes != 0)
                {
                    if (r.NumberOfTimesFired + 1 > d.MaxNumberOfTimes) //Too Many, kill it
                    {
                        TriggerAlert(function, d,
                                     "You have exceeded the number of times this function (" + function +
                                     ") is allowed to be fired",
                                     m_host);
                        return TriggerAction(d, m_host, itemID);
                    }
                    r.NumberOfTimesFired++;
                }
                if (d.TimeScale != 0)
                {
                    if (r.LastFired != 0 && Util.EnvironmentTickCountSubtract(r.LastFired) < d.TimeScale)
                    {
                        if (r.FunctionsSinceLastFired + 1 > d.FunctionsOverTimeScale)
                        {
                            TriggerAlert(function, d,
                                         "You have fired the given function " + function + " too quickly.", m_host);
                            return TriggerAction(d, m_host, itemID);
                        }
                    }
                    else
                    {
                        r.LastFired = Util.EnvironmentTickCount();
                        r.FunctionsSinceLastFired = 0; //Clear it out again
                    }
                    r.FunctionsSinceLastFired++;
                }
                //Put it back where it came from
                functions[isFunction ? function : API] = r;
                if (doInsert)
                    if (d.Type == LimitType.Owner)
                        m_functionLimiting[m_host.OwnerID] = functions;
                    else if (d.Type == LimitType.Script)
                        m_functionLimiting[itemID] = functions;
                    else if (d.Type == LimitType.Group)
                        m_functionLimiting[m_host.ParentEntity.UUID] = functions;
                    else if (d.Type == LimitType.Prim)
                        m_functionLimiting[m_host.UUID] = functions;
            }
            return true;
        }

        private void TriggerAlert(string function, LimitDef d, string message, ISceneChildEntity host)
        {
            if (d.Alert == LimitAlert.Console || d.Alert == LimitAlert.ConsoleAndInworld)
                MainConsole.Instance.Warn("[Limitor]: " + message);
            if (d.Alert == LimitAlert.Inworld || d.Alert == LimitAlert.ConsoleAndInworld)
            {
                IChatModule chatModule = host.ParentEntity.Scene.RequestModuleInterface<IChatModule>();
                if (chatModule != null)
                    chatModule.SimChat("[Limitor]: " + message, ChatTypeEnum.DebugChannel,
                                       2147483647, host.AbsolutePosition, host.Name, host.UUID, false,
                                       host.ParentEntity.Scene);
            }
        }

        /// <summary>
        ///   Fires the action associated with the limitation
        /// </summary>
        /// <param name = "d"></param>
        /// <param name = "m_host"></param>
        /// <param name = "itemID"></param>
        /// <returns>Whether the event should be dropped</returns>
        private bool TriggerAction(LimitDef d, ISceneChildEntity m_host, UUID itemID)
        {
            if (d.Action == LimitAction.None)
                return true;
            else if (d.Action == LimitAction.Drop)
                return false; //Drop it
            else if (d.Action == LimitAction.TerminateEvent)
                throw new Exception(""); //Blank messages kill events, but don't show anything on the console/inworld
            else if (d.Action == LimitAction.TerminateScript)
            {
                ScriptData script = GetScript(itemID);
                script.IgnoreNew = true; //Blocks all new events, can be reversed by resetting or resaving the script
                throw new Exception(""); //Blank messages kill events, but don't show anything on the console/inworld
            }
            else if (d.Action == LimitAction.Delay)
                MainConsole.Instance.Warn("Function delaying is not implemented");
            return true;
        }

        #endregion

        public class ThreatLevelDefinition
        {
            /// <summary>
            ///   Which owners have access to which functions
            /// </summary>
            private readonly Dictionary<string, List<UUID>> m_FunctionPerms = new Dictionary<string, List<UUID>>();

            private readonly bool m_allowGroupPermissions;

            private readonly List<UUID> m_allowedUsers = new List<UUID>();

            private readonly Dictionary<UUID, Dictionary<string, bool>> m_knownAllowedGroupFunctionsForAvatars =
                new Dictionary<UUID, Dictionary<string, bool>>();

            private readonly ScriptProtectionModule m_scriptProtectionModule;
            private readonly ThreatLevel m_threatLevel = ThreatLevel.None;
            private readonly UserSet m_userSet = UserSet.None;

            public ThreatLevelDefinition(ThreatLevel threatLevel, UserSet userSet, ScriptProtectionModule module)
            {
                m_threatLevel = threatLevel;
                m_userSet = userSet;
                m_scriptProtectionModule = module;
                m_allowGroupPermissions = m_scriptProtectionModule.m_config.GetBoolean(
                    "AllowGroupThreatPermissionCheck", m_allowGroupPermissions);

                string perm = m_scriptProtectionModule.m_config.GetString("Allow_" + m_threatLevel.ToString(), "");
                if (perm != "")
                {
                    string[] ids = perm.Split(',');
#if (!ISWIN)
                    foreach (string id in ids)
                    {
                        string current = id.Trim();
                        UUID uuid;

                        if (UUID.TryParse(current, out uuid))
                        {
                            if (uuid != UUID.Zero)
                                m_allowedUsers.Add(uuid);
                        }
                    }
#else
                    foreach (string current in ids.Select(id => id.Trim()))
                    {
                        UUID uuid;

                        if (UUID.TryParse(current, out uuid))
                        {
                            if (uuid != UUID.Zero)
                                m_allowedUsers.Add(uuid);
                        }
                    }
#endif
                }
                perm = m_scriptProtectionModule.m_config.GetString("Allow_All", "");
                if (perm != "")
                {
                    string[] ids = perm.Split(',');
#if (!ISWIN)
                    foreach (string id in ids)
                    {
                        string current = id.Trim();
                        UUID uuid;

                        if (UUID.TryParse(current, out uuid))
                        {
                            if (uuid != UUID.Zero)
                                m_allowedUsers.Add(uuid);
                        }
                    }
#else
                    foreach (string current in ids.Select(id => id.Trim()))
                    {
                        UUID uuid;

                        if (UUID.TryParse(current, out uuid))
                        {
                            if (uuid != UUID.Zero)
                                m_allowedUsers.Add(uuid);
                        }
                    }
#endif
                }
            }

            public void CheckThreatLevel(string function, ISceneChildEntity m_host, string API)
            {
                if (CheckUser(m_host))
                    return;
                List<UUID> FunctionPerms = new List<UUID>();
                if (!m_FunctionPerms.TryGetValue(function, out FunctionPerms))
                {
                    string perm = m_scriptProtectionModule.m_config.GetString("Allow_" + function, "");
                    if (perm == "")
                    {
                        FunctionPerms = null; // a null value is default, which means check against the max threat level
                    }
                    else
                    {
                        bool allowed;

                        if (bool.TryParse(perm, out allowed))
                        {
                            // Boolean given
                            FunctionPerms = allowed ? new List<UUID> { UUID.Zero } : new List<UUID>();
                        }
                        else
                        {
                            FunctionPerms = new List<UUID>();

                            string[] ids = perm.Split(new[] { ',' });
#if (!ISWIN)
                            foreach (string id in ids)
                            {
                                string current = id.Trim();
                                UUID uuid;

                                if (UUID.TryParse(current, out uuid))
                                {
                                    if (uuid != UUID.Zero)
                                        FunctionPerms.Add(uuid);
                                }
                            }
#else
                            foreach (string current in ids.Select(id => id.Trim()))
                            {
                                UUID uuid;

                                if (UUID.TryParse(current, out uuid))
                                {
                                    if (uuid != UUID.Zero)
                                        FunctionPerms.Add(uuid);
                                }
                            }
#endif
                        }
                        m_FunctionPerms[function] = FunctionPerms;
                    }
                }

                // If the list is null, then the value was true / undefined
                // Threat level governs permissions in this case
                //
                // If the list is non-null, then it is a list of UUIDs allowed
                // to use that particular function. False causes an empty
                // list and therefore means "no one"
                //
                // To allow use by anyone, the list contains UUID.Zero
                //
                if (FunctionPerms == null) // No list = true
                {
                    if (m_threatLevel > m_scriptProtectionModule.GetThreatLevel().m_threatLevel)
                        m_scriptProtectionModule.Error("Runtime Error: ",
                                                       String.Format(
                                                           "{0} permission denied.  Allowed threat level is {1} but function threat level is {2}.",
                                                           function,
                                                           m_scriptProtectionModule.GetThreatLevel().m_threatLevel,
                                                           m_threatLevel));
                }
                else
                {
                    if (!FunctionPerms.Contains(UUID.Zero))
                    {
                        if (!FunctionPerms.Contains(m_host.OwnerID))
                        {
                            if (m_allowGroupPermissions)
                            {
                                Dictionary<string, bool> cachedFunctions;
                                //Check to see whether we have already evaluated this function for this user
                                if (m_knownAllowedGroupFunctionsForAvatars.TryGetValue(m_host.OwnerID,
                                                                                       out cachedFunctions))
                                {
                                    if (cachedFunctions.ContainsKey(function))
                                    {
                                        if (cachedFunctions[function])
                                            return;
                                        else
                                            m_scriptProtectionModule.Error("Runtime Error: ",
                                                                           String.Format(
                                                                               "{0} permission denied.  Prim owner is not in the list of users allowed to execute this function.",
                                                                               function));
                                    }
                                }
                                else
                                    cachedFunctions = new Dictionary<string, bool>();
                                IGroupsModule groupsModule =
                                    m_host.ParentEntity.Scene.RequestModuleInterface<IGroupsModule>();
                                if (groupsModule != null)
                                {
                                    bool success = false;
                                    foreach (UUID id in FunctionPerms)
                                    {
                                        if (groupsModule.GroupPermissionCheck(m_host.OwnerID, id, GroupPowers.None))
                                        {
                                            success = true;
                                            break;
                                        }
                                    }
                                    //Cache the success
                                    cachedFunctions[function] = success;
                                    if (!m_knownAllowedGroupFunctionsForAvatars.ContainsKey(m_host.OwnerID))
                                        m_knownAllowedGroupFunctionsForAvatars.Add(m_host.OwnerID,
                                                                                   new Dictionary<string, bool>());
                                    m_knownAllowedGroupFunctionsForAvatars[m_host.OwnerID] = cachedFunctions;

                                    if (success)
                                        return; //All is good
                                }
                            }
                            m_scriptProtectionModule.Error("Runtime Error: ",
                                                           String.Format(
                                                               "{0} permission denied.  Prim owner is not in the list of users allowed to execute this function.",
                                                               function));
                        }
                    }
                }
            }

            private bool CheckUser(ISceneChildEntity host)
            {
                if (m_allowedUsers.Contains(host.OwnerID))
                    return true;

                if (m_userSet == UserSet.ParcelOwners)
                {
                    IScenePresence av = host.ParentEntity.Scene.GetScenePresence(host.OwnerID);
                    ILandObject lo = null;
                    if (av != null)
                        lo =
                            host.ParentEntity.Scene.RequestModuleInterface<IParcelManagementModule>().GetLandObject(
                                av.AbsolutePosition.X, av.AbsolutePosition.Y);
                    if (host.ParentEntity.Scene.Permissions.GenericParcelPermission(host.OwnerID, lo, 0))
                        return true;
                }
                else if ((m_userSet == UserSet.Administrators &&
                          host.ParentEntity.Scene.Permissions.IsGod(host.OwnerID)))
                {
                    m_allowedUsers.Add(host.OwnerID); //We don't need to lock as it blocks up above,
                    //and we don't need to Contains() either as we already let all users in above
                    return true;
                }
                return false;
            }

            public override string ToString()
            {
                return string.Format("ThreatLevel: {0}, UserSet : {1}", m_threatLevel.ToString(), m_userSet.ToString());
            }
        }

        #endregion

        #region Previously Compiled Scripts

        /// <summary>
        ///   Reset all lists (if hard), if not hard, just reset previously compiled
        /// </summary>
        /// <param name = "hard"></param>
        public void Reset(bool hard)
        {
            lock (PreviouslyCompiled)
            {
                PreviouslyCompiled.Clear();
            }
            if (hard)
            {
                lock (ScriptsItems)
                {
                    ScriptsItems.Clear();
                }
                lock (Scripts)
                {
                    Scripts.Clear();
                }
            }
        }

        public void AddPreviouslyCompiled(string source, ScriptData ID)
        {
            //string key = source.Length.ToString() + source.GetHashCode().ToString();
            string key = Util.Md5Hash(source);
            lock (PreviouslyCompiled)
            {
                if (!PreviouslyCompiled.ContainsKey(key))
                {
                    //PreviouslyCompiled.Add (source, ID.AssemblyName);
                    PreviouslyCompiled.Add(key, ID.AssemblyName);
                }
            }
        }

        public void RemovePreviouslyCompiled(string source)
        {
            if (string.IsNullOrEmpty(source))
                return;
            //string key = source.Length.ToString() + source.GetHashCode().ToString();
            string key = Util.Md5Hash(source);
            lock (PreviouslyCompiled)
            {
                if (PreviouslyCompiled.ContainsKey(key))
                {
                    PreviouslyCompiled.Remove(key);
                    //PreviouslyCompiled.Remove (source);
                }
            }
        }

        public string TryGetPreviouslyCompiledScript(string source)
        {
            //string key = source.Length.ToString() + source.GetHashCode().ToString();
            string key = Util.Md5Hash(source);
            string assemblyName = "";
            PreviouslyCompiled.TryGetValue(key, out assemblyName);
            //PreviouslyCompiled.TryGetValue (source, out assemblyName);

            return assemblyName;
        }

        public ScriptData GetScript(UUID primID, UUID itemID)
        {
            Dictionary<UUID, ScriptData> Instances;
            lock (Scripts)
            {
                if (Scripts.TryGetValue(primID, out Instances))
                {
                    ScriptData ID = null;
                    Instances.TryGetValue(itemID, out ID);
                    return ID;
                }
            }
            return null;
        }

        public ScriptData GetScript(UUID itemID)
        {
            lock (ScriptsItems)
            {
                UUID primID;
                if (ScriptsItems.TryGetValue(itemID, out primID))
                    return GetScript(primID, itemID);
                return null;
            }
        }

        public ScriptData[] GetScripts(UUID primID)
        {
            Dictionary<UUID, ScriptData> Instances;
            lock (Scripts)
            {
                if (Scripts.TryGetValue(primID, out Instances))
                    return new List<ScriptData>(Instances.Values).ToArray();
            }
            return null;
        }

        public void AddNewScript(ScriptData ID)
        {
            lock (ScriptsItems)
            {
                if (ID.Part != null)
                    ScriptsItems[ID.ItemID] = ID.Part.UUID;
            }
            lock (Scripts)
            {
                Dictionary<UUID, ScriptData> Instances = new Dictionary<UUID, ScriptData>();
                if (!Scripts.TryGetValue(ID.Part.UUID, out Instances))
                    Instances = new Dictionary<UUID, ScriptData>();

                Instances[ID.ItemID] = ID;
                Scripts[ID.Part.UUID] = Instances;
            }
        }

        public ScriptData[] GetAllScripts()
        {
            List<ScriptData> Ids = new List<ScriptData>();
            lock (Scripts)
            {
#if(!ISWIN)
                foreach (Dictionary<UUID, ScriptData> Instances in Scripts.Values)
                {
                    foreach (ScriptData ID in Instances.Values)
                    {
                        Ids.Add(ID);
                    }
                }
#else
                Ids.AddRange(Scripts.Values.SelectMany(Instances => Instances.Values));
#endif
            }
            return Ids.ToArray();
        }

        public void RemoveScript(ScriptData Data)
        {
            lock (ScriptsItems)
            {
                ScriptsItems.Remove(Data.ItemID);
            }
            lock (Scripts)
            {
                Dictionary<UUID, ScriptData> Instances = new Dictionary<UUID, ScriptData>();
                if (Scripts.TryGetValue(Data.Part.UUID, out Instances))
                {
                    Instances.Remove(Data.ItemID);
                    if (Instances.Count > 0)
                        Scripts[Data.Part.UUID] = Instances;
                    else
                        Scripts.Remove(Data.Part.UUID);
                }
            }
        }

        #endregion
    }
}