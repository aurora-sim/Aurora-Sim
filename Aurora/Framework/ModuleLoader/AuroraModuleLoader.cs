using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using log4net;

namespace Aurora.Framework
{
    public static class AuroraModuleLoader
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Module Loaders

        private static List<string> dllBlackList;
        private static bool firstLoad = true;
        private static List<Type> LoadedDlls = new List<Type>();
        private static Dictionary<string, Assembly> LoadedAssemblys = new Dictionary<string, Assembly>();

        public static List<T> PickupModules<T>()
        {
            return LoadModules<T>(Environment.CurrentDirectory);
        }
        
        /// <summary>
        /// Gets all modules found in the given directory. 
        /// Identifier is the name of the interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="moduleDir"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static List<T> LoadModules<T>(string moduleDir)
        {
            List<T> modules = new List<T>();
            if (firstLoad)
            {
                DirectoryInfo dir = new DirectoryInfo(moduleDir);
                dllBlackList = new List<string>();
                dllBlackList.Add(Path.Combine(dir.FullName, "NHibernate.ByteCode.Castle.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Antlr3.Runtime.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "AprSharp.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Axiom.MathLib.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "BclExtras35.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "BulletDotNET.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "C5.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Castle.Core.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Castle.DynamicProxy.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Castle.DynamicProxy2.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Community.CsharpSqlite.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Community.CsharpSqlite.Sqlite.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "CSJ2K.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "DotNetOpenId.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "DotNetOpenMail.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "DotSets.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Fadd.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Fadd.Globalization.Yaml.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "FluentNHibernate.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "GlynnTucker.Cache.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Google.ProtocolBuffers.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "GoogleTranslateAPI.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "HttpServer.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Iesi.Collections.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "intl3_svn.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Kds.Serialization.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "libapr.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "libapriconv.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "libaprutil.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "libbulletnet.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "libdb44d.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "libdb_dotNET43.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "libeay32.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "log4net.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Modified.XnaDevRu.BulletX.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Mono.Addins.CecilReflector.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Mono.Addins.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Mono.Addins.Setup.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Mono.Data.Sqlite.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Mono.Data.SqliteClient.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Mono.GetOptions.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Mono.PEToolkit.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Mono.Security.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "MonoXnaCompactMaths.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "MXP.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "MySql.Data.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "NDesk.Options.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Newtonsoft.Json.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Newtonsoft.Json.Net20.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "NHibernate.ByteCode.Castle.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "NHibernate.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "HttpServer_OpenSim.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Nini.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Npgsql.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "nunit.framework.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "ode.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Ode.NET.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "openjpeg-dotnet-x86_64.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "openjpeg-dotnet.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "OpenMetaverse.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "OpenMetaverse.Rendering.Meshmerizer.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "OpenMetaverse.Http.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "OpenMetaverse.StructuredData.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "OpenMetaverse.Utilities.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "OpenMetaverseTypes.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "PhysX-wrapper.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "PhysX_Wrapper_Dotnet.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "protobuf-net.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "PumaCode.SvnDotNet.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "RAIL.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "SmartThreadPool.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "sqlite3.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "ssleay32.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "SubversionSharp.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "svn_client-1.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "System.Data.SQLite.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "Tools.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "XMLRPC.dll"));
                dllBlackList.Add(Path.Combine(dir.FullName, "xunit.dll"));
                foreach (FileInfo fileInfo in dir.GetFiles("*.dll"))
                {
                    modules.AddRange(LoadModulesFromDLL<T>(fileInfo.FullName));
                }
                //firstLoad = false;
            }
            else
            {
                try
                {
                    foreach (Type pluginType in LoadedDlls)
                    {
                        try
                        {
                            if (pluginType.IsPublic)
                            {
                                if (!pluginType.IsAbstract)
                                {
                                    if (pluginType.GetInterface(typeof(T).Name) != null)
                                    {
                                        modules.Add((T)Activator.CreateInstance(pluginType));
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return modules;
        }
        
        private static List<T> LoadModulesFromDLL<T>(string dllName)
        {
            List<T> modules = new List<T>();
            if (dllBlackList.Contains(dllName))
                return modules;
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
                        try
                        {
                            if (pluginType.IsPublic)
                            {
                                if (!pluginType.IsAbstract)
                                {
                                    if (firstLoad)
                                    {
                                        //Only add on the first load
                                        //if (!LoadedDlls.Contains(pluginType))
                                        //    LoadedDlls.Add(pluginType);
                                    }
                                    if (pluginType.GetInterface(typeof(T).Name, true) != null)
                                    {
                                        modules.Add((T)Activator.CreateInstance(pluginType));
                                    }
                                }
                            }
                        }
                        catch (Exception ex )
                        {
                            m_log.Warn("[MODULELOADER]: Error loading module " + pluginType.Name + " in file " + dllName + " : " + ex.ToString());
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return modules;
        }

        #endregion

        public static T LoadPlugin<T>(string dllName, string type) 
        {
            try
            {
                Assembly pluginAssembly = Assembly.LoadFrom(dllName);

                foreach (Type pluginType in pluginAssembly.GetTypes())
                {
                    if (pluginType.IsPublic)
                    {
                        try
                        {
                            Type typeInterface = pluginType.GetInterface(type, true);

                            if (typeInterface != null)
                            {
                                return (T)Activator.CreateInstance(pluginType);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            } 
            catch (ReflectionTypeLoadException e)
            {
                foreach (Exception e2 in e.LoaderExceptions)
                {
                    m_log.Error(e2.ToString());
                }
                throw e;
            }
            return default(T);
        }
    }
}
