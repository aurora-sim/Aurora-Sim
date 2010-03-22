using System;
using System.Collections.Generic;
using System.IO;
using Aurora.DataManager.Migration;
using Aurora.DataManager.Migration.Migrators;
using Aurora.DataManager.SQLite;
using C5;
using NUnit.Framework;

namespace Aurora.DataManager.Tests
{
    public class MigrationTests 
    {
        private string dbFileName = "TestMigration.db";

        public class TestMigrator : Migrator
        {
            public TestMigrator()
            {
                Version = new Version(2010, 3, 13);
                CanProvideDefaults = true;

                schema = new List<Rec<string, ColumnDefinition[]>>();

                AddSchema("test_table", ColDefs(
                    ColDef("id", ColumnTypes.Integer),
                    ColDef("test_string", ColumnTypes.String),
                    ColDef("test_string1", ColumnTypes.String1),
                    ColDef("test_string2", ColumnTypes.String2),
                    ColDef("test_string45", ColumnTypes.String45),
                    ColDef("test_string50", ColumnTypes.String50),
                    ColDef("test_string100", ColumnTypes.String100),
                    ColDef("test_string512", ColumnTypes.String512),
                    ColDef("test_string1024", ColumnTypes.String1024),
                    ColDef("test_date", ColumnTypes.Date)
                    ));
            }

            protected override void DoCreateDefaults(DataSessionProvider sessionProvider, IGenericData genericData)
            {
                EnsureAllTablesInSchemaExist(genericData);
            }

            protected override bool DoValidate(DataSessionProvider sessionProvider, IGenericData genericData)
            {
                return TestThatAllTablesValidate(genericData);
            }

            protected override void DoMigrate(DataSessionProvider sessionProvider, IGenericData genericData)
            {
                DoCreateDefaults(sessionProvider, genericData);
            }

            protected override void DoPrepareRestorePoint(DataSessionProvider sessionProvider, IGenericData genericData)
            {
                CopyAllTablesToTempVersions(genericData);
            }

            public override void DoRestore(DataSessionProvider sessionProvider, IGenericData genericData)
            {
                RestoreTempTablesToReal(genericData);
            }
        }

        [Test]
        public void MigrationTestsTests()
        {
            CreateEmptyDatabase();
            DataSessionProvider sessionProvider = new DataSessionProvider(dbFileName);
            IGenericData genericData = new SQLiteLoader();
            genericData.ConnectToDatabase(string.Format("URI=file:{0},version=3",dbFileName));
            genericData.CloseDatabase();

            var migrators = new List<Migrator>();
            var testMigrator0 = new TestMigrator();
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