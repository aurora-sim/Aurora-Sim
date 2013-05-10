using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Servers.HttpServer.Interfaces;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Assets;
using Aurora.Framework.Services.ClassHelpers.Inventory;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TextureData = OpenMetaverse.AppearanceManager.TextureData;
using WearableData = OpenMetaverse.AppearanceManager.WearableData;

namespace Aurora.Services
{
    public class AgentAppearanceService : IService, IAgentAppearanceService
    {
        public string ServiceURI { get; protected set; }
        protected IRegistryCore m_registry;
        protected bool m_enabled = false;
        protected IAssetService m_assetService;
        protected IAvatarService m_avatarService;
        protected IInventoryService m_inventoryService;

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig ssaConfig = config.Configs["SSAService"];
            uint port = 8011;
            if (ssaConfig != null)
            {
                m_enabled = ssaConfig.GetBoolean("Enabled", m_enabled);
                port = ssaConfig.GetUInt("Port", port);
            }
            if (!m_enabled)
                return;
            IHttpServer server = registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);
            ServiceURI = server.ServerURI + "/";
            server.AddStreamHandler(new GenericStreamHandler("GET", "/texture/", GetBakedTexture));
            registry.RegisterModuleInterface<IAgentAppearanceService>(this);
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (!m_enabled) return;
            m_assetService = registry.RequestModuleInterface<IAssetService>();
            m_inventoryService = registry.RequestModuleInterface<IInventoryService>();
            m_avatarService = registry.RequestModuleInterface<IAvatarService>();
            m_assetService = registry.RequestModuleInterface<IAssetService>();

