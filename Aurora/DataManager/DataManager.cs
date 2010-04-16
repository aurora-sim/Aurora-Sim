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
    	#region IGenericData 
    	
        public static IGenericData DefaultGenericPlugin = null;
        public static IList<IGenericData> AllGenericPlugins = (C5.IList<IGenericData>)new List<IGenericData>();
        public static IGenericData GetDefaultGenericPlugin()
        {
            return DefaultGenericPlugin;
        }
        public static void SetDefaultGenericDataPlugin(IGenericData Plugin)
        {
            DefaultGenericPlugin = Plugin;
        }
        public static void AddGenericPlugin(IGenericData plugin)
        {
            AllGenericPlugins.Add(plugin);
        }
        
        #endregion
        
        #region IEstateData
        
        public static IEstateData DefaultEstatePlugin = null;
        public static IList<IEstateData> AllEstatePlugins = (C5.IList<IEstateData>)new List<IEstateData>();
        public static IEstateData GetDefaultEstatePlugin()
        {
            return DefaultEstatePlugin;
        }
        public static void SetDefaultEstatePlugin(IEstateData Plugin)
        {
            DefaultEstatePlugin = Plugin;
        }
        public static void AddEstatePlugin(IEstateData plugin)
        {
            AllEstatePlugins.Add(plugin);
        }
        
        #endregion
        
        #region IProfileData 
        
        public static IProfileData DefaultProfilePlugin = null;
        public static IList<IProfileData> AllProfilePlugins = (C5.IList<IProfileData>)new List<IProfileData>();
        public static IProfileData GetDefaultProfilePlugin()
        {
            return DefaultProfilePlugin;
        }
        public static void SetProfilePlugin(IProfileData Plugin)
        {
            DefaultProfilePlugin = Plugin;
        }
        public static void AddProfilePlugin(IProfileData plugin)
        {
            AllProfilePlugins.Add(plugin);
        }
        
        #endregion
        
        #region IRegionData
        
        public static IRegionData DefaultRegionPlugin = null;
        public static IList<IRegionData> AllRegionPlugins = (C5.IList<IRegionData>)new List<IRegionData>();
        public static IRegionData GetDefaultRegionPlugin()
        {
            return DefaultRegionPlugin;
        }
        public static void SetDefaultRegionPlugin(IRegionData Plugin)
        {
            DefaultRegionPlugin = Plugin;
        }
        public static void AddRegionPlugin(IRegionData plugin)
        {
            AllRegionPlugins.Add(plugin);
        }
        
        #endregion

        public static DataSessionProvider DataSessionProvider;
    }
}
