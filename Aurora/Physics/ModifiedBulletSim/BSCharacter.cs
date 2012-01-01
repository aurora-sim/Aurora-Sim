/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyrightD
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
using Aurora.Framework;

namespace OpenSim.Region.Physics.BulletSPlugin
{
    public class BSCharacter : PhysicsCharacter
    {
        private static readonly string LogHeader = "[BULLETS CHAR]";

        private readonly float _mass = 80f;
        private readonly Vector3 _scale;
        private readonly BSScene _scene;
        private Vector3 _acceleration;
        private UUID _avID;
        private String _avName;
        private float _buoyancy;
        private bool _collidingGround;
        private long _collidingGroundStep;
        private long _collidingStep;
        public float _density = 60f;
        private bool _floatOnWater;
        private bool _flying;
        private Vector3 _force;
        private bool _isColliding;
        private bool _isPhysical;
        private bool _jumping;
        private int _lastCollisionTime;
        private uint _localID;
        private Quaternion _orientation;
        private Vector3 _position;
        private Vector3 _preJumpForce;
        private int _preJumpTime;
        private bool _preJumping;
        private Vector3 _rotationalVelocity;
        private bool _setAlwaysRun;
        private Vector3 _size;

        private float _speedModifier = 1;
        private int _subscribedEventsMs;
        private Vector3 _targetVelocity;
        private bool _targetVelocityIsDecaying;
        private Vector3 _velocity;

        public BSCharacter(uint localID, UUID avID, String avName, BSScene parent_scene, Vector3 pos,
                           Quaternion rotation, Vector3 size, bool isFlying)
        {
            _localID = localID;
            _avID = avID;
            _avName = avName;
            _scene = parent_scene;
            _position = pos;
            _orientation = rotation;
            _size = size;
            _orientation = Quaternion.Identity;
            _velocity = Vector3.Zero;
            _buoyancy = 0f; // characters return a buoyancy of zero
            _scale = new Vector3(1f, 1f, 1f);
            float AVvolume =
                (float) (Math.PI*Math.Pow(_scene.Params.avatarCapsuleRadius, 2)*_scene.Params.avatarCapsuleHeight);
            _density = _scene.Params.avatarDensity;
            _mass = _density*AVvolume;
            _isPhysical = true;

            ShapeData shapeData = new ShapeData
                                      {
                                          ID = _localID,
                                          Type = ShapeData.PhysicsShapeType.SHAPE_AVATAR,
                                          Position = _position,
                                          Rotation = _orientation,
                                          Velocity = _velocity,
                                          Scale = _scale,
                                          Mass = _mass,
                                          Buoyancy = isFlying ? 1f : 0f,
                                          Static = ShapeData.numericFalse,
                                          Friction = _scene.Params.avatarFriction,
                                          Restitution = _scene.Params.defaultRestitution
                                      };

            // do actual create at taint time
#if (!ISWIN)
            _scene.TaintedObject(delegate()
            {
                BulletSimAPI.CreateObject(parent_scene.WorldID, shapeData);
            });
#else
            _scene.TaintedObject(() => BulletSimAPI.CreateObject(parent_scene.WorldID, shapeData));
#endif

            return;
        }

        // called when this character is being destroyed and the resources should be released

        public override Vector3 Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public override uint LocalID
        {
            set { _localID = value; }
            get { return _localID; }
        }

        public override Vector3 Position
        {
            get
            {
                // _position = BulletSimAPI.GetObjectPosition(_scene.WorldID, _localID);
                return _position;
            }
            set
            {
                _position = value;
#if (!ISWIN)
                _scene.TaintedObject(delegate()
                {
                    BulletSimAPI.SetObjectTranslation(_scene.WorldID, _localID, _position, _orientation);
                });
#else
                _scene.TaintedObject(
                    () => BulletSimAPI.SetObjectTranslation(_scene.WorldID, _localID, _position, _orientation));
#endif
            }
        }

        public override float Mass
        {
            get { return _mass; }
        }

