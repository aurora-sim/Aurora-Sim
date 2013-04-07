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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework.Physics;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.Utilities;
using Aurora.Framework.SceneInfo;

namespace OpenSim.Region.Physics.BulletSPlugin
{
public sealed class BSScene : PhysicsScene
{
    internal static readonly string LogHeader = "[BULLETS SCENE]";

    // The name of the region we're working for.
    public string RegionName { get; private set; }

    public string BulletSimVersion = "?";

    // The handle to the underlying managed or unmanaged version of Bullet being used.
    public string BulletEngineName { get; private set; }
    public BSAPITemplate PE;

    public Dictionary<uint, BSPhysObject> PhysObjects;
    public BSShapeCollection Shapes;

    // Keeping track of the objects with collisions so we can report begin and end of a collision
    public HashSet<BSPhysObject> ObjectsWithCollisions = new HashSet<BSPhysObject>();
    public HashSet<BSPhysObject> ObjectsWithNoMoreCollisions = new HashSet<BSPhysObject>();
    // Keep track of all the avatars so we can send them a collision event
    //    every tick so OpenSim will update its animation.
    private HashSet<BSPhysObject> m_avatars = new HashSet<BSPhysObject>();

    // let my minuions use my logger
    public ICommandConsole Logger { get { return MainConsole.Instance; } }

    public IMesher mesher;
    public uint WorldID { get; private set; }
    public BulletWorld World { get; private set; }

    // All the constraints that have been allocated in this instance.
    public BSConstraintCollection Constraints { get; private set; }

    // Simulation parameters
    internal int m_maxSubSteps;
    internal float m_fixedTimeStep;
    internal long m_simulationStep = 0;
    internal float NominalFrameRate { get; set; }
    public long SimulationStep { get { return m_simulationStep; } }
    internal float LastTimeStep { get; private set; }
    public override float StepTime { get { return m_fixedTimeStep; } }

    // Physical objects can register for prestep or poststep events
    public delegate void PreStepAction(float timeStep);
    public delegate void PostStepAction(float timeStep);
    public event PreStepAction BeforeStep;
    public event PostStepAction AfterStep;

    // A value of the time now so all the collision and update routines do not have to get their own
    // Set to 'now' just before all the prims and actors are called for collisions and updates
    public int SimulationNowTime { get; private set; }

    // True if initialized and ready to do simulation steps
    private bool m_initialized = false;

    // Flag which is true when processing taints.
    // Not guaranteed to be correct all the time (don't depend on this) but good for debugging.
    public bool InTaintTime { get; private set; }

    // Pinned memory used to pass step information between managed and unmanaged
    internal int m_maxCollisionsPerFrame;
    internal CollisionDesc[] m_collisionArray;

    internal int m_maxUpdatesPerFrame;
    internal EntityProperties[] m_updateArray;

    public const uint TERRAIN_ID = 0;       // OpenSim senses terrain with a localID of zero
    public const uint GROUNDPLANE_ID = 1;
    public const uint CHILDTERRAIN_ID = 2;  // Terrain allocated based on our mega-prim childre start here

    public float SimpleWaterLevel { get; set; }
    public BSTerrainManager TerrainManager { get; private set; }

    public ConfigurationParameters Params
    {
        get { return UnmanagedParams[0]; }
    }
    public Vector3 DefaultGravity
    {
        get { return new Vector3(0f, 0f, Params.gravity); }
    }
    // Just the Z value of the gravity
    public float DefaultGravityZ
    {
        get { return Params.gravity; }
    }

    // When functions in the unmanaged code must be called, it is only
    //   done at a known time just before the simulation step. The taint
    //   system saves all these function calls and executes them in
    //   order before the simulation.
    public delegate void TaintCallback();
    private struct TaintCallbackEntry
    {
        public String ident;
        public TaintCallback callback;
        public TaintCallbackEntry(string i, TaintCallback c)
        {
            ident = i;
            callback = c;
        }
    }
    private Object _taintLock = new Object();   // lock for using the next object
    private List<TaintCallbackEntry> _taintOperations;
    private Dictionary<string, TaintCallbackEntry> _postTaintOperations;
    private List<TaintCallbackEntry> _postStepOperations;

    // A pointer to an instance if this structure is passed to the C++ code
    // Used to pass basic configuration values to the unmanaged code.
    internal ConfigurationParameters[] UnmanagedParams;

