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
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Modules.Monitoring.Monitors
{
    public class AssetMonitor : IMonitor, IAssetMonitor
    {
        #region Declares

        private long assetCacheMemoryUsage;
        private TimeSpan assetRequestTimeAfterCacheMiss;
        private long assetServiceRequestFailures;
        private long assetsInCache;
        private long blockedMissingTextureRequests;
        private long textureCacheMemoryUsage;
        private long texturesInCache;

        /// <value>
        ///     Currently misleading since we can't currently subtract removed asset memory usage without a performance hit
        /// </value>
        public long AssetCacheMemoryUsage
        {
            get { return assetCacheMemoryUsage; }
        }

        /// <value>
        ///     Currently unused
        /// </value>
        public long TextureCacheMemoryUsage
        {
            get { return textureCacheMemoryUsage; }
        }

        /// <summary>
        ///     These statistics are being collected by push rather than pull.  Pull would be simpler, but I had the
        ///     notion of providing some flow statistics (which pull wouldn't give us).  Though admittedly these
        ///     haven't yet been implemented...
        /// </summary>
        public long AssetsInCache
        {
            get { return assetsInCache; }
        }

        /// <value>
        ///     Currently unused
        /// </value>
        public long TexturesInCache
        {
            get { return texturesInCache; }
        }

        /// <summary>
        ///     This is the time it took for the last asset request made in response to a cache miss.
        /// </summary>
        public TimeSpan AssetRequestTimeAfterCacheMiss
        {
            get { return assetRequestTimeAfterCacheMiss; }
        }

        /// <summary>
        ///     Number of persistent requests for missing textures we have started blocking from clients.  To some extent
        ///     this is just a temporary statistic to keep this problem in view - the root cause of this lies either
        ///     in a mishandling of the reply protocol, related to avatar appearance or may even originate in graphics
        ///     driver bugs on clients (though this seems less likely).
        /// </summary>
        public long BlockedMissingTextureRequests
        {
            get { return blockedMissingTextureRequests; }
        }

        /// <summary>
        ///     Record the number of times that an asset request has failed.  Failures are effectively exceptions, such as
        ///     request timeouts.  If an asset service replies that a particular asset cannot be found, this is not counted
        ///     as a failure
        /// </summary>
        public long AssetServiceRequestFailures
        {
            get { return assetServiceRequestFailures; }
        }

        #endregion

        #region Implementation of IMonitor

        #region IAssetMonitor Members

        public void AddAssetServiceRequestFailure()
        {
            assetServiceRequestFailures++;
        }

        public void AddAssetRequestTimeAfterCacheMiss(TimeSpan ts)
        {
            assetRequestTimeAfterCacheMiss = ts;
        }

        public void AddAsset(AssetBase asset)
        {
            if (asset != null && asset.Data != null)
            {
                if (asset.Type == (sbyte) AssetType.Texture)
                {
                    texturesInCache++;
                    // This could have been a pull stat, though there was originally a nebulous idea to measure flow rates
                    textureCacheMemoryUsage += asset.Data.Length;
                }
                else
                {
                    assetsInCache++;
                    assetCacheMemoryUsage += asset.Data.Length;
                }
            }
            else
                assetServiceRequestFailures++;
        }

        public void RemoveAsset(UUID uuid)
        {
            assetsInCache--;
        }

        /// <summary>
        ///     Signal that the asset cache has been cleared.
        /// </summary>
        public void ClearAssetCacheStatistics()
        {
            assetsInCache = 0;
            assetCacheMemoryUsage = 0;
            texturesInCache = 0;
            textureCacheMemoryUsage = 0;
        }

        public void AddBlockedMissingTextureRequest()
        {
            blockedMissingTextureRequests++;
        }

        #endregion

        #region IMonitor Members

        public double GetValue()
        {
            return 0;
        }

        public string GetName()
        {
            return "AssetMonitor";
        }

        public string GetFriendlyValue()
        {
            string Value = "";
            Value += "ASSET STATISTICS" + "\n";
            Value +=
                string.Format(
                    @"Asset cache contains   {0,6} non-texture assets using {1,10} K
Texture cache contains {2,6} texture     assets using {3,10} K
Latest asset request time after cache miss: {4}s
Blocked client requests for missing textures: {5}
Asset service request failures: {6}" +
                    Environment.NewLine,
                    AssetsInCache, Math.Round(AssetCacheMemoryUsage/1024.0),
                    TexturesInCache, Math.Round(TextureCacheMemoryUsage/1024.0),
                    assetRequestTimeAfterCacheMiss.Milliseconds/1000.0,
                    BlockedMissingTextureRequests,
                    AssetServiceRequestFailures);
            return Value;
        }

        public void ResetStats()
        {
        }

        #endregion

        #endregion
    }
}