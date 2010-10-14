using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class SimianUtils
    {
        private static ExpiringCache<string, OSDMap> m_memoryCache;
        private static int m_cacheTimeout = 30;

        public static bool GetGenericEntry(UUID ownerID, string type, string key, string m_ServerURI, out OSDMap map)
        {
            //m_log.InfoFormat("[SIMIAN-MUTELIST-CONNECTOR]  {0} called ({1},{2},{3})", System.Reflection.MethodBase.GetCurrentMethod().Name, ownerID, type, key);

            NameValueCollection RequestArgs = new NameValueCollection
            {
                { "RequestMethod", "GetGenerics" },
                { "OwnerID", ownerID.ToString() },
                { "Type", type },
                { "Key", key}
            };


            OSDMap Response = CachedPostRequest(RequestArgs, m_ServerURI);
            if (Response["Success"].AsBoolean() && Response["Entries"] is OSDArray)
            {
                OSDArray entryArray = (OSDArray)Response["Entries"];
                if (entryArray.Count == 1)
                {
                    OSDMap entryMap = entryArray[0] as OSDMap;
                    key = entryMap["Key"].AsString();
                    map = (OSDMap)OSDParser.DeserializeJson(entryMap["Value"].AsString());

                    //m_log.InfoFormat("[SIMIAN-MUTELIST-CONNECTOR]  Generics Result {0}", entryMap["Value"].AsString());

                    return true;
                }
                else
                {
                    ///m_log.InfoFormat("[SIMIAN-MUTELIST-CONNECTOR]  No Generics Results");
                }
            }
            else
            {
                //m_log.WarnFormat("[SIMIAN MUTELIST CONNECTOR]: Error retrieving group info ({0})", Response["Message"]);
            }
            map = null;
            return false;
        }

        public static bool AddGeneric(UUID ownerID, string type, string key, OSDMap map, string m_ServerURI)
        {
            string value = OSDParser.SerializeJsonString(map);

            NameValueCollection RequestArgs = new NameValueCollection
            {
                { "RequestMethod", "AddGeneric" },
                { "OwnerID", ownerID.ToString() },
                { "Type", type },
                { "Key", key },
                { "Value", value}
            };


            OSDMap Response = CachedPostRequest(RequestArgs, m_ServerURI);
            if (Response["Success"].AsBoolean())
            {
                return true;
            }
            else
            {
                //m_log.WarnFormat("[SIMIAN GROUPS CONNECTOR]: Error {0}, {1}, {2}, {3}", ownerID, type, key, Response["Message"]);
                return false;
            }
        }

        static OSDMap CachedPostRequest(NameValueCollection requestArgs, string m_ServerURI)
        {
            // Immediately forward the request if the cache is disabled.
            if (m_cacheTimeout == 0)
            {
                return WebUtil.PostToService(m_ServerURI, requestArgs);
            }

            // Check if this is an update or a request
            if (requestArgs["RequestMethod"] == "RemoveGeneric"
                || requestArgs["RequestMethod"] == "AddGeneric"
                )
            {
                // Any and all updates cause the cache to clear
                m_memoryCache.Clear();

                // Send update to server, return the response without caching it
                return WebUtil.PostToService(m_ServerURI, requestArgs);

            }

            // If we're not doing an update, we must be requesting data

            // Create the cache key for the request and see if we have it cached
            string CacheKey = WebUtil.BuildQueryString(requestArgs);
            OSDMap response = null;
            if (!m_memoryCache.TryGetValue(CacheKey, out response))
            {
                // if it wasn't in the cache, pass the request to the Simian Grid Services 
                response = WebUtil.PostToService(m_ServerURI, requestArgs);

                // and cache the response
                m_memoryCache.AddOrUpdate(CacheKey, response, TimeSpan.FromSeconds(m_cacheTimeout));
            }

            // return cached response
            return response;
        }

        public static bool RemoveGenericEntry(UUID ownerID, string type, string key, string m_ServerURI)
        {
            NameValueCollection requestArgs = new NameValueCollection
            {
                { "RequestMethod", "RemoveGeneric" },
                { "OwnerID", ownerID.ToString() },
                { "Type", type },
                { "Key", key }
            };


            OSDMap response = CachedPostRequest(requestArgs, m_ServerURI);
            if (response["Success"].AsBoolean())
            {
                return true;
            }
            else
            {
                //m_log.WarnFormat("[SIMIAN MUTELIST CONNECTOR]: Error {0}, {1}, {2}, {3}", ownerID, type, key, response["Message"]);
                return false;
            }
        }

        public static bool GetGenericEntries(UUID ownerID, string type, string m_ServerURI, out Dictionary<string, OSDMap> maps)
        {
            NameValueCollection requestArgs = new NameValueCollection
            {
                { "RequestMethod", "GetGenerics" },
                { "OwnerID", ownerID.ToString() },
                { "Type", type }
            };



            OSDMap response = CachedPostRequest(requestArgs, m_ServerURI);
            if (response["Success"].AsBoolean() && response["Entries"] is OSDArray)
            {
                maps = new Dictionary<string, OSDMap>();

                OSDArray entryArray = (OSDArray)response["Entries"];
                foreach (OSDMap entryMap in entryArray)
                {
                    //m_log.InfoFormat("[SIMIAN-MUTELIST-CONNECTOR]  Generics Result {0}", entryMap["Value"].AsString());
                    maps.Add(entryMap["Key"].AsString(), (OSDMap)OSDParser.DeserializeJson(entryMap["Value"].AsString()));
                }
                if (maps.Count == 0)
                {
                    //m_log.InfoFormat("[SIMIAN-MUTELIST-CONNECTOR]  No Generics Results");
                }

                return true;
            }
            else
            {
                maps = null;
                //m_log.WarnFormat("[SIMIAN MUTELIST CONNECTOR]: Error retrieving group info ({0})", response["Message"]);
            }
            return false;
        }

        private bool GetGenericEntries(string type, string key, string m_ServerURI, out Dictionary<UUID, OSDMap> maps)
        {
            NameValueCollection requestArgs = new NameValueCollection
            {
                { "RequestMethod", "GetGenerics" },
                { "Type", type },
                { "Key", key }
            };



            OSDMap response = CachedPostRequest(requestArgs, m_ServerURI);
            if (response["Success"].AsBoolean() && response["Entries"] is OSDArray)
            {
                maps = new Dictionary<UUID, OSDMap>();

                OSDArray entryArray = (OSDArray)response["Entries"];
                foreach (OSDMap entryMap in entryArray)
                {
                    maps.Add(entryMap["OwnerID"].AsUUID(), (OSDMap)OSDParser.DeserializeJson(entryMap["Value"].AsString()));
                }
                if (maps.Count == 0)
                {
                    //m_log.InfoFormat("[SIMIAN-MUTELIST-CONNECTOR]  No Generics Results");
                }
                return true;
            }
            else
            {
                maps = null;
                //m_log.WarnFormat("[SIMIAN-MUTELIST-CONNECTOR]: Error retrieving group info ({0})", response["Message"]);
            }
            return false;
        }
    }
}
