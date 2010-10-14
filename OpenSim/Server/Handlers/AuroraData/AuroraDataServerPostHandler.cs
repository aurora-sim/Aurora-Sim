using Nini.Config;
using log4net;
using System;
using System.Reflection;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;

namespace OpenSim.Server.Handlers.AuroraData
{
    public class AuroraDataServerPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private AgentInfoHandler AgentHandler = new AgentInfoHandler();
        private ProfileInfoHandler ProfileHandler = new ProfileInfoHandler();
        private TelehubInfoHandler TelehubHandler = new TelehubInfoHandler();
        private OfflineMessagesInfoHandler OfflineMessagesHandler = new OfflineMessagesInfoHandler();
        private EstateInfoHandler EstateHandler = new EstateInfoHandler();
        private MuteInfoHandler MuteHandler = new MuteInfoHandler();
        private DirectoryInfoHandler DirectoryHandler = new DirectoryInfoHandler();
        private GroupsServiceHandler GroupsHandler = new GroupsServiceHandler();

        public AuroraDataServerPostHandler() :
            base("POST", "/auroradata")
        {
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            string method = "";
            Dictionary<string, object> request = new Dictionary<string, object>();
            try
            {
                request = ServerUtils.ParseQueryString(body);
                //if (request.Count == 1)
                //    request = ServerUtils.ParseXmlResponse(body);
                object value = null;
                request.TryGetValue("<?xml version", out value);
                if (value != null)
                    request = ServerUtils.ParseXmlResponse(body);

                if (!request.ContainsKey("METHOD"))
                    return FailureResult();

                method = request["METHOD"].ToString();

                switch (method)
                {
                    #region Profile
                    case "getprofile":
                        return ProfileHandler.GetProfile(request);
                    case "updateprofile":
                        return ProfileHandler.UpdateProfile(request);
                    #endregion
                    #region Agents
                    case "getagent":
                        return AgentHandler.GetAgent(request);
                    case "updateagent":
                        return AgentHandler.UpdateAgent(request);
                    #endregion
                    #region Estates
                    case "loadestatesettings":
                        return EstateHandler.LoadEstateSettings(request);
                    case "saveestatesettings":
                        return EstateHandler.SaveEstateSettings(request);
                    case "linkregionestate":
                        return EstateHandler.LinkRegionEstate(request);
                    case "createestate":
                        return EstateHandler.CreateEstate(request);
                    case "deleteestate":
                        return EstateHandler.DeleteEstate(request);
                    case "getestates":
                        return EstateHandler.GetEstates(request);
                    #endregion
                    #region Mutes
                    case "getmutelist":
                        return MuteHandler.GetMuteList(request);
                    case "updatemute":
                        return MuteHandler.UpdateMute(request);
                    case "deletemute":
                        return MuteHandler.DeleteMute(request);
                    case "ismuted":
                        return MuteHandler.IsMuted(request);
                    #endregion
                    #region Offline Messages
                    case "addofflinemessage":
                        return OfflineMessagesHandler.AddOfflineMessage(request);
                    case "getofflinemessages":
                        return OfflineMessagesHandler.GetOfflineMessages(request);
                    #endregion
                    #region Search
                    case "addlandobject":
                        return DirectoryHandler.AddLandObject(request);
                    case "getparcelinfo":
                        return DirectoryHandler.GetParcelInfo(request);
                    case "getparcelbyowner":
                        return DirectoryHandler.GetParcelByOwner(request);
                    case "findland":
                        return DirectoryHandler.FindLand(request);
                    case "findlandforsale":
                        return DirectoryHandler.FindLandForSale(request);
                    case "findevents":
                        return DirectoryHandler.FindEvents(request);
                    case "findeventsinregion":
                        return DirectoryHandler.FindEventsInRegion(request);
                    case "findclassifieds":
                        return DirectoryHandler.FindClassifieds(request);
                    case "geteventinfo":
                        return DirectoryHandler.GetEventInfo(request);
                    case "findclassifiedsinregion":
                        return DirectoryHandler.FindClassifiedsInRegion(request);
                    #endregion
                    #region Groups
                    case "CreateGroup":
                        return GroupsHandler.CreateGroup(request);
                    case "AddGroupNotice":
                        return GroupsHandler.AddGroupNotice(request);
                    case "SetAgentActiveGroup":
                        return GroupsHandler.SetAgentActiveGroup(request);
                    case "SetAgentGroupSelectedRole":
                        return GroupsHandler.SetAgentGroupSelectedRole(request);
                    case "AddAgentToGroup":
                        return GroupsHandler.AddAgentToGroup(request);
                    case "AddRoleToGroup":
                        return GroupsHandler.AddRoleToGroup(request);
                    case "UpdateGroup":
                        return GroupsHandler.UpdateGroup(request);
                    case "RemoveRoleFromGroup":
                        return GroupsHandler.RemoveRoleFromGroup(request);
                    case "UpdateRole":
                        return GroupsHandler.UpdateRole(request);
                    case "SetAgentGroupInfo":
                        return GroupsHandler.SetAgentGroupInfo(request);
                    case "AddAgentGroupInvite":
                        return GroupsHandler.AddAgentGroupInvite(request);
                    case "RemoveAgentInvite":
                        return GroupsHandler.RemoveAgentInvite(request);
                    case "AddAgentToRole":
                        return GroupsHandler.AddAgentToRole(request);
                    case "RemoveAgentFromRole":
                        return GroupsHandler.RemoveAgentFromRole(request);
                    case "GetGroupRecord":
                        return GroupsHandler.GetGroupRecord(request);
                    case "GetMemberGroupProfile":
                        return GroupsHandler.GetMemberGroupProfile(request);
                    case "GetGroupMembershipData":
                        return GroupsHandler.GetGroupMembershipData(request);
                    case "RemoveAgentFromGroup":
                        return GroupsHandler.RemoveAgentFromGroup(request);
                    case "GetAgentActiveGroup":
                        return GroupsHandler.GetAgentActiveGroup(request);
                    case "GetAgentToGroupInvite":
                        return GroupsHandler.GetAgentToGroupInvite(request);
                    case "GetAgentGroupMemberData":
                        return GroupsHandler.GetAgentGroupMemberData(request);
                    case "GetGroupNotice":
                        return GroupsHandler.GetGroupNotice(request);
                    case "GetAgentGroupMemberships":
                        return GroupsHandler.GetAgentGroupMemberships(request);
                    case "FindGroups":
                        return GroupsHandler.FindGroups(request);
                    case "GetAgentGroupRoles":
                        return GroupsHandler.GetAgentGroupRoles(request);
                    case "GetGroupRoles":
                        return GroupsHandler.GetGroupRoles(request);
                    case "GetGroupMembers":
                        return GroupsHandler.GetGroupMembers(request);
                    case "GetGroupRoleMembers":
                        return GroupsHandler.GetGroupRoleMembers(request);
                    case "GetGroupNotices":
                        return GroupsHandler.GetGroupNotices(request);
                    case "GetGroupInvites":
                        return GroupsHandler.GetGroupInvites(request);
                    #endregion
                }
                m_log.DebugFormat("[AuroraDataServerPostHandler]: unknown method {0} request {1}", method.Length, method);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraDataServerPostHandler]: Exception {0} in " + method, e);
            }

