using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using C5;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using OpenMetaverse;

namespace Aurora.DataManager.MSSQL
{
    public class MSSQLDataLoader : DataManagerBase
    {
        readonly Mutex m_lock = new Mutex(false);
        string connectionString = "";
        private SqlConnection m_connection = null;
        private volatile bool m_InUse = false;

        public override string Identifier
        {
            get { return "MSSQLData"; }
        }

        public SqlConnection GetLockedConnection()
        {
            if (m_connection == null)
            {
                m_connection = new SqlConnection(connectionString);
                m_connection.Open();
                return m_connection;
            }
            else
            {
                SqlConnection clone = (SqlConnection)((ICloneable)m_connection).Clone();
                clone.Open();
                return clone;
            }
        }

        public IDbCommand Query(string sql, Dictionary<string, object> parameters, SqlConnection dbcon)
        {
            SqlCommand dbcommand;
            try
            {
                dbcommand = dbcon.CreateCommand();
                dbcommand.CommandText = sql;
                foreach (System.Collections.Generic.KeyValuePair<string, object> param in parameters)
                {
                    dbcommand.Parameters.AddWithValue(param.Key, param.Value);
                }
                return (IDbCommand)dbcommand;
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
            SqlConnection dbcon = GetLockedConnection();
            dbcon.Close();
            dbcon.Dispose();
        }

        public bool ExecuteCommand(string query)
        {
            SqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;

            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    reader.Close();
                    reader.Dispose();
                    result.Cancel();
                    result.Dispose();
                }
            }
            CloseDatabase(dbcon);
            return true;
        }

