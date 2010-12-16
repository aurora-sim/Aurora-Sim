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
                return Vector3.Zero;
            }
            set
            {
                
            }
        }

        UUID IClientAPI.AgentId
        {
            get { return UUID.Zero; }
        }

        UUID IClientAPI.SessionId
        {
            get { return UUID.Zero; }
        }

        UUID IClientAPI.SecureSessionId
        {
            get { return UUID.Zero; }
        }

        UUID IClientAPI.ActiveGroupId
        {
            get { return UUID.Zero; }
        }

        string IClientAPI.ActiveGroupName
        {
            get { return ""; }
        }

        ulong IClientAPI.ActiveGroupPowers
        {
            get { return 0; }
        }

        string IClientAPI.FirstName
        {
            get { return ""; }
        }

        string IClientAPI.LastName
        {
            get { return ""; }
        }

        IScene IClientAPI.Scene
        {
            get { return null; }
        }

        int IClientAPI.NextAnimationSequenceNumber
        {
            get { return 0; }
        }

        string IClientAPI.Name
        {
            get { return ""; }
        }

        bool IClientAPI.IsActive
        {
            get
            {
                return true;
            }
            set
            {
                
            }
        }

        bool IClientAPI.IsLoggingOut
        {
            get
            {
                return false;
            }
            set
            {
                
            }
        }

        bool IClientAPI.SendLogoutPacketWhenClosing
        {
            set {  }
        }

        uint IClientAPI.CircuitCode
        {
            get { return 0; }
        }

        IPEndPoint IClientAPI.RemoteEndPoint
        {
            get { return null; }
        }

        event GenericMessage IClientAPI.OnGenericMessage
        {
            add {  }
            remove {  }
        }

        event ImprovedInstantMessage IClientAPI.OnInstantMessage
        {
            add {  }
            remove {  }
        }

        event ChatMessage IClientAPI.OnChatFromClient
        {
            add {  }
            remove {  }
        }

        event TextureRequest IClientAPI.OnRequestTexture
        {
            add {  }
            remove {  }
        }

        event RezObject IClientAPI.OnRezObject
        {
            add {  }
            remove {  }
        }

        event ModifyTerrain IClientAPI.OnModifyTerrain
        {
            add {  }
            remove {  }
        }

        event BakeTerrain IClientAPI.OnBakeTerrain
        {
            add {  }
            remove {  }
        }

        event EstateChangeInfo IClientAPI.OnEstateChangeInfo
        {
            add {  }
            remove {  }
        }

        event SetAppearance IClientAPI.OnSetAppearance
        {
            add {  }
            remove {  }
        }

        event AvatarNowWearing IClientAPI.OnAvatarNowWearing
        {
            add {  }
            remove {  }
        }

        event RezSingleAttachmentFromInv IClientAPI.OnRezSingleAttachmentFromInv
        {
            add {  }
            remove {  }
        }

        event RezMultipleAttachmentsFromInv IClientAPI.OnRezMultipleAttachmentsFromInv
        {
            add {  }
            remove {  }
        }

        event UUIDNameRequest IClientAPI.OnDetachAttachmentIntoInv
        {
            add {  }
            remove {  }
        }

        event ObjectAttach IClientAPI.OnObjectAttach
        {
            add {  }
            remove {  }
        }

        event ObjectDeselect IClientAPI.OnObjectDetach
        {
            add {  }
            remove {  }
        }

        event ObjectDrop IClientAPI.OnObjectDrop
        {
            add {  }
            remove {  }
        }

        event StartAnim IClientAPI.OnStartAnim
        {
            add {  }
            remove {  }
        }

        event StopAnim IClientAPI.OnStopAnim
        {
            add {  }
            remove {  }
        }

        event LinkObjects IClientAPI.OnLinkObjects
        {
            add {  }
            remove {  }
        }

        event DelinkObjects IClientAPI.OnDelinkObjects
        {
            add {  }
            remove {  }
        }

        event RequestMapBlocks IClientAPI.OnRequestMapBlocks
        {
            add {  }
            remove {  }
        }

        event RequestMapName IClientAPI.OnMapNameRequest
        {
            add {  }
            remove {  }
        }

        event TeleportLocationRequest IClientAPI.OnTeleportLocationRequest
        {
            add {  }
            remove {  }
        }

        event RequestAvatarProperties IClientAPI.OnRequestAvatarProperties
        {
            add {  }
            remove {  }
        }

        event SetAlwaysRun IClientAPI.OnSetAlwaysRun
        {
            add {  }
            remove {  }
        }

        event TeleportLandmarkRequest IClientAPI.OnTeleportLandmarkRequest
        {
            add {  }
            remove {  }
        }

        event DeRezObject IClientAPI.OnDeRezObject
        {
            add {  }
            remove {  }
        }

        event Action<IClientAPI> IClientAPI.OnRegionHandShakeReply
        {
            add {  }
            remove {  }
        }

        event GenericCall1 IClientAPI.OnRequestWearables
        {
            add {  }
            remove {  }
        }

        event GenericCall1 IClientAPI.OnCompleteMovementToRegion
        {
            add {  }
            remove {  }
        }

        event UpdateAgent IClientAPI.OnAgentUpdate
        {
            add {  }
            remove {  }
        }

        event AgentRequestSit IClientAPI.OnAgentRequestSit
        {
            add {  }
            remove {  }
        }

        event AgentSit IClientAPI.OnAgentSit
        {
            add {  }
            remove {  }
        }

        event AvatarPickerRequest IClientAPI.OnAvatarPickerRequest
        {
            add {  }
            remove {  }
        }

        event Action<IClientAPI> IClientAPI.OnRequestAvatarsData
        {
            add {  }
            remove {  }
        }

        event AddNewPrim IClientAPI.OnAddPrim
        {
            add {  }
            remove {  }
        }

        event FetchInventory IClientAPI.OnAgentDataUpdateRequest
        {
            add {  }
            remove {  }
        }

        event TeleportLocationRequest IClientAPI.OnSetStartLocationRequest
        {
            add {  }
            remove {  }
        }

        event RequestGodlikePowers IClientAPI.OnRequestGodlikePowers
        {
            add {  }
            remove {  }
        }

        event GodKickUser IClientAPI.OnGodKickUser
        {
            add {  }
            remove {  }
        }

        event ObjectDuplicate IClientAPI.OnObjectDuplicate
        {
            add {  }
            remove {  }
        }

        event ObjectDuplicateOnRay IClientAPI.OnObjectDuplicateOnRay
        {
            add {  }
            remove {  }
        }

        event GrabObject IClientAPI.OnGrabObject
        {
            add {  }
            remove {  }
        }

        event DeGrabObject IClientAPI.OnDeGrabObject
        {
            add {  }
            remove {  }
        }

        event MoveObject IClientAPI.OnGrabUpdate
        {
            add {  }
            remove {  }
        }

        event SpinStart IClientAPI.OnSpinStart
        {
            add {  }
            remove {  }
        }

        event SpinObject IClientAPI.OnSpinUpdate
        {
            add {  }
            remove {  }
        }

        event SpinStop IClientAPI.OnSpinStop
        {
            add {  }
            remove {  }
        }

        event UpdateShape IClientAPI.OnUpdatePrimShape
        {
            add {  }
            remove {  }
        }

        event ObjectExtraParams IClientAPI.OnUpdateExtraParams
        {
            add {  }
            remove {  }
        }

        event ObjectRequest IClientAPI.OnObjectRequest
        {
            add {  }
            remove {  }
        }

        event ObjectSelect IClientAPI.OnObjectSelect
        {
            add {  }
            remove {  }
        }

        event ObjectDeselect IClientAPI.OnObjectDeselect
        {
            add {  }
            remove {  }
        }

        event GenericCall7 IClientAPI.OnObjectDescription
        {
            add {  }
            remove {  }
        }

        event GenericCall7 IClientAPI.OnObjectName
        {
            add {  }
            remove {  }
        }

        event GenericCall7 IClientAPI.OnObjectClickAction
        {
            add {  }
            remove {  }
        }

        event GenericCall7 IClientAPI.OnObjectMaterial
        {
            add {  }
            remove {  }
        }

        event RequestObjectPropertiesFamily IClientAPI.OnRequestObjectPropertiesFamily
        {
            add {  }
            remove {  }
        }

        event UpdatePrimFlags IClientAPI.OnUpdatePrimFlags
        {
            add {  }
            remove {  }
        }

        event UpdatePrimTexture IClientAPI.OnUpdatePrimTexture
        {
            add {  }
            remove {  }
        }

        event UpdateVectorWithUpdate IClientAPI.OnUpdatePrimGroupPosition
        {
            add {  }
            remove {  }
        }

        event UpdateVectorWithUpdate IClientAPI.OnUpdatePrimSinglePosition
        {
            add {  }
            remove {  }
        }

        event UpdatePrimRotation IClientAPI.OnUpdatePrimGroupRotation
        {
            add {  }
            remove {  }
        }

        event UpdatePrimSingleRotation IClientAPI.OnUpdatePrimSingleRotation
        {
            add {  }
            remove {  }
        }

        event UpdatePrimSingleRotationPosition IClientAPI.OnUpdatePrimSingleRotationPosition
        {
            add {  }
            remove {  }
        }

        event UpdatePrimGroupRotation IClientAPI.OnUpdatePrimGroupMouseRotation
        {
            add {  }
            remove {  }
        }

        event UpdateVector IClientAPI.OnUpdatePrimScale
        {
            add {  }
            remove {  }
        }

        event UpdateVector IClientAPI.OnUpdatePrimGroupScale
        {
            add {  }
            remove {  }
        }

        event StatusChange IClientAPI.OnChildAgentStatus
        {
            add {  }
            remove {  }
        }

        event ObjectPermissions IClientAPI.OnObjectPermissions
        {
            add {  }
            remove {  }
        }

        event CreateNewInventoryItem IClientAPI.OnCreateNewInventoryItem
        {
            add {  }
            remove {  }
        }

        event LinkInventoryItem IClientAPI.OnLinkInventoryItem
        {
            add {  }
            remove {  }
        }

        event CreateInventoryFolder IClientAPI.OnCreateNewInventoryFolder
        {
            add {  }
            remove {  }
        }

        event UpdateInventoryFolder IClientAPI.OnUpdateInventoryFolder
        {
            add {  }
            remove {  }
        }

        event MoveInventoryFolder IClientAPI.OnMoveInventoryFolder
        {
            add {  }
            remove {  }
        }

        event FetchInventoryDescendents IClientAPI.OnFetchInventoryDescendents
        {
            add {  }
            remove {  }
        }

        event PurgeInventoryDescendents IClientAPI.OnPurgeInventoryDescendents
        {
            add {  }
            remove {  }
        }

        event FetchInventory IClientAPI.OnFetchInventory
        {
            add {  }
            remove {  }
        }

        event RequestTaskInventory IClientAPI.OnRequestTaskInventory
        {
            add {  }
            remove {  }
        }

        event UpdateInventoryItem IClientAPI.OnUpdateInventoryItem
        {
            add {  }
            remove {  }
        }

        event CopyInventoryItem IClientAPI.OnCopyInventoryItem
        {
            add {  }
            remove {  }
        }

        event MoveInventoryItem IClientAPI.OnMoveInventoryItem
        {
            add {  }
            remove {  }
        }

        event RemoveInventoryFolder IClientAPI.OnRemoveInventoryFolder
        {
            add {  }
            remove {  }
        }

        event RemoveInventoryItem IClientAPI.OnRemoveInventoryItem
        {
            add {  }
            remove {  }
        }

        event UDPAssetUploadRequest IClientAPI.OnAssetUploadRequest
        {
            add {  }
            remove {  }
        }

        event XferReceive IClientAPI.OnXferReceive
        {
            add {  }
            remove {  }
        }

        event RequestXfer IClientAPI.OnRequestXfer
        {
            add {  }
            remove {  }
        }

        event ConfirmXfer IClientAPI.OnConfirmXfer
        {
            add {  }
            remove {  }
        }

        event AbortXfer IClientAPI.OnAbortXfer
        {
            add {  }
            remove {  }
        }

        event RezScript IClientAPI.OnRezScript
        {
            add {  }
            remove {  }
        }

        event UpdateTaskInventory IClientAPI.OnUpdateTaskInventory
        {
            add {  }
            remove {  }
        }

        event MoveTaskInventory IClientAPI.OnMoveTaskItem
        {
            add {  }
            remove {  }
        }

        event RemoveTaskInventory IClientAPI.OnRemoveTaskItem
        {
            add {  }
            remove {  }
        }

        event UUIDNameRequest IClientAPI.OnNameFromUUIDRequest
        {
            add {  }
            remove {  }
        }

        event ParcelAccessListRequest IClientAPI.OnParcelAccessListRequest
        {
            add {  }
            remove {  }
        }

        event ParcelAccessListUpdateRequest IClientAPI.OnParcelAccessListUpdateRequest
        {
            add {  }
            remove {  }
        }

        event ParcelPropertiesRequest IClientAPI.OnParcelPropertiesRequest
        {
            add {  }
            remove {  }
        }

        event ParcelDivideRequest IClientAPI.OnParcelDivideRequest
        {
            add {  }
            remove {  }
        }

        event ParcelJoinRequest IClientAPI.OnParcelJoinRequest
        {
            add {  }
            remove {  }
        }

        event ParcelPropertiesUpdateRequest IClientAPI.OnParcelPropertiesUpdateRequest
        {
            add {  }
            remove {  }
        }

        event ParcelSelectObjects IClientAPI.OnParcelSelectObjects
        {
            add {  }
            remove {  }
        }

        event ParcelObjectOwnerRequest IClientAPI.OnParcelObjectOwnerRequest
        {
            add {  }
            remove {  }
        }

        event ParcelAbandonRequest IClientAPI.OnParcelAbandonRequest
        {
            add {  }
            remove {  }
        }

        event ParcelGodForceOwner IClientAPI.OnParcelGodForceOwner
        {
            add {  }
            remove {  }
        }

        event ParcelReclaim IClientAPI.OnParcelReclaim
        {
            add {  }
            remove {  }
        }

        event ParcelReturnObjectsRequest IClientAPI.OnParcelReturnObjectsRequest
        {
            add {  }
            remove {  }
        }

        event ParcelDeedToGroup IClientAPI.OnParcelDeedToGroup
        {
            add {  }
            remove {  }
        }

        event RegionInfoRequest IClientAPI.OnRegionInfoRequest
        {
            add {  }
            remove {  }
        }

        event EstateCovenantRequest IClientAPI.OnEstateCovenantRequest
        {
            add {  }
            remove {  }
        }

        event FriendActionDelegate IClientAPI.OnApproveFriendRequest
        {
            add {  }
            remove {  }
        }

        event FriendActionDelegate IClientAPI.OnDenyFriendRequest
        {
            add {  }
            remove {  }
        }

        event FriendshipTermination IClientAPI.OnTerminateFriendship
        {
            add {  }
            remove {  }
        }

        event MoneyTransferRequest IClientAPI.OnMoneyTransferRequest
        {
            add {  }
            remove {  }
        }

        event EconomyDataRequest IClientAPI.OnEconomyDataRequest
        {
            add {  }
            remove {  }
        }

        event MoneyBalanceRequest IClientAPI.OnMoneyBalanceRequest
        {
            add {  }
            remove {  }
        }

        event UpdateAvatarProperties IClientAPI.OnUpdateAvatarProperties
        {
            add {  }
            remove {  }
        }

        event ParcelBuy IClientAPI.OnParcelBuy
        {
            add {  }
            remove {  }
        }

        event RequestPayPrice IClientAPI.OnRequestPayPrice
        {
            add {  }
            remove {  }
        }

        event ObjectSaleInfo IClientAPI.OnObjectSaleInfo
        {
            add {  }
            remove {  }
        }

        event ObjectBuy IClientAPI.OnObjectBuy
        {
            add {  }
            remove {  }
        }

        event BuyObjectInventory IClientAPI.OnBuyObjectInventory
        {
            add {  }
            remove {  }
        }

        event RequestTerrain IClientAPI.OnRequestTerrain
        {
            add {  }
            remove {  }
        }

        event RequestTerrain IClientAPI.OnUploadTerrain
        {
            add {  }
            remove {  }
        }

        event ObjectIncludeInSearch IClientAPI.OnObjectIncludeInSearch
        {
            add {  }
            remove {  }
        }

        event UUIDNameRequest IClientAPI.OnTeleportHomeRequest
        {
            add {  }
            remove {  }
        }

        event ScriptAnswer IClientAPI.OnScriptAnswer
        {
            add {  }
            remove {  }
        }

        event AgentSit IClientAPI.OnUndo
        {
            add {  }
            remove {  }
        }

        event AgentSit IClientAPI.OnRedo
        {
            add {  }
            remove {  }
        }

        event LandUndo IClientAPI.OnLandUndo
        {
            add {  }
            remove {  }
        }

        event ForceReleaseControls IClientAPI.OnForceReleaseControls
        {
            add {  }
            remove {  }
        }

        event GodLandStatRequest IClientAPI.OnLandStatRequest
        {
            add {  }
            remove {  }
        }

        event DetailedEstateDataRequest IClientAPI.OnDetailedEstateDataRequest
        {
            add {  }
            remove {  }
        }

        event SetEstateFlagsRequest IClientAPI.OnSetEstateFlagsRequest
        {
            add {  }
            remove {  }
        }

        event SetEstateTerrainBaseTexture IClientAPI.OnSetEstateTerrainBaseTexture
        {
            add {  }
            remove {  }
        }

        event SetEstateTerrainDetailTexture IClientAPI.OnSetEstateTerrainDetailTexture
        {
            add {  }
            remove {  }
        }

        event SetEstateTerrainTextureHeights IClientAPI.OnSetEstateTerrainTextureHeights
        {
            add {  }
            remove {  }
        }

        event CommitEstateTerrainTextureRequest IClientAPI.OnCommitEstateTerrainTextureRequest
        {
            add {  }
            remove {  }
        }

        event SetRegionTerrainSettings IClientAPI.OnSetRegionTerrainSettings
        {
            add {  }
            remove {  }
        }

        event EstateRestartSimRequest IClientAPI.OnEstateRestartSimRequest
        {
            add {  }
            remove {  }
        }

        event EstateChangeCovenantRequest IClientAPI.OnEstateChangeCovenantRequest
        {
            add {  }
            remove {  }
        }

        event UpdateEstateAccessDeltaRequest IClientAPI.OnUpdateEstateAccessDeltaRequest
        {
            add {  }
            remove {  }
        }

        event SimulatorBlueBoxMessageRequest IClientAPI.OnSimulatorBlueBoxMessageRequest
        {
            add {  }
            remove {  }
        }

        event EstateBlueBoxMessageRequest IClientAPI.OnEstateBlueBoxMessageRequest
        {
            add {  }
            remove {  }
        }

        event EstateDebugRegionRequest IClientAPI.OnEstateDebugRegionRequest
        {
            add {  }
            remove {  }
        }

        event EstateTeleportOneUserHomeRequest IClientAPI.OnEstateTeleportOneUserHomeRequest
        {
            add {  }
            remove {  }
        }

        event EstateTeleportAllUsersHomeRequest IClientAPI.OnEstateTeleportAllUsersHomeRequest
        {
            add {  }
            remove {  }
        }

        event UUIDNameRequest IClientAPI.OnUUIDGroupNameRequest
        {
            add {  }
            remove {  }
        }

        event RegionHandleRequest IClientAPI.OnRegionHandleRequest
        {
            add {  }
            remove {  }
        }

        event ParcelInfoRequest IClientAPI.OnParcelInfoRequest
        {
            add {  }
            remove {  }
        }

        event RequestObjectPropertiesFamily IClientAPI.OnObjectGroupRequest
        {
            add {  }
            remove {  }
        }

        event ScriptReset IClientAPI.OnScriptReset
        {
            add {  }
            remove {  }
        }

        event GetScriptRunning IClientAPI.OnGetScriptRunning
        {
            add {  }
            remove {  }
        }

        event SetScriptRunning IClientAPI.OnSetScriptRunning
        {
            add {  }
            remove {  }
        }

        event UpdateVector IClientAPI.OnAutoPilotGo
        {
            add {  }
            remove {  }
        }

        event TerrainUnacked IClientAPI.OnUnackedTerrain
        {
            add {  }
            remove {  }
        }

        event ActivateGesture IClientAPI.OnActivateGesture
        {
            add {  }
            remove {  }
        }

        event DeactivateGesture IClientAPI.OnDeactivateGesture
        {
            add {  }
            remove {  }
        }

        event ObjectOwner IClientAPI.OnObjectOwner
        {
            add {  }
            remove {  }
        }

        event DirPlacesQuery IClientAPI.OnDirPlacesQuery
        {
            add {  }
            remove {  }
        }

        event DirFindQuery IClientAPI.OnDirFindQuery
        {
            add {  }
            remove {  }
        }

        event DirLandQuery IClientAPI.OnDirLandQuery
        {
            add {  }
            remove {  }
        }

        event DirPopularQuery IClientAPI.OnDirPopularQuery
        {
            add {  }
            remove {  }
        }

        event DirClassifiedQuery IClientAPI.OnDirClassifiedQuery
        {
            add {  }
            remove {  }
        }

        event EventInfoRequest IClientAPI.OnEventInfoRequest
        {
            add {  }
            remove {  }
        }

        event ParcelSetOtherCleanTime IClientAPI.OnParcelSetOtherCleanTime
        {
            add {  }
            remove {  }
        }

        event MapItemRequest IClientAPI.OnMapItemRequest
        {
            add {  }
            remove {  }
        }

        event OfferCallingCard IClientAPI.OnOfferCallingCard
        {
            add {  }
            remove {  }
        }

        event AcceptCallingCard IClientAPI.OnAcceptCallingCard
        {
            add {  }
            remove {  }
        }

        event DeclineCallingCard IClientAPI.OnDeclineCallingCard
        {
            add {  }
            remove {  }
        }

        event SoundTrigger IClientAPI.OnSoundTrigger
        {
            add {  }
            remove {  }
        }

        event StartLure IClientAPI.OnStartLure
        {
            add {  }
            remove {  }
        }

        event TeleportLureRequest IClientAPI.OnTeleportLureRequest
        {
            add {  }
            remove {  }
        }

        event NetworkStats IClientAPI.OnNetworkStatsUpdate
        {
            add {  }
            remove {  }
        }

        event ClassifiedInfoRequest IClientAPI.OnClassifiedInfoRequest
        {
            add {  }
            remove {  }
        }

        event ClassifiedInfoUpdate IClientAPI.OnClassifiedInfoUpdate
        {
            add {  }
            remove {  }
        }

        event ClassifiedDelete IClientAPI.OnClassifiedDelete
        {
            add {  }
            remove {  }
        }

        event ClassifiedDelete IClientAPI.OnClassifiedGodDelete
        {
            add {  }
            remove {  }
        }

        event EventNotificationAddRequest IClientAPI.OnEventNotificationAddRequest
        {
            add {  }
            remove {  }
        }

        event EventNotificationRemoveRequest IClientAPI.OnEventNotificationRemoveRequest
        {
            add {  }
            remove {  }
        }

        event EventGodDelete IClientAPI.OnEventGodDelete
        {
            add {  }
            remove {  }
        }

        event ParcelDwellRequest IClientAPI.OnParcelDwellRequest
        {
            add {  }
            remove {  }
        }

        event UserInfoRequest IClientAPI.OnUserInfoRequest
        {
            add {  }
            remove {  }
        }

        event UpdateUserInfo IClientAPI.OnUpdateUserInfo
        {
            add {  }
            remove {  }
        }

        event RetrieveInstantMessages IClientAPI.OnRetrieveInstantMessages
        {
            add {  }
            remove {  }
        }

        event PickDelete IClientAPI.OnPickDelete
        {
            add {  }
            remove {  }
        }

        event PickGodDelete IClientAPI.OnPickGodDelete
        {
            add {  }
            remove {  }
        }

        event PickInfoUpdate IClientAPI.OnPickInfoUpdate
        {
            add {  }
            remove {  }
        }

        event AvatarNotesUpdate IClientAPI.OnAvatarNotesUpdate
        {
            add {  }
            remove {  }
        }

        event AvatarInterestUpdate IClientAPI.OnAvatarInterestUpdate
        {
            add {  }
            remove {  }
        }

        event GrantUserFriendRights IClientAPI.OnGrantUserRights
        {
            add {  }
            remove {  }
        }

        event MuteListRequest IClientAPI.OnMuteListRequest
        {
            add {  }
            remove {  }
        }

        event PlacesQuery IClientAPI.OnPlacesQuery
        {
            add {  }
            remove {  }
        }

        event FindAgentUpdate IClientAPI.OnFindAgent
        {
            add {  }
            remove {  }
        }

        event TrackAgentUpdate IClientAPI.OnTrackAgent
        {
            add {  }
            remove {  }
        }

        event NewUserReport IClientAPI.OnUserReport
        {
            add {  }
            remove {  }
        }

        event SaveStateHandler IClientAPI.OnSaveState
        {
            add {  }
            remove {  }
        }

        event GroupAccountSummaryRequest IClientAPI.OnGroupAccountSummaryRequest
        {
            add {  }
            remove {  }
        }

        event GroupAccountDetailsRequest IClientAPI.OnGroupAccountDetailsRequest
        {
            add {  }
            remove {  }
        }

        event GroupAccountTransactionsRequest IClientAPI.OnGroupAccountTransactionsRequest
        {
            add {  }
            remove {  }
        }

        event FreezeUserUpdate IClientAPI.OnParcelFreezeUser
        {
            add {  }
            remove {  }
        }

        event EjectUserUpdate IClientAPI.OnParcelEjectUser
        {
            add {  }
            remove {  }
        }

        event ParcelBuyPass IClientAPI.OnParcelBuyPass
        {
            add {  }
            remove {  }
        }

        event ParcelGodMark IClientAPI.OnParcelGodMark
        {
            add {  }
            remove {  }
        }

        event GroupActiveProposalsRequest IClientAPI.OnGroupActiveProposalsRequest
        {
            add {  }
            remove {  }
        }

        event GroupVoteHistoryRequest IClientAPI.OnGroupVoteHistoryRequest
        {
            add {  }
            remove {  }
        }

        event SimWideDeletesDelegate IClientAPI.OnSimWideDeletes
        {
            add {  }
            remove {  }
        }

        event SendPostcard IClientAPI.OnSendPostcard
        {
            add {  }
            remove {  }
        }

        event MuteListEntryUpdate IClientAPI.OnUpdateMuteListEntry
        {
            add {  }
            remove {  }
        }

        event MuteListEntryRemove IClientAPI.OnRemoveMuteListEntry
        {
            add {  }
            remove {  }
        }

        event GodlikeMessage IClientAPI.OnGodlikeMessage
        {
            add {  }
            remove {  }
        }

        event GodUpdateRegionInfoUpdate IClientAPI.OnGodUpdateRegionInfoUpdate
        {
            add {  }
            remove {  }
        }

        event ChangeInventoryItemFlags IClientAPI.OnChangeInventoryItemFlags
        {
            add {  }
            remove {  }
        }

        event TeleportCancel IClientAPI.OnTeleportCancel
        {
            add {  }
            remove {  }
        }

        event GodlikeMessage IClientAPI.OnEstateTelehubRequest
        {
            add {  }
            remove {  }
        }

        event ViewerStartAuction IClientAPI.OnViewerStartAuction
        {
            add {  }
            remove {  }
        }

        void IClientAPI.SetDebugPacketLevel(int newDebug)
        {
            
        }

        void IClientAPI.InPacket(object NewPack)
        {
            
        }

        void IClientAPI.ProcessInPacket(OpenMetaverse.Packets.Packet NewPack)
        {
            
        }

        void IClientAPI.Close()
        {
            
        }

        void IClientAPI.Kick(string message)
        {
            
        }

        void IClientAPI.Start()
        {
            
        }

        void IClientAPI.Stop()
        {
            
        }

        void IClientAPI.SendWearables(AvatarWearable[] wearables, int serial)
        {
            
        }

        void IClientAPI.SendAppearance(UUID agentID, byte[] visualParams, byte[] textureEntry)
        {
            
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
            
        }

        void IClientAPI.SendChatMessage(string message, byte type, Vector3 fromPos, string fromName, UUID fromAgentID, byte source, byte audible)
        {
            
        }

        void IClientAPI.SendInstantMessage(GridInstantMessage im)
        {
            
        }

        void IClientAPI.SendGenericMessage(string method, List<string> message)
        {
            
        }

        void IClientAPI.SendGenericMessage(string method, List<byte[]> message)
        {
            
        }

        void IClientAPI.SendLayerData(float[] map)
        {
            
        }

        void IClientAPI.SendLayerData(int px, int py, float[] map)
        {
            
        }

        void IClientAPI.SendLayerPacket(float[] map, int x, int y)
        {
            
        }

        void IClientAPI.SendWindData(Vector2[] windSpeeds)
        {
            
        }

        void IClientAPI.SendCloudData(float[] cloudCover)
        {
            
        }

        void IClientAPI.MoveAgentIntoRegion(RegionInfo regInfo, Vector3 pos, Vector3 look)
        {
            
        }

        void IClientAPI.InformClientOfNeighbour(ulong neighbourHandle, IPEndPoint neighbourExternalEndPoint)
        {
            
        }

        AgentCircuitData IClientAPI.RequestClientInfo()
        {
            return null;
        }

        void IClientAPI.CrossRegion(ulong newRegionHandle, Vector3 pos, Vector3 lookAt, IPEndPoint newRegionExternalEndPoint, string capsURL)
        {
            
        }

        void IClientAPI.SendMapBlock(List<MapBlockData> mapBlocks, uint flag)
        {
            
        }

        void IClientAPI.SendLocalTeleport(Vector3 position, Vector3 lookAt, uint flags)
        {
            
        }

        void IClientAPI.SendRegionTeleport(ulong regionHandle, byte simAccess, IPEndPoint regionExternalEndPoint, uint locationID, uint flags, string capsURL)
        {
            
        }

        void IClientAPI.SendTeleportFailed(string reason)
        {
            
        }

        void IClientAPI.SendMoneyBalance(UUID transaction, bool success, byte[] description, int balance)
        {
            
        }

        void IClientAPI.SendPayPrice(UUID objectID, int[] payPrice)
        {
            
        }

        void IClientAPI.SendCoarseLocationUpdate(List<UUID> users, List<Vector3> CoarseLocations)
        {
            
        }

        void IClientAPI.SetChildAgentThrottle(byte[] throttle)
        {
            
        }

        void IClientAPI.SendAvatarDataImmediate(ISceneEntity avatar)
        {
            
        }

        void IClientAPI.SendPrimUpdate(ISceneEntity entity, PrimUpdateFlags updateFlags)
        {
        }

        void IClientAPI.SendPrimUpdate(ISceneEntity entity, PrimUpdateFlags updateFlags, double priority)
        {
        }

        void IClientAPI.FlushPrimUpdates()
        {
            
        }

        void IClientAPI.SendInventoryFolderDetails(UUID ownerID, UUID folderID, List<InventoryItemBase> items, List<InventoryFolderBase> folders, int version, bool fetchFolders, bool fetchItems)
        {
            
        }

        void IClientAPI.SendInventoryItemDetails(UUID ownerID, InventoryItemBase item)
        {
            
        }

        void IClientAPI.SendInventoryItemCreateUpdate(InventoryItemBase Item, uint callbackId)
        {
            
        }

        void IClientAPI.SendRemoveInventoryItem(UUID itemID)
        {
            
        }

        void IClientAPI.SendTakeControls(int controls, bool passToAgent, bool TakeControls)
        {
            
        }

        void IClientAPI.SendTaskInventory(UUID taskID, short serial, byte[] fileName)
        {
            
        }

        void IClientAPI.SendBulkUpdateInventory(InventoryNodeBase node)
        {
            
        }

        void IClientAPI.SendXferPacket(ulong xferID, uint packet, byte[] data)
        {
            
        }

        void IClientAPI.SendEconomyData(float EnergyEfficiency, int ObjectCapacity, int ObjectCount, int PriceEnergyUnit, int PriceGroupCreate, int PriceObjectClaim, float PriceObjectRent, float PriceObjectScaleFactor, int PriceParcelClaim, float PriceParcelClaimFactor, int PriceParcelRent, int PricePublicObjectDecay, int PricePublicObjectDelete, int PriceRentLight, int PriceUpload, int TeleportMinPrice, float TeleportPriceExponent)
        {
            
        }

        void IClientAPI.SendAvatarPickerReply(AvatarPickerReplyAgentDataArgs AgentData, List<AvatarPickerReplyDataArgs> Data)
        {
            
        }

        void IClientAPI.SendAgentDataUpdate(UUID agentid, UUID activegroupid, string firstname, string lastname, ulong grouppowers, string groupname, string grouptitle)
        {
            
        }

        void IClientAPI.SendPreLoadSound(UUID objectID, UUID ownerID, UUID soundID)
        {
            
        }

        void IClientAPI.SendPlayAttachedSound(UUID soundID, UUID objectID, UUID ownerID, float gain, byte flags)
        {
            
        }

        void IClientAPI.SendTriggeredSound(UUID soundID, UUID ownerID, UUID objectID, UUID parentID, ulong handle, Vector3 position, float gain)
        {
            
        }

        void IClientAPI.SendAttachedSoundGainChange(UUID objectID, float gain)
        {
            
        }

        void IClientAPI.SendNameReply(UUID profileId, string firstname, string lastname)
        {
            
        }

        void IClientAPI.SendAlertMessage(string message)
        {
            
        }

        void IClientAPI.SendAgentAlertMessage(string message, bool modal)
        {
            
        }

        void IClientAPI.SendLoadURL(string objectname, UUID objectID, UUID ownerID, bool groupOwned, string message, string url)
        {
            
        }

        void IClientAPI.SendDialog(string objectname, UUID objectID, string ownerFirstName, string ownerLastName, string msg, UUID textureID, int ch, string[] buttonlabels)
        {
            
        }

        void IClientAPI.SendSunPos(Vector3 sunPos, Vector3 sunVel, ulong CurrentTime, uint SecondsPerSunCycle, uint SecondsPerYear, float OrbitalPosition)
        {
            
        }

        void IClientAPI.SendViewerEffect(OpenMetaverse.Packets.ViewerEffectPacket.EffectBlock[] effectBlocks)
        {
            
        }

        void IClientAPI.SendViewerTime(int phase)
        {
            
        }

        UUID IClientAPI.GetDefaultAnimation(string name)
        {
            return UUID.Zero;
        }

        void IClientAPI.SendAvatarProperties(UUID avatarID, string aboutText, string bornOn, byte[] charterMember, string flAbout, uint flags, UUID flImageID, UUID imageID, string profileURL, UUID partnerID)
        {
            
        }

        void IClientAPI.SendScriptQuestion(UUID taskID, string taskName, string ownerName, UUID itemID, int question)
        {
            
        }

        void IClientAPI.SendHealth(float health)
        {
            
        }

        void IClientAPI.SendEstateList(UUID invoice, int code, UUID[] Data, uint estateID)
        {
            
        }

        void IClientAPI.SendBannedUserList(UUID invoice, EstateBan[] banlist, uint estateID)
        {
            
        }

        void IClientAPI.SendRegionInfoToEstateMenu(RegionInfoForEstateMenuArgs args)
        {
            
        }

        void IClientAPI.SendEstateCovenantInformation(UUID covenant, int covenantLastUpdated)
        {
            
        }

        void IClientAPI.SendDetailedEstateData(UUID invoice, string estateName, uint estateID, uint parentEstate, uint estateFlags, uint sunPosition, UUID covenant, int covenantLastUpdated, string abuseEmail, UUID estateOwner)
        {
            
        }

        void IClientAPI.SendLandProperties(int sequence_id, bool snap_selection, int request_result, LandData landData, float simObjectBonusFactor, int parcelObjectCapacity, int simObjectCapacity, uint regionFlags)
        {
            
        }

        void IClientAPI.SendLandAccessListData(List<UUID> avatars, uint accessFlag, int localLandID)
        {
            
        }

        void IClientAPI.SendForceClientSelectObjects(List<uint> objectIDs)
        {
            
        }

        void IClientAPI.SendCameraConstraint(Vector4 ConstraintPlane)
        {
            
        }

        void IClientAPI.SendLandObjectOwners(LandData land, List<UUID> groups, Dictionary<UUID, int> ownersAndCount)
        {
            
        }

        void IClientAPI.SendLandParcelOverlay(byte[] data, int sequence_id)
        {
            
        }

        void IClientAPI.SendParcelMediaCommand(uint flags, ParcelMediaCommandEnum command, float time)
        {
            
        }

        void IClientAPI.SendParcelMediaUpdate(string mediaUrl, UUID mediaTextureID, byte autoScale, string mediaType, string mediaDesc, int mediaWidth, int mediaHeight, byte mediaLoop)
        {
            
        }

        void IClientAPI.SendAssetUploadCompleteMessage(sbyte AssetType, bool Success, UUID AssetFullID)
        {
            
        }

        void IClientAPI.SendConfirmXfer(ulong xferID, uint PacketID)
        {
            
        }

        void IClientAPI.SendXferRequest(ulong XferID, short AssetType, UUID vFileID, byte FilePath, byte[] FileName)
        {
            
        }

        void IClientAPI.SendInitiateDownload(string simFileName, string clientFileName)
        {
            
        }

        void IClientAPI.SendImageFirstPart(ushort numParts, UUID ImageUUID, uint ImageSize, byte[] ImageData, byte imageCodec)
        {
            
        }

        void IClientAPI.SendImageNextPart(ushort partNumber, UUID imageUuid, byte[] imageData)
        {
            
        }

        void IClientAPI.SendImageNotFound(UUID imageid)
        {
            
        }

        void IClientAPI.SendShutdownConnectionNotice()
        {
            
        }

        void IClientAPI.SendSimStats(SimStats stats)
        {
            
        }

        void IClientAPI.SendObjectPropertiesFamilyData(uint RequestFlags, UUID ObjectUUID, UUID OwnerID, UUID GroupID, uint BaseMask, uint OwnerMask, uint GroupMask, uint EveryoneMask, uint NextOwnerMask, int OwnershipCost, byte SaleType, int SalePrice, uint Category, UUID LastOwnerID, string ObjectName, string Description)
        {
            
        }

        void IClientAPI.SendObjectPropertiesReply(List<ISceneEntity> part)
        {
            
        }

        void IClientAPI.SendAgentOffline(UUID[] agentIDs)
        {
            
        }

        void IClientAPI.SendAgentOnline(UUID[] agentIDs)
        {
            
        }

        void IClientAPI.SendSitResponse(UUID TargetID, Vector3 OffsetPos, Quaternion SitOrientation, bool autopilot, Vector3 CameraAtOffset, Vector3 CameraEyeOffset, bool ForceMouseLook)
        {
            
        }

        void IClientAPI.SendAdminResponse(UUID Token, uint AdminLevel)
        {
            
        }

        void IClientAPI.SendGroupMembership(GroupMembershipData[] GroupMembership)
        {
            
        }

        void IClientAPI.SendGroupNameReply(UUID groupLLUID, string GroupName)
        {
            
        }

        void IClientAPI.SendJoinGroupReply(UUID groupID, bool success)
        {
            
        }

        void IClientAPI.SendEjectGroupMemberReply(UUID agentID, UUID groupID, bool success)
        {
            
        }

        void IClientAPI.SendLeaveGroupReply(UUID groupID, bool success)
        {
            
        }

        void IClientAPI.SendCreateGroupReply(UUID groupID, bool success, string message)
        {
            
        }

        void IClientAPI.SendLandStatReply(uint reportType, uint requestFlags, uint resultCount, LandStatReportItem[] lsrpia)
        {
            
        }

        void IClientAPI.SendScriptRunningReply(UUID objectID, UUID itemID, bool running)
        {
            
        }

        void IClientAPI.SendAsset(AssetRequestToClient req)
        {
            
        }

        void IClientAPI.SendTexture(AssetBase TextureAsset)
        {
            
        }

        byte[] IClientAPI.GetThrottlesPacked(float multiplier)
        {
            return null;
        }

        event ViewerEffectEventHandler IClientAPI.OnViewerEffect
        {
            add {  }
            remove {  }
        }

        event Action<IClientAPI> IClientAPI.OnLogout
        {
            add {  }
            remove {  }
        }

        event Action<IClientAPI> IClientAPI.OnConnectionClosed
        {
            add {  }
            remove {  }
        }

        void IClientAPI.SendBlueBoxMessage(UUID FromAvatarID, string FromAvatarName, string Message)
        {
            
        }

        void IClientAPI.SendLogoutPacket()
        {
            
        }

        EndPoint IClientAPI.GetClientEP()
        {
            return null;
        }

        void IClientAPI.SendSetFollowCamProperties(UUID objectID, SortedDictionary<int, float> parameters)
        {
            
        }

        void IClientAPI.SendClearFollowCamProperties(UUID objectID)
        {
            
        }

        void IClientAPI.SendRegionHandle(UUID regoinID, ulong handle)
        {
            
        }

        void IClientAPI.SendParcelInfo(LandData land, UUID parcelID, uint x, uint y, string SimName)
        {
            
        }

        void IClientAPI.SendScriptTeleportRequest(string objName, string simName, Vector3 pos, Vector3 lookAt)
        {
            
        }

        void IClientAPI.SendDirPlacesReply(UUID queryID, DirPlacesReplyData[] data)
        {
            
        }

        void IClientAPI.SendDirPeopleReply(UUID queryID, DirPeopleReplyData[] data)
        {
            
        }

        void IClientAPI.SendDirEventsReply(UUID queryID, DirEventsReplyData[] data)
        {
            
        }

        void IClientAPI.SendDirGroupsReply(UUID queryID, DirGroupsReplyData[] data)
        {
            
        }

        void IClientAPI.SendDirClassifiedReply(UUID queryID, DirClassifiedReplyData[] data)
        {
            
        }

        void IClientAPI.SendDirLandReply(UUID queryID, DirLandReplyData[] data)
        {
            
        }

        void IClientAPI.SendDirPopularReply(UUID queryID, DirPopularReplyData[] data)
        {
            
        }

        void IClientAPI.SendEventInfoReply(EventData info)
        {
            
        }

        void IClientAPI.SendMapItemReply(mapItemReply[] replies, uint mapitemtype, uint flags)
        {
            
        }

        void IClientAPI.SendAvatarGroupsReply(UUID avatarID, GroupMembershipData[] data)
        {
            
        }

        void IClientAPI.SendOfferCallingCard(UUID srcID, UUID transactionID)
        {
            
        }

        void IClientAPI.SendAcceptCallingCard(UUID transactionID)
        {
            
        }

        void IClientAPI.SendDeclineCallingCard(UUID transactionID)
        {
            
        }

        void IClientAPI.SendTerminateFriend(UUID exFriendID)
        {
            
        }

        void IClientAPI.SendAvatarClassifiedReply(UUID targetID, UUID[] classifiedID, string[] name)
        {
            
        }

        void IClientAPI.SendClassifiedInfoReply(UUID classifiedID, UUID creatorID, uint creationDate, uint expirationDate, uint category, string name, string description, UUID parcelID, uint parentEstate, UUID snapshotID, string simName, Vector3 globalPos, string parcelName, byte classifiedFlags, int price)
        {
            
        }

        void IClientAPI.SendAgentDropGroup(UUID groupID)
        {
            
        }

        void IClientAPI.SendAvatarNotesReply(UUID targetID, string text)
        {
            
        }

        void IClientAPI.SendAvatarPicksReply(UUID targetID, Dictionary<UUID, string> picks)
        {
            
        }

        void IClientAPI.SendPickInfoReply(UUID pickID, UUID creatorID, bool topPick, UUID parcelID, string name, string desc, UUID snapshotID, string user, string originalName, string simName, Vector3 posGlobal, int sortOrder, bool enabled)
        {
            
        }

        void IClientAPI.SendAvatarClassifiedReply(UUID targetID, Dictionary<UUID, string> classifieds)
        {
            
        }

        void IClientAPI.SendParcelDwellReply(int localID, UUID parcelID, float dwell)
        {
            
        }

        void IClientAPI.SendUserInfoReply(bool imViaEmail, bool visible, string email)
        {
            
        }

        void IClientAPI.SendUseCachedMuteList()
        {
            
        }

        void IClientAPI.SendMuteListUpdate(string filename)
        {
            
        }

        void IClientAPI.SendGroupActiveProposals(UUID groupID, UUID transactionID, GroupActiveProposals[] Proposals)
        {
            
        }

        void IClientAPI.SendGroupVoteHistory(UUID groupID, UUID transactionID, GroupVoteHistory Vote, GroupVoteHistoryItem[] Items)
        {
            
        }

        bool IClientAPI.AddGenericPacketHandler(string MethodName, GenericMessage handler)
        {
            return false;   
        }

        void IClientAPI.SendRebakeAvatarTextures(UUID textureID)
        {
            
        }

        void IClientAPI.SendAvatarInterestsReply(UUID avatarID, uint wantMask, string wantText, uint skillsMask, string skillsText, string languages)
        {
            
        }

        void IClientAPI.SendGroupAccountingDetails(IClientAPI sender, UUID groupID, UUID transactionID, UUID sessionID, int amt)
        {
            
        }

        void IClientAPI.SendGroupAccountingSummary(IClientAPI sender, UUID groupID, uint moneyAmt, int totalTier, int usedTier)
        {
            
        }

        void IClientAPI.SendGroupTransactionsSummaryDetails(IClientAPI sender, UUID groupID, UUID transactionID, UUID sessionID, int amt)
        {
            
        }

        void IClientAPI.SendChangeUserRights(UUID agentID, UUID friendID, int rights)
        {
            
        }

        void IClientAPI.SendTextBoxRequest(string message, int chatChannel, string objectname, string ownerFirstName, string ownerLastName, UUID objectId)
        {
            
        }

        void IClientAPI.SendPlacesQuery(ExtendedLandData[] LandData, UUID queryID, UUID transactionID)
        {
            
        }

        void IClientAPI.FireUpdateParcel(LandUpdateArgs args, int LocalID)
        {
            
        }

        void IClientAPI.SendTelehubInfo(Vector3 TelehubPos, Quaternion TelehubRot, List<Vector3> SpawnPoint, UUID ObjectID, string Name)
        {
            
        }

        void IClientAPI.StopFlying(ISceneEntity presence)
        {
            
        }

        #endregion
    }
}
