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
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

using OpenMetaverse;
using OpenMetaverse.StructuredData;

using Aurora.Framework;

namespace OpenSim.Framework
{
    /// <summary>
    /// Details of a Parcel of land
    /// </summary>
    public class LandData : IDataTransferable
    {
        // use only one serializer to give the runtime a chance to
        // optimize it (it won't do that if you use a new instance
        // every time)
        private static XmlSerializer serializer = new XmlSerializer(typeof (LandData));
        
        private Vector3 _AABBMax = new Vector3();
        private Vector3 _AABBMin = new Vector3();
        private int _area = 0;
        private uint _auctionID = 0; //Unemplemented. If set to 0, not being auctioned
        private UUID _authBuyerID = UUID.Zero; //Unemplemented. Authorized Buyer's UUID
        private ParcelCategory _category = ParcelCategory.None; //Unemplemented. Parcel's chosen category
        private int _claimDate = 0;
        private int _claimPrice = 0; //Unemplemented
        private UUID _globalID = UUID.Zero;
        private UUID _groupID = UUID.Zero;
        private int _groupPrims = 0;
        private bool _isGroupOwned = false;
        private byte[] _bitmap = new byte[512];
        private string _description = String.Empty;


        private uint _flags = (uint) ParcelFlags.AllowFly | (uint) ParcelFlags.AllowLandmark |
                                (uint) ParcelFlags.AllowAPrimitiveEntry |
                                (uint) ParcelFlags.AllowDeedToGroup | (uint) ParcelFlags.AllowTerraform |
                                (uint) ParcelFlags.CreateObjects | (uint) ParcelFlags.AllowOtherScripts |
                                (uint) ParcelFlags.SoundLocal | (uint)ParcelFlags.AllowVoiceChat;

        private byte _landingType = 0;
        private string _name = "Your Parcel";
        private ParcelStatus _status = ParcelStatus.Leased;
        private int _localID = 0;
        private byte _mediaAutoScale = 0;
        private UUID _mediaID = UUID.Zero;

        private string _mediaURL = String.Empty;
        private string _musicURL = String.Empty;
        private int _otherPrims = 0;
        private UUID _ownerID = UUID.Zero;
        private int _ownerPrims = 0;
        private List<ParcelManager.ParcelAccessEntry> _parcelAccessList = new List<ParcelManager.ParcelAccessEntry>();
        private float _passHours = 0;
        private int _passPrice = 0;
        private int _salePrice = 0;
        private int _selectedPrims = 0;
        private int _simwideArea = 0;
        private int _simwidePrims = 0;
        private UUID _snapshotID = UUID.Zero;
        private Vector3 _userLocation = new Vector3();
        private Vector3 _userLookAt = new Vector3();
        private int _otherCleanTime = 0;
        private ulong _regionHandle;
		private UUID _regionID;
        private string _mediaType = "none/none";
        private string _mediaDescription = "";
        private int _mediaHeight = 0;
        private int _mediaWidth = 0;
        private float _MediaLoopSet = 0;
        private bool _mediaLoop = false;
        private bool _obscureMusic = false;
        private bool _obscureMedia = false;
        private int _dwell = 0;

        /// <summary>
        /// Whether to obscure parcel media URL
        /// </summary>
        [XmlIgnore]
        public bool ObscureMedia {
            get {
                return _obscureMedia;
            }
            set {
                _obscureMedia = value;
            }
        }

        /// <summary>
        /// Whether to obscure parcel music URL
        /// </summary>
        [XmlIgnore]
        public bool ObscureMusic {
            get {
                return _obscureMusic;
            }
            set {
                _obscureMusic = value;
            }
        }

        /// <summary>
        /// Whether to loop parcel media
        /// </summary>
        [XmlIgnore]
        public bool MediaLoop {
            get {
                return _mediaLoop;
            }
            set {
                _mediaLoop = value;
            }
        }

        /// <summary>
        /// Height of parcel media render
        /// </summary>
        [XmlIgnore]
        public int MediaHeight {
            get {
                return _mediaHeight;
            }
            set {
                _mediaHeight = value;
            }
        }

        public float MediaLoopSet
        {
            get
            {
                return _MediaLoopSet;
            }
            set
            {
                _MediaLoopSet = value;
            }
        }

        /// <summary>
        /// Width of parcel media render
        /// </summary>
        [XmlIgnore]
        public int MediaWidth {
            get {
                return _mediaWidth;
            }
            set {
                _mediaWidth = value;
            }
        }

