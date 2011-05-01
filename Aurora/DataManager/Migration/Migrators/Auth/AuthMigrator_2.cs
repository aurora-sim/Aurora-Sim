using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuthMigrator_2 : Migrator
    {
        public AuthMigrator_2()
        {
            Version = new Version(0, 0, 2);
            MigrationName = "Auth";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            //
            // Change summery:
            //
            //   Make the password hash much longer to accommodate other types of passwords)
            //

            AddSchema("auth", ColDefs(
                ColDef("UUID", ColumnTypes.Char36, true),
                ColDef("passwordHash", ColumnTypes.String1024),
                ColDef ("passwordSalt", ColumnTypes.String1024),
                ColDef("accountType", ColumnTypes.Char32, true)));
            AddSchema("tokens", ColDefs(
                ColDef("UUID", ColumnTypes.Char36, true),
                ColDef("token", ColumnTypes.String255, true),
                ColDef ("validity", ColumnTypes.DateTime)));
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