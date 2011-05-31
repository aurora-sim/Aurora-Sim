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
using System.Net;
using System.Reflection;
using Aurora.Framework;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;

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

    public enum TravelMode
    {
        Walk,
        Fly,
        Teleport,
        None
    };

    #endregion

    public class Bot : IClientAPI
    {
        #region Declares

        private bool m_allowJump = true;
        private bool m_UseJumpDecisionTree = true;

        private bool m_paused = false;

        private static readonly ILog m_log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Vector3 DEFAULT_START_POSITION = new Vector3(128, 128, 128);

        private uint m_movementFlag = 0;
        private Quaternion m_bodyDirection = Quaternion.Identity;

        private UUID m_myID = UUID.Random();
        private Scene m_scene;
        private IScenePresence m_scenePresence;
        private AgentCircuitData m_circuitData;
        /// <summary>
        /// There are several events added so far,
        /// Update - called every 0.1s, allows for updating of the position of where the avatar is supposed to be goign
        /// Move - called every 10ms, allows for subtle changes and fast callbacks before the avatar moves toward its next location
        /// ToAvatar - a following event, called when the bot is within range of the avatar (range = m_followCloseToPoint)
        /// LostAvatar - a following event, called when the bot is out of the maximum range to look for its avatar (range = m_followLoseAvatarDistance)
        /// </summary>
        public AuroraEventManager EventManager = new AuroraEventManager ();

        private BotState m_currentState = BotState.Idle;
        public BotState State
        {
            get { return m_currentState; }
            set 
            { 
                m_previousState = m_currentState;  
                m_currentState = value;
            }
        }

        #region Jump Settings

        public bool AllowJump
        {
            get
            {
                return m_allowJump;
            }
            set
            {
                m_allowJump = value;
            }
        }

        public bool UseJumpDecisionTree
        {
            get
            {
                return m_UseJumpDecisionTree;
            }
            set
            {
                m_UseJumpDecisionTree = value;
            }
        }

        #endregion

        private BotState m_previousState = BotState.Idle;
        
        private Vector3 m_destination;

        private System.Timers.Timer m_frames;
        private System.Timers.Timer m_startTime;

        #region IClientAPI properties

        private UUID m_avatarCreatorID = UUID.Zero;
        private static UInt32 UniqueId = 1;

        private string m_firstName = "";
        private string m_lastName = "";

        private NodeGraph m_nodeGraph = new NodeGraph ();

        private float m_RexCharacterSpeedMod = 1.0f;

        public float RexCharacterSpeedMod
        {
            get { return m_RexCharacterSpeedMod; }
            set { m_RexCharacterSpeedMod = value; }
        }

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

        #endregion

        #endregion

        #region Constructor

        // creates new bot on the default location
        public Bot(Scene scene, AgentCircuitData data, UUID creatorID)
        {
            RegisterInterfaces();

            m_avatarCreatorID = creatorID;
            m_circuitData = data;
            m_scene = scene;
            
            m_circuitCode = UniqueId;
            m_frames = new System.Timers.Timer(100);
            m_frames.Elapsed += (frames_Elapsed);
            m_startTime = new System.Timers.Timer(10);
            m_startTime.Elapsed += (startTime_Elapsed);

            UniqueId++;
        }

        #endregion

        #region Initialize/Close

        public void Initialize()
        {
            List<IScenePresence> avatars = m_scene.GetScenePresences ();
            foreach (IScenePresence avatar in avatars)
            {
                if (avatar.ControllingClient == this)
                {
                    m_scenePresence = avatar;
                    break;
                }
            }
            m_startTime.Start ();
            m_frames.Start ();
        }

        public void Close(bool forceKill)
        {
            // Pull Client out of Region
            m_log.Info("[RexBot]: Removing bot " + Name);

            OnBotLogout();

            //raiseevent on the packet server to Shutdown the circuit
            OnBotConnectionClosed();

            m_frames.Stop();
            m_startTime.Stop ();
        }

        #endregion

        #region SetPath

        public void SetPath (List<Vector3> Positions, List<TravelMode> modes)
        {
            m_nodeGraph.Clear ();
            m_nodeGraph.AddRange (Positions, modes);
            GetNextDestination();
        }

        #endregion

        #region Set Av Speed

        public void SetMovementSpeedMod (float speed)
        {
            m_scenePresence.SpeedModifier = speed;
        }

        #endregion

        #region Move/Rotate the bot

        // Makes the bot walk to the specified destination
        private void WalkTo(Vector3 destination)
        {
            if (!Util.IsZeroVector(destination - m_scenePresence.AbsolutePosition))
            {
                walkTo(destination);
                State = BotState.Walking;

                m_destination = destination;
            }
        }

        // Makes the bot fly to the specified destination
        private void FlyTo(Vector3 destination)
        {
            if (Util.IsZeroVector(destination - m_scenePresence.AbsolutePosition) == false)
            {
                flyTo(destination);
                m_destination = destination;
                State = BotState.Flying;
            }
            else
            {
                m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_AT_POS;

                OnBotAgentUpdate(m_movementFlag, m_bodyDirection);
                m_movementFlag = (uint)AgentManager.ControlFlags.NONE;
            }
        }

        private void RotateTo(Vector3 destination)
        {
            Vector3 bot_forward = new Vector3(1, 0, 0);
            if (destination - m_scenePresence.AbsolutePosition != Vector3.Zero)
            {
                Vector3 bot_toward = Util.GetNormalizedVector (destination - m_scenePresence.AbsolutePosition);
                Quaternion rot_result = llRotBetween (bot_forward, bot_toward);
                m_bodyDirection = rot_result;
            }
            m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_AT_POS;

            OnBotAgentUpdate(m_movementFlag, m_bodyDirection);
            m_movementFlag = (uint)AgentManager.ControlFlags.NONE;
        }

        #region rotation helper functions

        private Vector3 llRot2Fwd (Quaternion r)
        {
            return (new Vector3 (1, 0, 0) * r);
        }

        private Quaternion llRotBetween (Vector3 a, Vector3 b)
        {
            //A and B should both be normalized
            double dotProduct = Vector3.Dot (a, b);
            Vector3 crossProduct = Vector3.Cross (a, b);
            double magProduct = Vector3.Distance (Vector3.Zero, a) * Vector3.Distance (Vector3.Zero, b);
            double angle = Math.Acos (dotProduct / magProduct);
            Vector3 axis = Vector3.Normalize (crossProduct);
            float s = (float)Math.Sin (angle / 2);

            return new Quaternion (axis.X * s, axis.Y * s, axis.Z * s, (float)Math.Cos (angle / 2));
        }

        #endregion

        #region Move / fly bot

        /// <summary>
        /// Does the actual movement of the bot
        /// </summary>
        /// <param name="pos"></param>
        private void walkTo (Vector3 pos)
        {
            Vector3 bot_forward = new Vector3 (2, 0, 0);
            Vector3 bot_toward;
            if (pos - m_scenePresence.AbsolutePosition != Vector3.Zero)
            {
                try
                {
                    bot_toward = Util.GetNormalizedVector (pos - m_scenePresence.AbsolutePosition);
                    Quaternion rot_result = llRotBetween (bot_forward, bot_toward);
                    m_bodyDirection = rot_result;
                }
                catch (System.ArgumentException)
                {
                }
            }
            m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_AT_POS;


            OnBotAgentUpdate (m_movementFlag, m_bodyDirection);
            m_movementFlag = (uint)AgentManager.ControlFlags.NONE;
        }

        /// <summary>
        /// Does the actual movement of the bot
        /// </summary>
        /// <param name="pos"></param>
        private void flyTo (Vector3 pos)
        {
            Vector3 bot_forward = new Vector3 (1, 0, 0);
            if (pos - m_scenePresence.AbsolutePosition != Vector3.Zero)
            {
                try
                {
                    Vector3 bot_toward = Util.GetNormalizedVector (pos - m_scenePresence.AbsolutePosition);
                    Quaternion rot_result = llRotBetween (bot_forward, bot_toward);
                    m_bodyDirection = rot_result;
                }
                catch (System.ArgumentException)
                {
                }
            }

            m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_FLY;

            Vector3 diffPos = m_destination - m_scenePresence.AbsolutePosition;
            if (Math.Abs (diffPos.X) > 1.5 || Math.Abs (diffPos.Y) > 1.5)
            {
                m_movementFlag |= (uint)AgentManager.ControlFlags.AGENT_CONTROL_AT_POS;
            }

            if (m_scenePresence.AbsolutePosition.Z < pos.Z - 1)
            {
                m_movementFlag |= (uint)AgentManager.ControlFlags.AGENT_CONTROL_UP_POS;
            }
            else if (m_scenePresence.AbsolutePosition.Z > pos.Z + 1)
            {
                m_movementFlag |= (uint)AgentManager.ControlFlags.AGENT_CONTROL_UP_NEG;
            }

            OnBotAgentUpdate (m_movementFlag, m_bodyDirection);
            m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_FLY;
        }

        #region Jump Decision Tree

        /// <summary>
        /// See whether we should jump based on the start and end positions given
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private bool JumpDecisionTree (Vector3 start, Vector3 end)
        {
            //Cast a ray in the direction that we are going
            List<ISceneChildEntity> entities = llCastRay (start, end);

            foreach (ISceneChildEntity entity in entities)
            {
                if (entity.IsAttachment)
                    continue; //No attachments

                //If the size is huge, we jump
                if (entity.Scale.Z > m_scenePresence.PhysicsActor.Size.Z)
                    return true;
            }
            return false;
        }

        public List<ISceneChildEntity> llCastRay (Vector3 start, Vector3 end)
        {
            Vector3 dir = new Vector3 ((float)(end - start).X, (float)(end - start).Y, (float)(end - start).Z);
            Vector3 startvector = new Vector3 ((float)start.X, (float)start.Y, (float)start.Z);
            Vector3 endvector = new Vector3 ((float)end.X, (float)end.Y, (float)end.Z);

            List<ISceneChildEntity> entities = new List<ISceneChildEntity> ();
            List<ContactResult> results = m_scenePresence.Scene.PhysicsScene.RaycastWorld (startvector, dir, dir.Length (), 5);

            double distance = Util.GetDistanceTo (startvector, endvector);
            if (distance == 0)
                distance = 0.001;
            Vector3 posToCheck = startvector;
            foreach (ContactResult result in results)
            {
                ISceneChildEntity child = m_scenePresence.Scene.GetSceneObjectPart (result.ConsumerID);
                if (!entities.Contains (child))
                    entities.Add (child);
            }

            return entities;
        }

        #endregion

        #endregion

        #endregion

        #region Enable/Disable walking

        /// <summary>
        /// Blocks walking and sets to only flying
        /// </summary>
        /// <param name="pos"></param>
        public void DisableWalk()
        {
            ShouldFly = true;
            m_scenePresence.ForceFly = true;
            m_closeToPoint = 1.5f;
        }

        /// <summary>
        /// Allows for flying and walkin
        /// </summary>
        /// <param name="pos"></param>
        public void EnableWalk()
        {
            ShouldFly = false;
            m_scenePresence.ForceFly = false;
            m_closeToPoint = 1;
        }

        #endregion

        #region Timers

        private void frames_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                m_frames.Stop ();
                m_startTime.Stop ();
            }
            catch { }
            if (m_scenePresence == null)
                return;
            Update();
            m_startTime.Start ();
            m_frames.Start ();
        }

        private void startTime_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            m_startTime.Stop ();
            GetNextDestination ();
            m_startTime.Start ();
        }

        #endregion

        #region Get next destination / Pause/Resume movement

        private void GetNextDestination()
        {
            if (m_scenePresence == null || m_paused)
                return;

            //Fire the move event
            EventManager.FireGenericEventHandler ("Move", null);

            Vector3 pos;
            TravelMode state;
            bool teleport;
            if (m_nodeGraph.GetNextPosition (m_scenePresence.AbsolutePosition, m_closeToPoint, 60, out pos, out state, out teleport))
            {
                if (state == TravelMode.Fly)
                    FlyTo (pos);
                else if (state == TravelMode.Walk)
                    WalkTo (pos);
                else if (state == TravelMode.Teleport)
                    m_scenePresence.Teleport (pos);
            }
        }

        public void PauseMovement ()
        {
            m_paused = true;
            //Stop movement
            WalkTo (m_scenePresence.AbsolutePosition);
        }

        public void ResumeMovement ()
        {
            m_paused = false;
        }

        #endregion

        #region Update Event

        public void Update ()
        {
            if (m_scenePresence == null || m_paused)
                return;
            //Tell any interested modules that we are ready to go
            EventManager.FireGenericEventHandler ("Update", null);

            //Now move the avatar
            if (State != BotState.Idle)
            {
                bool CheckFly = State == BotState.Flying;
                if (!ShouldFly && CheckFly)
                    ShouldFly = CheckFly;
                Vector3 diffPos = m_destination - m_scenePresence.AbsolutePosition;
                if (Math.Abs (diffPos.X) < m_closeToPoint && Math.Abs (diffPos.Y) < m_closeToPoint &&
                    (!CheckFly || (ShouldFly && Math.Abs (diffPos.Z) < m_closeToPoint))) //If we are flying, Z checking matters
                {
                    State = BotState.Idle;
                    //Restart the start time
                    m_startTime.Stop ();
                    m_startTime.Start ();
                }
                else
                {
                    //Move to the position!
                    switch (State)
                    {
                        case BotState.Walking:
                            walkTo (m_destination);
                            break;

                        case BotState.Flying:
                            flyTo (m_destination);
                            break;
                    }
                }
            }

            if (State == BotState.Idle)
            {
                //We arn't going anywhere, stop movement
                OnBotAgentUpdate (m_movementFlag, m_bodyDirection);
            }
        }

        #endregion


        #region A* Path Following Code

        #region Path declares

        public int cornerStoneX = 128;
        public int cornerStoneY = 128;
        int[,] currentMap = new int[5, 5];
        public bool ShouldFly = false;

        public bool IsOnAPath = false;
        public List<Vector3> WayPoints = new List<Vector3> ();
        public int CurrentWayPoint = 0;
        private float m_closeToPoint = 1;

        #endregion

        #region AStarBot memebers

        public void ReadMap (string map, int X, int Y, int CornerStoneX, int CornerStoneY)
        {
            cornerStoneX = CornerStoneX;
            cornerStoneY = CornerStoneY;
            currentMap = Games.Pathfinding.AStar2DTest.StartPath.ReadMap (map, X, Y);
            if (currentMap[0, 0] == -99)
            {
                m_log.Warn ("The map was found but failed to load. Check the map.");
            }
        }

        public List<Vector3> InnerFindPath (int[,] map, int startX, int startY, int finishX, int finishY)
        {
            CurrentWayPoint = 0; //Reset to the beginning of the list
            Games.Pathfinding.AStar2DTest.StartPath.Map = map;
            Games.Pathfinding.AStar2DTest.StartPath.xLimit = (int)Math.Sqrt (map.Length);
            Games.Pathfinding.AStar2DTest.StartPath.yLimit = (int)Math.Sqrt (map.Length);
            //ShowMap ("", null);
            List<string> points = Games.Pathfinding.AStar2DTest.StartPath.Path (startX, startY, finishX, finishY, 0, 0, 0);

            List<Vector3> waypoints = new List<Vector3> ();
            if (points.Contains ("no_path"))
            {
                m_log.Debug ("I'm sorry I could not find a solution to that path. Teleporting instead");
                return waypoints;
            }

            foreach (string s in points)
            {
                string[] Vector = s.Split (',');

                if (Vector.Length != 3)
                    continue;

                waypoints.Add (new Vector3 (float.Parse (Vector[0]),
                    float.Parse (Vector[1]),
                    float.Parse (Vector[2])));
            }
            return waypoints;
        }

        #endregion

        #region Follow Path

        public void FindPath (Vector3 currentPos, Vector3 finishVector)
        {
            // Bot position converted to map coordinates -maybe here we can check if on Map
            int startX = (int)currentPos.X - cornerStoneX;
            int startY = (int)currentPos.Y - cornerStoneY;

            m_log.Debug ("My Pos " + currentPos.ToString () + " , End Pos " + finishVector.ToString ());

            // Goal position converted to map coordinates
            int finishX = (int)finishVector.X - cornerStoneX;
            int finishY = (int)finishVector.Y - cornerStoneY;
            int finishZ = 25;

            m_scenePresence.StandUp (); //Can't follow a path if sitting

            CurrentWayPoint = 0; //Reset to the beginning of the list
            List<string> points = Games.Pathfinding.AStar2DTest.StartPath.Path (startX, startY, finishX, finishY, finishZ, cornerStoneX, cornerStoneY);

            if (points.Contains ("no_path"))
            {
                m_log.Debug ("I'm sorry I could not find a solution to that path. Teleporting instead");
                m_scenePresence.Teleport (finishVector);
                return;
            }
            else
            {
                if (!IsOnAPath)
                    EventManager.RegisterEventHandler ("Update", FollowUpdate);
                IsOnAPath = true;
            }

            m_nodeGraph.Clear ();

            lock (WayPoints)
            {
                foreach (string s in points)
                {
                    string[] Vector = s.Split (',');

                    if (Vector.Length != 3)
                        continue;

                    m_nodeGraph.Add (new Vector3 (float.Parse (Vector[0]),
                        float.Parse (Vector[1]),
                        float.Parse (Vector[2])), ShouldFly ? TravelMode.Fly : TravelMode.Walk);
                }
            }
        }

        #endregion

        #region Follow Update Method

        /// <summary>
        /// This is called to make the bot walk nicely around every 100 milliseconds by m_frames timer
        /// </summary>
        private object FollowUpdate (string functionName, object param)
        {
            return null;
        }

        #endregion

        #endregion

        #region Following Code 

        #region Following declares

        public IScenePresence FollowSP = null;
        public UUID FollowUUID = UUID.Zero;
        private float m_followCloseToPoint = 1.5f;
        private float m_followLoseAvatarDistance = 1000;
        private const float FollowTimeBeforeUpdate = 10;
        private float CurrentFollowTimeBeforeUpdate = 0;
        private int jumpTry = 0;

        public float FollowCloseToPoint
        {
            get { return m_followCloseToPoint; }
            set { m_followCloseToPoint = value; }
        }

        public float FollowLoseAvatarDistance
        {
            get { return m_followLoseAvatarDistance; }
            set { m_followLoseAvatarDistance = value; }
        }

        /// <summary>
        /// So that other bots can follow us
        /// </summary>
        public List<Bot> ChildFollowers = new List<Bot> ();

        #endregion

        public void FollowAvatar (string avatarName, float followDistance)
        {
            m_scenePresence.Scene.TryGetAvatarByName (avatarName, out FollowSP);
            if (FollowSP == null)
            {
                //Try by UUID then
                try
                {
                    m_scenePresence.Scene.TryGetScenePresence (UUID.Parse (avatarName), out FollowSP);
                }
                catch
                {
                }
            }
            if (FollowSP == null)
            {
                m_log.Warn ("Could not find avatar");
                return;
            }
            FollowUUID = FollowSP.UUID;
            EventManager.RegisterEventHandler ("Update", FollowingUpdate);
            EventManager.RegisterEventHandler ("Move", FollowingMove);
            m_scenePresence.StandUp (); //Can't follow if sitting
            m_followCloseToPoint = followDistance;
        }

        public void StopFollowAvatar ()
        {
            EventManager.UnregisterEventHandler ("Update", FollowingUpdate);
            FollowSP = null; //null out everything
            FollowUUID = UUID.Zero;
        }

        #region Following Update Event

        private object FollowingUpdate (string functionName, object param)
        {
            //Update, time to check where we should be going
            NewFollowing ();
            return null;
        }

        private object FollowingMove (string functionName, object param)
        {
            //Check to see whether we are close to our avatar, and fire the event if needed
            Vector3 targetPos = FollowSP.AbsolutePosition;
            Vector3 currentPos = m_scenePresence.AbsolutePosition;
            double distance = Util.GetDistanceTo (targetPos, currentPos);
            m_scenePresence.SetAlwaysRun = FollowSP.SetAlwaysRun;
            if (distance < m_followCloseToPoint)
            {
                //Fire our event
                EventManager.FireGenericEventHandler ("ToAvatar", null);
                bool fly = FollowSP.PhysicsActor == null ? ShouldFly : FollowSP.PhysicsActor.Flying;
                if (fly)
                    FlyTo (m_scenePresence.AbsolutePosition);
                else
                    WalkTo (m_scenePresence.AbsolutePosition);
            }
            else if (distance > m_followLoseAvatarDistance)
            {
                //Lost the avatar, fire the event
                EventManager.FireGenericEventHandler ("LostAvatar", null);
            }
            return null;
        }

        #endregion

        #region Old Following code

        private void OldFollowing ()
        {
            Vector3 diffAbsPos = FollowSP.AbsolutePosition - m_scenePresence.AbsolutePosition;
            if (Math.Abs (diffAbsPos.X) > m_followCloseToPoint || Math.Abs (diffAbsPos.Y) > m_followCloseToPoint)
            {
                Vector3 targetPos = FollowSP.AbsolutePosition;
                Vector3 ourPos = m_scenePresence.AbsolutePosition;
                bool fly = FollowSP.PhysicsActor == null ? ShouldFly : FollowSP.PhysicsActor.Flying;
                if (!fly && diffAbsPos.Z > 0.25)
                {
                    if (!m_allowJump)
                        targetPos.Z = ourPos.Z + 0.15f;
                    else if (m_UseJumpDecisionTree)
                    {
                        if (!JumpDecisionTree (m_scenePresence.AbsolutePosition, targetPos))
                            targetPos.Z = ourPos.Z + 0.15f;
                        else
                            jumpTry++;
                    }
                    if (jumpTry > 5 && diffAbsPos.Z > 3)
                    {
                        fly = true;
                    }
                }
                m_nodeGraph.Clear ();
                m_nodeGraph.Add (targetPos, fly ? TravelMode.Fly : TravelMode.Walk);
                m_scenePresence.SetAlwaysRun = FollowSP.SetAlwaysRun;
            }
            else
            {
                //Stop the bot then
                EventManager.FireGenericEventHandler ("ToAvatar", null);
                State = BotState.Idle;
                m_nodeGraph.Clear ();
            }
        }

        #endregion

        #region New Following code

        private void ShowMap (string mod, string[] cmd)
        {
            int sqrt = (int)Math.Sqrt (map.Length);
            for (int x = sqrt - 1; x > -1; x--)
            {
                string line = "";
                for (int y = sqrt - 1; y > -1; y--)
                {
                    if (x == 11 * resolution && y == 11 * resolution)
                    {
                        line += "XX" + ",";
                    }
                    else
                    {
                        if (map[x, y].ToString ().Length < 2)
                            line += " " + map[x, y] + ",";
                        else
                            line += map[x, y] + ",";
                    }
                }

                m_log.Warn (line.Remove (line.Length - 1));
            }
            m_log.Warn ("\n");
        }

        private int resolution = 10;
        private int[,] map;
        private int failedToMove = 0;
        private int sincefailedToMove = 0;
        private Vector3 m_lastPos = Vector3.Zero;
        private void NewFollowing ()
        {
            // FOLLOW an avatar - this is looking for an avatar UUID so wont follow a prim here  - yet
            //Call this each iteration so that if the av leaves, we don't get stuck following a null person
            FollowSP = m_scenePresence.Scene.GetScenePresence (FollowUUID);
            //If its still null, the person doesn't exist, cancel the follow and return
            if (FollowSP == null)
                return;
            m_scenePresence.SetAlwaysRun = FollowSP.SetAlwaysRun;
            Vector3 targetPos = FollowSP.AbsolutePosition;
            Vector3 currentPos = m_scenePresence.AbsolutePosition;
            CurrentFollowTimeBeforeUpdate++;
            m_closeToPoint = 0.5f;
            //if (CurrentFollowTimeBeforeUpdate <= 2)
            //    return;
            CurrentFollowTimeBeforeUpdate = 0;

            resolution = 3;
            double distance = Util.GetDistanceTo (targetPos, currentPos);
            if (distance > 10) //Greater than 10 meters, give up
            {
                m_log.Warn ("Target is out of range");
                //Try old style then
                OldFollowing ();
                foreach (Bot bot in ChildFollowers)
                {
                    bot.ParentMoved (m_nodeGraph);
                }
                return;
            }
            else if (distance < m_followCloseToPoint)
            {
                if (jumpTry > 0)
                {
                    m_scenePresence.PhysicsActor.Flying = false;
                    walkTo (m_scenePresence.AbsolutePosition);
                }
                jumpTry = 0;
                foreach (Bot bot in ChildFollowers)
                {
                    bot.ParentMoved (m_nodeGraph);
                }
            }

            List<ISceneChildEntity> raycastEntities = llCastRay (m_scenePresence.AbsolutePosition, FollowSP.AbsolutePosition);

            if (raycastEntities.Count == 0)
            {
                //Nothing between us and the target, go for it!
                OldFollowing ();
                foreach (Bot bot in ChildFollowers)
                {
                    bot.ParentMoved (m_nodeGraph);
                }
            }
            else
            {
                map = new int[22 * resolution, 22 * resolution]; //10 * resolution squares in each direction from our pos
                //We are in the center (11, 11) and our target is somewhere else
                int targetX = 11 * resolution, targetY = 11 * resolution;
                //Find where our target is on the map
                FindTargets (currentPos, targetPos, ref targetX, ref targetY);
                ISceneEntity[] entities = m_scenePresence.Scene.Entities.GetEntities (currentPos, 30);

                //Add all the entities to the map
                foreach (ISceneEntity entity in entities)
                {
                    if (entity.AbsolutePosition.Z < m_scenePresence.AbsolutePosition.Z + m_scenePresence.PhysicsActor.Size.Z / 2 + m_scenePresence.Velocity.Z / 2 &&
                        entity.AbsolutePosition.Z > m_scenePresence.AbsolutePosition.Z - m_scenePresence.PhysicsActor.Size.Z / 2 + m_scenePresence.Velocity.Z / 2)
                    {
                        int entitybaseX = (11 * resolution);
                        int entitybaseY = (11 * resolution);
                        //Find the bottom left corner, and then build outwards from it
                        FindTargets (currentPos, entity.AbsolutePosition - (entity.OOBsize / 2), ref entitybaseX, ref entitybaseY);
                        for (int x = (int)-(0.5 * resolution); x < entity.OOBsize.X * 2 * resolution + ((int)(0.5 * resolution)); x++)
                        {
                            for (int y = (int)-(0.5 * resolution); y < entity.OOBsize.Y * 2 * resolution + ((int)(0.5 * resolution)); y++)
                            {
                                if (entitybaseX + x > 0 && entitybaseY + y > 0 &&
                                    entitybaseX + x < (22 * resolution) && entitybaseY + y < (22 * resolution))
                                    if (x < 0 || y < 0 || x > (entity.OOBsize.X * 2) * resolution || y > (entity.OOBsize.Y * 2) * resolution)
                                        map[entitybaseX + x, entitybaseY + y] = 3; //Its a side hit, lock it down a bit
                                    else
                                        map[entitybaseX + x, entitybaseY + y] = -1; //Its a hit, lock it down
                            }
                        }
                    }
                }

                for (int x = 0; x < (22 * resolution); x++)
                {
                    for (int y = 0; y < (22 * resolution); y++)
                    {
                        if (x == targetX && y == targetY)
                            map[x, y] = 1;
                        else if (x == 11 * resolution && y == 11 * resolution)
                        {
                            int old = map[x, y];
                            map[x, y] = 1;
                        }
                        else if (map[x, y] == 0)
                            map[x, y] = 1;
                    }
                }

                //ShowMap ("", null);
                List<Vector3> path = InnerFindPath (map, (11 * resolution), (11 * resolution), targetX, targetY);

                int i = 0;
                Vector3 nextPos = ConvertPathToPos (entities, currentPos, path, ref i);
                Vector3 diffAbsPos = nextPos - targetPos;
                if (nextPos != Vector3.Zero)
                {
                    m_nodeGraph.Clear ();
                }
                else
                {
                    //Try the old way
                    OldFollowing ();
                    return;
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
                                if (!JumpDecisionTree (m_scenePresence.AbsolutePosition, targetPos))
                                {
                                    jumpTry--;
                                    targetPos.Z = nextPos.Z + 0.15f;
                                }
                                else
                                    jumpTry++;
                            }
                            else
                                jumpTry--;
                        }
                    }
                    else if (!fly)
                        jumpTry--;
                    nextPos.Z = targetPos.Z; //Fix the Z coordinate

                    m_nodeGraph.Add (nextPos, fly ? TravelMode.Fly : TravelMode.Walk);
                    i++;
                    nextPos = ConvertPathToPos (entities, currentPos, path, ref i);
                }
            }
            foreach (Bot bot in ChildFollowers)
            {
                bot.ParentMoved (m_nodeGraph);
            }
        }

        public void ParentMoved (NodeGraph graph)
        {
            m_nodeGraph.CopyFrom (graph);
        }

        private Vector3 ConvertPathToPos (ISceneEntity[] entites, Vector3 originalPos, List<Vector3> path, ref int i)
        {
        start:
            if (i == path.Count)
                return Vector3.Zero;
            if (path[i].X == (11 * resolution) && path[i].Y == (11 * resolution))
            {
                i++;
                goto start;
            }
            Vector3 pos = path[i];
            Vector3 newPos = originalPos - new Vector3 (((11 * resolution) - pos.X) / resolution, ((11 * resolution) - pos.Y) / resolution, 0);

            if (i < 2)
            {
                if (m_lastPos.ApproxEquals (newPos, 1))
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
                    else if (!m_lastPos.ApproxEquals (newPos, 2))
                        sincefailedToMove++;
                }
            }
            if (failedToMove > 1)
                CleanUpPos (entites, ref newPos);
            return newPos;
        }

        private void CleanUpPos (ISceneEntity[] entites, ref Vector3 pos)
        {
            List<ISceneChildEntity> childEntities = llCastRay (m_scenePresence.AbsolutePosition, pos);
            int restartNum = 0;
        restart:
            bool needsRestart = false;
            foreach (ISceneChildEntity entity in childEntities)
            {
                if (entity.AbsolutePosition.Z < m_scenePresence.AbsolutePosition.Z + 2 &&
                    entity.AbsolutePosition.Z > m_scenePresence.AbsolutePosition.Z - 2 &&
                    entity.Scale.Z > m_scenePresence.PhysicsActor.Size.Z)
                {
                    //If this position is inside an entity + its size + avatar size, move it out!
                    float sizeXPlus = (entity.AbsolutePosition.X + (entity.Scale.X / 2) + (m_scenePresence.PhysicsActor.Size.X / 2));
                    float sizeXNeg = (entity.AbsolutePosition.X - (entity.Scale.X / 2) - (m_scenePresence.PhysicsActor.Size.X / 2));
                    float sizeYPlus = (entity.AbsolutePosition.Y + (entity.Scale.Y / 2) + (m_scenePresence.PhysicsActor.Size.Y / 2));
                    float sizeYNeg = (entity.AbsolutePosition.Y - (entity.Scale.Y / 2) - (m_scenePresence.PhysicsActor.Size.Y / 2));

                    if (pos.X < sizeXPlus && pos.X > sizeXNeg)
                    {
                        if (pos.X < entity.AbsolutePosition.X)
                            pos.X = sizeXNeg - (m_scenePresence.PhysicsActor.Size.X / 2);
                        else
                            pos.X = sizeXPlus + (m_scenePresence.PhysicsActor.Size.X / 2);
                        needsRestart = true;
                    }
                    if (pos.Y < sizeYPlus && pos.Y > sizeYNeg)
                    {
                        if (pos.Y < entity.AbsolutePosition.Y)
                            pos.Y = sizeYNeg - (m_scenePresence.PhysicsActor.Size.Y / 2);
                        else
                            pos.Y = sizeYPlus + (m_scenePresence.PhysicsActor.Size.Y / 2);
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

        private void FindTargets (Vector3 currentPos, Vector3 targetPos, ref int targetX, ref int targetY)
        {
            //we're at pos 11, 11, so we have to add/subtract from there
            float xDiff = (targetPos.X - currentPos.X);
            float yDiff = (targetPos.Y - currentPos.Y);

            targetX += (int)(xDiff * resolution);
            targetY += (int)(yDiff * resolution);
        }

        #endregion

        #endregion






        #region Chat interface

        public void SendChatMessage (int sayType, string message, int channel)
        {
            OSChatMessage args = new OSChatMessage ();
            args.Message = message;
            args.Channel = channel;
            args.From = FirstName + " " + LastName;
            args.Position = m_scenePresence.AbsolutePosition;
            args.Sender = this;
            args.Type = (ChatTypeEnum)sayType;
            args.Scene = m_scene;

            OnBotChatFromViewer (this, args);
        }

        #endregion

        #region Useful IClientAPI members

        public void SendInstantMessage (GridInstantMessage im)
        {
            if (im.dialog == (byte)InstantMessageDialog.GodLikeRequestTeleport ||
                im.dialog == (byte)InstantMessageDialog.RequestTeleport)
            {
                if (m_avatarCreatorID == im.fromAgentID || this.Scene.Permissions.IsAdministrator (im.fromAgentID))
                {
                    ulong handle = 0;
                    uint x = 128;
                    uint y = 128;
                    uint z = 70;

                    Util.ParseFakeParcelID (im.imSessionID, out handle, out x, out y, out z);
                    m_scenePresence.Teleport (new Vector3 (x, y, z));
                }
            }
        }

        protected virtual void OnBotChatFromViewer (IClientAPI sender, OSChatMessage e)
        {
            OnChatFromClient (sender, e);
        }

        protected virtual void OnBotAgentUpdate (uint controlFlag, Quaternion bodyRotation)
        {
            if (OnAgentUpdate != null)
            {
                AgentUpdateArgs pack = new AgentUpdateArgs ();
                pack.ControlFlags = controlFlag;
                pack.BodyRotation = bodyRotation;
                OnAgentUpdate (this, pack);
            }
        }

        protected virtual void OnBotLogout ()
        {
            if (OnLogout != null)
            {
                OnLogout (this);
            }
        }

        protected virtual void OnBotConnectionClosed ()
        {
            OnConnectionClosed (this);
        }

        #endregion

        #region IClientAPI

        #region IClientCore Members

        private readonly Dictionary<Type, object> m_clientInterfaces = new Dictionary<Type, object> ();

        public T Get<T> ()
        {
            return (T)m_clientInterfaces[typeof (T)];
        }

        public bool TryGet<T> (out T iface)
        {
            if (m_clientInterfaces.ContainsKey (typeof (T)))
            {
                iface = (T)m_clientInterfaces[typeof (T)];
                return true;
            }
            iface = default (T);
            return false;
        }

        protected virtual void RegisterInterfaces ()
        {
        }

        protected void RegisterInterface<T> (T iface)
        {
            lock (m_clientInterfaces)
            {
                if (!m_clientInterfaces.ContainsKey (typeof (T)))
                {
                    m_clientInterfaces.Add (typeof (T), iface);
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

        public event TerrainUnacked OnUnackedTerrain;

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

        public void QueueDelayedUpdate (Mischel.Collections.PriorityQueueItem<EntityUpdate, double> it)
        {
        }

        public virtual void SendRegionHandshake (RegionInfo regionInfo, RegionHandshakeArgs args)
        {
            if (OnRegionHandShakeReply != null)
            {
                OnRegionHandShakeReply (this);
            }
        }

        #endregion

        #region IClientAPI Members

        public UUID SessionId
        {
            get;
            set;
        }

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
            get
            {
                return true;
            }
            set
            {
            }
        }

        public bool IsLoggingOut
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        public bool SendLogoutPacketWhenClosing
        {
            set { }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return new IPEndPoint (IPAddress.Loopback, (ushort)m_circuitCode); }
        }

        public void SetDebugPacketLevel (int newDebug)
        {
            
        }

        public void ProcessInPacket (OpenMetaverse.Packets.Packet NewPack)
        {
            
        }

        public void Stop ()
        {
            Close (true);
        }

        public void Kick (string message)
        {
            Close (true);
        }

        public void SendWearables (AvatarWearable[] wearables, int serial)
        {
            
        }

        public void SendAgentCachedTexture (List<CachedAgentArgs> args)
        {
            
        }

        public void SendAppearance (UUID agentID, byte[] visualParams, byte[] textureEntry)
        {
            
        }

        public void SendStartPingCheck (byte seq)
        {
            
        }

        public void SendKillObject (ulong regionHandle, IEntity[] entities)
        {
            
        }

        public void SendKillObject (ulong regionHandle, uint[] entities)
        {
            
        }

        public void SendAnimations (AnimationGroup animations)
        {
            
        }

        public void SendChatMessage (string message, byte type, Vector3 fromPos, string fromName, UUID fromAgentID, byte source, byte audible)
        {
            
        }

        public void SendGenericMessage (string method, List<string> message)
        {
            
        }

        public void SendGenericMessage (string method, List<byte[]> message)
        {
            
        }

        public void SendLayerData (short[] map)
        {
            
        }

        public void SendLayerData (int px, int py, short[] map)
        {
            
        }

        public void SendLayerData (int[] x, int[] y, short[] map, TerrainPatch.LayerType type)
        {
            
        }

        public void SendWindData (Vector2[] windSpeeds)
        {
            
        }

        public void SendCloudData (float[] cloudCover)
        {
            
        }

        public void MoveAgentIntoRegion (RegionInfo regInfo, Vector3 pos, Vector3 look)
        {
            
        }

        public AgentCircuitData RequestClientInfo ()
        {
            return m_circuitData;
        }

        public void SendMapBlock (List<MapBlockData> mapBlocks, uint flag)
        {
            
        }

        public void SendLocalTeleport (Vector3 position, Vector3 lookAt, uint flags)
        {
            
        }

        public void SendRegionTeleport (ulong regionHandle, byte simAccess, IPEndPoint regionExternalEndPoint, uint locationID, uint flags, string capsURL)
        {
            
        }

        public void SendTeleportFailed (string reason)
        {
            
        }

        public void SendTeleportStart (uint flags)
        {
            
        }

        public void SendTeleportProgress (uint flags, string message)
        {
            
        }

        public void SendMoneyBalance (UUID transaction, bool success, byte[] description, int balance)
        {
            
        }

        public void SendPayPrice (UUID objectID, int[] payPrice)
        {
            
        }

        public void SendCoarseLocationUpdate (List<UUID> users, List<Vector3> CoarseLocations)
        {
            
        }

        public void SetChildAgentThrottle (byte[] throttle)
        {
            
        }

        public void SendAvatarDataImmediate (IEntity avatar)
        {
            
        }

        public void SendAvatarUpdate(IEnumerable<EntityUpdate> updates)
            {

            }

        public void SendPrimUpdate(IEnumerable<EntityUpdate> updates)
            {

            }

        public void FlushPrimUpdates ()
        {
            
        }

        public void SendInventoryFolderDetails (UUID ownerID, UUID folderID, List<InventoryItemBase> items, List<InventoryFolderBase> folders, int version, bool fetchFolders, bool fetchItems)
        {
            
        }

        public void SendInventoryItemDetails (UUID ownerID, InventoryItemBase item)
        {
            
        }

        public void SendInventoryItemCreateUpdate (InventoryItemBase Item, uint callbackId)
        {
            
        }

        public void SendRemoveInventoryItem (UUID itemID)
        {
            
        }

        public void SendTakeControls (int controls, bool passToAgent, bool TakeControls)
        {
            
        }

        public void SendTaskInventory (UUID taskID, short serial, byte[] fileName)
        {
            
        }

        public void SendBulkUpdateInventory (InventoryNodeBase node)
        {
            
        }

        public void SendXferPacket (ulong xferID, uint packet, byte[] data)
        {
            
        }

        public void SendAbortXferPacket (ulong xferID)
        {
            
        }

        public void SendEconomyData (float EnergyEfficiency, int ObjectCapacity, int ObjectCount, int PriceEnergyUnit, int PriceGroupCreate, int PriceObjectClaim, float PriceObjectRent, float PriceObjectScaleFactor, int PriceParcelClaim, float PriceParcelClaimFactor, int PriceParcelRent, int PricePublicObjectDecay, int PricePublicObjectDelete, int PriceRentLight, int PriceUpload, int TeleportMinPrice, float TeleportPriceExponent)
        {
            
        }

        public void SendAvatarPickerReply (AvatarPickerReplyAgentDataArgs AgentData, List<AvatarPickerReplyDataArgs> Data)
        {
            
        }

        public void SendAgentDataUpdate (UUID agentid, UUID activegroupid, string firstname, string lastname, ulong grouppowers, string groupname, string grouptitle)
        {
            
        }

        public void SendPreLoadSound (UUID objectID, UUID ownerID, UUID soundID)
        {
            
        }

        public void SendPlayAttachedSound (UUID soundID, UUID objectID, UUID ownerID, float gain, byte flags)
        {
            
        }

        public void SendTriggeredSound (UUID soundID, UUID ownerID, UUID objectID, UUID parentID, ulong handle, Vector3 position, float gain)
        {
            
        }

        public void SendAttachedSoundGainChange (UUID objectID, float gain)
        {
            
        }

        public void SendNameReply (UUID profileId, string firstname, string lastname)
        {
            
        }

        public void SendAlertMessage (string message)
        {
            
        }

        public void SendAgentAlertMessage (string message, bool modal)
        {
            
        }

        public void SendLoadURL (string objectname, UUID objectID, UUID ownerID, bool groupOwned, string message, string url)
        {
            
        }

        public void SendDialog (string objectname, UUID objectID, string ownerFirstName, string ownerLastName, string msg, UUID textureID, int ch, string[] buttonlabels)
        {
            
        }

        public void SendSunPos (Vector3 sunPos, Vector3 sunVel, ulong CurrentTime, uint SecondsPerSunCycle, uint SecondsPerYear, float OrbitalPosition)
        {
            
        }

        public void SendViewerEffect (OpenMetaverse.Packets.ViewerEffectPacket.EffectBlock[] effectBlocks)
        {
            
        }

        public UUID GetDefaultAnimation (string name)
        {
            return UUID.Zero;
        }

        public void SendAvatarProperties (UUID avatarID, string aboutText, string bornOn, byte[] charterMember, string flAbout, uint flags, UUID flImageID, UUID imageID, string profileURL, UUID partnerID)
        {
            
        }

        public void SendScriptQuestion (UUID taskID, string taskName, string ownerName, UUID itemID, int question)
        {
            
        }

        public void SendHealth (float health)
        {
            
        }

        public void SendEstateList (UUID invoice, int code, UUID[] Data, uint estateID)
        {
            
        }

        public void SendBannedUserList (UUID invoice, EstateBan[] banlist, uint estateID)
        {
            
        }

        public void SendRegionInfoToEstateMenu (RegionInfoForEstateMenuArgs args)
        {
            
        }

        public void SendEstateCovenantInformation (UUID covenant, int covenantLastUpdated)
        {
            
        }

        public void SendDetailedEstateData (UUID invoice, string estateName, uint estateID, uint parentEstate, uint estateFlags, uint sunPosition, UUID covenant, int covenantLastUpdated, string abuseEmail, UUID estateOwner)
        {
            
        }

        public void SendLandProperties (int sequence_id, bool snap_selection, int request_result, LandData landData, float simObjectBonusFactor, int parcelObjectCapacity, int simObjectCapacity, uint regionFlags)
        {
            
        }

        public void SendLandAccessListData (List<UUID> avatars, uint accessFlag, int localLandID)
        {
            
        }

        public void SendForceClientSelectObjects (List<uint> objectIDs)
        {
            
        }

        public void SendCameraConstraint (Vector4 ConstraintPlane)
        {
            
        }

        public void SendLandObjectOwners (List<LandObjectOwners> objOwners)
        {
            
        }

        public void SendLandParcelOverlay (byte[] data, int sequence_id)
        {
            
        }

        public void SendParcelMediaCommand (uint flags, ParcelMediaCommandEnum command, float time)
        {
            
        }

        public void SendParcelMediaUpdate (string mediaUrl, UUID mediaTextureID, byte autoScale, string mediaType, string mediaDesc, int mediaWidth, int mediaHeight, byte mediaLoop)
        {
            
        }

        public void SendAssetUploadCompleteMessage (sbyte AssetType, bool Success, UUID AssetFullID)
        {
            
        }

        public void SendConfirmXfer (ulong xferID, uint PacketID)
        {
            
        }

        public void SendXferRequest (ulong XferID, short AssetType, UUID vFileID, byte FilePath, byte[] FileName)
        {
            
        }

        public void SendInitiateDownload (string simFileName, string clientFileName)
        {
            
        }

        public void SendImageFirstPart (ushort numParts, UUID ImageUUID, uint ImageSize, byte[] ImageData, byte imageCodec)
        {
            
        }

        public void SendImageNextPart (ushort partNumber, UUID imageUuid, byte[] imageData)
        {
            
        }

        public void SendImageNotFound (UUID imageid)
        {
            
        }

        public void SendSimStats (SimStats stats)
        {
            
        }

        public void SendObjectPropertiesFamilyData (uint RequestFlags, UUID ObjectUUID, UUID OwnerID, UUID GroupID, uint BaseMask, uint OwnerMask, uint GroupMask, uint EveryoneMask, uint NextOwnerMask, int OwnershipCost, byte SaleType, int SalePrice, uint Category, UUID LastOwnerID, string ObjectName, string Description)
        {
            
        }

        public void SendObjectPropertiesReply (List<IEntity> part)
        {
            
        }

        public void SendAgentOffline (UUID[] agentIDs)
        {
            
        }

        public void SendAgentOnline (UUID[] agentIDs)
        {
            
        }

        public void SendSitResponse (UUID TargetID, Vector3 OffsetPos, Quaternion SitOrientation, bool autopilot, Vector3 CameraAtOffset, Vector3 CameraEyeOffset, bool ForceMouseLook)
        {
            
        }

        public void SendAdminResponse (UUID Token, uint AdminLevel)
        {
            
        }

        public void SendGroupMembership (GroupMembershipData[] GroupMembership)
        {
            
        }

        public void SendGroupNameReply (UUID groupLLUID, string GroupName)
        {
            
        }

        public void SendJoinGroupReply (UUID groupID, bool success)
        {
            
        }

        public void SendEjectGroupMemberReply (UUID agentID, UUID groupID, bool success)
        {
            
        }

        public void SendLeaveGroupReply (UUID groupID, bool success)
        {
            
        }

        public void SendCreateGroupReply (UUID groupID, bool success, string message)
        {
            
        }

        public void SendLandStatReply (uint reportType, uint requestFlags, uint resultCount, LandStatReportItem[] lsrpia)
        {
            
        }

        public void SendScriptRunningReply (UUID objectID, UUID itemID, bool running)
        {
            
        }

        public void SendAsset (AssetRequestToClient req)
        {
            
        }

        public byte[] GetThrottlesPacked (float multiplier)
        {
            return new byte[0];
        }

        public void SendBlueBoxMessage (UUID FromAvatarID, string FromAvatarName, string Message)
        {
            
        }

        public void SendLogoutPacket ()
        {
            
        }

        public EndPoint GetClientEP ()
        {
            return null;
        }

        public void SendSetFollowCamProperties (UUID objectID, SortedDictionary<int, float> parameters)
        {
            
        }

        public void SendClearFollowCamProperties (UUID objectID)
        {
            
        }

        public void SendRegionHandle (UUID regoinID, ulong handle)
        {
            
        }

        public void SendParcelInfo (LandData land, UUID parcelID, uint x, uint y, string SimName)
        {
            
        }

        public void SendScriptTeleportRequest (string objName, string simName, Vector3 pos, Vector3 lookAt)
        {
            
        }

        public void SendDirPlacesReply (UUID queryID, DirPlacesReplyData[] data)
        {
            
        }

        public void SendDirPeopleReply (UUID queryID, DirPeopleReplyData[] data)
        {
            
        }

        public void SendDirEventsReply (UUID queryID, DirEventsReplyData[] data)
        {
        }

        public void SendDirGroupsReply (UUID queryID, DirGroupsReplyData[] data)
        {
        }

        public void SendDirClassifiedReply (UUID queryID, DirClassifiedReplyData[] data)
        {
        }

        public void SendDirLandReply (UUID queryID, DirLandReplyData[] data)
        {
        }

        public void SendDirPopularReply (UUID queryID, DirPopularReplyData[] data)
        {
        }

        public void SendEventInfoReply (EventData info)
        {
        }

        public void SendMapItemReply (mapItemReply[] replies, uint mapitemtype, uint flags)
        {
        }

        public void SendAvatarGroupsReply (UUID avatarID, GroupMembershipData[] data)
        {
        }

        public void SendOfferCallingCard (UUID srcID, UUID transactionID)
        {
        }

        public void SendAcceptCallingCard (UUID transactionID)
        {
        }

        public void SendDeclineCallingCard (UUID transactionID)
        {
        }

        public void SendTerminateFriend (UUID exFriendID)
        {
        }

        public void SendAvatarClassifiedReply (UUID targetID, UUID[] classifiedID, string[] name)
        {
        }

        public void SendClassifiedInfoReply (UUID classifiedID, UUID creatorID, uint creationDate, uint expirationDate, uint category, string name, string description, UUID parcelID, uint parentEstate, UUID snapshotID, string simName, Vector3 globalPos, string parcelName, byte classifiedFlags, int price)
        {
        }

        public void SendAgentDropGroup (UUID groupID)
        {
        }

        public void SendAvatarNotesReply (UUID targetID, string text)
        {
        }

        public void SendAvatarPicksReply (UUID targetID, Dictionary<UUID, string> picks)
        {
        }

        public void SendPickInfoReply (UUID pickID, UUID creatorID, bool topPick, UUID parcelID, string name, string desc, UUID snapshotID, string user, string originalName, string simName, Vector3 posGlobal, int sortOrder, bool enabled)
        {
        }

        public void SendAvatarClassifiedReply (UUID targetID, Dictionary<UUID, string> classifieds)
        {
        }

        public void SendParcelDwellReply (int localID, UUID parcelID, float dwell)
        {
        }

        public void SendUserInfoReply (bool imViaEmail, bool visible, string email)
        {
        }

        public void SendUseCachedMuteList ()
        {
        }

        public void SendMuteListUpdate (string filename)
        {
        }

        public void SendGroupActiveProposals (UUID groupID, UUID transactionID, GroupActiveProposals[] Proposals)
        {
        }

        public void SendGroupVoteHistory (UUID groupID, UUID transactionID, GroupVoteHistory Vote, GroupVoteHistoryItem[] Items)
        {
        }

        public bool AddGenericPacketHandler (string MethodName, GenericMessage handler)
        {
            return true;
        }

        public bool RemoveGenericPacketHandler (string MethodName)
        {
            return true;
        }

        public void SendRebakeAvatarTextures (UUID textureID)
        {
        }

        public void SendAvatarInterestsReply (UUID avatarID, uint wantMask, string wantText, uint skillsMask, string skillsText, string languages)
        {
        }

        public void SendGroupAccountingDetails (IClientAPI sender, UUID groupID, UUID transactionID, UUID sessionID, int amt)
        {
        }

        public void SendGroupAccountingSummary (IClientAPI sender, UUID groupID, uint moneyAmt, int totalTier, int usedTier)
        {
        }

        public void SendGroupTransactionsSummaryDetails (IClientAPI sender, UUID groupID, UUID transactionID, UUID sessionID, int amt)
        {
        }

        public void SendChangeUserRights (UUID agentID, UUID friendID, int rights)
        {
        }

        public void SendTextBoxRequest (string message, int chatChannel, string objectname, string ownerFirstName, string ownerLastName, UUID objectId)
        {
        }

        public void SendPlacesQuery (ExtendedLandData[] LandData, UUID queryID, UUID transactionID)
        {
        }

        public void FireUpdateParcel (LandUpdateArgs args, int LocalID)
        {
        }

        public void SendTelehubInfo (Vector3 TelehubPos, Quaternion TelehubRot, List<Vector3> SpawnPoint, UUID ObjectID, string Name)
        {
        }

        public void StopFlying (IEntity presence)
        {
        }

        public void Reset ()
        {
        }

        #endregion
    }
}
