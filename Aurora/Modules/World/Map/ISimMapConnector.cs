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

namespace Aurora.Modules
{
    public interface ISimMapConnector
    {
        List<SimMap> GetSimMap(UUID regionID, UUID AgentID);
        List<SimMap> GetSimMap(string mapName, UUID AgentID);
        List<SimMap> GetSimMapRange(uint XMin, uint YMin, uint XMax, uint YMax, UUID agentID);
        
        void UpdateSimMapOnlineStatus(UUID regionID);
        void UpdateSimMap(SimMap map);

        void AddAgent(UUID regionID, UUID agentID, Vector3 Position);
        void RemoveAgent(UUID regionID, UUID agentID);
        List<mapItemReply> GetMapItems(ulong regionHandle, GridItemType gridItemType);
    }
}