    // Sometimes you just have to log everything.
    //public ICommandConsole PhysicsLogging;
    private bool m_physicsLoggingEnabled;
    private string m_physicsLoggingDir;
    private string m_physicsLoggingPrefix;
    private int m_physicsLoggingFileMinutes;
    private bool m_physicsLoggingDoFlush;
    private bool m_physicsPhysicalDumpEnabled;
    public int PhysicsMetricDumpFrames { get; set; }
    // 'true' of the vehicle code is to log lots of details
    public bool VehicleLoggingEnabled { get; private set; }
    public bool VehiclePhysicalLoggingEnabled { get; private set; }
    public IScene Scene { get; private set; }

    #region Construction and Initialization

    public override void Initialise(IMesher meshmerizer, IScene scene)
    {
        Scene = scene;
        mesher = meshmerizer;
        _taintOperations = new List<TaintCallbackEntry>();
        _postTaintOperations = new Dictionary<string, TaintCallbackEntry>();
        _postStepOperations = new List<TaintCallbackEntry>();
        PhysObjects = new Dictionary<uint, BSPhysObject>();
        Shapes = new BSShapeCollection(this);

        // Allocate pinned memory to pass parameters.
        UnmanagedParams = new ConfigurationParameters[1];

        // Set default values for physics parameters plus any overrides from the ini file
        GetInitialParameterValues(scene.Config);

        // Get the connection to the physics engine (could be native or one of many DLLs)
        PE = SelectUnderlyingBulletEngine(BulletEngineName);

        // Enable very detailed logging.
        // By creating an empty logger when not logging, the log message invocation code
        //     can be left in and every call doesn't have to check for null.
        /*if (m_physicsLoggingEnabled)
        {
            PhysicsLogging = new Logging.LogWriter(m_physicsLoggingDir, m_physicsLoggingPrefix, m_physicsLoggingFileMinutes);
            PhysicsLogging.ErrorLogger = m_log; // for DEBUG. Let's the logger output error messages.
        }
        else
        {
            PhysicsLogging = new Logging.LogWriter();
        }*/

        // Allocate memory for returning of the updates and collisions from the physics engine
        m_collisionArray = new CollisionDesc[m_maxCollisionsPerFrame];
        m_updateArray = new EntityProperties[m_maxUpdatesPerFrame];

        // The bounding box for the simulated world. The origin is 0,0,0 unless we're
        //    a child in a mega-region.
        // Bullet actually doesn't care about the extents of the simulated
        //    area. It tracks active objects no matter where they are.
        Vector3 worldExtent = new Vector3(Constants.RegionSize, Constants.RegionSize, Constants.RegionHeight);

        World = PE.Initialize(worldExtent, Params, m_maxCollisionsPerFrame, ref m_collisionArray, m_maxUpdatesPerFrame, ref m_updateArray);

        Constraints = new BSConstraintCollection(World);

        TerrainManager = new BSTerrainManager(this);
        TerrainManager.CreateInitialGroundPlaneAndTerrain();

        MainConsole.Instance.WarnFormat("{0} Linksets implemented with {1}", LogHeader, (BSLinkset.LinksetImplementation)BSParam.LinksetImplementation);

        InTaintTime = false;
        m_initialized = true;
    }

    public override void PostInitialise(IConfigSource config)
    {
    }

    // All default parameter values are set here. There should be no values set in the
    // variable definitions.
    private void GetInitialParameterValues(IConfigSource config)
    {
        ConfigurationParameters parms = new ConfigurationParameters();
        UnmanagedParams[0] = parms;

        BSParam.SetParameterDefaultValues(this);

        if (config != null)
        {
            // If there are specifications in the ini file, use those values
            IConfig pConfig = config.Configs["BulletSim"];
            if (pConfig != null)
            {
                BSParam.SetParameterConfigurationValues(this, pConfig);

                // There are two Bullet implementations to choose from
                BulletEngineName = pConfig.GetString("BulletEngine", "BulletUnmanaged");

                // Very detailed logging for physics debugging
                // TODO: the boolean values can be moved to the normal parameter processing.
                m_physicsLoggingEnabled = pConfig.GetBoolean("PhysicsLoggingEnabled", false);
                m_physicsLoggingDir = pConfig.GetString("PhysicsLoggingDir", ".");
                m_physicsLoggingPrefix = pConfig.GetString("PhysicsLoggingPrefix", "physics-%REGIONNAME%-");
                m_physicsLoggingFileMinutes = pConfig.GetInt("PhysicsLoggingFileMinutes", 5);
                m_physicsLoggingDoFlush = pConfig.GetBoolean("PhysicsLoggingDoFlush", false);
                m_physicsPhysicalDumpEnabled = pConfig.GetBoolean("PhysicsPhysicalDumpEnabled", false);
                // Very detailed logging for vehicle debugging
                VehicleLoggingEnabled = pConfig.GetBoolean("VehicleLoggingEnabled", false);
                VehiclePhysicalLoggingEnabled = pConfig.GetBoolean("VehiclePhysicalLoggingEnabled", false);

                // Do any replacements in the parameters
                m_physicsLoggingPrefix = m_physicsLoggingPrefix.Replace("%REGIONNAME%", RegionName);
            }

            // The material characteristics.
            BSMaterials.InitializeFromDefaults(Params);
            if (pConfig != null)
            {
                // Let the user add new and interesting material property values.
                BSMaterials.InitializefromParameters(pConfig);
            }
        }
    }

