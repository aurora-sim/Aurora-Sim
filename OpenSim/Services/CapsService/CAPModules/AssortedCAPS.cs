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

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using OpenSim.Services.Interfaces;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

using OpenMetaverse;
using Aurora.DataManager;
using OpenMetaverse.StructuredData;
using System.Text;

namespace OpenSim.Services.CapsService
{
    public class AssortedCAPS : ICapsServiceConnector
    {
        private IRegionClientCapsService m_service;
        private IAgentInfoService m_agentInfoService;
        private IAgentProcessing m_agentProcessing;

        #region ICapsServiceConnector Members

        public void RegisterCaps(IRegionClientCapsService service)
        {
            m_service = service;
            m_agentInfoService = service.Registry.RequestModuleInterface<IAgentInfoService>();
            m_agentProcessing = service.Registry.RequestModuleInterface<IAgentProcessing>();

            HttpServerHandle method = delegate(string path, Stream request,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return ProcessUpdateAgentLanguage(request, m_service.AgentID);
            };
            service.AddStreamHandler("UpdateAgentLanguage", new GenericStreamHandler("POST", service.CreateCAPS("UpdateAgentLanguage", ""),
                                                      method));


            method = delegate(string path, Stream request,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return ProcessUpdateAgentInfo(request, m_service.AgentID);
            };
            service.AddStreamHandler("UpdateAgentInformation", new GenericStreamHandler("POST", service.CreateCAPS("UpdateAgentInformation", ""),
                                method));

            service.AddStreamHandler ("AvatarPickerSearch", new GenericStreamHandler ("GET", service.CreateCAPS("AvatarPickerSearch", ""),
                                                      ProcessAvatarPickerSearch));

            method = delegate(string path, Stream request,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return HomeLocation(request, m_service.AgentID);
            };
            service.AddStreamHandler("HomeLocation", new GenericStreamHandler("POST", service.CreateCAPS("HomeLocation", ""),
                                                      method));

            method = delegate(string path, Stream request,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return TeleportLocation(request, m_service.AgentID);
            };

            service.AddStreamHandler("TeleportLocation", new GenericStreamHandler("POST", service.CreateCAPS("TeleportLocation", ""),
                                                      method));
        }

        public void EnteringRegion()
        {
        }

        public void DeregisterCaps()
        {
            m_service.RemoveStreamHandler ("UpdateAgentLanguage", "POST");
            m_service.RemoveStreamHandler ("UpdateAgentInformation", "POST");
            m_service.RemoveStreamHandler ("AvatarPickerSearch", "GET");
            m_service.RemoveStreamHandler ("HomeLocation", "POST");
            m_service.RemoveStreamHandler ("TeleportLocation", "POST");
        }

        #region Other CAPS

        private byte[] HomeLocation(Stream request, UUID agentID)
        {
            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml(request);
            OSDMap HomeLocation = rm["HomeLocation"] as OSDMap;
            if (HomeLocation != null)
            {
                OSDMap pos = HomeLocation["LocationPos"] as OSDMap;
                Vector3 position = new Vector3((float)pos["X"].AsReal(),
                                               (float)pos["Y"].AsReal(),
                                               (float)pos["Z"].AsReal());
                OSDMap lookat = HomeLocation["LocationLookAt"] as OSDMap;
                Vector3 lookAt = new Vector3((float)lookat["X"].AsReal(),
                                             (float)lookat["Y"].AsReal(),
                                             (float)lookat["Z"].AsReal());
                //int locationID = HomeLocation["LocationId"].AsInteger();

                m_agentInfoService.SetHomePosition(agentID.ToString(), m_service.Region.RegionID, position, lookAt);
            }

            rm.Add("success", OSD.FromBoolean(true));
            return OSDParser.SerializeLLSDXmlBytes(rm);
        }

        private byte[] ProcessUpdateAgentLanguage(Stream request, UUID agentID)
        {
            OSDMap rm = OSDParser.DeserializeLLSDXml(request) as OSDMap;
            if (rm == null)
                return MainServer.BadRequest;
            IAgentConnector AgentFrontend = DataManager.RequestPlugin<IAgentConnector>();
            if (AgentFrontend != null)
            {
                IAgentInfo IAI = AgentFrontend.GetAgent(agentID);
                if (IAI == null)
                    return MainServer.BadRequest;
                IAI.Language = rm["language"].AsString();
                IAI.LanguageIsPublic = int.Parse(rm["language_is_public"].AsString()) == 1;
                AgentFrontend.UpdateAgent(IAI);
            }
            return MainServer.BlankResponse;
        }

