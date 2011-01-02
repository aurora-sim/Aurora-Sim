using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Capabilities;

using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.CapsService
{
    public class AssortedCAPS : ICapsServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IRegionClientCapsService m_service;
        private IPresenceService m_presenceService;
        private IGridUserService m_gridUserService;

        #region ICapsServiceConnector Members

        public void RegisterCaps(IRegionClientCapsService service)
        {
            m_service = service;
            m_presenceService = service.Registry.RequestModuleInterface<IPresenceService>();
            m_gridUserService = service.Registry.RequestModuleInterface<IGridUserService>();
            
            GenericHTTPMethod method = delegate(Hashtable httpMethod)
            {
                return ProcessUpdateAgentLanguage(httpMethod, m_service.AgentID);
            };
            service.AddStreamHandler("UpdateAgentLanguage", new RestHTTPHandler("POST", service.CreateCAPS("UpdateAgentLanguage", ""),
                                                      method));
            method = delegate(Hashtable httpMethod)
            {
                return ProcessUpdateAgentInfo(httpMethod, m_service.AgentID);
            };
            service.AddStreamHandler("UpdateAgentInformation", new RestHTTPHandler("POST", service.CreateCAPS("UpdateAgentInformation", ""),
                                                      method));

            method = delegate(Hashtable httpMethod)
            {
                return HomeLocation(httpMethod, m_service.AgentID);
            };
            service.AddStreamHandler("HomeLocation", new RestHTTPHandler("POST", service.CreateCAPS("HomeLocation", ""),
                                                      method));
        }

        #region Other CAPS

        private Hashtable HomeLocation(Hashtable mDhttpMethod, UUID agentID)
        {
            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
            OSDMap HomeLocation = rm["HomeLocation"] as OSDMap;
            OSDMap pos = HomeLocation["LocationPos"] as OSDMap;
            Vector3 position = new Vector3((float)pos["X"].AsReal(),
                (float)pos["Y"].AsReal(),
                (float)pos["Z"].AsReal());
            OSDMap lookat = HomeLocation["LocationLookAt"] as OSDMap;
            Vector3 lookAt = new Vector3((float)lookat["X"].AsReal(),
                (float)lookat["Y"].AsReal(),
                (float)lookat["Z"].AsReal());
            int locationID = HomeLocation["LocationId"].AsInteger();

            PresenceInfo presence = m_presenceService.GetAgents(new string[] { agentID.ToString() })[0];
            bool r = m_gridUserService.SetHome(agentID.ToString(), presence.RegionID, position, lookAt);

            rm.Add("success", OSD.FromBoolean(r));

            //Send back data
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(rm);
            return responsedata;
        }

        private Hashtable ProcessUpdateAgentLanguage(Hashtable m_dhttpMethod, UUID agentID)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";

            OSD r = OSDParser.DeserializeLLSDXml((string)m_dhttpMethod["requestbody"]);
            OSDMap rm = (OSDMap)r;
            IAgentConnector AgentFrontend = DataManager.RequestPlugin<IAgentConnector>();
            if (AgentFrontend != null)
            {
                IAgentInfo IAI = AgentFrontend.GetAgent(agentID);
                if (IAI == null)
                    return responsedata;
                IAI.Language = rm["language"].AsString();
                IAI.LanguageIsPublic = int.Parse(rm["language_is_public"].AsString()) == 1;
                AgentFrontend.UpdateAgent(IAI);
            }
            return responsedata;
        }

        private Hashtable ProcessUpdateAgentInfo(Hashtable mDhttpMethod, UUID agentID)
        {
            OSD r = OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
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
            Hashtable cancelresponsedata = new Hashtable();
            cancelresponsedata["int_response_code"] = 200; //501; //410; //404;
            cancelresponsedata["content_type"] = "text/plain";
            cancelresponsedata["keepalive"] = false;
            cancelresponsedata["str_response_string"] = "";
            return cancelresponsedata;
        }

        #endregion


        #endregion
    }
}
