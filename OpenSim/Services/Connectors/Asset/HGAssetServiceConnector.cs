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

using log4net;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Reflection;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Services.Interfaces;
using OpenSim.Services.Connectors.Hypergrid;
using OpenSim.Services.Connectors.SimianGrid;
using Aurora.Simulation.Base;

namespace OpenSim.Services.Connectors
{
    public class HGAssetServiceConnector : AssetServicesConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<string, IAssetService> m_connectors = new Dictionary<string, IAssetService>();

        public override string Name
        {
            get { return GetType().Name; }
        }

        public override void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AssetHandler", "") != Name)
                return; 
            m_log.Info("[HG ASSET SERVICE]: HG asset service enabled");

            IConfig assetConfig = config.Configs["AssetService"];
            if (assetConfig == null)
            {
                m_log.Error("[ASSET CONNECTOR]: AssetService missing from OpenSim.ini");
                throw new Exception("Asset connector init error");
            }

            string serviceURI = assetConfig.GetString("AssetServerURI",
                    String.Empty);

            if (serviceURI == String.Empty)
            {
                m_log.Error("[ASSET CONNECTOR]: No Server URI named in section AssetService");
                throw new Exception("Asset connector init error");
            }
            m_ServerURI = serviceURI;

            MainConsole.Instance.Commands.AddCommand("asset", false, "dump asset",
                                          "dump asset <id> <file>",
                                          "dump one cached asset", HandleDumpAsset);

