using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Aurora.Framework
{
    public static class AuroraModuleLoader
    {
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
    }
}
