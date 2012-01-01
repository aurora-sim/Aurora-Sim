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
using System.IO;
using System.Net;
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.Connectors.SimianGrid
{
    /// <summary>
    ///   Connects to the SimianGrid asset service
    /// </summary>
    public class SimianAssetServiceConnector : IAssetService, IService
    {
        private static string ZeroID = UUID.Zero.ToString();

        private IImprovedAssetCache m_cache;
        private string m_serverUrl = String.Empty;

        public SimianAssetServiceConnector()
        {
        }

        public SimianAssetServiceConnector(string url)
        {
            m_serverUrl = url;
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IAssetService Members

        public void Configure(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_cache = registry.RequestModuleInterface<IImprovedAssetCache>();
        }

        public void FinishedStartup()
        {
        }

        public IAssetService InnerService
        {
            get { return this; }
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AssetHandler", "") != Name)
                return;

            CommonInit(config);
            registry.RegisterModuleInterface<IAssetService>(this);
        }

        #endregion

        private void CommonInit(IConfigSource source)
        {
            IConfig gridConfig = source.Configs["AssetService"];
            if (gridConfig != null)
            {
                string serviceUrl = gridConfig.GetString("AssetServerURI");
                if (!String.IsNullOrEmpty(serviceUrl))
                {
                    if (!serviceUrl.EndsWith("/") && !serviceUrl.EndsWith("="))
                        serviceUrl = serviceUrl + '/';
                    m_serverUrl = serviceUrl;
                }
            }

            if (String.IsNullOrEmpty(m_serverUrl))
                MainConsole.Instance.Info("[SIMIAN ASSET CONNECTOR]: No AssetServerURI specified, disabling connector");
        }

        private AssetBase GetRemote(string id)
        {
            AssetBase asset = null;
            Uri url;

            // Determine if id is an absolute URL or a grid-relative UUID
            if (!Uri.TryCreate(id, UriKind.Absolute, out url))
                url = new Uri(m_serverUrl + id);

            try
            {
                HttpWebRequest request = UntrustedHttpWebRequest.Create(url);

                using (WebResponse response = request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        string creatorID = response.Headers.GetOne("X-Asset-Creator-Id") ?? string.Empty;

                        // Create the asset object
                        asset = new AssetBase(id, String.Empty,
                                              (AssetType) SLUtil.ContentTypeToSLAssetType(response.ContentType),
                                              UUID.Parse(creatorID));

                        // Grab the asset data from the response stream
                        using (MemoryStream stream = new MemoryStream())
                        {
                            responseStream.CopyTo(stream, Int32.MaxValue);
                            asset.Data = stream.ToArray();
                        }
                    }
                }

                // Cache store
                if (m_cache != null)
                    m_cache.Cache(asset);

                return asset;
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[SIMIAN ASSET CONNECTOR]: Asset GET from " + url + " failed: " + ex.Message);
                return null;
            }
        }

        #region IAssetService

        public AssetBase Get(string id)
        {
            if (String.IsNullOrEmpty(m_serverUrl))
            {
                MainConsole.Instance.Error("[SIMIAN ASSET CONNECTOR]: No AssetServerURI configured");
                throw new InvalidOperationException();
            }

            // Cache fetch
            if (m_cache != null)
            {
                AssetBase asset = m_cache.Get(id);
                if (asset != null)
                    return asset;
            }

            return GetRemote(id);
        }

        public virtual bool GetExists(string id)
        {
            return Get(id) != null;
        }

        public AssetBase GetCached(string id)
        {
            if (m_cache != null)
                return m_cache.Get(id);

            return null;
        }

        public byte[] GetData(string id)
        {
            AssetBase asset = Get(id);

            if (asset != null)
                return asset.Data;

            return null;
        }

        /// <summary>
        ///   Get an asset asynchronously
        /// </summary>
        /// <param name = "id">The asset id</param>
        /// <param name = "sender">Represents the requester.  Passed back via the handler</param>
        /// <param name = "handler">The handler to call back once the asset has been retrieved</param>
        /// <returns>True if the id was parseable, false otherwise</returns>
        public bool Get(string id, Object sender, AssetRetrieved handler)
        {
            if (String.IsNullOrEmpty(m_serverUrl))
            {
                MainConsole.Instance.Error("[SIMIAN ASSET CONNECTOR]: No AssetServerURI configured");
                throw new InvalidOperationException();
            }

            // Cache fetch
            if (m_cache != null)
            {
                AssetBase asset = m_cache.Get(id);
                if (asset != null)
                {
                    handler(id, sender, asset);
                    return true;
                }
            }

            Util.FireAndForget(
                delegate
                    {
                        AssetBase asset = GetRemote(id);
                        handler(id, sender, asset);
                    }
                );

            return true;
        }

        /// <summary>
        ///   Creates a new asset
        /// </summary>
        /// Returns a random ID if none is passed into it
        /// <param name = "asset"></param>
        /// <returns></returns>
        public UUID Store(AssetBase asset)
        {
            if (String.IsNullOrEmpty(m_serverUrl))
            {
                MainConsole.Instance.Error("[SIMIAN ASSET CONNECTOR]: No AssetServerURI configured");
                throw new InvalidOperationException();
            }

            bool storedInCache = false;
            string errorMessage = null;

            // AssetID handling
            if (asset.ID == UUID.Zero)
                asset.ID = UUID.Random();

            // Cache handling
            if (m_cache != null)
            {
                m_cache.Cache(asset);
                storedInCache = true;
            }

            // Local asset handling
            if ((asset.Flags & AssetFlags.Local) == AssetFlags.Local)
            {
                if (!storedInCache)
                {
                    MainConsole.Instance.Error("Cannot store local " + asset.TypeString + " asset without an asset cache");
                    asset.ID = UUID.Zero;
                }

                return asset.ID;
            }

            // Distinguish public and private assets
            bool isPublic = true;
            switch ((AssetType) asset.Type)
            {
                case AssetType.CallingCard:
                case AssetType.Gesture:
                case AssetType.LSLBytecode:
                case AssetType.LSLText:
                    isPublic = false;
                    break;
            }

            // Make sure ContentType is set
            if (String.IsNullOrEmpty(asset.TypeString))
                asset.TypeString = SLUtil.SLAssetTypeToContentType(asset.Type);

            // Build the remote storage request
            List<MultipartForm.Element> postParameters = new List<MultipartForm.Element>
                                                             {
                                                                 new MultipartForm.Parameter("AssetID",
                                                                                             asset.ID.ToString()),
                                                                 new MultipartForm.Parameter("CreatorID",
                                                                                             asset.CreatorID.ToString()),
                                                                 new MultipartForm.Parameter("Temporary",
                                                                                             ((asset.Flags &
                                                                                               AssetFlags.Temperary) ==
                                                                                              AssetFlags.Temperary)
                                                                                                 ? "1"
                                                                                                 : "0"),
                                                                 new MultipartForm.Parameter("Public",
                                                                                             isPublic ? "1" : "0"),
                                                                 new MultipartForm.File("Asset", asset.Name,
                                                                                        asset.TypeString, asset.Data)
                                                             };

            // Make the remote storage request
            try
            {
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(m_serverUrl);

                HttpWebResponse response = MultipartForm.Post(request, postParameters);
                using (Stream responseStream = response.GetResponseStream())
                {
                    string responseStr = null;

                    try
                    {
                        responseStr = responseStream.GetStreamString();
                        OSD responseOSD = OSDParser.Deserialize(responseStr);
                        if (responseOSD.Type == OSDType.Map)
                        {
                            OSDMap responseMap = (OSDMap) responseOSD;
                            if (responseMap["Success"].AsBoolean())
                                return asset.ID;
                            else
                                errorMessage = "Upload failed: " + responseMap["Message"].AsString();
                        }
                        else
                        {
                            errorMessage = "Response format was invalid:\n" + responseStr;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!String.IsNullOrEmpty(responseStr))
                            errorMessage = "Failed to parse the response:\n" + responseStr;
                        else
                            errorMessage = "Failed to retrieve the response: " + ex.Message;
                    }
                }
            }
            catch (WebException ex)
            {
                errorMessage = ex.Message;
            }

            MainConsole.Instance.WarnFormat("[SIMIAN ASSET CONNECTOR]: Failed to store asset \"{0}\" ({1}, {2}): {3}",
                             asset.Name, asset.ID, asset.TypeString, errorMessage);
            return UUID.Zero;
        }

        /// <summary>
        ///   Update an asset's content
        /// </summary>
        /// Attachments and bare scripts need this!!
        /// <param name = "id"> </param>
        /// <param name = "data"></param>
        /// <returns></returns>
        public bool UpdateContent(UUID id, byte[] data, out UUID newID)
        {
            newID = UUID.Zero;
            AssetBase asset = Get(id.ToString());

            if (asset == null)
            {
                MainConsole.Instance.Warn("[SIMIAN ASSET CONNECTOR]: Failed to fetch asset " + id + " for updating");
                return false;
            }

            asset.Data = data;

            UUID result = Store(asset);
            return result != UUID.Zero;
        }

        /// <summary>
        ///   Delete an asset
        /// </summary>
        /// <param name = "id"></param>
        /// <returns></returns>
        public bool Delete(UUID id)
        {
            if (String.IsNullOrEmpty(m_serverUrl))
            {
                MainConsole.Instance.Error("[SIMIAN ASSET CONNECTOR]: No AssetServerURI configured");
                throw new InvalidOperationException();
            }

            //string errorMessage = String.Empty;
            string url = m_serverUrl + id;

            if (m_cache != null)
                m_cache.Expire(id.ToString());

            try
            {
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
                request.Method = "DELETE";

                using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.NoContent)
                    {
                        MainConsole.Instance.Warn("[SIMIAN ASSET CONNECTOR]: Unexpected response when deleting asset " + url + ": " +
                                   response.StatusCode + " (" + response.StatusDescription + ")");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[SIMIAN ASSET CONNECTOR]: Failed to delete asset " + id + " from the asset service: " +
                           ex.Message);
                return false;
            }
        }

        #endregion IAssetService
    }
}