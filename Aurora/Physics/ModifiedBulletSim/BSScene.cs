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
using System.Linq;
using System.Runtime.InteropServices;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;

// TODOs for BulletSim (for BSScene, BSPrim, BSCharacter and BulletSim)
// Parameterize BulletSim. Pass a structure of parameters to the C++ code. Capsule size, friction, ...
// Adjust character capsule size when height is adjusted (ScenePresence.SetHeight)
// Test sculpties
// More efficient memory usage in passing hull information from BSPrim to BulletSim
// Four states of prim: Physical, regular, phantom and selected. Are we modeling these correctly?
//     In SL one can set both physical and phantom (gravity, does not effect others, makes collisions with ground)
//     At the moment, physical and phantom causes object to drop through the terrain
// Should prim.link() and prim.delink() membership checking happen at taint time?
// Mesh sharing. Use meshHash to tell if we already have a hull of that shape and only create once
// Do attachments need to be handled separately? Need collision events. Do not collide with VolumeDetect
// Implement the genCollisions feature in BulletSim::SetObjectProperties (don't pass up unneeded collisions)
// Implement LockAngularMotion
// Decide if clearing forces is the right thing to do when setting position (BulletSim::SetObjectTranslation)
// Built Galton board (lots of MoveTo's) and some slats were not positioned correctly (mistakes scattered)
//      No mistakes with ODE. Shape creation race condition?
// Does NeedsMeshing() really need to exclude all the different shapes?
// 
// Now Not-TODOs
//Implemented PID/APID/PIDHover
//

namespace OpenSim.Region.Physics.BulletSPlugin
{
    public class BSScene : PhysicsScene
    {
        #region Delegates

        public delegate void TaintCallback();

        #endregion

        public const uint TERRAIN_ID = 0; // OpenSim senses terrain with a localID of zero
        public const uint GROUNDPLANE_ID = 1;

        private static readonly string LogHeader = "[BULLETS SCENE]";
        private readonly Object _taintLock = new Object();

        private readonly Dictionary<uint, BSCharacter> m_avatars = new Dictionary<uint, BSCharacter>();
        private readonly List<EntityProperties> m_entProperties = new List<EntityProperties>();
        private readonly ConfigurationParameters[] m_params = new ConfigurationParameters[1];
        private readonly Dictionary<uint, BSPrim> m_prims = new Dictionary<uint, BSPrim>();
        private readonly List<BSPrim> m_vehicles = new List<BSPrim>();
        private bool _allowJump = true;
        private bool _allowPreJump = true;
        private float _avFlyingSpeed = 5.0f;
        private float _avRunningSpeed = 1.25f;
        private float _avWalkingSpeed = 0.75f;
        private float _delayingVelocityMultiplier = 0.98f;
        private bool _forceSimplePrimMeshing; // if a cube or sphere, let Bullet do internal shapes
        private float _maximumObjectMass;
        private bool _meshSculptedPrim = true; // cause scuplted prims to get meshed
        private float _preJumpForceMultiplier = 5;
        private int _preJumpTime = 500;
        private List<TaintCallback> _taintedObjects;
        private BulletSimAPI.DebugLogCallback debugLogCallbackHandle;
        private CollisionDesc[] m_collisionArray;
        private GCHandle m_collisionArrayPinnedHandle;
        private float m_fixedTimeStep;
        private ITerrainChannel m_heightMap;
        private bool m_initialized;
        private int m_maxCollisionsPerFrame;
        private int m_maxSubSteps;
        private int m_maxUpdatesPerFrame;
        private int m_meshLOD;
        private GCHandle m_paramsHandle;
        private RegionInfo m_region;
        private IRegistryCore m_registry;
        private int m_simulationNowTime;
        private long m_simulationStep;
        private EntityProperties[] m_updateArray;
        private GCHandle m_updateArrayPinnedHandle;
        private double m_waterLevel;
        private uint m_worldID;

        public IMesher mesher;

        public BSScene(string identifier)
        {
            m_initialized = false;
        }

        public uint WorldID
        {
            get { return m_worldID; }
        }

        public int MeshLOD
        {
            get { return m_meshLOD; }
        }

        public override bool DisableCollisions { get; set; }