    // A helper function that handles a true/false parameter and returns the proper float number encoding
    float ParamBoolean(IConfig config, string parmName, float deflt)
    {
        float ret = deflt;
        if (config.Contains(parmName))
        {
            ret = ConfigurationParameters.numericFalse;
            if (config.GetBoolean(parmName, false))
            {
                ret = ConfigurationParameters.numericTrue;
            }
        }
        return ret;
    }

    // Select the connection to the actual Bullet implementation.
    // The main engine selection is the engineName up to the first hypen.
    // So "Bullet-2.80-OpenCL-Intel" specifies the 'bullet' class here and the whole name
    //     is passed to the engine to do its special selection, etc.
    private BSAPITemplate SelectUnderlyingBulletEngine(string engineName)
    {
        // For the moment, do a simple switch statement.
        // Someday do fancyness with looking up the interfaces in the assembly.
        BSAPITemplate ret = null;

        string selectionName = engineName.ToLower();
        int hyphenIndex = engineName.IndexOf("-");
        if (hyphenIndex > 0)
            selectionName = engineName.ToLower().Substring(0, hyphenIndex - 1);

        switch (selectionName)
        {
            case "bulletunmanaged":
                ret = new BSAPIUnman(engineName, this);
                break;
            case "bulletxna":
                ret = new BSAPIXNA(engineName, this);
                break;
        }

        if (ret == null)
        {
            MainConsole.Instance.ErrorFormat("{0) COULD NOT SELECT BULLET ENGINE: '[BulletSim]PhysicsEngine' must be either 'BulletUnmanaged-*' or 'BulletXNA-*'", LogHeader);
        }
        else
        {
            MainConsole.Instance.WarnFormat("{0} Selected bullet engine {1} -> {2}/{3}", LogHeader, engineName, ret.BulletEngineName, ret.BulletEngineVersion);
        }

        return ret;
    }

    public override void Dispose()
    {
        // MainConsole.Instance.DebugFormat("{0}: Dispose()", LogHeader);

        // make sure no stepping happens while we're deleting stuff
        m_initialized = false;

        foreach (KeyValuePair<uint, BSPhysObject> kvp in PhysObjects)
        {
            kvp.Value.Destroy();
        }
        PhysObjects.Clear();

        // Now that the prims are all cleaned up, there should be no constraints left
        if (Constraints != null)
        {
            Constraints.Dispose();
            Constraints = null;
        }

        if (Shapes != null)
        {
            Shapes.Dispose();
            Shapes = null;
        }

        if (TerrainManager != null)
        {
            TerrainManager.ReleaseGroundPlaneAndTerrain();
            TerrainManager.Dispose();
            TerrainManager = null;
        }

        // Anything left in the unmanaged code should be cleaned out
        PE.Shutdown(World);
    }
    #endregion // Construction and Initialization

    #region Prim and Avatar addition and removal

    public override PhysicsActor AddAvatar(string avName, Vector3 position, Quaternion rotation, Vector3 size, bool isFlying, uint localID, UUID UUID)
    {
        if (!m_initialized) return null;

        BSCharacter actor = new BSCharacter(localID, avName, this, position, size, isFlying);
        actor.UUID = UUID;
        lock (PhysObjects)
            PhysObjects.Add(localID, actor);

        // TODO: Remove kludge someday.
        // We must generate a collision for avatars whether they collide or not.
        // This is required by OpenSim to update avatar animations, etc.
        lock (m_avatars)
            m_avatars.Add(actor);

        return actor;
    }

