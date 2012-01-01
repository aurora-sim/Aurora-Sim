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
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.Connectors
{
    public class AgentInfoConnector : IAgentInfoService, IService
    {
        #region Declares

        protected IRegistryCore m_registry;

        #endregion

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IAgentInfoService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AgentInfoHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IAgentInfoService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        public IAgentInfoService InnerService
        {
            get { return this; }
        }

        public UserInfo GetUserInfo(string userID)
        {
            List<string> urls = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(userID,
                                                                                                       "AgentInfoServerURI");
            foreach (string url in urls)
            {
                try
                {
                    OSDMap request = new OSDMap();
                    request["userID"] = userID;
                    request["Method"] = "GetUserInfo";
                    OSDMap result = WebUtils.PostToService(url, request, true, false);
                    OSD r = OSDParser.DeserializeJson(result["_RawResult"]);
                    if (r is OSDMap)
                    {
                        OSDMap innerresult = (OSDMap) r;
                        UserInfo info = new UserInfo();
                        if (innerresult["Result"].AsString() == "null")
                            return null;
                        info.FromOSD((OSDMap) innerresult["Result"]);
                        return info;
                    }
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        public UserInfo[] GetUserInfos(string[] userIDs)
        {
            List<string> urls =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AgentInfoServerURI");
            List<UserInfo> retVal = new List<UserInfo>();
            foreach (string url in urls)
            {
                OSDMap request = new OSDMap();
                OSDArray requestArray = new OSDArray();
                foreach (string t in userIDs)
                {
                    requestArray.Add(t);
                }
                request["userIDs"] = requestArray;
                request["Method"] = "GetUserInfos";
                OSDMap result = WebUtils.PostToService(url, request, true, false);
                OSD r = OSDParser.DeserializeJson(result["_RawResult"]);
                if (r is OSDMap)
                {
                    OSDMap innerresult = (OSDMap) r;
                    OSDArray resultArray = (OSDArray) innerresult["Result"];
                    foreach (OSD o in resultArray)
                    {
                        UserInfo info = new UserInfo();
                        info.FromOSD((OSDMap) o);
                        retVal.Add(info);
                    }
                }
            }
            return retVal.ToArray();
        }

        public string[] GetAgentsLocations(string requestor, string[] userIDs)
        {
            List<string> urls =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AgentInfoServerURI");
            List<string> retVal = new List<string>(userIDs.Length);
            foreach (string url in urls)
            {
                OSDMap request = new OSDMap();
                OSDArray requestArray = new OSDArray();
                foreach (string t in userIDs)
                    requestArray.Add(t);

                request["userIDs"] = requestArray;
                request["requestor"] = requestor;
                request["Method"] = "GetAgentsLocations";
                OSDMap result = WebUtils.PostToService(url, request, true, false);
                try
                {
                    OSD r = OSDParser.DeserializeJson(result["_RawResult"]);
                    if (r is OSDMap)
                    {
                        OSDMap innerresult = (OSDMap) r;
                        OSDArray resultArray = (OSDArray) innerresult["Result"];
#if (!ISWIN)
                        foreach (OSD o in resultArray)
                            retVal.Add(o.AsString());
#else
                        retVal.AddRange(resultArray.Select(o => o.AsString()));
#endif  
                    }
                }
                catch
                {
                    //Bad request, just leave it
                }
            }
            return retVal.ToArray();
        }

        public bool SetHomePosition(string userID, UUID homeID, Vector3 homePosition, Vector3 homeLookAt)
        {
            return false;
        }

        public void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
        }

        public void SetLoggedIn(string userID, bool loggingIn, bool fireLoggedInEvent, UUID enteringRegion)
        {
        }

        public void LockLoggedInStatus(string userID, bool locked)
        {
        }

        #endregion
    }
}