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
using System.Linq;
using System.Net;
using System.Reflection;
using System.Timers;
using Aurora.Framework;
using Games.Pathfinding.AStar2DTest;
using Mischel.Collections;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace Aurora.BotManager
{

    #region Enums

    public enum BotState
    {
        Idle,
        Walking,
        Flying,
        Unknown
    }

    #endregion

    #region Delegates

    public delegate void FollowingEvent(UUID avatarID, UUID botID);

    #endregion

    #region Avatar Controller

    public class BotAvatarController : IBotController
    {
        private IScenePresence m_scenePresence;
        private Bot m_bot;
        private bool m_hasStoppedMoving = false;

        public BotAvatarController(IScenePresence presence, Bot bot)
        {
            m_scenePresence = presence;
            m_bot = bot;
            if (presence.ControllingClient is BotClientAPI)
                (presence.ControllingClient as BotClientAPI).Initialize(this);
        }

        public void SetDrawDistance(float draw)
        {
            m_scenePresence.DrawDistance = draw;
        }

        public void SetSpeedModifier(float speed)
        {
            if (speed > 4)
                speed = 4;
            m_scenePresence.SpeedModifier = speed;
        }

        public Vector3 AbsolutePosition { get { return m_scenePresence.AbsolutePosition; } }

        // Makes the bot fly to the specified destination
        public void StopMoving(bool fly, bool clearPath)
        {
            if (m_hasStoppedMoving)
                return;
            m_hasStoppedMoving = true;
            m_bot.State = BotState.Idle;
            //Clear out any nodes
            if (clearPath)
                m_bot.m_nodeGraph.Clear();
            //Send the stop message
            m_bot.m_movementFlag = (uint)AgentManager.ControlFlags.NONE;
            if (fly)
                m_bot.m_movementFlag |= (uint)AgentManager.ControlFlags.AGENT_CONTROL_FLY;
            OnBotAgentUpdate(Vector3.Zero, m_bot.m_movementFlag, m_bot.m_bodyDirection, false);
            m_scenePresence.CollisionPlane = Vector4.UnitW;
            if (m_scenePresence.PhysicsActor != null)
                m_scenePresence.PhysicsActor.ForceSetVelocity(Vector3.Zero);
        }

        public bool CanMove { get { return m_scenePresence.AllowMovement && !m_scenePresence.Frozen && !m_scenePresence.FallenStandUp; } }

        public IScene GetScene()
        {
            return m_scenePresence.Scene;
        }

        public bool ForceFly { get { return m_scenePresence.ForceFly; } set { m_scenePresence.ForceFly = value; } }

        public bool SetAlwaysRun { get { return m_scenePresence.SetAlwaysRun; } set { m_scenePresence.SetAlwaysRun = value; } }

        public void Teleport(Vector3 pos)
        {
            m_scenePresence.Teleport(pos);
        }

        public PhysicsActor PhysicsActor { get { return m_scenePresence.PhysicsActor; } }

        public void StandUp()
        {
            m_scenePresence.StandUp();
        }

        public void UpdateMovementAnimations(bool p)
        {
            m_scenePresence.Animator.UpdateMovementAnimations(p);
        }

        public void OnBotAgentUpdate(Vector3 toward, uint controlFlag, Quaternion bodyRotation)
        {
            OnBotAgentUpdate(toward, controlFlag, bodyRotation, true);
        }

        public void OnBotAgentUpdate(Vector3 toward, uint controlFlag, Quaternion bodyRotation, bool isMoving)
        {
            if (isMoving)
                m_hasStoppedMoving = false;
            AgentUpdateArgs pack = new AgentUpdateArgs { ControlFlags = controlFlag, BodyRotation = bodyRotation };
            m_scenePresence.ControllingClient.ForceSendOnAgentUpdate(m_scenePresence.ControllingClient, pack);
        }

        public UUID UUID { get { return m_scenePresence.UUID; } }

        #region Chat interface

        public void SendChatMessage(int sayType, string message, int channel)
        {
            if (m_scenePresence == null || m_scenePresence.Scene == null)
                return;
            OSChatMessage args = new OSChatMessage
            {
                Message = message,
                Channel = channel,
                From = m_scenePresence.Name,
                Position = m_scenePresence.AbsolutePosition,
                Sender = m_scenePresence.ControllingClient,
                Type = (ChatTypeEnum)sayType,
                Scene = m_scenePresence.Scene
            };

            m_scenePresence.ControllingClient.OnForceChatFromViewer(m_scenePresence.ControllingClient, args);
        }

        public void SendInstantMessage(GridInstantMessage im)
        {
            if (im.dialog == (byte)InstantMessageDialog.GodLikeRequestTeleport ||
                im.dialog == (byte)InstantMessageDialog.RequestTeleport)
            {
                if (m_bot.AvatarCreatorID == im.fromAgentID || m_scenePresence.Scene.Permissions.IsGod(im.fromAgentID))
                {
                    ulong handle = 0;
                    uint x = 128;
                    uint y = 128;
                    uint z = 70;

                    Util.ParseFakeParcelID(im.imSessionID, out handle, out x, out y, out z);
                    m_scenePresence.Teleport(new Vector3(x, y, z));
                }
            }
        }

        public void Close()
        {
            m_scenePresence.ControllingClient.Close(false);
        }

        #endregion

        public string Name { get { return m_scenePresence.Name; } }


        public void Jump()
        {
            m_bot.WalkTo(m_scenePresence.AbsolutePosition + new Vector3(0, 0, 2));
        }
    }

    #endregion

    public sealed class Bot 
    {
        #region Declares

        public bool m_allowJump = true;
        public bool m_UseJumpDecisionTree = true;

        public bool m_paused;

        public uint m_movementFlag;
        public Quaternion m_bodyDirection = Quaternion.Identity;

        private IBotController m_controller;

        public IBotController Controller
        {
            get { return m_controller; }
        }

        /// <summary>
        ///   There are several events added so far,
        ///   Update - called every 0.1s, allows for updating of the position of where the avatar is supposed to be goign
        ///   Move - called every 10ms, allows for subtle changes and fast callbacks before the avatar moves toward its next location
        ///   ToAvatar - a following event, called when the bot is within range of the avatar (range = m_followCloseToPoint)
        ///   LostAvatar - a following event, called when the bot is out of the maximum range to look for its avatar (range = m_followLoseAvatarDistance)
        ///   HereEvent - Triggered when a script passes TRIGGER_HERE_EVENT via botSetMap
        ///   ChangedState = Triggered when the state of a bot changes
        /// </summary>
        public AuroraEventManager EventManager = new AuroraEventManager();

        public BotState m_currentState = BotState.Idle;
        public BotState m_previousState = BotState.Idle;

        public BotState State
        {
            get { return m_currentState; }
            set
            {
                if (m_currentState != value)
                {
                    m_previousState = m_currentState;
                    m_currentState = value;
                    EventManager.FireGenericEventHandler("ChangedState", null);
                }
            }
        }

        public bool lastFlying;

        #region Jump Settings

        public bool AllowJump
        {
            get { return m_allowJump; }
            set { m_allowJump = value; }
        }

        public bool UseJumpDecisionTree
        {
            get { return m_UseJumpDecisionTree; }
            set { m_UseJumpDecisionTree = value; }
        }

        #endregion

        public Timer m_frames;
        public int m_frame;

        #region IClientAPI properties

        private UUID m_avatarCreatorID = UUID.Zero;

        public UUID AvatarCreatorID
        {
            get { return m_avatarCreatorID; }
        }

        public bool ShouldFly;
        public bool m_forceDirectFollowing = false;
        public float m_closeToPoint = 1;

        public readonly NodeGraph m_nodeGraph = new NodeGraph();

        private float m_RexCharacterSpeedMod = 1.0f;

        public float RexCharacterSpeedMod
        {
            get { return m_RexCharacterSpeedMod; }
            set { m_RexCharacterSpeedMod = value; }
        }

        #endregion

        #endregion

        #region Initialize/Close

        public void Initialize(IScenePresence SP, UUID creatorID)
        {
            m_controller = new BotAvatarController(SP, this);
            m_controller.SetDrawDistance(1024f);
            m_avatarCreatorID = creatorID;
            m_frames = new Timer(10);
            m_frames.Elapsed += (frame_Elapsed);
            m_frames.Start();
        }

        public void Initialize(ISceneEntity entity)
        {
            m_controller = new BotPrimController(entity, this);
            m_avatarCreatorID = entity.OwnerID;
            m_frames = new Timer(10);
            m_frames.Elapsed += (frame_Elapsed);
            m_frames.Start();
        }

        public void Close(bool forceKill)
        {
            m_controller.Close();
            // Pull Client out of Region
            m_controller = null;

            m_frames.Stop();
        }

        #endregion

        #region SetPath

        public void SetPath(List<Vector3> Positions, List<TravelMode> modes, int flags)
        {
            m_nodeGraph.Clear();
            const int BOT_FOLLOW_FLAG_INDEFINITELY = 1;
            const int BOT_FOLLOW_FLAG_FORCEDIRECTPATH = 4;

            m_nodeGraph.FollowIndefinitely = (flags & BOT_FOLLOW_FLAG_INDEFINITELY) == BOT_FOLLOW_FLAG_INDEFINITELY;
            m_forceDirectFollowing = (flags & BOT_FOLLOW_FLAG_FORCEDIRECTPATH) == BOT_FOLLOW_FLAG_FORCEDIRECTPATH;
            m_nodeGraph.AddRange(Positions, modes);
            GetNextDestination();
        }

        #endregion

        #region Set Av Speed

        public void SetMovementSpeedMod(float speed)
        {
            m_controller.SetSpeedModifier(speed);
        }

        #endregion

        #region Move/Rotate the bot

        // Makes the bot walk to the specified destination
        public void WalkTo(Vector3 destination)
        {
            if (!Util.IsZeroVector(destination - m_controller.AbsolutePosition))
            {
                walkTo(destination);
                State = BotState.Walking;
                lastFlying = false;
            }
        }

        // Makes the bot fly to the specified destination
        public void FlyTo(Vector3 destination)
        {
            if (Util.IsZeroVector(destination - m_controller.AbsolutePosition) == false)
            {
                flyTo(destination);
                State = BotState.Flying;
                lastFlying = true;
            }
            else
            {
                m_movementFlag = (uint) AgentManager.ControlFlags.AGENT_CONTROL_AT_POS;

                m_controller.OnBotAgentUpdate(Vector3.Zero, m_movementFlag, m_bodyDirection);
                m_movementFlag = (uint) AgentManager.ControlFlags.NONE;
            }
        }

        private void RotateTo(Vector3 destination)
        {
            Vector3 bot_forward = new Vector3(1, 0, 0);
            if (destination - m_controller.AbsolutePosition != Vector3.Zero)
            {
                Vector3 bot_toward = Util.GetNormalizedVector(destination - m_controller.AbsolutePosition);
                Quaternion rot_result = llRotBetween(bot_forward, bot_toward);
                m_bodyDirection = rot_result;
            }
            m_movementFlag = (uint) AgentManager.ControlFlags.AGENT_CONTROL_AT_POS;

            m_controller.OnBotAgentUpdate(Vector3.Zero, m_movementFlag, m_bodyDirection);
            m_movementFlag = (uint) AgentManager.ControlFlags.NONE;
        }

        #region rotation helper functions

        private Vector3 llRot2Fwd(Quaternion r)
        {
            return (new Vector3(1, 0, 0)*r);
        }

        private Quaternion llRotBetween(Vector3 a, Vector3 b)
        {
            //A and B should both be normalized
            double dotProduct = Vector3.Dot(a, b);
            Vector3 crossProduct = Vector3.Cross(a, b);
            double magProduct = Vector3.Distance(Vector3.Zero, a)*Vector3.Distance(Vector3.Zero, b);
            double angle = Math.Acos(dotProduct/magProduct);
            Vector3 axis = Vector3.Normalize(crossProduct);
            float s = (float) Math.Sin(angle/2);

            return new Quaternion(axis.X*s, axis.Y*s, axis.Z*s, (float) Math.Cos(angle/2));
        }

        #endregion

        #region Move / fly bot

        /// <summary>
        ///   Does the actual movement of the bot
        /// </summary>
        /// <param name = "pos"></param>
        private void walkTo(Vector3 pos)
        {
            Vector3 bot_forward = new Vector3(2, 0, 0);
            Vector3 bot_toward = Vector3.Zero;
            if (pos - m_controller.AbsolutePosition != Vector3.Zero)
            {
                try
                {
                    bot_toward = Util.GetNormalizedVector(pos - m_controller.AbsolutePosition);
                    Quaternion rot_result = llRotBetween(bot_forward, bot_toward);
                    m_bodyDirection = rot_result;
                }
                catch (ArgumentException)
                {
                }
            }
            m_movementFlag = (uint) AgentManager.ControlFlags.AGENT_CONTROL_AT_POS;

            if (m_controller.CanMove)
                m_controller.OnBotAgentUpdate(bot_toward, m_movementFlag, m_bodyDirection);
            else
                m_controller.OnBotAgentUpdate(Vector3.Zero, (uint)AgentManager.ControlFlags.AGENT_CONTROL_STOP, Quaternion.Identity);

            m_movementFlag = (uint) AgentManager.ControlFlags.NONE;
        }

        /// <summary>
        ///   Does the actual movement of the bot
        /// </summary>
        /// <param name = "pos"></param>
        private void flyTo(Vector3 pos)
        {
            Vector3 bot_forward = new Vector3(1, 0, 0), bot_toward = Vector3.Zero;
            if (pos - m_controller.AbsolutePosition != Vector3.Zero)
            {
                try
                {
                    bot_toward = Util.GetNormalizedVector(pos - m_controller.AbsolutePosition);
                    Quaternion rot_result = llRotBetween(bot_forward, bot_toward);
                    m_bodyDirection = rot_result;
                }
                catch (ArgumentException)
                {
                }
            }

            m_movementFlag = (uint) AgentManager.ControlFlags.AGENT_CONTROL_FLY;

            Vector3 diffPos = pos - m_controller.AbsolutePosition;
            if (Math.Abs(diffPos.X) > 1.5 || Math.Abs(diffPos.Y) > 1.5)
            {
                m_movementFlag |= (uint) AgentManager.ControlFlags.AGENT_CONTROL_AT_POS;
            }

            if (m_controller.AbsolutePosition.Z < pos.Z - 1)
            {
                m_movementFlag |= (uint) AgentManager.ControlFlags.AGENT_CONTROL_UP_POS;
            }
            else if (m_controller.AbsolutePosition.Z > pos.Z + 1)
            {
                m_movementFlag |= (uint) AgentManager.ControlFlags.AGENT_CONTROL_UP_NEG;
            }

            if (m_controller.CanMove)
                m_controller.OnBotAgentUpdate(bot_toward, m_movementFlag, m_bodyDirection);
            m_movementFlag = (uint) AgentManager.ControlFlags.AGENT_CONTROL_FLY;
        }

        #region Jump Decision Tree

        /// <summary>
        ///   See whether we should jump based on the start and end positions given
        /// </summary>
        /// <param name = "start"></param>
        /// <param name = "end"></param>
        /// <returns></returns>
        private bool JumpDecisionTree(Vector3 start, Vector3 end)
        {
            //Cast a ray in the direction that we are going
            List<ISceneChildEntity> entities = llCastRay(start, end);
            foreach (ISceneChildEntity entity in entities)
            {
                if (!entity.IsAttachment)
                {
                    if (entity.Scale.Z > m_controller.PhysicsActor.Size.Z) return true;
                }
            }
            return false;
        }

        public List<ISceneChildEntity> llCastRay(Vector3 start, Vector3 end)
        {
            Vector3 dir = new Vector3((end - start).X, (end - start).Y, (end - start).Z);
            Vector3 startvector = new Vector3(start.X, start.Y, start.Z);
            Vector3 endvector = new Vector3(end.X, end.Y, end.Z);

            List<ISceneChildEntity> entities = new List<ISceneChildEntity>();
            List<ContactResult> results = m_controller.GetScene().PhysicsScene.RaycastWorld(startvector, dir, dir.Length(),
                                                                                          5);

            double distance = Util.GetDistanceTo(startvector, endvector);
            if (distance == 0)
                distance = 0.001;
            Vector3 posToCheck = startvector;
            foreach (ContactResult result in results)
            {
                ISceneChildEntity child = m_controller.GetScene().GetSceneObjectPart(result.ConsumerID);
                if (!entities.Contains(child))
                {
                    entities.Add(child);
                }
            }
            return entities;
        }

        #endregion

        #endregion

        #endregion

        #region Enable/Disable walking

        /// <summary>
        ///   Blocks walking and sets to only flying
        /// </summary>
        /// <param name = "pos"></param>
        public void DisableWalk()
        {
            ShouldFly = true;
            m_controller.ForceFly = true;
            m_closeToPoint = 1.5f;
        }

        /// <summary>
        ///   Allows for flying and walkin
        /// </summary>
        /// <param name = "pos"></param>
        public void EnableWalk()
        {
            ShouldFly = false;
            m_controller.ForceFly = false;
            m_closeToPoint = 1;
        }

        #endregion

        #region Timers

        private void frame_Elapsed(object sender, ElapsedEventArgs e)
        {
            m_frames.Stop();
            if (m_controller == null)
                return;

            m_frame++;
            GetNextDestination();

            if (m_frame%10 == 0) //Only every 10 frames
            {
                m_frame = 0;
                Update();
            }
            m_frames.Start();
        }

        #endregion

        #region Get next destination / Pause/Resume movement

        public bool ForceCloseToPoint = false;

        public void GetNextDestination()
        {
            //Fire the move event
            EventManager.FireGenericEventHandler("Move", null);

            if (m_controller == null || m_controller.PhysicsActor == null)
                return;
            if (m_paused)
            {
                m_controller.StopMoving(lastFlying, false);
                return;
            }

            Vector3 pos;
            TravelMode state;
            bool teleport;
            if(!ForceCloseToPoint)
                m_closeToPoint = m_controller.PhysicsActor is PhysicsCharacter && ((PhysicsCharacter)m_controller.PhysicsActor).Flying ? 1.5f : 1.0f;
            if (m_nodeGraph.GetNextPosition(m_controller.AbsolutePosition, m_closeToPoint, 60, out pos, out state,
                                            out teleport))
            {
                if (state == TravelMode.Fly)
                    FlyTo(pos);
                else if (state == TravelMode.Run)
                {
                    m_controller.SetAlwaysRun = true;
                    WalkTo(pos);
                }
                else if (state == TravelMode.Walk)
                {
                    m_controller.SetAlwaysRun = false;
                    WalkTo(pos);
                }
                else if (state == TravelMode.Teleport)
                {
                    m_controller.Teleport(pos);
                    m_nodeGraph.CurrentPos++;
                }
                else if (state == TravelMode.TriggerHereEvent)
                {
                    EventManager.FireGenericEventHandler("HereEvent", null);
                }
            }
            else
                m_controller.StopMoving(lastFlying, true);
        }

        public void PauseMovement()
        {
            m_paused = true;
        }

        public void ResumeMovement()
        {
            m_paused = false;
        }

        #endregion

        #region Update Event

        public void Update()
        {
            if (m_paused)
                return;
            //Tell any interested modules that we are ready to go
            EventManager.FireGenericEventHandler("Update", null);
        }

        #endregion

        #region FindPath

        public List<Vector3> InnerFindPath(int[,] map, int startX, int startY, int finishX, int finishY)
        {
            StartPath.Map = map;
            StartPath.xLimit = (int) Math.Sqrt(map.Length);
            StartPath.yLimit = (int) Math.Sqrt(map.Length);
            //ShowMap ("", null);
            List<string> points = StartPath.Path(startX, startY, finishX, finishY, 0, 0, 0);

            List<Vector3> waypoints = new List<Vector3>();
            if (points.Contains("no_path"))
            {
                MainConsole.Instance.Debug("I'm sorry I could not find a solution to that path. Teleporting instead");
                return waypoints;
            }

            waypoints.AddRange(from s in points
                               select s.Split(',')
                               into Vector where Vector.Length == 3 select new Vector3(float.Parse(Vector[0]), float.Parse(Vector[1]), float.Parse(Vector[2])));
            return waypoints;
        }

        #endregion

        #region Following Code

        #region Following declares

        public IScenePresence FollowSP;
        public Vector3 FollowOffset;
        public bool FollowRequiresLOS = false;
        public UUID FollowUUID = UUID.Zero;
        private float m_StopFollowDistance = 2f;
        private float m_StartFollowDistance = 3f;
        private float m_followLoseAvatarDistance = 1000;
        private const float FollowTimeBeforeUpdate = 10;
        private int jumpTry;
        private bool m_lostAvatar;

        public float StartFollowDistance
        {
            get { return m_StartFollowDistance; }
            set { m_StartFollowDistance = value; }
        }

        public float StopFollowDistance
        {
            get { return m_StopFollowDistance; }
            set { m_StopFollowDistance = value; }
        }

        public float FollowLoseAvatarDistance
        {
            get { return m_followLoseAvatarDistance; }
            set { m_followLoseAvatarDistance = value; }
        }

        #endregion

        #region Interface members

        public void FollowAvatar(string avatarName, float startFollowDistance, float stopFollowDistance, Vector3 offsetFromUser, bool requireLOS)
        {
            m_controller.GetScene().TryGetAvatarByName(avatarName, out FollowSP);
            if (FollowSP == null)
            {
                //Try by UUID then
                try
                {
                    m_controller.GetScene().TryGetScenePresence(UUID.Parse(avatarName), out FollowSP);
                }
                catch
                {
                }
            }
            if (FollowSP == null || FollowSP.IsChildAgent)
            {
                MainConsole.Instance.Warn("Could not find avatar " + avatarName + " for bot " + m_controller.Name + " to follow");
                return;
            }
            FollowRequiresLOS = requireLOS;
            FollowSP.PhysicsActor.OnRequestTerseUpdate += EventManager_OnClientMovement;
            FollowUUID = FollowSP.UUID;
            FollowOffset = offsetFromUser;
            EventManager.RegisterEventHandler("Update", FollowingUpdate);
            EventManager.RegisterEventHandler("Move", FollowingMove);
            m_controller.StandUp(); //Can't follow if sitting
            StartFollowDistance = startFollowDistance;
            StopFollowDistance = stopFollowDistance;
        }

        public void StopFollowAvatar()
        {
            EventManager.UnregisterEventHandler("Update", FollowingUpdate);
            if (FollowSP != null)
                FollowSP.PhysicsActor.OnRequestTerseUpdate -= EventManager_OnClientMovement;
            FollowSP = null; //null out everything
            FollowUUID = UUID.Zero;
        }

        #endregion

        #region Following Update Event

        private object FollowingUpdate(string functionName, object param)
        {
            //Update, time to check where we should be going
            FollowDecision();
            return null;
        }

        private object FollowingMove(string functionName, object param)
        {
            if (FollowSP == null)
                return null;
            //Check to see whether we are close to our avatar, and fire the event if needed
            Vector3 targetPos = FollowSP.AbsolutePosition + FollowOffset;
            Vector3 currentPos2 = m_controller.AbsolutePosition;
            double distance = Util.GetDistanceTo(targetPos, currentPos2);
            float closeToPoint = m_toAvatar ? StartFollowDistance : StopFollowDistance;
            //Fix how we are running
            m_controller.SetAlwaysRun = FollowSP.SetAlwaysRun;
            if (distance < closeToPoint)
            {
                //Fire our event once
                if (!m_toAvatar) //Changed
                {
                    EventManager.FireGenericEventHandler("ToAvatar", null);
                    //Fix the animation
                    m_controller.UpdateMovementAnimations(false);
                }
                m_toAvatar = true;
                bool fly = FollowSP.PhysicsActor == null ? ShouldFly : FollowSP.PhysicsActor.Flying;
                m_controller.StopMoving(fly, true);
                return null;
            }
            if (distance > m_followLoseAvatarDistance)
            {
                //Lost the avatar, fire the event
                if (!m_lostAvatar)
                {
                    EventManager.FireGenericEventHandler("LostAvatar", null);
                    //We stopped, fix the animation
                    m_controller.UpdateMovementAnimations(false);
                }
                m_lostAvatar = true;
                m_paused = true;
            }
            else if (m_lostAvatar)
            {
                m_lostAvatar = false;
                m_paused = false; //Fixed pause status, avatar entered our range again
            }
            m_toAvatar = false;
            return null;
        }

        #endregion

        #region Following Decision

        private void FollowDecision()
        {
            // FOLLOW an avatar - this is looking for an avatar UUID so wont follow a prim here  - yet
            //Call this each iteration so that if the av leaves, we don't get stuck following a null person
            FollowSP = m_controller.GetScene().GetScenePresence(FollowUUID);
            //If its still null, the person doesn't exist, cancel the follow and return
            if (FollowSP == null)
                return;

            Vector3 targetPos = FollowSP.AbsolutePosition + FollowOffset;
            Vector3 currentPos2 = m_controller.AbsolutePosition;

            resolution = 3;
            double distance = Util.GetDistanceTo(targetPos, currentPos2);
            List<ISceneChildEntity> raycastEntities = llCastRay(m_controller.AbsolutePosition,
                                                                FollowSP.AbsolutePosition);
            float closeToPoint = m_toAvatar ? StartFollowDistance : StopFollowDistance;
            if (FollowRequiresLOS && raycastEntities.Count > 0)
            {
                //Lost the avatar, fire the event
                if (!m_lostAvatar)
                {
                    EventManager.FireGenericEventHandler("LostAvatar", null);
                    //We stopped, fix the animation
                    m_controller.UpdateMovementAnimations(false);
                }
                m_lostAvatar = true;
                m_paused = true;
                return;
            }
            if (distance > 10) //Greater than 10 meters, give up
            {
                //Try direct then, since it is way out of range
                DirectFollowing();
            }
            else if (distance < closeToPoint && raycastEntities.Count == 0)
                //If the raycastEntities isn't zero, there is something between us and the avatar, don't stop on the other side of walls, etc
            {
                //We're here!
                //If we were having to fly to here, stop flying
                if (jumpTry > 0)
                {
                    if(m_controller.PhysicsActor is PhysicsCharacter)
                        ((PhysicsCharacter)m_controller.PhysicsActor).Flying = false;
                    walkTo(m_controller.AbsolutePosition);
                    //Fix the animation from flying > walking
                    m_controller.UpdateMovementAnimations(false);
                }
                jumpTry = 0;
            }
            else
            {
                if (raycastEntities.Count == 0)
                    //Nothing between us and the target, go for it!
                    DirectFollowing();
                else
                    //if (!BestFitPathFollowing (raycastEntities))//If this doesn't work, try significant positions
                    SignificantPositionFollowing();
            }
            ClearOutInSignificantPositions(false);
        }

        #endregion

        #region Direct Following code

        private void DirectFollowing()
        {
            if (m_controller == null)
                return;
            Vector3 diffAbsPos = (FollowSP.AbsolutePosition + FollowOffset) - m_controller.AbsolutePosition;
            Vector3 targetPos = FollowSP.AbsolutePosition + FollowOffset;
            Vector3 ourPos = m_controller.AbsolutePosition;
            bool fly = FollowSP.PhysicsActor == null ? ShouldFly : FollowSP.PhysicsActor.Flying;
            if (!fly && (diffAbsPos.Z > 0.25 || jumpTry > 5))
            {
                if (jumpTry > 5 || diffAbsPos.Z > 3)
                {
                    if (jumpTry <= 5)
                        jumpTry = 6;
                    fly = true;
                }
                else
                {
                    if (!m_allowJump)
                    {
                        jumpTry--;
                        targetPos.Z = ourPos.Z + 0.15f;
                    }
                    else if (m_UseJumpDecisionTree)
                    {
                        if (!JumpDecisionTree(m_controller.AbsolutePosition, targetPos))
                        {
                            jumpTry--;
                            targetPos.Z = ourPos.Z + 0.15f;
                        }
                        else
                        {
                            if (jumpTry < 0)
                                jumpTry = 0;
                            jumpTry++;
                        }
                    }
                    else
                        jumpTry--;
                }
            }
            else if (!fly)
            {
                if (diffAbsPos.Z < -3)
                {
                    //We should fly down to the avatar, rather than fall
                    //We also know that because this is the old, we have no entities in our way
                    //(unless this is > 10m, but that case is messed up anyway, needs dealt with later)
                    //so we can assume that it is safe to fly
                    fly = true;
                }
                jumpTry--;
            }
            m_nodeGraph.Clear();
            m_nodeGraph.Add(targetPos, fly ? TravelMode.Fly : TravelMode.Walk);
        }

        #endregion

        #region BestFitPath Following code

        private void ShowMap(string mod, string[] cmd)
        {
            int sqrt = (int) Math.Sqrt(map.Length);
            for (int x = sqrt - 1; x > -1; x--)
            {
                string line = "";
                for (int y = sqrt - 1; y > -1; y--)
                {
                    if (x == 11*resolution && y == 11*resolution)
                    {
                        line += "XX" + ",";
                    }
                    else
                    {
                        if (map[x, y].ToString().Length < 2)
                            line += " " + map[x, y] + ",";
                        else
                            line += map[x, y] + ",";
                    }
                }

                MainConsole.Instance.Warn(line.Remove(line.Length - 1));
            }
            MainConsole.Instance.Warn("\n");
        }

        private int resolution = 10;
        private int[,] map;
        private int failedToMove;
        private int sincefailedToMove;
        private Vector3 m_lastPos = Vector3.Zero;
        private bool m_toAvatar;

        private bool BestFitPathFollowing(List<ISceneChildEntity> raycastEntities)
        {
            Vector3 targetPos = FollowSP.AbsolutePosition + FollowOffset;
            Vector3 currentPos2 = m_controller.AbsolutePosition;

            ISceneEntity[] entities = new ISceneEntity[raycastEntities.Count];
            int ii = 0;
            foreach (ISceneChildEntity child in raycastEntities)
            {
                entities[ii] = child.ParentEntity;
                ii++;
            }

            map = new int[22*resolution,22*resolution]; //10 * resolution squares in each direction from our pos
            //We are in the center (11, 11) and our target is somewhere else
            int targetX = 11*resolution, targetY = 11*resolution;
            //Find where our target is on the map
            FindTargets(currentPos2, targetPos, ref targetX, ref targetY);
            //ISceneEntity[] entities = m_scenePresence.Scene.Entities.GetEntities (currentPos, 30);

            //Add all the entities to the map
            foreach (ISceneEntity entity in entities)
            {
                //if (entity.AbsolutePosition.Z < m_scenePresence.AbsolutePosition.Z + m_scenePresence.PhysicsActor.Size.Z / 2 + m_scenePresence.Velocity.Z / 2 &&
                //    entity.AbsolutePosition.Z > m_scenePresence.AbsolutePosition.Z - m_scenePresence.PhysicsActor.Size.Z / 2 + m_scenePresence.Velocity.Z / 2)
                {
                    int entitybaseX = (11*resolution);
                    int entitybaseY = (11*resolution);
                    //Find the bottom left corner, and then build outwards from it
                    FindTargets(currentPos2, entity.AbsolutePosition - (entity.OOBsize/2), ref entitybaseX,
                                ref entitybaseY);
                    for (int x = (int) -(0.5*resolution);
                         x < entity.OOBsize.X*2*resolution + ((int) (0.5*resolution));
                         x++)
                    {
                        for (int y = (int) -(0.5*resolution);
                             y < entity.OOBsize.Y*2*resolution + ((int) (0.5*resolution));
                             y++)
                        {
                            if (entitybaseX + x > 0 && entitybaseY + y > 0 &&
                                entitybaseX + x < (22*resolution) && entitybaseY + y < (22*resolution))
                                if (x < 0 || y < 0 || x > (entity.OOBsize.X*2)*resolution ||
                                    y > (entity.OOBsize.Y*2)*resolution)
                                    map[entitybaseX + x, entitybaseY + y] = 3; //Its a side hit, lock it down a bit
                                else
                                    map[entitybaseX + x, entitybaseY + y] = -1; //Its a hit, lock it down
                        }
                    }
                }
            }

            for (int x = 0; x < (22*resolution); x++)
            {
                for (int y = 0; y < (22*resolution); y++)
                {
                    if (x == targetX && y == targetY)
                        map[x, y] = 1;
                    else if (x == 11*resolution && y == 11*resolution)
                    {
                        int old = map[x, y];
                        map[x, y] = 1;
                    }
                    else if (map[x, y] == 0)
                        map[x, y] = 1;
                }
            }

            //ShowMap ("", null);
            List<Vector3> path = InnerFindPath(map, (11*resolution), (11*resolution), targetX, targetY);

            int i = 0;
            Vector3 nextPos = ConvertPathToPos(raycastEntities.ToArray(), entities, currentPos2, path, ref i);
            Vector3 diffAbsPos = nextPos - targetPos;
            if (nextPos != Vector3.Zero)
            {
                m_nodeGraph.Clear();
            }
            else
            {
                //Try another way
                return false;
            }
            bool fly = FollowSP.PhysicsActor == null ? ShouldFly : FollowSP.PhysicsActor.Flying;
            while (nextPos != Vector3.Zero)
            {
                if (!fly && (diffAbsPos.Z < -0.25 || jumpTry > 5))
                {
                    if (jumpTry > 5 || diffAbsPos.Z < -3)
                    {
                        if (jumpTry <= 5)
                            jumpTry = 6;
                        fly = true;
                    }
                    else
                    {
                        if (!m_allowJump)
                        {
                            jumpTry--;
                            targetPos.Z = nextPos.Z + 0.15f;
                        }
                        else if (m_UseJumpDecisionTree)
                        {
                            if (!JumpDecisionTree(m_controller.AbsolutePosition, targetPos))
                            {
                                jumpTry--;
                                targetPos.Z = nextPos.Z + 0.15f;
                            }
                            else
                            {
                                if (jumpTry < 0)
                                    jumpTry = 0;
                                jumpTry++;
                            }
                        }
                        else
                            jumpTry--;
                    }
                }
                else if (!fly)
                {
                    if (diffAbsPos.Z > 3)
                    {
                        //We should fly down to the avatar, rather than fall
                        fly = true;
                    }
                    jumpTry--;
                }
                nextPos.Z = targetPos.Z; //Fix the Z coordinate

                m_nodeGraph.Add(nextPos, fly ? TravelMode.Fly : TravelMode.Walk);
                i++;
                nextPos = ConvertPathToPos(raycastEntities.ToArray(), entities, currentPos2, path, ref i);
            }
            return true;
        }

        private Vector3 ConvertPathToPos(ISceneChildEntity[] raycastEntities, ISceneEntity[] entites,
                                         Vector3 originalPos, List<Vector3> path, ref int i)
        {
            start:
            if (i == path.Count)
                return Vector3.Zero;
            if (path[i].X == (11*resolution) && path[i].Y == (11*resolution))
            {
                i++;
                goto start;
            }
            Vector3 pos = path[i];
            Vector3 newPos = originalPos -
                             new Vector3(((11*resolution) - pos.X)/resolution, ((11*resolution) - pos.Y)/resolution, 0);

            if (i < 2)
            {
                if (m_lastPos.ApproxEquals(newPos, 1))
                    failedToMove++;
                else if (failedToMove < 2)
                {
                    m_lastPos = newPos;
                    failedToMove = 0;
                }
                else
                {
                    if (sincefailedToMove == 5)
                    {
                        sincefailedToMove = 0;
                        failedToMove = 1;
                    }
                    else if (!m_lastPos.ApproxEquals(newPos, 2))
                        sincefailedToMove++;
                }
            }
            if (failedToMove > 1)
            {
                return Vector3.Zero;
                //CleanUpPos (raycastEntities, entites, ref newPos);
            }
            return newPos;
        }

        private void CleanUpPos(ISceneChildEntity[] raycastEntities, ISceneEntity[] entites, ref Vector3 pos)
        {
            List<ISceneChildEntity> childEntities = llCastRay(m_controller.AbsolutePosition, pos);
            childEntities.AddRange(raycastEntities); //Add all of the ones that are in between us and the avatar as well
            int restartNum = 0;
            restart:
            bool needsRestart = false;
            foreach (ISceneChildEntity entity in childEntities)
            {
                if (entity.AbsolutePosition.Z < m_controller.AbsolutePosition.Z + 2 &&
                    entity.AbsolutePosition.Z > m_controller.AbsolutePosition.Z - 2)
                {
                    //If this position is inside an entity + its size + avatar size, move it out!
                    float sizeXPlus = (entity.AbsolutePosition.X + (entity.Scale.X/2) +
                                       (m_controller.PhysicsActor.Size.X/2));
                    float sizeXNeg = (entity.AbsolutePosition.X - (entity.Scale.X/2) -
                                      (m_controller.PhysicsActor.Size.X/2));
                    float sizeYPlus = (entity.AbsolutePosition.Y + (entity.Scale.Y/2) +
                                       (m_controller.PhysicsActor.Size.Y/2));
                    float sizeYNeg = (entity.AbsolutePosition.Y - (entity.Scale.Y/2) -
                                      (m_controller.PhysicsActor.Size.Y/2));

                    if (pos.X < sizeXPlus && pos.X > sizeXNeg)
                    {
                        if (pos.X < entity.AbsolutePosition.X)
                            pos.X = sizeXNeg - (m_controller.PhysicsActor.Size.X/2);
                        else
                            pos.X = sizeXPlus + (m_controller.PhysicsActor.Size.X/2);
                        needsRestart = true;
                    }
                    if (pos.Y < sizeYPlus && pos.Y > sizeYNeg)
                    {
                        if (pos.Y < entity.AbsolutePosition.Y)
                            pos.Y = sizeYNeg - (m_controller.PhysicsActor.Size.Y/2);
                        else
                            pos.Y = sizeYPlus + (m_controller.PhysicsActor.Size.Y/2);
                        needsRestart = true;
                    }
                }
            }
            //If we changed something, we need to recheck the pos...
            if (needsRestart && restartNum < 3)
            {
                restartNum++;
                goto restart;
            }
        }

        private void FindTargets(Vector3 currentPos, Vector3 targetPos, ref int targetX, ref int targetY)
        {
            //we're at pos 11, 11, so we have to add/subtract from there
            float xDiff = (targetPos.X - currentPos.X);
            float yDiff = (targetPos.Y - currentPos.Y);

            targetX += (int) (xDiff*resolution);
            targetY += (int) (yDiff*resolution);
        }

        #endregion

        #region Significant Client Movement Following code

        private List<Vector3> m_significantAvatarPositions = new List<Vector3>();
        private int currentPos;

        private void EventManager_OnClientMovement()
        {
            if (FollowSP != null)
                lock (m_significantAvatarPositions)
                    m_significantAvatarPositions.Add(FollowSP.AbsolutePosition);
        }

        private void ClearOutInSignificantPositions(bool checkPositions)
        {
            int closestPosition = 0;
            double closestDistance = 0;
            Vector3[] sigPos;
            lock (m_significantAvatarPositions)
            {
                sigPos = new Vector3[m_significantAvatarPositions.Count];
                m_significantAvatarPositions.CopyTo(sigPos);
            }

            for (int i = 0; i < sigPos.Length; i++)
            {
                double val = Util.GetDistanceTo(m_controller.AbsolutePosition, sigPos[i]);
                if (closestDistance == 0 || closestDistance > val)
                {
                    closestDistance = val;
                    closestPosition = i;
                }
            }
            if (currentPos > closestPosition)
            {
                currentPos = closestPosition + 2;
                //Going backwards? We must have no idea where we are
            }
            else //Going forwards in the line, all good
                currentPos = closestPosition + 2;

            //Remove all insignificant
            List<Vector3> vectors = new List<Vector3>();
            for (int i = sigPos.Length - 50; i < sigPos.Length; i++)
            {
                if (i < 0)
                    continue;
                vectors.Add(sigPos[i]);
            }
            m_significantAvatarPositions = vectors;
        }

        private void SignificantPositionFollowing()
        {
            //Do this first
            ClearOutInSignificantPositions(true);

            bool fly = FollowSP.PhysicsActor == null ? ShouldFly : FollowSP.PhysicsActor.Flying;
            if (m_significantAvatarPositions.Count > 0 && currentPos + 1 < m_significantAvatarPositions.Count)
            {
                m_nodeGraph.Clear();

                Vector3 targetPos = m_significantAvatarPositions[currentPos + 1];
                Vector3 diffAbsPos = targetPos - m_controller.AbsolutePosition;
                if (!fly && (diffAbsPos.Z < -0.25 || jumpTry > 5))
                {
                    if (jumpTry > 5 || diffAbsPos.Z < -3)
                    {
                        if (jumpTry <= 5)
                            jumpTry = 6;
                        fly = true;
                    }
                    else
                    {
                        if (!m_allowJump)
                        {
                            jumpTry--;
                            targetPos.Z = m_controller.AbsolutePosition.Z + 0.15f;
                        }
                        else if (m_UseJumpDecisionTree)
                        {
                            if (!JumpDecisionTree(m_controller.AbsolutePosition, targetPos))
                            {
                                jumpTry--;
                                targetPos.Z = m_controller.AbsolutePosition.Z + 0.15f;
                            }
                            else
                            {
                                if (jumpTry < 0)
                                    jumpTry = 0;
                                jumpTry++;
                            }
                        }
                        else
                            jumpTry--;
                    }
                }
                else if (!fly)
                {
                    if (diffAbsPos.Z > 3)
                    {
                        //We should fly down to the avatar, rather than fall
                        fly = true;
                    }
                    jumpTry--;
                }
                m_nodeGraph.Add(targetPos, fly ? TravelMode.Fly : TravelMode.Walk);
            }
        }

        #endregion

        #endregion

        #region Distance Event

        private readonly Dictionary<UUID, FollowingEvent> m_followDistanceEvents =
            new Dictionary<UUID, FollowingEvent>();

        private readonly Dictionary<UUID, float> m_followDistance = new Dictionary<UUID, float>();

        public void AddDistanceEvent(UUID avatarID, float distance, FollowingEvent ev)
        {
            m_followDistanceEvents[avatarID] = ev;
            m_followDistance[avatarID] = distance;
            if (m_followDistanceEvents.Count == 1) //Only the first time
                EventManager.RegisterEventHandler("Update", DistanceFollowUpdate);
        }

        public void RemoveDistanceEvent(UUID avatarID)
        {
            m_followDistanceEvents.Remove(avatarID);
            m_followDistance.Remove(avatarID);
            if (m_followDistanceEvents.Count == 0) //Only the first time
                EventManager.UnregisterEventHandler("Update", DistanceFollowUpdate);
        }

        private class FollowingEventHolder
        {
            public UUID AvID;
            public UUID BotID;
            public FollowingEvent Event;
        }

        public object DistanceFollowUpdate(string funct, object param)
        {
            List<FollowingEventHolder> events = (from kvp in m_followDistance
                                                 let sp = m_controller.GetScene().GetScenePresence(kvp.Key)
                                                 where sp != null
                                                 where Util.DistanceLessThan(sp.AbsolutePosition, m_controller.AbsolutePosition, kvp.Value)
                                                 select new FollowingEventHolder
                                                            {
                                                                Event = m_followDistanceEvents[kvp.Key], AvID = kvp.Key, BotID = m_controller.UUID
                                                            }).ToList();
            foreach (FollowingEventHolder h in events)
            {
                h.Event(h.AvID, h.BotID);
            }
            return null;
        }


        private readonly Dictionary<UUID, FollowingEvent> m_LineOfSightEvents = new Dictionary<UUID, FollowingEvent>();
        private readonly Dictionary<UUID, float> m_LineOfSight = new Dictionary<UUID, float>();

        public void AddLineOfSightEvent(UUID avatarID, float distance, FollowingEvent ev)
        {
            m_LineOfSightEvents[avatarID] = ev;
            m_LineOfSight[avatarID] = distance;
            if (m_followDistanceEvents.Count == 1) //Only the first time
                EventManager.RegisterEventHandler("Update", LineOfSightUpdate);
        }

        public void RemoveLineOfSightEvent(UUID avatarID)
        {
            m_LineOfSightEvents.Remove(avatarID);
            m_LineOfSight.Remove(avatarID);
            if (m_followDistanceEvents.Count == 0) //Only the first time
                EventManager.UnregisterEventHandler("Update", LineOfSightUpdate);
        }

        public object LineOfSightUpdate(string funct, object param)
        {
            List<FollowingEventHolder> events = (from kvp in m_LineOfSight
                                                 let sp = m_controller.GetScene().GetScenePresence(kvp.Key)
                                                 where sp != null
                                                 let entities = llCastRay(sp.AbsolutePosition, m_controller.AbsolutePosition)
                                                 where entities.Count == 0
                                                 where m_controller.AbsolutePosition.ApproxEquals(sp.AbsolutePosition, m_LineOfSight[kvp.Key])
                                                 select new FollowingEventHolder
                                                            {
                                                                Event = m_LineOfSightEvents[kvp.Key], AvID = kvp.Key, BotID = m_controller.UUID
                                                            }).ToList();
            foreach (FollowingEventHolder h in events)
            {
                h.Event(h.AvID, h.BotID);
            }
            return null;
        }

        #endregion

        #region Chat

        public void SendChatMessage(int sayType, string message, int channel)
        {
            m_controller.SendChatMessage(sayType, message, channel);
        }

        public void SendInstantMessage(GridInstantMessage gridInstantMessage)
        {
            m_controller.SendInstantMessage(gridInstantMessage);
        }

        #endregion
    }

    public class BotClientAPI : IClientAPI
    {
        public readonly AgentCircuitData m_circuitData;
        public readonly UUID m_myID = UUID.Random();
        public readonly IScene m_scene;
        private static UInt32 UniqueId = 1;
        private BotAvatarController m_controller;

        public UUID ScopeID
        {
            get;
            set;
        }

        public List<UUID> AllScopeIDs
        {
            get;
            set;
        }

        // creates new bot on the default location
        public BotClientAPI(IScene scene, AgentCircuitData data)
        {
            RegisterInterfaces();

            m_circuitData = data;
            m_scene = scene;
            AllScopeIDs = new List<UUID>();

            m_circuitCode = UniqueId;

            UniqueId++;
        }

        public void Initialize(BotAvatarController controller)
        {
            m_controller = controller;
        }

        public string m_firstName = "";
        public string m_lastName = "";
        public readonly Vector3 DEFAULT_START_POSITION = new Vector3(128, 128, 128);
        public Vector3 StartPos
        {
            get { return DEFAULT_START_POSITION; }
            set { }
        }

        public UUID AgentId
        {
            get { return m_myID; }
        }

        public string FirstName
        {
            get { return m_firstName; }
            set { m_firstName = value; }
        }

        public string LastName
        {
            get { return m_lastName; }
            set { m_lastName = value; }
        }

        private uint m_circuitCode;

        public uint CircuitCode
        {
            get { return m_circuitCode; }
            set { m_circuitCode = value; }
        }

        public String Name
        {
            get { return FirstName + " " + LastName; }
        }

        public IScene Scene
        {
            get { return m_scene; }
        }

        #region IClientAPI

        #region IClientCore Members

        private readonly Dictionary<Type, object> m_clientInterfaces = new Dictionary<Type, object>();

        public T Get<T>()
        {
            return (T)m_clientInterfaces[typeof(T)];
        }

        public bool TryGet<T>(out T iface)
        {
            if (m_clientInterfaces.ContainsKey(typeof(T)))
            {
                iface = (T)m_clientInterfaces[typeof(T)];
                return true;
            }
            iface = default(T);
            return false;
        }

        private void RegisterInterfaces()
        {
        }

        private void RegisterInterface<T>(T iface)
        {
            lock (m_clientInterfaces)
            {
                if (!m_clientInterfaces.ContainsKey(typeof(T)))
                {
                    m_clientInterfaces.Add(typeof(T), iface);
                }
            }
        }

        #endregion

#pragma warning disable 67
        public event Action<IClientAPI> OnLogout;
        public event ObjectPermissions OnObjectPermissions;

        public event MoneyTransferRequest OnMoneyTransferRequest;
        public event ParcelBuy OnParcelBuy;
        public event Action<IClientAPI> OnConnectionClosed;

        public event ImprovedInstantMessage OnInstantMessage;
        public event PreSendImprovedInstantMessage OnPreSendInstantMessage;
        public event ChatMessage OnChatFromClient;
        public event RezObject OnRezObject;
        public event ModifyTerrain OnModifyTerrain;
        public event BakeTerrain OnBakeTerrain;
        public event SetAppearance OnSetAppearance;
        public event AvatarNowWearing OnAvatarNowWearing;
        public event RezSingleAttachmentFromInv OnRezSingleAttachmentFromInv;
        public event UUIDNameRequest OnDetachAttachmentIntoInv;
        public event ObjectAttach OnObjectAttach;
        public event ObjectDeselect OnObjectDetach;
        public event ObjectDrop OnObjectDrop;
        public event StartAnim OnStartAnim;
        public event StopAnim OnStopAnim;
        public event LinkObjects OnLinkObjects;
        public event DelinkObjects OnDelinkObjects;
        public event RequestMapBlocks OnRequestMapBlocks;
        public event RequestMapName OnMapNameRequest;
        public event TeleportLocationRequest OnTeleportLocationRequest;
        public event TeleportLandmarkRequest OnTeleportLandmarkRequest;
        public event DisconnectUser OnDisconnectUser;
        public event RequestAvatarProperties OnRequestAvatarProperties;
        public event SetAlwaysRun OnSetAlwaysRun;
        public event DeRezObject OnDeRezObject;
        public event Action<IClientAPI> OnRegionHandShakeReply;
        public event GenericCall1 OnRequestWearables;
        public event GenericCall1 OnCompleteMovementToRegion;
        public event UpdateAgent OnAgentUpdate;
        public event AgentRequestSit OnAgentRequestSit;
        public event AgentSit OnAgentSit;
        public event AvatarPickerRequest OnAvatarPickerRequest;
        public event Action<IClientAPI> OnRequestAvatarsData;
        public event AddNewPrim OnAddPrim;
        public event RequestGodlikePowers OnRequestGodlikePowers;
        public event GodKickUser OnGodKickUser;
        public event ObjectDuplicate OnObjectDuplicate;
        public event GrabObject OnGrabObject;
        public event DeGrabObject OnDeGrabObject;
        public event MoveObject OnGrabUpdate;
        public event ViewerEffectEventHandler OnViewerEffect;

        public event FetchInventory OnAgentDataUpdateRequest;
        public event TeleportLocationRequest OnSetStartLocationRequest;

        public event UpdateShape OnUpdatePrimShape;
        public event ObjectExtraParams OnUpdateExtraParams;
        public event RequestObjectPropertiesFamily OnRequestObjectPropertiesFamily;
        public event ObjectSelect OnObjectSelect;
        public event GenericCall7 OnObjectDescription;
        public event GenericCall7 OnObjectName;
        public event GenericCall7 OnObjectClickAction;
        public event GenericCall7 OnObjectMaterial;
        public event UpdatePrimFlags OnUpdatePrimFlags;
        public event UpdatePrimTexture OnUpdatePrimTexture;
        public event UpdateVectorWithUpdate OnUpdatePrimGroupPosition;
        public event UpdateVectorWithUpdate OnUpdatePrimSinglePosition;
        public event UpdatePrimRotation OnUpdatePrimGroupRotation;
        public event UpdatePrimSingleRotation OnUpdatePrimSingleRotation;
        public event UpdatePrimGroupRotation OnUpdatePrimGroupMouseRotation;
        public event UpdateVector OnUpdatePrimScale;
        public event UpdateVector OnUpdatePrimGroupScale;
        public event StatusChange OnChildAgentStatus;
        public event GenericCall2 OnStopMovement;

        public event CreateNewInventoryItem OnCreateNewInventoryItem;
        public event CreateInventoryFolder OnCreateNewInventoryFolder;
        public event UpdateInventoryFolder OnUpdateInventoryFolder;
        public event MoveInventoryFolder OnMoveInventoryFolder;
        public event RemoveInventoryFolder OnRemoveInventoryFolder;
        public event RemoveInventoryItem OnRemoveInventoryItem;
        public event FetchInventoryDescendents OnFetchInventoryDescendents;
        public event PurgeInventoryDescendents OnPurgeInventoryDescendents;
        public event FetchInventory OnFetchInventory;
        public event RequestTaskInventory OnRequestTaskInventory;
        public event UpdateInventoryItem OnUpdateInventoryItem;
        public event CopyInventoryItem OnCopyInventoryItem;
        public event MoveInventoryItem OnMoveInventoryItem;
        public event UDPAssetUploadRequest OnAssetUploadRequest;
        public event RequestTerrain OnRequestTerrain;
        public event RequestTerrain OnUploadTerrain;
        public event XferReceive OnXferReceive;
        public event RequestXfer OnRequestXfer;
        public event ConfirmXfer OnConfirmXfer;
        public event AbortXfer OnAbortXfer;
        public event RezScript OnRezScript;
        public event UpdateTaskInventory OnUpdateTaskInventory;
        public event MoveTaskInventory OnMoveTaskItem;
        public event RemoveTaskInventory OnRemoveTaskItem;
        public event GenericMessage OnGenericMessage;
        public event UUIDNameRequest OnNameFromUUIDRequest;
        public event UUIDNameRequest OnUUIDGroupNameRequest;

        public event ParcelPropertiesRequest OnParcelPropertiesRequest;
        public event ParcelDivideRequest OnParcelDivideRequest;
        public event ParcelJoinRequest OnParcelJoinRequest;
        public event ParcelPropertiesUpdateRequest OnParcelPropertiesUpdateRequest;
        public event ParcelAbandonRequest OnParcelAbandonRequest;
        public event ParcelGodForceOwner OnParcelGodForceOwner;
        public event ParcelReclaim OnParcelReclaim;
        public event ParcelReturnObjectsRequest OnParcelReturnObjectsRequest;
        public event ParcelReturnObjectsRequest OnParcelDisableObjectsRequest;
        public event ParcelAccessListRequest OnParcelAccessListRequest;
        public event ParcelAccessListUpdateRequest OnParcelAccessListUpdateRequest;
        public event ParcelSelectObjects OnParcelSelectObjects;
        public event ParcelObjectOwnerRequest OnParcelObjectOwnerRequest;
        public event ObjectDeselect OnObjectDeselect;
        public event RegionInfoRequest OnRegionInfoRequest;
        public event EstateCovenantRequest OnEstateCovenantRequest;
        public event EstateChangeInfo OnEstateChangeInfo;

        public event ObjectDuplicateOnRay OnObjectDuplicateOnRay;

        public event FriendActionDelegate OnApproveFriendRequest;
        public event FriendActionDelegate OnDenyFriendRequest;
        public event FriendshipTermination OnTerminateFriendship;

        public event EconomyDataRequest OnEconomyDataRequest;
        public event MoneyBalanceRequest OnMoneyBalanceRequest;
        public event UpdateAvatarProperties OnUpdateAvatarProperties;

        public event ObjectIncludeInSearch OnObjectIncludeInSearch;
        public event UUIDNameRequest OnTeleportHomeRequest;

        public event ScriptAnswer OnScriptAnswer;
        public event RequestPayPrice OnRequestPayPrice;
        public event ObjectSaleInfo OnObjectSaleInfo;
        public event ObjectBuy OnObjectBuy;
        public event BuyObjectInventory OnBuyObjectInventory;
        public event AgentSit OnUndo;

        public event ForceReleaseControls OnForceReleaseControls;

        public event GodLandStatRequest OnLandStatRequest;
        public event RequestObjectPropertiesFamily OnObjectGroupRequest;
        public event AgentCachedTextureRequest OnAgentCachedTextureRequest;

        public event DetailedEstateDataRequest OnDetailedEstateDataRequest;
        public event SetEstateFlagsRequest OnSetEstateFlagsRequest;
        public event SetEstateTerrainBaseTexture OnSetEstateTerrainBaseTexture;
        public event SetEstateTerrainDetailTexture OnSetEstateTerrainDetailTexture;
        public event SetEstateTerrainTextureHeights OnSetEstateTerrainTextureHeights;
        public event CommitEstateTerrainTextureRequest OnCommitEstateTerrainTextureRequest;
        public event SetRegionTerrainSettings OnSetRegionTerrainSettings;
        public event EstateRestartSimRequest OnEstateRestartSimRequest;
        public event EstateChangeCovenantRequest OnEstateChangeCovenantRequest;
        public event UpdateEstateAccessDeltaRequest OnUpdateEstateAccessDeltaRequest;
        public event SimulatorBlueBoxMessageRequest OnSimulatorBlueBoxMessageRequest;
        public event EstateBlueBoxMessageRequest OnEstateBlueBoxMessageRequest;
        public event EstateDebugRegionRequest OnEstateDebugRegionRequest;
        public event EstateTeleportOneUserHomeRequest OnEstateTeleportOneUserHomeRequest;
        public event EstateTeleportAllUsersHomeRequest OnEstateTeleportAllUsersHomeRequest;
        public event ScriptReset OnScriptReset;
        public event GetScriptRunning OnGetScriptRunning;
        public event SetScriptRunning OnSetScriptRunning;
        public event UpdateVector OnAutoPilotGo;

        public event RegionHandleRequest OnRegionHandleRequest;
        public event ParcelInfoRequest OnParcelInfoRequest;

        public event ActivateGesture OnActivateGesture;
        public event DeactivateGesture OnDeactivateGesture;
        public event ObjectOwner OnObjectOwner;

        public event DirPlacesQuery OnDirPlacesQuery;
        public event DirFindQuery OnDirFindQuery;
        public event DirLandQuery OnDirLandQuery;
        public event DirPopularQuery OnDirPopularQuery;
        public event DirClassifiedQuery OnDirClassifiedQuery;
        public event EventInfoRequest OnEventInfoRequest;
        public event ParcelSetOtherCleanTime OnParcelSetOtherCleanTime;

        public event MapItemRequest OnMapItemRequest;

        public event OfferCallingCard OnOfferCallingCard;
        public event AcceptCallingCard OnAcceptCallingCard;
        public event DeclineCallingCard OnDeclineCallingCard;
        public event SoundTrigger OnSoundTrigger;

        public event StartLure OnStartLure;
        public event TeleportLureRequest OnTeleportLureRequest;
        public event NetworkStats OnNetworkStatsUpdate;

        public event ClassifiedInfoRequest OnClassifiedInfoRequest;
        public event ClassifiedInfoUpdate OnClassifiedInfoUpdate;
        public event ClassifiedDelete OnClassifiedDelete;
        public event ClassifiedDelete OnClassifiedGodDelete;

        public event EventNotificationAddRequest OnEventNotificationAddRequest;
        public event EventNotificationRemoveRequest OnEventNotificationRemoveRequest;
        public event EventGodDelete OnEventGodDelete;

        public event ParcelDwellRequest OnParcelDwellRequest;
        public event UserInfoRequest OnUserInfoRequest;
        public event UpdateUserInfo OnUpdateUserInfo;

        public event RetrieveInstantMessages OnRetrieveInstantMessages;
        public event SpinStart OnSpinStart;
        public event SpinStop OnSpinStop;
        public event SpinObject OnSpinUpdate;
        public event ParcelDeedToGroup OnParcelDeedToGroup;

        public event AvatarNotesUpdate OnAvatarNotesUpdate;
        public event MuteListRequest OnMuteListRequest;
        public event PickDelete OnPickDelete;
        public event PickGodDelete OnPickGodDelete;
        public event PickInfoUpdate OnPickInfoUpdate;

        public event PlacesQuery OnPlacesQuery;

        public event UpdatePrimSingleRotationPosition OnUpdatePrimSingleRotationPosition;

        public event ObjectRequest OnObjectRequest;

        public event AvatarInterestUpdate OnAvatarInterestUpdate;
        public event GrantUserFriendRights OnGrantUserRights;

        public event LinkInventoryItem OnLinkInventoryItem;

        public event AgentSit OnRedo;

        public event LandUndo OnLandUndo;

        public event FindAgentUpdate OnFindAgent;

        public event TrackAgentUpdate OnTrackAgent;

        public event NewUserReport OnUserReport;

        public event SaveStateHandler OnSaveState;

        public event GroupAccountSummaryRequest OnGroupAccountSummaryRequest;

        public event GroupAccountDetailsRequest OnGroupAccountDetailsRequest;

        public event GroupAccountTransactionsRequest OnGroupAccountTransactionsRequest;

        public event FreezeUserUpdate OnParcelFreezeUser;

        public event EjectUserUpdate OnParcelEjectUser;

        public event ParcelBuyPass OnParcelBuyPass;

        public event ParcelGodMark OnParcelGodMark;

        public event GroupActiveProposalsRequest OnGroupActiveProposalsRequest;

        public event GroupVoteHistoryRequest OnGroupVoteHistoryRequest;

        public event SimWideDeletesDelegate OnSimWideDeletes;

        public event GroupProposalBallotRequest OnGroupProposalBallotRequest;

        public event SendPostcard OnSendPostcard;

        public event MuteListEntryUpdate OnUpdateMuteListEntry;

        public event MuteListEntryRemove OnRemoveMuteListEntry;

        public event GodlikeMessage OnGodlikeMessage;

        public event GodUpdateRegionInfoUpdate OnGodUpdateRegionInfoUpdate;

        public event ChangeInventoryItemFlags OnChangeInventoryItemFlags;

        public event TeleportCancel OnTeleportCancel;

        public event GodlikeMessage OnEstateTelehubRequest;

        public event ViewerStartAuction OnViewerStartAuction;

#pragma warning restore 67

        public void QueueDelayedUpdate(PriorityQueueItem<EntityUpdate, double> it)
        {
        }

        public void SendRegionHandshake(RegionInfo regionInfo, RegionHandshakeArgs args)
        {
            if (OnRegionHandShakeReply != null)
            {
                OnRegionHandShakeReply(this);
            }
        }

        #endregion

        #region IClientAPI Members

        public UUID SessionId { get; set; }

        public UUID SecureSessionId
        {
            get { return UUID.Zero; }
        }

        public UUID ActiveGroupId
        {
            get { return UUID.Zero; }
        }

        public string ActiveGroupName
        {
            get { return ""; }
        }

        public ulong ActiveGroupPowers
        {
            get { return 0; }
        }

        public IPAddress EndPoint
        {
            get { return null; }
        }

        public int NextAnimationSequenceNumber
        {
            get { return 0; }
        }

        public bool IsActive
        {
            get { return true; }
            set { }
        }

        public bool IsLoggingOut
        {
            get { return false; }
            set { }
        }

        public bool SendLogoutPacketWhenClosing
        {
            set { }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return new IPEndPoint(IPAddress.Loopback, (ushort)m_circuitCode); }
        }

        public void SetDebugPacketLevel(int newDebug)
        {
        }

        public void ProcessInPacket(Packet NewPack)
        {
        }

        public void Stop()
        {
            Close(true);
        }

        public void Close(bool p)
        {
            //raiseevent on the packet server to Shutdown the circuit
            if (OnLogout != null)
                OnLogout(this);
            if(OnConnectionClosed != null)
                OnConnectionClosed(this);
        }

        public void ForceSendOnAgentUpdate(IClientAPI client, AgentUpdateArgs args)
        {
            OnAgentUpdate(client, args);
        }

        public void OnForceChatFromViewer(IClientAPI sender, OSChatMessage e)
        {
            OnChatFromClient(sender, e);
        }

        public void SendInstantMessage(GridInstantMessage im)
        {
            m_controller.SendInstantMessage(im);
        }

        public void Kick(string message)
        {
            Close(true);
        }

        public void SendWearables(AvatarWearable[] wearables, int serial)
        {
        }

        public void SendAgentCachedTexture(List<CachedAgentArgs> args)
        {
        }

        public void SendAppearance(UUID agentID, byte[] visualParams, byte[] textureEntry)
        {
        }

        public void SendStartPingCheck(byte seq)
        {
        }

        public void SendKillObject(ulong regionHandle, IEntity[] entities)
        {
        }

        public void SendKillObject(ulong regionHandle, uint[] entities)
        {
        }

        public void SendAnimations(AnimationGroup animations)
        {
        }

        public void SendChatMessage(string message, byte type, Vector3 fromPos, string fromName, UUID fromAgentID,
                                    byte source, byte audible)
        {
        }

        public void SendGenericMessage(string method, List<string> message)
        {
        }

        public void SendGenericMessage(string method, List<byte[]> message)
        {
        }

        public void SendLayerData(short[] map)
        {
        }

        public void SendLayerData(int px, int py, short[] map)
        {
        }

        public void SendLayerData(int[] x, int[] y, short[] map, TerrainPatch.LayerType type)
        {
        }

        public void SendWindData(Vector2[] windSpeeds)
        {
        }

        public void SendCloudData(float[] cloudCover)
        {
        }

        public void MoveAgentIntoRegion(RegionInfo regInfo, Vector3 pos, Vector3 look)
        {
        }

        public AgentCircuitData RequestClientInfo()
        {
            return m_circuitData;
        }

        public void SendMapBlock(List<MapBlockData> mapBlocks, uint flag)
        {
        }

        public void SendLocalTeleport(Vector3 position, Vector3 lookAt, uint flags)
        {
        }

        public void SendRegionTeleport(ulong regionHandle, byte simAccess, IPEndPoint regionExternalEndPoint,
                                       uint locationID, uint flags, string capsURL)
        {
        }

        public void SendTeleportFailed(string reason)
        {
        }

        public void SendTeleportStart(uint flags)
        {
        }

        public void SendTeleportProgress(uint flags, string message)
        {
        }

        public void SendMoneyBalance(UUID transaction, bool success, byte[] description, int balance)
        {
        }

        public void SendPayPrice(UUID objectID, int[] payPrice)
        {
        }

        public void SendCoarseLocationUpdate(List<UUID> users, List<Vector3> CoarseLocations)
        {
        }

        public void SetChildAgentThrottle(byte[] throttle)
        {
        }

        public void SendAvatarDataImmediate(IEntity avatar)
        {
        }

        public void SendAvatarUpdate(IEnumerable<EntityUpdate> updates)
        {
        }

        public void SendPrimUpdate(IEnumerable<EntityUpdate> updates)
        {
        }

        public void SendInventoryFolderDetails(UUID ownerID, UUID folderID, List<InventoryItemBase> items,
                                               List<InventoryFolderBase> folders, int version, bool fetchFolders,
                                               bool fetchItems)
        {
        }

        public void SendInventoryItemDetails(UUID ownerID, InventoryItemBase item)
        {
        }

        public void SendInventoryItemCreateUpdate(InventoryItemBase Item, uint callbackId)
        {
        }

        public void SendRemoveInventoryItem(UUID itemID)
        {
        }

        public void SendTakeControls(int controls, bool passToAgent, bool TakeControls)
        {
        }

        public void SendTaskInventory(UUID taskID, short serial, byte[] fileName)
        {
        }

        public void SendBulkUpdateInventory(InventoryItemBase node)
        {
        }

        public void SendBulkUpdateInventory(InventoryFolderBase node)
        {
        }

        public void SendXferPacket(ulong xferID, uint packet, byte[] data)
        {
        }

        public void SendAbortXferPacket(ulong xferID)
        {
        }

        public void SendEconomyData(float EnergyEfficiency, int ObjectCapacity, int ObjectCount, int PriceEnergyUnit,
                                    int PriceGroupCreate, int PriceObjectClaim, float PriceObjectRent,
                                    float PriceObjectScaleFactor, int PriceParcelClaim, float PriceParcelClaimFactor,
                                    int PriceParcelRent, int PricePublicObjectDecay, int PricePublicObjectDelete,
                                    int PriceRentLight, int PriceUpload, int TeleportMinPrice,
                                    float TeleportPriceExponent)
        {
        }

        public void SendAvatarPickerReply(AvatarPickerReplyAgentDataArgs AgentData, List<AvatarPickerReplyDataArgs> Data)
        {
        }

        public void SendAgentDataUpdate(UUID agentid, UUID activegroupid, string firstname, string lastname,
                                        ulong grouppowers, string groupname, string grouptitle)
        {
        }

        public void SendPreLoadSound(UUID objectID, UUID ownerID, UUID soundID)
        {
        }

        public void SendPlayAttachedSound(UUID soundID, UUID objectID, UUID ownerID, float gain, byte flags)
        {
        }

        public void SendTriggeredSound(UUID soundID, UUID ownerID, UUID objectID, UUID parentID, ulong handle,
                                       Vector3 position, float gain)
        {
        }

        public void SendAttachedSoundGainChange(UUID objectID, float gain)
        {
        }

        public void SendNameReply(UUID profileId, string firstname, string lastname)
        {
        }

        public void SendAlertMessage(string message)
        {
        }

        public void SendAgentAlertMessage(string message, bool modal)
        {
        }

        public void SendLoadURL(string objectname, UUID objectID, UUID ownerID, bool groupOwned, string message,
                                string url)
        {
        }

        public void SendDialog(string objectname, UUID objectID, UUID ownerID, string ownerFirstName,
                               string ownerLastName, string msg, UUID textureID, int ch, string[] buttonlabels)
        {
        }

        public void SendSunPos(Vector3 sunPos, Vector3 sunVel, ulong CurrentTime, uint SecondsPerSunCycle,
                               uint SecondsPerYear, float OrbitalPosition)
        {
        }

        public void SendViewerEffect(ViewerEffectPacket.EffectBlock[] effectBlocks)
        {
        }

        public UUID GetDefaultAnimation(string name)
        {
            return UUID.Zero;
        }

        public void SendAvatarProperties(UUID avatarID, string aboutText, string bornOn, byte[] charterMember,
                                         string flAbout, uint flags, UUID flImageID, UUID imageID, string profileURL,
                                         UUID partnerID)
        {
        }

        public void SendScriptQuestion(UUID taskID, string taskName, string ownerName, UUID itemID, int question)
        {
        }

        public void SendHealth(float health)
        {
        }

        public void SendEstateList(UUID invoice, int code, UUID[] Data, uint estateID)
        {
        }

        public void SendBannedUserList(UUID invoice, EstateBan[] banlist, uint estateID)
        {
        }

        public void SendRegionInfoToEstateMenu(RegionInfoForEstateMenuArgs args)
        {
        }

        public void SendEstateCovenantInformation(UUID covenant, int covenantLastUpdated)
        {
        }

        public void SendDetailedEstateData(UUID invoice, string estateName, uint estateID, uint parentEstate,
                                           uint estateFlags, uint sunPosition, UUID covenant, int covenantLastUpdated,
                                           string abuseEmail, UUID estateOwner)
        {
        }

        public void SendLandProperties(int sequence_id, bool snap_selection, int request_result, LandData landData,
                                       float simObjectBonusFactor, int parcelObjectCapacity, int simObjectCapacity,
                                       uint regionFlags)
        {
        }

        public void SendLandAccessListData(List<UUID> avatars, uint accessFlag, int localLandID)
        {
        }

        public void SendForceClientSelectObjects(List<uint> objectIDs)
        {
        }

        public void SendCameraConstraint(Vector4 ConstraintPlane)
        {
        }

        public void SendLandObjectOwners(List<LandObjectOwners> objOwners)
        {
        }

        public void SendLandParcelOverlay(byte[] data, int sequence_id)
        {
        }

        public void SendParcelMediaCommand(uint flags, ParcelMediaCommandEnum command, float time)
        {
        }

        public void SendParcelMediaUpdate(string mediaUrl, UUID mediaTextureID, byte autoScale, string mediaType,
                                          string mediaDesc, int mediaWidth, int mediaHeight, byte mediaLoop)
        {
        }

        public void SendAssetUploadCompleteMessage(sbyte AssetType, bool Success, UUID AssetFullID)
        {
        }

        public void SendConfirmXfer(ulong xferID, uint PacketID)
        {
        }

        public void SendXferRequest(ulong XferID, short AssetType, UUID vFileID, byte FilePath, byte[] FileName)
        {
        }

        public void SendInitiateDownload(string simFileName, string clientFileName)
        {
        }

        public void SendImageFirstPart(ushort numParts, UUID ImageUUID, uint ImageSize, byte[] ImageData,
                                       byte imageCodec)
        {
        }

        public void SendImageNextPart(ushort partNumber, UUID imageUuid, byte[] imageData)
        {
        }

        public void SendImageNotFound(UUID imageid)
        {
        }

        public void SendSimStats(SimStats stats)
        {
        }

        public void SendObjectPropertiesFamilyData(uint RequestFlags, UUID ObjectUUID, UUID OwnerID, UUID GroupID,
                                                   uint BaseMask, uint OwnerMask, uint GroupMask, uint EveryoneMask,
                                                   uint NextOwnerMask, int OwnershipCost, byte SaleType, int SalePrice,
                                                   uint Category, UUID LastOwnerID, string ObjectName,
                                                   string Description)
        {
        }

        public void SendObjectPropertiesReply(List<IEntity> part)
        {
        }

        public void SendAgentOffline(UUID[] agentIDs)
        {
        }

        public void SendAgentOnline(UUID[] agentIDs)
        {
        }

        public void SendSitResponse(UUID TargetID, Vector3 OffsetPos, Quaternion SitOrientation, bool autopilot,
                                    Vector3 CameraAtOffset, Vector3 CameraEyeOffset, bool ForceMouseLook)
        {
        }

        public void SendAdminResponse(UUID Token, uint AdminLevel)
        {
        }

        public void SendGroupMembership(GroupMembershipData[] GroupMembership)
        {
        }

        public void SendGroupNameReply(UUID groupLLUID, string GroupName)
        {
        }

        public void SendJoinGroupReply(UUID groupID, bool success)
        {
        }

        public void SendEjectGroupMemberReply(UUID agentID, UUID groupID, bool success)
        {
        }

        public void SendLeaveGroupReply(UUID groupID, bool success)
        {
        }

        public void SendCreateGroupReply(UUID groupID, bool success, string message)
        {
        }

        public void SendLandStatReply(uint reportType, uint requestFlags, uint resultCount, LandStatReportItem[] lsrpia)
        {
        }

        public void SendScriptRunningReply(UUID objectID, UUID itemID, bool running)
        {
        }

        public void SendAsset(AssetRequestToClient req)
        {
        }

        public byte[] GetThrottlesPacked(float multiplier)
        {
            return new byte[0];
        }

        public void SendBlueBoxMessage(UUID FromAvatarID, string FromAvatarName, string Message)
        {
        }

        public void SendLogoutPacket()
        {
        }

        public EndPoint GetClientEP()
        {
            return null;
        }

        public void SendSetFollowCamProperties(UUID objectID, SortedDictionary<int, float> parameters)
        {
        }

        public void SendClearFollowCamProperties(UUID objectID)
        {
        }

        public void SendRegionHandle(UUID regoinID, ulong handle)
        {
        }

        public void SendParcelInfo(LandData land, UUID parcelID, uint x, uint y, string SimName)
        {
        }

        public void SendScriptTeleportRequest(string objName, string simName, Vector3 pos, Vector3 lookAt)
        {
        }

        public void SendDirPlacesReply(UUID queryID, DirPlacesReplyData[] data)
        {
        }

        public void SendDirPeopleReply(UUID queryID, DirPeopleReplyData[] data)
        {
        }

        public void SendDirEventsReply(UUID queryID, DirEventsReplyData[] data)
        {
        }

        public void SendDirGroupsReply(UUID queryID, DirGroupsReplyData[] data)
        {
        }

        public void SendDirClassifiedReply(UUID queryID, DirClassifiedReplyData[] data)
        {
        }

        public void SendDirLandReply(UUID queryID, DirLandReplyData[] data)
        {
        }

        public void SendDirPopularReply(UUID queryID, DirPopularReplyData[] data)
        {
        }

        public void SendEventInfoReply(EventData info)
        {
        }

        public void SendMapItemReply(mapItemReply[] replies, uint mapitemtype, uint flags)
        {
        }

        public void SendAvatarGroupsReply(UUID avatarID, GroupMembershipData[] data)
        {
        }

        public void SendOfferCallingCard(UUID srcID, UUID transactionID)
        {
        }

        public void SendAcceptCallingCard(UUID transactionID)
        {
        }

        public void SendDeclineCallingCard(UUID transactionID)
        {
        }

        public void SendTerminateFriend(UUID exFriendID)
        {
        }

        public void SendAvatarClassifiedReply(UUID targetID, UUID[] classifiedID, string[] name)
        {
        }

        public void SendClassifiedInfoReply(UUID classifiedID, UUID creatorID, uint creationDate, uint expirationDate,
                                            uint category, string name, string description, UUID parcelID,
                                            uint parentEstate, UUID snapshotID, string simName, Vector3 globalPos,
                                            string parcelName, byte classifiedFlags, int price)
        {
        }

        public void SendAgentDropGroup(UUID groupID)
        {
        }

        public void SendAvatarNotesReply(UUID targetID, string text)
        {
        }

        public void SendAvatarPicksReply(UUID targetID, Dictionary<UUID, string> picks)
        {
        }

        public void SendPickInfoReply(UUID pickID, UUID creatorID, bool topPick, UUID parcelID, string name, string desc,
                                      UUID snapshotID, string user, string originalName, string simName,
                                      Vector3 posGlobal, int sortOrder, bool enabled)
        {
        }

        public void SendAvatarClassifiedReply(UUID targetID, Dictionary<UUID, string> classifieds)
        {
        }

        public void SendParcelDwellReply(int localID, UUID parcelID, float dwell)
        {
        }

        public void SendUserInfoReply(bool imViaEmail, bool visible, string email)
        {
        }

        public void SendUseCachedMuteList()
        {
        }

        public void SendMuteListUpdate(string filename)
        {
        }

        public void SendGroupActiveProposals(UUID groupID, UUID transactionID, GroupActiveProposals[] Proposals)
        {
        }

        public void SendGroupVoteHistory(UUID groupID, UUID transactionID, GroupVoteHistory Vote,
                                         GroupVoteHistoryItem[] Items)
        {
        }

        public bool AddGenericPacketHandler(string MethodName, GenericMessage handler)
        {
            return true;
        }

        public bool RemoveGenericPacketHandler(string MethodName)
        {
            return true;
        }

        public void SendRebakeAvatarTextures(UUID textureID)
        {
        }

        public void SendAvatarInterestsReply(UUID avatarID, uint wantMask, string wantText, uint skillsMask,
                                             string skillsText, string languages)
        {
        }

        public void SendGroupAccountingDetails(IClientAPI sender, UUID groupID, UUID transactionID, UUID sessionID,
                                               int amt, int currentInterval, int interval, string startDate, GroupAccountHistory[] history)
        {
        }

        public void SendGroupAccountingSummary(IClientAPI sender, UUID groupID, UUID requestID, int moneyAmt, int totalTier,
                                               int usedTier, string startDate, int currentInterval, int intervalLength,
                                               string taxDate, string lastTaxDate, int parcelDirectoryFee, int landTaxFee, int groupTaxFee, int objectTaxFee)
        {
        }

        public void SendGroupTransactionsSummaryDetails(IClientAPI sender, UUID groupID, UUID transactionID,
                                                        UUID sessionID, int currentInterval, int intervalDays, string startingDate, GroupAccountHistory[] history)
        {
        }

        public void SendChangeUserRights(UUID agentID, UUID friendID, int rights)
        {
        }

        public void SendTextBoxRequest(string message, int chatChannel, string objectname, string ownerFirstName,
                                       string ownerLastName, UUID objectId)
        {
        }

        public void SendPlacesQuery(ExtendedLandData[] LandData, UUID queryID, UUID transactionID)
        {
        }

        public void FireUpdateParcel(LandUpdateArgs args, int LocalID)
        {
        }

        public void SendTelehubInfo(Vector3 TelehubPos, Quaternion TelehubRot, List<Vector3> SpawnPoint, UUID ObjectID,
                                    string nameT)
        {
        }

        public void StopFlying(IEntity presence)
        {
        }

        public void Reset()
        {
        }

        public void HandleChatFromClient(OSChatMessage args)
        {
        }

        #endregion
    }
}