using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Data.SqliteClient;

namespace Aurora.DataManager.SQLite
{
    public class SQLiteLoader : DataManagerBase
    {
        protected List<string> m_ColumnNames;
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
        }

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
            var newConnection =
                (SqliteConnection) ((ICloneable) m_Connection).Clone();
            newConnection.Open();
            cmd.Connection = newConnection;
            SqliteDataReader reader = cmd.ExecuteReader();
            return reader;
        }

        protected int ExecuteNonQuery(SqliteCommand cmd)
        {
            lock (m_Connection)
            {
                var newConnection =
                    (SqliteConnection) ((ICloneable) m_Connection).Clone();
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

        public override List<string> Query(string keyRow, string keyValue, string table, string wantedValue)
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
                                      wantedValue, table, keyRow, keyValue);
            }
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
            if (RetVal.Count == 0)
                RetVal.Add("");
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);

            return RetVal;
        }

        public override void Insert(string table, string[] values)
        {
            var cmd = new SqliteCommand();

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

        public override void Delete(string table, string[] keys, string[] values)
        {
            var cmd = new SqliteCommand();

            string query = String.Format("delete from '{0}' where ", table);
            ;
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

        public override void Insert(string table, string[] values, string updateKey, string updateValue)
        {
            var cmd = new SqliteCommand();

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

        public override void Update(string table, string[] setValues, string[] setRows, string[] keyRows, string[] keyValues)
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
            var cmd = new SqliteCommand();
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        public override void CloseDatabase()
        {
            m_Connection.Close();
            m_Connection.Dispose();
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
            rdr.Dispose();
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
                case "text":
                    return ColumnTypes.String;
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
                default:
                    throw new Exception("You've discovered some type in SQLite that's not reconized by Aurora, please place the correct conversion in ConvertTypeToColumnType.");
            }
        }

        public override void DropTable(string tableName)
        {
            var cmd = new SqliteCommand();
            cmd.CommandText = string.Format("drop table {0}", tableName);
            ExecuteNonQuery(cmd);
        }

        protected override void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions)
        {
            var cmd = new SqliteCommand();
            cmd.CommandText = string.Format("insert into {0} select * from {1}", destinationTableName, sourceTableName);
            ExecuteNonQuery(cmd);
        }

        #region Migrations

        public void MigrationsManager()
        {
            UpdateTable(Environment.CurrentDirectory + "//Aurora//Migrations//SQLite//");
        }

        private void UpdateTable(string location)
        {
            var cmd = new SqliteCommand();
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
                var fileLines = new List<string>();
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
                    foreach (string sqlstring in fileLines)
                    {
                        if (!sqlstring.StartsWith("Migrations") && !sqlstring.StartsWith("//") && !(sqlstring == ""))
                        {
                            var cmd = new SqliteCommand();
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
            var dir = new DirectoryInfo(Dir);
            var files = new List<StreamReader>();

            foreach (FileInfo fileInfo in dir.GetFiles("*.sql"))
            {
                files.Add(fileInfo.OpenText());
            }
            return files;
        }

        #endregion

        #endregion
    }
}