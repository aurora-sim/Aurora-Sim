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
using OpenSim.Services.Interfaces;

namespace Aurora.BotManager
{
    /// <summary>
    /// Partially created by RealXtend
    /// </summary>
    public class BotManager : ISharedRegionModule, IAStarBotManager
    {
        #region IRegionModule Members

        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private Scene m_scene;
        private Dictionary<UUID, IRexBot> m_bots = new Dictionary<UUID, IRexBot>();

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(Scene scene)
        {
            scene.RegisterModuleInterface<IBotManager>(this);
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

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return GetType().AssemblyQualifiedName; }
        }

        #endregion

        #region IBotManager

        /// <summary>
        /// Finds the given users appearance
        /// </summary>
        /// <param name="target"></param>
        /// <param name="scene"></param>
        /// <returns></returns>
        private AvatarAppearance GetAppearance(UUID target)
        {
            AvatarData adata = m_scene.AvatarService.GetAvatar(target);
            if (adata != null)
            {
                AvatarAppearance x = adata.ToAvatarAppearance(target);
                return x;
            }
            return new AvatarAppearance();
        }

        /// <summary>
        /// Creates a new bot inworld
        /// </summary>
        /// <param name="FirstName"></param>
        /// <param name="LastName"></param>
        /// <param name="cloneAppearanceFrom">UUID of the avatar whos appearance will be copied to give this bot an appearance</param>
        /// <returns>ID of the bot</returns>
        public UUID CreateAvatar(string FirstName, string LastName, UUID cloneAppearanceFrom)
        {
            //Create the new bot data
            RexBot m_character = new RexBot(m_scene);

            m_character.FirstName = FirstName;
            m_character.LastName = LastName;

            AgentCircuitData m_aCircuitData = new AgentCircuitData();
            m_aCircuitData.child = false;

            m_aCircuitData.firstname = m_character.FirstName;
            m_aCircuitData.lastname = m_character.LastName;

            //Add the circuit data so they can login
            m_aCircuitData.circuitcode = m_character.CircuitCode;

            m_aCircuitData.Appearance = GetAppearance(cloneAppearanceFrom);//Sets up appearance

            m_scene.AuthenticateHandler.AgentCircuits.Add(m_character.CircuitCode, m_aCircuitData);
            //This adds them to the scene and sets them inworld
            m_scene.AddNewClient(m_character);

            m_character.Initialize();

            ScenePresence SP = m_scene.GetScenePresence(m_character.AgentId);
            SP.Appearance.SetAppearance(SP.Appearance.Texture, SP.Appearance.VisualParams);
            SP.SendAppearanceToAllOtherAgents();
            SP.SendAvatarDataToAllAgents();

            //Save them in the bots list
            m_bots.Add(m_character.AgentId, m_character);

            m_log.Info("[RexBotManager]: Added bot " + m_character.Name + " to scene.");

            //Return their UUID
            return m_character.AgentId;
        }

        /// <summary>
        /// Creates a new bot inworld
        /// </summary>
        /// <param name="FirstName"></param>
        /// <param name="LastName"></param>
        /// <param name="cloneAppearanceFrom">UUID of the avatar whos appearance will be copied to give this bot an appearance</param>
        /// <returns>ID of the bot</returns>
        public UUID CreateAStarAvatar(string FirstName, string LastName, UUID cloneAppearanceFrom)
        {
            //Create the new bot data
            AStarBot m_character = new AStarBot(m_scene);

            m_character.FirstName = FirstName;
            m_character.LastName = LastName;

            AgentCircuitData m_aCircuitData = new AgentCircuitData();
            m_aCircuitData.child = false;

            m_aCircuitData.firstname = m_character.FirstName;
            m_aCircuitData.lastname = m_character.LastName;

            //Add the circuit data so they can login
            m_aCircuitData.circuitcode = m_character.CircuitCode;

            m_aCircuitData.Appearance = GetAppearance(cloneAppearanceFrom);//Sets up appearance

            m_scene.AuthenticateHandler.AgentCircuits.Add(m_character.CircuitCode, m_aCircuitData);
            //This adds them to the scene and sets them inworld
            m_scene.AddNewClient(m_character);

            m_character.Initialize();

            ScenePresence SP = m_scene.GetScenePresence(m_character.AgentId);
            SP.Appearance.SetAppearance(SP.Appearance.Texture, SP.Appearance.VisualParams);
            SP.SendAppearanceToAllOtherAgents();
            SP.SendAvatarDataToAllAgents();
            m_character.Initialize(SP);//Tell the bot about the SP

            //Save them in the bots list
            m_bots.Add(m_character.AgentId, m_character);

            m_log.Info("[RexBotManager]: Added AStar bot " + m_character.Name + " to scene.");

            //Return their UUID
            return m_character.AgentId;
        }

