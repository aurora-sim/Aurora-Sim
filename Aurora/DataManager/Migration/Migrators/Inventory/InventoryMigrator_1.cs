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

namespace Aurora.DataManager.Migration.Migrators
{
    public class InventoryMigrator_1 : Migrator
    {
        public InventoryMigrator_1()
        {
            Version = new Version(0, 0, 1);
            MigrationName = "Inventory";

            schema = new List<SchemaDefinition>();

            AddSchema("inventoryfolders", ColDefs(
                ColDef("folderID", ColumnTypes.Char36),
                ColDef("agentID", ColumnTypes.Char36),
                ColDef("parentFolderID", ColumnTypes.Char36),
                ColDef("folderName", ColumnTypes.String64),
                ColDef("type", ColumnTypes.Integer11),
                ColDef("version", ColumnTypes.Integer11)
                                              ), IndexDefs(
                                                  IndexDef(new string[3] {"folderID", "agentID", "parentFolderID"},
                                                           IndexType.Primary)
                                                     ));

            AddSchema("inventoryitems", ColDefs(
                ColDef("assetID", ColumnTypes.Char36),
                ColDef("assetType", ColumnTypes.Integer11),
                ColDef("inventoryName", ColumnTypes.String64),
                ColDef("inventoryDescription", ColumnTypes.String128),
                ColDef("inventoryNextPermissions", ColumnTypes.Integer11),
                ColDef("inventoryCurrentPermissions", ColumnTypes.Integer11),
                ColDef("invType", ColumnTypes.Integer11),
                ColDef("creatorID", ColumnTypes.String128),
                ColDef("inventoryBasePermissions", ColumnTypes.Integer11),
                ColDef("inventoryEveryOnePermissions", ColumnTypes.Integer11),
                ColDef("salePrice", ColumnTypes.Integer11),
                ColDef("saleType", ColumnTypes.Integer11),
                ColDef("creationDate", ColumnTypes.Integer11),
                ColDef("groupID", ColumnTypes.Char36),
                ColDef("groupOwned", ColumnTypes.Integer11),
                ColDef("flags", ColumnTypes.Integer11),
                ColDef("inventoryID", ColumnTypes.Char36),
                ColDef("avatarID", ColumnTypes.Char36),
                ColDef("parentFolderID", ColumnTypes.Char36),
                ColDef("inventoryGroupPermissions", ColumnTypes.Integer11)
                                            ), IndexDefs(
                                                IndexDef(
                                                    new string[5]
                                                        {
                                                            "assetType", "flags", "inventoryID", "avatarID",
                                                            "parentFolderID"
                                                        }, IndexType.Primary)
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