        public long SimulationStep
        {
            get { return m_simulationStep; }
        }

        // A value of the time now so all the collision and update routines do not have to get their own
        // Set to 'now' just before all the prims and actors are called for collisions and updates

        public int SimulationNowTime
        {
            get { return m_simulationNowTime; }
        }

        public ConfigurationParameters Params
        {
            get { return m_params[0]; }
        }

        public Vector3 DefaultGravity
        {
            get { return new Vector3(0f, 0f, Params.gravity); }
        }

        public float MaximumObjectMass
        {
            get { return _maximumObjectMass; }
        }

        public float AvRunningSpeed
        {
            get { return _avRunningSpeed; }
        }

        public float AvFlyingSpeed
        {
            get { return _avFlyingSpeed; }
        }

        public override float TimeDilation { get; set; }

        public float AvWalkingSpeed
        {
            get { return _avWalkingSpeed; }
        }


        public bool AllowJump
        {
            get { return _allowJump; }
        }

        public bool AllowPreJump
        {
            get { return _allowPreJump; }
        }

        public float PreJumpForceMultiplier
        {
            get { return _preJumpForceMultiplier; }
        }

        public int PreJumpTime
        {
            get { return _preJumpTime; }
        }

        public float DelayingVelocityMultiplier
        {
            get { return _delayingVelocityMultiplier; }
        }

        public override float StepTime
        {
            get { return m_fixedTimeStep; }
        }

        public override bool IsThreaded
        {
            get { return false; }
        }

        public override void SetGravityForce(bool enabled, float forceX, float forceY, float forceZ)
        {
            base.SetGravityForce(enabled, forceX, forceY, forceZ);
        }

        public override void Initialise(IMesher meshmerizer, RegionInfo region, IRegistryCore registry)
        {
            m_registry = registry;
            m_region = region;
            mesher = meshmerizer;
        }

        public override void PostInitialise(IConfigSource config)
        {
            // Set default values for physics parameters plus any overrides from the ini file
            GetInitialParameterValues(config);

            if (m_initialized)
                return; //Only do this once
            // Allocate pinned memory to pass parameters.
            m_paramsHandle = GCHandle.Alloc(m_params, GCHandleType.Pinned);


            // allocate more pinned memory close to the above in an attempt to get the memory all together
            m_collisionArray = new CollisionDesc[m_maxCollisionsPerFrame];
            m_collisionArrayPinnedHandle = GCHandle.Alloc(m_collisionArray, GCHandleType.Pinned);
            m_updateArray = new EntityProperties[m_maxUpdatesPerFrame];
            m_updateArrayPinnedHandle = GCHandle.Alloc(m_updateArray, GCHandleType.Pinned);

            // if Debug, enable logging from the unmanaged code
            if (MainConsole.Instance.IsDebugEnabled)
            {
                MainConsole.Instance.DebugFormat("{0}: Initialize: Setting debug callback for unmanaged code", LogHeader);
                debugLogCallbackHandle = BulletLogger;
                BulletSimAPI.SetDebugLogCallback(debugLogCallbackHandle);
            }

            _taintedObjects = new List<TaintCallback>();

            // The bounding box for the simulated world
            Vector3 worldExtent = new Vector3(m_region.RegionSizeX, m_region.RegionSizeY, m_region.RegionSizeZ);

            // MainConsole.Instance.DebugFormat("{0}: Initialize: Calling BulletSimAPI.Initialize.", LogHeader);
            m_worldID = BulletSimAPI.Initialize(worldExtent, m_paramsHandle.AddrOfPinnedObject(),
                                                m_maxCollisionsPerFrame,
                                                m_collisionArrayPinnedHandle.AddrOfPinnedObject(),
                                                m_maxUpdatesPerFrame, m_updateArrayPinnedHandle.AddrOfPinnedObject());

            m_initialized = true;
        }

