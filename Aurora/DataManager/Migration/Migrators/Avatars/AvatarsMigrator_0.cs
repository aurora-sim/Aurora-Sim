using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AvatarsMigrator_0 : Migrator
    {
        public AvatarsMigrator_0 ()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "Avatars";

            schema = new List<Rec<string, ColumnDefinition[]>> ();
            this.RenameSchema ("Avatars", "avatars");
            this.RemoveSchema ("avatars");

            AddSchema ("avatars", ColDefs (ColDef ("PrincipalID", ColumnTypes.Char36, true),
                ColDef ("Name", ColumnTypes.String32, true),
                ColDef ("Value", ColumnTypes.Text)));
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