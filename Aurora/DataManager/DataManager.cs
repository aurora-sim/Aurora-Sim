using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using C5;
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
        public static IGenericData plugin = null;
        public static IGenericData GetGenericPlugin()
        {
            return plugin;
        }
        public static void SetGenericDataPlugin(IGenericData Plugin)
        {
            plugin = Plugin;
        }
        public static IEstateData estateplugin = null;
        public static IEstateData GetEstatePlugin()
        {
            return estateplugin;
        }
        public static void SetEstatePlugin(IEstateData Plugin)
        {
            estateplugin = Plugin;
        }
        public static IProfileData profileplugin = null;
        public static IProfileData GetProfilePlugin()
        {
            return profileplugin;
        }
        public static void SetProfilePlugin(IProfileData Plugin)
        {
            profileplugin = Plugin;
        }
        public static IRegionData regionplugin = null;

        public static IRegionData GetRegionPlugin()
        {
            return regionplugin;
        }
        public static void SetRegionPlugin(IRegionData Plugin)
        {
            regionplugin = Plugin;
        }

        public static DataSessionProvider DataSessionProvider;
    }
}
