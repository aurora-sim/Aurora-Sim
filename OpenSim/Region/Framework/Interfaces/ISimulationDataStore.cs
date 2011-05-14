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

using System.Collections.Generic;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface ISimulationDataStore
    {
        /// <summary>
        /// Initialises the data storage engine
        /// </summary>
        /// <param name="filename">The file to save the database to (may not be applicable).  Alternatively,
        /// a connection string for the database</param>
        void Initialise(string filename);

        /// <summary>
        /// Dispose the database
        /// </summary>
        void Dispose();
        
        /// <summary>
        /// Stores all object's details apart from inventory
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="regionUUID"></param>
        void StoreObject(SceneObjectGroup obj, UUID regionUUID);

        /// <summary>
        /// Entirely removes the object, including inventory
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="regionUUID"></param>
        /// <returns></returns>
        void RemoveObject(UUID uuid, UUID regionUUID);

        /// <summary>
        /// Removes multiple objects from the database
        /// </summary>
        /// <param name="objGroups"></param>
        void RemoveObjects(List<UUID> objGroups);

        /// <summary>
        /// Entirely removes the region, including prims and prim inventory
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="regionUUID"></param>
        /// <returns></returns>
        void RemoveRegion(UUID regionUUID);

        /// <summary>
        /// Store a prim's inventory
        /// </summary>
        /// <returns></returns>
        void StorePrimInventory(UUID primID, ICollection<TaskInventoryItem> items);

        /// <summary>
        /// Load persisted objects from region storage.
        /// </summary>
        /// <param name="regionUUID">the Region UUID</param>
        /// <returns>List of loaded groups</returns>
        List<SceneObjectGroup> LoadObjects(UUID regionUUID, Scene scene);

        /// <summary>
        /// Store a terrain revision in region storage
        /// </summary>
        /// <param name="ter">HeightField data</param>
        /// <param name="regionID">region UUID</param>
        void StoreTerrain(short[] terrain, UUID regionID, bool Revert);

        /// <summary>
        /// Load the latest terrain revision from region storage
        /// </summary>
        /// <param name="regionID">the region UUID</param>
        /// <returns>Heightfield data</returns>
        short[] LoadTerrain(IScene scene, bool RevertMap, int RegionSizeX, int RegionSizeY);

        /// <summary>
        /// Store a water revision in region storage
        /// </summary>
        /// <param name="ter">HeightField data</param>
        /// <param name="regionID">region UUID</param>
        void StoreWater(short[] terrain, UUID regionID, bool Revert);

        /// <summary>
        /// Load the latest water revision from region storage
        /// </summary>
        /// <param name="scene">the region</param>
        /// <returns>Heightfield data</returns>
        short[] LoadWater(IScene scene, bool RevertMap, int RegionSizeX, int RegionSizeY);

        /// <summary>
        /// Store the given parcel info in the database
        /// </summary>
        /// <param name="args"></param>
        void StoreLandObject(LandData args);

        /// <summary>
        /// Remove the given parcel from the database
        /// </summary>
        /// <param name="RegionID"></param>
        /// <param name="ParcelID"></param>
        void RemoveLandObject(UUID RegionID, UUID ParcelID);

        /// <summary>
        /// Load all parcels from the database
        /// </summary>
        /// <param name="regionUUID"></param>
        /// <returns></returns>
        List<LandData> LoadLandObjects(UUID regionUUID);

        /// <summary>
        /// Shutdown and exit the module
        /// </summary>
        void Shutdown ();

        /// <summary>
        /// The name of the plugin
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Make a copy of the given data store
        /// </summary>
        ISimulationDataStore Copy ();

        /// <summary>
        /// Something has changed in the region, just alerting us to the change if we need to do anything
        /// </summary>
        void Tainted ();
    }
}
