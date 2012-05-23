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
using OpenMetaverse;

namespace Aurora.Framework
{

    #region Delegates

    public delegate uint GenerateClientFlagsHandler(UUID userID, ISceneChildEntity part);

    public delegate void SetBypassPermissionsHandler(bool value);

    public delegate bool BypassPermissionsHandler();

    public delegate bool PropagatePermissionsHandler();

    public delegate bool RezObjectHandler(
        int objectCount, UUID owner, Vector3 objectPosition, IScene scene, out string reason);

    public delegate bool DeleteObjectHandler(UUID objectID, UUID deleter, IScene scene);

    public delegate bool TakeObjectHandler(UUID objectID, UUID stealer, IScene scene);

    public delegate bool TakeCopyObjectHandler(UUID objectID, UUID userID, IScene inScene);

    public delegate bool DuplicateObjectHandler(
        int objectCount, UUID objectID, UUID owner, IScene scene, Vector3 objectPosition);

    public delegate bool EditObjectHandler(UUID objectID, UUID editorID, IScene scene);

    public delegate bool EditObjectInventoryHandler(UUID objectID, UUID editorID, IScene scene);

    public delegate bool MoveObjectHandler(UUID objectID, UUID moverID, IScene scene);

    public delegate bool ObjectEntryHandler(UUID objectID, bool enteringRegion, Vector3 newPoint, UUID OwnerID);

    public delegate bool ReturnObjectsHandler(ILandObject land, UUID user, List<ISceneEntity> objects, IScene scene);

    public delegate bool InstantMessageHandler(UUID user, UUID target, IScene startScene);

    public delegate bool InventoryTransferHandler(UUID user, UUID target, IScene startScene);

    public delegate bool ViewScriptHandler(UUID script, UUID objectID, UUID user, IScene scene);

    public delegate bool ViewNotecardHandler(UUID script, UUID objectID, UUID user, IScene scene);

    public delegate bool EditScriptHandler(UUID script, UUID objectID, UUID user, IScene scene);

    public delegate bool EditNotecardHandler(UUID notecard, UUID objectID, UUID user, IScene scene);

    public delegate bool RunScriptHandler(UUID script, UUID objectID, UUID user, IScene scene);

    public delegate bool CompileScriptHandler(UUID ownerUUID, string scriptType, IScene scene);

    public delegate bool StartScriptHandler(UUID script, UUID user, IScene scene);

    public delegate bool StopScriptHandler(UUID script, UUID user, IScene scene);

    public delegate bool ResetScriptHandler(UUID prim, UUID script, UUID user, IScene scene);

    public delegate bool TerraformLandHandler(UUID user, Vector3 position, IScene requestFromScene);

    public delegate bool RunConsoleCommandHandler(UUID user, IScene requestFromScene);

    public delegate bool IssueEstateCommandHandler(UUID user, IScene requestFromScene, bool ownerCommand);

    public delegate bool IsGodHandler(UUID user, IScene requestFromScene);

    public delegate bool CanGodTpHandler(UUID user, UUID target);

    public delegate bool IsAdministratorHandler(UUID user);

    public delegate bool EditParcelPropertiesHandler(UUID user, ILandObject parcel, GroupPowers p, IScene scene);

    public delegate bool EditParcelHandler(UUID user, ILandObject parcel, IScene scene);

    public delegate bool SellParcelHandler(UUID user, ILandObject parcel, IScene scene);

    public delegate bool AbandonParcelHandler(UUID user, ILandObject parcel, IScene scene);

    public delegate bool ReclaimParcelHandler(UUID user, ILandObject parcel, IScene scene);

    public delegate bool DeedParcelHandler(UUID user, ILandObject parcel, IScene scene);

    public delegate bool DeedObjectHandler(UUID user, UUID group, IScene scene);

    public delegate bool BuyLandHandler(UUID user, ILandObject parcel, IScene scene);

    public delegate bool LinkObjectHandler(UUID user, UUID objectID);

    public delegate bool DelinkObjectHandler(UUID user, UUID objectID);

    public delegate bool CreateObjectInventoryHandler(int invType, UUID objectID, UUID userID);

    public delegate bool CopyObjectInventoryHandler(UUID itemID, UUID objectID, UUID userID);

    public delegate bool DeleteObjectInventoryHandler(UUID itemID, UUID objectID, UUID userID);

    public delegate bool CreateUserInventoryHandler(int invType, UUID userID);

    public delegate bool EditUserInventoryHandler(UUID itemID, UUID userID);

    public delegate bool CopyUserInventoryHandler(UUID itemID, UUID userID);

    public delegate bool DeleteUserInventoryHandler(UUID itemID, UUID userID);

