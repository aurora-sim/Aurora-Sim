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
using System.IO;
using System.Linq;
using System.Reflection;
using Aurora.DataManager.Migration;
using Aurora.Framework;
//using System.Data.Sqlite;
using Community.CsharpSqlite.SQLiteClient;
using OpenMetaverse;

namespace Aurora.DataManager.SQLite
{
    public class SQLiteLoader : DataManagerBase
    {
        protected Dictionary<string, FieldInfo> m_Fields = new Dictionary<string, FieldInfo>();
        protected string _connectionString;
        protected static bool _hadToConvert = false;
        protected static Dictionary<string, object> _locks = new Dictionary<string, object>();
        protected string _fileName;

        protected object GetLock()
        {
            lock (_locks)
            {
                if (!_locks.ContainsKey(_fileName))
                    _locks.Add(_fileName, new object());
                return _locks[_fileName];
            }
        }

        public override string Identifier
        {
            get { return "SQLiteConnector"; }
        }

        #region Database

        public override void ConnectToDatabase(string connectionString, string migratorName, bool validateTables)
        {
            _connectionString = connectionString;
            string[] s1 = _connectionString.Split(new[] { "Data Source=", "," }, StringSplitOptions.RemoveEmptyEntries);
            bool needsUTFConverted = false;
            _fileName = Path.GetFileName(s1[0]);
            if (s1[0].EndsWith(";"))
            {
                _fileName = Path.GetFileNameWithoutExtension(s1[1].Substring(7, s1[1].Length - 7)) + "utf8.db";
                _connectionString = "Data Source=file://" + _fileName;
                s1 = new string[1] { "file://" + _fileName };
                needsUTFConverted = true;
                _hadToConvert = true;
            }
            if (_fileName == s1[0]) //Only add this if we arn't an absolute path already
                _connectionString = _connectionString.Replace("Data Source=", "Data Source=" + Util.BasePathCombine("") + "\\");
            SqliteConnection connection = new SqliteConnection(_connectionString);
            connection.Open();
            var migrationManager = new MigrationManager(this, migratorName, validateTables);
            migrationManager.DetermineOperation();
            migrationManager.ExecuteOperation();
            connection.Close();

            if (needsUTFConverted && _hadToConvert)
            {
                string file = connectionString.Split(new[] { "Data Source=", "," }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(7);
                if (File.Exists(file))
                {
                    //UTF16 db, gotta convert it
                    System.Data.SQLite.SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection("Data Source=" + file + ";version=3;UseUTF16Encoding=True");
                    conn.Open();
                    var RetVal = new List<string>();
                    using (var cmd = new System.Data.SQLite.SQLiteCommand("SELECT name FROM Sqlite_master", conn))
                    {
                        using (IDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                for (int i = 0; i < rdr.FieldCount; i++)
                                {
                                    RetVal.Add(rdr.GetValue(i).ToString());
                                }
                            }
                        }
                    }
                    foreach (string table in RetVal)
                    {
                        if (TableExists(table) && !table.StartsWith("sqlite") && !table.StartsWith("idx_") && table != "aurora_migrator_version")
                        {
                            var retVal = new List<object[]>();
                            using (var cmd = new System.Data.SQLite.SQLiteCommand("SELECT * FROM " + table, conn))
                            {
                                using (IDataReader reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        List<object> obs = new List<object>();
                                        for (int i = 0; i < reader.FieldCount; i++)
                                        {
                                            Type r = reader[i].GetType();
                                            if (r == typeof(DBNull))
                                                obs.Add(null);
                                            else
                                                obs.Add(reader[i].ToString());
                                        }
                                        retVal.Add(obs.ToArray());
                                    }
                                }
                            }
                            try
                            {
                                if(retVal.Count > 0)
                                    InsertMultiple(table, retVal);
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        public override void CloseDatabase(DataReaderConnection conn)
        {
            if (conn == null)
                return;
            if (conn.DataReader != null)
                conn.DataReader.Close();
            if (conn != null && conn.Connection != null && conn.Connection is SqliteConnection)
                ((SqliteConnection)conn.Connection).Close();
        }

        #endregion

        #region Query

        protected void PrepReader(ref SqliteCommand cmd)
        {
            int retries = 0;
            restart:
            try
            {
                SqliteConnection connection = new SqliteConnection(_connectionString);
                connection.Open();
                cmd.Connection = connection;
            }

            catch (SqliteBusyException ex)
            {
                if (retries++ > 5)
                    MainConsole.Instance.Warn("[SqliteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " +
                               ex);
                else
                    goto restart;
            }
            catch (SqliteException ex)
            {
                MainConsole.Instance.Warn("[SqliteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " +
                           ex);
                //throw ex;
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[SqliteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " +
                           ex);
                throw ex;
            }
        }

        protected SqliteCommand PrepReader(string query)
        {
            try
            {
                SqliteConnection connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = query;
                return cmd as SqliteCommand;
            }
            catch (SqliteException)
            {
                //throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        protected int ExecuteNonQuery(SqliteCommand cmd)
        {
            int retries = 0;
            restart:
            try
            {
                lock (GetLock())
                {
                    PrepReader(ref cmd);
                    UnescapeSql(cmd);
                    var value = cmd.ExecuteNonQuery();
                    cmd.Connection.Close();
                    return value;
                }
            }
            catch (SqliteBusyException ex)
            {
                if (retries++ > 5)
                    MainConsole.Instance.Warn("[SqliteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " +
                               ex);
                else
                    goto restart;
                return 0;
            }
            catch (SqliteException ex)
            {
                MainConsole.Instance.Warn("[SqliteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " +
                           ex);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[SqliteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " +
                           ex);
                throw ex;
            }
            return 0;
        }

        private static void UnescapeSql(SqliteCommand cmd)
        {
            foreach (SqliteParameter v in cmd.Parameters)
            {
                if (v.Value == null)
                {
                    v.Value = "";
                }
                if (v.Value.ToString().Contains("\\'"))
                {
                    v.Value = v.Value.ToString().Replace("\\'", "\'");
                }
                if (v.Value.ToString().Contains("\\\""))
                {
                    v.Value = v.Value.ToString().Replace("\\\"", "\"");
                }
            }
        }

        protected void CloseReaderCommand(SqliteCommand cmd)
        {
            cmd.Connection.Close();
            cmd.Parameters.Clear();
            //cmd.Dispose ();
        }

        private void AddParams(ref SqliteCommand cmd, Dictionary<string, object> ps)
        {
            foreach (KeyValuePair<string, object> p in ps)
                AddParam(ref cmd, p.Key, p.Value);
        }

        private void AddParam(ref SqliteCommand cmd, string key, object value)
        {
            AddParam(ref cmd, key, value, false);
        }
        private void AddParam(ref SqliteCommand cmd, string key, object value, bool convertByteString)
        {
            if (value is UUID)
                cmd.Parameters.Add(key, value.ToString());
            else if (value is Vector3)
                cmd.Parameters.Add(key, value.ToString());
            else if (value is Quaternion)
                cmd.Parameters.Add(key, value.ToString());
            else if (value is byte[] && convertByteString)
                cmd.Parameters.Add(key, Utils.BytesToString((byte[])value));
            else
                cmd.Parameters.Add(key, value);
        }

        public override List<string> QueryFullData(string whereClause, string table, string wantedValue)
        {
            string query = String.Format("select {0} from {1} {2} ", wantedValue, table, whereClause);
            return QueryFullData2(query);
        }

        public override List<string> QueryFullData(string whereClause, QueryTables tables, string wantedValue)
        {
            string query = string.Format("SELECT {0} FROM {1} {2} ", wantedValue, tables.ToSQL(), whereClause);
            return QueryFullData2(query);
        }

        private List<string> QueryFullData2(string query)
        {
            var cmd = PrepReader(query);
            lock (GetLock())
            {
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    var RetVal = new List<string>();
                    while (reader.Read())
                    {
                        if (reader.HasRows)
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                RetVal.Add(reader.GetValue(i).ToString());
                            }
                        }
                    }
                    //reader.Close();
                    CloseReaderCommand(cmd);

                    return RetVal;
                }
            }
        }

        public override DataReaderConnection QueryData(string whereClause, string table, string wantedValue)
        {
            string query = String.Format("select {0} from {1} {2}",wantedValue, table, whereClause);
            SqliteConnection conn;
            var data = QueryData2(query, out conn);
            return new DataReaderConnection { DataReader = data, Connection = conn };
        }

        public override DataReaderConnection QueryData(string whereClause, QueryTables tables, string wantedValue)
        {
            string query = string.Format("SELECT {0} FROM {1} {2}", wantedValue, tables, whereClause);
            SqliteConnection conn;
            var data = QueryData2(query, out conn);
            return new DataReaderConnection { DataReader = data, Connection = conn };
        }

        private IDataReader QueryData2(string query, out SqliteConnection conn)
        {
            var cmd = PrepReader(query);
            conn = cmd.Connection;
            return cmd.ExecuteReader();
        }



        public override List<string> Query(string[] wantedValue, string table, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count)
        {
            string query = string.Format("SELECT {0} FROM {1}", string.Join(", ", wantedValue), table);
            return Query2(query, queryFilter, sort, start, count);
        }

        public override List<string> Query(string[] wantedValue, QueryTables tables, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count)
        {
            string query = string.Format("SELECT {0} FROM {1} ", string.Join(", ", wantedValue), tables.ToSQL());
            return Query2(query, queryFilter, sort, start, count);
        }

        private List<string> Query2(string query, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count)
        {
            Dictionary<string, object> ps = new Dictionary<string, object>();
            List<string> retVal = new List<string>();
            List<string> parts = new List<string>();

            if (queryFilter != null && queryFilter.Count > 0)
            {
                query += " WHERE " + queryFilter.ToSQL(':', out ps);
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

            if (start.HasValue)
            {
                query += " LIMIT " + start.Value.ToString();
                if (count.HasValue)
                {
                    query += ", " + count.Value.ToString();
                }
            }

            int i = 0;

            var cmd = PrepReader(query);
            AddParams(ref cmd, ps);
            lock (GetLock())
            {
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    var RetVal = new List<string>();
                    while (reader.Read())
                    {
                        if (reader.HasRows)
                        {
                            for (i = 0; i < reader.FieldCount; i++)
                                RetVal.Add(reader[i] == null ? null : reader[i].ToString());
                        }
                    }
                    //reader.Close();
                    CloseReaderCommand(cmd);

                    return RetVal;
                }
            }
        }

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
            Dictionary<string, object> ps = new Dictionary<string, object>();
            int i = 0;
            foreach (object value in keyValue)
            {
                ps[":" + keyRow[i].Replace("`", "")] = value;
                query += String.Format("{0} = :{1} and ", keyRow[i], keyRow[i].Replace("`", ""));
                i++;
            }
            query = query.Remove(query.Length - 5);
            var cmd = PrepReader(query);
            AddParams(ref cmd, ps);
            lock (GetLock())
            {
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    var RetVal = new Dictionary<string, List<string>>();
                    while (reader.Read())
                    {
                        if (reader.HasRows)
                        {
                            for (i = 0; i < reader.FieldCount; i++)
                            {
                                Type r = reader[i].GetType();
                                if (r == typeof(DBNull))
                                    AddValueToList(ref RetVal, reader.GetName(i), null);
                                else
                                    AddValueToList(ref RetVal, reader.GetName(i), reader[i].ToString());
                            }
                        }
                    }
                    //reader.Close();
                    CloseReaderCommand(cmd);

                    return RetVal;
                }
            }
        }

        private void AddValueToList(ref Dictionary<string, List<string>> dic, string key, string value)
        {
            if (!dic.ContainsKey(key))
                dic.Add(key, new List<string>());

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
                filter = " WHERE " + queryFilter.ToSQL(':', out ps);
            }

            List<string> parts = new List<string>();
            if (values != null)
            {
                foreach (KeyValuePair<string, object> value in values)
                {
                    string key = ":updateSet_" + value.Key.Replace("`", "");
                    ps[key] = value.Value;
                    parts.Add(string.Format("{0} = {1}", value.Key, key));
                }
            }
            if (incrementValues != null)
            {
                foreach (KeyValuePair<string, int> value in incrementValues)
                {
                    string key = ":updateSet_increment_" + value.Key.Replace("`", "");
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

            SqliteCommand cmd = new SqliteCommand(query);
            AddParams(ref cmd, ps);

            try
            {
                ExecuteNonQuery(cmd);
            }
            catch (SqliteException e)
            {
                MainConsole.Instance.Error("[SqliteLoader] Update(" + query + "), " + e);
            }
            CloseReaderCommand(cmd);
            return true;
        }

        #endregion

        #region Insert

        public override bool InsertMultiple(string table, List<object[]> values)
        {
            var cmd = new SqliteCommand();

            string query = String.Format("insert into {0} select ", table);
            int a = 0;
            foreach (object[] value in values)
            {
                foreach (object v in value)
                {
                    query += ":" + Util.ConvertDecString(a) + ",";
                    AddParam(ref cmd, Util.ConvertDecString(a++), v, true);
                }
                query = query.Remove(query.Length - 1);
                query += " union all select ";
            }
            query = query.Remove(query.Length - (" union all select ").Length);
            
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override bool Insert(string table, object[] values)
        {
            var cmd = new SqliteCommand();

            string query = "";
            query = String.Format("insert into {0} values(", table);
            int a = 0;
            foreach (object value in values)
            {
                query += ":" + Util.ConvertDecString(a) + ",";
                AddParam(ref cmd, Util.ConvertDecString(a++), value, true);
            }
            query = query.Remove(query.Length - 1);
            query += ")";
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        private bool InsertOrReplace(string table, Dictionary<string, object> row, bool insert)
        {
            SqliteCommand cmd = new SqliteCommand();
            string query = (insert ? "INSERT" : "REPLACE") + " INTO " + table + " (" + string.Join(", ", row.Keys.ToArray<string>()) + ")";
            List<string> ps = new List<string>();
            foreach (KeyValuePair<string, object> field in row)
            {
                string key = ":" + field.Key.Replace("`", "");
                ps.Add(key);
                AddParam(ref cmd, key, field.Value);
            }
            query += " VALUES( " + string.Join(", ", ps.ToArray<string>()) + " )";

            cmd.CommandText = query;
            try
            {
                ExecuteNonQuery(cmd);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[SqliteLoader] " + (insert ? "Insert" : "Replace") + "(" + query + "), " + e);
            }
            CloseReaderCommand(cmd);
            return true;
        }
        
        public override bool Insert(string table, Dictionary<string, object> row)
        {
            return InsertOrReplace(table, row, true);
        }

        public override bool Insert(string table, object[] values, string updateKey, object updateValue)
        {
            var cmd = new SqliteCommand();
            Dictionary<string, object> ps = new Dictionary<string, object>();

            string query = "";
            query = String.Format("insert into {0} values (", table);
            int i = 0;
            foreach (object value in values)
            {
                ps[":" + Util.ConvertDecString(i)] = value;
                query = String.Format(query + ":{0},", Util.ConvertDecString(i++));
            }
            query = query.Remove(query.Length - 1);
            query += ")";
            cmd.CommandText = query;
            AddParams(ref cmd, ps);
            try
            {
                ExecuteNonQuery(cmd);
                CloseReaderCommand(cmd);
            }
                //Execute the update then...
            catch (Exception)
            {
                cmd = new SqliteCommand();
                query = String.Format("UPDATE {0} SET {1} = '{2}'", table, updateKey, updateValue);
                cmd.CommandText = query;
                ExecuteNonQuery(cmd);
                CloseReaderCommand(cmd);
            }
            return true;
        }

        public override bool InsertSelect(string tableA, string[] fieldsA, string tableB, string[] valuesB)
        {
            SqliteCommand cmd = PrepReader(string.Format("INSERT INTO {0}{1} SELECT {2} FROM {3}",
                tableA,
                (fieldsA.Length > 0 ? " (" + string.Join(", ", fieldsA) + ")" : ""),
                string.Join(", ", valuesB),
                tableB
            ));

            try
            {
                ExecuteNonQuery(cmd);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[SqliteLoader] INSERT .. SELECT (" + cmd.CommandText + "), " + e);
            }
            CloseReaderCommand(cmd);
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
            filter.andLessThanEqFilters["(datetime(" + key.Replace("`", "") + ", 'localtime') - datetime('now', 'localtime'))"] = 0;

            return Delete(table, filter);
        }

        public override bool Delete(string table, QueryFilter queryFilter)
        {
            Dictionary<string, object> ps = new Dictionary<string, object>();
            string query = "DELETE FROM " + table + (queryFilter != null ? (" WHERE " + queryFilter.ToSQL(':', out ps)) : "");

            SqliteCommand cmd = new SqliteCommand(query);
            AddParams(ref cmd, ps);
            try
            {
                ExecuteNonQuery(cmd);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[SqliteDataManager] Delete(" + query + "), " + e);
                return false;
            }
            CloseReaderCommand(cmd);
            return true;
        }

        #endregion

        public override string ConCat(string[] toConcat)
        {
#if (!ISWIN)
            string returnValue = "";
            foreach (string s in toConcat)
                returnValue = returnValue + (s + " || ");
#else
            string returnValue = toConcat.Aggregate("", (current, s) => current + (s + " || "));
#endif
            return returnValue.Substring(0, returnValue.Length - 4);
        }

        #region Tables

        public override bool TableExists(string tableName)
        {
            var cmd = PrepReader("SELECT name FROM Sqlite_master WHERE name='" + tableName + "'");
            lock (GetLock())
            {
                using (IDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        CloseReaderCommand(cmd);
                        return true;
                    }
                    else
                    {
                        CloseReaderCommand(cmd);
                        return false;
                    }
                }
            }
        }

        public override void CreateTable(string table, ColumnDefinition[] columns, IndexDefinition[] indices)
        {
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

            bool has_auto_increment = false;
            foreach (ColumnDefinition column in columns)
            {
                if (column.Type.auto_increment)
                {
                    has_auto_increment = true;
                }
                columnDefinition.Add(column.Name + " " + GetColumnTypeStringSymbol(column.Type));
            }
            if (!has_auto_increment && primary != null && primary.Fields.Length > 0)
            {
                columnDefinition.Add("PRIMARY KEY (" + string.Join(", ", primary.Fields) + ")");
            }

            var cmd = new SqliteCommand {
                CommandText = string.Format("create table " + table + " ({0})", string.Join(", ", columnDefinition.ToArray()))
            };
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);

            if (indices.Length >= 1 && (primary == null || indices.Length >= 2))
            {
                columnDefinition = new List<string>(primary != null ? indices.Length : indices.Length - 1); // reusing existing variable for laziness
                uint i = 0;
                foreach (IndexDefinition index in indices)
                {
                    if (index.Type == IndexType.Primary || index.Fields.Length < 1)
                    {
                        continue;
                    }

                    i++;
                    columnDefinition.Add("CREATE " + (index.Type == IndexType.Unique ? "UNIQUE " : string.Empty) + "INDEX idx_" + table + "_" + i.ToString() + " ON " + table + "(" + string.Join(", ", index.Fields) + ")");
                }
                foreach (string query in columnDefinition)
                {
                    cmd = new SqliteCommand
                    {
                        CommandText = query
                    };
                    ExecuteNonQuery(cmd);
                    CloseReaderCommand(cmd);
                }
            }
        }

        public override void UpdateTable(string table, ColumnDefinition[] columns, IndexDefinition[] indices, Dictionary<string, string> renameColumns)
        {
            if (!TableExists(table))
            {
                throw new DataManagerException("Trying to update a table with name of one that does not exist.");
            }

            List<ColumnDefinition> oldColumns = ExtractColumnsFromTable(table);

            Dictionary<string, ColumnDefinition> sameColumns = new Dictionary<string, ColumnDefinition>();
            foreach (ColumnDefinition column in oldColumns)
            {
#if (!ISWIN)
                foreach (ColumnDefinition innercolumn in columns)
                {
                    if (innercolumn.Name.ToLower() == column.Name.ToLower() || renameColumns.ContainsKey(column.Name) && renameColumns[column.Name].ToLower() == innercolumn.Name.ToLower())
                    {
                        sameColumns.Add(column.Name, column);
                        break;
                    }
                }
#else
                if (columns.Any(innercolumn => innercolumn.Name.ToLower() == column.Name.ToLower() ||
                                               renameColumns.ContainsKey(column.Name) &&
                                               renameColumns[column.Name].ToLower() == innercolumn.Name.ToLower()))
                {
                    sameColumns.Add(column.Name, column);
                }
#endif
            }

            string renamedTempTableColumnDefinition = string.Empty;
            string renamedTempTableColumn = string.Empty;

            foreach (ColumnDefinition column in oldColumns)
            {
                if (renamedTempTableColumnDefinition != string.Empty)
                {
                    renamedTempTableColumnDefinition += ", ";
                    renamedTempTableColumn += ", ";
                }
                renamedTempTableColumn += column.Name;
                renamedTempTableColumnDefinition += column.Name + " " + GetColumnTypeStringSymbol(column.Type);
            }

            var cmd = new SqliteCommand {
                CommandText = "CREATE TABLE " + table + "__temp(" + renamedTempTableColumnDefinition + ");"
            };
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);

            cmd = new SqliteCommand {
                CommandText = "INSERT INTO " + table + "__temp SELECT " + renamedTempTableColumn + " from " + table + ";"
            };
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);

            cmd = new SqliteCommand {
                CommandText = "drop table " + table
            };
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);

            List<string> newTableColumnDefinition = new List<string>(columns.Length);

            IndexDefinition primary = null;
            foreach (IndexDefinition index in indices)
            {
                if (index.Type == IndexType.Primary)
                {
                    primary = index;
                    break;
                }
            }

            bool has_auto_increment = false;
            foreach (ColumnDefinition column in columns)
            {
                if (column.Type.auto_increment)
                {
                    has_auto_increment = true;
                }
                newTableColumnDefinition.Add(column.Name + " " + GetColumnTypeStringSymbol(column.Type));
            }
            if (!has_auto_increment && primary != null && primary.Fields.Length > 0){
                newTableColumnDefinition.Add("PRIMARY KEY (" + string.Join(", ", primary.Fields) + ")");
            }

            cmd = new SqliteCommand {
                CommandText = string.Format("create table " + table + " ({0}) ", string.Join(", ", newTableColumnDefinition.ToArray()))
            };
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);

