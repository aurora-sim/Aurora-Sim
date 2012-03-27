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
using log4net.Core;

namespace OpenSim.Services.AvatarService
{
    public class AvatarService : ConnectorBase, IAvatarService, IService
    {
        #region Declares

        protected IAvatarData m_Database;
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
            Init(registry, Name);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_Database = DataManager.RequestPlugin<IAvatarData>();
            registry.RequestModuleInterface<ISimulationBase>().EventManager.RegisterEventHandler("DeleteUserInformation", DeleteUserInformation);
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region IAvatarService Members

        public virtual IAvatarService InnerService
        {
            get { return this; }
        }

        private void RemoveOldBaked(UUID principalID, AvatarData newdata)
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
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public AvatarAppearance GetAppearance(UUID principalID)
        {
            AvatarData avatar = GetAvatar(principalID);
            if (avatar == null || avatar.Data.Count == 0)
                return null;
            return avatar.ToAvatarAppearance(principalID);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool SetAppearance(UUID principalID, AvatarAppearance appearance)
        {
            AvatarData avatar = new AvatarData(appearance);
            return SetAvatar(principalID, avatar);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public AvatarData GetAvatar(UUID principalID)
        {
            object remoteValue = DoRemote(principalID);
            if (remoteValue != null || m_doRemoteOnly)
                return (AvatarData)remoteValue;

            return m_Database.Get("PrincipalID", principalID.ToString());
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool SetAvatar(UUID principalID, AvatarData avatar)
        {
            object remoteValue = DoRemote(principalID, avatar);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("SetAppearance",
                                                                                                      new object[2]
                                                                                                          {
                                                                                                              principalID,
                                                                                                              avatar
                                                                                                          });
            RemoveOldBaked(principalID, avatar);
            return m_Database.Store(principalID, avatar);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool ResetAvatar(UUID principalID)
        {
            object remoteValue = DoRemote(principalID);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            return m_Database.Delete("PrincipalID", principalID.ToString());
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void CacheWearableData(UUID principalID, AvatarWearable wearable)
        {
            object remoteValue = DoRemote(principalID, wearable);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (!m_enableCacheBakedTextures)
            {
                IAssetService service = m_registry.RequestModuleInterface<IAssetService>();
                if (service != null)
                {
                    //Remove the old baked textures then from the DB as we don't want to keep them around
                    foreach (UUID texture in wearable.GetItems().Values)
                    {
                        service.Delete(texture);
                    }
                }
                return;
            }
            wearable.MaxItems = 0; //Unlimited items

            /*AvatarBaseData baseData = new AvatarBaseData();
            AvatarBaseData[] av = m_CacheDatabase.Get("PrincipalID", principalID.ToString());
            foreach (AvatarBaseData abd in av)
            {
                //If we have one already made, keep what is already there
                if (abd.Data["Name"] == "CachedWearables")
                {
                    baseData = abd;
                    OSDArray array = (OSDArray)OSDParser.DeserializeJson(abd.Data["Value"]);
                    AvatarWearable w = new AvatarWearable();
                    w.MaxItems = 0; //Unlimited items
                    w.Unpack(array);
                    foreach (KeyValuePair<UUID, UUID> kvp in w.GetItems())
                    {
                        wearable.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            //If we don't have one, set it up for saving a new one
            if (baseData.Data == null)
            {
                baseData.PrincipalID = principalID;
                baseData.Data = new Dictionary<string, string>();
                baseData.Data.Add("Name", "CachedWearables");
            }
            baseData.Data["Value"] = OSDParser.SerializeJsonString(wearable.Pack());
            try
            {
                bool store = m_CacheDatabase.Store(baseData);
                if (!store)
                {
                    MainConsole.Instance.Warn("[AvatarService]: Issue saving the cached wearables to the database.");
                }
            }
            catch
            {
            }*/
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
            UserAccount acc = m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(UUID.Zero, name);
            if (acc == null)
            {
                MainConsole.Instance.Output("No known avatar with that name.");
                return;
            }
            ResetAvatar(acc.PrincipalID);
            MainConsole.Instance.Output("Reset avatar's appearance successfully.");
        }

        #endregion
    }
}