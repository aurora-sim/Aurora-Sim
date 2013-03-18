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


using OpenMetaverse;
using System;
using System.IO;

namespace Aurora.Framework
{
    public interface ITerrainModule
    {
        ITerrainChannel TerrainMap { get; set; }
        ITerrainChannel TerrainRevertMap { get; set; }

        ITerrainChannel TerrainWaterMap { get; set; }
        ITerrainChannel TerrainWaterRevertMap { get; set; }
        void LoadFromFile(string filename, int offsetX, int offsetY);
        void SaveToFile(string filename);
        void ModifyTerrain(UUID user, Vector3 pos, byte size, byte action, UUID agentId);

        /// <summary>
        ///   Taint the terrain. This will lead to sending the terrain data to the clients again.
        ///   Use this if you change terrain data outside of the terrain module (e.g. in osTerrainSetHeight)
        /// </summary>
        void TaintTerrain();

        /// <summary>
        ///   Load a terrain from a stream.
        /// </summary>
        /// <param name = "filename">
        ///   Only required here to identify the image type.  Not otherwise used in the loading itself.
        /// </param>
        /// <param name = "stream"></param>
        void LoadFromStream(string filename, Stream stream);

        void LoadFromStream(string filename, Uri pathToTerrainHeightmap);
        void LoadFromStream(string filename, Stream stream, int offsetX, int offsetY);
        void LoadRevertMapFromStream(string filename, Stream stream, int offsetX, int offsetY);
        void LoadWaterFromStream(string filename, Stream stream, int offsetX, int offsetY);
        void LoadWaterRevertMapFromStream(string filename, Stream stream, int offsetX, int offsetY);

        /// <summary>
        ///   Save a terrain to a stream.
        /// </summary>
        /// <param name = "channel"></param>
        /// <param name = "filename">
        ///   Only required here to identify the image type.  Not otherwise used in the saving itself.
        /// </param>
        /// <param name = "stream"></param>
        void SaveToStream(ITerrainChannel channel, string filename, Stream stream);

        void UndoTerrain(ITerrainChannel channel);

        void LoadRevertMap();
        void LoadWorldHeightmap();
        void ResetTerrain();
        void UpdateWaterHeight(double height);
    }
}