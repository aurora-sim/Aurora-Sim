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

using Aurora.Framework;
using Aurora.Framework.Utilities;
using Nini.Config;
using OdeAPI;
using OpenMetaverse;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Aurora.Physics.AuroraOpenDynamicsEngine
{
    public class AuroraODEPhysicsScene : PhysicsScene
    {
        #region Declares

        public float ODE_STEPSIZE = 0.020f;
        protected float m_timeDilation = 1.0f;

        protected int framecount;

        public float gravityx;
        public float gravityy;
        public float gravityz = -9.8f;
        public Vector3 gravityVector;
        public Vector3 gravityVectorNormalized;
        public bool m_hasSetUpPrims;

        protected readonly IntPtr contactgroup;

        protected float contactsurfacelayer = 0.001f;

        public int geomContactPointsStartthrottle = 3;

        protected int contactsPerCollision = 80;
        protected IntPtr ContactgeomsArray = IntPtr.Zero;

        protected const int maxContactsbeforedeath = 2000;
        protected int m_currentmaxContactsbeforedeath = maxContactsbeforedeath;

        protected IntPtr GlobalContactsArray = IntPtr.Zero;

        public const d.ContactFlags CommumContactFlags =
            d.ContactFlags.SoftERP | d.ContactFlags.Bounce | d.ContactFlags.Approx1;

        protected d.Contact newGlobalcontact;

        protected float AvatarContactBounce = 0.3f;
        protected float FrictionMovementMultiplier = 0.3f; // should lower than one
        protected float FrictionScale = 5.0f;

        protected int HashspaceLow = -3; // current ODE limits
        protected int HashspaceHigh = 8;

        protected int GridSpaceScaleBits = 5;
        // used to do shifts to find space from position. Value decided from region size in init

        protected int nspacesPerSideX = 8;
        protected int nspacesPerSideY = 8;

        public float PID_D = 2200f;
        public float PID_P = 900f;
        public float avCapRadius = 0.37f;
        public float avDensity = 80f;
        public float avHeightFudgeFactor = 0.52f;
        public float avMovementDivisorWalk = 1.3f;
        public float avMovementDivisorRun = 0.8f;
        protected float minimumGroundFlightOffset = 3f;
        public float maximumMassObject = 100000.01f;
        public bool meshSculptedPrim = true;
        public bool forceSimplePrimMeshing = true;
        public float meshSculptLOD = 32;
        public float MeshSculptphysicalLOD = 16;
        public int geomCrossingFailuresBeforeOutofbounds = 1;
        public int bodyFramesAutoDisable = 10;
        protected bool m_filterCollisions;

        protected readonly d.NearCallback nearCallback;
        protected readonly HashSet<AuroraODECharacter> _characters = new HashSet<AuroraODECharacter>();
        protected readonly HashSet<AuroraODEPrim> _prims = new HashSet<AuroraODEPrim>();
        protected readonly object _activeprimsLock = new object();
        protected readonly HashSet<AuroraODEPrim> _activeprims = new HashSet<AuroraODEPrim>();

        public override List<PhysicsObject> ActiveObjects
        {
            get { return new List<AuroraODEPrim>(_activeprims).ConvertAll<PhysicsObject>(prim => prim); }
        }

        public ConcurrentQueue<NoParam> SimulationChangesQueue = new ConcurrentQueue<NoParam>();

        protected readonly List<d.ContactGeom> _perloopContact = new List<d.ContactGeom>();

        protected readonly Dictionary<UUID, PhysicsActor> _collisionEventDictionary =
            new Dictionary<UUID, PhysicsActor>();

        protected readonly object _collisionEventListLock = new object();

        public Dictionary<IntPtr, PhysicsActor> actor_name_map = new Dictionary<IntPtr, PhysicsActor>();


        public IntPtr RegionTerrain;
        protected short[] TerrainHeightFieldHeights;
        protected float[] ODETerrainHeightFieldHeights;
        protected ITerrainChannel m_channel;
        protected double WaterHeight = -1;
        public bool m_allowJump = true;
        public bool m_usepreJump = true;
        public int m_preJumpTime = 15;
        public float m_preJumpForceMultiplierX = 6;
        public float m_preJumpForceMultiplierY = 6;
        public float m_preJumpForceMultiplierZ = 4.5f;
        public float m_AvFlySpeed = 4.0f;


        protected int m_physicsiterations = 10;
        //protected int m_timeBetweenRevertingAutoConfigIterations = 50;
        protected const float m_SkipFramesAtms = 0.150f; // Drop frames gracefully at a 150 ms lag
        protected readonly PhysicsActor PANull = new NullObjectPhysicsActor();
        protected float step_time;
        protected RegionInfo m_region;
        protected IScene m_scene;
        protected IWindModule m_windModule;
        protected bool DoPhyWind;

        public RegionInfo Region
        {
            get { return m_region; }
        }

        public override float StepTime
        {
            get { return ODE_STEPSIZE; }
        }

        public IntPtr world;

        public IntPtr space;

        // split static geometry collision handling into spaces of 30 meters
        public IntPtr[,] staticPrimspace;

        public IMesher mesher;

        protected IConfigSource m_config;

        protected volatile int m_global_contactcount;


        public Vector2 WorldExtents;

        public bool AllowUnderwaterPhysics;
        public bool AllowAvGravity = true;
        public int AvGravityHeight = 4096;
        public bool AllowAvsToEscapeGravity = true;

        public float m_flightCeilingHeight = 2048.0f; // rex
        public bool m_useFlightCeilingHeight;

        protected AuroraODERayCastRequestManager m_rayCastManager;
        protected bool IsLocked;
        protected ConcurrentQueue<PhysicsObject> RemoveQueue = new ConcurrentQueue<PhysicsObject>();
        protected ConcurrentQueue<PhysicsObject> DeleteQueue = new ConcurrentQueue<PhysicsObject>();
        protected readonly HashSet<PhysicsActor> ActiveAddCollisionQueue = new HashSet<PhysicsActor>();
        protected readonly HashSet<PhysicsActor> ActiveRemoveCollisionQueue = new HashSet<PhysicsActor>();

        internal float AvDecayTime = 0.95f;

        public override bool UseUnderWaterPhysics
        {
            get { return AllowUnderwaterPhysics; }
        }

        #region Stats

        public override int StatPhysicsTaintTime { get; protected set; }

        public override int StatPhysicsMoveTime { get; protected set; }

        public override int StatCollisionOptimizedTime { get; protected set; }

        public override int StatSendCollisionsTime { get; protected set; }

        public override int StatAvatarUpdatePosAndVelocity { get; protected set; }

        public override int StatPrimUpdatePosAndVelocity { get; protected set; }

        public override int StatUnlockedArea { get; protected set; }

        public override int StatFindContactsTime { get; protected set; }

        public override int StatContactLoopTime { get; protected set; }

        public override int StatCollisionAccountingTime { get; protected set; }

        #endregion

        #endregion

        #region Constructor/Initialization

        /// <summary>
        ///     Initiailizes the scene
        ///     Sets many properties that ODE requires to be stable
        ///     These settings need to be tweaked 'exactly' right or weird stuff happens.
        /// </summary>
        public AuroraODEPhysicsScene()
        {
            nearCallback = near;
            // Create the world and the first space
            world = d.WorldCreate();
            space = d.HashSpaceCreate(IntPtr.Zero);


            contactgroup = d.JointGroupCreate(0);

            d.WorldSetAutoDisableFlag(world, false);
        }

        // Initialize the mesh plugin
        public override void Initialise(IMesher meshmerizer, IScene scene)
        {
            mesher = meshmerizer;
            m_region = scene.RegionInfo;
            m_scene = scene;
            WorldExtents = new Vector2(m_region.RegionSizeX, m_region.RegionSizeY);
        }

        public override void PostInitialise(IConfigSource config)
        {
            m_rayCastManager = new AuroraODERayCastRequestManager(this);
            m_config = config;
            PID_D = 2200.0f;
            PID_P = 900.0f;

            if (m_config != null)
            {
                IConfig physicsconfig = m_config.Configs["AuroraODEPhysicsSettings"];
                if (physicsconfig != null)
                {
                    gravityx = physicsconfig.GetFloat("world_gravityx", 0f);
                    gravityy = physicsconfig.GetFloat("world_gravityy", 0f);
                    gravityz = physicsconfig.GetFloat("world_gravityz", -9.8f);
                    //Set the vectors as well
                    gravityVector = new Vector3(gravityx, gravityy, gravityz);
                    gravityVectorNormalized = gravityVector;
                    gravityVectorNormalized.Normalize();

                    AvDecayTime = physicsconfig.GetFloat("avDecayTime", AvDecayTime);

                    AllowUnderwaterPhysics = physicsconfig.GetBoolean("useUnderWaterPhysics", false);
                    AllowAvGravity = physicsconfig.GetBoolean("useAvGravity", true);
                    AvGravityHeight = physicsconfig.GetInt("avGravityHeight", 4096);
                    AllowAvsToEscapeGravity = physicsconfig.GetBoolean("aviesCanEscapeGravity", true);

                    m_AvFlySpeed = physicsconfig.GetFloat("AvFlySpeed", m_AvFlySpeed);
                    m_allowJump = physicsconfig.GetBoolean("AllowJump", m_allowJump);
                    m_usepreJump = physicsconfig.GetBoolean("UsePreJump", m_usepreJump);
                    m_preJumpTime = physicsconfig.GetInt("PreJumpTime", m_preJumpTime);
                    m_preJumpForceMultiplierX = physicsconfig.GetFloat("PreJumpMultiplierX", m_preJumpForceMultiplierX);
                    m_preJumpForceMultiplierY = physicsconfig.GetFloat("PreJumpMultiplierY", m_preJumpForceMultiplierY);
                    m_preJumpForceMultiplierZ = physicsconfig.GetFloat("PreJumpMultiplierZ", m_preJumpForceMultiplierZ);

                    contactsurfacelayer = physicsconfig.GetFloat("world_contact_surface_layer", 0.001f);

                    AvatarContactBounce = physicsconfig.GetFloat("AvatarContactBounce", AvatarContactBounce);
                    FrictionMovementMultiplier = physicsconfig.GetFloat("FrictionMovementMultiplier",
                                                                        FrictionMovementMultiplier);
                    FrictionScale = physicsconfig.GetFloat("FrictionMovementMultiplier", FrictionScale);

                    ODE_STEPSIZE = physicsconfig.GetFloat("world_stepsize", 0.020f);
                    m_physicsiterations = physicsconfig.GetInt("world_internal_steps_without_collisions", 10);

                    avDensity = physicsconfig.GetFloat("av_density", 80f);
                    avHeightFudgeFactor = physicsconfig.GetFloat("av_height_fudge_factor", 0.52f);
                    avMovementDivisorWalk = (physicsconfig.GetFloat("WalkSpeed", 1.3f)*2);
                    avMovementDivisorRun = (physicsconfig.GetFloat("RunSpeed", 0.8f)*2);
                    avCapRadius = physicsconfig.GetFloat("av_capsule_radius", 0.37f);

                    contactsPerCollision = physicsconfig.GetInt("contacts_per_collision", 80);

                    geomContactPointsStartthrottle = physicsconfig.GetInt("geom_contactpoints_start_throttling", 3);
                    geomCrossingFailuresBeforeOutofbounds =
                        physicsconfig.GetInt("geom_crossing_failures_before_outofbounds", 5);

                    bodyFramesAutoDisable = physicsconfig.GetInt("body_frames_auto_disable", 10);

                    forceSimplePrimMeshing = physicsconfig.GetBoolean("force_simple_prim_meshing",
                                                                      forceSimplePrimMeshing);
                    meshSculptedPrim = physicsconfig.GetBoolean("mesh_sculpted_prim", true);
                    meshSculptLOD = physicsconfig.GetFloat("mesh_lod", 32f);
                    MeshSculptphysicalLOD = physicsconfig.GetFloat("mesh_physical_lod", 16f);
                    m_filterCollisions = physicsconfig.GetBoolean("filter_collisions", false);

                    PID_D = physicsconfig.GetFloat("av_pid_derivative", PID_D);
                    PID_P = physicsconfig.GetFloat("av_pid_proportional", PID_P);

                    m_useFlightCeilingHeight = physicsconfig.GetBoolean("Use_Flight_Ceiling_Height_Max",
                                                                        m_useFlightCeilingHeight);
                    m_flightCeilingHeight = physicsconfig.GetFloat("Flight_Ceiling_Height_Max", m_flightCeilingHeight);
                    //Rex

                    minimumGroundFlightOffset = physicsconfig.GetFloat("minimum_ground_flight_offset", 6f);
                    maximumMassObject = physicsconfig.GetFloat("maximum_mass_object", 100000.01f);
                    DoPhyWind = physicsconfig.GetBoolean("do_physics_wind", false);
                }
            }

            // alloc unmanaged memory to receive information from colision contact joints              
            ContactgeomsArray = Marshal.AllocHGlobal(contactsPerCollision*d.ContactGeom.unmanagedSizeOf);

            // alloc unmanaged memory to pass information to colision contact joints              
            GlobalContactsArray = Marshal.AllocHGlobal(maxContactsbeforedeath*d.Contact.unmanagedSizeOf);

            newGlobalcontact.surface.mode = CommumContactFlags;
            newGlobalcontact.surface.soft_cfm = 0.0001f;
            newGlobalcontact.surface.soft_erp = 0.6f;

            // Set the gravity,, don't disable things automatically (we set it explicitly on some things)
            d.WorldSetGravity(world, gravityx, gravityy, gravityz);
            d.WorldSetContactSurfaceLayer(world, contactsurfacelayer);

            d.WorldSetLinearDamping(world, 0.001f);
            d.WorldSetAngularDamping(world, 0.001f);
            d.WorldSetAngularDampingThreshold(world, 0f);
            d.WorldSetLinearDampingThreshold(world, 0f);
            d.WorldSetMaxAngularSpeed(world, 256f);

            d.WorldSetCFM(world, 1e-6f); // a bit harder than default
            d.WorldSetERP(world, 0.6f); // higher than original

            d.WorldSetContactMaxCorrectingVel(world, 30.0f);

            // Set how many steps we go without running collision testing
            // This is in addition to the step size.
            // Essentially Steps * m_physicsiterations
            d.WorldSetQuickStepNumIterations(world, m_physicsiterations);
            //d.WorldSetContactMaxCorrectingVel(world, 1000.0f);

            if (staticPrimspace != null)
                return; //Reloading config, don't mess with this stuff

            d.HashSpaceSetLevels(space, HashspaceLow, HashspaceHigh);

            //  spaces grid for static objects

            if (WorldExtents.X < WorldExtents.Y)
                // // constant is 1/log(2),  -3 for division by 8 plus 0.5 for rounding
                GridSpaceScaleBits = (int) (Math.Log(WorldExtents.X)*1.4426950f - 2.5f);
            else
                GridSpaceScaleBits = (int) (Math.Log(WorldExtents.Y)*1.4426950f - 2.5f);

            if (GridSpaceScaleBits < 4) // no less than 16m side
                GridSpaceScaleBits = 4;
            else if (GridSpaceScaleBits > 10)
                GridSpaceScaleBits = 10; // no more than 1Km side

            int nspacesPerSideX2 = (int) (WorldExtents.X) >> GridSpaceScaleBits;
            int nspacesPerSideY2 = (int) (WorldExtents.Y) >> GridSpaceScaleBits;

            if ((int) (WorldExtents.X) > nspacesPerSideX2 << GridSpaceScaleBits)
                nspacesPerSideX2++;
            if ((int) (WorldExtents.Y) > nspacesPerSideY2 << GridSpaceScaleBits)
                nspacesPerSideY2++;

            staticPrimspace = new IntPtr[nspacesPerSideX2,nspacesPerSideY2];

            IntPtr aSpace;

            for (int i = 0; i < nspacesPerSideX2; i++)
            {
                for (int j = 0; j < nspacesPerSideY2; j++)
                {
                    aSpace = d.HashSpaceCreate(space);
                    staticPrimspace[i, j] = aSpace;
                    d.GeomSetCategoryBits(aSpace, (int) CollisionCategories.Space);
                    d.HashSpaceSetLevels(aSpace, -2, 8);
                    d.SpaceSetSublevel(aSpace, 1);
                }
            }
        }

        #endregion

        #region Collision Detection

        private bool GetCurContactGeom(int index, ref d.ContactGeom newcontactgeom)
        {
            if (ContactgeomsArray == IntPtr.Zero || index >= contactsPerCollision)
                return false;

            IntPtr contactptr = new IntPtr(ContactgeomsArray.ToInt64() + (index*d.ContactGeom.unmanagedSizeOf));
            newcontactgeom = (d.ContactGeom) Marshal.PtrToStructure(contactptr, typeof (d.ContactGeom));
            return true;
        }

        private IntPtr CreateContacJoint(d.ContactGeom geom)
        {
            if (GlobalContactsArray == IntPtr.Zero || m_global_contactcount >= m_currentmaxContactsbeforedeath)
                return IntPtr.Zero;

            // damm copy...
            newGlobalcontact.geom.depth = geom.depth;
            newGlobalcontact.geom.g1 = geom.g1;
            newGlobalcontact.geom.g2 = geom.g2;
            newGlobalcontact.geom.pos = geom.pos;
            newGlobalcontact.geom.normal = geom.normal;
            newGlobalcontact.geom.side1 = geom.side1;
            newGlobalcontact.geom.side2 = geom.side2;

            IntPtr contact =
                new IntPtr(GlobalContactsArray.ToInt64() + (m_global_contactcount*d.Contact.unmanagedSizeOf));
            Marshal.StructureToPtr(newGlobalcontact, contact, false);
            return d.JointCreateContactPtr(world, contactgroup, contact);
        }

        /// <summary>
        ///     This is our near callback.  A geometry is near a body
        /// </summary>
        /// <param name="space">The space that contains the geoms.  Remember, spaces are also geoms</param>
        /// <param name="g1">a geometry or space</param>
        /// <param name="g2">another geometry or space</param>
        private void near(IntPtr space, IntPtr g1, IntPtr g2)
        {
            if (g1 == IntPtr.Zero || g2 == IntPtr.Zero || g1 == g2)
                return;

            // Test if we're colliding a geom with a space.
            // If so we have to drill down into the space recursively
            if (d.GeomIsSpace(g1) || d.GeomIsSpace(g2))
            {
                // Separating static prim geometry spaces.
                // We'll be calling near recursivly if one
                // of them is a space to find all of the
                // contact points in the space
                try
                {
                    d.SpaceCollide2(g1, g2, IntPtr.Zero, nearCallback);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.WarnFormat("[PHYSICS]: SpaceCollide2 failed: {0} ", e);
                    return;
                }
                return;
            }
            IntPtr b1 = d.GeomGetBody(g1);
            IntPtr b2 = d.GeomGetBody(g2);

            // Figure out how many contact points we have
            int count = 0;
            try
            {
                // Colliding Geom To Geom
                // This portion of the function 'was' blatantly ripped off from BoxStack.cs

                if (g1 == g2)
                    return; // Can't collide with yourself

                if (b1 != IntPtr.Zero && b2 != IntPtr.Zero && d.AreConnectedExcluding(b1, b2, d.JointType.Contact))
                    return;

                count = d.CollidePtr(g1, g2, (contactsPerCollision & 0xffff), ContactgeomsArray,
                                     d.ContactGeom.unmanagedSizeOf);
            }
            catch (Exception e)
            {
                MainConsole.Instance.WarnFormat("[PHYSICS]:  ode Collide failed: {0} ", e.ToString());

                PhysicsActor badObj;
                if (actor_name_map.TryGetValue(g1, out badObj))
                    if (badObj is AuroraODEPrim)
                        RemovePrim((AuroraODEPrim) badObj);
                    else if (badObj is AuroraODECharacter)
                        RemoveAvatar((AuroraODECharacter) badObj);
                if (actor_name_map.TryGetValue(g2, out badObj))
                    if (badObj is AuroraODEPrim)
                        RemovePrim((AuroraODEPrim) badObj);
                    else if (badObj is AuroraODECharacter)
                        RemoveAvatar((AuroraODECharacter) badObj);
                return;
            }

            if (count == 0)
                return;

            PhysicsActor p1;
            PhysicsActor p2;

            if (!actor_name_map.TryGetValue(g1, out p1))
                p1 = PANull;

            if (!actor_name_map.TryGetValue(g2, out p2))
                p2 = PANull;

            if (p1.CollisionScore >= float.MaxValue - count)
                p1.CollisionScore = 0;
            p1.CollisionScore += count;

            if (p2.CollisionScore >= float.MaxValue - count)
                p2.CollisionScore = 0;
            p2.CollisionScore += count;

            ContactPoint maxDepthContact = new ContactPoint();
            d.ContactGeom curContact = new d.ContactGeom();

            int NotSkipedCount = 0;

            //StatContactLoopTime = CollectTime(() =>

            #region Contact Loop

            {
                for (int i = 0; i < count; i++)
                {
                    if (!GetCurContactGeom(i, ref curContact))
                        break;

                    if (curContact.depth > maxDepthContact.PenetrationDepth)
                    {
                        maxDepthContact.PenetrationDepth = curContact.depth;
                        maxDepthContact.Position.X = curContact.pos.X;
                        maxDepthContact.Position.Y = curContact.pos.Y;
                        maxDepthContact.Position.Z = curContact.pos.Z;
                        maxDepthContact.Type = (ActorTypes) p1.PhysicsActorType;
                        maxDepthContact.SurfaceNormal.X = curContact.normal.X;
                        maxDepthContact.SurfaceNormal.Y = curContact.normal.Y;
                        maxDepthContact.SurfaceNormal.Z = curContact.normal.Z;
                    }
                }
            }
            if (p1 is AuroraODECharacter || p2 is AuroraODECharacter)
                AddODECollision(curContact, p1, p2, b1, b2, maxDepthContact, ref NotSkipedCount);
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (!GetCurContactGeom(i, ref curContact))
                        break;
                    AddODECollision(curContact, p1, p2, b1, b2, maxDepthContact, ref NotSkipedCount);
                }
            }

            #endregion//);

            //StatCollisionAccountingTime = CollectTime(() =>
            {
                if (NotSkipedCount > 0)
                {
                    if (NotSkipedCount > geomContactPointsStartthrottle)
                    {
                        // If there are more then 3 contact points, it's likely
                        // that we've got a pile of objects, so ...
                        // We don't want to send out hundreds of terse updates over and over again
                        // so lets throttle them and send them again after it's somewhat sorted out.
                        p2.ThrottleUpdates = true;
                    }
                }
                collision_accounting_events(p1, p2, maxDepthContact);
            } //);
        }

        private void AddODECollision(d.ContactGeom curContact, PhysicsActor p1, PhysicsActor p2, IntPtr b1, IntPtr b2,
                                     ContactPoint maxDepthContact, ref int NotSkipedCount)
        {
            IntPtr joint = IntPtr.Zero;

            bool p2col = true;

            // We only need to test p2 for 'jump crouch purposes'
            if (p2 is AuroraODECharacter && p1.PhysicsActorType == (int) ActorTypes.Prim)
            {
                // Testing if the collision is at the feet of the avatar
                if ((p2.Position.Z - maxDepthContact.Position.Z) < (p2.Size.Z*0.5f))
                    p2col = false;
            }

            p2.IsTruelyColliding = true;
            p2.IsColliding = p2col;

            // Logic for collision handling
            // Note, that if *all* contacts are skipped (VolumeDetect)
            // The prim still detects (and forwards) collision events but 
            // appears to be phantom for the world

            // No collision on volume detect prims
            if ((p1 is PhysicsObject && ((PhysicsObject) p1).VolumeDetect) ||
                (p2 is PhysicsObject && ((PhysicsObject) p2).VolumeDetect))
                return;

            if (curContact.depth < 0f)
                return; //Has to be penetrating

            if (m_filterCollisions &&
                checkDupe(curContact, p2.PhysicsActorType))
                return;
            if (m_filterCollisions)
                _perloopContact.Add(curContact);

            NotSkipedCount++;

            // If we're colliding against terrain
            if (p1.PhysicsActorType == (int) ActorTypes.Ground)
            {
                if (p2.PhysicsActorType == (int) ActorTypes.Prim)
                {
                    ((AuroraODEPrim) p2).GetContactParam(p2, ref newGlobalcontact);

                    joint = CreateContacJoint(curContact);
                }
                else
                {
                    newGlobalcontact = new d.Contact();
                    newGlobalcontact.surface.mode |= d.ContactFlags.SoftERP;
                    newGlobalcontact.surface.mu = 75;
                    newGlobalcontact.surface.bounce = 0.1f;
                    newGlobalcontact.surface.soft_erp = 0.05025f;
                    //GetContactParam(0.0f, AvatarContactBounce, ref newGlobalcontact);
                    joint = CreateContacJoint(curContact);
                }
                //Can't collide against anything else, agents do their own ground checks
            }
            else if ((p1.PhysicsActorType == (int) ActorTypes.Agent) &&
                     (p2.PhysicsActorType == (int) ActorTypes.Agent))
            {
                GetContactParam(0.0f, AvatarContactBounce, ref newGlobalcontact);

                joint = CreateContacJoint(curContact);
            }
            else if (p1.PhysicsActorType == (int) ActorTypes.Prim)
            {
                //Add restitution and friction changes
                ((AuroraODEPrim) p1).GetContactParam(p2, ref newGlobalcontact);

                joint = CreateContacJoint(curContact);
            }

            if (m_global_contactcount < m_currentmaxContactsbeforedeath && joint != IntPtr.Zero)
            {
                d.JointAttach(joint, b1, b2);
                m_global_contactcount++;
                joint = IntPtr.Zero;
            }
        }

        private void GetContactParam(float mu, float AvatarContactBounce, ref d.Contact newGlobalcontact)
        {
            newGlobalcontact.surface.bounce_vel = 0;
            newGlobalcontact.surface.bounce = AvatarContactBounce;
            newGlobalcontact.surface.mu = mu;
        }

        private bool checkDupe(d.ContactGeom contactGeom, int atype)
        {
            bool result = false;

            ActorTypes at = (ActorTypes) atype;
            foreach (d.ContactGeom contact in _perloopContact)
            {
                //if ((contact.g1 == contactGeom.g1 && contact.g2 == contactGeom.g2))
                //{
                // || (contact.g2 == contactGeom.g1 && contact.g1 == contactGeom.g2)
                if (at == ActorTypes.Agent)
                {
                    if (((Math.Abs(contactGeom.normal.X - contact.normal.X) < 1.026f) &&
                         (Math.Abs(contactGeom.normal.Y - contact.normal.Y) < 0.303f) &&
                         (Math.Abs(contactGeom.normal.Z - contact.normal.Z) < 0.065f)))
                    {
                        if (Math.Abs(contact.depth - contactGeom.depth) < 0.052f)
                        {
                            //contactGeom.depth *= .00005f;
                            //MainConsole.Instance.DebugFormat("[Collsion]: Depth {0}", Math.Abs(contact.depth - contactGeom.depth));
                            // MainConsole.Instance.DebugFormat("[Collision]: <{0},{1},{2}>", Math.Abs(contactGeom.normal.X - contact.normal.X), Math.Abs(contactGeom.normal.Y - contact.normal.Y), Math.Abs(contactGeom.normal.Z - contact.normal.Z));
                            result = true;
                            break;
                        }
                    }
                }
                else if (at == ActorTypes.Prim)
                {
                    //d.AABB aabb1 = new d.AABB();
                    //d.AABB aabb2 = new d.AABB();

                    //d.GeomGetAABB(contactGeom.g2, out aabb2);
                    //d.GeomGetAABB(contactGeom.g1, out aabb1);
                    //aabb1.
                    if (((Math.Abs(contactGeom.normal.X - contact.normal.X) < 1.026f) &&
                         (Math.Abs(contactGeom.normal.Y - contact.normal.Y) < 0.303f) &&
                         (Math.Abs(contactGeom.normal.Z - contact.normal.Z) < 0.065f)))
                    {
                        if (contactGeom.normal.X == contact.normal.X && contactGeom.normal.Y == contact.normal.Y &&
                            contactGeom.normal.Z == contact.normal.Z)
                        {
                            if (Math.Abs(contact.depth - contactGeom.depth) < 0.272f)
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void collision_accounting_events(PhysicsActor p1, PhysicsActor p2, ContactPoint contact)
        {
            if (!p2.SubscribedEvents() && !p1.SubscribedEvents())
                return;
            if (p1.SubscribedEvents())
                p1.AddCollisionEvent(p2.LocalID, contact);
            if (p2.SubscribedEvents())
                p2.AddCollisionEvent(p1.LocalID, contact);
        }

        /// <summary>
        ///     This is our collision testing routine in ODE
        /// </summary>
        /// <param name="timeStep"></param>
        private void collision_optimized(float timeStep)
        {
            m_global_contactcount = 0;
            //Clear out all the colliding attributes before we begin to collide anyone
            foreach (AuroraODECharacter chr in _characters)
            {
                chr.IsColliding = false;
                chr.IsTruelyColliding = false;
            }
            foreach (
                AuroraODECharacter chr in
                    _characters.Where(chr => chr != null && chr.Shell != IntPtr.Zero && chr.Body != IntPtr.Zero))
            {
                // test the avatar's geometry for collision with the space
                // This will return near and the space that they are the closest to
                // And we'll run this again against the avatar and the space segment
                // This will return with a bunch of possible objects in the space segment
                // and we'll run it again on all of them.
                try
                {
                    d.SpaceCollide2(space, chr.Shell, IntPtr.Zero, nearCallback);
                }
                catch (AccessViolationException)
                {
                    MainConsole.Instance.Warn("[PHYSICS]: Unable to space collide");
                }
            }

            lock (_activeprimsLock)
            {
                List<AuroraODEPrim> removeprims = null;
                foreach (AuroraODEPrim chr in _activeprims)
                {
                    //Fix colliding atributes!
                    chr.IsColliding = false;
                    chr.LinkSetIsColliding = false;
                }
                foreach (AuroraODEPrim prm in _activeprims)
                {
                    if (prm.Body != IntPtr.Zero && d.BodyIsEnabled(prm.Body) &&
                        (!prm.m_disabled) && (!prm.m_frozen))
                    {
                        try
                        {
                            lock (prm)
                            {
                                if (space != IntPtr.Zero && prm.prim_geom != IntPtr.Zero)
                                {
                                    d.SpaceCollide2(space, prm.prim_geom, IntPtr.Zero, nearCallback);
                                }
                                else
                                {
                                    if (removeprims == null)
                                        removeprims = new List<AuroraODEPrim>();
                                    removeprims.Add(prm);
                                    MainConsole.Instance.Debug(
                                        "[PHYSICS]: unable to collide test active prim against space.  The space was zero, the geom was zero or it was in the process of being removed.  Removed it from the active prim list.  This needs to be fixed!");
                                }
                            }
                        }
                        catch (AccessViolationException)
                        {
                            MainConsole.Instance.Warn("[PHYSICS]: Unable to space collide");
                        }
                    }
                    else if (prm.m_frozen)
                    {
                        if (removeprims == null)
                            removeprims = new List<AuroraODEPrim>();
                        removeprims.Add(prm);
                    }
                }
                if (removeprims != null)
                {
                    foreach (AuroraODEPrim chr in removeprims)
                        _activeprims.Remove(chr);
                }
            }

            if (m_filterCollisions)
                _perloopContact.Clear();
        }

        public float GetTerrainHeightAtXY(float x, float y)
        {
            // warning this code assumes terrain grid as 1m size
            if (TerrainHeightFieldHeights == null)
                return 0;

            if (x < 0)
                x = 0;
            if (y < 0)
                y = 0;

            int ix;
            int iy;
            float dx;
            float dy;

            if (x < m_region.RegionSizeX - 1)
            {
                ix = (int) x;
                dx = x - ix;
            }
            else
            {
                ix = m_region.RegionSizeX - 1;
                dx = 0;
            }
            if (y < m_region.RegionSizeY - 1)
            {
                iy = (int) y;
                dy = y - iy;
            }
            else
            {
                iy = m_region.RegionSizeY - 1;
                dy = 0;
            }

            float h0;
            float h1;
            float h2;

            float invterrainscale = 1.0f/Constants.TerrainCompression;

            iy *= m_region.RegionSizeX;

            if ((dx + dy) <= 1.0f)
            {
                h0 = (TerrainHeightFieldHeights[iy + ix])*invterrainscale;

                if (dx > 0)
                    h1 = ((TerrainHeightFieldHeights[iy + ix + 1])*invterrainscale - h0)*dx;
                else
                    h1 = 0;

                if (dy > 0)
                    h2 = ((TerrainHeightFieldHeights[iy + m_region.RegionSizeX + ix])*invterrainscale - h0)*dy;
                else
                    h2 = 0;

                return h0 + h1 + h2;
            }
            else
            {
                h0 = (TerrainHeightFieldHeights[iy + m_region.RegionSizeX + ix + 1])*invterrainscale;

                if (dx > 0)
                    h1 = ((TerrainHeightFieldHeights[iy + ix + 1])*invterrainscale - h0)*(1 - dy);
                else
                    h1 = 0;

                if (dy > 0)
                    h2 = ((TerrainHeightFieldHeights[iy + m_region.RegionSizeX + ix])*invterrainscale - h0)*(1 - dx);
                else
                    h2 = 0;

                return h0 + h1 + h2;
            }
        }

        public void addCollisionEventReporting(PhysicsActor obj)
        {
            if (IsLocked)
                ActiveAddCollisionQueue.Add(obj);
            else
            {
                lock (_collisionEventListLock)
                {
                    if (!_collisionEventDictionary.ContainsKey(obj.UUID))
                        _collisionEventDictionary.Add(obj.UUID, obj);
                }
            }
        }

        public void remCollisionEventReporting(PhysicsActor obj)
        {
            if (IsLocked)
                ActiveRemoveCollisionQueue.Add(obj);
            else
            {
                lock (_collisionEventListLock)
                    _collisionEventDictionary.Remove(obj.UUID);
            }
        }

        #endregion

        #region Add/Remove Entities

        public override PhysicsCharacter AddAvatar(string avName, Vector3 position, Quaternion rotation, Vector3 size,
                                                   bool isFlying, uint localID, UUID UUID)
        {
            Vector3 pos;
            pos.X = position.X;
            pos.Y = position.Y;
            pos.Z = position.Z;
            ODESpecificAvatar newAv = new ODESpecificAvatar(avName, this, pos, rotation, size)
                                          {
                                              LocalID = localID,
                                              UUID = UUID,
                                              Flying = isFlying,
                                              MinimumGroundFlightOffset = minimumGroundFlightOffset
                                          };

            return newAv;
        }

        /// <summary>
        ///     Adds a character to the list of avatars in the scene
        ///     Internally locked, as it is called only in the Simulation Changes loop
        /// </summary>
        /// <param name="chr"></param>
        internal void AddCharacter(AuroraODECharacter chr)
        {
            if (!_characters.Contains(chr))
                _characters.Add(chr);
        }

        /// <summary>
        ///     Removes a character from the list of avatars currently in the scene
        ///     Internally locked, as it is called only in the Simulation Changes loop
        /// </summary>
        /// <param name="chr"></param>
        internal void RemoveCharacter(AuroraODECharacter chr)
        {
            _characters.Remove(chr);
        }

        public override void RemoveAvatar(PhysicsCharacter actor)
        {
            //MainConsole.Instance.Debug("[PHYSICS]:ODELOCK");
            ((AuroraODECharacter) actor).Destroy();
        }

        internal void BadCharacter(AuroraODECharacter chr)
        {
            RemoveAvatar(chr);
            AddAvatar(chr.Name, new Vector3(m_region.RegionSizeX/2,
                                            m_region.RegionSizeY/2,
                                            m_region.RegionSizeZ/2), chr.Orientation,
                      new Vector3(chr.CAPSULE_RADIUS*2, chr.CAPSULE_RADIUS*2,
                                  chr.CAPSULE_LENGTH*2), true, chr.LocalID, chr.UUID);
        }

        internal void BadPrim(AuroraODEPrim auroraODEPrim)
        {
            DeletePrim(auroraODEPrim);
            //Can't really do this here... as it will be readded before the delete gets called, which is wrong...
            //So... leave the prim out there for now
            //AddPrimShape(auroraODEPrim.ParentEntity);
        }

        public override PhysicsObject AddPrimShape(ISceneChildEntity entity)
        {
            bool isPhysical = ((entity.ParentEntity.RootChild.Flags & PrimFlags.Physics) != 0);
            bool isPhantom = ((entity.ParentEntity.RootChild.Flags & PrimFlags.Phantom) != 0);
            bool physical = isPhysical & !isPhantom;
            AuroraODEPrim newPrim = new AuroraODEPrim(entity, this, false);

            if (physical)
                newPrim.IsPhysical = physical;

            lock (_prims)
                _prims.Add(newPrim);

            return newPrim;
        }

        internal void addActivePrim(AuroraODEPrim activatePrim)
        {
            // adds active prim..   (ones that should be iterated over in collisions_optimized
            lock (_activeprimsLock)
            {
                if (!_activeprims.Contains(activatePrim))
                    _activeprims.Add(activatePrim);
            }
        }

        public override float TimeDilation
        {
            get { return m_timeDilation; }
            set { m_timeDilation = value; }
        }

        internal void remActivePrim(AuroraODEPrim deactivatePrim)
        {
            lock (_activeprimsLock)
                _activeprims.Remove(deactivatePrim);
        }

        public override void RemovePrim(PhysicsObject prim)
        {
            //Add the prim to a queue which will be removed when Simulate has finished what it's doing.
            RemoveQueue.Enqueue(prim);
        }

        public override void DeletePrim(PhysicsObject prim)
        {
            //Add the prim to a queue which will be removed when Simulate has finished what it's doing.
            DeleteQueue.Enqueue(prim);
        }

        /// <summary>
        ///     This is called from within simulate but outside the locked portion
        ///     We need to do our own locking here
        ///     Essentially, we need to remove the prim from our space segment, whatever segment it's in.
        ///     If there are no more prim in the segment, we need to empty (spacedestroy)the segment and reclaim memory
        ///     that the space was using.
        /// </summary>
        /// <param name="prim"></param>
        internal void RemovePrimThreadLocked(AuroraODEPrim prim)
        {
            remCollisionEventReporting(prim);
            remActivePrim(prim);
            prim.m_frozen = true;
            if (prim.prim_geom != IntPtr.Zero)
            {
                prim.DestroyBody();
                prim.IsPhysical = false;
                prim.m_targetSpace = IntPtr.Zero;
                try
                {
                    if (prim.prim_geom != IntPtr.Zero)
                    {
                        d.GeomDestroy(prim.prim_geom);
                        prim.prim_geom = IntPtr.Zero;
                    }
                    else
                    {
                        MainConsole.Instance.Warn("[PHYSICS]: Unable to remove prim from physics scene");
                    }
                }
                catch (AccessViolationException)
                {
                    MainConsole.Instance.Info(
                        "[PHYSICS]: Couldn't remove prim from physics scene, it was already be removed.");
                }
            }
            if (!prim.childPrim)
            {
                lock (prim.childrenPrim)
                    foreach (AuroraODEPrim prm in prim.childrenPrim)
                        RemovePrimThreadLocked(prm);
            }
            if (prim.ParentEntity != null)
                prim.ParentEntity.PhysActor = null; //Delete it
            lock (_prims)
                _prims.Remove(prim);
        }

        #endregion

        #region Space Separation Calculation

        /// <summary>
        ///     Called when a static prim moves.  Allocates a space for the prim based on its position
        /// </summary>
        /// <param name="geom">The pointer to the geom that moved</param>
        /// <param name="pos">The position that the geom moved to</param>
        /// <param name="currentspace">A pointer to the space it was in before it was moved.</param>
        /// <returns>A pointer to the new space it's in</returns>
        public IntPtr recalculateSpaceForGeom(IntPtr geom, Vector3 pos, IntPtr currentspace)
        {
            // Called from setting the Position and Size of an ODEPrim so
            // it's already in locked space.

            // we don't want to remove the main space
            // we don't need to test physical here because this function should
            // never be called if the prim is physical(active)

            if (currentspace != space)
            {
                if (d.SpaceQuery(currentspace, geom) && currentspace != IntPtr.Zero)
                {
                    if (d.GeomIsSpace(currentspace))
                        d.SpaceRemove(currentspace, geom);
                    else
                    {
                        MainConsole.Instance.Info("[Physics]: Invalid Scene passed to 'recalculatespace':" +
                                                  currentspace +
                                                  " Geom:" + geom);
                    }
                }
                else
                {
                    IntPtr sGeomIsIn = d.GeomGetSpace(geom);
                    if (sGeomIsIn != IntPtr.Zero)
                    {
                        if (d.GeomIsSpace(currentspace))
                            d.SpaceRemove(sGeomIsIn, geom);
                        else
                        {
                            MainConsole.Instance.Info("[Physics]: Invalid Scene passed to 'recalculatespace':" +
                                                      sGeomIsIn + " Geom:" + geom);
                        }
                    }
                }
/* don't delete spaces
                //If there are no more geometries in the sub-space, we don't need it in the main space anymore
                if (d.SpaceGetNumGeoms(currentspace) == 0)
                {
                    if (currentspace != IntPtr.Zero)
                    {
                        if (d.GeomIsSpace(currentspace))
                        {
                            waitForSpaceUnlock(currentspace);
                            waitForSpaceUnlock(space);
                            d.SpaceRemove(space, currentspace);
                            // free up memory used by the space.

                            //d.SpaceDestroy(currentspace);
                            resetSpaceArrayItemToZero(currentspace);
                        }
                        else
                        {
                            MainConsole.Instance.Info("[Physics]: Invalid Scene passed to 'recalculatespace':" +
                                       currentspace + " Geom:" + geom);
                        }
                    }
                }
 */
            }
            else
            {
                // this is a physical object that got disabled. ;.;
                if (currentspace != IntPtr.Zero && geom != IntPtr.Zero)
                {
                    if (d.SpaceQuery(currentspace, geom))
                    {
                        if (d.GeomIsSpace(currentspace))
                            d.SpaceRemove(currentspace, geom);
                        else
                        {
                            MainConsole.Instance.Info("[Physics]: Invalid Scene passed to 'recalculatespace':" +
                                                      currentspace + " Geom:" + geom);
                        }
                    }
                    else
                    {
                        IntPtr sGeomIsIn = d.GeomGetSpace(geom);
                        if (sGeomIsIn != IntPtr.Zero)
                        {
                            if (d.GeomIsSpace(sGeomIsIn))
                                d.SpaceRemove(sGeomIsIn, geom);
                            else
                            {
                                MainConsole.Instance.Info("[Physics]: Invalid Scene passed to 'recalculatespace':" +
                                                          sGeomIsIn + " Geom:" + geom);
                            }
                        }
                    }
                }
            }

            // The routines in the Position and Size sections do the 'inserting' into the space,
            // so all we have to do is make sure that the space that we're putting the prim into
            // is in the 'main' space.
//            int[] iprimspaceArrItem = calculateSpaceArrayItemFromPos(pos);
            IntPtr newspace = calculateSpaceForGeom(pos);

/*  spaces aren't deleted so already created
            if (newspace == IntPtr.Zero)
            {
                newspace = createprimspace(iprimspaceArrItem[0], iprimspaceArrItem[1]);
                d.HashSpaceSetLevels(newspace, HashspaceLow, HashspaceHigh);
            }
*/
            return newspace;
        }

        /// <summary>
        ///     Calculates the space the prim should be in by its position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>A pointer to the space. This could be a new space or reused space.</returns>
        public IntPtr calculateSpaceForGeom(Vector3 pos)
        {
            int[] xyspace = calculateSpaceArrayItemFromPos(pos);
            //MainConsole.Instance.Info("[Physics]: Attempting to use arrayItem: " + xyspace[0].ToString() + "," + xyspace[1].ToString());
            return staticPrimspace[xyspace[0], xyspace[1]];
        }

        /// <summary>
        ///     Holds the space allocation logic
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>an array item based on the position</returns>
        public int[] calculateSpaceArrayItemFromPos(Vector3 pos)
        {
            int[] returnint = new int[2];

            returnint[0] = (int) (pos.X) >> GridSpaceScaleBits;

            if (returnint[0] >= nspacesPerSideX)
                returnint[0] = nspacesPerSideX - 1;
            if (returnint[0] < 0)
                returnint[0] = 0;

            returnint[1] = (int) (pos.Y) >> GridSpaceScaleBits;
            if (returnint[1] >= nspacesPerSideY)
                returnint[1] = nspacesPerSideY - 1;
            if (returnint[1] < 0)
                returnint[1] = 0;

            return returnint;
        }

        /// <summary>
        ///     Debug space message for printing the space that a prim/avatar is in.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>Returns which split up space the given position is in.</returns>
        public string whichspaceamIin(Vector3 pos)
        {
            return calculateSpaceForGeom(pos).ToString();
        }

        #endregion

        #region Meshing

        /// <summary>
        ///     Routine to figure out if we need to mesh this prim with our mesher
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal bool needsMeshing(ISceneChildEntity entity)
        {
            PrimitiveBaseShape pbs = entity.Shape;
            // most of this is redundant now as the mesher will return null if it cant mesh a prim
            // but we still need to check for sculptie meshing being enabled so this is the most
            // convenient place to do it for now...

            //    //if (pbs.PathCurve == (byte)Primitive.PathCurve.Circle && pbs.ProfileCurve == (byte)Primitive.ProfileCurve.Circle && pbs.PathScaleY <= 0.75f)
            //    //MainConsole.Instance.Debug("needsMeshing: " + " pathCurve: " + pbs.PathCurve.ToString() + " profileCurve: " + pbs.ProfileCurve.ToString() + " pathScaleY: " + Primitive.UnpackPathScale(pbs.PathScaleY).ToString());
            int iPropertiesNotSupportedDefault = 0;

//            return true;

            if (forceSimplePrimMeshing)
                return true;
            // let simple spheres use ode sphere object
            PrimitiveBaseShape sphere = PrimitiveBaseShape.CreateSphere();
            if (pbs.ProfileShape == ProfileShape.HalfCircle && pbs.PathCurve == (byte) Extrusion.Curve1
                && pbs.Scale.X == pbs.Scale.Y && pbs.Scale.X == pbs.Scale.Z && pbs.ProfileHollow == sphere.ProfileHollow &&
                pbs.PathBegin == sphere.PathBegin && pbs.PathEnd == sphere.PathEnd &&
                pbs.PathCurve == sphere.PathCurve && pbs.HollowShape == sphere.HollowShape &&
                pbs.PathRadiusOffset == sphere.PathRadiusOffset && pbs.PathRevolutions == sphere.PathRevolutions &&
                pbs.PathScaleY == sphere.PathScaleY && pbs.PathShearX == sphere.PathShearX &&
                pbs.PathShearY == sphere.PathShearY && pbs.PathSkew == sphere.PathSkew &&
                pbs.PathTaperY == sphere.PathTaperY && pbs.PathTwist == sphere.PathTwist &&
                pbs.PathTwistBegin == sphere.PathTwistBegin && pbs.ProfileBegin == sphere.ProfileBegin &&
                pbs.ProfileEnd == sphere.ProfileEnd && pbs.ProfileHollow == sphere.ProfileHollow &&
                pbs.ProfileShape == sphere.ProfileShape)
                return false;

            if (pbs.SculptEntry && !meshSculptedPrim)
                return false;
            else if (pbs.SculptType != (byte) SculptType.Mesh &&
                     pbs.SculptType != (byte) SculptType.None)
                return true; //Sculpty, mesh it
            else if (pbs.SculptType == (byte) SculptType.Mesh)
            {
                //Mesh, we need to see what the prims says to do with it
                if (entity.PhysicsType == (byte) PhysicsShapeType.Prim)
                    return false; //Supposed to be a simple box, nothing more
                else
                    return true; //Mesh it!
            }

            // if it's a standard box or sphere with no cuts, hollows, twist or top shear, return false since ODE can use an internal representation for the prim
            if (!forceSimplePrimMeshing)
            {
                if ((pbs.ProfileShape == ProfileShape.Square && pbs.PathCurve == (byte) Extrusion.Straight)
                    /*|| (pbs.ProfileShape == ProfileShape.HalfCircle && pbs.PathCurve == (byte)Extrusion.Curve1
                    && pbs.Scale.X == pbs.Scale.Y && pbs.Scale.Y == pbs.Scale.Z)*/)
                {
                    if (pbs.ProfileBegin == 0 && pbs.ProfileEnd == 0
                        && pbs.ProfileHollow == 0
                        && pbs.PathTwist == 0 && pbs.PathTwistBegin == 0
                        && pbs.PathBegin == 0 && pbs.PathEnd == 0
                        && pbs.PathTaperX == 0 && pbs.PathTaperY == 0
                        && pbs.PathScaleX == 100 && pbs.PathScaleY == 100
                        && pbs.PathShearX == 0 && pbs.PathShearY == 0)
                    {
#if SPAM
                    MainConsole.Instance.Warn("NonMesh");
#endif
                        return false;
                    }
                }
            }

            if (pbs.ProfileHollow != 0)
                iPropertiesNotSupportedDefault++;
            else if ((pbs.PathTwistBegin != 0) || (pbs.PathTwist != 0))
                iPropertiesNotSupportedDefault++;
            else if ((pbs.ProfileBegin != 0) || pbs.ProfileEnd != 0)
                iPropertiesNotSupportedDefault++;
            else if (pbs.PathBegin != 0 || pbs.PathEnd != 0)
                iPropertiesNotSupportedDefault++;
            else if ((pbs.PathScaleX != 100) || (pbs.PathScaleY != 100))
                iPropertiesNotSupportedDefault++;
            else if ((pbs.PathShearX != 0) || (pbs.PathShearY != 0))
                iPropertiesNotSupportedDefault++;
            else if (pbs.ProfileShape == ProfileShape.Circle && pbs.PathCurve == (byte) Extrusion.Straight)
                iPropertiesNotSupportedDefault++;
            else if (pbs.ProfileShape == ProfileShape.HalfCircle && pbs.PathCurve == (byte) Extrusion.Curve1 &&
                     (pbs.Scale.X != pbs.Scale.Y || pbs.Scale.Y != pbs.Scale.Z || pbs.Scale.Z != pbs.Scale.X))
                iPropertiesNotSupportedDefault++;
            else if (pbs.ProfileShape == ProfileShape.HalfCircle && pbs.PathCurve == (byte) Extrusion.Curve1)
                iPropertiesNotSupportedDefault++;
                // test for torus
            else if ((pbs.ProfileCurve & 0x07) == (byte) ProfileShape.Square &&
                     pbs.PathCurve == (byte) Extrusion.Curve1)
                iPropertiesNotSupportedDefault++;
            else if ((pbs.ProfileCurve & 0x07) == (byte) ProfileShape.HalfCircle &&
                     (pbs.PathCurve == (byte) Extrusion.Curve1 || pbs.PathCurve == (byte) Extrusion.Curve2))
                iPropertiesNotSupportedDefault++;
            else if ((pbs.ProfileCurve & 0x07) == (byte) ProfileShape.EquilateralTriangle)
            {
                if (pbs.PathCurve == (byte) Extrusion.Straight)
                {
                    iPropertiesNotSupportedDefault++;
                }
                else if (pbs.PathCurve == (byte) Extrusion.Curve1)
                {
                    iPropertiesNotSupportedDefault++;
                }
            }
            if ((pbs.ProfileCurve & 0x07) == (byte) ProfileShape.Circle)
            {
                if (pbs.PathCurve == (byte) Extrusion.Straight)
                {
                    iPropertiesNotSupportedDefault++;
                }

                    // ProfileCurve seems to combine hole shape and profile curve so we need to only compare against the lower 3 bits
                else if (pbs.PathCurve == (byte) Extrusion.Curve1)
                {
                    iPropertiesNotSupportedDefault++;
                }
            }


            if (iPropertiesNotSupportedDefault == 0)
            {
#if SPAM
                MainConsole.Instance.Warn("NonMesh");
#endif
                return false;
            }
#if SPAM
            MainConsole.Instance.Debug("Mesh");
#endif
            return true;
        }

        #endregion

        #region Changes/Tainting

        /// <summary>
        ///     Called to queue a change to a prim
        ///     to use in place of old taint mechanism so changes do have a time sequence
        /// </summary>
        public void AddSimulationChange(NoParam del)
        {
            SimulationChangesQueue.Enqueue(del);
        }

        #endregion

        #region Simulation Loop

        /// <summary>
        ///     This is our main simulate loop
        ///     It's thread locked by a Mutex in the scene.
        ///     It holds Collisions, it instructs ODE to step through the physical reactions
        ///     It moves the objects around in memory
        ///     It calls the methods that report back to the object owners.. (scenepresence, SceneObjectGroup)
        /// </summary>
        /// <param name="timeElapsed"></param>
        /// <returns></returns>
        public override void Simulate(float timeElapsed)
        {
            if (framecount >= int.MaxValue)
                framecount = 0;

            framecount++;
            step_time += timeElapsed;
            IsLocked = true;
            int nodesteps = 0;

            if (step_time > 0.5f)
                step_time = 0.5f; //Don't get ODE stuck in an eternal processing loop with huge step times

            while (step_time > 0.0f && nodesteps < 10)
            {
                try
                {
                    NoParam del;
                    while (SimulationChangesQueue.TryDequeue(out del) && m_scene.ShouldRunHeartbeat)
                        try
                        {
                            del();
                        }
                        catch
                        {
                        }

                    if (SimulationChangesQueue.Count == 0 && !m_hasSetUpPrims)
                    {
                        //Tell the mesher that we are done with the initialization 
                        //  of prim meshes and that it can clear it's in memory cache
                        m_hasSetUpPrims = true;
                        mesher.FinishedMeshing();
                    }
                    else if (!m_hasSetUpPrims)
                        return; //Don't do physics until the sim is completely set up

                    // Move characters
                    foreach (
                        ODESpecificAvatar actor in _characters.Where(actor => actor != null).Cast<ODESpecificAvatar>())
                        actor.Move(ODE_STEPSIZE);

                    // Move other active objects
                    lock (_activeprimsLock)
                        foreach (AuroraODEPrim prim in _activeprims)
                            prim.Move(ODE_STEPSIZE);

                    if (m_rayCastManager != null)
                        m_rayCastManager.ProcessQueuedRequests();

                    if (!DisableCollisions)
                        collision_optimized(timeElapsed);

                    d.WorldQuickStep(world, ODE_STEPSIZE);
                    d.JointGroupEmpty(contactgroup);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("[PHYSICS]: {0}, {1}, {2}", e, e.TargetSite, e);
                }

                step_time -= ODE_STEPSIZE;
                nodesteps++;
            }

            IsLocked = false;

            PhysicsObject prm;
            while (RemoveQueue.TryDequeue(out prm))
            {
                AuroraODEPrim p = (AuroraODEPrim) prm;
                p.setPrimForRemoval();
            }
            while (DeleteQueue.TryDequeue(out prm))
            {
                AuroraODEPrim p = (AuroraODEPrim) prm;
                p.setPrimForDeletion();
            }

            if (ActiveAddCollisionQueue.Count > 0)
            {
                lock (_collisionEventListLock)
                {
                    foreach (
                        PhysicsActor obj in
                            ActiveAddCollisionQueue.Where(obj => !_collisionEventDictionary.ContainsKey(obj.UUID)))
                        _collisionEventDictionary.Add(obj.UUID, obj);
                }
                ActiveAddCollisionQueue.Clear();
            }
            if (ActiveRemoveCollisionQueue.Count > 0)
            {
                lock (_collisionEventListLock)
                {
                    foreach (PhysicsActor obj in ActiveRemoveCollisionQueue)
                        _collisionEventDictionary.Remove(obj.UUID);
                }
                ActiveRemoveCollisionQueue.Clear();
            }

            if (!DisableCollisions)
            {
                foreach (AuroraODECharacter av in _characters.Where(av => av != null))
                    av.SendCollisions();
                lock (_collisionEventListLock)
                {
                    foreach (PhysicsActor obj in _collisionEventDictionary.Values.Where(obj => obj != null))
                        obj.SendCollisions();
                }
            }

            foreach (AuroraODECharacter actor in _characters.Where(actor => actor != null))
                actor.UpdatePositionAndVelocity(nodesteps*ODE_STEPSIZE);

            lock (_activeprimsLock)
            {
                foreach (AuroraODEPrim actor in _activeprims.Where(actor => actor.IsPhysical))
                    actor.UpdatePositionAndVelocity(nodesteps*ODE_STEPSIZE);
            }
        }

        private int CollectTime(NoParam del)
        {
            int time = Util.EnvironmentTickCount();
            del();
            return Util.EnvironmentTickCountSubtract(time);
        }

        #endregion

        #region Get/Set Terrain and water

        public override void SetTerrain(ITerrainChannel channel, short[] heightMap)
        {
            m_channel = channel;
            float[] _heightmap = ODETerrainHeightFieldHeights;
            if (ODETerrainHeightFieldHeights == null)
                _heightmap = new float[m_region.RegionSizeX*m_region.RegionSizeY];

            for (int x = 0; x < m_region.RegionSizeX; x++)
            {
                for (int y = 0; y < m_region.RegionSizeY; y++)
                {
                    _heightmap[(x*m_region.RegionSizeX) + y] = heightMap[y*m_region.RegionSizeX + x]/
                                                               Constants.TerrainCompression;
                }
            }

            float hfmin = _heightmap.Min();
            float hfmax = _heightmap.Max();

            SimulationChangesQueue.Enqueue(() =>
                                               {
                                                   if (RegionTerrain != IntPtr.Zero)
                                                   {
                                                       d.GeomHeightfieldDataDestroy(RegionTerrain);
                                                       d.SpaceRemove(space, RegionTerrain);
                                                       //d.GeomDestroy(RegionTerrain);
                                                       GC.RemoveMemoryPressure(_heightmap.Length);
                                                   }

                                                   const float scale = 1f;
                                                   const float offset = 0.0f;
                                                   float thickness = 0.2f;
                                                   const int wrap = 0;

                                                   IntPtr HeightmapData = d.GeomHeightfieldDataCreate();
                                                   GC.AddMemoryPressure(_heightmap.Length);
                                                   //Add the memory pressure properly (note: should we be doing this since we have it in managed memory?)
                                                   //Do NOT copy it! Otherwise, it'll copy the terrain into unmanaged memory where we can't release it each time
                                                   d.GeomHeightfieldDataBuildSingle(HeightmapData, _heightmap, 0,
                                                                                    m_region.RegionSizeX,
                                                                                    m_region.RegionSizeY,
                                                                                    m_region.RegionSizeX,
                                                                                    m_region.RegionSizeY, scale,
                                                                                    offset, thickness, wrap);

                                                   d.GeomHeightfieldDataSetBounds(HeightmapData, hfmin - 1.0f,
                                                                                  hfmax + 1.0f);
                                                   RegionTerrain = d.CreateHeightfield(space, HeightmapData, 1);

                                                   d.GeomSetCategoryBits(RegionTerrain, (int) (CollisionCategories.Land));
                                                   d.GeomSetCollideBits(RegionTerrain, (int) (CollisionCategories.Space));

                                                   actor_name_map[RegionTerrain] = new NullObjectPhysicsActor();

                                                   d.Matrix3 R = new d.Matrix3();

                                                   Quaternion q1 = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0),
                                                                                                  1.5707f);
                                                   Quaternion q2 = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0),
                                                                                                  1.5707f);

                                                   q1 = q1*q2;

                                                   Vector3 v3;
                                                   float angle;
                                                   q1.GetAxisAngle(out v3, out angle);

                                                   d.RFromAxisAndAngle(out R, v3.X, v3.Y, v3.Z, angle);

                                                   d.GeomSetRotation(RegionTerrain, ref R);
                                                   d.GeomSetPosition(RegionTerrain, (m_region.RegionSizeX*0.5f),
                                                                     (m_region.RegionSizeY*0.5f), 0);

                                                   TerrainHeightFieldHeights = heightMap;
                                                   ODETerrainHeightFieldHeights = _heightmap;
                                               });
        }

        public double GetWaterLevel(float x, float y)
        {
            return WaterHeight;
        }

        public override void SetWaterLevel(double height, short[] map)
        {
            WaterHeight = height;
        }

        #endregion

        #region Dispose

        public override void Dispose()
        {
            lock (_prims)
            {
                foreach (AuroraODEPrim prm in _prims)
                {
                    RemovePrim(prm);
                }
            }

            //foreach (OdeCharacter act in _characters)
            //{
            //RemoveAvatar(act);
            //}

            if (ContactgeomsArray != IntPtr.Zero)
                Marshal.FreeHGlobal(ContactgeomsArray);
            if (GlobalContactsArray != IntPtr.Zero)
                Marshal.FreeHGlobal(GlobalContactsArray);

            d.WorldDestroy(world);
            //d.CloseODE();
            m_rayCastManager.Dispose();
            m_rayCastManager = null;
        }

        #endregion

        #region Top colliders

        public override Dictionary<uint, float> GetTopColliders()
        {
            Dictionary<uint, float> returncolliders = new Dictionary<uint, float>();
            List<AuroraODEPrim> collidingPrims = new List<AuroraODEPrim>();
            lock (_prims)
            {
#if (!ISWIN)
                foreach (AuroraODEPrim prm in _prims)
                {
                    if (prm.CollisionScore > 0)
                    {
                        if (!collidingPrims.Contains(prm))
                        {
                            collidingPrims.Add(prm);
                        }
                    }
                }
#else
                foreach (
                    AuroraODEPrim prm in
                        _prims.Where(prm => prm.CollisionScore > 0).Where(prm => !collidingPrims.Contains(prm)))
                {
                    collidingPrims.Add(prm);
                }
#endif
            }
            //Sort them by their score
#if (!ISWIN)
            collidingPrims.Sort(delegate(AuroraODEPrim a, AuroraODEPrim b)
            {
                return b.CollisionScore.CompareTo(a.CollisionScore);
            });
#else
            collidingPrims.Sort((a, b) => b.CollisionScore.CompareTo(a.CollisionScore));
#endif
            //Limit to 25
            if (collidingPrims.Count > 25)
                collidingPrims.RemoveRange(25, collidingPrims.Count - 25);

            foreach (AuroraODEPrim prm in collidingPrims)
            {
                returncolliders[prm.LocalID] = prm.CollisionScore;
                prm.resetCollisionAccounting();
            }
            return returncolliders;
        }

        #endregion

        #region Raycasting

        public override bool SupportsRayCast()
        {
            return true;
        }

        public override void RaycastWorld(Vector3 position, Vector3 direction, float length, RaycastCallback retMethod)
        {
            if (retMethod != null)
            {
                m_rayCastManager.QueueRequest(position, direction, length, retMethod);
            }
        }

        public override void RaycastWorld(Vector3 position, Vector3 direction, float length, int Count,
                                          RayCallback retMethod)
        {
            if (retMethod != null)
            {
                m_rayCastManager.QueueRequest(position, direction, length, Count, retMethod);
            }
        }

        public override List<ContactResult> RaycastWorld(Vector3 position, Vector3 direction, float length, int Count)
        {
            ContactResult[] ourResults = null;
            RayCallback retMethod = delegate(List<ContactResult> results)
                                        {
                                            ourResults = new ContactResult[results.Count];
                                            results.CopyTo(ourResults, 0);
                                        };
            int waitTime = 0;
            m_rayCastManager.QueueRequest(position, direction, length, Count, retMethod);
            while (ourResults == null && waitTime < 1000)
            {
                Thread.Sleep(1);
                waitTime++;
            }
            if (ourResults == null)
                return new List<ContactResult>();
            return new List<ContactResult>(ourResults);
        }

        #endregion

        #region Gravity Calculation

        #region Structs

        private struct PointGravity
        {
            public float ForceX;
            public float ForceY;
            public float ForceZ;
            public float GravForce;

            /// <summary>
            ///     If this is true, the actor will have the forces applied to them
            ///     once they enter the area, rather than having gravity act like it does
            ///     in real life (pulling toward the center)
            /// </summary>
            public bool PointForce;

            public Vector3 Position;
            public float Radius;
        }

        #endregion

        private bool normalGravityEnabled = true;
        private readonly Dictionary<int, PointGravity> m_pointGravityPositions = new Dictionary<int, PointGravity>();
        private bool pointGravityInUse;

        public void CalculateGravity(float mass, Vector3 position, bool allowNormalGravity, float gravityModifier,
                                     ref Vector3 forceVector)
        {
            if (normalGravityEnabled && allowNormalGravity)
            {
                //normal gravity, one axis, no center
                forceVector.X += gravityx*mass*gravityModifier;
                forceVector.Y += gravityy*mass*gravityModifier;
                forceVector.Z += gravityz*mass*gravityModifier;
            }
            if (pointGravityInUse)
            {
                //Find the nearby centers of gravity
                foreach (PointGravity pg in m_pointGravityPositions.Values)
                {
                    float distance = Vector3.DistanceSquared(pg.Position, position);
                    if (distance < pg.Radius*pg.Radius)
                    {
                        float d = (distance/(pg.Radius*pg.Radius));
                        float radiusScaling = 1 - d;
                        radiusScaling *= radiusScaling;
                        if (pg.PointForce)
                        {
                            //Applies forces to the actor when in range
                            forceVector.X += pg.ForceX*radiusScaling*mass*gravityModifier;
                            forceVector.Y += pg.ForceY*radiusScaling*mass*gravityModifier;
                            forceVector.Z += pg.ForceZ*radiusScaling*mass*gravityModifier;
                        }
                        else
                        {
                            //Pulls the actor toward the point
                            forceVector.X += (pg.Position.X - position.X)*pg.GravForce*radiusScaling*mass*
                                             gravityModifier;
                            forceVector.Y += (pg.Position.Y - position.Y)*pg.GravForce*radiusScaling*mass*
                                             gravityModifier;
                            forceVector.Z += (pg.Position.Z - position.Z)*pg.GravForce*radiusScaling*mass*
                                             gravityModifier;
                            /*if (forceVector.Z < 50 && forceVector.Z > 0)
                                forceVector.Z = 0;
                            else if (forceVector.Z > -50 && forceVector.Z < 0)
                                forceVector.Z = 0;*/
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Sets gravity parameters in the single axis, if you want a point, use the gravity point pieces
        /// </summary>
        /// <param name="enabled">Enable one axis gravity (disables point gravity)</param>
        /// <param name="forceX"></param>
        /// <param name="forceY"></param>
        /// <param name="forceZ"></param>
        public override void SetGravityForce(bool enabled, float forceX, float forceY, float forceZ)
        {
            normalGravityEnabled = enabled;
            gravityx = forceX;
            gravityy = forceY;
            gravityz = forceZ;
            //Set the vectors as well
            gravityVector = new Vector3(gravityx, gravityy, gravityz);
            gravityVectorNormalized = gravityVector;
            gravityVectorNormalized.Normalize();

            //Fix the ODE gravity too
            d.WorldSetGravity(world, gravityx, gravityy, gravityz);
        }

        public override float[] GetGravityForce()
        {
            return new float[] {gravityx, gravityy, gravityz};
        }

        public override void AddGravityPoint(bool isApplyingForces, Vector3 position, float forceX, float forceY,
                                             float forceZ, float gravForce, float radius, int identifier)
        {
            PointGravity pointGrav = new PointGravity
                                         {
                                             ForceX = forceX,
                                             ForceY = forceY,
                                             ForceZ = forceZ,
                                             GravForce = gravForce,
                                             Radius = radius,
                                             Position = position,
                                             PointForce = isApplyingForces
                                         };

            pointGravityInUse = true;
            m_pointGravityPositions[identifier] = pointGrav;
        }

        #endregion

        #region Wind Calcs

        public void AddWindForce(float mass, d.Vector3 AbsolutePosition, ref Vector3 force)
        {
            if (!DoPhyWind)
                return;
            if (m_windModule == null)
                m_windModule = m_scene.RequestModuleInterface<IWindModule>();

            if (m_windModule == null)
                return;
            Vector3 windSpeed = m_windModule.WindSpeed((int) AbsolutePosition.X, (int) AbsolutePosition.Y,
                                                       (int) AbsolutePosition.Z);
            force = (windSpeed)/(mass);
            force /= 20f; //Constant that doesn't make it too windy
        }

        #endregion

        #region Drawstuff

#if USE_DRAWSTUFF
    // Keyboard callback
        public void command(int cmd)
        {
            IntPtr geom;
            d.Mass mass;
            d.Vector3 sides = new d.Vector3(d.RandReal() * 0.5f + 0.1f, d.RandReal() * 0.5f + 0.1f, d.RandReal() * 0.5f + 0.1f);

            

            Char ch = Char.ToLower((Char)cmd);
            switch ((Char)ch)
            {
                case 'w':
                    try
                    {
                        Vector3 rotate = (new Vector3(1, 0, 0) * Quaternion.CreateFromEulers(hpr.Z * Utils.DEG_TO_RAD, hpr.Y * Utils.DEG_TO_RAD, hpr.X * Utils.DEG_TO_RAD));

                        xyz.X += rotate.X; xyz.Y += rotate.Y; xyz.Z += rotate.Z;
                        ds.SetViewpoint(ref xyz, ref hpr);
                    }
                    catch (ArgumentException)
                    { hpr.X = 0; }
                    break;

                case 'a':
                    hpr.X++;
                    ds.SetViewpoint(ref xyz, ref hpr);
                    break;

                case 's':
                    try
                    {
                        Vector3 rotate2 = (new Vector3(-1, 0, 0) * Quaternion.CreateFromEulers(hpr.Z * Utils.DEG_TO_RAD, hpr.Y * Utils.DEG_TO_RAD, hpr.X * Utils.DEG_TO_RAD));

                        xyz.X += rotate2.X; xyz.Y += rotate2.Y; xyz.Z += rotate2.Z;
                        ds.SetViewpoint(ref xyz, ref hpr);
                    }
                    catch (ArgumentException)
                    { hpr.X = 0; }
                    break;
                case 'd':
                    hpr.X--;
                    ds.SetViewpoint(ref xyz, ref hpr);
                    break;
                case 'r':
                    xyz.Z++;
                    ds.SetViewpoint(ref xyz, ref hpr);
                    break;
                case 'f':
                    xyz.Z--;
                    ds.SetViewpoint(ref xyz, ref hpr);
                    break;
                case 'e':
                    xyz.Y++;
                    ds.SetViewpoint(ref xyz, ref hpr);
                    break;
                case 'q':
                    xyz.Y--;
                    ds.SetViewpoint(ref xyz, ref hpr);
                    break;
            }
        }

        public void step(int pause)
        {
            
            ds.SetColor(1.0f, 1.0f, 0.0f);
            ds.SetTexture(ds.Texture.Wood);
            lock (_prims)
            {
                foreach (OdePrim prm in _prims)
                {
                    //IntPtr body = d.GeomGetBody(prm.prim_geom);
                    if (prm.prim_geom != IntPtr.Zero)
                    {
                        d.Vector3 pos;
                        d.GeomCopyPosition(prm.prim_geom, out pos);
                        //d.BodyCopyPosition(body, out pos);

                        d.Matrix3 R;
                        d.GeomCopyRotation(prm.prim_geom, out R);
                        //d.BodyCopyRotation(body, out R);


                        d.Vector3 sides = new d.Vector3();
                        sides.X = prm.Size.X;
                        sides.Y = prm.Size.Y;
                        sides.Z = prm.Size.Z;

                        ds.DrawBox(ref pos, ref R, ref sides);
                    }
                }
            }
            ds.SetColor(1.0f, 0.0f, 0.0f);
            lock (_characters)
            {
                foreach (OdeCharacter chr in _characters)
                {
                    if (chr.Shell != IntPtr.Zero)
                    {
                        IntPtr body = d.GeomGetBody(chr.Shell);

                        d.Vector3 pos;
                        d.GeomCopyPosition(chr.Shell, out pos);
                        //d.BodyCopyPosition(body, out pos);

                        d.Matrix3 R;
                        d.GeomCopyRotation(chr.Shell, out R);
                        //d.BodyCopyRotation(body, out R);

                        ds.DrawCapsule(ref pos, ref R, chr.Size.Z, 0.35f);
                        d.Vector3 sides = new d.Vector3();
                        sides.X = 0.5f;
                        sides.Y = 0.5f;
                        sides.Z = 0.5f;

                        ds.DrawBox(ref pos, ref R, ref sides);
                    }
                }
            }
        }

        public void start(int unused)
        {
            ds.SetViewpoint(ref xyz, ref hpr);
        }
#endif

        #endregion
    }
}