            if (indices.Length >= 1 && (primary == null || indices.Length >= 2))
            {
                newTableColumnDefinition = new List<string>(primary != null ? indices.Length : indices.Length - 1); // reusing existing variable for laziness
                uint i = 0;
                foreach (IndexDefinition index in indices)
                {
                    if (index.Type == IndexType.Primary || index.Fields.Length < 1)
                    {
                        continue;
                    }

                    i++;
                    newTableColumnDefinition.Add("CREATE " + (index.Type == IndexType.Unique ? "UNIQUE " : string.Empty) + "INDEX idx_" + table + "_" + i.ToString() + " ON " + table + "(" + string.Join(", ", index.Fields) + ")");
                }
                foreach (string query in newTableColumnDefinition)
                {
                    cmd = new SqliteCommand
                    {
                        CommandText = query
                    };
                    ExecuteNonQuery(cmd);
                    CloseReaderCommand(cmd);
                }
            }



            string InsertFromTempTableColumnDefinition = string.Empty;
            string InsertIntoFromTempTableColumnDefinition = string.Empty;

            foreach (ColumnDefinition column in sameColumns.Values)
            {
                if (InsertFromTempTableColumnDefinition != string.Empty)
                {
                    InsertFromTempTableColumnDefinition += ", ";
                }
                if (InsertIntoFromTempTableColumnDefinition != string.Empty)
                {
                    InsertIntoFromTempTableColumnDefinition += ", ";
                }
                if (renameColumns.ContainsKey(column.Name))
                    InsertIntoFromTempTableColumnDefinition += renameColumns[column.Name];
                else
                    InsertIntoFromTempTableColumnDefinition += column.Name;
                InsertFromTempTableColumnDefinition += column.Name;
            }

