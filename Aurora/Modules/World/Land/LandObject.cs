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
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Land
{
    /// <summary>
    ///   Keeps track of a specific piece of land's information
    /// </summary>
    public class LandObject : ILandObject
    {
        #region Member Variables

        protected LandData m_landData = new LandData();
        private int m_lastSeqId;
        protected IParcelManagementModule m_parcelManagementModule;
        protected IScene m_scene;

        #endregion

        #region ILandObject Members

        public LandData LandData
        {
            get { return m_landData; }

            set
            {
                //Fix the land data HERE
                if (m_scene != null) //Not sure that this ever WILL be null... but we'll be safe...
                    value.Maturity = m_scene.RegionInfo.RegionSettings.Maturity;
                m_landData = value;
            }
        }

        public UUID RegionUUID
        {
            get { return m_scene.RegionInfo.RegionID; }
        }

        /// <summary>
        ///   Set the media url for this land parcel
        /// </summary>
        /// <param name = "url"></param>
        public void SetMediaUrl(string url)
        {
            LandData.MediaURL = url;
            SendLandUpdateToAvatarsOverMe();
        }

        /// <summary>
        ///   Set the music url for this land parcel
        /// </summary>
        /// <param name = "url"></param>
        public void SetMusicUrl(string url)
        {
            LandData.MusicURL = url;
            SendLandUpdateToAvatarsOverMe();
        }

        #endregion

        #region Constructors

        public LandObject(UUID owner_id, bool is_group_owned, IScene scene)
        {
            m_scene = scene;

            LandData.Maturity = m_scene.RegionInfo.RegionSettings.Maturity;
            LandData.OwnerID = owner_id;
            LandData.GroupID = is_group_owned ? owner_id : UUID.Zero;
            LandData.IsGroupOwned = is_group_owned;

            LandData.RegionID = scene.RegionInfo.RegionID;
            LandData.ScopeID = scene.RegionInfo.ScopeID;
            LandData.RegionHandle = scene.RegionInfo.RegionHandle;

            m_parcelManagementModule = scene.RequestModuleInterface<IParcelManagementModule>();

            //We don't set up the InfoID here... it will just be overwriten
        }

        public void SetInfoID()
        {
            //Make the InfoUUID for this parcel
            uint x = (uint) LandData.UserLocation.X, y = (uint) LandData.UserLocation.Y;
            findPointInParcel(this, ref x, ref y); // find a suitable spot
            LandData.InfoUUID = Util.BuildFakeParcelID(m_scene.RegionInfo.RegionHandle, x, y);
        }

        // this is needed for non-convex parcels (example: rectangular parcel, and in the exact center
        // another, smaller rectangular parcel). Both will have the same initial coordinates.
        private void findPointInParcel(ILandObject land, ref uint refX, ref uint refY)
        {
            // the point we started with already is in the parcel
            if (land.ContainsPoint((int) refX, (int) refY) && refX != 0 && refY != 0)
                return;

            // ... otherwise, we have to search for a point within the parcel
            uint startX = (uint) land.LandData.AABBMin.X;
            uint startY = (uint) land.LandData.AABBMin.Y;
            uint endX = (uint) land.LandData.AABBMax.X;
            uint endY = (uint) land.LandData.AABBMax.Y;

            // default: center of the parcel
            refX = (startX + endX)/2;
            refY = (startY + endY)/2;
            // If the center point is within the parcel, take that one
            if (land.ContainsPoint((int) refX, (int) refY)) return;

            // otherwise, go the long way.
            for (uint y = startY; y <= endY; ++y)
            {
                for (uint x = startX; x <= endX; ++x)
                {
                    if (land.ContainsPoint((int) x, (int) y))
                    {
                        // found a point
                        refX = x;
                        refY = y;
                        return;
                    }
                }
            }
        }

        #endregion

        #region Member Functions

        #region General Functions

        /// <summary>
        ///   Checks to see if this land object contains a point
        /// </summary>
        /// <param name = "x"></param>
        /// <param name = "y"></param>
        /// <returns>Returns true if the piece of land contains the specified point</returns>
        public bool ContainsPoint(int x, int y)
        {
            IParcelManagementModule parcelManModule = m_scene.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManModule == null)
                return false;
            if (x >= 0 && y >= 0 && x < m_scene.RegionInfo.RegionSizeX && y < m_scene.RegionInfo.RegionSizeY)
            {
                return (parcelManModule.LandIDList[x/4, y/4] == LandData.LocalID);
            }
            else
            {
                return false;
            }
        }

        public ILandObject Copy()
        {
            ILandObject newLand = new LandObject(LandData.OwnerID, LandData.IsGroupOwned, m_scene);

            //Place all new variables here!
            newLand.LandData = LandData.Copy();

            return newLand;
        }

        #endregion

        #region Packet Request Handling

        public void SendLandProperties(int sequence_id, bool snap_selection, int request_result,
                                       IClientAPI remote_client)
        {
            IEstateModule estateModule = m_scene.RequestModuleInterface<IEstateModule>();
            ulong regionFlags = 336723974 & ~((uint) (OpenMetaverse.RegionFlags.AllowLandmark | OpenMetaverse.RegionFlags.AllowSetHome));
            if (estateModule != null)
                regionFlags = estateModule.GetRegionFlags();

            int seq_id;
            if (snap_selection && (sequence_id == 0))
                seq_id = m_lastSeqId;
            else
            {
                seq_id = sequence_id;
                m_lastSeqId = seq_id;
            }

            int MaxPrimCounts = 0;
            IPrimCountModule primCountModule = m_scene.RequestModuleInterface<IPrimCountModule>();
            if (primCountModule != null)
            {
                MaxPrimCounts = primCountModule.GetParcelMaxPrimCount(this);
            }

            remote_client.SendLandProperties(seq_id,
                                             snap_selection, request_result, LandData,
                                             (float) m_scene.RegionInfo.RegionSettings.ObjectBonus,
                                             MaxPrimCounts,
                                             m_scene.RegionInfo.ObjectCapacity, (uint)regionFlags);
        }

        public void UpdateLandProperties(LandUpdateArgs args, IClientAPI remote_client)
        {
            if (m_scene.Permissions.CanEditParcel(remote_client.AgentId, this) &&
                m_scene.RegionInfo.EstateSettings.AllowParcelChanges)
            {
                try
                {
                    bool snap_selection = false;

                    if (args.AuthBuyerID != LandData.AuthBuyerID || args.SalePrice != LandData.SalePrice)
                    {
                        if (m_scene.Permissions.CanSellParcel(remote_client.AgentId, this) &&
                            m_scene.RegionInfo.RegionSettings.AllowLandResell)
                        {
                            LandData.AuthBuyerID = args.AuthBuyerID;
                            LandData.SalePrice = args.SalePrice;
                            snap_selection = true;
                        }
                        else
                        {
                            remote_client.SendAlertMessage("Permissions: You cannot set this parcel for sale");
                            args.ParcelFlags &= ~(uint) ParcelFlags.ForSale;
                            args.ParcelFlags &= ~(uint) ParcelFlags.ForSaleObjects;
                            args.ParcelFlags &= ~(uint) ParcelFlags.SellParcelObjects;
                        }
                    }

                    if (m_scene.Permissions.CanEditParcelProperties(remote_client.AgentId, this, GroupPowers.LandSetSale))
                    {
                        if (!LandData.IsGroupOwned)
                        {
                            LandData.GroupID = args.GroupID;
                        }
                    }

                    if (m_scene.Permissions.CanEditParcelProperties(remote_client.AgentId, this, GroupPowers.FindPlaces))
                        LandData.Category = args.Category;

                    if (m_scene.Permissions.CanEditParcelProperties(remote_client.AgentId, this, GroupPowers.ChangeMedia))
                    {
                        LandData.MediaAutoScale = args.MediaAutoScale;
                        LandData.MediaID = args.MediaID;
                        LandData.MediaURL = args.MediaURL;
                        LandData.MusicURL = args.MusicURL;
                        LandData.MediaType = args.MediaType;
                        LandData.MediaDescription = args.MediaDescription;
                        LandData.MediaWidth = args.MediaWidth;
                        LandData.MediaHeight = args.MediaHeight;
                        LandData.MediaLoop = args.MediaLoop;
                        LandData.ObscureMusic = args.ObscureMusic;
                        LandData.ObscureMedia = args.ObscureMedia;
                    }

                    if (m_scene.RegionInfo.RegionSettings.BlockFly &&
                        ((args.ParcelFlags & (uint) ParcelFlags.AllowFly) == (uint) ParcelFlags.AllowFly))
                        //Vanquish flying as per estate settings!
                        args.ParcelFlags &= ~(uint) ParcelFlags.AllowFly;

                    if (m_scene.RegionInfo.RegionSettings.RestrictPushing &&
                        ((args.ParcelFlags & (uint) ParcelFlags.RestrictPushObject) ==
                         (uint) ParcelFlags.RestrictPushObject))
                        //Vanquish pushing as per estate settings!
                        args.ParcelFlags &= ~(uint) ParcelFlags.RestrictPushObject;

                    if (!m_scene.RegionInfo.EstateSettings.AllowLandmark &&
                        ((args.ParcelFlags & (uint) ParcelFlags.AllowLandmark) == (uint) ParcelFlags.AllowLandmark))
                        //Vanquish landmarks as per estate settings!
                        args.ParcelFlags &= ~(uint) ParcelFlags.AllowLandmark;

                    if (m_scene.RegionInfo.RegionSettings.BlockShowInSearch &&
                        ((args.ParcelFlags & (uint) ParcelFlags.ShowDirectory) == (uint) ParcelFlags.ShowDirectory))
                        //Vanquish show in search as per estate settings!
                        args.ParcelFlags &= ~(uint) ParcelFlags.ShowDirectory;

                    if (m_scene.Permissions.CanEditParcelProperties(remote_client.AgentId, this,
                                                                    GroupPowers.SetLandingPoint))
                    {
                        LandData.LandingType = args.LandingType;
                        LandData.UserLocation = args.UserLocation;
                        LandData.UserLookAt = args.UserLookAt;
                    }

                    if (m_scene.Permissions.CanEditParcelProperties(remote_client.AgentId, this,
                                                                    GroupPowers.LandChangeIdentity))
                    {
                        LandData.Description = args.Desc;
                        LandData.Name = args.Name;
                        LandData.SnapshotID = args.SnapshotID;
                        LandData.Private = args.Privacy;
                    }

                    if (m_scene.Permissions.CanEditParcelProperties(remote_client.AgentId, this,
                                                                    GroupPowers.LandManagePasses))
                    {
                        LandData.PassHours = args.PassHours;
                        LandData.PassPrice = args.PassPrice;
                    }

                    if ((args.ParcelFlags & (uint) ParcelFlags.ShowDirectory) == (uint) ParcelFlags.ShowDirectory &&
                        (LandData.Flags & (uint) ParcelFlags.ShowDirectory) != (uint) ParcelFlags.ShowDirectory)
                    {
                        //If the flags have changed, we need to charge them.. maybe
                        // We really need to check per month or whatever
                        IScheduledMoneyModule moneyModule = m_scene.RequestModuleInterface<IScheduledMoneyModule>();
                        if (moneyModule != null)
                        {
                            if (!moneyModule.Charge(remote_client.AgentId, 30, "Parcel Show in Search Fee - " + LandData.GlobalID, 7))
                            {
                                remote_client.SendAlertMessage("You don't have enough money to set this parcel in search.");
                                args.ParcelFlags &= (uint)ParcelFlags.ShowDirectory;
                            }
                        }
                    }
                    LandData.Flags = args.ParcelFlags;

                    LandData.Status = LandData.OwnerID == m_parcelManagementModule.GodParcelOwner ? ParcelStatus.Abandoned : LandData.AuthBuyerID != UUID.Zero ? ParcelStatus.LeasePending : ParcelStatus.Leased;

                    m_parcelManagementModule.UpdateLandObject(this);

                    SendLandUpdateToAvatarsOverMe(snap_selection);
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.Warn("[LAND]: Error updating land object " + this.LandData.Name + " in region " +
                               this.m_scene.RegionInfo.RegionName + " : " + ex);
                }
            }
        }

        public void UpdateLandSold(UUID avatarID, UUID groupID, bool groupOwned, uint AuctionID, int claimprice,
                                   int area)
        {
            if ((LandData.Flags & (uint) ParcelFlags.SellParcelObjects) == (uint) ParcelFlags.SellParcelObjects)
            {
                //Sell all objects on the parcel too
                IPrimCountModule primCountModule = m_scene.RequestModuleInterface<IPrimCountModule>();
                IPrimCounts primCounts = primCountModule.GetPrimCounts(LandData.GlobalID);
#if (!ISWIN)
                foreach (ISceneEntity obj in primCounts.Objects)
                {
                    if (obj.OwnerID == LandData.OwnerID)
                    {
                        //Fix the owner/last owner
                        obj.SetOwnerId(avatarID);
                        //Then update all clients around
                        obj.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
                    }
                }
#else
                foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.OwnerID == LandData.OwnerID))
                {
                    //Fix the owner/last owner
                    obj.SetOwnerId(avatarID);
                    //Then update all clients around
                    obj.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
                }
#endif
            }

            LandData.OwnerID = avatarID;
            LandData.GroupID = groupID;
            LandData.IsGroupOwned = groupOwned;
            LandData.AuctionID = 0;
            LandData.ClaimDate = Util.UnixTimeSinceEpoch();
            LandData.ClaimPrice = claimprice;
            LandData.SalePrice = 0;
            LandData.AuthBuyerID = UUID.Zero;
            LandData.Flags &=
                ~(uint)
                 (ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects |
                  ParcelFlags.ShowDirectory);

            m_parcelManagementModule.UpdateLandObject(this);

            SendLandUpdateToAvatarsOverMe(true);
            //Send a full update to the client as well
            IScenePresence SP = m_scene.GetScenePresence(avatarID);
            SendLandUpdateToClient(SP.ControllingClient);
        }

        public void DeedToGroup(UUID groupID)
        {
            LandData.OwnerID = groupID;
            LandData.GroupID = groupID;
            LandData.IsGroupOwned = true;

            // Reset show in directory flag on deed
            LandData.Flags &=
                ~(uint)
                 (ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects |
                  ParcelFlags.ShowDirectory);

            m_parcelManagementModule.UpdateLandObject(this);
        }

        public bool IsEitherBannedOrRestricted(UUID avatar)
        {
            if (IsBannedFromLand(avatar))
            {
                return true;
            }
            else if (IsRestrictedFromLand(avatar))
            {
                return true;
            }
            return false;
        }

        public bool IsBannedFromLand(UUID avatar)
        {
            if (m_scene.Permissions.IsAdministrator(avatar))
                return false;

            if (LandData.ParcelAccessList.Count > 0)
            {
                ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry
                                                            {AgentID = avatar, Flags = AccessList.Ban};
                entry = LandData.ParcelAccessList.Find(delegate(ParcelManager.ParcelAccessEntry pae)
                                                           {
                                                               if (entry.AgentID == pae.AgentID &&
                                                                   entry.Flags == pae.Flags)
                                                                   return true;
                                                               return false;
                                                           });

                //See if they are on the list, but make sure the owner isn't banned
                if (entry.AgentID == avatar && LandData.OwnerID != avatar)
                {
                    //They are banned, so lets send them a notice about this parcel
                    return true;
                }
            }
            return false;
        }

        public bool IsRestrictedFromLand(UUID avatar)
        {
            if (m_scene.Permissions.GenericParcelPermission(avatar, this, (ulong) 1))
                return false;

            if ((LandData.Flags & (uint)ParcelFlags.UsePassList) > 0 ||
                (LandData.Flags & (uint)ParcelFlags.UseAccessList) > 0)
            {
                if (LandData.ParcelAccessList.Count > 0)
                {
                    ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry();
                    bool found = false;
                    foreach (ParcelManager.ParcelAccessEntry pae in LandData.ParcelAccessList.Where(pae => avatar == pae.AgentID && AccessList.Access == pae.Flags))
                    {
                        found = true;
                        entry = pae;
                        break;
                    }

                    //If they are not on the access list and are not the owner
                    if (!found)
                    {
                        if ((LandData.Flags & (uint)ParcelFlags.UseAccessGroup) != 0)
                        {
                            IScenePresence SP = m_scene.GetScenePresence(avatar);
                            if (SP != null && LandData.GroupID == SP.ControllingClient.ActiveGroupId)
                            {
                                //They are a part of the group, let them in
                                return false;
                            }
                            else
                            {
                                //They are not allowed in this parcel, but not banned, so lets send them a notice about this parcel
                                return true;
                            }
                        }
                        else
                        {
                            //No group checking, not on the access list, restricted
                            return true;
                        }
                    }
                    else
                    {
                        //If it does, we need to check the time
                        if (entry.Time.Ticks < DateTime.Now.Ticks)
                        {
                            //Time expired, remove them
                            LandData.ParcelAccessList.Remove(entry);
                            return true;
                        }
                    }
                }
                else if ((LandData.Flags & (uint)ParcelFlags.UseAccessGroup) > 0)
                {
                    IScenePresence SP = m_scene.GetScenePresence(avatar);
                    if (SP != null && LandData.GroupID == SP.ControllingClient.ActiveGroupId)
                    {
                        //They are a part of the group, let them in
                        return false;
                    }
                    else
                    {
                        //They are not allowed in this parcel, but not banned, so lets send them a notice about this parcel
                        return true;
                    }
                }
                return true;
            }
            return false;
        }

        public void SendLandUpdateToClient(IClientAPI remote_client)
        {
            SendLandProperties(0, false, 0, remote_client);
        }

        public void SendLandUpdateToClient(bool snap_selection, IClientAPI remote_client)
        {
            SendLandProperties(0, snap_selection, 0, remote_client);
        }

        public void SendLandUpdateToAvatarsOverMe()
        {
            SendLandUpdateToAvatarsOverMe(true);
        }

        public void SendLandUpdateToAvatarsOverMe(bool snap_selection)
        {
            m_scene.ForEachScenePresence(delegate(IScenePresence avatar)
                                             {
                                                 if (avatar.IsChildAgent)
                                                     return;

                                                 if (avatar.CurrentParcel.LandData.LocalID == LandData.LocalID)
                                                 {
                                                     if (((avatar.CurrentParcel.LandData.Flags & (uint)ParcelFlags.AllowDamage) !=
                                                          0) ||
                                                         m_scene.RegionInfo.RegionSettings.AllowDamage)
                                                         avatar.Invulnerable = false;
                                                     else
                                                         avatar.Invulnerable = true;

                                                     SendLandUpdateToClient(snap_selection, avatar.ControllingClient);
                                                 }
                                             });
        }

        #endregion

        #region AccessList Functions

        public List<List<UUID>> CreateAccessListArrayByFlag(AccessList flag)
        {
            List<List<UUID>> list = new List<List<UUID>>();
            int num = 0;
            list.Add(new List<UUID>());
#if (!ISWIN)
            foreach (ParcelManager.ParcelAccessEntry entry in LandData.ParcelAccessList)
            {
                if (entry.Flags == flag)
                {
                    if (list[num].Count > ParcelManagementModule.LAND_MAX_ENTRIES_PER_PACKET)
                    {
                        num++;
                        list.Add(new List<UUID>());
                    }
                    list[num].Add(entry.AgentID);
                }
            }
#else
            foreach (ParcelManager.ParcelAccessEntry entry in LandData.ParcelAccessList.Where(entry => entry.Flags == flag))
            {
                if (list[num].Count > ParcelManagementModule.LAND_MAX_ENTRIES_PER_PACKET)
                {
                    num++;
                    list.Add(new List<UUID>());
                }
                list[num].Add(entry.AgentID);
            }
#endif
            if (list[0].Count == 0)
            {
                list[num].Add(UUID.Zero);
            }

            return list;
        }

        public void SendAccessList(UUID agentID, UUID sessionID, uint flags, int sequenceID,
                                   IClientAPI remote_client)
        {
            if (flags == (uint) AccessList.Access || flags == (uint) AccessList.Both)
            {
                List<List<UUID>> avatars = CreateAccessListArrayByFlag(AccessList.Access);
                foreach (List<UUID> accessListAvs in avatars)
                {
                    remote_client.SendLandAccessListData(accessListAvs, (uint) AccessList.Access, LandData.LocalID);
                }
            }

            if (flags == (uint) AccessList.Ban || flags == (uint) AccessList.Both)
            {
                List<List<UUID>> avatars = CreateAccessListArrayByFlag(AccessList.Ban);
                foreach (List<UUID> accessListAvs in avatars)
                {
                    remote_client.SendLandAccessListData(accessListAvs, (uint) AccessList.Ban, LandData.LocalID);
                }
            }
        }

        public void UpdateAccessList(uint flags, List<ParcelManager.ParcelAccessEntry> entries, IClientAPI remote_client)
        {
            if (entries.Count == 1 && entries[0].AgentID == UUID.Zero)
                entries.Clear();

#if (!ISWIN)
            List<ParcelManager.ParcelAccessEntry> toRemove = new List<ParcelManager.ParcelAccessEntry>();
            foreach (ParcelManager.ParcelAccessEntry entry in LandData.ParcelAccessList)
            {
                if (entry.Flags == (AccessList) flags) toRemove.Add(entry);
            }
#else
            List<ParcelManager.ParcelAccessEntry> toRemove =
                LandData.ParcelAccessList.Where(entry => entry.Flags == (AccessList) flags).ToList();
#endif

            foreach (ParcelManager.ParcelAccessEntry entry in toRemove)
            {
                LandData.ParcelAccessList.Remove(entry);
            }
#if (!ISWIN)
            foreach (ParcelManager.ParcelAccessEntry entry in entries)
            {
                ParcelManager.ParcelAccessEntry temp = new ParcelManager.ParcelAccessEntry
                                                           {
                                                               AgentID = entry.AgentID,
                                                               Time = DateTime.MaxValue,
                                                               Flags = (AccessList) flags
                                                           };
                if (!LandData.ParcelAccessList.Contains(temp))
                {
                    LandData.ParcelAccessList.Add(temp);
                }
            }
#else
            foreach (ParcelManager.ParcelAccessEntry temp in entries.Select(entry => new ParcelManager.ParcelAccessEntry
                                                                           {
                                                                               AgentID = entry.AgentID,
                                                                               Time = DateTime.MaxValue,
                                                                               Flags = (AccessList) flags
                                                                           }).Where(temp => !LandData.ParcelAccessList.Contains(temp)))
            {
                LandData.ParcelAccessList.Add(temp);
            }
#endif

            m_parcelManagementModule.UpdateLandObject(this);
        }

        #endregion

        #region Update Functions

        /// <summary>
        ///   Update all settings in land such as area, bitmap byte array, etc
        /// </summary>
        public void ForceUpdateLandInfo()
        {
            UpdateAABBAndAreaValues();
        }

        /// <summary>
        ///   Updates the AABBMin and AABBMax values after area/shape modification of the land object
        /// </summary>
        private void UpdateAABBAndAreaValues()
        {
            ITerrainChannel heightmap = m_scene.RequestModuleInterface<ITerrainChannel>();
            IParcelManagementModule parcelManModule = m_scene.RequestModuleInterface<IParcelManagementModule>();
            int min_x = m_scene.RegionInfo.RegionSizeX/4;
            int min_y = m_scene.RegionInfo.RegionSizeY/4;
            int max_x = 0;
            int max_y = 0;
            int tempArea = 0;
            int x, y;
            for (x = 0; x < m_scene.RegionInfo.RegionSizeX/4; x++)
            {
                for (y = 0; y < m_scene.RegionInfo.RegionSizeY/4; y++)
                {
                    if (parcelManModule.LandIDList[x, y] == LandData.LocalID)
                    {
                        if (min_x > x) min_x = x;
                        if (min_y > y) min_y = y;
                        if (max_x < x) max_x = x;
                        if (max_y < y) max_y = y;
                        tempArea += 16; //16sqm peice of land
                    }
                }
            }
            int tx = min_x*4;
            if (tx > (m_scene.RegionInfo.RegionSizeX - 1))
                tx = (m_scene.RegionInfo.RegionSizeX - 1);
            int ty = min_y*4;
            if (ty > (m_scene.RegionInfo.RegionSizeY - 1))
                ty = (m_scene.RegionInfo.RegionSizeY - 1);
            float min = heightmap != null ? heightmap[tx, ty] : 0;
            LandData.AABBMin =
                new Vector3((min_x*4), (min_y*4),
                            min);

            tx = max_x*4;
            if (tx > (m_scene.RegionInfo.RegionSizeX - 1))
                tx = (m_scene.RegionInfo.RegionSizeX - 1);
            ty = max_y*4;
            if (ty > (m_scene.RegionInfo.RegionSizeY - 1))
                ty = (m_scene.RegionInfo.RegionSizeY - 1);
            min = heightmap != null ? heightmap[tx, ty] : 0;
            LandData.AABBMax =
                new Vector3((max_x*4), (max_y*4),
                            min);
            LandData.Area = tempArea;
        }

        #endregion

        #region Object Select and Object Owner Listing

        public void SendForceObjectSelect(int local_id, int request_type, List<UUID> returnIDs, IClientAPI remote_client)
        {
            if (m_scene.Permissions.CanEditParcel(remote_client.AgentId, this))
            {
                List<uint> resultLocalIDs = new List<uint>();
                try
                {
                    IPrimCountModule primCountModule = m_scene.RequestModuleInterface<IPrimCountModule>();
                    IPrimCounts primCounts = primCountModule.GetPrimCounts(LandData.GlobalID);
#if (!ISWIN)
                    foreach (ISceneEntity obj in primCounts.Objects)
                    {
                        if (obj.LocalId > 0)
                        {
                            if (request_type == ParcelManagementModule.LAND_SELECT_OBJECTS_OWNER && obj.OwnerID == LandData.OwnerID)
                            {
                                resultLocalIDs.Add(obj.LocalId);
                            }
                            else if (request_type == ParcelManagementModule.LAND_SELECT_OBJECTS_GROUP && obj.GroupID == LandData.GroupID && LandData.GroupID != UUID.Zero)
                            {
                                resultLocalIDs.Add(obj.LocalId);
                            }
                            else if (request_type == ParcelManagementModule.LAND_SELECT_OBJECTS_OTHER && obj.OwnerID != remote_client.AgentId)
                            {
                                resultLocalIDs.Add(obj.LocalId);
                            }
                            else if (request_type == (int) ObjectReturnType.List && returnIDs.Contains(obj.OwnerID))
                            {
                                resultLocalIDs.Add(obj.LocalId);
                            }
                            else if (request_type == (int) ObjectReturnType.Sell && obj.OwnerID == remote_client.AgentId)
                            {
                                resultLocalIDs.Add(obj.LocalId);
                            }
                        }
                    }
#else
                    foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.LocalId > 0))
                    {
                        if (request_type == ParcelManagementModule.LAND_SELECT_OBJECTS_OWNER &&
                            obj.OwnerID == LandData.OwnerID)
                        {
                            resultLocalIDs.Add(obj.LocalId);
                        }
                        else if (request_type == ParcelManagementModule.LAND_SELECT_OBJECTS_GROUP &&
                                 obj.GroupID == LandData.GroupID && LandData.GroupID != UUID.Zero)
                        {
                            resultLocalIDs.Add(obj.LocalId);
                        }
                        else if (request_type == ParcelManagementModule.LAND_SELECT_OBJECTS_OTHER &&
                                 obj.OwnerID != remote_client.AgentId)
                        {
                            resultLocalIDs.Add(obj.LocalId);
                        }
                        else if (request_type == (int) ObjectReturnType.List && returnIDs.Contains(obj.OwnerID))
                        {
                            resultLocalIDs.Add(obj.LocalId);
                        }
                        else if (request_type == (int) ObjectReturnType.Sell &&
                                 obj.OwnerID == remote_client.AgentId)
                        {
                            resultLocalIDs.Add(obj.LocalId);
                        }
                    }
#endif
                }
                catch (InvalidOperationException)
                {
                    MainConsole.Instance.Error("[LAND]: Unable to force select the parcel objects. Arr.");
                }

                remote_client.SendForceClientSelectObjects(resultLocalIDs);
            }
        }

        ///<summary>
        ///  Notify the parcel owner each avatar that owns prims situated on their land.  This notification includes
        ///  aggreagete details such as the number of prims.
        ///</summary>
        ///<param name = "remote_client">
        ///  <see cref = "IClientAPI" />
        ///</param>
        public void SendLandObjectOwners(IClientAPI remote_client)
        {
            if (m_scene.Permissions.CanViewObjectOwners(remote_client.AgentId, this))
            {
                IPrimCountModule primCountModule = m_scene.RequestModuleInterface<IPrimCountModule>();
                if (primCountModule != null)
                {
                    IPrimCounts primCounts = primCountModule.GetPrimCounts(LandData.GlobalID);
                    Dictionary<UUID, LandObjectOwners> owners = new Dictionary<UUID, LandObjectOwners>();
                    foreach (ISceneEntity grp in primCounts.Objects)
                    {
                        bool newlyAdded = false;
                        LandObjectOwners landObj;
                        if (!owners.TryGetValue(grp.OwnerID, out landObj))
                        {
                            landObj = new LandObjectOwners();
                            owners.Add(grp.OwnerID, landObj);
                            newlyAdded = true;
                        }

                        landObj.Count += grp.PrimCount;
                        //Only do all of this once
                        if (newlyAdded)
                        {
                            if (grp.GroupID != UUID.Zero && grp.GroupID == grp.OwnerID)
                                landObj.GroupOwned = true;
                            else
                                landObj.GroupOwned = false;
                            if (landObj.GroupOwned)
                                landObj.Online = false;
                            else
                            {
                                IAgentInfoService presenceS = m_scene.RequestModuleInterface<IAgentInfoService>();
                                UserInfo info = presenceS.GetUserInfo(grp.OwnerID.ToString());
                                if (info != null)
                                    landObj.Online = info.IsOnline;
                            }
                            landObj.OwnerID = grp.OwnerID;
                        }
                        if (grp.RootChild.Rezzed > landObj.TimeLastRezzed)
                            landObj.TimeLastRezzed = grp.RootChild.Rezzed;
                    }
                    remote_client.SendLandObjectOwners(new List<LandObjectOwners>(owners.Values));
                }
            }
        }

        #endregion

        #region Object Returning

        public List<ISceneEntity> GetPrimsOverByOwner(UUID targetID, int flags)
        {
            List<ISceneEntity> prims = new List<ISceneEntity>();
            IPrimCountModule primCountModule = m_scene.RequestModuleInterface<IPrimCountModule>();
            IPrimCounts primCounts = primCountModule.GetPrimCounts(LandData.GlobalID);
#if (!ISWIN)
            foreach (ISceneEntity obj in primCounts.Objects)
            {
                if (obj.OwnerID == m_landData.OwnerID)
                {
                    if (flags == 4)
                    {
#if (!ISWIN)
                        bool containsScripts = false;
                        foreach (ISceneChildEntity child in obj.ChildrenEntities())
                        {
                            if (child.Inventory.ContainsScripts())
                            {
                                containsScripts = true;
                                break;
                            }
                        }
#else
                        bool containsScripts = obj.ChildrenEntities().Any(child => child.Inventory.ContainsScripts());
#endif
                        if (!containsScripts)
                            continue;
                    }
                    prims.Add(obj);
                }
            }
#else
            foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.OwnerID == m_landData.OwnerID))
            {
                if (flags == 4)
                {
                    bool containsScripts = obj.ChildrenEntities().Any(child => child.Inventory.ContainsScripts());
                    if (!containsScripts)
                        continue;
                }
                prims.Add(obj);
            }
