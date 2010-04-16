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
                Aurora.DataManager.DataManager.SetGenericDataPlugin(GenericData);
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
                SQLiteEstate EstateData = new SQLiteEstate();
                EstateData.ConnectToDatabase(ConnectionString);
                
                Aurora.DataManager.DataManager.SetGenericDataPlugin(GenericData);
                Aurora.DataManager.DataManager.SetProfilePlugin((IProfileData)ProfileData);
                Aurora.DataManager.DataManager.SetRegionPlugin((IRegionData)RegionData);
                Aurora.DataManager.DataManager.SetEstatePlugin((IEstateData)EstateData);
            }
            else
            {
                m_log.Error("[AuroraData]: Data Plugin not found!");
            }
        }

        public string Name
        {
            get { return "LocalDataService"; }
        }
        public IGenericData GetGenericPlugin()
        {
            return Aurora.DataManager.DataManager.plugin;
        }
        public void SetGenericDataPlugin(IGenericData Plugin)
        {
            Aurora.DataManager.DataManager.plugin = Plugin;
        }
        public IEstateData GetEstatePlugin()
        {
            return Aurora.DataManager.DataManager.estateplugin;
        }
        public void SetEstatePlugin(IEstateData Plugin)
        {
            Aurora.DataManager.DataManager.estateplugin = Plugin;
        }
        public IProfileData GetProfilePlugin()
        {
            return Aurora.DataManager.DataManager.profileplugin;
        }
        public void SetProfilePlugin(IProfileData Plugin)
        {
            Aurora.DataManager.DataManager.profileplugin = Plugin;
        }
        public IRegionData GetRegionPlugin()
        {
            return Aurora.DataManager.DataManager.regionplugin;
        }
        public void SetRegionPlugin(IRegionData Plugin)
        {
            Aurora.DataManager.DataManager.regionplugin = Plugin;
        }
    }
}
