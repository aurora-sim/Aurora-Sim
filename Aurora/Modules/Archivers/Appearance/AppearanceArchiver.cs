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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Serialization.External;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using Aurora.Simulation.Base;

namespace Aurora.Modules.Archivers
{
    /// <summary>
    ///   This module loads/saves the avatar's appearance from/down into an "Avatar Archive", also known as an AA.
    /// </summary>
    public class AuroraAvatarAppearanceArchiver : IService, IAvatarAppearanceArchiver
    {
        #region Declares

        private IAssetService AssetService;
        private IAvatarService AvatarService;
        private IInventoryService InventoryService;
        private IUserAccountService UserAccountService;
        private IRegistryCore m_registry;

        #endregion

        #region IAvatarAppearanceArchiver Members

        public AvatarAppearance LoadAvatarArchive(string FileName, string Name)
        {
            UserAccount account = UserAccountService.GetUserAccount(null, Name);
            MainConsole.Instance.Info("[AvatarArchive] Loading archive from " + FileName);
            if (account == null)
            {
                MainConsole.Instance.Error("[AvatarArchive] User not found!");
                return null;
            }

            string archiveXML = "";
            if (FileName.EndsWith(".database"))
            {
                IAvatarArchiverConnector archiver = DataManager.DataManager.RequestPlugin<IAvatarArchiverConnector>();
                if (archiver != null)
                {
                    AvatarArchive archive = archiver.GetAvatarArchive(FileName.Substring(0, FileName.LastIndexOf(".database")));
                    archiveXML = archive.ArchiveXML;
                }
                else
                {
                    MainConsole.Instance.Error("[AvatarArchive] Unable to load from database!");
                    return null;
                }
            }
            else
            {
                StreamReader reader = new StreamReader(FileName);
                archiveXML = reader.ReadToEnd();
                reader.Close();
                reader.Dispose();
            }

            IScenePresence SP = null;
            ISceneManager manager = m_registry.RequestModuleInterface<ISceneManager>();
            if (manager != null)
            {
                foreach (IScene scene in manager.GetAllScenes())
                    if (scene.TryGetScenePresence(account.PrincipalID, out SP))
                        break;
                if (SP == null)
                    return null; //Bad people!
            }

            if (SP != null)
                SP.ControllingClient.SendAlertMessage("Appearance loading in progress...");

            string FolderNameToLoadInto = "";

            OSDMap map = ((OSDMap)OSDParser.DeserializeLLSDXml(archiveXML));

            OSDMap assetsMap = ((OSDMap)map["Assets"]);
            //OSDMap itemsMap = ((OSDMap)map["Items"]);
            OSDMap bodyMap = ((OSDMap)map["Body"]);

            AvatarAppearance appearance = ConvertXMLToAvatarAppearance(bodyMap, out FolderNameToLoadInto);

            appearance.Owner = account.PrincipalID;

            InventoryFolderBase AppearanceFolder = InventoryService.GetFolderForType(account.PrincipalID,
                                                                                     InventoryType.Wearable,
                                                                                     AssetType.Clothing);

            List<InventoryItemBase> items = new List<InventoryItemBase>();

            InventoryFolderBase folderForAppearance
                = new InventoryFolderBase(
                    UUID.Random(), FolderNameToLoadInto, account.PrincipalID,
                    -1, AppearanceFolder.ID, 1);

            InventoryService.AddFolder(folderForAppearance);

            folderForAppearance = InventoryService.GetFolder(folderForAppearance);

            try
            {
                LoadAssets(assetsMap);
                appearance = CopyWearablesAndAttachments(account.PrincipalID, UUID.Zero, appearance, folderForAppearance, account.PrincipalID, out items);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[AvatarArchiver]: Error loading assets and items, " + ex);
            }

            //Now update the client about the new items
            if (SP != null)
                SP.ControllingClient.SendBulkUpdateInventory(folderForAppearance);

            MainConsole.Instance.Info("[AvatarArchive] Loaded archive from " + FileName);
            return appearance;
        }

