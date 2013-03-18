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
using Aurora.Framework;
using OpenMetaverse;

namespace Aurora.Physics.AuroraOpenDynamicsEngine
{
    public class AuroraODECharacter : PhysicsCharacter
    {
        #region Declares

        protected readonly CollisionEventUpdate CollisionEventsThisFrame = new CollisionEventUpdate();
        protected AuroraODEPhysicsScene _parent_scene;
        public float CAPSULE_LENGTH = 2.140599f;
        public float CAPSULE_RADIUS = 0.37f;
        public float MinimumGroundFlightOffset = 3f;
        protected float PID_D;
        protected float PID_P;
        protected bool ShouldBeWalking = true;
        protected bool StartingUnderWater = true;
        protected bool WasUnderWater;
        public IntPtr Amotor = IntPtr.Zero;
        public IntPtr Body = IntPtr.Zero;
        public IntPtr Shell = IntPtr.Zero;

        protected Vector3 _position;
        protected Vector3 _target_velocity;
        protected Vector3 _velocity;
        protected Vector3 _target_force = Vector3.Zero;
        protected Vector3 _target_vel_force = Vector3.Zero;
        protected bool _wasZeroFlagFlying;
        protected bool _zeroFlag;
        protected bool flying;
        protected float lastUnderwaterPush;
        protected int m_ZeroUpdateSent;
        protected bool m_alwaysRun;
        protected CollisionCategories m_collisionCategories = (CollisionCategories.Character);

        // Default, Collide with Other Geometries, spaces, bodies and characters.
        protected const CollisionCategories m_collisionFlags = (CollisionCategories.Geom | CollisionCategories.Space
            | CollisionCategories.Body | CollisionCategories.Character | CollisionCategories.Land);

        //        float m_UpdateTimecntr = 0;
        //        float m_UpdateFPScntr = 0.05f;
        protected bool m_isJumping;
        public bool m_isPhysical; // the current physical status
        protected bool m_iscolliding;

        protected bool m_ispreJumping;
        protected Vector3 m_lastAngVelocity;
        protected Vector3 m_lastPosition;
        protected Vector3 m_lastVelocity;
        public uint m_localID;

        protected float m_mass = 80f;
        protected string m_name = String.Empty;
        protected bool m_pidControllerActive = true;
        protected int m_preJumpCounter;
        protected Vector3 m_preJumpForce = Vector3.Zero;
        protected Vector3 m_rotationalVelocity;
        protected float m_speedModifier = 1.0f;
        protected Quaternion m_taintRotation = Quaternion.Identity;
        protected bool m_shouldBePhysical = true;
        protected int m_lastForceApplied = 0;
        protected Vector3 m_forceAppliedBeforeFalling = Vector3.Zero;
        protected ODESpecificAvatar _parent_ref;

        // unique UUID of this character object
        public UUID m_uuid;
        protected bool realFlying;

        public override bool IsJumping
        {
            get { return m_isJumping; }
        }

        public override bool IsPreJumping
        {
            get { return m_ispreJumping; }
        }

        public override float SpeedModifier
        {
            get { return m_speedModifier; }
            set { m_speedModifier = value; }
        }

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        #endregion

        #region Constructor

        public AuroraODECharacter(String avName, AuroraODEPhysicsScene parent_scene, Vector3 pos, Quaternion rotation,
                                  Vector3 size)
        {
            m_uuid = UUID.Random();
            _parent_scene = parent_scene;

            m_taintRotation = rotation;

            if (pos.IsFinite())
            {
                if (pos.Z > 9999999f || pos.Z < -90f)
                {
                    pos.Z =
                        _parent_scene.GetTerrainHeightAtXY(_parent_scene.Region.RegionSizeX * 0.5f,
                                                          _parent_scene.Region.RegionSizeY * 0.5f) + 5.0f;
                }
                _position = pos;
            }
            else
            {
                _position.X = _parent_scene.Region.RegionSizeX * 0.5f;
                _position.Y = _parent_scene.Region.RegionSizeY * 0.5f;
                _position.Z = _parent_scene.GetTerrainHeightAtXY(_position.X, _position.Y) + 10f;

                MainConsole.Instance.Warn("[PHYSICS]: Got NaN Position on Character Create");
            }


            m_isPhysical = false; // current status: no ODE information exists
            Size = size;
            m_name = avName;
        }

        public void RebuildAvatar()
        {
            if (!(Shell == IntPtr.Zero && Body == IntPtr.Zero))
            {
                MainConsole.Instance.Warn("[PHYSICS]: re-creating the following avatar ODE data, even though it already exists - "
                           + (Shell != IntPtr.Zero ? "Shell " : "")
                           + (Body != IntPtr.Zero ? "Body " : ""));
            }
            _parent_ref.AvatarGeomAndBodyCreation(_position.X, _position.Y, _position.Z);

            _parent_scene.AddCharacter(this);
            m_isPhysical = true;
        }

