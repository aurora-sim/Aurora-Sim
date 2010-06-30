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
        private IConfigSource m_gConfig;
        private List<Scene> m_Scenes = new List<Scene>();
        private bool m_ProfileEnabled = true;
        protected IFriendsService m_FriendsService = null;

        #endregion

        #region IRegionModule Members

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
            IConfig friendsConfig = config.Configs["Friends"];
            if (friendsConfig != null)
            {
                int mPort = friendsConfig.GetInt("Port", 0);

                string connector = friendsConfig.GetString("Connector", String.Empty);
                Object[] args = new Object[] { config };

                m_FriendsService = ServerUtils.LoadPlugin<IFriendsService>(connector, args);

            }
            if (m_FriendsService == null)
            {
                m_log.Error("[AuroraProfile]: No Connector defined in section Friends, or filed to load, cannot continue");
                m_ProfileEnabled = false;
            }
            else if (profileConfig.GetString("ProfileModule", Name) != Name)
            {
                m_ProfileEnabled = false;
            }
        }

        public void AddRegion(Scene scene)
        {
            ProfileFrontend = DataManager.DataManager.RequestPlugin<IProfileConnector>("IProfileConnector");

            if (!m_Scenes.Contains(scene))
                m_Scenes.Add(scene);
            m_scene = scene;
            m_scene.EventManager.OnNewClient += NewClient;
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {
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

        public void NewClient(IClientAPI client)
        {
            IUserProfileInfo userProfile = ProfileFrontend.GetUserProfile(client.AgentId);
            if (userProfile == null)
                ProfileFrontend.CreateNewProfile(client.AgentId);

            if (m_ProfileEnabled)
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
        }

        public void RemoveClient(IClientAPI client)
        {
            client.OnRequestAvatarProperties -= RequestAvatarProperty;
            client.OnUpdateAvatarProperties -= UpdateAvatarProperties;
        }

        #endregion

        #region Helpers

        private bool IsFriendOfUser(UUID friend, UUID requested)
        {
            OpenSim.Services.Interfaces.FriendInfo[] friendList = m_FriendsService.GetFriends(requested);
            if (friend == requested)
                return true;

            foreach (OpenSim.Services.Interfaces.FriendInfo item in friendList)
            {
                if (item.PrincipalID == friend)
                {
                    return true;
                }
            }
            ScenePresence sp = m_scene.GetScenePresence(friend);
            if (sp.GodLevel != 0)
                return true;
            return false;
        }

        #endregion

        #region Profile Module

        public void HandleAvatarClassifiedsRequest(Object sender, string method, List<String> args)
        {
            if (!(sender is IClientAPI))
                return;

            IClientAPI remoteClient = (IClientAPI)sender;
            Dictionary<UUID, string> classifieds = new Dictionary<UUID, string>();
            bool isFriend = IsFriendOfUser(remoteClient.AgentId, new UUID(args[0]));
            if (isFriend)
            {
                IUserProfileInfo profile = ProfileFrontend.GetUserProfile(new UUID(args[0]));
                foreach (Classified classified in profile.Classifieds)
                {
                    classifieds.Add(new UUID(classified.ClassifiedUUID), classified.Name);
                }
                remoteClient.SendAvatarClassifiedReply(new UUID(args[0]), classifieds);
            }
            else
            {
                remoteClient.SendAvatarClassifiedReply(new UUID(args[0]), classifieds);
            }          
        }

        public void ClassifiedInfoRequest(UUID queryClassifiedID, IClientAPI remoteClient)
        {
            Classified classified = ProfileFrontend.FindClassified(queryClassifiedID.ToString());
            if (classified != null)
            {
                Vector3 globalPos = new Vector3();
                try
                {
                    Vector3.TryParse(classified.PosGlobal, out globalPos);
                }
                catch
                {
                    globalPos = new Vector3(128, 128, 128);
                }

                remoteClient.SendClassifiedInfoReply(queryClassifiedID, new UUID(classified.CreatorUUID), Convert.ToUInt32(classified.CreationDate), Convert.ToUInt32(classified.ExpirationDate), Convert.ToUInt32(classified.Category), classified.Name, classified.Description, new UUID(classified.ParcelUUID), Convert.ToUInt32(classified.ParentEstate), new UUID(classified.SnapshotUUID), classified.SimName, globalPos, classified.ParcelName, Convert.ToByte(classified.ClassifiedFlags), Convert.ToInt32(classified.PriceForListing));
            }
            else
            {
            }

        }
        
        public void ClassifiedInfoUpdate(UUID queryclassifiedID, uint queryCategory, string queryName, string queryDescription, UUID queryParcelID,
                                         uint queryParentEstate, UUID querySnapshotID, Vector3 queryGlobalPos, byte queryclassifiedFlags,
                                         int queryclassifiedPrice, IClientAPI remoteClient)
        {
            string creatorUUID = remoteClient.AgentId.ToString();
            string classifiedUUID = queryclassifiedID.ToString();
            string category = queryCategory.ToString();
            string name = queryName;
            string description = queryDescription;
            string parentestate = queryParentEstate.ToString();
            string snapshotUUID = querySnapshotID.ToString();
            string simname = remoteClient.Scene.RegionInfo.RegionName;
            string globalpos = queryGlobalPos.ToString();
            string classifiedFlags = queryclassifiedFlags.ToString();
            string classifiedPrice = queryclassifiedPrice.ToString();

            ScenePresence p = m_scene.GetScenePresence(remoteClient.AgentId);
            Vector3 avaPos = p.AbsolutePosition;
            string parceluuid = p.currentParcelUUID.ToString();
            Vector3 posGlobal = new Vector3(remoteClient.Scene.RegionInfo.RegionLocX * Constants.RegionSize + avaPos.X, remoteClient.Scene.RegionInfo.RegionLocY * Constants.RegionSize + avaPos.Y, avaPos.Z);
            string pos_global = posGlobal.ToString();
            ILandObject parcel = m_scene.LandChannel.GetLandObject(p.AbsolutePosition.X, p.AbsolutePosition.Y);
            string parcelname = parcel.LandData.Name;
            string creationdate = Util.UnixTimeSinceEpoch().ToString();
            int expirationdt = Util.UnixTimeSinceEpoch() + (365 * 24 * 60 * 60);
            string expirationdate = expirationdt.ToString();

            #region Checks on empty strings

            if (parcelname == "")
            {
                parcelname = "Unknown";
            }
            if (parceluuid == "")
            {
                parceluuid = "00000000-0000-0000-0000-0000000000000";
            }

            if (description == "")
            {
                description = "No Description";
            }

            #endregion

            Classified classified = new Classified();
            classified.ClassifiedUUID = classifiedUUID;
            classified.CreatorUUID=creatorUUID;
            classified.CreationDate =creationdate;
            classified.ExpirationDate =expirationdate;
            classified.Category = category;
            classified.Name = name;
            classified.Description =description;
            classified.ParcelUUID =parceluuid;
            classified.ParentEstate =parentestate;
            classified.SnapshotUUID=snapshotUUID;
            classified.SimName=simname;
            classified.PosGlobal=globalpos;
            classified.ParcelName=parcelname;
            classified.ClassifiedFlags =classifiedFlags;
            classified.PriceForListing = classifiedPrice;
            Classified OldClassified = ProfileFrontend.FindClassified(classifiedUUID.ToString());
            if (OldClassified != null)
            {
                ProfileFrontend.DeleteClassified(new UUID(classifiedUUID), remoteClient.AgentId);
            }
            ProfileFrontend.AddClassified(classified);
        }
        
        public void ClassifiedDelete(UUID queryClassifiedID, IClientAPI remoteClient)
        {
            ProfileFrontend.DeleteClassified(queryClassifiedID, remoteClient.AgentId);
        }
        
        public void GodClassifiedDelete(UUID queryClassifiedID, IClientAPI remoteClient)
        {
            ScenePresence sp = m_scene.GetScenePresence(remoteClient.AgentId);
            if (sp.GodLevel != 0)
            {
                ProfileFrontend.DeleteClassified(queryClassifiedID, remoteClient.AgentId);
            }
        }
        
        public void HandleAvatarPicksRequest(Object sender, string method, List<String> args)
        {
            if (!(sender is IClientAPI))
            {
                m_log.Debug("sender isnt IClientAPI");
                return;
            }
            IClientAPI remoteClient = (IClientAPI)sender;
            Dictionary<UUID, string> picks = new Dictionary<UUID, string>();
            bool isFriend = IsFriendOfUser(remoteClient.AgentId, new UUID(args[0]));
            if (isFriend)
            {
                IUserProfileInfo profile = ProfileFrontend.GetUserProfile(new UUID(args[0]));

                foreach (ProfilePickInfo pick in profile.Picks)
                {
                    picks.Add(new UUID(pick.pickuuid), pick.name);
                }
                remoteClient.SendAvatarPicksReply(new UUID(args[0]), picks);
            }
            else
            {
                remoteClient.SendAvatarPicksReply(new UUID(args[0]), picks);
            }
        }

        public void HandlePickInfoRequest(Object sender, string method, List<String> args)
        {
            if (!(sender is IClientAPI))
            {
                m_log.Debug("sender isnt IClientAPI");
                return;
            }
            IClientAPI remoteClient = (IClientAPI)sender;
            
            ProfilePickInfo pick = ProfileFrontend.FindPick(args[1]);
            if (pick != null)
            {
                Vector3 globalPos = new Vector3();
                try
                {
                    Vector3.TryParse(pick.posglobal, out globalPos);
                }
                catch (Exception ex)
                {
                    ex = new Exception();
                    globalPos = new Vector3(128, 128, 128);
                }
                bool two = false;
                int ten = 0;
                bool twelve = false;

                try
                {
                    two = Convert.ToBoolean(pick.toppick);
                    ten = Convert.ToInt32(pick.sortorder);
                    twelve = Convert.ToBoolean(pick.enabled);
                }
                catch (Exception ex)
                {
                    ex = new Exception();
                    two = false;
                    ten = 0;
                    twelve = true;
                }
                remoteClient.SendPickInfoReply(new UUID(pick.pickuuid), new UUID(pick.creatoruuid), two, new UUID(pick.parceluuid), pick.name, pick.description, new UUID(pick.snapshotuuid), pick.user, pick.originalname, pick.simname, globalPos, ten, twelve);
            }
        }
        
        public void PickInfoUpdate(IClientAPI remoteClient, UUID pickID, UUID creatorID, bool topPick, string name, string desc, UUID snapshotID, int sortOrder, bool enabled)
        {
            ProfilePickInfo oldpick = ProfileFrontend.FindPick(pickID.ToString());
            ScenePresence p = m_scene.GetScenePresence(remoteClient.AgentId);
            Vector3 avaPos = p.AbsolutePosition;

            string parceluuid = p.currentParcelUUID.ToString();
            Vector3 posGlobal = new Vector3(avaPos.X, avaPos.Y, avaPos.Z);
            posGlobal.X += p.Scene.RegionInfo.RegionLocX * 256;
            posGlobal.Y += p.Scene.RegionInfo.RegionLocY * 256;


            string pos_global = posGlobal.ToString();

            ILandObject targetlandObj = m_scene.LandChannel.GetLandObject(avaPos.X, avaPos.Y);
            UUID ownerid = targetlandObj.LandData.OwnerID;
            ScenePresence parcelowner = m_scene.GetScenePresence(ownerid);
            string parcelfirst;
            string parcellast;
            try
            {
                parcelfirst = parcelowner.Firstname;
                parcellast = parcelowner.Lastname;

            }
            catch (Exception ex)
            {
                ex = new Exception();
                parcelfirst = "";
                parcellast = "";
            }
            string user = parcelfirst + " " + parcellast;

            string OrigionalName = targetlandObj.LandData.Name;

            #region Checks on empty strings

            if (parceluuid == "")
            {
                parceluuid = "00000000-0000-0000-0000-0000000000000";
            }
            if (desc == "")
            {
                desc = " ";
            }


            #endregion

            if (oldpick == null)
            {
                ProfilePickInfo values = new ProfilePickInfo();
                values.pickuuid = pickID.ToString();
                values.creatoruuid = creatorID.ToString();
                values.toppick = topPick.ToString();
                values.parceluuid = parceluuid.ToString();
                values.name = name;
                values.description = desc;
                values.snapshotuuid = snapshotID.ToString();
                values.user = user;
                values.originalname = OrigionalName;
                values.simname = remoteClient.Scene.RegionInfo.RegionName;
                values.posglobal = pos_global;
                values.sortorder = sortOrder.ToString();
                values.enabled = enabled.ToString();
                ProfileFrontend.AddPick(values);
            }
            else
            {
                oldpick.parceluuid = parceluuid.ToString();
                oldpick.name = name;
                oldpick.snapshotuuid = snapshotID.ToString();
                oldpick.description = desc;
                oldpick.simname = remoteClient.Scene.RegionInfo.RegionName;
                oldpick.posglobal = pos_global;
                oldpick.sortorder = sortOrder.ToString();
                oldpick.enabled = enabled.ToString();
                ProfileFrontend.UpdatePick(oldpick);
            }
        }
        
        public void GodPickDelete(IClientAPI remoteClient, UUID AgentID, UUID PickID, UUID queryID)
        {
            ScenePresence sp = m_scene.GetScenePresence(remoteClient.AgentId);
            if (sp.GodLevel != 0)
            {
                ProfileFrontend.DeletePick(PickID, AgentID);
            }
        }
        
        public void PickDelete(IClientAPI remoteClient, UUID queryPickID)
        {
            ProfileFrontend.DeletePick(queryPickID, remoteClient.AgentId);
        }

        public void HandleAvatarNotesRequest(Object sender, string method, List<String> args)
        {
            if (!(sender is IClientAPI))
            {
                m_log.Debug("sender isnt IClientAPI");
                return;
            }
            IClientAPI remoteClient = (IClientAPI)sender;
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            string notes = "";
            UPI.Notes.TryGetValue(args[0], out notes);
            if (notes == null || notes == "")
            {
                AvatarNotesUpdate(remoteClient, new UUID(args[0]), "");
                UPI = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
                UPI.Notes.TryGetValue(args[0], out notes);
            }
            remoteClient.SendAvatarNotesReply(new UUID(args[0]), notes);
        }
        
        public void AvatarNotesUpdate(IClientAPI remoteClient, UUID queryTargetID, string queryNotes)
        {
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            string notes;
            if (queryNotes == "")
            {
                notes = "Insert your notes here.";
            }
            else
                notes = queryNotes;
            string oldNotes;
            if (UPI.Notes.TryGetValue(queryTargetID.ToString(), out oldNotes))
                UPI.Notes.Remove(queryTargetID.ToString());
            
            UPI.Notes.Add(queryTargetID.ToString(), notes);
            ProfileFrontend.UpdateUserNotes(remoteClient.AgentId, queryTargetID, notes, UPI);
        }

        public void AvatarInterestsUpdate(IClientAPI remoteClient, uint wantmask, string wanttext, uint skillsmask, string skillstext, string languages)
        {
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            UPI.Interests.WantToMask = wantmask.ToString();
            UPI.Interests.WantToText = wanttext;
            UPI.Interests.CanDoMask = skillsmask.ToString();
            UPI.Interests.CanDoText = skillstext;
            UPI.Interests.Languages = languages;
            ProfileFrontend.UpdateUserInterests(UPI);
        }

        public void RequestAvatarProperty(IClientAPI remoteClient, UUID target)
        {
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(target);
            OpenSim.Services.Interfaces.GridUserInfo TargetPI = m_scene.GridUserService.GetGridUserInfo(target.ToString());
            bool isFriend = IsFriendOfUser(remoteClient.AgentId, target);
            if (isFriend)
            {
                uint agentOnline = 0;
                if (TargetPI.Online)
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
                if (TargetPI.Online && UPI.Visible)
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

        public void UpdateAvatarProperties(IClientAPI remoteClient, OpenSim.Framework.UserProfileData newProfile, bool allowpublish, bool maturepublish)
        {
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(newProfile.ID);
            
            UPI.Image = newProfile.Image;
            UPI.FirstLifeImage = newProfile.FirstLifeImage;
            UPI.AboutText = newProfile.AboutText;
            UPI.FirstLifeAboutText = newProfile.FirstLifeAboutText;
            if (newProfile.ProfileUrl != "")
            {
                UPI.WebURL = newProfile.ProfileUrl;
            }

            UPI.AllowPublish = allowpublish;
            UPI.MaturePublish = maturepublish;
            ProfileFrontend.UpdateUserProfile(UPI);
            SendProfile(remoteClient, UPI, remoteClient.AgentId, 16);
        }

        private void SendProfile(IClientAPI remoteClient, IUserProfileInfo Profile, UUID target, uint agentOnline)
        {
            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, target);
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
        
        public void UserPreferencesRequest(IClientAPI remoteClient)
        {
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, remoteClient.AgentId);
            remoteClient.SendUserInfoReply(UPI.Visible, UPI.IMViaEmail, account.Email);
        }

        public void UpdateUserPreferences(bool imViaEmail, bool visible, IClientAPI remoteClient)
        {
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(remoteClient.AgentId);
            UPI.Visible = visible;
            UPI.IMViaEmail = imViaEmail;
            ProfileFrontend.UpdateUserProfile(UPI);
        }
        #endregion
    }
}