            registry.RegisterInterface<IAssetService>(this);
        }

        public override void PostStart(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AssetHandler", "") != Name)
                return;

            string serviceURI = registry.Get<IAutoConfigurationService>().FindValueOf("AssetServerURI",
                    "AssetService");

            if (serviceURI == String.Empty)
            {
                m_log.Error("[ASSET CONNECTOR]: No Server URI named in section AssetService");
                throw new Exception("Asset connector init error");
            }
            m_ServerURI = serviceURI;

            SetCache(registry.Get<IImprovedAssetCache>());
        }

        public override void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AssetHandler", "") != Name)
                return;

            registry.RegisterInterface<IAssetService>(this);
        }

        private bool IsHG(string id)
        {
            Uri assetUri;

            if (Uri.TryCreate(id, UriKind.Absolute, out assetUri) &&
                    assetUri.Scheme == Uri.UriSchemeHttp)
                return true;

            return false;
        }

        private bool StringToUrlAndAssetID(string id, out string url, out string assetID)
        {
            url = String.Empty;
            assetID = String.Empty;

            Uri assetUri;

            if (Uri.TryCreate(id, UriKind.Absolute, out assetUri) &&
                    assetUri.Scheme == Uri.UriSchemeHttp)
            {
                url = "http://" + assetUri.Authority;
                assetID = assetUri.LocalPath.Trim(new char[] {'/'});
                return true;
            }

            return false;
        }

        private IAssetService GetConnector(string url)
        {
            IAssetService connector = null;
            lock (m_connectors)
            {
                if (m_connectors.ContainsKey(url))
                {
                    connector = m_connectors[url];
                }
                else
                {
                    // Still not as flexible as I would like this to be,
                    // but good enough for now
                    string connectorType = new HeloServicesConnector(url).Helo();
                    m_log.DebugFormat("[HG ASSET SERVICE]: HELO returned {0}", connectorType);
                    if (connectorType == "opensim-simian")
                    {
                        connector = new SimianAssetServiceConnector(url);
                    }
                    else
                        connector = new AssetServicesConnector(url);

                    m_connectors.Add(url, connector);
                }
            }
            return connector;
        }

        public override AssetBase Get(string id)
        {
            AssetBase asset = null;

            if (m_Cache != null)
            {
                asset = m_Cache.Get(id);

                if (asset != null)
                    return asset;
            }

            if (IsHG(id))
            {
                string url = string.Empty;
                string assetID = string.Empty;

                if (StringToUrlAndAssetID(id, out url, out assetID))
                {
                    IAssetService connector = GetConnector(url);
                    asset = connector.Get(assetID);
                }
                if (asset != null)
                {
                    // Now store it locally
                    // For now, let me just do it for textures and scripts
                    if (((AssetType)asset.Type == AssetType.Texture) ||
                        ((AssetType)asset.Type == AssetType.LSLBytecode) ||
                        ((AssetType)asset.Type == AssetType.LSLText))
                    {
                        base.Store(asset);
                    }
                }
            }
            else
                asset = base.Get(id);

            if (m_Cache != null)
                m_Cache.Cache(asset);

            return asset;
        }

        public override AssetBase GetCached(string id)
        {
            AssetBase b = base.GetCached(id);
            if (b != null)
                return b;

            if (IsHG(id))
            {
                string url = string.Empty;
                string assetID = string.Empty;

                if (StringToUrlAndAssetID(id, out url, out assetID))
                {
                    IAssetService connector = GetConnector(url);
                    return connector.GetCached(assetID);
                }
            }
            return null;
        }

        public override AssetMetadata GetMetadata(string id)
        {
            AssetBase asset = null;
            
            if (m_Cache != null)
            {
                if (m_Cache != null)
                    m_Cache.Get(id);

                if (asset != null)
                    return asset.Metadata;
            }

            AssetMetadata metadata = null;

            if (IsHG(id))
            {
                string url = string.Empty;
                string assetID = string.Empty;

                if (StringToUrlAndAssetID(id, out url, out assetID))
                {
                    IAssetService connector = GetConnector(url);
                    return connector.GetMetadata(assetID);
                }
            }
            else
                metadata = base.GetMetadata(id);

            return metadata;
        }

        public override bool Get(string id, Object sender, AssetRetrieved handler)
        {
            AssetBase asset = null;

            if (m_Cache != null)
                asset = m_Cache.Get(id);

            if (asset != null)
            {
                Util.FireAndForget(delegate { handler(id, sender, asset); });
                return true;
            }

            if (IsHG(id))
            {
                string url = string.Empty;
                string assetID = string.Empty;

                if (StringToUrlAndAssetID(id, out url, out assetID))
                {
                    IAssetService connector = GetConnector(url);
                    return connector.Get(assetID, sender, delegate(string newAssetID, Object s, AssetBase a)
                    {
                        if (m_Cache != null)
                            m_Cache.Cache(a);
                        handler(assetID, s, a);
                    });
                }
                return false;
            }
            else
            {
                return base.Get(id, sender, delegate(string assetID, Object s, AssetBase a)
                {
                    if (m_Cache != null)
                        m_Cache.Cache(a);
                    handler(assetID, s, a);
                });
            }
        }

        public override string Store(AssetBase asset)
        {
            bool isHG = IsHG(asset.ID);

            if ((m_Cache != null) && !isHG)
                // Don't store it in the cache if the asset is to 
                // be sent to the other grid, because this is already
                // a copy of the local asset.
                m_Cache.Cache(asset);

            if (asset.Temporary || asset.Local)
            {
                if (m_Cache != null)
                    m_Cache.Cache(asset);
                return asset.ID;
            }

            string id = string.Empty;
            if (IsHG(asset.ID))
            {
                string url = string.Empty;
                string assetID = string.Empty;

                if (StringToUrlAndAssetID(asset.ID, out url, out assetID))
                {
                    IAssetService connector = GetConnector(url);
                    // Restore the assetID to a simple UUID
                    asset.ID = assetID;
                    return connector.Store(asset);
                }
            }
            else
                id = base.Store(asset);

            if (id != String.Empty)
            {
                // Placing this here, so that this work with old asset servers that don't send any reply back
                // SynchronousRestObjectRequester returns somethins that is not an empty string
                if (id != null)
                    asset.ID = id;

                if (m_Cache != null)
                    m_Cache.Cache(asset);
            }
            return id;
        }
    }
}