        public override Vector3 Force
        {
            get { return _force; }
            set
            {
                _force = value;
                // MainConsole.Instance.DebugFormat("{0}: Force = {1}", LogHeader, _force);
#if (!ISWIN)
                _scene.TaintedObject(delegate()
                {
                    BulletSimAPI.SetObjectForce(_scene.WorldID, _localID, _force);
                });
#else
                _scene.TaintedObject(() => BulletSimAPI.SetObjectForce(_scene.WorldID, _localID, _force));
#endif
            }
        }

        public override Vector3 CenterOfMass
        {
            get { return Vector3.Zero; }
        }

        public override Vector3 Velocity
        {
            get { return _velocity; }
            set
            {
                _velocity = value;
#if (!ISWIN)
                _scene.TaintedObject(delegate()
                {
                    BulletSimAPI.SetObjectVelocity(_scene.WorldID, _localID, _velocity);
                });
#else
                _scene.TaintedObject(() => BulletSimAPI.SetObjectVelocity(_scene.WorldID, _localID, _velocity));
#endif
            }
        }

        public override Vector3 Torque { get; set; }

        public override float CollisionScore { get; set; }

        public override Quaternion Orientation
        {
            get { return _orientation; }
            set
            {
                _orientation = value;
#if (!ISWIN)
                _scene.TaintedObject(delegate()
                {
                    // _position = BulletSimAPI.GetObjectPosition(_scene.WorldID, _localID);
                    BulletSimAPI.SetObjectTranslation(_scene.WorldID, _localID, _position, _orientation);
                });
#else
                _scene.TaintedObject(() => BulletSimAPI.SetObjectTranslation(_scene.WorldID, _localID, _position,
                                                                             _orientation));
#endif
            }
        }

        public override int PhysicsActorType
        {
            get { return (int) ActorTypes.Agent; }
        }

        public override bool IsPhysical
        {
            get { return _isPhysical; }
            set { _isPhysical = value; }
        }

        public override bool Flying
        {
            get { return _flying; }
            set
            {
                if (_flying != value)
                {
                    _flying = value;
                    ChangeFlying();
#if (!ISWIN)
                    _scene.TaintedObject(delegate()
                    {
                        // simulate flying by changing the effect of gravity
                        BulletSimAPI.SetObjectBuoyancy(_scene.WorldID, LocalID, _flying ? 1f : 0f);
                    });
#else
                    _scene.TaintedObject(() => BulletSimAPI.SetObjectBuoyancy(_scene.WorldID, LocalID,
                                                                              _flying ? 1f : 0f));
#endif
                }
            }
        }

        public override bool
            SetAlwaysRun
        {
            get { return _setAlwaysRun; }
            set { _setAlwaysRun = value; }
        }

        public override bool ThrottleUpdates { get; set; }

        public override bool IsColliding
        {
            get { return (_collidingStep == _scene.SimulationStep); }
            set { _isColliding = value; }
        }

        public bool CollidingGround
        {
            get { return (_collidingGroundStep == _scene.SimulationStep); }
            set { _collidingGround = value; }
        }

        public bool CollidingObj { get; set; }

        public override bool FloatOnWater
        {
            set { _floatOnWater = value; }
        }

        public override Vector3 RotationalVelocity
        {
            get { return _rotationalVelocity; }
            set { _rotationalVelocity = value; }
        }

        public override float Buoyancy
        {
            get { return _buoyancy; }
            set
            {
                _buoyancy = value;
#if (!ISWIN)
                _scene.TaintedObject(delegate()
                {
                    // simulate flying by changing the effect of gravity
                    BulletSimAPI.SetObjectBuoyancy(_scene.WorldID, LocalID, _buoyancy);
                });
#else
                _scene.TaintedObject(() => BulletSimAPI.SetObjectBuoyancy(_scene.WorldID, LocalID, _buoyancy));
#endif
            }
        }

        public override bool IsJumping
        {
            get { return _jumping; }
        }

        public override float SpeedModifier
        {
            get { return _speedModifier; }
            set { _speedModifier = value; }
        }

        public override bool IsPreJumping
        {
            get { return _preJumping; }
        }

        public void Destroy()
        {
#if (!ISWIN)
            _scene.TaintedObject(delegate()
            {
                BulletSimAPI.DestroyObject(_scene.WorldID, _localID);
            });
#else
            _scene.TaintedObject(() => BulletSimAPI.DestroyObject(_scene.WorldID, _localID));
#endif
        }

