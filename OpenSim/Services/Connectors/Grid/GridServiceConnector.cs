/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.Connectors
{
    public class GridServicesConnector : IGridService, IService
    {
        protected IRegistryCore m_registry;

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        #region IGridService Members

        public virtual IGridService InnerService
        {
            get { return this; }
        }

        public int MaxRegionSize
        {
            get { return 0; }
        }

        public int RegionViewSize
        {
            get { return 256; }
        }

        public void Configure(IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual void FinishedStartup()
        {
        }

        #endregion

        #region IGridService

        public virtual string RegisterRegion(GridRegion regionInfo, UUID SecureSessionID, out UUID SessionID,
                                             out List<GridRegion> neighbors)
        {
            neighbors = new List<GridRegion>();
            OSDMap map = new OSDMap();
            map["Region"] = regionInfo.ToOSD();
            map["SecureSessionID"] = SecureSessionID;
            map["Method"] = "Register";

            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RegistrationURI");
            foreach (string mServerUri in serverURIs)
            {
                OSDMap result = WebUtils.PostToService(mServerUri + "/grid", map, true, false);
                if (result["Success"].AsBoolean())
                {
                    try
                    {
                        OSD r = OSDParser.DeserializeJson(result["_RawResult"]);
                        if (r is OSDMap)
                        {
                            OSDMap innerresult = (OSDMap) r;
                            if (innerresult["Result"].AsString() == "")
                            {
                                object[] o = new object[2];
                                o[0] = regionInfo;
                                o[1] = innerresult;
                                SessionID = innerresult["SecureSessionID"].AsUUID();
                                m_registry.RequestModuleInterface<IConfigurationService>().AddNewUrls(regionInfo.RegionHandle.ToString(), (OSDMap) innerresult["URLs"]);

                                OSDArray array = (OSDArray) innerresult["Neighbors"];
                                foreach (OSD ar in array)
                                {
                                    GridRegion n = new GridRegion();
                                    n.FromOSD((OSDMap) ar);
                                    neighbors.Add(n);
                                }
                                m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("GridRegionRegistered", o);
                                return "";
                            }
                            else
                            {
                                SessionID = UUID.Zero;
                                return innerresult["Result"].AsString();
                            }
                        }
                    }
                    catch (Exception) //JsonException
                    {
                        MainConsole.Instance.Warn("[GridServiceConnector]: Exception on parsing OSDMap from server, legacy (OpenSim) server?");
                    }
                }
            }
            SessionID = SecureSessionID;
            return OldRegisterRegion(regionInfo);
        }

        public virtual string UpdateMap(GridRegion regionInfo, UUID SecureSessionID)
        {
            OSDMap map = new OSDMap();
            map["Region"] = regionInfo.ToOSD();
            map["SecureSessionID"] = SecureSessionID;
            map["Method"] = "UpdateMap";

            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
#if (!ISWIN)
            foreach (string mServerUri in serverURIs)
            {
                OSDMap result = WebUtils.PostToService(mServerUri, map, true, true);
                if (result["Success"].AsBoolean())
                {
                    try
                    {
                        OSDMap innerresult = (OSDMap) result["_Result"];
                        return innerresult["Result"].AsString();
                    }
                    catch
                    {
                    }
                }
            }
#else
            foreach (OSDMap result in serverURIs.Select(m_ServerURI => WebUtils.PostToService(m_ServerURI, map, true, true)).Where(result => result["Success"].AsBoolean()))
            {
                try
                {
                    OSDMap innerresult = (OSDMap) result["_Result"];
                    return innerresult["Result"].AsString();
                }
                catch
                {
                }
            }
#endif
            return "Error communicating with grid service";
        }

        public virtual bool DeregisterRegion(ulong regionhandle, UUID regionID, UUID SessionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONID"] = regionID.ToString();
            sendData["SESSIONID"] = SessionID.ToString();

            sendData["METHOD"] = "deregister";

            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(regionhandle.ToString(),
                                                                                           "GridServerURI");
                foreach (string mServerUri in serverURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, WebUtils.BuildQueryString(sendData));
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if ((replyData["Result"] != null) && (replyData["Result"].ToString().ToLower() == "success"))
                            return true;
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: DeregisterRegion received null reply");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server: {0}", e.Message);
            }

            return false;
        }

        public virtual GridRegion GetRegionByUUID(UUID scopeID, UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = scopeID.ToString();
            sendData["REGIONID"] = regionID.ToString();

            sendData["METHOD"] = "get_region_by_uuid";

            string reply = string.Empty;

            GridRegion rinfo = null;
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      WebUtils.BuildQueryString(sendData));
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if ((replyData != null) && (replyData.ContainsKey("result") && replyData["result"] != null))
                        {
                            if (replyData["result"] is Dictionary<string, object>)
                            {
                                rinfo = new GridRegion((Dictionary<string, object>) replyData["result"]);
                                rinfo.GenericMap["URL"] = m_ServerURI;
                            }
                            //else
                            //    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByUUID {0}, {1} received null response",
                            //        scopeID, regionID);
                        }
                        else
                            MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByUUID {0}, {1} received null response: {2}",
                                              scopeID, regionID, reply);
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByUUID received null reply");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server: {0}", e.Message);
                return null;
            }

            return rinfo;
        }

        public virtual GridRegion GetRegionByPosition(UUID scopeID, int x, int y)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = scopeID.ToString();
            sendData["X"] = x.ToString();
            sendData["Y"] = y.ToString();

            sendData["METHOD"] = "get_region_by_position";
            string reply = string.Empty;
            GridRegion rinfo = null;
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      WebUtils.BuildQueryString(sendData));

                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if ((replyData != null) && (replyData["result"] != null))
                        {
                            if (replyData["result"] is Dictionary<string, object>)
                            {
                                rinfo = new GridRegion((Dictionary<string, object>) replyData["result"]);
                                rinfo.GenericMap["URL"] = m_ServerURI;
                            }
                            //else
                            //    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByPosition {0}, {1}-{2} received no region",
                            //        scopeID, x, y);
                        }
                        else
                            MainConsole.Instance.DebugFormat(
                                "[GRID CONNECTOR]: GetRegionByPosition {0}, {1}-{2} received null response",
                                scopeID, x, y);
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByPosition received null reply");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server: {0}", e.Message);
                return null;
            }

            return rinfo;
        }

        public virtual GridRegion GetRegionByName(UUID scopeID, string regionName)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = scopeID.ToString();
            sendData["NAME"] = regionName;

            sendData["METHOD"] = "get_region_by_name";
            GridRegion rinfo = null;
            string reply = string.Empty;
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      WebUtils.BuildQueryString(sendData));
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if ((replyData != null) && (replyData["result"] != null))
                        {
                            if (replyData["result"] is Dictionary<string, object>)
                            {
                                rinfo = new GridRegion((Dictionary<string, object>) replyData["result"]);
                                rinfo.GenericMap["URL"] = m_ServerURI;
                            }
                        }
                        else
                            MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByPosition {0}, {1} received null response",
                                              scopeID, regionName);
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByName received null reply");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server: {0}", e.Message);
                return null;
            }

            return rinfo;
        }

        public virtual List<GridRegion> GetNeighbors(GridRegion r)
        {
            return new List<GridRegion>();
        }

        public virtual List<GridRegion> GetRegionsByName(UUID scopeID, string name, int maxNumber)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = scopeID.ToString();
            sendData["NAME"] = name;
            sendData["MAX"] = maxNumber.ToString();

            sendData["METHOD"] = "get_regions_by_name";
            List<GridRegion> rinfos = new List<GridRegion>();
            string reply = string.Empty;
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      WebUtils.BuildQueryString(sendData));
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection rinfosList = replyData.Values;
#if (!ISWIN)
                            foreach (object o in rinfosList)
                            {
                                Dictionary<string, object> r = o as Dictionary<string, object>;
                                if (r != null)
                                {
                                    GridRegion rinfo = new GridRegion(r);
                                    rinfo.GenericMap["URL"] = m_ServerURI;
                                    rinfos.Add(rinfo);
                                }
                            }
