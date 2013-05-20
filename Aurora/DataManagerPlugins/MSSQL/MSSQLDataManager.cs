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
using System.Data.SqlClient;
using System.Linq;
using Aurora.DataManager.Migration;
using Aurora.Framework;
using Aurora.Framework.Utilities;
using Aurora.Framework.Services;
using Aurora.Framework.ConsoleFramework;

namespace Aurora.DataManager.MSSQL
{
    public class MSSQLDataLoader : DataManagerBase
    {
        private string connectionString = "";

        public override string Identifier
        {
            get { return "MSSQLData"; }
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        public IDbCommand Query(string sql, Dictionary<string, object> parameters, SqlConnection dbcon)
        {
            SqlCommand dbcommand;
            try
            {
                dbcommand = dbcon.CreateCommand();
                dbcommand.CommandText = sql;
                foreach (KeyValuePair<string, object> param in parameters)
                {
                    dbcommand.Parameters.AddWithValue(param.Key, param.Value);
                }
                return dbcommand;
            }
            catch (Exception)
            {
                // Return null if it fails.
                return null;
            }
        }

        public override void ConnectToDatabase(string connectionstring)
        {
            connectionString = connectionstring;
        }

        public override void CloseDatabase(DataReaderConnection connection)
        {
            connection.DataReader.Dispose();
            (connection.Connection as SqlConnection).Dispose();
        }

        public bool ExecuteCommand(string query)
        {
            return ExecuteCommand(query, new Dictionary<string, object>());
        }

        public bool ExecuteCommand(string query, Dictionary<string, object> ps)
        {
            IDbCommand result;

            using(SqlConnection connection = GetConnection())
            {
                using (result = Query(query, ps, connection))
                {
                    return result.ExecuteNonQuery() > 0;
                }
            }
        }

        public override DataReaderConnection QueryData(string whereClause, string table, string wantedValue)
        {
            string query = String.Format("select {0} from {1} {2}",
                                         wantedValue, table, whereClause);

            SqlConnection conn = GetConnection();
            return new DataReaderConnection
            {
                Connection = conn,
                DataReader =
                    Query(query, new Dictionary<string, object>(),
                    conn).ExecuteReader()
            };
        }
        
        private static string QueryFilter2Query(QueryFilter filter)
        {
            /*string query = "";
            List<string> parts;
            bool had = false;
            if (filter.Count > 0)
            {
                query += "(";

                #region equality

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in filter.andFilters)
                {
                    parts.Add(string.Format("{0} = '{1}'", where.Key, where.Value));
                }
                if (parts.Count > 0)
                {
                    query += " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in filter.orFilters)
                {
                    parts.Add(string.Format("{0} = '{1}'", where.Key, where.Value));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, List<object>> where in filter.orMultiFilters)
                {
                    foreach (object value in where.Value)
                    {
                        parts.Add(string.Format("{0} = '{1}'", where.Key, value));
                    }
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region LIKE

                parts = new List<string>();
                foreach (KeyValuePair<string, string> where in filter.andLikeFilters)
                {
                    parts.Add(string.Format("{0} LIKE '{1}'", where.Key, where.Value));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, string> where in filter.orLikeFilters)
                {
                    parts.Add(string.Format("{0} LIKE '{1}'", where.Key, where.Value));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, List<string>> where in filter.orLikeMultiFilters)
                {
                    foreach (string value in where.Value)
                    {
                        parts.Add(string.Format("{0} LIKE '{1}'", where.Key, value));
                    }
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region bitfield &

                parts = new List<string>();
                foreach (KeyValuePair<string, uint> where in filter.andBitfieldAndFilters)
                {
                    parts.Add(string.Format("{0} & {1}", where.Key, where.Value));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, uint> where in filter.orBitfieldAndFilters)
                {
                    parts.Add(string.Format("{0} & {1}", where.Key, where.Value));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region greater than

                parts = new List<string>();
                foreach (KeyValuePair<string, int> where in filter.andGreaterThanFilters)
                {
                    parts.Add(string.Format("{0} > {1}", where.Key, where.Value));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, int> where in filter.orGreaterThanFilters)
                {
                    parts.Add(string.Format("{0} > {1}", where.Key, where.Value));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, int> where in filter.andGreaterThanEqFilters)
                {
                    parts.Add(string.Format("{0} >= {1}", where.Key, where.Value));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region less than

                parts = new List<string>();
                foreach (KeyValuePair<string, int> where in filter.andLessThanFilters)
                {
                    parts.Add(string.Format("{0} > {1}", where.Key, where.Value));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, int> where in filter.orLessThanFilters)
                {
                    parts.Add(string.Format("{0} > {1}", where.Key, where.Value));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, int> where in filter.andLessThanEqFilters)
                {
                    parts.Add(string.Format("{0} <= {1}", where.Key, where.Value));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                foreach (QueryFilter subFilter in filter.subFilters)
                {
                    query += (had ? " AND" : string.Empty) + QueryFilter2Query(subFilter);
                    if (subFilter.Count > 0)
                    {
                        had = true;
                    }
                }
                query += ")";
            }*/
            string query = "";
            return query;
        }

        public override List<string> Query(string[] wantedValue, QueryTables tables, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count)
        {
            throw new NotImplementedException();
        }

        public override List<string> Query(string[] wantedValue, string table, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count)
        {
            /*string query = string.Format("SELECT {0} FROM {1}", string.Join(", ", wantedValue), table);
            Dictionary<string, object> ps = new Dictionary<string, object>();
            List<string> retVal = new List<string>();
            List<string> parts = new List<string>();
            IDbCommand result;
            IDataReader reader;
            SqlConnection dbcon = GetLockedConnection();

            if (queryFilter != null && queryFilter.Count > 0)
            {
                query += " WHERE " + QueryFilter2Query(queryFilter);
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

            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
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
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Cancel();
                        result.Dispose();
                        CloseDatabase(dbcon);
                    }
                }
            }*/
            throw new NotImplementedException("query");
        }

        private void AddValueToList(ref Dictionary<string, List<string>> dic, string key, string value)
        {
            if (!dic.ContainsKey(key))
                dic.Add(key, new List<string>());

            dic[key].Add(value);
        }

        public override bool Update(string table, Dictionary<string, object> values, Dictionary<string, int> incrementValues, QueryFilter queryFilter, uint? start, uint? count)
        {
            if ((values == null || values.Count < 1) && (incrementValues == null || incrementValues.Count < 1))
            {
                MainConsole.Instance.Warn("Update attempted with no values");
                return false;
            }

            string query = string.Format("UPDATE {0}", table);
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
            return ExecuteCommand(query);
        }

        public override bool InsertMultiple(string table, List<object[]> values)
        {
            string query = String.Format("INSERT INTO {0} VALUES", table);
            foreach(object[] vals in values)
            {
                query += " (";
                query = vals.Aggregate(query, (current, value) => current + "'" + value + "',");
                query = query.Remove(query.Length - 1);
                query += "),";
            }
            query = query.Remove(query.Length - 1);
            return ExecuteCommand(query);
        }

        public override bool InsertSelect(string tableA, string[] fieldsA, string tableB, string[] valuesB)
        {
            throw new NotImplementedException();
        }

        public override bool Insert(string table, object[] values)
        {
            string query = String.Format("INSERT INTO {0} VALUES (", table);
            query = values.Aggregate(query, (current, value) => current + "'" + value + "',");
            query = query.Remove(query.Length - 1);
            query += ")";
            return ExecuteCommand(query);
        }

        public override bool Insert(string table, Dictionary<string, object> row)
        {
            string query = "INSERT INTO " + table + " (" +
                           string.Join(", ", row.Keys.ToArray<string>()) + ")";
            Dictionary<string, object> ps = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> field in row)
            {
                string key = "?" +
                             field.Key.Replace("`", "")
                                  .Replace("(", "_")
                                  .Replace(")", "")
                                  .Replace(" ", "_")
                                  .Replace("-", "minus")
                                  .Replace("+", "add")
                                  .Replace("/", "divide")
                                  .Replace("*", "multiply");
                ps[key] = field.Value;
            }
            query += " VALUES( " + string.Join(", ", ps.Keys.ToArray<string>()) + " )";

            return ExecuteCommand(query);
        }