    public override void RemoveAvatar(PhysicsActor actor)
    {
        // MainConsole.Instance.DebugFormat("{0}: RemoveAvatar", LogHeader);

        if (!m_initialized) return;

        BSCharacter bsactor = actor as BSCharacter;
        if (bsactor != null)
        {
            try
            {
                lock (PhysObjects)
                    PhysObjects.Remove(bsactor.LocalID);
                // Remove kludge someday
                lock (m_avatars)
                    m_avatars.Remove(bsactor);
            }
            catch (Exception e)
            {
                MainConsole.Instance.WarnFormat("{0}: Attempt to remove avatar that is not in physics scene: {1}", LogHeader, e);
            }
            bsactor.Destroy();
            // bsactor.dispose();
        }
        else
        {
            MainConsole.Instance.ErrorFormat("{0}: Requested to remove avatar that is not a BSCharacter. ID={1}, type={2}",
                                        LogHeader, actor.LocalID, actor.GetType().Name);
        }
    }

    public override void DeletePrim(PhysicsActor prim)
    {
        RemovePrim(prim);
    }

    public override void RemovePrim(PhysicsActor prim)
    {
        if (!m_initialized) return;

        BSPhysObject bsprim = prim as BSPhysObject;
        if (bsprim != null)
        {
            DetailLog("{0},RemovePrim,call", bsprim.LocalID);
            // MainConsole.Instance.DebugFormat("{0}: RemovePrim. id={1}/{2}", LogHeader, bsprim.Name, bsprim.LocalID);
            try
            {
                lock (PhysObjects) PhysObjects.Remove(bsprim.LocalID);
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("{0}: Attempt to remove prim that is not in physics scene: {1}", LogHeader, e);
            }
            bsprim.Destroy();
            // bsprim.dispose();
        }
        else
        {
            MainConsole.Instance.ErrorFormat("{0}: Attempt to remove prim that is not a BSPrim type.", LogHeader);
        }
    }

    public override PhysicsActor AddPrimShape(UUID primID, uint localID, string name, byte physicsType, PrimitiveBaseShape shape,
                                            Vector3 position, Vector3 size, Quaternion rotation, bool isPhysical)
    {
        // MainConsole.Instance.DebugFormat("{0}: AddPrimShape2: {1}", LogHeader, primName);

        if (!m_initialized) return null;

        // DetailLog("{0},BSScene.AddPrimShape,call", localID);

        BSPhysObject prim = new BSPrimLinkable(localID, name, this, position, size, rotation, shape, isPhysical);
        prim.UUID = primID;
        lock (PhysObjects) PhysObjects.Add(localID, prim);
        return prim;
    }

    #endregion // Prim and Avatar addition and removal

    #region Gravity Changes

    public override float[] GetGravityForce()
    {
        return new float[3] { 0, 0, DefaultGravityZ };
    }

    public override void SetGravityForce(bool enabled, float forceX, float forceY, float forceZ)
    {
        base.SetGravityForce(enabled, forceX, forceY, forceZ);
    }

    public override void AddGravityPoint(bool isApplyingForces, Vector3 position, float forceX, float forceY, float forceZ, float gravForce, float radius, int identifier)
    {
        base.AddGravityPoint(isApplyingForces, position, forceX, forceY, forceZ, gravForce, radius, identifier);
    }

    #endregion

    #region Simulation

    public override float TimeDilation
    {
        get;
        set;
    }

