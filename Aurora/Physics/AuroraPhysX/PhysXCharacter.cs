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
using System.Reflection;
using OpenMetaverse;
//using Ode.NET;
using StillDesign.PhysX.MathPrimitives;
using StillDesign.PhysX;
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;
using log4net;

namespace Aurora.Physics.AuroraPhysXEngine
{
    public class AuroraPhysXCharacter : PhysicsCharacter
    {
        #region Declare

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Vector3 _position;
        private Vector3 m_lastPosition;
        private d.Vector3 _zeroPosition;
        // private d.Matrix3 m_StandUpRotation;
        private Vector3 _velocity;
        private Vector3 m_lastVelocity;
        private Vector3 _target_velocity;
//        private Quaternion _orientation;
//        private Quaternion _lastorientation;
        private Vector3 m_rotationalVelocity;
        //private Vector3 m_lastRotationalVelocity;

        private bool _zeroFlag = false;
        private bool _wasZeroFlagFlying = false;
        private bool m_ZeroUpdateSent = false;

        float m_UpdateTimecntr = 0;
        float m_UpdateFPScntr = 0.05f;
        private bool m_isJumping = false;
        public override bool IsJumping
        {
            get { return m_isJumping; }
        }
        private int m_preJumpCounter = 0;
        private bool m_ispreJumping = false;
        private Vector3 m_preJumpForce = Vector3.Zero;
        public override bool IsPreJumping
        {
            get { return m_ispreJumping; }
        }

        private float m_speedModifier = 1.0f;
        public override float SpeedModifier
        {
            get { return m_speedModifier; }
            set { m_speedModifier = value; }
        }

        private float m_mass = 80f;
        private bool m_pidControllerActive = true;
        //private static float POSTURE_SERVO = 10000.0f;
        public float CAPSULE_RADIUS = 0.37f;
        public float CAPSULE_LENGTH = 2.140599f;
        private bool flying = false;
        private bool m_iscolliding = false;

        int m_colliderfilter = 0;

        private bool m_alwaysRun = false;
        private Vector3 m_taintPosition = Vector3.Zero;
        private Quaternion m_taintRotation = Quaternion.Identity;
        public uint m_localID = 0;
        // taints and their non-tainted counterparts
        public bool m_isPhysical = false; // the current physical status
        public bool m_tainted_isPhysical = false; // set when the physical status is tainted (false=not existing in physics engine, true=existing)
        public float MinimumGroundFlightOffset = 3f;
        
        private float lastUnderwaterPush = 0;
        private bool WasUnderWater = false;
        private bool ShouldBeWalking = true;
        private bool StartingUnderWater = true;
                    
        private float m_tainted_CAPSULE_LENGTH; // set when the capsule length changes. 
        private float AvatarHalfsize;


        private float m_buoyancy = 0f;

        private string m_name = String.Empty;
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        // Default we're a Character
        private CollisionCategories m_collisionCategories = (CollisionCategories.Character);

        // Default, Collide with Other Geometries, spaces, bodies and characters.
        private CollisionCategories m_collisionFlags = (CollisionCategories.Geom
                                                        | CollisionCategories.Space
                                                        | CollisionCategories.Body
                                                        | CollisionCategories.Character
//                                                        | CollisionCategories.Land
                                                        );
        public IntPtr Body = IntPtr.Zero;
        private AuroraODEPhysicsScene _parent_scene;
        public IntPtr Shell = IntPtr.Zero;
        public IntPtr Amotor = IntPtr.Zero;
        public d.Mass ShellMass;

        private CollisionEventUpdate CollisionEventsThisFrame = new CollisionEventUpdate();

        // unique UUID of this character object
        public UUID m_uuid;
        public bool bad = false;

        private float PID_P;
        private float PID_D;

        #endregion

        #region Constructor

        public AuroraPhysXCharacter (String avName, AuroraPhysXPhysicsScene parent_scene, Vector3 pos, Quaternion rotation, Vector3 size)
        {
            m_uuid = UUID.Random();

            m_taintRotation = rotation;
//            _orientation = rotation;
//            _lastorientation = rotation;

            if (pos.IsFinite())
            {
                if (pos.Z > 9999999f || pos.Z <-90f)
                {
//                    pos.Z = parent_scene.GetTerrainHeightAtXY(127, 127) + 5;
                    pos.Z = parent_scene.GetTerrainHeightAtXY(parent_scene.Region.RegionSizeX * 0.5f, parent_scene.Region.RegionSizeY * 0.5f) + 5.0f;
                }
                _position = pos;
                m_taintPosition.X = pos.X;
                m_taintPosition.Y = pos.Y;
                m_taintPosition.Z = pos.Z;
            }
            else
            {
                _position.X = (float)parent_scene.Region.RegionSizeX * 0.5f;
                _position.Y = (float)parent_scene.Region.RegionSizeY * 0.5f;
                _position.Z = parent_scene.GetTerrainHeightAtXY(_position.X, _position.Y) + 10f;
                    
                m_taintPosition.X = _position.X;
                m_taintPosition.Y = _position.Y;
                m_taintPosition.Z = _position.Z;
                m_log.Warn("[PHYSICS]: Got NaN Position on Character Create");
            }

            _parent_scene = parent_scene;

            CAPSULE_RADIUS = parent_scene.avCapRadius;

            // m_StandUpRotation =
            //     new d.Matrix3(0.5f, 0.7071068f, 0.5f, -0.7071068f, 0f, 0.7071068f, 0.5f, -0.7071068f,
            //                   0.5f);

            CAPSULE_LENGTH = (size.Z * 1.1f) - CAPSULE_RADIUS * 2.0f;

            if ((m_collisionFlags & CollisionCategories.Land) == 0)
                AvatarHalfsize = CAPSULE_LENGTH * 0.5f + CAPSULE_RADIUS;
            else
                AvatarHalfsize = CAPSULE_LENGTH * 0.5f + CAPSULE_RADIUS - 0.3f;

            //m_log.Info("[SIZE]: " + CAPSULE_LENGTH.ToString());
            m_tainted_CAPSULE_LENGTH = CAPSULE_LENGTH;

            m_isPhysical = false; // current status: no ODE information exists
            m_tainted_isPhysical = true; // new tainted status: need to create ODE information

            _parent_scene.AddPhysicsActorTaint(this);

            m_UpdateTimecntr = 0;
            m_UpdateFPScntr = 2.5f * parent_scene.StepTime; // this parameter needs retunning and possible came from ini file
            if (m_UpdateTimecntr > .1f) // try to keep it under 100ms
                m_UpdateTimecntr = .1f;
            m_name = avName;
        }

        #endregion

        #region Properties

        public override int PhysicsActorType
        {
            get { return (int) ActorTypes.Agent; }
        }

        /// <summary>
        /// If this is set, the avatar will move faster
        /// </summary>
        public override bool SetAlwaysRun
        {
            get { return m_alwaysRun; }
            set { m_alwaysRun = value; }
        }

        public override uint LocalID
        {
            get { return m_localID; }
            set { m_localID = value; }
        }

        public override float Buoyancy
        {
            get { return m_buoyancy; }
            set { m_buoyancy = value; }
        }

        public override bool FloatOnWater
        {
            set { return; }
        }

