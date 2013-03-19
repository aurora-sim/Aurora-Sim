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
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Assets;
using Aurora.Framework.Utilities;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Aurora.Modules.Archivers
{
    /// <summary>
    ///     Encapsulate the asynchronous requests for the assets required for an archive operation
    /// </summary>
    internal class AssetsRequest
    {
        /// <value>
        ///     Timeout threshold if we still need assets or missing asset notifications but have stopped receiving them
        ///     from the asset service
        /// </value>
        protected const int TIMEOUT = 60*1000;

        /// <value>
        ///     If a timeout does occur, limit the amount of UUID information put to the console.
        /// </value>
        protected const int MAX_UUID_DISPLAY_ON_TIMEOUT = 3;

        /// <value>
        ///     Record the number of asset replies required so we know when we've finished
        /// </value>
        private readonly int m_repliesRequired;

        /// <value>
        ///     Asset service used to request the assets
        /// </value>
        protected IAssetService m_assetService;

        protected AssetsArchiver m_assetsArchiver;

        /// <value>
        ///     Callback used when all the assets requested have been received.
        /// </value>
        protected AssetsRequestCallback m_assetsRequestCallback;

        /// <value>
        ///     List of assets that were found.  This will be passed back to the requester.
        /// </value>
        protected List<UUID> m_foundAssetUuids = new List<UUID>();

        /// <value>
        ///     Maintain a list of assets that could not be found.  This will be passed back to the requester.
        /// </value>
        protected List<UUID> m_notFoundAssetUuids = new List<UUID>();

        protected Timer m_requestCallbackTimer;

        /// <value>
        ///     State of this request
        /// </value>
        private RequestState m_requestState = RequestState.Initial;

        /// <value>
        ///     uuids to request
        /// </value>
        protected IDictionary<UUID, AssetType> m_uuids;

        protected internal AssetsRequest(
            AssetsArchiver assetsArchiver, IDictionary<UUID, AssetType> uuids,
            IAssetService assetService, AssetsRequestCallback assetsRequestCallback)
        {
            m_assetsArchiver = assetsArchiver;
            m_uuids = uuids;
            m_assetsRequestCallback = assetsRequestCallback;
            m_assetService = assetService;
            m_repliesRequired = uuids.Count;

            m_requestCallbackTimer = new Timer(TIMEOUT) {AutoReset = false};
            m_requestCallbackTimer.Elapsed += OnRequestCallbackTimeout;
        }

        protected internal void Execute()
        {
            m_requestState = RequestState.Running;

            MainConsole.Instance.DebugFormat("[ARCHIVER]: AssetsRequest executed looking for {0} assets",
                                             m_repliesRequired);

            // We can stop here if there are no assets to fetch
            if (m_repliesRequired == 0)
            {
                m_requestState = RequestState.Completed;
                PerformAssetsRequestCallback(null);
                return;
            }

            foreach (KeyValuePair<UUID, AssetType> kvp in m_uuids)
            {
                m_assetService.Get(kvp.Key.ToString(), kvp.Value, PreAssetRequestCallback);
            }

            m_requestCallbackTimer.Enabled = true;
        }

        protected void OnRequestCallbackTimeout(object source, ElapsedEventArgs args)
        {
            try
            {
                lock (this)
                {
                    // Take care of the possibilty that this thread started but was paused just outside the lock before
                    // the final request came in (assuming that such a thing is possible)
                    if (m_requestState == RequestState.Completed)
                        return;

                    m_requestState = RequestState.Aborted;
                }

                // Calculate which uuids were not found.  This is an expensive way of doing it, but this is a failure
                // case anyway.
                List<UUID> uuids = m_uuids.Keys.ToList();

                foreach (UUID uuid in m_foundAssetUuids)
                {
                    uuids.Remove(uuid);
                }

                foreach (UUID uuid in m_notFoundAssetUuids)
                {
                    uuids.Remove(uuid);
                }

                MainConsole.Instance.ErrorFormat(
                    "[ARCHIVER]: Asset service failed to return information about {0} requested assets", uuids.Count);

                int i = 0;
                foreach (UUID uuid in uuids)
                {
                    MainConsole.Instance.ErrorFormat("[ARCHIVER]: No information about asset {0} received", uuid);

                    if (++i >= MAX_UUID_DISPLAY_ON_TIMEOUT)
                        break;
                }

                if (uuids.Count > MAX_UUID_DISPLAY_ON_TIMEOUT)
                    MainConsole.Instance.ErrorFormat(
                        "[ARCHIVER]: (... {0} more not shown)", uuids.Count - MAX_UUID_DISPLAY_ON_TIMEOUT);

                MainConsole.Instance.Error("[ARCHIVER]: OAR save aborted.");
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[ARCHIVER]: Timeout handler exception {0}", e);
            }
            finally
            {
                m_assetsArchiver.ForceClose();
            }
        }

        protected void PreAssetRequestCallback(string fetchedAssetID, object assetType, AssetBase fetchedAsset)
        {
            // Check for broken asset types and fix them with the AssetType gleaned by UuidGatherer
            if (fetchedAsset != null && fetchedAsset.Type == (sbyte) AssetType.Unknown)
            {
                AssetType type = (AssetType) assetType;
                MainConsole.Instance.InfoFormat("[ARCHIVER]: Rewriting broken asset type for {0} to {1}",
                                                fetchedAsset.ID, type);
                fetchedAsset.Type = (sbyte) type;
            }

            AssetRequestCallback(fetchedAssetID, this, fetchedAsset);
        }

        /// <summary>
        ///     Called back by the asset cache when it has the asset
        /// </summary>
        /// <param name="assetID"></param>
        /// <param name="sender"></param>
        /// <param name="asset"></param>
        public void AssetRequestCallback(string assetID, object sender, AssetBase asset)
        {
            try
            {
                lock (this)
                {
                    //MainConsole.Instance.DebugFormat("[ARCHIVER]: Received callback for asset {0}", id);

                    m_requestCallbackTimer.Stop();

                    if (m_requestState == RequestState.Aborted)
                    {
                        MainConsole.Instance.WarnFormat(
                            "[ARCHIVER]: Received information about asset {0} after archive save abortion.  Ignoring.",
                            assetID);

                        return;
                    }

                    if (asset != null)
                    {
//                        MainConsole.Instance.DebugFormat("[ARCHIVER]: Writing asset {0}", id);
                        m_foundAssetUuids.Add(asset.ID);
                        m_assetsArchiver.WriteAsset(asset);
                    }
                    else
                    {
//                        MainConsole.Instance.DebugFormat("[ARCHIVER]: Recording asset {0} as not found", id);
                        m_notFoundAssetUuids.Add(new UUID(assetID));
                    }

                    if (m_foundAssetUuids.Count + m_notFoundAssetUuids.Count == m_repliesRequired)
                    {
                        m_requestState = RequestState.Completed;

                        MainConsole.Instance.InfoFormat(
                            "[ARCHIVER]: Successfully added {0} assets ({1} assets notified missing)",
                            m_foundAssetUuids.Count, m_notFoundAssetUuids.Count);

                        // We want to stop using the asset cache thread asap 
                        // as we now need to do the work of producing the rest of the archive
                        Util.FireAndForget(PerformAssetsRequestCallback);
                    }
                    else
                        m_requestCallbackTimer.Start();
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[ARCHIVER]: AssetRequestCallback failed with {0}", e);
            }
        }

        /// <summary>
        ///     Perform the callback on the original requester of the assets
        /// </summary>
        protected void PerformAssetsRequestCallback(object o)
        {
            try
            {
                m_assetsRequestCallback(m_foundAssetUuids, m_notFoundAssetUuids);
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat(
                    "[ARCHIVER]: Terminating archive creation since asset requster callback failed with {0}", e);
            }
        }

        #region Nested type: RequestState

        private enum RequestState
        {
            Initial,
            Running,
            Completed,
            Aborted
        };

        #endregion
    }
}