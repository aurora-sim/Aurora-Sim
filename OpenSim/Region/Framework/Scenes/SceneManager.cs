/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using OpenMetaverse;
using log4net;
using log4net.Core;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Physics.Manager;
using Nini.Config;
using OpenSim;
using Aurora.Simulation.Base;
using Aurora.Framework;

namespace OpenSim.Region.Framework.Scenes
{
    public delegate void NewScene (IScene scene);
    public delegate void NoParam ();
    /// <summary>
    /// Manager for adding, closing, reseting, and restarting scenes.
    /// </summary>
    public class SceneManager : IApplicationPlugin
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public event NewScene OnAddedScene;
        public event NewScene OnCloseScene;

        private List<IScene> m_localScenes = new List<IScene>();
        private ISimulationBase m_OpenSimBase;
        private IConfigSource m_config = null;
        public IConfigSource ConfigSource
        {
            get { return m_config; }
        }
        private int RegionsFinishedStarting = 0;
        public int AllRegions = 0;
        protected ISimulationDataStore m_simulationDataService;
        protected List<ISharedRegionStartupModule> m_startupPlugins = new List<ISharedRegionStartupModule>();
        
        public ISimulationDataStore SimulationDataService
        {
            get { return m_simulationDataService; }
            set { m_simulationDataService = value; }
        }

        public List<IScene> Scenes
        {
            get { return m_localScenes; }
        }

        public IScene CurrentOrFirstScene
        {
            get
            {
                if (MainConsole.Instance.ConsoleScene == null)
                {
                    if (m_localScenes.Count > 0)
                    {
                        return m_localScenes[0];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return MainConsole.Instance.ConsoleScene;
                }
            }
        }

        #endregion

        #region IApplicationPlugin members

        public void Initialize(ISimulationBase openSim)
        {
            m_OpenSimBase = openSim;

            IConfig handlerConfig = openSim.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("SceneManager", "") != Name)
                return;

            m_config = openSim.ConfigSource;

            string name = String.Empty;
            // Try reading the [SimulationDataStore] section
            IConfig simConfig = openSim.ConfigSource.Configs["SimulationDataStore"];
            if (simConfig != null)
            {
                name = simConfig.GetString("DatabaseLoaderName", "FileBasedDatabase");
            }

            ISimulationDataStore[] stores = AuroraModuleLoader.PickupModules<ISimulationDataStore> ().ToArray ();
            foreach (ISimulationDataStore store in stores)
            {
                if (store.Name == name)
                {
                    m_simulationDataService = store;
                    break;
                }
            }

            if (m_simulationDataService == null)
            {
                m_log.ErrorFormat("[SceneManager]: FAILED TO LOAD THE SIMULATION SERVICE AT '{0}', QUITING...", name);
                Console.Read ();//Wait till they see
                Environment.Exit(0);
            }
            m_simulationDataService.Initialise();

            AddConsoleCommands();

            //Load the startup modules for the region
            m_startupPlugins = AuroraModuleLoader.PickupModules<ISharedRegionStartupModule>();

            //Register us!
            m_OpenSimBase.ApplicationRegistry.RegisterModuleInterface<SceneManager>(this);
        }

        public void ReloadConfiguration(IConfigSource config)
        {
            //Update this
            m_config = config;
            if (m_localScenes == null)
                return;
            foreach (IScene scene in m_localScenes)
            {
                scene.Config = config;
                scene.PhysicsScene.PostInitialise(config);
            }
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
        }

        public void PostStart()
        {
        }

        public string Name
        {
            get { return "SceneManager"; }
        }

        public void Dispose()
        {
        }

        public void Close()
        {
            if (m_localScenes == null)
                return;
            IScene[] scenes = new IScene[m_localScenes.Count];
            m_localScenes.CopyTo(scenes, 0);
            // collect known shared modules in sharedModules
            for (int i = 0; i < scenes.Length; i++)
            {
                // close scene/region
                CloseRegion(scenes[i]);
            }
        }

        #endregion

        #region Startup complete

