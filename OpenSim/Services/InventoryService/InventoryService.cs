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
using Aurora.DataManager;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.InventoryService
{
    public class InventoryService : ConnectorBase, IInventoryService, IService
    {
        #region Declares

        protected bool m_AllowDelete = true;

        protected IAssetService m_AssetService;
        protected IInventoryData m_Database;
        protected ILibraryService m_LibraryService;
        protected IUserAccountService m_UserAccountService;
        protected Dictionary<UUID, InventoryItemBase> _tempItemCache = new Dictionary<UUID, InventoryItemBase>();

        #endregion

        #region IService Members

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("InventoryHandler", "") != Name)
                return;

            IConfig invConfig = config.Configs["InventoryService"];
            if (invConfig != null)
                m_AllowDelete = invConfig.GetBoolean("AllowDelete", true);

            if (MainConsole.Instance != null)
                MainConsole.Instance.Commands.AddCommand("fix inventory", "fix inventory",
                                                         "If the user's inventory has been corrupted, this function will attempt to fix it",
                                                         FixInventory);
            registry.RegisterModuleInterface<IInventoryService>(this);
            Init(registry, Name);
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
            m_Database = DataManager.RequestPlugin<IInventoryData>();
            m_UserAccountService = registry.RequestModuleInterface<IUserAccountService>();
            m_LibraryService = registry.RequestModuleInterface<ILibraryService>();
            m_AssetService = registry.RequestModuleInterface<IAssetService>();

            registry.RequestModuleInterface<ISimulationBase>().EventManager.RegisterEventHandler("DeleteUserInformation", DeleteUserInformation);
        }

        public virtual void FinishedStartup()
        {
            _addInventoryItemQueue.Start(0.5, (agentID, itemsToAdd) =>
                {
                    foreach (AddInventoryItemStore item in itemsToAdd)
                    {
                        AddItem(item.Item);
                        _tempItemCache.Remove(item.Item.ID);
                        if(item.Complete != null)
                            item.Complete();
                    }
                });
            _moveInventoryItemQueue.Start(0.5, (agentID, itemsToMove) =>
                {
                    foreach (var item in itemsToMove)
                    {
                        MoveItems(agentID, item.Items);
                        if (item.Complete != null)
                            item.Complete();
                    }
                });
        }

        #endregion

        #region IInventoryService Members

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public virtual bool CreateUserInventory(UUID principalID, bool createDefaultItems)
        {
            object remoteValue = DoRemote(principalID, createDefaultItems);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            List<InventoryItemBase> items;
            return CreateUserInventory(principalID, createDefaultItems, out items);
        }

        public virtual bool CreateUserInventory(UUID principalID, bool createDefaultItems, out List<InventoryItemBase> defaultItems)
        {
            // This is braindeaad. We can't ever communicate that we fixed
            // an existing inventory. Well, just return root folder status,
            // but check sanity anyway.
            //
            bool result = false;

            InventoryFolderBase rootFolder = GetRootFolder(principalID);

            if (rootFolder == null)
            {
                rootFolder = CreateFolder(principalID, UUID.Zero, (int)AssetType.RootFolder, "My Inventory");
                result = true;
            }

            InventoryFolderBase[] sysFolders = GetSystemFolders(principalID);

            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.Animation) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Animation, "Animations");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.Bodypart) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Bodypart, "Body Parts");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.CallingCard) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.CallingCard, "Calling Cards");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.Clothing) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Clothing, "Clothing");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.Gesture) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Gesture, "Gestures");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.Landmark) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Landmark, "Landmarks");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.LostAndFoundFolder) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.LostAndFoundFolder, "Lost And Found");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.Notecard) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Notecard, "Notecards");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.Object) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Object, "Objects");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.SnapshotFolder) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.SnapshotFolder, "Photo Album");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.LSLText) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.LSLText, "Scripts");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.Sound) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Sound, "Sounds");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.Texture) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Texture, "Textures");
            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.TrashFolder) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.TrashFolder, "Trash");

            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.Mesh) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Mesh, "Mesh");

            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.Inbox) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Inbox, "Received Items");

            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.Outbox) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Outbox, "Merchant Outbox");

            if (!Array.Exists(sysFolders, delegate(InventoryFolderBase f)
            {
                if (f.Type == (short)AssetType.CurrentOutfitFolder) return true;
                return false;
            }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.CurrentOutfitFolder, "Current Outfit");

            if (createDefaultItems && m_LibraryService != null)
            {
                defaultItems = new List<InventoryItemBase>();
                InventoryFolderBase bodypartFolder = GetFolderForType(principalID, InventoryType.Unknown,
                                                                      AssetType.Bodypart);
                InventoryFolderBase clothingFolder = GetFolderForType(principalID, InventoryType.Unknown,
                                                                      AssetType.Clothing);

                // Default items
                InventoryItemBase defaultShape = new InventoryItemBase
                {
                    Name = "Default shape",
                    Description = "Default shape description",
                    AssetType = (int)AssetType.Bodypart,
                    InvType = (int)InventoryType.Wearable,
                    Flags = (uint)WearableType.Shape,
                    ID = UUID.Random()
                };
                //Give a new copy to every person
                AssetBase asset = m_AssetService.Get(AvatarWearable.DEFAULT_BODY_ASSET.ToString());
                if (asset != null)
                {
                    asset.ID = UUID.Random();
                    asset.ID = m_AssetService.Store(asset);
                    defaultShape.AssetID = asset.ID;
                    defaultShape.Folder = bodypartFolder.ID;
                    defaultShape.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultShape.Owner = principalID;
                    defaultShape.BasePermissions = (uint)PermissionMask.All;
                    defaultShape.CurrentPermissions = (uint)PermissionMask.All;
                    defaultShape.EveryOnePermissions = (uint)PermissionMask.None;
                    defaultShape.NextPermissions = (uint)PermissionMask.All;
                    AddItem(defaultShape, false);
                    defaultItems.Add(defaultShape);
                }

                InventoryItemBase defaultSkin = new InventoryItemBase
                {
                    Name = "Default skin",
                    Description = "Default skin description",
                    AssetType = (int)AssetType.Bodypart,
                    InvType = (int)InventoryType.Wearable,
                    Flags = (uint)WearableType.Skin,
                    ID = UUID.Random()
                };
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_SKIN_ASSET.ToString());
                if (asset != null)
                {
                    asset.ID = UUID.Random();
                    asset.ID = m_AssetService.Store(asset);
                    defaultSkin.AssetID = asset.ID;
                    defaultSkin.Folder = bodypartFolder.ID;
                    defaultSkin.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultSkin.Owner = principalID;
                    defaultSkin.BasePermissions = (uint)PermissionMask.All;
                    defaultSkin.CurrentPermissions = (uint)PermissionMask.All;
                    defaultSkin.EveryOnePermissions = (uint)PermissionMask.None;
                    defaultSkin.NextPermissions = (uint)PermissionMask.All;
                    AddItem(defaultSkin, false);
                    defaultItems.Add(defaultSkin);
                }

                InventoryItemBase defaultHair = new InventoryItemBase
                {
                    Name = "Default hair",
                    Description = "Default hair description",
                    AssetType = (int)AssetType.Bodypart,
                    InvType = (int)InventoryType.Wearable,
                    Flags = (uint)WearableType.Hair,
                    ID = UUID.Random()
                };
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_HAIR_ASSET.ToString());
                if (asset != null)
                {
                    asset.ID = UUID.Random();
                    asset.ID = m_AssetService.Store(asset);
                    defaultHair.AssetID = asset.ID;
                    defaultHair.Folder = bodypartFolder.ID;
                    defaultHair.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultHair.Owner = principalID;
                    defaultHair.BasePermissions = (uint)PermissionMask.All;
                    defaultHair.CurrentPermissions = (uint)PermissionMask.All;
                    defaultHair.EveryOnePermissions = (uint)PermissionMask.None;
                    defaultHair.NextPermissions = (uint)PermissionMask.All;
                    AddItem(defaultHair, false);
                    defaultItems.Add(defaultHair);
                }

                InventoryItemBase defaultEyes = new InventoryItemBase
                {
                    Name = "Default eyes",
                    Description = "Default eyes description",
                    AssetType = (int)AssetType.Bodypart,
                    InvType = (int)InventoryType.Wearable,
                    Flags = (uint)WearableType.Eyes,
                    ID = UUID.Random()
                };
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_EYES_ASSET.ToString());
                if (asset != null)
                {
                    asset.ID = UUID.Random();
                    asset.ID = m_AssetService.Store(asset);
                    defaultEyes.AssetID = asset.ID;
                    defaultEyes.Folder = bodypartFolder.ID;
                    defaultEyes.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultEyes.Owner = principalID;
                    defaultEyes.BasePermissions = (uint)PermissionMask.All;
                    defaultEyes.CurrentPermissions = (uint)PermissionMask.All;
                    defaultEyes.EveryOnePermissions = (uint)PermissionMask.None;
                    defaultEyes.NextPermissions = (uint)PermissionMask.All;
                    AddItem(defaultEyes, false);
                    defaultItems.Add(defaultEyes);
                }

                InventoryItemBase defaultShirt = new InventoryItemBase
                {
                    Name = "Default shirt",
                    Description = "Default shirt description",
                    AssetType = (int)AssetType.Clothing,
                    InvType = (int)InventoryType.Wearable,
                    Flags = (uint)WearableType.Shirt,
                    ID = UUID.Random()
                };
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_SHIRT_ASSET.ToString());
                if (asset != null)
                {
                    OpenMetaverse.Assets.AssetClothing clothing = new OpenMetaverse.Assets.AssetClothing()
                    {
                        Creator = m_LibraryService.LibraryOwner,
                        Name = "Default shirt",
                        Owner = principalID,
                        Permissions = new Permissions((uint)PermissionMask.All, (uint)PermissionMask.All, (uint)PermissionMask.All, (uint)PermissionMask.All, (uint)PermissionMask.All),
                        WearableType = WearableType.Shirt,
                        Textures = new Dictionary<AvatarTextureIndex,UUID>() { { AvatarTextureIndex.UpperShirt, UUID.Parse("5748decc-f629-461c-9a36-a35a221fe21f") } }
                    };
                    clothing.Encode();
                    asset.Data = clothing.AssetData;
                    asset.ID = UUID.Random();
                    asset.ID = m_AssetService.Store(asset);
                    defaultShirt.AssetID = asset.ID;
                    defaultShirt.Folder = clothingFolder.ID;
                    defaultShirt.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultShirt.Owner = principalID;
                    defaultShirt.BasePermissions = (uint)PermissionMask.All;
                    defaultShirt.CurrentPermissions = (uint)PermissionMask.All;
                    defaultShirt.EveryOnePermissions = (uint)PermissionMask.None;
                    defaultShirt.NextPermissions = (uint)PermissionMask.All;
                    AddItem(defaultShirt, false);
                    defaultItems.Add(defaultShirt);
                }

                InventoryItemBase defaultPants = new InventoryItemBase
                {
                    Name = "Default pants",
                    Description = "Default pants description",
                    AssetType = (int)AssetType.Clothing,
                    InvType = (int)InventoryType.Wearable,
                    Flags = (uint)WearableType.Pants,
                    ID = UUID.Random()
                };
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_PANTS_ASSET.ToString());
                if (asset != null)
                {
                    OpenMetaverse.Assets.AssetClothing clothing = new OpenMetaverse.Assets.AssetClothing()
                    {
                        Creator = m_LibraryService.LibraryOwner,
                        Name = "Default pants",
                        Owner = principalID,
                        Permissions = new Permissions((uint)PermissionMask.All, (uint)PermissionMask.All, (uint)PermissionMask.All, (uint)PermissionMask.All, (uint)PermissionMask.All),
                        WearableType = WearableType.Pants,
                        Textures = new Dictionary<AvatarTextureIndex, UUID>() { { AvatarTextureIndex.LowerPants, UUID.Parse("5748decc-f629-461c-9a36-a35a221fe21f") } }
                    };
                    clothing.Encode();
                    asset.Data = clothing.AssetData;
                    asset.ID = UUID.Random();
                    asset.ID = m_AssetService.Store(asset);
                    defaultPants.AssetID = asset.ID;
                    defaultPants.Folder = clothingFolder.ID;
                    defaultPants.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultPants.Owner = principalID;
                    defaultPants.BasePermissions = (uint)PermissionMask.All;
                    defaultPants.CurrentPermissions = (uint)PermissionMask.All;
                    defaultPants.EveryOnePermissions = (uint)PermissionMask.None;
                    defaultPants.NextPermissions = (uint)PermissionMask.All;
                    AddItem(defaultPants, false);
                    defaultItems.Add(defaultPants);
                }
            }
            else
                defaultItems = new List<InventoryItemBase>();

            return result;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public virtual List<InventoryFolderBase> GetInventorySkeleton(UUID principalID)
        {
            object remoteValue = DoRemote(principalID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<InventoryFolderBase>)remoteValue;

            List<InventoryFolderBase> allFolders = m_Database.GetFolders(
                new[] { "agentID" },
                new[] { principalID.ToString() });

            if (allFolders.Count == 0)
                return null;

            return allFolders;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public virtual List<InventoryFolderBase> GetRootFolders(UUID principalID)
        {
            object remoteValue = DoRemote(principalID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<InventoryFolderBase>)remoteValue;

            return m_Database.GetFolders(
                new[] { "agentID", "parentFolderID" },
                new[] { principalID.ToString(), UUID.Zero.ToString() });
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Medium)]
        public virtual InventoryFolderBase GetRootFolder(UUID principalID)
        {
            object remoteValue = DoRemote(principalID);
            if (remoteValue != null || m_doRemoteOnly)
                return (InventoryFolderBase)remoteValue;

            List<InventoryFolderBase> folders = m_Database.GetFolders(
                new[] { "agentID", "parentFolderID" },
                new[] { principalID.ToString(), UUID.Zero.ToString() });

            if (folders.Count == 0)
                return null;

            InventoryFolderBase root = null;
#if (!ISWIN)
            foreach (InventoryFolderBase folder in folders)
            {
                if (folder.Name == "My Inventory") root = folder;
            }
#else
            foreach (InventoryFolderBase folder in folders.Where(folder => folder.Name == "My Inventory"))
                root = folder;
#endif
            if (folders == null) // oops
                root = folders[0];

            return root;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual InventoryFolderBase GetFolderForType(UUID principalID, InventoryType invType, AssetType type)
        {
            object remoteValue = DoRemote(principalID, invType, type);
            if (remoteValue != null || m_doRemoteOnly)
                return (InventoryFolderBase)remoteValue;

            if (invType == InventoryType.Snapshot)
                type = AssetType.SnapshotFolder;
            //Fix for snapshots, as they get the texture asset type, but need to get checked as snapshotfolder types

            List<InventoryFolderBase> folders = m_Database.GetFolders(
                new[] { "agentID", "type" },
                new[] { principalID.ToString(), ((int)type).ToString() });

            if (folders.Count == 0)
            {
                //                MainConsole.Instance.WarnFormat("[XINVENTORY SERVICE]: Found no folder for type {0} for user {1}", type, principalID);
                return null;
            }

            //            MainConsole.Instance.DebugFormat(
            //                "[XINVENTORY SERVICE]: Found folder {0} {1} for type {2} for user {3}", 
            //                folders[0].folderName, folders[0].folderID, type, principalID);

            return folders[0];
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.High, OnlyCallableIfUserInRegion = true)]
        public virtual InventoryCollection GetFolderContent(UUID UserID, UUID folderID)
        {
            object remoteValue = DoRemote(UserID, folderID);
            if (remoteValue != null || m_doRemoteOnly)
                return (InventoryCollection)remoteValue;

            // This method doesn't receive a valud principal id from the
            // connector. So we disregard the principal and look
            // by ID.
            //
            MainConsole.Instance.DebugFormat("[XINVENTORY SERVICE]: Fetch contents for folder {0}", folderID.ToString());
            InventoryCollection inventory = new InventoryCollection
            {
                UserID = UserID,
                FolderID = folderID,
                Folders = m_Database.GetFolders(
                    new[] { "parentFolderID" },
                    new[] { folderID.ToString() }),
                Items = m_Database.GetItems(
                    new[] { "parentFolderID" },
                    new[] { folderID.ToString() })
            };



            return inventory;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public virtual List<InventoryItemBase> GetFolderItems(UUID principalID, UUID folderID)
        {
            object remoteValue = DoRemote(principalID, folderID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<InventoryItemBase>)remoteValue;

            if (principalID != UUID.Zero)
                return m_Database.GetItems(
                    new[] { "parentFolderID", "avatarID" },
                    new[] { folderID.ToString(), principalID.ToString() });
            return m_Database.GetItems(
                new[] { "parentFolderID" },
                new[] { folderID.ToString() });
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public virtual OSDArray GetLLSDFolderItems(UUID principalID, UUID folderID)
        {
            object remoteValue = DoRemote(principalID, folderID);
            if (remoteValue != null || m_doRemoteOnly)
                return (OSDArray)remoteValue;

            // Since we probably don't get a valid principal here, either ...
            //
            return m_Database.GetLLSDItems(
                new[] { "parentFolderID", "avatarID" },
                new[] { folderID.ToString(), principalID.ToString() });
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual List<InventoryFolderBase> GetFolderFolders(UUID principalID, UUID folderID)
        {
            object remoteValue = DoRemote(principalID, folderID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<InventoryFolderBase>)remoteValue;

            // Since we probably don't get a valid principal here, either ...
            //
            List<InventoryFolderBase> invItems = m_Database.GetFolders(
                new[] { "parentFolderID" },
                new[] { folderID.ToString() });

            return invItems;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual bool AddFolder(InventoryFolderBase folder)
        {
            object remoteValue = DoRemote(folder);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            InventoryFolderBase check = GetFolder(folder);
            if (check != null)
                return false;

            return m_Database.StoreFolder(folder);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual bool UpdateFolder(InventoryFolderBase folder)
        {
            object remoteValue = DoRemote(folder);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            if (!m_AllowDelete) //Initial item MUST be created as a link folder
                if (folder.Type == (sbyte)AssetType.LinkFolder)
                    return false;

            InventoryFolderBase check = GetFolder(folder);
            if (check == null)
                return AddFolder(folder);

            if (check.Type != -1 || folder.Type != -1)
            {
                if (folder.Version > check.Version)
                    return false;
                check.Version = folder.Version;
                check.Type = folder.Type;
                check.Version++;
                return m_Database.StoreFolder(check);
            }

            if (folder.Version < check.Version)
                folder.Version = check.Version;
            folder.ID = check.ID;

            folder.Version++;
            return m_Database.StoreFolder(folder);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual bool MoveFolder(InventoryFolderBase folder)
        {
            object remoteValue = DoRemote(folder);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            List<InventoryFolderBase> x = m_Database.GetFolders(
                new[] { "folderID" },
                new[] { folder.ID.ToString() });

            if (x.Count == 0)
                return false;

            x[0].ParentID = folder.ParentID;

            return m_Database.StoreFolder(x[0]);
        }

        // We don't check the principal's ID here
        //
        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.High)]
        public virtual bool DeleteFolders(UUID principalID, List<UUID> folderIDs)
        {
            object remoteValue = DoRemote(principalID, folderIDs);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            if (!m_AllowDelete)
            {
                foreach (UUID id in folderIDs)
                {
                    if (!ParentIsLinkFolder(id))
                        continue;
                    InventoryFolderBase f = new InventoryFolderBase { ID = id };
                    PurgeFolder(f);
                    m_Database.DeleteFolders("folderID", id.ToString(), true);
                }
                return true;
            }

            // Ignore principal ID, it's bogus at connector level
            //
            foreach (UUID id in folderIDs)
            {
                if (!ParentIsTrash(id))
                    continue;
                InventoryFolderBase f = new InventoryFolderBase { ID = id };
                PurgeFolder(f);
                m_Database.DeleteFolders("folderID", id.ToString(), true);
            }

            return true;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.High)]
        public virtual bool PurgeFolder(InventoryFolderBase folder)
        {
            object remoteValue = DoRemote(folder);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            if (!m_AllowDelete && !ParentIsLinkFolder(folder.ID))
                return false;

            if (!ParentIsTrash(folder.ID))
                return false;

            List<InventoryFolderBase> subFolders = m_Database.GetFolders(
                new[] { "parentFolderID" },
                new[] { folder.ID.ToString() });

            foreach (InventoryFolderBase x in subFolders)
            {
                PurgeFolder(x);
                m_Database.DeleteFolders("folderID", x.ID.ToString(), true);
            }

            m_Database.DeleteItems("parentFolderID", folder.ID.ToString());

            return true;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public virtual bool ForcePurgeFolder(InventoryFolderBase folder)
        {
            object remoteValue = DoRemote(folder);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            List<InventoryFolderBase> subFolders = m_Database.GetFolders(
                new[] { "parentFolderID" },
                new[] { folder.ID.ToString() });

            foreach (InventoryFolderBase x in subFolders)
            {
                ForcePurgeFolder(x);
                m_Database.DeleteFolders("folderID", x.ID.ToString(), false);
            }

            m_Database.DeleteItems("parentFolderID", folder.ID.ToString());
            m_Database.DeleteFolders("folderID", folder.ID.ToString(), false);

            return true;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual bool AddItem(InventoryItemBase item)
        {
            object remoteValue = DoRemote(item);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            return AddItem(item, true);
        }

        public virtual bool AddItem(InventoryItemBase item, bool doParentFolderCheck)
        {
            if (doParentFolderCheck)
            {
                InventoryFolderBase folder = GetFolder(new InventoryFolderBase(item.Folder));

                if (folder == null || folder.Owner != item.Owner)
                    return false;
            }
            m_Database.IncrementFolder(item.Folder);
            return m_Database.StoreItem(item);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual bool UpdateItem(InventoryItemBase item)
        {
            object remoteValue = DoRemote(item);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            if (!m_AllowDelete) //Initial item MUST be created as a link or link folder
                if (item.AssetType == (sbyte)AssetType.Link || item.AssetType == (sbyte)AssetType.LinkFolder)
                    return false;
            m_Database.IncrementFolder(item.Folder);
            return m_Database.StoreItem(item);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual bool UpdateAssetIDForItem(UUID itemID, UUID assetID)
        {
            object remoteValue = DoRemote(itemID, assetID);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            return m_Database.UpdateAssetIDForItem(itemID, assetID);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual bool MoveItems(UUID principalID, List<InventoryItemBase> items)
        {
            object remoteValue = DoRemote(principalID, items);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            foreach (InventoryItemBase i in items)
            {
                m_Database.IncrementFolder(i.Folder); //Increment the new folder
                m_Database.IncrementFolderByItem(i.ID);
                //And the old folder too (have to use this one because we don't know the old folder)
                m_Database.MoveItem(i.ID.ToString(), i.Folder.ToString());
            }

            return true;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.High)]
        public virtual bool DeleteItems(UUID principalID, List<UUID> itemIDs)
        {
            object remoteValue = DoRemote(principalID, itemIDs);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            if (!m_AllowDelete)
            {
                foreach (UUID id in itemIDs)
                {
                    InventoryItemBase item = new InventoryItemBase(id);
                    item = GetItem(item);
                    m_Database.IncrementFolder(item.Folder);
                    if (!ParentIsLinkFolder(item.Folder))
                        continue;
                    m_Database.DeleteItems("inventoryID", id.ToString());
                }
                return true;
            }

            // Just use the ID... *facepalms*
            //
            foreach (UUID id in itemIDs)
            {
                m_Database.IncrementFolderByItem(id);
                m_Database.DeleteItems("inventoryID", id.ToString());
            }

            return true;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual InventoryItemBase GetItem(InventoryItemBase item)
        {
            if (_tempItemCache.ContainsKey(item.ID))
                return _tempItemCache[item.ID];
            object remoteValue = DoRemote(item);
            if (remoteValue != null || m_doRemoteOnly)
                return (InventoryItemBase)remoteValue;

            List<InventoryItemBase> items = m_Database.GetItems(
                new[] { "inventoryID" },
                new[] { item.ID.ToString() });

            foreach (InventoryItemBase xitem in items)
            {
                UUID nn;
                if (!UUID.TryParse(xitem.CreatorId, out nn))
                {
                    try
                    {
                        if (xitem.CreatorId != string.Empty)
                        {
                            string FullName = xitem.CreatorId.Remove(0, 7);
                            string[] FirstLast = FullName.Split(' ');
                            UserAccount account = m_UserAccountService.GetUserAccount(null, FirstLast[0],
                                                                                      FirstLast[1]);
                            if (account == null)
                            {
                                xitem.CreatorId = UUID.Zero.ToString();
                                m_Database.StoreItem(xitem);
                            }
                            else
                            {
                                xitem.CreatorId = account.PrincipalID.ToString();
                                m_Database.StoreItem(xitem);
                            }
                        }
                        else
                        {
                            xitem.CreatorId = UUID.Zero.ToString();
                            m_Database.StoreItem(xitem);
                        }
                    }
                    catch
                    {
                        xitem.CreatorId = UUID.Zero.ToString();
                    }
                }
            }

            if (items.Count == 0)
                return null;

            return items[0];
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public virtual OSDArray GetItem(UUID itemID)
        {
            object remoteValue = DoRemote(itemID);
            if (remoteValue != null || m_doRemoteOnly)
                return (OSDArray)remoteValue;

            return m_Database.GetLLSDItems(
                new string[1] { "inventoryID" },
                new string[1] { itemID.ToString() });
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual InventoryFolderBase GetFolder(InventoryFolderBase folder)
        {
            object remoteValue = DoRemote(folder);
            if (remoteValue != null || m_doRemoteOnly)
                return (InventoryFolderBase)remoteValue;

            List<InventoryFolderBase> folders = m_Database.GetFolders(
                new[] { "folderID" },
                new[] { folder.ID.ToString() });

            if (folders.Count == 0)
                return null;

            return folders[0];
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual InventoryFolderBase GetFolderByOwnerAndName(UUID FolderOwner, string FolderName)
        {
            object remoteValue = DoRemote(FolderOwner, FolderName);
            if (remoteValue != null || m_doRemoteOnly)
                return (InventoryFolderBase)remoteValue;

            List<InventoryFolderBase> folders = m_Database.GetFolders(
                new[] { "folderName", "agentID" },
                new[] { FolderName, FolderOwner.ToString() });

            if (folders.Count == 0)
                return null;

            return folders[0];
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public virtual List<InventoryItemBase> GetActiveGestures(UUID principalID)
        {
            object remoteValue = DoRemote(principalID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<InventoryItemBase>)remoteValue;

            return new List<InventoryItemBase>(m_Database.GetActiveGestures(principalID));
        }

        public object DeleteUserInformation(string name, object param)
        {
            UUID user = (UUID)param;
            var skel = GetInventorySkeleton(user);
            foreach (var folder in skel)
            {
                var items = GetFolderContent(user, folder.ID);
                DeleteItems(user, items.Items.ConvertAll<UUID>((item) => item.ID));
                ForcePurgeFolder(folder);
            }
            return null;
        }

        #endregion

        #region Asynchronous Commands
        
        protected ListCombiningTimedSaving<AddInventoryItemStore> _addInventoryItemQueue = new ListCombiningTimedSaving<AddInventoryItemStore>();
        protected ListCombiningTimedSaving<MoveInventoryItemStore> _moveInventoryItemQueue = new ListCombiningTimedSaving<MoveInventoryItemStore>();

        public void AddItemAsync(InventoryItemBase item, NoParam success)
        {
            if (UUID.Zero == item.Folder)
            {
                InventoryFolderBase f = GetFolderForType(item.Owner, (InventoryType)item.InvType, (AssetType)item.AssetType);
                if (f != null)
                    item.Folder = f.ID;
                else
                {
                    f = GetRootFolder(item.Owner);
                    if (f != null)
                        item.Folder = f.ID;
                    else
                    {
                        MainConsole.Instance.WarnFormat(
                            "[LLClientInventory]: Could not find root folder for {0} when trying to add item {1} with no parent folder specified",
                            item.Owner, item.Name);
                        return;
                    }
                }
            }

            if (!_tempItemCache.ContainsKey(item.ID))
                _tempItemCache.Add(item.ID, item);
            _addInventoryItemQueue.Add(item.Owner, new AddInventoryItemStore(item, success));
        }

        public void MoveItemsAsync(UUID agentID, List<InventoryItemBase> items, NoParam success)
        {
            _moveInventoryItemQueue.Add(agentID, new MoveInventoryItemStore(items, success));
        }

        /// <summary>
        /// Give an entire inventory folder from one user to another.  The entire contents (including all descendent
        /// folders) is given.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="senderId">ID of the sender of the item</param>
        /// <param name="folderId"></param>
        /// <param name="recipientParentFolderId">
        /// The id of the receipient folder in which the send folder should be placed.  If UUID.Zero then the
        /// recipient folder is the root folder
        /// </param>
        /// <returns>
        /// The inventory folder copy given, null if the copy was unsuccessful
        /// </returns>
        public void GiveInventoryFolderAsync(
            UUID recipientId, UUID senderId, UUID folderId, UUID recipientParentFolderId, GiveFolderParam success)
        {
            Util.FireAndForget(o =>
            {
                // Retrieve the folder from the sender
                InventoryFolderBase folder = GetFolder(new InventoryFolderBase(folderId));
                if (null == folder)
                {
                    MainConsole.Instance.ErrorFormat(
                            "[InventoryService]: Could not find inventory folder {0} to give", folderId);
                    success(null);
                    return;
                }

                //Find the folder for the receiver
                if (recipientParentFolderId == UUID.Zero)
                {
                    InventoryFolderBase recipientRootFolder = GetRootFolder(recipientId);
                    if (recipientRootFolder != null)
                        recipientParentFolderId = recipientRootFolder.ID;
                    else
                    {
                        MainConsole.Instance.WarnFormat("[InventoryService]: Unable to find root folder for receiving agent");
                        success(null);
                        return;
                    }
                }

                UUID newFolderId = UUID.Random();
                InventoryFolderBase newFolder
                    = new InventoryFolderBase(
                        newFolderId, folder.Name, recipientId, folder.Type, recipientParentFolderId, folder.Version);
                AddFolder(newFolder);

                // Give all the subfolders
                InventoryCollection contents = GetFolderContent(senderId, folderId);
                foreach (InventoryFolderBase childFolder in contents.Folders)
                {
                    GiveInventoryFolderAsync(recipientId, senderId, childFolder.ID, newFolder.ID, null);
                }

                // Give all the items
                foreach (InventoryItemBase item in contents.Items)
                {
                    InnerGiveInventoryItem(recipientId, senderId, item, newFolder.ID, true);
                }
                success(newFolder);
            });
        }

        /// <summary>
        /// Give an inventory item from one user to another
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="senderId">ID of the sender of the item</param>
        /// <param name="itemId"></param>
        /// <param name="recipientFolderId">
        /// The id of the folder in which the copy item should go.  If UUID.Zero then the item is placed in the most
        /// appropriate default folder.
        /// </param>
        /// <param name="doOwnerCheck">This is for when the item is being given away publically, such as when it is posted on a group notice</param>
        /// <returns>
        /// The inventory item copy given, null if the give was unsuccessful
        /// </returns>
        public void GiveInventoryItemAsync (UUID recipient, UUID senderId, UUID itemId, 
            UUID recipientFolderId, bool doOwnerCheck, GiveItemParam success)
        {
            Util.FireAndForget(o =>
            {
                InventoryItemBase item = new InventoryItemBase(itemId, senderId);
                item = GetItem(item);
                success(InnerGiveInventoryItem(recipient, senderId,
                    item, recipientFolderId, doOwnerCheck));
            });
        }

        public InventoryItemBase InnerGiveInventoryItem(
            UUID recipient, UUID senderId, InventoryItemBase item, UUID recipientFolderId, bool doOwnerCheck)
        {
            if (item == null)
            {
                MainConsole.Instance.Info("[InventoryService]: Could not find item to give to " + recipient);
                return null;
            }
            if (!doOwnerCheck || item.Owner == senderId)
            {
                if ((item.CurrentPermissions & (uint)PermissionMask.Transfer) == 0)
                    return null;

                IUserFinder uman = m_registry.RequestModuleInterface<IUserFinder>();
                if (uman != null)
                    uman.AddUser(item.CreatorIdAsUuid, item.CreatorData);

                // Insert a copy of the item into the recipient
                InventoryItemBase itemCopy = new InventoryItemBase
                {
                    Owner = recipient,
                    CreatorId = item.CreatorId,
                    CreatorData = item.CreatorData,
                    ID = UUID.Random(),
                    AssetID = item.AssetID,
                    Description = item.Description,
                    Name = item.Name,
                    AssetType = item.AssetType,
                    InvType = item.InvType,
                    Folder = recipientFolderId
                };

                if (recipient != senderId)
                {
                    // Trying to do this right this time. This is evil. If
                    // you believe in Good, go elsewhere. Vampires and other
                    // evil creatores only beyond this point. You have been
                    // warned.

                    // We're going to mask a lot of things by the next perms
                    // Tweak the next perms to be nicer to our data
                    //
                    // In this mask, all the bits we do NOT want to mess
                    // with are set. These are:
                    //
                    // Transfer
                    // Copy
                    // Modufy
                    const uint permsMask = ~((uint)PermissionMask.Copy |
                                             (uint)PermissionMask.Transfer |
                                             (uint)PermissionMask.Modify);

                    // Now, reduce the next perms to the mask bits
                    // relevant to the operation
                    uint nextPerms = permsMask | (item.NextPermissions &
                                      ((uint)PermissionMask.Copy |
                                       (uint)PermissionMask.Transfer |
                                       (uint)PermissionMask.Modify));

                    // nextPerms now has all bits set, except for the actual
                    // next permission bits.

                    // This checks for no mod, no copy, no trans.
                    // This indicates an error or messed up item. Do it like
                    // SL and assume trans
                    if (nextPerms == permsMask)
                        nextPerms |= (uint)PermissionMask.Transfer;

                    // Inventory owner perms are the logical AND of the
                    // folded perms and the root prim perms, however, if
                    // the root prim is mod, the inventory perms will be
                    // mod. This happens on "take" and is of little concern
                    // here, save for preventing escalation

                    // This hack ensures that items previously permalocked
                    // get unlocked when they're passed or rezzed
                    uint basePerms = item.BasePermissions |
                                    (uint)PermissionMask.Move;
                    uint ownerPerms = item.CurrentPermissions;

                    // If this is an object, root prim perms may be more
                    // permissive than folded perms. Use folded perms as
                    // a mask
                    if (item.InvType == (int)InventoryType.Object)
                    {
                        // Create a safe mask for the current perms
                        uint foldedPerms = (item.CurrentPermissions & 7) << 13;
                        foldedPerms |= permsMask;

                        bool isRootMod = (item.CurrentPermissions &
                                          (uint)PermissionMask.Modify) != 0;

                        // Mask the owner perms to the folded perms
                        ownerPerms &= foldedPerms;
                        basePerms &= foldedPerms;

                        // If the root was mod, let the mask reflect that
                        // We also need to adjust the base here, because
                        // we should be able to edit in-inventory perms
                        // for the root prim, if it's mod.
                        if (isRootMod)
                        {
                            ownerPerms |= (uint)PermissionMask.Modify;
                            basePerms |= (uint)PermissionMask.Modify;
                        }
                    }

                    // These will be applied to the root prim at next rez.
                    // The slam bit (bit 3) and folded permission (bits 0-2)
                    // are preserved due to the above mangling
                    ownerPerms &= nextPerms;

                    // Mask the base permissions. This is a conservative
                    // approach altering only the three main perms
                    basePerms &= nextPerms;

                    // Assign to the actual item. Make sure the slam bit is
                    // set, if it wasn't set before.
                    itemCopy.BasePermissions = basePerms;
                    itemCopy.CurrentPermissions = ownerPerms | 16; // Slam

                    itemCopy.NextPermissions = item.NextPermissions;

                    // This preserves "everyone can move"
                    itemCopy.EveryOnePermissions = item.EveryOnePermissions &
                                                   nextPerms;

                    // Intentionally killing "share with group" here, as
                    // the recipient will not have the group this is
                    // set to
                    itemCopy.GroupPermissions = 0;
                }
                else
                {
                    itemCopy.CurrentPermissions = item.CurrentPermissions;
                    itemCopy.NextPermissions = item.NextPermissions;
                    itemCopy.EveryOnePermissions = item.EveryOnePermissions & item.NextPermissions;
                    itemCopy.GroupPermissions = item.GroupPermissions & item.NextPermissions;
                    itemCopy.BasePermissions = item.BasePermissions;
                }

                if (itemCopy.Folder == UUID.Zero)
                {
                    InventoryFolderBase folder = GetFolderForType(recipient,
                        (InventoryType)itemCopy.InvType, (AssetType)itemCopy.AssetType);

                    if (folder != null)
                        itemCopy.Folder = folder.ID;
                }

                itemCopy.GroupID = UUID.Zero;
                itemCopy.GroupOwned = false;
                itemCopy.Flags = item.Flags;
                itemCopy.SalePrice = item.SalePrice;
                itemCopy.SaleType = item.SaleType;

                AddItemAsync(itemCopy, () =>
                {
                    if ((item.CurrentPermissions & (uint)PermissionMask.Copy) == 0)
                        DeleteItems(senderId, new List<UUID> { item.ID });
                });

                return itemCopy;
            }
            MainConsole.Instance.WarnFormat("[InventoryService]: Failed to give item {0} as item does not belong to giver", item.ID.ToString());
            return null;
        }

        #region Internal Classes

        protected class AddInventoryItemStore
        {
            public AddInventoryItemStore(InventoryItemBase item, NoParam success)
            {
                Item = item;
                Complete = success;
            }
            public InventoryItemBase Item;
            public NoParam Complete;
        }

        protected class MoveInventoryItemStore
        {
            public MoveInventoryItemStore(List<InventoryItemBase> items, NoParam success)
            {
                Items = items;
                Complete = success;
            }
            public List<InventoryItemBase> Items;
            public NoParam Complete;
        }

        #endregion

        #endregion

        #region Console Commands

        public virtual void FixInventory(string[] cmd)
        {
            string userName = MainConsole.Instance.Prompt("Name of user");
            UserAccount account = m_UserAccountService.GetUserAccount(null, userName);
            if (account == null)
            {
                MainConsole.Instance.Warn("Could not find user");
                return;
            }
            InventoryFolderBase rootFolder = GetRootFolder(account.PrincipalID);

            //Fix having a default root folder
            if (rootFolder == null)
            {
                MainConsole.Instance.Warn("Fixing default root folder...");
                List<InventoryFolderBase> skel = GetInventorySkeleton(account.PrincipalID);
                if (skel.Count == 0)
                {
                    CreateUserInventory(account.PrincipalID, false);
                    rootFolder = GetRootFolder(account.PrincipalID);
                }
                else
                {
                    rootFolder = new InventoryFolderBase
                    {
                        Name = "My Inventory",
                        Type = (short)AssetType.RootFolder,
                        Version = 1,
                        ID = skel[0].ParentID,
                        Owner = account.PrincipalID,
                        ParentID = UUID.Zero
                    };

                }
            }
            //Check against multiple root folders
            List<InventoryFolderBase> rootFolders = GetRootFolders(account.PrincipalID);
            List<UUID> badFolders = new List<UUID>();
            if (rootFolders.Count != 1)
            {
                //No duplicate folders!
#if (!ISWIN)
                foreach (InventoryFolderBase f in rootFolders)
                {
                    if (!badFolders.Contains(f.ID) && f.ID != rootFolder.ID)
                    {
                        MainConsole.Instance.Warn("Removing duplicate root folder " + f.Name);
                        badFolders.Add(f.ID);
                    }
                }
#else
                foreach (InventoryFolderBase f in rootFolders.Where(f => !badFolders.Contains(f.ID) && f.ID != rootFolder.ID))
                {
                    MainConsole.Instance.Warn("Removing duplicate root folder " + f.Name);
                    badFolders.Add(f.ID);
                }
#endif
            }
            //Fix any root folders that shouldn't be root folders
            List<InventoryFolderBase> skeleton = GetInventorySkeleton(account.PrincipalID);
            List<UUID> foundFolders = new List<UUID>();
            foreach (InventoryFolderBase f in skeleton)
            {
                if (!foundFolders.Contains(f.ID))
                    foundFolders.Add(f.ID);
                if (f.Name == "My Inventory" && f.ParentID != UUID.Zero)
                {
                    //Merge them all together
                    badFolders.Add(f.ID);
                }
            }
            foreach (InventoryFolderBase f in skeleton)
            {
                if ((!foundFolders.Contains(f.ParentID) && f.ParentID != UUID.Zero) ||
                    f.ID == f.ParentID)
                {
                    //The viewer loses the parentID when something goes wrong
                    //it puts it in the top where My Inventory should be
                    //We need to put it back in the My Inventory folder, as the sub folders are right for some reason
                    f.ParentID = rootFolder.ID;
                    m_Database.StoreFolder(f);
                    MainConsole.Instance.WarnFormat("Fixing folder {0}", f.Name);
                }
                else if (badFolders.Contains(f.ParentID))
                {
                    //Put it back in the My Inventory folder
                    f.ParentID = rootFolder.ID;
                    m_Database.StoreFolder(f);
                    MainConsole.Instance.WarnFormat("Fixing folder {0}", f.Name);
                }
                else if (f.Type == (short)AssetType.CurrentOutfitFolder)
                {
                    List<InventoryItemBase> items = GetFolderItems(account.PrincipalID, f.ID);
                    //Check the links!
                    List<UUID> brokenLinks = new List<UUID>();
                    foreach (InventoryItemBase item in items)
                    {
                        InventoryItemBase linkedItem = null;
                        if ((linkedItem = GetItem(new InventoryItemBase(item.AssetID))) == null)
                        {
                            //Broken link...
                            brokenLinks.Add(item.ID);
                        }
                        else if (linkedItem.ID == AvatarWearable.DEFAULT_EYES_ITEM ||
                                 linkedItem.ID == AvatarWearable.DEFAULT_BODY_ITEM ||
                                 linkedItem.ID == AvatarWearable.DEFAULT_HAIR_ITEM ||
                                 linkedItem.ID == AvatarWearable.DEFAULT_PANTS_ITEM ||
                                 linkedItem.ID == AvatarWearable.DEFAULT_SHIRT_ITEM ||
                                 linkedItem.ID == AvatarWearable.DEFAULT_SKIN_ITEM)
                        {
                            //Default item link, needs removed
                            brokenLinks.Add(item.ID);
                        }
                    }
                    if (brokenLinks.Count != 0)
                        DeleteItems(account.PrincipalID, brokenLinks);
                }
                else if (f.Type == (short)AssetType.Mesh)
                {
                    ForcePurgeFolder(f);
                }
            }
            foreach (UUID id in badFolders)
            {
                m_Database.DeleteFolders("folderID", id.ToString(), false);
            }
            //Make sure that all default folders exist
            CreateUserInventory(account.PrincipalID, false);
            //Refetch the skeleton now
            skeleton = GetInventorySkeleton(account.PrincipalID);
            Dictionary<int, UUID> defaultFolders = new Dictionary<int, UUID>();
            Dictionary<UUID, UUID> changedFolders = new Dictionary<UUID, UUID>();
#if (!ISWIN)
            foreach (InventoryFolderBase folder in skeleton)
            {
                if (folder.Type != -1)
                {
                    if (!defaultFolders.ContainsKey(folder.Type))
                        defaultFolders[folder.Type] = folder.ID;
                    else
                        changedFolders.Add(folder.ID, defaultFolders[folder.Type]);
                }
            }
#else
            foreach (InventoryFolderBase folder in skeleton.Where(folder => folder.Type != -1))
            {
                if (!defaultFolders.ContainsKey(folder.Type))
                    defaultFolders[folder.Type] = folder.ID;
                else
                    changedFolders.Add(folder.ID, defaultFolders[folder.Type]);
            }
#endif
            foreach (InventoryFolderBase folder in skeleton)
            {
                if (folder.Type != -1 && defaultFolders[folder.Type] != folder.ID)
                {
                    //Delete the dup
                    ForcePurgeFolder(folder);
                    MainConsole.Instance.Warn("Purging duplicate default inventory type folder " + folder.Name);
                }
                if (changedFolders.ContainsKey(folder.ParentID))
                {
                    folder.ParentID = changedFolders[folder.ParentID];
                    MainConsole.Instance.Warn("Merging child folder of default inventory type " + folder.Name);
                    m_Database.StoreFolder(folder);
                }
            }
            MainConsole.Instance.Warn("Completed the check");
        }

        #endregion

        #region Helpers

        protected InventoryFolderBase CreateFolder(UUID principalID, UUID parentID, int type, string name)
        {
            InventoryFolderBase newFolder = new InventoryFolderBase
            {
                Name = name,
                Type = (short)type,
                Version = 1,
                ID = UUID.Random(),
                Owner = principalID,
                ParentID = parentID
            };


            m_Database.StoreFolder(newFolder);

            return newFolder;
        }

        protected virtual InventoryFolderBase[] GetSystemFolders(UUID principalID)
        {
            //            MainConsole.Instance.DebugFormat("[XINVENTORY SERVICE]: Getting system folders for {0}", principalID);

            InventoryFolderBase[] allFolders = m_Database.GetFolders(
                new[] { "agentID" },
                new[] { principalID.ToString() }).ToArray();

            InventoryFolderBase[] sysFolders = Array.FindAll(
                allFolders,
                delegate(InventoryFolderBase f)
                {
                    if (f.Type > 0)
                        return true;
                    return false;
                });

            //            MainConsole.Instance.DebugFormat(
            //                "[XINVENTORY SERVICE]: Found {0} system folders for {1}", sysFolders.Length, principalID);

            return sysFolders;
        }

        private bool ParentIsTrash(UUID folderID)
        {
            List<InventoryFolderBase> folder = m_Database.GetFolders(new[] { "folderID" }, new[] { folderID.ToString() });
            if (folder.Count < 1)
                return false;

            if (folder[0].Type == (int)AssetType.TrashFolder ||
                folder[0].Type == (int)AssetType.LostAndFoundFolder)
                return true;

            UUID parentFolder = folder[0].ParentID;

            while (parentFolder != UUID.Zero)
            {
                List<InventoryFolderBase> parent = m_Database.GetFolders(new[] { "folderID" },
                                                                         new[] { parentFolder.ToString() });
                if (parent.Count < 1)
                    return false;

                if (parent[0].Type == (int)AssetType.TrashFolder ||
                    parent[0].Type == (int)AssetType.LostAndFoundFolder)
                    return true;
                if (parent[0].Type == (int)AssetType.RootFolder)
                    return false;

                parentFolder = parent[0].ParentID;
            }
            return false;
        }

        private bool ParentIsLinkFolder(UUID folderID)
        {
            List<InventoryFolderBase> folder = m_Database.GetFolders(new[] { "folderID" }, new[] { folderID.ToString() });
            if (folder.Count < 1)
                return false;

            if (folder[0].Type == (int)AssetType.LinkFolder)
                return true;

            UUID parentFolder = folder[0].ParentID;

            while (parentFolder != UUID.Zero)
            {
                List<InventoryFolderBase> parent = m_Database.GetFolders(new[] { "folderID" },
                                                                         new[] { parentFolder.ToString() });
                if (parent.Count < 1)
                    return false;

                if (parent[0].Type == (int)AssetType.LinkFolder)
                    return true;
                if (parent[0].Type == (int)AssetType.RootFolder)
                    return false;

                parentFolder = parent[0].ParentID;
            }
            return false;
        }

        #endregion
    }
}