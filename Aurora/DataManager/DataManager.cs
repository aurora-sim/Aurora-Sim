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
        
        #region FrontendConnectors

        public static IProfileConnector IProfileConnector;
        public static IGridConnector IGridConnector;
        public static IAgentConnector IAgentConnector;
        public static IScriptDataConnector IScriptDataConnector;
        public static IEstateConnector IEstateConnector;
        public static IOfflineMessagesConnector IOfflineMessagesConnector;
        public static IAbuseReportsConnector IAbuseReportsConnector;
        public static IDirectoryServiceConnector IDirectoryServiceConnector;
        public static IAssetConnector IAssetConnector;
        public static IAvatarArchiverConnector IAvatarArchiverConnector;

        #endregion

        public static DataSessionProvider DataSessionProvider;
        public static DataSessionProvider StateSaveDataSessionProvider;
    }
}
