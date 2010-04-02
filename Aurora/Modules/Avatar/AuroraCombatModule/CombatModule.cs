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

namespace Aurora.Modules
{
    public class CombatModule : IRegionModule
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
        public void Initialise(Scene scene, IConfigSource config)
        {
            m_config = config.Configs["CombatModule"];
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
            scene.AuroraEventManager.OnGenericEvent += new Aurora.Framework.OnGenericEventHandler(AuroraEventManager_OnGenericEvent);
        }

        void AuroraEventManager_OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "HealthUpdate")
            {
                object[] Update = (Object[])parameters;
                ScenePresence SP = (ScenePresence)Update[0];
                Dictionary<uint, ContactPoint> coldata = (Dictionary<uint, ContactPoint>)Update[1];
                if (SP.m_invulnerable)
                    return;

                float starthealth = SP.Health;
                uint killerObj = 0;
                foreach (uint localid in coldata.Keys)
                {
                    SceneObjectPart part = SP.Scene.GetSceneObjectPart(localid);

                    if (part != null && part.ParentGroup.Damage != -1.0f)
                        SP.Health -= part.ParentGroup.Damage;
                    else
                    {
                        if (coldata[localid].PenetrationDepth >= 0.10f)
                            SP.Health -= coldata[localid].PenetrationDepth * 5.0f;
                    }

                    if (SP.Health <= 0.0f)
                    {
                        if (localid != 0)
                            killerObj = localid;
                    }
                    //m_log.Debug("[AVATAR]: Collision with localid: " + localid.ToString() + " at depth: " + coldata[localid].ToString());
                }
                //Health = 100;
                if (!SP.m_invulnerable)
                {
                    if (starthealth != SP.Health)
                    {
                        SP.ControllingClient.SendHealth(SP.Health);
                    }
                    if (SP.Health <= 0)
                    {
                        SP.Scene.EventManager.TriggerAvatarKill(killerObj, SP);
                    }
                }
            }
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
                    FireDeadAvatarEvent(DeadAvatar.Name, DeadAvatar);
                }
            }
            else
            {
                bool foundResult = false;
                string resultstring = String.Empty;
                DeadAvatar.Scene.ForEachScenePresence(delegate(ScenePresence sp)
                                             {
                        if (sp.LocalId == killerObjectLocalID)
                        {
                            sp.ControllingClient.SendAlertMessage("You fragged " + DeadAvatar.Firstname + " " + DeadAvatar.Lastname);
                            resultstring = sp.Firstname + " " + sp.Lastname;
                            foundResult = true;
                            if (FireEvent)
                            {
                                FireDeadAvatarEvent(resultstring, DeadAvatar);
                            }
                        }
                                                      });
                if (!foundResult)
                {
                    SceneObjectPart part = DeadAvatar.Scene.GetSceneObjectPart(killerObjectLocalID);
                    if (part != null)
                    {
                        ScenePresence av = DeadAvatar.Scene.GetScenePresence(part.OwnerID);
                        if (av != null)
                        {
                            av.ControllingClient.SendAlertMessage("You fragged " + DeadAvatar.Firstname + " " + DeadAvatar.Lastname);
                            resultstring = av.Firstname + " " + av.Lastname;
                            DeadAvatar.ControllingClient.SendAgentAlertMessage("You got killed by " + resultstring + "!", true);
                            if (FireEvent)
                            {
                                FireDeadAvatarEvent(resultstring, DeadAvatar);
                            }
                        }
                        else
                        {
                            string killer = DeadAvatar.Scene.GetUserName(part.OwnerID);
                            DeadAvatar.ControllingClient.SendAgentAlertMessage("You impaled yourself on " + part.Name + " owned by " + killer + "!", true);
                            if (FireEvent)
                            {
                                FireDeadAvatarEvent(killer, DeadAvatar);
                            }
                        }
                        //DeadAvatar.Scene. part.ObjectOwner
                    }
                    else
                    {
                        DeadAvatar.ControllingClient.SendAgentAlertMessage("You died!", true);
                        if (FireEvent)
                        {
                            FireDeadAvatarEvent("Unknown", DeadAvatar);
                        }
                    }
                }
            }
            DeadAvatar.Health = 100;
            DeadAvatar.Scene.TeleportClientHome(DeadAvatar.UUID, DeadAvatar.ControllingClient);
        }

        private void FireDeadAvatarEvent(string Killer, ScenePresence SP)
        {
            foreach (IScriptModule m in SP.m_scriptEngines)
            {
                foreach (EntityBase grp in SP.Scene.Entities.GetEntities())
                {
                    if (grp is SceneObjectGroup)
                    {
                        SceneObjectGroup group = (SceneObjectGroup)grp;
                        m.PostObjectEvent(group.RootPart.UUID, "avatardead", new Object[] { SP.Name, Killer });
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
