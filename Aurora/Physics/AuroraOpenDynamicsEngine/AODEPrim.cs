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

/*
 * Revised August 26 2009 by Kitto Flora. ODEDynamics.cs replaces
 * ODEVehicleSettings.cs. It and ODEPrim.cs are re-organised:
 * ODEPrim.cs contains methods dealing with Prim editing, Prim
 * characteristics and Kinetic motion.
 * ODEDynamics.cs contains methods dealing with Prim Physical motion
 * (dynamics) and the associated settings. Old Linear and angular
 * motors for dynamic motion have been replace with  MoveLinear()
 * and MoveAngular(); 'Physical' is used only to switch ODE dynamic 
 * simualtion on/off; VEHICAL_TYPE_NONE/VEHICAL_TYPE_<other> is to
 * switch between 'VEHICLE' parameter use and general dynamics
 * settings use.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OdeAPI;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Physics;
//using Ode.NET;

namespace Aurora.Physics.AuroraOpenDynamicsEngine
{
    /// <summary>
    ///   Various properties that ODE uses for AMotors but isn't exposed in ODE.NET so we must define them ourselves.
    /// </summary>
    public class AuroraODEPrim : PhysicsObject
    {
        private const CollisionCategories m_default_collisionFlags = (CollisionCategories.Geom
                                                                      | CollisionCategories.Space
                                                                      | CollisionCategories.Body
                                                                      | CollisionCategories.Character
                                                                     );

        private static readonly Dictionary<ulong, IntPtr> m_MeshToTriMeshMap = new Dictionary<ulong, IntPtr>();
        private readonly AuroraODEPhysicsScene _parent_scene;

        private readonly Vector3 _torque = Vector3.Zero;
        private readonly int body_autodisable_frames = 20;
        private readonly AuroraODEDynamics m_vehicle;
        private IntPtr Amotor = IntPtr.Zero;
        public IntPtr Body { get; private set; }
        private CollisionEventUpdate CollisionEventsThisFrame;

        private float PID_D = 35f;
        private float PID_G = 25f;
        private Vector3 _acceleration;
        private float _mass; // prim or object mass
        private IMesh _mesh;
        private Quaternion _orientation;
        private PhysicsObject _parent;
        private ISceneChildEntity _parent_entity;
        private PrimitiveBaseShape _pbs;
        private Vector3 _position;
        private Vector3 _size;
        private IntPtr _triMeshData;
        private Vector3 _velocity;
        private bool _zeroFlag;
        private bool _zeroFlagForceSet;
        internal volatile bool childPrim;
        internal List<AuroraODEPrim> childrenPrim = new List<AuroraODEPrim>();
        private int fakeori; // control the use of above
        private int fakepos; // control the use of above
        private bool hasOOBoffsetFromMesh; // if true we did compute it form mesh centroid, else from aabb
        private bool iscolliding;
        private Vector3 m_angularforceacc;
        private Vector3 m_angularlock = Vector3.One;
        private bool m_blockPhysicalReconstruction;
        private bool m_buildingRepresentation;

        // KF: These next 7 params apply to llSetHoverHeight(float height, integer water, float tau),
        // and are for non-VEHICLES only.

        private float m_buoyancy; //KF: m_buoyancy should be set by llSetBuoyancy() for non-vehicle. 

        // private float m_tensor = 5f;

        private bool m_collidesLand = true;
        private bool m_collidesWater;

        // Default we're a Geometry
        private CollisionCategories m_collisionCategories = (CollisionCategories.Geom);

        // Default, Collide with Other Geometries, spaces and Bodies
        private CollisionCategories m_collisionFlags = m_default_collisionFlags;
        internal float m_collisionscore;
        private int m_crossingfailures;

        internal bool m_disabled;
        private bool m_eventsubscription;
        //This disables the prim so that it cannot do much anything at all

        private Vector3 m_force;
        private Vector3 m_forceacc;
        internal bool m_frozen;
        private bool m_isSelected;

        private bool m_isVolumeDetect; // If true, this prim only detects collisions but doesn't collide actively
        private bool m_isphysical;
        private int m_lastUpdateSent;
        private Vector3 m_lastVelocity;
        private Quaternion m_lastorientation;
        private Vector3 m_lastposition;
        private uint m_localID;
        private int m_material = (int)Material.Wood;
        private bool m_primIsRemoved;
        private Vector3 m_pushForce;
        private Vector3 m_rotationalVelocity;
        internal IntPtr m_targetSpace = IntPtr.Zero;

        private bool m_throttleUpdates;

        private float primMass; // prim own mass

        public Vector3 primOOBoffset; // is centroid out of mesh or rest aabb
        public Vector3 primOOBsize; // prim real dimensions from mesh 
        public IntPtr prim_geom;
        private d.Mass primdMass; // prim inertia information on it's own referencial
        private Quaternion showorientation; // tmp hack see showposition
        private Vector3 showposition; // a temp hack for now rest of code expects position to be changed immediately

        public AuroraODEPrim(ISceneChildEntity entity, AuroraODEPhysicsScene parent_scene, bool pisPhysical)
        {
            m_vehicle = new AuroraODEDynamics();
            //gc = GCHandle.Alloc(prim_geom, GCHandleType.Pinned);

            // correct for changed timestep
            PID_D /= (parent_scene.ODE_STEPSIZE * 50f); // original ode fps of 50
            PID_G /= (parent_scene.ODE_STEPSIZE * 50f);

            body_autodisable_frames = parent_scene.bodyFramesAutoDisable;

            prim_geom = IntPtr.Zero;

            _size = entity.Scale;
            _position = entity.AbsolutePosition;
            fakepos = 0;
            _orientation = entity.GetWorldRotation();
            fakeori = 0;
            _pbs = entity.Shape;
            _parent_entity = entity;

            _parent_scene = parent_scene;
            m_targetSpace = IntPtr.Zero;

            /*
                        m_isphysical = pisPhysical;
                        if (m_isphysical)
                            m_targetSpace = _parent_scene.space;
            */
            m_isphysical = false;

            m_forceacc = Vector3.Zero;
            m_angularforceacc = Vector3.Zero;

            hasOOBoffsetFromMesh = false;
            _triMeshData = IntPtr.Zero;

            CalcPrimBodyData();

            _parent_scene.AddSimulationChange(() => changeadd());
        }

        public ISceneChildEntity ParentEntity
        {
            get { return _parent_entity; }
        }

        public override bool BuildingRepresentation
        {
            get { return m_buildingRepresentation; }
            set
            {
                if (value)
                    m_buildingRepresentation = value;
                //else
                //    _parent_scene.AddSimulationChange(() => m_buildingRepresentation = false);
            }
        }

        public override bool BlockPhysicalReconstruction
        {
            get { return m_blockPhysicalReconstruction; }
            set
            {
                if (value)
                    m_blockPhysicalReconstruction = value;
                else
                    _parent_scene.AddSimulationChange(() =>
                    {
                        if (value)
                            DestroyBody();
                        else
                        {
                            m_blockPhysicalReconstruction = false;
                            if (!childPrim)
                                MakeBody();
                        }
                        if (!childPrim && childrenPrim.Count > 0)
                        {
                            foreach (AuroraODEPrim prm in childrenPrim)
                                prm.BlockPhysicalReconstruction = value;
                        }
                    });
            }
        }

        public PhysicsObject Parent
        {
            get { return _parent; }
        }

        public override int PhysicsActorType
        {
            get { return (int)ActorTypes.Prim; }
        }

        public override uint LocalID
        {
            get { return m_localID; }
            set
            {
                //MainConsole.Instance.Info("[PHYSICS]: Setting TrackerID: " + value);
                m_localID = value;
            }
        }

        public override bool VolumeDetect
        {
            get { return m_isVolumeDetect; }
            set { _parent_scene.AddSimulationChange(() => changevoldtc(value)); }
        }

        public override bool Selected
        {
            set
            {
                // This only makes the object not collidable if the object
                // is physical or the object is modified somehow *IN THE FUTURE*
                // without this, if an avatar selects prim, they can walk right
                // through it while it's selected
                if ((IsPhysical && !_zeroFlag) || !value)
                    _parent_scene.AddSimulationChange(() => changeSelectedStatus(value));
                else
                    m_isSelected = value;
                if (m_isSelected)
                    disableBodySoft();
            }
        }

        public override bool IsPhysical
        {
            get
            {
                if (childPrim && _parent != null) // root prim defines if is physical or not
                    return ((AuroraODEPrim)_parent).m_isphysical;
                else
                    return m_isphysical;
            }
            set
            {
                _parent_scene.AddSimulationChange(() => changePhysicsStatus(value));
                if (!value) // Zero the remembered last velocity
                    m_lastVelocity = Vector3.Zero;
            }
        }

        public override bool IsTruelyColliding { get; set; }
        public override bool IsColliding
        {
            get { return iscolliding; }
            set
            {
                if (value && _parent != null)
                    _parent.LinkSetIsColliding = value;
                LinkSetIsColliding = value;
                iscolliding = value;
            }
        }

        public override bool LinkSetIsColliding { get; set; }

        public override bool ThrottleUpdates
        {
            get { return m_throttleUpdates; }
            set { m_throttleUpdates = value; }
        }

        public override Vector3 Position
        {
            get
            {
                if (fakepos > 0)
                    return showposition;
                else
                    return _position;
            }
            set
            {
                showposition = value;
                fakepos++;
                _parent_scene.AddSimulationChange(() => changePosition(value));
            }
        }

        public override Vector3 Size
        {
            get { return _size; }
            set
            {
                if (value.IsFinite())
                    _parent_scene.AddSimulationChange(() => changesize(value));
                else
                    MainConsole.Instance.Warn("[PHYSICS]: Got NaN Size on object");
            }
        }


        public override float Mass
        {
            get { return _mass; }
        }

        public override Vector3 Force
        {
            //get { return Vector3.Zero; }
            get { return m_force; }
            set
            {
                if (value.IsFinite())
                {
                    _parent_scene.AddSimulationChange(() => changeforce(value, false));
                }
                else
                {
                    MainConsole.Instance.Warn("[PHYSICS]: NaN in Force Applied to an Object");
                }
            }
        }

        public override int VehicleType
        {
            get { return (int)m_vehicle.Type; }
            set { _parent_scene.AddSimulationChange(() => changeVehicleType(value)); }
        }

        public override Vector3 CenterOfMass
        {
            get
            {
                d.Vector3 dtmp;
                if (IsPhysical && !childPrim && Body != IntPtr.Zero)
                {
                    dtmp = d.BodyGetPosition(Body);
                    return new Vector3(dtmp.X, dtmp.Y, dtmp.Z);
                }
                else if (prim_geom != IntPtr.Zero)
                {
                    d.Quaternion dq;
                    d.GeomCopyQuaternion(prim_geom, out dq);
                    Quaternion q;
                    q.X = dq.X;
                    q.Y = dq.Y;
                    q.Z = dq.Z;
                    q.W = dq.W;

                    Vector3 vtmp = primOOBoffset * q;
                    dtmp = d.GeomGetPosition(prim_geom);
                    return new Vector3(dtmp.X + vtmp.X, dtmp.Y + vtmp.Y, dtmp.Z + vtmp.Z);
                }
                else
                    return Vector3.Zero;
            }
        }

        public override PrimitiveBaseShape Shape
        {
            set { _parent_scene.AddSimulationChange(() => changeshape(value)); }
        }

        public override Vector3 Velocity
        {
            get
            {
                // Averate previous velocity with the new one so
                // client object interpolation works a 'little' better
                if (_zeroFlag)
                    return Vector3.Zero;

                Vector3 returnVelocity = Vector3.Zero;
                returnVelocity.X = (m_lastVelocity.X + _velocity.X) / 2;
                returnVelocity.Y = (m_lastVelocity.Y + _velocity.Y) / 2;
                returnVelocity.Z = (m_lastVelocity.Z + _velocity.Z) / 2;
                return returnVelocity;
            }
            set
            {
                if (value.IsFinite())
                    _parent_scene.AddSimulationChange(() => changevelocity(value));
                else
                {
                    MainConsole.Instance.Warn("[PHYSICS]: Got NaN Velocity in Object");
                }
            }
        }

        public override Vector3 Torque
        {
            get
            {
                if (childPrim || !m_isphysical || Body == IntPtr.Zero)
                    return Vector3.Zero;

                return _torque;
            }

            set
            {
                if (value.IsFinite())
                    _parent_scene.AddSimulationChange(() => changeSetTorque(value));
                else
                    MainConsole.Instance.Warn("[PHYSICS]: Got NaN Torque in Object");
            }
        }

        public override float CollisionScore
        {
            get { return m_collisionscore; }
            set { m_collisionscore = value; }
        }

        public override Quaternion Orientation
        {
            get
            {
                if (fakeori > 0)
                    return showorientation;
                else
                    return _orientation;
            }
            set
            {
                if (QuaternionIsFinite(value))
                {
                    showorientation = value;
                    fakeori++;
                    _parent_scene.AddSimulationChange(() => changeOrientation(value));
                }
                else
                    MainConsole.Instance.Warn("[PHYSICS]: Got NaN quaternion Orientation from Scene in Object");
            }
        }

        public override Vector3 Acceleration
        {
            get { return _acceleration; }
        }

        public override Vector3 RotationalVelocity
        {
            get
            {
                if (_zeroFlag)
                    return Vector3.Zero;

                if (m_rotationalVelocity.ApproxEquals(Vector3.Zero, 0.00001f))
                    return Vector3.Zero;

                return m_rotationalVelocity;
            }
            set
            {
                if (value.IsFinite())
                    _parent_scene.AddSimulationChange(() => changeangvelocity(value));
                else
                    MainConsole.Instance.Warn("[PHYSICS]: Got NaN RotationalVelocity in Object");
            }
        }

        public override float Buoyancy
        {
            get { return m_buoyancy; }
            set { m_buoyancy = value; }
        }

        public override bool FloatOnWater
        {
            set { _parent_scene.AddSimulationChange(() => changefloatonwater(value)); }
        }

        public override void ClearVelocity()
        {
            _velocity = Vector3.Zero;
            _acceleration = Vector3.Zero;
            m_rotationalVelocity = Vector3.Zero;
            m_lastorientation = Orientation;
            m_lastposition = Position;
            m_lastVelocity = _velocity;
            if(Body != null && Body != IntPtr.Zero)
                d.BodySetLinearVel(Body, 0, 0, 0);
        }

        public override void ForceSetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
            m_lastVelocity = velocity;
            if (Body != IntPtr.Zero)
                d.BodySetLinearVel(Body, velocity.X, velocity.Y, velocity.Z);
        }

        public void ForceSetRotVelocity(Vector3 velocity)
        {
            m_rotationalVelocity = velocity;
            if (Body != IntPtr.Zero)
                d.BodySetAngularVel(Body, velocity.X, velocity.Y, velocity.Z);
        }

        public void SetGeom(IntPtr geom)
        {
            prim_geom = geom;
            //Console.WriteLine("SetGeom to " + prim_geom + " for " + m_primName);
            if (prim_geom != IntPtr.Zero)
            {
                d.GeomSetCategoryBits(prim_geom, (int)m_collisionCategories);
                d.GeomSetCollideBits(prim_geom, (int)m_collisionFlags);

                CalcPrimBodyData();

                _parent_scene.actor_name_map[prim_geom] = this;
            }

            if (childPrim)
            {
                if (_parent != null && _parent is AuroraODEPrim)
                {
                    AuroraODEPrim parent = (AuroraODEPrim)_parent;
                    //Console.WriteLine("SetGeom calls ChildSetGeom");
                    parent.ChildSetGeom(this);
                }
            }
            //MainConsole.Instance.Warn("Setting Geom to: " + prim_geom);
        }

        public void enableBodySoft()
        {
            if (!childPrim)
            {
                if (m_isphysical && Body != IntPtr.Zero)
                {
                    d.BodyEnable(Body);
                    if (m_vehicle.Type != Vehicle.TYPE_NONE)
                        m_vehicle.Enable(Body, this, _parent_scene);
                }

                m_disabled = false;
            }
        }

        public void disableBodySoft()
        {
            if (!childPrim)
            {
                m_disabled = true;
                m_vehicle.Disable(this);
                if (IsPhysical && Body != IntPtr.Zero)
                    d.BodyDisable(Body);
            }
        }

        private void MakeBody()
        {
            //            d.Vector3 dvtmp;
            //            d.Vector3 dbtmp;


            if (m_blockPhysicalReconstruction) // building is blocked
                return;

            if (childPrim) // child prims don't get own bodies;
                return;

            if (prim_geom == IntPtr.Zero)
            {
                MainConsole.Instance.Warn("[PHYSICS]: Unable to link the linkset.  Root has no geom yet");
                return;
            }

            if (!m_isphysical) // only physical things get a body
                return;

            if (Body != IntPtr.Zero) // who shouldn't have one already ?
            {
                d.BodyDestroy(Body);
                Body = IntPtr.Zero;
                MainConsole.Instance.Warn("[PHYSICS]: MakeBody called having a body");
            }


            d.Mass objdmass = new d.Mass { };
            d.Matrix3 mymat = new d.Matrix3();
            d.Quaternion myrot = new d.Quaternion();

            Body = d.BodyCreate(_parent_scene.world);

            DMassDup(ref primdMass, out objdmass);

            // rotate inertia
            myrot.X = _orientation.X;
            myrot.Y = _orientation.Y;
            myrot.Z = _orientation.Z;
            myrot.W = _orientation.W;

            d.RfromQ(out mymat, ref myrot);
            d.MassRotate(ref objdmass, ref mymat);

            // set the body rotation and position
            d.BodySetRotation(Body, ref mymat);

            // recompute full object inertia if needed
            if (childrenPrim.Count > 0)
            {
                d.Matrix3 mat = new d.Matrix3();
                d.Quaternion quat = new d.Quaternion();
                d.Mass tmpdmass = new d.Mass { };
                Vector3 rcm;

                rcm.X = _position.X + objdmass.c.X;
                rcm.Y = _position.Y + objdmass.c.Y;
                rcm.Z = _position.Z + objdmass.c.Z;

                lock (childrenPrim)
                {
                    foreach (AuroraODEPrim prm in childrenPrim)
                    {
                        if (prm.prim_geom == IntPtr.Zero)
                        {
                            MainConsole.Instance.Warn(
                                "[PHYSICS]: Unable to link one of the linkset elements, skipping it.  No geom yet");
                            continue;
                        }

                        DMassCopy(ref prm.primdMass, ref tmpdmass);

                        // apply prim current rotation to inertia
                        quat.W = prm._orientation.W;
                        quat.X = prm._orientation.X;
                        quat.Y = prm._orientation.Y;
                        quat.Z = prm._orientation.Z;
                        d.RfromQ(out mat, ref quat);
                        d.MassRotate(ref tmpdmass, ref mat);

                        Vector3 ppos = prm._position;
                        ppos.X += tmpdmass.c.X - rcm.X;
                        ppos.Y += tmpdmass.c.Y - rcm.Y;
                        ppos.Z += tmpdmass.c.Z - rcm.Z;

                        // refer inertia to root prim center of mass position
                        d.MassTranslate(ref tmpdmass,
                                        ppos.X,
                                        ppos.Y,
                                        ppos.Z);

                        d.MassAdd(ref objdmass, ref tmpdmass); // add to total object inertia

                        // fix prim colision cats

                        d.GeomClearOffset(prm.prim_geom);
                        d.GeomSetBody(prm.prim_geom, Body);
                        prm.Body = Body;
                        d.GeomSetOffsetWorldRotation(prm.prim_geom, ref mat); // set relative rotation
                    }
                }
            }

            d.GeomClearOffset(prim_geom); // make sure we don't have a hidden offset
            // associate root geom with body
            d.GeomSetBody(prim_geom, Body);

            d.BodySetPosition(Body, _position.X + objdmass.c.X, _position.Y + objdmass.c.Y, _position.Z + objdmass.c.Z);
            d.GeomSetOffsetWorldPosition(prim_geom, _position.X, _position.Y, _position.Z);

            d.MassTranslate(ref objdmass, -objdmass.c.X, -objdmass.c.Y, -objdmass.c.Z);
            // ode wants inertia at center of body
            myrot.W = -myrot.W;
            d.RfromQ(out mymat, ref myrot);
            d.MassRotate(ref objdmass, ref mymat);
            d.BodySetMass(Body, ref objdmass);
            _mass = objdmass.mass;

            m_collisionCategories |= CollisionCategories.Body;
            m_collisionFlags |= (CollisionCategories.Land | CollisionCategories.Wind);

            // disconnect from world gravity so we can apply buoyancy
            //            if (!testRealGravity)
            d.BodySetGravityMode(Body, false);

            d.BodySetAutoDisableFlag(Body, true);
            d.BodySetAutoDisableSteps(Body, body_autodisable_frames);
            d.BodySetDamping(Body, .001f, .0002f);
            m_disabled = false;

            d.GeomSetCategoryBits(prim_geom, (int)m_collisionCategories);
            d.GeomSetCollideBits(prim_geom, (int)m_collisionFlags);

            if (m_targetSpace != _parent_scene.space)
            {
                if (d.SpaceQuery(m_targetSpace, prim_geom))
                    d.SpaceRemove(m_targetSpace, prim_geom);

                m_targetSpace = _parent_scene.space;
                d.SpaceAdd(m_targetSpace, prim_geom);
            }

            lock (childrenPrim)
            {
                foreach (AuroraODEPrim prm in childrenPrim)
                {
                    if (prm.prim_geom == IntPtr.Zero)
                        continue;

                    Vector3 ppos = prm._position;
                    d.GeomSetOffsetWorldPosition(prm.prim_geom, ppos.X, ppos.Y, ppos.Z); // set relative position

                    prm.m_collisionCategories |= CollisionCategories.Body;
                    prm.m_collisionFlags |= (CollisionCategories.Land | CollisionCategories.Wind);
                    d.GeomSetCategoryBits(prm.prim_geom, (int)prm.m_collisionCategories);
                    d.GeomSetCollideBits(prm.prim_geom, (int)prm.m_collisionFlags);


                    if (prm.m_targetSpace != _parent_scene.space)
                    {
                        if (d.SpaceQuery(prm.m_targetSpace, prm.prim_geom))
                            d.SpaceRemove(prm.m_targetSpace, prm.prim_geom);

                        prm.m_targetSpace = _parent_scene.space;
                        d.SpaceAdd(m_targetSpace, prm.prim_geom);
                    }

                    prm.m_disabled = false;
                    _parent_scene.addActivePrim(prm);
                }
            }
            // The body doesn't already have a finite rotation mode set here
            if ((!m_angularlock.ApproxEquals(Vector3.One, 0.0f)) && _parent == null)
            {
                createAMotor(m_angularlock);
            }
            if (m_vehicle.Type != Vehicle.TYPE_NONE)
                m_vehicle.Enable(Body, this, _parent_scene);

            _parent_scene.addActivePrim(this);
        }

        private void SetInStaticSpace(AuroraODEPrim prm)
        {
            if (prm.m_targetSpace != null && prm.m_targetSpace == _parent_scene.space)
            {
                if (d.SpaceQuery(prm.m_targetSpace, prm.prim_geom))
                    d.SpaceRemove(prm.m_targetSpace, prm.prim_geom);
            }
            prm.m_targetSpace = _parent_scene.calculateSpaceForGeom(prm._position);
            d.SpaceAdd(prm.m_targetSpace, prm.prim_geom);
        }


        public void DestroyBody()
        // for now removes all colisions etc from childs, full body reconstruction is needed after this
        {
            //this kills the body so things like 'mesh' can re-create it.
            lock (this)
            {
                if (Body != IntPtr.Zero)
                {
                    _parent_scene.remActivePrim(this);
                    m_collisionCategories &= ~CollisionCategories.Body;
                    m_collisionFlags &= ~(CollisionCategories.Wind | CollisionCategories.Land);
                    if (prim_geom != IntPtr.Zero)
                    {
                        d.GeomSetCategoryBits(prim_geom, (int)m_collisionCategories);
                        d.GeomSetCollideBits(prim_geom, (int)m_collisionFlags);
                        UpdateDataFromGeom();
                        SetInStaticSpace(this);
                    }
                    if (!childPrim)
                    {
                        lock (childrenPrim)
                        {
                            foreach (AuroraODEPrim prm in childrenPrim)
                            {
                                _parent_scene.remActivePrim(prm);
                                prm.m_collisionCategories &= ~CollisionCategories.Body;
                                prm.m_collisionFlags &= ~(CollisionCategories.Wind | CollisionCategories.Land);
                                if (prm.prim_geom != IntPtr.Zero)
                                {
                                    d.GeomSetCategoryBits(prm.prim_geom, (int)m_collisionCategories);
                                    d.GeomSetCollideBits(prm.prim_geom, (int)m_collisionFlags);
                                    prm.UpdateDataFromGeom();
                                    prm.Body = IntPtr.Zero;
                                    SetInStaticSpace(prm);
                                }
                                prm._mass = prm.primMass;
                            }
                        }
                        m_vehicle.Disable(this);
                        d.BodyDestroy(Body);
                    }
                }
                Body = IntPtr.Zero;
            }
            _mass = primMass;
            m_disabled = true;
        }

        public bool setMesh(AuroraODEPhysicsScene parent_scene, IMesh mesh)
        {
            // This sleeper is there to moderate how long it takes between
            // setting up the mesh and pre-processing it when we get rapid fire mesh requests on a single object

            //Thread.Sleep(10);

            //Kill Body so that mesh can re-make the geom
            if (IsPhysical && Body != IntPtr.Zero)
            {
                if (childPrim)
                {
                    if (_parent != null)
                    {
                        AuroraODEPrim parent = (AuroraODEPrim)_parent;
                        parent.ChildDelink(this);
                    }
                }
                else
                {
                    DestroyBody();
                }
            }

            IntPtr vertices, indices;
            int vertexCount, indexCount;
            int vertexStride, triStride;
            mesh.getVertexListAsPtrToFloatArray(out vertices, out vertexStride, out vertexCount);
            // Note, that vertices are fixed in unmanaged heap
            mesh.getIndexListAsPtrToIntArray(out indices, out triStride, out indexCount);
            // Also fixed, needs release after usage

            if (vertexCount == 0 || indexCount == 0)
            {
                MainConsole.Instance.WarnFormat("[PHYSICS]: Got invalid mesh on prim at <{0},{1},{2}>. It can be a sculpt with alpha channel in map. Replacing it by a small box.", _position.X, _position.Y, _position.Z);
                _size.X = 0.01f;
                _size.Y = 0.01f;
                _size.Z = 0.01f;
                return false;
            }

            primOOBoffset = mesh.GetCentroid();
            hasOOBoffsetFromMesh = true;

            mesh.releaseSourceMeshData(); // free up the original mesh data to save memory
            if (m_MeshToTriMeshMap.ContainsKey(mesh.Key))
            {
                _triMeshData = m_MeshToTriMeshMap[mesh.Key];
            }
            else
            {
                _triMeshData = d.GeomTriMeshDataCreate();

                d.GeomTriMeshDataBuildSimple(_triMeshData, vertices, vertexStride, vertexCount, indices, indexCount,
                                             triStride);
                d.GeomTriMeshDataPreprocess(_triMeshData);
                m_MeshToTriMeshMap[mesh.Key] = _triMeshData;
            }

            try
            {
                if (prim_geom == IntPtr.Zero)
                {
                    SetGeom(d.CreateTriMesh(m_targetSpace, _triMeshData, null, null, null));
                }
            }
            catch (AccessViolationException)
            {
                MainConsole.Instance.Error("[PHYSICS]: MESH LOCKED");
                return false;
            }

            return true;
        }

        private void changeAngularLock(Vector3 newlock)
        {
            // do we have a Physical object?
            if (Body != IntPtr.Zero)
            {
                //Check that we have a Parent
                //If we have a parent then we're not authorative here
                if (_parent == null)
                {
                    if (!newlock.ApproxEquals(Vector3.One, 0f))
                    {
                        createAMotor(newlock);
                    }
                    else
                    {
                        if (Amotor != IntPtr.Zero)
                        {
                            d.JointDestroy(Amotor);
                            Amotor = IntPtr.Zero;
                        }
                    }
                }
            }
            // Store this for later in case we get turned into a separate body
            m_angularlock = newlock;
        }

        private void changelink(AuroraODEPrim newparent)
        {
            // If the newly set parent is not null
            // create link
            if (_parent == null && newparent != null)
            {
                newparent.ParentPrim(this);
            }
            // If the newly set parent is null
            // destroy link
            else if (_parent != null)
            {
                if (_parent is AuroraODEPrim)
                {
                    if (newparent != _parent)
                    {
                        AuroraODEPrim obj = (AuroraODEPrim)_parent;
                        obj.ChildDelink(this);
                        childPrim = false;

                        if (newparent != null)
                        {
                            newparent.ParentPrim(this);
                        }
                    }
                }
            }

            _parent = newparent;
        }

        // I'm the parent
        // prim is the child
        public void ParentPrim(AuroraODEPrim prim)
        {
            //Console.WriteLine("ParentPrim  " + m_primName);
            if (this.m_localID != prim.m_localID)
            {
                DestroyBody();

                lock (childrenPrim)
                {
#if (!ISWIN)
                    foreach (AuroraODEPrim prm in prim.childrenPrim)
                    {
                        if (!childrenPrim.Contains(prm))
                        {
                            childrenPrim.Add(prm);
                        }
                    }
#else
                    foreach (AuroraODEPrim prm in prim.childrenPrim.Where(prm => !childrenPrim.Contains(prm)))
                    {
                        childrenPrim.Add(prm);
                    }
#endif
                    if (!childrenPrim.Contains(prim)) // must allow full reconstruction
                        childrenPrim.Add(prim);
                }
                //Remove old children
                prim.childrenPrim.Clear();
                prim.childPrim = true;
                prim._parent = this;

                if (prim.Body != IntPtr.Zero)
                {
                    prim.DestroyBody(); // don't loose bodies around
                    prim.Body = IntPtr.Zero;
                }
                MakeBody(); // full nasty reconstruction
            }
        }

        private void ChildSetGeom(AuroraODEPrim odePrim)
        {
            DestroyBody();
            MakeBody();
        }

        private void UpdateChildsfromgeom()
        {
            if (childrenPrim.Count > 0)
            {
                foreach (AuroraODEPrim prm in childrenPrim)
                    prm.UpdateDataFromGeom();
            }
        }

        private void UpdateDataFromGeom()
        {
            if (prim_geom != IntPtr.Zero)
            {
                d.Vector3 lpos = d.GeomGetPosition(prim_geom);
                _position.X = lpos.X;
                _position.Y = lpos.Y;
                _position.Z = lpos.Z;
                d.Quaternion qtmp = new d.Quaternion
                {
                };
                d.GeomCopyQuaternion(prim_geom, out qtmp);
                _orientation.W = qtmp.W;
                _orientation.X = qtmp.X;
                _orientation.Y = qtmp.Y;
                _orientation.Z = qtmp.Z;
            }
        }

        private void ChildDelink(AuroraODEPrim odePrim)
        {
            // Okay, we have a delinked child.. destroy all body and remake
            if (odePrim != this && !childrenPrim.Contains(odePrim))
                return;

            DestroyBody();

            if (odePrim == this)
            {
                AuroraODEPrim newroot = null;
                lock (childrenPrim)
                {
                    if (childrenPrim.Count > 0)
                    {
                        newroot = childrenPrim[0];
                        childrenPrim.RemoveAt(0);
                        foreach (AuroraODEPrim prm in childrenPrim)
                        {
                            newroot.childrenPrim.Add(prm);
                        }
                        childrenPrim.Clear();
                    }
                    if (newroot != null)
                    {
                        newroot.childPrim = false;
                        newroot._parent = null;
                        newroot.MakeBody();
                    }
                }
            }

            else
            {
                lock (childrenPrim)
                {
                    childrenPrim.Remove(odePrim);
                    odePrim.childPrim = false;
                    odePrim._parent = null;
                    //odePrim.UpdateDataFromGeom ();
                    odePrim.MakeBody();
                }
            }

            MakeBody();
        }

        private void ChildRemove(AuroraODEPrim odePrim)
        {
            // Okay, we have a delinked child.. destroy all body and remake
            if (odePrim != this && !childrenPrim.Contains(odePrim))
                return;

            DestroyBody();

            if (odePrim == this)
            {
                AuroraODEPrim newroot = null;
                lock (childrenPrim)
                {
                    if (childrenPrim.Count > 0)
                    {
                        newroot = childrenPrim[0];
                        childrenPrim.RemoveAt(0);
                        foreach (AuroraODEPrim prm in childrenPrim)
                        {
                            newroot.childrenPrim.Add(prm);
                        }
                        childrenPrim.Clear();
                    }
                    if (newroot != null)
                    {
                        newroot.childPrim = false;
                        newroot._parent = null;
                        newroot.MakeBody();
                    }
                }
                return;
            }
            else
            {
                lock (childrenPrim)
                {
                    childrenPrim.Remove(odePrim);
                    odePrim.childPrim = false;
                    odePrim._parent = null;
                }
            }

            MakeBody();
        }

        private void changeSelectedStatus(bool newsel)
        {
            bool isphys = IsPhysical;

            if (newsel)
            {
                m_collisionCategories = CollisionCategories.Selected;
                m_collisionFlags = (CollisionCategories.Sensor | CollisionCategories.Space);

                // We do the body disable soft twice because 'in theory' a collision could have happened
                // in between the disabling and the collision properties setting
                // which would wake the physical body up from a soft disabling and potentially cause it to fall
                // through the ground.

                // NOTE FOR JOINTS: this doesn't always work for jointed assemblies because if you select
                // just one part of the assembly, the rest of the assembly is non-selected and still simulating,
                // so that causes the selected part to wake up and continue moving.

                // even if you select all parts of a jointed assembly, it is not guaranteed that the entire
                // assembly will stop simulating during the selection, because of the lack of atomicity
                // of select operations (their processing could be interrupted by a thread switch, causing
                // simulation to continue before all of the selected object notifications trickle down to
                // the physics engine).

                // e.g. we select 100 prims that are connected by joints. non-atomically, the first 50 are
                // selected and disabled. then, due to a thread switch, the selection processing is
                // interrupted and the physics engine continues to simulate, so the last 50 items, whose
                // selection was not yet processed, continues to simulate. this wakes up ALL of the 
                // first 50 again. then the last 50 are disabled. then the first 50, which were just woken
                // up, start simulating again, which in turn wakes up the last 50.

                if (isphys)
                {
                    disableBodySoft();
                }

                if (prim_geom != IntPtr.Zero)
                {
                    d.GeomSetCategoryBits(prim_geom, (int)m_collisionCategories);
                    d.GeomSetCollideBits(prim_geom, (int)m_collisionFlags);
                }

                if (isphys)
                {
                    disableBodySoft();
                }
            }
            else
            {
                m_collisionCategories = CollisionCategories.Geom;

                if (isphys)
                    m_collisionCategories |= CollisionCategories.Body;

                m_collisionFlags = m_default_collisionFlags;

                if (m_collidesLand)
                    m_collisionFlags |= CollisionCategories.Land;
                if (m_collidesWater)
                    m_collisionFlags |= CollisionCategories.Water;

                if (prim_geom != IntPtr.Zero)
                {
                    d.GeomSetCategoryBits(prim_geom, (int)m_collisionCategories);
                    d.GeomSetCollideBits(prim_geom, (int)m_collisionFlags);
                }
                if (isphys)
                {
                    if (Body != IntPtr.Zero)
                    {
                        d.BodySetLinearVel(Body, 0f, 0f, 0f);
                        d.BodySetAngularVel(Body, 0f, 0f, 0f);
                        d.BodySetForce(Body, 0, 0, 0);
                        d.BodySetTorque(Body, 0, 0, 0);
                        enableBodySoft();
                    }
                }
            }

            resetCollisionAccounting();
            m_isSelected = newsel;
            if (!m_isSelected)
            {
                _zeroFlag = false;
            }
        }

        //end changeSelectedStatus

        public void CreateGeom(IntPtr m_targetSpace, IMesh _mesh)
        {
            hasOOBoffsetFromMesh = false;

            bool havemesh = false;
            //Console.WriteLine("CreateGeom:");
            if (_mesh != null)
            {
                havemesh = setMesh(_parent_scene, _mesh); // this will give a mesh to non trivial known prims
            }

            if (!havemesh)
            {
                if (_pbs.ProfileShape == ProfileShape.HalfCircle && _pbs.PathCurve == (byte)Extrusion.Curve1
                    && _size.X == _size.Y && _size.Y == _size.Z)
                {
                    // it's a sphere
                    try
                    {
                        SetGeom(d.CreateSphere(m_targetSpace, _size.X * 0.5f));
                    }
                    catch (Exception e)
                    {
                        MainConsole.Instance.Warn("[PHYSICS]: Create sphere failed: {0}", e);
                        return;
                    }
                }
                else
                {
                    try
                    {
                        SetGeom(d.CreateBox(m_targetSpace, _size.X, _size.Y, _size.Z));
                    }
                    catch (Exception e)
                    {
                        MainConsole.Instance.Warn("[PHYSICS]: Create box failed: {0}", e);
                        return;
                    }
                }
            }
        }

        private void RemoveGeom()
        {
            if (prim_geom != IntPtr.Zero)
            {
                _parent_scene.actor_name_map.Remove(prim_geom);
                try
                {
                    d.GeomDestroy(prim_geom);
                    /*
                                        if (_triMeshData != IntPtr.Zero)
                                        {
                                            d.GeomTriMeshDataDestroy(_triMeshData);
                                            _triMeshData = IntPtr.Zero;
                                        }
                     */
                }

                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("[PHYSICS]: PrimGeom destruction failed {1}", e);
                }

                prim_geom = IntPtr.Zero;
            }
            else
            {
                MainConsole.Instance.ErrorFormat("[PHYSICS]: PrimGeom destruction BAD");
            }
            Body = IntPtr.Zero;
            hasOOBoffsetFromMesh = false;
            CalcPrimBodyData();
        }

        public void changeadd()
        {
            // all prims are now created non physical
            IntPtr targetspace = _parent_scene.calculateSpaceForGeom(_position);
            m_targetSpace = targetspace;

            if (_mesh == null)
            {
                if (_parent_scene.needsMeshing(_parent_entity))
                {
                    // Don't need to re-enable body..   it's done in SetMesh
                    _mesh = _parent_scene.mesher.CreateMesh(_parent_entity.Name, _pbs, _size,
                                                            _parent_scene.meshSculptLOD, true);

                    //Tell things above if they want to cache it or something
                    if(_mesh != null)
                        _parent_entity.ParentEntity.GeneratedMesh(_parent_entity, _mesh);
                    // createmesh returns null when it's a shape that isn't a cube.
                    // MainConsole.Instance.Debug(m_localID);

                    // Remove the reference to any JPEG2000 sculpt data so it can be GCed
                    _pbs.SculptData = null;
                }
            }


            //Console.WriteLine("changeadd 1");
            CreateGeom(m_targetSpace, _mesh);
            CalcPrimBodyData();

            if (prim_geom != IntPtr.Zero)
            {
                d.GeomSetPosition(prim_geom, _position.X, _position.Y, _position.Z);
                d.Quaternion myrot = new d.Quaternion();
                Quaternion fake = _orientation;
                myrot.X = fake.X;
                myrot.Y = fake.Y;
                myrot.Z = fake.Z;
                myrot.W = fake.W;
                d.GeomSetQuaternion(prim_geom, ref myrot);
                //                    _parent_scene.actor_name_map[prim_geom] = (PhysicsActor)this;
            }

            changeSelectedStatus(m_isSelected);
        }

        public void changePosition(Vector3 newpos)
        {
            if (m_isphysical)
            {
                if (childPrim)
                {
                    if (m_blockPhysicalReconstruction) // inertia is messed, must rebuild
                    {
                        _position = newpos;
                    }
                }
                else
                {
                    if (newpos != _position)
                    {
                        d.GeomSetPosition(prim_geom, newpos.X, newpos.Y, newpos.Z);
                        _position = newpos;
                    }
                }
                if (Body != IntPtr.Zero)
                    d.BodyEnable(Body);
            }

            else
            {
                if (newpos != _position)
                {
                    IntPtr tempspace = _parent_scene.recalculateSpaceForGeom(prim_geom, newpos, m_targetSpace);
                    m_targetSpace = tempspace;
                    d.GeomSetPosition(prim_geom, newpos.X, newpos.Y, newpos.Z);
                    d.SpaceAdd(m_targetSpace, prim_geom);
                    _position = newpos;
                }
            }

            if (--fakepos < 0)
                fakepos = 0;

            changeSelectedStatus(m_isSelected);
            resetCollisionAccounting();
        }

        public void changeOrientation(Quaternion newrot)
        {
            if (m_isphysical)
            {
                if (childPrim)
                {
                    if (m_blockPhysicalReconstruction) // inertia is messed, must rebuild
                    {
                        _orientation = newrot;
                    }
                }
                else
                {
                    if (newrot != _orientation)
                    {
                        d.Quaternion myrot = new d.Quaternion();
                        Quaternion fake = newrot;
                        myrot.X = fake.X;
                        myrot.Y = fake.Y;
                        myrot.Z = fake.Z;
                        myrot.W = fake.W;
                        d.GeomSetQuaternion(prim_geom, ref myrot);
                        _orientation = newrot;
                        if (Body != IntPtr.Zero && !m_angularlock.ApproxEquals(Vector3.One, 0f))
                            createAMotor(m_angularlock);
                    }
                }
                if (Body != IntPtr.Zero)
                    d.BodyEnable(Body);
            }

            else
            {
                if (newrot != _orientation)
                {
                    d.Quaternion myrot = new d.Quaternion();
                    Quaternion fake = newrot;
                    myrot.X = fake.X;
                    myrot.Y = fake.Y;
                    myrot.Z = fake.Z;
                    myrot.W = fake.W;
                    d.GeomSetQuaternion(prim_geom, ref myrot);
                    _orientation = newrot;
                }
            }
            if (--fakeori < 0)
                fakeori = 0;

            changeSelectedStatus(m_isSelected);
            resetCollisionAccounting();
        }

        /*
                public void changemoveandrotate(Vector3 newpos, Quaternion newrot)
                {
                    if (m_isphysical)
                    {
                        if (childPrim)
                        {
                            if (m_blockPhysicalReconstruction)  // inertia is messed, must rebuild
                            {
                                _orientation = newrot;
                                _position = newpos;
                            }
                        }
                        else
                        {
                            if (newrot != _orientation)
                            {
                                d.Quaternion myrot = new d.Quaternion();
                                Quaternion fake = newrot;
                                myrot.X = fake.X;
                                myrot.Y = fake.Y;
                                myrot.Z = fake.Z;
                                myrot.W = fake.W;
                                d.GeomSetQuaternion(prim_geom, ref myrot);
                                _orientation = newrot;
                                if (Body != IntPtr.Zero && !m_angularlock.ApproxEquals(Vector3.One, 0f))
                                    createAMotor(m_angularlock);
                            }
                            if (newpos != _position)
                            {
                                d.GeomSetPosition(prim_geom, newpos.X, newpos.Y, newpos.Z);
                                _position = newpos;
                            }
                        }
                        if (Body != IntPtr.Zero)
                            d.BodyEnable(Body);
                    }

                    else
                    {
                        if (newrot != _orientation)
                        {
                            d.Quaternion myrot = new d.Quaternion();
                            Quaternion fake = newrot;
                            myrot.X = fake.X;
                            myrot.Y = fake.Y;
                            myrot.Z = fake.Z;
                            myrot.W = fake.W;
                            d.GeomSetQuaternion(prim_geom, ref myrot);
                            _orientation = newrot;
                        }
                        if (newpos != _position)
                        {
                            _parent_scene.waitForSpaceUnlock(m_targetSpace);
                            IntPtr tempspace = _parent_scene.recalculateSpaceForGeom(prim_geom, newpos, m_targetSpace);
                            m_targetSpace = tempspace;
                            d.GeomSetPosition(prim_geom, newpos.X, newpos.Y, newpos.Z);
                            d.SpaceAdd(m_targetSpace, prim_geom);
                            _position = newpos;
                        }
                    }

                    if (--fakepos < 0)
                        fakepos = 0;
                    if (--fakeori < 0)
                        fakeori = 0;

                    changeSelectedStatus(m_isSelected);

                    resetCollisionAccounting();
                }
        */

        public void Move(float timestep)
        {
            if (m_isphysical && Body != IntPtr.Zero && !m_isSelected && !childPrim && !m_blockPhysicalReconstruction)
            // KF: Only move root prims.
            {
                float fx = 0;
                float fy = 0;
                float fz = 0;

                if (m_vehicle.Type != Vehicle.TYPE_NONE)
                {
                    // 'VEHICLES' are dealt with in ODEDynamics.cs
                    m_vehicle.Step(Body, timestep, _parent_scene, this);
                    if (m_disabled || m_frozen)
                    {
                        d.BodySetForce(Body, 0, 0, 0);
                        d.BodySetLinearVel(Body, 0, 0, 0);
                        d.BodySetAngularVel(Body, 0, 0, 0);
                        _parent_scene.BadPrim(this.childPrim ? (AuroraODEPrim)_parent : this);
                    }
                    else
                    {
                        /*
                        d.Vector3 vel = d.BodyGetLinearVel (Body);
                        m_lastVelocity = _velocity;
                        _velocity = new Vector3 ((float)vel.X, (float)vel.Y, (float)vel.Z);
                        d.Vector3 pos = d.BodyGetPosition(Body);
                        m_lastposition = _position;
                        _position = new Vector3 ((float)pos.X, (float)pos.Y, (float)pos.Z);
                        d.Quaternion ori;
                        _zeroFlag = false;

                        _acceleration = ((_velocity - m_lastVelocity) / timestep);
                        //MainConsole.Instance.Info ("[PHYSICS]: P1: " + _position + " V2: " + m_lastposition + " Acceleration: " + _acceleration.ToString ());
                        d.GeomCopyQuaternion (prim_geom, out ori);
                        _orientation.X = ori.X;
                        _orientation.Y = ori.Y;
                        _orientation.Z = ori.Z;
                        _orientation.W = ori.W;
                        d.Vector3 rotvel = d.BodyGetAngularVel (Body);
                        m_rotationalVelocity.X = (float)rotvel.X;
                        m_rotationalVelocity.Y = (float)rotvel.Y;
                        m_rotationalVelocity.Z = (float)rotvel.Z;
                         */
                    }
                }
                else
                {
                    Vector3 dcpos = d.BodyGetPosition(Body).ToVector3();
                    Vector3 vel = d.BodyGetLinearVel(Body).ToVector3();

                    Vector3 gravForce = new Vector3();
                    _parent_scene.CalculateGravity(_mass, dcpos, true,
                        (1.0f - m_buoyancy) * _parent_entity.GravityMultiplier, ref gravForce);

                    fx *= _mass;
                    fy *= _mass;
                    fz *= _mass;

                    fx += gravForce.X;
                    fy += gravForce.Y;
                    fz += gravForce.Z;

                    fx += m_force.X;
                    fy += m_force.Y;
                    fz += m_force.Z;

                    fx += m_pushForce.X * 10;
                    fy += m_pushForce.Y * 10;
                    fz += m_pushForce.Z * 10;
                    m_pushForce = Vector3.Zero;

                    #region drag and forces accumulators

                    fx += m_forceacc.X;
                    fy += m_forceacc.Y;
                    fz += m_forceacc.Z;
                    m_forceacc = Vector3.Zero;

                    Vector3 newtorque;
                    newtorque.X = m_angularforceacc.X;
                    newtorque.Y = m_angularforceacc.Y;
                    newtorque.Z = m_angularforceacc.Z;
                    m_angularforceacc = Vector3.Zero;

                    #endregion

                    if (Math.Abs(fx) < 0.01)
                        fx = 0;
                    if (Math.Abs(fy) < 0.01)
                        fy = 0;
                    if (Math.Abs(fz) < 0.01)
                        fz = 0;

                    if (!d.BodyIsEnabled(Body))
                        d.BodyEnable(Body);

                    if (fx != 0 || fy != 0 || fz != 0)
                        d.BodyAddForce(Body, fx, fy, fz);

                    if (newtorque.X != 0 || newtorque.Y != 0 || newtorque.Z != 0)
                        d.BodyAddTorque(Body, newtorque.X, newtorque.Y, newtorque.Z);
                }
            }
        }

        public void UpdatePositionAndVelocity(float timestep)
        {
            if (m_frozen)
                return;
            //  no lock; called from Simulate() -- if you call this from elsewhere, gotta lock or do Monitor.Enter/Exit!
            if (_parent == null)
            {
                if (Body != IntPtr.Zero && prim_geom != IntPtr.Zero)
                {
                    d.Vector3 cpos = d.BodyGetPosition(Body); // object position ( center of mass)
                    d.Vector3 lpos = d.GeomGetPosition(prim_geom); // root position that is seem by rest of simulator

                    #region Crossing failures

                    if (cpos.X > ((int)_parent_scene.WorldExtents.X - 0.05f) ||
                        cpos.X < 0f ||
                        cpos.Y > ((int)_parent_scene.WorldExtents.Y - 0.05f) ||
                        cpos.Y < 0f ||
                        cpos.Z < -100 ||
                        cpos.Z > 100000)
                    {
                        if (m_crossingfailures < _parent_scene.geomCrossingFailuresBeforeOutofbounds)
                        {
                            _position.X = lpos.X;
                            _position.Y = lpos.Y;
                            _position.Z = lpos.Z;
                            m_crossingfailures++;

                            m_lastposition = _position;
                            m_lastorientation = _orientation;

                            base.RequestPhysicsterseUpdate();
                            m_crossingfailures = 0;
                            return;
                        }
                        else
                        {
                            if (m_vehicle.Type == Vehicle.TYPE_NONE)
                            {
                                m_disabled = true;
                                m_frozen = true;

                                Vector3 l_position;
                                l_position.X = lpos.X;
                                l_position.Y = lpos.Y;
                                l_position.Z = lpos.Z;

                                base.RaiseOutOfBounds(l_position);
                                m_crossingfailures = 0;
                                return;
                            }
                            else
                            {
                                Vector3 newPos = Position;
                                newPos.X = Util.Clip(Position.X, 0.75f, _parent_scene.Region.RegionSizeX - 0.75f);
                                newPos.Y = Util.Clip(Position.Y, 0.75f, _parent_scene.Region.RegionSizeY - 0.75f);
                                Position = newPos;
                                d.BodySetPosition(Body, newPos.X, newPos.Y, newPos.Z);
                                m_crossingfailures = 0;
                            }
                        }
                    }

                    #endregion

                    #region Out of bounds

                    if (cpos.Z < 0 ||
                        (cpos.Z > _parent_scene.m_flightCeilingHeight && _parent_scene.m_useFlightCeilingHeight))
                    {
                        // This is so prim that get lost underground don't fall forever and suck up
                        //
                        // Sim resources and memory.
                        // Disables the prim's movement physics....
                        // It's a hack and will generate a console message if it fails.

                        //IsPhysical = false;
                        base.RaiseOutOfBounds(_position);

                        cpos.Z = cpos.Z < 0 ? 0 : _parent_scene.m_flightCeilingHeight;

                        _acceleration.X = 0;
                        _acceleration.Y = 0;
                        _acceleration.Z = 0;

                        _velocity.X = 0;
                        _velocity.Y = 0;
                        _velocity.Z = 0;
                        m_rotationalVelocity.X = 0;
                        m_rotationalVelocity.Y = 0;
                        m_rotationalVelocity.Z = 0;

                        d.BodySetLinearVel(Body, 0, 0, 0); // stop it
                        d.BodySetAngularVel(Body, 0, 0, 0); // stop it
                        d.BodySetPosition(Body, cpos.X, cpos.Y, cpos.Z); // put it somewhere 

                        m_lastposition = _position;
                        m_lastorientation = _orientation;
                        base.RequestPhysicsterseUpdate();

                        m_throttleUpdates = true;
                        _zeroFlag = true;
                        m_frozen = true;
                        return;
                    }

                    #endregion

                    d.Quaternion ori;
                    d.GeomCopyQuaternion(prim_geom, out ori);

                    if ((Math.Abs(m_lastposition.X - lpos.X) < 0.01)
                        && (Math.Abs(m_lastposition.Y - lpos.Y) < 0.01)
                        && (Math.Abs(m_lastposition.Z - lpos.Z) < 0.01)
                        && (Math.Abs(m_lastorientation.X - ori.X) < 0.001)
                        && (Math.Abs(m_lastorientation.Y - ori.Y) < 0.001)
                        && (Math.Abs(m_lastorientation.Z - ori.Z) < 0.001)
                        )
                    {
                        _zeroFlag = true;
                        if (!_zeroFlagForceSet)
                        {
                            _zeroFlagForceSet = true;
                            m_lastUpdateSent = 2;
                        }
                    }
                    else
                    {
                        _zeroFlagForceSet = false;
                        _zeroFlag = false;
                        m_lastUpdateSent = 2;
                    }

                    bool needupdate = false;


                    if (_zeroFlag)
                    {
                        if (m_lastUpdateSent > 0)
                        {
                            //Keep the velocity, it won't be sent anywhere outside of the physics engine because of the _zeroFlag checks
                            //And this allows us to keep the _zeroFlag check a bit more stable

                            _velocity.X = 0.0f;
                            _velocity.Y = 0.0f;
                            _velocity.Z = 0.0f;

                            _acceleration.X = 0;
                            _acceleration.Y = 0;
                            _acceleration.Z = 0;

                            m_rotationalVelocity.X = 0;
                            m_rotationalVelocity.Y = 0;
                            m_rotationalVelocity.Z = 0;

                            // better let ode keep dealing with small values --Ubit
                            //ODE doesn't deal with them though, it just keeps adding them, never stopping the movement of the prim..
                            // its supposed to!
                            /*
                                                        d.BodySetLinearVel(Body, 0, 0, 0);
                                                        d.BodySetAngularVel(Body, 0, 0, 0);
                                                        d.BodySetForce(Body, 0, 0, 0);
                            */
                            needupdate = true;
                            m_lastUpdateSent--;
                        }
                    }
                    else
                    {
                        _position.X = lpos.X;
                        _position.Y = lpos.Y;
                        _position.Z = lpos.Z;

                        _orientation.X = ori.X;
                        _orientation.Y = ori.Y;
                        _orientation.Z = ori.Z;
                        _orientation.W = ori.W;

                        d.Vector3 vel = d.BodyGetLinearVel(Body);
                        d.Vector3 rotvel = d.BodyGetAngularVel(Body);

                        _velocity.X = vel.X;
                        _velocity.Y = vel.Y;
                        _velocity.Z = vel.Z;

                        _acceleration = ((_velocity - m_lastVelocity) / timestep);

                        //MainConsole.Instance.Info("[PHYSICS]: V1: " + _velocity + " V2: " + m_lastVelocity + " Acceleration: " + _acceleration.ToString());

                        m_rotationalVelocity.X = rotvel.X;
                        m_rotationalVelocity.Y = rotvel.Y;
                        m_rotationalVelocity.Z = rotvel.Z;
                        needupdate = true;
                    }

                    m_lastVelocity = _velocity; // for accelaration

                    if (needupdate)
                    {
                        m_lastposition = _position;
                        m_lastorientation = _orientation;
                        base.RequestPhysicsterseUpdate();
                    }
                }
                else
                {
                    // Not a body..   so Make sure the client isn't interpolating
                    _velocity.X = 0;
                    _velocity.Y = 0;
                    _velocity.Z = 0;

                    _acceleration.X = 0;
                    _acceleration.Y = 0;
                    _acceleration.Z = 0;

                    m_rotationalVelocity.X = 0;
                    m_rotationalVelocity.Y = 0;
                    m_rotationalVelocity.Z = 0;
                    _zeroFlag = true;
                    m_frozen = true;
                }
            }
        }

        private d.Quaternion ConvertTodQuat(Quaternion q)
        {
            d.Quaternion dq = new d.Quaternion { X = q.X, Y = q.Y, Z = q.Z, W = q.W };
            return dq;
        }

        internal void resetCollisionAccounting()
        {
            m_collisionscore = 0;
            m_disabled = false;
        }

        public void changedisable()
        {
            m_disabled = true;
            if (Body != IntPtr.Zero)
            {
                d.BodyDisable(Body);
                Body = IntPtr.Zero;
            }
        }

        public void changePhysicsStatus(bool newphys)
        {
            m_isphysical = newphys;
            if (!childPrim)
            {
                if (newphys)
                {
                    if (Body == IntPtr.Zero)
                    {
                        if (_pbs.SculptEntry && _parent_scene.meshSculptedPrim)
                        {
                            changeshape(_pbs);
                        }
                        else
                        {
                            MakeBody();
                        }
                    }
                }
                else
                {
                    if (Body != IntPtr.Zero)
                    {
                        //UpdateChildsfromgeom ();
                        if (_pbs.SculptEntry && _parent_scene.meshSculptedPrim)
                        {
                            changeshape(_pbs);
                        }
                        else
                            DestroyBody();
                    }
                }
            }

            changeSelectedStatus(m_isSelected);
            resetCollisionAccounting();
            FirePhysicalRepresentationChanged();
        }


        public void changefloatonwater(bool arg)
        {
            m_collidesWater = arg;

            if (prim_geom != IntPtr.Zero)
            {
                if (m_collidesWater)
                    m_collisionFlags |= CollisionCategories.Water;
                else
                    m_collisionFlags &= ~CollisionCategories.Water;
                d.GeomSetCollideBits(prim_geom, (int)m_collisionFlags);
            }
        }


        public void changeprimsizeshape()
        {
            _parent_scene.actor_name_map.Remove(prim_geom);
            AuroraODEPrim parent = null;

            bool chp = childPrim;
            if (chp)
                parent = (AuroraODEPrim)_parent;

            // Cleanup of old prim geometry and Bodies
            if (chp)
            {
                if (parent != null)
                    parent.DestroyBody();
            }
            else
            {
                DestroyBody();
            }

            if (prim_geom != IntPtr.Zero)
            {
                try
                {
                    d.GeomDestroy(prim_geom);
                }
                catch (AccessViolationException)
                {
                    prim_geom = IntPtr.Zero;
                    MainConsole.Instance.Error("[PHYSICS]: PrimGeom dead");
                }
                prim_geom = IntPtr.Zero;
            }
            // we don't need to do space calculation because the client sends a position update also.
            if (_size.X <= 0)
                _size.X = 0.01f;
            if (_size.Y <= 0)
                _size.Y = 0.01f;
            if (_size.Z <= 0)
                _size.Z = 0.01f;
            // Construction of new prim


            if (_parent_scene.needsMeshing(_parent_entity))
            {
                float meshlod = _parent_scene.meshSculptLOD;
                IMesh mesh = _parent_scene.mesher.CreateMesh(_parent_entity.Name, _pbs, _size, meshlod, true);
                // createmesh returns null when it doesn't mesh.
                CreateGeom(m_targetSpace, mesh);
            }
            else
            {
                _mesh = null;
                //Console.WriteLine("changeshape");
                CreateGeom(m_targetSpace, null);
            }

            CalcPrimBodyData();

            if (prim_geom != IntPtr.Zero)
            {
                d.GeomSetPosition(prim_geom, _position.X, _position.Y, _position.Z);
                d.Quaternion myrot = new d.Quaternion();
                Quaternion fake = _orientation;
                myrot.X = fake.X;
                myrot.Y = fake.Y;
                myrot.Z = fake.Z;
                myrot.W = fake.W;
                d.GeomSetQuaternion(prim_geom, ref myrot);

                _parent_scene.actor_name_map[prim_geom] = this;
            }

            changeSelectedStatus(m_isSelected);

            if (chp)
            {
                if (parent != null)
                {
                    parent.MakeBody();
                }
            }
            else
                MakeBody();
            ;
        }

        public void changeshape(PrimitiveBaseShape arg)
        {
            _pbs = arg;
            changeprimsizeshape();
        }

        public void changesize(Vector3 arg)
        {
            _size = arg;
            changeprimsizeshape();
        }

        public void changeAddForce(object arg)
        {
            if (!m_isSelected)
            {
                if (IsPhysical)
                {
                    if (m_vehicle.Type == Vehicle.TYPE_NONE)
                        m_forceacc += (Vector3)arg * 100;
                    else
                        m_vehicle.ProcessForceTaint((Vector3)arg);
                }
            }
        }

        public void changeSetTorque(Vector3 newtorque)
        {
            if (!m_isSelected)
            {
                if (IsPhysical && Body != IntPtr.Zero)
                {
                    d.BodySetTorque(Body, newtorque.X, newtorque.Y, newtorque.Z);
                }
            }
        }

        public void changeAddAngularForce(Vector3 arg)
        {
            if (!m_isSelected && IsPhysical)
                m_angularforceacc += arg * 100;
        }

        private void changevelocity(Vector3 arg)
        {
            _velocity = arg;
            if (!m_isSelected)
            {
                if (IsPhysical)
                {
                    if (Body != IntPtr.Zero)
                    {
                        d.BodySetLinearVel(Body, _velocity.X, _velocity.Y, _velocity.Z);
                    }
                }

                //resetCollisionAccounting();
            }
        }

        public void setPrimForRemoval()
        {
            if (m_primIsRemoved)
                return; //Already being removed

            m_primIsRemoved = true;
            lock (childrenPrim)
            {
                foreach (AuroraODEPrim prm in childrenPrim)
                {
                    prm.m_primIsRemoved = true;
                }
            }

            _parent_scene.AddSimulationChange(() =>
            {
                if (_parent != null)
                {
                    AuroraODEPrim parent = (AuroraODEPrim)_parent;
                    parent.ChildRemove(this);
                }
                else
                    ChildRemove(this);

                RemoveGeom();
                m_targetSpace = IntPtr.Zero;
                _parent_scene.RemovePrimThreadLocked(this);
            });
        }

        public void setPrimForDeletion()
        {
            if (m_primIsRemoved)
                return; //Already being removed

            m_primIsRemoved = true;
            lock (childrenPrim)
            {
                foreach (AuroraODEPrim prm in childrenPrim)
                {
                    prm.m_primIsRemoved = true;
                }
            }

            _parent_scene.AddSimulationChange(() => deletePrimLocked());
        }

        private void deletePrimLocked()
        {
            if (_parent != null)
            {
                AuroraODEPrim parent = (AuroraODEPrim)_parent;
                parent.DestroyBody();
            }
            else
                DestroyBody();

            RemoveGeom();
            m_targetSpace = IntPtr.Zero;
            _parent_scene.RemovePrimThreadLocked(this);
        }

        public override void VehicleFloatParam(int param, float value)
        {
            strVehicleFloatParam strf = new strVehicleFloatParam { param = param, value = value };
            _parent_scene.AddSimulationChange(() => changeVehicleFloatParam(strf.param, strf.value));
        }

        public override void VehicleVectorParam(int param, Vector3 value)
        {
            strVehicleVectorParam strv = new strVehicleVectorParam { param = param, value = value };
            _parent_scene.AddSimulationChange(() => changeVehicleVectorParam(strv.param, strv.value));
        }

        public override void VehicleRotationParam(int param, Quaternion rotation)
        {
            strVehicleQuartParam strq = new strVehicleQuartParam { param = param, value = rotation };
            _parent_scene.AddSimulationChange(() => changeVehicleRotationParam(strq.param, strq.value));
        }

        public override void VehicleFlags(int param, bool remove)
        {
            strVehicleBoolParam strb = new strVehicleBoolParam { param = param, value = remove };
            _parent_scene.AddSimulationChange(() => changeVehicleFlags(strb.param, strb.value));
        }

        public override void SetCameraPos(Quaternion CameraRotation)
        {
            _parent_scene.AddSimulationChange(() => changeSetCameraPos(CameraRotation));
        }

        internal static bool QuaternionIsFinite(Quaternion q)
        {
            if (Single.IsNaN(q.X) || Single.IsInfinity(q.X))
                return false;
            if (Single.IsNaN(q.Y) || Single.IsInfinity(q.Y))
                return false;
            if (Single.IsNaN(q.Z) || Single.IsInfinity(q.Z))
                return false;
            if (Single.IsNaN(q.W) || Single.IsInfinity(q.W))
                return false;
            return true;
        }

        public override void AddForce(Vector3 force, bool pushforce)
        {
            if (force.IsFinite())
            {
                _parent_scene.AddSimulationChange(() => changeforce(force, pushforce));
            }
            else
            {
                MainConsole.Instance.Warn("[PHYSICS]: Got Invalid linear force vector from Scene in Object");
            }
            //MainConsole.Instance.Info("[PHYSICS]: Added Force:" + force.ToString() +  " to prim at " + Position.ToString());
        }

        public override void AddAngularForce(Vector3 force, bool pushforce)
        {
            if (force.IsFinite())
                _parent_scene.AddSimulationChange(() => changeAddAngularForce(force));
            else
                MainConsole.Instance.Warn("[PHYSICS]: Got Invalid Angular force vector from Scene in Object");
        }

        public override void CrossingFailure()
        {
            m_crossingfailures++;
            if (m_crossingfailures > _parent_scene.geomCrossingFailuresBeforeOutofbounds)
            {
                base.RaiseOutOfBounds(_position);
                return;
            }
            else if (m_crossingfailures == _parent_scene.geomCrossingFailuresBeforeOutofbounds)
            {
                MainConsole.Instance.Warn("[PHYSICS]: Too many crossing failures for: " + _parent_entity.Name + " @ " +
                           _parent_entity.AbsolutePosition);
            }
        }

        public override void link(PhysicsObject obj)
        {
            _parent_scene.AddSimulationChange(() => changelink((AuroraODEPrim)obj));
        }

        public override void delink()
        {
            _parent_scene.AddSimulationChange(() => changelink(null));
        }

        public override void LockAngularMotion(Vector3 axis)
        {
            // reverse the zero/non zero values for ODE.
            if (axis.IsFinite())
            {
                axis.X = (axis.X > 0) ? 1f : 0f;
                axis.Y = (axis.Y > 0) ? 1f : 0f;
                axis.Z = (axis.Z > 0) ? 1f : 0f;
                MainConsole.Instance.DebugFormat("[axislock]: <{0},{1},{2}>", axis.X, axis.Y, axis.Z);
                _parent_scene.AddSimulationChange(() => changeAngularLock(axis));
            }
            else
            {
                MainConsole.Instance.Warn("[PHYSICS]: Got NaN locking axis from Scene on Object");
            }
        }

        private void createAMotor(Vector3 axis)
        {
            if (Body == IntPtr.Zero)
                return;

            if (Amotor != IntPtr.Zero)
            {
                d.JointDestroy(Amotor);
                Amotor = IntPtr.Zero;
            }

            int axisnum = 3 - (int)(axis.X + axis.Y + axis.Z);

            if (axisnum <= 0)
                return;

            // stop it
            d.BodySetTorque(Body, 0, 0, 0);
            d.BodySetAngularVel(Body, 0, 0, 0);

            Amotor = d.JointCreateAMotor(_parent_scene.world, IntPtr.Zero);
            d.JointAttach(Amotor, Body, IntPtr.Zero);

            d.JointSetAMotorMode(Amotor, 0);

            d.JointSetAMotorNumAxes(Amotor, axisnum);

            // get current orientation to lock

            d.Quaternion dcur = d.BodyGetQuaternion(Body);
            Quaternion curr; // crap convertion between identical things
            curr.X = dcur.X;
            curr.Y = dcur.Y;
            curr.Z = dcur.Z;
            curr.W = dcur.W;
            Vector3 ax;

            int i = 0;
            int j = 0;
            if (axis.X == 0)
            {
                ax = (new Vector3(1, 0, 0)) * curr; // rotate world X to current local X
                // ODE should do this  with axis relative to body 1 but seems to fail
                d.JointSetAMotorAxis(Amotor, 0, 0, ax.X, ax.Y, ax.Z);
                d.JointSetAMotorAngle(Amotor, 0, 0);
                d.JointSetAMotorParam(Amotor, (int)d.JointParam.LoStop, -0.000001f);
                d.JointSetAMotorParam(Amotor, (int)d.JointParam.HiStop, 0.000001f);
                d.JointSetAMotorParam(Amotor, (int)d.JointParam.Vel, 0);
                d.JointSetAMotorParam(Amotor, (int)d.JointParam.FudgeFactor, 0.0001f);
                d.JointSetAMotorParam(Amotor, (int)d.JointParam.Bounce, 0f);
                d.JointSetAMotorParam(Amotor, (int)d.JointParam.FMax, 550000000);
                i++;
                j = 256; // aodeplugin.cs doesn't have all parameters so this moves to next axis set
            }

            if (axis.Y == 0)
            {
                ax = (new Vector3(0, 1, 0)) * curr;
                d.JointSetAMotorAxis(Amotor, i, 0, ax.X, ax.Y, ax.Z);
                d.JointSetAMotorAngle(Amotor, i, 0);
                d.JointSetAMotorParam(Amotor, j + (int)d.JointParam.LoStop, -0.000001f);
                d.JointSetAMotorParam(Amotor, j + (int)d.JointParam.HiStop, 0.000001f);
                d.JointSetAMotorParam(Amotor, j + (int)d.JointParam.Vel, 0);
                d.JointSetAMotorParam(Amotor, j + (int)d.JointParam.FudgeFactor, 0.0001f);
                d.JointSetAMotorParam(Amotor, j + (int)d.JointParam.Bounce, 0f);
                d.JointSetAMotorParam(Amotor, j + (int)d.JointParam.FMax, 550000000);
                i++;
                j += 256;
            }

            if (axis.Z == 0)
            {
                ax = (new Vector3(0, 0, 1)) * curr;
                d.JointSetAMotorAxis(Amotor, i, 0, ax.X, ax.Y, ax.Z);
                d.JointSetAMotorAngle(Amotor, i, 0);
                d.JointSetAMotorParam(Amotor, j + (int)d.JointParam.LoStop, -0.000001f);
                d.JointSetAMotorParam(Amotor, j + (int)d.JointParam.HiStop, 0.000001f);
                d.JointSetAMotorParam(Amotor, j + (int)d.JointParam.Vel, 0);
                d.JointSetAMotorParam(Amotor, j + (int)d.JointParam.FudgeFactor, 0.0001f);
                d.JointSetAMotorParam(Amotor, j + (int)d.JointParam.Bounce, 0f);
                d.JointSetAMotorParam(Amotor, j + (int)d.JointParam.FMax, 550000000);
            }

            d.JointAddAMotorTorques(Amotor, 0.001f, 0.001f, 0.001f);
        }

        public Matrix4 FromDMass(d.Mass pMass)
        {
            Matrix4 obj;
            obj.M11 = pMass.I.M00;
            obj.M12 = pMass.I.M01;
            obj.M13 = pMass.I.M02;
            obj.M14 = 0;
            obj.M21 = pMass.I.M10;
            obj.M22 = pMass.I.M11;
            obj.M23 = pMass.I.M12;
            obj.M24 = 0;
            obj.M31 = pMass.I.M20;
            obj.M32 = pMass.I.M21;
            obj.M33 = pMass.I.M22;
            obj.M34 = 0;
            obj.M41 = 0;
            obj.M42 = 0;
            obj.M43 = 0;
            obj.M44 = 1;
            return obj;
        }

        public d.Mass FromMatrix4(Matrix4 pMat, ref d.Mass obj)
        {
            obj.I.M00 = pMat[0, 0];
            obj.I.M01 = pMat[0, 1];
            obj.I.M02 = pMat[0, 2];
            obj.I.M10 = pMat[1, 0];
            obj.I.M11 = pMat[1, 1];
            obj.I.M12 = pMat[1, 2];
            obj.I.M20 = pMat[2, 0];
            obj.I.M21 = pMat[2, 1];
            obj.I.M22 = pMat[2, 2];
            return obj;
        }

        public override void SubscribeEvents(int ms)
        {
            m_eventsubscription = true;
            _parent_scene.addCollisionEventReporting(this);
        }

        public override void UnSubscribeEvents()
        {
            _parent_scene.remCollisionEventReporting(this);
            m_eventsubscription = false;
        }

        public override void AddCollisionEvent(uint CollidedWith, ContactPoint contact)
        {
            if (base.SubscribedToCollisions() && SubscribedEvents())
            //If we don't have anything that we are going to trigger, don't even add
            {
                if (CollisionEventsThisFrame == null)
                    CollisionEventsThisFrame = new CollisionEventUpdate();
                CollisionEventsThisFrame.addCollider(CollidedWith, contact);
            }
        }

        public override void SendCollisions()
        {
            if (CollisionEventsThisFrame == null || m_frozen) //No collisions or frozen, don't mess with it
                return;
            base.SendCollisionUpdate(CollisionEventsThisFrame.Copy());
            CollisionEventsThisFrame = CollisionEventsThisFrame.m_objCollisionList.Count == 0 ? null : new CollisionEventUpdate();
        }

        public override bool SubscribedEvents()
        {
            return m_eventsubscription;
        }

        private static void DMassCopy(ref d.Mass src, ref d.Mass dst)
        {
            dst.c.W = src.c.W;
            dst.c.X = src.c.X;
            dst.c.Y = src.c.Y;
            dst.c.Z = src.c.Z;
            dst.mass = src.mass;
            dst.I.M00 = src.I.M00;
            dst.I.M01 = src.I.M01;
            dst.I.M02 = src.I.M02;
            dst.I.M10 = src.I.M10;
            dst.I.M11 = src.I.M11;
            dst.I.M12 = src.I.M12;
            dst.I.M20 = src.I.M20;
            dst.I.M21 = src.I.M21;
            dst.I.M22 = src.I.M22;
        }

        private static void DMassDup(ref d.Mass src, out d.Mass dst)
        {
            dst = new d.Mass
            {
            };

            dst.c.W = src.c.W;
            dst.c.X = src.c.X;
            dst.c.Y = src.c.Y;
            dst.c.Z = src.c.Z;
            dst.mass = src.mass;
            dst.I.M00 = src.I.M00;
            dst.I.M01 = src.I.M01;
            dst.I.M02 = src.I.M02;
            dst.I.M10 = src.I.M10;
            dst.I.M11 = src.I.M11;
            dst.I.M12 = src.I.M12;
            dst.I.M20 = src.I.M20;
            dst.I.M21 = src.I.M21;
            dst.I.M22 = src.I.M22;
        }

        private void changeacceleration(Object arg)
        {
            _acceleration = (Vector3)arg;
        }

        private void changeangvelocity(Vector3 arg)
        {
            m_rotationalVelocity = arg;
        }

        private void changeforce(Vector3 force, bool pushforce)
        {
            if (pushforce)
            {
                if (IsPhysical && m_vehicle.Type != Vehicle.TYPE_NONE)
                    m_vehicle.ProcessForceTaint(force);
                else
                    m_pushForce = force;
            }
            else
                m_force = force;
        }

        private void changevoldtc(bool arg)
        {
            m_isVolumeDetect = arg;
        }

        private void donullchange()
        {
        }

        private void changeVehicleType(int value)
        {
            m_vehicle.ProcessTypeChange(this, (Vehicle)value, _parent_scene.ODE_STEPSIZE);
            if (m_vehicle.Type == Vehicle.TYPE_NONE)
                m_vehicle.Enable(Body, this, _parent_scene);
        }

        private void changeVehicleFloatParam(int param, float value)
        {
            m_vehicle.ProcessFloatVehicleParam((Vehicle)param, value, _parent_scene.ODE_STEPSIZE);
        }

        private void changeVehicleVectorParam(int param, Vector3 value)
        {
            m_vehicle.ProcessVectorVehicleParam((Vehicle)param, value, _parent_scene.ODE_STEPSIZE);
        }

        private void changeVehicleRotationParam(int param, Quaternion rotation)
        {
            m_vehicle.ProcessRotationVehicleParam((Vehicle)param, rotation);
        }

        private void changeVehicleFlags(int param, bool remove)
        {
            m_vehicle.ProcessVehicleFlags(param, remove);
        }

        private void changeSetCameraPos(Quaternion CameraRotation)
        {
            m_vehicle.ProcessSetCameraPos(CameraRotation);
        }

        #region Material/Contact setting/getting

        public override void SetMaterial(int pMaterial, bool forceMaterialSettings)
        {
            Material oldMaterial = (Material)m_material;
            m_material = pMaterial;
            Material m = (Material)pMaterial;
            if (!forceMaterialSettings || oldMaterial == m)
                return;
            //Fix restitution and friction values as well
            switch (m)
            {
                case Material.Flesh:
                    _parent_entity.Friction = 0.9f;
                    _parent_entity.Restitution = 0.3f;
                    break;
                case Material.Glass:
                    _parent_entity.Friction = 0.2f;
                    _parent_entity.Restitution = 0.7f;
                    break;
                case Material.Metal:
                    _parent_entity.Friction = 0.3f;
                    _parent_entity.Restitution = 0.4f;
                    break;
                case Material.Plastic:
                    _parent_entity.Friction = 0.4f;
                    _parent_entity.Restitution = 0.7f;
                    break;
                case Material.Rubber:
                    _parent_entity.Friction = 0.9f;
                    _parent_entity.Restitution = 0.9f;
                    break;
                case Material.Stone:
                    _parent_entity.Friction = 0.8f;
                    _parent_entity.Restitution = 0.4f;
                    break;
                case Material.Wood:
                    _parent_entity.Friction = 0.6f;
                    _parent_entity.Restitution = 0.5f;
                    break;
                default:
                    //?????
                    break;
            }
        }

        public void GetContactParam(PhysicsActor actor, ref d.Contact contact)
        {
            int vehicleType = 0;
            if ((_parent != null && (vehicleType = _parent.VehicleType) != (int)Vehicle.TYPE_NONE) ||
                (vehicleType = VehicleType) != (int)Vehicle.TYPE_NONE ||
                (actor is AuroraODEPrim && ((AuroraODEPrim)actor).Parent != null && (vehicleType = ((AuroraODEPrim)actor).Parent.VehicleType) != (int)Vehicle.TYPE_NONE) ||
                (actor is AuroraODEPrim && (vehicleType = ((AuroraODEPrim)actor).VehicleType) != (int)Vehicle.TYPE_NONE))
            {
                if (vehicleType == (int)Vehicle.TYPE_CAR)
                {
                    contact.surface.bounce = 0;
                    contact.surface.bounce_vel = 0;
                    contact.surface.mu = 2;
                }
                else if (vehicleType == (int)Vehicle.TYPE_SLED)
                {
                    contact.surface.bounce = 0;
                    contact.surface.bounce_vel = 0;
                    contact.surface.mu = 0;
                }
                else if (vehicleType == (int)Vehicle.TYPE_AIRPLANE ||
                    vehicleType == (int)Vehicle.TYPE_BALLOON ||
                    vehicleType == (int)Vehicle.TYPE_BOAT)
                {
                    contact.surface.bounce = 0;
                    contact.surface.bounce_vel = 0;
                    contact.surface.mu = 100;
                }
            }
            else
            {
                float restSquared = _parent_entity.Restitution * _parent_entity.Restitution * _parent_entity.Restitution;
                float maxVel = Velocity.Z < -1f ? -1f : Velocity.Z > 1f ? 1f : Velocity.Z;
                contact.surface.bounce = (maxVel * -(restSquared));//Its about 1:1 surprisingly, even though this constant was for havok
                if (contact.surface.bounce > 1.5f)
                    contact.surface.bounce = 0.5f; //Limit the bouncing please...
                if (contact.surface.bounce <= 0)
                {
                    contact.surface.bounce = 0;
                    contact.surface.bounce_vel = 0;
                }
                else
                    contact.surface.bounce_vel = 0.01f * restSquared * (-maxVel * restSquared); //give it a good amount of bounce and have it depend on how much velocity is there too
                contact.surface.mu = 800;
                if (contact.surface.bounce_vel != 0)
                    contact.surface.mode |= d.ContactFlags.Bounce;
                else
                    contact.surface.mode &= d.ContactFlags.Bounce;
                if (actor.PhysicsActorType == (int)ActorTypes.Prim)
                    contact.surface.mu *= _parent_entity.Friction;
                else if (actor.PhysicsActorType == (int)ActorTypes.Ground)
                    contact.surface.mu *= 2;
                else
                    contact.surface.mu /= 2;
                if (m_vehicle.Type != Vehicle.TYPE_NONE && actor.PhysicsActorType != (int)ActorTypes.Agent)
                    contact.surface.mu *= 0.05f;
            }
        }

        #endregion

        #region Mass Calculation

        private float CalculatePrimVolume()
        {
            float volume = _size.X * _size.Y * _size.Z; // default
            float tmp;

            float hollowAmount = _pbs.ProfileHollow * 2.0e-5f;
            float hollowVolume = hollowAmount * hollowAmount;

            switch (_pbs.ProfileShape)
            {
                case ProfileShape.Square:
                    // default box

                    if (_pbs.PathCurve == (byte)Extrusion.Straight)
                    {
                        if (hollowAmount > 0.0)
                        {
                            switch (_pbs.HollowShape)
                            {
                                case HollowShape.Square:
                                case HollowShape.Same:
                                    break;

                                case HollowShape.Circle:

                                    hollowVolume *= 0.78539816339f;
                                    break;

                                case HollowShape.Triangle:

                                    hollowVolume *= (0.5f * .5f);
                                    break;

                                default:
                                    hollowVolume = 0;
                                    break;
                            }
                            volume *= (1.0f - hollowVolume);
                        }
                    }

                    else if (_pbs.PathCurve == (byte)Extrusion.Curve1)
                    {
                        //a tube 

                        volume *= 0.78539816339e-2f * (200 - _pbs.PathScaleX);
                        tmp = 1.0f - 2.0e-2f * (200 - _pbs.PathScaleY);
                        volume -= volume * tmp * tmp;

                        if (hollowAmount > 0.0)
                        {
                            hollowVolume *= hollowAmount;

                            switch (_pbs.HollowShape)
                            {
                                case HollowShape.Square:
                                case HollowShape.Same:
                                    break;

                                case HollowShape.Circle:
                                    hollowVolume *= 0.78539816339f;
                                    ;
                                    break;

                                case HollowShape.Triangle:
                                    hollowVolume *= 0.5f * 0.5f;
                                    break;
                                default:
                                    hollowVolume = 0;
                                    break;
                            }
                            volume *= (1.0f - hollowVolume);
                        }
                    }

                    break;

                case ProfileShape.Circle:

                    if (_pbs.PathCurve == (byte)Extrusion.Straight)
                    {
                        volume *= 0.78539816339f; // elipse base

                        if (hollowAmount > 0.0)
                        {
                            switch (_pbs.HollowShape)
                            {
                                case HollowShape.Same:
                                case HollowShape.Circle:
                                    break;

                                case HollowShape.Square:
                                    hollowVolume *= 0.5f * 2.5984480504799f;
                                    break;

                                case HollowShape.Triangle:
                                    hollowVolume *= .5f * 1.27323954473516f;
                                    break;

                                default:
                                    hollowVolume = 0;
                                    break;
                            }
                            volume *= (1.0f - hollowVolume);
                        }
                    }

                    else if (_pbs.PathCurve == (byte)Extrusion.Curve1)
                    {
                        volume *= 0.61685027506808491367715568749226e-2f * (200 - _pbs.PathScaleX);
                        tmp = 1.0f - .02f * (200 - _pbs.PathScaleY);
                        volume *= (1.0f - tmp * tmp);

                        if (hollowAmount > 0.0)
                        {
                            // calculate the hollow volume by it's shape compared to the prim shape
                            hollowVolume *= hollowAmount;

                            switch (_pbs.HollowShape)
                            {
                                case HollowShape.Same:
                                case HollowShape.Circle:
                                    break;

                                case HollowShape.Square:
                                    hollowVolume *= 0.5f * 2.5984480504799f;
                                    break;

                                case HollowShape.Triangle:
                                    hollowVolume *= .5f * 1.27323954473516f;
                                    break;

                                default:
                                    hollowVolume = 0;
                                    break;
                            }
                            volume *= (1.0f - hollowVolume);
                        }
                    }
                    break;

                case ProfileShape.HalfCircle:
                    if (_pbs.PathCurve == (byte)Extrusion.Curve1)
                    {
                        volume *= 0.52359877559829887307710723054658f;
                    }
                    break;

                case ProfileShape.EquilateralTriangle:

                    if (_pbs.PathCurve == (byte)Extrusion.Straight)
                    {
                        volume *= 0.32475953f;

                        if (hollowAmount > 0.0)
                        {
                            // calculate the hollow volume by it's shape compared to the prim shape
                            switch (_pbs.HollowShape)
                            {
                                case HollowShape.Same:
                                case HollowShape.Triangle:
                                    hollowVolume *= .25f;
                                    break;

                                case HollowShape.Square:
                                    hollowVolume *= 0.499849f * 3.07920140172638f;
                                    break;

                                case HollowShape.Circle:
                                    // Hollow shape is a perfect cyllinder in respect to the cube's scale
                                    // Cyllinder hollow volume calculation

                                    hollowVolume *= 0.1963495f * 3.07920140172638f;
                                    break;

                                default:
                                    hollowVolume = 0;
                                    break;
                            }
                            volume *= (1.0f - hollowVolume);
                        }
                    }
                    else if (_pbs.PathCurve == (byte)Extrusion.Curve1)
                    {
                        volume *= 0.32475953f;
                        volume *= 0.01f * (200 - _pbs.PathScaleX);
                        tmp = 1.0f - .02f * (200 - _pbs.PathScaleY);
                        volume *= (1.0f - tmp * tmp);

                        if (hollowAmount > 0.0)
                        {
                            hollowVolume *= hollowAmount;

                            switch (_pbs.HollowShape)
                            {
                                case HollowShape.Same:
                                case HollowShape.Triangle:
                                    hollowVolume *= .25f;
                                    break;

                                case HollowShape.Square:
                                    hollowVolume *= 0.499849f * 3.07920140172638f;
                                    break;

                                case HollowShape.Circle:

                                    hollowVolume *= 0.1963495f * 3.07920140172638f;
                                    break;

                                default:
                                    hollowVolume = 0;
                                    break;
                            }
                            volume *= (1.0f - hollowVolume);
                        }
                    }
                    break;

                default:
                    break;
            }

            float taperX1;
            float taperY1;
            float taperX;
            float taperY;
            float pathBegin;
            float pathEnd;
            float profileBegin;
            float profileEnd;

            if (_pbs.PathCurve == (byte)Extrusion.Straight || _pbs.PathCurve == (byte)Extrusion.Flexible)
            {
                taperX1 = _pbs.PathScaleX * 0.01f;
                if (taperX1 > 1.0f)
                    taperX1 = 2.0f - taperX1;
                taperX = 1.0f - taperX1;

                taperY1 = _pbs.PathScaleY * 0.01f;
                if (taperY1 > 1.0f)
                    taperY1 = 2.0f - taperY1;
                taperY = 1.0f - taperY1;
            }
            else
            {
                taperX = _pbs.PathTaperX * 0.01f;
                if (taperX < 0.0f)
                    taperX = -taperX;
                taperX1 = 1.0f - taperX;

                taperY = _pbs.PathTaperY * 0.01f;
                if (taperY < 0.0f)
                    taperY = -taperY;
                taperY1 = 1.0f - taperY;
            }

            volume *= (taperX1 * taperY1 + 0.5f * (taperX1 * taperY + taperX * taperY1) + 0.3333333333f * taperX * taperY);

            pathBegin = _pbs.PathBegin * 2.0e-5f;
            pathEnd = 1.0f - _pbs.PathEnd * 2.0e-5f;
            volume *= (pathEnd - pathBegin);

            // this is crude aproximation
            profileBegin = _pbs.ProfileBegin * 2.0e-5f;
            profileEnd = 1.0f - _pbs.ProfileEnd * 2.0e-5f;
            volume *= (profileEnd - profileBegin);
            return volume;
        }

        // end CalculateMass


        public void CalcPrimBodyData()
        {
            if (prim_geom == IntPtr.Zero)
            {
                // Ubit let's have a initial basic OOB
                primOOBsize.X = _size.X;
                primOOBsize.Y = _size.Y;
                primOOBsize.Z = _size.Z;
                primOOBoffset = Vector3.Zero;
            }
            else
            {
                d.AABB AABB;
                d.GeomGetAABB(prim_geom, out AABB); // get the AABB from engine geom

                primOOBsize.X = (AABB.MaxX - AABB.MinX);
                primOOBsize.Y = (AABB.MaxY - AABB.MinY);
                primOOBsize.Z = (AABB.MaxZ - AABB.MinZ);
                if (!hasOOBoffsetFromMesh)
                {
                    primOOBoffset.X = (AABB.MaxX + AABB.MinX) * 0.5f;
                    primOOBoffset.Y = (AABB.MaxY + AABB.MinY) * 0.5f;
                    primOOBoffset.Z = (AABB.MaxZ + AABB.MinZ) * 0.5f;
                }
            }

            // also its own inertia and mass
            // keep using basic shape mass for now
            float volume = CalculatePrimVolume();

            primMass = _parent_entity.Density * volume * 0.01f; //Divide by 100 as its a bit high for ODE....

            if (primMass <= 0)
                primMass = 0.0001f; //ckrinke: Mass must be greater then zero.
            //            else if (returnMass > _parent_scene.maximumMassObject)
            //                returnMass = _parent_scene.maximumMassObject;

            if (primMass > _parent_scene.maximumMassObject)
                primMass = _parent_scene.maximumMassObject;

            _mass = primMass;

            d.MassSetBoxTotal(out primdMass, primMass, primOOBsize.X, primOOBsize.Y, primOOBsize.Z);

            d.MassTranslate(ref primdMass,
                            primOOBoffset.X,
                            primOOBoffset.Y,
                            primOOBoffset.Z);

            primOOBsize *= 0.5f; // let obb size be a corner coords
        }

        #endregion

        #region Nested type: strVehicleBoolParam

        private struct strVehicleBoolParam
        {
            public int param;
            public bool value;
        }

        #endregion

        #region Nested type: strVehicleFloatParam

        private struct strVehicleFloatParam
        {
            public int param;
            public float value;
        }

        #endregion

        #region Nested type: strVehicleQuartParam

        private struct strVehicleQuartParam
        {
            public int param;
            public Quaternion value;
        }

        #endregion

        #region Nested type: strVehicleVectorParam

        private struct strVehicleVectorParam
        {
            public int param;
            public Vector3 value;
        }

        #endregion
    }
}