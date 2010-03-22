using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public interface IInventoryPlugin
    {
        void Startup(IInventoryPluginService service);
        void CreateNewInventory(UUID user, InventoryFolder defaultRootFolder);
    }
    public interface IInventoryPluginService
    {
        void AddInventoryItemType(InventoryObjectType type);
        void EnsureFolderForPreferredTypeUnderFolder(string folderName, InventoryObjectType inventoryObjectType, InventoryFolder defaultRootFolder);
        bool DoesFolderExistForPreferedType(InventoryFolder folder, InventoryObjectType inventoryObjectType);
        bool AddInventoryItem(UUID user, InventoryItem item);
        InventoryFolderBase ConvertInventoryFolderToInventoryFolderBase(InventoryFolder folder);
        InventoryItemBase ConvertInventoryItemToInventoryItemBase(InventoryItem item);
        InventoryItem ConvertInventoryItemBaseToInventoryItem(InventoryItemBase item);
        InventoryFolder ConvertInventoryFolderBaseToInventoryFolder(InventoryFolderBase folder);
    }
}