        public override bool Replace(string table, Dictionary<string, object> row)
        {
            throw new NotImplementedException("replace");
        }

        public override bool Delete(string table, QueryFilter queryFilter)
        {
            Dictionary<string, object> ps = new Dictionary<string, object>();
            string query = "DELETE FROM " + table +
                           (queryFilter != null ? (" WHERE " + queryFilter.ToSQL('?', out ps)) : "");

            return ExecuteCommand(query);
        }

        public override string ConCat(string[] toConcat)
        {
            string returnValue = toConcat.Aggregate("", (current, s) => current + (s + " + "));
            return returnValue.Substring(0, returnValue.Length - 3);
        }

        public override bool DeleteByTime(string table, string key)
        {
            QueryFilter filter = new QueryFilter();
            filter.andLessThanEqFilters["(UNIX_TIMESTAMP(`" + key.Replace("`", "") + "`) - UNIX_TIMESTAMP())"] = 0;

            return Delete(table, filter);
        }

        public override void CreateTable(string table, ColumnDefinition[] columns, IndexDefinition[] indices)
        {
            if (TableExists(table))
            {
                throw new DataManagerException("Trying to create a table with name of one that already exists.");
            }

            string columnDefinition = string.Empty;

            foreach (ColumnDefinition column in columns)
            {
                if (columnDefinition != string.Empty)
                {
                    columnDefinition += ", ";
                }
                columnDefinition += column.Name + " " + GetColumnTypeStringSymbol(column.Type);
            }

            string query = string.Format("create table " + table + " ( {0} {1}) ", columnDefinition, string.Empty);
            ExecuteCommand(query);
        }

