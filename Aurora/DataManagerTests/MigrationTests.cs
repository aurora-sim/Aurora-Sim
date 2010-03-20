using System;
using System.Collections.Generic;
using System.IO;
using Aurora.DataManager.Migration;
using Aurora.DataManager.Migration.Migrators;
using Aurora.DataManager.SQLite;
using NUnit.Framework;

namespace Aurora.DataManager.Tests
{
    public class MigrationTests 
    {
        private string dbFileName = "TestMigration.db";

        [Test]
        public void MigrationTestsTests()
        {
            CreateEmptyDatabase();
            DataSessionProvider sessionProvider = new DataSessionProvider(dbFileName);
            IGenericData genericData = new SQLiteLoader();
            genericData.ConnectToDatabase(string.Format("URI=file:{0},version=3",dbFileName));
            genericData.CloseDatabase();
            CreateEmptyDatabase();

            var migrators = new List<Migrator>();
            var testMigrator0 = new AuroraMigrator_2010_03_13();
            migrators.Add(testMigrator0);

            var migrationManager = new MigrationManager(sessionProvider, genericData, migrators);
            Assert.AreEqual(testMigrator0.Version, migrationManager.LatestVersion, "Latest version is correct");
            Assert.IsNull(migrationManager.GetDescriptionOfCurrentOperation(),"Description should be null before deciding what to do.");
            migrationManager.DetermineOperation();
            var operationDescription = migrationManager.GetDescriptionOfCurrentOperation();
            Assert.AreEqual(MigrationOperationTypes.CreateDefaultAndUpgradeToTarget, operationDescription.OperationType, "Operation type is correct.");
            Assert.AreEqual(testMigrator0.Version, operationDescription.CurrentVersion, "Current version is correct");
            //There will be no migration because there is only one migrator which will provide the default
            Assert.IsNull(operationDescription.StartVersion, "Start migration version is correct");
            Assert.IsNull(operationDescription.EndVersion, "End migration version is correct");
            try
            {
                migrationManager.ExecuteOperation();
                Assert.AreEqual(testMigrator0.Version, genericData.GetAuroraVersion(), "Version of settings is updated");
            }
            catch(MigrationOperationException e)
            {
                Assert.Fail("Something failed during execution we weren't expecting.");  
            }
            bool valid = migrationManager.ValidateVersion(migrationManager.LatestVersion);
            Assert.AreEqual(true,valid,"Database is a valid version");

            migrationManager.DetermineOperation();
            var operationDescription2 = migrationManager.GetDescriptionOfCurrentOperation();
            Assert.AreEqual(MigrationOperationTypes.DoNothing, operationDescription2.OperationType, "Operation type is correct.");
            Assert.AreEqual(testMigrator0.Version, operationDescription2.CurrentVersion, "Current version is correct");
            Assert.IsNull(operationDescription2.StartVersion, "Start migration version is correct");
            Assert.IsNull(operationDescription2.EndVersion, "End migration version is correct");
            migrationManager.ExecuteOperation();
            
            genericData.CloseDatabase();
            //empty the database just to be safe for other tests
            CreateEmptyDatabase();
        }

        private void CreateEmptyDatabase()
        {
            if( File.Exists(dbFileName))
            {
                File.Delete(dbFileName);
            }
        }
    }
}