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
 *     * Neither the name of the OpenSimulator Project nor the
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
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;

namespace OpenSim.Region.Framework.Scenes.Animation
{
    /// <summary>
    /// Handle all animation duties for a scene presence
    /// </summary>
    public class ScenePresenceAnimator
    {
        public AnimationSet Animations
        {
            get { return m_animations;  }
        }
        protected AnimationSet m_animations = null;

        /// <value>
        /// The current movement animation
        /// </value>
        public string CurrentMovementAnimation
        {
            get { return m_movementAnimation; }
        }

        private static AvatarAnimations m_defaultAnimations = null;

        public static AvatarAnimations DefaultAnimations
        {
            get
            {
                if (m_defaultAnimations == null)
                    m_defaultAnimations = new AvatarAnimations();
                return m_defaultAnimations;
            }
        }

        protected string m_movementAnimation = "DEFAULT";

        private float m_animTickFall;
        private float m_animTickJump;
        private float m_timesBeforeSlowFlyIsOff = 0;
        private float m_animTickStandup = 0;
        
        /// <value>
        /// The scene presence that this animator applies to
        /// </value>
        protected ScenePresence m_scenePresence;
        
        public ScenePresenceAnimator(ScenePresence sp)
        {
            m_scenePresence = sp;
            //This step makes sure that we don't waste almost 2.5! seconds on incoming agents
            m_animations = new AnimationSet(DefaultAnimations);
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

        public void RemoveAnimation(UUID animID)
        {
            if (m_scenePresence.IsChildAgent)
                return;

            if (m_animations.Remove(animID))
                SendAnimPack();
        }

        // Called from scripts
        public bool RemoveAnimation(string name)
        {
            if (m_scenePresence.IsChildAgent)
                return false;

            UUID animID = m_scenePresence.ControllingClient.GetDefaultAnimation(name);
            if (animID == UUID.Zero)
                return false;

            RemoveAnimation(animID);
            return true;
        }

        public void ResetAnimations()
        {
            m_animations.Clear();
        }
        
        /// <summary>
        /// The movement animation is reserved for "main" animations
        /// that are mutually exclusive, e.g. flying and sitting.
        /// </summary>
        public void TrySetMovementAnimation(string anim)
        {
            //m_log.DebugFormat("Updating movement animation to {0}", anim);

            if (!m_scenePresence.Scene.m_useSplatAnimation && anim == "STANDUP")
                anim = "LAND";

            if (!m_scenePresence.IsChildAgent)
            {
                if (m_animations.TrySetDefaultAnimation(
                    anim, m_scenePresence.ControllingClient.NextAnimationSequenceNumber, m_scenePresence.UUID))
                {
                    // 16384 is CHANGED_ANIMATION
                    m_scenePresence.SendScriptEventToAttachments("changed", new Object[] { (int)Changed.ANIMATION});
                    SendAnimPack();
                }
            }
        }

        /// <summary>
        /// This method determines the proper movement related animation
        /// </summary>
        private string GetMovementAnimation()
        {
            const float SLOWFLY_DELAY = 15f;
            const float STANDUP_TIME = 2f;
            const float BRUSH_TIME = 3.5f;

            const float SOFTLAND_FORCE = 80;
            
            const float PREJUMP_DELAY = 0.35f;

            #region Inputs

            if (m_scenePresence.SitGround)
            {
                return "SIT_GROUND_CONSTRAINED";
            }
            AgentManager.ControlFlags controlFlags = (AgentManager.ControlFlags)m_scenePresence.AgentControlFlags;
            PhysicsActor actor = m_scenePresence.PhysicsActor;

            // Create forward and left vectors from the current avatar rotation
            Matrix4 rotMatrix = Matrix4.CreateFromQuaternion(m_scenePresence.Rotation);
            Vector3 fwd = Vector3.Transform(Vector3.UnitX, rotMatrix);
            Vector3 left = Vector3.Transform(Vector3.UnitY, rotMatrix);

            // Check control flags
            bool heldForward =
                (((controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_AT_POS) == AgentManager.ControlFlags.AGENT_CONTROL_AT_POS) || ((controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_AT_POS) == AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_AT_POS));
            bool yawPos = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_YAW_POS) == AgentManager.ControlFlags.AGENT_CONTROL_YAW_POS;
            bool yawNeg = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_YAW_NEG) == AgentManager.ControlFlags.AGENT_CONTROL_YAW_NEG;
            bool heldBack = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_AT_NEG) == AgentManager.ControlFlags.AGENT_CONTROL_AT_NEG;
            bool heldLeft = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_LEFT_POS) == AgentManager.ControlFlags.AGENT_CONTROL_LEFT_POS;
            bool heldRight = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_LEFT_NEG) == AgentManager.ControlFlags.AGENT_CONTROL_LEFT_NEG;
            bool heldTurnLeft = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_TURN_LEFT) == AgentManager.ControlFlags.AGENT_CONTROL_TURN_LEFT;
            bool heldTurnRight = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_TURN_RIGHT) == AgentManager.ControlFlags.AGENT_CONTROL_TURN_RIGHT;
            bool heldUp = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_UP_POS) == AgentManager.ControlFlags.AGENT_CONTROL_UP_POS;
            bool heldDown = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_UP_NEG) == AgentManager.ControlFlags.AGENT_CONTROL_UP_NEG;
            //bool flying = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_FLY) == AgentManager.ControlFlags.AGENT_CONTROL_FLY;
            //bool mouselook = (controlFlags & AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) == AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK;

            // Direction in which the avatar is trying to move
            Vector3 move = Vector3.Zero;
            if (heldForward) { move.X += fwd.X; move.Y += fwd.Y; }
            if (heldBack) { move.X -= fwd.X; move.Y -= fwd.Y; }
            if (heldLeft) { move.X += left.X; move.Y += left.Y; }
            if (heldRight) { move.X -= left.X; move.Y -= left.Y; }
            if (heldUp) { move.Z += 1; }
            if (heldDown) { move.Z -= 1; }
            bool jumping = m_animTickJump != 0;
            float fallVelocity = (actor != null) ? actor.Velocity.Z : 0.0f;

            if (heldTurnLeft && yawPos && !heldForward &&
                !heldBack && !jumping && actor != null &&
                !actor.Flying && move.Z == 0 &&
                fallVelocity == 0.0f && !heldUp &&
                !heldDown && move.CompareTo(Vector3.Zero) == 0)
            {
                return "TURNLEFT";
            }
            if (heldTurnRight && yawNeg && !heldForward &&
                !heldBack && !jumping && actor != null &&
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

            float standupElapsed = (float)(Util.EnvironmentTickCount() - m_animTickStandup) / 1000f;
            if (standupElapsed < STANDUP_TIME &&
                m_scenePresence.Scene.m_useSplatAnimation)
            {
                // Falling long enough to trigger the animation
                m_scenePresence.AllowMovement = false;
                return "STANDUP";
            }
            else if (standupElapsed < BRUSH_TIME &&
                m_scenePresence.Scene.m_useSplatAnimation)
            {
                m_scenePresence.AllowMovement = false;
                return "BRUSH";
            }
            else if(m_animTickStandup != 0)
            {
                m_animTickStandup = 0;
                m_scenePresence.AllowMovement = true;
            }

            #endregion Standup

            #region Flying

            if (actor != null && actor.Flying)
            {
                m_animTickFall = 0;
                m_animTickJump = 0;
                if (move.X != 0f || move.Y != 0f)
                {
                    if (move.Z == 0)
                    {
                        if (m_scenePresence.Scene.SceneGraph.PhysicsScene.UseUnderWaterPhysics &&
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
                        if (m_scenePresence.Scene.SceneGraph.PhysicsScene.UseUnderWaterPhysics &&
                            actor.Position.Z < m_scenePresence.Scene.RegionInfo.RegionSettings.WaterHeight)
                            return "SWIM_UP";
                        else
                            return "FLYSLOW";
                    }
                    if (m_scenePresence.Scene.SceneGraph.PhysicsScene.UseUnderWaterPhysics &&
                            actor.Position.Z < m_scenePresence.Scene.RegionInfo.RegionSettings.WaterHeight)
                        return "SWIM_DOWN";
                    else
                        return "FLY";
                }
                else if (move.Z > 0f)
                {
                    //This is for the slow fly timer
                    m_timesBeforeSlowFlyIsOff = 0;
                    if (m_scenePresence.Scene.SceneGraph.PhysicsScene.UseUnderWaterPhysics &&
                            actor.Position.Z < m_scenePresence.Scene.RegionInfo.RegionSettings.WaterHeight)
                        return "SWIM_UP";
                    else
                        return "HOVER_UP";
                }
                else if (move.Z < 0f)
                {
                    //This is for the slow fly timer
                    m_timesBeforeSlowFlyIsOff = 0;
                    if (m_scenePresence.Scene.SceneGraph.PhysicsScene.UseUnderWaterPhysics &&
                            actor.Position.Z < m_scenePresence.Scene.RegionInfo.RegionSettings.WaterHeight)
                        return "SWIM_DOWN";
                    else
                    {
                        ITerrainChannel channel = m_scenePresence.Scene.RequestModuleInterface<ITerrainChannel>();
                        float groundHeight = channel.GetNormalizedGroundHeight(m_scenePresence.AbsolutePosition.X, m_scenePresence.AbsolutePosition.Y);
                        if (actor != null && (m_scenePresence.AbsolutePosition.Z - groundHeight) < 2)
                            return "LAND";
                        else
                            return "HOVER_DOWN";
                    }
                }
                else
                {
                    //This is for the slow fly timer
                    m_timesBeforeSlowFlyIsOff = 0;
                    if (m_scenePresence.Scene.SceneGraph.PhysicsScene.UseUnderWaterPhysics &&
                            actor.Position.Z < m_scenePresence.Scene.RegionInfo.RegionSettings.WaterHeight)
                        return "SWIM_HOVER";
                    else
                        return "HOVER";
                }
            }

            m_timesBeforeSlowFlyIsOff = 0;
            #endregion Flying

            #region Falling/Floating/Landing

            if (actor == null && !jumping && move.Z == 0 || (actor != null && (!actor.CollidingObj && !actor.CollidingGround) && m_scenePresence.Velocity.Z < -2))
            {
                //Always return falldown immediately as there shouldn't be a waiting period
                if(m_animTickFall == 0)
                    m_animTickFall = Util.EnvironmentTickCount();
                return "FALLDOWN";
            }

            #endregion Falling/Floating/Landing

            #region Ground Movement

            //This needs to be in front of landing, otherwise you get odd landing effects sometimes when one jumps
            // -- Revolution
            if (move.Z > 0f || m_animTickJump != 0)
            {
                if (m_scenePresence.Scene.m_usePreJump)
                {
                    //This is to check to make sure they arn't trying to fly up by holding down jump
                    if ((m_scenePresence.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_FLY) == 0)
                    {
                        // Jumping
                        float jumpChange = (((float)Util.EnvironmentTickCount()) - m_animTickJump) / 1000;
                        if (!jumping || (jumpChange < PREJUMP_DELAY && m_animTickJump > 0))
                        {
                            // Begin prejump
                            if (m_animTickJump == 0)
                                m_animTickJump = (float)Util.EnvironmentTickCount();
                            return "PREJUMP";
                        }
                        else if (m_animTickJump != 0)
                        {
                            #region PreJump 
                            /*m_hasPreJumped = true;
                            if (m_scenePresence.PreJumpForce.Z != 0 && !m_hasJumpAddedForce)
                            {
                                m_hasJumpAddedForce = true;
                                m_scenePresence.PreJumpForce.X /= 2f;
                                m_scenePresence.PreJumpForce.Y /= 2f;
                                //m_scenePresence.PreJumpForce.Z *= 1.75f;
                                if(m_scenePresence.Scene.m_UseNewStyleMovement)
                                    m_scenePresence.m_velocityIsDecaying = false;

                                m_scenePresence.PhysicsActor.Velocity = m_scenePresence.PreJumpForce;
                                //m_scenePresence.m_forceToApply = m_scenePresence.PreJumpForce;

                                m_scenePresence.PreJumpForce = new Vector3(
                                    m_scenePresence.PreJumpForce.X > 0 ? 7 : (m_scenePresence.PreJumpForce.X < 0 ? -3 : 0),
                                    m_scenePresence.PreJumpForce.Y > 0 ? 7 : (m_scenePresence.PreJumpForce.Y < 0 ? -3 : 0),
                                    0);

                                m_jumpZ = 0;
                                return "JUMP";
                            }

                            if (jumpChange >= 3) //Kill this if it takes too long
                            {
                                m_scenePresence.PhysicsActor.Velocity = Vector3.Zero;
                                m_animTickJump = 0;
                                m_scenePresence.AllowMovement = true;
                                if (m_scenePresence.Scene.m_UseNewStyleMovement)
                                    m_scenePresence.m_velocityIsDecaying = true;
                                m_animTickNextJump = Util.TickCount();
                                return "STAND";
                            }

                            //Check #1: Going up.
                            if (m_jumpZ == 0 &&
                                m_scenePresence.Velocity.Z >= -0.3)
                            {
                                //This stops from double jump when you jump straight up and doesn't break jumping with X and Y velocity
                                //This particular check makes sure that we do not break jumping straight up
                                if (!m_scenePresence.m_forceToApply.HasValue ||
                                     (m_scenePresence.m_forceToApply.HasValue &&
                                     m_scenePresence.m_forceToApply.Value.X != 0 &&
                                     m_scenePresence.m_forceToApply.Value.Y != 0))
                                {
                                    m_scenePresence.PreJumpForce.Z = -1f;

                                    m_scenePresence.m_forceToApply = m_scenePresence.PreJumpForce;
                                }

                                m_scenePresence.AllowMovement = false;
                                return "JUMP";
                            }
                            //Check #2: Coming down
                            else if (m_jumpZ <= 1 &&
                                m_scenePresence.Velocity.Z < 0)
                            {
                                m_jumpZ = 1;
                                //This stops from double jump when you jump straight up and doesn't break jumping with X and Y velocity
                                //This particular check makes sure that we do not break jumping straight up
                                if (!m_scenePresence.m_forceToApply.HasValue ||
                                     (m_scenePresence.m_forceToApply.HasValue &&
                                     m_scenePresence.m_forceToApply.Value.X != 0 &&
                                     m_scenePresence.m_forceToApply.Value.Y != 0))
                                {
                                    m_scenePresence.PreJumpForce.Z = -0.1f;

                                    m_scenePresence.m_forceToApply = m_scenePresence.PreJumpForce;
                                }

                                m_scenePresence.AllowMovement = false;
                                return "JUMP";
                            }
                            else
                            {
                                //m_scenePresence.m_forceToApply = Vector3.Zero;
                                m_scenePresence.PhysicsActor.Velocity = Vector3.Zero;
                                m_animTickJump = 0;
                                m_scenePresence.AllowMovement = true;
                                if (m_scenePresence.Scene.m_UseNewStyleMovement)
                                    m_scenePresence.m_velocityIsDecaying = true;
                                m_animTickNextJump = Util.EnvironmentTickCount();
                                return "STAND";
                            }*/
                            #endregion
                            if (m_scenePresence.PreJumpForce != Vector3.Zero)
                            {
                                Vector3 jumpForce = m_scenePresence.PreJumpForce;
                                m_scenePresence.PreJumpForce = Vector3.Zero;
                                m_scenePresence.AddNewMovement(jumpForce, Quaternion.Identity);
                                m_animTickJump = -42;
                                return "JUMP";
                            }

                            m_animTickJump++;
                            //This never gets hit as velocity is really broken
                            //if (m_scenePresence.Velocity.Z < -0.50)
                            //{
                            //    m_scenePresence.m_forceToApply = Vector3.Zero;
                            //    m_scenePresence.m_overrideUserInput = false;
                            //}
                            return "JUMP";
                        }
                    }
                }
                else
                {
                    // Jumping
                    //float jumpChange = (((float)Util.EnvironmentTickCount()) - m_animTickJump) / 1000;
                    if (!jumping)
                    {
                        m_animTickJump = Util.EnvironmentTickCount();
                        return "JUMP";
                    }
                    else
                    {
                        // Start actual jump
                        if (m_animTickJump > 0)
                        {
                            m_animTickJump = -20;
                            return "JUMP";
                        }

                        m_animTickJump++;
                        return "JUMP";
                    }
                }
            }
            if (m_scenePresence.IsJumping)
            {
                m_scenePresence.IsJumping = false;
                m_scenePresence.AllowMovement = true;
            }

            if (m_movementAnimation == "FALLDOWN")
            {
                float fallElapsed = (float)(Util.EnvironmentTickCount() - m_animTickFall) / 1000f;
                if (fallElapsed < 0.75)
                {
                    m_animTickFall = Util.EnvironmentTickCount();

                    return "SOFT_LAND";
                }
                else if (fallElapsed < 1.1)
                {
                    m_animTickFall = Util.EnvironmentTickCount();

                    return "LAND";
                }
                else
                {
                    if (m_scenePresence.Scene.m_useSplatAnimation)
                    {
                        m_animTickStandup = Util.EnvironmentTickCount();
                        return "STANDUP";
                    }
                    else
                        return "LAND";
                }
                /*//Experimentally found variables, but it makes soft landings look good.
                // -- Revolution
                //Note: we use m_scenePresence.LastVelocity for a reason! The PhysActor and SP Velocity are both cleared before this is called.
                
                float Z = m_scenePresence.LastVelocity.Z * m_scenePresence.LastVelocity.Z;
                if (Math.Abs(m_scenePresence.LastVelocity.X) < 0.1 && Math.Abs(m_scenePresence.LastVelocity.Y) < 0.1)
                    Z *= Z; //If you are falling down, the calculation is different..
                if (Z < SOFTLAND_FORCE)
                {
                    m_animTickFall = Util.EnvironmentTickCount();

                    return "SOFT_LAND";
                }
                else if (Z < LAND_FORCE)
                {
                    m_animTickFall = Util.EnvironmentTickCount();

                    return "LAND";
                }
                else
                {
                    if (m_scenePresence.Scene.m_useSplatAnimation)
                    {
                        m_animTickStandup = Util.EnvironmentTickCount();
                        return "STANDUP";
                    }
                    else
                        return "LAND";
                }*/
            }
            else if (m_movementAnimation == "LAND")
            {
                if (actor.Velocity.Z != 0)
                {
                    if (actor.Velocity.Z < SOFTLAND_FORCE)
                        return "LAND";
                    return "SOFT_LAND";
                }
            }

            m_animTickFall = 0;

            if (move.Z <= 0f)
            {
                // Not jumping
                m_animTickJump = 0;

                if (move.X != 0f || move.Y != 0f ||
                    actor.Velocity.X != 0 && actor.Velocity.Y != 0)
                {
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
                    if (move.Z < 0f)
                        return "CROUCH";
                    else
                       return "STAND";
                }
            }

            #endregion Ground Movement

            return m_movementAnimation;
        }

        /// <summary>
        /// Update the movement animation of this avatar according to its current state
        /// </summary>
        public void UpdateMovementAnimations()
        {
            m_movementAnimation = GetMovementAnimation();
            TrySetMovementAnimation(m_movementAnimation);
        }

        public UUID[] GetAnimationArray()
        {
            UUID[] animIDs;
            int[] sequenceNums;
            UUID[] objectIDs;
            m_animations.GetArrays(out animIDs, out sequenceNums, out objectIDs);
            return animIDs;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="animations"></param>
        /// <param name="seqs"></param>
        /// <param name="objectIDs"></param>
        public void SendAnimPack(UUID[] animations, int[] seqs, UUID[] objectIDs)
        {
            if (m_scenePresence.IsChildAgent)
                return;

            m_scenePresence.Scene.ForEachClient(
                delegate(IClientAPI client) 
                { 
                    client.SendAnimations(animations, seqs, m_scenePresence.ControllingClient.AgentId, objectIDs); 
                });
        }

        public void SendAnimPackToClient(IClientAPI client)
        {
            if (m_scenePresence.IsChildAgent)
                return;

            UUID[] animIDs;
            int[] sequenceNums;
            UUID[] objectIDs;

            m_animations.GetArrays(out animIDs, out sequenceNums, out objectIDs);
            client.SendAnimations(animIDs, sequenceNums, m_scenePresence.ControllingClient.AgentId, objectIDs);
        }

        /// <summary>
        /// Send animation information about this avatar to all clients.
        /// </summary>
        public void SendAnimPack()
        {
            //m_log.Debug("Sending animation pack to all");
            
            if (m_scenePresence.IsChildAgent)
                return;

            UUID[] animIDs;
            int[] sequenceNums;
            UUID[] objectIDs;

            m_animations.GetArrays(out animIDs, out sequenceNums, out objectIDs);

            SendAnimPack(animIDs, sequenceNums, objectIDs);
        }

        public void Close()
        {
            m_animations = null;
            m_scenePresence = null;
        }
    }
}
