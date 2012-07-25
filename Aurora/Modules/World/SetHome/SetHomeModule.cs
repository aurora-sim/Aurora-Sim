/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Capabilities;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.SetHome
{
    public class SetHomeModule : INonSharedRegionModule
    {
        //private static readonly ILog MainConsole.Instance =
        //    LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IScene m_scene;

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource pSource)
        {
        }

        public void AddRegion(IScene scene)
        {
            m_scene = scene;
            m_scene.EventManager.OnNewClient += EventManager_OnNewClient;
            m_scene.EventManager.OnClosingClient += EventManager_OnClosingClient;
            m_scene.EventManager.OnRegisterCaps += RegisterCaps;
        }

        public void RemoveRegion(IScene scene)
        {
            m_scene.EventManager.OnRegisterCaps -= RegisterCaps;
            m_scene.EventManager.OnNewClient -= EventManager_OnNewClient;
            m_scene.EventManager.OnClosingClient -= EventManager_OnClosingClient;
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "SetHomeModule"; }
        }

        public void Close()
        {
        }

        #endregion

        public void PostInitialise()
        {
        }

        private void EventManager_OnNewClient(IClientAPI client)
        {
            client.OnSetStartLocationRequest += SetHomeRezPoint;
        }

        private void EventManager_OnClosingClient(IClientAPI client)
        {
            client.OnSetStartLocationRequest -= SetHomeRezPoint;
        }

        public OSDMap RegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["ServerReleaseNotes"] = CapsUtil.CreateCAPS("ServerReleaseNotes", "");

            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["ServerReleaseNotes"],
                                                      delegate(string path, Stream request,
                                                        OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                      {
                                                          return ProcessServerReleaseNotes(agentID);
                                                      }));

            retVal["CopyInventoryFromNotecard"] = CapsUtil.CreateCAPS("CopyInventoryFromNotecard", "");

            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["CopyInventoryFromNotecard"],
                                                      delegate(string path, Stream request,
                                                        OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                      {
                                                          return CopyInventoryFromNotecard(request, agentID);
                                                      }));
            return retVal;
        }

        private byte[] ProcessServerReleaseNotes(UUID agentID)
        {
            OSDMap osd = new OSDMap {{"ServerReleaseNotes", new OSDString(Utilities.GetServerReleaseNotesURL())}};
            return OSDParser.SerializeLLSDXmlBytes(osd);
        }

        private byte[] CopyInventoryFromNotecard(Stream request, UUID agentID)
        {
            OSDMap rm = (OSDMap) OSDParser.DeserializeLLSDXml(request);
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
                        notecardItem = new InventoryItemBase(NotecardID, agentID) {AssetID = item.AssetID};
                    }
                }
            }
            else
                notecardItem = m_scene.InventoryService.GetItem(new InventoryItemBase(NotecardID));
            if (notecardItem != null && notecardItem.Owner == agentID)
            {
                AssetBase asset = m_scene.AssetService.Get(notecardItem.AssetID.ToString());
                if (asset != null)
                {
                    AssetNotecard noteCardAsset = new AssetNotecard(UUID.Zero, asset.Data);
                    noteCardAsset.Decode();
                    bool found = false;
                    UUID lastOwnerID = UUID.Zero;
                    foreach (InventoryItem notecardObjectItem in noteCardAsset.EmbeddedItems.Where(notecardObjectItem => notecardObjectItem.UUID == ItemID))
                    {
                        //Make sure that it exists
                        found = true;
                        lastOwnerID = notecardObjectItem.OwnerID;
                        break;
                    }
                    if (found)
                    {
                        m_scene.InventoryService.GiveInventoryItemAsync(agentID, lastOwnerID, ItemID, FolderID, false,
                            (item) =>
                        {
                            IClientAPI client;
                            m_scene.ClientManager.TryGetValue(agentID, out client);
                            if (item != null)
                                client.SendBulkUpdateInventory(item);
                            else
                                client.SendAlertMessage("Failed to retrieve item");
                        });
                    }
                }
            }

            return new byte[0];
        }

        /// <summary>
        ///   Sets the Home Point. The LoginService uses this to know where to put a user when they log-in
        /// </summary>
        /// <param name = "remoteClient"></param>
        /// <param name = "regionHandle"></param>
        /// <param name = "position"></param>
        /// <param name = "lookAt"></param>
        /// <param name = "flags"></param>
        public void SetHomeRezPoint(IClientAPI remoteClient, ulong regionHandle, Vector3 position, Vector3 lookAt,
                                    uint flags)
        {
            IScene scene = remoteClient.Scene;

            IScenePresence SP = scene.GetScenePresence(remoteClient.AgentId);
            IDialogModule module = scene.RequestModuleInterface<IDialogModule>();

            if (SP != null)
            {
                if (scene.Permissions.CanSetHome(SP.UUID))
                {
                    IAvatarAppearanceModule appearance = SP.RequestModuleInterface<IAvatarAppearanceModule>();
                    position.Z += appearance.Appearance.AvatarHeight/2;
                    IAgentInfoService agentInfoService = scene.RequestModuleInterface<IAgentInfoService>();
                    if (agentInfoService != null &&
                        agentInfoService.SetHomePosition(remoteClient.AgentId.ToString(), scene.RegionInfo.RegionID,
                                                         position, lookAt) &&
                        module != null) //Do this last so it doesn't screw up the rest
                    {
                        // FUBAR ALERT: this needs to be "Home position set." so the viewer saves a home-screenshot.
                        module.SendAlertToUser(remoteClient, "Home position set.");
                    }
                    else if (module != null)
                        module.SendAlertToUser(remoteClient, "Set Home request failed.");
                }
                else if (module != null)
                    module.SendAlertToUser(remoteClient,
                                           "Set Home request failed: Permissions do not allow the setting of home here.");
            }
        }
    }
}