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

using log4net;
using System;
using System.Collections.Generic;
using System.Reflection;
using Nini.Config;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Services.Connectors;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Region.CoreModules.ServiceConnectorsOut.Grid
{
    public class RemoteGridServicesConnector :
            GridServicesConnector, ISharedRegionModule, IGridService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Enabled = false;

        private IGridService m_LocalGridService;

        private GridCache m_GridCache;

        public RemoteGridServicesConnector()
        {
            m_GridCache = new GridCache();
        }
            
        public RemoteGridServicesConnector(IConfigSource source)
        {
            m_GridCache = new GridCache();
            InitialiseServices(source);
        }

        #region ISharedRegionmodule

        public Type ReplaceableInterface 
        {
            get { return null; }
        }

        public string Name
        {
            get { return "RemoteGridServicesConnector"; }
        }

        public override void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("GridServices", "");
                if (name == Name)
                {
                    InitialiseServices(source);
                    m_Enabled = true;
                    m_log.Info("[REMOTE GRID CONNECTOR]: Remote grid enabled");
                }
            }
        }

        private void InitialiseServices(IConfigSource source)
        {
            IConfig gridConfig = source.Configs["GridService"];
            if (gridConfig == null)
            {
                m_log.Error("[REMOTE GRID CONNECTOR]: GridService missing from OpenSim.ini");
                return;
            }

            base.Initialise(source);

            m_LocalGridService = new LocalGridServicesConnector(source);
        }

        public void PostInitialise()
        {
            if (m_LocalGridService != null)
                ((ISharedRegionModule)m_LocalGridService).PostInitialise();
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (m_Enabled)
                scene.RegisterModuleInterface<IGridService>(this);

            if (m_LocalGridService != null)
                ((ISharedRegionModule)m_LocalGridService).AddRegion(scene);
        }

        public void RemoveRegion(Scene scene)
        {
            if (m_LocalGridService != null)
                ((ISharedRegionModule)m_LocalGridService).RemoveRegion(scene);
        }

        public void RegionLoaded(Scene scene)
        {
        }

        #endregion

        #region IGridService

        public override string RegisterRegion(UUID scopeID, GridRegion regionInfo)
        {
            m_GridCache.AddRegion(regionInfo);
            string msg = m_LocalGridService.RegisterRegion(scopeID, regionInfo);

            if (msg == String.Empty)
                return base.RegisterRegion(scopeID, regionInfo);

            return msg;
        }

        public override bool DeregisterRegion(UUID regionID)
        {
            m_GridCache.RemoveRegion(regionID);
            if (m_LocalGridService.DeregisterRegion(regionID))
                return base.DeregisterRegion(regionID);

            return false;
        }

        // Let's override GetNeighbours completely -- never go to the grid server
        // Neighbours are/should be cached locally
        // For retrieval from the DB, caller should call GetRegionByPosition
        public override List<GridRegion> GetNeighbours(UUID scopeID, UUID regionID)
        {
            return m_LocalGridService.GetNeighbours(scopeID, regionID);
        }

        public override GridRegion GetRegionByUUID(UUID scopeID, UUID regionID)
        {
            GridRegion rinfo = null;//m_GridCache.GetRegionByUUID(regionID);

            if (rinfo == null) 
                rinfo = m_LocalGridService.GetRegionByUUID(scopeID, regionID);
            if (rinfo == null)
                rinfo = base.GetRegionByUUID(scopeID, regionID);

            m_GridCache.AddRegion(rinfo);
            return rinfo;
        }

        public override GridRegion GetRegionByPosition(UUID scopeID, int x, int y)
        {
            bool found = false;
            GridRegion rinfo = null;//m_GridCache.GetRegionByPosition(x, y, out found);

            if (!found && rinfo == null)
                rinfo = m_LocalGridService.GetRegionByPosition(scopeID, x, y);
            if (!found && rinfo == null)
                rinfo = base.GetRegionByPosition(scopeID, x, y);

            m_GridCache.AddRegion(rinfo, x, y);
            return rinfo;
        }

        public override GridRegion GetRegionByName(UUID scopeID, string regionName)
        {
            GridRegion rinfo = m_LocalGridService.GetRegionByName(scopeID, regionName);
            if (rinfo == null)
                rinfo = base.GetRegionByName(scopeID, regionName);

            m_GridCache.AddRegion(rinfo);
            return rinfo;
        }

        public override List<GridRegion> GetRegionsByName(UUID scopeID, string name, int maxNumber)
        {
            List<GridRegion> rinfo = m_LocalGridService.GetRegionsByName(scopeID, name, maxNumber);
            //m_log.DebugFormat("[REMOTE GRID CONNECTOR]: Local GetRegionsByName {0} found {1} regions", name, rinfo.Count);
            List<GridRegion> grinfo = base.GetRegionsByName(scopeID, name, maxNumber);

            if (grinfo != null)
            {
                //m_log.DebugFormat("[REMOTE GRID CONNECTOR]: Remote GetRegionsByName {0} found {1} regions", name, grinfo.Count);
                rinfo.AddRange(grinfo);
            }

            m_GridCache.AddRegions(rinfo);
            return rinfo;
        }

        public override List<GridRegion> GetRegionRange(UUID scopeID, int xmin, int xmax, int ymin, int ymax)
        {
            List <GridRegion> regions =  base.GetRegionRange(scopeID, xmin, xmax, ymin, ymax);
            m_GridCache.AddRegions(regions);
            return regions;
        }

        public override int GetRegionFlags(UUID scopeID, UUID regionID)
        {
            int flags = m_LocalGridService.GetRegionFlags(scopeID, regionID);
            if (flags == -1)
                flags = base.GetRegionFlags(scopeID, regionID);

            return flags;
        }
        #endregion
    }

    public class GridCache
    {
        private class GridRegionCache
        {
            public GridRegion region;
            public int locX;
            public int locY;
            public DateTime LastUpdated;
            public bool IsNull = false;
        }

        private List<GridRegionCache> cache = new List<GridRegionCache>();
        public void AddRegion(GridRegion regionInfo)
        {
            if (regionInfo == null)
                return;
            GridRegionCache newcache = new GridRegionCache();
            newcache.LastUpdated = DateTime.UtcNow;
            newcache.locX = regionInfo.RegionLocX;
            newcache.locY = regionInfo.RegionLocY;
            newcache.region = regionInfo;
            bool found = false;
            for (int i = 0; i < cache.Count; i++)
            {
                if (cache[i].region == null)
                {
                    if (cache[i].locX == newcache.region.RegionLocX && cache[i].locY == newcache.region.RegionLocY)
                    {
                        found = true;
                        TimeSpan ts = cache[i].LastUpdated - DateTime.UtcNow;
                        if (ts.Hours > 1)
                        {
                            cache[i] = newcache;
                        }
                    }
                }
                else
                {
                    if (cache[i].region.RegionID == regionInfo.RegionID)
                    {
                        found = true;
                        TimeSpan ts = cache[i].LastUpdated - DateTime.UtcNow;
                        if (ts.Hours > 1)
                        {
                            cache[i] = newcache;
                        }
                    }
                }
            }
            if (!found)
                cache.Add(newcache);
        }

        public void RemoveRegion(UUID regionID)
        {
            for (int i = 0; i < cache.Count; i++)
            {
                if (cache[i].region.RegionID == regionID)
                {
                    cache.Remove(cache[i]);
                    return;
                }
            }
        }

        public void AddRegions(List<GridRegion> rinfo)
        {
            foreach (GridRegion region in rinfo)
            {
                AddRegion(region);
            }
        }

        public GridRegion GetRegionByPosition(int x, int y, out bool found)
        {
            found = false;
            for (int i = 0; i < cache.Count; i++)
            {
                if (cache[i].locX == x)
                {
                    if (cache[i].locY == y)
                    {
                        found = true;
                        return cache[i].region;
                    }
                }
            }
            return null;
        }

        internal GridRegion GetRegionByUUID(UUID regionID)
        {
            for (int i = 0; i < cache.Count; i++)
            {
                if (cache[i].region == null)
                    continue;
                if (cache[i].region.RegionID == regionID)
                {
                    return cache[i].region;
                }
            }
            return null;
        }

        internal void AddRegion(GridRegion regionInfo, int x, int y)
        {
            GridRegionCache newcache = new GridRegionCache();
            newcache.LastUpdated = DateTime.UtcNow;
            newcache.locX = x;
            newcache.locY = y;
            newcache.region = regionInfo;
            bool found = false;
            for (int i = 0; i < cache.Count; i++)
            {
                if (regionInfo != null)
                {
                    if (cache[i].region == null)
                    {
                        if (cache[i].locX == x && cache[i].locY == y)
                        {
                            found = true;
                            TimeSpan ts = cache[i].LastUpdated - DateTime.UtcNow;
                            if (ts.Hours > 1)
                            {
                                cache[i] = newcache;
                            }
                        }
                    }
                    else
                    {
                        if (cache[i].region.RegionID == regionInfo.RegionID)
                        {
                            found = true;
                            TimeSpan ts = cache[i].LastUpdated - DateTime.UtcNow;
                            if (ts.Hours > 1)
                            {
                                cache[i] = newcache;
                            }
                        }
                    }
                }
                else
                {
                    if (cache[i].locX == x && cache[i].locY == y)
                    {
                        found = true;
                        TimeSpan ts = cache[i].LastUpdated - DateTime.UtcNow;
                        if (ts.Hours > 1)
                        {
                            cache[i] = newcache;
                        }
                    }
                }
            }
            if (!found)
                cache.Add(newcache);
        }
    }
}
