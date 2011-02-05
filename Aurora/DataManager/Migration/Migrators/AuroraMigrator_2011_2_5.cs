using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuroraMigrator_2011_2_5 : Migrator
    {
        public AuroraMigrator_2011_2_5()
        {
            Version = new Version(2011, 2, 5);
            CanProvideDefaults = true;

            schema = new AuroraMigrator_2011_1_28().schema;
            renameSchema = new Dictionary<string, string>();

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