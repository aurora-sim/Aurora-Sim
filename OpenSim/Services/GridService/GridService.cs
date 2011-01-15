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

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Nini.Config;
using log4net;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Data;
using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using OpenMetaverse;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace OpenSim.Services.GridService
{
    public class GridService : IGridService, IService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_DeleteOnUnregister = true;
        private static GridService m_RootInstance = null;
        protected IConfigSource m_config;
        protected static HypergridLinker m_HypergridLinker;
        protected Aurora.Framework.IEstateConnector m_EstateConnector;
        protected IRegionData m_Database = null;

        protected IAuthenticationService m_AuthenticationService = null;
        protected bool m_AllowDuplicateNames = false;
        protected bool m_AllowHypergridMapSearch = false;
        protected bool m_UseSessionID = true;
        protected Dictionary<UUID, UUID> GridSessionIDs = new Dictionary<UUID, UUID>();

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            string dllName = String.Empty;
            string connString = String.Empty;
            string realm = "regions";

            //
            // Try reading the [DatabaseService] section, if it exists
            //
            IConfig dbConfig = config.Configs["DatabaseService"];
            if (dbConfig != null)
            {
                if (dllName == String.Empty)
                    dllName = dbConfig.GetString("StorageProvider", String.Empty);
                if (connString == String.Empty)
                    connString = dbConfig.GetString("ConnectionString", String.Empty);
            }

            //
            // [GridService] section overrides [DatabaseService], if it exists
            //
            IConfig gridConfig = config.Configs["GridService"];
            if (gridConfig != null)
            {
                dllName = gridConfig.GetString("StorageProvider", dllName);
                connString = gridConfig.GetString("ConnectionString", connString);
                realm = gridConfig.GetString("Realm", realm);
            }

            //
            // We tried, but this doesn't exist. We can't proceed.
            //
            if (dllName.Equals(String.Empty))
                throw new Exception("No StorageProvider configured");

            m_Database = Aurora.DataManager.DataManager.RequestPlugin<IRegionData>();
            if(m_Database == null)
                m_Database = AuroraModuleLoader.LoadPlugin<IRegionData>(dllName, new Object[] { connString, realm });
            if (m_Database == null)
                throw new Exception("Could not find a storage interface in the given module");

            //m_log.DebugFormat("[GRID SERVICE]: Starting...");

            m_config = config;
            if (gridConfig != null)
            {
                m_DeleteOnUnregister = gridConfig.GetBoolean("DeleteOnUnregister", true);
                m_UseSessionID = !gridConfig.GetBoolean("DisableSessionID", false);
                m_AllowDuplicateNames = gridConfig.GetBoolean("AllowDuplicateNames", m_AllowDuplicateNames);
                m_AllowHypergridMapSearch = gridConfig.GetBoolean("AllowHypergridMapSearch", m_AllowHypergridMapSearch);
            }

            if (m_RootInstance == null)
            {
                m_RootInstance = this;

                if (MainConsole.Instance != null)
                {
                    MainConsole.Instance.Commands.AddCommand("grid", true,
                            "show region",
                            "show region <Region name>",
                            "Show details on a region",
                            String.Empty,
                            HandleShowRegion);

                    MainConsole.Instance.Commands.AddCommand("grid", true,
                            "set region flags",
                            "set region flags <Region name> <flags>",
                            "Set database flags for region",
                            String.Empty,
                            HandleSetFlags);
                }
                m_HypergridLinker = new HypergridLinker(m_config, this, m_Database);
            }
            registry.RegisterModuleInterface<IGridService>(this);
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostStart(IConfigSource config, IRegistryCore registry)
        {
            m_EstateConnector = Aurora.DataManager.DataManager.RequestPlugin<Aurora.Framework.IEstateConnector>();
            m_AuthenticationService = registry.RequestModuleInterface<IAuthenticationService>();
            m_HypergridLinker.PostInitialize(registry);
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
        }

        #region IGridService

        public string RegisterRegion(UUID scopeID, GridRegion regionInfos, UUID oldSessionID, out UUID SessionID)
        {
            SessionID = UUID.Zero;
            UUID NeedToDeletePreviousRegion = UUID.Zero;

            IConfig gridConfig = m_config.Configs["GridService"];
            // This needs better sanity testing. What if regionInfo is registering in
            // overlapping coords?
            GridRegion region = m_Database.Get(regionInfos.RegionLocX, regionInfos.RegionLocY, scopeID);
            if (region != null)
            {
                // There is a preexisting record
                //
                // Get it's flags
                //
                RegionFlags rflags = (RegionFlags)region.Flags;

                // Is this a reservation?
                //
                if ((rflags & RegionFlags.Reservation) != 0)
                {
                    // Regions reserved for the null key cannot be taken.
                    if (region.Token == UUID.Zero.ToString())
                        return "Region location is reserved";

                    // Treat it as an auth request
                    //
                    // NOTE: Fudging the flags value here, so these flags
                    //       should not be used elsewhere. Don't optimize
                    //       this with the later retrieval of the same flags!
                    rflags |= RegionFlags.Authenticate;
                }

                /*if ((rflags & RegionFlags.Authenticate) != 0)
                {
                    // Can we authenticate at all?
                    //
                    if (m_AuthenticationService == null)
                        return "No authentication possible";

                    if (!m_AuthenticationService.Verify(regionInfos., regionInfos.Token, 30))
                        return "Bad authentication";
                }*/
            }

            if ((region != null) && (region.RegionID != regionInfos.RegionID))
            {
                m_log.WarnFormat("[GRID SERVICE]: Region {0} tried to register in coordinates {1}, {2} which are already in use in scope {3}.",
                    regionInfos.RegionID, regionInfos.RegionLocX, regionInfos.RegionLocY, scopeID);
                return "Region overlaps another region";
            }
            if ((region != null) && (region.RegionID == regionInfos.RegionID) &&
                ((region.RegionLocX != regionInfos.RegionLocX) || (region.RegionLocY != regionInfos.RegionLocY)))
            {
                if ((region.Flags & (int)RegionFlags.NoMove) != 0)
                    return "Can't move this region," + region.RegionLocX + "," + region.RegionLocY;

                // Region reregistering in other coordinates. Delete the old entry
                m_log.DebugFormat("[GRID SERVICE]: Region {0} ({1}) was previously registered at {2}-{3}. Deleting old entry.",
                    regionInfos.RegionName, regionInfos.RegionID, regionInfos.RegionLocX, regionInfos.RegionLocY);

                NeedToDeletePreviousRegion = regionInfos.RegionID;
            }

            if (!m_AllowDuplicateNames)
            {
                List<GridRegion> dupe = m_Database.Get(regionInfos.RegionName, scopeID);
                if (dupe != null && dupe.Count > 0)
                {
                    foreach (GridRegion d in dupe)
                    {
                        if (d.RegionID != regionInfos.RegionID)
                        {
                            m_log.WarnFormat("[GRID SERVICE]: Region {0} tried to register duplicate name with ID {1}.",
                                regionInfos.RegionName, regionInfos.RegionID);
                            return "Duplicate region name";
                        }
                    }
                }
            }

            regionInfos.ScopeID = scopeID;

            if (region != null)
            {
                int oldFlags = region.Flags;
                if ((oldFlags & (int)RegionFlags.LockedOut) != 0)
                    return "Region locked out";

                oldFlags &= ~(int)RegionFlags.Reservation;

                regionInfos.Flags = oldFlags; // Preserve flags
            }
            else
            {
                regionInfos.Flags = 0;
                if ((gridConfig != null) && regionInfos.RegionName != string.Empty)
                {
                    int newFlags = 0;
                    string regionName = regionInfos.RegionName.Trim().Replace(' ', '_');
                    newFlags = ParseFlags(newFlags, gridConfig.GetString("DefaultRegionFlags", String.Empty));
                    newFlags = ParseFlags(newFlags, gridConfig.GetString("Region_" + regionName, String.Empty));
                    newFlags = ParseFlags(newFlags, gridConfig.GetString("Region_" + regionInfos.RegionID.ToString(), String.Empty));
                    regionInfos.Flags = newFlags;
                }
            }

            regionInfos.Flags |= (int)RegionFlags.RegionOnline;
            regionInfos.Flags |= (int)RegionFlags.Safe;
            regionInfos.LastSeen = Util.UnixTimeSinceEpoch();

            if (region != null)
            {
                //If we already have a session, we need to check it
                if (m_UseSessionID && region.SessionID != oldSessionID)
                {
                    m_log.Warn("[GRID SERVICE]: Region called register, but the sessionID they provided is wrong!");
                    return "Wrong Session ID";
                }
            }

            //Update the sessionID, use the old so that we don't generate a bunch of these
            if (oldSessionID == UUID.Zero)
                SessionID = UUID.Random();
            else
                SessionID = oldSessionID;
            regionInfos.SessionID = SessionID;

            // Everything is ok, let's register
            try
            {
                if (NeedToDeletePreviousRegion != UUID.Zero)
                    m_Database.Delete(NeedToDeletePreviousRegion);

                m_Database.Store(regionInfos);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[GRID SERVICE]: Database exception: {0}", e.ToString());
            }

            //m_log.DebugFormat("[GRID SERVICE]: Region {0} ({1}) registered successfully at {2}-{3}",
            //    regionInfos.RegionName, regionInfos.RegionID, regionInfos.RegionLocX, regionInfos.RegionLocY);

            return String.Empty;
        }

        public string UpdateMap(UUID scopeID, GridRegion gregion, UUID sessionID)
        {
            GridRegion region = m_Database.Get(gregion.RegionID, scopeID);
            if (region != null)
            {
                if (m_UseSessionID && region.SessionID != sessionID)
                {
                    m_log.Warn("[GRID SERVICE]: Region called UpdateMap, but provided incorrect SessionID! Possible attempt to disable a region!!");
                    return "Wrong Session ID";
                }

                m_log.DebugFormat("[GRID SERVICE]: Region {0} updated its map", gregion.RegionID);
                
                m_Database.Delete(gregion.RegionID);

                region.Flags |= (int)RegionFlags.RegionOnline;

                region.TerrainImage = gregion.TerrainImage;
                region.TerrainMapImage = gregion.TerrainMapImage;
                region.SessionID = sessionID;

                try
                {
                    region.LastSeen = Util.UnixTimeSinceEpoch();
                    m_Database.Store(region);
                }
                catch (Exception e)
                {
                    m_log.DebugFormat("[GRID SERVICE]: Database exception: {0}", e);
                }
            }

            return "";
        }

        public bool DeregisterRegion(UUID regionID, UUID SessionID)
        {
            GridRegion region = m_Database.Get(regionID, UUID.Zero);
            if (region == null)
                return false;

            if (m_UseSessionID && region.SessionID != SessionID)
            {
                m_log.Warn("[GRID SERVICE]: Region called deregister, but provided incorrect SessionID! Possible attempt to disable a region!!");
                return false;
            }

            m_log.DebugFormat("[GRID SERVICE]: Region {0} deregistered", regionID);
            
            if (!m_DeleteOnUnregister || (region.Flags & (int)RegionFlags.Persistent) != 0)
            {
                region.Flags &= ~(int)RegionFlags.RegionOnline;
                region.LastSeen = Util.UnixTimeSinceEpoch();
                try
                {
                    m_Database.Store(region);
                }
                catch (Exception e)
                {
                    m_log.DebugFormat("[GRID SERVICE]: Database exception: {0}", e);
                }

                return true;

            }

            return m_Database.Delete(regionID);
        }

        public List<GridRegion> GetNeighbours(UUID scopeID, UUID regionID)
        {
            List<GridRegion> rinfos = new List<GridRegion>();
            GridRegion region = m_Database.Get(regionID, scopeID);
            if (region != null)
            {
                // Not really? Maybe?
                List<GridRegion> rdatas = m_Database.Get(region.RegionLocX - (int)Constants.RegionSize - 1, region.RegionLocY - (int)Constants.RegionSize - 1, 
                    region.RegionLocX + (int)Constants.RegionSize + 1, region.RegionLocY + (int)Constants.RegionSize + 1, scopeID);

                foreach (GridRegion rdata in rdatas)
                    if (rdata.RegionID != regionID)
                    {
                        if ((rdata.Flags & (int)RegionFlags.Hyperlink) == 0) // no hyperlinks as neighbours
                            rinfos.Add(rdata);
                    }

                m_log.DebugFormat("[GRID SERVICE]: region {0} has {1} neighours", region.RegionName, rinfos.Count);
            }
            return rinfos;
        }

        public GridRegion GetRegionByUUID(UUID scopeID, UUID regionID)
        {
            return m_Database.Get(regionID, scopeID);
        }

        public GridRegion GetRegionByPosition(UUID scopeID, int x, int y)
        {
            return m_Database.Get(x, y, scopeID);
        }

        public GridRegion GetRegionByName(UUID scopeID, string regionName)
        {
            List<GridRegion> rdatas = m_Database.Get(regionName + "%", scopeID);
            if ((rdatas != null) && (rdatas.Count > 0))
            {
                //Sort to find the region with the exact name that was given
                rdatas.Sort(new RegionDataComparison(regionName));
                //Results are backwards... so it needs reversed
                rdatas.Reverse();
                return rdatas[0];
            }

            return null;
        }

        public List<GridRegion> GetRegionsByName(UUID scopeID, string name, int maxNumber)
        {
            m_log.DebugFormat("[GRID SERVICE]: GetRegionsByName {0}", name);

            int count = 0;
            List<GridRegion> rinfos = new List<GridRegion>();
            List<GridRegion> rdatas = m_Database.Get("%" + name + "%", scopeID);

            if (rdatas != null)
            {
                //Sort to find the region with the exact name that was given
                rdatas.Sort(new RegionDataComparison(name));
                //Results are backwards... so it needs reversed
                rdatas.Reverse();
                foreach (GridRegion rdata in rdatas)
                {
                    if (count++ < maxNumber)
                    {
                        rinfos.Add(rdata);
                    }
                }
            }

            if (m_AllowHypergridMapSearch && (rdatas == null || (rdatas != null && rdatas.Count == 0)) && name.Contains("."))
            {
                GridRegion r = m_HypergridLinker.LinkRegion(scopeID, name);
                if (r != null)
                    rinfos.Add(r);
            }

            return rinfos;
        }

        public class RegionDataComparison : IComparer<GridRegion>
        {
            string RegionName;
            public RegionDataComparison(string regionName)
            {
                RegionName = regionName;
            }

            int IComparer<GridRegion>.Compare(GridRegion x, GridRegion y)
            {
                if (x.RegionName == RegionName)
                    return 1;
                else if (y.RegionName == RegionName)
                    return -1;
                else
                    return 0;
            }
        }

        public List<GridRegion> GetRegionRange(UUID scopeID, int xmin, int xmax, int ymin, int ymax)
        {
            return m_Database.Get(xmin, ymin, xmax, ymax, scopeID);
        }

        #endregion

        public List<GridRegion> GetDefaultRegions(UUID scopeID)
        {
            List<GridRegion> ret = new List<GridRegion>();
            List<GridRegion> regions = m_Database.GetDefaultRegions(scopeID);

            foreach (GridRegion r in regions)
            {
                if ((r.Flags & (int)RegionFlags.RegionOnline) != 0)
                    ret.Add(r);
            }

            m_log.DebugFormat("[GRID SERVICE]: GetDefaultRegions returning {0} regions", ret.Count);
            return ret;
        }

        /// <summary>
        /// Attempts to find regions that are good for the agent to login to if the default and fallback regions are down.
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public List<GridRegion> GetSafeRegions(UUID scopeID, int x, int y)
        {
            return m_Database.GetSafeRegions(scopeID, x, y);
        }

        /// <summary>
        /// Tells the grid server that this region is not able to be connected to.
        /// This updates the down flag in the map and blocks it from becoming a 'safe' region fallback
        /// Only called by LLLoginService
        /// </summary>
        /// <param name="r"></param>
        public void SetRegionUnsafe(UUID ID)
        {
            GridRegion data = m_Database.Get(ID, UUID.Zero);
            if ((data.Flags & (int)RegionFlags.Safe) != (int)RegionFlags.Safe)
                data.Flags &= (int)RegionFlags.Safe; //Remove the safe var
            if ((data.Flags & (int)RegionFlags.RegionOnline) != (int)RegionFlags.RegionOnline)
                data.Flags &= (int)RegionFlags.RegionOnline; //Remove online too
            m_Database.Store(data);
        }

        public List<GridRegion> GetFallbackRegions(UUID scopeID, int x, int y)
        {
            List<GridRegion> ret = new List<GridRegion>();

            List<GridRegion> regions = m_Database.GetFallbackRegions(scopeID, x, y);

            foreach (GridRegion r in regions)
            {
                if ((r.Flags & (int)RegionFlags.RegionOnline) != 0)
                    ret.Add(r);
            }

            m_log.DebugFormat("[GRID SERVICE]: Fallback returned {0} regions", ret.Count);
            return ret;
        }

        public List<GridRegion> GetHyperlinks(UUID scopeID)
        {
            List<GridRegion> ret = new List<GridRegion>();

            List<GridRegion> regions = m_Database.GetHyperlinks(scopeID);

            foreach (GridRegion r in regions)
            {
                if ((r.Flags & (int)RegionFlags.RegionOnline) != 0)
                    ret.Add(r);
            }

            m_log.DebugFormat("[GRID SERVICE]: Hyperlinks returned {0} regions", ret.Count);
            return ret;
        }
        
        public int GetRegionFlags(UUID scopeID, UUID regionID)
        {
            GridRegion region = m_Database.Get(regionID, scopeID);

            if (region != null)
            {
                //m_log.DebugFormat("[GRID SERVICE]: Request for flags of {0}: {1}", regionID, flags);
                return region.Flags;
            }
            else
                return -1;
        }

        private void HandleShowRegion(string module, string[] cmd)
        {
            if (cmd.Length != 3)
            {
                MainConsole.Instance.Output("Syntax: show region <region name>");
                return;
            }
            List<GridRegion> regions = m_Database.Get(cmd[2], UUID.Zero);
            if (regions == null || regions.Count < 1)
            {
                MainConsole.Instance.Output("Region not found");
                return;
            }

            foreach (GridRegion r in regions)
            {
                MainConsole.Instance.Output("-------------------------------------------------------------------------------");
                RegionFlags flags = (RegionFlags)Convert.ToInt32(r.Flags);
                MainConsole.Instance.Output("Region Name: " + r.RegionName);
                MainConsole.Instance.Output("Region UUID: " + r.RegionID);
                MainConsole.Instance.Output("Region Location: " + String.Format("{0},{1}", r.RegionLocX, r.RegionLocY));
                MainConsole.Instance.Output("Region URI: " + r.ServerURI);
                MainConsole.Instance.Output("Region Owner: " + r.EstateOwner);
                MainConsole.Instance.Output("Region Flags: " + flags);
                MainConsole.Instance.Output("-------------------------------------------------------------------------------");
            }
            return;
        }

        private int ParseFlags(int prev, string flags)
        {
            RegionFlags f = (RegionFlags)prev;

            string[] parts = flags.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string p in parts)
            {
                int val;

                try
                {
                    if (p.StartsWith("+"))
                    {
                        val = (int)Enum.Parse(typeof(RegionFlags), p.Substring(1));
                        f |= (RegionFlags)val;
                    }
                    else if (p.StartsWith("-"))
                    {
                        val = (int)Enum.Parse(typeof(RegionFlags), p.Substring(1));
                        f &= ~(RegionFlags)val;
                    }
                    else
                    {
                        val = (int)Enum.Parse(typeof(RegionFlags), p);
                        f |= (RegionFlags)val;
                    }
                }
                catch (Exception)
                {
                    MainConsole.Instance.Output("Error in flag specification: " + p);
                }
            }

            return (int)f;
        }

        private void HandleSetFlags(string module, string[] cmd)
        {
            if (cmd.Length < 5)
            {
                MainConsole.Instance.Output("Syntax: set region flags <region name> <flags>");
                return;
            }

            List<GridRegion> regions = m_Database.Get(cmd[3], UUID.Zero);
            if (regions == null || regions.Count < 1)
            {
                MainConsole.Instance.Output("Region not found");
                return;
            }

            foreach (GridRegion r in regions)
            {
                int flags = r.Flags;
                flags = ParseFlags(flags, cmd[4]);
                r.Flags = flags;
                RegionFlags f = (RegionFlags)flags;

                MainConsole.Instance.Output(String.Format("Set region {0} to {1}", r.RegionName, f));
                m_Database.Store(r);
            }
        }

        public void AddAgent(UUID regionID, UUID agentID, Vector3 Position)
        {
            SimMap map = GetSimMap(regionID);

            Position.X = NormalizePosition(Position.X);
            Position.Y = NormalizePosition(Position.Y);
            Position.Z = NormalizePosition(Position.Z);

            if (!map.AgentPosition.ContainsKey(agentID))
                map.NumberOfAgents += 1;

            map.AgentPosition[agentID] = Position;
            SetSimMap(regionID, map);
        }

        private Dictionary<UUID, SimMap> SimMapCache = new Dictionary<UUID, SimMap>();

        private void SetSimMap(UUID RegionID, SimMap map)
        {
            SimMapCache[RegionID] = map;
        }

        private SimMap GetSimMap(UUID regionID)
        {
            SimMap map;
            if (!SimMapCache.TryGetValue(regionID, out map))
                map = new SimMap();
            return map;
        }

        private float NormalizePosition(float number)
        {
            try
            {
                double n = Math.Round(number, 0); //Remove the decimal
                string Number = n.ToString();//Round the last

                string first = Number.Remove(Number.Length - 1);
                if (first == "")
                    return 0;
                int FirstNumber = 0;
                if (first.StartsWith("."))
                    FirstNumber = 0;
                else
                    FirstNumber = int.Parse(first);

                string endNumber = Number.Remove(0, Number.Length - 1);
                if (endNumber == "")
                    return 0;
                float EndNumber = float.Parse(endNumber);
                if (EndNumber < 2.5f)
                    EndNumber = 0;
                else if (EndNumber > 7.5)
                {
                    EndNumber = 0;
                    FirstNumber++;
                }
                else
                    EndNumber = 5;
                return float.Parse(FirstNumber + EndNumber.ToString());
            }
            catch(Exception ex)
            {
                m_log.Error("[GridService]: Error in NormalizePosition " + ex);
            }
            return 0;
        }

        public void RemoveAgent(UUID regionID, UUID agentID)
        {
            SimMap map = GetSimMap(regionID);

            //Remove the agent's location from memory
            if (map.AgentPosition.Remove(agentID))
                map.NumberOfAgents -= 1;

            SetSimMap(regionID, map);
        }

        public multipleMapItemReply GetMapItems(ulong regionHandle, GridItemType gridItemType)
        {
            multipleMapItemReply allItems = new multipleMapItemReply();
            if (gridItemType == GridItemType.AgentLocations)
            {
                uint X;
                uint Y;
                Utils.LongToUInts(regionHandle, out X, out Y);
                allItems.items[regionHandle] = GetItems((int)X, (int)Y);
                for (uint x = X; x < 8; x++)
                {
                    for (uint y = Y; y < 8; y++)
                    {
                        allItems.items[Utils.UIntsToLong(x, y)] = GetItems((int)x, (int)y);
                    }
                }
            }
            return allItems;
        }

        private List<mapItemReply> GetItems(int X, int Y)
        {
            GridRegion region = GetRegionByPosition(UUID.Zero, X, Y);
            if (region == null || region.Access == (byte)SimAccess.Down || region.Access == (byte)SimAccess.NonExistent)
                return new List<mapItemReply>();
            SimMap map = GetSimMap(region.RegionID);

            List<mapItemReply> mapItems = new List<mapItemReply>();
            Dictionary<Vector3, int> Positions = new Dictionary<Vector3, int>();
            foreach (Vector3 position in map.AgentPosition.Values)
            {
                int Number = 0;
                if (!Positions.TryGetValue(position, out Number))
                    Number = 0;
                Positions[position] = Number;
            }
            foreach (KeyValuePair<Vector3, int> position in Positions)
            {
                mapItemReply mapitem = new mapItemReply();
                mapitem.x = (uint)(region.RegionLocX + position.Key.X);
                mapitem.y = (uint)(region.RegionLocY + position.Key.Y);
                mapitem.id = UUID.Zero;
                mapitem.name = Util.Md5Hash(region.RegionName + Environment.TickCount.ToString());
                mapitem.Extra = (int)position.Value;
                mapitem.Extra2 = 0;
                mapItems.Add(mapitem);
            }

            if (mapItems.Count == 0)
            {
                mapItemReply mapitem = new mapItemReply();
                mapitem.x = (uint)(region.RegionLocX + 1);
                mapitem.y = (uint)(region.RegionLocY + 1);
                mapitem.id = UUID.Zero;
                mapitem.name = Util.Md5Hash(region.RegionName + Environment.TickCount.ToString());
                mapitem.Extra = 0;
                mapitem.Extra2 = 0;
                mapItems.Add(mapitem);
            }
            return mapItems;
        }

        public string GridServiceURL
        {
            get { return "Local"; }
        }

        public class SimMap
        {
            public uint NumberOfAgents;

            //These things should not be sent to the region
            public Dictionary<UUID, Vector3> AgentPosition = new Dictionary<UUID, Vector3>();
        }
    }
}
