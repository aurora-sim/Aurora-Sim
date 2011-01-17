using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;
using OpenMetaverse;
using Nini.Config;
using OpenSim.Framework;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class LocalAssetConnector : IAssetConnector
	{
		private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("AssetConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString);

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
            else
            {
                //Check to make sure that something else exists
                string m_ServerURI = simBase.ApplicationRegistry.RequestModuleInterface<IAutoConfigurationService>().FindValueOf("RemoteServerURI", "AuroraData");
                if (m_ServerURI == "") //Blank, not set up
                {
                    OpenSim.Framework.Console.MainConsole.Instance.Output("[AuroraDataService]: Falling back on local connector for " + "AssetConnector", "None");
                    GD = GenericData;

                    if (source.Configs[Name] != null)
                        defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                    GD.ConnectToDatabase(defaultConnectionString);

                    DataManager.DataManager.RegisterPlugin(Name, this);
                }
            }
        }

        public string Name
        {
            get { return "IAssetConnector"; }
        }

        public void Dispose()
        {
        }

		public void UpdateLSLData(string token, string key, string value)
        {
            List<string> Test = GD.Query(new string[] { "Token", "Key" }, new string[] { token, key }, "lslgenericdata", "*");
            if (Test.Count == 0)
            {
                GD.Insert("lslgenericdata", new string[] { token, key, value });
            }
            else
            {
                GD.Update("lslgenericdata", new string[] { "Value" }, new string[] { value }, new string[] { "key" }, new string[] { key });
            }
        }

        public List<string> FindLSLData(string token, string key)
        {
            return GD.Query(new string[] { "Token", "Key" }, new string[] { token, key }, "lslgenericdata", "*");
        }
    }
}
