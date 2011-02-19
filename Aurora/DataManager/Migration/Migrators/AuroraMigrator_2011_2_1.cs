using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuroraMigrator_2011_2_1 : Migrator
    {
        public AuroraMigrator_2011_2_1()
        {
            Version = new Version(2011, 2, 1);

            schema = new AuroraMigrator_2011_1_28().schema;
            renameSchema = new Dictionary<string, string>();

            //
            // Change summery:
            //
            //   Add the new UserInfo table that replaces the GridUser and Presence tables
            //
            AddSchema("userinfo", ColDefs(
                ColDef("UserID", ColumnTypes.String50, true),
                ColDef("RegionID", ColumnTypes.String50, true),
                ColDef("SessionID", ColumnTypes.String50),
                ColDef("LastSeen", ColumnTypes.Integer30),
                ColDef("IsOnline", ColumnTypes.String36),
                ColDef("LastLogin", ColumnTypes.String50),
                ColDef("LastLogout", ColumnTypes.String50),
                ColDef("Info", ColumnTypes.String512)));

            //
            // Change summery:
            //
            //   remove the old avatararchives and define the new one
            //
            //Remove the old name
            this.RemoveSchema("avatararchives");

            AddSchema("avatararchives", ColDefs(
                ColDef("Name", ColumnTypes.String50, true),
                ColDef("Archive", ColumnTypes.Blob),
                ColDef("Snapshot", ColumnTypes.Char36),
                ColDef("IsPublic", ColumnTypes.Integer11)));

            //
            // Change summery:
            //
            //   Changes the default length of "Description" and "Title" so that they aren't limited to only 50 chars
            //
            RemoveSchema("osrole");

            AddSchema("osrole", ColDefs(ColDef("GroupID", ColumnTypes.String50, true),
                ColDef("RoleID", ColumnTypes.String50, true),
                ColDef("Name", ColumnTypes.String512),
                ColDef("Description", ColumnTypes.String512),
                ColDef("Title", ColumnTypes.String512),
                ColDef("Powers", ColumnTypes.String50)));
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