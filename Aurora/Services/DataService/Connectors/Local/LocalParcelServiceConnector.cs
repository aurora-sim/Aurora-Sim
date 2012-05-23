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

using System;
using System.Collections.Generic;
using System.Reflection;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Services.DataService
{
    public class LocalParcelServiceConnector : IParcelServiceConnector
    {
        private IGenericData GD;

        #region IParcelServiceConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("ParcelConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString, "Parcel",
                                     source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IParcelServiceConnector"; }
        }

        /// <summary>
        ///   This also updates the parcel, not for just adding a new one
        /// </summary>
        /// <param name = "args"></param>
        public void StoreLandObject(LandData args)
        {
            GenericUtils.AddGeneric(args.RegionID, "LandData", args.GlobalID.ToString(), args.ToOSD(), GD);
            //Parcel access is saved seperately
            SaveParcelAccessList(args);
        }

        /// <summary>
        ///   Get a specific region's parcel info
        /// </summary>
        /// <param name = "RegionID"></param>
        /// <param name = "ParcelID"></param>
        /// <returns></returns>
        public LandData GetLandData(UUID RegionID, UUID ParcelID)
        {
            LandData data = GenericUtils.GetGeneric<LandData>(RegionID, "LandData", ParcelID.ToString(), GD);
            //Stored seperately, so rebuild it
            BuildParcelAccessList(data);
            return data;
        }

        /// <summary>
        ///   Load all parcels in the region
        /// </summary>
        /// <param name = "regionID"></param>
        /// <returns></returns>
        public List<LandData> LoadLandObjects(UUID regionID)
        {
            //Load all from the database
            List<LandData> AllLandObjects = new List<LandData>();
            try
            {
                AllLandObjects = GenericUtils.GetGenerics<LandData>(regionID, "LandData", GD);
            }
            catch (Exception ex)
            {
                AllLandObjects = new List<LandData>();
                MainConsole.Instance.Info("[ParcelService]: Failed to load parcels, " + ex);
            }
            foreach (LandData t in AllLandObjects)
            {
                BuildParcelAccessList(t);
            }
            return AllLandObjects;
        }

        /// <summary>
        ///   Delete a parcel from the database
        /// </summary>
        /// <param name = "RegionID"></param>
        /// <param name = "ParcelID"></param>
        public void RemoveLandObject(UUID RegionID, UUID ParcelID)
        {
            //Remove both the generic and the parcel access list
            GenericUtils.RemoveGenericByKeyAndType(RegionID, "LandData", ParcelID.ToString(), GD);
            QueryFilter filter = new QueryFilter();
            filter.andFilters["ParcelID"] = ParcelID;
            GD.Delete("parcelaccess", filter);
        }

        /// <summary>
        ///   Delete a parcel from the database
        /// </summary>
        /// <param name = "RegionID"></param>
        public void RemoveAllLandObjects(UUID RegionID)
        {
            GenericUtils.RemoveGenericByType(RegionID, "LandData", GD);
        }

        /// <summary>
        ///   Delete a parcel from the database
        /// </summary>
        /// <param name = "RegionID"></param>
        /// <param name = "ParcelID"></param>
        public void RemoveLandObject(UUID RegionID)
        {
            List<LandData> parcels = LoadLandObjects(RegionID);
            //Remove both the generic and the parcel access list
            GenericUtils.RemoveGenericByType(RegionID, "LandData", GD);
            QueryFilter filter = new QueryFilter();
            foreach (LandData data in parcels)
            {
                filter.andFilters["ParcelID"] = data.GlobalID;
                GD.Delete("parcelaccess", filter);
            }
        }

        #endregion

        public void Dispose()
        {
        }

        /// <summary>
        ///   Save this parcel's access list
        /// </summary>
        /// <param name = "data"></param>
        private void SaveParcelAccessList(LandData data)
        {
            //Clear out all old parcel bans and access list entries
            QueryFilter filter = new QueryFilter();
            filter.andFilters["ParcelID"] = data.GlobalID;
            GD.Delete("parcelaccess", filter);
            foreach (ParcelManager.ParcelAccessEntry entry in data.ParcelAccessList)
            {
                Dictionary<string, object> row = new Dictionary<string, object>(4);
                row["ParcelID"] = data.GlobalID;
                row["AccessID"] = entry.AgentID;
                row["Flags"] = entry.Flags;
                row["Time"] = entry.Time.Ticks;
                //Replace all the old ones
                GD.Replace("parcelaccess", row);
            }
        }

        /// <summary>
        ///   Rebuild the access list from the database
        /// </summary>
        /// <param name = "LandData"></param>
        private void BuildParcelAccessList(LandData LandData)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["ParcelID"] = LandData.GlobalID;
            List<string> Query = GD.Query(new[]{
                "AccessID",
                "Flags",
                "Time"
            }, "parcelaccess", filter, null, null, null);

            for (int i = 0; i < Query.Count; i += 3)
            {
                ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry
                                                            {
                                                                AgentID = UUID.Parse(Query[i]),
                                                                Flags = (AccessList) Enum.Parse(typeof (AccessList), Query[i + 1]),
                                                                Time = new DateTime(long.Parse(Query[i + 2]))
                                                            };
                LandData.ParcelAccessList.Add(entry);
            }
        }
    }
}