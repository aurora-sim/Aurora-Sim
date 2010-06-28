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

            Aurora.Framework.AuroraModuleLoader.LoadPlugins<IAuroraDataPlugin>("/Aurora/DataPlugin", new AuroraDataPluginInitialiser(DataConnector, source));
            IDirectoryServiceConnector DSC = Aurora.DataManager.DataManager.RequestPlugin<IDirectoryServiceConnector>("IDirectoryServiceConnector");
        }
    }

    public class AuroraDataPluginInitialiser : PluginInitialiserBase
    {
        IGenericData GD;
        IConfigSource m_source;

        public AuroraDataPluginInitialiser(IGenericData GenericData, IConfigSource source)
        {
            GD = GenericData;
            m_source = source;
        }
        public override void Initialise(IPlugin plugin)
        {
            IAuroraDataPlugin dataplugin = plugin as IAuroraDataPlugin;
            dataplugin.Initialise(GD, m_source);
        }
    }
}
