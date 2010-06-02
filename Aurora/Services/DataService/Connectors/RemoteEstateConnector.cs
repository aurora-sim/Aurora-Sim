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
    public class RemoteEstateConnector : IEstateConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public RemoteEstateConnector(string serverURI)
        {
            m_ServerURI = serverURI;
        }

        public EstateSettings LoadEstateSettings(UUID regionID, bool create)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONID"] = regionID.ToString();
            sendData["CREATE"] = create;
            sendData["METHOD"] = "loadestatesettings";

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
                        EstateSettings ES = new EstateSettings(replyData);
                        return ES;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: LoadEstateSettings {0} received null response",
                            regionID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }

            return new EstateSettings();
        }

        public EstateSettings LoadEstateSettings(int estateID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["ESTATEID"] = estateID;
            sendData["METHOD"] = "loadestatesettings";

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
                            return new EstateSettings();

                        EstateSettings ES = new EstateSettings(replyData);
                        return ES;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: LoadEstateSettings {0} received null response",
                            estateID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }

            return new EstateSettings();
        }

        public bool StoreEstateSettings(EstateSettings es)
        {
            Dictionary<string, object> sendData = es.ToKeyValuePairs();

            sendData["METHOD"] = "storeestatesettings";

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
                        if (!replyData.ContainsKey("result") || (replyData["result"].ToString().ToLower() == "null"))
                            return false;

                        return true;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: LoadEstateSettings {0} received null response",
                            es.EstateID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }

            return false;
        }

        public void SaveEstateSettings(EstateSettings es)
        {
            Dictionary<string, object> sendData = es.ToKeyValuePairs();

            sendData["METHOD"] = "storeestatesettings";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public List<int> GetEstates(string search)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SEARCH"] = search;
            sendData["METHOD"] = "getestates";
            List<int> Estates = new List<int>();
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
                            return Estates;
                        foreach (object obj in replyData.Values)
                        {
                            if (obj is Dictionary<string, object>)
                            {
                                Dictionary<string, object> dictionary = obj as Dictionary<string, object>;
                                foreach (object value in dictionary)
                                {
                                    Estates.Add(int.Parse(value.ToString()));
                                }
                            }
                        }
                        return Estates;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: GetEstates {0} received null response",
                            search);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }

            return Estates;
        }

        public bool LinkRegion(UUID regionID, int estateID, string password)
        {
            Dictionary<string, object> sendData = new Dictionary<string,object>();
            sendData["REGIONID"] = regionID;
            sendData["ESTATEID"] = estateID;
            sendData["PASSWORD"] = password;
            sendData["METHOD"] = "linkregionestate";

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
                        if (!replyData.ContainsKey("result") || (replyData["result"].ToString().ToLower() == "null"))
                            return false;

                        return true;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: LinkRegion {0} received null response",
                            regionID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }

            return false;
        }

        public List<UUID> GetRegions(int estateID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["ESTATEID"] = estateID;
            sendData["METHOD"] = "getregioninestate";
            List<UUID> Regions = new List<UUID>();
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
                            return Regions;
                        foreach (object obj in replyData.Values)
                        {
                            if (obj is Dictionary<string, object>)
                            {
                                Dictionary<string, object> dictionary = obj as Dictionary<string, object>;
                                foreach (object value in dictionary)
                                {
                                    Regions.Add(UUID.Parse(value.ToString()));
                                }
                            }
                        }
                        return Regions;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: GetEstates {0} received null response",
                            estateID);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }

            return Regions;
        }

        public bool DeleteEstate(int estateID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["ESTATEID"] = estateID;
            sendData["METHOD"] = "deleteestate";

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
                        if (!replyData.ContainsKey("result") || (replyData["result"].ToString().ToLower() == "null"))
                            return false;

                        return true;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: DeleteEstate {0} received null response",
                            estateID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }

            return false;
        }
    }
}
