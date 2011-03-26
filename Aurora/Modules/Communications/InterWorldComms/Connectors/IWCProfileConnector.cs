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
    public class IWCProfileConnector : IProfileConnector
    {
        protected LocalProfileConnector m_localService;
        protected RemoteProfileConnector m_remoteService;

        private IRegistryCore m_registry;

        public void Initialize (IGenericData unneeded, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString ("ProfileConnector", "LocalConnector") == "IWCConnector")
            {
                m_localService = new LocalProfileConnector ();
                m_localService.Initialize (unneeded, source, simBase, defaultConnectionString);
                m_remoteService = new RemoteProfileConnector ();
                m_remoteService.Initialize (unneeded, source, simBase, defaultConnectionString);
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin (Name, this);
            }
        }

        public string Name
        {
            get { return "IProfileConnector"; }
        }

        public void Dispose ()
        {
        }

        #region IProfileConnector Members

        public IUserProfileInfo GetUserProfile (UUID agentID)
        {
            IUserProfileInfo profile = m_localService.GetUserProfile (agentID);
            if (profile == null)
                profile = m_remoteService.GetUserProfile (agentID);
            return profile;
        }

        public bool UpdateUserProfile (IUserProfileInfo Profile)
        {
            bool success = m_localService.UpdateUserProfile (Profile);
            if (!success)
                success = m_remoteService.UpdateUserProfile (Profile);
            return success;
        }

        public void CreateNewProfile (UUID UUID)
        {
            m_localService.CreateNewProfile (UUID);
        }

        public bool AddClassified (Classified classified)
        {
            bool success = m_localService.AddClassified (classified);
            if (!success)
                success = m_remoteService.AddClassified (classified);
            return success;
        }

        public Classified GetClassified (UUID queryClassifiedID)
        {
            Classified Classified = m_localService.GetClassified (queryClassifiedID);
            if (Classified == null)
                Classified = m_remoteService.GetClassified (queryClassifiedID);
            return Classified;
        }

        public List<Classified> GetClassifieds (UUID ownerID)
        {
            List<Classified> Classifieds = m_localService.GetClassifieds (ownerID);
            if (Classifieds == null)
                Classifieds = m_remoteService.GetClassifieds (ownerID);
            return Classifieds;
        }

        public void RemoveClassified (UUID queryClassifiedID)
        {
            m_localService.RemoveClassified (queryClassifiedID);
            m_remoteService.RemoveClassified (queryClassifiedID);
        }

        public bool AddPick (ProfilePickInfo pick)
        {
            bool success = m_localService.AddPick (pick);
            if (!success)
                success = m_remoteService.AddPick (pick);
            return success;
        }

        public ProfilePickInfo GetPick (UUID queryPickID)
        {
            ProfilePickInfo pick = m_localService.GetPick (queryPickID);
            if (pick == null)
                pick = m_remoteService.GetPick (queryPickID);
            return pick;
        }

        public List<ProfilePickInfo> GetPicks (UUID ownerID)
        {
            List<ProfilePickInfo> picks = m_localService.GetPicks (ownerID);
            if (picks == null)
                picks = m_remoteService.GetPicks (ownerID);
            return picks;
        }

        public void RemovePick (UUID queryPickID)
        {
            m_localService.RemovePick (queryPickID);
            m_remoteService.RemovePick (queryPickID);
        }

        #endregion
    }
}