        /// <summary>
        /// Upper corner of the AABB for the parcel
        /// </summary>
        [XmlIgnore]
        public Vector3 AABBMax {
            get {
                return _AABBMax;
            }
            set {
                _AABBMax = value;
            }
        }
        /// <summary>
        /// Lower corner of the AABB for the parcel
        /// </summary>
        [XmlIgnore]
        public Vector3 AABBMin {
            get {
                return _AABBMin;
            }
            set {
                _AABBMin = value;
            }
        }

        /// <summary>
        /// Area in meters^2 the parcel contains
        /// </summary>
        public int Area {
            get {
                return _area;
            }
            set {
                _area = value;
            }
        }

        /// <summary>
        /// ID of auction (3rd Party Integration) when parcel is being auctioned
        /// </summary>
        public uint AuctionID {
            get {
                return _auctionID;
            }
            set {
                _auctionID = value;
            }
        }

        /// <summary>
        /// UUID of authorized buyer of parcel.  This is UUID.Zero if anyone can buy it.
        /// </summary>
        public UUID AuthBuyerID {
            get {
                return _authBuyerID;
            }
            set {
                _authBuyerID = value;
            }
        }

        /// <summary>
        /// Category of parcel.  Used for classifying the parcel in classified listings
        /// </summary>
        public ParcelCategory Category {
            get {
                return _category;
            }
            set {
                _category = value;
            }
        }

        /// <summary>
        /// Date that the current owner purchased or claimed the parcel
        /// </summary>
        public int ClaimDate {
            get {
                return _claimDate;
            }
            set {
                _claimDate = value;
            }
        }

        /// <summary>
        /// The last price that the parcel was sold at
        /// </summary>
        public int ClaimPrice {
            get {
                return _claimPrice;
            }
            set {
                _claimPrice = value;
            }
        }

        /// <summary>
        /// Global ID for the parcel.  (3rd Party Integration)
        /// </summary>
        public UUID GlobalID {
            get {
                return _globalID;
            }
            set {
                _globalID = value;
            }
        }

        protected UUID _infoUUID;
        /// <summary>
        /// Grid Wide ID for the parcel.
        /// </summary>
        public UUID InfoUUID
        {
            get
            {
                return _infoUUID;
            }
            set
            {
                _infoUUID = value;
            }
        }

        /// <summary>
        /// Unique ID of the Group that owns
        /// </summary>
        public UUID GroupID {
            get {
                return _groupID;
            }
            set {
                _groupID = value;
            }
        }

        /// <summary>
        /// Number of SceneObjectPart that are owned by a Group
        /// </summary>
        [XmlIgnore]
        public int GroupPrims {
            get {
                return _groupPrims;
            }
            set {
                _groupPrims = value;
            }
        }

        /// <summary>
        /// Returns true if the Land Parcel is owned by a group
        /// </summary>
        public bool IsGroupOwned {
            get {
                return _isGroupOwned;
            }
            set {
                _isGroupOwned = value;
            }
        }

        /// <summary>
        /// jp2 data for the image representative of the parcel in the parcel dialog
        /// </summary>
        public byte[] Bitmap {
            get {
                return _bitmap;
            }
            set {
                _bitmap = value;
            }
        }

        /// <summary>
        /// Parcel Description
        /// </summary>
        public string Description {
            get {
                return _description;
            }
            set {
                _description = value;
            }
        }

        /// <summary>
        /// Parcel settings.  Access flags, Fly, NoPush, Voice, Scripts allowed, etc.  ParcelFlags
        /// </summary>
        public uint Flags {
            get {
                return _flags;
            }
            set {
                _flags = value;
            }
        }

        /// <summary>
        /// Determines if people are able to teleport where they please on the parcel or if they 
        /// get constrainted to a specific point on teleport within the parcel
        /// </summary>
        public byte LandingType {
            get {
                return _landingType;
            }
            set {
                _landingType = value;
            }
        }

        private int _Maturity = 2;
        public int Maturity
        {
            get
            {
                return _Maturity;
            }
            set
            {
                _Maturity = value;
            }
        }

        public int Dwell {
            get {
                return _dwell;
            }
            set {
                _dwell = value;
            }
        }

        /// <summary>
        /// Parcel Name
        /// </summary>
        public string Name {
            get {
                return _name;
            }
            set {
                _name = value;
            }
        }

