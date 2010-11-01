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

namespace Aurora.Modules.World.Auction
{
    public class SetHomeModule : INonSharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;

        public void Initialise(IConfigSource pSource)
        {
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;
            m_scene.EventManager.OnRegisterCaps += RegisterCaps;
        }

        public void RemoveRegion(Scene scene)
        {
            m_scene.EventManager.OnRegisterCaps -= RegisterCaps;
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

        public void RegisterCaps(UUID agentID, Caps caps)
        {
            UUID capuuid = UUID.Random();

            caps.RegisterHandler("ServerReleaseNotes",
                                new RestHTTPHandler("POST", "/CAPS/ServerReleaseNotes/" + capuuid + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return ProcessServerReleaseNotes(m_dhttpMethod, agentID, capuuid);
                                                      }));

            caps.RegisterHandler("CopyInventoryFromNotecard",
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
            UUID NotecardID = rm["notecard-id"].AsUUID();
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

        public string Name
        {
            get { return "SetHomeModule"; }
        }

        public void Close()
        {
        }
    }
}
