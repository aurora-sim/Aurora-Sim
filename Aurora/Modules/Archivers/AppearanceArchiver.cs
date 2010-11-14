using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using log4net;
using Aurora.Framework;

namespace Aurora.Modules
{
    /// <summary>
    /// This module loads/saves the avatar's appearance from/down into an "Avatar Archive", also known as an AA.
    /// </summary>
    public class AuroraAvatarAppearanceArchiver : ISharedRegionModule, IAvatarAppearanceArchiver
	{
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private Scene m_scene;
		
        public void Initialise(Nini.Config.IConfigSource source)
        {
		}

        public void AddRegion(Scene scene)
        {
            if (m_scene == null)
                m_scene = scene;

            MainConsole.Instance.Commands.AddCommand("region", false, "save avatar archive", "save avatar archive <First> <Last> <Filename> <FolderNameToSaveInto>", "Saves appearance to an avatar archive archive (Note: put \"\" around the FolderName if you need more than one word)", HandleSaveAvatarArchive);
            MainConsole.Instance.Commands.AddCommand("region", false, "load avatar archive", "load avatar archive <First> <Last> <Filename>", "Loads appearance from an avatar archive archive", HandleLoadAvatarArchive);
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {

        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise() { }

		public void Close()
		{
		}

		public string Name 
        {
			get { return "AuroraAvatarArchiver"; }
		}

		public bool IsSharedModule
        {
			get { return true; }
		}

		protected void HandleLoadAvatarArchive(string module, string[] cmdparams)
		{
			if (cmdparams.Length != 6) {
				m_log.Debug("[AvatarArchive] Not enough parameters!");
				return;
			}
			UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]);
			LoadAvatarArchive(cmdparams[5], cmdparams[3], cmdparams[4]);
		}

        public void LoadAvatarArchive(string FileName, string First, string Last)
        {
            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, First, Last);
            m_log.Debug("[AvatarArchive] Loading archive from " + FileName);
            if (account == null)
            {
                m_log.Error("[AvatarArchive] User not found!");
                return;
            }
            StreamReader reader = new StreamReader(FileName);

            string file = reader.ReadToEnd();

            reader.Close();
            reader.Dispose();
            ScenePresence SP;
            m_scene.TryGetScenePresence(account.PrincipalID, out SP);
            if (SP == null)
                return; //Bad people!
            SP.ControllingClient.SendAlertMessage("Appearance loading in progress...");

            //Clear out the old appearance
            SP.Appearance.ClearWearables();
            SP.Appearance.ClearAttachments();
            SP.SendWearables();

            string FolderNameToLoadInto = "";
            List<UUID> AttachmentUUIDs = new List<UUID>();
            List<int> AttachmentPoints = new List<int>();
            List<UUID> AttachmentAssets = new List<UUID>();

            AvatarAppearance appearance = ConvertXMLToAvatarAppearance(file, out AttachmentUUIDs, out AttachmentPoints, out AttachmentAssets, out FolderNameToLoadInto);

            appearance.Owner = account.PrincipalID;

            List<InventoryItemBase> items = new List<InventoryItemBase>();

            InventoryFolderBase AppearanceFolder = m_scene.InventoryService.GetFolderForType(account.PrincipalID, AssetType.Clothing);

            UUID newFolderId = UUID.Random();

            InventoryFolderBase folderForAppearance
                = new InventoryFolderBase(
                    newFolderId, FolderNameToLoadInto, account.PrincipalID,
                    -1, AppearanceFolder.ID, 1);

            m_scene.InventoryService.AddFolder(folderForAppearance);
            folderForAppearance = m_scene.InventoryService.GetFolder(folderForAppearance);

            #region Appearance setup

            if (appearance.BodyItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.BodyItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.BodyItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.BodyItem = IB.ID;
                }
            }

