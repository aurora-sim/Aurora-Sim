using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using C5;
using MySql.Data.MySqlClient;
using Aurora.DataManager;

namespace Aurora.DataManager.MySQL
{
    public class MySQLDataLoader : DataManagerBase
    {
        MySqlConnection MySQLConn = null;
        readonly Mutex m_lock = new Mutex(false);
        string connectionString = "";

        public override string Identifier
        {
            get { return "MySQLData"; }
        }

        public MySqlConnection GetLockedConnection()
        {
            if (MySQLConn != null)
            {
                if (MySQLConn.Ping())
                {
                    return MySQLConn;
                }
                MySQLConn.Close();
                MySQLConn.Open();
                return MySQLConn;
            }
            try
            {
                m_lock.WaitOne();
            }
            catch (Exception ex)
            {
                ex = new Exception();
                m_lock.ReleaseMutex();
                return GetLockedConnection();
            }

            try
            {
                MySqlConnection dbcon = new MySqlConnection(connectionString);
                try
                {
                    dbcon.Open();
                    MySQLConn = dbcon;
                }
                catch (Exception e)
                {
                    MySQLConn = null;
                    throw new Exception("[MySQLData] Connection error while using connection string [" + connectionString + "]", e);
                }
                return dbcon;
            }
            catch (Exception e)
            {
                throw new Exception("[MySQLData] Error initialising database: " + e.ToString());
            }
            finally
            {
                m_lock.ReleaseMutex();
            }
        }

        public IDbCommand Query(string sql, Dictionary<string, object> parameters, MySqlConnection dbcon)
        {
            MySqlCommand dbcommand;
            try
            {
                dbcommand = (MySqlCommand)dbcon.CreateCommand();
                dbcommand.CommandText = sql;
                foreach (System.Collections.Generic.KeyValuePair<string, object> param in parameters)
                {
                    dbcommand.Parameters.AddWithValue(param.Key, param.Value);
                }
                return (IDbCommand)dbcommand;
            }
            catch (Exception e)
            {
                // Return null if it fails.
                return null;
            }
        }

        public override void ConnectToDatabase(string connectionstring)
        {
            connectionString = connectionstring;
            GetLockedConnection();
            MigrationsManager();
        }

        #region Migrations
        public void MigrationsManager()
        {
            UpdateTable(Environment.CurrentDirectory + "//Aurora//Migrations//MySQL//");
        }

