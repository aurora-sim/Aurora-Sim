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

//#define USE_DRAWSTUFF

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;
using log4net;
using Nini.Config;
using Ode.NET;
#if USE_DRAWSTUFF
using Drawstuff.NET;
#endif
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;
using OpenMetaverse;

//using OpenSim.Region.Physics.OdePlugin.Meshing;

namespace Aurora.Physics.AuroraOpenDynamicsEngine
{
    public sealed class AuroraPhysXPhysicsScene : PhysicsScene
    {
        #region Declares

        #region Enums

        /// <summary>
        /// this are prim change comands replace old taints
        /// but for now use a single comand per call since argument passing still doesn't support multiple comands
        /// </summary>
        public enum changes : int
        {
            Add = 0,                // arg null. finishs the prim creation. should be used internally only ( to remove later ?)
            Remove,
            Link,               // arg AuroraPhysXPrim new parent prim or null to delink. Makes the prim part of a object with prim parent as root
            //  or removes from a object if arg is null
            DeLink,
            Position,           // arg Vector3 new position in world coords. Changes prim position. Prim must know if it is root or child
            Orientation,        // arg Quaternion new orientation in world coords. Changes prim position. Prim must know it it is root or child
            PosOffset,          // not in use
            // arg Vector3 new position in local coords. Changes prim position in object
            OriOffset,          // not in use
            // arg Vector3 new position in local coords. Changes prim position in object
            Velocity,
            AngVelocity,
            Acceleration,
            Force,
            Torque,

            AddForce,
            AddAngForce,
            AngLock,

            Size,
            Shape,

            CollidesWater,
            VolumeDtc,

            Physical,
            Selected,
            disabled,
            buildingrepresentation,

            Null             //keep this last used do dim the methods array. does nothing but pulsing the prim
        }

        #endregion


        private CollisionLocker ode;

        public float ODE_STEPSIZE = 0.020f;
        private float m_timeDilation = 1.0f;

        public float gravityx = 0f;
        public float gravityy = 0f;
        public float gravityz = -9.8f;
        public bool m_hasSetUpPrims = false;
        private const int maxContactsbeforedeath = 10000;
        private int m_currentmaxContactsbeforedeath = maxContactsbeforedeath;

        private float contactsurfacelayer = 0.001f;

        private int HashspaceLow = -3;  // current ODE limits
        private int HashspaceHigh = 10;
        private int GridSpaceScaleBits = 5; // used to do shifts to find space from position. Value decided from region size in init
        private int nspacesPerSideX = 8;
        private int nspacesPerSideY = 8;

        private int framecount = 0;

        private readonly IntPtr contactgroup;

        private float nmAvatarObjectContactFriction = 250f;
        private float nmAvatarObjectContactBounce = 0.1f;

        private float mAvatarObjectContactFriction = 75f;
        private float mAvatarObjectContactBounce = 0.1f;

        public float PID_D = 3200f;
        public float PID_P = 1400f;
        public float avCapRadius = 0.37f;
        public float avDensity = 80f;
        public float avHeightFudgeFactor = 0.52f;
        public float avMovementDivisorWalk = 1.3f;
        public float avMovementDivisorRun = 0.8f;
        private float minimumGroundFlightOffset = 3f;
        public float maximumMassObject = 100000.01f;

        public bool meshSculptedPrim = true;
        public bool forceSimplePrimMeshing = true;

        public float meshSculptLOD = 32;
        public float MeshSculptphysicalLOD = 16;

        public float geomDefaultDensity = 10.000006836f;

        public int geomContactPointsStartthrottle = 3;
        public int geomUpdatesPerThrottledUpdate = 15;

        public float bodyPIDD = 35f;
        public float bodyPIDG = 25;

        public int geomCrossingFailuresBeforeOutofbounds = 1;

        public int bodyFramesAutoDisable = 10;

        private bool m_filterCollisions = false;

        private d.NearCallback nearCallback;
        public d.TriCallback triCallback;
        private readonly HashSet<AuroraODECharacter> _characters = new HashSet<AuroraODECharacter>();
        private readonly HashSet<AuroraPhysXPrim> _prims = new HashSet<AuroraPhysXPrim>();
        private readonly object _activeprimsLock = new object ();
        private readonly HashSet<AuroraPhysXPrim> _activeprims = new HashSet<AuroraPhysXPrim>();
        private readonly HashSet<AuroraODECharacter> _taintedActors = new HashSet<AuroraODECharacter>();
        public struct AODEchangeitem
        {
            public AuroraPhysXPrim prim;
            public changes what;
            public Object arg;
        }
        public OpenSim.Framework.LocklessQueue<AODEchangeitem> ChangesQueue = new OpenSim.Framework.LocklessQueue<AODEchangeitem>();
        private readonly List<d.ContactGeom> _perloopContact = new List<d.ContactGeom> ();

        private readonly List<PhysicsActor> _collisionEventPrimList = new List<PhysicsActor> ();
        private readonly Dictionary<UUID, PhysicsActor> _collisionEventPrimDictionary = new Dictionary<UUID, PhysicsActor> ();
        private readonly object _collisionEventListLock = new object ();

        private readonly HashSet<AuroraODECharacter> _badCharacter = new HashSet<AuroraODECharacter>();
        public Dictionary<IntPtr, PhysicsActor> actor_name_map = new Dictionary<IntPtr, PhysicsActor>();
        private d.ContactGeom[] contacts;
        public IntPtr RegionTerrain;
        private readonly Dictionary<IntPtr, short[]> TerrainHeightFieldHeights = new Dictionary<IntPtr, short[]> ();
        private readonly Dictionary<IntPtr, short[]> ODETerrainHeightFieldHeights = new Dictionary<IntPtr, short[]> ();
        private ITerrainChannel m_channel = null;
        private readonly Dictionary<IntPtr, float[]> TerrainHeightFieldlimits = new Dictionary<IntPtr, float[]>();
        private short[] WaterHeightFieldHeight;
        public bool m_EnableAutoConfig = true;
        public bool m_allowJump = true;
        public bool m_usepreJump = true;
        public int m_preJumpTime = 15;
        public float m_preJumpForceMultiplier = 4;
        public float m_AvFlySpeed = 4.0f;

        private d.Contact contact;
        private d.Contact AvatarMovementprimContact;

        private int m_physicsiterations = 10;
        //private int m_timeBetweenRevertingAutoConfigIterations = 50;
        private const float m_SkipFramesAtms = 0.150f; // Drop frames gracefully at a 150 ms lag
        private readonly PhysicsActor PANull = new NullObjectPhysicsActor();
        private float step_time = 0.0f;
        private RegionInfo m_region;
        private IRegistryCore m_registry;
        private IWindModule m_windModule = null;

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

        public Object OdeLock;

        public IMesher mesher;

        private IConfigSource m_config;

        public bool physics_logging = false;
        public int physics_logging_interval = 0;
        public bool physics_logging_append_existing_logfile = false;

        private volatile int m_global_contactcount = 0;

        public Vector2 WorldExtents;
        private int contactsPerCollision = 80;

        public bool AllowUnderwaterPhysics = false;
        public bool AllowAvGravity = true;
        public int AvGravityHeight = 4096;
        public bool AllowAvsToEscapeGravity = true;

        public float m_flightCeilingHeight = 2048.0f; // rex
        public bool m_useFlightCeilingHeight = false;

        private AuroraODERayCastRequestManager m_rayCastManager;
        private bool IsLocked = false;
        private List<PhysicsActor> RemoveQueue;
        private List<PhysicsActor> ActiveAddCollisionQueue = new List<PhysicsActor>();
        private List<PhysicsActor> ActiveRemoveCollisionQueue = new List<PhysicsActor>();
        private bool m_disableCollisions = false;

        public float m_avDecayTime = 0.985f;
        public float m_avStopDecaying = 2.05f;

        public override bool DisableCollisions
        {
            get
            {
                return m_disableCollisions;
            }
            set
            {
                m_disableCollisions = value;
            }
        }

        public override bool UseUnderWaterPhysics
        {
            get { return AllowUnderwaterPhysics; }
        }

        #region Stats

        private int m_StatPhysicsTaintTime;
        public override int StatPhysicsTaintTime
        {
            get { return m_StatPhysicsTaintTime; }
        }

        private int m_StatPhysicsMoveTime;
        public override int StatPhysicsMoveTime
        {
            get { return m_StatPhysicsMoveTime; }
        }

        private int m_StatCollisionOptimizedTime;
        public override int StatCollisionOptimizedTime
        {
            get { return m_StatCollisionOptimizedTime; }
        }

        private int m_StatSendCollisionsTime;
        public override int StatSendCollisionsTime
        {
            get { return m_StatSendCollisionsTime; }
        }

        private int m_StatAvatarUpdatePosAndVelocity;
        public override int StatAvatarUpdatePosAndVelocity
        {
            get { return m_StatAvatarUpdatePosAndVelocity; }
        }

        private int m_StatPrimUpdatePosAndVelocity;
        public override int StatPrimUpdatePosAndVelocity
        {
            get { return m_StatPrimUpdatePosAndVelocity; }
        }

        private int m_StatUnlockedArea;
        public override int StatUnlockedArea
        {
            get { return m_StatUnlockedArea; }
        }

        private int m_StatFindContactsTime;
        public override int StatFindContactsTime
        {
            get { return m_StatFindContactsTime; }
        }

        private int m_StatContactLoopTime;
        public override int StatContactLoopTime
        {
            get { return m_StatContactLoopTime; }
        }

        private int m_StatCollisionAccountingTime;
        public override int StatCollisionAccountingTime
        {
            get { return m_StatCollisionAccountingTime; }
        }

        #endregion

        #endregion

        #region Constructor/Initialization

        /// <summary>
        /// Initiailizes the scene
        /// Sets many properties that ODE requires to be stable
        /// These settings need to be tweaked 'exactly' right or weird stuff happens.
        /// </summary>
        public AuroraPhysXPhysicsScene(CollisionLocker dode, string sceneIdentifier)
        {
            OdeLock = new Object();
            ode = dode;
            nearCallback = near;
            triCallback = TriCallback;
            lock (OdeLock)
            {
                // Create the world and the first space
                world = d.WorldCreate();
                space = d.HashSpaceCreate(IntPtr.Zero);


                contactgroup = d.JointGroupCreate(0);
                //contactgroup

                d.WorldSetAutoDisableFlag(world, false);
#if USE_DRAWSTUFF
                
                Thread viewthread = new Thread(new ParameterizedThreadStart(startvisualization));
                viewthread.Start();
#endif
            }
        }

#if USE_DRAWSTUFF
        public void startvisualization(object o)
        {
            ds.Functions fn;
            fn.version = ds.VERSION;
            fn.start = new ds.CallbackFunction(start);
            fn.step = new ds.CallbackFunction(step);
            fn.command = new ds.CallbackFunction(command);
            fn.stop = null;
            fn.path_to_textures = "./textures";
            string[] args = new string[0];
            ds.SimulationLoop(args.Length, args, 352, 288, ref fn);
        }
#endif