        private bool m_shouldBePhysical = true;
        public override bool IsPhysical
        {
            get
            {
                return m_shouldBePhysical;
            }
            set
            {
                m_shouldBePhysical = value;
            }
        }

        public override bool ThrottleUpdates
        {
            get { return false; }
            set { return; }
        }

        public override bool Flying
        {
            get { return flying; }
            set { flying = value; }
        }

        /// <summary>
        /// Returns if the avatar is colliding in general.
        /// This includes the ground and objects and avatar.
        /// in this and next collision sets there is a general set to false
        /// at begin of loop, so a false is 2 sets while a true is a false plus a 1
        /// </summary>
        public override bool IsColliding
        {
            get { return m_iscolliding; }
            set
            {
                if (value)
                {
                    m_colliderfilter += 2;
                    if (m_colliderfilter > 2)
                        m_colliderfilter = 2;
                }
                else
                {
                    m_colliderfilter--;
                    if (m_colliderfilter < 0)
                        m_colliderfilter = 0;
                }

                if (m_colliderfilter == 0)
                    m_iscolliding = false;
                else
                    m_iscolliding = true;
            }
        }

        /// <summary>
        /// turn the PID controller on or off.
        /// The PID Controller will turn on all by itself in many situations
        /// </summary>
        /// <param name="status"></param>
        public void SetPidStatus(bool status)
        {
            m_pidControllerActive = status;
        }

        /// <summary>
        /// This 'puts' an avatar somewhere in the physics space.
        /// Not really a good choice unless you 'know' it's a good
        /// spot otherwise you're likely to orbit the avatar.
        /// </summary>
        public override Vector3 Position
        {
            get { return _position; }
            set
            {
                if (Body == IntPtr.Zero || Shell == IntPtr.Zero)
                {
                    if (value.IsFinite())
                    {
                        if (value.Z > 9999999f || value.Z <-90f)
                        {
                            value.Z = _parent_scene.GetTerrainHeightAtXY(_parent_scene.Region.RegionSizeX * 0.5f, _parent_scene.Region.RegionSizeY * 0.5f) + 5;
                        }

                        _position.X = value.X;
                        _position.Y = value.Y;
                        _position.Z = value.Z;

                        m_taintPosition.X = value.X;
                        m_taintPosition.Y = value.Y;
                        m_taintPosition.Z = value.Z;
                        _parent_scene.AddPhysicsActorTaint(this);
                    }
                    else
                    {
                        m_log.Warn("[PHYSICS]: Got a NaN Position from Scene on a Character");
                    }
                }
            }
        }

        public override Vector3 RotationalVelocity
        {
            get { return m_rotationalVelocity; }
            set { m_rotationalVelocity = value; }
        }

        /// <summary>
        /// This property sets the height of the avatar only.  We use the height to make sure the avatar stands up straight
        /// and use it to offset landings properly
        /// </summary>
        public override Vector3 Size
        {
            get { return new Vector3(CAPSULE_RADIUS * 2, CAPSULE_RADIUS * 2, CAPSULE_LENGTH); }
            set
            {
                if (value.IsFinite())
                {
                    Vector3 SetSize = value;

                    if (((SetSize.Z * 1.1f) - CAPSULE_RADIUS * 2.0f) == CAPSULE_LENGTH)
                    {
                        //It is the same, do not rebuild
                        m_log.Info("[Physics]: Not rebuilding the avatar capsule, as it is the same size as the previous capsule.");
                        return;
                    }

                    m_pidControllerActive = true;

                    m_tainted_CAPSULE_LENGTH = (SetSize.Z * 1.1f) - CAPSULE_RADIUS * 2.0f;
                    if ((m_collisionFlags & CollisionCategories.Land) == 0)
                        AvatarHalfsize = CAPSULE_LENGTH * 0.5f + CAPSULE_RADIUS;
                    else
                        AvatarHalfsize = CAPSULE_LENGTH * 0.5f + CAPSULE_RADIUS - 0.3f;
                    //m_log.Info("[RESIZE]: " + m_tainted_CAPSULE_LENGTH.ToString());

                    Velocity = Vector3.Zero;

                    _parent_scene.AddPhysicsActorTaint(this);
                }
                else
                {
                    m_log.Warn("[PHYSICS]: Got a NaN Size from Scene on a Character");
                }
            }
        }

        //
        /// <summary>
        /// Uses the capped cyllinder volume formula to calculate the avatar's mass.
        /// This may be used in calculations in the scene/scenepresence
        /// </summary>
        public override float Mass
        {
            get
            {
                return m_mass;
            }
        }

        public override Vector3 Force
        {
            get { return _target_velocity; }
            set { return; }
        }

        public override Vector3 CenterOfMass
        {
            get { return Vector3.Zero; }
        }

        public override Vector3 Velocity
        {
            get
            {
                // There's a problem with Vector3.Zero! Don't Use it Here!
                //if (_zeroFlag)
                //    return Vector3.Zero;
                //m_lastUpdateSent = false;
                return _velocity;
            }
            set
            {
                if (value.IsFinite())
                {
                    m_pidControllerActive = true;
                    _target_velocity = value;
                }
                else
                {
                    m_log.Warn("[PHYSICS]: Got a NaN velocity from Scene in a Character");
                }
            }
        }

        /// <summary>
        /// This adds to the force that will be used for moving the avatar in the next physics heartbeat iteration.
        /// </summary>
        /// <param name="force"></param>
        public override void AddMovementForce(Vector3 force)
        {
            _target_velocity += force;            
        }

        /// <summary>
        /// This sets the force that will be used for moving the avatar in the next physics heartbeat iteration.
        /// Note: we do accept Vector3.Zero here as that is an overriding stop for the physics engine.
        /// </summary>
        /// <param name="force"></param>
        public override void SetMovementForce(Vector3 force)
        {
            if (!Flying)
            {
                if (force.Z >= 0.5f)
                {
                    if (!_parent_scene.m_allowJump)
                        return;
                    if (_parent_scene.m_usepreJump)
                    {
                        m_ispreJumping = true;
                        m_preJumpForce = force;
                        m_preJumpCounter = 0;
                        TriggerMovementUpdate();
                        return;
                    }
                    else
                    {
                        m_isJumping = true;
                        m_preJumpCounter = _parent_scene.m_preJumpTime;
                        TriggerMovementUpdate();
                        force *= _parent_scene.m_preJumpForceMultiplier;
                    }
                }
            }
            if (m_ispreJumping)
            {
                TriggerMovementUpdate();
                return;
            }
            if(force != Vector3.Zero)
                _target_velocity = force;       
        }

        public override Vector3 Torque
        {
            get { return Vector3.Zero; }
            set { return; }
        }

        public override float CollisionScore
        {
            get { return 0f; }
            set { }
        }

        public override Quaternion Orientation
        {
            get { return m_taintRotation; }
            set
            {
                m_taintRotation = value;
                //Matrix3 or = Orientation.ToRotationMatrix();
                //d.Matrix3 ord = new d.Matrix3(or.m00, or.m10, or.m20, or.m01, or.m11, or.m21, or.m02, or.m12, or.m22);
                //d.BodySetRotation(Body, ref ord);
            }
        }

        #endregion

        #region Methods

        #region Rebuild the avatar representation


