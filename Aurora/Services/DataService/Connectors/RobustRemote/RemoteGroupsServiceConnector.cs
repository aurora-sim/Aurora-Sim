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
using System.Linq;
using System.Reflection;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class RemoteGroupsServiceConnector : IGroupsServiceConnector
    {
        private IRegistryCore m_registry;

        #region IGroupsServiceConnector Members

        #region IAuroraDataPlugin members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("GroupsConnector", "LocalConnector") == "RemoteConnector")
            {
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IGroupsServiceConnector"; }
        }

        #endregion

        public void CreateGroup(UUID groupID, string name, string charter, bool showInList, UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish, UUID founderID, ulong EveryonePowers, UUID OwnerRoleID, ulong OwnerPowers)
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

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(founderID.ToString(),
                                                                                           "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
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

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
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

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
        }

        public string SetAgentActiveGroup(UUID AgentID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "SetAgentActiveGroup";
            sendData["AgentID"] = AgentID;
            sendData["GroupID"] = GroupID;
            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(AgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
                           reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                            return replyData["groupTitle"].ToString();
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.ToString());
            }
            return "No Title Could Be Found";
        }

        public UUID GetAgentActiveGroup(UUID requestingAgentID, UUID AgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetAgentActiveGroup";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["AgentID"] = AgentID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf (requestingAgentID.ToString (), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
                           reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            // Success
                            return UUID.Parse(replyData["A"].ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.ToString());
            }

            return UUID.Zero;
        }

        public string SetAgentGroupSelectedRole(UUID AgentID, UUID GroupID, UUID RoleID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "SetAgentGroupSelectedRole";
            sendData["AgentID"] = AgentID;
            sendData["GroupID"] = GroupID;
            sendData["RoleID"] = RoleID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(AgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
                           reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                            return replyData["groupTitle"].ToString();
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.ToString());
            }
            return "No Title Could Be Found";
        }

        public void AddAgentToGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "AddAgentToGroup";
            sendData["AgentID"] = AgentID;
            sendData["GroupID"] = GroupID;
            sendData["RoleID"] = RoleID;
            sendData["RequestingAgentID"] = requestingAgentID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
        }

        public bool RemoveAgentFromGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "RemoveAgentFromGroup";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;
            sendData["AgentID"] = AgentID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                             m_ServerURI,
                                                                             reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            bool group = false;
#if (!ISWIN)
                            foreach (object f in replyvalues)
                            {
                                if (bool.TryParse(f.ToString(), out group))
                                {
                                    break;
                                }
                            }
#else
                            foreach (object f in replyvalues.Where(f => bool.TryParse(f.ToString(), out group)))
                            {
                                break;
                            }
#endif
                            // Success
                            return group;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return false;
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

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
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

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
        }

        public void RemoveRoleFromGroup(UUID requestingAgentID, UUID RoleID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "RemoveRoleFromGroup";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["RoleID"] = RoleID;
            sendData["GroupID"] = GroupID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
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

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
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

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
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

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
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

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
        }

        public void RemoveAgentInvite(UUID requestingAgentID, UUID inviteID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "RemoveAgentInvite";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["inviteID"] = inviteID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
        }

        public void AddGroupProposal(UUID agentID, GroupProposalInfo info)
        {
        }

        public uint GetNumberOfGroupNotices(UUID requestingAgentID, UUID GroupID)
        {
            List<UUID> GroupIDs = new List<UUID>();
            GroupIDs.Add(GroupID);
            return GetNumberOfGroupNotices(requestingAgentID, GroupIDs);
        }

        public uint GetNumberOfGroupNotices(UUID requestingAgentID, List<UUID> GroupIDs)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetNumberOfGroupNotices";
            sendData["requestingAgentID"] = requestingAgentID.ToString();
            sendData["GroupIDs"] = GroupIDs;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                             m_ServerURI,
                                                                             reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            uint numGroupNotices = 0;
                            foreach (object f in replyvalues.Where(f => uint.TryParse(f.ToString(), out numGroupNotices)))
                            {
                                break;
                            }
                            // Success
                            return numGroupNotices;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }
            return 0;
        }

        public uint GetNumberOfGroups(UUID requestingAgentID, Dictionary<string, bool> boolFields)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetNumberOfGroups";
            sendData["requestingAgentID"] = requestingAgentID.ToString();
            sendData["boolFields"] = boolFields;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                             m_ServerURI,
                                                                             reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            uint numGroups = 0;
