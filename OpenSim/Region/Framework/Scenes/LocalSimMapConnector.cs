using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using log4net;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;

namespace OpenSim.Region.Framework.Scenes
{
    public class LocalSimMapConnector : ISimMapConnector
    {
        IGridService GridService;
        SimMapConnector SimMapConnector;
        public LocalSimMapConnector(IGridService GS)
        {
            GridService = GS;
            SimMapConnector = new SimMapConnector(GridService);
        }

        public List<SimMap> GetSimMap(UUID regionID, UUID agentID)
        {
            List<SimMap> Sims = new List<SimMap>();
            SimMap map = SimMapConnector.GetSimMap(regionID, agentID);
            Sims.Add(map);
            return Sims;
        }

        public List<SimMap> GetSimMap(string RegionName, UUID agentID)
        {
            List<SimMap> Sims = new List<SimMap>();
            List<OpenSim.Services.Interfaces.GridRegion> Regions = GridService.GetRegionsByName(UUID.Zero, RegionName, 20);

            foreach (OpenSim.Services.Interfaces.GridRegion region in Regions)
            {
                SimMap map = SimMapConnector.GetSimMap(region.RegionID, agentID);
                Sims.Add(map);
            }
            return Sims;
        }

        public List<SimMap> GetSimMapRange(uint XMin, uint YMin, uint XMax, uint YMax, UUID agentID)
        {
            List<SimMap> Sims = new List<SimMap>();
            List<OpenSim.Services.Interfaces.GridRegion> Regions = GridService.GetRegionRange(UUID.Zero,
                    (int)XMin,
                    (int)XMax,
                    (int)YMin,
                    (int)YMax);

            foreach (OpenSim.Services.Interfaces.GridRegion region in Regions)
            {
                SimMap map = SimMapConnector.GetSimMap(region.RegionID, agentID);
                if (map != null)
                    Sims.Add(map);
            }

            if (Sims.Count == 0)
            {
                int X = (int)(XMax + XMin) / 2;
                int Y = (int)(YMax + YMin) / 2;
                Sims.Add(SimMapConnector.NotFound(X, Y));
            }
            return Sims;
        }

        public void UpdateSimMapOnlineStatus(UUID regionID)
        {
            SimMapConnector.UpdateSimMap(regionID);
        }

        public void UpdateSimMap(SimMap map)
        {
            SimMapConnector.UpdateSimMap(map);
        }

        public List<mapItemReply> GetMapItems(ulong regionHandle, GridItemType gridItemType)
        {
            return SimMapConnector.GetMapItems(regionHandle, gridItemType);
        }

        public void AddAgent(UUID regionID, UUID agentID, Vector3 Position)
        {
            SimMapConnector.AddAgent(regionID, agentID, Position);
        }

        public void RemoveAgent(UUID regionID, UUID agentID)
        {
            SimMapConnector.RemoveAgent(regionID, agentID);
        }
    }
}
