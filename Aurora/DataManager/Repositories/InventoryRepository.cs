using System;
using System.Collections.Generic;
using System.Diagnostics;
using NHibernate.Criterion;
using Aurora.Framework;
using OpenMetaverse;

namespace Aurora.DataManager.Repositories
{
    public class InventoryRepository : DataManagerRepository
    {
        public InventoryRepository(DataSessionProvider sessionProvider) : base(sessionProvider) { }

        /// <summary>
        /// Gets the folder that contains all folders.
        /// ParentID of this folder is UUID.Zero.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public AuroraInventoryFolder GetRootFolder(UUID user)
        {
            using (var session = OpenSession())
            {
                var rootFolders = session.CreateCriteria(typeof(AuroraInventoryFolder)).Add(Expression.IsNull("ParentFolder")).List<AuroraInventoryFolder>();
                if (rootFolders.Count > 0)
                {
                    Debug.Assert(rootFolders.Count == 1, "This is unexpected that we have more than one root folder.");
                    return rootFolders[0];
                }
            }
            return null;
        }

        public AuroraInventoryFolder CreateRootFolderAndSave(UUID owner, string folderRootName)
        {
            using (var session = OpenSession())
            {
                var folder = new AuroraInventoryFolder();
                folder.Owner = owner.ToString();
                folder.Name = folderRootName;
                folder.FolderID = UUID.Random().ToString();
                session.SaveOrUpdate(folder);
                return folder;
            }
        }

        public AuroraInventoryFolder CreateFolderAndSave(string folderName, AuroraInventoryFolder parentFolder)
        {
            using (var session = OpenSession())
            {
                var folder = new AuroraInventoryFolder();
                folder.Owner = parentFolder.Owner.ToString();
                folder.Name = folderName;
                folder.ParentFolder = parentFolder;
                folder.FolderID = UUID.Random().ToString();
                session.SaveOrUpdate(folder);
                return folder;
            }
        }

        public IList<AuroraInventoryFolder> GetSubfoldersWithAnyAssetPreferences(AuroraInventoryFolder rootFolder)
        {
            using (var session = OpenSession())
            {
                return session.CreateCriteria(typeof(AuroraInventoryFolder)).Add(Expression.Eq("ParentFolder", rootFolder)).Add(Expression.IsNotNull("PreferredAssetType")).List<AuroraInventoryFolder>();
            }
        }

        /// <summary>
        /// Gets all folders that are in the root folder.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public IList<AuroraInventoryFolder> GetMainFolders(UUID user)
        {
            using (var session = OpenSession())
            {
                return session.CreateCriteria(typeof(AuroraInventoryFolder)).Add(Expression.Eq("ParentFolder", GetRootFolder(user))).List<AuroraInventoryFolder>();
            }
        }