        /// <summary>
        /// Status of Parcel, Leased, Abandoned, For Sale
        /// </summary>
        public ParcelStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
            }
        }

        /// <summary>
        /// Internal ID of the parcel.  Sometimes the client will try to use this value
        /// </summary>
        public int LocalID {
            get {
                return _localID;
            }
            set {
                _localID = value;
            }
        }

        public ulong RegionHandle
        {
            get
            {
                return _regionHandle;
            }
            set
            {
                _regionHandle = value;
            }
        }

        public string m_GenericData = "";

        public string GenericData
        {
            get
            {
                return m_GenericData;
            }
            set
            {
                m_GenericData = value;
            }
        }

        [XmlIgnore]
        public OSDMap GenericDataMap
        {
            get
            {
                OSD osd = OSDParser.DeserializeLLSDXml(m_GenericData);
                OSDMap map = new OSDMap();
                if (osd.Type == OSDType.Map)
                {
                    map = (OSDMap)osd;
                }
                return map;
            }
        }

        public void AddGenericData(string Key, object Value)
        {
            OSD osd = OSDParser.DeserializeLLSDXml(m_GenericData);
            OSDMap map = new OSDMap();
            if (osd.Type == OSDType.Map)
            {
                map = (OSDMap)osd;
            }
            if (Value is OSD)
            {
                map[Key] = Value as OSD;
            }
            else
                map[Key] = OSD.FromObject(Value);
            m_GenericData = OSDParser.SerializeLLSDXmlString(map);
        }


        public void RemoveGenericData(string Key)
        {
            OSD osd = OSDParser.DeserializeLLSDXml(m_GenericData);
            OSDMap map = new OSDMap();
            if (osd.Type == OSDType.Map)
            {
                map = (OSDMap)osd;
                map.Remove(Key);
                m_GenericData = OSDParser.SerializeLLSDXmlString(map);
            }
        }



        public UUID RegionID
        {
            get
            {
                return _regionID;
            }
            set
            {
                _regionID = value;
            }
        }

        /// <summary>
        /// Determines if we scale the media based on the surface it's on
        /// </summary>
        public byte MediaAutoScale {
            get {
                return _mediaAutoScale;
            }
            set {
                _mediaAutoScale = value;
            }
        }

        /// <summary>
        /// Texture Guid to replace with the output of the media stream
        /// </summary>
        public UUID MediaID {
            get {
                return _mediaID;
            }
            set {
                _mediaID = value;
            }
        }

        /// <summary>
        /// URL to the media file to display
        /// </summary>
        public string MediaURL {
            get {
                return _mediaURL;
            }
            set {
                _mediaURL = value;
            }
        }

        public string MediaType
        {
            get
            {
                return _mediaType;
            }
            set
            {
                _mediaType = value;
            }
        }

        /// <summary>
        /// URL to the shoutcast music stream to play on the parcel
        /// </summary>
        public string MusicURL {
            get {
                return _musicURL;
            }
            set {
                _musicURL = value;
            }
        }

        /// <summary>
        /// Number of SceneObjectPart that are owned by users who do not own the parcel
        /// and don't have the 'group.  These are elegable for AutoReturn collection
        /// </summary>
        [XmlIgnore]
        public int OtherPrims {
            get {
                return _otherPrims;
            }
            set {
                _otherPrims = value;
            }
        }

        /// <summary>
        /// Owner Avatar or Group of the parcel.  Naturally, all land masses must be
        /// owned by someone
        /// </summary>
        public UUID OwnerID {
            get {
                return _ownerID;
            }
            set {
                _ownerID = value;
            }
        }

        /// <summary>
        /// Number of SceneObjectPart that are owned by the owner of the parcel
        /// </summary>
        [XmlIgnore]
        public int OwnerPrims {
            get {
                return _ownerPrims;
            }
            set {
                _ownerPrims = value;
            }
        }

        /// <summary>
        /// List of access data for the parcel.  User data, some bitflags, and a time
        /// </summary>
        public List<ParcelManager.ParcelAccessEntry> ParcelAccessList {
            get {
                return _parcelAccessList;
            }
            set {
                _parcelAccessList = value;
            }
        }

        /// <summary>
        /// How long in hours a Pass to the parcel is given
        /// </summary>
        public float PassHours {
            get {
                return _passHours;
            }
            set {
                _passHours = value;
            }
        }

        /// <summary>
        /// Price to purchase a Pass to a restricted parcel
        /// </summary>
        public int PassPrice {
            get {
                return _passPrice;
            }
            set {
                _passPrice = value;
            }
        }

        /// <summary>
        /// When the parcel is being sold, this is the price to purchase the parcel
        /// </summary>
        public int SalePrice {
            get {
                return _salePrice;
            }
            set {
                _salePrice = value;
            }
        }

        /// <summary>
        /// Number of SceneObjectPart that are currently selected by avatar
        /// </summary>
        [XmlIgnore]
        public int SelectedPrims {
            get {
                return _selectedPrims;
            }
            set {
                _selectedPrims = value;
            }
        }

        /// <summary>
        /// Number of meters^2 in the Simulator
        /// </summary>
        [XmlIgnore]
        public int SimwideArea {
            get {
                return _simwideArea;
            }
            set {
                _simwideArea = value;
            }
        }

        /// <summary>
        /// Number of SceneObjectPart in the Simulator
        /// </summary>
        [XmlIgnore]
        public int SimwidePrims {
            get {
                return _simwidePrims;
            }
            set {
                _simwidePrims = value;
            }
        }

        /// <summary>
        /// ID of the snapshot used in the client parcel dialog of the parcel
        /// </summary>
        public UUID SnapshotID {
            get {
                return _snapshotID;
            }
            set {
                _snapshotID = value;
            }
        }

        /// <summary>
        /// When teleporting is restricted to a certain point, this is the location 
        /// that the user will be redirected to
        /// </summary>
        public Vector3 UserLocation {
            get {
                return _userLocation;
            }
            set {
                _userLocation = value;
            }
        }

        /// <summary>
        /// When teleporting is restricted to a certain point, this is the rotation 
        /// that the user will be positioned
        /// </summary>
        public Vector3 UserLookAt {
            get {
                return _userLookAt;
            }
            set {
                _userLookAt = value;
            }
        }

        /// <summary>
        /// Number of minutes to return SceneObjectGroup that are owned by someone who doesn't own 
        /// the parcel and isn't set to the same 'group' as the parcel.
        /// </summary>
        public int OtherCleanTime {
            get {
                return _otherCleanTime;
            }
            set {
                _otherCleanTime = value;
            }
        }

        /// <summary>
        /// parcel media description
        /// </summary>
        public string MediaDescription {
            get {
                return _mediaDescription;
            }
            set {
                _mediaDescription = value;
            }
        }

        public LandData()
        {
            _globalID = UUID.Random();
        }

        /// <summary>
        /// Make a new copy of the land data
        /// </summary>
        /// <returns></returns>
        public LandData Copy()
        {
            LandData landData = new LandData();

            landData._AABBMax = _AABBMax;
            landData._AABBMin = _AABBMin;
            landData._area = _area;
            landData._auctionID = _auctionID;
            landData._authBuyerID = _authBuyerID;
            landData._category = _category;
            landData._claimDate = _claimDate;
            landData._claimPrice = _claimPrice;
            landData._globalID = _globalID;
            landData._groupID = _groupID;
            landData._groupPrims = _groupPrims;
            landData._otherPrims = _otherPrims;
            landData._ownerPrims = _ownerPrims;
            landData._selectedPrims = _selectedPrims;
            landData._isGroupOwned = _isGroupOwned;
            landData._localID = _localID;
            landData._landingType = _landingType;
            landData._mediaAutoScale = _mediaAutoScale;
            landData._mediaID = _mediaID;
            landData._mediaURL = _mediaURL;
            landData._musicURL = _musicURL;
            landData._ownerID = _ownerID;
            landData._bitmap = (byte[]) _bitmap.Clone();
            landData._description = _description;
            landData._flags = _flags;
            landData._name = _name;
            landData._status = _status;
            landData._passHours = _passHours;
            landData._passPrice = _passPrice;
            landData._salePrice = _salePrice;
            landData._snapshotID = _snapshotID;
            landData._userLocation = _userLocation;
            landData._userLookAt = _userLookAt;
            landData._otherCleanTime = _otherCleanTime;
            landData._dwell = _dwell;
            landData._mediaType = _mediaType;
            landData._mediaDescription = _mediaDescription;
            landData._mediaWidth = _mediaWidth;
            landData._mediaHeight = _mediaHeight;
            landData._mediaLoop = _mediaLoop;
            landData._MediaLoopSet = _MediaLoopSet;
            landData._obscureMusic = _obscureMusic;
            landData._obscureMedia = _obscureMedia;
            landData._regionID = _regionID;
            landData._regionHandle = _regionHandle;
            landData._infoUUID = _infoUUID;

            landData._parcelAccessList.Clear();
            foreach (ParcelManager.ParcelAccessEntry entry in _parcelAccessList)
            {
                ParcelManager.ParcelAccessEntry newEntry = new ParcelManager.ParcelAccessEntry();
                newEntry.AgentID = entry.AgentID;
                newEntry.Flags = entry.Flags;
                newEntry.Time = entry.Time;

                landData._parcelAccessList.Add(newEntry);
            }

            return landData;
        }

        public void ToXml(XmlWriter xmlWriter)
        {
            serializer.Serialize(xmlWriter, this);
        }

        /// <summary>
        /// Restore a LandData object from the serialized xml representation.
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <returns></returns>
        public static LandData FromXml(XmlReader xmlReader)
        {
            LandData land = (LandData)serializer.Deserialize(xmlReader);

            return land;
        }

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            return Util.OSDToDictionary(ToOSD());
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["GroupID"] = OSD.FromUUID(GroupID);
            map["OwnerID"] = OSD.FromUUID(OwnerID);
            map["Maturity"] = OSD.FromInteger(Maturity);
            map["Area"] = OSD.FromInteger(Area);
            map["AuctionID"] = OSD.FromUInteger(AuctionID);
            map["SalePrice"] = OSD.FromInteger(SalePrice);
            map["InfoUUID"] = OSD.FromUUID(InfoUUID);
            map["Dwell"] = OSD.FromInteger(Dwell);
            map["Flags"] = OSD.FromUInteger(Flags);
            map["Name"] = OSD.FromString(Name);
            map["Description"] = OSD.FromString(Description);
            map["UserLocation"] = OSD.FromVector3(UserLocation);
            map["LocalID"] = OSD.FromInteger(LocalID);
            map["GlobalID"] = OSD.FromUUID(GlobalID);
            map["RegionID"] = OSD.FromUUID(RegionID);
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
            map["Category"] = OSD.FromInteger((int)Category);
            map["ClaimDate"] = OSD.FromInteger(ClaimDate);
            map["ClaimPrice"] = OSD.FromInteger(ClaimPrice);
            map["Status"] = OSD.FromInteger((int)Status);
            map["LandingType"] = OSD.FromInteger(LandingType);
            map["PassHours"] = OSD.FromReal(PassHours);
            map["PassPrice"] = OSD.FromInteger(PassPrice);
            map["UserLookAt"] = OSD.FromVector3(UserLookAt);
            map["AuthBuyerID"] = OSD.FromUUID(AuthBuyerID);
            map["OtherCleanTime"] = OSD.FromInteger(OtherCleanTime);
            map["RegionHandle"] = OSD.FromULong(RegionHandle);
            map["GenericData"] = OSD.FromString(GenericData);
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            RegionID = map["RegionID"].AsUUID();
            GlobalID = map["GlobalID"].AsUUID();
            LocalID = map["LocalID"].AsInteger();
            UserLocation = map["UserLocation"].AsVector3();
            Name = map["Name"].AsString();
            Description = map["Description"].AsString();
            Flags = map["Flags"].AsUInteger();
            Dwell = map["Dwell"].AsInteger();
            InfoUUID = map["InfoUUID"].AsUUID();
            AuctionID = map["AuctionID"].AsUInteger();
            Area = map["Area"].AsInteger();
            Maturity = map["Maturity"].AsInteger();
            OwnerID = map["OwnerID"].AsUUID();
            GroupID = map["GroupID"].AsUUID();
            SnapshotID = map["SnapshotID"].AsUUID();
            MediaDescription = map["MediaDescription"].AsString();
            MediaWidth = map["MediaWidth"].AsInteger();
            MediaHeight = map["MediaHeight"].AsInteger();
            MediaLoop = map["MediaLoop"].AsBoolean();
            MediaType = map["MediaType"].AsString();
            ObscureMedia = map["ObscureMedia"].AsBoolean();
            ObscureMusic = map["ObscureMusic"].AsBoolean();
            MediaLoopSet = (float)map["MediaLoopSet"].AsReal();
            MediaAutoScale = (byte)map["MediaAutoScale"].AsInteger();
            MediaURL = map["MediaURL"].AsString();
            Bitmap = map["Bitmap"].AsBinary();
            Category = (ParcelCategory)map["Category"].AsInteger();
            ClaimDate = map["ClaimDate"].AsInteger();
            ClaimPrice = map["ClaimPrice"].AsInteger();
            Status = (ParcelStatus)map["Status"].AsInteger();
            LandingType = (byte)map["LandingType"].AsInteger();
            PassHours = (float)map["PassHours"].AsReal();
            PassPrice = map["PassPrice"].AsInteger();
            UserLookAt = map["UserLookAt"].AsVector3();
            AuthBuyerID = map["AuthBuyerID"].AsUUID();
            OtherCleanTime = map["OtherCleanTime"].AsInteger();
            RegionHandle = map["RegionHandle"].AsULong();
            GenericData = map["GenericData"].AsString();

            if (GroupID != UUID.Zero)
                IsGroupOwned = true;
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }

        public override IDataTransferable Duplicate()
        {
            LandData m = new LandData();
            m.FromOSD(ToOSD());
            return m;
        }
    }
}
