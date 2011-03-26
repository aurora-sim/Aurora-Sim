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
using System.Collections.Generic;
using OpenMetaverse;
using log4net;
using Nini.Config;
using System.Reflection;
using OpenSim.Services.Interfaces;
using OpenSim.Data;
using OpenSim.Framework;
using Aurora.Framework;
using Aurora.Simulation.Base;

namespace OpenSim.Services.InventoryService
{
    public class InventoryService : IInventoryService, IService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        protected IInventoryData m_Database;
        protected IUserAccountService m_UserAccountService;
        protected IAssetService m_AssetService;
        protected ILibraryService m_LibraryService;
        protected bool m_AllowDelete = true;

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
                m_AllowDelete = invConfig.GetBoolean ("AllowDelete", true);

            registry.RegisterModuleInterface<IInventoryService>(this);
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
            m_Database = Aurora.DataManager.DataManager.RequestPlugin<IInventoryData> ();
            m_UserAccountService = registry.RequestModuleInterface<IUserAccountService>();
            m_LibraryService = registry.RequestModuleInterface<ILibraryService>();
            m_AssetService = registry.RequestModuleInterface<IAssetService>();
        }

        public void FinishedStartup()
        {
        }

        public virtual bool CreateUserInventory(UUID principalID)
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

            InventoryFolderBase[] sysFolders = GetSystemFolders (principalID);

            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.Animation) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Animation, "Animations");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.Bodypart) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Bodypart, "Body Parts");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.CallingCard) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.CallingCard, "Calling Cards");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.Clothing) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Clothing, "Clothing");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.Gesture) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Gesture, "Gestures");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.Landmark) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Landmark, "Landmarks");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.LostAndFoundFolder) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.LostAndFoundFolder, "Lost And Found");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.Notecard) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Notecard, "Notecards");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.Object) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Object, "Objects");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.SnapshotFolder) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.SnapshotFolder, "Photo Album");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.LSLText) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.LSLText, "Scripts");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.Sound) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Sound, "Sounds");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.Texture) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.Texture, "Textures");
            if (!Array.Exists (sysFolders, delegate (InventoryFolderBase f) { if (f.Type == (short)AssetType.TrashFolder) return true; return false; }))
                CreateFolder(principalID, rootFolder.ID, (int)AssetType.TrashFolder, "Trash");

            if (m_LibraryService != null)
            {

                InventoryFolderBase bodypartFolder = GetFolderForType(principalID, AssetType.Bodypart);
                InventoryFolderBase clothingFolder = GetFolderForType(principalID, AssetType.Clothing);

                // Default items
                InventoryItemBase defaultShape = new InventoryItemBase();
                defaultShape.Name = "Default shape";
                defaultShape.Description = "Default shape description";
                defaultShape.AssetType = (int)AssetType.Bodypart;
                defaultShape.InvType = (int)InventoryType.Wearable;
                defaultShape.Flags = (uint)WearableType.Shape;
                defaultShape.ID = AvatarWearable.DEFAULT_BODY_ITEM;
                //Give a new copy to every person
                AssetBase asset = m_AssetService.Get(AvatarWearable.DEFAULT_BODY_ASSET.ToString());
                if (asset != null)
                {
                    asset.FullID = UUID.Random();
                    m_AssetService.Store(asset);
                    defaultShape.AssetID = asset.FullID;
                    defaultShape.Folder = bodypartFolder.ID;
                    defaultShape.CreatorId = UUID.Zero.ToString();
                    AddItem(defaultShape);
                }

                InventoryItemBase defaultSkin = new InventoryItemBase();
                defaultSkin.Name = "Default skin";
                defaultSkin.Description = "Default skin description";
                defaultSkin.AssetType = (int)AssetType.Bodypart;
                defaultSkin.InvType = (int)InventoryType.Wearable;
                defaultSkin.Flags = (uint)WearableType.Skin;
                defaultSkin.ID = AvatarWearable.DEFAULT_SKIN_ITEM;
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_SKIN_ASSET.ToString());
                if (asset != null)
                {
                    asset.FullID = UUID.Random();
                    m_AssetService.Store(asset);
                    defaultSkin.AssetID = asset.FullID;
                    defaultSkin.Folder = bodypartFolder.ID;
                    defaultSkin.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultSkin.Owner = principalID;
                    defaultSkin.BasePermissions = (uint)PermissionMask.All;
                    defaultSkin.CurrentPermissions = (uint)PermissionMask.All;
                    defaultSkin.EveryOnePermissions = (uint)PermissionMask.None;
                    defaultSkin.NextPermissions = (uint)PermissionMask.All;
                    AddItem(defaultSkin);
                }

                InventoryItemBase defaultHair = new InventoryItemBase();
                defaultHair.Name = "Default hair";
                defaultHair.Description = "Default hair description";
                defaultHair.AssetType = (int)AssetType.Bodypart;
                defaultHair.InvType = (int)InventoryType.Wearable;
                defaultHair.Flags = (uint)WearableType.Hair;
                defaultHair.ID = AvatarWearable.DEFAULT_HAIR_ITEM;
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_HAIR_ASSET.ToString());
                if (asset != null)
                {
                    asset.FullID = UUID.Random();
                    m_AssetService.Store(asset);
                    defaultHair.AssetID = asset.FullID;
                    defaultHair.Folder = bodypartFolder.ID;
                    defaultHair.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultHair.Owner = principalID;
                    defaultHair.BasePermissions = (uint)PermissionMask.All;
                    defaultHair.CurrentPermissions = (uint)PermissionMask.All;
                    defaultHair.EveryOnePermissions = (uint)PermissionMask.None;
                    defaultHair.NextPermissions = (uint)PermissionMask.All;
                    AddItem(defaultHair);
                }

                InventoryItemBase defaultEyes = new InventoryItemBase();
                defaultEyes.Name = "Default eyes";
                defaultEyes.Description = "Default eyes description";
                defaultEyes.AssetType = (int)AssetType.Bodypart;
                defaultEyes.InvType = (int)InventoryType.Wearable;
                defaultEyes.Flags = (uint)WearableType.Eyes;
                defaultEyes.ID = AvatarWearable.DEFAULT_EYES_ITEM;
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_EYES_ASSET.ToString());
                if (asset != null)
                {
                    asset.FullID = UUID.Random();
                    m_AssetService.Store(asset);
                    defaultEyes.AssetID = asset.FullID;
                    defaultEyes.Folder = bodypartFolder.ID;
                    defaultEyes.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultEyes.Owner = principalID;
                    defaultEyes.BasePermissions = (uint)PermissionMask.All;
                    defaultEyes.CurrentPermissions = (uint)PermissionMask.All;
                    defaultEyes.EveryOnePermissions = (uint)PermissionMask.None;
                    defaultEyes.NextPermissions = (uint)PermissionMask.All;
                    AddItem(defaultEyes);
                }

                InventoryItemBase defaultShirt = new InventoryItemBase();
                defaultShirt.Name = "Default shirt";
                defaultShirt.Description = "Default shirt description";
                defaultShirt.AssetType = (int)AssetType.Clothing;
                defaultShirt.InvType = (int)InventoryType.Wearable;
                defaultShirt.Flags = (uint)WearableType.Shirt;
                defaultShirt.ID = AvatarWearable.DEFAULT_SHIRT_ITEM;
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_SHIRT_ASSET.ToString());
                if (asset != null)
                {
                    asset.FullID = UUID.Random();
                    m_AssetService.Store(asset);
                    defaultShirt.AssetID = asset.FullID;
                    defaultShirt.Folder = clothingFolder.ID;
                    defaultShirt.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultShirt.Owner = principalID;
                    defaultShirt.BasePermissions = (uint)PermissionMask.All;
                    defaultShirt.CurrentPermissions = (uint)PermissionMask.All;
                    defaultShirt.EveryOnePermissions = (uint)PermissionMask.None;
                    defaultShirt.NextPermissions = (uint)PermissionMask.All;
                    AddItem(defaultShirt);
                }

                InventoryItemBase defaultPants = new InventoryItemBase();
                defaultPants.Name = "Default pants";
                defaultPants.Description = "Default pants description";
                defaultPants.AssetType = (int)AssetType.Clothing;
                defaultPants.InvType = (int)InventoryType.Wearable;
                defaultPants.Flags = (uint)WearableType.Pants;
                defaultPants.ID = AvatarWearable.DEFAULT_PANTS_ITEM;
                //Give a new copy to every person
                asset = m_AssetService.Get(AvatarWearable.DEFAULT_PANTS_ASSET.ToString());
                if (asset != null)
                {
                    asset.FullID = UUID.Random();
                    m_AssetService.Store(asset);
                    defaultPants.AssetID = asset.FullID;
                    defaultPants.Folder = clothingFolder.ID;
                    defaultPants.CreatorId = m_LibraryService.LibraryOwner.ToString();
                    defaultPants.Owner = principalID;
                    defaultPants.BasePermissions = (uint)PermissionMask.All;
                    defaultPants.CurrentPermissions = (uint)PermissionMask.All;
                    defaultPants.EveryOnePermissions = (uint)PermissionMask.None;
                    defaultPants.NextPermissions = (uint)PermissionMask.All;
                    AddItem(defaultPants);
                }
            }

            return result;
        }

        protected InventoryFolderBase CreateFolder (UUID principalID, UUID parentID, int type, string name)
        {
            InventoryFolderBase newFolder = new InventoryFolderBase ();

            newFolder.Name = name;
            newFolder.Type = (short)type;
            newFolder.Version = 1;
            newFolder.ID = UUID.Random();
            newFolder.Owner = principalID;
            newFolder.ParentID = parentID;

            m_Database.StoreFolder(newFolder);

            return newFolder;
        }

        protected virtual InventoryFolderBase[] GetSystemFolders (UUID principalID)
        {
//            m_log.DebugFormat("[XINVENTORY SERVICE]: Getting system folders for {0}", principalID);

            InventoryFolderBase[] allFolders = m_Database.GetFolders (
                    new string[] { "agentID" },
                    new string[] { principalID.ToString() });

            InventoryFolderBase[] sysFolders = Array.FindAll (
                    allFolders,
                    delegate (InventoryFolderBase f)
                    {
                        if (f.Type > 0)
                            return true;
                        return false;
                    });

//            m_log.DebugFormat(
//                "[XINVENTORY SERVICE]: Found {0} system folders for {1}", sysFolders.Length, principalID);
            
            return sysFolders;
        }

        public virtual List<InventoryFolderBase> GetInventorySkeleton(UUID principalID)
        {
            InventoryFolderBase[] allFolders = m_Database.GetFolders (
                    new string[] { "agentID" },
                    new string[] { principalID.ToString() });

            if (allFolders.Length == 0)
                return null;

            return new List<InventoryFolderBase>(allFolders);
        }

        public virtual InventoryFolderBase GetRootFolder(UUID principalID)
        {
            InventoryFolderBase[] folders = m_Database.GetFolders (
                    new string[] { "agentID", "parentFolderID"},
                    new string[] { principalID.ToString(), UUID.Zero.ToString() });

            if (folders.Length == 0)
                return null;

            InventoryFolderBase root = null;
            foreach (InventoryFolderBase folder in folders)
                if (folder.Name == "My Inventory")
                    root = folder;
            if (folders == null) // oops
                root = folders[0];

            return root;
        }

        public virtual InventoryFolderBase GetFolderForType(UUID principalID, AssetType type)
        {
//            m_log.DebugFormat("[XINVENTORY SERVICE]: Getting folder type {0} for user {1}", type, principalID);

            InventoryFolderBase[] folders = m_Database.GetFolders (
                    new string[] { "agentID", "type"},
                    new string[] { principalID.ToString(), ((int)type).ToString() });

            if (folders.Length == 0)
            {
//                m_log.WarnFormat("[XINVENTORY SERVICE]: Found no folder for type {0} for user {1}", type, principalID);
                return null;
            }
            
//            m_log.DebugFormat(
//                "[XINVENTORY SERVICE]: Found folder {0} {1} for type {2} for user {3}", 
//                folders[0].folderName, folders[0].folderID, type, principalID);

            return folders[0];
        }

        public virtual InventoryCollection GetFolderContent(UUID principalID, UUID folderID)
        {
            // This method doesn't receive a valud principal id from the
            // connector. So we disregard the principal and look
            // by ID.
            //
            m_log.DebugFormat("[XINVENTORY SERVICE]: Fetch contents for folder {0}", folderID.ToString());
            InventoryCollection inventory = new InventoryCollection();
            inventory.UserID = principalID;
            inventory.Folders = new List<InventoryFolderBase>();
            inventory.Items = new List<InventoryItemBase>();

            inventory.Folders.AddRange(m_Database.GetFolders (
                    new string[] { "parentFolderID"},
                    new string[] { folderID.ToString() }));

            inventory.Items.AddRange(m_Database.GetItems (
                    new string[] { "parentFolderID"},
                    new string[] { folderID.ToString() }));

            return inventory;
        }
        
        public virtual List<InventoryItemBase> GetFolderItems(UUID principalID, UUID folderID)
        {
//            m_log.DebugFormat("[XINVENTORY]: Fetch items for folder {0}", folderID);
            
            // Since we probably don't get a valid principal here, either ...
            //
            List<InventoryItemBase> invItems = new List<InventoryItemBase>();

            invItems.AddRange(m_Database.GetItems (
                    new string[] { "parentFolderID"},
                    new string[] { folderID.ToString() }));

            return invItems;
        }

        public virtual bool AddFolder(InventoryFolderBase folder)
        {
            InventoryFolderBase check = GetFolder(folder);
            if (check != null)
                return false;

            return m_Database.StoreFolder (folder);
        }

        public virtual bool UpdateFolder(InventoryFolderBase folder)
        {
            InventoryFolderBase check = GetFolder(folder);
            if (check == null)
                return AddFolder(folder);

            if (check.Type != -1 || folder.Type != -1)
            {
                if (folder.Version > check.Version)
                    return false;
                check.Version = (ushort)folder.Version;
                return m_Database.StoreFolder (check);
            }

            if (folder.Version < check.Version)
                folder.Version = check.Version;
            folder.ID = check.ID;

            return m_Database.StoreFolder (folder);
        }

        public virtual bool MoveFolder(InventoryFolderBase folder)
        {
            InventoryFolderBase[] x = m_Database.GetFolders (
                    new string[] { "folderID" },
                    new string[] { folder.ID.ToString() });

            if (x.Length == 0)
                return false;

            x[0].ParentID = folder.ParentID;

            return m_Database.StoreFolder(x[0]);
        }

        // We don't check the principal's ID here
        //
        public virtual bool DeleteFolders(UUID principalID, List<UUID> folderIDs)
        {
            if (!m_AllowDelete)
                return false;

            // Ignore principal ID, it's bogus at connector level
            //
            foreach (UUID id in folderIDs)
            {
                if (!ParentIsTrash(id))
                    continue;
                InventoryFolderBase f = new InventoryFolderBase();
                f.ID = id;
                PurgeFolder(f);
                m_Database.DeleteFolders("folderID", id.ToString());
            }

            return true;
        }

        public virtual bool PurgeFolder(InventoryFolderBase folder)
        {
            if (!m_AllowDelete)
                return false;

            if (!ParentIsTrash(folder.ID))
                return false;

            InventoryFolderBase[] subFolders = m_Database.GetFolders (
                    new string[] { "parentFolderID" },
                    new string[] { folder.ID.ToString() });

            foreach (InventoryFolderBase x in subFolders)
            {
                PurgeFolder(x);
                m_Database.DeleteFolders("folderID", x.ID.ToString());
            }

            m_Database.DeleteItems("parentFolderID", folder.ID.ToString());

            return true;
        }

        public virtual bool ForcePurgeFolder (InventoryFolderBase folder)
        {
            InventoryFolderBase[] subFolders = m_Database.GetFolders (
                    new string[] { "parentFolderID" },
                    new string[] { folder.ID.ToString () });

            foreach (InventoryFolderBase x in subFolders)
            {
                PurgeFolder (x);
                m_Database.DeleteFolders ("folderID", x.ID.ToString ());
            }

            m_Database.DeleteItems ("parentFolderID", folder.ID.ToString ());
            m_Database.DeleteFolders ("folderID", folder.ID.ToString ());

            return true;
        }

        public virtual bool AddItem(InventoryItemBase item)
        {
//            m_log.DebugFormat(
//                "[XINVENTORY SERVICE]: Adding item {0} to folder {1} for {2}", item.ID, item.Folder, item.Owner);
            
            return m_Database.StoreItem(item);
        }

        public virtual bool UpdateItem(InventoryItemBase item)
        {
            return m_Database.StoreItem(item);
        }

        public virtual bool MoveItems(UUID principalID, List<InventoryItemBase> items)
        {
            // Principal is b0rked. *sigh*
            //
            foreach (InventoryItemBase i in items)
            {
                m_Database.MoveItem(i.ID.ToString(), i.Folder.ToString());
            }

            return true;
        }

        public virtual bool DeleteItems(UUID principalID, List<UUID> itemIDs)
        {
            if (!m_AllowDelete)
                return false;

            // Just use the ID... *facepalms*
            //
            foreach (UUID id in itemIDs)
                m_Database.DeleteItems("inventoryID", id.ToString());

            return true;
        }

        public virtual InventoryItemBase GetItem(InventoryItemBase item)
        {
            InventoryItemBase[] items = m_Database.GetItems (
                    new string[] { "inventoryID" },
                    new string[] { item.ID.ToString() });

            foreach (InventoryItemBase xitem in items)
            {
                UUID nn;
                if (!UUID.TryParse(xitem.CreatorId, out nn))
                {
                    try
                    {
                        if (xitem.CreatorId != string.Empty)
                        {
                            string FullName = xitem.CreatorId.Remove (0, 7);
                            string[] FirstLast = FullName.Split(' ');
                            UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, FirstLast[0], FirstLast[1]);
                            if (account == null)
                            {
                                xitem.CreatorId = UUID.Zero.ToString ();
                                m_Database.StoreItem(xitem);
                            }
                            else
                            {
                                xitem.CreatorId = account.PrincipalID.ToString ();
                                m_Database.StoreItem(xitem);
                            }
                        }
                        else
                        {
                            xitem.CreatorId = UUID.Zero.ToString ();
                            m_Database.StoreItem(xitem);
                        }
                    }
                    catch
                    {
                        xitem.CreatorId = UUID.Zero.ToString ();
                    }
                }
            }

            if (items.Length == 0)
                return null;

            return items[0];
        }

        public virtual InventoryFolderBase GetFolder(InventoryFolderBase folder)
        {
            InventoryFolderBase[] folders = m_Database.GetFolders (
                    new string[] { "folderID"},
                    new string[] { folder.ID.ToString() });

            if (folders.Length == 0)
                return null;

            return folders[0];
        }

        public virtual List<InventoryItemBase> GetActiveGestures(UUID principalID)
        {
            return new List<InventoryItemBase> (m_Database.GetActiveGestures (principalID));
        }

        public virtual int GetAssetPermissions(UUID principalID, UUID assetID)
        {
            return m_Database.GetAssetPermissions(principalID, assetID);
        }

        /// <summary>
        /// Does the user have an inventory?
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public bool HasInventoryForUser (UUID principalID)
        {
            return GetRootFolder (principalID) != null;
        }

        private bool ParentIsTrash(UUID folderID)
        {
            InventoryFolderBase[] folder = m_Database.GetFolders(new string[] {"folderID"}, new string[] {folderID.ToString()});
            if (folder.Length < 1)
                return false;

            if (folder[0].Type == (int)AssetType.TrashFolder)
                return true;

            UUID parentFolder = folder[0].ParentID;

            while (parentFolder != UUID.Zero)
            {
                InventoryFolderBase[] parent = m_Database.GetFolders (new string[] { "folderID" }, new string[] { parentFolder.ToString () });
                if (parent.Length < 1)
                    return false;

                if (parent[0].Type == (int)AssetType.TrashFolder)
                    return true;
                if (parent[0].Type == (int)AssetType.RootFolder)
                    return false;

                parentFolder = parent[0].ParentID;
            }
            return false;
        }
    }
}
