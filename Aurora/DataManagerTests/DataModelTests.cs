using Aurora.DataManager.Repositories;
using NUnit.Framework;

namespace Aurora.DataManager.Tests
{
    public class DataModelTests
    {
        [Test]
        public void InventoryRepositoryTests()
        {
            var repo = new InventoryRepository(new DataSessionProvider("InventoryRepoTest"));
            var folder = repo.CreateFolderAndSave();
            var folders = repo.GetAllFolders();
        }
    }
}