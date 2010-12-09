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
using System.Reflection;
using log4net;
using Nini.Config;
using OpenSim;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework;
using Aurora.Simulation.Base;

namespace OpenSim.ApplicationPlugins.RegionModulesController
{
    public class RegionModulesControllerPlugin : IRegionModulesController,
            IApplicationPlugin
    {
        // Logger
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        // Config access
        private ISimulationBase m_openSim;

        // Our name
        private string m_name = "RegionModulesControllerPlugin";

        // List of shared module instances, for adding to Scenes
        private List<ISharedRegionModule> m_sharedInstances =
                new List<ISharedRegionModule>();

        protected List<IRegionModuleBase> IRegionModuleBaseModules = new List<IRegionModuleBase>();
        public List<IRegionModuleBase> AllModules
        {
            get 
            {
                return IRegionModuleBaseModules;
            }
        }

#region IApplicationPlugin implementation

        public void Initialize (ISimulationBase openSim)
        {
            IConfig handlerConfig = openSim.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("RegionModulesControllerPlugin", "") != Name)
                return;

            m_openSim = openSim;
            m_openSim.ApplicationRegistry.RegisterInterface<IRegionModulesController>(this);
            // Scan modules and load all that aren't disabled
            m_sharedInstances = Aurora.Framework.AuroraModuleLoader.PickupModules<ISharedRegionModule>();
            foreach (ISharedRegionModule module in m_sharedInstances)
            {
                module.Initialise(m_openSim.ConfigSource);
            }
        }

        public void PostInitialise ()
        {
            IConfig handlerConfig = m_openSim.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("RegionModulesControllerPlugin", "") != Name)
                return;

            //m_log.DebugFormat("[REGIONMODULES]: PostInitializing...");

            // Immediately run PostInitialise on shared modules
            foreach (ISharedRegionModule module in m_sharedInstances)
            {
                module.PostInitialise();
            }
        }

        public void Close()
        {
        }

#endregion

#region IPlugin implementation

        // We don't do that here
        //
        public void Initialise ()
        {
            throw new System.NotImplementedException();
        }

#endregion

#region IDisposable implementation

        // Cleanup
        //
        public void Dispose ()
        {
            // We expect that all regions have been removed already
            while (m_sharedInstances.Count > 0)
            {
                m_sharedInstances[0].Close();
                m_sharedInstances.RemoveAt(0);
            }
        }

#endregion


        public string Name
        {
            get
            {
                return m_name;
            }
        }

