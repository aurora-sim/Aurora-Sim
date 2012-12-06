/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;

namespace OpenSim.Region.Framework.Scenes.Animation
{
    /// <summary>
    ///   Handle all animation duties for a scene presence
    /// </summary>
    public class Animator : IAnimator
    {
        private static AvatarAnimations m_defaultAnimations;
        protected int SLOWFLY_DELAY = 10;

        private float m_animTickFall;
        private float m_animTickStandup;
        private float m_animTickWalk;
        protected AnimationSet m_animations;
        protected string m_movementAnimation = "DEFAULT";

        /// <value>
        ///   The scene presence that this animator applies to
        /// </value>
        protected IScenePresence m_scenePresence;

        private int m_timesBeforeSlowFlyIsOff;
        protected bool m_useSplatAnimation = true;
        private bool wasLastFlying = false;

        public bool NeedsAnimationResent { get; set; }

        public Animator(IScenePresence sp)
        {
            m_scenePresence = sp;
            IConfig animationConfig = sp.Scene.Config.Configs["Animations"];
            if (animationConfig != null)
            {
                SLOWFLY_DELAY = animationConfig.GetInt("SlowFlyDelay", SLOWFLY_DELAY);
                m_useSplatAnimation = animationConfig.GetBoolean("enableSplatAnimation", m_useSplatAnimation);
            }
            //This step makes sure that we don't waste almost 2.5! seconds on incoming agents
            m_animations = new AnimationSet(DefaultAnimations);
        }

        public static AvatarAnimations DefaultAnimations
        {
            get
            {
                if (m_defaultAnimations == null)
                    m_defaultAnimations = new AvatarAnimations();
                return m_defaultAnimations;
            }
        }

        #region IAnimator Members

        public AnimationSet Animations
        {
            get { return m_animations; }
        }

        /// <value>
        ///   The current movement animation
        /// </value>
        public string CurrentMovementAnimation
        {
            get { return m_movementAnimation; }
        }

        public void AddAnimation(UUID animID, UUID objectID)
        {
            if (m_scenePresence.IsChildAgent)
                return;

            if (m_animations.Add(animID, m_scenePresence.ControllingClient.NextAnimationSequenceNumber, objectID))
                SendAnimPack();
        }

        // Called from scripts
        public bool AddAnimation(string name, UUID objectID)
        {
            if (m_scenePresence.IsChildAgent)
                return false;

            UUID animID = m_scenePresence.ControllingClient.GetDefaultAnimation(name);
            if (animID == UUID.Zero)
                return false;

            AddAnimation(animID, objectID);
            return true;
        }

        /// <summary>
        ///   Remove the given animation from the list of current animations
        /// </summary>
        /// <param name = "animID"></param>
        public void RemoveAnimation(UUID animID)
        {
            if (m_scenePresence.IsChildAgent)
                return;

            if (m_animations.Remove(animID))
                SendAnimPack();
        }

        /// <summary>
        ///   Remove the given animation from the list of current animations
        /// </summary>
        /// <param name = "name"></param>
        public bool RemoveAnimation(string name)
        {
            if (m_scenePresence.IsChildAgent)
                return false;

            UUID animID = m_scenePresence.ControllingClient.GetDefaultAnimation(name);
            if (animID == UUID.Zero)
            {
                if (DefaultAnimations.AnimsUUID.ContainsKey(name.ToUpper()))
                    animID = DefaultAnimations.AnimsUUID[name];
                else
                    return false;
            }

            RemoveAnimation(animID);
            return true;
        }

        /// <summary>
        ///   Clear out all animations
        /// </summary>
        public void ResetAnimations()
        {
            m_animations.Clear();
        }

        /// <summary>
        ///   The movement animation is reserved for "main" animations
        ///   that are mutually exclusive, e.g. flying and sitting.
        /// </summary>
        public void TrySetMovementAnimation(string anim)
        {
            TrySetMovementAnimation(anim, false);
        }

