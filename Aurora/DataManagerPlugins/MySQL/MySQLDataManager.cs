using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using C5;
using MySql.Data.MySqlClient;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using OpenMetaverse;

namespace Aurora.DataManager.MySQL
{
    public class MySQLDataLoader : DataManagerBase
    {
        private string connectionString = "";
        private MySqlConnection m_connection = null;

        public override string Identifier
        {
            get { return "MySQLData"; }
        }

        public MySqlConnection GetLockedConnection()
        {
            if (m_connection == null)
            {
                m_connection = new MySqlConnection(connectionString);
                m_connection.Open();
                return m_connection;
            }
            else
            {
                MySqlConnection clone = m_connection.Clone();
                clone.Open();
                return clone;
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
            catch (Exception)
            {
                // Return null if it fails.
                return null;
            }
        }

        public override void ConnectToDatabase(string connectionstring)
        {
            connectionString = connectionstring;
            MySqlConnection dbcon = GetLockedConnection();
            CloseDatabase(dbcon);
        }

        public override List<string> Query(string keyRow, object keyValue, string table, string wantedValue)
        {
            MySqlConnection dbcon = GetLockedConnection();
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
            try
            {
                using (result = Query(query, new Dictionary<string, object>(), dbcon))
                {
                    using (reader = result.ExecuteReader())
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
                }
            }
            catch
            {
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

        public override List<string> Query(string whereClause, string table, string wantedValue)
        {
            MySqlConnection dbcon = GetLockedConnection();
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
            MySqlConnection dbcon = GetLockedConnection();
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
            MySqlConnection dbcon = GetLockedConnection();
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
            MySqlConnection dbcon = GetLockedConnection();
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
            MySqlConnection dbcon = GetLockedConnection();
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
            MySqlConnection dbcon = GetLockedConnection();
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
            MySqlConnection dbcon = GetLockedConnection();
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

        public void CloseDatabase(MySqlConnection connection)
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

            MySqlConnection dbcon = GetLockedConnection();
            MySqlCommand dbcommand = dbcon.CreateCommand();
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
                    return "blob";
                case ColumnTypes.Date:
                    return "DATE";
                default:
                    throw new DataManagerException("Unknown column type.");
            }
        }

        public override void DropTable(string tableName)
        {
            MySqlConnection dbcon = GetLockedConnection();
            MySqlCommand dbcommand = dbcon.CreateCommand();
            dbcommand.CommandText = string.Format("drop table {0}", tableName); ;
            dbcommand.ExecuteNonQuery();
            CloseDatabase(dbcon);
        }

        protected override void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions)
        {
            MySqlConnection dbcon = GetLockedConnection();
            MySqlCommand dbcommand = dbcon.CreateCommand();
            dbcommand.CommandText = string.Format("insert into {0} select * from {1}", destinationTableName, sourceTableName);
            dbcommand.ExecuteNonQuery();
            CloseDatabase(dbcon);
        }

        public override bool TableExists(string table)
        {
            MySqlConnection dbcon = GetLockedConnection();
            MySqlCommand dbcommand = dbcon.CreateCommand();
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


            MySqlConnection dbcon = GetLockedConnection();
            MySqlCommand dbcommand = dbcon.CreateCommand();
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
                case "varchar(8196)":
                    return ColumnTypes.String8196;
                case "blob":
                    return ColumnTypes.Blob;
                default:
                    throw new Exception("You've discovered some type in MySQL that's not reconized by Aurora, please place the correct conversion in ConvertTypeToColumnType.");
            }
        }
        public override RegionLightShareData LoadRegionWindlightSettings(UUID regionUUID)
        {
            IDbCommand reader = null;
            IDataReader result = null;
        	RegionLightShareData nWP = new RegionLightShareData();
            nWP.OnSave += UpdateRegionWindlightSettings;

        	string command = "select * from `regionwindlight` where region_id = " + regionUUID.ToString();
            MySqlConnection dbcon = GetLockedConnection();
            using (reader = Query(command, new Dictionary<string, object>(), dbcon))
        	{
                try
                {
                    using (result = reader.ExecuteReader())
                    {
                        if (!result.Read())
                        {
                            //No result, so store our default windlight profile and return it
                            nWP.regionID = regionUUID;
                            StoreRegionWindlightSettings(nWP);
                            return nWP;
                        }
                        else
                        {
                            UUID.TryParse(result["region_id"].ToString(), out nWP.regionID);
                            nWP.waterColor.X = Convert.ToSingle(result["water_color_r"]);
                            nWP.waterColor.Y = Convert.ToSingle(result["water_color_g"]);
                            nWP.waterColor.Z = Convert.ToSingle(result["water_color_b"]);
                            nWP.waterFogDensityExponent = Convert.ToSingle(result["water_fog_density_exponent"]);
                            nWP.underwaterFogModifier = Convert.ToSingle(result["underwater_fog_modifier"]);
                            nWP.reflectionWaveletScale.X = Convert.ToSingle(result["reflection_wavelet_scale_1"]);
                            nWP.reflectionWaveletScale.Y = Convert.ToSingle(result["reflection_wavelet_scale_2"]);
                            nWP.reflectionWaveletScale.Z = Convert.ToSingle(result["reflection_wavelet_scale_3"]);
                            nWP.fresnelScale = Convert.ToSingle(result["fresnel_scale"]);
                            nWP.fresnelOffset = Convert.ToSingle(result["fresnel_offset"]);
                            nWP.refractScaleAbove = Convert.ToSingle(result["refract_scale_above"]);
                            nWP.refractScaleBelow = Convert.ToSingle(result["refract_scale_below"]);
                            nWP.blurMultiplier = Convert.ToSingle(result["blur_multiplier"]);
                            nWP.bigWaveDirection.X = Convert.ToSingle(result["big_wave_direction_x"]);
                            nWP.bigWaveDirection.Y = Convert.ToSingle(result["big_wave_direction_y"]);
                            nWP.littleWaveDirection.X = Convert.ToSingle(result["little_wave_direction_x"]);
                            nWP.littleWaveDirection.Y = Convert.ToSingle(result["little_wave_direction_y"]);
                            UUID.TryParse(result["normal_map_texture"].ToString(), out nWP.normalMapTexture);
                            nWP.horizon.X = Convert.ToSingle(result["horizon_r"]);
                            nWP.horizon.Y = Convert.ToSingle(result["horizon_g"]);
                            nWP.horizon.Z = Convert.ToSingle(result["horizon_b"]);
                            nWP.horizon.W = Convert.ToSingle(result["horizon_i"]);
                            nWP.hazeHorizon = Convert.ToSingle(result["haze_horizon"]);
                            nWP.blueDensity.X = Convert.ToSingle(result["blue_density_r"]);
                            nWP.blueDensity.Y = Convert.ToSingle(result["blue_density_g"]);
                            nWP.blueDensity.Z = Convert.ToSingle(result["blue_density_b"]);
                            nWP.blueDensity.W = Convert.ToSingle(result["blue_density_i"]);
                            nWP.hazeDensity = Convert.ToSingle(result["haze_density"]);
                            nWP.densityMultiplier = Convert.ToSingle(result["density_multiplier"]);
                            nWP.distanceMultiplier = Convert.ToSingle(result["distance_multiplier"]);
                            nWP.maxAltitude = Convert.ToUInt16(result["max_altitude"]);
                            nWP.sunMoonColor.X = Convert.ToSingle(result["sun_moon_color_r"]);
                            nWP.sunMoonColor.Y = Convert.ToSingle(result["sun_moon_color_g"]);
                            nWP.sunMoonColor.Z = Convert.ToSingle(result["sun_moon_color_b"]);
                            nWP.sunMoonColor.W = Convert.ToSingle(result["sun_moon_color_i"]);
                            nWP.sunMoonPosition = Convert.ToSingle(result["sun_moon_position"]);
                            nWP.ambient.X = Convert.ToSingle(result["ambient_r"]);
                            nWP.ambient.Y = Convert.ToSingle(result["ambient_g"]);
                            nWP.ambient.Z = Convert.ToSingle(result["ambient_b"]);
                            nWP.ambient.W = Convert.ToSingle(result["ambient_i"]);
                            nWP.eastAngle = Convert.ToSingle(result["east_angle"]);
                            nWP.sunGlowFocus = Convert.ToSingle(result["sun_glow_focus"]);
                            nWP.sunGlowSize = Convert.ToSingle(result["sun_glow_size"]);
                            nWP.sceneGamma = Convert.ToSingle(result["scene_gamma"]);
                            nWP.starBrightness = Convert.ToSingle(result["star_brightness"]);
                            nWP.cloudColor.X = Convert.ToSingle(result["cloud_color_r"]);
                            nWP.cloudColor.Y = Convert.ToSingle(result["cloud_color_g"]);
                            nWP.cloudColor.Z = Convert.ToSingle(result["cloud_color_b"]);
                            nWP.cloudColor.W = Convert.ToSingle(result["cloud_color_i"]);
                            nWP.cloudXYDensity.X = Convert.ToSingle(result["cloud_x"]);
                            nWP.cloudXYDensity.Y = Convert.ToSingle(result["cloud_y"]);
                            nWP.cloudXYDensity.Z = Convert.ToSingle(result["cloud_density"]);
                            nWP.cloudCoverage = Convert.ToSingle(result["cloud_coverage"]);
                            nWP.cloudScale = Convert.ToSingle(result["cloud_scale"]);
                            nWP.cloudDetailXYDensity.X = Convert.ToSingle(result["cloud_detail_x"]);
                            nWP.cloudDetailXYDensity.Y = Convert.ToSingle(result["cloud_detail_y"]);
                            nWP.cloudDetailXYDensity.Z = Convert.ToSingle(result["cloud_detail_density"]);
                            nWP.cloudScrollX = Convert.ToSingle(result["cloud_scroll_x"]);
                            nWP.cloudScrollXLock = Convert.ToBoolean(result["cloud_scroll_x_lock"]);
                            nWP.cloudScrollY = Convert.ToSingle(result["cloud_scroll_y"]);
                            nWP.cloudScrollYLock = Convert.ToBoolean(result["cloud_scroll_y_lock"]);
                            nWP.drawClassicClouds = Convert.ToBoolean(result["draw_classic_clouds"]);
                        }
                    }
                }
                finally
                {
                    result.Close();
                    result.Dispose();
                    reader.Cancel();
                    reader.Dispose();
                    CloseDatabase(dbcon);
                }
        	}
        	
        	return nWP;
        }

