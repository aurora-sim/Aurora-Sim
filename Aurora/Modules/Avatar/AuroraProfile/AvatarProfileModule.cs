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
using System.Globalization;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using log4net;
using Nini.Config;
using OpenMetaverse;
using ProfileFlags = OpenMetaverse.ProfileFlags;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Nwc.XmlRpc;
using System.Xml;
using Aurora.Framework;
using Aurora.DataManager;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;
using OpenSim.Region.DataSnapshot.Interfaces;
using OpenMetaverse.StructuredData;

namespace Aurora.Modules
{
    public class AuroraProfileModule : ISharedRegionModule
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;
        private IConfigSource m_config;
        private Dictionary<string, Dictionary<UUID, string>> ClassifiedsCache = new Dictionary<string, Dictionary<UUID, string>>();
        private Dictionary<string, List<string>> ClassifiedInfoCache = new Dictionary<string, List<string>>();
        private IProfileConnector ProfileFrontend = null;
        private IFriendsModule m_friendsModule;
        private IConfigSource m_gConfig;
        private List<Scene> m_Scenes = new List<Scene>();
        private bool m_ProfileEnabled = true;

        #endregion

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
            m_config = config;
            m_gConfig = config;
            IConfig profileConfig = config.Configs["Profile"];
            if (profileConfig == null)
            {
                m_log.Info("[AuroraProfile] Not configured, disabling");
                return;
            }
            if (profileConfig.GetString("ProfileModule", Name) != Name)
            {
                m_ProfileEnabled = false;
            }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_ProfileEnabled)
                return;
            ProfileFrontend = DataManager.DataManager.RequestPlugin<IProfileConnector>();
            if (ProfileFrontend == null)
                return;

            if (!m_Scenes.Contains(scene))
                m_Scenes.Add(scene);
            m_scene = scene;
            m_scene.EventManager.OnNewClient += NewClient;
            m_scene.EventManager.OnClosingClient += OnClosingClient;
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_ProfileEnabled)
                return;
            if (m_Scenes.Contains(scene))
                m_Scenes.Remove(scene);
            m_scene.EventManager.OnNewClient -= NewClient;
            m_scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public void RegionLoaded(Scene scene)
        {
            m_friendsModule = scene.RequestModuleInterface<IFriendsModule>();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "AuroraProfileModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        #endregion

        #region Client

        private void OnClosingClient(IClientAPI client)
        {
            client.OnRequestAvatarProperties -= RequestAvatarProperty;
            client.OnUpdateAvatarProperties -= UpdateAvatarProperties;
            client.RemoveGenericPacketHandler("avatarclassifiedsrequest");
            client.OnClassifiedInfoRequest -= ClassifiedInfoRequest;
            client.OnClassifiedInfoUpdate -= ClassifiedInfoUpdate;
            client.OnClassifiedDelete -= ClassifiedDelete;
            client.OnClassifiedGodDelete -= GodClassifiedDelete;
            client.OnUserInfoRequest -= UserPreferencesRequest;
            client.OnUpdateUserInfo -= UpdateUserPreferences;
            //Track agents
            client.OnTrackAgent -= TrackAgent;
            client.OnFindAgent -= TrackAgent;

            // Notes
            client.RemoveGenericPacketHandler("avatarnotesrequest");
            client.OnAvatarNotesUpdate -= AvatarNotesUpdate;

            //Profile
            client.OnAvatarInterestUpdate -= AvatarInterestsUpdate;

            // Picks
            client.RemoveGenericPacketHandler("avatarpicksrequest");
            client.RemoveGenericPacketHandler("pickinforequest");
            client.OnPickInfoUpdate -= PickInfoUpdate;
            client.OnPickDelete -= PickDelete;
            client.OnPickGodDelete -= GodPickDelete;
        }

        public void NewClient(IClientAPI client)
        {
            client.OnRequestAvatarProperties += RequestAvatarProperty;
            client.OnUpdateAvatarProperties += UpdateAvatarProperties;
            client.AddGenericPacketHandler("avatarclassifiedsrequest", HandleAvatarClassifiedsRequest);
            client.OnClassifiedInfoRequest += ClassifiedInfoRequest;
            client.OnClassifiedInfoUpdate += ClassifiedInfoUpdate;
            client.OnClassifiedDelete += ClassifiedDelete;
            client.OnClassifiedGodDelete += GodClassifiedDelete;
            client.OnUserInfoRequest += UserPreferencesRequest;
            client.OnUpdateUserInfo += UpdateUserPreferences;
            //Track agents
            client.OnTrackAgent += TrackAgent;
            client.OnFindAgent += TrackAgent;

            // Notes
            client.AddGenericPacketHandler("avatarnotesrequest", HandleAvatarNotesRequest);
            client.OnAvatarNotesUpdate += AvatarNotesUpdate;

            //Profile
            client.OnAvatarInterestUpdate += AvatarInterestsUpdate;

            // Picks
            client.AddGenericPacketHandler("avatarpicksrequest", HandleAvatarPicksRequest);
            client.AddGenericPacketHandler("pickinforequest", HandlePickInfoRequest);
            client.OnPickInfoUpdate += PickInfoUpdate;
            client.OnPickDelete += PickDelete;
            client.OnPickGodDelete += GodPickDelete;
        }

        #endregion

        #region Classifieds

        public void HandleAvatarClassifiedsRequest(Object sender, string method, List<String> args)
        {
            if (!(sender is IClientAPI))
                return;

            IClientAPI remoteClient = (IClientAPI)sender;
            Dictionary<UUID, string> classifieds = new Dictionary<UUID, string>();
            UUID requestedUUID = new UUID(args[0]);

            //Does this person have the power to view this person's profile?
            bool isFriend = IsFriendOfUser(remoteClient.AgentId, requestedUUID);
            if (isFriend)
            {
                IUserProfileInfo profile = ProfileFrontend.GetUserProfile(requestedUUID);
                if (profile == null)
                    return;
                foreach (object classified in profile.Classifieds.Values)
                {
                    Classified Classified = (Classified)classified;
                    classifieds.Add(Classified.ClassifiedUUID, Classified.Name);
                }
            }

            remoteClient.SendAvatarClassifiedReply(requestedUUID, classifieds);        
        }

        public void ClassifiedInfoRequest(UUID queryClassifiedID, IClientAPI remoteClient)
        {
            IUserProfileInfo info = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            if (info == null)
                return;
            if (info.Classifieds.ContainsKey(queryClassifiedID.ToString()))
            {
                OSDMap classifieds = Util.DictionaryToOSD(info.Classifieds);
                Classified classified = new Classified(Util.OSDToDictionary((OSDMap)classifieds[queryClassifiedID.ToString()]));
                remoteClient.SendClassifiedInfoReply(queryClassifiedID, classified.CreatorUUID, classified.CreationDate, classified.ExpirationDate, classified.Category, classified.Name, classified.Description, classified.ParcelUUID, classified.ParentEstate, classified.SnapshotUUID, classified.SimName, classified.GlobalPos, classified.ParcelName, classified.ClassifiedFlags, classified.PriceForListing);
            }
        }
        
        public void ClassifiedInfoUpdate(UUID queryclassifiedID, uint queryCategory, string queryName, string queryDescription, UUID queryParcelID,
                                         uint queryParentEstate, UUID querySnapshotID, Vector3 queryGlobalPos, byte queryclassifiedFlags,
                                         int queryclassifiedPrice, IClientAPI remoteClient)
        {
            //Security check
            IUserProfileInfo info = ProfileFrontend.GetUserProfile(remoteClient.AgentId);

            if (info == null)
                return;
            if (info.Classifieds.ContainsKey(queryclassifiedID.ToString()))
            {
                OSDMap Classifieds = Util.DictionaryToOSD(info.Classifieds);
                Classified oldClassified = new Classified(Util.OSDToDictionary((OSDMap)Classifieds[queryclassifiedID.ToString()]));
                if (oldClassified.CreatorUUID != remoteClient.AgentId)
                    return;

                info.Classifieds.Remove(queryclassifiedID.ToString());
            }

            UUID creatorUUID = remoteClient.AgentId;
            UUID classifiedUUID = queryclassifiedID;
            uint category = queryCategory;
            string name = queryName;
            string description = queryDescription;
            uint parentestate = queryParentEstate;
            UUID snapshotUUID = querySnapshotID;
            string simname = remoteClient.Scene.RegionInfo.RegionName;
            Vector3 globalpos = queryGlobalPos;
            byte classifiedFlags = queryclassifiedFlags;
            int classifiedPrice = queryclassifiedPrice;

            ScenePresence p = m_scene.GetScenePresence(remoteClient.AgentId);

            UUID parceluuid = p.currentParcelUUID;
            string parcelname = "Unknown";

            string pos_global = new Vector3(remoteClient.Scene.RegionInfo.RegionLocX * Constants.RegionSize + p.AbsolutePosition.X,
                remoteClient.Scene.RegionInfo.RegionLocY * Constants.RegionSize + p.AbsolutePosition.Y,
                p.AbsolutePosition.Z).ToString();

            ILandObject parcel = m_scene.LandChannel.GetLandObject(p.AbsolutePosition.X, p.AbsolutePosition.Y);
            if(parcel != null)
            {
                parcelname = parcel.LandData.Name;
                parceluuid = parcel.LandData.InfoUUID;
            }

            uint creationdate = (uint)Util.UnixTimeSinceEpoch();
            
            uint expirationdate = (uint)Util.UnixTimeSinceEpoch() + (365 * 24 * 60 * 60);

            Classified classified = new Classified();
            classified.ClassifiedUUID = classifiedUUID;
            classified.CreatorUUID = creatorUUID;
            classified.CreationDate = creationdate;
            classified.ExpirationDate = expirationdate;
            classified.Category = category;
            classified.Name = name;
            classified.Description = description;
            classified.ParcelUUID = parceluuid;
            classified.ParentEstate = parentestate;
            classified.SnapshotUUID = snapshotUUID;
            classified.SimName = simname;
            classified.GlobalPos = globalpos;
            classified.ParcelName = parcelname;
            classified.ClassifiedFlags = classifiedFlags;
            classified.PriceForListing = classifiedPrice;

            info.Classifieds.Add(classified.ClassifiedUUID.ToString(), Util.DictionaryToOSD(classified.ToKeyValuePairs()));
            ProfileFrontend.UpdateUserProfile(info);
        }
        
        public void ClassifiedDelete(UUID queryClassifiedID, IClientAPI remoteClient)
        {
            //Security check
            IUserProfileInfo info = ProfileFrontend.GetUserProfile(remoteClient.AgentId);

            if (info == null)
                return;
            if (info.Classifieds.ContainsKey(queryClassifiedID.ToString()))
            {
                OSDMap Classifieds = Util.DictionaryToOSD(info.Classifieds);
                Classified oldClassified = new Classified(Util.OSDToDictionary((OSDMap)Classifieds[queryClassifiedID.ToString()]));
                if (oldClassified.CreatorUUID != remoteClient.AgentId)
                    return;

                info.Classifieds.Remove(queryClassifiedID.ToString());
                ProfileFrontend.UpdateUserProfile(info);
            }
        }
        
        public void GodClassifiedDelete(UUID queryClassifiedID, IClientAPI remoteClient)
        {
            if (m_scene.Permissions.IsGod(remoteClient.AgentId))
            {
                IUserProfileInfo info = ProfileFrontend.GetUserProfile(remoteClient.AgentId);

                if (info == null)
                    return;
                if (info.Classifieds.ContainsKey(queryClassifiedID.ToString()))
                {
                    OSDMap Classifieds = Util.DictionaryToOSD(info.Classifieds);
                    Classified oldClassified = new Classified(Util.OSDToDictionary((OSDMap)Classifieds[queryClassifiedID.ToString()]));
                    if (oldClassified.CreatorUUID != remoteClient.AgentId)
                        return;

                    info.Classifieds.Remove(queryClassifiedID.ToString());
                    ProfileFrontend.UpdateUserProfile(info);
                }
            }
        }
        
        #endregion

        #region Picks

        public void HandleAvatarPicksRequest(Object sender, string method, List<String> args)
        {
            if (!(sender is IClientAPI))
                return;
            
            IClientAPI remoteClient = (IClientAPI)sender;
            Dictionary<UUID, string> picks = new Dictionary<UUID, string>();
            UUID requestedUUID = new UUID(args[0]);

            bool isFriend = IsFriendOfUser(remoteClient.AgentId, requestedUUID);
            if (isFriend)
            {
                IUserProfileInfo profile = ProfileFrontend.GetUserProfile(requestedUUID);

                if (profile == null)
                    return;
                foreach (object pick in profile.Picks.Values)
                {
                    ProfilePickInfo Pick = (ProfilePickInfo)pick;
                    picks.Add(new UUID(Pick.PickUUID), Pick.Name);
                }
            }
            remoteClient.SendAvatarPicksReply(requestedUUID, picks);
        }

        public void HandlePickInfoRequest(Object sender, string method, List<String> args)
        {
            if (!(sender is IClientAPI))
                return;

            IClientAPI remoteClient = (IClientAPI)sender;
            UUID PickUUID = UUID.Parse(args[1]);

            IUserProfileInfo info = ProfileFrontend.GetUserProfile(remoteClient.AgentId);

            if (info == null)
                return;
            if (info.Picks.ContainsKey(PickUUID.ToString()))
            {
                OSDMap picks = Util.DictionaryToOSD(info.Picks);
                ProfilePickInfo pick = new ProfilePickInfo(Util.OSDToDictionary((OSDMap)picks[PickUUID.ToString()]));
                remoteClient.SendPickInfoReply(pick.PickUUID, pick.CreatorUUID, pick.TopPick == 1 ? true : false, pick.ParcelUUID, pick.Name, pick.Description, pick.SnapshotUUID, pick.User, pick.OriginalName, pick.SimName, pick.GlobalPos, pick.SortOrder, pick.Enabled == 1 ? true : false);
            }
        }
        
        public void PickInfoUpdate(IClientAPI remoteClient, UUID pickID, UUID creatorID, bool topPick, string name, string desc, UUID snapshotID, int sortOrder, bool enabled)
        {
            IUserProfileInfo info = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            if (info == null)
                return;
            ScenePresence p = m_scene.GetScenePresence(remoteClient.AgentId);

            UUID parceluuid = p.currentParcelUUID;
            string user = "(unknown)";
            string OrigionalName = "(unknown)";
            
            Vector3 pos_global = new Vector3(p.AbsolutePosition.X + p.Scene.RegionInfo.RegionLocX * 256,
                p.AbsolutePosition.Y + p.Scene.RegionInfo.RegionLocY * 256,
                p.AbsolutePosition.Z);

            ILandObject targetlandObj = m_scene.LandChannel.GetLandObject(p.AbsolutePosition.X, p.AbsolutePosition.Y);

            if (targetlandObj != null)
            {
                ScenePresence parcelowner = m_scene.GetScenePresence(targetlandObj.LandData.OwnerID);

                if (parcelowner != null)
                    user = parcelowner.Name;

                parceluuid = targetlandObj.LandData.InfoUUID;

                OrigionalName = targetlandObj.LandData.Name;
            }

            OSDMap picks = Util.DictionaryToOSD(info.Picks);
            if (!picks.ContainsKey(pickID.ToString()))
            {
                ProfilePickInfo values = new ProfilePickInfo();
                values.PickUUID = pickID;
                values.CreatorUUID = creatorID;
                values.TopPick = topPick ? 1 : 0;
                values.ParcelUUID = parceluuid;
                values.Name = name;
                values.Description = desc;
                values.SnapshotUUID = snapshotID;
                values.User = user;
                values.OriginalName = OrigionalName;
                values.SimName = remoteClient.Scene.RegionInfo.RegionName;
                values.GlobalPos = pos_global;
                values.SortOrder = sortOrder;
                values.Enabled = enabled ? 1 : 0;
                picks.Add(pickID.ToString(), Util.DictionaryToOSD(values.ToKeyValuePairs()));
            }
            else
            {
                ProfilePickInfo oldpick = new ProfilePickInfo(Util.OSDToDictionary((OSDMap)picks[pickID.ToString()]));
                //Security check
                if (oldpick.CreatorUUID != remoteClient.AgentId)
                    return;

                oldpick.ParcelUUID = parceluuid;
                oldpick.Name = name;
                oldpick.SnapshotUUID = snapshotID;
                oldpick.Description = desc;
                oldpick.SimName = remoteClient.Scene.RegionInfo.RegionName;
                oldpick.GlobalPos = pos_global;
                oldpick.SortOrder = sortOrder;
                oldpick.Enabled = enabled ? 1 : 0;
                picks.Add(pickID.ToString(), Util.DictionaryToOSD(oldpick.ToKeyValuePairs()));
            }
            info.Picks = Util.OSDToDictionary(picks);
            ProfileFrontend.UpdateUserProfile(info);
        }

        public void GodPickDelete(IClientAPI remoteClient, UUID AgentID, UUID queryPickID, UUID queryID)
        {
            if (m_scene.Permissions.IsGod(remoteClient.AgentId))
            {
                IUserProfileInfo info = ProfileFrontend.GetUserProfile(remoteClient.AgentId);

                if (info == null)
                    return;
                if (info.Picks.ContainsKey(queryPickID.ToString()))
                {
                    OSDMap picks = Util.DictionaryToOSD(info.Picks);
                    ProfilePickInfo oldpick = new ProfilePickInfo(Util.OSDToDictionary((OSDMap)picks[queryPickID.ToString()]));
                    if (oldpick.CreatorUUID != remoteClient.AgentId)
                        return;

                    info.Picks.Remove(queryPickID.ToString());
                    ProfileFrontend.UpdateUserProfile(info);
                }
            }
        }
        
        public void PickDelete(IClientAPI remoteClient, UUID queryPickID)
        {
            IUserProfileInfo info = ProfileFrontend.GetUserProfile(remoteClient.AgentId);

            if (info == null)
                return;
            if (info.Picks.ContainsKey(queryPickID.ToString()))
            {
                OSDMap picks = Util.DictionaryToOSD(info.Picks);
                ProfilePickInfo oldpick = new ProfilePickInfo(Util.OSDToDictionary((OSDMap)picks[queryPickID.ToString()]));
                if (oldpick.CreatorUUID != remoteClient.AgentId)
                    return;

                info.Picks.Remove(queryPickID.ToString());
                ProfileFrontend.UpdateUserProfile(info);
            }
        }

        #endregion

        #region Notes

        public void HandleAvatarNotesRequest(Object sender, string method, List<String> args)
        {
            if (!(sender is IClientAPI))
            {
                m_log.Debug("sender isnt IClientAPI");
                return;
            }

            IClientAPI remoteClient = (IClientAPI)sender;
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            if (UPI == null)
                return;
            
            object notes = "";
            string targetNotesUUID = args[0];

            if(!UPI.Notes.TryGetValue(targetNotesUUID, out notes))
                notes = "";

            remoteClient.SendAvatarNotesReply(new UUID(targetNotesUUID), notes.ToString());
        }
        
        public void AvatarNotesUpdate(IClientAPI remoteClient, UUID queryTargetID, string queryNotes)
        {
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            if (UPI == null)
                return;
            String notes = queryNotes;
            
            UPI.Notes[queryTargetID.ToString()] = notes;

            ProfileFrontend.UpdateUserProfile(UPI);
        }

        #endregion

        #region Interests

        public void AvatarInterestsUpdate(IClientAPI remoteClient, uint wantmask, string wanttext, uint skillsmask, string skillstext, string languages)
        {
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            UPI.Interests.WantToMask = wantmask;
            UPI.Interests.WantToText = wanttext;
            UPI.Interests.CanDoMask = skillsmask;
            UPI.Interests.CanDoText = skillstext;
            UPI.Interests.Languages = languages;
            ProfileFrontend.UpdateUserProfile(UPI);
        }

        #endregion

        #region Requesting and Sending Profile Info

        public void RequestAvatarProperty(IClientAPI remoteClient, UUID target)
        {
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(target);
            if (UPI == null)
                return;
            OpenSim.Services.Interfaces.GridUserInfo TargetPI = m_scene.GridUserService.GetGridUserInfo(target.ToString());
            bool isFriend = IsFriendOfUser(remoteClient.AgentId, target);
            if (isFriend)
            {
                uint agentOnline = 0;
                if (TargetPI != null && TargetPI.Online)
                {
                    agentOnline = 16;
                }
                SendProfile(remoteClient, UPI, target, agentOnline);
            }
            else
            {
                UserAccount TargetAccount = m_scene.UserAccountService.GetUserAccount(UUID.Zero, target);
                //See if all can see this person
                //Not a friend, so send the first page only and if they are online
                uint agentOnline = 0;
                if (TargetPI != null && TargetPI.Online && UPI.Visible)
                {
                    agentOnline = 16;
                }

                Byte[] charterMember;
                if (UPI.MembershipGroup == "")
                {
                    charterMember = new Byte[1];
                    charterMember[0] = (Byte)((TargetAccount.UserFlags & 0xf00) >> 8);
                }
                else
                {
                    charterMember = OpenMetaverse.Utils.StringToBytes(UPI.MembershipGroup);
                }
                remoteClient.SendAvatarProperties(UPI.PrincipalID, "",
                                                  Util.ToDateTime(UPI.Created).ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                                                  charterMember, "", (uint)(TargetAccount.UserFlags & agentOnline),
                                                  UUID.Zero, UUID.Zero, "", UUID.Zero);

            }
        }

        public void UpdateAvatarProperties(IClientAPI remoteClient, string AboutText, string FLAboutText, UUID FLImageID, UUID ImageID, string WebProfileURL, bool allowpublish, bool maturepublish)
        {
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            if (UPI == null)
                return;
            
            UPI.Image = ImageID;
            UPI.FirstLifeImage = FLImageID;
            UPI.AboutText = AboutText;
            UPI.FirstLifeAboutText = FLAboutText;
            UPI.WebURL = WebProfileURL;

            UPI.AllowPublish = allowpublish;
            UPI.MaturePublish = maturepublish;
            ProfileFrontend.UpdateUserProfile(UPI);
            SendProfile(remoteClient, UPI, remoteClient.AgentId, 16);
        }

        private void SendProfile(IClientAPI remoteClient, IUserProfileInfo Profile, UUID target, uint agentOnline)
        {
            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, target);
            if (Profile == null || account == null)
                return;
            Byte[] charterMember;
            if (Profile.MembershipGroup == " " || Profile.MembershipGroup == "")
            {
                charterMember = new Byte[1];
                charterMember[0] = (Byte)((account.UserFlags & 0xf00) >> 8);
            }
            else
            {
                charterMember = OpenMetaverse.Utils.StringToBytes(Profile.MembershipGroup);
            }
            uint membershipGroupINT = 0;
            if (Profile.MembershipGroup != "")
                membershipGroupINT = 4;

            uint flags = Convert.ToUInt32(Profile.AllowPublish) + Convert.ToUInt32(Profile.MaturePublish) + membershipGroupINT + (uint)agentOnline + (uint)account.UserFlags;
            remoteClient.SendAvatarInterestsReply(target, Convert.ToUInt32(Profile.Interests.WantToMask), Profile.Interests.WantToText, Convert.ToUInt32(Profile.Interests.CanDoMask), Profile.Interests.CanDoText, Profile.Interests.Languages);
            remoteClient.SendAvatarProperties(Profile.PrincipalID, Profile.AboutText,
                                              Util.ToDateTime(Profile.Created).ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                                              charterMember, Profile.FirstLifeAboutText, flags,
                                              Profile.FirstLifeImage, Profile.Image, Profile.WebURL, new UUID(Profile.Partner));
        }

        #endregion

        #region User Preferences

        public void UserPreferencesRequest(IClientAPI remoteClient)
        {
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            if (UPI == null)
                return;
            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, remoteClient.AgentId);
            remoteClient.SendUserInfoReply(UPI.Visible, UPI.IMViaEmail, account.Email);
        }

        public void UpdateUserPreferences(bool imViaEmail, bool visible, IClientAPI remoteClient)
        {
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            if (UPI == null)
                return;
            UPI.Visible = visible;
            UPI.IMViaEmail = imViaEmail;
            ProfileFrontend.UpdateUserProfile(UPI);
        }

        #endregion

        #region Track Agent

        public void TrackAgent(IClientAPI client, UUID hunter, UUID target)
        {
            bool isFriend = IsFriendOfUser(target, hunter);
            if (isFriend)
            {
                IFriendsModule module = m_Scenes[0].RequestModuleInterface<IFriendsModule>();
                if (module != null)
                {
                    int perms = module.GetFriendPerms(hunter, target);
                    if ((perms & (int)FriendRights.CanSeeOnMap) == (int)FriendRights.CanSeeOnMap)
                    {
                        OpenSim.Services.Interfaces.GridUserInfo GUI = m_scene.GridUserService.GetGridUserInfo(target.ToString());
                        if (GUI != null)
                        {
                            OpenSim.Services.Interfaces.GridRegion region = m_scene.GridService.GetRegionByUUID(UUID.Zero, GUI.LastRegionID);

                            client.SendScriptTeleportRequest(client.Name, region.RegionName,
                                                                               GUI.LastPosition,
                                                                               GUI.LastLookAt);
                        }
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private bool IsFriendOfUser(UUID friend, UUID requested)
        {
            if (friend == requested)
                return true;
            if (m_friendsModule.GetFriendPerms(requested, friend) == -1) //They aren't a friend
            {
                ScenePresence SP = findScenePresence(friend);
                if (SP != null && SP.Scene.Permissions.IsAdministrator(friend)) //Check is admin
                    return true;

                return false;
            }
            return true;
        }

        public ScenePresence findScenePresence(UUID avID)
        {
            foreach (Scene s in m_Scenes)
            {
                ScenePresence SP = s.GetScenePresence(avID);
                if (SP != null)
                {
                    return SP;
                }
            }
            return null;
        }

        #endregion
    }
}
