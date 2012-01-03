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
    public class RemoteMuteListConnector : IMuteListConnector
    {
        private IRegistryCore m_registry;

        #region IMuteListConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            m_registry = simBase;
            if (source.Configs["AuroraConnectors"].GetString("MuteListConnector", "LocalConnector") == "RemoteConnector")
            {
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IMuteListConnector"; }
        }

        public MuteList[] GetMuteList(UUID PrincipalID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["METHOD"] = "getmutelist";

            string reqString = WebUtils.BuildQueryString(sendData);
            List<MuteList> Mutes = new List<MuteList>();
            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(PrincipalID.ToString(),
                                                                                           "RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                             m_ServerURI + "/auroradata",
                                                                             reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        foreach (object f in replyData)
                        {
                            KeyValuePair<string, object> value = (KeyValuePair<string, object>) f;
                            if (value.Value is Dictionary<string, object>)
                            {
                                Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                                MuteList mute = new MuteList();
                                mute.FromKVP(valuevalue);
                                Mutes.Add(mute);
                            }
                        }
                    }
                }
                return Mutes.ToArray();
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteMuteListConnector]: Exception when contacting server: {0}", e);
            }
            return Mutes.ToArray();
        }

        public void UpdateMute(MuteList mute, UUID PrincipalID)
        {
            Dictionary<string, object> sendData = mute.ToKVP();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["METHOD"] = "updatemute";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(PrincipalID.ToString(),
                                                                                           "RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    AsynchronousRestObjectRequester.MakeRequest("POST",
                                                                m_ServerURI + "/auroradata",
                                                                reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteMuteListConnector]: Exception when contacting server: {0}", e);
            }
        }

        public void DeleteMute(UUID muteID, UUID PrincipalID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["MUTEID"] = muteID.ToString();
            sendData["METHOD"] = "deletemute";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(PrincipalID.ToString(),
                                                                                           "RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    AsynchronousRestObjectRequester.MakeRequest("POST",
                                                                m_ServerURI + "/auroradata",
                                                                reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteMuteListConnector]: Exception when contacting server: {0}", e);
            }
        }

        public bool IsMuted(UUID PrincipalID, UUID PossibleMuteID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["MUTEID"] = PossibleMuteID.ToString();
            sendData["METHOD"] = "ismuted";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(PrincipalID.ToString(),
                                                                                           "RemoteServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                   m_ServerURI + "/auroradata",
                                                                                                                                   reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
                    return bool.Parse(replyData["Muted"].ToString());
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteMuteListConnector]: Exception when contacting server: {0}", e);
            }
            return false;
        }

        #endregion

        public void Dispose()
        {
        }
    }
}