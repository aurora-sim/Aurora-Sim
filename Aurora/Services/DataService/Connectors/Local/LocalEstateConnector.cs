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
using OpenSim.Framework;
using log4net;

namespace Aurora.Services.DataService
{
    public class LocalEstateConnector : IEstateConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IGenericData GD;
        private IRegistryCore m_registry;

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
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IEstateConnector"; }
        }

        public bool LoadEstateSettings(UUID regionID, out EstateSettings settings)
        {
            settings = null;
            List<string> estateID = GD.Query(new[] {"ID", "`Key`"}, new object[] {regionID, "EstateID"}, "estates",
                                             "`Value`");
            if (estateID.Count != 0)
                settings = LoadEstateSettings(Convert.ToInt32(estateID[0]));
            return true;
        }

        public EstateSettings CreateEstate(EstateSettings es, UUID RegionID)
        {
            int EstateID = 0;
            List<string> QueryResults = GD.Query("`Key`", "EstateID", "estates", "`Value`", " ORDER BY `Value` DESC");
            EstateID = QueryResults.Count == 0 ? 99 : int.Parse(QueryResults[0]);

            if (EstateID == 0)
                EstateID = 99;

            //Check for other estates with the same name
            List<int> Estates = GetEstates(es.EstateName);
            if (Estates != null)
            {
                foreach (int otherEstateID in Estates)
                {
                    EstateSettings otherEstate = this.LoadEstateSettings(otherEstateID);
                    if (otherEstate.EstateName == es.EstateName)
                    { //Cant have two estates with the same name.
                        //We set the estate name so that the region can get the error and so we don't have to spit out more junk to find it.
                        return new EstateSettings()
                        {
                            EstateID = 0,
                            EstateName = "Duplicate Estate Name. Please Change."
                        };
                    }
                }
            }

            EstateID++;
            es.EstateID = (uint) EstateID;

            List<object> Values = new List<object> {es.EstateID, "EstateSettings"};
            OSD map = es.ToOSD(true);
            Values.Add(OSDParser.SerializeLLSDXmlString(map));
            GD.Insert("estates", Values.ToArray());

            GD.Insert("estates", new object[]
                                     {
                                         RegionID,
                                         "EstateID",
                                         EstateID
                                     });

            es.OnSave += SaveEstateSettings;
            return es;
        }

        public void SaveEstateSettings(EstateSettings es)
        {
            List<string> query = null;
            try
            {
                query = GD.Query(new[] {"ID", "`Key`"}, new object[] {es.EstateID, "EstateSettings"}, "estates",
                                 "`Value`");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return; //Couldn't find it, return default then.

            OSDMap estateInfo = (OSDMap) OSDParser.DeserializeLLSDXml(query[0]);

            if (estateInfo["EstatePass"].AsString() != es.EstatePass)
            {
                m_log.Warn("[ESTATE SERVICE]: Wrong estate password in updating of estate " + es.EstateName +
                           "! Possible attempt to hack this estate!");
                return;
            }

            List<string> Keys = new List<string> {"Value"};
            List<object> Values = new List<object> {OSDParser.SerializeLLSDXmlString(es.ToOSD(true))};

            GD.Update("estates", Values.ToArray(), Keys.ToArray(), new[] {"ID", "`Key`"},
                      new object[] {es.EstateID, "EstateSettings"});

            SaveBanList(es);
            SaveUUIDList(es.EstateID, "EstateManagers", es.EstateManagers);
            SaveUUIDList(es.EstateID, "EstateAccess", es.EstateAccess);
            SaveUUIDList(es.EstateID, "EstateGroups", es.EstateGroups);

            m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("EstateUpdated",
                                                                                                      es);
        }

        public List<int> GetEstates(string search)
        {
			List<int> result = new List<int>();
            List<string> RetVal = GD.Query("", "", "estates", "`Value`",
                                           " where `Key` = 'EstateSettings' and `Value` LIKE '%<key>EstateName</key><string>" +
                                           search.MySqlEscape() + "</string>%'");
            if (RetVal.Count == 0)
                return null;
            foreach (string val in RetVal)
            {
                OSD oval = OSDParser.DeserializeLLSDXml (val);
                if (oval is OSDMap)
                {
                    OSDMap estateInfo = (OSDMap)oval;
                    if (estateInfo["EstateName"].AsString () == search)
                        result.Add (estateInfo["EstateID"].AsInteger ());
                }
            }
            return result;
        }

        public List<UUID> GetRegions(uint estateID)
        {
            List<UUID> result = new List<UUID>();
            List<string> RetVal = GD.Query(new string[]{"`Key`","`Value`"}, new object[]{"EstateID",estateID}, "estates", "ID");
            if (RetVal.Count == 0)
                return null;
            foreach (string val in RetVal)
            {
                result.Add(UUID.Parse(val));
            }
            return result;
        }

        public List<EstateSettings> GetEstates(UUID OwnerID)
        {
            List<EstateSettings> result = new List<EstateSettings>();
            List<string> RetVal = GD.Query("", "", "estates", "`Value`", " where `Key` = 'EstateSettings' and `Value` LIKE '%<key>EstateOwner</key><uuid>" + OwnerID + "</uuid>%'");
            if (RetVal.Count == 0)
                return null;
            foreach (string val in RetVal)
            {
                OSD oval = OSDParser.DeserializeLLSDXml (val);
                if (oval is OSDMap)
                {
                    OSDMap estateInfo = (OSDMap)oval;
                    EstateSettings es = LoadEstateSettings (estateInfo["EstateID"].AsInteger ());
                    if(es != null)
                        result.Add (es);
                }
            }
            return result;
        }

        public bool LinkRegion(UUID regionID, int estateID, string password)
        {
            List<string> query = null;
            try
            {
                query = GD.Query(new[] {"ID", "`Key`"}, new object[] {estateID, "EstateSettings"}, "estates", "`Value`");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return false; //Couldn't find it, return default then.

            OSDMap estateInfo = (OSDMap) OSDParser.DeserializeLLSDXml(query[0]);

            if (estateInfo["EstatePass"].AsString() != password)
                return false;

            GD.Replace("estates", new[] {"ID", "`Key`", "`Value`"},
                       new object[]
                           {
                               regionID,
                               "EstateID",
                               estateID
                           });

            return true;
        }

        public bool DelinkRegion(UUID regionID, string password)
        {
            List<string> query = null;
            try
            {
                //First make sure they are in the estate
                query = GD.Query(new[] {"ID", "`Key`"}, new object[] {regionID, "EstateID"}, "estates", "`Value`");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return false; //Couldn't find it, return default then.

            try
            {
                //Now pull the estate settings to check the password
                query = GD.Query(new[] {"ID", "`Key`"}, new object[] {query[0], "EstateSettings"}, "estates", "`Value`");
            }
            catch
            {
            }

            OSDMap estateInfo = (OSDMap) OSDParser.DeserializeLLSDXml(query[0]);

            if (estateInfo["EstatePass"].AsString() != password)
                return false; //fakers!

            GD.Delete("estates", new[] {"ID", "`Key`"},
                      new object[]
                          {
                              regionID,
                              "EstateID"
                          });

            return true;
        }

        public bool DeleteEstate(int estateID, string password)
        {
            List<string> query = null;
            try
            {
                query = GD.Query(new[] {"ID", "`Key`"}, new object[] {estateID, "EstateSettings"}, "estates", "Value");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return false; //Couldn't find it, return default then.

            OSDMap estateInfo = (OSDMap) OSDParser.DeserializeLLSDXml(query[0]);

            if (estateInfo["EstatePass"].AsString() != password)
                return false;

            GD.Delete("estates", new[] {"ID"}, new object[] {estateID});

            return true;
        }

        #endregion

        public void Dispose()
        {
        }

        public EstateSettings LoadEstateSettings(int estateID)
        {
            EstateSettings settings = new EstateSettings();
            List<string> query = null;
            try
            {
                query = GD.Query(new[] {"ID", "`Key`"}, new object[] {estateID, "EstateSettings"}, "estates", "`Value`");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return null; //Couldn't find it, return default then.

            OSDMap estateInfo = (OSDMap) OSDParser.DeserializeLLSDXml(query[0]);

            settings.AbuseEmail = estateInfo["AbuseEmail"].AsString();
            settings.AbuseEmailToEstateOwner = estateInfo["AbuseEmailToEstateOwner"].AsInteger() == 1;
            settings.AllowDirectTeleport = estateInfo["AllowDirectTeleport"].AsInteger() == 1;
            settings.AllowVoice = estateInfo["AllowVoice"].AsInteger() == 1;
            settings.BillableFactor = (float) estateInfo["BillableFactor"].AsReal();
            settings.BlockDwell = estateInfo["BlockDwell"].AsInteger() == 1;
            settings.DenyAnonymous = estateInfo["DenyAnonymous"].AsInteger() == 1;
            settings.DenyIdentified = estateInfo["DenyIdentified"].AsInteger() == 1;
            settings.DenyMinors = estateInfo["DenyMinors"].AsInteger() == 1;
            settings.DenyTransacted = estateInfo["DenyTransacted"].AsInteger() == 1;
            settings.EstateName = estateInfo["EstateName"].AsString();
            settings.EstateOwner = estateInfo["EstateOwner"].AsUUID();
            settings.EstateSkipScripts = estateInfo["EstateSkipScripts"].AsInteger() == 1;
            settings.FixedSun = estateInfo["FixedSun"].AsInteger() == 1;
            settings.ParentEstateID = estateInfo["ParentEstateID"].AsUInteger();
            settings.PricePerMeter = estateInfo["PricePerMeter"].AsInteger();
            settings.PublicAccess = estateInfo["PublicAccess"].AsInteger() == 1;
            settings.RedirectGridX = estateInfo["RedirectGridX"].AsInteger();
            settings.RedirectGridY = estateInfo["RedirectGridY"].AsInteger();
            settings.ResetHomeOnTeleport = estateInfo["ResetHomeOnTeleport"].AsInteger() == 1;
            settings.SunPosition = estateInfo["SunPosition"].AsReal();
            settings.TaxFree = estateInfo["TaxFree"].AsInteger() == 1;
            settings.UseGlobalTime = estateInfo["UseGlobalTime"].AsInteger() == 1;
            settings.EstateID = estateInfo["EstateID"].AsUInteger();
            settings.AllowLandmark = estateInfo["AllowLandmark"].AsInteger() == 1;
            settings.AllowParcelChanges = estateInfo["AllowParcelChanges"].AsInteger() == 1;
            settings.AllowSetHome = estateInfo["AllowSetHome"].AsInteger() == 1;
            settings.EstatePass = estateInfo["EstatePass"].AsString();

            LoadBanList(settings);
            settings.EstateAccess = LoadUUIDList(settings.EstateID, "EstateAccess");
            settings.EstateGroups = LoadUUIDList(settings.EstateID, "EstateGroups");
            settings.EstateManagers = LoadUUIDList(settings.EstateID, "EstateManagers");
            settings.OnSave += SaveEstateSettings;
            return settings;
        }

        public List<UUID> GetRegions(int estateID)
        {
            List<string> RegionIDs = GD.Query(new[] {"`Key`", "`Value`"}, new object[] {"EstateID", estateID}, "estates",
                                              "ID");
            return RegionIDs.Select(RegionID => new UUID(RegionID)).ToList();
        }

        #region Helpers

        private void LoadBanList(EstateSettings es)
        {
            es.ClearBans();

            List<string> query = null;
            try
            {
                query = GD.Query(new[] {"ID", "`Key`"}, new object[] {es.EstateID, "EstateBans"}, "estates", "`Value`");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return; //Couldn't find it, return then.

            OSDMap estateInfo = (OSDMap) OSDParser.DeserializeLLSDXml(query[0]);
            foreach (EstateBan eb in from OSDMap estateBan in estateInfo.Values select new EstateBan
                                                                                           {
                                                                                               BannedUserID = estateBan["BannedUserID"].AsUUID(),
                                                                                               BannedHostAddress = estateBan["BannedHostAddress"].AsString(),
                                                                                               BannedHostIPMask = estateBan["BannedHostIPMask"].AsString()
                                                                                           })
            {
                es.AddBan(eb);
            }
        }

        private UUID[] LoadUUIDList(uint EstateID, string table)
        {
            List<UUID> uuids = new List<UUID>();
            List<string> query = null;
            try
            {
                query = GD.Query(new[] {"ID", "`Key`"}, new object[] {EstateID, table}, "estates", "`Value`");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return uuids.ToArray(); //Couldn't find it, return then.

            OSDArray estateInfo = (OSDArray) OSDParser.DeserializeLLSDXml(query[0]);
            uuids.AddRange(estateInfo.Select(uuid => uuid.AsUUID()));

            return uuids.ToArray();
        }

        private void SaveBanList(EstateSettings es)
        {
            OSDMap estateBans = new OSDMap();
            foreach (EstateBan b in es.EstateBans)
            {
                OSDMap estateBan = new OSDMap
                                       {
                                           {"BannedUserID", OSD.FromUUID(b.BannedUserID)},
                                           {"BannedHostAddress", OSD.FromString(b.BannedHostAddress)},
                                           {"BannedHostIPMask", OSD.FromString(b.BannedHostIPMask)}
                                       };
                estateBans.Add(b.BannedUserID.ToString(), estateBan);
            }

            string value = OSDParser.SerializeLLSDXmlString(estateBans);
            GD.Replace("estates", new[] {"ID", "`Key`", "`Value`"}, new object[] {es.EstateID, "EstateBans", value});
        }

        private void SaveUUIDList(uint EstateID, string table, UUID[] data)
        {
            OSDArray estate = new OSDArray();
            foreach (UUID uuid in data)
            {
                estate.Add(OSD.FromUUID(uuid));
            }

            string value = OSDParser.SerializeLLSDXmlString(estate);
            GD.Replace("estates", new[] {"ID", "`Key`", "`Value`"}, new object[] {EstateID, table, value});
        }

        #endregion
    }
}