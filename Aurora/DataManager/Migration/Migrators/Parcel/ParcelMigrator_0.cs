using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class ParcelMigrator_0 : Migrator
    {
        public ParcelMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "Parcel";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            AddSchema("parcelaccess", ColDefs(
                ColDef("ParcelID", ColumnTypes.String50, true),
                ColDef("AccessID", ColumnTypes.String50),
                ColDef("Flags", ColumnTypes.String50),
                ColDef("Time", ColumnTypes.String50)));
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