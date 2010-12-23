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
using System.Reflection;
using OpenMetaverse;
using Ode.NET;
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;
using log4net;

namespace Aurora.Physics.AuroraOpenDynamicsEngine
{
    public class AuroraODECharacter : PhysicsActor
    {
        #region Declare

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Vector3 _position;
        private Vector3 m_lastPosition;
        private d.Vector3 _zeroPosition;
        // private d.Matrix3 m_StandUpRotation;
        private bool _zeroFlag = false;
        private bool m_lastUpdateSent = false;
        /*private Vector3 __velocity; //For testing to see when Vector3.Zero is set for the velocity
        private Vector3 _velocity
        {
            get { return __velocity; }
            set 
            {
                if (value == Vector3.Zero)
                {
                    m_log.Warn("VECTOR3 ZERO! zero flag: " + _zeroFlag + ", flying: " + flying);
                }
                __velocity = value;
            }
        }*/
        private Vector3 _velocity;
        private Vector3 m_lastVelocity;
        private Vector3 _target_velocity;
        private Vector3 _acceleration;
        private Vector3 m_rotationalVelocity;
        private Vector3 m_lastRotationalVelocity;
        private float m_mass = 80f;
        public float m_density = 60f;
        private bool m_pidControllerActive = true;
        public float PID_D = 800.0f;
        public float PID_P = 900.0f;
        //private static float POSTURE_SERVO = 10000.0f;
        public float CAPSULE_RADIUS = 0.37f;
        public float CAPSULE_LENGTH = 2.140599f;
        public float m_tensor = 3800000f;
        public float heightFudgeFactor = 0.52f;
        public float walkDivisor = 1.3f;
        public float runDivisor = 0.8f;
        private bool flying = false;
        private bool m_iscolliding = false;
        private bool m_iscollidingGround = false;
        private bool m_wascolliding = false;
        private bool m_wascollidingGround = false;
        private bool m_iscollidingObj = false;
        private bool m_alwaysRun = false;
        private int m_requestedUpdateFrequency = 0;
        private Vector3 m_taintPosition = Vector3.Zero;
        public uint m_localID = 0;
        public bool m_returnCollisions = false;
        // taints and their non-tainted counterparts
        public bool m_isPhysical = false; // the current physical status
        public bool m_tainted_isPhysical = false; // set when the physical status is tainted (false=not existing in physics engine, true=existing)
        public float MinimumGroundFlightOffset = 3f;

         private float lastUnderwaterPush = 0;
        private bool WasUnderWater = false;
        private bool ShouldBeWalking = true;
        private bool StartingUnderWater = true;
                    
        private float m_tainted_CAPSULE_LENGTH; // set when the capsule length changes. 
        private float m_tiltMagnitudeWhenProjectedOnXYPlane = 0.1131371f; // used to introduce a fixed tilt because a straight-up capsule falls through terrain, probably a bug in terrain collider


        private float m_buoyancy = 0f;

        // private CollisionLocker ode;

        private string m_name = String.Empty;

        private bool[] m_colliderarr = new bool[11];
        private bool[] m_colliderGroundarr = new bool[11];

        // Default we're a Character
        private CollisionCategories m_collisionCategories = (CollisionCategories.Character);

        // Default, Collide with Other Geometries, spaces, bodies and characters.
        private CollisionCategories m_collisionFlags = (CollisionCategories.Geom
                                                        | CollisionCategories.Space
                                                        | CollisionCategories.Body
                                                        | CollisionCategories.Character
                                                        | CollisionCategories.Land);
        public IntPtr Body = IntPtr.Zero;
        private AuroraODEPhysicsScene _parent_scene;
        public IntPtr Shell = IntPtr.Zero;
        public IntPtr Amotor = IntPtr.Zero;
        public d.Mass ShellMass;
        public bool collidelock = false;

        public int m_eventsubscription = 0;
        private CollisionEventUpdate CollisionEventsThisFrame = new CollisionEventUpdate();

        // unique UUID of this character object
        public UUID m_uuid;
        public bool bad = false;
        private int m_WaitGroundCheck = 0;

        #endregion

        #region Constructor

        public AuroraODECharacter(String avName, AuroraODEPhysicsScene parent_scene, Vector3 pos, CollisionLocker dode, Vector3 size, float pid_d, float pid_p, float capsule_radius, float tensor, float density, float height_fudge_factor, float walk_divisor, float rundivisor)
        {
            m_uuid = UUID.Random();

            if (pos.IsFinite())
            {
                if (pos.Z > 9999999f)
                {
                    pos.Z = parent_scene.GetTerrainHeightAtXY(127, 127) + 5;
                }
                if (pos.Z < -90000f)
                {
                    pos.Z = parent_scene.GetTerrainHeightAtXY(127, 127) + 5;
                }
                _position = pos;
                m_taintPosition.X = pos.X;
                m_taintPosition.Y = pos.Y;
                m_taintPosition.Z = pos.Z;
            }
            else
            {
                _position = new Vector3(((float)parent_scene.WorldExtents.X * 0.5f), ((float)parent_scene.WorldExtents.Y * 0.5f), parent_scene.GetTerrainHeightAtXY(128f, 128f) + 10f);
                m_taintPosition.X = _position.X;
                m_taintPosition.Y = _position.Y;
                m_taintPosition.Z = _position.Z;
                m_log.Warn("[PHYSICS]: Got NaN Position on Character Create");
            }

            _parent_scene = parent_scene;

            PID_D = pid_d;
            PID_P = pid_p;
            CAPSULE_RADIUS = capsule_radius;
            m_tensor = tensor;
            m_density = density;
            heightFudgeFactor = height_fudge_factor;
            walkDivisor = walk_divisor;
            runDivisor = rundivisor;

            // m_StandUpRotation =
            //     new d.Matrix3(0.5f, 0.7071068f, 0.5f, -0.7071068f, 0f, 0.7071068f, 0.5f, -0.7071068f,
            //                   0.5f);

            for (int i = 0; i < 11; i++)
            {
                m_colliderarr[i] = false;
            }
            CAPSULE_LENGTH = (size.Z * 1.1f) - CAPSULE_RADIUS * 2.0f;
            //m_log.Info("[SIZE]: " + CAPSULE_LENGTH.ToString());
            m_tainted_CAPSULE_LENGTH = CAPSULE_LENGTH;

            m_isPhysical = false; // current status: no ODE information exists
            m_tainted_isPhysical = true; // new tainted status: need to create ODE information

            _parent_scene.AddPhysicsActorTaint(this);
            
            m_name = avName;
        }