        /// <summary>
        /// This creates the Avatar's physical Surrogate at the position supplied
        /// WARNING: This MUST NOT be called outside of ProcessTaints, else we can have unsynchronized access
        /// to ODE internals. ProcessTaints is called from within thread-locked Simulate(), so it is the only 
        /// place that is safe to call this routine AvatarGeomAndBodyCreation.
        /// </summary>
        /// <param name="npositionX"></param>
        /// <param name="npositionY"></param>
        /// <param name="npositionZ"></param>
        private void AvatarGeomAndBodyCreation(float npositionX, float npositionY, float npositionZ)//, float tensor)
        {
            _parent_scene.waitForSpaceUnlock(_parent_scene.space);
            if (CAPSULE_LENGTH <= 0)
            {
                m_log.Warn("[PHYSICS]: The capsule size you specified in aurora.ini is invalid!  Setting it to the smallest possible size!");
                CAPSULE_LENGTH = 1.2f;

            }

            if (CAPSULE_RADIUS <= 0)
            {
                m_log.Warn("[PHYSICS]: The capsule size you specified in aurora.ini is invalid!  Setting it to the normal size!");
                CAPSULE_RADIUS = 0.37f;

            }
            Shell = d.CreateCapsule(_parent_scene.space, CAPSULE_RADIUS, CAPSULE_LENGTH);
            
            d.GeomSetCategoryBits(Shell, (int)m_collisionCategories);
            d.GeomSetCollideBits(Shell, (int)m_collisionFlags);

            d.MassSetCapsule(out ShellMass, 150f, 3, CAPSULE_RADIUS, CAPSULE_LENGTH); // density 200

            m_mass=ShellMass.mass;

            // rescale PID parameters 
            PID_D = _parent_scene.PID_D;
            PID_P = _parent_scene.PID_P;


            // rescale PID parameters so that this aren't so affected by mass
            // but more importante, don't get unstable

            PID_D /= 50 * 80; // original mass of 80, 50 ODE fps ??
            PID_D *= m_mass / _parent_scene.ODE_STEPSIZE;
            PID_P /= 50 * 80;
            PID_P *= m_mass / _parent_scene.ODE_STEPSIZE;          
            
            Body = d.BodyCreate(_parent_scene.world);
            
            d.BodySetPosition(Body, npositionX, npositionY, npositionZ);

            // disconnect from world gravity so we can apply buoyancy
            d.BodySetGravityMode(Body, false);
            d.BodySetAutoDisableFlag(Body, false);

            _position.X = npositionX;
            _position.Y = npositionY;
            _position.Z = npositionZ;

            m_taintPosition.X = _position.X;
            m_taintPosition.Y = _position.Y;
            m_taintPosition.Z = _position.Z;

            d.BodySetMass(Body, ref ShellMass);

            d.Matrix3 m_caprot;
            // 90 Stand up on the cap of the capped cyllinder
            m_taintRotation = new Quaternion(0,0,1,(float)(Math.PI / 2));
            d.RFromAxisAndAngle(out m_caprot, m_taintRotation.X, m_taintRotation.Y, m_taintRotation.Z, m_taintRotation.W);

            d.GeomSetRotation (Shell, ref m_caprot);     
            d.GeomSetBody(Shell, Body);


            // The purpose of the AMotor here is to keep the avatar's physical
            // surrogate from rotating while moving
            Amotor = d.JointCreateAMotor (_parent_scene.world, IntPtr.Zero);
            d.JointAttach (Amotor, Body, IntPtr.Zero);
            int dAMotorEuler = 1;
            d.JointSetAMotorMode (Amotor, dAMotorEuler);
            d.JointSetAMotorNumAxes (Amotor, 3);
            d.JointSetAMotorAxis (Amotor, 0, 0, 1, 0, 0);
            d.JointSetAMotorAxis (Amotor, 1, 0, 0, 1, 0);
            d.JointSetAMotorAxis (Amotor, 2, 0, 0, 0, 1);
            d.JointSetAMotorAngle (Amotor, 0, 0);
            d.JointSetAMotorAngle (Amotor, 1, 0);
            d.JointSetAMotorAngle (Amotor, 2, 0);

            // These lowstops and high stops are effectively (no wiggle room)
            //if (_parent_scene.IsAvCapsuleTilted)
            /*{
                d.JointSetAMotorParam (Amotor, (int)dParam.LowStop, -0.000000000001f);
                d.JointSetAMotorParam (Amotor, (int)dParam.LoStop3, -0.000000000001f);
                d.JointSetAMotorParam (Amotor, (int)dParam.LoStop2, -0.000000000001f);
                d.JointSetAMotorParam (Amotor, (int)dParam.HiStop, 0.000000000001f);
                d.JointSetAMotorParam (Amotor, (int)dParam.HiStop3, 0.000000000001f);
                d.JointSetAMotorParam (Amotor, (int)dParam.HiStop2, 0.000000000001f);
            }
            else*/
            {
                #region Documentation of capsule motor LowStop and HighStop parameters
                // Intentionally introduce some tilt into the capsule by setting
                // the motor stops to small epsilon values. This small tilt prevents
                // the capsule from falling into the terrain; a straight-up capsule
                // (with -0..0 motor stops) falls into the terrain for reasons yet
                // to be comprehended in their entirety.
                #endregion
                AlignAvatarTiltWithCurrentDirectionOfMovement (Vector3.Zero, new Vector3(0,0,-1));
                d.JointSetAMotorParam (Amotor, (int)dParam.LowStop, 0.08f);
                d.JointSetAMotorParam (Amotor, (int)dParam.LoStop3, -0f);
                d.JointSetAMotorParam (Amotor, (int)dParam.LoStop2, 0.08f);
                d.JointSetAMotorParam (Amotor, (int)dParam.HiStop, 0.08f); // must be same as lowstop, else a different, spurious tilt is introduced
                d.JointSetAMotorParam (Amotor, (int)dParam.HiStop3, 0f); // same as lowstop
                d.JointSetAMotorParam (Amotor, (int)dParam.HiStop2, 0.08f); // same as lowstop
            }

            // Fudge factor is 1f by default, we're setting it to 0.  We don't want it to Fudge or the
            // capped cyllinder will fall over
            d.JointSetAMotorParam (Amotor, (int)dParam.FudgeFactor, 0f);
            d.JointSetAMotorParam (Amotor, (int)dParam.FMax, 3800000f);
 
        }

        #endregion

        #region Move

