using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class DirectoryMigrator_1 : Migrator
    {
        public DirectoryMigrator_1()
        {
            Version = new Version(0, 0, 1);
            MigrationName = "Directory";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            AddSchema("searchparcel", ColDefs(ColDef("RegionID", ColumnTypes.String50),
                ColDef("ParcelID", ColumnTypes.String50, true),
                ColDef("LocalID", ColumnTypes.String50),
                ColDef("LandingX", ColumnTypes.String50),
                ColDef("LandingY", ColumnTypes.String50),
                ColDef("LandingZ", ColumnTypes.String50),
                ColDef("Name", ColumnTypes.String50),
                ColDef("Description", ColumnTypes.String50),
                ColDef("Flags", ColumnTypes.String50),
                ColDef("Dwell", ColumnTypes.String50),
                ColDef("InfoUUID", ColumnTypes.String50),
                ColDef("ForSale", ColumnTypes.String50),
                ColDef("SalePrice", ColumnTypes.String50),
                ColDef("Auction", ColumnTypes.String50),
                ColDef("Area", ColumnTypes.String50),
                ColDef("EstateID", ColumnTypes.String50),
                ColDef("Maturity", ColumnTypes.String50),
                ColDef("OwnerID", ColumnTypes.String50),
                ColDef("GroupID", ColumnTypes.String50),
                ColDef("ShowInSearch", ColumnTypes.String50),
                ColDef("SnapshotID", ColumnTypes.String50),
                ColDef("Bitmap", ColumnTypes.String1024)));

            AddSchema("events", ColDefs(
                ColDef("EOwnerID", ColumnTypes.String50),
                ColDef("EName", ColumnTypes.String50),
                ColDef("EID", ColumnTypes.String50, true),
                ColDef("ECreatorID", ColumnTypes.String50),
                ColDef("ECategory", ColumnTypes.String50),
                ColDef("EDesc", ColumnTypes.String50),
                ColDef("EDate", ColumnTypes.String50),
                ColDef("ECoverCharge", ColumnTypes.String50),
                ColDef("ECoverAmount", ColumnTypes.String50),
                ColDef("ESimName", ColumnTypes.String50),
                ColDef("EGlobalPos", ColumnTypes.String50),
                ColDef("EFlags", ColumnTypes.String50),
                ColDef("EMature", ColumnTypes.String50),
                ColDef("EDuration", ColumnTypes.String50)
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