using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;
using Nini.Config;

namespace Aurora.Framework
{
    public interface IProfileData
    {
        DirPlacesReplyData[] PlacesQuery(string queryText, string category, string table, string wantedValue, int StartQuery);
        DirLandReplyData[] LandForSaleQuery(string searchType, string price, string area, string table, string wantedValue, int StartQuery);
        DirClassifiedReplyData[] ClassifiedsQuery(string queryText, string category, string queryFlags, int StartQuery);
        DirEventsReplyData[] EventQuery(string queryText, string flags, string table, string wantedValue, int StartQuery);
        EventData GetEventInfo(string p);
        DirEventsReplyData[] GetAllEventsNearXY(string table, int X, int Y);
        EventData[] GetEvents();
        Classified[] GetClassifieds();

    }
    public interface IRegionData
    {
        Dictionary<string, string> GetRegionHidden();
        string AbuseReports();
        ObjectMediaURLInfo getObjectMediaInfo(string objectID, int side);
        bool GetIsRegionMature(string region);
        bool StoreRegionWindlightSettings(RegionLightShareData wl);
        void AddLandObject(OpenSim.Framework.LandData ILandData);
        RegionLightShareData LoadRegionWindlightSettings(UUID regionUUID);

        AbuseReport GetAbuseReport(int formNumber);

        OfflineMessage[] GetOfflineMessages(string agentID);

        bool AddOfflineMessage(string fromUUID, string fromName, string toUUID, string message);
    }

    public interface IEstateData
    {
        EstateSettings LoadEstateSettings(UUID regionID, bool create);
        EstateSettings LoadEstateSettings(int estateID);
        bool StoreEstateSettings(EstateSettings es);
        List<int> GetEstates(string search);
        bool LinkRegion(UUID regionID, int estateID, string password);
        List<UUID> GetRegions(int estateID);
        bool DeleteEstate(int estateID);
    }

    public class OfflineMessage
    {
        public string FromUUID;
        public string ToUUID;
        public string FromName;
        public string Message;
    }

    public class AbuseReport
    {
        public string Category;
        public string Reporter;
        public string ObjectName;
        public string ObjectUUID;
        public string Abuser;
        public string Location;
        public string Details;
        public string Position;
        public string Estate;
        public string Summary;
        public string ReportNumber;
        public string AssignedTo;
        public string Active;
        public string Checked;
        public string Notes;
    }

    public interface IGroupsServicesConnector
    {
        UUID CreateGroup(UUID RequestingAgentID, string name, string charter, bool showInList, UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish, UUID founderID);
        void UpdateGroup(UUID RequestingAgentID, UUID groupID, string charter, bool showInList, UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish);
        GroupRecord GetGroupRecord(UUID RequestingAgentID, UUID GroupID, string GroupName);
        List<DirGroupsReplyData> FindGroups(UUID RequestingAgentID, string search);
        List<GroupMembersData> GetGroupMembers(UUID RequestingAgentID, UUID GroupID);

        void AddGroupRole(UUID RequestingAgentID, UUID groupID, UUID roleID, string name, string description, string title, ulong powers);
        void UpdateGroupRole(UUID RequestingAgentID, UUID groupID, UUID roleID, string name, string description, string title, ulong powers);
        void RemoveGroupRole(UUID RequestingAgentID, UUID groupID, UUID roleID);
        List<GroupRolesData> GetGroupRoles(UUID RequestingAgentID, UUID GroupID);
        List<GroupRoleMembersData> GetGroupRoleMembers(UUID RequestingAgentID, UUID GroupID);