    public delegate bool TeleportHandler(
        UUID userID, IScene scene, Vector3 Position, uint TeleportFlags, out Vector3 newPosition, out string reason);

    public delegate bool OutgoingRemoteTeleport(UUID userID, IScene scene, out string reason);

    public delegate bool IncomingAgentHandler(IScene scene, AgentCircuitData agent, bool isRootAgent, out string reason);

    public delegate bool PushObjectHandler(UUID userID, ILandObject parcel);

    public delegate bool EditParcelAccessListHandler(UUID userID, ILandObject parcel, uint flags);

    public delegate bool GenericParcelHandler(UUID user, ILandObject parcel, ulong groupPowers);

    public delegate bool TakeLandmark(UUID user);

    public delegate bool ControlPrimMediaHandler(UUID userID, UUID primID, int face);

    public delegate bool InteractWithPrimMediaHandler(UUID userID, UUID primID, int face);

    #endregion

    public class ScenePermissions
    {
        private readonly IScene m_scene;

        public ScenePermissions(IScene scene)
        {
            m_scene = scene;
        }

        #region Events

        public event GenerateClientFlagsHandler OnGenerateClientFlags;
        public event SetBypassPermissionsHandler OnSetBypassPermissions;
        public event BypassPermissionsHandler OnBypassPermissions;
        public event PropagatePermissionsHandler OnPropagatePermissions;
        public event RezObjectHandler OnRezObject;
        public event DeleteObjectHandler OnDeleteObject;
        public event TakeObjectHandler OnTakeObject;
        public event TakeCopyObjectHandler OnTakeCopyObject;
        public event DuplicateObjectHandler OnDuplicateObject;
        public event EditObjectHandler OnEditObject;
        public event EditObjectInventoryHandler OnEditObjectInventory;
        public event MoveObjectHandler OnMoveObject;
        public event ObjectEntryHandler OnObjectEntry;
        public event ReturnObjectsHandler OnReturnObjects;
        public event InstantMessageHandler OnInstantMessage;
        public event CanGodTpHandler OnCanGodTp;
        public event InventoryTransferHandler OnInventoryTransfer;
        public event ViewScriptHandler OnViewScript;
        public event ViewNotecardHandler OnViewNotecard;
        public event EditScriptHandler OnEditScript;
        public event EditNotecardHandler OnEditNotecard;
        public event RunScriptHandler OnRunScript;
        public event CompileScriptHandler OnCompileScript;
        public event StartScriptHandler OnStartScript;
        public event StopScriptHandler OnStopScript;
        public event ResetScriptHandler OnResetScript;
        public event TerraformLandHandler OnTerraformLand;
        public event RunConsoleCommandHandler OnRunConsoleCommand;
        public event IssueEstateCommandHandler OnIssueEstateCommand;
        public event IsGodHandler OnIsGod;
        public event IsAdministratorHandler OnIsAdministrator;
        public event EditParcelHandler OnEditParcel;
        public event EditParcelHandler OnSubdivideParcel;
        public event EditParcelPropertiesHandler OnEditParcelProperties;
        public event SellParcelHandler OnSellParcel;
        public event AbandonParcelHandler OnAbandonParcel;
        public event ReclaimParcelHandler OnReclaimParcel;
        public event DeedParcelHandler OnDeedParcel;
        public event DeedObjectHandler OnDeedObject;
        public event BuyLandHandler OnBuyLand;
        public event LinkObjectHandler OnLinkObject;
        public event DelinkObjectHandler OnDelinkObject;
        public event DelinkObjectHandler OnIsInGroup;
        public event CreateObjectInventoryHandler OnCreateObjectInventory;
        public event CopyObjectInventoryHandler OnCopyObjectInventory;
        public event DeleteObjectInventoryHandler OnDeleteObjectInventory;
        public event CreateUserInventoryHandler OnCreateUserInventory;
        public event EditUserInventoryHandler OnEditUserInventory;
        public event CopyUserInventoryHandler OnCopyUserInventory;
        public event DeleteUserInventoryHandler OnDeleteUserInventory;
        public event OutgoingRemoteTeleport OnAllowedOutgoingLocalTeleport;
        public event OutgoingRemoteTeleport OnAllowedOutgoingRemoteTeleport;
        public event IncomingAgentHandler OnAllowIncomingAgent;
        public event TeleportHandler OnAllowedIncomingTeleport;
        public event PushObjectHandler OnPushObject;
        public event PushObjectHandler OnViewObjectOwners;
        public event EditParcelAccessListHandler OnEditParcelAccessList;
        public event GenericParcelHandler OnGenericParcelHandler;
        public event TakeLandmark OnTakeLandmark;
        public event TakeLandmark OnSetHomePoint;
        public event ControlPrimMediaHandler OnControlPrimMedia;
        public event InteractWithPrimMediaHandler OnInteractWithPrimMedia;

