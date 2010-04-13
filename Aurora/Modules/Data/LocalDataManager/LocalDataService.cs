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

namespace Aurora.Modules
{
    public class LocalDataService: IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        internal IConfig m_config;
        string PluginModule = "";
        string ConnectionString = "";

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_config = source.Configs["AuroraData"];

            if (null == m_config)
            {
                m_log.Error("[AuroraData]: no data plugin found!");
                return;
            }
            string ServiceName = m_config.GetString("Service", "");
            if (ServiceName != Name)
                return;

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
                Aurora.DataManager.DataManager.SetRemoteDataPlugin(GenericData);
                Aurora.DataManager.DataManager.SetProfilePlugin((IProfileData)ProfileData);
                Aurora.DataManager.DataManager.SetRegionPlugin((IRegionData)RegionData);
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
                Aurora.DataManager.DataManager.SetRemoteDataPlugin(GenericData);
                Aurora.DataManager.DataManager.SetProfilePlugin((IProfileData)ProfileData);
                Aurora.DataManager.DataManager.SetRegionPlugin((IRegionData)RegionData);
            }
            else
            {
                m_log.Error("[AuroraData]: Data Plugin not found!");
            }
        }

        public void PostInitialise() {}

        public void Close() {}

        public string Name
        {
            get { return "LocalDataService"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
    }
}
