using System.Collections.Generic;
using System.Linq;
using Aurora.DataManager.DataModels;
using Aurora.DataManager.Repositories;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using InventoryFolder = Aurora.DataManager.DataModels.InventoryFolder;

namespace Aurora.Services.InventoryService
{
    /// <summary>
    /// This class will operate as a conversion layer for results from the inventory repository to the opensim types
    /// </summary>
    public class AuroraInventoryService : IInventoryService
    {
        private readonly InventoryObjectType animationType;
        private readonly InventoryObjectType bodyPartType;
        private readonly InventoryObjectType callingCardType;
        private readonly InventoryObjectType clothingType;
        private readonly InventoryObjectType gestureType;
        private readonly IList<InventoryObjectType> inventoryTypes;
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

        public AuroraInventoryService()
        {
            repository = new InventoryRepository(DataManager.DataManager.DataSessionProvider);
            inventoryTypes = repository.GetAllInventoryTypes();
            animationType = GetOrCreateInventoryTypeByName("ANIMATION");
            gestureType = GetOrCreateInventoryTypeByName("GESTURE");
            bodyPartType = GetOrCreateInventoryTypeByName("BODY_PART");
            callingCardType = GetOrCreateInventoryTypeByName("CALLING_CARD");
            clothingType = GetOrCreateInventoryTypeByName("CLOTHING");
            gestureType = GetOrCreateInventoryTypeByName("GESTURE");
            landmarkType = GetOrCreateInventoryTypeByName("LANDMARK");
            lostAndFoundType = GetOrCreateInventoryTypeByName("LOST_AND_FOUND");
            notecardType = GetOrCreateInventoryTypeByName("NOTECARD");
            objectType = GetOrCreateInventoryTypeByName("OBJECT");
            photoType = GetOrCreateInventoryTypeByName("PHOTO");
            scriptType = GetOrCreateInventoryTypeByName("SCRIPT");
            soundType = GetOrCreateInventoryTypeByName("SOUND");
            textureType = GetOrCreateInventoryTypeByName("TEXTURE");
            trashType = GetOrCreateInventoryTypeByName("TRASH");
        }

        #region IInventoryService Members

        public bool CreateUserInventory(UUID user)
        {
            bool result = false;

            InventoryFolder defaultRootFolder = repository.GetRootFolderByName(user, FOLDER_ROOT_NAME);
            if (defaultRootFolder == null)
            {
                defaultRootFolder = repository.CreateFolderAndSave(user, FOLDER_ROOT_NAME);
                result = true;
            }

            IList<InventoryFolder> defaultRootFolderSubFolders = repository.GetSubfoldersWithAnyAssetPreferences(defaultRootFolder);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_ANIMATION_NAME, animationType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_BODY_PARTS_NAME, bodyPartType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_CALLING_CARDS_NAME, callingCardType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_CLOTHING_NAME, clothingType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_GESTURES_NAME, gestureType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_LANDMARKS_NAME, landmarkType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_LOST_AND_FOUND_NAME, lostAndFoundType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_NOTECARDS, notecardType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_OBJECTS_NAME, objectType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_PHOTO_ALBUM_NAME, photoType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_OBJECTS_NAME, animationType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_SCRIPTS_NAME, scriptType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_SOUNDS_NAME, soundType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_TEXTURES_NAME, textureType, defaultRootFolder, defaultRootFolderSubFolders);
            EnsureFolderForPreferredTypeUnderFolder(FOLDER_TRASH_NAME, trashType, defaultRootFolder, defaultRootFolderSubFolders);

            return result;
        }

        public List<InventoryFolderBase> GetInventorySkeleton(UUID userId)
        {
            return null;
        }

        public InventoryCollection GetUserInventory(UUID userId)
        {
            return new InventoryCollection();
        }

        public void GetUserInventory(UUID userId, InventoryReceiptCallback callback)
        {
        }

        public InventoryFolderBase GetRootFolder(UUID userId)
        {
            return new InventoryFolderBase();
        }

        public InventoryFolderBase GetFolderForType(UUID userId, AssetType type)
        {
            return new InventoryFolderBase();
        }

        public InventoryCollection GetFolderContent(UUID userId, UUID folderId)
        {
            return new InventoryCollection();
        }

        public List<InventoryItemBase> GetFolderItems(UUID userId, UUID folderId)
        {
            return new List<InventoryItemBase>();
        }

        public bool AddFolder(InventoryFolderBase folder)
        {
            return false;
        }

        public bool UpdateFolder(InventoryFolderBase folder)
        {
            return false;
        }

        public bool MoveFolder(InventoryFolderBase folder)
        {
            return false;
        }

        public bool DeleteFolders(UUID userId, List<UUID> folderIds)
        {
            return false;
        }

        public bool PurgeFolder(InventoryFolderBase folder)
        {
            return false;
        }

        public bool AddItem(InventoryItemBase item)
        {
            return false;
        }

        public bool UpdateItem(InventoryItemBase item)
        {
            return false;
        }

        public bool DeleteItems(UUID userId, List<UUID> itemIDs)
        {
            return false;
        }

        public InventoryItemBase GetItem(InventoryItemBase item)
        {
            return new InventoryItemBase();
        }

        public InventoryFolderBase GetFolder(InventoryFolderBase folder)
        {
            return new InventoryFolderBase();
        }

        public int GetAssetPermissions(UUID userId, UUID assetId)
        {
            return 0;
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

        public bool HasInventoryForUser(UUID userId)
        {
            return false;
        }

        public bool MoveItems(UUID ownerId, List<InventoryItemBase> items)
        {
            return false;
        }

        #endregion

        private InventoryItemBase ConvertInventoryItemToInventoryItemBase(InventoryItem activeGesture)
        {
            var inventoryItemBase = new InventoryItemBase();

            //TODO: translate values

            return inventoryItemBase;
        }

        private InventoryObjectType GetOrCreateInventoryTypeByName(string name)
        {
            InventoryObjectType iot = (from it in inventoryTypes where it.Name == name select it).First();
            if (iot == null)
            {
                iot = repository.CreateInventoryTypeAndSave(name);
            }
            return iot;
        }

        private void EnsureFolderForPreferredTypeUnderFolder(string folderAnimationName, InventoryObjectType inventoryObjectType, InventoryFolder defaultRootFolder, IList<InventoryFolder> defaultRootFolderSubFolders)
        {
            if (!DoesFolderExistForPreferedType(defaultRootFolderSubFolders, animationType))
            {
                repository.CreateFolderUnderFolderAndSave(FOLDER_ANIMATION_NAME, defaultRootFolder, animationType);
            }
        }

        private bool DoesFolderExistForPreferedType(IList<InventoryFolder> folders, InventoryObjectType inventoryObjectType)
        {
            return (from f in folders where f.PreferredAssetType == inventoryObjectType select f).Count() > 0;
        }
    }
}