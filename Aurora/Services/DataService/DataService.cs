using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Aurora.DataManager.Migration;
using log4net;
using Nini.Config;
using Aurora.Framework;
using Aurora.DataManager;
using Aurora.DataManager.MySQL;
using Aurora.DataManager.SQLite;
using OpenSim.Framework;

namespace Aurora.Services.DataService
{
    public class LocalDataService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        internal IConfig m_config;
        string PluginModule = "";
        string ConnectionString = "";

        public void Initialise(IConfigSource source)
        {
            m_config = source.Configs["AuroraData"];
            if (null == m_config)
            {
                m_log.Error("[AuroraData]: no data plugin found!");
                return;
            }
            
            PluginModule = m_config.GetString("PluginModule", "");
            ConnectionString = m_config.GetString("ConnectionString", "");
            IGenericData ScriptSaveDataConnector = null;
            if (PluginModule == "MySQL")
            {
                DataManager.DataSessionProviderConnector.DataSessionProvider = new DataSessionProvider(DataManagerTechnology.MySql, ConnectionString);
                MySQLDataLoader GenericData = new MySQLDataLoader();
                GenericData.ConnectToDatabase(ConnectionString);
                ScriptSaveDataConnector = GenericData;

                var migrationManager = new MigrationManager(DataManager.DataSessionProviderConnector.DataSessionProvider, GenericData);
                migrationManager.DetermineOperation();
                migrationManager.ExecuteOperation();

                Aurora.DataManager.DataManager.SetDefaultGenericDataPlugin(GenericData);
            }
            else if (PluginModule == "SQLite")
            {
                DataManager.DataSessionProviderConnector.DataSessionProvider = new DataSessionProvider(DataManagerTechnology.SQLite, ConnectionString);
                SQLiteLoader GenericData = new SQLiteLoader();
                GenericData.ConnectToDatabase(ConnectionString);

                //SQLite needs a second database for ScriptSaves, otherwise it locks it up...
                string ScriptConnectionString = m_config.GetString("ScriptConnectionString", "");
                DataManager.DataSessionProviderConnector.StateSaveDataSessionProvider = new DataSessionProvider(DataManagerTechnology.SQLite, ScriptConnectionString);
                SQLiteStateSaver ScriptSaverData = new SQLiteStateSaver();
                ScriptSaverData.ConnectToDatabase(ScriptConnectionString);
                ScriptSaveDataConnector = ScriptSaverData;

                var migrationManager = new MigrationManager(DataManager.DataSessionProviderConnector.DataSessionProvider, GenericData);
                migrationManager.DetermineOperation();
                migrationManager.ExecuteOperation();

                var statesavemigrationManager = new MigrationManager(DataManager.DataSessionProviderConnector.StateSaveDataSessionProvider, ScriptSaverData);
                statesavemigrationManager.DetermineOperation();
                statesavemigrationManager.ExecuteOperation();

                Aurora.DataManager.DataManager.SetDefaultGenericDataPlugin(GenericData);
            }

            DataManager.DataManager.IScriptDataConnector = new LocalScriptDataConnector(ScriptSaveDataConnector);
            string Connector = m_config.GetString("Connector", "LocalConnector");
            if (Connector == "LocalConnector")
            {
                DataManager.DataManager.ISimMapConnector = new LocalGridConnector();
                DataManager.DataManager.IAgentConnector = new LocalAgentConnector();
                DataManager.DataManager.IRegionConnector = new LocalGridConnector();
                DataManager.DataManager.IProfileConnector = new LocalProfileConnector();
                DataManager.DataManager.IEstateConnector = new LocalEstateConnector();
                DataManager.DataManager.IAbuseReportsConnector = new LocalAbuseReportsConnector();
                DataManager.DataManager.IAssetConnector = new LocalAssetConnector();
                DataManager.DataManager.IOfflineMessagesConnector = new LocalOfflineMessagesConnector();
                DataManager.DataManager.IDirectoryServiceConnector = new LocalDirectoryServiceConnector();
                DataManager.DataManager.IEstateConnector = new LocalEstateConnector();
                DataManager.DataManager.IAvatarArchiverConnector = new LocalAvatarArchiverConnector();
                return;
            }
            DataManager.DataManager.ISimMapConnector = new LocalGridConnector();
            DataManager.DataManager.IAbuseReportsConnector = new LocalAbuseReportsConnector();
            DataManager.DataManager.IAssetConnector = new LocalAssetConnector();
            DataManager.DataManager.IOfflineMessagesConnector = new LocalOfflineMessagesConnector();
            DataManager.DataManager.IDirectoryServiceConnector = new LocalDirectoryServiceConnector();
            DataManager.DataManager.IEstateConnector = new LocalEstateConnector();
            DataManager.DataManager.IAvatarArchiverConnector = new LocalAvatarArchiverConnector();
            if (Connector != "RemoteConnector")
            {
                m_log.Error("[AuroraDataService]: No Connector found with that name!");
                return;
            }
            string RemoteConnectionString = m_config.GetString("RemoteServerURI", "");
            DataManager.DataManager.IAgentConnector = new RemoteAgentConnector(RemoteConnectionString);
            DataManager.DataManager.IRegionConnector = new RemoteGridConnector(RemoteConnectionString);
            DataManager.DataManager.IProfileConnector = new RemoteProfileConnector(RemoteConnectionString);
        }
    }
}
