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
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Services.DataService
{
    public class LocalRegionConnector : IRegionConnector
    {
        private IGenericData GD;

        #region IRegionConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            GD = GenericData;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase(defaultConnectionString, "Region",
                                 source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            DataManager.DataManager.RegisterPlugin(Name + "Local", this);

            if (source.Configs["AuroraConnectors"].GetString("RegionConnector", "LocalConnector") == "LocalConnector")
            {
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IRegionConnector"; }
        }

        /// <summary>
        ///   Adds a new telehub in the region. Replaces an old one automatically.
        /// </summary>
        /// <param name = "telehub"></param>
        public void AddTelehub(Telehub telehub, ulong regionhandle)
        {
            //Look for a telehub first.
            if (FindTelehub(new UUID(telehub.RegionID), 0) != null)
            {
                //Found one, time to update it.
                GD.Update("telehubs", new object[]
                                          {
                                              telehub.TelehubLocX,
                                              telehub.TelehubLocY,
                                              telehub.TelehubLocZ,
                                              telehub.TelehubRotX,
                                              telehub.TelehubRotY,
                                              telehub.TelehubRotZ,
                                              telehub.BuildFromList(telehub.SpawnPos),
                                              telehub.ObjectUUID,
                                              telehub.Name.MySqlEscape(50)
                                          }, new[]
                                                 {
                                                     "TelehubLocX",
                                                     "TelehubLocY",
                                                     "TelehubLocZ",
                                                     "TelehubRotX",
                                                     "TelehubRotY",
                                                     "TelehubRotZ",
                                                     "Spawns",
                                                     "ObjectUUID",
                                                     "Name"
                                                 }, new[] {"RegionID"}, new object[] {telehub.RegionID});
            }
            else
            {
                //Make a new one
                List<object> values = new List<object>
                                          {
                                              telehub.RegionID,
                                              telehub.RegionLocX,
                                              telehub.RegionLocY,
                                              telehub.TelehubLocX,
                                              telehub.TelehubLocY,
                                              telehub.TelehubLocZ,
                                              telehub.TelehubRotX,
                                              telehub.TelehubRotY,
                                              telehub.TelehubRotZ,
                                              telehub.BuildFromList(telehub.SpawnPos),
                                              telehub.ObjectUUID,
                                              telehub.Name.MySqlEscape(50)
                                          };
                GD.Insert("telehubs", values.ToArray());
            }
        }

        /// <summary>
        ///   Removes the telehub if it exists.
        /// </summary>
        /// <param name = "regionID"></param>
        public void RemoveTelehub(UUID regionID, ulong regionHandle)
        {
            //Look for a telehub first.
            if (FindTelehub(regionID, 0) != null)
            {
                GD.Delete("telehubs", new[] {"RegionID"}, new object[] {regionID});
            }
        }

        /// <summary>
        ///   Attempts to find a telehub in the region; if one is not found, returns false.
        /// </summary>
        /// <param name = "regionID">Region ID</param>
        /// <param name = "position">The position of the telehub</param>
        /// <returns></returns>
        public Telehub FindTelehub(UUID regionID, ulong regionHandle)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["RegionID"] = regionID;
            List<string> telehubposition = GD.Query(new string[11]{
                "RegionLocX",
                "RegionLocY",
                "TelehubLocX",
                "TelehubLocY",
                "TelehubLocZ",
                "TelehubRotX",
                "TelehubRotY",
                "TelehubRotZ",
                "Spawns",
                "ObjectUUID",
                "Name"
            }, "telehubs", filter, null, null, null);

            //Not the right number of values, so its not there.
            return (telehubposition.Count != 11) ? null : new Telehub
            {
                RegionID = regionID,
                RegionLocX = float.Parse(telehubposition[0]),
                RegionLocY = float.Parse(telehubposition[1]),
                TelehubLocX = float.Parse(telehubposition[2]),
                TelehubLocY = float.Parse(telehubposition[3]),
                TelehubLocZ = float.Parse(telehubposition[4]),
                TelehubRotX = float.Parse(telehubposition[5]),
                TelehubRotY = float.Parse(telehubposition[6]),
                TelehubRotZ = float.Parse(telehubposition[7]),
                SpawnPos = Telehub.BuildToList(telehubposition[8]),
                ObjectUUID = UUID.Parse(telehubposition[9]),
                Name = telehubposition[10]
            };
        }

        #endregion

        public void Dispose()
        {
        }
    }
}