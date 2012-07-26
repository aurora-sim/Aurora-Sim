/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Permissions
{
    public class PermissionsModule : INonSharedRegionModule, IPermissionsModule
    {
        private IConfig PermissionsConfig;
        protected IScene m_scene;

        #region Constants

        // These are here for testing.  They will be taken out

        //private uint PERM_ALL = (uint)2147483647;
        private uint PERM_COPY = 32768;
        private uint PERM_LOCKED = 540672;
        //private uint PERM_MODIFY = (uint)16384;
        private uint PERM_MOVE = 524288;
        private uint PERM_TRANS = 8192;

        #endregion

        #region Bypass Permissions / Debug Permissions Stuff

        // Bypasses the permissions engine
        private readonly Dictionary<string, bool> GrantAScript = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> GrantCS = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> GrantJS = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> GrantLSL = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> GrantVB = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> GrantYP = new Dictionary<string, bool>();
        private readonly List<UUID> m_allowedAdministrators = new List<UUID>();
        private bool m_ParcelOwnerIsGod;
        private bool m_RegionManagerIsGod;
        private bool m_RegionOwnerIsGod = true;
        private bool m_adminHasToBeInGodMode = true;
        private bool m_allowAdminFriendEditRights = true;
        private bool m_allowGridGods = true;
        private UserSet m_allowedAScriptScriptCompilers = UserSet.All;
        private UserSet m_allowedCSScriptCompilers = UserSet.All;
        private UserSet m_allowedJSScriptCompilers = UserSet.All;

        /// <value>
        ///   The set of users that are allowed to compile certain scripts.  This is only active if 
        ///   permissions are not being bypassed.  This overrides normal permissions.
        /// </value>
        private UserSet m_allowedLSLScriptCompilers = UserSet.All;

        /// <value>
        ///   The set of users that are allowed to create scripts.  This is only active if permissions are not being
        ///   bypassed.  This overrides normal permissions.
        /// </value>
        private UserSet m_allowedScriptCreators = UserSet.All;

        /// <value>
        ///   The set of users that are allowed to edit (save) scripts.  This is only active if 
        ///   permissions are not being bypassed.  This overrides normal permissions.-
        /// </value>
        private UserSet m_allowedScriptEditors = UserSet.All;


        private UserSet m_allowedVBScriptCompilers = UserSet.All;
        private UserSet m_allowedYPScriptCompilers = UserSet.All;
        private bool m_bypassPermissions;
        private bool m_bypassPermissionsValue = true;
        private bool m_debugPermissions;
        private IGroupsModule m_groupsModule;
        private IMoapModule m_moapModule;
        private IParcelManagementModule m_parcelManagement;
        private bool m_propagatePermissions = true;

        #endregion

        public bool IsSharedModule
        {
            get { return false; }
        }

        #region Helper Functions

        protected void SendPermissionError(UUID user, string reason)
        {
            m_scene.EventManager.TriggerPermissionError(user, reason);
        }

        protected void DebugPermissionInformation(string permissionCalled)
        {
            if (m_debugPermissions)
                MainConsole.Instance.Debug("[PERMISSIONS]: " + permissionCalled + " was called from " + m_scene.RegionInfo.RegionName);
        }

        public bool IsInGroup(UUID user, UUID groupID)
        {
            return IsGroupMember(groupID, user, 0);
        }

        // Checks if the given group is active and if the user is a group member
        // with the powers requested (powers = 0 for no powers check)
        protected bool IsGroupMember(UUID groupID, UUID userID, ulong powers)
        {
            if (null == m_groupsModule)
                return false;

            return m_groupsModule.GroupPermissionCheck(userID, groupID, (GroupPowers) powers);
        }

        /// <summary>
        ///   Is the given user an administrator (in other words, a god)?
        /// </summary>
        /// <param name = "user"></param>
        /// <returns></returns>
        protected bool IsAdministrator(UUID user)
        {
            return InternalIsAdministrator(user, false);
        }

        /// <summary>
        ///   Is the given user an administrator (in other words, a god)?
        /// </summary>
        /// <param name = "user"></param>
        /// <returns></returns>
        private bool InternalIsAdministrator(UUID user, bool checkGodStatus)
        {
            if (user == UUID.Zero) return false;

            if (m_allowedAdministrators.Contains(user))
                return !checkGodStatus || CheckIsInGodMode(user);

            if (m_RegionOwnerIsGod && m_scene.RegionInfo.EstateSettings.EstateOwner == user)
                return !checkGodStatus || CheckIsInGodMode(user);

            if (m_RegionManagerIsGod && IsEstateManager(user))
                return !checkGodStatus || CheckIsInGodMode(user);

            IScenePresence sp = m_scene.GetScenePresence(user);
            if (m_ParcelOwnerIsGod && m_parcelManagement != null && sp != null)
            {
                ILandObject landObject = m_parcelManagement.GetLandObject(sp.AbsolutePosition.X, sp.AbsolutePosition.Y);
                if (landObject != null && landObject.LandData.OwnerID == user)
                    return !checkGodStatus || CheckIsInGodMode(user);
            }

            if (m_allowGridGods)
            {
                if (sp != null)
                {
                    if (sp.UserLevel > 0)
                        return !checkGodStatus || CheckIsInGodMode(user);
                }
                UserAccount account = m_scene.UserAccountService.GetUserAccount(null, user);
                if (account != null)
                {
                    if (account.UserLevel > 0)
                        return !checkGodStatus || CheckIsInGodMode(user);
                }
            }

            return false;
        }

        protected bool CheckIsInGodMode(UUID userID)
        {
            if (m_adminHasToBeInGodMode)
            {
                IScenePresence sp = m_scene.GetScenePresence(userID);
                if (sp != null && sp.GodLevel == 0) //Allow null presences to be god always, as they are just gods
                    return false;
                else if (sp != null)
                    return true; //Only allow logged in users to have god mode
            }
            return false;
        }

        protected bool IsFriendWithPerms(UUID user, UUID objectOwner)
        {
            if (user == UUID.Zero)
                return false;

            if (user == objectOwner)
                return true; //Same person, implicit trust

            int friendPerms = m_scene.RequestModuleInterface<IFriendsModule>().GetFriendPerms(user, objectOwner);

            if (friendPerms == -1) //Not a friend
                return false;

            if ((friendPerms & (int) FriendRights.CanModifyObjects) != 0)
                return true;

            return false;
        }

        protected bool IsEstateManager(UUID user)
        {
            if (user == UUID.Zero) return false;

            return m_scene.RegionInfo.EstateSettings.IsEstateManager(user);
        }

        #endregion

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
            PermissionsConfig = config.Configs["Permissions"];

            m_allowGridGods = PermissionsConfig.GetBoolean("allow_grid_gods", m_allowGridGods);
            m_bypassPermissions = !PermissionsConfig.GetBoolean("serverside_object_permissions", m_bypassPermissions);
            m_propagatePermissions = PermissionsConfig.GetBoolean("propagate_permissions", m_propagatePermissions);
            m_RegionOwnerIsGod = PermissionsConfig.GetBoolean("region_owner_is_god", m_RegionOwnerIsGod);
            m_RegionManagerIsGod = PermissionsConfig.GetBoolean("region_manager_is_god", m_RegionManagerIsGod);
            m_ParcelOwnerIsGod = PermissionsConfig.GetBoolean("parcel_owner_is_god", m_ParcelOwnerIsGod);
            m_allowAdminFriendEditRights = PermissionsConfig.GetBoolean("allow_god_friends_edit_with_rights",
                                                                        m_allowAdminFriendEditRights);
            m_adminHasToBeInGodMode = PermissionsConfig.GetBoolean("admin_has_to_be_in_god_mode",
                                                                   m_adminHasToBeInGodMode);

            m_allowedScriptCreators
                = UserSetHelpers.ParseUserSetConfigSetting(PermissionsConfig, "allowed_script_creators",
                                                           m_allowedScriptCreators);
            m_allowedScriptEditors
                = UserSetHelpers.ParseUserSetConfigSetting(PermissionsConfig, "allowed_script_editors",
                                                           m_allowedScriptEditors);

            m_allowedLSLScriptCompilers
                = UserSetHelpers.ParseUserSetConfigSetting(PermissionsConfig, "allowed_lsl_script_compilers",
                                                           m_allowedLSLScriptCompilers);
            m_allowedCSScriptCompilers
                = UserSetHelpers.ParseUserSetConfigSetting(PermissionsConfig, "allowed_cs_script_compilers",
                                                           m_allowedCSScriptCompilers);
            m_allowedJSScriptCompilers
                = UserSetHelpers.ParseUserSetConfigSetting(PermissionsConfig, "allowed_js_script_compilers",
                                                           m_allowedJSScriptCompilers);
            m_allowedVBScriptCompilers
                = UserSetHelpers.ParseUserSetConfigSetting(PermissionsConfig, "allowed_vb_script_compilers",
                                                           m_allowedVBScriptCompilers);
            m_allowedYPScriptCompilers
                = UserSetHelpers.ParseUserSetConfigSetting(PermissionsConfig, "allowed_yp_script_compilers",
                                                           m_allowedYPScriptCompilers);
            m_allowedAScriptScriptCompilers
                = UserSetHelpers.ParseUserSetConfigSetting(PermissionsConfig, "allowed_ascript_script_compilers",
                                                           m_allowedAScriptScriptCompilers);

            string perm = PermissionsConfig.GetString("Allowed_Administrators", "");
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
                            m_allowedAdministrators.Add(uuid);
                    }
                }
