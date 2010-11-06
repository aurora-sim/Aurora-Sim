using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Data.Sqlite;
using Aurora.Framework;
using OpenSim.Framework;
using OpenMetaverse;
using log4net;
using Aurora.DataManager.Migration;

namespace Aurora.DataManager.SQLite
{
    public class SQLiteLoader : DataManagerBase
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private SqliteConnection m_Connection;

        protected Dictionary<string, FieldInfo> m_Fields = new Dictionary<string, FieldInfo>();


        public override string Identifier
        {
            get { return "SQLiteConnector"; }
        }

        public override void ConnectToDatabase(string connectionString)
        {
            m_Connection = new SqliteConnection(connectionString);
            m_Connection.Open();

            var migrationManager = new MigrationManager(DataSessionProviderConnector.DataSessionProvider, this);
            migrationManager.DetermineOperation();
            migrationManager.ExecuteOperation();
        }

        protected IDataReader ExecuteReader(SqliteCommand cmd)
        {
            try
            {
                var newConnection =
                    (SqliteConnection)((ICloneable)m_Connection).Clone();
                if (newConnection.State != ConnectionState.Open)
                    newConnection.Open();
                cmd.Connection = newConnection;
                SqliteDataReader reader = cmd.ExecuteReader();
                return reader;
            }
            catch (Mono.Data.Sqlite.SqliteException ex)
            {
                m_log.Warn("[SQLiteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " + ex);
                //throw ex;
            }
            catch (Exception ex)
            {
                m_log.Warn("[SQLiteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " + ex);
                throw ex;
            }
            return null;
        }

        protected int ExecuteNonQuery(SqliteCommand cmd)
        {
            try
            {
                lock (m_Connection)
                {
                    var newConnection =
                        (SqliteConnection)((ICloneable)m_Connection).Clone();
                    if (newConnection.State != ConnectionState.Open)
                        newConnection.Open();
                    cmd.Connection = newConnection;

                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Mono.Data.Sqlite.SqliteException)
            {
                //m_log.Warn("[SQLiteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " + ex);
                //throw ex;
            }
            catch (Exception ex)
            {
                m_log.Warn("[SQLiteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " + ex);
                throw ex;
            }
            return 0;
        }

        protected IDataReader GetReader(SqliteCommand cmd)
        {
            return ExecuteReader(cmd);
        }

        protected void CloseReaderCommand(SqliteCommand cmd)
        {
            cmd.Connection.Close();
        }

        public override List<string> Query(string keyRow, object keyValue, string table, string wantedValue)
        {
            var cmd = new SqliteCommand();
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
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            var RetVal = new List<string>();
            try
            {
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (reader[i] is byte[])
                            RetVal.Add(OpenMetaverse.Utils.BytesToString((byte[])reader[i]));
                        else
                            RetVal.Add(reader[i].ToString());
                    }
                }
                reader.Close();
                CloseReaderCommand(cmd);
            }
            catch
            {
            }

