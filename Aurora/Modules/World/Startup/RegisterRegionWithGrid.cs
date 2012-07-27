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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules.Startup
{
    public class RegisterRegionWithGridModule : ISharedRegionStartupModule, IGridRegisterModule
    {
        #region Declares

        private readonly Dictionary<string, string> genericInfo = new Dictionary<string, string>();
        private readonly Dictionary<UUID, List<GridRegion>> m_knownNeighbors = new Dictionary<UUID, List<GridRegion>>();
        private readonly List<IScene> m_scenes = new List<IScene>();
        private IConfigSource m_config;
        private string m_RegisterRegionPassword = "";

        #endregion

        #region IGridRegisterModule Members

        /// <summary>
        ///   Update the grid server with new info about this region
        /// </summary>
        /// <param name = "scene"></param>
        public void UpdateGridRegion(IScene scene)
        {
            IGridService GridService = scene.RequestModuleInterface<IGridService>();
            GridService.UpdateMap(BuildGridRegion(scene.RegionInfo));
        }

        /// <summary>
        ///   Register this region with the grid service
        /// </summary>
        /// <param name = "scene"></param>
        /// <param name = "returnResponseFirstTime">Should we try to walk the user through what went wrong?</param>
        /// <param name="continueTrying"> </param>
        /// <param name="password"> </param>
        public bool RegisterRegionWithGrid(IScene scene, bool returnResponseFirstTime, bool continueTrying, string password)
        {
            GridRegion region = BuildGridRegion(scene.RegionInfo);

            IGenericsConnector g = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            GridSessionID s = null;
            IGridService GridService = scene.RequestModuleInterface<IGridService>();
            if (g != null) //Get the sessionID from the database if possible
                s = g.GetGeneric<GridSessionID>(scene.RegionInfo.RegionID, "GridSessionID", "GridSessionID");

            if (s == null)
            {
                s = new GridSessionID {SessionID = scene.RegionInfo.GridSecureSessionID};
                //Set it from the regionInfo if it knows anything
            }

            scene.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("PreRegisterRegion", region);

            //Tell the grid service about us
            RegisterRegion error = GridService.RegisterRegion(region, s.SessionID, password);
            if (error.Error == String.Empty)
            {
                s.SessionID = error.SessionID;
                //If it registered ok, we save the sessionID to the database and tlel the neighbor service about it
                scene.RegionInfo.GridSecureSessionID = error.SessionID;
                //Update our local copy of what our region flags are
                scene.RegionInfo.RegionFlags = error.RegionFlags;
                scene.RegionInfo.ScopeID = error.Region.ScopeID;
                scene.RegionInfo.AllScopeIDs = error.Region.AllScopeIDs;

                //Save the new SessionID to the database
                if (g != null) g.AddGeneric(scene.RegionInfo.RegionID, "GridSessionID", "GridSessionID", s.ToOSD());

                m_knownNeighbors[scene.RegionInfo.RegionID] = error.Neighbors;
                return true; //Success
            }
            if (returnResponseFirstTime && !continueTrying)
            {
                MainConsole.Instance.Error("[RegisterRegionWithGrid]: Registration of region with grid failed again - " + error.Error);
                return false;
            }

            //Parse the error and try to do something about it if at all possible
            if (error.Error == "Region location is reserved")
            {
                MainConsole.Instance.Error(
                    "[RegisterRegionWithGrid]: Registration of region with grid failed - The region location you specified is reserved. You must move your region.");
                int X = 0, Y = 0;
                int.TryParse(MainConsole.Instance.Prompt("New Region Location X", "1000"), out X);
                int.TryParse(MainConsole.Instance.Prompt("New Region Location Y", "1000"), out Y);

                scene.RegionInfo.RegionLocX = X*Constants.RegionSize;
                scene.RegionInfo.RegionLocY = Y*Constants.RegionSize;

                IRegionLoader[] loaders = scene.RequestModuleInterfaces<IRegionLoader>();
                foreach (IRegionLoader loader in loaders)
                {
                    loader.UpdateRegionInfo(scene.RegionInfo.RegionName, scene.RegionInfo);
                }
            }
            else if (error.Error == "Region overlaps another region")
            {
                MainConsole.Instance.Error("[RegisterRegionWithGrid]: Registration of region " + scene.RegionInfo.RegionName +
                                           " with the grid failed - The region location you specified is already in use. You must move your region.");
                int X = 0, Y = 0;
                int.TryParse(
                    MainConsole.Instance.Prompt("New Region Location X",
                                                (scene.RegionInfo.RegionLocX/256).ToString()), out X);
                int.TryParse(
                    MainConsole.Instance.Prompt("New Region Location Y",
                                                (scene.RegionInfo.RegionLocY/256).ToString()), out Y);

                scene.RegionInfo.RegionLocX = X*Constants.RegionSize;
                scene.RegionInfo.RegionLocY = Y*Constants.RegionSize;

                IRegionLoader[] loaders = scene.RequestModuleInterfaces<IRegionLoader>();
                foreach (IRegionLoader loader in loaders)
                {
                    loader.UpdateRegionInfo(scene.RegionInfo.RegionName, scene.RegionInfo);
                }
            }
            else if (error.Error.Contains("Can't move this region"))
            {
                MainConsole.Instance.Error("[RegisterRegionWithGrid]: Registration of region " + scene.RegionInfo.RegionName +
                                           " with the grid failed - You can not move this region. Moving it back to its original position.");
                //Opensim Grid Servers don't have this functionality.
                try
                {
                    string[] position = error.Error.Split(',');

                    scene.RegionInfo.RegionLocX = int.Parse(position[1])*Constants.RegionSize;
                    scene.RegionInfo.RegionLocY = int.Parse(position[2])*Constants.RegionSize;

                    IRegionLoader[] loaders = scene.RequestModuleInterfaces<IRegionLoader>();
                    foreach (IRegionLoader loader in loaders)
                    {
                        loader.UpdateRegionInfo(scene.RegionInfo.RegionName, scene.RegionInfo);
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error(
                        "Unable to move the region back to its original position, is this an opensim server? Please manually move the region back.");
                    throw e;
                }
            }
            else if (error.Error == "Duplicate region name")
            {
                MainConsole.Instance.Error("[RegisterRegionWithGrid]: Registration of region " + scene.RegionInfo.RegionName +
                                           " with the grid failed - The region name you specified is already in use. Please change the name.");
                string oldRegionName = scene.RegionInfo.RegionName;
                scene.RegionInfo.RegionName = MainConsole.Instance.Prompt("New Region Name", "");

                IRegionLoader[] loaders = scene.RequestModuleInterfaces<IRegionLoader>();
                foreach (IRegionLoader loader in loaders)
                {
                    loader.UpdateRegionInfo(oldRegionName, scene.RegionInfo);
                }
            }
            else if (error.Error == "Region locked out")
            {
                MainConsole.Instance.Error("[RegisterRegionWithGrid]: Registration of region " + scene.RegionInfo.RegionName +
                                           " with the grid the failed - The region you are attempting to join has been blocked from connecting. Please connect another region.");
                MainConsole.Instance.Prompt("Press enter when you are ready to exit");
                Environment.Exit(0);
            }
            else if (error.Error == "Could not reach grid service")
            {
                MainConsole.Instance.Error("[RegisterRegionWithGrid]: Registration of region " + scene.RegionInfo.RegionName +
                                           " with the grid failed - The grid service can not be found! Please make sure that you can connect to the grid server and that the grid server is on.");
                MainConsole.Instance.Error(
                    "You should also make sure you've provided the correct address and port of the grid service.");
                string input =
                    MainConsole.Instance.Prompt(
                        "Press enter when you are ready to proceed, or type cancel to exit");
                if (input == "cancel")
                {
                    Environment.Exit(0);
                }
            }
            else if (error.Error == "Wrong Session ID")
            {
                MainConsole.Instance.Error("[RegisterRegionWithGrid]: Registration of region " + scene.RegionInfo.RegionName +
                                           " with the grid failed - Wrong Session ID for this region!");
                MainConsole.Instance.Error(
                    "This means that this region has failed to connect to the grid server and needs removed from it before it can connect again.");
                MainConsole.Instance.Error(
                    "If you are running the Aurora.Server instance this region is connecting to, type \"clear grid region <RegionName>\" and then press enter on this console and it will work");
                MainConsole.Instance.Error(
                    "If you are not running the Aurora.Server instance this region is connecting to, please contact your grid operator so that he can fix it");

                string input =
                    MainConsole.Instance.Prompt(
                        "Press enter when you are ready to proceed, or type cancel to exit");
                if (input == "cancel")
                    Environment.Exit(0);
            }
            else
            {
                MainConsole.Instance.Error("[RegisterRegionWithGrid]: Registration of region " + scene.RegionInfo.RegionName +
                                           " with the grid failed - " + error.Error + "!");
                string input =
                    MainConsole.Instance.Prompt(
                        "Press enter when you are ready to proceed, or type cancel to exit");
                if (input == "cancel")
                    Environment.Exit(0);
            }
            return RegisterRegionWithGrid(scene, true, continueTrying, password);
        }

        public List<GridRegion> GetNeighbors(IScene scene)
        {
            if (!m_knownNeighbors.ContainsKey(scene.RegionInfo.RegionID))
                return new List<GridRegion>();
            return new List<GridRegion>(m_knownNeighbors[scene.RegionInfo.RegionID]);
        }

        public void AddGenericInfo(string key, string value)
        {
            genericInfo[key] = value;
        }

        #endregion

        #region GridSessionID class

        /// <summary>
        ///   This class is used to save the GridSessionID for the given region/grid service
        /// </summary>
        public class GridSessionID : IDataTransferable
        {
            public UUID SessionID;

            public override void FromOSD(OSDMap map)
            {
                SessionID = map["SessionID"].AsUUID();
            }

            public override OSDMap ToOSD()
            {
                OSDMap map = new OSDMap {{"SessionID", SessionID}};
                return map;
            }

            public override Dictionary<string, object> ToKVP()
            {
                return Util.OSDToDictionary(ToOSD());
            }

            public override void FromKVP(Dictionary<string, object> KVP)
            {
                FromOSD(Util.DictionaryToOSD(KVP));
            }
        }

        #endregion

        #region ISharedRegionStartupModule Members

        public void Initialise(IScene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            m_scenes.Add(scene);
            //Register the interface
            m_config = source;
            scene.RegisterModuleInterface<IGridRegisterModule>(this);

            IConfig gridConfig = m_config.Configs["Configuration"];
            m_RegisterRegionPassword = Util.Md5Hash(gridConfig.GetString("RegisterRegionPassword", m_RegisterRegionPassword));

            //Now register our region with the grid
            RegisterRegionWithGrid(scene, false, true, m_RegisterRegionPassword);
        }

        public void PostInitialise(IScene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void FinishStartup(IScene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void PostFinishStartup(IScene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            scene.RequestModuleInterface<IAsyncMessageRecievedService>().OnMessageReceived +=
                RegisterRegionWithGridModule_OnMessageReceived;
        }

        public void StartupComplete()
        {
        }

        public void Close(IScene scene)
        {
            //Deregister the interface
            scene.UnregisterModuleInterface<IGridRegisterModule>(this);
            m_scenes.Remove(scene);

            MainConsole.Instance.InfoFormat("[RegisterRegionWithGrid]: Deregistering region {0} from the grid...",
                             scene.RegionInfo.RegionName);

            //Deregister from the grid server
            IGridService GridService = scene.RequestModuleInterface<IGridService>();
            GridRegion r = BuildGridRegion(scene.RegionInfo);
            r.IsOnline = false;
            string error = "";
            if(scene.RegionInfo.HasBeenDeleted)
                GridService.DeregisterRegion(r);
            else if ((error = GridService.UpdateMap(r)) != "")
                MainConsole.Instance.WarnFormat("[RegisterRegionWithGrid]: Deregister from grid failed for region {0}, {1}",
                                 scene.RegionInfo.RegionName, error);
        }

        public void DeleteRegion(IScene scene)
        {
            IGridService GridService = scene.RequestModuleInterface<IGridService>();
            if (!GridService.DeregisterRegion(BuildGridRegion(scene.RegionInfo)))
                MainConsole.Instance.WarnFormat("[RegisterRegionWithGrid]: Deregister from grid failed for region {0}",
                                 scene.RegionInfo.RegionName);
        }

        #endregion

        private OSDMap RegisterRegionWithGridModule_OnMessageReceived(OSDMap message)
        {
            if (!message.ContainsKey("Method"))
                return null;

            if (message["Method"] == "NeighborChange")
            {
                OSDMap innerMessage = (OSDMap) message["Message"];
                bool down = innerMessage["Down"].AsBoolean();
                UUID regionID = innerMessage["Region"].AsUUID();
                UUID targetregionID = innerMessage["TargetRegion"].AsUUID();

                if (m_knownNeighbors.ContainsKey(targetregionID))
                {
                    if (down)
                    {
                        //Remove it
                        m_knownNeighbors[targetregionID].RemoveAll(delegate(GridRegion r)
                                                                       {
                                                                           if (r.RegionID == regionID)
                                                                               return true;
                                                                           return false;
                                                                       });
                    }
                    else
                    {
                        //Add it if it doesn't already exist
                        if (m_knownNeighbors[targetregionID].Find(delegate(GridRegion rr)
                                                                      {
                                                                          if (rr.RegionID == regionID)
                                                                              return true;
                                                                          return false;
                                                                      }) == null)
                            m_knownNeighbors[targetregionID].Add(m_scenes[0].GridService.GetRegionByUUID(null,
                                                                                                         regionID));
                    }
                }
            }
            return null;
        }

        private GridRegion BuildGridRegion(RegionInfo regionInfo)
        {
            GridRegion region = new GridRegion(regionInfo);
            OSDMap map = new OSDMap();
            foreach (KeyValuePair<string, string> kvp in genericInfo)
            {
                map[kvp.Key] = kvp.Value;
            }
            region.GenericMap = map;
            return region;
        }
    }
}