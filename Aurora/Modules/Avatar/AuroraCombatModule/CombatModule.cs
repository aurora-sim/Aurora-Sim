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
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenMetaverse;

namespace OpenSim.Region.CoreModules.Avatar.Combat.CombatModule
{
    public class AuroraCombatModule : INonSharedRegionModule, ICombatModule
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
        private float MaximumHealth;
        private bool m_enabled;
        private Dictionary<string, List<UUID>> Teams = new Dictionary<string, List<UUID>>();
        private List<UUID> CombatAllowedAgents = new List<UUID>();
        public List<UUID> PrimCombatServers = new List<UUID>();
        public bool ForceRequireCombatPermission = true;
        public bool DisallowTeleportingForCombatants = true;
        public Scene m_scene;

        public void Initialise(IConfigSource source)
        {
            m_config = source.Configs["CombatModule"];
            if (m_config != null)
            {
                m_enabled = m_config.GetBoolean("Enabled", true);
                MaximumHealth = m_config.GetFloat("MaximumHealth", 100);
                ForceRequireCombatPermission = m_config.GetBoolean("ForceRequireCombatPermission", ForceRequireCombatPermission);
                DisallowTeleportingForCombatants = m_config.GetBoolean("DisallowTeleportingForCombatants", DisallowTeleportingForCombatants);
            }
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (m_enabled)
            {
                m_scene = scene;
                scene.RegisterModuleInterface<ICombatModule>(this);
                scene.EventManager.OnNewPresence += NewPresence;
                scene.EventManager.OnRemovePresence += EventManager_OnRemovePresence;
                scene.EventManager.OnAvatarEnteringNewParcel += AvatarEnteringParcel;
                scene.Permissions.OnAllowedOutgoingLocalTeleport += AllowedTeleports;
                scene.Permissions.OnAllowedOutgoingRemoteTeleport += AllowedTeleports;
                scene.EventManager.OnLandObjectAdded += OnLandObjectAdded;
            }
        }

        public void RemoveRegion(Scene scene)
        {
            if (m_enabled)
            {
                scene.EventManager.OnNewPresence -= NewPresence;
                scene.EventManager.OnRemovePresence -= EventManager_OnRemovePresence;
                scene.EventManager.OnAvatarEnteringNewParcel -= AvatarEnteringParcel;
                scene.Permissions.OnAllowedOutgoingLocalTeleport -= AllowedTeleports;
                scene.Permissions.OnAllowedOutgoingRemoteTeleport -= AllowedTeleports;
                scene.EventManager.OnLandObjectAdded -= OnLandObjectAdded;
            }
        }

        private bool AllowedTeleports(UUID userID, IScene scene, out string reason)
        {
            //Make sure that agents that are in combat cannot tp around. They CAN tp if they are out of combat however
            reason = "";
            IScenePresence SP = null;
            if (scene.TryGetScenePresence(userID, out SP))
                if (DisallowTeleportingForCombatants)
                    if (SP.RequestModuleInterface<ICombatPresence>() != null)
                        if (SP.RequestModuleInterface<ICombatPresence>().HasLeftCombat == false)
                            if (!SP.Invulnerable)
                                return false;
            return true;
        }

        void NewPresence (IScenePresence presence)
        {
            presence.RegisterModuleInterface<ICombatPresence>(new CombatPresence(this, presence, m_config));
        }

