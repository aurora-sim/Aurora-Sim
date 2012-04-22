/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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

using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Services.AvatarService;
using OpenSim.Services.Connectors;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules
{
    public class IWCAvatarConnector : ConnectorBase, IAvatarService, IService
    {
        protected AvatarService m_localService;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IAvatarService Members

        public virtual IAvatarService InnerService
        {
            get
            {
                //If we are getting URls for an IWC connection, we don't want to be calling other things, as they are calling us about only our info
                //If we arn't, its ar region we are serving, so give it everything we know
                if (m_registry.RequestModuleInterface<InterWorldCommunications>().IsGettingUrlsForIWCConnection)
                    return m_localService;
                else
                    return this;
            }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public AvatarAppearance GetAppearance(UUID principalID)
        {
            AvatarAppearance app = m_localService.GetAppearance(principalID);
            if (app == null)
                app = (AvatarAppearance)DoRemoteForced(principalID);
            return app;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool SetAppearance(UUID principalID, AvatarAppearance appearance)
        {
            bool success = m_localService.SetAppearance(principalID, appearance);
            if (!success)
                success = (bool)DoRemoteForced(principalID, appearance);
            return success;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public AvatarData GetAvatar(UUID principalID)
        {
            AvatarData app = m_localService.GetAvatar(principalID);
            if (app == null)
                app = (AvatarData)DoRemoteForced(principalID);
            return app;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool SetAvatar(UUID principalID, AvatarData avatar)
        {
            bool success = m_localService.SetAvatar(principalID, avatar);
            if (!success)
                success = (bool)DoRemoteForced(principalID, avatar);
            return success;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool ResetAvatar(UUID principalID)
        {
            bool success = m_localService.ResetAvatar(principalID);
            if (!success)
                success = (bool)DoRemoteForced(principalID);
            return success;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void CacheWearableData(UUID principalID, AvatarWearable cachedWearable)
        {
            //NOT DONE
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AvatarHandler", "") != Name)
                return;

            m_localService = new AvatarService();
            m_localService.Initialize(config, registry);
            Init(registry, Name);
            registry.RegisterModuleInterface<IAvatarService>(this);
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_localService != null)
                m_localService.Start(config, registry);
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}