    // Simulate one timestep
    public override void Simulate(float timeStep)
    {
        // prevent simulation until we've been initialized
        if (!m_initialized) return;

        LastTimeStep = timeStep;

        int updatedEntityCount = 0;
        int collidersCount = 0;

        int beforeTime = 0;
        int simTime = 0;

        // update the prim states while we know the physics engine is not busy
        int numTaints = _taintOperations.Count;

        InTaintTime = true; // Only used for debugging so locking is not necessary.

        beforeTime = Util.EnvironmentTickCount();

        ProcessTaints();

        // Some of the physical objects requre individual, pre-step calls
        //      (vehicles and avatar movement, in particular)
        TriggerPreStepEvent(timeStep);

        // the prestep actions might have added taints
        numTaints += _taintOperations.Count;
        ProcessTaints();

        StatPhysicsTaintTime = Util.EnvironmentTickCountSubtract(beforeTime);
        InTaintTime = false; // Only used for debugging so locking is not necessary.

        // The following causes the unmanaged code to output ALL the values found in ALL the objects in the world.
        // Only enable this in a limited test world with few objects.
        if (m_physicsPhysicalDumpEnabled)
            PE.DumpAllInfo(World);

        // step the physical world one interval
        m_simulationStep++;
        int numSubSteps = 0;
        try
        {
            beforeTime = Util.EnvironmentTickCount();

            numSubSteps = PE.PhysicsStep(World, timeStep, m_maxSubSteps, m_fixedTimeStep, out updatedEntityCount, out collidersCount);

            //if (PhysicsLogging.Enabled)
            {
                StatContactLoopTime = Util.EnvironmentTickCountSubtract(beforeTime);
                DetailLog("{0},Simulate,call, frame={1}, nTaints={2}, simTime={3}, substeps={4}, updates={5}, colliders={6}, objWColl={7}",
                                        DetailLogZero, m_simulationStep, numTaints, StatContactLoopTime, numSubSteps,
                                        updatedEntityCount, collidersCount, ObjectsWithCollisions.Count);
            }
        }
        catch (Exception e)
        {
            MainConsole.Instance.WarnFormat("{0},PhysicsStep Exception: nTaints={1}, substeps={2}, updates={3}, colliders={4}, e={5}",
                        LogHeader, numTaints, numSubSteps, updatedEntityCount, collidersCount, e);
            DetailLog("{0},PhysicsStepException,call, nTaints={1}, substeps={2}, updates={3}, colliders={4}",
                        DetailLogZero, numTaints, numSubSteps, updatedEntityCount, collidersCount);
            updatedEntityCount = 0;
            collidersCount = 0;
        }

        if (PhysicsMetricDumpFrames != 0 && ((m_simulationStep % PhysicsMetricDumpFrames) == 0))
            PE.DumpPhysicsStatistics(World);

        // Get a value for 'now' so all the collision and update routines don't have to get their own.
        SimulationNowTime = Util.EnvironmentTickCount();

        beforeTime = Util.EnvironmentTickCount();

        // If there were collisions, process them by sending the event to the prim.
        // Collisions must be processed before updates.
        if (collidersCount > 0)
        {
            for (int ii = 0; ii < collidersCount; ii++)
            {
                uint cA = m_collisionArray[ii].aID;
                uint cB = m_collisionArray[ii].bID;
                Vector3 point = m_collisionArray[ii].point;
                Vector3 normal = m_collisionArray[ii].normal;
                float penetration = m_collisionArray[ii].penetration;
                SendCollision(cA, cB, point, normal, penetration);
                SendCollision(cB, cA, point, -normal, penetration);
            }
        }

        // The above SendCollision's batch up the collisions on the objects.
        //      Now push the collisions into the simulator.
        if (ObjectsWithCollisions.Count > 0)
        {
            foreach (BSPhysObject bsp in ObjectsWithCollisions)
                if (!bsp.SendCollisions())
                {
                    // If the object is done colliding, see that it's removed from the colliding list
                    ObjectsWithNoMoreCollisions.Add(bsp);
                }
        }

        // This is a kludge to get avatar movement updates.
        // The simulator expects collisions for avatars even if there are have been no collisions.
        //    The event updates avatar animations and stuff.
        // If you fix avatar animation updates, remove this overhead and let normal collision processing happen.
        foreach (BSPhysObject bsp in m_avatars)
            if (!ObjectsWithCollisions.Contains(bsp))   // don't call avatars twice
                bsp.SendCollisions();

        // Objects that are done colliding are removed from the ObjectsWithCollisions list.
        // Not done above because it is inside an iteration of ObjectWithCollisions.
        // This complex collision processing is required to create an empty collision
        //     event call after all real collisions have happened on an object. This enables
        //     the simulator to generate the 'collision end' event.
        if (ObjectsWithNoMoreCollisions.Count > 0)
        {
            foreach (BSPhysObject po in ObjectsWithNoMoreCollisions)
                ObjectsWithCollisions.Remove(po);
            ObjectsWithNoMoreCollisions.Clear();
        }
        // Done with collisions.

        // If any of the objects had updated properties, tell the object it has been changed by the physics engine
        if (updatedEntityCount > 0)
        {
            for (int ii = 0; ii < updatedEntityCount; ii++)
            {
                EntityProperties entprop = m_updateArray[ii];
                BSPhysObject pobj;
                if (PhysObjects.TryGetValue(entprop.ID, out pobj))
                {
                    pobj.UpdateProperties(entprop);
                }
            }
        }

        StatPhysicsMoveTime = Util.EnvironmentTickCountSubtract(beforeTime);

        TriggerPostStepEvent(timeStep);

        // The following causes the unmanaged code to output ALL the values found in ALL the objects in the world.
        // Only enable this in a limited test world with few objects.
        if (m_physicsPhysicalDumpEnabled)
            PE.DumpAllInfo(World);

        // The physics engine returns the number of milliseconds it simulated this call.
        // These are summed and normalized to one second and divided by 1000 to give the reported physics FPS.
        // Multiply by a fixed nominal frame rate to give a rate similar to the simulator (usually 55).
        //return (float)numSubSteps * m_fixedTimeStep * 1000f * NominalFrameRate;
    }

