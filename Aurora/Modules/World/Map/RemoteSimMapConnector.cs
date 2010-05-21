using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using log4net;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;

namespace Aurora.Modules
{
    public class RemoteSimMapConnector : ISimMapConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public RemoteSimMapConnector(string serverURI)
        {
            m_ServerURI = serverURI;
        }

        public List<SimMap> GetSimMap(UUID regionID, UUID AgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONID"] = regionID.ToString();
            sendData["AGENTID"] = AgentID.ToString();
            sendData["METHOD"] = "getsimmap";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/SIMMAP",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        List<SimMap> Sims = new List<SimMap>();
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                SimMap map = new SimMap((Dictionary<string, object>)f);
                                Sims.Add(map);
                            }
                            else
                                m_log.DebugFormat("[RemoteSimMapConnector]: GetSimMap {0} received invalid response type {1}",
                                    regionID, f.GetType());
                        }
                        // Success
                        return Sims;
                    }

                    else
                        m_log.DebugFormat("[RemoteSimMapConnector]: GetSimMap {0} received null response",
                            regionID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[RemoteSimMapConnector]: Exception when contacting server: {0}", e.Message);
            }

            return new List<SimMap>();
        }

        public List<SimMap> GetSimMap(string mapName, UUID AgentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONNAME"] = mapName.ToString();
            sendData["AGENTID"] = AgentID.ToString();
            sendData["METHOD"] = "getsimmap";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/SIMMAP",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        List<SimMap> Sims = new List<SimMap>();
                        foreach (object f in replyData)
                        {
                            if (f is KeyValuePair<string,object>)
                            {
                                Dictionary<string, object> value = ((KeyValuePair<string, object>)f).Value as Dictionary<string, object>;
                                SimMap map = new SimMap(value);
                                Sims.Add(map);
                            }
                            else
                                m_log.DebugFormat("[RemoteSimMapConnector]: GetSimMap {0} received invalid response type {1}",
                                    mapName, f.GetType());
                        }
                        // Success
                        return Sims;
                    }

                    else
                        m_log.DebugFormat("[RemoteSimMapConnector]: GetSimMap {0} received null response",
                            mapName);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[RemoteSimMapConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public List<SimMap> GetSimMapRange(uint XMin, uint YMin, uint XMax, uint YMax, UUID agentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONLOCXMIN"] = XMin;
            sendData["REGIONLOCYMIN"] = YMin;
            sendData["REGIONLOCXMAX"] = XMax;
            sendData["REGIONLOCYMAX"] = YMax;
            sendData["AGENTID"] = agentID;
            sendData["METHOD"] = "getsimmaprange";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/SIMMAP",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        List<SimMap> Sims = new List<SimMap>();
                        foreach (object f in replyData)
                        {
                            if (f is KeyValuePair<string, object>)
                            {
                                Dictionary<string, object> value = ((KeyValuePair<string, object>)f).Value as Dictionary<string, object>;
                                SimMap map = new SimMap(value);
                                Sims.Add(map);
                            }
                            else
                                m_log.DebugFormat("[RemoteSimMapConnector]: GetSimMapRange {0} received invalid response type {1}",
                                    agentID, f.GetType());
                        }
                        // Success
                        return Sims;
                    }

                    else
                        m_log.DebugFormat("[RemoteSimMapConnector]: GetSimMapRange {0} received null response",
                            agentID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[RemoteSimMapConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public void UpdateSimMapOnlineStatus(UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONID"] = regionID;
            sendData["METHOD"] = "updatesimmap";

            string reqString = ServerUtils.BuildQueryString(sendData);
            string reply = "";
            try
            {
                reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/SIMMAP",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[RemoteSimMapConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void UpdateSimMap(SimMap sim)
        {
            Dictionary<string, object> sendData = sim.ToKeyValuePairs();

            sendData["METHOD"] = "fullupdatesimmap";

            string reqString = ServerUtils.BuildQueryString(sendData);
            string reply = "";
            try
            {
                reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/SIMMAP",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[RemoteSimMapConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void AddAgent(UUID regionID, UUID agentID, Vector3 Position)
        {
            Dictionary<string, object> sendData = new Dictionary<string,object>();

            sendData["REGIONID"] = regionID;
            sendData["AGENTID"] = agentID;
            sendData["X"] = Position.X;
            sendData["Y"] = Position.Y;
            sendData["Z"] = Position.Z;
            sendData["METHOD"] = "addagent";

            string reqString = ServerUtils.BuildQueryString(sendData);
            string reply = "";
            try
            {
                reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/SIMMAP",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[RemoteSimMapConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void RemoveAgent(UUID regionID, UUID agentID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONID"] = regionID;
            sendData["AGENTID"] = agentID;
            sendData["METHOD"] = "removeagent";

            string reqString = ServerUtils.BuildQueryString(sendData);
            string reply = "";
            try
            {
                reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/SIMMAP",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[RemoteSimMapConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public List<mapItemReply> GetMapItems(ulong regionHandle, GridItemType gridItemType)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONHANDLE"] = regionHandle;
            sendData["GRIDITEMTYPE"] = (int)gridItemType;
            sendData["METHOD"] = "getmapitems";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/SIMMAP",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        List<mapItemReply> items = new List<mapItemReply>();
                        foreach (object f in replyData)
                        {
                            if (f is KeyValuePair<string, object>)
                            {
                                Dictionary<string, object> value = ((KeyValuePair<string, object>)f).Value as Dictionary<string, object>;
                                mapItemReply map = new mapItemReply(value);
                                items.Add(map);
                            }
                            else
                                m_log.DebugFormat("[RemoteSimMapConnector]: GetMapItems {0} received invalid response type {1}",
                                    regionHandle, f.GetType());
                        }
                        // Success
                        return items;
                    }

                    else
                        m_log.DebugFormat("[RemoteSimMapConnector]: GetMapItems {0} received null response",
                            regionHandle);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[RemoteSimMapConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }
    }
}
