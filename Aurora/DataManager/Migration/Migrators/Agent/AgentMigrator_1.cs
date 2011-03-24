using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AgentMigrator_1 : Migrator
    {
        public AgentMigrator_1()
        {
            Version = new Version(0, 0, 1);
            MigrationName = "Agent";

            schema = new List<Rec<string, ColumnDefinition[]>> ();

            AddSchema ("userdata", ColDefs (
                ColDef ("ID", ColumnTypes.String45, true),
                ColDef ("Key", ColumnTypes.String50, true),
                ColDef ("Value", ColumnTypes.Text)
                ));

            AddSchema ("userclassifieds", ColDefs (ColDef ("Name", ColumnTypes.String50),
                 ColDef ("Category", ColumnTypes.String50),
                 ColDef ("SimName", ColumnTypes.String50),
                 ColDef ("OwnerUUID", ColumnTypes.String50),
                 ColDef ("ClassifiedUUID", ColumnTypes.String50, true),
                 ColDef ("Classified", ColumnTypes.String8196)));
            
            AddSchema ("userpicks", ColDefs (ColDef ("Name", ColumnTypes.String50),
                 ColDef ("SimName", ColumnTypes.String50),
                 ColDef ("OwnerUUID", ColumnTypes.String50),
                 ColDef ("PickUUID", ColumnTypes.String50, true),
                 ColDef ("Pick", ColumnTypes.String8196)));

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