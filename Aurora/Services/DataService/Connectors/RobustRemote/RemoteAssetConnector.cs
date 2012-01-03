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
using System.Reflection;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using Aurora.Simulation.Base;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class RemoteAssetConnector : IAssetConnector
    {
        private IRegistryCore m_registry;

        #region IAssetConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AssetConnector", "LocalConnector") == "RemoteConnector")
            {
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IAssetConnector"; }
        }

        public void UpdateLSLData(string token, string key, string value)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["token"] = token;
            sendData["key"] = key;
            sendData["value"] = value;
            sendData["METHOD"] = "updatelsldata";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    AsynchronousRestObjectRequester.MakeRequest("POST",
                                                                m_ServerURI,
                                                                reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteAssetConnector]: Exception when contacting server: {0}", e);
            }
        }

        public List<string> FindLSLData(string token, string key)
        {
            List<string> data = new List<string>();
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["token"] = token;
            sendData["key"] = key;
            sendData["METHOD"] = "findlsldata";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
                           reqString);

                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            foreach (object obj in replyData.Values)
                            {
                                data.Add (obj.ToString ());
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteAssetConnector]: Exception when contacting server: {0}", e.ToString());
            }
            return data;
        }

        #endregion

        public void Dispose()
        {
        }
    }
}