        void EventManager_OnRemovePresence (IScenePresence presence)
        {
            CombatPresence m = (CombatPresence)presence.RequestModuleInterface<ICombatPresence> ();
            if (m != null)
            {
                m.Close ();
                presence.UnregisterModuleInterface<ICombatPresence> (m);
            }
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void AddCombatPermission(UUID AgentID)
        {
            if(!CombatAllowedAgents.Contains(AgentID))
                CombatAllowedAgents.Add(AgentID);
        }

        public bool CheckCombatPermission(UUID AgentID)
        {
            return CombatAllowedAgents.Contains(AgentID);
        }

        public void RegisterToAvatarDeathEvents(UUID primID)
        {
            if(!PrimCombatServers.Contains(primID))
                PrimCombatServers.Add(primID);
        }

        public void DeregisterFromAvatarDeathEvents(UUID primID)
        {
            if (PrimCombatServers.Contains(primID))
                PrimCombatServers.Remove(primID);
        }

        public void AddPlayerToTeam(string Team, UUID AgentID)
        {
            lock (Teams)
            {
                List<UUID> Teammates = new List<UUID>();
                if (Teams.ContainsKey(Team))
                {
                    Teams.TryGetValue(Team, out Teammates);
                    Teams.Remove(Team);
                }
                Teammates.Add(AgentID);
                Teams.Add(Team, Teammates);
            }
        }

        public void RemovePlayerFromTeam(string Team, UUID AgentID)
        {
            lock (Teams)
            {
                List<UUID> Teammates = new List<UUID>();
                if (Teams.ContainsKey(Team))
                {
                    Teams.TryGetValue(Team, out Teammates);
                    Teams.Remove(Team);
                }
                if (Teammates.Contains(AgentID))
                    Teammates.Remove(AgentID);
                Teams.Add(Team, Teammates);
            }
        }

        public List<UUID> GetTeammates(string Team)
        {
            lock (Teams)
            {
                List<UUID> Teammates = new List<UUID>();
                if (Teams.ContainsKey(Team))
                {
                    Teams.TryGetValue(Team, out Teammates);
                }
                return Teammates;
            }
        }

        private class CombatPresence : ICombatPresence
        {
            private IScenePresence m_SP;
            private bool FireOnDeadEvent;
            private bool AllowTeamKilling;
            private bool AllowTeams;
            private bool SendTeamKillerInfo;
            private float MaximumHealth;
            private float MaximumDamageToInflict;
            private float TeamHitsBeforeSend;
            private float DamageToTeamKillers;
            private bool m_HasLeftCombat;
            private int m_SecondsBeforeRespawn;
            private Vector3 m_RespawnPosition;
            private bool m_shouldRespawn;
            private AuroraCombatModule m_combatModule;
            private float m_health = 100f; 
            private string m_Team;
            private Dictionary<UUID, float> TeamHits = new Dictionary<UUID, float> ();
            private System.Timers.Timer m_healthtimer = new System.Timers.Timer ();
            //private Dictionary<string, float> GenericStats = new Dictionary<string, float>();

            public float Health
            {
                get { return m_health; }
                set { m_health = value; }
            }

            public bool HasLeftCombat
            {
                get { return m_HasLeftCombat; }
                set { m_HasLeftCombat = value; }
            }

            public string Team
            {
                get { return m_Team; }
                set
                {
                    m_combatModule.RemovePlayerFromTeam(m_Team, m_SP.UUID);
                    m_Team = value;
                    m_combatModule.AddPlayerToTeam(m_Team, m_SP.UUID);
                }
            }

            public CombatPresence (AuroraCombatModule module, IScenePresence SP, IConfig m_config)
            {
                m_SP = SP;
                m_combatModule = module;

                FireOnDeadEvent = m_config.GetBoolean("FireDeadEvent", false);
                AllowTeamKilling = m_config.GetBoolean("AllowTeamKilling", true);
                AllowTeams = m_config.GetBoolean("AllowTeams", false);
                SendTeamKillerInfo = m_config.GetBoolean("SendTeamKillerInfo", false);
                TeamHitsBeforeSend = m_config.GetFloat("TeamHitsBeforeSend", 3);
                DamageToTeamKillers = m_config.GetFloat("DamageToTeamKillers", 100);
                MaximumHealth = m_config.GetFloat("MaximumHealth", 100);
                MaximumDamageToInflict = m_config.GetFloat("MaximumDamageToInflict", 100);
                m_RespawnPosition.X = m_config.GetFloat("RespawnPositionX", 128);
                m_RespawnPosition.Y = m_config.GetFloat("RespawnPositionY", 128);
                m_RespawnPosition.Z = m_config.GetFloat("RespawnPositionZ", 128);
                m_SecondsBeforeRespawn = m_config.GetInt("SecondsBeforeRespawn", 5);
                m_shouldRespawn = m_config.GetBoolean("ShouldRespawn", false);

                HasLeftCombat = false;
                m_Team = "No Team";

                SP.OnAddPhysics += SP_OnAddPhysics;
                SP.OnRemovePhysics += SP_OnRemovePhysics;

                //Use this to fix the avatars health
                m_healthtimer.Interval = 1000; // 1 sec
                m_healthtimer.Enabled = true;
                m_healthtimer.Elapsed += new System.Timers.ElapsedEventHandler (fixAvatarHealth_Elapsed);
            }

            public void Close ()
            {
                m_combatModule = null;
                m_healthtimer.Stop ();
                m_healthtimer.Close ();
                m_SP.OnAddPhysics -= SP_OnAddPhysics;
                m_SP.OnRemovePhysics -= SP_OnRemovePhysics;
                SP_OnRemovePhysics ();
                m_SP = null;
            }

            public void SP_OnRemovePhysics()
            {
                if(m_SP.PhysicsActor != null)
                    m_SP.PhysicsActor.OnCollisionUpdate -= PhysicsActor_OnCollisionUpdate;
            }

            public void SP_OnAddPhysics()
            {
                if (m_SP.PhysicsActor != null)
                    m_SP.PhysicsActor.OnCollisionUpdate += PhysicsActor_OnCollisionUpdate;
            }

            public void PhysicsActor_OnCollisionUpdate(EventArgs e)
            {
                if (m_SP == null || m_SP.Invulnerable)
                    return;

                if (HasLeftCombat)
                    return;

                if (e == null)
                    return;

                CollisionEventUpdate collisionData = (CollisionEventUpdate)e;
                Dictionary<uint, ContactPoint> coldata = collisionData.m_objCollisionList;

                float starthealth = Health;
                uint killerObj = 0;
                foreach (uint localid in coldata.Keys)
                {
                    ISceneChildEntity part = m_SP.Scene.GetSceneObjectPart(localid);
                    if (part != null)
                    {
                        if (part.ParentEntity.Damage != -1.0f)
                            continue;
                        IScenePresence otherAvatar = m_SP.Scene.GetScenePresence (part.OwnerID);
                        if (otherAvatar != null) // If the avatar is null, the person is not inworld, and not on a team
                        {
                            if (otherAvatar.RequestModuleInterface<ICombatPresence>().HasLeftCombat)
                            {
                                //If they have left combat, do not let them cause any damage.
                                continue;
                            }
                        }
                        if (part.ParentEntity.Damage > MaximumDamageToInflict)
                            part.ParentEntity.Damage = MaximumDamageToInflict;

                        if (AllowTeams)
                        {
                            if (otherAvatar != null) // If the avatar is null, the person is not inworld, and not on a team
                            {
                                ICombatPresence OtherAvatarCP = otherAvatar.RequestModuleInterface<ICombatPresence>();
                                if (OtherAvatarCP.Team == Team)
                                {
                                    float Hits = 0;
                                    if (!TeamHits.TryGetValue(otherAvatar.UUID, out Hits))
                                        Hits = 0;
                                    Hits++;
                                    if (SendTeamKillerInfo && Hits == TeamHitsBeforeSend)
                                    {
                                        otherAvatar.ControllingClient.SendAlertMessage("You have shot too many teammates and " + DamageToTeamKillers + " health has been taken from you!");
                                        OtherAvatarCP.Health -= DamageToTeamKillers;
                                        otherAvatar.ControllingClient.SendHealth(OtherAvatarCP.Health);
                                        if (OtherAvatarCP.Health <= 0)
                                        {
                                            KillAvatar(otherAvatar.LocalId, true);
                                        }
                                        Hits = 0;
                                    }
                                    TeamHits[otherAvatar.UUID] = Hits;

                                    if(AllowTeamKilling) //Green light on team killing
                                        Health -= part.ParentEntity.Damage;
                                }
                                else //Object, hit em
                                    Health -= part.ParentEntity.Damage;
                            }
                            else //Other team, hit em
                                Health -= part.ParentEntity.Damage;
                        }
                        else //Free for all, hit em
                            Health -= part.ParentEntity.Damage;
                    }
                    else
                    {
                        float Z = m_SP.Velocity.Length () / 20;
                        if (coldata[localid].PenetrationDepth >= 0.05f && Math.Abs(m_SP.Velocity.X) < 1 && Math.Abs(m_SP.Velocity.Y) < 1)
                        {
                            Z = Math.Min (Z, 1.5f);
                            float damage = Math.Min (coldata[localid].PenetrationDepth, 15f);
                            Health -= damage * Z;
                        }
                    }

                    if (Health > m_combatModule.MaximumHealth)
                        Health = m_combatModule.MaximumHealth;

                    if (Health <= 0.0f)
                    {
                        if (localid != 0)
                            killerObj = localid;
                    }
                    //m_log.Debug("[AVATAR]: Collision with localid: " + localid.ToString() + " at depth: " + coldata[localid].ToString());
                }

                if (starthealth != Health)
                {
                    m_SP.ControllingClient.SendHealth(Health);
                }
                if (Health <= 0)
                {
                    KillAvatar(killerObj, true);
                }
            }

            public void KillAvatar(uint killerObjectLocalID, bool TeleportAgent)
            {
                string deadAvatarMessage;
                IScenePresence killingAvatar = null;
                string killingAvatarMessage = "You fragged " + m_SP.Name;

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
                        ISceneChildEntity part = m_SP.Scene.GetSceneObjectPart (killerObjectLocalID);
                        if (part == null)
                        {
                            // Cause of death: Unknown
                            deadAvatarMessage = "You died!";
                            if (FireOnDeadEvent)
                                FireDeadAvatarEvent("", m_SP, null);
                        }
                        else
                        {
                            // Try to find the avatar wielding the killing object
                            killingAvatar = m_SP.Scene.GetScenePresence(part.OwnerID);
                            if (killingAvatar == null)
                            {
                                UserAccount account = m_SP.Scene.UserAccountService.GetUserAccount(m_SP.Scene.RegionInfo.ScopeID, part.OwnerID);
                                deadAvatarMessage = String.Format("You impaled yourself on {0} owned by {1}!", part.Name, account.Name);
                                if (FireOnDeadEvent)
                                    FireDeadAvatarEvent (account.Name, m_SP, part.ParentEntity);
                            }
                            else
                            {
                                killingAvatarMessage = String.Format("You fragged {0}!", m_SP.Name);
                                if (FireOnDeadEvent)
                                    FireDeadAvatarEvent (killingAvatar.Name, m_SP, part.ParentEntity);
                                deadAvatarMessage = String.Format("You got killed by {0}!", killingAvatar.Name);
                            }
                        }
                    }
                    else
                    {
                        ISceneChildEntity part = m_SP.Scene.GetSceneObjectPart (killerObjectLocalID);
                        if (part == null)
                        {
                            // Cause of death: Unknown
                            killingAvatarMessage = String.Format("You fragged {0}!", m_SP.Name);
                            deadAvatarMessage = String.Format("You got killed by {0}!", killingAvatar.Name);
                            if (FireOnDeadEvent)
                                FireDeadAvatarEvent(killingAvatar.Name, m_SP, null);
                        }
                        else
                        {
                            killingAvatarMessage = String.Format("You fragged {0}!", m_SP.Name);
                            deadAvatarMessage = String.Format("You got killed by {0}!", killingAvatar.Name);
                            if (FireOnDeadEvent)
                                FireDeadAvatarEvent (killingAvatar.Name, m_SP, part.ParentEntity);
                        }
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

                Health = MaximumHealth;
                if (TeleportAgent)
                {
                    if (m_shouldRespawn)
                    {
                        if (m_SecondsBeforeRespawn != 0)
                        {
                            m_SP.AllowMovement = false;
                            this.HasLeftCombat = true;
                            System.Timers.Timer t = new System.Timers.Timer();
                            //Use this to reenable movement and combat
                            t.Interval = m_SecondsBeforeRespawn * 1000;
                            t.Enabled = true;
                            t.Elapsed += new System.Timers.ElapsedEventHandler(t_Elapsed);
                        }
                        m_SP.Teleport(m_RespawnPosition);
                    }
                    else
                    {
                        IEntityTransferModule transferModule = m_SP.Scene.RequestModuleInterface<IEntityTransferModule>();
                        if (transferModule != null)
                            transferModule.TeleportHome(m_SP.UUID, m_SP.ControllingClient);
                    }
                }
            }

            void fixAvatarHealth_Elapsed (object sender, System.Timers.ElapsedEventArgs e)
            {
                //Regenerate health a bit every second
                if ((int)(Health + 0.0625) <= m_combatModule.MaximumHealth)
                    Health += 0.0625f;
            }

            void t_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                m_SP.AllowMovement = true;
                this.HasLeftCombat = false;
            }

            private void FireDeadAvatarEvent (string KillerName, IScenePresence DeadAv, ISceneEntity killer)
            {
                IScriptModule[] scriptEngines = DeadAv.Scene.RequestModuleInterfaces<IScriptModule>();
                foreach (IScriptModule m in scriptEngines)
                {
                    if (killer != null)
                    {
                        foreach (ISceneChildEntity part in killer.ChildrenEntities())
                        {
                            m.PostObjectEvent(part.UUID, "dead_avatar", new object[] { DeadAv.Name, KillerName, DeadAv.UUID });
                        }
                        foreach (UUID primID in m_combatModule.PrimCombatServers)
                        {
                            m.PostObjectEvent(primID, "dead_avatar", new object[] { DeadAv.Name, KillerName, DeadAv.UUID });
                        }
                    }
                }
            }

            public void LeaveCombat()
            {
                m_combatModule.RemovePlayerFromTeam(m_Team, m_SP.UUID);
                HasLeftCombat = true;
            }

            public void JoinCombat()
            {
                HasLeftCombat = false;
                m_combatModule.AddPlayerToTeam(m_Team, m_SP.UUID);
            }

            public List<UUID> GetTeammates()
            {
                return m_combatModule.GetTeammates(m_Team);
            }

            public void IncurDamage(uint localID, double damage, UUID OwnerID)
            {
                if (damage < 0)
                    return;

                IScenePresence SP = m_SP.Scene.GetScenePresence (OwnerID);
                if (SP == null)
                    return;
                ICombatPresence cp = SP.RequestModuleInterface<ICombatPresence>();
                if (!cp.HasLeftCombat || !m_combatModule.ForceRequireCombatPermission)
                {
                    if (damage > MaximumDamageToInflict)
                        damage = MaximumDamageToInflict;
                    float health = Health;
                    health -= (float)damage;
                    m_SP.ControllingClient.SendHealth(health);
                    if (health <= 0)
                        KillAvatar(localID, true);
                }
            }

            public void IncurDamage(uint localID, double damage, string RegionName, Vector3 pos, Vector3 lookat, UUID OwnerID)
            {
                if (damage < 0)
                    return;

                IScenePresence SP = m_SP.Scene.GetScenePresence (OwnerID);
                if (SP == null)
                    return;
                ICombatPresence cp = SP.RequestModuleInterface<ICombatPresence>();
                if (!cp.HasLeftCombat || !m_combatModule.ForceRequireCombatPermission)
                {
                    if (damage > MaximumDamageToInflict)
                        damage = MaximumDamageToInflict;
                    float health = Health;
                    health -= (float)damage;
                    m_SP.ControllingClient.SendHealth(health);
                    if (health <= 0)
                    {
                        KillAvatar(localID, false);
                        m_SP.ControllingClient.SendTeleportStart((uint)TeleportFlags.ViaHome);
                        IEntityTransferModule entityTransfer = m_SP.Scene.RequestModuleInterface<IEntityTransferModule>();
                        if (entityTransfer != null)
                        {
                            entityTransfer.RequestTeleportLocation(m_SP.ControllingClient, RegionName, pos, lookat, (uint)TeleportFlags.ViaHome);
                        }
                    }
                }
            }

            public void IncurHealing(double healing, UUID OwnerID)
            {
                if (healing < 0)
                    return;
                IScenePresence SP = m_SP.Scene.GetScenePresence (OwnerID);
                if (SP == null)
                    return;
                ICombatPresence cp = SP.RequestModuleInterface<ICombatPresence>();
                if (!cp.HasLeftCombat || !m_combatModule.ForceRequireCombatPermission)
                {
                    float health = Health;
                    health += (float)healing;
                    if (health >= MaximumHealth)
                        health = MaximumHealth;

                    m_SP.ControllingClient.SendHealth(health);
                }
            }

            public void SetStat(string StatName, float statValue)
            {

            }
        }