#endif
            return prims;
        }

        public void ReturnLandObjects(uint type, UUID[] owners, UUID[] tasks, IClientAPI remote_client)
        {
            Dictionary<UUID, List<ISceneEntity>> returns =
                new Dictionary<UUID, List<ISceneEntity>>();

            IPrimCountModule primCountModule = m_scene.RequestModuleInterface<IPrimCountModule>();
            IPrimCounts primCounts = primCountModule.GetPrimCounts(LandData.GlobalID);
            if (type == (uint) ObjectReturnType.Owner)
            {
#if (!ISWIN)
                foreach (ISceneEntity obj in primCounts.Objects)
                {
                    if (obj.OwnerID == m_landData.OwnerID)
                    {
                        if (!returns.ContainsKey(obj.OwnerID))
                            returns[obj.OwnerID] = new List<ISceneEntity>();
                        if (!returns[obj.OwnerID].Contains(obj))
                            returns[obj.OwnerID].Add(obj);
                    }
                }
#else
                foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.OwnerID == m_landData.OwnerID))
                {
                    if (!returns.ContainsKey(obj.OwnerID))
                        returns[obj.OwnerID] =
                            new List<ISceneEntity>();
                    if (!returns[obj.OwnerID].Contains(obj))
                        returns[obj.OwnerID].Add(obj);
                }
