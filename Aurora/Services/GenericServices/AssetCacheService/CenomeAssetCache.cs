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
using Aurora.Framework.Utilities;
using Nini.Config;
using System;

namespace Aurora.Services
{
    /// <summary>
    ///     Cenome memory asset cache.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Cache is enabled by setting "AssetCaching" configuration to value "CenomeMemoryAssetCache".
    ///         When cache is successfully enable log should have message
    ///         "[ASSET CACHE]: Cenome asset cache enabled (MaxSize = XXX bytes, MaxCount = XXX, ExpirationTime = XXX)".
    ///     </para>
    ///     <para>
    ///         Cache's size is limited by two parameters:
    ///         maximal allowed size in bytes and maximal allowed asset count. When new asset
    ///         is added to cache that have achieved either size or count limitation, cache
    ///         will automatically remove less recently used assets from cache. Additionally
    ///         asset's lifetime is controlled by expiration time.
    ///     </para>
    ///     <para>
    ///         <list type="table">
    ///             <listheader>
    ///                 <term>Configuration</term>
    ///                 <description>Description</description>
    ///             </listheader>
    ///             <item>
    ///                 <term>MaxSize</term>
    ///                 <description>Maximal size of the cache in bytes. Default value: 128MB (134 217 728 bytes).</description>
    ///             </item>
    ///             <item>
    ///                 <term>MaxCount</term>
    ///                 <description>Maximal count of assets stored to cache. Default value: 4096 assets.</description>
    ///             </item>
    ///             <item>
    ///                 <term>ExpirationTime</term>
    ///                 <description>Asset's expiration time in minutes. Default value: 30 minutes.</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     Enabling Cenome Asset Cache:
    ///     <code>
    ///     [Modules]
    ///     AssetCaching = "CenomeMemoryAssetCache"
    ///   </code>
    ///     Setting size and expiration time limitations:
    ///     <code>
    ///     [AssetCache]
    ///     ; 256 MB (default: 134217728)
    ///     MaxSize =  268435456
    ///     ; How many assets it is possible to store cache (default: 4096)
    ///     MaxCount = 16384
    ///     ; Expiration time - 1 hour (default: 30 minutes)
    ///     ExpirationTime = 60
    ///   </code>
    /// </example>
    public class CenomeMemoryAssetCache : IImprovedAssetCache, IService
    {
        #region Declares

        /// <summary>
        ///     Cache's default maximal asset count.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Assuming that average asset size is about 32768 bytes.
        ///     </para>
        /// </remarks>
        public const int DefaultMaxCount = 4096;

        /// <summary>
        ///     Default maximal size of the cache in bytes
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         128MB = 128 * 1024^2 = 134 217 728 bytes.
        ///     </para>
        /// </remarks>
        public const long DefaultMaxSize = 134217728;

        /// <summary>
        ///     Asset's default expiration time in the cache.
        /// </summary>
        public static readonly TimeSpan DefaultExpirationTime = TimeSpan.FromMinutes(30.0);

        /// <summary>
        ///     Cache object.
        /// </summary>
        private ICnmCache<string, AssetBase> m_cache;

        /// <summary>
        ///     Count of cache commands
        /// </summary>
        private int m_cachedCount;

        /// <summary>
        ///     How many gets before dumping statistics
        /// </summary>
        /// <remarks>
        ///     If 0 or less, then disabled.
        /// </remarks>
        private int m_debugEpoch;

        /// <summary>
        ///     Count of get requests
        /// </summary>
        private int m_getCount;

        /// <summary>
        ///     How many hits
        /// </summary>
        private int m_hitCount;

        #endregion

        /// <summary>
        ///     Gets region module's name.
        /// </summary>
        public string Name
        {
            get { return "CenomeMemoryAssetCache"; }
        }

        #region IImprovedAssetCache Members

        /// <summary>
        ///     Cache asset.
        /// </summary>
        /// <param name="assetID"></param>
        /// <param name="asset">
        ///     The asset that is being cached.
        /// </param>
        public void Cache(string assetID, AssetBase asset)
        {
            if (asset != null)
            {
//                MainConsole.Instance.DebugFormat("[CENOME ASSET CACHE]: Caching asset {0}", asset.IDString);

                long size = asset.Data != null ? asset.Data.Length : 1;
                m_cache.Set(asset.IDString, asset, size);
                m_cachedCount++;
            }
        }

        public void CacheData(string assetID, byte[] asset)
        {
        }

