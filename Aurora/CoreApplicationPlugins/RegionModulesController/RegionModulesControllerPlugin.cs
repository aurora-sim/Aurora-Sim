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

using Aurora.Framework;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.ModuleLoader;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Services;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Aurora.CoreApplicationPlugins
{
    public class RegionModulesControllerPlugin : IRegionModulesController, IApplicationPlugin
    {
        protected List<IRegionModuleBase> IRegionModuleBaseModules = new List<IRegionModuleBase>();

        // Config access

        // Our name
        private const string m_name = "RegionModulesControllerPlugin";
        private ISimulationBase m_openSim;

        #region IApplicationPlugin Members

        public string Name
        {
            get { return m_name; }
        }

        #endregion

        #region IRegionModulesController implementation

        // The root of all evil.
        // This is where we handle adding the modules to scenes when they
        // load. This means that here we deal with replaceable interfaces,
        // nonshared modules, etc.
        //

        protected Dictionary<IScene, Dictionary<string, IRegionModuleBase>> RegionModules =
            new Dictionary<IScene, Dictionary<string, IRegionModuleBase>>();

        public void AddRegionToModules(IScene scene)
        {
            Dictionary<Type, INonSharedRegionModule> deferredNonSharedModules =
                new Dictionary<Type, INonSharedRegionModule>();

            // We need this to see if a module has already been loaded and
            // has defined a replaceable interface. It's a generic call,
            // so this can't be used directly. It will be used later
            Type s = scene.GetType();
            MethodInfo mi = s.GetMethod("RequestModuleInterface");

            // Scan for, and load, nonshared modules
            List<INonSharedRegionModule> list = new List<INonSharedRegionModule>();
            List<INonSharedRegionModule> m_nonSharedModules = AuroraModuleLoader.PickupModules<INonSharedRegionModule>();
            foreach (INonSharedRegionModule module in m_nonSharedModules)
            {
                Type replaceableInterface = module.ReplaceableInterface;
                if (replaceableInterface != null)
                {
                    MethodInfo mii = mi.MakeGenericMethod(replaceableInterface);

                    if (mii.Invoke(scene, new object[0]) != null)
                    {
                        MainConsole.Instance.DebugFormat("[REGIONMODULE]: Not loading {0} because another module has registered {1}",
                                          module.Name, replaceableInterface);
                        continue;
                    }

                    deferredNonSharedModules[replaceableInterface] = module;
                    MainConsole.Instance.DebugFormat("[REGIONMODULE]: Deferred load of {0}", module.Name);
                    continue;
                }

                //MainConsole.Instance.DebugFormat("[REGIONMODULE]: Adding scene {0} to non-shared module {1}",
                //                  scene.RegionInfo.RegionName, module.Name);

                // Initialise the module
                module.Initialise(m_openSim.ConfigSource);

                IRegionModuleBaseModules.Add(module);
                list.Add(module);
            }

            // Now add the modules that we found to the scene. If a module
            // wishes to override a replaceable interface, it needs to
            // register it in Initialise, so that the deferred module
            // won't load.
            foreach (INonSharedRegionModule module in list)
            {
                try
                {
                    module.AddRegion(scene);
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.ErrorFormat("[RegionModulesControllerPlugin]: Failed to load module {0}: {1}", module.Name, ex.ToString());
                }
                AddRegionModule(scene, module.Name, module);
            }

            // Same thing for nonshared modules, load them unless overridden
            List<INonSharedRegionModule> deferredlist =
                new List<INonSharedRegionModule>();

            foreach (INonSharedRegionModule module in deferredNonSharedModules.Values)
            {
                // Check interface override
                Type replaceableInterface = module.ReplaceableInterface;
                if (replaceableInterface != null)
                {
                    MethodInfo mii = mi.MakeGenericMethod(replaceableInterface);

                    if (mii.Invoke(scene, new object[0]) != null)
                    {
                        MainConsole.Instance.DebugFormat("[REGIONMODULE]: Not loading {0} because another module has registered {1}",
                                          module.Name, replaceableInterface);
                        continue;
                    }
                }

                MainConsole.Instance.DebugFormat("[REGIONMODULE]: Adding scene {0} to non-shared module {1} (deferred)",
                                  scene.RegionInfo.RegionName, module.Name);

                try
                {
                    module.Initialise(m_openSim.ConfigSource);
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.ErrorFormat("[RegionModulesControllerPlugin]: Failed to load module {0}: {1}", module.Name, ex.ToString());
                }

                IRegionModuleBaseModules.Add(module);
                list.Add(module);
                deferredlist.Add(module);
            }

            // Finally, load valid deferred modules
            foreach (INonSharedRegionModule module in deferredlist)
            {
                try
                {
                    module.AddRegion(scene);
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.ErrorFormat("[RegionModulesControllerPlugin]: Failed to load module {0}: {1}", module.Name, ex.ToString());
                }
                AddRegionModule(scene, module.Name, module);
            }

            // This is needed for all module types. Modules will register
            // Interfaces with scene in AddScene, and will also need a means
            // to access interfaces registered by other modules. Without
            // this extra method, a module attempting to use another modules's
            // interface would be successful only depending on load order,
            // which can't be depended upon, or modules would need to resort
            // to ugly kludges to attempt to request interfaces when needed
            // and unneccessary caching logic repeated in all modules.
            // The extra function stub is just that much cleaner
            //
            foreach (INonSharedRegionModule module in list)
            {
                try
                {
                    module.RegionLoaded(scene);
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.ErrorFormat("[RegionModulesControllerPlugin]: Failed to load module {0}: {1}", module.Name, ex.ToString());
                }
            }
        }

        public void RemoveRegionFromModules(IScene scene)
        {
            foreach (IRegionModuleBase module in RegionModules[scene].Values)
            {
                MainConsole.Instance.DebugFormat("[REGIONMODULE]: Removing scene {0} from module {1}",
                                  scene.RegionInfo.RegionName, module.Name);
                module.RemoveRegion(scene);
                if (module is INonSharedRegionModule)
                {
                    // as we were the only user, this instance has to die
                    module.Close();
                }
            }
            RegionModules[scene].Clear();
        }

        private void AddRegionModule(IScene scene, string p, IRegionModuleBase module)
        {
            if (!RegionModules.ContainsKey(scene))
                RegionModules.Add(scene, new Dictionary<string, IRegionModuleBase>());
            RegionModules[scene][p] = module;
        }

        #endregion

        #region IRegionModulesController Members

        public List<IRegionModuleBase> AllModules
        {
            get { return IRegionModuleBaseModules; }
        }

        #endregion

        #region IApplicationPlugin implementation

        public void PreStartup(ISimulationBase simBase)
        {
        }

        public void Initialize(ISimulationBase openSim)
        {
            m_openSim = openSim;

            IConfig handlerConfig = openSim.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("RegionModulesControllerPlugin", "") != Name)
                return;

            m_openSim.ApplicationRegistry.RegisterModuleInterface<IRegionModulesController>(this);
        }

        public void ReloadConfiguration(IConfigSource config)
        {
            //Update all modules that we have here
            foreach (IRegionModuleBase module in AllModules)
            {
                try
                {
                    module.Initialise(config);
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.ErrorFormat("[RegionModulesControllerPlugin]: Failed to load module {0}: {1}", module.Name, ex.ToString());
                }
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

        public void Close()
        {
        }

        #endregion
    }
}