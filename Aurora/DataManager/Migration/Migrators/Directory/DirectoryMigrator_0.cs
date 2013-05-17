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

namespace Aurora.DataManager.Migration.Migrators.Directory
{
    public class DirectoryMigrator_0 : Migrator
    {
        private static readonly List<SchemaDefinition> _schema = new List<SchemaDefinition>()
        {
            new SchemaDefinition("searchparcel",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "RegionID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "ParcelID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "LocalID", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "LandingX", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "LandingY", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "LandingZ", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "Name", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "Description", Type = ColumnTypeDef.String255},
                    new ColumnDefinition {Name = "Flags", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "Dwell", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "InfoUUID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "ForSale", Type = ColumnTypeDef.TinyInt1},
                    new ColumnDefinition {Name = "SalePrice", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "Auction", Type = ColumnTypeDef.TinyInt1},
                    new ColumnDefinition {Name = "Area", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "EstateID", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "Maturity", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "OwnerID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "GroupID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "ShowInSearch", Type = ColumnTypeDef.TinyInt1},
                    new ColumnDefinition {Name = "SnapshotID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "Bitmap", Type = ColumnTypeDef.LongBlob},
                    new ColumnDefinition {Name = "Category", Type = ColumnTypeDef.String64},
                    new ColumnDefinition {Name = "ScopeID", Type = ColumnTypeDef.Char36DefaultZero}
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"ParcelID"}, Type = IndexType.Primary },
                    new IndexDefinition() { Fields = new string[] {"RegionID", "OwnerID", "Flags", "Category"}, Type = IndexType.Index },
                    new IndexDefinition() { Fields = new string[] {"OwnerID"}, Type = IndexType.Index },
                    new IndexDefinition() { Fields = new string[] {"RegionID", "Name"}, Type = IndexType.Index },
                    new IndexDefinition() { Fields = new string[] {"ForSale", "SalePrice", "Area"}, Type = IndexType.Index }
                }),
            new SchemaDefinition("asevents",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "EID", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "creator", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "region", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "parcel", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "date", Type = ColumnTypeDef.Date},
                    new ColumnDefinition {Name = "cover", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "maturity", Type = ColumnTypeDef.TinyInt4},
                    new ColumnDefinition {Name = "flags", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "duration", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "localPosX", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "localPosY", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "localPosZ", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "name", Type = ColumnTypeDef.TinyInt1},
                    new ColumnDefinition {Name = "description", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "category", Type = ColumnTypeDef.String64},
                    new ColumnDefinition {Name = "scopeID", Type = ColumnTypeDef.Char36}
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"EID"}, Type = IndexType.Primary },
                    new IndexDefinition() { Fields = new string[] {"name"}, Type = IndexType.Index },
                    new IndexDefinition() { Fields = new string[] {"date", "flags"}, Type = IndexType.Index },
                    new IndexDefinition() { Fields = new string[] {"region", "maturity"}, Type = IndexType.Index }
                }),
            new SchemaDefinition("event_notifications",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "UserID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "EventID", Type = ColumnTypeDef.Integer11}
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"UserID"}, Type = IndexType.Primary }
                }),
        };

        public DirectoryMigrator_0()
        {
            Version = new Version(0, 1, 0);
            MigrationName = "Directory";
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