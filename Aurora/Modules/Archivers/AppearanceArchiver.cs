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
    public class AuroraAppearance: IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        Scene m_scene;
        private IProfileConnector ProfileFrontend;
        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            if(m_scene == null)
                m_scene = scene;
            MainConsole.Instance.Commands.AddCommand("region", false, "save AA",
                                          "save AA <First> <Last> <Filename>",
                                          "Saves appearance to an AA archive", HandleSaveAA);
            MainConsole.Instance.Commands.AddCommand("region", false, "load AA",
                                          "load AA <First> <Last> <Filename>",
                                          "Loads appearance from an AA archive", HandleLoadAA);
            scene.EventManager.OnNewClient += new EventManager.OnNewClientDelegate(EventManager_OnNewClient);
        }

        void EventManager_OnNewClient(IClientAPI client)
        {
            var GenericData = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(client.AgentId);
            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, client.AgentId);
            if (UPI == null)
            {
                ProfileFrontend.CreateNewProfile(account.PrincipalID);
            }
            if (UPI.IsNewUser && UPI.AArchiveName != " ")
            {
                LoadAA(account, UPI.AArchiveName);
            }
            UPI.IsNewUser = false;
            ProfileFrontend.UpdateUserProfile(UPI);
        }

        public void PostInitialise()
        {
            ProfileFrontend = DataManager.DataManager.IProfileConnector;
        }

        public void Close(){}

        public string Name { get { return "AuroraAppearance"; } }

        public bool IsSharedModule
        {
            get { return true; }
        }
        
        protected void HandleLoadAA(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 5)
            {
                m_log.Debug("[AA] Not enough parameters!");
                return;
            }
            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, cmdparams[2], cmdparams[3]);
            LoadAA(account, cmdparams[4]);
        }

        protected void LoadAA(UserAccount account, string FileName)
        {
            m_log.Debug("[AA] Loading AA from " + FileName);
            if (account == null)
            {
                m_log.Error("[AA] User not found!");
                return;
            }
            StreamReader reader = new StreamReader(FileName);
            List<string> file = new List<string>();
            string line = reader.ReadToEnd();
            string[] lines = line.Split('\n');
            foreach (string splitLine in lines)
            {
                file.Add(splitLine);
            }
            reader.Close();
            reader.Dispose();
            List<UUID> AttachmentUUIDs = new List<UUID>();
            AvatarAppearance appearance = ConvertXMLToAvatarAppearance(file, out AttachmentUUIDs);
            appearance.Owner = account.PrincipalID;
            List<InventoryItemBase> items = new List<InventoryItemBase>();
            if (appearance.BodyItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.BodyItem));
            if (appearance.EyesItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.EyesItem));
            if (appearance.GlovesItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.GlovesItem));
            if (appearance.HairItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.HairItem));
            if (appearance.JacketItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.JacketItem));
            if (appearance.PantsItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.PantsItem));
            if (appearance.ShirtItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.ShirtItem));
            if (appearance.ShoesItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.ShoesItem));
            if (appearance.SkinItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.SkinItem));
            if (appearance.SkirtItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.SkirtItem));
            if (appearance.SocksItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.SocksItem));
            if (appearance.UnderPantsItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.UnderPantsItem));
            if (appearance.UnderShirtItem != UUID.Zero)
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, appearance.UnderShirtItem));
            foreach (UUID uuid in AttachmentUUIDs)
            {
                items.Add(m_scene.GiveInventoryItem(
                                account.PrincipalID,
                                appearance.Owner, uuid));
            }
            ScenePresence SP;
            m_scene.TryGetScenePresence(account.PrincipalID, out SP);
            foreach (InventoryItemBase itemCopy in items)
            {
                if (itemCopy == null && SP != null)
                {
                    SP.ControllingClient.SendAgentAlertMessage("Can't find item to give. Nothing given.", false);
                    continue;
                }
                if (SP != null && !SP.IsChildAgent)
                {
                    SP.ControllingClient.SendBulkUpdateInventory(itemCopy);
                }
            }
            appearance.Owner = account.PrincipalID;
            AvatarData adata = new AvatarData(appearance);
            m_scene.AvatarService.SetAvatar(account.PrincipalID, adata);
            SP.Appearance = appearance;
            if (SP != null)
            {
                SP.SendAppearanceToOtherAgent(SP);
            }
        }

        private AvatarAppearance ConvertXMLToAvatarAppearance(List<string> file, out List<UUID> Attachment)
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
            Byte[] bytes = new byte[500];
            int i = 0;
            while (i <= 30)
            {
                newFile.RemoveAt(0);
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
            List<string> AttachmentAsset = new List<string>();
            List<string> AttachmentItem = new List<string>();
            List<string> AttachmentPoint = new List<string>();
            string texture = "";
            Attachment = new List<UUID>();
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
                    AttachmentItem.Add(newpartLine);
                    Attachment.Add(new UUID(newpartLine));
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
                    AttachmentPoint.Add(newpartLine);
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
                appearance.SetAttachment(Convert.ToInt32(AttachmentPoint[i]), new UUID(AttachmentItem[i]), new UUID(asset));
                i++;
            }
            return appearance;
        }
        protected void HandleSaveAA(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 5)
            {
                m_log.Debug("[AA] Not enough parameters!");
            }
            m_log.Debug("[AA] Saving AA to " + cmdparams[4]);
            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, cmdparams[2], cmdparams[3]);
            if (account == null)
            {
                m_log.Error("[AA] User not found!");
                return;
            }
            AvatarData avatarData = m_scene.AvatarService.GetAvatar(account.PrincipalID);
            AvatarAppearance appearance = avatarData.ToAvatarAppearance(account.PrincipalID);
            StreamWriter writer = new StreamWriter(cmdparams[4]);
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
                    writer.Write("<AI" + attachInfo["item"] + ">\n");
                    writer.Write("<AA" + attachInfo["asset"] + ">\n");
                    writer.Write("<AP" + (int)element.Key + ">\n");
                }
            }
            writer.Write("</attachments>");
            writer.Close();
            writer.Dispose();
        }
    }
}