        // All default parameter values are set here. There should be no values set in the
        // variable definitions.
        private void GetInitialParameterValues(IConfigSource config)
        {
            ConfigurationParameters parms = new ConfigurationParameters();

            _meshSculptedPrim = true; // mesh sculpted prims
            _forceSimplePrimMeshing = false; // use complex meshing if called for

            m_meshLOD = 32;

            m_maxSubSteps = 10;
            m_fixedTimeStep = 1f/60f;
            m_maxCollisionsPerFrame = 2048;
            m_maxUpdatesPerFrame = 2048;
            _maximumObjectMass = 10000.01f;

            parms.defaultFriction = 0.70f;
            parms.defaultDensity = 10.000006836f; // Aluminum g/cm3
            parms.defaultRestitution = 0f;
            parms.collisionMargin = 0.0f;
            parms.gravity = -9.80665f;

            parms.linearDamping = 0.0f;
            parms.angularDamping = 0.0f;
            parms.deactivationTime = 0.2f;
            parms.linearSleepingThreshold = 0.8f;
            parms.angularSleepingThreshold = 1.0f;
            parms.ccdMotionThreshold = 0.5f; // set to zero to disable
            parms.ccdSweptSphereRadius = 0.2f;

            parms.terrainFriction = 0.85f;
            parms.terrainHitFriction = 0.8f;
            parms.terrainRestitution = 0.2f;
            parms.avatarFriction = 0.85f;
            parms.avatarDensity = 60f;
            parms.avatarCapsuleRadius = 0.37f;
            parms.avatarCapsuleHeight = 1.5f; // 2.140599f

            if (config != null)
            {
                // If there are specifications in the ini file, use those values
                // WHEN ADDING OR UPDATING THIS SECTION, BE SURE TO ALSO UPDATE OpenSimDefaults.ini
                IConfig pConfig = config.Configs["ModifiedBulletSim"];
                if (pConfig != null)
                {
                    _meshSculptedPrim = pConfig.GetBoolean("MeshSculptedPrim", _meshSculptedPrim);
                    _forceSimplePrimMeshing = pConfig.GetBoolean("ForceSimplePrimMeshing", _forceSimplePrimMeshing);

                    m_meshLOD = pConfig.GetInt("MeshLevelOfDetail", m_meshLOD);

                    m_maxSubSteps = pConfig.GetInt("MaxSubSteps", m_maxSubSteps);
                    m_fixedTimeStep = pConfig.GetFloat("FixedTimeStep", m_fixedTimeStep);
                    m_maxCollisionsPerFrame = pConfig.GetInt("MaxCollisionsPerFrame", m_maxCollisionsPerFrame);
                    m_maxUpdatesPerFrame = pConfig.GetInt("MaxUpdatesPerFrame", m_maxUpdatesPerFrame);
                    _maximumObjectMass = pConfig.GetFloat("MaxObjectMass", _maximumObjectMass);

                    _allowJump = pConfig.GetBoolean("AllowJump", _allowJump);
                    _allowPreJump = pConfig.GetBoolean("AllowPreJump", _allowPreJump);
                    _preJumpForceMultiplier = pConfig.GetFloat("PreJumpMultiplier", _preJumpForceMultiplier);
                    _delayingVelocityMultiplier = pConfig.GetFloat("DelayingVelocityMultiplier",
                                                                   _delayingVelocityMultiplier);

                    parms.defaultFriction = pConfig.GetFloat("DefaultFriction", parms.defaultFriction);
                    parms.defaultDensity = pConfig.GetFloat("DefaultDensity", parms.defaultDensity);
                    parms.defaultRestitution = pConfig.GetFloat("DefaultRestitution", parms.defaultRestitution);
                    parms.collisionMargin = pConfig.GetFloat("CollisionMargin", parms.collisionMargin);
                    parms.gravity = pConfig.GetFloat("Gravity", parms.gravity);

                    parms.linearDamping = pConfig.GetFloat("LinearDamping", parms.linearDamping);
                    parms.angularDamping = pConfig.GetFloat("AngularDamping", parms.angularDamping);
                    parms.deactivationTime = pConfig.GetFloat("DeactivationTime", parms.deactivationTime);
                    parms.linearSleepingThreshold = pConfig.GetFloat("LinearSleepingThreshold",
                                                                     parms.linearSleepingThreshold);
                    parms.angularSleepingThreshold = pConfig.GetFloat("AngularSleepingThreshold",
                                                                      parms.angularSleepingThreshold);
                    parms.ccdMotionThreshold = pConfig.GetFloat("CcdMotionThreshold", parms.ccdMotionThreshold);
                    parms.ccdSweptSphereRadius = pConfig.GetFloat("CcdSweptSphereRadius", parms.ccdSweptSphereRadius);

                    parms.terrainFriction = pConfig.GetFloat("TerrainFriction", parms.terrainFriction);
                    parms.terrainHitFriction = pConfig.GetFloat("TerrainHitFriction", parms.terrainHitFriction);
                    parms.terrainRestitution = pConfig.GetFloat("TerrainRestitution", parms.terrainRestitution);
                    parms.avatarFriction = pConfig.GetFloat("AvatarFriction", parms.avatarFriction);
                    parms.avatarDensity = pConfig.GetFloat("AvatarDensity", parms.avatarDensity);
                    parms.avatarCapsuleRadius = pConfig.GetFloat("AvatarCapsuleRadius", parms.avatarCapsuleRadius);
                    parms.avatarCapsuleHeight = pConfig.GetFloat("AvatarCapsuleHeight", parms.avatarCapsuleHeight);
                }
            }
            m_params[0] = parms;
        }

