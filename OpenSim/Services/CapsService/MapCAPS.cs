using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;
using OpenSim.Framework.Capabilities;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.CapsService
{
    public class MapCAPS : ICapsServiceConnector
    {
        private readonly string m_mapLayerPath = "0001";
        private UUID m_agentID = UUID.Zero;
        private IPrivateCapsService m_handler;
        private List<MapBlockData> m_mapLayer = new List<MapBlockData>();
        public List<IRequestHandler> RegisterCaps(UUID agentID, IHttpServer server, IPrivateCapsService handler)
        {
            m_handler = handler;
            m_agentID = agentID;

            List<IRequestHandler> handlers = new List<IRequestHandler>();
            RestMethod method = delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return MapLayerRequest(request, path, param, httpRequest, httpResponse, agentID);
            };
            handlers.Add(new RestStreamHandler("POST", handler.CreateCAPS("MapLayer", m_mapLayerPath),
                                                      method));
            return handlers;
        }

        /// <summary>
        /// Callback for a map layer request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="agentID"></param>
        /// <param name="caps"></param>
        /// <returns></returns>
        public string MapLayerRequest(string request, string path, string param,
                                      OSHttpRequest httpRequest, OSHttpResponse httpResponse, UUID agentID)
        {
            uint x, y;
            Utils.LongToUInts(m_handler.RegionHandle, out x, out y);
            int bottom = (int)y - 100;
            int top = (int)y + 100;
            int left = (int)x - 100;
            int right = (int)x + 100;


            OSDArray layerData = new OSDArray();
            layerData.Add(GetOSDMapLayerResponse(bottom, left, right, top, new UUID("00000000-0000-1111-9999-000000000006")));
            OSDArray mapBlocksData = new OSDArray();

            List<MapBlockData> mapBlocks = new List<MapBlockData>();
            if (m_mapLayer != null)
            {
                mapBlocks = m_mapLayer;
            }
            else
            {
                List<GridRegion> regions = m_handler.GridService.GetRegionRange(UUID.Zero,
                        left * (int)Constants.RegionSize,
                        right * (int)Constants.RegionSize,
                        bottom * (int)Constants.RegionSize,
                        top * (int)Constants.RegionSize);
                foreach (GridRegion r in regions)
                {
                    mapBlocks.Add(MapBlockFromGridRegion(r));
                }
                m_mapLayer = mapBlocks;
            }
            foreach (MapBlockData block in m_mapLayer)
            {
                //Add to the array
                mapBlocksData.Add(block.ToOSD());
            }
            OSDMap response = MapLayerResponce(layerData, mapBlocksData);
            string resp = OSDParser.SerializeLLSDXmlString(response);
            return resp;
        }

        protected MapBlockData MapBlockFromGridRegion(GridRegion r)
        {
            MapBlockData block = new MapBlockData();
            if (r == null)
            {
                block.Access = (byte)SimAccess.Down;
                block.MapImageID = UUID.Zero;
                return block;
            }
            block.Access = r.Access;
            block.MapImageID = r.TerrainImage;
            block.Name = r.RegionName;
            block.X = (ushort)(r.RegionLocX / Constants.RegionSize);
            block.Y = (ushort)(r.RegionLocY / Constants.RegionSize);
            return block;
        }

        protected static OSDMap MapLayerResponce(OSDArray layerData, OSDArray mapBlocksData)
        {
            OSDMap map = new OSDMap();
            OSDMap agentMap = new OSDMap();
            agentMap["Flags"] = 0;
            map["AgentData"] = agentMap;
            map["LayerData"] = layerData;
            map["MapBlocks"] = mapBlocksData;
            return map;
        }

        /// <summary>
        ///
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
