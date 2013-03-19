using Aurora.Framework;
using Aurora.Framework.Physics;
using Aurora.Framework.Utilities;
using OdeAPI;
using OpenMetaverse;
using System;

namespace Aurora.Physics.AuroraOpenDynamicsEngine
{
    public class ODESpecificAvatar : AuroraODECharacter
    {
        #region Declares

        public d.Mass ShellMass;
        protected Vector3 _zeroPosition;

        #endregion

        #region Constructor

        public ODESpecificAvatar(String avName, AuroraODEPhysicsScene parent_scene, Vector3 pos, Quaternion rotation,
                                 Vector3 size) : base(avName, parent_scene, pos, rotation, size)
        {
            base._parent_ref = this;
        }

        #endregion

        #region Move

        private int _appliedFallingForce = 0;

        /// <summary>
        ///     Called from Simulate
        ///     This is the avatar's movement control + PID Controller
        /// </summary>
        /// <param name="timeStep"></param>
        /// <returns>True if the avatar should be removed from the simulation</returns>
        public void Move(float timeStep)
        {
            if (Body == IntPtr.Zero || !IsPhysical)
                return;

            Vector3 vec = Vector3.Zero;
            Vector3 vel = d.BodyGetLinearVel(Body).ToVector3();
            Vector3 tempPos = d.BodyGetPosition(Body).ToVector3();

            #region Flight Ceiling

            // rex, added height check

            if (m_pidControllerActive == false)
                _zeroPosition = tempPos;

            if (_parent_scene.m_useFlightCeilingHeight && tempPos.Z > _parent_scene.m_flightCeilingHeight)
            {
                tempPos.Z = _parent_scene.m_flightCeilingHeight;
                d.BodySetPosition(Body, tempPos.X, tempPos.Y, tempPos.Z);
                if (vel.Z > 0.0f)
                {
                    vel.Z = 0.0f;
                    d.BodySetLinearVel(Body, vel.X, vel.Y, vel.Z);
                }
                if (_target_velocity.Z > 0.0f)
                    _target_velocity.Z = 0.0f;
            }

            // endrex

            #endregion

            #region NonFinite Pos

            Vector3 localPos = new Vector3(tempPos.X, tempPos.Y, tempPos.Z);

            if (!localPos.IsFinite())
            {
                MainConsole.Instance.Warn("[PHYSICS]: Avatar Position is non-finite!");

                _parent_scene.BadCharacter(this);
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
            if (flying)
                movementmult *= _parent_scene.m_AvFlySpeed;

            #endregion

            #region Jump code

            if (IsJumping)
            {
                if (flying ||
                    ((IsColliding) && m_preJumpCounter > _parent_scene.m_preJumpTime || m_preJumpCounter > 150))
                {
                    m_isJumping = false;
                    m_preJumpCounter = 0;
                    _target_velocity.X /= 2;
                    _target_velocity.Y /= 2;
                    _target_velocity.Z = -0.5f;
                }
                else
                {
                    _target_velocity.X = m_preJumpForce.X*_parent_scene.m_preJumpForceMultiplierX/2.5f;
                    _target_velocity.Y = m_preJumpForce.Y*_parent_scene.m_preJumpForceMultiplierY/2.5f;
                    m_preJumpCounter++;
                }
            }
            else if (m_ispreJumping)
            {
                if (m_preJumpCounter == _parent_scene.m_preJumpTime)
                {
                    m_ispreJumping = false;
                    _target_velocity.X = m_preJumpForce.X;
                    _target_velocity.Y = m_preJumpForce.Y;
                    _target_velocity.Z = m_preJumpForce.Z*
                                         (m_alwaysRun
                                              ? _parent_scene.m_preJumpForceMultiplierZ/2.5f
                                              : _parent_scene.m_preJumpForceMultiplierZ/2.25f);

                    m_preJumpCounter = 0;
                    m_isJumping = true;
                }
                else
                {
                    m_preJumpCounter++;
                    TriggerMovementUpdate();
                    return;
                }
            }

            #endregion

            #region Check for underground

            d.AABB aabb;
            d.GeomGetAABB(Shell, out aabb);
            float chrminZ = aabb.MinZ;

            Vector3 posch = localPos;

            float ftmp;

            if (flying)
            {
                ftmp = 0.75f*timeStep;
                posch.X += vel.X*ftmp;
                posch.Y += vel.Y*ftmp;
            }

            float groundHeight = _parent_scene.GetTerrainHeightAtXY(posch.X, posch.Y);

            if (chrminZ < groundHeight)
            {
                float depth = groundHeight - chrminZ;

                if (_target_velocity.Z < 0)
                    _target_velocity.Z = 0;

                if (!flying)
                {
                    if (vel.Z < -10f)
                        vel.Z = -10f;
                    vec.Z = -vel.Z*PID_D*1.5f + depth*PID_P*50.0f;
                }
                else
                {
                    vec.Z = depth*PID_P*50.0f;
                }

                if (depth < 0.12f)
                {
                    if (!m_iscolliding)
                    {
                        m_iscolliding = true;
                        m_colliderfilter = 15;

                        ContactPoint point = new ContactPoint
                                                 {
                                                     Type = ActorTypes.Ground,
                                                     PenetrationDepth = depth,
                                                     Position = {X = localPos.X, Y = localPos.Y, Z = chrminZ},
                                                     SurfaceNormal = new Vector3(0, 0, -1f)
                                                 };

                        //0 is the ground localID
                        AddCollisionEvent(0, point);
                    }
                    vec.Z *= 0.5f;
                }
            }
            /*
                        if(Flying && _target_velocity == Vector3.Zero &&
                            Math.Abs(vel.Z) < 0.1)
                            notMoving = true;
            */

            #endregion

            #region Gravity

            if (!flying)
                vec.Z += -9.8f*35*Mass*(_appliedFallingForce > 100 ? 1 : _appliedFallingForce++/100f)*
                         (this.IsTruelyColliding ? 0.5f : 1.0f);
            else if (_parent_scene.AllowAvGravity && _target_velocity.Z > 0 &&
                     tempPos.Z > _parent_scene.AvGravityHeight) //Should be stop avies from flying upwards
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
            if (IsColliding)
                _appliedFallingForce = 10;

            #endregion

            if (Flying)

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
                    float target_altitude = _parent_scene.GetTerrainHeightAtXY(tempPos.X, tempPos.Y) +
                                            MinimumGroundFlightOffset;

                    //We cheat a bit and do a bit lower than normal
                    if ((tempPos.Z - CAPSULE_LENGTH) < target_altitude ||
                        (tempPos.Z - CAPSULE_LENGTH) <
                        _parent_scene.GetTerrainHeightAtXY(tempPos.X + forwardVel.X, tempPos.Y + forwardVel.Y)
                        + MinimumGroundFlightOffset)
                        if (_target_velocity.Z < 0)
                            vec.Z += (target_altitude - tempPos.Z)*PID_D*0.5f; //Don't apply so much
                        else if ((tempPos.Z - CAPSULE_LENGTH) + 5 < target_altitude)
                            vec.Z += (target_altitude - tempPos.Z)*PID_D*3.05f;
                        else
                            vec.Z += (target_altitude - tempPos.Z)*PID_D*1.75f;
                }
                else
                {
                    //Straight up and down, only apply when they are very close to the ground
                    float target_altitude = _parent_scene.GetTerrainHeightAtXY(tempPos.X, tempPos.Y);

                    if ((tempPos.Z - CAPSULE_LENGTH + (MinimumGroundFlightOffset/1.5)) <
                        target_altitude + MinimumGroundFlightOffset)
                    {
                        if ((tempPos.Z - CAPSULE_LENGTH) < target_altitude + 1)
                        {
                            vec.Z += ((target_altitude + 4) - (tempPos.Z - CAPSULE_LENGTH))*PID_D;
                        }
                        else
                            vec.Z += ((target_altitude + MinimumGroundFlightOffset) - (tempPos.Z - CAPSULE_LENGTH))*
                                     PID_D*0.5f;
                    }
                }

            #endregion

            #region Force application

            #region Force push application

            bool noDisable = false;
            if (_target_force != Vector3.Zero)
            {
                _target_vel_force = _target_force/2;
                _target_force = Vector3.Zero;
                noDisable = true;
            }
            if (_target_vel_force.X != 0)
                vec.X += (_target_vel_force.X)*PID_D*2;
            if (_target_vel_force.Y != 0)
                vec.Y += (_target_vel_force.Y)*PID_D*2;
            if (_target_vel_force.Z != 0)
                vec.Z += (_target_vel_force.Z)*PID_D;

            #endregion

            if (!flying && !IsTruelyColliding)
            {
                //Falling, and haven't yet hit the ground
                vec.X += ((_target_velocity.X + m_forceAppliedBeforeFalling.X)*movementmult - vel.X)*PID_D*2;
                vec.Y += ((_target_velocity.Y + m_forceAppliedBeforeFalling.Y)*movementmult - vel.Y)*PID_D*2;
                vec.Z += ((_target_velocity.Z + m_forceAppliedBeforeFalling.Z)*movementmult - vel.Z)*PID_D;
            }
            else
            {
                m_forceAppliedBeforeFalling = !flying ? Vector3.Zero : _target_velocity;

                vec.X += (_target_velocity.X*movementmult - vel.X)*PID_D*2;
                vec.Y += (_target_velocity.Y*movementmult - vel.Y)*PID_D*2;
                vec.Z += (_target_velocity.Z*movementmult - vel.Z)*PID_D;
            }

            Vector3 combinedForceVelocity = _target_velocity + _target_vel_force;
            if ((combinedForceVelocity == Vector3.Zero ||
                 (Math.Abs(combinedForceVelocity.X) < 0.01f &&
                  Math.Abs(combinedForceVelocity.Y) < 0.01f &&
                  Math.Abs(combinedForceVelocity.Z) < 0.01f)) &&
                Math.Abs(vel.X) < 0.1 && Math.Abs(vel.Y) < 0.1 &&
                Math.Abs(vel.Z) < 0.1 && !(_appliedFallingForce > 0) && !noDisable)
            {
                //Body isn't moving, disable it
                _target_velocity = Vector3.Zero;
                _target_vel_force = Vector3.Zero;
                d.BodySetLinearVel(Body, 0, 0, 0);
                d.BodyDisable(Body);
            }
            else
            {
                _target_velocity *= _parent_scene.AvDecayTime;
                _target_vel_force *= _parent_scene.AvDecayTime;
                d.BodyEnable(Body);
                d.BodyAddForce(Body, vec.X, vec.Y, vec.Z);
            }

            #endregion
        }

