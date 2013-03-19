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
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;

namespace Aurora.Framework
{
    /// <summary>
    ///     Details of a Parcel of land
    /// </summary>
    [Serializable, ProtoContract(UseProtoMembersOnly = false)]
    public class LandData : IDataTransferable
    {
        private Vector3 _AABBMax;
        private Vector3 _AABBMin;
        private int _Maturity;
        private float _MediaLoopSet;
        private int _area;
        private uint _auctionID;
        private UUID _authBuyerID = UUID.Zero;
        private byte[] _bitmap = new byte[512];
        private ParcelCategory _category = ParcelCategory.None;
        private bool _firstParty = false;
        private int _claimDate;
        private int _claimPrice;
        private string _description = String.Empty;
        private int _dwell;

        private uint _flags = (uint) ParcelFlags.AllowFly | (uint) ParcelFlags.AllowLandmark |
                              (uint) ParcelFlags.AllowAPrimitiveEntry |
                              (uint) ParcelFlags.AllowDeedToGroup | (uint) ParcelFlags.AllowTerraform |
                              (uint) ParcelFlags.CreateObjects | (uint) ParcelFlags.AllowOtherScripts |
                              (uint) ParcelFlags.SoundLocal | (uint) ParcelFlags.AllowVoiceChat;

        private UUID _globalID = UUID.Zero;
        private UUID _groupID = UUID.Zero;
        protected UUID _infoUUID;
        private bool _isGroupOwned;

        private byte _landingType = 2;
        private int _localID;
        private byte _mediaAutoScale;
        private string _mediaDescription = "";
        private int _mediaHeight;
        private UUID _mediaID = UUID.Zero;
        private bool _mediaLoop;
        private string _mediaType = "none/none";

        private string _mediaURL = String.Empty;
        private int _mediaWidth;
        private string _musicURL = String.Empty;
        private string _name = "Your Parcel";
        private bool _obscureMedia;
        private bool _obscureMusic;
        private int _otherCleanTime;
        private UUID _ownerID = UUID.Zero;
        private List<ParcelManager.ParcelAccessEntry> _parcelAccessList = new List<ParcelManager.ParcelAccessEntry>();
        private float _passHours;
        private int _passPrice;
        private bool _private;
        private ulong _regionHandle;
        private UUID _regionID;
        private UUID _scopeID;
        private int _salePrice;
        private UUID _snapshotID = UUID.Zero;
        private ParcelStatus _status = ParcelStatus.Leased;
        private Vector3 _userLocation;
        private Vector3 _userLookAt;
        private AuctionInfo m_AuctionInfo = new AuctionInfo();

        #region constructor

        public LandData()
        {
            _globalID = UUID.Random();
        }

        public LandData(OSDMap map)
        {
            FromOSD(map);
        }

        #endregion

        #region properties

        /// <summary>
        ///     Whether to obscure parcel media URL
        /// </summary>
        [ProtoMember(1)]
        public bool ObscureMedia
        {
            get { return _obscureMedia; }
            set { _obscureMedia = value; }
        }

        /// <summary>
        ///     Whether to obscure parcel music URL
        /// </summary>
        [ProtoMember(2)]
        public bool ObscureMusic
        {
            get { return _obscureMusic; }
            set { _obscureMusic = value; }
        }

        /// <summary>
        ///     Whether to loop parcel media
        /// </summary>
        [ProtoMember(3)]
        public bool MediaLoop
        {
            get { return _mediaLoop; }
            set { _mediaLoop = value; }
        }

        /// <summary>
        ///     Height of parcel media render
        /// </summary>
        [ProtoMember(4)]
        public int MediaHeight
        {
            get { return _mediaHeight; }
            set { _mediaHeight = value; }
        }

        [ProtoMember(5)]
        public float MediaLoopSet
        {
            get { return _MediaLoopSet; }
            set { _MediaLoopSet = value; }
        }

        /// <summary>
        ///     Width of parcel media render
        /// </summary>
        [ProtoMember(6)]
        public int MediaWidth
        {
            get { return _mediaWidth; }
            set { _mediaWidth = value; }
        }

        /// <summary>
        ///     Upper corner of the AABB for the parcel
        /// </summary>
        [ProtoMember(7)]
        public Vector3 AABBMax
        {
            get { return _AABBMax; }
            set { _AABBMax = value; }
        }

        /// <summary>
        ///     Lower corner of the AABB for the parcel
        /// </summary>
        [ProtoMember(8)]
        public Vector3 AABBMin
        {
            get { return _AABBMin; }
            set { _AABBMin = value; }
        }