    // Something has collided
    private void SendCollision(uint localID, uint collidingWith, Vector3 collidePoint, Vector3 collideNormal, float penetration)
    {
        if (localID <= TerrainManager.HighestTerrainID)
        {
            return;         // don't send collisions to the terrain
        }

        BSPhysObject collider;
        if (!PhysObjects.TryGetValue(localID, out collider))
        {
            // If the object that is colliding cannot be found, just ignore the collision.
            DetailLog("{0},BSScene.SendCollision,colliderNotInObjectList,id={1},with={2}", DetailLogZero, localID, collidingWith);
            return;
        }

        // The terrain is not in the physical object list so 'collidee' can be null when Collide() is called.
        BSPhysObject collidee = null;
        PhysObjects.TryGetValue(collidingWith, out collidee);

        // DetailLog("{0},BSScene.SendCollision,collide,id={1},with={2}", DetailLogZero, localID, collidingWith);

        if (collider.Collide(collidingWith, collidee, collidePoint, collideNormal, penetration))
        {
            // If a collision was posted, remember to send it to the simulator
            ObjectsWithCollisions.Add(collider);
        }

        return;
    }

    #endregion // Simulation

    public override void GetResults() { }

    #region Terrain

    public override void SetTerrain(ITerrainChannel channel, short[] heightMap)
    {
        float[] heightmap = new float[Scene.RegionInfo.RegionSizeX * Scene.RegionInfo.RegionSizeY];

        for (int x = 0; x < Scene.RegionInfo.RegionSizeX; x++)
        {
            for (int y = 0; y < Scene.RegionInfo.RegionSizeY; y++)
            {
                heightmap[(x * Scene.RegionInfo.RegionSizeX) + y] = ((float)heightMap[x * Scene.RegionInfo.RegionSizeX + y]) / Constants.TerrainCompression;
            }
        }
        TerrainManager.SetTerrain(heightmap);
    }

    public override void SetWaterLevel(double baseheight, short[] map)
    {
        SimpleWaterLevel = (float)baseheight;
    }

    #endregion // Terrain

    public override Dictionary<uint, float> GetTopColliders()
    {
        Dictionary<uint, float> topColliders;

        lock (PhysObjects)
        {
            foreach (KeyValuePair<uint, BSPhysObject> kvp in PhysObjects)
            {
                kvp.Value.ComputeCollisionScore();
            }

            List<BSPhysObject> orderedPrims = new List<BSPhysObject>(PhysObjects.Values);
            orderedPrims.OrderByDescending(p => p.CollisionScore);
            topColliders = orderedPrims.Take(25).ToDictionary(p => p.LocalID, p => p.CollisionScore);
        }

        return topColliders;
    }

    public override bool IsThreaded { get { return false;  } }

    #region Taints
    // The simulation execution order is:
    // Simulate()
    //    DoOneTimeTaints
    //    TriggerPreStepEvent
    //    DoOneTimeTaints
    //    Step()
    //       ProcessAndSendToSimulatorCollisions
    //       ProcessAndSendToSimulatorPropertyUpdates
    //    TriggerPostStepEvent

    // Calls to the PhysicsActors can't directly call into the physics engine
    //       because it might be busy. We delay changes to a known time.
    // We rely on C#'s closure to save and restore the context for the delegate.
    public void TaintedObject(String ident, TaintCallback callback)
    {
        if (!m_initialized) return;

        lock (_taintLock)
        {
            _taintOperations.Add(new TaintCallbackEntry(ident, callback));
        }

        return;
    }

