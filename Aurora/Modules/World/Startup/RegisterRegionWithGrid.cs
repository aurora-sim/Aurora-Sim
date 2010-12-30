using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using System.Xml;
using Nini.Config;
using log4net;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules
{
    public class RegisterRegionWithGridModule : ISharedRegionStartupModule, IGridRegisterModule
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IConfigSource m_config;

        #endregion

        #region ISharedRegionStartupModule Members

        public void Initialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            m_config = source;
        }

        public void PostInitialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void FinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            //Register the interface
            scene.RegisterModuleInterface<IGridRegisterModule>(this);
            //Now register our region with the grid
            RegisterRegionWithGrid(scene);
        }

        public void Close(Scene scene)
        {
            //Deregister the interface
            scene.UnregisterModuleInterface<IGridRegisterModule>(this);
        }

        #endregion

        #region IGridRegisterModule Members

        /// <summary>
        /// Update the grid server with new info about this region
        /// </summary>
        /// <param name="scene"></param>
        public void UpdateGridRegion(Scene scene)
        {
            scene.GridService.UpdateMap(scene.RegionInfo.ScopeID, new GridRegion(scene.RegionInfo), scene.RegionInfo.GridSecureSessionID);
        }

        /// <summary>
        /// Register this region with the grid service
        /// </summary>
        /// <param name="scene"></param>
        public void RegisterRegionWithGrid(Scene scene)
        {
            GridRegion region = new GridRegion(scene.RegionInfo);

            IGenericsConnector g = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            GridSessionID s = null;
            if (g != null) //Get the sessionID from the database if possible
                s = g.GetGeneric<GridSessionID>(scene.RegionInfo.RegionID, "GridSessionID", scene.GridService.GridServiceURL, new GridSessionID());

            if (s == null)
            {
                s = new GridSessionID();
                //Set it from the regionInfo if it knows anything
                s.SessionID = scene.RegionInfo.GridSecureSessionID;
            }

            //Tell the grid service about us
            string error = scene.GridService.RegisterRegion(scene.RegionInfo.ScopeID, region, s.SessionID, out s.SessionID);
            if (error == String.Empty)
            {
                //If it registered ok, we save the sessionID to the database and tlel the neighbor service about it
                scene.RegionInfo.GridSecureSessionID = s.SessionID;

                //Save the new SessionID to the database
                g.AddGeneric(scene.RegionInfo.RegionID, "GridSessionID", scene.GridService.GridServiceURL, s.ToOSD());

                //Tell the neighbor service about it
                INeighbourService service = scene.RequestModuleInterface<INeighbourService>();
                if (service != null)
                    service.InformNeighborsThatRegionIsUp(scene.RegionInfo);
            }
            else
            {
                //Parse the error and try to do something about it if at all possible
                if (error == "Region location is reserved")
                {
                    m_log.Error("[STARTUP]: Registration of region with grid failed - The region location you specified is reserved. You must move your region.");
                    uint X = 0, Y = 0;
                    uint.TryParse(MainConsole.Instance.CmdPrompt("New Region Location X", "1000"), out X);
                    uint.TryParse(MainConsole.Instance.CmdPrompt("New Region Location Y", "1000"), out Y);

                    scene.RegionInfo.RegionLocX = X;
                    scene.RegionInfo.RegionLocY = Y;

                    IConfig config = m_config.Configs["RegionStartup"];
                    if (config != null)
                    {
                        //TERRIBLE! Needs to be modular, but we can't access the module from a scene module!
                        if (config.GetString("Default") == "RegionLoaderDataBaseSystem")
                            Aurora.DataManager.DataManager.RequestPlugin<Aurora.Framework.IRegionInfoConnector>().UpdateRegionInfo(scene.RegionInfo, false);
                        else
                            SaveChangesFile("", scene.RegionInfo);
                    }
                    else
                        SaveChangesFile("", scene.RegionInfo);
                }
                if (error == "Region overlaps another region")
                {
                    m_log.Error("[STARTUP]: Registration of region " + scene.RegionInfo.RegionName + " with the grid failed - The region location you specified is already in use. You must move your region.");
                    uint X = 0, Y = 0;
                    uint.TryParse(MainConsole.Instance.CmdPrompt("New Region Location X", "1000"), out X);
                    uint.TryParse(MainConsole.Instance.CmdPrompt("New Region Location Y", "1000"), out Y);

                    scene.RegionInfo.RegionLocX = X;
                    scene.RegionInfo.RegionLocY = Y;

                    IConfig config = m_config.Configs["RegionStartup"];
                    if (config != null)
                    {
                        //TERRIBLE! Needs to be modular, but we can't access the module from a scene module!
                        if (config.GetString("Default") == "RegionLoaderDataBaseSystem")
                            Aurora.DataManager.DataManager.RequestPlugin<Aurora.Framework.IRegionInfoConnector>().UpdateRegionInfo(scene.RegionInfo, false);
                        else
                            SaveChangesFile("", scene.RegionInfo);
                    }
                    else
                        SaveChangesFile("", scene.RegionInfo);
                }
                if (error.Contains("Can't move this region"))
                {
                    m_log.Error("[STARTUP]: Registration of region " + scene.RegionInfo.RegionName + " with the grid failed - You can not move this region. Moving it back to its original position.");
                    //Opensim Grid Servers don't have this functionality.
                    try
                    {
                        string[] position = error.Split(',');

                        scene.RegionInfo.RegionLocX = uint.Parse(position[1]);
                        scene.RegionInfo.RegionLocY = uint.Parse(position[2]);

                        IConfig config = m_config.Configs["RegionStartup"];
                        if (config != null)
                        {
                            //TERRIBLE! Needs to be modular, but we can't access the module from a scene module!
                            if (config.GetString("Default") == "RegionLoaderDataBaseSystem")
                                Aurora.DataManager.DataManager.RequestPlugin<Aurora.Framework.IRegionInfoConnector>().UpdateRegionInfo(scene.RegionInfo, false);
                            else
                                SaveChangesFile("", scene.RegionInfo);
                        }
                        else
                            SaveChangesFile("", scene.RegionInfo);
                    }
                    catch (Exception e)
                    {
                        m_log.Error("Unable to move the region back to its original position, is this an opensim server? Please manually move the region back.");
                        throw e;
                    }
                }
                if (error == "Duplicate region name")
                {
                    m_log.Error("[STARTUP]: Registration of region " + scene.RegionInfo.RegionName + " with the grid failed - The region name you specified is already in use. Please change the name.");
                    string oldRegionName = scene.RegionInfo.RegionName;
                    scene.RegionInfo.RegionName = MainConsole.Instance.CmdPrompt("New Region Name", "");

                    IConfig config = m_config.Configs["RegionStartup"];
                    if (config != null)
                    {
                        //TERRIBLE! Needs to be modular, but we can't access the module from a scene module!
                        if (config.GetString("Default") == "RegionLoaderDataBaseSystem")
                            Aurora.DataManager.DataManager.RequestPlugin<Aurora.Framework.IRegionInfoConnector>().UpdateRegionInfo(scene.RegionInfo, false);
                        else
                            SaveChangesFile(oldRegionName, scene.RegionInfo);
                    }
                    else
                        SaveChangesFile(oldRegionName, scene.RegionInfo);
                }
                if (error == "Region locked out")
                {
                    m_log.Error("[STARTUP]: Registration of region " + scene.RegionInfo.RegionName + " with the grid the failed - The region you are attempting to join has been blocked from connecting. Please connect another region.");
                    string input = MainConsole.Instance.CmdPrompt("Press enter when you are ready to exit");
                    Environment.Exit(0);
                }
                if (error == "Error communicating with grid service")
                {
                    m_log.Error("[STARTUP]: Registration of region " + scene.RegionInfo.RegionName + " with the grid failed - The grid service can not be found! Please make sure that you can connect to the grid server and that the grid server is on.");
                    string input = MainConsole.Instance.CmdPrompt("Press enter when you are ready to proceed, or type cancel to exit");
                    if (input == "cancel")
                    {
                        Environment.Exit(0);
                    }
                }
                if (error == "Wrong Session ID")
                {
                    m_log.Error("[STARTUP]: Registration of region " + scene.RegionInfo.RegionName + " with the grid failed - Wrong Session ID for this region!");
                    string input = MainConsole.Instance.CmdPrompt("Press enter when you are ready to proceed, or type cancel to exit");
                    if (input == "cancel")
                    {
                        Environment.Exit(0);
                    }
                }
                RegisterRegionWithGrid(scene);
            }
        }

        #region Private Members

        /// <summary>
        /// Save the changes to the RegionInfo to the file that it came from in the Regions/ directory
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="regionInfo"></param>
        private void SaveChangesFile(string oldName, RegionInfo regionInfo)
        {
            string regionConfigPath = Path.Combine(Util.configDir(), "Regions");

            if (oldName == "")
                oldName = regionInfo.RegionName;
            try
            {
                IConfig config = m_config.Configs["RegionStartup"];
                if (config != null)
                {
                    regionConfigPath = config.GetString("RegionsDirectory", regionConfigPath).Trim();
                }
            }
            catch (Exception)
            {
                // No INI setting recorded.
            }
            if (!Directory.Exists(regionConfigPath))
                return;

            string[] iniFiles = Directory.GetFiles(regionConfigPath, "*.ini");
            foreach (string file in iniFiles)
            {
                IConfigSource source = new IniConfigSource(file, Nini.Ini.IniFileType.AuroraStyle);
                IConfig cnf = source.Configs[oldName];
                if (cnf != null)
                {
                    try
                    {
                        source.Configs.Remove(cnf);
                        cnf.Set("Location", regionInfo.RegionLocX + "," + regionInfo.RegionLocY);
                        cnf.Set("RegionType", regionInfo.RegionType);
                        cnf.Name = regionInfo.RegionName;
                        source.Configs.Add(cnf);
                    }
                    catch
                    {
                    }
                    source.Save();
                    break;
                }
            }
        }

        #endregion

        #region GridSessionID class

        /// <summary>
        /// This class is used to save the GridSessionID for the given region/grid service
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
                OSDMap map = new OSDMap();
                map.Add("SessionID", SessionID);
                return map;
            }

            public override Dictionary<string, object> ToKeyValuePairs()
            {
                return Util.OSDToDictionary(ToOSD());
            }

            public override void FromKVP(Dictionary<string, object> KVP)
            {
                FromOSD(Util.DictionaryToOSD(KVP));
            }

            public override IDataTransferable Duplicate()
            {
                GridSessionID m = new GridSessionID();
                m.FromOSD(ToOSD());
                return m;
            }
        }

        #endregion

        #endregion
    }
}
