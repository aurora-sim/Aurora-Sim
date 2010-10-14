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
        private List<Scene> m_Scenes = new List<Scene>();
        private bool m_SearchEnabled = false;
        private IGroupsModule GroupsModule = null;
        private IDirectoryServiceConnector DSC = null;

        #endregion

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
            m_config = config;
            IConfig searchConfig = config.Configs["Search"];
            if (searchConfig != null)
                if (searchConfig.GetString("SearchModule", Name) == Name)
                    m_SearchEnabled = true;
        }

        public void AddRegion(Scene scene)
        {
            if (!m_SearchEnabled)
                return;

            if (!m_Scenes.Contains(scene))
                m_Scenes.Add(scene);
            m_scene = scene;
            m_scene.EventManager.OnNewClient += NewClient;
            m_scene.EventManager.OnClientClosed += RemoveClient;
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_SearchEnabled)
                return;

            if (m_Scenes.Contains(scene))
                m_Scenes.Remove(scene);
            m_scene.EventManager.OnNewClient -= NewClient;
            m_scene.EventManager.OnClientClosed -= RemoveClient;
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_SearchEnabled)
                return;

            ProfileFrontend = DataManager.DataManager.RequestPlugin<IProfileConnector>();
            DSC = Aurora.DataManager.DataManager.RequestPlugin<IDirectoryServiceConnector>();
            GroupsModule = m_scene.RequestModuleInterface<IGroupsModule>();
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

        public void RemoveClient(UUID clientID, Scene scene)
        {
            IClientAPI client = scene.GetScenePresence(clientID).ControllingClient;
            client.OnDirPlacesQuery -= DirPlacesQuery;
            client.OnDirFindQuery -= DirFindQuery;
            client.OnDirPopularQuery -= DirPopularQuery;
            client.OnDirLandQuery -= DirLandQuery;
            client.OnDirClassifiedQuery -= DirClassifiedQuery;
            // Response after Directory Queries
            client.OnEventInfoRequest -= EventInfoRequest;
            client.OnMapItemRequest -= HandleMapItemRequest;
            client.OnPlacesQuery -= OnPlacesQueryRequest;
        }

        #endregion

        #region Search Module

        /// <summary>
        /// Parcel request
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="queryID"></param>
        /// <param name="queryText"></param>
        /// <param name="queryFlags"></param>
        /// <param name="category"></param>
        /// <param name="simName"></param>
        /// <param name="queryStart"></param>
        protected void DirPlacesQuery(IClientAPI remoteClient, UUID queryID,
                                      string queryText, int queryFlags, int category, string simName,
                                      int queryStart)
        {
            DirPlacesReplyData[] ReturnValues = DSC.FindLand(queryText, category.ToString(), queryStart, (uint)queryFlags);
            if (ReturnValues.Length > 10)
            {
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
                        if (ReturnValues.Length - i < 10)
                        {
                            data = new DirPlacesReplyData[ReturnValues.Length - i];
                        }
                        else
                        {
                            data = new DirPlacesReplyData[10];
                        }
                    }
                }
                remoteClient.SendDirPlacesReply(queryID, data);
            }
            else
            {
                remoteClient.SendDirPlacesReply(queryID, ReturnValues);
            }
        }

        public void DirPopularQuery(IClientAPI remoteClient, UUID queryID, uint queryFlags)
        {
            /// <summary>
            /// Deprecated.
            /// </summary>
        }

        /// <summary>
        /// Land for sale request
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="queryID"></param>
        /// <param name="queryFlags"></param>
        /// <param name="searchType"></param>
        /// <param name="price"></param>
        /// <param name="area"></param>
        /// <param name="queryStart"></param>
        public void DirLandQuery(IClientAPI remoteClient, UUID queryID,
                                 uint queryFlags, uint searchType, int price, int area,
                                 int queryStart)
        {
            DirLandReplyData[] ReturnValues = DSC.FindLandForSale(searchType.ToString(), price.ToString(), area.ToString(), queryStart, queryFlags);
            if (ReturnValues.Length > 10)
            {
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
                        if (ReturnValues.Length - i < 10)
                        {
                            data = new DirLandReplyData[ReturnValues.Length - i];
                        }
                        else
                        {
                            data = new DirLandReplyData[10];
                        }
                    }
                }
                remoteClient.SendDirLandReply(queryID, data);
            }
            else
                remoteClient.SendDirLandReply(queryID, ReturnValues);
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

        //TODO: Flagged to optimize
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
                }
                else if (UserProfile.AllowPublish)
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
                }
            }
            if (data.Length > 10)
            {
                DirPeopleReplyData[] retvals = new DirPeopleReplyData[10];
                
                i = 0;
                foreach (DirPeopleReplyData d in data)
                {
                    retvals[i] = d;
                    i++;
                    if (i == 10)
                    {
                        remoteClient.SendDirPeopleReply(queryID, retvals);
                        i = 0;
                        if (data.Length - i < 10)
                        {
                            retvals = new DirPeopleReplyData[data.Length - i];
                        }
                        else
                        {
                            retvals = new DirPeopleReplyData[10];
                        }
                    }
                }
                remoteClient.SendDirPeopleReply(queryID, retvals);
            }
            else
                remoteClient.SendDirPeopleReply(queryID, data);
            
        }

        public void DirEventsQuery(IClientAPI remoteClient, UUID queryID,
                                   string queryText, uint queryFlags, int queryStart)
        {
            DirEventsReplyData[] ReturnValues = DSC.FindEvents(queryText, queryFlags.ToString(), queryStart);

            if (ReturnValues.Length > 10)
            {
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
                        if (data.Length - i < 10)
                        {
                            data = new DirEventsReplyData[data.Length - i];
                        }
                        else
                        {
                            data = new DirEventsReplyData[10];
                        }
                    }
                }
                remoteClient.SendDirEventsReply(queryID, data);
            }
            else
                remoteClient.SendDirEventsReply(queryID, ReturnValues);
        }

        public void DirClassifiedQuery(IClientAPI remoteClient, UUID queryID,
                                       string queryText, uint queryFlags, uint category,
                                       int queryStart)
        {
            DirClassifiedReplyData[] ReturnValues = DSC.FindClassifieds(queryText, category.ToString(), queryFlags.ToString(), queryStart);
            if (ReturnValues.Length > 10)
            {
                DirClassifiedReplyData[] data = new DirClassifiedReplyData[10];
                int i = 0;

                foreach (DirClassifiedReplyData d in ReturnValues)
                {
                    data[i] = d;
                    i++;
                    if (i == 10)
                    {
                        remoteClient.SendDirClassifiedReply(queryID, data);
                        i = 0;
                        if (data.Length - i < 10)
                        {
                            data = new DirClassifiedReplyData[data.Length - i];
                        }
                        else
                        {
                            data = new DirClassifiedReplyData[10];
                        }
                    }
                }
                remoteClient.SendDirClassifiedReply(queryID, data);
            }
            else
                remoteClient.SendDirClassifiedReply(queryID, ReturnValues);
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
            uint xstart = 0;
            uint ystart = 0;
            OpenMetaverse.Utils.LongToUInts(m_scene.RegionInfo.RegionHandle, out xstart, out ystart);
            OpenSim.Services.Interfaces.GridRegion GR = m_scene.GridService.GetRegionByPosition(UUID.Zero, (int)xstart, (int)ystart);

            #region Telehub
            if (itemtype == (uint)OpenMetaverse.GridItemType.Telehub)
            {
                IRegionConnector GF = DataManager.DataManager.RequestPlugin<IRegionConnector>();
                if (GF == null)
                    return;
                int tc = Environment.TickCount;
                Telehub telehub = GF.FindTelehub(GR.RegionID);
                if (telehub != null)
                {
                    mapitem = new mapItemReply();
                    mapitem.x = (uint)(GR.RegionLocX + telehub.TelehubLocX);
                    mapitem.y = (uint)(GR.RegionLocY + telehub.TelehubLocY);
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
                if (DSC == null)
                    return;
                DirLandReplyData[] Landdata = DSC.FindLandForSale("4294967295", int.MaxValue.ToString(), "0", 0, (uint)DirectoryManager.DirFindFlags.IncludePG);
                
                uint locX = 0;
                uint locY = 0;
                foreach (DirLandReplyData landDir in Landdata)
                {
                    LandData landdata = DSC.GetParcelInfo(landDir.parcelID);
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
                    mapitem.x = (uint)(locX + landdata.UserLocation.X);
                    mapitem.y = (uint)(locY + landdata.UserLocation.Y);
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
                if (DSC == null)
                    return;
                DirLandReplyData[] Landdata = DSC.FindLandForSale("4294967295", int.MaxValue.ToString(), "0",0, 0);
                
                uint locX = 0;
                uint locY = 0;
                foreach (DirLandReplyData landDir in Landdata)
                {
                    LandData landdata = DSC.GetParcelInfo(landDir.parcelID);
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
                    mapitem.x = (uint)(locX + landdata.UserLocation.X);
                    mapitem.y = (uint)(locY + landdata.UserLocation.Y);
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
                if (DSC == null)
                    return;
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
                    mapitem.x = (uint)globalPos.X;
                    mapitem.y = (uint)globalPos.Y;
                    mapitem.id = UUID.Random();
                    mapitem.name = eventData.name;
                    mapitem.Extra = (int)eventdata.dateUTC;
                    mapitem.Extra2 = (int)eventdata.eventID;//(int)DirectoryManager.EventFlags.PG;
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
                if (DSC == null)
                    return;
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
                    mapitem.x = (uint)globalPos.X;
                    mapitem.y = (uint)globalPos.Y;
                    mapitem.id = UUID.Random();
                    mapitem.name = eventData.name;
                    mapitem.Extra = (int)eventdata.dateUTC;
                    mapitem.Extra2 = (int)eventdata.eventID;//(int)DirectoryManager.EventFlags.PG;
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
                if (DSC == null)
                    return;
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
                    mapitem.x = (uint)globalPos.X;
                    mapitem.y = (uint)globalPos.Y;
                    mapitem.id = UUID.Random();
                    mapitem.name = eventData.name;
                    mapitem.Extra = (int)eventdata.dateUTC;
                    mapitem.Extra2 = (int)eventdata.eventID;//(int)DirectoryManager.EventFlags.PG;
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
                if (DSC == null)
                    return;
                Classified[] Classifieds = DSC.GetClassifiedsInRegion(GR.RegionName);
                foreach (Classified classified in Classifieds)
                {
                    OpenSim.Services.Interfaces.GridRegion region = m_scene.GridService.GetRegionByName(UUID.Zero, classified.SimName);
                    mapitem = new mapItemReply();
                    mapitem.x = (uint)(region.RegionLocX + classified.GlobalPos.X);
                    mapitem.y = (uint)(region.RegionLocY + classified.GlobalPos.Y);
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
                LandData[] LandData = DSC.GetParcelByOwner(client.AgentId);
                List<ExtendedLandData> parcels = new List<ExtendedLandData>();
                foreach (LandData land in LandData)
                {
                    OpenSim.Services.Interfaces.GridRegion region = m_scene.GridService.GetRegionByUUID(UUID.Zero, land.RegionID);
                    if (region != null)
                    {
                        ExtendedLandData parcel = new ExtendedLandData();
                        parcel.LandData = land;
                        parcel.RegionType = region.RegionType;
                        parcel.RegionName = region.RegionName;
                        parcel.GlobalPosX = region.RegionLocX + land.UserLocation.X;
                        parcel.GlobalPosY = region.RegionLocY + land.UserLocation.Y;
                        parcels.Add(parcel);
                    }
                }
                
                client.SendPlacesQuery(parcels.ToArray(), QueryID, TransactionID);
            }
            if (QueryFlags == 256) //Group Owned
            {
                LandData[] LandData = DSC.GetParcelByOwner(QueryID);
                List<ExtendedLandData> parcels = new List<ExtendedLandData>();
                foreach (LandData land in LandData)
                {
                    OpenSim.Services.Interfaces.GridRegion region = m_scene.GridService.GetRegionByUUID(UUID.Zero, land.RegionID);
                    if (region != null)
                    {
                        ExtendedLandData parcel = new ExtendedLandData();
                        parcel.LandData = land;
                        parcel.RegionType = region.RegionType;
                        parcel.RegionName = region.RegionName;
                        parcel.GlobalPosX = region.RegionLocX + land.UserLocation.X;
                        parcel.GlobalPosY = region.RegionLocY + land.UserLocation.Y;
                        parcels.Add(parcel);
                    }
                }

                client.SendPlacesQuery(parcels.ToArray(), QueryID, TransactionID);
            }
        }

        #endregion
    }
}
