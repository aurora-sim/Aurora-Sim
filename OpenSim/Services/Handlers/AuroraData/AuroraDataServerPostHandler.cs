/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Simulation.Base;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services
{
    public class AuroraDataServerPostHandler : BaseStreamHandler
    {
        private readonly AbuseReportsHandler AbuseHandler = new AbuseReportsHandler();
        private readonly AgentInfoHandler AgentHandler = new AgentInfoHandler();
        private readonly AssetHandler AssetHandler = new AssetHandler();
        private readonly DirectoryInfoHandler DirectoryHandler = new DirectoryInfoHandler();
        private readonly GroupsServiceHandler GroupsHandler;
        private readonly MuteInfoHandler MuteHandler = new MuteInfoHandler();
        private OfflineMessagesInfoHandler OfflineMessagesHandler = new OfflineMessagesInfoHandler();
        private TelehubInfoHandler TelehubHandler = new TelehubInfoHandler();

        protected string m_SessionID;
        protected IRegistryCore m_registry;

        public AuroraDataServerPostHandler(string url, string SessionID, IRegistryCore registry) :
            base("POST", url)
        {
            m_SessionID = SessionID;
            m_registry = registry;
            GroupsHandler = new GroupsServiceHandler(registry);
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
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return AgentHandler.GetAgent(request);

                        #endregion

                        #region Assets

                    case "updatelsldata":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return AssetHandler.UpdateLSLData(request);
                    case "findlsldata":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return AssetHandler.FindLSLData(request);

                        #endregion

                        #region Mutes

                    case "getmutelist":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return MuteHandler.GetMuteList(request);
                    case "updatemute":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return MuteHandler.UpdateMute(request);
                    case "deletemute":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return MuteHandler.DeleteMute(request);
                    case "ismuted":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return MuteHandler.IsMuted(request);

                        #endregion

                        #region Search

                    case "findland":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.FindLand(request);
                    case "findlandforsale":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.FindLandForSale(request);
                    case "findevents":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.FindEvents(request);
                    case "findeventsinregion":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.FindEventsInRegion(request);
                    case "findclassifieds":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.FindClassifieds(request);
                    case "geteventinfo":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.GetEventInfo(request);
                    case "findclassifiedsinregion":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.FindClassifiedsInRegion(request);

                        #endregion

                        #region Groups

                    case "CreateGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.CreateGroup(request);
                    case "AddGroupNotice":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.AddGroupNotice(request);
                    case "SetAgentActiveGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.SetAgentActiveGroup(request);
                    case "SetAgentGroupSelectedRole":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.SetAgentGroupSelectedRole(request);
                    case "AddAgentToGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.AddAgentToGroup(request);
                    case "AddRoleToGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.AddRoleToGroup(request);
                    case "UpdateGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.UpdateGroup(request);
                    case "RemoveRoleFromGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.RemoveRoleFromGroup(request);
                    case "UpdateRole":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.UpdateRole(request);
                    case "SetAgentGroupInfo":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.SetAgentGroupInfo(request);
                    case "AddAgentGroupInvite":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.AddAgentGroupInvite(request);
                    case "RemoveAgentInvite":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.RemoveAgentInvite(request);
                    case "AddAgentToRole":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.AddAgentToRole(request);
                    case "RemoveAgentFromRole":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.RemoveAgentFromRole(request);
                    case "GetGroupRecord":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupRecord(request);
                    case "GetMemberGroupProfile":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetMemberGroupProfile(request);
                    case "GetGroupMembershipData":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupMembershipData(request);
                    case "RemoveAgentFromGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GroupsHandler.RemoveAgentFromGroup(request);
                    case "GetAgentActiveGroup":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetAgentActiveGroup(request);
                    case "GetAgentToGroupInvite":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetAgentToGroupInvite(request);
                    case "GetAgentGroupMemberData":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetAgentGroupMemberData(request);
                    case "GetGroupNotice":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupNotice(request);
                    case "GetAgentGroupMemberships":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetAgentGroupMemberships(request);
                    case "GetGroupRecords":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupRecords(request);
                    case "FindGroups":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.FindGroups(request);
                    case "GetAgentGroupRoles":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetAgentGroupRoles(request);
                    case "GetGroupRoles":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupRoles(request);
                    case "GetGroupMembers":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupMembers(request);
                    case "GetGroupRoleMembers":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupRoleMembers(request);
                    case "GetGroupNotices":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupNotices(request);
                    case "GetGroupInvites":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GroupsHandler.GetGroupInvites(request);

                        #endregion

                        #region Abuse Reports

                    case "AddAbuseReport":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return AbuseHandler.AddAbuseReport(request);

                        #endregion
                }
                MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: unknown method {0} request {1}", method.Length, method);
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: Exception {0} in " + method, e);
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
            XmlTextWriter xw = new XmlTextWriter(ms, null) {Formatting = Formatting.Indented};
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        #endregion
    }

    public class AgentInfoHandler
    {
        private readonly IAgentConnector AgentConnector;

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
                result["result"] = Agent.ToKVP();

            string xmlString = WebUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }
    }

    public class AssetHandler
    {
        private readonly IAssetConnector AssetConnector;

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
                result.Add(Util.ConvertDecString(i), d);
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
            XmlTextWriter xw = new XmlTextWriter(ms, null) {Formatting = Formatting.Indented};
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }
    }

    public class GroupsServiceHandler
    {
        private readonly IGroupsServiceConnector GroupsServiceConnector;
        private readonly IUserAccountService UserServiceConnector;

        public GroupsServiceHandler(IRegistryCore registry)
        {
            GroupsServiceConnector = DataManager.RequestPlugin<IGroupsServiceConnector>("IGroupsServiceConnectorLocal");
            UserServiceConnector = registry.RequestModuleInterface<IUserAccountService>();
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
                                               insigniaID, membershipFee, openEnrollment, allowPublish, maturePublish,
                                               founderID, EveryonePowers, OwnerRoleID, OwnerPowers);

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

            string title = GroupsServiceConnector.SetAgentActiveGroup(AgentID, GroupID);

            Dictionary<string, object> result = new Dictionary<string, object>();
            result["groupTitle"] = title;

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] SetAgentGroupSelectedRole(Dictionary<string, object> request)
        {
            UUID AgentID = UUID.Parse(request["AgentID"].ToString());
            UUID GroupID = UUID.Parse(request["GroupID"].ToString());
            UUID RoleID = UUID.Parse(request["RoleID"].ToString());

            string title = GroupsServiceConnector.SetAgentGroupSelectedRole(AgentID, GroupID, RoleID);

            Dictionary<string, object> result = new Dictionary<string, object>();
            result["groupTitle"] = title;

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
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

            GroupsServiceConnector.UpdateGroup(requestingAgentID, groupID, charter, showInList, insigniaID,
                                               membershipFee, openEnrollment, allowPublish, maturePublish);

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

            GroupsServiceConnector.AddAgentGroupInvite(requestingAgentID, inviteID, GroupID, roleID, AgentID,
                                                       FromAgentName);

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
            if (request.ContainsKey("GroupName"))
                GroupName = request["GroupName"].ToString();

            GroupRecord r = GroupsServiceConnector.GetGroupRecord(requestingAgentID, GroupID, GroupName);
            if (r != null)
                result.Add("A", r.ToKVP());

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
            if (r != null)
                result.Add("A", r.ToKVP());

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
            result.Add("A", r.ToKVP());

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetAgentToGroupInvite(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID inviteID = UUID.Parse(request["inviteID"].ToString());

            GroupInviteInfo r = GroupsServiceConnector.GetAgentToGroupInvite(requestingAgentID, inviteID);
            if (r != null)
                result.Add("A", r.ToKVP());

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
            result.Add("A", r.ToKVP());

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetGroupNotice(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UUID noticeID = UUID.Parse(request["noticeID"].ToString());

            GroupNoticeInfo r = GroupsServiceConnector.GetGroupNotice(requestingAgentID, noticeID);
            result.Add("A", r.ToKVP());

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
                result.Add(Util.ConvertDecString(i), r.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetGroupRecords(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            UserAccount requestingAgent = UserServiceConnector.GetUserAccount(UUID.Zero, requestingAgentID);
            List<GroupRecord> rs = new List<GroupRecord>();
            if (requestingAgent.UserLevel > 0)
            {
                uint start = uint.Parse(request["start"].ToString());
                uint count = uint.Parse(request["count"].ToString());
                Dictionary<string, object> ssort = WebUtils.ParseXmlResponse(request["sort"].ToString());
                Dictionary<string, object> bboolFields = WebUtils.ParseXmlResponse(request["boolFields"].ToString());
                Dictionary<string, bool> sort = new Dictionary<string, bool>();
                foreach (KeyValuePair<string, object> kvp in ssort)
                    sort.Add(kvp.Key, (bool)kvp.Value);
                Dictionary<string, bool> boolFields = new Dictionary<string, bool>();
                foreach (KeyValuePair<string, object> kvp in bboolFields)
                    boolFields.Add(kvp.Key, (bool)kvp.Value);
                rs = GroupsServiceConnector.GetGroupRecords(requestingAgentID, start, count, sort, boolFields);
            }

            int i = 0;
            foreach (GroupRecord r in rs)
            {
                result.Add(Util.ConvertDecString(i), r.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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


            List<DirGroupsReplyData> rs = GroupsServiceConnector.FindGroups(requestingAgentID, search, StartQuery,
                                                                            queryflags);
            int i = 0;
            foreach (DirGroupsReplyData r in rs)
            {
                result.Add(Util.ConvertDecString(i), r.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
                result.Add(Util.ConvertDecString(i), r.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
                result.Add(Util.ConvertDecString(i), r.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
#if (!ISWIN)
            foreach (GroupMembersData r in rs)
            {
                if (r != null)
                {
                    result.Add(Util.ConvertDecString(i), r.ToKVP());
                    i++;
                }
            }
#else
            foreach (GroupMembersData r in rs.Where(r => r != null))
            {
                result.Add(Util.ConvertDecString(i), r.ToKVP());
                i++;
            }
#endif

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
                result.Add(Util.ConvertDecString(i), r.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetGroupNotices(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID requestingAgentID = UUID.Parse(request["requestingAgentID"].ToString());
            uint start = uint.Parse(request["start"].ToString());
            uint count = uint.Parse(request["count"].ToString());
            List<UUID> GroupIDs = Util.ConvertToList(request["GroupIDs"].ToString()).ConvertAll(x=>new UUID(x));


            List<GroupNoticeData> rs = GroupsServiceConnector.GetGroupNotices(requestingAgentID, start, count, GroupIDs);
            int i = 0;
            foreach (GroupNoticeData r in rs)
            {
                result.Add(Util.ConvertDecString(i), r.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
                result.Add(Util.ConvertDecString(i), r.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
            XmlTextWriter xw = new XmlTextWriter(ms, null) {Formatting = Formatting.Indented};
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }
    }

    public class DirectoryInfoHandler
    {
        private readonly IDirectoryServiceConnector DirectoryServiceConnector;

        public DirectoryInfoHandler()
        {
            DirectoryServiceConnector =
                DataManager.RequestPlugin<IDirectoryServiceConnector>("IDirectoryServiceConnectorLocal");
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
                result.Add(Util.ConvertDecString(i), land.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] FindLandForSale(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string SEARCHTYPE = request["SEARCHTYPE"].ToString();
            uint PRICE = uint.Parse(request["PRICE"].ToString());
            uint AREA = uint.Parse(request["AREA"].ToString());
            int STARTQUERY = int.Parse(request["STARTQUERY"].ToString());
            uint FLAGS = uint.Parse(request["FLAGS"].ToString());
            DirLandReplyData[] lands = DirectoryServiceConnector.FindLandForSale(SEARCHTYPE, PRICE, AREA, STARTQUERY, FLAGS);

            int i = 0;
            foreach (DirLandReplyData land in lands)
            {
                result.Add(Util.ConvertDecString(i), land.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] FindEvents(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string QUERYTEXT = request["QUERYTEXT"].ToString();
            uint FLAGS = uint.Parse(request["FLAGS"].ToString());
            int STARTQUERY = int.Parse(request["STARTQUERY"].ToString());
            DirEventsReplyData[] lands = DirectoryServiceConnector.FindEvents(QUERYTEXT, FLAGS, STARTQUERY);

            int i = 0;
            foreach (DirEventsReplyData land in lands)
            {
                result.Add(Util.ConvertDecString(i), land.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
                result.Add(Util.ConvertDecString(i), land.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] FindClassifieds(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string QUERYTEXT = request["QUERYTEXT"].ToString();
            string CATEGORY = request["CATEGORY"].ToString();
            uint QUERYFLAGS = uint.Parse(request["QUERYFLAGS"].ToString());
            int STARTQUERY = int.Parse(request["STARTQUERY"].ToString());
            DirClassifiedReplyData[] lands = DirectoryServiceConnector.FindClassifieds(QUERYTEXT, CATEGORY, QUERYFLAGS, STARTQUERY);

            int i = 0;
            foreach (DirClassifiedReplyData land in lands)
            {
                result.Add(Util.ConvertDecString(i), land.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetEventInfo(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            uint EVENTID = uint.Parse(request["EVENTID"].ToString());
            EventData eventdata = DirectoryServiceConnector.GetEventInfo(EVENTID);

            result.Add("event", eventdata.ToKVP());

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
                result.Add(Util.ConvertDecString(i), classified.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
            XmlTextWriter xw = new XmlTextWriter(ms, null) {Formatting = Formatting.Indented};
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }
    }

    public class MuteInfoHandler
    {
        private readonly IMuteListConnector MuteListConnector;

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
                result.Add(Util.ConvertDecString(i), Mute.ToKVP());
                i++;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
            XmlTextWriter xw = new XmlTextWriter(ms, null) {Formatting = Formatting.Indented};
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }
    }

    public class TelehubInfoHandler
    {
        private IRegionConnector GridConnector;

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
            XmlTextWriter xw = new XmlTextWriter(ms, null) {Formatting = Formatting.Indented};
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }
    }

    public class AbuseReportsHandler
    {
        public byte[] AddAbuseReport(Dictionary<string, object> request)
        {
            IAbuseReportsConnector m_AbuseReportsService =
                DataManager.RequestPlugin<IAbuseReportsConnector>("IAbuseReportsConnectorLocal");

            AbuseReport ar = new AbuseReport();
            ar.FromKVP(request);
            m_AbuseReportsService.AddAbuseReport(ar);
            //MainConsole.Instance.DebugFormat("[ABUSEREPORTS HANDLER]: neighbours for region {0}: {1}", regionID, rinfos.Count);

            return SuccessResult();
        }

        public byte[] UpdateAbuseReport(Dictionary<string, object> request)
        {
            AbuseReport ar = new AbuseReport();
            ar.FromKVP(request);
            IAbuseReportsConnector m_AbuseReportsService =
                DataManager.RequestPlugin<IAbuseReportsConnector>("IAbuseReportsConnectorLocal");
            m_AbuseReportsService.UpdateAbuseReport(ar, request["Password"].ToString());
            //MainConsole.Instance.DebugFormat("[ABUSEREPORTS HANDLER]: neighbours for region {0}: {1}", regionID, rinfos.Count);

            return SuccessResult();
        }

        public byte[] GetAbuseReport(Dictionary<string, object> request)
        {
            IAbuseReportsConnector m_AbuseReportsService =
                DataManager.RequestPlugin<IAbuseReportsConnector>("IAbuseReportsConnectorLocal");
            string xmlString = WebUtils.BuildXmlResponse(
                m_AbuseReportsService.GetAbuseReport(int.Parse(request["Number"].ToString()),
                                                     request["Password"].ToString()).ToKVP());
            //MainConsole.Instance.DebugFormat("[FRIENDS HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetAbuseReports(Dictionary<string, object> request)
        {
            IAbuseReportsConnector m_AbuseReportsService =
                DataManager.RequestPlugin<IAbuseReportsConnector>("IAbuseReportsConnectorLocal");
            List<AbuseReport> ars = m_AbuseReportsService.GetAbuseReports(int.Parse(request["start"].ToString()),
                                                                          int.Parse(request["count"].ToString()),
                                                                          request["filter"].ToString());
#if (!ISWIN)
            Dictionary<string, object> returnvalue = new Dictionary<string, object>();
            foreach (AbuseReport ar in ars)
                returnvalue.Add(ar.Number.ToString(), ar);
#else
            Dictionary<string, object> returnvalue = ars.ToDictionary<AbuseReport, string, object>(ar => ar.Number.ToString(), ar => ar);
#endif

            string xmlString = WebUtils.BuildXmlResponse(returnvalue);
            //MainConsole.Instance.DebugFormat("[FRIENDS HANDLER]: resp string: {0}", xmlString);
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
            XmlTextWriter xw = new XmlTextWriter(ms, null) {Formatting = Formatting.Indented};
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }
    }
}