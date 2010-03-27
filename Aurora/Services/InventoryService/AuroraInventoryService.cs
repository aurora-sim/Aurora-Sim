using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Aurora.DataManager.Repositories;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using Aurora.Framework;

namespace Aurora.Services.InventoryService
{
    /// <summary>
    /// This class will operate as a conversion layer for results from the inventory repository to the opensim types
    /// </summary>
    public class AuroraInventoryService : IInventoryPluginService, IInventoryService
    {
        private readonly InventoryObjectType animationType;
        private readonly InventoryObjectType bodyPartType;
        private readonly InventoryObjectType callingCardType;
        private readonly InventoryObjectType clothingType;
        private readonly InventoryObjectType gestureType;
        private readonly InventoryObjectType landmarkType;
        private readonly InventoryObjectType lostAndFoundType;
        private readonly InventoryObjectType notecardType;
        private readonly InventoryObjectType objectType;
        private readonly InventoryObjectType photoType;
        private readonly InventoryRepository repository;
        private readonly InventoryObjectType scriptType;
        private readonly InventoryObjectType soundType;
        private readonly InventoryObjectType textureType;
        private readonly InventoryObjectType trashType;

        private const string FOLDER_ANIMATION_NAME = "Animations";
        private const string FOLDER_BODY_PARTS_NAME = "Body Parts";
        private const string FOLDER_CALLING_CARDS_NAME = "Calling Cards";
        private const string FOLDER_CLOTHING_NAME = "Clothing";
        private const string FOLDER_GESTURES_NAME = "Gestures";
        private const string FOLDER_LANDMARKS_NAME = "Landmarks";
        private const string FOLDER_LOST_AND_FOUND_NAME = "Lost And Found";
        private const string FOLDER_NOTECARDS = "Notecards";
        private const string FOLDER_OBJECTS_NAME = "Objects";
        private const string FOLDER_PHOTO_ALBUM_NAME = "Photo Album";
        private const string FOLDER_ROOT_NAME = "My Inventory";
        private const string FOLDER_SCRIPTS_NAME = "Scripts";
        private const string FOLDER_SOUNDS_NAME = "Sounds";
        private const string FOLDER_TEXTURES_NAME = "Textures";
        private const string FOLDER_TRASH_NAME = "Trash";

        private List<IInventoryPlugin> Plugins = new List<IInventoryPlugin>();

        public AuroraInventoryService()
        {
            #region Get Plugins
            Plugins = Aurora.Framework.AuroraModuleLoader.PickupModules<IInventoryPlugin>(Environment.CurrentDirectory, "IInventoryPlugin");
            foreach (IInventoryPlugin plugin in Plugins)
            {
                plugin.Startup(this);
            }
            #endregion

            repository = new InventoryRepository(DataManager.DataManager.DataSessionProvider);
            animationType = repository.CreateInventoryType("ANIMATION", (int)AssetType.Animation);
            bodyPartType = repository.CreateInventoryType("BODY_PART", (int)AssetType.Bodypart);
            callingCardType = repository.CreateInventoryType("CALLING_CARD", (int)AssetType.CallingCard);
            clothingType = repository.CreateInventoryType("CLOTHING", (int)AssetType.Clothing);
            gestureType = repository.CreateInventoryType("GESTURE", (int)AssetType.Gesture);
            landmarkType = repository.CreateInventoryType("LANDMARK", (int)AssetType.Landmark);
            lostAndFoundType = repository.CreateInventoryType("LOST_AND_FOUND", (int)AssetType.LostAndFoundFolder);
            notecardType = repository.CreateInventoryType("NOTECARD", (int)AssetType.Notecard);
            objectType = repository.CreateInventoryType("OBJECT", (int)AssetType.Object);
            photoType = repository.CreateInventoryType("PHOTO", (int)AssetType.SnapshotFolder);
            scriptType = repository.CreateInventoryType("SCRIPT", (int)AssetType.LSLText);
            soundType = repository.CreateInventoryType("SOUND", (int)AssetType.Sound);
            textureType = repository.CreateInventoryType("TEXTURE", (int)AssetType.Texture);
            trashType = repository.CreateInventoryType("TRASH", (int)AssetType.TrashFolder);
        }

