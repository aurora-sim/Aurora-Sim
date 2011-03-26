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
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;

namespace OpenSim.Services
{
    public class AuroraDataServerPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private AgentInfoHandler AgentHandler = new AgentInfoHandler();
        private AssetHandler AssetHandler = new AssetHandler();
        private TelehubInfoHandler TelehubHandler = new TelehubInfoHandler();
        private OfflineMessagesInfoHandler OfflineMessagesHandler = new OfflineMessagesInfoHandler();
        private EstateInfoHandler EstateHandler = new EstateInfoHandler();
        private MuteInfoHandler MuteHandler = new MuteInfoHandler();
        private DirectoryInfoHandler DirectoryHandler = new DirectoryInfoHandler();
        private GroupsServiceHandler GroupsHandler = new GroupsServiceHandler();
        private AbuseReportsHandler AbuseHandler = new AbuseReportsHandler();
        
        protected ulong m_regionHandle;
        protected IRegistryCore m_registry;

        public AuroraDataServerPostHandler(string url, ulong regionHandle, IRegistryCore registry) :
            base("POST", url)
        {
            m_regionHandle = regionHandle;
            m_registry = registry;
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
                request = WebUtils.ParseQueryString(body);
                //if (request.Count == 1)
                //    request = ServerUtils.ParseXmlResponse(body);
                object value = null;
                request.TryGetValue("<?xml version", out value);
                if (value != null)
                    request = WebUtils.ParseXmlResponse(body);

                if (!request.ContainsKey("METHOD"))
                    return FailureResult();

                method = request["METHOD"].ToString();
                IGridRegistrationService urlModule =
                            m_registry.RequestModuleInterface<IGridRegistrationService>();
                switch (method)
                {
                    #region Agents
                    case "getagent":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return AgentHandler.GetAgent(request);
                    #endregion
                    #region Assets
                    case "updatelsldata":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return AssetHandler.UpdateLSLData(request);
                    case "findlsldata":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return AssetHandler.FindLSLData(request);
                    #endregion
                    #region Estates
                    case "loadestatesettings":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return EstateHandler.LoadEstateSettings(request);
                    case "saveestatesettings":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return EstateHandler.SaveEstateSettings(request);
                    case "linkregionestate":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return EstateHandler.LinkRegionEstate(request);
                    case "delinkregionestate":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return EstateHandler.DelinkRegionEstate(request);
                    case "createestate":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return EstateHandler.CreateEstate(request);
                    case "deleteestate":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return EstateHandler.DeleteEstate(request);
                    case "getestates":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return EstateHandler.GetEstates(request);
                    case "getestatesowner":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return EstateHandler.GetEstatesOwner(request);
                    #endregion
                    #region Mutes
                    case "getmutelist":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return MuteHandler.GetMuteList(request);
                    case "updatemute":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return MuteHandler.UpdateMute(request);
                    case "deletemute":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return MuteHandler.DeleteMute(request);
                    case "ismuted":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return MuteHandler.IsMuted(request);
                    #endregion
                    #region Offline Messages
                    case "addofflinemessage":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return OfflineMessagesHandler.AddOfflineMessage(request);
                    case "getofflinemessages":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return OfflineMessagesHandler.GetOfflineMessages(request);
                    #endregion
                    #region Search
                    case "addlandobject":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.AddLandObject(request);
                    case "getparcelinfo":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.GetParcelInfo(request);
                    case "getparcelbyowner":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.GetParcelByOwner(request);
                    case "findland":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.FindLand(request);
                    case "findlandforsale":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.FindLandForSale(request);
                    case "findevents":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.FindEvents(request);
                    case "findeventsinregion":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.FindEventsInRegion(request);
                    case "findclassifieds":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.FindClassifieds(request);
                    case "geteventinfo":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.GetEventInfo(request);
                    case "findclassifiedsinregion":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.FindClassifiedsInRegion(request);
                    #endregion
                    #region Groups
                    case "CreateGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.CreateGroup(request);
                    case "AddGroupNotice":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.AddGroupNotice(request);
                    case "SetAgentActiveGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.SetAgentActiveGroup(request);
                    case "SetAgentGroupSelectedRole":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.SetAgentGroupSelectedRole(request);
                    case "AddAgentToGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.AddAgentToGroup(request);
                    case "AddRoleToGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.AddRoleToGroup(request);
                    case "UpdateGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.UpdateGroup(request);
                    case "RemoveRoleFromGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.RemoveRoleFromGroup(request);
                    case "UpdateRole":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.UpdateRole(request);
                    case "SetAgentGroupInfo":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.SetAgentGroupInfo(request);
                    case "AddAgentGroupInvite":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.AddAgentGroupInvite(request);
                    case "RemoveAgentInvite":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.RemoveAgentInvite(request);
                    case "AddAgentToRole":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.AddAgentToRole(request);
                    case "RemoveAgentFromRole":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.RemoveAgentFromRole(request);
                    case "GetGroupRecord":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupRecord(request);
                    case "GetMemberGroupProfile":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetMemberGroupProfile(request);
                    case "GetGroupMembershipData":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupMembershipData(request);
                    case "RemoveAgentFromGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.RemoveAgentFromGroup(request);
                    case "GetAgentActiveGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetAgentActiveGroup(request);
                    case "GetAgentToGroupInvite":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetAgentToGroupInvite(request);
                    case "GetAgentGroupMemberData":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetAgentGroupMemberData(request);
                    case "GetGroupNotice":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupNotice(request);
                    case "GetAgentGroupMemberships":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetAgentGroupMemberships(request);
                    case "FindGroups":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.FindGroups(request);
                    case "GetAgentGroupRoles":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetAgentGroupRoles(request);
                    case "GetGroupRoles":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupRoles(request);
                    case "GetGroupMembers":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupMembers(request);
                    case "GetGroupRoleMembers":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupRoleMembers(request);
                    case "GetGroupNotices":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupNotices(request);
                    case "GetGroupInvites":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupInvites(request);
                    #endregion
                    #region Abuse Reports
                    case "AddAbuseReport":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult();
                        return AbuseHandler.AddAbuseReport(request);
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
        IAgentConnector AgentConnector;
        public AgentInfoHandler()
        {
            AgentConnector = DataManager.RequestPlugin<IAgentConnector>("IAgentConnectorLocal");
        }

        public byte[] GetAgent(Dictionary<string, object> request)
        {
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);

            IAgentInfo Agent = AgentConnector.GetAgent(principalID);
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (Agent == null)
                result["result"] = "null";
            else
                result["result"] = Agent.ToKeyValuePairs();

            string xmlString = WebUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }
    }