        private void AlignAvatarTiltWithCurrentDirectionOfMovement (Vector3 movementVector, Vector3 gravity)
        {
            if (gravity == Vector3.Zero)
                gravity = new Vector3 (0, 0, -1f);
            movementVector.Z = 0f;
            float magnitude = (float)Math.Sqrt ((double)(movementVector.X * movementVector.X + movementVector.Y * movementVector.Y));
            if (magnitude < 0.1f)
                return;

            gravity.Normalize ();
            Quaternion rot = Vector3.RotationBetween (new Vector3 (0, 0, -1), gravity);
            // normalize the velocity vector
            float invMagnitude = 1.0f / magnitude;
            movementVector.X *= invMagnitude;
            movementVector.Y *= invMagnitude;

            movementVector *= rot;

            // if we change the capsule heading too often, the capsule can fall down
            // therefore we snap movement vector to just 1 of 4 predefined directions (ne, nw, se, sw),
            // meaning only 4 possible capsule tilt orientations
            if (movementVector.X > 0)
            {
                // east
                if (movementVector.Y > 0)
                {
                    // northeast
                    movementVector.X = (float)Math.Sqrt (2.0);
                    movementVector.Y = (float)Math.Sqrt (2.0);
                }
                else
                {
                    // southeast
                    movementVector.X = (float)Math.Sqrt (2.0);
                    movementVector.Y = -(float)Math.Sqrt (2.0);
                }
            }
            else
            {
                // west
                if (movementVector.Y > 0)
                {
                    // northwest
                    movementVector.X = -(float)Math.Sqrt (2.0);
                    movementVector.Y = (float)Math.Sqrt (2.0);
                }
                else
                {
                    // southwest
                    movementVector.X = -(float)Math.Sqrt (2.0);
                    movementVector.Y = -(float)Math.Sqrt (2.0);
                }
            }


            // movementVector.Z is zero

            // calculate tilt components based on desired amount of tilt and current (snapped) heading.
            // the "-" sign is to force the tilt to be OPPOSITE the direction of movement.
            float xTiltComponent = -movementVector.X * 0.1131371f;
            float yTiltComponent = -movementVector.Y * 0.1131371f;
            float zTiltComponent = -movementVector.Z * 0.1131371f;

            //m_log.Debug("[PHYSICS] changing avatar tilt");
            d.JointSetAMotorParam (Amotor, (int)dParam.LowStop, xTiltComponent);
            d.JointSetAMotorParam (Amotor, (int)dParam.HiStop, xTiltComponent); // must be same as lowstop, else a different, spurious tilt is introduced
            d.JointSetAMotorParam (Amotor, (int)dParam.LoStop2, yTiltComponent);
            d.JointSetAMotorParam (Amotor, (int)dParam.HiStop2, yTiltComponent); // same as lowstop
            d.JointSetAMotorParam (Amotor, (int)dParam.LoStop3, zTiltComponent);
            d.JointSetAMotorParam (Amotor, (int)dParam.HiStop3, zTiltComponent); // same as lowstop
        }
      
