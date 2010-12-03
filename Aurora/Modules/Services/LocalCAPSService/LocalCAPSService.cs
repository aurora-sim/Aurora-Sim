using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Caps = OpenSim.Framework.Capabilities.Caps;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Services.CapsService;
using OpenSim.Server.Base;

namespace Aurora.Modules
{
    /// <summary>
    /// This sets up the CAPS service if there is not a remote CAPS service on the ROBUST server
    /// </summary>
    public class LocalCAPSService : ISharedRegionModule
    {
        //private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private bool m_enabled = false;
        private Scene m_scene;

        public void PostInitialise()
        {
        }

        public string Name
        {
            get { return "LocalCapsService"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["LocalCapsService"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("CapsService", "LocalCapsService");
                if (name == Name)
                {
                    m_enabled = true;
                }
            }
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_enabled)
                return;
            if (m_scene != null)
                return; //only load once

            m_scene = scene;

            scene.EventManager.OnRegisterCaps += new EventManager.RegisterCapsEvent(EventManager_OnRegisterCaps);
        }

        void EventManager_OnRegisterCaps(UUID agentID, Caps caps)
        {
            CAPSPrivateSeedHandler handler = new CAPSPrivateSeedHandler(null,//the server IS null for a reason, so that we don't add the handlers at the wrong time
                m_scene.InventoryService, 
                m_scene.LibraryService, m_scene.GridUserService,
                m_scene.PresenceService, "", agentID, "", false); //URL and Hostname are all "" as well so that we don't add the hostname by accident

            List<IRequestHandler> handlers = handler.GetServerCAPS();

            foreach (IRequestHandler handle in handlers)
            {
                if (handler.registeredCAPSPath.ContainsKey(handle.Path))
                {
                    caps.RegisterHandler(handler.registeredCAPSPath[handle.Path].ToString(), handle);
                }
            }
        }
    }
}