#endif
            }
            else if (type == (uint) ObjectReturnType.Group && m_landData.GroupID != UUID.Zero)
            {
#if (!ISWIN)
                foreach (ISceneEntity obj in primCounts.Objects)
                {
                    if (obj.GroupID == m_landData.GroupID)
                    {
                        if (!returns.ContainsKey(obj.OwnerID))
                            returns[obj.OwnerID] = new List<ISceneEntity>();
                        if (!returns[obj.OwnerID].Contains(obj))
                            returns[obj.OwnerID].Add(obj);
                    }
                }
#else
                foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.GroupID == m_landData.GroupID))
                {
                    if (!returns.ContainsKey(obj.OwnerID))
                        returns[obj.OwnerID] =
                            new List<ISceneEntity>();
                    if (!returns[obj.OwnerID].Contains(obj))
                        returns[obj.OwnerID].Add(obj);
                }
#endif
            }
            else if (type == (uint) ObjectReturnType.Other)
            {
#if (!ISWIN)
                foreach (ISceneEntity obj in primCounts.Objects)
                {
                    if (obj.OwnerID != m_landData.OwnerID && (obj.GroupID != m_landData.GroupID || m_landData.GroupID == UUID.Zero))
                    {
                        if (!returns.ContainsKey(obj.OwnerID))
                            returns[obj.OwnerID] = new List<ISceneEntity>();
                        if (!returns[obj.OwnerID].Contains(obj))
                            returns[obj.OwnerID].Add(obj);
                    }
                }
#else
                foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.OwnerID != m_landData.OwnerID &&
                                                                             (obj.GroupID != m_landData.GroupID ||
                                                                              m_landData.GroupID == UUID.Zero)))
                {
                    if (!returns.ContainsKey(obj.OwnerID))
                        returns[obj.OwnerID] =
                            new List<ISceneEntity>();
                    if (!returns[obj.OwnerID].Contains(obj))
                        returns[obj.OwnerID].Add(obj);
                }
