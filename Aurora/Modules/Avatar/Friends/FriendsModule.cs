/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;

namespace Aurora.Modules.Friends
{
    public class FriendsModule : ISharedRegionModule, IFriendsModule
    {
        protected Dictionary<UUID, UserFriendData> m_Friends =
            new Dictionary<UUID, UserFriendData>();

        protected List<IScene> m_Scenes = new List<IScene>();
        public bool m_enabled = true;
        protected bool m_firstStart = true;

        protected Dictionary<UUID, List<UUID>> m_friendsToInformOfStatusChanges = new Dictionary<UUID, List<UUID>>();

        protected IFriendsService FriendsService
        {
            get { return m_Scenes[0].RequestModuleInterface<IFriendsService>(); }
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

        public IAsyncMessagePostService AsyncMessagePostService
        {
            get { return m_Scenes[0].RequestModuleInterface<IAsyncMessagePostService>(); }
        }

        public ISyncMessagePosterService SyncMessagePosterService
        {
            get { return m_Scenes[0].RequestModuleInterface<ISyncMessagePosterService>(); }
        }

        public IAsyncMessageRecievedService AsyncMessageRecievedService
        {
            get { return m_Scenes[0].RequestModuleInterface<IAsyncMessageRecievedService>(); }
        }

        #region IFriendsModule Members

        public int GetFriendPerms(UUID principalID, UUID friendID)
        {
            FriendInfo[] friends = GetFriends(principalID);
#if (!ISWIN)
            foreach (FriendInfo fi in friends)
            {
                if (fi.Friend == friendID.ToString())
                {
                    return fi.TheirFlags;
                }
            }
#else
            foreach (FriendInfo fi in friends.Where(fi => fi.Friend == friendID.ToString()))
            {
                return fi.TheirFlags;
            }
#endif

            return -1;
        }

        public void SendFriendsStatusMessage(UUID FriendToInformID, UUID userID, bool online)
        {
            // Try local
            IClientAPI friendClient = LocateClientObject(FriendToInformID);
            if (friendClient != null)
            {
                //MainConsole.Instance.DebugFormat("[FRIENDS]: Local Status Notify {0} that user {1} is {2}", friendID, userID, online);
                // the  friend in this sim as root agent
                if (online)
                    friendClient.SendAgentOnline(new[] {userID});
                else
                    friendClient.SendAgentOffline(new[] {userID});
                // we're done
                return;
            }

            lock (m_friendsToInformOfStatusChanges)
            {
                if (!m_friendsToInformOfStatusChanges.ContainsKey(FriendToInformID))
                    m_friendsToInformOfStatusChanges.Add(FriendToInformID, new List<UUID>());
                m_friendsToInformOfStatusChanges[FriendToInformID].Add(userID);
            }

            // Friend is not online. Ignore.
        }

        public FriendInfo[] GetFriends(UUID agentID)
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

        public FriendInfo[] GetFriendsRequest(UUID agentID)
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
        #endregion

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public void AddRegion(IScene scene)
        {
            if (!m_enabled)
                return;

            m_Scenes.Add(scene);
            scene.RegisterModuleInterface<IFriendsModule>(this);

            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
            scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
        }

        public void RegionLoaded(IScene scene)
        {
            if (m_firstStart)
                AsyncMessageRecievedService.OnMessageReceived += OnMessageReceived;
            m_firstStart = false;
        }

        public void RemoveRegion(IScene scene)
        {
            if (!m_enabled)
                return;

            m_Scenes.Remove(scene);
            scene.UnregisterModuleInterface<IFriendsModule>(this);

            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
            scene.EventManager.OnMakeRootAgent -= OnMakeRootAgent;
        }

        public string Name
        {
            get { return "FriendsModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        protected OSDMap OnMessageReceived(OSDMap message)
        {
            if (!message.ContainsKey("Method"))
                return null;
            if (message["Method"] == "FriendGrantRights")
            {
                UUID Requester = message["Requester"].AsUUID();
                UUID Target = message["Target"].AsUUID();
                int MyFlags = message["MyFlags"].AsInteger();
                int Rights = message["Rights"].AsInteger();
                LocalGrantRights(Requester, Target, MyFlags, Rights);
            }
            else if (message["Method"] == "FriendTerminated")
            {
                UUID Requester = message["Requester"].AsUUID();
                UUID ExFriend = message["ExFriend"].AsUUID();
                LocalFriendshipTerminated(ExFriend, Requester);
            }
            else if (message["Method"] == "FriendshipOffered")
            {
                //UUID Requester = message["Requester"].AsUUID();
                UUID Friend = message["Friend"].AsUUID();
                GridInstantMessage im = new GridInstantMessage();
                im.FromOSD((OSDMap) message["Message"]);
                LocalFriendshipOffered(Friend, im);
            }
            else if (message["Method"] == "FriendshipDenied")
            {
                UUID Requester = message["Requester"].AsUUID();
                string ClientName = message["ClientName"].AsString();
                UUID FriendID = message["FriendID"].AsUUID();
                LocalFriendshipDenied(Requester, ClientName, FriendID);
            }
            else if (message["Method"] == "FriendshipApproved")
            {
                UUID Requester = message["Requester"].AsUUID();
                string ClientName = message["ClientName"].AsString();
                UUID FriendID = message["FriendID"].AsUUID();
                LocalFriendshipApproved(Requester, ClientName, null, FriendID);
            }
            return null;
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
            OfflineFriendRequest(client);
            //Only send if they are root!
            //Util.FireAndForget(delegate(object o)
            //{
            //    SendFriendsOnlineIfNeeded(client);
            //});
        }

        private void OnMakeRootAgent(IScenePresence presence)
        {
            //Only send if they are root!
            Util.FireAndForget(delegate { SendFriendsOnlineIfNeeded(presence.ControllingClient); });
        }

        public void SendFriendsOnlineIfNeeded(IClientAPI client)
        {
            UUID agentID = client.AgentId;

            // Send outstanding friendship offers
            List<string> outstanding = new List<string>();
            FriendInfo[] friends = GetFriends(agentID);
            foreach (FriendInfo fi in friends)
            {
                UUID friendID;
                string url = "", first = "", last = "", secret = "";
                HGUtil.ParseUniversalUserIdentifier(fi.Friend, out friendID, out url, out first, out last,
                                                        out secret);
                if (friendID != UUID.Zero)
                {
                    if (fi.TheirFlags == -1)
                        outstanding.Add(fi.Friend);
                }
            }

            GridInstantMessage im = new GridInstantMessage(client.Scene, UUID.Zero, String.Empty, agentID,
                                                           (byte) InstantMessageDialog.FriendshipOffered,
                                                           "Will you be my friend?", true, Vector3.Zero);

            foreach (string fid in outstanding)
            {
                UUID fromAgentID;
                string url = "", first = "", last = "", secret = "";
                if (!UUID.TryParse(fid, out fromAgentID))
                    if (
                        !HGUtil.ParseUniversalUserIdentifier(fid, out fromAgentID, out url, out first, out last,
                                                             out secret))
                        continue;

                UserAccount account = m_Scenes[0].UserAccountService.GetUserAccount(client.Scene.RegionInfo.AllScopeIDs,
                                                                                    fromAgentID);

                im.fromAgentID = fromAgentID;
                if (account != null)
                    im.fromAgentName = account.Name;
                else
                    im.fromAgentName = first + " " + last;
                im.offline = 1;
                im.imSessionID = im.fromAgentID;

                // Finally
                LocalFriendshipOffered(agentID, im);
            }

            lock (m_friendsToInformOfStatusChanges)
            {
                if (m_friendsToInformOfStatusChanges.ContainsKey(agentID))
                {
                    List<UUID> onlineFriends = new List<UUID>(m_friendsToInformOfStatusChanges[agentID]);
                    foreach (UUID friend in onlineFriends)
                    {
                        SendFriendsStatusMessage(agentID, friend, true);
                    }
                    m_friendsToInformOfStatusChanges.Remove(agentID);
                }
            }
        }

        /// <summary>
        ///   Find the client for a ID
        /// </summary>
        public IClientAPI LocateClientObject(UUID agentID)
        {
            IScene scene = GetClientScene(agentID);
            if (scene != null)
            {
                IScenePresence presence = scene.GetScenePresence(agentID);
                if (presence != null)
                    return presence.ControllingClient;
            }

            return null;
        }

        /// <summary>
        ///   Find the scene for an agent
        /// </summary>
        public IScene GetClientScene(UUID agentId)
        {
            lock (m_Scenes)
            {
                foreach (IScene scene in from scene in m_Scenes let presence = scene.GetScenePresence(agentId) where presence != null && !presence.IsChildAgent select scene)
                {
                    return scene;
                }
            }

            return null;
        }

        private void OnInstantMessage(IClientAPI client, GridInstantMessage im)
        {
            if ((InstantMessageDialog) im.dialog == InstantMessageDialog.FriendshipOffered)
            {
                // we got a friendship offer
                UUID principalID = im.fromAgentID;
                UUID friendID = im.toAgentID;

                //Can't trust the incoming name for friend offers, so we have to find it ourselves.
                UserAccount sender = m_Scenes[0].UserAccountService.GetUserAccount(m_Scenes[0].RegionInfo.AllScopeIDs, principalID);
                im.fromAgentName = sender.Name;
                UserAccount reciever = m_Scenes[0].UserAccountService.GetUserAccount(m_Scenes[0].RegionInfo.AllScopeIDs, friendID);

                MainConsole.Instance.DebugFormat("[FRIENDS]: {0} offered friendship to {1}", sender.Name, reciever.Name);
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
            UserAccount account = UserAccountService.GetUserAccount(m_Scenes[0].RegionInfo.AllScopeIDs, agentID);
            im.fromAgentName = (account == null) ? "Unknown" : account.Name;

            if (LocalFriendshipOffered(friendID, im))
                return;

            // The prospective friend is not here [as root]. Let's4 forward.
            SyncMessagePosterService.Post(SyncMessageHelper.FriendshipOffered(
                agentID, friendID, im, m_Scenes[0].RegionInfo.RegionHandle), m_Scenes[0].RegionInfo.RegionHandle);
            // If the prospective friend is not online, he'll get the message upon login.
        }

        private void OnApproveFriendRequest(IClientAPI client, UUID agentID, UUID friendID,
                                            List<UUID> callingCardFolders)
        {
            MainConsole.Instance.DebugFormat("[FRIENDS]: {0} accepted friendship from {1}", agentID, friendID);

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
                UserAccount account = client.Scene.UserAccountService.GetUserAccount(client.AllScopeIDs, friendID);
                UUID folderID =
                    client.Scene.InventoryService.GetFolderForType(agentID, InventoryType.Unknown, AssetType.CallingCard)
                        .ID;
                if (account != null)
                    ccmodule.CreateCallingCard(client, friendID, folderID, account.Name);
            }

            // Try Local
            if (LocalFriendshipApproved(agentID, client.Name, client, friendID))
                return;
            SyncMessagePosterService.Post(SyncMessageHelper.FriendshipApproved(
                agentID, client.Name, friendID, m_Scenes[0].RegionInfo.RegionHandle),
                                          m_Scenes[0].RegionInfo.RegionHandle);
        }

        private void OnDenyFriendRequest(IClientAPI client, UUID agentID, UUID friendID, List<UUID> callingCardFolders)
        {
            MainConsole.Instance.DebugFormat("[FRIENDS]: {0} denied friendship to {1}", agentID, friendID);


            FriendInfo[] friends = FriendsService.GetFriendsRequest(agentID).ToArray();
            foreach (FriendInfo fi in friends)
            {
                if (fi.MyFlags == 0)
                {
                    UUID fromAgentID;
                    string url = "", first = "", last = "", secret = "";
                    if (!UUID.TryParse(fi.Friend, out fromAgentID))
                        if (
                            !HGUtil.ParseUniversalUserIdentifier(fi.Friend, out fromAgentID, out url, out first, out last, out secret))
                            continue;
                    if (fromAgentID == friendID)//Get those pesky HG travelers as well
                        FriendsService.Delete(agentID, fi.Friend);
                }
            }
            FriendsService.Delete(friendID, agentID.ToString());

            //
            // Notify the friend
            //

            // Try local
            if (LocalFriendshipDenied(agentID, client.Name, friendID))
                return;
            SyncMessagePosterService.Post(SyncMessageHelper.FriendshipDenied(
                agentID, client.Name, friendID, m_Scenes[0].RegionInfo.RegionHandle),
                                          m_Scenes[0].RegionInfo.RegionHandle);
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

            SyncMessagePosterService.Post(SyncMessageHelper.FriendTerminated(
                agentID, exfriendID, m_Scenes[0].RegionInfo.RegionHandle), m_Scenes[0].RegionInfo.RegionHandle);
        }

        private void OnGrantUserRights(IClientAPI remoteClient, UUID requester, UUID target, int rights)
        {
            FriendInfo[] friends = GetFriends(remoteClient.AgentId);
            if (friends.Length == 0)
                return;

            MainConsole.Instance.DebugFormat("[FRIENDS MODULE]: User {0} changing rights to {1} for friend {2}", requester, rights,
                              target);
            // Let's find the friend in this user's friend list
            FriendInfo friend = null;
#if (!ISWIN)
            foreach (FriendInfo fi in friends)
            {
                if (fi.Friend == target.ToString())
                {
                    friend = fi;
                }
            }
#else
            foreach (FriendInfo fi in friends.Where(fi => fi.Friend == target.ToString()))
            {
                friend = fi;
            }
#endif

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
                    SyncMessagePosterService.Post(SyncMessageHelper.FriendGrantRights(
                        requester, target, myFlags, rights, m_Scenes[0].RegionInfo.RegionHandle),
                                                  m_Scenes[0].RegionInfo.RegionHandle);
                }
            }
        }

        public void OfflineFriendRequest(IClientAPI client)
        {
            // Barrowed a few lines from SendFriendsOnlineIfNeeded() above.
            UUID agentID = client.AgentId;
            FriendInfo[] friends = FriendsService.GetFriendsRequest(agentID).ToArray();
            GridInstantMessage im = new GridInstantMessage(client.Scene, UUID.Zero, String.Empty, agentID,
                                                       (byte)InstantMessageDialog.FriendshipOffered,
                                                       "Will you be my friend?", true, Vector3.Zero);
            foreach (FriendInfo fi in friends)
            {
                if(fi.MyFlags == 0)
                {
                    UUID fromAgentID;
                    string url = "", first = "", last = "", secret = "";
                    if (!UUID.TryParse(fi.Friend, out fromAgentID))
                        if (
                            !HGUtil.ParseUniversalUserIdentifier(fi.Friend, out fromAgentID, out url, out first, out last, out secret))
                            continue;

                    UserAccount account = m_Scenes[0].UserAccountService.GetUserAccount(client.Scene.RegionInfo.AllScopeIDs, fromAgentID);
                    im.fromAgentID = fromAgentID;
                    if (account != null)
                        im.fromAgentName = account.Name;
                    else
                        im.fromAgentName = first + " " + last;
                    im.offline = 1;
                    im.imSessionID = im.fromAgentID;

                    LocalFriendshipOffered(agentID, im);
                }
            }
        }

        private void UpdateFriendsCache(UUID agentID)
        {
            UserFriendData friendsData = new UserFriendData
                                             {
                                                 PrincipalID = agentID,
                                                 Refcount = 0,
                                                 Friends = FriendsService.GetFriends(agentID).ToArray()
                                             };
            lock (m_Friends)
            {
                m_Friends[agentID] = friendsData;
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
                if (us != null)
                    us.SendAgentOnline(new[] {friendID});

                // the prospective friend in this sim as root agent
                GridInstantMessage im = new GridInstantMessage(m_Scenes[0], userID, name, friendID,
                                                               (byte) InstantMessageDialog.FriendshipAccepted,
                                                               userID.ToString(), false, Vector3.Zero);
                friendClient.SendInstantMessage(im);

                // Update the local cache
                UpdateFriendsCache(friendID);


                //
                // put a calling card into the inventory of the friend
                //
                ICallingCardModule ccmodule = friendClient.Scene.RequestModuleInterface<ICallingCardModule>();
                if (ccmodule != null)
                {
                    UserAccount account = friendClient.Scene.UserAccountService.GetUserAccount(friendClient.AllScopeIDs, 
                        userID);
                    UUID folderID =
                        friendClient.Scene.InventoryService.GetFolderForType(friendID, InventoryType.Unknown,
                                                                             AssetType.CallingCard).ID;
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
                                                               (byte) InstantMessageDialog.FriendshipDeclined,
                                                               userID.ToString(), false, Vector3.Zero);
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
                bool onlineBitChanged = ((rights ^ userFlags) & (int) FriendRights.CanSeeOnline) != 0;
                if (onlineBitChanged)
                {
                    if ((rights & (int) FriendRights.CanSeeOnline) == 1)
                        friendClient.SendAgentOnline(new[] {new UUID(userID)});
                    else
                        friendClient.SendAgentOffline(new[] {new UUID(userID)});
                }
                else
                {
                    bool canEditObjectsChanged = ((rights ^ userFlags) & (int) FriendRights.CanModifyObjects) != 0;
                    if (canEditObjectsChanged)
                        friendClient.SendChangeUserRights(userID, friendID, rights);
                }

                // Update local cache
                FriendInfo[] friends = GetFriends(friendID);
                lock (m_Friends)
                {
#if (!ISWIN)
                    foreach (FriendInfo finfo in friends)
                    {
                        if (finfo.Friend == userID.ToString())
                        {
                            finfo.TheirFlags = rights;
                        }
                    }
#else
                    foreach (FriendInfo finfo in friends.Where(finfo => finfo.Friend == userID.ToString()))
                    {
                        finfo.TheirFlags = rights;
                    }
#endif
                }
                friends = GetFriends(userID);
                lock (m_Friends)
                {
#if (!ISWIN)
                    foreach (FriendInfo finfo in friends)
                    {
                        if (finfo.Friend == friendID.ToString())
                        {
                            finfo.MyFlags = rights;
                        }
                    }
#else
                    foreach (FriendInfo finfo in friends.Where(finfo => finfo.Friend == friendID.ToString()))
                    {
                        finfo.MyFlags = rights;
                    }
#endif
                }
                //Add primFlag updates for all the prims in the sim with the owner, so that the new permissions are set up correctly
                IScenePresence friendSP = friendClient.Scene.GetScenePresence(friendClient.AgentId);
#if (!ISWIN)
                foreach (ISceneEntity entity in friendClient.Scene.Entities.GetEntities())
                {
                    if (entity.OwnerID == userID)
                    {
                        entity.ScheduleGroupUpdateToAvatar(friendSP, PrimUpdateFlags.PrimFlags);
                    }
                }
#else
                foreach (ISceneEntity entity in friendClient.Scene.Entities.GetEntities().Where(entity => entity.OwnerID == userID))
                {
                    entity.ScheduleGroupUpdateToAvatar(friendSP, PrimUpdateFlags.PrimFlags);
                }
#endif

                return true;
            }

            return false;
        }

        #endregion

        #region Nested type: UserFriendData

        protected class UserFriendData
        {
            public FriendInfo[] Friends;
            public UUID PrincipalID;
            public int Refcount;

            public bool IsFriend(string friend)
            {
#if (!ISWIN)
                foreach (FriendInfo fi in Friends)
                {
                    if (fi.Friend == friend) return true;
                }
                return false;
#else
                return Friends.Any(fi => fi.Friend == friend);
#endif
            }
        }

        #endregion
    }
}