    public class AssetHandler
    {
        IAssetConnector AssetConnector;
        public AssetHandler()
        {
            AssetConnector = DataManager.RequestPlugin<IAssetConnector>("IAssetConnectorLocal");
        }

        public byte[] FindLSLData(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string token = request["token"].ToString();
            string key = request["key"].ToString();
            List<string> data = AssetConnector.FindLSLData(token, key);

            int i = 0;
            foreach (string d in data)
            {
                result.Add(ConvertDecString(i), d);
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] UpdateLSLData(Dictionary<string, object> request)
        {
            string token = request["token"].ToString();
            string key = request["key"].ToString();
            string value = request["value"].ToString();

            AssetConnector.UpdateLSLData(token, key, value);

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

    public class GroupsServiceHandler
    {
        IGroupsServiceConnector GroupsServiceConnector;
        public GroupsServiceHandler()
        {
            GroupsServiceConnector = DataManager.RequestPlugin<IGroupsServiceConnector>("IGroupsServiceConnectorLocal");
        }

        public byte[] CreateGroup(Dictionary<string, object> request)
        {
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
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());

            GroupsServiceConnector.SetAgentActiveGroup(AgentID, GroupID);

            return SuccessResult();
        }

        public byte[] SetAgentGroupSelectedRole(Dictionary<string, object> request)
        {
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID RoleID = UUID.Parse(request["RoleID"].ToString());

            GroupsServiceConnector.SetAgentGroupSelectedRole(AgentID, GroupID, RoleID);

            return SuccessResult();
        }

        public byte[] AddAgentToGroup(Dictionary<string, object> request)
        {
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID RoleID = UUID.Parse(request["RoleID"].ToString());
            UUID RequestingAgentID = UUID.Parse(request["RequestingAgentID"].ToString());

            GroupsServiceConnector.AddAgentToGroup(RequestingAgentID, AgentID, GroupID, RoleID);

            return SuccessResult();
        }

        public byte[] AddRoleToGroup(Dictionary<string, object> request)
        {
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
            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID RoleID = UUID.Parse(request["RoleID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());

            GroupsServiceConnector.RemoveRoleFromGroup(requestingAgentID, RoleID, GroupID);

            return SuccessResult();
        }

        public byte[] UpdateRole(Dictionary<string, object> request)
        {
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
            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID inviteID = UUID.Parse(request["inviteID"].ToString());

            GroupsServiceConnector.RemoveAgentInvite(requestingAgentID, inviteID);

            return SuccessResult();
        }

        public byte[] AddAgentToRole(Dictionary<string, object> request)
        {
            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID RoleID = UUID.Parse(request["RoleID"].ToString());

            GroupsServiceConnector.AddAgentToRole(requestingAgentID, AgentID, GroupID, RoleID);

            return SuccessResult();
        }

        public byte[] RemoveAgentFromRole(Dictionary<string, object> request)
        {
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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
        IDirectoryServiceConnector DirectoryServiceConnector;
        public DirectoryInfoHandler()
        {
            DirectoryServiceConnector = DataManager.RequestPlugin<IDirectoryServiceConnector>("IDirectoryServiceConnectorLocal");
        }

        public byte[] GetParcelInfo(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID INFOUUID = UUID.Parse(request["INFOUUID"].ToString());
            LandData land = DirectoryServiceConnector.GetParcelInfo(INFOUUID);

            if(land != null)
                result.Add("Land", land.ToKeyValuePairs());

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] FindEventsInRegion(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string RegionName = request["REGIONNAME"].ToString();
            int maturity = int.Parse(request["MATURITY"].ToString());
            DirEventsReplyData[] lands = DirectoryServiceConnector.FindAllEventsInRegion(RegionName, maturity);

            int i = 0;
            foreach (DirEventsReplyData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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

            string xmlString = WebUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] AddLandObject(Dictionary<string, object> request)
        {
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
        IEstateConnector EstateConnector;
        public EstateInfoHandler()
        {
            EstateConnector = DataManager.RequestPlugin<IEstateConnector>("IEstateConnectorLocal");
        }

        public byte[] GetEstates(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string search = request["SEARCH"].ToString();
            List<int> EstateIDs = EstateConnector.GetEstates(search);
            Dictionary<string, object> estateresult = new Dictionary<string, object>();
            int i = 0;
            if(EstateIDs != null)
            {
                foreach (int estateID in EstateIDs)
                {
                    estateresult.Add(ConvertDecString(i), estateID);
                    i++;
                }
            }
            result["result"] = estateresult;

            string xmlString = WebUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetEstatesOwner(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID search = UUID.Parse(request["SEARCH"].ToString());
            List<EstateSettings> EstateIDs = EstateConnector.GetEstates(search);
            Dictionary<string, object> estateresult = new Dictionary<string, object>();
            int i = 0;
            foreach (EstateSettings estateID in EstateIDs)
            {
                estateresult.Add(ConvertDecString(i), estateID.ToKeyValuePairs(false));
                i++;
            }
            result["result"] = estateresult;

            string xmlString = WebUtils.BuildXmlResponse(result);
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

        public byte[] DelinkRegionEstate(Dictionary<string, object> request)
        {
            string Password = request["PASSWORD"].ToString();
            UUID RegionID = new UUID(request["REGIONID"].ToString());
            if (EstateConnector.DelinkRegion(RegionID, Password))
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
            string xmlString = WebUtils.BuildXmlResponse(result);
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
            if (request.ContainsKey("REGIONID"))
            {
                string regionID = request["REGIONID"].ToString();
                if(regionID != null)
                    EstateConnector.LoadEstateSettings(UUID.Parse(regionID), out ES);
            }
            if (ES == null)
                return FailureResult();

            //This NEEDS to be false here, otherwise passwords will be sent unsecurely!
            Dictionary<string, object> result = ES.ToKeyValuePairs(false);
            string xmlString = WebUtils.BuildXmlResponse(result);
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
        IMuteListConnector MuteListConnector;
        public MuteInfoHandler()
        {
            MuteListConnector = DataManager.RequestPlugin<IMuteListConnector>("IMuteListConnectorLocal");
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

            string xmlString = WebUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] UpdateMute(Dictionary<string, object> request)
        {
            MuteList mute = new MuteList();
            mute.FromKVP(request);
            UUID PRINCIPALID = UUID.Parse(request["PRINCIPALID"].ToString());
            MuteListConnector.UpdateMute(mute, PRINCIPALID);

            return SuccessResult();
        }

        public byte[] DeleteMute(Dictionary<string, object> request)
        {
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

            string xmlString = WebUtils.BuildXmlResponse(result);
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
        IOfflineMessagesConnector OfflineMessagesConnector;
        public OfflineMessagesInfoHandler()
        {
            OfflineMessagesConnector = DataManager.RequestPlugin<IOfflineMessagesConnector>("IOfflineMessagesConnectorLocal");
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

            string xmlString = WebUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] AddOfflineMessage(Dictionary<string, object> request)
        {
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

    public class TelehubInfoHandler
    {
        IRegionConnector GridConnector;
        public TelehubInfoHandler()
        {
            GridConnector = DataManager.RequestPlugin<IRegionConnector>("IRegionConnectorLocal");
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

    public class AbuseReportsHandler
    {
        public byte[] AddAbuseReport(Dictionary<string, object> request)
        {
            IAbuseReportsConnector m_AbuseReportsService = DataManager.RequestPlugin<IAbuseReportsConnector>("IAbuseReportsConnectorLocal");

            AbuseReport ar = new AbuseReport(request);
            m_AbuseReportsService.AddAbuseReport(ar);
            //m_log.DebugFormat("[ABUSEREPORTS HANDLER]: neighbours for region {0}: {1}", regionID, rinfos.Count);

            return SuccessResult();
        }

        public byte[] UpdateAbuseReport(Dictionary<string, object> request)
        {
            AbuseReport ar = new AbuseReport(request);
            IAbuseReportsConnector m_AbuseReportsService = DataManager.RequestPlugin<IAbuseReportsConnector>("IAbuseReportsConnectorLocal");
            m_AbuseReportsService.UpdateAbuseReport(ar, request["Password"].ToString());
            //m_log.DebugFormat("[ABUSEREPORTS HANDLER]: neighbours for region {0}: {1}", regionID, rinfos.Count);

            return SuccessResult();
        }

        public byte[] GetAbuseReport(Dictionary<string, object> request)
        {
            IAbuseReportsConnector m_AbuseReportsService = DataManager.RequestPlugin<IAbuseReportsConnector>("IAbuseReportsConnectorLocal");
            string xmlString = WebUtils.BuildXmlResponse(
                m_AbuseReportsService.GetAbuseReport(int.Parse(request["Number"].ToString()), request["Password"].ToString()).ToKeyValuePairs());
            //m_log.DebugFormat("[FRIENDS HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetAbuseReports(Dictionary<string, object> request)
        {
            IAbuseReportsConnector m_AbuseReportsService = DataManager.RequestPlugin<IAbuseReportsConnector>("IAbuseReportsConnectorLocal");
            List<AbuseReport> ars = m_AbuseReportsService.GetAbuseReports(int.Parse(request["start"].ToString()), int.Parse(request["count"].ToString()), request["filter"].ToString());
            Dictionary<string, object> returnvalue = new Dictionary<string, object>();
            foreach (AbuseReport ar in ars)
                returnvalue.Add(ar.Number.ToString(), ar);

            string xmlString = WebUtils.BuildXmlResponse(returnvalue);
            //m_log.DebugFormat("[FRIENDS HANDLER]: resp string: {0}", xmlString);
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
}
