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

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            GD = GenericData;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase(defaultConnectionString, "Asset", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            DataManager.DataManager.RegisterPlugin(Name+"Local", this);

            if (source.Configs["AuroraConnectors"].GetString("AssetConnector", "LocalConnector") == "LocalConnector")
            {
                DataManager.DataManager.RegisterPlugin(Name, this);
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
            List<string> Test = GD.Query (new string[] { "Token", "KeySetting" }, new string[] { token, key }, "lslgenericdata", "*");
            if (Test.Count == 0)
            {
                GD.Insert("lslgenericdata", new string[] { token, key, value });
            }
            else
            {
                GD.Update ("lslgenericdata", new object[] { value }, new string[] { "ValueSetting" }, new string[] { "KeySetting" }, new object[] { key });
            }
        }

        public List<string> FindLSLData(string token, string key)
        {
            return GD.Query (new string[] { "Token", "KeySetting" }, new string[] { token, key }, "lslgenericdata", "*");
        }
    }
}
