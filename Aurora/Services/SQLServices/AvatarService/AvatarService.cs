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

using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Assets;
using Aurora.Framework.Services.ClassHelpers.Inventory;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Services.SQLServices.AvatarService
{
    public class AvatarService : ConnectorBase, IAvatarService, IService
    {
        #region Declares

        protected IAvatarData m_Database;
        protected IAssetService m_assetService;
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
                                                         ResetAvatarAppearance, false, true);
            Init(registry, Name, serverPath: "/avatar/", serverHandlerName: "AvatarServerURI");
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_Database = Framework.Utilities.DataManager.RequestPlugin<IAvatarData>();
            m_ArchiveService = registry.RequestModuleInterface<IAvatarAppearanceArchiver>();
            registry.RequestModuleInterface<ISimulationBase>()
                    .EventManager.RegisterEventHandler("DeleteUserInformation", DeleteUserInformation);
        }

        public void FinishedStartup()
        {
            m_assetService = m_registry.RequestModuleInterface<IAssetService>();
            m_invService = m_registry.RequestModuleInterface<IInventoryService>();
        }

        #endregion

        #region IAvatarService Members

        public virtual IAvatarService InnerService
        {
            get { return this; }
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public AvatarAppearance GetAppearance(UUID principalID)
        {
            object remoteValue = DoRemoteByURL("AvatarServerURI", principalID);
            if (remoteValue != null || m_doRemoteOnly)
                return (AvatarAppearance) remoteValue;

            return m_Database.Get(principalID);
        }

        public AvatarAppearance GetAndEnsureAppearance(UUID principalID, string defaultUserAvatarArchive, out bool loadedArchive)
        {
            loadedArchive = false;
            AvatarAppearance avappearance = GetAppearance(principalID);
            if (avappearance == null)
            {
                //Create an appearance for the user if one doesn't exist
                if (defaultUserAvatarArchive != "")
                {
                    AvatarArchive arch = m_ArchiveService.LoadAvatarArchive(defaultUserAvatarArchive, principalID);
                    if (arch != null)
                    {
                        avappearance = arch.Appearance;
                        SetAppearance(principalID, avappearance);
                        loadedArchive = true;
                    }
                }
                if(avappearance == null)//Set as ruth
                {
                    avappearance = new AvatarAppearance(principalID);
                    SetAppearance(principalID, avappearance);
                }
            }
            return avappearance;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public void SetAppearance(UUID principalID, AvatarAppearance appearance)
        {
            if (m_doRemoteOnly)
            {
                DoRemotePostByURL("AvatarServerURI", principalID, appearance);
                return;
            }

            m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("SetAppearance",
                                                                                                      new object[2]
                                                                                                          {
                                                                                                              principalID,
                                                                                                              appearance
                                                                                                          });
            m_Database.Store(principalID, appearance);
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public void ResetAvatar(UUID principalID)
        {
            if (m_doRemoteOnly)
            {
                DoRemotePostByURL("AvatarServerURI", principalID);
                return;
            }

            m_Database.Delete(principalID);
        }

        private object DeleteUserInformation(string name, object param)
        {
            UUID user = (UUID) param;
            ResetAvatar(user);
            return null;
        }

        #endregion

        #region Console Commands

        public void ResetAvatarAppearance(IScene scene, string[] cmd)
        {
            string name = "";
            name = cmd.Length == 3 ? MainConsole.Instance.Prompt("Avatar Name") : Util.CombineParams(cmd, 3);
            UserAccount acc = m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(null, name);
            if (acc == null)
            {
                MainConsole.Instance.Format(Level.Off, "No known avatar with that name.");
                return;
            }
            ResetAvatar(acc.PrincipalID);
            InventoryFolderBase folder = m_invService.GetFolderForType(acc.PrincipalID, (InventoryType) 0,
                                                                       AssetType.CurrentOutfitFolder);
            if (folder != null)
                m_invService.ForcePurgeFolder(folder);

            MainConsole.Instance.Format(Level.Off, "Reset avatar's appearance successfully.");
        }

        #endregion
    }
}