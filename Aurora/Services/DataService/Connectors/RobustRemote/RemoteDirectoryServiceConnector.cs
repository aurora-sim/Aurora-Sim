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
using Aurora.Simulation.Base;

namespace Aurora.Services.DataService
{
    public class RemoteDirectoryServiceConnector : IDirectoryServiceConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private List<string> m_ServerURIs = new List<string>();

        public void Initialize(IGenericData GenericData, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("DirectoryServiceConnector", "LocalConnector") == "RemoteConnector")
            {
                m_ServerURIs = simBase.ApplicationRegistry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                if (m_ServerURIs.Count != 0)
                    DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IDirectoryServiceConnector"; }
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// This also updates the parcel, not for just adding a new one
        /// </summary>
        /// <param name="args"></param>
        /// <param name="regionID"></param>
        /// <param name="forSale"></param>
        /// <param name="EstateID"></param>
        /// <param name="showInSearch"></param>
        public void AddLandObject(LandData args)
        {
            Dictionary<string, object> sendData = args.ToKeyValuePairs();

            sendData["METHOD"] = "addlandobject";

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    AsynchronousRestObjectRequester.MakeRequest("POST",
                           m_ServerURI + "/auroradata",
                           reqString);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public LandData GetParcelInfo(UUID InfoUUID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["INFOUUID"] = InfoUUID.ToString();
            sendData["METHOD"] = "getparcelinfo";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI + "/auroradata",
                           reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                            LandData land = null;
                            foreach (object f in replyvalues)
                            {
                                if (f is Dictionary<string, object>)
                                {
                                    land = new LandData();
                                    land.FromKVP((Dictionary<string, object>)f);
                                    break;
                                }
                                else
                                    m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: GetParcelInfo {0} received invalid response type {1}",
                                        InfoUUID, f.GetType());
                            }
                            // Success
                            return land;
                        }

                        else
                            m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: GetParcelInfo {0} received null response",
                                InfoUUID);
                    }
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public LandData[] GetParcelByOwner(UUID OwnerID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["OWNERID"] = OwnerID;
            sendData["METHOD"] = "getparcelbyowner";

            string reqString = WebUtils.BuildQueryString(sendData);
            List<LandData> Land = new List<LandData>();
            try
            {
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
                            KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                            if (value.Value is Dictionary<string, object>)
                            {
                                Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                                LandData land = new LandData();
                                land.FromKVP(valuevalue);
                                Land.Add(land);
                            }
                        }
                    }
                }
                return Land.ToArray();
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
            return Land.ToArray();
        }

        public DirPlacesReplyData[] FindLand(string queryText, string category, int StartQuery, uint Flags)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["QUERYTEXT"] = queryText;
            sendData["CATEGORY"] = category;
            sendData["STARTQUERY"] = StartQuery;
            sendData["FLAGS"] = Flags;
            sendData["METHOD"] = "findland";

            string reqString = WebUtils.BuildQueryString(sendData);
            List<DirPlacesReplyData> Land = new List<DirPlacesReplyData>();
            try
            {
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
                            KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                            if (value.Value is Dictionary<string, object>)
                            {
                                Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                                DirPlacesReplyData land = new DirPlacesReplyData(valuevalue);
                                Land.Add(land);
                            }
                        }
                    }
                }
                return Land.ToArray();
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
            return Land.ToArray();
        }

        public DirLandReplyData[] FindLandForSale(string searchType, string price, string area, int StartQuery, uint Flags)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SEARCHTYPE"] = searchType;
            sendData["PRICE"] = price;
            sendData["AREA"] = area;
            sendData["STARTQUERY"] = StartQuery;
            sendData["FLAGS"] = Flags;
            sendData["METHOD"] = "findlandforsale";

            string reqString = WebUtils.BuildQueryString(sendData);
            List<DirLandReplyData> Land = new List<DirLandReplyData>();
            try
            {
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
                            KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                            if (value.Value is Dictionary<string, object>)
                            {
                                Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                                DirLandReplyData land = new DirLandReplyData(valuevalue);
                                Land.Add(land);
                            }
                        }
                    }
                }
                return Land.ToArray();
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
            return Land.ToArray();
        }

        public DirEventsReplyData[] FindEvents(string queryText, string flags, int StartQuery)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["QUERYTEXT"] = queryText;
            sendData["FLAGS"] = flags;
            sendData["STARTQUERY"] = StartQuery;
            sendData["METHOD"] = "findevents";

            string reqString = WebUtils.BuildQueryString(sendData);
            List<DirEventsReplyData> Events = new List<DirEventsReplyData>();
            try
            {
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
                            KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                            if (value.Value is Dictionary<string, object>)
                            {
                                Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                                DirEventsReplyData direvent = new DirEventsReplyData(valuevalue);
                                Events.Add(direvent);
                            }
                        }
                    }
                }
                return Events.ToArray();
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
            return Events.ToArray();
        }

        public DirEventsReplyData[] FindAllEventsInRegion(string regionName, int maturity)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONNAME"] = regionName;
            sendData["MATURITY"] = maturity;
            sendData["METHOD"] = "findeventsinregion";

            string reqString = WebUtils.BuildQueryString(sendData);
            List<DirEventsReplyData> Events = new List<DirEventsReplyData>();
            try
            {
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
                            KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                            if (value.Value is Dictionary<string, object>)
                            {
                                Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                                DirEventsReplyData direvent = new DirEventsReplyData(valuevalue);
                                Events.Add(direvent);
                            }
                        }
                    }
                }
                return Events.ToArray();
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
            return Events.ToArray();
        }

        public DirClassifiedReplyData[] FindClassifieds(string queryText, string category, string queryFlags, int StartQuery)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["QUERYTEXT"] = queryText;
            sendData["CATEGORY"] = category;
            sendData["QUERYFLAGS"] = queryFlags;
            sendData["STARTQUERY"] = StartQuery;
            sendData["METHOD"] = "findclassifieds";

            string reqString = WebUtils.BuildQueryString(sendData);
            List<DirClassifiedReplyData> Classifieds = new List<DirClassifiedReplyData>();
            try
            {
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
                            KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                            if (value.Value is Dictionary<string, object>)
                            {
                                Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                                DirClassifiedReplyData classified = new DirClassifiedReplyData(valuevalue);
                                Classifieds.Add(classified);
                            }
                        }
                    }
                }
                return Classifieds.ToArray();
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
            return Classifieds.ToArray();
        }

        public EventData GetEventInfo(string EventID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["EVENTID"] = EventID;
            sendData["METHOD"] = "geteventinfo";

            string reqString = WebUtils.BuildQueryString(sendData);
            try
            {
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
                            KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                            if (value.Value is Dictionary<string, object>)
                            {
                                Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                                EventData eventdata = new EventData(valuevalue);
                                return eventdata;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
            return null;
        }

        public Classified[] GetClassifiedsInRegion(string regionName)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONNAME"] = regionName;
            sendData["METHOD"] = "findclassifiedsinregion";

            string reqString = WebUtils.BuildQueryString(sendData);
            List<Classified> Classifieds = new List<Classified>();
            try
            {
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
                            KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                            if (value.Value is Dictionary<string, object>)
                            {
                                Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                                Classified classified = new Classified();
                                classified.FromKVP(valuevalue);
                                Classifieds.Add(classified);
                            }
                        }
                    }
                }
                return Classifieds.ToArray();
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
            return Classifieds.ToArray();
        }

        public void AddClassifieds(OSDMap map)
        {
            //Not able to be done remotely
        }

        public void RemoveClassifieds(OSDMap map)
        {
            //Not able to be done remotely
        }
    }
}