        public void HandleStartupComplete(IScene scene, List<string> data)
        {
            RegionsFinishedStarting++;
            if (RegionsFinishedStarting >= AllRegions)
            {
                FinishStartUp();
            }
        }

        private void FinishStartUp()
        {
            //Tell modules about it 
            StartupCompleteModules();

            m_OpenSimBase.RunStartupCommands();

            // For now, start at the 'root' level by default
            if (Scenes.Count == 1)
            {
                // If there is only one region, select it
                ChangeSelectedRegion(Scenes[0].RegionInfo.RegionName);
            }
            else
            {
                ChangeSelectedRegion("root");
            }

            TimeSpan timeTaken = DateTime.Now - m_OpenSimBase.StartupTime;

            m_log.InfoFormat ("[SceneManager]: Startup is complete and took {0}m {1}.{2}s", timeTaken.Minutes, timeTaken.Seconds, timeTaken.Milliseconds);
            AuroraModuleLoader.ClearCache ();
            // In 99.9% of cases it is a bad idea to manually force garbage collection. However,
            // this is a rare case where we know we have just went through a long cycle of heap
            // allocations, and there is no more work to be done until someone logs in
            GC.Collect ();
        }

        #endregion

        #region ForEach functions

        public void ForEachCurrentScene(Action<IScene> func)
        {
            if (MainConsole.Instance.ConsoleScene == null)
            {
                m_localScenes.ForEach(func);
            }
            else
            {
                func (MainConsole.Instance.ConsoleScene);
            }
        }

        public void ForEachScene(Action<IScene> action)
        {
            m_localScenes.ForEach(action);
        }

        #endregion

        #region TrySetScene functions

        public bool TrySetCurrentScene(string regionName)
        {
            if ((String.Compare(regionName, "root") == 0))
            {
                MainConsole.Instance.ConsoleScene = null;
                return true;
            }
            else
            {
                foreach (IScene scene in m_localScenes)
                {
                    if (String.Compare(scene.RegionInfo.RegionName, regionName, true) == 0)
                    {
                        MainConsole.Instance.ConsoleScene = scene;
                        return true;
                    }
                }

                return false;
            }
        }

        public bool TrySetCurrentScene(UUID regionID)
        {
            m_log.Debug("Searching for Region: '" + regionID + "'");

            foreach (IScene scene in m_localScenes)
            {
                if (scene.RegionInfo.RegionID == regionID)
                {
                    MainConsole.Instance.ConsoleScene = scene;
                    return true;
                }
            }

            return false;
        }

