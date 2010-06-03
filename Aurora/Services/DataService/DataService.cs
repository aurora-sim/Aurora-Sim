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
using Aurora.DataManager.MSSQL;
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
            else if (PluginModule == "MSSQL2008")
            {
                DataManager.DataSessionProviderConnector.DataSessionProvider = new DataSessionProvider(DataManagerTechnology.MSSQL2008, ConnectionString);
                MSSQLDataLoader GenericData = new MSSQLDataLoader();
                GenericData.ConnectToDatabase(ConnectionString);
                ScriptSaveDataConnector = GenericData;

                var migrationManager = new MigrationManager(DataManager.DataSessionProviderConnector.DataSessionProvider, GenericData);
                migrationManager.DetermineOperation();
                migrationManager.ExecuteOperation();

                Aurora.DataManager.DataManager.SetDefaultGenericDataPlugin(GenericData);
            }
            else if (PluginModule == "MSSQL7")
            {
                DataManager.DataSessionProviderConnector.DataSessionProvider = new DataSessionProvider(DataManagerTechnology.MSSQL7, ConnectionString);
                MSSQLDataLoader GenericData = new MSSQLDataLoader();
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
            IConfig m_ConnectorConfig = source.Configs["AuroraConnectors"];

            string AbuseReportsConnector = m_ConnectorConfig.GetString("AbuseReportsConnector", "LocalConnector");
            string AssetConnector = m_ConnectorConfig.GetString("AssetConnector", "LocalConnector");
            string AvatarArchiverConnector = m_ConnectorConfig.GetString("AvatarArchiverConnector", "LocalConnector");
            string CurrencyConnector = m_ConnectorConfig.GetString("CurrencyConnector", "LocalConnector");
            string SimMapDataConnector = m_ConnectorConfig.GetString("SimMapDataConnector", "LocalConnector");
            string ScriptDataConnector = m_ConnectorConfig.GetString("ScriptDataConnector", "LocalConnector");
            string RegionInfoConnector = m_config.GetString("RegionInfoConnector", "LocalConnector");
            string AgentConnector = m_ConnectorConfig.GetString("AgentConnector", "LocalConnector");
            string RegionConnector = m_ConnectorConfig.GetString("RegionConnector", "LocalConnector");
            string ProfileConnector = m_ConnectorConfig.GetString("ProfileConnector", "LocalConnector");
            string EstateConnector = m_ConnectorConfig.GetString("EstateConnector", "LocalConnector");
            string OfflineMessagesConnector = m_ConnectorConfig.GetString("OfflineMessagesConnector", "LocalConnector");
            string DirectoryServiceConnector = m_ConnectorConfig.GetString("DirectoryServiceConnector", "LocalConnector");
            string MuteListConnector = m_ConnectorConfig.GetString("MuteListConnector", "LocalConnector");
            string ParcelConnector = m_ConnectorConfig.GetString("ParcelConnector", "LocalConnector");
            string RemoteConnectionString = m_config.GetString("RemoteServerURI", "");

            //Always local connectors.
            if (AbuseReportsConnector == "LocalConnector")
                DataManager.DataManager.IAbuseReportsConnector = new LocalAbuseReportsConnector();
            if (AssetConnector == "LocalConnector")
                DataManager.DataManager.IAssetConnector = new LocalAssetConnector();
            if (AvatarArchiverConnector == "LocalConnector")
                DataManager.DataManager.IAvatarArchiverConnector = new LocalAvatarArchiverConnector();
            if (CurrencyConnector == "LocalConnector")
                DataManager.DataManager.ICurrencyConnector = new LocalCurrencyConnector();
            if (SimMapDataConnector == "LocalConnector")
                DataManager.DataManager.ISimMapDataConnector = new LocalSimMapConnector();
            if (ScriptDataConnector == "LocalConnector")
                DataManager.DataManager.IScriptDataConnector = new LocalScriptDataConnector(ScriptSaveDataConnector);
            if (RegionInfoConnector == "LocalConnector")
                DataManager.DataManager.IRegionInfoConnector = new LocalRegionInfoConnector();
            if (ParcelConnector == "LocalConnector")
                DataManager.DataManager.IParcelServiceConnector = new LocalParcelServiceConnector();
            //End always local connectors.

            if (AgentConnector == "LocalConnector")
                DataManager.DataManager.IAgentConnector = new LocalAgentConnector();
            if (RegionConnector == "LocalConnector")
                DataManager.DataManager.IRegionConnector = new LocalRegionConnector();
            if (ProfileConnector == "LocalConnector")
                DataManager.DataManager.IProfileConnector = new LocalProfileConnector();
            if (EstateConnector == "LocalConnector")
                DataManager.DataManager.IEstateConnector = new LocalEstateConnector();
            if (OfflineMessagesConnector == "LocalConnector")
                DataManager.DataManager.IOfflineMessagesConnector = new LocalOfflineMessagesConnector();
            if (MuteListConnector == "LocalConnector")
                DataManager.DataManager.IMuteListConnector = new LocalMuteListConnector();

            //Start remote connectors.

            //Connectors that still need a remote connector
            if (DirectoryServiceConnector == "RemoteConnector")
                DataManager.DataManager.IDirectoryServiceConnector = new LocalDirectoryServiceConnector();
            //End connectors that still need a remote connector

            if (EstateConnector == "RemoteConnector")
                DataManager.DataManager.IEstateConnector = new RemoteEstateConnector(RemoteConnectionString);
            if (MuteListConnector == "RemoteConnector")
                DataManager.DataManager.IMuteListConnector = new RemoteMuteListConnector(RemoteConnectionString);
            if (AgentConnector == "RemoteConnector")
                DataManager.DataManager.IAgentConnector = new RemoteAgentConnector(RemoteConnectionString);
            if (RegionConnector == "RemoteConnector")
                DataManager.DataManager.IRegionConnector = new RemoteRegionConnector(RemoteConnectionString);
            if (ProfileConnector == "RemoteConnector")
                DataManager.DataManager.IProfileConnector = new RemoteProfileConnector(RemoteConnectionString);
            if (OfflineMessagesConnector == "RemoteConnector")
                DataManager.DataManager.IOfflineMessagesConnector = new RemoteOfflineMessagesConnector(RemoteConnectionString);
        }
    }
}
