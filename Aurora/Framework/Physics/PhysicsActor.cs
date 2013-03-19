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
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using OpenMetaverse;

namespace Aurora.Framework
{
    public struct ContactPoint
    {
        public float PenetrationDepth;
        public Vector3 Position;
        public Vector3 SurfaceNormal;
        public ActorTypes Type;

        public ContactPoint(Vector3 position, Vector3 surfaceNormal, float penetrationDepth, ActorTypes type)
        {
            Type = type;
            Position = position;
            SurfaceNormal = surfaceNormal;
            PenetrationDepth = penetrationDepth;
        }
    }

    public class CollisionEventUpdate : EventArgs
    {
        // Raising the event on the object, so don't need to provide location..  further up the tree knows that info.

        public bool Cleared;
        private Dictionary<uint, ContactPoint> m_objCollisionList = new Dictionary<uint, ContactPoint>();

        public CollisionEventUpdate()
        {
            m_objCollisionList = new Dictionary<uint, ContactPoint>();
        }

        public void addCollider(uint localID, ContactPoint contact)
        {
            Cleared = false;
            /*ContactPoint oldCol;
            if(!m_objCollisionList.TryGetValue(localID, out oldCol))
            {
                */
            lock (m_objCollisionList)
                m_objCollisionList[localID] = contact;
            /*}
            else
            {
                if(oldCol.PenetrationDepth < contact.PenetrationDepth)
                    lock(m_objCollisionList)
                        m_objCollisionList[localID] = contact;
            }*/
        }

        public int Count
        {
            get { lock (m_objCollisionList) return m_objCollisionList.Count; }
        }

        /// <summary>
        ///     Reset all the info about this collider
        /// </summary>
        public void Clear()
        {
            Cleared = true;
            lock (m_objCollisionList)
                m_objCollisionList.Clear();
        }

        public CollisionEventUpdate Copy()
        {
            CollisionEventUpdate c = new CollisionEventUpdate();
            lock (m_objCollisionList)
            {
                foreach (KeyValuePair<uint, ContactPoint> kvp in m_objCollisionList)
                    c.m_objCollisionList.Add(kvp.Key, kvp.Value);
            }
            return c;
        }

        public Dictionary<uint, ContactPoint> GetCollisionEvents()
        {
            Dictionary<uint, ContactPoint> c = new Dictionary<uint, ContactPoint>();
            lock (m_objCollisionList)
            {
                foreach (KeyValuePair<uint, ContactPoint> kvp in m_objCollisionList)
                    c.Add(kvp.Key, kvp.Value);
            }
            return c;
        }
    }

    public delegate void PositionUpdate(Vector3 position);

    public delegate void VelocityUpdate(Vector3 velocity);

    public delegate void OrientationUpdate(Quaternion orientation);

    public enum ActorTypes
    {
        Unknown = 0,
        Agent = 1,
        Prim = 2,
        Ground = 3,
        Water = 4
    }

    public abstract class PhysicsCharacter : PhysicsActor
    {
        public abstract bool IsJumping { get; }
        public abstract float SpeedModifier { get; set; }
        public abstract bool IsPreJumping { get; }
        public abstract bool Flying { get; set; }
        public abstract bool SetAlwaysRun { get; set; }

        public virtual void AddMovementForce(Vector3 force)
        {
        }

        public virtual void SetMovementForce(Vector3 force)
        {
        }

        public virtual void Destroy()
        {
        }

        public delegate bool checkForRegionCrossing();

        public event checkForRegionCrossing OnCheckForRegionCrossing;

        public virtual bool CheckForRegionCrossing()
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            checkForRegionCrossing handler = OnCheckForRegionCrossing;

