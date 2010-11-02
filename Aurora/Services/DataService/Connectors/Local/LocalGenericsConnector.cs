using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using Nini.Config;
using OpenMetaverse.StructuredData;

namespace Aurora.Services.DataService
{
    public class LocalGenericsConnector : IGenericsConnector, IAuroraDataPlugin
	{
		private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, IConfigSource source, string defaultConnectionString)
        {
            if(source.Configs["AuroraConnectors"].GetString("GenericsConnector", "LocalConnector") == "LocalConnector")
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
            get { return "IGenericsConnector"; }
        }

        public void Dispose()
        {
        }

        public T GetGeneric<T>(UUID OwnerID, string Type, string Key, T data) where T : IDataTransferable
        {
            return GenericUtils.GetGeneric<T>(OwnerID, Type, Key, GD, data);
        }

        public List<T> GetGenerics<T>(UUID OwnerID, string Type, T data) where T : IDataTransferable
        {
            return GenericUtils.GetGenerics<T>(OwnerID, Type, GD, data);
        }

        public void AddGeneric(UUID AgentID, string Type, string Key, OSDMap Value)
        {
            GenericUtils.AddGeneric(AgentID, Type, Key, Value, GD);
        }

        public void RemoveGeneric(UUID AgentID, string Type, string Key)
        {
            GenericUtils.RemoveGeneric(AgentID, Type, Key, GD);
        }

        public void RemoveGeneric(UUID AgentID, string Type)
        {
            GenericUtils.RemoveGeneric(AgentID, Type, GD);
        }
    }
}