        public override List<string> Query(string keyRow, object keyValue, string table, string wantedValue)
        {
            SqlConnection dbcon = GetLockedConnection();
            IDbCommand result = null;
            IDataReader reader = null;
            List<string> RetVal = new List<string>();
            string query = "";
            if (keyRow == "")
            {
                query = String.Format("select {0} from {1}",
                                      wantedValue, table);
            }
            else
            {
                query = String.Format("select {0} from {1} where {2} = '{3}'",
                                      wantedValue, table, keyRow, keyValue.ToString());
            }
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                RetVal.Add(reader.GetString(i));
                            }
                        }
                        return RetVal;
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
            }
        }

        public override IDataReader QueryReader(string keyRow, object keyValue, string table, string wantedValue)
        {
            SqlConnection dbcon = GetLockedConnection();
            IDbCommand result = null;
            IDataReader reader = null;
            List<string> RetVal = new List<string>();
            string query = "";
            if (keyRow == "")
            {
                query = String.Format("select {0} from {1}",
                                      wantedValue, table);
            }
            else
            {
                query = String.Format("select {0} from {1} where {2} = '{3}'",
                                      wantedValue, table, keyRow, keyValue.ToString());
            }
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                reader = result.ExecuteReader();
                return reader;
            }
        }

        public override List<string> Query(string whereClause, string table, string wantedValue)
        {
            SqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            List<string> RetVal = new List<string>();
            string query = String.Format("select {0} from {1} where {2}",
                                      wantedValue, table, whereClause);
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                RetVal.Add(reader.GetString(i));
                            }
                        }
                        return RetVal;
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
            }
        }

        public override List<string> Query(string keyRow, object keyValue, string table, string wantedValue, string order)
        {
            SqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            List<string> RetVal = new List<string>();
            string query = "";
            if (keyRow == "")
            {
                query = String.Format("select {0} from {1}",
                                      wantedValue, table);
            }
            else
            {
                query = String.Format("select {0} from {1} where {2} = '{3}'",
                                      wantedValue, table, keyRow, keyValue);
            }
            using (result = Query(query + order, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                RetVal.Add(reader.GetString(i));
                            }
                        }
                        return RetVal;
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
            }
        }

        public override List<string> Query(string[] keyRow, object[] keyValue, string table, string wantedValue)
        {
            SqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            List<string> RetVal = new List<string>();
            string query = String.Format("select {0} from {1} where ",
                                      wantedValue, table);
            int i = 0;
            foreach (object value in keyValue)
            {
                query += String.Format("{0} = '{1}' and ", keyRow[i], value);
                i++;
            }
            query = query.Remove(query.Length - 5);
            
            
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
                                RetVal.Add(reader.GetString(i));
                            }
                        }
                        return RetVal;
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
            }
        }

        public override bool Update(string table, object[] setValues, string[] setRows, string[] keyRows, object[] keyValues)
        {
            SqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            string query = String.Format("update {0} set ", table);
            int i = 0;
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            foreach (object value in setValues)
            {
                query += string.Format("{0} = ?{1},", setRows[i], setRows[i]);
                string valueSTR = value.ToString();
                if(valueSTR == "")
                    valueSTR = " ";
                parameters["?" + setRows[i]] = valueSTR;
                i++;
            }
            i = 0;
            query = query.Remove(query.Length - 1);
            query += " where ";
            foreach (object value in keyValues)
            {
                query += String.Format("{0}  = '{1}' and ", keyRows[i], value);
                i++;
            }
            query = query.Remove(query.Length - 5);
            using (result = Query(query, parameters, dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    reader.Close();
                    reader.Dispose();
                    result.Cancel();
                    result.Dispose();
                    CloseDatabase(dbcon);
                }
            }
            return true;
        }

        public override bool Insert(string table, object[] values)
        {
            SqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;

            string query = String.Format("insert into {0} values (", table);
            foreach (object value in values)
            {
                query += String.Format("'{0}',", value);
            }
            query = query.Remove(query.Length - 1);
            query += ")";
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                try
                {
                    using (reader = result.ExecuteReader())
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Cancel();
                        result.Dispose();
                        CloseDatabase(dbcon);
                    }
                }
                catch { }
            }
            return true;
        }

        public override bool Insert(string table, string[] keys, object[] values)
        {
            SqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;

            string query = String.Format("insert into {0} values (", table);
            foreach (object value in values)
            {
                query += String.Format("'{0}',", value);
            }
            query = query.Remove(query.Length - 1);
            query += ")";
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                try
                {
                    using (reader = result.ExecuteReader())
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Cancel();
                        result.Dispose();
                        CloseDatabase(dbcon);
                    }
                }
                catch { }
            }
            return true;
        }

        public override bool Insert(string table, object[] values, string updateKey, object updateValue)
        {
            SqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            string query = String.Format("insert into {0} VALUES('", table);
            foreach (object value in values)
            {
                query += value.ToString() + "','";
            }
            query = query.Remove(query.Length - 2);
            query += String.Format(") ON DUPLICATE KEY UPDATE {0} = '{1}'", updateKey, updateValue);
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    reader.Close();
                    reader.Dispose();
                    result.Cancel();
                    result.Dispose();
                    CloseDatabase(dbcon);
                }
            }
            return true;
        }

        public override bool Delete(string table, string[] keys, object[] values)
        {
            SqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            string query = "delete from " + table + " WHERE ";
            int i = 0;
            foreach (object value in values)
            {
                query += keys[i] + " = '" + value.ToString() + "' AND ";
                i++;
            }
            query = query.Remove(query.Length - 5);
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    reader.Close();
                    reader.Dispose();
                    result.Cancel();
                    result.Dispose();
                }
            }
            CloseDatabase(dbcon);
            return true;
        }

        public void CloseDatabase(SqlConnection connection)
        {
            connection.Close();
            connection.Dispose();
        }

        public override void CloseDatabase()
        {
            m_connection.Close();
            m_connection.Dispose();
        }

        public override void CreateTable(string table, ColumnDefinition[] columns)
        {
            if (TableExists(table))
            {
                throw new DataManagerException("Trying to create a table with name of one that already exists.");
            }

            string columnDefinition = string.Empty;
            var primaryColumns = (from cd in columns where cd.IsPrimary == true select cd);
            bool multiplePrimary = primaryColumns.Count() > 1;

            foreach (ColumnDefinition column in columns)
            {
                if (columnDefinition != string.Empty)
                {
                    columnDefinition += ", ";
                }
                columnDefinition += column.Name + " " + GetColumnTypeStringSymbol(column.Type) + ((column.IsPrimary && !multiplePrimary) ? " PRIMARY KEY" : string.Empty);
            }

            string multiplePrimaryString = string.Empty;
            if (multiplePrimary)
            {
                string listOfPrimaryNamesString = string.Empty;
                foreach (ColumnDefinition column in primaryColumns)
                {
                    if (listOfPrimaryNamesString != string.Empty)
                    {
                        listOfPrimaryNamesString += ", ";
                    }
                    listOfPrimaryNamesString += column.Name;
                }
                multiplePrimaryString = string.Format(", PRIMARY KEY ({0}) ", listOfPrimaryNamesString);
            }

            string query = string.Format("create table " + table + " ( {0} {1}) ", columnDefinition, multiplePrimaryString);

            SqlConnection dbcon = GetLockedConnection();
            SqlCommand dbcommand = dbcon.CreateCommand();
            dbcommand.CommandText = query;
            dbcommand.ExecuteNonQuery();
            CloseDatabase(dbcon);
        }

        private string GetColumnTypeStringSymbol(ColumnTypes type)
        {
            switch (type)
            {
                case ColumnTypes.Integer:
                    return "INTEGER";
                case ColumnTypes.String:
                    return "TEXT";
                case ColumnTypes.String1:
                    return "VARCHAR(1)";
                case ColumnTypes.String2:
                    return "VARCHAR(2)";
                case ColumnTypes.String45:
                    return "VARCHAR(45)";
                case ColumnTypes.String50:
                    return "VARCHAR(50)";
                case ColumnTypes.String100:
                    return "VARCHAR(100)";
                case ColumnTypes.String512:
                    return "VARCHAR(512)";
                case ColumnTypes.String1024:
                    return "VARCHAR(1024)";
                case ColumnTypes.String8196:
                    return "VARCHAR(8196)";
                case ColumnTypes.Blob:
                    return "image";
                case ColumnTypes.Date:
                    return "DATE";
                case ColumnTypes.Text:
                    return "TEXT";
                default:
                    throw new DataManagerException("Unknown column type.");
            }
        }

        public override void DropTable(string tableName)
        {
            SqlConnection dbcon = GetLockedConnection();
            SqlCommand dbcommand = dbcon.CreateCommand();
            dbcommand.CommandText = string.Format("drop table {0}", tableName); ;
            dbcommand.ExecuteNonQuery();
            CloseDatabase(dbcon);
        }

        protected override void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions)
        {
            SqlConnection dbcon = GetLockedConnection();
            SqlCommand dbcommand = dbcon.CreateCommand();
            dbcommand.CommandText = string.Format("insert into {0} select * from {1}", destinationTableName, sourceTableName);
            dbcommand.ExecuteNonQuery();
            CloseDatabase(dbcon);
        }

        public override bool TableExists(string table)
        {
            SqlConnection dbcon = GetLockedConnection();
            SqlCommand dbcommand = dbcon.CreateCommand();
            dbcommand.CommandText = string.Format("select table_name from information_schema.tables where table_schema=database() and table_name='{0}'", table.ToLower());
            var rdr = dbcommand.ExecuteReader();

            var ret = false;
            if (rdr.Read())
            {
                ret = true;
            }

            rdr.Close();
            rdr.Dispose();
            dbcommand.Dispose();
            CloseDatabase(dbcon);
            return ret;
        }

        protected override List<ColumnDefinition> ExtractColumnsFromTable(string tableName)
        {
            var defs = new List<ColumnDefinition>();


            SqlConnection dbcon = GetLockedConnection();
            SqlCommand dbcommand = dbcon.CreateCommand();
            dbcommand.CommandText = string.Format("desc {0}", tableName);
            var rdr = dbcommand.ExecuteReader();
            while (rdr.Read())
            {
                var name = rdr["Field"];
                var pk = rdr["Key"];
                var type = rdr["Type"];
                defs.Add(new ColumnDefinition { Name = name.ToString(), IsPrimary = pk.ToString()=="PRI", Type = ConvertTypeToColumnType(type.ToString()) });
            }
            rdr.Close();
            rdr.Dispose();
            dbcommand.Dispose();
            CloseDatabase(dbcon);
            return defs;
        }

        private ColumnTypes ConvertTypeToColumnType(string typeString)
        {
            string tStr = typeString.ToLower();
            //we'll base our names on lowercase
            switch (tStr)
            {
                case "int(11)":
                    return ColumnTypes.Integer;
                case "integer":
                    return ColumnTypes.Integer;
                case "varchar(1)":
                    return ColumnTypes.String1;
                case "varchar(2)":
                    return ColumnTypes.String2;
                case "varchar(45)":
                    return ColumnTypes.String45;
                case "varchar(50)":
                    return ColumnTypes.String50;
                case "varchar(100)":
                    return ColumnTypes.String100;
                case "varchar(512)":
                    return ColumnTypes.String512;
                case "varchar(1024)":
                    return ColumnTypes.String1024;
                case "date":
                    return ColumnTypes.Date;
                case "varchar(8196)":
                    return ColumnTypes.String8196;
                case "text":
                    return ColumnTypes.Text;
                case "image":
                    return ColumnTypes.Blob;
                default:
                    throw new Exception("You've discovered some type in MySQL that's not reconized by Aurora, please place the correct conversion in ConvertTypeToColumnType.");
            }
        }
    }
}

