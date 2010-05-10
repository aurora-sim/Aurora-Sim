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
    public class RemoteAgentConnector : IAgentConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public RemoteAgentConnector(string serverURI)
        {
            m_ServerURI = serverURI;
        }

        #region IAgentConnector Members

        public IAgentInfo GetAgent(UUID agentID)
        {
            throw new NotImplementedException();
        }

        public void UpdateAgent(IAgentInfo agent)
        {
            throw new NotImplementedException();
        }

        public void CreateNewAgent(UUID agentID)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
