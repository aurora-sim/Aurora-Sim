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
using System.Reflection;
using Aurora.DataManager;
using Nini.Config;
using log4net;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Simulation.Base;

namespace OpenSim.Services.AssetService
{
    public class AssetService : IAssetService, IService
    {
        private static readonly ILog m_Log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
        protected IRegistryCore m_registry;
        protected IAssetDataPlugin m_database;

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        public IAssetService InnerService
        {
            get { return this; }
        }

        public virtual void Initialize (IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AssetHandler", "") != Name)
                return;
            Configure(config, registry);
        }

        public virtual void Configure (IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;

            m_database = DataManager.RequestPlugin<IAssetDataPlugin>();
            if (m_database == null)
                throw new Exception("Could not find a storage interface in the given module");

            registry.RegisterModuleInterface<IAssetService>(this);

            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand("show digest",
                        "show digest <ID>",
                        "Show asset digest", HandleShowDigest);

                MainConsole.Instance.Commands.AddCommand("delete asset",
                        "delete asset <ID>",
                        "Delete asset from database", HandleDeleteAsset);
            }

            m_Log.Debug("[ASSET SERVICE]: Local asset service enabled");
        }

        public virtual void Start (IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual void FinishedStartup ()
        {
        }

        public virtual AssetBase Get (string id)
        {
            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (cache != null)
            {
                AssetBase cachedAsset = cache.Get(id);
                if (cachedAsset != null)
                    return cachedAsset;
            }
            AssetBase asset = m_database.GetAsset(UUID.Parse(id));
            if (cache != null && asset != null)
                cache.Cache(asset);
            return asset;
        }

        public virtual AssetBase GetCached (string id)
        {
            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (cache != null)
                return cache.Get(id);
            return null;
        }

        public virtual byte[] GetData (string id)
        {
            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (cache != null)
            {
                AssetBase cachedAsset = cache.Get(id);
                if (cachedAsset != null)
                    return cachedAsset.Data;
            }
            AssetBase asset = m_database.GetAsset(UUID.Parse(id));
            if (cache != null && asset != null)
                cache.Cache(asset);
            if (asset != null) return asset.Data;
            return new byte[] { };
        }

        public virtual bool GetExists (string id)
        {
            return m_database.ExistsAsset(UUID.Parse(id));
        }

        public virtual bool Get (String id, Object sender, AssetRetrieved handler)
        {
            //m_log.DebugFormat("[AssetService]: Get asset async {0}", id);

            AssetBase asset = m_database.GetAsset(UUID.Parse(id));
            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (cache != null && asset != null)
                cache.Cache(asset);

            //m_log.DebugFormat("[AssetService]: Got asset {0}", asset);

            handler(id, sender, asset);

            return true;
        }

        public virtual UUID Store (AssetBase asset)
        {
            //m_log.DebugFormat("[ASSET SERVICE]: Store asset {0} {1}", asset.Name, asset.ID);
            asset.ID = m_database.Store(asset);
            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (cache != null && asset != null)
            {
                cache.Expire(asset.ID.ToString());
                cache.Cache(asset);
            }

            return asset != null ? asset.ID : UUID.Zero;
        }

        public virtual bool UpdateContent (UUID id, byte[] data)
        {
            m_database.UpdateContent(id, data);
            return true;
        }

        public virtual bool Delete (UUID id)
        {
            m_Log.DebugFormat("[ASSET SERVICE]: Deleting asset {0}", id);
            AssetBase asset = m_database.GetAsset(id);
            if (asset == null)
                return false;

            if ((int)(asset.Flags & AssetFlags.Maptile) != 0 || //Depriated, use Deletable instead
                (int)(asset.Flags & AssetFlags.Deletable) != 0)
            {
                return m_database.Delete(id);
            }
            m_Log.DebugFormat("[ASSET SERVICE]: Request to delete asset {0}, but flags are not Maptile", id);

            return false;
        }

        void HandleShowDigest(string[] args)
        {
            if (args.Length < 3)
            {
                m_Log.Info("Syntax: show digest <ID>");
                return;
            }

            AssetBase asset = Get(args[2]);

            if (asset == null || asset.Data.Length == 0)
            {
                m_Log.Info("Asset not found");
                return;
            }

            int i;

            m_Log.Info(String.Format("Name: {0}", asset.Name));
            m_Log.Info(String.Format("Description: {0}", asset.Description));
            m_Log.Info(String.Format("Type: {0}", asset.TypeAsset));
            m_Log.Info(String.Format("Content-type: {0}", asset.TypeAsset.ToString()));
            m_Log.Info(String.Format("Flags: {0}", asset.Flags));

            for (i = 0; i < 5; i++)
            {
                int off = i * 16;
                if (asset.Data.Length <= off)
                    break;
                int len = 16;
                if (asset.Data.Length < off + len)
                    len = asset.Data.Length - off;

                byte[] line = new byte[len];
                Array.Copy(asset.Data, off, line, 0, len);

                string text = BitConverter.ToString(line);
                m_Log.Info(String.Format("{0:x4}: {1}", off, text));
            }
        }

        void HandleDeleteAsset(string[] args)
        {
            if (args.Length < 3)
            {
                m_Log.Info("Syntax: delete asset <ID>");
                return;
            }

            AssetBase asset = Get(args[2]);

            if (asset == null || asset.Data.Length == 0)
            {
                m_Log.Info("Asset not found");
                return;
            }

            Delete(UUID.Parse(args[2]));

            m_Log.Info("Asset deleted");
        }
    }
}
