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
using System.Linq;
using System.Reflection;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Services.DataService
{
    public class LocalEstateConnector : ConnectorBase, IEstateConnector
    {
        private IGenericData GD;
        private string m_estateTable = "estatesettings";

        #region IEstateConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore registry,
                               string defaultConnectionString)
        {
            GD = GenericData;
            m_registry = registry;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase(defaultConnectionString, "Estate",
                                 source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            DataManager.DataManager.RegisterPlugin(Name + "Local", this);

            if (source.Configs["AuroraConnectors"].GetString("EstateConnector", "LocalConnector") == "LocalConnector")
            {
                DataManager.DataManager.RegisterPlugin(this);
            }
            Init(registry, Name);
        }

        public string Name
        {
            get { return "IEstateConnector"; }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public EstateSettings GetEstateSettings(UUID regionID)
        {
            object remoteValue = DoRemote(regionID);
            if (remoteValue != null)
                return (EstateSettings)remoteValue;


            EstateSettings settings = new EstateSettings() { EstateID = 0 };
            int estateID = GetEstateID(regionID);
            if (estateID == 0)
                return settings;
            settings = GetEstate(estateID);
            return settings;
        }

        public EstateSettings GetEstateSettings(int EstateID)
        {
            return GetEstate(EstateID);
        }

        public EstateSettings GetEstateSettings(string name)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["EstateName"] = name;
            return GetEstate(
                int.Parse(GD.Query(new string[1]{ "EstateID" }, "estatesettings", filter, null, null, null)[0])
            );
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public int CreateNewEstate(EstateSettings es, UUID RegionID)
        {
            object remoteValue = DoRemote(es.ToOSD(), RegionID);
            if (remoteValue != null)
                return (int)remoteValue;


            int estateID = GetEstate(es.EstateOwner, es.EstateName);
            if (estateID > 0)
            {
                if (LinkRegion(RegionID, estateID))
                    return estateID;
                return 0;
            }
            es.EstateID = GetNewEstateID();
            SaveEstateSettings(es, true);
            LinkRegion(RegionID, (int)es.EstateID);
            return (int)es.EstateID;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void SaveEstateSettings(EstateSettings es)
        {
            object remoteValue = DoRemote(es.ToOSD());
            if (remoteValue != null)
                return;

            SaveEstateSettings(es, false);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool LinkRegion(UUID regionID, int estateID)
        {
            object remoteValue = DoRemote(regionID, estateID);
            if (remoteValue != null)
                return (bool)remoteValue;

            GD.Replace("estateregions", new[] { "RegionID", "EstateID" },
                       new object[]
                           {
                               regionID,
                               estateID
                           });

            return true;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool DelinkRegion(UUID regionID)
        {
            object remoteValue = DoRemote(regionID);
            if (remoteValue != null)
                return (bool)remoteValue;

            GD.Delete("estateregions", new[] {"RegionID"},
                      new object[]
                          {
                              regionID
                          });

            return true;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.High)]
        public bool DeleteEstate(int estateID)
        {
            object remoteValue = DoRemote(estateID);
            if (remoteValue != null)
                return (bool)remoteValue;


            GD.Delete("estateregions", new[] { "EstateID" }, new object[] { estateID });
            GD.Delete(m_estateTable, new[] { "EstateID" }, new object[] { estateID });

            return true;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public int GetEstate(UUID ownerID, string name)
        {
            object remoteValue = DoRemote(ownerID, name);
            if (remoteValue != null)
            {
                return (int)remoteValue;
            }

            QueryFilter filter = new QueryFilter();
            filter.andFilters["EstateName"] = name;
            filter.andFilters["EstateOwner"] = ownerID;

            List<string> retVal = GD.Query(new string[1] { "EstateID" }, "estatesettings", filter, null, null, null);

            if (retVal.Count > 0)
                return int.Parse(retVal[0]);
            return 0;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<UUID> GetRegions(int estateID)
        {
            object remoteValue = DoRemote(estateID);
            if (remoteValue != null)
            {
                return (List<UUID>)remoteValue;
            }

            QueryFilter filter = new QueryFilter();
            filter.andFilters["EstateID"] = estateID;
            return GD.Query(new string[1] { "RegionID" }, "estateregions", filter, null, null, null).ConvertAll(x => UUID.Parse(x));
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<EstateSettings> GetEstates(UUID OwnerID)
        {
            return GetEstates(OwnerID, new Dictionary<string,bool>(0));
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<EstateSettings> GetEstates(UUID OwnerID, Dictionary<string, bool> boolFields)
        {
            object remoteValue = DoRemote(OwnerID, boolFields);
            if (remoteValue != null)
            {
                return (List<EstateSettings>)remoteValue;
            }

            List<EstateSettings> settings = new List<EstateSettings>();

            QueryFilter filter = new QueryFilter();
            filter.andFilters["EstateOwner"] = OwnerID;
            List<int> retVal = GD.Query(new string[1] { "EstateID" }, m_estateTable, filter, null, null, null).ConvertAll(x => int.Parse(x));
            foreach (int estateID in retVal)
            {
                bool Add = true;
                EstateSettings es = GetEstate(estateID);

                if(boolFields.Count > 0){
                    OSDMap esmap = es.ToOSD();
                    foreach(KeyValuePair<string, bool> field in boolFields){
                        if(esmap.ContainsKey(field.Key) && esmap[field.Key].AsBoolean() != field.Value){
                            Add = false;
                            break;
                        }
                    }
                }

                if (Add)
                {
                    settings.Add(es);
                }
            }
            return settings;
        }

        #endregion

        public void Dispose()
        {
        }

        #region Helpers

        public int GetEstateID(UUID regionID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["RegionID"] = regionID;

            List<string> retVal = GD.Query(new string[1] { "EstateID" }, "estateregions", filter, null, null, null);

            return (retVal.Count > 0) ? int.Parse(retVal[0]) : 0;
        }

        private EstateSettings GetEstate(int estateID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["EstateID"] = estateID;
            List<string> retVals = GD.Query(new string[1] { "*" }, m_estateTable, filter, null, null, null);
            EstateSettings settings = new EstateSettings{
                EstateID = 0
            };
            if (retVals.Count > 0)
            {
                settings.FromOSD((OSDMap)OSDParser.DeserializeJson(retVals[4]));
            }
            return settings;
        }

        private uint GetNewEstateID()
        {
            List<string> QueryResults = GD.Query(new string[2]{
                "COUNT(EstateID)",
                "MAX(EstateID)"
            }, m_estateTable, null, null, null, null);
            return (uint.Parse(QueryResults[0]) > 0) ? uint.Parse(QueryResults[1]) + 1 : 100;
        }

        protected void SaveEstateSettings(EstateSettings es, bool doInsert)
        {
            string[] keys = new string[5]
            {
                "EstateID",
                "EstateName",
                "EstateOwner",
                "ParentEstateID",
                "Settings"
            };
            object[] values = new object[5]
            {
                es.EstateID,
                es.EstateName,
                es.EstateOwner,
                es.ParentEstateID,
                OSDParser.SerializeJsonString(es.ToOSD())
            };
            if (!doInsert)
                GD.Update(m_estateTable, values, keys, new string[1] { "EstateID" }, new object[1] { es.EstateID });
            else
                GD.Insert(m_estateTable, keys, values);
        }

        #endregion
    }
}