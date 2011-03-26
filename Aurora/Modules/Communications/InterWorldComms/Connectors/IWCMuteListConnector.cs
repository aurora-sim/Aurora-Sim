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
    public class IWCMuteListConnector : IMuteListConnector
    {
        protected LocalMuteListConnector m_localService;
        protected RemoteMuteListConnector m_remoteService;

        private IRegistryCore m_registry;

        public void Initialize (IGenericData unneeded, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString ("MuteListConnector", "LocalConnector") == "IWCConnector")
            {
                m_localService = new LocalMuteListConnector ();
                m_localService.Initialize (unneeded, source, simBase, defaultConnectionString);
                m_remoteService = new RemoteMuteListConnector ();
                m_remoteService.Initialize (unneeded, source, simBase, defaultConnectionString);
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin (Name, this);
            }
        }

        public string Name
        {
            get { return "IMuteListConnector"; }
        }

        public void Dispose ()
        {
        }

        #region IMuteListConnector Members

        public MuteList[] GetMuteList (UUID AgentID)
        {
            List<MuteList> list = new List<MuteList>(m_localService.GetMuteList (AgentID));
            list.AddRange (m_remoteService.GetMuteList (AgentID));
            return list.ToArray ();
        }

        public void UpdateMute (MuteList mute, UUID AgentID)
        {
            m_localService.UpdateMute (mute, AgentID);
            m_remoteService.UpdateMute (mute, AgentID);
        }

        public void DeleteMute (UUID muteID, UUID AgentID)
        {
            m_localService.DeleteMute (muteID, AgentID);
            m_remoteService.DeleteMute (muteID, AgentID);
        }

        public bool IsMuted (UUID AgentID, UUID PossibleMuteID)
        {
            if (!m_localService.IsMuted (AgentID, PossibleMuteID))
                if (!m_remoteService.IsMuted (AgentID, PossibleMuteID))
                    return false;
            return true;
        }

        #endregion
    }
}
