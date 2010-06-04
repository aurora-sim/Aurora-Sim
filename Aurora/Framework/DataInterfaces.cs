using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;
using Nini.Config;

namespace Aurora.Framework
{
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
        List<string> Query(string whereClause, string table, string wantedValue);
        List<string> Query(string keyRow, object keyValue, string table, string wantedValue, string Order);
        List<string> Query(string[] keyRow, object[] keyValue, string table, string wantedValue);
        IDataReader QueryReader(string keyRow, object keyValue, string table, string wantedValue);
        bool Insert(string table, object[] values);
        bool Insert(string table, string[] keys, object[] values);
        bool Delete(string table, string[] keys, object[] values);
        bool Insert(string table, object[] values, string updateKey, object updateValue);
        string Identifier { get; }
        //REFACTORING ISSUE
        RegionLightShareData LoadRegionWindlightSettings(UUID regionUUID);
        bool StoreRegionWindlightSettings(RegionLightShareData wl);
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
        MySql,
        MSSQL2008,
        MSSQL7
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
        String100,
        Blob
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
}
