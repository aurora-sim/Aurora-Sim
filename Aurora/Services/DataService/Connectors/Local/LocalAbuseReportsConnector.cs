using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using Nini.Config;

namespace Aurora.Services.DataService
{
    public class LocalAbuseReportsConnector : IAbuseReportsConnector, IAuroraDataPlugin
	{
		private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, IConfigSource source, string defaultConnectionString)
        {
            if(source.Configs["AuroraConnectors"].GetString("AbuseReportsConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString);

                List<string> Results = GD.Query("Method", "abusereports", "passwords", "Password");
                if (Results.Count == 0)
                {
                    string newPass = MainConsole.Instance.PasswdPrompt("Password to access Abuse Reports");
                    GD.Insert("passwords", new object[] { "abusereports", Util.Md5Hash(Util.Md5Hash(newPass)) });
                }
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IAbuseReportsConnector"; }
        }

        public void Dispose()
        {
        }

		public AbuseReport GetAbuseReport(int Number, string Password)
		{
            if (!CheckPassword(Password))
                return null;
			AbuseReport report = new AbuseReport();
            List<string> Reports = GD.Query("Number", Number, "abusereports", "*");
            if (Reports.Count == 0)
                return null;
            report.Category = Reports[0];
			report.ReporterName = Reports[1];
			report.ObjectName = Reports[2];
			report.ObjectUUID = new UUID(Reports[3]);
			report.AbuserName = Reports[4];
			report.AbuseLocation = Reports[5];
			report.AbuseDetails = Reports[6];
			report.ObjectPosition = Reports[7];
            report.RegionName = Reports[8];
			report.ScreenshotID = new UUID(Reports[9]);
			report.AbuseSummary = Reports[10];
			report.Number = int.Parse(Reports[11]);
			report.AssignedTo = Reports[12];
			report.Active = int.Parse(Reports[13]) == 1;
			report.Checked = int.Parse(Reports[14]) == 1;
			report.Notes = Reports[15];
			return report;
		}

		public void AddAbuseReport(AbuseReport report)
		{
            List<object> InsertValues = new List<object>();
			InsertValues.Add(report.Category);
			InsertValues.Add(report.ReporterName);
			InsertValues.Add(report.ObjectName);
			InsertValues.Add(report.ObjectUUID);
			InsertValues.Add(report.AbuserName);
			InsertValues.Add(report.AbuseLocation);
			InsertValues.Add(report.AbuseDetails);
			InsertValues.Add(report.ObjectPosition);
            InsertValues.Add(report.RegionName);
			InsertValues.Add(report.ScreenshotID);
			InsertValues.Add(report.AbuseSummary);

			//We do not trust the number sent by the region. Always find it ourselves
			List<string> values = GD.Query("", "", "abusereports", "Number", " ORDER BY Number DESC");
			if (values.Count == 0)
				report.Number = 0;
			else
				report.Number = int.Parse(values[0]);

            report.Number++;

			InsertValues.Add(report.Number);

			InsertValues.Add(report.AssignedTo);
            InsertValues.Add(report.Active ? 1 : 0);
            InsertValues.Add(report.Checked ? 1 : 0);
			InsertValues.Add(report.Notes);

			GD.Insert("abusereports", InsertValues.ToArray());
		}

        public void UpdateAbuseReport(AbuseReport report, string Password)
        {
            if (!CheckPassword(Password))
                return;
            //This is update, so we trust the number as it should know the number it's updating now.
            List<object> InsertValues = new List<object>();
            InsertValues.Add(report.Category);
            InsertValues.Add(report.ReporterName);
            InsertValues.Add(report.ObjectName);
            InsertValues.Add(report.ObjectUUID);
            InsertValues.Add(report.AbuserName);
            InsertValues.Add(report.AbuseLocation);
            InsertValues.Add(report.AbuseDetails);
            InsertValues.Add(report.ObjectPosition);
            InsertValues.Add(report.RegionName);
            InsertValues.Add(report.ScreenshotID);
            InsertValues.Add(report.AbuseSummary);
            InsertValues.Add(report.Number);

            InsertValues.Add(report.AssignedTo);
            InsertValues.Add(report.Active ? 1 : 0);
            InsertValues.Add(report.Checked ? 1 : 0);
            InsertValues.Add(report.Notes);

            List<string> InsertKeys = new List<string>();
            InsertKeys.Add("Category");
            InsertKeys.Add("ReporterName");
            InsertKeys.Add("ObjectName");
            InsertKeys.Add("ObjectUUID");
            InsertKeys.Add("AbuserName");
            InsertKeys.Add("AbuseLocation");
            InsertKeys.Add("AbuseDetails");
            InsertKeys.Add("ObjectPosition");
            InsertKeys.Add("EstateID");
            InsertKeys.Add("ScreenshotID");
            InsertKeys.Add("AbuseSummary");
            InsertKeys.Add("Number");
            InsertKeys.Add("AssignedTo");
            InsertKeys.Add("Active");
            InsertKeys.Add("Checked");
            InsertKeys.Add("Notes");

            GD.Replace("abusereports", InsertKeys.ToArray(),InsertValues.ToArray());
        }

        private bool CheckPassword(string Password)
        {
            List<string> TruePassword = GD.Query("Method", "abusereports", "passwords", "Password");
            if (TruePassword.Count == 0)
                return false;
            string OtherPass = Util.Md5Hash(Password);
            if (OtherPass == TruePassword[0])
                return true;
            return false;
        }
    }
}
