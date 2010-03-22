using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using C5;
using log4net;
using Nini.Config;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Aurora.DataManager;
using Aurora.DataManager.MySQL;
using Aurora.DataManager.SQLite;

namespace Aurora.Modules.DataPlugins
{
    public class RemoteDataService : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        internal IConfig m_config;
        string URL = "";
        string Password = "";

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

            URL = m_config.GetString("URL", "");
            Password = m_config.GetString("Password", "");

            RemoteDataConnector connector = new RemoteDataConnector(URL, Password);
            Aurora.DataManager.DataManager.SetGenericPlugin(connector);
            Aurora.DataManager.DataManager.SetProfilePlugin(connector);
            Aurora.DataManager.DataManager.SetRegionPlugin(connector);
        }

        public void PostInitialise(){ }

        public void Close() { }

        public string Name
        {
            get { return "RemoteDataService"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
    }
    
    public class RemoteDataConnector : IGenericData, IRegionData, IProfileData
    {
        private string URL = "";
        private string Password = "";
        public RemoteDataConnector(string url, string password)
        {
            URL = url;
            Password = password;
        }

        #region IData Members

        public string Identifier
        {
            get { return "RemoteDataConnector"; }
        }

        public void ConnectToDatabase(string connectionString)
        {
        }

        public List<string> Query(string keyRow, string keyValue, string table, string wantedValue)
        {
            throw new NotImplementedException();
        }

        public void Insert(string table, string[] values)
        {
            throw new NotImplementedException();
        }

        public void Delete(string table, string[] keys, string[] values)
        {
            throw new NotImplementedException();
        }

        public void Insert(string table, string[] values, string updateKey, string updateValue)
        {
            throw new NotImplementedException();
        }

        public void Update(string table, string[] setValues, string[] setRows, string[] keyRows, string[] keyValues)
        {
            throw new NotImplementedException();
        }

        public void CloseDatabase()
        {
            throw new NotImplementedException();
        }

        public bool TableExists(string table)
        {
            throw new NotImplementedException();
        }

        public void CreateTable(string table, ColumnDefinition[] columns)
        {
            throw new NotImplementedException();
        }

        public void CreateTable(string table, List<Rec<string, ColumnTypes>> columns)
        {
            throw new NotImplementedException();
        }

        public Version GetAuroraVersion()
        {
            throw new NotImplementedException();
        }

        public void WriteAuroraVersion(Version version)
        {
            throw new NotImplementedException();
        }

        public void CopyTableToTable(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions)
        {
            throw new NotImplementedException();
        }

        public bool VerifyTableExists(string tableName, ColumnDefinition[] columnDefinitions)
        {
            throw new NotImplementedException();
        }

        public void EnsureTableExists(string tableName, ColumnDefinition[] columnDefinitions)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> GetRegionHidden()
        {
            throw new NotImplementedException();
        }

        public string AbuseReports()
        {
            throw new NotImplementedException();
        }

        public ObjectMediaURLInfo[] getObjectMediaInfo(string objectID)
        {
            throw new NotImplementedException();
        }

        public List<string> ReadClassifiedInfoRow(string classifiedID)
        {
            throw new NotImplementedException();
        }

        public Dictionary<OpenMetaverse.UUID, string> ReadClassifedRow(string creatoruuid)
        {
            throw new NotImplementedException();
        }

        public Dictionary<OpenMetaverse.UUID, string> ReadPickRow(string creator)
        {
            throw new NotImplementedException();
        }

        public List<string> ReadInterestsInfoRow(string agentID)
        {
            throw new NotImplementedException();
        }

        public List<string> ReadPickInfoRow(string creator, string pickID)
        {
            throw new NotImplementedException();
        }

        public void InvalidateProfileNotes(OpenMetaverse.UUID target)
        {
            throw new NotImplementedException();
        }

        public AuroraProfileData GetProfileNotes(OpenMetaverse.UUID agentID, OpenMetaverse.UUID target)
        {
            throw new NotImplementedException();
        }

        public List<string> Query(string query)
        {
            throw new NotImplementedException();
        }

        public AuroraProfileData GetProfileInfo(OpenMetaverse.UUID agentID)
        {
            throw new NotImplementedException();
        }

        public void UpdateUserProfile(AuroraProfileData Profile)
        {
            throw new NotImplementedException();
        }

        public AuroraProfileData CreateTemperaryAccount(string client, string first, string last)
        {
            throw new NotImplementedException();
        }

        #endregion

        private object AskForData(string Type, object[] parameters)
        {
            return null;
        }
    }
}
