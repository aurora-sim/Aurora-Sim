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
using System.Diagnostics;
using System.Security.Cryptography;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;

namespace Aurora.Framework
{
    [Flags]
    public enum AssetFlags
    {
        Normal = 0, // Immutable asset
        Maptile = 1, // Depriated, use Deletable instead: What it says
        Rewritable = 2, // Content can be rewritten
        Collectable = 4, // Can be GC'ed after some time
        Deletable = 8, // The asset can be deleted
        Local = 16, // Region-only asset, never stored in the database
        Temporary = 32,
        // Is this asset going to exist permanently in the database, or can it be purged after a set amount of time?
        RemotelyAccessable = 64 // Regions outside of this grid can access this asset
    }

    /// <summary>
    ///   Asset class.   All Assets are reference by this class or a class derived from this class
    /// </summary>
    [Serializable, ProtoContract(UseProtoMembersOnly = false)]
    public class AssetBase : IDataTransferable, IDisposable
    {
        private static readonly SHA256Managed SHA256Managed = new SHA256Managed();
        private string idString = "";
        private byte[] myData = new byte[] {};
        private string myHashCode = "";

        #region Initiation

        // This is needed for .NET serialization!!!
        // Do NOT "Optimize" away!
        public AssetBase()
        {
            SimpleInitialize();
        }

        public AssetBase(string assetID)
        {
            SimpleInitialize();
            IDString = assetID;
        }

        public AssetBase(UUID assetID)
        {
            SimpleInitialize();
            ID = assetID;
        }

        public AssetBase(string assetID, string name, AssetType assetType, UUID creatorID)
        {
            Initiate(assetID, name, assetType, creatorID);
        }

        public AssetBase(UUID assetID, string name, AssetType assetType, UUID creatorID)
        {
            Initiate(assetID.ToString(), name, assetType, creatorID);
        }

        private void SimpleInitialize()
        {
            DatabaseTable = "assets";
            ID = UUID.Zero;
            TypeAsset = AssetType.Unknown;
            CreatorID = UUID.Zero;
            Description = "";
            Name = "";
            HostUri = "";
            LastAccessed = DateTime.UtcNow;
            CreationDate = DateTime.UtcNow;
            HashCode = "";
            LastHashCode = "";
            ParentID = UUID.Zero;
            MetaOnly = true;
            Data = new byte[] {};
            Flags = AssetFlags.Normal;
        }

        public void Initiate(string assetID, string name, AssetType assetType, UUID creatorID)
        {
            SimpleInitialize();
            if (assetType == AssetType.Unknown)
            {
                StackTrace trace = new StackTrace(true);
                MainConsole.Instance.ErrorFormat("[ASSETBASE]: Creating asset '{0}' ({1}) with an unknown asset type\n{2}",
                                  name, assetID, trace);
            }

            IDString = assetID;
            Name = name;
            TypeAsset = assetType;
            CreatorID = creatorID;
        }

        #endregion

        #region is Type asset

        public bool IsTextualAsset
        {
            get { return !IsBinaryAsset; }
        }

        /// <summary>
        ///   Checks if this asset is a binary or text asset
        /// </summary>
        public bool IsBinaryAsset
        {
            get
            {
                return
                    (TypeAsset == AssetType.Animation ||
                     TypeAsset == AssetType.Gesture ||
                     TypeAsset == AssetType.Simstate ||
                     TypeAsset == AssetType.Unknown ||
                     TypeAsset == AssetType.Object ||
                     TypeAsset == AssetType.Sound ||
                     TypeAsset == AssetType.SoundWAV ||
                     TypeAsset == AssetType.Texture ||
                     TypeAsset == AssetType.TextureTGA ||
                     TypeAsset == AssetType.Folder ||
                     TypeAsset == AssetType.RootFolder ||
                     TypeAsset == AssetType.LostAndFoundFolder ||
                     TypeAsset == AssetType.SnapshotFolder ||
                     TypeAsset == AssetType.TrashFolder ||
                     TypeAsset == AssetType.ImageJPEG ||
                     TypeAsset == AssetType.ImageTGA ||
                     TypeAsset == AssetType.LSLBytecode);
            }
        }

        public bool ContainsReferences
        {
            get
            {
                return
                    IsTextualAsset && (
                                          TypeAsset != AssetType.Notecard
                                          && TypeAsset != AssetType.CallingCard
                                          && TypeAsset != AssetType.LSLText
                                          && TypeAsset != AssetType.Landmark);
            }
        }

        #endregion

        #region properties

        /// <summary>
        /// used by archive loaders to track with assets have been saved	
        /// </summary>
        public bool HasBeenSaved { get; set; }

        public string TypeString
        {
            get { return SLUtil.SLAssetTypeToContentType((int) TypeAsset); }
            set { TypeAsset = (AssetType) SLUtil.ContentTypeToSLAssetType(value); }
        }

        [ProtoMember(1)]
        public virtual byte[] Data
        {
            get { return myData; }
            set
            {
                myData = value;
                if (myData != null)
                {
                    MetaOnly = (myData.Length == 0);
                    if (!MetaOnly)
                        HashCode = FillHash(myData);
                }
                else
                    HashCode = "";
            }
        }

        public UUID ID { get; set; }
        // This is only used for cache
        [ProtoMember(2)]
        public string IDString
        {
            get { return idString.Length > 0 ? idString : ID.ToString(); }
            set
            {
                UUID k;
                if (UUID.TryParse(value, out k))
                    ID = k;
                else if ((value.Length >= 37) && (UUID.TryParse(value.Substring(value.Length - 36), out k)))
                {
                    ID = k;
                    idString = value;
                }
                else idString = value;
            }
        }

