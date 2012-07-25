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
using System.Linq;
using Aurora.Framework;

namespace Aurora.DataManager.Migration
{
    public interface IMigrator
    {
        string MigrationName { get; }
    }

    public class Migrator : IMigrator, IRestorePoint
    {
        private readonly Dictionary<string, string> renameSchema = new Dictionary<string, string>();
        public Dictionary<string, string> renameColumns = new Dictionary<string, string>();
        public List<SchemaDefinition> schema;

        public Version Version { get; protected set; }

        #region IMigrator Members

        public String MigrationName { get; protected set; }

        #endregion

        #region IRestorePoint Members

        public virtual void DoRestore(IDataConnector genericData)
        {
            RestoreTempTablesToReal(genericData);
        }

        #endregion

        public bool Validate(IDataConnector genericData)
        {
            if (genericData.GetAuroraVersion(MigrationName) != Version)
            {
                return false;
            }
            return DoValidate(genericData);
        }

        protected virtual bool DoValidate(IDataConnector genericData)
        {
            return true;
        }

        public IRestorePoint PrepareRestorePoint(IDataConnector genericData)
        {
            DoPrepareRestorePoint(genericData);
            return this;
        }

        protected virtual void DoPrepareRestorePoint(IDataConnector genericData)
        {
        }

        public void Migrate(IDataConnector genericData)
        {
            DoMigrate(genericData);
            genericData.WriteAuroraVersion(Version, MigrationName);
        }

        protected virtual void DoMigrate(IDataConnector genericData)
        {
        }

        public void CreateDefaults(IDataConnector genericData)
        {
            DoCreateDefaults(genericData);
            genericData.WriteAuroraVersion(Version, MigrationName);
        }

        protected virtual void DoCreateDefaults(IDataConnector genericData)
        {
        }

        protected ColumnDefinition[] ColDefs(params ColumnDefinition[] defs)
        {
            return defs;
        }

        protected ColumnDefinition ColDef(string name, ColumnTypes columnType)
        {
            ColumnTypeDef type = new ColumnTypeDef();
            switch (columnType)
            {
                case ColumnTypes.Blob:
                    type.Type = ColumnType.Blob;
                    break;
                case ColumnTypes.Char32:
                    type.Type = ColumnType.Char;
                    type.Size = 32;
                    break;
                case ColumnTypes.Char36:
                    type.Type = ColumnType.Char;
                    type.Size = 36;
                    break;
                case ColumnTypes.Char5:
                    type.Type = ColumnType.Char;
                    type.Size = 5;
                    break;
                case ColumnTypes.Date:
                    type.Type = ColumnType.Date;
                    break;
                case ColumnTypes.DateTime:
                    type.Type = ColumnType.DateTime;
                    break;
                case ColumnTypes.Double:
                    type.Type = ColumnType.Double;
                    break;
                case ColumnTypes.Float:
                    type.Type = ColumnType.Float;
                    break;
                case ColumnTypes.Integer11:
                    type.Type = ColumnType.Integer;
                    type.Size = 11;
                    break;
                case ColumnTypes.Integer30:
                    type.Type = ColumnType.Integer;
                    type.Size = 30;
                    break;
                case ColumnTypes.LongBlob:
                    type.Type = ColumnType.LongBlob;
                    break;
                case ColumnTypes.LongText:
                    type.Type = ColumnType.LongText;
                    break;
                case ColumnTypes.MediumText:
                    type.Type = ColumnType.MediumText;
                    break;
                case ColumnTypes.String:
                    type.Type = ColumnType.Text;
                    break;
                case ColumnTypes.String1:
                    type.Type = ColumnType.String;
                    type.Size = 1;
                    break;
                case ColumnTypes.String10:
                    type.Type = ColumnType.String;
                    type.Size = 10;
                    break;
                case ColumnTypes.String100:
                    type.Type = ColumnType.String;
                    type.Size = 100;
                    break;
                case ColumnTypes.String1024:
                    type.Type = ColumnType.String;
                    type.Size = 1024;
                    break;
                case ColumnTypes.String128:
                    type.Type = ColumnType.String;
                    type.Size = 128;
                    break;
                case ColumnTypes.String16:
                    type.Type = ColumnType.String;
                    type.Size = 16;
                    break;
                case ColumnTypes.String2:
                    type.Type = ColumnType.String;
                    type.Size = 2;
                    break;
                case ColumnTypes.String255:
                    type.Type = ColumnType.String;
                    type.Size = 255;
                    break;
                case ColumnTypes.String30:
                    type.Type = ColumnType.String;
                    type.Size = 30;
                    break;
                case ColumnTypes.String32:
                    type.Type = ColumnType.String;
                    type.Size = 32;
                    break;
                case ColumnTypes.String36:
                    type.Type = ColumnType.String;
                    type.Size = 36;
                    break;
                case ColumnTypes.String45:
                    type.Type = ColumnType.String;
                    type.Size = 45;
                    break;
                case ColumnTypes.String50:
                    type.Type = ColumnType.String;
                    type.Size = 50;
                    break;
                case ColumnTypes.String512:
                    type.Type = ColumnType.String;
                    type.Size = 512;
                    break;
                case ColumnTypes.String64:
                    type.Type = ColumnType.String;
                    type.Size = 64;
                    break;
                case ColumnTypes.String8196:
                    type.Type = ColumnType.String;
                    type.Size = 8196;
                    break;
                case ColumnTypes.Text:
                    type.Type = ColumnType.Text;
                    break;
                case ColumnTypes.TinyInt1:
                    type.Type = ColumnType.TinyInt;
                    type.Size = 1;
                    break;
                case ColumnTypes.TinyInt4:
                    type.Type = ColumnType.TinyInt;
                    type.Size = 4;
                    break;
                default:
                    type.Type = ColumnType.Unknown;
                    break;
            }
            return new ColumnDefinition {Name = name, Type = type};
        }

