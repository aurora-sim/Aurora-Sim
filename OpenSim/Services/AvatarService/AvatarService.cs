/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Nini.Config;
using log4net;
using OpenSim.Framework;
using OpenSim.Data;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Simulation.Base;

namespace OpenSim.Services.AvatarService
{
    public class AvatarService : IAvatarService, IService
    {
        protected IAvatarData m_Database = null;
        protected IRegistryCore m_registry = null;
        protected bool m_enableCacheBakedTextures = true;

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AvatarHandler", "") != Name)
                return;

            m_registry = registry;

            IConfig avatarConfig = config.Configs["AvatarService"];
            if (avatarConfig != null)
                m_enableCacheBakedTextures = avatarConfig.GetBoolean ("EnableBakedTextureCaching", m_enableCacheBakedTextures);

            registry.RegisterModuleInterface<IAvatarService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_Database = Aurora.DataManager.DataManager.RequestPlugin<IAvatarData> ();
        }

        public void FinishedStartup()
        {
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
            return m_Database.Store (principalID, avatar);
        }

        public bool ResetAvatar(UUID principalID)
        {
            return m_Database.Delete("PrincipalID", principalID.ToString());
        }

        public bool SetItems(UUID principalID, string[] names, string[] values)
        {
            return m_Database.SetItems (principalID, names, values);
        }

        public bool RemoveItems(UUID principalID, string[] names)
        {
            foreach (string name in names)
            {
                m_Database.Delete(principalID, name);
            }
            return true;
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
                        service.Delete(texture.ToString());
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
                    m_log.Warn("[AvatarService]: Issue saving the cached wearables to the database.");
                }
            }
            catch
            {
            }*/
        }
    }
}
