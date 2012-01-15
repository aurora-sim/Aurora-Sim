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

using System.Collections.Generic;
using System.Linq;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Connectors;
using OpenSim.Services.GridService;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace Aurora.Modules
{
    public class IWCGridConnector : ConnectorBase, IGridService, IService
    {
        protected IGridService m_localService;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IGridService Members

        public virtual IGridService InnerService
        {
            get
            {
                //If we are getting URls for an IWC connection, we don't want to be calling other things, as they are calling us about only our info
                //If we arn't, its ar region we are serving, so give it everything we know
                if (m_registry.RequestModuleInterface<InterWorldCommunications>().IsGettingUrlsForIWCConnection)
                    return m_localService;
                else
                    return this;
            }
        }

        public void Configure(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_localService != null)
                m_localService.Start(config, registry);
        }

        public void FinishedStartup()
        {
            if (m_localService != null)
                m_localService.FinishedStartup();
        }

        public int MaxRegionSize
        {
            get { return m_localService.MaxRegionSize; }
        }

        public int RegionViewSize
        {
            get { return m_localService.RegionViewSize; }
        }

        public RegisterRegion RegisterRegion(GridRegion regionInfos, UUID oldSessionID)
        {
            return m_localService.RegisterRegion(regionInfos, oldSessionID);
        }

        public bool DeregisterRegion(GridRegion region)
        {
            return m_localService.DeregisterRegion(region);
        }

        public GridRegion GetRegionByUUID(UUID scopeID, UUID regionID)
        {
            GridRegion r = m_localService.GetRegionByUUID(scopeID, regionID);
            if (r == null)
            {
                r = (GridRegion)DoRemoteForced(scopeID, regionID);
                UpdateGridRegionForIWC(ref r);
            }

            return r;
        }

        public GridRegion GetRegionByPosition(UUID scopeID, int x, int y)
        {
            GridRegion r = m_localService.GetRegionByPosition(scopeID, x, y);
            if (r == null)
            {
                r = (GridRegion)DoRemoteForced(scopeID, x, y);
                UpdateGridRegionForIWC(ref r);
            }
            return r;
        }

        public GridRegion GetRegionByName(UUID scopeID, string regionName)
        {
            GridRegion r = m_localService.GetRegionByName(scopeID, regionName);
            if (r == null)
            {
                r = (GridRegion)DoRemoteForced(scopeID, regionName);
                UpdateGridRegionForIWC(ref r);
            }
            return r;
        }

        public List<GridRegion> GetRegionsByName(UUID scopeID, string name, int maxNumber)
        {
            List<GridRegion> r = m_localService.GetRegionsByName(scopeID, name, maxNumber);
            List<GridRegion> remoteRegions = (List<GridRegion>)DoRemoteForced(scopeID, name, maxNumber);
            if (remoteRegions != null)
            {
                UpdateGridRegionsForIWC(ref remoteRegions);
                r.AddRange(remoteRegions);
            }
            //Sort to find the region with the exact name that was given
            r.Sort(new GridService.RegionDataComparison(name));
            //Results are backwards... so it needs reversed
            r.Reverse();
            return r;
        }

        public List<GridRegion> GetRegionRange(UUID scopeID, int xmin, int xmax, int ymin, int ymax)
        {
            List<GridRegion> r = m_localService.GetRegionRange(scopeID, xmin, xmax, ymin, ymax);
            List<GridRegion> remoteRegions = (List<GridRegion>)DoRemoteForced(scopeID, xmin, xmax, ymin, ymax);
            if (remoteRegions != null)
            {
                UpdateGridRegionsForIWC(ref remoteRegions);
                r.AddRange(remoteRegions);
            }
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
            if (flags == -1)
                flags = (int)DoRemoteForced(scopeID, regionID);

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
                reply = (multipleMapItemReply)DoRemoteForced(regionHandle, gridItemType);

            return reply;
        }

        public void SetRegionUnsafe(UUID RegionID)
        {
            m_localService.SetRegionUnsafe(RegionID);
        }

        public void SetRegionSafe(UUID RegionID)
        {
            m_localService.SetRegionUnsafe(RegionID);
        }

        public bool VerifyRegionSessionID(GridRegion r, UUID SessionID)
        {
            return m_localService.VerifyRegionSessionID(r, SessionID);
        }

        public List<GridRegion> GetNeighbors(GridRegion r)
        {
            List<GridRegion> neighbors = m_localService.GetNeighbors(r);
            List<GridRegion> remoteNeighbors = (List<GridRegion>)DoRemoteForced(r);
            UpdateGridRegionsForIWC(ref remoteNeighbors);
            neighbors.AddRange(remoteNeighbors);
            return neighbors;
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("GridHandler", "") != Name)
                return;

            string localHandler = handlerConfig.GetString("LocalGridHandler", "GridService");
            List<IGridService> services = AuroraModuleLoader.PickupModules<IGridService>();
#if (!ISWIN)
            foreach (IGridService s in services)
            {
                if (s.GetType().Name == localHandler) m_localService = s;
            }
#else
            foreach (IGridService s in services.Where(s => s.GetType().Name == localHandler))
                m_localService = s;
#endif

            m_registry = registry;
            if (m_localService == null)
                m_localService = new GridService();
            m_localService.Configure(config, registry);
            registry.RegisterModuleInterface<IGridService>(this);
            Init(registry, Name);
        }

        #endregion

        private void UpdateGridRegionsForIWC(ref List<GridRegion> rs)
        {
            for (int i = 0; i < rs.Count; i++)
            {
                GridRegion r = rs[i];
                UpdateGridRegionForIWC(ref r);
                rs[i] = r;
            }
        }

        private GridRegion UpdateGridRegionForIWC(ref GridRegion r)
        {
            if (r == null)
                return r;
            InterWorldCommunications comms = m_registry.RequestModuleInterface<InterWorldCommunications>();
            r.Flags |= (int) RegionFlags.Foreign;
            //if (r.GenericMap["GridUrl"] == "")
            //    r.GenericMap["ThreatLevel"] = comms.m_untrustedConnectionsDefaultTrust.ToString();
            //else
            //    r.GenericMap["ThreatLevel"] = comms.GetThreatLevelForUrl(r.GenericMap["GridUrl"]).ToString();
            return r;
        }
    }
}