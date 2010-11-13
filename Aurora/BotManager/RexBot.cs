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

namespace Aurora.BotManager
{
    /// <summary>
    /// Created by RealXtend
    /// </summary>
	public interface IRexBot
    {
        void SetPath(NavMesh mesh, int startNode, bool reverse, int timeOut);
        void PauseAutoMove();
        void StopAutoMove();
        void EnableAutoMove();
        void UnpauseAutoMove();
        void SetMovementSpeedMod(float speed);
        void DisableWalk();
        void EnableWalk();
    }

    public class RexBot : GenericNpcCharacter, IRexBot, IClientAPI, IClientCore
    {
        #region Declares
        public enum RexBotState { Idle, Walking, Flying, Unknown }

        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Vector3 DEFAULT_START_POSITION = new Vector3(128, 128, 128);
        private static string DEFAULT_GREETING = "Ready to serve, Master.";

        private static UInt32 UniqueId = 1;

        private string m_firstName = "Default";
        private string m_lastName = "RexBot" + UniqueId.ToString();

        private uint m_movementFlag = 0;
        private Quaternion m_bodyDirection = Quaternion.Identity;
        private short m_frameCount = 0;

        private UUID m_myID = UUID.Random();
        private Scene m_scene;
        private ScenePresence m_scenePresence;

        private RexBotState m_currentState = RexBotState.Idle;
        public RexBotState State
        {
            get { return m_currentState; }
            set { m_previousState = m_currentState;  m_currentState = value; }
        }
        private RexBotState m_previousState = RexBotState.Idle;
        
        private bool m_autoMove = true;
        private Vector3 m_destination;

        private System.Timers.Timer m_frames;
        private System.Timers.Timer m_walkTime;
        private System.Timers.Timer m_startTime;

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

        public override Vector3 StartPos
        {
            get { return DEFAULT_START_POSITION; }
            set { }
        }

        public override UUID AgentId
        {
            get { return m_myID; }
        }

        public override string FirstName
        {
            get { return m_firstName; }
            set { m_firstName = value; }
        }

        public override string LastName
        {
            get { return m_lastName; }
            set { m_lastName = value; }
        }

        private uint m_circuitCode;

        public override uint CircuitCode
        {
            get { return m_circuitCode; }
            set { m_circuitCode = value; }
        }

        public override String Name
        {
            get { return FirstName + " " + LastName; }
        }

        public override IScene Scene
        {
            get { return m_scene; }
        }

        #endregion

        // creates new bot on the default location
        public RexBot(Scene scene)
        {
            RegisterInterfaces(); 

            m_scene = scene;
            m_navMesh = null;
            
            m_scene.EventManager.OnNewClient += eventManager_OnNewClient;
            
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
            List<ScenePresence> avatars = m_scene.ScenePresences;
            foreach (ScenePresence avatar in avatars)
            {
                if (avatar.ControllingClient == this)
                {
                    m_scenePresence = avatar;
                    break;
                }
            }

            m_scenePresence.Teleport(DEFAULT_START_POSITION);
        }

        public override void Close()
        {
            // Pull Client out of Region
            m_log.Info("[RexBot]: Removing bot " + Name);

            OnBotLogout();

            //raiseevent on the packet server to Shutdown the circuit
            OnBotConnectionClosed();

            m_frames.Stop();
            m_walkTime.Stop();

            m_scene.RemoveClient(AgentId);
        }

        #endregion

        #region SetPath

        public void SetPath(NavMesh mesh, int startNode, bool reverse, int timeOut)
        {
            m_navMesh = new NavMeshInstance(mesh, startNode, reverse, timeOut);

            SetDefaultWalktimeInterval();

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
            m_scenePresence.ForceFly = true;
        }