        // Initialize the mesh plugin
        public override void Initialise(IMesher meshmerizer, RegionInfo region, IRegistryCore registry)
        {
            mesher = meshmerizer;
            m_region = region;
            m_registry = registry;
            WorldExtents = new Vector2(region.RegionSizeX, region.RegionSizeY);
        }

        public override void PostInitialise(IConfigSource config)
        {
            m_rayCastManager = new AuroraODERayCastRequestManager(this);
            RemoveQueue = new List<PhysicsActor>();
            m_config = config;
            // Defaults

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                PID_D = 3200.0f;
                PID_P = 1400.0f;
            }
            else
            {
                PID_D = 2200.0f;
                PID_P = 900.0f;
            }

            if (m_config != null)
            {
                IConfig physicsconfig = m_config.Configs["AuroraODEPhysicsSettings"];
                if (physicsconfig != null)
                {
                    gravityx = physicsconfig.GetFloat("world_gravityx", 0f);
                    gravityy = physicsconfig.GetFloat("world_gravityy", 0f);
                    gravityz = physicsconfig.GetFloat("world_gravityz", -9.8f);

                    m_avDecayTime = physicsconfig.GetFloat("avDecayTime", m_avDecayTime);
                    m_avStopDecaying = physicsconfig.GetFloat("avStopDecaying", m_avStopDecaying);

                    AllowUnderwaterPhysics = physicsconfig.GetBoolean("useUnderWaterPhysics", false);
                    AllowAvGravity = physicsconfig.GetBoolean("useAvGravity", true);
                    AvGravityHeight = physicsconfig.GetInt("avGravityHeight", 4096);
                    AllowAvsToEscapeGravity = physicsconfig.GetBoolean("aviesCanEscapeGravity", true);

                    m_AvFlySpeed = physicsconfig.GetFloat("AvFlySpeed", m_AvFlySpeed);
                    m_allowJump = physicsconfig.GetBoolean("AllowJump", m_allowJump);
                    m_usepreJump = physicsconfig.GetBoolean("UsePreJump", m_usepreJump);
                    m_preJumpTime = physicsconfig.GetInt("PreJumpTime", m_preJumpTime);
                    m_preJumpForceMultiplier = physicsconfig.GetFloat("PreJumpMultiplier", m_preJumpForceMultiplier);

                    contactsurfacelayer = physicsconfig.GetFloat("world_contact_surface_layer", 0.001f);

                    nmAvatarObjectContactFriction = physicsconfig.GetFloat("objectcontact_friction", 250f);
                    nmAvatarObjectContactBounce = physicsconfig.GetFloat("objectcontact_bounce", 0.2f);

                    mAvatarObjectContactFriction = physicsconfig.GetFloat("m_avatarobjectcontact_friction", 75f);
                    mAvatarObjectContactBounce = physicsconfig.GetFloat("m_avatarobjectcontact_bounce", 0.1f);

                    ODE_STEPSIZE = physicsconfig.GetFloat("world_stepsize", 0.020f);
                    m_physicsiterations = physicsconfig.GetInt("world_internal_steps_without_collisions", 10);

                    avDensity = physicsconfig.GetFloat("av_density", 80f);
                    avHeightFudgeFactor = physicsconfig.GetFloat("av_height_fudge_factor", 0.52f);
                    avMovementDivisorWalk = (physicsconfig.GetFloat("WalkSpeed", 1.3f) * 2);
                    avMovementDivisorRun = (physicsconfig.GetFloat("RunSpeed", 0.8f) * 2);
                    avCapRadius = physicsconfig.GetFloat("av_capsule_radius", 0.37f);

                    contactsPerCollision = physicsconfig.GetInt("contacts_per_collision", 80);

                    geomContactPointsStartthrottle = physicsconfig.GetInt("geom_contactpoints_start_throttling", 3);
                    geomUpdatesPerThrottledUpdate = physicsconfig.GetInt("geom_updates_before_throttled_update", 15);
                    geomCrossingFailuresBeforeOutofbounds = physicsconfig.GetInt("geom_crossing_failures_before_outofbounds", 5);

                    geomDefaultDensity = physicsconfig.GetFloat("geometry_default_density", 10.000006836f);
                    bodyFramesAutoDisable = physicsconfig.GetInt("body_frames_auto_disable", 10);

                    bodyPIDD = physicsconfig.GetFloat("body_pid_derivative", 35f);
                    bodyPIDG = physicsconfig.GetFloat("body_pid_gain", 25f);

                    forceSimplePrimMeshing = physicsconfig.GetBoolean("force_simple_prim_meshing", forceSimplePrimMeshing);
                    meshSculptedPrim = physicsconfig.GetBoolean("mesh_sculpted_prim", true);
                    meshSculptLOD = physicsconfig.GetFloat("mesh_lod", 32f);
                    MeshSculptphysicalLOD = physicsconfig.GetFloat("mesh_physical_lod", 16f);
                    m_filterCollisions = physicsconfig.GetBoolean("filter_collisions", false);

                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        PID_D = physicsconfig.GetFloat("av_pid_derivative_linux", 2200.0f);
                        PID_P = physicsconfig.GetFloat("av_pid_proportional_linux", 900.0f);
                    }
                    else
                    {
                        PID_D = physicsconfig.GetFloat("av_pid_derivative_win", 2200.0f);
                        PID_P = physicsconfig.GetFloat("av_pid_proportional_win", 900.0f);
                    }
                    physics_logging = physicsconfig.GetBoolean("physics_logging", false);
                    physics_logging_interval = physicsconfig.GetInt("physics_logging_interval", 0);
                    physics_logging_append_existing_logfile = physicsconfig.GetBoolean("physics_logging_append_existing_logfile", false);
                    m_useFlightCeilingHeight = physicsconfig.GetBoolean("Use_Flight_Ceiling_Height_Max", m_useFlightCeilingHeight);
                    m_flightCeilingHeight = physicsconfig.GetFloat("Flight_Ceiling_Height_Max", m_flightCeilingHeight); //Rex

                    minimumGroundFlightOffset = physicsconfig.GetFloat("minimum_ground_flight_offset", 3f);
                    maximumMassObject = physicsconfig.GetFloat("maximum_mass_object", 100000.01f);
                }
            }

