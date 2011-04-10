using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using MySql.Data.MySqlClient;
using Aurora.Framework;
using Aurora.DataManager.Migration;
using log4net;

namespace Aurora.DataManager.MySQL
{
    public class MySQLDataLoader : DataManagerBase
    {
        private string m_connectionString = "";
        private MySqlConnection m_connection = null;
        private volatile bool m_locked = false;
        private volatile bool m_needsStateChange = false;
        private static readonly ILog m_log =
                LogManager.GetLogger (
                MethodBase.GetCurrentMethod ().DeclaringType);

        public override string Identifier
        {
            get { return "MySQLData"; }
        }

        public MySqlConnection GetLockedConnection()
        {
            while (m_locked)
            {
                Thread.Sleep(0);
            }

            m_locked = true;
            if (m_connection == null)
            {
                m_connection = new MySqlConnection(m_connectionString);
                m_connection.StateChange += new StateChangeEventHandler(ConnectionStateChange);
                m_connection.InfoMessage += new MySqlInfoMessageEventHandler(ConnectionInfoMessage);
                m_connection.Open();
            }
            else
            {
                try
                {
                    CheckConnection();
                    m_connection.Ping();
                }
                catch (MySqlException e)
                {
                    try
                    {
                        m_connection.Close();
                    }
                    catch { }
                    m_locked = false;
                    return GetLockedConnection();
                }
            }
            return m_connection;
        }

        private void CheckConnection()
        {
            if (m_needsStateChange)
            {
                //We need to reopen the connection, it timed out
                if (m_connection.State != ConnectionState.Open)
                    m_connection.Open ();
                m_needsStateChange = false;
            }
        }

        void ConnectionInfoMessage(object sender, MySqlInfoMessageEventArgs args)
        {
            if (args.errors != null)
            {
                foreach(MySqlError error in args.errors)
                {
                    m_log.DebugFormat ("[MySQLError]: Level: {0}, Message: {1}, Code: {2}", error.Level, error.Message, error.Code);
                }
            }
        }

        void ConnectionStateChange(object sender, StateChangeEventArgs e)
        {
            if (e.CurrentState == ConnectionState.Closed || e.CurrentState == ConnectionState.Broken)
            {
                //It closed or timed out, we need to restart it
                m_needsStateChange = true;
            }
        }

        public void CloseDatabase(MySqlConnection connection)
        {
            m_locked = false;
            //connection.Close();
            //connection.Dispose();
        }

        public override void CloseDatabase()
        {
            m_locked = false;
            //m_connection.Close();
            //m_connection.Dispose();
        }

        public IDbCommand Query(string sql, Dictionary<string, object> parameters, MySqlConnection dbcon)
        {
            MySqlCommand dbcommand;
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

        public override void ConnectToDatabase(string connectionstring, string migratorName, bool validateTables)
        {
            m_connectionString = connectionstring;
            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                CloseDatabase (dbcon);

                var migrationManager = new MigrationManager (this, migratorName, validateTables);
                migrationManager.DetermineOperation ();
                migrationManager.ExecuteOperation ();
            }
        }