        private byte[] ProcessAvatarPickerSearch(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            NameValueCollection query = HttpUtility.ParseQueryString(httpRequest.Url.Query);
            string amt = query.GetOne("page-size");
            string name = query.GetOne("names");
            List<UserAccount> accounts = m_service.Registry.RequestModuleInterface<IUserAccountService>().GetUserAccounts(m_service.ClientCaps.AccountInfo.AllScopeIDs, name, 0, uint.Parse(amt)) ??
                                         new List<UserAccount>(0);

            OSDMap body = new OSDMap();
            OSDArray array = new OSDArray();
            foreach (UserAccount account in accounts)
            {
                OSDMap map = new OSDMap();
                map["agent_id"] = account.PrincipalID;
                IUserProfileInfo profileInfo = DataManager.RequestPlugin<IProfileConnector>().GetUserProfile(account.PrincipalID);
                map["display_name"] = (profileInfo == null || profileInfo.DisplayName == "") ? account.Name : profileInfo.DisplayName;
                map["username"] = account.Name;
                array.Add(map);
            }

            body["agents"] = array;
            return OSDParser.SerializeLLSDXmlBytes(body);
        }

        private byte[] ProcessUpdateAgentInfo(Stream request, UUID agentID)
        {
            OSD r = OSDParser.DeserializeLLSDXml(request);
            OSDMap rm = (OSDMap)r;
            OSDMap access = (OSDMap)rm["access_prefs"];
            string Level = access["max"].AsString();
            int maxLevel = 0;
            if (Level == "PG")
                maxLevel = 0;
            if (Level == "M")
                maxLevel = 1;
            if (Level == "A")
                maxLevel = 2;
            IAgentConnector data = DataManager.RequestPlugin<IAgentConnector>();
            if (data != null)
            {
                IAgentInfo agent = data.GetAgent(agentID);
                agent.MaturityRating = maxLevel;
                data.UpdateAgent(agent);
            }
            return MainServer.BlankResponse;
        }

        private bool _isInTeleportCurrently = false;
        private byte[] TeleportLocation (Stream request, UUID agentID)
        {
            OSDMap retVal = new OSDMap();
            if (_isInTeleportCurrently)
            {
                retVal.Add("reason", "Duplicate teleport request.");
                retVal.Add("success", OSD.FromBoolean(false));
                return OSDParser.SerializeLLSDXmlBytes(retVal);
            }
            _isInTeleportCurrently = true;

            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml(request);
            OSDMap pos = rm["LocationPos"] as OSDMap;
            Vector3 position = new Vector3((float)pos["X"].AsReal(),
                (float)pos["Y"].AsReal(),
                (float)pos["Z"].AsReal());
            /*OSDMap lookat = rm["LocationLookAt"] as OSDMap;
            Vector3 lookAt = new Vector3((float)lookat["X"].AsReal(),
                (float)lookat["Y"].AsReal(),
                (float)lookat["Z"].AsReal());*/
            ulong RegionHandle = rm["RegionHandle"].AsULong();
            const uint tpFlags = 16;

            if (m_service.ClientCaps.GetRootCapsService().RegionHandle != m_service.RegionHandle)
            {
                retVal.Add("reason", "Contacted by non-root region for teleport. Protocol implemention is wrong.");
                retVal.Add("success", OSD.FromBoolean(false));
                return OSDParser.SerializeLLSDXmlBytes(retVal);
            }

            string reason = "";
            int x, y;
            Util.UlongToInts(RegionHandle, out x, out y);
            GridRegion destination = m_service.Registry.RequestModuleInterface<IGridService>().GetRegionByPosition(
                m_service.ClientCaps.AccountInfo.AllScopeIDs, x, y);
            ISimulationService simService = m_service.Registry.RequestModuleInterface<ISimulationService>();
            AgentData ad = new AgentData();
            AgentCircuitData circuitData = null;
            if (destination != null)
            {
                simService.RetrieveAgent(m_service.Region, m_service.AgentID, true, out ad, out circuitData);
                if (ad != null)
                {
                    ad.Position = position;
                    ad.Center = position;
                    circuitData.startpos = position;
                }
            }
            if(destination == null || circuitData == null)
            {
                retVal.Add("reason", "Could not find the destination region.");
                retVal.Add("success", OSD.FromBoolean(false));
                return OSDParser.SerializeLLSDXmlBytes(retVal);
            }
            circuitData.reallyischild = false;
            circuitData.child = false;

            if (m_service.RegionHandle != destination.RegionHandle)
                simService.MakeChildAgent(m_service.AgentID, m_service.Region);

            if(m_agentProcessing.TeleportAgent(ref destination, tpFlags, ad == null ? 0 : (int)ad.Far, circuitData, ad,
                m_service.AgentID, m_service.RegionHandle, out reason) || reason == "")
            {
                retVal.Add("success", OSD.FromBoolean(true));
            }
            else
            {
                if (reason != "Already in a teleport")//If this error occurs... don't kick them out of their current region
                    simService.FailedToMoveAgentIntoNewRegion(m_service.AgentID, destination.RegionID);
                retVal.Add("reason", reason);
                retVal.Add("success", OSD.FromBoolean(false));
            }

            //Send back data
            _isInTeleportCurrently = false;
            return OSDParser.SerializeLLSDXmlBytes(retVal);
        }

        #endregion


        #endregion
    }
}
