using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class GenericsMigrator_1 : Migrator
    {
        public GenericsMigrator_1()
        {
            Version = new Version(0, 0, 1);
            MigrationName = "Generics";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            AddSchema("generics", ColDefs(
                ColDef("OwnerID", ColumnTypes.String45, true),
                ColDef("Type", ColumnTypes.String45, true),
                ColDef("Key", ColumnTypes.String50, true),
                ColDef("Value", ColumnTypes.LongText)
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