        #endregion

        #region Rebuild the avatar representation

        /// <summary>
        ///     This creates the Avatar's physical Surrogate at the position supplied
        ///     WARNING: This MUST NOT be called outside of ProcessTaints, else we can have unsynchronized access
        ///     to ODE internals. ProcessTaints is called from within thread-locked Simulate(), so it is the only
        ///     place that is safe to call this routine AvatarGeomAndBodyCreation.
        /// </summary>
        /// <param name="npositionX"></param>
        /// <param name="npositionY"></param>
        /// <param name="npositionZ"></param>
        public void AvatarGeomAndBodyCreation(float npositionX, float npositionY, float npositionZ) //, float tensor)
        {
            if (CAPSULE_LENGTH <= 0)
            {
                MainConsole.Instance.Warn(
                    "[PHYSICS]: The capsule size you specified in aurora.ini is invalid!  Setting it to the smallest possible size!");
                CAPSULE_LENGTH = 1.2f;
            }

            if (CAPSULE_RADIUS <= 0)
            {
                MainConsole.Instance.Warn(
                    "[PHYSICS]: The capsule size you specified in aurora.ini is invalid!  Setting it to the normal size!");
                CAPSULE_RADIUS = 0.37f;
            }
            Shell = d.CreateCapsule(_parent_scene.space, CAPSULE_RADIUS, CAPSULE_LENGTH);

            d.GeomSetCategoryBits(Shell, (int) m_collisionCategories);
            d.GeomSetCollideBits(Shell, (int) m_collisionFlags);

            d.MassSetCapsule(out ShellMass, 80f, 3, CAPSULE_RADIUS, CAPSULE_LENGTH);

            m_mass = ShellMass.mass;

            // rescale PID parameters 
            PID_D = _parent_scene.PID_D;
            PID_P = _parent_scene.PID_P;


            // rescale PID parameters so that this aren't so affected by mass
            // but more importante, don't get unstable

            PID_D /= 50*80; // original mass of 80, 50 ODE fps ??
            PID_D *= m_mass/_parent_scene.ODE_STEPSIZE;
            PID_P /= 50*80;
            PID_P *= m_mass/_parent_scene.ODE_STEPSIZE;

            Body = d.BodyCreate(_parent_scene.world);

            d.BodySetPosition(Body, npositionX, npositionY, npositionZ);

            // disconnect from world gravity so we can apply buoyancy
            d.BodySetGravityMode(Body, false);
            d.BodySetAutoDisableFlag(Body, false);

            Position = new Vector3(npositionX, npositionY, npositionZ);

            d.BodySetMass(Body, ref ShellMass);
            d.GeomSetBody(Shell, Body);

            Amotor = d.JointCreateAMotor(_parent_scene.world, IntPtr.Zero);
            d.JointAttach(Amotor, Body, IntPtr.Zero);

            d.JointSetAMotorMode(Amotor, 0);
            d.JointSetAMotorNumAxes(Amotor, 3);
            d.JointSetAMotorAxis(Amotor, 0, 0, 1, 0, 0);
            d.JointSetAMotorAxis(Amotor, 1, 0, 0, 1, 0);
            d.JointSetAMotorAxis(Amotor, 2, 0, 0, 0, 1);

            d.JointSetAMotorAngle(Amotor, 0, 0);
            d.JointSetAMotorAngle(Amotor, 1, 0);
            d.JointSetAMotorAngle(Amotor, 2, 0);


            d.JointSetAMotorParam(Amotor, (int) dParam.StopCFM, 0f); // make it HARD
            d.JointSetAMotorParam(Amotor, (int) dParam.StopCFM2, 0f);
            d.JointSetAMotorParam(Amotor, (int) dParam.StopCFM3, 0f);
            d.JointSetAMotorParam(Amotor, (int) dParam.StopERP, 0.8f);
            d.JointSetAMotorParam(Amotor, (int) dParam.StopERP2, 0.8f);
            d.JointSetAMotorParam(Amotor, (int) dParam.StopERP3, 0.8f);

            // These lowstops and high stops are effectively (no wiggle room)
            d.JointSetAMotorParam(Amotor, (int) dParam.LowStop, -1e-5f);
            d.JointSetAMotorParam(Amotor, (int) dParam.HiStop, 1e-5f);
            d.JointSetAMotorParam(Amotor, (int) dParam.LoStop2, -1e-5f);
            d.JointSetAMotorParam(Amotor, (int) dParam.HiStop2, 1e-5f);
            d.JointSetAMotorParam(Amotor, (int) dParam.LoStop3, -1e-5f);
            d.JointSetAMotorParam(Amotor, (int) dParam.HiStop3, 1e-5f);

            d.JointSetAMotorParam(Amotor, (int) d.JointParam.Vel, 0);
            d.JointSetAMotorParam(Amotor, (int) d.JointParam.Vel2, 0);
            d.JointSetAMotorParam(Amotor, (int) d.JointParam.Vel3, 0);

            d.JointSetAMotorParam(Amotor, (int) dParam.FMax, 5e6f);
            d.JointSetAMotorParam(Amotor, (int) dParam.FMax2, 5e6f);
            d.JointSetAMotorParam(Amotor, (int) dParam.FMax3, 5e6f);

            Velocity = Vector3.Zero;

            _parent_scene.actor_name_map[Shell] = this;
        }

