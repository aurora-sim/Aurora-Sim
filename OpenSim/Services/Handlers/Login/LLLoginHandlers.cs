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
using System.Collections;
using System.Net;
using System.Reflection;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services
{
    public class LLLoginHandlers
    {
        private readonly ILoginService m_LocalService;
        private readonly bool m_Proxy;

        public LLLoginHandlers(ILoginService service, IConfigSource config, bool hasProxy)
        {
            m_LocalService = service;
            m_Proxy = hasProxy;
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
                    reply = m_LocalService.Login(UUID.Zero, loginName, "UserAccount", passwd, startLocation, clientVersion, channel,
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

                    MainConsole.Instance.InfoFormat("[LOGIN]: XMLRPC Set Level to {2} Requested by {0} {1}", first, last, level);

                    Hashtable reply = m_LocalService.SetLevel(first, last, passwd, level, remoteClient);

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

        public OSD HandleLLSDLogin(string path, OSD request, IPEndPoint remoteClient)
        {
            if (request.Type == OSDType.Map)
            {
                OSDMap map = (OSDMap) request;

                if (map.ContainsKey("first") && map.ContainsKey("last") && map.ContainsKey("passwd"))
                {
                    string startLocation = string.Empty;

                    if (map.ContainsKey("start"))
                        startLocation = map["start"].AsString();

                    MainConsole.Instance.Info("[LOGIN]: LLSD Login Requested for: '" + map["first"].AsString() + "' '" +
                               map["last"].AsString() + "' / " + startLocation);

                    LoginResponse reply = null;
                    string loginName = map["name"].AsString() == ""
                                           ? map["first"].AsString() + " " + map["last"].AsString()
                                           : map["name"].AsString();
                    reply = m_LocalService.Login(UUID.Zero, loginName, "UserAccount", map["passwd"].AsString(), startLocation,
                                                     "", "", "", "", remoteClient, new Hashtable());
                    return reply.ToOSDMap();
                }
            }

            return FailedOSDResponse();
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

        private OSD FailedOSDResponse()
        {
            OSDMap map = new OSDMap();

            map["reason"] = OSD.FromString("key");
            map["message"] = OSD.FromString("Invalid login credentials. Check your username and passwd.");
            map["login"] = OSD.FromString("false");

            return map;
        }
    }
}