        protected IndexDefinition[] IndexDefs(params IndexDefinition[] defs)
        {
            return defs;
        }

        protected IndexDefinition IndexDef(string[] fields, IndexType indexType)
        {
            return new IndexDefinition { Fields = fields, Type = indexType };
        }

        protected void AddSchema(string table, ColumnDefinition[] definitions)
        {
            AddSchema(table, definitions, new IndexDefinition[0]);
        }

        protected void AddSchema(string table, ColumnDefinition[] definitions, IndexDefinition[] indexes)
        {
            schema.Add(new SchemaDefinition(table, definitions, indexes));
        }

        protected void RenameSchema(string oldTable, string newTable)
        {
            renameSchema.Add(oldTable, newTable);
        }

        protected void RemoveSchema(string table)
        {
            //Remove all of the tables that have this name
            schema.RemoveAll(delegate(SchemaDefinition r)
            {
                if (r.Name == table)
                    return true;
                return false;
            });
        }

        protected void EnsureAllTablesInSchemaExist(IDataConnector genericData)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, string> r in renameSchema)
            {
                genericData.RenameTable(r.Key, r.Value);
            }
            foreach (var s in schema)
            {
                genericData.EnsureTableExists(s.Name, s.Columns, s.Indices, renameColumns);
            }
        }

        protected bool TestThatAllTablesValidate(IDataConnector genericData)
        {
#if (!ISWIN)
            foreach (SchemaDefinition s in schema)
            {
                if (!genericData.VerifyTableExists(s.Name, s.Columns, s.Indices)) return false;
            }
            return true;
#else
            return schema.All(s => genericData.VerifyTableExists(s.Name, s.Columns, s.Indices));
#endif
        }

        public bool DebugTestThatAllTablesValidate(IDataConnector genericData, out SchemaDefinition reason)
        {
            reason = null;
#if (!ISWIN)
            foreach (var s in schema)
            {
                if (!genericData.VerifyTableExists(s.Name, s.Columns, s.Indices))
                {
                    reason = s;
                    return false;
                }
            }
#else
            foreach (var s in schema.Where(s => !genericData.VerifyTableExists(s.Name, s.Columns, s.Indices)))
            {
                reason = s;
                return false;
            }
#endif
            return true;
        }

        protected void CopyAllTablesToTempVersions(IDataConnector genericData)
        {
            foreach (var s in schema)
            {
                CopyTableToTempVersion(genericData, s.Name, s.Columns, s.Indices);
            }
        }

        protected void RestoreTempTablesToReal(IDataConnector genericData)
        {
            foreach (var s in schema)
            {
                RestoreTempTableToReal(genericData, s.Name, s.Columns, s.Indices);
            }
        }

        private void CopyTableToTempVersion(IDataConnector genericData, string tablename, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions)
        {
            genericData.CopyTableToTable(tablename, GetTempTableNameFromTableName(tablename), columnDefinitions, indexDefinitions);
        }

        private string GetTempTableNameFromTableName(string tablename)
        {
            return tablename + "_temp";
        }

        private void RestoreTempTableToReal(IDataConnector genericData, string tablename, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions)
        {
            genericData.CopyTableToTable(GetTempTableNameFromTableName(GetTempTableNameFromTableName(tablename)), tablename, columnDefinitions, indexDefinitions);
        }

        public void ClearRestorePoint(IDataConnector genericData)
        {
            foreach (var s in schema)
            {
                DeleteTempVersion(genericData, s.Name);
            }
        }

        private void DeleteTempVersion(IDataConnector genericData, string tableName)
        {
            string tempTableName = GetTempTableNameFromTableName(tableName);
            if (genericData.TableExists(tempTableName))
            {
                genericData.DropTable(tempTableName);
            }
        }

        public virtual void FinishedMigration(IDataConnector genericData)
        {
        }
    }
}