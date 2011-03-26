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
        private string StorageProvider = "";
        private string ConnectionString = "";

        public void Initialise(IConfigSource source, IRegistryCore simBase)
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
                MySQLDataLoader GenericData = new MySQLDataLoader();

                DataConnector = GenericData;
            }
            else if (StorageProvider == "MSSQL2008")
            {
                MSSQLDataLoader GenericData = new MSSQLDataLoader();

                DataConnector = GenericData;
            }
            else if (StorageProvider == "MSSQL7")
            {
                MSSQLDataLoader GenericData = new MSSQLDataLoader();

                DataConnector = GenericData;
            }
            else if (StorageProvider == "SQLite" || StorageProvider == "OpenSim.Data.SQLite.dll") //Allow for fallback when AuroraData isn't set
            {
                SQLiteLoader GenericData = new SQLiteLoader();

                DataConnector = GenericData;
            }

            List<IAuroraDataPlugin> Plugins = AuroraModuleLoader.PickupModules<IAuroraDataPlugin>();
            foreach (IAuroraDataPlugin plugin in Plugins)
            {
                plugin.Initialize(DataConnector.Copy(), source, simBase, ConnectionString);
            }
        }
    }
}
