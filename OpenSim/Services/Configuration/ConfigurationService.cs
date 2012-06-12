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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nini.Config;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.ConfigurationService
{
    /// <summary>
    ///   This is an application plugin so that it loads asap as it is used by many things (IService modules especially)
    /// </summary>
    public class ConfigurationService : IConfigurationService, IApplicationPlugin
    {
        #region Declares

        protected Dictionary<string, OSDMap> m_allConfigs = new Dictionary<string, OSDMap>();
        protected OSDMap m_autoConfig = new OSDMap();
        protected IConfigSource m_config;
        protected Dictionary<string, OSDMap> m_knownUsers = new Dictionary<string, OSDMap>();

        #endregion

        #region IApplicationPlugin Members

        public void PreStartup(ISimulationBase simBase)
        {
        }

        public void Initialize(ISimulationBase openSim)
        {
            m_config = openSim.ConfigSource;

            IConfig handlerConfig = m_config.Configs["Handlers"];
            if (handlerConfig.GetString("ConfigurationHandler", "") != Name)
                return;

            //Register us
            openSim.ApplicationRegistry.RegisterModuleInterface<IConfigurationService>(this);

            FindConfiguration(m_config.Configs["Configuration"]);
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
        }

        public void PostStart()
        {
        }

        public void Close()
        {
        }

        public void ReloadConfiguration(IConfigSource m_config)
        {
            IConfig handlerConfig = m_config.Configs["Handlers"];
            if (handlerConfig.GetString("ConfigurationHandler", "") != Name)
                return;

            FindConfiguration(m_config.Configs["Configuration"]);
        }

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        #endregion

        #region IConfigurationService Members

        public virtual void AddNewUser(string userID, OSDMap urls)
        {
            m_knownUsers[userID] = urls;
        }

        public virtual void AddNewUrls(string key, OSDMap urls)
        {
            if(urls.ContainsKey("ServerURI"))
                m_autoConfig.Remove("ServerURI");
            foreach (KeyValuePair<string, OSD> kvp in urls)
            {
                if (kvp.Value == "" && kvp.Value.Type != OSDType.Array)
                    continue;
                if (!m_autoConfig.ContainsKey(kvp.Key))
                {
                    if (kvp.Value.Type == OSDType.String)
                        m_autoConfig[kvp.Key] = kvp.Value;
                    else if (kvp.Value.Type != OSDType.Boolean) // "Success" coming from IWC
                        m_autoConfig[kvp.Key] = string.Join(",", ((OSDArray)kvp.Value).ConvertAll<string>((osd) => osd).ToArray());
                }
                else
                {
                    List<string> keys = kvp.Value.Type == OSDType.Array ? ((OSDArray)kvp.Value).ConvertAll<string>((osd) => osd) : new List<string>(new string[1] { kvp.Value.AsString() });

                    foreach (string url in keys)
                    {
                        //Check to see whether the base URLs are the same (removes the UUID at the end)
                        if (url.Length < 36)
                            continue; //Not a URL
                        string u = url.Remove(url.Length - 36, 36);
                        if (!m_autoConfig[kvp.Key].AsString().Contains(u))
                            m_autoConfig[kvp.Key] = m_autoConfig[kvp.Key] + "," + kvp.Value;
                    }
                }
            }
            m_allConfigs[key] = urls;
        }

        public virtual void RemoveUrls(string key)
        {
            if (!m_allConfigs.ContainsKey(key))
                return;
            OSDMap newAutoConfig = new OSDMap();
            foreach (KeyValuePair<string, OSD> kvp in m_autoConfig)
            {
                if (kvp.Value == "")
                    continue;
                if (m_autoConfig.ContainsKey(kvp.Key))
                {
                    string[] s = m_autoConfig[kvp.Key].AsString().Split(',');
                    List<string> newS = new List<string>();
                    foreach (string ss in s)
                        if (ss == m_allConfigs[key][kvp.Key])
                            continue;
                        else
                            newS.Add(ss);

                    newAutoConfig[kvp.Key] = string.Join(",", newS.ToArray());
                }
            }
            m_autoConfig = newAutoConfig;
            m_allConfigs.Remove(key);
        }

        public virtual OSDMap GetValues()
        {
            return m_autoConfig;
        }

        public virtual OSDMap GetValuesFor(string key)
        {
            return m_allConfigs[key];
        }

        public virtual List<string> FindValueOf(string key)
        {
            List<string> keys = new List<string>();

            keys = m_autoConfig.ContainsKey(key) ? FindValueOfFromOSDMap(key, m_autoConfig) : FindValueOfFromConfiguration(key);
            RemoveBlanks(ref keys);
            return keys;
        }

        public virtual List<string> FindValueOf(string userID, string key)
        {
            if (m_knownUsers.ContainsKey(userID) && m_knownUsers[userID][key] != "")
            {
                return FindValueOfFromOSDMap(key, m_knownUsers[userID]);
            }
            if (m_allConfigs.ContainsKey(userID) && m_allConfigs[userID][key] != "")
            {
                return FindValueOfFromOSDMap(key, m_allConfigs[userID]);
            }
#if (!ISWIN)
            foreach (string name in m_allConfigs.Keys)
            {
                if (m_allConfigs[name].ContainsKey(key) && m_allConfigs[name][key] != "")
                {
                    return FindValueOfFromOSDMap(key, m_allConfigs[name]);
                }
            }
#else
            foreach (string name in m_allConfigs.Keys.Where(name => m_allConfigs[name].ContainsKey(key) && m_allConfigs[name][key] != ""))
            {
                return FindValueOfFromOSDMap(key, m_allConfigs[name]);
            }
#endif
            return FindValueOf(key);
        }

        public virtual List<string> FindValueOf(string userID, string key, bool returnAll)
        {
            if (userID == "" || userID == OpenMetaverse.UUID.Zero.ToString())
                return FindValueOf(key);
            if (!returnAll)
                return FindValueOf(userID, key);

            RemoveDupsList urls = new RemoveDupsList();
            if (m_knownUsers.ContainsKey(userID) && m_knownUsers[userID][key] != "")
            {
                urls.AddRange(FindValueOfFromOSDMap(key, m_knownUsers[userID]));
            }
            if (m_allConfigs.ContainsKey(userID) && m_allConfigs[userID][key] != "")
            {
                urls.AddRange(FindValueOfFromOSDMap(key, m_allConfigs[userID]));
            }
#if (!ISWIN)
            foreach (string name in m_allConfigs.Keys)
            {
                if (m_allConfigs[name].ContainsKey(key) && m_allConfigs[name][key] != "")
                {
                    urls.AddRange(FindValueOfFromOSDMap(key, m_allConfigs[name]));
                }
            }
#else
            foreach (string name in m_allConfigs.Keys.Where(name => m_allConfigs[name].ContainsKey(key) && m_allConfigs[name][key] != ""))
            {
                urls.AddRange(FindValueOfFromOSDMap(key, m_allConfigs[name]));
            }
#endif
            urls.AddRange(FindValueOf(key));

            return urls.Urls;
        }

        public virtual List<string> FindValueOf(string userID, string regionID, string key)
        {
            if (m_knownUsers.ContainsKey(userID))
            {
                return FindValueOfFromOSDMap(key, m_knownUsers[userID]);
            }
            else if (m_allConfigs.ContainsKey(userID))
            {
                return FindValueOfFromOSDMap(key, m_allConfigs[userID]);
            }
            else if (m_allConfigs.ContainsKey(userID + "|" + regionID))
            {
                return FindValueOfFromOSDMap(key, m_allConfigs[userID + "|" + regionID]);
            }
            else
            {
                if (m_knownUsers.ContainsKey(regionID))
                {
                    return FindValueOfFromOSDMap(key, m_knownUsers[regionID]);
                }
                else if (m_allConfigs.ContainsKey(regionID))
                {
                    return FindValueOfFromOSDMap(key, m_allConfigs[regionID]);
                }
#if (!ISWIN)
                foreach (string name in m_allConfigs.Keys)
                {
                    if (m_allConfigs[name].ContainsKey(key))
                    {
                        return FindValueOfFromOSDMap(key, m_allConfigs[name]);
                    }
                }
#else
                foreach (string name in m_allConfigs.Keys.Where(name => m_allConfigs[name].ContainsKey(key)))
                {
                    return FindValueOfFromOSDMap(key, m_allConfigs[name]);
                }
#endif
            }
            return FindValueOf(key);
        }

        #endregion

        public void Dispose()
        {
        }

        protected void FindConfiguration(IConfig autoConfig)
        {
            if (autoConfig == null)
                return;

            string serverURL = autoConfig.GetString("RegistrationURI", "");
            OSDMap request = new OSDMap();
            if (serverURL == "")
            {
                //Get the urls from the config
                GetConfigFor("GridServerURI", request);
                request["RegistrationURI"] = request["GridServerURI"];
                GetConfigFor("GridUserServerURI", request);
                GetConfigFor("AssetServerURI", request);
                GetConfigFor("InventoryServerURI", request);
                GetConfigFor("AvatarServerURI", request);
                GetConfigFor("PresenceServerURI", request);
                GetConfigFor("UserInfoServerURI", request);
                GetConfigFor("UserAccountServerURI", request);
                GetConfigFor("AuthenticationServerURI", request);
                GetConfigFor("FriendsServerURI", request);
                GetConfigFor("RemoteServerURI", request);
                GetConfigFor("EventQueueServiceURI", request);
                GetConfigFor("AbuseReportURI", request);
                AddNewUrls("default", request);
            }
            else
            {
                GetConfigFor("RegistrationURI", request);
                AddNewUrls("default", request);
            }
        }

        public void GetConfigFor(string name, OSDMap request)
        {
            request[name] = m_config.Configs["Configuration"].GetString(name, "");
        }

        public virtual void RemoveBlanks(ref List<string> keys)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] == "")
                {
                    keys.RemoveAt(i);
                    i--;
                }
            }
        }

        public virtual List<string> FindValueOfFromOSDMap(string key, OSDMap urls)
        {
            List<string> keys = new List<string>();
            int next = urls["Next" + key].AsInteger();
            string[] configKeys = urls[key].AsString().Split(',');
            if (configKeys.Length == 1)
                return new List<string>(configKeys);
            if (next >= configKeys.Length - 1)
                next = 0;
            for (int i = next; i < configKeys.Length - 1; i++)
                keys.Add(configKeys[i]);
            for (int i = 0; i < next; i++)
                keys.Add(configKeys[i]);
            urls["Next" + key] = ++next;
            return keys;
        }

        public virtual List<string> FindValueOfFromConfiguration(string key)
        {
            List<string> keys = new List<string>();

            if (m_config.Configs["Configuration"] != null)
            {
                string[] configKeys = m_config.Configs["Configuration"].GetString(key, "").Split(',');
                keys.AddRange(configKeys);
            }

            return keys;
        }

        #region Nested type: RemoveDupsList

        private class RemoveDupsList
        {
            public readonly List<string> Urls = new List<string>();

            public void AddRange(IEnumerable<string> e)
            {
#if (!ISWIN)
                foreach (string ee in e)
                {
                    if (ee != "")
                    {
                        if (!Urls.Contains(ee))
                        {
                            Urls.Add(ee);
                        }
                    }
                }
#else
                foreach (string ee in e.Where(ee => ee != "").Where(ee => !Urls.Contains(ee)))
                {
                    Urls.Add(ee);
                }
#endif
            }
        }

        #endregion
    }
}