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
    public class AuroraSearchModule : ISharedRegionModule
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;
        private IConfigSource m_config;
        private IProfileConnector ProfileFrontend = null;
        private IGenericData GenericData = null;
        private IConfigSource m_gConfig;
        private List<Scene> m_Scenes = new List<Scene>();
        private bool m_SearchEnabled = true;
        protected IFriendsService m_FriendsService = null;
        protected IGroupsModule GroupsModule = null;
        protected IDirectoryServiceConnector DSC = null;
        private System.Timers.Timer aTimer = null;
        protected double parserTime = 3600000;
        private IDataSnapshot DataSnapShotManager;

        #endregion

        #region IRegionModule Members

        public void Initialise(IConfigSource config)
        {
            m_config = config;
            m_gConfig = config;
            IConfig searchConfig = config.Configs["Search"];
            if (searchConfig == null)
            {
                m_log.Info("[AuroraSearch] Not configured, disabling");
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
            parserTime = searchConfig.GetDouble("ParserTime", 3600000);
            if (m_FriendsService == null)
            {
                m_log.Error("[AuroraSearch]: No Connector defined in section Friends, or filed to load, cannot continue");
                m_SearchEnabled = false;
            }
            else if (searchConfig.GetString("SearchModule", Name) != Name)
            {
                m_SearchEnabled = false;
            }
        }

        public void AddRegion(Scene scene)
        {
            ProfileFrontend = DataManager.DataManager.IProfileConnector;

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
            GenericData = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
            DSC = Aurora.DataManager.DataManager.IDirectoryServiceConnector;
            GroupsModule = m_scene.RequestModuleInterface<IGroupsModule>();
            DataSnapShotManager = m_scene.RequestModuleInterface<IDataSnapshot>();
            if (m_SearchEnabled && DataSnapShotManager != null)
                StartSearch();
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
            get { return "AuroraSearchModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        #endregion

        #region Client

        public void NewClient(IClientAPI client)
        {
            if (m_SearchEnabled)
            {
                // Subscribe to messages
                client.OnDirPlacesQuery += DirPlacesQuery;
                client.OnDirFindQuery += DirFindQuery;
                client.OnDirPopularQuery += DirPopularQuery;
                client.OnDirLandQuery += DirLandQuery;
                client.OnDirClassifiedQuery += DirClassifiedQuery;
                // Response after Directory Queries
                client.OnEventInfoRequest += EventInfoRequest;
                client.OnMapItemRequest += HandleMapItemRequest;
                client.OnPlacesQuery += OnPlacesQueryRequest;
            }
        }

        public void RemoveClient(IClientAPI client)
        {
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

        #region Search Module

        protected void DirPlacesQuery(IClientAPI remoteClient, UUID queryID,
                                      string queryText, int queryFlags, int category, string simName,
                                      int queryStart)
        {
            DirPlacesReplyData[] ReturnValues = DSC.FindLand(queryText, category.ToString(), queryStart);

            DirPlacesReplyData[] data = new DirPlacesReplyData[10];

            int i = 0;
            foreach (DirPlacesReplyData d in ReturnValues)
            {
                data[i] = d;
                i++;
                if (i == 10)
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

        public void DirLandQuery(IClientAPI remoteClient, UUID queryID,
                                 uint queryFlags, uint searchType, int price, int area,
                                 int queryStart)
        {
            DirLandReplyData[] ReturnValues = DSC.FindLandForSale(searchType.ToString(), price.ToString(), area.ToString(), queryStart);

            DirLandReplyData[] data = new DirLandReplyData[10];

            int i = 0;
            foreach (DirLandReplyData d in ReturnValues)
            {
                data[i] = d;
                i++;
                if (i == 10)
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

        public void DirPeopleQuery(IClientAPI remoteClient, UUID queryID,
                                   string queryText, uint queryFlags, int queryStart)
        {
            List<UserAccount> accounts = m_Scenes[0].UserAccountService.GetUserAccounts(m_Scenes[0].RegionInfo.ScopeID, queryText);
            DirPeopleReplyData[] data =
                    new DirPeopleReplyData[accounts.Count];

            int i = 0;
            foreach (UserAccount item in accounts)
            {
                IUserProfileInfo UserProfile = ProfileFrontend.GetUserProfile(item.PrincipalID);
                if (UserProfile == null)
                {
                    data[i] = new DirPeopleReplyData();
                    data[i].agentID = item.PrincipalID;
                    data[i].firstName = item.FirstName;
                    data[i].lastName = item.LastName;
                    if (GroupsModule == null)
                        data[i].group = "";
                    else
                    {
                        data[i].group = "";
                        GroupMembershipData[] memberships = GroupsModule.GetMembershipData(item.PrincipalID);
                        foreach (GroupMembershipData membership in memberships)
                        {
                            if (membership.Active)
                                data[i].group = membership.GroupName;
                        }
                    }
                    OpenSim.Services.Interfaces.GridUserInfo Pinfo = m_scene.GridUserService.GetGridUserInfo(item.PrincipalID.ToString());
                    if (Pinfo != null)
                        data[i].online = true;
                    data[i].reputation = 0;
                    i++;
                    continue;
                }
                if (UserProfile.AllowPublish)
                {
                    data[i] = new DirPeopleReplyData();
                    data[i].agentID = item.PrincipalID;
                    data[i].firstName = item.FirstName;
                    data[i].lastName = item.LastName;
                    if (GroupsModule == null)
                        data[i].group = "";
                    else
                    {
                        data[i].group = "";
                        GroupMembershipData[] memberships = GroupsModule.GetMembershipData(item.PrincipalID);
                        foreach (GroupMembershipData membership in memberships)
                        {
                            if (membership.Active)
                                data[i].group = membership.GroupName;
                        }
                    }
                    OpenSim.Services.Interfaces.GridUserInfo Pinfo = m_scene.GridUserService.GetGridUserInfo(item.PrincipalID.ToString());
                    data[i].online = Pinfo.Online;
                    data[i].reputation = 0;
                    i++;
                }
            }

            remoteClient.SendDirPeopleReply(queryID, data);
        }

        public void DirEventsQuery(IClientAPI remoteClient, UUID queryID,
                                   string queryText, uint queryFlags, int queryStart)
        {
            DirEventsReplyData[] ReturnValues = DSC.FindEvents(queryText, queryFlags.ToString(), queryStart);

            DirEventsReplyData[] data = new DirEventsReplyData[10];
            int i = 0;

            foreach (DirEventsReplyData d in ReturnValues)
            {
                data[i] = d;
                i++;
                if (i == 10)
                {
                    remoteClient.SendDirEventsReply(queryID, data);
                    i = 0;
                    data = new DirEventsReplyData[10];
                }
            }
            remoteClient.SendDirEventsReply(queryID, data);
        }

        public void DirClassifiedQuery(IClientAPI remoteClient, UUID queryID,
                                       string queryText, uint queryFlags, uint category,
                                       int queryStart)
        {
            DirClassifiedReplyData[] ReplyData = DSC.FindClassifieds(queryText, category.ToString(), queryFlags.ToString(), queryStart);

            DirClassifiedReplyData[] data = new DirClassifiedReplyData[10];
            int i = 0;

            foreach (DirClassifiedReplyData d in ReplyData)
            {
                data[i] = d;
                i++;
                if (i == 10)
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
            EventData data = DSC.GetEventInfo(queryEventID.ToString());
            remoteClient.SendEventInfoReply(data);
        }

        public virtual void HandleMapItemRequest(IClientAPI remoteClient, uint flags,
                                                 uint EstateID, bool godlike, uint itemtype, ulong regionhandle)
        {
            //All the parts are in for this, except for popular places and those are not in as they are not reqested anymore.

            List<mapItemReply> mapitems = new List<mapItemReply>();
            mapItemReply mapitem = new mapItemReply();
            return;
            uint xstart = 0;
            uint ystart = 0;
            OpenMetaverse.Utils.LongToUInts(m_scene.RegionInfo.RegionHandle, out xstart, out ystart);
            OpenSim.Services.Interfaces.GridRegion GR = m_scene.GridService.GetRegionByPosition(UUID.Zero, (int)xstart, (int)ystart);

            #region Telehub
            if (itemtype == (uint)OpenMetaverse.GridItemType.Telehub)
            {
                IRegionConnector GF = DataManager.DataManager.IRegionConnector;
                int tc = Environment.TickCount;
                Telehub telehub = GF.FindTelehub(GR.RegionID);
                if (telehub != null)
                {
                    mapitem = new mapItemReply();
                    mapitem.x = (uint)(GR.RegionLocX + telehub.TelehubX);
                    mapitem.y = (uint)(GR.RegionLocY + telehub.TelehubY);
                    mapitem.id = GR.RegionID;
                    mapitem.name = Util.Md5Hash(GR.RegionName + tc.ToString());
                    mapitem.Extra = 1;
                    mapitem.Extra2 = 0;
                    mapitems.Add(mapitem);
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                    mapitems.Clear();
                }
            }

            #endregion

            #region Land for sale

            if (itemtype == (uint)OpenMetaverse.GridItemType.LandForSale)
            {
                DirLandReplyData[] Landdata = DSC.FindLandForSale("4294967295", int.MaxValue.ToString(), "0", 0);
                
                uint locX = 0;
                uint locY = 0;
                foreach (DirLandReplyData landDir in Landdata)
                {
                    AuroraLandData landdata = DSC.GetLandData(landDir.parcelID);
                    if (landdata.Maturity != 0)
                    {
                        continue;
                    }
                    foreach (Scene scene in m_Scenes)
                    {
                        if (scene.RegionInfo.RegionID == landdata.RegionID)
                        {
                            locX = scene.RegionInfo.RegionLocX;
                            locY = scene.RegionInfo.RegionLocY;
                        }
                    }
                    if (locY == 0 && locX == 0)
                        continue;
                    mapitem = new mapItemReply();
                    mapitem.x = (uint)(locX + landdata.LandingX);
                    mapitem.y = (uint)(locY + landdata.LandingY);
                    mapitem.id = landDir.parcelID;
                    mapitem.name = landDir.name;
                    mapitem.Extra = landDir.actualArea;
                    mapitem.Extra2 = landDir.salePrice;
                    mapitems.Add(mapitem);
                }
                if (mapitems.Count != 0)
                {
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                    mapitems.Clear();
                }
            }

            if (itemtype == (uint)OpenMetaverse.GridItemType.AdultLandForSale)
            {
                DirLandReplyData[] Landdata = DSC.FindLandForSale("4294967295", int.MaxValue.ToString(), "0", 0);
                
                uint locX = 0;
                uint locY = 0;
                foreach (DirLandReplyData landDir in Landdata)
                {
                    AuroraLandData landdata = DSC.GetLandData(landDir.parcelID);
                    if (landdata.Maturity == 0)
                    {
                        continue;
                    }
                    foreach (Scene scene in m_Scenes)
                    {
                        if (scene.RegionInfo.RegionID == landdata.RegionID)
                        {
                            locX = scene.RegionInfo.RegionLocX;
                            locY = scene.RegionInfo.RegionLocY;
                        }
                    }
                    if (locY == 0 && locX == 0)
                        continue;
                    mapitem = new mapItemReply();
                    mapitem.x = (uint)(locX + landdata.LandingX);
                    mapitem.y = (uint)(locY + landdata.LandingY);
                    mapitem.id = landDir.parcelID;
                    mapitem.name = landDir.name;
                    mapitem.Extra = landDir.actualArea;
                    mapitem.Extra2 = landDir.salePrice;
                    mapitems.Add(mapitem);
                }
                if (mapitems.Count != 0)
                {
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                    mapitems.Clear();
                }
            }

            #endregion

            #region Events

            if (itemtype == (uint)OpenMetaverse.GridItemType.PgEvent)
            {
                DirEventsReplyData[] Eventdata = DSC.FindAllEventsInRegion(GR.RegionName);
                foreach (DirEventsReplyData eventData in Eventdata)
                {
                    EventData eventdata = DSC.GetEventInfo(eventData.eventID.ToString());

                    string RegionName = eventdata.simName;
                    Vector3 globalPos = eventdata.globalPos;
                    int Mature = eventdata.maturity;
                    if (Mature != 0)
                        continue;
                    OpenSim.Services.Interfaces.GridRegion region = m_scene.GridService.GetRegionByName(UUID.Zero, RegionName);
                    mapitem = new mapItemReply();
                    mapitem.x = (uint)(region.RegionLocX + globalPos.X);
                    mapitem.y = (uint)(region.RegionLocY + globalPos.Y);
                    mapitem.id = eventData.ownerID;
                    mapitem.name = eventData.name;
                    mapitem.Extra2 = (int)DirectoryManager.EventFlags.PG;
                    mapitems.Add(mapitem);
                }
                if (mapitems.Count != 0)
                {
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                    mapitems.Clear();
                }
            }

            if (itemtype == (uint)OpenMetaverse.GridItemType.AdultEvent)
            {
                DirEventsReplyData[] Eventdata = DSC.FindAllEventsInRegion(GR.RegionName);
                foreach (DirEventsReplyData eventData in Eventdata)
                {
                    EventData eventdata = DSC.GetEventInfo(eventData.eventID.ToString());

                    string RegionName = eventdata.simName;
                    Vector3 globalPos = eventdata.globalPos;
                    int Mature = eventdata.maturity;
                    if (Mature != 2)
                        continue;
                    OpenSim.Services.Interfaces.GridRegion region = m_scene.GridService.GetRegionByName(UUID.Zero, RegionName);
                    mapitem = new mapItemReply();
                    mapitem.x = (uint)(region.RegionLocX + globalPos.X);
                    mapitem.y = (uint)(region.RegionLocY + globalPos.Y);
                    mapitem.id = eventData.ownerID;
                    mapitem.name = eventData.name;
                    mapitem.Extra2 = (int)DirectoryManager.EventFlags.Adult;
                    mapitems.Add(mapitem);
                }
                if (mapitems.Count != 0)
                {
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                    mapitems.Clear();
                }
            }
            if (itemtype == (uint)OpenMetaverse.GridItemType.MatureEvent)
            {
                DirEventsReplyData[] Eventdata = DSC.FindAllEventsInRegion(GR.RegionName);
                foreach (DirEventsReplyData eventData in Eventdata)
                {
                    EventData eventdata = DSC.GetEventInfo(eventData.eventID.ToString());

                    string RegionName = eventdata.simName;
                    Vector3 globalPos = eventdata.globalPos;
                    int Mature = eventdata.maturity;
                    if (Mature != 1)
                        continue;
                    OpenSim.Services.Interfaces.GridRegion region = m_scene.GridService.GetRegionByName(UUID.Zero, RegionName);
                    mapitem = new mapItemReply();
                    mapitem.x = (uint)(region.RegionLocX + globalPos.X);
                    mapitem.y = (uint)(region.RegionLocY + globalPos.Y);
                    mapitem.id = eventData.ownerID;
                    mapitem.name = eventData.name;
                    mapitem.Extra2 = (int)DirectoryManager.EventFlags.Mature;
                    mapitems.Add(mapitem);
                }
                if (mapitems.Count != 0)
                {
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                    mapitems.Clear();
                }
            }

            #endregion

            #region Classified

            if (itemtype == (uint)OpenMetaverse.GridItemType.Classified)
            {
                Classified[] Classifieds = DSC.GetClassifiedsInRegion(GR.RegionName);
                foreach (Classified classified in Classifieds)
                {
                    Vector3 Position = new Vector3();
                    Vector3.TryParse(classified.PosGlobal, out Position);
                    OpenSim.Services.Interfaces.GridRegion region = m_scene.GridService.GetRegionByName(UUID.Zero, classified.SimName);
                    mapitem = new mapItemReply();
                    mapitem.x = (uint)(region.RegionLocX + Position.X);
                    mapitem.y = (uint)(region.RegionLocY + Position.Y);
                    mapitem.id = new UUID(classified.CreatorUUID);
                    mapitem.name = classified.Name;
                    mapitem.Extra = 0;
                    mapitem.Extra2 = 0;
                    mapitems.Add(mapitem);
                }
                if (mapitems.Count != 0)
                {
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                    mapitems.Clear();
                }
            }

            #endregion
        }

        public void OnPlacesQueryRequest(UUID QueryID, UUID TransactionID, string QueryText, uint QueryFlags, byte Category, string SimName, IClientAPI client)
        {
            if (QueryFlags == 64) //Agent Owned
            {
                List<ILandObject> LandQueried = new List<ILandObject>();
                List<string> SimNames = new List<string>();
                List<string> SimXs = new List<string>();
                List<string> SimYs = new List<string>();
                List<OpenSim.Framework.RegionInfo> RegionInfos = new List<OpenSim.Framework.RegionInfo>();
                List<object> Parcels = null;
                foreach (Scene scene in m_Scenes)
                {
                    List<ILandObject> AllParcels = scene.LandChannel.AllParcels();
                    foreach (ILandObject LandObject in AllParcels)
                    {
                        if (LandObject.LandData.OwnerID == client.AgentId)
                        {
                            SimNames.Add(scene.RegionInfo.RegionName);
                            if (LandObject.LandData.UserLocation == Vector3.Zero)
                            {
                                for (int x = 0; x < 64; x++)
                                {
                                    for (int y = 0; y < 64; y++)
                                    {
                                        if (LandObject.LandBitmap[x, y])
                                        {
                                            SimXs.Add(((x * 4) + (scene.RegionInfo.RegionLocX * 256)).ToString());
                                            SimYs.Add(((y * 4) + (scene.RegionInfo.RegionLocY * 256)).ToString());
                                            RegionInfos.Add(scene.RegionInfo);
                                            x = (int)Constants.RegionSize;
                                            y = (int)Constants.RegionSize;
                                            continue;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                SimXs.Add(((LandObject.LandData.UserLocation.X) + (scene.RegionInfo.RegionLocX * 256)).ToString());
                                SimYs.Add(((LandObject.LandData.UserLocation.Y) + (scene.RegionInfo.RegionLocY * 256)).ToString());
                            }
                            LandQueried.Add(LandObject);
                        }
                    }
                }
                Parcels = new List<object>(LandQueried.ToArray());
                client.SendPlacesQuery(SimNames, Parcels, QueryID, client.AgentId, TransactionID, SimXs, SimYs, RegionInfos.ToArray());
            }
        }

        private void StartSearch()
        {
            aTimer = new System.Timers.Timer(parserTime);
            aTimer.Elapsed += new System.Timers.ElapsedEventHandler(ParseRegions);
            aTimer.Enabled = true;
            aTimer.Start();
            foreach (Scene scene in m_Scenes)
            {
                FireParser(scene, scene.RegionInfo.RegionName);
            }
        }

        private void ParseRegions(object source, System.Timers.ElapsedEventArgs e)
        {
            foreach (Scene scene in m_Scenes)
            {
                FireParser(scene, scene.RegionInfo.RegionName);
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

        private void FireParser(Scene currentScene, string regionName)
        {
            m_log.Info("[SearchModule]: Starting Search for region " + regionName + ".");
            XmlDocument doc = DataSnapShotManager.GetSnapshot(regionName);
            if (doc == null)
            {
                m_log.Error("[SearchModule]: Null ref in the XMLDOC.");
                return;
            }
            XmlNodeList rootL = doc.GetElementsByTagName("region");
            RegionXMLInfo info = new RegionXMLInfo();
            foreach (XmlNode rootNode in rootL)
            {
                foreach (XmlNode subRootNode in rootNode.ChildNodes)
                {
                    if (subRootNode.Name == "info")
                    {
                        foreach (XmlNode part in subRootNode.ChildNodes)
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

                        List<string> query = GenericData.Query("RID", info.UUID.ToString(), "searchregions", "*");
                        if (query.Count != 0)
                        {
                            if (query[0] != "")
                            {
                                GenericData.Delete("searchregions", new string[] { "RID" }, new string[] { info.UUID.ToString() });
                                GenericData.Delete("searchparcels", new string[] { "RID" }, new string[] { info.UUID.ToString() });
                                GenericData.Delete("searchobjects", new string[] { "RID" }, new string[] { info.UUID.ToString() });
                                GenericData.Delete("searchallparcels", new string[] { "RID" }, new string[] { info.UUID.ToString() });
                                GenericData.Delete("searchparcelsales", new string[] { "RID" }, new string[] { info.UUID.ToString() });
                            }
                        }
                    }
                }
            }


            foreach (XmlNode rootNode in rootL)
            {
                foreach (XmlNode subRootNode in rootNode.ChildNodes)
                {
                    if (subRootNode.Name == "data")
                    {
                        foreach (XmlNode part in subRootNode.ChildNodes)
                        {
                            if (part.Name == "estate")
                            {
                                foreach (XmlNode subpart in part.ChildNodes)
                                {
                                    foreach (XmlNode subsubpart in subpart.ChildNodes)
                                    {
                                        switch (subsubpart.Name)
                                        {
                                            case "uuid":
                                                info.UserUUID = subsubpart.InnerText;
                                                break;
                                            case "name":
                                                info.UserName = subsubpart.InnerText;
                                                break;
                                        }
                                    }
                                }
                                GenericData.Insert("searchregions", new string[] { info.Name, info.UUID, info.Handle, info.URL, info.UserName, info.UserName });
                            }
                            if (part.Name == "objectdata")
                            {
                                foreach (XmlNode subsubpart in part.ChildNodes)
                                {
                                    ObjectXMLInfo OInfo = new ObjectXMLInfo();
                                    foreach (XmlNode subpart in subsubpart.ChildNodes)
                                    {
                                        switch (subpart.Name)
                                        {
                                            case "uuid":
                                                OInfo.UUID = subpart.InnerText;
                                                break;
                                            case "regionuuid":
                                                OInfo.RegionUUID = subpart.InnerText;
                                                break;
                                            case "parceluuid":
                                                OInfo.ParcelUUID = subpart.InnerText;
                                                break;
                                            case "title":
                                                OInfo.Title = subpart.InnerText;
                                                break;
                                            case "description":
                                                OInfo.Desc = subpart.InnerText;
                                                break;
                                            case "flags":
                                                OInfo.Flags = subpart.InnerText;
                                                break;
                                        }
                                    }
                                    if (OInfo.UUID != null)
                                        GenericData.Insert("searchobjects", new string[] { OInfo.UUID, OInfo.ParcelUUID, OInfo.Title, OInfo.Desc, OInfo.RegionUUID });
                                }
                            }
                            if (part.Name == "parceldata")
                            {
                                foreach (XmlNode pppart in part.ChildNodes)
                                {
                                    ParcelXMLInfo PInfo = new ParcelXMLInfo();
                                    if (pppart.Attributes != null)
                                    {
                                        foreach (XmlNode att in pppart.Attributes)
                                        {
                                            switch (att.Name)
                                            {
                                                case "build":
                                                    PInfo.Build = att.InnerText;
                                                    break;
                                                case "category":
                                                    PInfo.Category = att.InnerText;
                                                    break;
                                                case "showinsearch":
                                                    PInfo.Directory = att.InnerText;
                                                    break;
                                                case "forsale":
                                                    PInfo.ForSale = att.InnerText;
                                                    break;
                                                case "public":
                                                    PInfo.Public = att.InnerText;
                                                    break;
                                                case "salesprice":
                                                    PInfo.SalePrice = att.InnerText;
                                                    break;
                                                case "scripts":
                                                    PInfo.Script = att.InnerText;
                                                    break;
                                            }
                                        }
                                    }
                                    foreach (XmlNode ppart in pppart.ChildNodes)
                                    {
                                        switch (ppart.Name)
                                        {
                                            case "area":
                                                PInfo.Area = ppart.InnerText;
                                                break;
                                            case "description":
                                                PInfo.Desc = ppart.InnerText;
                                                break;
                                            case "dwell":
                                                PInfo.Dwell = ppart.InnerText;
                                                break;
                                            case "groupuuid":
                                                PInfo.GroupUUID = ppart.ChildNodes[0].InnerText;
                                                break;
                                            case "infouuid":
                                                PInfo.InfoUUID = ppart.InnerText;
                                                break;
                                            case "location":
                                                PInfo.Landing = ppart.InnerText;
                                                break;
                                            case "name":
                                                PInfo.Name = ppart.InnerText;
                                                break;
                                            case "owner":
                                                PInfo.OwnerUUID = ppart.ChildNodes.Item(0).InnerText;
                                                break;
                                            case "uuid":
                                                PInfo.UUID = ppart.InnerText;
                                                break;
                                        }
                                    }
                                    if (PInfo.UUID == null)
                                        continue;
                                    if (PInfo.GroupUUID == null)
                                        PInfo.GroupUUID = UUID.Zero.ToString();
                                    GenericData.Insert("searchallparcels", new string[] { info.UUID, PInfo.Name, PInfo.OwnerUUID, PInfo.GroupUUID, PInfo.Landing, PInfo.UUID, PInfo.InfoUUID, PInfo.Area });
                                    if (Convert.ToBoolean(PInfo.Directory))
                                        GenericData.Insert("searchparcels", new string[] { info.UUID, PInfo.Name, PInfo.UUID, PInfo.Landing, PInfo.Desc, PInfo.Category, PInfo.Build, PInfo.Script, PInfo.Public, PInfo.Dwell, PInfo.InfoUUID, false.ToString(), false.ToString() });
                                    SimMap map = DataManager.DataManager.ISimMapConnector.GetSimMap(currentScene.RegionInfo.RegionID);
                                    if (Convert.ToBoolean(PInfo.ForSale))
                                    {
                                        LandData LD = m_scene.LandChannel.GetLandObject(int.Parse(PInfo.Landing.Split(',')[0]),int.Parse(PInfo.Landing.Split(',')[1])).LandData;
                                        
                                        GenericData.Insert("searchparcelsales", new string[] { info.UUID, PInfo.Name, PInfo.UUID, PInfo.Area, PInfo.SalePrice, PInfo.Landing, PInfo.InfoUUID, PInfo.Dwell, currentScene.RegionInfo.EstateSettings.EstateID.ToString(), ((map.SimFlags & SimMapFlags.Hidden) == SimMapFlags.Hidden).ToString() });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #endregion
    }
}
