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
using System.Collections.Specialized;
using System.Linq;
using Aurora.Simulation.Base;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Services.DataService
{
    public class SimianUtils
    {
        private static readonly ExpiringCache<string, OSDMap> m_memoryCache = new ExpiringCache<string, OSDMap>();
        private static int m_cacheTimeout = 30;

        public static bool GetGenericEntry(UUID ownerID, string type, string key, string m_ServerURI, out OSDMap map)
        {
            //MainConsole.Instance.InfoFormat("[SIMIAN-MUTELIST-CONNECTOR]  {0} called ({1},{2},{3})", System.Reflection.MethodBase.GetCurrentMethod().Name, ownerID, type, key);

            NameValueCollection RequestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetGenerics"},
                                                      {"OwnerID", ownerID.ToString()},
                                                      {"Type", type},
                                                      {"Key", key}
                                                  };


            OSDMap Response = CachedPostRequest(RequestArgs, m_ServerURI);
            if (Response["Success"].AsBoolean() && Response["Entries"] is OSDArray)
            {
                OSDArray entryArray = (OSDArray) Response["Entries"];
                if (entryArray.Count == 1)
                {
                    OSDMap entryMap = entryArray[0] as OSDMap;
                    key = entryMap["Key"].AsString();
                    map = (OSDMap) OSDParser.DeserializeJson(entryMap["Value"].AsString());

                    //MainConsole.Instance.InfoFormat("[SIMIAN-MUTELIST-CONNECTOR]  Generics Result {0}", entryMap["Value"].AsString());

                    return true;
                }
                else
                {
                    ///MainConsole.Instance.InfoFormat("[SIMIAN-MUTELIST-CONNECTOR]  No Generics Results");
                }
            }
            else
            {
                //MainConsole.Instance.WarnFormat("[SIMIAN MUTELIST CONNECTOR]: Error retrieving group info ({0})", Response["Message"]);
            }
            map = null;
            return false;
        }

        public static bool AddGeneric(UUID ownerID, string type, string key, OSDMap map, string m_ServerURI)
        {
            string value = OSDParser.SerializeJsonString(map);

            NameValueCollection RequestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "AddGeneric"},
                                                      {"OwnerID", ownerID.ToString()},
                                                      {"Type", type},
                                                      {"Key", key},
                                                      {"Value", value}
                                                  };


            OSDMap Response = CachedPostRequest(RequestArgs, m_ServerURI);
            if (Response["Success"].AsBoolean())
            {
                return true;
            }
            else
            {
                //MainConsole.Instance.WarnFormat("[SIMIAN GROUPS CONNECTOR]: Error {0}, {1}, {2}, {3}", ownerID, type, key, Response["Message"]);
                return false;
            }
        }

        private static OSDMap CachedPostRequest(NameValueCollection requestArgs, string m_ServerURI)
        {
            // Immediately forward the request if the cache is disabled.
            if (m_cacheTimeout == 0)
            {
                return WebUtils.PostToService(m_ServerURI, requestArgs);
            }

            // Check if this is an update or a request
            if (requestArgs["RequestMethod"] == "RemoveGeneric"
                || requestArgs["RequestMethod"] == "AddGeneric"
                )
            {
                // Any and all updates cause the cache to clear
                m_memoryCache.Clear();

                // Send update to server, return the response without caching it
                return WebUtils.PostToService(m_ServerURI, requestArgs);
            }

            // If we're not doing an update, we must be requesting data

            // Create the cache key for the request and see if we have it cached
            string CacheKey = WebUtils.BuildQueryString(requestArgs);
            OSDMap response = null;
            if (!m_memoryCache.TryGetValue(CacheKey, out response))
            {
                // if it wasn't in the cache, pass the request to the Simian Grid Services 
                response = WebUtils.PostToService(m_ServerURI, requestArgs);

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
                                                      {"RequestMethod", "RemoveGeneric"},
                                                      {"OwnerID", ownerID.ToString()},
                                                      {"Type", type},
                                                      {"Key", key}
                                                  };


            OSDMap response = CachedPostRequest(requestArgs, m_ServerURI);
            if (response["Success"].AsBoolean())
            {
                return true;
            }
            else
            {
                //MainConsole.Instance.WarnFormat("[SIMIAN MUTELIST CONNECTOR]: Error {0}, {1}, {2}, {3}", ownerID, type, key, response["Message"]);
                return false;
            }
        }

        public static bool GetGenericEntries(UUID ownerID, string type, string m_ServerURI,
                                             out Dictionary<string, OSDMap> maps)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetGenerics"},
                                                      {"OwnerID", ownerID.ToString()},
                                                      {"Type", type}
                                                  };


            OSDMap response = CachedPostRequest(requestArgs, m_ServerURI);
            if (response["Success"].AsBoolean() && response["Entries"] is OSDArray)
            {
                OSDArray entryArray = (OSDArray) response["Entries"];
#if (!ISWIN)
                maps = new Dictionary<string, OSDMap>();
                foreach (OSDMap map in entryArray)
                    maps.Add(map["Key"].AsString(), (OSDMap) OSDParser.DeserializeJson(map["Value"].AsString()));
#else
                maps = entryArray.Cast<OSDMap>().ToDictionary(entryMap => entryMap["Key"].AsString(), entryMap => (OSDMap) OSDParser.DeserializeJson(entryMap["Value"].AsString()));
#endif
                if (maps.Count == 0)
                {
                    //MainConsole.Instance.InfoFormat("[SIMIAN-MUTELIST-CONNECTOR]  No Generics Results");
                }

                return true;
            }
            else
            {
                maps = null;
                //MainConsole.Instance.WarnFormat("[SIMIAN MUTELIST CONNECTOR]: Error retrieving group info ({0})", response["Message"]);
            }
            return false;
        }

        private bool GetGenericEntries(string type, string key, string m_ServerURI, out Dictionary<UUID, OSDMap> maps)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetGenerics"},
                                                      {"Type", type},
                                                      {"Key", key}
                                                  };


            OSDMap response = CachedPostRequest(requestArgs, m_ServerURI);
            if (response["Success"].AsBoolean() && response["Entries"] is OSDArray)
            {
                OSDArray entryArray = (OSDArray) response["Entries"];
#if (!ISWIN)
                maps = new Dictionary<UUID, OSDMap>();
                foreach (OSDMap map in entryArray)
                    maps.Add(map["OwnerID"].AsUUID(), (OSDMap) OSDParser.DeserializeJson(map["Value"].AsString()));
#else
                maps = entryArray.Cast<OSDMap>().ToDictionary(entryMap => entryMap["OwnerID"].AsUUID(), entryMap => (OSDMap) OSDParser.DeserializeJson(entryMap["Value"].AsString()));
#endif
                if (maps.Count == 0)
                {
                    //MainConsole.Instance.InfoFormat("[SIMIAN-MUTELIST-CONNECTOR]  No Generics Results");
                }
                return true;
            }
            else
            {
                maps = null;
                //MainConsole.Instance.WarnFormat("[SIMIAN-MUTELIST-CONNECTOR]: Error retrieving group info ({0})", response["Message"]);
            }
            return false;
        }
    }
}