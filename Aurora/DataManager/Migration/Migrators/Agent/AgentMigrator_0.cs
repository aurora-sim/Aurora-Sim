using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AgentMigrator_0 : Migrator
    {
        public AgentMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "Agent";

            schema = new List<Rec<string, ColumnDefinition[]>>();
            renameSchema = new Dictionary<string, string>();

            AddSchema("userdata", ColDefs(
                ColDef("ID", ColumnTypes.String45, true),
                ColDef("Key", ColumnTypes.String50, true),
                ColDef("Value", ColumnTypes.Text)
                ));

            AddSchema("macban", ColDefs(ColDef("macAddress", ColumnTypes.String50, true)));

            AddSchema("bannedviewers", ColDefs(ColDef("Client", ColumnTypes.String50, true)));
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