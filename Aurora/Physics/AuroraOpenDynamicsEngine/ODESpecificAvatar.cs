using System;
using System.Collections.Generic;
using System.Reflection;
using OdeAPI;
using OpenMetaverse;
using Aurora.Framework;

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

        /// <summary>
        ///   Called from Simulate
        ///   This is the avatar's movement control + PID Controller
        /// </summary>
        /// <param name = "timeStep"></param>
        /// <returns>True if the avatar should be removed from the simulation</returns>
        public bool Move(float timeStep)
        {
            //  no lock; for now it's only called from within Simulate()

            // If the PID Controller isn't active then we set our force
            // calculating base velocity to the current position

            if (Body == IntPtr.Zero)
                return false;

            if (!m_shouldBePhysical)
                return false;

            Vector3 vec = Vector3.Zero, tempPos;
            Vector3 vel = d.BodyGetLinearVel(Body).ToVector3();
            d.Vector3 tempPosD;
            d.BodyCopyPosition(Body, out tempPosD);
            tempPos = tempPosD.ToVector3();

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
                    Shell = IntPtr.Zero;
                }
                return true;
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
            //            movementmult *= 1 / _parent_scene.TimeDilation;
            if (flying)
                movementmult *= _parent_scene.m_AvFlySpeed;

            #endregion

            #region Check for underground

            /*d.AABB aabb;
            d.GeomGetAABB(Shell, out aabb);
            float chrminZ = aabb.MinZ;

            Vector3 posch = localPos;

            float ftmp;

            if (flying)
            {
                ftmp = 0.75f * timeStep;
                posch.X += vel.X * ftmp;
                posch.Y += vel.Y * ftmp;
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
                    vec.Z = -vel.Z * PID_D * 1.5f + depth * PID_P * 50.0f;
                }
                else
                {
                    vec.Z = depth * PID_P * 50.0f;
                }

                if (depth < 0.12f)
                {
                    m_iscolliding = true;
                    m_colliderfilter = 2;
                    m_iscollidingGround = true;

                    ContactPoint point = new ContactPoint
                    {
                        Type = ActorTypes.Ground,
                        PenetrationDepth = depth,
                        Position = { X = localPos.X, Y = localPos.Y, Z = chrminZ },
                        SurfaceNormal = new Vector3(0, 0, -1f)
                    };

                    //0 is the ground localID
                    AddCollisionEvent(0, point);
                    vec.Z *= 0.5f;
                }
                else
                    m_iscollidingGround = false;
            }
            else
                m_iscollidingGround = false;
            /*
                        if(Flying && _target_velocity == Vector3.Zero &&
                            Math.Abs(vel.Z) < 0.1)
                            notMoving = true;
            */

            #endregion

            #region Movement

            #region Jump code

            if (IsJumping)
            {
                //                if ((IsColliding) && m_preJumpCounter > _parent_scene.m_preJumpTime || m_preJumpCounter > 150)
                if (flying || ((IsColliding) && m_preJumpCounter > _parent_scene.m_preJumpTime || m_preJumpCounter > 150))
                {
                    m_isJumping = false;
                    m_preJumpCounter = 0;
                    _target_velocity.X /= 2;
                    _target_velocity.Y /= 2;
                    _target_velocity.Z = -0.5f;
                }
                else
                {
                    _target_velocity.X = m_preJumpForce.X * _parent_scene.m_preJumpForceMultiplierX;
                    _target_velocity.Y = m_preJumpForce.Y * _parent_scene.m_preJumpForceMultiplierY;
                    m_preJumpCounter++;
                }
            }
            else if (m_ispreJumping)
            {
                if (m_preJumpCounter == _parent_scene.m_preJumpTime)
                {
                    m_ispreJumping = false;
                    _target_velocity.X = m_preJumpForce.X * _parent_scene.m_preJumpForceMultiplierX;
                    _target_velocity.Y = m_preJumpForce.Y * _parent_scene.m_preJumpForceMultiplierY;
                    _target_velocity.Z = m_preJumpForce.Z * (this.m_alwaysRun ? _parent_scene.m_preJumpForceMultiplierZ / 1.5f : _parent_scene.m_preJumpForceMultiplierZ);

                    m_preJumpCounter = 0;
                    m_isJumping = true;
                }
                else
                {
                    m_preJumpCounter++;
                    TriggerMovementUpdate();
                    return false;
                }
            }


            //This is for jumping on prims, since otherwise, you don't get off the ground sometimes
            //            if (m_iscolliding && m_isJumping && _target_velocity.Z < 1 && !Flying)
            //                _target_velocity.Z += m_preJumpForce.Z * _parent_scene.m_preJumpForceMultiplier;

            #endregion

            Vector3 gravForce = new Vector3();

            //  if velocity is zero, use position control; otherwise, velocity control
            if (_target_velocity == Vector3.Zero &&
                Math.Abs(vel.X) < 0.1 && Math.Abs(vel.Y) < 0.1 && Math.Abs(vel.Z) < 0.1 &&
                (this.m_iscolliding || this.flying || (this._zeroFlag && _wasZeroFlagFlying == flying)))
            //This is so that if we get moved by something else, it will update us in the client
            {
                m_forceAppliedBeforeFalling = Vector3.Zero;
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
                    //                    if (!realFlying)
                    //                        vec.Z +=  - vel.Z * PID_D * 5;
                    //                    else
                    if (flying)
                        vec.Z += -vel.Z * PID_D * 0.5f + (_zeroPosition.Z - tempPos.Z) * PID_P;

                    //                    _parent_scene.CalculateGravity(m_mass, tempPos, true, 0.15f, ref gravForce);
                    //                    vec += gravForce;
                }
            }
            else
            {
                m_pidControllerActive = true;
                _zeroFlag = false;

                if (m_iscolliding)
                {
                    if (!flying) //If there is a ground collision, it sets flying to false, so check against real flying
                    {
                        m_forceAppliedBeforeFalling = Vector3.Zero;
                        // We're standing or walking on something
                        if (_target_velocity.X != 0.0f)
                            vec.X += (_target_velocity.X * movementmult - vel.X) * PID_D * 2;
                        if (_target_velocity.Y != 0.0f)
                            vec.Y += (_target_velocity.Y * movementmult - vel.Y) * PID_D * 2;
                        if (_target_velocity.Z != 0.0f)
                            vec.Z += (_target_velocity.Z * movementmult - vel.Z) * PID_D;
                        /*// We're standing or walking on something
                        vec.X += (_target_velocity.X*movementmult - vel.X)*PID_D*2;
                        vec.Y += (_target_velocity.Y*movementmult - vel.Y)*PID_D*2;
                        if (_target_velocity.Z > 0.0f)
                            vec.Z += (_target_velocity.Z*movementmult - vel.Z)*PID_D;
                                // + (_zeroPosition.Z - tempPos.Z) * PID_P)) _zeropos maybe bad here*/
                    }
                    else
                    {
                        // We're flying and colliding with something
                        vec.X += (_target_velocity.X * movementmult - vel.X) * PID_D * 0.5f;
                        vec.Y += (_target_velocity.Y * movementmult - vel.Y) * PID_D * 0.5f;
                        //if(_target_velocity.Z > 0)
                        vec.Z += (_target_velocity.Z * movementmult - vel.Z) * PID_D * 0.5f;
                    }
                }
                else
                {
                    if (flying)
                    {
                        // we're flying
                        vec.X += (_target_velocity.X * movementmult - vel.X) * PID_D * 0.75f;
                        vec.Y += (_target_velocity.Y * movementmult - vel.Y) * PID_D * 0.75f;
                    }
                    else
                    {
                        if (IsTruelyColliding)
                        {
                            m_forceAppliedBeforeFalling = Vector3.Zero;
                            // We're standing or walking on something
                            if (_target_velocity.X != 0.0f)
                                vec.X += (_target_velocity.X * movementmult - vel.X) * PID_D * 2;
                            if (_target_velocity.Y != 0.0f)
                                vec.Y += (_target_velocity.Y * movementmult - vel.Y) * PID_D * 2;
                            if (_target_velocity.Z != 0.0f)
                                vec.Z += (_target_velocity.Z * movementmult - vel.Z) * PID_D;
                        }
                        else
                        {
                            // we're not colliding and we're not flying so that means we're falling!
                            // m_iscolliding includes collisions with the ground.
                            vec.X += (_target_velocity.X + m_forceAppliedBeforeFalling.X * movementmult * 2 - vel.X) * PID_D * 0.85f;
                            vec.Y += (_target_velocity.Y + m_forceAppliedBeforeFalling.Y * movementmult * 2 - vel.Y) * PID_D * 0.85f;
                        }
                    }
                }


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
                    m_forceAppliedBeforeFalling = _target_velocity;
                }
            }

            if (realFlying)
            {
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
                            vec.Z += (target_altitude - tempPos.Z) * PID_P * 0.5f; //Don't apply so much
                        else
                            vec.Z += (target_altitude - tempPos.Z) * PID_P * 1.05f;
                }
                else
                {
                    //Straight up and down, only apply when they are very close to the ground
                    float target_altitude = _parent_scene.GetTerrainHeightAtXY(tempPos.X, tempPos.Y);

                    if ((tempPos.Z - CAPSULE_LENGTH + (MinimumGroundFlightOffset / 1.5)) <
                        target_altitude + MinimumGroundFlightOffset)
                    {
                        if ((tempPos.Z - CAPSULE_LENGTH) < target_altitude + 1)
                        {
                            vec.Z += ((target_altitude + 4) - (tempPos.Z - CAPSULE_LENGTH)) * PID_P;
                        }
                        else
                            vec.Z += ((target_altitude + MinimumGroundFlightOffset) - (tempPos.Z - CAPSULE_LENGTH)) *
                                     PID_P * 0.5f;
                    }
                }

                #endregion
            }

            #region Gravity

            if (!flying)
                _parent_scene.CalculateGravity(m_mass, tempPos, true, 1.0f, ref gravForce);
            else
                _parent_scene.CalculateGravity(m_mass, tempPos, false, 0.65f, ref gravForce);
            //Allow point gravity and repulsors affect us a bit

            if (_target_velocity == Vector3.Zero && vec.X == 0 && vec.Y == 0 && (m_iscolliding || flying))
                vec = Vector3.Zero;
            else
                vec += gravForce;

            #endregion

            #region Under water physics

            if (_parent_scene.AllowUnderwaterPhysics && tempPos.X < _parent_scene.Region.RegionSizeX &&
                tempPos.Y < _parent_scene.Region.RegionSizeY)
            {
                //Position plus height to av's shoulder (aprox) is just above water
                if ((tempPos.Z + (CAPSULE_LENGTH / 3) - .25f) < _parent_scene.GetWaterLevel(tempPos.X, tempPos.Y))
                {
                    if (StartingUnderWater)
                        ShouldBeWalking = Flying = false;
                    StartingUnderWater = false;
                    WasUnderWater = true;
                    Flying = true;
                    lastUnderwaterPush = 0;
                    if (ShouldBeWalking)
                    {
                        lastUnderwaterPush += (float)(_parent_scene.GetWaterLevel(tempPos.X, tempPos.Y) - tempPos.Z) * 33 +
                                              3;
                        vec.Z += lastUnderwaterPush;
                    }
                    else
                    {
                        lastUnderwaterPush += 3500;
                        lastUnderwaterPush += (float)(_parent_scene.GetWaterLevel(tempPos.X, tempPos.Y) - tempPos.Z) * 8;
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

            if (vec.IsFinite())
            {
                if (vec.X < 100000000 && vec.Y < 10000000 && vec.Z < 10000000)
                //Checks for crazy, going to NaN us values
                {
                    // round small values to zero. those possible are just errors
                    if (Math.Abs(vec.X) < 0.001)
                        vec.X = 0;
                    if (Math.Abs(vec.Y) < 0.001)
                        vec.Y = 0;
                    if (Math.Abs(vec.Z) < 0.001)
                        vec.Z = 0;

                    //ODE autodisables not moving prims, accept it and reenable when we need to
                    if (!d.BodyIsEnabled(Body))
                        d.BodyEnable(Body);

                    if (vec == Vector3.Zero) //if we arn't moving, STOP
                    {
                        if (m_lastForceApplied != -1)
                        {
                            m_lastForceApplied = -1;
                            d.BodySetLinearVel(Body, vec.X, vec.Y, vec.Z);
                        }
                    }
                    else
                    {
                        if (m_lastForceApplied < 5)
                            vec *= m_lastForceApplied / 5;
                        if (vec != Vector3.Zero)
                            //force /= m_mass;
                            d.BodyAddForce(Body, vec.X, vec.Y, vec.Z);
                        m_lastForceApplied++;
                    }

                    //                    if (!_zeroFlag && (!flying || m_iscolliding))
                    //                        AlignAvatarTiltWithCurrentDirectionOfMovement (vec, gravForce);

                    // the Amotor still lets avatar rotation to drift during colisions
                    // so force it back to identity

                    d.Quaternion qtmp;
                    qtmp.W = 1;
                    qtmp.X = 0;
                    qtmp.Y = 0;
                    qtmp.Z = 0;
                    d.BodySetQuaternion(Body, ref qtmp);
                    d.BodySetAngularVel(Body, 0, 0, 0);

                    //When falling, we keep going faster and faster, and eventually, the client blue screens (blue is all you see).
                    // The speed that does this is slightly higher than -30, so we cap it here so we never do that during falling.
                    if (vel.Z < -30)
                    {
                        vel.Z = -30;
                        d.BodySetLinearVel(Body, vel.X, vel.Y, vel.Z);
                    }

                    //Decay out the target velocity DON'T it forces tons of updates

                    _target_velocity *= _parent_scene.m_avDecayTime;
                    if (!_zeroFlag && _target_velocity.ApproxEquals(Vector3.Zero, _parent_scene.m_avStopDecaying))
                        _target_velocity = Vector3.Zero;

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
                MainConsole.Instance.Warn("[PHYSICS]: Got a NaN force vector in Move()");
                MainConsole.Instance.Warn("[PHYSICS]: Avatar Position is non-finite!");
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
                return true;
            }

            #endregion

            return false;
        }

        #endregion

        #region Rebuild the avatar representation

        /// <summary>
        ///   This creates the Avatar's physical Surrogate at the position supplied
        ///   WARNING: This MUST NOT be called outside of ProcessTaints, else we can have unsynchronized access
        ///   to ODE internals. ProcessTaints is called from within thread-locked Simulate(), so it is the only 
        ///   place that is safe to call this routine AvatarGeomAndBodyCreation.
        /// </summary>
        /// <param name = "npositionX"></param>
        /// <param name = "npositionY"></param>
        /// <param name = "npositionZ"></param>
        public void AvatarGeomAndBodyCreation(float npositionX, float npositionY, float npositionZ) //, float tensor)
        {
            _parent_scene.waitForSpaceUnlock(_parent_scene.space);
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

            d.GeomSetCategoryBits(Shell, (int)m_collisionCategories);
            d.GeomSetCollideBits(Shell, (int)m_collisionFlags);

            d.MassSetCapsule(out ShellMass, 80f, 3, CAPSULE_RADIUS, CAPSULE_LENGTH);

            m_mass = ShellMass.mass;

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

            _parent_scene.AddChange(this, changes.Position, _position);

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


            d.JointSetAMotorParam(Amotor, (int)dParam.StopCFM, 0f); // make it HARD
            d.JointSetAMotorParam(Amotor, (int)dParam.StopCFM2, 0f);
            d.JointSetAMotorParam(Amotor, (int)dParam.StopCFM3, 0f);
            d.JointSetAMotorParam(Amotor, (int)dParam.StopERP, 0.8f);
            d.JointSetAMotorParam(Amotor, (int)dParam.StopERP2, 0.8f);
            d.JointSetAMotorParam(Amotor, (int)dParam.StopERP3, 0.8f);

            // These lowstops and high stops are effectively (no wiggle room)
            d.JointSetAMotorParam(Amotor, (int)dParam.LowStop, -1e-5f);
            d.JointSetAMotorParam(Amotor, (int)dParam.HiStop, 1e-5f);
            d.JointSetAMotorParam(Amotor, (int)dParam.LoStop2, -1e-5f);
            d.JointSetAMotorParam(Amotor, (int)dParam.HiStop2, 1e-5f);
            d.JointSetAMotorParam(Amotor, (int)dParam.LoStop3, -1e-5f);
            d.JointSetAMotorParam(Amotor, (int)dParam.HiStop3, 1e-5f);

            d.JointSetAMotorParam(Amotor, (int)d.JointParam.Vel, 0);
            d.JointSetAMotorParam(Amotor, (int)d.JointParam.Vel2, 0);
            d.JointSetAMotorParam(Amotor, (int)d.JointParam.Vel3, 0);

            d.JointSetAMotorParam(Amotor, (int)dParam.FMax, 5e6f);
            d.JointSetAMotorParam(Amotor, (int)dParam.FMax2, 5e6f);
            d.JointSetAMotorParam(Amotor, (int)dParam.FMax3, 5e6f);

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

        public void SetForceLocked(Vector3 taintForce)
        {
            if (taintForce.X != 0f || taintForce.Y != 0f || taintForce.Z != 0)
                d.BodyAddForce(Body, taintForce.X, taintForce.Y, taintForce.Z);
        }

        #endregion
    }
}
