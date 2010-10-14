using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;
using OpenMetaverse;
using Nini.Config;

namespace Aurora.Services.DataService
{
    public class LocalAssetConnector : IAssetConnector, IAuroraDataPlugin
	{
		private IGenericData GD = null;

        public void Initialise(IGenericData GenericData, IConfigSource source, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AssetConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString);

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
            List<string> Test = GD.Query(new string[] { "Token", "Key" }, new string[] { token, key }, "LSLGenericData", "*");
            if (Test.Count == 0)
            {
                GD.Insert("LSLGenericData", new string[] { token, key, value });
            }
            else
            {
                GD.Update("LSLGenericData", new string[] { "Value" }, new string[] { value }, new string[] { "key" }, new string[] { key });
            }
        }

        public List<string> FindLSLData(string token, string key)
        {
            return GD.Query(new string[] { "Token", "Key" }, new string[] { token, key }, "LSLGenericData", "*");
        }
    }
}