#endif
            }
            else if (type == (uint) ObjectReturnType.List)
            {
                List<UUID> ownerlist = new List<UUID>(owners);

#if (!ISWIN)
                foreach (ISceneEntity obj in primCounts.Objects)
                {
                    if (ownerlist.Contains(obj.OwnerID))
                    {
                        if (!returns.ContainsKey(obj.OwnerID))
                            returns[obj.OwnerID] = new List<ISceneEntity>();
                        if (!returns[obj.OwnerID].Contains(obj))
                            returns[obj.OwnerID].Add(obj);
                    }
                }
#else
                foreach (ISceneEntity obj in primCounts.Objects.Where(obj => ownerlist.Contains(obj.OwnerID)))
                {
                    if (!returns.ContainsKey(obj.OwnerID))
                        returns[obj.OwnerID] =
                            new List<ISceneEntity>();
                    if (!returns[obj.OwnerID].Contains(obj))
                        returns[obj.OwnerID].Add(obj);
                }
#endif
            }
            else if (type == 1)
            {
                List<UUID> Tasks = new List<UUID>(tasks);
#if (!ISWIN)
                foreach (ISceneEntity obj in primCounts.Objects)
                {
                    if (Tasks.Contains(obj.UUID))
                    {
                        if (!returns.ContainsKey(obj.OwnerID))
                            returns[obj.OwnerID] = new List<ISceneEntity>();
                        if (!returns[obj.OwnerID].Contains(obj))
                            returns[obj.OwnerID].Add(obj);
                    }
                }
#else
                foreach (ISceneEntity obj in primCounts.Objects.Where(obj => Tasks.Contains(obj.UUID)))
                {
                    if (!returns.ContainsKey(obj.OwnerID))
                        returns[obj.OwnerID] =
                            new List<ISceneEntity>();
                    if (!returns[obj.OwnerID].Contains(obj))
                        returns[obj.OwnerID].Add(obj);
                }
#endif
            }

