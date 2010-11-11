using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using log4net;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;


namespace Aurora.Services.DataService
{
    public class RemoteGroupsServiceConnector : IGroupsServiceConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public void Initialize(IGenericData GenericData, IConfigSource source, string DefaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("GroupsConnector", "LocalConnector") == "RemoteConnector")
            {
                m_ServerURI = source.Configs["AuroraData"].GetString("RemoteServerURI", "");
                if (m_ServerURI != "")
                    DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IGroupsServiceConnector"; }
        }

        public void Dispose()
        {
        }

        public void CreateGroup(UUID groupID, string name, string charter, bool showInList, UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish, UUID founderID,
            ulong EveryonePowers, UUID OwnerRoleID, ulong OwnerPowers)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "CreateGroup";
            sendData["groupID"] = groupID;
            sendData["name"] = name;
            sendData["charter"] = charter;
            sendData["showInList"] = showInList;
            sendData["insigniaID"] = insigniaID;
            sendData["membershipFee"] = membershipFee;
            sendData["openEnrollment"] = openEnrollment;
            sendData["allowPublish"] = allowPublish;
            sendData["maturePublish"] = maturePublish;
            sendData["founderID"] = founderID;
            sendData["EveryonePowers"] = EveryonePowers;
            sendData["OwnerRoleID"] = OwnerRoleID;
            sendData["OwnerPowers"] = OwnerPowers;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void AddGroupNotice(UUID requestingAgentID, UUID groupID, UUID noticeID, string fromName, string subject, string message, UUID ItemID, int AssetType, string ItemName)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "AddGroupNotice";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["groupID"] = groupID;
            sendData["noticeID"] = noticeID;
            sendData["fromName"] = fromName;
            sendData["subject"] = subject;
            sendData["message"] = message;
            sendData["ItemID"] = ItemID;
            sendData["AssetType"] = AssetType;
            sendData["ItemName"] = ItemName;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void SetAgentActiveGroup(UUID AgentID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "SetAgentActiveGroup";
            sendData["AgentID"] = AgentID;
            sendData["GroupID"] = GroupID;
            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void SetAgentGroupSelectedRole(UUID AgentID, UUID GroupID, UUID RoleID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "SetAgentGroupSelectedRole";
            sendData["AgentID"] = AgentID;
            sendData["GroupID"] = GroupID;
            sendData["RoleID"] = RoleID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void AddAgentToGroup(UUID AgentID, UUID GroupID, UUID RoleID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "AddAgentToGroup";
            sendData["AgentID"] = AgentID;
            sendData["GroupID"] = GroupID;
            sendData["RoleID"] = RoleID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void AddRoleToGroup(UUID requestingAgentID, UUID GroupID, UUID RoleID, string Name, string Description, string Title, ulong Powers)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "AddRoleToGroup";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;
            sendData["RoleID"] = RoleID;
            sendData["Name"] = Name;
            sendData["Description"] = Description;
            sendData["Title"] = Title;
            sendData["Powers"] = Powers;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void UpdateGroup(UUID requestingAgentID, UUID groupID, string charter, int showInList, UUID insigniaID, int membershipFee, int openEnrollment, int allowPublish, int maturePublish)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "UpdateGroup";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["groupID"] = groupID;
            sendData["charter"] = charter;
            sendData["showInList"] = showInList;
            sendData["insigniaID"] = insigniaID;
            sendData["membershipFee"] = membershipFee;
            sendData["openEnrollment"] = openEnrollment;
            sendData["allowPublish"] = allowPublish;
            sendData["maturePublish"] = maturePublish;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void RemoveRoleFromGroup(UUID requestingAgentID, UUID RoleID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "RemoveRoleFromGroup";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["RoleID"] = RoleID;
            sendData["GroupID"] = GroupID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void UpdateRole(UUID requestingAgentID, UUID GroupID, UUID RoleID, string Name, string Desc, string Title, ulong Powers)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "UpdateRole";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;
            sendData["RoleID"] = RoleID;
            sendData["Name"] = Name;
            sendData["Desc"] = Desc;
            sendData["Title"] = Title;
            sendData["Powers"] = Powers;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void SetAgentGroupInfo(UUID requestingAgentID, UUID AgentID, UUID GroupID, int AcceptNotices, int ListInProfile)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "SetAgentGroupInfo";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["AgentID"] = AgentID;
            sendData["GroupID"] = GroupID;
            sendData["AcceptNotices"] = AcceptNotices;
            sendData["ListInProfile"] = ListInProfile;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void AddAgentGroupInvite(UUID requestingAgentID, UUID inviteID, UUID GroupID, UUID roleID, UUID AgentID, string FromAgentName)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "AddAgentGroupInvite";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["inviteID"] = inviteID;
            sendData["GroupID"] = GroupID;
            sendData["roleID"] = roleID;
            sendData["AgentID"] = AgentID;
            sendData["FromAgentName"] = FromAgentName;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void RemoveAgentInvite(UUID requestingAgentID, UUID inviteID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "RemoveAgentInvite";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["inviteID"] = inviteID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void AddAgentToRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "AddAgentToRole";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["AgentID"] = AgentID;
            sendData["GroupID"] = GroupID;
            sendData["RoleID"] = RoleID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void RemoveAgentFromRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "RemoveAgentFromRole";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["AgentID"] = AgentID;
            sendData["GroupID"] = GroupID;
            sendData["RoleID"] = RoleID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public GroupRecord GetGroupRecord(UUID requestingAgentID, UUID GroupID, string GroupName)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupRecord";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;
            sendData["GroupName"] = GroupName;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        GroupRecord group = null;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                group = new GroupRecord((Dictionary<string, object>)f);
                            }
                        }
                        // Success
                        return group;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public GroupProfileData GetMemberGroupProfile(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetMemberGroupProfile";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;
            sendData["AgentID"] = AgentID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        GroupProfileData group = null;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                group = new GroupProfileData((Dictionary<string, object>)f);
                            }
                        }
                        // Success
                        return group;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public GroupMembershipData GetGroupMembershipData(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupMembershipData";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;
            sendData["AgentID"] = AgentID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        GroupMembershipData group = null;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                group = new GroupMembershipData((Dictionary<string, object>)f);
                            }
                        }
                        // Success
                        return group;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public bool RemoveAgentFromGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "RemoveAgentFromGroup";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;
            sendData["AgentID"] = AgentID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        bool group = false;
                        foreach (object f in replyvalues)
                        {
                            if (f is bool)
                            {
                                group = Convert.ToBoolean(f);
                            }
                        }
                        // Success
                        return group;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return false;
        }

