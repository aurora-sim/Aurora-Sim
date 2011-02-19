using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class ScriptDataMigrator_0 : Migrator
    {
        public ScriptDataMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "ScriptData";

            schema = new List<Rec<string, ColumnDefinition[]>>();
            renameSchema = new Dictionary<string, string>();

            AddSchema("auroradotnetstatesaves", ColDefs(
                ColDef("State", ColumnTypes.String50),
                ColDef("ItemID", ColumnTypes.String50, true),
                ColDef("Source", ColumnTypes.Text),
                ColDef("Running", ColumnTypes.String50),
                ColDef("Variables", ColumnTypes.Text),
                ColDef("Plugins", ColumnTypes.Text),
                ColDef("Permissions", ColumnTypes.String50),
                ColDef("MinEventDelay", ColumnTypes.String50),
                ColDef("AssemblyName", ColumnTypes.Text),
                ColDef("Disabled", ColumnTypes.String45),
                ColDef("UserInventoryItemID", ColumnTypes.String50)
                ));
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