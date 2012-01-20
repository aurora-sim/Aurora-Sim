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
using System.Linq;
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.Connectors
{
    public class AssetServicesConnector : IAssetServiceConnector, IService
    {
        protected IImprovedAssetCache m_Cache;
        protected IRegistryCore m_registry;
        protected string m_serverURL = "";

        public AssetServicesConnector()
        {
        }

        public AssetServicesConnector(string url)
        {
            m_serverURL = url;
        }

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        #region IAssetServiceConnector Members

        public IAssetService InnerService
        {
            get { return this; }
        }

        public virtual bool GetExists(string id)
        {
            AssetBase asset = null;
            if (m_Cache != null)
                asset = m_Cache.Get(id);

            if (asset != null)
                return true;

            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string>(new string[1] {m_serverURL});
#if (!ISWIN)
            foreach (string m_ServerURI in serverURIs)
            {
                string uri = m_ServerURI + "/" + id + "/exists";

                bool exists = SynchronousRestObjectRequester.
                        MakeRequest<int, bool>("GET", uri, 0);
                if (exists)
                    return exists;
            }
            return false;
#else
            return serverURIs.Select(m_ServerURI => m_ServerURI + "/" + id + "/exists").Select(uri => SynchronousRestObjectRequester.MakeRequest<int, bool>("GET", uri, 0)).FirstOrDefault(exists => exists);
#endif
        }

        public virtual AssetBase Get(string id)
        {
            AssetBase asset = null;

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
#if (!ISWIN)
                foreach (string mServerUri in serverURIs)
                {
                    string uri = mServerUri + "/" + id;
                    asset = SynchronousRestObjectRequester.MakeRequest<int, AssetBase>("GET", uri, 0);

                    if (m_Cache != null && asset != null)
                        m_Cache.Cache(asset);
                    if (asset != null)
                        return asset;
                }
#else
                foreach (string uri in serverURIs.Select(m_ServerURI => m_ServerURI + "/" + id))
                {
                    asset = SynchronousRestObjectRequester.
                        MakeRequest<int, AssetBase>("GET", uri, 0);

                    if (m_Cache != null && asset != null)
                        m_Cache.Cache(asset);
                    if (asset != null)
                        return asset;
                }
#endif
            return null;
        }

        public virtual AssetBase GetCached(string id)
        {
            if (m_Cache != null)
                return m_Cache.Get(id);

            return null;
        }

        public virtual byte[] GetData(string id)
        {
            if (m_Cache != null)
            {
                AssetBase fullAsset = m_Cache.Get(id);

                if (fullAsset != null)
                    return fullAsset.Data;
            }

            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string>(new string[1] {m_serverURL});
#if (!ISWIN)
            foreach (string mServerUri in serverURIs)
            {
                RestClient rc = new RestClient(mServerUri);
                rc.AddResourcePath("assets");
                rc.AddResourcePath(id);
                rc.AddResourcePath("data");

                rc.RequestMethod = "GET";

                Stream s = rc.Request();

                if (s == null)
                    return null;

                if (s.Length > 0)
                {
                    byte[] ret = new byte[s.Length];
                    s.Read(ret, 0, (int) s.Length);

                    return ret;
                }
            }
#else
            foreach (RestClient rc in serverURIs.Select(m_ServerURI => new RestClient(m_ServerURI)))
            {
                rc.AddResourcePath("assets");
                rc.AddResourcePath(id);
                rc.AddResourcePath("data");

                rc.RequestMethod = "GET";

                Stream s = rc.Request();

                if (s == null)
                    return null;

                if (s.Length > 0)
                {
                    byte[] ret = new byte[s.Length];
                    s.Read(ret, 0, (int) s.Length);

                    return ret;
                }
            }
#endif

            return null;
        }

        public virtual bool Get(string id, Object sender, AssetRetrieved handler)
        {
            if ((m_Cache != null) && m_Cache.Contains(id))
            {
                Util.FireAndForget(delegate
                {
                    handler(id, sender, m_Cache.Get(id));
                });
                return true;
            }

            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string>(new string[1] { m_serverURL });
            foreach (string m_ServerURI in serverURIs)
            {
                string uri = m_ServerURI + "/" + id;
                bool result = false;

                AsynchronousRestObjectRequester.
                    MakeRequest("GET", uri, 0,
                                delegate(AssetBase a)
                                {
                                    if (m_Cache != null)
                                        m_Cache.Cache(a);
                                    handler(id, sender, a);
                                    result = true;
                                });

                if (result)
                    return result;
            }

            return false;
        }

        public virtual UUID Store(AssetBase asset)
        {
            if ((asset.Flags & AssetFlags.Local) == AssetFlags.Local)
            {
                if (m_Cache != null)
                    m_Cache.Cache(asset);

                return asset.ID;
            }

            UUID newID = UUID.Zero;
            string request = asset.CompressedPack();
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string>(new string[1] {m_serverURL});
            foreach (string mServerUri in serverURIs)
            {
                string resp = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri + "/", request);
                if (resp == "")
                    continue;
                if (UUID.TryParse(resp, out newID))
                {
                    // Placing this here, so that this work with old asset servers that don't send any reply back
                    // SynchronousRestObjectRequester returns somethins that is not an empty string
                    asset.ID = newID;

                    if (m_Cache != null)
                        m_Cache.Cache(asset);
                }
            }
            return newID;
        }

        public virtual bool UpdateContent(UUID id, byte[] data, out UUID newID)
        {
            OSDMap map = new OSDMap();
            map["Data"] = data;
            map["ID"] = id;
            map["Method"] = "UpdateContent";
            string request = Util.Compress(OSDParser.SerializeJsonString(map));

            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string>(new string[1] {m_serverURL});

#if (!ISWIN)
            foreach (string mServerUri in serverURIs)
            {
                string resp = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri + "/", request);
                if (resp == "")
                    continue;
                if ((UUID.TryParse(resp, out newID)) && (newID != UUID.Zero))
                    return true;
            }
#else
            foreach (string resp in serverURIs.Select(mServerUri => SynchronousRestFormsRequester.MakeRequest("POST", mServerUri + "/", request)).Where(resp => resp != ""))
            {
                if ((UUID.TryParse(resp, out newID)) && (newID != UUID.Zero))
                    return true;
            }
#endif
            newID = UUID.Zero;
            return false;
        }

        public virtual bool Delete(UUID id)
        {
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string>(new string[1] {m_serverURL});
#if (!ISWIN)
            foreach (string mServerUri in serverURIs)
            {
                string uri = mServerUri + "/" + id;
                SynchronousRestObjectRequester.MakeRequest<int, bool>("DELETE", uri, 0);
            }
#else
            foreach (string uri in serverURIs.Select(m_ServerURI => m_ServerURI + "/" + id))
            {
                SynchronousRestObjectRequester.
                    MakeRequest<int, bool>("DELETE", uri, 0);
            }
#endif
            if (m_Cache != null)
                m_Cache.Expire(id.ToString());

            return true;
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
            SetCache(registry.RequestModuleInterface<IImprovedAssetCache>());
        }

        public void FinishedStartup()
        {
        }

        public void Configure(IConfigSource config, IRegistryCore registry)
        {
        }

        #endregion

        #region IService Members

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
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

        #endregion

        protected void SetCache(IImprovedAssetCache cache)
        {
            m_Cache = cache;
        }

        protected virtual void HandleDumpAsset(string[] args)
        {
            if (args.Length != 4)
            {
                MainConsole.Instance.Info("Syntax: dump asset <id> <file>");
                return;
            }

            UUID assetID;

            if (!UUID.TryParse(args[2], out assetID))
            {
                MainConsole.Instance.Info("Invalid asset ID");
                return;
            }

            if (m_Cache == null)
            {
                MainConsole.Instance.Info("Instance uses no cache");
                return;
            }

            AssetBase asset = m_Cache.Get(assetID.ToString());

            if (asset == null)
            {
                MainConsole.Instance.Info("Asset not found in cache");
                return;
            }

            string fileName = args[3];

            FileStream fs = File.Create(fileName);
            fs.Write(asset.Data, 0, asset.Data.Length);

            fs.Close();
        }
    }
}