        void AddAgentToGroup(UUID RequestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        void RemoveAgentFromGroup(UUID RequestingAgentID, UUID AgentID, UUID GroupID);

        void AddAgentToGroupInvite(UUID RequestingAgentID, UUID inviteID, UUID groupID, UUID roleID, UUID agentID);
        GroupInviteInfo GetAgentToGroupInvite(UUID RequestingAgentID, UUID inviteID);
        void RemoveAgentToGroupInvite(UUID RequestingAgentID, UUID inviteID);

        void AddAgentToGroupRole(UUID RequestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        void RemoveAgentFromGroupRole(UUID RequestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        List<GroupRolesData> GetAgentGroupRoles(UUID RequestingAgentID, UUID AgentID, UUID GroupID);

        void SetAgentActiveGroup(UUID RequestingAgentID, UUID AgentID, UUID GroupID);
        GroupMembershipData GetAgentActiveMembership(UUID RequestingAgentID, UUID AgentID);

        void SetAgentActiveGroupRole(UUID RequestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        void SetAgentGroupInfo(UUID RequestingAgentID, UUID AgentID, UUID GroupID, bool AcceptNotices, bool ListInProfile);

        GroupMembershipData GetAgentGroupMembership(UUID RequestingAgentID, UUID AgentID, UUID GroupID);
        List<GroupMembershipData> GetAgentGroupMemberships(UUID RequestingAgentID, UUID AgentID);

        void AddGroupNotice(UUID RequestingAgentID, UUID groupID, UUID noticeID, string fromName, string subject, string message, byte[] binaryBucket);
        GroupNoticeInfo GetGroupNotice(UUID RequestingAgentID, UUID noticeID);
        List<GroupNoticeData> GetGroupNotices(UUID RequestingAgentID, UUID GroupID);

        void ResetAgentGroupChatSessions(UUID agentID);
        bool hasAgentBeenInvitedToGroupChatSession(UUID agentID, UUID groupID);
        bool hasAgentDroppedGroupChatSession(UUID agentID, UUID groupID);
        void AgentDroppedFromGroupChatSession(UUID agentID, UUID groupID);
        void AgentInvitedToGroupChatSession(UUID agentID, UUID groupID);
    }

    public class GroupNoticeInfo
    {
        public GroupNoticeData noticeData = new GroupNoticeData();
        public UUID GroupID = UUID.Zero;
        public string Message = string.Empty;
        public byte[] BinaryBucket = new byte[0];
    }

    public class GroupInviteInfo
    {
        public UUID GroupID = UUID.Zero;
        public UUID RoleID = UUID.Zero;
        public UUID AgentID = UUID.Zero;
        public UUID InviteID = UUID.Zero;
    }
    
    public interface IGenericData
    {
        /// <summary>
        /// update table set setRow = setValue WHERE keyRow = keyValue
        /// </summary>
        bool Update(string table, object[] setValues, string[] setRows, string[] keyRows, object[] keyValues);
        /// <summary>
        /// select wantedValue from table where keyRow = keyValue
        /// </summary>
        List<string> Query(string keyRow, object keyValue, string table, string wantedValue);
        List<string> Query(string[] keyRow, object[] keyValue, string table, string wantedValue);
        bool Insert(string table, object[] values);
        bool Delete(string table, string[] keys, object[] values);
        bool Insert(string table, object[] values, string updateKey, object updateValue);
        string Identifier { get; }
    }

    public interface IDataConnector : IGenericData
    {
        void ConnectToDatabase(string connectionString);
        void CloseDatabase();
        bool TableExists(string table);
        void CreateTable(string table, ColumnDefinition[] columns);
        Version GetAuroraVersion();
        void WriteAuroraVersion(Version version);
        void CopyTableToTable(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions);
        bool VerifyTableExists(string tableName, ColumnDefinition[] columnDefinitions);
        void EnsureTableExists(string tableName, ColumnDefinition[] columnDefinitions);
        void DropTable(string tableName);
        string Identifier { get; }
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
        String8196,
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
    public interface IDataService
    {
        void Initialise(Nini.Config.IConfigSource source);
        IGenericData GetGenericPlugin();
        IEstateData GetEstatePlugin();
        IProfileData GetProfilePlugin();
        IRegionData GetRegionPlugin();
        void SetGenericDataPlugin(IGenericData Plugin);
        void SetEstatePlugin(IEstateData Plugin);
        void SetProfilePlugin(IProfileData Plugin);
        void SetRegionPlugin(IRegionData Plugin);
    }
}