        private AvatarAppearance CopyWearablesAndAttachments(UUID destination, UUID source, AvatarAppearance avatarAppearance, InventoryFolderBase destinationFolder, UUID agentid, out List<InventoryItemBase> items)
        {
            if (destinationFolder == null)
                throw new Exception("Cannot locate folder(s)");
            items = new List<InventoryItemBase>();

            // Wearables
            AvatarWearable[] wearables = avatarAppearance.Wearables;

            for (int i = 0; i < wearables.Length; i++)
            {
                AvatarWearable wearable = wearables[i];
                for (int ii = 0; ii < wearable.Count; ii++)
                {
                    if (wearable[ii].ItemID != UUID.Zero)
                    {
                        // Get inventory item and copy it
                        InventoryItemBase item = new InventoryItemBase(wearable[ii].ItemID);
                        item = InventoryService.GetItem(item);

                        if (item != null)
                        {

                            InventoryItemBase destinationItem = InventoryService.InnerGiveInventoryItem(destination,
                                                                                                    destination, item,
                                                                                                    destinationFolder.ID,
                                                                                                    false);
                            items.Add(destinationItem);
                            MainConsole.Instance.DebugFormat("[RADMIN]: Added item {0} to folder {1}",
                                                             destinationItem.ID, destinationFolder.ID);

                            // Wear item
                            AvatarWearable newWearable = new AvatarWearable();
                            newWearable.Wear(destinationItem.ID, wearable[ii].AssetID);
                            avatarAppearance.SetWearable(i, newWearable);
                        }
                        else
                        {
                            MainConsole.Instance.WarnFormat("[RADMIN]: Error transferring {0} to folder {1}",
                                                            wearable[ii].ItemID, destinationFolder.ID);
                        }
                    }
                }
            }

            // Attachments
            List<AvatarAttachment> attachments = avatarAppearance.GetAttachments();

            foreach (AvatarAttachment attachment in attachments)
            {
                int attachpoint = attachment.AttachPoint;
                UUID itemID = attachment.ItemID;

                if (itemID != UUID.Zero)
                {
                    // Get inventory item and copy it
                    InventoryItemBase item = new InventoryItemBase(itemID, source);
                    item = InventoryService.GetItem(item);

                    if (item != null)
                    {
                        InventoryItemBase destinationItem = InventoryService.InnerGiveInventoryItem(destination,
                                                                                                    destination, item,
                                                                                                    destinationFolder.ID,
                                                                                                    false);
                        items.Add(destinationItem);
                        MainConsole.Instance.DebugFormat("[RADMIN]: Added item {0} to folder {1}", destinationItem.ID, destinationFolder.ID);

                        // Attach item
                        avatarAppearance.SetAttachment(attachpoint, destinationItem.ID, destinationItem.AssetID);
                        MainConsole.Instance.DebugFormat("[RADMIN]: Attached {0}", destinationItem.ID);
                    }
                    else
                    {
                        MainConsole.Instance.WarnFormat("[RADMIN]: Error transferring {0} to folder {1}", itemID, destinationFolder.ID);
                    }
                }
            }
            return avatarAppearance;
        }

        #endregion

        #region Console Commands

        protected void HandleLoadAvatarArchive(string[] cmdparams)
        {
            if (cmdparams.Length != 6)
            {
                MainConsole.Instance.Info("[AvatarArchive] Not enough parameters!");
                return;
            }
            LoadAvatarArchive(cmdparams[5], cmdparams[3] + " " + cmdparams[4]);
        }

        private InventoryItemBase GiveInventoryItem(UUID senderId, UUID recipient, InventoryItemBase item,
                                                    InventoryFolderBase parentFolder)
        {
            InventoryItemBase itemCopy = new InventoryItemBase
            {
                Owner = recipient,
                CreatorId = item.CreatorId,
                CreatorData = item.CreatorData,
                ID = UUID.Random(),
                AssetID = item.AssetID,
                Description = item.Description,
                Name = item.Name,
                AssetType = item.AssetType,
                InvType = item.InvType,
                Folder = UUID.Zero,
                NextPermissions = (uint)PermissionMask.All,
                GroupPermissions = (uint)PermissionMask.All,
                EveryOnePermissions = (uint)PermissionMask.All,
                CurrentPermissions = (uint)PermissionMask.All
            };

            //Give full permissions for them

            if (parentFolder == null)
            {
                InventoryFolderBase folder = InventoryService.GetFolderForType(recipient, InventoryType.Unknown,
                                                                               (AssetType)itemCopy.AssetType);

                if (folder != null)
                    itemCopy.Folder = folder.ID;
                else
                {
                    InventoryFolderBase root = InventoryService.GetRootFolder(recipient);

                    if (root != null)
                        itemCopy.Folder = root.ID;
                    else
                        return null; // No destination
                }
            }
            else
                itemCopy.Folder = parentFolder.ID; //We already have a folder to put it in

            itemCopy.GroupID = UUID.Zero;
            itemCopy.GroupOwned = false;
            itemCopy.Flags = item.Flags;
            itemCopy.SalePrice = item.SalePrice;
            itemCopy.SaleType = item.SaleType;

            InventoryService.AddItem(itemCopy);
            return itemCopy;
        }

        private AvatarAppearance ConvertXMLToAvatarAppearance(OSDMap map, out string FolderNameToPlaceAppearanceIn)
        {
            AvatarAppearance appearance = new AvatarAppearance();
            appearance.Unpack(map);
            FolderNameToPlaceAppearanceIn = map["FolderName"].AsString();
            return appearance;
        }