        /// <summary>
        ///   The movement animation is reserved for "main" animations
        ///   that are mutually exclusive, e.g. flying and sitting.
        /// </summary>
        public void TrySetMovementAnimation(string anim, bool sendTerseUpdateIfNotSending)
        {
            //MainConsole.Instance.DebugFormat("Updating movement animation to {0}", anim);

            if (!m_useSplatAnimation && anim == "STANDUP")
                anim = "LAND";

            if (!m_scenePresence.IsChildAgent)
            {
                if (m_animations.TrySetDefaultAnimation(
                    anim, m_scenePresence.ControllingClient.NextAnimationSequenceNumber, m_scenePresence.UUID))
                {
                    // 16384 is CHANGED_ANIMATION
                    IAttachmentsModule attMod = m_scenePresence.Scene.RequestModuleInterface<IAttachmentsModule>();
                    if (attMod != null)
                        attMod.SendScriptEventToAttachments(m_scenePresence.UUID, "changed",
                                                            new Object[] {(int) Changed.ANIMATION});
                    SendAnimPack();
                }
                else if (sendTerseUpdateIfNotSending)
                    m_scenePresence.SendTerseUpdateToAllClients(); //Send the terse update alone then
            }
        }

        /// <summary>
        ///   This method determines the proper movement related animation
        /// </summary>
        public string GetMovementAnimation()
        {
            const float STANDUP_TIME = 2f;
            const float BRUSH_TIME = 3.5f;
            const float FALL_AFTER_MOVE_TIME = 0.75f;
            const float SOFTLAND_FORCE = 80;

            #region Inputs

            if (m_scenePresence.SitGround)
            {
                return "SIT_GROUND_CONSTRAINED";
            }
            AgentManager.ControlFlags controlFlags = (AgentManager.ControlFlags) m_scenePresence.AgentControlFlags;
            PhysicsCharacter actor = m_scenePresence.PhysicsActor;

            // Create forward and left vectors from the current avatar rotation
            Vector3 fwd = Vector3.UnitX*m_scenePresence.Rotation;
            Vector3 left = Vector3.UnitY*m_scenePresence.Rotation;

            // Check control flags
            bool heldForward =
                (((controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_AT_POS) ==
                  AgentManager.ControlFlags.AGENT_CONTROL_AT_POS) ||
                 ((controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_AT_POS) ==
                  AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_AT_POS));
            bool yawPos = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_YAW_POS) ==
                          AgentManager.ControlFlags.AGENT_CONTROL_YAW_POS;
            bool yawNeg = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_YAW_NEG) ==
                          AgentManager.ControlFlags.AGENT_CONTROL_YAW_NEG;
            bool heldBack = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_AT_NEG) ==
                            AgentManager.ControlFlags.AGENT_CONTROL_AT_NEG;
            bool heldLeft = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_LEFT_POS) ==
                            AgentManager.ControlFlags.AGENT_CONTROL_LEFT_POS;
            bool heldRight = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_LEFT_NEG) ==
                             AgentManager.ControlFlags.AGENT_CONTROL_LEFT_NEG;
            bool heldTurnLeft = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_TURN_LEFT) ==
                                AgentManager.ControlFlags.AGENT_CONTROL_TURN_LEFT;
            bool heldTurnRight = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_TURN_RIGHT) ==
                                 AgentManager.ControlFlags.AGENT_CONTROL_TURN_RIGHT;
            bool heldUp = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_UP_POS) ==
                          AgentManager.ControlFlags.AGENT_CONTROL_UP_POS;
            bool heldDown = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_UP_NEG) ==
                            AgentManager.ControlFlags.AGENT_CONTROL_UP_NEG;
            //bool flying = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_FLY) == AgentManager.ControlFlags.AGENT_CONTROL_FLY;
            //bool mouselook = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) == AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK;

            // Direction in which the avatar is trying to move
            Vector3 move = Vector3.Zero;
            if (heldForward)
            {
                move.X += fwd.X;
                move.Y += fwd.Y;
            }
            if (heldBack)
            {
                move.X -= fwd.X;
                move.Y -= fwd.Y;
            }
            if (heldLeft)
            {
                move.X += left.X;
                move.Y += left.Y;
            }
            if (heldRight)
            {
                move.X -= left.X;
                move.Y -= left.Y;
            }
            if (heldUp)
            {
                move.Z += 1;
            }
            if (heldDown)
            {
                move.Z -= 1;
            }
            float fallVelocity = (actor != null) ? actor.Velocity.Z : 0.0f;

            if (heldTurnLeft && yawPos && !heldForward &&
                !heldBack && actor != null && !actor.IsJumping &&
                !actor.Flying && move.Z == 0 &&
                fallVelocity == 0.0f && !heldUp &&
                !heldDown && move.CompareTo(Vector3.Zero) == 0)
            {
                return "TURNLEFT";
            }
            if (heldTurnRight && yawNeg && !heldForward &&
                !heldBack && actor != null && !actor.IsJumping &&
                !actor.Flying && move.Z == 0 &&
                fallVelocity == 0.0f && !heldUp &&
                !heldDown && move.CompareTo(Vector3.Zero) == 0)
            {
                return "TURNRIGHT";
            }

            // Is the avatar trying to move?
            //            bool moving = (move != Vector3.Zero);

            #endregion Inputs

            #region Standup

            float standupElapsed = (Util.EnvironmentTickCount() - m_animTickStandup)/1000f;
            if (m_scenePresence.PhysicsActor != null && standupElapsed < STANDUP_TIME &&
                m_useSplatAnimation)
            {
                // Falling long enough to trigger the animation
                m_scenePresence.FallenStandUp = true;
                m_scenePresence.PhysicsActor.Velocity = Vector3.Zero;
                return "STANDUP";
            }
            else if (standupElapsed < BRUSH_TIME &&
                     m_useSplatAnimation)
            {
                m_scenePresence.FallenStandUp = true;
                return "BRUSH";
            }
            else if (m_animTickStandup != 0 || m_scenePresence.FallenStandUp)
            {
                m_scenePresence.FallenStandUp = false;
                m_animTickStandup = 0;
            }

            #endregion Standup

            #region Flying

