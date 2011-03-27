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
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework.Client;
using System.Diagnostics;
using Aurora.Framework;
using System.IO;

namespace Aurora.BotManager
{
    public class RexBot : IRexBot, IClientAPI, IClientCore
    {
        #region Declares
        public enum RexBotState { Idle, Walking, Flying, Unknown }

        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Vector3 DEFAULT_START_POSITION = new Vector3(128, 128, 128);

        private static UInt32 UniqueId = 1;

        private string m_firstName = "Default";
        private string m_lastName = "RexBot" + UniqueId.ToString();

        private uint m_movementFlag = 0;
        private Quaternion m_bodyDirection = Quaternion.Identity;
        private short m_frameCount = 0;

        private UUID m_myID = UUID.Random();
        private Scene m_scene;
        private IScenePresence m_scenePresence;
        private AgentCircuitData m_circuitData;

        private RexBotState m_currentState = RexBotState.Idle;
        public RexBotState State
        {
            get { return m_currentState; }
            set 
            { 
                m_previousState = m_currentState;  
                m_currentState = value;
            }
        }
        private RexBotState m_previousState = RexBotState.Idle;
        
        private bool m_autoMove = true;
        private Vector3 m_destination;

        private System.Timers.Timer m_frames;
        private System.Timers.Timer m_walkTime;
        private System.Timers.Timer m_startTime;

        #region Following declares

        private const float followDistance = 2.5f;
        public bool IsFollowing = false;
        public IScenePresence FollowSP = null;
        public string FollowName = "";
        private const float FollowTimeBeforeUpdate = 10;
        private float CurrentFollowTimeBeforeUpdate = 0;

        #endregion

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

        private NavMeshInstance m_navMesh;

        private float m_RexCharacterSpeedMod = 1.0f;

        public float RexCharacterSpeedMod
        {
            get { return m_RexCharacterSpeedMod; }
            set { m_RexCharacterSpeedMod = value; }
        }

        public NavMeshInstance NavMeshInstance
        {
            get { return m_navMesh; }
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

        // creates new bot on the default location
        public RexBot(Scene scene, AgentCircuitData data)
        {
            RegisterInterfaces();

            m_circuitData = data;
            m_scene = scene;
            m_navMesh = null;
            
            m_circuitCode = UniqueId;
            m_frames = new System.Timers.Timer(100);
            m_frames.Start();
            m_frames.Elapsed += (frames_Elapsed);
            m_walkTime = new System.Timers.Timer(30000);
            m_walkTime.Elapsed += (walkTime_Elapsed);
            m_startTime = new System.Timers.Timer(10);
            m_startTime.Elapsed += (startTime_Elapsed);

            UniqueId++;
        }

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

            m_scenePresence.Teleport(DEFAULT_START_POSITION);
        }

        public void Close()
        {
            // Pull Client out of Region
            m_log.Info("[RexBot]: Removing bot " + Name);

            OnBotLogout();

            //raiseevent on the packet server to Shutdown the circuit
            OnBotConnectionClosed();

            m_frames.Stop();
            m_walkTime.Stop();
            m_startTime.Stop ();
        }

        #endregion

        #region SetPath

        public void SetPath(NavMesh mesh, int startNode, bool reverse, int timeOut, bool tpToStart)
        {
            m_navMesh = new NavMeshInstance(mesh, startNode, reverse, timeOut);

            SetDefaultWalktimeInterval();

            if(tpToStart)
                m_scenePresence.Teleport(m_navMesh.GetNextNode().Position);
            GetNextDestination();
        }

        #endregion

        #region Move/Rotate the bot

        // Makes the bot walk to the specified destination
        private void WalkTo(Vector3 destination)
        {
            if (Util.IsZeroVector(destination - m_scenePresence.AbsolutePosition) == false)
            {
                walkTo(destination);
                State = RexBotState.Walking;

                m_destination = destination;

                m_walkTime.Stop();
                SetDefaultWalktimeInterval();
                m_walkTime.Start();
            }
        }

