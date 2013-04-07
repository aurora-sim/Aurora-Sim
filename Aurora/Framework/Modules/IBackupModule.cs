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

using Aurora.Framework.SceneInfo.Entities;

namespace Aurora.Framework.Modules
{
    public interface IBackupModule
    {
        /// <summary>
        ///     Are we currently loading prims?
        /// </summary>
        bool LoadingPrims { get; set; }

        /// <summary>
        ///     Loads all parcels from storage (database)
        ///     Sets up the parcel interfaces and modules
        /// </summary>
        void LoadAllLandObjectsFromStorage();

        /// <summary>
        ///     Loads all prims from storage (database)
        ///     This is normally called during startup, but can be called later if not called during startup
        /// </summary>
        void LoadPrimsFromStorage();

        /// <summary>
        ///     Creates script instances in all objects that have scripts in them
        ///     This is normally called during startup, but can be called later if not called during startup
        /// </summary>
        void CreateScriptInstances();

        /// <summary>
        ///     Add a backup taint to the prim.
        /// </summary>
        /// <param name="sceneObjectGroup"></param>
        void AddPrimBackupTaint(ISceneEntity sceneObjectGroup);

        /// <summary>
        ///     Remove all objects from the given region.
        /// </summary>
        void DeleteAllSceneObjects();

        /// <summary>
        ///     Synchronously delete the objects from the scene.
        ///     This does send kill object updates and resets the parcel prim counts.
        /// </summary>
        /// <param name="groups"></param>
        /// <param name="deleteScripts"></param>
        /// <param name="sendKillPackets"></param>
        /// <returns></returns>
        bool DeleteSceneObjects(ISceneEntity[] groups, bool deleteScripts, bool sendKillPackets);

        /// <summary>
        ///     Removes all current objects from the scene, but not from the database
        /// </summary>
        void ResetRegionToStartupDefault();
    }
}