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

        public void Initialise(IConfigSource source)
        {
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
            presence.RegisterModuleInterface<ScenePresenceCombat>(new ScenePresenceCombat(presence));
        }

        public void RegionLoaded(Scene scene)
        {
        }

        private class ScenePresenceCombat
        {
            ScenePresence m_SP;
            public ScenePresenceCombat(ScenePresence SP)
            {
                m_SP = SP;
                SP.PhysicsActor.OnCollisionUpdate += new OpenSim.Region.Physics.Manager.PhysicsActor.CollisionUpdate(PhysicsActor_OnCollisionUpdate);
            }

            void PhysicsActor_OnCollisionUpdate(EventArgs e)
            {
                if (e == null)
                    return;

                CollisionEventUpdate collisionData = (CollisionEventUpdate)e;
                Dictionary<uint, ContactPoint> coldata = collisionData.m_objCollisionList;

                if (m_SP.m_invulnerable)
                    return;

                float starthealth = m_SP.Health;
                uint killerObj = 0;
                foreach (uint localid in coldata.Keys)
                {
                    SceneObjectPart part = m_SP.Scene.GetSceneObjectPart(localid);

                    if (part != null && part.ParentGroup.Damage != -1.0f)
                        m_SP.Health -= part.ParentGroup.Damage;
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

            private void KillAvatar(uint killerObjectLocalID)
            {
                string deadAvatarMessage;
                ScenePresence killingAvatar = null;
                string killingAvatarMessage;

                if (killerObjectLocalID == 0)
                    deadAvatarMessage = "You committed suicide!";
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
                        }
                        else
                        {
                            // Try to find the avatar wielding the killing object
                            killingAvatar = m_SP.Scene.GetScenePresence(part.OwnerID);
                            if (killingAvatar == null)
                                deadAvatarMessage = String.Format("You impaled yourself on {0} owned by {1}!", part.Name, m_SP.Scene.GetUserName(part.OwnerID));
                            else
                            {
                                killingAvatarMessage = String.Format("You fragged {0}!", m_SP.Name);
                                deadAvatarMessage = String.Format("You got killed by {0}!", killingAvatar.Name);
                            }
                        }
                    }
                    else
                    {
                        killingAvatarMessage = String.Format("You fragged {0}!", m_SP.Name);
                        deadAvatarMessage = String.Format("You got killed by {0}!", killingAvatar.Name);
                    }
                }
                try
                {
                    m_SP.ControllingClient.SendAgentAlertMessage(deadAvatarMessage, true);
                    if (killingAvatar != null)
                        killingAvatar.ControllingClient.SendAlertMessage("You fragged " + m_SP.Firstname + " " + m_SP.Lastname);
                }
                catch (InvalidOperationException)
                { }

                m_SP.Health = 100;
                m_SP.Scene.TeleportClientHome(m_SP.UUID, m_SP.ControllingClient);
            }
        }

        private void AvatarEnteringParcel(ScenePresence avatar, int localLandID, UUID regionID)
        {
            try
            {
                ILandObject obj = avatar.Scene.LandChannel.GetLandObject(avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y);

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
    public class CombatModule : INonSharedRegionModule
    {
        //private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Region UUIDS indexed by AgentID
        /// </summary>
        //private Dictionary<UUID, UUID> m_rootAgents = new Dictionary<UUID, UUID>();

        /// <summary>
        /// Scenes by Region Handle
        /// </summary>
        private Dictionary<ulong, Scene> m_scenel = new Dictionary<ulong, Scene>();

        /// <summary>
        /// Startup
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="config"></param>
        public void Initialise(IConfigSource config)
        {
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

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "CombatModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        private void KillAvatar(uint killerObjectLocalID, ScenePresence deadAvatar)
        {
            string deadAvatarMessage;
            ScenePresence killingAvatar = null;
            string killingAvatarMessage;

            if (killerObjectLocalID == 0)
                deadAvatarMessage = "You committed suicide!";
            else
            {
                // Try to get the avatar responsible for the killing
                killingAvatar = deadAvatar.Scene.GetScenePresence(killerObjectLocalID);
                if (killingAvatar == null)
                {
                    // Try to get the object which was responsible for the killing
                    SceneObjectPart part = deadAvatar.Scene.GetSceneObjectPart(killerObjectLocalID);
                    if (part == null)
                    {
                        // Cause of death: Unknown
                        deadAvatarMessage = "You died!";
                    }
                    else
                    {
                        // Try to find the avatar wielding the killing object
                        killingAvatar = deadAvatar.Scene.GetScenePresence(part.OwnerID);
                        if (killingAvatar == null)
                            deadAvatarMessage = String.Format("You impaled yourself on {0} owned by {1}!", part.Name, deadAvatar.Scene.GetUserName(part.OwnerID));
                        else
                        {
                            killingAvatarMessage = String.Format("You fragged {0}!", deadAvatar.Name);
                            deadAvatarMessage = String.Format("You got killed by {0}!", killingAvatar.Name);
                        }
                    }
                }
                else
                {
                    killingAvatarMessage = String.Format("You fragged {0}!", deadAvatar.Name);
                    deadAvatarMessage = String.Format("You got killed by {0}!", killingAvatar.Name);
                }
            }
            try
            {
                deadAvatar.ControllingClient.SendAgentAlertMessage(deadAvatarMessage, true);
                if(killingAvatar != null)
                    killingAvatar.ControllingClient.SendAlertMessage("You fragged " + deadAvatar.Firstname + " " + deadAvatar.Lastname);
            }
            catch (InvalidOperationException)
            { }

            deadAvatar.Health = 100;
            deadAvatar.Scene.TeleportClientHome(deadAvatar.UUID, deadAvatar.ControllingClient);
        }

        private void AvatarEnteringParcel(ScenePresence avatar, int localLandID, UUID regionID)
        {            
            try
            {
                ILandObject obj = avatar.Scene.LandChannel.GetLandObject(avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y);
                
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