            if (appearance.EyesItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.EyesItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.EyesItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.EyesItem = IB.ID;
                }
            }

            if (appearance.GlovesItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.GlovesItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.GlovesItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.GlovesItem = IB.ID;
                }
            }

            if (appearance.HairItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.HairItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.HairItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.HairItem = IB.ID;
                }
            }

            if (appearance.JacketItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.JacketItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.JacketItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.JacketItem = IB.ID;
                }
            }

            if (appearance.PantsItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.PantsItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.PantsItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.PantsItem = IB.ID;
                }
            }

            if (appearance.ShirtItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.ShirtItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.ShirtItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.ShirtItem = IB.ID;
                }
            }

            if (appearance.ShoesItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.ShoesItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.ShoesItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.ShoesItem = IB.ID;
                }
            }

            if (appearance.SkinItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.SkinItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.SkinItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.SkinItem = IB.ID;
                }
            }

            if (appearance.SkirtItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.SkirtItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.SkirtItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.SkirtItem = IB.ID;
                }
            }

            if (appearance.SocksItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.SocksItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.SocksItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.SocksItem = IB.ID;
                }
            }

            if (appearance.UnderPantsItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.UnderPantsItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.UnderPantsItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.UnderPantsItem = IB.ID;
                }
            }

            if (appearance.UnderShirtItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.UnderShirtItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.UnderShirtItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.UnderShirtItem = IB.ID;
                }
            }

            if (appearance.AlphaItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.AlphaItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.AlphaItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.AlphaItem = IB.ID;
                }
            }

            if (appearance.TattooItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.TattooItem));
                if (IB != null)
                {
                    IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.TattooItem, folderForAppearance);
                    IB.Folder = folderForAppearance.ID;
                    items.Add(IB);
                    appearance.TattooItem = IB.ID;
                }
            }

            foreach (UUID uuid in AttachmentUUIDs)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(uuid));
                if (IB == null)
                    continue;
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, uuid, folderForAppearance);
                items.Add(IB);
            }

            #endregion
            SP.ControllingClient.SendBulkUpdateInventory(folderForAppearance);
            foreach (InventoryItemBase itemCopy in items)
            {
                if (itemCopy == null)
                {
                    SP.ControllingClient.SendAgentAlertMessage("Can't find item to give. Nothing given.", false);
                    continue;
                }
                if (!SP.IsChildAgent)
                {
                    SP.ControllingClient.SendBulkUpdateInventory(itemCopy);
                }
            }

            appearance.Owner = account.PrincipalID;
            AvatarData adata = new AvatarData(appearance);
            m_scene.AvatarService.SetAvatar(account.PrincipalID, adata);

            SP.Appearance = appearance;
            SP.SendAppearanceToOtherAgent(SP);
            SP.SendWearables();
            SP.SendAppearanceToAllOtherAgents();

            if (appearance.Texture != null)
            {
                for (int i = 0; i < appearance.Texture.FaceTextures.Length; i++)
                {
                    Primitive.TextureEntryFace face = (appearance.Texture.FaceTextures[i]);

                    if (face != null && face.TextureID != AppearanceManager.DEFAULT_AVATAR_TEXTURE)
                    {
                        m_log.Warn("[APPEARANCE]: Missing baked texture " + face.TextureID + " (" + i + ") for avatar " + this.Name);
                        SP.ControllingClient.SendRebakeAvatarTextures(face.TextureID);
                    }
                }
            }
            m_log.Debug("[AvatarArchive] Loaded archive from " + FileName);
        }

        private InventoryItemBase GiveInventoryItem(UUID senderId, UUID recipient, UUID itemId, InventoryFolderBase parentFolder)
        {
            InventoryItemBase item = new InventoryItemBase(itemId, senderId);
            item = m_scene.InventoryService.GetItem(item);


            InventoryItemBase itemCopy = new InventoryItemBase();
            itemCopy.Owner = recipient;
            itemCopy.CreatorId = item.CreatorId;
            itemCopy.ID = UUID.Random();
            itemCopy.AssetID = item.AssetID;
            itemCopy.Description = item.Description;
            itemCopy.Name = item.Name;
            itemCopy.AssetType = item.AssetType;
            itemCopy.InvType = item.InvType;
            itemCopy.Folder = UUID.Zero;

            //Give full permissions for them
            itemCopy.NextPermissions = (uint)PermissionMask.All;
            itemCopy.GroupPermissions = (uint)PermissionMask.All;
            itemCopy.EveryOnePermissions = (uint)PermissionMask.All;
            itemCopy.CurrentPermissions = (uint)PermissionMask.All;

            if (parentFolder == null)
            {
                InventoryFolderBase folder = m_scene.InventoryService.GetFolderForType(recipient, (AssetType)itemCopy.AssetType);

                if (folder != null)
                    itemCopy.Folder = folder.ID;
                else
                {
                    InventoryFolderBase root = m_scene.InventoryService.GetRootFolder(recipient);

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

            m_scene.InventoryService.AddItem(itemCopy);
            return itemCopy;
        }

        private AvatarAppearance ConvertXMLToAvatarAppearance(string file, out List<UUID> AttachmentIDs, out List<int> AttachmentPoints, out List<UUID> AttachmentAsset, out string FolderNameToPlaceAppearanceIn)
        {
            AttachmentPoints = new List<int>();
            AttachmentAsset = new List<UUID>();
            AttachmentIDs = new List<UUID>();

            AvatarAppearance appearance = new AvatarAppearance();

            OSDMap map = (OSDMap)((OSDMap)OSDParser.DeserializeLLSDXml(file))["Body"];

            appearance.AvatarHeight = (float)map["AvatarHeight"].AsReal();
            appearance.BodyAsset = map["BodyAsset"].AsUUID();
            appearance.BodyItem = map["BodyItem"].AsUUID();
            appearance.EyesAsset = map["EyesAsset"].AsUUID();
            appearance.EyesItem = map["EyesItem"].AsUUID();
            appearance.GlovesAsset = map["GlovesAsset"].AsUUID();
            appearance.GlovesItem = map["GlovesItem"].AsUUID();
            appearance.HairAsset = map["HairAsset"].AsUUID();
            appearance.HairItem = map["HairItem"].AsUUID();
            //Skip Hip Offset
            appearance.JacketAsset = map["JacketAsset"].AsUUID();
            appearance.JacketItem = map["JacketItem"].AsUUID();
            appearance.Owner = map["Owner"].AsUUID();
            appearance.PantsAsset = map["PantsAsset"].AsUUID();
            appearance.PantsItem = map["PantsItem"].AsUUID();
            appearance.Serial = map["Serial"].AsInteger();
            appearance.ShirtAsset = map["ShirtAsset"].AsUUID();
            appearance.ShirtItem = map["ShirtItem"].AsUUID();
            appearance.ShoesAsset = map["ShoesAsset"].AsUUID();
            appearance.ShoesItem = map["ShoesItem"].AsUUID();
            appearance.SkinAsset = map["SkinAsset"].AsUUID();
            appearance.SkinItem = map["SkinItem"].AsUUID();
            appearance.SkirtAsset = map["SkirtAsset"].AsUUID();
            appearance.SkirtItem = map["SkirtItem"].AsUUID();
            appearance.SocksAsset = map["SocksAsset"].AsUUID();
            appearance.SocksItem = map["SocksItem"].AsUUID();
            appearance.UnderPantsAsset = map["UnderPantsAsset"].AsUUID();
            appearance.UnderPantsItem = map["UnderPantsItem"].AsUUID();
            appearance.UnderShirtAsset = map["UnderShirtAsset"].AsUUID();
            appearance.UnderShirtItem = map["UnderShirtItem"].AsUUID();
            appearance.TattooAsset = map["TattooAsset"].AsUUID();
            appearance.TattooItem = map["TattooItem"].AsUUID();
            appearance.AlphaAsset = map["AlphaAsset"].AsUUID();
            appearance.AlphaItem = map["AlphaItem"].AsUUID();
            FolderNameToPlaceAppearanceIn = map["FolderName"].AsString();
            appearance.VisualParams = map["VisualParams"].AsBinary();

            OSDArray wearables = (OSDArray)map["AvatarWearables"];
            List<AvatarWearable> AvatarWearables = new List<AvatarWearable>();
            foreach (OSD o in wearables)
            {
                OSDMap wearable = (OSDMap)o;
                AvatarWearable wear = new AvatarWearable();
                wear.AssetID = wearable["Asset"].AsUUID();
                wear.ItemID = wearable["Item"].AsUUID();
                AvatarWearables.Add(wear);
            }
            appearance.Wearables = AvatarWearables.ToArray();

            appearance.Texture = Primitive.TextureEntry.FromOSD(map["Texture"]);

            OSDArray attachmentsArray = (OSDArray)map["Attachments"];
            foreach (OSD o in wearables)
            {
                OSDMap attachment = (OSDMap)o;
                UUID Asset = attachment["Asset"].AsUUID();
                UUID Item = attachment["Item"].AsUUID();
                int AttachmentPoint = attachment["Point"].AsInteger();

                AttachmentAsset.Add(Asset);
                AttachmentIDs.Add(Item);
                AttachmentPoints.Add(AttachmentPoint);

                appearance.SetAttachment(AttachmentPoint, Item, Asset);
            }
            return appearance;
        }

        protected void HandleSaveAvatarArchive(string module, string[] cmdparams)
		{
			if (cmdparams.Length != 7)
            {
				m_log.Debug("[AvatarArchive] Not enough parameters!");
			}
			UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]);
			if (account == null) 
            {
				m_log.Error("[AvatarArchive] User not found!");
				return;
			}

            ScenePresence SP;
            m_scene.TryGetScenePresence(account.PrincipalID, out SP);
            if (SP == null)
                return; //Bad people!
            SP.ControllingClient.SendAlertMessage("Appearance saving in progress...");

			AvatarData avatarData = m_scene.AvatarService.GetAvatar(account.PrincipalID);
			AvatarAppearance appearance = avatarData.ToAvatarAppearance(account.PrincipalID);
			StreamWriter writer = new StreamWriter(cmdparams[5]);
            OSDMap map = new OSDMap();
            OSDMap body = new OSDMap();
            body.Add("AvatarHeight", OSD.FromReal(appearance.AvatarHeight));
            body.Add("BodyAsset", OSD.FromUUID(appearance.BodyAsset));
            body.Add("BodyItem", OSD.FromUUID(appearance.BodyItem));
            body.Add("EyesAsset", OSD.FromUUID(appearance.EyesAsset));
            body.Add("EyesItem", OSD.FromUUID(appearance.EyesItem));
            body.Add("GlovesAsset", OSD.FromUUID(appearance.GlovesAsset));
            body.Add("GlovesItem", OSD.FromUUID(appearance.GlovesItem));
            body.Add("HairAsset", OSD.FromUUID(appearance.HairAsset));
            body.Add("HairItem", OSD.FromUUID(appearance.HairItem));
            body.Add("HipOffset", OSD.FromReal(appearance.HipOffset));
            body.Add("JacketAsset", OSD.FromUUID(appearance.JacketAsset));
            body.Add("JacketItem", OSD.FromUUID(appearance.JacketItem));
            body.Add("Owner", OSD.FromUUID(appearance.Owner));
            body.Add("PantsAsset", OSD.FromUUID(appearance.PantsAsset));
            body.Add("Serial", OSD.FromInteger(appearance.Serial));
            body.Add("ShirtAsset", OSD.FromUUID(appearance.ShirtAsset));
            body.Add("ShirtItem", OSD.FromUUID(appearance.ShirtItem));
            body.Add("ShoesAsset", OSD.FromUUID(appearance.ShoesAsset));
            body.Add("ShoesItem", OSD.FromUUID(appearance.ShoesItem));
            body.Add("SkinAsset", OSD.FromUUID(appearance.SkinAsset));
            body.Add("SkirtAsset", OSD.FromUUID(appearance.SkirtAsset));
            body.Add("SkirtItem", OSD.FromUUID(appearance.SkirtItem));
            body.Add("SocksAsset", OSD.FromUUID(appearance.SocksAsset));
            body.Add("SocksItem", OSD.FromUUID(appearance.SocksItem));
            body.Add("UnderPantsAsset", OSD.FromUUID(appearance.UnderPantsAsset));
            body.Add("UnderPantsItem", OSD.FromUUID(appearance.UnderPantsItem));
            body.Add("UnderShirtAsset", OSD.FromUUID(appearance.UnderShirtAsset));
            body.Add("UnderShirtItem", OSD.FromUUID(appearance.UnderShirtItem));
            body.Add("TattooAsset", OSD.FromUUID(appearance.TattooAsset));
            body.Add("TattooItem", OSD.FromUUID(appearance.TattooItem));
            body.Add("AlphaAsset", OSD.FromUUID(appearance.AlphaAsset));
            body.Add("AlphaItem", OSD.FromUUID(appearance.AlphaItem));
            body.Add("FolderName", OSD.FromString(cmdparams[6]));
            body.Add("VisualParams", OSD.FromBinary(appearance.VisualParams));

            OSDArray wearables = new OSDArray();
            foreach (AvatarWearable wear in appearance.Wearables)
            {
                OSDMap wearable = new OSDMap();
                wearable.Add("Asset", wear.AssetID);
                wearable.Add("Item", wear.ItemID);
                wearables.Add(wearable);
            }
            body.Add("AvatarWearables", wearables);

            body.Add("Texture", appearance.Texture.GetOSD());

            OSDArray attachmentsArray = new OSDArray();
            
            Hashtable attachments = appearance.GetAttachments();
            if (attachments != null)
            {
                foreach (DictionaryEntry element in attachments)
                {
                    Hashtable attachInfo = (Hashtable)element.Value;
                    InventoryItemBase IB = new InventoryItemBase(UUID.Parse(attachInfo["item"].ToString()));
                    OSDMap attachment = new OSDMap();
                    attachment.Add("Asset", OSD.FromUUID(IB.AssetID));
                    attachment.Add("Item", OSD.FromUUID(UUID.Parse(attachInfo["item"].ToString())));
                    attachment.Add("Point", OSD.FromInteger((int)element.Key));
                    attachmentsArray.Add(attachment);
                }
            }

            body.Add("Attachments", attachmentsArray);


            map.Add("Body", body);
            //Write the map
            writer.Write(OSDParser.SerializeLLSDXmlString(map));
			writer.Close();
			writer.Dispose();
			m_log.Debug("[AvatarArchive] Saved archive to " + cmdparams[5]);
		}
	}
}
