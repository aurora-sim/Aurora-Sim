using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Aurora.DataManager;
using C5;
using Mono.Data.SqliteClient;
using OpenSim.Data;

namespace Aurora.DataManager.SQLite
{
    public class SQLiteLoader : IGenericData
    {

        SqliteConnection m_Connection = null;
        protected Dictionary<string, FieldInfo> m_Fields =
                new Dictionary<string, FieldInfo>();

        protected List<string> m_ColumnNames = null;
        protected FieldInfo m_DataField = null;
        private string m_ConnectionString = "";
        protected const string versionTableName = "aurora_version";
        protected const string columnVersion = "version";

        public string Identifier
        {
            get { return "SQLiteConnector"; }
        }
        public void ConnectToDatabase(string connectionString)
        {
            m_Connection = new SqliteConnection(connectionString);
            m_Connection.Open();
            m_ConnectionString = connectionString;
            MigrationsManager();
        }

        #region Migrations
        public void MigrationsManager()
        {
            UpdateTable(Environment.CurrentDirectory + "//Aurora//Migrations//SQLite//");
        }

        private void UpdateTable(string location)
        {
            SqliteCommand cmd = new SqliteCommand();
            string query = "select migrations from Migrations";
            cmd.CommandText = query;
            try
            {
                IDataReader reader = GetReader(cmd);
                string RetVal = "";
                if (reader.Read())
                {
                    RetVal = reader.GetString(0);
                }
                reader.Close();
                reader.Dispose();
                UpgradeTable(location, Convert.ToInt32(RetVal));
            }
            catch (Exception ex)
            {
                UpgradeTable(location, 0);
            }
        }

        private void UpgradeTable(string location, int currentVersion)
        {
            List<StreamReader> files = ReadSQLMigrations(location);
            foreach (StreamReader file in files)
            {
                List<string> fileLines = new List<string>();
                string line = "";
                while ((line = file.ReadLine()) != null)
                {
                    fileLines.Add(line);
                }
                string Migrations = fileLines[0].Split('=')[1];
                Migrations = Migrations.TrimStart(' ');
                int MigrationsVersion = Convert.ToInt32(Migrations);
                if (MigrationsVersion == currentVersion)
                    continue;
                if (MigrationsVersion > currentVersion)
                {
                    foreach(string sqlstring in fileLines)
                    {
                        if (!sqlstring.StartsWith("Migrations") && !sqlstring.StartsWith("//") && !(sqlstring == ""))
                        {
                            SqliteCommand cmd = new SqliteCommand();
                            cmd.CommandText = sqlstring;
                            ExecuteNonQuery(cmd);
                        }
                    }
                    currentVersion = MigrationsVersion;
                }
            }
        }

        #region Find Updates
        public List<StreamReader> ReadSQLMigrations(string Dir)
        {
            DirectoryInfo dir = new DirectoryInfo(Dir);
            List<StreamReader> files = new List<StreamReader>();

            foreach (FileInfo fileInfo in dir.GetFiles("*.sql"))
            {
                files.Add(fileInfo.OpenText());
            }
            return files;
        }

        #endregion

        #endregion

        private void CheckColumnNames(IDataReader reader)
        {
            if (m_ColumnNames != null)
                return;

            m_ColumnNames = new List<string>();

            DataTable schemaTable = reader.GetSchemaTable();
            foreach (DataRow row in schemaTable.Rows)
            {
                if (row["ColumnName"] != null &&
                        (!m_Fields.ContainsKey(row["ColumnName"].ToString())))
                    m_ColumnNames.Add(row["ColumnName"].ToString());
            }
        }
        protected IDataReader ExecuteReader(SqliteCommand cmd)
        {
            SqliteConnection newConnection =
                    (SqliteConnection)((ICloneable)m_Connection).Clone();
            newConnection.Open();
            cmd.Connection = newConnection;
            SqliteDataReader reader = cmd.ExecuteReader();
            return reader;
        }
        protected int ExecuteNonQuery(SqliteCommand cmd)
        {
            lock (m_Connection)
            {
                SqliteConnection newConnection =
                    (SqliteConnection)((ICloneable)m_Connection).Clone();
                newConnection.Open();
                cmd.Connection = newConnection;

                return cmd.ExecuteNonQuery();
            }
        }
        protected IDataReader GetReader(SqliteCommand cmd)
        {
            IDataReader reader = ExecuteReader(cmd);
            if (reader == null)
                return null;

            CheckColumnNames(reader);
            return reader;
        }
        protected void CloseReaderCommand(SqliteCommand cmd)
        {
            cmd.Connection.Close();
            cmd.Connection.Dispose();
            cmd.Dispose();
        }