        public override void UpdateTable(string table, ColumnDefinition[] columns, IndexDefinition[] indices, Dictionary<string, string> renameColumns)
        {
            if (TableExists(table))
            {
                throw new DataManagerException("Trying to create a table with name of one that already exists.");
            }

            string columnDefinition = string.Empty;

            foreach (ColumnDefinition column in columns)
            {
                if (columnDefinition != string.Empty)
                {
                    columnDefinition += ", ";
                }
                columnDefinition += column.Name + " " + GetColumnTypeStringSymbol(column.Type);
            }

            string query = string.Format("create table " + table + " ( {0} {1}) ", columnDefinition, string.Empty);
            ExecuteCommand(query);
        }

        public override void DropTable(string tableName)
        {
            ExecuteCommand(string.Format("drop table {0}", tableName));
        }

        public override void ForceRenameTable(string oldTableName, string newTableName)
        {
            ExecuteCommand(string.Format("RENAME TABLE {0} TO {1}", oldTableName, newTableName));
        }

        protected override void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions)
        {
            ExecuteCommand(string.Format("INSERT INTO {0} SELECT * FROM {1}", destinationTableName, sourceTableName));
        }

        public override bool TableExists(string table)
        {
            bool ret = false;
            using (SqlConnection dbcon = GetConnection())
            {
                using (SqlCommand dbcommand = dbcon.CreateCommand())
                {
                    dbcommand.CommandText =
                        string.Format(
                            "select table_name from information_schema.tables where table_schema=database() and table_name='{0}'",
                            table.ToLower());
                    using (var rdr = dbcommand.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }

        protected override List<ColumnDefinition> ExtractColumnsFromTable(string tableName)
        {
            var defs = new List<ColumnDefinition>();

            using (SqlConnection dbcon = GetConnection())
            {
                using (SqlCommand dbcommand = dbcon.CreateCommand())
                {
                    dbcommand.CommandText = string.Format("desc {0}", tableName);
                    var rdr = dbcommand.ExecuteReader();
                    while (rdr.Read())
                    {
                        var name = rdr["Field"];
                        var pk = rdr["Key"];
                        var type = rdr["Type"];
                        defs.Add(new ColumnDefinition
                                     {
                                         Name = name.ToString(),
                                         Type = ConvertTypeToColumnType(type.ToString())
                                     });
                    }
                }
            }
            return defs;
        }

        protected override Dictionary<string, IndexDefinition> ExtractIndicesFromTable(string tableName)
        {
            throw new NotImplementedException();
        }

        public override string GetColumnTypeStringSymbol(ColumnTypeDef coldef)
        {
            string symbol;
            switch (coldef.Type)
            {
                case ColumnType.Blob:
                    symbol = "IMAGE";
                    break;
                case ColumnType.LongBlob:
                    symbol = "IMAGE";
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
                    symbol = "INT(" + coldef.Size + ")" + (coldef.unsigned ? " UNSIGNED" : "");
                    break;
                case ColumnType.TinyInt:
                    symbol = "TINYINT(" + coldef.Size + ")" + (coldef.unsigned ? " UNSIGNED" : "");
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
                case ColumnType.Binary:
                    symbol = "BINARY(" + coldef.Size + ")";
                    break;
                default:
                    throw new DataManagerException("Unknown column type.");
            }

            return symbol + (coldef.isNull ? " NULL" : " NOT NULL") +
                   ((coldef.isNull && coldef.defaultValue == null)
                        ? " DEFAULT NULL"
                        : (coldef.defaultValue != null ? " DEFAULT '" + coldef.defaultValue.MySqlEscape() + "'" : "")) +
                   ((coldef.Type == ColumnType.Integer || coldef.Type == ColumnType.TinyInt) && coldef.auto_increment
                        ? " AUTO_INCREMENT"
                        : "");
        }
    }
}