        public override void AddForce(Vector3 force, bool pushforce)
        {
            if (force.IsFinite())
            {
                _force.X += force.X;
                _force.Y += force.Y;
                _force.Z += force.Z;
#if (!ISWIN)
                _scene.TaintedObject(delegate()
                {
                    BulletSimAPI.SetObjectForce(_scene.WorldID, _localID, _force);
                });
#else
                _scene.TaintedObject(() => BulletSimAPI.SetObjectForce(_scene.WorldID, _localID, _force));
#endif
            }
            else
            {
                MainConsole.Instance.WarnFormat("{0}: Got a NaN force applied to a Character", LogHeader);
            }
            //m_lastUpdateSent = false;
        }

        public override void SubscribeEvents(int ms)
        {
            _subscribedEventsMs = ms;
            _lastCollisionTime = Util.EnvironmentTickCount() - _subscribedEventsMs; // make first collision happen
        }

        public override void UnSubscribeEvents()
        {
            _subscribedEventsMs = 0;
        }

        public override bool SubscribedEvents()
        {
            return (_subscribedEventsMs > 0);
        }

        #region Movement/Update

        // The physics engine says that properties have updated. Update same and inform
        // the world that things have changed.
        public void UpdateProperties(EntityProperties entprop)
        {
            bool changed = false;

            #region Updating Position

            if (entprop.ID != 0)
            {
                // we assign to the local variables so the normal set action does not happen
                if (_position != entprop.Position)
                {
                    _position = entprop.Position;
                    changed = true;
                }
                if (_orientation != entprop.Rotation)
                {
                    _orientation = entprop.Rotation;
                    changed = true;
                }
                if (_velocity != entprop.Velocity)
                {
                    changed = true;
                    _velocity = entprop.Velocity;
                }
                if (_acceleration != entprop.Acceleration)
                {
                    _acceleration = entprop.Acceleration;
                    changed = true;
                }
                if (_rotationalVelocity != entprop.RotationalVelocity)
                {
                    changed = true;
                    _rotationalVelocity = entprop.RotationalVelocity;
                }
                if (changed)
                    TriggerMovementUpdate();
            }

            #endregion

            #region Jump code

            if (_preJumping && Util.EnvironmentTickCountSubtract(_preJumpTime) > _scene.PreJumpTime)
            {
                //Time to jump
                _jumping = true;
                _preJumping = false;
                Velocity += _preJumpForce;
                _targetVelocityIsDecaying = false;
                TriggerMovementUpdate();
            }
            if (_jumping && Util.EnvironmentTickCountSubtract(_preJumpTime) > _scene.PreJumpTime + 2000)
            {
                _jumping = false;
                _targetVelocity = Vector3.Zero;
                TriggerMovementUpdate();
            }
            else if (_jumping && Util.EnvironmentTickCountSubtract(_preJumpTime) > _scene.PreJumpTime + 750)
            {
                _targetVelocityIsDecaying = false;
                TriggerMovementUpdate();
            }
            else if (_jumping && Util.EnvironmentTickCountSubtract(_preJumpTime) > _scene.PreJumpTime + 500)
            {
                _targetVelocityIsDecaying = false;
                Velocity -= _preJumpForce/100; //Cut down on going up
                TriggerMovementUpdate();
            }

            #endregion

            #region Decaying velocity

            if (_targetVelocityIsDecaying)
            {
                _targetVelocity *= _scene.DelayingVelocityMultiplier;
                if (_targetVelocity.ApproxEquals(Vector3.Zero, 0.1f) || _velocity == Vector3.Zero)
                    _targetVelocity = Vector3.Zero;
            }
            if (_targetVelocity != Vector3.Zero)
                Velocity = new Vector3(
                    _targetVelocity.X == 0
                        ? Velocity.X
                        : (_targetVelocity.X*0.25f) + (Velocity.X*0.75f),
                    _targetVelocity.Y == 0
                        ? Velocity.Y
                        : (_targetVelocity.Y*0.25f) + (Velocity.Y*0.75f),
                    _targetVelocity.Z == 0
                        ? Velocity.Z
                        : (_targetVelocity.Z*0.25f) + (Velocity.Z*0.75f));
            else if (Velocity.X != 0 || Velocity.Y != 0 || Velocity.Z > 0)
                Velocity *= _scene.DelayingVelocityMultiplier;
            if (Velocity.ApproxEquals(Vector3.Zero, 0.3f))
                Velocity = Vector3.Zero;

            #endregion
        }