        #endregion

        #region Properties

        public override int PhysicsActorType
        {
            get { return (int)ActorTypes.Agent; }
        }

        /// <summary>
        ///   If this is set, the avatar will move faster
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

        public override bool FloatOnWater
        {
            set {}
        }

        public override bool IsPhysical
        {
            get { return m_shouldBePhysical; }
            set { m_shouldBePhysical = value; }
        }

        public override bool ThrottleUpdates
        {
            get { return false; }
            set {}
        }

        public override bool Flying
        {
            get { return realFlying; }
            set
            {
                realFlying = value;
                flying = value;

                if (!m_isPhysical)
                    _parent_scene.AddSimulationChange(() => { flying = value; realFlying = value; });
            }
        }

        public override bool IsTruelyColliding { get; set; }

        protected int m_colliderfilter;
        /// <summary>
        ///   Returns if the avatar is colliding in general.
        ///   This includes the ground and objects and avatar.
        ///   in this and next collision sets there is a general set to false
        ///   at begin of loop, so a false is 2 sets while a true is a false plus a 1
        /// </summary>
        public override bool IsColliding
        {
            get { return m_iscolliding; }
            set
            {
                if (value)
                {
                    m_colliderfilter += 15;
                    if (m_colliderfilter > 15)
                        m_colliderfilter = 15;
                }
                else
                {
                    m_colliderfilter--;
                    if (m_colliderfilter < 0)
                        m_colliderfilter = 0;
                }

                m_iscolliding = m_colliderfilter != 0;
            }
        }

        /// <summary>
        ///   This 'puts' an avatar somewhere in the physics space.
        ///   Not really a good choice unless you 'know' it's a good
        ///   spot otherwise you're likely to orbit the avatar.
        /// </summary>
        public override Vector3 Position
        {
            get { return _position; }
            set
            {
                if (value.IsFinite())
                {
                    if (value.Z > 9999999f || value.Z < -90f)
                    {
                        value.Z =
                            _parent_scene.GetTerrainHeightAtXY(_parent_scene.Region.RegionSizeX * 0.5f,
                                                               _parent_scene.Region.RegionSizeY * 0.5f) + 5;
                    }

                    _position.X = value.X;
                    _position.Y = value.Y;
                    _position.Z = value.Z;

                    _parent_scene.AddSimulationChange(() =>
                    {
                        if (!value.ApproxEquals(_position, 0.05f) && Body != null)
                            _parent_ref.SetPositionLocked(value);
                    });
                }
                else
                {
                    MainConsole.Instance.Warn("[PHYSICS]: Got a NaN Position from Scene on a Character");
                }
            }
        }

        public override Vector3 RotationalVelocity
        {
            get { return m_rotationalVelocity; }
            set { m_rotationalVelocity = value; }
        }

        private Vector3 _lastSetSize = Vector3.Zero;
        /// <summary>
        ///   This property sets the height of the avatar only.  We use the height to make sure the avatar stands up straight
        ///   and use it to offset landings properly
        /// </summary>
        public override Vector3 Size
        {
            get { return _lastSetSize; }
            set
            {
                if (value.IsFinite())
                {
                    if (_lastSetSize.Z == value.Z)
                    {
                        //It is the same, do not rebuild
                        MainConsole.Instance.Info(
                            "[Physics]: Not rebuilding the avatar capsule, as it is the same size as the previous capsule.");
                        return;
                    }
                    _lastSetSize = value;

                    m_pidControllerActive = true;
                    CAPSULE_RADIUS = _parent_scene.avCapRadius;
                    CAPSULE_LENGTH = (_lastSetSize.Z * 1.1f) - CAPSULE_RADIUS * 2.0f;
                    Velocity = Vector3.Zero;

                    _parent_scene.AddSimulationChange(() => RebuildAvatar());
                }
                else
                {
                    MainConsole.Instance.Warn("[PHYSICS]: Got a NaN Size from Scene on a Character");
                }
            }
        }

        //
        /// <summary>
        ///   Uses the capped cyllinder volume formula to calculate the avatar's mass.
        ///   This may be used in calculations in the scene/scenepresence
        /// </summary>
        public override float Mass
        {
            get { return m_mass; }
        }

        public override Vector3 Force
        {
            get { return _target_velocity; }
            set {}
        }