#if (!ISWIN)
                            foreach (object f in replyvalues)
                            {
                                if (uint.TryParse(f.ToString(), out numGroups))
                                {
                                    break;
                                }
                            }
#else
                            foreach (object f in replyvalues.Where(f => uint.TryParse(f.ToString(), out numGroups)))
                            {
                                break;
                            }
#endif
                            // Success
                            return numGroups;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }
            return 0;
        }

        public GroupRecord GetGroupRecord(UUID requestingAgentID, UUID GroupID, string GroupName)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupRecord";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;
            sendData["GroupName"] = GroupName;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(requestingAgentID.ToString(), "RemoteServerURI", true);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
                           reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            foreach (object f in replyvalues)
                            {
                                if (f is Dictionary<string, object>)
                                {
                                    GroupRecord group = new GroupRecord((Dictionary<string, object>)f);
                                    // Success
                                    return group;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e.ToString());
            }

            return null;
        }

        private static List<GroupRecord> remoteGroupRecordsQueryResult(IRegistryCore registry, Dictionary<string, object> sendData)
        {
            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs = registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
#if (!ISWIN)
                foreach (string mServerUri in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            // Success
                            List<GroupRecord> list = new List<GroupRecord>();
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null) list.Add(new GroupRecord(f));
                            }
                            return list;
                        }
                    }
                }
#else
                foreach (Dictionary<string, object>.ValueCollection replyvalues in from m_ServerURI in m_ServerURIs
                                                                                   select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                         m_ServerURI,
                                                                                                         reqString) into reply
                                                                                   where reply != string.Empty
                                                                                   select WebUtils.ParseXmlResponse(reply) into replyData
                                                                                   where replyData != null
                                                                                   select replyData.Values)
                {
                    // Success
                    return replyvalues.OfType<Dictionary<string, object>>().Select(f => new GroupRecord(f)).ToList();
                }
#endif

            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return new List<GroupRecord>(0);
        }

        public List<GroupRecord> GetGroupRecords(UUID requestingAgentID, uint start, uint count, Dictionary<string, bool> sort, Dictionary<string, bool> boolFields)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupRecords";
            sendData["requestingAgentID"] = requestingAgentID.ToString();
            sendData["start"] = start;
            sendData["count"] = count;
            sendData["sort"] = sort;
            sendData["boolFields"] = boolFields;

            return remoteGroupRecordsQueryResult(m_registry, sendData);
        }

        public List<GroupRecord> GetGroupRecords(UUID requestingAgentID, List<UUID> GroupIDs)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupRecords";
            sendData["requestingAgentID"] = requestingAgentID.ToString();
            sendData["GroupIDs"] = GroupIDs;

            return remoteGroupRecordsQueryResult(m_registry, sendData);
        }

        public GroupProfileData GetMemberGroupProfile(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetMemberGroupProfile";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;
            sendData["AgentID"] = AgentID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string mServerUri in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            foreach (object f in replyvalues)
                            {
                                if (f is Dictionary<string, object>)
                                {
                                    GroupProfileData @group = new GroupProfileData((Dictionary<string, object>) f);
                                    // Success
                                    return group;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
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

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                             m_ServerURI,
                                                                             reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            GroupMembershipData group = null;
#if (!ISWIN)
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null)
                                {
                                    group = new GroupMembershipData(f);
                                }
                            }
#else
                            foreach (Dictionary<string, object> f in replyvalues.OfType<Dictionary<string, object>>())
                            {
                                group = new GroupMembershipData(f);
                            }
#endif
                            // Success
                            return group;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return null;
        }

        public List<GroupMembershipData> GetAgentGroupMemberships(UUID requestingAgentID, UUID AgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetAgentGroupMemberships";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["AgentID"] = AgentID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
#if (!ISWIN)
                foreach (string mServerUri in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            // Success
                            List<GroupMembershipData> list = new List<GroupMembershipData>();
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null) list.Add(new GroupMembershipData(f));
                            }
                            return list;
                        }
                    }
                }