        #endregion

        #region Properties

        public override int PhysicsActorType
        {
            get { return (int) ActorTypes.Agent; }
            set { return; }
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
            set { m_localID = value; }
        }

        public override bool Grabbed
        {
            set { return; }
        }

        public override bool Selected
        {
            set { return; }
        }

        public override bool VolumeDetect
        {
            get { return false; }
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

        public override bool IsPhysical
        {
            get { return false; }
            set { return; }
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
        /// </summary>
        public override bool IsColliding
        {
            get { return m_iscolliding; }
            set
            {
                int i;
                int truecount = 0;
                int falsecount = 0;

                if (m_colliderarr.Length >= 10)
                {
                    for (i = 0; i < 10; i++)
                    {
                        m_colliderarr[i] = m_colliderarr[i + 1];
                    }
                }
                m_colliderarr[10] = value;

                for (i = 0; i < 11; i++)
                {
                    if (m_colliderarr[i])
                    {
                        truecount++;
                    }
                    else
                    {
                        falsecount++;
                    }
                }

                // Equal truecounts and false counts means we're colliding with something.

                if (falsecount >= 1.2*truecount)
                {
                    m_iscolliding = false;
                }
                else
                {
                    m_iscolliding = true;
                }
                if (m_wascolliding != m_iscolliding)
                {
                    //base.SendCollisionUpdate(new CollisionEventUpdate());
                }
                m_wascolliding = m_iscolliding;
            }
        }

        /// <summary>
        /// Returns if an avatar is colliding with the ground
        /// </summary>
        public override bool CollidingGround
        {
            get { return m_iscollidingGround; }
            set
            {
                // Collisions against the ground are not really reliable
                // So, to get a consistant value we have to average the current result over time
                // Currently we use 1 second = 10 calls to this.
                int i;
                int truecount = 0;
                int falsecount = 0;

                if (m_colliderGroundarr.Length >= 10)
                {
                    for (i = 0; i < 10; i++)
                    {
                        m_colliderGroundarr[i] = m_colliderGroundarr[i + 1];
                    }
                }
                m_colliderGroundarr[10] = value;

                for (i = 0; i < 11; i++)
                {
                    if (m_colliderGroundarr[i])
                    {
                        truecount++;
                    }
                    else
                    {
                        falsecount++;
                    }
                }

                // Equal truecounts and false counts means we're colliding with something.

                if (falsecount > 1.2*truecount)
                {
                    m_iscollidingGround = false;
                }
                else
                {
                    m_iscollidingGround = true;
                }
                if (m_wascollidingGround != m_iscollidingGround)
                {
                    //base.SendCollisionUpdate(new CollisionEventUpdate());
                }
                m_wascollidingGround = m_iscollidingGround;
            }
        }

        /// <summary>
        /// Returns if the avatar is colliding with an object
        /// </summary>
        public override bool CollidingObj
        {
            get { return m_iscollidingObj; }
            set
            {
                m_iscollidingObj = value;
                if (value)
                    m_pidControllerActive = false;
                else
                    m_pidControllerActive = true;
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

        public override bool Stopped
        {
            get { return _zeroFlag; }
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
                        if (value.Z > 9999999f)
                        {
                            value.Z = _parent_scene.GetTerrainHeightAtXY(127, 127) + 5;
                        }
                        if (value.Z < -90000f)
                        {
                            value.Z = _parent_scene.GetTerrainHeightAtXY(127, 127) + 5;
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
            set { }
        }

        public override Vector3 Force
        {
            get { return _target_velocity; }
            set { return; }
        }

        public override int VehicleType
        {
            get { return 0; }
            set { return; }
        }

        public override Vector3 CenterOfMass
        {
            get { return Vector3.Zero; }
        }

        public override Vector3 GeometricCenter
        {
            get { return Vector3.Zero; }
        }

        public override PrimitiveBaseShape Shape
        {
            set { return; }
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

        public override bool Kinematic
        {
            get { return false; }
            set { }
        }

        public override Quaternion Orientation
        {
            get { return Quaternion.Identity; }
            set
            {
                //Matrix3 or = Orientation.ToRotationMatrix();
                //d.Matrix3 ord = new d.Matrix3(or.m00, or.m10, or.m20, or.m01, or.m11, or.m21, or.m02, or.m12, or.m22);
                //d.BodySetRotation(Body, ref ord);
            }
        }

        public override Vector3 Acceleration
        {
            get { return _acceleration; }
        }

        public override Vector3 PIDTarget { get { return Vector3.Zero; } set { return; } }
        public override bool PIDActive { get { return false; } set { return; } }
        public override float PIDTau { get { return 0; } set { return; } }

        public override float PIDHoverHeight { set { return; } }
        public override bool PIDHoverActive { set { return; } }
        public override PIDHoverType PIDHoverType { set { return; } }
        public override float PIDHoverTau { set { return; } }

        public override Quaternion APIDTarget { set { return; } }
        public override bool APIDActive { set { return; } }
        public override float APIDStrength { set { return; } }
        public override float APIDDamping { set { return; } }

        #endregion

        #region Methods

        #region Rebuild the avatar representation

        private float FindNewAvatarMass()
        {
            float volume = Size.X * Size.Y * Size.Z;
            return volume * 200; //Aluminum g/cm3 aprox * 5
        }

        /// <summary>
        /// This creates the Avatar's physical Surrogate at the position supplied
        /// WARNING: This MUST NOT be called outside of ProcessTaints, else we can have unsynchronized access
        /// to ODE internals. ProcessTaints is called from within thread-locked Simulate(), so it is the only 
        /// place that is safe to call this routine AvatarGeomAndBodyCreation.
        /// </summary>
        /// <param name="npositionX"></param>
        /// <param name="npositionY"></param>
        /// <param name="npositionZ"></param>
        private void AvatarGeomAndBodyCreation(float npositionX, float npositionY, float npositionZ, float tensor)
        {
            int dAMotorEuler = 1;
            _parent_scene.waitForSpaceUnlock(_parent_scene.space);
            if (CAPSULE_LENGTH <= 0)
            {
                m_log.Warn("[PHYSICS]: The capsule size you specified in opensim.ini is invalid!  Setting it to the smallest possible size!");
                CAPSULE_LENGTH = 1.2f;

            }

            if (CAPSULE_RADIUS <= 0)
            {
                m_log.Warn("[PHYSICS]: The capsule size you specified in opensim.ini is invalid!  Setting it to the normal size!");
                CAPSULE_RADIUS = 0.37f;

            }
            Shell = d.CreateCapsule(_parent_scene.space, CAPSULE_RADIUS, CAPSULE_LENGTH);
            
            d.GeomSetCategoryBits(Shell, (int)m_collisionCategories);
            d.GeomSetCollideBits(Shell, (int)m_collisionFlags);

            m_mass = FindNewAvatarMass();

            d.MassSetCapsuleTotal(out ShellMass, m_mass, 2, CAPSULE_RADIUS, CAPSULE_LENGTH);
            Body = d.BodyCreate(_parent_scene.world);
            m_mass /= 2; //nasty nasty hack to keep things from being awkward when you stop moving
            d.BodySetPosition(Body, npositionX, npositionY, npositionZ);

            // disconnect from world gravity so we can apply buoyancy
            d.BodySetGravityMode(Body, false);

            _position.X = npositionX;
            _position.Y = npositionY;
            _position.Z = npositionZ;


            m_taintPosition.X = _position.X;
            m_taintPosition.Y = _position.Y;
            m_taintPosition.Z = _position.Z;

            d.BodySetMass(Body, ref ShellMass);
            d.Matrix3 m_caprot;
            // 90 Stand up on the cap of the capped cyllinder
            if (_parent_scene.IsAvCapsuleTilted)
            {
                d.RFromAxisAndAngle(out m_caprot, 1, 0, 1, (float)(Math.PI / 2));
            }
            else
            {
                d.RFromAxisAndAngle(out m_caprot, 0, 0, 1, (float)(Math.PI / 2));
            }


            d.GeomSetRotation(Shell, ref m_caprot);
            d.BodySetRotation(Body, ref m_caprot);

            d.GeomSetBody(Shell, Body);


            // The purpose of the AMotor here is to keep the avatar's physical
            // surrogate from rotating while moving
            Amotor = d.JointCreateAMotor(_parent_scene.world, IntPtr.Zero);
            d.JointAttach(Amotor, Body, IntPtr.Zero);
            d.JointSetAMotorMode(Amotor, dAMotorEuler);
            d.JointSetAMotorNumAxes(Amotor, 3);
            d.JointSetAMotorAxis(Amotor, 0, 0, 1, 0, 0);
            d.JointSetAMotorAxis(Amotor, 1, 0, 0, 1, 0);
            d.JointSetAMotorAxis(Amotor, 2, 0, 0, 0, 1);
            d.JointSetAMotorAngle(Amotor, 0, 0);
            d.JointSetAMotorAngle(Amotor, 1, 0);
            d.JointSetAMotorAngle(Amotor, 2, 0);

            // These lowstops and high stops are effectively (no wiggle room)
            if (_parent_scene.IsAvCapsuleTilted)
            {
                d.JointSetAMotorParam(Amotor, (int)dParam.LowStop, -0.000000000001f);
                d.JointSetAMotorParam(Amotor, (int)dParam.LoStop3, -0.000000000001f);
                d.JointSetAMotorParam(Amotor, (int)dParam.LoStop2, -0.000000000001f);
                d.JointSetAMotorParam(Amotor, (int)dParam.HiStop, 0.000000000001f);
                d.JointSetAMotorParam(Amotor, (int)dParam.HiStop3, 0.000000000001f);
                d.JointSetAMotorParam(Amotor, (int)dParam.HiStop2, 0.000000000001f);
            }
            else
            {
                #region Documentation of capsule motor LowStop and HighStop parameters
                // Intentionally introduce some tilt into the capsule by setting
                // the motor stops to small epsilon values. This small tilt prevents
                // the capsule from falling into the terrain; a straight-up capsule
                // (with -0..0 motor stops) falls into the terrain for reasons yet
                // to be comprehended in their entirety.
                #endregion
                AlignAvatarTiltWithCurrentDirectionOfMovement(Vector3.Zero);
                d.JointSetAMotorParam(Amotor, (int)dParam.LowStop, 0.08f);
                d.JointSetAMotorParam(Amotor, (int)dParam.LoStop3, -0f);
                d.JointSetAMotorParam(Amotor, (int)dParam.LoStop2, 0.08f);
                d.JointSetAMotorParam(Amotor, (int)dParam.HiStop,  0.08f); // must be same as lowstop, else a different, spurious tilt is introduced
                d.JointSetAMotorParam(Amotor, (int)dParam.HiStop3, 0f); // same as lowstop
                d.JointSetAMotorParam(Amotor, (int)dParam.HiStop2, 0.08f); // same as lowstop
            }

            // Fudge factor is 1f by default, we're setting it to 0.  We don't want it to Fudge or the
            // capped cyllinder will fall over
            d.JointSetAMotorParam(Amotor, (int)dParam.FudgeFactor, 0f);
            d.JointSetAMotorParam(Amotor, (int)dParam.FMax, tensor);

            //d.Matrix3 bodyrotation = d.BodyGetRotation(Body);
            //d.QfromR(
            //d.Matrix3 checkrotation = new d.Matrix3(0.7071068,0.5, -0.7071068,
            //
            //m_log.Info("[PHYSICSAV]: Rotation: " + bodyrotation.M00 + " : " + bodyrotation.M01 + " : " + bodyrotation.M02 + " : " + bodyrotation.M10 + " : " + bodyrotation.M11 + " : " + bodyrotation.M12 + " : " + bodyrotation.M20 + " : " + bodyrotation.M21 + " : " + bodyrotation.M22);
            //standupStraight();
        }

        #endregion

        #region Move

        private void AlignAvatarTiltWithCurrentDirectionOfMovement(Vector3 movementVector)
        {
            movementVector.Z = 0f;
            float magnitude = (float)Math.Sqrt((double)(movementVector.X * movementVector.X + movementVector.Y * movementVector.Y));
            if (magnitude < 0.1f) return;

            // normalize the velocity vector
            float invMagnitude = 1.0f / magnitude;
            movementVector.X *= invMagnitude;
            movementVector.Y *= invMagnitude;

            // if we change the capsule heading too often, the capsule can fall down
            // therefore we snap movement vector to just 1 of 4 predefined directions (ne, nw, se, sw),
            // meaning only 4 possible capsule tilt orientations

            if (movementVector == Vector3.Zero)
            {
                return;
            }
            else if (movementVector.X > 0)
            {
                // east
                if (movementVector.Y > 0)
                {
                    // northeast
                    movementVector.X = (float)Math.Sqrt(4);
                    movementVector.Y = (float)Math.Sqrt(4);
                }
                else
                {
                    // southeast
                    movementVector.X = (float)Math.Sqrt(4);
                    movementVector.Y = -(float)Math.Sqrt(4);
                }
            }
            else
            {
                // west
                if (movementVector.Y > 0)
                {
                    // northwest
                    movementVector.X = -(float)Math.Sqrt(4);
                    movementVector.Y = (float)Math.Sqrt(4);
                }
                else
                {
                    // southwest
                    movementVector.X = -(float)Math.Sqrt(4);
                    movementVector.Y = -(float)Math.Sqrt(4);
                }
            }


            // movementVector.Z is zero

            // calculate tilt components based on desired amount of tilt and current (snapped) heading.
            // the "-" sign is to force the tilt to be OPPOSITE the direction of movement.
            float xTiltComponent = -movementVector.X * m_tiltMagnitudeWhenProjectedOnXYPlane;
            float yTiltComponent = -movementVector.Y * m_tiltMagnitudeWhenProjectedOnXYPlane;
            //m_log.Debug(movementVector.X + " " + movementVector.Y);
            //m_log.Debug("[PHYSICS] changing avatar tilt");
            d.JointSetAMotorParam(Amotor, (int)dParam.LowStop, xTiltComponent);
            d.JointSetAMotorParam(Amotor, (int)dParam.HiStop, xTiltComponent); // must be same as lowstop, else a different, spurious tilt is introduced
            d.JointSetAMotorParam(Amotor, (int)dParam.LoStop2, yTiltComponent);
            d.JointSetAMotorParam(Amotor, (int)dParam.HiStop2, yTiltComponent); // same as lowstop
            d.JointSetAMotorParam(Amotor, (int)dParam.LoStop3, 0f);
            d.JointSetAMotorParam(Amotor, (int)dParam.HiStop3, 0f); // same as lowstop
        }

        /// <summary>
        /// Called from Simulate
        /// This is the avatar's movement control + PID Controller
        /// </summary>
        /// <param name="timeStep"></param>
        public void Move(float timeStep, List<AuroraODECharacter> defects)
        {
            //  no lock; for now it's only called from within Simulate()

            // If the PID Controller isn't active then we set our force
            // calculating base velocity to the current position

            if (Body == IntPtr.Zero)
                return;

            if (m_pidControllerActive == false)
            {
                _zeroPosition = d.BodyGetPosition(Body);
            }
            //PidStatus = true;

            // rex, added height check
            d.Vector3 tempPos = d.BodyGetPosition(Body);
            if (_parent_scene.m_useFlightCeilingHeight && tempPos.Z > _parent_scene.m_flightCeilingHeight)
            {
                tempPos.Z = _parent_scene.m_flightCeilingHeight;
                d.BodySetPosition(Body, tempPos.X, tempPos.Y, tempPos.Z);
                d.Vector3 tempVel = d.BodyGetLinearVel(Body);
                if (tempVel.Z > 0.0f)
                {
                    tempVel.Z = 0.0f;
                    d.BodySetLinearVel(Body, tempVel.X, tempVel.Y, tempVel.Z);
                }
                if (_target_velocity.Z > 0.0f)
                    _target_velocity.Z = 0.0f;
            }

            // endrex

            d.Vector3 localpos = d.BodyGetPosition(Body);
            Vector3 localPos = new Vector3((float)localpos.X, (float)localpos.Y, (float)localpos.Z);

            if (!localPos.IsFinite())
            {
                m_log.Warn("[PHYSICS]: Avatar Position is non-finite!");
                defects.Add(this);
                // _parent_scene.RemoveCharacter(this);

                // destroy avatar capsule and related ODE data
                if (Amotor != IntPtr.Zero)
                {
                    // Kill the Amotor
                    d.JointDestroy(Amotor);
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
                    _parent_scene.geom_name_map.Remove(Shell);
                    Shell = IntPtr.Zero;
                }
                return;
            }

            Vector3 vec = Vector3.Zero;
            d.Vector3 vel = d.BodyGetLinearVel(Body);

            float movementdivisor = 1f;

            if (!m_alwaysRun)
                movementdivisor = walkDivisor * (_parent_scene.TimeDilation < 0.3 ? 0.6f : _parent_scene.TimeDilation); //Dynamically adjust it for slower sims
            else
                movementdivisor = runDivisor * (_parent_scene.TimeDilation < 0.3 ? 0.6f : _parent_scene.TimeDilation); //Dynamically adjust it for slower sims

            //  if velocity is zero, use position control; otherwise, velocity control
            if (_target_velocity.X == 0.0f && _target_velocity.Y == 0.0f && _target_velocity.Z == 0.0f &&
                Math.Abs(vel.X) < 0.5 && Math.Abs(vel.Y) < 0.5 && Math.Abs(vel.Z) < 0.5) //This is so that if we get moved by something else, it will update us in the client
            {
                //  keep track of where we stopped.  No more slippin' & slidin'
                if (!_zeroFlag)
                {
                    _zeroFlag = true;
                    _zeroPosition = d.BodyGetPosition(Body);
                }

                if (m_pidControllerActive)
                {
                    // We only want to deactivate the PID Controller if we think we want to have our surrogate
                    // react to the physics scene by moving it's position.
                    // Avatar to Avatar collisions
                    // Prim to avatar collisions

                    d.Vector3 pos = d.BodyGetPosition(Body);
                    Vector3 velocity = new Vector3((float)vel.X, (float)vel.Y, (float)vel.Z);

                    //NOTE: THIS WILL CAUSE BENDING KNEE
                    //if (!velocity.ApproxEquals(Vector3.Zero, 0.2f))
                    //{
                    vec.X = (float)((_target_velocity.X - vel.X) * (PID_D) + (_zeroPosition.X - pos.X) * (PID_P * 2));
                    vec.Y = (float)((_target_velocity.Y - vel.Y) * (PID_D) + (_zeroPosition.Y - pos.Y) * (PID_P * 2));
                    //if(!Flying)
                    //    vec.Z += -(_parent_scene.gravityz * 3f * m_mass);
                    //if (flying)
                    //    vec.Z = (_target_velocity.Z - vel.Z) * (PID_D) + (_zeroPosition.Z - pos.Z) * PID_P;
                    //}
                }
            }
            else
            {
                m_pidControllerActive = true;
                _zeroFlag = false;
                if (m_iscolliding && !flying)
                {
                    // We're standing or walking on something
                    vec.X = (float)((_target_velocity.X / movementdivisor) - vel.X) * (PID_D);
                    vec.Y = (float)((_target_velocity.Y / movementdivisor) - vel.Y) * (PID_D);
                    //Stop pushing down if we are just standing
                    //if (_target_velocity.Z < -0.3)
                    //    vec.Z += -(_parent_scene.gravityz * 3f * m_mass);
                    //if (_target_velocity.Z < 0)
                    //    _target_velocity.Z = 0;
                }
                else if (m_iscolliding && flying)
                {
                    // We're flying and colliding with something
                    vec.X = (float)((_target_velocity.X / movementdivisor) - vel.X) * (PID_D / 16);
                    vec.Y = (float)((_target_velocity.Y / movementdivisor) - vel.Y) * (PID_D / 16);
                }
                else if (!m_iscolliding && flying)
                {
                    // we're in mid air suspended
                    vec.X = (float)((_target_velocity.X / movementdivisor) - vel.X) * (PID_D / 6);
                    vec.Y = (float)((_target_velocity.Y / movementdivisor) - vel.Y) * (PID_D / 6);
                }
                if (m_iscolliding && !flying && _target_velocity.Z > 0.0f)
                {
                    // We're colliding with something and we're not flying but we're moving
                    // This means we're walking or running.
                    d.Vector3 pos = d.BodyGetPosition(Body);
                    vec.Z = (float)((_target_velocity.Z - vel.Z) * PID_D + (_zeroPosition.Z - pos.Z) * PID_P);
                    if (_target_velocity.X > 0)
                        vec.X = (float)((_target_velocity.X - vel.X) / 1.2f) * PID_D;
                    if (_target_velocity.Y > 0)
                        vec.Y = (float)((_target_velocity.Y - vel.Y) / 1.2f) * PID_D;
                }
                else if (!m_iscolliding && !flying)
                {
                    // we're not colliding and we're not flying so that means we're falling!
                    // m_iscolliding includes collisions with the ground.

                    // d.Vector3 pos = d.BodyGetPosition(Body);
                    if (_target_velocity.X > 0)
                        vec.X = (float)((_target_velocity.X - vel.X) / 1.2f) * PID_D;
                    if (_target_velocity.Y > 0)
                        vec.Y = (float)((_target_velocity.Y - vel.Y) / 1.2f) * PID_D;
                }

                if (flying)
                {
                    #region Av gravity

                    float oldVelZ = (float)vel.Z;
                    if (_parent_scene.AllowAvGravity &&
                        tempPos.Z > _parent_scene.AvGravityHeight) //Should be stop avies from flying upwards
                    {
                        //Decay going up 
                        if (_target_velocity.Z > 0)
                        {
                            //How much should we force them down?
                            float divisor = (_parent_scene.AllowAvsToEscapeGravity ? 100 : 25);
                            //How much should we force them down?
                            float fudgeHeight = (_parent_scene.AllowAvsToEscapeGravity ? 80 : 30);
                            //We add the 30 so that gravity is resonably strong once they pass the min height
                            float Multiplier = (float)(((tempPos.Z + fudgeHeight) - _parent_scene.AvGravityHeight) / divisor);

                            //Limit these so that things don't go wrong
                            if (Multiplier < 1)
                                Multiplier = 1;

                            float maxpower = (_parent_scene.AllowAvsToEscapeGravity ? 1.5f : 3);

                            if (Multiplier > maxpower)
                                Multiplier = maxpower;
                            _target_velocity.Z /= Multiplier;
                            oldVelZ /= Multiplier;
                        }
                    }

                    #endregion

                    vec.Z = (_target_velocity.Z - oldVelZ) * (PID_P);
                    if (_parent_scene.AllowAvGravity && tempPos.Z > _parent_scene.AvGravityHeight)
                        //Add extra gravity
                        vec.Z += ((25 * _parent_scene.gravityz) * Mass);
                }
            }
            if (flying)
            {
                //Fix grav when flying
                if (!_parent_scene.AllowAvGravity ||
                    (_parent_scene.AllowAvGravity && tempPos.Z < _parent_scene.AvGravityHeight))
                {
                    //Cancel out gravity if we are flying
                    vec.X += _parent_scene.gravityx * -3f * m_mass;
                    vec.Y += _parent_scene.gravityy * -3f * m_mass;
                    vec.Z += _parent_scene.gravityz * -3f * m_mass;
                }

                #region Auto Fly Height

                //Added for auto fly height. Kitto Flora
                //Changed to only check if the avatar is flying around,

                // Revolution: If the avatar is going down, they are trying to land (probably), so don't push them up to make it harder
                //   Only if they are moving around sideways do we need to push them up
                if (_target_velocity.X != 0 || _target_velocity.Y != 0)
                {
                    Vector3 forwardVel = new Vector3(_target_velocity.X > 0 ? 2 : (_target_velocity.X < 0 ? -2 : 0),
                        _target_velocity.Y > 0 ? 2 : (_target_velocity.Y < 0 ? -2 : 0),
                        0);
                    float target_altitude = _parent_scene.GetTerrainHeightAtXY(_position.X, _position.Y) + MinimumGroundFlightOffset;

                    //We cheat a bit and do a bit lower than normal
                    if ((_position.Z - CAPSULE_LENGTH) < target_altitude)
                        vec.Z += (target_altitude - _position.Z) * PID_D;
                    //Check in the direction we are going as well
                    target_altitude = _parent_scene.GetTerrainHeightAtXY(_position.X + forwardVel.X, _position.Y + forwardVel.Y) + MinimumGroundFlightOffset;
                    if ((_position.Z - CAPSULE_LENGTH) < target_altitude)
                        vec.Z += (target_altitude - _position.Z) * PID_D / 2;
                }
                else
                {
                    //Straight up and down, only apply when they are very close to the ground
                    float target_altitude = _parent_scene.GetTerrainHeightAtXY(_position.X, _position.Y);

                    if ((_position.Z - CAPSULE_LENGTH + (MinimumGroundFlightOffset / 1.5)) < target_altitude + MinimumGroundFlightOffset)
                    {
                        if ((_position.Z - CAPSULE_LENGTH) < target_altitude + 1)
                        {
                            vec.Z += ((target_altitude + 4) - (_position.Z - CAPSULE_LENGTH)) * PID_D;
                        }
                        else
                            vec.Z += ((target_altitude + MinimumGroundFlightOffset) - (_position.Z - CAPSULE_LENGTH)) * PID_D / 2;
                    }
                }

                #endregion
            }

            #region Gravity

            //The '3' is since the gravity isn't exactly what people would expect... maybe should be a config option someday
            if (!_parent_scene.UsePointGravity)
            {
                //Add normal gravity
                vec.X += _parent_scene.gravityx * 3f * m_mass;
                vec.Y += _parent_scene.gravityy * 3f * m_mass;
                vec.Z += _parent_scene.gravityz * 3f * m_mass;
            }
            else
            {
                Vector3 cog = _parent_scene.PointOfGravity;
                if (cog.X != 0)
                    vec.X += (cog.X - Position.X) * m_mass;
                if (cog.Y != 0)
                    vec.Y += (cog.Y - Position.Y) * m_mass;
                if (cog.Z != 0)
                    vec.Z += (cog.Z - Position.Z) * m_mass;
            }

            #endregion

            #region Under water physics

            if (_parent_scene.AllowUnderwaterPhysics)
            {
                //Position plus height to av's shoulder (aprox) is just above water
                if ((_position.Z + (CAPSULE_LENGTH / 3) - .25f) < _parent_scene.waterlevel)
                {
                    if (StartingUnderWater)
                        ShouldBeWalking = Flying == false;
                    StartingUnderWater = false;
                    WasUnderWater = true;
                    Flying = true;
                    lastUnderwaterPush = 0;
                    if (ShouldBeWalking)
                    {
                        lastUnderwaterPush += (_parent_scene.waterlevel - _position.Z) * 100 + 10;
                        vec.Z += lastUnderwaterPush;
                    }
                    else
                    {
                        lastUnderwaterPush += 8000;
                        lastUnderwaterPush += (_parent_scene.waterlevel - _position.Z) * 25;
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

            #region Check for underground

            if (!flying || (flying && _target_velocity.X == 0 || _target_velocity.Y == 0)) //Don't duplicate the ground check for flying from above, it will already have given us a good shove
            {
                if (m_WaitGroundCheck >= 10 && vel.Z != 0)
                {
                    float groundHeight = _parent_scene.GetTerrainHeightAtXY(_position.X, _position.Y);
                    if (_position.Z - (CAPSULE_LENGTH / 2) < groundHeight)
                        vec.Z += ((groundHeight - (_position.Z - (CAPSULE_LENGTH / 2))) * 100);
                    m_WaitGroundCheck = -1;
                }
                m_WaitGroundCheck++;
            }

            #endregion

            if (vec.IsFinite())
            {
                if (vec.X < 100000 && vec.Y < 100000 && vec.Z < 100000) //Checks for crazy, going to NaN us values
                {
                    d.Vector3 veloc = d.BodyGetLinearVel(Body);
                    //Stop us from fidgiting if we have a small velocity
                    if (((Math.Abs(vec.X) < 0.09 && Math.Abs(vec.Y) < 0.09 && Math.Abs(vec.Z) < 0.03) && !flying && vec.Z != 0))
                    {
                        //m_log.Warn("Nulling Velo: " + vec.ToString());
                        vec = new Vector3(0, 0, 0);
                        d.BodySetLinearVel(Body, 0, 0, 0);
                    }

                    //Reduce insanely small values to 0 if the velocity isn't going up
                    if (Math.Abs(vec.Z) < 0.01 && veloc.Z < 0.6)
                    {
                        if (veloc.Z != 0)
                        {
                            if (-veloc.Z > 0)
                                vec.Z = 0;
                            else
                                vec.Z = -veloc.Z * 5;
                            d.BodySetLinearVel(Body, veloc.X, veloc.Y, vec.Z);
                        }
                    }

                    d.Vector3 posi = d.BodyGetPosition(Body);
                    //if (vec != Vector3.Zero)
                    //    m_log.Warn("[Physics]: Vec:" + vec + ",Vel:" + veloc.Z + ",zero_flag:" + _zeroFlag);
                    doForce(vec);

                    //When falling, we keep going faster and faster, and eventually, the client blue screens (blue is all you see).
                    // The speed that does this is slightly higher than -30, so we cap it here so we never do that during falling.
                    if (vel.Z < -30)
                    {
                        vel.Z = -30;
                        d.BodySetLinearVel(Body, vel.X, vel.Y, vel.Z);
                    }

                    //Check if the capsule is tilted before changing it
                    if (!_zeroFlag && !_parent_scene.IsAvCapsuleTilted)
                        AlignAvatarTiltWithCurrentDirectionOfMovement(vec);
                }
                else
                {
                    //This is a safe guard from going NaN, but it isn't very smooth... which is ok
                    d.BodySetForce(Body, 0, 0, 0);
                    d.BodySetLinearVel(Body, 0, 0, 0);
                }
            }
            else
            {
                m_log.Warn("[PHYSICS]: Got a NaN force vector in Move()");
                m_log.Warn("[PHYSICS]: Avatar Position is non-finite!");
                defects.Add(this);
                // _parent_scene.RemoveCharacter(this);
                // destroy avatar capsule and related ODE data
                if (Amotor != IntPtr.Zero)
                {
                    // Kill the Amotor
                    d.JointDestroy(Amotor);
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
                    _parent_scene.geom_name_map.Remove(Shell);
                    Shell = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Updates the reported position and velocity.  This essentially sends the data up to ScenePresence.
        /// </summary>
        public void UpdatePositionAndVelocity()
        {
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


            //  kluge to keep things in bounds.  ODE lets dead avatars drift away (they should be removed!)
            if (vec.X < 0.0f) vec.X = 0.0f;
            if (vec.Y < 0.0f) vec.Y = 0.0f;
            if (vec.X > (int)_parent_scene.WorldExtents.X - 0.05f) vec.X = (int)_parent_scene.WorldExtents.X - 0.05f;
            if (vec.Y > (int)_parent_scene.WorldExtents.Y - 0.05f) vec.Y = (int)_parent_scene.WorldExtents.Y - 0.05f;

            _position.X = (float)vec.X;
            _position.Y = (float)vec.Y;
            _position.Z = (float)vec.Z;

            // Did we move last? = zeroflag
            // This helps keep us from sliding all over

            if (_zeroFlag)
            {
                /*if (CollisionEventsThisFrame != null)
                {
                    base.SendCollisionUpdate(CollisionEventsThisFrame);
                }
                CollisionEventsThisFrame = new CollisionEventUpdate();
                m_eventsubscription = 0;*/
                _velocity = Vector3.Zero;

                // Did we send out the 'stopped' message?
                if (!m_lastUpdateSent)
                {
                    m_lastUpdateSent = true;
                    base.RequestPhysicsterseUpdate();
                }
            }
            else
            {
                m_lastUpdateSent = false;
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
                if (vec.X == 0 && vec.Y == 0 && vec.Z == 0)
                {
                    m_log.Warn("[AODECharacter]: We have a malformed Velocity, ignoring...");
                }
                else
                    _velocity = new Vector3((float)(vec.X), (float)(vec.Y), (float)(vec.Z));

                const float VELOCITY_TOLERANCE = 0.001f;
                const float POSITION_TOLERANCE = 0.05f;

                //Check to see whether we need to trigger the significant movement method in the presence
                if (!RotationalVelocity.ApproxEquals(m_lastRotationalVelocity, VELOCITY_TOLERANCE) ||
                    !Velocity.ApproxEquals(m_lastVelocity, VELOCITY_TOLERANCE) ||
                    !Position.ApproxEquals(m_lastPosition, POSITION_TOLERANCE))
                {
                    // Update the "last" values
                    m_lastPosition = Position;
                    m_lastRotationalVelocity = RotationalVelocity;
                    m_lastVelocity = Velocity;
                    base.RequestPhysicsterseUpdate();
                    base.TriggerSignificantMovement();
                }
            }
        }

        #endregion

        #region Unused code (for prims)

        public override void link(PhysicsActor obj)
        {

        }

        public override void delink()
        {

        }

        public override void LockAngularMotion(Vector3 axis)
        {

        }

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

        #region Vehicle (not used)

        public override void VehicleFloatParam(int param, float value)
        {

        }

        public override void VehicleVectorParam(int param, Vector3 value)
        {

        }

        public override void VehicleRotationParam(int param, Quaternion rotation)
        {

        }

        public override void VehicleFlags(int param, bool remove)
        {

        }

        #endregion

        public override void SetVolumeDetect(int param)
        {

        }

        public void SetAcceleration(Vector3 accel)
        {
            m_pidControllerActive = true;
            _acceleration = accel;
        }

        public override void AddAngularForce(Vector3 force, bool pushforce)
        {

        }

        public override void SetMomentum(Vector3 momentum)
        {
        }

        public override void CrossingFailure()
        {
        }

        public override void SetCameraPos(Vector3 CameraRotation)
        {
        }

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
            if (!collidelock && force != Vector3.Zero)
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
            m_requestedUpdateFrequency = ms;
            m_eventsubscription = ms;
            _parent_scene.addCollisionEventReporting(this);
        }

        public override void UnSubscribeEvents()
        {
            _parent_scene.remCollisionEventReporting(this);
            m_requestedUpdateFrequency = 0;
            m_eventsubscription = 0;
        }

        public void AddCollisionEvent(uint CollidedWith, ContactPoint contact)
        {
            if (m_eventsubscription > 0)
            {
                CollisionEventsThisFrame.addCollider(CollidedWith, contact);
            }
        }

        public void SendCollisions()
        {
            if (m_eventsubscription > m_requestedUpdateFrequency)
            {
                if (CollisionEventsThisFrame != null)
                {
                    base.SendCollisionUpdate(CollisionEventsThisFrame);
                }
                CollisionEventsThisFrame = new CollisionEventUpdate();
                m_eventsubscription = 0;
            }
        }

        public override bool SubscribedEvents()
        {
            if (m_eventsubscription > 0)
                return true;
            return false;
        }

        public void ProcessTaints(float timestep)
        {

            if (m_tainted_isPhysical != m_isPhysical)
            {
                if (m_tainted_isPhysical)
                {
                    // Create avatar capsule and related ODE data
                    if (!(Shell == IntPtr.Zero && Body == IntPtr.Zero && Amotor == IntPtr.Zero))
                    {
                        m_log.Warn("[PHYSICS]: re-creating the following avatar ODE data, even though it already exists - "
                            + (Shell!=IntPtr.Zero ? "Shell ":"")
                            + (Body!=IntPtr.Zero ? "Body ":"")
                            + (Amotor!=IntPtr.Zero ? "Amotor ":""));
                    }
                    AvatarGeomAndBodyCreation(_position.X, _position.Y, _position.Z, m_tensor);
                    
                    _parent_scene.geom_name_map[Shell] = m_name;
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
                        d.JointDestroy(Amotor);
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
                        _parent_scene.geom_name_map.Remove(Shell);
                        Shell = IntPtr.Zero;
                    }

                }

                m_isPhysical = m_tainted_isPhysical;
            }

            if (m_tainted_CAPSULE_LENGTH != CAPSULE_LENGTH)
            {
                if (Shell != IntPtr.Zero && Body != IntPtr.Zero && Amotor != IntPtr.Zero)
                {

                    m_pidControllerActive = true;
                    // no lock needed on _parent_scene.OdeLock because we are called from within the thread lock in OdePlugin's simulate()
                    d.JointDestroy(Amotor);
                    float prevCapsule = CAPSULE_LENGTH;
                    CAPSULE_LENGTH = m_tainted_CAPSULE_LENGTH;
                    //m_log.Info("[SIZE]: " + CAPSULE_LENGTH.ToString());
                    d.BodyDestroy(Body);
                    d.GeomDestroy(Shell);
                    AvatarGeomAndBodyCreation(_position.X, _position.Y,
                                      _position.Z + (CAPSULE_LENGTH - prevCapsule), m_tensor);
                    Velocity = Vector3.Zero;

                    _parent_scene.geom_name_map[Shell] = m_name;
                    _parent_scene.actor_name_map[Shell] = (PhysicsActor)this;
                }
                else
                {
                    m_log.Warn("[PHYSICS]: trying to change capsule size, but the following ODE data is missing - " 
                        + (Shell==IntPtr.Zero ? "Shell ":"")
                        + (Body==IntPtr.Zero ? "Body ":"")
                        + (Amotor==IntPtr.Zero ? "Amotor ":""));
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

        }

        internal void AddCollisionFrameTime(int p)
        {
            // protect it from overflow crashing
            if (m_eventsubscription + p >= int.MaxValue)
                m_eventsubscription = 0;
            m_eventsubscription += p;
        }

        #endregion
    }
}
