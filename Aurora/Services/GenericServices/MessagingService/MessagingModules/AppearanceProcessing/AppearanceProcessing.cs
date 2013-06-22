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

using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.DatabaseInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Other;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System.Collections.Generic;
using GridRegion = Aurora.Framework.Services.GridRegion;

namespace Aurora.Services
{
    public class AppearanceProcessing : IService
    {
        #region Declares

        protected IRegistryCore m_registry;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
            //Also look for incoming messages to display
            m_registry.RequestModuleInterface<ISyncMessageRecievedService>().OnMessageReceived += OnMessageReceived;
        }

        #endregion

        /// <summary>
        ///     Region side
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected OSDMap OnMessageReceived(OSDMap message)
        {
            //We need to check and see if this is an AgentStatusChange
            if (message.ContainsKey("Method") && message["Method"] == "UpdateAvatarAppearance")
            {
                AvatarAppearance appearance = new AvatarAppearance(message["AgentID"], (OSDMap)message["Appearance"]);
                ISceneManager manager = m_registry.RequestModuleInterface<ISceneManager>();
                if (manager != null)
                {
                    foreach (IScene scene in manager.Scenes)
                    {
                        IScenePresence sp = scene.GetScenePresence(appearance.Owner);
                        if (sp != null && !sp.IsChildAgent)
                        {
                            IAvatarFactory factory = scene.RequestModuleInterface<IAvatarFactory>();
                            sp.RequestModuleInterface<IAvatarAppearanceModule>().Appearance = appearance;
                            sp.RequestModuleInterface<IAvatarAppearanceModule>().SendAppearanceToAgent(sp);
                            sp.RequestModuleInterface<IAvatarAppearanceModule>().SendAppearanceToAllOtherAgents();
                        }
                    }
                }
            }
            return null;
        }
    }
}