        private class CombatObject : ICombatPresence
        {
            private float m_health = 100f;
            private string m_Team;
            private float MaximumHealth;
            private float MaximumDamageToInflict;
            private AuroraCombatModule m_combatModule;
            private SceneObjectPart m_part;

            public string Team
            {
                get
                {
                    return m_Team;
                }
                set
                {
                    m_combatModule.RemovePlayerFromTeam(m_Team, m_part.UUID);
                    m_Team = value;
                    m_combatModule.AddPlayerToTeam(m_Team, m_part.UUID);
                }
            }

            public float Health
            {
                get
                {
                    return m_health;
                }
                set
                {
                    m_health = value;
                }
            }

            public bool HasLeftCombat
            {
                get
                {
                    return false;
                }
                set
                {
                }
            }

            public CombatObject(AuroraCombatModule module, SceneObjectPart part, IConfig m_config)
            {
                m_part = part;
                m_combatModule = module;
                
                MaximumHealth = m_config.GetFloat("MaximumHealth", 100);
                MaximumDamageToInflict = m_config.GetFloat("MaximumDamageToInflict", 100);


                m_Team = "No Team";

                m_part.OnAddPhysics += AddPhysics;
                m_part.OnRemovePhysics += RemovePhysics;
            }

            public void RemovePhysics()
            {
                if (m_part.PhysActor != null)
                    m_part.PhysActor.OnCollisionUpdate -= PhysicsActor_OnCollisionUpdate;
            }

