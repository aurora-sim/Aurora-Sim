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
    public class RemoteDirectoryServiceConnector : IDirectoryServiceConnector, IAuroraDataPlugin
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public void Initialise(IGenericData GenericData, IConfigSource source)
        {
            if (source.Configs["AuroraConnectors"].GetString("DirectoryServiceConnector", "LocalConnector") == "RemoteConnector")
            {
                m_ServerURI = source.Configs["AuroraData"].GetString("RemoteServerURI", "");
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
        public void AddLandObject(LandData args, UUID regionID, bool forSale, uint EstateID, bool showInSearch, UUID InfoUUID)
        {
            Dictionary<string, object> sendData = ConvertFromLandData(args, regionID, forSale, EstateID, showInSearch, InfoUUID).ToKeyValuePairs();

            sendData["METHOD"] = "addlandobject";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        private AuroraLandData ConvertFromLandData(LandData data, UUID regionID, bool forSale, uint EstateID, bool showInSearch, UUID InfoUUID)
        {
            AuroraLandData adata = new AuroraLandData();
            adata.Area = data.Area;
            adata.AuctionID = data.AuctionID;
            adata.AuthBuyerID = data.AuthBuyerID;
            adata.Category = data.Category;
            adata.ClaimDate = data.ClaimDate;
            adata.ClaimPrice = data.ClaimPrice;
            adata.Description = data.Description;
            adata.Dwell = data.Dwell;
            adata.EstateID = EstateID;
            adata.Flags = data.Flags;
            adata.ForSale = forSale;
            adata.GroupID = data.GroupID;
            adata.InfoUUID = InfoUUID;
            adata.LandingType = data.LandingType;
            adata.LandingX = data.UserLocation.X;
            adata.LandingY = data.UserLocation.Y;
            adata.LandingZ = data.UserLocation.Z;
            adata.LocalID = data.LocalID;
            adata.LookAtX = data.UserLookAt.X;
            adata.LookAtY = data.UserLookAt.Y;
            adata.LookAtZ = data.UserLookAt.Z;
            adata.Maturity = data.Maturity;
            adata.Name = data.Name;
            adata.OwnerID = data.OwnerID;
            adata.ParcelID = data.GlobalID;
            adata.RegionID = regionID;
            adata.SalePrice = data.SalePrice;
            adata.ShowInSearch = showInSearch;
            adata.SnapshotID = data.SnapshotID;
            adata.Status = data.Status;
            return adata;
        }

        public AuroraLandData GetParcelInfo(UUID InfoUUID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["INFOUUID"] = InfoUUID.ToString();
            sendData["METHOD"] = "getparcelinfo";

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
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        AuroraLandData land = null;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                land = new AuroraLandData((Dictionary<string, object>)f);
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
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public AuroraLandData[] GetParcelByOwner(UUID OwnerID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["OWNERID"] = OwnerID;
            sendData["METHOD"] = "getparcelbyowner";

            string reqString = ServerUtils.BuildQueryString(sendData);
            List<AuroraLandData> Land = new List<AuroraLandData>();
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    foreach (object f in replyData)
                    {
                        KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                        if (value.Value is Dictionary<string, object>)
                        {
                            Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                            AuroraLandData land = new AuroraLandData(valuevalue);
                            Land.Add(land);
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

        public DirPlacesReplyData[] FindLand(string queryText, string category, int StartQuery)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["QUERYTEXT"] = queryText;
            sendData["CATEGORY"] = category;
            sendData["STARTQUERY"] = StartQuery;
            sendData["METHOD"] = "findland";

            string reqString = ServerUtils.BuildQueryString(sendData);
            List<DirPlacesReplyData> Land = new List<DirPlacesReplyData>();
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

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
                return Land.ToArray();
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
            return Land.ToArray();
        }

        public DirLandReplyData[] FindLandForSale(string searchType, string price, string area, int StartQuery)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SEARCHTYPE"] = searchType;
            sendData["PRICE"] = price;
            sendData["AREA"] = area;
            sendData["STARTQUERY"] = StartQuery;
            sendData["METHOD"] = "findlandforsale";

            string reqString = ServerUtils.BuildQueryString(sendData);
            List<DirLandReplyData> Land = new List<DirLandReplyData>();
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

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

            string reqString = ServerUtils.BuildQueryString(sendData);
            List<DirEventsReplyData> Events = new List<DirEventsReplyData>();
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

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
                return Events.ToArray();
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteDirectoryServiceConnector]: Exception when contacting server: {0}", e.Message);
            }
            return Events.ToArray();
        }

        public DirEventsReplyData[] FindAllEventsInRegion(string regionName)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONNAME"] = regionName;
            sendData["METHOD"] = "findeventsinregion";

            string reqString = ServerUtils.BuildQueryString(sendData);
            List<DirEventsReplyData> Events = new List<DirEventsReplyData>();
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

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

            string reqString = ServerUtils.BuildQueryString(sendData);
            List<DirClassifiedReplyData> Classifieds = new List<DirClassifiedReplyData>();
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

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

            string reqString = ServerUtils.BuildQueryString(sendData);
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

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

            string reqString = ServerUtils.BuildQueryString(sendData);
            List<Classified> Classifieds = new List<Classified>();
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    foreach (object f in replyData)
                    {
                        KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                        if (value.Value is Dictionary<string, object>)
                        {
                            Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                            Classified classified = new Classified(valuevalue);
                            Classifieds.Add(classified);
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
    }
}
