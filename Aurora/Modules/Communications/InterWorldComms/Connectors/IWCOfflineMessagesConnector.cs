using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Nini.Config;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Aurora.Services.DataService;
using OpenSim.Framework;
using OpenSim.Services.AvatarService;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules 
{
    public class IWCOfflineMessagesConnector : IOfflineMessagesConnector
    {
        protected LocalOfflineMessagesConnector m_localService;
        protected RemoteOfflineMessagesConnector m_remoteService;

        private IRegistryCore m_registry;

        public void Initialize (IGenericData unneeded, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString ("OfflineMessagesConnector", "LocalConnector") == "IWCConnector")
            {
                m_localService = new LocalOfflineMessagesConnector ();
                m_localService.Initialize (unneeded, source, simBase, defaultConnectionString);
                m_remoteService = new RemoteOfflineMessagesConnector ();
                m_remoteService.Initialize (unneeded, source, simBase, defaultConnectionString);
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin (Name, this);
            }
        }

        public string Name
        {
            get { return "IOfflineMessagesConnector"; }
        }

        public void Dispose ()
        {
        }

        #region IOfflineMessagesConnector Members

        public GridInstantMessage[] GetOfflineMessages (UUID agentID)
        {
            List<GridInstantMessage> messages = new List<GridInstantMessage> (m_localService.GetOfflineMessages (agentID));
            messages.AddRange (m_remoteService.GetOfflineMessages (agentID));
            return messages.ToArray ();
        }

        public void AddOfflineMessage (GridInstantMessage message)
        {
            m_localService.AddOfflineMessage (message);
        }

        #endregion
    }
}
