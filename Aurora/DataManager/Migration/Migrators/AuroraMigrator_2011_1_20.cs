using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuroraMigrator_2011_1_20 : Migrator
    {
        public AuroraMigrator_2011_1_20()
        {
            Version = new Version(2011, 1, 20);
            CanProvideDefaults = true;

            schema = new AuroraMigrator_2011_1_16().schema;
            renameSchema = new Dictionary<string, string>();

            //
            // Change summery:
            //
            //   Force 'UserAccounts' to 'useraccounts'
            //     Note: we do multiple renames here as it doesn't 
            //     always like just switching to lowercase (as in SQLite)
            //
            renameSchema.Add("UserAccounts", "useraccountslower");
            renameSchema.Add("useraccountslower", "useraccounts");

            //Remove the old name
            this.RemoveSchema("UserAccounts");
            //Add the new lowercase one
            AddSchema("useraccounts", ColDefs(
                ColDef("PrincipalID", ColumnTypes.Char36, true),
                ColDef("ScopeID", ColumnTypes.Char36),
                ColDef("FirstName", ColumnTypes.String64),
                ColDef("LastName", ColumnTypes.String64),
                ColDef("Email", ColumnTypes.String64),
                ColDef("ServiceURLs", ColumnTypes.Text),
                ColDef("Created", ColumnTypes.Integer11),
                ColDef("UserLevel", ColumnTypes.Integer11),
                ColDef("UserFlags", ColumnTypes.Integer11),
                ColDef("UserTitle", ColumnTypes.String64)));
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