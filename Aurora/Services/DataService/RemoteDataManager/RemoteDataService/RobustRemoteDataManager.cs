using System;
using Nini.Config;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;

namespace Aurora.Services.DataService
{
    public class RobustRemoteDataManager : ServiceConnector
    {
        private RemoteDataHandlers m_RemoteDataService;
        private string m_ConfigName = "RemoteDataService";

        public RobustRemoteDataManager(IConfigSource config, IHttpServer server, string configName) :
                base(config, server, configName)
        {
            if (configName != String.Empty)
                m_ConfigName = configName;
            IConfig serverConfig = config.Configs[m_ConfigName];
            if (serverConfig == null)
                throw new Exception(String.Format("No section '{0}' in config file", m_ConfigName));

            string ConnectionPassword = serverConfig.GetString("ConnectionPassword",
                    String.Empty);
            string ConnectionString = serverConfig.GetString("DatabaseConnectionString",
                    String.Empty);
            string DataManagerType = serverConfig.GetString("DataManager",
                    String.Empty);
            m_RemoteDataService = new RemoteDataHandlers(ConnectionPassword, DataManagerType, ConnectionString);

            //Add External Handlers here
        }
    }
    public class RemoteDataHandlers
    {
        string ConnectionPassword = "";
        string ConnectionString = "";
        string DataManagerType = "";

        public RemoteDataHandlers(string password, string type, string connectionString)
        {
            ConnectionPassword = password;
            DataManagerType = type;
            ConnectionString = connectionString;
        }
    }
}
