/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mono.Data.SqliteClient;
using OpenMetaverse;
using OpenSim.Framework;
using Aurora.Framework;

namespace OpenSim.Region.UserStatistics
{
    public class Sessions_Report : IStatsController
    {
        #region IStatsController Members

        public string ReportName
        {
            get { return "Sessions"; }
        }

        public Hashtable ProcessModel(Hashtable pParams)
        {
            Hashtable modeldata = new Hashtable();
            modeldata.Add("Scenes", pParams["Scenes"]);
            modeldata.Add("Reports", pParams["Reports"]);
            List<SessionList> lstSessions = new List<SessionList>();
            Hashtable requestvars = (Hashtable) pParams["RequestVars"];
            

            string puserUUID = string.Empty;
            string clientVersionString = string.Empty;

            if (requestvars != null)
            {
                if (requestvars.ContainsKey("UserID"))
                {
                    UUID testUUID = UUID.Zero;
                    if (UUID.TryParse(requestvars["UserID"].ToString(), out testUUID))
                    {
                        puserUUID = requestvars["UserID"].ToString();

                    }
                }

                if (requestvars.ContainsKey("VersionString"))
                {
                    clientVersionString = requestvars["VersionString"].ToString();
                }
            }

            lstSessions = Aurora.DataManager.DataManager.RequestPlugin<IWebStatsDataConnector>().GetSessionList(puserUUID, clientVersionString);
            
            modeldata["SessionData"] = lstSessions;
            return modeldata;
        }

        public string RenderView(Hashtable pModelResult)
        {
            List<SessionList> lstSession = (List<SessionList>) pModelResult["SessionData"];
            Dictionary<string, IStatsController> reports = (Dictionary<string, IStatsController>)pModelResult["Reports"];

            const string STYLESHEET =
    @"
<STYLE>
body
{
    font-size:15px; font-family:Helvetica, Verdana; color:Black;
}
TABLE.defaultr { }
TR.defaultr { padding: 5px; }
TD.header { font-weight:bold; padding:5px; }
TD.content {}
TD.contentright { text-align: right; }
TD.contentcenter { text-align: center; }
TD.align_top { vertical-align: top; }
</STYLE>
";

            StringBuilder output = new StringBuilder();
            HTMLUtil.HtmlHeaders_O(ref output);
            output.Append(STYLESHEET);
            HTMLUtil.HtmlHeaders_C(ref output);

            HTMLUtil.AddReportLinks(ref output, reports, "");

            HTMLUtil.TABLE_O(ref output, "defaultr");
            HTMLUtil.TR_O(ref output, "defaultr");
            HTMLUtil.TD_O(ref output, "header");
            output.Append("FirstName");
            HTMLUtil.TD_C(ref output);
            HTMLUtil.TD_O(ref output, "header");
            output.Append("LastName");
            HTMLUtil.TD_C(ref output);
            HTMLUtil.TD_O(ref output, "header");
            output.Append("SessionEnd");
            HTMLUtil.TD_C(ref output);
            HTMLUtil.TD_O(ref output, "header");
            output.Append("SessionLength");
            HTMLUtil.TD_C(ref output);
            HTMLUtil.TD_O(ref output, "header");
            output.Append("Client");
            HTMLUtil.TD_C(ref output);
            HTMLUtil.TR_C(ref output);
            if (lstSession.Count == 0)
            {
                HTMLUtil.TR_O(ref output, "");
                HTMLUtil.TD_O(ref output, "align_top", 1, 5);
                output.Append("No results for that query");
                HTMLUtil.TD_C(ref output);
                HTMLUtil.TR_C(ref output);
            }
            foreach (SessionList ssnlst in lstSession)
            {
                int cnt = 0;
                foreach (ShortSessionData sesdata in ssnlst.sessions)
                {
                    HTMLUtil.TR_O(ref output, "");
                    if (cnt++ == 0)
                    {
                        HTMLUtil.TD_O(ref output, "align_top", ssnlst.sessions.Count, 1);
                        output.Append(ssnlst.firstname);
                        HTMLUtil.TD_C(ref output);
                        HTMLUtil.TD_O(ref output, "align_top", ssnlst.sessions.Count, 1);
                        output.Append(ssnlst.lastname);
                        HTMLUtil.TD_C(ref output);
                    }
                    HTMLUtil.TD_O(ref output, "content");
                    output.Append(sesdata.last_update.ToShortDateString());
                    output.Append(" - ");
                    output.Append(sesdata.last_update.ToShortTimeString());
                    HTMLUtil.TD_C(ref output);
                    HTMLUtil.TD_O(ref output, "content");
                    TimeSpan dtlength = sesdata.last_update.Subtract(sesdata.start_time);
                    if (dtlength.Days > 0)
                    {
                        output.Append(dtlength.Days);
                        output.Append(" Days ");
                    }
                    if (dtlength.Hours > 0)
                    {
                        output.Append(dtlength.Hours);
                        output.Append(" Hours ");
                    }
                    if (dtlength.Minutes > 0)
                    {
                        output.Append(dtlength.Minutes);
                        output.Append(" Minutes");
                    }
                    HTMLUtil.TD_C(ref output);
                    HTMLUtil.TD_O(ref output, "content");
                    output.Append(sesdata.client_version);
                    HTMLUtil.TD_C(ref output);
                    HTMLUtil.TR_C(ref output);
                    
                }
                HTMLUtil.TR_O(ref output, "");
                HTMLUtil.TD_O(ref output, "align_top", 1, 5);
                HTMLUtil.HR(ref output, "");
                HTMLUtil.TD_C(ref output);
                HTMLUtil.TR_C(ref output);
            }
            HTMLUtil.TABLE_C(ref output);
            output.Append("</BODY>\n</HTML>");
            return output.ToString();
        }

        #endregion
    }
}