        /// <summary>
        ///     Area in meters^2 the parcel contains
        /// </summary>
        [ProtoMember(9)]
        public int Area
        {
            get { return _area; }
            set { _area = value; }
        }

        /// <summary>
        ///     ID of auction (3rd Party Integration) when parcel is being auctioned
        /// </summary>
        [ProtoMember(10)]
        public uint AuctionID
        {
            get { return _auctionID; }
            set { _auctionID = value; }
        }

        /// <summary>
        ///     UUID of authorized buyer of parcel.  This is UUID.Zero if anyone can buy it.
        /// </summary>
        [ProtoMember(11)]
        public UUID AuthBuyerID
        {
            get { return _authBuyerID; }
            set { _authBuyerID = value; }
        }

        /// <summary>
        ///     Category of parcel.  Used for classifying the parcel in classified listings
        /// </summary>
        [ProtoMember(12)]
        public ParcelCategory Category
        {
            get { return _category; }
            set { _category = value; }
        }

        [ProtoMember(13)]
        public bool FirstParty
        {
            get { return _firstParty; }
            set { _firstParty = value; }
        }

        /// <summary>
        ///     Date that the current owner purchased or claimed the parcel
        /// </summary>
        [ProtoMember(14)]
        public int ClaimDate
        {
            get { return _claimDate; }
            set { _claimDate = value; }
        }

        /// <summary>
        ///     The last price that the parcel was sold at
        /// </summary>
        [ProtoMember(15)]
        public int ClaimPrice
        {
            get { return _claimPrice; }
            set { _claimPrice = value; }
        }

        /// <summary>
        ///     Global ID for the parcel.  (3rd Party Integration)
        /// </summary>
        [ProtoMember(16)]
        public UUID GlobalID
        {
            get { return _globalID; }
            set { _globalID = value; }
        }

        /// <summary>
        ///     Grid Wide ID for the parcel.
        /// </summary>
        [ProtoMember(17)]
        public UUID InfoUUID
        {
            get { return _infoUUID; }
            set { _infoUUID = value; }
        }

        /// <summary>
        ///     Unique ID of the Group that owns
        /// </summary>
        [ProtoMember(18)]
        public UUID GroupID
        {
            get { return _groupID; }
            set { _groupID = value; }
        }

        /// <summary>
        ///     Returns true if the Land Parcel is owned by a group
        /// </summary>
        [ProtoMember(19)]
        public bool IsGroupOwned
        {
            get { return _isGroupOwned; }
            set { _isGroupOwned = value; }
        }

        /// <summary>
        ///     jp2 data for the image representative of the parcel in the parcel dialog
        /// </summary>
        [ProtoMember(20, OverwriteList = true)]
        public byte[] Bitmap
        {
            get { return _bitmap; }
            set { _bitmap = value; }
        }

        /// <summary>
        ///     Parcel Description
        /// </summary>
        [ProtoMember(21)]
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        /// <summary>
        ///     Parcel settings.  Access flags, Fly, NoPush, Voice, Scripts allowed, etc.  ParcelFlags
        /// </summary>
        [ProtoMember(22)]
        public uint Flags
        {
            get { return _flags; } //Force add allow voice chat
            set { _flags = value; }
        }

        /// <summary>
        ///     Determines if people are able to teleport where they please on the parcel or if they
        ///     get constrainted to a specific point on teleport within the parcel
        /// </summary>
        [ProtoMember(23)]
        public byte LandingType
        {
            get { return _landingType; }
            set { _landingType = value; }
        }

        [ProtoMember(24)]
        public int Maturity
        {
            get { return _Maturity; }
            set { _Maturity = value; }
        }

        [ProtoMember(25)]
        public int Dwell
        {
            get { return _dwell; }
            set { _dwell = value; }
        }

