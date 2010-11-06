using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuroraMigrator_2010_11_4 : Migrator
    {
        public AuroraMigrator_2010_11_4()
        {
            Version = new Version(2010, 11, 4);
            CanProvideDefaults = true;

            schema = new AuroraMigrator_2010_03_13().schema;

            //Added tables

            AddSchema("stats_session_data", ColDefs(ColDef("session_id", ColumnTypes.String50, true),
                 ColDef("agent_id", ColumnTypes.String50),
                 ColDef("region_id", ColumnTypes.String50),
                 ColDef("last_updated", ColumnTypes.Integer),
                 ColDef("remote_ip", ColumnTypes.String45),
                 ColDef("name_f", ColumnTypes.String50),
                 ColDef("name_l", ColumnTypes.String50),
                 ColDef("avg_agents_in_view", ColumnTypes.String50),
                 ColDef("min_agents_in_view", ColumnTypes.String50),
                 ColDef("max_agents_in_view", ColumnTypes.String50),
                 ColDef("mode_agents_in_view", ColumnTypes.String50),
                 ColDef("avg_fps", ColumnTypes.String50),
                 ColDef("min_fps", ColumnTypes.String50),
                 ColDef("max_fps", ColumnTypes.String50),
                 ColDef("mode_fps", ColumnTypes.String50),
                 ColDef("a_language", ColumnTypes.String50),
                 ColDef("mem_use", ColumnTypes.String50),
                 ColDef("meters_traveled", ColumnTypes.String50),
                 ColDef("avg_ping", ColumnTypes.String50),
                 ColDef("min_ping", ColumnTypes.String50),
                 ColDef("max_ping", ColumnTypes.String50),
                 ColDef("mode_ping", ColumnTypes.String50),
                 ColDef("regions_visited", ColumnTypes.String50),
                 ColDef("run_time", ColumnTypes.String50),
                 ColDef("avg_sim_fps", ColumnTypes.String50),
                 ColDef("min_sim_fps", ColumnTypes.String50),
                 ColDef("max_sim_fps", ColumnTypes.String50),
                 ColDef("mode_sim_fps", ColumnTypes.String50),
                 ColDef("start_time", ColumnTypes.Integer),
                 ColDef("client_version", ColumnTypes.String50),
                 ColDef("s_cpu", ColumnTypes.String50),
                 ColDef("s_gpu", ColumnTypes.String50),
                 ColDef("s_os", ColumnTypes.String50),
                 ColDef("s_ram", ColumnTypes.String50),
                 ColDef("d_object_kb", ColumnTypes.String50),
                 ColDef("d_texture_kb", ColumnTypes.String50),
                 ColDef("d_world_kb", ColumnTypes.String50),
                 ColDef("n_in_kb", ColumnTypes.String50),
                 ColDef("n_in_pk", ColumnTypes.String50),
                 ColDef("n_out_kb", ColumnTypes.String50),
                 ColDef("n_out_pk", ColumnTypes.String50),
                 ColDef("f_dropped", ColumnTypes.String50),
                 ColDef("f_failed_resends", ColumnTypes.String50),
                 ColDef("f_invalid", ColumnTypes.String50),
                 ColDef("f_off_circuit", ColumnTypes.String50),
                 ColDef("f_resent", ColumnTypes.String50),
                 ColDef("f_send_packet", ColumnTypes.String50)));

            AddSchema("profileclassifieds", ColDefs(ColDef("Name", ColumnTypes.String50),
                 ColDef("Category", ColumnTypes.String50),
                 ColDef("SimName", ColumnTypes.String50),
                 ColDef("ClassifiedUUID", ColumnTypes.String50, true),
                 ColDef("Classified", ColumnTypes.String8196)));
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