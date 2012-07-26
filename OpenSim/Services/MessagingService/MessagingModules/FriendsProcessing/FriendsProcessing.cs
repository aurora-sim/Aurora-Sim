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
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.MessagingService
{
    public class FriendsProcessing : IService
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
            registry.RequestModuleInterface<ISimulationBase>().EventManager.RegisterEventHandler("UserStatusChange",
                                                                                                 OnGenericEvent);
        }

        public void FinishedStartup()
        {
            //Also look for incoming messages to display
            m_registry.RequestModuleInterface<IAsyncMessageRecievedService>().OnMessageReceived += OnMessageReceived;
        }

        #endregion

        protected object OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "UserStatusChange")
            {
                //A user has logged in or out... we need to update friends lists across the grid

                IAsyncMessagePostService asyncPoster = m_registry.RequestModuleInterface<IAsyncMessagePostService>();
                IFriendsService friendsService = m_registry.RequestModuleInterface<IFriendsService>();
                ICapsService capsService = m_registry.RequestModuleInterface<ICapsService>();
                IGridService gridService = m_registry.RequestModuleInterface<IGridService>();
                if (asyncPoster != null && friendsService != null && capsService != null && gridService != null)
                {
                    //Get all friends
                    object[] info = (object[]) parameters;
                    UUID us = UUID.Parse(info[0].ToString());
                    bool isOnline = bool.Parse(info[1].ToString());

                    List<FriendInfo> friends = friendsService.GetFriends(us);
                    List<UUID> OnlineFriends = new List<UUID>();
                    foreach (FriendInfo friend in friends)
                    {
                        if (friend.TheirFlags == -1 || friend.MyFlags == -1)
                            continue; //Not validiated yet!
                        UUID FriendToInform = UUID.Zero;
                        string url, first, last, secret;
                        if (!UUID.TryParse(friend.Friend, out FriendToInform))
                            HGUtil.ParseUniversalUserIdentifier(friend.Friend, out FriendToInform, out url, out first,
                                                                out last, out secret);
                        //Now find their caps service so that we can find where they are root (and if they are logged in)
                        IClientCapsService clientCaps = capsService.GetClientCapsService(FriendToInform);
                        if (clientCaps != null)
                        {
                            //Find the root agent
                            IRegionClientCapsService regionClientCaps = clientCaps.GetRootCapsService();
                            if (regionClientCaps != null)
                            {
                                OnlineFriends.Add(FriendToInform);
                                //Post!
                                asyncPoster.Post(regionClientCaps.RegionHandle,
                                                 SyncMessageHelper.AgentStatusChange(us, FriendToInform, isOnline));
                            }
                            else
                            {
                                //If they don't have a root agent, wtf happened?
                                capsService.RemoveCAPS(clientCaps.AgentID);
                            }
                        }
                        else
                        {
                            IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
                            if (agentInfoService != null)
                            {
                                UserInfo friendinfo = agentInfoService.GetUserInfo(FriendToInform.ToString());
                                if (friendinfo != null && friendinfo.IsOnline)
                                {
                                    OnlineFriends.Add(FriendToInform);
                                    //Post!
                                    GridRegion r = gridService.GetRegionByUUID(null, friendinfo.CurrentRegionID);
                                    if (r != null)
                                        asyncPoster.Post(r.RegionHandle,
                                                         SyncMessageHelper.AgentStatusChange(us, FriendToInform,
                                                                                             isOnline));
                                }
                                else
                                {
                                    IUserAgentService uas = m_registry.RequestModuleInterface<IUserAgentService>();
                                    if (uas != null)
                                    {
                                        bool online = uas.RemoteStatusNotification(friend, us, isOnline);
                                        if (online)
                                            OnlineFriends.Add(FriendToInform);
                                    }
                                }
                            }
                        }
                    }
                    //If the user is coming online, send all their friends online statuses to them
                    if (isOnline)
                    {
                        GridRegion ourRegion = gridService.GetRegionByUUID(null, UUID.Parse(info[2].ToString()));
                        if (ourRegion != null)
                        {
                            foreach (UUID onlineFriend in OnlineFriends)
                            {
                                asyncPoster.Post(ourRegion.RegionHandle,
                                                 SyncMessageHelper.AgentStatusChange(onlineFriend, us, isOnline));
                            }
                        }
                    }
                }
            }
            return null;
        }

        protected OSDMap OnMessageReceived(OSDMap message)
        {
            IAsyncMessagePostService asyncPost = m_registry.RequestModuleInterface<IAsyncMessagePostService>();
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
                if (manager != null && manager.GetAllScenes().Count > 0)
                {
                    IFriendsModule friendsModule = manager.GetCurrentOrFirstScene().RequestModuleInterface<IFriendsModule>();
                    if (friendsModule != null)
                    {
                        //Send the message
                        friendsModule.SendFriendsStatusMessage(FriendToInformID, AgentID, NewStatus);
                    }
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "FriendGrantRights")
            {
                OSDMap body = (OSDMap) message["Message"];
                UUID targetID = body["Target"].AsUUID();
                IClientCapsService friendSession =
                    m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(targetID);
                if (friendSession != null && friendSession.GetRootCapsService() != null)
                {
                    //Forward the message
                    asyncPost.Post(friendSession.GetRootCapsService().RegionHandle, message);
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "FriendshipOffered")
            {
                OSDMap body = (OSDMap) message["Message"];
                UUID targetID = body["Friend"].AsUUID();
                IClientCapsService friendSession =
                    m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(targetID);
                if (friendSession != null && friendSession.GetRootCapsService() != null)
                {
                    //Forward the message
                    asyncPost.Post(friendSession.GetRootCapsService().RegionHandle, message);
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "FriendTerminated")
            {
                OSDMap body = (OSDMap) message["Message"];
                UUID targetID = body["ExFriend"].AsUUID();
                IClientCapsService friendSession =
                    m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(targetID);
                if (friendSession != null && friendSession.GetRootCapsService() != null)
                {
                    //Forward the message
                    asyncPost.Post(friendSession.GetRootCapsService().RegionHandle, message);
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "FriendshipDenied")
            {
                OSDMap body = (OSDMap) message["Message"];
                UUID targetID = body["FriendID"].AsUUID();
                IClientCapsService friendSession =
                    m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(targetID);
                if (friendSession != null && friendSession.GetRootCapsService() != null)
                {
                    //Forward the message
                    asyncPost.Post(friendSession.GetRootCapsService().RegionHandle, message);
                }
            }
            else if (message.ContainsKey("Method") && message["Method"] == "FriendshipApproved")
            {
                OSDMap body = (OSDMap) message["Message"];
                UUID targetID = body["FriendID"].AsUUID();
                IClientCapsService friendSession =
                    m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(targetID);
                if (friendSession != null && friendSession.GetRootCapsService() != null)
                {
                    //Forward the message
                    asyncPost.Post(friendSession.GetRootCapsService().RegionHandle, message);
                }
            }
            return null;
        }
    }
}