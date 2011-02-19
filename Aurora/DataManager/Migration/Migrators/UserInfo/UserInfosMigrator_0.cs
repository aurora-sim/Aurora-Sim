using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class UserInfoMigrator_0 : Migrator
    {
        public UserInfoMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "UserInfo";

            schema = new List<Rec<string, ColumnDefinition[]>>();
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