//            if (actor != null && actor.Flying)
            if (actor != null &&
                (m_scenePresence.AgentControlFlags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_FLY) ==
                (uint) AgentManager.ControlFlags.AGENT_CONTROL_FLY || m_scenePresence.ForceFly)
            {
                m_animTickFall = 0;
                if (move.X != 0f || move.Y != 0f)
                {
                    if (move.Z == 0)
                    {
                        if (m_scenePresence.Scene.PhysicsScene.UseUnderWaterPhysics &&
                            actor.Position.Z < m_scenePresence.Scene.RegionInfo.RegionSettings.WaterHeight)
                        {
                            return "SWIM_FORWARD";
                        }
                        else
                        {
                            if (m_timesBeforeSlowFlyIsOff < SLOWFLY_DELAY)
                            {
                                m_timesBeforeSlowFlyIsOff++;
                                return "FLYSLOW";
                            }
                            else
                                return "FLY";
                        }
                    }
                    else if (move.Z > 0)
                    {
                        if (m_scenePresence.Scene.PhysicsScene.UseUnderWaterPhysics &&
                            actor.Position.Z < m_scenePresence.Scene.RegionInfo.RegionSettings.WaterHeight)
                            return "SWIM_UP";
                        else
                            return "FLYSLOW";
                    }
                    if (m_scenePresence.Scene.PhysicsScene.UseUnderWaterPhysics &&
                        actor.Position.Z < m_scenePresence.Scene.RegionInfo.RegionSettings.WaterHeight)
                        return "SWIM_DOWN";
                    else
                        return "FLY";
                }
                else if (move.Z > 0f)
                {
                    //This is for the slow fly timer
                    m_timesBeforeSlowFlyIsOff = 0;
                    if (m_scenePresence.Scene.PhysicsScene.UseUnderWaterPhysics &&
                        actor.Position.Z < m_scenePresence.Scene.RegionInfo.RegionSettings.WaterHeight)
                        return "SWIM_UP";
                    else
                        return "HOVER_UP";
                }
                else if (move.Z < 0f)
                {
                    wasLastFlying = true;
                    //This is for the slow fly timer
                    m_timesBeforeSlowFlyIsOff = 0;
                    if (m_scenePresence.Scene.PhysicsScene.UseUnderWaterPhysics &&
                        actor.Position.Z < m_scenePresence.Scene.RegionInfo.RegionSettings.WaterHeight)
                        return "SWIM_DOWN";
                    else
                    {
                        ITerrainChannel channel = m_scenePresence.Scene.RequestModuleInterface<ITerrainChannel>();
                        if (channel != null)
                        {
                            float groundHeight =
                                channel.GetNormalizedGroundHeight((int) m_scenePresence.AbsolutePosition.X,
                                                                  (int) m_scenePresence.AbsolutePosition.Y);
                            if (actor != null && (m_scenePresence.AbsolutePosition.Z - groundHeight) < 2)
                                return "LAND";
                            else
                                return "HOVER_DOWN";
                        }
                        else
                            return "HOVER_DOWN";
                    }
                }
                else
                {
                    //This is for the slow fly timer
                    m_timesBeforeSlowFlyIsOff = 0;
                    if (m_scenePresence.Scene.PhysicsScene.UseUnderWaterPhysics &&
                        actor.Position.Z < m_scenePresence.Scene.RegionInfo.RegionSettings.WaterHeight)
                        return "SWIM_HOVER";
                    else
                        return "HOVER";
                }
            }

            m_timesBeforeSlowFlyIsOff = 0;

            #endregion Flying

            #region Jumping

            if (actor != null && actor.IsJumping)
            {
                return "JUMP";
            }
            if (actor != null && actor.IsPreJumping)
            {
                return "PREJUMP";
            }

            #endregion

            #region Falling/Floating/Landing

            float walkElapsed = (Util.EnvironmentTickCount() - m_animTickWalk) / 1000f;
            if (actor != null && actor.IsPhysical && !actor.IsJumping && (!actor.IsColliding) && actor.Velocity.Z < -2 &&
                walkElapsed > FALL_AFTER_MOVE_TIME)
            {
                //Always return falldown immediately as there shouldn't be a waiting period
                if (m_animTickFall == 0)
                    m_animTickFall = Util.EnvironmentTickCount();
                return "FALLDOWN";
            }

            #endregion Falling/Floating/Landing

            #region Ground Movement

            if (m_movementAnimation == "FALLDOWN")
            {
                float fallElapsed = (Util.EnvironmentTickCount() - m_animTickFall)/1000f;
                if (fallElapsed < 0.75)
                {
                    m_animTickFall = Util.EnvironmentTickCount();

                    return "SOFT_LAND";
                }
                else if (fallElapsed < 1.1 || (Math.Abs(actor.Velocity.X) > 1 && Math.Abs(actor.Velocity.Y) > 1 && actor.Velocity.Z < 3))
                {
                    m_animTickFall = Util.EnvironmentTickCount();

                    return "LAND";
                }
                else
                {
                    if (m_useSplatAnimation)
                    {
                        m_animTickStandup = Util.EnvironmentTickCount();
                        return "STANDUP";
                    }
                    else
                        return "LAND";
                }
            }
            else if (m_movementAnimation == "LAND")
            {
                if (actor != null && actor.Velocity.Z < 0)
                {
                    if (actor.Velocity.Z < SOFTLAND_FORCE)
                        return "LAND";
                    return "SOFT_LAND";
                }
                //return "LAND";
            }

            m_animTickFall = 0;

            if (move.Z <= 0f)
            {
                if (actor != null && (move.X != 0f || move.Y != 0f ||
                                      actor.Velocity.X != 0 && actor.Velocity.Y != 0))
                {
                    wasLastFlying = false;
                    if(actor.IsColliding)
                        m_animTickWalk = Util.EnvironmentTickCount();
                    // Walking / crouchwalking / running
                    if (move.Z < 0f)
                        return "CROUCHWALK";
                    else if (m_scenePresence.SetAlwaysRun)
                        return "RUN";
                    else
                        return "WALK";
                }
                else
                {
                    // Not walking
                    if (move.Z < 0f && !wasLastFlying)
                        return "CROUCH";
                    else
                        return "STAND";
                }
            }

            #endregion Ground Movement

            return m_movementAnimation;
        }

        /// <summary>
        ///   Update the movement animation of this avatar according to its current state
        /// </summary>
        public void UpdateMovementAnimations(bool sendTerseUpdate)
        {
            string oldanimation = m_movementAnimation;
            m_movementAnimation = GetMovementAnimation();
            if (NeedsAnimationResent || oldanimation != m_movementAnimation || sendTerseUpdate)
            {
                NeedsAnimationResent = false;
                TrySetMovementAnimation(m_movementAnimation, sendTerseUpdate);
            }
        }

        /// <summary>
        ///   Gets a list of the animations that are currently in use by this avatar
        /// </summary>
        /// <returns></returns>
        public UUID[] GetAnimationArray()
        {
            UUID[] animIDs;
            int[] sequenceNums;
            UUID[] objectIDs;
            m_animations.GetArrays(out animIDs, out sequenceNums, out objectIDs);
            return animIDs;
        }

        /// <summary>
        ///   Sends all clients the given information for this avatar
        /// </summary>
        /// <param name = "animations"></param>
        /// <param name = "seqs"></param>
        /// <param name = "objectIDs"></param>
        public void SendAnimPack(UUID[] animations, int[] sequenceNums, UUID[] objectIDs)
        {
            if (m_scenePresence.IsChildAgent)
                return;

            AnimationGroup anis = new AnimationGroup
                                      {
                                          Animations = animations,
                                          SequenceNums = sequenceNums,
                                          ObjectIDs = objectIDs,
                                          AvatarID = m_scenePresence.UUID
                                      };
#if (!ISWIN)
            m_scenePresence.Scene.ForEachScenePresence(
                delegate(IScenePresence presence)
                {
                    presence.SceneViewer.QueuePresenceForAnimationUpdate(m_scenePresence, anis);
                });
#else
            m_scenePresence.Scene.ForEachScenePresence(
                presence => presence.SceneViewer.QueuePresenceForAnimationUpdate(presence, anis));
#endif
        }

        /// <summary>
        ///   Send an animation update to the given client
        /// </summary>
        /// <param name = "client"></param>
        public void SendAnimPackToClient(IClientAPI client)
        {
            if (m_scenePresence.IsChildAgent)
                return;

            UUID[] animations;
            int[] sequenceNums;
            UUID[] objectIDs;

            m_animations.GetArrays(out animations, out sequenceNums, out objectIDs);
            AnimationGroup anis = new AnimationGroup
                                      {
                                          Animations = animations,
                                          SequenceNums = sequenceNums,
                                          ObjectIDs = objectIDs,
                                          AvatarID = m_scenePresence.ControllingClient.AgentId
                                      };
            m_scenePresence.Scene.GetScenePresence(client.AgentId).SceneViewer.QueuePresenceForAnimationUpdate(
                m_scenePresence, anis);
        }

        /// <summary>
        ///   Send animation information about this avatar to all clients.
        /// </summary>
        public void SendAnimPack()
        {
            //MainConsole.Instance.Debug("Sending animation pack to all");

            if (m_scenePresence.IsChildAgent)
                return;

            UUID[] animIDs;
            int[] sequenceNums;
            UUID[] objectIDs;

            m_animations.GetArrays(out animIDs, out sequenceNums, out objectIDs);

            SendAnimPack(animIDs, sequenceNums, objectIDs);
        }

        /// <summary>
        ///   Close out and remove any current data
        /// </summary>
        public void Close()
        {
            m_animations = null;
            m_scenePresence = null;
        }

        #endregion
    }
}