        public override bool StoreRegionWindlightSettings(RegionLightShareData wl)
        {
            MySqlConnection dbcon = GetLockedConnection();
            using (MySqlCommand cmd = dbcon.CreateCommand())
            {
                cmd.CommandText = "REPLACE INTO `regionwindlight` (`region_id`, `water_color_r`, `water_color_g`, ";
                cmd.CommandText += "`water_color_b`, `water_fog_density_exponent`, `underwater_fog_modifier`, ";
                cmd.CommandText += "`reflection_wavelet_scale_1`, `reflection_wavelet_scale_2`, `reflection_wavelet_scale_3`, ";
                cmd.CommandText += "`fresnel_scale`, `fresnel_offset`, `refract_scale_above`, `refract_scale_below`, ";
                cmd.CommandText += "`blur_multiplier`, `big_wave_direction_x`, `big_wave_direction_y`, `little_wave_direction_x`, ";
                cmd.CommandText += "`little_wave_direction_y`, `normal_map_texture`, `horizon_r`, `horizon_g`, `horizon_b`, ";
                cmd.CommandText += "`horizon_i`, `haze_horizon`, `blue_density_r`, `blue_density_g`, `blue_density_b`, ";
                cmd.CommandText += "`blue_density_i`, `haze_density`, `density_multiplier`, `distance_multiplier`, `max_altitude`, ";
                cmd.CommandText += "`sun_moon_color_r`, `sun_moon_color_g`, `sun_moon_color_b`, `sun_moon_color_i`, `sun_moon_position`, ";
                cmd.CommandText += "`ambient_r`, `ambient_g`, `ambient_b`, `ambient_i`, `east_angle`, `sun_glow_focus`, `sun_glow_size`, ";
                cmd.CommandText += "`scene_gamma`, `star_brightness`, `cloud_color_r`, `cloud_color_g`, `cloud_color_b`, `cloud_color_i`, ";
                cmd.CommandText += "`cloud_x`, `cloud_y`, `cloud_density`, `cloud_coverage`, `cloud_scale`, `cloud_detail_x`, ";
                cmd.CommandText += "`cloud_detail_y`, `cloud_detail_density`, `cloud_scroll_x`, `cloud_scroll_x_lock`, `cloud_scroll_y`, ";
                cmd.CommandText += "`cloud_scroll_y_lock`, `draw_classic_clouds`) VALUES (?region_id, ?water_color_r, ";
                cmd.CommandText += "?water_color_g, ?water_color_b, ?water_fog_density_exponent, ?underwater_fog_modifier, ?reflection_wavelet_scale_1, ";
                cmd.CommandText += "?reflection_wavelet_scale_2, ?reflection_wavelet_scale_3, ?fresnel_scale, ?fresnel_offset, ?refract_scale_above, ";
                cmd.CommandText += "?refract_scale_below, ?blur_multiplier, ?big_wave_direction_x, ?big_wave_direction_y, ?little_wave_direction_x, ";
                cmd.CommandText += "?little_wave_direction_y, ?normal_map_texture, ?horizon_r, ?horizon_g, ?horizon_b, ?horizon_i, ?haze_horizon, ";
                cmd.CommandText += "?blue_density_r, ?blue_density_g, ?blue_density_b, ?blue_density_i, ?haze_density, ?density_multiplier, ";
                cmd.CommandText += "?distance_multiplier, ?max_altitude, ?sun_moon_color_r, ?sun_moon_color_g, ?sun_moon_color_b, ";
                cmd.CommandText += "?sun_moon_color_i, ?sun_moon_position, ?ambient_r, ?ambient_g, ?ambient_b, ?ambient_i, ?east_angle, ";
                cmd.CommandText += "?sun_glow_focus, ?sun_glow_size, ?scene_gamma, ?star_brightness, ?cloud_color_r, ?cloud_color_g, ";
                cmd.CommandText += "?cloud_color_b, ?cloud_color_i, ?cloud_x, ?cloud_y, ?cloud_density, ?cloud_coverage, ?cloud_scale, ";
                cmd.CommandText += "?cloud_detail_x, ?cloud_detail_y, ?cloud_detail_density, ?cloud_scroll_x, ?cloud_scroll_x_lock, ";
                cmd.CommandText += "?cloud_scroll_y, ?cloud_scroll_y_lock, ?draw_classic_clouds)";

                cmd.Parameters.AddWithValue("region_id", wl.regionID);
                cmd.Parameters.AddWithValue("water_color_r", wl.waterColor.X);
                cmd.Parameters.AddWithValue("water_color_g", wl.waterColor.Y);
                cmd.Parameters.AddWithValue("water_color_b", wl.waterColor.Z);
                cmd.Parameters.AddWithValue("water_fog_density_exponent", wl.waterFogDensityExponent);
                cmd.Parameters.AddWithValue("underwater_fog_modifier", wl.underwaterFogModifier);
                cmd.Parameters.AddWithValue("reflection_wavelet_scale_1", wl.reflectionWaveletScale.X);
                cmd.Parameters.AddWithValue("reflection_wavelet_scale_2", wl.reflectionWaveletScale.Y);
                cmd.Parameters.AddWithValue("reflection_wavelet_scale_3", wl.reflectionWaveletScale.Z);
                cmd.Parameters.AddWithValue("fresnel_scale", wl.fresnelScale);
                cmd.Parameters.AddWithValue("fresnel_offset", wl.fresnelOffset);
                cmd.Parameters.AddWithValue("refract_scale_above", wl.refractScaleAbove);
                cmd.Parameters.AddWithValue("refract_scale_below", wl.refractScaleBelow);
                cmd.Parameters.AddWithValue("blur_multiplier", wl.blurMultiplier);
                cmd.Parameters.AddWithValue("big_wave_direction_x", wl.bigWaveDirection.X);
                cmd.Parameters.AddWithValue("big_wave_direction_y", wl.bigWaveDirection.Y);
                cmd.Parameters.AddWithValue("little_wave_direction_x", wl.littleWaveDirection.X);
                cmd.Parameters.AddWithValue("little_wave_direction_y", wl.littleWaveDirection.Y);
                cmd.Parameters.AddWithValue("normal_map_texture", wl.normalMapTexture);
                cmd.Parameters.AddWithValue("horizon_r", wl.horizon.X);
                cmd.Parameters.AddWithValue("horizon_g", wl.horizon.Y);
                cmd.Parameters.AddWithValue("horizon_b", wl.horizon.Z);
                cmd.Parameters.AddWithValue("horizon_i", wl.horizon.W);
                cmd.Parameters.AddWithValue("haze_horizon", wl.hazeHorizon);
                cmd.Parameters.AddWithValue("blue_density_r", wl.blueDensity.X);
                cmd.Parameters.AddWithValue("blue_density_g", wl.blueDensity.Y);
                cmd.Parameters.AddWithValue("blue_density_b", wl.blueDensity.Z);
                cmd.Parameters.AddWithValue("blue_density_i", wl.blueDensity.W);
                cmd.Parameters.AddWithValue("haze_density", wl.hazeDensity);
                cmd.Parameters.AddWithValue("density_multiplier", wl.densityMultiplier);
                cmd.Parameters.AddWithValue("distance_multiplier", wl.distanceMultiplier);
                cmd.Parameters.AddWithValue("max_altitude", wl.maxAltitude);
                cmd.Parameters.AddWithValue("sun_moon_color_r", wl.sunMoonColor.X);
                cmd.Parameters.AddWithValue("sun_moon_color_g", wl.sunMoonColor.Y);
                cmd.Parameters.AddWithValue("sun_moon_color_b", wl.sunMoonColor.Z);
                cmd.Parameters.AddWithValue("sun_moon_color_i", wl.sunMoonColor.W);
                cmd.Parameters.AddWithValue("sun_moon_position", wl.sunMoonPosition);
                cmd.Parameters.AddWithValue("ambient_r", wl.ambient.X);
                cmd.Parameters.AddWithValue("ambient_g", wl.ambient.Y);
                cmd.Parameters.AddWithValue("ambient_b", wl.ambient.Z);
                cmd.Parameters.AddWithValue("ambient_i", wl.ambient.W);
                cmd.Parameters.AddWithValue("east_angle", wl.eastAngle);
                cmd.Parameters.AddWithValue("sun_glow_focus", wl.sunGlowFocus);
                cmd.Parameters.AddWithValue("sun_glow_size", wl.sunGlowSize);
                cmd.Parameters.AddWithValue("scene_gamma", wl.sceneGamma);
                cmd.Parameters.AddWithValue("star_brightness", wl.starBrightness);
                cmd.Parameters.AddWithValue("cloud_color_r", wl.cloudColor.X);
                cmd.Parameters.AddWithValue("cloud_color_g", wl.cloudColor.Y);
                cmd.Parameters.AddWithValue("cloud_color_b", wl.cloudColor.Z);
                cmd.Parameters.AddWithValue("cloud_color_i", wl.cloudColor.W);
                cmd.Parameters.AddWithValue("cloud_x", wl.cloudXYDensity.X);
                cmd.Parameters.AddWithValue("cloud_y", wl.cloudXYDensity.Y);
                cmd.Parameters.AddWithValue("cloud_density", wl.cloudXYDensity.Z);
                cmd.Parameters.AddWithValue("cloud_coverage", wl.cloudCoverage);
                cmd.Parameters.AddWithValue("cloud_scale", wl.cloudScale);
                cmd.Parameters.AddWithValue("cloud_detail_x", wl.cloudDetailXYDensity.X);
                cmd.Parameters.AddWithValue("cloud_detail_y", wl.cloudDetailXYDensity.Y);
                cmd.Parameters.AddWithValue("cloud_detail_density", wl.cloudDetailXYDensity.Z);
                cmd.Parameters.AddWithValue("cloud_scroll_x", wl.cloudScrollX);
                cmd.Parameters.AddWithValue("cloud_scroll_x_lock", wl.cloudScrollXLock);
                cmd.Parameters.AddWithValue("cloud_scroll_y", wl.cloudScrollY);
                cmd.Parameters.AddWithValue("cloud_scroll_y_lock", wl.cloudScrollYLock);
                cmd.Parameters.AddWithValue("draw_classic_clouds", wl.drawClassicClouds);

                cmd.ExecuteNonQuery();
                cmd.Cancel();
                cmd.Dispose();
            }
            CloseDatabase(dbcon);
            return true;
        }
        public void UpdateRegionWindlightSettings(RegionLightShareData wl)
        {
            MySqlConnection dbcon = GetLockedConnection();
            using (MySqlCommand cmd = dbcon.CreateCommand())
            {
                cmd.CommandText = "REPLACE INTO `regionwindlight` (`region_id`, `water_color_r`, `water_color_g`, ";
                cmd.CommandText += "`water_color_b`, `water_fog_density_exponent`, `underwater_fog_modifier`, ";
                cmd.CommandText += "`reflection_wavelet_scale_1`, `reflection_wavelet_scale_2`, `reflection_wavelet_scale_3`, ";
                cmd.CommandText += "`fresnel_scale`, `fresnel_offset`, `refract_scale_above`, `refract_scale_below`, ";
                cmd.CommandText += "`blur_multiplier`, `big_wave_direction_x`, `big_wave_direction_y`, `little_wave_direction_x`, ";
                cmd.CommandText += "`little_wave_direction_y`, `normal_map_texture`, `horizon_r`, `horizon_g`, `horizon_b`, ";
                cmd.CommandText += "`horizon_i`, `haze_horizon`, `blue_density_r`, `blue_density_g`, `blue_density_b`, ";
                cmd.CommandText += "`blue_density_i`, `haze_density`, `density_multiplier`, `distance_multiplier`, `max_altitude`, ";
                cmd.CommandText += "`sun_moon_color_r`, `sun_moon_color_g`, `sun_moon_color_b`, `sun_moon_color_i`, `sun_moon_position`, ";
                cmd.CommandText += "`ambient_r`, `ambient_g`, `ambient_b`, `ambient_i`, `east_angle`, `sun_glow_focus`, `sun_glow_size`, ";
                cmd.CommandText += "`scene_gamma`, `star_brightness`, `cloud_color_r`, `cloud_color_g`, `cloud_color_b`, `cloud_color_i`, ";
                cmd.CommandText += "`cloud_x`, `cloud_y`, `cloud_density`, `cloud_coverage`, `cloud_scale`, `cloud_detail_x`, ";
                cmd.CommandText += "`cloud_detail_y`, `cloud_detail_density`, `cloud_scroll_x`, `cloud_scroll_x_lock`, `cloud_scroll_y`, ";
                cmd.CommandText += "`cloud_scroll_y_lock`, `draw_classic_clouds`) VALUES (?region_id, ?water_color_r, ";
                cmd.CommandText += "?water_color_g, ?water_color_b, ?water_fog_density_exponent, ?underwater_fog_modifier, ?reflection_wavelet_scale_1, ";
                cmd.CommandText += "?reflection_wavelet_scale_2, ?reflection_wavelet_scale_3, ?fresnel_scale, ?fresnel_offset, ?refract_scale_above, ";
                cmd.CommandText += "?refract_scale_below, ?blur_multiplier, ?big_wave_direction_x, ?big_wave_direction_y, ?little_wave_direction_x, ";
                cmd.CommandText += "?little_wave_direction_y, ?normal_map_texture, ?horizon_r, ?horizon_g, ?horizon_b, ?horizon_i, ?haze_horizon, ";
                cmd.CommandText += "?blue_density_r, ?blue_density_g, ?blue_density_b, ?blue_density_i, ?haze_density, ?density_multiplier, ";
                cmd.CommandText += "?distance_multiplier, ?max_altitude, ?sun_moon_color_r, ?sun_moon_color_g, ?sun_moon_color_b, ";
                cmd.CommandText += "?sun_moon_color_i, ?sun_moon_position, ?ambient_r, ?ambient_g, ?ambient_b, ?ambient_i, ?east_angle, ";
                cmd.CommandText += "?sun_glow_focus, ?sun_glow_size, ?scene_gamma, ?star_brightness, ?cloud_color_r, ?cloud_color_g, ";
                cmd.CommandText += "?cloud_color_b, ?cloud_color_i, ?cloud_x, ?cloud_y, ?cloud_density, ?cloud_coverage, ?cloud_scale, ";
                cmd.CommandText += "?cloud_detail_x, ?cloud_detail_y, ?cloud_detail_density, ?cloud_scroll_x, ?cloud_scroll_x_lock, ";
                cmd.CommandText += "?cloud_scroll_y, ?cloud_scroll_y_lock, ?draw_classic_clouds)";

                cmd.Parameters.AddWithValue("region_id", wl.regionID);
                cmd.Parameters.AddWithValue("water_color_r", wl.waterColor.X);
                cmd.Parameters.AddWithValue("water_color_g", wl.waterColor.Y);
                cmd.Parameters.AddWithValue("water_color_b", wl.waterColor.Z);
                cmd.Parameters.AddWithValue("water_fog_density_exponent", wl.waterFogDensityExponent);
                cmd.Parameters.AddWithValue("underwater_fog_modifier", wl.underwaterFogModifier);
                cmd.Parameters.AddWithValue("reflection_wavelet_scale_1", wl.reflectionWaveletScale.X);
                cmd.Parameters.AddWithValue("reflection_wavelet_scale_2", wl.reflectionWaveletScale.Y);
                cmd.Parameters.AddWithValue("reflection_wavelet_scale_3", wl.reflectionWaveletScale.Z);
                cmd.Parameters.AddWithValue("fresnel_scale", wl.fresnelScale);
                cmd.Parameters.AddWithValue("fresnel_offset", wl.fresnelOffset);
                cmd.Parameters.AddWithValue("refract_scale_above", wl.refractScaleAbove);
                cmd.Parameters.AddWithValue("refract_scale_below", wl.refractScaleBelow);
                cmd.Parameters.AddWithValue("blur_multiplier", wl.blurMultiplier);
                cmd.Parameters.AddWithValue("big_wave_direction_x", wl.bigWaveDirection.X);
                cmd.Parameters.AddWithValue("big_wave_direction_y", wl.bigWaveDirection.Y);
                cmd.Parameters.AddWithValue("little_wave_direction_x", wl.littleWaveDirection.X);
                cmd.Parameters.AddWithValue("little_wave_direction_y", wl.littleWaveDirection.Y);
                cmd.Parameters.AddWithValue("normal_map_texture", wl.normalMapTexture);
                cmd.Parameters.AddWithValue("horizon_r", wl.horizon.X);
                cmd.Parameters.AddWithValue("horizon_g", wl.horizon.Y);
                cmd.Parameters.AddWithValue("horizon_b", wl.horizon.Z);
                cmd.Parameters.AddWithValue("horizon_i", wl.horizon.W);
                cmd.Parameters.AddWithValue("haze_horizon", wl.hazeHorizon);
                cmd.Parameters.AddWithValue("blue_density_r", wl.blueDensity.X);
                cmd.Parameters.AddWithValue("blue_density_g", wl.blueDensity.Y);
                cmd.Parameters.AddWithValue("blue_density_b", wl.blueDensity.Z);
                cmd.Parameters.AddWithValue("blue_density_i", wl.blueDensity.W);
                cmd.Parameters.AddWithValue("haze_density", wl.hazeDensity);
                cmd.Parameters.AddWithValue("density_multiplier", wl.densityMultiplier);
                cmd.Parameters.AddWithValue("distance_multiplier", wl.distanceMultiplier);
                cmd.Parameters.AddWithValue("max_altitude", wl.maxAltitude);
                cmd.Parameters.AddWithValue("sun_moon_color_r", wl.sunMoonColor.X);
                cmd.Parameters.AddWithValue("sun_moon_color_g", wl.sunMoonColor.Y);
                cmd.Parameters.AddWithValue("sun_moon_color_b", wl.sunMoonColor.Z);
                cmd.Parameters.AddWithValue("sun_moon_color_i", wl.sunMoonColor.W);
                cmd.Parameters.AddWithValue("sun_moon_position", wl.sunMoonPosition);
                cmd.Parameters.AddWithValue("ambient_r", wl.ambient.X);
                cmd.Parameters.AddWithValue("ambient_g", wl.ambient.Y);
                cmd.Parameters.AddWithValue("ambient_b", wl.ambient.Z);
                cmd.Parameters.AddWithValue("ambient_i", wl.ambient.W);
                cmd.Parameters.AddWithValue("east_angle", wl.eastAngle);
                cmd.Parameters.AddWithValue("sun_glow_focus", wl.sunGlowFocus);
                cmd.Parameters.AddWithValue("sun_glow_size", wl.sunGlowSize);
                cmd.Parameters.AddWithValue("scene_gamma", wl.sceneGamma);
                cmd.Parameters.AddWithValue("star_brightness", wl.starBrightness);
                cmd.Parameters.AddWithValue("cloud_color_r", wl.cloudColor.X);
                cmd.Parameters.AddWithValue("cloud_color_g", wl.cloudColor.Y);
                cmd.Parameters.AddWithValue("cloud_color_b", wl.cloudColor.Z);
                cmd.Parameters.AddWithValue("cloud_color_i", wl.cloudColor.W);
                cmd.Parameters.AddWithValue("cloud_x", wl.cloudXYDensity.X);
                cmd.Parameters.AddWithValue("cloud_y", wl.cloudXYDensity.Y);
                cmd.Parameters.AddWithValue("cloud_density", wl.cloudXYDensity.Z);
                cmd.Parameters.AddWithValue("cloud_coverage", wl.cloudCoverage);
                cmd.Parameters.AddWithValue("cloud_scale", wl.cloudScale);
                cmd.Parameters.AddWithValue("cloud_detail_x", wl.cloudDetailXYDensity.X);
                cmd.Parameters.AddWithValue("cloud_detail_y", wl.cloudDetailXYDensity.Y);
                cmd.Parameters.AddWithValue("cloud_detail_density", wl.cloudDetailXYDensity.Z);
                cmd.Parameters.AddWithValue("cloud_scroll_x", wl.cloudScrollX);
                cmd.Parameters.AddWithValue("cloud_scroll_x_lock", wl.cloudScrollXLock);
                cmd.Parameters.AddWithValue("cloud_scroll_y", wl.cloudScrollY);
                cmd.Parameters.AddWithValue("cloud_scroll_y_lock", wl.cloudScrollYLock);
                cmd.Parameters.AddWithValue("draw_classic_clouds", wl.drawClassicClouds);

                cmd.ExecuteNonQuery();
                cmd.Cancel();
                cmd.Dispose();
                CloseDatabase(dbcon);
            }
        }
    }
}

