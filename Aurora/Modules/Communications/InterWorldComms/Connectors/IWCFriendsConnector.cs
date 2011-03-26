using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Services.Connectors;
using OpenSim.Services;
using OpenSim.Services.Friends;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;

namespace Aurora.Modules 
{
    public class IWCFriendsConnector : IFriendsService, IService
    {
        protected FriendsService m_localService;
        protected FriendsServicesConnector m_remoteService;
        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public IFriendsService InnerService
        {
            get { return m_localService; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString ("FriendsHandler", "") != Name)
                return;

            m_localService = new FriendsService ();
            m_localService.Initialize(config, registry);
            m_remoteService = new FriendsServicesConnector ();
            m_remoteService.Initialize(config, registry);
            registry.RegisterModuleInterface<IFriendsService> (this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_localService != null)
                m_localService.Start(config, registry);
        }

        public void FinishedStartup()
        {
            if (m_localService != null)
                m_localService.FinishedStartup();
        }

        #endregion

        #region IFriendsService Members

        public FriendInfo[] GetFriends (UUID PrincipalID)
        {
            FriendInfo[] friends = m_localService.GetFriends (PrincipalID);
            if (friends == null || friends.Length == 0)
                friends = m_remoteService.GetFriends (PrincipalID);
            return friends;
        }

        public bool StoreFriend (UUID PrincipalID, string Friend, int flags)
        {
            bool success = m_localService.StoreFriend (PrincipalID, Friend, flags);
            if (!success)
                success = m_remoteService.StoreFriend (PrincipalID, Friend, flags);
            return success;
        }

        public bool Delete (UUID PrincipalID, string Friend)
        {
            bool success = m_localService.Delete (PrincipalID, Friend);
            if (!success)
                success = m_remoteService.Delete (PrincipalID, Friend);
            return success;
        }

        #endregion
    }
}