        /// <summary>
        ///     Parcel Name
        /// </summary>
        [ProtoMember(26)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        ///     Status of Parcel, Leased, Abandoned, For Sale
        /// </summary>
        [ProtoMember(27)]
        public ParcelStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        /// <summary>
        ///     Internal ID of the parcel.  Sometimes the client will try to use this value
        /// </summary>
        [ProtoMember(28)]
        public int LocalID
        {
            get { return _localID; }
            set { _localID = value; }
        }

        [ProtoMember(29)]
        public ulong RegionHandle
        {
            get { return _regionHandle; }
            set { _regionHandle = value; }
        }

        [ProtoMember(30)]
        public AuctionInfo AuctionInfo
        {
            get { return m_AuctionInfo; }
            set { m_AuctionInfo = value; }
        }

        [ProtoMember(31)]
        public UUID RegionID
        {
            get { return _regionID; }
            set { _regionID = value; }
        }

        [ProtoMember(32)]
        public UUID ScopeID
        {
            get { return _scopeID; }
            set { _scopeID = value; }
        }

        /// <summary>
        ///     Determines if we scale the media based on the surface it's on
        /// </summary>
        [ProtoMember(33)]
        public byte MediaAutoScale
        {
            get { return _mediaAutoScale; }
            set { _mediaAutoScale = value; }
        }

        /// <summary>
        ///     Texture Guid to replace with the output of the media stream
        /// </summary>
        [ProtoMember(34)]
        public UUID MediaID
        {
            get { return _mediaID; }
            set { _mediaID = value; }
        }

        /// <summary>
        ///     URL to the media file to display
        /// </summary>
        [ProtoMember(35)]
        public string MediaURL
        {
            get { return _mediaURL; }
            set { _mediaURL = value; }
        }

        [ProtoMember(36)]
        public string MediaType
        {
            get { return _mediaType; }
            set { _mediaType = value; }
        }

        /// <summary>
        ///     URL to the shoutcast music stream to play on the parcel
        /// </summary>
        [ProtoMember(37)]
        public string MusicURL
        {
            get { return _musicURL; }
            set { _musicURL = value; }
        }

        /// <summary>
        ///     Owner Avatar or Group of the parcel.  Naturally, all land masses must be
        ///     owned by someone
        /// </summary>
        [ProtoMember(38)]
        public UUID OwnerID
        {
            get { return _ownerID; }
            set { _ownerID = value; }
        }

        /// <summary>
        ///     List of access data for the parcel.  User data, some bitflags, and a time
        /// </summary>
        [ProtoMember(39)]
        public List<ParcelManager.ParcelAccessEntry> ParcelAccessList
        {
            get { return _parcelAccessList; }
            set { _parcelAccessList = value; }
        }

        /// <summary>
        ///     How long in hours a Pass to the parcel is given
        /// </summary>
        [ProtoMember(40)]
        public float PassHours
        {
            get { return _passHours; }
            set { _passHours = value; }
        }

        /// <summary>
        ///     Price to purchase a Pass to a restricted parcel
        /// </summary>
        [ProtoMember(41)]
        public int PassPrice
        {
            get { return _passPrice; }
            set { _passPrice = value; }
        }

        /// <summary>
        ///     When the parcel is being sold, this is the price to purchase the parcel
        /// </summary>
        [ProtoMember(42)]
        public int SalePrice
        {
            get { return _salePrice; }
            set { _salePrice = value; }
        }

        /// <summary>
        ///     Number of meters^2 in the Simulator
        /// </summary>
        [ProtoMember(43)]
        public int SimwideArea { get; set; }

        /// <summary>
        ///     ID of the snapshot used in the client parcel dialog of the parcel
        /// </summary>
        [ProtoMember(44)]
        public UUID SnapshotID
        {
            get { return _snapshotID; }
            set { _snapshotID = value; }
        }

        /// <summary>
        ///     When teleporting is restricted to a certain point, this is the location
        ///     that the user will be redirected to
        /// </summary>
        [ProtoMember(45)]
        public Vector3 UserLocation
        {
            get { return _userLocation; }
            set { _userLocation = value; }
        }

        /// <summary>
        ///     When teleporting is restricted to a certain point, this is the rotation
        ///     that the user will be positioned
        /// </summary>
        [ProtoMember(46)]
        public Vector3 UserLookAt
        {
            get { return _userLookAt; }
            set { _userLookAt = value; }
        }

        /// <summary>
        ///     Number of minutes to return SceneObjectGroup that are owned by someone who doesn't own
        ///     the parcel and isn't set to the same 'group' as the parcel.
        /// </summary>
        [ProtoMember(47)]
        public int OtherCleanTime
        {
            get { return _otherCleanTime; }
            set { _otherCleanTime = value; }
        }

        /// <summary>
        ///     parcel media description
        /// </summary>
        [ProtoMember(48)]
        public string MediaDescription
        {
            get { return _mediaDescription; }
            set { _mediaDescription = value; }
        }

        [ProtoMember(49)]
        public bool Private
        {
            get { return _private; }
            set { _private = value; }
        }

        #endregion

        /// <summary>
        ///     Make a new copy of the land data
        /// </summary>
        /// <returns></returns>
        public LandData Copy()
        {
            LandData landData = new LandData
                                    {
                                        _AABBMax = _AABBMax,
                                        _AABBMin = _AABBMin,
                                        _area = _area,
                                        _auctionID = _auctionID,
                                        _authBuyerID = _authBuyerID,
                                        _category = _category,
                                        _claimDate = _claimDate,
                                        _claimPrice = _claimPrice,
                                        _globalID = _globalID,
                                        _groupID = _groupID,
                                        _isGroupOwned = _isGroupOwned,
                                        _localID = _localID,
                                        _landingType = _landingType,
                                        _mediaAutoScale = _mediaAutoScale,
                                        _mediaID = _mediaID,
                                        _mediaURL = _mediaURL,
                                        _musicURL = _musicURL,
                                        _ownerID = _ownerID,
                                        _bitmap = (byte[]) _bitmap.Clone(),
                                        _description = _description,
                                        _flags = _flags,
                                        _name = _name,
                                        _status = _status,
                                        _passHours = _passHours,
                                        _passPrice = _passPrice,
                                        _salePrice = _salePrice,
                                        _snapshotID = _snapshotID,
                                        _userLocation = _userLocation,
                                        _userLookAt = _userLookAt,
                                        _otherCleanTime = _otherCleanTime,
                                        _dwell = _dwell,
                                        _mediaType = _mediaType,
                                        _mediaDescription = _mediaDescription,
                                        _mediaWidth = _mediaWidth,
                                        _mediaHeight = _mediaHeight,
                                        _mediaLoop = _mediaLoop,
                                        _MediaLoopSet = _MediaLoopSet,
                                        _obscureMusic = _obscureMusic,
                                        _obscureMedia = _obscureMedia,
                                        _regionID = _regionID,
                                        _regionHandle = _regionHandle,
                                        _infoUUID = _infoUUID,
                                        _Maturity = _Maturity,
                                        _private = _private
                                    };


            landData._parcelAccessList.Clear();
#if (!ISWIN)
            foreach (ParcelManager.ParcelAccessEntry entry in _parcelAccessList)
            {
                ParcelManager.ParcelAccessEntry newEntry = new ParcelManager.ParcelAccessEntry
                                                               {
                                                                   AgentID = entry.AgentID, Flags = entry.Flags, Time = entry.Time
                                                               };
                landData._parcelAccessList.Add(newEntry);
            }
#else
            foreach (
                ParcelManager.ParcelAccessEntry newEntry in
                    _parcelAccessList.Select(entry => new ParcelManager.ParcelAccessEntry
                                                          {
                                                              AgentID = entry.AgentID,
                                                              Flags = entry.Flags,
                                                              Time = entry.Time
                                                          }))
            {
                landData._parcelAccessList.Add(newEntry);
            }
#endif

            return landData;
        }

        #region IDataTransferable

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["GroupID"] = OSD.FromUUID(GroupID);
            map["IsGroupOwned"] = OSD.FromBoolean(IsGroupOwned);
            map["OwnerID"] = OSD.FromUUID(OwnerID);
            map["Maturity"] = OSD.FromInteger(Maturity);
            map["Area"] = OSD.FromInteger(Area);
            map["AuctionID"] = OSD.FromUInteger(AuctionID);
            map["SalePrice"] = OSD.FromInteger(SalePrice);
            map["InfoUUID"] = OSD.FromUUID(InfoUUID);
            map["Dwell"] = OSD.FromInteger(Dwell);
            map["Flags"] = OSD.FromInteger((int) Flags);
            map["Name"] = OSD.FromString(Name);
            map["Description"] = OSD.FromString(Description);
            map["UserLocation"] = OSD.FromVector3(UserLocation);
            map["LocalID"] = OSD.FromInteger(LocalID);
            map["GlobalID"] = OSD.FromUUID(GlobalID);
            map["RegionID"] = OSD.FromUUID(RegionID);
            map["ScopeID"] = OSD.FromUUID(ScopeID);
            map["MediaDescription"] = OSD.FromString(MediaDescription);
            map["MediaWidth"] = OSD.FromInteger(MediaWidth);
            map["MediaHeight"] = OSD.FromInteger(MediaHeight);
            map["MediaLoop"] = OSD.FromBoolean(MediaLoop);
            map["MediaType"] = OSD.FromString(MediaType);
            map["ObscureMedia"] = OSD.FromBoolean(ObscureMedia);
            map["ObscureMusic"] = OSD.FromBoolean(ObscureMusic);
            map["SnapshotID"] = OSD.FromUUID(SnapshotID);
            map["MediaAutoScale"] = OSD.FromInteger(MediaAutoScale);
            map["MediaLoopSet"] = OSD.FromReal(MediaLoopSet);
            map["MediaURL"] = OSD.FromString(MediaURL);
            map["MusicURL"] = OSD.FromString(MusicURL);
            map["Bitmap"] = OSD.FromBinary(Bitmap);
            map["Category"] = OSD.FromInteger((int) Category);
            map["FirstParty"] = OSD.FromBoolean(FirstParty);
            map["ClaimDate"] = OSD.FromInteger(ClaimDate);
            map["ClaimPrice"] = OSD.FromInteger(ClaimPrice);
            map["Status"] = OSD.FromInteger((int) Status);
            map["LandingType"] = OSD.FromInteger(LandingType);
            map["PassHours"] = OSD.FromReal(PassHours);
            map["PassPrice"] = OSD.FromInteger(PassPrice);
            map["UserLookAt"] = OSD.FromVector3(UserLookAt);
            map["AuthBuyerID"] = OSD.FromUUID(AuthBuyerID);
            map["OtherCleanTime"] = OSD.FromInteger(OtherCleanTime);
            map["RegionHandle"] = OSD.FromULong(RegionHandle);
            map["Private"] = OSD.FromBoolean(Private);
            map["AuctionInfo"] = AuctionInfo.ToOSD();
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            RegionID = map["RegionID"].AsUUID();
            ScopeID = map["ScopeID"].AsUUID();
            GlobalID = map["GlobalID"].AsUUID();
            LocalID = map["LocalID"].AsInteger();
            SalePrice = map["SalePrice"].AsInteger();
            UserLocation = map["UserLocation"].AsVector3();
            UserLookAt = map["UserLookAt"].AsVector3();
            Name = map["Name"].AsString();
            Description = map["Description"].AsString();
            Flags = (uint) map["Flags"].AsInteger();
            Dwell = map["Dwell"].AsInteger();
            InfoUUID = map["InfoUUID"].AsUUID();
            AuctionID = map["AuctionID"].AsUInteger();
            Area = map["Area"].AsInteger();
            Maturity = map["Maturity"].AsInteger();
            OwnerID = map["OwnerID"].AsUUID();
            GroupID = map["GroupID"].AsUUID();
            IsGroupOwned = (GroupID != UUID.Zero);
            SnapshotID = map["SnapshotID"].AsUUID();
            MediaDescription = map["MediaDescription"].AsString();
            MediaWidth = map["MediaWidth"].AsInteger();
            MediaHeight = map["MediaHeight"].AsInteger();
            MediaLoop = map["MediaLoop"].AsBoolean();
            MediaType = map["MediaType"].AsString();
            ObscureMedia = map["ObscureMedia"].AsBoolean();
            ObscureMusic = map["ObscureMusic"].AsBoolean();
            MediaLoopSet = (float) map["MediaLoopSet"].AsReal();
            MediaAutoScale = (byte) map["MediaAutoScale"].AsInteger();
            MediaURL = map["MediaURL"].AsString();
            MusicURL = map["MusicURL"].AsString();
            Bitmap = map["Bitmap"].AsBinary();
            Category = (ParcelCategory) map["Category"].AsInteger();
            FirstParty = map["FirstParty"].AsBoolean();
            ClaimDate = map["ClaimDate"].AsInteger();
            ClaimPrice = map["ClaimPrice"].AsInteger();
            Status = (ParcelStatus) map["Status"].AsInteger();
            LandingType = (byte) map["LandingType"].AsInteger();
            PassHours = (float) map["PassHours"].AsReal();
            PassPrice = map["PassPrice"].AsInteger();
            AuthBuyerID = map["AuthBuyerID"].AsUUID();
            OtherCleanTime = map["OtherCleanTime"].AsInteger();
            RegionHandle = map["RegionHandle"].AsULong();
            Private = map["Private"].AsBoolean();
            AuctionInfo = new AuctionInfo();
            if (map.ContainsKey("AuctionInfo"))
                AuctionInfo.FromOSD((OSDMap) map["AuctionInfo"]);

            if ((IsGroupOwned) && (GroupID != OwnerID)) OwnerID = GroupID;
        }

        #endregion
    }
}