        public override List<string> Query(string keyRow, object keyValue, string table, string wantedValue)
        {
            IDbCommand result = null;
            IDataReader reader = null;
            List<string> retVal = new List<string>();
            string query;
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
            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                try
                {
                    using (result = Query (query, new Dictionary<string, object> (), dbcon))
                    {
                        CheckConnection ();
                        using (reader = result.ExecuteReader ())
                        {
                            while (reader.Read ())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (reader[i] is byte[])
                                        retVal.Add (OpenMetaverse.Utils.BytesToString ((byte[])reader[i]));
                                    else
                                        retVal.Add (reader.GetString (i));
                                }
                            }
                            return retVal;
                        }
                    }
                }
                catch
                {
                    return retVal;
                }
                finally
                {
                    try
                    {
                        if (reader != null)
                        {
                            reader.Close();
                            reader.Dispose();
                        }
                        if (result != null) result.Dispose();
                    }catch{}
                    CloseDatabase (dbcon);
                }
            }
        }

        public override IDbCommand QueryReader(string keyRow, object keyValue, string table, string wantedValue)
        {
            string query;
            if (keyRow == "")
            {
                query = String.Format ("select {0} from {1}",
                                      wantedValue, table);
            }
            else
            {
                query = String.Format ("select {0} from {1} where {2} = '{3}'",
                                      wantedValue, table, keyRow, keyValue.ToString ());
            }
            MySqlConnection dbcon = GetLockedConnection ();
            return Query (query, new Dictionary<string, object> (), dbcon);
        }

        public override List<string> Query(string whereClause, string table, string wantedValue)
        {
            IDbCommand result = null;
            IDataReader reader = null;
            List<string> retVal = new List<string>();
            string query = String.Format("select {0} from {1} where {2}",
                                      wantedValue, table, whereClause);
            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                try
                {
                    using (result = Query(query, new Dictionary<string, object>(), dbcon))
                    {
                        CheckConnection();
                        using (reader = result.ExecuteReader())
                        {

                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (reader[i] != DBNull.Value)
                                        retVal.Add(reader.GetString(i));
                                }
                            }
                            return retVal;
                        }
                    }
                }
                finally
                {
                    try
                    {
                        if (reader != null)
                        {
                            reader.Close();
                            reader.Dispose();
                        }
                        if (result != null) result.Dispose();
                    }catch{}
                    CloseDatabase(dbcon);
                }
            }
        }

        public override List<string> QueryFullData(string whereClause, string table, string wantedValue)
        {
            IDbCommand result = null;
            IDataReader reader = null;
            List<string> retVal = new List<string>();
            string query = String.Format("select {0} from {1} {2}",
                                         wantedValue, table, whereClause);
            using (MySqlConnection dbcon = GetLockedConnection())
            {
                try
                {
                    using (result = Query(query, new Dictionary<string, object>(), dbcon))
                    {
                        CheckConnection();
                        using (reader = result.ExecuteReader())
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
                }
                finally
                {
                    try
                    {
                        if (reader != null)
                        {
                            reader.Close();
                            reader.Dispose();
                        }
                        if (result != null) result.Dispose();
                    }catch{}
                    CloseDatabase(dbcon);
                }
            }
        }

        public override IDbCommand QueryDataFull(string whereClause, string table, string wantedValue)
        {
            string query = String.Format("select {0} from {1} {2}",
                                      wantedValue, table, whereClause);
            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                 return Query (query, new Dictionary<string, object> (), dbcon);
            }
        }

        public override IDbCommand QueryData(string whereClause, string table, string wantedValue)
        {
            string query = String.Format("select {0} from {1} where {2}",
                                      wantedValue, table, whereClause);
            MySqlConnection dbcon = GetLockedConnection ();
            return Query (query, new Dictionary<string, object> (), dbcon);
        }

        public override List<string> Query(string keyRow, object keyValue, string table, string wantedValue, string order)
        {
            IDbCommand result = null;
            IDataReader reader = null;
            List<string> retVal = new List<string>();
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
            using (MySqlConnection dbcon = GetLockedConnection())
            {
                try
                {
                    using (result = Query(query + order, new Dictionary<string, object>(), dbcon))
                    {
                        CheckConnection();
                        using (reader = result.ExecuteReader())
                        {

                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    Type r = reader[i].GetType();
                                    retVal.Add(r == typeof (DBNull) ? null : reader.GetString(i));
                                }
                            }
                            return retVal;
                        }

                    }
                }
                catch (Exception)
                {
                    return new List<string>();
                }
                finally
                {
                    try
                    {
                        if (reader != null)
                        {
                            reader.Close();
                            reader.Dispose();
                        }
                        if (result != null) result.Dispose();
                    }catch{}
                    CloseDatabase(dbcon);
                }
            }
        }

        public override List<string> Query (string[] keyRow, object[] keyValue, string table, string wantedValue)
        {
            IDbCommand result = null;
            IDataReader reader = null;
            List<string> retVal = new List<string> ();
            string query = String.Format ("select {0} from {1} where ",
                                      wantedValue, table);
            int i = 0;
            foreach (object value in keyValue)
            {
                query += String.Format ("{0} = '{1}' and ", keyRow[i], value);
                i++;
            }
            query = query.Remove (query.Length - 5);


            using (MySqlConnection dbcon = GetLockedConnection())
            {
                try
                {
                    using (result = Query(query, new Dictionary<string, object>(), dbcon))
                    {
                        CheckConnection();
                        using (reader = result.ExecuteReader())
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
                }
                catch
                {
                    return null;
                }
                finally
                {
                    try
                    {
                        if (reader != null)
                        {
                            reader.Close();
                            reader.Dispose();
                        }
                        if (result != null) result.Dispose();
                    }catch{}
                    CloseDatabase(dbcon);
                }
            }
        }

        public override Dictionary<string, List<string>> QueryNames (string[] keyRow, object[] keyValue, string table, string wantedValue)
        {
            IDbCommand result = null;
            IDataReader reader = null;
            Dictionary<string, List<string>> retVal = new Dictionary<string, List<string>> ();
            string query = String.Format ("select {0} from {1} where ",
                                      wantedValue, table);
            int i = 0;
            foreach (object value in keyValue)
            {
                query += String.Format ("{0} = '{1}' and ", keyRow[i], value);
                i++;
            }
            query = query.Remove (query.Length - 5);

            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                try
                {
                    using (result = Query(query, new Dictionary<string, object>(), dbcon))
                    {
                        CheckConnection();
                        using (reader = result.ExecuteReader())
                        {

                            while (reader.Read())
                            {
                                for (i = 0; i < reader.FieldCount; i++)
                                {
                                    Type r = reader[i].GetType();
                                    AddValueToList(ref retVal, reader.GetName(i),
                                                   r == typeof(DBNull) ? null : reader[i].ToString());
                                }
                            }
                            return retVal;

                        }
                    }
                }
                catch
                {
                    return null;
                }
                finally
                {
                    try
                    {
                        if (reader != null)
                        {
                            reader.Close();
                            reader.Dispose();
                        }
                        if (result != null) result.Dispose();
                    }catch{}
                    CloseDatabase(dbcon);
                }
            }
        }

        private void AddValueToList (ref Dictionary<string, List<string>> dic, string key, string value)
        {
            if (!dic.ContainsKey (key))
                dic.Add (key, new List<string> ());

            dic[key].Add (value);
        }

        public override bool Update(string table, object[] setValues, string[] setRows, string[] keyRows, object[] keyValues)
        {
            IDbCommand result = null;
            IDataReader reader = null;
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
            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                try
                {
                    using (result = Query (query, parameters, dbcon))
                    {
                        CheckConnection ();
                        reader = result.ExecuteReader();
                    }
                }
                catch (MySqlException)
                {
                }
                finally
                {
                    try
                    {
                        if (reader != null)
                        {
                            reader.Close();
                            reader.Dispose();
                        }
                        if (result != null) result.Dispose();
                    }
                    catch{}
                    CloseDatabase(dbcon);
                }
            }
            return true;
        }

        public override bool Insert(string table, object[] values)
        {
            IDbCommand result = null;
            IDataReader reader = null;

            string query = String.Format("insert into {0} values (", table);
            query = values.Aggregate(query, (current, value) => current + String.Format("'{0}',", value));
            query = query.Remove(query.Length - 1);
            query += ")";

            using (MySqlConnection dbcon = GetLockedConnection())
            {
                try
                {
                    using (result = Query(query, new Dictionary<string, object>(), dbcon))
                    {
                        CheckConnection();
                        reader = result.ExecuteReader();
                    }
                }
                catch
                {

                }
                finally
                {
                    try
                    {
                        if (reader != null)
                        {
                            reader.Close();
                            reader.Dispose();
                        }
                        if (result != null) result.Dispose();
                    }catch{}
                    CloseDatabase(dbcon);
                }
            }
            return true;
        }

        public override bool Insert(string table, string[] keys, object[] values)
        {
            IDbCommand result;

            string query = String.Format("insert into {0} (", table);
            Dictionary<string, object> param = new Dictionary<string, object>();

            int i = 0;
            foreach (string key in keys)
            {
                param.Add("?" + key, values[i]);
                query += String.Format("{0},", key);
                i++;
            }
            query = query.Remove(query.Length - 1);
            query += ") values (";

            query = keys.Aggregate(query, (current, key) => current + String.Format("?{0},", key));
            query = query.Remove(query.Length - 1);
            query += ")";

            using (MySqlConnection dbcon = GetLockedConnection())
            {
                try
                {
                    using (result = Query(query, param, dbcon))
                    {
                        CheckConnection();
                        result.ExecuteReader();
                    }
                    result.Dispose();
                }
                catch
                {
                }
                finally
                {
                    CloseDatabase(dbcon);
                }
            }
            return true;
        }

        public override bool Replace(string table, string[] keys, object[] values)
        {
            IDbCommand result;
            
            string query = String.Format("replace into {0} (", table);
            Dictionary<string, object> param = new Dictionary<string, object>();

            int i = 0;
            foreach (string key in keys)
            {
                string kkey = key;
                if (key.Contains('`'))
                    kkey = key.Replace("`", ""); //Remove them

                param.Add("?" + kkey, values[i].ToString());
                query += "`" + kkey + "`" + ",";
                i++;
            }
            query = query.Remove(query.Length - 1);
            query += ") values (";

            foreach (string key in keys)
            {
                string kkey = key;
                if (key.Contains('`'))
                    kkey = key.Replace("`", ""); //Remove them
                query += String.Format("?{0},", kkey);
            }
            query = query.Remove(query.Length - 1);
            query += ")";

            using (MySqlConnection dbcon = GetLockedConnection())
            {
                try
                {
                    using (result = Query(query, param, dbcon))
                    {
                        CheckConnection();
                        result.ExecuteReader();
                    }
                    result.Dispose();
                }
                catch
                {
                    return false;
                }
                finally
                {
                    CloseDatabase(dbcon);
                }
            }
            return true;
        }

        public override bool DirectReplace(string table, string[] keys, object[] values)
        {
            IDbCommand result;
            
            string query = String.Format("replace into {0} (", table);
            Dictionary<string, object> param = new Dictionary<string, object>();

            foreach (string key in keys)
            {
                string kkey = key;
                if (key.Contains('`'))
                    kkey = key.Replace("`", ""); //Remove them

                query += "`" + kkey + "`" + ",";
            }
            query = query.Remove(query.Length - 1);
            query += ") values (";

            query = values.Aggregate(query, (current, key) => current + String.Format("{0},", key.ToString()));
            query = query.Remove(query.Length - 1);
            query += ")";

            using (MySqlConnection dbcon = GetLockedConnection())
            {
                try
                {
                    using (result = Query(query, param, dbcon))
                    {
                        result.ExecuteReader();
                    }
                    result.Dispose();
                }
                catch
                {
                    return false;
                }
                finally
                {
                    CloseDatabase(dbcon);
                }
            }
            return true;
        }

        public override bool Insert(string table, object[] values, string updateKey, object updateValue)
        {
            IDbCommand result;
            string query = String.Format("insert into {0} VALUES('", table);
            query = values.Aggregate(query, (current, value) => current + (value + "','"));
            query = query.Remove(query.Length - 2);
            query += String.Format(") ON DUPLICATE KEY UPDATE {0} = '{1}'", updateKey, updateValue);
            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                try
                {
                    using (result = Query(query, new Dictionary<string, object>(), dbcon))
                    {
                        CheckConnection();
                        result.ExecuteReader();
                    }
                    result.Dispose();
                }
                catch
                {
                    return false;
                }
                finally
                {
                    CloseDatabase(dbcon);
                }
            }
            return true;
        }

        public override bool Delete(string table, string[] keys, object[] values)
        {
            IDbCommand result;
            string query = "delete from " + table + (keys.Length > 0 ? " WHERE " : "");
            int i = 0;
            foreach (object value in values)
            {
                query += keys[i] + " = '" + value + "' AND ";
                i++;
            }
            if(keys.Length > 0)
                query = query.Remove(query.Length - 5);
            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                try
                {
                    using (result = Query(query, new Dictionary<string, object>(), dbcon))
                    {
                        CheckConnection();
                        result.ExecuteReader();
                    }
                    result.Dispose();
                }
                catch
                {
                    return false;
                }
                finally
                {
                    CloseDatabase(dbcon);
                }
            }
            return true;
        }

        public override string FormatDateTimeString(int time)
        {
            if (time == 0)
                return "now()";
            return "date_add(now(), interval " + time + " minute)";
        }

        public override string IsNull(string field, string defaultValue)
        {
            return "IFNULL(" + field + "," + defaultValue + ")";
        }

        public override string ConCat(string[] toConcat)
        {
            string returnValue = toConcat.Aggregate("concat(", (current, s) => current + (s + ","));
            return returnValue.Substring(0, returnValue.Length - 1) + ")";
        }

        public override bool Delete(string table, string whereclause)
        {
            IDbCommand result;
            string query = "delete from " + table + " WHERE " + whereclause;
            using (MySqlConnection dbcon = GetLockedConnection())
            {
                try
                {
                    using (result = Query(query, new Dictionary<string, object>(), dbcon))
                    {
                        CheckConnection();
                        result.ExecuteReader();
                    }
                    result.Dispose();
                }
                catch
                {
                    return false;
                }
                finally
                {
                    CloseDatabase(dbcon);
                }
            }
            return true;
        }

        public override bool DeleteByTime(string table, string key)
        {
            IDbCommand result;
            string query = "delete from " + table + " WHERE '" + key + "' < now()";
            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                try
                {
                    using (result = Query(query, new Dictionary<string, object>(), dbcon))
                    {
                        CheckConnection();
                        result.ExecuteReader();
                    }
                    result.Dispose();
                }
                catch
                {
                    return false;
                }
                finally
                {
                    CloseDatabase(dbcon);
                }
            }
            return true;
        }

        public override void CreateTable(string table, ColumnDefinition[] columns)
        {
            table = table.ToLower();
            if (TableExists(table))
            {
                throw new DataManagerException("Trying to create a table with name of one that already exists.");
            }

            string columnDefinition = string.Empty;
            var primaryColumns = (from cd in columns where cd.IsPrimary select cd);
            bool multiplePrimary = primaryColumns.Count() > 1;

            foreach (ColumnDefinition column in columns)
            {
                if (columnDefinition != string.Empty)
                {
                    columnDefinition += ", ";
                }
                columnDefinition += "`" + column.Name + "` " + GetColumnTypeStringSymbol(column.Type) + ((column.IsPrimary && !multiplePrimary) ? " PRIMARY KEY" : string.Empty);
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
                    listOfPrimaryNamesString += "`" + column.Name + "`";
                }
                multiplePrimaryString = string.Format(", PRIMARY KEY ({0}) ", listOfPrimaryNamesString);
            }

            string query = string.Format("create table " + table + " ( {0} {1}) ", columnDefinition, multiplePrimaryString);

            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                try
                {
                    using (MySqlCommand dbcommand = dbcon.CreateCommand())
                    {
                        CheckConnection();
                        dbcommand.CommandText = query;
                        dbcommand.ExecuteNonQuery();
                    }
                }
                catch
                {

                }
                finally
                {
                    CloseDatabase(dbcon);
                }
            }
        }

        public override void UpdateTable(string table, ColumnDefinition[] columns)
        {
            table = table.ToLower();
            if (!TableExists(table))
            {
                throw new DataManagerException("Trying to update a table with name of one that does not exist.");
            }

            List<ColumnDefinition> oldColumns = ExtractColumnsFromTable(table);

            Dictionary<string, ColumnDefinition> removedColumns = new Dictionary<string, ColumnDefinition>();
            Dictionary<string, ColumnDefinition> modifiedColumns = new Dictionary<string, ColumnDefinition>();
            Dictionary<string, ColumnDefinition> addedColumns = columns.Where(column => !oldColumns.Contains(column)).ToDictionary(column => column.Name);
            foreach (ColumnDefinition column in oldColumns.Where(column => !columns.Contains(column)))
            {
                if (addedColumns.ContainsKey(column.Name))
                {
                    modifiedColumns.Add(column.Name, addedColumns[column.Name]);
                    addedColumns.Remove(column.Name);
                }
                else
                    removedColumns.Add(column.Name, column);
            }

            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                try
                {
                    foreach (ColumnDefinition column in addedColumns.Values)
                    {
                        string addedColumnsQuery = "add " + column.Name + " " + GetColumnTypeStringSymbol(column.Type) + " ";
                        string query = string.Format("alter table " + table + " " + addedColumnsQuery);

                        MySqlCommand dbcommand = dbcon.CreateCommand();
                        dbcommand.CommandText = query;
                        try
                        {
                            CheckConnection();
                            dbcommand.ExecuteNonQuery();
                        }
                        catch
                        {
                        }
                    }
                    foreach (ColumnDefinition column in modifiedColumns.Values)
                    {
                        string modifiedColumnsQuery = "modify column " + column.Name + " " + GetColumnTypeStringSymbol(column.Type) + " ";
                        string query = string.Format("alter table " + table + " " + modifiedColumnsQuery);

                        MySqlCommand dbcommand = dbcon.CreateCommand();
                        dbcommand.CommandText = query;
                        try
                        {
                            CheckConnection();
                            dbcommand.ExecuteNonQuery();
                        }
                        catch
                        {
                        }
                    }
                    foreach (ColumnDefinition column in removedColumns.Values)
                    {
                        string droppedColumnsQuery = "drop " + column.Name + " ";
                        string query = string.Format("alter table " + table + " " + droppedColumnsQuery);

                        MySqlCommand dbcommand = dbcon.CreateCommand();
                        dbcommand.CommandText = query;
                        try
                        {
                            CheckConnection();
                            dbcommand.ExecuteNonQuery();
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {

                }
                finally
                {
                    CloseDatabase(dbcon);
                }
            }
        }

        public override string GetColumnTypeStringSymbol(ColumnTypes type)
        {
            switch (type)
            {
                case ColumnTypes.Integer11:
                    return "int(11)";
                case ColumnTypes.Integer30:
                    return "int(30)";
                case ColumnTypes.Char36:
                    return "char(36)";
                case ColumnTypes.Char32:
                    return "char(32)";
                case ColumnTypes.String:
                    return "TEXT";
                case ColumnTypes.String1:
                    return "VARCHAR(1)";
                case ColumnTypes.String2:
                    return "VARCHAR(2)";
                case ColumnTypes.String16:
                    return "VARCHAR(16)";
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
                case ColumnTypes.Blob:
                    return "blob";
                case ColumnTypes.LongBlob:
                    return "longblob";
                case ColumnTypes.Date:
                    return "DATE";
                case ColumnTypes.DateTime:
                    return "DATETIME";
                default:
                    throw new DataManagerException("Unknown column type.");
            }
        }

        public override void DropTable(string tableName)
        {
            tableName = tableName.ToLower();
            using (MySqlConnection dbcon = GetLockedConnection())
            {
                try
                {
                    using (MySqlCommand dbcommand = dbcon.CreateCommand())
                    {
                        CheckConnection();
                        dbcommand.CommandText = string.Format("drop table {0}", tableName);
                        dbcommand.ExecuteNonQuery();
                    }
                }
                catch
                {

                }
                finally
                {
                    CloseDatabase(dbcon);
                }
            }
        }

        public override void ForceRenameTable(string oldTableName, string newTableName)
        {
            newTableName = newTableName.ToLower();
            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                try
                {
                    using (MySqlCommand dbcommand = dbcon.CreateCommand())
                    {
                        CheckConnection();
                        dbcommand.CommandText = string.Format("RENAME TABLE {0} TO {1}", oldTableName, newTableName);
                        dbcommand.ExecuteNonQuery();
                    }
                }
                catch
                {
                    
                }
                finally
                {
                    CloseDatabase(dbcon);
                }
            }
        }

        protected override void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions)
        {
            sourceTableName = sourceTableName.ToLower();
            destinationTableName = destinationTableName.ToLower();
            using (MySqlConnection dbcon = GetLockedConnection())
            {
                try
                {
                    using (MySqlCommand dbcommand = dbcon.CreateCommand())
                    {
                        CheckConnection();
                        dbcommand.CommandText = string.Format("insert into {0} select * from {1}", destinationTableName,
                                                              sourceTableName);
                        dbcommand.ExecuteNonQuery();
                    }
                }
                catch
                {

                }
                finally
                {
                    CloseDatabase(dbcon);
                }
            }
        }

        public override bool TableExists(string table)
        {
            table = table.ToLower();
            var ret = false;
            IDbCommand result = null;
            IDataReader reader = null;
            List<string> retVal = new List<string>();
            using (MySqlConnection dbcon = GetLockedConnection())
            {
                try
                {
                    using (result = Query("show tables", new Dictionary<string, object>(), dbcon))
                    {
                        CheckConnection();
                        using (reader = result.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    retVal.Add(reader.GetString(i));
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    try
                    {
                        if (reader != null)
                        {
                            reader.Close();
                            reader.Dispose();
                        }
                        if (result != null)
                            result.Dispose();
                    }
                    catch{}
                    CloseDatabase(dbcon);
                }
                if (retVal.Contains(table))
                {
                    ret = true;
                }
            }
            return ret;
        }

        protected override List<ColumnDefinition> ExtractColumnsFromTable(string tableName)
        {
            var defs = new List<ColumnDefinition>();
            tableName = tableName.ToLower();
            MySqlDataReader rdr = null;
            MySqlCommand dbcommand = null;
            using (MySqlConnection dbcon = GetLockedConnection ())
            {
                try
                {
                    dbcommand = dbcon.CreateCommand();
                    dbcommand.CommandText = string.Format("desc {0}", tableName);
                    CheckConnection();
                    rdr = dbcommand.ExecuteReader();
                    while (rdr.Read())
                    {
                        var name = rdr["Field"];
                        var pk = rdr["Key"];
                        var type = rdr["Type"];
                        defs.Add(new ColumnDefinition { Name = name.ToString(), IsPrimary = pk.ToString() == "PRI", Type = ConvertTypeToColumnType(type.ToString()) });
                    }
                }
                catch
                {
                }
                finally
                {
                    try
                    {
                        if (rdr != null)
                        {
                            rdr.Close();
                            rdr.Dispose();
                        }
                        if (dbcommand != null) dbcommand.Dispose();
                    }catch{}
                    CloseDatabase(dbcon);
                }
                
                
            }
            return defs;
        }

        private ColumnTypes ConvertTypeToColumnType(string typeString)
        {
            string tStr = typeString.ToLower();
            //we'll base our names on lowercase
            switch (tStr)
            {
                case "int(11)":
                    return ColumnTypes.Integer11;
                case "int(30)":
                    return ColumnTypes.Integer30;
                case "integer":
                    return ColumnTypes.Integer11;
                case "char(36)":
                    return ColumnTypes.Char36;
                case "char(32)":
                    return ColumnTypes.Char32;
                case "varchar(1)":
                    return ColumnTypes.String1;
                case "varchar(2)":
                    return ColumnTypes.String2;
                case "varchar(16)":
                    return ColumnTypes.String16;
                case "varchar(32)":
                    return ColumnTypes.String32;
                case "varchar(36)":
                    return ColumnTypes.String36;
                case "varchar(45)":
                    return ColumnTypes.String45;
                case "varchar(50)":
                    return ColumnTypes.String50;
                case "varchar(64)":
                    return ColumnTypes.String64;
                case "varchar(128)":
                    return ColumnTypes.String128;
                case "varchar(100)":
                    return ColumnTypes.String100;
                case "varchar(255)":
                    return ColumnTypes.String255;
                case "varchar(512)":
                    return ColumnTypes.String512;
                case "varchar(1024)":
                    return ColumnTypes.String1024;
                case "date":
                    return ColumnTypes.Date;
                case "datetime":
                    return ColumnTypes.DateTime;
                case "varchar(8196)":
                    return ColumnTypes.String8196;
                case "text":
                    return ColumnTypes.Text;
                case "blob":
                    return ColumnTypes.Blob;
                case "longblob":
                    return ColumnTypes.LongBlob;
                case "smallint(6)":
                    return ColumnTypes.Integer11;
                case "int(10)":
                    return ColumnTypes.Integer11;
                case "tinyint(4)":
                    return ColumnTypes.Integer11;
            }
            if (tStr.StartsWith ("varchar"))
            {
                //... Someone was editing the database
                // Swallow the exception... but set it to the highest setting so we don't break anything
                return ColumnTypes.String8196;
            }
            if (tStr.StartsWith ("int"))
            {
                //... Someone was editing the database
                // Swallow the exception... but set it to the highest setting so we don't break anything
                return ColumnTypes.Integer11;
            }
            throw new Exception("You've discovered some type in MySQL that's not reconized by Aurora, please place the correct conversion in ConvertTypeToColumnType. Type: " + tStr);
        }

        public override IGenericData Copy()
        {
            return new MySQLDataLoader();
        }
    }
}

