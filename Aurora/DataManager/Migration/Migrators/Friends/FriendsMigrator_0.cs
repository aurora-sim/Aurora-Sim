using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class FriendsMigrator_0 : Migrator
    {
        public FriendsMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "Friends";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            //
            // Change summery:
            //
            //   Force 'Friends' to 'friends'
            //     Note: we do multiple renames here as it doesn't 
            //     always like just switching to lowercase (as in SQLite)
            //
            this.RenameSchema("Friends", "friends");

            //Remove the old name
            this.RemoveSchema("friends");
            //Add the new lowercase one
            AddSchema("friends", ColDefs(
                ColDef("PrincipalID", ColumnTypes.Char36, true),
                ColDef("Friend", ColumnTypes.String255),
                ColDef("Flags", ColumnTypes.String16),
                ColDef("Offered", ColumnTypes.Char32)));
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