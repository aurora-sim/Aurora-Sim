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
using System.Xml.Serialization;
using System.Reflection;
using log4net;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Framework
{
    [Flags]
    public enum AssetFlags : int
    {
        Normal = 0,         // Immutable asset
        Maptile = 1,        // Depriated, use Deletable instead: What it says
        Rewritable = 2,     // Content can be rewritten
        Collectable = 4,    // Can be GC'ed after some time
        Deletable = 8       // The asset can be deleted
    }

    /// <summary>
    /// Asset class.   All Assets are reference by this class or a class derived from this class
    /// </summary>
    [Serializable]
    public class AssetBase
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Data of the Asset
        /// </summary>
        private byte[] m_data;

        /// <summary>
        /// Meta Data of the Asset
        /// </summary>
        private AssetMetadata m_metadata;

        // This is needed for .NET serialization!!!
        // Do NOT "Optimize" away!
        public AssetBase()
        {
            m_metadata = new AssetMetadata();
            m_metadata.FullID = UUID.Zero;
            m_metadata.ID = UUID.Zero.ToString();
            m_metadata.Type = (sbyte)AssetType.Unknown;
            m_metadata.CreatorID = String.Empty;
        }

        public AssetBase(UUID assetID, string name, sbyte assetType, string creatorID)
        {
            if (assetType == (sbyte)AssetType.Unknown)
            {
                System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(true);
                m_log.ErrorFormat("[ASSETBASE]: Creating asset '{0}' ({1}) with an unknown asset type\n{2}",
                    name, assetID, trace.ToString());
            }

            m_metadata = new AssetMetadata();
            m_metadata.FullID = assetID;
            m_metadata.Name = name;
            m_metadata.Type = assetType;
            m_metadata.CreatorID = creatorID;
        }

        public AssetBase(string assetID, string name, sbyte assetType, string creatorID)
        {
            if (assetType == (sbyte)AssetType.Unknown)
            {
                System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(true);
                m_log.ErrorFormat("[ASSETBASE]: Creating asset '{0}' ({1}) with an unknown asset type\n{2}",
                    name, assetID, trace.ToString());
            }

            m_metadata = new AssetMetadata();
            m_metadata.ID = assetID;
            m_metadata.Name = name;
            m_metadata.Type = assetType;
            m_metadata.CreatorID = creatorID;
        }

        public bool ContainsReferences
        {
            get
            {
                return 
                    IsTextualAsset && (
                    Type != (sbyte)AssetType.Notecard
                    && Type != (sbyte)AssetType.CallingCard
                    && Type != (sbyte)AssetType.LSLText
                    && Type != (sbyte)AssetType.Landmark);
            }
        }

        public bool IsTextualAsset
        {
            get
            {
                return !IsBinaryAsset;
            }

        }

        /// <summary>
        /// Checks if this asset is a binary or text asset
        /// </summary>
        public bool IsBinaryAsset
        {
            get
            {
                return 
                    (Type == (sbyte) AssetType.Animation ||
                     Type == (sbyte)AssetType.Gesture ||
                     Type == (sbyte)AssetType.Simstate ||
                     Type == (sbyte)AssetType.Unknown ||
                     Type == (sbyte)AssetType.Object ||
                     Type == (sbyte)AssetType.Sound ||
                     Type == (sbyte)AssetType.SoundWAV ||
                     Type == (sbyte)AssetType.Texture ||
                     Type == (sbyte)AssetType.TextureTGA ||
                     Type == (sbyte)AssetType.Folder ||
                     Type == (sbyte)AssetType.RootFolder ||
                     Type == (sbyte)AssetType.LostAndFoundFolder ||
                     Type == (sbyte)AssetType.SnapshotFolder ||
                     Type == (sbyte)AssetType.TrashFolder ||
                     Type == (sbyte)AssetType.ImageJPEG ||
                     Type == (sbyte) AssetType.ImageTGA ||
                     Type == (sbyte) AssetType.LSLBytecode);
            }
        }

        public virtual byte[] Data
        {
            get { return m_data; }
            set { m_data = value; }
        }

        /// <summary>
        /// Asset UUID
        /// </summary>
        public UUID FullID
        {
            get { return m_metadata.FullID; }
            set { m_metadata.FullID = value; }
        }
        /// <summary>
        /// Asset MetaData ID (transferring from UUID to string ID)
        /// </summary>
        public string ID
        {
            get { return m_metadata.ID; }
            set { m_metadata.ID = value; }
        }

        public string Name
        {
            get { return m_metadata.Name; }
            set { m_metadata.Name = value; }
        }

        public string Description
        {
            get { return m_metadata.Description; }
            set { m_metadata.Description = value; }
        }

        /// <summary>
        /// (sbyte) AssetType enum
        /// </summary>
        public sbyte Type
        {
            get { return m_metadata.Type; }
            set { m_metadata.Type = value; }
        }

        /// <summary>
        /// Is this a region only asset, or does this exist on the asset server also
        /// </summary>
        public bool Local
        {
            get { return m_metadata.Local; }
            set { m_metadata.Local = value; }
        }

        /// <summary>
        /// Is this asset going to be saved to the asset database?
        /// </summary>
        public bool Temporary
        {
            get { return m_metadata.Temporary; }
            set { m_metadata.Temporary = value; }
        }

        public AssetFlags Flags
        {
            get { return m_metadata.Flags; }
            set { m_metadata.Flags = value; }
        }

        [XmlIgnore]
        public AssetMetadata Metadata
        {
            get { return m_metadata; }
            set { m_metadata = value; }
        }

        public override string ToString()
        {
            return FullID.ToString();
        }
    }

    [Serializable]
    public class AssetMetadata
    {
        private UUID m_fullid;
        private string m_id;
        private string m_name = String.Empty;
        private string m_description = String.Empty;
        private DateTime m_creation_date;
        private sbyte m_type = (sbyte)AssetType.Unknown;
        private string m_content_type;
        private byte[] m_sha1;
        private bool m_local;
        private bool m_temporary;
        private string m_creatorid;
        private AssetFlags m_flags;

        public UUID FullID
        {
            get { return m_fullid; }
            set { m_fullid = value; m_id = m_fullid.ToString(); }
        }

        public string ID
        {
            //get { return m_fullid.ToString(); }
            //set { m_fullid = new UUID(value); }
            get
            {
                if (String.IsNullOrEmpty(m_id))
                    m_id = m_fullid.ToString();

                return m_id;
            }
            set
            {
                UUID uuid = UUID.Zero;
                if (UUID.TryParse(value, out uuid))
                {
                    m_fullid = uuid;
                    m_id = m_fullid.ToString();
                }
                else
                    m_id = value;
            }
        }

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public string Description
        {
            get { return m_description; }
            set { m_description = value; }
        }

        public DateTime CreationDate
        {
            get { return m_creation_date; }
            set { m_creation_date = value; }
        }

        public sbyte Type
        {
            get { return m_type; }
            set { m_type = value; }
        }

        public string ContentType
        {
            get
            {
                if (!String.IsNullOrEmpty(m_content_type))
                    return m_content_type;
                else
                    return SLUtil.SLAssetTypeToContentType(m_type);
            }
            set
            {
                m_content_type = value;

                sbyte type = (sbyte)SLUtil.ContentTypeToSLAssetType(value);
                if (type != -1)
                    m_type = type;
            }
        }

        public byte[] SHA1
        {
            get { return m_sha1; }
            set { m_sha1 = value; }
        }

        public bool Local
        {
            get { return m_local; }
            set { m_local = value; }
        }

        public bool Temporary
        {
            get { return m_temporary; }
            set { m_temporary = value; }
        }

        public string CreatorID
        {
            get { return m_creatorid; }
            set { m_creatorid = value; }
        }

        public AssetFlags Flags
        {
            get { return m_flags; }
            set { m_flags = value; }
        }
    }

    public enum AssetMetaDataFlags
    {
        Normal = 0,               ///No flags
        Deletable = 1,            ///Can this asset be deleted from the database?
        Temperary = 4,            ///Is this asset going to exist permanently in the database, or can it be purged after a set amount of time?
        Local = 8,                ///Region-only asset, never stored in the database
        RemotelyAccessable = 16,  ///Regions outside of this grid can access this asset
        Rewritable = 32,          ///The asset can be changed
    }

    public class AssetMetaverseData
    {
        #region Declares

        /// <summary>
        /// The UUID of the Asset.
        /// </summary>
        private UUID m_AssetID = UUID.Zero;

        /// <summary>
        /// The hostname of the place this asset was created.
        /// This can be called to retrieve the asset if needed.
        /// </summary>
        private String m_HostUri = "";

        /// <summary>
        /// The asset data itself.
        /// </summary>
        private byte[] m_Data = new byte[0];

        /// <summary>
        /// The name of this asset.
        /// </summary>
        private String m_Name = "";

        /// <summary>
        /// The last time this asset was accessed. Used for purging the database of temperary assets.
        /// </summary>
        private DateTime m_LastAccessed = DateTime.Now;

        /// <summary>
        /// The last time this asset was changed in the database.
        /// </summary>
        private DateTime m_LastChanged = DateTime.Now;

        /// <summary>
        /// The time that this asset was created.
        /// </summary>
        private DateTime m_CreationDate = DateTime.Now;

        /// <summary>
        /// The UUID of the Creator of this asset.
        /// It is assumed that the Creator is from the same place the asset was created, and therefore, 
        ///    the creator info should be able to be found by the m_HostUri above as well.
        /// </summary>
        private UUID m_CreatorID;

        /// <summary>
        /// The UUID of the Owner of this asset.
        /// </summary>
        private UUID m_OwnerID;

        /// <summary>
        /// The flags that this asset has.
        /// </summary>
        private AssetMetaDataFlags m_AssetFlags = AssetMetaDataFlags.Normal;

        /// <summary>
        /// The type of asset this represents.
        /// </summary>
        private AssetType m_AssetType = AssetType.Unknown;

        public UUID AssetID
        {
            get { return m_AssetID; }
        }

        public UUID CreatorID
        {
            get { return m_CreatorID; }
        }

        public UUID OwnerID
        {
            get { return m_OwnerID; }
        }

        public String HostUri
        {
            get { return m_HostUri; }
        }

        public byte[] Data
        {
            get { return m_Data; }
            set
            {
                //Update the times on the asset
                m_LastAccessed = DateTime.Now;
                m_LastChanged = DateTime.Now;
                m_Data = value;
            }
        }

        public DateTime CreationDate
        {
            get { return m_CreationDate; }
        }

        public DateTime LastAccessed
        {
            get { return m_LastAccessed; }
        }

        public DateTime LastChanged
        {
            get { return m_LastChanged; }
        }

        public String Name
        {
            get { return m_Name; }
            set
            {
                //Update the times on the asset
                m_LastAccessed = DateTime.Now;
                m_LastChanged = DateTime.Now; 
                m_Name = value;
            }
        }

        public AssetType AssetType
        {
            get { return m_AssetType; }
            set
            {
                //Update the times on the asset
                m_LastAccessed = DateTime.Now;
                m_LastChanged = DateTime.Now;
                m_AssetType = value;
            }
        }

        public AssetMetaDataFlags AssetFlags
        {
            get { return m_AssetFlags; }
            set
            {
                //Update the times on the asset
                m_LastAccessed = DateTime.Now;
                m_LastChanged = DateTime.Now;
                m_AssetFlags = value;
            }
        }

        /// <summary>
        /// Checks if this asset is a text based asset
        /// This is used in the database to determine what table the asset should go in
        /// </summary>
        public bool IsTextBasedAsset
        {
            get
            {
                return
                    (m_AssetType == AssetType.Bodypart ||
                     m_AssetType == AssetType.Bodypart ||
                     m_AssetType == AssetType.CallingCard ||
                     m_AssetType == AssetType.Clothing ||
                     m_AssetType == AssetType.Gesture ||
                     m_AssetType == AssetType.Landmark ||
                     m_AssetType == AssetType.LSLBytecode ||
                     m_AssetType == AssetType.LSLText ||
                     m_AssetType == AssetType.Notecard ||
                     m_AssetType == AssetType.Object ||
                     m_AssetType == AssetType.Simstate);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// A constructor of the class
        /// </summary>
        /// <param name="assetID">The ID of this asset</param>
        /// <param name="creatorID">The creator of this asset</param>
        /// <param name="ownerID">The current owner of this asset</param>
        /// <param name="hostUri">The HostUri that this asset can be accessed at</param>
        /// <param name="data">The binary data of this asset</param>
        /// <param name="name">The name of this asset</param>
        /// <param name="assetType">The type of asset this is</param>
        /// <param name="assetFlags">The flags that the asset will have</param>
        public AssetMetaverseData(UUID assetID, UUID creatorID, UUID ownerID, String hostUri, byte[] data,
            String name, AssetType assetType, AssetMetaDataFlags assetFlags)
        {
            m_AssetID = assetID;
            m_CreatorID = creatorID;
            m_OwnerID = ownerID;
            m_HostUri = hostUri;
            m_Data = data;
            m_Name = name;
            m_AssetType = assetType;
            m_AssetFlags = assetFlags;
        }

        /// <summary>
        /// A constructor of the class
        /// </summary>
        /// <param name="assetID">The ID of this asset</param>
        /// <param name="creatorID">The creator of this asset</param>
        /// <param name="ownerID">The current owner of this asset</param>
        /// <param name="hostUri">The HostUri that this asset can be accessed at</param>
        /// <param name="data">The binary data of this asset</param>
        /// <param name="name">The name of this asset</param>
        public AssetMetaverseData(UUID assetID, UUID creatorID, UUID ownerID, String hostUri, byte[] data,
            String name)
        {
            m_AssetID = assetID;
            m_CreatorID = creatorID;
            m_OwnerID = ownerID;
            m_HostUri = hostUri;
            m_Data = data;
            m_Name = name;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// A nicer way to display the asset rather than letting it do it automatically
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return m_AssetID.ToString();
        }

        #endregion

        #region Packing/Unpacking

        /// <summary>
        /// Pack this asset into an OSDMap
        /// </summary>
        /// <returns></returns>
        public OSD Pack()
        {
            OSDMap assetMap = new OSDMap();

            assetMap["AssetFlags"] = (int)this.AssetFlags;
            assetMap["AssetID"] = this.AssetID;
            assetMap["CreationDate"] = this.CreationDate;
            assetMap["CreatorID"] = this.CreatorID;
            assetMap["Data"] = this.Data;
            assetMap["HostUri"] = this.HostUri;
            assetMap["LastAccessed"] = this.LastAccessed;
            assetMap["LastChanged"] = this.LastChanged;
            assetMap["Name"] = this.Name;
            assetMap["AssetType"] = (int)this.AssetType;

            return assetMap;
        }

        /// <summary>
        /// Unpack the asset from an OSDMap
        /// </summary>
        /// <param name="osd"></param>
        public void Unpack(OSD osd)
        {
            if (!(osd is OSDMap))
                return;
            OSDMap assetMap = (OSDMap)osd;

            if(assetMap.ContainsKey("AssetFlags"))
                m_AssetFlags = (AssetMetaDataFlags)assetMap["AssetFlags"].AsInteger();

            if (assetMap.ContainsKey("AssetID"))
                m_AssetID = assetMap["AssetID"].AsUUID();

            if (assetMap.ContainsKey("CreationDate"))
                m_CreationDate = assetMap["CreationDate"].AsDate();

            if (assetMap.ContainsKey("CreatorID"))
                m_CreatorID = assetMap["CreatorID"].AsUUID();

            if (assetMap.ContainsKey("OwnerID"))
                m_OwnerID = assetMap["OwnerID"].AsUUID();

            if (assetMap.ContainsKey("Data"))
                m_Data = assetMap["Data"].AsBinary();

            if (assetMap.ContainsKey("HostUri"))
                m_HostUri = assetMap["HostUri"].AsString();

            if (assetMap.ContainsKey("LastAccessed"))
                m_LastAccessed = assetMap["LastAccessed"].AsDate();

            if (assetMap.ContainsKey("LastChanged"))
                m_LastChanged = assetMap["LastChanged"].AsDate();

            if (assetMap.ContainsKey("Name"))
                m_Name = assetMap["Name"].AsString();

            if (assetMap.ContainsKey("AssetType"))
                m_AssetType = (AssetType)assetMap["AssetType"].AsInteger();
        }

        /// <summary>
        /// Make an OSDMap (json) with only the needed parts for the database and then compress it
        /// </summary>
        /// <returns>A compressed (gzip) string of the data needed for the database</returns>
        public string CompressedPack()
        {
            OSDMap assetMap = new OSDMap();

            assetMap["AssetFlags"] = (int)this.AssetFlags;
            assetMap["CreationDate"] = this.CreationDate;
            assetMap["CreatorID"] = this.CreatorID;

            //In the database table, don't save it
            //assetMap["OwnerID"] = this.OwnerID;

            //In the database table, don't save it
            //assetMap["Data"] = this.Data;

            assetMap["HostUri"] = this.HostUri;
            assetMap["LastAccessed"] = this.LastAccessed;
            assetMap["LastChanged"] = this.LastChanged;

            //In the database table, don't save it
            //assetMap["Name"] = this.Name;

            //In the database table, don't save it
            //assetMap["AssetType"] = (int)this.AssetType;

            //Serialize it with json
            string jsonString = OSDParser.SerializeJsonString(assetMap);
            //Now use gzip to compress this map
            string compressedString = Util.Compress(jsonString);

            return compressedString;
        }

        public void CompressedUnpack(string compressedString)
        {
            //Decompress the info back to json format
            string jsonString = Util.Decompress(compressedString);
            //Build the OSDMap 
            OSDMap assetMap = (OSDMap)OSDParser.DeserializeJson(jsonString);
            //Now unpack the contents
            Unpack(assetMap);
        }

        #endregion
    }
}
