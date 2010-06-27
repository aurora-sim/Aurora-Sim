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
            IGenericData DataConnector = null;
            if (PluginModule == "MySQL")
            {
                DataManager.DataSessionProviderConnector.DataSessionProvider = new DataSessionProvider(DataManagerTechnology.MySql, ConnectionString);
                MySQLDataLoader GenericData = new MySQLDataLoader();
                GenericData.ConnectToDatabase(ConnectionString);
                ScriptSaveDataConnector = GenericData;

                var migrationManager = new MigrationManager(DataManager.DataSessionProviderConnector.DataSessionProvider, GenericData);
                migrationManager.DetermineOperation();
                migrationManager.ExecuteOperation();

                DataConnector = GenericData;
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

                DataConnector = GenericData;
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

                DataConnector = GenericData;
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

                DataConnector = GenericData;
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
                DataManager.DataManager.RegisterPlugin("IAbuseReportsConnector", new LocalAbuseReportsConnector(DataConnector));
            if (AssetConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("IAssetConnector", new LocalAssetConnector(DataConnector));
            if (AvatarArchiverConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("IAvatarArchiverConnector", new LocalAvatarArchiverConnector(DataConnector));
            if (CurrencyConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("ICurrencyConnector", new LocalCurrencyConnector(DataConnector));
            if (SimMapDataConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("ISimMapDataConnector", new LocalSimMapConnector(DataConnector));
            if (ScriptDataConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("IScriptDataConnector", new LocalScriptDataConnector(ScriptSaveDataConnector));
            if (RegionInfoConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("IRegionInfoConnector", new LocalRegionInfoConnector(DataConnector));
            if (ParcelConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("IParcelServiceConnector", new LocalParcelServiceConnector(DataConnector));
            //End always local connectors.

            if (AgentConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("IAgentConnector", new LocalAgentConnector(DataConnector));
            if (RegionConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("IRegionConnector", new LocalRegionConnector(DataConnector));
            if (ProfileConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("IProfileConnector", new LocalProfileConnector(DataConnector));
            if (EstateConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("IEstateConnector", new LocalEstateConnector(DataConnector));
            if (OfflineMessagesConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("IOfflineMessagesConnector", new LocalOfflineMessagesConnector(DataConnector));
            if (MuteListConnector == "LocalConnector")
                DataManager.DataManager.RegisterPlugin("IMuteListConnector", new LocalMuteListConnector(DataConnector));

            //Start remote connectors.

            //Connectors that still need a remote connector
            if (DirectoryServiceConnector == "RemoteConnector")
                DataManager.DataManager.RegisterPlugin("IDirectoryServiceConnector", new LocalDirectoryServiceConnector(DataConnector));
            //End connectors that still need a remote connector

            if (EstateConnector == "RemoteConnector")
                DataManager.DataManager.RegisterPlugin("IEstateConnector", new RemoteEstateConnector(RemoteConnectionString));
            if (MuteListConnector == "RemoteConnector")
                DataManager.DataManager.RegisterPlugin("IMuteListConnector", new RemoteMuteListConnector(RemoteConnectionString));
            if (AgentConnector == "RemoteConnector")
                DataManager.DataManager.RegisterPlugin("IAgentConnector", new RemoteAgentConnector(RemoteConnectionString));
            if (RegionConnector == "RemoteConnector")
                DataManager.DataManager.RegisterPlugin("IRegionConnector", new RemoteRegionConnector(RemoteConnectionString));
            if (ProfileConnector == "RemoteConnector")
                DataManager.DataManager.RegisterPlugin("IProfileConnector", new RemoteProfileConnector(RemoteConnectionString));
            if (OfflineMessagesConnector == "RemoteConnector")
                DataManager.DataManager.RegisterPlugin("IOfflineMessagesConnector", new RemoteOfflineMessagesConnector(RemoteConnectionString));
        }
    }
}
