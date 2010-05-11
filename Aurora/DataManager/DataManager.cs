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
        public static IGenericData GetDefaultGenericPlugin()
        {
            return DefaultGenericPlugin;
        }
        public static void SetDefaultGenericDataPlugin(IGenericData Plugin)
        {
            DefaultGenericPlugin = Plugin;
        }
        #endregion
        
        #region IEstateData
        
        public static IEstateData DefaultEstatePlugin = null;
        public static IEstateData GetDefaultEstatePlugin()
        {
            return DefaultEstatePlugin;
        }
        public static void SetDefaultEstatePlugin(IEstateData Plugin)
        {
            DefaultEstatePlugin = Plugin;
        }
        #endregion
        
        #region IProfileData 
        
        public static IProfileData DefaultProfilePlugin = null;
        public static IProfileData GetDefaultProfilePlugin()
        {
            return DefaultProfilePlugin;
        }
        public static void SetDefaultProfilePlugin(IProfileData Plugin)
        {
            DefaultProfilePlugin = Plugin;
        }
        
        #endregion
        
        #region IRegionData
        
        public static IRegionData DefaultRegionPlugin = null;
        public static IRegionData GetDefaultRegionPlugin()
        {
            return DefaultRegionPlugin;
        }
        public static void SetDefaultRegionPlugin(IRegionData Plugin)
        {
            DefaultRegionPlugin = Plugin;
        }
        #endregion

        #region IGroupData

        public static IGroupsServicesConnector DefaultGroupPlugin = null;
        public static IGroupsServicesConnector GetDefaultGroupPlugin()
        {
            return DefaultGroupPlugin;
        }
        public static void SetDefaultGroupDataPlugin(IGroupsServicesConnector Plugin)
        {
            DefaultGroupPlugin = Plugin;
        }
        #endregion

        #region FrontendConnectors

        public static IProfileConnector IProfileConnector;
        public static IGridConnector IGridConnector;
        public static IAgentConnector IAgentConnector;


        #endregion

        public static DataSessionProvider DataSessionProvider;
        public static DataSessionProvider StateSaveDataSessionProvider;
    }
}
