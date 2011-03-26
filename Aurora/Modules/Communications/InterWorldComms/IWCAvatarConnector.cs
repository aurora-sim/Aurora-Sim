using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Services.Connectors;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using OpenSim.Services.AvatarService;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules 
{
    public class IWCAvatarConnector : IAvatarService, IService
    {
        protected AvatarService m_localService;
        protected AvatarServicesConnector m_remoteService;

        #region IService Members

        public string Name
        {
            get { return GetType ().Name; }
        }

        public virtual IAvatarService InnerService
        {
            get { return m_localService; }
        }

        public void Initialize (IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString ("AvatarHandler", "") != Name)
                return;

            m_localService = new AvatarService ();
            m_localService.Initialize (config, registry);
            m_remoteService = new AvatarServicesConnector ();
            m_remoteService.Initialize (config, registry);
            registry.RegisterModuleInterface<IAvatarService> (this);
        }

        public void Start (IConfigSource config, IRegistryCore registry)
        {
            if (m_localService != null)
                m_localService.Start (config, registry);
        }

        public void FinishedStartup ()
        {
        }

        #endregion

        #region IAvatarService Members

        public AvatarAppearance GetAppearance (UUID userID)
        {
            AvatarAppearance app = m_localService.GetAppearance (userID);
            if (app == null)
                app = m_remoteService.GetAppearance (userID);
            return app;
        }

        public bool SetAppearance (UUID userID, AvatarAppearance appearance)
        {
            bool success = m_localService.SetAppearance (userID, appearance);
            if (!success)
                success = m_remoteService.SetAppearance (userID, appearance);
            return success;
        }

        public AvatarData GetAvatar (UUID userID)
        {
            AvatarData app = m_localService.GetAvatar (userID);
            if (app == null)
                app = m_remoteService.GetAvatar (userID);
            return app;
        }

        public bool SetAvatar (UUID userID, AvatarData avatar)
        {
            bool success = m_localService.SetAvatar (userID, avatar);
            if (!success)
                success = m_remoteService.SetAvatar (userID, avatar);
            return success;
        }

        public bool ResetAvatar (UUID userID)
        {
            bool success = m_localService.ResetAvatar (userID);
            if (!success)
                success = m_remoteService.ResetAvatar (userID);
            return success;
        }

        public bool SetItems (UUID userID, string[] names, string[] values)
        {
            bool success = m_localService.SetItems (userID, names, values);
            if (!success)
                success = m_remoteService.SetItems (userID, names, values);
            return success;
        }

        public bool RemoveItems (UUID userID, string[] names)
        {
            bool success = m_localService.RemoveItems (userID, names);
            if (!success)
                success = m_remoteService.RemoveItems (userID, names);
            return success;
        }

        public void CacheWearableData (UUID principalID, AvatarWearable cachedWearable)
        {
            //NOT DONE
        }

        #endregion
    }
}
