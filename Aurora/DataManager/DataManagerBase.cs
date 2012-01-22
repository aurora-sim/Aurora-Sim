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
using System.Data;
using System.Linq;
using System.Reflection;
using Aurora.Framework;

namespace Aurora.DataManager
{
    public abstract class DataManagerBase : IDataConnector
    {
        private const string VERSION_TABLE_NAME = "aurora_migrator_version";
        private const string COLUMN_NAME = "name";
        private const string COLUMN_VERSION = "version";

        #region IDataConnector Members

        public abstract string Identifier { get; }
        public abstract bool TableExists(string table);
        public abstract void CreateTable(string table, ColumnDefinition[] columns, IndexDefinition[] indexDefinitions);

        public Version GetAuroraVersion(string migratorName)
        {
            if (!TableExists(VERSION_TABLE_NAME))
            {
                CreateTable(VERSION_TABLE_NAME, new[]{
                    new ColumnDefinition{
                        Name = COLUMN_VERSION,
                        Type = ColumnTypes.String
                    },
                    new ColumnDefinition{
                        Name = COLUMN_NAME,
                        Type = ColumnTypes.String
                    }
                }, new IndexDefinition[0]);
            }

            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where[COLUMN_NAME] = migratorName;
            List<string> results = Query(new string[] { COLUMN_VERSION }, VERSION_TABLE_NAME, new QueryFilter
            {
                andFilters = where
            }, null, null, null);
            if (results.Count > 0)
            {
                Version[] highestVersion = {null};
#if (!ISWIN)
                foreach (string result in results)
                {
                    if (result.Trim() != string.Empty)
                    {
                        var version = new Version(result);
                        if (highestVersion[0] == null || version > highestVersion[0])
                        {
                            highestVersion[0] = version;
                        }
                    }
                }
#else
                foreach (var version in results.Where(result => result.Trim() != string.Empty).Select(result => new Version(result)).Where(version => highestVersion[0] == null || version > highestVersion[0]))
                {
                    highestVersion[0] = version;
                }
#endif
                return highestVersion[0];
            }

            return null;
        }

        public void WriteAuroraVersion(Version version, string MigrationName)
        {
            if (!TableExists(VERSION_TABLE_NAME))
            {
                CreateTable(VERSION_TABLE_NAME, new[]{new ColumnDefinition{
                    Name = COLUMN_VERSION,
                    Type = ColumnTypes.String100
                }}, new IndexDefinition[0]);
            }
            //Remove previous versions
            Delete(VERSION_TABLE_NAME, new string[1] {COLUMN_NAME}, new object[1] {MigrationName});
            //Add the new version
            Insert(VERSION_TABLE_NAME, new[] {version.ToString(), MigrationName});
        }

        public void CopyTableToTable(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions)
        {
            if (!TableExists(sourceTableName))
            {
                throw new MigrationOperationException("Cannot copy table to new name, source table does not exist: " + sourceTableName);
            }

            if (TableExists(destinationTableName))
            {
                this.DropTable(destinationTableName);
                if (TableExists(destinationTableName))
                    throw new MigrationOperationException("Cannot copy table to new name, table with same name already exists: " + destinationTableName);
            }

            if (!VerifyTableExists(sourceTableName, columnDefinitions, indexDefinitions))
            {
                throw new MigrationOperationException("Cannot copy table to new name, source table does not match columnDefinitions: " + destinationTableName);
            }

            EnsureTableExists(destinationTableName, columnDefinitions, indexDefinitions, null);
            CopyAllDataBetweenMatchingTables(sourceTableName, destinationTableName, columnDefinitions, indexDefinitions);
        }

