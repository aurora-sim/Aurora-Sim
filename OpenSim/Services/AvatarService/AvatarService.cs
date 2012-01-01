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
using OpenSim.Services.Interfaces;
using log4net.Core;

namespace OpenSim.Services.AvatarService
{
    public class AvatarService : IAvatarService, IService
    {
        protected IAvatarData m_Database;
        protected bool m_enableCacheBakedTextures = true;
        protected IRegistryCore m_registry;

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        #region IAvatarService Members

        public virtual IAvatarService InnerService
        {
            get { return this; }
        }

        public AvatarAppearance GetAppearance(UUID principalID)
        {
            AvatarData avatar = GetAvatar(principalID);
            if (avatar == null || avatar.Data.Count == 0)
                return null;
            return avatar.ToAvatarAppearance(principalID);
        }

        public bool SetAppearance(UUID principalID, AvatarAppearance appearance)
        {
            AvatarData avatar = new AvatarData(appearance);
            return SetAvatar(principalID, avatar);
        }

        public AvatarData GetAvatar(UUID principalID)
        {
            return m_Database.Get("PrincipalID", principalID.ToString());
        }

        public bool SetAvatar(UUID principalID, AvatarData avatar)
        {
            m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("SetAppearance",
                                                                                                      new object[2]
                                                                                                          {
                                                                                                              principalID,
                                                                                                              avatar
                                                                                                          });
            return m_Database.Store(principalID, avatar);
        }

        public bool ResetAvatar(UUID principalID)
        {
            return m_Database.Delete("PrincipalID", principalID.ToString());
        }

        public void CacheWearableData(UUID principalID, AvatarWearable wearable)
        {
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

        #endregion

        #region IService Members

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

            if(MainConsole.Instance != null)
                MainConsole.Instance.Commands.AddCommand("reset avatar appearance", "reset avatar appearance [Name]",
                                                         "Resets the given avatar's appearance to the default",
                                                         ResetAvatarAppearance);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_Database = DataManager.RequestPlugin<IAvatarData>();
        }

        public void FinishedStartup()
        {
        }

        #endregion

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
    }
}