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
using System.Data;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class LocalAbuseReportsConnector : IAbuseReportsConnector
    {
        private IGenericData GD;
        private string WebPassword = "";

        #region IAbuseReportsConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            GD = GenericData;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase(defaultConnectionString, "AbuseReports",
                                 source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            DataManager.DataManager.RegisterPlugin(Name + "Local", this);
            if (source.Configs["AuroraConnectors"].GetString("AbuseReportsConnector", "LocalConnector") ==
                "LocalConnector")
            {
                WebPassword = Util.Md5Hash(source.Configs["Handlers"].GetString("WebUIHandlerPassword", String.Empty));

                //List<string> Results = GD.Query("Method", "abusereports", "passwords", "Password");
                //if (Results.Count == 0)
                //{
                //    string newPass = MainConsole.Instance.PasswdPrompt("Password to access Abuse Reports");
                //    GD.Insert("passwords", new object[] { "abusereports", Util.Md5Hash(Util.Md5Hash(newPass)) });
                //}
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IAbuseReportsConnector"; }
        }

        /// <summary>
        ///   Gets the abuse report associated with the number and uses the pass to authenticate.
        /// </summary>
        /// <param name = "Number"></param>
        /// <param name = "Password"></param>
        /// <returns></returns>
        public AbuseReport GetAbuseReport(int Number, string Password)
        {
            if (!CheckPassword(Password))
            {
                return null;
            }

            QueryFilter filter = new QueryFilter();
            filter.andFilters["Number"] = Number;
            List<string> Reports = GD.Query(new string[] { "*" }, "abusereports", filter, null, null, null);

            return (Reports.Count == 0) ? null : new AbuseReport
            {
                Category = Reports[0],
                ReporterName = Reports[1],
                ObjectName = Reports[2],
                ObjectUUID = new UUID(Reports[3]),
                AbuserName = Reports[4],
                AbuseLocation = Reports[5],
                AbuseDetails = Reports[6],
                ObjectPosition = Reports[7],
                RegionName = Reports[8],
                ScreenshotID = new UUID(Reports[9]),
                AbuseSummary = Reports[10],
                Number = int.Parse(Reports[11]),
                AssignedTo = Reports[12],
                Active = int.Parse(Reports[13]) == 1,
                Checked = int.Parse(Reports[14]) == 1,
                Notes = Reports[15]
            };
        }

        public List<AbuseReport> GetAbuseReports(int start, int count, string filter)
        {
            List<AbuseReport> rv = new List<AbuseReport>();
            IDataReader dr =
                GD.QueryData(
                    "where CAST(number AS UNSIGNED) >= " + start.ToString() + " and " + filter + " LIMIT 0, 10",
                    "abusereports", "*");
            try
            {
                while (dr.Read())
                {
                    AbuseReport report = new AbuseReport
                                             {
                                                 Category = dr[0].ToString(),
                                                 ReporterName = dr[1].ToString(),
                                                 ObjectName = dr[2].ToString(),
                                                 ObjectUUID = new UUID(dr[3].ToString()),
                                                 AbuserName = dr[4].ToString(),
                                                 AbuseLocation = dr[5].ToString(),
                                                 AbuseDetails = dr[6].ToString(),
                                                 ObjectPosition = dr[7].ToString(),
                                                 RegionName = dr[8].ToString(),
                                                 ScreenshotID = new UUID(dr[9].ToString()),
                                                 AbuseSummary = dr[10].ToString(),
                                                 Number = int.Parse(dr[11].ToString()),
                                                 AssignedTo = dr[12].ToString(),
                                                 Active = int.Parse(dr[13].ToString()) == 1,
                                                 Checked = int.Parse(dr[14].ToString()) == 1,
                                                 Notes = dr[15].ToString()
                                             };
                    rv.Add(report);
                }
                dr.Close();
                dr.Dispose();
            }
            catch
            {
            }
            GD.CloseDatabase();
            return rv;
        }

        /// <summary>
        ///   Adds a new abuse report to the database
        /// </summary>
        /// <param name = "report"></param>
        /// <param name = "Password"></param>
        public void AddAbuseReport(AbuseReport report)
        {
            List<object> InsertValues = new List<object>{
                report.Category.ToString().MySqlEscape(100),
                report.ReporterName.MySqlEscape(100),
                report.ObjectName.MySqlEscape(100),
                report.ObjectUUID,
                report.AbuserName.MySqlEscape(100),
                report.AbuseLocation.MySqlEscape(100),
                report.AbuseDetails.MySqlEscape(512),
                report.ObjectPosition.MySqlEscape(100),
                report.RegionName.MySqlEscape(100),
                report.ScreenshotID,
                report.AbuseSummary.MySqlEscape(100)
            };

            Dictionary<string, bool> sort = new Dictionary<string, bool>(1);
            sort["Number"] = false;

            //We do not trust the number sent by the region. Always find it ourselves
            List<string> values = GD.Query(new string[1] { "Number" }, "abusereports", null, sort, null, null);
            report.Number = values.Count == 0 ? 0 : int.Parse(values[0]);

            report.Number++;

            InsertValues.Add(report.Number);

            InsertValues.Add(report.AssignedTo.MySqlEscape(100));
            InsertValues.Add(report.Active ? 1 : 0);
            InsertValues.Add(report.Checked ? 1 : 0);
            InsertValues.Add(report.Notes.MySqlEscape(1024));

            GD.Insert("abusereports", InsertValues.ToArray());
        }

        /// <summary>
        ///   Updates an abuse report and authenticates with the password.
        /// </summary>
        /// <param name = "report"></param>
        /// <param name = "Password"></param>
        public void UpdateAbuseReport(AbuseReport report, string Password)
        {
            if (!CheckPassword(Password))
            {
                return;
            }
            Dictionary<string, object> row = new Dictionary<string, object>(16);
            //This is update, so we trust the number as it should know the number it's updating now.
            row["Category"] = report.Category.ToString().MySqlEscape(100);
            row["ReporterName"] = report.ReporterName.MySqlEscape(100);
            row["ObjectName"] = report.ObjectName.MySqlEscape(100);
            row["ObjectUUID"] = report.ObjectUUID;
            row["AbuserName"] = report.AbuserName.MySqlEscape(100);
            row["AbuseLocation"] = report.AbuseLocation.MySqlEscape(100);
            row["AbuseDetails"] = report.AbuseDetails.MySqlEscape(512);
            row["ObjectPosition"] = report.ObjectPosition.MySqlEscape(100);
            row["RegionName"] = report.RegionName.MySqlEscape(100);
            row["ScreenshotID"] = report.ScreenshotID;
            row["AbuseSummary"] = report.AbuseSummary.MySqlEscape(100);
            row["Number"] = report.Number;
            row["AssignedTo"] = report.AssignedTo.MySqlEscape(100);
            row["Active"] = report.Active ? 1 : 0;
            row["Checked"] = report.Checked ? 1 : 0;
            row["Notes"] = report.Notes.MySqlEscape(1024);

            GD.Replace("abusereports", row);
        }

        #endregion

        public void Dispose()
        {
        }

        /// <summary>
        ///   Check the user's password, not currently used
        /// </summary>
        /// <param name = "Password"></param>
        /// <returns></returns>
        private bool CheckPassword(string Password)
        {
            if (Password == WebPassword)
            {
                return true;
            }
            string OtherPass = Util.Md5Hash(Password);
            if (OtherPass == WebPassword)
            {
                return true;
            }
            QueryFilter filter = new QueryFilter();
            filter.andFilters["Method"] = "abusereports";
            List<string> TruePassword = GD.Query(new string[] { "Password" }, "passwords", filter, null, null, null);

            return !(
                TruePassword.Count == 0 ||
                OtherPass == TruePassword[0]
            );
        }
    }
}