            lock (OdeLock)
            {
                contacts = new d.ContactGeom[contactsPerCollision];

                // Centeral contact friction and bounce
                // ckrinke 11/10/08 Enabling soft_erp but not soft_cfm until I figure out why
                // an avatar falls through in Z but not in X or Y when walking on a prim.
                contact.surface.mode |= d.ContactFlags.SoftERP;
                contact.surface.mu = nmAvatarObjectContactFriction;
                contact.surface.bounce = nmAvatarObjectContactBounce;
                contact.surface.soft_cfm = 0.010f;
                contact.surface.soft_erp = 0.010f;

                // Prim contact friction and bounce
                // THis is the *non* moving version of friction and bounce
                // Use this when an avatar comes in contact with a prim
                // and is moving
                AvatarMovementprimContact.surface.mu = mAvatarObjectContactFriction;
                AvatarMovementprimContact.surface.bounce = mAvatarObjectContactBounce;

                // Set the gravity,, don't disable things automatically (we set it explicitly on some things)
                d.WorldSetGravity (world, gravityx, gravityy, gravityz);
                d.WorldSetContactSurfaceLayer (world, contactsurfacelayer);

                d.WorldSetLinearDamping (world, 256f);
                d.WorldSetAngularDamping (world, 256f);
                d.WorldSetAngularDampingThreshold (world, 256f);
                d.WorldSetLinearDampingThreshold (world, 256f);
                d.WorldSetMaxAngularSpeed (world, 256f);

                // Set how many steps we go without running collision testing
                // This is in addition to the step size.
                // Essentially Steps * m_physicsiterations
                d.WorldSetQuickStepNumIterations (world, m_physicsiterations);
                //d.WorldSetContactMaxCorrectingVel(world, 1000.0f);

                if (staticPrimspace != null)
                    return;//Reloading config, don't mess with this stuff

                d.HashSpaceSetLevels (space, HashspaceLow, HashspaceHigh);

                //  spaces grid for static objects

                if (WorldExtents.X < WorldExtents.Y)
                    // // constant is 1/log(2),  -3 for division by 8 plus 0.5 for rounding
                    GridSpaceScaleBits = (int)(Math.Log ((double)WorldExtents.X) * 1.4426950f - 2.5f);
                else
                    GridSpaceScaleBits = (int)(Math.Log ((double)WorldExtents.Y) * 1.4426950f - 2.5f);

                if (GridSpaceScaleBits < 4) // no less than 16m side
                    GridSpaceScaleBits = 4;
                else if (GridSpaceScaleBits > 10)
                    GridSpaceScaleBits = 10;   // no more than 1Km side

                int nspacesPerSideX = (int)(WorldExtents.X) >> GridSpaceScaleBits;
                int nspacesPerSideY = (int)(WorldExtents.Y) >> GridSpaceScaleBits;

                if ((int)(WorldExtents.X) > nspacesPerSideX << GridSpaceScaleBits)
                    nspacesPerSideX++;
                if ((int)(WorldExtents.Y) > nspacesPerSideY << GridSpaceScaleBits)
                    nspacesPerSideY++;

                staticPrimspace = new IntPtr[nspacesPerSideX, nspacesPerSideY];

                IntPtr aSpace;

                for (int i = 0; i < nspacesPerSideX; i++)
                {
                    for (int j = 0; j < nspacesPerSideY; j++)
                    {
                        aSpace = d.HashSpaceCreate (IntPtr.Zero);
                        staticPrimspace[i, j] = aSpace;
                        d.GeomSetCategoryBits (aSpace, (int)CollisionCategories.Space);
                        waitForSpaceUnlock (aSpace);
                        d.SpaceSetSublevel (aSpace, 1);
                        d.SpaceAdd (space, aSpace);
                    }
                }
            }
        }

        #endregion

        #region Collision Detection

        /// <summary>
        /// This is our near callback.  A geometry is near a body
        /// </summary>
        /// <param name="space">The space that contains the geoms.  Remember, spaces are also geoms</param>
        /// <param name="g1">a geometry or space</param>
        /// <param name="g2">another geometry or space</param>
        private void near(IntPtr space, IntPtr g1, IntPtr g2)
        {
            //  no lock here!  It's invoked from within Simulate(), which is thread-locked

            if (g1 == IntPtr.Zero || g2 == IntPtr.Zero)
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
                catch (AccessViolationException)
                {
                    m_log.Warn("[PHYSICS]: Unable to collide test a space");
                    return;
                }
                //Colliding a space or a geom with a space or a geom. so drill down

                //Collide all geoms in each space..
                //if (d.GeomIsSpace(g1)) 
                //    d.SpaceCollide(g1, IntPtr.Zero, nearCallback);
                //if (d.GeomIsSpace(g2))
                //    d.SpaceCollide(g2, IntPtr.Zero, nearCallback);
                return;
            }
            IntPtr b1 = d.GeomGetBody(g1);
            IntPtr b2 = d.GeomGetBody(g2);

            int FindContactsTime = Util.EnvironmentTickCount();

            // d.GeomClassID id = d.GeomGetClass(g1);

            //if (id == d.GeomClassId.TriMeshClass)
            //{
            //               m_log.InfoFormat("near: A collision was detected between {1} and {2}", 0, name1, name2);
            //m_log.Debug("near: A collision was detected between {1} and {2}", 0, name1, name2);
            //}

            // Figure out how many contact points we have
            int count = 0;
            PhysicsActor p1;
            PhysicsActor p2;

            if (!actor_name_map.TryGetValue (g1, out p1))
                p1 = PANull;

            if (!actor_name_map.TryGetValue (g2, out p2))
                p2 = PANull;
            if (p1 is AuroraPhysXPrim && (p1 as AuroraPhysXPrim)._zeroFlag)
                if (p2 is AuroraPhysXPrim && (p2 as AuroraPhysXPrim)._zeroFlag)
                    return;//If its frozen, don't collide it against other objects, let them collide against it
                else
                    (p1 as AuroraPhysXPrim)._zeroFlag = false;
            try
            {
                // Colliding Geom To Geom
                // This portion of the function 'was' blatantly ripped off from BoxStack.cs

                if (g1 == g2)
                    return; // Can't collide with yourself

                if (b1 != IntPtr.Zero && b2 != IntPtr.Zero && d.AreConnectedExcluding(b1, b2, d.JointType.Contact))
                    return;

                lock (contacts)
                {
                    count = d.Collide(g1, g2, (contacts.Length & 0xffff), contacts, d.ContactGeom.SizeOf);
                }
            }
            catch (SEHException)
            {
                m_log.Error("[PHYSICS]: The Operating system shut down ODE because of corrupt memory.  This could be a result of really irregular terrain.  If this repeats continuously, restart using Basic Physics and terrain fill your terrain.  Restarting the sim.");
                ode.drelease(world);
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[PHYSICS]: Unable to collide test an object: {0}", e.ToString());
                return;
            }

            if (count == 0)
                return;

            m_StatFindContactsTime = Util.EnvironmentTickCountSubtract(FindContactsTime);

            ContactPoint maxDepthContact = new ContactPoint();
            if (!DisableCollisions)
            {
                if (p1.CollisionScore + count >= float.MaxValue)
                    p1.CollisionScore = 0;
                p1.CollisionScore += count;

                if (p2.CollisionScore + count >= float.MaxValue)
                    p2.CollisionScore = 0;
                p2.CollisionScore += count;
            }

            int ContactLoopTime = Util.EnvironmentTickCount();

            #region Contact Loop

            IntPtr joint = IntPtr.Zero;
            for (int i = 0; i < count; i++)
            {
                d.ContactGeom curContact = contacts[i];

                if (curContact.depth > maxDepthContact.PenetrationDepth)
                {
                    maxDepthContact.PenetrationDepth = curContact.depth;
                    maxDepthContact.Position.X = curContact.pos.X;
                    maxDepthContact.Position.Y = curContact.pos.Y;
                    maxDepthContact.Position.Z = curContact.pos.Z;
                    maxDepthContact.SurfaceNormal.X = curContact.normal.X;
                    maxDepthContact.SurfaceNormal.Y = curContact.normal.Y;
                    maxDepthContact.SurfaceNormal.Z = curContact.normal.Z;
                    //                    maxDepthContact = new ContactPoint(
                    //                        new Vector3((float)curContact.pos.X, (float)curContact.pos.Y, (float)curContact.pos.Z),
                    //                        new Vector3((float)curContact.normal.X, (float)curContact.normal.Y, (float)curContact.normal.Z),
                    //                        (float)curContact.depth
                    //                    );
                }

                //m_log.Warn("[CCOUNT]: " + count);
                // If we're colliding with terrain, use 'TerrainContact' instead of contact.
                // allows us to have different settings

                // we don't want prim or avatar to explode

                #region InterPenetration Handling - Unintended physics explosions
                #region disabled code1

                /*

                if (curContact.depth >= 0.08f)
                    {
                    //This is disabled at the moment only because it needs more tweaking
                    //It will eventually be uncommented
                    if (contact.depth >= 1.00f)
                    {
                        //m_log.Debug("[PHYSICS]: " + contact.depth.ToString());
                    }

                    //If you interpenetrate a prim with an agent
                    if ((p2.PhysicsActorType == (int) ActorTypes.Agent &&
                         p1.PhysicsActorType == (int) ActorTypes.Prim) ||
                        (p1.PhysicsActorType == (int) ActorTypes.Agent &&
                         p2.PhysicsActorType == (int) ActorTypes.Prim))
                    {
                        
                        //contact.depth = contact.depth * 4.15f;
                        if (p2.PhysicsActorType == (int) ActorTypes.Agent)
                        {
                            p2.CollidingObj = true;
                            contact.depth = 0.003f;
                            p2.Velocity = p2.Velocity + new PhysicsVector(0, 0, 2.5f);
                            OdeCharacter character = (OdeCharacter) p2;
                            character.SetPidStatus(true);
                            contact.pos = new d.Vector3(contact.pos.X + (p1.Size.X / 2), contact.pos.Y + (p1.Size.Y / 2), contact.pos.Z + (p1.Size.Z / 2));

                        }
                        else
                        {

                            //contact.depth = 0.0000000f;
                        }
                        if (p1.PhysicsActorType == (int) ActorTypes.Agent)
                        {

                            p1.CollidingObj = true;
                            contact.depth = 0.003f;
                            p1.Velocity = p1.Velocity + new PhysicsVector(0, 0, 2.5f);
                            contact.pos = new d.Vector3(contact.pos.X + (p2.Size.X / 2), contact.pos.Y + (p2.Size.Y / 2), contact.pos.Z + (p2.Size.Z / 2));
                            OdeCharacter character = (OdeCharacter)p1;
                            character.SetPidStatus(true);
                        }
                        else
                        {

                            //contact.depth = 0.0000000f;
                        }
                          
                        
                     
                    }
*/
                // If you interpenetrate a prim with another prim
                /*
                    if (p1.PhysicsActorType == (int) ActorTypes.Prim && p2.PhysicsActorType == (int) ActorTypes.Prim)
                    {
                        #region disabledcode2
                        //OdePrim op1 = (OdePrim)p1;
                        //OdePrim op2 = (OdePrim)p2;
                        //op1.m_collisionscore++;
                        //op2.m_collisionscore++;

                        //if (op1.m_collisionscore > 8000 || op2.m_collisionscore > 8000)
                        //{
                            //op1.m_taintdisable = true;
                            //AddPhysicsActorTaint(p1);
                            //op2.m_taintdisable = true;
                            //AddPhysicsActorTaint(p2);
                        //}

                        //if (contact.depth >= 0.25f)
                        //{
                            // Don't collide, one or both prim will expld.

                            //op1.m_interpenetrationcount++;
                            //op2.m_interpenetrationcount++;
                            //interpenetrations_before_disable = 200;
                            //if (op1.m_interpenetrationcount >= interpenetrations_before_disable)
                            //{
                                //op1.m_taintdisable = true;
                                //AddPhysicsActorTaint(p1);
                            //}
                            //if (op2.m_interpenetrationcount >= interpenetrations_before_disable)
                            //{
                               // op2.m_taintdisable = true;
                                //AddPhysicsActorTaint(p2);
                            //}

                            //contact.depth = contact.depth / 8f;
                            //contact.normal = new d.Vector3(0, 0, 1);
                        //}
                        //if (op1.m_disabled || op2.m_disabled)
                        //{
                            //Manually disabled objects stay disabled
                            //contact.depth = 0f;
                        //}
                        #endregion
                    }
                        
//                    if (curContact.depth >= 1.0f)
                    {
                    //m_log.Info("[P]: " + contact.depth.ToString());
                    if ((p2.PhysicsActorType == (int)ActorTypes.Agent &&
                         p1.PhysicsActorType == (int)ActorTypes.Ground) ||
                        (p1.PhysicsActorType == (int)ActorTypes.Agent &&
                         p2.PhysicsActorType == (int)ActorTypes.Ground))
                        {
                        if (p2.PhysicsActorType == (int)ActorTypes.Agent)
                            {
                            if (p2 is AuroraODECharacter)
                                {
                                AuroraODECharacter character = (AuroraODECharacter)p2;

                                //p2.CollidingObj = true;
                                if (p2.Position.Z < curContact.pos.Z) // don't colide from underside
                                    {
                                    p2.CollidingGround = false;
                                    p2.CollidingGround = false;
                                    continue;
                                    }

                                curContact.depth = 0.00000003f;
 
                                p2.Velocity = p2.Velocity + new Vector3(0f, 0f, 1.5f);
                                curContact.pos =
                                              new d.Vector3(curContact.pos.X + (p2.Size.X / 2),
                                                              curContact.pos.Y + (p2.Size.Y / 2),
                                                              curContact.pos.Z + (p2.Size.Z / 2));
                                character.SetPidStatus(true);

                                }
                            }

                        if (p1.PhysicsActorType == (int)ActorTypes.Agent)
                            {
                            if (p1 is AuroraODECharacter)
                                {
                                AuroraODECharacter character = (AuroraODECharacter)p1;

                                //p2.CollidingObj = true;
                                if (p1.Position.Z < curContact.pos.Z)
                                    {
                                    p1.CollidingGround = false;
                                    p1.CollidingGround = false;
                                    continue;
                                    }

                                curContact.depth = 0.00000003f;
                                p1.Velocity = p1.Velocity + new Vector3(0f, 0f, 0.5f);
                                curContact.pos =
                                                new d.Vector3(curContact.pos.X + (p1.Size.X / 2),
                                                              curContact.pos.Y + (p1.Size.Y / 2),
                                                              curContact.pos.Z + (p1.Size.Z / 2));
                                character.SetPidStatus(true);

                                }
                            }
                        }

                    }

                }
*/
                #endregion
                #endregion

                // Logic for collision handling
                // Note, that if *all* contacts are skipped (VolumeDetect)
                // The prim still detects (and forwards) collision events but 
                // appears to be phantom for the world
                Boolean skipThisContact = false;

                if (p1 is PhysicsObject && ((PhysicsObject)p1).VolumeDetect)
                    skipThisContact = true;   // No collision on volume detect prims

                if (p2 is PhysicsObject && ((PhysicsObject)p2).VolumeDetect)
                    skipThisContact = true;   // No collision on volume detect prims

                if (curContact.depth < 0f)
                    skipThisContact = true;

                if (!skipThisContact && 
                    m_filterCollisions && 
                    checkDupe (curContact, p2.PhysicsActorType))
                    skipThisContact = true;

                if (!skipThisContact)
                {
                    // If we're colliding against terrain
                    if (p1.PhysicsActorType == (int)ActorTypes.Ground)
                    {
                        if (p2.PhysicsActorType == (int)ActorTypes.Prim)
                        {
                            if (m_filterCollisions)
                                _perloopContact.Add (curContact);

                            //Add restitution and friction changes
                            d.Contact contact = ((AuroraPhysXPrim)p2).GetContactPoint (ActorTypes.Ground);
                            contact.geom = curContact;

                            if (m_global_contactcount < m_currentmaxContactsbeforedeath)
                            {
                                joint = d.JointCreateContact (world, contactgroup, ref contact);
                                m_global_contactcount++;
                            }
                        }
                        //Can't collide against anything else, agents do their own ground checks
                    }
                    else
                    {
                        // we're colliding with prim or avatar
                        // check if we're moving
                        if ((p2.PhysicsActorType == (int)ActorTypes.Agent))
                        {
                            if ((Math.Abs (p2.Velocity.X) > 0.01f || Math.Abs (p2.Velocity.Y) > 0.01f))
                            {
                                // Use the Movement prim contact
                                AvatarMovementprimContact.geom = curContact;
                                if (m_filterCollisions)
                                    _perloopContact.Add (curContact);
                                if (m_global_contactcount < m_currentmaxContactsbeforedeath)
                                {
                                    joint = d.JointCreateContact (world, contactgroup, ref AvatarMovementprimContact);
                                    m_global_contactcount++;
                                }
                            }
                            else
                            {
                                // Use the non movement contact
                                contact.geom = curContact;
                                if (m_filterCollisions)
                                    _perloopContact.Add (curContact);

                                if (m_global_contactcount < m_currentmaxContactsbeforedeath)
                                {
                                    joint = d.JointCreateContact (world, contactgroup, ref contact);
                                    m_global_contactcount++;
                                }
                            }
                        }
                        else if (p2.PhysicsActorType == (int)ActorTypes.Prim)
                        {
                            if (m_filterCollisions)
                                _perloopContact.Add (curContact);

                            //Add restitution and friction changes
                            d.Contact contact = ((AuroraPhysXPrim)p2).GetContactPoint (ActorTypes.Prim);
                            contact.geom = curContact;

                            if (m_global_contactcount < m_currentmaxContactsbeforedeath)
                            {
                                joint = d.JointCreateContact (world, contactgroup, ref contact);
                                m_global_contactcount++;
                            }
                        }
                    }

                    if (m_global_contactcount < m_currentmaxContactsbeforedeath && joint != IntPtr.Zero) // stack collide!
                    {
                        d.JointAttach (joint, b1, b2);
                        m_global_contactcount++;
                    }
                }
                //m_log.Debug(count.ToString());
                //m_log.Debug("near: A collision was detected between {1} and {2}", 0, name1, name2);
            }

            #endregion

            m_StatContactLoopTime = Util.EnvironmentTickCountSubtract(ContactLoopTime);

            int CollisionAccountingTime = Util.EnvironmentTickCount();

            if (!DisableCollisions)
            {
                bool p2col = false;

                // We only need to test p2 for 'jump crouch purposes'
                if (p2 is AuroraODECharacter && p1.PhysicsActorType == (int)ActorTypes.Prim)
                {
                    // Testing if the collision is at the feet of the avatar
                    if ((p2.Position.Z - maxDepthContact.Position.Z) > (p2.Size.Z * 0.6f))
                        p2col = true;
                }
                else
                    p2col = true;

                p2.IsColliding = p2col;

                if (count > geomContactPointsStartthrottle)
                {
                    // If there are more then 3 contact points, it's likely
                    // that we've got a pile of objects, so ...
                    // We don't want to send out hundreds of terse updates over and over again
                    // so lets throttle them and send them again after it's somewhat sorted out.
                    p2.ThrottleUpdates = true;
                }
                collision_accounting_events(p1, p2, maxDepthContact);
            }
            m_StatCollisionAccountingTime = Util.EnvironmentTickCountSubtract(CollisionAccountingTime);
        }

        private d.SurfaceParameters CopyContact (d.SurfaceParameters surfaceParameters)
        {
            d.SurfaceParameters p = new d.SurfaceParameters ();
            p.bounce = surfaceParameters.bounce;
            p.bounce_vel = surfaceParameters.bounce_vel;
            p.mode = surfaceParameters.mode;
            p.motion1 = surfaceParameters.motion1;
            p.motion2 = surfaceParameters.motion2;
            p.motionN = surfaceParameters.motionN;
            p.mu = surfaceParameters.mu;
            p.mu2 = surfaceParameters.mu2;
            p.slip1 = surfaceParameters.slip1;
            p.slip2 = surfaceParameters.slip2;
            p.soft_cfm = surfaceParameters.soft_cfm;
            p.soft_erp = surfaceParameters.soft_erp;
            return p;
        }

        private bool checkDupe(d.ContactGeom contactGeom, int atype)
        {
            bool result = false;
            //return result;
            if (!m_filterCollisions)
                return false;

            ActorTypes at = (ActorTypes)atype;
            //Stopwatch watch = new Stopwatch();
            //watch.Start();
            lock (_perloopContact)
            {
                foreach (d.ContactGeom contact in _perloopContact)
                {
                    //if ((contact.g1 == contactGeom.g1 && contact.g2 == contactGeom.g2))
                    //{
                    // || (contact.g2 == contactGeom.g1 && contact.g1 == contactGeom.g2)
                    if (at == ActorTypes.Agent)
                    {
                        if (((Math.Abs(contactGeom.normal.X - contact.normal.X) < 1.026f) && (Math.Abs(contactGeom.normal.Y - contact.normal.Y) < 0.303f) && (Math.Abs(contactGeom.normal.Z - contact.normal.Z) < 0.065f)))
                        {

                            if (Math.Abs(contact.depth - contactGeom.depth) < 0.052f)
                            {
                                //contactGeom.depth *= .00005f;
                                //m_log.DebugFormat("[Collsion]: Depth {0}", Math.Abs(contact.depth - contactGeom.depth));
                                // m_log.DebugFormat("[Collision]: <{0},{1},{2}>", Math.Abs(contactGeom.normal.X - contact.normal.X), Math.Abs(contactGeom.normal.Y - contact.normal.Y), Math.Abs(contactGeom.normal.Z - contact.normal.Z));
                                result = true;
                                break;
                            }
                            else
                            {
                                //m_log.DebugFormat("[Collsion]: Depth {0}", Math.Abs(contact.depth - contactGeom.depth));
                            }
                        }
                        else
                        {
                            //m_log.DebugFormat("[Collision]: <{0},{1},{2}>", Math.Abs(contactGeom.normal.X - contact.normal.X), Math.Abs(contactGeom.normal.Y - contact.normal.Y), Math.Abs(contactGeom.normal.Z - contact.normal.Z));
                            //int i = 0;
                        }
                    }
                    else if (at == ActorTypes.Prim)
                    {
                        //d.AABB aabb1 = new d.AABB();
                        //d.AABB aabb2 = new d.AABB();

                        //d.GeomGetAABB(contactGeom.g2, out aabb2);
                        //d.GeomGetAABB(contactGeom.g1, out aabb1);
                        //aabb1.
                        if (((Math.Abs(contactGeom.normal.X - contact.normal.X) < 1.026f) && (Math.Abs(contactGeom.normal.Y - contact.normal.Y) < 0.303f) && (Math.Abs(contactGeom.normal.Z - contact.normal.Z) < 0.065f)))
                        {
                            if (contactGeom.normal.X == contact.normal.X && contactGeom.normal.Y == contact.normal.Y && contactGeom.normal.Z == contact.normal.Z)
                            {
                                if (Math.Abs(contact.depth - contactGeom.depth) < 0.272f)
                                {
                                    result = true;
                                    break;
                                }
                            }
                            //m_log.DebugFormat("[Collsion]: Depth {0}", Math.Abs(contact.depth - contactGeom.depth));
                            //m_log.DebugFormat("[Collision]: <{0},{1},{2}>", Math.Abs(contactGeom.normal.X - contact.normal.X), Math.Abs(contactGeom.normal.Y - contact.normal.Y), Math.Abs(contactGeom.normal.Z - contact.normal.Z));
                        }

                    }

                    //}

                }
            }

            //watch.Stop();
            //m_log.Warn(watch.ElapsedMilliseconds);
            return result;
        }

        private void collision_accounting_events(PhysicsActor p1, PhysicsActor p2, ContactPoint contact)
        {
            if (!p2.SubscribedEvents() && !p1.SubscribedEvents())
                return;
            FireCollisionEvent (p1, p2, contact);

            p1.AddCollisionEvent (p2.LocalID, contact);
            p2.AddCollisionEvent (p1.LocalID, contact);
        }

        public int TriCallback(IntPtr trimesh, IntPtr refObject, int triangleIndex)
        {
            d.Vector3 v0 = new d.Vector3();
            d.Vector3 v1 = new d.Vector3();
            d.Vector3 v2 = new d.Vector3();

            d.GeomTriMeshGetTriangle(trimesh, 0, ref v0, ref v1, ref v2);

            return 1;
        }

        /// <summary>
        /// This is our collision testing routine in ODE
        /// </summary>
        /// <param name="timeStep"></param>
        private void collision_optimized(float timeStep)
        {
            if (m_filterCollisions)
                _perloopContact.Clear();

            if (m_EnableAutoConfig)
            {
                if (Math.Abs ((m_timeDilation * contactsPerCollision - contacts.Length)) > 10)
                {
                    //This'll cause weird physics inworld
                    //m_currentmaxContactsbeforedeath = Math.Max(100, (int)(maxContactsbeforedeath * TimeDilation));
                    contacts = new d.ContactGeom[Math.Max (5, (int)(m_timeDilation * contactsPerCollision))];
                    m_log.DebugFormat ("[ODE]: AutoConfig: changing contact amount to {0}, {1}%", contacts.Length, (m_timeDilation * contactsPerCollision) / contactsPerCollision * 100);
                }
                else if(contactsPerCollision - contacts.Length < 10 &&
                    contacts.Length != contactsPerCollision)
                {
                    contacts = new d.ContactGeom[contactsPerCollision];
                }
            }

            lock (_characters)
            {
                foreach (AuroraODECharacter chr in _characters)
                {
                    // Reset the collision values to false
                    // since we don't know if we're colliding yet

                    // For some reason this can happen. Don't ask...
                    //
                    if (chr == null)
                        continue;

                    if (chr.Shell == IntPtr.Zero || chr.Body == IntPtr.Zero)
                        continue;

                    chr.IsColliding = false;

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
                        m_log.Warn("[PHYSICS]: Unable to space collide");
                    }
                }
            }

            lock (_activeprimsLock)
            {
                List<AuroraPhysXPrim> removeprims = null;
                foreach (AuroraPhysXPrim chr in _activeprims)
                {
                    if (chr.Body != IntPtr.Zero && d.BodyIsEnabled(chr.Body) &&
                        (!chr.m_disabled) && (!chr.m_frozen) && !(chr._zeroFlag))
                    {
                        //Fix colliding atributes!
                        chr.IsColliding = false;

                        try
                        {
                            lock (chr)
                            {
                                if (space != IntPtr.Zero && chr.prim_geom != IntPtr.Zero)
                                {
                                    d.SpaceCollide2(space, chr.prim_geom, IntPtr.Zero, nearCallback);
                                }
                                else
                                {
                                    if (removeprims == null)
                                        removeprims = new List<AuroraPhysXPrim>();
                                    removeprims.Add(chr);
                                    m_log.Debug("[PHYSICS]: unable to collide test active prim against space.  The space was zero, the geom was zero or it was in the process of being removed.  Removed it from the active prim list.  This needs to be fixed!");
                                }
                            }
                        }
                        catch (AccessViolationException)
                        {
                            m_log.Warn("[PHYSICS]: Unable to space collide");
                        }
                    }
                    else if (chr.m_frozen)
                    {
                        if (removeprims == null)
                            removeprims = new List<AuroraPhysXPrim>();
                        removeprims.Add(chr);
                    }
                }
                if (removeprims != null)
                {
                    foreach (AuroraPhysXPrim chr in removeprims)
                    {
                        _activeprims.Remove(chr);
                    }
                }
            }

            if (m_filterCollisions)
                _perloopContact.Clear();
        }

        public bool CheckTerrainColisionAABB (IntPtr geom)
        {

            // assumes 1m terrain resolution

            if (geom == IntPtr.Zero || TerrainHeightFieldHeights.Count == 0)
                return false;

            d.Vector3 pos;

            pos = d.GeomGetPosition (geom);


            // megas thing
            int offsetX = ((int)(pos.X / m_region.RegionSizeX)) * m_region.RegionSizeX;
            int offsetY = ((int)(pos.Y / m_region.RegionSizeY)) * m_region.RegionSizeY;
            if (RegionTerrain == IntPtr.Zero)
                return false;

            pos.X -= offsetX;
            pos.Y -= offsetY;

            if (pos.X < 0 || pos.Y < 0)
                return false;
            if (pos.X > m_region.RegionSizeX || pos.Y > m_region.RegionSizeY)
                return false;

            d.AABB aabb;

            d.GeomGetAABB (geom, out aabb);

            int minx, maxx, miny, maxy;

            if (aabb.MaxZ < TerrainHeightFieldlimits[RegionTerrain][0])
                return true;
            if (aabb.MinZ > TerrainHeightFieldlimits[RegionTerrain][1])
                return false;

            minx = (int)(aabb.MinX - offsetX);
            miny = (int)(aabb.MinY - offsetY);
            maxx = (int)(aabb.MaxX - offsetX);
            maxy = (int)(aabb.MaxY - offsetY);

            if (minx < 0)
                minx = 0;
            if (miny < 0)
                miny = 0;

            if (maxx > m_region.RegionSizeX)
                maxx = (int)m_region.RegionSizeX;
            if (maxy > m_region.RegionSizeY)
                maxy = (int)m_region.RegionSizeY;

            int i;
            int j;
            float minh = aabb.MinZ;

            int centerx = (minx + maxx) / 2;
            int centery = (miny + maxy) / 2;

            // assumes region size is integer
            centery *= Region.RegionSizeX;
            maxy *= Region.RegionSizeX;

            // start near center of aabb
            for (j = centery; j < maxy; j += Region.RegionSizeX)
            {
                for (i = centerx; i < maxx; i++)
                {
                    if (TerrainHeightFieldHeights[RegionTerrain][j + i] >= minh)
                        return true;
                }
                i = minx;
                while (i < centerx)
                {
                    if (TerrainHeightFieldHeights[RegionTerrain][j + i] >= minh)
                        return true;
                    i++;
                }
            }

            j = miny * Region.RegionSizeX;
            while (j < centery)
            {
                for (i = minx; i < maxx; i++)
                {
                    if (TerrainHeightFieldHeights[RegionTerrain][j + i] >= minh)
                        return true;
                }
                j += Region.RegionSizeX;
            }

            return false;
        }

        // Recovered for use by fly height. Kitto Flora
        public float GetTerrainHeightAtXY (float x, float y)
        {
            return m_channel[(int)x, (int)y];
        }
        // End recovered. Kitto Flora

        public void addCollisionEventReporting(PhysicsActor obj)
        {
            if (IsLocked)
            {
                ActiveAddCollisionQueue.Add(obj);
            }
            else
            {
                lock (_collisionEventListLock)
                {
                    if (!_collisionEventPrimDictionary.ContainsKey (obj.UUID))
                    {
                        _collisionEventPrimDictionary.Add (obj.UUID, obj);
                        _collisionEventPrimList.Add (obj);
                    }
                }
            }
        }

        public void remCollisionEventReporting(PhysicsActor obj)
        {
            if (IsLocked)
            {
                ActiveRemoveCollisionQueue.Add(obj);
            }
            else
            {
                lock (_collisionEventListLock)
                {
                    _collisionEventPrimList.Remove (obj);
                    _collisionEventPrimDictionary.Remove (obj.UUID);
                }
            }
        }

        #endregion

        #region Add/Remove Entities

        public override PhysicsCharacter AddAvatar(string avName, Vector3 position, Quaternion rotation, Vector3 size, bool isFlying, uint localID, UUID UUID)
        {
            Vector3 pos;
            pos.X = position.X;
            pos.Y = position.Y;
            pos.Z = position.Z;
            AuroraODECharacter newAv = new AuroraODECharacter(avName, this, pos, rotation, size);
            newAv.LocalID = localID;
            newAv.UUID = UUID;
            newAv.Flying = isFlying;
            newAv.MinimumGroundFlightOffset = minimumGroundFlightOffset;

            return newAv;
        }

        public void AddCharacter(AuroraODECharacter chr)
        {
            lock (_characters)
            {
                if (!_characters.Contains(chr))
                {
                    if (!chr.bad)
                        _characters.Add(chr);
                    else
                        m_log.DebugFormat("[PHYSICS] Did not add BAD actor {0} to characters list", chr.m_uuid);
                }
            }
        }

        public void RemoveCharacter(AuroraODECharacter chr)
        {
            lock (_characters)
            {
                _characters.Remove(chr);
            }
        }

        public void BadCharacter(AuroraODECharacter chr)
        {
            lock (_badCharacter)
            {
                if (!_badCharacter.Contains(chr))
                    _badCharacter.Add(chr);
            }
        }

        public override void RemoveAvatar(PhysicsCharacter actor)
        {
            //m_log.Debug("[PHYSICS]:ODELOCK");
            ((AuroraODECharacter)actor).Destroy();

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
            AuroraPhysXPrim newPrim;
            newPrim = new AuroraPhysXPrim (entity, this, false, ode);

            if(physical)
                newPrim.IsPhysical = physical;

            lock (_prims)
                _prims.Add(newPrim);

            return newPrim;
        }

        public void addActivePrim(AuroraPhysXPrim activatePrim)
        {
            // adds active prim..   (ones that should be iterated over in collisions_optimized
            lock (_activeprimsLock)
            {
                if (!_activeprims.Contains(activatePrim))
                    _activeprims.Add(activatePrim);
                //else
                //  m_log.Warn("[PHYSICS]: Double Entry in _activeprims detected, potential crash immenent");
            }
        }

        public override float TimeDilation
        {
            get { return m_timeDilation; }
            set { m_timeDilation = value; }
        }

        public void remActivePrim(AuroraPhysXPrim deactivatePrim)
        {
            lock (_activeprimsLock)
            {
                _activeprims.Remove(deactivatePrim);
            }
        }

        public override void RemovePrim(PhysicsObject prim)
        {
            if (prim is AuroraPhysXPrim)
            {
                if (!IsLocked) //Fix a deadlock situation.. have we been locked by Simulate?
                {
                    lock (OdeLock)
                    {
                        AuroraPhysXPrim p = (AuroraPhysXPrim)prim;

                        p.setPrimForRemoval();
                        AddPhysicsActorTaint(prim);
                        //RemovePrimThreadLocked(p);
                    }
                }
                else
                {
                    //Add the prim to a queue which will be removed when Simulate has finished what it's doing.
                    RemoveQueue.Add(prim);
                }
            }
        }

        /// <summary>
        /// This is called from within simulate but outside the locked portion
        /// We need to do our own locking here
        /// Essentially, we need to remove the prim from our space segment, whatever segment it's in.
        ///
        /// If there are no more prim in the segment, we need to empty (spacedestroy)the segment and reclaim memory
        /// that the space was using.
        /// </summary>
        /// <param name="prim"></param>
        public void RemovePrimThreadLocked(AuroraPhysXPrim prim)
        {
            //Console.WriteLine("RemovePrimThreadLocked " +  prim.m_primName);
            lock (prim)
            {
                remCollisionEventReporting(prim);
                remActivePrim(prim);
                lock (ode)
                {
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
                                m_log.Warn("[PHYSICS]: Unable to remove prim from physics scene");
                            }
                        }
                        catch (AccessViolationException)
                        {
                            m_log.Info("[PHYSICS]: Couldn't remove prim from physics scene, it was already be removed.");
                        }
                    }
                    if (!prim.childPrim)
                    {
                        lock (prim.childrenPrim)
                        {
                            foreach (AuroraPhysXPrim prm in prim.childrenPrim)
                            {
                                RemovePrimThreadLocked (prm);
                            }
                        }
                    }
                    lock (_prims)
                        _prims.Remove (prim);
                }
            }
        }

        #endregion

        #region Space Separation Calculation

        /// <summary>
        /// Takes a space pointer and zeros out the array we're using to hold the spaces
        /// </summary>
        /// <param name="pSpace"></param>
        public void resetSpaceArrayItemToZero(IntPtr pSpace)
        {
            for (int x = 0; x < staticPrimspace.GetLength(0); x++)
            {
                for (int y = 0; y < staticPrimspace.GetLength(1); y++)
                {
                    if (staticPrimspace[x, y] == pSpace)
                        staticPrimspace[x, y] = IntPtr.Zero;
                }
            }
        }

        public void resetSpaceArrayItemToZero(int arrayitemX, int arrayitemY)
        {
            staticPrimspace[arrayitemX, arrayitemY] = IntPtr.Zero;
        }

        /// <summary>
        /// Called when a static prim moves.  Allocates a space for the prim based on its position
        /// </summary>
        /// <param name="geom">the pointer to the geom that moved</param>
        /// <param name="pos">the position that the geom moved to</param>
        /// <param name="currentspace">a pointer to the space it was in before it was moved.</param>
        /// <returns>a pointer to the new space it's in</returns>
        public IntPtr recalculateSpaceForGeom(IntPtr geom, Vector3 pos, IntPtr currentspace)
        {
            // Called from setting the Position and Size of an PhysXPrim so
            // it's already in locked space.

            // we don't want to remove the main space
            // we don't need to test physical here because this function should
            // never be called if the prim is physical(active)

            // All physical prim end up in the root space
            //Thread.Sleep(20);
            if (currentspace != space)
            {
                //m_log.Info("[SPACE]: C:" + currentspace.ToString() + " g:" + geom.ToString());
                //if (currentspace == IntPtr.Zero)
                //{
                //int adfadf = 0;
                //}
                if (d.SpaceQuery(currentspace, geom) && currentspace != IntPtr.Zero)
                {
                    if (d.GeomIsSpace(currentspace))
                    {
                        waitForSpaceUnlock(currentspace);
                        d.SpaceRemove(currentspace, geom);
                    }
                    else
                    {
                        m_log.Info("[Physics]: Invalid Scene passed to 'recalculatespace':" + currentspace +
                                   " Geom:" + geom);
                    }
                }
                else
                {
                    IntPtr sGeomIsIn = d.GeomGetSpace(geom);
                    if (sGeomIsIn != IntPtr.Zero)
                    {
                        if (d.GeomIsSpace(currentspace))
                        {
                            waitForSpaceUnlock(sGeomIsIn);
                            d.SpaceRemove(sGeomIsIn, geom);
                        }
                        else
                        {
                            m_log.Info("[Physics]: Invalid Scene passed to 'recalculatespace':" +
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
                            m_log.Info("[Physics]: Invalid Scene passed to 'recalculatespace':" +
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
                        {
                            waitForSpaceUnlock(currentspace);
                            d.SpaceRemove(currentspace, geom);
                        }
                        else
                        {
                            m_log.Info("[Physics]: Invalid Scene passed to 'recalculatespace':" +
                                       currentspace + " Geom:" + geom);
                        }
                    }
                    else
                    {
                        IntPtr sGeomIsIn = d.GeomGetSpace(geom);
                        if (sGeomIsIn != IntPtr.Zero)
                        {
                            if (d.GeomIsSpace(sGeomIsIn))
                            {
                                waitForSpaceUnlock(sGeomIsIn);
                                d.SpaceRemove(sGeomIsIn, geom);
                            }
                            else
                            {
                                m_log.Info("[Physics]: Invalid Scene passed to 'recalculatespace':" +
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
        /// Creates a new space at X Y
        /// </summary>
        /// <param name="iprimspaceArrItemX"></param>
        /// <param name="iprimspaceArrItemY"></param>
        /// <returns>A pointer to the created space</returns>
/* not in use ( and is wrong)
        public IntPtr createprimspace(int iprimspaceArrItemX, int iprimspaceArrItemY)
        {
            // creating a new space for prim and inserting it into main space.
            staticPrimspace[iprimspaceArrItemX, iprimspaceArrItemY] = d.HashSpaceCreate(IntPtr.Zero);
            d.GeomSetCategoryBits(staticPrimspace[iprimspaceArrItemX, iprimspaceArrItemY], (int)CollisionCategories.Space);
            waitForSpaceUnlock(space);
            d.SpaceSetSublevel(space, 1);
            d.SpaceAdd(space, staticPrimspace[iprimspaceArrItemX, iprimspaceArrItemY]);
            return staticPrimspace[iprimspaceArrItemX, iprimspaceArrItemY];
        }
*/
        /// <summary>
        /// Calculates the space the prim should be in by its position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>a pointer to the space. This could be a new space or reused space.</returns>
        public IntPtr calculateSpaceForGeom(Vector3 pos)
        {
            int[] xyspace = calculateSpaceArrayItemFromPos(pos);
            //m_log.Info("[Physics]: Attempting to use arrayItem: " + xyspace[0].ToString() + "," + xyspace[1].ToString());
            return staticPrimspace[xyspace[0], xyspace[1]];
        }

        /// <summary>
        /// Holds the space allocation logic
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>an array item based on the position</returns>
        public int[] calculateSpaceArrayItemFromPos(Vector3 pos)
        {
            int[] returnint = new int[2];

            returnint[0] = (int)(pos.X) >> GridSpaceScaleBits;

            if (returnint[0] >= nspacesPerSideX)
                returnint[0] = nspacesPerSideX -1;
            if (returnint[0] < 0)
                returnint[0] = 0;

            returnint[1] = (int)(pos.Y) >> GridSpaceScaleBits;
            if (returnint[1] >= nspacesPerSideY)
                returnint[1] = nspacesPerSideY-1;
            if (returnint[1] < 0)
                returnint[1] = 0;

            return returnint;
        }

        internal void waitForSpaceUnlock (IntPtr space)
        {
            //if (space != IntPtr.Zero)
            //while (d.SpaceLockQuery(space)) { } // Wait and do nothing
        }

        /// <summary>
        /// Debug space message for printing the space that a prim/avatar is in.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>Returns which split up space the given position is in.</returns>
        public string whichspaceamIin (Vector3 pos)
        {
            return calculateSpaceForGeom (pos).ToString ();
        }

        #endregion

        #region Meshing

        /// <summary>
        /// Routine to figure out if we need to mesh this prim with our mesher
        /// </summary>
        /// <param name="pbs"></param>
        /// <returns></returns>
        public bool needsMeshing(PrimitiveBaseShape pbs)
        {
            // most of this is redundant now as the mesher will return null if it cant mesh a prim
            // but we still need to check for sculptie meshing being enabled so this is the most
            // convenient place to do it for now...

            //    //if (pbs.PathCurve == (byte)Primitive.PathCurve.Circle && pbs.ProfileCurve == (byte)Primitive.ProfileCurve.Circle && pbs.PathScaleY <= 0.75f)
            //    //m_log.Debug("needsMeshing: " + " pathCurve: " + pbs.PathCurve.ToString() + " profileCurve: " + pbs.ProfileCurve.ToString() + " pathScaleY: " + Primitive.UnpackPathScale(pbs.PathScaleY).ToString());
            int iPropertiesNotSupportedDefault = 0;

//            return true;

            if (forceSimplePrimMeshing)
                return true;
            // let simple spheres use ode sphere object
            if(pbs.ProfileShape == ProfileShape.HalfCircle && pbs.PathCurve == (byte)Extrusion.Curve1
                    && pbs.Scale.X == pbs.Scale.Y && pbs.Scale.X == pbs.Scale.Z && pbs.ProfileHollow == 0)
                return false;

            if (pbs.SculptEntry && !meshSculptedPrim)
            {
#if SPAM
                m_log.Warn("NonMesh");
#endif
                return false;
            }
            else if (pbs.SculptType != (byte)SculptType.None)
                return true;

            // if it's a standard box or sphere with no cuts, hollows, twist or top shear, return false since ODE can use an internal representation for the prim
            if (!forceSimplePrimMeshing)
            {
                if ((pbs.ProfileShape == ProfileShape.Square && pbs.PathCurve == (byte)Extrusion.Straight)
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
                    m_log.Warn("NonMesh");
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
            else if (pbs.ProfileShape == ProfileShape.Circle && pbs.PathCurve == (byte)Extrusion.Straight)
                iPropertiesNotSupportedDefault++;
            else if (pbs.ProfileShape == ProfileShape.HalfCircle && pbs.PathCurve == (byte)Extrusion.Curve1 && (pbs.Scale.X != pbs.Scale.Y || pbs.Scale.Y != pbs.Scale.Z || pbs.Scale.Z != pbs.Scale.X))
                iPropertiesNotSupportedDefault++;
            else if (pbs.ProfileShape == ProfileShape.HalfCircle && pbs.PathCurve == (byte)Extrusion.Curve1)
                iPropertiesNotSupportedDefault++;
            // test for torus
            else if ((pbs.ProfileCurve & 0x07) == (byte)ProfileShape.Square && pbs.PathCurve == (byte)Extrusion.Curve1)
                    iPropertiesNotSupportedDefault++;
            else if ((pbs.ProfileCurve & 0x07) == (byte)ProfileShape.HalfCircle && 
                (pbs.PathCurve == (byte)Extrusion.Curve1 || pbs.PathCurve == (byte)Extrusion.Curve2))
                    iPropertiesNotSupportedDefault++;
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
            if ((pbs.ProfileCurve & 0x07) == (byte)ProfileShape.Circle)
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


            if (iPropertiesNotSupportedDefault == 0)
            {
#if SPAM
                m_log.Warn("NonMesh");
#endif
                return false;
            }
#if SPAM
            m_log.Debug("Mesh");
#endif
            return true;
        }

        #endregion

        #region Changes/Tainting

        /// <summary>
        /// Called to queue a change to a prim
        /// to use in place of old taint mechanism so changes do have a time sequence
        /// </summary>
        public void AddChange(AuroraPhysXPrim prim,changes what,Object arg)
            {
            AODEchangeitem item = new AODEchangeitem();
            item.prim = prim;
            item.what = what;
            item.arg = arg;
//            lock (ChangesQueue)
                {
                ChangesQueue.Enqueue(item);
                }
            }

        private bool GetNextChange(out AODEchangeitem item)
            {
//            lock (ChangesQueue)
                {
                return ChangesQueue.Dequeue(out item);
                }
            }

        /// <summary>
        /// Called after our prim properties are set Scale, position etc.
        /// We use this event queue like method to keep changes to the physical scene occuring in the threadlocked mutex
        /// This assures us that we have no race conditions
        /// </summary>
        /// <param name="prim"></param>
        public override void AddPhysicsActorTaint(PhysicsActor prim)
        {
            if (prim is AuroraPhysXPrim)
            {
/* ignore taints for prims
                AuroraPhysXPrim taintedprim = ((AuroraPhysXPrim)prim);
                lock (_taintedPrimLock)
                {
                    if (!(_taintedPrimH.Contains(taintedprim)))
                    {
                        //Console.WriteLine("AddPhysicsActorTaint to " +  taintedprim.m_primName);
                        _taintedPrimH.Add(taintedprim);                    // HashSet for searching
                        _taintedPrimL.Add(taintedprim);                    // List for ordered readout
                    }
                }
 */
                return;
            }
            else if (prim is AuroraODECharacter)
            {
                AuroraODECharacter taintedchar = ((AuroraODECharacter)prim);
                lock (_taintedActors)
                {
                    if (!(_taintedActors.Contains(taintedchar)))
                    {
                        _taintedActors.Add(taintedchar);
                        if (taintedchar.bad)
                            m_log.DebugFormat("[PHYSICS]: Added BAD actor {0} to tainted actors", taintedchar.m_uuid);
                    }
                }
            }
        }

        #endregion

        #region Simulation Loop

        /// <summary>
        /// This is our main simulate loop
        /// It's thread locked by a Mutex in the scene.
        /// It holds Collisions, it instructs ODE to step through the physical reactions
        /// It moves the objects around in memory
        /// It calls the methods that report back to the object owners.. (scenepresence, SceneObjectGroup)
        /// </summary>
        /// <param name="timeStep"></param>
        /// <returns></returns>
        public override float Simulate(float timeElapsed)
        {
            if (framecount >= int.MaxValue)
                framecount = 0;

            //if (m_worldOffset != Vector3.Zero)
            //    return 0;

            framecount++;

            float fps = 0;
            //m_log.Info(timeStep.ToString());
            step_time += timeElapsed;

            // If We're loaded down by something else,
            // or debugging with the Visual Studio project on pause
            // skip a few frames to catch up gracefully.
            // without shooting the physicsactors all over the place

            // Process 10 frames if the sim is running normal..
            // process 5 frames if the sim is running slow
            /*if (m_EnableAutoConfig && TimeDilation < 0.75f && m_physicsiterations > 5)
            {
                //m_log.Warn("[ODE]: Sim is lagging, changing to half resolution");
                // Instead of trying to catch up, it'll do 5 physics frames only
                step_time = ODE_STEPSIZE;
                m_physicsiterations = 5;
                m_timeBetweenRevertingAutoConfigIterations = 50;
                try
                {
                    d.WorldSetQuickStepNumIterations(world, m_physicsiterations);
                }
                catch (StackOverflowException)
                {
                    m_log.Error("[PHYSICS]: The operating system wasn't able to allocate enough memory for the simulation.  Restarting the sim.");
                    ode.drelease(world);
                }
            }
            else if (m_EnableAutoConfig && TimeDilation > 0.75f && m_physicsiterations < 10 &&
                m_timeBetweenRevertingAutoConfigIterations == 0) //Wait for 50 frames before doing the next step down
            {
                //m_log.Warn("[ODE]: Sim is not lagging, changing to full resolution");
                step_time = ODE_STEPSIZE;
                m_physicsiterations++;
                m_timeBetweenRevertingAutoConfigIterations = 50;
                try
                {
                    d.WorldSetQuickStepNumIterations(world, m_physicsiterations);
                }
                catch (StackOverflowException)
                {
                    m_log.Error("[PHYSICS]: The operating system wasn't able to allocate enough memory for the simulation.  Restarting the sim.");
                    ode.drelease(world);
                }
            }
            if (m_physicsiterations != 10 && TimeDilation > 0.75f)
                m_timeBetweenRevertingAutoConfigIterations--; //Subtract for the next time*/

            IsLocked = true;
            lock (OdeLock)
            {
                int nodesteps = 0;

                // Figure out the Frames Per Second we're going at.
                fps = (step_time / ODE_STEPSIZE) * 1000;

                if (step_time > 0.5f)
                    step_time = 0.5f; //Don't get ODE stuck in an eternal processing loop with huge step times

                while (step_time > 0.0f && nodesteps < 10)
                {
                    try
                    {
                        int PhysicsTaintTime = Util.EnvironmentTickCount();

                        // Insert, remove Characters
                        lock (_taintedActors)
                        {
                            if (_taintedActors.Count > 0)
                            {
                                bool processedtaints = false;
                                foreach (AuroraODECharacter character in _taintedActors)
                                {
                                    character.ProcessTaints(timeElapsed);
                                    processedtaints = true;
                                }

                                if (processedtaints)
                                    _taintedActors.Clear();
                            }
                        }

                        int tlimit = 5000;
                        AODEchangeitem item;
                        while (GetNextChange(out item))
                        {
                            if (item.prim != null)
                            {
                                try
                                {
                                    if (item.prim.DoAChange(item.what, item.arg))
                                        RemovePrimThreadLocked(item.prim);
                                }
                                catch { };
                            }
                            if (tlimit-- <= 0)
                                break;
                        }

                        if (ChangesQueue.Count == 0 && !m_hasSetUpPrims)
                        {
                            //Tell the mesher that we are done with the initialization 
                            //  of prim meshes and that it can clear it's in memory cache
                            m_hasSetUpPrims = true;
                            mesher.FinishedMeshing ();
                        }
                        else if (!m_hasSetUpPrims)
                            return fps;//Don't do physics until the sim is completely set up

                        m_StatPhysicsTaintTime = Util.EnvironmentTickCountSubtract(PhysicsTaintTime);

                        int PhysicsMoveTime = Util.EnvironmentTickCount();

                        if (!DisableCollisions)
                        {
                            // Move characters
                            lock (_characters)
                            {
                                List<AuroraODECharacter> defects = new List<AuroraODECharacter>();
                                foreach (AuroraODECharacter actor in _characters)
                                {
                                    if (actor != null)
                                        actor.Move(ODE_STEPSIZE, ref defects);
                                }
                                if (0 != defects.Count)
                                {
                                    foreach (AuroraODECharacter defect in defects)
                                    {
                                        defect.Destroy();
                                        RemoveCharacter (defect);
                                        AddAvatar (defect.Name, new Vector3 (m_region.RegionSizeX / 2,
                                            m_region.RegionSizeY / 2,
                                            m_region.RegionSizeZ / 2), defect.Orientation, defect.Size, true, defect.LocalID, defect.UUID);
                                    }
                                }
                            }

                            // Move other active objects
                            lock (_activeprimsLock)
                            {
                                List<AuroraPhysXPrim> defects = new List<AuroraPhysXPrim> ();
                                foreach (AuroraPhysXPrim prim in _activeprims)
                                {
                                    prim.m_collisionscore = 0;
                                    prim.Move(ODE_STEPSIZE, ref defects);
                                }
                                if (defects.Count > 0)
                                {
                                    foreach (AuroraPhysXPrim defect in defects)
                                    {
                                        foreach (ISceneChildEntity child in defect.ParentEntity.ParentEntity.ChildrenEntities ())
                                        {
                                            if (child.PhysActor != null)
                                            {
                                                RemovePrimThreadLocked ((AuroraPhysXPrim)child.PhysActor);
                                                child.PhysActor = null;//Delete it
                                            }
                                        }
                                        //Destroy it
                                        RemovePrimThreadLocked (defect);
                                        defect.ParentEntity.PhysActor = null;//Delete it
                                    }
                                }
                            }
                        }
                        if (m_rayCastManager != null)
                            m_rayCastManager.ProcessQueuedRequests ();

                        m_StatPhysicsMoveTime = Util.EnvironmentTickCountSubtract(PhysicsMoveTime);


                        int CollisionOptimizedTime = Util.EnvironmentTickCount();
                        collision_optimized(timeElapsed);
                        m_StatCollisionOptimizedTime = Util.EnvironmentTickCountSubtract(CollisionOptimizedTime);


                        int SendCollisionsTime = Util.EnvironmentTickCount();
                        if (!DisableCollisions)
                        {
                            lock (_collisionEventListLock)
                            {
                                foreach (PhysicsActor obj in _collisionEventPrimList)
                                {
                                    if (obj == null)
                                        continue;

                                    obj.SendCollisions();
                                }
                            }
                        }
                        m_StatSendCollisionsTime = Util.EnvironmentTickCountSubtract(SendCollisionsTime);

                        m_global_contactcount = 0;
                        d.WorldQuickStep (world, ODE_STEPSIZE);
                        d.JointGroupEmpty(contactgroup);
                        //ode.dunlock(world);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat ("[PHYSICS]: {0}, {1}, {2}", e.ToString (), e.TargetSite, e);
                        ode.dunlock (world);
                    }

                    step_time -= ODE_STEPSIZE;
                    nodesteps++;
                }
                int AvatarUpdatePosAndVelocity = Util.EnvironmentTickCount();

                if (!DisableCollisions)
                {
                    lock (_characters)
                    {
                        foreach (AuroraODECharacter actor in _characters)
                        {
                            if (actor != null)
                            {
                                if (actor.bad)
                                    m_log.WarnFormat("[PHYSICS]: BAD Actor {0} in _characters list was not removed?", actor.m_uuid);
                                else
                                    actor.UpdatePositionAndVelocity(nodesteps * ODE_STEPSIZE);
                            }
                        }
                    }
                }
                lock (_badCharacter)
                {
                    if (_badCharacter.Count > 0)
                    {
                        foreach (AuroraODECharacter chr in _badCharacter)
                        {
                            RemoveCharacter(chr);
                        }
                        _badCharacter.Clear();
                    }
                }

                m_StatAvatarUpdatePosAndVelocity = Util.EnvironmentTickCountSubtract(AvatarUpdatePosAndVelocity);

                int PrimUpdatePosAndVelocity = Util.EnvironmentTickCount();

                if (!DisableCollisions)
                {
                    lock (_activeprimsLock)
                    {
                        foreach (AuroraPhysXPrim actor in _activeprims)
                        {
                            if (actor.IsPhysical)
                            {
                                actor.UpdatePositionAndVelocity(nodesteps * ODE_STEPSIZE);
                            }
                        }
                    }
                }

                m_StatPrimUpdatePosAndVelocity = Util.EnvironmentTickCountSubtract(PrimUpdatePosAndVelocity);

                // Finished with all sim stepping. If requested, dump world state to file for debugging.
                // This overwrites all dump files in-place. Should this be a growing logfile, or separate snapshots?
                if (physics_logging && (physics_logging_interval > 0) && (framecount % physics_logging_interval == 0))
                {
                    string fname = "state-" + world.ToString() + ".DIF"; // give each physics world a separate filename
                    string prefix = "world" + world.ToString(); // prefix for variable names in exported .DIF file

                    if (physics_logging_append_existing_logfile)
                    {
                        string header = "-------------- START OF PHYSICS FRAME " + framecount.ToString() + " --------------";
                        TextWriter fwriter = File.AppendText(fname);
                        fwriter.WriteLine(header);
                        fwriter.Close();
                    }
                    d.WorldExportDIF(world, fname, physics_logging_append_existing_logfile, prefix);
                }
                IsLocked = false;
            }

            int UnlockedArea = Util.EnvironmentTickCount();

            if (RemoveQueue.Count > 0)
            {
                while (RemoveQueue.Count != 0)
                {
                    if (RemoveQueue[0] != null)
                    {
                        AuroraPhysXPrim p = (AuroraPhysXPrim)RemoveQueue[0];

                        p.setPrimForRemoval();
                        AddPhysicsActorTaint(p);
                    }
                    RemoveQueue.RemoveAt(0);
                }
            }
            if (ActiveAddCollisionQueue.Count > 0)
            {
                lock (_collisionEventListLock)
                {
                    foreach (PhysicsActor obj in ActiveAddCollisionQueue)
                    {
                        //add
                        if (!_collisionEventPrimDictionary.ContainsKey (obj.UUID))
                        {
                            _collisionEventPrimDictionary.Add (obj.UUID, obj);
                            _collisionEventPrimList.Add (obj);
                        }
                    }
                }
                ActiveAddCollisionQueue.Clear();
            }
            if (ActiveRemoveCollisionQueue.Count > 0)
            {
                lock (_collisionEventListLock)
                {
                    foreach (PhysicsActor obj in ActiveRemoveCollisionQueue)
                    {
                        //remove
                        _collisionEventPrimDictionary.Remove (obj.UUID);
                        _collisionEventPrimList.Remove (obj);
                    }
                }
                ActiveRemoveCollisionQueue.Clear();
            }

            m_StatUnlockedArea = Util.EnvironmentTickCountSubtract(UnlockedArea);

            return fps;
        }

        #endregion

        #region Get/Set Terrain and water

        public override void SetTerrain (ITerrainChannel channel, short[] heightMap)
        {
            m_channel = channel;
            bool needToCreateHeightmapinODE = false;
            short[] _heightmap;
            if (!ODETerrainHeightFieldHeights.TryGetValue (RegionTerrain, out _heightmap))
            {
                needToCreateHeightmapinODE = true;//We don't have any terrain yet, we need to generate one
                _heightmap = new short[((m_region.RegionSizeX + 3) * (m_region.RegionSizeY + 3))];
            }

            int heightmapWidth = m_region.RegionSizeX + 2;
            int heightmapHeight = m_region.RegionSizeY + 2;

            int heightmapWidthSamples = m_region.RegionSizeX + 3; // + one to complete the 256m + 2 margins each side
            int heightmapHeightSamples = m_region.RegionSizeY + 3;

            float hfmin = 2000;
            float hfmax = -2000;

            for (int x = 0; x < heightmapWidthSamples; x++)
            {
                for (int y = 0; y < heightmapHeightSamples; y++)
                {
                    //Some notes on this part
                    //xx and yy are used for the original heightmap, as we are offsetting the new one by 1
                    // so we subtract one so that we can put the heightmap in correctly
                    int xx = x - 1;
                    if(xx < 0)
                        xx = 0;
                    if(xx > m_region.RegionSizeX - 1)
                        xx = m_region.RegionSizeX - 1;
                    int yy = y - 1;
                    if (yy < 0)
                        yy = 0;
                    if (yy > m_region.RegionSizeY - 1)
                        yy = m_region.RegionSizeY - 1;

                    short val = heightMap[yy * m_region.RegionSizeX + xx];
                    //ODE is evil... flip x and y
                    _heightmap[(x * heightmapHeightSamples) + y] = val;

                    hfmin = (val < hfmin) ? val : hfmin;
                    hfmax = (val > hfmax) ? val : hfmax;
                }
            }

            needToCreateHeightmapinODE = true;//ODE seems to have issues with not rebuilding :(
            TerrainHeightFieldHeights.Remove (RegionTerrain);
            TerrainHeightFieldlimits.Remove (RegionTerrain);
            ODETerrainHeightFieldHeights.Remove (RegionTerrain);
            if (RegionTerrain != IntPtr.Zero)
            {
                d.SpaceRemove (space, RegionTerrain);
                d.GeomDestroy (RegionTerrain);
            }
            if (!needToCreateHeightmapinODE)
            {
                TerrainHeightFieldHeights.Remove (RegionTerrain);
                TerrainHeightFieldlimits.Remove (RegionTerrain);
                ODETerrainHeightFieldHeights.Remove (RegionTerrain);
                float[] heighlimits = new float[2];
                heighlimits[0] = hfmin;
                heighlimits[1] = hfmax;
                TerrainHeightFieldHeights.Add (RegionTerrain, heightMap);
                TerrainHeightFieldlimits.Add (RegionTerrain, heighlimits);
                ODETerrainHeightFieldHeights.Add (RegionTerrain, _heightmap);
                return;//If we have already done this once, we don't need to do it again
            }
            lock (OdeLock)
            {
                const float scale = (1f / (float)Constants.TerrainCompression);
                const float offset = 0.0f;
                float thickness = 0.01f;
                const int wrap = 0;

                IntPtr HeightmapData = d.GeomHeightfieldDataCreate ();
                GC.AddMemoryPressure (_heightmap.Length);//Add the memory pressure properly (note: should we be doing this since we have it in managed memory?)
                //Do NOT copy it! Otherwise, it'll copy the terrain into unmanaged memory where we can't release it each time
                d.GeomHeightfieldDataBuildShort (HeightmapData, _heightmap, 0, heightmapHeight, heightmapWidth,
                                                 heightmapHeightSamples, heightmapWidthSamples, scale,
                                                 offset, thickness, wrap);

                d.GeomHeightfieldDataSetBounds (HeightmapData, hfmin - 1.0f, hfmax + 1.0f);
                RegionTerrain = d.CreateHeightfield (space, HeightmapData, 1);

                if (RegionTerrain != IntPtr.Zero)
                {
                    d.GeomSetCategoryBits (RegionTerrain, (int)(CollisionCategories.Land));
                    d.GeomSetCollideBits (RegionTerrain, (int)(CollisionCategories.Space));
                }

                NullObjectPhysicsActor terrainActor = new NullObjectPhysicsActor ();

                actor_name_map[RegionTerrain] = terrainActor;

                d.Matrix3 R = new d.Matrix3 ();

                Quaternion q1 = Quaternion.CreateFromAxisAngle (new Vector3 (1, 0, 0), 1.5707f);
                Quaternion q2 = Quaternion.CreateFromAxisAngle (new Vector3 (0, 1, 0), 1.5707f);

                q1 = q1 * q2;

                Vector3 v3;
                float angle;
                q1.GetAxisAngle (out v3, out angle);

                d.RFromAxisAndAngle (out R, v3.X, v3.Y, v3.Z, angle);

                d.GeomSetRotation (RegionTerrain, ref R);
                d.GeomSetPosition (RegionTerrain, (m_region.RegionSizeX * 0.5f), (m_region.RegionSizeY * 0.5f), 0);

                float[] heighlimits = new float[2];
                heighlimits[0] = hfmin;
                heighlimits[1] = hfmax;

                TerrainHeightFieldHeights.Add (RegionTerrain, heightMap);
                ODETerrainHeightFieldHeights.Add (RegionTerrain, _heightmap);
                TerrainHeightFieldlimits.Add (RegionTerrain, heighlimits);
            }
        }

        public double GetWaterLevel(float x, float y)
        {
            return WaterHeightFieldHeight[(int)y * Region.RegionSizeX + (int)x];
        }

        public override void SetWaterLevel(short[] map)
        {
            WaterHeightFieldHeight = map;
        }

        #endregion

        #region Dispose

        public override void Dispose()
        {
            lock (OdeLock)
            {
                lock (_prims)
                {
                    foreach (AuroraPhysXPrim prm in _prims)
                    {
                        RemovePrim(prm);
                    }
                }

                //foreach (OdeCharacter act in _characters)
                //{
                //RemoveAvatar(act);
                //}
                d.WorldDestroy(world);
                //d.CloseODE();
            }
            m_rayCastManager.Dispose();
            m_rayCastManager = null;
        }

        #endregion

        #region Top colliders

        public override Dictionary<uint, float> GetTopColliders()
        {
            Dictionary<uint, float> returncolliders = new Dictionary<uint, float>();
            List<AuroraPhysXPrim> collidingPrims = new List<AuroraPhysXPrim>();
            lock (_prims)
            {
                foreach (AuroraPhysXPrim prm in _prims)
                {
                    if (prm.CollisionScore > 0)
                    {
                        if(!collidingPrims.Contains(prm))
                            collidingPrims.Add(prm);
                    }
                }
            }
            //Sort them by their score
            collidingPrims.Sort(delegate(AuroraPhysXPrim a, AuroraPhysXPrim b)
            {
                return b.CollisionScore.CompareTo(a.CollisionScore);
            });
            //Limit to 25
            if(collidingPrims.Count > 25)
                collidingPrims.RemoveRange(25, collidingPrims.Count - 25);

            foreach (AuroraPhysXPrim prm in collidingPrims)
            {
                returncolliders[prm.m_localID] = prm.CollisionScore;
                prm.CollisionScore = 0f;
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

        public override void RaycastWorld(Vector3 position, Vector3 direction, float length, int Count, RayCallback retMethod)
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
                return new List<ContactResult> ();
            return new List<ContactResult>(ourResults);
        }

        #endregion

        #region Gravity Calculation

        #region Structs

        private struct PointGravity
        {
            public Vector3 Position;
            public float ForceX;
            public float ForceY;
            public float ForceZ;
            public float GravForce;
            public float Radius;
            /// <summary>
            /// If this is true, the actor will have the forces applied to them 
            ///   once they enter the area, rather than having gravity act like it does 
            ///   in real life (pulling toward the center)
            /// </summary>
            public bool PointForce;
        }

        #endregion

        private bool normalGravityEnabled = true;
        private Dictionary<int, PointGravity> m_pointGravityPositions = new Dictionary<int, PointGravity> ();
        private bool pointGravityInUse = false;

        public void CalculateGravity (float mass, d.Vector3 position, bool allowNormalGravity, float gravityModifier, ref Vector3 forceVector)
        {
            if (normalGravityEnabled && allowNormalGravity)
            {
                //normal gravity, one axis, no center
                forceVector.X += gravityx * mass * gravityModifier;
                forceVector.Y += gravityy * mass * gravityModifier;
                forceVector.Z += gravityz * mass * gravityModifier;
            }
            if(pointGravityInUse)
            {
                Vector3 pos = new Vector3((float)position.X, (float)position.Y, (float)position.Z);
                //Find the nearby centers of gravity
                foreach (PointGravity pg in m_pointGravityPositions.Values)
                {
                    float distance = Vector3.DistanceSquared (pg.Position, pos);
                    if (distance < pg.Radius * pg.Radius)
                    {
                        float d = (distance / (pg.Radius * pg.Radius));
                        float radiusScaling = 1 - d;
                        radiusScaling *= radiusScaling;
                        if (pg.PointForce)
                        {
                            //Applies forces to the actor when in range
                            forceVector.X += pg.ForceX * radiusScaling * mass * gravityModifier;
                            forceVector.Y += pg.ForceY * radiusScaling * mass * gravityModifier;
                            forceVector.Z += pg.ForceZ * radiusScaling * mass * gravityModifier;
                        }
                        else
                        {
                            //Pulls the actor toward the point
                            forceVector.X += (pg.Position.X - pos.X) * pg.GravForce * radiusScaling * mass * gravityModifier;
                            forceVector.Y += (pg.Position.Y - pos.Y) * pg.GravForce * radiusScaling * mass * gravityModifier;
                            forceVector.Z += (pg.Position.Z - pos.Z) * pg.GravForce * radiusScaling * mass * gravityModifier;
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
        /// Sets gravity parameters in the single axis, if you want a point, use the gravity point pieces
        /// </summary>
        /// <param name="enabled">Enable one axis gravity (disables point gravity)</param>
        /// <param name="forceX"></param>
        /// <param name="forceY"></param>
        /// <param name="forceZ"></param>
        public override void SetGravityForce (bool enabled, float forceX, float forceY, float forceZ)
        {
            normalGravityEnabled = enabled;
            gravityx = forceX;
            gravityy = forceY;
            gravityz = forceZ;


            //Fix the ODE gravity too
            d.WorldSetGravity (world, gravityx, gravityy, gravityz);
        }

        public override float[] GetGravityForce ()
        {
            return new float[3] { gravityx, gravityy, gravityz };
        }

        public override void AddGravityPoint (bool isApplyingForces, Vector3 position, float forceX, float forceY, float forceZ, float gravForce, float radius, int identifier)
        {
            PointGravity pointGrav = new PointGravity ();
            pointGrav.ForceX = forceX;
            pointGrav.ForceY = forceY;
            pointGrav.ForceZ = forceZ;
            pointGrav.GravForce = gravForce;
            pointGrav.Radius = radius;
            pointGrav.Position = position;
            pointGrav.PointForce = isApplyingForces;

            pointGravityInUse = true;
            m_pointGravityPositions[identifier] = pointGrav;
        }

        #endregion

        #region Wind Calcs

        public void AddWindForce (float mass, d.Vector3 AbsolutePosition, ref Vector3 force)
        {
            if (m_windModule == null)
                m_windModule = m_registry.RequestModuleInterface<IWindModule> ();

            if (m_windModule == null)
                return;
            Vector3 windSpeed = m_windModule.WindSpeed ((int)AbsolutePosition.X, (int)AbsolutePosition.Y, (int)AbsolutePosition.Z);
            force = (windSpeed) / (mass);
            //force /= 20f;//Constant that doesn't make it too windy
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
