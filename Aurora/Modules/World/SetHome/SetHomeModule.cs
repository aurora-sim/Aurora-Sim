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
using OpenSim.Services.Interfaces;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Modules.World.Auction
{
    public class SetHomeModule : INonSharedRegionModule
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
            m_scene.EventManager.OnNewClient += EventManager_OnNewClient;
            m_scene.EventManager.OnClosingClient += EventManager_OnClosingClient;
            m_scene.EventManager.OnRegisterCaps += RegisterCaps;
        }

        public void RemoveRegion(Scene scene)
        {
            m_scene.EventManager.OnRegisterCaps -= RegisterCaps;
            m_scene.EventManager.OnNewClient -= EventManager_OnNewClient;
            m_scene.EventManager.OnClosingClient -= EventManager_OnClosingClient;
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

        public string Name
        {
            get { return "SetHomeModule"; }
        }

        public void Close()
        {
        }

        void EventManager_OnNewClient(IClientAPI client)
        {
            client.OnSetStartLocationRequest += SetHomeRezPoint;
        }

        void EventManager_OnClosingClient(IClientAPI client)
        {
            client.OnSetStartLocationRequest -= SetHomeRezPoint;
        }

        public void RegisterCaps(UUID agentID, IRegionClientCapsService caps)
        {
            UUID capuuid = UUID.Random();

            caps.AddStreamHandler("ServerReleaseNotes",
                                new RestHTTPHandler("POST", "/CAPS/ServerReleaseNotes/" + capuuid + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return ProcessServerReleaseNotes(m_dhttpMethod, agentID, capuuid);
                                                      }));

            caps.AddStreamHandler("CopyInventoryFromNotecard",
                                new RestHTTPHandler("POST", "/CAPS/" + capuuid + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return CopyInventoryFromNotecard(m_dhttpMethod, capuuid, agentID);
                                                      }));
        }

        private Hashtable ProcessServerReleaseNotes(Hashtable m_dhttpMethod, UUID agentID, UUID capuuid)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;

            OSDMap osd = new OSDMap();
            osd.Add("ServerReleaseNotes", new OSDString(Aurora.Framework.Utilities.GetServerReleaseNotesURL()));
            string response = OSDParser.SerializeLLSDXmlString(osd);
            responsedata["str_response_string"] = response;
            return responsedata;
        }

        private Hashtable CopyInventoryFromNotecard(Hashtable mDhttpMethod, UUID capuuid, UUID agentID)
        {
            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
            UUID FolderID = rm["folder-id"].AsUUID();
            UUID ItemID = rm["item-id"].AsUUID();
            //UUID NotecardID = rm["notecard-id"].AsUUID();
            //There is an object-id as well, but objects arn't handled through this currently as of Sept. 2010
            
            //TODO: Check notecard for this
            //SLUtil.ParseNotecardToList

            InventoryItemBase item = m_scene.GiveInventoryItem(agentID, agentID, ItemID, FolderID);

            IClientAPI client;
            m_scene.TryGetClient(agentID, out client);
            client.SendBulkUpdateInventory(item);

            //Send back data
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";
            return responsedata;
        }

        /// <summary>
        /// Sets the Home Point. The LoginService uses this to know where to put a user when they log-in
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="regionHandle"></param>
        /// <param name="position"></param>
        /// <param name="lookAt"></param>
        /// <param name="flags"></param>
        public void SetHomeRezPoint(IClientAPI remoteClient, ulong regionHandle, Vector3 position, Vector3 lookAt, uint flags)
        {
            Scene scene = (Scene)remoteClient.Scene;
            if (scene == null)
                return;

            ScenePresence SP = scene.GetScenePresence(remoteClient.AgentId);
            IDialogModule module = scene.RequestModuleInterface<IDialogModule>();

            if (module != null && SP != null)
            {
                if (scene.Permissions.CanSetHome(SP.UUID))
                {
                    position.Z += SP.Appearance.AvatarHeight / 2;
                    if (scene.GridUserService != null &&
                        scene.GridUserService.SetHome(remoteClient.AgentId.ToString(), scene.RegionInfo.RegionID, position, lookAt) &&
                        module != null) //Do this last so it doesn't screw up the rest
                    {
                        // FUBAR ALERT: this needs to be "Home position set." so the viewer saves a home-screenshot.
                        module.SendAlertToUser(remoteClient, "Home position set.");
                    }
                    else if (module != null)
                        module.SendAlertToUser(remoteClient, "Set Home request failed.");
                }
                else if (module != null)
                    module.SendAlertToUser(remoteClient, "Set Home request failed: Permissions do not allow the setting of home here.");
            }
        }
    }
}
