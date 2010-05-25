using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public static class AuroraModuleLoader
    {
        #region New Style Loaders

        public static List<T> LoadPlugins<T>(string loaderString, PluginInitialiserBase baseInit) where T : IPlugin
        {
            using (PluginLoader<T> loader = new PluginLoader<T>(baseInit))
            {
                loader.Load(loaderString);
                return loader.Plugins;
            }
        }

        #endregion

        //Decapriated 24-5-10 - Revolution Smythe
        // Commit 
        /*#region Old Style Region Loaders

        static List<string> dllBlackList;
        static bool firstLoad = true;
        /// <summary>
        /// Gets all modules found in the given directory. 
        /// Identifier is the name of the interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="moduleDir"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        
        public static List<T> PickupModules<T>(string moduleDir, string identifier)
        {
            List<T> modules = new List<T>();
            if (firstLoad)
            {
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
                DirectoryInfo dir = new DirectoryInfo(moduleDir);
                firstLoad = false;
                foreach (FileInfo fileInfo in dir.GetFiles("*.dll"))
                {
                    modules.AddRange(LoadRegionModules<T>(fileInfo.FullName, identifier));
                }
            }
            else
            {
                try
                {
                    foreach (Type pluginType in LoadedDlls)
                    {
                        if (pluginType.IsPublic)
                        {
                            if (!pluginType.IsAbstract)
                            {
                                if (pluginType.GetInterface(identifier) != null)
                                {
                                    modules.Add((T)Activator.CreateInstance(pluginType));
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return modules;
        }

        private static List<T> LoadRegionModules<T>(string dllName, string identifier)
        {
            T[] modules = LoadModules<T>(dllName, identifier);
            List<T> initializedModules = new List<T>();

            if (modules.Length > 0)
            {
                foreach (T module in modules)
                {
                    initializedModules.Add(module);
                }
            }
            return initializedModules;
        }
        private static List<Type> LoadedDlls = new List<Type>();
        private static Dictionary<string, Assembly> LoadedAssemblys = new Dictionary<string, Assembly>();
        private static T[] LoadModules<T>(string dllName, string identifier)
        {
            List<T> modules = new List<T>();
            if (dllBlackList.Contains(dllName))
                return modules.ToArray();
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
                }
            }

            if (pluginAssembly != null)
            {
                try
                {
                    foreach (Type pluginType in pluginAssembly.GetTypes())
                    {
                        if (!LoadedDlls.Contains(pluginType))
                            LoadedDlls.Add(pluginType);
                        if (pluginType.GetInterface(identifier) != null)
                        {
                            if (pluginType.IsPublic)
                            {
                                if (!pluginType.IsAbstract)
                                {
                                    modules.Add((T)Activator.CreateInstance(pluginType));
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return modules.ToArray();
        }


        public static List<string> FindModules(string moduleDir, string identifier, string blockedDll)
        {
            DirectoryInfo dir = new DirectoryInfo(moduleDir);
            List<string> modules = new List<string>();

            foreach (FileInfo fileInfo in dir.GetFiles("*.dll"))
            {
                if (fileInfo.Name != blockedDll)
                    modules.AddRange(FindModulesByDLL(fileInfo, identifier));
                else
                    continue;
            }
            return modules;
        }
        private static string[] FindModulesByDLL(FileInfo fileInfo, string identifier)
        {
            List<string> modules = new List<string>();

            Assembly pluginAssembly;
            if (!LoadedAssemblys.TryGetValue(fileInfo.FullName, out pluginAssembly))
            {
                try
                {
                    pluginAssembly = Assembly.LoadFrom(fileInfo.FullName);
                    LoadedAssemblys.Add(fileInfo.FullName, pluginAssembly);
                }
                catch (BadImageFormatException)
                {
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
                                if (pluginType.GetInterface(identifier) != null)
                                {
                                    modules.Add(fileInfo.Name+":"+pluginType.Name);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return modules.ToArray();
        }

        #endregion*/
    }
}
