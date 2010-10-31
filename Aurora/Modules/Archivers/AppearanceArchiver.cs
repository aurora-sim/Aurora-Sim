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
using log4net;
using Aurora.Framework;

namespace Aurora.Modules
{
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

            string line = reader.ReadToEnd();
            string[] lines = line.Split('\n');
            List<string> file = new List<string>(lines);

            reader.Close();
            reader.Dispose();
            ScenePresence SP;
            m_scene.TryGetScenePresence(account.PrincipalID, out SP);
            if (SP == null)
                return; //Bad people!
            SP.ControllingClient.SendAlertMessage("Appearance loading in progress...");

            SP.Appearance.ClearWearables();
            SP.Appearance.ClearAttachments();
            SP.SendWearables();

            string FolderNameToLoadInto = "";
            List<UUID> AttachmentUUIDs = new List<UUID>();
            List<string> AttachmentPoints = new List<string>();
            List<string> AttachmentAssets = new List<string>();

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
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.BodyItem, folderForAppearance);
                items.Add(IB);
                appearance.BodyItem = IB.ID;
            }

            if (appearance.EyesItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.EyesItem));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.EyesItem, folderForAppearance);
                items.Add(IB);
                appearance.EyesItem = IB.ID;
            }

            if (appearance.GlovesItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.GlovesItem));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.GlovesItem, folderForAppearance);
                items.Add(IB);
                appearance.GlovesItem = IB.ID;
            }

            if (appearance.HairItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.HairItem));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.HairItem, folderForAppearance);
                items.Add(IB);
                appearance.HairItem = IB.ID;
            }

            if (appearance.JacketItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.JacketItem));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.JacketItem, folderForAppearance);
                items.Add(IB);
                appearance.JacketItem = IB.ID;
            }

            if (appearance.PantsItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.PantsItem));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.PantsItem, folderForAppearance);
                items.Add(IB);
                appearance.PantsItem = IB.ID;
            }

            if (appearance.ShirtItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.ShirtItem));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.ShirtItem, folderForAppearance);
                items.Add(IB);
                appearance.ShirtItem = IB.ID;
            }

            if (appearance.ShoesItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.ShoesItem));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.ShoesItem, folderForAppearance);
                items.Add(IB);
                appearance.ShoesItem = IB.ID;
            }

            if (appearance.SkinItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.SkinItem));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.SkinItem, folderForAppearance);
                items.Add(IB);
                appearance.SkinItem = IB.ID;
            }

            if (appearance.SkirtItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.SkirtItem));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.SkirtItem, folderForAppearance);
                items.Add(IB);
                appearance.SkirtItem = IB.ID;
            }

            if (appearance.SocksItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.SocksItem));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.SocksItem, folderForAppearance);
                items.Add(IB);
                appearance.SocksItem = IB.ID;
            }

            if (appearance.UnderPantsItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.UnderPantsItem));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.UnderPantsItem, folderForAppearance);
                items.Add(IB);
                appearance.UnderPantsItem = IB.ID;
            }

            if (appearance.UnderShirtItem != UUID.Zero)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(appearance.UnderShirtItem));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, appearance.UnderShirtItem, folderForAppearance);
                items.Add(IB);
                appearance.UnderShirtItem = IB.ID;
            }

            appearance.ClearAttachments(); //Clear so that we can rebuild
            int i = 0;
            foreach (UUID uuid in AttachmentUUIDs)
            {
                InventoryItemBase IB = m_scene.InventoryService.GetItem(new InventoryItemBase(uuid));
                IB = GiveInventoryItem(IB.CreatorIdAsUuid, appearance.Owner, uuid, folderForAppearance);
                items.Add(IB);
                appearance.SetAttachment(int.Parse(AttachmentPoints[i]), IB.ID, UUID.Parse(AttachmentAssets[i]));
                i++;
            }

            #endregion

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
                for (i = 0; i < appearance.Texture.FaceTextures.Length; i++)
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

            if (m_scene.InventoryService.AddItem(itemCopy))
            {
                IInventoryAccessModule invAccess = m_scene.RequestModuleInterface<IInventoryAccessModule>();
                if (invAccess != null)
                    invAccess.TransferInventoryAssets(itemCopy, senderId, recipient);
            }
            return itemCopy;
        }

        private AvatarAppearance ConvertXMLToAvatarAppearance(List<string> file, out List<UUID> AttachmentIDs, out List<string> AttachmentPoints, out List<string> AttachmentAsset, out string FolderNameToPlaceAppearanceIn)
        {
            AvatarAppearance appearance = new AvatarAppearance();
            List<string> newFile = new List<string>();
            foreach (string line in file)
            {
                string newLine = line.TrimStart('<');
                newFile.Add(newLine.TrimEnd('>'));
            }
            appearance.AvatarHeight = Convert.ToInt32(newFile[1]);
            appearance.BodyAsset = new UUID(newFile[2]);
            appearance.BodyItem = new UUID(newFile[3]);
            appearance.EyesAsset = new UUID(newFile[4]);
            appearance.EyesItem = new UUID(newFile[5]);
            appearance.GlovesAsset = new UUID(newFile[6]);
            appearance.GlovesItem = new UUID(newFile[7]);
            appearance.HairAsset = new UUID(newFile[8]);
            appearance.HairItem = new UUID(newFile[9]);
            //Skip Hip Offset
            appearance.JacketAsset = new UUID(newFile[11]);
            appearance.JacketItem = new UUID(newFile[12]);
            appearance.Owner = new UUID(newFile[13]);
            appearance.PantsAsset = new UUID(newFile[14]);
            appearance.PantsItem = new UUID(newFile[15]);
            appearance.Serial = Convert.ToInt32(newFile[16]);
            appearance.ShirtAsset = new UUID(newFile[17]);
            appearance.ShirtItem = new UUID(newFile[18]);
            appearance.ShoesAsset = new UUID(newFile[19]);
            appearance.ShoesItem = new UUID(newFile[20]);
            appearance.SkinAsset = new UUID(newFile[21]);
            appearance.SkinItem = new UUID(newFile[22]);
            appearance.SkirtAsset = new UUID(newFile[23]);
            appearance.SkirtItem = new UUID(newFile[24]);
            appearance.SocksAsset = new UUID(newFile[25]);
            appearance.SocksItem = new UUID(newFile[26]);
            //appearance.Texture = new Primitive.TextureEntry(newFile[27],);
            appearance.UnderPantsAsset = new UUID(newFile[27]);
            appearance.UnderPantsItem = new UUID(newFile[28]);
            appearance.UnderShirtAsset = new UUID(newFile[29]);
            appearance.UnderShirtItem = new UUID(newFile[30]);
            FolderNameToPlaceAppearanceIn = newFile[31];

            Byte[] bytes = new byte[218];
            int i = 0;
            while (i <= 31)
            {
                newFile.RemoveAt(0); //Clear out the already processed parts
                i++;
            }
            i = 0;
            if (newFile[0] == "VisualParams")
            {
                foreach (string partLine in newFile)
                {
                    if (partLine.StartsWith("/VP"))
                    {
                        string newpartLine = "";
                        newpartLine = partLine.Replace("/VP", "");
                        bytes[i] = Convert.ToByte(newpartLine);
                        i++;
                    }
                }
            }
            appearance.VisualParams = bytes;
            List<string> WearableAsset = new List<string>();
            List<string> WearableItem = new List<string>();
            string texture = "";
            AttachmentIDs = new List<UUID>();
            AttachmentPoints = new List<string>();
            AttachmentAsset = new List<string>();
            foreach (string partLine in newFile)
            {
                if (partLine.StartsWith("WA"))
                {
                    string newpartLine = "";
                    newpartLine = partLine.Replace("WA", "");
                    WearableAsset.Add(newpartLine);
                }
                if (partLine.StartsWith("WI"))
                {
                    string newpartLine = "";
                    newpartLine = partLine.Replace("WI", "");
                    WearableItem.Add(newpartLine);
                }
                if (partLine.StartsWith("TEXTURE"))
                {
                    string newpartLine = "";
                    newpartLine = partLine.Replace("TEXTURE", "");
                    texture = newpartLine;
                }
                if (partLine.StartsWith("AI"))
                {
                    string newpartLine = "";
                    newpartLine = partLine.Replace("AI", "");
                    AttachmentIDs.Add(new UUID(newpartLine));
                }
                if (partLine.StartsWith("AA"))
                {
                    string newpartLine = "";
                    newpartLine = partLine.Replace("AA", "");
                    AttachmentAsset.Add(newpartLine);
                }
                if (partLine.StartsWith("AP"))
                {
                    string newpartLine = "";
                    newpartLine = partLine.Replace("AP", "");
                    AttachmentPoints.Add(newpartLine);
                }
            }
            //byte[] textureBytes = Utils.StringToBytes(texture);
            //appearance.Texture = new Primitive.TextureEntry(textureBytes, 0, textureBytes.Length);
            AvatarWearable[] wearables = new AvatarWearable[13];
            i = 0;
            foreach (string asset in WearableAsset)
            {
                AvatarWearable wearable = new AvatarWearable(new UUID(asset), new UUID(WearableItem[i]));
                wearables[i] = wearable;
                i++;
            }
            i = 0;
            foreach (string asset in AttachmentAsset)
            {
                appearance.SetAttachment(Convert.ToInt32(AttachmentPoints[i]), AttachmentIDs[i], new UUID(asset));
                i++;
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
			writer.Write("<avatar>\n");
			writer.Write("<" + appearance.AvatarHeight + ">\n");
			writer.Write("<" + appearance.BodyAsset + ">\n");
			writer.Write("<" + appearance.BodyItem + ">\n");
			writer.Write("<" + appearance.EyesAsset + ">\n");
			writer.Write("<" + appearance.EyesItem + ">\n");
			writer.Write("<" + appearance.GlovesAsset + ">\n");
			writer.Write("<" + appearance.GlovesItem + ">\n");
			writer.Write("<" + appearance.HairAsset + ">\n");
			writer.Write("<" + appearance.HairItem + ">\n");
			writer.Write("<" + appearance.HipOffset + ">\n");
			writer.Write("<" + appearance.JacketAsset + ">\n");
			writer.Write("<" + appearance.JacketItem + ">\n");
			writer.Write("<" + appearance.Owner + ">\n");
			writer.Write("<" + appearance.PantsAsset + ">\n");
			writer.Write("<" + appearance.PantsItem + ">\n");
			writer.Write("<" + appearance.Serial + ">\n");
			writer.Write("<" + appearance.ShirtAsset + ">\n");
			writer.Write("<" + appearance.ShirtItem + ">\n");
			writer.Write("<" + appearance.ShoesAsset + ">\n");
			writer.Write("<" + appearance.ShoesItem + ">\n");
			writer.Write("<" + appearance.SkinAsset + ">\n");
			writer.Write("<" + appearance.SkinItem + ">\n");
			writer.Write("<" + appearance.SkirtAsset + ">\n");
			writer.Write("<" + appearance.SkirtItem + ">\n");
			writer.Write("<" + appearance.SocksAsset + ">\n");
			writer.Write("<" + appearance.SocksItem + ">\n");
			writer.Write("<" + appearance.UnderPantsAsset + ">\n");
			writer.Write("<" + appearance.UnderPantsItem + ">\n");
			writer.Write("<" + appearance.UnderShirtAsset + ">\n");
			writer.Write("<" + appearance.UnderShirtItem + ">\n");
			writer.Write("<VisualParams>\n");
			foreach (Byte Byte in appearance.VisualParams)
            {
				writer.Write("</VP" + Convert.ToString(Byte) + ">\n");
			}
			writer.Write("</VisualParams>\n");
			writer.Write("<wearables>\n");
			foreach (AvatarWearable wear in appearance.Wearables)
            {
				writer.Write("<WA" + wear.AssetID + ">\n");
				writer.Write("<WI" + wear.ItemID + ">\n");
			}
			writer.Write("</wearables>\n");
			writer.Write("<TEXTURE" + appearance.Texture.ToString().Replace("\n", "") + "TEXTURE>\n");
			writer.Write("</avatar>");
			Hashtable attachments = appearance.GetAttachments();
			writer.Write("<attachments>\n");
			if (attachments != null)
            {
				foreach (DictionaryEntry element in attachments) 
                {
					Hashtable attachInfo = (Hashtable)element.Value;
                    InventoryItemBase IB = new InventoryItemBase(UUID.Parse(attachInfo["item"].ToString()));
                    writer.Write("<AI" + attachInfo["item"] + ">\n");
                    writer.Write("<AA" + IB.AssetID + ">\n");
					writer.Write("<AP" + (int)element.Key + ">\n");
				}
			}
			writer.Write("</attachments>");
			writer.Close();
			writer.Dispose();
			m_log.Debug("[AvatarArchive] Saved archive to " + cmdparams[5]);
		}
	}
}
