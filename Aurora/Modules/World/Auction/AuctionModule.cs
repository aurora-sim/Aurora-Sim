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

namespace Aurora.Modules
{
    public class AuctionModule : INonSharedRegionModule
    {
        //private static readonly ILog m_log =
        //    LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;

        public void Initialise(IConfigSource pSource)
        {
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;
            m_scene.EventManager.OnRegisterCaps += RegisterCaps;
            m_scene.EventManager.OnNewClient += OnNewClient;
            m_scene.EventManager.OnClosingClient += OnClosingClient;
        }

        public void RemoveRegion(Scene scene)
        {
            m_scene.EventManager.OnRegisterCaps -= RegisterCaps;
            m_scene.EventManager.OnNewClient -= OnNewClient;
            m_scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise()
        {
        }

        public void OnNewClient(IClientAPI client)
        {
            client.OnViewerStartAuction += client_OnViewerStartAuction;
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnViewerStartAuction -= client_OnViewerStartAuction;
        }

        void client_OnViewerStartAuction(IClientAPI client, int LocalID, UUID SnapshotID)
        {
            if (!m_scene.Permissions.IsGod(client.AgentId))
                return;
            IParcelManagementModule parcelManagement = m_scene.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject landObject = parcelManagement.GetLandObject(LocalID);
                if (landObject == null)
                    return;
                landObject.LandData.SnapshotID = SnapshotID;
                landObject.LandData.AuctionID++;
                landObject.SendLandUpdateToAvatarsOverMe();
            }
        }

        public void RegisterCaps(UUID agentID, Caps caps)
        {
            UUID capuuid = UUID.Random();

            caps.RegisterHandler("ViewerStartAuction",
                                new RestHTTPHandler("POST", "/CAPS/" + capuuid + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return ViewerStartAuction(m_dhttpMethod, capuuid);
                                                      }));
        }

        private Hashtable ViewerStartAuction(Hashtable mDhttpMethod, UUID capuuid)
        {
            //OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
            
            //Send back data
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";
            return responsedata;
        }

        public string Name
        {
            get { return "AuctionModule"; }
        }

        public void Close()
        {
        }
    }
}