#if (!ISWIN)
            foreach (List<ISceneEntity> ol in returns.Values)
            {
                if (m_scene.Permissions.CanReturnObjects(this, remote_client.AgentId, ol))
                {
                    //The return system will take care of the returned objects
                    m_parcelManagementModule.AddReturns(ol[0].OwnerID, ol[0].Name, ol[0].AbsolutePosition, "Parcel Owner Return", ol);
                    //m_scene.returnObjects(ol.ToArray(), remote_client.AgentId);
                }
            }
#else
            foreach (List<ISceneEntity> ol in returns.Values.Where(ol => m_scene.Permissions.CanReturnObjects(this, remote_client.AgentId, ol)))
            {
                //The return system will take care of the returned objects
                m_parcelManagementModule.AddReturns(ol[0].OwnerID, ol[0].Name, ol[0].AbsolutePosition,
                                                    "Parcel Owner Return", ol);
                //m_scene.returnObjects(ol.ToArray(), remote_client.AgentId);
            }
#endif
        }

        public void DisableLandObjects(uint type, UUID[] owners, UUID[] tasks, IClientAPI remote_client)
        {
            Dictionary<UUID, List<ISceneEntity>> disabled =
                new Dictionary<UUID, List<ISceneEntity>>();

            IPrimCountModule primCountModule = m_scene.RequestModuleInterface<IPrimCountModule>();
            IPrimCounts primCounts = primCountModule.GetPrimCounts(LandData.GlobalID);
            if (type == (uint) ObjectReturnType.Owner)
            {
#if (!ISWIN)
                foreach (ISceneEntity obj in primCounts.Objects)
                {
                    if (obj.OwnerID == m_landData.OwnerID)
                    {
                        if (!disabled.ContainsKey(obj.OwnerID))
                            disabled[obj.OwnerID] = new List<ISceneEntity>();

                        disabled[obj.OwnerID].Add(obj);
                    }
                }
#else
                foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.OwnerID == m_landData.OwnerID))
                {
                    if (!disabled.ContainsKey(obj.OwnerID))
                        disabled[obj.OwnerID] =
                            new List<ISceneEntity>();

                    disabled[obj.OwnerID].Add(obj);
                }
