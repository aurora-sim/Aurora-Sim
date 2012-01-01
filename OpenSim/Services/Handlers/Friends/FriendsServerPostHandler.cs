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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Aurora.Simulation.Base;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;

namespace OpenSim.Services
{
    public class FriendsServerPostHandler : BaseStreamHandler
    {
        private readonly IFriendsService m_FriendsService;
        protected string m_SessionID;
        protected IRegistryCore m_registry;

        public FriendsServerPostHandler(string url, IFriendsService service, string SessionID, IRegistryCore registry) :
            base("POST", url)
        {
            m_FriendsService = service;
            m_SessionID = SessionID;
            m_registry = registry;
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

                string method = request["METHOD"].ToString();

                IGridRegistrationService urlModule =
                    m_registry.RequestModuleInterface<IGridRegistrationService>();

                switch (method)
                {
                    case "getfriends":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GetFriends(request);

                    case "storefriend":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return StoreFriend(request);

                    case "deletefriend":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return DeleteFriend(request);
                }
                MainConsole.Instance.DebugFormat("[FRIENDS HANDLER]: unknown method {0} request {1}", method.Length, method);
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[FRIENDS HANDLER]: Exception {0}", e);
            }

            return FailureResult();
        }

        #region Method-specific handlers

        private byte[] GetFriends(Dictionary<string, object> request)
        {
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                MainConsole.Instance.WarnFormat("[FRIENDS HANDLER]: no principalID in request to get friends");

            FriendInfo[] finfos = m_FriendsService.GetFriends(principalID);
            //MainConsole.Instance.DebugFormat("[FRIENDS HANDLER]: neighbours for region {0}: {1}", regionID, rinfos.Count);

            Dictionary<string, object> result = new Dictionary<string, object>();
            if ((finfos == null) || ((finfos != null) && (finfos.Length == 0)))
                result["result"] = "null";
            else
            {
                int i = 0;
#if (!ISWIN)
                foreach (FriendInfo finfo in finfos)
                {
                    Dictionary<string, object> rinfoDict = finfo.ToKeyValuePairs();
                    result["friend" + i] = rinfoDict;
                    i++;
                }
#else
                foreach (Dictionary<string, object> rinfoDict in finfos.Select(finfo => finfo.ToKeyValuePairs()))
                {
                    result["friend" + i] = rinfoDict;
                    i++;
                }
#endif
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[FRIENDS HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] StoreFriend(Dictionary<string, object> request)
        {
            FriendInfo friend = new FriendInfo(request);

            bool success = m_FriendsService.StoreFriend(friend.PrincipalID, friend.Friend, friend.MyFlags);

            if (success)
                return SuccessResult();
            else
                return FailureResult();
        }

        private byte[] DeleteFriend(Dictionary<string, object> request)
        {
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                MainConsole.Instance.WarnFormat("[FRIENDS HANDLER]: no principalID in request to delete friend");
            string friend = string.Empty;
            if (request.ContainsKey("FRIEND"))
                friend = request["FRIEND"].ToString();

            bool success = m_FriendsService.Delete(principalID, friend);
            if (success)
                return SuccessResult();
            else
                return FailureResult();
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