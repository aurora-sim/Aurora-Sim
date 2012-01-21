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
    public class LocalGroupsServiceConnector : IGroupsServiceConnector
    {
        private IGenericData data;
        List<UUID> agentsCanBypassGroupNoticePermsCheck = new List<UUID>();

        #region IGroupsServiceConnector Members

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
        }

        public string Name
        {
            get { return "IGroupsServiceConnector"; }
        }

        #endregion

        public void CreateGroup(UUID groupID, string name, string charter, bool showInList, UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish, UUID founderID, ulong EveryonePowers, UUID OwnerRoleID, ulong OwnerPowers)
        {
            List<string> Keys = new List<string>
                                    {
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
                                    };
            List<Object> Values = new List<object>
                                      {
                                          groupID,
                                          name.MySqlEscape(50),
                                          charter.MySqlEscape(50),
                                          insigniaID,
                                          founderID,
                                          membershipFee,
                                          openEnrollment ? 1 : 0,
                                          showInList ? 1 : 0,
                                          allowPublish ? 1 : 0,
                                          maturePublish ? 1 : 0,
                                          OwnerRoleID
                                      };
            data.Insert("osgroup", Keys.ToArray(), Values.ToArray());

            //Add everyone role to group
            AddRoleToGroup(founderID, groupID, UUID.Zero, "Everyone", "Everyone in the group is in the everyone role.",
                           "Member of " + name, EveryonePowers);

            ulong groupPowers = 296868139497678;

            UUID officersRole = UUID.Random();
            //Add officers role to group
            AddRoleToGroup(founderID, groupID, officersRole, "Officers",
                           "The officers of the group, with more powers than regular members.", "Officer of " + name,
                           groupPowers);

            //Add owner role to group
            AddRoleToGroup(founderID, groupID, OwnerRoleID, "Owners", "Owners of " + name, "Owner of " + name,
                           OwnerPowers);

            //Add owner to the group as owner
            AddAgentToGroup(founderID, founderID, groupID, OwnerRoleID);

            SetAgentGroupSelectedRole(founderID, groupID, OwnerRoleID);

            SetAgentActiveGroup(founderID, groupID);
        }

        public void UpdateGroup(UUID requestingAgentID, UUID groupID, string charter, int showInList, UUID insigniaID, int membershipFee, int openEnrollment, int allowPublish, int maturePublish)
        {
            if (
                !CheckGroupPermissions(requestingAgentID, groupID,
                                       (ulong) (GroupPowers.ChangeOptions | GroupPowers.ChangeIdentity)))
                return;
            data.Update("osgroup", new object[]
                                       {
                                           charter.MySqlEscape(50),
                                           insigniaID,
                                           membershipFee,
                                           openEnrollment,
                                           showInList,
                                           allowPublish,
                                           maturePublish
                                       }, new[]
                                              {
                                                  "Charter",
                                                  "InsigniaID",
                                                  "MembershipFee",
                                                  "OpenEnrollment",
                                                  "ShowInList",
                                                  "AllowPublish",
                                                  "MaturePublish"
                                              }, new[] {"GroupID"}, new object[] {groupID});
        }

        public void AddGroupNotice(UUID requestingAgentID, UUID groupID, UUID noticeID, string fromName, string subject, string message, UUID ItemID, int AssetType, string ItemName)
        {
            if (!CheckGroupPermissions(requestingAgentID, groupID, (ulong)GroupPowers.SendNotices))
                return;
            List<string> Keys = new List<string>
                                    {
                                        "GroupID",
                                        "NoticeID",
                                        "Timestamp",
                                        "FromName",
                                        "Subject",
                                        "Message",
                                        "HasAttachment",
                                        "ItemID",
                                        "AssetType",
                                        "ItemName"
                                    };

            List<object> Values = new List<object>
                                      {
                                          groupID,
                                          noticeID,
                                          ((uint) Util.UnixTimeSinceEpoch()),
                                          fromName.MySqlEscape(50),
                                          subject.MySqlEscape(50),
                                          message.MySqlEscape(1024),
                                          (ItemID != UUID.Zero) ? 1 : 0,
                                          ItemID,
                                          AssetType,
                                          ItemName.MySqlEscape(50)
                                      };

            data.Insert("osgroupnotice", Keys.ToArray(), Values.ToArray());
        }

        public string SetAgentActiveGroup(UUID AgentID, UUID GroupID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["AgentID"] = AgentID;
            if (data.Query(new string[1] { "*" }, "osagent", filter, null, null, null).Count != 0)
            {
                data.Update("osagent", new object[] { GroupID }, new[] { "ActiveGroupID" }, new[] { "AgentID" }, new object[] { AgentID });
            }
            else
            {
                data.Insert("osagent", new[]{
                    "AgentID",
                    "ActiveGroupID"
                }, new object[]{
                    AgentID,
                    GroupID
                });
            }
            GroupMembersData gdata = GetAgentGroupMemberData(AgentID, GroupID, AgentID);
            return gdata == null ? "" : gdata.Title;
        }

        public UUID GetAgentActiveGroup(UUID RequestingAgentID, UUID AgentID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["AgentID"] = AgentID;
            List<string> groups = data.Query(new string[1] { "ActiveGroupID" }, "osagent", filter, null, null, null);

            return (groups.Count != 0) ? UUID.Parse(groups[0]) : UUID.Zero;
        }

        public string SetAgentGroupSelectedRole(UUID AgentID, UUID GroupID, UUID RoleID)
        {
            data.Update("osgroupmembership", new object[] {RoleID}, new[] {"SelectedRoleID"}, new[]
                                                                                                  {
                                                                                                      "AgentID",
                                                                                                      "GroupID"
                                                                                                  }, new object[]
                                                                                                         {
                                                                                                             AgentID,
                                                                                                             GroupID
                                                                                                         });
            GroupMembersData gdata = GetAgentGroupMemberData(AgentID, GroupID, AgentID);
            return gdata == null ? "" : gdata.Title;
        }

        public void AddAgentToGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            Dictionary<string, object> where = new Dictionary<string, object>(2);
            where["AgentID"] = AgentID;
            where["GroupID"] = GroupID;

            if (data.Query(new string[1] { "*" }, "osgroupmembership", new QueryFilter
            {
                andFilters = where
            }, null, null, null).Count != 0)
            {
                MainConsole.Instance.Error("[AGM]: Agent " + AgentID + " is already in " + GroupID);
                return;
            }
            else
            {
                List<string> Keys = new List<string>{
                    "GroupID",
                    "AgentID",
                    "SelectedRoleID",
                    "Contribution",
                    "ListInProfile",
                    "AcceptNotices"
                };
                List<Object> Values = new List<object> {GroupID, AgentID, RoleID, 0, 1, 1};
                data.Insert("osgroupmembership", Keys.ToArray(), Values.ToArray());
            }

            // Make sure they're in the Everyone role
            AddAgentToRole(requestingAgentID, AgentID, GroupID, UUID.Zero);
            // Make sure they're in specified role, if they were invited
            if (RoleID != UUID.Zero)
                AddAgentToRole(requestingAgentID, AgentID, GroupID, RoleID);
            //Set the role they were invited to as their selected role
            SetAgentGroupSelectedRole(AgentID, GroupID, RoleID);
            SetAgentActiveGroup(AgentID, GroupID);
        }

        public bool RemoveAgentFromGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            if ((!CheckGroupPermissions(requestingAgentID, GroupID, (ulong) GroupPowers.RemoveMember)) &&
                (requestingAgentID != AgentID)) //Allow kicking yourself
                return false;
            // 1. If group is agent's active group, change active group to uuidZero
            // 2. Remove Agent from group (osgroupmembership)
            // 3. Remove Agent from all of the groups roles (osgrouprolemembership)
            data.Update("osagent", new object[] {UUID.Zero}, new[] {"ActiveGroupID"}, new[]
                                                                                          {
                                                                                              "AgentID",
                                                                                              "ActiveGroupID"
                                                                                          }, new object[]
                                                                                                 {
                                                                                                     AgentID,
                                                                                                     GroupID
                                                                                                 });

            data.Delete("osgrouprolemembership", new[]
                                                     {
                                                         "AgentID",
                                                         "GroupID"
                                                     }, new object[]
                                                            {
                                                                AgentID,
                                                                GroupID
                                                            });
            data.Delete("osgroupmembership", new[]
                                                 {
                                                     "AgentID",
                                                     "GroupID"
                                                 }, new object[]
                                                        {
                                                            AgentID,
                                                            GroupID
                                                        });

            return true;
        }

        public void AddRoleToGroup(UUID requestingAgentID, UUID GroupID, UUID RoleID, string Name, string Description, string Title, ulong Powers)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong) GroupPowers.CreateRole))
                return;
            List<string> Keys = new List<string> {"GroupID", "RoleID", "Name", "Description", "Title", "Powers"};
            List<Object> Values = new List<object>
                                      {GroupID, RoleID, Name.MySqlEscape(50), Description.MySqlEscape(50), Title, Powers};
            data.Insert("osrole", Keys.ToArray(), Values.ToArray());
        }

        public void UpdateRole(UUID requestingAgentID, UUID GroupID, UUID RoleID, string Name, string Desc, string Title, ulong Powers)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong) GroupPowers.RoleProperties))
                return;
            List<string> Keys = new List<string> {"RoleID"};
            if (Name != null)
                Keys.Add("Name");
            if (Desc != null)
                Keys.Add("Description");
            if (Title != null)
                Keys.Add("Title");

            Keys.Add("Powers");

            List<object> Values = new List<object> {RoleID};
            if (Name != null)
                Values.Add(Name.MySqlEscape(512));
            if (Desc != null)
                Values.Add(Desc.MySqlEscape(512));
            if (Title != null)
                Values.Add(Title.MySqlEscape(512));

            Values.Add(Powers);

            data.Update("osrole", Values.ToArray(), Keys.ToArray(), new[]
                                                                        {
                                                                            "GroupID",
                                                                            "RoleID"
                                                                        }, new object[]
                                                                               {
                                                                                   GroupID,
                                                                                   RoleID
                                                                               });
        }

        public void RemoveRoleFromGroup(UUID requestingAgentID, UUID RoleID, UUID GroupID)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong) GroupPowers.DeleteRole))
                return;
            data.Delete("osgrouprolemembership", new[]
                                                     {
                                                         "GroupID",
                                                         "RoleID"
                                                     }, new object[]
                                                            {
                                                                GroupID,
                                                                RoleID
                                                            });
            data.Update("osgroupmembership", new object[] {UUID.Zero}, new[] {"SelectedRoleID"}, new[]
                                                                                                     {
                                                                                                         "GroupID",
                                                                                                         "SelectedRoleID"
                                                                                                     }, new object[]
                                                                                                            {
                                                                                                                GroupID,
                                                                                                                RoleID
                                                                                                            });
            data.Delete("osrole", new[]
                                      {
                                          "GroupID",
                                          "RoleID"
                                      }, new object[]
                                             {
                                                 GroupID,
                                                 RoleID
                                             });
        }

        public void AddAgentToRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong) GroupPowers.AssignMember))
            {
                //This isn't an open and shut case, they could be setting the agent to their role, which would allow for AssignMemberLimited
                if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong) GroupPowers.AssignMemberLimited))
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
            if (uint.Parse(data.Query(new string[1] { "COUNT(AgentID)" }, "osgrouprolemembership", filter, null, null, null)[0]) == 0)
            {
                data.Insert("osgrouprolemembership", new[]{
                    "GroupID",
                    "RoleID",
                    "AgentID"
                }, new object[]{
                    GroupID,
                    RoleID,
                    AgentID
                });
            }
        }

        public void RemoveAgentFromRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong) GroupPowers.AssignMember))
                return;
            data.Update("osgroupmembership", new object[] {UUID.Zero}, new[] {"SelectedRoleID"}, new[]
                                                                                                     {
                                                                                                         "AgentID",
                                                                                                         "GroupID",
                                                                                                         "SelectedRoleID"
                                                                                                     }, new object[]
                                                                                                            {
                                                                                                                AgentID,
                                                                                                                GroupID,
                                                                                                                RoleID
                                                                                                            });
            data.Delete("osgrouprolemembership", new[]
                                                     {
                                                         "AgentID",
                                                         "GroupID",
                                                         "RoleID"
                                                     }, new object[]
                                                            {
                                                                AgentID,
                                                                GroupID,
                                                                RoleID
                                                            });
        }

        public void SetAgentGroupInfo(UUID requestingAgentID, UUID AgentID, UUID GroupID, int AcceptNotices, int ListInProfile)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong) GroupPowers.ChangeIdentity))
                return;

            data.Update("osgroupmembership", new object[]
                                                 {
                                                     AgentID,
                                                     AcceptNotices,
                                                     ListInProfile
                                                 }, new[]
                                                        {
                                                            "AgentID",
                                                            "AcceptNotices",
                                                            "ListInProfile"
                                                        }, new[]
                                                               {
                                                                   "GroupID",
                                                                   "AgentID"
                                                               }, new object[]
                                                                      {
                                                                          AgentID,
                                                                          GroupID
                                                                      });
        }

        public void AddAgentGroupInvite(UUID requestingAgentID, UUID inviteID, UUID GroupID, UUID roleID, UUID AgentID, string FromAgentName)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong) GroupPowers.Invite))
                return;
            data.Delete("osgroupinvite", new[]
                                             {
                                                 "AgentID",
                                                 "GroupID"
                                             }, new object[]
                                                    {
                                                        AgentID,
                                                        GroupID
                                                    });
            data.Insert("osgroupinvite", new[]
                                             {
                                                 "InviteID",
                                                 "GroupID",
                                                 "RoleID",
                                                 "AgentID",
                                                 "TMStamp",
                                                 "FromAgentName"
                                             }, new object[]
                                                    {
                                                        inviteID,
                                                        GroupID,
                                                        roleID,
                                                        AgentID,
                                                        Util.UnixTimeSinceEpoch(),
                                                        FromAgentName.MySqlEscape(50)
                                                    });
        }

        public void RemoveAgentInvite(UUID requestingAgentID, UUID inviteID)
        {
            data.Delete("osgroupinvite", new[] {"InviteID"}, new object[] {inviteID});
        }

        public void AddGroupProposal(UUID agentID, GroupProposalInfo info)
        {
            GenericUtils.AddGeneric(agentID, "Proposal", info.GroupID.ToString(), info.ToOSD(), data);
        }

        public uint GetNumberOfGroupNotices(UUID requestingAgentID, UUID GroupID)
        {
            List<UUID> GroupIDs = new List<UUID>();
            GroupIDs.Add(GroupID);
            return GetNumberOfGroupNotices(requestingAgentID, GroupIDs);
        }

        public uint GetNumberOfGroupNotices(UUID requestingAgentID, List<UUID> GroupIDs)
        {
            List<UUID> groupIDs = new List<UUID>();
            if (!agentsCanBypassGroupNoticePermsCheck.Contains(requestingAgentID))
            {
                foreach (UUID GroupID in GroupIDs)
                {
                    if (CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.ReceiveNotices))
                    {
                        groupIDs.Add(GroupID);
                    }
                }
            }
            else
            {
                groupIDs = GroupIDs;
            }

            QueryFilter filter = new QueryFilter();
            filter.orMultiFilters["GroupID"] = new List<object>(groupIDs.Count);
            foreach (UUID GroupID in groupIDs)
            {
                filter.orMultiFilters["GroupID"].Add(GroupID);
            }

            return uint.Parse(data.Query(new string[1] { "COUNT(NoticeID)" }, "osgroupnotice", filter, null, null, null)[0]);
        }

        public uint GetNumberOfGroups(UUID requestingAgentID, Dictionary<string, bool> boolFields)
        {
            QueryFilter filter = new QueryFilter();

            string[] BoolFields = { "OpenEnrollment", "ShowInList", "AllowPublish", "MaturePublish" };
            foreach (string field in BoolFields)
            {
                if (boolFields.ContainsKey(field) == true)
                {
                    filter.andFilters[field] = boolFields[field] ? "1" : "0";
                }
            }

            return uint.Parse(data.Query(new string[1] { "COUNT(GroupID)" }, "osgroup", filter, null, null, null)[0]);
        }

        private static GroupRecord GroupRecordQueryResult2GroupRecord(List<String> result){
            return new GroupRecord{
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

        public GroupRecord GetGroupRecord(UUID requestingAgentID, UUID GroupID, string GroupName)
        {
            GroupRecord record = new GroupRecord();

            QueryFilter filter = new QueryFilter();

            List<string> Keys = new List<string>();
            List<object> Values = new List<object>();
            if (GroupID != UUID.Zero)
            {
                filter.andFilters["GroupID"] = GroupID;
            }
            if (!string.IsNullOrEmpty(GroupName))
            {
                filter.andFilters["Name"] = GroupName.MySqlEscape(50);
            }
            if (filter.Count == 0)
            {
                return null;
            }
            List<string> osgroupsData = data.Query(new string[11]{
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

        public List<GroupRecord> GetGroupRecords(UUID requestingAgentID, uint start, uint count, Dictionary<string, bool> sort, Dictionary<string, bool> boolFields)
        {
//            List<string> filter = new List<string>();

            string[] sortAndBool = { "OpenEnrollment", "MaturePublish" };
            string[] BoolFields = { "OpenEnrollment", "ShowInList", "AllowPublish", "MaturePublish" };
            string[] SortFields = { "Name", "MembershipFee", "OpenEnrollment", "MaturePublish" };

            foreach (string field in sortAndBool)
            {
                if (boolFields.ContainsKey(field) == true && sort.ContainsKey(field) == true)
                {
                    sort.Remove(field);
                }
            }

            QueryFilter filter = new QueryFilter();

            foreach (string field in BoolFields)
            {
                if (boolFields.ContainsKey(field) == true)
                {
                    filter.andFilters[field] = boolFields[field] ? "1" : "0";
                }
            }

            List<GroupRecord> Reply = new List<GroupRecord>();

            List<string> osgroupsData = data.Query(new string[]{
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
            for (int i = 0; i < osgroupsData.Count; i+= 11)
            {
                Reply.Add(GroupRecordQueryResult2GroupRecord(osgroupsData.GetRange(i, 11)));
            }
            return Reply;
        }

        public List<GroupRecord> GetGroupRecords(UUID requestingAgentID, List<UUID> GroupIDs)
        {
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

            List<string> osgroupsData = data.Query(new string[11]{
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

        public GroupProfileData GetMemberGroupProfile(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong) GroupPowers.MemberVisible))
                return new GroupProfileData();

            GroupProfileData GPD = new GroupProfileData();
            GroupRecord record = GetGroupRecord(requestingAgentID, GroupID, null);

            QueryFilter filter1 = new QueryFilter();
            filter1.andFilters["GroupID"] = AgentID; // yes these look the wrong way around
            filter1.andFilters["AgentID"] = GroupID; // but they were like that when I got here! ~ SignpostMarv

            QueryFilter filter2 = new QueryFilter();
            filter2.andFilters["GroupID"] = GroupID;

            List<string> Membership = data.Query(new string[3]{
                "Contribution",
                "ListInProfile",
                "SelectedRoleID"
            }, "osgroupmembership", filter1, null, null, null);

            int GroupMemCount = int.Parse(data.Query(new string[] { "COUNT(AgentID)" }, "osgroupmembership", filter2, null, null, null)[0]);

            int GroupRoleCount = int.Parse(data.Query(new string[] { "COUNT(RoleID)" }, "osrole", filter2, null, null, null)[0]);

            QueryFilter filter3 = new QueryFilter();
            filter3.andFilters["RoleID"] = Membership[2];
            List<string> GroupRole = data.Query(new string[] {
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

        public GroupMembershipData GetGroupMembershipData(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            GroupMembershipData GMD = new GroupMembershipData();
            if (GroupID == UUID.Zero)
            {
                GroupID = GetAgentActiveGroup(requestingAgentID, AgentID);
            }
            GroupRecord record = GetGroupRecord(requestingAgentID, GroupID, null);

            QueryFilter filter = new QueryFilter();
            filter.andFilters["GroupID"] = GroupID;
            filter.andFilters["AgentID"] = AgentID;

            List<string> Membership = data.Query(new string[]{
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

            List<string> GroupRole = data.Query(new string[]{
                "Title",
                "Powers"
            }, "osrole", filter, null, null, null);

            if (GroupRole.Count != 2)
            {
                return null;
            }

            GMD.AcceptNotices = int.Parse(Membership[0]) == 1;
            //TODO: Figure out what this is and its effects if false
            GMD.Active = true;
            GMD.ActiveRole = UUID.Parse(Membership[3]);
            GMD.AllowPublish = record.AllowPublish;
            GMD.Charter = record.Charter;
            GMD.Contribution = int.Parse(Membership[1]);
            GMD.FounderID = record.FounderID;
            GMD.GroupID = record.GroupID;
            GMD.GroupName = record.GroupName;
            GMD.GroupPicture = record.GroupPicture;
            GMD.GroupPowers = ulong.Parse(GroupRole[1]);
            GMD.GroupTitle = GroupRole[0];
            GMD.ListInProfile = int.Parse(Membership[2]) == 1;
            GMD.MaturePublish = record.MaturePublish;
            GMD.MembershipFee = record.MembershipFee;
            GMD.OpenEnrollment = record.OpenEnrollment;
            GMD.ShowInList = record.ShowInList;

            return GMD;
        }

        public List<GroupMembershipData> GetAgentGroupMemberships(UUID requestingAgentID, UUID AgentID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["AgentID"] = AgentID;

            List<string> Groups = data.Query(new string[1] { "GroupID" }, "osgroupmembership", filter, null, null, null);

#if (!ISWIN)
            List<GroupMembershipData> list = new List<GroupMembershipData>();
            foreach (string groupId in Groups)
            {
                GroupMembershipData temp = GetGroupMembershipData(requestingAgentID, UUID.Parse(groupId), AgentID);
                if (temp != null) list.Add(temp);
            }
            return list;
#else
            return Groups.Select(GroupID => GetGroupMembershipData(requestingAgentID, UUID.Parse(GroupID), AgentID)).Where(temp => temp != null).ToList();
#endif
        }

        public GroupInviteInfo GetAgentToGroupInvite(UUID requestingAgentID, UUID inviteID)
        {
            GroupInviteInfo invite = new GroupInviteInfo();

            Dictionary<string, object> where = new Dictionary<string, object>(2);
            where["AgentID"] = requestingAgentID;
            where["InviteID"] = inviteID;

            List<string> groupInvite = data.Query(new string[1] { "*" }, "osgroupinvite", new QueryFilter{
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

        public List<GroupInviteInfo> GetGroupInvites(UUID requestingAgentID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["AgentID"] = requestingAgentID;

            List<string> groupInvite = data.Query(new string[1] { "*" }, "osgroupinvite", filter, null, null, null);

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

        public GroupMembersData GetAgentGroupMemberData(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
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

        public List<GroupMembersData> GetGroupMembers(UUID requestingAgentID, UUID GroupID)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.None))
            {
                return new List<GroupMembersData>(0);
            }

            QueryFilter filter = new QueryFilter();
            filter.andFilters["GroupID"] = GroupID;
            List<string> Agents = data.Query(new string[1] { "AgentID" }, "osgroupmembership", filter, null, null, null);
#if (!ISWIN)
            List<GroupMembersData> list = new List<GroupMembersData>();
            foreach (string agent in Agents)
                list.Add(GetAgentGroupMemberData(requestingAgentID, GroupID, UUID.Parse(agent)));
            return list;
#else
            return Agents.Select(Agent => GetAgentGroupMemberData(requestingAgentID, GroupID, UUID.Parse(Agent))).ToList();
#endif
        }

        public List<DirGroupsReplyData> FindGroups(UUID requestingAgentID, string search, int StartQuery, uint queryflags)
        {
            QueryFilter filter = new QueryFilter();
            filter.andLikeFilters["Name"] = "%" + search.MySqlEscape(50) + "%";

            List<string> retVal = data.Query(new string[5]{
                "GroupID",
                "Name",
                "ShowInList",
                "AllowPublish",
                "MaturePublish"
            }, "osgroup", filter, null, (uint)StartQuery, 50);

            List<DirGroupsReplyData> Reply = new List<DirGroupsReplyData>();
            DirGroupsReplyData dirgroup;

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

                dirgroup = new DirGroupsReplyData();

                dirgroup.groupID = UUID.Parse(retVal[i]);
                dirgroup.groupName = retVal[i + 1];

                filter = new QueryFilter();
                filter.andFilters["GroupID"] = dirgroup.groupID;
                dirgroup.members = int.Parse(data.Query(new string[1] { "COUNT(AgentID)" }, "osgroupmembership", filter, null, null, null)[0]);

                Reply.Add(dirgroup);
            }
            return Reply;
        }

        public List<GroupRolesData> GetAgentGroupRoles(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.None))
            {
                return new List<GroupRolesData>(0);
            }

            QueryFilter filter = new QueryFilter();
            filter.andFilters["AgentID"] = AgentID;
            filter.andFilters["GroupID"] = GroupID;

            List<string> RoleIDs = data.Query(new string[1] { "RoleID" }, "osgrouprolemembership", filter, null, null, null);



            filter = new QueryFilter();

            List<GroupRolesData> RolesData = new List<GroupRolesData>();
            List<string> Role;

            foreach (string RoleID in RoleIDs)
            {
                filter.andFilters["RoleID"] = RoleID;
                Role = data.Query(new string[4]{
                    "Name",
                    "Description",
                    "Title",
                    "Powers"
                }, "osrole", filter, null, null, null);
                RolesData.Add(new GroupRolesData
                {
                    RoleID = UUID.Parse(RoleID),
                    Name = Role[0],
                    Description = Role[1],
                    Powers = ulong.Parse(Role[3]),
                    Title = Role[2]
                });
            }

            return RolesData;
        }

        public List<GroupRolesData> GetGroupRoles(UUID requestingAgentID, UUID GroupID)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.None))
            {
                return new List<GroupRolesData>(0);
            }

            List<GroupRolesData> GroupRoles = new List<GroupRolesData>();

            QueryFilter rolesFilter = new QueryFilter();
            rolesFilter.andFilters["GroupID"] = GroupID;
            List<string> Roles = data.Query(new string[5]{
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
                int Count = int.Parse(data.Query(new string[1] { "COUNT(AgentID)" }, "osgrouprolemembership", filter, null, null, null)[0]);

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

        public List<GroupRoleMembersData> GetGroupRoleMembers(UUID requestingAgentID, UUID GroupID)
        {
            List<GroupRoleMembersData> RoleMembers = new List<GroupRoleMembersData>();

            QueryFilter filter = new QueryFilter();
            filter.andFilters["GroupID"] = GroupID;
            List<string> Roles = data.Query(new string[2]{
                "RoleID",
                "AgentID"
            }, "osgrouprolemembership", filter, null, null, null);

            GroupMembersData GMD = GetAgentGroupMemberData(requestingAgentID, GroupID, requestingAgentID);
            long canViewMemebersBit = 140737488355328L;
            long canDoBit;
            for (int i = 0; i < Roles.Count; i += 2)
            {
                GroupRoleMembersData RoleMember = new GroupRoleMembersData
                {
                    RoleID = UUID.Parse(Roles[i]),
                    MemberID = UUID.Parse(Roles[i + 1])
                };
                filter.andFilters.Remove("GroupID");
                filter.andFilters["RoleID"] = RoleMember.RoleID;
                List<string> roleInfo = data.Query(new string[1] { "Powers" }, "osrole", filter, null, null, null);
                canDoBit = long.Parse(roleInfo[0]);
                // if they are a member, they can see everyone, otherwise, only the roles that are supposed to be shown
                if (GMD != null || ((canDoBit & canViewMemebersBit) == canViewMemebersBit || RoleMember.MemberID == requestingAgentID))
                {
                    RoleMembers.Add(RoleMember);
                }
            }

            return RoleMembers;
        }

        public GroupNoticeInfo GetGroupNotice(UUID requestingAgentID, UUID noticeID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["NoticeID"] = noticeID;
            List<string> notice = data.Query(new string[9]{
                "GroupID",
                "Timestamp",
                "FromName",
                "Subject",
                "ItemID",
                "HasAttachment",
                "Message",
                "AssetType",
                "ItemName"
            }, "osgroupnotice", filter, null, null, null);

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
                GND.AssetType = (byte) int.Parse(notice[7]);
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

        public List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, uint start, uint count, UUID GroupID)
        {
            List<UUID> GroupIDs = new List<UUID>();
            GroupIDs.Add(GroupID);
            return GetGroupNotices(requestingAgentID, start, count, GroupIDs);
        }

        public List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, uint start, uint count, List<UUID> GroupIDs)
        {
            List<UUID> groupIDs = new List<UUID>();
            if (!agentsCanBypassGroupNoticePermsCheck.Contains(requestingAgentID))
            {
                foreach (UUID GroupID in GroupIDs)
                {
                    if (CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.ReceiveNotices))
                    {
                        groupIDs.Add(GroupID);
                    }
                }
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

                Dictionary<string, bool> sort = new Dictionary<string,bool>(1);
                sort["Timestamp"] = false;

                List<string> notice = data.Query(new string[]{
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
                }, "osgroupnotice", filter, sort, start, count);

                for (int i = 0; i < notice.Count; i += 10)
                {
                    AllNotices.Add(GroupNoticeQueryResult2GroupNoticeData(notice.GetRange(i, 10)));
                }
            }
            return AllNotices;
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