            cmd = new SqliteCommand {
                CommandText = "INSERT INTO " + table + " (" + InsertIntoFromTempTableColumnDefinition + ") SELECT " + InsertFromTempTableColumnDefinition + " from " + table + "__temp;"
            };
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);

            cmd = new SqliteCommand {
                CommandText = "drop table " + table + "__temp"
            };
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        public override string GetColumnTypeStringSymbol(ColumnTypes type)
        {
            switch (type)
            {
                case ColumnTypes.Double:
                    return "DOUBLE";
                case ColumnTypes.Integer11:
                    return "INT(11)";
                case ColumnTypes.Integer30:
                    return "INT(30)";
                case ColumnTypes.UInteger11:
                    return "INT(11) UNSIGNED";
                case ColumnTypes.UInteger30:
                    return "INT(30) UNSIGNED";
                case ColumnTypes.Char36:
                    return "CHAR(36)";
                case ColumnTypes.Char32:
                    return "CHAR(32)";
                case ColumnTypes.Char5:
                    return "CHAR(5)";
                case ColumnTypes.String:
                    return "TEXT";
                case ColumnTypes.String1:
                    return "VARCHAR(1)";
                case ColumnTypes.String2:
                    return "VARCHAR(2)";
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
                case ColumnTypes.String10:
                    return "VARCHAR(10)";
                case ColumnTypes.String255:
                    return "VARCHAR(255)";
                case ColumnTypes.String512:
                    return "VARCHAR(512)";
                case ColumnTypes.String1024:
                    return "VARCHAR(1024)";
                case ColumnTypes.String8196:
                    return "VARCHAR(8196)";
                case ColumnTypes.Blob:
                    return "blob";
                case ColumnTypes.LongBlob:
                    return "blob";
                case ColumnTypes.Text:
                    return "VARCHAR(512)";
                case ColumnTypes.MediumText:
                    return "VARCHAR(512)";
                case ColumnTypes.LongText:
                    return "VARCHAR(512)";
                case ColumnTypes.Date:
                    return "DATE";
                case ColumnTypes.DateTime:
                    return "DATETIME";
                case ColumnTypes.Float:
                    return "float";
                case ColumnTypes.Unknown:
                    return "";
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
                case ColumnType.LongBlob:
                    symbol = "BLOB";
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
                    if (!coldef.auto_increment)
                    {
                        symbol = "INT(" + coldef.Size + ")";
                    }
                    else
                    {
                        symbol = "INTEGER PRIMARY KEY AUTOINCREMENT";
                    }
                    break;
                case ColumnType.TinyInt:
                    symbol = "TINYINT(" + coldef.Size + ")";
                    break;
                case ColumnType.String:
                    symbol = "VARCHAR(" + coldef.Size + ")";
                    break;
                case ColumnType.Text:
                case ColumnType.MediumText:
                case ColumnType.LongText:
                    symbol = "TEXT";
                    break;
                case ColumnType.UUID:
                    symbol = "CHAR(36)";
                    break;
                default:
                    throw new DataManagerException("Unknown column type.");
            }
            return symbol + (coldef.isNull ? " NULL" : " NOT NULL") +
                ((coldef.isNull && coldef.defaultValue == null) ? " DEFAULT NULL" : 
                (coldef.defaultValue != null ? " DEFAULT " + (coldef.defaultValue.StartsWith("'") && coldef.defaultValue.EndsWith("'") ? coldef.defaultValue : "'" + coldef.defaultValue + "'") : ""));
        }