        /// <summary>
        /// Sets up where the bot should be walking
        /// </summary>
        /// <param name="Bot">ID of the bot</param>
        /// <param name="Positions">List of positions the bot will move to</param>
        /// <param name="mode">List of what the bot should be doing inbetween the positions</param>
        public void SetBotMap(UUID Bot, List<Vector3> Positions, List<TravelMode> mode)
        {
            IRexBot bot;
            //Find the bot
            if (m_bots.TryGetValue(Bot, out bot))
            {
                NavMesh mesh = new NavMesh();
                int i = 0;
                foreach (Vector3 position in Positions)
                {
                    //Add the position first
                    mesh.AddNode(position);
                    //Add the edge so that we know in which order the positions are and what to do between them
                    if (i + 1 == Positions.Count)
                        mesh.AddEdge(i, 0, mode[i]);
                    else
                        mesh.AddEdge(i, i + 1, mode[i]);
                    i++;
                }
                if (true)
                {
                }
                //Tell the bot about it
                bot.SetPath(mesh, 0, false, 100000);
            }
        }

        /// <summary>
        /// Unfreeze the bot and let them finish their route
        /// </summary>
        /// <param name="Bot"></param>
        public void UnpauseAutoMove(UUID Bot)
        {
            IRexBot bot;
            if (m_bots.TryGetValue(Bot, out bot))
                bot.UnpauseAutoMove();
        }

        /// <summary>
        /// Stop the bot from moving until Unpause is called
        /// NOTE: this does not turn off the timer to auto teleport the agent to the next destination
        /// </summary>
        /// <param name="Bot"></param>
        public void PauseAutoMove(UUID Bot)
        {
            IRexBot bot;
            if (m_bots.TryGetValue(Bot, out bot))
                bot.PauseAutoMove();
        }

        /// <summary>
        /// Stop the bot from moving
        /// </summary>
        /// <param name="Bot"></param>
        public void StopAutoMove(UUID Bot)
        {
            IRexBot bot;
            if (m_bots.TryGetValue(Bot, out bot))
                bot.StopAutoMove();
        }

        /// <summary>
        /// Start the bot moving
        /// </summary>
        /// <param name="Bot"></param>
        public void EnableAutoMove(UUID Bot)
        {
            IRexBot bot;
            if (m_bots.TryGetValue(Bot, out bot))
                bot.EnableAutoMove();
        }

        /// <summary>
        /// Speed up or slow down the bot
        /// </summary>
        /// <param name="Bot"></param>
        /// <param name="modifier"></param>
        public void SetMovementSpeedMod(UUID Bot, float modifier)
        {
            IRexBot bot;
            if (m_bots.TryGetValue(Bot, out bot))
                bot.SetMovementSpeedMod(modifier);
        }

        #endregion

        #region IAStarBotManager

        /// <summary>
        /// Reads the map file for the bot
        /// </summary>
        /// <param name="Bot"></param>
        /// <param name="modifier"></param>
        public int[,] ReadMap(UUID botID, string filename, int X, int Y, int CornerStoneX, int CornerStoneY)
        {
            IRexBot bot;
            if (m_bots.TryGetValue(botID, out bot))
            {
                if (bot is IAStarBot)
                {
                    IAStarBot abot = bot as IAStarBot;
                    return abot.ReadMap(filename, X, Y, CornerStoneX, CornerStoneY);
                }
            }
            return new int[0, 0];
        }

        /// <summary>
        /// Starts to find the path for the bot
        /// </summary>
        /// <param name="Bot"></param>
        /// <param name="modifier"></param>
        public void FindPath(UUID botID, Vector3 currentPos, Vector3 finishVector)
        {
            IRexBot bot;
            if (m_bots.TryGetValue(botID, out bot))
            {
                if (bot is IAStarBot)
                {
                    IAStarBot abot = bot as IAStarBot;
                    abot.FindPath(currentPos, finishVector);
                }
            }
        }

        /// <summary>
        /// Begins to follow the given user
        /// </summary>
        /// <param name="Bot"></param>
        /// <param name="modifier"></param>
        public void FollowAvatar(UUID botID, string avatarName)
        {
            IRexBot bot;
            if (m_bots.TryGetValue(botID, out bot))
            {
                if (bot is IAStarBot)
                {
                    IAStarBot abot = bot as IAStarBot;
                    abot.FollowAvatar(avatarName);
                }
            }
        }

        #endregion
    }
}