            MainConsole.Instance.Commands.AddCommand("bake avatar", "bake avatar", "Bakes an avatar's appearance", BakeAvatar);
        }

        public void FinishedStartup()
        {
            if (!m_enabled) return;
            IGridInfo gridInfo = m_registry.RequestModuleInterface<IGridInfo>();
            if(gridInfo != null)
                gridInfo.AgentAppearanceURI = ServiceURI;
        }

        private byte[] GetBakedTexture(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            string[] req = path.Split('/');
            UUID avID = UUID.Parse(req[2]);
            //string type = req[3];
            UUID textureID = UUID.Parse(req[4]);

            //IAvatarService avService = m_registry.RequestModuleInterface<IAvatarService>();
            //Aurora.Framework.ClientInterfaces.AvatarAppearance appearance = avService.GetAppearance(avID);
            //AvatarTextureIndex textureIndex = AppearanceManager.BakeTypeToAgentTextureIndex((BakeType)Enum.Parse(typeof(BakeType), type, true));
            //AssetBase texture = m_assetService.Get(appearance.Texture.FaceTextures[(int)textureIndex].TextureID.ToString());
            AssetBase texture = m_assetService.Get(textureID.ToString());
            if (texture == null)
            {
                MainConsole.Instance.WarnFormat("[AgentAppearanceService]: Could not find baked texture {0} for {1}", textureID, avID);
                return new byte[0];
            }
            MainConsole.Instance.InfoFormat("[AgentAppearanceService]: Found baked texture {0} for {1}", textureID, avID);
            // Full content request
            httpResponse.StatusCode = (int)System.Net.HttpStatusCode.OK;
            httpResponse.ContentType = texture.TypeString;
            return texture.Data;
        }

        private TextureData[] Textures = new TextureData[(int)AvatarTextureIndex.NumberOfEntries];
        //private List<UUID> m_lastInventoryItemIDs = new List<UUID>();

        private void BakeAvatar(string[] cmd)
        {
            string name = MainConsole.Instance.Prompt("Name: ");
            IUserAccountService uas = m_registry.RequestModuleInterface<IUserAccountService>();
            if (uas != null)
            {
                UserAccount account = uas.GetUserAccount(null, name);
                if (account != null)
                    BakeAppearance(account.PrincipalID, 0);
                else
                    MainConsole.Instance.Warn("No such user found");
            }
        }

        public AvatarAppearance BakeAppearance(UUID agentID, int cof_version)
        {
            AvatarAppearance appearance = m_avatarService.GetAppearance(agentID);
            List<BakeType> pendingBakes = new List<BakeType>();
            InventoryFolderBase cof = m_inventoryService.GetFolderForType(agentID, InventoryType.Unknown, AssetType.CurrentOutfitFolder);
            if (cof.Version < cof_version)
            {
                int i = 0;
                while (i < 10)
                {
                    cof = m_inventoryService.GetFolderForType(agentID, InventoryType.Unknown, AssetType.CurrentOutfitFolder);
                    System.Threading.Thread.Sleep(100);
                    if (cof.Version >= cof_version)
                        break;
                    i++;
                }
            }
            List<InventoryItemBase> items = m_inventoryService.GetFolderItems(agentID, cof.ID);
            foreach (InventoryItemBase itm in items)
                MainConsole.Instance.Warn("[ServerSideAppearance]: Baking " + itm.Name);

            for (int i = 0; i < Textures.Length; i++)
                Textures[i] = new TextureData();

            WearableData alphaWearable = null;
            List<UUID> currentItemIDs = new List<UUID>();
            foreach (InventoryItemBase itm in items)
            {
                if (itm.AssetType == (int)AssetType.Link)
                {
                    UUID assetID = m_inventoryService.GetItemAssetID(agentID, itm.AssetID);
                    if (appearance.Wearables.Any((w) => w.GetItem(assetID) != UUID.Zero))
                    {
                        currentItemIDs.Add(assetID);
                        //if (m_lastInventoryItemIDs.Contains(assetID))
                        //    continue;
                        WearableData wearable = new WearableData();
                        AssetBase asset = m_assetService.Get(assetID.ToString());
                        if (asset != null && asset.TypeAsset != AssetType.Object)
                        {
                            wearable.Asset = new AssetClothing(assetID, asset.Data);
                            if (wearable.Asset.Decode())
                            {
                                wearable.AssetID = assetID;
                                wearable.AssetType = wearable.Asset.AssetType;
                                wearable.WearableType = wearable.Asset.WearableType;
                                wearable.ItemID = itm.AssetID;
                                if (wearable.WearableType == WearableType.Alpha)
                                {
                                    alphaWearable = wearable;
                                    continue;
                                }
                                AppearanceManager.DecodeWearableParams(wearable, ref Textures);
                            }
                        }
                    }
                    else
                    {
                    }
                }
            }
            /*foreach (UUID id in m_lastInventoryItemIDs)
            {
                if (!currentItemIDs.Contains(id))
                {
                    OpenMetaverse.AppearanceManager.WearableData wearable = new OpenMetaverse.AppearanceManager.WearableData();
                    AssetBase asset = m_assetService.Get(id.ToString());
                    if (asset != null && asset.TypeAsset != AssetType.Object)
                    {
                        wearable.Asset = new AssetClothing(id, asset.Data);
                        if (wearable.Asset.Decode())
                        {
                            foreach (KeyValuePair<AvatarTextureIndex, UUID> entry in wearable.Asset.Textures)
                            {
                                int i = (int)entry.Key;

                                Textures[i].Texture = null;
                                Textures[i].TextureID = UUID.Zero;
                            }
                        }
                    }
                }
            }*/
            //m_lastInventoryItemIDs = currentItemIDs;
            for (int i = 0; i < Textures.Length; i++)
            {
                /*if (Textures[i].TextureID == UUID.Zero)
                    continue;
                if (Textures[i].Texture != null)
                    continue;*/
                AssetBase asset = m_assetService.Get(Textures[i].TextureID.ToString());
                if (asset != null)
                {
                    Textures[i].Texture = new AssetTexture(Textures[i].TextureID, asset.Data);
                    Textures[i].Texture.Decode();
                }
            }

            for (int bakedIndex = 0; bakedIndex < AppearanceManager.BAKED_TEXTURE_COUNT; bakedIndex++)
            {
                AvatarTextureIndex textureIndex = AppearanceManager.BakeTypeToAgentTextureIndex((BakeType)bakedIndex);

                if (Textures[(int)textureIndex].TextureID == UUID.Zero)
                {
                    // If this is the skirt layer and we're not wearing a skirt then skip it
                    if (bakedIndex == (int)BakeType.Skirt && appearance.Wearables[(int)WearableType.Skirt].Count == 0)
                        continue;

                    pendingBakes.Add((BakeType)bakedIndex);
                }
            }

            int start = Environment.TickCount;
            List<UUID> newBakeIDs = new List<UUID>();
            foreach (BakeType bakeType in pendingBakes)
            {
                UUID assetID = UUID.Zero;
                List<AvatarTextureIndex> textureIndices = OpenMetaverse.AppearanceManager.BakeTypeToTextures(bakeType);
                Baker oven = new Baker(bakeType);

                for (int i = 0; i < textureIndices.Count; i++)
                {
                    int textureIndex = (int)textureIndices[i];
                    TextureData texture = Textures[(int)textureIndex];
                    texture.TextureIndex = (AvatarTextureIndex)textureIndex;
                    if (alphaWearable != null)
                    {
                        if (alphaWearable.Asset.Textures.ContainsKey(texture.TextureIndex) &&
                            alphaWearable.Asset.Textures[texture.TextureIndex] != UUID.Parse("5748decc-f629-461c-9a36-a35a221fe21f"))
                        {
                            assetID = alphaWearable.Asset.Textures[texture.TextureIndex];
                            goto bake_complete;
                        }
                    }

                    oven.AddTexture(texture);
                }

                oven.Bake();
                byte[] assetData = oven.BakedTexture.AssetData;
                AssetBase newBakedAsset = new AssetBase(UUID.Random());
                newBakedAsset.Data = assetData;
                newBakedAsset.TypeAsset = AssetType.Texture;
                newBakedAsset.Name = "ServerSideAppearance Texture";
                newBakedAsset.Flags = AssetFlags.Deletable | AssetFlags.Collectable | AssetFlags.Rewritable | AssetFlags.Temporary;
                if (appearance.Texture.FaceTextures[(int)AppearanceManager.BakeTypeToAgentTextureIndex(bakeType)].TextureID != UUID.Zero)
                    m_assetService.Delete(appearance.Texture.FaceTextures[(int)AppearanceManager.BakeTypeToAgentTextureIndex(bakeType)].TextureID);
                assetID = m_assetService.Store(newBakedAsset);
            bake_complete:
                newBakeIDs.Add(assetID);
                MainConsole.Instance.WarnFormat("[ServerSideAppearance]: Baked {0}", assetID);
                int place = (int)AppearanceManager.BakeTypeToAgentTextureIndex(bakeType);
                appearance.Texture.FaceTextures[place].TextureID = assetID;
            }

            MainConsole.Instance.ErrorFormat("[ServerSideAppearance]: Baking took {0} ms", (Environment.TickCount - start));

            appearance.Serial = cof_version + 1;
            cof = m_inventoryService.GetFolderForType(agentID, InventoryType.Unknown, AssetType.CurrentOutfitFolder);
            if (cof.Version > cof_version)
            {
                //it changed during the baking... kill it with fire!
                return null;
            }
            m_avatarService.SetAppearance(agentID, appearance);
            return appearance;
        }
    }
}
