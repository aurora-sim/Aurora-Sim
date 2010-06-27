using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;

namespace Aurora.Services.DataService
{
	public class LocalAbuseReportsConnector : IAbuseReportsConnector
	{
		private IGenericData GD = null;
		public LocalAbuseReportsConnector(IGenericData GenericData)
		{
            GD = GenericData;
            List<string> Results = GD.Query("Method", "AbuseReports", "Passwords", "Password");
            if (Results.Count == 0)
            {
                string newPass = MainConsole.Instance.CmdPrompt("Password to access Abuse Reports");
                GD.Insert("Passwords", new object[] { "AbuseReports", Util.Md5Hash(newPass) });
            }
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
			report.EstateID = int.Parse(Reports[8]);
			report.ScreenshotID = new UUID(Reports[9]);
			report.AbuseSummary = Reports[10];
			report.Number = int.Parse(Reports[11]);
			report.AssignedTo = Reports[12];
			report.Active = bool.Parse(Reports[13]);
			report.Checked = bool.Parse(Reports[14]);
			report.Notes = Reports[15];
			return report;
		}

		public void AddAbuseReport(AbuseReport report, string Password)
		{
            if (!CheckPassword(Password))
                return;
            List<object> InsertValues = new List<object>();
			InsertValues.Add(report.Category);
			InsertValues.Add(report.ReporterName);
			InsertValues.Add(report.ObjectName);
			InsertValues.Add(report.ObjectUUID);
			InsertValues.Add(report.AbuserName);
			InsertValues.Add(report.AbuseLocation);
			InsertValues.Add(report.AbuseDetails);
			InsertValues.Add(report.ObjectPosition);
			InsertValues.Add(report.EstateID);
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
			InsertValues.Add(report.Active);
			InsertValues.Add(report.Checked);
			InsertValues.Add(report.Notes);

			GD.Insert("abusereports", InsertValues.ToArray());
		}

        public void UpdateAbuseReport(AbuseReport report, string Password)
        {
            if (!CheckPassword(Password))
                return;
            //This is update, so we trust the number as it should know the number it's updating now.
            GD.Delete("abusereports", new string[] { "Number" }, new object[] { report.Number });
            AddAbuseReport(report, Password);
        }

        private bool CheckPassword(string Password)
        {
            List<string> TruePassword = GD.Query("Method", "AbuseReports", "passwords", "Password");
            if (TruePassword.Count == 0)
                return false;
            if (Util.Md5Hash(Password) == TruePassword[0])
                return true;
            return false;
        }
	}
}