#region IRegionModulesController implementation

        // The root of all evil.
        // This is where we handle adding the modules to scenes when they
        // load. This means that here we deal with replaceable interfaces,
        // nonshared modules, etc.
        //
        public void AddRegionToModules (Scene scene)
        {
            Dictionary<Type, ISharedRegionModule> deferredSharedModules =
                    new Dictionary<Type, ISharedRegionModule>();
            Dictionary<Type, INonSharedRegionModule> deferredNonSharedModules =
                    new Dictionary<Type, INonSharedRegionModule>();

            // We need this to see if a module has already been loaded and
            // has defined a replaceable interface. It's a generic call,
            // so this can't be used directly. It will be used later
            Type s = scene.GetType();
            MethodInfo mi = s.GetMethod("RequestModuleInterface");

            // This will hold the shared modules we actually load
            List<ISharedRegionModule> sharedlist =
                    new List<ISharedRegionModule>();

            // Iterate over the shared modules that have been loaded
            // Add them to the new Scene
            foreach (ISharedRegionModule module in m_sharedInstances)
            {
                // Here is where we check if a replaceable interface
                // is defined. If it is, the module is checked against
                // the interfaces already defined. If the interface is
                // defined, we simply skip the module. Else, if the module
                // defines a replaceable interface, we add it to the deferred
                // list.
                Type replaceableInterface = module.ReplaceableInterface;
                if (replaceableInterface != null)
                {
                    MethodInfo mii = mi.MakeGenericMethod(replaceableInterface);

                    if (mii.Invoke(scene, new object[0]) != null)
                    {
                        m_log.DebugFormat("[REGIONMODULE]: Not loading {0} because another module has registered {1}", module.Name, replaceableInterface.ToString());
                        continue;
                    }

                    deferredSharedModules[replaceableInterface] = module;
                    //m_log.DebugFormat("[REGIONMODULE]: Deferred load of {0}", module.Name);
                    continue;
                }

                //m_log.DebugFormat("[REGIONMODULE]: Adding scene {0} to shared module {1}",
                //                  scene.RegionInfo.RegionName, module.Name);

                module.AddRegion(scene);
                AddRegionModule(scene, module.Name, module);

                IRegionModuleBaseModules.Add(module);
                sharedlist.Add(module);
            }

            IConfig modulesConfig =
                    m_openSim.ConfigSource.Configs["Modules"];

            // Scan for, and load, nonshared modules
            List<INonSharedRegionModule> list = new List<INonSharedRegionModule>();
            /*foreach (TypeExtensionNode node in m_nonSharedModules)
            {
                Object[] ctorArgs = new Object[] {0};

                // Read the config
                string moduleString =
                        modulesConfig.GetString("Setup_" + node.Id, String.Empty);

                // Get the port number, if there is one
                if (moduleString != String.Empty)
                {
                    // Get the port number from the string
                    string[] moduleParts = moduleString.Split(new char[] {'/'},
                            2);
                    if (moduleParts.Length > 1)
                        ctorArgs[0] = Convert.ToUInt32(moduleParts[0]);
                }

                // Actually load it
                INonSharedRegionModule module = null;

                Type[] ctorParamTypes = new Type[ctorArgs.Length];
                for (int i = 0; i < ctorParamTypes.Length; i++)
                    ctorParamTypes[i] = ctorArgs[i].GetType();

                if (node.Type.GetConstructor(ctorParamTypes) != null)
                    module = (INonSharedRegionModule)Activator.CreateInstance(node.Type, ctorArgs);
                else
                    module = (INonSharedRegionModule)Activator.CreateInstance(node.Type);

                // Check for replaceable interfaces
                Type replaceableInterface = module.ReplaceableInterface;
                if (replaceableInterface != null)
                {
                    MethodInfo mii = mi.MakeGenericMethod(replaceableInterface);

                    if (mii.Invoke(scene, new object[0]) != null)
                    {
                        m_log.DebugFormat("[REGIONMODULE]: Not loading {0} because another module has registered {1}", module.Name, replaceableInterface.ToString());
                        continue;
                    }

                    deferredNonSharedModules[replaceableInterface] = module;
                    m_log.DebugFormat("[REGIONMODULE]: Deferred load of {0}", module.Name);
                    continue;
                }

                //m_log.DebugFormat("[REGIONMODULE]: Adding scene {0} to non-shared module {1}",
                //                  scene.RegionInfo.RegionName, module.Name);

                // Initialise the module
                module.Initialise(m_openSim.ConfigSource);

                IRegionModuleBaseModules.Add(module);
                list.Add(module);
            }*/

            List<INonSharedRegionModule> m_nonSharedModules = Aurora.Framework.AuroraModuleLoader.PickupModules<INonSharedRegionModule>();
            foreach (INonSharedRegionModule module in m_nonSharedModules)
            {
                Type replaceableInterface = module.ReplaceableInterface;
                if (replaceableInterface != null)
                {
                    MethodInfo mii = mi.MakeGenericMethod(replaceableInterface);

                    if (mii.Invoke(scene, new object[0]) != null)
                    {
                        m_log.DebugFormat("[REGIONMODULE]: Not loading {0} because another module has registered {1}", module.Name, replaceableInterface.ToString());
                        continue;
                    }

                    deferredNonSharedModules[replaceableInterface] = module;
                    m_log.DebugFormat("[REGIONMODULE]: Deferred load of {0}", module.Name);
                    continue;
                }

                if (module is ISharedRegionModule) //Don't load IShared!
                {
                    continue;
                }

                //m_log.DebugFormat("[REGIONMODULE]: Adding scene {0} to non-shared module {1}",
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
                module.AddRegion(scene);
                AddRegionModule(scene, module.Name, module);
            }

            // Now all modules without a replaceable base interface are loaded
            // Replaceable modules have either been skipped, or omitted.
            // Now scan the deferred modules here
            foreach (ISharedRegionModule module in deferredSharedModules.Values)
            {
                // Determine if the interface has been replaced
                Type replaceableInterface = module.ReplaceableInterface;
                MethodInfo mii = mi.MakeGenericMethod(replaceableInterface);

                if (mii.Invoke(scene, new object[0]) != null)
                {
                    m_log.DebugFormat("[REGIONMODULE]: Not loading {0} because another module has registered {1}", module.Name, replaceableInterface.ToString());
                    continue;
                }

                m_log.DebugFormat("[REGIONMODULE]: Adding scene {0} to shared module {1} (deferred)",
                                  scene.RegionInfo.RegionName, module.Name);

                // Not replaced, load the module
                module.AddRegion(scene);
                AddRegionModule(scene, module.Name, module);

                IRegionModuleBaseModules.Add(module);
                sharedlist.Add(module);
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
                        m_log.DebugFormat("[REGIONMODULE]: Not loading {0} because another module has registered {1}", module.Name, replaceableInterface.ToString());
                        continue;
                    }
                }

                m_log.DebugFormat("[REGIONMODULE]: Adding scene {0} to non-shared module {1} (deferred)",
                                  scene.RegionInfo.RegionName, module.Name);

                module.Initialise(m_openSim.ConfigSource);

                IRegionModuleBaseModules.Add(module);
                list.Add(module);
                deferredlist.Add(module);
            }

            // Finally, load valid deferred modules
            foreach (INonSharedRegionModule module in deferredlist)
            {
                module.AddRegion(scene);
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
            foreach (ISharedRegionModule module in sharedlist)
            {
                module.RegionLoaded(scene);
            }

            foreach (INonSharedRegionModule module in list)
            {
                module.RegionLoaded(scene);
            }
        }

        protected Dictionary<Scene, Dictionary<string, IRegionModuleBase>> RegionModules = new Dictionary<Scene, Dictionary<string, IRegionModuleBase>>();

        private void AddRegionModule(Scene scene, string p, IRegionModuleBase module)
        {
            if (!RegionModules.ContainsKey(scene))
                RegionModules.Add(scene, new Dictionary<string, IRegionModuleBase>());
            RegionModules[scene][p] = module;
        }

        public void RemoveRegionFromModules (Scene scene)
        {
            foreach (IRegionModuleBase module in RegionModules[scene].Values)
            {
                m_log.DebugFormat("[REGIONMODULE]: Removing scene {0} from module {1}",
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

#endregion
    }
}
