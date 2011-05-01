using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuthMigrator_0 : Migrator
    {
        public AuthMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "Auth";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            //
            // Change summery:
            //
            //   Force the tables to lowercase
            //     Note: we do multiple renames here as it doesn't 
            //     always like just switching to lowercase (as in SQLite)
            //
            this.RenameSchema("Auth", "auth");
            this.RenameSchema("Tokens", "tokens");

            //Remove the old name
            this.RemoveSchema("auth");
            this.RemoveSchema("tokens");
            //Add the new lowercase one
            AddSchema("auth", ColDefs(
                ColDef("UUID", ColumnTypes.Char36, true),
                ColDef("passwordHash", ColumnTypes.Char32),
                ColDef("passwordSalt", ColumnTypes.Char32),
                ColDef("webLoginKey", ColumnTypes.String255),
                ColDef("accountType", ColumnTypes.Char32)));
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