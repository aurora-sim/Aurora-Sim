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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.MiniModule
{
    public class World : MarshalByRefObject, IWorld, IWorldAudio
    {
        private readonly Heightmap m_heights;
        private readonly IScene m_internalScene;

        private readonly ObjectAccessor m_objs;
        private readonly ISecurityCredential m_security;

        public World(IScene internalScene, ISecurityCredential securityCredential)
        {
            m_security = securityCredential;
            m_internalScene = internalScene;
            m_heights = new Heightmap(m_internalScene);
            m_objs = new ObjectAccessor(m_internalScene, securityCredential);
        }

        #region Events

        #region OnNewUser

        private bool _OnNewUserActive;

        public event OnNewUserDelegate OnNewUser
        {
            add
            {
                if (!_OnNewUserActive)
                {
                    _OnNewUserActive = true;
                    m_internalScene.EventManager.OnNewPresence += EventManager_OnNewPresence;
                }

                _OnNewUser += value;
            }
            remove
            {
                _OnNewUser -= value;

                if (_OnNewUser == null)
                {
                    _OnNewUserActive = false;
                    m_internalScene.EventManager.OnNewPresence -= EventManager_OnNewPresence;
                }
            }
        }

        private event OnNewUserDelegate _OnNewUser;

        private void EventManager_OnNewPresence(IScenePresence presence)
        {
            if (_OnNewUser != null)
            {
                NewUserEventArgs e = new NewUserEventArgs { Avatar = new SPAvatar(m_internalScene, presence.UUID, m_security) };
                _OnNewUser(this, e);
            }
        }

        #endregion

        #region OnChat

        private bool _OnChatActive;

        public IWorldAudio Audio
        {
            get { return this; }
        }

        public event OnChatDelegate OnChat
        {
            add
            {
                if (!_OnChatActive)
                {
                    _OnChatActive = true;
                    m_internalScene.EventManager.OnChatFromClient += EventManager_OnChatFromClient;
                    m_internalScene.EventManager.OnChatFromWorld += EventManager_OnChatFromWorld;
                }

                _OnChat += value;
            }
            remove
            {
                _OnChat -= value;

                if (_OnChat == null)
                {
                    _OnChatActive = false;
                    m_internalScene.EventManager.OnChatFromClient -= EventManager_OnChatFromClient;
                    m_internalScene.EventManager.OnChatFromWorld -= EventManager_OnChatFromWorld;
                }
            }
        }

        private event OnChatDelegate _OnChat;

        private void EventManager_OnChatFromWorld(object sender, OSChatMessage chat)
        {
            if (_OnChat != null)
            {
                HandleChatPacket(chat);
                return;
            }
        }

        private void HandleChatPacket(OSChatMessage chat)
        {
            if (string.IsNullOrEmpty(chat.Message))
                return;

            // Object?
            if (chat.Sender == null && chat.SenderObject != null)
            {
                ChatEventArgs e = new ChatEventArgs
                                      {
                                          Sender = new SOPObject(m_internalScene, chat.SenderObject.LocalId, m_security),
                                          Text = chat.Message,
                                          Channel = chat.Channel
                                      };

                _OnChat(this, e);
                return;
            }
            // Avatar?
            if (chat.Sender != null && chat.SenderObject == null)
            {
                ChatEventArgs e = new ChatEventArgs
                                      {
                                          Sender = new SPAvatar(m_internalScene, chat.SenderUUID, m_security),
                                          Text = chat.Message,
                                          Channel = chat.Channel
                                      };

                _OnChat(this, e);
                return;
            }
            // Skip if other
        }

        private void EventManager_OnChatFromClient(object sender, OSChatMessage chat)
        {
            if (_OnChat != null)
            {
                HandleChatPacket(chat);
                return;
            }
        }

        #endregion

        #endregion

        #region IWorld Members

        public IObjectAccessor Objects
        {
            get { return m_objs; }
        }

        public IParcel[] Parcels
        {
            get
            {
                IParcelManagementModule parcelManagement =
                    m_internalScene.RequestModuleInterface<IParcelManagementModule>();
                List<IParcel> m_parcels = new List<IParcel>();
                if (parcelManagement != null)
                {
                    List<ILandObject> m_los = parcelManagement.AllParcels();

#if (!ISWIN)
                    foreach (ILandObject landObject in m_los)
                    {
                        m_parcels.Add(new LOParcel(m_internalScene, landObject.LandData.LocalID));
                    }
#else
                    m_parcels.AddRange(m_los.Select(landObject => new LOParcel(m_internalScene, landObject.LandData.LocalID)).Cast<IParcel>());
#endif
                }

                return m_parcels.ToArray();
            }
        }


        public IAvatar[] Avatars
        {
            get
            {
                List<IScenePresence> ents = m_internalScene.Entities.GetPresences();
                IAvatar[] rets = new IAvatar[ents.Count];

                for (int i = 0; i < ents.Count; i++)
                {
                    IScenePresence ent = ents[i];
                    rets[i] = new SPAvatar(m_internalScene, ent.UUID, m_security);
                }

                return rets;
            }
        }

        public IHeightmap Terrain
        {
            get { return m_heights; }
        }

        #endregion

        #region Implementation of IWorldAudio

        public void PlaySound(UUID audio, Vector3 position, double volume)
        {
            ISoundModule soundModule = m_internalScene.RequestModuleInterface<ISoundModule>();
            if (soundModule != null)
            {
                soundModule.TriggerSound(audio, UUID.Zero, UUID.Zero, UUID.Zero, volume, position,
                                         m_internalScene.RegionInfo.RegionHandle, 0);
            }
        }

        public void PlaySound(UUID audio, Vector3 position)
        {
            ISoundModule soundModule = m_internalScene.RequestModuleInterface<ISoundModule>();
            if (soundModule != null)
            {
                soundModule.TriggerSound(audio, UUID.Zero, UUID.Zero, UUID.Zero, 1.0, position,
                                         m_internalScene.RegionInfo.RegionHandle, 0);
            }
        }

        #endregion
    }
}