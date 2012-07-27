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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes.Animation;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using OpenSim.Services.Interfaces;
using PrimType = Aurora.Framework.PrimType;

namespace OpenSim.Region.Framework.Scenes
{
    public delegate void SendCourseLocationsMethod (UUID scene, IScenePresence presence, List<Vector3> coarseLocations, List<UUID> avatarUUIDs);

    public class ScenePresence : EntityBase, IScenePresence
    {
        #region Declares

        public event AddPhysics OnAddPhysics;
        public event RemovePhysics OnRemovePhysics;
        public event AddPhysics OnSignificantClientMovement;

        protected static readonly Array DIR_CONTROL_FLAGS = Enum.GetValues(typeof(Dir_ControlFlags));
        protected static readonly Vector3 HEAD_ADJUSTMENT = new Vector3(0f, 0f, 0.3f);
        
        /// <summary>
        /// Experimentally determined "fudge factor" to make sit-target positions
        /// the same as in SecondLife. Fudge factor was tested for 36 different
        /// test cases including prims of type box, sphere, cylinder, and torus,
        /// with varying parameters for sit target location, prim size, prim
        /// rotation, prim cut, prim twist, prim taper, and prim shear. See mantis
        /// issue #1716
        /// </summary>
        protected static readonly Vector3 SIT_TARGET_ADJUSTMENT = new Vector3(0.1f, 0.0f, 0.3f);

        private UUID m_currentParcelUUID = UUID.Zero;

        public UUID CurrentParcelUUID
        {
            get
            {
                return m_currentParcelUUID;
            }
            set
            {
                m_currentParcelUUID = value;
            }
        }

        public ILandObject CurrentParcel { get; set; }

        protected ISceneViewer m_sceneViewer;

        /// <value>
        /// The animator for this avatar
        /// </value>
        public IAnimator Animator
        {
            get { return m_animator; }
        }
        protected Animator m_animator;

        protected SceneObjectGroup proxyObjectGroup;
        public Vector3 LastKnownAllowedPosition { get; set; }

        protected Vector4 m_CollisionPlane = Vector4.UnitW;
        public Vector4 CollisionPlane
        {
            get
            {
                return m_CollisionPlane;
            }
            set
            {
                m_CollisionPlane = value;
            }
        }

        protected uint m_movementflag;
        protected UUID m_requestedSitTargetUUID;
        protected bool m_sitting;

        public UUID SittingOnUUID
        {
            get { return m_requestedSitTargetUUID; }
        }
        public bool Sitting
        {
            get { return m_sitting; }
        }

        protected bool m_enqueueSendChildAgentUpdate = false;
        protected DateTime m_enqueueSendChildAgentUpdateTime = new DateTime();

        public bool SitGround { get; set; }

        protected SendCourseLocationsMethod m_sendCourseLocationsMethod;

        protected float m_sitAvatarHeight = 2.0f;

        protected int m_godLevel;
        protected readonly int m_userLevel;

        public bool m_invulnerable = true;

        protected Vector3 m_lastChildAgentUpdatePosition;
        protected Vector3 m_lastChildAgentUpdateCamPosition;

        protected int m_perfMonMS;

        protected bool m_setAlwaysRun;

        protected bool m_forceFly;
        protected bool m_flyDisabled;
        protected volatile bool m_creatingPhysicalRepresentation = false;

        protected const float SIGNIFICANT_MOVEMENT = 2.0f;
        protected const float TERSE_UPDATE_MOVEMENT = 0.5f;

        protected Quaternion m_bodyRot = Quaternion.Identity;

        protected const int LAND_VELOCITYMAG_MAX = 20;

        // Default AV Height
        protected float m_defaultAvHeight = 1.56f;

        protected ulong crossingFromRegion;

        protected readonly Vector3[] Dir_Vectors = new Vector3[12];

        /// <summary>
        /// Position of agent's camera in world (region cordinates)
        /// </summary>
        protected Vector3 m_CameraCenter;
        /// <summary>
        /// Used for trigging signficant camera movement
        /// </summary>
        protected Vector3 m_lastCameraCenter;

        // Use these three vectors to figure out what the agent is looking at
        // Convert it to a Matrix and/or Quaternion
        protected Vector3 m_CameraAtAxis;
        protected Vector3 m_CameraLeftAxis;
        protected Vector3 m_CameraUpAxis;
        protected AgentManager.ControlFlags m_AgentControlFlags;
        protected Quaternion m_headrotation = Quaternion.Identity;
        protected byte m_state;

        protected bool m_autopilotMoving;
        protected Vector3 m_autoPilotTarget;
        protected bool m_sitAtAutoTarget;

        protected string m_nextSitAnimation = String.Empty;

        //PauPaw:Proper PID Controler for autopilot************
        protected bool m_moveToPositionInProgress;
        protected Vector3 m_moveToPositionTarget;

        protected bool m_followCamAuto;

        protected int m_movementUpdateCount;

        protected const int NumMovementsBetweenRayCast = 5;

        /// <summary>
        /// ONLY HERE FOR OPENSIM COMPATIBILITY
        /// </summary>
        protected string m_callbackURI = null;
        public string CallbackURI
        {
            get { return m_callbackURI; }
            set
            {
                m_callbackURI = value;
            }
        }

        protected bool CameraConstraintActive;
        //private int m_moveToPositionStateStatus;
        //*****************************************************

        // Agent's Draw distance.
        protected float m_DrawDistance;

        /// <summary>
        /// Implemented Control Flags
        /// </summary>
        private enum Dir_ControlFlags
        {
            DIR_CONTROL_FLAG_FORWARD = AgentManager.ControlFlags.AGENT_CONTROL_AT_POS,
            DIR_CONTROL_FLAG_BACK = AgentManager.ControlFlags.AGENT_CONTROL_AT_NEG,
            DIR_CONTROL_FLAG_LEFT = AgentManager.ControlFlags.AGENT_CONTROL_LEFT_POS,
            DIR_CONTROL_FLAG_RIGHT = AgentManager.ControlFlags.AGENT_CONTROL_LEFT_NEG,
            DIR_CONTROL_FLAG_UP = AgentManager.ControlFlags.AGENT_CONTROL_UP_POS,
            DIR_CONTROL_FLAG_DOWN = AgentManager.ControlFlags.AGENT_CONTROL_UP_NEG,
            DIR_CONTROL_FLAG_FORWARD_NUDGE = AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_AT_POS,
            DIR_CONTROL_FLAG_BACKWARD_NUDGE = AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_AT_NEG,
            DIR_CONTROL_FLAG_LEFT_NUDGE = AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_LEFT_POS,
            DIR_CONTROL_FLAG_RIGHT_NUDGE = AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_LEFT_NEG,
            DIR_CONTROL_FLAG_DOWN_NUDGE = AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_UP_NEG,
            DIR_CONTROL_FLAG_UP_NUDGE = AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_UP_POS,
        }
        
        /// <summary>
        /// Position at which a significant movement was made
        /// </summary>
        private Vector3 posLastSignificantMove;
        private Vector3 posLastTerseUpdate;

        private UUID CollisionSoundID = UUID.Zero;
        private int CollisionSoundLastTriggered = 0;

        #endregion

        #region Properties

        public bool AttachmentsLoaded 
        {
            get; 
            set;
        }

        public bool SuccessfullyMadeRootAgent
        {
            get;
            private set;
        }

        /// <summary>
        /// Physical scene representation of this Avatar.
        /// </summary>
        public PhysicsCharacter PhysicsActor
        {
            get { return m_physicsActor; }
            set { m_physicsActor = value; }
        }

        public uint MovementFlag
        {
            get { return m_movementflag; }
            set { m_movementflag = value; }
        }

        public bool Updated { get; set; }

        public bool Invulnerable
        {
            get { return m_invulnerable; }
            set { m_invulnerable = value; }
        }

        /// <summary>
        /// The User Level the client has (taken from UserAccount)
        /// </summary>
        public int UserLevel
        {
            get { return m_userLevel; }
        }

        /// <summary>
        /// The current status of god level in the client
        /// </summary>
        public int GodLevel
        {
            get { return m_godLevel; }
            set { m_godLevel = value; }
        }

        public Vector3 CameraPosition
        {
            get { return m_CameraCenter; }
        }

        public Quaternion CameraRotation
        {
            get { return Util.Axes2Rot(m_CameraAtAxis, m_CameraLeftAxis, m_CameraUpAxis); }
        }

        public Vector3 CameraAtAxis
        {
            get { return m_CameraAtAxis; }
        }

        public Vector3 CameraLeftAxis
        {
            get { return m_CameraLeftAxis; }
        }

        public Vector3 CameraUpAxis
        {
            get { return m_CameraUpAxis; }
        }

        public Vector3 Lookat
        {
            get
            {
                Vector3 a = new Vector3(m_CameraAtAxis.X, m_CameraAtAxis.Y, 0);

                if (a == Vector3.Zero)
                    return a;

                return Util.GetNormalizedVector(a);
            }
        }

        private readonly string m_firstname;

        public string Firstname
        {
            get { return m_firstname; }
        }

        private readonly string m_lastname;

        public string Lastname
        {
            get { return m_lastname; }
        }

