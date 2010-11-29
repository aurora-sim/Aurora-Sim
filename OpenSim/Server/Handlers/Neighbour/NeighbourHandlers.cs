/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Reflection;
using System.Net;
using System.Text;

using OpenSim.Server.Base;
using OpenSim.Server.Handlers.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Nini.Config;
using log4net;


namespace OpenSim.Server.Handlers.Neighbour
{
    public class NeighbourGetHandler : BaseStreamHandler
    {
        // unused: private ISimulationService m_SimulationService;
        // unused: private IAuthenticationService m_AuthenticationService;

        public NeighbourGetHandler(INeighbourService service, IAuthenticationService authentication) :
                base("GET", "/region")
        {
            // unused: m_SimulationService = service;
            // unused: m_AuthenticationService = authentication;
        }

        public override byte[] Handle(string path, Stream request,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            // Not implemented yet
            Console.WriteLine("--- Get region --- " + path);
            httpResponse.StatusCode = (int)HttpStatusCode.NotImplemented;
            return new byte[] { };
        }
    }

    public class NeighbourPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private INeighbourService m_NeighbourService;
        private IAuthenticationService m_AuthenticationService;
        // unused: private bool m_AllowForeignGuests;

        public NeighbourPostHandler(INeighbourService service, IAuthenticationService authentication) :
            base("POST", "/region")
        {
            m_NeighbourService = service;
            m_AuthenticationService = authentication;
            // unused: m_AllowForeignGuests = foreignGuests;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            byte[] result = new byte[0];

            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            Dictionary<string, object> request =
                        ServerUtils.ParseQueryString(body);

            OSDMap args = Util.DictionaryToOSD(request);

            // retrieve the region
            RegionInfo aRegion = new RegionInfo();
            try
            {
                aRegion.UnpackRegionInfoData(args);
            }
            catch (Exception ex)
            {
                m_log.InfoFormat("[RegionPostHandler]: exception on unpacking region info {0}", ex.Message);
                httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                httpResponse.StatusDescription = "Problems with data deserialization";
                return result;
            }

            if (m_AuthenticationService != null)
            {
                // Authentication
                string authority = string.Empty;
                string authToken = string.Empty;
                if (!RestHandlerUtils.GetAuthentication(httpRequest, out authority, out authToken))
                {
                    m_log.InfoFormat("[RegionPostHandler]: Authentication failed for neighbour message {0}", path);
                    httpResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return result;
                }
                // Rethink this
                //if (!m_AuthenticationService.VerifyKey(aRegion.RegionID, authToken))
                //{
                //    m_log.InfoFormat("[RegionPostHandler]: Authentication failed for neighbour message {0}", path);
                //    httpResponse.StatusCode = (int)HttpStatusCode.Forbidden;
                //    return result;
                //}
                m_log.DebugFormat("[RegionPostHandler]: Authentication succeeded for {0}", aRegion.RegionID);
            }

            // Finally!
            List<GridRegion> thisRegion = m_NeighbourService.InformNeighborsThatRegionisUp(aRegion);
            
            OSDMap resp = new OSDMap(1);

            if (thisRegion.Count != 0)
            {
                resp["success"] = OSD.FromBoolean(true);
                int i = 0;
                foreach (GridRegion r in thisRegion)
                {
                    Dictionary<string, object> region = r.ToKeyValuePairs();
                    resp["region" + i] = Util.DictionaryToOSD(region);
                    i++;
                }
            }
            else
                resp["success"] = OSD.FromBoolean(false);

            httpResponse.StatusCode = (int)HttpStatusCode.OK;

            string xmlString = ServerUtils.BuildXmlResponse(Util.OSDToDictionary(resp));
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }
    }

    public class NeighbourPutHandler : BaseStreamHandler
    {
        // unused: private ISimulationService m_SimulationService;
        // unused: private IAuthenticationService m_AuthenticationService;

        public NeighbourPutHandler(INeighbourService service, IAuthenticationService authentication) :
            base("PUT", "/region")
        {
            // unused: m_SimulationService = service;
            // unused: m_AuthenticationService = authentication;
        }

        public override byte[] Handle(string path, Stream request,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            // Not implemented yet
            httpResponse.StatusCode = (int)HttpStatusCode.NotImplemented;
            return new byte[] { };
        }
    }

    public class NeighbourDeleteHandler : BaseStreamHandler
    {
        // unused: private ISimulationService m_SimulationService;
        // unused: private IAuthenticationService m_AuthenticationService;

        public NeighbourDeleteHandler(INeighbourService service, IAuthenticationService authentication) :
            base("DELETE", "/region")
        {
            // unused: m_SimulationService = service;
            // unused: m_AuthenticationService = authentication;
        }

        public override byte[] Handle(string path, Stream request,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            // Not implemented yet
            httpResponse.StatusCode = (int)HttpStatusCode.NotImplemented;
            return new byte[] { };
        }
    }
}