        /// <summary>
        ///     Clear asset cache.
        /// </summary>
        public void Clear()
        {
            m_cache.Clear();
        }

        public bool Contains(string id)
        {
            return m_cache.Contains(id);
        }

        /// <summary>
        ///     Expire (remove) asset stored to cache.
        /// </summary>
        /// <param name="id">
        ///     The expired asset's id.
        /// </param>
        public void Expire(string id)
        {
            m_cache.Remove(id);
        }

        /// <summary>
        ///     Get asset stored
        /// </summary>
        /// <param name="id">
        ///     The asset's id.
        /// </param>
        /// <returns>
        ///     Asset if it is found from cache; otherwise <see langword="null" />.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Caller should always check that is return value <see langword="null" />.
        ///         Cache doesn't guarantee in any situation that asset is stored to it.
        ///     </para>
        /// </remarks>
        public AssetBase Get(string id)
        {
            bool found;
            return Get(id, out found);
        }

        public byte[] GetData(string id, out bool found)
        {
            found = false;
            return null;
        }

        public AssetBase Get(string id, out bool found)
        {
            m_getCount++;
            found = false;
            AssetBase assetBase;
            if (m_cache.TryGetValue(id, out assetBase))
            {
                found = true;
                m_hitCount++;
            }

            if (m_getCount == m_debugEpoch)
            {
                MainConsole.Instance.DebugFormat(
                    "[ASSET CACHE]: Cached = {0}, Get = {1}, Hits = {2}%, Size = {3} bytes, Avg. A. Size = {4} bytes",
                    m_cachedCount,
                    m_getCount,
                    ((double) m_hitCount/m_getCount)*100.0,
                    m_cache.Size,
                    m_cache.Size/m_cache.Count);
                m_getCount = 0;
                m_hitCount = 0;
                m_cachedCount = 0;
            }

//            if (null == assetBase)
//                MainConsole.Instance.DebugFormat("[CENOME ASSET CACHE]: Asset {0} not in cache", id);

            return assetBase;
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_cache = null;

            IConfig moduleConfig = config.Configs["Modules"];
            if (moduleConfig == null)
                return;

            string name = moduleConfig.GetString("AssetCaching");
            //MainConsole.Instance.DebugFormat("[XXX] name = {0} (this module's name: {1}", name, Name);

            if (name != Name)
                return;

            long maxSize = DefaultMaxSize;
            int maxCount = DefaultMaxCount;
            TimeSpan expirationTime = DefaultExpirationTime;

            IConfig assetConfig = config.Configs["AssetCache"];
            if (assetConfig != null)
            {
                // Get optional configurations
                maxSize = assetConfig.GetLong("MaxSize", DefaultMaxSize);
                maxCount = assetConfig.GetInt("MaxCount", DefaultMaxCount);
                expirationTime =
                    TimeSpan.FromMinutes(assetConfig.GetInt("ExpirationTime", (int) DefaultExpirationTime.TotalMinutes));

                // Debugging purposes only
                m_debugEpoch = assetConfig.GetInt("DebugEpoch", 0);
            }

            Initialize(maxSize, maxCount, expirationTime);
            registry.RegisterModuleInterface<IImprovedAssetCache>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion

        /// <summary>
        ///     Initialize asset cache module, with custom parameters.
        /// </summary>
        /// <param name="maximalSize">
        ///     Cache's maximal size in bytes.
        /// </param>
        /// <param name="maximalCount">
        ///     Cache's maximal count of assets.
        /// </param>
        /// <param name="expirationTime">
        ///     Asset's expiration time.
        /// </param>
        protected void Initialize(long maximalSize, int maximalCount, TimeSpan expirationTime)
        {
            if (maximalSize <= 0 || maximalCount <= 0)
            {
                //MainConsole.Instance.Debug("[ASSET CACHE]: Cenome asset cache is not enabled.");
                return;
            }

            if (expirationTime <= TimeSpan.Zero)
            {
                // Disable expiration time
                expirationTime = TimeSpan.MaxValue;
            }

            // Create cache and add synchronization wrapper over it
            m_cache =
                CnmSynchronizedCache<string, AssetBase>.Synchronized(new CnmMemoryCache<string, AssetBase>(
                                                                         maximalSize, maximalCount, expirationTime));
            MainConsole.Instance.DebugFormat(
                "[ASSET CACHE]: Cenome asset cache enabled (MaxSize = {0} bytes, MaxCount = {1}, ExpirationTime = {2})",
                maximalSize,
                maximalCount,
                expirationTime);
        }
    }
}