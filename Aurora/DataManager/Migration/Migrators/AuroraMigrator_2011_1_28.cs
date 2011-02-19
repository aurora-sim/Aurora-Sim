using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuroraMigrator_2011_1_28 : Migrator
    {
        public AuroraMigrator_2011_1_28()
        {
            Version = new Version(2011, 1, 28);
            CanProvideDefaults = true;

            schema = new AuroraMigrator_2011_1_20().schema;
            renameSchema = new Dictionary<string, string>();

            //
            // Change summery:
            //
            //   Add a new column to searchparcel
            //
            //Remove the old name
            this.RemoveSchema("searchparcel");
            //Add the new column
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

        public override void DoRestore(IDataConnector genericData)
        {
            RestoreTempTablesToReal(genericData);
        }
    }
}