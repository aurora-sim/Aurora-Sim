using System;
using System.Collections.Generic;
using Aurora.DataManager.DataModels;
using NHibernate;
using OpenMetaverse;
using InventoryFolder = Aurora.DataManager.DataModels.InventoryFolder;

namespace Aurora.DataManager.Repositories
{
    public class InventoryRepository : DataManagerRepository, IInventoryData
    {
        public InventoryRepository(DataSessionProvider sessionProvider) : base(sessionProvider) { }
        public IList<InventoryObjectType> AllInventoryObjectTypes = new List<InventoryObjectType>();
        
        public InventoryFolder CreateFolderAndSave()
        {
            using(var session = OpenSession())
            {
                var folder = new InventoryFolder();
                session.SaveOrUpdate(folder);
                return folder;
            }
        }

        public IList<InventoryFolder> GetAllFolders()
        {
            using (var session = OpenSession())
            {
                return session.CreateCriteria(typeof(InventoryFolder)).List<InventoryFolder>();
            }
        }

        public bool CreateUserInventory(UUID user)
        {
            return true;
        }

        public IList<InventoryItem> GetActiveInventoryItemsByType(InventoryObjectType gestureType)
        {
            return new List<InventoryItem>();
        }

        /// <summary>
        /// Gets the folder that contains all folders.
        /// ParentID of this folder is UUID.Zero.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public InventoryFolder GetRootFolder(UUID user)
        {
            throw new NotImplementedException();
        }

        public InventoryFolder CreateFolderAndSave(UUID user, string folderRootName, UUID parentID)
        {
            throw new NotImplementedException();
        }

        public IList<InventoryFolder> GetSubfoldersWithAnyAssetPreferences(InventoryFolder defaultRootFolder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all folders that are in the root folder.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public IList<InventoryFolder> GetMainFolders(UUID user)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the first root folder that has the given name.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="folderRootName"></param>
        /// <returns></returns>
        public InventoryFolder GetMainFolderByName(UUID user, string folderRootName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new folder with the given name under the given folder.
        /// If the ObjectType.Type == 0, it is a normal folder with no PreferredAssetType.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parentFolder"></param>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public InventoryFolder CreateFolderUnderFolderAndSave(string name, InventoryFolder parentFolder, InventoryObjectType objectType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the folder the item is in
        /// </summary>
        /// <param name="baseItem"></param>
        /// <returns></returns>
        public InventoryFolder GetParentFolder(OpenSim.Framework.InventoryItemBase baseItem)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the folder the folder is in.
        /// Returns null if the root folder is queried.
        /// </summary>
        /// <param name="baseFolder"></param>
        /// <returns></returns>
        public InventoryFolder GetParentFolder(OpenSim.Framework.InventoryFolderBase baseFolder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the parentFolder to a new ID.
        /// </summary>
        /// <param name="IFFolder"></param>
        public void UpdateParentFolder(InventoryFolder IFFolder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes all given folders of the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="folderIds"></param>
        public void RemoveFolders(UUID userId, List<UUID> folderIds)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes all the sub units of the given folder.
        /// </summary>
        /// <param name="folder"></param>
        public void RemoveFoldersAndItems(OpenSim.Framework.InventoryFolderBase folder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all folders of the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<InventoryFolder> GetAllFolders(UUID userId)
        {
            throw new NotImplementedException();
        }

        #region IInventoryData

        /// <summary>
        /// Adds a new InventoryObjectType to the repository.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="assetType"></param>
        /// <returns></returns>
        public bool AssignNewInventoryType(string name, int assetType)
        {
            if (AllInventoryObjectTypes.Contains(GetInventoryObjectTypeByType(assetType)))
                return false;
            CreateInventoryType(name, assetType);
            return true;
        }
        
        public InventoryObjectType CreateInventoryType(string name, int assetType)
        {
            InventoryObjectType type = new InventoryObjectType();
            type.Name = name;
            type.Type = assetType;
            AllInventoryObjectTypes.Add(type);
            return type;
        }

        /// <summary>
        /// Gets the InventoryObjectType from its type identifier.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public InventoryObjectType GetInventoryObjectTypeByType(int type)
        {
            foreach (InventoryObjectType ot in AllInventoryObjectTypes)
            {
                if (ot.Type == type)
                    return ot;
            }
            return null;
        }

        /// <summary>
        /// Gets all the current InventoryObjectTypes.
        /// </summary>
        /// <returns></returns>
        public IList<InventoryObjectType> GetAllInventoryTypes()
        {
            return AllInventoryObjectTypes;
        }

        #endregion

        public IList<InventoryFolder> GetUserFolders(UUID userId)
        {
            throw new NotImplementedException();
        }
    }
}