        // Makes the bot fly to the specified destination
        private void FlyTo(Vector3 destination)
        {
            if (Util.IsZeroVector(destination - m_scenePresence.AbsolutePosition) == false)
            {
                flyTo(destination);
                m_destination = destination;
                State = RexBotState.Flying;

                m_walkTime.Stop();
                SetDefaultWalktimeInterval();
                m_walkTime.Start();
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
            Vector3 bot_toward = Util.GetNormalizedVector(destination - m_scenePresence.AbsolutePosition);
            Quaternion rot_result = llRotBetween(bot_forward, bot_toward);
            m_bodyDirection = rot_result;
            m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_AT_POS;

            OnBotAgentUpdate(m_movementFlag, m_bodyDirection);
            m_movementFlag = (uint)AgentManager.ControlFlags.NONE;
        }

        #endregion

        #region Start/Stop movement

        public void UnpauseAutoMove()
        {
            EnableAutoMove(true, false);
        }

        public void PauseAutoMove()
        {
            EnableAutoMove(false, false);
        }

        public void StopAutoMove()
        {
            EnableAutoMove(false, true);
        }

        public void EnableAutoMove()
        {
            EnableAutoMove(true, true);
        }

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

        #region Set Av Speed

        public void SetMovementSpeedMod(float speed)
        {
            m_scenePresence.SpeedModifier = speed;
        }

        #endregion

        #region Automove and walk interval

        private void SetDefaultWalktimeInterval()
        {
            if (m_navMesh != null)
                m_walkTime.Interval = m_navMesh.TimeOut * 1000;
            else
                m_walkTime.Interval = 600000; // 10 minutes to get to destination 
        }

        private void EnableAutoMove(bool enable, bool stopWarpTimer)
        {
            if (enable != m_autoMove)
            {
                m_autoMove = enable;
                if (enable)
                {
                    State = m_previousState; // restore previous state
                    if (stopWarpTimer)
                    {
                        m_walkTime.Stop();
                        SetDefaultWalktimeInterval();
                        m_walkTime.Start();
                    }
                }
                else
                {
                    State = RexBotState.Idle;
                    if (stopWarpTimer)
                    {
                        m_walkTime.Stop();
                    }
                    m_startTime.Stop();
                    m_movementFlag = (uint)AgentManager.ControlFlags.NONE;
                }
            }
        }

        #endregion

        #region rotation helper functions
        private Vector3 llRot2Fwd(Quaternion r)
        {
            return (new Vector3(1, 0, 0) * r);
        }

        private Quaternion llRotBetween(Vector3 a, Vector3 b)
        {
            //A and B should both be normalized
            double dotProduct = Vector3.Dot(a, b);
            Vector3 crossProduct = Vector3.Cross(a, b);
            double magProduct = Vector3.Distance(Vector3.Zero, a) * Vector3.Distance(Vector3.Zero,b);
            double angle = Math.Acos(dotProduct / magProduct);
            Vector3 axis = Vector3.Normalize(crossProduct);
            float s = (float)Math.Sin(angle / 2);

            return new Quaternion(axis.X * s, axis.Y * s, axis.Z * s, (float)Math.Cos(angle / 2));
        }

        #endregion

        #region Timers

        private void walkTime_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            m_walkTime.Stop();
            m_scenePresence.Teleport(m_destination);
        }

        private void frames_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Update();
        }

