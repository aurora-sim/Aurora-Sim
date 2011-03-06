using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class UserInfoMigrator_2 : Migrator
    {
        public UserInfoMigrator_2()
        {
            Version = new Version(0, 0, 2);
            MigrationName = "UserInfo";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            //
            // Change summery:
            //
            //   Add the new UserInfo table that replaces the GridUser and Presence tables
            //
            RemoveSchema("userinfo");
            AddSchema("userinfo", ColDefs(
                ColDef("UserID", ColumnTypes.String50, true),
                ColDef("RegionID", ColumnTypes.String50),
                ColDef("LastSeen", ColumnTypes.Integer30),
                ColDef("IsOnline", ColumnTypes.String36),
                ColDef("LastLogin", ColumnTypes.String50),
                ColDef("LastLogout", ColumnTypes.String50),
                ColDef("Info", ColumnTypes.String512),
                ColDef("CurrentRegionID", ColumnTypes.Char36),
                ColDef("CurrentPosition", ColumnTypes.String36),
                ColDef("CurrentLookat", ColumnTypes.String36),
                ColDef("HomeRegionID", ColumnTypes.Char36),
                ColDef("HomePosition", ColumnTypes.String36),
                ColDef("HomeLookat", ColumnTypes.String36)));
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