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
        private static Dictionary<string, object> Plugins = new Dictionary<string, object>();
        public static T RequestPlugin<T>()
        {
            if (Plugins.ContainsKey(typeof(T).Name))
            {
                object Plugin;
                Plugins.TryGetValue(typeof(T).Name, out Plugin);
                return (T)Plugin;
            }
            return default(T);
        }

        public static void RegisterPlugin(string Name, object Plugin)
        {
            if (!Plugins.ContainsKey(Name))
                Plugins.Add(Name, Plugin);
        }
    }
}