        #region IInventoryService Members

        #region Completed

        public bool CreateUserInventory(UUID user)
        {
            bool result = false;

            AuroraInventoryFolder defaultRootFolder = repository.GetRootFolder(user);
            if (defaultRootFolder == null)
            {
                defaultRootFolder = repository.CreateRootFolderAndSave(user, FOLDER_ROOT_NAME);
                result = true;
            }

            foreach (IInventoryPlugin plugin in Plugins)
            {
                plugin.CreateNewInventory(user, defaultRootFolder);
            }

            IList<AuroraInventoryFolder> defaultRootFolderSubFolders = repository.GetSubfoldersWithAnyAssetPreferences(defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_ANIMATION_NAME, animationType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_BODY_PARTS_NAME, bodyPartType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_CALLING_CARDS_NAME, callingCardType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_CLOTHING_NAME, clothingType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_GESTURES_NAME, gestureType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_LANDMARKS_NAME, landmarkType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_LOST_AND_FOUND_NAME, lostAndFoundType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_NOTECARDS, notecardType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_OBJECTS_NAME, objectType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_PHOTO_ALBUM_NAME, photoType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_OBJECTS_NAME, animationType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_SCRIPTS_NAME, scriptType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_SOUNDS_NAME, soundType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_TEXTURES_NAME, textureType, defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_TRASH_NAME, trashType, defaultRootFolder);

            return result;
        }

        public InventoryCollection GetUserInventory(UUID userId)
        {
            InventoryCollection collection = new InventoryCollection();
            collection.UserID = userId;
            List<InventoryFolderBase> Folders = new List<InventoryFolderBase>();
            List<InventoryItemBase> Items = new List<InventoryItemBase>();
            foreach (AuroraInventoryFolder folder in repository.GetAllFolders(userId))
            {
                Folders.Add(ConvertInventoryFolderToInventoryFolderBase(folder));
            }
            foreach (InventoryFolderBase folder in Folders)
            {
                Items.AddRange(GetFolderItems(userId, folder.ID));
            }
            collection.Folders = Folders;
            collection.Items = Items;

            return collection;
        }

        public bool MoveFolder(InventoryFolderBase folder)
        {
            AuroraInventoryFolder IFFolder = ConvertInventoryFolderBaseToInventoryFolder(folder);
            repository.UpdateParentFolder(IFFolder);
            return true;
        }

        public bool DeleteFolders(UUID userId, List<UUID> folderIds)
        {
            repository.RemoveFolders(folderIds);
            return true;
        }

        public bool PurgeFolder(InventoryFolderBase folder)
        {
            repository.RemoveFoldersAndItems(folder);
            return false;
        }

        public List<InventoryFolderBase> GetInventorySkeleton(UUID userId)
        {
            List<InventoryFolderBase> Folders = new List<InventoryFolderBase>();
            IList<AuroraInventoryFolder> folders = repository.GetAllFolders(userId);
            foreach (AuroraInventoryFolder folder in folders)
            {
                Folders.Add(ConvertInventoryFolderToInventoryFolderBase(folder));
            }
            return Folders;
        }

        public void GetUserInventory(UUID userId, InventoryReceiptCallback callback)
        {
            List<InventoryFolderImpl> Folders = new List<InventoryFolderImpl>();
            List<InventoryItemBase> Items = new List<InventoryItemBase>();
            foreach (AuroraInventoryFolder folder in repository.GetUserFolders(userId))
            {
                Folders.Add(new InventoryFolderImpl(ConvertInventoryFolderToInventoryFolderBase(folder)));
            }
            foreach (InventoryFolderBase folder in Folders)
            {
                Items.AddRange(GetFolderItems(userId, folder.ID));
            }


            Util.FireAndForget(delegate { callback(Folders, Items); });
        }

