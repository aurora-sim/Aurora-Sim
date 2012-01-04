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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace OpenSim.Services
{
    public class GridServerPostHandler : BaseStreamHandler
    {
        private readonly IRegionConnector TelehubConnector;
        private readonly IGridService m_GridService;
        private readonly string m_SessionID;
        private readonly IRegistryCore m_registry;
        private readonly bool m_secure = true;

        public GridServerPostHandler(string url, IRegistryCore registry, IGridService service, bool secure,
                                     string SessionID) :
                                         base("POST", url)
        {
            m_secure = secure;
            m_GridService = service;
            m_registry = registry;
            m_SessionID = SessionID;
            TelehubConnector = DataManager.RequestPlugin<IRegionConnector>();
        }

        public override byte[] Handle(string path, Stream requestData,
                                      OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            //MainConsole.Instance.DebugFormat("[XXX]: query String: {0}", body);

            try
            {
                Dictionary<string, object> request =
                    WebUtils.ParseQueryString(body);

                if (!request.ContainsKey("METHOD"))
                {
                    //OSD map test
                    OSDMap map = WebUtils.GetOSDMap(body);
                    if (map != null)
                    {
                        if (map.ContainsKey("Method"))
                        {
                            if (map["Method"] == "Register")
                                return NewRegister(map);
                            else if (!m_secure)
                                return null;
                            if (map["Method"] == "UpdateMap")
                                return UpdateMap(map);
                        }
                    }
                    else
                        return FailureResult();
                }
                if (!m_secure)
                    return null;

                string method = request["METHOD"].ToString();

                IGridRegistrationService urlModule =
                    m_registry.RequestModuleInterface<IGridRegistrationService>();
                switch (method)
                {
                    case "register":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.None))
                                return FailureResult();
                        return Register(request);

                    case "deregister":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.None))
                                return FailureResult();
                        return Deregister(request);

                    case "get_region_by_uuid":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GetRegionByUUID(request);

                    case "get_region_by_position":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GetRegionByPosition(request);

                    case "get_region_by_name":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GetRegionByName(request);

                    case "get_regions_by_name":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GetRegionsByName(request);

                    case "get_region_range":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GetRegionRange(request);

                    case "get_default_regions":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GetDefaultRegions(request);

                    case "get_fallback_regions":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GetFallbackRegions(request);

                    case "get_safe_regions":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return GetSafeRegions(request);

                    case "get_region_flags":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GetRegionFlags(request);

                    case "getmapitems":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return GetMapItems(request);

                    case "removetelehub":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return RemoveTelehub(request);

                    case "addtelehub":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return AddTelehub(request);

                    case "findtelehub":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return FindTelehub(request);
                }
                MainConsole.Instance.DebugFormat("[GRID HANDLER]: unknown method {0} request {1}", method.Length, method);
            }
            catch (Exception e)
            {
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: Exception {0}", e);
            }

            return FailureResult();
        }

        #region Method-specific handlers

        private byte[] Register(Dictionary<string, object> request)
        {
            int versionNumberMin = 0, versionNumberMax = 0;
            if (request.ContainsKey("VERSIONMIN"))
                Int32.TryParse(request["VERSIONMIN"].ToString(), out versionNumberMin);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no minimum protocol version in request to register region");

            if (request.ContainsKey("VERSIONMAX"))
                Int32.TryParse(request["VERSIONMAX"].ToString(), out versionNumberMax);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no maximum protocol version in request to register region");

            UUID sessionIDIn = UUID.Zero;
            if (request.ContainsKey("SESSIONID"))
                UUID.TryParse(request["SESSIONID"].ToString(), out sessionIDIn);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no sessionID in request to register region");

            // Check the protocol version
            if ((versionNumberMin > ProtocolVersions.ServerProtocolVersionMax &&
                 versionNumberMax < ProtocolVersions.ServerProtocolVersionMax))
            {
                // Can't do, there is no overlap in the acceptable ranges
                return FailureResult();
            }

            Dictionary<string, object> rinfoData = new Dictionary<string, object>();
            GridRegion rinfo = null;
            try
            {
                foreach (KeyValuePair<string, object> kvp in request)
                    rinfoData[kvp.Key] = kvp.Value.ToString();
                rinfo = new GridRegion(rinfoData);
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[GRID HANDLER]: exception unpacking region data: {0}", e);
            }

            string result = "Error communicating with grid service";

            UUID SessionID = UUID.Zero;
            List<GridRegion> neighbors;
            if (rinfo != null)
                result = m_GridService.RegisterRegion(rinfo, sessionIDIn, out SessionID, out neighbors);

            if (result == String.Empty)
                return SuccessResult(SessionID.ToString());
            else
                return FailureResult(result);
        }

        private byte[] NewRegister(OSDMap request)
        {
            GridRegion rinfo = new GridRegion();
            rinfo.FromOSD((OSDMap) request["Region"]);
            UUID SecureSessionID = request["SecureSessionID"].AsUUID();
            string result = "";
            List<GridRegion> neighbors = new List<GridRegion>();
            if (rinfo != null)
                result = m_GridService.RegisterRegion(rinfo, SecureSessionID, out SecureSessionID, out neighbors);

            OSDMap resultMap = new OSDMap();
            resultMap["SecureSessionID"] = SecureSessionID;
            resultMap["Result"] = result;

            if (result == "")
            {
                object[] o = new object[3] {resultMap, SecureSessionID, rinfo};
                m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler(
                    "GridRegionSuccessfullyRegistered", o);

                //Send the neighbors as well
                OSDArray array = new OSDArray();
                foreach (GridRegion r in neighbors)
                {
                    array.Add(r.ToOSD());
                }
                resultMap["Neighbors"] = array;
            }

            return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(resultMap));
        }

        private byte[] UpdateMap(OSDMap request)
        {
            GridRegion rinfo = new GridRegion();
            rinfo.FromOSD((OSDMap) request["Region"]);
            UUID SecureSessionID = request["SecureSessionID"].AsUUID();
            string result = "";
            if (rinfo != null)
                result = m_GridService.UpdateMap(rinfo, SecureSessionID);

            OSDMap resultMap = new OSDMap();
            resultMap["Result"] = result;

            return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(OSD.FromBoolean(true)));
        }

        private byte[] Deregister(Dictionary<string, object> request)
        {
            UUID regionID = UUID.Zero;
            if (request.ContainsKey("REGIONID"))
                UUID.TryParse(request["REGIONID"].ToString(), out regionID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no regionID in request to deregister region");

            UUID sessionID = UUID.Zero;
            if (request.ContainsKey("SESSIONID"))
                UUID.TryParse(request["SESSIONID"].ToString(), out sessionID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no sessionID in request to deregister region");
            GridRegion r = m_GridService.GetRegionByUUID(UUID.Zero, regionID);
            if (r != null & m_SessionID == r.RegionHandle.ToString())
            {
                bool result = m_GridService.DeregisterRegion(r.RegionHandle, regionID, sessionID);
                if (result)
                    m_registry.RequestModuleInterface<IGridRegistrationService>().RemoveUrlsForClient(
                        sessionID.ToString());
                if (result)
                    return SuccessResult();
                else
                    return FailureResult();
            }
            return FailureResult();
        }

        private byte[] GetRegionByUUID(Dictionary<string, object> request)
        {
            UUID scopeID = UUID.Zero;
            if (request.ContainsKey("SCOPEID"))
                UUID.TryParse(request["SCOPEID"].ToString(), out scopeID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no scopeID in request to get neighbours");

            UUID regionID = UUID.Zero;
            if (request.ContainsKey("REGIONID"))
                UUID.TryParse(request["REGIONID"].ToString(), out regionID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no regionID in request to get neighbours");

            GridRegion rinfo = m_GridService.GetRegionByUUID(scopeID, regionID);
            rinfo = CleanRegion(rinfo);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: neighbours for region {0}: {1}", regionID, rinfos.Count);

            Dictionary<string, object> result = new Dictionary<string, object>();
            if (rinfo == null)
                result["result"] = "null";
            else
                result["result"] = rinfo.ToKVP();

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetRegionByPosition(Dictionary<string, object> request)
        {
            UUID scopeID = UUID.Zero;
            if (request.ContainsKey("SCOPEID"))
                UUID.TryParse(request["SCOPEID"].ToString(), out scopeID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no scopeID in request to get region by position");

            int x = 0, y = 0;
            if (request.ContainsKey("X"))
                Int32.TryParse(request["X"].ToString(), out x);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no X in request to get region by position");
            if (request.ContainsKey("Y"))
                Int32.TryParse(request["Y"].ToString(), out y);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no Y in request to get region by position");

            GridRegion rinfo = m_GridService.GetRegionByPosition(scopeID, x, y);
            rinfo = CleanRegion(rinfo);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: neighbours for region {0}: {1}", regionID, rinfos.Count);

            Dictionary<string, object> result = new Dictionary<string, object>();
            if (rinfo == null)
                result["result"] = "null";
            else
                result["result"] = rinfo.ToKVP();

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetRegionByName(Dictionary<string, object> request)
        {
            UUID scopeID = UUID.Zero;
            if (request.ContainsKey("SCOPEID"))
                UUID.TryParse(request["SCOPEID"].ToString(), out scopeID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no scopeID in request to get region by name");

            string regionName = string.Empty;
            if (request.ContainsKey("NAME"))
                regionName = request["NAME"].ToString();
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no name in request to get region by name");

            GridRegion rinfo = m_GridService.GetRegionByName(scopeID, regionName);
            rinfo = CleanRegion(rinfo);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: neighbours for region {0}: {1}", regionID, rinfos.Count);

            Dictionary<string, object> result = new Dictionary<string, object>();
            if (rinfo == null)
                result["result"] = "null";
            else
                result["result"] = rinfo.ToKVP();

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetRegionsByName(Dictionary<string, object> request)
        {
            UUID scopeID = UUID.Zero;
            if (request.ContainsKey("SCOPEID"))
                UUID.TryParse(request["SCOPEID"].ToString(), out scopeID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no scopeID in request to get regions by name");

            string regionName = string.Empty;
            if (request.ContainsKey("NAME"))
                regionName = request["NAME"].ToString();
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no NAME in request to get regions by name");

            int max = 0;
            if (request.ContainsKey("MAX"))
                Int32.TryParse(request["MAX"].ToString(), out max);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no MAX in request to get regions by name");

            List<GridRegion> rinfos = m_GridService.GetRegionsByName(scopeID, regionName, max);
            rinfos = CleanRegions(rinfos);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: neighbours for region {0}: {1}", regionID, rinfos.Count);

            Dictionary<string, object> result = new Dictionary<string, object>();
            if ((rinfos == null) || ((rinfos != null) && (rinfos.Count == 0)))
                result["result"] = "null";
            else
            {
                int i = 0;
#if (!ISWIN)
                foreach (GridRegion rinfo in rinfos)
                {
                    Dictionary<string, object> rinfoDict = rinfo.ToKVP();
                    result["region" + i] = rinfoDict;
                    i++;
                }
#else
                foreach (Dictionary<string, object> rinfoDict in rinfos.Select(rinfo => rinfo.ToKVP()))
                {
                    result["region" + i] = rinfoDict;
                    i++;
                }
#endif
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetRegionRange(Dictionary<string, object> request)
        {
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: GetRegionRange");
            UUID scopeID = UUID.Zero;
            if (request.ContainsKey("SCOPEID"))
                UUID.TryParse(request["SCOPEID"].ToString(), out scopeID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no scopeID in request to get region range");

            int xmin = 0, xmax = 0, ymin = 0, ymax = 0;
            if (request.ContainsKey("XMIN"))
                Int32.TryParse(request["XMIN"].ToString(), out xmin);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no XMIN in request to get region range");
            if (request.ContainsKey("XMAX"))
                Int32.TryParse(request["XMAX"].ToString(), out xmax);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no XMAX in request to get region range");
            if (request.ContainsKey("YMIN"))
                Int32.TryParse(request["YMIN"].ToString(), out ymin);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no YMIN in request to get region range");
            if (request.ContainsKey("YMAX"))
                Int32.TryParse(request["YMAX"].ToString(), out ymax);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no YMAX in request to get region range");


            List<GridRegion> rinfos = m_GridService.GetRegionRange(scopeID, xmin, xmax, ymin, ymax);
            rinfos = CleanRegions(rinfos);

            Dictionary<string, object> result = new Dictionary<string, object>();
            if ((rinfos == null) || ((rinfos != null) && (rinfos.Count == 0)))
                result["result"] = "null";
            else
            {
                int i = 0;
#if (!ISWIN)
                foreach (GridRegion rinfo in rinfos)
                {
                    Dictionary<string, object> rinfoDict = rinfo.ToKVP();
                    result["region" + i] = rinfoDict;
                    i++;
                }
#else
                foreach (Dictionary<string, object> rinfoDict in rinfos.Select(rinfo => rinfo.ToKVP()))
                {
                    result["region" + i] = rinfoDict;
                    i++;
                }
#endif
            }
            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetDefaultRegions(Dictionary<string, object> request)
        {
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: GetDefaultRegions");
            UUID scopeID = UUID.Zero;
            if (request.ContainsKey("SCOPEID"))
                UUID.TryParse(request["SCOPEID"].ToString(), out scopeID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no scopeID in request to get region range");

            List<GridRegion> rinfos = m_GridService.GetDefaultRegions(scopeID);
            rinfos = CleanRegions(rinfos);

            Dictionary<string, object> result = new Dictionary<string, object>();
            if ((rinfos == null) || ((rinfos != null) && (rinfos.Count == 0)))
                result["result"] = "null";
            else
            {
                int i = 0;
#if (!ISWIN)
                foreach (GridRegion rinfo in rinfos)
                {
                    Dictionary<string, object> rinfoDict = rinfo.ToKVP();
                    result["region" + i] = rinfoDict;
                    i++;
                }
#else
                foreach (Dictionary<string, object> rinfoDict in rinfos.Select(rinfo => rinfo.ToKVP()))
                {
                    result["region" + i] = rinfoDict;
                    i++;
                }
#endif
            }
            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetFallbackRegions(Dictionary<string, object> request)
        {
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: GetRegionRange");
            UUID scopeID = UUID.Zero;
            if (request.ContainsKey("SCOPEID"))
                UUID.TryParse(request["SCOPEID"].ToString(), out scopeID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no scopeID in request to get fallback regions");

            int x = 0, y = 0;
            if (request.ContainsKey("X"))
                Int32.TryParse(request["X"].ToString(), out x);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no X in request to get fallback regions");
            if (request.ContainsKey("Y"))
                Int32.TryParse(request["Y"].ToString(), out y);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no Y in request to get fallback regions");


            List<GridRegion> rinfos = m_GridService.GetFallbackRegions(scopeID, x, y);
            rinfos = CleanRegions(rinfos);

            Dictionary<string, object> result = new Dictionary<string, object>();
            if ((rinfos == null) || ((rinfos != null) && (rinfos.Count == 0)))
                result["result"] = "null";
            else
            {
                int i = 0;
#if (!ISWIN)
                foreach (GridRegion rinfo in rinfos)
                {
                    Dictionary<string, object> rinfoDict = rinfo.ToKVP();
                    result["region" + i] = rinfoDict;
                    i++;
                }
#else
                foreach (Dictionary<string, object> rinfoDict in rinfos.Select(rinfo => rinfo.ToKVP()))
                {
                    result["region" + i] = rinfoDict;
                    i++;
                }
#endif
            }
            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetSafeRegions(Dictionary<string, object> request)
        {
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: GetRegionRange");
            UUID scopeID = UUID.Zero;
            if (request.ContainsKey("SCOPEID"))
                UUID.TryParse(request["SCOPEID"].ToString(), out scopeID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no scopeID in request to get fallback regions");

            int x = 0, y = 0;
            if (request.ContainsKey("X"))
                Int32.TryParse(request["X"].ToString(), out x);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no X in request to get fallback regions");
            if (request.ContainsKey("Y"))
                Int32.TryParse(request["Y"].ToString(), out y);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no Y in request to get fallback regions");


            List<GridRegion> rinfos = m_GridService.GetSafeRegions(scopeID, x, y);
            rinfos = CleanRegions(rinfos);

            Dictionary<string, object> result = new Dictionary<string, object>();
            if ((rinfos == null) || ((rinfos != null) && (rinfos.Count == 0)))
                result["result"] = "null";
            else
            {
                int i = 0;
#if (!ISWIN)
                foreach (GridRegion rinfo in rinfos)
                {
                    Dictionary<string, object> rinfoDict = rinfo.ToKVP();
                    result["region" + i] = rinfoDict;
                    i++;
                }
#else
                foreach (Dictionary<string, object> rinfoDict in rinfos.Select(rinfo => rinfo.ToKVP()))
                {
                    result["region" + i] = rinfoDict;
                    i++;
                }
#endif
            }
            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetRegionFlags(Dictionary<string, object> request)
        {
            UUID scopeID = UUID.Zero;
            if (request.ContainsKey("SCOPEID"))
                UUID.TryParse(request["SCOPEID"].ToString(), out scopeID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no scopeID in request to get neighbours");

            UUID regionID = UUID.Zero;
            if (request.ContainsKey("REGIONID"))
                UUID.TryParse(request["REGIONID"].ToString(), out regionID);
            else
                MainConsole.Instance.WarnFormat("[GRID HANDLER]: no regionID in request to get neighbours");

            int flags = m_GridService.GetRegionFlags(scopeID, regionID);
            // MainConsole.Instance.DebugFormat("[GRID HANDLER]: flags for region {0}: {1}", regionID, flags);

            Dictionary<string, object> result = new Dictionary<string, object>();
            result["result"] = flags.ToString();

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[GRID HANDLER]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetMapItems(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            ulong regionHandle = ulong.Parse(request["REGIONHANDLE"].ToString());
            GridItemType gridItemType = (GridItemType) int.Parse(request["GRIDITEMTYPE"].ToString());

            multipleMapItemReply items = m_GridService.GetMapItems(regionHandle, gridItemType);

            result["MapItems"] = items.ToKeyValuePairs();

            string xmlString = WebUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] RemoveTelehub(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            int RegionX, RegionY;
            ulong handle;
            if (ulong.TryParse(m_SessionID, out handle))
            {
                Util.UlongToInts(handle, out RegionX, out RegionY);
                GridRegion r = m_GridService.GetRegionByPosition(UUID.Zero, RegionX, RegionY);
                if (r != null)
                    TelehubConnector.RemoveTelehub(r.RegionID, 0);
                result["result"] = "Successful";
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] AddTelehub(Dictionary<string, object> request)
        {
            Telehub telehub = new Telehub();
            telehub.FromKVP(request);

            int RegionX, RegionY;
            ulong handle;
            if (ulong.TryParse(m_SessionID, out handle))
            {
                Util.UlongToInts(handle, out RegionX, out RegionY);
                GridRegion r = m_GridService.GetRegionByPosition(UUID.Zero, RegionX, RegionY);
                if (r != null)
                {
                    telehub.RegionID = r.RegionID;
                    TelehubConnector.AddTelehub(telehub, 0);
                }
            }

            return SuccessResult();
        }

        public byte[] FindTelehub(Dictionary<string, object> request)
        {
            UUID regionID = UUID.Zero;
            UUID.TryParse(request["REGIONID"].ToString(), out regionID);

            Dictionary<string, object> result = new Dictionary<string, object>();
            Telehub telehub = TelehubConnector.FindTelehub(regionID, 0);
            if (telehub != null)
                result = telehub.ToKVP();
            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        #endregion

        #region Misc

        /// <summary>
        ///   Clean secure info out of the regions so that they do not get sent away from the grid service
        ///   This makes sure that the SessionID and other info is secure and cannot be retrieved remotely
        /// </summary>
        /// <param name = "regions"></param>
        /// <returns></returns>
        private List<GridRegion> CleanRegions(List<GridRegion> regions)
        {
            List<GridRegion> regionsToReturn = new List<GridRegion>();
            foreach (GridRegion region in regions)
            {
                regionsToReturn.Add(CleanRegion(region));
            }
            return regions;
        }

        /// <summary>
        ///   Clean secure info out of the regions so that they do not get sent away from the grid service
        ///   This makes sure that the SessionID and other info is secure and cannot be retrieved remotely
        /// </summary>
        /// <param name = "region"></param>
        /// <returns></returns>
        private GridRegion CleanRegion(GridRegion region)
        {
            if (region == null)
                return null;
            bool regionOnline = (region.Flags & (int) RegionFlags.RegionOnline) != 0;
            region.Flags = 0;
            if (regionOnline)
                region.Flags |= (int) RegionFlags.RegionOnline;
            region.SessionID = UUID.Zero;
            region.LastSeen = 0;
            region.AuthToken = "";
            return region;
        }

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                                             "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                                                       "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] SuccessResult(string result)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["Result"] = "Success";
            sendData["Message"] = result;

            string xmlString = WebUtils.BuildXmlResponse(sendData);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] FailureResult()
        {
            return FailureResult(String.Empty);
        }

        private byte[] FailureResult(string msg)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                                             "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                                                       "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Failure"));

            rootElement.AppendChild(result);

            XmlElement message = doc.CreateElement("", "Message", "");
            message.AppendChild(doc.CreateTextNode(msg));

            rootElement.AppendChild(message);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null) {Formatting = Formatting.Indented};
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        #endregion
    }
}