        private void startTime_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            m_startTime.Stop();
            GetNextDestination();
        }

        #endregion

        #region Chat interface

        public void SendChatMessage (int sayType, string message)
        {
            OSChatMessage args = new OSChatMessage ();
            args.Message = message;
            args.Channel = 0;
            args.From = FirstName + " " + LastName;
            args.Position = m_scenePresence.AbsolutePosition;
            args.Sender = this;
            args.Type = (ChatTypeEnum)sayType;
            args.Scene = m_scene;

            OnBotChatFromViewer (this, args);
        }

        #endregion

        #region Move / fly bot base code

        /// <summary>
        /// Does the actual movement of the bot
        /// </summary>
        /// <param name="pos"></param>
        private void walkTo(Vector3 pos)
        {
            Vector3 bot_forward = new Vector3(1, 0, 0);
            Vector3 bot_toward;
            try
            {
                bot_toward = Util.GetNormalizedVector(pos - m_scenePresence.AbsolutePosition);
                Quaternion rot_result = llRotBetween(bot_forward, bot_toward);
                m_bodyDirection = rot_result;
            } catch (System.ArgumentException) {}
            m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_AT_POS;
            

            OnBotAgentUpdate(m_movementFlag, m_bodyDirection);
            m_movementFlag = (uint)AgentManager.ControlFlags.NONE;
        }

        /// <summary>
        /// Does the actual movement of the bot
        /// </summary>
        /// <param name="pos"></param>
        private void flyTo(Vector3 pos)
        {
            Vector3 bot_forward = new Vector3(1, 0, 0);
            try
            {
                Vector3 bot_toward = Util.GetNormalizedVector(pos - m_scenePresence.AbsolutePosition);
                Quaternion rot_result = llRotBetween(bot_forward, bot_toward);
                m_bodyDirection = rot_result;
            }
            catch (System.ArgumentException)
            {
                
            }
            
            m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_FLY;

            Vector3 diffPos = m_destination - m_scenePresence.AbsolutePosition;
            if (Math.Abs(diffPos.X) > 1.5 || Math.Abs(diffPos.Y) > 1.5)
            {
                m_movementFlag |= (uint)AgentManager.ControlFlags.AGENT_CONTROL_AT_POS;
            }

            if (m_scenePresence.AbsolutePosition.Z < pos.Z-1)
            {
                m_movementFlag |= (uint)AgentManager.ControlFlags.AGENT_CONTROL_UP_POS;
            }
            else if (m_scenePresence.AbsolutePosition.Z > pos.Z+1)
            {
                m_movementFlag |= (uint)AgentManager.ControlFlags.AGENT_CONTROL_UP_NEG;
            }

            OnBotAgentUpdate(m_movementFlag, m_bodyDirection);
            m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_FLY;
        }

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

            IsFollowing = false; //Turn off following

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
                IsOnAPath = true;
            }

            lock (WayPoints)
            {
                foreach (string s in points)
                {
                    m_log.Debug (s);
                    string[] Vector = s.Split (',');

                    if (Vector.Length != 3)
                        continue;

                    WayPoints.Add (new Vector3 (float.Parse (Vector[0]),
                        float.Parse (Vector[1]),
                        float.Parse (Vector[2])));
                }
            }
        }

        public void FollowAvatar (string avatarName)
        {
            m_scenePresence.StandUp (); //Can't follow if sitting
            IsFollowing = true;
            FollowName = avatarName;
            m_autoMove = false;
        }

        public void StopFollowAvatar (string avatarName)
        {
            FollowSP = null; //null out everything
            IsFollowing = false;
            FollowName = "";
            m_autoMove = true;
        }

        #endregion

        #region Update and move the bot

        private void GetNextDestination()
        {
            if (m_navMesh != null)
            {
                Node node = m_navMesh.GetNextNode();

                if (node.Mode == TravelMode.Fly)
                    FlyTo(node.Position);
                else if (node.Mode == TravelMode.Walk)
                    WalkTo(node.Position);
            }
        }

        /// <summary>
        /// This is called to make the bot walk nicely around every 100 milliseconds by m_frames timer
        /// </summary>
        private void Update ()
        {
            if (m_scenePresence == null)
                return;

            if (IsFollowing)
            {
                // FOLLOW an avatar - this is looking for an avatar UUID so wont follow a prim here  - yet
                if (FollowSP == null)
                {
                    m_scenePresence.Scene.TryGetAvatarByName (FollowName, out FollowSP);
                    if (FollowSP == null)
                    {
                        //Try by UUID then
                        try
                        {
                            m_scenePresence.Scene.TryGetScenePresence (UUID.Parse (FollowName), out FollowSP);
                        }
                        catch
                        {
                        }
                    }
                }
                //If its still null, the person doesn't exist, cancel the follow and return
                if (FollowSP == null)
                {
                    IsFollowing = false;
                    m_log.Warn ("Could not find avatar " + FollowName);
                }
                else
                {
                    //Only check so many times
                    CurrentFollowTimeBeforeUpdate++;
                    if (CurrentFollowTimeBeforeUpdate == FollowTimeBeforeUpdate)
                    {
                        Vector3 diffAbsPos = FollowSP.AbsolutePosition - m_scenePresence.AbsolutePosition;
                        if (Math.Abs (diffAbsPos.X) > m_closeToPoint || Math.Abs (diffAbsPos.Y) > m_closeToPoint)
                        {
                            NavMesh mesh = new NavMesh ();

                            bool fly = FollowSP.PhysicsActor == null ? ShouldFly : FollowSP.PhysicsActor.Flying;
                            mesh.AddEdge (0, 1, fly ? TravelMode.Fly : TravelMode.Walk);
                            mesh.AddNode (m_scenePresence.AbsolutePosition); //Give it the current pos so that it will know where to start

                            mesh.AddEdge (1, 2, fly ? TravelMode.Fly : TravelMode.Walk);
                            mesh.AddNode (FollowSP.AbsolutePosition); //Give it the new point so that it will head toward it
                            SetPath (mesh, 0, false, 10000, false); //Set and go
                        }
                        else
                        {
                            //Stop the bot then
                            State = RexBotState.Idle;
                            m_walkTime.Stop ();
                            m_startTime.Stop ();
                        }
                        //Reset the time
                        CurrentFollowTimeBeforeUpdate = -1;
                    }
                }
            }
            else if (IsOnAPath)
            {
                lock (WayPoints)
                {
                    if (WayPoints[CurrentWayPoint].ApproxEquals (m_scenePresence.AbsolutePosition, m_closeToPoint)) //Are we about to the new position?
                    {
                        //We need to update the waypoint then and send the av to a new location
                        CurrentWayPoint++;
                        if (WayPoints.Count >= CurrentWayPoint)
                        {
                            //We are at the last point, end the path checking
                            IsOnAPath = false;
                            return;
                        }
                        NavMesh mesh = new NavMesh ();
                        //Build the next mesh to tell the bot where to go
                        mesh.AddEdge (0, 1, ShouldFly ? TravelMode.Fly : TravelMode.Walk);
                        mesh.AddNode (m_scenePresence.AbsolutePosition); //Give it the current pos so that it will know where to start
                        mesh.AddEdge (1, 2, ShouldFly ? TravelMode.Fly : TravelMode.Walk);
                        mesh.AddNode (WayPoints[CurrentWayPoint]); //Give it the new point so that it will head toward it
                        SetPath (mesh, 0, false, 10000, false); //Set and go
                    }
                }
            }

            if (State != RexBotState.Idle)
            {
                bool CheckFly = State == RexBotState.Flying;
                Vector3 diffPos = m_destination - m_scenePresence.AbsolutePosition;
                if (Math.Abs (diffPos.X) < 1 && Math.Abs (diffPos.Y) < m_closeToPoint &&
                    (!CheckFly || (ShouldFly && Math.Abs (diffPos.Z) < m_closeToPoint))) //If we are flying, Z checking matters
                {
                    State = RexBotState.Idle;
                    m_walkTime.Stop ();
                    m_startTime.Stop ();
                    if (m_autoMove)
                        m_startTime.Start ();
                }
                else
                {
                    //Move to the position!
                    switch (State)
                    {
                        case RexBotState.Walking:
                            walkTo (m_destination);
                            break;

                        case RexBotState.Flying:
                            flyTo (m_destination);
                            break;
                    }
                }
            }

            if (State != RexBotState.Flying && State != RexBotState.Walking)
            {
                OnBotAgentUpdate (m_movementFlag, m_bodyDirection);
            }
            m_frameCount++;
        }

        #endregion

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

        protected virtual void RegisterInterfaces()
        {
            RegisterInterface<IRexBot>(this);
            RegisterInterface<IClientAPI>(this);
            RegisterInterface<RexBot>(this);
        }

        protected void RegisterInterface<T>(T iface)
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

        protected virtual void OnBotChatFromViewer (object sender, OSChatMessage e)
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

        #region IClientAPI

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

            if (OnCompleteMovementToRegion != null)
            {
                OnCompleteMovementToRegion (this);
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
            Close ();
        }

        public void Kick (string message)
        {
            Close ();
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

        public void SendAnimations (UUID[] animID, int[] seqs, UUID sourceAgentId, UUID[] objectIDs)
        {
            
        }

        public void SendChatMessage (string message, byte type, Vector3 fromPos, string fromName, UUID fromAgentID, byte source, byte audible)
        {
            
        }

        public void SendInstantMessage (GridInstantMessage im)
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

        public void SendPrimUpdate (IEnumerable<EntityUpdate> updates)
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