    // Sometimes a potentially tainted operation can be used in and out of taint time.
    // This routine executes the command immediately if in taint-time otherwise it is queued.
    public void TaintedObject(bool inTaintTime, string ident, TaintCallback callback)
    {
        if (inTaintTime)
            callback();
        else
            TaintedObject(ident, callback);
    }

    private void TriggerPreStepEvent(float timeStep)
    {
        PreStepAction actions = BeforeStep;
        if (actions != null)
            actions(timeStep);

    }

    private void TriggerPostStepEvent(float timeStep)
    {
        PostStepAction actions = AfterStep;
        if (actions != null)
            actions(timeStep);

    }

    // When someone tries to change a property on a BSPrim or BSCharacter, the object queues
    // a callback into itself to do the actual property change. That callback is called
    // here just before the physics engine is called to step the simulation.
    public void ProcessTaints()
    {
        ProcessRegularTaints();
        ProcessPostTaintTaints();
    }

    private void ProcessRegularTaints()
    {
        if (_taintOperations.Count > 0)  // save allocating new list if there is nothing to process
        {
            // swizzle a new list into the list location so we can process what's there
            List<TaintCallbackEntry> oldList;
            lock (_taintLock)
            {
                oldList = _taintOperations;
                _taintOperations = new List<TaintCallbackEntry>();
            }

            foreach (TaintCallbackEntry tcbe in oldList)
            {
                try
                {
                    DetailLog("{0},BSScene.ProcessTaints,doTaint,id={1}", DetailLogZero, tcbe.ident); // DEBUG DEBUG DEBUG
                    tcbe.callback();
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("{0}: ProcessTaints: {1}: Exception: {2}", LogHeader, tcbe.ident, e);
                }
            }
            oldList.Clear();
        }
    }

    // Schedule an update to happen after all the regular taints are processed.
    // Note that new requests for the same operation ("ident") for the same object ("ID")
    //     will replace any previous operation by the same object.
    public void PostTaintObject(String ident, uint ID, TaintCallback callback)
    {
        string uniqueIdent = ident + "-" + ID.ToString();
        lock (_taintLock)
        {
            _postTaintOperations[uniqueIdent] = new TaintCallbackEntry(uniqueIdent, callback);
        }

        return;
    }

    // Taints that happen after the normal taint processing but before the simulation step.
    private void ProcessPostTaintTaints()
    {
        if (_postTaintOperations.Count > 0)
        {
            Dictionary<string, TaintCallbackEntry> oldList;
            lock (_taintLock)
            {
                oldList = _postTaintOperations;
                _postTaintOperations = new Dictionary<string, TaintCallbackEntry>();
            }

            foreach (KeyValuePair<string,TaintCallbackEntry> kvp in oldList)
            {
                try
                {
                    DetailLog("{0},BSScene.ProcessPostTaintTaints,doTaint,id={1}", DetailLogZero, kvp.Key); // DEBUG DEBUG DEBUG
                    kvp.Value.callback();
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("{0}: ProcessPostTaintTaints: {1}: Exception: {2}", LogHeader, kvp.Key, e);
                }
            }
            oldList.Clear();
        }
    }

    // Only used for debugging. Does not change state of anything so locking is not necessary.
    public bool AssertInTaintTime(string whereFrom)
    {
        if (!InTaintTime)
        {
            DetailLog("{0},BSScene.AssertInTaintTime,NOT IN TAINT TIME,Region={1},Where={2}", DetailLogZero, RegionName, whereFrom);
            MainConsole.Instance.ErrorFormat("{0} NOT IN TAINT TIME!! Region={1}, Where={2}", LogHeader, RegionName, whereFrom);
            // Util.PrintCallStack(DetailLog);
        }
        return InTaintTime;
    }

    #endregion // Taints

    // Invoke the detailed logger and output something if it's enabled.
    public void DetailLog(string msg, params Object[] args)
    {
        //PhysicsLogging.TraceFormat(msg, args);
        // Add the Flush() if debugging crashes. Gets all the messages written out.
        //if (m_physicsLoggingDoFlush) PhysicsLogging.Flush();
    }
    // Used to fill in the LocalID when there isn't one. It's the correct number of characters.
    public const string DetailLogZero = "0000000000";

}
}
