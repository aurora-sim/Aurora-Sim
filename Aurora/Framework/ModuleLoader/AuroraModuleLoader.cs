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
                dllBlackList.Add("Community.CsharpSqlite.dll");
                dllBlackList.Add("Community.CsharpSqlite.Sqlite.dll");
                dllBlackList.Add("CSJ2K.dll");
                dllBlackList.Add("DotNetOpenId.dll");
                dllBlackList.Add("DotNetOpenMail.dll");
                dllBlackList.Add("DotSets.dll");
                dllBlackList.Add("Fadd.dll");
                dllBlackList.Add("Fadd.Globalization.Yaml.dll");
                dllBlackList.Add("FluentNHibernate.dll");
                dllBlackList.Add("GlynnTucker.Cache.dll");
                dllBlackList.Add("Google.ProtocolBuffers.dll");
                dllBlackList.Add("GoogleTranslateAPI.dll");
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
                dllBlackList.Add("Mono.Addins.CecilReflector.dll");
                dllBlackList.Add("Mono.Addins.dll");
                dllBlackList.Add("Mono.Addins.Setup.dll");
                dllBlackList.Add("Mono.Data.Sqlite.dll");
                dllBlackList.Add("Mono.Data.SqliteClient.dll");
                dllBlackList.Add("Mono.GetOptions.dll");
                dllBlackList.Add("Mono.PEToolkit.dll");
                dllBlackList.Add("Mono.Security.dll");
                dllBlackList.Add("MonoXnaCompactMaths.dll");
                dllBlackList.Add("MXP.dll");
                dllBlackList.Add("MySql.Data.dll");
                dllBlackList.Add("NDesk.Options.dll");
                dllBlackList.Add("Newtonsoft.Json.dll");
                dllBlackList.Add("Newtonsoft.Json.Net20.dll");
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
                dllBlackList.Add("OpenMetaverse.Rendering.Meshmerizer.dll");
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
                        Type typeInterface = pluginType.GetInterface(type, true);

                        if (typeInterface != null)
                        {
                            return (T)Activator.CreateInstance(pluginType);
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
