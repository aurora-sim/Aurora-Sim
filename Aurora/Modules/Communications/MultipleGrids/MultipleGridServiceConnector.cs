using System;
using System.Collections.Generic;
using Nini.Config;
using log4net;
using System.Reflection;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Region.CoreModules.ServiceConnectorsOut;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Grid;

namespace Aurora.Modules.Communications.MultipleGrids
{
    public class MultipleGridServicesConnector : ISharedRegionModule, IGridService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Enabled = false;
        private List<IGridService> AllServices = new List<IGridService>();

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "MultipleGridServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("GridServices", "");
                if (name == Name)
                {
                    IConfig multipleConfig = source.Configs["MultipleGridsModule"];
                    if (multipleConfig != null)
                    {
                        IConfig UAS = source.Configs["GridService"];
                        if (UAS != null)
                        {
                            string[] Grids = multipleConfig.GetString("GridServerURIs", "").Split(',');
                            //Set it so that it works for them
                            moduleConfig.Set("GridServices", "RemoteGridServicesConnector");
                            foreach (string gridURL in Grids)
                            {
                                //Set their gridURL
                                UAS.Set("GridServerURI", gridURL);
                                //Start it up
                                RemoteGridServicesConnector connector = new RemoteGridServicesConnector();
                                connector.Initialise(source);
                                AllServices.Add(connector);
                                m_log.Info("[GRID CONNECTOR]: Multiple grid users enabled for " + gridURL);
                            }
                        }
                    }
                    //Reset the name
                    moduleConfig.Set("GridServices", Name);
                    m_Enabled = true;
                }
            }
        }

        public void PostInitialise()
        {
            if (!m_Enabled)
                return;
        }

        public void Close()
        {
            if (!m_Enabled)
                return;
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            scene.RegisterModuleInterface<IGridService>(this);
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;
        }

        #region IGridService Members

        public string RegisterRegion(UUID scopeID, OpenSim.Services.Interfaces.GridRegion regionInfos, UUID oldSessionID, out UUID SessionID)
        {
            SessionID = UUID.Zero;
            string ret = "";
            foreach (IGridService service in AllServices)
            {
                UUID s = UUID.Zero;
                ret += service.RegisterRegion(scopeID, regionInfos, oldSessionID, out s);
                if (s != UUID.Zero)
                    SessionID = s;
            }
            return ret;
        }

        public bool DeregisterRegion(UUID regionID, UUID SessionID)
        {
            foreach (IGridService service in AllServices)
            {
                if (!service.DeregisterRegion(regionID, SessionID))
                    return false;
            }
            return true;
        }

        public List<OpenSim.Services.Interfaces.GridRegion> GetNeighbours(UUID scopeID, UUID regionID)
        {
            List<OpenSim.Services.Interfaces.GridRegion> n = new List<OpenSim.Services.Interfaces.GridRegion>();
            foreach (IGridService service in AllServices)
            {
                n.AddRange(service.GetNeighbours(scopeID, regionID));
            }
            return n;
        }

        public OpenSim.Services.Interfaces.GridRegion GetRegionByUUID(UUID scopeID, UUID regionID)
        {
            OpenSim.Services.Interfaces.GridRegion r = null;
            foreach (IGridService service in AllServices)
            {
                r = service.GetRegionByUUID(scopeID, regionID);
                if (r != null)
                    return r;
            }
            return r;
        }

        public OpenSim.Services.Interfaces.GridRegion GetRegionByPosition(UUID scopeID, int x, int y)
        {
            OpenSim.Services.Interfaces.GridRegion r = null;
            foreach (IGridService service in AllServices)
            {
                r = service.GetRegionByPosition(scopeID, x, y);
                if (r != null)
                    return r;
            }
            return r;
        }

        public OpenSim.Services.Interfaces.GridRegion GetRegionByName(UUID scopeID, string regionName)
        {
            OpenSim.Services.Interfaces.GridRegion r = null;
            foreach (IGridService service in AllServices)
            {
                r = service.GetRegionByName(scopeID, regionName);
                if (r != null)
                    return r;
            }
            return r;
        }

        public List<OpenSim.Services.Interfaces.GridRegion> GetRegionsByName(UUID scopeID, string name, int maxNumber)
        {
            List<OpenSim.Services.Interfaces.GridRegion> n = new List<OpenSim.Services.Interfaces.GridRegion>();
            foreach (IGridService service in AllServices)
            {
                n.AddRange(service.GetRegionsByName(scopeID, name, maxNumber));
            }
            return n;
        }

        public List<OpenSim.Services.Interfaces.GridRegion> GetRegionRange(UUID scopeID, int xmin, int xmax, int ymin, int ymax)
        {
            List<OpenSim.Services.Interfaces.GridRegion> n = new List<OpenSim.Services.Interfaces.GridRegion>();
            foreach (IGridService service in AllServices)
            {
                n.AddRange(service.GetRegionRange(scopeID, xmin, xmax, ymin, ymax));
            }
            return n;
        }

        public List<OpenSim.Services.Interfaces.GridRegion> GetDefaultRegions(UUID scopeID)
        {
            List<OpenSim.Services.Interfaces.GridRegion> n = new List<OpenSim.Services.Interfaces.GridRegion>();
            foreach (IGridService service in AllServices)
            {
                n.AddRange(service.GetDefaultRegions(scopeID));
            }
            return n;
        }

        public List<OpenSim.Services.Interfaces.GridRegion> GetFallbackRegions(UUID scopeID, int x, int y)
        {
            List<OpenSim.Services.Interfaces.GridRegion> n = new List<OpenSim.Services.Interfaces.GridRegion>();
            foreach (IGridService service in AllServices)
            {
                n.AddRange(service.GetFallbackRegions(scopeID, x, y));
            }
            return n;
        }

        public List<OpenSim.Services.Interfaces.GridRegion> GetSafeRegions(UUID scopeID, int x, int y)
        {
            List<OpenSim.Services.Interfaces.GridRegion> n = new List<OpenSim.Services.Interfaces.GridRegion>();
            foreach (IGridService service in AllServices)
            {
                n.AddRange(service.GetSafeRegions(scopeID, x, y));
            }
            return n;
        }

        public List<OpenSim.Services.Interfaces.GridRegion> GetHyperlinks(UUID scopeID)
        {
            List<OpenSim.Services.Interfaces.GridRegion> n = new List<OpenSim.Services.Interfaces.GridRegion>();
            foreach (IGridService service in AllServices)
            {
                n.AddRange(service.GetHyperlinks(scopeID));
            }
            return n;
        }

        public int GetRegionFlags(UUID scopeID, UUID regionID)
        {
            int r = 0;
            foreach (IGridService service in AllServices)
            {
                r = service.GetRegionFlags(scopeID, regionID);
                if (r != 0)
                    return r;
            }
            return r;
        }

        public string UpdateMap(UUID scopeID, OpenSim.Services.Interfaces.GridRegion region, UUID sessionID)
        {
            string r = "";
            foreach (IGridService service in AllServices)
            {
                r += service.UpdateMap(scopeID, region, sessionID);
            }
            return r;
        }

        public OpenSim.Framework.multipleMapItemReply GetMapItems(ulong regionHandle, GridItemType gridItemType)
        {
            OpenSim.Framework.multipleMapItemReply r = new OpenSim.Framework.multipleMapItemReply();
            foreach (IGridService service in AllServices)
            {
                OpenSim.Framework.multipleMapItemReply rr = service.GetMapItems(regionHandle, gridItemType);
                foreach(KeyValuePair<ulong, List<OpenSim.Framework.mapItemReply>> kvp in rr.items)
                {
                    if(!r.items.ContainsKey(kvp.Key))
                        r.items.Add(kvp.Key, kvp.Value);
                }
            }
            return r;
        }

        public void RemoveAgent(UUID regionID, UUID agentID)
        {
            foreach (IGridService service in AllServices)
            {
                service.RemoveAgent(regionID, agentID);
            }
        }

        public void AddAgent(UUID regionID, UUID agentID, Vector3 Position)
        {
            foreach (IGridService service in AllServices)
            {
                service.AddAgent(regionID, agentID, Position);
            }
        }

        public void SetRegionUnsafe(UUID ID)
        {
            foreach (IGridService service in AllServices)
            {
                service.SetRegionUnsafe(ID);
            }
        }

        public string GridServiceURL
        {
            get { return AllServices[0].GridServiceURL; }
        }

        #endregion
    }
}
