using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class RegionMigrator_0 : Migrator
    {
        public RegionMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "Region";

            schema = new List<Rec<string, ColumnDefinition[]>>();
            renameSchema = new Dictionary<string, string>();

            AddSchema("telehubs", ColDefs(
                ColDef("RegionID", ColumnTypes.String50, true),
                ColDef("RegionLocX", ColumnTypes.String50),
                ColDef("RegionLocY", ColumnTypes.String50),
                ColDef("TelehubLocX", ColumnTypes.String50),
                ColDef("TelehubLocY", ColumnTypes.String50),
                ColDef("TelehubLocZ", ColumnTypes.String50),
                ColDef("TelehubRotX", ColumnTypes.String50),
                ColDef("TelehubRotY", ColumnTypes.String50),
                ColDef("TelehubRotZ", ColumnTypes.String50),
                ColDef("Spawns", ColumnTypes.String1024),
                ColDef("ObjectUUID", ColumnTypes.String50),
                ColDef("Name", ColumnTypes.String50)
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