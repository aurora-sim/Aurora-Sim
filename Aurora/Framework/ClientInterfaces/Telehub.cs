/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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
using System.Linq;
using Aurora.Framework.Modules;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework.ClientInterfaces
{
    public class Telehub : IDataTransferable
    {
        /// <summary>
        ///     Name of the teleHUB object
        /// </summary>
        public string Name = "";

        /// <summary>
        ///     UUID of the teleHUB object
        /// </summary>
        public UUID ObjectUUID = UUID.Zero;

        /// <summary>
        ///     Region UUID
        /// </summary>
        public UUID RegionID = UUID.Zero;

        /// <summary>
        ///     Global region coordinates (in meters)
        /// </summary>
        public float RegionLocX;

        public float RegionLocY;

        /// <summary>
        ///     Positions users will spawn at in order of creation
        /// </summary>
        public List<Vector3> SpawnPos = new List<Vector3>();

        /// <summary>
        ///     Position of the telehub in the region
        /// </summary>
        public float TelehubLocX;

        public float TelehubLocY;
        public float TelehubLocZ;

        /// <summary>
        ///     Rotation of the av
        /// </summary>
        public float TelehubRotX;

        public float TelehubRotY;
        public float TelehubRotZ;

        public string BuildFromList(List<Vector3> SpawnPos)
        {
            return SpawnPos.Aggregate("", (current, Pos) => current + (Pos.ToString() + "\n"));
        }

        public static List<Vector3> BuildToList(string SpawnPos)
        {
            if (SpawnPos == "" || SpawnPos == " ")
                return new List<Vector3>();
            return (from Pos in SpawnPos.Split('\n') where Pos != "" select Vector3.Parse(Pos)).ToList();
        }

        public override void FromOSD(OSDMap map)
        {
            RegionID = map["RegionID"].AsUUID();
            RegionLocX = (float) map["RegionLocX"].AsReal();
            RegionLocY = (float) map["RegionLocY"].AsReal();
            TelehubRotX = (float) map["TelehubRotX"].AsReal();
            TelehubRotY = (float) map["TelehubRotY"].AsReal();
            TelehubRotZ = (float) map["TelehubRotZ"].AsReal();
            TelehubLocX = (float) map["TelehubLocX"].AsReal();
            TelehubLocY = (float) map["TelehubLocY"].AsReal();
            TelehubLocZ = (float) map["TelehubLocZ"].AsReal();
            SpawnPos = BuildToList(map["Spawns"].AsString());
            Name = map["Name"].AsString();
            ObjectUUID = map["ObjectUUID"].AsUUID();
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap
                             {
                                 {"RegionID", OSD.FromUUID(RegionID)},
                                 {"RegionLocX", OSD.FromReal(RegionLocX)},
                                 {"RegionLocY", OSD.FromReal(RegionLocY)},
                                 {"TelehubRotX", OSD.FromReal(TelehubRotX)},
                                 {"TelehubRotY", OSD.FromReal(TelehubRotY)},
                                 {"TelehubRotZ", OSD.FromReal(TelehubRotZ)},
                                 {"TelehubLocX", OSD.FromReal(TelehubLocX)},
                                 {"TelehubLocY", OSD.FromReal(TelehubLocY)},
                                 {"TelehubLocZ", OSD.FromReal(TelehubLocZ)},
                                 {"Spawns", OSD.FromString(BuildFromList(SpawnPos))},
                                 {"ObjectUUID", OSD.FromUUID(ObjectUUID)},
                                 {"Name", OSD.FromString(Name)}
                             };
            return map;
        }
    }
}