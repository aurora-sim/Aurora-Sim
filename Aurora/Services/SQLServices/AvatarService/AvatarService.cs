/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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

using Aurora.DataManager;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.SQLServices.AvatarService
{
    public class AvatarService : ConnectorBase, IAvatarService, IService
    {
        #region Declares

        protected IAvatarData m_Database;
        protected IInventoryService m_invService;
        protected IAvatarAppearanceArchiver m_ArchiveService;
        protected bool m_enableCacheBakedTextures = true;

        #endregion

        #region IService Members

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;

            IConfig avatarConfig = config.Configs["AvatarService"];
            if (avatarConfig != null)
                m_enableCacheBakedTextures = avatarConfig.GetBoolean("EnableBakedTextureCaching",
                                                                     m_enableCacheBakedTextures);

            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AvatarHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IAvatarService>(this);

            if (MainConsole.Instance != null)
                MainConsole.Instance.Commands.AddCommand("reset avatar appearance", "reset avatar appearance [Name]",
                                                         "Resets the given avatar's appearance to the default",
                                                         ResetAvatarAppearance);
            Init(registry, Name, serverPath: "/avatar/");
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_Database = Aurora.DataManager.DataManager.RequestPlugin<IAvatarData>();
            m_ArchiveService = registry.RequestModuleInterface<IAvatarAppearanceArchiver>();
            registry.RequestModuleInterface<ISimulationBase>().EventManager.RegisterEventHandler("DeleteUserInformation", DeleteUserInformation);
        }

        public void FinishedStartup()
        {
            m_invService = m_registry.RequestModuleInterface<IInventoryService>();
        }

        #endregion

        #region IAvatarService Members

        public virtual IAvatarService InnerService
        {
            get { return this; }
        }

        /*private void RemoveOldBaked(UUID principalID, AvatarData newdata)
        {
            if (!newdata.Data.ContainsKey("Textures")) return;
            AvatarData olddata = m_Database.Get("PrincipalID", principalID.ToString());
            if ((olddata == null) || (!olddata.Data.ContainsKey("Textures"))) return;

            Primitive.TextureEntry old_textures = Primitive.TextureEntry.FromOSD(OSDParser.DeserializeJson(olddata.Data["Textures"]));
            Primitive.TextureEntry new_textures = Primitive.TextureEntry.FromOSD(OSDParser.DeserializeJson(newdata.Data["Textures"]));
            IAssetService service = m_registry.RequestModuleInterface<IAssetService>();
            for (uint i = 0; i < old_textures.FaceTextures.Length; i++)
            {
                if ((old_textures.FaceTextures[i] == null) || ((new_textures.FaceTextures[i] != null) &&
                    (old_textures.FaceTextures[i].TextureID == new_textures.FaceTextures[i].TextureID))) continue;
                AssetBase ab = service.Get(old_textures.FaceTextures[i].TextureID.ToString());
                if ((ab != null) && (ab.Name == "Baked Texture"))
                    service.Delete(old_textures.FaceTextures[i].TextureID);
            }
        }*/

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public AvatarAppearance GetAppearance(UUID principalID)
        {
            object remoteValue = DoRemoteByURL("AvatarServerURI", principalID);
            if (remoteValue != null || m_doRemoteOnly)
                return (AvatarAppearance)remoteValue;

            return m_Database.Get(principalID);
        }

        public AvatarAppearance GetAndEnsureAppearance(UUID principalID, string avatarName, string defaultUserAvatarArchive, out bool loadedArchive)
        {
            loadedArchive = false;
            AvatarAppearance avappearance = GetAppearance(principalID);
            if (avappearance == null)
            {
                //Create an appearance for the user if one doesn't exist
                if (defaultUserAvatarArchive != "")
                {
                    avappearance = m_ArchiveService.LoadAvatarArchive(defaultUserAvatarArchive, avatarName);
                    SetAppearance(principalID, avappearance);
                    loadedArchive = true;
                }
                else
                {
                    avappearance = new AvatarAppearance(principalID);
                    SetAppearance(principalID, avappearance);
                }
            }
            return avappearance;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool SetAppearance(UUID principalID, AvatarAppearance appearance)
        {
            object remoteValue = DoRemoteByURL("AvatarServerURI", principalID, appearance);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("SetAppearance",
                                                                                                      new object[2]
                                                                                                          {
                                                                                                              principalID,
                                                                                                              appearance
                                                                                                          });
            //RemoveOldBaked(principalID, avatar);
            //avatar = FixWearables(principalID, avatar.ToAvatarAppearance(principalID));
            return m_Database.Store(principalID, appearance);
        }

        /*private AvatarAppearance FixWearables(UUID userID, AvatarAppearance appearance)
        {
            for (int i = 0; i < AvatarWearable.MAX_WEARABLES; i++)
            {
                for (int j = 0; j < appearance.Wearables[j].Count; j++)
                {
                    if (appearance.Wearables[i][j].ItemID == UUID.Zero)
                        continue;

                    InventoryItemBase baseItem = new InventoryItemBase(appearance.Wearables[i][j].ItemID, userID);
                    baseItem = m_invService.GetItem(baseItem);

                    if (baseItem != null)
                    {
                        if (baseItem.AssetType == (int)AssetType.Link)
                        {
                            baseItem = new InventoryItemBase(baseItem.AssetID, userID);
                            baseItem = m_invService.GetItem(baseItem);
                        }
                        appearance.Wearables[i].Clear();
                        appearance.Wearables[i].Add(baseItem.ID, baseItem.AssetID);
                    }
                    else
                    {
                        MainConsole.Instance.ErrorFormat(
                            "[AvatarFactory]: Can't find inventory item {0} for {1}, setting to default",
                            appearance.Wearables[i][j].ItemID, (WearableType)i);

                        appearance.Wearables[i].RemoveItem(appearance.Wearables[i][j].ItemID);
                        appearance.Wearables[i].Add(AvatarWearable.DefaultWearables[i][j].ItemID,
                                                    AvatarWearable.DefaultWearables[i][j].AssetID);
                    }
                }
            }

            return new AvatarData(appearance);
        }*/

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool ResetAvatar(UUID principalID)
        {
            object remoteValue = DoRemoteByURL("AvatarServerURI", principalID);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            return m_Database.Delete(principalID);
        }

        public object DeleteUserInformation(string name, object param)
        {
            UUID user = (UUID)param;
            ResetAvatar(user);
            return null;
        }

        #endregion

        #region Console Commands

        public void ResetAvatarAppearance(string[] cmd)
        {
            string name = "";
            name = cmd.Length == 3 ? MainConsole.Instance.Prompt("Avatar Name") : Util.CombineParams(cmd, 3);
            UserAccount acc = m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(null, name);
            if (acc == null)
            {
                MainConsole.Instance.Output("No known avatar with that name.");
                return;
            }
            ResetAvatar(acc.PrincipalID);
            InventoryFolderBase folder = m_invService.GetFolderForType(acc.PrincipalID, (InventoryType)0, AssetType.CurrentOutfitFolder);
            if (folder != null)
            {
                m_invService.ForcePurgeFolder(folder);
            }
            MainConsole.Instance.Output("Reset avatar's appearance successfully.");
        }

        #endregion
    }
}