using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AbuseReportsMigrator_0 : Migrator
    {
        public AbuseReportsMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "AbuseReports";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            AddSchema("abusereports", ColDefs(
                ColDef("Category", ColumnTypes.String100),
                ColDef("ReporterName", ColumnTypes.String100),
                ColDef("ObjectName", ColumnTypes.String100),
                ColDef("ObjectUUID", ColumnTypes.String100),
                ColDef("AbuserName", ColumnTypes.String100),
                ColDef("AbuseLocation", ColumnTypes.String100),
                ColDef("AbuseDetails", ColumnTypes.String512),
                ColDef("ObjectPosition", ColumnTypes.String100),
                ColDef("RegionName", ColumnTypes.String100),
                ColDef("ScreenshotID", ColumnTypes.String100),
                ColDef("AbuseSummary", ColumnTypes.String100),
                ColDef("Number", ColumnTypes.String100, true),
                ColDef("AssignedTo", ColumnTypes.String100),
                ColDef("Active", ColumnTypes.String100),
                ColDef("Checked", ColumnTypes.String100),
                ColDef("Notes", ColumnTypes.String1024)
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