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
            registry.RegisterInterface<IGridService>(this);
        }

        public void PostInitialize(IRegistryCore registry)
        {
            m_EstateConnector = Aurora.DataManager.DataManager.RequestPlugin<Aurora.Framework.IEstateConnector>();
            m_AuthenticationService = registry.Get<IAuthenticationService>();
            m_HypergridLinker.PostInitialize(registry);
        }

        #region IGridService

        public string RegisterRegion(UUID scopeID, GridRegion regionInfos, UUID oldSessionID, out UUID SessionID)
        {
            SessionID = UUID.Zero;
            UUID NeedToDeletePreviousRegion = UUID.Zero;

            IConfig gridConfig = m_config.Configs["GridService"];
            // This needs better sanity testing. What if regionInfo is registering in
            // overlapping coords?
            RegionData region = m_Database.Get(regionInfos.RegionLocX, regionInfos.RegionLocY, scopeID);
            if (region != null)
            {
                // There is a preexisting record
                //
                // Get it's flags
                //
                OpenSim.Data.RegionFlags rflags = (OpenSim.Data.RegionFlags)Convert.ToInt32(region.Data["flags"]);

                // Is this a reservation?
                //
                if ((rflags & OpenSim.Data.RegionFlags.Reservation) != 0)
                {
                    // Regions reserved for the null key cannot be taken.
                    if ((string)region.Data["PrincipalID"] == UUID.Zero.ToString())
                        return "Region location is reserved";

                    // Treat it as an auth request
                    //
                    // NOTE: Fudging the flags value here, so these flags
                    //       should not be used elsewhere. Don't optimize
                    //       this with the later retrieval of the same flags!
                    rflags |= OpenSim.Data.RegionFlags.Authenticate;
                }

                if ((rflags & OpenSim.Data.RegionFlags.Authenticate) != 0)
                {
                    // Can we authenticate at all?
                    //
                    if (m_AuthenticationService == null)
                        return "No authentication possible";

                    if (!m_AuthenticationService.Verify(new UUID(region.Data["PrincipalID"].ToString()), regionInfos.Token, 30))
                        return "Bad authentication";
                }
            }

            if ((region != null) && (region.RegionID != regionInfos.RegionID))
            {
                m_log.WarnFormat("[GRID SERVICE]: Region {0} tried to register in coordinates {1}, {2} which are already in use in scope {3}.",
                    regionInfos.RegionID, regionInfos.RegionLocX, regionInfos.RegionLocY, scopeID);
                return "Region overlaps another region";
            }
            if ((region != null) && (region.RegionID == regionInfos.RegionID) &&
                ((region.posX != regionInfos.RegionLocX) || (region.posY != regionInfos.RegionLocY)))
            {
                if ((Convert.ToInt32(region.Data["flags"]) & (int)OpenSim.Data.RegionFlags.NoMove) != 0)
                    return "Can't move this region," + region.posX + "," + region.posY;

                // Region reregistering in other coordinates. Delete the old entry
                m_log.DebugFormat("[GRID SERVICE]: Region {0} ({1}) was previously registered at {2}-{3}. Deleting old entry.",
                    regionInfos.RegionName, regionInfos.RegionID, regionInfos.RegionLocX, regionInfos.RegionLocY);

                NeedToDeletePreviousRegion = regionInfos.RegionID;
            }

            if (!m_AllowDuplicateNames)
            {
                List<RegionData> dupe = m_Database.Get(regionInfos.RegionName, scopeID);
                if (dupe != null && dupe.Count > 0)
                {
                    foreach (RegionData d in dupe)
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

            RegionData rdata = RegionInfo2RegionData(regionInfos);
            rdata.ScopeID = scopeID;

            if (region != null)
            {
                int oldFlags = Convert.ToInt32(region.Data["flags"]);
                if ((oldFlags & (int)OpenSim.Data.RegionFlags.LockedOut) != 0)
                    return "Region locked out";

                oldFlags &= ~(int)OpenSim.Data.RegionFlags.Reservation;

                rdata.Data["flags"] = oldFlags.ToString(); // Preserve flags
            }
            else
            {
                rdata.Data["flags"] = "0";
                if ((gridConfig != null) && rdata.RegionName != string.Empty)
                {
                    int newFlags = 0;
                    string regionName = rdata.RegionName.Trim().Replace(' ', '_');
                    newFlags = ParseFlags(newFlags, gridConfig.GetString("DefaultRegionFlags", String.Empty));
                    newFlags = ParseFlags(newFlags, gridConfig.GetString("Region_" + regionName, String.Empty));
                    newFlags = ParseFlags(newFlags, gridConfig.GetString("Region_" + rdata.RegionID.ToString(), String.Empty));
                    rdata.Data["flags"] = newFlags.ToString();
                }
            }

            int flags = Convert.ToInt32(rdata.Data["flags"]);
            flags |= (int)OpenSim.Data.RegionFlags.RegionOnline;
            flags |= (int)OpenSim.Data.RegionFlags.Safe;
            rdata.Data["flags"] = flags.ToString();
            rdata.Data["last_seen"] = Util.UnixTimeSinceEpoch();

            if (region != null)
            {
                //If we already have a session, we need to check it
                if (m_UseSessionID && region.Data["sessionid"].ToString() != oldSessionID.ToString())
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
            rdata.Data["sessionid"] = SessionID.ToString();

            // Everything is ok, let's register
            try
            {
                if (NeedToDeletePreviousRegion != UUID.Zero)
                    m_Database.Delete(NeedToDeletePreviousRegion);

                m_Database.Store(rdata);
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
            RegionData region = m_Database.Get(gregion.RegionID, scopeID);
            if (region != null)
            {
                if (m_UseSessionID && region.Data["sessionid"].ToString() != sessionID.ToString())
                {
                    m_log.Warn("[GRID SERVICE]: Region called UpdateMap, but provided incorrect SessionID! Possible attempt to disable a region!!");
                    return "Wrong Session ID";
                }

                m_log.DebugFormat("[GRID SERVICE]: Region {0} updated its map", gregion.RegionID);
                
                m_Database.Delete(gregion.RegionID);

                int oldFlags = Convert.ToInt32(region.Data["flags"]);
                oldFlags |= (int)OpenSim.Data.RegionFlags.RegionOnline;
                region.Data["flags"] = oldFlags.ToString(); // Preserve flags

                region.Data["regionMapTexture"] = gregion.TerrainImage.ToString();
                region.Data["regionTerrainTexture"] = gregion.TerrainMapImage.ToString();
                region.Data["sessionid"] = sessionID.ToString();

                try
                {
                    region.Data["last_seen"] = Util.UnixTimeSinceEpoch();
                    region.Data.Remove("locZ");
                    region.Data.Remove("eastOverrideHandle");
                    region.Data.Remove("westOverrideHandle");
                    region.Data.Remove("southOverrideHandle");
                    region.Data.Remove("northOverrideHandle");
                    region.Data.Remove("serverRemotingPort");
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
            RegionData region = m_Database.Get(regionID, UUID.Zero);
            if (region == null)
                return false;

            if (m_UseSessionID && region.Data["sessionid"].ToString() != SessionID.ToString())
            {
                m_log.Warn("[GRID SERVICE]: Region called deregister, but provided incorrect SessionID! Possible attempt to disable a region!!");
                return false;
            }

            m_log.DebugFormat("[GRID SERVICE]: Region {0} deregistered", regionID);
            
            int flags = Convert.ToInt32(region.Data["flags"]);

            if (!m_DeleteOnUnregister || (flags & (int)OpenSim.Data.RegionFlags.Persistent) != 0)
            {
                flags &= ~(int)OpenSim.Data.RegionFlags.RegionOnline;
                region.Data["flags"] = flags.ToString();
                region.Data["last_seen"] = Util.UnixTimeSinceEpoch();
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
            RegionData region = m_Database.Get(regionID, scopeID);
            if (region != null)
            {
                // Not really? Maybe?
                List<RegionData> rdatas = m_Database.Get(region.posX - (int)Constants.RegionSize - 1, region.posY - (int)Constants.RegionSize - 1, 
                    region.posX + (int)Constants.RegionSize + 1, region.posY + (int)Constants.RegionSize + 1, scopeID);

                foreach (RegionData rdata in rdatas)
                    if (rdata.RegionID != regionID)
                    {
                        int flags = Convert.ToInt32(rdata.Data["flags"]);
                        if ((flags & (int)Data.RegionFlags.Hyperlink) == 0) // no hyperlinks as neighbours
                            rinfos.Add(RegionData2RegionInfo(rdata));
                    }

                m_log.DebugFormat("[GRID SERVICE]: region {0} has {1} neighours", region.RegionName, rinfos.Count);
            }
            return rinfos;
        }

        public GridRegion GetRegionByUUID(UUID scopeID, UUID regionID)
        {
            RegionData rdata = m_Database.Get(regionID, scopeID);
            if (rdata != null)
                return RegionData2RegionInfo(rdata);

            return null;
        }

        public GridRegion GetRegionByPosition(UUID scopeID, int x, int y)
        {
            int snapX = (int)(x / Constants.RegionSize) * (int)Constants.RegionSize;
            int snapY = (int)(y / Constants.RegionSize) * (int)Constants.RegionSize;
            RegionData rdata = m_Database.Get(snapX, snapY, scopeID);
            if (rdata != null)
                return RegionData2RegionInfo(rdata);

            return null;
        }

        public GridRegion GetRegionByName(UUID scopeID, string regionName)
        {
            List<RegionData> rdatas = m_Database.Get(regionName + "%", scopeID);
            if ((rdatas != null) && (rdatas.Count > 0))
            {
                //Sort to find the region with the exact name that was given
                rdatas.Sort(new RegionDataComparison(regionName));
                //Results are backwards... so it needs reversed
                rdatas.Reverse();
                return RegionData2RegionInfo(rdatas[0]);
            }

            return null;
        }

        public List<GridRegion> GetRegionsByName(UUID scopeID, string name, int maxNumber)
        {
            m_log.DebugFormat("[GRID SERVICE]: GetRegionsByName {0}", name);

            int count = 0;
            List<GridRegion> rinfos = new List<GridRegion>();
            List<RegionData> rdatas = m_Database.Get("%" + name + "%", scopeID);

            if (rdatas != null)
            {
                //Sort to find the region with the exact name that was given
                rdatas.Sort(new RegionDataComparison(name));
                //Results are backwards... so it needs reversed
                rdatas.Reverse();
                foreach (RegionData rdata in rdatas)
                {
                    if (count++ < maxNumber)
                    {
                        GridRegion region = RegionData2RegionInfo(rdata);
                        if(region != null)
                            rinfos.Add(region);
                    }
                }
            }

            if (m_AllowHypergridMapSearch && (rdatas == null || (rdatas != null && rdatas.Count == 0)) && name.Contains("."))
            {
                if (m_HypergridLinker == null)
                    m_HypergridLinker = new HypergridLinker(m_config, this, m_Database);

                GridRegion r = m_HypergridLinker.LinkRegion(scopeID, name);
                if (r != null)
                    rinfos.Add(r);
            }

            return rinfos;
        }

        public class RegionDataComparison : IComparer<RegionData>
        {
            string RegionName;
            public RegionDataComparison(string regionName)
            {
                RegionName = regionName;
            }

            int IComparer<RegionData>.Compare(RegionData x, RegionData y)
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
            int xminSnap = (int)(xmin / Constants.RegionSize) * (int)Constants.RegionSize;
            int xmaxSnap = (int)(xmax / Constants.RegionSize) * (int)Constants.RegionSize;
            int yminSnap = (int)(ymin / Constants.RegionSize) * (int)Constants.RegionSize;
            int ymaxSnap = (int)(ymax / Constants.RegionSize) * (int)Constants.RegionSize;

            List<RegionData> rdatas = m_Database.Get(xminSnap, yminSnap, xmaxSnap, ymaxSnap, scopeID);
            List<GridRegion> rinfos = new List<GridRegion>();
            foreach (RegionData rdata in rdatas)
                rinfos.Add(RegionData2RegionInfo(rdata));

            return rinfos;
        }

        #endregion

        #region Data structure conversions

        public RegionData RegionInfo2RegionData(GridRegion rinfo)
        {
            RegionData rdata = new RegionData();
            rdata.posX = (int)rinfo.RegionLocX;
            rdata.posY = (int)rinfo.RegionLocY;
            rdata.RegionID = rinfo.RegionID;
            rdata.RegionName = rinfo.RegionName;
            rdata.Data = rinfo.ToKeyValuePairs();
            rdata.Data["regionHandle"] = Utils.UIntsToLong((uint)rdata.posX, (uint)rdata.posY);
            rdata.Data["owner_uuid"] = rinfo.EstateOwner.ToString();
            return rdata;
        }

        public GridRegion RegionData2RegionInfo(RegionData rdata)
        {
            GridRegion rinfo = new GridRegion(rdata.Data);
            rinfo.RegionLocX = rdata.posX;
            rinfo.RegionLocY = rdata.posY;
            rinfo.RegionID = rdata.RegionID;
            rinfo.RegionName = rdata.RegionName;
            rinfo.ScopeID = rdata.ScopeID;

            //Check whether it should be down
            if (rdata.Data.ContainsKey("last_seen"))
            {
                if (int.Parse(rdata.Data["last_seen"].ToString()) > Util.UnixTimeSinceEpoch() + (1000 * 6))
                {
                    rinfo.Access |= (int)SimAccess.Down;
                }
            }
            //Check the hidden flag
            int flags = Convert.ToInt32(rdata.Data["flags"]);

            if ((flags & (int)OpenSim.Data.RegionFlags.Hidden) != 0)
            {
                rinfo = null; //Cannot be found, only logged into directly
            }

            return rinfo;
        }

        #endregion

        public List<GridRegion> GetDefaultRegions(UUID scopeID)
        {
            List<GridRegion> ret = new List<GridRegion>();

            List<RegionData> regions = m_Database.GetDefaultRegions(scopeID);

            foreach (RegionData r in regions)
            {
                if ((Convert.ToInt32(r.Data["flags"]) & (int)OpenSim.Data.RegionFlags.RegionOnline) != 0)
                    ret.Add(RegionData2RegionInfo(r));
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
            List<GridRegion> ret = new List<GridRegion>();

            List<RegionData> regions = m_Database.GetSafeRegions(scopeID, x, y);

            foreach (RegionData r in regions)
            {
                ret.Add(RegionData2RegionInfo(r));
            }

            m_log.DebugFormat("[GRID SERVICE]: Safe returned {0} regions", ret.Count);
            return ret;
        }

        /// <summary>
        /// Tells the grid server that this region is not able to be connected to.
        /// This updates the down flag in the map and blocks it from becoming a 'safe' region fallback
        /// Only called by LLLoginService
        /// </summary>
        /// <param name="r"></param>
        public void SetRegionUnsafe(UUID ID)
        {
            RegionData data = m_Database.Get(ID, UUID.Zero);
            int flags = int.Parse(data.Data["flags"].ToString());
            if ((flags & (int)OpenSim.Data.RegionFlags.Safe) != (int)OpenSim.Data.RegionFlags.Safe)
                flags &= (int)OpenSim.Data.RegionFlags.Safe; //Remove the safe var
            if ((flags & (int)OpenSim.Data.RegionFlags.RegionOnline) != (int)OpenSim.Data.RegionFlags.RegionOnline)
                flags &= (int)OpenSim.Data.RegionFlags.RegionOnline; //Remove online too
            data.Data["flags"] = flags;
            m_Database.SetDataItem(ID, "flags", flags.ToString());
        }

        public List<GridRegion> GetFallbackRegions(UUID scopeID, int x, int y)
        {
            List<GridRegion> ret = new List<GridRegion>();

            List<RegionData> regions = m_Database.GetFallbackRegions(scopeID, x, y);

            foreach (RegionData r in regions)
            {
                if ((Convert.ToInt32(r.Data["flags"]) & (int)OpenSim.Data.RegionFlags.RegionOnline) != 0)
                    ret.Add(RegionData2RegionInfo(r));
            }

            m_log.DebugFormat("[GRID SERVICE]: Fallback returned {0} regions", ret.Count);
            return ret;
        }

        public List<GridRegion> GetHyperlinks(UUID scopeID)
        {
            List<GridRegion> ret = new List<GridRegion>();

            List<RegionData> regions = m_Database.GetHyperlinks(scopeID);

            foreach (RegionData r in regions)
            {
                if ((Convert.ToInt32(r.Data["flags"]) & (int)OpenSim.Data.RegionFlags.RegionOnline) != 0)
                    ret.Add(RegionData2RegionInfo(r));
            }

            m_log.DebugFormat("[GRID SERVICE]: Hyperlinks returned {0} regions", ret.Count);
            return ret;
        }
        
        public int GetRegionFlags(UUID scopeID, UUID regionID)
        {
            RegionData region = m_Database.Get(regionID, scopeID);

            if (region != null)
            {
                int flags = Convert.ToInt32(region.Data["flags"]);
                //m_log.DebugFormat("[GRID SERVICE]: Request for flags of {0}: {1}", regionID, flags);
                return flags;
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
            List<RegionData> regions = m_Database.Get(cmd[2], UUID.Zero);
            if (regions == null || regions.Count < 1)
            {
                MainConsole.Instance.Output("Region not found");
                return;
            }

            foreach (RegionData r in regions)
            {
                MainConsole.Instance.Output("-------------------------------------------------------------------------------");
                OpenSim.Data.RegionFlags flags = (OpenSim.Data.RegionFlags)Convert.ToInt32(r.Data["flags"]);
                MainConsole.Instance.Output("Region Name: " + r.RegionName);
                MainConsole.Instance.Output("Region UUID: " + r.RegionID);
                MainConsole.Instance.Output("Region Location: " + String.Format("{0},{1}", r.posX, r.posY));
                MainConsole.Instance.Output("Region URI: " + "http://" + r.Data["serverIP"].ToString() + ":" + r.Data["serverPort"].ToString());
                MainConsole.Instance.Output("Region Owner: " + r.Data["owner_uuid"].ToString());
                MainConsole.Instance.Output("Region Flags: " + flags);
                MainConsole.Instance.Output("-------------------------------------------------------------------------------");
            }
            return;
        }

        private int ParseFlags(int prev, string flags)
        {
            OpenSim.Data.RegionFlags f = (OpenSim.Data.RegionFlags)prev;

            string[] parts = flags.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string p in parts)
            {
                int val;

                try
                {
                    if (p.StartsWith("+"))
                    {
                        val = (int)Enum.Parse(typeof(OpenSim.Data.RegionFlags), p.Substring(1));
                        f |= (OpenSim.Data.RegionFlags)val;
                    }
                    else if (p.StartsWith("-"))
                    {
                        val = (int)Enum.Parse(typeof(OpenSim.Data.RegionFlags), p.Substring(1));
                        f &= ~(OpenSim.Data.RegionFlags)val;
                    }
                    else
                    {
                        val = (int)Enum.Parse(typeof(OpenSim.Data.RegionFlags), p);
                        f |= (OpenSim.Data.RegionFlags)val;
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

            List<RegionData> regions = m_Database.Get(cmd[3], UUID.Zero);
            if (regions == null || regions.Count < 1)
            {
                MainConsole.Instance.Output("Region not found");
                return;
            }

            foreach (RegionData r in regions)
            {
                int flags = Convert.ToInt32(r.Data["flags"]);
                flags = ParseFlags(flags, cmd[4]);
                r.Data["flags"] = flags.ToString();
                OpenSim.Data.RegionFlags f = (OpenSim.Data.RegionFlags)flags;

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
                int FirstNumber = int.Parse(first);
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