#else
                foreach (Dictionary<string, object>.ValueCollection replyvalues in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                          m_ServerURI,
                                                                                                                                          reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply) into replyData where replyData != null select replyData.Values)
                {
                    // Success
                    return replyvalues.OfType<Dictionary<string, object>>().Select(f => new GroupMembershipData(f)).ToList();
                }
#endif
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return new List<GroupMembershipData>();
        }

        public GroupInviteInfo GetAgentToGroupInvite(UUID requestingAgentID, UUID inviteID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetAgentToGroupInvite";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["inviteID"] = inviteID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                             m_ServerURI,
                                                                             reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            GroupInviteInfo group = null;
#if (!ISWIN)
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null)
                                {
                                    group = new GroupInviteInfo(f);
                                }
                            }
#else
                            foreach (Dictionary<string, object> f in replyvalues.OfType<Dictionary<string, object>>())
                            {
                                group = new GroupInviteInfo(f);
                            }
#endif
                            // Success
                            return group;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return null;
        }

        public List<GroupInviteInfo> GetGroupInvites(UUID requestingAgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupInvites";
            sendData["requestingAgentID"] = requestingAgentID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
#if (!ISWIN)
                foreach (string mServerUri in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            // Success
                            List<GroupInviteInfo> list = new List<GroupInviteInfo>();
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null) list.Add(new GroupInviteInfo(f));
                            }
                            return list;
                        }
                    }
                }
#else
                foreach (Dictionary<string, object>.ValueCollection replyvalues in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                          m_ServerURI,
                                                                                                                                          reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply) into replyData where replyData != null select replyData.Values)
                {
                    // Success
                    return replyvalues.OfType<Dictionary<string, object>>().Select(f => new GroupInviteInfo(f)).ToList();
                }
#endif
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return new List<GroupInviteInfo>();
        }

        public GroupMembersData GetAgentGroupMemberData(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetAgentGroupMemberData";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;
            sendData["AgentID"] = AgentID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                             m_ServerURI,
                                                                             reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            GroupMembersData group = null;
#if (!ISWIN)
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null)
                                {
                                    group = new GroupMembersData(f);
                                }
                            }
#else
                            foreach (Dictionary<string, object> f in replyvalues.OfType<Dictionary<string, object>>())
                            {
                                group = new GroupMembersData(f);
                            }
#endif
                            // Success
                            return group;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return null;
        }

        public List<GroupMembersData> GetGroupMembers(UUID requestingAgentID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupMembers";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
#if (!ISWIN)
                foreach (string mServerUri in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            // Success
                            List<GroupMembersData> list = new List<GroupMembersData>();
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null) list.Add(new GroupMembersData(f));
                            }
                            return list;
                        }
                    }
                }
#else
                foreach (Dictionary<string, object>.ValueCollection replyvalues in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                          m_ServerURI,
                                                                                                                                          reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply) into replyData where replyData != null select replyData.Values)
                {
                    return replyvalues.OfType<Dictionary<string, object>>().Select(f => new GroupMembersData(f)).ToList();
                }
#endif
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return new List<GroupMembersData>();
        }

        public List<DirGroupsReplyData> FindGroups(UUID requestingAgentID, string search, int StartQuery, uint queryflags)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "FindGroups";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["search"] = search;
            sendData["StartQuery"] = StartQuery;
            sendData["queryflags"] = queryflags;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
#if (!ISWIN)
                foreach (string mServerUri in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            // Success
                            List<DirGroupsReplyData> list = new List<DirGroupsReplyData>();
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null) list.Add(new DirGroupsReplyData(f));
                            }
                            return list;
                        }
                    }
                }
#else
                foreach (Dictionary<string, object>.ValueCollection replyvalues in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                          m_ServerURI,
                                                                                                                                          reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply) into replyData where replyData != null select replyData.Values)
                {
                    // Success
                    return replyvalues.OfType<Dictionary<string, object>>().Select(f => new DirGroupsReplyData(f)).ToList();
                }
#endif
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
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

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
#if (!ISWIN)
                foreach (string mServerUri in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            // Success
                            List<GroupRolesData> list = new List<GroupRolesData>();
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null) list.Add(new GroupRolesData(f));
                            }
                            return list;
                        }
                    }
                }