        #endregion

        #region Object Permission Checks

        public uint GenerateClientFlags(UUID userID, ISceneChildEntity part)
        {
            // libomv will moan about PrimFlags.ObjectYouOfficer being
            // obsolete...
#pragma warning disable 0612
            const PrimFlags DEFAULT_FLAGS =
                PrimFlags.ObjectModify |
                PrimFlags.ObjectCopy |
                PrimFlags.ObjectMove |
                PrimFlags.ObjectTransfer |
                PrimFlags.ObjectYouOwner |
                PrimFlags.ObjectAnyOwner |
                PrimFlags.ObjectOwnerModify |
                PrimFlags.ObjectYouOfficer;
#pragma warning restore 0612

            if (part == null)
                return 0;

            uint perms = part.GetEffectiveObjectFlags() | (uint) DEFAULT_FLAGS;

            GenerateClientFlagsHandler handlerGenerateClientFlags = OnGenerateClientFlags;
            if (handlerGenerateClientFlags != null)
            {
                Delegate[] list = handlerGenerateClientFlags.GetInvocationList();
#if (!ISWIN)
                foreach (GenerateClientFlagsHandler handler in list)
                    perms = perms & handler(userID, part);
#else
                perms = list.Cast<GenerateClientFlagsHandler>().Aggregate(perms, (current, check) => current & check(userID, part));
#endif
            }
            return perms;
        }

        public void SetBypassPermissions(bool value)
        {
            SetBypassPermissionsHandler handler = OnSetBypassPermissions;
            if (handler != null)
                handler(value);
        }

        public bool BypassPermissions()
        {
            BypassPermissionsHandler handler = OnBypassPermissions;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (BypassPermissionsHandler h in list)
                {
                    if (h() == false) return false;
                }
                return true;
#else
                return list.Cast<BypassPermissionsHandler>().All(h => h() != false);
#endif
            }
            return true;
        }

