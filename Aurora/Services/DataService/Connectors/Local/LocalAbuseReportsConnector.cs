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
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenSim.Framework;
using Nini.Config;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class LocalAbuseReportsConnector : IAbuseReportsConnector
	{
		private IGenericData GD = null;
        private string WebPassword = "";

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            GD = GenericData;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString ("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase (defaultConnectionString, "AbuseReports", source.Configs["AuroraConnectors"].GetBoolean ("ValidateTables", true));

            DataManager.DataManager.RegisterPlugin (Name + "Local", this);
            if (source.Configs["AuroraConnectors"].GetString ("AbuseReportsConnector", "LocalConnector") == "LocalConnector")
            {
                WebPassword = Util.Md5Hash(source.Configs["Handlers"].GetString("WireduxHandlerPassword", String.Empty));

                //List<string> Results = GD.Query("Method", "abusereports", "passwords", "Password");
                //if (Results.Count == 0)
                //{
                //    string newPass = MainConsole.Instance.PasswdPrompt("Password to access Abuse Reports");
                //    GD.Insert("passwords", new object[] { "abusereports", Util.Md5Hash(Util.Md5Hash(newPass)) });
                //}
                DataManager.DataManager.RegisterPlugin (Name, this);
            }

        }

        public string Name
        {
            get { return "IAbuseReportsConnector"; }
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Gets the abuse report associated with the number and uses the pass to authenticate.
        /// </summary>
        /// <param name="Number"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
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

        public List<AbuseReport> GetAbuseReports(int start, int count, string filter)
        {
            List<AbuseReport> rv = new List<AbuseReport> ();
            System.Data.IDataReader dr = GD.QueryData ("where CONVERT(number, UNSIGNED) >= " + start.ToString () + " and " + filter + " LIMIT 0, 10", "abusereports", "*");
            try
            {
                while (dr.Read ())
                {
                    AbuseReport report = new AbuseReport ();
                    report.Category = dr[0].ToString ();
                    report.ReporterName = dr[1].ToString ();
                    report.ObjectName = dr[2].ToString ();
                    report.ObjectUUID = new UUID (dr[3].ToString ());
                    report.AbuserName = dr[4].ToString ();
                    report.AbuseLocation = dr[5].ToString ();
                    report.AbuseDetails = dr[6].ToString ();
                    report.ObjectPosition = dr[7].ToString ();
                    report.RegionName = dr[8].ToString ();
                    report.ScreenshotID = new UUID (dr[9].ToString ());
                    report.AbuseSummary = dr[10].ToString ();
                    report.Number = int.Parse (dr[11].ToString ());
                    report.AssignedTo = dr[12].ToString ();
                    report.Active = int.Parse (dr[13].ToString ()) == 1;
                    report.Checked = int.Parse (dr[14].ToString ()) == 1;
                    report.Notes = dr[15].ToString ();
                    rv.Add (report);
                }
                dr.Close ();
                dr.Dispose ();
            }
            catch
            {
            }
            GD.CloseDatabase ();
            return rv;
        }

        /// <summary>
        /// Adds a new abuse report to the database
        /// </summary>
        /// <param name="report"></param>
        /// <param name="Password"></param>
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

        /// <summary>
        /// Updates an abuse report and authenticates with the password.
        /// </summary>
        /// <param name="report"></param>
        /// <param name="Password"></param>
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
            InsertKeys.Add("RegionName");
            InsertKeys.Add("ScreenshotID");
            InsertKeys.Add("AbuseSummary");
            InsertKeys.Add("Number");
            InsertKeys.Add("AssignedTo");
            InsertKeys.Add("Active");
            InsertKeys.Add("Checked");
            InsertKeys.Add("Notes");

            GD.Replace("abusereports", InsertKeys.ToArray(),InsertValues.ToArray());
        }

        /// <summary>
        /// Check the user's password, not currently used
        /// </summary>
        /// <param name="Password"></param>
        /// <returns></returns>
        private bool CheckPassword(string Password)
        {
            if (Password == WebPassword)
                return true;
            string OtherPass = Util.Md5Hash (Password);
            if (OtherPass == WebPassword)
                return true;
            List<string> TruePassword = GD.Query("Method", "abusereports", "passwords", "Password");
            if (TruePassword.Count == 0)
                return false;
            if (OtherPass == TruePassword[0])
                return true;
            return false;
        }
    }
}