            public void AddPhysics()
            {
                if (m_part.PhysActor != null)
                    m_part.PhysActor.OnCollisionUpdate += PhysicsActor_OnCollisionUpdate;
            }

            public void PhysicsActor_OnCollisionUpdate(EventArgs e)
            {
                if (HasLeftCombat)
                    return;

                if (e == null)
                    return;

                CollisionEventUpdate collisionData = (CollisionEventUpdate)e;
                Dictionary<uint, ContactPoint> coldata = collisionData.m_objCollisionList;

                UUID killerObj = UUID.Zero;
                foreach (uint localid in coldata.Keys)
                {
                    ISceneChildEntity part = m_part.ParentGroup.Scene.GetSceneObjectPart (localid);
                    if (part != null && part.ParentEntity.Damage != -1.0f)
                    {
                        if (part.ParentEntity.Damage > MaximumDamageToInflict)
                            part.ParentEntity.Damage = MaximumDamageToInflict;

                        Health -= part.ParentEntity.Damage;
                        if (Health <= 0.0f) 
                            killerObj = part.UUID;
                    }
                    else
                    {
                        float Z = Math.Abs(m_part.Velocity.Z);
                        if (coldata[localid].PenetrationDepth >= 0.05f)
                            Health -= coldata[localid].PenetrationDepth * Z;
                    }

                    //Regenerate health (this is approx 1 sec)
                    if ((int)(Health + 0.0625) <= m_combatModule.MaximumHealth)
                        Health += 0.0625f;

                    if (Health > m_combatModule.MaximumHealth)
                        Health = m_combatModule.MaximumHealth;
                }
                if (Health <= 0)
                {
                    Die(killerObj);
                }
            }