        /// <summary>
        /// Called from Simulate
        /// This is the avatar's movement control + PID Controller
        /// </summary>
        /// <param name="timeStep"></param>
        public void Move (float timeStep, ref List<AuroraODECharacter> defects)
        {
            //  no lock; for now it's only called from within Simulate()

            // If the PID Controller isn't active then we set our force
            // calculating base velocity to the current position

            if (Body == IntPtr.Zero)
                return;

            if (!m_shouldBePhysical)
                return;

            Vector3 vec = Vector3.Zero;
            d.Vector3 vel = d.BodyGetLinearVel (Body);
            d.Vector3 tempPos = d.BodyGetPosition (Body);

            #region Flight Ceiling

            // rex, added height check

            if (m_pidControllerActive == false)
            {
                _zeroPosition = d.BodyGetPosition (Body);
            }

            if (_parent_scene.m_useFlightCeilingHeight && tempPos.Z > _parent_scene.m_flightCeilingHeight)
            {
                tempPos.Z = _parent_scene.m_flightCeilingHeight;
                d.BodySetPosition (Body, tempPos.X, tempPos.Y, tempPos.Z);
                d.Vector3 tempVel = d.BodyGetLinearVel (Body);
                if (tempVel.Z > 0.0f)
                {
                    tempVel.Z = 0.0f;
                    d.BodySetLinearVel (Body, tempVel.X, tempVel.Y, tempVel.Z);
                }
                if (_target_velocity.Z > 0.0f)
                    _target_velocity.Z = 0.0f;
            }

            // endrex

            #endregion

            #region NonFinite Pos

            Vector3 localPos = new Vector3 ((float)tempPos.X, (float)tempPos.Y, (float)tempPos.Z);

            if (!localPos.IsFinite ())
            {
                m_log.Warn ("[PHYSICS]: Avatar Position is non-finite!");
                defects.Add (this);
                // _parent_scene.RemoveCharacter(this);

                // destroy avatar capsule and related ODE data
                
                if (Amotor != IntPtr.Zero)
                {
                    // Kill the Amotor
                    d.JointDestroy(Amotor);
                    Amotor = IntPtr.Zero;
                }
                
                //kill the Geometry
                _parent_scene.waitForSpaceUnlock (_parent_scene.space);

                if (Body != IntPtr.Zero)
                {
                    //kill the body
                    d.BodyDestroy (Body);

                    Body = IntPtr.Zero;
                }

                if (Shell != IntPtr.Zero)
                {
                    d.GeomDestroy (Shell);
                    Shell = IntPtr.Zero;
                }
                return;
            }

            #endregion

            #region Check for out of region

            if (Position.X < 0.25f || Position.Y < 0.25f ||
                Position.X > _parent_scene.Region.RegionSizeX - .25f ||
                Position.Y > _parent_scene.Region.RegionSizeY - .25f)
            {
                if (!CheckForRegionCrossing())
                {
                    Vector3 newPos = Position;
                    newPos.X = Util.Clip(Position.X, 0.75f, _parent_scene.Region.RegionSizeX - 0.75f);
                    newPos.Y = Util.Clip(Position.Y, 0.75f, _parent_scene.Region.RegionSizeY - 0.75f);
                    Position = newPos;
                    d.BodySetPosition(Body, newPos.X, newPos.Y, newPos.Z);
                }
            }

            #endregion

            #region Movement Multiplier

            float movementmult = 1f;
            if (!m_alwaysRun)
                movementmult /= _parent_scene.avMovementDivisorWalk;
            else
                movementmult /= _parent_scene.avMovementDivisorRun;

            movementmult *= 10;
            movementmult *= SpeedModifier;
            movementmult *= 1 / _parent_scene.TimeDilation;
            if (flying)
                movementmult *= _parent_scene.m_AvFlySpeed;

            #endregion

            #region Check for underground

            bool groundCollision = false;
            if (!(Position.X < 0.25f || Position.Y < 0.25f ||
                Position.X > _parent_scene.Region.RegionSizeX - .25f ||
                Position.Y > _parent_scene.Region.RegionSizeY - .25f))
            {
                float groundHeight = _parent_scene.GetTerrainHeightAtXY (
                           tempPos.X + (tempPos.X == 0 ? tempPos.X : timeStep * 0.75f * vel.X),
                           tempPos.Y + (tempPos.Y == 0 ? tempPos.Y : timeStep * 0.75f * vel.Y));

                if ((tempPos.Z - AvatarHalfsize) < groundHeight)
                {
                    if (!flying)
                    {
                        //if (_target_velocity.Z < 0)
                        _target_velocity.Z = 0;
                        vec.Z = -vel.Z * PID_D * 1.5f + ((groundHeight - (tempPos.Z - AvatarHalfsize)) * PID_P * 100.0f);
                    }
                    else
                        vec.Z = ((groundHeight - (tempPos.Z - AvatarHalfsize)) * PID_P);
                }
                if (tempPos.Z - AvatarHalfsize - groundHeight < 0.12f)
                {
                    m_iscolliding = true;
                    groundCollision = true;
                    flying = false; // ground the avatar
                    ContactPoint point = new ContactPoint ();
                    point.PenetrationDepth = vel.Z;
                    point.Position = Position;
                    point.SurfaceNormal = Vector3.Zero;

                    //0 is the ground localID
                    AddCollisionEvent (0, point);
                }
            }

            #endregion

            #region Movement

            #region Jump code

            if (IsJumping && (IsColliding) && m_preJumpCounter > _parent_scene.m_preJumpTime || m_preJumpCounter > 150)
            {
                m_isJumping = false;
                m_preJumpCounter = 0;
            }
            if (IsJumping)
                m_preJumpCounter++;

            if (m_ispreJumping && m_preJumpCounter == _parent_scene.m_preJumpTime)
            {
                m_ispreJumping = false;
                //if (!m_iscolliding)
                //    vel.Z = -0.6f;
                if (!m_iscolliding)//not Ground collision, as we cleared it out before calling this, and the ground allows normal jumps
                {
                    _target_velocity.X += m_preJumpForce.X * _parent_scene.m_preJumpForceMultiplier * 7;
                    _target_velocity.Y += m_preJumpForce.Y * _parent_scene.m_preJumpForceMultiplier * 7;
                }
                _target_velocity.Z += m_preJumpForce.Z * _parent_scene.m_preJumpForceMultiplier;
                
                m_preJumpCounter = 0;
                m_isJumping = true;
            }
            if (m_ispreJumping)
            {
                m_preJumpCounter++;
                TriggerMovementUpdate();
                return;
            }
            //This is for jumping on prims, since otherwise, you don't get off the ground sometimes
            if (m_iscolliding && m_isJumping && _target_velocity.Z < 1 && !Flying)
                _target_velocity.Z = m_preJumpForce.Z * _parent_scene.m_preJumpForceMultiplier;

            #endregion

            Vector3 gravForce = new Vector3 ();

            #region Gravity

            /*if (!flying)
                _parent_scene.CalculateGravity (m_mass, tempPos, true, 1, ref gravForce);
            else
                _parent_scene.CalculateGravity (m_mass, tempPos, false, 0.75f, ref gravForce);//Allow point gravity and repulsors affect us a bit

            Vector3 gravCopy = gravForce;
            gravCopy.Normalize ();
            Quaternion rotDiff = Vector3.RotationBetween (new Vector3 (0, 0, -1), gravCopy);
            if (rotDiff != Quaternion.Identity)
            {
                rotDiff.Normalize ();
            }
            //_target_velocity *= rotDiff;*/

            #endregion

            //  if velocity is zero, use position control; otherwise, velocity control
            if (_target_velocity == Vector3.Zero &&
                Math.Abs (vel.X) < 0.05 && Math.Abs (vel.Y) < 0.05 && Math.Abs (vel.Z) < 0.05 && (this.m_iscolliding || this.flying || (this._zeroFlag && _wasZeroFlagFlying == flying)))
            //This is so that if we get moved by something else, it will update us in the client
            {
                m_isJumping = false;
                //  keep track of where we stopped.  No more slippin' & slidin'
                if (!_zeroFlag)
                {
                    _zeroFlag = true;
                    _wasZeroFlagFlying = flying;
                    _zeroPosition = tempPos;
                }

                if (m_pidControllerActive)
                {
                    // We only want to deactivate the PID Controller if we think we want to have our surrogate
                    // react to the physics scene by moving it's position.
                    // Avatar to Avatar collisions
                    // Prim to avatar collisions
                    // if target vel is zero why was it here ?
                    vec.X = -vel.X * PID_D + (_zeroPosition.X - tempPos.X) * PID_P;
                    vec.Y = -vel.Y * PID_D + (_zeroPosition.Y - tempPos.Y) * PID_P;
                }
            }
            else
            {
                m_pidControllerActive = true;
                _zeroFlag = false;

                if (m_iscolliding)
                {
                    if (!flying)
                    {
                        // We're standing or walking on something
                        if (_target_velocity.X != 0.0f)
                            vec.X = (_target_velocity.X * movementmult - vel.X) * PID_D * 2;
                        if (_target_velocity.Y != 0.0f)
                            vec.Y = (_target_velocity.Y * movementmult - vel.Y) * PID_D * 2;
                        if (_target_velocity.Z != 0.0f)
                            vec.Z = (_target_velocity.Z * movementmult - vel.Z) * PID_D;// + (_zeroPosition.Z - tempPos.Z) * PID_P)) _zeropos maybe bad here
                    }
                    else
                    {
                        // We're flying and colliding with something
                        vec.X = (_target_velocity.X * movementmult - vel.X) * PID_D * 0.5f;
                        vec.Y = (_target_velocity.Y * movementmult - vel.Y) * PID_D * 0.5f;
                    }
                }
                else
                {
                    if (flying)
                    {
                        // we're flying
                        vec.X = (_target_velocity.X * movementmult - vel.X) * PID_D * 0.75f;
                        vec.Y = (_target_velocity.Y * movementmult - vel.Y) * PID_D * 0.75f;
                    }
                    else
                    {
                        // we're not colliding and we're not flying so that means we're falling!
                        // m_iscolliding includes collisions with the ground.
                        vec.X = (_target_velocity.X - vel.X) * PID_D * 0.85f;
                        vec.Y = (_target_velocity.Y - vel.Y) * PID_D * 0.85f;
                    }
                }

                #region Gravity

                if (!flying && groundCollision)//No ground collisions! Otherwise, we bounce on terrain
                    _parent_scene.CalculateGravity (m_mass, tempPos, true, 0.45f, ref gravForce);
                else if (!flying)
                    _parent_scene.CalculateGravity (m_mass, tempPos, true, 1, ref gravForce);
                else
                    _parent_scene.CalculateGravity (m_mass, tempPos, false, 0.75f, ref gravForce);//Allow point gravity and repulsors affect us a bit

                vec += gravForce;

                #endregion

                if (flying)
                {
                    #region Av gravity

                    if (_parent_scene.AllowAvGravity &&
                        tempPos.Z > _parent_scene.AvGravityHeight) //Should be stop avies from flying upwards
                    {
                        //Decay going up 
                        if (_target_velocity.Z > 0)
                        {
                            //How much should we force them down?
                            float Multiplier = (_parent_scene.AllowAvsToEscapeGravity ? .03f : .1f);
                            //How much should we force them down?
                            float fudgeHeight = (_parent_scene.AllowAvsToEscapeGravity ? 80 : 30);
                            //We add the 30 so that gravity is resonably strong once they pass the min height
                            Multiplier *= tempPos.Z + fudgeHeight - _parent_scene.AvGravityHeight;

                            //Limit these so that things don't go wrong
                            if (Multiplier < 1)
                                Multiplier = 1;

                            float maxpower = (_parent_scene.AllowAvsToEscapeGravity ? 1.5f : 3f);

                            if (Multiplier > maxpower)
                                Multiplier = maxpower;

                            _target_velocity.Z /= Multiplier;
                            vel.Z /= Multiplier;
                        }
                    }

                    #endregion

                    vec.Z = (_target_velocity.Z * movementmult - vel.Z) * PID_D * 0.5f;
                    if (_parent_scene.AllowAvGravity && tempPos.Z > _parent_scene.AvGravityHeight)
                        //Add extra gravity
                        vec.Z += ((10 * _parent_scene.gravityz) * Mass);
                }
            }

            if (flying)
            {
                #region Auto Fly Height

                //Added for auto fly height. Kitto Flora
                //Changed to only check if the avatar is flying around,

                // Revolution: If the avatar is going down, they are trying to land (probably), so don't push them up to make it harder
                //   Only if they are moving around sideways do we need to push them up
                if (_target_velocity.X != 0 || _target_velocity.Y != 0)
                {
                    Vector3 forwardVel = new Vector3 (_target_velocity.X > 0 ? 2 : (_target_velocity.X < 0 ? -2 : 0),
                        _target_velocity.Y > 0 ? 2 : (_target_velocity.Y < 0 ? -2 : 0),
                        0);
                    float target_altitude = _parent_scene.GetTerrainHeightAtXY (tempPos.X, tempPos.Y) + MinimumGroundFlightOffset;

                    //We cheat a bit and do a bit lower than normal
                    if ((tempPos.Z - CAPSULE_LENGTH) < target_altitude ||
                            (tempPos.Z - CAPSULE_LENGTH) < _parent_scene.GetTerrainHeightAtXY (tempPos.X + forwardVel.X, tempPos.Y + forwardVel.Y)
                            + MinimumGroundFlightOffset)
                        vec.Z += (target_altitude - tempPos.Z) * PID_P * 0.5f;
                }
                else
                {
                    //Straight up and down, only apply when they are very close to the ground
                    float target_altitude = _parent_scene.GetTerrainHeightAtXY (tempPos.X, tempPos.Y);

                    if ((tempPos.Z - CAPSULE_LENGTH + (MinimumGroundFlightOffset / 1.5)) < target_altitude + MinimumGroundFlightOffset)
                    {
                        if ((tempPos.Z - CAPSULE_LENGTH) < target_altitude + 1)
                        {
                            vec.Z += ((target_altitude + 4) - (tempPos.Z - CAPSULE_LENGTH)) * PID_P;
                        }
                        else
                            vec.Z += ((target_altitude + MinimumGroundFlightOffset) - (tempPos.Z - CAPSULE_LENGTH)) * PID_P * 0.5f;
                    }
                }

                #endregion
            }

            #region Under water physics

            if (_parent_scene.AllowUnderwaterPhysics && (float)tempPos.X < _parent_scene.Region.RegionSizeX &&
                (float)tempPos.Y < _parent_scene.Region.RegionSizeY)
            {
                //Position plus height to av's shoulder (aprox) is just above water
                if ((tempPos.Z + (CAPSULE_LENGTH / 3) - .25f) < _parent_scene.GetWaterLevel ((float)tempPos.X, (float)tempPos.Y))
                {
                    if (StartingUnderWater)
                        ShouldBeWalking = Flying = false;
                    StartingUnderWater = false;
                    WasUnderWater = true;
                    Flying = true;
                    lastUnderwaterPush = 0;
                    if (ShouldBeWalking)
                    {
                        lastUnderwaterPush += (float)(_parent_scene.GetWaterLevel ((float)tempPos.X, (float)tempPos.Y) - tempPos.Z) * 33 + 3;
                        vec.Z += lastUnderwaterPush;
                    }
                    else
                    {
                        lastUnderwaterPush += 3500;
                        lastUnderwaterPush += (float)(_parent_scene.GetWaterLevel ((float)tempPos.X, (float)tempPos.Y) - tempPos.Z) * 8;
                        vec.Z += lastUnderwaterPush;
                    }
                }
                else
                {
                    StartingUnderWater = true;
                    if (WasUnderWater)
                    {
                        WasUnderWater = false;
                        Flying = true;
                    }
                }
            }

            #endregion

            #endregion

            #region Apply the force

            if (vec.IsFinite ())
            {
                if (vec.X < 100000000 && vec.Y < 10000000 && vec.Z < 10000000) //Checks for crazy, going to NaN us values
                {
                    // round small values to zero. those possible are just errors
                    if (Math.Abs (vec.X) < 0.001)
                        vec.X = 0;
                    if (Math.Abs (vec.Y) < 0.001)
                        vec.Y = 0;
                    if (Math.Abs (vec.Z) < 0.001)
                        vec.Z = 0;

                    //ODE autodisables not moving prims, accept it and reenable when we need to
                    if (!d.BodyIsEnabled (Body))
                        d.BodyEnable (Body);

                    if (vec == Vector3.Zero) //if we arn't moving, STOP
                        d.BodySetLinearVel(Body, vec.X, vec.Y, vec.Z);
                    else
                        doForce (vec);
                     
                    if (!_zeroFlag)
                        AlignAvatarTiltWithCurrentDirectionOfMovement (vec, gravForce);

                    //When falling, we keep going faster and faster, and eventually, the client blue screens (blue is all you see).
                    // The speed that does this is slightly higher than -30, so we cap it here so we never do that during falling.
                    if (vel.Z < -30)
                    {
                        vel.Z = -30;
                        d.BodySetLinearVel (Body, vel.X, vel.Y, vel.Z);
                    }

                    //Decay out the target velocity
                    _target_velocity *= _parent_scene.m_avDecayTime;
                    if (!_zeroFlag && _target_velocity.ApproxEquals (Vector3.Zero, _parent_scene.m_avStopDecaying))
                        _target_velocity = Vector3.Zero;
                }
                else
                {
                    //This is a safe guard from going NaN, but it isn't very smooth... which is ok
                    d.BodySetForce (Body, 0, 0, 0);
                    d.BodySetLinearVel (Body, 0, 0, 0);
                }
            }
            else
            {
                m_log.Warn ("[PHYSICS]: Got a NaN force vector in Move()");
                m_log.Warn ("[PHYSICS]: Avatar Position is non-finite!");
                defects.Add (this);
                //kill the Geometry
                _parent_scene.waitForSpaceUnlock (_parent_scene.space);

                if (Body != IntPtr.Zero)
                {
                    //kill the body
                    d.BodyDestroy (Body);

                    Body = IntPtr.Zero;
                }

                if (Shell != IntPtr.Zero)
                {
                    d.GeomDestroy (Shell);
                    Shell = IntPtr.Zero;
                }
            }

            #endregion
        }

