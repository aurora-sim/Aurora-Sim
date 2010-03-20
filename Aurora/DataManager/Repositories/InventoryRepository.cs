using System;
using System.Collections.Generic;
using Aurora.DataManager.DataModels;
using NHibernate;
using OpenMetaverse;
using InventoryFolder = Aurora.DataManager.DataModels.InventoryFolder;

namespace Aurora.DataManager.Repositories
{
    public class InventoryRepository : DataManagerRepository
    {
        public InventoryRepository(DataSessionProvider sessionProvider) : base(sessionProvider) { }

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

        public IList<InventoryObjectType> GetAllInventoryTypes()
        {
            return new List<InventoryObjectType>();
        }

        public bool CreateUserInventory(UUID user)
        {
            return true;
        }

        public IList<InventoryItem> GetActiveInventoryItemsByType(InventoryObjectType gestureType)
        {
            return new List<InventoryItem>();
        }

        public IList<InventoryFolder> GetRootFolders(UUID user)
        {
            throw new NotImplementedException();
        }

        public InventoryFolder CreateFolderAndSave(UUID user, string folderRootName)
        {
            throw new NotImplementedException();
        }

        public IList<InventoryFolder> GetSubfoldersWithAnyAssetPreferences(InventoryFolder defaultRootFolder)
        {
            throw new NotImplementedException();
        }

        public InventoryFolder GetRootFolderByName(UUID user, string folderRootName)
        {
            throw new NotImplementedException();
        }

        public InventoryFolder CreateFolderUnderFolderAndSave(string name, InventoryFolder parentFolder, object folderAnimationName)
        {
            throw new NotImplementedException();
        }

        public InventoryObjectType CreateInventoryTypeAndSave(string name)
        {
            return null;
        }
    }
}