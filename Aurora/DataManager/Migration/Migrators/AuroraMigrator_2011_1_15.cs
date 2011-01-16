using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuroraMigrator_2011_1_15 : Migrator
    {
        public AuroraMigrator_2011_1_15()
        {
            Version = new Version(2011, 1, 15);
            CanProvideDefaults = true;

            schema = new AuroraMigrator_2010_12_30().schema;

            //Added tables

            //
            // Change summery:
            //
            //   Add the new 'gridregions' table to replace the old 'regions' table
            //
            AddSchema("gridregions", ColDefs(
                ColDef("ScopeID", ColumnTypes.String45),
                ColDef("RegionUUID", ColumnTypes.String45, true),
                ColDef("RegionName", ColumnTypes.String50),
                ColDef("LocX", ColumnTypes.Integer),
                ColDef("LocY", ColumnTypes.Integer),
                ColDef("LocZ", ColumnTypes.Integer),
                ColDef("OwnerUUID", ColumnTypes.String45),
                ColDef("Access", ColumnTypes.Integer),
                ColDef("SizeX", ColumnTypes.Integer),
                ColDef("SizeY", ColumnTypes.Integer),
                ColDef("SizeZ", ColumnTypes.Integer),
                ColDef("Flags", ColumnTypes.Integer),
                ColDef("SessionID", ColumnTypes.String45),
                ColDef("Info", ColumnTypes.Text)));
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