        /// <summary>
        /// Gets the first root folder that has the given name.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public AuroraInventoryFolder GetMainFolderByName(UUID user, string name)
        {
            using (var session = OpenSession())
            {
                var rootSubFolders = session.CreateCriteria(typeof(AuroraInventoryFolder)).Add(Expression.Eq("ParentFolder", GetRootFolder(user))).Add(Expression.Eq("Name", name)).List<AuroraInventoryFolder>();
                if (rootSubFolders.Count > 0)
                {
                    return rootSubFolders[0];
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new folder with the given name under the given folder.
        /// If the ObjectType.Type == 0, it is a normal folder with no PreferredAssetType.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parentFolder"></param>
        /// <param name="preferredType"></param>
        /// <returns></returns>
        public AuroraInventoryFolder CreateFolderUnderFolderAndSave(string folderName, AuroraInventoryFolder parentFolder, int preferredType)
        {
            using (var session = OpenSession())
            {
                var folder = new AuroraInventoryFolder();
                folder.Owner = parentFolder.Owner.ToString();
                folder.Name = folderName;
                folder.ParentFolder = parentFolder;
                folder.PreferredAssetType = preferredType;
                session.SaveOrUpdate(folder);
                return folder;
            }
        }

        /// <summary>
        /// Gets the folder the item is in
        /// </summary>
        /// <param name="baseItem"></param>
        /// <returns></returns>
        public AuroraInventoryFolder GetParentFolderOfFolder(AuroraInventoryItem baseItem)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the folder the folder is in.
        /// Returns null if the root folder is queried.
        /// </summary>
        /// <param name="baseFolder"></param>
        /// <returns></returns>
        public AuroraInventoryFolder GetParentFolderOfFolder(UUID id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the parentFolder to a new ID.
        /// </summary>
        /// <param name="IFFolder"></param>
        public void UpdateParentFolder(AuroraInventoryFolder folder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes all given folders of the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="folderIds"></param>
        public void RemoveFolders(List<UUID> folderIds)
        {
            using (var session = OpenSession())
            {
                var rootSubFolders = session.CreateCriteria(typeof(AuroraInventoryFolder)).List<AuroraInventoryFolder>();
                foreach (var inventoryFolder in rootSubFolders)
                {
                    if (folderIds.Contains(UUID.Parse(inventoryFolder.FolderID)))
                    {
                        session.Delete(inventoryFolder);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all the sub units of the given folder.
        /// </summary>
        /// <param name="folder"></param>
        public void RemoveFoldersAndItems(UUID folder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all folders of the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IList<AuroraInventoryFolder> GetAllFolders(UUID userId)
        {
            using (var session = OpenSession())
            {
                return session.CreateCriteria(typeof(AuroraInventoryFolder)).Add(Expression.Eq("ParentFolder", GetRootFolder(userId))).List<AuroraInventoryFolder>();
            }
        }


        public IList<AuroraInventoryFolder> GetChildFolders(AuroraInventoryFolder parentFolder)
        {
            using (var session = OpenSession())
            {
                return session.CreateCriteria(typeof(AuroraInventoryFolder)).Add(Expression.Eq("ParentFolder", parentFolder)).List<AuroraInventoryFolder>();
            }
        }

        public IList<AuroraInventoryFolder> GetUserFolders(UUID userId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrives the AuroraInventoryFolder by its ID.
        /// </summary>
        /// <param name="inventoryFolder"></param>
        /// <returns></returns>
        public AuroraInventoryFolder GetFolder(AuroraInventoryFolder inventoryFolder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrives the InventoryItem by its ID.
        /// </summary>
        public AuroraInventoryItem GetItem(UUID uuid)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrives the InventoryItem by its ID.
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public AuroraInventoryItem GetFolder(UUID uuid)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the parentID, name, and owner
        /// </summary>
        /// <param name="folder"></param>
        public void UpdateFolder(AuroraInventoryFolder folder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes all InventoryItems with the specified UUIDs.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="itemIDs"></param>
        public void RemoveItems(UUID userId, List<UUID> itemIDs)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new InventoryItem based off the InventoryItem given.
        /// </summary>
        /// <param name="inventoryItem"></param>
        public void CreateItem(AuroraInventoryItem inventoryItem)
        {
            using (var session = OpenSession())
            {
                session.SaveOrUpdate(inventoryItem);
            }
        }

        public void CreateItems(IList<AuroraInventoryItem> inventoryItems)
        {
            using (var session = OpenSession())
            {
                foreach (var item in inventoryItems)
                {
                    session.SaveOrUpdate(item);
                }
            }
        }

        public void UpdateItem(AuroraInventoryItem inventoryItem)
        {
            using (var session = OpenSession())
            {
                session.SaveOrUpdate(inventoryItem);
            }
        }

        public void UpdateItems(IList<AuroraInventoryItem> inventoryItems)
        {
            using (var session = OpenSession())
            {
                foreach (var item in inventoryItems)
                {
                    session.SaveOrUpdate(item);
                }
            }
        }

        /// <summary>
        /// Creates a new item with a new UUID, but has a parentItem
        /// </summary>
        /// <param name="item"></param>
        public void CreateLinkedItem(AuroraInventoryItem item)
        {
            
        }

        public IList<AuroraInventoryItem> GetItemsInFolder(UUID userID, AuroraInventoryFolder inventoryFolder)
        {
            using (var session = OpenSession())
            {
                return session.CreateCriteria(typeof(AuroraInventoryItem)).Add(Expression.Eq("OwnerID", userID)).Add(Expression.Eq("ParentFolder", inventoryFolder)).List<AuroraInventoryItem>();
            }
        }

        public IList<AuroraInventoryItem> GetActiveInventoryItemsByType(UUID userID, int assetType)
        {
            using (var session = OpenSession())
            {
                return session.CreateCriteria(typeof(AuroraInventoryItem)).Add(Expression.Eq("OwnerID", userID)).Add(Expression.Eq("AssetType", assetType)).List<AuroraInventoryItem>();
            }
        }

        public AuroraInventoryFolder GetParentFolderOfItem(UUID id)
        {
            using (var session = OpenSession())
            {
                var rootFolders = session.CreateCriteria(typeof(AuroraInventoryFolder)).SetFetchSize(1).Add(Expression.Eq("FolderID",id)).List<AuroraInventoryFolder>();
                if (rootFolders.Count > 0)
                {
                    return rootFolders[0];
                }
            }
            return null;
        }
    }
}