            return FailureResult();

        }

        #region Misc

        private byte[] FailureResult()
        {
            return FailureResult(String.Empty);
        }

        private byte[] FailureResult(string msg)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Failure"));

            rootElement.AppendChild(result);

            XmlElement message = doc.CreateElement("", "Message", "");
            message.AppendChild(doc.CreateTextNode(msg));

            rootElement.AppendChild(message);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        #endregion
    }

    public class AgentInfoHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        IAgentConnector AgentConnector;
        public AgentInfoHandler()
        {
            AgentConnector = DataManager.RequestPlugin<IAgentConnector>();
        }

        public byte[] GetAgent(Dictionary<string, object> request)
        {
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get agent");

            IAgentInfo Agent = AgentConnector.GetAgent(principalID);
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (Agent == null)
                result["result"] = "null";
            else
                result["result"] = Agent.ToKeyValuePairs();

            string xmlString = ServerUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] UpdateAgent(Dictionary<string, object> request)
        {
            /*Dictionary<string, object> result = new Dictionary<string, object>();

            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
            {
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to update agent");
                result["result"] = "null";
                string FailedxmlString = ServerUtils.BuildXmlResponse(result);
                m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", FailedxmlString);
                UTF8Encoding Failedencoding = new UTF8Encoding();
                return Failedencoding.GetBytes(FailedxmlString);
            }

            IAgentInfo Agent = new IAgentInfo(request);
            AgentConnector.UpdateAgent(Agent);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);*/
            return null;
        }
    }
    public class GroupsServiceHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        IGroupsServiceConnector GroupsServiceConnector;
        public GroupsServiceHandler()
        {
            GroupsServiceConnector = DataManager.RequestPlugin<IGroupsServiceConnector>();
        }

        public byte[] CreateGroup(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID groupID = UUID.Parse(request["groupID"].ToString());
            string name = request["name"].ToString();
            string charter = request["charter"].ToString();
            bool showInList = bool.Parse(request["showInList"].ToString());
            UUID insigniaID = UUID.Parse(request["insigniaID"].ToString());
            int membershipFee = int.Parse(request["membershipFee"].ToString());
            bool openEnrollment = bool.Parse(request["openEnrollment"].ToString());
            bool allowPublish = bool.Parse(request["allowPublish"].ToString());
            bool maturePublish = bool.Parse(request["maturePublish"].ToString());
            UUID founderID = UUID.Parse(request["founderID"].ToString());
            ulong EveryonePowers = ulong.Parse(request["EveryonePowers"].ToString());
            UUID OwnerRoleID = UUID.Parse(request["OwnerRoleID"].ToString());
            ulong OwnerPowers = ulong.Parse(request["OwnerPowers"].ToString());

            GroupsServiceConnector.CreateGroup(groupID, name, charter, showInList,
                insigniaID, membershipFee, openEnrollment, allowPublish, maturePublish, founderID, EveryonePowers, OwnerRoleID, OwnerPowers);

            return SuccessResult();
        }

        public byte[] AddGroupNotice(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID groupID = UUID.Parse(request["groupID"].ToString());
            UUID noticeID = UUID.Parse(request["noticeID"].ToString());
            string fromName = request["fromName"].ToString();
            string subject = request["subject"].ToString();
            string message = request["message"].ToString();
            UUID ItemID = UUID.Parse(request["ItemID"].ToString());
            int AssetType = int.Parse(request["AssetType"].ToString());
            string ItemName = request["ItemName"].ToString();

            GroupsServiceConnector.AddGroupNotice(requestingAgentID, groupID, noticeID, fromName, subject,
                message, ItemID, AssetType, ItemName);

            return SuccessResult();
        }

        public byte[] SetAgentActiveGroup(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());

            GroupsServiceConnector.SetAgentActiveGroup(AgentID, GroupID);

            return SuccessResult();
        }

        public byte[] SetAgentGroupSelectedRole(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID RoleID = UUID.Parse(request["RoleID"].ToString());

            GroupsServiceConnector.SetAgentGroupSelectedRole(AgentID, GroupID, RoleID);

            return SuccessResult();
        }

        public byte[] AddAgentToGroup(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID RoleID = UUID.Parse(request["RoleID"].ToString());

            GroupsServiceConnector.AddAgentToGroup(AgentID, GroupID, RoleID);

            return SuccessResult();
        }

        public byte[] AddRoleToGroup(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID RoleID = UUID.Parse(request["RoleID"].ToString());
            string Name = request["Name"].ToString();
            string Description = request["Description"].ToString();
            string Title = request["Title"].ToString();
            ulong Powers = ulong.Parse(request["Powers"].ToString());

            GroupsServiceConnector.AddRoleToGroup(requestingAgentID, GroupID, RoleID, Name, Description, Title, Powers);

            return SuccessResult();
        }

        public byte[] UpdateGroup(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID groupID = UUID.Parse(request["groupID"].ToString());
            string charter = request["charter"].ToString();
            int showInList = int.Parse(request["showInList"].ToString());
            UUID insigniaID = UUID.Parse(request["insigniaID"].ToString());
            int membershipFee = int.Parse(request["membershipFee"].ToString());
            int openEnrollment = int.Parse(request["openEnrollment"].ToString());
            int allowPublish = int.Parse(request["allowPublish"].ToString());
            int maturePublish = int.Parse(request["maturePublish"].ToString());

            GroupsServiceConnector.UpdateGroup(requestingAgentID, groupID, charter, showInList, insigniaID, membershipFee, openEnrollment, allowPublish, maturePublish);

            return SuccessResult();
        }

        public byte[] RemoveRoleFromGroup(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID RoleID = UUID.Parse(request["RoleID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());

            GroupsServiceConnector.RemoveRoleFromGroup(requestingAgentID, RoleID, GroupID);

            return SuccessResult();
        }

        public byte[] UpdateRole(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID RoleID = UUID.Parse(request["RoleID"].ToString());
            string Name = request["Name"].ToString();
            string Desc = request["Desc"].ToString();
            string Title = request["Title"].ToString();
            ulong Powers = ulong.Parse(request["Powers"].ToString());

            GroupsServiceConnector.UpdateRole(requestingAgentID, GroupID, RoleID, Name, Desc, Title, Powers);

            return SuccessResult();
        }

        public byte[] SetAgentGroupInfo(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            int AcceptNotices = int.Parse(request["AcceptNotices"].ToString());
            int ListInProfile = int.Parse(request["ListInProfile"].ToString());

            GroupsServiceConnector.SetAgentGroupInfo(requestingAgentID, AgentID, GroupID, AcceptNotices, ListInProfile);

            return SuccessResult();
        }

        public byte[] AddAgentGroupInvite(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID inviteID = UUID.Parse(request["inviteID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID roleID = UUID.Parse(request["roleID"].ToString());
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            string FromAgentName = request["FromAgentName"].ToString();

            GroupsServiceConnector.AddAgentGroupInvite(requestingAgentID, inviteID, GroupID, roleID, AgentID, FromAgentName);

            return SuccessResult();
        }

        public byte[] RemoveAgentInvite(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID inviteID = UUID.Parse(request["inviteID"].ToString());

            GroupsServiceConnector.RemoveAgentInvite(requestingAgentID, inviteID);

            return SuccessResult();
        }

        public byte[] AddAgentToRole(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID RoleID = UUID.Parse(request["RoleID"].ToString());

            GroupsServiceConnector.AddAgentToRole(requestingAgentID, AgentID, GroupID, RoleID);

            return SuccessResult();
        }

        public byte[] RemoveAgentFromRole(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID RoleID = UUID.Parse(request["RoleID"].ToString());

            GroupsServiceConnector.RemoveAgentFromRole(requestingAgentID, AgentID, GroupID, RoleID);

            return SuccessResult();
        }

        public byte[] GetGroupRecord(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            string GroupName = "";
            if(request.ContainsKey("GroupName"))
                GroupName = request["GroupName"].ToString();

            GroupRecord r = GroupsServiceConnector.GetGroupRecord(requestingAgentID, GroupID, GroupName);
            if(r != null)
                result.Add("A", r.ToKeyValuePairs());

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetGroupMembershipData(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());

            GroupMembershipData r = GroupsServiceConnector.GetGroupMembershipData(requestingAgentID, GroupID, AgentID);
            if(r != null)
                result.Add("A", r.ToKeyValuePairs());

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetMemberGroupProfile(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());

            GroupProfileData r = GroupsServiceConnector.GetMemberGroupProfile(requestingAgentID, GroupID, AgentID);
            result.Add("A", r.ToKeyValuePairs());

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] RemoveAgentFromGroup(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());

            bool r = GroupsServiceConnector.RemoveAgentFromGroup(requestingAgentID, AgentID, GroupID);
            result.Add("A", r);

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetAgentActiveGroup(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());

            UUID r = GroupsServiceConnector.GetAgentActiveGroup(requestingAgentID, AgentID);
            result.Add("A", r);

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetAgentToGroupInvite(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID inviteID = UUID.Parse(request["inviteID"].ToString());

            GroupInviteInfo r = GroupsServiceConnector.GetAgentToGroupInvite(requestingAgentID, inviteID);
            result.Add("A", r.ToKeyValuePairs());

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetAgentGroupMemberData(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());

            GroupMembersData r = GroupsServiceConnector.GetAgentGroupMemberData(requestingAgentID, GroupID, AgentID);
            result.Add("A", r.ToKeyValuePairs());

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetGroupNotice(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID noticeID = UUID.Parse(request["noticeID"].ToString());

            GroupNoticeInfo r = GroupsServiceConnector.GetGroupNotice(requestingAgentID, noticeID);
            result.Add("A", r.ToKeyValuePairs());

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetAgentGroupMemberships(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());


            List<GroupMembershipData> rs = GroupsServiceConnector.GetAgentGroupMemberships(requestingAgentID, AgentID);
            int i = 0;
            foreach (GroupMembershipData r in rs)
            {
                result.Add(ConvertDecString(i), r.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] FindGroups(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            string search = request["search"].ToString();
            int StartQuery = int.Parse(request["StartQuery"].ToString());
            uint queryflags = uint.Parse(request["queryflags"].ToString());


            List<DirGroupsReplyData> rs = GroupsServiceConnector.FindGroups(requestingAgentID, search, StartQuery, queryflags);
            int i = 0;
            foreach (DirGroupsReplyData r in rs)
            {
                result.Add(ConvertDecString(i), r.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetAgentGroupRoles(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());


            List<GroupRolesData> rs = GroupsServiceConnector.GetAgentGroupRoles(requestingAgentID, AgentID, GroupID);
            int i = 0;
            foreach (GroupRolesData r in rs)
            {
                result.Add(ConvertDecString(i), r.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetGroupRoles(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());


            List<GroupRolesData> rs = GroupsServiceConnector.GetGroupRoles(requestingAgentID, GroupID);
            int i = 0;
            foreach (GroupRolesData r in rs)
            {
                result.Add(ConvertDecString(i), r.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetGroupMembers(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());


            List<GroupMembersData> rs = GroupsServiceConnector.GetGroupMembers(requestingAgentID, GroupID);
            int i = 0;
            foreach (GroupMembersData r in rs)
            {
                result.Add(ConvertDecString(i), r.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetGroupRoleMembers(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());


            List<GroupRoleMembersData> rs = GroupsServiceConnector.GetGroupRoleMembers(requestingAgentID, GroupID);
            int i = 0;
            foreach (GroupRoleMembersData r in rs)
            {
                result.Add(ConvertDecString(i), r.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetGroupNotices(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());


            List<GroupNoticeData> rs = GroupsServiceConnector.GetGroupNotices(requestingAgentID, GroupID);
            int i = 0;
            foreach (GroupNoticeData r in rs)
            {
                result.Add(ConvertDecString(i), r.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetGroupInvites(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());


            List<GroupInviteInfo> rs = GroupsServiceConnector.GetGroupInvites(requestingAgentID);
            int i = 0;
            foreach (GroupInviteInfo r in rs)
            {
                result.Add(ConvertDecString(i), r.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }


        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        // http://social.msdn.microsoft.com/forums/en-US/csharpgeneral/thread/68f7ca38-5cd1-411f-b8d4-e4f7a688bc03
        // By: A Million Lemmings
        public string ConvertDecString(int dvalue)
        {

            string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string retVal = string.Empty;

            double value = Convert.ToDouble(dvalue);

            do
            {

                double remainder = value - (26 * Math.Truncate(value / 26));

                retVal = retVal + CHARS.Substring((int)remainder, 1);

                value = Math.Truncate(value / 26);

            }
            while (value > 0);



            return retVal;

        }
    }

    public class DirectoryInfoHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        IDirectoryServiceConnector DirectoryServiceConnector;
        public DirectoryInfoHandler()
        {
            DirectoryServiceConnector = DataManager.RequestPlugin<IDirectoryServiceConnector>();
        }

        public byte[] GetParcelInfo(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID INFOUUID = UUID.Parse(request["INFOUUID"].ToString());
            LandData land = DirectoryServiceConnector.GetParcelInfo(INFOUUID);

            result.Add("Land", land.ToKeyValuePairs());

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetParcelByOwner(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID OWNERID = UUID.Parse(request["OWNERID"].ToString());
            LandData[] lands = DirectoryServiceConnector.GetParcelByOwner(OWNERID);

            int i = 0;
            foreach (LandData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] FindLand(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string QUERYTEXT = request["QUERYTEXT"].ToString();
            string CATEGORY = request["CATEGORY"].ToString();
            int STARTQUERY = int.Parse(request["STARTQUERY"].ToString());
            uint FLAGS = uint.Parse(request["FLAGS"].ToString());
            DirPlacesReplyData[] lands = DirectoryServiceConnector.FindLand(QUERYTEXT, CATEGORY, STARTQUERY, FLAGS);

            int i = 0;
            foreach (DirPlacesReplyData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] FindLandForSale(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string SEARCHTYPE = request["SEARCHTYPE"].ToString();
            int PRICE = int.Parse(request["PRICE"].ToString());
            int AREA = int.Parse(request["AREA"].ToString());
            int STARTQUERY = int.Parse(request["STARTQUERY"].ToString());
            uint FLAGS = uint.Parse(request["FLAGS"].ToString());
            DirLandReplyData[] lands = DirectoryServiceConnector.FindLandForSale(SEARCHTYPE, PRICE.ToString(), AREA.ToString(), STARTQUERY, FLAGS);

            int i = 0;
            foreach (DirLandReplyData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] FindEvents(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string QUERYTEXT = request["QUERYTEXT"].ToString();
            int FLAGS = int.Parse(request["FLAGS"].ToString());
            int STARTQUERY = int.Parse(request["STARTQUERY"].ToString());
            DirEventsReplyData[] lands = DirectoryServiceConnector.FindEvents(QUERYTEXT, FLAGS.ToString(), STARTQUERY);

            int i = 0;
            foreach (DirEventsReplyData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] FindEventsInRegion(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string RegionName = request["REGIONNAME"].ToString();
            DirEventsReplyData[] lands = DirectoryServiceConnector.FindAllEventsInRegion(RegionName);

            int i = 0;
            foreach (DirEventsReplyData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] FindClassifieds(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string QUERYTEXT = request["QUERYTEXT"].ToString();
            string CATEGORY = request["CATEGORY"].ToString();
            string QUERYFLAGS = request["QUERYFLAGS"].ToString();
            int STARTQUERY = int.Parse(request["STARTQUERY"].ToString());
            DirClassifiedReplyData[] lands = DirectoryServiceConnector.FindClassifieds(QUERYTEXT, CATEGORY, QUERYFLAGS, STARTQUERY);

            int i = 0;
            foreach (DirClassifiedReplyData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetEventInfo(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string EVENTID = request["EVENTID"].ToString();
            EventData eventdata = DirectoryServiceConnector.GetEventInfo(EVENTID);

            result.Add("event", eventdata.ToKeyValuePairs());

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] FindClassifiedsInRegion(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string RegionName = request["REGIONNAME"].ToString();
            Classified[] classifieds = DirectoryServiceConnector.GetClassifiedsInRegion(RegionName);

            int i = 0;
            foreach (Classified classified in classifieds)
            {
                result.Add(ConvertDecString(i), classified.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] AddLandObject(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            LandData land = new LandData();
            land.FromKVP(request);
            DirectoryServiceConnector.AddLandObject(land);

            return SuccessResult();
        }

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        // http://social.msdn.microsoft.com/forums/en-US/csharpgeneral/thread/68f7ca38-5cd1-411f-b8d4-e4f7a688bc03
        // By: A Million Lemmings
        public string ConvertDecString(int dvalue)
        {

            string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string retVal = string.Empty;

            double value = Convert.ToDouble(dvalue);

            do
            {

                double remainder = value - (26 * Math.Truncate(value / 26));

                retVal = retVal + CHARS.Substring((int)remainder, 1);

                value = Math.Truncate(value / 26);

            }
            while (value > 0);



            return retVal;

        }
    }

    public class EstateInfoHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        IEstateConnector EstateConnector;
        public EstateInfoHandler()
        {
            EstateConnector = DataManager.RequestPlugin<IEstateConnector>();
        }

        public byte[] GetEstates(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string search = request["SEARCH"].ToString();
            List<int> EstateIDs = EstateConnector.GetEstates(search);
            Dictionary<string, object> estateresult = new Dictionary<string, object>();
            int i = 0;
            foreach (int estateID in EstateIDs)
            {
                estateresult.Add(ConvertDecString(i), estateID);
                i++;
            }
            result["result"] = estateresult;

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] LinkRegionEstate(Dictionary<string, object> request)
        {
            int EstateID = int.Parse(request["ESTATEID"].ToString());
            string Password = request["PASSWORD"].ToString();
            UUID RegionID = new UUID(request["REGIONID"].ToString());
            if (EstateConnector.LinkRegion(RegionID, EstateID, Password))
                return SuccessResult();
            else
                return FailureResult();
        }

        public byte[] SaveEstateSettings(Dictionary<string, object> request)
        {
            EstateSettings ES = new EstateSettings(request);
            EstateConnector.SaveEstateSettings(ES);
            return SuccessResult();
        }

        public byte[] CreateEstate(Dictionary<string, object> request)
        {
            EstateSettings ES = new EstateSettings(request);

            UUID RegionID = new UUID(request["REGIONID"].ToString());

            ES = EstateConnector.CreateEstate(ES, RegionID);

            //This is not a local transfer, MUST be false!
            Dictionary<string, object> result = ES.ToKeyValuePairs(false);
            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] DeleteEstate(Dictionary<string, object> request)
        {
            int EstateID = int.Parse(request["ESTATEID"].ToString());
            string Password = request["PASSWORD"].ToString();

            if (EstateConnector.DeleteEstate(EstateID, Password))
                return SuccessResult();
            else
                return FailureResult();
        }

        public byte[] LoadEstateSettings(Dictionary<string, object> request)
        {
            //Warning! This services two different methods
            EstateSettings ES = null;
            if (request.ContainsKey("ESTATEID"))
            {
                int EstateID = int.Parse(request["ESTATEID"].ToString());
                ES = EstateConnector.LoadEstateSettings(EstateID);
            }
            else
            {
                string regionID = request["REGIONID"].ToString();
                if(regionID != null)
                    ES = EstateConnector.LoadEstateSettings(UUID.Parse(regionID));
            }

            //This NEEDS to be false here, otherwise passwords will be sent unsecurely!
            Dictionary<string, object> result = ES.ToKeyValuePairs(false);
            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }
        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] FailureResult()
        {
            return FailureResult(String.Empty);
        }

        private byte[] FailureResult(string msg)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Failure"));

            rootElement.AppendChild(result);

            XmlElement message = doc.CreateElement("", "Message", "");
            message.AppendChild(doc.CreateTextNode(msg));

            rootElement.AppendChild(message);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        // http://social.msdn.microsoft.com/forums/en-US/csharpgeneral/thread/68f7ca38-5cd1-411f-b8d4-e4f7a688bc03
        // By: A Million Lemmings
        public string ConvertDecString(int dvalue)
        {

            string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string retVal = string.Empty;

            double value = Convert.ToDouble(dvalue);

            do
            {

                double remainder = value - (26 * Math.Truncate(value / 26));

                retVal = retVal + CHARS.Substring((int)remainder, 1);

                value = Math.Truncate(value / 26);

            }
            while (value > 0);



            return retVal;

        }
    }

    public class MuteInfoHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        IMuteListConnector MuteListConnector;
        public MuteInfoHandler()
        {
            MuteListConnector = DataManager.RequestPlugin<IMuteListConnector>();
        }

        public byte[] GetMuteList(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID PRINCIPALID = UUID.Parse(request["PRINCIPALID"].ToString());
            MuteList[] Mutes = MuteListConnector.GetMuteList(PRINCIPALID);

            int i = 0;
            foreach (MuteList Mute in Mutes)
            {
                result.Add(ConvertDecString(i), Mute.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] UpdateMute(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            MuteList mute = new MuteList();
            mute.FromKVP(request);
            UUID PRINCIPALID = UUID.Parse(request["PRINCIPALID"].ToString());
            MuteListConnector.UpdateMute(mute, PRINCIPALID);

            return SuccessResult();
        }

        public byte[] DeleteMute(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID MUTEID = UUID.Parse(request["MUTEID"].ToString());
            UUID PRINCIPALID = UUID.Parse(request["PRINCIPALID"].ToString());
            MuteListConnector.DeleteMute(MUTEID, PRINCIPALID);

            return SuccessResult();
        }

        public byte[] IsMuted(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID MUTEID = UUID.Parse(request["MUTEID"].ToString());
            UUID PRINCIPALID = UUID.Parse(request["PRINCIPALID"].ToString());
            bool IsMuted = MuteListConnector.IsMuted(PRINCIPALID, MUTEID);
            result["Muted"] = IsMuted;

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }
        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        // http://social.msdn.microsoft.com/forums/en-US/csharpgeneral/thread/68f7ca38-5cd1-411f-b8d4-e4f7a688bc03
        // By: A Million Lemmings
        public string ConvertDecString(int dvalue)
        {

            string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string retVal = string.Empty;

            double value = Convert.ToDouble(dvalue);

            do
            {

                double remainder = value - (26 * Math.Truncate(value / 26));

                retVal = retVal + CHARS.Substring((int)remainder, 1);

                value = Math.Truncate(value / 26);

            }
            while (value > 0);



            return retVal;

        }
    }

    public class OfflineMessagesInfoHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        IOfflineMessagesConnector OfflineMessagesConnector;
        public OfflineMessagesInfoHandler()
        {
            OfflineMessagesConnector = DataManager.RequestPlugin<IOfflineMessagesConnector>();
        }

        public byte[] GetOfflineMessages(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID PRINCIPALID = UUID.Parse(request["PRINCIPALID"].ToString());
            GridInstantMessage[] Messages = OfflineMessagesConnector.GetOfflineMessages(PRINCIPALID);

            int i = 0;
            foreach (GridInstantMessage Message in Messages)
            {
                result.Add(ConvertDecString(i), Message.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] AddOfflineMessage(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            GridInstantMessage message = new GridInstantMessage();
            message.FromKVP(request);
            OfflineMessagesConnector.AddOfflineMessage(message);

            return SuccessResult();
        }

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        // http://social.msdn.microsoft.com/forums/en-US/csharpgeneral/thread/68f7ca38-5cd1-411f-b8d4-e4f7a688bc03
        // By: A Million Lemmings
        public string ConvertDecString(int dvalue)
        {

            string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string retVal = string.Empty;

            double value = Convert.ToDouble(dvalue);

            do
            {

                double remainder = value - (26 * Math.Truncate(value / 26));

                retVal = retVal + CHARS.Substring((int)remainder, 1);

                value = Math.Truncate(value / 26);

            }
            while (value > 0);



            return retVal;

        }
    }

    public class ProfileInfoHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        IProfileConnector ProfileConnector;
        public ProfileInfoHandler()
        {
            ProfileConnector = DataManager.RequestPlugin<IProfileConnector>();
        }

        public byte[] GetProfile(Dictionary<string, object> request)
        {
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");

            IUserProfileInfo UserProfile = ProfileConnector.GetUserProfile(principalID);
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (UserProfile == null)
                result["result"] = "null";
            else
            {
                result["result"] = UserProfile.ToKeyValuePairs();
            }
             
            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] UpdateProfile(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
            {
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");
                result["result"] = "null";
                string FailedxmlString = ServerUtils.BuildXmlResponse(result);
                m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", FailedxmlString);
                UTF8Encoding Failedencoding = new UTF8Encoding();
                return Failedencoding.GetBytes(FailedxmlString);
            }

            IUserProfileInfo UserProfile = new IUserProfileInfo(request);
            ProfileConnector.UpdateUserProfile(UserProfile);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }
    }

    public class TelehubInfoHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        IRegionConnector GridConnector;
        public TelehubInfoHandler()
        {
            GridConnector = DataManager.RequestPlugin<IRegionConnector>();
        }

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }
    }
}