        protected void HandleSaveAvatarArchive(string[] cmdparams)
        {
            if (cmdparams.Length < 7)
            {
                MainConsole.Instance.Info("[AvatarArchive] Not enough parameters!");
            }
            UserAccount account = UserAccountService.GetUserAccount(null, cmdparams[3] + " " + cmdparams[4]);
            if (account == null)
            {
                MainConsole.Instance.Error("[AvatarArchive] User not found!");
                return;
            }

            IScenePresence SP = null;
            ISceneManager manager = m_registry.RequestModuleInterface<ISceneManager>();
            if (manager != null)
            {
                foreach (IScene scene in manager.GetAllScenes())
                    if (scene.TryGetScenePresence(account.PrincipalID, out SP))
                        break;
                if (SP == null)
                    return; //Bad people!
            }

            if (SP != null)
                SP.ControllingClient.SendAlertMessage("Appearance saving in progress...");

            AvatarAppearance appearance = AvatarService.GetAppearance(account.PrincipalID);
            if (appearance == null)
            {
                IAvatarAppearanceModule appearancemod = m_registry.RequestModuleInterface<IAvatarAppearanceModule>();
                appearance = appearancemod.Appearance;
            }
            OSDMap map = new OSDMap();
            OSDMap body = new OSDMap();
            OSDMap assets = new OSDMap();
            OSDMap items = new OSDMap();
            body = appearance.Pack();
            body.Add("FolderName", OSD.FromString(cmdparams[6]));

            foreach (AvatarWearable wear in appearance.Wearables)
            {
                for (int i = 0; i < wear.Count; i++)
                {
                    WearableItem w = wear[i];
                    if (w.AssetID != UUID.Zero)
                    {
                        SaveItem(w.ItemID, items, assets);
                        SaveAsset(w.AssetID, assets);
                    }
                }
            }
            List<AvatarAttachment> attachments = appearance.GetAttachments();
#if (!ISWIN)
            foreach (AvatarAttachment a in attachments)
            {
                if (a.AssetID != UUID.Zero)
                {
                    SaveItem(a.ItemID, items, assets);
                    SaveAsset(a.AssetID, assets);
                }
            }
#else
            foreach (AvatarAttachment a in attachments.Where(a => a.AssetID != UUID.Zero))
            {
                SaveItem(a.ItemID, items, assets);
                SaveAsset(a.AssetID, assets);
            }
#endif
            map.Add("Body", body);
            map.Add("Assets", assets);
            map.Add("Items", items);

            //Write the map
            if (cmdparams[5].EndsWith(".database"))
            {
                IAvatarArchiverConnector archiver = DataManager.DataManager.RequestPlugin<IAvatarArchiverConnector>();
                if (archiver != null)
                {
                    AvatarArchive archive = new AvatarArchive();
                    archive.ArchiveXML = OSDParser.SerializeLLSDXmlString(map);

                    // Add the extra details for archives
                    archive.Name = cmdparams[5].Substring(0, cmdparams[5].LastIndexOf(".database"));
                    if (cmdparams.Length > 7)
                    {
                        if (cmdparams.Contains("--snapshot"))
                        {
                            UUID snapshot;
                            int index = 0;
                            for(; index < cmdparams.Length; index++)
                            {
                                if(cmdparams[index] == "--snapshot")
                                {
                                    index++;
                                    break;
                                }
                            }
                            if(index < cmdparams.Length && UUID.TryParse(cmdparams[index], out snapshot))
                            {
                                archive.Snapshot = snapshot.ToString();
                            }
                        }
                        else
                        {
                            archive.Snapshot = UUID.Zero.ToString();
                        }
                        if (cmdparams.Contains("--public"))
                        {
                            archive.IsPublic = 1;
                        }
                    }
                    else
                    {
                        archive.Snapshot = UUID.Zero.ToString();
                        archive.IsPublic = 0;
                    }

                    // Save the archive
                    archiver.SaveAvatarArchive(archive);
                    MainConsole.Instance.Info("[AvatarArchive] Saved archive to database as: " + archive.Name);
                }
                else
                {
                    MainConsole.Instance.Error("[AvatarArchive] Unable to save to database!");
                    return;
                }
            }
            else
            {
                StreamWriter writer = new StreamWriter(cmdparams[5], false);
                writer.Write(OSDParser.SerializeLLSDXmlString(map));
                writer.Close();
                writer.Dispose();
                MainConsole.Instance.Info("[AvatarArchive] Saved archive to " + cmdparams[5]);
            }
        }

