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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using Aurora.Simulation.Base;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.Connectors
{
    public class NeighborPassword 
    {
        public UUID RegionID;
        public UUID Password;
    }
    public class NeighborServiceConnector : IService, INeighborService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        protected IGridService m_GridService = null;
        protected Dictionary<UUID, List<GridRegion>> m_KnownNeighbors = new Dictionary<UUID, List<GridRegion>>();
        protected Dictionary<UUID, List<NeighborPassword>> m_KnownNeighborsPass = new Dictionary<UUID, List<NeighborPassword>>();
        private LocalNeighborServiceConnector m_LocalService;

        public Dictionary<UUID, List<GridRegion>> Neighbors
        {
            get { return m_KnownNeighbors; }
        }

        #region INeighborService

        public virtual List<GridRegion> InformNeighborsThatRegionIsUp(RegionInfo incomingRegion)
        {
            List<GridRegion> nowInformedRegions = m_LocalService.InformNeighborsThatRegionIsUp(incomingRegion);

            //Get the known regions from the local connector, as it queried the grid service to find them all
            m_KnownNeighbors = m_LocalService.Neighbors;

            int RegionsNotInformed = m_KnownNeighbors[incomingRegion.RegionID].Count - nowInformedRegions.Count;

            //We informed all of them locally, so quit early
            if (RegionsNotInformed == 0)
                return nowInformedRegions;

            //Now add the remote ones and tell it which ones have already been informed locally so that it doesn't inform them twice
            nowInformedRegions.AddRange(InformNeighborsRegionIsUp(incomingRegion, nowInformedRegions));

            //Now check to see if we informed everyone
            RegionsNotInformed = m_KnownNeighbors[incomingRegion.RegionID].Count - nowInformedRegions.Count;
            if (RegionsNotInformed != 0)
            {
                m_log.Warn("[NeighborsService]: Failed to inform " + RegionsNotInformed + " neighbors remotely about a new neighbor.");
            }
            return nowInformedRegions;
        }

        public List<GridRegion> InformNeighborsRegionIsUp(RegionInfo incomingRegion, List<GridRegion> alreadyInformedRegions)
        {
            List<GridRegion> informedRegions = new List<GridRegion>();
            foreach (GridRegion neighbor in Neighbors[incomingRegion.RegionID])
            {
                //If we have already informed the region, don't tell it again
                if (alreadyInformedRegions.Contains(neighbor))
                    continue;
                //Call the region then and add the regions it informed
                informedRegions.AddRange(DoHelloNeighbourCall(neighbor, incomingRegion));
            }
            return informedRegions;
        }

        protected List<GridRegion> DoHelloNeighbourCall(GridRegion region, RegionInfo thisRegion)
        {
            List<GridRegion> informedRegions = new List<GridRegion>();
            string uri = "http://" + region.ExternalEndPoint.Address + ":" + region.HttpPort + "/region/" + thisRegion.RegionID + "/";
            //m_log.Debug("   >>> DoHelloNeighbourCall <<< " + uri);

            // Fill it in
            Dictionary<string, object> args = new Dictionary<string, object>();
            try
            {
                args = PackRegionInfo(thisRegion, region.RegionID);
            }
            catch (Exception e)
            {
                m_log.Debug("[REST COMMS]: PackRegionInfoData failed with exception: " + e.Message);
                return informedRegions;
            }
            args["METHOD"] = "inform_neighbors_region_is_up";

            string queryString = WebUtils.BuildQueryString(args);
            string reply = SynchronousRestFormsRequester.MakeRequest("POST", uri, queryString);

            if (reply == "")
                return informedRegions;

            Dictionary<string, object> response = WebUtils.ParseXmlResponse(reply);

            try
            {
                if (response == null)
                    return informedRegions;

                //Didn't inform, return now
                if (!response.ContainsKey("success") || response["success"].ToString() != "true")
                    return informedRegions;

                foreach (KeyValuePair<string, object> kvp in response)
                {
                    if (kvp.Value is Dictionary<string, object>)
                    {
                        Dictionary<string, object> r = kvp.Value as Dictionary<string, object>;
                        GridRegion nregion = new GridRegion(r);
                        informedRegions.Add(nregion);
                    }
                }
            }
            catch(Exception ex)
            {
                m_log.Warn("[NeighborServiceConnector]: Failed to read response from neighbor " + ex.ToString());
            }

            return informedRegions;
        }

        public List<GridRegion> InformNeighborsRegionIsDown(RegionInfo closingRegion, List<GridRegion> alreadyInformedRegions, List<GridRegion> neighbors)
        {
            List<GridRegion> informedRegions = new List<GridRegion>();
            foreach (GridRegion neighbor in neighbors)
            {
                //If we have already informed the region, don't tell it again
                if (alreadyInformedRegions.Contains(neighbor))
                    continue;
                //Call the region then and add the regions it informed
                informedRegions.AddRange(DoGoodbyeNeighbourCall(neighbor, closingRegion));
            }
            return informedRegions;
        }

        protected List<GridRegion> DoGoodbyeNeighbourCall(GridRegion region, RegionInfo thisRegion)
        {
            List<GridRegion> informedRegions = new List<GridRegion>();
            string uri = "http://" + region.ExternalEndPoint.Address + ":" + region.HttpPort + "/region/" + thisRegion.RegionID + "/";
            //m_log.Debug("   >>> DoHelloNeighbourCall <<< " + uri);

            // Fill it in
            Dictionary<string, object> args = new Dictionary<string, object>();
            try
            {
                args = PackRegionInfo(thisRegion, region.RegionID);
            }
            catch (Exception e)
            {
                m_log.Debug("[REST COMMS]: PackRegionInfoData failed with exception: " + e.Message);
                return informedRegions;
            }
            args["METHOD"] = "inform_neighbors_region_is_down";

            string queryString = WebUtils.BuildQueryString(args);
            string reply = SynchronousRestFormsRequester.MakeRequest("POST", uri, queryString);

            if (reply == "")
                return informedRegions;

            Dictionary<string, object> response = WebUtils.ParseXmlResponse(reply);

            try
            {
                if (response == null)
                    return informedRegions;

                //Didn't inform, return now
                if (!response.ContainsKey("success") || response["success"].ToString() != "true")
                    return informedRegions;

                foreach (KeyValuePair<string, object> kvp in response)
                {
                    if (kvp.Value is Dictionary<string, object>)
                    {
                        Dictionary<string, object> r = kvp.Value as Dictionary<string, object>;
                        GridRegion nregion = new GridRegion(r);
                        informedRegions.Add(nregion);
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.Warn("[NeighborServiceConnector]: Failed to read response from neighbor " + ex.ToString());
            }

            return informedRegions;
        }

        public virtual void SendChildAgentUpdate(AgentPosition childAgentUpdate, UUID regionID)
        {
            m_LocalService.SendChildAgentUpdate(childAgentUpdate, regionID);
        }

        public virtual List<GridRegion> InformNeighborsThatRegionIsDown(RegionInfo closingRegion)
        {
            List<GridRegion> neighbors = m_KnownNeighbors[closingRegion.RegionID];
            List<GridRegion> nowInformedRegions = m_LocalService.InformNeighborsThatRegionIsDown(closingRegion);

            int RegionsNotInformed = neighbors.Count - nowInformedRegions.Count;

            //We informed all of them locally, so quit early
            if (RegionsNotInformed == 0)
                return nowInformedRegions;

            //Now add the remote ones and tell it which ones have already been informed locally so that it doesn't inform them twice
            nowInformedRegions.AddRange(InformNeighborsRegionIsDown(closingRegion, nowInformedRegions, neighbors));

            //Now check to see if we informed everyone
            RegionsNotInformed = neighbors.Count - nowInformedRegions.Count;
            if (RegionsNotInformed != 0)
            {
                m_log.Warn("[NeighborsService]: Failed to inform " + RegionsNotInformed + " neighbors remotely about a closing neighbor.");
            }
            return nowInformedRegions;
        }

        public virtual bool SendChatMessageToNeighbors(OSChatMessage message, ChatSourceType type, RegionInfo region)
        {
            bool RetVal = false;
            List<GridRegion> NotifiedRegions = m_LocalService.SendChatMessageToNeighbors(message, type, region, out RetVal);

            if (m_KnownNeighbors.ContainsKey(region.RegionID))
            {
                int RegionsNotInformed = m_KnownNeighbors[region.RegionID].Count - NotifiedRegions.Count;

                //We informed all of them locally, so quit early
                if (RegionsNotInformed == 0)
                    return RetVal;

                //Now add the remote ones and tell it which ones have already been informed locally so that it doesn't inform them twice
                InformNeighborsOfChatMessage(message, type, region, NotifiedRegions, m_KnownNeighbors[region.RegionID]);
            }
            //This tells the chat module whether we should send the message in the region it originated from, and if it 
            return RetVal;
        }

        public virtual List<GridRegion> GetNeighbors(RegionInfo region)
        {
            List<GridRegion> neighbors = m_LocalService.GetNeighbors(region);
            if (neighbors.Count != 0)
                return neighbors;
            
            string uri = "http://" + region.ExternalEndPoint.Address + ":" + region.HttpPort + "/region/" + region.RegionID + "/";
            //m_log.Debug("   >>> DoHelloNeighbourCall <<< " + uri);

            // Fill it in
            Dictionary<string, object> args = new Dictionary<string, object>();
            try
            {
                args = PackRegionInfo(region, UUID.Zero);
            }
            catch (Exception e)
            {
                m_log.Debug("[REST COMMS]: PackRegionInfoData failed with exception: " + e.Message);
                return neighbors;
            }
            args["METHOD"] = "get_neighbors";

            string queryString = WebUtils.BuildQueryString(args);
            string reply = SynchronousRestFormsRequester.MakeRequest("POST", uri, queryString);

            if (reply == "")
                return neighbors;

            Dictionary<string, object> response = WebUtils.ParseXmlResponse(reply);

            try
            {
                if (response == null)
                    return neighbors;

                //Didn't inform, return now
                if (!response.ContainsKey("success") || response["success"].ToString() != "true")
                    return neighbors;

                foreach (KeyValuePair<string, object> kvp in response)
                {
                    if (kvp.Value is Dictionary<string, object>)
                    {
                        Dictionary<string, object> r = kvp.Value as Dictionary<string, object>;
                        GridRegion nregion = new GridRegion(r);
                        neighbors.Add(nregion);
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.Warn("[NeighborServiceConnector]: Failed to read response from neighbor " + ex.ToString());
            }

            return neighbors;
        }

        protected void InformNeighborsOfChatMessage(OSChatMessage message, ChatSourceType type, RegionInfo region, List<GridRegion> alreadyInformedRegions, List<GridRegion> neighbors)
        {
            foreach (GridRegion neighbor in neighbors)
            {
                //If we have already informed the region, don't tell it again
                if (alreadyInformedRegions.Contains(neighbor))
                    continue;
                //Call the region then and add the regions it informed
                InformNeighborOfChatMessage(message, type, neighbor, region);
            }
        }

        protected void InformNeighborOfChatMessage(OSChatMessage message, ChatSourceType type, GridRegion region, RegionInfo thisRegion)
        {
            string uri = "http://" + region.ExternalEndPoint.Address + ":" + region.HttpPort + "/region/" + thisRegion.RegionID + "/";
            //m_log.Debug("   >>> DoHelloNeighbourCall <<< " + uri);

            // Fill it in
            Dictionary<string, object> args = new Dictionary<string, object>();

            try
            {
                args = Util.OSDToDictionary(thisRegion.PackRegionInfoData());
            }
            catch (Exception e)
            {
                m_log.Debug("[REST COMMS]: PackRegionInfoData failed with exception: " + e.Message);
                return;
            }
            args["MESSAGE"] = WebUtils.BuildQueryString(message.ToKVP());
            args["TYPE"] = (int)type;
            args["METHOD"] = "inform_neighbors_of_chat_message";

            string queryString = WebUtils.BuildQueryString(args);
            string reply = SynchronousRestFormsRequester.MakeRequest("POST", uri, queryString);
        }

        public virtual void CloseAllNeighborAgents(UUID AgentID, UUID currentRegionID)
        {
            m_LocalService.CloseAllNeighborAgents(AgentID, currentRegionID);
        }

        public virtual void CloseNeighborAgents(uint newRegionX, uint newRegionY, UUID AgentID, UUID currentRegionID)
        {
            m_LocalService.CloseNeighborAgents(newRegionX, newRegionY, AgentID, currentRegionID);
        }

        public virtual bool IsOutsideView(uint x, uint newRegionX, uint y, uint newRegionY)
        {
            return m_LocalService.IsOutsideView(x, newRegionX, y, newRegionY);
        }

        private Dictionary<string, object> PackRegionInfo(RegionInfo thisRegion, UUID uUID)
        {
            List<NeighborPassword> passes = m_KnownNeighborsPass[uUID];
            foreach (NeighborPassword p in passes)
            {
                if (thisRegion.RegionID == p.RegionID)
                {
                    thisRegion.Password = p.Password;
                    break;
                }
            }
            return Util.OSDToDictionary(thisRegion.PackRegionInfoData());
        }

        public void RemoveScene(IScene scene)
        {
            m_LocalService.RemoveScene(scene);
        }

        public void Init(IScene scene)
        {
            m_LocalService.Init(scene);
        }

        #endregion

        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlers = config.Configs["Handlers"];
            if (handlers.GetString("NeighborHandler", "") == Name)
            {
                registry.RegisterModuleInterface<INeighborService>(this);
                m_LocalService = new LocalNeighborServiceConnector();
                m_LocalService.ReadConfig(config);
            }
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostStart(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlers = config.Configs["Handlers"];
            if (handlers.GetString("NeighborHandler", "") == Name)
                m_LocalService.PostStart(config, registry);
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlers = config.Configs["Handlers"];
            if (handlers.GetString("NeighborHandler", "") == Name)
                registry.RegisterModuleInterface<INeighborService>(this);
        }

        #endregion
    }
}