#else
                foreach (string current in ids.Select(id => id.Trim()))
                {
                    UUID uuid;

                    if (UUID.TryParse(current, out uuid))
                    {
                        if (uuid != UUID.Zero)
                            m_allowedAdministrators.Add(uuid);
                    }
                }
#endif
            }


            string permissionModules = PermissionsConfig.GetString("Modules", "DefaultPermissionsModule");

            List<string> modules = new List<string>(permissionModules.Split(','));

            if (!modules.Contains("DefaultPermissionsModule"))
                return;
        }

        public void AddRegion(IScene scene)
        {
            m_scene = scene;

            m_scene.RegisterModuleInterface<IPermissionsModule>(this);

            //Register functions with Scene External Checks!
            m_scene.Permissions.OnBypassPermissions += BypassPermissions;
            m_scene.Permissions.OnSetBypassPermissions += SetBypassPermissions;
            m_scene.Permissions.OnPropagatePermissions += PropagatePermissions;
            m_scene.Permissions.OnGenerateClientFlags += GenerateClientFlags;
            m_scene.Permissions.OnAbandonParcel += CanAbandonParcel;
            m_scene.Permissions.OnReclaimParcel += CanReclaimParcel;
            m_scene.Permissions.OnDeedParcel += CanDeedParcel;
            m_scene.Permissions.OnDeedObject += CanDeedObject;
            m_scene.Permissions.OnIsGod += IsGod;
            m_scene.Permissions.OnIsInGroup += IsInGroup;
            m_scene.Permissions.OnIsAdministrator += IsAdministrator;
            m_scene.Permissions.OnDuplicateObject += CanDuplicateObject;
            m_scene.Permissions.OnDeleteObject += CanDeleteObject;
            m_scene.Permissions.OnEditObject += CanEditObject; //MAYBE FULLY IMPLEMENTED
            m_scene.Permissions.OnEditParcel += CanEditParcel;
            m_scene.Permissions.OnSubdivideParcel += CanSubdivideParcel;
            m_scene.Permissions.OnEditParcelProperties += CanEditParcelProperties; //MAYBE FULLY IMPLEMENTED
            m_scene.Permissions.OnInstantMessage += CanInstantMessage;
            m_scene.Permissions.OnCanGodTp += CanGodTp;
            m_scene.Permissions.OnInventoryTransfer += CanInventoryTransfer; //NOT YET IMPLEMENTED
            m_scene.Permissions.OnIssueEstateCommand += CanIssueEstateCommand;
            m_scene.Permissions.OnMoveObject += CanMoveObject;
            m_scene.Permissions.OnObjectEntry += CanObjectEntry;
            m_scene.Permissions.OnReturnObjects += CanReturnObjects;
            m_scene.Permissions.OnRezObject += CanRezObject;
            m_scene.Permissions.OnRunConsoleCommand += CanRunConsoleCommand;
            m_scene.Permissions.OnRunScript += CanRunScript;
            m_scene.Permissions.OnCompileScript += CanCompileScript;
            m_scene.Permissions.OnSellParcel += CanSellParcel;
            m_scene.Permissions.OnTakeObject += CanTakeObject;
            m_scene.Permissions.OnTakeCopyObject += CanTakeCopyObject;
            m_scene.Permissions.OnTerraformLand += CanTerraformLand;
            m_scene.Permissions.OnLinkObject += CanLinkObject; //MAYBE FULLY IMPLEMENTED
            m_scene.Permissions.OnDelinkObject += CanDelinkObject; //MAYBE FULLY IMPLEMENTED
            m_scene.Permissions.OnBuyLand += CanBuyLand; //NOT YET IMPLEMENTED

            m_scene.Permissions.OnViewNotecard += CanViewNotecard;
            m_scene.Permissions.OnViewScript += CanViewScript;
            m_scene.Permissions.OnEditNotecard += CanEditNotecard;
            m_scene.Permissions.OnEditScript += CanEditScript;

            m_scene.Permissions.OnCreateObjectInventory += CanCreateObjectInventory;
            m_scene.Permissions.OnEditObjectInventory += CanEditObjectInventory;
            m_scene.Permissions.OnCopyObjectInventory += CanCopyObjectInventory;
            m_scene.Permissions.OnDeleteObjectInventory += CanDeleteObjectInventory;
            m_scene.Permissions.OnResetScript += CanResetScript;

            m_scene.Permissions.OnCreateUserInventory += CanCreateUserInventory;
            m_scene.Permissions.OnCopyUserInventory += CanCopyUserInventory; //NOT YET IMPLEMENTED
            m_scene.Permissions.OnEditUserInventory += CanEditUserInventory; //NOT YET IMPLEMENTED
            m_scene.Permissions.OnDeleteUserInventory += CanDeleteUserInventory; //NOT YET IMPLEMENTED

            m_scene.Permissions.OnControlPrimMedia += CanControlPrimMedia;
            m_scene.Permissions.OnInteractWithPrimMedia += CanInteractWithPrimMedia;

            m_scene.Permissions.OnPushObject += CanPushObject;
            m_scene.Permissions.OnViewObjectOwners += CanViewObjectOwners;
            m_scene.Permissions.OnEditParcelAccessList += CanEditParcelAccessList;
            m_scene.Permissions.OnGenericParcelHandler += GenericParcelPermission;
            m_scene.Permissions.OnTakeLandmark += TakeLandmark;
            m_scene.Permissions.OnSetHomePoint += SetHomePoint;

            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand(
                    "bypass permissions",
                    "bypass permissions <true / false>",
                    "Bypass permission checks",
                    HandleBypassPermissions);

                MainConsole.Instance.Commands.AddCommand(
                    "force permissions",
                    "force permissions <true / false>",
                    "Force permissions on or off",
                    HandleForcePermissions);

                MainConsole.Instance.Commands.AddCommand(
                    "debug permissions",
                    "debug permissions <true / false>",
                    "Enable permissions debugging",
                    HandleDebugPermissions);
            }


            string grant = PermissionsConfig.GetString("GrantLSL", "");
            if (grant.Length > 0)
            {
#if (!ISWIN)
                foreach (string uuidl in grant.Split(','))
                {
                    string uuid = uuidl.Trim(" \t".ToCharArray());
                    GrantLSL.Add(uuid, true);
                }
#else
                foreach (string uuid in grant.Split(',').Select(uuidl => uuidl.Trim(" \t".ToCharArray())))
                {
                    GrantLSL.Add(uuid, true);
                }
#endif
            }

            grant = PermissionsConfig.GetString("GrantCS", "");
            if (grant.Length > 0)
            {
#if (!ISWIN)
                foreach (string uuidl in grant.Split(','))
                {
                    string uuid = uuidl.Trim(" \t".ToCharArray());
                    GrantCS.Add(uuid, true);
                }
#else
                foreach (string uuid in grant.Split(',').Select(uuidl => uuidl.Trim(" \t".ToCharArray())))
                {
                    GrantCS.Add(uuid, true);
                }
#endif
            }

            grant = PermissionsConfig.GetString("GrantVB", "");
            if (grant.Length > 0)
            {
#if (!ISWIN)
                foreach (string uuidl in grant.Split(','))
                {
                    string uuid = uuidl.Trim(" \t".ToCharArray());
                    GrantVB.Add(uuid, true);
                }
#else
                foreach (string uuid in grant.Split(',').Select(uuidl => uuidl.Trim(" \t".ToCharArray())))
                {
                    GrantVB.Add(uuid, true);
                }
#endif
            }

            grant = PermissionsConfig.GetString("GrantJS", "");
            if (grant.Length > 0)
            {
#if (!ISWIN)
                foreach (string uuidl in grant.Split(','))
                {
                    string uuid = uuidl.Trim(" \t".ToCharArray());
                    GrantJS.Add(uuid, true);
                }
#else
                foreach (string uuid in grant.Split(',').Select(uuidl => uuidl.Trim(" \t".ToCharArray())))
                {
                    GrantJS.Add(uuid, true);
                }
#endif
            }

            grant = PermissionsConfig.GetString("GrantYP", "");
            if (grant.Length > 0)
            {
#if (!ISWIN)
                foreach (string uuidl in grant.Split(','))
                {
                    string uuid = uuidl.Trim(" \t".ToCharArray());
                    GrantYP.Add(uuid, true);
                }
#else
                foreach (string uuid in grant.Split(',').Select(uuidl => uuidl.Trim(" \t".ToCharArray())))
                {
                    GrantYP.Add(uuid, true);
                }
#endif
            }

            grant = PermissionsConfig.GetString("GrantAScript", "");
            if (grant.Length > 0)
            {
#if (!ISWIN)
                foreach (string uuidl in grant.Split(','))
                {
                    string uuid = uuidl.Trim(" \t".ToCharArray());
                    GrantAScript.Add(uuid, true);
                }
#else
                foreach (string uuid in grant.Split(',').Select(uuidl => uuidl.Trim(" \t".ToCharArray())))
                {
                    GrantAScript.Add(uuid, true);
                }
#endif
            }
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            //if (m_friendsModule == null)
            //    MainConsole.Instance.Warn("[PERMISSIONS]: Friends module not found, friend permissions will not work");

            m_groupsModule = m_scene.RequestModuleInterface<IGroupsModule>();

            //if (m_groupsModule == null)
            //    MainConsole.Instance.Warn("[PERMISSIONS]: Groups module not found, group permissions will not work");

            m_moapModule = m_scene.RequestModuleInterface<IMoapModule>();

            // This log line will be commented out when no longer required for debugging
