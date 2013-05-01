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

using Aurora.Framework;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Other;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System.Collections.Generic;
using FriendInfo = Aurora.Framework.Services.FriendInfo;

namespace Aurora.Services
{
    public class FriendsProcessing : IService
    {
        #region Declares

        protected IRegistryCore m_registry;
        protected IAgentInfoService m_agentInfoService;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            registry.RequestModuleInterface<ISimulationBase>().EventManager.RegisterEventHandler("UserStatusChange",
                                                                                                 OnGenericEvent);
        }

        public void FinishedStartup()
        {
            //Also look for incoming messages to display
            m_agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
            m_registry.RequestModuleInterface<ISyncMessageRecievedService>().OnMessageReceived += OnMessageReceived;
        }

        #endregion

        protected object OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "UserStatusChange")
            {
                //A user has logged in or out... we need to update friends lists across the grid

                System.Threading.WaitCallback delayed = state =>
                {
                    System.Threading.Thread.Sleep(5000);
                    SendFriendStatusChanges(parameters);
                };

                System.Threading.ThreadPool.QueueUserWorkItem(delayed);
            }
            return null;
        }

        private void SendFriendStatusChanges(object parameters)
        {
            ISyncMessagePosterService asyncPoster = m_registry.RequestModuleInterface<ISyncMessagePosterService>();
            IFriendsService friendsService = m_registry.RequestModuleInterface<IFriendsService>();
            if (asyncPoster != null && friendsService != null)
            {
                //Get all friends
                object[] info = (object[])parameters;
                UUID us = UUID.Parse(info[0].ToString());
                bool isOnline = bool.Parse(info[1].ToString());

                List<FriendInfo> friends = friendsService.GetFriends(us);
                List<UUID> OnlineFriends = new List<UUID>();
                foreach (FriendInfo friend in friends)
                {
                    if (friend.TheirFlags == -1 || friend.MyFlags == -1)
                        continue; //Not validiated yet!
                    UUID FriendToInform = UUID.Zero;
                    if (!UUID.TryParse(friend.Friend, out FriendToInform))
                        continue;

                    UserInfo user = m_agentInfoService.GetUserInfo(friend.Friend);
                    //Now find their caps service so that we can find where they are root (and if they are logged in)
                    if (user != null && user.IsOnline)
                    {
                        //Find the root agent
                        OnlineFriends.Add(FriendToInform);
                        //Post!
                        asyncPoster.Post(user.CurrentRegionURI,
                                         SyncMessageHelper.AgentStatusChange(us, FriendToInform, isOnline));
                    }
                }
                //If the user is coming online, send all their friends online statuses to them
                if (isOnline)
                {
                    UserInfo user = m_agentInfoService.GetUserInfo(us.ToString());
                    if (user != null)
                        asyncPoster.Post(user.CurrentRegionURI,
                                         SyncMessageHelper.AgentStatusChanges(OnlineFriends, us, true));
                }
            }
        }

        protected OSDMap OnMessageReceived(OSDMap message)
        {
            ISyncMessagePosterService asyncPost = m_registry.RequestModuleInterface<ISyncMessagePosterService>();
            //We need to check and see if this is an AgentStatusChange
            if (message.ContainsKey("Method") && message["Method"] == "AgentStatusChange")
            {
                OSDMap innerMessage = (OSDMap) message["Message"];
                //We got a message, now pass it on to the clients that need it
                UUID AgentID = innerMessage["AgentID"].AsUUID();
                UUID FriendToInformID = innerMessage["FriendToInformID"].AsUUID();
                bool NewStatus = innerMessage["NewStatus"].AsBoolean();

                //Do this since IFriendsModule is a scene module, not a ISimulationBase module (not interchangable)
                ISceneManager manager = m_registry.RequestModuleInterface<ISceneManager>();
                if (manager != null && manager.Scene != null)
                {
                    IFriendsModule friendsModule = manager.Scene.RequestModuleInterface<IFriendsModule>();
                    if (friendsModule != null)
                    {
                        //Send the message
                        friendsModule.SendFriendsStatusMessage(FriendToInformID, AgentID, NewStatus);
                    }
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "AgentStatusChanges")
            {
                OSDMap innerMessage = (OSDMap) message["Message"];
                //We got a message, now pass it on to the clients that need it
                List<UUID> AgentIDs = ((OSDArray) innerMessage["AgentIDs"]).ConvertAll<UUID>((o) => o);
                UUID FriendToInformID = innerMessage["FriendToInformID"].AsUUID();
                bool NewStatus = innerMessage["NewStatus"].AsBoolean();

                //Do this since IFriendsModule is a scene module, not a ISimulationBase module (not interchangable)
                ISceneManager manager = m_registry.RequestModuleInterface<ISceneManager>();
                if (manager != null && manager.Scene != null)
                {
                    IFriendsModule friendsModule = manager.Scene.RequestModuleInterface<IFriendsModule>();
                    if (friendsModule != null)
                    {
                        //Send the message
                        foreach (UUID agentID in AgentIDs)
                            friendsModule.SendFriendsStatusMessage(FriendToInformID, agentID, NewStatus);
                    }
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "FriendGrantRights")
            {
                OSDMap body = (OSDMap) message["Message"];
                UUID targetID = body["Target"].AsUUID();
                IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
                UserInfo info;
                if (agentInfoService != null && (info = agentInfoService.GetUserInfo(targetID.ToString())) != null &&
                    info.IsOnline)
                {
                    //Forward the message
                    asyncPost.Post(info.CurrentRegionURI, message);
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "FriendshipOffered")
            {
                OSDMap body = (OSDMap) message["Message"];
                UUID targetID = body["Friend"].AsUUID();
                IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
                UserInfo info;
                if (agentInfoService != null && (info = agentInfoService.GetUserInfo(targetID.ToString())) != null &&
                    info.IsOnline)
                {
                    //Forward the message
                    asyncPost.Post(info.CurrentRegionURI, message);
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "FriendTerminated")
            {
                OSDMap body = (OSDMap) message["Message"];
                UUID targetID = body["ExFriend"].AsUUID();
                IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
                UserInfo info;
                if (agentInfoService != null && (info = agentInfoService.GetUserInfo(targetID.ToString())) != null &&
                    info.IsOnline)
                {
                    //Forward the message
                    asyncPost.Post(info.CurrentRegionURI, message);
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "FriendshipDenied")
            {
                OSDMap body = (OSDMap) message["Message"];
                UUID targetID = body["FriendID"].AsUUID();
                IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
                UserInfo info;
                if (agentInfoService != null && (info = agentInfoService.GetUserInfo(targetID.ToString())) != null &&
                    info.IsOnline)
                {
                    //Forward the message
                    asyncPost.Post(info.CurrentRegionURI, message);
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "FriendshipApproved")
            {
                OSDMap body = (OSDMap) message["Message"];
                UUID targetID = body["FriendID"].AsUUID();
                IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
                UserInfo info;
                if (agentInfoService != null && (info = agentInfoService.GetUserInfo(targetID.ToString())) != null &&
                    info.IsOnline)
                {
                    //Forward the message
                    asyncPost.Post(info.CurrentRegionURI, message);
                }
            }
            return null;
        }
    }
}