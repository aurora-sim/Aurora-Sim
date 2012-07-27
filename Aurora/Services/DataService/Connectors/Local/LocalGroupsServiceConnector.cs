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
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Services.DataService
{
    public class LocalGroupsServiceConnector : ConnectorBase, IGroupsServiceConnector
    {
        #region Declares

        private IGenericData data;
        List<UUID> agentsCanBypassGroupNoticePermsCheck = new List<UUID>();

        #endregion

        #region IAuroraDataPlugin members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            data = GenericData;

            if (source.Configs[Name] != null)
            {
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);
            }
            if (source.Configs["Groups"] != null)
            {
                agentsCanBypassGroupNoticePermsCheck = Util.ConvertToList(source.Configs["Groups"].GetString("AgentsCanBypassGroupNoticePermsCheck", "")).ConvertAll(x => new UUID(x));
            }

            data.ConnectToDatabase(defaultConnectionString, "Groups",
                                   source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            DataManager.DataManager.RegisterPlugin(Name + "Local", this);

            if (source.Configs["AuroraConnectors"].GetString("GroupsConnector", "LocalConnector") == "LocalConnector")
            {
                DataManager.DataManager.RegisterPlugin(this);
            }
            Init(simBase, Name);
        }

        public string Name
        {
            get { return "IGroupsServiceConnector"; }
        }

        #endregion

        #region IGroupsServiceConnector Members

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void CreateGroup(UUID groupID, string name, string charter, bool showInList, UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish, UUID founderID, ulong EveryonePowers, UUID OwnerRoleID, ulong OwnerPowers)
        {
            object remoteValue = DoRemote(groupID, name, charter, showInList, insigniaID, membershipFee, openEnrollment, allowPublish, maturePublish, founderID, EveryonePowers, OwnerRoleID, OwnerPowers);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            Dictionary<string, object> row = new Dictionary<string, object>(11);
            row["GroupID"] = groupID;
            row["Name"] = name;
            row["Charter"] = charter;
            row["InsigniaID"] = insigniaID;
            row["FounderID"] = founderID;
            row["MembershipFee"] = membershipFee;
            row["OpenEnrollment"] = openEnrollment ? 1 : 0;
            row["ShowInList"] = showInList ? 1 : 0;
            row["AllowPublish"] = allowPublish ? 1 : 0;
            row["MaturePublish"] = maturePublish ? 1 : 0;
            row["OwnerRoleID"] = OwnerRoleID;

            data.Insert("osgroup", row);

            //Add everyone role to group
            AddRoleToGroup(founderID, groupID, UUID.Zero, "Everyone", "Everyone in the group is in the everyone role.", "Member of " + name, EveryonePowers);

            const ulong groupPowers = 296868139497678;

            UUID officersRole = UUID.Random();
            //Add officers role to group
            AddRoleToGroup(founderID, groupID, officersRole, "Officers", "The officers of the group, with more powers than regular members.", "Officer of " + name, groupPowers);

            //Add owner role to group
            AddRoleToGroup(founderID, groupID, OwnerRoleID, "Owners", "Owners of " + name, "Owner of " + name, OwnerPowers);

            //Add owner to the group as owner
            AddAgentToGroup(founderID, founderID, groupID, OwnerRoleID);
            AddAgentToRole(founderID, founderID, groupID, officersRole);

            SetAgentGroupSelectedRole(founderID, groupID, OwnerRoleID);

            SetAgentActiveGroup(founderID, groupID);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void UpdateGroup(UUID requestingAgentID, UUID groupID, string charter, int showInList, UUID insigniaID, int membershipFee, int openEnrollment, int allowPublish, int maturePublish)
        {
            object remoteValue = DoRemote(requestingAgentID, groupID, charter, showInList, insigniaID, membershipFee, openEnrollment, allowPublish, maturePublish);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (CheckGroupPermissions(requestingAgentID, groupID, (ulong)(GroupPowers.ChangeOptions | GroupPowers.ChangeIdentity)))
            {
                Dictionary<string, object> values = new Dictionary<string, object>(6);
                values["Charter"] = charter;
                values["InsigniaID"] = insigniaID;
                values["MembershipFee"] = membershipFee;
                values["OpenEnrollment"] = openEnrollment;
                values["ShowInList"] = showInList;
                values["AllowPublish"] = allowPublish;
                values["MaturePublish"] = maturePublish;

                QueryFilter filter = new QueryFilter();
                filter.andFilters["GroupID"] = groupID;

                data.Update("osgroup", values, null, filter, null, null);
            }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void AddGroupNotice(UUID requestingAgentID, UUID groupID, UUID noticeID, string fromName, string subject, string message, UUID ItemID, int AssetType, string ItemName)
        {
            object remoteValue = DoRemote(requestingAgentID, groupID, noticeID, fromName, subject, message, ItemID, AssetType, ItemName);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (CheckGroupPermissions(requestingAgentID, groupID, (ulong)GroupPowers.SendNotices))
            {
                Dictionary<string, object> row = new Dictionary<string, object>(10);
                row["GroupID"] = groupID;
                row["NoticeID"] = noticeID == UUID.Zero ? UUID.Random() : noticeID;
                row["Timestamp"] = ((uint) Util.UnixTimeSinceEpoch());
                row["FromName"] = fromName;
                row["Subject"] = subject;
                row["Message"] = message;
                row["HasAttachment"] = (ItemID != UUID.Zero) ? 1 : 0;
                row["ItemID"] = ItemID;
                row["AssetType"] = AssetType;
                row["ItemName"] = ItemName;

                data.Insert("osgroupnotice", row);
            }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.High)]
        public bool EditGroupNotice(UUID requestingAgentID, UUID groupID, UUID noticeID, string subject, string message)
        {
            object remoteValue = DoRemote(requestingAgentID, groupID, noticeID, subject, message);
            if (remoteValue != null || m_doRemoteOnly)
            {
                return (bool)remoteValue;
            }

            if(!agentsCanBypassGroupNoticePermsCheck.Contains(requestingAgentID) && !CheckGroupPermissions(requestingAgentID, groupID, (ulong)GroupPowers.SendNotices)){
                MainConsole.Instance.TraceFormat("Permission check failed when trying to edit group notice {0}.", noticeID);
                return false;
            }

            GroupNoticeInfo GNI = GetGroupNotice(requestingAgentID, noticeID);
            if (GNI == null)
            {
                MainConsole.Instance.TraceFormat("Could not find group notice {0}", noticeID);
                return false;
            }
            else if (GNI.GroupID != groupID)
            {
                MainConsole.Instance.TraceFormat("Group notice {0} group ID {1} does not match supplied group ID {2}", noticeID, GNI.GroupID, groupID);
                return false;
            }
            else if(subject.Trim() == string.Empty || message.Trim() == string.Empty)
            {
                MainConsole.Instance.TraceFormat("Could not edit group notice {0}, message or subject was empty", noticeID);
                return false;
            }

            QueryFilter filter = new QueryFilter();
            filter.andFilters["GroupID"] = groupID;
            filter.andFilters["NoticeID"] = noticeID;

            Dictionary<string, object> update = new Dictionary<string,object>(2);
            update["Subject"] = subject.Trim();
            update["Message"] = message.Trim();

            return data.Update("osgroupnotice", update, null, filter, null, null);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.High)]
        public bool RemoveGroupNotice(UUID requestingAgentID, UUID groupID, UUID noticeID)
        {
            object remoteValue = DoRemote(requestingAgentID, groupID, noticeID);
            if (remoteValue != null || m_doRemoteOnly)
            {
                return (bool)remoteValue;
            }

            if (!agentsCanBypassGroupNoticePermsCheck.Contains(requestingAgentID) && !CheckGroupPermissions(requestingAgentID, groupID, (ulong)GroupPowers.SendNotices))
            {
                MainConsole.Instance.TraceFormat("Permission check failed when trying to edit group notice {0}.", noticeID);
                return false;
            }

            GroupNoticeInfo GNI = GetGroupNotice(requestingAgentID, noticeID);
            if (GNI == null)
            {
                MainConsole.Instance.TraceFormat("Could not find group notice {0}", noticeID);
                return false;
            }
            else if (GNI.GroupID != groupID)
            {
                MainConsole.Instance.TraceFormat("Group notice {0} group ID {1} does not match supplied group ID {2}", noticeID, GNI.GroupID, groupID);
                return false;
            }

            QueryFilter filter = new QueryFilter();
            filter.andFilters["GroupID"] = groupID;
            filter.andFilters["NoticeID"] = noticeID;

            return data.Delete("osgroupnotice", filter);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public string SetAgentActiveGroup(UUID AgentID, UUID GroupID)
        {
            object remoteValue = DoRemote(AgentID, GroupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (string)remoteValue;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["AgentID"] = AgentID;
            if (data.Query(new[] { "*" }, "osagent", filter, null, null, null).Count != 0)
            {
                Dictionary<string, object> values = new Dictionary<string, object>(1);
                values["ActiveGroupID"] = GroupID;

                data.Update("osagent", values, null, filter, null, null);
            }
            else
            {
                Dictionary<string, object> row = new Dictionary<string, object>(2);
                row["AgentID"] = AgentID;
                row["ActiveGroupID"] = GroupID;
                data.Insert("osagent", row);
            }
            GroupMembersData gdata = GetAgentGroupMemberData(AgentID, GroupID, AgentID);
            return gdata == null ? "" : gdata.Title;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public UUID GetAgentActiveGroup(UUID RequestingAgentID, UUID AgentID)
        {
            object remoteValue = DoRemote(RequestingAgentID, AgentID);
            if (remoteValue != null || m_doRemoteOnly)
                return (UUID)remoteValue; // note: this is bad, you can't cast a null object to a UUID

            QueryFilter filter = new QueryFilter();
            filter.andFilters["AgentID"] = AgentID;
            List<string> groups = data.Query(new string[1] { "ActiveGroupID" }, "osagent", filter, null, null, null);

            return (groups.Count != 0) ? UUID.Parse(groups[0]) : UUID.Zero;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public string SetAgentGroupSelectedRole(UUID AgentID, UUID GroupID, UUID RoleID)
        {
            object remoteValue = DoRemote(AgentID, GroupID, RoleID);
            if (remoteValue != null || m_doRemoteOnly)
                return (string)remoteValue;

            Dictionary<string, object> values = new Dictionary<string, object>(1);
            values["SelectedRoleID"] = RoleID;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["AgentID"] = AgentID;
            filter.andFilters["GroupID"] = GroupID;

            data.Update("osgroupmembership", values, null, filter, null, null);

            GroupMembersData gdata = GetAgentGroupMemberData(AgentID, GroupID, AgentID);
            return gdata == null ? "" : gdata.Title;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void AddAgentToGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            object remoteValue = DoRemote(requestingAgentID, AgentID, GroupID, RoleID);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            Dictionary<string, object> where = new Dictionary<string, object>(2);
            where["AgentID"] = AgentID;
            where["GroupID"] = GroupID;

            if (data.Query(new[] { "*" }, "osgroupmembership", new QueryFilter
            {
                andFilters = where
            }, null, null, null).Count != 0)
            {
                MainConsole.Instance.Error("[AGM]: Agent " + AgentID + " is already in " + GroupID);
                return;
            }
            Dictionary<string, object> row = new Dictionary<string, object>(6);
            row["GroupID"] = GroupID;
            row["AgentID"] = AgentID;
            row["SelectedRoleID"] = RoleID;
            row["Contribution"] = 0;
            row["ListInProfile"] = 1;
            row["AcceptNotices"] = 1;
            data.Insert("osgroupmembership", row);

            // Make sure they're in the Everyone role
            AddAgentToRole(requestingAgentID, AgentID, GroupID, UUID.Zero);
            // Make sure they're in specified role, if they were invited
            if (RoleID != UUID.Zero)
                AddAgentToRole(requestingAgentID, AgentID, GroupID, RoleID);
            //Set the role they were invited to as their selected role
            SetAgentGroupSelectedRole(AgentID, GroupID, RoleID);
            SetAgentActiveGroup(AgentID, GroupID);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool RemoveAgentFromGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            //Allow kicking yourself
            object remoteValue = DoRemote(requestingAgentID, AgentID, GroupID);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue != null && (bool)remoteValue;

            if ((CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.RemoveMember)) || (requestingAgentID == AgentID))
            {
                QueryFilter filter = new QueryFilter();
                filter.andFilters["AgentID"] = AgentID;
                filter.andFilters["ActiveGroupID"] = GroupID;

                Dictionary<string, object> values = new Dictionary<string, object>(1);
                values["ActiveGroupID"] = UUID.Zero;

                // 1. If group is agent's active group, change active group to uuidZero
                data.Update("osagent", values, null, filter, null, null);

                filter.andFilters.Remove("ActiveGroupID");
                filter.andFilters["GroupID"] = GroupID;

                // 2. Remove Agent from group (osgroupmembership)
                data.Delete("osgrouprolemembership", filter);

                // 3. Remove Agent from all of the groups roles (osgrouprolemembership)
                data.Delete("osgroupmembership", filter);

                return true;
            }
            return false;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void AddRoleToGroup(UUID requestingAgentID, UUID GroupID, UUID RoleID, string NameOf, string Description, string Title, ulong Powers)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupID, RoleID, NameOf, Description, Title, Powers);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.CreateRole))
            {
                Dictionary<string, object> row = new Dictionary<string, object>(6);
                row["GroupID"] = GroupID;
                row["RoleID"] = RoleID;
                row["Name"] = NameOf;
                row["Description"] = Description;
                row["Title"] = Title;
                row["Powers"] = (long)Powers;
                data.Insert("osrole", row);
            }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void UpdateRole(UUID requestingAgentID, UUID GroupID, UUID RoleID, string NameOf, string Desc, string Title, ulong Powers)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupID, RoleID, NameOf, Desc, Title, Powers);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.RoleProperties))
            {
                Dictionary<string, object> values = new Dictionary<string, object>();
                values["RoleID"] = RoleID;
                if (NameOf != null)
                {
                    values["Name"] = NameOf;
                }
                if (Desc != null)
                {
                    values["Description"] = Desc;
                }
                if (Title != null)
                {
                    values["Title"] = Title;
                }
                values["Powers"] = Powers;

                QueryFilter filter = new QueryFilter();
                filter.andFilters["GroupID"] = GroupID;
                filter.andFilters["RoleID"] = RoleID;

                data.Update("osrole", values, null, filter, null, null);
            }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void RemoveRoleFromGroup(UUID requestingAgentID, UUID RoleID, UUID GroupID)
        {
            object remoteValue = DoRemote(requestingAgentID, RoleID, GroupID);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.DeleteRole))
            {
                Dictionary<string, object> values = new Dictionary<string, object>(1);
                values["SelectedRoleID"] = UUID.Zero;

                QueryFilter ufilter = new QueryFilter();
                ufilter.andFilters["GroupID"] = GroupID;
                ufilter.andFilters["SelectedRoleID"] = RoleID;

                QueryFilter dfilter = new QueryFilter();
                dfilter.andFilters["GroupID"] = GroupID;
                dfilter.andFilters["RoleID"] = RoleID;

                data.Delete("osgrouprolemembership", dfilter);
                data.Update("osgroupmembership", values, null, ufilter, null, null);
                data.Delete("osrole", dfilter);
            }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void AddAgentToRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            object remoteValue = DoRemote(requestingAgentID, AgentID, GroupID, RoleID);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.AssignMember))
            {
                //This isn't an open and shut case, they could be setting the agent to their role, which would allow for AssignMemberLimited
                if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.AssignMemberLimited))
                {
                    MainConsole.Instance.Warn("[AGM]: User " + requestingAgentID + " attempted to add user " + AgentID +
                               " to group " + GroupID + ", but did not have permissions to do so!");
                    return;
                }
            }

            QueryFilter filter = new QueryFilter();
            filter.andFilters["GroupID"] = GroupID;
            filter.andFilters["RoleID"] = RoleID;
            filter.andFilters["AgentID"] = AgentID;
            //Make sure they arn't already in this role
            if (uint.Parse(data.Query(new[] { "COUNT(AgentID)" }, "osgrouprolemembership", filter, null, null, null)[0]) == 0)
            {
                Dictionary<string, object> row = new Dictionary<string, object>(3);
                row["GroupID"] = GroupID;
                row["RoleID"] = RoleID;
                row["AgentID"] = AgentID;
                data.Insert("osgrouprolemembership", row);
            }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void RemoveAgentFromRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            object remoteValue = DoRemote(requestingAgentID, AgentID, GroupID, RoleID);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.AssignMember))
            {
                Dictionary<string, object> values = new Dictionary<string, object>(1);
                values["SelectedRoleID"] = UUID.Zero;

                QueryFilter filter = new QueryFilter();
                filter.andFilters["AgentID"] = AgentID;
                filter.andFilters["GroupID"] = GroupID;
                filter.andFilters["SelectedRoleID"] = RoleID;

                data.Update("osgroupmembership", values, null, filter, null, null);

                filter.andFilters.Remove("SelectedRoleID");
                filter.andFilters["RoleID"] = RoleID;
                data.Delete("osgrouprolemembership", filter);
            }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void SetAgentGroupInfo(UUID requestingAgentID, UUID AgentID, UUID GroupID, int AcceptNotices, int ListInProfile)
        {
            object remoteValue = DoRemote(requestingAgentID, AgentID, GroupID, AcceptNotices, ListInProfile);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.ChangeIdentity))
            {
                return;
            }

            Dictionary<string, object> values = new Dictionary<string, object>(3);
            values["AgentID"] = AgentID;
            values["AcceptNotices"] = AcceptNotices;
            values["ListInProfile"] = ListInProfile;

            QueryFilter filter = new QueryFilter();
            // these look the wrong way around ~ SignpostMarv
            filter.andFilters["GroupID"] = AgentID;
            filter.andFilters["AgentID"] = GroupID;

            data.Update("osgroupmembership", values, null, filter, null, null);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void AddAgentGroupInvite(UUID requestingAgentID, UUID inviteID, UUID GroupID, UUID roleID, UUID AgentID, string FromAgentName)
        {
            object remoteValue = DoRemote(requestingAgentID, inviteID, GroupID, roleID, AgentID, FromAgentName);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.Invite))
            {
                QueryFilter filter = new QueryFilter();
                filter.andFilters["AgentID"] = AgentID;
                filter.andFilters["GroupID"] = GroupID;
                data.Delete("osgroupinvite", filter);

                Dictionary<string, object> row = new Dictionary<string, object>(6);
                row["InviteID"] = inviteID;
                row["GroupID"] = GroupID;
                row["RoleID"] = roleID;
                row["AgentID"] = AgentID;
                row["TMStamp"] = Util.UnixTimeSinceEpoch();
                row["FromAgentName"] = FromAgentName;
                data.Insert("osgroupinvite", row);
            }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void RemoveAgentInvite(UUID requestingAgentID, UUID inviteID)
        {
            object remoteValue = DoRemote(requestingAgentID, inviteID);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["InviteID"] = inviteID;
            data.Delete("osgroupinvite", filter);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void AddGroupProposal(UUID agentID, GroupProposalInfo info)
        {
            object remoteValue = DoRemote(agentID, info);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (CheckGroupPermissions(agentID, info.GroupID, (ulong)GroupPowers.StartProposal))
                GenericUtils.AddGeneric(info.GroupID, "Proposal", info.VoteID.ToString(), info.ToOSD(), data);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupProposalInfo> GetActiveProposals(UUID agentID, UUID groupID)
        {
            object remoteValue = DoRemote(agentID, groupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupProposalInfo>)remoteValue;

            if (!CheckGroupPermissions(agentID, groupID, (ulong)GroupPowers.VoteOnProposal))
                return new List<GroupProposalInfo>();

            List<GroupProposalInfo> proposals = GenericUtils.GetGenerics<GroupProposalInfo>(groupID, "Proposal", data);
            proposals = (from p in proposals where p.Ending > DateTime.Now select p).ToList();
            foreach (GroupProposalInfo p in proposals)
                p.VoteCast = GetHasVoted(agentID, p);

            return proposals;//Return only ones that are still running
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupProposalInfo> GetInactiveProposals(UUID agentID, UUID groupID)
        {
            object remoteValue = DoRemote(agentID, groupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupProposalInfo>)remoteValue;

            if (!CheckGroupPermissions(agentID, groupID, (ulong)GroupPowers.VoteOnProposal))
                return new List<GroupProposalInfo>();

            List<GroupProposalInfo> proposals = GenericUtils.GetGenerics<GroupProposalInfo>(groupID, "Proposal", data);
            proposals = (from p in proposals where p.Ending < DateTime.Now select p).ToList();
            List<GroupProposalInfo> proposalsNeedingResults = (from p in proposals where !p.HasCalculatedResult select p).ToList();
            foreach (GroupProposalInfo p in proposalsNeedingResults)
            {
                List<OpenMetaverse.StructuredData.OSDMap> maps = GenericUtils.GetGenerics(p.GroupID, p.VoteID.ToString(), data);
                int yes = 0;
                int no = 0;
                foreach (OpenMetaverse.StructuredData.OSDMap vote in maps)
                {
                    if (vote["Vote"].AsString().ToLower() == "yes")
                        yes++;
                    else if (vote["Vote"].AsString().ToLower() == "no")
                        no++;
                }
                if (yes + no < p.Quorum)
                    p.Result = false;
                /*if (yes > no)
                    p.Result = true;
                else
                    p.Result = false;*/
                p.HasCalculatedResult = true;
                GenericUtils.AddGeneric(p.GroupID, "Proposal", p.VoteID.ToString(), p.ToOSD(), data);
            }
            foreach (GroupProposalInfo p in proposals)
                p.VoteCast = GetHasVoted(agentID, p);

            return proposals;//Return only ones that are still running
        }

        private string GetHasVoted(UUID agentID, GroupProposalInfo p)
        {
            OpenMetaverse.StructuredData.OSDMap map = GenericUtils.GetGeneric(p.GroupID, p.VoteID.ToString(), agentID.ToString(), data);
            if (map != null)
                return map["Vote"];
            return "";
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void VoteOnActiveProposals(UUID agentID, UUID groupID, UUID proposalID, string vote)
        {
            object remoteValue = DoRemote(agentID, groupID, proposalID, vote);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (!CheckGroupPermissions(agentID, groupID, (ulong)GroupPowers.VoteOnProposal))
                return;

            OpenMetaverse.StructuredData.OSDMap map = new OpenMetaverse.StructuredData.OSDMap();
            map["Vote"] = vote;
            GenericUtils.AddGeneric(groupID, proposalID.ToString(), agentID.ToString(), map, data);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public uint GetNumberOfGroupNotices(UUID requestingAgentID, UUID GroupID)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (uint)remoteValue; // note: this is bad, you can't cast a null object to a uint

            List<UUID> GroupIDs = new List<UUID> { GroupID };
            return GetNumberOfGroupNotices(requestingAgentID, GroupIDs);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public uint GetNumberOfGroupNotices(UUID requestingAgentID, List<UUID> GroupIDs)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupIDs);
            if (remoteValue != null || m_doRemoteOnly)
                return (uint)remoteValue; // note: this is bad, you can't cast a null object to a uint

            bool had = GroupIDs.Count > 0;

            List<UUID> groupIDs = new List<UUID>();
            if (!agentsCanBypassGroupNoticePermsCheck.Contains(requestingAgentID))
            {
#if (!ISWIN)
                foreach (UUID GroupID in GroupIDs)
                {
                    if (CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.ReceiveNotices))
                        groupIDs.Add(GroupID);
                }
#else
                groupIDs.AddRange(GroupIDs.Where(GroupID => CheckGroupPermissions(requestingAgentID, GroupID, (ulong) GroupPowers.ReceiveNotices)));
#endif
            }
            else
            {
                groupIDs = GroupIDs;
            }

            if (had && groupIDs.Count == 0)
            {
                return 0;
            }

            QueryFilter filter = new QueryFilter();
            List<object> filterGroupIDs = new List<object>(groupIDs.Count);
            filterGroupIDs.AddRange(groupIDs.Cast<object>());
            if (filterGroupIDs.Count > 0)
            {
                filter.orMultiFilters["GroupID"] = filterGroupIDs;
            }

            return uint.Parse(data.Query(new[] { "COUNT(NoticeID)" }, "osgroupnotice", filter, null, null, null)[0]);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public uint GetNumberOfGroups(UUID requestingAgentID, Dictionary<string, bool> boolFields)
        {
            object remoteValue = DoRemote(requestingAgentID, boolFields);
            if (remoteValue != null || m_doRemoteOnly)
                return (uint)remoteValue; // note: this is bad, you can't cast a null object to a uint

            QueryFilter filter = new QueryFilter();

            string[] BoolFields = { "OpenEnrollment", "ShowInList", "AllowPublish", "MaturePublish" };
            foreach (string field in BoolFields)
            {
                if (boolFields.ContainsKey(field))
                {
                    filter.andFilters[field] = boolFields[field] ? "1" : "0";
                }
            }

            return uint.Parse(data.Query(new[] { "COUNT(GroupID)" }, "osgroup", filter, null, null, null)[0]);
        }

        private static GroupRecord GroupRecordQueryResult2GroupRecord(List<String> result)
        {
            return new GroupRecord
            {
                GroupID = UUID.Parse(result[0]),
                GroupName = result[1],
                Charter = result[2],
                GroupPicture = UUID.Parse(result[3]),
                FounderID = UUID.Parse(result[4]),
                MembershipFee = int.Parse(result[5]),
                OpenEnrollment = int.Parse(result[6]) == 1,
                ShowInList = int.Parse(result[7]) == 1,
                AllowPublish = int.Parse(result[8]) == 1,
                MaturePublish = int.Parse(result[9]) == 1,
                OwnerRoleID = UUID.Parse(result[10])
            };
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public GroupRecord GetGroupRecord(UUID requestingAgentID, UUID GroupID, string GroupName)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupID, GroupName);
            if (remoteValue != null || m_doRemoteOnly)
                return (GroupRecord)remoteValue;

            QueryFilter filter = new QueryFilter();

            if (GroupID != UUID.Zero)
            {
                filter.andFilters["GroupID"] = GroupID;
            }
            if (!string.IsNullOrEmpty(GroupName))
            {
                filter.andFilters["Name"] = GroupName;
            }
            if (filter.Count == 0)
            {
                return null;
            }
            List<string> osgroupsData = data.Query(new[]{
                "GroupID",
                "Name",
                "Charter",
                "InsigniaID",
                "FounderID",
                "MembershipFee",
                "OpenEnrollment",
                "ShowInList",
                "AllowPublish",
                "MaturePublish",
                "OwnerRoleID"
            }, "osgroup", filter, null, null, null);
            return (osgroupsData.Count == 0) ? null : GroupRecordQueryResult2GroupRecord(osgroupsData);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupRecord> GetGroupRecords(UUID requestingAgentID, uint start, uint count, Dictionary<string, bool> sort, Dictionary<string, bool> boolFields)
        {
            //            List<string> filter = new List<string>();

            object remoteValue = DoRemote(requestingAgentID, start, count, boolFields);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupRecord>)remoteValue;

            string[] sortAndBool = { "OpenEnrollment", "MaturePublish" };
            string[] BoolFields = { "OpenEnrollment", "ShowInList", "AllowPublish", "MaturePublish" };

            foreach (string field in sortAndBool)
            {
                if (boolFields.ContainsKey(field) && sort.ContainsKey(field))
                {
                    sort.Remove(field);
                }
            }

            QueryFilter filter = new QueryFilter();

            foreach (string field in BoolFields)
            {
                if (boolFields.ContainsKey(field))
                {
                    filter.andFilters[field] = boolFields[field] ? "1" : "0";
                }
            }

            List<GroupRecord> Reply = new List<GroupRecord>();

            List<string> osgroupsData = data.Query(new[]{
                "GroupID",
                "Name",
                "Charter",
                "InsigniaID",
                "FounderID",
                "MembershipFee",
                "OpenEnrollment",
                "ShowInList",
                "AllowPublish",
                "MaturePublish",
                "OwnerRoleID"
            }, "osgroup", filter, sort, start, count);

            if (osgroupsData.Count < 11)
            {
                return Reply;
            }
            for (int i = 0; i < osgroupsData.Count; i += 11)
            {
                Reply.Add(GroupRecordQueryResult2GroupRecord(osgroupsData.GetRange(i, 11)));
            }
            return Reply;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupRecord> GetGroupRecords(UUID requestingAgentID, List<UUID> GroupIDs)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupIDs);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupRecord>)remoteValue;

            List<GroupRecord> Reply = new List<GroupRecord>(0);
            if (GroupIDs.Count <= 0)
            {
                return Reply;
            }

            QueryFilter filter = new QueryFilter();
            filter.orMultiFilters["GroupID"] = new List<object>();
            foreach (UUID groupID in GroupIDs)
            {
                filter.orMultiFilters["GroupID"].Add(groupID);
            }

            List<string> osgroupsData = data.Query(new[]{
                "GroupID",
                "Name",
                "Charter",
                "InsigniaID",
                "FounderID",
                "MembershipFee",
                "OpenEnrollment",
                "ShowInList",
                "AllowPublish",
                "MaturePublish",
                "OwnerRoleID"
            }, "osgroup", filter, null, null, null);

            if (osgroupsData.Count < 11)
            {
                return Reply;
            }
            for (int i = 0; i < osgroupsData.Count; i += 11)
            {
                Reply.Add(GroupRecordQueryResult2GroupRecord(osgroupsData.GetRange(i, 11)));
            }
            return Reply;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public GroupProfileData GetMemberGroupProfile(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupID, AgentID);
            if (remoteValue != null || m_doRemoteOnly)
                return (GroupProfileData)remoteValue;

            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.MemberVisible))
                return new GroupProfileData();

            GroupProfileData GPD = new GroupProfileData();
            GroupRecord record = GetGroupRecord(requestingAgentID, GroupID, null);

            QueryFilter filter1 = new QueryFilter();
            filter1.andFilters["GroupID"] = AgentID; // yes these look the wrong way around
            filter1.andFilters["AgentID"] = GroupID; // but they were like that when I got here! ~ SignpostMarv

            QueryFilter filter2 = new QueryFilter();
            filter2.andFilters["GroupID"] = GroupID;

            List<string> Membership = data.Query(new[]{
                "Contribution",
                "ListInProfile",
                "SelectedRoleID"
            }, "osgroupmembership", filter1, null, null, null);

            int GroupMemCount = int.Parse(data.Query(new[] { "COUNT(AgentID)" }, "osgroupmembership", filter2, null, null, null)[0]);

            int GroupRoleCount = int.Parse(data.Query(new[] { "COUNT(RoleID)" }, "osrole", filter2, null, null, null)[0]);

            QueryFilter filter3 = new QueryFilter();
            filter3.andFilters["RoleID"] = Membership[2];
            List<string> GroupRole = data.Query(new[] {
                "Name",
                "Powers"
            }, "osrole", filter3, null, null, null);

            GPD.AllowPublish = record.AllowPublish;
            GPD.Charter = record.Charter;
            GPD.FounderID = record.FounderID;
            GPD.GroupID = record.GroupID;
            GPD.GroupMembershipCount = GroupMemCount;
            GPD.GroupRolesCount = GroupRoleCount;
            GPD.InsigniaID = record.GroupPicture;
            GPD.MaturePublish = record.MaturePublish;
            GPD.MembershipFee = record.MembershipFee;
            GPD.MemberTitle = GroupRole[0];
            GPD.Money = 0;

            GPD.Name = record.GroupName;
            GPD.OpenEnrollment = record.OpenEnrollment;
            GPD.OwnerRole = record.OwnerRoleID;
            GPD.PowersMask = ulong.Parse(GroupRole[1]);
            GPD.ShowInList = int.Parse(Membership[2]) == 1;

            return GPD;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public GroupMembershipData GetGroupMembershipData(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupID, AgentID);
            if (remoteValue != null || m_doRemoteOnly)
                return (GroupMembershipData)remoteValue;

            if (GroupID == UUID.Zero)
                GroupID = GetAgentActiveGroup(requestingAgentID, AgentID);
            if (GroupID == UUID.Zero)
                return null;

            QueryTables tables = new QueryTables();
            tables.AddTable("osgroup", "osg");
            tables.AddTable("osgroupmembership", "osgm", JoinType.Inner, new[,] { { "osg.GroupID", "osgm.GroupID" } });
            tables.AddTable("osrole", "osr", JoinType.Inner, new[,] { { "osgm.SelectedRoleID", "osr.RoleID" }, { "osr.GroupID", "osg.GroupID" } });

            QueryFilter filter = new QueryFilter();
            filter.andFilters["osg.GroupID"] = GroupID;
            filter.andFilters["osgm.AgentID"] = AgentID;

            string[] fields = new[]
                {
                    "osgm.AcceptNotices",
                    "osgm.Contribution",
                    "osgm.ListInProfile",
                    "osgm.SelectedRoleID",
                    "osr.Title",
                    "osr.Powers",
                    "osg.AllowPublish",
                    "osg.Charter",
                    "osg.FounderID",
                    "osg.Name",
                    "osg.InsigniaID",
                    "osg.MaturePublish",
                    "osg.MembershipFee",
                    "osg.OpenEnrollment",
                    "osg.ShowInList"
                };
            List<string> Membership = data.Query(fields, tables, filter, null, null, null);

            if (fields.Length != Membership.Count)
                return null;

            GroupMembershipData GMD = new GroupMembershipData
            {
                AcceptNotices = int.Parse(Membership[0]) == 1,
                Active = true, //TODO: Figure out what this is and its effects if false
                ActiveRole = UUID.Parse(Membership[3]),
                AllowPublish = int.Parse(Membership[6]) == 1,
                Charter = Membership[7],
                Contribution = int.Parse(Membership[1]),
                FounderID = UUID.Parse(Membership[8]),
                GroupID = GroupID,
                GroupName = Membership[9],
                GroupPicture = UUID.Parse(Membership[10]),
                GroupPowers = ulong.Parse(Membership[5]),
                GroupTitle = Membership[4],
                ListInProfile = int.Parse(Membership[2]) == 1,
                MaturePublish = int.Parse(Membership[11]) == 1,
                MembershipFee = int.Parse(Membership[12]),
                OpenEnrollment = int.Parse(Membership[13]) == 1,
                ShowInList = int.Parse(Membership[14]) == 1
            };


            return GMD;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupTitlesData> GetGroupTitles(UUID requestingAgentID, UUID GroupID)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupTitlesData>)remoteValue;

            QueryTables tables = new QueryTables();
            tables.AddTable("osgroupmembership", "osgm");
            tables.AddTable("osgrouprolemembership", "osgrm", JoinType.Inner, new[,] { { "osgm.AgentID", "osgrm.AgentID" }, { "osgm.GroupID", "osgrm.GroupID" } });
            tables.AddTable("osrole", "osr", JoinType.Inner, new[,] { { "osgrm.RoleID", "osr.RoleID" }, { "osgm.GroupID", "osr.GroupID" } });


            QueryFilter filter = new QueryFilter();
            filter.andFilters["osgm.AgentID"] = requestingAgentID;
            filter.andFilters["osgm.GroupID"] = GroupID;

            List<string> Membership = data.Query(new[] { 
                "osgm.SelectedRoleID",
                "osgrm.RoleID",
                "osr.Name"
            }, tables, filter, null, null, null);


            List<GroupTitlesData> titles = new List<GroupTitlesData>();
            for (int loop = 0; loop < Membership.Count(); loop += 3)
            {
                titles.Add(new GroupTitlesData { Name = Membership[loop + 2], UUID = UUID.Parse(Membership[loop + 1]), Selected = Membership[loop + 0] == Membership[loop + 1] });
            }
            return titles;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupMembershipData> GetAgentGroupMemberships(UUID requestingAgentID, UUID AgentID)
        {
            object remoteValue = DoRemote(requestingAgentID, AgentID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupMembershipData>)remoteValue;

            QueryTables tables = new QueryTables();
            tables.AddTable("osgroup", "osg");
            tables.AddTable("osgroupmembership", "osgm", JoinType.Inner, new[,] { { "osg.GroupID", "osgm.GroupID" } });
            tables.AddTable("osrole", "osr", JoinType.Inner, new[,] { { "osgm.SelectedRoleID", "osr.RoleID" } });

            QueryFilter filter = new QueryFilter();
            filter.andFilters["osgm.AgentID"] = AgentID;

            string[] fields = new[]
                {
                    "osgm.AcceptNotices",
                    "osgm.Contribution",
                    "osgm.ListInProfile",
                    "osgm.SelectedRoleID",
                    "osr.Title",
                    "osr.Powers",
                    "osg.AllowPublish",
                    "osg.Charter",
                    "osg.FounderID",
                    "osg.Name",
                    "osg.InsigniaID",
                    "osg.MaturePublish",
                    "osg.MembershipFee",
                    "osg.OpenEnrollment",
                    "osg.ShowInList",
                    "osg.GroupID"
                };
            List<string> Membership = data.Query(fields, tables, filter, null, null, null);
            List<GroupMembershipData> results = new List<GroupMembershipData>();
            for (int loop = 0; loop < Membership.Count; loop += fields.Length)
            {
                results.Add(new GroupMembershipData
                {
                    AcceptNotices = int.Parse(Membership[loop + 0]) == 1,
                    Active = true,
                    //TODO: Figure out what this is and its effects if false
                    ActiveRole = UUID.Parse(Membership[loop + 3]),
                    AllowPublish = int.Parse(Membership[loop + 6]) == 1,
                    Charter = Membership[loop + 7],
                    Contribution = int.Parse(Membership[loop + 1]),
                    FounderID = UUID.Parse(Membership[loop + 8]),
                    GroupID = UUID.Parse(Membership[loop + 15]),
                    GroupName = Membership[loop + 9],
                    GroupPicture = UUID.Parse(Membership[loop + 10]),
                    GroupPowers = ulong.Parse(Membership[loop + 5]),
                    GroupTitle = Membership[loop + 4],
                    ListInProfile = int.Parse(Membership[loop + 2]) == 1,
                    MaturePublish = int.Parse(Membership[loop + 11]) == 1,
                    MembershipFee = int.Parse(Membership[loop + 12]),
                    OpenEnrollment = int.Parse(Membership[loop + 13]) == 1,
                    ShowInList = int.Parse(Membership[loop + 14]) == 1
                });
            }
            return results;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public GroupInviteInfo GetAgentToGroupInvite(UUID requestingAgentID, UUID inviteID)
        {
            object remoteValue = DoRemote(requestingAgentID, inviteID);
            if (remoteValue != null || m_doRemoteOnly)
                return (GroupInviteInfo)remoteValue;

            GroupInviteInfo invite = new GroupInviteInfo();

            Dictionary<string, object> where = new Dictionary<string, object>(2);
            where["AgentID"] = requestingAgentID;
            where["InviteID"] = inviteID;

            List<string> groupInvite = data.Query(new[] { "*" }, "osgroupinvite", new QueryFilter
            {
                andFilters = where
            }, null, null, null);

            if (groupInvite.Count == 0)
            {
                return null;
            }
            invite.AgentID = UUID.Parse(groupInvite[3]);
            invite.GroupID = UUID.Parse(groupInvite[1]);
            invite.InviteID = UUID.Parse(groupInvite[0]);
            invite.RoleID = UUID.Parse(groupInvite[2]);
            invite.FromAgentName = groupInvite[5];

            return invite;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupInviteInfo> GetGroupInvites(UUID requestingAgentID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["AgentID"] = requestingAgentID;

            object remoteValue = DoRemote(requestingAgentID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupInviteInfo>)remoteValue;

            List<string> groupInvite = data.Query(new[] { "*" }, "osgroupinvite", filter, null, null, null);

            List<GroupInviteInfo> invites = new List<GroupInviteInfo>();

            for (int i = 0; i < groupInvite.Count; i += 6)
            {
                invites.Add(new GroupInviteInfo
                {
                    AgentID = UUID.Parse(groupInvite[i + 3]),
                    GroupID = UUID.Parse(groupInvite[i + 1]),
                    InviteID = UUID.Parse(groupInvite[i]),
                    RoleID = UUID.Parse(groupInvite[i + 2]),
                    FromAgentName = groupInvite[i + 5]
                });
            }

            return invites;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public GroupMembersData GetAgentGroupMemberData(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupID, AgentID);
            if (remoteValue != null || m_doRemoteOnly)
                return (GroupMembersData)remoteValue;


            QueryFilter filter = new QueryFilter();
            filter.andFilters["GroupID"] = GroupID;
            filter.andFilters["AgentID"] = requestingAgentID;

            //Permissions
            List<string> OtherPermiss = data.Query(new string[4] { 
                "AcceptNotices",
                "Contribution",
                "ListInProfile", 
                "SelectedRoleID"
            }, "osgroupmembership", filter, null, null, null);

            if (OtherPermiss.Count == 0)
            {
                return null;
            }

            filter.andFilters["AgentID"] = AgentID;

            List<string> Membership = data.Query(new string[4] { 
                "AcceptNotices",
                "Contribution",
                "ListInProfile", 
                "SelectedRoleID"
            }, "osgroupmembership", filter, null, null, null);

            if (Membership.Count != 4)
            {
                return null;
            }

            filter.andFilters.Remove("AgentID");
            filter.andFilters["RoleID"] = Membership[3];

            List<string> GroupRole = data.Query(new string[2] { 
                "Title",
                "Powers"
            }, "osrole", filter, null, null, null);

            if (GroupRole.Count != 2)
            {
                return null;
            }

            filter.andFilters.Remove("RoleID");

            List<string> OwnerRoleID = data.Query(new string[1] { 
                "OwnerRoleID"
            }, "osgroup", filter, null, null, null);

            filter.andFilters["RoleID"] = OwnerRoleID[0];
            filter.andFilters["AgentID"] = AgentID;

            bool IsOwner = uint.Parse(data.Query(new string[1] { 
                "COUNT(AgentID)"
            }, "osgrouprolemembership", filter, null, null, null)[0]) == 1;

            GroupMembersData GMD = new GroupMembersData
            {
                AcceptNotices = (Membership[0]) == "1",
                AgentID = AgentID,
                Contribution = int.Parse(Membership[1]),
                IsOwner = IsOwner,
                ListInProfile = (Membership[2]) == "1",
                AgentPowers = ulong.Parse(GroupRole[1]),
                Title = GroupRole[0],
                OnlineStatus = "(Online)"
            };

            return GMD;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupMembersData> GetGroupMembers(UUID requestingAgentID, UUID GroupID)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupMembersData>)remoteValue;

            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.None))
            {
                return new List<GroupMembersData>(0);
            }

            QueryFilter filter = new QueryFilter();
            filter.andFilters["GroupID"] = GroupID;
            List<string> Agents = data.Query(new[] { "AgentID" }, "osgroupmembership", filter, null, null, null);

            List<GroupMembersData> list = new List<GroupMembersData>();
            foreach (string agent in Agents)
            {
                GroupMembersData d = GetAgentGroupMemberData(requestingAgentID, GroupID, UUID.Parse(agent));
                if (d == null) continue;
                OpenSim.Services.Interfaces.UserInfo info =
                    m_registry.RequestModuleInterface<OpenSim.Services.Interfaces.IAgentInfoService>().GetUserInfo(
                        d.AgentID.ToString());
                if (info != null && !info.IsOnline)
                    d.OnlineStatus = info.LastLogin.ToShortDateString();
                else if (info == null)
                    d.OnlineStatus = "Unknown";
                else
                    d.OnlineStatus = "Online";
                list.Add(d);
            }
            return list;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<DirGroupsReplyData> FindGroups(UUID requestingAgentID, string search, int StartQuery, uint queryflags)
        {
            object remoteValue = DoRemote(requestingAgentID, search, StartQuery, queryflags);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<DirGroupsReplyData>)remoteValue;

            QueryFilter filter = new QueryFilter();
            filter.andLikeFilters["Name"] = "%" + search + "%";

            List<string> retVal = data.Query(new[]{
                "GroupID",
                "Name",
                "ShowInList",
                "AllowPublish",
                "MaturePublish"
            }, "osgroup", filter, null, (uint)StartQuery, 50);

            List<DirGroupsReplyData> Reply = new List<DirGroupsReplyData>();

            for (int i = 0; i < retVal.Count; i += 5)
            {
                if (retVal[i + 2] == "0")// (ShowInList param) They don't want to be shown in search.. respect this
                {
                    continue;
                }

                if ((queryflags & (uint)DirectoryManager.DirFindFlags.IncludeMature) != (uint)DirectoryManager.DirFindFlags.IncludeMature)
                {
                    if (retVal[i + 4] == "1") // (MaturePublish param) Check for pg,mature
                    {
                        continue;
                    }
                }

                DirGroupsReplyData dirgroup = new DirGroupsReplyData { groupID = UUID.Parse(retVal[i]), groupName = retVal[i + 1] };
                filter = new QueryFilter();
                filter.andFilters["GroupID"] = dirgroup.groupID;
                dirgroup.members = int.Parse(data.Query(new[] { "COUNT(AgentID)" }, "osgroupmembership", filter, null, null, null)[0]);

                Reply.Add(dirgroup);
            }
            return Reply;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupRolesData> GetAgentGroupRoles(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            // I couldn't actually get this function to call when testing changes
            object remoteValue = DoRemote(requestingAgentID, AgentID, GroupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupRolesData>)remoteValue;

            //No permissions check necessary, we are checking only roles that they are in, so if they arn't in the group, that isn't a problem

            QueryTables tables = new QueryTables();
            tables.AddTable("osgrouprolemembership", "osgm");
            tables.AddTable("osrole", "osr", JoinType.Inner, new[,] { { "osgm.RoleID", "osr.RoleID" } });

            QueryFilter filter = new QueryFilter();
            filter.andFilters["osgm.AgentID"] = AgentID;
            filter.andFilters["osgm.GroupID"] = GroupID;

            string[] fields = new[]
                                  {
                                      "osr.Name",
                                      "osr.Description",
                                      "osr.Title",
                                      "osr.Powers",
                                      "osr.RoleID"
                                  };
            List<string> Roles = data.Query(fields, tables, filter, null, null, null);

            filter = new QueryFilter();

            List<GroupRolesData> RolesData = new List<GroupRolesData>();

            for (int loop = 0; loop < Roles.Count; loop += fields.Length)
            {
                RolesData.Add(new GroupRolesData
                {
                    RoleID = UUID.Parse(Roles[loop + 4]),
                    Name = Roles[loop + 0],
                    Description = Roles[loop + 1],
                    Powers = ulong.Parse(Roles[loop + 3]),
                    Title = Roles[loop + 2]
                });
            }

            return RolesData;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupRolesData> GetGroupRoles(UUID requestingAgentID, UUID GroupID)
        {
            // Can't use joins here without a group by as well
            object remoteValue = DoRemote(requestingAgentID, GroupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupRolesData>)remoteValue;

            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.None))
            {
                return new List<GroupRolesData>(0);
            }

            List<GroupRolesData> GroupRoles = new List<GroupRolesData>();

            QueryFilter rolesFilter = new QueryFilter();
            rolesFilter.andFilters["GroupID"] = GroupID;
            List<string> Roles = data.Query(new[]{
                "Name",
                "Description",
                "Title",
                "Powers",
                "RoleID"
            }, "osrole", rolesFilter, null, null, null);

            QueryFilter filter = new QueryFilter();
            filter.andFilters["GroupID"] = GroupID;

            for (int i = 0; i < Roles.Count; i += 5)
            {
                filter.andFilters["RoleID"] = UUID.Parse(Roles[i + 4]);
                int Count = int.Parse(data.Query(new[] { "COUNT(AgentID)" }, "osgrouprolemembership", filter, null, null, null)[0]);

                GroupRoles.Add(new GroupRolesData
                {
                    Members = Count,
                    RoleID = UUID.Parse(Roles[i + 4]),
                    Name = Roles[i + 0],
                    Description = Roles[i + 1],
                    Powers = ulong.Parse(Roles[i + 3]),
                    Title = Roles[i + 2]
                });
            }
            return GroupRoles;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupRoleMembersData> GetGroupRoleMembers(UUID requestingAgentID, UUID GroupID)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupRoleMembersData>)remoteValue;

            List<GroupRoleMembersData> RoleMembers = new List<GroupRoleMembersData>();

            QueryTables tables = new QueryTables();
            tables.AddTable("osgrouprolemembership", "osgrm");
            tables.AddTable("osrole", "osr", JoinType.Inner, new[,] { { "osr.RoleID", "osgrm.RoleID" } });

            QueryFilter filter = new QueryFilter();
            filter.andFilters["osgrm.GroupID"] = GroupID;
            string[] fields = new[]
                                  {
                                      "osgrm.RoleID",
                                      "osgrm.AgentID",
                                      "osr.Powers"
                                  };
            List<string> Roles = data.Query(fields, tables, filter, null, null, null);

            GroupMembersData GMD = GetAgentGroupMemberData(requestingAgentID, GroupID, requestingAgentID);
            const long canViewMemebersBit = 140737488355328L;
            for (int i = 0; i < Roles.Count; i += fields.Length)
            {
                GroupRoleMembersData RoleMember = new GroupRoleMembersData
                {
                    RoleID = UUID.Parse(Roles[i]),
                    MemberID = UUID.Parse(Roles[i + 1])
                };

                // if they are a member, they can see everyone, otherwise, only the roles that are supposed to be shown
                if (GMD != null || ((long.Parse(Roles[i + 2]) & canViewMemebersBit) == canViewMemebersBit || RoleMember.MemberID == requestingAgentID))
                    RoleMembers.Add(RoleMember);
            }

            return RoleMembers;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public GroupNoticeData GetGroupNoticeData(UUID requestingAgentID, UUID noticeID)
        {
            object remoteValue = DoRemote(requestingAgentID, noticeID);
            if (remoteValue != null || m_doRemoteOnly)
            {
                return (GroupNoticeData)remoteValue;
            }

            QueryFilter filter = new QueryFilter();
            filter.andFilters["NoticeID"] = noticeID;
            string[] fields = new string[9]{
                "GroupID",
                "Timestamp",
                "FromName",
                "Subject",
                "ItemID",
                "HasAttachment",
                "Message",
                "AssetType",
                "ItemName"
            };
            List<string> notice = data.Query(fields, "osgroupnotice", filter, null, null, null);

            if (notice.Count != fields.Length)
            {
                return null;
            }

            GroupNoticeData GND = new GroupNoticeData
            {
                GroupID = UUID.Parse(notice[0]),
                NoticeID = noticeID,
                Timestamp = uint.Parse(notice[1]),
                FromName = notice[2],
                Subject = notice[3],
                HasAttachment = int.Parse(notice[5]) == 1
            };
            if (GND.HasAttachment)
            {
                GND.ItemID = UUID.Parse(notice[4]);
                GND.AssetType = (byte)int.Parse(notice[7]);
                GND.ItemName = notice[8];
            }

            return GND;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public GroupNoticeInfo GetGroupNotice(UUID requestingAgentID, UUID noticeID)
        {
            object remoteValue = DoRemote(requestingAgentID, noticeID);
            if (remoteValue != null || m_doRemoteOnly)
                return (GroupNoticeInfo)remoteValue;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["NoticeID"] = noticeID;
            string[] fields = new string[9]{
                "GroupID",
                "Timestamp",
                "FromName",
                "Subject",
                "ItemID",
                "HasAttachment",
                "Message",
                "AssetType",
                "ItemName"
            };
            List<string> notice = data.Query(fields, "osgroupnotice", filter, null, null, null);

            if (notice.Count != fields.Length)
            {
                return null;
            }

            GroupNoticeData GND = new GroupNoticeData
            {
                NoticeID = noticeID,
                Timestamp = uint.Parse(notice[1]),
                FromName = notice[2],
                Subject = notice[3],
                HasAttachment = int.Parse(notice[5]) == 1
            };
            if (GND.HasAttachment)
            {
                GND.ItemID = UUID.Parse(notice[4]);
                GND.AssetType = (byte)int.Parse(notice[7]);
                GND.ItemName = notice[8];
            }

            GroupNoticeInfo info = new GroupNoticeInfo
            {
                BinaryBucket = new byte[0],
                GroupID = UUID.Parse(notice[0]),
                Message = notice[6],
                noticeData = GND
            };

            return (!agentsCanBypassGroupNoticePermsCheck.Contains(requestingAgentID) && !CheckGroupPermissions(requestingAgentID, info.GroupID, (ulong)GroupPowers.ReceiveNotices)) ? null : info;
        }

        private static GroupNoticeData GroupNoticeQueryResult2GroupNoticeData(List<string> result)
        {
            GroupNoticeData GND = new GroupNoticeData
            {
                GroupID = UUID.Parse(result[0]),
                NoticeID = UUID.Parse(result[6]),
                Timestamp = uint.Parse(result[1]),
                FromName = result[2],
                Subject = result[3],
                HasAttachment = int.Parse(result[5]) == 1
            };
            if (GND.HasAttachment)
            {
                GND.ItemID = UUID.Parse(result[4]);
                GND.AssetType = (byte)int.Parse(result[8]);
                GND.ItemName = result[9];
            }
            return GND;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, uint start, uint count, UUID GroupID)
        {
            object remoteValue = DoRemote(requestingAgentID, start, count, GroupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupNoticeData>)remoteValue;

            return GetGroupNotices(requestingAgentID, start, count, new List<UUID>(new[] { GroupID }));
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, uint start, uint count, List<UUID> GroupIDs)
        {
            object remoteValue = DoRemote(requestingAgentID, start, count, GroupIDs);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<GroupNoticeData>)remoteValue;

            List<UUID> groupIDs = new List<UUID>();
            if (!agentsCanBypassGroupNoticePermsCheck.Contains(requestingAgentID))
            {
#if (!ISWIN)
                foreach (UUID GroupID in GroupIDs)
                {
                    if (CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.ReceiveNotices))
                    {
                        groupIDs.Add(GroupID);
                    }
                }
#else
                groupIDs.AddRange(GroupIDs.Where(GroupID => CheckGroupPermissions(requestingAgentID, GroupID, (ulong) GroupPowers.ReceiveNotices)));
#endif
            }
            else
            {
                groupIDs = GroupIDs;
            }

            List<GroupNoticeData> AllNotices = new List<GroupNoticeData>();
            if (groupIDs.Count > 0)
            {

                QueryFilter filter = new QueryFilter();
                filter.orMultiFilters["GroupID"] = new List<object>(groupIDs.Count);
                foreach (UUID groupID in groupIDs)
                {
                    filter.orMultiFilters["GroupID"].Add(groupID);
                }

                Dictionary<string, bool> sort = new Dictionary<string, bool>(1);
                sort["Timestamp"] = false;

                uint? s = null;
                if (start != 0)
                    s = start;
                uint? c = null;
                if (count != 0)
                    c = count;

                List<string> notice = data.Query(new[]{
                    "GroupID",
                    "Timestamp",
                    "FromName",
                    "Subject",
                    "ItemID",
                    "HasAttachment",
                    "NoticeID",
                    "Message",
                    "AssetType",
                    "ItemName"
                }, "osgroupnotice", filter, sort, s, c);

                for (int i = 0; i < notice.Count; i += 10)
                {
                    AllNotices.Add(GroupNoticeQueryResult2GroupNoticeData(notice.GetRange(i, 10)));
                }
            }
            return AllNotices;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public GroupProfileData GetGroupProfile(UUID requestingAgentID, UUID GroupID)
        {
            object remoteValue = DoRemote(requestingAgentID, GroupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (GroupProfileData)remoteValue;

            GroupProfileData profile = new GroupProfileData();

            GroupRecord groupInfo = GetGroupRecord(requestingAgentID, GroupID, null);
            if (groupInfo != null)
            {
                profile.AllowPublish = groupInfo.AllowPublish;
                profile.Charter = groupInfo.Charter;
                profile.FounderID = groupInfo.FounderID;
                profile.GroupID = GroupID;
                profile.GroupMembershipCount =
                    GetGroupMembers(requestingAgentID, GroupID).Count;
                profile.GroupRolesCount = GetGroupRoles(requestingAgentID, GroupID).Count;
                profile.InsigniaID = groupInfo.GroupPicture;
                profile.MaturePublish = groupInfo.MaturePublish;
                profile.MembershipFee = groupInfo.MembershipFee;
                profile.Money = 0; // TODO: Get this from the currency server?
                profile.Name = groupInfo.GroupName;
                profile.OpenEnrollment = groupInfo.OpenEnrollment;
                profile.OwnerRole = groupInfo.OwnerRoleID;
                profile.ShowInList = groupInfo.ShowInList;
            }

            GroupMembershipData memberInfo = GetGroupMembershipData(requestingAgentID,
                                                                                 GroupID,
                                                                                 requestingAgentID);
            if (memberInfo != null)
            {
                profile.MemberTitle = memberInfo.GroupTitle;
                profile.PowersMask = memberInfo.GroupPowers;
            }

            return profile;
        }

        #endregion

        public void Dispose()
        {
        }

        public bool CheckGroupPermissions(UUID AgentID, UUID GroupID, ulong Permissions)
        {
            if (GroupID == UUID.Zero)
                return false;

            if (AgentID == UUID.Zero)
                return false;

            GroupMembersData GMD = GetAgentGroupMemberData(AgentID, GroupID, AgentID);
            GroupRecord record = GetGroupRecord(AgentID, GroupID, null);
            if (Permissions == 0)
            {
                if (GMD != null || record.FounderID == AgentID)
                    return true;
                return false;
            }

            if (record != null && record.FounderID == AgentID)
                return true;

            if (GMD == null)
                return false;

            if ((GMD.AgentPowers & Permissions) != Permissions)
                return false;

            return true;
        }
    }
}