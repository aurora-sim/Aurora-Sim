/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    public class AbuseReport : IDataTransferable
    {
        public string AbuseDetails;
        public string AbuseLocation;
        public string AbuseSummary;
        public string AbuserName;
        public bool Active;
        public string AssignedTo;
        public object Category;
        public bool Checked;
        public string Notes;
        public int Number;
        public string ObjectName;
        public string ObjectPosition;
        public UUID ObjectUUID;
        public string RegionName;
        public string ReporterName;
        public UUID ScreenshotID;

        public AbuseReport()
        {
        }

        public override void FromKVP(Dictionary<string, object> DicCol)
        {
            AbuseDetails = DicCol["AbuseDetails"].ToString();
            AbuseLocation = DicCol["AbuseLocation"].ToString();
            AbuserName = DicCol["AbuserName"].ToString();
            AbuseSummary = DicCol["AbuseSummary"].ToString();
            Active = Convert.ToBoolean(DicCol["Active"].ToString());
            AssignedTo = DicCol["AssignedTo"].ToString();
            Category = DicCol["Category"].ToString();
            Checked = Convert.ToBoolean(DicCol["Checked"]);
            Notes = DicCol["Notes"].ToString();
            Number = int.Parse(DicCol["Number"].ToString());
            ObjectName = DicCol["ObjectName"].ToString();
            ObjectPosition = DicCol["ObjectPosition"].ToString();
            ObjectUUID = new UUID(DicCol["ObjectUUID"].ToString());
            RegionName = DicCol["RegionName"].ToString();
            ReporterName = DicCol["ReporterName"].ToString();
            ScreenshotID = new UUID(DicCol["ScreenshotID"].ToString());
        }

        public override Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> NewDicCol = new Dictionary<string, object>();
            NewDicCol["AbuseDetails"] = AbuseDetails;
            NewDicCol["AbuseLocation"] = AbuseLocation;
            NewDicCol["AbuserName"] = AbuserName;
            NewDicCol["AbuseSummary"] = AbuseSummary;
            NewDicCol["Active"] = Active;
            NewDicCol["AssignedTo"] = AssignedTo;
            NewDicCol["Category"] = Category;
            NewDicCol["Checked"] = Checked;
            NewDicCol["Notes"] = Notes;
            NewDicCol["Number"] = Number;
            NewDicCol["ObjectName"] = ObjectName;
            NewDicCol["ObjectPosition"] = ObjectPosition;
            NewDicCol["ObjectUUID"] = ObjectUUID;
            NewDicCol["RegionName"] = RegionName;
            NewDicCol["ReporterName"] = ReporterName;
            NewDicCol["ScreenshotID"] = ScreenshotID;
            return NewDicCol;
        }

        public override void FromOSD(OSDMap DicCol)
        {
            AbuseDetails = DicCol["AbuseDetails"].AsString();
            AbuseLocation = DicCol["AbuseLocation"].AsString();
            AbuserName = DicCol["AbuserName"].AsString();
            AbuseSummary = DicCol["AbuseSummary"].AsString();
            Active = DicCol["Active"].AsBoolean();
            AssignedTo = DicCol["AssignedTo"].AsString();
            Category = DicCol["Category"].AsString();
            Checked = Convert.ToBoolean(DicCol["Checked"].AsString());
            Notes = DicCol["Notes"].AsString();
            Number = DicCol["Number"].AsInteger();
            ObjectName = DicCol["ObjectName"].AsString();
            ObjectPosition = DicCol["ObjectPosition"].AsString();
            ObjectUUID = DicCol["ObjectUUID"].AsUUID();
            RegionName = DicCol["RegionName"].AsString();
            ReporterName = DicCol["ReporterName"].AsString();
            ScreenshotID = new UUID(DicCol["ScreenshotID"].AsString());
        }

        public override OSDMap ToOSD()
        {
            OSDMap NewDicCol = new OSDMap();
            NewDicCol["AbuseDetails"] = AbuseDetails;
            NewDicCol["AbuseLocation"] = AbuseLocation;
            NewDicCol["AbuserName"] = AbuserName;
            NewDicCol["AbuseSummary"] = AbuseSummary;
            NewDicCol["Active"] = Active;
            NewDicCol["AssignedTo"] = AssignedTo;
            NewDicCol["Category"] = Category.ToString();
            NewDicCol["Checked"] = Checked;
            NewDicCol["Notes"] = Notes;
            NewDicCol["Number"] = Number;
            NewDicCol["ObjectName"] = ObjectName;
            NewDicCol["ObjectPosition"] = ObjectPosition;
            NewDicCol["ObjectUUID"] = ObjectUUID;
            NewDicCol["RegionName"] = RegionName;
            NewDicCol["ReporterName"] = ReporterName;
            NewDicCol["ScreenshotID"] = ScreenshotID;
            return NewDicCol;
        }
    }

    public interface IAbuseReports
    {
        /// <summary>
        ///   Gets the abuse report associated with the number and uses the pass to authenticate.
        /// </summary>
        /// <param name = "Number"></param>
        /// <param name = "Password"></param>
        /// <returns></returns>
        AbuseReport GetAbuseReport(int Number, string Password);

        /// <summary>
        ///   Adds a new abuse report to the database
        /// </summary>
        /// <param name = "report"></param>
        /// <param name = "Password"></param>
        void AddAbuseReport(AbuseReport report);

        /// <summary>
        ///   Updates an abuse report and authenticates with the password.
        /// </summary>
        /// <param name = "report"></param>
        /// <param name = "Password"></param>
        void UpdateAbuseReport(AbuseReport report, string Password);

        /// <summary>
        ///   Gets a collection of abuse reports
        /// </summary>
        /// <param name = "start"></param>
        /// <param name = "count"></param>
        /// <param name = "filter"></param>
        /// <returns></returns>
        List<AbuseReport> GetAbuseReports(int start, int count, bool active);
    }
}