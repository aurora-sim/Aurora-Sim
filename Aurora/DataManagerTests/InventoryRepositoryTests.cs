using System.IO;
using Aurora.DataManager.Repositories;
using NUnit.Framework;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.DataManager.Tests
{
    public class InventoryRepositoryTests
    {
        [Test]
        public void BasicTests()
        {
            var userId = UUID.Random();
            var MY_INVENTORY = "My Inventory";
            string TEST_FOLDER = "Test Folder";
            string file = "InventoryRepoTest.db";



            DataSessionProvider sessionProvider = new DataSessionProvider(DataManagerTechnology.SQLite, string.Format("URI=file:{0},version=3", file));
            if(File.Exists(file))
            {
                File.Delete(file);
            }
            var repo = new InventoryRepository(sessionProvider);

            var folder = repo.CreateRootFolderAndSave(userId, MY_INVENTORY);
            Assert.AreEqual(MY_INVENTORY, folder.Name);
            Assert.IsNull(folder.ParentFolder);
            Assert.AreEqual(userId.ToString(), folder.Owner);

            var rootFolder = repo.GetRootFolder(userId);
            Assert.AreEqual(folder.ID, rootFolder.ID);
            Assert.AreEqual(MY_INVENTORY, rootFolder.Name);
            Assert.IsNull(rootFolder.ParentFolder);
            Assert.AreEqual(userId.ToString(), rootFolder.Owner);


            var testFolder = repo.CreateFolderAndSave(TEST_FOLDER, rootFolder);
            Assert.AreEqual(TEST_FOLDER, testFolder.Name);
            Assert.AreEqual(rootFolder.ID, testFolder.ParentFolder.ID);
            Assert.AreEqual(rootFolder.Owner, testFolder.Owner);

            var childFolders = repo.GetChildFolders(rootFolder);
            Assert.AreEqual(1, childFolders.Count);
            Assert.AreEqual(testFolder.ID, childFolders[0].ID);
            Assert.AreEqual(TEST_FOLDER, childFolders[0].Name);
            Assert.AreEqual(rootFolder.ID, childFolders[0].ParentFolder.ID);
            Assert.AreEqual(rootFolder.Owner, childFolders[0].Owner);
        }
    }
}