            public void LeaveCombat()
            {
                //NoOp
            }

            public void JoinCombat()
            {
                //NoOp
            }

            public List<UUID> GetTeammates()
            {
                return m_combatModule.GetTeammates(m_Team);
            }

            public void IncurDamage(uint localID, double damage, UUID OwnerID)
            {
                if (damage < 0)
                    return;

                if (damage > MaximumDamageToInflict)
                    damage = MaximumDamageToInflict;
                float health = Health;
                health -= (float)damage;
                if (health <= 0)
                    Die(OwnerID);
            }

            public void IncurDamage(uint localID, double damage, string RegionName, Vector3 pos, Vector3 lookat, UUID OwnerID)
            {
                if (damage < 0)
                    return;

                if (damage > MaximumDamageToInflict)
                    damage = MaximumDamageToInflict;
                float health = Health;
                health -= (float)damage;
                if (health <= 0)
                    Die(OwnerID);
            }

            public void IncurHealing(double healing, UUID OwnerID)
            {
                if (healing < 0)
                    return;

                float health = Health;
                health += (float)healing;
                if (health >= MaximumHealth)
                    health = MaximumHealth;
            }

            private void Die(UUID OwnerID)
            {
                foreach (IScriptModule m in m_part.ParentGroup.Scene.RequestModuleInterfaces<IScriptModule>())
                {
                    m.PostObjectEvent(m_part.UUID, "dead_object", new object[] { OwnerID });
                }
            }

            public void SetStat(string StatName, float statValue)
            {
            }
        }

        void OnLandObjectAdded (LandData newParcel)
        {
            //If a new land object is added or updated, we need to redo the check for the avatars invulnerability
            m_scene.ForEachScenePresence (delegate (IScenePresence sp)
            {
                AvatarEnteringParcel (sp, 0, sp.Scene.RegionInfo.RegionID);
            });
        }

        private void AvatarEnteringParcel (IScenePresence avatar, int localLandID, UUID regionID)
        {
            ILandObject obj = null;
            IParcelManagementModule parcelManagement = avatar.Scene.RequestModuleInterface<IParcelManagementModule> ();
            if (parcelManagement != null)
            {
                obj = parcelManagement.GetLandObject (avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y);
            }
            if (obj == null)
                return;

            try
            {
                
                if ((obj.LandData.Flags & (uint)ParcelFlags.AllowDamage) != 0)
                {
                    ICombatPresence CP = avatar.RequestModuleInterface<ICombatPresence>();
                    CP.Health = MaximumHealth;
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
