using System;
using System.Collections.Generic;
using Nini.Config;
using log4net;
using System.Reflection;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Region.CoreModules.ServiceConnectorsOut;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Avatar;

namespace Aurora.Modules.Communications.MultipleGrids
{
    public class MultipleAvatarServicesConnector : ISharedRegionModule, IAvatarService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Enabled = false;
        private List<IAvatarService> AllServices = new List<IAvatarService>();

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "MultipleAvatarServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("AvatarServices", "");
                if (name == Name)
                {
                    IConfig multipleConfig = source.Configs["MultipleGridsModule"];
                    if (multipleConfig != null)
                    {
                        IConfig UAS = source.Configs["AvatarService"];
                        if (UAS != null)
                        {
                            string[] Grids = multipleConfig.GetString("AvatarServerURIs", "").Split(',');
                            //Set it so that it works for them
                            moduleConfig.Set("AvatarServices", "RemoteAvatarServicesConnector");
                            foreach (string gridURL in Grids)
                            {
                                //Set their gridURL
                                UAS.Set("AvatarServerURI", gridURL);
                                //Start it up
                                RemoteAvatarServicesConnector connector = new RemoteAvatarServicesConnector();
                                connector.Initialise(source);
                                AllServices.Add(connector);
                                m_log.Info("[AVATAR CONNECTOR]: Multiple avatar services enabled for " + gridURL);
                            }
                        }
                    }
                    //Reset the name
                    moduleConfig.Set("AvatarServices", Name);
                    m_Enabled = true;
                }
            }
        }

        public void PostInitialise()
        {
            if (!m_Enabled)
                return;
        }

        public void Close()
        {
            if (!m_Enabled)
                return;
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            scene.RegisterModuleInterface<IAvatarService>(this);
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;
        }

        #region IAvatarService Members

        public OpenSim.Framework.AvatarAppearance GetAppearance(UUID userID)
        {
            OpenSim.Framework.AvatarAppearance appearance = null;
            foreach (IAvatarService service in AllServices)
            {
                appearance = service.GetAppearance(userID);
                if (appearance != null)
                    return appearance;
            }
            return appearance;
        }

        public bool SetAppearance(UUID userID, OpenSim.Framework.AvatarAppearance appearance)
        {
            bool r = false;
            foreach (IAvatarService service in AllServices)
            {
                r = service.SetAppearance(userID, appearance);
                if (r)
                    return r;
            }
            return r;
        }

        public AvatarData GetAvatar(UUID userID)
        {
            AvatarData appearance = null;
            foreach (IAvatarService service in AllServices)
            {
                appearance = service.GetAvatar(userID);
                if (appearance != null)
                    return appearance;
            }
            return appearance;
        }

        public bool SetAvatar(UUID userID, AvatarData avatar)
        {
            bool r = false;
            foreach (IAvatarService service in AllServices)
            {
                r = service.SetAvatar(userID, avatar);
                if (r)
                    return r;
            }
            return r;
        }

        public bool ResetAvatar(UUID userID)
        {
            bool r = false;
            foreach (IAvatarService service in AllServices)
            {
                r = service.ResetAvatar(userID);
                if (r)
                    return r;
            }
            return r;
        }

        public bool SetItems(UUID userID, string[] names, string[] values)
        {
            bool r = false;
            foreach (IAvatarService service in AllServices)
            {
                r = service.SetItems(userID, names, values);
                if (r)
                    return r;
            }
            return r;
        }

        public bool RemoveItems(UUID userID, string[] names)
        {
            bool r = false;
            foreach (IAvatarService service in AllServices)
            {
                r = service.RemoveItems(userID, names);
                if (r)
                    return r;
            }
            return r;
        }

        #endregion
    }
}
