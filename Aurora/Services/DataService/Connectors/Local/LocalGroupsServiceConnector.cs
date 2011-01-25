using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using Nini.Config;
using log4net;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
	public class LocalGroupsServiceConnector : IGroupsServiceConnector
	{
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        IGenericData data;

        public void Initialize(IGenericData GenericData, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("GroupsConnector", "LocalConnector") == "LocalConnector")
            {
                data = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                data.ConnectToDatabase(defaultConnectionString);

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
            else
            {
                //Check to make sure that something else exists
                List<string> m_ServerURI = simBase.ApplicationRegistry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                if (m_ServerURI.Count == 0) //Blank, not set up
                {
                    OpenSim.Framework.Console.MainConsole.Instance.Output("[AuroraDataService]: Falling back on local connector for " + "GroupsConnector", "None");
                    data = GenericData;

                    if (source.Configs[Name] != null)
                        defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                    data.ConnectToDatabase(defaultConnectionString);

                    DataManager.DataManager.RegisterPlugin(Name, this);
                }
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
			List<string> Keys = new List<string>();
			Keys.Add("GroupID");
			Keys.Add("Name");
			Keys.Add("Charter");
			Keys.Add("InsigniaID");
			Keys.Add("FounderID");
			Keys.Add("MembershipFee");
			Keys.Add("OpenEnrollment");
			Keys.Add("ShowInList");
			Keys.Add("AllowPublish");
			Keys.Add("MaturePublish");
			Keys.Add("OwnerRoleID");
			List<Object> Values = new List<object>();
			Values.Add(groupID);
			Values.Add(name);
			Values.Add(charter);
			Values.Add(insigniaID);
			Values.Add(founderID);
			Values.Add(membershipFee);
			Values.Add(openEnrollment == true ? 1 : 0);
			Values.Add(showInList == true ? 1 : 0);
			Values.Add(allowPublish == true ? 1 : 0);
			Values.Add(maturePublish == true ? 1 : 0);
			Values.Add(OwnerRoleID);
            data.Insert("osgroup", Keys.ToArray(), Values.ToArray());

            //Add everyone role to group
            AddRoleToGroup(founderID, groupID, UUID.Zero, "Everyone", "Everyone in the group is in the everyone role.", "Member of " + name, EveryonePowers);

            ulong groupPowers = 296868139497678;

            UUID officersRole = UUID.Random();
            //Add officers role to group
            AddRoleToGroup(founderID, groupID, officersRole, "Officers", "The officers of the group, with more powers than regular members.", "Officer of " + name, groupPowers);

            //Add owner role to group
			AddRoleToGroup(founderID, groupID, OwnerRoleID, "Owners", "Owners of " + name, "Owner of " + name, OwnerPowers);

            //Add owner to the group as owner
            AddAgentToGroup(founderID, founderID, groupID, OwnerRoleID);

			SetAgentGroupSelectedRole(founderID, groupID, OwnerRoleID);

			SetAgentActiveGroup(founderID, groupID);
		}

		public void SetAgentActiveGroup(UUID AgentID, UUID GroupID)
		{
			if (data.Query("AgentID", AgentID, "osagent", "*").Count != 0)
				data.Update("osagent", new object[] { GroupID }, new string[] { "ActiveGroupID" }, new string[] { "AgentID" }, new object[] { AgentID });
			else
				data.Insert("osagent", new string[] {
					"AgentID",
                    "ActiveGroupID"
				}, new object[] {
					AgentID,
					GroupID
				});
		}

		public void SetAgentGroupSelectedRole(UUID AgentID, UUID GroupID, UUID RoleID)
		{
			data.Update("osgroupmembership", new object[] { RoleID }, new string[] { "SelectedRoleID" }, new string[] {
				"AgentID",
				"GroupID"
			}, new object[] {
				AgentID,
				GroupID
			});
		}

        public void AddAgentToGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            if (data.Query(new string[] {
				"AgentID",
				"GroupID"
			}, new object[] {
				AgentID,
				GroupID
			}, "osgroupmembership", "*").Count != 0)
            {
                m_log.Error("[AGM]: Agent " + AgentID + " is already in " + GroupID);
                return;
            }
            else
            {
                List<string> Keys = new List<string>();
                Keys.Add("GroupID");
                Keys.Add("AgentID");
                Keys.Add("SelectedRoleID");
                Keys.Add("Contribution");
                Keys.Add("ListInProfile");
                Keys.Add("AcceptNotices");
                List<Object> Values = new List<object>();
                Values.Add(GroupID);
                Values.Add(AgentID);
                Values.Add(RoleID);
                Values.Add(0);
                Values.Add(1);
                Values.Add(1);
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

        public void AddRoleToGroup(UUID requestingAgentID, UUID GroupID, UUID RoleID, string Name, string Description, string Title, ulong Powers)
		{
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.CreateRole))
                return;
            List<string> Keys = new List<string>();
			Keys.Add("GroupID");
			Keys.Add("RoleID");
			Keys.Add("Name");
			Keys.Add("Description");
			Keys.Add("Title");
			Keys.Add("Powers");
			List<Object> Values = new List<object>();
			Values.Add(GroupID);
			Values.Add(RoleID);
			Values.Add(Name);
			Values.Add(Description);
			Values.Add(Title);
			Values.Add(Powers);
			data.Insert("osrole", Keys.ToArray(), Values.ToArray());
		}

		public void UpdateGroup(UUID requestingAgentID, UUID groupID, string charter, int showInList, UUID insigniaID, int membershipFee, int openEnrollment, int allowPublish, int maturePublish)
		{
            if (!CheckGroupPermissions(requestingAgentID, groupID, (ulong)(GroupPowers.ChangeOptions | GroupPowers.ChangeIdentity)))
                return;
            data.Update("osgroup", new object[]
            {
                charter,
				insigniaID,
				membershipFee,
				openEnrollment,
				showInList,
				allowPublish,
				maturePublish
			}, new string[] {
				"Charter",
				"InsigniaID",
				"MembershipFee",
				"OpenEnrollment",
				"ShowInList",
				"AllowPublish",
				"MaturePublish"
			}, new string[] { "GroupID" }, new object[] { groupID });
		}

        public void RemoveRoleFromGroup(UUID requestingAgentID, UUID RoleID, UUID GroupID)
		{
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.DeleteRole))
                return;
            data.Delete("osgrouprolemembership", new string[] {
				"GroupID",
				"RoleID"
			}, new object[] {
				GroupID,
				RoleID
			});
			data.Update("osgroupmembership", new object[] { UUID.Zero }, new string[] { "SelectedRoleID" }, new string[] {
				"GroupID",
				"SelectedRoleID"
			}, new object[] {
				GroupID,
				RoleID
			});
			data.Delete("osrole", new string[] {
				"GroupID",
				"RoleID"
			}, new object[] {
				GroupID,
				RoleID
			});
		}

        public void UpdateRole(UUID requestingAgentID, UUID GroupID, UUID RoleID, string Name, string Desc, string Title, ulong Powers)
		{
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.RoleProperties))
                return;
            List<string> Keys = new List<string>();
			Keys.Add("RoleID");
			if (Name != null)
				Keys.Add("Name");
			if (Desc != null)
				Keys.Add("Description");
			if (Title != null)
				Keys.Add("Title");

			Keys.Add("Powers");

			List<object> Values = new List<object>();
            Values.Add(RoleID);
			if (Name != null)
				Values.Add(Name);
			if (Desc != null)
				Values.Add(Desc);
			if (Title != null)
				Values.Add(Title);

			Values.Add(Powers);

			data.Update("osrole", Values.ToArray(), Keys.ToArray(), new string[] {
				"GroupID",
				"RoleID"
			}, new object[] {
				GroupID,
				RoleID
			});
		}

		public GroupRecord GetGroupRecord(UUID requestingAgentID, UUID GroupID, string GroupName)
		{
			GroupRecord record = new GroupRecord();

			List<string> Keys = new List<string>();
			List<object> Values = new List<object>();
			if (GroupID != UUID.Zero) {
				Keys.Add("GroupID");
				Values.Add(GroupID);
			}
			if ((GroupName != null) && (GroupName != string.Empty)) {
				Keys.Add("Name");
				Values.Add(GroupName);
			}
			List<string> osgroupsData = data.Query(Keys.ToArray(), Values.ToArray(), "osgroup", "GroupID, Name, Charter, InsigniaID, FounderID, MembershipFee, OpenEnrollment, ShowInList, AllowPublish, MaturePublish, OwnerRoleID");
            if (osgroupsData.Count == 0)
                return null;
			record.GroupID = UUID.Parse(osgroupsData[0]);
			record.GroupName = osgroupsData[1];
			record.Charter = osgroupsData[2];
			record.GroupPicture = UUID.Parse(osgroupsData[3]);
			record.FounderID = UUID.Parse(osgroupsData[4]);
			record.MembershipFee = int.Parse(osgroupsData[5]);
			record.OpenEnrollment = int.Parse(osgroupsData[6]) == 1;
			record.ShowInList = int.Parse(osgroupsData[7]) == 1;
			record.AllowPublish = int.Parse(osgroupsData[8]) == 1;
			record.MaturePublish = int.Parse(osgroupsData[9]) == 1;
			record.OwnerRoleID = UUID.Parse(osgroupsData[10]);

			return record;
		}

		public GroupProfileData GetMemberGroupProfile(UUID requestingAgentID, UUID GroupID, UUID AgentID)
		{
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.MemberVisible))
                return new GroupProfileData();

            GroupProfileData GPD = new GroupProfileData();
			GroupRecord record = GetGroupRecord(requestingAgentID, GroupID, null);
			List<string> Membership = data.Query(new string[] {
				"GroupID",
				"AgentID"
			}, new object[] {
				AgentID,
				GroupID
			}, "osgroupmembership", "Contribution, ListInProfile, SelectedRoleID");
			List<string> GroupMemCount = data.Query(new string[] { "GroupID" }, new object[] { GroupID }, "osgroupmembership", "count(AgentID)");
			List<string> GroupRoleCount = data.Query(new string[] { "GroupID" }, new object[] { GroupID }, "osrole", "count(RoleID)");
			List<string> GroupRole = data.Query(new string[] { "RoleID" }, new object[] { Membership[2] }, "osrole", "Name, Powers");

			GPD.AllowPublish = record.AllowPublish;
			GPD.Charter = record.Charter;
			GPD.FounderID = record.FounderID;
			GPD.GroupID = record.GroupID;
			GPD.GroupMembershipCount = int.Parse(GroupMemCount[0]);
			GPD.GroupRolesCount = int.Parse(GroupRoleCount[0]);
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
			GroupRecord record = GetGroupRecord(requestingAgentID, GroupID, null);
			List<string> Membership = data.Query(new string[] {
				"GroupID",
				"AgentID"
			}, new object[] {
				GroupID,
				AgentID
			}, "osgroupmembership", "AcceptNotices, Contribution, ListInProfile, SelectedRoleID");
            if (Membership.Count == 0)
                return null;

            List<string> GroupRole = data.Query(new string[] { "GroupID", "RoleID" }, new object[] { GroupID, Membership[3] }, "osrole", "Title, Powers");
            if (GroupRole.Count == 0)
                return null;
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
			List<string> Groups = data.Query(new string[] {
				"AgentID"
			}, new object[] {
				AgentID
			}, "osgroupmembership", "GroupID");
			List<GroupMembershipData> GroupDatas = new List<GroupMembershipData>();
			foreach (string GroupID in Groups) 
            {
				GroupDatas.Add(GetGroupMembershipData(requestingAgentID, UUID.Parse(GroupID), AgentID));
			}
			return GroupDatas;
		}

		public void SetAgentGroupInfo(UUID requestingAgentID, UUID AgentID, UUID GroupID, int AcceptNotices, int ListInProfile)
		{
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.ChangeIdentity))
                return;
            
            data.Update("osgroupmembership", new object[] {
				AgentID,
				AcceptNotices,
				ListInProfile
			}, new string[] {
				"AgentID",
				"AcceptNotices",
				"ListInProfile"
			}, new string[] {
				"GroupID",
				"AgentID"
			}, new object[] {
				AgentID,
				GroupID
			});
		}

        public void AddAgentGroupInvite(UUID requestingAgentID, UUID inviteID, UUID GroupID, UUID roleID, UUID AgentID, string FromAgentName)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.Invite))
                return;
			data.Delete("osgroupinvite", new string[] {
				"AgentID",
				"GroupID"
			}, new object[] {
				AgentID,
				GroupID
			});
			data.Insert("osgroupinvite", new string[] {
				"InviteID",
				"GroupID",
				"RoleID",
				"AgentID",
                "TMStamp",
                "FromAgentName"
			}, new object[] {
				inviteID,
				GroupID,
				roleID,
				AgentID,
                Util.UnixTimeSinceEpoch(),
                FromAgentName
			});
		}

		public GroupInviteInfo GetAgentToGroupInvite(UUID requestingAgentID, UUID inviteID)
		{
			GroupInviteInfo invite = new GroupInviteInfo();

			List<string> groupInvite = data.Query(new string[] {
				"AgentID",
				"InviteID"
			}, new object[] {
				requestingAgentID,
				inviteID
			}, "osgroupinvite", "*");
			invite.AgentID = UUID.Parse(groupInvite[3]);
			invite.GroupID = UUID.Parse(groupInvite[1]);
			invite.InviteID = UUID.Parse(groupInvite[0]);
            invite.RoleID = UUID.Parse(groupInvite[2]);
            invite.FromAgentName = groupInvite[5];

			return invite;
		}

        public List<GroupInviteInfo> GetGroupInvites(UUID requestingAgentID)
        {
            List<string> groupInvite = data.Query(new string[] {
				"AgentID"
			}, new object[] {
				requestingAgentID
			}, "osgroupinvite", "*");
            List<GroupInviteInfo> invites = new List<GroupInviteInfo>();

            for (int i = 0; i < groupInvite.Count; i += 6)
            {
                GroupInviteInfo invite = new GroupInviteInfo();
                invite.AgentID = UUID.Parse(groupInvite[i + 3]);
                invite.GroupID = UUID.Parse(groupInvite[i + 1]);
                invite.InviteID = UUID.Parse(groupInvite[i]);
                invite.RoleID = UUID.Parse(groupInvite[i + 2]);
                invite.FromAgentName = groupInvite[i + 5];
                invites.Add(invite);
            }

            return invites;
        }

		public void RemoveAgentInvite(UUID requestingAgentID, UUID inviteID)
		{
			data.Delete("osgroupinvite", new string[] { "InviteID" }, new object[] { inviteID });
		}

        public bool RemoveAgentFromGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID)
		{
            if ((!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.RemoveMember)) && (requestingAgentID != AgentID)) //Allow kicking yourself
                return false;
			// 1. If group is agent's active group, change active group to uuidZero
			// 2. Remove Agent from group (osgroupmembership)
			// 3. Remove Agent from all of the groups roles (osgrouprolemembership)
			data.Update("osagent", new object[] { UUID.Zero }, new string[] { "ActiveGroupID" }, new string[] {
				"AgentID",
				"ActiveGroupID"
			}, new object[] {
				AgentID,
				GroupID
			});

			data.Delete("osgrouprolemembership", new string[] {
				"AgentID",
				"GroupID"
			}, new object[] {
				AgentID,
				GroupID
			});
			data.Delete("osgroupmembership", new string[] {
				"AgentID",
				"GroupID"
			}, new object[] {
				AgentID,
				GroupID
			});

            return true;
		}

		public void AddAgentToRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.AssignMember))
            {
                //This isn't an open and shut case, they could be setting the agent to their role, which would allow for AssignMemberLimited
                if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.AssignMemberLimited))
                {
                    m_log.Warn("[AGM]: User " + requestingAgentID + " attempted to add user " + AgentID +
                        " to group " + GroupID + ", but did not have permissions to do so!");
                    return;
                }
            }

            List<string> query = data.Query(new string[] {
				"AgentID",
				"RoleID",
				"GroupID"
			}, new object[] {
				AgentID,
				RoleID,
				GroupID
			}, "osgrouprolemembership", "count(AgentID)");
            //Make sure they arn't already in this role
            if (query[0] == "0")
            {
                data.Insert("osgrouprolemembership", new string[] {
					"GroupID",
					"RoleID",
					"AgentID"
				}, new object[] {
					GroupID,
					RoleID,
					AgentID
				});
            }
		}

		public void RemoveAgentFromRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
		{
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.AssignMember))
                return;
			data.Update("osgroupmembership", new object[] { UUID.Zero }, new string[] { "SelectedRoleID" }, new string[] {
				"AgentID",
				"GroupID",
				"SelectedRoleID"
			}, new object[] {
				AgentID,
				GroupID,
				RoleID
			});
			data.Delete("osgrouprolemembership", new string[] {
				"AgentID",
				"GroupID",
				"RoleID"
			}, new object[] {
				AgentID,
				GroupID,
				RoleID
			});
		}

        public List<DirGroupsReplyData> FindGroups(UUID requestingAgentID, string search, int StartQuery, uint queryflags)
        {
            string whereClause = " Name LIKE '%" + search + "%' LIMIT " + StartQuery + ",50 ";
            List<string> retVal = data.Query(whereClause, "osgroup", "GroupID,Name,ShowInList,AllowPublish,MaturePublish");

            List<DirGroupsReplyData> Reply = new List<DirGroupsReplyData>();
            DirGroupsReplyData dirgroup = new DirGroupsReplyData();
            for (int i = 0; i < retVal.Count; i += 5)
            {
                if (retVal[i + 2] == "0") // (ShowInList param) They don't want to be shown in search.. respect this
                    continue;

                if ((queryflags & (uint)DirectoryManager.DirFindFlags.IncludeMature) != (uint)DirectoryManager.DirFindFlags.IncludeMature)
                    if (retVal[i + 4] == "1")// (MaturePublish param) Check for pg,mature
                        continue;
                dirgroup.groupID = UUID.Parse(retVal[i]);
                dirgroup.groupName = retVal[i + 1];
                dirgroup.members = int.Parse(data.Query(new string[] { "GroupID" }, new object[] { dirgroup.groupID }, "osgroupmembership", "count(AgentID)")[0]);
                Reply.Add(dirgroup);
                dirgroup = new DirGroupsReplyData();
            }
            return Reply;
        }

		public UUID GetAgentActiveGroup(UUID RequestingAgentID, UUID AgentID)
		{
            List<string> groups = data.Query("AgentID", AgentID, "osagent", "ActiveGroupID");
            if (groups.Count != 0)
                return UUID.Parse(groups[0]);
            else
                return UUID.Zero;
		}

		public List<GroupRolesData> GetAgentGroupRoles(UUID requestingAgentID, UUID AgentID, UUID GroupID)
		{
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.None))
                return new List<GroupRolesData>();
            List<GroupRolesData> AgentRoles = new List<GroupRolesData>();
			List<string> RoleIDs = data.Query(new string[] {
				"AgentID",
				"GroupID"
			}, new object[] {
				AgentID,
				GroupID
			}, "osgrouprolemembership", "RoleID");

			foreach (string RoleID in RoleIDs) 
            {
				List<string> Role = data.Query("RoleID", RoleID, "osrole", "Name,Description,Title,Powers");
				GroupRolesData roledata = new GroupRolesData();
				roledata.RoleID = UUID.Parse(RoleID);
				roledata.Name = Role[0];
				roledata.Description = Role[1];
				roledata.Powers = ulong.Parse(Role[3]);
				roledata.Title = Role[2];
				AgentRoles.Add(roledata);
			}
			return AgentRoles;
		}

		public List<GroupRolesData> GetGroupRoles(UUID requestingAgentID, UUID GroupID)
		{
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.None))
                return new List<GroupRolesData>();
            List<GroupRolesData> GroupRoles = new List<GroupRolesData>();
			List<string> Roles = data.Query("GroupID", GroupID, "osrole", "Name,Description,Title,Powers,RoleID");
			for (int i = 0; i < Roles.Count; i += 5) 
            {
                List<string> Count = data.Query(new string[] {
				"GroupID",
				"RoleID"
			}, new object[] {
				GroupID,
                UUID.Parse(Roles[i + 4])
			}, "osgrouprolemembership", "count(AgentID)");
				GroupRolesData roledata = new GroupRolesData();
                roledata.Members = int.Parse(Count[0]);
				roledata.RoleID = UUID.Parse(Roles[i + 4]);
				roledata.Name = Roles[i + 0];
				roledata.Description = Roles[i + 1];
				roledata.Powers = ulong.Parse(Roles[i + 3]);
				roledata.Title = Roles[i + 2];
				GroupRoles.Add(roledata);
			}
			return GroupRoles;
		}

		public List<GroupMembersData> GetGroupMembers(UUID requestingAgentID, UUID GroupID)
		{
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.None))
                return new List<GroupMembersData>();

            List<GroupMembersData> Members = new List<GroupMembersData>();
			List<string> Agents = data.Query("GroupID", GroupID, "osgroupmembership", "AgentID");
			foreach (string Agent in Agents)
            {
				Members.Add(GetAgentGroupMemberData(requestingAgentID, GroupID, UUID.Parse(Agent)));
			}
			return Members;
		}

		public GroupMembersData GetAgentGroupMemberData(UUID requestingAgentID, UUID GroupID, UUID AgentID)
		{
            //Permissions
            List<string> OtherPermiss = data.Query(new string[] {
				"GroupID",
				"AgentID"
			}, new object[] {
				GroupID,
				requestingAgentID
			}, "osgroupmembership", "AcceptNotices, Contribution, ListInProfile, SelectedRoleID");
            if (OtherPermiss.Count == 0)
                return null;

            List<string> Membership = data.Query(new string[] {
				"GroupID",
				"AgentID"
			}, new object[] {
				GroupID,
				AgentID
			}, "osgroupmembership", "AcceptNotices, Contribution, ListInProfile, SelectedRoleID");
            if (Membership.Count == 0)
                return null;
            List<string> GroupRole = data.Query(new string[] { "RoleID" }, new object[] { Membership[3] }, "osrole", "Title, Powers");
            if (GroupRole.Count == 0)
                return null;
            List<string> OwnerRoleID = data.Query(new string[] { "GroupID" }, new object[] { GroupID }, "osgroup", "OwnerRoleID");
			bool IsOwner = data.Query(new string[] {
				"GroupID",
				"RoleID",
				"AgentID"
			}, new object[] {
				GroupID,
				OwnerRoleID[0],
				AgentID
			}, "osgrouprolemembership", "count(AgentID)")[0] != "0";

			GroupMembersData GMD = new GroupMembersData();

			GMD.AcceptNotices = (Membership[0]) == "1";
			GMD.AgentID = AgentID;
			GMD.Contribution = int.Parse(Membership[1]);
			GMD.IsOwner = IsOwner;
			GMD.ListInProfile = (Membership[2]) == "1";
			GMD.AgentPowers = ulong.Parse(GroupRole[1]);
			GMD.Title = GroupRole[0];
            GMD.OnlineStatus = "(Online)";

			return GMD;
		}

		public List<GroupRoleMembersData> GetGroupRoleMembers(UUID requestingAgentID, UUID GroupID)
		{
            List<GroupRoleMembersData> RoleMembers = new List<GroupRoleMembersData>();
            List<string> Roles = data.Query("GroupID", GroupID, "osgrouprolemembership", "RoleID,AgentID");
            GroupMembersData GMD = GetAgentGroupMemberData(requestingAgentID, GroupID, requestingAgentID);
            for (int i = 0; i < Roles.Count; i += 2)
            {
                GroupRoleMembersData RoleMember = new GroupRoleMembersData();
                RoleMember.RoleID = UUID.Parse(Roles[i]);
                RoleMember.MemberID = UUID.Parse(Roles[i + 1]);
                List<string> roleInfo = data.Query("RoleID", RoleMember.RoleID, "osrole", "Powers");
                long canViewMemebersBit = 140737488355328L;
                long canDoBit = long.Parse(roleInfo[0]);
                // if they are a member, they can see everyone, otherwise, only the roles that are supposed to be shown
                if (GMD != null || ((canDoBit & canViewMemebersBit) == canViewMemebersBit || RoleMember.MemberID == requestingAgentID))
                    RoleMembers.Add(RoleMember);
            }
            
            return RoleMembers;
		}

		public GroupNoticeInfo GetGroupNotice(UUID requestingAgentID, UUID noticeID)
		{
            List<string> notice = data.Query("NoticeID", noticeID, "osgroupnotice", "GroupID,Timestamp,FromName,Subject,ItemID,HasAttachment,Message,AssetType,ItemName");
			GroupNoticeData GND = new GroupNoticeData();
			GND.NoticeID = noticeID;
			GND.Timestamp = uint.Parse(notice[1]);
			GND.FromName = notice[2];
			GND.Subject = notice[3];
            GND.HasAttachment = int.Parse(notice[5]) == 1;
            if (GND.HasAttachment)
            {
                GND.ItemID = UUID.Parse(notice[4]);
                GND.AssetType = (byte)int.Parse(notice[7]);
                GND.ItemName = notice[8];
            }

			GroupNoticeInfo info = new GroupNoticeInfo();
			info.BinaryBucket = new byte[0];
			info.GroupID = UUID.Parse(notice[0]);
			info.Message = notice[6];
			info.noticeData = GND;

            if (!CheckGroupPermissions(requestingAgentID, info.GroupID, (ulong)GroupPowers.ReceiveNotices))
                return null;
            return info;
		}

		public List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, UUID GroupID)
        {
            if (!CheckGroupPermissions(requestingAgentID, GroupID, (ulong)GroupPowers.ReceiveNotices))
                return new List<GroupNoticeData>();
			List<GroupNoticeData> AllNotices = new List<GroupNoticeData>();
            List<string> notice = data.Query("GroupID", GroupID, "osgroupnotice", "GroupID,Timestamp,FromName,Subject,ItemID,HasAttachment,NoticeID,Message,AssetType,ItemName");
            for (int i = 0; i < notice.Count; i += 10)
            {
                GroupNoticeData GND = new GroupNoticeData();
                GND.NoticeID = UUID.Parse(notice[i + 6]);
                GND.Timestamp = uint.Parse(notice[i + 1]);
                GND.FromName = notice[i + 2];
                GND.Subject = notice[i + 3];
                GND.HasAttachment = int.Parse(notice[i + 5]) == 1;
                if (GND.HasAttachment)
                {
                    GND.ItemID = UUID.Parse(notice[i + 4]);
                    GND.AssetType = (byte)int.Parse(notice[i + 8]);
                    GND.ItemName = notice[i + 9];
                }

				AllNotices.Add(GND);
			}
			return AllNotices;
		}

        public void AddGroupNotice(UUID requestingAgentID, UUID groupID, UUID noticeID, string fromName, string subject, string message, UUID ItemID, int AssetType, string ItemName)
		{
            if (!CheckGroupPermissions(requestingAgentID, groupID, (ulong)GroupPowers.SendNotices))
                return;
			List<string> Keys = new List<string>();
			Keys.Add("GroupID");
			Keys.Add("NoticeID");
			Keys.Add("Timestamp");
			Keys.Add("FromName");
			Keys.Add("Subject");
			Keys.Add("Message");

            Keys.Add("HasAttachment");
            Keys.Add("ItemID");
            Keys.Add("AssetType");
            Keys.Add("ItemName");

			List<object> Values = new List<object>();
			Values.Add(groupID);
			Values.Add(noticeID);
			Values.Add(((uint)Util.UnixTimeSinceEpoch()));
			Values.Add(fromName);
			Values.Add(subject);
			Values.Add(message);

            Values.Add((ItemID != UUID.Zero) ? 1 : 0);
            Values.Add(ItemID);
            Values.Add(AssetType);
            Values.Add(ItemName);

			data.Insert("osgroupnotice", Keys.ToArray(), Values.ToArray());
        }

        public void AddGroupProposal(UUID agentID, GroupProposalInfo info)
        {
            GenericUtils.AddGeneric(agentID, "Proposal", info.GroupID.ToString(), info.ToOSD(), data);
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
