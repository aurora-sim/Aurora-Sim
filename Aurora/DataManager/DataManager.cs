using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using C5;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using Nini.Config;
using Aurora.Framework;
using OpenMetaverse;
using Settings = NHibernate.Cfg.Settings;
using Aurora.DataManager.DataModels;

namespace Aurora.DataManager
{
    public enum DataManagerTechnology
    {
        SQLite,
        MySql
    }

    public enum ColumnTypes
    {
        Integer,
        String,
        Date,
        String50,
        String512,
        String45,
        String1024,
        String2,
        String1,
        String100
    }

    #region Interfaces
    public interface IProfileData
    {
        List<string> ReadClassifiedInfoRow(string classifiedID);
        Dictionary<UUID, string> ReadClassifedRow(string creatoruuid);
        Dictionary<UUID, string> ReadPickRow(string creator);
        List<string> ReadInterestsInfoRow(string agentID);
        List<string> ReadPickInfoRow(string creator,string pickID);
        void InvalidateProfileNotes(UUID target);
        AuroraProfileData GetProfileNotes(UUID agentID, UUID target);
        List<string> Query(string query);
        AuroraProfileData GetProfileInfo(UUID agentID);

        void UpdateUserProfile(AuroraProfileData Profile);

        AuroraProfileData CreateTemperaryAccount(string client, string first, string last);
    }
    public interface IRegionData
    {
        Dictionary<string, string> GetRegionHidden();
        string AbuseReports();
        ObjectMediaURLInfo[] getObjectMediaInfo(string objectID);
    }
    public interface IInventoryData
    {
        bool AssignNewInventoryType(string name, int assetType);
        InventoryObjectType GetInventoryObjectTypeByType(int type);
        System.Collections.Generic.IList<InventoryObjectType> GetAllInventoryTypes();
    }
    public class ObjectMediaURLInfo
    {
        public string alt_image_enable = "";
        public bool auto_loop = true;
        public bool auto_play = true;
        public bool auto_scale = true;
        public bool auto_zoom = false;
        public int controls = 0;
        public string current_url = "http://www.google.com/";
        public bool first_click_interact = false;
        public int height_pixels = 0;
        public string home_url = "http://www.google.com/";
        public int perms_control = 7;
        public int perms_interact = 7;
        public string whitelist = "";
        public bool whitelist_enable = false;
        public int width_pixels = 0;
        public string object_media_version;
    }
    public class GridRegionFlags
    {
        public bool IsIWCConnected;
    }
    public interface IGridRegionData
    {
        GridRegionFlags GetRegionFlags(UUID regionID);
    }

    public class ColumnDefinition
    {
        public string Name { get; set; }
        public ColumnTypes Type { get; set; }
        public bool IsPrimary { get; set; }

        public override bool Equals(object obj)
        {
            var cdef = obj as ColumnDefinition;
            if( cdef != null )
            {
                return cdef.Name == Name && cdef.Type == Type && cdef.IsPrimary == IsPrimary;
            }
            return false;
        }
    }

    public interface IGenericData
    {
        string Identifier { get; }
        void ConnectToDatabase(string connectionString);
        /// <summary>
        /// select wantedValue from table where keyRow = keyValue
        /// </summary>
        List<string> Query(string keyRow, string keyValue, string table, string wantedValue);
        void Insert(string table, string[] values);
        void Delete(string table, string[] keys, string[] values);
        void Insert(string table, string[] values, string updateKey, string updateValue);
        /// <summary>
        /// update table set setRow = setValue WHERE keyRow = keyValue
        /// </summary>
        void Update(string table, string[] setValues, string[] setRows, string[] keyRows, string[] keyValues);
        void CloseDatabase();
        bool TableExists(string table);
        void CreateTable(string table, ColumnDefinition[] columns);
        Version GetAuroraVersion();
        void WriteAuroraVersion(Version version);
        void CopyTableToTable(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions);
        bool VerifyTableExists(string tableName, ColumnDefinition[] columnDefinitions);
        void EnsureTableExists(string tableName, ColumnDefinition[] columnDefinitions);
        void DropTable(string tableName);
    }
    #endregion
    public static class DataManager
    {
        private static IGenericData plugin = null;
        public static IGenericData GetGenericPlugin()
        {
            return plugin;
        }
        public static void SetGenericPlugin(IGenericData Plugin)
        {
            plugin = Plugin;
        }
        private static IProfileData profileplugin = null;
        public static IProfileData GetProfilePlugin()
        {
            return profileplugin;
        }
        public static void SetProfilePlugin(IProfileData Plugin)
        {
            profileplugin = Plugin;
        }
        private static IRegionData regionplugin = null;

        public static IRegionData GetRegionPlugin()
        {
            return regionplugin;
        }
        public static void SetRegionPlugin(IRegionData Plugin)
        {
            regionplugin = Plugin;
        }

        public static DataSessionProvider DataSessionProvider = new DataSessionProvider();
    }
}
