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
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class RemoteRegionConnector : IRegionConnector
    {
        private IRegistryCore m_registry;

        #region IRegionConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("RegionConnector", "LocalConnector") == "RemoteConnector")
            {
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IRegionConnector"; }
        }

        public void AddTelehub(Telehub telehub, ulong RegionHandle)
        {
            Dictionary<string, object> sendData = telehub.ToKVP();
            sendData["METHOD"] = "addtelehub";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(RegionHandle.ToString(),
                                                                                           "GridServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteRegionConnector]: Exception when contacting server: {0}", e);
            }
        }

        public void RemoveTelehub(UUID regionID, ulong regionHandle)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["METHOD"] = "removetelehub";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(regionHandle.ToString(),
                                                                                           "GridServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteRegionConnector]: Exception when contacting server: {0}", e);
            }
        }

        public Telehub FindTelehub(UUID regionID, ulong regionHandle)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "findtelehub";
            sendData["REGIONID"] = regionID.ToString();

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(regionHandle.ToString(),
                                                                                           "GridServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                   m_ServerURI,
                                                                                                                                   reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("Result") ||
                            replyData.ContainsKey("Result") && replyData["Result"].ToString() != "Failure")
                        {
                            if (replyData.Count != 0)
                            {
                                Telehub t = new Telehub();
                                t.FromKVP(replyData);
                                if (t.RegionID != UUID.Zero)
                                    return t;
                            }
                        }
                    }
                    else
                    {
                        MainConsole.Instance.DebugFormat("[AuroraRemoteRegionConnector]: RemoveTelehub {0} received null response",
                                          regionID.ToString());
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteRegionConnector]: Exception when contacting server: {0}", e);
            }
            return null;
        }

        #endregion

        public void Dispose()
        {
        }
    }
}