#else
                foreach (Dictionary<string, object>.ValueCollection replyvalues in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                          m_ServerURI,
                                                                                                                                          reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply) into replyData where replyData != null select replyData.Values)
                {
                    // Success
                    return replyvalues.OfType<Dictionary<string, object>>().Select(f => new GroupRolesData(f)).ToList();
                }
#endif
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return new List<GroupRolesData>();
        }

        public List<GroupRolesData> GetGroupRoles(UUID requestingAgentID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupRoles";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
#if (!ISWIN)
                foreach (string mServerUri in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            // Success
                            List<GroupRolesData> list = new List<GroupRolesData>();
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null) list.Add(new GroupRolesData(f));
                            }
                            return list;
                        }
                    }
                }
#else
                foreach (Dictionary<string, object>.ValueCollection replyvalues in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                          m_ServerURI,
                                                                                                                                          reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply) into replyData where replyData != null select replyData.Values)
                {
                    // Success
                    return replyvalues.OfType<Dictionary<string, object>>().Select(f => new GroupRolesData(f)).ToList();
                }
#endif
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return new List<GroupRolesData>();
        }

        public List<GroupRoleMembersData> GetGroupRoleMembers(UUID requestingAgentID, UUID GroupID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupRoleMembers";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["GroupID"] = GroupID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
#if (!ISWIN)
                foreach (string mServerUri in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            // Success
                            List<GroupRoleMembersData> list = new List<GroupRoleMembersData>();
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null) list.Add(new GroupRoleMembersData(f));
                            }
                            return list;
                        }
                    }
                }
#else
                foreach (Dictionary<string, object>.ValueCollection replyvalues in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                          m_ServerURI,
                                                                                                                                          reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply) into replyData where replyData != null select replyData.Values)
                {
                    // Success
                    return replyvalues.OfType<Dictionary<string, object>>().Select(f => new GroupRoleMembersData(f)).ToList();
                }
#endif
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return new List<GroupRoleMembersData>();
        }

        public GroupNoticeInfo GetGroupNotice(UUID requestingAgentID, UUID noticeID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupNotice";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["noticeID"] = noticeID;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                             m_ServerURI,
                                                                             reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            GroupNoticeInfo group = null;
#if (!ISWIN)
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null)
                                {
                                    group = new GroupNoticeInfo(f);
                                }
                            }
#else
                            foreach (Dictionary<string, object> f in replyvalues.OfType<Dictionary<string, object>>())
                            {
                                group = new GroupNoticeInfo(f);
                            }
#endif
                            // Success
                            return group;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return null;
        }

        public List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, uint start, uint count, UUID GroupID)
        {
            List<UUID> GroupIDs = new List<UUID>();
            GroupIDs.Add(GroupID);
            return GetGroupNotices(requestingAgentID, start, count, GroupIDs);
        }

        public List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, uint start, uint count, List<UUID> GroupIDs)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "GetGroupNotices";
            sendData["requestingAgentID"] = requestingAgentID;
            sendData["start"] = start;
            sendData["count"] = count;
            sendData["GroupIDs"] = GroupIDs;

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        requestingAgentID.ToString(), "RemoteServerURI", false);
#if (!ISWIN)
                foreach (string mServerUri in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            // Success
                            List<GroupNoticeData> list = new List<GroupNoticeData>();
                            foreach (object replyvalue in replyvalues)
                            {
                                Dictionary<string, object> f = replyvalue as Dictionary<string, object>;
                                if (f != null) list.Add(new GroupNoticeData(f));
                            }
                            return list;
                        }
                    }
                }
#else
                foreach (Dictionary<string, object>.ValueCollection replyvalues in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                          m_ServerURI,
                                                                                                                                          reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply) into replyData where replyData != null select replyData.Values)
                {
                    // Success
                    return replyvalues.OfType<Dictionary<string, object>>().Select(f => new GroupNoticeData(f)).ToList();
                }
#endif
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteGroupsServiceConnector]: Exception when contacting server: {0}", e);
            }

            return new List<GroupNoticeData>();
        }

        #endregion

        public void Dispose()
        {
        }
    }
}