        public InventoryFolderBase GetRootFolder(UUID userId)
        {
            AuroraInventoryFolder folder = repository.GetRootFolder(userId);
            if (folder == null)
                return null;
            return ConvertInventoryFolderToInventoryFolderBase(folder);
        }

        public bool AddFolder(InventoryFolderBase folder)
        {
            AuroraInventoryFolder IFFolder = ConvertInventoryFolderBaseToInventoryFolder(folder);
            repository.CreateFolderUnderFolderAndSave(IFFolder.Name, IFFolder.ParentFolder, IFFolder.PreferredAssetType);
            return true;
        }

        public int GetAssetPermissions(UUID userId, UUID assetId)
        {
            InventoryFolderBase parent = GetRootFolder(userId);
            return FindAssetPerms(parent, assetId);
        }

        private int FindAssetPerms(InventoryFolderBase folder, UUID assetID)
        {
            InventoryCollection contents = GetFolderContent(folder.Owner, folder.ID);

            int perms = 0;
            foreach (InventoryItemBase item in contents.Items)
            {
                if (item.AssetID == assetID)
                    perms = (int)item.CurrentPermissions | perms;
            }

            foreach (InventoryFolderBase subfolder in contents.Folders)
                perms = perms | FindAssetPerms(subfolder, assetID);

            return perms;
        }

        public List<InventoryItemBase> GetActiveGestures(UUID userId)
        {
            IList<InventoryItem> activeGestures = repository.GetActiveInventoryItemsByType(gestureType);

            var osActiveGestures = new List<InventoryItemBase>();

            foreach (InventoryItem activeGesture in activeGestures)
            {
                osActiveGestures.Add(ConvertInventoryItemToInventoryItemBase(activeGesture));
            }

            return osActiveGestures;
        }

        public InventoryFolderBase GetFolderForType(UUID userId, AssetType type)
        {
            IList<AuroraInventoryFolder> Folders = repository.GetMainFolders(userId);
            foreach (AuroraInventoryFolder folder in Folders)
            {
                if (folder.PreferredAssetType == (int)type)
                {
                    return ConvertInventoryFolderToInventoryFolderBase(folder);
                }
            }

            return ConvertInventoryFolderToInventoryFolderBase(repository.CreateFolderUnderFolderAndSave(type.ToString(), repository.GetRootFolder(userId), (int)type));
        }

        public InventoryCollection GetFolderContent(UUID userId, UUID folderId)
        {
            InventoryCollection collection = new InventoryCollection();
            List<InventoryFolderBase> Folders = new List<InventoryFolderBase>();

            AuroraInventoryFolder newFolder = new AuroraInventoryFolder();
            newFolder.FolderId = folderId.ToString();

            IList<AuroraInventoryFolder> folders = repository.GetChildFolders(repository.GetFolder(newFolder));
            List<InventoryItemBase> Items = new List<InventoryItemBase>();

            foreach (AuroraInventoryFolder folder in folders)
            {
                Items.AddRange(GetFolderItems(userId, new UUID(folder.FolderId)));
                Folders.Add(ConvertInventoryFolderToInventoryFolderBase(folder));
            }

            collection.Folders = Folders;
            collection.Items = Items;
            collection.UserID = userId;

            return collection;
        }

        public bool UpdateFolder(InventoryFolderBase folder)
        {
            AuroraInventoryFolder IFFolder = ConvertInventoryFolderBaseToInventoryFolder(folder);
            repository.UpdateFolder(IFFolder);
            return true;
        }

        public InventoryItemBase GetItem(InventoryItemBase item)
        {
            return ConvertInventoryItemToInventoryItemBase(repository.GetItem(ConvertInventoryItemBaseToInventoryItem(item)));
        }

        public InventoryFolderBase GetFolder(InventoryFolderBase folder)
        {
            return ConvertInventoryFolderToInventoryFolderBase(repository.GetFolder(ConvertInventoryFolderBaseToInventoryFolder(folder)));
        }

