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
        private string StorageProvider = "";
        private string ConnectionString = "";

        public void Initialise(IConfigSource source)
        {
            //
            // Try reading the [DatabaseService] section, if it exists
            //
            IConfig dbConfig = source.Configs["DatabaseService"];
            if (dbConfig != null)
            {
                StorageProvider = dbConfig.GetString("StorageProvider", String.Empty);
                ConnectionString = dbConfig.GetString("ConnectionString", String.Empty);
            }

            //
            // [AuroraData] section overrides [DatabaseService], if it exists
            //
            IConfig m_config = source.Configs["AuroraData"];
            if (m_config != null)
            {
                StorageProvider = m_config.GetString("StorageProvider", StorageProvider);
                ConnectionString = m_config.GetString("ConnectionString", ConnectionString);
            }

            IGenericData DataConnector = null;
            if (StorageProvider == "MySQL" || StorageProvider == "OpenSim.Data.MySQL.dll") //Allow for fallback when AuroraData isn't set
            {
                DataManager.DataSessionProviderConnector.DataSessionProvider = new DataSessionProvider(DataManagerTechnology.MySql, ConnectionString);
                MySQLDataLoader GenericData = new MySQLDataLoader();

                DataConnector = GenericData;
            }
            else if (StorageProvider == "MSSQL2008")
            {
                DataManager.DataSessionProviderConnector.DataSessionProvider = new DataSessionProvider(DataManagerTechnology.MSSQL2008, ConnectionString);
                MSSQLDataLoader GenericData = new MSSQLDataLoader();

                DataConnector = GenericData;
            }
            else if (StorageProvider == "MSSQL7")
            {
                DataManager.DataSessionProviderConnector.DataSessionProvider = new DataSessionProvider(DataManagerTechnology.MSSQL7, ConnectionString);
                MSSQLDataLoader GenericData = new MSSQLDataLoader();

                DataConnector = GenericData;
            }
            else if (StorageProvider == "SQLite" || StorageProvider == "OpenSim.Data.SQLite.dll") //Allow for fallback when AuroraData isn't set
            {
                DataManager.DataSessionProviderConnector.DataSessionProvider = new DataSessionProvider(DataManagerTechnology.SQLite, ConnectionString);
                SQLiteLoader GenericData = new SQLiteLoader();

                DataConnector = GenericData;
            }

            List<IAuroraDataPlugin> Plugins = AuroraModuleLoader.PickupModules<IAuroraDataPlugin>();
            foreach (IAuroraDataPlugin plugin in Plugins)
            {
                plugin.Initialize(DataConnector, source, ConnectionString);
            }
        }
    }

    public class AuroraDataPluginInitialiser : PluginInitialiserBase
    {
        IGenericData GD;
        IConfigSource m_source;
        string m_defaultConnectionString = "";

        public AuroraDataPluginInitialiser(IGenericData GenericData, IConfigSource source, string DefaultConnectionString)
        {
            GD = GenericData;
            m_source = source;
            m_defaultConnectionString = DefaultConnectionString;
        }
        public override void Initialise(IPlugin plugin)
        {
            IAuroraDataPlugin dataplugin = plugin as IAuroraDataPlugin;
            IGenericData GenericData = GD.Copy();
            dataplugin.Initialize(GenericData, m_source, m_defaultConnectionString);
        }
    }
}
