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
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class LocalOfflineMessagesConnector : IOfflineMessagesConnector
	{
        private IGenericData GD = null;
        private int m_maxOfflineMessages = 20;

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            GD = GenericData;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase(defaultConnectionString, "Generics", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            DataManager.DataManager.RegisterPlugin(Name+"Local", this);

            m_maxOfflineMessages = source.Configs["AuroraConnectors"].GetInt ("MaxOfflineMessages", m_maxOfflineMessages);
            if (source.Configs["AuroraConnectors"].GetString("OfflineMessagesConnector", "LocalConnector") == "LocalConnector")
            {
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IOfflineMessagesConnector"; }
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Gets all offline messages for the user in GridInstantMessage format.
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns></returns>
        public GridInstantMessage[] GetOfflineMessages(UUID agentID)
		{
            //Get all the messages
            List<GridInstantMessage> Messages = GenericUtils.GetGenerics<GridInstantMessage>(agentID, "OfflineMessages", GD, new GridInstantMessage());
            //Clear them out now that we have them
            GenericUtils.RemoveGeneric(agentID, "OfflineMessages", GD);
            return Messages.ToArray();
		}

        /// <summary>
        /// Adds a new offline message for the user.
        /// </summary>
        /// <param name="message"></param>
        public bool AddOfflineMessage(GridInstantMessage message)
		{
            if(GenericUtils.GetGenericCount(message.toAgentID, "OfflineMessages", GD) < m_maxOfflineMessages)
            {
                GenericUtils.AddGeneric(message.toAgentID, "OfflineMessages", UUID.Random().ToString(), message.ToOSD(), GD);
                return true;
            }
            return false;
		}
	}
}