        public bool AddItem(InventoryItemBase item)
        {
            repository.CreateItem(ConvertInventoryItemBaseToInventoryItem(item));
            return true;
        }

        public bool DeleteItems(UUID userId, List<UUID> itemIDs)
        {
            repository.RemoveItems(userId, itemIDs);
            return true;
        }

        public bool UpdateItem(InventoryItemBase item)
        {
            repository.UpdateItem(ConvertInventoryItemBaseToInventoryItem(item));
            return true;
        }

        public bool HasInventoryForUser(UUID userId)
        {
            return GetRootFolder(userId) != null;
        }

        public bool LinkItem(IClientAPI client, UUID oldItemID, UUID parentID, uint Callback)
        {
            InventoryItem item = new InventoryItem(oldItemID);
            item = repository.GetItem(item);
            repository.CreateLinkedItem(item);
            return true;
        }

        public bool MoveItems(UUID ownerId, List<InventoryItemBase> items)
        {
            List<InventoryItem> Items = new List<InventoryItem>();
            foreach (InventoryItemBase item in items)
                Items.Add(ConvertInventoryItemBaseToInventoryItem(item));

            repository.UpdateItems(Items);

            return true;
        }

        #endregion

        public List<InventoryItemBase> GetFolderItems(UUID userId, UUID folderId)
        {
            List<InventoryItem> items = repository.GetItemsInFolder(ConvertInventoryFolderBaseToInventoryFolder(GetFolder(new InventoryFolderBase(folderId))));
            List<InventoryItemBase> Items = new List<InventoryItemBase>();
            foreach (InventoryItem item in items)
            {
                Items.Add(ConvertInventoryItemToInventoryItemBase(item));
            }
            return Items;
        }

        #endregion

        #region IInventoryPluginService

        public void AddInventoryItemType(InventoryObjectType type)
        {
            repository.AssignNewInventoryType(type.Name, type.Type);
        }

        public void EnsureFolderForPreferredTypeUnderFolder(string folderName, InventoryObjectType inventoryObjectType, AuroraInventoryFolder defaultRootFolder)
        {
            if (!DoesFolderExistForPreferedType(defaultRootFolder, inventoryObjectType))
            {
                repository.CreateFolderUnderFolderAndSave(folderName, defaultRootFolder, animationType.Type);
            }
        }

        public bool DoesFolderExistForPreferedType(AuroraInventoryFolder folder, InventoryObjectType inventoryObjectType)
        {
            return (from f in repository.GetChildFolders(folder) where f.PreferredAssetType == inventoryObjectType.Type select f).Count() > 0;
        }

        public bool AddInventoryItem(UUID user, InventoryItem item)
        {
            repository.CreateItem(item);
            return true;
        }

        public InventoryFolderBase ConvertInventoryFolderToInventoryFolderBase(AuroraInventoryFolder folder)
        {
            InventoryFolderBase IFfolder = new InventoryFolderBase();
            IFfolder.ID = new UUID(folder.FolderId);
            IFfolder.Name = folder.Name;
            IFfolder.Owner = new UUID(folder.Owner);
            IFfolder.ParentID = new UUID(folder.ParentFolder.FolderId);
            IFfolder.Type = (short)folder.PreferredAssetType;
            IFfolder.Version = 1;
            return IFfolder;
        }

