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
    public class BotManager : ISharedRegionModule, IBotManager
    {
        #region IRegionModule Members

        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private Dictionary<UUID, Bot> m_bots = new Dictionary<UUID, Bot> ();

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(Scene scene)
        {
            scene.RegisterModuleInterface<IBotManager> (this);
            scene.RegisterModuleInterface<BotManager> (this);
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
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
        private AvatarAppearance GetAppearance(UUID target, IScene scene)
        {
            IScenePresence sp = scene.GetScenePresence (target);
            if (sp != null)
            {
                IAvatarAppearanceModule aa = sp.RequestModuleInterface<IAvatarAppearanceModule> ();
                if (aa != null)
                    return aa.Appearance;
            }
            return scene.AvatarService.GetAppearance (target);
        }

        /// <summary>
        /// Creates a new bot inworld
        /// </summary>
        /// <param name="FirstName"></param>
        /// <param name="LastName"></param>
        /// <param name="cloneAppearanceFrom">UUID of the avatar whos appearance will be copied to give this bot an appearance</param>
        /// <returns>ID of the bot</returns>
        public UUID CreateAvatar (string FirstName, string LastName, IScene s, UUID cloneAppearanceFrom, UUID creatorID, Vector3 startPos)
        {
            Scene scene = (Scene)s;
            AgentCircuitData m_aCircuitData = new AgentCircuitData ();
            m_aCircuitData.child = false;

            //Add the circuit data so they can login
            m_aCircuitData.circuitcode = (uint)Util.RandomClass.Next();

            m_aCircuitData.Appearance = GetAppearance (cloneAppearanceFrom, s);//Sets up appearance
            if (m_aCircuitData.Appearance == null)
            {
                m_aCircuitData.Appearance = new AvatarAppearance ();
                m_aCircuitData.Appearance.Wearables = AvatarWearable.DefaultWearables;
            }
            //Create the new bot data
            Bot m_character = new Bot (scene, m_aCircuitData, creatorID);

            m_character.FirstName = FirstName;
            m_character.LastName = LastName;
            m_aCircuitData.AgentID = m_character.AgentId;
            m_aCircuitData.Appearance.Owner = m_character.AgentId;
            List<AvatarAttachment> attachments = m_aCircuitData.Appearance.GetAttachments ();

            foreach (AvatarAttachment att in attachments)
            {
                InventoryItemBase item = scene.InventoryService.GetItem(new InventoryItemBase(att.ItemID));
                if (item != null)
                {
                    item.Owner = m_character.AgentId;
                    item.Folder = UUID.Zero;
                    scene.InventoryService.AddItem (item);
                }
            }

            scene.AuthenticateHandler.AgentCircuits.Add (m_character.CircuitCode, m_aCircuitData);
            //This adds them to the scene and sets them inworld
            scene.AddNewClient (m_character);
            m_character.Initialize ();
            IScenePresence SP = scene.GetScenePresence (m_character.AgentId);
            SP.MakeRootAgent (m_character.StartPos, false);
            //Move them
            SP.Teleport (startPos);

            IAvatarAppearanceModule appearance = SP.RequestModuleInterface<IAvatarAppearanceModule> ();
            appearance.Appearance.SetAppearance (appearance.Appearance.Texture, appearance.Appearance.VisualParams);
            appearance.SendAvatarDataToAllAgents ();
            appearance.SendAppearanceToAllOtherAgents ();
            appearance.SendOtherAgentsAppearanceToMe ();
            IAvatarFactory avFactory = scene.RequestModuleInterface<IAvatarFactory> ();
            if (avFactory != null)
                avFactory.QueueInitialAppearanceSend (SP.UUID);

            //Save them in the bots list
            m_bots.Add(m_character.AgentId, m_character);

            m_log.Info("[RexBotManager]: Added bot " + m_character.Name + " to scene.");

            //Return their UUID
            return m_character.AgentId;
        }

        public void RemoveAvatar (UUID avatarID, IScene scene)
        {
            if (!m_bots.Remove (avatarID))
                return;
            IScenePresence sp = scene.GetScenePresence (avatarID);
            if (sp == null)
                return;
            //Kill the agent
            IEntityTransferModule module = scene.RequestModuleInterface<IEntityTransferModule> ();
            module.IncomingCloseAgent (scene, avatarID);
        }

        public void PauseMovement (UUID botID)
        {
            Bot bot;
            //Find the bot
            if (m_bots.TryGetValue (botID, out bot))
                bot.PauseMovement ();
        }

        public void ResumeMovement (UUID botID)
        {
            Bot bot;
            //Find the bot
            if (m_bots.TryGetValue (botID, out bot))
                bot.ResumeMovement ();
        }

        /// <summary>
        /// Sets up where the bot should be walking
        /// </summary>
        /// <param name="Bot">ID of the bot</param>
        /// <param name="Positions">List of positions the bot will move to</param>
        /// <param name="mode">List of what the bot should be doing inbetween the positions</param>
        public void SetBotMap(UUID Bot, List<Vector3> Positions, List<TravelMode> mode, int flags)
        {
            Bot bot;
            //Find the bot
            if (m_bots.TryGetValue(Bot, out bot))
                bot.SetPath (Positions, mode, flags);
        }

        /// <summary>
        /// Speed up or slow down the bot
        /// </summary>
        /// <param name="Bot"></param>
        /// <param name="modifier"></param>
        public void SetMovementSpeedMod(UUID Bot, float modifier)
        {
            Bot bot;
            if (m_bots.TryGetValue(Bot, out bot))
                bot.SetMovementSpeedMod(modifier);
        }

        public void SetBotShouldFly (UUID botID, bool shouldFly)
        {
            Bot bot;
            if (m_bots.TryGetValue (botID, out bot))
                if (shouldFly)
                    bot.DisableWalk ();
                else
                    bot.EnableWalk ();
        }

        #region Tag/Remove bots

        private Dictionary<string, List<UUID>> m_botTags = new Dictionary<string, List<UUID>> ();
        public void AddTagToBot (UUID Bot, string tag)
        {
            if (!m_botTags.ContainsKey (tag))
                m_botTags.Add (tag, new List<UUID> ());
            m_botTags[tag].Add (Bot);
        }

        public List<UUID> GetBotsWithTag (string tag)
        {
            if (!m_botTags.ContainsKey (tag))
                return new List<UUID> ();
            return m_botTags[tag];
        }

        public void RemoveBots (string tag)
        {
            List<UUID> bots = GetBotsWithTag(tag);
            foreach(UUID bot in bots)
            {
                IScene s = m_bots[bot].Scene;
                RemoveAvatar (bot, s);
            }
        }

        #endregion

        #endregion

        #region IAStarBotManager

        /// <summary>
        /// Reads the map file for the bot
        /// </summary>
        /// <param name="Bot"></param>
        /// <param name="modifier"></param>
        public void ReadMap(UUID botID, string map, int X, int Y, int CornerStoneX, int CornerStoneY)
        {
            Bot bot;
            if (m_bots.TryGetValue(botID, out bot))
            {
                bot.ReadMap (map, X, Y, CornerStoneX, CornerStoneY);
            }
        }

        /// <summary>
        /// Starts to find the path for the bot
        /// </summary>
        /// <param name="Bot"></param>
        /// <param name="modifier"></param>
        public void FindPath(UUID botID, Vector3 currentPos, Vector3 finishVector)
        {
            Bot bot;
            if (m_bots.TryGetValue(botID, out bot))
            {
                bot.FindPath(currentPos, finishVector);
            }
        }

        /// <summary>
        /// Begins to follow the given user
        /// </summary>
        /// <param name="Bot"></param>
        /// <param name="modifier"></param>
        public void FollowAvatar (UUID botID, string avatarName, float startFollowDistance, float endFollowDistance)
        {
            Bot bot;
            if (m_bots.TryGetValue (botID, out bot))
            {
                bot.FollowAvatar (avatarName, startFollowDistance, endFollowDistance);
            }
        }

        /// <summary>
        /// SecondBotID begins to follow BotID
        /// </summary>
        /// <param name="Bot"></param>
        /// <param name="modifier"></param>
        public void FollowBot (UUID botID, UUID secondBotID, float followDistance)
        {
            Bot bot;
            if (m_bots.TryGetValue (botID, out bot))
            {
                Bot secondBot;
                if (m_bots.TryGetValue (secondBotID, out secondBot))
                {
                    bot.ChildFollowers.Add (secondBot);
                }
            }
        }

        /// <summary>
        /// Stops following the given bot
        /// </summary>
        /// <param name="Bot"></param>
        /// <param name="modifier"></param>
        public void StopFollowBot (UUID botID, UUID secondBotID)
        {
            Bot bot;
            if (m_bots.TryGetValue (botID, out bot))
            {
                Bot secondBot;
                if (m_bots.TryGetValue (secondBotID, out secondBot))
                {
                    bot.ChildFollowers.Remove (secondBot);
                }
            }
        }

        /// <summary>
        /// Stops following the given user
        /// </summary>
        /// <param name="Bot"></param>
        /// <param name="modifier"></param>
        public void StopFollowAvatar (UUID botID)
        {
            Bot bot;
            if (m_bots.TryGetValue (botID, out bot))
                bot.StopFollowAvatar ();
        }

        /// <summary>
        /// Sends a chat message to all clients
        /// </summary>
        /// <param name="Bot"></param>
        /// <param name="modifier"></param>
        public void SendChatMessage (UUID botID, string message, int sayType, int channel)
        {
            Bot bot;
            if (m_bots.TryGetValue (botID, out bot))
                bot.SendChatMessage (sayType, message, channel);
        }

        #endregion
    }
}
