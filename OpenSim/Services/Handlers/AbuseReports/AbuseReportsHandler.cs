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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Aurora.Simulation.Base;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.Handlers.AbuseReports
{
    public class AbuseReportsHandler : BaseStreamHandler
    {
        private readonly IAbuseReports m_AbuseReportsService;
        private readonly string m_SessionID;
        private readonly IRegistryCore m_registry;

        public AbuseReportsHandler(string url, IAbuseReports service, IRegistryCore reg, string SessionID) :
            base("POST", url)
        {
            m_AbuseReportsService = service;
            m_registry = reg;
            m_SessionID = SessionID;
        }

        public override byte[] Handle(string path, Stream requestData,
                                      OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            //MainConsole.Instance.DebugFormat("[XXX]: query String: {0}", body);

            try
            {
                Dictionary<string, object> request =
                    WebUtils.ParseQueryString(body);

                if (!request.ContainsKey("METHOD"))
                    return FailureResult();
                IGridRegistrationService urlModule =
                    m_registry.RequestModuleInterface<IGridRegistrationService>();

                string method = request["METHOD"].ToString();
                            
                switch (method)
                {
                    case "AddAbuseReport":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return AddAbuseReport(request);
                    case "GetAbuseReport":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return GetAbuseReport(request);
                    case "UpdateAbuseReport":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return UpdateAbuseReport(request);
                    case "UpdateAbuseReports":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return GetAbuseReports(request);
                }
                MainConsole.Instance.DebugFormat("[ABUSEREPORT HANDLER]: unknown method {0} request {1}", method.Length, method);
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[ABUSEREPORT HANDLER]: Exception {0}", e);
            }

            return FailureResult();
        }

        #region Method-specific handlers

        private byte[] AddAbuseReport(Dictionary<string, object> request)
        {
            AbuseReport ar = new AbuseReport();
            ar.FromKVP(request);
            m_AbuseReportsService.AddAbuseReport(ar);
            //MainConsole.Instance.DebugFormat("[ABUSEREPORTS HANDLER]: neighbours for region {0}: {1}", regionID, rinfos.Count);

            return SuccessResult();
        }

        private byte[] UpdateAbuseReport(Dictionary<string, object> request)
        {
            AbuseReport ar = new AbuseReport();
            ar.FromKVP(request);
            m_AbuseReportsService.UpdateAbuseReport(ar, request["Password"].ToString());
            //MainConsole.Instance.DebugFormat("[ABUSEREPORTS HANDLER]: neighbours for region {0}: {1}", regionID, rinfos.Count);

            return SuccessResult();
        }

        private byte[] GetAbuseReport(Dictionary<string, object> request)
        {
            string xmlString = WebUtils.BuildXmlResponse(
                m_AbuseReportsService.GetAbuseReport(int.Parse(request["Number"].ToString()),
                                                     request["Password"].ToString()).ToKVP());
            //MainConsole.Instance.DebugFormat("[FRIENDS HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetAbuseReports(Dictionary<string, object> request)
        {
            List<AbuseReport> ars = m_AbuseReportsService.GetAbuseReports(int.Parse(request["start"].ToString()),
                                                                          int.Parse(request["count"].ToString()),
                                                                          request["filter"].ToString());
#if (!ISWIN)
            Dictionary<string, object> returnvalue = new Dictionary<string, object>();
            foreach (AbuseReport ar in ars)
                returnvalue.Add(ar.Number.ToString(), ar);
#else
            Dictionary<string, object> returnvalue = ars.ToDictionary<AbuseReport, string, object>(ar => ar.Number.ToString(), ar => ar);
#endif

            string xmlString = WebUtils.BuildXmlResponse(returnvalue);
            //MainConsole.Instance.DebugFormat("[FRIENDS HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        #endregion

        #region Misc

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                                             "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                                                       "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] FailureResult()
        {
            return FailureResult(String.Empty);
        }

        private byte[] FailureResult(string msg)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                                             "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                                                       "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Failure"));

            rootElement.AppendChild(result);

            XmlElement message = doc.CreateElement("", "Message", "");
            message.AppendChild(doc.CreateTextNode(msg));

            rootElement.AppendChild(message);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null) {Formatting = Formatting.Indented};
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        #endregion
    }
}