        // Called directly from unmanaged code so don't do much
        private void BulletLogger(string msg)
        {
            MainConsole.Instance.Debug("[BULLETS UNMANAGED]:" + msg);
        }

        public override PhysicsCharacter AddAvatar(string avName, Vector3 position, Quaternion rotation, Vector3 size,
                                                   bool isFlying, uint LocalID, UUID UUID)
        {
            // MainConsole.Instance.DebugFormat("{0}: AddAvatar: {1}", LogHeader, avName);
            BSCharacter actor = new BSCharacter(LocalID, UUID, avName, this, position, rotation, size, isFlying);
            lock (m_avatars) m_avatars.Add(LocalID, actor);
            return actor;
        }

        public override void RemoveAvatar(PhysicsCharacter actor)
        {
            // MainConsole.Instance.DebugFormat("{0}: RemoveAvatar", LogHeader);
            if (actor is BSCharacter)
                ((BSCharacter) actor).Destroy();

            try
            {
                lock (m_avatars) m_avatars.Remove(actor.LocalID);
            }
            catch (Exception e)
            {
                MainConsole.Instance.WarnFormat("{0}: Attempt to remove avatar that is not in physics scene: {1}", LogHeader, e);
            }
        }

        public override void RemovePrim(PhysicsObject prim)
        {
            // MainConsole.Instance.DebugFormat("{0}: RemovePrim", LogHeader);
            if (prim is BSPrim)
                ((BSPrim) prim).Destroy();

            try
            {
                lock (m_prims) m_prims.Remove(prim.LocalID);
            }
            catch (Exception e)
            {
                MainConsole.Instance.WarnFormat("{0}: Attempt to remove prim that is not in physics scene: {1}", LogHeader, e);
            }
        }

        public override void DeletePrim(PhysicsObject prim)
        {
            RemovePrim(prim);
        }

        public override PhysicsObject AddPrimShape(ISceneChildEntity entity)
        {
            bool isPhysical = ((entity.ParentEntity.RootChild.Flags & PrimFlags.Physics) != 0);
            bool isPhantom = ((entity.ParentEntity.RootChild.Flags & PrimFlags.Phantom) != 0);
            bool physical = isPhysical & !isPhantom;
            /*IOpenRegionSettingsModule WSModule = entity.ParentEntity.Scene.RequestModuleInterface<IOpenRegionSettingsModule> ();
        if (WSModule != null)
            if (!WSModule.AllowPhysicalPrims)
                physical = false;*/
            BSPrim prim = new BSPrim(entity, isPhysical, this);
            lock (m_prims) m_prims.Add(entity.LocalId, prim);
            return prim;
        }

        // This is a call from the simulator saying that some physical property has been updated.
        // The BulletSim driver senses the changing of relevant properties so this taint 
        // information call is not needed.
        public override void AddPhysicsActorTaint(PhysicsActor prim)
        {
        }

