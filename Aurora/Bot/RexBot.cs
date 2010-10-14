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

namespace OpenSim.Region.Examples.RexBot
{
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
    }
}
