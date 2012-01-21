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
using C5;

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
        public List<Rec<string, ColumnDefinition[], IndexDefinition[]>> schema;
        public List<Rec<string, IndexDefinition[]>> dropIndices;

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
            return new ColumnDefinition {Name = name, Type = columnType};
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
            schema.Add(new Rec<string, ColumnDefinition[], IndexDefinition[]>(table, definitions, indexes));
        }

        protected void RenameSchema(string oldTable, string newTable)
        {
            renameSchema.Add(oldTable, newTable);
        }

        protected void RemoveSchema(string table)
        {
            //Remove all of the tables that have this name
            schema.RemoveAll(delegate(Rec<string, ColumnDefinition[], IndexDefinition[]> r)
            {
                if (r.X1 == table)
                    return true;
                return false;
            });
        }

        protected void RemoveIndices(string table, IndexDefinition[] indexes)
        {
            dropIndices.Add(new Rec<string, IndexDefinition[]>(table, indexes));
        }

        protected void EnsureAllTablesInSchemaExist(IDataConnector genericData)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, string> r in renameSchema)
            {
                genericData.RenameTable(r.Key, r.Value);
            }
            foreach (var s in schema)
            {
                genericData.EnsureTableExists(s.X1, s.X2, s.X3, renameColumns);
            }
        }

        protected bool TestThatAllTablesValidate(IDataConnector genericData)
        {
#if (!ISWIN)
            foreach (Rec<string, ColumnDefinition[], IndexDefinition[]> s in schema)
            {
                if (!genericData.VerifyTableExists(s.X1, s.X2, s.X3)) return false;
            }
            return true;
#else
            return schema.All(s => genericData.VerifyTableExists(s.X1, s.X2, s.X3));
#endif
        }

        public bool DebugTestThatAllTablesValidate(IDataConnector genericData, out Rec<string, ColumnDefinition[], IndexDefinition[]> reason)
        {
            reason = new Rec<string, ColumnDefinition[], IndexDefinition[]>();
#if (!ISWIN)
            foreach (var s in schema)
            {
                if (!genericData.VerifyTableExists(s.X1, s.X2, s.X3))
                {
                    reason = s;
                    return false;
                }
            }
#else
            foreach (var s in schema.Where(s => !genericData.VerifyTableExists(s.X1, s.X2, s.X3)))
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
                CopyTableToTempVersion(genericData, s.X1, s.X2, s.X3);
            }
        }

        protected void RestoreTempTablesToReal(IDataConnector genericData)
        {
            foreach (var s in schema)
            {
                RestoreTempTableToReal(genericData, s.X1, s.X2, s.X3);
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
                DeleteTempVersion(genericData, s.X1);
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