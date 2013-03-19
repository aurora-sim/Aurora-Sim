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

using System.Collections.Generic;
using Aurora.Framework.SceneInfo;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Framework
{
    public delegate void RaycastCallback(
        bool hitYN, Vector3 collisionPoint, uint localid, float distance, Vector3 normal);

    public delegate void RayCallback(List<ContactResult> list);

    public struct ContactResult
    {
        public uint ConsumerID;
        public float Depth;
        public Vector3 Normal;
        public Vector3 Pos;
    }

    public delegate void OnCollisionEvent(PhysicsActor actor, PhysicsActor collidedActor, ContactPoint contact);

    public abstract class PhysicsScene
    {
        public virtual float TimeDilation
        {
            get { return 1.0f; }
            set { }
        }

        public virtual float StepTime
        {
            get { return 0; }
        }

        public virtual bool IsThreaded
        {
            get { return false; }
        }

        public virtual bool DisableCollisions { get; set; }

        public virtual List<PhysicsObject> ActiveObjects
        {
            get { return null; }
        }

        public virtual bool UseUnderWaterPhysics
        {
            get { return false; }
        }

        public virtual int StatPhysicsTaintTime { get; protected set; }

        public virtual int StatPhysicsMoveTime { get; protected set; }

        public virtual int StatCollisionOptimizedTime { get; protected set; }

        public virtual int StatSendCollisionsTime { get; protected set; }

        public virtual int StatAvatarUpdatePosAndVelocity { get; protected set; }

        public virtual int StatPrimUpdatePosAndVelocity { get; protected set; }

        public virtual int StatUnlockedArea { get; protected set; }

        public virtual int StatFindContactsTime { get; protected set; }

        public virtual int StatContactLoopTime { get; protected set; }

        public virtual int StatCollisionAccountingTime { get; protected set; }

        public abstract void Initialise(IMesher meshmerizer, IScene scene);
        public abstract void PostInitialise(IConfigSource config);

        public abstract PhysicsCharacter AddAvatar(string avName, Vector3 position, Quaternion rotation, Vector3 size,
                                                   bool isFlying, uint LocalID, UUID UUID);

        public abstract void RemoveAvatar(PhysicsCharacter actor);

        public abstract void RemovePrim(PhysicsObject prim);
        public abstract void DeletePrim(PhysicsObject prim);

        public abstract PhysicsObject AddPrimShape(ISceneChildEntity entity);

        public abstract void Simulate(float timeStep);

        public virtual void GetResults()
        {
        }

        public abstract void SetTerrain(ITerrainChannel channel, short[] heightMap);

        public abstract void SetWaterLevel(double height, short[] map);

        public abstract void Dispose();

        public abstract Dictionary<uint, float> GetTopColliders();

        /// <summary>
        ///     True if the physics plugin supports raycasting against the physics scene
        /// </summary>
        public virtual bool SupportsRayCast()
        {
            return false;
        }

        /// <summary>
        ///     Queue a raycast against the physics scene.
        ///     The provided callback method will be called when the raycast is complete
        ///     Many physics engines don't support collision testing at the same time as
        ///     manipulating the physics scene, so we queue the request up and callback
        ///     a custom method when the raycast is complete.
        ///     This allows physics engines that give an immediate result to callback immediately
        ///     and ones that don't, to callback when it gets a result back.
        ///     ODE for example will not allow you to change the scene while collision testing or
        ///     it asserts, 'opteration not valid for locked space'.  This includes adding a ray to the scene.
        ///     This is named RayCastWorld to not conflict with modrex's Raycast method.
        /// </summary>
        /// <param name="position">Origin of the ray</param>
        /// <param name="direction">Direction of the ray</param>
        /// <param name="length">Length of ray in meters</param>
        /// <param name="retMethod">Method to call when the raycast is complete</param>
        public virtual void RaycastWorld(Vector3 position, Vector3 direction, float length, RaycastCallback retMethod)
        {
            if (retMethod != null)
                retMethod(false, Vector3.Zero, 0, 999999999999f, Vector3.Zero);
        }

        public virtual void RaycastWorld(Vector3 position, Vector3 direction, float length, int Count,
                                         RayCallback retMethod)
        {
            if (retMethod != null)
                retMethod(new List<ContactResult>());
        }

        public virtual List<ContactResult> RaycastWorld(Vector3 position, Vector3 direction, float length, int Count)
        {
            return new List<ContactResult>();
        }

        public virtual void SetGravityForce(bool enabled, float forceX, float forceY, float forceZ)
        {
        }

        public virtual float[] GetGravityForce()
        {
            return new float[3] {0, 0, 0};
        }

        public virtual void AddGravityPoint(bool isApplyingForces, Vector3 position, float forceX, float forceY,
                                            float forceZ, float gravForce, float radius, int identifier)
        {
        }

        public virtual void UpdatesLoop()
        {
        }
    }

    public class NullPhysicsScene : PhysicsScene
    {
        private static int m_workIndicator;

        public override bool DisableCollisions
        {
            get { return false; }
            set { }
        }

        public override bool UseUnderWaterPhysics
        {
            get { return false; }
        }

        public override void Initialise(IMesher meshmerizer, IScene scene)
        {
            // Does nothing right now
        }

        public override void PostInitialise(IConfigSource config)
        {
        }

        public override PhysicsCharacter AddAvatar(string avName, Vector3 position, Quaternion rotation, Vector3 size,
                                                   bool isFlying, uint localID, UUID UUID)
        {
            MainConsole.Instance.InfoFormat("[PHYSICS]: NullPhysicsScene : AddAvatar({0})", position);
            return new NullCharacterPhysicsActor();
        }

        public override void RemoveAvatar(PhysicsCharacter actor)
        {
        }

        public override void RemovePrim(PhysicsObject prim)
        {
        }

        public override void DeletePrim(PhysicsObject prim)
        {
        }

        public override void SetWaterLevel(double height, short[] map)
        {
        }

        /*
                    public override PhysicsActor AddPrim(Vector3 position, Vector3 size, Quaternion rotation)
                    {
                        MainConsole.Instance.InfoFormat("NullPhysicsScene : AddPrim({0},{1})", position, size);
                        return PhysicsActor.Null;
                    }
        */

        public override PhysicsObject AddPrimShape(ISceneChildEntity entity)
        {
            return new NullObjectPhysicsActor();
        }

        public override void Simulate(float timeStep)
        {
            m_workIndicator = (m_workIndicator + 1)%10;
        }

        public override void SetTerrain(ITerrainChannel channel, short[] heightMap)
        {
            MainConsole.Instance.InfoFormat("[PHYSICS]: NullPhysicsScene : SetTerrain({0} items)", heightMap.Length);
        }

        public override void Dispose()
        {
        }

        public override Dictionary<uint, float> GetTopColliders()
        {
            Dictionary<uint, float> returncolliders = new Dictionary<uint, float>();
            return returncolliders;
        }
    }
}