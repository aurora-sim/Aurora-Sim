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
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System.Collections.Generic;

namespace Aurora.Framework
{
    public delegate void AssetRetrieved(string id, Object sender, AssetBase asset);

    /// <summary>
    ///   Interface for getting a module that can access assets (only) outside of this grid
    /// </summary>
    public interface IAssetServiceConnector : IAssetService
    {
    }

    /// <summary>
    ///   Interface for getting a module that can access assets inside and outside of this grid
    /// </summary>
    public interface IExternalAssetService : IAssetService
    {
    }

    public interface IAssetService
    {
        /// <summary>
        ///   Get the local service (if applicable)
        /// </summary>
        IAssetService InnerService { get; }

        /// <summary>
        ///   Get an asset synchronously.
        /// </summary>
        /// <param name = "id"></param>
        /// <returns></returns>
        AssetBase Get(string id);

        /// <summary>
        ///   Get a mesh asset synchronously.
        /// </summary>
        /// <param name = "id"></param>
        /// <returns></returns>
        AssetBase GetMesh(string id);

        /// <summary>
        ///   Get whether an asset with the given ID exists
        /// </summary>
        /// <param name = "id"></param>
        /// <returns></returns>
        bool GetExists(string id);

        /// <summary>
        ///   Get the asset data for the given asset
        /// </summary>
        /// <param name = "id"></param>
        /// <returns></returns>
        byte[] GetData(string id);

        /// <summary>
        ///   Synchronously fetches an asset from the local cache only
        /// </summary>
        /// <param name = "id">Asset ID</param>
        /// <returns>The fetched asset, or null if it did not exist in the local cache</returns>
        AssetBase GetCached(string id);

        /// <summary>
        ///   Get an asset synchronously or asynchronously (depending on whether 
        ///   it is locally cached) and fire a callback with the fetched asset
        /// </summary>
        /// <param name = "id">The asset id</param>
        /// <param name = "sender">Represents the requester.  Passed back via the handler</param>
        /// <param name = "handler">The handler to call back once the asset has been retrieved</param>
        void Get(string id, Object sender, AssetRetrieved handler);

        /// <summary>
        ///   Creates a new asset
        /// </summary>
        /// Returns a random ID if none is passed into it
        /// <param name = "asset"></param>
        /// <returns></returns>
        UUID Store(AssetBase asset);

        /// <summary>
        ///   Update an asset's content. Will return false, and UUID.ZERO if it fails
        /// </summary>
        /// Attachments and bare scripts need this!!
        /// <param name = "id"> </param>
        /// <param name = "data"></param>
        /// <returns></returns>
        UUID UpdateContent(UUID id, byte[] data);

        /// <summary>
        ///   Delete an asset
        /// </summary>
        /// <param name = "id"></param>
        /// <returns></returns>
        bool Delete(UUID id);

        void Configure(IConfigSource config, IRegistryCore registry);

        void Start(IConfigSource config, IRegistryCore registry);

        void FinishedStartup();
    }

    public interface IAssetDataPlugin : IAuroraDataPlugin
    {
        AssetBase GetAsset(UUID uuid);
        AssetBase GetMeta(UUID uuid);
        UUID Store(AssetBase asset);
        bool StoreAsset(AssetBase asset);
        void UpdateContent(UUID id, byte[] asset, out UUID newID);
        bool ExistsAsset(UUID uuid);
        bool Delete(UUID id);
        bool Delete(UUID id, bool ignoreFlags);
        List<string> GetAssetUUIDs(uint? start, uint? count);
    }
}