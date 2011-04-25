using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
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

        public OSDMap RegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["ServerReleaseNotes"] = CapsUtil.CreateCAPS("ServerReleaseNotes", "");

            server.AddStreamHandler(new RestHTTPHandler("POST", retVal["ServerReleaseNotes"],
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return ProcessServerReleaseNotes(m_dhttpMethod, agentID);
                                                      }));

            retVal["CopyInventoryFromNotecard"] = CapsUtil.CreateCAPS("CopyInventoryFromNotecard", "");

            server.AddStreamHandler(new RestHTTPHandler("POST", retVal["CopyInventoryFromNotecard"],
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return CopyInventoryFromNotecard(m_dhttpMethod, agentID);
                                                      }));
            return retVal;
        }

        private Hashtable ProcessServerReleaseNotes(Hashtable m_dhttpMethod, UUID agentID)
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

        private Hashtable CopyInventoryFromNotecard(Hashtable mDhttpMethod, UUID agentID)
        {
            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
            UUID FolderID = rm["folder-id"].AsUUID();
            UUID ItemID = rm["item-id"].AsUUID();
            UUID NotecardID = rm["notecard-id"].AsUUID();
            UUID ObjectID = rm["object-id"].AsUUID();
            InventoryItemBase notecardItem = null;
            if (ObjectID != UUID.Zero)
            {
                ISceneChildEntity part = m_scene.GetSceneObjectPart(ObjectID);
                if (part != null)
                {
                    TaskInventoryItem item = part.Inventory.GetInventoryItem(NotecardID);
                    if (m_scene.Permissions.CanCopyObjectInventory(NotecardID, ObjectID, agentID))
                    {
                        notecardItem = new InventoryItemBase(NotecardID, agentID);
                        notecardItem.AssetID = item.AssetID;
                    }
                }
            }
            else
                notecardItem = m_scene.InventoryService.GetItem(new InventoryItemBase(NotecardID));
            if(notecardItem != null && notecardItem.Owner == agentID)
            {
                AssetBase asset = m_scene.AssetService.Get(notecardItem.AssetID.ToString());
                if (asset != null)
                {
                    System.Text.UTF8Encoding enc =
                                new System.Text.UTF8Encoding();
                    List<string> notecardData = SLUtil.ParseNotecardToList(enc.GetString(asset.Data));
                    OpenMetaverse.Assets.AssetNotecard noteCardAsset = new OpenMetaverse.Assets.AssetNotecard(UUID.Zero, asset.Data);
                    noteCardAsset.Decode();
                    bool found = false;
                    foreach (InventoryItem notecardObjectItem in noteCardAsset.EmbeddedItems)
                    {
                        if (notecardObjectItem.UUID == ItemID)
                        {
                            //Make sure that it exists
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        InventoryItemBase item = null;
                        ILLClientInventory inventoryModule = m_scene.RequestModuleInterface<ILLClientInventory>();
                        if (inventoryModule != null)
                            item = inventoryModule.GiveInventoryItem(agentID, agentID, ItemID, FolderID);

                        IClientAPI client;
                        m_scene.TryGetClient(agentID, out client);
                        client.SendBulkUpdateInventory(item);
                    }
                }
            }

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

            IScenePresence SP = scene.GetScenePresence (remoteClient.AgentId);
            IDialogModule module = scene.RequestModuleInterface<IDialogModule>();

            if (SP != null)
            {
                if (scene.Permissions.CanSetHome(SP.UUID))
                {
                    IAvatarAppearanceModule appearance = SP.RequestModuleInterface<IAvatarAppearanceModule> ();
                    position.Z += appearance.Appearance.AvatarHeight / 2;
                    IAgentInfoService agentInfoService = scene.RequestModuleInterface<IAgentInfoService>();
                    if (agentInfoService != null &&
                        agentInfoService.SetHomePosition(remoteClient.AgentId.ToString(), scene.RegionInfo.RegionID, position, lookAt) &&
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