        // Simulate one timestep
        public override void Simulate(float timeStep)
        {
            int updatedEntityCount;
            IntPtr updatedEntitiesPtr;
            int collidersCount;
            IntPtr collidersPtr;

            // prevent simulation until we've been initialized
            if (!m_initialized) return;

            // update the prim states while we know the physics engine is not busy
            ProcessTaints();

            // Some of the prims operate with special vehicle properties
            ProcessVehicles(timeStep);
            ProcessTaints(); // the vehicles might have added taints

            // step the physical world one interval
            m_simulationStep++;
            int numSubSteps = BulletSimAPI.PhysicsStep(m_worldID, timeStep, m_maxSubSteps, m_fixedTimeStep,
                                                       out updatedEntityCount, out updatedEntitiesPtr,
                                                       out collidersCount, out collidersPtr);

            // Don't have to use the pointers passed back since we know it is the same pinned memory we passed in

            // Get a value for 'now' so all the collision and update routines don't have to get their own
            m_simulationNowTime = Util.EnvironmentTickCount();

            List<PhysicsActor> _colliders = new List<PhysicsActor>();
            // If there were collisions, process them by sending the event to the prim.
            // Collisions must be processed before updates.
            if (collidersCount > 0 && !DisableCollisions)
            {
                for (int ii = 0; ii < collidersCount; ii++)
                {
                    uint cA = m_collisionArray[ii].aID;
                    uint cB = m_collisionArray[ii].bID;
                    Vector3 point = m_collisionArray[ii].point;
                    Vector3 normal = m_collisionArray[ii].normal;
                    PhysicsActor c1, c2;
                    SendCollision(cA, cB, point, normal, 0.01f, out c1);
                    SendCollision(cB, cA, point, -normal, 0.01f, out c2);
                    _colliders.Add(c1);
                    _colliders.Add(c2);
                }
                //Send out all the collisions now
#if (!ISWIN)
                foreach (PhysicsActor colID in _colliders)
                {
                    if (colID != null)
                    {
                        colID.SendCollisions();
                    }
                }
#else
                foreach (PhysicsActor colID in _colliders.Where(colID => colID != null))
                {
                    colID.SendCollisions();
                }
#endif
            }
            if (updatedEntityCount > 0)
                lock (m_entProperties)
                    m_entProperties.AddRange(m_updateArray);
        }

        public override void UpdatesLoop()
        {
            //Things work better if we do this... however bad it may be
            foreach (BSCharacter character in m_avatars.Values)
            {
                character.UpdateProperties(new EntityProperties());
            }
            // If any of the objects had updated properties, tell the object it has been changed by the physics engine
            EntityProperties[] props = null;
            lock (m_entProperties)
            {
                if (m_entProperties.Count > 0)
                {
                    props = new EntityProperties[m_entProperties.Count];
                    m_entProperties.CopyTo(props);
                    m_entProperties.Clear();
                }
            }
            if (props != null)
            {
                foreach (EntityProperties entprop in props)
                {
                    // MainConsole.Instance.DebugFormat("{0}: entprop[{1}]: id={2}, pos={3}", LogHeader, ii, entprop.ID, entprop.Position);
                    BSCharacter character;
                    if (m_avatars.TryGetValue(entprop.ID, out character))
                        character.UpdateProperties(entprop);
                    BSPrim prim;
                    if (m_prims.TryGetValue(entprop.ID, out prim))
                        prim.UpdateProperties(entprop);
                }
            }
        }

        // Something has collided
        private void SendCollision(uint localID, uint collidingWith, Vector3 collidePoint, Vector3 collideNormal,
                                   float penitration,
                                   out PhysicsActor collider)
        {
            collider = null;
            if (localID == TERRAIN_ID || localID == GROUNDPLANE_ID)
                return; // don't send collisions to the terrain

            ActorTypes type = ActorTypes.Prim;
            if (collidingWith == TERRAIN_ID || collidingWith == GROUNDPLANE_ID)
                type = ActorTypes.Ground;
            else if (m_avatars.ContainsKey(collidingWith))
                type = ActorTypes.Agent;

            BSPrim prim;
            if (m_prims.TryGetValue(localID, out prim))
            {
                collider = prim;
                prim.Collide(collidingWith, type, collidePoint, collideNormal, penitration);
                return;
            }
            BSCharacter actor;
            if (m_avatars.TryGetValue(localID, out actor))
            {
                collider = actor;
                actor.Collide(collidingWith, type, collidePoint, collideNormal, penitration);
                return;
            }
            return;
        }

