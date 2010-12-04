using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;
using OpenSim.Framework.Capabilities;
using OSD = OpenMetaverse.StructuredData.OSD;
using OSDArray = OpenMetaverse.StructuredData.OSDArray;
using OSDMap = OpenMetaverse.StructuredData.OSDMap;
using OpenSim.Services.Base;

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
        private IPrivateCapsService m_handler;
        #region ICapsServiceConnector Members

        public List<IRequestHandler> RegisterCaps(UUID agentID, IHttpServer server, IPrivateCapsService handler)
        {
            m_handler = handler;
            List<IRequestHandler> handlers = new List<IRequestHandler>();

            GenericHTTPMethod method = delegate(Hashtable httpMethod)
            {
                return ProcessUpdateAgentLanguage(httpMethod, agentID);
            };
            handlers.Add(new RestHTTPHandler("POST", handler.CreateCAPS("UpdateAgentLanguage"),
                                                      method));
            method = delegate(Hashtable httpMethod)
            {
                return ProcessUpdateAgentInfo(httpMethod, agentID);
            };
            handlers.Add(new RestHTTPHandler("POST", handler.CreateCAPS("UpdateAgentInformation"),
                                                      method));

            method = delegate(Hashtable httpMethod)
            {
                return HomeLocation(httpMethod, agentID);
            };
            handlers.Add(new RestHTTPHandler("POST", handler.CreateCAPS("HomeLocation"),
                                                      method));
            return handlers;
        }

        #region Other CAPS

        private Hashtable HomeLocation(Hashtable mDhttpMethod, UUID agentID)
        {
            OpenMetaverse.StructuredData.OSDMap rm = (OpenMetaverse.StructuredData.OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
            OpenMetaverse.StructuredData.OSDMap HomeLocation = rm["HomeLocation"] as OpenMetaverse.StructuredData.OSDMap;
            OpenMetaverse.StructuredData.OSDMap pos = HomeLocation["LocationPos"] as OpenMetaverse.StructuredData.OSDMap;
            Vector3 position = new Vector3((float)pos["X"].AsReal(),
                (float)pos["Y"].AsReal(),
                (float)pos["Z"].AsReal());
            OpenMetaverse.StructuredData.OSDMap lookat = HomeLocation["LocationLookAt"] as OpenMetaverse.StructuredData.OSDMap;
            Vector3 lookAt = new Vector3((float)lookat["X"].AsReal(),
                (float)lookat["Y"].AsReal(),
                (float)lookat["Z"].AsReal());
            int locationID = HomeLocation["LocationId"].AsInteger();

            PresenceInfo presence = m_handler.PresenceService.GetAgents(new string[] { agentID.ToString() })[0];
            bool r = m_handler.GridUserService.SetHome(agentID.ToString(), presence.RegionID, position, lookAt);

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

            OpenMetaverse.StructuredData.OSD r = OpenMetaverse.StructuredData.OSDParser.DeserializeLLSDXml((string)m_dhttpMethod["requestbody"]);
            OpenMetaverse.StructuredData.OSDMap rm = (OpenMetaverse.StructuredData.OSDMap)r;
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
            OpenMetaverse.StructuredData.OSD r = OpenMetaverse.StructuredData.OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
            OpenMetaverse.StructuredData.OSDMap rm = (OpenMetaverse.StructuredData.OSDMap)r;
            OpenMetaverse.StructuredData.OSDMap access = (OpenMetaverse.StructuredData.OSDMap)rm["access_prefs"];
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
