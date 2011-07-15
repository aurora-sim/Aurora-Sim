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

using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Aurora.Simulation.Base;

namespace OpenSim.Services.Connectors
{
    public class AssetServicesConnector : IAssetServiceConnector, IService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private IRegistryCore m_registry;
        protected IImprovedAssetCache m_Cache = null;
        private string m_serverURL = "";

        public AssetServicesConnector ()
        {
        }

        public AssetServicesConnector (string url)
        {
            m_serverURL = url;
        }

        public IAssetService InnerService
        {
            get { return this; }
        }

        protected void SetCache(IImprovedAssetCache cache)
        {
            m_Cache = cache;
        }

        public virtual bool GetExists(string id)
        {
            AssetBase asset = null;
            if (m_Cache != null)
                asset = m_Cache.Get(id);

            if (asset != null)
                return true;

            List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string> (new string[1] { m_serverURL });
            foreach (string m_ServerURI in serverURIs)
            {
                string uri = m_ServerURI + "/" + id + "/exists";

                bool exists = SynchronousRestObjectRequester.
                        MakeRequest<int, bool>("GET", uri, 0);
                if (exists)
                    return exists;
            }

            return false;
        }

        public virtual AssetBase Get(string id)
        {
            AssetBase asset = null;
            List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string> (new string[1] { m_serverURL });
            foreach (string m_ServerURI in serverURIs)
            {
                string uri = m_ServerURI + "/" + id;

                if (m_Cache != null)
                    asset = m_Cache.Get(id);

                if (asset == null)
                {
                    asset = SynchronousRestObjectRequester.
                            MakeRequest<int, AssetBase>("GET", uri, 0);

                    if (m_Cache != null && asset != null)
                        m_Cache.Cache(asset);
                }
                if (asset != null)
                    return asset;
            }
            return asset;
        }

        public virtual AssetBase GetCached(string id)
        {
            if (m_Cache != null)
                return m_Cache.Get(id);

            return null;
        }

        public virtual AssetBase GetMetadata(string id)
        {
            if (m_Cache != null)
            {
                AssetBase fullAsset = m_Cache.Get(id);

                if (fullAsset != null)
                {
                    // thinking I should not include the data since this use to request metadata
                    fullAsset.Data = new byte[]{};
                    return fullAsset;
                }
            }

            List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string> (new string[1] { m_serverURL });
            foreach (string m_ServerURI in serverURIs)
            {
                string uri = m_ServerURI + "/" + id + "/metadata";

                AssetBase asset = SynchronousRestObjectRequester.
                        MakeRequest<int, AssetBase>("GET", uri, 0);
                if (asset != null)
                    return asset;
            }
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

            List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string> (new string[1] { m_serverURL });
            foreach (string m_ServerURI in serverURIs)
            {
                RestClient rc = new RestClient(m_ServerURI);
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
                    s.Read(ret, 0, (int)s.Length);

                    return ret;
                }
            }

            return null;
        }

        public virtual bool Get(string id, Object sender, AssetRetrieved handler)
        {
            List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string> (new string[1] { m_serverURL });
            foreach (string m_ServerURI in serverURIs)
            {
                string uri = m_ServerURI + "/" + id;

                AssetBase asset = null;
                if (m_Cache != null)
                    asset = m_Cache.Get(id);

                if (asset == null)
                {
                    bool result = false;

                    AsynchronousRestObjectRequester.
                            MakeRequest<int, AssetBase>("GET", uri, 0,
                            delegate(AssetBase a)
                            {
                                if (m_Cache != null)
                                    m_Cache.Cache(a);
                                handler(id, sender, a);
                                result = true;
                            });

                    if(result)
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

        public virtual UUID Store(AssetBase asset)
        {
            if ((asset.Flags & AssetFlags.Local) == AssetFlags.Local)
            {
                if (m_Cache != null)
                    m_Cache.Cache(asset);

                return asset.ID;
            }

            UUID newID = UUID.Zero;
            List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string> (new string[1] { m_serverURL });
            foreach (string m_ServerURI in serverURIs)
            {
                string uri = m_ServerURI + "/";

                try
                {
                    UUID.TryParse(SynchronousRestObjectRequester.
                            MakeRequest<AssetBase, string>("POST", uri, asset), out newID);
                }
                catch (Exception e)
                {
                    m_log.WarnFormat("[ASSET CONNECTOR]: Unable to send asset {0} to asset server. Reason: {1}", asset.ID, e.Message);
                }

                if (newID != UUID.Zero)
                {
                    // Placing this here, so that this work with old asset servers that don't send any reply back
                    // SynchronousRestObjectRequester returns somethins that is not an empty string
                    asset.ID =  newID;

                    if (m_Cache != null)
                        m_Cache.Cache(asset);
                }
            }
            return newID;
        }

        public virtual bool UpdateContent(UUID id, byte[] data)
        {
            AssetBase asset = null;

            if (m_Cache != null)
                asset = m_Cache.Get(id.ToString());

            if (asset == null)
            {
                asset = GetMetadata(id.ToString());
                if (asset == null)
                    return false;
            }
            asset.Data = data;

            List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string> (new string[1] { m_serverURL });
            foreach (string m_ServerURI in serverURIs)
            {
                string uri = m_ServerURI + "/" + id;

                if (SynchronousRestObjectRequester.
                        MakeRequest<AssetBase, bool>("POST", uri, asset))
                {
                    if (m_Cache != null)
                        m_Cache.Cache(asset);

                    return true;
                }
            }
            return false;
        }

        public virtual bool Delete(UUID id)
        {
            List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AssetServerURI");
            if (m_serverURL != string.Empty)
                serverURIs = new List<string> (new string[1] { m_serverURL });
            foreach (string m_ServerURI in serverURIs)
            {
                string uri = m_ServerURI + "/" + id;

                SynchronousRestObjectRequester.
                        MakeRequest<int, bool>("DELETE", uri, 0);
            }
            if (m_Cache != null)
                m_Cache.Expire(id.ToString());

            return true;
        }

        protected virtual void HandleDumpAsset(string[] args)
        {
            if (args.Length != 4)
            {
                m_log.Info ("Syntax: dump asset <id> <file>");
                return;
            }

            UUID assetID;

            if (!UUID.TryParse(args[2], out assetID))
            {
                m_log.Info ("Invalid asset ID");
                return;
            }

            if (m_Cache == null)
            {
                m_log.Info ("Instance uses no cache");
                return;
            }

            AssetBase asset = m_Cache.Get(assetID.ToString());

            if (asset == null)
            {
                m_log.Info ("Asset not found in cache");
                return;
            }

            string fileName = args[3];

            FileStream fs = File.Create(fileName);
            fs.Write(asset.Data, 0, asset.Data.Length);

            fs.Close();
        }

        #region IService Members

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            registry.RegisterModuleInterface<IAssetServiceConnector> (this);

            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AssetHandler", "") != Name)
                return;

            if (MainConsole.Instance != null)
                MainConsole.Instance.Commands.AddCommand ("dump asset",
                                          "dump asset <id> <file>",
                                          "dump one cached asset", HandleDumpAsset);

            registry.RegisterModuleInterface<IAssetService>(this);
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
            SetCache (registry.RequestModuleInterface<IImprovedAssetCache> ());
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}
