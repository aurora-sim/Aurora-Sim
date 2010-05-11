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

namespace Aurora.Services.DataService
{
    public class RemoteGridConnector : IGridConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
        private IGridConnector GridConnector;

        private string m_ServerURI = "";

        public RemoteGridConnector(string serverURI)
        {
            m_ServerURI = serverURI;
        }

        #region IGridConnector Members

        public GridRegionFlags GetRegionFlags(UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONID"] = regionID.ToString();
            sendData["METHOD"] = "getregionflags";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("result"))
                            return (GridRegionFlags)(-1);


                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        GridRegionFlags flags = (GridRegionFlags)(-1);
                        foreach (object f in replyvalues)
                        {
                            if (f is GridRegionFlags)
                            {
                                flags = (GridRegionFlags)f;
                            }
                            else
                                m_log.DebugFormat("[AuroraRemoteProfileConnector]: GetRegionFlags {0} received invalid response type {1}",
                                    regionID, f.GetType());
                        }
                        // Success
                        return flags;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: GetRegionFlags {0} received null response",
                            regionID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }

            return (GridRegionFlags)(-1);
        }

        public void SetRegionFlags(UUID regionID, GridRegionFlags flags)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONID"] = regionID.ToString();
            sendData["FLAGS"] = flags.ToString();
            sendData["METHOD"] = "setregionflags";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        if (replyData.ContainsKey("result") && (replyData["result"].ToString().ToLower() == "null"))
                        {
                            m_log.DebugFormat("[AuroraRemoteProfileConnector]: SetRegionFlags {0} received null response",
                                regionID);
                        }
                    }

                    else
                    {
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: SetRegionFlags {0} received null response",
                            regionID);
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void CreateRegion(UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONID"] = regionID.ToString();
            sendData["METHOD"] = "createregion";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        if (replyData.ContainsKey("result") && (replyData["result"].ToString().ToLower() == "null"))
                        {
                            m_log.DebugFormat("[AuroraRemoteProfileConnector]: CreateNewAgent {0} received null response",
                                regionID);
                        }
                    }

                    else
                    {
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: CreateNewAgent {0} received null response",
                            regionID);
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void AddTelehub(UUID regionID, Vector3 position, int regionPosX, int regionPosY)
        {
            throw new NotImplementedException();
        }

        public void RemoveTelehub(UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = regionID.ToString();
            sendData["METHOD"] = "removetelehub";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        if (replyData.ContainsKey("result") && (replyData["result"].ToString().ToLower() == "null"))
                        {
                            m_log.DebugFormat("[AuroraRemoteProfileConnector]: RemoveTelehub {0} received null response",
                                regionID);
                        }
                    }

                    else
                    {
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: RemoveTelehub {0} received null response",
                            regionID);
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public bool FindTelehub(UUID regionID, out Vector3 position)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
