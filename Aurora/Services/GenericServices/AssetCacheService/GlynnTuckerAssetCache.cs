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

using Aurora.Framework;
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Assets;
using GlynnTucker.Cache;
using Nini.Config;
using System;

namespace Aurora.Services
{
    public class GlynnTuckerAssetCache : IService, IImprovedAssetCache
    {
        #region Declares

        private ICache m_Cache;
        private uint m_DebugRate;
        private ulong m_Hits;
        private ulong m_Requests;

        // Instrumentation

        public string Name
        {
            get { return "GlynnTuckerAssetCache"; }
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig moduleConfig = config.Configs["Modules"];

            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("AssetCaching");
                //MainConsole.Instance.DebugFormat("[ASSET CACHE] name = {0} (this module's name: {1}). Sync? ", name, Name, m_Cache.IsSynchronized);

                if (name == Name)
                {
                    m_Cache = new SimpleMemoryCache();

                    MainConsole.Instance.Info("[ASSET CACHE]: GlynnTucker asset cache enabled");

                    // Instrumentation
                    IConfig cacheConfig = config.Configs["AssetCache"];
                    if (cacheConfig != null)
                        m_DebugRate = (uint) cacheConfig.GetInt("DebugRate", 0);
                    registry.RegisterModuleInterface<IImprovedAssetCache>(this);
                }
            }
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region IImprovedAssetCache

        ////////////////////////////////////////////////////////////
        // IImprovedAssetCache
        //

        public void Cache(string assetID, AssetBase asset)
        {
            if (asset != null)
                m_Cache.AddOrUpdate(asset.IDString, asset);
        }

        public void CacheData(string assetID, byte[] asset)
        {
        }

        public AssetBase Get(string id)
        {
            bool found;
            return Get(id, out found);
        }

        public AssetBase Get(string id, out bool found)
        {
            Object asset = null;
            m_Cache.TryGet(id, out asset);
            found = asset != null;
            Debug(asset);

            return (AssetBase) asset;
        }

        public byte[] GetData(string id, out bool found)
        {
            found = false;
            return null;
        }

        public void Expire(string id)
        {
            Object asset = null;
            if (m_Cache.TryGet(id, out asset))
                m_Cache.Remove(id);
        }

        public void Clear()
        {
            m_Cache.Clear();
        }

        public bool Contains(string id)
        {
            return m_Cache.Contains(id);
        }

        private void Debug(Object asset)
        {
            // Temporary instrumentation to measure the hit/miss rate
            if (m_DebugRate > 0)
            {
                ++m_Requests;
                if (asset != null)
                    ++m_Hits;

                if ((m_Requests%m_DebugRate) == 0)
                    MainConsole.Instance.DebugFormat("[ASSET CACHE]: Hit Rate {0} / {1} == {2}%", m_Hits, m_Requests,
                                                     (m_Hits/(float) m_Requests)*100.0f);
            }
            // End instrumentation
        }

        #endregion
    }
}