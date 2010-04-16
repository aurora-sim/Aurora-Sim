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
        public static IGenericData RetriverGenericPlugin = null;
        public static IList<IGenericData> AllGenericPlugins = (System.Collections.Generic.IList<IGenericData>)new List<IGenericData>();
        
        public static IGenericData GetDefaultGenericPlugin()
        {
            if (RetriverGenericPlugin != null)
                return RetriverGenericPlugin;
            return DefaultGenericPlugin;
        }
        public static void SetGenericDataRetriver(IGenericData Plugin)
        {
            RetriverGenericPlugin = Plugin;
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
        public static IList<IEstateData> AllEstatePlugins = (System.Collections.Generic.IList<IEstateData>)new List<IEstateData>();
        public static IEstateData RetriverEstatePlugin = null;
        public static IEstateData GetDefaultEstatePlugin()
        {
            if (RetriverEstatePlugin != null)
                return RetriverEstatePlugin;
            return DefaultEstatePlugin;
        }
        public static void SetEstateDataRetriver(IEstateData Plugin)
        {
            RetriverEstatePlugin = Plugin;
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
        public static IList<IProfileData> AllProfilePlugins = (System.Collections.Generic.IList<IProfileData>)new List<IProfileData>();
        public static IProfileData RetriverProfilePlugin = null;
        public static IProfileData GetDefaultProfilePlugin()
        {
            if (RetriverProfilePlugin != null)
                return RetriverProfilePlugin;
            return DefaultProfilePlugin;
        }
        public static void SetProfileDataRetriver(IProfileData Plugin)
        {
            RetriverProfilePlugin = Plugin;
        }
        public static void SetDefaultProfilePlugin(IProfileData Plugin)
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
        public static IList<IRegionData> AllRegionPlugins = (System.Collections.Generic.IList<IRegionData>)new List<IRegionData>();
        public static IRegionData RetriverRegionPlugin = null;
        public static IRegionData GetDefaultRegionPlugin()
        {
            if (RetriverRegionPlugin != null)
                return RetriverRegionPlugin;
            return DefaultRegionPlugin;
        }
        public static void SetRegionDataRetriver(IRegionData Plugin)
        {
            RetriverRegionPlugin = Plugin;
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
