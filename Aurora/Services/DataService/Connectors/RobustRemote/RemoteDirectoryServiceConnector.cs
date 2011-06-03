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

        private IRegistryCore m_registry;

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("DirectoryServiceConnector", "LocalConnector") == "RemoteConnector")
            {
                m_registry = simBase;
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
            OSDMap mess = args.ToOSD ();
            mess["Method"] = "addlandobject";
            List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
            foreach (string m_ServerURI in m_ServerURIs)
            {
                WebUtils.PostToService (m_ServerURI + "osd", mess, false, false);
            }
        }

        public void RemoveLandObject (UUID regionID, LandData args)
        {
            OSDMap mess = args.ToOSD ();
            mess["Method"] = "removelandobject";
            List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf ("RemoteServerURI");
            foreach (string m_ServerURI in m_ServerURIs)
            {
                WebUtils.PostToService (m_ServerURI + "osd", mess, false, false);
            }
        }

        public LandData GetParcelInfo(UUID InfoUUID)
        {
            OSDMap mess = new OSDMap ();
            mess["Method"] = "getparcelinfo";
            mess["InfoUUID"] = InfoUUID;
            List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf ("RemoteServerURI");
            foreach (string m_ServerURI in m_ServerURIs)
            {
                OSDMap results = WebUtils.PostToService (m_ServerURI + "osd", mess, true, false);
                OSDMap innerResults = (OSDMap)OSDParser.DeserializeJson (results["_RawResult"]);
                if (innerResults["Success"])
                {
                    LandData result = new LandData ();
                    result.FromOSD (innerResults);
                    return result;
                }
            }
            return null;
        }

        public LandData[] GetParcelByOwner(UUID OwnerID)
        {
            List<LandData> Land = new List<LandData> ();
            OSDMap mess = new OSDMap ();
            mess["Method"] = "getparcelbyowner";
            mess["OwnerID"] = OwnerID;
            List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf ("RemoteServerURI");
            foreach (string m_ServerURI in m_ServerURIs)
            {
                OSDMap results = WebUtils.PostToService (m_ServerURI + "osd", mess, true, false);
                OSDMap innerResults = (OSDMap)OSDParser.DeserializeJson (results["_RawResult"]);

                OSDArray parcels = (OSDArray)innerResults["Parcels"];
                foreach(OSD o in parcels)
                {
                    LandData result = new LandData ();
                    result.FromOSD ((OSDMap)o);
                    Land.Add (result);
                }
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
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
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
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.ToString());
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
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
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
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.ToString());
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
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
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
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.ToString());
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
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
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
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.ToString());
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
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
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
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.ToString());
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
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
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
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.ToString());
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
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
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
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.ToString());
            }
            return Classifieds.ToArray();
        }
    }
}