        [ProtoMember(3)]
        public string HashCode
        {
            get { return myHashCode; }
            set
            {
                // ensure we keep the orginal hash from when it was loaded 
                // so we can check if its being used anymore with any other assets
                if ((LastHashCode == myHashCode) || (LastHashCode == ""))
                    LastHashCode = myHashCode;
                myHashCode = value;
            }
        }

        [ProtoMember(4)]
        public string LastHashCode { get; set; }

        [ProtoMember(5)]
        public string Name { get; set; }
        [ProtoMember(6)]
        public string Description { get; set; }

        public AssetType TypeAsset { get; set; }

        [ProtoMember(7)]
        public int Type
        {
            get { return (int) TypeAsset; }
            set { TypeAsset = (AssetType) value; }
        }

        [ProtoMember(8)]
        public AssetFlags Flags { get; set; }
        [ProtoMember(9)]
        public string DatabaseTable { get; set; }
        [ProtoMember(10)]
        public string HostUri { get; set; }
        [ProtoMember(11)]
        public DateTime LastAccessed { get; set; }
        [ProtoMember(12)]
        public DateTime CreationDate { get; set; }
        public UUID CreatorID { get; set; }
        [ProtoMember(13)]
        public string SerializedCreatorID { get { return CreatorID.ToString(); } set { CreatorID = UUID.Parse(value); } }
        public UUID ParentID { get; set; }
        [ProtoMember(14)]
        public string SerializedParentID { get { return ParentID.ToString(); } set { ParentID = UUID.Parse(value); } }
        [ProtoMember(15)]
        public bool MetaOnly { get; set; }

        // should run this if your filling out a new asset
        // ensures we don't try to pull it from the database when saving the asset
        // because we need to know if it changed.
        public static string FillHash(byte[] data)
        {
            return Convert.ToBase64String(SHA256Managed.ComputeHash(data)) + data.Length;
        }

        public override string ToString()
        {
            return ID.ToString();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Data = null;
        }

        #endregion

        #region Packing/Unpacking

        /// <summary>
        ///   Pack this asset into an OSDMap
        /// </summary>
        /// <returns></returns>
        public OSDMap Pack()
        {
            return ToOSD();
        }

        /// <summary>
        ///   Pack this asset into an OSDMap
        /// </summary>
        /// <returns></returns>
        public override OSDMap ToOSD()
        {
            OSDMap assetMap = new OSDMap
                                  {
                                      {"AssetFlags", OSD.FromInteger((int) Flags)},
                                      {"AssetID", ID},
                                      {"CreationDate", OSD.FromDate(CreationDate)},
                                      {"CreatorID", OSD.FromUUID(CreatorID)},
                                      {"Data", OSD.FromBinary(Data)},
                                      {"HostUri", OSD.FromString(HostUri)},
                                      {"LastAccessed", OSD.FromDate(LastAccessed)},
                                      {"Name", OSD.FromString(Name)},
                                      {"ParentID", CreationDate},
                                      {"TypeAsset", OSD.FromInteger((int) TypeAsset)},
                                      {"Description", OSD.FromString(Description)},
                                      {"DatabaseTable", OSD.FromString(DatabaseTable)}
                                  };
            return assetMap;
        }

        public override void FromOSD(OSDMap map)
        {
            Unpack(map);
        }

        /// <summary>
        ///   Unpack the asset from an OSDMap
        /// </summary>
        /// <param name = "osd"></param>
        public AssetBase Unpack(OSD osd)
        {
            if (!(osd is OSDMap))
                return null;
            OSDMap assetMap = (OSDMap) osd;

            if (assetMap.ContainsKey("AssetFlags"))
                Flags = (AssetFlags) assetMap["AssetFlags"].AsInteger();

            if (assetMap.ContainsKey("AssetID"))
                ID = assetMap["AssetID"].AsUUID();

            if (assetMap.ContainsKey("CreationDate"))
                CreationDate = assetMap["CreationDate"].AsDate();

            if (assetMap.ContainsKey("CreatorID"))
                CreatorID = assetMap["CreatorID"].AsUUID();

            if (assetMap.ContainsKey("Data"))
                Data = assetMap["Data"].AsBinary();

            if (assetMap.ContainsKey("HostUri"))
                HostUri = assetMap["HostUri"].AsString();

            if (assetMap.ContainsKey("LastAccessed"))
                LastAccessed = assetMap["LastAccessed"].AsDate();

            if (assetMap.ContainsKey("Name"))
                Name = assetMap["Name"].AsString();

            if (assetMap.ContainsKey("TypeAsset"))
                TypeAsset = (AssetType) assetMap["TypeAsset"].AsInteger();

            if (assetMap.ContainsKey("Description"))
                Description = assetMap["Description"].AsString();

            if (assetMap.ContainsKey("ParentID"))
                ParentID = assetMap["ParentID"].AsUUID();

            if (assetMap.ContainsKey("DatabaseTable"))
                DatabaseTable = assetMap["DatabaseTable"].AsString();

            return this;
        }

        /// <summary>
        ///   Make an OSDMap (json) with only the needed parts for the database and then compress it
        /// </summary>
        /// <returns>A compressed (gzip) string of the data needed for the database</returns>
        public string CompressedPack()
        {
            OSDMap assetMap = ToOSD();

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
            OSDMap assetMap = (OSDMap) OSDParser.DeserializeJson(jsonString);
            //Now unpack the contents
            Unpack(assetMap);
        }

        #endregion
    }
}