        /// <summary>
        /// Allows for flying and walkin
        /// </summary>
        /// <param name="pos"></param>
        public void EnableWalk()
        {
            m_scenePresence.ForceFly = false;
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

        private void eventManager_OnNewClient(IClientAPI client)
        {
            if (client != this)
                client.OnChatFromClient += client_OnChatFromViewer;
        }

        private void client_OnChatFromViewer(object sender, OSChatMessage e)
        {
            if (e.Message != null && e.Message.Length > 0)
            {
                if (e.Message.StartsWith("!"))
                {
                    string[] param = e.Message.Split(' ');

                    switch (param[0])
                    {
                        case "!continue":
                            break;
                        case "!stop":
                            m_movementFlag = (uint)AgentManager.ControlFlags.NONE;
                            State = RexBotState.Idle;
                            break;
                        case "!go":
                            #region go
                            if (param.Length > 1)
                            {
                                switch (param[1])
                                {
                                    case "left":
                                        m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_TURN_LEFT |
                                                   (uint)AgentManager.ControlFlags.AGENT_CONTROL_LEFT_POS;
                                        break;
                                    case "right":
                                        m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_TURN_RIGHT |
                                                   (uint)AgentManager.ControlFlags.AGENT_CONTROL_LEFT_NEG;
                                        break;
                                    case "forward":
                                        m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_AT_POS;
                                        break;
                                    case "back":
                                        m_movementFlag = (uint)AgentManager.ControlFlags.AGENT_CONTROL_AT_NEG;
                                        break;
                                    default:
                                        string[] xyz = param[1].Split(',');
                                        if (xyz.Length == 3)
                                        {
                                            try
                                            {
                                                Vector3 pos = Vector3.Parse(param[1]);

                                                SetDefaultWalktimeInterval();
                                                m_walkTime.Start();
                                                walkTo(pos);
                                                State = RexBotState.Walking;
                                                m_destination = pos;
                                            }
                                            catch (Exception ex)
                                            {
                                                System.Console.WriteLine(ex.ToString());
                                            }
                                        }
                                        else if (xyz.Length == 4)
                                        {
                                            try
                                            {
                                                Vector3 pos = Vector3.Parse(param[1]);

                                                m_walkTime.Interval = (Convert.ToDouble(xyz[3]) * 1000);
                                                m_walkTime.Start();
                                                walkTo(pos);
                                                State = RexBotState.Walking;
                                                m_destination = pos;
                                            }
                                            catch (Exception ex)
                                            {
                                                System.Console.WriteLine(ex.ToString());
                                            }
                                        }
                                        break;
                                }
                            }
                            break;
                            #endregion
                        case "!fly":
                            if (param.Length >= 2)
                            {
                                string[] loc = param[1].Split(',');
                                if (loc.Length > 2)
                                {
                                    try
                                    {
                                        Vector3 pos = Vector3.Parse(param[1]);
                                        flyTo(pos);
                                        m_destination = pos;
                                        if (loc.Length == 4)
                                        {
                                            m_walkTime.Interval = (Convert.ToDouble(loc[3]) * 1000);
                                        }
                                        else
                                        {
                                            SetDefaultWalktimeInterval();
                                        }
                                        m_walkTime.Start();
                                        State = RexBotState.Flying;
                                    }
                                    catch (Exception E)
                                    {
                                        System.Console.WriteLine(E.ToString());
                                    }
                                }
                            }
                            break;
                        case "!teleport":
                            #region teleport
                            try
                            {
                                Vector3 pos = Vector3.Parse(param[1]);
                                m_scenePresence.AbsolutePosition = pos;
                            }
                            catch (Exception ex)
                            {
                                OSChatMessage args = new OSChatMessage();
                                args.Message = "Invalid message " + ex.Message;
                                args.Channel = 0;
                                args.From = FirstName + " " + LastName;
                                args.Position = new Vector3(128, 128, 26);
                                args.Sender = this;
                                args.Type = ChatTypeEnum.Say;

                                OnBotChatFromViewer(this, args);
                            }
                            #endregion
                            break;
                        default:
                            if (!e.Message.Contains("!teleport"))
                            {
                                OSChatMessage args = new OSChatMessage();
                                args.Message = "Sorry. Don't understand your message " + e.Message;
                                args.Channel = 0;
                                args.From = FirstName + " " + LastName;
                                args.Position = new Vector3(128, 128, 26);
                                args.Sender = this;
                                args.Type = ChatTypeEnum.Shout;

                                OnBotChatFromViewer(this, args);
                            }
                            break;
                    }

                    if (e.Message.StartsWith("!go ") && m_currentState != RexBotState.Walking)
                    {
                        OnBotAgentUpdate(m_movementFlag, m_bodyDirection);
                        m_movementFlag = (uint)AgentManager.ControlFlags.NONE;
                    }
                }
            }
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
        private void Update()
        {
            if(m_scenePresence == null)
                return;
        
            Vector3 diffPos = m_destination - m_scenePresence.AbsolutePosition;
            switch (State)
            {
                case RexBotState.Walking:
                    if (Math.Abs(diffPos.X) < 1 && Math.Abs(diffPos.Y) < 1)
                    {
                        State = RexBotState.Idle;
                        m_walkTime.Stop();
                        //                    GetNextDestination();
                        m_startTime.Stop();
                        if (m_autoMove)
                        {
                            m_startTime.Start();
                        }
                    }
                    else
                    {
                        walkTo(m_destination);
                    }
                    break;

                case RexBotState.Flying:
                    if (Math.Abs(diffPos.X) < 1.5 && Math.Abs(diffPos.Y) < 1.5 && Math.Abs(diffPos.Z) < 1.5)
                    {
                        State = RexBotState.Idle;
                        m_walkTime.Stop();
                        //                    GetNextDestination();
                        m_startTime.Stop();
                        if (m_autoMove)
                        {
                            m_startTime.Start();
                        }
                    }
                    else
                    {
                        flyTo(m_destination);
                    }
                    break;
            }

            if (State != RexBotState.Flying && State != RexBotState.Walking)
            {
                OnBotAgentUpdate(m_movementFlag, m_bodyDirection);
            }

            if (m_frameCount >= 250)
            {
                OSChatMessage args = new OSChatMessage();
                args.Message = DEFAULT_GREETING;
                args.Channel = 0;
                args.From = FirstName + " " + LastName;
                args.Position = new Vector3(128, 128, 26);
                args.Sender = this;
                args.Type = ChatTypeEnum.Shout;
                args.Scene = m_scene;

                OnBotChatFromViewer(this, args);
                m_frameCount = 0;
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

        public void Disconnect()
        {
        }

        public void Disconnect(string reason)
        {
        }

        #endregion

        #region IClientAPI Members

        Vector3 IClientAPI.StartPos
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        UUID IClientAPI.AgentId
        {
            get { throw new NotImplementedException(); }
        }

        UUID IClientAPI.SessionId
        {
            get { throw new NotImplementedException(); }
        }

        UUID IClientAPI.SecureSessionId
        {
            get { throw new NotImplementedException(); }
        }

        UUID IClientAPI.ActiveGroupId
        {
            get { throw new NotImplementedException(); }
        }

        string IClientAPI.ActiveGroupName
        {
            get { throw new NotImplementedException(); }
        }

        ulong IClientAPI.ActiveGroupPowers
        {
            get { throw new NotImplementedException(); }
        }

        ulong IClientAPI.GetGroupPowers(UUID groupID)
        {
            throw new NotImplementedException();
        }

        bool IClientAPI.IsGroupMember(UUID GroupID)
        {
            throw new NotImplementedException();
        }

        string IClientAPI.FirstName
        {
            get { throw new NotImplementedException(); }
        }

        string IClientAPI.LastName
        {
            get { throw new NotImplementedException(); }
        }

        IScene IClientAPI.Scene
        {
            get { throw new NotImplementedException(); }
        }

        int IClientAPI.NextAnimationSequenceNumber
        {
            get { throw new NotImplementedException(); }
        }

        string IClientAPI.Name
        {
            get { throw new NotImplementedException(); }
        }

        bool IClientAPI.IsActive
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool IClientAPI.IsLoggingOut
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool IClientAPI.SendLogoutPacketWhenClosing
        {
            set { throw new NotImplementedException(); }
        }

        uint IClientAPI.CircuitCode
        {
            get { throw new NotImplementedException(); }
        }

        IPEndPoint IClientAPI.RemoteEndPoint
        {
            get { throw new NotImplementedException(); }
        }

        event GenericMessage IClientAPI.OnGenericMessage
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ImprovedInstantMessage IClientAPI.OnInstantMessage
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ChatMessage IClientAPI.OnChatFromClient
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event TextureRequest IClientAPI.OnRequestTexture
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RezObject IClientAPI.OnRezObject
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ModifyTerrain IClientAPI.OnModifyTerrain
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event BakeTerrain IClientAPI.OnBakeTerrain
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EstateChangeInfo IClientAPI.OnEstateChangeInfo
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SetAppearance IClientAPI.OnSetAppearance
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event AvatarNowWearing IClientAPI.OnAvatarNowWearing
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RezSingleAttachmentFromInv IClientAPI.OnRezSingleAttachmentFromInv
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RezMultipleAttachmentsFromInv IClientAPI.OnRezMultipleAttachmentsFromInv
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UUIDNameRequest IClientAPI.OnDetachAttachmentIntoInv
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectAttach IClientAPI.OnObjectAttach
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectDeselect IClientAPI.OnObjectDetach
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectDrop IClientAPI.OnObjectDrop
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event StartAnim IClientAPI.OnStartAnim
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event StopAnim IClientAPI.OnStopAnim
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event LinkObjects IClientAPI.OnLinkObjects
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event DelinkObjects IClientAPI.OnDelinkObjects
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RequestMapBlocks IClientAPI.OnRequestMapBlocks
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RequestMapName IClientAPI.OnMapNameRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event TeleportLocationRequest IClientAPI.OnTeleportLocationRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RequestAvatarProperties IClientAPI.OnRequestAvatarProperties
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SetAlwaysRun IClientAPI.OnSetAlwaysRun
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event TeleportLandmarkRequest IClientAPI.OnTeleportLandmarkRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event DeRezObject IClientAPI.OnDeRezObject
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event Action<IClientAPI> IClientAPI.OnRegionHandShakeReply
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GenericCall2 IClientAPI.OnRequestWearables
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GenericCall1 IClientAPI.OnCompleteMovementToRegion
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateAgent IClientAPI.OnAgentUpdate
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event AgentRequestSit IClientAPI.OnAgentRequestSit
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event AgentSit IClientAPI.OnAgentSit
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event AvatarPickerRequest IClientAPI.OnAvatarPickerRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event Action<IClientAPI> IClientAPI.OnRequestAvatarsData
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event AddNewPrim IClientAPI.OnAddPrim
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event FetchInventory IClientAPI.OnAgentDataUpdateRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event TeleportLocationRequest IClientAPI.OnSetStartLocationRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RequestGodlikePowers IClientAPI.OnRequestGodlikePowers
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GodKickUser IClientAPI.OnGodKickUser
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectDuplicate IClientAPI.OnObjectDuplicate
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectDuplicateOnRay IClientAPI.OnObjectDuplicateOnRay
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GrabObject IClientAPI.OnGrabObject
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event DeGrabObject IClientAPI.OnDeGrabObject
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event MoveObject IClientAPI.OnGrabUpdate
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SpinStart IClientAPI.OnSpinStart
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SpinObject IClientAPI.OnSpinUpdate
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SpinStop IClientAPI.OnSpinStop
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateShape IClientAPI.OnUpdatePrimShape
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectExtraParams IClientAPI.OnUpdateExtraParams
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectRequest IClientAPI.OnObjectRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectSelect IClientAPI.OnObjectSelect
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectDeselect IClientAPI.OnObjectDeselect
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GenericCall7 IClientAPI.OnObjectDescription
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GenericCall7 IClientAPI.OnObjectName
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GenericCall7 IClientAPI.OnObjectClickAction
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GenericCall7 IClientAPI.OnObjectMaterial
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RequestObjectPropertiesFamily IClientAPI.OnRequestObjectPropertiesFamily
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdatePrimFlags IClientAPI.OnUpdatePrimFlags
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdatePrimTexture IClientAPI.OnUpdatePrimTexture
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateVectorWithUpdate IClientAPI.OnUpdatePrimGroupPosition
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateVectorWithUpdate IClientAPI.OnUpdatePrimSinglePosition
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdatePrimRotation IClientAPI.OnUpdatePrimGroupRotation
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdatePrimSingleRotation IClientAPI.OnUpdatePrimSingleRotation
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdatePrimSingleRotationPosition IClientAPI.OnUpdatePrimSingleRotationPosition
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdatePrimGroupRotation IClientAPI.OnUpdatePrimGroupMouseRotation
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateVector IClientAPI.OnUpdatePrimScale
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateVector IClientAPI.OnUpdatePrimGroupScale
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event StatusChange IClientAPI.OnChildAgentStatus
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectPermissions IClientAPI.OnObjectPermissions
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event CreateNewInventoryItem IClientAPI.OnCreateNewInventoryItem
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event LinkInventoryItem IClientAPI.OnLinkInventoryItem
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event CreateInventoryFolder IClientAPI.OnCreateNewInventoryFolder
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateInventoryFolder IClientAPI.OnUpdateInventoryFolder
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event MoveInventoryFolder IClientAPI.OnMoveInventoryFolder
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event FetchInventoryDescendents IClientAPI.OnFetchInventoryDescendents
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event PurgeInventoryDescendents IClientAPI.OnPurgeInventoryDescendents
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event FetchInventory IClientAPI.OnFetchInventory
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RequestTaskInventory IClientAPI.OnRequestTaskInventory
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateInventoryItem IClientAPI.OnUpdateInventoryItem
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event CopyInventoryItem IClientAPI.OnCopyInventoryItem
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event MoveInventoryItem IClientAPI.OnMoveInventoryItem
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RemoveInventoryFolder IClientAPI.OnRemoveInventoryFolder
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RemoveInventoryItem IClientAPI.OnRemoveInventoryItem
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UDPAssetUploadRequest IClientAPI.OnAssetUploadRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event XferReceive IClientAPI.OnXferReceive
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RequestXfer IClientAPI.OnRequestXfer
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ConfirmXfer IClientAPI.OnConfirmXfer
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event AbortXfer IClientAPI.OnAbortXfer
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RezScript IClientAPI.OnRezScript
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateTaskInventory IClientAPI.OnUpdateTaskInventory
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event MoveTaskInventory IClientAPI.OnMoveTaskItem
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RemoveTaskInventory IClientAPI.OnRemoveTaskItem
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UUIDNameRequest IClientAPI.OnNameFromUUIDRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelAccessListRequest IClientAPI.OnParcelAccessListRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelAccessListUpdateRequest IClientAPI.OnParcelAccessListUpdateRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelPropertiesRequest IClientAPI.OnParcelPropertiesRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelDivideRequest IClientAPI.OnParcelDivideRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelJoinRequest IClientAPI.OnParcelJoinRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelPropertiesUpdateRequest IClientAPI.OnParcelPropertiesUpdateRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelSelectObjects IClientAPI.OnParcelSelectObjects
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelObjectOwnerRequest IClientAPI.OnParcelObjectOwnerRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelAbandonRequest IClientAPI.OnParcelAbandonRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelGodForceOwner IClientAPI.OnParcelGodForceOwner
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelReclaim IClientAPI.OnParcelReclaim
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelReturnObjectsRequest IClientAPI.OnParcelReturnObjectsRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelDeedToGroup IClientAPI.OnParcelDeedToGroup
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RegionInfoRequest IClientAPI.OnRegionInfoRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EstateCovenantRequest IClientAPI.OnEstateCovenantRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event FriendActionDelegate IClientAPI.OnApproveFriendRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event FriendActionDelegate IClientAPI.OnDenyFriendRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event FriendshipTermination IClientAPI.OnTerminateFriendship
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event MoneyTransferRequest IClientAPI.OnMoneyTransferRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EconomyDataRequest IClientAPI.OnEconomyDataRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event MoneyBalanceRequest IClientAPI.OnMoneyBalanceRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateAvatarProperties IClientAPI.OnUpdateAvatarProperties
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelBuy IClientAPI.OnParcelBuy
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RequestPayPrice IClientAPI.OnRequestPayPrice
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectSaleInfo IClientAPI.OnObjectSaleInfo
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectBuy IClientAPI.OnObjectBuy
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event BuyObjectInventory IClientAPI.OnBuyObjectInventory
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RequestTerrain IClientAPI.OnRequestTerrain
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RequestTerrain IClientAPI.OnUploadTerrain
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectIncludeInSearch IClientAPI.OnObjectIncludeInSearch
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UUIDNameRequest IClientAPI.OnTeleportHomeRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ScriptAnswer IClientAPI.OnScriptAnswer
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event AgentSit IClientAPI.OnUndo
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event AgentSit IClientAPI.OnRedo
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event LandUndo IClientAPI.OnLandUndo
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ForceReleaseControls IClientAPI.OnForceReleaseControls
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GodLandStatRequest IClientAPI.OnLandStatRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event DetailedEstateDataRequest IClientAPI.OnDetailedEstateDataRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SetEstateFlagsRequest IClientAPI.OnSetEstateFlagsRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SetEstateTerrainBaseTexture IClientAPI.OnSetEstateTerrainBaseTexture
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SetEstateTerrainDetailTexture IClientAPI.OnSetEstateTerrainDetailTexture
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SetEstateTerrainTextureHeights IClientAPI.OnSetEstateTerrainTextureHeights
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event CommitEstateTerrainTextureRequest IClientAPI.OnCommitEstateTerrainTextureRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SetRegionTerrainSettings IClientAPI.OnSetRegionTerrainSettings
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EstateRestartSimRequest IClientAPI.OnEstateRestartSimRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EstateChangeCovenantRequest IClientAPI.OnEstateChangeCovenantRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateEstateAccessDeltaRequest IClientAPI.OnUpdateEstateAccessDeltaRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SimulatorBlueBoxMessageRequest IClientAPI.OnSimulatorBlueBoxMessageRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EstateBlueBoxMessageRequest IClientAPI.OnEstateBlueBoxMessageRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EstateDebugRegionRequest IClientAPI.OnEstateDebugRegionRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EstateTeleportOneUserHomeRequest IClientAPI.OnEstateTeleportOneUserHomeRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EstateTeleportAllUsersHomeRequest IClientAPI.OnEstateTeleportAllUsersHomeRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UUIDNameRequest IClientAPI.OnUUIDGroupNameRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RegionHandleRequest IClientAPI.OnRegionHandleRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelInfoRequest IClientAPI.OnParcelInfoRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RequestObjectPropertiesFamily IClientAPI.OnObjectGroupRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ScriptReset IClientAPI.OnScriptReset
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GetScriptRunning IClientAPI.OnGetScriptRunning
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SetScriptRunning IClientAPI.OnSetScriptRunning
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateVector IClientAPI.OnAutoPilotGo
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event TerrainUnacked IClientAPI.OnUnackedTerrain
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ActivateGesture IClientAPI.OnActivateGesture
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event DeactivateGesture IClientAPI.OnDeactivateGesture
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ObjectOwner IClientAPI.OnObjectOwner
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event DirPlacesQuery IClientAPI.OnDirPlacesQuery
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event DirFindQuery IClientAPI.OnDirFindQuery
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event DirLandQuery IClientAPI.OnDirLandQuery
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event DirPopularQuery IClientAPI.OnDirPopularQuery
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event DirClassifiedQuery IClientAPI.OnDirClassifiedQuery
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EventInfoRequest IClientAPI.OnEventInfoRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelSetOtherCleanTime IClientAPI.OnParcelSetOtherCleanTime
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event MapItemRequest IClientAPI.OnMapItemRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event OfferCallingCard IClientAPI.OnOfferCallingCard
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event AcceptCallingCard IClientAPI.OnAcceptCallingCard
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event DeclineCallingCard IClientAPI.OnDeclineCallingCard
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SoundTrigger IClientAPI.OnSoundTrigger
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event StartLure IClientAPI.OnStartLure
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event TeleportLureRequest IClientAPI.OnTeleportLureRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event NetworkStats IClientAPI.OnNetworkStatsUpdate
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ClassifiedInfoRequest IClientAPI.OnClassifiedInfoRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ClassifiedInfoUpdate IClientAPI.OnClassifiedInfoUpdate
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ClassifiedDelete IClientAPI.OnClassifiedDelete
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ClassifiedDelete IClientAPI.OnClassifiedGodDelete
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EventNotificationAddRequest IClientAPI.OnEventNotificationAddRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EventNotificationRemoveRequest IClientAPI.OnEventNotificationRemoveRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EventGodDelete IClientAPI.OnEventGodDelete
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelDwellRequest IClientAPI.OnParcelDwellRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UserInfoRequest IClientAPI.OnUserInfoRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event UpdateUserInfo IClientAPI.OnUpdateUserInfo
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event RetrieveInstantMessages IClientAPI.OnRetrieveInstantMessages
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event PickDelete IClientAPI.OnPickDelete
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event PickGodDelete IClientAPI.OnPickGodDelete
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event PickInfoUpdate IClientAPI.OnPickInfoUpdate
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event AvatarNotesUpdate IClientAPI.OnAvatarNotesUpdate
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event AvatarInterestUpdate IClientAPI.OnAvatarInterestUpdate
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GrantUserFriendRights IClientAPI.OnGrantUserRights
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event MuteListRequest IClientAPI.OnMuteListRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event PlacesQuery IClientAPI.OnPlacesQuery
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event FindAgentUpdate IClientAPI.OnFindAgent
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event TrackAgentUpdate IClientAPI.OnTrackAgent
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event NewUserReport IClientAPI.OnUserReport
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SaveStateHandler IClientAPI.OnSaveState
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GroupAccountSummaryRequest IClientAPI.OnGroupAccountSummaryRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GroupAccountDetailsRequest IClientAPI.OnGroupAccountDetailsRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GroupAccountTransactionsRequest IClientAPI.OnGroupAccountTransactionsRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event FreezeUserUpdate IClientAPI.OnParcelFreezeUser
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EjectUserUpdate IClientAPI.OnParcelEjectUser
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelBuyPass IClientAPI.OnParcelBuyPass
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ParcelGodMark IClientAPI.OnParcelGodMark
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GroupActiveProposalsRequest IClientAPI.OnGroupActiveProposalsRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GroupVoteHistoryRequest IClientAPI.OnGroupVoteHistoryRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SimWideDeletesDelegate IClientAPI.OnSimWideDeletes
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event SendPostcard IClientAPI.OnSendPostcard
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event MuteListEntryUpdate IClientAPI.OnUpdateMuteListEntry
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event MuteListEntryRemove IClientAPI.OnRemoveMuteListEntry
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GodlikeMessage IClientAPI.OnGodlikeMessage
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GodUpdateRegionInfoUpdate IClientAPI.OnGodUpdateRegionInfoUpdate
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ChangeInventoryItemFlags IClientAPI.OnChangeInventoryItemFlags
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event TeleportCancel IClientAPI.OnTeleportCancel
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event GodlikeMessage IClientAPI.OnEstateTelehubRequest
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ViewerStartAuction IClientAPI.OnViewerStartAuction
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        void IClientAPI.SetDebugPacketLevel(int newDebug)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.InPacket(object NewPack)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.ProcessInPacket(OpenMetaverse.Packets.Packet NewPack)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.Close()
        {
            throw new NotImplementedException();
        }

        void IClientAPI.Kick(string message)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.Start()
        {
            throw new NotImplementedException();
        }

        void IClientAPI.Stop()
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendWearables(AvatarWearable[] wearables, int serial)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAppearance(UUID agentID, byte[] visualParams, byte[] textureEntry)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendStartPingCheck(byte seq)
        {
        }

        void IClientAPI.SendKillObject(ulong regionHandle, ISceneEntity[] localIDs)
        {
        }

        void IClientAPI.SendAnimations(UUID[] animID, int[] seqs, UUID sourceAgentId, UUID[] objectIDs)
        {
        }

        void IClientAPI.SendRegionHandshake(RegionInfo regionInfo, RegionHandshakeArgs args)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendChatMessage(string message, byte type, Vector3 fromPos, string fromName, UUID fromAgentID, byte source, byte audible)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendInstantMessage(GridInstantMessage im)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendGenericMessage(string method, List<string> message)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendGenericMessage(string method, List<byte[]> message)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendLayerData(float[] map)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendLayerData(int px, int py, float[] map)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendLayerPacket(float[] map, int x, int y)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendWindData(Vector2[] windSpeeds)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendCloudData(float[] cloudCover)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.MoveAgentIntoRegion(RegionInfo regInfo, Vector3 pos, Vector3 look)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.InformClientOfNeighbour(ulong neighbourHandle, IPEndPoint neighbourExternalEndPoint)
        {
            throw new NotImplementedException();
        }

        AgentCircuitData IClientAPI.RequestClientInfo()
        {
            throw new NotImplementedException();
        }

        void IClientAPI.CrossRegion(ulong newRegionHandle, Vector3 pos, Vector3 lookAt, IPEndPoint newRegionExternalEndPoint, string capsURL)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendMapBlock(List<MapBlockData> mapBlocks, uint flag)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendLocalTeleport(Vector3 position, Vector3 lookAt, uint flags)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendRegionTeleport(ulong regionHandle, byte simAccess, IPEndPoint regionExternalEndPoint, uint locationID, uint flags, string capsURL)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendTeleportFailed(string reason)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendMoneyBalance(UUID transaction, bool success, byte[] description, int balance)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendPayPrice(UUID objectID, int[] payPrice)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendCoarseLocationUpdate(List<UUID> users, List<Vector3> CoarseLocations)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SetChildAgentThrottle(byte[] throttle)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAvatarDataImmediate(ISceneEntity avatar)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendPrimUpdate(ISceneEntity entity, PrimUpdateFlags updateFlags)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.ReprioritizeUpdates()
        {
            throw new NotImplementedException();
        }

        void IClientAPI.FlushPrimUpdates()
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendInventoryFolderDetails(UUID ownerID, UUID folderID, List<InventoryItemBase> items, List<InventoryFolderBase> folders, int version, bool fetchFolders, bool fetchItems)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendInventoryItemDetails(UUID ownerID, InventoryItemBase item)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendInventoryItemCreateUpdate(InventoryItemBase Item, uint callbackId)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendRemoveInventoryItem(UUID itemID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendTakeControls(int controls, bool passToAgent, bool TakeControls)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendTaskInventory(UUID taskID, short serial, byte[] fileName)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendBulkUpdateInventory(InventoryNodeBase node)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendXferPacket(ulong xferID, uint packet, byte[] data)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendEconomyData(float EnergyEfficiency, int ObjectCapacity, int ObjectCount, int PriceEnergyUnit, int PriceGroupCreate, int PriceObjectClaim, float PriceObjectRent, float PriceObjectScaleFactor, int PriceParcelClaim, float PriceParcelClaimFactor, int PriceParcelRent, int PricePublicObjectDecay, int PricePublicObjectDelete, int PriceRentLight, int PriceUpload, int TeleportMinPrice, float TeleportPriceExponent)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAvatarPickerReply(AvatarPickerReplyAgentDataArgs AgentData, List<AvatarPickerReplyDataArgs> Data)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAgentDataUpdate(UUID agentid, UUID activegroupid, string firstname, string lastname, ulong grouppowers, string groupname, string grouptitle)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendPreLoadSound(UUID objectID, UUID ownerID, UUID soundID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendPlayAttachedSound(UUID soundID, UUID objectID, UUID ownerID, float gain, byte flags)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendTriggeredSound(UUID soundID, UUID ownerID, UUID objectID, UUID parentID, ulong handle, Vector3 position, float gain)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAttachedSoundGainChange(UUID objectID, float gain)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendNameReply(UUID profileId, string firstname, string lastname)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAlertMessage(string message)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAgentAlertMessage(string message, bool modal)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendLoadURL(string objectname, UUID objectID, UUID ownerID, bool groupOwned, string message, string url)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendDialog(string objectname, UUID objectID, string ownerFirstName, string ownerLastName, string msg, UUID textureID, int ch, string[] buttonlabels)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendSunPos(Vector3 sunPos, Vector3 sunVel, ulong CurrentTime, uint SecondsPerSunCycle, uint SecondsPerYear, float OrbitalPosition)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendViewerEffect(OpenMetaverse.Packets.ViewerEffectPacket.EffectBlock[] effectBlocks)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendViewerTime(int phase)
        {
            throw new NotImplementedException();
        }

        UUID IClientAPI.GetDefaultAnimation(string name)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAvatarProperties(UUID avatarID, string aboutText, string bornOn, byte[] charterMember, string flAbout, uint flags, UUID flImageID, UUID imageID, string profileURL, UUID partnerID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendScriptQuestion(UUID taskID, string taskName, string ownerName, UUID itemID, int question)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendHealth(float health)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendEstateList(UUID invoice, int code, UUID[] Data, uint estateID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendBannedUserList(UUID invoice, EstateBan[] banlist, uint estateID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendRegionInfoToEstateMenu(RegionInfoForEstateMenuArgs args)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendEstateCovenantInformation(UUID covenant, int covenantLastUpdated)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendDetailedEstateData(UUID invoice, string estateName, uint estateID, uint parentEstate, uint estateFlags, uint sunPosition, UUID covenant, int covenantLastUpdated, string abuseEmail, UUID estateOwner)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendLandProperties(int sequence_id, bool snap_selection, int request_result, LandData landData, float simObjectBonusFactor, int parcelObjectCapacity, int simObjectCapacity, uint regionFlags)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendLandAccessListData(List<UUID> avatars, uint accessFlag, int localLandID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendForceClientSelectObjects(List<uint> objectIDs)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendCameraConstraint(Vector4 ConstraintPlane)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendLandObjectOwners(LandData land, List<UUID> groups, Dictionary<UUID, int> ownersAndCount)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendLandParcelOverlay(byte[] data, int sequence_id)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendParcelMediaCommand(uint flags, ParcelMediaCommandEnum command, float time)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendParcelMediaUpdate(string mediaUrl, UUID mediaTextureID, byte autoScale, string mediaType, string mediaDesc, int mediaWidth, int mediaHeight, byte mediaLoop)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAssetUploadCompleteMessage(sbyte AssetType, bool Success, UUID AssetFullID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendConfirmXfer(ulong xferID, uint PacketID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendXferRequest(ulong XferID, short AssetType, UUID vFileID, byte FilePath, byte[] FileName)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendInitiateDownload(string simFileName, string clientFileName)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendImageFirstPart(ushort numParts, UUID ImageUUID, uint ImageSize, byte[] ImageData, byte imageCodec)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendImageNextPart(ushort partNumber, UUID imageUuid, byte[] imageData)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendImageNotFound(UUID imageid)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendShutdownConnectionNotice()
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendSimStats(SimStats stats)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendObjectPropertiesFamilyData(uint RequestFlags, UUID ObjectUUID, UUID OwnerID, UUID GroupID, uint BaseMask, uint OwnerMask, uint GroupMask, uint EveryoneMask, uint NextOwnerMask, int OwnershipCost, byte SaleType, int SalePrice, uint Category, UUID LastOwnerID, string ObjectName, string Description)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendObjectPropertiesReply(List<ISceneEntity> part)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAgentOffline(UUID[] agentIDs)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAgentOnline(UUID[] agentIDs)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendSitResponse(UUID TargetID, Vector3 OffsetPos, Quaternion SitOrientation, bool autopilot, Vector3 CameraAtOffset, Vector3 CameraEyeOffset, bool ForceMouseLook)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAdminResponse(UUID Token, uint AdminLevel)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendGroupMembership(GroupMembershipData[] GroupMembership)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendGroupNameReply(UUID groupLLUID, string GroupName)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendJoinGroupReply(UUID groupID, bool success)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendEjectGroupMemberReply(UUID agentID, UUID groupID, bool success)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendLeaveGroupReply(UUID groupID, bool success)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendCreateGroupReply(UUID groupID, bool success, string message)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendLandStatReply(uint reportType, uint requestFlags, uint resultCount, LandStatReportItem[] lsrpia)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendScriptRunningReply(UUID objectID, UUID itemID, bool running)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAsset(AssetRequestToClient req)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendTexture(AssetBase TextureAsset)
        {
            throw new NotImplementedException();
        }

        byte[] IClientAPI.GetThrottlesPacked(float multiplier)
        {
            throw new NotImplementedException();
        }

        event ViewerEffectEventHandler IClientAPI.OnViewerEffect
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event Action<IClientAPI> IClientAPI.OnLogout
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event Action<IClientAPI> IClientAPI.OnConnectionClosed
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        void IClientAPI.SendBlueBoxMessage(UUID FromAvatarID, string FromAvatarName, string Message)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendLogoutPacket()
        {
            throw new NotImplementedException();
        }

        EndPoint IClientAPI.GetClientEP()
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendSetFollowCamProperties(UUID objectID, SortedDictionary<int, float> parameters)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendClearFollowCamProperties(UUID objectID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendRegionHandle(UUID regoinID, ulong handle)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendParcelInfo(LandData land, UUID parcelID, uint x, uint y, string SimName)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendScriptTeleportRequest(string objName, string simName, Vector3 pos, Vector3 lookAt)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendDirPlacesReply(UUID queryID, DirPlacesReplyData[] data)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendDirPeopleReply(UUID queryID, DirPeopleReplyData[] data)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendDirEventsReply(UUID queryID, DirEventsReplyData[] data)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendDirGroupsReply(UUID queryID, DirGroupsReplyData[] data)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendDirClassifiedReply(UUID queryID, DirClassifiedReplyData[] data)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendDirLandReply(UUID queryID, DirLandReplyData[] data)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendDirPopularReply(UUID queryID, DirPopularReplyData[] data)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendEventInfoReply(EventData info)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendMapItemReply(mapItemReply[] replies, uint mapitemtype, uint flags)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAvatarGroupsReply(UUID avatarID, GroupMembershipData[] data)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendOfferCallingCard(UUID srcID, UUID transactionID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAcceptCallingCard(UUID transactionID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendDeclineCallingCard(UUID transactionID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendTerminateFriend(UUID exFriendID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAvatarClassifiedReply(UUID targetID, UUID[] classifiedID, string[] name)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendClassifiedInfoReply(UUID classifiedID, UUID creatorID, uint creationDate, uint expirationDate, uint category, string name, string description, UUID parcelID, uint parentEstate, UUID snapshotID, string simName, Vector3 globalPos, string parcelName, byte classifiedFlags, int price)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAgentDropGroup(UUID groupID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.RefreshGroupMembership()
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAvatarNotesReply(UUID targetID, string text)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAvatarPicksReply(UUID targetID, Dictionary<UUID, string> picks)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendPickInfoReply(UUID pickID, UUID creatorID, bool topPick, UUID parcelID, string name, string desc, UUID snapshotID, string user, string originalName, string simName, Vector3 posGlobal, int sortOrder, bool enabled)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAvatarClassifiedReply(UUID targetID, Dictionary<UUID, string> classifieds)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendParcelDwellReply(int localID, UUID parcelID, float dwell)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendUserInfoReply(bool imViaEmail, bool visible, string email)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendUseCachedMuteList()
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendMuteListUpdate(string filename)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendGroupActiveProposals(UUID groupID, UUID transactionID, GroupActiveProposals[] Proposals)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendGroupVoteHistory(UUID groupID, UUID transactionID, GroupVoteHistory Vote, GroupVoteHistoryItem[] Items)
        {
            throw new NotImplementedException();
        }

        bool IClientAPI.AddGenericPacketHandler(string MethodName, GenericMessage handler)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendRebakeAvatarTextures(UUID textureID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendAvatarInterestsReply(UUID avatarID, uint wantMask, string wantText, uint skillsMask, string skillsText, string languages)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendGroupAccountingDetails(IClientAPI sender, UUID groupID, UUID transactionID, UUID sessionID, int amt)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendGroupAccountingSummary(IClientAPI sender, UUID groupID, uint moneyAmt, int totalTier, int usedTier)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendGroupTransactionsSummaryDetails(IClientAPI sender, UUID groupID, UUID transactionID, UUID sessionID, int amt)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendChangeUserRights(UUID agentID, UUID friendID, int rights)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendTextBoxRequest(string message, int chatChannel, string objectname, string ownerFirstName, string ownerLastName, UUID objectId)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendPlacesQuery(ExtendedLandData[] LandData, UUID queryID, UUID transactionID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.FireUpdateParcel(LandUpdateArgs args, int LocalID)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.SendTelehubInfo(Vector3 TelehubPos, Quaternion TelehubRot, List<Vector3> SpawnPoint, UUID ObjectID, string Name)
        {
            throw new NotImplementedException();
        }

        void IClientAPI.StopFlying(ISceneEntity presence)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
