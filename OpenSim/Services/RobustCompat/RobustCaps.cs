using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Capabilities;
using Aurora.Simulation.Base;

namespace OpenSim.Services.RobustCompat
{
    public class RobustCaps : INonSharedRegionModule
    {
        #region Declares

        private Scene m_scene;
        private bool m_enabled = false;

        #endregion

        #region IRegionModuleBase Members

        public void Initialise(Nini.Config.IConfigSource source)
        {
            m_enabled = source.Configs["Handlers"].GetBoolean("RobustCompatibility", m_enabled);
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;
            m_scene = scene;
            scene.EventManager.OnClosingClient += OnClosingClient;
            scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
            scene.AuroraEventManager.OnGenericEvent += OnGenericEvent;
        }

        void OnMakeRootAgent (IScenePresence presence)
        {
            if ((presence.CallbackURI != null) && !presence.CallbackURI.Equals(""))
            {
                WebUtils.ServiceOSDRequest(presence.CallbackURI, null, "DELETE", 10000);
                presence.CallbackURI = null;
            }
        }

        void OnClosingClient(IClientAPI client)
        {
            ICapsService service = m_scene.RequestModuleInterface<ICapsService>();
            if (service != null)
            {
                IClientCapsService clientCaps = service.GetClientCapsService(client.AgentId);
                if (clientCaps != null)
                {
                    IRegionClientCapsService regionCaps = clientCaps.GetCapsService(m_scene.RegionInfo.RegionHandle);
                    if (regionCaps != null)
                    {
                        regionCaps.Close();
                        clientCaps.RemoveCAPS(m_scene.RegionInfo.RegionHandle);
                    }
                }
            }
        }

        protected object OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "NewUserConnection")
            {
                ICapsService service = m_scene.RequestModuleInterface<ICapsService>();
                if (service != null)
                {
                    OSDMap param = (OSDMap)parameters;
                    AgentCircuitData circuit = new AgentCircuitData();
                    circuit.UnpackAgentCircuitData((OSDMap)param["Agent"]);
                    service.CreateCAPS(circuit.AgentID, CapsUtil.GetCapsSeedPath(circuit.CapsPath),
                        m_scene.RegionInfo.RegionHandle, !circuit.child, circuit);
                    IClientCapsService clientCaps = service.GetClientCapsService(circuit.AgentID);
                    if (clientCaps != null)
                    {
                        IRegionClientCapsService regionCaps = clientCaps.GetCapsService(m_scene.RegionInfo.RegionHandle);
                        if (regionCaps != null)
                        {
                            regionCaps.AddCAPS((OSDMap)param["CapsUrls"]);
                        }
                    }
                }
            }
            else if (FunctionName == "UserStatusChange")
            {
                UserInfo info = (UserInfo)parameters;
                if (!info.IsOnline) //Logging out
                {
                    ICapsService service = m_scene.RequestModuleInterface<ICapsService>();
                    if (service != null)
                    {
                        service.RemoveCAPS(UUID.Parse(info.UserID));
                    }
                }
            }
            return null;
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "RobustCaps"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion
    }
}