        public void ChangeSelectedRegion(string newRegionName)
        {
            if (!TrySetCurrentScene(newRegionName))
            {
                m_log.Info (String.Format ("Couldn't select region {0}", newRegionName));
                return;
            }

            string regionName = (MainConsole.Instance.ConsoleScene == null ?
                "root" : MainConsole.Instance.ConsoleScene.RegionInfo.RegionName);
            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.DefaultPrompt = String.Format ("Region ({0}) ", regionName);
            }
        }

        #endregion

        #region TryGet functions

        public bool TryGetScene (string regionName, out IScene scene)
        {
            foreach (IScene mscene in m_localScenes)
            {
                if (String.Compare(mscene.RegionInfo.RegionName, regionName, true) == 0)
                {
                    scene = mscene;
                    return true;
                }
            }
            scene = null;
            return false;
        }

        public bool TryGetScene(UUID regionID, out IScene scene)
        {
            foreach (IScene mscene in m_localScenes)
            {
                if (mscene.RegionInfo.RegionID == regionID)
                {
                    scene = mscene;
                    return true;
                }
            }

            scene = null;
            return false;
        }

        /// <summary>
        /// Try to find a current scene at the given location
        /// </summary>
        /// <param name="locX">In meters</param>
        /// <param name="locY">In meters</param>
        /// <param name="scene"></param>
        /// <returns></returns>
        public bool TryGetScene(int locX, int locY, out IScene scene)
        {
            foreach (IScene mscene in m_localScenes)
            {
                if (mscene.RegionInfo.RegionLocX == locX &&
                    mscene.RegionInfo.RegionLocY == locY)
                {
                    scene = mscene;
                    return true;
                }
            }

            scene = null;
            return false;
        }

        /// <summary>
        /// Try to find a current scene at the given location
        /// </summary>
        /// <param name="locX">In meters</param>
        /// <param name="locY">In meters</param>
        /// <param name="scene"></param>
        /// <returns></returns>
        public bool TryGetScene(ulong RegionHandle, out IScene scene)
        {
            int X, Y;
            Util.UlongToInts (RegionHandle, out X, out Y);
            return TryGetScene (X, Y, out scene);
        }

        #endregion

        #region Add a region

        public IScene StartNewRegion (RegionInfo regionInfo)
        {
            ISceneLoader sceneLoader = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<ISceneLoader> ();
            if (sceneLoader == null)
                throw new Exception ("No Scene Loader Interface!");

            //Get the new scene from the interface
            IScene scene = sceneLoader.CreateScene (regionInfo);
            StartNewRegion (scene);
            return scene;
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="portadd_flag"></param>
        /// <param name="do_post_init"></param>
        /// <returns></returns>
        public void StartNewRegion(IScene scene)
        {
            StartModules (scene);

            //Do this here so that we don't have issues later when startup complete messages start coming in
            m_localScenes.Add (scene);

            // set the initial ports
            scene.RegionInfo.HttpPort = MainServer.Instance.Port;

            m_log.Info("[Modules]: Loading region modules");
            IRegionModulesController controller;
            if (m_OpenSimBase.ApplicationRegistry.TryRequestModuleInterface(out controller))
            {
                controller.AddRegionToModules(scene);
            }
            else
                m_log.Error("[Modules]: The new RegionModulesController is missing...");

            //Post init the modules now
            PostInitModules(scene);

            //Start the heartbeats
            scene.StartHeartbeat();
            //Tell the scene that the startup is complete 
            // Note: this event is added in the scene constructor
            scene.FinishedStartup("Startup", new List<string>());
        }

        /// <summary>
        /// Gets a new copy of the simulation data store, keep one per region
        /// </summary>
        /// <returns></returns>
        public ISimulationDataStore GetNewSimulationDataStore()
        {
            return m_simulationDataService.Copy();
        }

        #endregion

        #region Reset a region

        public void ResetScene()
        {
            if (MainConsole.Instance.ConsoleScene == null)
            {
                m_log.Warn("You must use this command on a region. Use 'change region' to change to the region you would like to change");
                return;
            }

            IBackupModule backup = MainConsole.Instance.ConsoleScene.RequestModuleInterface<IBackupModule> ();
            if(backup != null)
                backup.DeleteAllSceneObjects();
            ITerrainModule module = MainConsole.Instance.ConsoleScene.RequestModuleInterface<ITerrainModule> ();
            if (module != null)
            {
                module.ResetTerrain();
            }
            m_log.Warn ("Region " + MainConsole.Instance.ConsoleScene.RegionInfo.RegionName + " was reset");
        }

        #endregion

        #region Restart a region

        public void HandleRestart(IScene scene)
        {
            CloseModules(scene);
            m_localScenes.Remove (scene);
            StartNewRegion (scene.RegionInfo);
        }

        #endregion

        #region Shutdown regions

        public void RemoveRegion (IScene scene, bool cleanup)
        {
            IBackupModule backup = scene.RequestModuleInterface<IBackupModule>();
            if (backup != null)
                backup.DeleteAllSceneObjects();

            CloseRegion(scene);

            if (!cleanup)
                return;

            IRegionLoader[] loaders = m_OpenSimBase.ApplicationRegistry.RequestModuleInterfaces<IRegionLoader>();
            foreach (IRegionLoader loader in loaders)
            {
                loader.DeleteRegion(scene.RegionInfo);
            }
        }

        /// <summary>
        /// Remove a region from the simulator without deleting it permanently.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public void CloseRegion (IScene scene)
        {
            // only need to check this if we are not at the
            // root level
            if ((MainConsole.Instance.ConsoleScene != null) && (MainConsole.Instance.ConsoleScene.RegionInfo.RegionID == scene.RegionInfo.RegionID))
            {
                TrySetCurrentScene ("root");
            }

            m_localScenes.Remove(scene);
            scene.Close();

            m_log.DebugFormat("[SceneManager]: Shutting down region modules for {0}", scene.RegionInfo.RegionName);
            IRegionModulesController controller;
            if (m_OpenSimBase.ApplicationRegistry.TryRequestModuleInterface<IRegionModulesController>(out controller))
                controller.RemoveRegionFromModules(scene);

            CloseModules(scene);
        }

        #endregion

        #region ISharedRegionStartupModule initialization

        public void StartModules(IScene scene)
        {
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.Initialise(scene, m_config, m_OpenSimBase);
            }
        }

        public void PostInitModules(IScene scene)
        {
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.PostInitialise(scene, m_config, m_OpenSimBase);
            }
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.FinishStartup(scene, m_config, m_OpenSimBase);
            }
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.PostFinishStartup(scene, m_config, m_OpenSimBase);
            }
            if (OnAddedScene != null)
                OnAddedScene (scene);
        }

        public void StartupCompleteModules()
        {
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.StartupComplete();
            }
        }

        public void CloseModules (IScene scene)
        {
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.Close(scene);
            }
            if (OnCloseScene != null)
                OnCloseScene(scene);
        }

        #endregion

        #region Console Commands

        private void AddConsoleCommands()
        {
            if (MainConsole.Instance == null)
                return;
            MainConsole.Instance.Commands.AddCommand ("show users", "show users [full]", "Shows users in the given region (if full is added, child agents are shown as well)", HandleShowUsers);
            MainConsole.Instance.Commands.AddCommand ("show regions", "show regions", "Show information about all regions in this instance", HandleShowRegions);
            MainConsole.Instance.Commands.AddCommand ("show maturity", "show maturity", "Show all region's maturity levels", HandleShowMaturity);

            MainConsole.Instance.Commands.AddCommand ("force update", "force update", "Force the update of all objects on clients", HandleForceUpdate);

            MainConsole.Instance.Commands.AddCommand("debug packet", "debug packet [level]", "Turn on packet debugging", Debug);
            MainConsole.Instance.Commands.AddCommand("debug scene", "debug scene [scripting] [collisions] [physics]", "Turn on scene debugging", Debug);

            MainConsole.Instance.Commands.AddCommand("change region", "change region [region name]", "Change current console region", ChangeSelectedRegion);

            MainConsole.Instance.Commands.AddCommand("load xml2", "load xml2", "Load a region's data from XML2 format", LoadXml2);

            MainConsole.Instance.Commands.AddCommand("save xml2", "save xml2", "Save a region's data in XML2 format", SaveXml2);

            MainConsole.Instance.Commands.AddCommand ("load oar", "load oar [--merge] [--skip-assets] [oar name] [OffsetX=#] [OffsetY=#] [OffsetZ=#]", "Load a region's data from OAR archive.  --merge will merge the oar with the existing scene.  --skip-assets will load the oar but ignore the assets it contains. OffsetX will change where the X location of the oar is loaded, and the same for Y and Z.", LoadOar);

            MainConsole.Instance.Commands.AddCommand("save oar", "save oar [-v|--version=N] [<OAR path>]", "Save a region's data to an OAR archive -v|--version=N generates scene objects as per older versions of the serialization (e.g. -v=0)" + Environment.NewLine
                                           + "The OAR path must be a filesystem path."
                                           + "  If this is not given then the oar is saved to region.oar in the current directory.", SaveOar);

            MainConsole.Instance.Commands.AddCommand("kick user", "kick user [first] [last] [message]", "Kick a user off the simulator", KickUserCommand);

            MainConsole.Instance.Commands.AddCommand("reset region", "reset region", "Reset region to the default terrain, wipe all prims, etc.", RunCommand);

            MainConsole.Instance.Commands.AddCommand("restart-instance", "restart-instance", "Restarts the instance (as if you closed and re-opened Aurora)", RunCommand);

            MainConsole.Instance.Commands.AddCommand("command-script", "command-script [script]", "Run a command script from file", RunCommand);

            MainConsole.Instance.Commands.AddCommand("remove-region", "remove-region [name]", "Remove a region from this simulator", RunCommand);

            MainConsole.Instance.Commands.AddCommand("delete-region", "delete-region [name]", "Delete a region from disk", RunCommand);

            MainConsole.Instance.Commands.AddCommand ("modules list", "modules list", "Lists all simulator modules", HandleModulesList);

            MainConsole.Instance.Commands.AddCommand ("modules unload", "modules unload [module]", "Unload the given simulator module", HandleModulesUnload);

            MainConsole.Instance.Commands.AddCommand ("kill uuid", "kill uuid [UUID]", "Kill an object by UUID", KillUUID);
        }

        /// <summary>
        /// Kicks users off the region
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmdparams">name of avatar to kick</param>
        private void KickUserCommand(string[] cmdparams)
        {
            string alert = null;
            IList agents = CurrentOrFirstScene.GetScenePresences ();

            if (cmdparams.Length < 4)
            {
                if (cmdparams.Length < 3)
                    return;
                UUID avID = UUID.Zero;
                if (cmdparams[2] == "all")
                {
                    foreach (IScenePresence presence in agents)
                    {
                        RegionInfo regionInfo = presence.Scene.RegionInfo;

                        m_log.Info (String.Format ("Kicking user: {0,-16}{1,-37} in region: {2,-16}", presence.Name, presence.UUID, regionInfo.RegionName));

                        // kick client...
                        if (alert != null)
                            presence.ControllingClient.Kick(alert);
                        else
                            presence.ControllingClient.Kick("\nThe Aurora manager kicked you out.\n");

                        // ...and close on our side
                        IEntityTransferModule transferModule = presence.Scene.RequestModuleInterface<IEntityTransferModule> ();
                        if(transferModule != null)
                            transferModule.IncomingCloseAgent (presence.Scene, presence.UUID);
                    }
                }
                else if(UUID.TryParse(cmdparams[2], out avID))
                {
                    foreach (IScenePresence presence in agents)
                    {
                        if (presence.UUID == avID)
                        {
                            RegionInfo regionInfo = presence.Scene.RegionInfo;

                            m_log.Info (String.Format ("Kicking user: {0,-16}{1,-37} in region: {2,-16}", presence.Name, presence.UUID, regionInfo.RegionName));

                            // kick client...
                            if (alert != null)
                                presence.ControllingClient.Kick (alert);
                            else
                                presence.ControllingClient.Kick ("\nThe Aurora manager kicked you out.\n");

                            // ...and close on our side
                            IEntityTransferModule transferModule = presence.Scene.RequestModuleInterface<IEntityTransferModule> ();
                            if (transferModule != null)
                                transferModule.IncomingCloseAgent (presence.Scene, presence.UUID);
                        }
                    }
                }
            }

            if (cmdparams.Length > 4)
                alert = String.Format("\n{0}\n", String.Join(" ", cmdparams, 4, cmdparams.Length - 4));

            foreach (IScenePresence presence in agents)
            {
                RegionInfo regionInfo = presence.Scene.RegionInfo;
                string param = Util.CombineParams (cmdparams, 2);
                if (presence.Name.ToLower().Contains (param.ToLower ()) ||
                    (presence.Firstname.ToLower ().Contains (cmdparams[2].ToLower ()) && presence.Lastname.ToLower ().Contains (cmdparams[3].ToLower ())))
                {
                    m_log.Info (String.Format ("Kicking user: {0,-16}{1,-37} in region: {2,-16}", presence.Name, presence.UUID, regionInfo.RegionName));

                    // kick client...
                    if (alert != null)
                        presence.ControllingClient.Kick(alert);
                    else
                        presence.ControllingClient.Kick("\nThe Aurora manager kicked you out.\n");

                    // ...and close on our side
                    IEntityTransferModule transferModule = presence.Scene.RequestModuleInterface<IEntityTransferModule> ();
                    if (transferModule != null)
                        transferModule.IncomingCloseAgent (presence.Scene, presence.UUID);
                }
            }
            m_log.Info ("");
        }

        /// <summary>
        /// Force resending of all updates to all clients in active region(s)
        /// </summary>
        /// <param name="module"></param>
        /// <param name="args"></param>
        private void HandleForceUpdate(string[] args)
        {
            m_log.Info ("Updating all clients");
            ForEachCurrentScene(delegate(IScene scene)
            {
                ISceneEntity[] EntityList = scene.Entities.GetEntities ();

                foreach (ISceneEntity ent in EntityList)
                {
                    if (ent is SceneObjectGroup)
                    {
                        ((SceneObjectGroup)ent).ScheduleGroupUpdate (PrimUpdateFlags.ForcedFullUpdate);
                    }
                }
                List<IScenePresence> presences = scene.Entities.GetPresences ();
                foreach(IScenePresence presence in presences)
                {
                    if(!presence.IsChildAgent)
                        scene.ForEachClient(delegate(IClientAPI client)
                        {
                            client.SendAvatarDataImmediate(presence);
                        });
                }
            });
        }

        /// <summary>
        /// Load, Unload, and list Region modules in use
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmd"></param>
        private void HandleModulesUnload(string[] cmd)
        {
            List<string> args = new List<string> (cmd);
            args.RemoveAt (0);
            string[] cmdparams = args.ToArray ();

            IRegionModulesController controller = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<IRegionModulesController> ();
            if (cmdparams.Length > 1)
            {
                foreach (IRegionModuleBase irm in controller.AllModules)
                {
                    if (irm.Name.ToLower () == cmdparams[1].ToLower ())
                    {
                        m_log.Info (String.Format ("Unloading module: {0}", irm.Name));
                        foreach (IScene scene in Scenes)
                            irm.RemoveRegion (scene);
                        irm.Close ();
                    }
                }
            }
        }

        /// <summary>
        /// Load, Unload, and list Region modules in use
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmd"></param>
        private void HandleModulesList (string[] cmd)
        {
            List<string> args = new List<string> (cmd);
            args.RemoveAt (0);
            
            IRegionModulesController controller = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<IRegionModulesController> ();
            foreach (IRegionModuleBase irm in controller.AllModules)
            {
                if (irm is ISharedRegionModule)
                    m_log.Info (String.Format ("Shared region module: {0}", irm.Name));
                else if (irm is INonSharedRegionModule)
                    m_log.Info (String.Format ("Nonshared region module: {0}", irm.Name));
                else
                    m_log.Info (String.Format ("Unknown type " + irm.GetType ().ToString () + " region module: {0}", irm.Name));
            }
        }

        /// <summary>
        /// Serialize region data to XML2Format
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmdparams"></param>
        protected void SaveXml2(string[] cmdparams)
        {
            if (cmdparams.Length > 2)
            {
                IRegionSerialiserModule serialiser = CurrentOrFirstScene.RequestModuleInterface<IRegionSerialiserModule>();
                if (serialiser != null)
                    serialiser.SavePrimsToXml2(CurrentOrFirstScene, cmdparams[2]);
            }
            else
            {
                m_log.Warn("Wrong number of parameters!");
            }
        }

        /// <summary>
        /// Runs commands issued by the server console from the operator
        /// </summary>
        /// <param name="command">The first argument of the parameter (the command)</param>
        /// <param name="cmdparams">Additional arguments passed to the command</param>
        public void RunCommand(string[] cmdparams)
        {
            List<string> args = new List<string>(cmdparams);
            if (args.Count < 1)
                return;

            string command = args[0];
            args.RemoveAt(0);

            cmdparams = args.ToArray();

            switch (command)
            {
                case "reset":
                    if (cmdparams.Length > 0)
                        if (cmdparams[0] == "region")
                            ResetScene();
                    break;
                case "command-script":
                    if (cmdparams.Length > 0)
                    {
                        m_OpenSimBase.RunCommandScript(cmdparams[0]);
                    }
                    break;

                case "remove-region":
                    string regRemoveName = Util.CombineParams(cmdparams, 0);

                    IScene removeScene;
                    if (TryGetScene(regRemoveName, out removeScene))
                        RemoveRegion(removeScene, false);
                    else
                        m_log.Info ("no region with that name");
                    break;

                case "delete-region":
                    string regDeleteName = Util.CombineParams(cmdparams, 0);

                    IScene killScene;
                    if (TryGetScene(regDeleteName, out killScene))
                        RemoveRegion(killScene, true);
                    else
                        m_log.Info ("no region with that name");
                    break;
                case "restart-instance":
                    //This kills the instance and restarts it
                    MainConsole.Instance.EndConsoleProcessing();
                    break;
            }
        }

        /// <summary>
        /// Change the currently selected region.  The selected region is that operated upon by single region commands.
        /// </summary>
        /// <param name="cmdParams"></param>
        protected void ChangeSelectedRegion(string[] cmdparams)
        {
            if (cmdparams.Length > 2)
            {
                string newRegionName = Util.CombineParams(cmdparams, 2);
                ChangeSelectedRegion(newRegionName);
            }
            else
            {
                m_log.Info ("Usage: change region <region name>");
            }
        }

        /// <summary>
        /// Turn on some debugging values for OpenSim.
        /// </summary>
        /// <param name="args"></param>
        protected void Debug(string[] args)
        {
            if (args.Length == 1)
                return;

            switch (args[1])
            {
                case "packet":
                    if (args.Length > 2)
                    {
                        int newDebug;
                        if (int.TryParse(args[2], out newDebug))
                        {
                            SetDebugPacketLevelOnCurrentScene(newDebug);
                        }
                        else
                        {
                            m_log.Info ("packet debug should be 0..255");
                        }
                        m_log.Info (String.Format ("New packet debug: {0}", newDebug));
                    }

                    break;
                default:

                    m_log.Info ("Unknown debug");
                    break;
            }
        }

        /// <summary>
        /// Set the debug packet level on the current scene.  This level governs which packets are printed out to the
        /// console.
        /// </summary>
        /// <param name="newDebug"></param>
        private void SetDebugPacketLevelOnCurrentScene(int newDebug)
        {
            ForEachCurrentScene(
                delegate(IScene scene)
                {
                    scene.ForEachScenePresence (delegate (IScenePresence scenePresence)
                    {
                        if (!scenePresence.IsChildAgent)
                        {
                            m_log.DebugFormat("Packet debug for {0} set to {1}",
                                              scenePresence.Name,
                                              newDebug);

                            scenePresence.ControllingClient.SetDebugPacketLevel(newDebug);
                        }
                    });
                }
            );
        }

        public void HandleShowUsers (string[] cmd)
        {
            List<string> args = new List<string> (cmd);
            args.RemoveAt (0);
            string[] showParams = args.ToArray ();
            
            IList agents;
            if (showParams.Length > 1 && showParams[1] == "full")
            {
                agents = CurrentOrFirstScene.GetScenePresences ();
            }
            else
            {
                agents = CurrentOrFirstScene.GetScenePresences ();
            }

            m_log.Info (String.Format ("\nAgents connected: {0}\n", agents.Count));

            m_log.Info (String.Format ("{0,-16}{1,-16}{2,-37}{3,-11}{4,-16}{5,-30}", "Firstname", "Lastname", "Agent ID", "Root/Child", "Region", "Position"));

            foreach (IScenePresence presence in agents)
            {
                RegionInfo regionInfo = presence.Scene.RegionInfo;
                string regionName;

                if (regionInfo == null)
                {
                    regionName = "Unresolvable";
                }
                else
                {
                    regionName = regionInfo.RegionName;
                }

                m_log.Info (String.Format ("{0,-16}{1,-37}{2,-11}{3,-16}{4,-30}", presence.Name, presence.UUID, presence.IsChildAgent ? "Child" : "Root", regionName, presence.AbsolutePosition.ToString ()));
            }

            m_log.Info (String.Empty);
            m_log.Info (String.Empty);
        }

        public void HandleShowRegions (string[] cmd)
        {
            ForEachScene (delegate (IScene scene)
            {
                m_log.Info (scene.ToString ());
            });
        }

        public void HandleShowMaturity (string[] cmd)
        {
            ForEachCurrentScene (delegate (IScene scene)
            {
                string rating = "";
                if (scene.RegionInfo.RegionSettings.Maturity == 1)
                {
                    rating = "Mature";
                }
                else if (scene.RegionInfo.RegionSettings.Maturity == 2)
                {
                    rating = "Adult";
                }
                else
                {
                    rating = "PG";
                }
                m_log.Info (String.Format ("Region Name: {0}, Region Rating {1}", scene.RegionInfo.RegionName, rating));
            });
        }

        public void SendCommandToPluginModules(string[] cmdparams)
        {
            ForEachCurrentScene(delegate(IScene scene) { scene.EventManager.TriggerOnPluginConsole(cmdparams); });
        }

        public void SetBypassPermissionsOnCurrentScene(bool bypassPermissions)
        {
            ForEachCurrentScene(delegate(IScene scene) { scene.Permissions.SetBypassPermissions(bypassPermissions); });
        }

        /// <summary>
        /// Load region data from Xml2Format
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmdparams"></param>
        protected void LoadXml2(string[] cmdparams)
        {
            if (cmdparams.Length > 2)
            {
                try
                {
                    IRegionSerialiserModule serialiser = CurrentOrFirstScene.RequestModuleInterface<IRegionSerialiserModule>();
                    if (serialiser != null)
                        serialiser.LoadPrimsFromXml2(CurrentOrFirstScene, cmdparams[2]);
                }
                catch (FileNotFoundException)
                {
                    m_log.Info ("Specified xml not found. Usage: load xml2 <filename>");
                }
            }
            else
            {
                m_log.Warn("Not enough parameters!");
            }
        }

        /// <summary>
        /// Load a whole region from an opensimulator archive.
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void LoadOar(string[] cmdparams)
        {
            try
            {
                IRegionArchiverModule archiver = CurrentOrFirstScene.RequestModuleInterface<IRegionArchiverModule>();
                if (archiver != null)
                    archiver.HandleLoadOarConsoleCommand(string.Empty, cmdparams);
            }
            catch (Exception e)
            {
                m_log.Error (e.ToString ());
            }
        }

        /// <summary>
        /// Save a region to a file, including all the assets needed to restore it.
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void SaveOar(string[] cmdparams)
        {
            IRegionArchiverModule archiver = CurrentOrFirstScene.RequestModuleInterface<IRegionArchiverModule>();
            if (archiver != null)
                archiver.HandleSaveOarConsoleCommand(string.Empty, cmdparams);
        }

        /// <summary>
        /// Kill an object given its UUID.
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void KillUUID(string[] cmdparams)
        {
            if (cmdparams.Length > 2)
            {
                UUID id = UUID.Zero;
                ISceneEntity grp = null;
                IScene sc = null;

                if (!UUID.TryParse(cmdparams[2], out id))
                {
                    m_log.Info ("[KillUUID]: Error bad UUID format!");
                    return;
                }

                ForEachScene(delegate(IScene scene)
                {
                    ISceneChildEntity part = scene.GetSceneObjectPart (id);
                    if (part == null)
                        return;

                    grp = part.ParentEntity;
                    sc = scene;
                });

                if (grp == null)
                {
                    m_log.Info (String.Format ("[KillUUID]: Given UUID {0} not found!", id));
                }
                else
                {
                    m_log.Info (String.Format ("[KillUUID]: Found UUID {0} in scene {1}", id, sc.RegionInfo.RegionName));
                    try
                    {
                        IBackupModule backup = sc.RequestModuleInterface<IBackupModule>();
                        if (backup != null)
                            backup.DeleteSceneObjects (new ISceneEntity[1] { grp }, true);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat("[KillUUID]: Error while removing objects from scene: " + e);
                    }
                }
            }
            else
            {
                m_log.Info ("[KillUUID]: Usage: kill uuid <UUID>");
            }
        }

        #endregion
    }
}