        public override Vector3 Velocity
        {
            get
            {
                // There's a problem with Vector3.Zero! Don't Use it Here!
                //if (_zeroFlag)
                //    return Vector3.Zero;
                //And definitely don't set this, otherwise, we never stop sending updates!
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
                    MainConsole.Instance.Warn("[PHYSICS]: Got a NaN velocity from Scene in a Character");
                }
            }
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
                _parent_scene.AddSimulationChange(() =>
                {
                    if (Body != IntPtr.Zero)
                        _parent_ref.SetRotationLocked(value);
                });
            }
        }

        /// <summary>
        ///   turn the PID controller on or off.
        ///   The PID Controller will turn on all by itself in many situations
        /// </summary>
        /// <param name = "status"></param>
        public void SetPidStatus(bool status)
        {
            m_pidControllerActive = status;
        }

        public override void ForceSetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
            m_lastVelocity = velocity;
        }

        public override void ForceSetPosition(Vector3 position)
        {
            _position = position;
            m_lastPosition = position;
        }

        /// <summary>
        ///   This adds to the force that will be used for moving the avatar in the next physics heartbeat iteration.
        /// </summary>
        /// <param name = "force"></param>
        public override void AddMovementForce(Vector3 force)
        {
            _target_velocity += force;
        }

        /// <summary>
        ///   This sets the force that will be used for moving the avatar in the next physics heartbeat iteration.
        ///   Note: we do accept Vector3.Zero here as that is an overriding stop for the physics engine.
        /// </summary>
        /// <param name = "force"></param>
        public override void SetMovementForce(Vector3 force)
        {
            if (_parent_scene.m_allowJump && !Flying && IsColliding && force.Z >= 0.5f)
            {
                if (_parent_scene.m_usepreJump)
                {
                    if (!m_ispreJumping && !m_isJumping)
                    {
                        m_ispreJumping = true;
                        m_preJumpForce = force;
                        m_preJumpCounter = 0;
                        TriggerMovementUpdate();
                        return;
                    }
                }
                else
                {
                    if (!m_isJumping)
                    {
                        m_isJumping = true;
                        m_preJumpCounter = _parent_scene.m_preJumpTime;
                        TriggerMovementUpdate();
                        //Leave the / 2, its there so that the jump doesn't go crazy
                        force.X *= _parent_scene.m_preJumpForceMultiplierX / 2;
                        force.Y *= _parent_scene.m_preJumpForceMultiplierY / 2;
                        force.Z *= _parent_scene.m_preJumpForceMultiplierZ;
                    }
                }
            }
            if (m_ispreJumping || m_isJumping)
            {
                TriggerMovementUpdate();
                return;
            }
            if (force != Vector3.Zero)
                _target_velocity = force;
        }

        #endregion

        #region Methods

        #region Move

        /// <summary>
        ///   Updates the reported position and velocity.  This essentially sends the data up to ScenePresence.
        /// </summary>
        public void UpdatePositionAndVelocity(float timestep)
        {
            if (!IsPhysical)
                return;

            //  no lock; called from Simulate() -- if you call this from elsewhere, gotta lock or do Monitor.Enter/Exit!
            Vector3 vec;
            try
            {
                vec = _parent_ref.GetPosition();
            }
            catch (NullReferenceException)
            {
                _parent_scene.BadCharacter(this);
                vec = new Vector3(_position.X, _position.Y, _position.Z);
                base.RaiseOutOfBounds(_position); // Tells ScenePresence that there's a problem!
                MainConsole.Instance.WarnFormat("[ODEPLUGIN]: Avatar Null reference for Avatar {0}, physical actor {1}", m_name, m_uuid);
            }

            // vec is a ptr into internal ode data better not mess with it

            _position.X = vec.X;
            _position.Y = vec.Y;
            _position.Z = vec.Z;

            if (!_position.IsFinite())
            {
                _parent_scene.BadCharacter(this);
                base.RaiseOutOfBounds(_position); // Tells ScenePresence that there's a problem!
                return;
            }

            try
            {
                vec = _parent_ref.GetLinearVelocity();
            }
            catch (NullReferenceException)
            {
                vec.X = _velocity.X;
                vec.Y = _velocity.Y;
                vec.Z = _velocity.Z;
            }

            Vector3 rvec;
            try
            {
                rvec = _parent_ref.GetAngularVelocity();
            }
            catch (NullReferenceException)
            {
                rvec.X = m_rotationalVelocity.X;
                rvec.Y = m_rotationalVelocity.Y;
                rvec.Z = m_rotationalVelocity.Z;
            }

            m_rotationalVelocity.X = rvec.X;
            m_rotationalVelocity.Y = rvec.Y;
            m_rotationalVelocity.Z = rvec.Z;

            // vec is a ptr into internal ode data better not mess with it

            _velocity.X = vec.X;
            _velocity.Y = vec.Y;
            _velocity.Z = vec.Z;

            if (!_velocity.IsFinite())
            {
                _parent_scene.BadCharacter(this);
                base.RaiseOutOfBounds(_position); // Tells ScenePresence that there's a problem!
                return;
            }

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

            // slow down updates, changed y mind: updates should go at physics fps, acording to movement conditions
/*
            m_UpdateTimecntr += timestep;
            m_UpdateFPScntr = 2.5f * _parent_scene.StepTime;
            if(m_UpdateTimecntr < m_UpdateFPScntr)
                return;

            m_UpdateTimecntr = 0;
*/
            float VELOCITY_TOLERANCE = 0.025f*0.25f;
            if (_parent_scene.TimeDilation < 0.5)
            {
                float percent = (1f - _parent_scene.TimeDilation) * 100;
                VELOCITY_TOLERANCE *= percent * 2;
            }
            //const float ANG_VELOCITY_TOLERANCE = 0.05f;
            const float POSITION_TOLERANCE = 5.0f;
            bool needSendUpdate = false;

            float vlength = (_velocity - m_lastVelocity).LengthSquared();
            float plength = (_position - m_lastPosition).LengthSquared();
            if ( //!VelIsZero &&
                (
                    vlength > VELOCITY_TOLERANCE ||
                    plength > POSITION_TOLERANCE
                //(anglength > ANG_VELOCITY_TOLERANCE) ||
                //true
                //                    (Math.Abs(_lastorientation.X - _orientation.X) > 0.001) ||
                //                    (Math.Abs(_lastorientation.Y - _orientation.Y) > 0.001) ||
                //                    (Math.Abs(_lastorientation.Z - _orientation.Z) > 0.001) ||
                //                    (Math.Abs(_lastorientation.W - _orientation.W) > 0.001)
                ))
            {
                needSendUpdate = true;
                m_ZeroUpdateSent = 3;
                //                            _lastorientation = Orientation;
                //                        base.RequestPhysicsterseUpdate();
                //                        base.TriggerSignificantMovement();
            }
            else if (VelIsZero)
            {
                if (m_ZeroUpdateSent > 0)
                {
                    needSendUpdate = true;
                    m_ZeroUpdateSent--;
                }
            }

            if (needSendUpdate)
            {
                m_lastPosition = _position;
                //                        m_lastRotationalVelocity = RotationalVelocity;
                m_lastVelocity = _velocity;
                m_lastAngVelocity = RotationalVelocity;

                base.TriggerSignificantMovement();
                //Tell any listeners about the new info
                // This is for animations
                base.TriggerMovementUpdate();
            }
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
            //MainConsole.Instance.Debug(movementVector.X + " " + movementVector.Y);
            //MainConsole.Instance.Debug("[PHYSICS] changing avatar tilt");
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
//             //MainConsole.Instance.Info("[PHYSICSAV]: Rotation: " + bodyrotation.M00 + " : " + bodyrotation.M01 + " : " + bodyrotation.M02 + " : " + bodyrotation.M10 + " : " + bodyrotation.M11 + " : " + bodyrotation.M12 + " : " + bodyrotation.M20 + " : " + bodyrotation.M21 + " : " + bodyrotation.M22);
        //         }

        #endregion

        #region Forces

        /// <summary>
        ///   Adds the force supplied to the Target Velocity
        ///   The PID controller takes this target velocity and tries to make it a reality
        /// </summary>
        /// <param name = "force"></param>
        /// <param name = "pushforce"></param>
        public override void AddForce(Vector3 force, bool pushforce)
        {
            if (force.IsFinite())
            {
                if (pushforce)
                {
                    m_pidControllerActive = false;
                    _parent_scene.AddSimulationChange(() =>
                    {
                        if (Body != IntPtr.Zero)
                            _target_force = force;
                    });
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
                MainConsole.Instance.Warn("[PHYSICS]: Got a NaN force applied to a Character");
            }
            //m_lastUpdateSent = false;
        }

        #endregion

        #region Destroy

        /// <summary>
        ///   Cleanup the things we use in the scene.
        /// </summary>
        public override void Destroy()
        {
            m_isPhysical = true;
            _parent_scene.AddSimulationChange(() =>
            {
                _parent_scene.RemoveCharacter(this);
                // destroy avatar capsule and related ODE data
                _parent_ref.DestroyBodyThreadLocked();
                m_isPhysical = false;
            });
        }

        #endregion

        #endregion

        #region Collision events

        public override void AddCollisionEvent(uint CollidedWith, ContactPoint contact)
        {
            CollisionEventsThisFrame.addCollider(CollidedWith, contact);
        }

        public override void SendCollisions()
        {
            if (!IsPhysical)
                return; //Not physical, its not supposed to be here

            if (!CollisionEventsThisFrame.Cleared)
            {
                base.SendCollisionUpdate(CollisionEventsThisFrame.Copy());
                CollisionEventsThisFrame.Clear();
            }
        }

        public override bool SubscribedEvents()
        {
            return true;
        }

        #endregion
    }
}