        public bool VerifyTableExists(string tableName, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions)
        {
            if (!TableExists(tableName))
            {
                MainConsole.Instance.Warn("[DataMigrator]: Issue finding table " + tableName + " when verifing tables exist!");
                return false;
            }

            List<ColumnDefinition> extractedColumns = ExtractColumnsFromTable(tableName);
            List<ColumnDefinition> newColumns = new List<ColumnDefinition>(columnDefinitions);
            foreach (ColumnDefinition columnDefinition in columnDefinitions)
            {
                if (!extractedColumns.Contains(columnDefinition))
                {
#if (!ISWIN)
                    ColumnDefinition thisDef = null;
                    foreach (ColumnDefinition extractedDefinition in extractedColumns)
                    {
                        if (extractedDefinition.Name.ToLower() == columnDefinition.Name.ToLower())
                        {
                            thisDef = extractedDefinition;
                            break;
                        }
                    }
#else
                    ColumnDefinition thisDef = extractedColumns.FirstOrDefault(extractedDefinition => extractedDefinition.Name.ToLower() == columnDefinition.Name.ToLower());
#endif
                    //Check to see whether the two tables have the same type, but under different names
                    if (thisDef != null)
                    {
                        if (GetColumnTypeStringSymbol(thisDef.Type) == GetColumnTypeStringSymbol(columnDefinition.Type))
                        {
                            continue; //They are the same type, let them go on through
                        }
                    }
                    MainConsole.Instance.Warn("[DataMigrator]: Issue verifing table " + tableName + " column " + columnDefinition.Name + " when verifing tables exist, problem with new column definitions");
                    return false;
                }
            }
            foreach (ColumnDefinition columnDefinition in extractedColumns)
            {
                if (!newColumns.Contains(columnDefinition))
                {
#if (!ISWIN)
                    ColumnDefinition thisDef = null;
                    foreach (ColumnDefinition extractedDefinition in newColumns)
                    {
                        if (extractedDefinition.Name.ToLower() == columnDefinition.Name.ToLower())
                        {
                            thisDef = extractedDefinition;
                            break;
                        }
                    }
#else
                    ColumnDefinition thisDef = newColumns.FirstOrDefault(extractedDefinition => extractedDefinition.Name.ToLower() == columnDefinition.Name.ToLower());
#endif
                    //Check to see whether the two tables have the same type, but under different names
                    if (thisDef != null)
                    {
                        if (GetColumnTypeStringSymbol(thisDef.Type) == GetColumnTypeStringSymbol(columnDefinition.Type))
                        {
                            continue; //They are the same type, let them go on through
                        }
                    }
                    MainConsole.Instance.Debug("[DataMigrator]: Issue verifing table " + tableName + " column " + columnDefinition.Name + " when verifing tables exist, problem with old column definitions");
                    return false;
                }
            }

            Dictionary<string, IndexDefinition> ei = ExtractIndicesFromTable(tableName);
            List<IndexDefinition> extractedIndices = new List<IndexDefinition>(ei.Count);
            foreach(KeyValuePair<string, IndexDefinition> kvp in ei){
                extractedIndices.Add(kvp.Value);
            }
            List<IndexDefinition> newIndices = new List<IndexDefinition>(indexDefinitions);

            foreach (IndexDefinition indexDefinition in indexDefinitions)
            {
                if (!extractedIndices.Contains(indexDefinition))
                {
                    IndexDefinition thisDef = null;
                    foreach (IndexDefinition extractedDefinition in extractedIndices)
                    {
                        if (extractedDefinition.Equals(indexDefinition))
                        {
                            thisDef = extractedDefinition;
                            break;
                        }
                    }
                    if (thisDef == null)
                    {
                        MainConsole.Instance.Warn("[DataMigrator]: Issue verifing table " + tableName + " index " + indexDefinition.Type.ToString() + " (" + string.Join(", ", indexDefinition.Fields) + ") when verifing tables exist");
                        return false;
                    }
                }
            }
            foreach (IndexDefinition indexDefinition in extractedIndices)
            {
                if (!newIndices.Contains(indexDefinition))
                {
                    IndexDefinition thisDef = null;
                    foreach (IndexDefinition extractedDefinition in newIndices)
                    {
                        if (extractedDefinition.Equals(indexDefinition))
                        {
                            thisDef = extractedDefinition;
                            break;
                        }
                    }
                    if (thisDef == null)
                    {
                        MainConsole.Instance.Warn("[DataMigrator]: Issue verifing table " + tableName + " index " + indexDefinition.Type.ToString() + " (" + string.Join(", ", indexDefinition.Fields) + ") when verifing tables exist");
                        return false;
                    }
                }
            }

            return true;
        }

        public void EnsureTableExists(string tableName, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions, Dictionary<string, string> renameColumns)
        {
            if (TableExists(tableName))
            {
                if (!VerifyTableExists(tableName, columnDefinitions, indexDefinitions))
                {
                    //throw new MigrationOperationException("Cannot create, table with same name and different columns already exists. This should be fixed in a migration: " + tableName);
                    UpdateTable(tableName, columnDefinitions, indexDefinitions, renameColumns);
                }
                return;
            }

            CreateTable(tableName, columnDefinitions, indexDefinitions);
        }

        public void RenameTable(string oldTableName, string newTableName)
        {
            //Make sure that the old one exists and the new one doesn't
            if (TableExists(oldTableName) && !TableExists(newTableName))
            {
                ForceRenameTable(oldTableName, newTableName);
            }
        }
        public abstract void DropTable(string tableName);

        #endregion

        #region IGenericData members

        #region UPDATE

        public abstract bool Update(string table, object[] setValues, string[] setRows, string[] keyRows, object[] keyValues);

        public abstract bool DirectUpdate(string table, object[] setValues, string[] setRows, string[] keyRows, object[] keyValues);

        #endregion

        #region SELECT

        public abstract List<string> Query(string[] wantedValue, string table, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count);

        public abstract List<string> QueryFullData(string whereClause, string table, string wantedValue);

        public abstract IDataReader QueryData(string whereClause, string table, string wantedValue);

        public abstract Dictionary<string, List<string>> QueryNames(string[] keyRow, object[] keyValue, string table, string wantedValue);

        #endregion

        #region INSERT

        public abstract bool Insert(string table, object[] values);
        public abstract bool Insert(string table, string[] keys, object[] values);
        public abstract bool Insert(string table, object[] values, string updateKey, object updateValue);
        public abstract bool InsertMultiple(string table, List<object[]> values);

        #endregion

        #region REPLACE INTO

        public abstract bool Replace(string table, string[] keys, object[] values);
        public abstract bool DirectReplace(string table, string[] keys, object[] values);

        #endregion

        #region DELETE

        public abstract bool Delete(string table, string[] keys, object[] values);
        public abstract bool Delete(string table, string whereclause);
        public abstract bool DeleteByTime(string table, string key);

        #endregion

        public abstract void ConnectToDatabase(string connectionString, string migratorName, bool validateTables);

        public abstract void CloseDatabase();

        public abstract IGenericData Copy();
        public abstract string FormatDateTimeString(int time);
        public abstract string IsNull(string Field, string defaultValue);
        public abstract string ConCat(string[] toConcat);

        #endregion

        public abstract void UpdateTable(string table, ColumnDefinition[] columns, IndexDefinition[] indexDefinitions, Dictionary<string, string> renameColumns);

        public abstract string GetColumnTypeStringSymbol(ColumnTypes type);
        public abstract void ForceRenameTable(string oldTableName, string newTableName);

        protected abstract void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions);

        protected abstract List<ColumnDefinition> ExtractColumnsFromTable(string tableName);
        protected abstract Dictionary<string, IndexDefinition> ExtractIndicesFromTable(string tableName);
    }
}