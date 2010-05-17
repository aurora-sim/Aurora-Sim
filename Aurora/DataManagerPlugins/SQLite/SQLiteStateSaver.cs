using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Data.SqliteClient;
using Aurora.Framework;
using OpenSim.Framework;
using OpenMetaverse;
using log4net;

namespace Aurora.DataManager.SQLite
{
    public class SQLiteStateSaver : DataManagerBase
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected List<string> m_ColumnNames;
        private SqliteConnection m_Connection;

        protected Dictionary<string, FieldInfo> m_Fields = new Dictionary<string, FieldInfo>();


        public override string Identifier
        {
            get { return "SQLiteStateSaver"; }
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
            try
            {
                var newConnection =
                    (SqliteConnection)((ICloneable)m_Connection).Clone();
                newConnection.Open();
                cmd.Connection = newConnection;
                SqliteDataReader reader = cmd.ExecuteReader();
                return reader;
            }
            catch (Mono.Data.SqliteClient.SqliteBusyException)
            {
                System.Threading.Thread.Sleep(5);
                return ExecuteReader(cmd);
            }
            catch (Exception ex)
            {
                //m_log.Warn("[SQLiteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " + ex);
                throw ex;
            }
        }

        protected int ExecuteNonQuery(SqliteCommand cmd)
        {
            try
            {
                lock (m_Connection)
                {
                    var newConnection =
                        (SqliteConnection)((ICloneable)m_Connection).Clone();
                    newConnection.Open();
                    cmd.Connection = newConnection;

                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Mono.Data.SqliteClient.SqliteBusyException)
            {
                System.Threading.Thread.Sleep(5);
                return ExecuteNonQuery(cmd);
            }
            catch (Exception ex)
            {
                //m_log.Warn("[SQLiteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " + ex);
                throw ex;
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

        public List<string> Query(string query)
        {
            SqliteCommand cmd = new SqliteCommand();
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

            CloseReaderCommand(cmd);
            return RetVal;
        }

        public List<string> Query(SqliteCommand cmd)
        {
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

            CloseReaderCommand(cmd);
            return RetVal;
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
            if (RetVal.Count == 0)
                RetVal.Add("");
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);

            return RetVal;
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

        public override List<string> Query(string[] keyRow, object[] keyValue, string table, string wantedValue)
        {
            var cmd = new SqliteCommand();
            string query = String.Format("select {0} from {1} where ",
                                      wantedValue, table);
            int i = 0;
            foreach (object value in keyValue)
            {
                query += keyRow[i] + " = '" + value.ToString() + "' and ";
                i++;
            }
            query = query.Remove(query.Length - 4);
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            var RetVal = new List<string>();
            while (reader.Read())
            {
                for (i = 0; i < reader.FieldCount; i++)
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

        public override bool Insert(string table, object[] values)
        {
            var cmd = new SqliteCommand();

            string query = "";
            query = String.Format("insert into {0} values ('", table);
            foreach (object value in values)
            {
                query += value.ToString() + "','";
            }
            query = query.Remove(query.Length - 2);
            query += ")";
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override bool Delete(string table, string[] keys, object[] values)
        {
            var cmd = new SqliteCommand();

            string query = String.Format("delete from '{0}' where ", table);
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
            query = String.Format("insert into '{0}' values ('", table);
            foreach (object value in values)
            {
                query = String.Format(query + "{0}','", value.ToString());
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
            string query = "update " + table + " set ";
            int i = 0;
            foreach (object value in setValues)
            {
                query += setRows[i] + " = '" + value.ToString() + "',";
                i++;
            }
            i = 0;
            query = query.Remove(query.Length - 1);
            query += " where ";
            foreach (object value in keyValues)
            {
                query += keyRows[i] + " = '" + value.ToString() + "' and";
                i++;
            }
            query = query.Remove(query.Length - 4);
            var cmd = new SqliteCommand();
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
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
                case ColumnTypes.String8196:
                    return "VARCHAR(8196)";
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
                case "varchar(8196)":
                    return ColumnTypes.String8196;
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
        
        public override RegionLightShareData LoadRegionWindlightSettings(UUID regionUUID)
        {
        	RegionLightShareData nWP = new RegionLightShareData();
            nWP.OnSave += UpdateRegionWindlightSettings;

        	string command = "select * from `regionwindlight` where region_id = "+regionUUID.ToString();

        	SqliteCommand cmd = new SqliteCommand();
        	cmd.CommandText = command;
        	IDataReader result = GetReader(cmd);
        	List<string> retval = new List<string>();

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
        	return nWP;
        }

        public override bool StoreRegionWindlightSettings(RegionLightShareData wl)
        {
            var cmd = new SqliteCommand();
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
            cmd.CommandText += "`cloud_scroll_y_lock`, `draw_classic_clouds`) VALUES ('" + wl.regionID.ToString() + "', '" + wl.waterColor.X + "', ";
            cmd.CommandText += "'" + wl.waterColor.Y + "', '" + wl.waterColor.Z + "', '" + wl.waterFogDensityExponent + "', '" + wl.underwaterFogModifier + "', '" + wl.reflectionWaveletScale.X + "', ";
            cmd.CommandText += "'" + wl.reflectionWaveletScale.Y + "', '" + wl.reflectionWaveletScale.X + "', '" + wl.fresnelScale + "', '" + wl.fresnelOffset + "', '" + wl.refractScaleAbove + "', ";
            cmd.CommandText += "'" + wl.refractScaleBelow + "', '" + wl.blurMultiplier + "', '" + wl.bigWaveDirection.X + "', '" + wl.bigWaveDirection.Y + "', '" + wl.littleWaveDirection.X + "', ";
            cmd.CommandText += "'" + wl.littleWaveDirection.Y + "', '" + wl.normalMapTexture + "', '" + wl.horizon.X + "', '" + wl.horizon.Y + "', '" + wl.horizon.Z + "', '" + wl.horizon.W + "', '" + wl.hazeHorizon + "', ";
            cmd.CommandText += "'" + wl.blueDensity.X + "', '" + wl.blueDensity.Y + "', '" + wl.blueDensity.Z + "', '" + wl.blueDensity.W + "', '" + wl.hazeDensity + "', '" + wl.densityMultiplier + "', ";
            cmd.CommandText += "'" + wl.distanceMultiplier + "', '" + wl.maxAltitude + "', '" + wl.sunMoonColor.X + "', '" + wl.sunMoonColor.Y + "', '" + wl.sunMoonColor.Z + "', ";
            cmd.CommandText += "'" + wl.sunMoonColor.W + "', '" + wl.sunMoonPosition + "', '" + wl.ambient.X + "', '" + wl.ambient.Y + "', '" + wl.ambient.Z + "', '" + wl.ambient.W + "', '" + wl.eastAngle + "', ";
            cmd.CommandText += "'" + wl.sunGlowFocus + "', '" + wl.sunGlowFocus + "', '" + wl.sceneGamma + "', '" + wl.starBrightness + "', '" + wl.cloudColor.X + "', '" + wl.cloudColor.Y + "', ";
            cmd.CommandText += "'" + wl.cloudColor.Z + "', '" + wl.cloudColor.W + "', '" + wl.cloudXYDensity.X + "', '" + wl.cloudXYDensity.Y + "', '" + wl.cloudXYDensity.Z + "', '" + wl.cloudCoverage + "', '" + wl.cloudScale + "', ";
            cmd.CommandText += "'" + wl.cloudDetailXYDensity.X + "', '" + wl.cloudDetailXYDensity.Y + "', '" + wl.cloudDetailXYDensity.Z + "', '" + wl.cloudScrollX + "', '" + wl.cloudScrollXLock + "', ";
            cmd.CommandText += "'" + wl.cloudScrollY + "', '" + wl.cloudScrollYLock + "', '" + wl.drawClassicClouds + "')";

            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);

            return true;
        }
        public void UpdateRegionWindlightSettings(RegionLightShareData wl)
        {
            var cmd = new SqliteCommand();
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
            cmd.CommandText += "`cloud_scroll_y_lock`, `draw_classic_clouds`) VALUES ('" + wl.regionID.ToString() + "', '" + wl.waterColor.X + "', ";
            cmd.CommandText += "'" + wl.waterColor.Y + "', '" + wl.waterColor.Z + "', '" + wl.waterFogDensityExponent + "', '" + wl.underwaterFogModifier + "', '" + wl.reflectionWaveletScale.X + "', ";
            cmd.CommandText += "'" + wl.reflectionWaveletScale.Y + "', '" + wl.reflectionWaveletScale.X + "', '" + wl.fresnelScale + "', '" + wl.fresnelOffset + "', '" + wl.refractScaleAbove + "', ";
            cmd.CommandText += "'" + wl.refractScaleBelow + "', '" + wl.blurMultiplier + "', '" + wl.bigWaveDirection.X + "', '" + wl.bigWaveDirection.Y + "', '" + wl.littleWaveDirection.X + "', ";
            cmd.CommandText += "'" + wl.littleWaveDirection.Y + "', '" + wl.normalMapTexture + "', '" + wl.horizon.X + "', '" + wl.horizon.Y + "', '" + wl.horizon.Z + "', '" + wl.horizon.W + "', '" + wl.hazeHorizon + "', ";
            cmd.CommandText += "'" + wl.blueDensity.X + "', '" + wl.blueDensity.Y + "', '" + wl.blueDensity.Z + "', '" + wl.blueDensity.W + "', '" + wl.hazeDensity + "', '" + wl.densityMultiplier + "', ";
            cmd.CommandText += "'" + wl.distanceMultiplier + "', '" + wl.maxAltitude + "', '" + wl.sunMoonColor.X + "', '" + wl.sunMoonColor.Y + "', '" + wl.sunMoonColor.Z + "', ";
            cmd.CommandText += "'" + wl.sunMoonColor.W + "', '" + wl.sunMoonPosition + "', '" + wl.ambient.X + "', '" + wl.ambient.Y + "', '" + wl.ambient.Z + "', '" + wl.ambient.W + "', '" + wl.eastAngle + "', ";
            cmd.CommandText += "'" + wl.sunGlowFocus + "', '" + wl.sunGlowFocus + "', '" + wl.sceneGamma + "', '" + wl.starBrightness + "', '" + wl.cloudColor.X + "', '" + wl.cloudColor.Y + "', ";
            cmd.CommandText += "'" + wl.cloudColor.Z + "', '" + wl.cloudColor.W + "', '" + wl.cloudXYDensity.X + "', '" + wl.cloudXYDensity.Y + "', '" + wl.cloudXYDensity.Z + "', '" + wl.cloudCoverage + "', '" + wl.cloudScale + "', ";
            cmd.CommandText += "'" + wl.cloudDetailXYDensity.X + "', '" + wl.cloudDetailXYDensity.Y + "', '" + wl.cloudDetailXYDensity.Z + "', '" + wl.cloudScrollX + "', '" + wl.cloudScrollXLock + "', ";
            cmd.CommandText += "'" + wl.cloudScrollY + "', '" + wl.cloudScrollYLock + "', '" + wl.drawClassicClouds + "')";

            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }
    }
}