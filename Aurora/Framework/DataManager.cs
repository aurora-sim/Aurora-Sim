using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using Nini.Config;
using Aurora.Framework;
using OpenMetaverse;
using Settings = NHibernate.Cfg.Settings;

namespace Aurora.DataManager
{
    /// <summary>
    /// Plugin manager that deals with retrieving IDataPlugins
    /// </summary>
    public static class DataManager
    {
        private static Dictionary<string, IAuroraDataPlugin> Plugins = new Dictionary<string, IAuroraDataPlugin>();
        /// <summary>
        /// Request a data plugin from the registry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T RequestPlugin<T>() where T : IAuroraDataPlugin
        {
            if (Plugins.ContainsKey(typeof(T).Name))
            {
                IAuroraDataPlugin Plugin;
                Plugins.TryGetValue(typeof(T).Name, out Plugin);
                return (T)Plugin;
            }
            //Return null if we can't find it
            return default(T);
        }

        /// <summary>
        /// Register a new plugin to the registry
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Plugin"></param>
        public static void RegisterPlugin(string Name, IAuroraDataPlugin Plugin)
        {
            if (!Plugins.ContainsKey(Name))
                Plugins.Add(Name, Plugin);
        }
    }
}
