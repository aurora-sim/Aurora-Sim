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
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Connectors;
using OpenSim.Services.Interfaces;
using Aurora.Simulation.Base;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.Robust
{
    public class RobustGridServicesConnector : IGridService, IService
    {
        public string Name
        {
            get { return GetType().Name; }
        }

        private GridRegion FixGridRegion(GridRegion gridRegion)
        {
            if (gridRegion == null)
                return null;
            ISceneManager manager = m_registry.RequestModuleInterface<ISceneManager>();
            if (manager != null)
            {
                foreach (IScene scene in manager.GetAllScenes().Where(scene => scene.RegionInfo.RegionID == gridRegion.RegionID))
                {
                    gridRegion.RegionSizeX = scene.RegionInfo.RegionSizeX;
                    gridRegion.RegionSizeY = scene.RegionInfo.RegionSizeY;
                    return gridRegion;
                }
            }
            return gridRegion;
        }

        private List<GridRegion> FixGridRegions (List<GridRegion> list)
        {
            List<GridRegion> rs = new List<GridRegion> ();
            foreach (GridRegion r in list)
            {
                rs.Add (FixGridRegion(r));
            }
            return rs;
        }

        private IRegistryCore m_registry;
        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("GridHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IGridService>(this);
        }


        #region IGridService

        private string GetScopeID(List<UUID> scopeIds)
        {
            if (scopeIds == null || scopeIds.Count == 0)
                return UUID.Zero.ToString();
            return scopeIds[0].ToString();
        }

        public RegisterRegion RegisterRegion(GridRegion regionInfo, UUID oldSessionID, string password)
        {
            Dictionary<string, object> rinfo = regionInfo.ToKVP();
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kvp in rinfo)
                sendData[kvp.Key] = kvp.Value.ToString();

            sendData["SCOPEID"] = UUID.Zero.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "register";

            string reqString = WebUtils.BuildQueryString(sendData);
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
            foreach (string uri in serverURIs)
            {
                // MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: queryString = {0}", reqString);
                try
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", uri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData.ContainsKey("Result") && (replyData["Result"].ToString().ToLower() == "success"))
                        {
                            return new RegisterRegion() { Error = "" };
                        }
                        else if (replyData.ContainsKey("Result") && (replyData["Result"].ToString().ToLower() == "failure"))
                        {
                            MainConsole.Instance.ErrorFormat(
                                "[GRID CONNECTOR]: Registration failed: {0} when contacting {1}", replyData["Message"], uri);

                            return new RegisterRegion() { Error = replyData["Message"].ToString() };
                        }
                        else if (!replyData.ContainsKey("Result"))
                        {
                            MainConsole.Instance.ErrorFormat(
                                "[GRID CONNECTOR]: reply data does not contain result field when contacting {0}", uri);
                        }
                        else
                        {
                            MainConsole.Instance.ErrorFormat(
                                "[GRID CONNECTOR]: unexpected result {0} when contacting {1}", replyData["Result"], uri);

                            return new RegisterRegion() { Error = "Unexpected result " + replyData["Result"].ToString() };
                        }
                    }
                    else
                    {
                        MainConsole.Instance.ErrorFormat(
                            "[GRID CONNECTOR]: RegisterRegion received null reply when contacting grid server at {0}", uri);
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("[GRID CONNECTOR]: Exception when contacting grid server at {0}: {1}", uri, e.Message);
                }
            }

            return new RegisterRegion() { Error = string.Format("Error communicating with the grid service") };
        }

        public bool DeregisterRegion(GridRegion region)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONID"] = region.RegionID.ToString();

            sendData["METHOD"] = "deregister";

            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
            foreach (string uri in serverURIs)
            {
                try
                {
                    string reply
                        = SynchronousRestFormsRequester.MakeRequest("POST", uri, WebUtils.BuildQueryString(sendData));

                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if ((replyData["Result"] != null) && (replyData["Result"].ToString().ToLower() == "success"))
                            return true;
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: DeregisterRegion received null reply");
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server at {0}: {1}", uri, e.Message);
                }
            }

            return false;
        }

        public List<GridRegion> GetNeighbors(List<UUID> scopeIDs, GridRegion region)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = region.ScopeID.ToString();
            sendData["REGIONID"] = region.RegionID.ToString();

            sendData["METHOD"] = "get_neighbours";

            List<GridRegion> rinfos = new List<GridRegion>();

            string reqString = WebUtils.BuildQueryString(sendData);
            string reply = string.Empty;
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
            foreach (string uri in serverURIs)
            {
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST", uri, reqString);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server at {0}: {1}", uri, e.Message);
                    return rinfos;
                }

                Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                if (replyData != null)
                {
                    Dictionary<string, object>.ValueCollection rinfosList = replyData.Values;
                    //MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: get neighbours returned {0} elements", rinfosList.Count);
                    foreach (object r in rinfosList)
                    {
                        if (r is Dictionary<string, object>)
                        {
                            GridRegion rinfo = new GridRegion();
                            rinfo.FromKVP((Dictionary<string, object>)r);
                            rinfos.Add(rinfo);
                        }
                    }
                }
                else
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetNeighbours {0}, {1} received null response",
                        region.ScopeID, region.RegionID);
            }

            return FixGridRegions(rinfos);
        }

        public GridRegion GetRegionByUUID(List<UUID> scopeIDs, UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = GetScopeID(scopeIDs);
            sendData["REGIONID"] = regionID.ToString();

            sendData["METHOD"] = "get_region_by_uuid";

            string reply = string.Empty;
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
            foreach (string uri in serverURIs)
            {
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST", uri, WebUtils.BuildQueryString(sendData));
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server at {0}: {1}", uri, e.Message);
                    return null;
                }

                GridRegion rinfo = null;

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if ((replyData != null) && (replyData["result"] != null))
                    {
                        if (replyData["result"] is Dictionary<string, object>)
                        {
                            rinfo = new GridRegion();
                            rinfo.FromKVP((Dictionary<string, object>)replyData["result"]);
                            return FixGridRegion(rinfo);
                        }
                        //else
                        //    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByUUID {0}, {1} received null response",
                        //        scopeID, regionID);
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByUUID {0} received null response",
                            regionID);
                }
                else
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByUUID received null reply");
            }

            return null;
        }

        public GridRegion GetRegionByPosition(List<UUID> scopeIDs, int x, int y)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = GetScopeID(scopeIDs);
            sendData["X"] = x.ToString();
            sendData["Y"] = y.ToString();

            sendData["METHOD"] = "get_region_by_position";
            string reply = string.Empty;
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
            foreach (string uri in serverURIs)
            {
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                            uri,
                            WebUtils.BuildQueryString(sendData));
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server at {0}: {1}", uri, e.Message);
                    return null;
                }

                GridRegion rinfo = null;
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if ((replyData != null) && (replyData["result"] != null))
                    {
                        if (replyData["result"] is Dictionary<string, object>)
                        {
                            rinfo = new GridRegion();
                            rinfo.FromKVP((Dictionary<string, object>)replyData["result"]);
                            return FixGridRegion(rinfo);
                        }
                        //else
                        //    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByPosition {0}, {1}-{2} received no region",
                        //        scopeID, x, y);
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByPosition {0}-{1} received null response",
                            x, y);
                }
                else
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByPosition received null reply");
            }

            return null;
        }

        public GridRegion GetRegionByName(List<UUID> scopeIDs, string regionName)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = GetScopeID(scopeIDs);
            sendData["NAME"] = regionName;

            sendData["METHOD"] = "get_region_by_name";
            string reply = string.Empty;
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
            foreach (string uri in serverURIs)
            {
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                            uri,
                            WebUtils.BuildQueryString(sendData));
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server at {0}: {1}", uri, e.Message);
                    return null;
                }

                GridRegion rinfo = null;
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if ((replyData != null) && (replyData["result"] != null))
                    {
                        if (replyData["result"] is Dictionary<string, object>)
                        {
                            rinfo = new GridRegion();
                            rinfo.FromKVP((Dictionary<string, object>)replyData["result"]);
                            return FixGridRegion(rinfo);
                        }
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByPosition {0} received null response",
                            regionName);
                }
                else
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionByName received null reply");
            }

            return null;
        }

        public uint GetRegionsByNameCount(List<UUID> scopeIDs, string name)
        {
            return 0;
        }

        public List<GridRegion> GetRegionsByName(List<UUID> scopeIDs, string name, uint? start, uint? count)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = GetScopeID(scopeIDs);
            sendData["NAME"] = name;
            sendData["MAX"] = count == null ? "0" : count.ToString();

            sendData["METHOD"] = "get_regions_by_name";
            List<GridRegion> rinfos = new List<GridRegion>();
            string reply = string.Empty;
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
            foreach (string uri in serverURIs)
            {
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                            uri,
                            WebUtils.BuildQueryString(sendData));
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server at {0}: {1}", uri, e.Message);
                    return rinfos;
                }

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection rinfosList = replyData.Values;
                        foreach (object r in rinfosList)
                        {
                            if (r is Dictionary<string, object>)
                            {
                                GridRegion rinfo = new GridRegion();
                                rinfo.FromKVP((Dictionary<string, object>)r);
                                rinfos.Add(rinfo);
                            }
                        }
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionsByName {0} received null response",
                            name);
                }
                else
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionsByName received null reply");
            }

            return FixGridRegions(rinfos);
        }

        public List<GridRegion> GetRegionRange(List<UUID> scopeIDs, float centerX, float centerY, uint squareRangeFromCenterInMeters)
        {
            return new List<GridRegion>();
        }

        public List<GridRegion> GetRegionRange(List<UUID> scopeIDs, int xmin, int xmax, int ymin, int ymax)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = GetScopeID(scopeIDs);
            sendData["XMIN"] = xmin.ToString();
            sendData["XMAX"] = xmax.ToString();
            sendData["YMIN"] = ymin.ToString();
            sendData["YMAX"] = ymax.ToString();

            sendData["METHOD"] = "get_region_range";

            List<GridRegion> rinfos = new List<GridRegion>();
            string reply = string.Empty;
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
            foreach (string uri in serverURIs)
            {
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                            uri,
                            WebUtils.BuildQueryString(sendData));

                    //MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: reply was {0}", reply);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server at {0}: {1}", uri, e.Message);
                    return rinfos;
                }

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection rinfosList = replyData.Values;
                        foreach (object r in rinfosList)
                        {
                            if (r is Dictionary<string, object>)
                            {
                                GridRegion rinfo = new GridRegion();
                                rinfo.FromKVP((Dictionary<string, object>)r);
                                rinfos.Add(rinfo);
                            }
                        }
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionRange {0}-{1} {2}-{3} received null response",
                            xmin, xmax, ymin, ymax);
                }
                else
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionRange received null reply");
            }

            return FixGridRegions(rinfos);
        }

        public List<GridRegion> GetDefaultRegions(List<UUID> scopeIDs)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = GetScopeID(scopeIDs);
            sendData["METHOD"] = "get_default_regions";

            List<GridRegion> rinfos = new List<GridRegion>();
            string reply = string.Empty;
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
            foreach (string uri in serverURIs)
            {
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                            uri,
                            WebUtils.BuildQueryString(sendData));

                    //MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: reply was {0}", reply);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server at {0}: {1}", uri, e.Message);
                    return rinfos;
                }

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection rinfosList = replyData.Values;
                        foreach (object r in rinfosList)
                        {
                            if (r is Dictionary<string, object>)
                            {
                                GridRegion rinfo = new GridRegion();
                                rinfo.FromKVP((Dictionary<string, object>)r);
                                rinfos.Add(rinfo);
                            }
                        }
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetDefaultRegions received null response");
                }
                else
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetDefaultRegions received null reply");
            }

            return FixGridRegions(rinfos);
        }

        public List<GridRegion> GetFallbackRegions(List<UUID> scopeIDs, int x, int y)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = GetScopeID(scopeIDs);
            sendData["X"] = x.ToString();
            sendData["Y"] = y.ToString();

            sendData["METHOD"] = "get_fallback_regions";

            List<GridRegion> rinfos = new List<GridRegion>();
            string reply = string.Empty;
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
            foreach (string uri in serverURIs)
            {
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                            uri,
                            WebUtils.BuildQueryString(sendData));

                    //MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: reply was {0}", reply);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server at {0}: {1}", uri, e.Message);
                    return rinfos;
                }

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        Dictionary<string, object>.ValueCollection rinfosList = replyData.Values;
                        foreach (object r in rinfosList)
                        {
                            if (r is Dictionary<string, object>)
                            {
                                GridRegion rinfo = new GridRegion();
                                rinfo.FromKVP((Dictionary<string, object>)r);
                                rinfos.Add(rinfo);
                            }
                        }
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetFallbackRegions {0}-{1} received null response",
                            x, y);
                }
                else
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetFallbackRegions received null reply");
            }

            return FixGridRegions(rinfos);
        }

        public int GetRegionFlags(List<UUID> scopeIDs, UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SCOPEID"] = GetScopeID(scopeIDs);
            sendData["REGIONID"] = regionID.ToString();

            sendData["METHOD"] = "get_region_flags";

            string reply = string.Empty;
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
            foreach (string uri in serverURIs)
            {
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                            uri,
                            WebUtils.BuildQueryString(sendData));
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: Exception when contacting grid server at {0}: {1}", uri, e.Message);
                    return -1;
                }

                int flags = -1;

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if ((replyData != null) && replyData.ContainsKey("result") && (replyData["result"] != null))
                    {
                        Int32.TryParse((string)replyData["result"], out flags);
                        return flags;
                        //else
                        //    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionFlags {0}, {1} received wrong type {2}",
                        //        scopeID, regionID, replyData["result"].GetType());
                    }
                    else
                        MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionFlags {0} received null response",
                            regionID);
                }
                else
                    MainConsole.Instance.DebugFormat("[GRID CONNECTOR]: GetRegionFlags received null reply");
            }

            return -1;
        }

        #endregion

        public IGridService InnerService
        {
            get { return this; }
        }

        public int GetMaxRegionSize()
        {
            return 256;
        }

        public int GetRegionViewSize()
        {
            return 1;
        }

        public List<GridRegion> GetSafeRegions(List<UUID> scopeIDs, int x, int y)
        {
            return new List<GridRegion>();
        }

        public string UpdateMap(GridRegion region)
        {
            return "";
        }

        public multipleMapItemReply GetMapItems(List<UUID> scopeIDs, ulong regionHandle, GridItemType gridItemType)
        {
            return null;
        }

        public void SetRegionUnsafe(UUID id)
        {
        }

        public void SetRegionSafe(UUID id)
        {
        }

        public bool VerifyRegionSessionID(GridRegion r, UUID SessionID)
        {
            return true;
        }

        public void Configure(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }
    }
}