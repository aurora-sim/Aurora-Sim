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

namespace Aurora.DataManager.Migration.Migrators.Groups
{
    /// <summary>
    ///     Changes:
    /// </summary>
    public class GroupsMigrator_0 : Migrator
    {
        private static readonly List<SchemaDefinition> _schema = new List<SchemaDefinition>()
        {
            new SchemaDefinition("osagent",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "AgentID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "ActiveGroupID", Type = ColumnTypeDef.Char36}
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"AgentID"}, Type = IndexType.Primary }
                }),
            new SchemaDefinition("osgroup",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "GroupID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "Name", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "Charter", Type = ColumnTypeDef.Text},
                    new ColumnDefinition {Name = "InsigniaID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "FounderID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "MembershipFee", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "OpenEnrollment", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "ShowInList", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "AllowPublish", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "MaturePublish", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "OwnerRoleID", Type = ColumnTypeDef.Char36}
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"GroupID"}, Type = IndexType.Primary },
                    new IndexDefinition() { Fields = new string[] {"Name"}, Type = IndexType.Unique }
                }),
            new SchemaDefinition("osgroupinvite",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "InviteID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "GroupID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "RoleID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "AgentID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "TMStamp", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "FromAgentName", Type = ColumnTypeDef.String50}
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"InviteID", "GroupID", "RoleID", "AgentID"}, Type = IndexType.Primary },
                    new IndexDefinition() { Fields = new string[] {"AgentID", "InviteID"}, Type = IndexType.Index }
                }),
            new SchemaDefinition("osgroupmembership",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "GroupID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "AgentID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "SelectedRoleID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "Contribution", Type = ColumnTypeDef.String45},
                    new ColumnDefinition {Name = "ListInProfile", Type = ColumnTypeDef.String45},
                    new ColumnDefinition {Name = "AcceptNotices", Type = ColumnTypeDef.String45}
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"GroupID", "AgentID"}, Type = IndexType.Primary },
                    new IndexDefinition() { Fields = new string[] {"AgentID"}, Type = IndexType.Index }
                }),
            new SchemaDefinition("osgroupnotice",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "GroupID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "NoticeID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "Timestamp", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "FromName", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "Subject", Type = ColumnTypeDef.String255},
                    new ColumnDefinition {Name = "Message", Type = ColumnTypeDef.Text},
                    new ColumnDefinition {Name = "HasAttachment", Type = ColumnTypeDef.TinyInt1},
                    new ColumnDefinition {Name = "ItemID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "AssetType", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "ItemName", Type = ColumnTypeDef.String50}
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"GroupID", "NoticeID", "Timestamp"}, Type = IndexType.Primary }
                }),
            new SchemaDefinition("osgrouprolemembership",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "GroupID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "RoleID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "AgentID", Type = ColumnTypeDef.Char36}
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"GroupID", "RoleID", "AgentID"}, Type = IndexType.Primary },
                    new IndexDefinition() { Fields = new string[] {"AgentID", "GroupID"}, Type = IndexType.Index }
                }),
            new SchemaDefinition("osrole",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "GroupID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "RoleID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "Name", Type = ColumnTypeDef.String255},
                    new ColumnDefinition {Name = "Description", Type = ColumnTypeDef.String255},
                    new ColumnDefinition {Name = "Title", Type = ColumnTypeDef.String255},
                    new ColumnDefinition {Name = "Powers", Type = ColumnTypeDef.String50}
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"GroupID", "RoleID"}, Type = IndexType.Primary },
                    new IndexDefinition() { Fields = new string[] {"RoleID"}, Type = IndexType.Index }
                }),
        };

        public GroupsMigrator_0()
        {
            Version = new Version(0, 1, 0);
            MigrationName = "Groups";
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