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
        private static IRemoteGenericData plugin = null;
        public static IRemoteGenericData GetGenericPlugin()
        {
            return plugin;
        }
        public static void SetRemoteDataPlugin(IRemoteGenericData Plugin)
        {
            plugin = Plugin;
        }
        private static IProfileData profileplugin = null;
        public static IProfileData GetProfilePlugin()
        {
            return profileplugin;
        }
        public static void SetProfilePlugin(IProfileData Plugin)
        {
            profileplugin = Plugin;
        }
        private static IRegionData regionplugin = null;

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
