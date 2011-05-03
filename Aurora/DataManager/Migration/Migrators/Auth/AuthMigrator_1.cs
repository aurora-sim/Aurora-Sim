using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuthMigrator_1 : Migrator
    {
        public AuthMigrator_1()
        {
            Version = new Version(0, 0, 1);
            MigrationName = "Auth";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            //
            // Change summery:
            //
            //   Remove the webLoginKey pieces (as it shouldn't be used in this way)
            //

            AddSchema("auth", ColDefs(
                ColDef("UUID", ColumnTypes.Char36, true),
                ColDef("passwordHash", ColumnTypes.Char32),
                ColDef("passwordSalt", ColumnTypes.Char32),
                ColDef("accountType", ColumnTypes.Char32, true)));
            AddSchema("tokens", ColDefs(
                ColDef("UUID", ColumnTypes.Char36, true),
                ColDef("token", ColumnTypes.String255, true),
                ColDef ("validity", ColumnTypes.Date)));
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