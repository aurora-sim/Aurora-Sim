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
using Aurora.Framework;
using Aurora.Framework.Utilities;

namespace Aurora.DataManager.Migration.Migrators.Asset
{
    public class AuroraAssetMigrator_2 : Migrator
    {
        public AuroraAssetMigrator_2()
        {
            Version = new Version(0, 0, 2);
            MigrationName = "BlackholeAsset";

            schema = new List<SchemaDefinition>();

            AddSchema("auroraassets_A", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_B", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_C", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_D", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_E", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_F", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_0", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_1", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_2", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_3", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_4", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_5", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_6", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_7", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_8", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_9", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024),
                ColDef("parent_id", ColumnTypes.String36)
                                            ), IndexDefs(
                                                IndexDef(new string[4] {"id", "hash_code", "creator_id", "parent_id"},
                                                         IndexType.Primary),
                                                IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                   ));

            AddSchema("auroraassets_old", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("parent_id", ColumnTypes.String36),
                ColDef("creator_id", ColumnTypes.String36),
                ColDef("name", ColumnTypes.String64),
                ColDef("description", ColumnTypes.String128),
                ColDef("asset_type", ColumnTypes.Integer11),
                ColDef("create_time", ColumnTypes.Integer11),
                ColDef("access_time", ColumnTypes.Integer11),
                ColDef("asset_flags", ColumnTypes.String64),
                ColDef("owner_id", ColumnTypes.String36),
                ColDef("host_uri", ColumnTypes.String1024)
                                              ), IndexDefs(
                                                  IndexDef(
                                                      new string[4] {"id", "hash_code", "parent_id", "creator_id"},
                                                      IndexType.Primary),
                                                  IndexDef(new string[1] {"hash_code"}, IndexType.Index)
                                                     ));

            AddSchema("auroraassets_tasks", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("task_type", ColumnTypes.String64),
                ColDef("task_values", ColumnTypes.String255)
                                                ), IndexDefs(
                                                    IndexDef(new string[1] {"id"}, IndexType.Primary)
                                                       ));

            AddSchema("auroraassets_temp", ColDefs(
                ColDef("id", ColumnTypes.String36),
                ColDef("hash_code", ColumnTypes.String64),
                ColDef("creator_id", ColumnTypes.String36)
                                               ), IndexDefs(
                                                   IndexDef(new string[3] {"id", "hash_code", "creator_id"},
                                                            IndexType.Primary),
                                                   IndexDef(new string[2] {"hash_code", "creator_id"}, IndexType.Index)
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