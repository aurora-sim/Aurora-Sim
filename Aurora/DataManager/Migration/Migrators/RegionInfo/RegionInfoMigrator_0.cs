using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class RegionInfoMigrator_0 : Migrator
    {
        public RegionInfoMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "RegionInfo";

            schema = new List<Rec<string, ColumnDefinition[]>>();
            renameSchema = new Dictionary<string, string>();

            AddSchema("simulator", ColDefs(
                ColDef("RegionID", ColumnTypes.String50, true),
                ColDef("RegionName", ColumnTypes.String50),
                ColDef("RegionInfo", ColumnTypes.String1024),
                ColDef("Disabled", ColumnTypes.String45)));
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