using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using C5;
using log4net;
using Nini.Config;
using Aurora.Framework;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Aurora.DataManager;
using Aurora.DataManager.MySQL;
using Aurora.DataManager.SQLite;

namespace Aurora.Modules.DataPlugins
{
    public class RemoteDataService : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        internal IConfig m_config;
        string URL = "";
        string Password = "";

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_config = source.Configs["AuroraData"];

            if (null == m_config)
            {
                m_log.Error("[AuroraData]: no data plugin found!");
                return;
            }
            string ServiceName = m_config.GetString("Service", "");
            if (ServiceName != Name)
                return;

            URL = m_config.GetString("URL", "");
            Password = m_config.GetString("Password", "");

            RemoteDataConnector connector = new RemoteDataConnector(URL, Password);
            Aurora.DataManager.DataManager.SetGenericDataPlugin(connector);
            Aurora.DataManager.DataManager.SetProfilePlugin(connector);
            Aurora.DataManager.DataManager.SetRegionPlugin(connector);
        }

        public void PostInitialise(){ }

        public void Close() { }

        public string Name
        {
            get { return "RemoteDataService"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
    }

    public class RemoteDataConnector : IGenericData, IRegionData, IProfileData
    {
        private string URL = "";
        private string Password = "";

        public RemoteDataConnector(string url, string password)
        {
            URL = url;
            Password = password;
        }

        #region IData Members

        public string Identifier
        {
            get { return "RemoteDataConnector"; }
        }

        public List<string> ReadClassifiedInfoRow(string classifiedID)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "classifiedID", classifiedID);
            return ParseList<string>(PerformRemoteOperation("ReadClassifiedInfoRow", parameters));
        }

        public Dictionary<UUID, string> ReadClassifedRow(string creatoruuid)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "creatoruuid", creatoruuid);
            return ParseDictionary<UUID, string>(PerformRemoteOperation("ReadClassifedRow", parameters));
        }

        public Dictionary<UUID, string> ReadPickRow(string creator)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "creator", creator);
            return ParseDictionary<UUID, string>(PerformRemoteOperation("ReadPickRow", parameters));
        }

        public List<string> ReadInterestsInfoRow(string agentID)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "agentID", agentID);
            return ParseList<string>(PerformRemoteOperation("ReadInterestsInfoRow", parameters));
        }

        public List<string> ReadPickInfoRow(string creator, string pickID)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "creator", creator);
            AddParameter(parameters, "pickID", pickID);
            return ParseList<string>(PerformRemoteOperation("ReadPickInfoRow", parameters));
        }

        public AuroraProfileData GetProfileNotes(UUID agentID, UUID target)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "agentID", agentID);
            AddParameter(parameters, "target", target);
            return ParseObject<AuroraProfileData>(PerformRemoteOperation("GetProfileNotes", parameters));
        }

        public void InvalidateProfileNotes(UUID target)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "target", target);
            PerformRemoteOperation("InvalidateProfileNotes", parameters);
        }

        public void FullUpdateUserProfile(AuroraProfileData Profile)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "Profile", Profile);
            PerformRemoteOperation("FullUpdateUserProfile", parameters);
        }

        public List<string> Query(string query)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "query", query);
            return ParseList<string>(PerformRemoteOperation("Query", parameters));
        }

        public AuroraProfileData GetProfileInfo(UUID agentID)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "agentID", agentID);
            return ParseObject<AuroraProfileData>(PerformRemoteOperation("GetProfileInfo", parameters));
        }

        public void UpdateUserProfile(AuroraProfileData Profile)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "Profile", Profile);
            PerformRemoteOperation("UpdateUserProfile", parameters);
        }

        public AuroraProfileData CreateTemperaryAccount(string client, string first, string last)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "client", client);
            AddParameter(parameters, "first", first);
            AddParameter(parameters, "last", last);
            return ParseObject<AuroraProfileData>(PerformRemoteOperation("CreateTemperaryAccount", parameters));
        }

        public DirPlacesReplyData[] PlacesQuery(string queryText, string category, string table, string wantedValue, int StartQuery)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "queryText", queryText);
            AddParameter(parameters, "category", category);
            AddParameter(parameters, "table", table);
            AddParameter(parameters, "wantedValue", wantedValue);
            AddParameter(parameters, "StartQuery", StartQuery);
            return ParseArray<DirPlacesReplyData>(PerformRemoteOperation("PlacesQuery", parameters));
        }

        public DirLandReplyData[] LandForSaleQuery(string searchType, string price, string area, string table, string wantedValue, int StartQuery)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "searchType", searchType);
            AddParameter(parameters, "price", price);
            AddParameter(parameters, "area", area);
            AddParameter(parameters, "table", table);
            AddParameter(parameters, "wantedValue", wantedValue);
            AddParameter(parameters, "StartQuery", StartQuery);
            return ParseArray<DirLandReplyData>(PerformRemoteOperation("LandForSaleQuery", parameters));
        }

        public DirClassifiedReplyData[] ClassifiedsQuery(string queryText, string category, string queryFlags, int StartQuery)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "queryText", queryText);
            AddParameter(parameters, "category", category);
            AddParameter(parameters, "queryFlags", queryFlags);
            AddParameter(parameters, "StartQuery", StartQuery);
            return ParseArray<DirClassifiedReplyData>(PerformRemoteOperation("ClassifiedsQuery", parameters));
        }

        public DirEventsReplyData[] EventQuery(string queryText, string flags, string table, string wantedValue, int StartQuery)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "queryText", queryText);
            AddParameter(parameters, "flags", flags);
            AddParameter(parameters, "table", table);
            AddParameter(parameters, "wantedValue", wantedValue);
            AddParameter(parameters, "StartQuery", StartQuery);
            return ParseArray<DirEventsReplyData>(PerformRemoteOperation("EventQuery", parameters));
        }

        public EventData GetEventInfo(string eventid)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "eventid", eventid);
            return ParseObject<EventData>(PerformRemoteOperation("GetEventInfo", parameters));
        }
        
        public DirEventsReplyData[] GetAllEventsNearXY(string table, int X, int Y)
		{
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "table", table);
            AddParameter(parameters, "X", X);
            AddParameter(parameters, "Y", Y);
            return ParseArray<DirEventsReplyData>(PerformRemoteOperation("GetAllEventsNearXY", parameters));
        }

        public Dictionary<string, string> GetRegionHidden()
        {
            throw new NotImplementedException();
        }

        public string AbuseReports()
        {
            var parameters = new Dictionary<string, string>();
            return ParseObject<string>(PerformRemoteOperation("AbuseReports", parameters));
        }

        public ObjectMediaURLInfo[] getObjectMediaInfo(string objectID)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "objectID", objectID);
            return ParseArray<ObjectMediaURLInfo>(PerformRemoteOperation("getObjectMediaInfo", parameters));
        }

        public bool GetIsRegionMature(string uuid)
		{
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "uuid", uuid);
            return ParseObject<bool>(PerformRemoteOperation("GetIsRegionMature", parameters));
		}
        
        public EventData[] GetEvents()
        {
            var parameters = new Dictionary<string, string>();
            return ParseArray<EventData>(PerformRemoteOperation("GetEvents", parameters));
        }

        public Classified[] GetClassifieds()
        {
            var parameters = new Dictionary<string, string>();
            return ParseArray<Classified>(PerformRemoteOperation("GetClassifieds",parameters));
        }

        public AbuseReport GetAbuseReport(int formNumber)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "formNumber", formNumber);
            return ParseObject<AbuseReport>(PerformRemoteOperation("GetAbuseReport",parameters));
        }

        public OfflineMessage[] GetOfflineMessages(string agentID)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "agentID", agentID);
            return ParseArray<OfflineMessage>(PerformRemoteOperation("GetOfflineMessages",parameters));
        }

        public void AddOfflineMessage(string fromUUID, string fromName, string toUUID, string message)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters,"fromUUID", fromUUID);
            AddParameter(parameters,"fromName", fromName);
            AddParameter(parameters,"toUUID", toUUID);
            AddParameter(parameters,"message", message);
            PerformRemoteOperation("AddOfflineMessage",parameters);
        }

        private List<T> ParseList<T>(string operation)
        {
            return null;
        }

        private T[] ParseArray<T>(string result)
        {
            return null;
        }

        private T ParseObject<T>(string result)
        {
            
            return ConvertStringToObject<T>(result);
        }

        private Dictionary<UUID, string> ParseDictionary<T, T1>(string operation)
        {
            throw new NotImplementedException();
        }

        private string PerformRemoteOperation(string operation, Dictionary<string, string> parameters)
        {
            return "";
        }

        private void AddParameter<T>(Dictionary<string, string> parameters, string name, T value)
        {
            string v = ConvertObjectToString<T>(value);
            parameters.Add(name.ToLower(),v);
        }

        public T ConvertStringToObject<T>(string value)
        {
            if (typeof(T) == typeof(string))
            {
                return (T) new Object();
            }
            else
            {
                throw new ArgumentException("Invalid type!");
            }
        }

        private string ConvertObjectToString<T>(object value)
        {
            string v = "";
            if (value.GetType() == typeof(string))
            {

            }
            else
            {
                v = value.ToString();
            }
            return v;
        }

        #endregion

        public void Update(string table, string[] setValues, string[] setRows, string[] keyRows, string[] keyValues)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "table", table);
            AddParameter(parameters, "setValues", setValues);
            AddParameter(parameters, "setRows", setRows);
            AddParameter(parameters, "keyRows", keyRows);
            AddParameter(parameters, "keyValues", keyValues);
            PerformRemoteOperation("Update", parameters);
        }

        public List<string> Query(string keyRow, string keyValue, string table, string wantedValue)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "keyRow", keyRow);
            AddParameter(parameters, "keyValue", keyValue);
            AddParameter(parameters, "table", table);
            AddParameter(parameters, "wantedValue", wantedValue);
            return ParseList<string>(PerformRemoteOperation("Query", parameters));
        }

        public List<string> Query(string[] keyRow, string[] keyValue, string table, string wantedValue)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "keyRow", keyRow);
            AddParameter(parameters, "keyValue", keyValue);
            AddParameter(parameters, "table", table);
            AddParameter(parameters, "wantedValue", wantedValue);
            return ParseList<string>(PerformRemoteOperation("Query", parameters));
        }

        public void Insert(string table, string[] values)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "table", table);
            AddParameter(parameters, "values", values);
            PerformRemoteOperation("Insert", parameters);
        }

        public void Delete(string table, string[] keys, string[] values)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "table", table);
            AddParameter(parameters, "keys", values);
            AddParameter(parameters, "values", values);
            PerformRemoteOperation("Delete", parameters);
        }

        public void Insert(string table, string[] values, string updateKey, string updateValue)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "table", table);
            AddParameter(parameters, "values", values);
            AddParameter(parameters, "updateKey", updateKey);
            AddParameter(parameters, "updateValue", updateValue);
            PerformRemoteOperation("Insert", parameters);
        }

        public void StoreRegionWindlightSettings(RegionLightShareData lightShareData)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "lightShareData", lightShareData);
            PerformRemoteOperation("StoreRegionWindlightSettings", parameters);
        }

        public RegionLightShareData LoadRegionWindlightSettings(UUID regionUUID)
        {
            var parameters = new Dictionary<string, string>();
            AddParameter(parameters, "regionUUID", regionUUID);
            return ParseObject<RegionLightShareData>(PerformRemoteOperation("LoadRegionWindlightSettings", parameters));
        }
    }
}