#else
                            foreach (GridRegion rinfo in rinfosList.OfType<Dictionary<string, object>>().Select(r => new GridRegion(r)))
                            {
                                rinfo.GenericMap["URL"] = m_ServerURI;
                                rinfos.Add(rinfo);
                            }
#endif
                        }
                        else
                            MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionsByName {0}, {1}, {2} received null response",
                                              scopeID, name, maxNumber);
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionsByName received null reply");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server: {0}", e.Message);
                return rinfos;
            }

            return rinfos;
        }

        public virtual List<GridRegion> GetRegionRange(UUID scopeID, int xmin, int xmax, int ymin, int ymax)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = scopeID.ToString();
            sendData["XMIN"] = xmin.ToString();
            sendData["XMAX"] = xmax.ToString();
            sendData["YMIN"] = ymin.ToString();
            sendData["YMAX"] = ymax.ToString();

            sendData["METHOD"] = "get_region_range";

            List<GridRegion> rinfos = new List<GridRegion>();
            string reply = string.Empty;
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      WebUtils.BuildQueryString(sendData));

                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            Dictionary<string, object>.ValueCollection rinfosList = replyData.Values;
#if (!ISWIN)
                            foreach (object o in rinfosList)
                            {
                                Dictionary<string, object> r = o as Dictionary<string, object>;
                                if (r != null)
                                {
                                    GridRegion rinfo = new GridRegion(r);
                                    rinfo.GenericMap["URL"] = m_ServerURI;
                                    rinfos.Add(rinfo);
                                }
                            }