        public override void GetResults()
        {
        }

        public override void SetTerrain(ITerrainChannel channel, short[] heightMap)
        {
            m_heightMap = channel;
            this.TaintedObject(delegate
                                   {
                                       float[] t = new float[heightMap.Length];
                                       for (int i = 0; i < heightMap.Length; i++)
                                       {
                                           t[i] = (heightMap[i])/Constants.TerrainCompression;
                                       }
                                       BulletSimAPI.SetHeightmap(m_worldID, t);
                                   });
        }

        public float GetTerrainHeightAtXY(float tX, float tY)
        {
            return m_heightMap[(int) tX, (int) tY];
        }

        public override void SetWaterLevel(double baseheight, short[] map)
        {
            m_waterLevel = baseheight;
        }

        public double GetWaterLevel()
        {
            return m_waterLevel;
        }

        public override void Dispose()
        {
            MainConsole.Instance.DebugFormat("{0}: Dispose()", LogHeader);
        }

        public override Dictionary<uint, float> GetTopColliders()
        {
            return new Dictionary<uint, float>();
        }

        /// <summary>
        ///   Routine to figure out if we need to mesh this prim with our mesher
        /// </summary>
        /// <param name = "pbs"></param>
        /// <returns>true if the prim needs meshing</returns>
        public bool NeedsMeshing(PrimitiveBaseShape pbs)
        {
            // most of this is redundant now as the mesher will return null if it cant mesh a prim
            // but we still need to check for sculptie meshing being enabled so this is the most
            // convenient place to do it for now...

            // int iPropertiesNotSupportedDefault = 0;

            if (pbs.SculptEntry && !_meshSculptedPrim)
            {
                // Render sculpties as boxes
                return false;
            }

            // if it's a standard box or sphere with no cuts, hollows, twist or top shear, return false since Bullet 
            // can use an internal representation for the prim
            if (!_forceSimplePrimMeshing)
            {
                // MainConsole.Instance.DebugFormat("{0}: NeedsMeshing: simple mesh: profshape={1}, curve={2}", LogHeader, pbs.ProfileShape, pbs.PathCurve);
                if ((pbs.ProfileShape == ProfileShape.Square && pbs.PathCurve == (byte) Extrusion.Straight)
                    || (pbs.ProfileShape == ProfileShape.HalfCircle && pbs.PathCurve == (byte) Extrusion.Curve1
                        && pbs.Scale.X == pbs.Scale.Y && pbs.Scale.Y == pbs.Scale.Z))
                {
                    if (pbs.ProfileBegin == 0 && pbs.ProfileEnd == 0
                        && pbs.ProfileHollow == 0
                        && pbs.PathTwist == 0 && pbs.PathTwistBegin == 0
                        && pbs.PathBegin == 0 && pbs.PathEnd == 0
                        && pbs.PathTaperX == 0 && pbs.PathTaperY == 0
                        && pbs.PathScaleX == 100 && pbs.PathScaleY == 100
                        && pbs.PathShearX == 0 && pbs.PathShearY == 0)
                    {
                        return false;
                    }
                }
            }

            /*  TODO: verify that the mesher will now do all these shapes
        if (pbs.ProfileHollow != 0)
            iPropertiesNotSupportedDefault++;

        if ((pbs.PathBegin != 0) || pbs.PathEnd != 0)
            iPropertiesNotSupportedDefault++;

        if ((pbs.PathTwistBegin != 0) || (pbs.PathTwist != 0))
            iPropertiesNotSupportedDefault++; 

        if ((pbs.ProfileBegin != 0) || pbs.ProfileEnd != 0)
            iPropertiesNotSupportedDefault++;

        if ((pbs.PathScaleX != 100) || (pbs.PathScaleY != 100))
            iPropertiesNotSupportedDefault++;

        if ((pbs.PathShearX != 0) || (pbs.PathShearY != 0))
            iPropertiesNotSupportedDefault++;

        if (pbs.ProfileShape == ProfileShape.Circle && pbs.PathCurve == (byte)Extrusion.Straight)
            iPropertiesNotSupportedDefault++;

        if (pbs.ProfileShape == ProfileShape.HalfCircle && pbs.PathCurve == (byte)Extrusion.Curve1 && (pbs.Scale.X != pbs.Scale.Y || pbs.Scale.Y != pbs.Scale.Z || pbs.Scale.Z != pbs.Scale.X))
            iPropertiesNotSupportedDefault++;

        if (pbs.ProfileShape == ProfileShape.HalfCircle && pbs.PathCurve == (byte) Extrusion.Curve1)
            iPropertiesNotSupportedDefault++;

        // test for torus
        if ((pbs.ProfileCurve & 0x07) == (byte)ProfileShape.Square)
        {
            if (pbs.PathCurve == (byte)Extrusion.Curve1)
            {
                iPropertiesNotSupportedDefault++;
            }
        }
        else if ((pbs.ProfileCurve & 0x07) == (byte)ProfileShape.Circle)
        {
            if (pbs.PathCurve == (byte)Extrusion.Straight)
            {
                iPropertiesNotSupportedDefault++;
            }
            // ProfileCurve seems to combine hole shape and profile curve so we need to only compare against the lower 3 bits
            else if (pbs.PathCurve == (byte)Extrusion.Curve1)
            {
                iPropertiesNotSupportedDefault++;
            }
        }
        else if ((pbs.ProfileCurve & 0x07) == (byte)ProfileShape.HalfCircle)
        {
            if (pbs.PathCurve == (byte)Extrusion.Curve1 || pbs.PathCurve == (byte)Extrusion.Curve2)
            {
                iPropertiesNotSupportedDefault++;
            }
        }
        else if ((pbs.ProfileCurve & 0x07) == (byte)ProfileShape.EquilateralTriangle)
        {
            if (pbs.PathCurve == (byte)Extrusion.Straight)
            {
                iPropertiesNotSupportedDefault++;
            }
            else if (pbs.PathCurve == (byte)Extrusion.Curve1)
            {
                iPropertiesNotSupportedDefault++;
            }
        }
        if (iPropertiesNotSupportedDefault == 0)
        {
            return false;
        }
         */
            return true;
        }

