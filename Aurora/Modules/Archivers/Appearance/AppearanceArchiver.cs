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

using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.DatabaseInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Serialization.External;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Assets;
using Aurora.Framework.Services.ClassHelpers.Inventory;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aurora.Modules.Archivers
{
    /// <summary>
    ///     This module loads/saves the avatar's appearance from/down into an "Avatar Archive", also known as an AA.
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

        public AvatarArchive LoadAvatarArchive(string fileName, UUID principalID)
        {
            AvatarArchive archive = new AvatarArchive();
            UserAccount account = UserAccountService.GetUserAccount(null, principalID);
            if (account == null)
            {
                MainConsole.Instance.Error("[AvatarArchive]: User not found!");
                return null;
            }

            if (!File.Exists(fileName))
            {
                MainConsole.Instance.Error("[AvatarArchive]: Unable to load from file: file does not exist!");
                return null;
            }
            MainConsole.Instance.Info("[AvatarArchive]: Loading archive from " + fileName);

            archive.FromOSD((OSDMap)OSDParser.DeserializeLLSDXml(File.ReadAllText(fileName)));

            AvatarAppearance appearance = ConvertXMLToAvatarAppearance(archive.BodyMap);

            appearance.Owner = principalID;

            InventoryFolderBase AppearanceFolder = InventoryService.GetFolderForType(account.PrincipalID,
                                                                                     InventoryType.Wearable,
                                                                                     AssetType.Clothing);

            List<InventoryItemBase> items = new List<InventoryItemBase>();

            InventoryFolderBase folderForAppearance
                = new InventoryFolderBase(
                    UUID.Random(), archive.FolderName, account.PrincipalID,
                    -1, AppearanceFolder.ID, 1);

            InventoryService.AddFolder(folderForAppearance);

            folderForAppearance = InventoryService.GetFolder(folderForAppearance);

            try
            {
                LoadAssets(archive.AssetsMap);
                appearance = CopyWearablesAndAttachments(account.PrincipalID, UUID.Zero, appearance, folderForAppearance,
                                                         account.PrincipalID, out items);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[AvatarArchiver]: Error loading assets and items, " + ex);
            }

            MainConsole.Instance.Info("[AvatarArchive]: Loaded archive from " + fileName);
            archive.Appearance = appearance;
            return archive;
        }

        public bool SaveAvatarArchive(string fileName, UUID principalID, string folderName, UUID snapshotUUID, bool isPublic)
        {
            UserAccount account = UserAccountService.GetUserAccount(null, principalID);
            if (account == null)
            {
                MainConsole.Instance.Error("[AvatarArchive]: User not found!");
                return false;
            }

            AvatarAppearance appearance = AvatarService.GetAppearance(account.PrincipalID);
            if (appearance == null)
            {
                MainConsole.Instance.Error("[AvatarArchive] Appearance not found!");
                return false;
            }
            AvatarArchive archive = new AvatarArchive();
            archive.AssetsMap = new OSDMap();
            archive.BodyMap = appearance.Pack();
            archive.Appearance = appearance;
            archive.ItemsMap = new OSDMap();

            foreach (AvatarWearable wear in appearance.Wearables)
            {
                for (int i = 0; i < wear.Count; i++)
                {
                    WearableItem w = wear[i];
                    if (w.AssetID != UUID.Zero)
                    {
                        SaveItem(w.ItemID, ref archive);
                        SaveAsset(w.AssetID, ref archive);
                    }
                }
            }
            List<AvatarAttachment> attachments = appearance.GetAttachments();

            foreach (AvatarAttachment a in attachments.Where(a => a.AssetID != UUID.Zero))
            {
                SaveItem(a.ItemID, ref archive);
                SaveAsset(a.AssetID, ref archive);
            }

            archive.FolderName = folderName;
            archive.Snapshot = snapshotUUID;
            archive.IsPublic = isPublic;

            File.WriteAllText(fileName, OSDParser.SerializeLLSDXmlString(archive.ToOSD()));
            MainConsole.Instance.Info("[AvatarArchive] Saved archive to " + fileName);

            return true;
        }

        public List<AvatarArchive> GetAvatarArchives()
        {
            List<AvatarArchive> archives = new List<AvatarArchive>();

            foreach (string file in Directory.GetFiles(Environment.CurrentDirectory, "*.aa"))
            {
                try
                {
                    AvatarArchive archive = new AvatarArchive();
                    archive.FromOSD((OSDMap)OSDParser.DeserializeLLSDXml(File.ReadAllText(file)));
                    if (archive.IsPublic)
                        archives.Add(archive);
                }
                catch
                {
                }
            }
            return archives;
        }

        #endregion

        #region Console Commands

        protected void HandleLoadAvatarArchive(string[] cmdparams)
        {
            if (cmdparams.Length != 6)
            {
                MainConsole.Instance.Info("[AvatarArchive]: Not enough parameters!");
                return;
            }
            UserAccount account = UserAccountService.GetUserAccount(null, cmdparams[3] + " " + cmdparams[4]);
            if (account == null)
            {
                MainConsole.Instance.Info("[AvatarArchive]: No such account found!");
                return;
            }
            AvatarArchive archive = LoadAvatarArchive(cmdparams[5], account.PrincipalID);
            if (archive != null)
                AvatarService.SetAppearance(account.PrincipalID, archive.Appearance);
        }

        protected void HandleSaveAvatarArchive(string[] cmdparams)
        {
            if (cmdparams.Length < 7)
            {
                MainConsole.Instance.Info("[AvatarArchive]: Not enough parameters!");
                return;
            }
            UserAccount account = UserAccountService.GetUserAccount(null, cmdparams[3] + " " + cmdparams[4]);
            if (account == null)
            {
                MainConsole.Instance.Error("[AvatarArchive]: User not found!");
                return;
            }
            string foldername = "/";
            UUID snapshotUUID = UUID.Zero;
            bool isPublic = true;
            if (cmdparams.Length > 6)
                foldername = OSD.FromString(cmdparams[6]);
            for (int i = 7; i < cmdparams.Length; )
            {
                if (cmdparams[i].StartsWith("--private"))
                {
                    isPublic = false;
                    i++;
                }
                else if (cmdparams[i].StartsWith("--snapshot"))
                {
                    snapshotUUID = UUID.Parse(cmdparams[i+1]);
                    i += 2;
                }
            }

            SaveAvatarArchive(cmdparams[5], account.PrincipalID, foldername, snapshotUUID, isPublic);
        }

        #endregion

        #region Helpers

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

        private AvatarAppearance CopyWearablesAndAttachments(UUID destination, UUID source,
                                                             AvatarAppearance avatarAppearance,
                                                             InventoryFolderBase destinationFolder, UUID agentid,
                                                             out List<InventoryItemBase> items)
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
                        InventoryItemBase item = InventoryService.GetItem(agentid, wearable[ii].ItemID);

                        if (item != null)
                        {
                            InventoryItemBase destinationItem = InventoryService.InnerGiveInventoryItem(destination,
                                                                                                        destination,
                                                                                                        item,
                                                                                                        destinationFolder
                                                                                                            .ID,
                                                                                                        false);
                            items.Add(destinationItem);
                            MainConsole.Instance.DebugFormat("[RADMIN]: Added item {0} to folder {1}",
                                                             destinationItem.ID, destinationFolder.ID);

                            // Wear item
                            AvatarWearable newWearable = new AvatarWearable();
                            newWearable.Wear(destinationItem.ID, destinationItem.AssetID);
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
                    InventoryItemBase item = InventoryService.GetItem(source, itemID);

                    if (item != null)
                    {
                        InventoryItemBase destinationItem = InventoryService.InnerGiveInventoryItem(destination,
                                                                                                    destination, item,
                                                                                                    destinationFolder.ID,
                                                                                                    false);
                        items.Add(destinationItem);
                        MainConsole.Instance.DebugFormat("[RADMIN]: Added item {0} to folder {1}", destinationItem.ID,
                                                         destinationFolder.ID);

                        // Attach item
                        avatarAppearance.SetAttachment(attachpoint, destinationItem.ID, destinationItem.AssetID);
                        MainConsole.Instance.DebugFormat("[RADMIN]: Attached {0}", destinationItem.ID);
                    }
                    else
                    {
                        MainConsole.Instance.WarnFormat("[RADMIN]: Error transferring {0} to folder {1}", itemID,
                                                        destinationFolder.ID);
                    }
                }
            }
            return avatarAppearance;
        }

        private AvatarAppearance ConvertXMLToAvatarAppearance(OSDMap map)
        {
            AvatarAppearance appearance = new AvatarAppearance();
            appearance.Unpack(map);
            return appearance;
        }

        private void SaveAsset(UUID AssetID, ref AvatarArchive archive)
        {
            try
            {
                AssetBase asset = AssetService.Get(AssetID.ToString());
                if (asset != null)
                {
                    MainConsole.Instance.Info("[AvatarArchive]: Saving asset " + asset.ID);
                    archive.AssetsMap[asset.ID.ToString()] = asset.ToOSD();
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

        private AssetBase LoadAssetBase(OSDMap map)
        {
            AssetBase asset = new AssetBase();
            asset.FromOSD(map);
            return asset;
        }

        private void SaveItem(UUID ItemID, ref AvatarArchive archive)
        {
            InventoryItemBase saveItem = InventoryService.GetItem(UUID.Zero, ItemID);
            if (saveItem == null)
            {
                MainConsole.Instance.Warn("[AvatarArchive]: Could not find item to save: " + ItemID);
                return;
            }
            MainConsole.Instance.Info("[AvatarArchive]: Saving item " + ItemID.ToString());
            archive.ItemsMap[ItemID.ToString()] = saveItem.ToOSD();
        }

        private void LoadAssets(OSDMap assets)
        {
            foreach (KeyValuePair<string, OSD> kvp in assets)
            {
                UUID AssetID = UUID.Parse(kvp.Key);
                OSDMap assetMap = (OSDMap) kvp.Value;
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
                InventoryItemBase item = new InventoryItemBase();
                item.FromOSD((OSDMap)kvp.Value);
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
                                                         "save avatar archive <First> <Last> <Filename> <FolderNameToSaveInto> (--snapshot <UUID>) (--private)",
                                                         "Saves appearance to an avatar archive (.aa is the recommended file extension) (Note: put \"\" around the FolderName if you need more than one word. Put all attachments in BodyParts folder before saving the archive) Both --snapshot and --private are optional. --private tells any web interfaces that they cannot display this as a default avatar. --snapshot sets a picture to display on the web interface if this archive is being used as a default avatar.",
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