//            if (m_moapModule == null)
//                MainConsole.Instance.Warn("[PERMISSIONS]: Media on a prim module not found, media on a prim permissions will not work");

            m_parcelManagement = m_scene.RequestModuleInterface<IParcelManagementModule>();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "PermissionsModule"; }
        }

        #endregion

        public void HandleBypassPermissions(string[] args)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene && MainConsole.Instance.ConsoleScene != null)
                return;

            if (args.Length > 2)
            {
                bool val;

                if (!bool.TryParse(args[2], out val))
                    return;

                m_bypassPermissions = val;

                if (MainConsole.Instance.ConsoleScene != null || (MainConsole.Instance.ConsoleScene == null && !MainConsole.Instance.HasProcessedCurrentCommand))
                {
                    MainConsole.Instance.HasProcessedCurrentCommand = true;
                    MainConsole.Instance.InfoFormat(
                        "[PERMISSIONS]: Set permissions bypass to {0} for {1}",
                        m_bypassPermissions, m_scene.RegionInfo.RegionName);
                }
            }
        }

        public void HandleForcePermissions(string[] args)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene && MainConsole.Instance.ConsoleScene != null)
                return;

            if (!m_bypassPermissions)
            {
                MainConsole.Instance.Error("[PERMISSIONS] Permissions can't be forced unless they are bypassed first");
                return;
            }

            if (args.Length > 2)
            {
                bool val;

                if (!bool.TryParse(args[2], out val))
                    return;

                m_bypassPermissionsValue = val;

                if (MainConsole.Instance.ConsoleScene != null || (MainConsole.Instance.ConsoleScene == null && !MainConsole.Instance.HasProcessedCurrentCommand))
                {
                    MainConsole.Instance.HasProcessedCurrentCommand = true;
                    MainConsole.Instance.InfoFormat("[PERMISSIONS] Forced permissions to {0} in {1}", m_bypassPermissionsValue,
                                 m_scene.RegionInfo.RegionName);
                }
            }
        }

        public void HandleDebugPermissions(string[] args)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene && MainConsole.Instance.ConsoleScene != null)
                return;

            if (args.Length > 2)
            {
                bool val;

                if (!bool.TryParse(args[2], out val))
                    return;

                m_debugPermissions = val;

                if (MainConsole.Instance.ConsoleScene != null || (MainConsole.Instance.ConsoleScene == null && !MainConsole.Instance.HasProcessedCurrentCommand))
                {
                    MainConsole.Instance.HasProcessedCurrentCommand = true;
                    MainConsole.Instance.InfoFormat("[PERMISSIONS] Set permissions debugging to {0} in {1}", m_debugPermissions,
                                 m_scene.RegionInfo.RegionName);
                }
            }
        }

        public void PostInitialise()
        {
        }

        public bool PropagatePermissions()
        {
            if (m_bypassPermissions)
                return false;

            return m_propagatePermissions;
        }

        public bool BypassPermissions()
        {
            return m_bypassPermissions;
        }

        public void SetBypassPermissions(bool value)
        {
            m_bypassPermissions = value;
        }

        private bool CanLinkObject(UUID userID, UUID objectID)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericObjectPermission(userID, objectID, false);
        }

        private bool CanDelinkObject(UUID userID, UUID objectID)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericObjectPermission(userID, objectID, false);
        }

        private bool CanBuyLand(UUID userID, ILandObject parcel, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return true;
        }

        private bool CanCopyObjectInventory(UUID itemID, UUID objectID, UUID userID)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericObjectPermission(userID, objectID, true);
        }

        private bool CanDeleteObjectInventory(UUID itemID, UUID objectID, UUID userID)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericObjectPermission(userID, objectID, true);
        }

        /// <summary>
        ///   Check whether the specified user is allowed to directly create the given inventory type in a prim's
        ///   inventory (e.g. the New Script button in the 1.21 Linden Lab client).
        /// </summary>
        /// <param name = "invType"></param>
        /// <param name = "objectID"></param>
        /// <param name = "userID"></param>
        /// <returns></returns>
        private bool CanCreateObjectInventory(int invType, UUID objectID, UUID userID)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            if ((int) InventoryType.LSL == invType)
            {
                if (m_allowedScriptCreators == UserSet.Administrators && !IsAdministrator(userID))
                    return false;
                if (m_allowedScriptCreators == UserSet.ParcelOwners &&
                    !GenericParcelPermission(userID, m_scene.GetScenePresence(userID).AbsolutePosition, 0))
                    return false;
            }

            ISceneChildEntity part = m_scene.GetSceneObjectPart(objectID);
            if (part.OwnerID == userID)
                return true;

            if (IsGroupMember(part.GroupID, userID, (ulong) GroupPowers.ObjectManipulate))
                return true;

            return false;
        }

        /// <summary>
        ///   Check whether the specified user is allowed to create the given inventory type in their inventory.
        /// </summary>
        /// <param name = "invType"></param>
        /// <param name = "userID"></param>
        /// <returns></returns>
        private bool CanCreateUserInventory(int invType, UUID userID)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            if ((int) InventoryType.LSL == invType)
            {
                if (m_allowedScriptCreators == UserSet.Administrators && !IsAdministrator(userID))
                    return false;
                if (m_allowedScriptCreators == UserSet.ParcelOwners &&
                    !GenericParcelPermission(userID, m_scene.GetScenePresence(userID).AbsolutePosition, 0))
                    return false;
            }

            if ((int) InventoryType.Landmark == invType)
            {
                IScenePresence SP = m_scene.GetScenePresence(userID);
                if (m_parcelManagement == null)
                    return true;
                ILandObject parcel = m_parcelManagement.GetLandObject(SP.AbsolutePosition.X, SP.AbsolutePosition.Y);
                if ((parcel.LandData.Flags & (int) ParcelFlags.AllowLandmark) != 0)
                    return true;
                else
                    return GenericParcelPermission(userID, parcel, (uint) GroupPowers.AllowLandmark);
            }

            return true;
        }

        /// <summary>
        ///   Check whether the specified user is allowed to copy the given inventory type in their inventory.
        /// </summary>
        /// <param name = "itemID"></param>
        /// <param name = "userID"></param>
        /// <returns></returns>
        private bool CanCopyUserInventory(UUID itemID, UUID userID)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return true;
        }

        /// <summary>
        ///   Check whether the specified user is allowed to edit the given inventory item within their own inventory.
        /// </summary>
        /// <param name = "itemID"></param>
        /// <param name = "userID"></param>
        /// <returns></returns>
        private bool CanEditUserInventory(UUID itemID, UUID userID)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return true;
        }

        /// <summary>
        ///   Check whether the specified user is allowed to delete the given inventory item from their own inventory.
        /// </summary>
        /// <param name = "itemID"></param>
        /// <param name = "userID"></param>
        /// <returns></returns>
        private bool CanDeleteUserInventory(UUID itemID, UUID userID)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return true;
        }

        private bool CanResetScript(UUID prim, UUID script, UUID agentID, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            ISceneChildEntity part = m_scene.GetSceneObjectPart(prim);

            // If we selected a sub-prim to reset, prim won't represent the object, but only a part.
            // We have to check the permissions of the object, though.
            if (part.ParentID != 0) prim = part.ParentUUID;

            // You can reset the scripts in any object you can edit
            return GenericObjectPermission(agentID, prim, false);
        }

        private bool CanCompileScript(UUID ownerUUID, string scriptType, IScene scene)
        {
            //MainConsole.Instance.DebugFormat("check if {0} is allowed to compile {1}", ownerUUID, scriptType);
            switch (scriptType)
            {
                case "lsl":
                case "lsl2":
                    if ((m_allowedLSLScriptCompilers == UserSet.Administrators && !IsAdministrator(ownerUUID)) ||
                        (m_allowedLSLScriptCompilers == UserSet.ParcelOwners &&
                         !GenericParcelPermission(ownerUUID, scene.GetScenePresence(ownerUUID).AbsolutePosition, 0)) ||
                        GrantLSL.Count == 0 || GrantLSL.ContainsKey(ownerUUID.ToString()))
                    {
                        return (true);
                    }
                    break;
                case "cs":
                    if ((m_allowedCSScriptCompilers == UserSet.Administrators && !IsAdministrator(ownerUUID)) ||
                        (m_allowedCSScriptCompilers == UserSet.ParcelOwners &&
                         !GenericParcelPermission(ownerUUID, scene.GetScenePresence(ownerUUID).AbsolutePosition, 0)) ||
                        GrantCS.Count == 0 || GrantCS.ContainsKey(ownerUUID.ToString()))
                    {
                        return (true);
                    }
                    break;
                case "vb":
                    if ((m_allowedVBScriptCompilers == UserSet.Administrators && !IsAdministrator(ownerUUID)) ||
                        (m_allowedVBScriptCompilers == UserSet.ParcelOwners &&
                         !GenericParcelPermission(ownerUUID, scene.GetScenePresence(ownerUUID).AbsolutePosition, 0)) ||
                        GrantVB.Count == 0 || GrantVB.ContainsKey(ownerUUID.ToString()))
                    {
                        return (true);
                    }
                    break;
                case "js":
                    if ((m_allowedJSScriptCompilers == UserSet.Administrators && !IsAdministrator(ownerUUID)) ||
                        (m_allowedJSScriptCompilers == UserSet.ParcelOwners &&
                         !GenericParcelPermission(ownerUUID, scene.GetScenePresence(ownerUUID).AbsolutePosition, 0)) ||
                        GrantJS.Count == 0 || GrantJS.ContainsKey(ownerUUID.ToString()))
                    {
                        return (true);
                    }
                    break;
                case "yp":
                    if ((m_allowedYPScriptCompilers == UserSet.Administrators && !IsAdministrator(ownerUUID)) ||
                        (m_allowedYPScriptCompilers == UserSet.ParcelOwners &&
                         !GenericParcelPermission(ownerUUID, scene.GetScenePresence(ownerUUID).AbsolutePosition, 0)) ||
                        GrantYP.Count == 0 || GrantYP.ContainsKey(ownerUUID.ToString()))
                    {
                        return (true);
                    }
                    break;
                case "ascript":
                    if ((m_allowedAScriptScriptCompilers == UserSet.Administrators && !IsAdministrator(ownerUUID)) ||
                        (m_allowedAScriptScriptCompilers == UserSet.ParcelOwners &&
                         !GenericParcelPermission(ownerUUID, scene.GetScenePresence(ownerUUID).AbsolutePosition, 0)) ||
                        GrantAScript.Count == 0 || GrantAScript.ContainsKey(ownerUUID.ToString()))
                    {
                        return (true);
                    }
                    break;
            }
            return false;
        }

        private bool CanPushObject(UUID userID, ILandObject parcel)
        {
            //This is used to check who is pusing objects in the parcel
            //When this is called, the AllowPushObject flag has already been checked

            return GenericParcelPermission(userID, parcel, (ulong) GroupPowers.ObjectManipulate);
        }

        private bool CanViewObjectOwners(UUID userID, ILandObject parcel)
        {
            //If you have none of the three return flags, you cannot view object owners in the return menu
            if (!GenericParcelPermission(userID, parcel, (ulong) GroupPowers.ReturnNonGroup))
                if (!GenericParcelPermission(userID, parcel, (ulong) GroupPowers.ReturnGroupSet))
                    if (!GenericParcelPermission(userID, parcel, (ulong) GroupPowers.ReturnGroupOwned))
                        return false;
            return true;
        }

        private bool CanEditParcelAccessList(UUID userID, ILandObject parcel, uint flags)
        {
            AccessList AccessFlag = (AccessList) flags;
            if (AccessFlag == AccessList.Access)
                if (!GenericParcelPermission(userID, parcel, (ulong) GroupPowers.LandManageAllowed))
                    return false;
                else if (AccessFlag == AccessList.Ban)
                    if (
                        !GenericParcelPermission(userID, parcel,
                                                 (ulong) (GroupPowers.LandManageAllowed | GroupPowers.LandManageBanned)))
                        return false;
            return true;
        }

        private bool CanControlPrimMedia(UUID agentID, UUID primID, int face)
        {
//            MainConsole.Instance.DebugFormat(
//                "[PERMISSONS]: Performing CanControlPrimMedia check with agentID {0}, primID {1}, face {2}",
//                agentID, primID, face);

            if (null == m_moapModule)
                return false;

            ISceneChildEntity part = m_scene.GetSceneObjectPart(primID);
            if (null == part)
                return false;

            MediaEntry me = m_moapModule.GetMediaEntry(part, face);

            // If there is no existing media entry then it can be controlled (in this context, created).
            if (null == me)
                return true;

//            MainConsole.Instance.DebugFormat(
//                "[PERMISSIONS]: Checking CanControlPrimMedia for {0} on {1} face {2} with control permissions {3}", 
//                agentID, primID, face, me.ControlPermissions);

            return GenericObjectPermission(part.UUID, agentID, true);
        }

        private bool CanInteractWithPrimMedia(UUID agentID, UUID primID, int face)
        {
//            MainConsole.Instance.DebugFormat(
//                "[PERMISSONS]: Performing CanInteractWithPrimMedia check with agentID {0}, primID {1}, face {2}",
//                agentID, primID, face);

            if (null == m_moapModule)
                return false;

            ISceneChildEntity part = m_scene.GetSceneObjectPart(primID);
            if (null == part)
                return false;

            MediaEntry me = m_moapModule.GetMediaEntry(part, face);

            // If there is no existing media entry then it can be controlled (in this context, created).
            if (null == me)
                return true;

//            MainConsole.Instance.DebugFormat(
//                "[PERMISSIONS]: Checking CanInteractWithPrimMedia for {0} on {1} face {2} with interact permissions {3}", 
//                agentID, primID, face, me.InteractPermissions);

            return GenericPrimMediaPermission(part, agentID, me.InteractPermissions);
        }

        private bool GenericPrimMediaPermission(ISceneChildEntity part, UUID agentID, MediaPermission perms)
        {
//            if (IsAdministrator(agentID))
//                return true;

            if ((perms & MediaPermission.Anyone) == MediaPermission.Anyone)
                return true;

            if ((perms & MediaPermission.Owner) == MediaPermission.Owner)
            {
                if (agentID == part.OwnerID)
                    return true;
            }

            if ((perms & MediaPermission.Group) == MediaPermission.Group)
            {
                if (IsGroupMember(part.GroupID, agentID, 0))
                    return true;
            }

            return false;
        }

        #region Object Permissions

        public PermissionClass GetPermissionClass(UUID user, ISceneChildEntity obj)
        {
            if (obj == null)
                return PermissionClass.Everyone;

            if (m_bypassPermissions)
                return PermissionClass.Owner;

            // Object owners should be able to edit their own content
            UUID objectOwner = obj.OwnerID;
            if (user == objectOwner)
                return PermissionClass.Owner;

            if (IsFriendWithPerms(user, objectOwner))
                return PermissionClass.Owner;

            // Estate users should be able to edit anything in the sim if RegionOwnerIsGod is set
            if (m_RegionOwnerIsGod && IsEstateManager(user) && !IsAdministrator(objectOwner))
                return PermissionClass.Owner;

            // Admin should be able to edit anything in the sim (including admin objects)
            if (IsAdministrator(user))
                return PermissionClass.Owner;

            // Users should be able to edit what is over their land.
            Vector3 taskPos = obj.AbsolutePosition;
            if (m_parcelManagement != null)
            {
                ILandObject parcel = m_parcelManagement.GetLandObject(taskPos.X, taskPos.Y);
                if (parcel != null && parcel.LandData.OwnerID == user && m_ParcelOwnerIsGod)
                {
                    // Admin objects should not be editable by the above
                    if (!IsAdministrator(objectOwner))
                        return PermissionClass.Owner;
                }
            }

            // Group permissions
            if ((obj.GroupID != UUID.Zero) && IsGroupMember(obj.GroupID, user, 0))
                return PermissionClass.Group;

            return PermissionClass.Everyone;
        }

        public uint GenerateClientFlags(UUID user, ISceneChildEntity task)
        {
            // Here's the way this works,
            // ObjectFlags and Permission flags are two different enumerations
            // ObjectFlags, however, tells the client to change what it will allow the user to do.
            // So, that means that all of the permissions type ObjectFlags are /temporary/ and only
            // supposed to be set when customizing the objectflags for the client.

            // These temporary objectflags get computed and added in this function based on the
            // Permission mask that's appropriate!
            // Outside of this method, they should never be added to objectflags!
            // -teravus

            // this shouldn't ever happen..     return no permissions/objectflags.
            if (task == null)
                return 0;

            uint objflags = task.GetEffectiveObjectFlags();
            UUID objectOwner = task.OwnerID;


            // Remove any of the objectFlags that are temporary.  These will get added back if appropriate
            // in the next bit of code

            // libomv will moan about PrimFlags.ObjectYouOfficer being
            // deprecated
#pragma warning disable 0612 
            objflags &= (uint)
                        ~(PrimFlags.ObjectCopy | // Tells client you can copy the object
                          PrimFlags.ObjectModify | // tells client you can modify the object
                          PrimFlags.ObjectMove | // tells client that you can move the object (only, no mod)
                          PrimFlags.ObjectTransfer |
                          // tells the client that you can /take/ the object if you don't own it
                          PrimFlags.ObjectYouOwner | // Tells client that you're the owner of the object
                          PrimFlags.ObjectAnyOwner | // Tells client that someone owns the object
                          PrimFlags.ObjectOwnerModify | // Tells client that you're the owner of the object
                          PrimFlags.ObjectYouOfficer
                         // Tells client that you've got group object editing permission. Used when ObjectGroupOwned is set
                         );
#pragma warning restore 0612


            // Creating the three ObjectFlags options for this method to choose from.
            // Customize the OwnerMask
            uint objectOwnerMask = ApplyObjectModifyMasks(task.OwnerMask, objflags);
            objectOwnerMask |= (uint) PrimFlags.ObjectYouOwner | (uint) PrimFlags.ObjectAnyOwner |
                               (uint) PrimFlags.ObjectOwnerModify;

            // Customize the GroupMask
            uint objectGroupMask = ApplyObjectModifyMasks(task.GroupMask, objflags);

            // Customize the EveryoneMask
            uint objectEveryoneMask = ApplyObjectModifyMasks(task.EveryoneMask, objflags);
            if (objectOwner != UUID.Zero)
                objectEveryoneMask |= (uint) PrimFlags.ObjectAnyOwner;

            PermissionClass permissionClass = GetPermissionClass(user, task);
            switch (permissionClass)
            {
                case PermissionClass.Owner:
                    return objectOwnerMask;
                case PermissionClass.Group:
                    return objectGroupMask | objectEveryoneMask;
                case PermissionClass.Everyone:
                default:
                    return objectEveryoneMask;
            }
        }

        private uint ApplyObjectModifyMasks(uint setPermissionMask, uint objectFlagsMask)
        {
            // We are adding the temporary objectflags to the object's objectflags based on the
            // permission flag given.  These change the F flags on the client.

            if ((setPermissionMask & (uint) PermissionMask.Copy) != 0)
            {
                objectFlagsMask |= (uint) PrimFlags.ObjectCopy;
            }

            if ((setPermissionMask & (uint) PermissionMask.Move) != 0)
            {
                objectFlagsMask |= (uint) PrimFlags.ObjectMove;
            }

            if ((setPermissionMask & (uint) PermissionMask.Modify) != 0)
            {
                objectFlagsMask |= (uint) PrimFlags.ObjectModify;
            }

            if ((setPermissionMask & (uint) PermissionMask.Transfer) != 0)
            {
                objectFlagsMask |= (uint) PrimFlags.ObjectTransfer;
            }

            return objectFlagsMask;
        }

        /// <summary>
        ///   General permissions checks for any operation involving an object.  These supplement more specific checks
        ///   implemented by callers.
        /// </summary>
        /// <param name = "currentUser"></param>
        /// <param name = "objId"></param>
        /// <param name = "denyOnLocked"></param>
        /// <returns></returns>
        protected bool GenericObjectPermission(UUID currentUser, UUID objId, bool denyOnLocked)
        {
            // Default: deny
            bool locked = false;

            SceneObjectGroup group;
            IEntity entity;
            if (!m_scene.Entities.TryGetValue(objId, out entity))
            {
                if (!m_scene.Entities.TryGetChildPrimParent(objId, out entity))
                    return false;
                    
                group = (SceneObjectGroup) entity;
            }
            else
            {
                // If it's not an object, we cant edit it.
                if (!(entity is SceneObjectGroup))
                    return false;
                    
                group = (SceneObjectGroup) entity;
            }

            UUID objectOwner = group.OwnerID;
            locked = ((group.RootPart.OwnerMask & PERM_LOCKED) == 0);

            // People shouldn't be able to do anything with locked objects, except the Administrator
            // The 'set permissions' runs through a different permission check, so when an object owner
            // sets an object locked, the only thing that they can do is unlock it.
            //
            // Nobody but the object owner can set permissions on an object
            //

            if (locked && (!IsAdministrator(currentUser)) && denyOnLocked)
                return false;
                
            // Object owners should be able to edit their own content
            if (currentUser == objectOwner)
                return true;

            //Check friend perms
            if (IsFriendWithPerms(currentUser, objectOwner))
                return true;

            // Group members should be able to edit group objects
            ISceneChildEntity part = m_scene.GetSceneObjectPart(objId);
            if ((group.GroupID != UUID.Zero)
                && (part != null && (part.GroupMask & (uint) PermissionMask.Modify) != 0)
                && IsGroupMember(group.GroupID, currentUser, (ulong) GroupPowers.ObjectManipulate))
            {
                // Return immediately, so that the administrator can shares group objects
                return true;
            }

            // Users should be able to edit what is over their land.
            if (m_parcelManagement != null)
            {
                ILandObject parcel = m_parcelManagement.GetLandObject(group.AbsolutePosition.X, group.AbsolutePosition.Y);
                if ((parcel != null) && (parcel.LandData.OwnerID == currentUser))
                    return true;
            }

            // Estate users should be able to edit anything in the sim
            if (IsEstateManager(currentUser))
                return true;

            // Admin objects should not be editable by the above
            if (IsAdministrator(objectOwner))
            {
                bool permission = (IsFriendWithPerms(currentUser, objectOwner) && m_allowAdminFriendEditRights);
                if(permission)
                    return true;
            }

            // Admin should be able to edit anything in the sim (including admin objects)
            if (IsAdministrator(currentUser))
                return true;

            return false;
        }

        #endregion

        #region Generic Permissions

        protected bool GenericCommunicationPermission(UUID user, UUID target)
        {
            //TODO:FEATURE: Setting this to true so that cool stuff can happen until we define what determines Generic Communication Permission
            //bool permission = false;
            return true;
            /*string reason = "Only registered users may communicate with another account.";

            // Uhh, we need to finish this before we enable it..   because it's blocking all sorts of goodies and features
            if (IsAdministrator(user))
                permission = true;

            if (IsEstateManager(user))
                permission = true;

            if (!permission)
                SendPermissionError(user, reason);

            return permission;*/
        }

        public bool GenericEstatePermission(UUID user)
        {
            // Default: deny

            // Estate admins should be able to use estate tools
            if (IsEstateManager(user))
                return true;

            // Administrators always have permission
            if (IsAdministrator(user))
                return true;

            return false;
        }

        protected bool GenericParcelPermission(UUID user, ILandObject parcel, ulong groupPowers)
        {
            if (parcel.LandData.OwnerID == user)
                return true;

            if ((parcel.LandData.GroupID != UUID.Zero) && IsGroupMember(parcel.LandData.GroupID, user, groupPowers))
                return true;

            if (GenericEstatePermission(user))
                return true;

            return false;
        }

        public bool SetHomePoint(UUID userID)
        {
            if (GenericEstatePermission(userID))
                return true;

            if (!m_scene.RegionInfo.EstateSettings.AllowSetHome)
                return false;

            IScenePresence SP = m_scene.GetScenePresence(userID);
            if (SP == null)
                return false;

            if (m_parcelManagement == null)
                return true;
            ILandObject parcel = m_parcelManagement.GetLandObject(SP.AbsolutePosition.X, SP.AbsolutePosition.Y);
            if (parcel == null) return false;

            if (GenericParcelPermission(userID, parcel, (ulong) GroupPowers.AllowSetHome))
                return true;

            return false;
        }

        public bool TakeLandmark(UUID userID)
        {
            if (IsAdministrator(userID))
                return true;

            if (GenericEstatePermission(userID))
                return true;

            //No landmarks except for estate owners or gods
            if (!m_scene.RegionInfo.EstateSettings.AllowLandmark)
                return false;

            IScenePresence SP = m_scene.GetScenePresence(userID);
            if (SP == null)
                return false;

            if (m_parcelManagement == null)
                return true;
            ILandObject parcel = m_parcelManagement.GetLandObject(SP.AbsolutePosition.X, SP.AbsolutePosition.Y);
            if (parcel == null) return false;

            if (GenericParcelPermission(userID, parcel, (ulong) GroupPowers.AllowLandmark))
                return true;

            if ((parcel.LandData.Flags & (uint) ParcelFlags.AllowLandmark) == 0)
                return false;

            return true;
        }

        private bool CanEditParcelProperties(UUID user, ILandObject parcel, GroupPowers p, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericParcelPermission(user, parcel, (ulong) p);
        }

        protected bool GenericParcelPermission(UUID user, Vector3 pos, ulong groupPowers)
        {
            if (m_parcelManagement == null)
                return true;
            ILandObject parcel = m_parcelManagement.GetLandObject(pos.X, pos.Y);
            if (parcel == null) return false;
            return GenericParcelPermission(user, parcel, groupPowers);
        }

        #endregion

        #region Permission Checks

        private bool CanAbandonParcel(UUID user, ILandObject parcel, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericParcelPermission(user, parcel, (ulong) GroupPowers.LandRelease);
        }

        private bool CanReclaimParcel(UUID user, ILandObject parcel, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericParcelPermission(user, parcel, (ulong) GroupPowers.LandRelease);
        }

        private bool CanDeedParcel(UUID user, ILandObject parcel, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            if (parcel.LandData.OwnerID != user) // Only the owner can deed!
                return false;

            return GenericParcelPermission(user, parcel, (ulong) GroupPowers.LandDeed);
        }

        private bool CanDeedObject(UUID user, UUID group, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return m_groupsModule.GroupPermissionCheck(user, group, GroupPowers.DeedObject);
        }

        private bool IsGod(UUID user, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return InternalIsAdministrator(user, true);
        }

        private bool CanDuplicateObject(int objectCount, UUID objectID, UUID owner, IScene scene, Vector3 objectPosition)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            if (!GenericObjectPermission(owner, objectID, true))
            {
                //They can't even edit the object
                return false;
            }

            ISceneChildEntity part = scene.GetSceneObjectPart(objectID);
            if (part == null)
                return false;

            if (part.OwnerID == owner)
                return ((part.OwnerMask & PERM_COPY) != 0);

            if (part.GroupID != UUID.Zero)
            {
                if ((part.OwnerID == part.GroupID) &&
                    ((owner != part.LastOwnerID) || ((part.GroupMask & PERM_TRANS) == 0)))
                    return false;

                if ((part.GroupMask & PERM_COPY) == 0)
                    return false;

                if (!IsGroupMember(part.GroupID, owner, (ulong) GroupPowers.ObjectManipulate))
                    return false;
            }

            //If they can rez, they can duplicate
            string reason;
            return CanRezObject(objectCount, owner, objectPosition, scene, out reason);
        }

        private bool CanDeleteObject(UUID objectID, UUID deleter, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericObjectPermission(deleter, objectID, false);
        }

        private bool CanEditObject(UUID objectID, UUID editorID, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericObjectPermission(editorID, objectID, false);
        }

        private bool CanEditObjectInventory(UUID objectID, UUID editorID, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            ISceneChildEntity part = m_scene.GetSceneObjectPart(objectID);

            if (part == null)
            {
                IEntity group;
                m_scene.Entities.TryGetValue(objectID, out group);
                if (group == null)
                {
                    MainConsole.Instance.Warn("[PERMISSIONS]: COULD NOT FIND PRIM FOR CanEditObjectInventory! " + objectID);
                    return false;
                }
                objectID = group.UUID;
            }
            else
            {
                // If we selected a sub-prim to edit, the objectID won't represent the object, but only a part.
                // We have to check the permissions of the group, though.
                if (part.ParentEntity != null)
                {
                    objectID = part.ParentEntity.UUID;
                }
            }

            return GenericObjectPermission(editorID, objectID, false);
        }

        private bool CanEditParcel(UUID user, ILandObject parcel, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericParcelPermission(user, parcel, (ulong) GroupPowers.LandChangeIdentity);
        }

        private bool CanSubdivideParcel(UUID user, ILandObject parcel, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericParcelPermission(user, parcel, (ulong) GroupPowers.LandDivideJoin);
        }

        /// <summary>
        ///   Check whether the specified user can edit the given script
        /// </summary>
        /// <param name = "script"></param>
        /// <param name = "objectID"></param>
        /// <param name = "user"></param>
        /// <param name = "scene"></param>
        /// <returns></returns>
        private bool CanEditScript(UUID script, UUID objectID, UUID user, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            if (m_allowedScriptEditors == UserSet.Administrators && !IsAdministrator(user))
                return false;
            if (m_allowedScriptEditors == UserSet.ParcelOwners &&
                !GenericParcelPermission(user, scene.GetScenePresence(user).AbsolutePosition, 0))
                return false;

            // Ordinarily, if you can view it, you can edit it
            // There is no viewing a no mod script
            //
            return CanViewScript(script, objectID, user, scene);
        }

        /// <summary>
        ///   Check whether the specified user can edit the given notecard
        /// </summary>
        /// <param name = "notecard"></param>
        /// <param name = "objectID"></param>
        /// <param name = "user"></param>
        /// <param name = "scene"></param>
        /// <returns></returns>
        private bool CanEditNotecard(UUID notecard, UUID objectID, UUID user, IScene scene)
        {
            return CanViewNotecard(notecard, objectID, user, scene);
        }

        private bool CanInstantMessage(UUID user, UUID target, IScene startScene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            // If the sender is an object, check owner instead
            //
            ISceneChildEntity part = startScene.GetSceneObjectPart(user);
            if (part != null)
                user = part.OwnerID;

            return GenericCommunicationPermission(user, target);
        }

        private bool CanGodTp(UUID user, UUID target)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            if(IsGod(user, m_scene))
            {
                if (IsGod(target, m_scene)) //if they are an admin
                    return false;

                return true;
            }
            return false;
        }

        private bool CanInventoryTransfer(UUID user, UUID target, IScene startScene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericCommunicationPermission(user, target);
        }

        private bool CanIssueEstateCommand(UUID user, IScene requestFromScene, bool ownerCommand)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            if (IsAdministrator(user))
                return true;

            if (m_scene.RegionInfo.EstateSettings.IsEstateOwner(user))
                return true;

            if (ownerCommand)
                return false;

            return GenericEstatePermission(user);
        }

        private bool CanMoveObject(UUID objectID, UUID moverID, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions)
            {
                ISceneChildEntity part = scene.GetSceneObjectPart(objectID);
                if (part.OwnerID != moverID)
                {
                    if (part.ParentEntity != null && !part.ParentEntity.IsDeleted)
                    {
                        if (part.ParentEntity.IsAttachment)
                            return false;
                    }
                }
                return m_bypassPermissionsValue;
            }

            bool permission = GenericObjectPermission(moverID, objectID, true);
            if (!permission)
            {
                IEntity ent;
                if (!m_scene.Entities.TryGetValue(objectID, out ent))
                {
                    return false;
                }

                // The client
                // may request to edit linked parts, and therefore, it needs
                // to also check for SceneObjectPart

                // If it's not an object, we cant edit it.
                if (!(ent is ISceneEntity))
                {
                    return false;
                }

                ISceneEntity task = (ISceneEntity) ent;


                // UUID taskOwner = null;
                // Added this because at this point in time it wouldn't be wise for
                // the administrator object permissions to take effect.
                // UUID objectOwner = task.OwnerID;

                // Anyone can move
                if ((task.RootChild.EveryoneMask & PERM_MOVE) != 0)
                    permission = true;

                // Locked
                if ((task.RootChild.OwnerMask & PERM_LOCKED) == 0)
                    permission = false;
            }
            else
            {
                bool locked = false;
                IEntity ent;
                if (!m_scene.Entities.TryGetValue(objectID, out ent))
                {
                    return false;
                }

                // If it's not an object, we cant edit it.
                if (!(ent is ISceneEntity))
                {
                    return false;
                }

                ISceneEntity group = (ISceneEntity) ent;

                UUID objectOwner = group.OwnerID;
                locked = ((group.RootChild.OwnerMask & PERM_LOCKED) == 0);

                // This is an exception to the generic object permission.
                // Administrators who lock their objects should not be able to move them,
                // however generic object permission should return true.
                // This keeps locked objects from being affected by random click + drag actions by accident
                // and allows the administrator to grab or delete a locked object.

                // Administrators and estate managers are still able to click+grab locked objects not
                // owned by them in the scene
                // This is by design.

                if (locked && (moverID == objectOwner))
                    return false;
            }
            return permission;
        }

        private bool CanObjectEntry(UUID objectID, bool enteringRegion, Vector3 newPoint, UUID OwnerID)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            if (newPoint.X > m_scene.RegionInfo.RegionSizeX || newPoint.X < 0 ||
                newPoint.Y > m_scene.RegionInfo.RegionSizeY || newPoint.Y < 0)
            {
                return true;
            }
            IEntity ent;
            //If the object is entering the region, its not here yet and we can't check for it
            if (!enteringRegion && !m_scene.Entities.TryGetValue(objectID, out ent))
            {
                return false;
            }

            //If there is no parcel management, we don't do anymore checks
            if (m_parcelManagement == null)
                return true;

            ILandObject land = m_parcelManagement.GetLandObject(newPoint.X, newPoint.Y);

            if (land == null)
            {
                return false;
            }

            if ((land.LandData.Flags & ((int) ParcelFlags.AllowAPrimitiveEntry)) != 0)
            {
                return true;
            }

            if (land.LandData.OwnerID == OwnerID)
            {
                return true;
            }

            //Check for group permissions
            if (((land.LandData.Flags & ((int) ParcelFlags.AllowGroupObjectEntry)) != 0) &&
                (land.LandData.GroupID != UUID.Zero)
                && IsGroupMember(land.LandData.GroupID, OwnerID, (ulong) GroupPowers.None))
            {
                return true;
            }

            //check for admin statuses
            if (IsEstateManager(OwnerID))
            {
                return true;
            }

            if (IsAdministrator(OwnerID))
            {
                return true;
            }

            //Otherwise false
            return false;
        }

        private bool CanReturnObjects(ILandObject land, UUID user, List<ISceneEntity> objects, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            ILandObject l;

            IScenePresence sp = scene.GetScenePresence(user);
            if (sp == null)
                return false;

            IClientAPI client = sp.ControllingClient;

            //Make a copy so that it doesn't get modified outside of this loop
#if (!ISWIN)
            foreach (ISceneEntity g in new List<ISceneEntity>(objects))
            {
                if (!GenericObjectPermission(user, g.UUID, false))
                {
                    // This is a short cut for efficiency. If land is non-null,
                    // then all objects are on that parcel and we can save
                    // ourselves the checking for each prim. Much faster.
                    //
                    if (land != null)
                    {
                        l = land;
                    }
                    else
                    {
                        Vector3 pos = g.AbsolutePosition;
                        if (m_parcelManagement == null)
                            continue;

                        l = m_parcelManagement.GetLandObject(pos.X, pos.Y);
                    }

                    // If it's not over any land, then we can't do a thing
                    if (l == null)
                    {
                        objects.Remove(g);
                        continue;
                    }

                    // If we own the land outright, then allow
                    //
                    if (l.LandData.OwnerID == user)
                        continue;

                    // Group voodoo
                    //
                    if (l.LandData.IsGroupOwned)
                    {
                        // Not a group member, or no rights at all
                        //
                        if (!m_groupsModule.GroupPermissionCheck(client.AgentId, g.GroupID, GroupPowers.None))
                        {
                            objects.Remove(g);
                            continue;
                        }

                        // Group deeded object?
                        //
                        if (g.OwnerID == l.LandData.GroupID && !m_groupsModule.GroupPermissionCheck(client.AgentId, g.GroupID, GroupPowers.ReturnGroupOwned))
                        {
                            objects.Remove(g);
                            continue;
                        }

                        // Group set object?
                        //
                        if (g.GroupID == l.LandData.GroupID && !m_groupsModule.GroupPermissionCheck(client.AgentId, g.GroupID, GroupPowers.ReturnGroupSet))
                        {
                            objects.Remove(g);
                            continue;
                        }

                        if (!m_groupsModule.GroupPermissionCheck(client.AgentId, g.GroupID, GroupPowers.ReturnNonGroup))
                        {
                            objects.Remove(g);
                            continue;
                        }

                        // So we can remove all objects from this group land.
                        // Fine.
                        //
                        continue;
                    }

                    // By default, we can't remove
                    //
                    objects.Remove(g);
                }
            }
#else
            foreach (ISceneEntity g in new List<ISceneEntity>(objects).Where(g => !GenericObjectPermission(user, g.UUID, false)))
            {
                // This is a short cut for efficiency. If land is non-null,
                // then all objects are on that parcel and we can save
                // ourselves the checking for each prim. Much faster.
                //
                if (land != null)
                {
                    l = land;
                }
                else
                {
                    Vector3 pos = g.AbsolutePosition;
                    if (m_parcelManagement == null)
                        continue;

                    l = m_parcelManagement.GetLandObject(pos.X, pos.Y);
                }

                // If it's not over any land, then we can't do a thing
                if (l == null)
                {
                    objects.Remove(g);
                    continue;
                }

                // If we own the land outright, then allow
                //
                if (l.LandData.OwnerID == user)
                    continue;

                // Group voodoo
                //
                if (l.LandData.IsGroupOwned)
                {
                    // Not a group member, or no rights at all
                    //
                    if (!m_groupsModule.GroupPermissionCheck(client.AgentId, g.GroupID, GroupPowers.None))
                    {
                        objects.Remove(g);
                        continue;
                    }

                    // Group deeded object?
                    //
                    if (g.OwnerID == l.LandData.GroupID &&
                        !m_groupsModule.GroupPermissionCheck(client.AgentId, g.GroupID, GroupPowers.ReturnGroupOwned))
                    {
                        objects.Remove(g);
                        continue;
                    }

                    // Group set object?
                    //
                    if (g.GroupID == l.LandData.GroupID &&
                        !m_groupsModule.GroupPermissionCheck(client.AgentId, g.GroupID, GroupPowers.ReturnGroupSet))
                    {
                        objects.Remove(g);
                        continue;
                    }

                    if (!m_groupsModule.GroupPermissionCheck(client.AgentId, g.GroupID, GroupPowers.ReturnNonGroup))
                    {
                        objects.Remove(g);
                        continue;
                    }

                    // So we can remove all objects from this group land.
                    // Fine.
                    //
                    continue;
                }

                // By default, we can't remove
                //
                objects.Remove(g);
            }
#endif

            if (objects.Count == 0)
                return false;

            return true;
        }

        private bool CanRezObject(int objectCount, UUID attemptedRezzer, Vector3 objectPosition, IScene scene,
                                  out string reason)
        {
            reason = "";
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            bool permission = false;

            if (m_parcelManagement == null)
                return true;
            ILandObject land = m_parcelManagement.GetLandObject(objectPosition.X, objectPosition.Y);
            if (land == null)
            {
                reason = "Cannot find land at your current location";
                return false;
            }

            if ((land.LandData.Flags & ((int) ParcelFlags.CreateObjects)) ==
                (int) ParcelFlags.CreateObjects)
                permission = true;

            if ((land.LandData.Flags & ((int) ParcelFlags.CreateGroupObjects)) ==
                (int) ParcelFlags.CreateGroupObjects &&
                land.LandData.GroupID != UUID.Zero &&
                IsGroupMember(land.LandData.GroupID, attemptedRezzer, (ulong) GroupPowers.AllowRez))
                permission = true;

            if (IsAdministrator(attemptedRezzer))
                return true;

            // Powers are zero, because GroupPowers.AllowRez is not a precondition for rezzing objects
            if (GenericParcelPermission(attemptedRezzer, land, 0))
                permission = true;

            IPrimCountModule primCountModule = m_scene.RequestModuleInterface<IPrimCountModule>();
            if (primCountModule != null)
            {
                IPrimCounts primCounts = primCountModule.GetPrimCounts(land.LandData.GlobalID);
                int MaxPrimCounts = primCountModule.GetParcelMaxPrimCount(land);
                if (primCounts.Total + objectCount > MaxPrimCounts)
                {
                    reason = "There are too many prims in this parcel.";
                    return false;
                }
            }

            return permission;
        }

        private bool CanRunConsoleCommand(UUID user, IScene requestFromScene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;


            return IsAdministrator(user);
        }

        private bool CanRunScript(UUID script, UUID objectID, UUID user, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            ISceneChildEntity part = scene.GetSceneObjectPart(objectID);

            if (part == null)
                return false;

            if (m_parcelManagement == null)
                return true;
            ILandObject parcel = m_parcelManagement.GetLandObject(part.AbsolutePosition.X, part.AbsolutePosition.Y);

            if (parcel == null)
                return false;

            if ((parcel.LandData.Flags & (int) ParcelFlags.AllowOtherScripts) != 0)
                return true;

            if ((parcel.LandData.Flags & (int) ParcelFlags.AllowGroupScripts) == 0)
            {
                //Only owner can run then
                return GenericParcelPermission(user, parcel, 0);
            }

            return GenericParcelPermission(user, parcel, (ulong) GroupPowers.None);
        }

        private bool CanSellParcel(UUID user, ILandObject parcel, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericParcelPermission(user, parcel, (ulong) GroupPowers.LandSetSale);
        }

        private bool CanTakeObject(UUID objectID, UUID stealer, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            return GenericObjectPermission(stealer, objectID, false);
        }

        private bool CanTakeCopyObject(UUID objectID, UUID userID, IScene inScene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            bool permission = GenericObjectPermission(userID, objectID, false);
            if (!permission)
            {
                IEntity ent;
                if (!m_scene.Entities.TryGetValue(objectID, out ent))
                {
                    return false;
                }

                // If it's not an object, we cant edit it.
                if (!(ent is SceneObjectGroup))
                {
                    return false;
                }

                SceneObjectGroup task = (SceneObjectGroup) ent;
                // UUID taskOwner = null;
                // Added this because at this point in time it wouldn't be wise for
                // the administrator object permissions to take effect.
                // UUID objectOwner = task.OwnerID;

                if ((task.RootPart.EveryoneMask & PERM_COPY) != 0)
                    permission = true;

                if (task.OwnerID != userID)
                {
                    if ((task.GetEffectivePermissions() & (PERM_COPY | PERM_TRANS)) != (PERM_COPY | PERM_TRANS))
                        permission = false;
                }
                else
                {
                    if ((task.GetEffectivePermissions() & PERM_COPY) != PERM_COPY)
                        permission = false;
                }
            }
            else
            {
                IEntity ent;
                if (m_scene.Entities.TryGetValue(objectID, out ent) && ent is SceneObjectGroup)
                {
                    SceneObjectGroup task = (SceneObjectGroup) ent;

                    if ((task.GetEffectivePermissions() & (PERM_COPY | PERM_TRANS)) != (PERM_COPY | PERM_TRANS))
                        permission = false;
                }
            }

            return permission;
        }

        private bool CanTerraformLand(UUID user, Vector3 position, IScene requestFromScene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            // Estate override
            if (GenericEstatePermission(user))
                return true;

            if (m_scene.RegionInfo.RegionSettings.BlockTerraform)
                return false; //Blocks land owners and others

            float X = position.X;
            float Y = position.Y;

            if (X > (m_scene.RegionInfo.RegionSizeX - 1))
                X = (m_scene.RegionInfo.RegionSizeX - 1);
            if (Y > (m_scene.RegionInfo.RegionSizeY - 1))
                Y = (m_scene.RegionInfo.RegionSizeY - 1);
            if (X < 0)
                X = 0;
            if (Y < 0)
                Y = 0;

            if (m_parcelManagement == null)
                return true;
            ILandObject parcel = m_parcelManagement.GetLandObject(X, Y);
            if (parcel == null)
                return false;

            // Others allowed to terraform?
            if ((parcel.LandData.Flags & ((int) ParcelFlags.AllowTerraform)) != 0)
                return true;

            // Land owner can terraform too
            if (parcel != null && GenericParcelPermission(user, parcel, (ulong) GroupPowers.AllowEditLand))
                return true;

            return false;
        }

        /// <summary>
        ///   Check whether the specified user can view the given script
        /// </summary>
        /// <param name = "script"></param>
        /// <param name = "objectID"></param>
        /// <param name = "user"></param>
        /// <param name = "scene"></param>
        /// <returns></returns>
        private bool CanViewScript(UUID script, UUID objectID, UUID user, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            if (IsAdministrator(user))
                return true;

            if (objectID == UUID.Zero) // User inventory
            {
                IInventoryService invService = m_scene.InventoryService;
                InventoryItemBase assetRequestItem = new InventoryItemBase(script, user);
                assetRequestItem = invService.GetItem(assetRequestItem);
                if (assetRequestItem == null)
                {
                    //Can't find, can't read
                    return false;
                }

                // SL is rather harebrained here. In SL, a script you
                // have mod/copy no trans is readable. This subverts
                // permissions, but is used in some products, most
                // notably Hippo door plugin and HippoRent 5 networked
                // prim counter.
                // To enable this broken SL-ism, remove Transfer from
                // the below expressions.
                // Trying to improve on SL perms by making a script
                // readable only if it's really full perms
                //
                if ((assetRequestItem.CurrentPermissions &
                     ((uint) PermissionMask.Modify |
                      (uint) PermissionMask.Copy |
                      (uint) PermissionMask.Transfer)) !=
                    ((uint) PermissionMask.Modify |
                     (uint) PermissionMask.Copy |
                     (uint) PermissionMask.Transfer))
                    return false;
            }
            else // Prim inventory
            {
                ISceneChildEntity part = scene.GetSceneObjectPart(objectID);

                if (part == null)
                {
                    MainConsole.Instance.Warn("[PERMISSIONS]: NULL PRIM IN canViewScript! " + objectID);
                    return false;
                }

                if (part.OwnerID != user)
                {
                    if (part.GroupID != UUID.Zero)
                    {
                        if (!IsGroupMember(part.GroupID, user, 0))
                            return false;

                        if ((part.GroupMask & (uint) PermissionMask.Modify) == 0)
                            return false;
                    }
                }
                else
                {
                    if ((part.OwnerMask & (uint) PermissionMask.Modify) == 0)
                        return false;
                }

                TaskInventoryItem ti = part.Inventory.GetInventoryItem(script);

                if (ti == null)
                    return false;

                if (ti.OwnerID != user)
                {
                    if (ti.GroupID == UUID.Zero)
                        return false;

                    if (!IsGroupMember(ti.GroupID, user, 0))
                        return false;
                }

                // Require full perms
                if ((ti.CurrentPermissions &
                     ((uint) PermissionMask.Modify |
                      (uint) PermissionMask.Copy |
                      (uint) PermissionMask.Transfer)) !=
                    ((uint) PermissionMask.Modify |
                     (uint) PermissionMask.Copy |
                     (uint) PermissionMask.Transfer))
                    return false;
            }

            return true;
        }

        /// <summary>
        ///   Check whether the specified user can view the given notecard
        /// </summary>
        /// <param name = "script"></param>
        /// <param name = "objectID"></param>
        /// <param name = "user"></param>
        /// <param name = "scene"></param>
        /// <returns></returns>
        private bool CanViewNotecard(UUID notecard, UUID objectID, UUID user, IScene scene)
        {
            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            DebugPermissionInformation(MethodBase.GetCurrentMethod().Name);
            if (m_bypassPermissions) return m_bypassPermissionsValue;

            if (IsAdministrator(user))
                return true;

            if (objectID == UUID.Zero) // User inventory
            {
                IInventoryService invService = m_scene.InventoryService;
                InventoryItemBase assetRequestItem = new InventoryItemBase(notecard, user);
                assetRequestItem = invService.GetItem(assetRequestItem);
                if (assetRequestItem == null) // Library item
                {
                    //Can't find, can't read
                    return false;
                }

                // SL is rather harebrained here. In SL, a script you
                // have mod/copy no trans is readable. This subverts
                // permissions, but is used in some products, most
                // notably Hippo door plugin and HippoRent 5 networked
                // prim counter.
                // To enable this broken SL-ism, remove Transfer from
                // the below expressions.
                // Trying to improve on SL perms by making a script
                // readable only if it's really full perms
                //
                if ((assetRequestItem.CurrentPermissions &
                     ((uint) PermissionMask.Modify |
                      (uint) PermissionMask.Copy |
                      (uint) PermissionMask.Transfer)) !=
                    ((uint) PermissionMask.Modify |
                     (uint) PermissionMask.Copy |
                     (uint) PermissionMask.Transfer))
                    return false;
            }
            else // Prim inventory
            {
                ISceneChildEntity part = scene.GetSceneObjectPart(objectID);

                if (part == null)
                {
                    MainConsole.Instance.Warn("[PERMISSIONS]: NULL PRIM IN canViewNotecard! " + objectID);
                    return false;
                }

                if (part.OwnerID != user)
                {
                    if (part.GroupID != UUID.Zero)
                    {
                        if (!IsGroupMember(part.GroupID, user, 0))
                            return false;

                        if ((part.GroupMask & (uint) PermissionMask.Modify) == 0)
                            return false;
                    }
                }
                else
                {
                    if ((part.OwnerMask & (uint) PermissionMask.Modify) == 0)
                        return false;
                }

                TaskInventoryItem ti = part.Inventory.GetInventoryItem(notecard);

                if (ti == null)
                    return false;

                if (ti.OwnerID != user)
                {
                    if (ti.GroupID == UUID.Zero)
                        return false;

                    if (!IsGroupMember(ti.GroupID, user, 0))
                        return false;
                }

                // Require full perms
                if ((ti.CurrentPermissions &
                     ((uint) PermissionMask.Modify |
                      (uint) PermissionMask.Copy |
                      (uint) PermissionMask.Transfer)) !=
                    ((uint) PermissionMask.Modify |
                     (uint) PermissionMask.Copy |
                     (uint) PermissionMask.Transfer))
                    return false;
            }

            return true;
        }

        #endregion
    }
}