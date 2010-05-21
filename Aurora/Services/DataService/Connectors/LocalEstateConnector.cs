using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.Services.DataService
{
	public class LocalEstateConnector : IEstateConnector
	{
		private IGenericData GenericData = null;
		public LocalEstateConnector()
		{
			GenericData = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
		}

		public EstateSettings LoadEstateSettings(UUID regionID, bool create)
		{
			List<string> estateID = GenericData.Query("RegionID", regionID, "estate_map", "EstateID");
            if (estateID.Count == 0 && !create)
            {
				return new EstateSettings();
			}
            else if (estateID.Count == 0 && create) 
            {
                int EstateID = 0;
                EstateSettings es = new EstateSettings();
				List<string> QueryResults = GenericData.Query("", "", "estate_map", "EstateID", " ORDER BY EstateID DESC");
				if (QueryResults.Count == 0)
                {
                    EstateID = 99;
				} 
                else
                    EstateID = int.Parse(QueryResults[0]);

                if (EstateID == 0)
                    EstateID = 99;

				EstateID++;
                
                List<object> Values = new List<object>();
				Values.Add(EstateID);
				Values.Add(es.EstateName);
				Values.Add(Convert.ToInt32(es.AbuseEmailToEstateOwner));
				Values.Add(Convert.ToInt32(es.DenyAnonymous));
				Values.Add(Convert.ToInt32(es.ResetHomeOnTeleport));
				Values.Add(Convert.ToInt32(es.FixedSun));
				Values.Add(Convert.ToInt32(es.DenyTransacted));
				Values.Add(Convert.ToInt32(es.BlockDwell));
				Values.Add(Convert.ToInt32(es.DenyIdentified));
				Values.Add(Convert.ToInt32(es.AllowVoice));
				Values.Add(Convert.ToInt32(es.UseGlobalTime));
				Values.Add(es.PricePerMeter);
				Values.Add(Convert.ToInt32(es.TaxFree));
				Values.Add(Convert.ToInt32(es.AllowDirectTeleport));
				Values.Add(es.RedirectGridX);
				Values.Add(es.RedirectGridY);
				Values.Add(es.ParentEstateID);
				Values.Add(es.SunPosition);
				Values.Add(Convert.ToInt32(es.EstateSkipScripts));
				Values.Add(es.BillableFactor);
				Values.Add(Convert.ToInt32(es.PublicAccess));
				Values.Add(es.AbuseEmail);
				Values.Add(es.EstateOwner);
				Values.Add(Convert.ToInt32(es.DenyMinors));
				Values.Add(es.EstatePass);
                GenericData.Insert("estate_settings", Values.ToArray());

				GenericData.Insert("estate_map", new object[] {
					regionID,
					EstateID
				});
                return LoadEstateSettings(EstateID);
			}
            else if (estateID.Count == 0)
            {
                return new EstateSettings();
            }
            else
            {
                return LoadEstateSettings(Convert.ToInt32(estateID[0]));
            }
		}

		public OpenSim.Framework.EstateSettings LoadEstateSettings(int estateID)
		{
            EstateSettings settings = new EstateSettings();
            List<string> results = GenericData.Query("EstateID", estateID, "estate_settings", "*");
            if (results.Count == 0)
                return settings;
            settings.AbuseEmail = results[21];
			settings.AbuseEmailToEstateOwner = results[2] == "1";
			settings.AllowDirectTeleport = results[13] == "1";
			settings.AllowVoice = results[9] == "1";
			settings.BillableFactor = Convert.ToInt32(results[19]);
			settings.BlockDwell = results[7] == "1";
			settings.DenyAnonymous = results[3] == "1";
			settings.DenyIdentified = results[8] == "1";
			settings.DenyMinors = results[23] == "1";
			settings.DenyTransacted = results[6] == "1";
			settings.EstateName = results[1];
			settings.EstateOwner = new OpenMetaverse.UUID(results[22]);
			settings.EstateSkipScripts = results[18] == "1";
			settings.FixedSun = results[5] == "1";
			settings.ParentEstateID = Convert.ToUInt32(results[16]);
			settings.PricePerMeter = Convert.ToInt32(results[11]);
			settings.PublicAccess = results[20] == "1";
			settings.RedirectGridX = Convert.ToInt32(results[14]);
			settings.RedirectGridY = Convert.ToInt32(results[15]);
			settings.ResetHomeOnTeleport = results[4] == "1";
			settings.SunPosition = Convert.ToInt32(results[17]);
			settings.TaxFree = results[12] == "1";
			settings.UseGlobalTime = results[10] == "1";
			settings.EstateID = Convert.ToUInt32(results[0]);

			settings.EstateAccess = LoadUUIDList(settings.EstateID, "estate_users");
			LoadBanList(settings);
			settings.EstateGroups = LoadUUIDList(settings.EstateID, "estate_groups");
			settings.EstateManagers = LoadUUIDList(settings.EstateID, "estate_managers");
			settings.OnSave += SaveEstateSettings;
			return settings;
		}

		private void LoadBanList(EstateSettings es)
		{
			es.ClearBans();

			List<string> RetVal = GenericData.Query("EstateID", es.EstateID, "estateban", "bannedUUID");
            if (RetVal.Count == 0)
                return;
            foreach (string userID in RetVal)
            {
				EstateBan eb = new EstateBan();

				UUID uuid = new UUID();
				UUID.TryParse(userID, out uuid);

				eb.BannedUserID = uuid;
				eb.BannedHostAddress = "0.0.0.0";
				eb.BannedHostIPMask = "0.0.0.0";
				es.AddBan(eb);
			}
		}

		UUID[] LoadUUIDList(uint EstateID, string table)
		{
			List<UUID> uuids = new List<UUID>();

			List<string> RetVal = GenericData.Query("EstateID", EstateID, table, "uuid");
            if (RetVal.Count == 0)
                return uuids.ToArray();
            foreach (string userID in RetVal)
            {
				UUID uuid = new UUID();
				UUID.TryParse(userID, out uuid);

				uuids.Add(uuid);
			}
			return uuids.ToArray();
		}

		private void SaveBanList(EstateSettings es)
		{
			GenericData.Delete("estateban", new string[] { "EstateID" }, new object[] { es.EstateID });
			foreach (EstateBan b in es.EstateBans) {
				List<object> banList = new List<object>();
				banList.Add(es.EstateID.ToString());
				banList.Add(b.BannedUserID.ToString());
				banList.Add("");
				banList.Add("");
				banList.Add("");
				GenericData.Insert("estateban", banList.ToArray());
			}
		}

		void SaveUUIDList(uint EstateID, string table, UUID[] data)
		{
			GenericData.Delete(table, new string[] { "EstateID" }, new object[] { EstateID });

			foreach (UUID uuid in data) {
				List<object> List = new List<object>();
				List.Add(EstateID);
				List.Add(uuid);

				GenericData.Insert(table, List.ToArray());
			}
		}

		public bool StoreEstateSettings(OpenSim.Framework.EstateSettings es)
		{
			List<object> SetValues = new List<object>();
            SetValues.Add(es.EstateID); 
            SetValues.Add(es.EstateName);
			SetValues.Add(Convert.ToInt32(es.AbuseEmailToEstateOwner));
			SetValues.Add(Convert.ToInt32(es.DenyAnonymous));
			SetValues.Add(Convert.ToInt32(es.ResetHomeOnTeleport));
			SetValues.Add(Convert.ToInt32(es.FixedSun));
			SetValues.Add(Convert.ToInt32(es.DenyTransacted));
			SetValues.Add(Convert.ToInt32(es.BlockDwell));
			SetValues.Add(Convert.ToInt32(es.DenyIdentified));
			SetValues.Add(Convert.ToInt32(es.AllowVoice));
			SetValues.Add(Convert.ToInt32(es.UseGlobalTime));
			SetValues.Add(es.PricePerMeter);
			SetValues.Add(Convert.ToInt32(es.TaxFree));
			SetValues.Add(Convert.ToInt32(es.AllowDirectTeleport));
			SetValues.Add(es.RedirectGridX);
			SetValues.Add(es.RedirectGridY);
			SetValues.Add(es.ParentEstateID);
			SetValues.Add(es.SunPosition);
			SetValues.Add(Convert.ToInt32(es.EstateSkipScripts));
			SetValues.Add(es.BillableFactor);
			SetValues.Add(Convert.ToInt32(es.PublicAccess));
			SetValues.Add(es.AbuseEmail);
			SetValues.Add(es.EstateOwner);
			SetValues.Add(Convert.ToInt32(es.DenyMinors));
			SetValues.Add(es.EstatePass);

			List<string> SetKeys = new List<string>();
			SetKeys.Add("EstateID");
			SetKeys.Add("EstateName");
			SetKeys.Add("AbuseEmailToEstateOwner");
			SetKeys.Add("DenyAnonymous");
			SetKeys.Add("ResetHomeOnTeleport");
			SetKeys.Add("FixedSun");
			SetKeys.Add("DenyTransacted");
			SetKeys.Add("BlockDwell");
			SetKeys.Add("DenyIdentified");
			SetKeys.Add("AllowVoice");
			SetKeys.Add("UseGlobalTime");
			SetKeys.Add("PricePerMeter");
			SetKeys.Add("TaxFree");
			SetKeys.Add("AllowDirectTeleport");
			SetKeys.Add("RedirectGridX");
			SetKeys.Add("RedirectGridY");
			SetKeys.Add("ParentEstateID");
			SetKeys.Add("SunPosition");
			SetKeys.Add("EstateSkipScripts");
			SetKeys.Add("BillableFactor");
			SetKeys.Add("PublicAccess");
			SetKeys.Add("AbuseEmail");
			SetKeys.Add("EstateOwner");
			SetKeys.Add("DenyMinors");
			SetKeys.Add("EstatePass");

			GenericData.Update("estate_settings", SetValues.ToArray(), SetKeys.ToArray(), new string[] { "EstateID" }, new object[] { es.EstateID });

			SaveBanList(es);
			SaveUUIDList(es.EstateID, "estate_managers", es.EstateManagers);
			SaveUUIDList(es.EstateID, "estate_users", es.EstateAccess);
			SaveUUIDList(es.EstateID, "estate_groups", es.EstateGroups);
			return true;
		}

		public void SaveEstateSettings(OpenSim.Framework.EstateSettings es)
		{
			List<object> SetValues = new List<object>();
            SetValues.Add(es.EstateID);
            SetValues.Add(es.EstateName);
            SetValues.Add(Convert.ToInt32(es.AbuseEmailToEstateOwner));
            SetValues.Add(Convert.ToInt32(es.DenyAnonymous));
            SetValues.Add(Convert.ToInt32(es.ResetHomeOnTeleport));
            SetValues.Add(Convert.ToInt32(es.FixedSun));
            SetValues.Add(Convert.ToInt32(es.DenyTransacted));
            SetValues.Add(Convert.ToInt32(es.BlockDwell));
            SetValues.Add(Convert.ToInt32(es.DenyIdentified));
            SetValues.Add(Convert.ToInt32(es.AllowVoice));
            SetValues.Add(Convert.ToInt32(es.UseGlobalTime));
            SetValues.Add(es.PricePerMeter);
            SetValues.Add(Convert.ToInt32(es.TaxFree));
            SetValues.Add(Convert.ToInt32(es.AllowDirectTeleport));
            SetValues.Add(es.RedirectGridX);
            SetValues.Add(es.RedirectGridY);
            SetValues.Add(es.ParentEstateID);
            SetValues.Add(es.SunPosition);
            SetValues.Add(Convert.ToInt32(es.EstateSkipScripts));
            SetValues.Add(es.BillableFactor);
            SetValues.Add(Convert.ToInt32(es.PublicAccess));
            SetValues.Add(es.AbuseEmail);
            SetValues.Add(es.EstateOwner);
            SetValues.Add(Convert.ToInt32(es.DenyMinors));
            SetValues.Add(es.EstatePass);

			List<string> SetKeys = new List<string>();
			SetKeys.Add("EstateID");
			SetKeys.Add("EstateName");
			SetKeys.Add("AbuseEmailToEstateOwner");
			SetKeys.Add("DenyAnonymous");
			SetKeys.Add("ResetHomeOnTeleport");
			SetKeys.Add("FixedSun");
			SetKeys.Add("DenyTransacted");
			SetKeys.Add("BlockDwell");
			SetKeys.Add("DenyIdentified");
			SetKeys.Add("AllowVoice");
			SetKeys.Add("UseGlobalTime");
			SetKeys.Add("PricePerMeter");
			SetKeys.Add("TaxFree");
			SetKeys.Add("AllowDirectTeleport");
			SetKeys.Add("RedirectGridX");
			SetKeys.Add("RedirectGridY");
			SetKeys.Add("ParentEstateID");
			SetKeys.Add("SunPosition");
			SetKeys.Add("EstateSkipScripts");
			SetKeys.Add("BillableFactor");
			SetKeys.Add("PublicAccess");
			SetKeys.Add("AbuseEmail");
			SetKeys.Add("EstateOwner");
			SetKeys.Add("DenyMinors");
			SetKeys.Add("EstatePass");

			GenericData.Update("estate_settings", SetValues.ToArray(), SetKeys.ToArray(), new string[] { "EstateID" }, new object[] { es.EstateID });

			SaveBanList(es);
			SaveUUIDList(es.EstateID, "estate_managers", es.EstateManagers);
			SaveUUIDList(es.EstateID, "estate_users", es.EstateAccess);
			SaveUUIDList(es.EstateID, "estate_groups", es.EstateGroups);
		}

		public List<int> GetEstates(string search)
		{
			List<int> result = new List<int>();
			List<string> RetVal = GenericData.Query("EstateName", search, "estate_settings", "EstateID");
            if (RetVal.Count == 0)
                return result;
            foreach (string val in RetVal)
            {
                if (val == "")
                    continue;
				result.Add(Convert.ToInt32(val));
			}
			return result;
		}

		public bool LinkRegion(OpenMetaverse.UUID regionID, int estateID, string password)
		{
			List<string> queriedpassword = GenericData.Query("EstateID", estateID, "estate_settings", "EstatePass");
			if (queriedpassword.Count == 0)
				return false;
			if (Util.Md5Hash(password) != queriedpassword[0])
				return false;

			GenericData.Insert("estate_map", new object[] {
				regionID,
				estateID
			});

			return true;
		}

		public List<OpenMetaverse.UUID> GetRegions(int estateID)
		{
			List<string> RegionIDs = GenericData.Query("EstateID", estateID, "estate_map", "RegionID");
			List<UUID> regions = new List<UUID>();
			foreach (string RegionID in RegionIDs)
				regions.Add(new UUID(RegionID));
			return regions;
		}

		public bool DeleteEstate(int estateID)
		{
			GenericData.Delete("estateban", new string[] { "EstateID" }, new object[] { estateID });

			GenericData.Delete("estate_groups", new string[] { "EstateID" }, new object[] { estateID });

			GenericData.Delete("estate_managers", new string[] { "EstateID" }, new object[] { estateID });

			GenericData.Delete("estate_map", new string[] { "EstateID" }, new object[] { estateID });

			GenericData.Delete("estate_settings", new string[] { "EstateID" }, new object[] { estateID });

			GenericData.Delete("estate_users", new string[] { "EstateID" }, new object[] { estateID });

			return true;
		}
	}
}
