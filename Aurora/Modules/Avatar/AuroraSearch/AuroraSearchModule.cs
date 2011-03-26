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
using OpenMetaverse.Packets;
using ProfileFlags = OpenMetaverse.ProfileFlags;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Nwc.XmlRpc;
using System.Xml;
using Aurora.Framework;
using Aurora.DataManager;
using OpenSim.Services.Interfaces;
using Aurora.Simulation.Base;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;

namespace Aurora.Modules
{
    public class AuroraSearchModule : ISharedRegionModule
    {
        #region Declares

        //private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IProfileConnector ProfileFrontend = null;
        private List<Scene> m_Scenes = new List<Scene>();
        private bool m_SearchEnabled = false;
        private IGroupsModule GroupsModule = null;
        private IDirectoryServiceConnector directoryService = null;

        #endregion

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
            IConfig searchConfig = config.Configs["Search"];
            if (searchConfig != null) //Check whether we are enabled
                if (searchConfig.GetString("SearchModule", Name) == Name)
                    m_SearchEnabled = true;
        }

        public void AddRegion(Scene scene)
        {
            if (!m_SearchEnabled)
                return;

            if (!m_Scenes.Contains(scene))
                m_Scenes.Add(scene);
            scene.EventManager.OnNewClient += NewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_SearchEnabled)
                return;

