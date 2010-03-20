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
using System.Reflection;
using log4net;
using Nini.Config;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.Framework
{
    public class ModuleLoader
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Dictionary<string, Assembly> LoadedAssemblys = new Dictionary<string, Assembly>();

        private readonly List<IRegionModule> m_loadedModules = new List<IRegionModule>();
        private readonly Dictionary<string, IRegionModule> m_loadedSharedModules = new Dictionary<string, IRegionModule>();
        private readonly IConfigSource m_config;
        private readonly List<string> dllBlackList;

        public ModuleLoader(IConfigSource config)
        {
            m_config = config;
            dllBlackList = new List<string>();
            dllBlackList.Add("NHibernate.ByteCode.Castle.dll");
            dllBlackList.Add("Antlr3.Runtime.dll");
            dllBlackList.Add("AprSharp.dll");
            dllBlackList.Add("Axiom.MathLib.dll");
            dllBlackList.Add("BclExtras35.dll");
            dllBlackList.Add("BulletDotNET.dll");
            dllBlackList.Add("C5.dll");
            dllBlackList.Add("Castle.Core.dll");
            dllBlackList.Add("Castle.DynamicProxy.dll");
            dllBlackList.Add("Castle.DynamicProxy2.dll");
            dllBlackList.Add("CookComputing.XmlRpcV2.dll");
            dllBlackList.Add("CSJ2K.dll");
            dllBlackList.Add("DotNetOpenId.dll");
            dllBlackList.Add("DotNetOpenMail.dll");
            dllBlackList.Add("DotSets.dll");
            dllBlackList.Add("Fadd.dll");
            dllBlackList.Add("Fadd.Globalization.Yaml.dll");
            dllBlackList.Add("FluentNHibernate.dll");
            dllBlackList.Add("GlynnTucker.Cache.dll");
            dllBlackList.Add("Google.ProtocolBuffers.dll");
            dllBlackList.Add("HttpServer.dll");
            dllBlackList.Add("Iesi.Collections.dll");
            dllBlackList.Add("intl3_svn.dll");
            dllBlackList.Add("Kds.Serialization.dll");
            dllBlackList.Add("libapr.dll");
            dllBlackList.Add("libapriconv.dll");
            dllBlackList.Add("libaprutil.dll");
            dllBlackList.Add("libbulletnet.dll");
            dllBlackList.Add("libdb44d.dll");
            dllBlackList.Add("libdb_dotNET43.dll");
            dllBlackList.Add("libeay32.dll");
            dllBlackList.Add("log4net.dll");
            dllBlackList.Add("Modified.XnaDevRu.BulletX.dll");
            dllBlackList.Add("Mono.Addins.dll");
            dllBlackList.Add("Mono.Data.SqliteClient.dll");
            dllBlackList.Add("Mono.GetOptions.dll");
            dllBlackList.Add("Mono.PEToolkit.dll");
            dllBlackList.Add("Mono.Security.dll");
            dllBlackList.Add("MonoXnaCompactMaths.dll");
            dllBlackList.Add("MXP.dll");
            dllBlackList.Add("MySql.Data.dll");
            dllBlackList.Add("NDesk.Options.dll");
            dllBlackList.Add("Newtonsoft.Json.dll");
            dllBlackList.Add("NHibernate.ByteCode.Castle.dll");
            dllBlackList.Add("NHibernate.dll");
            dllBlackList.Add("HttpServer_OpenSim.dll");
            dllBlackList.Add("Nini.dll");
            dllBlackList.Add("Npgsql.dll");
            dllBlackList.Add("nunit.framework.dll");
            dllBlackList.Add("ode.dll");
            dllBlackList.Add("Ode.NET.dll");
            dllBlackList.Add("openjpeg-dotnet-x86_64.dll");
            dllBlackList.Add("openjpeg-dotnet.dll");
            dllBlackList.Add("OpenMetaverse.dll");
            dllBlackList.Add("OpenMetaverse.Http.dll");
            dllBlackList.Add("OpenMetaverse.StructuredData.dll");
            dllBlackList.Add("OpenMetaverse.Utilities.dll");
            dllBlackList.Add("OpenMetaverseTypes.dll");
            dllBlackList.Add("PhysX-wrapper.dll");
            dllBlackList.Add("PhysX_Wrapper_Dotnet.dll");
            dllBlackList.Add("protobuf-net.dll");
            dllBlackList.Add("PumaCode.SvnDotNet.dll");
            dllBlackList.Add("RAIL.dll");
            dllBlackList.Add("SmartThreadPool.dll");
            dllBlackList.Add("sqlite3.dll");
            dllBlackList.Add("ssleay32.dll");
            dllBlackList.Add("SubversionSharp.dll");
            dllBlackList.Add("svn_client-1.dll");
            dllBlackList.Add("System.Data.SQLite.dll");
            dllBlackList.Add("Tools.dll");
            dllBlackList.Add("XMLRPC.dll");
            dllBlackList.Add("xunit.dll");
        }

        public IRegionModule[] GetLoadedSharedModules
        {
            get
            {
                IRegionModule[] regionModules = new IRegionModule[m_loadedSharedModules.Count];
                m_loadedSharedModules.Values.CopyTo(regionModules, 0);
                return regionModules;
            }
        }

        public List<IRegionModule> PickupModules(Scene scene, string moduleDir)
        {
            DirectoryInfo dir = new DirectoryInfo(moduleDir);
            List<IRegionModule> modules = new List<IRegionModule>();

            foreach (FileInfo fileInfo in dir.GetFiles("*.dll"))
            {
                if (!dllBlackList.Contains(fileInfo.Name))
                {
                    modules.AddRange(LoadRegionModules(fileInfo.FullName, scene));
                }
            }
            return modules;
        }

        public void LoadDefaultSharedModule(IRegionModule module)
        {
            if (m_loadedSharedModules.ContainsKey(module.Name))
            {
                m_log.ErrorFormat("[MODULES]: Module name \"{0}\" already exists in module list. Module not added!", module.Name);
            }
            else
            {
                m_loadedSharedModules.Add(module.Name, module);
            }
        }


        public void InitialiseSharedModules(Scene scene)
        {
            foreach (IRegionModule module in m_loadedSharedModules.Values)
            {
                module.Initialise(scene, m_config);
                scene.AddModule(module.Name, module); //should be doing this?
            }
        }

        public void InitializeModule(IRegionModule module, Scene scene)
        {
            module.Initialise(scene, m_config);
            scene.AddModule(module.Name, module);
            m_loadedModules.Add(module);
        }

        /// <summary>
        ///  Loads/initialises a Module instance that can be used by multiple Regions
        /// </summary>
        /// <param name="dllName"></param>
        /// <param name="moduleName"></param>
        public void LoadSharedModule(string dllName, string moduleName)
        {
            IRegionModule module = LoadModule(dllName, moduleName);

            if (module != null)
                LoadSharedModule(module);
        }

        /// <summary>
        ///  Loads/initialises a Module instance that can be used by multiple Regions
        /// </summary>
        /// <param name="module"></param>
        public void LoadSharedModule(IRegionModule module)
        {
            if (!m_loadedSharedModules.ContainsKey(module.Name))
            {
                m_loadedSharedModules.Add(module.Name, module);
            }
        }

        public List<IRegionModule> LoadRegionModules(string dllName, Scene scene)
        {
            IRegionModule[] modules = LoadModules(dllName);
            List<IRegionModule> initializedModules = new List<IRegionModule>();

            if (modules.Length > 0)
            {
                m_log.InfoFormat("[MODULES]: Found Module Library [{0}]", dllName);
                foreach (IRegionModule module in modules)
                {
                    if (!module.IsSharedModule)
                    {
                        m_log.InfoFormat("[MODULES]:    [{0}]: Initializing.", module.Name);
                        InitializeModule(module, scene);
                        initializedModules.Add(module);
                    }
                    else
                    {
                        m_log.InfoFormat("[MODULES]:    [{0}]: Loading Shared Module.", module.Name);
                        LoadSharedModule(module);
                    }
                }
            }
            return initializedModules;
        }

        public void LoadRegionModule(string dllName, string moduleName, Scene scene)
        {
            IRegionModule module = LoadModule(dllName, moduleName);
            if (module != null)
            {
                InitializeModule(module, scene);
            }
        }

        /// <summary>
        /// Loads a external Module (if not already loaded) and creates a new instance of it.
        /// </summary>
        /// <param name="dllName"></param>
        /// <param name="moduleName"></param>
        public IRegionModule LoadModule(string dllName, string moduleName)
        {
            IRegionModule[] modules = LoadModules(dllName);

            foreach (IRegionModule module in modules)
            {
                if ((module != null) && (module.Name == moduleName))
                {
                    return module;
                }
            }

            return null;
        }

        public IRegionModule[] LoadModules(string dllName)
        {
            //m_log.DebugFormat("[MODULES]: Looking for modules in {0}", dllName);
            
            List<IRegionModule> modules = new List<IRegionModule>();

            Assembly pluginAssembly;
            if (!LoadedAssemblys.TryGetValue(dllName, out pluginAssembly))
            {
                try
                {
                    pluginAssembly = Assembly.LoadFrom(dllName);
                    LoadedAssemblys.Add(dllName, pluginAssembly);
                }
                catch (BadImageFormatException)
                {
                    //m_log.InfoFormat("[MODULES]: The file [{0}] is not a module assembly.", e.FileName);
                }
            }

            if (pluginAssembly != null)
            {
                try
                {
                    foreach (Type pluginType in pluginAssembly.GetTypes())
                    {
                        if (pluginType.IsPublic)
                        {
                            if (!pluginType.IsAbstract)
                            {
                                if (pluginType.GetInterface("IRegionModule") != null)
                                {
                                    modules.Add((IRegionModule)Activator.CreateInstance(pluginType));
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat(
                        "[MODULES]: Could not load types for [{0}].  Exception {1}", pluginAssembly.FullName, e);
                    
                    // justincc: Right now this is fatal to really get the user's attention
                    throw e;
                }
            }

            return modules.ToArray();
        }

        public void PostInitialise()
        {
            foreach (IRegionModule module in m_loadedSharedModules.Values)
            {
                module.PostInitialise();
            }

            foreach (IRegionModule module in m_loadedModules)
            {
                module.PostInitialise();
            }
        }

        public void ClearCache()
        {
            LoadedAssemblys.Clear();
        }

        public void UnloadModule(IRegionModule rm)
        {
            rm.Close();

            m_loadedModules.Remove(rm);
        }
    }
}
