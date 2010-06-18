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
using Nini.Config;
using OpenSim.Region.Physics.Manager;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using Mono.Addins;

namespace OpenSim.Region.CoreModules.Avatar.Combat.CombatModule
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class AuroraCombatModule : INonSharedRegionModule
    {
        public string Name
        {
            get { return "AuroraCombatModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }
        private IConfig m_config;
        private float MaximumHealth = 100;

        public void Initialise(IConfigSource source)
        {
            m_config = source.Configs["CombatModule"];
            MaximumHealth = m_config.GetFloat("MaximumHealth", 100);
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            scene.EventManager.OnNewPresence += EventManager_OnNewPresence;
            scene.EventManager.OnAvatarEnteringNewParcel += AvatarEnteringParcel;
        }

        public void RemoveRegion(Scene scene)
        {
            scene.EventManager.OnNewPresence -= EventManager_OnNewPresence;
            scene.EventManager.OnAvatarEnteringNewParcel -= AvatarEnteringParcel;
        }

        void EventManager_OnNewPresence(ScenePresence presence)
        {
            presence.RegisterModuleInterface<ICombatPresence>(new CombatPresence(presence, m_config));
        }

        public void RegionLoaded(Scene scene)
        {
        }

        private class CombatPresence : ICombatPresence
        {
            ScenePresence m_SP;
            bool FireOnDeadEvent;
            bool AllowTeamKilling;
            bool AllowTeams;
            bool SendTeamKillerInfo;
            float MaximumHealth;
            float MaximumDamageToInflict;
            float TeamHitsBeforeSend;
            float DamageToTeamKillers;
            string m_Team;
            public string Team
            {
                get{return m_Team;}
                set{m_Team = value;}
            }

            Dictionary<UUID, float> TeamHits = new Dictionary<UUID, float>();

            public CombatPresence(ScenePresence SP, IConfig m_config)
            {
                m_SP = SP;

                FireOnDeadEvent = m_config.GetBoolean("FireDeadEvent", false);
                AllowTeamKilling = m_config.GetBoolean("AllowTeamKilling", true);
                AllowTeams = m_config.GetBoolean("AllowTeams", false);
                SendTeamKillerInfo = m_config.GetBoolean("SendTeamKillerInfo", false);
                TeamHitsBeforeSend = m_config.GetFloat("TeamHitsBeforeSend", 3);
                DamageToTeamKillers = m_config.GetFloat("DamageToTeamKillers", 100);
                MaximumHealth = m_config.GetFloat("MaximumHealth", 100);
                MaximumDamageToInflict = m_config.GetFloat("MaximumDamageToInflict", 100);

                Team = "";

                SP.OnAddPhysics += new ScenePresence.AddPhysics(SP_OnAddPhysics);
                SP.OnRemovePhysics += new ScenePresence.RemovePhysics(SP_OnRemovePhysics);
            }

            void SP_OnRemovePhysics()
            {
                m_SP.PhysicsActor.OnCollisionUpdate -= PhysicsActor_OnCollisionUpdate;
            }

            void SP_OnAddPhysics()
            {
                m_SP.PhysicsActor.OnCollisionUpdate += PhysicsActor_OnCollisionUpdate;
            }

            void PhysicsActor_OnCollisionUpdate(EventArgs e)
            {
                if (m_SP.m_invulnerable)
                    return;

                if (e == null)
                    return;

                CollisionEventUpdate collisionData = (CollisionEventUpdate)e;
                Dictionary<uint, ContactPoint> coldata = collisionData.m_objCollisionList;

                float starthealth = m_SP.Health;
                uint killerObj = 0;
                foreach (uint localid in coldata.Keys)
                {
                    SceneObjectPart part = m_SP.Scene.GetSceneObjectPart(localid);

                    if (part != null && part.ParentGroup.Damage != -1.0f)
                    {
                        if (part.ParentGroup.Damage > MaximumDamageToInflict)
                            part.ParentGroup.Damage = MaximumDamageToInflict;

                        if (AllowTeams)
                        {
                            ScenePresence otherAvatar = m_SP.Scene.GetScenePresence(part.OwnerID);
                            if (otherAvatar != null) // If the avatar is null, the person is not inworld, and not on a team
                            {
                                if (otherAvatar.RequestModuleInterface<CombatPresence>().Team == Team)
                                {
                                    float Hits = 0;
                                    if(TeamHits.ContainsKey(otherAvatar.UUID))
                                    {
                                        TeamHits.TryGetValue(otherAvatar.UUID, out Hits);
                                        TeamHits.Remove(otherAvatar.UUID);
                                    }
                                    Hits++;
                                    if (Hits == TeamHitsBeforeSend)
                                    {
                                        otherAvatar.ControllingClient.SendAlertMessage("You have shot too many teammates!");
                                        otherAvatar.Health -= DamageToTeamKillers;
                                        if (otherAvatar.Health <= 0)
                                        {
                                            KillAvatar(m_SP.LocalId);
                                        }
                                        Hits = 0;
                                    }
                                    TeamHits.Add(otherAvatar.UUID, Hits);
                                }
                            }
                        }

                        m_SP.Health -= part.ParentGroup.Damage;
                    }
                    else
                    {
                        if (coldata[localid].PenetrationDepth >= 0.10f)
                            m_SP.Health -= coldata[localid].PenetrationDepth * 5.0f;
                    }

                    if (m_SP.Health <= 0.0f)
                    {
                        if (localid != 0)
                            killerObj = localid;
                    }
                    //m_log.Debug("[AVATAR]: Collision with localid: " + localid.ToString() + " at depth: " + coldata[localid].ToString());
                }
                if (starthealth != m_SP.Health)
                {
                    m_SP.ControllingClient.SendHealth(m_SP.Health);
                }
                if (m_SP.Health <= 0)
                {
                    KillAvatar(killerObj);
                }
            }

            public void KillAvatar(uint killerObjectLocalID)
            {
                string deadAvatarMessage;
                ScenePresence killingAvatar = null;
                string killingAvatarMessage = "You fragged " + m_SP.Firstname + " " + m_SP.Lastname;

                if (killerObjectLocalID == 0)
                {
                    deadAvatarMessage = "You committed suicide!";
                    if (FireOnDeadEvent)
                    {
                        FireDeadAvatarEvent(m_SP.Name, m_SP, null);
                    }
                }
                else
                {
                    // Try to get the avatar responsible for the killing
                    killingAvatar = m_SP.Scene.GetScenePresence(killerObjectLocalID);
                    if (killingAvatar == null)
                    {
                        // Try to get the object which was responsible for the killing
                        SceneObjectPart part = m_SP.Scene.GetSceneObjectPart(killerObjectLocalID);
                        if (part == null)
                        {
                            // Cause of death: Unknown
                            deadAvatarMessage = "You died!";
                            if (FireOnDeadEvent)
                                FireDeadAvatarEvent("", m_SP, part.ParentGroup);
                        }
                        else
                        {
                            // Try to find the avatar wielding the killing object
                            killingAvatar = m_SP.Scene.GetScenePresence(part.OwnerID);
                            if (killingAvatar == null)
                            {
                                deadAvatarMessage = String.Format("You impaled yourself on {0} owned by {1}!", part.Name, m_SP.Scene.GetUserName(part.OwnerID));
                                if (FireOnDeadEvent)
                                    FireDeadAvatarEvent(m_SP.Scene.GetUserName(part.OwnerID), m_SP, part.ParentGroup);
                            }
                            else
                            {
                                killingAvatarMessage = String.Format("You fragged {0}!", m_SP.Name);
                                if (FireOnDeadEvent)
                                    FireDeadAvatarEvent(killingAvatar.Name, m_SP, part.ParentGroup);
                                deadAvatarMessage = String.Format("You got killed by {0}!", killingAvatar.Name);
                            }
                        }
                    }
                    else
                    {
                        SceneObjectPart part = m_SP.Scene.GetSceneObjectPart(killerObjectLocalID);
                        killingAvatarMessage = String.Format("You fragged {0}!", m_SP.Name);
                        deadAvatarMessage = String.Format("You got killed by {0}!", killingAvatar.Name);
                        if (FireOnDeadEvent)
                            FireDeadAvatarEvent(killingAvatar.Name, m_SP, part.ParentGroup);
                    }
                }
                try
                {
                    m_SP.ControllingClient.SendAgentAlertMessage(deadAvatarMessage, true);
                    if (killingAvatar != null)
                        killingAvatar.ControllingClient.SendAlertMessage(killingAvatarMessage);
                }
                catch (InvalidOperationException)
                { }

                m_SP.Health = MaximumHealth;
                m_SP.Scene.TeleportClientHome(m_SP.UUID, m_SP.ControllingClient);
            }

            private void FireDeadAvatarEvent(string KillerName, ScenePresence DeadAv, SceneObjectGroup killer)
            {
                foreach (IScriptModule m in DeadAv.m_scriptEngines)
                {
                    if (killer != null)
                    {
                        foreach (SceneObjectPart part in killer.Children.Values)
                        {
                            foreach (UUID ID in part.Inventory.GetInventoryList())
                            {
                                m.PostObjectEvent(ID, "deadavatar", new object[] { DeadAv.Name, KillerName, DeadAv.UUID });
                            }
                        }
                    }
                }
            }
        }

        private void AvatarEnteringParcel(ScenePresence avatar, int localLandID, UUID regionID)
        {
            try
            {
                ILandObject obj = avatar.Scene.LandChannel.GetLandObject(avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y);

                if ((obj.LandData.Flags & (uint)ParcelFlags.AllowDamage) != 0)
                {
                    avatar.Health = MaximumHealth;
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