        public override string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                //No changing of avatar's names!
            }
        }

        public float DrawDistance
        {
            get { return m_DrawDistance; }
            set
            {
                if (m_DrawDistance != value && value != 0)
                {
                    m_DrawDistance = value;
                    //Fire the event
                    Scene.AuroraEventManager.FireGenericEventHandler("DrawDistanceChanged", this);
                    if (!IsChildAgent)
                    {
                        //Send an update to all child agents if we are a root agent
                        AddChildAgentUpdateTaint(5);
                    }
                }
            }
        }

        public bool IsTainted { get; set; }

        private PresenceTaint m_Taints = 0;
        public PresenceTaint Taints
        {
            get { return m_Taints; }
            set { m_Taints = value; }
        }

        protected bool m_allowMovement = true;

        public bool AllowMovement
        {
            get { return m_allowMovement; }
            set { m_allowMovement = value; }
        }

        protected bool m_FallenStandUp = true;

        public bool FallenStandUp
        {
            get { return m_FallenStandUp; }
            set { m_FallenStandUp = value; }
        }

        protected bool m_Frozen = false;

        public bool Frozen
        {
            get { return m_Frozen; }
            set { m_Frozen = value; }
        }

        protected bool m_IsJumping = false;
        public bool IsJumping
        {
            get { return m_IsJumping; }
            set { m_IsJumping = value; }
        }

        protected bool ClientIsStarting = true;

        public bool SetAlwaysRun
        {
            get
            {
                return PhysicsActor != null ? PhysicsActor.SetAlwaysRun : m_setAlwaysRun;
            }
            set
            {
                m_setAlwaysRun = value;
                if (PhysicsActor != null)
                {
                    PhysicsActor.SetAlwaysRun = value;
                }
            }
        }

        public byte State
        {
            get { return m_state; }
            set { m_state = value; }
        }

        public uint AgentControlFlags
        {
            get { return (uint)m_AgentControlFlags; }
            set { m_AgentControlFlags = (AgentManager.ControlFlags)value; }
        }

        /// <summary>
        /// This works out to be the ClientView object associated with this avatar, or it's client connection manager
        /// </summary>
        private readonly IClientAPI m_controllingClient;

        protected PhysicsCharacter m_physicsActor;

        /// <value>
        /// The client controlling this presence
        /// </value>
        public IClientAPI ControllingClient
        {
            get { return m_controllingClient; }
        }

        protected Vector3 m_parentPosition;
        public Vector3 ParentPosition
        {
            get { return m_parentPosition; }
            set { m_parentPosition = value; }
        }

        /// <summary>
        /// Position of this avatar relative to the region the avatar is in
        /// </summary>
        public override sealed Vector3 AbsolutePosition
        {
            get
            {
                return GetAbsolutePosition ();
            }
            set
            {
                PhysicsCharacter actor = m_physicsActor;
                if (actor != null)
                {
                    try
                    {
                        lock (m_scene.SyncRoot)
                        {
                            if (m_physicsActor != null)
                            {
                                m_physicsActor.Position = value;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MainConsole.Instance.Error("[SCENEPRESENCE]: ABSOLUTE POSITION " + e);
                    }
                }

                m_pos = value;
                m_parentPosition = Vector3.Zero;
            }
        }

        public Vector3 GetAbsolutePosition ()
        {
            PhysicsActor actor = m_physicsActor;
            if (actor != null)
                m_pos = actor.Position;
            else
            {
                //return m_pos; 
                // OpenSim Mantis #4063. Obtain the correct position of a seated avatar. In addition
                // to providing the correct position while the avatar is seated, this value will also
                // be used as the location to unsit to.
                //
                // If m_parentID is not 0, assume we are a seated avatar and we should return the
                // position based on the sittarget offset and rotation of the prim we are seated on.
                //
                // Generally, m_pos will contain the position of the avator in the sim unless the avatar
                // is on a sit target. While on a sit target, m_pos will contain the desired offset
                // without the parent rotation applied.
                if (m_parentID != UUID.Zero)
                {
                    ISceneChildEntity part = m_scene.GetSceneObjectPart (m_parentID);
                    if (part != null)
                    {
                        return m_parentPosition + (m_pos * part.GetWorldRotation ());
                    }
                    return m_parentPosition + m_pos;
                }
            }

            return m_pos;
        }

        public Vector3 OffsetPosition
        {
            get { return m_pos; }
            set { m_pos = value; }
        }

        protected Vector3 m_savedVelocity;

        /// <summary>
        /// Current velocity of the avatar.
        /// </summary>
        public override Vector3 Velocity
        {
            get
            {
                PhysicsCharacter actor = m_physicsActor;
                if (actor != null)
                    return actor.Velocity;

                Vector3 vel = m_savedVelocity;
                m_savedVelocity = Vector3.Zero;
                return vel;
            }
            set
            {
                PhysicsCharacter actor = m_physicsActor;
                if (actor != null)
                {
                    try
                    {
                        lock (m_scene.SyncRoot)
                            actor.Velocity = value;
                    }
                    catch (Exception e)
                    {
                        MainConsole.Instance.Error ("[SCENEPRESENCE]: VELOCITY " + e.Message);
                    }
                }
                else
                    m_savedVelocity = value;
            }
        }

        public void ClearSavedVelocity ()
        {
            Velocity = Vector3.Zero;
            m_savedVelocity = Vector3.Zero;
        }

        public override Quaternion Rotation
        {
            get { return m_bodyRot; }
            set { m_bodyRot = value; }
        }

        /// <summary>
        /// If this is true, agent doesn't have a representation in this scene.
        ///    this is an agent 'looking into' this scene from a nearby scene(region)
        ///
        /// if False, this agent has a representation in this scene
        /// </summary>
        protected bool m_isChildAgent = true;

        public bool IsChildAgent
        {
            get { return m_isChildAgent; }
            set 
            { 
                m_isChildAgent = value;
            }
        }

        public ulong RootAgentHandle { get; set; }

        protected UUID m_parentID;

        public UUID ParentID
        {
            get { return m_parentID; }
            set { m_parentID = value; }
        }

        public ISceneViewer SceneViewer
        {
            get { return m_sceneViewer; }
        }

        protected bool m_inTransit;
        protected bool m_mouseLook;
        protected bool m_leftButtonDown;
        protected bool m_isAway = false;
        protected bool m_isBusy = false;

        public bool IsInTransit
        {
            get { return m_inTransit; }
            set { m_inTransit = value; }
        }

        public bool IsAway
        {
            get { return m_isAway; }
            set { m_isAway = value; }
        }

        public bool IsBusy
        {
            get { return m_isBusy; }
            set { m_isBusy = value; }
        }

        public float SpeedModifier
        {
            get 
            {
                if(PhysicsActor != null)
                    return PhysicsActor.SpeedModifier;
                return 1;
            }
            set 
            {
                if(PhysicsActor != null)
                    PhysicsActor.SpeedModifier = value;
            }
        }

        public bool ForceFly
        {
            get { return m_forceFly; }
            set { m_forceFly = value; }
        }

        public bool FlyDisabled
        {
            get { return m_flyDisabled; }
            set { m_flyDisabled = value; }
        }

        #endregion

        #region Constructor(s)
        
        public ScenePresence()
        {
            IsTainted = false;
            SitGround = false;
            CurrentParcel = null;
            m_sendCourseLocationsMethod = SendCoarseLocationsDefault;
        }

        public ScenePresence(IClientAPI client, IScene world)
            : this()
        {
            m_controllingClient = client;
            m_firstname = m_controllingClient.FirstName;
            m_lastname = m_controllingClient.LastName;
            m_name = m_controllingClient.Name;
            m_scene = world;
            m_uuid = client.AgentId;
            m_localId = m_scene.SceneGraph.AllocateLocalId();

            CreateSceneViewer();

            m_animator = new Animator(this);

            UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.AllScopeIDs, m_uuid);

            if (account != null)
            {
                m_userLevel = account.UserLevel;
                client.ScopeID = account.ScopeID;
                client.AllScopeIDs = account.AllScopeIDs;
            }
            else
                client.ScopeID = m_scene.RegionInfo.ScopeID;

            AbsolutePosition = posLastSignificantMove = m_CameraCenter = m_controllingClient.StartPos;

            // This won't send anything, as we are still a child here...
            //Animator.TrySetMovementAnimation("STAND"); 

            // we created a new ScenePresence (a new child agent) in a fresh region.
            // Request info about all the (root) agents in this region
            // Note: This won't send data *to* other clients in that region (children don't send)
            //SendInitialFullUpdateToAllClients();
            //SendOtherAgentsAvatarDataToMe();
            //Comment this out for now, just to see what happens
            //SendOtherAgentsAppearanceToMe();

            RegisterToEvents();
            SetDirectionVectors();
        }

        private void CreateSceneViewer()
        {
            m_sceneViewer = new SceneViewer(this);
        }

        public virtual void RegisterToEvents()
        {
            m_controllingClient.OnCompleteMovementToRegion += CompleteMovement;
            m_controllingClient.OnAgentUpdate += HandleAgentUpdate;
            m_controllingClient.OnAgentRequestSit += HandleAgentRequestSit;
            m_controllingClient.OnAgentSit += HandleAgentSit;
            m_controllingClient.OnSetAlwaysRun += HandleSetAlwaysRun;
            m_controllingClient.OnStartAnim += HandleStartAnim;
            m_controllingClient.OnStopAnim += HandleStopAnim;
            m_controllingClient.OnAutoPilotGo += DoAutoPilot;
            m_controllingClient.AddGenericPacketHandler("autopilot", DoMoveToPosition);
            m_controllingClient.OnRegionHandleRequest += RegionHandleRequest;
            m_controllingClient.OnNameFromUUIDRequest += HandleUUIDNameRequest;
        }

        private void SetDirectionVectors()
        {
            Dir_Vectors[0] = Vector3.UnitX; //FORWARD
            Dir_Vectors[1] = -Vector3.UnitX; //BACK
            Dir_Vectors[2] = Vector3.UnitY; //LEFT
            Dir_Vectors[3] = -Vector3.UnitY; //RIGHT
            Dir_Vectors[4] = Vector3.UnitZ; //UP
            Dir_Vectors[5] = -Vector3.UnitZ; //DOWN
            Dir_Vectors[6] = Vector3.UnitX*2; //FORWARD_NUDGE
            Dir_Vectors[7] = -Vector3.UnitX; //BACK_NUDGE
            Dir_Vectors[8] = new Vector3(0, 4, 0); //LEFT Nudge
            Dir_Vectors[9] = new Vector3(0, -4, 0); //RIGHT Nudge
            Dir_Vectors[10] = new Vector3(0f, 0f, -0.5f); //DOWN_Nudge
            Dir_Vectors[11] = new Vector3(0f, 0f, 0.5f); //UP_Nudge
        }

        private Vector3[] GetWalkDirectionVectors()
        {
            Vector3[] vector = new Vector3[12];
            vector[0] = new Vector3(m_CameraUpAxis.Z, 0f, -m_CameraAtAxis.Z); //FORWARD
            vector[1] = new Vector3(-m_CameraUpAxis.Z, 0f, m_CameraAtAxis.Z); //BACK
            vector[2] = Vector3.UnitY; //LEFT
            vector[3] = -Vector3.UnitY; //RIGHT
            vector[4] = new Vector3(m_CameraAtAxis.Z, 0f, m_CameraUpAxis.Z); //UP
            vector[5] = new Vector3(-m_CameraAtAxis.Z, 0f, -m_CameraUpAxis.Z); //DOWN
            vector[8] = new Vector3(-m_CameraAtAxis.Z, 0f, -m_CameraUpAxis.Z); //DOWN_Nudge
            vector[6] = (new Vector3(m_CameraUpAxis.Z, 0f, -m_CameraAtAxis.Z) * 2); //FORWARD Nudge
            vector[7] = new Vector3(-m_CameraUpAxis.Z, 0f, m_CameraAtAxis.Z); //BACK Nudge
            vector[8] = new Vector3(0, 2, 0); //LEFT Nudge
            vector[9] = new Vector3(0, -2, 0); //RIGHT Nudge
            vector[10] = new Vector3(m_CameraAtAxis.Z, 0f, m_CameraUpAxis.Z); //DOWN_Nudge
            vector[11] = new Vector3(m_CameraAtAxis.Z, 0f, -m_CameraUpAxis.Z); //UP_Nudge
            return vector;
        }

        #endregion

        #region Status Methods

        /// <summary>
        /// This turns a child agent, into a root agent
        /// This is called when an agent teleports into a region, or if an
        /// agent crosses into this region from a neighbor over the border
        /// </summary>
        public virtual void MakeRootAgent (Vector3 pos, bool isFlying, bool makePhysicalActor)
        {
            AbsolutePosition = pos;

            int xmult = m_savedVelocity.X > 0 ? 1 : -1;
            int ymult = m_savedVelocity.Y > 0 ? 1 : -1;
            Vector3 look = new Vector3 (0.99f * xmult, 0.99f * ymult, 0);

            IsChildAgent = false;
            RootAgentHandle = Scene.RegionInfo.RegionHandle;

            //Do this and SendInitialData FIRST before MakeRootAgent to try to get the updates to the client out so that appearance loads better
            m_controllingClient.MoveAgentIntoRegion (Scene.RegionInfo, AbsolutePosition, look);

            MainConsole.Instance.DebugFormat(
                "[SCENE]: Upgrading child to root agent for {0} in {1}",
                Name, m_scene.RegionInfo.RegionName);

            // On the next prim update, all objects will be sent
            //
            m_sceneViewer.Reset ();

            if(makePhysicalActor)
            {
                AddToPhysicalScene(isFlying, false);
                //m_physicsActor.Position += m_savedVelocity * 0.25f;
                m_physicsActor.Velocity = m_savedVelocity * 0.25f;

                if(m_forceFly)
                    m_physicsActor.Flying = true;
                else if(m_flyDisabled)
                    m_physicsActor.Flying = false;
            }

            m_savedVelocity = Vector3.Zero;
            SuccessfulTransit();

            // Don't send an animation pack here, since on a region crossing this will sometimes cause a flying 
            // avatar to return to the standing position in mid-air.  On login it looks like this is being sent
            // elsewhere anyway
            // Animator.SendAnimPack();

            SendScriptEventToAllAttachments(Changed.TELEPORT);

            m_scene.EventManager.TriggerOnMakeRootAgent(this);

            SuccessfullyMadeRootAgent = true;

            //Tell the grid that we successfully got here

            AgentCircuitData agent = ControllingClient.RequestClientInfo ();
            agent.startpos = AbsolutePosition;
            IAvatarAppearanceModule appearance = RequestModuleInterface<IAvatarAppearanceModule>();
            if (appearance != null)
            {
                if(makePhysicalActor)//Someone else will deal with this
                {
                    //Send updates to everyone about us
                    foreach(IScenePresence sp in m_scene.GetScenePresences())
                    {
                        sp.SceneViewer.QueuePresenceForFullUpdate(this, true);
                    }
                }
                if(appearance.Appearance != null)
                    agent.Appearance = appearance.Appearance;
            }

            ISyncMessagePosterService syncPoster = Scene.RequestModuleInterface<ISyncMessagePosterService> ();
            if (syncPoster != null)
                syncPoster.Post (SyncMessageHelper.ArrivedAtDestination (UUID, (int)DrawDistance, agent, Scene.RegionInfo.RegionHandle), Scene.RegionInfo.RegionHandle);
        }

        /// <summary>
        /// This turns a root agent into a child agent
        /// when an agent departs this region for a neighbor, this gets called.
        ///
        /// It doesn't get called for a teleport.  Reason being, an agent that
        /// teleports out may not end up anywhere near this region
        /// </summary>
        public virtual void MakeChildAgent(GridRegion destination)
        {
            SuccessfullyMadeRootAgent = false;
            SuccessfulTransit ();
            // It looks like m_animator is set to null somewhere, and MakeChild
            // is called after that. Probably in aborted teleports.
            if (m_animator == null)
                m_animator = new Animator(this);
            else
                Animator.ResetAnimations();

            MainConsole.Instance.DebugFormat(
                 "[SCENEPRESENCE]: Downgrading root agent {0}, {1} to a child agent in {2}",
                 Name, UUID, m_scene.RegionInfo.RegionName);

            IsChildAgent = true;
            RootAgentHandle = destination.RegionHandle;
            RemoveFromPhysicalScene ();
            m_sceneViewer.Reset ();

            SendScriptEventToAllAttachments(Changed.TELEPORT);
            m_scene.EventManager.TriggerOnMakeChildAgent(this, destination);

            Reset();
        }

        public virtual void SetAgentLeaving(GridRegion destindation)
        {
            m_scene.EventManager.TriggerOnSetAgentLeaving(this, destindation);
        }

        public virtual void AgentFailedToLeave()
        {
            m_scene.EventManager.TriggerOnAgentFailedToLeave(this);
        }

        /// <summary>
        /// Removes physics plugin scene representation of this agent if it exists.
        /// </summary>
        public virtual void RemoveFromPhysicalScene ()
        {
            try
            {
                if (PhysicsActor != null)
                {
                    if (OnRemovePhysics != null)
                        OnRemovePhysics();
                    if (m_physicsActor != null)
                        m_scene.PhysicsScene.RemoveAvatar(m_physicsActor);
                    if (m_physicsActor != null)
                        m_physicsActor.OnCollisionUpdate -= PhysicsCollisionUpdate;
                    if (m_physicsActor != null)
                        m_physicsActor.OnRequestTerseUpdate -= SendPhysicsTerseUpdateToAllClients;
                    if (m_physicsActor != null)
                        m_physicsActor.OnSignificantMovement -= CheckForSignificantMovement;
                    if (m_physicsActor != null)
                        m_physicsActor.OnPositionAndVelocityUpdate -= PhysicsUpdatePosAndVelocity;
                    if (m_physicsActor != null)
                        m_physicsActor.OnCheckForRegionCrossing -= CheckForBorderCrossing;
                    if (m_physicsActor != null)
                        m_physicsActor.OnOutOfBounds -= OutOfBoundsCall;
                    if (m_physicsActor != null)
                        m_scene.PhysicsScene.RemoveAvatar(PhysicsActor);
                    m_physicsActor = null;
                }
            }
            catch { }
        }

        public virtual void Teleport (Vector3 pos)
        {
            bool isFlying = false;
            if (m_physicsActor != null)
                isFlying = m_physicsActor.Flying;
            
            RemoveFromPhysicalScene();
            Velocity = Vector3.Zero;
            AbsolutePosition = pos;
            AddToPhysicalScene(isFlying, true);

            SendScriptEventToAllAttachments(Changed.TELEPORT);
            SendTerseUpdateToAllClients();
        }

        private void SendScriptEventToAllAttachments(Changed c)
        {
            IAttachmentsModule attMod = Scene.RequestModuleInterface<IAttachmentsModule>();
            if (attMod != null)
                attMod.SendScriptEventToAttachments(UUID, "changed", new Object[] { c });
        }

        public virtual void TeleportWithMomentum (Vector3 pos)
        {
            bool isFlying = false;
            if (m_physicsActor != null)
                isFlying = m_physicsActor.Flying;

            RemoveFromPhysicalScene();
            AbsolutePosition = pos;
            AddToPhysicalScene(isFlying, true);

            SendScriptEventToAllAttachments(Changed.TELEPORT);
            SendTerseUpdateToAllClients();
        }

        public virtual void StopFlying ()
        {
            ControllingClient.StopFlying(this);
        }

        #endregion

        #region Event Handlers

        public void HandleUUIDNameRequest(UUID uuid, IClientAPI remote_client)
        {
            UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.AllScopeIDs, uuid);
            if (account != null)
            {
                remote_client.SendNameReply (uuid, account.FirstName, account.LastName);
            }
            else
            {
                IScenePresence presence;
                if ((presence = Scene.GetScenePresence (uuid)) != null)
                {
                    remote_client.SendNameReply (uuid, presence.Firstname, presence.Lastname);
                }
            }
        }

        public void RegionHandleRequest(IClientAPI client, UUID regionID)
        {
            ulong handle = 0;
            if (regionID == m_scene.RegionInfo.RegionID)
                handle = m_scene.RegionInfo.RegionHandle;
            else
            {
                GridRegion r = m_scene.GridService.GetRegionByUUID(client.AllScopeIDs, regionID);
                if (r != null)
                    handle = r.RegionHandle;
            }

            if (handle != 0)
                client.SendRegionHandle(regionID, handle);
        }

        /// <summary>
        /// Complete Avatar's movement into the region.
        /// This is called upon a very important packet sent from the client,
        /// so it's client-controlled. Never call this method directly.
        /// </summary>
        private void CompleteMovement(IClientAPI client)
        {
            //MainConsole.Instance.Debug("[SCENE PRESENCE]: CompleteMovement for " + Name + " in " + m_regionInfo.RegionName);

            string reason = "";
            Vector3 pos;
            //Get a good position and make sure that we exist in the grid
            AgentCircuitData agent = m_scene.AuthenticateHandler.GetAgentCircuitData (UUID);

            if (agent == null || !Scene.Permissions.AllowedIncomingTeleport (UUID, AbsolutePosition, agent.teleportFlags, out pos, out reason))
            {
                MainConsole.Instance.Error("[ScenePresence]: Error in MakeRootAgent! Could not authorize agent " + Name +
                    ", reason: " + reason);
                return;
            }

            bool m_flying = ((m_AgentControlFlags & AgentManager.ControlFlags.AGENT_CONTROL_FLY) != 0);
            MakeRootAgent(pos, m_flying, m_objectToSitOn == null);
            Animator.NeedsAnimationResent = true;

            if(m_objectToSitOn != null)
            {
                SitOnObjectAfterCrossing(m_objectToSitOn);
                m_objectToSitOn = null;
            }
        }

        /// <summary>
        /// Callback for the Camera view block check.  Gets called with the results of the camera view block test
        /// hitYN is true when there's something in the way.
        /// </summary>
        /// <param name="hitYN"></param>
        /// <param name="collisionPoint"></param>
        /// <param name="localid"></param>
        /// <param name="distance"></param>
        /// <param name="pNormal"></param>
        public void RayCastCameraCallback(bool hitYN, Vector3 collisionPoint, uint localid, float distance, Vector3 pNormal)
        {
            if (m_followCamAuto)
            {
                if (hitYN)
                {
                    CameraConstraintActive = true;
                    //MainConsole.Instance.DebugFormat("[RAYCASTRESULT]: {0}, {1}, {2}, {3}", hitYN, collisionPoint, localid, distance);

                    Vector3 normal = Vector3.Normalize(new Vector3(0f, 0f, collisionPoint.Z) - collisionPoint);
                    ControllingClient.SendCameraConstraint(new Vector4(normal.X, normal.Y, normal.Z, -1 * Vector3.Distance(new Vector3(0, 0, collisionPoint.Z), collisionPoint)));
                }
                else
                {
                    if (CameraConstraintActive)
                    {
                        ControllingClient.SendCameraConstraint(new Vector4(0f, 0.5f, 0.9f, -3000f));
                        CameraConstraintActive = false;
                    }
                }
            }
        }

        /// <summary>
        /// This is the event handler for client movement. If a client is moving, this event is triggering.
        /// </summary>
        public virtual void HandleAgentUpdate (IClientAPI remoteClient, AgentUpdateArgs agentData)
        {
            m_perfMonMS = Util.EnvironmentTickCount();

            ++m_movementUpdateCount;
            if (m_movementUpdateCount < 1)
                m_movementUpdateCount = 1;

            #region Sanity Checking

            // This is irritating.  Really.
            if (!AbsolutePosition.IsFinite())
            {
                OutOfBoundsCall(Vector3.Zero);
                return;
            }

            #endregion Sanity Checking

            #region Inputs

            if (Frozen)
                return; //Do nothing, just end

            AgentManager.ControlFlags flags = (AgentManager.ControlFlags)agentData.ControlFlags;
            Quaternion bodyRotation = agentData.BodyRotation;

            //Check to see whether ray casting needs done
            // We multiply by 10 so that we don't trigger it when the camera moves slightly (as its 2 meter change)
            if (Util.GetFlatDistanceTo(agentData.CameraCenter, m_lastCameraCenter) > SIGNIFICANT_MOVEMENT * 10)
            {
                m_lastCameraCenter = agentData.CameraCenter;
                Scene.AuroraEventManager.FireGenericEventHandler("SignficantCameraMovement", this);
            }

            // Camera location in world.  We'll need to raytrace
            // from this location from time to time.
            m_CameraCenter = agentData.CameraCenter;

            // Use these three vectors to figure out what the agent is looking at
            // Convert it to a Matrix and/or Quaternion
            m_CameraAtAxis = agentData.CameraAtAxis;
            m_CameraLeftAxis = agentData.CameraLeftAxis;
            m_CameraUpAxis = agentData.CameraUpAxis;
            // The Agent's Draw distance setting
            DrawDistance = agentData.Far;

            // Check if Client has camera in 'follow cam' or 'build' mode.
            Vector3 camdif = (Vector3.One * m_bodyRot - Vector3.One * CameraRotation);

            m_followCamAuto = ((m_CameraUpAxis.Z > 0.959f && m_CameraUpAxis.Z < 0.98f)
                               && (Math.Abs(camdif.X) < 0.4f && Math.Abs(camdif.Y) < 0.4f));

            m_mouseLook = (flags & AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) != 0;
            m_leftButtonDown = (flags & AgentManager.ControlFlags.AGENT_CONTROL_LBUTTON_DOWN) != 0;
            m_isAway = (flags & AgentManager.ControlFlags.AGENT_CONTROL_AWAY) != 0;
            #endregion Inputs

            if ((flags & AgentManager.ControlFlags.AGENT_CONTROL_STAND_UP) != 0)
            {
                StandUp();
            }

            //MainConsole.Instance.DebugFormat("[FollowCam]: {0}", m_followCamAuto);
            // Raycast from the avatar's head to the camera to see if there's anything blocking the view
            if ((m_movementUpdateCount % NumMovementsBetweenRayCast) == 0 && m_scene.PhysicsScene.SupportsRayCast())
            {
                if (m_followCamAuto)
                {
                    Vector3 posAdjusted = m_pos + HEAD_ADJUSTMENT;
                    m_scene.PhysicsScene.RaycastWorld(m_pos, Vector3.Normalize(m_CameraCenter - posAdjusted), Vector3.Distance(m_CameraCenter, posAdjusted) + 0.3f, RayCastCameraCallback);
                }
            }
            if (!m_CameraCenter.IsFinite())
            {
                m_CameraCenter = new Vector3(128, 128, 128);
            }

            IScriptControllerModule m = RequestModuleInterface<IScriptControllerModule> ();
            if (m != null) //Tell any scripts about it
                m.OnNewMovement (ref flags);

            if (m_autopilotMoving)
                CheckAtSitTarget();

            // In the future, these values might need to go global.
            // Here's where you get them.
            if (!SitGround)
                SitGround = (flags & AgentManager.ControlFlags.AGENT_CONTROL_SIT_ON_GROUND) != 0;
            m_AgentControlFlags = flags;
            m_headrotation = agentData.HeadRotation;
            m_state = agentData.State;

            PhysicsCharacter actor = PhysicsActor;
            if (actor == null)
            {
                //This happens while sitting, don't spam it
                //MainConsole.Instance.Debug("Null physical actor in AgentUpdate in " + m_scene.RegionInfo.RegionName);
                return;
            }
            
            bool update_movementflag = false;
            bool update_rotation = false;

            if (AllowMovement && !SitGround && !Frozen)
            {
                if (FallenStandUp)
                {
                    //Poke the animator a bit
                    Animator.UpdateMovementAnimations(false);
                    m_bodyRot = bodyRotation;
                    AddNewMovement (Vector3.Zero, bodyRotation);
                    return;
                }
                if (agentData.UseClientAgentPosition)
                {
                    m_moveToPositionInProgress = (agentData.ClientAgentPosition - AbsolutePosition).Length() > 0.2f;
                    m_moveToPositionTarget = agentData.ClientAgentPosition;
                }

                int i = 0;
                
                bool DCFlagKeyPressed = false;
                Vector3 agent_control_v3 = Vector3.Zero;
                Quaternion q = bodyRotation;

                bool oldflying = PhysicsActor.Flying;

                if (m_forceFly)
                    actor.Flying = true;
                else if (m_flyDisabled)
                    actor.Flying = false;
                else if(actor.Flying != ((flags & AgentManager.ControlFlags.AGENT_CONTROL_FLY) != 0))
                    actor.Flying = ((flags & AgentManager.ControlFlags.AGENT_CONTROL_FLY) != 0);

                if (actor.Flying != oldflying)
                    update_movementflag = true;

                if (q != m_bodyRot)
                {
                    Quaternion delta = Quaternion.Inverse(m_bodyRot) * q;
                    m_bodyRot = q;
                    if (!(Math.Abs(delta.X) < 1e-5f && Math.Abs(delta.Y) < 1e-5f && Math.Abs(delta.Z) < 1e-5f))
                        update_rotation = true;
                }

                if (m_parentID == UUID.Zero)
                {
                    bool bAllowUpdateMoveToPosition = false;
                    bool bResetMoveToPosition = false;

                    Vector3[] dirVectors;

                    // use camera up angle when in mouselook and not flying or when holding the left mouse button down and not flying
                    // this prevents 'jumping' in inappropriate situations.
                    if ((m_mouseLook && !m_physicsActor.Flying) || (m_leftButtonDown && !m_physicsActor.Flying))
                        dirVectors = GetWalkDirectionVectors();
                    else
                        dirVectors = Dir_Vectors;

                    // The fact that m_movementflag is a byte needs to be fixed
                    // it really should be a uint
                    const uint nudgehack = 250;
                    //Do these two like this to block out all others because it will slow it down
                    if ((flags & AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_LEFT_POS) == AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_LEFT_POS)
                    {
                        bResetMoveToPosition = true;
                        DCFlagKeyPressed = true;
                        agent_control_v3 += dirVectors[8];
                    }
                    else if ((flags & AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_LEFT_NEG) == AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_LEFT_NEG)
                    {
                        bResetMoveToPosition = true;
                        DCFlagKeyPressed = true;
                        agent_control_v3 += dirVectors[9];
                    }
                    else
                    {
                        foreach (Dir_ControlFlags DCF in DIR_CONTROL_FLAGS)
                        {
                            if (((uint)flags & (uint)DCF) != 0)
                            {
                                bResetMoveToPosition = true;
                                DCFlagKeyPressed = true;
                                agent_control_v3 += dirVectors[i];
                                //MainConsole.Instance.DebugFormat("[Motion]: {0}, {1}",i, dirVectors[i]);

                                if ((m_movementflag & (uint)DCF) == 0)
                                {
                                    if (DCF == Dir_ControlFlags.DIR_CONTROL_FLAG_FORWARD_NUDGE || DCF == Dir_ControlFlags.DIR_CONTROL_FLAG_BACKWARD_NUDGE)
                                    {
                                        //                                        m_movementflag |= (byte)nudgehack;
                                        m_movementflag |= nudgehack;
                                    }
                                    m_movementflag += (uint)DCF;
                                    update_movementflag = true;
                                }
                            }
                            else
                            {
                                if ((m_movementflag & (uint)DCF) != 0 ||
                                    ((DCF == Dir_ControlFlags.DIR_CONTROL_FLAG_FORWARD_NUDGE || DCF == Dir_ControlFlags.DIR_CONTROL_FLAG_BACKWARD_NUDGE)
                                    && ((m_movementflag & nudgehack) == nudgehack))
                                    ) // This or is for Nudge forward
                                {
                                    m_movementflag -= ((uint)DCF);

                                    update_movementflag = true;
                                    /*
                                        if ((DCF == Dir_ControlFlags.DIR_CONTROL_FLAG_FORWARD_NUDGE || DCF == Dir_ControlFlags.DIR_CONTROL_FLAG_BACKWARD_NUDGE)
                                        && ((m_movementflag & (byte)nudgehack) == nudgehack))
                                        {
                                            MainConsole.Instance.Debug("Removed Hack flag");
                                        }
                                    */
                                }
                                else
                                {
                                    bAllowUpdateMoveToPosition = true;
                                }
                            }
                            i++;
                        }
                    }

                    //Paupaw:Do Proper PID for Autopilot here
                    if (bResetMoveToPosition)
                    {
                        m_moveToPositionTarget = Vector3.Zero;
                        m_moveToPositionInProgress = false;
                        update_movementflag = true;
                        bAllowUpdateMoveToPosition = false;
                    }

                    if (bAllowUpdateMoveToPosition && (m_moveToPositionInProgress && !m_autopilotMoving))
                    {
                        //Check the error term of the current position in relation to the target position
                        if (Util.GetFlatDistanceTo(AbsolutePosition, m_moveToPositionTarget) <= 0.5f)
                        {
                            // we are close enough to the target
                            m_moveToPositionTarget = Vector3.Zero;
                            m_moveToPositionInProgress = false;
                            update_movementflag = true;
                        }
                        else
                        {
                            try
                            {
                                // move avatar in 2D at one meter/second towards target, in avatar coordinate frame.
                                // This movement vector gets added to the velocity through AddNewMovement().
                                // Theoretically we might need a more complex PID approach here if other 
                                // unknown forces are acting on the avatar and we need to adaptively respond
                                // to such forces, but the following simple approach seems to works fine.
                            Vector3 LocalVectorToTarget3D=
                                                         (m_moveToPositionTarget - AbsolutePosition) // vector from cur. pos to target in global coords
                                //                                    * Matrix4.CreateFromQuaternion(Quaternion.Inverse(bodyRotation)); // change to avatar coords
                                                        * Quaternion.Inverse(bodyRotation); // mult by matix is faster but with creation, use *quarternion
                                // Ignore z component of vector
                                Vector3 LocalVectorToTarget2D;
                                LocalVectorToTarget2D.X = LocalVectorToTarget3D.X;
                                LocalVectorToTarget2D.Y = LocalVectorToTarget3D.Y;
                                LocalVectorToTarget2D.Z = 0f;

                                agent_control_v3 += LocalVectorToTarget2D;

                                // update avatar movement flags. the avatar coordinate system is as follows:
                                //
                                //                        +X (forward)
                                //
                                //                        ^
                                //                        |
                                //                        |
                                //                        |
                                //                        |
                                //     (left) +Y <--------o--------> -Y
                                //                       avatar
                                //                        |
                                //                        |
                                //                        |
                                //                        |
                                //                        v
                                //                        -X
                                //

                                // based on the above avatar coordinate system, classify the movement into 
                                // one of left/right/back/forward.
                                if (LocalVectorToTarget2D.Y > 0)//MoveLeft
                                {
                                    m_movementflag += (uint)Dir_ControlFlags.DIR_CONTROL_FLAG_LEFT;
                                    //AgentControlFlags
                                    AgentControlFlags |= (uint)Dir_ControlFlags.DIR_CONTROL_FLAG_LEFT;
                                    update_movementflag = true;
                                }
                                else if (LocalVectorToTarget2D.Y < 0) //MoveRight
                                {
                                    m_movementflag += (uint)Dir_ControlFlags.DIR_CONTROL_FLAG_RIGHT;
                                    AgentControlFlags |= (uint)Dir_ControlFlags.DIR_CONTROL_FLAG_RIGHT;
                                    update_movementflag = true;
                                }
                                if (LocalVectorToTarget2D.X < 0) //MoveBack
                                {
                                    m_movementflag += (uint)Dir_ControlFlags.DIR_CONTROL_FLAG_BACK;
                                    AgentControlFlags |= (uint)Dir_ControlFlags.DIR_CONTROL_FLAG_BACK;
                                    update_movementflag = true;
                                }
                                else if (LocalVectorToTarget2D.X > 0) //Move Forward
                                {
                                    m_movementflag += (uint)Dir_ControlFlags.DIR_CONTROL_FLAG_FORWARD;
                                    AgentControlFlags |= (uint)Dir_ControlFlags.DIR_CONTROL_FLAG_FORWARD;
                                    update_movementflag = true;
                                }
                            }
                            catch (Exception e)
                            {
                                //Avoid system crash, can be slower but...
                                MainConsole.Instance.DebugFormat("Crash! {0}", e);
                            }
                        }
                    }
                }

                // Cause the avatar to stop flying if it's colliding
                // with something with the down arrow pressed.

                // Only do this if we're flying
                if (m_physicsActor != null && m_physicsActor.Flying && !m_forceFly)
                {
                    // Landing detection code

                    // Are the landing controls requirements filled?
                    bool controlland = (((flags & AgentManager.ControlFlags.AGENT_CONTROL_UP_NEG) != 0) ||
                                        ((flags & AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_UP_NEG) != 0));

                    // Are the collision requirements fulfilled?
                    bool colliding = (m_physicsActor.IsColliding == true);

                    if (m_physicsActor.Flying && colliding && controlland)
                    {
                        // nesting this check because LengthSquared() is expensive and we don't 
                        // want to do it every step when flying.
                        // then call it in the if...
                        //The == Zero and Z > 0.1 are to stop people from flying and then falling down because the physics engine hasn't calculted the push yet
                        if (Velocity != Vector3.Zero && Math.Abs(Velocity.Z) > 0.05 && (Velocity.LengthSquared() <= LAND_VELOCITYMAG_MAX))
                        {
                            StopFlying ();
                            SendTerseUpdateToAllClients ();
                        }
                    }
                }

                // If the agent update does move the avatar, then calculate the force ready for the velocity update,
                // which occurs later in the main scene loop
                if (update_movementflag || (update_rotation && DCFlagKeyPressed))
                {
                    //                    MainConsole.Instance.DebugFormat("{0} {1}", update_movementflag, (update_rotation && DCFlagKeyPressed));
                    //                    MainConsole.Instance.DebugFormat(
                    //                        "In {0} adding velocity to {1} of {2}", m_scene.RegionInfo.RegionName, Name, agent_control_v3);

                    AddNewMovement(agent_control_v3, q);
                }
            }

            if ((update_movementflag || update_rotation) && (m_parentID == UUID.Zero))
                 Animator.UpdateMovementAnimations(true);
            

            IAgentUpdateMonitor reporter = (IAgentUpdateMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), MonitorModuleHelper.AgentUpdateCount);
            if (reporter != null)
                reporter.AddAgentTime(Util.EnvironmentTickCountSubtract(m_perfMonMS));
        }

        public void DoAutoPilot(uint not_used, Vector3 Pos, IClientAPI remote_client)
        {
            m_autopilotMoving = true;
            m_autoPilotTarget = Pos;
            m_sitAtAutoTarget = false;
            PrimitiveBaseShape proxy = PrimitiveBaseShape.Default;
            //proxy.PCode = (byte)PCode.ParticleSystem;

            proxyObjectGroup = new SceneObjectGroup(UUID, Pos, Rotation, proxy, "", m_scene);
            proxyObjectGroup.AttachToScene(m_scene);

            // Commented out this code since it could never have executed, but might still be informative.
            //            if (proxyObjectGroup != null)
            //            {
            proxyObjectGroup.ScheduleGroupUpdate (PrimUpdateFlags.ForcedFullUpdate);
            remote_client.SendSitResponse(proxyObjectGroup.UUID, Vector3.Zero, Quaternion.Identity, true, Vector3.Zero, Vector3.Zero, false);
            IBackupModule backup = m_scene.RequestModuleInterface<IBackupModule>();
            if (backup != null)
                backup.DeleteSceneObjects(new[] { proxyObjectGroup }, true, true);
            //            }
            //            else
            //            {
            //                m_autopilotMoving = false;
            //                m_autoPilotTarget = Vector3.Zero;
            //                ControllingClient.SendAlertMessage("Autopilot cancelled");
            //            }
        }

        public void DoMoveToPosition(Object sender, string method, List<String> args)
        {
            try
            {
                float locx = 0f;
                float locy = 0f;
                float locz = 0f;
                try
                {
                    uint regionX = 0;
                    uint regionY = 0;
                    Utils.LongToUInts(Scene.RegionInfo.RegionHandle, out regionX, out regionY);
                    locx = Convert.ToSingle(args[0]) - regionX;
                    locy = Convert.ToSingle(args[1]) - regionY;
                    locz = Convert.ToSingle(args[2]);
                }
                catch (InvalidCastException)
                {
                    MainConsole.Instance.Error("[CLIENT]: Invalid autopilot request");
                    return;
                }
                m_moveToPositionInProgress = true;
                m_moveToPositionTarget = new Vector3(locx, locy, locz);
                //MainConsole.Instance.Warn("Moving to " + m_moveToPositionTarget);
            }
            catch (Exception ex)
            {
                //Why did I get this error?
               MainConsole.Instance.Error("[SCENEPRESENCE]: DoMoveToPosition" + ex);
            }
        }

        private void CheckAtSitTarget()
        {
            //MainConsole.Instance.Debug("[AUTOPILOT]: " + Util.GetDistanceTo(AbsolutePosition, m_autoPilotTarget).ToString());
            if (Util.GetDistanceTo(AbsolutePosition, m_autoPilotTarget) <= 1.5)
            {
                if (m_sitAtAutoTarget)
                {
                    ISceneChildEntity part = m_scene.GetSceneObjectPart (m_requestedSitTargetUUID);
                    if (part != null)
                    {
                        m_autoPilotTarget = Vector3.Zero;
                        m_autopilotMoving = false;
                        SendSitResponse(ControllingClient, m_requestedSitTargetUUID, Vector3.Zero, Quaternion.Identity);
                    }
                    m_requestedSitTargetUUID = UUID.Zero;
                    m_sitting = false;
                }
            }
        }

        /// <summary>
        /// Perform the logic necessary to stand the avatar up.  This method also executes
        /// the stand animation.
        /// </summary>
        public virtual void StandUp ()
        {
            SitGround = false;

            if (m_parentID != UUID.Zero)
            {
                ISceneChildEntity part = m_scene.GetSceneObjectPart (m_parentID);
                if (part != null)
                {
                    //Block movement of vehicles for a bit until after the changed event has fired
                    if(part.PhysActor != null)
                        part.PhysActor.Selected = true;
                    IScriptControllerModule m = RequestModuleInterface<IScriptControllerModule> ();
                    if (m != null)
                        m.RemoveAllScriptControllers (part);
                    
                    // Reset sit target.
                    if (part.SitTargetAvatar.Contains(UUID))
                        part.RemoveAvatarOnSitTarget(UUID);

                    m_parentPosition = part.GetWorldPosition();
                    Vector3 MovePos = new Vector3 {X = 1};
                    //TODO: Make this configurable
                    MovePos *= Rotation;
                    m_parentPosition += MovePos;
                    ControllingClient.SendClearFollowCamProperties(part.ParentUUID);
                    if(part.PhysActor != null)
                        part.PhysActor.Selected = false;
                }
                if(m_physicsActor == null)
                    AddToPhysicalScene(false, false);
                m_pos += m_parentPosition + new Vector3(0.0f, 0.0f, 2.0f*m_sitAvatarHeight);
                m_parentPosition = Vector3.Zero;
            }

            if(m_physicsActor == null)
                AddToPhysicalScene(false, false);

            m_parentID = UUID.Zero;
            m_requestedSitTargetUUID = UUID.Zero;
            m_sitting = false;
            foreach(IScenePresence sp in m_scene.GetScenePresences())
            {
                if(sp.SceneViewer.Culler.ShowEntityToClient(sp, this, Scene))
                    sp.ControllingClient.SendAvatarDataImmediate(this);
            }

            Animator.TrySetMovementAnimation("STAND");
        }

        #region Sit code

        private ISceneChildEntity FindNextAvailableSitTarget(UUID targetID)
        {
            ISceneChildEntity targetPart = m_scene.GetSceneObjectPart(targetID);
            if (targetPart == null)
                return null;

            // If the primitive the player clicked on has a sit target and that sit target is not full, that sit target is used.
            // If the primitive the player clicked on has no sit target, and one or more other linked objects have sit targets that are not full, the sit target of the object with the lowest link number will be used.

            // Get our own copy of the part array, and sort into the order we want to test
            ISceneChildEntity[] partArray = targetPart.ParentEntity.ChildrenEntities().ToArray();
            Array.Sort(partArray, delegate(ISceneChildEntity p1, ISceneChildEntity p2)
            {
                // we want the originally selected part first, then the rest in link order -- so make the selected part link num (-1)
                int linkNum1 = p1 == targetPart ? -1 : p1.LinkNum;
                int linkNum2 = p2 == targetPart ? -1 : p2.LinkNum;
                return linkNum1 - linkNum2;
            }
                );

            //look for prims with explicit sit targets that are available
            foreach (ISceneChildEntity part in partArray)
            {
                // Is a sit target available?
                Vector3 avSitOffSet = part.SitTargetPosition;
                Quaternion avSitOrientation = part.SitTargetOrientation;

                bool SitTargetisSet =
                    (!(avSitOffSet.X == 0f && avSitOffSet.Y == 0f && avSitOffSet.Z == 0f && avSitOrientation.W == 1f &&
                       avSitOrientation.X == 0f && avSitOrientation.Y == 0f && avSitOrientation.Z == 0f));

                if (SitTargetisSet)
                {
                    //switch the target to this prim
                    return part;
                }
            }

            // no explicit sit target found - use original target
            return targetPart;
        }

        private ISceneChildEntity FindNextAvailableSitTarget(UUID targetID, UUID notID)
        {
            ISceneChildEntity targetPart = m_scene.GetSceneObjectPart(targetID);
            if (targetPart == null)
                return null;

            // If the primitive the player clicked on has a sit target and that sit target is not full, that sit target is used.
            // If the primitive the player clicked on has no sit target, and one or more other linked objects have sit targets that are not full, the sit target of the object with the lowest link number will be used.

            // Get our own copy of the part array, and sort into the order we want to test
            ISceneChildEntity[] partArray = targetPart.ParentEntity.ChildrenEntities().ToArray();
            Array.Sort(partArray, delegate(ISceneChildEntity p1, ISceneChildEntity p2)
            {
                // we want the originally selected part first, then the rest in link order -- so make the selected part link num (-1)
                int linkNum1 = p1 == targetPart ? -1 : p1.LinkNum;
                int linkNum2 = p2 == targetPart ? -1 : p2.LinkNum;
                return linkNum1 - linkNum2;
            }
                );

            //look for prims with explicit sit targets that are available
            foreach (ISceneChildEntity part in partArray)
            {
                // Is a sit target available?
                Vector3 avSitOffSet = part.SitTargetPosition;
                Quaternion avSitOrientation = part.SitTargetOrientation;

                bool SitTargetisSet =
                    (!(avSitOffSet.X == 0f && avSitOffSet.Y == 0f && avSitOffSet.Z == 0f && avSitOrientation.W == 1f &&
                       avSitOrientation.X == 0f && avSitOrientation.Y == 0f && avSitOrientation.Z == 0f));

                if (SitTargetisSet && part.UUID != notID)
                {
                    //switch the target to this prim
                    return part;
                }
            }

            // no explicit sit target found - use original target
            return targetPart;
        }

        private SittingObjectData m_objectToSitOn = null;
        public void SitOnObjectAfterCrossing (SittingObjectData sod)
        {
            UUID targetID = sod.m_objectID;

            ISceneChildEntity part = FindNextAvailableSitTarget (targetID);
            if(part == null)
                return;

            if(String.IsNullOrEmpty(sod.m_animation))
                m_nextSitAnimation = "SIT";

            m_requestedSitTargetUUID = targetID;
            m_sitting = true;

            Vector3 sitTargetPos = sod.m_sitTargetPos;
            Quaternion sitTargetOrient = sod.m_sitTargetRot;

            m_pos = new Vector3(sitTargetPos.X, sitTargetPos.Y, sitTargetPos.Z);
            //m_pos += SIT_TARGET_ADJUSTMENT;
            m_bodyRot = sitTargetOrient;
            m_parentPosition = part.AbsolutePosition;
            m_parentID = m_requestedSitTargetUUID;

            part.SitTargetAvatar.Add(UUID);
            Velocity = Vector3.Zero;
            RemoveFromPhysicalScene();

            //Send updates to everyone about us
            foreach(IScenePresence sp in m_scene.GetScenePresences())
            {
                sp.SceneViewer.QueuePresenceForFullUpdate(this, true);
            }
            Animator.TrySetMovementAnimation(m_nextSitAnimation);
        }

        private void SendSitResponse(IClientAPI remoteClient, UUID targetID, Vector3 offset, Quaternion pSitOrientation)
        {
            bool autopilot = true;
            Vector3 pos = new Vector3();
            Quaternion sitOrientation = pSitOrientation;
            Vector3 cameraEyeOffset = Vector3.Zero;
            Vector3 cameraAtOffset = Vector3.Zero;

            ISceneChildEntity part = FindNextAvailableSitTarget(targetID);
            if (part.SitTargetAvatar.Count > 0)
                part = FindNextAvailableSitTarget(targetID, part.UUID);

            m_requestedSitTargetUUID = part.UUID;
            m_sitting = true;

            // Is a sit target available?
            Vector3 avSitOffSet = part.SitTargetPosition;
            Quaternion avSitOrientation = part.SitTargetOrientation;
            bool UseSitTarget = false;

            bool SitTargetisSet =
                (!(avSitOffSet.X == 0f && avSitOffSet.Y == 0f && avSitOffSet.Z == 0f &&
                   (
                       avSitOrientation.X == 0f && avSitOrientation.Y == 0f && avSitOrientation.Z == 0f &&
                       avSitOrientation.W == 1f // Valid Zero Rotation quaternion
                       ||
                       avSitOrientation.X == 0f && avSitOrientation.Y == 0f && avSitOrientation.Z == 1f &&
                       avSitOrientation.W == 0f // W-Z Mapping was invalid at one point
                       ||
                       avSitOrientation.X == 0f && avSitOrientation.Y == 0f && avSitOrientation.Z == 0f &&
                       avSitOrientation.W == 0f // Invalid Quaternion
                   )
                  ));

            m_requestedSitTargetUUID = part.UUID;
            m_sitting = true;
            part.SetAvatarOnSitTarget(UUID);
            var root = part.ParentEntity.RootChild;
            if (SitTargetisSet)
            {
                offset = new Vector3(avSitOffSet.X, avSitOffSet.Y, avSitOffSet.Z);
                sitOrientation = avSitOrientation;
                autopilot = false;
                UseSitTarget = true;
            }

            pos = part.AbsolutePosition; // +offset;
            if (m_physicsActor != null)
            {
                // If we're not using the client autopilot, we're immediately warping the avatar to the location
                // We can remove the physicsActor until they stand up.
                m_sitAvatarHeight = m_physicsActor.Size.Z;

                if (autopilot)
                {
                    Vector3 targetpos = new Vector3(m_pos.X - part.AbsolutePosition.X - (part.Scale.X/2),
                                                    m_pos.Y - part.AbsolutePosition.Y - (part.Scale.Y/2),
                                                    m_pos.Z - part.AbsolutePosition.Z - (part.Scale.Z/2));
                    if (targetpos.Length() < 4.5)
                    {
                        autopilot = false;
                        Velocity = Vector3.Zero;
                        RemoveFromPhysicalScene();
                        Vector3 Position = part.AbsolutePosition;
                        Vector3 MovePos = Vector3.Zero;
                        IAvatarAppearanceModule appearance = RequestModuleInterface<IAvatarAppearanceModule>();
                        if (appearance != null)
                        {
                            if (part.GetPrimType() == PrimType.BOX ||
                                part.GetPrimType() == PrimType.CYLINDER ||
                                part.GetPrimType() == PrimType.TORUS ||
                                part.GetPrimType() == PrimType.TUBE ||
                                part.GetPrimType() == PrimType.RING ||
                                part.GetPrimType() == PrimType.PRISM ||
                                part.GetPrimType() == PrimType.SCULPT)
                            {
                                Position.Z += part.Scale.Z/2f;
                                Position.Z += appearance.Appearance.AvatarHeight/2;
                                Position.Z -= (float) (SIT_TARGET_ADJUSTMENT.Z/1.5); //m_appearance.AvatarHeight / 15;

                                MovePos.X = (part.Scale.X/2) + .1f;
                                MovePos *= Rotation;
                            }
                            else if (part.GetPrimType() == PrimType.SPHERE)
                            {
                                Position.Z += part.Scale.Z/2f;
                                Position.Z += appearance.Appearance.AvatarHeight/2;
                                Position.Z -= (float) (SIT_TARGET_ADJUSTMENT.Z/1.5); //m_appearance.AvatarHeight / 15;

                                MovePos.X = (float) (part.Scale.X/2.5);
                                MovePos *= Rotation;
                            }
                        }
                        Position += MovePos;
                        AbsolutePosition = Position;
                    }
                }
                else
                    RemoveFromPhysicalScene();
            }

            cameraAtOffset = part.CameraAtOffset;
            cameraEyeOffset = part.CameraEyeOffset;
            bool forceMouselook = part.ForceMouselook;

            ControllingClient.SendSitResponse(part.UUID, offset, sitOrientation, autopilot, cameraAtOffset,
                                              cameraEyeOffset, forceMouselook);
            //Remove any bad terse updates lieing around
            SceneViewer.ClearPresenceUpdates(this);
            System.Threading.Thread.Sleep(10);
                //Sleep for a little bit to make sure all other threads are finished sending anything
            // This calls HandleAgentSit twice, once from here, and the client calls
            // HandleAgentSit itself after it gets to the location
            // It doesn't get to the location until we've moved them there though
            // which happens in HandleAgentSit :P
            m_autopilotMoving = autopilot;
            m_autoPilotTarget = pos;
            m_sitAtAutoTarget = autopilot;
            if (!autopilot)
                HandleAgentSit(remoteClient, UUID, String.IsNullOrEmpty(m_nextSitAnimation) ? "SIT" : m_nextSitAnimation,
                               UseSitTarget);
        }

        public void HandleAgentRequestSit(IClientAPI remoteClient, UUID targetID, Vector3 offset)
        {
            if (m_parentID != UUID.Zero)
                StandUp();

            m_nextSitAnimation = "SIT";
            
            ISceneChildEntity part = FindNextAvailableSitTarget (targetID);

            if (part != null)
            {
                if (!String.IsNullOrEmpty(part.SitAnimation))
                    m_nextSitAnimation = part.SitAnimation;

                m_sitting = true;
                m_requestedSitTargetUUID = targetID;
                
                SendSitResponse(remoteClient, targetID, offset, Quaternion.Identity);
            }
            else
                MainConsole.Instance.Warn("Sit requested on unknown object: " + targetID.ToString());
        }
        
        public void HandleAgentRequestSit(IClientAPI remoteClient, UUID agentID, UUID targetID, Vector3 offset, string sitAnimation)
        {
            if (m_parentID != UUID.Zero)
                StandUp();

            m_nextSitAnimation = !String.IsNullOrEmpty(sitAnimation) ? sitAnimation : "SIT";

            ISceneChildEntity part = FindNextAvailableSitTarget (targetID);
            if (part != null)
            {
                m_requestedSitTargetUUID = targetID;
                m_sitting = true;

                MainConsole.Instance.DebugFormat("[SIT]: Client requested Sit Position: {0}", offset);
                SendSitResponse(remoteClient, targetID, offset, Quaternion.Identity);
            }
            else
                MainConsole.Instance.Warn("Sit requested on unknown object: " + targetID);
        }

        public void HandleAgentSit(IClientAPI remoteClient, UUID agentID)
        {
            HandleAgentSit(remoteClient, agentID, !String.IsNullOrEmpty(m_nextSitAnimation) ? m_nextSitAnimation : "SIT",
                           false);
        }

        public void HandleAgentSit(IClientAPI remoteClient, UUID agentID, string sitAnimation, bool UseSitTarget)
        {
            ISceneChildEntity part = m_scene.GetSceneObjectPart (m_requestedSitTargetUUID);
            if (part != null)
            {
                //This MUST be done first so that we don't get any position updates from the PhysActor once we sit
                try
                {
                    if(PhysicsActor != null)
                        RemoveFromPhysicalScene ();
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.Warn (ex);
                }

                if (m_sitAtAutoTarget || !m_autopilotMoving)
                {
                    if (UseSitTarget)
                    {
                        Vector3 sitTargetPos = part.SitTargetPosition;
                        Quaternion sitTargetOrient = part.SitTargetOrientation;

                        m_pos = new Vector3(sitTargetPos.X, sitTargetPos.Y, sitTargetPos.Z);
                        m_pos += SIT_TARGET_ADJUSTMENT;
                        m_bodyRot = sitTargetOrient;
                        m_parentPosition = part.AbsolutePosition;
                    }
                    else
                    {
                        m_pos -= part.AbsolutePosition;
                        m_parentPosition = part.AbsolutePosition;
                    }
                }

                m_parentID = m_requestedSitTargetUUID;
                Velocity = Vector3.Zero;
                //Force send a full update
                ControllingClient.SendAvatarDataImmediate(this);
                foreach (IScenePresence sp in m_scene.GetScenePresences ())
                {
                    if (sp.UUID != UUID &&
                        sp.SceneViewer.Culler.ShowEntityToClient (sp, this, Scene))
                        sp.ControllingClient.SendAvatarDataImmediate (this);
                }
                Animator.TrySetMovementAnimation(sitAnimation);
            }
        }

        #endregion

        /// <summary>
        /// Event handler for the 'Always run' setting on the client
        /// Tells the physics plugin to increase speed of movement.
        /// </summary>
        public void HandleSetAlwaysRun(IClientAPI remoteClient, bool pSetAlwaysRun)
        {
            m_setAlwaysRun = pSetAlwaysRun;
            if (PhysicsActor != null)
            {
                PhysicsActor.SetAlwaysRun = pSetAlwaysRun;
            }
        }

        public void HandleStartAnim(IClientAPI remoteClient, UUID animID)
        {
            if (animID == Animations.BUSY)
                m_isBusy = true;
            Animator.AddAnimation(animID, UUID.Zero);
        }

        public void HandleStopAnim(IClientAPI remoteClient, UUID animID)
        {
            if (animID == Animations.BUSY)
                m_isBusy = false;
            Animator.RemoveAnimation(animID);
        }


        
        /// <summary>
        /// Rotate the avatar to the given rotation and apply a movement in the given relative vector
        /// </summary>
        /// <param name="vec">The vector in which to move.  This is relative to the rotation argument</param>
        /// <param name="rotation">The direction in which this avatar should now face.</param>
        public void AddNewMovement(Vector3 vec, Quaternion rotation)
        {
            if (IsChildAgent)
            {
                // WHAT??? we can't make them a root agent though... what if they shouldn't be here?
                //  Or even worse, what if they are spoofing the client???
                MainConsole.Instance.Info("[SCENEPRESENCE]: AddNewMovement() called on child agent for " + Name + "! Possible attempt to force a fake agent into a sim!");
                return;
            }

            PhysicsCharacter actor = m_physicsActor;
            if (actor != null)
            {
                /*Vector3 direc = (rotation == Quaternion.Identity ? vec : (vec * rotation));
                Rotation = rotation;
                if (direc == Vector3.Zero)
                    PhysicsActor.Velocity = Vector3.Zero;
                else
                {
                    direc.Normalize();
                    PhysicsActor.SetMovementForce(direc * 1.5f);
                }*/
                Vector3 direc = (rotation == Quaternion.Identity ? vec : (vec * rotation));
                Rotation = rotation;
                direc.Normalize();
                if (!actor.Flying && direc.Z > 0f && direc.Z < 0.2f)
                    direc.Z = 0;//Disable walking up into the air unless we are attempting to jump
                actor.SetMovementForce(direc * 1.2f);
            }
        }
        #endregion

        #region Overridden Methods

        public virtual void Close()
        {
            m_sceneViewer.Close();

            RemoveFromPhysicalScene();
            if (m_animator == null)
                return;
            m_animator.Close();
            m_animator = null;
        }

        public virtual void Update ()
        {
            if (!IsChildAgent && m_parentID != UUID.Zero)
            {
                SceneObjectPart part = Scene.GetSceneObjectPart(m_parentID) as SceneObjectPart;
                if (part != null)
                {
                    part.SetPhysActorCameraPos((m_AgentControlFlags & AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) ==
                                               AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK
                                                   ? CameraRotation
                                                   : Quaternion.Identity);
                }
            }
            if((Taints & PresenceTaint.SignificantMovement) == PresenceTaint.SignificantMovement)
            {
                Taints &= ~PresenceTaint.SignificantMovement;
                //Trigger the movement now
                TriggerSignificantClientMovement();
            }
            if ((Taints & PresenceTaint.TerseUpdate) == PresenceTaint.TerseUpdate)
            {
                Taints &= ~PresenceTaint.TerseUpdate;
                //Send the terse update
                SendTerseUpdateToAllClients ();
            }
            if ((Taints & PresenceTaint.Movement) == PresenceTaint.Movement)
            {
                Taints &= ~PresenceTaint.Movement;
                //Finish out the event
                UpdatePosAndVelocity ();
            }
            if (m_enqueueSendChildAgentUpdate &&
                m_enqueueSendChildAgentUpdateTime != new DateTime ())
            {
                if (DateTime.Now > m_enqueueSendChildAgentUpdateTime)
                {
                    Taints &= ~PresenceTaint.Other;
                    //Reset it now
                    m_enqueueSendChildAgentUpdateTime = new DateTime ();
                    m_enqueueSendChildAgentUpdate = false;

                    AgentPosition agentpos = new AgentPosition
                                                 {
                                                     AgentID = UUID,
                                                     AtAxis = CameraAtAxis,
                                                     Center = m_lastChildAgentUpdateCamPosition,
                                                     Far = DrawDistance,
                                                     LeftAxis = CameraLeftAxis,
                                                     Position = m_lastChildAgentUpdatePosition,
                                                     RegionHandle = Scene.RegionInfo.RegionHandle,
                                                     UpAxis = CameraUpAxis,
                                                     Velocity = Velocity
                                                 };

                    //Send the child agent data update
                    ISyncMessagePosterService syncPoster = Scene.RequestModuleInterface<ISyncMessagePosterService> ();
                    if (syncPoster != null)
                        syncPoster.Post (SyncMessageHelper.SendChildAgentUpdate (agentpos, m_scene.RegionInfo.RegionHandle), m_scene.RegionInfo.RegionHandle);
                }
                else
                    Scene.SceneGraph.TaintPresenceForUpdate(this, PresenceTaint.Other);//We havn't sent the update yet, keep tainting
            }
        }

        #endregion

        #region Update Client(s)

        /// <summary>
        /// Tell the SceneViewer for the given client about the update
        /// </summary>
        /// <param name="part"></param>
        /// <param name="flags"></param>
        public virtual void AddUpdateToAvatar(ISceneChildEntity part, PrimUpdateFlags flags)
        {
            m_sceneViewer.QueuePartForUpdate(part, flags);
        }

        /// <summary>
        /// Sends a location update to the client connected to this scenePresence
        /// </summary>
        /// <param name="remoteClient"></param>
        public virtual void SendTerseUpdateToClient (IScenePresence remoteClient)
        {
            //MainConsole.Instance.DebugFormat("[SCENEPRESENCE]: TerseUpdate: Pos={0} Rot={1} Vel={2}", m_pos, m_bodyRot, m_velocity);
            remoteClient.SceneViewer.QueuePresenceForUpdate (
                this,
                PrimUpdateFlags.TerseUpdate);
        }

        /// <summary>
        /// Send a location/velocity/accelleration update to all agents in scene
        /// </summary>
        public virtual void SendTerseUpdateToAllClients ()
        {
            m_perfMonMS = Util.EnvironmentTickCount();

            m_scene.ForEachScenePresence(SendTerseUpdateToClient);

            IAgentUpdateMonitor reporter = (IAgentUpdateMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), MonitorModuleHelper.AgentUpdateCount);
            if (reporter != null)
            {
                reporter.AddAgentTime(Util.EnvironmentTickCountSubtract(m_perfMonMS));
                reporter.AddAgentUpdates(m_scene.GetScenePresenceCount());
            }
        }

        public virtual void SendCoarseLocations (List<Vector3> coarseLocations, List<UUID> avatarUUIDs)
        {
            SendCourseLocationsMethod d = m_sendCourseLocationsMethod;
            if (d != null)
            {
                d.Invoke(m_scene.RegionInfo.RegionID, this, coarseLocations, avatarUUIDs);
            }
        }

        public virtual void SetSendCourseLocationMethod (SendCourseLocationsMethod d)
        {
            if (d != null)
                m_sendCourseLocationsMethod = d;
        }

        public virtual void SendCoarseLocationsDefault (UUID sceneId, IScenePresence p, List<Vector3> coarseLocations, List<UUID> avatarUUIDs)
        {
            m_perfMonMS = Util.EnvironmentTickCount();
            m_controllingClient.SendCoarseLocationUpdate(avatarUUIDs, coarseLocations);
            IAgentUpdateMonitor reporter = (IAgentUpdateMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), MonitorModuleHelper.AgentUpdateCount);
            if (reporter != null)
                reporter.AddAgentTime(Util.EnvironmentTickCountSubtract(m_perfMonMS));
        }

        #endregion

        #region Significant Movement Method

        /// <summary>
        /// This checks for a significant movement and sends a courselocationchange update
        /// </summary>
        protected virtual void CheckForSignificantMovement ()
        {
            // Movement updates for agents in neighboring regions are sent directly to clients.
            // This value only affects how often agent positions are sent to neighbor regions
            // for things such as distance-based update prioritization
            if (Vector3.DistanceSquared(AbsolutePosition, posLastSignificantMove) > SIGNIFICANT_MOVEMENT * SIGNIFICANT_MOVEMENT)
            {
                posLastSignificantMove = AbsolutePosition;
                Scene.SceneGraph.TaintPresenceForUpdate (this, PresenceTaint.SignificantMovement);
            }
            if (Vector3.DistanceSquared (AbsolutePosition, posLastTerseUpdate) > TERSE_UPDATE_MOVEMENT * TERSE_UPDATE_MOVEMENT)
            {
                posLastTerseUpdate = AbsolutePosition;
                Scene.SceneGraph.TaintPresenceForUpdate (this, PresenceTaint.Movement);
            }
            if (m_sceneViewer == null || m_sceneViewer.Prioritizer == null)
                return;

            // Minimum Draw distance is 64 meters, the Radius of the draw distance sphere is 32m
            double  tmpsq = m_sceneViewer.Prioritizer.ChildReprioritizationDistance;
            tmpsq *= tmpsq;
            float vel = Velocity.LengthSquared();
            if (Vector3.DistanceSquared(AbsolutePosition, m_lastChildAgentUpdatePosition) >= tmpsq ||
                Vector3.DistanceSquared (CameraPosition, m_lastChildAgentUpdateCamPosition) >= tmpsq) 
                 
            {
                m_lastChildAgentUpdatePosition = AbsolutePosition;
                m_lastChildAgentUpdateCamPosition = CameraPosition;

                AddChildAgentUpdateTaint (5);
            }

            // Disabled for now until we can make sure that we only send one of these per simulation loop,
            //   as with lots of clients, this will lag the client badly.
            //
            // Moving collision sound ID inside this loop so that we don't trigger it too much
            if (CollisionSoundID != UUID.Zero && (CollisionSoundLastTriggered == 0 ||
                Util.EnvironmentTickCount() - CollisionSoundLastTriggered > 0))
            {
                ISoundModule module = Scene.RequestModuleInterface<ISoundModule>();
                module.TriggerSound(CollisionSoundID, UUID, UUID, UUID.Zero, 1, AbsolutePosition, Scene.RegionInfo.RegionHandle, 100);
                CollisionSoundLastTriggered = Util.EnvironmentTickCount() + 100;//Only 10 a second please!
                CollisionSoundID = UUID.Zero;
            }
        }

        public virtual void AddChildAgentUpdateTaint (int seconds)
        {
            Scene.SceneGraph.TaintPresenceForUpdate (this, PresenceTaint.Other);
            m_enqueueSendChildAgentUpdate = true;
            m_enqueueSendChildAgentUpdateTime = DateTime.Now.AddSeconds(seconds);
        }

        public virtual void SendPhysicsTerseUpdateToAllClients ()
        {
            Scene.SceneGraph.TaintPresenceForUpdate(this, PresenceTaint.TerseUpdate);
        }

        #endregion

        #region Border Crossing Methods

        private readonly Dictionary<UUID, int> m_failedNeighborCrossing = new Dictionary<UUID, int>();
        private Vector3 m_lastSigInfiniteRegionPos = Vector3.Zero;
        private bool m_foundNeighbors = false;
        private List<GridRegion> m_nearbyInfiniteRegions = new List<GridRegion>();

        /// <summary>
        /// Checks to see if the avatar is in range of a border and calls CrossToNewRegion
        /// </summary>
        protected virtual bool CheckForBorderCrossing ()
        {
            if (IsChildAgent)
                return false;
            //Don't check if the avatar is sitting on something. Crossing should be called
            // directly by the SOG if the object needs to cross.
            if (m_parentID != UUID.Zero)
                return false;

            Vector3 pos2 = AbsolutePosition;
            Vector3 vel = Velocity;

            const float timeStep = 0.1f;
            pos2.X = pos2.X + ((Math.Abs (vel.X) < 2.5 ? vel.X * timeStep * 2 : vel.X * timeStep));
            pos2.Y = pos2.Y + ((Math.Abs (vel.Y) < 2.5 ? vel.Y * timeStep * 2 : vel.Y * timeStep));
            pos2.Z = pos2.Z + ((Math.Abs (vel.Z) < 2.5 ? vel.Z * timeStep * 2 : vel.Z * timeStep));

            if (!IsInTransit)
            {
                if(pos2.X < 0f || pos2.Y < 0f ||
                    pos2.X > Scene.RegionInfo.RegionSizeX || pos2.Y > Scene.RegionInfo.RegionSizeY)
                {
                    if(Scene.RegionInfo.InfiniteRegion)
                    {
                        if(!m_foundNeighbors)
                        {
                            m_foundNeighbors = true;
                            m_lastSigInfiniteRegionPos = AbsolutePosition;
                            IGridRegisterModule neighborService = Scene.RequestModuleInterface<IGridRegisterModule>();
                            if(neighborService != null)
                                m_nearbyInfiniteRegions = neighborService.GetNeighbors(Scene);
                        }
                        double TargetX = Scene.RegionInfo.RegionLocX + (double)pos2.X;
                        double TargetY = Scene.RegionInfo.RegionLocY + (double)pos2.Y;
                        if(m_lastSigInfiniteRegionPos.X - AbsolutePosition.X > 128 ||
                            m_lastSigInfiniteRegionPos.X - AbsolutePosition.X < -128 ||
                            m_lastSigInfiniteRegionPos.Y - AbsolutePosition.Y > 128 ||
                            m_lastSigInfiniteRegionPos.Y - AbsolutePosition.Y < -128)
                        {
                            m_lastSigInfiniteRegionPos = AbsolutePosition;
                            m_nearbyInfiniteRegions = Scene.GridService.GetRegionRange(ControllingClient.AllScopeIDs,
                                (int)(TargetX - Scene.GridService.GetMaxRegionSize()),
                                (int)(TargetX + 256),
                                (int)(TargetY - Scene.GridService.GetMaxRegionSize()),
                                (int)(TargetY + 256));
                        }
#if (!ISWIN)
                        GridRegion neighborRegion = null;
                        foreach (GridRegion region in m_nearbyInfiniteRegions)
                        {
                            if (TargetX >= region.RegionLocX && TargetY >= region.RegionLocY && TargetX < region.RegionLocX + region.RegionSizeX && TargetY < region.RegionLocY + region.RegionSizeY)
                            {
                                neighborRegion = region;
                                break;
                            }
                        }
#else
                        GridRegion neighborRegion =
                            m_nearbyInfiniteRegions.FirstOrDefault(
                                region =>
                                TargetX >= region.RegionLocX && TargetY >= region.RegionLocY &&
                                TargetX < region.RegionLocX + region.RegionSizeX &&
                                TargetY < region.RegionLocY + region.RegionSizeY);
#endif

                        if(neighborRegion != null)
                        {
                            if(m_failedNeighborCrossing.ContainsKey(neighborRegion.RegionID))
                            {
                                int diff = Util.EnvironmentTickCountSubtract(m_failedNeighborCrossing[neighborRegion.RegionID]);
                                if (diff > 10 * 1000)
                                    m_failedNeighborCrossing.Remove(neighborRegion.RegionID); //Only allow it to retry every 10 seconds
                                else
                                {
                                    MainConsole.Instance.DebugFormat("[ScenePresence]: Unable to cross to a neighboring region, because we failed to contact the other region");
                                    return false;
                                }
                            }

                            InTransit();
                            bool isFlying = false;

                            if(m_physicsActor != null)
                                isFlying = m_physicsActor.Flying;

                            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
                            if(transferModule != null)
                                transferModule.Cross(this, isFlying, neighborRegion);
                            else
                                MainConsole.Instance.DebugFormat("[ScenePresence]: Unable to cross agent to neighbouring region, because there is no AgentTransferModule");
                        }
                        return true;
                    }
                    else
                    {
                        //If we are headed out of the region, make sure we have a region there
                        IGridRegisterModule neighborService = Scene.RequestModuleInterface<IGridRegisterModule>();
                        if(neighborService != null)
                        {
                            List<GridRegion> neighbors = neighborService.GetNeighbors(Scene);

                            double TargetX = (double)Scene.RegionInfo.RegionLocX + (double)pos2.X;
                            double TargetY = (double)Scene.RegionInfo.RegionLocY + (double)pos2.Y;

                            GridRegion neighborRegion = null;

                            foreach(GridRegion region in neighbors)
                            {
                                if(TargetX >= (double)region.RegionLocX
                                    && TargetY >= (double)region.RegionLocY
                                    && TargetX < (double)(region.RegionLocX + region.RegionSizeX)
                                    && TargetY < (double)(region.RegionLocY + region.RegionSizeY))
                                {
                                    neighborRegion = region;
                                    break;
                                }
                            }

                            if(neighborRegion != null)
                            {
                                if(m_failedNeighborCrossing.ContainsKey(neighborRegion.RegionID))
                                {
                                    int diff = Util.EnvironmentTickCountSubtract(m_failedNeighborCrossing[neighborRegion.RegionID]);
                                    if(diff > 10 * 1000)
                                        m_failedNeighborCrossing.Remove(neighborRegion.RegionID); //Only allow it to retry every 10 seconds
                                    else
                                    {
                                        MainConsole.Instance.DebugFormat("[ScenePresence]: Unable to cross to a neighboring region, because we failed to contact the other region");
                                        return false;
                                    }
                                }

                                InTransit();
                                bool isFlying = false;

                                if(m_physicsActor != null)
                                    isFlying = m_physicsActor.Flying;

                                IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
                                if(transferModule != null)
                                    transferModule.Cross(this, isFlying, neighborRegion);
                                else
                                    MainConsole.Instance.DebugFormat("[ScenePresence]: Unable to cross agent to neighbouring region, because there is no AgentTransferModule");

                                return true;
                            }
                            //else
                            //    MainConsole.Instance.Debug("[ScenePresence]: Could not find region for " + Name + " to cross into @ {" + TargetX / 256 + ", " + TargetY / 256 + "}");
                        }
                    }
                }
            }
            else
            {
                //Crossings are much nastier if this code is enabled
                //RemoveFromPhysicalScene();
                // This constant has been inferred from experimentation
                // I'm not sure what this value should be, so I tried a few values.
                /*timeStep = 0.025f;
                pos2 = AbsolutePosition;
                pos2.X = pos2.X + (vel.X * timeStep);
                pos2.Y = pos2.Y + (vel.Y * timeStep);
                pos2.Z = pos2.Z + (vel.Z * timeStep);
                //Velocity = (AbsolutePosition - pos2) * 2;
                AbsolutePosition = pos2;*/
                return true;
            }
            return false;
        }

        public virtual void InTransit ()
        {
            m_inTransit = true;

            if ((m_physicsActor != null) && m_physicsActor.Flying)
                m_AgentControlFlags |= AgentManager.ControlFlags.AGENT_CONTROL_FLY;
            else if ((m_AgentControlFlags & AgentManager.ControlFlags.AGENT_CONTROL_FLY) != 0)
                m_AgentControlFlags &= ~AgentManager.ControlFlags.AGENT_CONTROL_FLY;
        }

        public void SuccessfulTransit()
        {
            m_inTransit = false;
        }

        public void FailedTransit ()
        {
            m_inTransit = false;
        }

        public void SuccessfulCrossingTransit (GridRegion crossingRegion)
        {
            m_inTransit = false;
            //We got there fine, remove it
            m_failedNeighborCrossing.Remove(crossingRegion.RegionID);
        }

        public void FailedCrossingTransit (GridRegion failedCrossingRegion)
        {
            m_inTransit = false;
            m_failedNeighborCrossing[failedCrossingRegion.RegionID] = Util.EnvironmentTickCount();
        }

        private void Reset()
        {
            //Reset the parcel UUID for the user
            CurrentParcelUUID = UUID.Zero;
            CurrentParcel = null;
            // Put the child agent back at the center
            AbsolutePosition
                = new Vector3(Scene.RegionInfo.RegionSizeX * 0.5f, Scene.RegionInfo.RegionSizeY * 0.5f, 70);
            if(Animator != null)
                Animator.ResetAnimations();
            m_parentID = UUID.Zero;
            m_parentPosition = Vector3.Zero;
            ControllingClient.Reset();
            SuccessfulTransit ();
        }

        #endregion

        #region Child Agent Updates

        public virtual void ChildAgentDataUpdate (AgentData cAgentData)
        {
            //MainConsole.Instance.Debug("   >>> ChildAgentDataUpdate <<< " + Scene.RegionInfo.RegionName);
            //if (!IsChildAgent)
            //    return;

            CopyFrom(cAgentData);
        }

        /// <summary>
        /// This updates important decision making data about a child agent
        /// The main purpose is to figure out what objects to send to a child agent that's in a neighboring region
        /// </summary>
        public virtual void ChildAgentDataUpdate (AgentPosition cAgentData, int tRegionX, int tRegionY, int rRegionX, int rRegionY)
        {
            if (!IsChildAgent)
                return;

            //MainConsole.Instance.Debug("   >>> ChildAgentPositionUpdate <<< " + rRegionX + "-" + rRegionY);
            int shiftx = rRegionX - tRegionX;
            int shifty = rRegionY - tRegionY;

            Vector3 offset = new Vector3(shiftx, shifty, 0f);

            DrawDistance = cAgentData.Far;
            m_pos = cAgentData.Position + offset;

            m_CameraCenter = cAgentData.Center + offset;

            TriggerSignificantClientMovement();

            m_savedVelocity = cAgentData.Velocity;
        }

        public void TriggerSignificantClientMovement ()
        {
            if(OnSignificantClientMovement != null)
                OnSignificantClientMovement();
            m_scene.EventManager.TriggerSignificantClientMovement(this);
        }

        public virtual void CopyTo (AgentData cAgent)
        {
            cAgent.AgentID = UUID;
            cAgent.RegionID = Scene.RegionInfo.RegionID;

            cAgent.Position = AbsolutePosition + OffsetPosition;
            cAgent.Velocity = Velocity;
            cAgent.Center = m_CameraCenter;
            cAgent.AtAxis = m_CameraAtAxis;
            cAgent.LeftAxis = m_CameraLeftAxis;
            cAgent.UpAxis = m_CameraUpAxis;

            cAgent.Far = DrawDistance;

            // Throttles 
            float multiplier = 1;
            int innacurateNeighbors = m_scene.RequestModuleInterface<IGridRegisterModule> ().GetNeighbors (m_scene).Count;
            if (innacurateNeighbors != 0)
            {
                multiplier = 1f / innacurateNeighbors;
            }
            if (multiplier <= 0.25f)
            {
                multiplier = 0.25f;
            }
            //MainConsole.Instance.Info("[NeighborThrottle]: " + m_scene.GetInaccurateNeighborCount().ToString() + " - m: " + multiplier.ToString());
            cAgent.Throttles = ControllingClient.GetThrottlesPacked(multiplier);

            cAgent.HeadRotation = m_headrotation;
            cAgent.BodyRotation = m_bodyRot;
            cAgent.ControlFlags = (uint)m_AgentControlFlags;

            //This is checked by the other sim, so we don't have to validate it at all
            //if (m_scene.Permissions.IsGod(new UUID(cAgent.AgentID)))
            cAgent.GodLevel = (byte)m_godLevel;
            //else 
            //    cAgent.GodLevel = (byte) 0;

            cAgent.Speed = SpeedModifier;
            cAgent.DrawDistance = DrawDistance;
            cAgent.AlwaysRun = m_setAlwaysRun;
            IAvatarAppearanceModule appearance = RequestModuleInterface<IAvatarAppearanceModule> ();
            if (appearance != null)
            {
                cAgent.SentInitialWearables = appearance.InitialHasWearablesBeenSent;
                cAgent.Appearance = new AvatarAppearance (appearance.Appearance);
            }

            IScriptControllerModule m = RequestModuleInterface<IScriptControllerModule> ();
            if (m != null)
                cAgent.Controllers = m.Serialize ();

            cAgent.SittingObjects = new SittingObjectData();
            if(Sitting)
            {
                ISceneChildEntity child = Scene.GetSceneObjectPart(SittingOnUUID);
                if(child != null && child.ParentEntity != null)
                {
                    cAgent.SittingObjects.m_sittingObjectXML = ((ISceneObject)child.ParentEntity).ToXml2();
                    cAgent.SittingObjects.m_sitTargetPos = OffsetPosition;//Get the difference
                    cAgent.SittingObjects.m_sitTargetRot = m_bodyRot;
                    cAgent.SittingObjects.m_animation = m_nextSitAnimation;
                }
            }
            
            // Animations
            if (Animator != null)
                cAgent.Anims = Animator.Animations.ToArray();
        }

        public virtual void CopyFrom (AgentData cAgent)
        {
            try
            {
                m_callbackURI = cAgent.CallbackURI;
                m_pos = cAgent.Position;
                if(PhysicsActor != null)
                {
                    AbsolutePosition = cAgent.Position;
                    PhysicsActor.ForceSetPosition(cAgent.Position);
                }
                Velocity = cAgent.Velocity;
                m_CameraCenter = cAgent.Center;
                SetHeight (cAgent.Size.Z);
                m_CameraAtAxis = cAgent.AtAxis;
                m_CameraLeftAxis = cAgent.LeftAxis;
                m_CameraUpAxis = cAgent.UpAxis;

                DrawDistance = cAgent.Far;

                if ((cAgent.Throttles != null) && cAgent.Throttles.Length > 0)
                    ControllingClient.SetChildAgentThrottle(cAgent.Throttles);

                m_headrotation = cAgent.HeadRotation;
                m_bodyRot = cAgent.BodyRotation;
                m_AgentControlFlags = (AgentManager.ControlFlags)cAgent.ControlFlags;
                m_savedVelocity = cAgent.Velocity;
                 
                SpeedModifier = cAgent.Speed;
                DrawDistance = cAgent.DrawDistance;
                m_setAlwaysRun = cAgent.AlwaysRun;
                if(cAgent.IsCrossing)
                {
                    m_scene.AuthenticateHandler.GetAgentCircuitData(UUID).teleportFlags |= (uint)TeleportFlags.ViaRegionID;
                    m_scene.AuthenticateHandler.GetAgentCircuitData(UUID).reallyischild = false;//We're going to be a root
                }
                IAvatarAppearanceModule appearance = RequestModuleInterface<IAvatarAppearanceModule> ();
                if (appearance != null)
                {
                    appearance.InitialHasWearablesBeenSent = cAgent.SentInitialWearables;
                    appearance.Appearance = new AvatarAppearance (cAgent.Appearance);
                }

                // Animations
                try
                {
                    Animator.ResetAnimations();
                    Animator.Animations.FromArray(cAgent.Anims);
                }
                catch { }
                try
                {
                    if(cAgent.SittingObjects != null && cAgent.SittingObjects.m_sittingObjectXML != "")
                    {
                        ISceneObject sceneObject = null;
                        IRegionSerialiserModule mod = Scene.RequestModuleInterface<IRegionSerialiserModule>();
                        if(mod != null)
                            sceneObject = mod.DeserializeGroupFromXml2(cAgent.SittingObjects.m_sittingObjectXML, Scene);

                        if(sceneObject != null)
                        {
                            //We were sitting on something when we crossed
                            if(Scene.SceneGraph.RestorePrimToScene(sceneObject, false))
                            {
                                if(sceneObject.RootChild.IsSelected)
                                    sceneObject.RootChild.CreateSelected = true;
                                sceneObject.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
                                sceneObject.CreateScriptInstances(0, false, StateSource.PrimCrossing, UUID.Zero, false);

                                sceneObject.RootChild.PhysActor.ForceSetVelocity(cAgent.Velocity);
                                sceneObject.RootChild.PhysActor.Velocity = (cAgent.Velocity);
                                sceneObject.AbsolutePosition = cAgent.Position;
                                Animator.TrySetMovementAnimation(cAgent.SittingObjects.m_animation);
                                m_nextSitAnimation = cAgent.SittingObjects.m_animation;
                                cAgent.SittingObjects.m_objectID = sceneObject.UUID;
                                m_objectToSitOn = cAgent.SittingObjects;

                                foreach(ISceneChildEntity child in sceneObject.ChildrenEntities())
                                {
                                    foreach(TaskInventoryItem taskInv in child.Inventory.GetInventoryItems())
                                    {
                                        foreach(ControllerData cd in cAgent.Controllers)
                                        {
                                            if(cd.ItemID == taskInv.ItemID || cd.ItemID == taskInv.OldItemID)
                                            {
                                                cd.ItemID = taskInv.ItemID;
                                            }
                                        }
                                    }
                                }

                                try
                                {
                                    IScriptControllerModule m = RequestModuleInterface<IScriptControllerModule>();
                                    if(m != null)
                                        if(cAgent.Controllers != null)
                                            m.Deserialize(cAgent.Controllers);
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
            }
            catch(Exception ex)
            {
                MainConsole.Instance.Warn("[ScenePresence]: Error in CopyFrom: " + ex);
            }
        }

        #endregion Child Agent Updates

        #region Physics

        /// <summary>
        /// Adds a physical representation of the avatar to the Physics plugin
        /// </summary>
        public virtual void AddToPhysicalScene (bool isFlying, bool AddAvHeightToPosition)
        {
            //Make sure we arn't already doing this
            if (m_creatingPhysicalRepresentation)
                return;

            //Set this so we don't do it multiple times
            m_creatingPhysicalRepresentation = true;

            Vector3 size = new Vector3(0, 0, m_defaultAvHeight);
            IAvatarAppearanceModule appearance = RequestModuleInterface<IAvatarAppearanceModule> ();
            if (appearance != null)
                size.Z = appearance.Appearance.AvatarHeight;

            PhysicsScene scene = m_scene.PhysicsScene;

            Vector3 pVec = AbsolutePosition;

            if(AddAvHeightToPosition) //This is here so that after teleports, you arrive just slightly higher so that you don't fall through the ground/objects
                pVec.Z += size.Z;

            m_physicsActor = scene.AddAvatar(Name, pVec, Rotation, size, isFlying, LocalId, UUID);

            m_physicsActor.OnRequestTerseUpdate += SendPhysicsTerseUpdateToAllClients;
            m_physicsActor.OnSignificantMovement += CheckForSignificantMovement;
            m_physicsActor.OnCollisionUpdate += PhysicsCollisionUpdate;
            m_physicsActor.OnPositionAndVelocityUpdate += PhysicsUpdatePosAndVelocity;
            m_physicsActor.OnCheckForRegionCrossing += CheckForBorderCrossing;

            m_physicsActor.OnOutOfBounds += OutOfBoundsCall; // Called for PhysicsActors when there's something wrong
            m_physicsActor.Orientation = Rotation;

            m_physicsActor.Flying = isFlying;

            //Tell any events about it
            if (OnAddPhysics != null)
                OnAddPhysics();

            //All done, reset this
            m_creatingPhysicalRepresentation = false;
        }

        /// <summary>
        /// Sets avatar height in the phyiscs plugin
        /// </summary>
        public virtual void SetHeight (float height)
        {
            //If the av exists, set their new size, if not, add them to the region
            if (height != 0 && PhysicsActor != null && !IsChildAgent)
            {
                if (Math.Abs(height - PhysicsActor.Size.Z) > 0.1)
                {
                    Vector3 SetSize = new Vector3 (0.45f, 0.6f, height);
                    PhysicsActor.Size = SetSize;
                }
            }
        }

        protected void OutOfBoundsCall(Vector3 pos)
        {
            m_pos = new Vector3(m_scene.RegionInfo.RegionSizeX / 2, m_scene.RegionInfo.RegionSizeY / 2,
                    128);
            if (PhysicsActor != null)
            {
                PhysicsActor.ForceSetPosition(m_pos);
                PhysicsActor.ForceSetVelocity(Vector3.Zero);
                RemoveFromPhysicalScene();
            }
            MainConsole.Instance.Error("[AVATAR]: NonFinite Avatar position detected... Reset Position, the client may be messed up now.");

            //Make them fly so that they don't just fall
            AddToPhysicalScene(true, false);
            Velocity = Vector3.Zero;
            PhysicsActor.ForceSetPosition(m_pos);
            PhysicsActor.ForceSetVelocity(Vector3.Zero);
            SceneViewer.SendPresenceFullUpdate(this);

            if (ControllingClient != null)
                ControllingClient.SendAgentAlertMessage("Physics is having a problem with your avatar.  You may not be able to move until you relog.", true);
        }

        protected void PhysicsUpdatePosAndVelocity()
        {
            //Whenever the physics engine updates its positions, we get this update and make sure the animator has the newest info
            //Scene.SceneGraph.TaintPresenceForUpdate (this, PresenceTaint.Movement);
            if(Animator != null && m_parentID == UUID.Zero)
                Animator.UpdateMovementAnimations(true);
        }

        protected void UpdatePosAndVelocity()
        {
            //Whenever the physics engine updates its positions, we get this update and make sure the animator has the newest info
            if (Animator != null && m_parentID == UUID.Zero)
                Animator.UpdateMovementAnimations (true);
        }

        #region Cached Attachments (Internal Use Only!)

        private ISceneEntity[] m_cachedAttachments = new ISceneEntity[0];
        public void SetAttachments(ISceneEntity[] groups)
        {
            m_cachedAttachments = groups;
        }

        #endregion

        private static readonly UUID SoundWoodCollision = new UUID("063c97d3-033a-4e9b-98d8-05c8074922cb");
        // Event called by the physics plugin to tell the avatar about a collision.
        protected virtual void PhysicsCollisionUpdate (EventArgs e)
        {
            if (e == null)
                return;

            CollisionEventUpdate collisionData = (CollisionEventUpdate)e;
            Dictionary<uint, ContactPoint> coldata = collisionData.m_objCollisionList;

            if (coldata.Keys.Count > 0)
            {
                //Fire events for attachments
                foreach (ISceneEntity grp in m_cachedAttachments)
                {
                    grp.FireAttachmentCollisionEvents (e);
                }
            }

            //This is only used for collision sounds, which we have disabled ATM because they hit the client hard
            //add the items that started colliding this time to the last colliders list.
            foreach (uint localID in coldata.Keys)
            {
                //Play collision sounds
                ISceneChildEntity child;
                if (localID != 0 && CollisionSoundID == UUID.Zero && !IsChildAgent && (child = Scene.GetSceneObjectPart(localID)) != null &&
                    child.ParentEntity.RootChild.PhysActor != null && child.ParentEntity.RootChild.PhysActor.IsPhysical)
                {
                    CollisionSoundID = SoundWoodCollision;
                    break;
                }
            }

            if (coldata.Count != 0 && Animator != null)
            {
                //If we are on the ground, we need to fix the collision plane for the avie (fixes their feet in the viewer)
                switch (Animator.CurrentMovementAnimation)
                {
                    case "STAND":
                    case "WALK":
                    case "RUN":
                    case "CROUCH":
                    case "CROUCHWALK":
                        {
                            ContactPoint lowest;
                            lowest.SurfaceNormal = Vector3.Zero;
                            lowest.Position = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue)
                                                  {Z = Single.NaN};

                            //Find the lowest contact to use first
                            foreach (ContactPoint contact in coldata.Values)
                            {
                                if (Single.IsNaN(lowest.Position.Z) || contact.Position.Z != 0 && contact.Position.Z < lowest.Position.Z)
                                {
                                    if (contact.Type != ActorTypes.Agent)
                                        lowest = contact;
                                }
                            }

                            //Then if the normal isn't zero, set it (if its zero, it tends to do odd things in the client)
                            if (lowest.Position != new Vector3(float.MaxValue, float.MaxValue, float.MaxValue))
                            {
                                Vector4 newPlane = new Vector4(-lowest.SurfaceNormal, -Vector3.Dot(lowest.Position, lowest.SurfaceNormal));
                                //if (lowest.SurfaceNormal != Vector3.Zero)//Generates a 0,0,0,0, which is bad for the client
                                if (!CollisionPlane.ApproxEquals (newPlane, 0.5f))
                                {
                                    if (PhysicsActor != null && PhysicsActor.IsColliding)
                                    {
                                        CollisionPlane = newPlane;
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        public virtual void PushForce (Vector3 impulse)
        {
            if (PhysicsActor != null)
            {
                PhysicsActor.AddForce(impulse, true);
            }
        }

        #endregion
    }
}
