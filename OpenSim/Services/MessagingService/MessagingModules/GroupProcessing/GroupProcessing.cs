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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.MessagingService
{
    public class GroupProcessing : IService
    {
        #region Declares

        protected IRegistryCore m_registry;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
            //Also look for incoming messages to display
            m_registry.RequestModuleInterface<ISyncMessageRecievedService>().OnMessageReceived += OnMessageReceived;
        }

        #endregion

        protected OSDMap OnMessageReceived(OSDMap message)
        {
            //We need to check and see if this is an GroupSessionAgentUpdate
            if (message.ContainsKey("Method") && message["Method"] == "GroupSessionAgentUpdate")
            {
                //COMES IN ON AURORA.SERVER SIDE
                //Send it on to whomever it concerns
                OSDMap innerMessage = (OSDMap) message["Message"];
                if (innerMessage["message"] == "ChatterBoxSessionAgentListUpdates")
                    //ONLY forward on this type of message
                {
                    UUID agentID = message["AgentID"];
                    IEventQueueService eqs = m_registry.RequestModuleInterface<IEventQueueService>();
                    ICapsService caps = m_registry.RequestModuleInterface<ICapsService>();
                    if (caps != null)
                    {
                        IClientCapsService clientCaps = caps.GetClientCapsService(agentID);
                        if (clientCaps != null && clientCaps.GetRootCapsService() != null)
                            eqs.Enqueue(innerMessage, agentID, clientCaps.GetRootCapsService().RegionHandle);
                    }
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "FixGroupRoleTitles")
            {
                //COMES IN ON AURORA.SERVER SIDE FROM REGION
                UUID groupID = message["GroupID"].AsUUID();
                UUID agentID = message["AgentID"].AsUUID();
                UUID roleID = message["RoleID"].AsUUID();
                byte type = (byte) message["Type"].AsInteger();
                IGroupsServiceConnector con = DataManager.RequestPlugin<IGroupsServiceConnector>();
                List<GroupRoleMembersData> members = con.GetGroupRoleMembers(agentID, groupID);
                List<GroupRolesData> roles = con.GetGroupRoles(agentID, groupID);
                GroupRolesData everyone = null;
#if (!ISWIN)
                foreach (GroupRolesData role in roles)
                {
                    if (role.Name == "Everyone") everyone = role;
                }
#else
                foreach (GroupRolesData role in roles.Where(role => role.Name == "Everyone"))
                    everyone = role;
#endif

                List<Interfaces.GridRegion> regionsToBeUpdated = new List<Interfaces.GridRegion>();
                foreach (GroupRoleMembersData data in members)
                {
                    if (data.RoleID == roleID)
                    {
                        //They were affected by the change
                        switch ((GroupRoleUpdate) type)
                        {
                            case GroupRoleUpdate.Create:
                            case GroupRoleUpdate.NoUpdate:
                                //No changes...
                                break;

                            case GroupRoleUpdate.UpdatePowers: //Possible we don't need to send this?
                            case GroupRoleUpdate.UpdateAll:
                            case GroupRoleUpdate.UpdateData:
                            case GroupRoleUpdate.Delete:
                                if (type == (byte) GroupRoleUpdate.Delete)
                                    //Set them to the most limited role since their role is gone
                                    con.SetAgentGroupSelectedRole(data.MemberID, groupID, everyone.RoleID);
                                //Need to update their title inworld
                                ICapsService caps = m_registry.RequestModuleInterface<ICapsService>();
                                if (caps != null)
                                {
                                    IClientCapsService clientCaps = caps.GetClientCapsService(agentID);
                                    if (clientCaps != null && clientCaps.GetRootCapsService() != null)
                                        regionsToBeUpdated.Add(clientCaps.GetRootCapsService().Region);
                                }
                                break;
                        }
                    }
                }
                if (regionsToBeUpdated.Count != 0)
                {
                    ISyncMessagePosterService messagePost = m_registry.RequestModuleInterface<ISyncMessagePosterService>();
                    if (messagePost != null)
                    {
                        foreach (Interfaces.GridRegion region in regionsToBeUpdated)
                        {
                            OSDMap outgoingMessage = new OSDMap();
                            outgoingMessage["Method"] = "ForceUpdateGroupTitles";
                            outgoingMessage["GroupID"] = groupID;
                            outgoingMessage["RoleID"] = roleID;
                            outgoingMessage["RegionID"] = region.RegionID;
                            messagePost.Post(region.ServerURI, outgoingMessage);
                        }
                    }
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "ForceUpdateGroupTitles")
            {
                //COMES IN ON REGION SIDE FROM AURORA.SERVER
                UUID groupID = message["GroupID"].AsUUID();
                UUID roleID = message["RoleID"].AsUUID();
                UUID regionID = message["RegionID"].AsUUID();
                IGroupsModule gm = m_registry.RequestModuleInterface<IGroupsModule>();
                if (gm != null)
                    gm.UpdateUsersForExternalRoleUpdate(groupID, roleID, regionID);
            }
            else if (message.ContainsKey("Method") && message["Method"] == "SendGroupNoticeToUsers")
            {
                //COMES IN ON REGION SIDE FROM AURORA.SERVER
                GroupNoticeInfo notice = new GroupNoticeInfo();
                notice.FromOSD((OSDMap)message["Notice"]);
                IGroupsModule gm = m_registry.RequestModuleInterface<IGroupsModule>();
                if (gm != null)
                    gm.SendGroupNoticeToUsers(null, notice, true);
            }
            return null;
        }
    }
}