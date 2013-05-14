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

namespace Aurora.DataManager.Migration.Migrators.Inventory
{
    public class InventoryMigrator_0 : Migrator
    {
        private static readonly List<SchemaDefinition> _schema = new List<SchemaDefinition>()
        {
            new SchemaDefinition("inventoryfolders",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "folderID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "agentID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "parentFolderID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "folderName", Type = ColumnTypeDef.String128},
                    new ColumnDefinition {Name = "type", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "version", Type = ColumnTypeDef.Integer11},
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"folderID", "agentID", "parentFolderID"}, Type = IndexType.Primary },
                    new IndexDefinition() { Fields = new string[] {"folderID"}, Type = IndexType.Index },
                    new IndexDefinition() { Fields = new string[] {"agentID", "folderID"}, Type = IndexType.Index }
                }),
            new SchemaDefinition("inventoryitems",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "assetID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "assetType", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "inventoryName", Type = ColumnTypeDef.String128},
                    new ColumnDefinition {Name = "inventoryDescription", Type = ColumnTypeDef.String128},
                    new ColumnDefinition {Name = "inventoryNextPermissions", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "inventoryCurrentPermissions", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "invType", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "creatorID", Type = ColumnTypeDef.String128},
                    new ColumnDefinition {Name = "inventoryBasePermissions", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "inventoryEveryOnePermissions", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "salePrice", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "saleType", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "creationDate", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "groupID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "groupOwned", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "flags", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "inventoryID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "avatarID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "parentFolderID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "inventoryGroupPermissions", Type = ColumnTypeDef.Integer11}
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"assetType", "flags", "inventoryID", "avatarID", "parentFolderID"}, Type = IndexType.Primary },
                    new IndexDefinition() { Fields = new string[] {"parentFolderID", "avatarID"}, Type = IndexType.Index },
                    new IndexDefinition() { Fields = new string[] {"avatarID", "assetType"}, Type = IndexType.Index },
                    new IndexDefinition() { Fields = new string[] {"inventoryID"}, Type = IndexType.Index },
                    new IndexDefinition() { Fields = new string[] {"assetID", "avatarID"}, Type = IndexType.Index }
                }),
        };

        public InventoryMigrator_0()
        {
            Version = new Version(0, 1, 0);
            MigrationName = "Inventory";
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