            return RetVal;
        }

        public override IDataReader QueryReader(string keyRow, object keyValue, string table, string wantedValue)
        {
            var cmd = new SqliteCommand();
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
            cmd.CommandText = query;
            return GetReader(cmd);
        }

        public override List<string> Query(string whereClause, string table, string wantedValue)
        {
            var cmd = new SqliteCommand();
            string query = "";
            query = String.Format("select {0} from {1} where {2}",
                                      wantedValue, table, whereClause);
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            var RetVal = new List<string>();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    RetVal.Add(reader.GetString(i));
                }
            }
            reader.Close();
            CloseReaderCommand(cmd);

            return RetVal;
        }

        public override List<string> QueryFullData(string whereClause, string table, string wantedValue)
        {
            var cmd = new SqliteCommand();
            string query = "";
            query = String.Format("select {0} from {1} {2}",
                                      wantedValue, table, whereClause);
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            var RetVal = new List<string>();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    RetVal.Add(reader.GetValue(i).ToString());
                }
            }
            reader.Close();
            CloseReaderCommand(cmd);

            return RetVal;
        }



        public override IDataReader QueryDataFull(string whereClause, string table, string wantedValue)
        {
            var cmd = new SqliteCommand();
            string query = "";
            query = String.Format("select {0} from {1} {2}",
                                      wantedValue, table, whereClause);
            cmd.CommandText = query;
            return GetReader(cmd);
        }

        public override IDataReader QueryData(string whereClause, string table, string wantedValue)
        {
            var cmd = new SqliteCommand();
            string query = "";
            query = String.Format("select {0} from {1} where {2}",
                                      wantedValue, table, whereClause);
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            return reader;
        }

        public override List<string> Query(string keyRow, object keyValue, string table, string wantedValue, string Order)
        {
            var cmd = new SqliteCommand();
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
            cmd.CommandText = query + Order;
            IDataReader reader = GetReader(cmd);
            var RetVal = new List<string>();
            try
            {
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        RetVal.Add(reader.GetString(i));
                    }
                }
            }
            catch { }
            reader.Close();
            CloseReaderCommand(cmd);

            return RetVal;
        }

        public override List<string> Query(string[] keyRow, object[] keyValue, string table, string wantedValue)
        {
            var cmd = new SqliteCommand();
            string query = String.Format("select {0} from {1} where ",
                                      wantedValue, table);
            int i = 0;
            foreach (object value in keyValue)
            {
                query += String.Format("{0} = '{1}' and ", keyRow[i], value);
                i++;
            }
            query = query.Remove(query.Length - 5);
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            var RetVal = new List<string>();
            while (reader.Read())
            {
                for (i = 0; i < reader.FieldCount; i++)
                {
                    RetVal.Add(reader[i].ToString());
                }
            }
            reader.Close();
            CloseReaderCommand(cmd);

            return RetVal;
        }

        public override bool Insert(string table, object[] values)
        {
            var cmd = new SqliteCommand();

            string query = "";
            query = String.Format("insert into {0} values (", table);
            foreach (object value in values)
            {
                object v = value;
                if (v is byte[])
                    v = OpenMetaverse.Utils.BytesToString((byte[])v);
                query = String.Format(query + "'{0}',", v);
            }
            query = query.Remove(query.Length - 1);
            query += ")";
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override bool Insert(string table, string[] keys, object[] values)
        {
            SqliteCommand cmd = new SqliteCommand();

            string query = "";
            query = String.Format("insert into {0} (", table);
            
            int i = 0;
            foreach (object key in keys)
            {
                cmd.Parameters.AddWithValue(":" + key, values[i]);
                query += key + ",";
                i++;
            }

            query = query.Remove(query.Length - 1);
            query += ") values (";

            foreach (object key in keys)
            {
                query += String.Format(":{0},", key);
            }
            query = query.Remove(query.Length - 1);
            query += ")";

            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override bool Replace(string table, string[] keys, object[] values)
        {
            var cmd = new SqliteCommand();

            string query = "";
            query = String.Format("replace into {0} (", table);

            int i = 0;
            foreach (string key in keys)
            {
                string k = key;
                if (k.StartsWith("`"))
                {
                    k = k.Remove(0, 1);
                    k = k.Remove(k.Length - 1, 1);
                }
                cmd.Parameters.AddWithValue(":" + k, values[i]);
                query += key + ",";
                i++;
            }

            query = query.Remove(query.Length - 1);
            query += ") values (";

            foreach (string key in keys)
            {
                string k = key;
                if (k.StartsWith("`"))
                {
                    k = k.Remove(0, 1);
                    k = k.Remove(k.Length - 1, 1);
                }
                query += String.Format(":{0},", k);
            }

            query = query.Remove(query.Length - 1);
            query += ")";

            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override bool Delete(string table, string[] keys, object[] values)
        {
            var cmd = new SqliteCommand();

            string query = String.Format("delete from {0} where ", table);
            ;
            int i = 0;
            foreach (object value in values)
            {
                query += keys[i] + " = '" + value.ToString() + "' and ";
                i++;
            }
            query = query.Remove(query.Length - 4);
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override bool Insert(string table, object[] values, string updateKey, object updateValue)
        {
            var cmd = new SqliteCommand();

            string query = "";
            query = String.Format("insert into {0} values (", table);
            foreach (object value in values)
            {
                query = String.Format(query + "'{0}',", value);
            }
            query = query.Remove(query.Length - 1);
            query += ")";
            cmd.CommandText = query;
            try
            {
                ExecuteNonQuery(cmd);
                CloseReaderCommand(cmd);
            }
                //Execute the update then...
            catch (Exception)
            {
                cmd = new SqliteCommand();
                query = String.Format("UPDATE {0} SET {1} = '{2}'", table, updateKey, updateValue.ToString());
                cmd.CommandText = query;
                ExecuteNonQuery(cmd);
                CloseReaderCommand(cmd);
            }
            return true;
        }

        public override bool Update(string table, object[] setValues, string[] setRows, string[] keyRows, object[] keyValues)
        {
            var cmd = new SqliteCommand();
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string query = String.Format("update {0} set ", table);
            int i = 0;
            
            foreach (object value in setValues)
            {
                query += string.Format("{0} = :{1},", setRows[i], setRows[i]);

                cmd.Parameters.AddWithValue(":" + setRows[i], value);
                i++;
            }
            i = 0;
            query = query.Remove(query.Length - 1);
            query += " where ";
            foreach (object value in keyValues)
            {
                query += String.Format("{0} = '{1}' and ", keyRows[i], value);
                i++;
            }
            query = query.Remove(query.Length - 5);
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override void CloseDatabase()
        {
            m_Connection.Close();
        }

        public override bool TableExists(string tableName)
        {
            var cmd = new SqliteCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE name='" + tableName + "'";
            IDataReader rdr = ExecuteReader(cmd);
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
            if( multiplePrimary )
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

            var cmd = new SqliteCommand();
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
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
                    return "blob";
                case ColumnTypes.Text:
                    return "TEXT";
                case ColumnTypes.Date:
                    return "DATE";
                default:
                    throw new DataManagerException("Unknown column type.");
            }
        }

        protected override List<ColumnDefinition> ExtractColumnsFromTable(string tableName)
        {
            var defs = new List<ColumnDefinition>();


            var cmd = new SqliteCommand();
            cmd.CommandText = string.Format("PRAGMA table_info({0})", tableName);
            IDataReader rdr = ExecuteReader(cmd);
            while (rdr.Read())
            {
                var name = rdr["name"];
                var pk = rdr["pk"];
                var type = rdr["type"];
                defs.Add(new ColumnDefinition {Name = name.ToString(), IsPrimary = (int.Parse(pk.ToString()) > 0), Type = ConvertTypeToColumnType(type.ToString())});
            }
            rdr.Close();
            CloseReaderCommand(cmd);

            return defs;
        }

        private ColumnTypes ConvertTypeToColumnType(string typeString)
        {
            string tStr = typeString.ToLower();
            //we'll base our names on lowercase
            switch (tStr)
            {
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
                case "text":
                    return ColumnTypes.Text;
                case "varchar(8196)":
                    return ColumnTypes.String8196;
                case "blob":
                    return ColumnTypes.Blob;
                default:
                    throw new Exception("You've discovered some type in SQLite that's not reconized by Aurora, please place the correct conversion in ConvertTypeToColumnType.");
            }
        }

        public override void DropTable(string tableName)
        {
            var cmd = new SqliteCommand();
            cmd.CommandText = string.Format("drop table {0}", tableName);
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        protected override void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions)
        {
            var cmd = new SqliteCommand();
            cmd.CommandText = string.Format("insert into {0} select * from {1}", destinationTableName, sourceTableName);
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        public override IGenericData Copy()
        {
            return new SQLiteLoader();
        }
    }
}