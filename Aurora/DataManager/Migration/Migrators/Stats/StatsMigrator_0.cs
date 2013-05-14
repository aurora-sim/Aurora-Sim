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
using Aurora.Framework.Utilities;

namespace Aurora.DataManager.Migration.Migrators.Stats
{
    public class StatsMigrator_0 : Migrator
    {
        private static readonly List<SchemaDefinition> _schema = new List<SchemaDefinition>()
        {
            new SchemaDefinition("statsdata",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "session_id", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "agent_id", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "region_id", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "agents_in_view", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "fps", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "a_language", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "mem_use", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "meters_traveled", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "ping", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "regions_visited", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "run_time", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "sim_fps", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "start_time", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "client_version", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "s_cpu", Type = ColumnTypeDef.String128},
                    new ColumnDefinition {Name = "s_gpu", Type = ColumnTypeDef.String128},
                    new ColumnDefinition {Name = "s_gpuclass", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "s_gpuvendor", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "s_os", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "s_ram", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "d_object_kb", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "d_texture_kb", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "d_world_kb", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "n_in_kb", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "n_in_pk", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "n_out_kb", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "n_out_pk", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "f_dropped", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "f_failed_resends", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "f_invalid", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "f_off_circuit", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "f_resent", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "f_send_packet", Type = ColumnTypeDef.Integer11},
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"session_id"}, Type = IndexType.Primary }
                }),
        };

        public StatsMigrator_0()
        {
            Version = new Version(0, 1, 0);
            MigrationName = "Stats";
            base.schema = _schema;
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