        protected override List<ColumnDefinition> ExtractColumnsFromTable(string tableName)
        {
            List<ColumnDefinition> defs = new List<ColumnDefinition>();
            IndexDefinition primary = null;
            bool isFaux = false;
            foreach (KeyValuePair<string, IndexDefinition> index in ExtractIndicesFromTable(tableName))
            {
                if (index.Value.Type == IndexType.Primary)
                {
                    isFaux = index.Key == "#fauxprimary#";
                    primary = index.Value;
                    break;
                }
            }

            var cmd = PrepReader(string.Format("PRAGMA table_info({0})", tableName));
            lock (GetLock())
            {
                using (SqliteDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        if (rdr.HasRows)
                        {

                            var name = rdr["name"];
                            var type = rdr["type"];
                            object defaultValue = rdr["dflt_value"];

                            ColumnTypeDef typeDef = ConvertTypeToColumnType(type.ToString());
                            typeDef.isNull = uint.Parse(rdr["notnull"].ToString()) == 0;
                            typeDef.defaultValue = defaultValue == null || defaultValue.GetType() == typeof(System.DBNull) ? null : defaultValue.ToString();

                            if (
                                uint.Parse(rdr["pk"].ToString()) == 1 &&
                                primary != null &&
                                isFaux == true &&
                                primary.Fields.Length == 1 &&
                                primary.Fields[0].ToLower() == name.ToString().ToLower() &&
                                (typeDef.Type == ColumnType.Integer || typeDef.Type == ColumnType.TinyInt)
                            )
                            {
                                typeDef.auto_increment = true;
                            }

                            defs.Add(new ColumnDefinition
                            {
                                Name = name.ToString(),
                                Type = typeDef,
                            });
                        }
                    }
                    rdr.Close();
                }
            }
            CloseReaderCommand(cmd);