            if (handler != null)
                return handler();
            return false;
        }
    }

    public abstract class PhysicsObject : PhysicsActor
    {
        public virtual void link(PhysicsObject obj)
        {
        }

        public virtual void delink()
        {
        }

        public virtual bool LinkSetIsColliding { get; set; }

        public virtual void LockAngularMotion(Vector3 axis)
        {
        }

        public virtual PrimitiveBaseShape Shape
        {
            set { if (value == null) throw new ArgumentNullException("value"); }
        }

        public abstract bool Selected { set; }

        public abstract void CrossingFailure();

        public virtual void SetMaterial(int material, bool forceMaterialSettings)
        {
        }

        // set never appears to be called
        public virtual int VehicleType
        {
            get { return 0; }
            set { return; }
        }

        public virtual void VehicleFloatParam(int param, float value)
        {
        }

        public virtual void VehicleVectorParam(int param, Vector3 value)
        {
        }

        public virtual void VehicleRotationParam(int param, Quaternion rotation)
        {
        }

        public virtual void VehicleFlags(int param, bool remove)
        {
        }

        public virtual void SetCameraPos(Quaternion CameraRotation)
        {
        }

        public virtual bool BuildingRepresentation { get; set; }
        public virtual bool BlockPhysicalReconstruction { get; set; }
        public abstract float Buoyancy { get; set; }
        public abstract Vector3 CenterOfMass { get; }
        public abstract Vector3 Torque { get; set; }
        public abstract void SubscribeEvents(int ms);
        public abstract void UnSubscribeEvents();

        //set never appears to be called
        public virtual bool VolumeDetect
        {
            get { return false; }
            set { return; }
        }

        public abstract Vector3 Acceleration { get; }
        public abstract void AddAngularForce(Vector3 force, bool pushforce);

        public virtual void ClearVelocity()
        {
        }

        public event BlankHandler OnPhysicalRepresentationChanged;

        public void FirePhysicalRepresentationChanged()
        {
            if (OnPhysicalRepresentationChanged != null)
                OnPhysicalRepresentationChanged();
        }

        public virtual void Destroy()
        {
        }
    }

    public abstract class PhysicsActor
    {
        // disable warning: public events
#pragma warning disable 67
        public delegate void RequestTerseUpdate();

        public delegate void CollisionUpdate(EventArgs e);

        public delegate void OutOfBounds(Vector3 pos);

        public event RequestTerseUpdate OnRequestTerseUpdate;
        public event RequestTerseUpdate OnSignificantMovement;
        public event RequestTerseUpdate OnPositionAndVelocityUpdate;
        public event CollisionUpdate OnCollisionUpdate;
        public event OutOfBounds OnOutOfBounds;
#pragma warning restore 67

        public abstract Vector3 Size { get; set; }

        public abstract uint LocalID { get; set; }

        public UUID UUID { get; set; }

        public virtual void RequestPhysicsterseUpdate()
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            RequestTerseUpdate handler = OnRequestTerseUpdate;

            if (handler != null)
                handler();
        }

        public virtual void RaiseOutOfBounds(Vector3 pos)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            OutOfBounds handler = OnOutOfBounds;

            if (handler != null)
                handler(pos);
        }

        public virtual void SendCollisionUpdate(EventArgs e)
        {
            CollisionUpdate handler = OnCollisionUpdate;

            if (handler != null)
                handler(e);
        }

        public virtual bool SubscribedToCollisions()
        {
            return OnCollisionUpdate != null;
        }

        public virtual void TriggerSignificantMovement()
        {
            //Call significant movement
            RequestTerseUpdate significantMovement = OnSignificantMovement;

            if (significantMovement != null)
                significantMovement();
        }

        public virtual void TriggerMovementUpdate()
        {
            //Call significant movement
            RequestTerseUpdate movementUpdate = OnPositionAndVelocityUpdate;

            if (movementUpdate != null)
                movementUpdate();
        }

        public abstract Vector3 Position { get; set; }
        public abstract float Mass { get; }
        public abstract Vector3 Force { get; set; }

        public abstract Vector3 Velocity { get; set; }
        public abstract float CollisionScore { get; set; }
        public abstract Quaternion Orientation { get; set; }
        public abstract int PhysicsActorType { get; }
        public abstract bool IsPhysical { get; set; }
        public abstract bool ThrottleUpdates { get; set; }
        public abstract bool IsColliding { get; set; }
        public abstract bool IsTruelyColliding { get; set; }
        public abstract bool FloatOnWater { set; }
        public abstract Vector3 RotationalVelocity { get; set; }

        public abstract void AddForce(Vector3 force, bool pushforce);
        public abstract bool SubscribedEvents();

        public abstract void SendCollisions();
        public abstract void AddCollisionEvent(uint localID, ContactPoint contact);

        public virtual void ForceSetVelocity(Vector3 velocity)
        {
        }

        public virtual void ForceSetPosition(Vector3 position)
        {
        }
    }

    public class NullObjectPhysicsActor : PhysicsObject
    {
        public override Vector3 Position
        {
            get { return Vector3.Zero; }
            set { return; }
        }

        public override uint LocalID
        {
            get { return 0; }
            set { return; }
        }

        public override bool Selected
        {
            set { return; }
        }

        public override float Buoyancy
        {
            get { return 0f; }
            set { return; }
        }

        public override bool FloatOnWater
        {
            set { return; }
        }

        public override Vector3 Size
        {
            get { return Vector3.Zero; }
            set { return; }
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

        public override Vector3 Velocity
        {
            get { return Vector3.Zero; }
            set { return; }
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
            get { return Quaternion.Identity; }
            set { }
        }

        public override Vector3 Acceleration
        {
            get { return Vector3.Zero; }
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

        public override bool IsColliding { get; set; }
        public override bool IsTruelyColliding { get; set; }

        public override int PhysicsActorType
        {
            get { return (int) ActorTypes.Ground; }
        }

        public override Vector3 RotationalVelocity
        {
            get { return Vector3.Zero; }
            set { return; }
        }

        public override void CrossingFailure()
        {
        }

        public override void AddForce(Vector3 force, bool pushforce)
        {
        }

        public override void AddAngularForce(Vector3 force, bool pushforce)
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

    public class NullCharacterPhysicsActor : PhysicsCharacter
    {
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

        public override Vector3 Position
        {
            get { return Vector3.Zero; }
            set { return; }
        }

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

        public override Vector3 Size
        {
            get { return Vector3.Zero; }
            set { return; }
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

        public override Vector3 Velocity
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

        public override bool IsPhysical
        {
            get { return false; }
            set { return; }
        }

        public override bool Flying
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

        public override int PhysicsActorType
        {
            get { return (int) ActorTypes.Unknown; }
        }

        public override Vector3 RotationalVelocity
        {
            get { return Vector3.Zero; }
            set { return; }
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
}