        private void UpdateTable(string location)
        {
            string query = "select migrations from Migrations";
            try
            {
                string RetVal = "";
                MySqlConnection dbcon = GetLockedConnection();
                IDbCommand result;
                IDataReader reader;
                using (result = Query(query, new Dictionary<string, object>(), dbcon))
                {
                    using (reader = result.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            RetVal = reader.GetString(0);
                        }
                    }
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
                    foreach (string sqlstring in fileLines)
                    {
                        if (!sqlstring.StartsWith("Migrations") && !sqlstring.StartsWith("//") && !(sqlstring == ""))
                        {
                            MySqlConnection dbcon = GetLockedConnection();
                            IDbCommand result;
                            IDataReader reader;
                            using (result = Query(sqlstring, new Dictionary<string, object>(), dbcon))
                            {
                                using (reader = result.ExecuteReader())
                                {
                                }
                            }
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


        private void CreateTables()
        {
            string query = "CREATE TABLE usernotes (useruuid VARCHAR(50), targetuuid VARCHAR(50),notes VARCHAR(512),noteUUID VARCHAR(50) PRIMARY KEY);";
            CreateTable(query);
            query = "CREATE TABLE userpicks (pickuuid VARCHAR(50) PRIMARY KEY, creatoruuid VARCHAR(50),toppick VARCHAR(512),parceluuid VARCHAR(50),name VARCHAR(50),description VARCHAR(50),snapshotuuid VARCHAR(50),user VARCHAR(50),originalname VARCHAR(50),simname VARCHAR(50),posglobal VARCHAR(50),sortorder VARCHAR(50),enabled VARCHAR(50));";
            CreateTable(query);
            query = "CREATE TABLE usersauth (userUUID VARCHAR(50) PRIMARY KEY, userLogin VARCHAR(50),userFirst VARCHAR(512),userLast VARCHAR(50),userEmail VARCHAR(50),userPass VARCHAR(50),userMac VARCHAR(50),userIP VARCHAR(50),userAcceptTOS VARCHAR(50),userGodLevel VARCHAR(50),userRealFirst VARCHAR(50),userRealLast VARCHAR(50),userAddress VARCHAR(50),userZip VARCHAR(50),userCountry VARCHAR(50),tempBanned VARCHAR(50),permaBanned VARCHAR(50),profileAllowPublish VARCHAR(50),profileMaturePublish VARCHAR(50),profileURL VARCHAR(50), AboutText VARCHAR(50), Email VARCHAR(50), CustomType VARCHAR(50), profileWantToMask VARCHAR(50),profileWantToText VARCHAR(50),profileSkillsMask VARCHAR(50),profileSkillsText VARCHAR(50),profileLanguages VARCHAR(50),visible VARCHAR(50),imviaemail VARCHAR(50),membershipGroup VARCHAR(50),FirstLifeAboutText VARCHAR(50),FirstLifeImage VARCHAR(50),Partner VARCHAR(50), Image VARCHAR(50));";
            CreateTable(query);
            query = "CREATE TABLE classifieds (classifieduuid VARCHAR(50) PRIMARY KEY, creatoruuid VARCHAR(50),creationdate VARCHAR(512),expirationdate VARCHAR(50),category VARCHAR(50),name VARCHAR(50),description VARCHAR(50),parceluuid VARCHAR(50),parentestate VARCHAR(50),snapshotuuid VARCHAR(50),simname VARCHAR(50),posglobal VARCHAR(50),parcelname VARCHAR(50),classifiedflags VARCHAR(50),priceforlisting VARCHAR(50));";
            CreateTable(query);
            query = "CREATE TABLE auroraregions (regionName VARCHAR(50), regionHandle VARCHAR(50),hidden VARCHAR(1),regionUUID VARCHAR(50) PRIMARY KEY,regionX VARCHAR(50),regionY VARCHAR(50),telehubX VARCHAR(50),telehubY VARCHAR(50));";
            CreateTable(query);
            query = "CREATE TABLE macban (macAddress VARCHAR(50) PRIMARY KEY);";
            CreateTable(query);
            query = "CREATE TABLE BannedViewers (Client VARCHAR(50) PRIMARY KEY);";
            CreateTable(query);
            query = "CREATE TABLE mutelists (userID VARCHAR(50) ,muteID VARCHAR(50),muteName VARCHAR(50),muteType VARCHAR(50),muteUUID VARCHAR(50) PRIMARY KEY);";
            CreateTable(query);
            query = "CREATE TABLE abusereports (Category VARCHAR(100) ,AReporter VARCHAR(100),OName VARCHAR(100),OUUID VARCHAR(100),AName VARCHAR(100) PRIMARY KEY,ADetails VARCHAR(100),OPos VARCHAR(100),Estate VARCHAR(100),Summary VARCHAR(100));";
            CreateTable(query);
            query = "CREATE TABLE primsMediaURL (objectUUID VARCHAR(100) ,CurrentURL VARCHAR(100),HomeURL VARCHAR(512),User VARCHAR(100),Version VARCHAR(100), PRIMARY KEY (Version, objectUUID) );";
            CreateTable(query);
        }

        private bool CreateTable(string sqlstatement)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            using (result = Query(sqlstatement, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        return true;
                    }
                    finally { }
                }
            }
        }

        public override List<string> Query(string keyRow, string keyValue, string table, string wantedValue)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            List<string> RetVal = new List<string>();
            string query = "";
            if(keyRow == "")
                query = "select " + wantedValue + " from " + table;
            else
                query = "select " + wantedValue + " from " + table + " where " + keyRow + " = " + keyValue;
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
                        if (RetVal.Count == 0)
                        {
                            RetVal.Add("");
                            return RetVal;
                        }
                        else
                        {
                            return RetVal;
                        }
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }
        }

