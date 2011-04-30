using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AssetMigrator_0 : Migrator
    {
        public AssetMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "Asset";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            AddSchema("lslgenericdata", ColDefs(ColDef("Token", ColumnTypes.String50, true),
                ColDef("KeySetting", ColumnTypes.String50, true),
                ColDef("ValueSetting", ColumnTypes.String50)));

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

        protected override void DoCreateDefaults(IDataConnector genericData)
        {
            EnsureAllTablesInSchemaExist(genericData);
        }

        protected override bool DoValidate(IDataConnector genericData)
        {
            return TestThatAllTablesValidate(genericData);
        }

        protected override void DoMigrate(IDataConnector genericData)
        {
            DoCreateDefaults(genericData);
        }

        protected override void DoPrepareRestorePoint(IDataConnector genericData)
        {
            CopyAllTablesToTempVersions(genericData);
        }
    }
}