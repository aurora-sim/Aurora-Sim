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
 *     * Neither the name of the OpenSim Project nor the
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
using System.IO;
using System.Text;
using System.Xml;
using OpenMetaverse;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Aurora.Framework;

namespace OpenSim.Region.Examples.RexBot
{
    public class RexBotManager : ISharedRegionModule, IBotManager
    {
        #region IRegionModule Members

        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private Scene m_scene;
        private Dictionary<UUID, RexBot> m_bots;

        private AgentCircuitData m_aCircuitData;

        public void Initialise(IConfigSource source)
        {
            m_aCircuitData = new AgentCircuitData();
            m_aCircuitData.child = false;
            m_bots = new Dictionary<UUID, RexBot>();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void AddRegion(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
            m_scene = scene;
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
            m_scene = null;
            m_bots.Clear();
        }

        public string Name
        {
            get { return GetType().AssemblyQualifiedName; }
        }

        #endregion

        #region IBotManager

        private AvatarAppearance GetAppearance(UUID target, Scene scene)
        {
            if (m_appearanceCache.ContainsKey(target))
                return m_appearanceCache[target];

            AvatarData adata = scene.AvatarService.GetAvatar(target);
            if (adata != null)
            {
                AvatarAppearance x = adata.ToAvatarAppearance(target);

                m_appearanceCache.Add(target, x);

                return x;
            }
            return new AvatarAppearance();
        }

        public UUID CreateAvatar(string FirstName, string LastName)
        {
            RexBot m_character = new RexBot(m_scene);

            m_character.FirstName = FirstName;
            m_character.LastName = LastName;

            m_aCircuitData.firstname = m_character.FirstName;
            m_aCircuitData.lastname = m_character.LastName;
            m_aCircuitData.circuitcode = m_character.CircuitCode;
            m_scene.AuthenticateHandler.AgentCircuits.Add(m_character.CircuitCode, m_aCircuitData);

            m_scene.AddNewClient(m_character);
            m_character.Initialize();
            m_bots.Add(m_character.AgentId, m_character);

            m_log.Info("[RexBotManager]: Added bot " + m_character.Name + " to scene.");

            return m_character.AgentId;
        }

        public void SetBotMap(UUID Bot, List<Vector3> Positions, List<TravelMode> mode)
        {
            RexBot bot;
            if (m_bots.TryGetValue(Bot, out bot))
            {
                NavMesh mesh = new NavMesh();
                int i = 0;
                foreach (Vector3 position in Positions)
                {
                    mesh.AddNode(position);
                    mesh.AddEdge(i, i + 1, TravelMode.Walk);
                    i++;
                }
                bot.SetPath(mesh, 0, false, 100000);
            }
        }

        public void UnpauseAutoMove(UUID Bot)
        {
            RexBot bot;
            if (m_bots.TryGetValue(Bot, out bot))
                bot.UnpauseAutoMove();
        }

        public void PauseAutoMove(UUID Bot)
        {
            RexBot bot;
            if (m_bots.TryGetValue(Bot, out bot))
                bot.PauseAutoMove();
        }

        public void StopAutoMove(UUID Bot)
        {
            RexBot bot;
            if (m_bots.TryGetValue(Bot, out bot))
                bot.StopAutoMove();
        }

        public void EnableAutoMove(UUID Bot)
        {
            RexBot bot;
            if (m_bots.TryGetValue(Bot, out bot))
                bot.EnableAutoMove();
        }

        public void SetMovementSpeedMod(UUID Bot, float modifier)
        {
            RexBot bot;
            if (m_bots.TryGetValue(Bot, out bot))
                bot.SetMovementSpeedMod(modifier);
        }

        #endregion
    }
}
