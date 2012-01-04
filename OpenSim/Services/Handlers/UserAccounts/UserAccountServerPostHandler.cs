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

namespace OpenSim.Services
{
    public class UserAccountServerPostHandler : BaseStreamHandler
    {
        private readonly IUserAccountService m_UserAccountService;
        protected string m_SessionID;
        protected IRegistryCore m_registry;

        public UserAccountServerPostHandler(string url, IUserAccountService service, string SessionID,
                                            IRegistryCore registry) :
                                                base("POST", url)
        {
            m_UserAccountService = service.InnerService;
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

            // We need to check the authorization header
            //httpRequest.Headers["authorization"] ...

            //MainConsole.Instance.DebugFormat("[XXX]: query String: {0}", body);
            string method = string.Empty;
            try
            {
                Dictionary<string, object> request =
                    WebUtils.ParseQueryString(body);

                if (!request.ContainsKey("METHOD"))
                    return FailureResult();

                method = request["METHOD"].ToString();
                IGridRegistrationService urlModule =
                    m_registry.RequestModuleInterface<IGridRegistrationService>();
                switch (method)
                {
                    case "getaccount":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.None))
                                return new byte[0];
                        return GetAccount(request);
                    case "getaccounts":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.None))
                                return new byte[0];
                        return GetAccounts(request);
                    case "setaccount":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Full))
                                return new byte[0];
                        return StoreAccount(request);
                }
                MainConsole.Instance.DebugFormat("[USER SERVICE HANDLER]: unknown method request: {0}", method);
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[USER SERVICE HANDLER]: Exception in method {0}: {1}", method, e);
            }

            return FailureResult();
        }

        private byte[] GetAccount(Dictionary<string, object> request)
        {
            UserAccount account = null;
            UUID scopeID = UUID.Zero;
            Dictionary<string, object> result = new Dictionary<string, object>();

            if (!request.ContainsKey("ScopeID"))
            {
                result["result"] = "null";
                return ResultToBytes(result);
            }

            if (!UUID.TryParse(request["ScopeID"].ToString(), out scopeID))
            {
                result["result"] = "null";
                return ResultToBytes(result);
            }

            if (request.ContainsKey("UserID") && request["UserID"] != null)
            {
                UUID userID;
                if (UUID.TryParse(request["UserID"].ToString(), out userID))
                    account = m_UserAccountService.GetUserAccount(scopeID, userID);
            }
            else if (request.ContainsKey("Name") && request["Name"] != null)
                account = m_UserAccountService.GetUserAccount(scopeID, request["Name"].ToString());
            else if (request.ContainsKey("FirstName") && request.ContainsKey("LastName") &&
                     request["FirstName"] != null && request["LastName"] != null)
                account = m_UserAccountService.GetUserAccount(scopeID, request["FirstName"].ToString(),
                                                              request["LastName"].ToString());

            if (account == null)
                result["result"] = "null";
            else
            {
                result["result"] = account.ToKVP();
            }

            return ResultToBytes(result);
        }

        private byte[] GetAccounts(Dictionary<string, object> request)
        {
            if (!request.ContainsKey("ScopeID") || !request.ContainsKey("query"))
                return FailureResult();

            UUID scopeID = UUID.Zero;
            if (!UUID.TryParse(request["ScopeID"].ToString(), out scopeID))
                return FailureResult();

            string query = request["query"].ToString();

            List<UserAccount> accounts = m_UserAccountService.GetUserAccounts(scopeID, query);

            Dictionary<string, object> result = new Dictionary<string, object>();
            if ((accounts == null) || ((accounts != null) && (accounts.Count == 0)))
                result["result"] = "null";
            else
            {
                int i = 0;
                foreach(UserAccount acc in accounts)
                {
                    result["account" + i] = acc.ToKVP();
                    i++;
                }
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] StoreAccount(Dictionary<string, object> request)
        {
            // No can do. No changing user accounts from remote sims
            return FailureResult();
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

            XmlElement result = doc.CreateElement("", "result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] FailureResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                                             "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                                                       "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "result", "");
            result.AppendChild(doc.CreateTextNode("Failure"));

            rootElement.AppendChild(result);

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

        private byte[] ResultToBytes(Dictionary<string, object> result)
        {
            string xmlString = WebUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }
    }
}