using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using Nini.Config;
using log4net;
using System.Reflection;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class LocalEstateConnector : IEstateConnector
	{
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("EstateConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString);

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
            else
            {
                //Check to make sure that something else exists
                string m_ServerURI = simBase.ApplicationRegistry.RequestModuleInterface<IAutoConfigurationService>().FindValueOf("RemoteServerURI", "AuroraData");
                if (m_ServerURI == "") //Blank, not set up
                {
                    OpenSim.Framework.Console.MainConsole.Instance.Output("[AuroraDataService]: Falling back on local connector for " + "EstateConnector", "None");
                    GD = GenericData;

                    if (source.Configs[Name] != null)
                        defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                    GD.ConnectToDatabase(defaultConnectionString);

                    DataManager.DataManager.RegisterPlugin(Name, this);
                }
            }
        }

        public string Name
        {
            get { return "IEstateConnector"; }
        }

        public void Dispose()
        {
        }

		public EstateSettings LoadEstateSettings(UUID regionID)
		{
            List<string> estateID = GD.Query(new string[] { "ID", "`Key`" }, new object[] { regionID, "EstateID" }, "estates", "Value");
            if (estateID.Count == 0)
				return new EstateSettings();
            else
                return LoadEstateSettings(Convert.ToInt32(estateID[0]));
		}

		public OpenSim.Framework.EstateSettings LoadEstateSettings(int estateID)
		{
            EstateSettings settings = new EstateSettings();
            List<string> query = null;
            try
            {
                query = GD.Query(new string[] { "ID", "`Key`" }, new object[] { estateID, "EstateSettings" }, "estates", "Value");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return settings; //Couldn't find it, return default then.

            OSDMap estateInfo = (OSDMap)OSDParser.DeserializeLLSDXml(query[0]);

            settings.AbuseEmail = estateInfo["AbuseEmail"].AsString();
            settings.AbuseEmailToEstateOwner = estateInfo["AbuseEmailToEstateOwner"].AsInteger() == 1;
            settings.AllowDirectTeleport = estateInfo["AllowDirectTeleport"].AsInteger() == 1;
            settings.AllowVoice = estateInfo["AllowVoice"].AsInteger() == 1;
            settings.BillableFactor = (float)estateInfo["BillableFactor"].AsReal();
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

        #region Helpers

        private void LoadBanList(EstateSettings es)
		{
			es.ClearBans();

            List<string> query = null;
            try
            {
                query = GD.Query(new string[] { "ID", "`Key`" }, new object[] { es.EstateID, "EstateBans" }, "estates", "Value");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return; //Couldn't find it, return then.

            OSDMap estateInfo = (OSDMap)OSDParser.DeserializeLLSDXml(query[0]);
            foreach (OSD map in estateInfo.Values)
            {
                OSDMap estateBan = (OSDMap)map;
				EstateBan eb = new EstateBan();

                eb.BannedUserID = estateBan["BannedUserID"].AsUUID();
                eb.BannedHostAddress = estateBan["BannedHostAddress"].AsString();
                eb.BannedHostIPMask = estateBan["BannedHostIPMask"].AsString();
				es.AddBan(eb);
			}
		}

		UUID[] LoadUUIDList(uint EstateID, string table)
        {
            List<UUID> uuids = new List<UUID>();
            List<string> query = null;
            try
            {
                query = GD.Query(new string[] { "ID", "`Key`" }, new object[] { EstateID, table }, "estates", "Value");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return uuids.ToArray(); //Couldn't find it, return then.

            OSDArray estateInfo = (OSDArray)OSDParser.DeserializeLLSDXml(query[0]);
            foreach (OSD uuid in estateInfo)
            {
                uuids.Add(uuid.AsUUID());
            }

            return uuids.ToArray();
		}

		private void SaveBanList(EstateSettings es)
		{
            OSDMap estateBans = new OSDMap();
            foreach (EstateBan b in es.EstateBans)
            {
                OSDMap estateBan = new OSDMap();
                estateBan.Add("BannedUserID", OSD.FromUUID(b.BannedUserID));
                estateBan.Add("BannedHostAddress", OSD.FromString(b.BannedHostAddress));
                estateBan.Add("BannedHostIPMask", OSD.FromString(b.BannedHostIPMask));
                estateBans.Add(b.BannedUserID.ToString(), estateBan);
			}

            string value = OSDParser.SerializeLLSDXmlString(estateBans);
            GD.Replace("estates", new string[] { "ID", "`Key`", "`Value`" }, new object[] { es.EstateID, "EstateBans", value });
		}

		void SaveUUIDList(uint EstateID, string table, UUID[] data)
		{
			OSDArray estate = new OSDArray();
            foreach (UUID uuid in data) 
            {
                estate.Add(OSD.FromUUID(uuid));
            }

            string value = OSDParser.SerializeLLSDXmlString(estate);
            GD.Replace("estates", new string[] { "ID", "`Key`", "`Value`" }, new object[] { EstateID, table, value });
        }

        #endregion

        public EstateSettings CreateEstate(EstateSettings es, UUID RegionID)
		{
            int EstateID = 0;
            List<string> QueryResults = GD.Query("`Key`", "EstateID", "estates", "`Value`", " ORDER BY `Value` DESC");
            if (QueryResults.Count == 0)
                EstateID = 99;
            else
                EstateID = int.Parse(QueryResults[0]);

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
            es.EstateID = (uint)EstateID;

            List<object> Values = new List<object>();
            Values.Add(es.EstateID);
            Values.Add("EstateSettings");
            OSDMap map = Util.DictionaryToOSD(es.ToKeyValuePairs(true));
            Values.Add(OSDParser.SerializeLLSDXmlString(map));
            GD.Insert("estates", Values.ToArray());

            GD.Insert("estates", new object[] {
					RegionID,
                    "EstateID",
					EstateID
				});

            es.OnSave += SaveEstateSettings;
            return es;
		}

        public void SaveEstateSettings(OpenSim.Framework.EstateSettings es)
        {
            List<string> query = null;
            try
            {
                query = GD.Query(new string[] { "ID", "`Key`" }, new object[] { es.EstateID, "EstateSettings" }, "estates", "Value");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return; //Couldn't find it, return default then.

            OSDMap estateInfo = (OSDMap)OSDParser.DeserializeLLSDXml(query[0]);

            if (estateInfo["EstatePass"].AsString() != es.EstatePass)
            {
                m_log.Warn("[ESTATE SERVICE]: Wrong estate password in updating of estate " + es.EstateName + "! Possible attempt to hack this estate!");
                return;
            }

            List<string> Keys = new List<string>();
            Keys.Add("Value");
            List<object> Values = new List<object>();
            Values.Add(OSDParser.SerializeLLSDXmlString(Util.DictionaryToOSD(es.ToKeyValuePairs(true))));

            GD.Update("estates", Values.ToArray(), Keys.ToArray(), new string[] { "ID", "`Key`" }, new object[] { es.EstateID, "EstateSettings" });

            SaveBanList(es);
            SaveUUIDList(es.EstateID, "EstateManagers", es.EstateManagers);
            SaveUUIDList(es.EstateID, "EstateAccess", es.EstateAccess);
            SaveUUIDList(es.EstateID, "EstateGroups", es.EstateGroups);
        }

		public List<int> GetEstates(string search)
		{
			List<int> result = new List<int>();
			List<string> RetVal = GD.Query("", "", "estates", "Value", " where `Key` = 'EstateSettings' and Value LIKE '%" + search + "%'");
            if (RetVal.Count == 0)
                return null;
            foreach (string val in RetVal)
            {
                OSDMap estateInfo = (OSDMap)OSDParser.DeserializeLLSDXml(val);
                result.Add(estateInfo["EstateID"].AsInteger());
			}
			return result;
		}

		public bool LinkRegion(OpenMetaverse.UUID regionID, int estateID, string password)
		{
			List<string> query = null;
            try
            {
                query = GD.Query(new string[] { "ID", "`Key`" }, new object[] { estateID, "EstateSettings" }, "estates", "Value");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return false; //Couldn't find it, return default then.

            OSDMap estateInfo = (OSDMap)OSDParser.DeserializeLLSDXml(query[0]);

            if (estateInfo["EstatePass"].AsString() != password)
                return false;

			GD.Replace("estates", new string[]{"ID", "Key", "Value"},
                new object[] {
				regionID,
                "EstateID",
				estateID
			});

			return true;
		}

		public List<OpenMetaverse.UUID> GetRegions(int estateID)
		{
            List<string> RegionIDs = GD.Query(new string[] { "`Key`", "`Value`" }, new object[] { "EstateID", estateID }, "estates", "ID");
			List<UUID> regions = new List<UUID>();
			foreach (string RegionID in RegionIDs)
				regions.Add(new UUID(RegionID));
			return regions;
		}

        public bool DeleteEstate(int estateID, string password)
		{
            List<string> query = null;
            try
            {
                query = GD.Query(new string[] { "ID", "`Key`" }, new object[] { estateID, "EstateSettings" }, "estates", "Value");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return false; //Couldn't find it, return default then.

            OSDMap estateInfo = (OSDMap)OSDParser.DeserializeLLSDXml(query[0]);

            if (estateInfo["EstatePass"].AsString() != password)
                return false;

			GD.Delete("estates", new string[] { "ID" }, new object[] { estateID });

			return true;
		}
	}
}