            if (m_Scenes.Contains(scene))
                m_Scenes.Remove(scene);
            scene.EventManager.OnNewClient -= NewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_SearchEnabled)
                return;
            //Pull in the services we need
            ProfileFrontend = DataManager.DataManager.RequestPlugin<IProfileConnector>();
            directoryService = Aurora.DataManager.DataManager.RequestPlugin<IDirectoryServiceConnector>();
            GroupsModule = scene.RequestModuleInterface<IGroupsModule>();
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
            client.OnAvatarPickerRequest += ProcessAvatarPickerRequest;
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnDirPlacesQuery -= DirPlacesQuery;
            client.OnDirFindQuery -= DirFindQuery;
            client.OnDirPopularQuery -= DirPopularQuery;
            client.OnDirLandQuery -= DirLandQuery;
            client.OnDirClassifiedQuery -= DirClassifiedQuery;
            // Response after Directory Queries
            client.OnEventInfoRequest -= EventInfoRequest;
            client.OnMapItemRequest -= HandleMapItemRequest;
            client.OnPlacesQuery -= OnPlacesQueryRequest;
            client.OnAvatarPickerRequest -= ProcessAvatarPickerRequest;
        }

        #endregion

        #region Search Module

        /// <summary>
        /// Parcel request
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="queryID"></param>
        /// <param name="queryText">The thing to search for</param>
        /// <param name="queryFlags"></param>
        /// <param name="category"></param>
        /// <param name="simName"></param>
        /// <param name="queryStart"></param>
        protected void DirPlacesQuery(IClientAPI remoteClient, UUID queryID,
                                      string queryText, int queryFlags, int category, string simName,
                                      int queryStart)
        {
            List<DirPlacesReplyData> ReturnValues = new List<DirPlacesReplyData>(directoryService.FindLand(queryText, category.ToString(), queryStart, (uint)queryFlags));
            
            SplitPackets<DirPlacesReplyData> (ReturnValues, delegate (DirPlacesReplyData[] data)
            {
                remoteClient.SendDirPlacesReply (queryID, data);
            });
        }

        public void DirPopularQuery(IClientAPI remoteClient, UUID queryID, uint queryFlags)
        {
            /// <summary>
            /// Deprecated as no newer client support it
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
            List<DirLandReplyData> ReturnValues = new List<DirLandReplyData>(directoryService.FindLandForSale(searchType.ToString(), price.ToString(), area.ToString(), queryStart, queryFlags));
            
            SplitPackets<DirLandReplyData> (ReturnValues, delegate (DirLandReplyData[] data)
            {
                remoteClient.SendDirLandReply (queryID, data);
            });
        }

        /// <summary>
        /// Finds either people or events
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="queryID">Just a UUID to send back to the client</param>
        /// <param name="queryText">The term to search for</param>
        /// <param name="queryFlags">Flags like maturity, etc</param>
        /// <param name="queryStart">Where in the search should we start? 0, 10, 20, etc</param>
        public void DirFindQuery(IClientAPI remoteClient, UUID queryID,
                                 string queryText, uint queryFlags, int queryStart)
        {
            if ((queryFlags & 1) != 0) //People query
            {
                DirPeopleQuery(remoteClient, queryID, queryText, queryFlags,
                               queryStart);
                return;
            }
            else if ((queryFlags & 32) != 0) //Events query
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
            //Find the user accounts
            List<UserAccount> accounts = m_Scenes[0].UserAccountService.GetUserAccounts(m_Scenes[0].RegionInfo.ScopeID, queryText);
            List<DirPeopleReplyData> ReturnValues =
                    new List<DirPeopleReplyData>();

            foreach (UserAccount item in accounts)
            {
                //This is really bad, we should not be searching for all of these people again in the Profile service
                IUserProfileInfo UserProfile = ProfileFrontend.GetUserProfile(item.PrincipalID);
                if (UserProfile == null)
                {
                    DirPeopleReplyData person = new DirPeopleReplyData();
                    person.agentID = item.PrincipalID;
                    person.firstName = item.FirstName;
                    person.lastName = item.LastName;
                    if (GroupsModule == null)
                        person.group = "";
                    else
                    {
                        person.group = "";
                        GroupMembershipData[] memberships = GroupsModule.GetMembershipData(item.PrincipalID);
                        foreach (GroupMembershipData membership in memberships)
                        {
                            if (membership.Active)
                                person.group = membership.GroupName;
                        }
                    }
                    //Then we have to pull the GUI to see if the user is online or not
                    UserInfo Pinfo = m_Scenes[0].RequestModuleInterface<IAgentInfoService>().GetUserInfo(item.PrincipalID.ToString());
                    if (Pinfo != null && Pinfo.IsOnline) //If it is null, they are offline
                        person.online = true;
                    person.reputation = 0;
                    ReturnValues.Add(person);
                }
                else if (UserProfile.AllowPublish) //Check whether they want to be in search or not
                {
                    DirPeopleReplyData person = new DirPeopleReplyData();
                    person.agentID = item.PrincipalID;
                    person.firstName = item.FirstName;
                    person.lastName = item.LastName;
                    if (GroupsModule == null)
                        person.group = "";
                    else
                    {
                        person.group = "";
                        //Check what group they have set
                        GroupMembershipData[] memberships = GroupsModule.GetMembershipData(item.PrincipalID);
                        foreach (GroupMembershipData membership in memberships)
                        {
                            if (membership.Active)
                                person.group = membership.GroupName;
                        }
                    }
                    //Then we have to pull the GUI to see if the user is online or not
                    UserInfo Pinfo = m_Scenes[0].RequestModuleInterface<IAgentInfoService>().GetUserInfo(item.PrincipalID.ToString());
                    if (Pinfo != null && Pinfo.IsOnline)
                        person.online = true;
                    person.reputation = 0;
                    ReturnValues.Add(person);
                }
            }

            SplitPackets<DirPeopleReplyData> (ReturnValues, delegate (DirPeopleReplyData[] data)
            {
                remoteClient.SendDirPeopleReply (queryID, data);
            });
        }

        public void DirEventsQuery(IClientAPI remoteClient, UUID queryID,
                                   string queryText, uint queryFlags, int queryStart)
        {
            List<DirEventsReplyData> ReturnValues = new List<DirEventsReplyData>(directoryService.FindEvents(queryText, queryFlags.ToString(), queryStart));

            SplitPackets<DirEventsReplyData> (ReturnValues, delegate (DirEventsReplyData[] data)
            {
                remoteClient.SendDirEventsReply (queryID, data);
            });
        }

        public void DirClassifiedQuery(IClientAPI remoteClient, UUID queryID,
                                       string queryText, uint queryFlags, uint category,
                                       int queryStart)
        {
            List<DirClassifiedReplyData> ReturnValues = new List<DirClassifiedReplyData>(directoryService.FindClassifieds(queryText, category.ToString(), queryFlags.ToString(), queryStart));

            SplitPackets<DirClassifiedReplyData> (ReturnValues, delegate (DirClassifiedReplyData[] data)
            {
                remoteClient.SendDirClassifiedReply (queryID, data);
            });
        }

        public delegate void SendPacket<T>(T[] data);
        public void SplitPackets<T>(List<T> packets, SendPacket<T> send)
        {
            if (packets.Count == 0)
            {
                send(new T[0]);
                return;
            }
            int i = 0;
            while (i < packets.Count)
            {
                int count = Math.Min (10, packets.Count);
                //Split into sets of 10 packets
                T[] data = packets.GetRange (i, count).ToArray();
                i += count;
                if (data.Length != 0)
                    send(data);
            }
        }

        /// <summary>
        /// Tell the client about X event
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="queryEventID">ID of the event</param>
        public void EventInfoRequest(IClientAPI remoteClient, uint queryEventID)
        {
            //Find the event
            EventData data = directoryService.GetEventInfo(queryEventID.ToString());
            //Send the event
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
            OpenMetaverse.Utils.LongToUInts(remoteClient.Scene.RegionInfo.RegionHandle, out xstart, out ystart);
            OpenSim.Services.Interfaces.GridRegion GR = m_Scenes[0].GridService.GetRegionByPosition(UUID.Zero, (int)xstart, (int)ystart);

            #region Telehub
            if (itemtype == (uint)OpenMetaverse.GridItemType.Telehub)
            {
                IRegionConnector GF = DataManager.DataManager.RequestPlugin<IRegionConnector>();
                if (GF == null)
                    return;

                int tc = Environment.TickCount;
                //Find the telehub
                Telehub telehub = GF.FindTelehub(GR.RegionID, GR.RegionHandle);
                if (telehub != null)
                {
                    mapitem = new mapItemReply();
                    //The position is in GLOBAL coordinates (in meters)
                    mapitem.x = (uint)(GR.RegionLocX + telehub.TelehubLocX);
                    mapitem.y = (uint)(GR.RegionLocY + telehub.TelehubLocY);
                    mapitem.id = GR.RegionID;
                    //This is how the name is sent, go figure
                    mapitem.name = Util.Md5Hash(GR.RegionName + tc.ToString());
                    //Not sure, but this is what gets sent
                    mapitem.Extra = 1;
                    mapitem.Extra2 = 0;

                    mapitems.Add(mapitem);
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                    mapitems.Clear();
                }
            }

            #endregion

            #region Land for sale

            //PG land that is for sale
            if (itemtype == (uint)OpenMetaverse.GridItemType.LandForSale)
            {
                if (directoryService == null)
                    return;
                //Find all the land, use "0" for the flags so we get all land for sale, no price or area checking
                DirLandReplyData[] Landdata = directoryService.FindLandForSale("0", int.MaxValue.ToString(), "0", 0, 0);
                
                int locX = 0;
                int locY = 0;
                foreach (DirLandReplyData landDir in Landdata)
                {
                    LandData landdata = directoryService.GetParcelInfo(landDir.parcelID);
                    if (landdata.Maturity != 0)
                        continue; //Not a PG land 
                    foreach (Scene scene in m_Scenes)
                    {
                        if (scene.RegionInfo.RegionID == landdata.RegionID)
                        {
                            //Global coords, so add the meters
                            locX = scene.RegionInfo.RegionLocX;
                            locY = scene.RegionInfo.RegionLocY;
                        }
                    }
                    if (locY == 0 && locX == 0)
                    {
                        //Ask the grid service for the coordinates if the region is not local
                        OpenSim.Services.Interfaces.GridRegion r = m_Scenes[0].GridService.GetRegionByUUID(UUID.Zero, landdata.RegionID);
                        if (r != null)
                        {
                            locX = r.RegionLocX;
                            locY = r.RegionLocY;
                        }
                    }
                    if (locY == 0 && locX == 0) //Couldn't find the region, don't send
                        continue;

                    mapitem = new mapItemReply();
                    //Global coords, so make sure its in meters
                    mapitem.x = (uint)(locX + landdata.UserLocation.X);
                    mapitem.y = (uint)(locY + landdata.UserLocation.Y);
                    mapitem.id = landDir.parcelID;
                    mapitem.name = landDir.name;
                    mapitem.Extra = landDir.actualArea;
                    mapitem.Extra2 = landDir.salePrice;
                    mapitems.Add(mapitem);
                }
                //Send all the map items
                if (mapitems.Count != 0)
                {
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                    mapitems.Clear();
                }
            }

            //Adult or mature land that is for sale
            if (itemtype == (uint)OpenMetaverse.GridItemType.AdultLandForSale)
            {
                if (directoryService == null)
                    return;
                //Find all the land, use "0" for the flags so we get all land for sale, no price or area checking
                DirLandReplyData[] Landdata = directoryService.FindLandForSale("0", int.MaxValue.ToString(), "0", 0, 0);
                
                int locX = 0;
                int locY = 0;
                foreach (DirLandReplyData landDir in Landdata)
                {
                    LandData landdata = directoryService.GetParcelInfo(landDir.parcelID);
                    if (landdata.Maturity == 0)
                        continue; //Its PG
                    foreach (Scene scene in m_Scenes)
                    {
                        if (scene.RegionInfo.RegionID == landdata.RegionID)
                        {
                            locX = scene.RegionInfo.RegionLocX;
                            locY = scene.RegionInfo.RegionLocY;
                        }
                    }
                    if (locY == 0 && locX == 0)
                    {
                        //Ask the grid service for the coordinates if the region is not local
                        OpenSim.Services.Interfaces.GridRegion r = m_Scenes[0].GridService.GetRegionByUUID(UUID.Zero, landdata.RegionID);
                        if (r != null)
                        {
                            locX = r.RegionLocX;
                            locY = r.RegionLocY;
                        }
                    }
                    if (locY == 0 && locX == 0) //Couldn't find the region, don't send
                        continue;

                    mapitem = new mapItemReply();
                    //Global coords, so make sure its in meters
                    mapitem.x = (uint)(locX + landdata.UserLocation.X);
                    mapitem.y = (uint)(locY + landdata.UserLocation.Y);
                    mapitem.id = landDir.parcelID;
                    mapitem.name = landDir.name;
                    mapitem.Extra = landDir.actualArea;
                    mapitem.Extra2 = landDir.salePrice;

                    mapitems.Add(mapitem);
                }
                //Send the results if we have any
                if (mapitems.Count != 0)
                {
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                    mapitems.Clear();
                }
            }

            #endregion

            #region Events

            if (itemtype == (uint)OpenMetaverse.GridItemType.PgEvent ||
                itemtype == (uint)OpenMetaverse.GridItemType.MatureEvent ||
                itemtype == (uint)OpenMetaverse.GridItemType.AdultEvent)
            {
                if (directoryService == null)
                    return;

                //Find the maturity level
                int maturity = itemtype == (uint)OpenMetaverse.GridItemType.PgEvent ?
                    (int)DirectoryManager.EventFlags.PG :
                    (itemtype == (uint)GridItemType.MatureEvent) ?
                    (int)DirectoryManager.EventFlags.Mature :
                    (int)DirectoryManager.EventFlags.Adult;

                //Gets all the events occuring in the given region by maturity level
                DirEventsReplyData[] Eventdata = directoryService.FindAllEventsInRegion(GR.RegionName, maturity);
                
                foreach (DirEventsReplyData eventData in Eventdata)
                {
                    //Get more info on the event
                    EventData eventdata = directoryService.GetEventInfo(eventData.eventID.ToString());
                    
                    Vector3 globalPos = eventdata.globalPos;
                    mapitem = new mapItemReply();
                    
                    //Use global position plus half the region so that it doesn't always appear in the bottom corner
                    mapitem.x = (uint)(globalPos.X + (remoteClient.Scene.RegionInfo.RegionSizeX / 2));
                    mapitem.y = (uint)(globalPos.Y + (remoteClient.Scene.RegionInfo.RegionSizeY / 2));
                    
                    mapitem.id = UUID.Random();
                    mapitem.name = eventData.name;
                    mapitem.Extra = (int)eventdata.dateUTC;
                    mapitem.Extra2 = (int)eventdata.eventID;
                    mapitems.Add(mapitem);
                }
                //Send if we have any
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
                if (directoryService == null)
                    return;
                //Get all the classifieds in this region
                Classified[] Classifieds = directoryService.GetClassifiedsInRegion(GR.RegionName);
                foreach (Classified classified in Classifieds)
                {
                    //Get the region so we have its position
                    OpenSim.Services.Interfaces.GridRegion region = m_Scenes[0].GridService.GetRegionByName(UUID.Zero, classified.SimName);
                    
                    mapitem = new mapItemReply();
                    
                    //Use global position plus half the sim so that all classifieds are not in the bottom corner
                    mapitem.x = (uint)(region.RegionLocX + classified.GlobalPos.X + (remoteClient.Scene.RegionInfo.RegionSizeX / 2));
                    mapitem.y = (uint)(region.RegionLocY + classified.GlobalPos.Y + (remoteClient.Scene.RegionInfo.RegionSizeY / 2));
                    
                    mapitem.id = classified.CreatorUUID;
                    mapitem.name = classified.Name;
                    mapitem.Extra = 0;
                    mapitem.Extra2 = 0;
                    mapitems.Add(mapitem);
                }
                //Send the events, if we have any
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
                //Get all the parcels
                LandData[] LandData = directoryService.GetParcelByOwner(client.AgentId);

                List<ExtendedLandData> parcels = new List<ExtendedLandData>();
                foreach (LandData land in LandData)
                {
                    //Find the region so we can add the meters correctly
                    OpenSim.Services.Interfaces.GridRegion region = m_Scenes[0].GridService.GetRegionByUUID(UUID.Zero, land.RegionID);
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
                //Find all the group owned land
                LandData[] LandData = directoryService.GetParcelByOwner(QueryID);

                List<ExtendedLandData> parcels = new List<ExtendedLandData>();
                foreach (LandData land in LandData)
                {
                    //Find the region from the grid service so that we can add the meters correctly
                    OpenSim.Services.Interfaces.GridRegion region = m_Scenes[0].GridService.GetRegionByUUID(UUID.Zero, land.RegionID);
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
                //Send if we have any parcels
                if(parcels.Count != 0)
                    client.SendPlacesQuery(parcels.ToArray(), QueryID, TransactionID);
            }
        }

        public void ProcessAvatarPickerRequest(IClientAPI client, UUID avatarID, UUID RequestID, string query)
        {
            Scene scene = (Scene)client.Scene;
            List<UserAccount> accounts = scene.UserAccountService.GetUserAccounts(scene.RegionInfo.ScopeID, query);

            if (accounts == null)
                accounts = new List<UserAccount>(0);

            AvatarPickerReplyPacket replyPacket = (AvatarPickerReplyPacket)PacketPool.Instance.GetPacket(PacketType.AvatarPickerReply);
            // TODO: don't create new blocks if recycling an old packet

            AvatarPickerReplyPacket.DataBlock[] searchData =
                new AvatarPickerReplyPacket.DataBlock[accounts.Count];
            AvatarPickerReplyPacket.AgentDataBlock agentData = new AvatarPickerReplyPacket.AgentDataBlock();

            agentData.AgentID = avatarID;
            agentData.QueryID = RequestID;
            replyPacket.AgentData = agentData;
            //byte[] bytes = new byte[AvatarResponses.Count*32];

            int i = 0;
            foreach (UserAccount item in accounts)
            {
                UUID translatedIDtem = item.PrincipalID;
                searchData[i] = new AvatarPickerReplyPacket.DataBlock();
                searchData[i].AvatarID = translatedIDtem;
                searchData[i].FirstName = Utils.StringToBytes((string)item.FirstName);
                searchData[i].LastName = Utils.StringToBytes((string)item.LastName);
                i++;
            }
            if (accounts.Count == 0)
            {
                searchData = new AvatarPickerReplyPacket.DataBlock[0];
            }
            replyPacket.Data = searchData;

            AvatarPickerReplyAgentDataArgs agent_data = new AvatarPickerReplyAgentDataArgs();
            agent_data.AgentID = replyPacket.AgentData.AgentID;
            agent_data.QueryID = replyPacket.AgentData.QueryID;

            List<AvatarPickerReplyDataArgs> data_args = new List<AvatarPickerReplyDataArgs>();
            for (i = 0; i < replyPacket.Data.Length; i++)
            {
                AvatarPickerReplyDataArgs data_arg = new AvatarPickerReplyDataArgs();
                data_arg.AvatarID = replyPacket.Data[i].AvatarID;
                data_arg.FirstName = replyPacket.Data[i].FirstName;
                data_arg.LastName = replyPacket.Data[i].LastName;
                data_args.Add(data_arg);
            }
            client.SendAvatarPickerReply(agent_data, data_args);
        }

        #endregion
    }
}
