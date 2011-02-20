using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class GridRegionMigrator_0 : Migrator
    {
        public GridRegionMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "GridRegions";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            //
            // Change summery:
            //
            //   Add the new 'gridregions' table to replace the old 'regions' table
            //
            AddSchema("gridregions", ColDefs(
                ColDef("ScopeID", ColumnTypes.String45),
                ColDef("RegionUUID", ColumnTypes.String45, true),
                ColDef("RegionName", ColumnTypes.String50),
                ColDef("LocX", ColumnTypes.Integer11),
                ColDef("LocY", ColumnTypes.Integer11),
                ColDef("LocZ", ColumnTypes.Integer11),
                ColDef("OwnerUUID", ColumnTypes.String45),
                ColDef("Access", ColumnTypes.Integer11),
                ColDef("SizeX", ColumnTypes.Integer11),
                ColDef("SizeY", ColumnTypes.Integer11),
                ColDef("SizeZ", ColumnTypes.Integer11),
                ColDef("Flags", ColumnTypes.Integer11),
                ColDef("SessionID", ColumnTypes.String45),
                ColDef("Info", ColumnTypes.Text)));
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