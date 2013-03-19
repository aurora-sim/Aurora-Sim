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
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Servers.HttpServer.Interfaces;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.IO;

namespace Aurora.Modules.Auction
{
    public class AuctionModule : IAuctionModule, INonSharedRegionModule
    {
        private IScene m_scene;

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource pSource)
        {
        }

        public void AddRegion(IScene scene)
        {
            m_scene = scene;
            m_scene.EventManager.OnRegisterCaps += RegisterCaps;
            m_scene.EventManager.OnNewClient += OnNewClient;
            m_scene.EventManager.OnClosingClient += OnClosingClient;
        }

        public void RemoveRegion(IScene scene)
        {
            m_scene.EventManager.OnRegisterCaps -= RegisterCaps;
            m_scene.EventManager.OnNewClient -= OnNewClient;
            m_scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "AuctionModule"; }
        }

        public void Close()
        {
        }

        #endregion

        #region Client members

        public void OnNewClient(IClientAPI client)
        {
            client.OnViewerStartAuction += StartAuction;
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnViewerStartAuction -= StartAuction;
        }

        public void StartAuction(IClientAPI client, int LocalID, UUID SnapshotID)
        {
            if (!m_scene.Permissions.IsGod(client.AgentId))
                return;
            StartAuction(LocalID, SnapshotID);
        }

        #endregion

        #region CAPS

        public OSDMap RegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["ViewerStartAuction"] = CapsUtil.CreateCAPS("ViewerStartAuction", "");

            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["ViewerStartAuction"],
                                                             ViewerStartAuction));
            return retVal;
        }

        private byte[] ViewerStartAuction(string path, Stream request,
                                          OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            //OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml(request);

            return MainServer.NoResponse;
        }

        #endregion

        #region IAuctionModule members

        public void StartAuction(int LocalID, UUID SnapshotID)
        {
            IParcelManagementModule parcelManagement = m_scene.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject landObject = parcelManagement.GetLandObject(LocalID);
                if (landObject == null)
                    return;
                landObject.LandData.SnapshotID = SnapshotID;
                landObject.LandData.AuctionID = (uint) Util.RandomClass.Next(0, int.MaxValue);
                landObject.LandData.Status = ParcelStatus.Abandoned;
                landObject.SendLandUpdateToAvatarsOverMe();
            }
        }

        public void SetAuctionInfo(int LocalID, AuctionInfo info)
        {
            SaveAuctionInfo(LocalID, info);
        }

        public void AddAuctionBid(int LocalID, UUID userID, int bid)
        {
            AuctionInfo info = GetAuctionInfo(LocalID);
            info.AuctionBids.Add(new AuctionBid() {Amount = bid, AuctionBidder = userID, TimeBid = DateTime.Now});
            SaveAuctionInfo(LocalID, info);
        }

        public void AuctionEnd(int LocalID)
        {
            IParcelManagementModule parcelManagement = m_scene.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject landObject = parcelManagement.GetLandObject(LocalID);
                if (landObject == null)
                    return;

                AuctionInfo info = GetAuctionInfo(LocalID);
                AuctionBid highestBid = new AuctionBid() {Amount = 0};
                foreach (AuctionBid bid in info.AuctionBids)
                    if (highestBid.Amount < bid.Amount)
                        highestBid = bid;

                IOfflineMessagesConnector offlineMessages =
                    Framework.Utilities.DataManager.RequestPlugin<IOfflineMessagesConnector>();
                if (offlineMessages != null)
                    offlineMessages.AddOfflineMessage(new GridInstantMessage()
                                                          {
                                                              binaryBucket = new byte[0],
                                                              dialog = (byte) InstantMessageDialog.MessageBox,
                                                              fromAgentID = UUID.Zero,
                                                              fromAgentName = "System",
                                                              fromGroup = false,
                                                              imSessionID = UUID.Random(),
                                                              message =
                                                                  "You won the auction for the parcel " +
                                                                  landObject.LandData.Name + ", paying " +
                                                                  highestBid.Amount + " for it",
                                                              offline = 0,
                                                              ParentEstateID = 0,
                                                              Position = Vector3.Zero,
                                                              RegionID = m_scene.RegionInfo.RegionID,
                                                              timestamp = (uint) Util.UnixTimeSinceEpoch(),
                                                              toAgentID = highestBid.AuctionBidder
                                                          });
                landObject.UpdateLandSold(highestBid.AuctionBidder, UUID.Zero, false, landObject.LandData.AuctionID,
                                          highestBid.Amount, landObject.LandData.Area);
            }
        }

        private void SaveAuctionInfo(int LocalID, AuctionInfo info)
        {
            IParcelManagementModule parcelManagement = m_scene.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject landObject = parcelManagement.GetLandObject(LocalID);
                if (landObject == null)
                    return;
                landObject.LandData.AuctionInfo = info;
            }
        }

        private AuctionInfo GetAuctionInfo(int LocalID)
        {
            IParcelManagementModule parcelManagement = m_scene.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject landObject = parcelManagement.GetLandObject(LocalID);
                if (landObject == null)
                    return null;
                return landObject.LandData.AuctionInfo;
            }
            return null;
        }

        #endregion
    }
}