        private void SaveAsset(UUID AssetID, OSDMap assetMap)
        {
            try
            {
                AssetBase asset = AssetService.Get(AssetID.ToString());
                if (asset != null)
                {
                    OSDMap assetData = new OSDMap();
                    MainConsole.Instance.Info("[AvatarArchive]: Saving asset " + asset.ID);
                    CreateMetaDataMap(asset, assetData);
                    assetData.Add("AssetData", OSD.FromBinary(asset.Data));
                    assetMap.Add(asset.ID.ToString(), assetData);
                }
                else
                {
                    MainConsole.Instance.Warn("[AvatarArchive]: Could not find asset to save: " + AssetID.ToString());
                    return;
                }
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[AvatarArchive]: Could not save asset: " + AssetID.ToString() + ", " + ex);
            }
        }

        private void CreateMetaDataMap(AssetBase data, OSDMap map)
        {
            map["ContentType"] = OSD.FromString(data.TypeString);
            map["CreationDate"] = OSD.FromDate(data.CreationDate);
            map["CreatorID"] = OSD.FromUUID(data.CreatorID);
            map["Description"] = OSD.FromString(data.Description);
            map["ID"] = OSD.FromUUID(data.ID);
            map["Name"] = OSD.FromString(data.Name);
            map["Type"] = OSD.FromInteger(data.Type);
        }

        private AssetBase LoadAssetBase(OSDMap map)
        {
            AssetBase asset = new AssetBase
            {
                Data = map["AssetData"].AsBinary(),
                TypeString = map["ContentType"].AsString(),
                CreationDate = map["CreationDate"].AsDate(),
                CreatorID = map["CreatorID"].AsUUID(),
                Description = map["Description"].AsString(),
                ID = map["ID"].AsUUID(),
                Name = map["Name"].AsString(),
                Type = (sbyte)map["Type"].AsInteger()
            };
            return asset;
        }

        private void SaveItem(UUID ItemID, OSDMap itemMap, OSDMap assets)
        {
            InventoryItemBase saveItem = InventoryService.GetItem(new InventoryItemBase(ItemID));
            if (saveItem == null)
            {
                MainConsole.Instance.Warn("[AvatarArchive]: Could not find item to save: " + ItemID);
                return;
            }
            MainConsole.Instance.Info("[AvatarArchive]: Saving item " + ItemID.ToString());
            string serialization = UserInventoryItemSerializer.Serialize(saveItem);
            itemMap[ItemID.ToString()] = OSD.FromString(serialization);
        }

        private void LoadAssets(OSDMap assets)
        {
            foreach (KeyValuePair<string, OSD> kvp in assets)
            {
                UUID AssetID = UUID.Parse(kvp.Key);
                OSDMap assetMap = (OSDMap)kvp.Value;
                AssetBase asset = AssetService.Get(AssetID.ToString());
                MainConsole.Instance.Info("[AvatarArchive]: Loading asset " + AssetID.ToString());
                if (asset == null) //Don't overwrite
                {
                    asset = LoadAssetBase(assetMap);
                    asset.ID = AssetService.Store(asset);
                }
            }
        }

        private void LoadItems(OSDMap items, UUID OwnerID, InventoryFolderBase folderForAppearance,
                               out List<InventoryItemBase> litems)
        {
            litems = new List<InventoryItemBase>();
            foreach (KeyValuePair<string, OSD> kvp in items)
            {
                string serialization = kvp.Value.AsString();
                InventoryItemBase item = UserInventoryItemSerializer.Deserialize(serialization);
                MainConsole.Instance.Info("[AvatarArchive]: Loading item " + item.ID.ToString());
                item = GiveInventoryItem(item.CreatorIdAsUuid, OwnerID, item, folderForAppearance);
                litems.Add(item);
            }
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand("save avatar archive",
                                                         "save avatar archive <First> <Last> <Filename> <FolderNameToSaveInto> (--snapshot <UUID>) (--public)",
                                                         "Saves appearance to an avatar archive (Note: put \"\" around the FolderName if you need more than one word. Put all attachments in BodyParts folder before saving the archive) Both --snapshot and --public are optional.",
                                                         HandleSaveAvatarArchive);
                MainConsole.Instance.Commands.AddCommand("load avatar archive",
                                                         "load avatar archive <First> <Last> <Filename>",
                                                         "Loads appearance from an avatar archive",
                                                         HandleLoadAvatarArchive);
            }
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            UserAccountService = registry.RequestModuleInterface<IUserAccountService>();
            AvatarService = registry.RequestModuleInterface<IAvatarService>();
            AssetService = registry.RequestModuleInterface<IAssetService>();
            InventoryService = registry.RequestModuleInterface<IInventoryService>();
            m_registry.RegisterModuleInterface<IAvatarAppearanceArchiver>(this);
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}