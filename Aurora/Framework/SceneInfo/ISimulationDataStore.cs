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

using System.Collections.Generic;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface ISimulationDataStore
    {
        /// <summary>
        ///   The name of the plugin
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A new map tile needs generated
        /// </summary>
        bool MapTileNeedsGenerated { get; set; }

        /// <summary>
        ///   Whether we should save backups currently or not
        /// </summary>
        bool SaveBackups { get; set; }

        /// <summary>
        ///   Initialises the data storage engine
        /// </summary>
        void Initialise();

        /// <summary>
        ///   Entirely removes the region, this includes everything about the region
        /// </summary>
        /// <param name = "uuid"></param>
        /// <param name = "regionUUID"></param>
        /// <returns></returns>
        void RemoveRegion(UUID regionUUID);

        /// <summary>
        ///   Something has changed in the region, just alerting us to the change if we need to do anything
        /// </summary>
        void Tainted();

        /// <summary>
        ///   Load persisted objects from region storage.
        /// </summary>
        /// <param name = "regionUUID">the Region UUID</param>
        /// <returns>List of loaded groups</returns>
        List<ISceneEntity> LoadObjects(IScene scene);

        /// <summary>
        ///   Load the latest terrain revision from region storage
        /// </summary>
        /// <param name = "regionID">the region UUID</param>
        /// <returns>Heightfield data</returns>
        short[] LoadTerrain(IScene scene, bool RevertMap, int RegionSizeX, int RegionSizeY);

        /// <summary>
        ///   Load the latest water revision from region storage
        /// </summary>
        /// <param name = "scene">the region</param>
        /// <returns>Heightfield data</returns>
        short[] LoadWater(IScene scene, bool RevertMap, int RegionSizeX, int RegionSizeY);

        /// <summary>
        ///   Load all parcels from the database
        /// </summary>
        /// <param name = "regionUUID"></param>
        /// <returns></returns>
        List<LandData> LoadLandObjects(UUID regionUUID);

        /// <summary>
        ///   Shutdown and exit the module
        /// </summary>
        void Shutdown();

        /// <summary>
        ///   Make a copy of the given data store
        /// </summary>
        ISimulationDataStore Copy();

        /// <summary>
        ///   Rename any backups that we might have to a new regionName
        /// </summary>
        /// <param name = "oldRegionName"></param>
        /// <param name = "newRegionName"></param>
        /// <param name = "configSource"></param>
        void RenameBackupFiles(string oldRegionName, string newRegionName, IConfigSource configSource);
    }

    /// <summary>
    ///   Legacy component so that we can load from OpenSim and older Aurora databases
    /// </summary>
    public interface ILegacySimulationDataStore
    {
        /// <summary>
        ///   The name of the plugin
        /// </summary>
        string Name { get; }

        /// <summary>
        ///   Initialises the data storage engine
        /// </summary>
        /// <param name = "filename">The file to save the database to (may not be applicable).  Alternatively,
        ///   a connection string for the database</param>
        void Initialise(string filename);

        /// <summary>
        ///   Dispose the database
        /// </summary>
        void Dispose();

        /// <summary>
        ///   Load persisted objects from region storage.
        /// </summary>
        /// <param name = "regionUUID">the Region UUID</param>
        /// <returns>List of loaded groups</returns>
        List<ISceneEntity> LoadObjects(UUID regionUUID, IScene scene);

        /// <summary>
        ///   Load the latest terrain revision from region storage
        /// </summary>
        /// <param name = "regionID">the region UUID</param>
        /// <returns>Heightfield data</returns>
        short[] LoadTerrain(IScene scene, bool RevertMap, int RegionSizeX, int RegionSizeY);

        /// <summary>
        ///   Load all parcels from the database
        /// </summary>
        /// <param name = "regionUUID"></param>
        /// <returns></returns>
        List<LandData> LoadLandObjects(UUID regionUUID);

        /// <summary>
        ///   Removes all parcels assocated with the given region
        /// </summary>
        /// <param name = "regionUUID"></param>
        void RemoveAllLandObjects(UUID regionUUID);
    }
}