        public List<string> Query(string keyRow, string keyValue, string table, string wantedValue)
        {
            SqliteCommand cmd = new SqliteCommand();
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
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            List<string> RetVal = new List<string>();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    RetVal.Add(reader.GetString(i));
                }
            }
            if (RetVal.Count == 0)
                RetVal.Add("");
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);

            return RetVal;
        }

        //public List<string> QueryWithDefault<T>(string keyRow, string keyValue, string table, string wantedValue, T defaultValue)
        //{
        //    var result = Query(keyRow, keyValue, table, wantedValue);
        //    if (result.Count == 0)
        //    {
        //        result.Add(defaultValue.ToString());
        //    }
        //    return result;
        //}

        public void Insert(string table, string[] values)
        {
            SqliteCommand cmd = new SqliteCommand();

            string query = "";
            query = String.Format("insert into '{0}' values ('", table);
            foreach (string value in values)
            {
                query = String.Format(query + "{0}','", value);
            }
            query = query.Remove(query.Length - 2);
            query += ")";
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        public void Delete(string table, string[] keys, string[] values)
        {
            SqliteCommand cmd = new SqliteCommand();

            string query = String.Format("delete from '{0}' where ", table); ;
            int i = 0;
            foreach (string value in values)
            {
                query += keys[i] + " = '" + value + "' and ";
                i++;
            }
            query = query.Remove(query.Length - 4);
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        public void Insert(string table, string[] values, string updateKey, string updateValue)
        {
            SqliteCommand cmd = new SqliteCommand();

            string query = "";
            query = String.Format("insert into '{0}' values ('", table);
            foreach (string value in values)
            {
                query = String.Format(query + "{0}','", value);
            }
            query = query.Remove(query.Length - 2);
            query += ")";
            cmd.CommandText = query;
            try
            {
                ExecuteNonQuery(cmd);
                CloseReaderCommand(cmd);
            }
            //Execute the update then...
            catch (Exception ex)
            {
                cmd = new SqliteCommand();
                query = String.Format("UPDATE {0} SET {1} = '{2}'", table, updateKey, updateValue);
                cmd.CommandText = query;
                ExecuteNonQuery(cmd);
                CloseReaderCommand(cmd);
            }
        }

        public void Update(string table, string[] setValues, string[] setRows, string[] keyRows, string[] keyValues)
        {
            string query = "update " + table + " set ";
            int i = 0;
            foreach (string value in setValues)
            {
                query += setRows[i] + " = '" + value + "',";
                i++;
            }
            i = 0;
            query = query.Remove(query.Length - 1);
            query += " where ";
            foreach (string value in keyValues)
            {
                query += keyRows[i] + " = '" + value + "' and";
                i++;
            }
            query = query.Remove(query.Length - 4);
            SqliteCommand cmd = new SqliteCommand();
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        public void CloseDatabase()
        {
            m_Connection.Close();
            m_Connection.Dispose();
        }

        public bool TableExists(string tableName)
        {
            SqliteCommand cmd = new SqliteCommand();
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

        public void CreateTable(string table, List<Rec<string, ColumnTypes>> columns)
        {
            if( TableExists(table))
            {
                throw new DataManagerException("Trying to create a table with name of one that already exists.");
            }

            string columnDefinition = string.Empty;
            foreach (var column in columns)
            {
                if( columnDefinition != string.Empty )
                {
                    columnDefinition += ", ";
                }
                columnDefinition += column.X1 + " " + GetColumnTypeStringSymbol(column.X2);
            }

            string query = string.Format("create table " + table + " ( {0} ) ",columnDefinition);

            SqliteCommand cmd = new SqliteCommand();
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        private string GetColumnTypeStringSymbol(ColumnTypes type)
        {
            switch(type)
            {
                case ColumnTypes.Integer:
                    return "INTEGER";
                case ColumnTypes.String:
                    return "TEXT";
                case ColumnTypes.Date:
                    return "DATE";
                default:
                    throw new DataManagerException("Unknown column type.");
            }
        }

        public Version GetAuroraVersion()
        {
            
            if (!TableExists(versionTableName))
            {
                CreateTable(versionTableName, new List<Rec<string, ColumnTypes>>() {new Rec<string, ColumnTypes>(columnVersion, ColumnTypes.String)});
            }

            var results = Query(string.Empty, string.Empty, versionTableName, columnVersion);
            if (results.Count > 0)
            {
                Version highestVersion = null;
                foreach (var result in results)
                {
                    if( result.Trim() == string.Empty)
                    {
                        continue;
                    }
                    Version version = new Version(result);
                    if( highestVersion == null || version > highestVersion)
                    {
                        highestVersion = version;
                    }
                }
                return highestVersion;
            }
            
            return null;
        }

        public void WriteAuroraVersion(Version version)
        {
            if (!TableExists(versionTableName))
            {
                CreateTable(versionTableName, new List<Rec<string, ColumnTypes>>() { new Rec<string, ColumnTypes>(columnVersion, ColumnTypes.String) });
            }
            Insert(versionTableName,new []{version.ToString()});
        }
    }
}