        /// <summary>
        /// Updates the reported position and velocity.  This essentially sends the data up to ScenePresence.
        /// </summary>
        public void UpdatePositionAndVelocity(float timestep)
        {
            if (!m_shouldBePhysical)
                return;

            //  no lock; called from Simulate() -- if you call this from elsewhere, gotta lock or do Monitor.Enter/Exit!
            d.Vector3 vec;
            try
            {
                vec = d.BodyGetPosition(Body);
            }
            catch (NullReferenceException)
            {
                bad = true;
                _parent_scene.BadCharacter(this);
                vec = new d.Vector3(_position.X, _position.Y, _position.Z);
                base.RaiseOutOfBounds(_position); // Tells ScenePresence that there's a problem!
                m_log.WarnFormat("[ODEPLUGIN]: Avatar Null reference for Avatar {0}, physical actor {1}", m_name, m_uuid);
            }

            // vec is a ptr into internal ode data better not mess with it

            _position.X = (float)vec.X;
            _position.Y = (float)vec.Y;
            _position.Z = (float)vec.Z;

            try
            {
                vec = d.BodyGetLinearVel(Body);
            }
            catch (NullReferenceException)
            {
                vec.X = _velocity.X;
                vec.Y = _velocity.Y;
                vec.Z = _velocity.Z;
            }


            // vec is a ptr into internal ode data better not mess with it

            _velocity.X = vec.X;
            _velocity.Y = vec.Y;
            _velocity.Z = vec.Z;

            bool VelIsZero = false;
            int vcntr = 0;
            if (Math.Abs(_velocity.X) < 0.01)
            {
                vcntr++;
                _velocity.X = 0;
            }
            if (Math.Abs(_velocity.Y) < 0.01)
            {
                vcntr++;
                _velocity.Y = 0;
            }
            if (Math.Abs(_velocity.Z) < 0.01)
            {
                vcntr++;
                _velocity.Z = 0;
            }
            if (vcntr == 3)
                VelIsZero = true;

            // slow down updates
            m_UpdateTimecntr += timestep;
            if (m_UpdateTimecntr < m_UpdateFPScntr)
                return;

            m_UpdateTimecntr = 0;

            const float VELOCITY_TOLERANCE = 0.1f;
            const float POSITION_TOLERANCE = 0.25f;
            bool needSendUpdate = false;

            //Check to see whether we need to trigger the significant movement method in the presence
            // avas don't rotate for now                if (!RotationalVelocity.ApproxEquals(m_lastRotationalVelocity, VELOCITY_TOLERANCE) ||
            // but simulator does not process rotation changes
            if (//!VelIsZero &&
                //                   (!Velocity.ApproxEquals(m_lastVelocity, VELOCITY_TOLERANCE) ||
                (
                (Math.Abs(Velocity.X - m_lastVelocity.X) > VELOCITY_TOLERANCE) ||
                (Math.Abs(Velocity.Y - m_lastVelocity.Y) > VELOCITY_TOLERANCE) ||
                (Math.Abs(Velocity.Z - m_lastVelocity.Z) > VELOCITY_TOLERANCE) ||
                (Math.Abs(_position.X - m_lastPosition.X) > POSITION_TOLERANCE) ||
                (Math.Abs(_position.Y - m_lastPosition.Y) > POSITION_TOLERANCE) ||
                (Math.Abs(_position.Z - m_lastPosition.Z) > POSITION_TOLERANCE)// ||
                //                    (Math.Abs(_lastorientation.X - _orientation.X) > 0.001) ||
                //                    (Math.Abs(_lastorientation.Y - _orientation.Y) > 0.001) ||
                //                    (Math.Abs(_lastorientation.Z - _orientation.Z) > 0.001) ||
                //                    (Math.Abs(_lastorientation.W - _orientation.W) > 0.001)
                ))
            {
                // Update the "last" values
                needSendUpdate = true;
                m_ZeroUpdateSent = false;
                m_lastPosition = _position;
                //                        m_lastRotationalVelocity = RotationalVelocity;
                m_lastVelocity = Velocity;
                //                            _lastorientation = Orientation;
                //                        base.RequestPhysicsterseUpdate();
                //                        base.TriggerSignificantMovement();
            }
            else if (VelIsZero)
            {
                if (!m_ZeroUpdateSent)
                {
                    needSendUpdate = true;
                    m_ZeroUpdateSent = true;
                }
            }

            if (needSendUpdate)
            {
                base.RequestPhysicsterseUpdate();
                base.TriggerSignificantMovement();
            }

            //Tell any listeners about the new info
            // This is for animations
            base.TriggerMovementUpdate ();
        }

