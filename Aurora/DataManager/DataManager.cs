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
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Aurora.Framework;
using OpenMetaverse;

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
        Date
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
    }
    public class GridRegionFlags
    {
        public bool IsIWCConnected;
    }
    public interface IGridRegionData
    {
        GridRegionFlags GetRegionFlags(UUID regionID);
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
        void CreateTable(string table,List<Rec<string, ColumnTypes>> columns);
        Version GetAuroraVersion();
        void WriteAuroraVersion(Version version);
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
