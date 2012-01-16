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

using Nini.Config;

using OpenMetaverse;
using OpenMetaverse.StructuredData;
using EventFlags = OpenMetaverse.DirectoryManager.EventFlags;

using OpenSim.Services.Interfaces;

using Aurora.Framework;
using Aurora.Simulation.Base;
using Aurora.Framework.Servers.HttpServer;

namespace Aurora.Services.DataService
{
    public class RemoteDirectoryServiceConnector : IDirectoryServiceConnector
    {
        private IRegistryCore m_registry;

        #region IDirectoryServiceConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("DirectoryServiceConnector", "LocalConnector") ==
                "RemoteConnector")
            {
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public void Dispose()
        {
        }

        public string Name
        {
            get { return "IDirectoryServiceConnector"; }
        }

        #region Regions

        /// <summary>
        ///   This adds the entire region into the search database
        /// </summary>
        /// <param name = "args"></param>
        public void AddRegion(List<LandData> parcels)
        {
            OSDMap mess = new OSDMap();
            OSDArray requests = new OSDArray();
            foreach (LandData data in parcels)
                requests.Add(data.ToOSD());
            mess["Requests"] = requests;
            mess["Method"] = "addregion";
            List<string> m_ServerURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
            foreach (string m_ServerURI in m_ServerURIs)
            {
                WebUtils.PostToService(m_ServerURI + "osd", mess, false, false);
            }
        }

        public void ClearRegion(UUID regionID)
        {
            OSDMap mess = new OSDMap();
            mess["Method"] = "clearregion";
            mess["RegionID"] = regionID;
            List<string> m_ServerURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
            foreach (string m_ServerURI in m_ServerURIs)
            {
                WebUtils.PostToService(m_ServerURI + "osd", mess, false, false);
            }
        }

        #endregion

        #region Parcels

        public LandData GetParcelInfo(UUID InfoUUID)
        {
            OSDMap mess = new OSDMap();
            mess["Method"] = "getparcelinfo";
            mess["InfoUUID"] = InfoUUID;
            List<string> m_ServerURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
            foreach (string m_ServerURI in m_ServerURIs)
            {
                OSDMap results = WebUtils.PostToService(m_ServerURI + "osd", mess, true, false);
                OSDMap innerResults = (OSDMap)OSDParser.DeserializeJson(results["_RawResult"]);
                if (innerResults["Success"])
                {
                    LandData result = new LandData(innerResults);
                    return result;
                }
            }
            return null;
        }

        public LandData GetParcelInfo(UUID RegionID, UUID ScopeID, string ParcelName)
        {
            OSDMap mess = new OSDMap();
            mess["Method"] = "GetParcelInfo";
            mess["RegionID"] = RegionID;
            mess["ScopeID"] = ScopeID;
            mess["ParcelName"] = ParcelName;
            List<string> m_ServerURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
            foreach (string m_ServerURI in m_ServerURIs)
            {
                OSDMap results = WebUtils.PostToService(m_ServerURI + "osd", mess, true, false);
                OSDMap innerResults = (OSDMap)OSDParser.DeserializeJson(results["_RawResult"]);
                if (innerResults["Success"])
                {
                    LandData result = new LandData(innerResults);
                    return result;
                }
            }
            return null;
        }

        public LandData[] GetParcelByOwner(UUID OwnerID)
        {
            List<LandData> Land = new List<LandData>();
            OSDMap mess = new OSDMap();
            mess["Method"] = "getparcelbyowner";
            mess["OwnerID"] = OwnerID;
            List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
            foreach (string m_ServerURI in m_ServerURIs)
            {
                OSDMap results = WebUtils.PostToService(m_ServerURI + "osd", mess, true, false);
                OSDMap innerResults = (OSDMap) OSDParser.DeserializeJson(results["_RawResult"]);

                OSDArray parcels = (OSDArray) innerResults["Parcels"];
                foreach (OSD o in parcels)
                {
                    Land.Add(new LandData((OSDMap)o));
                }
            }
            return Land.ToArray();
        }

        public List<LandData> GetParcelsByRegion(uint start, uint count, UUID RegionID, UUID scopeID, UUID owner, ParcelFlags flags, ParcelCategory category)
        {
            List<LandData> resp = new List<LandData>(0);
            if (count == 0)
            {
                return resp;
            }
            OSDMap mess = new OSDMap();
            mess["Method"] = "GetParcelsByRegion";
            mess["start"] = OSD.FromUInteger(start);
            mess["count"] = OSD.FromUInteger(count);
            mess["RegionID"] = OSD.FromUUID(RegionID);
            mess["scopeID"] = OSD.FromUUID(scopeID);
            mess["owner"] = OSD.FromUUID(owner);
            mess["flags"] = OSD.FromUInteger((uint)flags);
            mess["category"] = OSD.FromInteger((int)category);
            List<string> m_ServerURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
            resp = new List<LandData>();
            foreach (string m_ServerURI in m_ServerURIs)
            {
                OSDMap results = WebUtils.PostToService(m_ServerURI + "osd", mess, true, false);
                OSDMap innerResults = (OSDMap)OSDParser.DeserializeJson(results["_RawResult"]);
                OSDArray parcels = (OSDArray)innerResults["Parcels"];
                foreach (OSD o in parcels)
                {
                    resp.Add(new LandData((OSDMap)o));
                }
                break;
            }
            return resp;
        }

        public uint GetNumberOfParcelsByRegion(UUID RegionID, UUID scopeID, UUID owner, ParcelFlags flags, ParcelCategory category)
        {
            Dictionary<string, object> mess = new Dictionary<string, object>();
            mess["Method"] = "GetNumberOfParcelsByRegion";
            mess["RegionID"] = RegionID;
            mess["scopeID"] = scopeID;
            mess["owner"] = owner;
            mess["flags"] = (uint)flags;
            mess["category"] = (int)category;
            string reqString = WebUtils.BuildXmlResponse(mess);

            List<string> m_ServerURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");

            foreach (string m_ServerURI in m_ServerURIs)
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                         m_ServerURI,
                                                                         reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        uint numParcels = 0;
                        foreach (object f in replyvalues.Where(f => uint.TryParse(f.ToString(), out numParcels)))
                        {
                            break;
                        }
                        // Success
                        return numParcels;
                    }
                }
            }
            return 0;
        }

        public List<LandData> GetParcelsWithNameByRegion(uint start, uint count, UUID RegionID, UUID ScopeID, string name)
        {
            List<LandData> resp = new List<LandData>(0);
            if (count == 0)
            {
                return resp;
            }
            OSDMap mess = new OSDMap();
            mess["Method"] = "GetParcelsWithNameByRegion";
            mess["start"] = OSD.FromUInteger(start);
            mess["count"] = OSD.FromUInteger(count);
            mess["RegionID"] = OSD.FromUUID(RegionID);
            mess["ScopeID"] = OSD.FromUUID(ScopeID);
            mess["name"] = OSD.FromString(name);
            List<string> m_ServerURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
            resp = new List<LandData>();
            foreach (string m_ServerURI in m_ServerURIs)
            {
                OSDMap results = WebUtils.PostToService(m_ServerURI + "osd", mess, true, false);
                OSDMap innerResults = (OSDMap)OSDParser.DeserializeJson(results["_RawResult"]);
                OSDArray parcels = (OSDArray)innerResults["Parcels"];
                foreach (OSD o in parcels)
                {
                    resp.Add(new LandData((OSDMap)o));
                }
                break;
            }
            return resp;
        }

        public uint GetNumberOfParcelsWithNameByRegion(UUID RegionID, UUID ScopeID, string name)
        {
            Dictionary<string, object> mess = new Dictionary<string, object>();
            mess["Method"] = "GetNumberOfParcelsWithNameByRegion";
            mess["RegionID"] = RegionID;
            mess["ScopeID"] = ScopeID;
            mess["name"] = name;
            string reqString = WebUtils.BuildXmlResponse(mess);

            List<string> m_ServerURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");

            foreach (string m_ServerURI in m_ServerURIs)
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                         m_ServerURI,
                                                                         reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        uint numParcels = 0;
                        foreach (object f in replyvalues.Where(f => uint.TryParse(f.ToString(), out numParcels)))
                        {
                            break;
                        }
                        // Success
                        return numParcels;
                    }
                }
            }
            return 0;
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
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                Land.AddRange(from m_ServerURI in m_ServerURIs
                              select SynchronousRestFormsRequester.MakeRequest("POST", m_ServerURI, reqString)
                              into reply where reply != string.Empty from object f in WebUtils.ParseXmlResponse(reply)
                              select (KeyValuePair<string, object>) f
                              into value where value.Value is Dictionary<string, object>
                              select value.Value as Dictionary<string, object>
                              into valuevalue select new DirPlacesReplyData(valuevalue));
                return Land.ToArray();
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
            return Land.ToArray();
        }

        public DirLandReplyData[] FindLandForSale(string searchType, uint price, uint area, int StartQuery, uint Flags)
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
                Land.AddRange(from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST", m_ServerURI, reqString) into reply where reply != string.Empty from object f in WebUtils.ParseXmlResponse(reply) select (KeyValuePair<string, object>) f into value where value.Value is Dictionary<string, object> select value.Value as Dictionary<string, object> into valuevalue select new DirLandReplyData(valuevalue));
                return Land.ToArray();
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
            return Land.ToArray();
        }

        #endregion

        #region Events

        public DirEventsReplyData[] FindEvents(string queryText, uint flags, int StartQuery)
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
                Events.AddRange(from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST", m_ServerURI, reqString) into reply where reply != string.Empty from object f in WebUtils.ParseXmlResponse(reply) select (KeyValuePair<string, object>) f into value where value.Value is Dictionary<string, object> select value.Value as Dictionary<string, object> into valuevalue select new DirEventsReplyData(valuevalue));
                return Events.ToArray();
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
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
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                Events.AddRange(from m_ServerURI in m_ServerURIs
                                select SynchronousRestFormsRequester.MakeRequest("POST", m_ServerURI, reqString)
                                into reply where reply != string.Empty from object f in WebUtils.ParseXmlResponse(reply)
                                select (KeyValuePair<string, object>) f
                                into value where value.Value is Dictionary<string, object>
                                select value.Value as Dictionary<string, object>
                                into valuevalue select new DirEventsReplyData(valuevalue));
                return Events.ToArray();
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
            return Events.ToArray();
        }

        public EventData GetEventInfo(uint EventID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["EVENTID"] = EventID;
            sendData["METHOD"] = "geteventinfo";

            string reqString = WebUtils.BuildQueryString(sendData);
            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (EventData eventdata in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                  m_ServerURI,
                                                                                                                                  reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply) into replyData from object f in replyData select (KeyValuePair<string, object>) f into value where value.Value is Dictionary<string, object> select value.Value as Dictionary<string, object> into valuevalue select new EventData(valuevalue))
                {
                    return eventdata;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
            return null;
        }

        public EventData CreateEvent(UUID creator, UUID regionID, UUID parcelID, DateTime date, uint cover, EventFlags maturity, uint flags, uint duration, Vector3 localPos, string name, string description, string category)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["METHOD"] = "CreateEvent";
            sendData["Creator"] = creator;
            sendData["RegionID"] = regionID;
            sendData["ParcelID"] = parcelID;
            sendData["Date"] = date;
            sendData["Cover"] = cover;
            sendData["Maturity"] = maturity;
            sendData["Flags"] = flags;
            sendData["Duration"] = duration;
            sendData["LocalPos"] = localPos;
            sendData["Name"] = name;
            sendData["Description"] = description;
            sendData["Category"] = category;


            string reqString = WebUtils.BuildQueryString(sendData);
            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (EventData eventdata in from m_ServerURI in m_ServerURIs
                                                select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                 m_ServerURI,
                                                                                                 reqString) into reply
                                                where reply != string.Empty
                                                select WebUtils.ParseXmlResponse(reply) into replyData
                                                from object f in replyData
                                                select (KeyValuePair<string, object>)f into value
                                                where value.Value is Dictionary<string, object>
                                                select value.Value as Dictionary<string, object> into valuevalue
                                                select new EventData(valuevalue))
                {
                    return eventdata;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
            return null;
        }

        public List<EventData> GetEvents(uint start, uint count, Dictionary<string, bool> sort, Dictionary<string, object> filter)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["METHOD"] = "GetEvents";
            sendData["start"] = start;
            sendData["count"] = count;
            sendData["sort"] = sort;
            sendData["filter"] = filter;

            string reqString = WebUtils.BuildQueryString(sendData);
            List<EventData> Events = new List<EventData>();
            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (EventData eventdata in from m_ServerURI in m_ServerURIs
                                                select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                 m_ServerURI,
                                                                                                 reqString) into reply
                                                where reply != string.Empty
                                                select WebUtils.ParseXmlResponse(reply) into replyData
                                                from object f in replyData
                                                select (KeyValuePair<string, object>)f into value
                                                where value.Value is Dictionary<string, object>
                                                select value.Value as Dictionary<string, object> into valuevalue
                                                select new EventData(valuevalue))
                {
                    Events.Add(eventdata);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
            return Events;
        }

        public uint GetNumberOfEvents(Dictionary<string, object> filter)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["METHOD"] = "GetEvents";
            sendData["filter"] = filter;

            string reqString = WebUtils.BuildQueryString(sendData);

            List<string> m_ServerURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");

            foreach (string m_ServerURI in m_ServerURIs)
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST", m_ServerURI, reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        uint numEvents = 0;
                        foreach (object f in replyvalues.Where(f => uint.TryParse(f.ToString(), out numEvents)))
                        {
                            break;
                        }
                        // Success
                        return numEvents;
                    }
                }
            }
            return 0;
        }

        public uint GetMaxEventID()
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["METHOD"] = "GetMaxEventID";

            string reqString = WebUtils.BuildQueryString(sendData);

            List<string> m_ServerURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");

            foreach (string m_ServerURI in m_ServerURIs)
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST", m_ServerURI, reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        uint maxEventID = 0;
                        foreach (object f in replyvalues.Where(f => uint.TryParse(f.ToString(), out maxEventID)))
                        {
                            break;
                        }
                        // Success
                        return maxEventID;
                    }
                }
            }
            return 0;
        }

        #endregion

        #region Classifieds

        public DirClassifiedReplyData[] FindClassifieds(string queryText, string category, uint queryFlags, int StartQuery)
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
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                Classifieds.AddRange(from m_ServerURI in m_ServerURIs
                                     select SynchronousRestFormsRequester.MakeRequest("POST", m_ServerURI, reqString)
                                     into reply where reply != string.Empty
                                     from object f in WebUtils.ParseXmlResponse(reply)
                                     select (KeyValuePair<string, object>) f
                                     into value where value.Value is Dictionary<string, object>
                                     select value.Value as Dictionary<string, object>
                                     into valuevalue select new DirClassifiedReplyData(valuevalue));
                return Classifieds.ToArray();
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
            return Classifieds.ToArray();
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
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
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
                            KeyValuePair<string, object> value = (KeyValuePair<string, object>) f;
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
                MainConsole.Instance.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e);
            }
            return Classifieds.ToArray();
        }

        #endregion

        #endregion
    }
}