        #endregion

        #region Unused code
/* suspended
        private void AlignAvatarTiltWithCurrentDirectionOfMovement(Vector3 movementVector)
            {
            if (!_parent_scene.IsAvCapsuleTilted)
                return;

            movementVector.Z = 0f;           

            if (movementVector == Vector3.Zero)
                {
                return;
                }

            // if we change the capsule heading too often, the capsule can fall down
            // therefore we snap movement vector to just 1 of 4 predefined directions (ne, nw, se, sw),
            // meaning only 4 possible capsule tilt orientations

            float sqr2 = 1.41421356f; // square root of 2  lasy to cut extra digits

            if (movementVector.X > 0)
                {
                movementVector.X = sqr2;

                // east ?? there is no east above
                if (movementVector.Y > 0)
                    {
                    // northeast
                    movementVector.Y = sqr2;
                    }
                else
                    {
                    // southeast
                    movementVector.Y = -sqr2;
                    }
                }
            else
                {
                movementVector.X = -sqr2;
                // west 

                if (movementVector.Y > 0)
                    {
                    // northwest
                    movementVector.Y = sqr2;
                    }
                else
                    {
                    // southwest
                    movementVector.Y = -sqr2;
                    }
                }

            // movementVector.Z is zero

            // calculate tilt components based on desired amount of tilt and current (snapped) heading.
            // the "-" sign is to force the tilt to be OPPOSITE the direction of movement.
            float xTiltComponent = -movementVector.X * m_tiltMagnitudeWhenProjectedOnXYPlane;
            float yTiltComponent = -movementVector.Y * m_tiltMagnitudeWhenProjectedOnXYPlane;
            //m_log.Debug(movementVector.X + " " + movementVector.Y);
            //m_log.Debug("[PHYSICS] changing avatar tilt");
            d.JointSetAMotorAngle(Amotor, 0, xTiltComponent);
            d.JointSetAMotorAngle(Amotor, 1, yTiltComponent);
            d.JointSetAMotorAngle(Amotor, 2, 0);
            d.JointSetAMotorParam(Amotor, (int)dParam.LowStop, xTiltComponent - 0.001f);
            d.JointSetAMotorParam(Amotor, (int)dParam.HiStop, xTiltComponent + 0.001f); // must be same as lowstop, else a different, spurious tilt is introduced
            d.JointSetAMotorParam(Amotor, (int)dParam.LoStop2, yTiltComponent - 0.001f);
            d.JointSetAMotorParam(Amotor, (int)dParam.HiStop2, yTiltComponent + 0.001f); // same as lowstop
            d.JointSetAMotorParam(Amotor, (int)dParam.LoStop3, - 0.001f);
            d.JointSetAMotorParam(Amotor, (int)dParam.HiStop3, 0.001f); // same as lowstop
            }
*/

//      This code is very useful. Written by DanX0r. We're just not using it right now.
//      Commented out to prevent a warning.
//
//         private void standupStraight()
//         {
//             // The purpose of this routine here is to quickly stabilize the Body while it's popped up in the air.
//             // The amotor needs a few seconds to stabilize so without it, the avatar shoots up sky high when you
//             // change appearance and when you enter the simulator
//             // After this routine is done, the amotor stabilizes much quicker
//             d.Vector3 feet;
//             d.Vector3 head;
//             d.BodyGetRelPointPos(Body, 0.0f, 0.0f, -1.0f, out feet);
//             d.BodyGetRelPointPos(Body, 0.0f, 0.0f, 1.0f, out head);
//             float posture = head.Z - feet.Z;

//             // restoring force proportional to lack of posture:
//             float servo = (2.5f - posture) * POSTURE_SERVO;
//             d.BodyAddForceAtRelPos(Body, 0.0f, 0.0f, servo, 0.0f, 0.0f, 1.0f);
//             d.BodyAddForceAtRelPos(Body, 0.0f, 0.0f, -servo, 0.0f, 0.0f, -1.0f);
//             //d.Matrix3 bodyrotation = d.BodyGetRotation(Body);
//             //m_log.Info("[PHYSICSAV]: Rotation: " + bodyrotation.M00 + " : " + bodyrotation.M01 + " : " + bodyrotation.M02 + " : " + bodyrotation.M10 + " : " + bodyrotation.M11 + " : " + bodyrotation.M12 + " : " + bodyrotation.M20 + " : " + bodyrotation.M21 + " : " + bodyrotation.M22);
        //         }

