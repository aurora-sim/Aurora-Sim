using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuroraMigrator_2010_12_30 : Migrator
    {
        public AuroraMigrator_2010_12_30()
        {
            Version = new Version(2010, 12, 30);
            CanProvideDefaults = true;

            schema = new AuroraMigrator_2010_11_4().schema;
            renameSchema = new Dictionary<string, string>();

            //Added tables

            //
            // Change summery:
            //
            //   Rewrite of the table to make it more generic
            //
            RemoveSchema("simulator");
            AddSchema("simulator", ColDefs(
                ColDef("RegionID", ColumnTypes.String50, true),
                ColDef("RegionName", ColumnTypes.String50),
                ColDef("RegionInfo", ColumnTypes.String1024),
                ColDef("Disabled", ColumnTypes.String45)));

            //
            // Change summery:
            //
            //   Changes the default length of "Message" so that it isn't limited to only 50 chars
            //
            RemoveSchema("osgroupnotice");
            AddSchema("osgroupnotice", ColDefs(ColDef("GroupID", ColumnTypes.String50, true),
                ColDef("NoticeID", ColumnTypes.String50, true),
                ColDef("Timestamp", ColumnTypes.String50, true),
                ColDef("FromName", ColumnTypes.String50),
                ColDef("Subject", ColumnTypes.String50),
                ColDef("Message", ColumnTypes.String1024),
                ColDef("HasAttachment", ColumnTypes.String50),
                ColDef("ItemID", ColumnTypes.String50),
                ColDef("AssetType", ColumnTypes.String50),
                ColDef("ItemName", ColumnTypes.String50)));
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