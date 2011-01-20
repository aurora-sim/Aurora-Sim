using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuroraMigrator_2011_1_16 : Migrator
    {
        public AuroraMigrator_2011_1_16()
        {
            Version = new Version(2011, 1, 16);
            CanProvideDefaults = true;

            schema = new AuroraMigrator_2011_1_15().schema;
            renameSchema = new Dictionary<string, string>();

            //Added tables

            //
            // Change summery:
            //
            //   Add 'UserAccounts' to the tables that we are to track now
            //
            AddSchema("UserAccounts", ColDefs(
                ColDef("PrincipalID", ColumnTypes.Char36, true),
                ColDef("ScopeID", ColumnTypes.Char36),
                ColDef("FirstName", ColumnTypes.String64),
                ColDef("LastName", ColumnTypes.String64),
                ColDef("Email", ColumnTypes.String64),
                ColDef("ServiceURLs", ColumnTypes.Text),
                ColDef("Created", ColumnTypes.Integer11),
                ColDef("UserLevel", ColumnTypes.Integer11),
                ColDef("UserFlags", ColumnTypes.Integer11),
                ColDef("UserTitle", ColumnTypes.String64)));

            //
            // Change summery:
            //
            //   Split the old 'assets' table up into 3 new tables that will each deal with part of the assets.
            //
            AddSchema("assetblob", ColDefs(
                ColDef("AssetID", ColumnTypes.Char36, true),
                ColDef("AssetType", ColumnTypes.Integer11),
                ColDef("OwnerID", ColumnTypes.Char36, true),
                ColDef("Data", ColumnTypes.LongBlob),
                ColDef("Info", ColumnTypes.String512)));

            AddSchema("assettext", ColDefs(
                ColDef("AssetID", ColumnTypes.Char36, true),
                ColDef("AssetType", ColumnTypes.Integer11),
                ColDef("OwnerID", ColumnTypes.Char36, true),
                ColDef("Data", ColumnTypes.Text),
                ColDef("Info", ColumnTypes.String512)));

            AddSchema("assetmesh", ColDefs(
                ColDef("AssetID", ColumnTypes.Char36, true),
                ColDef("OwnerID", ColumnTypes.Char36, true),
                ColDef("Data", ColumnTypes.LongBlob),
                ColDef("Info", ColumnTypes.String512)));
        }

        protected override void DoCreateDefaults(DataSessionProvider sessionProvider, IDataConnector genericData)
        {
            EnsureAllTablesInSchemaExist(genericData);
        }

        protected override bool DoValidate(DataSessionProvider sessionProvider, IDataConnector genericData)
        {
            return TestThatAllTablesValidate(genericData);
        }

        protected override void DoMigrate(DataSessionProvider sessionProvider, IDataConnector genericData)
        {
            DoCreateDefaults(sessionProvider, genericData);
        }

        protected override void DoPrepareRestorePoint(DataSessionProvider sessionProvider, IDataConnector genericData)
        {
            CopyAllTablesToTempVersions(genericData);
        }

        public override void DoRestore(DataSessionProvider sessionProvider, IDataConnector genericData)
        {
            RestoreTempTablesToReal(genericData);
        }
    }
}