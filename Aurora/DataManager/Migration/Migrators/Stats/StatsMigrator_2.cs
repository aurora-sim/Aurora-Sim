/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using Aurora.DataManager.Migration;
using Aurora.Framework;

namespace Aurora.DataManager
{
    public class StatsMigrator_2 : Migrator
    {
        public StatsMigrator_2()
        {
            Version = new Version(0, 0, 2);
            MigrationName = "Stats";

            schema = new List<SchemaDefinition>();

            AddSchema("statsdata", ColDefs(ColDef("session_id", ColumnTypes.String50),
                                           ColDef("agent_id", ColumnTypes.String50),
                                           ColDef("region_id", ColumnTypes.String50),
                                           ColDef("agents_in_view", ColumnTypes.String50),
                                           ColDef("fps", ColumnTypes.Integer11),
                                           ColDef("a_language", ColumnTypes.String50),
                                           ColDef("mem_use", ColumnTypes.Integer11),
                                           ColDef("meters_traveled", ColumnTypes.Integer11),
                                           ColDef("ping", ColumnTypes.Integer11),
                                           ColDef("regions_visited", ColumnTypes.Integer11),
                                           ColDef("run_time", ColumnTypes.String50),
                                           ColDef("sim_fps", ColumnTypes.Integer11),
                                           ColDef("start_time", ColumnTypes.Integer11),
                                           ColDef("client_version", ColumnTypes.String50),
                                           ColDef("s_cpu", ColumnTypes.String128),
                                           ColDef("s_gpu", ColumnTypes.String128),
                                           ColDef("s_gpuclass", ColumnTypes.String50),
                                           ColDef("s_gpuvendor", ColumnTypes.String50),
                                           ColDef("s_gpuversion", ColumnTypes.String50),
                                           ColDef("s_os", ColumnTypes.String50),
                                           ColDef("s_ram", ColumnTypes.String50),
                                           ColDef("d_object_kb", ColumnTypes.Integer11),
                                           ColDef("d_texture_kb", ColumnTypes.Integer11),
                                           ColDef("d_world_kb", ColumnTypes.Integer11),
                                           ColDef("n_in_kb", ColumnTypes.Integer11),
                                           ColDef("n_in_pk", ColumnTypes.Integer11),
                                           ColDef("n_out_kb", ColumnTypes.Integer11),
                                           ColDef("n_out_pk", ColumnTypes.Integer11),
                                           ColDef("f_dropped", ColumnTypes.Integer11),
                                           ColDef("f_failed_resends", ColumnTypes.Integer11),
                                           ColDef("f_invalid", ColumnTypes.Integer11),
                                           ColDef("f_off_circuit", ColumnTypes.Integer11),
                                           ColDef("f_resent", ColumnTypes.Integer11),
                                           ColDef("f_send_packet", ColumnTypes.Integer11)),
                      IndexDefs(
                          IndexDef(new string[1] {"session_id"}, IndexType.Primary)));
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