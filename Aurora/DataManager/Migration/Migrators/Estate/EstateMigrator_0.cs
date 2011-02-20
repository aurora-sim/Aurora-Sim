using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class EstateMigrator_0 : Migrator
    {
        public EstateMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "Estate";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            AddSchema("estates", ColDefs(
                ColDef("ID", ColumnTypes.String45, true),
                ColDef("Key", ColumnTypes.String50, true),
                ColDef("Value", ColumnTypes.Text)
                ));
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