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
using System.Reflection;
using Nini.Config;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;
using log4net;
using OpenMetaverse;
using Mono.Addins;

namespace Aurora.Modules
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class CombatModule : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Region UUIDS indexed by AgentID
        /// </summary>
        //private Dictionary<UUID, UUID> m_rootAgents = new Dictionary<UUID, UUID>();

        /// <summary>
        /// Scenes by Region Handle
        /// </summary>
        private Dictionary<ulong, Scene> m_scenel = new Dictionary<ulong, Scene>();
        private IConfig m_config;
        /// <summary>
        /// Startup
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="config"></param>
        public void Initialise(IConfigSource config)
        {
            m_config = config.Configs["CombatModule"];
        }

        public void AddRegion(Scene scene)
        {
            lock (m_scenel)
            {
                if (m_scenel.ContainsKey(scene.RegionInfo.RegionHandle))
                {
                    m_scenel[scene.RegionInfo.RegionHandle] = scene;
                }
                else
                {
                    m_scenel.Add(scene.RegionInfo.RegionHandle, scene);
                }
            }

            scene.EventManager.OnAvatarKilled += KillAvatar;
            scene.EventManager.OnAvatarEnteringNewParcel += AvatarEnteringParcel;
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {

        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise(){}

        public void Close(){}

        public string Name{get { return "AuroraCombatModule"; }}

        public bool IsSharedModule{get { return true; }}

        private void KillAvatar(uint killerObjectLocalID, ScenePresence DeadAvatar)
        {
            bool FireEvent = m_config.GetBoolean("FireDeadEvent", false);
            if (killerObjectLocalID == 0)
            {
                DeadAvatar.ControllingClient.SendAgentAlertMessage("You committed suicide!", true);
                if (FireEvent)
                {
                    FireDeadAvatarEvent(DeadAvatar.Name, DeadAvatar, null);
                }
            }
            else
            {
                SceneObjectPart part = DeadAvatar.Scene.GetSceneObjectPart(killerObjectLocalID);
                ScenePresence sp = DeadAvatar.Scene.GetScenePresence(killerObjectLocalID);
                if (sp.LocalId != null)
                {
                    sp.ControllingClient.SendAlertMessage("You fragged " + DeadAvatar.Firstname + " " + DeadAvatar.Lastname);
                    if (FireEvent && part != null)
                        FireDeadAvatarEvent(sp.Name, DeadAvatar, part.ParentGroup);
                }
                else
                {
                    if (part != null)
                    {
                        ScenePresence av = DeadAvatar.Scene.GetScenePresence(part.OwnerID);
                        if (av != null)
                        {
                            av.ControllingClient.SendAlertMessage("You fragged " + DeadAvatar.Firstname + " " + DeadAvatar.Lastname);
                            DeadAvatar.ControllingClient.SendAgentAlertMessage("You got killed by " + av.Name + "!", true);
                            if (FireEvent)
                            {
                                FireDeadAvatarEvent(av.Name, DeadAvatar, part.ParentGroup);
                            }
                        }
                        else
                        {
                            string killer = DeadAvatar.Scene.GetUserName(part.OwnerID);
                            DeadAvatar.ControllingClient.SendAgentAlertMessage("You impaled yourself on " + part.Name + " owned by " + killer + "!", true);
                            if (FireEvent)
                            {
                                FireDeadAvatarEvent(killer, DeadAvatar, null);
                            }
                        }
                    }
                    else
                    {
                        DeadAvatar.ControllingClient.SendAgentAlertMessage("You died!", true);
                        if (FireEvent)
                        {
                            FireDeadAvatarEvent("Unknown", DeadAvatar, null);
                        }
                    }
                }
            }
            DeadAvatar.Health = 100;
            DeadAvatar.Scene.TeleportClientHome(DeadAvatar.UUID, DeadAvatar.ControllingClient);
        }

        private void FireDeadAvatarEvent(string Killer, ScenePresence DeadAv, SceneObjectGroup killer)
        {
            foreach (IScriptModule m in DeadAv.m_scriptEngines)
            {
                foreach (SceneObjectPart part in killer.Children.Values)
                {
                    foreach (UUID ID in part.Inventory.GetInventoryList())
                    {
                        m.PostObjectEvent(ID, "deadavatar", new object[] { DeadAv.Name, Killer, DeadAv.UUID });
                    }
                }
            }
        }

        private void AvatarEnteringParcel(ScenePresence avatar, int localLandID, UUID regionID)
        {
            ILandObject obj = avatar.Scene.LandChannel.GetLandObject(avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y);
            try
            {
                if ((obj.LandData.Flags & (uint)ParcelFlags.AllowDamage) != 0)
                {
                    avatar.Invulnerable = false;
                }
                else
                {
                    avatar.Invulnerable = true;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