        public override void SetMovementForce(Vector3 force)
        {
            if (!_flying)
            {
                if (force.Z >= 0.5f)
                {
                    if (!_scene.AllowJump)
                        return;
                    if (_scene.AllowPreJump)
                    {
                        _preJumping = true;
                        if (force.X == 0 && force.Y == 0)
                            _preJumpForce = force*_scene.PreJumpForceMultiplier*2;
                        else
                            _preJumpForce = force*_scene.PreJumpForceMultiplier*3.5f;
                        _preJumpTime = Util.EnvironmentTickCount();
                        TriggerMovementUpdate();
                        return;
                    }
                    else
                    {
                        _jumping = true;
                        _preJumpTime = Util.EnvironmentTickCountAdd(_scene.PreJumpTime);
                        TriggerMovementUpdate();
                        Velocity += force*_scene.PreJumpForceMultiplier;
                    }
                }
            }
            if (_preJumping)
            {
                TriggerMovementUpdate();
                return;
            }
            if (force != Vector3.Zero)
            {
                float multiplier;
                multiplier = !_setAlwaysRun ? _scene.AvWalkingSpeed : _scene.AvRunningSpeed;

                multiplier *= SpeedModifier*5;
                multiplier *= (1/_scene.TimeDilation); //Factor in Time Dilation
                if (Flying)
                    multiplier *= _scene.AvFlyingSpeed; //Add flying speeds

                _targetVelocity = force*multiplier;
                _targetVelocityIsDecaying = false; //We arn't decaying yet
                if (!_flying)
                    _targetVelocity.Z = Velocity.Z;
                Velocity = _targetVelocity; //Step it up
            }
            else
                _targetVelocityIsDecaying = true; //Start slowing us down
        }

        private void ChangeFlying()
        {
            if (!_flying) //Do this so that we fall down immediately
                _targetVelocity = Vector3.Zero;
        }

        #endregion

        #region Collisions

        private readonly CollisionEventUpdate args = new CollisionEventUpdate();

        public void Collide(uint collidingWith, ActorTypes type, Vector3 contactPoint, Vector3 contactNormal,
                            float pentrationDepth)
        {
            // MainConsole.Instance.DebugFormat("{0}: Collide: ms={1}, id={2}, with={3}", LogHeader, _subscribedEventsMs, LocalID, collidingWith);

            // The following makes IsColliding() and IsCollidingGround() work
            _collidingStep = _scene.SimulationStep;
            if (collidingWith == BSScene.TERRAIN_ID || collidingWith == BSScene.GROUNDPLANE_ID)
            {
                _collidingGroundStep = _scene.SimulationStep;
            }

            // throttle collisions to the rate specified in the subscription
            if (_subscribedEventsMs == 0) return; // don't want collisions
            int nowTime = _scene.SimulationNowTime;
            if (nowTime < (_lastCollisionTime + _subscribedEventsMs)) return;
            _lastCollisionTime = nowTime;

            Dictionary<uint, ContactPoint> contactPoints = new Dictionary<uint, ContactPoint>();
            AddCollisionEvent(collidingWith, new ContactPoint(contactPoint, contactNormal, pentrationDepth, type));
        }

        public override void SendCollisions()
        {
            if (!args.Cleared)
            {
                SendCollisionUpdate(args.Copy());
                args.Clear();
            }
        }

        public override void AddCollisionEvent(uint localID, ContactPoint contact)
        {
            args.addCollider(localID, contact);
        }

        #endregion

        #region ForceSet**

        public override void ForceSetPosition(Vector3 position)
        {
            Position = position;
            _position = position;
        }

        public override void ForceSetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
            Velocity = velocity;
        }

        #endregion
    }
}