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
    public class AuroraProfileModule : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;
        private IConfigSource m_config;
        private Dictionary<string, Dictionary<UUID, string>> ClassifiedsCache = new Dictionary<string, Dictionary<UUID, string>>();
        private Dictionary<string, List<string>> ClassifiedInfoCache = new Dictionary<string, List<string>>();
        private IProfileData ProfileData = null;
        private IGenericData GenericData = null;
        private IConfigSource m_gConfig;
        private List<Scene> m_Scenes = new List<Scene>();
        private string m_SearchServer = "";
        private bool m_SearchEnabled = true;
        private bool m_ProfileEnabled = true;
        protected IFriendsService m_FriendsService = null;
        protected IGroupsModule GroupsModule = null;

        protected double parserTime = 60000;
        #region IRegionModule Members

        public void Initialise(Scene scene, IConfigSource config)
        {
            m_config = config;
            IConfig profileConfig = config.Configs["Profile"];
            if (profileConfig == null)
            {
                m_log.Info("[AuroraProfileSearch] Not configured, disabling");
                m_SearchEnabled = false;
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
            parserTime = profileConfig.GetDouble("ParserTime", 60000);
            if (m_FriendsService == null)
            {
                m_log.Error("[AuroraProfileSearch]: No Connector defined in section Friends, or filed to load, cannot continue");
                m_ProfileEnabled = false;
                m_SearchEnabled = false;
            }
            else if (profileConfig.GetString("ProfileModule", Name) != Name)
            {
                m_ProfileEnabled = false;
            }
            else if (profileConfig.GetString("SearchModule", Name) != Name)
            {
                m_SearchEnabled = false;
            }

            if (!m_Scenes.Contains(scene))
                m_Scenes.Add(scene);
            m_scene = scene;
            m_gConfig = config;
            m_scene.EventManager.OnNewClient += NewClient;
        }
        private IDataSnapshot DataSnapShotManager;
        public void PostInitialise()
        {
            ProfileData = Aurora.DataManager.DataManager.GetProfilePlugin();
            GenericData = Aurora.DataManager.DataManager.GetGenericPlugin();
            GroupsModule = m_scene.RequestModuleInterface<IGroupsModule>();
            DataSnapShotManager = m_scene.RequestModuleInterface<IDataSnapshot>();
            if (m_SearchEnabled)
                StartSearch();
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "AuroraProfileSearch"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public Scene World
        {
            get { return m_scene; }
        }
        #endregion
        
        public void NewClient(IClientAPI client)
        {
            if (m_ProfileEnabled)
            {
                client.OnRequestAvatarProperties += RequestAvatarProperty;
                client.OnUpdateAvatarProperties += UpdateAvatarProperties;
                client.AddGenericPacketHandler("avatarclassifiedsrequest", HandleAvatarClassifiedsRequest);
                client.OnClassifiedInfoRequest += ProfileClassifiedInfoRequest;
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
            if(m_SearchEnabled)
            {
                // Subscribe to messages
                client.OnDirPlacesQuery += DirPlacesQuery;
                client.OnDirFindQuery += DirFindQuery;
                client.OnDirPopularQuery += DirPopularQuery;
                client.OnDirLandQuery += DirLandQuery;
                client.OnDirClassifiedQuery += DirClassifiedQuery;
                // Response after Directory Queries
                client.OnEventInfoRequest += EventInfoRequest;
                client.OnClassifiedInfoRequest += ClassifiedInfoRequest;
                client.OnMapItemRequest += HandleMapItemRequest;
                client.OnPlacesQuery += OnPlacesQueryRequest;
            }
        }

        public void RemoveClient(IClientAPI client)
        {
            client.OnRequestAvatarProperties -= RequestAvatarProperty;
            client.OnUpdateAvatarProperties -= UpdateAvatarProperties;
        }
        
        #region Profile Module

        public void HandleAvatarClassifiedsRequest(Object sender, string method, List<String> args)
        {
            if (!(sender is IClientAPI))
                return;

            IClientAPI remoteClient = (IClientAPI)sender;
            ScenePresence sp = m_scene.GetScenePresence(remoteClient.AgentId);
            OpenSim.Services.Interfaces.FriendInfo[] friendList = m_FriendsService.GetFriends(new UUID(args[0]));
            Dictionary<UUID, string> classifieds;
            if (new UUID(args[0]) != remoteClient.AgentId)
            {
                foreach (OpenSim.Services.Interfaces.FriendInfo item in friendList)
                {
                    if (item.PrincipalID == remoteClient.AgentId)
                    {
                        classifieds = ProfileData.ReadClassifedRow(args[0]);
                        remoteClient.SendAvatarClassifiedReply(new UUID(args[0]), classifieds);
                        return;
                    }
                }
                if (sp.GodLevel != 0)
                {
                    classifieds = ProfileData.ReadClassifedRow(args[0]);
                    remoteClient.SendAvatarClassifiedReply(new UUID(args[0]), classifieds);
                }
            }
            else
            {
                classifieds = ProfileData.ReadClassifedRow(args[0]);
                remoteClient.SendAvatarClassifiedReply(new UUID(args[0]), classifieds);
            }
        }
        private void ProfileClassifiedInfoRequest(UUID classifiedID, IClientAPI client)
        {
            if (!(client is IClientAPI))
            {
                m_log.Debug("sender isnt IClientAPI");
                return;
            }
            IClientAPI remoteClient = (IClientAPI)client;
            List<string> classifiedinfo;
            classifiedinfo = ProfileData.ReadClassifiedInfoRow(classifiedID.ToString());
            Vector3 globalPos = new Vector3();
            try
            {
                Vector3.TryParse(classifiedinfo[10], out globalPos);
            }
            catch (Exception ex)
            {
                ex = new Exception();
                globalPos = new Vector3(128, 128, 128);
            }
            remoteClient.SendClassifiedInfoReply(classifiedID, new UUID(classifiedinfo[0]), Convert.ToUInt32(classifiedinfo[1]), Convert.ToUInt32(classifiedinfo[2]), Convert.ToUInt32(classifiedinfo[3]), classifiedinfo[4], classifiedinfo[5], new UUID(classifiedinfo[6]), Convert.ToUInt32(classifiedinfo[7]), new UUID(classifiedinfo[8]), classifiedinfo[9], globalPos, classifiedinfo[11], Convert.ToByte(classifiedinfo[12]), Convert.ToInt32(classifiedinfo[13]));
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

            ScenePresence p = World.GetScenePresence(remoteClient.AgentId);
            Vector3 avaPos = p.AbsolutePosition;
            string parceluuid = p.currentParcelUUID.ToString();
            Vector3 posGlobal = new Vector3(remoteClient.Scene.RegionInfo.RegionLocX * Constants.RegionSize + avaPos.X, remoteClient.Scene.RegionInfo.RegionLocY * Constants.RegionSize + avaPos.Y, avaPos.Z);
            string pos_global = posGlobal.ToString();
            ILandObject parcel = World.LandChannel.GetLandObject(p.AbsolutePosition.X, p.AbsolutePosition.Y);
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

            List<string> values = new List<string>();
            values.Add(classifiedUUID);
            values.Add(creatorUUID);
            values.Add(creationdate);
            values.Add(expirationdate);
            values.Add(category);
            values.Add(name);
            values.Add(description);
            values.Add(parceluuid);
            values.Add(parentestate);
            values.Add(snapshotUUID);
            values.Add(simname);
            values.Add(globalpos);
            values.Add(parcelname);
            values.Add(classifiedFlags);
            values.Add(classifiedPrice);
            GenericData.Insert("classifieds", values.ToArray());
        }
        public void ClassifiedDelete(UUID queryClassifiedID, IClientAPI remoteClient)
        {
            List<string> keys = new List<string>();
            List<string> values = new List<string>();
            keys.Add("classifieduuid");
            values.Add(queryClassifiedID.ToString());
            GenericData.Delete("classifieds",keys.ToArray(), values.ToArray());

        }
        public void GodClassifiedDelete(UUID queryClassifiedID, IClientAPI remoteClient)
        {
            ScenePresence sp = m_scene.GetScenePresence(remoteClient.AgentId);
            if (sp.GodLevel != 0)
            {
                List<string> keys = new List<string>();
                List<string> values = new List<string>();
                keys.Add("classifieduuid");
                values.Add(queryClassifiedID.ToString());
                GenericData.Delete("classifieds", keys.ToArray(), values.ToArray());
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
            ScenePresence sp = m_scene.GetScenePresence(remoteClient.AgentId);
            OpenSim.Services.Interfaces.FriendInfo[] friendList = m_FriendsService.GetFriends(new UUID(args[0]));
            if (new UUID(args[0]) != remoteClient.AgentId)
            {
                foreach (OpenSim.Services.Interfaces.FriendInfo item in friendList)
                {
                    if (item.PrincipalID == remoteClient.AgentId)
                    {
                        Dictionary<UUID, string> pickuuid = ProfileData.ReadPickRow(args[0]);
                        Dictionary<UUID, string> picks = new Dictionary<UUID, string>();

                        if (pickuuid == null)
                        {
                            remoteClient.SendAvatarPicksReply(new UUID(args[0]), picks);
                        }
                        else
                        {
                            picks = pickuuid;
                            remoteClient.SendAvatarPicksReply(new UUID(args[0]), picks);
                        }
                    }
                }
                if (sp.GodLevel != 0)
                {
                    Dictionary<UUID, string> pickuuid = ProfileData.ReadPickRow(args[0]);
                    Dictionary<UUID, string> picks = new Dictionary<UUID, string>();

                    if (pickuuid == null)
                    {
                        remoteClient.SendAvatarPicksReply(new UUID(args[0]), picks);
                    }
                    else
                    {
                        picks = pickuuid;
                        remoteClient.SendAvatarPicksReply(new UUID(args[0]), picks);
                    }
                }
            }
            else
            {
                Dictionary<UUID, string> pickuuid = ProfileData.ReadPickRow(args[0]);
                Dictionary<UUID, string> picks = new Dictionary<UUID, string>();

                if (pickuuid == null)
                {
                    remoteClient.SendAvatarPicksReply(new UUID(args[0]), picks);
                }
                else
                {
                    picks = pickuuid;
                    remoteClient.SendAvatarPicksReply(new UUID(args[0]), picks);
                }
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
            string avatar_id = args[0];
            string pick_id = args[1];

            List<string> pickinfo = ProfileData.ReadPickInfoRow(avatar_id,pick_id);
            Vector3 globalPos = new Vector3();
            try
            {
                Vector3.TryParse(pickinfo[10], out globalPos);
            }
            catch (Exception ex)
            {
                ex = new Exception();
                globalPos = new Vector3(128, 128, 128);
            }
            bool two = false;
            int ten = 0;
            bool twelve = false;

            #region Check for null values

            if (pickinfo[0] == null)
            {
                pickinfo[0] = "";
            }
            if (pickinfo[1] == null)
            {
                pickinfo[1] = "";
            }
            if (pickinfo[2] == null)
            {
                pickinfo[2] = "false";
            }
            if (pickinfo[3] == null)
            {
                pickinfo[3] = "";
            }
            if (pickinfo[4] == null)
            {
                pickinfo[4] = "";
            }
            if (pickinfo[5] == null)
            {
                pickinfo[5] = "";
            }
            if (pickinfo[6] == null)
            {
                pickinfo[6] = "";
            }
            if (pickinfo[7] == null)
            {
                pickinfo[7] = "";
            }
            if (pickinfo[8] == null)
            {
                pickinfo[8] = "";
            }
            if (pickinfo[9] == null)
            {
                pickinfo[9] = "";
            }
            if (pickinfo[11] == null)
            {
                pickinfo[11] = "0";
            }
            if (pickinfo[12] == null)
            {
                pickinfo[12] = "false";
            }
            #endregion

            try
            {
                two = Convert.ToBoolean(pickinfo[2]);
                ten = Convert.ToInt32(pickinfo[11]);
                twelve = Convert.ToBoolean(pickinfo[12]);
            }
            catch (Exception ex)
            {
                ex = new Exception();
                two = false;
                ten = 0;
                twelve = true;
            }
            remoteClient.SendPickInfoReply(new UUID(pickinfo[0]), new UUID(pickinfo[1]), two, new UUID(pickinfo[3]), pickinfo[4], pickinfo[5], new UUID(pickinfo[6]), pickinfo[7], pickinfo[8], pickinfo[9], globalPos, ten, twelve);
        }
        public void PickInfoUpdate(IClientAPI remoteClient, UUID pickID, UUID creatorID, bool topPick, string name, string desc, UUID snapshotID, int sortOrder, bool enabled)
        {
            string pick = ProfileData.Query("select pickuuid from userpicks where creatoruuid = '" + creatorID + "' AND pickuuid = '" + pickID.ToString() + "'")[0];
            ScenePresence p = World.GetScenePresence(remoteClient.AgentId);
            Vector3 avaPos = p.AbsolutePosition;

            string parceluuid = p.currentParcelUUID.ToString();
            Vector3 posGlobal = new Vector3(avaPos.X, avaPos.Y, avaPos.Z);

            string pos_global = posGlobal.ToString();

            ILandObject targetlandObj = World.LandChannel.GetLandObject(avaPos.X, avaPos.Y);
            UUID ownerid = targetlandObj.LandData.OwnerID;
            ScenePresence parcelowner = World.GetScenePresence(ownerid);
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

            if (pick == "")
            {
                List<string> values = new List<string>();
                values.Add(pickID.ToString());
                values.Add(creatorID.ToString());
                values.Add(topPick.ToString());
                values.Add(parceluuid.ToString());
                values.Add(name);
                values.Add(desc);
                values.Add(snapshotID.ToString());
                values.Add(user);
                values.Add(OrigionalName);
                values.Add(remoteClient.Scene.RegionInfo.RegionName);
                values.Add(pos_global);
                values.Add(sortOrder.ToString());
                values.Add(enabled.ToString());
                GenericData.Insert("userpicks", values.ToArray());
            }
            else
            {
                List<string> keys = new List<string>();
                List<string> values = new List<string>();
                keys.Add("parceluuid");
                keys.Add("name");
                keys.Add("snapshotuuid");
                keys.Add("description");
                keys.Add("simname");
                keys.Add("posglobal");
                keys.Add("sortorder");
                keys.Add("enabled");
                values.Add(parceluuid.ToString());
                values.Add(name);
                values.Add(snapshotID.ToString());
                values.Add(desc);
                values.Add(remoteClient.Scene.RegionInfo.RegionName);
                values.Add(pos_global);
                values.Add(sortOrder.ToString());
                values.Add(enabled.ToString());
                List<string> keys2 = new List<string>();
                keys2.Add("pickuuid");
                List<string> values2 = new List<string>();
                values2.Add(pickID.ToString());
                GenericData.Update("userpicks", values.ToArray(), keys.ToArray(), keys2.ToArray(), values2.ToArray());
            }
        }
        public void GodPickDelete(IClientAPI remoteClient, UUID AgentID, UUID PickID, UUID queryID)
        {
            ScenePresence sp = m_scene.GetScenePresence(remoteClient.AgentId);
            if (sp.GodLevel != 0)
            {
                List<string> keys = new List<string>();
                List<string> values = new List<string>();
                keys.Add("pickuuid");
                values.Add(PickID.ToString());
                GenericData.Delete("userpicks", keys.ToArray(), values.ToArray());
            }
        }
        public void PickDelete(IClientAPI remoteClient, UUID queryPickID)
        {
            List<string> keys = new List<string>();
            List<string> values = new List<string>();
            keys.Add("pickuuid");
            values.Add(queryPickID.ToString());
            GenericData.Delete("userpicks", keys.ToArray(), values.ToArray());
        }
        public void HandleAvatarNotesRequest(Object sender, string method, List<String> args)
        {
            if (!(sender is IClientAPI))
            {
                m_log.Debug("sender isnt IClientAPI");
                return;
            }
            IClientAPI remoteClient = (IClientAPI)sender;
            AuroraProfileData UserProfile = ProfileData.GetProfileNotes(remoteClient.AgentId, new UUID(args[0]));
            string notes;
            UserProfile.Notes.TryGetValue(new UUID(args[0]), out notes);
            remoteClient.SendAvatarNotesReply(new UUID(args[0]), notes);
        }
        public void AvatarNotesUpdate(IClientAPI remoteClient, UUID queryTargetID, string queryNotes)
        {
            string notes;
            if (queryNotes == "")
            {
                notes = "Insert your notes here.";
            }
            else
                notes = queryNotes;
            List<string> keys = new List<string>();
            List<string> values = new List<string>();
            keys.Add("notes");
            values.Add(notes);
            List<string> keys2 = new List<string>();
            List<string> values2 = new List<string>();
            keys2.Add("targetuuid");
            values2.Add(queryTargetID.ToString());
            GenericData.Update("usernotes", values.ToArray(), keys.ToArray(), keys2.ToArray(), values2.ToArray());
            ProfileData.InvalidateProfileNotes(queryTargetID);
        }

        public void AvatarInterestsUpdate(IClientAPI remoteClient, uint wantmask, string wanttext, uint skillsmask, string skillstext, string languages)
        {
            AuroraProfileData targetprofile = ProfileData.GetProfileInfo(remoteClient.AgentId);
            targetprofile.Interests[0] = wantmask.ToString();
            targetprofile.Interests[1] = wanttext;
            targetprofile.Interests[2] = skillsmask.ToString();
            targetprofile.Interests[3] = skillstext;
            targetprofile.Interests[4] = languages;
            ProfileData.UpdateUserProfile(targetprofile);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="avatarID"></param>
        public void RequestAvatarProperty(IClientAPI remoteClient, UUID target)
        {
            AuroraProfileData targetprofile = ProfileData.GetProfileInfo(target);
            OpenSim.Services.Interfaces.FriendInfo[] friendList = m_FriendsService.GetFriends(target);
            ScenePresence RequestorSP = m_scene.GetScenePresence(remoteClient.AgentId);
            ScenePresence TargetSP = m_scene.GetScenePresence(target);

            if (target != remoteClient.AgentId)
            {
                if (ProfileData.Query("select visible from usersauth where userUUID = '" + remoteClient.AgentId.ToString() + "'")[0] == "true")
                {
                    foreach (OpenSim.Services.Interfaces.FriendInfo item in friendList)
                    {
                        if (item.PrincipalID == remoteClient.AgentId)
                        {
                            if (null != targetprofile)
                            {
                                uint agentOnline = 0;
                                /*if (m_scene.PresenceService.GetAgents(
                                {
                                    if ((item.MyFlags & (uint)FriendRights.CanSeeOnline) != 0)
                                    {
                                        agentOnline = 16;
                                    }
                                }*/
                                SendProfile(remoteClient, targetprofile, target, agentOnline);
                                remoteClient.SendAvatarInterestsReply(target, Convert.ToUInt32(targetprofile.Interests[0]), targetprofile.Interests[1], Convert.ToUInt32(targetprofile.Interests[2]), targetprofile.Interests[3], targetprofile.Interests[4]);
                                return;
                            }
                            else
                            {
                                m_log.Debug("[AvatarProfilesModule]: Got null for profile for " + target.ToString());
                            }
                        }
                    }
                    if (RequestorSP.GodLevel != 0)
                    {
                        if (null != targetprofile)
                        {
                            uint agentOnline = 0;
                            /*if (agent.AgentOnline)
                            {
                                agentOnline = 16;
                            }*/
                            SendProfile(remoteClient, targetprofile , target, agentOnline);
                            remoteClient.SendAvatarInterestsReply(target, Convert.ToUInt32(targetprofile.Interests[0]), targetprofile.Interests[1], Convert.ToUInt32(targetprofile.Interests[2]), targetprofile.Interests[3], targetprofile.Interests[4]);
                            return;
                        }
                        else
                        {
                            m_log.Debug("[AvatarProfilesModule]: Got null for profile for " + target.ToString());
                        }
                    }
                    Byte[] charterMember;
                    if (targetprofile.CustomType == "")
                    {
                        charterMember = new Byte[1];
                        charterMember[0] = (Byte)((targetprofile.UserFlags & 0xf00) >> 8);
                    }
                    else
                    {
                        charterMember = OpenMetaverse.Utils.StringToBytes(targetprofile.CustomType);
                    }
                    remoteClient.SendAvatarProperties(new UUID(targetprofile.Identifier), "",
                                                      Util.ToDateTime(targetprofile.Created).ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                                                      charterMember, "", (uint)(targetprofile.UserFlags & 0xff),
                                                      UUID.Zero, UUID.Zero, "", UUID.Zero);
                }
                else
                {
                    uint agentOnline = 0;
                    /*if (agent.AgentOnline)
                    {
                        agentOnline = 16;
                    }*/

                    foreach (OpenSim.Services.Interfaces.FriendInfo item in friendList)
                    {
                        if (item.PrincipalID == remoteClient.AgentId)
                        {
                            if (null != targetprofile)
                            {
                                SendProfile(remoteClient, targetprofile, target, agentOnline);
                                remoteClient.SendAvatarInterestsReply(target, Convert.ToUInt32(targetprofile.Interests[0]), targetprofile.Interests[1], Convert.ToUInt32(targetprofile.Interests[2]), targetprofile.Interests[3], targetprofile.Interests[4]);
                                return;
                            }
                            else
                            {
                                m_log.Debug("[AvatarProfilesModule]: Got null for profile for " + target.ToString());
                            }
                        }
                    }
                    if (RequestorSP.GodLevel != 0)
                    {
                        if (null != targetprofile)
                        {
                            SendProfile(remoteClient, targetprofile, target, agentOnline);
                            remoteClient.SendAvatarInterestsReply(target, Convert.ToUInt32(targetprofile.Interests[0]), targetprofile.Interests[1], Convert.ToUInt32(targetprofile.Interests[2]), targetprofile.Interests[3], targetprofile.Interests[4]);
                            return;
                        }
                        else
                        {
                            m_log.Debug("[AvatarProfilesModule]: Got null for profile for " + target.ToString());
                        }
                    }
                    Byte[] charterMember;
                    if (targetprofile.CustomType == "")
                    {
                        charterMember = new Byte[1];
                        charterMember[0] = (Byte)((targetprofile.UserFlags & 0xf00) >> 8);
                    }
                    else
                    {
                        charterMember = OpenMetaverse.Utils.StringToBytes(targetprofile.CustomType);
                    }
                    remoteClient.SendAvatarProperties(new UUID(targetprofile.Identifier), "",
                                                      Util.ToDateTime(targetprofile.Created).ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                                                      charterMember, "", (uint)(targetprofile.UserFlags & 0xff),
                                                      UUID.Zero, UUID.Zero, "", UUID.Zero);
                }
            }
            else
            {
                if (null != targetprofile)
                {
                    SendProfile(remoteClient, targetprofile, target, 0);
                    remoteClient.SendAvatarInterestsReply(target, Convert.ToUInt32(targetprofile.Interests[0]), targetprofile.Interests[1], Convert.ToUInt32(targetprofile.Interests[2]), targetprofile.Interests[3], targetprofile.Interests[4]);
                }
                else
                {
                    m_scene.RequestModuleInterface<IAuthService>().CreateUserAuth(target.ToString(), TargetSP.Firstname, TargetSP.Lastname);
                    m_log.Debug("[AvatarProfilesModule]: Got null for profile for " + target.ToString());
                    RequestAvatarProperty(remoteClient, target);
                }
            }
        }

        public void UpdateAvatarProperties(IClientAPI remoteClient, OpenSim.Framework.UserProfileData newProfile/*, bool allowpublish, bool maturepublish*/)
        {
            int allowpublishINT = 0;
            int maturepublishINT = 0;
            /*if (allowpublish == true)
                allowpublishINT = 1;
            if (maturepublish == true)
                maturepublishINT = 1;*/
            AuroraProfileData Profile = ProfileData.GetProfileInfo(remoteClient.AgentId);
            // if it's the profile of the user requesting the update, then we change only a few things.

            if (remoteClient.AgentId.ToString() == Profile.Identifier)
            {
                Profile.Image = newProfile.Image;
                Profile.FirstLifeImage = newProfile.FirstLifeImage;
                Profile.AboutText = newProfile.AboutText;
                Profile.FirstLifeAboutText = newProfile.FirstLifeAboutText;
                if (newProfile.ProfileUrl != "")
                {
                    Profile.ProfileURL = newProfile.ProfileUrl;
                }
            }
            else
                return;

            Profile.AllowPublish = allowpublishINT.ToString();
            Profile.MaturePublish = maturepublishINT.ToString();
            ProfileData.UpdateUserProfile(Profile);
            SendProfile(remoteClient, Profile, remoteClient.AgentId, 16);
        }

        private void SendProfile(IClientAPI remoteClient, AuroraProfileData Profile, UUID target, uint agentOnline)
        {
            Byte[] charterMember;
            if (Profile.CustomType == " ")
            {
                charterMember = new Byte[1];
                charterMember[0] = (Byte)((Profile.UserFlags & 0xf00) >> 8);
            }
            else
            {
                charterMember = OpenMetaverse.Utils.StringToBytes(Profile.CustomType);
            }
            uint membershipGroupINT = 0;
            if (Profile.MembershipGroup != "")
                membershipGroupINT = 4;

            uint flags = Convert.ToUInt32(Profile.AllowPublish) + Convert.ToUInt32(Profile.MaturePublish) + membershipGroupINT + (uint)agentOnline + (uint)Profile.UserFlags;

            remoteClient.SendAvatarProperties(new UUID(Profile.Identifier), Profile.AboutText,
                                              Util.ToDateTime(Profile.Created).ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                                              charterMember, Profile.FirstLifeAboutText, flags,
                                              Profile.FirstLifeImage, Profile.Image, Profile.ProfileURL, new UUID(Profile.Partner));
        }
        
        public void UserPreferencesRequest(IClientAPI remoteClient)
        {
            List<string> UserInfo = ProfileData.Query("select imviaemail,visible,email from usersauth where userUUID = '" + remoteClient.AgentId.ToString() + "'");
            remoteClient.SendUserInfoReply(
                Convert.ToBoolean(UserInfo[0]),
                Convert.ToBoolean(UserInfo[1]),
                UserInfo[2]);
        }

        public void UpdateUserPreferences(bool imViaEmail, bool visible, IClientAPI remoteClient)
        {
            List<string> keys = new List<string>();
            List<string> values = new List<string>();
            keys.Add("imviaemail");
            values.Add(imViaEmail.ToString());
            keys.Add("visible");
            values.Add(visible.ToString());
            List<string> keys2 = new List<string>();
            List<string> values2 = new List<string>();
            keys2.Add("UUID");
            values2.Add(remoteClient.AgentId.ToString());
            GenericData.Update("users", values.ToArray(), keys.ToArray(), values2.ToArray(), keys2.ToArray());
        }
        #endregion

        #region Search Module
        //FIXME
        protected void DirPlacesQuery(IClientAPI remoteClient, UUID queryID,
                                      string queryText, int queryFlags, int category, string simName,
                                      int queryStart)
        {
        	//TODO: Add queryStart!!!
        	DirPlacesReplyData[] ReturnValues = ProfileData.PlacesQuery(queryText,category.ToString(),"parcels","PID,PName,PForSale,PAuction,PDwell");

            DirPlacesReplyData[] data = new DirPlacesReplyData[10];

            int i = 0;
            foreach (DirPlacesReplyData d in ReturnValues)
            {
            	data[i] = d;
            	i++;
            	if(i == 10)
            	{
                	remoteClient.SendDirPlacesReply(queryID, data);
                    i = 0;
                    data = new DirPlacesReplyData[10];
            	}
            }
            remoteClient.SendDirPlacesReply(queryID, data);
        }

        public void DirPopularQuery(IClientAPI remoteClient, UUID queryID, uint queryFlags)
        {
        	
        	/// <summary>
        	/// Decapriated.
        	/// </summary>
        }
        
        //FIXME
        public void DirLandQuery(IClientAPI remoteClient, UUID queryID,
                                 uint queryFlags, uint searchType, int price, int area,
                                 int queryStart)
        {
			//TODO: Add queryStart!!!
            DirLandReplyData[] ReturnValues = ProfileData.LandForSaleQuery(searchType.ToString(),price.ToString(),area.ToString(),"forsaleparcels","PID,PName,PForSale,PAuction,PSalePrice,PActualArea");

            DirLandReplyData[] data = new DirLandReplyData[10];

            int i = 0;
            foreach (DirLandReplyData d in ReturnValues)
            {
            	data[i] = d;
            	i++;
            	if(i == 10)
            	{
                	remoteClient.SendDirLandReply(queryID, data);
                    i = 0;
                    data = new DirLandReplyData[10];
            	}
            }
            remoteClient.SendDirLandReply(queryID, data);
        }

        public void DirFindQuery(IClientAPI remoteClient, UUID queryID,
                                 string queryText, uint queryFlags, int queryStart)
        {
            if ((queryFlags & 1) != 0)
            {
                DirPeopleQuery(remoteClient, queryID, queryText, queryFlags,
                               queryStart);
                return;
            }
            else if ((queryFlags & 32) != 0)
            {
                DirEventsQuery(remoteClient, queryID, queryText, queryFlags,
                               queryStart);
                return;
            }
        }

        //Finished!
        public void DirPeopleQuery(IClientAPI remoteClient, UUID queryID,
                                   string queryText, uint queryFlags, int queryStart)
        {
            List<UserAccount> accounts = m_Scenes[0].UserAccountService.GetUserAccounts(m_Scenes[0].RegionInfo.ScopeID, queryText);
			DirPeopleReplyData[] data =
                    new DirPeopleReplyData[accounts.Count];

            int i = 0;
            foreach (UserAccount item in accounts)
            {
                AuroraProfileData UserProfile = ProfileData.GetProfileInfo(item.PrincipalID);
                if (UserProfile.AllowPublish == "1")
                {
                	data[i] = new DirPeopleReplyData();
                    data[i].agentID = item.PrincipalID;
                    data[i].firstName = item.FirstName;
                    data[i].lastName = item.LastName;
                    if(GroupsModule == null)
                    	data[i].group = "";
                    else
                    {
                    	data[i].group = "";
                    	GroupMembershipData[] memberships = GroupsModule.GetMembershipData(item.PrincipalID);
                    	foreach(GroupMembershipData membership in memberships)
                    	{
                    		if(membership.Active)
                    			data[i].group = membership.GroupName;
                    	}
                    }
                    List<string> user = new List<string>();
                    user.Add(item.PrincipalID.ToString());
                    OpenSim.Services.Interfaces.PresenceInfo Pinfo = m_Scenes[0].PresenceService.GetAgents(user.ToArray())[0];
                    data[i].online = Pinfo.Online;
                    data[i].reputation = 0;
                    i++;
                }
            }

            remoteClient.SendDirPeopleReply(queryID, data);
        }

        //FIXME
        public void DirEventsQuery(IClientAPI remoteClient, UUID queryID,
                                   string queryText, uint queryFlags, int queryStart)
        {
            //TODO: Add queryStart!!!
            DirEventsReplyData[] ReturnValues = ProfileData.EventQuery(queryText, queryFlags.ToString(),"Events","EOwnerID,EName,EID,EDate,ETime,EFlags");

            DirEventsReplyData[] data = new DirEventsReplyData[10];
			int i = 0;
			
            foreach (DirEventsReplyData d in ReturnValues)
            {
            	data[i] = d;
            	i++;
            	if(i == 10)
            	{
                	remoteClient.SendDirEventsReply(queryID, data);
                    i = 0;
                    data = new DirEventsReplyData[10];
            	}
            }
            remoteClient.SendDirEventsReply(queryID, data);
        }
		//FIXME
        public void DirClassifiedQuery(IClientAPI remoteClient, UUID queryID,
                                       string queryText, uint queryFlags, uint category,
                                       int queryStart)
        {
        	//TODO: Add queryStart!!!
            DirClassifiedReplyData[] ReplyData = ProfileData.ClassifiedsQuery(queryText, category.ToString(), queryFlags.ToString());
            
        	DirClassifiedReplyData[] data = new DirClassifiedReplyData[10];
			int i = 0;
			
            foreach (DirClassifiedReplyData d in ReplyData)
            {
            	data[i] = d;
            	i++;
            	if(i == 10)
            	{
                	remoteClient.SendDirClassifiedReply(queryID, data);
                    i = 0;
                    data = new DirClassifiedReplyData[10];
            	}
            }
            remoteClient.SendDirClassifiedReply(queryID, data);
        }

        public void EventInfoRequest(IClientAPI remoteClient, uint queryEventID)
        {
            EventData data = new EventData();
            data = ProfileData.GetEventInfo(queryEventID.ToString());
            remoteClient.SendEventInfoReply(data);
        }

        public void ClassifiedInfoRequest(UUID queryClassifiedID, IClientAPI remoteClient)
        {
            List<string> classifiedinfo;
            classifiedinfo = ProfileData.ReadClassifiedInfoRow(queryClassifiedID.ToString());
            Vector3 globalPos = new Vector3();
            try
            {
                Vector3.TryParse(classifiedinfo[10], out globalPos);
            }
            catch (Exception ex)
            {
                ex = new Exception();
                globalPos = new Vector3(128, 128, 128);
            }
            remoteClient.SendClassifiedInfoReply(queryClassifiedID, new UUID(classifiedinfo[0]), Convert.ToUInt32(classifiedinfo[1]), Convert.ToUInt32(classifiedinfo[2]), Convert.ToUInt32(classifiedinfo[3]), classifiedinfo[4], classifiedinfo[5], new UUID(classifiedinfo[6]), Convert.ToUInt32(classifiedinfo[7]), new UUID(classifiedinfo[8]), classifiedinfo[9], globalPos, classifiedinfo[11], Convert.ToByte(classifiedinfo[12]), Convert.ToInt32(classifiedinfo[13]));

        }

        public virtual void HandleMapItemRequest(IClientAPI remoteClient, uint flags,
                                                 uint EstateID, bool godlike, uint itemtype, ulong regionhandle)
        {
            uint xstart = 0;
            uint ystart = 0;
            OpenMetaverse.Utils.LongToUInts(m_scene.RegionInfo.RegionHandle, out xstart, out ystart);
            List<mapItemReply> mapitems = new List<mapItemReply>();
            mapItemReply mapitem = new mapItemReply();
            #region Telehub
            if (itemtype == 1) //(Telehub)
            {
                List<string> Telehubs = ProfileData.Query("select telehubX,telehubY,regionX,regionY from auroraregions");
                int i = 0;
                List<string> TelehubsX = new List<string>();
                List<string> TelehubsY = new List<string>();
                List<string> RegionX = new List<string>();
                List<string> RegionY = new List<string>();
                foreach (string info in Telehubs)
                {
                    if (i == 0)
                    {
                        if(info != "")
                            TelehubsX.Add(info);
                    }
                    if (i == 1)
                    {
                        if (info != "")
                            TelehubsY.Add(info);
                    }
                    if (i == 2)
                    {
                        if (info != "")
                            RegionX.Add(info);
                    }
                    if (i == 3)
                    {
                        if (info != "")
                            RegionY.Add(info);
                        i = -1;
                    }
                    i += 1;
                }
                int tc = Environment.TickCount;
                i = 0;
                if (TelehubsX.Count != 0)
                {
                    for (i = 0; i + 1 <= TelehubsX.Count; i++)
                    {
                        mapitem = new mapItemReply();
                        mapitem.x = (uint)(Convert.ToUInt32(RegionX[i]) + Convert.ToUInt32(TelehubsX[i]));
                        mapitem.y = (uint)(Convert.ToUInt32(RegionY[i]) + Convert.ToUInt32(TelehubsY[i]));
                        mapitem.id = UUID.Zero;
                        mapitem.name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString());
                        mapitem.Extra = 1;
                        mapitem.Extra2 = 0;
                        mapitems.Add(mapitem);
                    }
                }
                remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                mapitems.Clear();
            }
			#endregion
            #region 7
            if (itemtype == 7) //(land sales)
            {
                DirLandReplyData[] Landdata = ProfileData.LandForSaleQuery("4294967295",int.MaxValue.ToString(),"0","forsaleparcels","PID,PName,PForSale,PAuction,PSalePrice,PActualArea");
                    
                uint locX = 0;
                uint locY = 0;
                foreach (DirLandReplyData landDir in Landdata)
                {
                	List<string> ParcelInfo = GenericData.Query("PID",landDir.parcelID.ToString(),"parcels","PLandingX, PLandingY, PRegionID");
                    foreach (Scene scene in m_Scenes)
                    {
                        if (scene.RegionInfo.RegionID == new UUID(ParcelInfo[2]))
                        {
                            locX = scene.RegionInfo.RegionLocX;
                            locY = scene.RegionInfo.RegionLocY;
                        }
                    }
                    mapitem = new mapItemReply();
                    mapitem.x = (uint)(locX + Convert.ToDecimal(ParcelInfo[0]));
                    mapitem.y = (uint)(locY + Convert.ToDecimal(ParcelInfo[1]));
                    mapitem.id = landDir.parcelID;
                    mapitem.name = landDir.name;
                    mapitem.Extra = landDir.actualArea;
                    mapitem.Extra2 = landDir.salePrice;
                    mapitems.Add(mapitem);
                }
                remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                mapitems.Clear();
            }
            #endregion
            //Events
            if (itemtype == 2) //(Events)
            {
                /*int tc = Environment.TickCount;
                Hashtable ReqHash = new Hashtable();
                ReqHash["text"] = "0|0|";
                ReqHash["flags"] = "32";
                ReqHash["query_start"] = "0";

                Hashtable result = GenericXMLRPCRequest(ReqHash,
                                                        "dir_events_query");

                if (!Convert.ToBoolean(result["success"]))
                {
                    remoteClient.SendAgentAlertMessage("", false);
                    return;
                }

                ArrayList dataArray = (ArrayList)result["data"];

                int count = dataArray.Count;
                if (count > 100)
                    count = 101;

                DirEventsReplyData[] Eventdata = new DirEventsReplyData[count];

                int i = 0;

                foreach (Object o in dataArray)
                {
                    Hashtable d = (Hashtable)o;
                    Eventdata[i] = new DirEventsReplyData();
                    Eventdata[i].ownerID = new UUID(d["owner_id"].ToString());
                    Eventdata[i].name = d["name"].ToString();
                    Eventdata[i].eventID = Convert.ToUInt32(d["event_id"]);
                    Eventdata[i].date = d["date"].ToString();
                    Eventdata[i].unixTime = Convert.ToUInt32(d["unix_time"]);
                    Eventdata[i].eventFlags = Convert.ToUInt32(d["event_flags"]);
                    i++;
                    if (i >= count)
                        break;
                }
                /*foreach (DirEventsReplyData eventData in Eventdata)
                {
                    string globalPos = GenericData.GetSQL("select globalPos from events where eventid = '"+eventData.eventID.ToString()+"'",m_gConfig);
                    string[] Position = globalPos.Split(',');
                    mapitem = new mapItemReply();
                    mapitem.x = (uint)(Convert.ToDecimal(Position[0]));
                    mapitem.y = (uint)(Convert.ToDecimal(Position[1]));
                    mapitem.id = eventData.ownerID;
                    mapitem.name = eventData.name;
                    mapitem.Extra = 0;
                    mapitem.Extra2 = 0;
                    mapitems.Add(mapitem);
                }
                remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                mapitems.Clear();*/
            }
        }
        public void OnPlacesQueryRequest(UUID QueryID, UUID TransactionID, string QueryText, uint QueryFlags, byte Category, string SimName, IClientAPI client)
        {
            if (QueryFlags == 64) //Agent Owned
            {
                List<ILandObject> LandQueried = new List<ILandObject>();
                List<string> SimNames = new List<string>();
                List<string> SimXs = new List<string>();
                List<string> SimYs = new List<string>();
                foreach (Scene scene in m_Scenes)
                {
                    List<ILandObject> AllParcels = scene.LandChannel.AllParcels();
                    string simNameTemp = "";
                    string simX = "";
                    string simY = "";
                    foreach (ILandObject LandObject in AllParcels)
                    {
                        if (LandObject.LandData.OwnerID == client.AgentId)
                        {
                            simNameTemp = ProfileData.Query("select regionName from regions where uuid = '" + LandObject.RegionUUID.ToString() + "'")[0];
                            SimNames.Add(simNameTemp);
                            simX = ProfileData.Query("select locX from regions where uuid = '" + LandObject.RegionUUID.ToString() + "'")[0];
                            SimXs.Add(simX);
                            simY = ProfileData.Query("select locX from regions where uuid = '" + LandObject.RegionUUID.ToString() + "'")[0];
                            SimYs.Add(simY);
                            LandQueried.Add(LandObject);
                        }
                    }
                }
                ScenePresence SP;
                Scene AVscene = (Scene)client.Scene;
                AVscene.TryGetScenePresence(client.AgentId, out SP);
                /*LLClientView rcv;
                if (SP.ClientView.TryGet(out rcv))
                {
                    rcv.SendPlacesQuery(SimNames, LandQueried, QueryID, client.AgentId, TransactionID, SimXs, SimYs);
                }*/
            }
        }


        private System.Timers.Timer aTimer = null;
        private void StartSearch()
        {
            aTimer = new System.Timers.Timer(parserTime);
            aTimer.Elapsed += new System.Timers.ElapsedEventHandler(ParseRegions);
            aTimer.Enabled = true;
            
        }

        private void ParseRegions(object source, System.Timers.ElapsedEventArgs e)
        {
            foreach (Scene scene in m_Scenes)
            {
                FireParser(scene.RegionInfo.RegionName);
            }
        }

        #region XML Info Classes

        private class RegionXMLInfo
        {
            public string UUID;
            public string Name;
            public string Handle;
            public string URL;
            public string UserName;
            public string UserUUID;
        }

        private class ObjectXMLInfo
        {
            public string UUID;
            public string RegionUUID;
            public string ParcelUUID;
            public string Title;
            public string Desc;
            public string Flags;
        }

        private class ParcelXMLInfo
        {
            public string Name;
            public string UUID;
            public string InfoUUID;
            public string Landing;
            public string Desc;
            public string Area;
            public string Category;
            public string SalePrice;
            public string Dwell;
            public string OwnerUUID;
            public string GroupUUID;
            public string ForSale;
            public string Directory;
            public string Build;
            public string Script;
            public string Public;
        }

        #endregion

        #region Parser

        private void FireParser(string regionName)
        {
            XmlDocument doc = DataSnapShotManager.GetSnapshot(regionName);
            XmlNodeList rootL = doc.GetElementsByTagName("region");
            XmlNode rootNode = rootL[0];
            RegionXMLInfo info = new RegionXMLInfo();
            if (rootNode != null)
            {
                XmlNodeList partL = rootNode.ChildNodes;

                foreach (XmlNode part in partL)
                {
                    switch (part.Name)
                    {
                        case "uuid":
                            info.UUID = part.InnerText;
                            break;
                        case "name":
                            info.Name = part.InnerText;
                            break;
                        case "handle":
                            info.Handle = part.InnerText;
                            break;
                        case "url":
                            info.URL = part.InnerText;
                            break;
                    }
                }
                List<string> query = GenericData.Query("regionuuid", info.UUID.ToString(), "regions", "*");
                if (query.Count != 0)
                {
                    GenericData.Delete("regions", new string[] { "regionuuid" }, new string[] { info.UUID.ToString() });
                    GenericData.Delete("parcels", new string[] { "regionuuid" }, new string[] { info.UUID.ToString() });
                    GenericData.Delete("objects", new string[] { "regionuuid" }, new string[] { info.UUID.ToString() });
                    GenericData.Delete("allparcels", new string[] { "regionUUID" }, new string[] { info.UUID.ToString() });
                    GenericData.Delete("parcelsales", new string[] { "regionUUID" }, new string[] { info.UUID.ToString() });
                }
            }
            XmlNode rootD = doc.GetElementsByTagName("data")[0];
            XmlNode rootE = doc.GetElementsByTagName("estate")[0];
            XmlNodeList rootP = doc.GetElementsByTagName("parcel");
            XmlNodeList rootO = doc.GetElementsByTagName("object");
            foreach (XmlNode part in rootE)
            {
                switch (part.Name)
                {
                    case "uuid":
                        info.UserName = part.InnerText;
                        break;
                    case "name":
                        info.UserUUID = part.InnerText;
                        break;
                }
            }
            //Add region to the DB.
            GenericData.Insert("regions", new string[] { info.Name, info.UUID, info.Handle, info.URL, info.UserName, info.UserName });
            foreach (XmlNode inner in rootP)
            {
                foreach (XmlNode part in inner.ChildNodes)
                {
                    ParcelXMLInfo PInfo = new ParcelXMLInfo();
                    switch (part.Name)
                    {
                        case "area":
                            PInfo.Area = part.InnerText;
                            break;
                        case "build":
                            PInfo.Build = part.InnerText;
                            break;
                        case "category":
                            PInfo.Category = part.InnerText;
                            break;
                        case "description":
                            PInfo.Desc = part.InnerText;
                            break;
                        case "showinsearch":
                            PInfo.Directory = part.InnerText;
                            break;
                        case "dwell":
                            PInfo.Dwell = part.InnerText;
                            break;
                        case "forsale":
                            PInfo.ForSale = part.InnerText;
                            break;
                        case "groupuuid":
                            PInfo.GroupUUID = part.ChildNodes[0].InnerText;
                            break;
                        case "infouuid":
                            PInfo.InfoUUID = part.InnerText;
                            break;
                        case "location":
                            PInfo.Landing = part.InnerText;
                            break;
                        case "name":
                            PInfo.Name = part.InnerText;
                            break;
                        case "owner":
                            PInfo.OwnerUUID = part.ChildNodes.Item(0).InnerText;
                            break;
                        case "public":
                            PInfo.Public = part.InnerText;
                            break;
                        case "salesprice":
                            PInfo.SalePrice = part.InnerText;
                            break;
                        case "scripts":
                            PInfo.Script = part.InnerText;
                            break;
                        case "uuid":
                            PInfo.UUID = part.InnerText;
                            break;
                    }
                    GenericData.Insert("allparcels", new string[] { info.UUID, PInfo.Name, PInfo.OwnerUUID, PInfo.GroupUUID, PInfo.Landing, PInfo.UUID, PInfo.InfoUUID, PInfo.Area });
                    if (Convert.ToBoolean(PInfo.Directory))
                        GenericData.Insert("parcels", new string[] { info.UUID, PInfo.Name, PInfo.UUID, PInfo.Landing, PInfo.UUID, PInfo.Desc, PInfo.Category, PInfo.Build, PInfo.Script, PInfo.Public, PInfo.Dwell, PInfo.InfoUUID });
                    //TODO: CHECK THESE 0 AND FALSE VALUES!
                    if (Convert.ToBoolean(PInfo.ForSale))
                        GenericData.Insert("parcelsales", new string[] { info.UUID, PInfo.Name, PInfo.UUID, PInfo.Area, PInfo.SalePrice, PInfo.Landing, PInfo.InfoUUID, PInfo.Dwell, "0", "false" });
                }
            }

            //Now for objects!
            foreach (XmlNode inner in rootO)
            {
                foreach (XmlNode part in inner.ChildNodes)
                {
                    ObjectXMLInfo OInfo = new ObjectXMLInfo();
                    switch (part.Name)
                    {
                        case "uuid":
                            OInfo.UUID = part.InnerText;
                            break;
                        case "regionuuid":
                            OInfo.RegionUUID = part.InnerText;
                            break;
                        case "parceluuid":
                            OInfo.ParcelUUID = part.InnerText;
                            break;
                        case "title":
                            OInfo.Title = part.InnerText;
                            break;
                        case "description":
                            OInfo.Desc = part.InnerText;
                            break;
                        case "flags":
                            OInfo.Flags = part.InnerText;
                            break;
                    }
                    GenericData.Insert("objects", new string[] { OInfo.UUID, OInfo.ParcelUUID, OInfo.Title, OInfo.Desc, OInfo.RegionUUID });
                }
            }
        }
        #endregion

        #endregion
    }
}
