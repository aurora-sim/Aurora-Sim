using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class InventoryMigrator_0 : Migrator
    {
        public InventoryMigrator_0 ()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "Inventory";

            schema = new List<Rec<string, ColumnDefinition[]>> ();

            AddSchema ("inventoryfolders", ColDefs (ColDef ("folderID", ColumnTypes.Char36, true),
                ColDef ("agentID", ColumnTypes.Char36, true),
                ColDef ("parentFolderID", ColumnTypes.Char36, true),
                ColDef ("folderName", ColumnTypes.String64),
                ColDef ("type", ColumnTypes.Integer11),
                ColDef ("version", ColumnTypes.Integer11)));

            AddSchema ("inventoryitems", ColDefs (ColDef ("assetID", ColumnTypes.Char36),
                ColDef ("assetType", ColumnTypes.Integer11),
                ColDef ("inventoryName", ColumnTypes.String64),
                ColDef ("inventoryDescription", ColumnTypes.String128),
                ColDef ("inventoryNextPermissions", ColumnTypes.Integer11),
                ColDef ("inventoryCurrentPermissions", ColumnTypes.Integer11),
                ColDef ("invType", ColumnTypes.Integer11),
                ColDef ("creatorID", ColumnTypes.String128),
                ColDef ("inventoryBasePermissions", ColumnTypes.Integer11),
                ColDef ("inventoryEveryOnePermissions", ColumnTypes.Integer11),
                ColDef ("salePrice", ColumnTypes.Integer11),
                ColDef ("saleType", ColumnTypes.Integer11),
                ColDef ("creationDate", ColumnTypes.Integer11),
                ColDef ("groupID", ColumnTypes.Char36),
                ColDef ("groupOwned", ColumnTypes.Integer11),
                ColDef ("flags", ColumnTypes.Integer11, true),
                ColDef ("inventoryID", ColumnTypes.Char36, true),
                ColDef ("avatarID", ColumnTypes.Char36, true),
                ColDef ("parentFolderID", ColumnTypes.Char36, true),
                ColDef ("inventoryGroupPermissions", ColumnTypes.Integer11)));
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