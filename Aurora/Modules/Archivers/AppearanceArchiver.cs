using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Serialization.External;
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
        private IInventoryService InventoryService;
        private IAssetService AssetService;
        private IUserAccountService UserAccountService;
        private IAvatarService AvatarService;
		
        public void Initialise(Nini.Config.IConfigSource source)
        {
		}

        public void AddRegion(Scene scene)
        {
            if (m_scene == null)
                m_scene = scene;

            InventoryService = m_scene.InventoryService;
            AssetService = m_scene.AssetService;
            UserAccountService = m_scene.UserAccountService;
            AvatarService = m_scene.AvatarService;

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
			LoadAvatarArchive(cmdparams[5], cmdparams[3], cmdparams[4]);
		}

        public void LoadAvatarArchive(string FileName, string First, string Last)
        {
            UserAccount account = UserAccountService.GetUserAccount(UUID.Zero, First, Last);
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

            string FolderNameToLoadInto = "";

            OSDMap map = ((OSDMap)OSDParser.DeserializeLLSDXml(file));

            OSDMap assetsMap = ((OSDMap)map["Assets"]);
            OSDMap itemsMap = ((OSDMap)map["Items"]);
            OSDMap bodyMap = ((OSDMap)map["Body"]);

            AvatarAppearance appearance = ConvertXMLToAvatarAppearance(bodyMap, out FolderNameToLoadInto);

            appearance.Owner = account.PrincipalID;

            List<InventoryItemBase> items = new List<InventoryItemBase>();

            InventoryFolderBase AppearanceFolder = InventoryService.GetFolderForType(account.PrincipalID, AssetType.Clothing);

            InventoryFolderBase folderForAppearance
                = new InventoryFolderBase(
                    UUID.Random(), FolderNameToLoadInto, account.PrincipalID,
                    -1, AppearanceFolder.ID, 1);

            InventoryService.AddFolder(folderForAppearance);

            folderForAppearance = InventoryService.GetFolder(folderForAppearance);

            try
            {
                LoadAssets(assetsMap);
                LoadItems(itemsMap, account.PrincipalID, folderForAppearance, out items);
            }
            catch (Exception ex)
            {
                m_log.Warn("[AvatarArchiver]: Error loading assets and items, " + ex.ToString());
            }
            
            //Now update the client about the new items
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
            m_log.Debug("[AvatarArchive] Loaded archive from " + FileName);
        }

        private InventoryItemBase GiveInventoryItem(UUID senderId, UUID recipient, InventoryItemBase item, InventoryFolderBase parentFolder)
        {
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
                InventoryFolderBase folder = InventoryService.GetFolderForType(recipient, (AssetType)itemCopy.AssetType);

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
			UserAccount account = UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]);
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

			AvatarData avatarData = AvatarService.GetAvatar(account.PrincipalID);
			AvatarAppearance appearance = avatarData.ToAvatarAppearance(account.PrincipalID);
			StreamWriter writer = new StreamWriter(cmdparams[5]);
            OSDMap map = new OSDMap();
            OSDMap body = new OSDMap();
            OSDMap assets = new OSDMap();
            OSDMap items = new OSDMap();
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
                if (wear.AssetID != UUID.Zero)
                {
                    SaveAsset(wear.AssetID, assets);
                    SaveItem(wear.ItemID, items);
                }
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
                    SaveAsset(IB.AssetID, assets);
                    SaveItem(UUID.Parse(attachInfo["item"].ToString()), items);
                    attachment.Add("Asset", OSD.FromUUID(IB.AssetID));
                    attachment.Add("Item", OSD.FromUUID(UUID.Parse(attachInfo["item"].ToString())));
                    attachment.Add("Point", OSD.FromInteger((int)element.Key));
                    attachmentsArray.Add(attachment);
                }
            }

            body.Add("Attachments", attachmentsArray);


            map.Add("Body", body);
            map.Add("Assets", assets);
            map.Add("Items", items);
            //Write the map
            writer.Write(OSDParser.SerializeLLSDXmlString(map));
			writer.Close();
			writer.Dispose();
			m_log.Debug("[AvatarArchive] Saved archive to " + cmdparams[5]);
		}

        private void SaveAsset(UUID AssetID, OSDMap assetMap)
        {
            AssetBase asset = AssetService.Get(AssetID.ToString());
            if (asset != null)
            {
                OSDMap assetData = new OSDMap();
                m_log.Info("[AvatarArchive]: Saving asset " + asset.ID);
                CreateMetaDataMap(asset.Metadata, assetData);
                assetData.Add("AssetData", OSD.FromBinary(asset.Data));
                assetMap.Add(asset.ID, assetData);
            }
        }

        private void CreateMetaDataMap(AssetMetadata data, OSDMap map)
        {
            map["ContentType"] = OSD.FromString(data.ContentType);
            map["CreationDate"] = OSD.FromDate(data.CreationDate);
            map["CreatorID"] = OSD.FromString(data.CreatorID);
            map["Description"] = OSD.FromString(data.Description);
            map["ID"] = OSD.FromString(data.ID);
            map["Name"] = OSD.FromString(data.Name);
            map["Type"] = OSD.FromInteger(data.Type);
        }

        private AssetBase LoadAssetBase(OSDMap map)
        {
            AssetBase asset = new AssetBase();
            asset.Data = map["AssetData"].AsBinary();

            AssetMetadata md = new AssetMetadata();
            md.ContentType = map["ContentType"].AsString();
            md.CreationDate = map["CreationDate"].AsDate();
            md.CreatorID = map["CreatorID"].AsString();
            md.Description = map["Description"].AsString();
            md.ID = map["ID"].AsString();
            md.Name = map["Name"].AsString();
            md.Type = (sbyte)map["Type"].AsInteger();

            asset.Metadata = md;
            asset.ID = md.ID;
            asset.FullID = UUID.Parse(md.ID);
            asset.Name = md.Name;
            asset.Type = md.Type;

            return asset;
        }

        private void SaveItem(UUID ItemID, OSDMap itemMap)
        {
            InventoryItemBase saveItem = InventoryService.GetItem(new InventoryItemBase(ItemID));
            m_log.Info("[AvatarArchive]: Saving item " + ItemID.ToString());
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
                m_log.Info("[AvatarArchive]: Loading asset " + AssetID.ToString());
                if (asset == null) //Don't overwrite
                {
                    asset = LoadAssetBase(assetMap);
                    AssetService.Store(asset);
                }
            }
        }

        private void LoadItems(OSDMap items, UUID OwnerID, InventoryFolderBase folderForAppearance, out List<InventoryItemBase> litems)
        {
            litems = new List<InventoryItemBase>();
            foreach (KeyValuePair<string, OSD> kvp in items)
            {
                string serialization = kvp.Value.AsString();
                InventoryItemBase item = OpenSim.Framework.Serialization.External.UserInventoryItemSerializer.Deserialize(serialization);
                m_log.Info("[AvatarArchive]: Loading item " + item.ID.ToString());
                item = GiveInventoryItem(item.CreatorIdAsUuid, OwnerID, item, folderForAppearance);
                litems.Add(item);
            }
        }
	}
}
