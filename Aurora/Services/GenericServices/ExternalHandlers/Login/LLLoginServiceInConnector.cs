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

using Aurora.Framework;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Interfaces;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections;
using System.Net;

namespace Aurora.Services
{
    public class LLLoginServiceInConnector : IService
    {
        private IConfigSource m_Config;

        private ILoginService m_loginService;
        private bool m_Proxy;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("LLLoginHandler", "") != Name)
                return;

            IHttpServer server =
                registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(
                    (uint) handlerConfig.GetInt("LLLoginHandlerPort"));
            MainConsole.Instance.Debug("[LLLOGIN IN CONNECTOR]: Starting...");
            ReadLocalServiceFromConfig(config);
            m_loginService = registry.RequestModuleInterface<ILoginService>();

            InitializeHandlers(server);
        }

        public void FinishedStartup()
        {
        }

        #endregion

        private void ReadLocalServiceFromConfig(IConfigSource config)
        {
            m_Config = config;
            IConfig serverConfig = config.Configs["LoginService"];
            if (serverConfig == null)
                throw new Exception(String.Format("No section LoginService in config file"));

            m_Proxy = serverConfig.GetBoolean("HasProxy", false);
        }

        private void InitializeHandlers(IHttpServer server)
        {
            server.AddXmlRPCHandler("login_to_simulator", HandleXMLRPCLogin);
            server.AddXmlRPCHandler("/", HandleXMLRPCLogin);
            server.AddXmlRPCHandler("set_login_level", HandleXMLRPCSetLoginLevel);
        }

        public XmlRpcResponse HandleXMLRPCLogin(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            Hashtable requestData = (Hashtable) request.Params[0];
            if (m_Proxy && request.Params[3] != null)
            {
                IPEndPoint ep = NetworkUtils.GetClientIPFromXFF((string) request.Params[3]);
                if (ep != null)
                    // Bang!
                    remoteClient = ep;
            }

            if (requestData != null)
            {
                if (((requestData.ContainsKey("first") && requestData["first"] != null &&
                      requestData.ContainsKey("last") && requestData["last"] != null) ||
                     requestData.ContainsKey("username") && requestData["username"] != null) &&
                    ((requestData.ContainsKey("passwd") && requestData["passwd"] != null) ||
                     (requestData.ContainsKey("web_login_key") && requestData["web_login_key"] != null)))
                {
                    string first = requestData.ContainsKey("first") ? requestData["first"].ToString() : "";
                    string last = requestData.ContainsKey("last") ? requestData["last"].ToString() : "";
                    string name = requestData.ContainsKey("username") ? requestData["username"].ToString() : "";
                    string passwd = "";
                    if (!requestData.ContainsKey("web_login_key"))
                        passwd = requestData["passwd"].ToString();
                    else
                        passwd = requestData["web_login_key"].ToString();

                    string startLocation = string.Empty;
                    if (requestData.ContainsKey("start"))
                        startLocation = requestData["start"].ToString();

                    string clientVersion = "Unknown";
                    if (requestData.Contains("version") && requestData["version"] != null)
                        clientVersion = requestData["version"].ToString();

                    //MAC BANNING START
                    string mac = (string) requestData["mac"];
                    if (mac == "")
                        return FailedXMLRPCResponse("Bad Viewer Connection.");

                    string channel = "Unknown";
                    if (requestData.Contains("channel") && requestData["channel"] != null)
                        channel = requestData["channel"].ToString();

                    if (channel == "")
                        return FailedXMLRPCResponse("Bad Viewer Connection.");

                    string id0 = "Unknown";
                    if (requestData.Contains("id0") && requestData["id0"] != null)
                        id0 = requestData["id0"].ToString();

                    LoginResponse reply = null;


                    string loginName = (name == "" || name == null) ? first + " " + last : name;
                    reply = m_loginService.Login(UUID.Zero, loginName, "UserAccount", passwd, startLocation,
                                                 clientVersion, channel,
                                                 mac, id0, remoteClient, requestData);
                    XmlRpcResponse response = new XmlRpcResponse {Value = reply.ToHashtable()};
                    return response;
                }
            }

            return FailedXMLRPCResponse();
        }

        public XmlRpcResponse HandleXMLRPCSetLoginLevel(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            Hashtable requestData = (Hashtable) request.Params[0];

            if (requestData != null)
            {
                if (requestData.ContainsKey("first") && requestData["first"] != null &&
                    requestData.ContainsKey("last") && requestData["last"] != null &&
                    requestData.ContainsKey("level") && requestData["level"] != null &&
                    requestData.ContainsKey("passwd") && requestData["passwd"] != null)
                {
                    string first = requestData["first"].ToString();
                    string last = requestData["last"].ToString();
                    string passwd = requestData["passwd"].ToString();
                    int level = Int32.Parse(requestData["level"].ToString());

                    MainConsole.Instance.InfoFormat("[LOGIN]: XMLRPC Set Level to {2} Requested by {0} {1}", first, last,
                                                    level);

                    Hashtable reply = m_loginService.SetLevel(first, last, passwd, level, remoteClient);

                    XmlRpcResponse response = new XmlRpcResponse {Value = reply};

                    return response;
                }
            }

            XmlRpcResponse failResponse = new XmlRpcResponse();
            Hashtable failHash = new Hashtable();
            failHash["success"] = "false";
            failResponse.Value = failHash;

            return failResponse;
        }

        private XmlRpcResponse FailedXMLRPCResponse()
        {
            Hashtable hash = new Hashtable();
            hash["reason"] = "key";
            hash["message"] = "Incomplete login credentials. Check your username and password.";
            hash["login"] = "false";

            XmlRpcResponse response = new XmlRpcResponse {Value = hash};

            return response;
        }

        private XmlRpcResponse FailedXMLRPCResponse(string message)
        {
            Hashtable hash = new Hashtable();
            hash["reason"] = "key";
            hash["message"] = message;
            hash["login"] = "false";

            XmlRpcResponse response = new XmlRpcResponse {Value = hash};

            return response;
        }
    }
}