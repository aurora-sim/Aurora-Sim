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

using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System.Collections.Generic;
using System.IO;
using GridRegion = Aurora.Framework.Services.GridRegion;

namespace Aurora.Services
{
    public class MapCAPS : IExternalCapsRequestHandler
    {
        private const int m_mapDistance = 100;
        private readonly List<MapBlockData> m_mapLayer = new List<MapBlockData>();
        private bool m_allowCapsMessage = true;
        private IGridService m_gridService;
        private UUID m_agentID;
        private Aurora.Framework.Services.GridRegion m_region;
        private string m_uri;
        private List<UUID> m_userScopeIDs = new List<UUID>();

        #region ICapsServiceConnector Members

        public string Name { get { return GetType().Name; } }

        public void IncomingCapsRequest(UUID agentID, Aurora.Framework.Services.GridRegion region, ISimulationBase simbase, ref OSDMap capURLs)
        {
            m_agentID = agentID;
            m_region = region;
            m_userScopeIDs = simbase.ApplicationRegistry.RequestModuleInterface<IUserAccountService>().GetUserAccount(null, m_agentID).AllScopeIDs;

            m_gridService = simbase.ApplicationRegistry.RequestModuleInterface<IGridService>();
            IConfig config =
                simbase.ConfigSource.Configs["MapCaps"];
            if (config != null)
                m_allowCapsMessage = config.GetBoolean("AllowCapsMessage", m_allowCapsMessage);

            HttpServerHandle method = delegate(string path, Stream request, OSHttpRequest httpRequest,
                                               OSHttpResponse httpResponse)
                                          {
                                              return MapLayerRequest(request.ReadUntilEnd(), httpRequest, httpResponse);
                                          };
            m_uri = "/CAPS/MapLayer/" + UUID.Random() + "/";
            capURLs["MapLayer"] = MainServer.Instance.ServerURI + m_uri;
            capURLs["MapLayerGod"] = MainServer.Instance.ServerURI + m_uri;

            MainServer.Instance.AddStreamHandler(new GenericStreamHandler("POST", m_uri, method));
        }

        public void IncomingCapsDestruction()
        {
            MainServer.Instance.RemoveStreamHandler("POST", m_uri);
            m_mapLayer.Clear();
        }

        #endregion

        /// <summary>
        ///     Callback for a map layer request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="httpRequest"></param>
        /// <param name="httpResponse"></param>
        /// <param name="agentID"></param>
        /// <returns></returns>
        public byte[] MapLayerRequest(string request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            int bottom = (m_region.RegionLocY / Constants.RegionSize) - m_mapDistance;
            int top = (m_region.RegionLocY / Constants.RegionSize) + m_mapDistance;
            int left = (m_region.RegionLocX / Constants.RegionSize) - m_mapDistance;
            int right = (m_region.RegionLocX / Constants.RegionSize) + m_mapDistance;

            OSDMap map = (OSDMap) OSDParser.DeserializeLLSDXml(request);

            int flags = map["Flags"].AsInteger();

            OSDArray layerData = new OSDArray
                                     {
                                         GetOSDMapLayerResponse(bottom, left, right, top,
                                                                new UUID("00000000-0000-1111-9999-000000000006"))
                                     };
            OSDArray mapBlocksData = new OSDArray();

            if (m_allowCapsMessage)
            {
                if (m_mapLayer == null || m_mapLayer.Count == 0)
                {
                    List<GridRegion> regions = m_gridService.GetRegionRange(
                        m_userScopeIDs,
                        left*Constants.RegionSize,
                        right*Constants.RegionSize,
                        bottom*Constants.RegionSize,
                        top*Constants.RegionSize);
                    foreach (GridRegion r in regions)
                    {
                        m_mapLayer.Add(MapBlockFromGridRegion(r, flags));
                    }
                }
            }
            foreach (MapBlockData block in m_mapLayer)
            {
                //Add to the array
                mapBlocksData.Add(block.ToOSD());
            }
            OSDMap response = MapLayerResponce(layerData, mapBlocksData, flags);
            return OSDParser.SerializeLLSDXmlBytes(response);
        }

        protected MapBlockData MapBlockFromGridRegion(GridRegion r, int flag)
        {
            MapBlockData block = new MapBlockData();
            if (r == null)
            {
                block.Access = (byte) SimAccess.Down;
                block.MapImageID = UUID.Zero;
                return block;
            }
            block.Access = r.Access;
            if ((flag & 0xffff) == 0)
                block.MapImageID = r.TerrainImage;
            if ((flag & 0xffff) == 1)
                block.MapImageID = r.TerrainMapImage;
            if ((flag & 0xffff) == 2)
                block.MapImageID = r.ParcelMapImage;
            block.Name = r.RegionName;
            block.X = (ushort) (r.RegionLocX/Constants.RegionSize);
            block.Y = (ushort) (r.RegionLocY/Constants.RegionSize);
            block.SizeX = (ushort) (r.RegionSizeX);
            block.SizeY = (ushort) (r.RegionSizeY);
            return block;
        }

        protected static OSDMap MapLayerResponce(OSDArray layerData, OSDArray mapBlocksData, int flags)
        {
            OSDMap map = new OSDMap();
            OSDMap agentMap = new OSDMap();
            agentMap["Flags"] = flags;
            map["AgentData"] = agentMap;
            map["LayerData"] = layerData;
            map["MapBlocks"] = mapBlocksData;
            return map;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        protected static OSDMap GetOSDMapLayerResponse(int bottom, int left, int right, int top, UUID imageID)
        {
            OSDMap mapLayer = new OSDMap();
            mapLayer["Bottom"] = bottom;
            mapLayer["Left"] = left;
            mapLayer["Right"] = right;
            mapLayer["Top"] = top;
            mapLayer["ImageID"] = imageID;

            return mapLayer;
        }
    }
}