        public UUID GetAgentActiveGroup(UUID RequestingAgentID, UUID AgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetAgentActiveGroup";
            sendData["requestingAgentID"] = RequestingAgentID;
            sendData["AgentID"] = AgentID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        // Success
                        return UUID.Parse(replyData["A"].ToString());
                    }
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return UUID.Zero;
        }

        public GroupInviteInfo GetAgentToGroupInvite(UUID requestingAgentID, UUID inviteID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetAgentToGroupInvite";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["inviteID"] = inviteID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        GroupInviteInfo group = null;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                group = new GroupInviteInfo((Dictionary<string, object>)f);
                            }
                        }
                        // Success
                        return group;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public GroupMembersData GetAgentGroupMemberData(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetAgentGroupMemberData";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;
            sendData["AgentID"] = AgentID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        GroupMembersData group = null;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                group = new GroupMembersData((Dictionary<string, object>)f);
                            }
                        }
                        // Success
                        return group;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public GroupNoticeInfo GetGroupNotice(UUID requestingAgentID, UUID noticeID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupNotice";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["noticeID"] = noticeID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        GroupNoticeInfo group = null;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                group = new GroupNoticeInfo((Dictionary<string, object>)f);
                            }
                        }
                        // Success
                        return group;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public List<GroupMembershipData> GetAgentGroupMemberships(UUID requestingAgentID, UUID AgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetAgentGroupMemberships";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["AgentID"] = AgentID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        List<GroupMembershipData> Groups = new List<GroupMembershipData>();
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                Groups.Add(new GroupMembershipData((Dictionary<string, object>)f));
                            }
                        }
                        // Success
                        return Groups;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return new List<GroupMembershipData>();
        }

        public List<DirGroupsReplyData> FindGroups(UUID requestingAgentID, string search, int StartQuery, uint queryflags)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "FindGroups";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["search"] = search;
            sendData["StartQuery"] = StartQuery;
            sendData["queryflags"] = queryflags;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        List<DirGroupsReplyData> Groups = new List<DirGroupsReplyData>();
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                Groups.Add(new DirGroupsReplyData((Dictionary<string, object>)f));
                            }
                        }
                        // Success
                        return Groups;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return new List<DirGroupsReplyData>();
        }

        public List<GroupRolesData> GetAgentGroupRoles(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetAgentGroupRoles";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["AgentID"] = AgentID;
            sendData["GroupID"] = GroupID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        List<GroupRolesData> Groups = new List<GroupRolesData>();
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                Groups.Add(new GroupRolesData((Dictionary<string, object>)f));
                            }
                        }
                        // Success
                        return Groups;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return new List<GroupRolesData>();
        }

        public List<GroupRolesData> GetGroupRoles(UUID requestingAgentID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupRoles";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        List<GroupRolesData> Groups = new List<GroupRolesData>();
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                Groups.Add(new GroupRolesData((Dictionary<string, object>)f));
                            }
                        }
                        // Success
                        return Groups;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return new List<GroupRolesData>();
        }

        public List<GroupMembersData> GetGroupMembers(UUID requestingAgentID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupMembers";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        List<GroupMembersData> Groups = new List<GroupMembersData>();
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                Groups.Add(new GroupMembersData((Dictionary<string, object>)f));
                            }
                        }
                        // Success
                        return Groups;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return new List<GroupMembersData>();
        }

        public List<GroupRoleMembersData> GetGroupRoleMembers(UUID requestingAgentID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupRoleMembers";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        List<GroupRoleMembersData> Groups = new List<GroupRoleMembersData>();
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                Groups.Add(new GroupRoleMembersData((Dictionary<string, object>)f));
                            }
                        }
                        // Success
                        return Groups;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return new List<GroupRoleMembersData>();
        }

        public List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupNotices";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        List<GroupNoticeData> Groups = new List<GroupNoticeData>();
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                Groups.Add(new GroupNoticeData((Dictionary<string, object>)f));
                            }
                        }
                        // Success
                        return Groups;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return new List<GroupNoticeData>();
        }

        public List<GroupInviteInfo> GetGroupInvites(UUID requestingAgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupInvites";
            sendData["requestingAgentID"] = requestingAgentID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        List<GroupInviteInfo> Groups = new List<GroupInviteInfo>();
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                Groups.Add(new GroupInviteInfo((Dictionary<string, object>)f));
                            }
                        }
                        // Success
                        return Groups;
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return new List<GroupInviteInfo>();
        }
    }
}