        public InventoryItemBase ConvertInventoryItemToInventoryItemBase(InventoryItem item)
        {
            var inventoryItemBase = new InventoryItemBase();
            inventoryItemBase.AssetID = item.AssetUUID;
            inventoryItemBase.AssetType = (int)item.AssetType;
            inventoryItemBase.BasePermissions = (uint)item.Permissions.BaseMask;
            inventoryItemBase.CreationDate = DateTimeToInt(item.CreationDate);
            inventoryItemBase.CreatorId = item.CreatorID.ToString();
            inventoryItemBase.CreatorIdAsUuid = item.CreatorID;
            inventoryItemBase.CurrentPermissions = (uint)item.Permissions.OwnerMask;
            inventoryItemBase.Description = item.Description;
            inventoryItemBase.EveryOnePermissions = (uint)item.Permissions.EveryoneMask;
            inventoryItemBase.Flags = item.Flags;
            inventoryItemBase.Folder = new UUID(repository.GetParentFolder(inventoryItemBase).FolderId);
            inventoryItemBase.GroupID = item.GroupID;
            inventoryItemBase.GroupOwned = item.GroupOwned;
            inventoryItemBase.GroupPermissions = (uint)item.Permissions.GroupMask;
            inventoryItemBase.ID = item.UUID;
            inventoryItemBase.InvType = (int)item.InventoryType;
            inventoryItemBase.Name = item.Name;
            inventoryItemBase.NextPermissions = (uint)item.Permissions.NextOwnerMask;
            inventoryItemBase.Owner = item.OwnerID;
            inventoryItemBase.SalePrice = item.SalePrice;
            inventoryItemBase.SaleType = Convert.ToByte(item.SaleType);

            return inventoryItemBase;
        }

        private readonly DateTime unixEpoch =
            DateTime.ParseExact("1970-01-01 00:00:00 +0", "yyyy-MM-dd hh:mm:ss z", DateTimeFormatInfo.InvariantInfo).ToUniversalTime();

        private int DateTimeToInt(DateTime stamp)
        {
            TimeSpan t = stamp.ToUniversalTime() - unixEpoch;
            return (int)t.TotalSeconds;
        }

        private DateTime IntToDateTime(int stamp)
        {
            TimeSpan t = new TimeSpan(stamp);
            DateTime time = new DateTime(t.Ticks + unixEpoch.Ticks, DateTimeKind.Utc);
            return time;
        }

        public InventoryItem ConvertInventoryItemBaseToInventoryItem(InventoryItemBase item)
        {
            InventoryItem IIitem = new InventoryItem(item.ID);
            IIitem.AssetType = (AssetType)item.AssetType;
            IIitem.AssetUUID = item.AssetID;
            IIitem.CreationDate = IntToDateTime(item.CreationDate);
            IIitem.CreatorID = item.CreatorIdAsUuid;
            IIitem.Description = item.Description;
            IIitem.Flags = item.Flags;
            IIitem.GroupID = item.GroupID;
            IIitem.GroupOwned = item.GroupOwned;
            IIitem.InventoryType = (InventoryType)item.InvType;
            IIitem.LastOwnerID = UUID.Zero;
            IIitem.Name = item.Name;
            IIitem.OwnerID = item.Owner;
            IIitem.ParentUUID = UUID.Zero;
            Permissions permission = new Permissions();
            permission.BaseMask = (PermissionMask)item.BasePermissions;
            permission.EveryoneMask = (PermissionMask)item.EveryOnePermissions;
            permission.GroupMask = (PermissionMask)item.GroupPermissions;
            permission.NextOwnerMask = (PermissionMask)item.NextPermissions;
            permission.OwnerMask = (PermissionMask)item.CurrentPermissions;
            IIitem.Permissions = permission;
            IIitem.SalePrice = item.SalePrice;
            IIitem.SaleType = (SaleType)item.SaleType;
            IIitem.TransactionID = UUID.Zero;
            IIitem.UUID = item.ID;
            return IIitem;
        }

        public AuroraInventoryFolder ConvertInventoryFolderBaseToInventoryFolder(InventoryFolderBase folder)
        {
            AuroraInventoryFolder IFfolder = new AuroraInventoryFolder();
            IFfolder.FolderId = folder.ID.ToString();
            IFfolder.Name = folder.Name;
            IFfolder.Owner = folder.Owner.ToString();
            IFfolder.ParentFolder = repository.GetParentFolder(folder);
            InventoryObjectType type = repository.GetInventoryObjectTypeByType(folder.Type);
            if (type == null)
                IFfolder.PreferredAssetType = repository.CreateInventoryType(folder.Name, folder.Type).Type;
            else
                IFfolder.PreferredAssetType = type.Type;
            return IFfolder;
        }

        #endregion
    }
}