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
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services
{
    public class AgentInfoServerPostHandler : BaseStreamHandler
    {
        private readonly IAgentInfoService m_AgentInfoService;
        private readonly string m_SessionID;
        private readonly IRegistryCore m_registry;

        public AgentInfoServerPostHandler(string url, IRegistryCore registry, IAgentInfoService service,
                                          string SessionID) :
                                              base("POST", url)
        {
            m_AgentInfoService = service;
            m_registry = registry;
            m_SessionID = SessionID;
        }

        public override byte[] Handle(string path, Stream requestData,
                                      OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            string body = GetBodyAsString(requestData);

            //MainConsole.Instance.DebugFormat("[XXX]: query String: {0}", body);

            try
            {
                IGridRegistrationService urlModule =
                    m_registry.RequestModuleInterface<IGridRegistrationService>();
                OSDMap map = WebUtils.GetOSDMap(body);
                if (map != null)
                {
                    if (map.ContainsKey("Method"))
                    {
                        if (map["Method"] == "GetUserInfo")
                        {
                            if (urlModule != null)
                                if (!urlModule.CheckThreatLevel(m_SessionID, map["Method"], ThreatLevel.None))
                                    return FailureResult();
                            return GetUserInfo(map);
                        }
                        if (map["Method"] == "GetUserInfos")
                        {
                            if (urlModule != null)
                                if (!urlModule.CheckThreatLevel(m_SessionID, map["Method"], ThreatLevel.Low))
                                    return FailureResult();
                            return GetUserInfos(map);
                        }
                        if (map["Method"] == "GetAgentsLocations")
                        {
                            if (urlModule != null)
                                if (!urlModule.CheckThreatLevel(m_SessionID, map["Method"], ThreatLevel.Low))
                                    return FailureResult();
                            return GetAgentsLocations(map);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.WarnFormat("[AGENT INFO HANDLER]: Exception {0}", e);
            }

            return FailureResult();
        }

        #region Method-specific handlers

        private byte[] GetUserInfo(OSDMap request)
        {
            string userID = request["userID"].AsString();

            UserInfo result = m_AgentInfoService.GetUserInfo(userID);

            OSDMap resultMap = new OSDMap();
            if (result == null)
                resultMap["Result"] = "null";
            else
                resultMap["Result"] = result.ToOSD();
            return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(resultMap));
        }

        private byte[] GetUserInfos(OSDMap request)
        {
            OSDArray userIDs = (OSDArray) request["userIDs"];
            string[] users = new string[userIDs.Count];
            for (int i = 0; i < userIDs.Count; i++)
            {
                users[i] = userIDs[i];
            }

            UserInfo[] result = m_AgentInfoService.GetUserInfos(users);

            OSDArray resultArray = new OSDArray();
            foreach (UserInfo info in result)
            {
                resultArray.Add(info != null ? info.ToOSD() : new OSD());
            }

            OSDMap resultMap = new OSDMap();
            resultMap["Result"] = resultArray;
            return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(resultMap));
        }

        private byte[] GetAgentsLocations(OSDMap request)
        {
            OSDArray userIDs = (OSDArray) request["userIDs"];
            string requestor = request["requestor"].AsString();
            string[] users = new string[userIDs.Count];
            for (int i = 0; i < userIDs.Count; i++)
            {
                users[i] = userIDs[i];
            }

            string[] result = m_AgentInfoService.GetAgentsLocations(requestor, users);

            OSDArray resultArray = new OSDArray();
            foreach (string info in result)
            {
                resultArray.Add(info);
            }

            OSDMap resultMap = new OSDMap();
            resultMap["Result"] = resultArray;
            return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(resultMap));
        }

        #endregion

        #region Misc

        /// <summary>
        ///   Clean secure info out of the regions so that they do not get sent away from the grid service
        ///   This makes sure that the SessionID and other info is secure and cannot be retrieved remotely
        /// </summary>
        /// <param name = "regions"></param>
        /// <returns></returns>
        private List<GridRegion> CleanRegions(List<GridRegion> regions)
        {
            List<GridRegion> regionsToReturn = new List<GridRegion>();
            foreach (GridRegion region in regions)
            {
                regionsToReturn.Add(CleanRegion(region));
            }
            return regions;
        }

        /// <summary>
        ///   Clean secure info out of the regions so that they do not get sent away from the grid service
        ///   This makes sure that the SessionID and other info is secure and cannot be retrieved remotely
        /// </summary>
        /// <param name = "region"></param>
        /// <returns></returns>
        private GridRegion CleanRegion(GridRegion region)
        {
            if (region == null)
                return null;
            region.Flags = 0;
            region.SessionID = UUID.Zero;
            region.LastSeen = 0;
            region.AuthToken = "";
            return region;
        }

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

        private byte[] SuccessResult(string result)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["Result"] = "Success";
            sendData["Message"] = result;

            string xmlString = WebUtils.BuildXmlResponse(sendData);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
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