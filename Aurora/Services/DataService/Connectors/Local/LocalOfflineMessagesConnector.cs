using System;
using System.Collections.Generic;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;
using OpenMetaverse;
using Nini.Config;
using OpenSim.Framework;
using System.Xml;
using System.Xml.Serialization;

namespace Aurora.Services.DataService
{
    public class LocalOfflineMessagesConnector : IOfflineMessagesConnector, IAuroraDataPlugin
	{
        private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, IConfigSource source, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("OfflineMessagesConnector", "LocalConnector") == "LocalConnector")
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
                string m_ServerURI = source.Configs["AuroraData"].GetString("RemoteServerURI", "");
                if (m_ServerURI == "") //Blank, not set up
                {
                    OpenSim.Framework.Console.MainConsole.Instance.Output("[AuroraDataService]: Falling back on local connector for " + "OfflineMessagesConnector", "None");
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
            get { return "IOfflineMessagesConnector"; }
        }

        public void Dispose()
        {
        }

        public GridInstantMessage[] GetOfflineMessages(UUID agentID)
		{
            List<GridInstantMessage> Messages = GenericUtils.GetGenerics<GridInstantMessage>(agentID, "OfflineMessages", GD, new GridInstantMessage());
            GenericUtils.RemoveGeneric(agentID, "OfflineMessages", GD);
            return Messages.ToArray();
		}

		public void AddOfflineMessage(GridInstantMessage message)
		{
            GenericUtils.AddGeneric(new UUID(message.toAgentID), "OfflineMessages", UUID.Random().ToString(), message.ToOSD(), GD);
		}
	}
}
