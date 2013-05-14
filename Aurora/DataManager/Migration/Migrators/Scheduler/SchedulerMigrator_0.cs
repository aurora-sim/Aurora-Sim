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

namespace Aurora.DataManager.Migration.Migrators.Scheduler
{
    public class SchedulerMigrator_0 : Migrator
    {
        private static readonly List<SchemaDefinition> _schema = new List<SchemaDefinition>()
        {
            new SchemaDefinition("scheduler",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "id", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "fire_function", Type = ColumnTypeDef.String128},
                    new ColumnDefinition {Name = "fire_params", Type = ColumnTypeDef.MediumText},
                    new ColumnDefinition {Name = "run_once", Type = ColumnTypeDef.TinyInt1},
                    new ColumnDefinition {Name = "run_every", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "runs_next", Type = ColumnTypeDef.DateTime},
                    new ColumnDefinition {Name = "keep_history", Type = ColumnTypeDef.TinyInt1},
                    new ColumnDefinition {Name = "require_reciept", Type = ColumnTypeDef.TinyInt1},
                    new ColumnDefinition {Name = "last_history_id", Type = ColumnTypeDef.String36},
                    new ColumnDefinition {Name = "create_time", Type = ColumnTypeDef.DateTime},
                    new ColumnDefinition {Name = "start_time", Type = ColumnTypeDef.DateTime},
                    new ColumnDefinition {Name = "run_every_type", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "enabled", Type = ColumnTypeDef.TinyInt1},
                    new ColumnDefinition {Name = "schedule_for", Type = ColumnTypeDef.Char36DefaultZero},
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"id"}, Type = IndexType.Primary },
                    new IndexDefinition() { Fields = new string[] {"runs_next", "enabled"}, Type = IndexType.Index },
                    new IndexDefinition() { Fields = new string[] {"schedule_for", "fire_function"}, Type = IndexType.Index }
                }),
            new SchemaDefinition("scheduler_history",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "id", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "scheduler_id", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "ran_time", Type = ColumnTypeDef.DateTime},
                    new ColumnDefinition {Name = "run_time", Type = ColumnTypeDef.DateTime},
                    new ColumnDefinition {Name = "reciept", Type = ColumnTypeDef.MediumText},
                    new ColumnDefinition {Name = "is_complete", Type = ColumnTypeDef.TinyInt1},
                    new ColumnDefinition {Name = "complete_time", Type = ColumnTypeDef.DateTime},
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"id", "scheduler_id"}, Type = IndexType.Primary }
                }),
        };

        public SchedulerMigrator_0()
        {
            Version = new Version(0, 1, 0);
            MigrationName = "Scheduler";
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