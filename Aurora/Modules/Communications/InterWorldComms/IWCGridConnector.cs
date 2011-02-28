using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Services.Connectors;
using OpenSim.Services.GridService;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules 
{
    public class IWCGridConnector : IGridService, IService
    {
        protected GridService m_localService;
        protected GridServicesConnector m_remoteService;
        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public virtual IGridService InnerService
        {
            get { return m_localService; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("GridHandler", "") != Name)
                return;

            m_localService = new GridService();
            m_localService.Configure(config, registry);
            m_remoteService = new GridServicesConnector();
            m_remoteService.Initialize(config, registry);
            registry.RegisterModuleInterface<IGridService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_localService.Start(config, registry);
        }

        public void FinishedStartup()
        {
            m_localService.FinishedStartup();
        }

        #endregion

        #region IGridService Members

        public string RegisterRegion(GridRegion regionInfos, UUID oldSessionID, out UUID SessionID)
        {
            return m_localService.RegisterRegion(regionInfos, oldSessionID, out SessionID);
        }

        public bool DeregisterRegion(UUID regionID, UUID SessionID)
        {
            return m_localService.DeregisterRegion(regionID, SessionID);
        }

        public GridRegion GetRegionByUUID(UUID scopeID, UUID regionID)
        {
            GridRegion r = m_localService.GetRegionByUUID(scopeID, regionID);
            if (r == null)
            {
                r = m_remoteService.GetRegionByUUID(scopeID, regionID);
            }
            return r;
        }

        public GridRegion GetRegionByPosition(UUID scopeID, int x, int y)
        {
            GridRegion r = m_localService.GetRegionByPosition(scopeID, x, y);
            if (r == null)
            {
                r = m_remoteService.GetRegionByPosition(scopeID, x, y);
            }
            return r;
        }

        public GridRegion GetRegionByName(UUID scopeID, string regionName)
        {
            GridRegion r = m_localService.GetRegionByName(scopeID, regionName);
            if (r == null)
            {
                r = m_remoteService.GetRegionByName(scopeID, regionName);
            }
            return r;
        }

        public List<GridRegion> GetRegionsByName(UUID scopeID, string name, int maxNumber)
        {
            List<GridRegion> r = m_localService.GetRegionsByName(scopeID, name, maxNumber);
            r.AddRange(m_remoteService.GetRegionsByName(scopeID, name, maxNumber));
            return r;
        }

        public List<GridRegion> GetRegionRange(UUID scopeID, int xmin, int xmax, int ymin, int ymax)
        {
            List<GridRegion> r = m_localService.GetRegionRange(scopeID, xmin, xmax, ymin, ymax);
            r.AddRange(m_remoteService.GetRegionRange(scopeID, xmin, xmax, ymin, ymax));
            return r;
        }

        public List<GridRegion> GetDefaultRegions(UUID scopeID)
        {
            return m_localService.GetDefaultRegions(scopeID);
        }

        public List<GridRegion> GetFallbackRegions(UUID scopeID, int x, int y)
        {
            return m_localService.GetFallbackRegions(scopeID, x, y);
        }

        public List<GridRegion> GetSafeRegions(UUID scopeID, int x, int y)
        {
            return m_localService.GetSafeRegions(scopeID, x, y);
        }

        public int GetRegionFlags(UUID scopeID, UUID regionID)
        {
            int flags = m_localService.GetRegionFlags(scopeID, regionID);
            if(flags == -1)
            {
                flags = m_remoteService.GetRegionFlags(scopeID, regionID);
            }
            return flags;
        }

        public string UpdateMap(GridRegion region, UUID sessionID)
        {
            return m_localService.UpdateMap(region, sessionID);
        }

        public multipleMapItemReply GetMapItems(ulong regionHandle, GridItemType gridItemType)
        {
            multipleMapItemReply reply = m_localService.GetMapItems(regionHandle, gridItemType);
            if (reply.items.Count == 0)
            {
                reply = m_remoteService.GetMapItems(regionHandle, gridItemType);
            }
            return reply;
        }

        public void SetRegionUnsafe(UUID RegionID)
        {
            m_localService.SetRegionUnsafe(RegionID);
        }

        #endregion
    }
}