#else
                            foreach (GridRegion rinfo in rinfosList.OfType<Dictionary<string, object>>().Select(r => new GridRegion(r)))
                            {
                                rinfo.GenericMap["URL"] = m_ServerURI;
                                rinfos.Add(rinfo);
                            }
#endif
                        }
                        else
                            MainConsole.Instance.DebugFormat(
                                "[GRID CONNECTOR]: GetRegionRange {0}, {1}-{2} {3}-{4} received null response",
                                scopeID, xmin, xmax, ymin, ymax);
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionRange received null reply");

                    //MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: reply was {0}", reply);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server: {0}", e.Message);
                return rinfos;
            }

            return rinfos;
        }

        public List<GridRegion> GetDefaultRegions(UUID scopeID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = scopeID.ToString();

            sendData["METHOD"] = "get_default_regions";

            List<GridRegion> rinfos = new List<GridRegion>();
            string reply = string.Empty;
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      WebUtils.BuildQueryString(sendData));

                    //MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: reply was {0}", reply);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server: {0}", e.Message);
                return rinfos;
            }

            if (reply != string.Empty)
            {
                Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                if (replyData != null)
                {
                    Dictionary<string, object>.ValueCollection rinfosList = replyData.Values;
#if (!ISWIN)
                    foreach (object r in rinfosList)
                    {
                        if (r is Dictionary<string, object>)
                        {
                            GridRegion rinfo = new GridRegion((Dictionary<string, object>)r);
                            rinfos.Add(rinfo);
                        }
                    }
#else
                    rinfos.AddRange(rinfosList.OfType<Dictionary<string, object>>().Select(r => new GridRegion(r)));
#endif
                }
                else
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetDefaultRegions {0} received null response",
                                      scopeID);
            }
            else
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetDefaultRegions received null reply");

            return rinfos;
        }

        public List<GridRegion> GetFallbackRegions(UUID scopeID, int x, int y)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = scopeID.ToString();
            sendData["X"] = x.ToString();
            sendData["Y"] = y.ToString();

            sendData["METHOD"] = "get_fallback_regions";

            List<GridRegion> rinfos = new List<GridRegion>();
            string reply = string.Empty;
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      WebUtils.BuildQueryString(sendData));

                    //MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: reply was {0}", reply);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server: {0}", e.Message);
                return rinfos;
            }

            if (reply != string.Empty)
            {
                Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                if (replyData != null)
                {
                    Dictionary<string, object>.ValueCollection rinfosList = replyData.Values;
#if (!ISWIN)
                    foreach (object r in rinfosList)
                    {
                        if (r is Dictionary<string, object>)
                        {
                            GridRegion rinfo = new GridRegion((Dictionary<string, object>)r);
                            rinfos.Add(rinfo);
                        }
                    }
#else
                    rinfos.AddRange(rinfosList.OfType<Dictionary<string, object>>().Select(r => new GridRegion(r)));
#endif
                }
                else
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetFallbackRegions {0}, {1}-{2} received null response",
                                      scopeID, x, y);
            }
            else
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetFallbackRegions received null reply");

            return rinfos;
        }

        public List<GridRegion> GetSafeRegions(UUID scopeID, int x, int y)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = scopeID.ToString();
            sendData["X"] = x.ToString();
            sendData["Y"] = y.ToString();

            sendData["METHOD"] = "get_safe_regions";

            List<GridRegion> rinfos = new List<GridRegion>();
            string reply = string.Empty;
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      WebUtils.BuildQueryString(sendData));

                    //MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: reply was {0}", reply);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server: {0}", e.Message);
                return rinfos;
            }

            if (reply != string.Empty)
            {
                Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                if (replyData != null)
                {
                    Dictionary<string, object>.ValueCollection rinfosList = replyData.Values;
#if (!ISWIN)
                    foreach (object r in rinfosList)
                    {
                        if (r is Dictionary<string, object>)
                        {
                            GridRegion rinfo = new GridRegion((Dictionary<string, object>)r);
                            rinfos.Add(rinfo);
                        }
                    }
#else
                    rinfos.AddRange(rinfosList.OfType<Dictionary<string, object>>().Select(r => new GridRegion(r)));
#endif

                }
                else
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetSafeRegions {0}, {1}-{2} received null response",
                                      scopeID, x, y);
            }
            else
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetSafeRegions received null reply");

            return rinfos;
        }

        public virtual int GetRegionFlags(UUID scopeID, UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = scopeID.ToString();
            sendData["REGIONID"] = regionID.ToString();

            sendData["METHOD"] = "get_region_flags";

            int flags = -1;

            string reply = string.Empty;
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      WebUtils.BuildQueryString(sendData));
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if ((replyData != null) && replyData.ContainsKey("result") && (replyData["result"] != null))
                        {
                            Int32.TryParse((string) replyData["result"], out flags);
                            //else
                            //    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionFlags {0}, {1} received wrong type {2}",
                            //        scopeID, regionID, replyData["result"].GetType());
                        }
                        else
                            MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionFlags {0}, {1} received null response",
                                              scopeID, regionID);
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionFlags received null reply");
                    if (flags != -1)
                        return flags;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server: {0}", e.Message);
                return -1;
            }

            return flags;
        }

        public multipleMapItemReply GetMapItems(ulong regionHandle, GridItemType gridItemType)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONHANDLE"] = regionHandle;
            sendData["GRIDITEMTYPE"] = (int) gridItemType;
            sendData["METHOD"] = "getmapitems";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in serverURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                 m_ServerURI,
                                                                                                                                 reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
                    if (replyData != null)
                    {
                        multipleMapItemReply items = new multipleMapItemReply();
                        if (replyData.ContainsKey("Result") &&
                            (replyData["Result"].ToString().ToLower() == "failure"))
                            return items;

                        items = new multipleMapItemReply((replyData["MapItems"]) as Dictionary<string, object>);

                        // Success
                        return items;
                    }

                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetMapItems {0} received null response",
                                          regionHandle);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public bool VerifyRegionSessionID(GridRegion r, UUID SessionID)
        {
            return r.SessionID == SessionID;
        }

        public virtual void SetRegionUnsafe(UUID regionID)
        {
        }

        public virtual void SetRegionSafe(UUID regionID)
        {
        }

        public virtual string OldRegisterRegion(GridRegion region)
        {
            Dictionary<string, object> rinfo = region.ToKVP();
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kvp in rinfo)
                sendData[kvp.Key] = kvp.Value;

            sendData["SCOPEID"] = region.ScopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "register";

            string reqString = WebUtils.BuildQueryString(sendData);
            // MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: queryString = {0}", reqString);
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string mServerUri in serverURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData.ContainsKey("Result") && (replyData["Result"].ToString().ToLower() == "success"))
                        {
                            return String.Empty;
                        }
                        else if (replyData.ContainsKey("Result") && (replyData["Result"].ToString().ToLower() == "failure"))
                        {
                            MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Registration failed: {0}", replyData["Message"]);
                            return replyData["Message"].ToString();
                        }
                        else if (!replyData.ContainsKey("Result"))
                        {
                            MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: reply data does not contain result field");
                        }
                        else
                        {
                            MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: unexpected result {0}", replyData["Result"]);
                            return "Unexpected result " + replyData["Result"];
                        }
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: RegisterRegion received null reply");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server: {0}", e.Message);
            }

            return "Error communicating with grid service";
        }

        #endregion

        #region IService Members

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("GridHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IGridService>(this);
        }

        #endregion
    }
}