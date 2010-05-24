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
using System.IO;
using System.Net;
using System.Reflection;
using OpenMetaverse;
using log4net;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Physics.Manager;
using Nini.Config;
using OpenSim.Framework.Console;
using OpenSim;

namespace OpenSim.Region.Framework.Scenes
{
    /// <summary>
    /// Manager for adding, closing and restarting scenes.
    /// </summary>
    public class SceneManager
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly List<Scene> m_localScenes;
        private Scene m_currentScene = null;
        private IOpenSimBase m_OpenSimBase;
        private StorageManager m_StorageManager;

        public List<Scene> Scenes
        {
            get { return m_localScenes; }
        }

        public Scene CurrentScene
        {
            get { return m_currentScene; }
        }

        public Scene CurrentOrFirstScene
        {
            get
            {
                if (m_currentScene == null)
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
                    return m_currentScene;
                }
            }
        }

        public SceneManager(IOpenSimBase OSB, StorageManager StorageManager)
        {
            m_StorageManager = StorageManager;
            m_OpenSimBase = OSB;
            m_localScenes = new List<Scene>();
        }

        public void Close()
        {
            if (proxyUrl.Length > 0)
            {
                Util.XmlRpcCommand(proxyUrl, "Stop");
            }
            // collect known shared modules in sharedModules
            Dictionary<string, IRegionModule> sharedModules = new Dictionary<string, IRegionModule>();
            for (int i = 0; i < m_localScenes.Count; i++)
            {
                // extract known shared modules from scene
                foreach (string k in m_localScenes[i].Modules.Keys)
                {
                    if (m_localScenes[i].Modules[k].IsSharedModule &&
                        !sharedModules.ContainsKey(k))
                        sharedModules[k] = m_localScenes[i].Modules[k];
                }
                // close scene/region
                m_localScenes[i].Close();
            }

            // all regions/scenes are now closed, we can now safely
            // close all shared modules
            foreach (IRegionModule mod in sharedModules.Values)
            {
                mod.Close();
            }
        }

        public void Close(Scene cscene)
        {
            if (m_localScenes.Contains(cscene))
            {
                for (int i = 0; i < m_localScenes.Count; i++)
                {
                    if (m_localScenes[i].Equals(cscene))
                    {
                        m_localScenes[i].Close();
                    }
                }
            }
        }

        public void Add(Scene scene)
        {
            scene.OnRestart += HandleRestart;
            m_localScenes.Add(scene);
        }

        public void HandleRestart(RegionInfo rdata)
        {
            m_log.Error("[SCENEMANAGER]: Got Restart message for region:" + rdata.RegionName + " Sending up to main");
            int RegionSceneElement = -1;
            for (int i = 0; i < m_localScenes.Count; i++)
            {
                if (rdata.RegionName == m_localScenes[i].RegionInfo.RegionName)
                {
                    RegionSceneElement = i;
                }
            }

            // Now we make sure the region is no longer known about by the SceneManager
            // Prevents duplicates.

            if (RegionSceneElement >= 0)
            {
                m_localScenes.RemoveAt(RegionSceneElement);
            }

            // Restarting this sim.
            m_log.Info("[SCENEMANAGER]: Got restart signal");

            m_OpenSimBase.ShutdownClientServer(m_localScenes[RegionSceneElement].RegionInfo);
            IScene scene;
            CreateRegion(m_localScenes[RegionSceneElement].RegionInfo, true, out scene);
        }

        /// <summary>
        /// Load an xml file of prims in OpenSimulator's current 'xml2' file format to the current scene
        /// </summary>
        public void LoadCurrentSceneFromXml2(string filename)
        {
            IRegionSerialiserModule serialiser = CurrentOrFirstScene.RequestModuleInterface<IRegionSerialiserModule>();
            if (serialiser != null)
                serialiser.LoadPrimsFromXml2(CurrentOrFirstScene, filename);
        }

        /// <summary>
        /// Save the current scene to an OpenSimulator archive.  This archive will eventually include the prim's assets
        /// as well as the details of the prims themselves.
        /// </summary>
        /// <param name="cmdparams"></param>
        public void SaveCurrentSceneToArchive(string[] cmdparams)
        {
            IRegionArchiverModule archiver = CurrentOrFirstScene.RequestModuleInterface<IRegionArchiverModule>();
            if (archiver != null)
                archiver.HandleSaveOarConsoleCommand(string.Empty, cmdparams);
        }

        /// <summary>
        /// Load an OpenSim archive into the current scene.  This will load both the shapes of the prims and upload
        /// their assets to the asset service.
        /// </summary>
        /// <param name="cmdparams"></param>
        public void LoadArchiveToCurrentScene(string[] cmdparams)
        {
            IRegionArchiverModule archiver = CurrentOrFirstScene.RequestModuleInterface<IRegionArchiverModule>();
            if (archiver != null)
                archiver.HandleLoadOarConsoleCommand(string.Empty, cmdparams);
        }

        public void SendCommandToPluginModules(string[] cmdparams)
        {
            ForEachCurrentScene(delegate(Scene scene) { scene.SendCommandToPlugins(cmdparams); });
        }

        public void SetBypassPermissionsOnCurrentScene(bool bypassPermissions)
        {
            ForEachCurrentScene(delegate(Scene scene) { scene.Permissions.SetBypassPermissions(bypassPermissions); });
        }

        private void ForEachCurrentScene(Action<Scene> func)
        {
            if (m_currentScene == null)
            {
                m_localScenes.ForEach(func);
            }
            else
            {
                func(m_currentScene);
            }
        }

        public void RestartCurrentScene()
        {
            ForEachCurrentScene(delegate(Scene scene) { scene.RestartNow(); });
        }

        public void BackupCurrentScene()
        {
            ForEachCurrentScene(delegate(Scene scene) { scene.Backup(); });
        }

        public bool TrySetCurrentScene(string regionName)
        {
            if ((String.Compare(regionName, "root") == 0) 
                || (String.Compare(regionName, "..") == 0)
                || (String.Compare(regionName, "/") == 0))
            {
                m_currentScene = null;
                return true;
            }
            else
            {
                foreach (Scene scene in m_localScenes)
                {
                    if (String.Compare(scene.RegionInfo.RegionName, regionName, true) == 0)
                    {
                        m_currentScene = scene;
                        return true;
                    }
                }

                return false;
            }
        }

        public bool TrySetCurrentScene(UUID regionID)
        {
            m_log.Debug("Searching for Region: '" + regionID + "'");

            foreach (Scene scene in m_localScenes)
            {
                if (scene.RegionInfo.RegionID == regionID)
                {
                    m_currentScene = scene;
                    return true;
                }
            }

            return false;
        }

        #region TryGetScene functions

        public bool TryGetScene(string regionName, out Scene scene)
        {
            foreach (Scene mscene in m_localScenes)
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

        public bool TryGetScene(UUID regionID, out Scene scene)
        {
            foreach (Scene mscene in m_localScenes)
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

        public bool TryGetScene(uint locX, uint locY, out Scene scene)
        {
            foreach (Scene mscene in m_localScenes)
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

        public bool TryGetScene(IPEndPoint ipEndPoint, out Scene scene)
        {
            foreach (Scene mscene in m_localScenes)
            {
                if ((mscene.RegionInfo.InternalEndPoint.Equals(ipEndPoint.Address)) &&
                    (mscene.RegionInfo.InternalEndPoint.Port == ipEndPoint.Port))
                {
                    scene = mscene;
                    return true;
                }
            }
            
            scene = null;
            return false;
        }

        #endregion

        /// <summary>
        /// Set the debug packet level on the current scene.  This level governs which packets are printed out to the
        /// console.
        /// </summary>
        /// <param name="newDebug"></param>
        public void SetDebugPacketLevelOnCurrentScene(int newDebug)
        {
            ForEachCurrentScene(
                delegate(Scene scene)
                {
                    scene.ForEachScenePresence(delegate(ScenePresence scenePresence)
                    {
                        if (!scenePresence.IsChildAgent)
                        {
                            m_log.DebugFormat("Packet debug for {0} {1} set to {2}",
                                              scenePresence.Firstname,
                                              scenePresence.Lastname,
                                              newDebug);

                            scenePresence.ControllingClient.SetDebugPacketLevel(newDebug);
                        }
                    });
                }
            );
        }

        public List<ScenePresence> GetCurrentSceneAvatars()
        {
            List<ScenePresence> avatars = new List<ScenePresence>();

            ForEachCurrentScene(
                delegate(Scene scene)
                {
                    scene.ForEachScenePresence(delegate(ScenePresence scenePresence)
                    {
                        if (!scenePresence.IsChildAgent)
                            avatars.Add(scenePresence);
                    });
                }
            );

            return avatars;
        }

        public List<ScenePresence> GetCurrentScenePresences()
        {
            List<ScenePresence> presences = new List<ScenePresence>();

            ForEachCurrentScene(delegate(Scene scene)
            {
                scene.ForEachScenePresence(delegate(ScenePresence sp)
                {
                    presences.Add(sp);
                });
            });

            return presences;
        }

        public void ForceCurrentSceneClientUpdate()
        {
            ForEachCurrentScene(delegate(Scene scene) { scene.ForceClientUpdate(); });
        }

        public void HandleEditCommandOnCurrentScene(string[] cmdparams)
        {
            ForEachCurrentScene(delegate(Scene scene) { scene.HandleEditCommand(cmdparams); });
        }

        public bool TryGetScenePresence(UUID avatarId, out ScenePresence avatar)
        {
            foreach (Scene scene in m_localScenes)
            {
                if (scene.TryGetScenePresence(avatarId, out avatar))
                {
                    return true;
                }
            }

            avatar = null;
            return false;
        }

        public bool TryGetAvatarsScene(UUID avatarId, out Scene scene)
        {
            ScenePresence avatar = null;
            foreach (Scene mScene in m_localScenes)
            {
                if (mScene.TryGetScenePresence(avatarId, out avatar))
                {
                    scene = mScene;
                    return true;
                }
            }

            scene = null;
            return false;
        }

        public void CloseScene(Scene scene)
        {
            m_localScenes.Remove(scene);
            scene.Close();
        }

        public bool TryGetAvatarByName(string avatarName, out ScenePresence avatar)
        {
            foreach (Scene scene in m_localScenes)
            {
                if (scene.TryGetAvatarByName(avatarName, out avatar))
                {
                    return true;
                }
            }

            avatar = null;
            return false;
        }

        public void ForEachScene(Action<Scene> action)
        {
            m_localScenes.ForEach(action);
        }

        private string proxyUrl = "";
        private int proxyOffset = 0;
        private string SecretID = UUID.Random().ToString();

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="portadd_flag"></param>
        /// <returns></returns>
        public IClientNetworkServer CreateRegion(RegionInfo regionInfo, bool portadd_flag, out IScene scene)
        {
            return CreateRegion(regionInfo, portadd_flag, false, out scene);
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <returns></returns>
        public IClientNetworkServer CreateRegion(RegionInfo regionInfo, out IScene scene)
        {
            return CreateRegion(regionInfo, false, true, out scene);
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="portadd_flag"></param>
        /// <param name="do_post_init"></param>
        /// <returns></returns>
        public IClientNetworkServer CreateRegion(RegionInfo regionInfo, bool portadd_flag, bool do_post_init, out IScene mscene)
        {
            int port = regionInfo.InternalEndPoint.Port;

            // set initial ServerURI
            regionInfo.ServerURI = "http://" + regionInfo.ExternalHostName + ":" + regionInfo.InternalEndPoint.Port;
            regionInfo.HttpPort = MainServer.Instance.Port;

            regionInfo.osSecret = SecretID;

            IConfig networkConfig = m_OpenSimBase.ConfigSource.Configs["Network"];
            if (networkConfig != null)
            {
                proxyUrl = networkConfig.GetString("proxy_url", "");
                proxyOffset = Int32.Parse(networkConfig.GetString("proxy_offset", "0"));
            }

            if ((proxyUrl.Length > 0) && (portadd_flag))
            {
                // set proxy url to RegionInfo
                regionInfo.proxyUrl = proxyUrl;
                regionInfo.ProxyOffset = proxyOffset;
                Util.XmlRpcCommand(proxyUrl, "AddPort", port, port + proxyOffset, regionInfo.ExternalHostName);
            }

            IClientNetworkServer clientServer = null;
            Scene scene = SetupScene(regionInfo, proxyOffset, m_OpenSimBase.ConfigSource, out clientServer);

            //m_log.Info("[MODULES]: Loading Region's modules (old style)");

            List<IRegionModule> Modules = Aurora.Framework.AuroraModuleLoader.PickupModules<IRegionModule>(Environment.CurrentDirectory, "IRegionModule");

            // This needs to be ahead of the script engine load, so the
            // script module can pick up events exposed by a module
            foreach (IRegionModule module in Modules)
            {
                module.Initialise(scene, m_OpenSimBase.ConfigSource);
                scene.AddModule(module.Name, module); //should be doing this?
            }

            // Use this in the future, the line above will be deprecated soon
            //m_log.Info("[MODULES]: Loading Region's modules (new style)");
            IRegionModulesController controller;
            if (m_OpenSimBase.ApplicationRegistry.TryGet(out controller))
            {
                controller.AddRegionToModules(scene);
            }
            else
                m_log.Error("[MODULES]: The new RegionModulesController is missing...");

            scene.SetModuleInterfaces();

            // Prims have to be loaded after module configuration since some modules may be invoked during the load
            scene.LoadPrimsFromStorage(regionInfo.RegionID);

            // moved these here as the terrain texture has to be created after the modules are initialized
            // and has to happen before the region is registered with the grid.
            scene.CreateTerrainTexture();

            // TODO : Try setting resource for region xstats here on scene
            MainServer.Instance.AddStreamHandler(new Region.Framework.Scenes.RegionStatsHandler(regionInfo));

            RegisterRegionWithGrid(scene);

            // We need to do this after we've initialized the
            // scripting engines.
            scene.CreateScriptInstances();

            scene.loadAllLandObjectsFromStorage(regionInfo.RegionID);
            scene.EventManager.TriggerParcelPrimCountUpdate();

            Add(scene);

            m_OpenSimBase.ClientServers.Add(clientServer);
            clientServer.Start();

            if (do_post_init)
            {
                foreach (IRegionModule module in Modules)
                {
                    module.PostInitialise();
                }
            }
            scene.EventManager.OnShutdown += delegate() { ShutdownRegion(scene); };

            mscene = scene;

            scene.StartTimer();

            return clientServer;
        }

        private void RegisterRegionWithGrid(Scene scene)
        {
            string error = scene.RegisterRegionWithGrid();
            if (error != "")
            {
                if (error == "Region location is reserved")
                {
                    m_log.Error("[STARTUP]: Registration of region with grid failed - The region location you specified is reserved. You must move your region.");
                    scene.RegionInfo.RegionLocY = uint.Parse(MainConsole.Instance.CmdPrompt("New Region Location X", "1000"));
                    scene.RegionInfo.RegionLocX = uint.Parse(MainConsole.Instance.CmdPrompt("New Region Location Y", "1000"));

                    Aurora.DataManager.DataManager.IRegionInfoConnector.UpdateRegionInfo(scene.RegionInfo, false);
                }
                if (error == "Region overlaps another region")
                {
                    m_log.Error("[STARTUP]: Registration of region with grid failed - The region location you specified is already in use. You must move your region.");
                    scene.RegionInfo.RegionLocY = uint.Parse(MainConsole.Instance.CmdPrompt("New Region Location X", "1000"));
                    scene.RegionInfo.RegionLocX = uint.Parse(MainConsole.Instance.CmdPrompt("New Region Location Y", "1000"));

                    Aurora.DataManager.DataManager.IRegionInfoConnector.UpdateRegionInfo(scene.RegionInfo, false);
                }
                if (error.Contains("Can't move this region"))
                {
                    m_log.Error("[STARTUP]: Registration of region with grid failed - You can not move this region. Moving it back to its original position.");
                    //Opensim Grid Servers don't have this functionality.
                    try
                    {
                        string[] position = error.Split(',');

                        scene.RegionInfo.RegionLocY = uint.Parse(position[1]);
                        scene.RegionInfo.RegionLocX = uint.Parse(position[2]);

                        Aurora.DataManager.DataManager.IRegionInfoConnector.UpdateRegionInfo(scene.RegionInfo, false);
                    }
                    catch (Exception e)
                    {
                        m_log.Error("Unable to move the region back to its original position, is this an opensim server? Please manually move the region back.");
                        throw e;
                    }
                }
                if (error == "Duplicate region name")
                {
                    m_log.Error("[STARTUP]: Registration of region with grid failed - The region name you specified is already in use. Please change the name.");
                    scene.RegionInfo.RegionName = MainConsole.Instance.CmdPrompt("New Region Name", "");

                    Aurora.DataManager.DataManager.IRegionInfoConnector.UpdateRegionInfo(scene.RegionInfo, false);
                }
                if (error == "Region locked out")
                {
                    m_log.Error("[STARTUP]: Registration of region with grid failed - The region you are attempting to join has been blocked from connecting. Please connect another region.");
                    throw new Exception(error);
                }
                if (error == "Error communicating with grid service")
                {
                    m_log.Error("[STARTUP]: Registration of region with grid failed - The grid service can not be found! Please make sure that you can connect to the grid server and that the grid server is on.");
                    string input = MainConsole.Instance.CmdPrompt("Press enter when you are ready to proceed, or type cancel to exit");
                    if (input == "cancel")
                    {
                        Environment.Exit(0);
                    }
                }
                RegisterRegionWithGrid(scene);
            }
        }

        private void ShutdownRegion(Scene scene)
        {
            m_log.DebugFormat("[SHUTDOWN]: Shutting down region {0}", scene.RegionInfo.RegionName);
            IRegionModulesController controller;
            if (m_OpenSimBase.ApplicationRegistry.TryGet<IRegionModulesController>(out controller))
            {
                controller.RemoveRegionFromModules(scene);
            }
        }

        public void RemoveRegion(IScene scene, bool cleanup)
        {
            // only need to check this if we are not at the
            // root level
            if ((CurrentScene != null) && (CurrentScene.RegionInfo.RegionID == scene.RegionInfo.RegionID))
            {
                TrySetCurrentScene("..");
            }

            ((Scene)scene).DeleteAllSceneObjects();
            CloseScene((Scene)scene);
            m_OpenSimBase.ShutdownClientServer(scene.RegionInfo);

            if (!cleanup)
                return;

            if (!String.IsNullOrEmpty(scene.RegionInfo.RegionFile))
            {
                if (scene.RegionInfo.RegionFile.ToLower().EndsWith(".xml"))
                {
                    File.Delete(scene.RegionInfo.RegionFile);
                    m_log.InfoFormat("[OPENSIM]: deleting region file \"{0}\"", scene.RegionInfo.RegionFile);
                }
                if (scene.RegionInfo.RegionFile.ToLower().EndsWith(".ini"))
                {
                    try
                    {
                        IniConfigSource source = new IniConfigSource(scene.RegionInfo.RegionFile);
                        if (source.Configs[scene.RegionInfo.RegionName] != null)
                        {
                            source.Configs.Remove(scene.RegionInfo.RegionName);

                            if (source.Configs.Count == 0)
                            {
                                File.Delete(scene.RegionInfo.RegionFile);
                            }
                            else
                            {
                                source.Save(scene.RegionInfo.RegionFile);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public void RemoveRegion(string name, bool cleanUp)
        {
            Scene target;
            if (TryGetScene(name, out target))
                RemoveRegion(target, cleanUp);
        }

        /// <summary>
        /// Remove a region from the simulator without deleting it permanently.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public void CloseRegion(IScene scene)
        {
            // only need to check this if we are not at the
            // root level
            if ((CurrentScene != null) && (CurrentScene.RegionInfo.RegionID == scene.RegionInfo.RegionID))
            {
                TrySetCurrentScene("..");
            }

            CloseScene((Scene)scene);
            m_OpenSimBase.ShutdownClientServer(scene.RegionInfo);
        }

        /// <summary>
        /// Remove a region from the simulator without deleting it permanently.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void CloseRegion(string name)
        {
            Scene target;
            if (TryGetScene(name, out target))
                CloseRegion(target);
        }

        /// <summary>
        /// Create a scene and its initial base structures.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="clientServer"> </param>
        /// <returns></returns>
        protected Scene SetupScene(RegionInfo regionInfo, out IClientNetworkServer clientServer)
        {
            return SetupScene(regionInfo, 0, null, out clientServer);
        }

        /// <summary>
        /// Create a scene and its initial base structures.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="proxyOffset"></param>
        /// <param name="configSource"></param>
        /// <param name="clientServer"> </param>
        /// <returns></returns>
        protected Scene SetupScene(RegionInfo regionInfo, int proxyOffset, IConfigSource configSource, out IClientNetworkServer clientServer)
        {
            AgentCircuitManager circuitManager = new AgentCircuitManager();
            IPAddress listenIP = regionInfo.InternalEndPoint.Address;
            //if (!IPAddress.TryParse(regionInfo.InternalEndPoint, out listenIP))
            //    listenIP = IPAddress.Parse("0.0.0.0");

            uint port = (uint)regionInfo.InternalEndPoint.Port;

            clientServer = m_OpenSimBase.CreateNetworkServer(listenIP, ref port, proxyOffset, regionInfo.m_allow_alternate_ports, circuitManager);

            regionInfo.InternalEndPoint.Port = (int)port;

            Scene scene = CreateScene(regionInfo, m_StorageManager, circuitManager);

            
            clientServer.AddScene(scene);

            scene.LoadWorldMap();

            scene.PhysicsScene = GetPhysicsScene(m_OpenSimBase.ConfigurationSettings.PhysicsEngine, m_OpenSimBase.ConfigurationSettings.MeshEngineName, m_OpenSimBase.ConfigSource, scene.RegionInfo.RegionName);
            scene.PhysicsScene.SetTerrain(scene.Heightmap.GetFloatsSerialised());
            scene.PhysicsScene.SetWaterLevel((float)regionInfo.RegionSettings.WaterHeight);

            return scene;
        }

        protected Scene CreateScene(RegionInfo regionInfo, StorageManager storageManager, AgentCircuitManager circuitManager)
        {
            SceneCommunicationService sceneGridService = new SceneCommunicationService();

            return new Scene(regionInfo, circuitManager, sceneGridService, storageManager, false, m_OpenSimBase.ConfigurationSettings.PhysicalPrim, m_OpenSimBase.ConfigurationSettings.See_into_region_from_neighbor, m_OpenSimBase.ConfigSource, m_OpenSimBase.Version);
        }

        /// <summary>
        /// Get a new physics scene.
        /// </summary>
        /// <param name="engine">The name of the physics engine to use</param>
        /// <param name="meshEngine">The name of the mesh engine to use</param>
        /// <param name="config">The configuration data to pass to the physics and mesh engines</param>
        /// <param name="osSceneIdentifier">
        /// The name of the OpenSim scene this physics scene is serving.  This will be used in log messages.
        /// </param>
        /// <returns></returns>
        protected PhysicsScene GetPhysicsScene(
            string engine, string meshEngine, IConfigSource config, string osSceneIdentifier)
        {
            PhysicsPluginManager physicsPluginManager;
            physicsPluginManager = new PhysicsPluginManager();
            physicsPluginManager.LoadPluginsFromAssemblies("Physics");

            return physicsPluginManager.GetPhysicsScene(engine, meshEngine, config, osSceneIdentifier);
        }
    }
}
