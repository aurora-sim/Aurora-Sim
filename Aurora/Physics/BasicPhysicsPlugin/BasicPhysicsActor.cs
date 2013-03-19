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

using Aurora.Framework.Physics;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Physics.BasicPhysicsPlugin
{
    public class BasicCharacterActor : PhysicsCharacter
    {
        private Vector3 _size;

        public override bool IsJumping
        {
            get { return false; }
        }

        public override bool IsPreJumping
        {
            get { return false; }
        }

        public override float SpeedModifier
        {
            get { return 1.0f; }
            set { }
        }

        public override int PhysicsActorType
        {
            get { return (int) ActorTypes.Agent; }
        }

        public override Vector3 RotationalVelocity { get; set; }

        public override bool SetAlwaysRun
        {
            get { return false; }
            set { return; }
        }

        public override uint LocalID
        {
            get { return 0; }
            set { return; }
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

        public override bool Flying { get; set; }

        public override bool IsTruelyColliding { get; set; }
        public override bool IsColliding { get; set; }

        public override Vector3 Position { get; set; }

        public override Vector3 Size
        {
            get { return _size; }
            set
            {
                _size = value;
                _size.Z = _size.Z/2.0f;
            }
        }

        public override float Mass
        {
            get { return 0f; }
        }

        public override Vector3 Force
        {
            get { return Vector3.Zero; }
            set { return; }
        }

        public override Vector3 Velocity { get; set; }

        public override float CollisionScore
        {
            get { return 0f; }
            set { }
        }

        public override Quaternion Orientation
        {
            get { return Quaternion.Identity; }
            set { }
        }

        public override void AddForce(Vector3 force, bool pushforce)
        {
        }

        public override bool SubscribedEvents()
        {
            return false;
        }

        public override void SendCollisions()
        {
        }

        public override void AddCollisionEvent(uint CollidedWith, ContactPoint contact)
        {
        }
    }

    public class BasicObjectActor : PhysicsObject
    {
        private Vector3 _size;

        public override bool Selected
        {
            set { }
        }

        public override int PhysicsActorType
        {
            get { return (int) ActorTypes.Agent; }
        }

        public override Vector3 RotationalVelocity { get; set; }

        public override uint LocalID
        {
            get { return 0; }
            set { return; }
        }

        public override bool FloatOnWater
        {
            set { return; }
        }

        public override float Buoyancy
        {
            get { return 0f; }
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

        public override bool IsTruelyColliding { get; set; }
        public override bool IsColliding { get; set; }

        public override Vector3 Position { get; set; }

        public override Vector3 Size
        {
            get { return _size; }
            set
            {
                _size = value;
                _size.Z = _size.Z/2.0f;
            }
        }

        public override float Mass
        {
            get { return 0f; }
        }

        public override Vector3 Force
        {
            get { return Vector3.Zero; }
            set { return; }
        }

        public override Vector3 CenterOfMass
        {
            get { return Vector3.Zero; }
        }

        public override Vector3 Velocity { get; set; }

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
            get { return Quaternion.Identity; }
            set { }
        }

        public override Vector3 Acceleration
        {
            get { return Vector3.Zero; }
        }

        public override void AddForce(Vector3 force, bool pushforce)
        {
        }

        public override void AddAngularForce(Vector3 force, bool pushforce)
        {
        }

        public override void CrossingFailure()
        {
        }

        public override void SubscribeEvents(int ms)
        {
        }

        public override void UnSubscribeEvents()
        {
        }

        public override bool SubscribedEvents()
        {
            return false;
        }

        public override void SendCollisions()
        {
        }

        public override void AddCollisionEvent(uint CollidedWith, ContactPoint contact)
        {
        }
    }
}