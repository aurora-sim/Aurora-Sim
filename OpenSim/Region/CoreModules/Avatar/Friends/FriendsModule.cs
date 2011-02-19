/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Services.Connectors.Friends;
using Aurora.Simulation.Base;
using OpenSim.Framework.Servers.HttpServer;
using Aurora.Framework;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Region.CoreModules.Avatar.Friends
{
    public class FriendsModule : ISharedRegionModule, IFriendsModule
    {
        protected class UserFriendData
        {
            public UUID PrincipalID;
            public FriendInfo[] Friends;
            public int Refcount;

            public bool IsFriend(string friend)
            {
                foreach (FriendInfo fi in Friends)
                {
                    if (fi.Friend == friend)
                        return true;
                }

                return false;
            }
        }
        public bool m_enabled = true;
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected List<Scene> m_Scenes = new List<Scene>();

        protected FriendsSimConnector m_FriendsSimConnector;

        protected Dictionary<UUID, UserFriendData> m_Friends =
                new Dictionary<UUID, UserFriendData>();
        
        protected IFriendsService FriendsService
        {
            get
            {
                return  m_Scenes[0].RequestModuleInterface<IFriendsService>();
            }
        }

        protected IGridService GridService
        {
            get 
            {
                if (m_Scenes.Count == 0)
                    return null;
                return m_Scenes[0].GridService; 
            }
        }

        public IUserAccountService UserAccountService
        {
            get { return m_Scenes[0].UserAccountService; }
        }

        public void Initialise(IConfigSource config)
        {
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;
            if (m_FriendsSimConnector == null)
                m_FriendsSimConnector = new FriendsSimConnector();

            m_Scenes.Add(scene);
            scene.RegisterModuleInterface<IFriendsModule>(this);

            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            m_Scenes.Remove(scene);
            scene.UnregisterModuleInterface<IFriendsModule>(this);

            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public string Name
        {
            get { return "FriendsModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnInstantMessage -= OnInstantMessage;
            client.OnApproveFriendRequest -= OnApproveFriendRequest;
            client.OnDenyFriendRequest -= OnDenyFriendRequest;
            client.OnTerminateFriendship -= OnTerminateFriendship;
            client.OnGrantUserRights -= OnGrantUserRights;
        }

        private void OnNewClient(IClientAPI client)
        {
            client.OnInstantMessage += OnInstantMessage;
            client.OnApproveFriendRequest += OnApproveFriendRequest;
            client.OnDenyFriendRequest += OnDenyFriendRequest;
            client.OnTerminateFriendship += OnTerminateFriendship;
            client.OnGrantUserRights += OnGrantUserRights;

            Util.FireAndForget(delegate(object o)
            {
                SendFriendsOnlineIfNeeded(client);
            });
        }

        public int GetFriendPerms(UUID principalID, UUID friendID)
        {
            FriendInfo[] friends = GetFriends(principalID);
            foreach (FriendInfo fi in friends)
            {
                if (fi.Friend == friendID.ToString())
                    return fi.TheirFlags;
            }

            return -1;
        }

        public void SendFriendsOnlineIfNeeded(IClientAPI client)
        {
            UUID agentID = client.AgentId;

            // Send outstanding friendship offers
            List<string> outstanding = new List<string>();
            FriendInfo[] friends = GetFriends(agentID);
            foreach (FriendInfo fi in friends)
            {
                if (fi.TheirFlags == -1)
                    outstanding.Add(fi.Friend);
            }

            GridInstantMessage im = new GridInstantMessage(client.Scene, UUID.Zero, String.Empty, agentID, (byte)InstantMessageDialog.FriendshipOffered,
                "Will you be my friend?", true, Vector3.Zero);

            foreach (string fid in outstanding)
            {
                UUID fromAgentID;
                if (!UUID.TryParse(fid, out fromAgentID))
                    continue;

                UserAccount account = m_Scenes[0].UserAccountService.GetUserAccount(client.Scene.RegionInfo.ScopeID, fromAgentID);

                im.fromAgentID = fromAgentID.Guid;
                im.fromAgentName = account.FirstName + " " + account.LastName;
                im.offline = 1;
                im.imSessionID = im.fromAgentID;

                // Finally
                LocalFriendshipOffered(agentID, im);
            }
        }

        /// <summary>
        /// Find the client for a ID
        /// </summary>
        public IClientAPI LocateClientObject(UUID agentID)
        {
            Scene scene = GetClientScene(agentID);
            if (scene != null)
            {
                ScenePresence presence = scene.GetScenePresence(agentID);
                if (presence != null)
                    return presence.ControllingClient;
            }

            return null;
        }

        /// <summary>
        /// Find the scene for an agent
        /// </summary>
        public Scene GetClientScene(UUID agentId)
        {
            lock (m_Scenes)
            {
                foreach (Scene scene in m_Scenes)
                {
                    ScenePresence presence = scene.GetScenePresence(agentId);
                    if (presence != null && !presence.IsChildAgent)
                        return scene;
                }
            }

            return null;
        }

        public void SendFriendsStatusMessage(UUID FriendToInformID, UUID userID, bool online)
        {
            // Try local
            if (LocalStatusNotification(userID, FriendToInformID, online))
                return;

            // Friend is not online. Ignore.
        }

        private void OnInstantMessage(IClientAPI client, GridInstantMessage im)
        {
            if ((InstantMessageDialog)im.dialog == InstantMessageDialog.FriendshipOffered)
            { 
                // we got a friendship offer
                UUID principalID = new UUID(im.fromAgentID);
                UUID friendID = new UUID(im.toAgentID);

                //Can't trust the incoming name for friend offers, so we have to find it ourselves.
                UserAccount sender = m_Scenes[0].UserAccountService.GetUserAccount(UUID.Zero, principalID);
                im.fromAgentName = sender.FirstName + " " + sender.LastName;
                UserAccount reciever = m_Scenes[0].UserAccountService.GetUserAccount(UUID.Zero, friendID);

                m_log.DebugFormat("[FRIENDS]: {0} offered friendship to {1}", sender.FirstName + " " + sender.LastName, reciever.FirstName + " " + reciever.LastName);
                // This user wants to be friends with the other user.
                // Let's add the relation backwards, in case the other is not online
                FriendsService.StoreFriend(friendID, principalID.ToString(), 0);

                // Now let's ask the other user to be friends with this user
                ForwardFriendshipOffer(principalID, friendID, im);
            }
        }

        private void ForwardFriendshipOffer(UUID agentID, UUID friendID, GridInstantMessage im)
        {
            // !!!!!!!! This is a hack so that we don't have to keep state (transactionID/imSessionID)
            // We stick this agent's ID as imSession, so that it's directly available on the receiving end
            im.imSessionID = im.fromAgentID;

            // Try the local sim
            UserAccount account = UserAccountService.GetUserAccount(m_Scenes[0].RegionInfo.ScopeID, agentID);
            im.fromAgentName = (account == null) ? "Unknown" : account.FirstName + " " + account.LastName;
            
            if (LocalFriendshipOffered(friendID, im))
                return;

            // The prospective friend is not here [as root]. Let's forward.
            UserInfo friendSession = m_Scenes[0].RequestModuleInterface<IAgentInfoService>().GetUserInfo(friendID.ToString());
            if (friendSession != null && friendSession.IsOnline)
            {
                GridRegion region = GridService.GetRegionByUUID(m_Scenes[0].RegionInfo.ScopeID, friendSession.CurrentRegionID);
                m_FriendsSimConnector.FriendshipOffered(region, agentID, friendID, im.message);
            }
            // If the prospective friend is not online, he'll get the message upon login.
        }

        private void OnApproveFriendRequest(IClientAPI client, UUID agentID, UUID friendID, List<UUID> callingCardFolders)
        {
            m_log.DebugFormat("[FRIENDS]: {0} accepted friendship from {1}", agentID, friendID);
            
            FriendsService.StoreFriend(agentID, friendID.ToString(), 1);
            FriendsService.StoreFriend(friendID, agentID.ToString(), 1);

            // Update the local cache
            UpdateFriendsCache(agentID);

            //
            // Notify the friend
            //

            //
            // Send calling card to the local user
            //

            ICallingCardModule ccmodule = client.Scene.RequestModuleInterface<ICallingCardModule>();
            if (ccmodule != null)
            {
                UserAccount account = ((Scene)client.Scene).UserAccountService.GetUserAccount(UUID.Zero, friendID);
                UUID folderID = ((Scene)client.Scene).InventoryService.GetFolderForType(agentID, AssetType.CallingCard).ID;
                ccmodule.CreateCallingCard(client, friendID, folderID, account.Name);
            }
                
            // Try Local
            if (LocalFriendshipApproved(agentID, client.Name, client, friendID))
                return;

            // The friend is not here
            UserInfo friendSession = m_Scenes[0].RequestModuleInterface<IAgentInfoService>().GetUserInfo(friendID.ToString());
            if (friendSession != null && friendSession.IsOnline)
            {
                GridRegion region = GridService.GetRegionByUUID(m_Scenes[0].RegionInfo.ScopeID, friendSession.CurrentRegionID);
                m_FriendsSimConnector.FriendshipApproved(region, agentID, client.Name, friendID);
            }
        }

        private void OnDenyFriendRequest(IClientAPI client, UUID agentID, UUID friendID, List<UUID> callingCardFolders)
        {
            m_log.DebugFormat("[FRIENDS]: {0} denied friendship to {1}", agentID, friendID);

            FriendsService.Delete(agentID, friendID.ToString());
            FriendsService.Delete(friendID, agentID.ToString());

            //
            // Notify the friend
            //

            // Try local
            if (LocalFriendshipDenied(agentID, client.Name, friendID))
                return;

            UserInfo friendSession = m_Scenes[0].RequestModuleInterface<IAgentInfoService>().GetUserInfo(friendID.ToString());
            if (friendSession != null && friendSession.IsOnline)
            {
                GridRegion region = GridService.GetRegionByUUID(m_Scenes[0].RegionInfo.ScopeID, friendSession.CurrentRegionID);
                if (region != null)
                    m_FriendsSimConnector.FriendshipDenied(region, agentID, client.Name, friendID);
                else
                    m_log.WarnFormat("[FRIENDS]: Could not find region {0} in locating {1}", friendSession.CurrentRegionID, friendID);
            }
        }

        private void OnTerminateFriendship(IClientAPI client, UUID agentID, UUID exfriendID)
        {
            FriendsService.Delete(agentID, exfriendID.ToString());
            FriendsService.Delete(exfriendID, agentID.ToString());

            // Update local cache
            UpdateFriendsCache(agentID);

            client.SendTerminateFriend(exfriendID);

            //
            // Notify the friend
            //

            // Try local
            if (LocalFriendshipTerminated(exfriendID, agentID))
                return;

            UserInfo friendSession = m_Scenes[0].RequestModuleInterface<IAgentInfoService>().GetUserInfo(exfriendID.ToString());
            if (friendSession != null && friendSession.IsOnline)
            {
                GridRegion region = GridService.GetRegionByUUID(m_Scenes[0].RegionInfo.ScopeID, friendSession.CurrentRegionID);
                m_FriendsSimConnector.FriendshipTerminated(region, agentID, exfriendID);
            }
        }

        private void OnGrantUserRights(IClientAPI remoteClient, UUID requester, UUID target, int rights)
        {
            FriendInfo[] friends = GetFriends(remoteClient.AgentId);
            if (friends.Length == 0)
                return;

            m_log.DebugFormat("[FRIENDS MODULE]: User {0} changing rights to {1} for friend {2}", requester, rights, target);
            // Let's find the friend in this user's friend list
            FriendInfo friend = null;
            foreach (FriendInfo fi in friends)
            {
                if (fi.Friend == target.ToString())
                    friend = fi;
            }

            if (friend != null) // Found it
            {
                // Store it on the DB
                FriendsService.StoreFriend(requester, target.ToString(), rights);

                // Store it in the local cache
                int myFlags = friend.MyFlags;
                friend.MyFlags = rights;

                // Always send this back to the original client
                remoteClient.SendChangeUserRights(requester, target, rights);

                //
                // Notify the friend
                //

                // Try local
                if (!LocalGrantRights(requester, target, myFlags, rights))
                {
                    UserInfo friendSession = m_Scenes[0].RequestModuleInterface<IAgentInfoService>().GetUserInfo(target.ToString());
                    if (friendSession != null && friendSession.IsOnline)
                    {
                        GridRegion region = GridService.GetRegionByUUID(m_Scenes[0].RegionInfo.ScopeID, friendSession.CurrentRegionID);
                        m_FriendsSimConnector.GrantRights(region, requester, target, myFlags, rights);
                    }
                }
            }
        }

        #region Local

        public bool LocalFriendshipOffered(UUID toID, GridInstantMessage im)
        {
            IClientAPI friendClient = LocateClientObject(toID);
            if (friendClient != null)
            {
                // the prospective friend in this sim as root agent
                friendClient.SendInstantMessage(im);
                // we're done
                return true;
            }
            return false;
        }

        public bool LocalFriendshipApproved(UUID userID, string name, IClientAPI us, UUID friendID)
        {
            IClientAPI friendClient = LocateClientObject(friendID);
            if (friendClient != null)
            {
                //They are online, send the online message
                if(us != null)
                    us.SendAgentOnline(new UUID[] { friendID });

                // the prospective friend in this sim as root agent
                GridInstantMessage im = new GridInstantMessage(m_Scenes[0], userID, name, friendID,
                    (byte)OpenMetaverse.InstantMessageDialog.FriendshipAccepted, userID.ToString(), false, Vector3.Zero);
                friendClient.SendInstantMessage(im);

                // Update the local cache
                UpdateFriendsCache(friendID);


                //
                // put a calling card into the inventory of the friend
                //
                ICallingCardModule ccmodule = friendClient.Scene.RequestModuleInterface<ICallingCardModule>();
                if (ccmodule != null)
                {
                    UserAccount account = ((Scene)friendClient.Scene).UserAccountService.GetUserAccount(UUID.Zero, userID);
                    UUID folderID = ((Scene)friendClient.Scene).InventoryService.GetFolderForType(friendID, AssetType.CallingCard).ID;
                    ccmodule.CreateCallingCard(friendClient, userID, folderID, account.Name);
                }
                // we're done
                return true;
            }

            return false;
        }

        public bool LocalFriendshipDenied(UUID userID, string userName, UUID friendID)
        {
            IClientAPI friendClient = LocateClientObject(friendID);
            if (friendClient != null)
            {
                // the prospective friend in this sim as root agent
                GridInstantMessage im = new GridInstantMessage(m_Scenes[0], userID, userName, friendID,
                    (byte)OpenMetaverse.InstantMessageDialog.FriendshipDeclined, userID.ToString(), false, Vector3.Zero);
                friendClient.SendInstantMessage(im);
                // we're done
                return true;
            }
            
            return false;
        }

        public bool LocalFriendshipTerminated(UUID exfriendID, UUID terminatingUser)
        {
            IClientAPI friendClient = LocateClientObject(exfriendID);
            if (friendClient != null)
            {
                // update local cache
                UpdateFriendsCache(exfriendID);
                // the friend in this sim as root agent
                // you do NOT send the friend his uuid...  /me sighs...    - Revolution
                friendClient.SendTerminateFriend(terminatingUser);
                return true;
            }

            return false;
        }

        public bool LocalGrantRights(UUID userID, UUID friendID, int userFlags, int rights)
        {
            IClientAPI friendClient = LocateClientObject(friendID);
            if (friendClient != null)
            {
                bool onlineBitChanged = ((rights ^ userFlags) & (int)FriendRights.CanSeeOnline) != 0;
                if (onlineBitChanged)
                {
                    if ((rights & (int)FriendRights.CanSeeOnline) == 1)
                        friendClient.SendAgentOnline(new UUID[] { new UUID(userID) });
                    else
                        friendClient.SendAgentOffline(new UUID[] { new UUID(userID) });
                }
                else
                {
                    bool canEditObjectsChanged = ((rights ^ userFlags) & (int)FriendRights.CanModifyObjects) != 0;
                    if (canEditObjectsChanged)
                        friendClient.SendChangeUserRights(userID, friendID, rights);

                }

                // Update local cache
                FriendInfo[] friends = GetFriends(friendID);
                lock (m_Friends)
                {
                    foreach (FriendInfo finfo in friends)
                    {
                        if (finfo.Friend == userID.ToString())
                            finfo.TheirFlags = rights;
                    }
                }

                return true;
            }

            return false;

        }

        public bool LocalStatusNotification(UUID userID, UUID friendID, bool online)
        {
            IClientAPI friendClient = LocateClientObject(friendID);
            if (friendClient != null)
            {
                //m_log.DebugFormat("[FRIENDS]: Local Status Notify {0} that user {1} is {2}", friendID, userID, online);
                // the  friend in this sim as root agent
                if (online)
                    friendClient.SendAgentOnline(new UUID[] { userID });
                else
                    friendClient.SendAgentOffline(new UUID[] { userID });
                // we're done
                return true;
            }

            return false;
        }

        #endregion
        private FriendInfo[] GetFriends(UUID agentID)
        {
            UserFriendData friendsData;

            lock (m_Friends)
            {
                if (m_Friends.TryGetValue(agentID, out friendsData))
                    return friendsData.Friends;
                else
                {
                    UpdateFriendsCache(agentID);
                    if (m_Friends.TryGetValue(agentID, out friendsData))
                        return friendsData.Friends;
                }
            }

            return new FriendInfo[0];
        }

        private void UpdateFriendsCache(UUID agentID)
        {
            UserFriendData friendsData = new UserFriendData();
            friendsData.PrincipalID = agentID;
            friendsData.Refcount = 0;
            friendsData.Friends = FriendsService.GetFriends(agentID);
            lock (m_Friends)
            {
                m_Friends[agentID] = friendsData;
            }
        }
    }
}