        // The calls to the PhysicsActors can't directly call into the physics engine
        // because it might be busy. We we delay changes to a known time.
        // We rely on C#'s closure to save and restore the context for the delegate.
        public void TaintedObject(TaintCallback callback)
        {
            lock (_taintLock)
                _taintedObjects.Add(callback);
            return;
        }

        // When someone tries to change a property on a BSPrim or BSCharacter, the object queues
        // a callback into itself to do the actual property change. That callback is called
        // here just before the physics engine is called to step the simulation.
        public void ProcessTaints()
        {
            if (_taintedObjects.Count > 0) // save allocating new list if there is nothing to process
            {
                // swizzle a new list into the list location so we can process what's there
                List<TaintCallback> oldList;
                lock (_taintLock)
                {
                    oldList = _taintedObjects;
                    _taintedObjects = new List<TaintCallback>();
                }

                foreach (TaintCallback callback in oldList)
                {
                    try
                    {
                        callback();
                    }
                    catch (Exception e)
                    {
                        MainConsole.Instance.ErrorFormat("{0}: ProcessTaints: Exception: {1}", LogHeader, e);
                    }
                }
                oldList.Clear();
            }
        }

        #region Vehicles

        // Make so the scene will call this prim for vehicle actions each tick.
        // Safe to call if prim is already in the vehicle list.
        public void AddVehiclePrim(BSPrim vehicle)
        {
            lock (m_vehicles)
            {
                if (!m_vehicles.Contains(vehicle))
                {
                    m_vehicles.Add(vehicle);
                }
            }
        }

        // Remove a prim from our list of vehicles.
        // Safe to call if the prim is not in the vehicle list.
        public void RemoveVehiclePrim(BSPrim vehicle)
        {
            lock (m_vehicles)
            {
                if (m_vehicles.Contains(vehicle))
                {
                    m_vehicles.Remove(vehicle);
                }
            }
        }

        // Some prims have extra vehicle actions
        // no locking because only called when physics engine is not busy
        private void ProcessVehicles(float timeStep)
        {
            foreach (BSPrim prim in m_vehicles)
            {
                prim.StepVehicle(timeStep);
            }
        }

        #endregion Vehicles
    }
}