        public bool PropagatePermissions()
        {
            PropagatePermissionsHandler handler = OnPropagatePermissions;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (PropagatePermissionsHandler h in list)
                {
                    if (h() == false) return false;
                }
                return true;
#else
                return list.Cast<PropagatePermissionsHandler>().All(h => h() != false);
#endif
            }
            return true;
        }

        public bool CanReclaimParcel(UUID user, ILandObject parcel)
        {
            ReclaimParcelHandler handler = OnReclaimParcel;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (ReclaimParcelHandler h in list)
                {
                    if (h(user, parcel, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<ReclaimParcelHandler>().All(h => h(user, parcel, m_scene) != false);
#endif
            }
            return true;
        }

        public bool CanDeedParcel(UUID user, ILandObject parcel)
        {
            DeedParcelHandler handler = OnDeedParcel;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (DeedParcelHandler h in list)
                {
                    if (h(user, parcel, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<DeedParcelHandler>().All(h => h(user, parcel, m_scene) != false);
#endif
            }
            return true;
        }

        public bool CanDeedObject(UUID user, UUID group)
        {
            DeedObjectHandler handler = OnDeedObject;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (DeedObjectHandler h in list)
                {
                    if (h(user, group, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<DeedObjectHandler>().All(h => h(user, group, m_scene) != false);
#endif
            }
            return true;
        }

        public bool CanBuyLand(UUID user, ILandObject parcel)
        {
            BuyLandHandler handler = OnBuyLand;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (BuyLandHandler h in list)
                {
                    if (h(user, parcel, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<BuyLandHandler>().All(h => h(user, parcel, m_scene) != false);
#endif
            }
            return true;
        }

        public bool CanLinkObject(UUID user, UUID objectID)
        {
            LinkObjectHandler handler = OnLinkObject;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (LinkObjectHandler h in list)
                {
                    if (h(user, objectID) == false) return false;
                }
                return true;
#else
                return list.Cast<LinkObjectHandler>().All(h => h(user, objectID) != false);
#endif
            }
            return true;
        }

        public bool CanDelinkObject(UUID user, UUID objectID)
        {
            DelinkObjectHandler handler = OnDelinkObject;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (DelinkObjectHandler h in list)
                {
                    if (h(user, objectID) == false) return false;
                }
                return true;
#else
                return list.Cast<DelinkObjectHandler>().All(h => h(user, objectID) != false);
#endif
            }
            return true;
        }

        public bool IsInGroup(UUID user, UUID groupID)
        {
            DelinkObjectHandler handler = OnIsInGroup;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (DelinkObjectHandler h in list)
                {
                    if (h(user, groupID) == false) return false;
                }
                return true;
#else
                return list.Cast<DelinkObjectHandler>().All(h => h(user, groupID) != false);
#endif
            }
            return true;
        }

        #region REZ OBJECT

        public bool CanRezObject(int objectCount, UUID owner, Vector3 objectPosition, out string reason)
        {
            RezObjectHandler handler = OnRezObject;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
                foreach (RezObjectHandler h in list)
                {
                    if (h(objectCount, owner, objectPosition, m_scene, out reason) == false)
                        return false;
                }
            }
            reason = "";
            return true;
        }

        #endregion

        #region DELETE OBJECT

        public bool CanDeleteObject(UUID objectID, UUID deleter)
        {
            DeleteObjectHandler handler = OnDeleteObject;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (DeleteObjectHandler h in list)
                {
                    if (h(objectID, deleter, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<DeleteObjectHandler>().All(h => h(objectID, deleter, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region TAKE OBJECT

        public bool CanTakeObject(UUID objectID, UUID AvatarTakingUUID)
        {
            TakeObjectHandler handler = OnTakeObject;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (TakeObjectHandler h in list)
                {
                    if (h(objectID, AvatarTakingUUID, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<TakeObjectHandler>().All(h => h(objectID, AvatarTakingUUID, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region TAKE COPY OBJECT

        public bool CanTakeCopyObject(UUID objectID, UUID userID)
        {
            TakeCopyObjectHandler handler = OnTakeCopyObject;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (TakeCopyObjectHandler h in list)
                {
                    if (h(objectID, userID, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<TakeCopyObjectHandler>().All(h => h(objectID, userID, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region DUPLICATE OBJECT

        public bool CanDuplicateObject(int objectCount, UUID objectID, UUID owner, Vector3 objectPosition)
        {
            DuplicateObjectHandler handler = OnDuplicateObject;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (DuplicateObjectHandler h in list)
                {
                    if (h(objectCount, objectID, owner, m_scene, objectPosition) == false) return false;
                }
                return true;
#else
                return list.Cast<DuplicateObjectHandler>().All(h => h(objectCount, objectID, owner, m_scene, objectPosition) != false);
#endif
            }
            return true;
        }

        #endregion

        #region EDIT OBJECT

        public bool CanEditObject(UUID objectID, UUID editorID)
        {
            EditObjectHandler handler = OnEditObject;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (EditObjectHandler h in list)
                {
                    if (h(objectID, editorID, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<EditObjectHandler>().All(h => h(objectID, editorID, m_scene) != false);
#endif
            }
            return true;
        }

        public bool CanEditObjectInventory(UUID objectID, UUID editorID)
        {
            EditObjectInventoryHandler handler = OnEditObjectInventory;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (EditObjectInventoryHandler h in list)
                {
                    if (h(objectID, editorID, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<EditObjectInventoryHandler>().All(h => h(objectID, editorID, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region MOVE OBJECT

        public bool CanMoveObject(UUID objectID, UUID moverID)
        {
            MoveObjectHandler handler = OnMoveObject;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (MoveObjectHandler h in list)
                {
                    if (h(objectID, moverID, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<MoveObjectHandler>().All(h => h(objectID, moverID, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region OBJECT ENTRY

        public bool CanObjectEntry(UUID objectID, bool enteringRegion, Vector3 newPoint, UUID OwnerID)
        {
            ObjectEntryHandler handler = OnObjectEntry;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (ObjectEntryHandler h in list)
                {
                    if (h(objectID, enteringRegion, newPoint, OwnerID) == false) return false;
                }
                return true;
#else
                return list.Cast<ObjectEntryHandler>().All(h => h(objectID, enteringRegion, newPoint, OwnerID) != false);
#endif
            }
            return true;
        }

        #endregion

        #region RETURN OBJECT

        public bool CanReturnObjects(ILandObject land, UUID user, List<ISceneEntity> objects)
        {
            ReturnObjectsHandler handler = OnReturnObjects;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (ReturnObjectsHandler h in list)
                {
                    if (h(land, user, objects, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<ReturnObjectsHandler>().All(h => h(land, user, objects, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region INSTANT MESSAGE

        public bool CanInstantMessage(UUID user, UUID target)
        {
            InstantMessageHandler handler = OnInstantMessage;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (InstantMessageHandler h in list)
                {
                    if (h(user, target, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<InstantMessageHandler>().All(h => h(user, target, m_scene) != false);
#endif
            }
            return true;
        }

        /// <summary>
        ///   Checks whether the user is in god mode
        /// </summary>
        /// <param name = "user"></param>
        /// <returns></returns>
        public bool CanGodTeleport(UUID user, UUID target)
        {
            CanGodTpHandler handler = OnCanGodTp;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (CanGodTpHandler h in list)
                {
                    if (h(user, target) == false) return false;
                }
                return true;
#else
                return list.Cast<CanGodTpHandler>().All(h => h(user, target) != false);
#endif
            }
            return true;
        }

        #endregion

        #region INVENTORY TRANSFER

        public bool CanInventoryTransfer(UUID user, UUID target)
        {
            InventoryTransferHandler handler = OnInventoryTransfer;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (InventoryTransferHandler h in list)
                {
                    if (h(user, target, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<InventoryTransferHandler>().All(h => h(user, target, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region VIEW SCRIPT

        public bool CanViewScript(UUID script, UUID objectID, UUID user)
        {
            ViewScriptHandler handler = OnViewScript;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (ViewScriptHandler h in list)
                {
                    if (h(script, objectID, user, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<ViewScriptHandler>().All(h => h(script, objectID, user, m_scene) != false);
#endif
            }
            return true;
        }

        public bool CanViewNotecard(UUID script, UUID objectID, UUID user)
        {
            ViewNotecardHandler handler = OnViewNotecard;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (ViewNotecardHandler h in list)
                {
                    if (h(script, objectID, user, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<ViewNotecardHandler>().All(h => h(script, objectID, user, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region EDIT SCRIPT

        public bool CanEditScript(UUID script, UUID objectID, UUID user)
        {
            EditScriptHandler handler = OnEditScript;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (EditScriptHandler h in list)
                {
                    if (h(script, objectID, user, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<EditScriptHandler>().All(h => h(script, objectID, user, m_scene) != false);
#endif
            }
            return true;
        }

        public bool CanEditNotecard(UUID script, UUID objectID, UUID user)
        {
            EditNotecardHandler handler = OnEditNotecard;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (EditNotecardHandler h in list)
                {
                    if (h(script, objectID, user, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<EditNotecardHandler>().All(h => h(script, objectID, user, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region RUN SCRIPT (When Script Placed in Object)

        public bool CanRunScript(UUID script, UUID objectID, UUID user)
        {
            RunScriptHandler handler = OnRunScript;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (RunScriptHandler h in list)
                {
                    if (h(script, objectID, user, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<RunScriptHandler>().All(h => h(script, objectID, user, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region COMPILE SCRIPT (When Script needs to get (re)compiled)

        public bool CanCompileScript(UUID ownerUUID, string scriptType)
        {
            CompileScriptHandler handler = OnCompileScript;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (CompileScriptHandler h in list)
                {
                    if (h(ownerUUID, scriptType, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<CompileScriptHandler>().All(h => h(ownerUUID, scriptType, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region START SCRIPT (When Script run box is Checked after placed in object)

        public bool CanStartScript(UUID script, UUID user)
        {
            StartScriptHandler handler = OnStartScript;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (StartScriptHandler h in list)
                {
                    if (h(script, user, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<StartScriptHandler>().All(h => h(script, user, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region STOP SCRIPT (When Script run box is unchecked after placed in object)

        public bool CanStopScript(UUID script, UUID user)
        {
            StopScriptHandler handler = OnStopScript;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (StopScriptHandler h in list)
                {
                    if (h(script, user, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<StopScriptHandler>().All(h => h(script, user, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region RESET SCRIPT

        public bool CanResetScript(UUID prim, UUID script, UUID user)
        {
            ResetScriptHandler handler = OnResetScript;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (ResetScriptHandler h in list)
                {
                    if (h(prim, script, user, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<ResetScriptHandler>().All(h => h(prim, script, user, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region TERRAFORM LAND

        public bool CanTerraformLand(UUID user, Vector3 pos)
        {
            TerraformLandHandler handler = OnTerraformLand;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (TerraformLandHandler h in list)
                {
                    if (h(user, pos, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<TerraformLandHandler>().All(h => h(user, pos, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region RUN CONSOLE COMMAND

        public bool CanRunConsoleCommand(UUID user)
        {
            RunConsoleCommandHandler handler = OnRunConsoleCommand;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (RunConsoleCommandHandler h in list)
                {
                    if (h(user, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<RunConsoleCommandHandler>().All(h => h(user, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region CAN ISSUE ESTATE COMMAND

        public bool CanIssueEstateCommand(UUID user, bool ownerCommand)
        {
            IssueEstateCommandHandler handler = OnIssueEstateCommand;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (IssueEstateCommandHandler h in list)
                {
                    if (h(user, m_scene, ownerCommand) == false) return false;
                }
                return true;
#else
                return list.Cast<IssueEstateCommandHandler>().All(h => h(user, m_scene, ownerCommand) != false);
#endif
            }
            return true;
        }

        #endregion

        #region CAN BE GODLIKE

        /// <summary>
        ///   Checks whether the user is in god mode
        /// </summary>
        /// <param name = "user"></param>
        /// <returns></returns>
        public bool IsGod(UUID user)
        {
            IsGodHandler handler = OnIsGod;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (IsGodHandler h in list)
                {
                    if (h(user, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<IsGodHandler>().All(h => h(user, m_scene) != false);
#endif
            }
            return true;
        }

        /// <summary>
        ///   Checks whether the user can be in god mode
        /// </summary>
        /// <param name = "user"></param>
        /// <returns></returns>
        public bool IsAdministrator(UUID user)
        {
            IsAdministratorHandler handler = OnIsAdministrator;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (IsAdministratorHandler h in list)
                {
                    if (h(user) == false) return false;
                }
                return true;
#else
                return list.Cast<IsAdministratorHandler>().All(h => h(user) != false);
#endif
            }
            return true;
        }

        #endregion

        #region EDIT PARCEL

        public bool CanEditParcelProperties(UUID user, ILandObject parcel, GroupPowers groupPowers)
        {
            EditParcelPropertiesHandler handler = OnEditParcelProperties;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (EditParcelPropertiesHandler h in list)
                {
                    if (h(user, parcel, groupPowers, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<EditParcelPropertiesHandler>().All(h => h(user, parcel, groupPowers, m_scene) != false);
#endif
            }
            return true;
        }

        public bool CanEditParcel(UUID user, ILandObject parcel)
        {
            EditParcelHandler handler = OnEditParcel;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (EditParcelHandler h in list)
                {
                    if (h(user, parcel, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<EditParcelHandler>().All(h => h(user, parcel, m_scene) != false);
#endif
            }
            return true;
        }

        public bool CanSubdivideParcel(UUID user, ILandObject parcel)
        {
            EditParcelHandler handler = OnSubdivideParcel;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (EditParcelHandler h in list)
                {
                    if (h(user, parcel, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<EditParcelHandler>().All(h => h(user, parcel, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region SELL PARCEL

        public bool CanSellParcel(UUID user, ILandObject parcel)
        {
            SellParcelHandler handler = OnSellParcel;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (SellParcelHandler h in list)
                {
                    if (h(user, parcel, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<SellParcelHandler>().All(h => h(user, parcel, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #region ABANDON PARCEL

        public bool CanAbandonParcel(UUID user, ILandObject parcel)
        {
            AbandonParcelHandler handler = OnAbandonParcel;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (AbandonParcelHandler h in list)
                {
                    if (h(user, parcel, m_scene) == false) return false;
                }
                return true;
#else
                return list.Cast<AbandonParcelHandler>().All(h => h(user, parcel, m_scene) != false);
#endif
            }
            return true;
        }

        #endregion

        #endregion

        /// Check whether the specified user is allowed to directly create the given inventory type in a prim's
        /// inventory (e.g. the New Script button in the 1.21 Linden Lab client).
        /// </summary>
        /// <param name = "invType"></param>
        /// <param name = "objectID"></param>
        /// <param name = "userID"></param>
        /// <returns></returns>
        public bool CanCreateObjectInventory(int invType, UUID objectID, UUID userID)
        {
            CreateObjectInventoryHandler handler = OnCreateObjectInventory;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (CreateObjectInventoryHandler h in list)
                {
                    if (h(invType, objectID, userID) == false) return false;
                }
                return true;
#else
                return list.Cast<CreateObjectInventoryHandler>().All(h => h(invType, objectID, userID) != false);
#endif
            }
            return true;
        }

        public bool CanCopyObjectInventory(UUID itemID, UUID objectID, UUID userID)
        {
            CopyObjectInventoryHandler handler = OnCopyObjectInventory;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (CopyObjectInventoryHandler h in list)
                {
                    if (h(itemID, objectID, userID) == false) return false;
                }
                return true;
#else
                return list.Cast<CopyObjectInventoryHandler>().All(h => h(itemID, objectID, userID) != false);
#endif
            }
            return true;
        }

        public bool CanDeleteObjectInventory(UUID itemID, UUID objectID, UUID userID)
        {
            DeleteObjectInventoryHandler handler = OnDeleteObjectInventory;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (DeleteObjectInventoryHandler h in list)
                {
                    if (h(itemID, objectID, userID) == false) return false;
                }
                return true;
#else
                return list.Cast<DeleteObjectInventoryHandler>().All(h => h(itemID, objectID, userID) != false);
#endif
            }
            return true;
        }

        /// <summary>
        ///   Check whether the specified user is allowed to create the given inventory type in their inventory.
        /// </summary>
        /// <param name = "invType"></param>
        /// <param name = "userID"></param>
        /// <returns></returns>
        public bool CanCreateUserInventory(int invType, UUID userID)
        {
            CreateUserInventoryHandler handler = OnCreateUserInventory;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (CreateUserInventoryHandler h in list)
                {
                    if (h(invType, userID) == false) return false;
                }
                return true;
#else
                return list.Cast<CreateUserInventoryHandler>().All(h => h(invType, userID) != false);
#endif
            }
            return true;
        }

        /// <summary>
        ///   Check whether the specified user is allowed to edit the given inventory item within their own inventory.
        /// </summary>
        /// <param name = "itemID"></param>
        /// <param name = "userID"></param>
        /// <returns></returns>
        public bool CanEditUserInventory(UUID itemID, UUID userID)
        {
            EditUserInventoryHandler handler = OnEditUserInventory;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (EditUserInventoryHandler h in list)
                {
                    if (h(itemID, userID) == false) return false;
                }
                return true;
#else
                return list.Cast<EditUserInventoryHandler>().All(h => h(itemID, userID) != false);
#endif
            }
            return true;
        }

        /// <summary>
        ///   Check whether the specified user is allowed to copy the given inventory item from their own inventory.
        /// </summary>
        /// <param name = "itemID"></param>
        /// <param name = "userID"></param>
        /// <returns></returns>
        public bool CanCopyUserInventory(UUID itemID, UUID userID)
        {
            CopyUserInventoryHandler handler = OnCopyUserInventory;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (CopyUserInventoryHandler h in list)
                {
                    if (h(itemID, userID) == false) return false;
                }
                return true;
#else
                return list.Cast<CopyUserInventoryHandler>().All(h => h(itemID, userID) != false);
#endif
            }
            return true;
        }

        /// <summary>
        ///   Check whether the specified user is allowed to edit the given inventory item within their own inventory.
        /// </summary>
        /// <param name = "itemID"></param>
        /// <param name = "userID"></param>
        /// <returns></returns>
        public bool CanDeleteUserInventory(UUID itemID, UUID userID)
        {
            DeleteUserInventoryHandler handler = OnDeleteUserInventory;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (DeleteUserInventoryHandler h in list)
                {
                    if (h(itemID, userID) == false) return false;
                }
                return true;
#else
                return list.Cast<DeleteUserInventoryHandler>().All(h => h(itemID, userID) != false);
#endif
            }
            return true;
        }

        /// <summary>
        ///   Check to make sure the user is allowed to teleport within this region
        /// </summary>
        /// <param name = "userID">The user that is attempting to leave</param>
        /// <param name = "reason">If this check fails, this is the reason why</param>
        /// <returns>Whether the user is allowed to teleport locally</returns>
        public bool AllowedOutgoingLocalTeleport(UUID userID, out string reason)
        {
            reason = "";
            OutgoingRemoteTeleport handler = OnAllowedOutgoingLocalTeleport;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
                foreach (OutgoingRemoteTeleport h in list)
                {
                    if (h(userID, m_scene, out reason) == false)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        ///   Check to make sure the user can be teleporting out of the region to a remote region.
        ///   If this is false, the user is denied the ability to leave the region at all.
        /// </summary>
        /// <param name = "userID">The user that is attempting to leave the region</param>
        /// <param name = "reason">If this fails, this explains why it failed</param>
        /// <returns>Whether the user is allowed to teleport to remote regions</returns>
        public bool AllowedOutgoingRemoteTeleport(UUID userID, out string reason)
        {
            reason = "";
            OutgoingRemoteTeleport handler = OnAllowedOutgoingRemoteTeleport;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
                foreach (OutgoingRemoteTeleport h in list)
                {
                    if (h(userID, m_scene, out reason) == false)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        ///   Check to make sure this user has the ability to have an agent in this region.
        ///   This checks whether they exist in the grid, whether they are banned from the region and more.
        ///   It is called by the SimulationService in CreateAgent mainly.
        /// </summary>
        /// <param name = "agent">The Agent that is coming in</param>
        /// <param name = "isRootAgent">Whether this agent will be a root agent</param>
        /// <param name = "reason">If it fails, this explains why they cannot enter</param>
        /// <returns>Whether this user is allowed to have an agent in this region</returns>
        public bool AllowedIncomingAgent(AgentCircuitData agent, bool isRootAgent, out string reason)
        {
            IncomingAgentHandler handler = OnAllowIncomingAgent;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
                foreach (IncomingAgentHandler h in list)
                {
                    if (h(m_scene, agent, isRootAgent, out reason) == false)
                        return false;
                }
            }
            reason = "";
            return true;
        }

        /// <summary>
        ///   Check to see whether the user is actually in this region 
        ///   and then figure out if they can be where they want to be
        /// </summary>
        /// <param name = "userID">The user who is teleporting (can be either incoming from a remote region, or a local teleport)</param>
        /// <param name = "Position">The position the user has requested</param>
        /// <param name = "newPosition">The position the user is going to get</param>
        /// <param name = "reason">If the check fails, this will tell why</param>
        /// <returns>Whether this user can teleport into/around this region</returns>
        public bool AllowedIncomingTeleport(UUID userID, Vector3 Position, uint TeleportFlags, out Vector3 newPosition,
                                            out string reason)
        {
            newPosition = Position;
            reason = "";
            TeleportHandler handler = OnAllowedIncomingTeleport;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
                foreach (TeleportHandler h in list)
                {
                    if (h(userID, m_scene, Position, TeleportFlags, out newPosition, out reason) == false)
                        return false;
                }
            }
            return true;
        }

        public bool CanPushObject(UUID uUID, ILandObject targetlandObj)
        {
            PushObjectHandler handler = OnPushObject;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (PushObjectHandler h in list)
                {
                    if (h(uUID, targetlandObj) == false) return false;
                }
                return true;
#else
                return list.Cast<PushObjectHandler>().All(h => h(uUID, targetlandObj) != false);
#endif
            }
            return true;
        }

        public bool CanViewObjectOwners(UUID uUID, ILandObject targetlandObj)
        {
            PushObjectHandler handler = OnViewObjectOwners;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (PushObjectHandler h in list)
                {
                    if (h(uUID, targetlandObj) == false) return false;
                }
                return true;
#else
                return list.Cast<PushObjectHandler>().All(h => h(uUID, targetlandObj) != false);
#endif
            }
            return true;
        }

        public bool CanEditParcelAccessList(UUID uUID, ILandObject land, uint flags)
        {
            EditParcelAccessListHandler handler = OnEditParcelAccessList;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (EditParcelAccessListHandler h in list)
                {
                    if (h(uUID, land, flags) == false) return false;
                }
                return true;
#else
                return list.Cast<EditParcelAccessListHandler>().All(h => h(uUID, land, flags) != false);
#endif
            }
            return true;
        }

        public bool GenericParcelPermission(UUID user, ILandObject parcel, ulong groupPowers)
        {
            GenericParcelHandler handler = OnGenericParcelHandler;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (GenericParcelHandler h in list)
                {
                    if (h(user, parcel, groupPowers) == false) return false;
                }
                return true;
#else
                return list.Cast<GenericParcelHandler>().All(h => h(user, parcel, groupPowers) != false);
#endif
            }
            return true;
        }

        public bool CanTakeLandmark(UUID user)
        {
            TakeLandmark handler = OnTakeLandmark;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (TakeLandmark h in list)
                {
                    if (h(user) == false) return false;
                }
                return true;
#else
                return list.Cast<TakeLandmark>().All(h => h(user) != false);
#endif
            }
            return true;
        }

        public bool CanSetHome(UUID userID)
        {
            TakeLandmark handler = OnSetHomePoint;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                return list.Cast<TakeLandmark>().All(h => h(userID) != false);
#else
                return list.Cast<TakeLandmark>().All(h => h(userID) != false);
#endif
            }
            return true;
        }

        public bool CanControlPrimMedia(UUID userID, UUID primID, int face)
        {
            ControlPrimMediaHandler handler = OnControlPrimMedia;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (ControlPrimMediaHandler h in list)
                {
                    if (h(userID, primID, face) == false) return false;
                }
                return true;
#else
                return list.Cast<ControlPrimMediaHandler>().All(h => h(userID, primID, face) != false);
#endif
            }
            return true;
        }

        public bool CanInteractWithPrimMedia(UUID userID, UUID primID, int face)
        {
            InteractWithPrimMediaHandler handler = OnInteractWithPrimMedia;
            if (handler != null)
            {
                Delegate[] list = handler.GetInvocationList();
#if (!ISWIN)
                foreach (InteractWithPrimMediaHandler h in list)
                {
                    if (h(userID, primID, face) == false) return false;
                }
                return true;
#else
                return list.Cast<InteractWithPrimMediaHandler>().All(h => h(userID, primID, face) != false);
#endif
            }
            return true;
        }
    }
}