#endif
            }
            else if (type == (uint) ObjectReturnType.Group && m_landData.GroupID != UUID.Zero)
            {
#if (!ISWIN)
                foreach (ISceneEntity obj in primCounts.Objects)
                {
                    if (obj.GroupID == m_landData.GroupID)
                    {
                        if (!disabled.ContainsKey(obj.OwnerID))
                            disabled[obj.OwnerID] = new List<ISceneEntity>();

                        disabled[obj.OwnerID].Add(obj);
                    }
                }
#else
                foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.GroupID == m_landData.GroupID))
                {
                    if (!disabled.ContainsKey(obj.OwnerID))
                        disabled[obj.OwnerID] =
                            new List<ISceneEntity>();

                    disabled[obj.OwnerID].Add(obj);
                }
#endif
            }
            else if (type == (uint) ObjectReturnType.Other)
            {
#if (!ISWIN)
                foreach (ISceneEntity obj in primCounts.Objects)
                {
                    if (obj.OwnerID != m_landData.OwnerID && (obj.GroupID != m_landData.GroupID || m_landData.GroupID == UUID.Zero))
                    {
                        if (!disabled.ContainsKey(obj.OwnerID))
                            disabled[obj.OwnerID] = new List<ISceneEntity>();
                        disabled[obj.OwnerID].Add(obj);
                    }
                }
#else
                foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.OwnerID != m_landData.OwnerID &&
                                                                             (obj.GroupID != m_landData.GroupID ||
                                                                              m_landData.GroupID == UUID.Zero)))
                {
                    if (!disabled.ContainsKey(obj.OwnerID))
                        disabled[obj.OwnerID] =
                            new List<ISceneEntity>();
                    disabled[obj.OwnerID].Add(obj);
                }
