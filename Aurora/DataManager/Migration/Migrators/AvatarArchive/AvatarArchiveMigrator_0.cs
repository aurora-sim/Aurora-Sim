using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AvatarArchiveMigrator_0 : Migrator
    {
        public AvatarArchiveMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "AvatarArchive";

            schema = new List<Rec<string, ColumnDefinition[]>>();
            renameSchema = new Dictionary<string, string>();

            AddSchema("avatararchives", ColDefs(
                ColDef("Name", ColumnTypes.String50, true),
                ColDef("Archive", ColumnTypes.Blob),
                ColDef("Snapshot", ColumnTypes.Char36),
                ColDef("IsPublic", ColumnTypes.Integer11)));

            AddSchema("passwords", ColDefs(ColDef("Method", ColumnTypes.String50, true),
                ColDef("Password", ColumnTypes.String50)));
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