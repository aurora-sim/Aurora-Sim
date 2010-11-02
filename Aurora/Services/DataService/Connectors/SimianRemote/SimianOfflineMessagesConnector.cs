using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using log4net;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;

namespace Aurora.Services.DataService
{
    public class SimianOfflineMessagesConnector : IOfflineMessagesConnector, IAuroraDataPlugin
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public void Initialize(IGenericData unneeded, IConfigSource source, string DefaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("OfflineMessagesConnector", "LocalConnector") == "SimianConnector")
            {
                m_ServerURI = source.Configs["AuroraData"].GetString("RemoteServerURI", "");
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

        #region IOfflineMessagesConnector Members

        public GridInstantMessage[] GetOfflineMessages(UUID PrincipalID)
        {
            List<GridInstantMessage> Messages = new List<GridInstantMessage>();
            Dictionary<string, OSDMap> Maps = new Dictionary<string,OSDMap>();
            if(SimianUtils.GetGenericEntries(PrincipalID, "OfflineMessages", m_ServerURI, out Maps))
            {
                GridInstantMessage baseMessage = new GridInstantMessage();
                foreach(OSDMap map in Maps.Values)
                {
                    baseMessage.FromOSD(map);
                    Messages.Add(baseMessage);
                }
            }
            return Messages.ToArray();
        }

        public void AddOfflineMessage(GridInstantMessage message)
        {
            SimianUtils.AddGeneric(new UUID(message.toAgentID), "OfflineMessages", UUID.Random().ToString(), message.ToOSD(), m_ServerURI);
        }

        #endregion
    }
}