        #endregion

        #region Destroy

        public void DestroyBodyThreadLocked()
        {
            if (Amotor != IntPtr.Zero)
            {
                // Kill the Amotor
                d.JointDestroy(Amotor);
                Amotor = IntPtr.Zero;
            }

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

        #endregion

        #region Taints

        public Vector3 GetAngularVelocity()
        {
            Vector3 rvec;
            rvec = d.BodyGetAngularVel(Body).ToVector3();
            return rvec;
        }

        public Vector3 GetLinearVelocity()
        {
            return d.BodyGetLinearVel(Body).ToVector3();
        }

        public Vector3 GetPosition()
        {
            return d.BodyGetPosition(Body).ToVector3();
        }

        public void SetRotationLocked(Quaternion taintRot)
        {
            d.Quaternion q = new d.Quaternion
                                 {
                                     W = taintRot.W,
                                     X = taintRot.X,
                                     Y = taintRot.Y,
                                     Z = taintRot.Z
                                 };
            d.BodySetQuaternion(Body, ref q); // just keep in sync with rest of simutator
        }

        public void SetPositionLocked(Vector3 taintPos)
        {
            d.BodySetPosition(Body, taintPos.X, taintPos.Y, taintPos.Z);

            _position.X = taintPos.X;
            _position.Y = taintPos.Y;
            _position.Z = taintPos.Z;
        }

        #endregion
    }
}