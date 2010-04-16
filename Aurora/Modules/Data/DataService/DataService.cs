using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Aurora.DataManager.Migration;
using log4net;
using Nini.Config;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Aurora.DataManager;
using Aurora.DataManager.MySQL;
using Aurora.DataManager.SQLite;
using OpenSim.Framework;

namespace Aurora.Modules
{
    public class LocalDataService: IDataService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        internal IConfig m_config;
        string PluginModule = "";
        string ConnectionString = "";

        public void Initialise(IScene scene, IConfigSource source)
        {
            m_config = source.Configs["AuroraData"];

            if (null == m_config)
            {
                m_log.Error("[AuroraData]: no data plugin found!");
                return;
            }
            
            PluginModule = m_config.GetString("PluginModule", "");
            ConnectionString = m_config.GetString("ConnectionString", "");
            if (PluginModule == "MySQL")
            {
                DataManager.DataManager.DataSessionProvider = new DataSessionProvider(DataManagerTechnology.MySql, ConnectionString);
                MySQLDataLoader GenericData = new MySQLDataLoader();
                GenericData.ConnectToDatabase(ConnectionString);

                var migrationManager = new MigrationManager(DataManager.DataManager.DataSessionProvider, GenericData);
                migrationManager.DetermineOperation();
                migrationManager.ExecuteOperation();

                MySQLProfile ProfileData = new MySQLProfile();
                ProfileData.ConnectToDatabase(ConnectionString);
                MySQLRegion RegionData = new MySQLRegion();
                RegionData.ConnectToDatabase(ConnectionString);
                Aurora.DataManager.DataManager.AddGenericPlugin(GenericData);
                Aurora.DataManager.DataManager.AddProfilePlugin((IProfileData)ProfileData);
                Aurora.DataManager.DataManager.AddRegionPlugin((IRegionData)RegionData);
            }
            else if (PluginModule == "SQLite")
            {
                DataManager.DataManager.DataSessionProvider = new DataSessionProvider(DataManagerTechnology.SQLite, ConnectionString);
                SQLiteLoader GenericData = new SQLiteLoader();
                GenericData.ConnectToDatabase(ConnectionString);

                var migrationManager = new MigrationManager(DataManager.DataManager.DataSessionProvider, GenericData);
                migrationManager.DetermineOperation();
                migrationManager.ExecuteOperation();

                SQLiteProfile ProfileData = new SQLiteProfile();
                ProfileData.ConnectToDatabase(ConnectionString);
                SQLiteRegion RegionData = new SQLiteRegion();
                RegionData.ConnectToDatabase(ConnectionString);
                SQLiteEstate EstateData = new SQLiteEstate();
                EstateData.ConnectToDatabase(ConnectionString);
                
                Aurora.DataManager.DataManager.AddGenericPlugin(GenericData);
                Aurora.DataManager.DataManager.AddProfilePlugin((IProfileData)ProfileData);
                Aurora.DataManager.DataManager.AddRegionPlugin((IRegionData)RegionData);
                Aurora.DataManager.DataManager.AddEstatePlugin((IEstateData)EstateData);
            }

            int i = 0;
            string[] RemoteConnectionStrings = m_config.GetString("RemoteConnectionStrings", "").Split(',');
            string[] RemoteConnectionPasswords = m_config.GetString("RemoteConnectionPasswords", "").Split(',');
            foreach (string remoteconnection in RemoteConnectionStrings)
            {
                RemoteDataConnector connector = new RemoteDataConnector(RemoteConnectionStrings[i], RemoteConnectionPasswords[i]);
                Aurora.DataManager.DataManager.AddGenericPlugin(connector);
                Aurora.DataManager.DataManager.AddProfilePlugin(connector);
                Aurora.DataManager.DataManager.AddRegionPlugin(connector);
            }
            Aurora.DataManager.DataManager.SetDefaultGenericDataPlugin(new GenericData());
            Aurora.DataManager.DataManager.SetDefaultEstatePlugin(new EstateData());
            Aurora.DataManager.DataManager.SetProfilePlugin(new ProfileData());
            Aurora.DataManager.DataManager.SetDefaultRegionPlugin(new RegionData());
        }

        public string Name
        {
            get { return "DataService"; }
        }
        public IGenericData GetGenericPlugin()
        {
            return Aurora.DataManager.DataManager.DefaultGenericPlugin;
        }
        public void SetGenericDataPlugin(IGenericData Plugin)
        {
            Aurora.DataManager.DataManager.DefaultGenericPlugin = Plugin;
        }
        public IEstateData GetEstatePlugin()
        {
            return Aurora.DataManager.DataManager.DefaultEstatePlugin;
        }
        public void SetEstatePlugin(IEstateData Plugin)
        {
            Aurora.DataManager.DataManager.DefaultEstatePlugin = Plugin;
        }
        public IProfileData GetProfilePlugin()
        {
            return Aurora.DataManager.DataManager.DefaultProfilePlugin;
        }
        public void SetProfilePlugin(IProfileData Plugin)
        {
            Aurora.DataManager.DataManager.DefaultProfilePlugin = Plugin;
        }
        public IRegionData GetRegionPlugin()
        {
            return Aurora.DataManager.DataManager.DefaultRegionPlugin;
        }
        public void SetRegionPlugin(IRegionData Plugin)
        {
            Aurora.DataManager.DataManager.DefaultRegionPlugin = Plugin;
        }
    }
}
