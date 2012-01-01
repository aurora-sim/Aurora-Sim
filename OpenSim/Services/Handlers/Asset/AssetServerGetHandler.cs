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
using System.IO;
using System.Net;
using System.Reflection;
using System.Xml.Serialization;
using Aurora.Simulation.Base;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services
{
    public class AssetServerGetHandler : BaseStreamHandler
    {
        private readonly IAssetService m_AssetService;
        protected string m_SessionID;
        protected IRegistryCore m_registry;

        public AssetServerGetHandler(IAssetService service, string url, string SessionID, IRegistryCore registry) :
            base("GET", url)
        {
            m_AssetService = service;
            m_SessionID = SessionID;
            m_registry = registry;
        }

        public override byte[] Handle(string path, Stream request,
                                      OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            byte[] result = new byte[0];

            string[] p = SplitParams(path);

            if (p.Length == 0)
                return result;

            IGridRegistrationService urlModule =
                m_registry.RequestModuleInterface<IGridRegistrationService>();
            if (m_SessionID != "" && urlModule != null)
                if (!urlModule.CheckThreatLevel(m_SessionID, "Asset_Get", ThreatLevel.Low))
                    return new byte[0];
            if (p.Length > 1 && p[1] == "data")
            {
                result = m_AssetService.GetData(p[0]);
                if (result == null)
                {
                    httpResponse.StatusCode = (int) HttpStatusCode.NotFound;
                    httpResponse.ContentType = "text/plain";
                    result = new byte[0];
                }
                else
                {
                    httpResponse.StatusCode = (int) HttpStatusCode.OK;
                    httpResponse.ContentType = "application/octet-stream";
                }
            }
            else if (p.Length > 1 && p[1] == "exists")
            {
                try
                {
                    bool RetVal = m_AssetService.GetExists(p[0]);
                    XmlSerializer xs =
                        new XmlSerializer(typeof (AssetBase));
                    result = WebUtils.SerializeResult(xs, RetVal);

                    if (result == null)
                    {
                        httpResponse.StatusCode = (int) HttpStatusCode.NotFound;
                        httpResponse.ContentType = "text/plain";
                        result = new byte[0];
                    }
                    else
                    {
                        httpResponse.StatusCode = (int) HttpStatusCode.OK;
                        httpResponse.ContentType = "application/octet-stream";
                    }
                }
                catch (Exception ex)
                {
                    result = new byte[0];
                    MainConsole.Instance.Warn("[AssetServerGetHandler]: Error serializing the result for /exists for asset " + p[0] +
                               ", " + ex);
                }
            }
            else
            {
                AssetBase asset = m_AssetService.Get(p[0]);

                if (asset != null)
                {
                    XmlSerializer xs = new XmlSerializer(typeof (AssetBase));
                    result = Util.CompressBytes(WebUtils.SerializeResult(xs, asset));

                    httpResponse.StatusCode = (int) HttpStatusCode.OK;
                    httpResponse.ContentType =
                        SLUtil.SLAssetTypeToContentType(asset.Type) + "/gzip";
                }
                else
                {
                    httpResponse.StatusCode = (int) HttpStatusCode.NotFound;
                    httpResponse.ContentType = "text/plain";
                    result = new byte[0];
                }
            }
            return result;
        }
    }
}