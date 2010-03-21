using Aurora.DataManager.Repositories;
using NUnit.Framework;
using OpenMetaverse;

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

            DataSessionProvider sessionProvider = new DataSessionProvider("InventoryRepoTest.db");
            sessionProvider.DeleteLocalResources();
            var repo = new InventoryRepository(sessionProvider);

            var folder = repo.CreateRootFolderAndSave(userId, MY_INVENTORY);
            Assert.AreEqual(MY_INVENTORY, folder.Name);
            Assert.IsNull(folder.ParentFolder);
            Assert.AreEqual(userId.ToString(), folder.Owner);

            var rootFolder = repo.GetRootFolder(userId);
            Assert.AreEqual(folder.Id, rootFolder.Id);
            Assert.AreEqual(MY_INVENTORY, rootFolder.Name);
            Assert.IsNull(rootFolder.ParentFolder);
            Assert.AreEqual(userId.ToString(), rootFolder.Owner);


            var testFolder = repo.CreateFolderAndSave(TEST_FOLDER, rootFolder);
            Assert.AreEqual(TEST_FOLDER, testFolder.Name);
            Assert.AreEqual(rootFolder.Id, testFolder.ParentFolder.Id);
            Assert.AreEqual(rootFolder.Owner, testFolder.Owner);

            var childFolders = repo.GetChildFolders(rootFolder);
            Assert.AreEqual(1, childFolders.Count);
            Assert.AreEqual(testFolder.Id, childFolders[0].Id);
            Assert.AreEqual(TEST_FOLDER, childFolders[0].Name);
            Assert.AreEqual(rootFolder.Id, childFolders[0].ParentFolder.Id);
            Assert.AreEqual(rootFolder.Owner, childFolders[0].Owner);
        }
    }
}