#endif
            }
            else if (type == (uint) ObjectReturnType.List)
            {
                List<UUID> ownerlist = new List<UUID>(owners);

#if (!ISWIN)
                foreach (ISceneEntity obj in primCounts.Objects)
                {
                    if (ownerlist.Contains(obj.OwnerID))
                    {
                        if (!disabled.ContainsKey(obj.OwnerID))
                            disabled[obj.OwnerID] = new List<ISceneEntity>();
                        disabled[obj.OwnerID].Add(obj);
                    }
                }
#else
                foreach (ISceneEntity obj in primCounts.Objects.Where(obj => ownerlist.Contains(obj.OwnerID)))
                {
                    if (!disabled.ContainsKey(obj.OwnerID))
                        disabled[obj.OwnerID] =
                            new List<ISceneEntity>();
                    disabled[obj.OwnerID].Add(obj);
                }
#endif
            }
            else if (type == 1)
            {
                List<UUID> Tasks = new List<UUID>(tasks);
#if (!ISWIN)
                foreach (ISceneEntity obj in primCounts.Objects)
                {
                    if (Tasks.Contains(obj.UUID))
                    {
                        if (!disabled.ContainsKey(obj.OwnerID))
                            disabled[obj.OwnerID] = new List<ISceneEntity>();
                        disabled[obj.OwnerID].Add(obj);
                    }
                }
#else
                foreach (ISceneEntity obj in primCounts.Objects.Where(obj => Tasks.Contains(obj.UUID)))
                {
                    if (!disabled.ContainsKey(obj.OwnerID))
                        disabled[obj.OwnerID] =
                            new List<ISceneEntity>();
                    disabled[obj.OwnerID].Add(obj);
                }
#endif
            }

            IScriptModule[] modules = m_scene.RequestModuleInterfaces<IScriptModule>();
            foreach (List<ISceneEntity> ol in disabled.Values)
            {
                foreach (ISceneEntity group in ol)
                {
                    if (m_scene.Permissions.CanEditObject(group.UUID, remote_client.AgentId))
                    {
                        foreach (IScriptModule module in modules)
                        {
                            //Disable the entire object
                            foreach (ISceneChildEntity part in group.ChildrenEntities())
                            {
                                foreach (TaskInventoryItem item in part.Inventory.GetInventoryItems())
                                {
                                    if (item.InvType == (int) InventoryType.LSL)
                                    {
                                        module.SuspendScript(item.ItemID);
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