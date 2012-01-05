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

using System.IO;
using System.Text;
using System.Xml.Serialization;
using Aurora.Simulation.Base;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services
{
    public class AssetServerPostHandler : BaseStreamHandler
    {
        // private static readonly ILog MainConsole.Instance = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IAssetService m_AssetService;
        protected string m_SessionID;
        protected IRegistryCore m_registry;

        public AssetServerPostHandler(IAssetService service, string url, string SessionID, IRegistryCore registry) :
            base("POST", url)
        {
            m_AssetService = service;
            m_SessionID = SessionID;
            m_registry = registry;
        }

        public override byte[] Handle(string path, Stream request,
                                      OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            string body = GetBodyAsString(request);
            OSDMap map = WebUtils.GetOSDMap(Util.Decompress(body));
            IGridRegistrationService urlModule =
                m_registry.RequestModuleInterface<IGridRegistrationService>();
            if (m_SessionID != "" && urlModule != null)
                if (!urlModule.CheckThreatLevel(m_SessionID, "Asset_Update", ThreatLevel.High))
                    return new byte[0];
            UUID newID;
            if (map.ContainsKey("Method") && map["Method"] == "UpdateContent")
                m_AssetService.UpdateContent(map["ID"].AsUUID(), map["Data"].AsBinary(), out newID);
            else
            {
                AssetBase asset = new AssetBase();
                asset = asset.Unpack(map);
                newID = m_AssetService.Store(asset);
            }

            return Encoding.UTF8.GetBytes(newID.ToString());
        }
    }
}