using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public interface IProfileData
    {
        List<string> ReadClassifiedInfoRow(string classifiedID);
        Dictionary<UUID, string> ReadClassifedRow(string creatoruuid);
        Dictionary<UUID, string> ReadPickRow(string creator);
        List<string> ReadInterestsInfoRow(string agentID);
        List<string> ReadPickInfoRow(string creator, string pickID);
        AuroraProfileData GetProfileNotes(UUID agentID, UUID target);
        void InvalidateProfileNotes(UUID target);
        void FullUpdateUserProfile(AuroraProfileData Profile);
        List<string> Query(string query);
        AuroraProfileData GetProfileInfo(UUID agentID);

        void UpdateUserProfile(AuroraProfileData Profile);

        AuroraProfileData CreateTemperaryAccount(string client, string first, string last);
        
        DirPlacesReplyData[] PlacesQuery(string queryText, string category, string table, string wantedValue);
        DirLandReplyData[] LandForSaleQuery(string searchType, string price, string area, string table, string wantedValue);
        DirClassifiedReplyData[] ClassifiedsQuery(string queryText, string category, string queryFlags);
        DirEventsReplyData[] EventQuery(string queryText, string flags, string table, string wantedValue);
        EventData GetEventInfo(string p);
    }
    public interface IRegionData
    {
        Dictionary<string, string> GetRegionHidden();
        string AbuseReports();
        ObjectMediaURLInfo[] getObjectMediaInfo(string objectID);
    }
    
    public interface IGenericData
    {
        string Identifier { get; }
        void ConnectToDatabase(string connectionString);
        /// <summary>
        /// select wantedValue from table where keyRow = keyValue
        /// </summary>
        List<string> Query(string keyRow, string keyValue, string table, string wantedValue);
        List<string> Query(string[] keyRow, string[] keyValue, string table, string wantedValue);
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
        void StoreRegionWindlightSettings(RegionLightShareData wl);
        RegionLightShareData LoadRegionWindlightSettings(UUID regionUUID);
    }

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
    public class ColumnDefinition
    {
        public string Name { get; set; }
        public ColumnTypes Type { get; set; }
        public bool IsPrimary { get; set; }

        public override bool Equals(object obj)
        {
            var cdef = obj as ColumnDefinition;
            if (cdef != null)
            {
                return cdef.Name == Name && cdef.Type == Type && cdef.IsPrimary == IsPrimary;
            }
            return false;
        }
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
}
