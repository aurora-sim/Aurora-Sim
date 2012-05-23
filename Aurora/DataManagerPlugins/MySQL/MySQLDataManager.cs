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
using Aurora.DataManager.Migration;
using Aurora.Framework;
using MySql.Data.MySqlClient;
using OpenMetaverse;

namespace Aurora.DataManager.MySQL
{
    public class MySQLDataLoader : DataManagerBase
    {
        private string m_connectionString = "";

        public override string Identifier
        {
            get { return "MySQLData"; }
        }

        #region Database

        public override void ConnectToDatabase(string connectionstring, string migratorName, bool validateTables)
        {
            m_connectionString = connectionstring;
            MySqlConnection c = new MySqlConnection(connectionstring);
            int subStrA = connectionstring.IndexOf("Database=");
            int subStrB = connectionstring.IndexOf(";", subStrA);
            string noDatabaseConnector = m_connectionString.Substring(0, subStrA) + m_connectionString.Substring(subStrB+1);

            retry:
            try
            {
                ExecuteNonQuery(noDatabaseConnector, "create schema IF NOT EXISTS " + c.Database, new Dictionary<string, object>(), false);
            }
            catch
            {
                MainConsole.Instance.Error("[MySQLDatabase]: We cannot connect to the MySQL instance you have provided. Please make sure it is online, and then press enter to try again.");
                Console.ReadKey();
                goto retry;
            }

            var migrationManager = new MigrationManager(this, migratorName, validateTables);
            migrationManager.DetermineOperation();
            migrationManager.ExecuteOperation();
        }

        public void CloseDatabase(MySqlConnection connection)
        {
            //Interlocked.Decrement (ref m_locked);
            //connection.Close();
            //connection.Dispose();
        }

        public override void CloseDatabase()
        {
            //Interlocked.Decrement (ref m_locked);
            //m_connection.Close();
            //m_connection.Dispose();
        }

        #endregion

        #region Query

        public IDataReader Query(string sql, Dictionary<string, object> parameters)
        {
            try
            {
                MySqlParameter[] param = new MySqlParameter[parameters.Count];
                int i = 0;
                foreach (KeyValuePair<string, object> p in parameters)
                {
                    param[i] = new MySqlParameter(p.Key, p.Value);
                    i++;
                }
                return MySqlHelper.ExecuteReader(m_connectionString, sql, param);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Query(" + sql + "), " + e);
                return null;
            }
        }

        public void ExecuteNonQuery(string sql, Dictionary<string, object> parameters)
        {
            ExecuteNonQuery(m_connectionString, sql, parameters);
        }

        public void ExecuteNonQuery(string connStr, string sql, Dictionary<string, object> parameters)
        {
            ExecuteNonQuery(connStr, sql, parameters, true);
        }

        public void ExecuteNonQuery(string connStr, string sql, Dictionary<string, object> parameters, bool spamConsole)
        {
            try
            {
                MySqlParameter[] param = new MySqlParameter[parameters.Count];
                int i = 0;
                foreach (KeyValuePair<string, object> p in parameters)
                {
                    param[i] = new MySqlParameter(p.Key, p.Value);
                    i++;
                }
                MySqlHelper.ExecuteNonQuery(connStr, sql, param);
            }
            catch (Exception e)
            {
                if (spamConsole)
                    MainConsole.Instance.Error("[MySQLDataLoader] ExecuteNonQuery(" + sql + "), " + e);
                else
                    throw e;
            }
        }

        public override List<string> QueryFullData(string whereClause, string table, string wantedValue)
        {
            string query = String.Format("select {0} from {1} {2}", wantedValue, table, whereClause);
            return QueryFullData2(query);
        }

        public override List<string> QueryFullData(string whereClause, QueryTables tables, string wantedValue)
        {
            string query = string.Format("SELECT {0} FROM {1} {2}", wantedValue, tables.ToSQL(), whereClause);
            return QueryFullData2(query);
        }