            return defs;
        }

        protected override Dictionary<string, IndexDefinition> ExtractIndicesFromTable(string tableName)
        {
            Dictionary<string, IndexDefinition> defs = new Dictionary<string, IndexDefinition>();
            IndexDefinition primary = new IndexDefinition
            {
                Fields = new string[]{},
                Type = IndexType.Primary
            };

            string autoIncrementField = null;

            List<string> fields = new List<string>();

            SqliteCommand cmd = PrepReader(string.Format("PRAGMA table_info({0})", tableName));
            lock (GetLock())
            {
                using (SqliteDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        if (rdr.HasRows)
                        {

                            if (uint.Parse(rdr["pk"].ToString()) > 0)
                            {
                                fields.Add(rdr["name"].ToString());
                                if (autoIncrementField == null)
                                {
                                    ColumnTypeDef typeDef = ConvertTypeToColumnType(rdr["type"].ToString());
                                    if (typeDef.Type == ColumnType.Integer || typeDef.Type == ColumnType.TinyInt)
                                    {
                                        autoIncrementField = rdr["name"].ToString();
                                    }
                                }
                            }
                        }
                    }
                    rdr.Close();
                }
            }
            CloseReaderCommand(cmd);
            primary.Fields = fields.ToArray();

            cmd = PrepReader(string.Format("PRAGMA index_list({0})", tableName));
            Dictionary<string, bool> indices = new Dictionary<string, bool>();
            lock (GetLock())
            {
                using (SqliteDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        if (rdr.HasRows)
                            indices[rdr["name"].ToString()] = (uint.Parse(rdr["unique"].ToString()) > 0);
                    }
                    rdr.Close();
                }
            }
            CloseReaderCommand(cmd);

            bool checkForPrimary = primary.Fields.Length > 0;
            foreach (KeyValuePair<string, bool> index in indices)
            {
                defs[index.Key] = new IndexDefinition
                {
                    Type = index.Value ? IndexType.Unique : IndexType.Index
                };
                fields = new List<string>();
                cmd = PrepReader(string.Format("PRAGMA index_info({0})", index.Key));
                lock (GetLock())
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (rdr.HasRows)
                                fields.Add(rdr["name"].ToString());
                        }
                        rdr.Close();
                    }
                }
                defs[index.Key].Fields = fields.ToArray();
                CloseReaderCommand(cmd);
                if (checkForPrimary && defs[index.Key].Fields.Length == primary.Fields.Length)
                {
                    uint i = 0;
                    bool isPrimary = true;
                    foreach (string pkField in primary.Fields)
                    {
                        if (defs[index.Key].Fields[i++] != pkField)
                        {
                            isPrimary = false;
                            break;
                        }
                    }
                    if (isPrimary)
                    {
//                        MainConsole.Instance.Warn("[" + Identifier + "]: Primary Key found (" + string.Join(", ", defs[index.Key].Fields) + ")");
                        defs[index.Key].Type = IndexType.Primary;
                        checkForPrimary = false;
                    }
                }
            }

            if (checkForPrimary == true && autoIncrementField != null)
            {
                primary = new IndexDefinition
                {
                    Fields = new string[1] { autoIncrementField },
                    Type = IndexType.Primary
                };
                defs["#fauxprimary#"] = primary;
            }

            return defs;
        }

        public override void DropTable(string tableName)
        {
            var cmd = new SqliteCommand {CommandText = string.Format("drop table {0}", tableName)};
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        public override void ForceRenameTable(string oldTableName, string newTableName)
        {
            var cmd = new SqliteCommand
                          {
                              CommandText =
                                  string.Format("ALTER TABLE {0} RENAME TO {1}", oldTableName,
                                                newTableName + "_renametemp")
                          };
            ExecuteNonQuery(cmd);
            cmd.CommandText = string.Format("ALTER TABLE {0} RENAME TO {1}", newTableName + "_renametemp", newTableName);
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        protected override void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions)
        {
            var cmd = new SqliteCommand
                          {
                              CommandText =
                                  string.Format("insert into {0} select * from {1}", destinationTableName, sourceTableName)
                          };
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        #endregion

        public override IGenericData Copy()
        {
            return new SQLiteLoader();
        }
    }
}