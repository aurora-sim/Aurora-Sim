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
    public static class DataManager
    {
        private static Dictionary<string, object> Plugins = new Dictionary<string, object>();
        public static T RequestPlugin<T>(string Type)
        {
            if (Plugins.ContainsKey(Type))
            {
                object Plugin;
                Plugins.TryGetValue(Type, out Plugin);
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