        //public List<string> QueryWithDefault<T>(string keyRow, string keyValue, string table, string wantedValue, T defaultValue)
        //{
        //    var result = Query(keyRow, keyValue, table, wantedValue);
        //    if( result.Count == 0)
        //    {
        //        result.Add(defaultValue.ToString());
        //    }
        //    return result;
        //}

        public List<string> Query(string keyRow, string keyValue, string table)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            List<string> RetVal = new List<string>();
            using (result = Query("select * from " + table + " where " + keyRow + " = " + keyValue, new Dictionary<string, object>(), dbcon))
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
                        if (RetVal.Count == 0)
                        {
                            RetVal.Add("");
                            return RetVal;
                        }
                        else
                        {
                            return RetVal;
                        }
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }
        }

        public override void Update(string table, string[] setValues, string[] setRows, string[] keyRows, string[] keyValues)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            string query = "update '" + table + "' set '";
            int i = 0;
            foreach (string value in setValues)
            {
                query += setRows[i] + "' = '" + value + "',";
                i++;
            }
            i = 0;
            query = query.Remove(query.Length - 1);
            query += " WHERE '";
            foreach (string value in keyValues)
            {
                query += keyRows[i] + "' = '" + value + "' AND";
                i++;
            }
            query = query.Remove(query.Length - 3);
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    reader.Close();
                    reader.Dispose();
                    result.Dispose();
                }
            }
        }

        public override void CloseDatabase()
        {
            throw new NotImplementedException();
        }

        public override bool TableExists(string table)
        {
            throw new NotImplementedException();
        }

        public override void CreateTable(string table, ColumnDefinition[] columns)
        {
            throw new NotImplementedException();
        }

        public override void Insert(string table, string[] values)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            string query = "insert into " + table + " VALUES('";
            foreach (string value in values)
            {
                query += value + "','";
            }
            query += ")";
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    reader.Close();
                    reader.Dispose();
                    result.Dispose();
                }
            }
        }
        public override void Insert(string table, string[] values, string updateKey, string updateValue)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            string query = "insert into " + table + " VALUES('";
            foreach (string value in values)
            {
                query += value + "','";
            }
            query += ") ON DUPLICATE KEY UPDATE '" + updateKey+"' = '" + updateValue + "'";
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    reader.Close();
                    reader.Dispose();
                    result.Dispose();
                }
            }
        }
        public override void Delete(string table, string[] keys, string[] values)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            string query = "delete from " + table + " WHERE '";
            int i = 0;
            foreach (string value in values)
            {
                query += keys[i] + "' = '" + value + "' AND";
                i++;
            }
            query = query.Remove(query.Length - 3);
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                }
            }
        }

        public Version GetAuroraVersion()
        {
            throw new NotImplementedException();
        }

        public void WriteAuroraVersion(Version version)
        {
            throw new NotImplementedException();
        }

        public void CopyTableToTable(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions)
        {
            throw new NotImplementedException();
        }

        public bool VerifyTableExists(string tableName, ColumnDefinition[] columnDefinitions)
        {
            throw new NotImplementedException();
        }

        public void EnsureTableExists(string tableName, ColumnDefinition[] columnDefinitions)
        {
            throw new NotImplementedException();
        }

        public override void DropTable(string tableName)
        {
            throw new NotImplementedException();
        }

        protected override void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions)
        {
            throw new NotImplementedException();
        }

        protected override List<ColumnDefinition> ExtractColumnsFromTable(string tableName)
        {
            throw new NotImplementedException();
        }

        public string Query(string query)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        if (reader.Read())
                        {
                            return reader.GetString(0);
                        }
                        else
                        {
                            return "";
                        }
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }
        }
    }
}