        private List<string> QueryFullData2(string query)
        {
            IDataReader reader = null;
            List<string> retVal = new List<string>();
            try
            {
                using (reader = Query(query, new Dictionary<string, object>()))
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            retVal.Add(reader.GetString(i));
                        }
                    }
                    return retVal;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] QueryFullData(" + query + "), " + e);
                return null;
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                        //reader.Dispose ();
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[MySQLDataLoader] Query(" + query + "), " + e);
                }
            }
        }

        public override IDataReader QueryData(string whereClause, string table, string wantedValue)
        {
            string query = String.Format("select {0} from {1} {2}", wantedValue, table, whereClause);
            return QueryData2(query);
        }

        public override IDataReader QueryData(string whereClause, QueryTables tables, string wantedValue)
        {
            string query = string.Format("SELECT {0} FROM {1} {2}", wantedValue, tables.ToSQL(), whereClause);
            return QueryData2(query);
        }

        private IDataReader QueryData2(string query)
        {
            return Query(query, new Dictionary<string, object>());
        }

        public override List<string> Query(string[] wantedValue, string table, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count)
        {
            string query = string.Format("SELECT {0} FROM {1}", string.Join(", ", wantedValue), table);
            return Query2(query, queryFilter, sort, start, count);
        }

        public override List<string> Query(string[] wantedValue, QueryTables tables, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count)
        {
            string query = string.Format("SELECT {0} FROM {1}", string.Join(", ", wantedValue), tables.ToSQL());
            return Query2(query, queryFilter, sort, start, count);
        }

        private List<string> Query2(string sqll, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count)
        {
            string query = sqll;
            Dictionary<string, object> ps = new Dictionary<string,object>();
            List<string> retVal = new List<string>();
            List<string> parts = new List<string>();

            if (queryFilter != null && queryFilter.Count > 0)
            {
                query += " WHERE " + queryFilter.ToSQL('?', out ps);
            }

            if (sort != null && sort.Count > 0)
            {
                parts = new List<string>();
                foreach (KeyValuePair<string, bool> sortOrder in sort)
                {
                    parts.Add(string.Format("`{0}` {1}", sortOrder.Key, sortOrder.Value ? "ASC" : "DESC"));
                }
                query += " ORDER BY " + string.Join(", ", parts.ToArray());
            }

            if(start.HasValue){
                query += " LIMIT " + start.Value.ToString();
                if (count.HasValue)
                {
                    query += ", " + count.Value.ToString();
                }
            }

            IDataReader reader = null;
            int i = 0;
            try
            {
                using (reader = Query(query, ps))
                {
                    while (reader.Read())
                    {
                        for (i = 0; i < reader.FieldCount; i++)
                        {
                            Type r = reader[i].GetType();
                            retVal.Add(r == typeof(DBNull) ? null : reader.GetString(i));
                        }
                    }
                    return retVal;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Query(" + query + "), " + e);
                return null;
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                        //reader.Dispose ();
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[MySQLDataLoader] Query(" + query + "), " + e);
                }
            }
        }

        /*public override Dictionary<string, List<string>> QueryNames(string[] wantedValue, string table, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count)
        {
        }*/

        public override Dictionary<string, List<string>> QueryNames(string[] keyRow, object[] keyValue, string table, string wantedValue)
        {
            string query = String.Format("select {0} from {1} where ", wantedValue, table);
            return QueryNames2(keyRow, keyValue, query);
        }

        public override Dictionary<string, List<string>> QueryNames(string[] keyRow, object[] keyValue, QueryTables tables, string wantedValue)
        {
            string query = string.Format("SELECT {0} FROM {1} where ", wantedValue, tables.ToSQL());
            return QueryNames2(keyRow, keyValue, query);
        }

        private Dictionary<string, List<string>> QueryNames2(string[] keyRow, object[] keyValue, string query)
        {
            IDataReader reader = null;
            Dictionary<string, List<string>> retVal = new Dictionary<string, List<string>>();
            Dictionary<string, object> ps = new Dictionary<string, object>();
            int i = 0;
            foreach (object value in keyValue)
            {
                query += String.Format("{0} = ?{1} and ", keyRow[i], keyRow[i]);
                ps["?" + keyRow[i]] = value;
                i++;
            }
            query = query.Remove(query.Length - 5);

            try
            {
                using (reader = Query(query, ps))
                {
                    while (reader.Read())
                    {
                        for (i = 0; i < reader.FieldCount; i++)
                        {
                            Type r = reader[i].GetType();
                            AddValueToList(ref retVal, reader.GetName(i),
                                           r == typeof (DBNull) ? null : reader[i].ToString());
                        }
                    }
                    return retVal;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] QueryNames(" + query + "), " + e);
                return null;
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                        //reader.Dispose ();
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[MySQLDataLoader] QueryNames(" + query + "), " + e);
                }
            }
        }

        private void AddValueToList(ref Dictionary<string, List<string>> dic, string key, string value)
        {
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, new List<string>());
            }

            dic[key].Add(value);
        }

        #endregion

        #region Update

        public override bool Update(string table, Dictionary<string, object> values, Dictionary<string, int> incrementValues, QueryFilter queryFilter, uint? start, uint? count)
        {
            if ((values == null || values.Count < 1) && (incrementValues == null || incrementValues.Count < 1))
            {
                MainConsole.Instance.Warn("Update attempted with no values");
                return false;
            }

            string query = string.Format("UPDATE {0}", table); ;
            Dictionary<string, object> ps = new Dictionary<string, object>();

            string filter = "";
            if (queryFilter != null && queryFilter.Count > 0)
            {
                filter = " WHERE " + queryFilter.ToSQL('?', out ps);
            }

            List<string> parts = new List<string>();
            if (values != null)
            {
                foreach (KeyValuePair<string, object> value in values)
                {
                    string key = "?updateSet_" + value.Key.Replace("`", "");
                    ps[key] = value.Value;
                    parts.Add(string.Format("{0} = {1}", value.Key, key));
                }
            }
            if (incrementValues != null)
            {
                foreach (KeyValuePair<string, int> value in incrementValues)
                {
                    string key = "?updateSet_increment_" + value.Key.Replace("`", "");
                    ps[key] = value.Value;
                    parts.Add(string.Format("{0} = {0} + {1}", value.Key, key));
                }
            }

            query += " SET " + string.Join(", ", parts.ToArray()) + filter;

            if (start.HasValue)
            {
                query += " LIMIT " + start.Value.ToString();
                if (count.HasValue)
                {
                    query += ", " + count.Value.ToString();
                }
            }

            try
            {
                ExecuteNonQuery(query, ps);
            }
            catch (MySqlException e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Update(" + query + "), " + e);
            }
            return true;
        }

        #endregion

        #region Insert

        public override bool InsertMultiple(string table, List<object[]> values)
        {
            string query = String.Format("insert into {0} select ", table);
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            int i = 0;
            foreach (object[] value in values)
            {
                foreach (object v in value)
                {
                    parameters[Util.ConvertDecString(i)] = v;
                    query += "?" + Util.ConvertDecString(i++) + ",";
                }
                query = query.Remove(query.Length - 1);
                query += " union all select ";
            }
            query = query.Remove(query.Length - (" union all select ").Length);

            try
            {
                ExecuteNonQuery(query, parameters);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Insert(" + query + "), " + e);
            }
            return true;
        }

        public override bool Insert(string table, object[] values)
        {
            string query = String.Format("insert into {0} values (", table);
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            int i = 0;
            foreach (object o in values)
            {
                parameters[Util.ConvertDecString(i)] = o;
                query += "?" + Util.ConvertDecString(i++) + ",";
            }
            query = query.Remove(query.Length - 1);
            query += ")";

            try
            {
                ExecuteNonQuery(query, parameters);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Insert(" + query + "), " + e);
            }
            return true;
        }

        private bool InsertOrReplace(string table, Dictionary<string, object> row, bool insert)
        {
            string query = (insert ? "INSERT" : "REPLACE") + " INTO " + table + " (" + string.Join(", ", row.Keys.ToArray<string>()) + ")";
            Dictionary<string, object> ps = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> field in row)
            {
                string key = "?" + field.Key.Replace("`", "").Replace("(", "_").Replace(")", "").Replace(" ", "_").Replace("-", "minus").Replace("+", "add").Replace("/", "divide").Replace("*", "multiply");
                ps[key] = field.Value;
            }
            query += " VALUES( " + string.Join(", ", ps.Keys.ToArray<string>()) + " )";

            try
            {
                ExecuteNonQuery(query, ps);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] " + (insert ? "Insert" : "Replace") + "(" + query + "), " + e);
            }
            return true;
        }

        public override bool Insert(string table, Dictionary<string, object> row)
        {
            return InsertOrReplace(table, row, true);
        }

        public override bool Insert(string table, object[] values, string updateKey, object updateValue)
        {
            string query = String.Format("insert into {0} VALUES(", table);
            Dictionary<string, object> param = new Dictionary<string, object>();
            int i = 0;
            foreach (object o in values)
            {
                param["?" + Util.ConvertDecString(i)] = o;
                query += "?" + Util.ConvertDecString(i++) + ",";
            }
            param["?update"] = updateValue;
            query = query.Remove(query.Length - 1);
            query += String.Format(") ON DUPLICATE KEY UPDATE {0} = ?update", updateKey);
            try
            {
                ExecuteNonQuery(query, param);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Insert(" + query + "), " + e);
                return false;
            }
            return true;
        }

        public override bool InsertSelect(string tableA, string[] fieldsA, string tableB, string[] valuesB)
        {
            string query = string.Format("INSERT INTO {0}{1} SELECT {2} FROM {3}",
                tableA,
                (fieldsA.Length > 0 ? " (" + string.Join(", ", fieldsA) + ")" : ""),
                string.Join(", ", valuesB),
                tableB
            );

            try
            {
                ExecuteNonQuery(query, new Dictionary<string,object>(0));
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] INSERT .. SELECT (" + query + "), " + e);
            }
            return true;
        }

        #endregion

        #region REPLACE INTO

        public override bool Replace(string table, Dictionary<string, object> row)
        {
            return InsertOrReplace(table, row, false);
        }

        #endregion

        #region Delete

        public override bool DeleteByTime(string table, string key)
        {
            QueryFilter filter = new QueryFilter();
            filter.andLessThanEqFilters["(UNIX_TIMESTAMP(`" + key.Replace("`","") + "`) - UNIX_TIMESTAMP())"] = 0;

            return Delete(table, filter);
        }

        public override bool Delete(string table, QueryFilter queryFilter)
        {
            Dictionary<string, object> ps = new Dictionary<string,object>();
            string query = "DELETE FROM " + table + (queryFilter != null ? (" WHERE " + queryFilter.ToSQL('?', out ps)) : "");

            try
            {
                ExecuteNonQuery(query, ps);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] Delete(" + query + "), " + e);
                return false;
            }
            return true;
        }

        #endregion

        public override string ConCat(string[] toConcat)
        {
#if (!ISWIN)
            string returnValue = "concat(";
            foreach (string s in toConcat)
                returnValue = returnValue + (s + ",");
#else
            string returnValue = toConcat.Aggregate("concat(", (current, s) => current + (s + ","));
#endif
            return returnValue.Substring(0, returnValue.Length - 1) + ")";
        }

        #region Tables

        public override void CreateTable(string table, ColumnDefinition[] columns, IndexDefinition[] indices)
        {
            table = table.ToLower();
            if (TableExists(table))
            {
                throw new DataManagerException("Trying to create a table with name of one that already exists.");
            }

            IndexDefinition primary = null;
            foreach (IndexDefinition index in indices)
            {
                if (index.Type == IndexType.Primary)
                {
                    primary = index;
                    break;
                }
            }

            List<string> columnDefinition = new List<string>();

            foreach (ColumnDefinition column in columns)
            {
                columnDefinition.Add("`" + column.Name + "` " + GetColumnTypeStringSymbol(column.Type));
            }
            if (primary != null && primary.Fields.Length > 0)
            {
                columnDefinition.Add("PRIMARY KEY (`" + string.Join("`, `", primary.Fields) + "`)");
            }

            List<string> indicesQuery = new List<string>(indices.Length);
            foreach (IndexDefinition index in indices)
            {
                string type = "KEY";
                switch (index.Type)
                {
                    case IndexType.Primary:
                        continue;
                    case IndexType.Unique:
                        type = "UNIQUE";
                        break;
                    case IndexType.Index:
                    default:
                        type = "KEY";
                        break;
                }
                indicesQuery.Add(string.Format("{0}( {1} )", type, "`" + string.Join("`, `", index.Fields) + "`"));
            }

            string query = string.Format("create table " + table + " ( {0} {1}) ", string.Join(", ", columnDefinition.ToArray()), indicesQuery.Count > 0 ? ", " + string.Join(", ", indicesQuery.ToArray()) : string.Empty);

            try
            {
                ExecuteNonQuery(query, new Dictionary<string, object>());
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] CreateTable", e);
            }
        }

        public override void UpdateTable(string table, ColumnDefinition[] columns, IndexDefinition[] indices, Dictionary<string, string> renameColumns)
        {
            table = table.ToLower();
            if (!TableExists(table))
            {
                throw new DataManagerException("Trying to update a table with name of one that does not exist.");
            }

            List<ColumnDefinition> oldColumns = ExtractColumnsFromTable(table);

            Dictionary<string, ColumnDefinition> removedColumns = new Dictionary<string, ColumnDefinition>();
            Dictionary<string, ColumnDefinition> modifiedColumns = new Dictionary<string, ColumnDefinition>();
#if (!ISWIN)
            Dictionary<string, ColumnDefinition> addedColumns = new Dictionary<string, ColumnDefinition>();
            foreach (ColumnDefinition column in columns)
            {
                if (!oldColumns.Contains(column)) addedColumns.Add(column.Name.ToLower(), column);
            }
            foreach (ColumnDefinition column in oldColumns)
            {
                if (!columns.Contains(column))
                {
                    if (addedColumns.ContainsKey(column.Name.ToLower()))
                    {
                        if (column.Name.ToLower() != addedColumns[column.Name.ToLower()].Name.ToLower() || column.Type != addedColumns[column.Name.ToLower()].Type)
                        {
                            modifiedColumns.Add(column.Name.ToLower(), addedColumns[column.Name.ToLower()]);
                        }
                        addedColumns.Remove(column.Name.ToLower());
                    }
                    else
                    {
                        removedColumns.Add(column.Name.ToLower(), column);
                    }
                }
            }
#else
            Dictionary<string, ColumnDefinition> addedColumns = columns.Where(column => !oldColumns.Contains(column)).ToDictionary(column => column.Name.ToLower());
            foreach (ColumnDefinition column in oldColumns.Where(column => !columns.Contains(column)))
            {
                if (addedColumns.ContainsKey(column.Name.ToLower()))
                {
                    if (column.Name.ToLower() != addedColumns[column.Name.ToLower()].Name.ToLower() || column.Type != addedColumns[column.Name.ToLower()].Type)
                    {
                        modifiedColumns.Add(column.Name.ToLower(), addedColumns[column.Name.ToLower()]);
                    }
                    addedColumns.Remove(column.Name.ToLower());
                }
                else{
                    removedColumns.Add(column.Name.ToLower(), column);
                }
            }
#endif


            try
            {
#if (!ISWIN)
                foreach (ColumnDefinition column in addedColumns.Values)
                {
                    string addedColumnsQuery = "add `" + column.Name + "` " + GetColumnTypeStringSymbol(column.Type) + " ";
                    string query = string.Format("alter table " + table + " " + addedColumnsQuery);
                    ExecuteNonQuery(query, new Dictionary<string, object>());
                }
                foreach (ColumnDefinition column in modifiedColumns.Values)
                {
                    string modifiedColumnsQuery = "modify column `" + column.Name + "` " + GetColumnTypeStringSymbol(column.Type) + " ";
                    string query = string.Format("alter table " + table + " " + modifiedColumnsQuery);
                    ExecuteNonQuery(query, new Dictionary<string, object>());
                }
                foreach (ColumnDefinition column in removedColumns.Values)
                {
                    string droppedColumnsQuery = "drop `" + column.Name + "` ";
                    string query = string.Format("alter table " + table + " " + droppedColumnsQuery);
                    ExecuteNonQuery(query, new Dictionary<string, object>());
                }
#else
                foreach (string query in addedColumns.Values.Select(column => "add `" + column.Name + "` " + GetColumnTypeStringSymbol(column.Type) +
                                                                              " ").Select(addedColumnsQuery => string.Format("alter table " + table + " " + addedColumnsQuery)))
                {
                    ExecuteNonQuery(query, new Dictionary<string, object>());
                }
                foreach (string query in modifiedColumns.Values.Select(column => "modify column `" + column.Name + "` " +
                                                                                 GetColumnTypeStringSymbol(column.Type) + " ").Select(modifiedColumnsQuery => string.Format("alter table " + table + " " + modifiedColumnsQuery)))
                {
                    ExecuteNonQuery(query, new Dictionary<string, object>());
                }
                foreach (string query in removedColumns.Values.Select(column => "drop `" + column.Name + "` ").Select(droppedColumnsQuery => string.Format("alter table " + table + " " + droppedColumnsQuery)))
                {
                    ExecuteNonQuery(query, new Dictionary<string, object>());
                }
#endif
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] UpdateTable", e);
            }

            Dictionary<string, IndexDefinition> oldIndicesDict = ExtractIndicesFromTable(table);

            List<string> removeIndices = new List<string>();
            List<string> oldIndexNames = new List<string>(oldIndicesDict.Count);
            List<IndexDefinition> oldIndices = new List<IndexDefinition>(oldIndicesDict.Count);
            List<IndexDefinition> newIndices = new List<IndexDefinition>();

            foreach (KeyValuePair<string, IndexDefinition> oldIndex in oldIndicesDict)
            {
                oldIndexNames.Add(oldIndex.Key);
                oldIndices.Add(oldIndex.Value);
            }
            int i=0;
            foreach(IndexDefinition oldIndex in oldIndices){
                bool found = false;
                foreach (IndexDefinition newIndex in indices)
                {
                    if (oldIndex.Equals(newIndex))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    removeIndices.Add(oldIndexNames[i]);
                }
                ++i;
            }

            foreach (IndexDefinition newIndex in indices)
            {
                bool found = false;
                foreach (IndexDefinition oldIndex in oldIndices)
                {
                    if (oldIndex.Equals(newIndex))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    newIndices.Add(newIndex);
                }
            }

            foreach (string oldIndex in removeIndices)
            {
                ExecuteNonQuery(string.Format("ALTER TABLE `{0}` DROP INDEX `{1}`", table, oldIndex), new Dictionary<string, object>());
            }
            foreach (IndexDefinition newIndex in newIndices)
            {
                ExecuteNonQuery(string.Format("ALTER TABLE `{0}` ADD {1} (`{2}`)", table, newIndex.Type == IndexType.Primary ? "PRIMARY KEY" : (newIndex.Type == IndexType.Unique ? "UNIQUE" : "INDEX"), string.Join("`, `", newIndex.Fields)), new Dictionary<string,object>());
            }
        }

        public override string GetColumnTypeStringSymbol(ColumnTypes type)
        {
            switch (type)
            {
                case ColumnTypes.Double:
                    return "DOUBLE";
                case ColumnTypes.Integer11:
                    return "int(11)";
                case ColumnTypes.Integer30:
                    return "int(30)";
                case ColumnTypes.UInteger11:
                    return "INT(11) UNSIGNED";
                case ColumnTypes.UInteger30:
                    return "INT(30) UNSIGNED";
                case ColumnTypes.Char36:
                    return "char(36)";
                case ColumnTypes.Char32:
                    return "char(32)";
                case ColumnTypes.Char5:
                    return "char(5)";
                case ColumnTypes.String:
                    return "TEXT";
                case ColumnTypes.String1:
                    return "VARCHAR(1)";
                case ColumnTypes.String2:
                    return "VARCHAR(2)";
                case ColumnTypes.String10:
                    return "VARCHAR(10)";
                case ColumnTypes.String16:
                    return "VARCHAR(16)";
                case ColumnTypes.String30:
                    return "VARCHAR(30)";
                case ColumnTypes.String32:
                    return "VARCHAR(32)";
                case ColumnTypes.String36:
                    return "VARCHAR(36)";
                case ColumnTypes.String45:
                    return "VARCHAR(45)";
                case ColumnTypes.String50:
                    return "VARCHAR(50)";
                case ColumnTypes.String64:
                    return "VARCHAR(64)";
                case ColumnTypes.String128:
                    return "VARCHAR(128)";
                case ColumnTypes.String100:
                    return "VARCHAR(100)";
                case ColumnTypes.String255:
                    return "VARCHAR(255)";
                case ColumnTypes.String512:
                    return "VARCHAR(512)";
                case ColumnTypes.String1024:
                    return "VARCHAR(1024)";
                case ColumnTypes.String8196:
                    return "VARCHAR(8196)";
                case ColumnTypes.Text:
                    return "TEXT";
                case ColumnTypes.MediumText:
                    return "MEDIUMTEXT";
                case ColumnTypes.LongText:
                    return "LONGTEXT";
                case ColumnTypes.Blob:
                    return "blob";
                case ColumnTypes.LongBlob:
                    return "longblob";
                case ColumnTypes.Date:
                    return "DATE";
                case ColumnTypes.DateTime:
                    return "DATETIME";
                case ColumnTypes.Float:
                    return "float";
                case ColumnTypes.TinyInt1:
                    return "TINYINT(1)";
                case ColumnTypes.TinyInt4:
                    return "TINYINT(4)";
                default:
                    throw new DataManagerException("Unknown column type.");
            }
        }

        public override string GetColumnTypeStringSymbol(ColumnTypeDef coldef)
        {
            string symbol;
            switch (coldef.Type)
            {
                case ColumnType.Blob:
                    symbol = "BLOB";
                    break;
                case ColumnType.LongBlob:
                    symbol = "LONGBLOB";
                    break;
                case ColumnType.Boolean:
                    symbol = "TINYINT(1)";
                    break;
                case ColumnType.Char:
                    symbol = "CHAR(" + coldef.Size + ")";
                    break;
                case ColumnType.Date:
                    symbol = "DATE";
                    break;
                case ColumnType.DateTime:
                    symbol = "DATETIME";
                    break;
                case ColumnType.Double:
                    symbol = "DOUBLE";
                    break;
                case ColumnType.Float:
                    symbol = "FLOAT";
                    break;
                case ColumnType.Integer:
                    symbol = "INT(" + coldef.Size + ")" + (coldef.unsigned ? " unsigned" : "");
                    break;
                case ColumnType.TinyInt:
                    symbol = "TINYINT(" + coldef.Size + ")" + (coldef.unsigned ? " unsigned" : "");
                    break;
                case ColumnType.String:
                    symbol = "VARCHAR(" + coldef.Size + ")";
                    break;
                case ColumnType.Text:
                    symbol = "TEXT";
                    break;
                case ColumnType.MediumText:
                    symbol = "MEDIUMTEXT";
                    break;
                case ColumnType.LongText:
                    symbol = "LONGTEXT";
                    break;
                case ColumnType.UUID:
                    symbol = "CHAR(36)";
                    break;
                default:
                    throw new DataManagerException("Unknown column type.");
            }

            return symbol + (coldef.isNull ? " NULL" : " NOT NULL") + ((coldef.isNull && coldef.defaultValue == null) ? " DEFAULT NULL" : (coldef.defaultValue != null ? " DEFAULT '" + coldef.defaultValue.MySqlEscape() + "'" : "")) + ((coldef.Type == ColumnType.Integer || coldef.Type == ColumnType.TinyInt) && coldef.auto_increment ? " AUTO_INCREMENT" : "");
        }

        public override void DropTable(string tableName)
        {
            tableName = tableName.ToLower();
            try
            {
                ExecuteNonQuery(string.Format("drop table {0}", tableName), new Dictionary<string, object>());
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] DropTable", e);
            }
        }

        public override void ForceRenameTable(string oldTableName, string newTableName)
        {
            newTableName = newTableName.ToLower();
            try
            {
                ExecuteNonQuery(string.Format("RENAME TABLE {0} TO {1}", oldTableName, newTableName),
                                new Dictionary<string, object>());
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] ForceRenameTable", e);
            }
        }

        protected override void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions)
        {
            sourceTableName = sourceTableName.ToLower();
            destinationTableName = destinationTableName.ToLower();
            try
            {
                ExecuteNonQuery(string.Format("insert into {0} select * from {1}", destinationTableName, sourceTableName), new Dictionary<string, object>());
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] CopyAllDataBetweenMatchingTables", e);
            }
        }

        public override bool TableExists(string table)
        {
            IDataReader reader = null;
            List<string> retVal = new List<string>();
            try
            {
                using (reader = Query("show tables", new Dictionary<string, object>()))
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            retVal.Add(reader.GetString(i).ToLower());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] TableExists", e);
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                        //reader.Dispose ();
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[MySQLDataLoader] TableExists", e);
                }
            }
            return retVal.Contains(table.ToLower());
        }

        protected override List<ColumnDefinition> ExtractColumnsFromTable(string tableName)
        {
            var defs = new List<ColumnDefinition>();
            tableName = tableName.ToLower();
            IDataReader rdr = null;
            try
            {
                rdr = Query(string.Format("desc {0}", tableName), new Dictionary<string, object>());
                while (rdr.Read())
                {
                    var name = rdr["Field"];
                    //var pk = rdr["Key"];
                    var type = rdr["Type"];
                    //var extra = rdr["Extra"];
                    object defaultValue = rdr["Default"];

                    ColumnTypeDef typeDef = ConvertTypeToColumnType(type.ToString());
                    typeDef.isNull = rdr["Null"].ToString() == "YES";
                    typeDef.auto_increment = rdr["Extra"].ToString().IndexOf("auto_increment") >= 0;
                    typeDef.defaultValue = defaultValue.GetType() == typeof(System.DBNull) ? null : defaultValue.ToString();
                    defs.Add(new ColumnDefinition
                    {
                        Name = name.ToString(),
                        Type = typeDef,
                    });
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] ExtractColumnsFromTable", e);
            }
            finally
            {
                try
                {
                    if (rdr != null)
                    {
                        rdr.Close();
                        //rdr.Dispose ();
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Debug("[MySQLDataLoader] ExtractColumnsFromTable", e);
                }
            }
            return defs;
        }

        protected override Dictionary<string, IndexDefinition> ExtractIndicesFromTable(string tableName)
        {
            Dictionary<string, IndexDefinition> defs = new Dictionary<string, IndexDefinition>();
            tableName = tableName.ToLower();
            IDataReader rdr = null;
            Dictionary<string, Dictionary<uint, string>> indexLookup = new Dictionary<string, Dictionary<uint, string>>();
            Dictionary<string, bool> indexIsUnique = new Dictionary<string,bool>();

            try
            {
                rdr = Query(string.Format("SHOW INDEX IN {0}", tableName), new Dictionary<string, object>());
                while (rdr.Read())
                {
                    string name = rdr["Column_name"].ToString();
                    bool unique = uint.Parse(rdr["Non_unique"].ToString()) == 0;
                    string index = rdr["Key_name"].ToString();
                    uint sequence = uint.Parse(rdr["Seq_in_index"].ToString());
                    if (indexLookup.ContainsKey(index) == false)
                    {
                        indexLookup[index] = new Dictionary<uint, string>();
                    }
                    indexIsUnique[index] = unique;
                    indexLookup[index][sequence - 1] = name;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[MySQLDataLoader] ExtractIndicesFromTable", e);
            }
            finally
            {
                try
                {
                    if (rdr != null)
                    {
                        rdr.Close();
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Debug("[MySQLDataLoader] ExtractIndicesFromTable", e);
                }
            }

            foreach (KeyValuePair<string, Dictionary<uint, string>> index in indexLookup)
            {
                index.Value.OrderBy(x=>x.Key);
                defs[index.Key] = new IndexDefinition
                {
                    Fields = index.Value.Values.ToArray<string>(),
                    Type = (indexIsUnique[index.Key] ? (index.Key == "PRIMARY" ? IndexType.Primary : IndexType.Unique) : IndexType.Index)
                };
            }

            return defs;
        }
        
        #endregion

        public override IGenericData Copy()
        {
            return new MySQLDataLoader();
        }
    }
}