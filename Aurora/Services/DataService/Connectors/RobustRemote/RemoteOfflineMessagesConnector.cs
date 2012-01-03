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
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class RemoteOfflineMessagesConnector : IOfflineMessagesConnector
    {
        private IRegistryCore m_registry;

        #region IOfflineMessagesConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            m_registry = simBase;
            if (source.Configs["AuroraConnectors"].GetString("OfflineMessagesConnector", "LocalConnector") ==
                "RemoteConnector")
            {
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IOfflineMessagesConnector"; }
        }

        public GridInstantMessage[] GetOfflineMessages(UUID PrincipalID)
        {
            OSDMap map = new OSDMap();

            map["PrincipalID"] = PrincipalID;
            map["Method"] = "getofflinemessages";

            List<GridInstantMessage> Messages = new List<GridInstantMessage>();
            try
            {
                List<string> urls =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(PrincipalID.ToString(),
                                                                                           "RemoteServerURI");
                foreach (string url in urls)
                {
                    OSDMap result = WebUtils.PostToService(url + "osd", map, true, false);
                    OSDArray array = (OSDArray) OSDParser.DeserializeJson(result["_RawResult"]);
                    foreach (OSD o in array)
                    {
                        GridInstantMessage message = new GridInstantMessage();
                        message.FromOSD((OSDMap) o);
                        Messages.Add(message);
                    }
                }
                return Messages.ToArray();
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteOfflineMessagesConnector]: Exception when contacting server: {0}", e);
            }
            return Messages.ToArray();
        }

        public bool AddOfflineMessage(GridInstantMessage message)
        {
            OSDMap sendData = message.ToOSD();

            sendData["Method"] = "addofflinemessage";

            try
            {
                List<string> urls =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        message.toAgentID.ToString(), "RemoteServerURI");
#if (!ISWIN)
                foreach (string url in urls)
                {
                    OSDMap result = WebUtils.PostToService(url + "osd", sendData, true, false);
                    return ((OSDMap) OSDParser.DeserializeJson(result["_RawResult"]))["Result"].AsBoolean();
                }
#else
                foreach (OSDMap result in urls.Select(url => WebUtils.PostToService(url + "osd", sendData, true, false)))
                {
                    return ((OSDMap) OSDParser.DeserializeJson(result["_RawResult"]))["Result"].AsBoolean();
                }
#endif
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteOfflineMessagesConnector]: Exception when contacting server: {0}", e);
            }
            return false;
        }

        #endregion

        public void Dispose()
        {
        }
    }
}