        #endregion

        #region Forces

        /// <summary>
        /// Adds the force supplied to the Target Velocity
        /// The PID controller takes this target velocity and tries to make it a reality
        /// </summary>
        /// <param name="force"></param>
        public override void AddForce(Vector3 force, bool pushforce)
        {
            if (force.IsFinite())
            {
                if (pushforce)
                {
                    m_pidControllerActive = false;
                    force *= 100f;
                    doForce(force);
                    // If uncommented, things get pushed off world
                    //
                    // m_log.Debug("Push!");
                    // _target_velocity.X += force.X;
                    // _target_velocity.Y += force.Y;
                    // _target_velocity.Z += force.Z;
                }
                else
                {
                    m_pidControllerActive = true;
                    _target_velocity.X += force.X;
                    _target_velocity.Y += force.Y;
                    _target_velocity.Z += force.Z;
                }
            }
            else
            {
                m_log.Warn("[PHYSICS]: Got a NaN force applied to a Character");
            }
            //m_lastUpdateSent = false;
        }

        /// <summary>
        /// After all of the forces add up with 'add force' we apply them with doForce
        /// </summary>
        /// <param name="force"></param>
        public void doForce(Vector3 force)
        {
            if (force != Vector3.Zero)
            {
                //force /= m_mass;
                d.BodyAddForce(Body, force.X, force.Y, force.Z);
                //d.BodySetRotation(Body, ref m_StandUpRotation);
                //standupStraight();
            }
        }

        #endregion

        #region Destroy

        /// <summary>
        /// Cleanup the things we use in the scene.
        /// </summary>
        public void Destroy()
        {
            m_tainted_isPhysical = false;
            _parent_scene.AddPhysicsActorTaint(this);
        }

        #endregion

        #endregion

        #region Collision events

        public override void SubscribeEvents(int ms)
        {
            _parent_scene.addCollisionEventReporting(this);
        }

        public override void UnSubscribeEvents()
        {
            _parent_scene.remCollisionEventReporting(this);
        }

        public override void AddCollisionEvent(uint CollidedWith, ContactPoint contact)
        {
            CollisionEventsThisFrame.addCollider(CollidedWith, contact);
        }

        public override void SendCollisions ()
        {
            if (!IsPhysical)
                return;//Not physical, its not supposed to be here

            if (CollisionEventsThisFrame != null)
                base.SendCollisionUpdate (CollisionEventsThisFrame);

            CollisionEventsThisFrame = new CollisionEventUpdate ();
        }

        public override bool SubscribedEvents()
        {
            return true;
        }

        public void ProcessTaints(float timestep)
        {
            if (!m_shouldBePhysical)
                return;

            if (m_tainted_isPhysical != m_isPhysical)
            {
                if (m_tainted_isPhysical)
                {
                    // Create avatar capsule and related ODE data
                    if (!(Shell == IntPtr.Zero && Body == IntPtr.Zero)) // && Amotor == IntPtr.Zero))
                    {
                        m_log.Warn("[PHYSICS]: re-creating the following avatar ODE data, even though it already exists - "
                            + (Shell!=IntPtr.Zero ? "Shell ":"")
                            + (Body!=IntPtr.Zero ? "Body ":"")
                            );
                            // + (Amotor!=IntPtr.Zero ? "Amotor ":""));
                    }
                    AvatarGeomAndBodyCreation(_position.X, _position.Y, _position.Z);//, _parent_scene.avStandupTensor);
                    
                    _parent_scene.actor_name_map[Shell] = (PhysicsActor)this;
                    _parent_scene.AddCharacter(this);
                }
                else
                {
                    _parent_scene.RemoveCharacter(this);
                    // destroy avatar capsule and related ODE data
                    if (Amotor != IntPtr.Zero)
                    {
                        // Kill the Amotor
                        d.JointDestroy (Amotor);
                        Amotor = IntPtr.Zero;
                    }
                    //kill the Geometry
                    _parent_scene.waitForSpaceUnlock(_parent_scene.space);

                    if (Body != IntPtr.Zero)
                    {
                        //kill the body
                        d.BodyDestroy(Body);

                        Body = IntPtr.Zero;
                    }

                    if (Shell != IntPtr.Zero)
                    {
                        d.GeomDestroy(Shell);
                        Shell = IntPtr.Zero;
                    }

                }

                m_isPhysical = m_tainted_isPhysical;
            }

            if (m_tainted_CAPSULE_LENGTH != CAPSULE_LENGTH)
            {
                if (Shell != IntPtr.Zero && Body != IntPtr.Zero) // && Amotor != IntPtr.Zero)
                {
                    if (Amotor != IntPtr.Zero)
                    {
                        // Kill the Amotor
                        d.JointDestroy (Amotor);
                        Amotor = IntPtr.Zero;
                    }
                    m_pidControllerActive = true;
                    // no lock needed on _parent_scene.OdeLock because we are called from within the thread lock in OdePlugin's simulate()
                    float prevCapsule = CAPSULE_LENGTH;
                    CAPSULE_LENGTH = m_tainted_CAPSULE_LENGTH;
                    //m_log.Info("[SIZE]: " + CAPSULE_LENGTH.ToString());
                    d.BodyDestroy(Body);
                    d.GeomDestroy(Shell);
                    AvatarGeomAndBodyCreation(_position.X, _position.Y,
                                      _position.Z + (CAPSULE_LENGTH - prevCapsule));//, _parent_scene.avStandupTensor);
                    Velocity = Vector3.Zero;

                    _parent_scene.actor_name_map[Shell] = (PhysicsActor)this;
                }
                else
                {
                    m_log.Warn("[PHYSICS]: trying to change capsule size, but the following ODE data is missing - " 
                        + (Shell==IntPtr.Zero ? "Shell ":"")
                        + (Body==IntPtr.Zero ? "Body ":"")
                        );
                }
            }

            if (!m_taintPosition.ApproxEquals(_position, 0.05f))
            {
                if (Body != IntPtr.Zero)
                {
                    d.BodySetPosition(Body, m_taintPosition.X, m_taintPosition.Y, m_taintPosition.Z);

                    _position.X = m_taintPosition.X;
                    _position.Y = m_taintPosition.Y;
                    _position.Z = m_taintPosition.Z;
                }
            }
            if (Body != IntPtr.Zero)
            {
                d.Quaternion q = new d.Quaternion ();
                q.W = m_taintRotation.W;
                q.X = m_taintRotation.X;
                q.Y = m_taintRotation.Y;
                q.Z = m_taintRotation.Z;
                d.BodySetQuaternion (Body, ref q); // just keep in sync with rest of simutator
            }
        }

        #endregion
    }
}
