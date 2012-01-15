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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Connectors;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.RobustCompat
{
    /*public class RobustAssetServicesConnector : AssetServicesConnector
    {
        public override string Name
        {
            get { return GetType().Name; }
        }

        public override void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            registry.RegisterModuleInterface<IAssetServiceConnector>(this);

            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AssetHandler", "") != Name)
                return;

            if (MainConsole.Instance != null)
                MainConsole.Instance.Commands.AddCommand("dump asset",
                                                         "dump asset <id> <file>",
                                                         "dump one cached asset", HandleDumpAsset);

            registry.RegisterModuleInterface<IAssetService>(this);
        }

        public override Aurora.Framework.AssetBase Get(string id)
        {
            Aurora.Framework.AssetBase asset = null;
            AssetBase rasset = null;

            if (m_Cache != null)
            {
                asset = m_Cache.Get(id);
                if ((asset != null) && ((asset.Data != null) && (asset.Data.Length != 0)))
                    return asset;
            }

            List<string> serverURIs = m_registry == null
                                          ? null
                                          : m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                                              "AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string>(new string[1] {m_serverURL});
            if (serverURIs != null)
                foreach (string mServerUri in serverURIs)
                {
                    string uri = mServerUri + "/" + id;
                    rasset = SynchronousRestObjectRequester.MakeRequest<int, AssetBase>("GET", uri, 0);
                    asset = TearDown(rasset);
                    if (m_Cache != null && asset != null)
                        m_Cache.Cache(asset);
                    if (asset != null)
                        return asset;
                }
            return null;
        }

        public override bool Get(string id, object sender, AssetRetrieved handler)
        {
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string>(new string[1] {m_serverURL});
            foreach (string m_ServerURI in serverURIs)
            {
                string uri = m_ServerURI + "/" + id;

                Aurora.Framework.AssetBase asset = null;
                if (m_Cache != null)
                    asset = m_Cache.Get(id);

                if (asset == null)
                {
                    bool result = false;

                    AsynchronousRestObjectRequester.
                        MakeRequest("GET", uri, 0,
                                    delegate(AssetBase aa)
                                        {
                                            Aurora.Framework.AssetBase a = TearDown(aa);
                                            if (m_Cache != null)
                                                m_Cache.Cache(a);
                                            handler(id, sender, a);
                                            result = true;
                                        });

                    if (result)
                        return result;
                }
                else
                {
                    handler(id, sender, asset);
                    return true;
                }
            }

            return false;
        }

        public override UUID Store(Aurora.Framework.AssetBase asset)
        {
            AssetBase rasset = Build(asset);
            if ((asset.Flags & AssetFlags.Local) == AssetFlags.Local)
            {
                if (m_Cache != null)
                    m_Cache.Cache(asset);

                return asset.ID;
            }

            UUID newID = UUID.Zero;
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string>(new string[1] {m_serverURL});
            foreach (string mServerUri in serverURIs)
            {
                string uri = mServerUri + "/";
                try
                {
                    string request = SynchronousRestObjectRequester.MakeRequest<AssetBase, string>("POST", uri, rasset);

                    UUID.TryParse(request, out newID);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.WarnFormat("[ASSET CONNECTOR]: Unable to send asset {0} to asset server. Reason: {1}", asset.ID, e.Message);
                }

                if (newID != UUID.Zero)
                {
                    // Placing this here, so that this work with old asset servers that don't send any reply back
                    // SynchronousRestObjectRequester returns somethins that is not an empty string
                    asset.ID = newID;

                    if (m_Cache != null)
                        m_Cache.Cache(asset);
                }
                else
                    return asset.ID; //OPENSIM
            }
            return newID;
        }

        public AssetBase Build(Aurora.Framework.AssetBase asset)
        {
            AssetBase r = new AssetBase
                              {
                                  CreatorID = asset.CreatorID.ToString(),
                                  Data = asset.Data,
                                  Description = asset.Description,
                                  Flags = asset.Flags,
                                  ID = asset.ID.ToString(),
                                  Name = asset.Name,
                                  Type = (sbyte) asset.Type
                              };
            return r;
        }

        public Aurora.Framework.AssetBase TearDown(AssetBase asset)
        {
            if (asset == null)
                return null;
            Aurora.Framework.AssetBase r = new Aurora.Framework.AssetBase
                                        {
                                            CreatorID = UUID.Parse(asset.CreatorID),
                                            Data = asset.Data,
                                            Description = asset.Description,
                                            Flags = asset.Flags,
                                            ID = UUID.Parse(asset.ID),
                                            Name = asset.Name,
                                            Type = asset.Type
                                        };
            return r;
        }

        #region Nested type: AssetBase

        /// <summary>
        ///   Asset class.   All Assets are reference by this class or a class derived from this class
        /// </summary>
        [Serializable]
        public class AssetBase
        {
            /// <summary>
            ///   Data of the Asset
            /// </summary>
            private byte[] m_data;

            /// <summary>
            ///   Meta Data of the Asset
            /// </summary>
            private AssetMetadata m_metadata;

            // This is needed for .NET serialization!!!
            // Do NOT "Optimize" away!
            public AssetBase()
            {
                m_metadata = new AssetMetadata
                                 {
                                     FullID = UUID.Zero,
                                     ID = UUID.Zero.ToString(),
                                     Type = (sbyte) AssetType.Unknown,
                                     CreatorID = String.Empty
                                 };
            }

            public AssetBase(UUID assetID, string name, sbyte assetType, string creatorID)
            {
                if (assetType == (sbyte) AssetType.Unknown)
                {
                    StackTrace trace = new StackTrace(true);
                    MainConsole.Instance.ErrorFormat("[ASSETBASE]: Creating asset '{0}' ({1}) with an unknown asset type\n{2}",
                                      name, assetID, trace);
                }

                m_metadata = new AssetMetadata {FullID = assetID, Name = name, Type = assetType, CreatorID = creatorID};
            }

            public AssetBase(string assetID, string name, sbyte assetType, string creatorID)
            {
                if (assetType == (sbyte) AssetType.Unknown)
                {
                    StackTrace trace = new StackTrace(true);
                    MainConsole.Instance.ErrorFormat("[ASSETBASE]: Creating asset '{0}' ({1}) with an unknown asset type\n{2}",
                                      name, assetID, trace);
                }

                m_metadata = new AssetMetadata {ID = assetID, Name = name, Type = assetType, CreatorID = creatorID};
            }

            public bool ContainsReferences
            {
                get
                {
                    return
                        IsTextualAsset && (
                                              Type != (sbyte) AssetType.Notecard
                                              && Type != (sbyte) AssetType.CallingCard
                                              && Type != (sbyte) AssetType.LSLText
                                              && Type != (sbyte) AssetType.Landmark);
                }
            }

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
                        (Type == (sbyte) AssetType.Animation ||
                         Type == (sbyte) AssetType.Gesture ||
                         Type == (sbyte) AssetType.Simstate ||
                         Type == (sbyte) AssetType.Unknown ||
                         Type == (sbyte) AssetType.Object ||
                         Type == (sbyte) AssetType.Sound ||
                         Type == (sbyte) AssetType.SoundWAV ||
                         Type == (sbyte) AssetType.Texture ||
                         Type == (sbyte) AssetType.TextureTGA ||
                         Type == (sbyte) AssetType.Folder ||
                         Type == (sbyte) AssetType.RootFolder ||
                         Type == (sbyte) AssetType.LostAndFoundFolder ||
                         Type == (sbyte) AssetType.SnapshotFolder ||
                         Type == (sbyte) AssetType.TrashFolder ||
                         Type == (sbyte) AssetType.ImageJPEG ||
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
            ///   Asset UUID
            /// </summary>
            public UUID FullID
            {
                get { return m_metadata.FullID; }
                set { m_metadata.FullID = value; }
            }

            /// <summary>
            ///   Asset MetaData ID (transferring from UUID to string ID)
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
            ///   (sbyte) AssetType enum
            /// </summary>
            public sbyte Type
            {
                get { return m_metadata.Type; }
                set { m_metadata.Type = value; }
            }

            /// <summary>
            ///   Is this a region only asset, or does this exist on the asset server also
            /// </summary>
            public bool Local
            {
                get { return m_metadata.Local; }
                set { m_metadata.Local = value; }
            }

            /// <summary>
            ///   Is this asset going to be saved to the asset database?
            /// </summary>
            public bool Temporary
            {
                get { return m_metadata.Temporary; }
                set { m_metadata.Temporary = value; }
            }

            public string CreatorID
            {
                get { return m_metadata.CreatorID; }
                set { m_metadata.CreatorID = value; }
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

        #endregion

        #region Nested type: AssetMetadata

        [Serializable]
        public class AssetMetadata
        {
            private string m_content_type;
            private DateTime m_creation_date;
            private string m_creatorid;
            private string m_description = String.Empty;
            private AssetFlags m_flags;
            private UUID m_fullid;
            private string m_id;
            private bool m_local;
            private string m_name = String.Empty;
            private byte[] m_sha1;
            private bool m_temporary;
            private sbyte m_type = (sbyte) AssetType.Unknown;

            public UUID FullID
            {
                get { return m_fullid; }
                set
                {
                    m_fullid = value;
                    m_id = m_fullid.ToString();
                }
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

                    sbyte type = SLUtil.ContentTypeToSLAssetType(value);
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

        #endregion
    }*/
}