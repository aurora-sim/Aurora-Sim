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
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Physics.ConvexDecompositionDotNet;
using Aurora.Framework.Physics;
using OMV = OpenMetaverse;
using Quaternion = OpenMetaverse.Quaternion;

namespace OpenSim.Region.Physics.BulletSPlugin
{
    [Serializable]
    public sealed class BSPrim : PhysicsObject
    {
        private const float ROTATION_TOLERANCE = 0.01f;
        private const float VELOCITY_TOLERANCE = 0.01f;
        private const float POSITION_TOLERANCE = 0.05f;
        private const float ACCELERATION_TOLERANCE = 0.01f;
        private const float ROTATIONAL_VELOCITY_TOLERANCE = 0.01f;
        private const bool SHOULD_DAMP_UPDATES = true;
        private static readonly string LogHeader = "[BULLETS PRIM]";
        private static readonly Dictionary<ulong, float[]> _hullMeshDictionary = new Dictionary<ulong, float[]>();

        private readonly String _avName;
        private readonly List<BSPrim> _childrenPrims;
        private readonly ISceneChildEntity _parent_entity;
        private readonly BSScene _scene;
        private readonly BSDynamics _vehicle;
        private Vector3 _acceleration;
        private Vector3 _angularVelocity;
        private float _buoyancy;
        private bool _collidingGround;
        private long _collidingGroundStep;
        private bool _collidingObj;
        private long _collidingStep;
        private float _collisionScore;
        private bool _floatOnWater;
        private bool _flying;
        private Vector3 _force;
        private ulong _hullKey;
        private List<ConvexResult> _hulls;
        private bool _isColliding;
        private bool _isPhysical;
        private bool _isSelected;
        private bool _isVolumeDetect;
        private int _lastCollisionTime;
        private Vector3 _lastRotationalVelocity;
        private Vector3 _lastVelocity;
        private int _limitedUpdatesCount;
        private uint _localID;
        private float _mass;
        private int _material;
        private IMesh _mesh;
        private Quaternion _orientation;

        private BSPrim _parentPrim;
        private PrimitiveBaseShape _pbs;
        private Vector3 _position;
        private Vector3 _rotationalVelocity;
        private Vector3 _scale;
        private bool _setAlwaysRun;
        private ShapeData.PhysicsShapeType _shapeType;
        private Vector3 _size;

        private int _subscribedEventsMs;
        private bool _throttleUpdates;
        private Vector3 _torque;
        private Vector3 _velocity;

        public BSPrim(ISceneChildEntity entity, bool isPhysical, BSScene parent_scene)
        {
            _parent_entity = entity;
            // MainConsole.Instance.DebugFormat("{0}: BSPrim creation of {1}, id={2}", LogHeader, primName, localID);
            _localID = _parent_entity.LocalId;
            _avName = _parent_entity.Name;
            _scene = parent_scene;
            _position = _parent_entity.AbsolutePosition;
            _size = _parent_entity.Scale;
            _scale = new Vector3(1f, 1f, 1f); // the scale will be set by CreateGeom depending on object type
            _orientation = _parent_entity.Rotation;
            _buoyancy = 1f;
            _velocity = Vector3.Zero;
            _rotationalVelocity = Vector3.Zero;
            _angularVelocity = Vector3.Zero;
            _hullKey = 0;
            _pbs = _parent_entity.Shape;
            _isPhysical = isPhysical;
            _isVolumeDetect = _parent_entity.VolumeDetectActive;
            _subscribedEventsMs = 0;
            _parentPrim = null;
            _vehicle = new BSDynamics(this);
            _childrenPrims = new List<BSPrim>();
            _mass = _isPhysical ? CalculateMass() : 0f;
            // do the actual object creation at taint time
            _scene.TaintedObject(RecreateGeomAndObject);
        }

        // called when this prim is being destroyed and we should free all the resources

        public override Vector3 Size
        {
            get { return _size; }
            set
            {
                _size = value;
                _scene.TaintedObject(delegate
                                         {
                                             if (_isPhysical) _mass = CalculateMass(); // changing size changes the mass
                                             BulletSimAPI.SetObjectScaleMass(_scene.WorldID, _localID, _scale, _mass,
                                                                             _isPhysical);
                                             RecreateGeomAndObject();
                                         });
            }
        }

        public override PrimitiveBaseShape Shape
        {
            set
            {
                _pbs = value;
                _scene.TaintedObject(delegate
                                         {
                                             if (_isPhysical)
                                                 _mass = CalculateMass(); // changing the shape changes the mass
                                             RecreateGeomAndObject();
                                         });
            }
        }

        public override uint LocalID
        {
            set { _localID = value; }
            get { return _localID; }
        }

        public override bool Selected
        {
            set
            {
                _isSelected = value;
                _scene.TaintedObject(SetObjectDynamic);
            }
        }

        public BSPrim ParentPrim
        {
            set { _parentPrim = value; }
        }

        public ulong HullKey
        {
            get { return _hullKey; }
        }

        // return true if we are the root of a linkset (there are children to manage)
        public bool IsRootOfLinkset
        {
            get { return (_parentPrim == null && _childrenPrims.Count != 0); }
        }

        // Set motion values to zero.
        // Do it to the properties so the values get set in the physics engine.
        // Push the setting of the values to the viewer.

        public override Vector3 Position
        {
            get
            {
                // don't do the following GetObjectPosition because this function is called a zillion times
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
                    // MainConsole.Instance.DebugFormat("{0}: setPosition: id={1}, position={2}", LogHeader, _localID, _position);
                });
#else
                _scene.TaintedObject(() => BulletSimAPI.SetObjectTranslation(_scene.WorldID, _localID, _position,
                                                                             _orientation));
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
                    BulletSimAPI.SetObjectVelocity(_scene.WorldID, LocalID, _velocity);
                });
#else
                _scene.TaintedObject(() => BulletSimAPI.SetObjectVelocity(_scene.WorldID, LocalID, _velocity));
#endif
            }
        }

        public override Vector3 Torque
        {
            get { return _torque; }
            set { _torque = value; }
        }

        public override float CollisionScore
        {
            get { return _collisionScore; }
            set { _collisionScore = value; }
        }

        public override Vector3 Acceleration
        {
            get { return _acceleration; }
        }

        public override Quaternion Orientation
        {
            get { return _orientation; }
            set
            {
                _orientation = value;
                // MainConsole.Instance.DebugFormat("{0}: set orientation: id={1}, ori={2}", LogHeader, LocalID, _orientation);
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
            get { return (int) ActorTypes.Prim; }
        }

        public override bool IsPhysical
        {
            get { return _isPhysical; }
            set
            {
                _isPhysical = value;
                _scene.TaintedObject(SetObjectDynamic);
            }
        }

        // An object is static (does not move) if selected or not physical
        private bool IsStatic
        {
            get { return _isSelected || !IsPhysical; }
        }

        // An object is solid if it's not phantom and if it's not doing VolumeDetect
        private bool IsSolid
        {
            get { return !IsPhantom && !_isVolumeDetect; }
        }

        // make gravity work if the object is physical and not selected
        // no locking here because only called when it is safe

        // prims don't fly
        public override bool Flying
        {
            get { return _flying; }
            set { _flying = value; }
        }

        public override bool SetAlwaysRun
        {
            get { return _setAlwaysRun; }
            set { _setAlwaysRun = value; }
        }

        public override bool ThrottleUpdates
        {
            get { return _throttleUpdates; }
            set { _throttleUpdates = value; }
        }

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

        public bool CollidingObj
        {
            get { return _collidingObj; }
            set { _collidingObj = value; }
        }

        public bool IsPhantom
        {
            get
            {
                // SceneObjectPart removes phantom objects from the physics scene
                // so, although we could implement touching and such, we never
                // are invoked as a phantom object
                return false;
            }
        }

        public override bool FloatOnWater
        {
            set { _floatOnWater = value; }
        }

        public override Vector3 RotationalVelocity
        {
            get { return _rotationalVelocity; }
            set
            {
                _rotationalVelocity = value;
                // MainConsole.Instance.DebugFormat("{0}: RotationalVelocity={1}", LogHeader, _rotationalVelocity);
#if (!ISWIN)
                _scene.TaintedObject(delegate()
                {
                    BulletSimAPI.SetObjectAngularVelocity(_scene.WorldID, LocalID, _rotationalVelocity);
                });
#else
                _scene.TaintedObject(
                    () => BulletSimAPI.SetObjectAngularVelocity(_scene.WorldID, LocalID, _rotationalVelocity));
#endif
            }
        }

        public Vector3 AngularVelocity
        {
            get { return _angularVelocity; }
            set { _angularVelocity = value; }
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
                    BulletSimAPI.SetObjectBuoyancy(_scene.WorldID, _localID, 1 - _buoyancy);//Bullet has no change as 0, instead of 1
                });
#else
                _scene.TaintedObject(
                    () => BulletSimAPI.SetObjectBuoyancy(_scene.WorldID, _localID, 1 - _buoyancy));
#endif
            }
        }

        public override bool VolumeDetect
        {
            get { return _isVolumeDetect; }
            set
            {
                if (_isVolumeDetect != value)
                {
                    _isVolumeDetect = value;
                    _scene.TaintedObject(SetObjectDynamic);
                }
            }
        }

        #region Vehicles

        public override int VehicleType
        {
            get { return (int) _vehicle.Type; // if we are a vehicle, return that type
            }
            set
            {
                Vehicle type = (Vehicle) value;
                _vehicle.ProcessTypeChange(type);
                _scene.TaintedObject(delegate
                                         {
                                             if (type == Vehicle.TYPE_NONE)
                                             {
                                                 _scene.RemoveVehiclePrim(this);
                                             }
                                             else
                                             {
                                                 // make it so the scene will call us each tick to do vehicle things
                                                 _scene.AddVehiclePrim(this);
                                             }
                                             return;
                                         });
            }
        }

        public override void VehicleFloatParam(int param, float value)
        {
            _vehicle.ProcessFloatVehicleParam((Vehicle) param, value);
        }

        public override void VehicleVectorParam(int param, Vector3 value)
        {
            _vehicle.ProcessVectorVehicleParam((Vehicle) param, value);
        }

        public override void VehicleRotationParam(int param, Quaternion rotation)
        {
            _vehicle.ProcessRotationVehicleParam((Vehicle) param, rotation);
        }

        public override void VehicleFlags(int param, bool remove)
        {
            _vehicle.ProcessVehicleFlags(param, remove);
        }

        // Called each simulation step to advance vehicle characteristics
        public void StepVehicle(float timeStep)
        {
            _vehicle.Step(timeStep, _scene);
        }

        #endregion

        public void Destroy()
        {
            // MainConsole.Instance.DebugFormat("{0}: Destroy", LogHeader);
            // Undo any vehicle properties
            _vehicle.ProcessTypeChange(Vehicle.TYPE_NONE);
            _scene.RemoveVehiclePrim(this); // just to make sure
#if (!ISWIN)
            _scene.TaintedObject(delegate()
            {
                BulletSimAPI.DestroyObject(_scene.WorldID, _localID);
            });
#else
            _scene.TaintedObject(() => BulletSimAPI.DestroyObject(_scene.WorldID, _localID));
#endif
        }

        public override void CrossingFailure()
        {
            return;
        }

        // link me to the specified parent
        public override void link(PhysicsObject obj)
        {
            BSPrim parent = (BSPrim) obj;
            // MainConsole.Instance.DebugFormat("{0}: link {1}/{2} to {3}", LogHeader, _avName, _localID, obj.LocalID);
            // TODO: decide if this parent checking needs to happen at taint time
            if (_parentPrim == null)
            {
                if (parent != null)
                {
                    // I don't have a parent so I am joining a linkset
                    parent.AddChildToLinkset(this);
                }
            }
            else
            {
                // I already have a parent, is parenting changing?
                if (parent != _parentPrim)
                {
                    if (parent == null)
                    {
                        // we are being removed from a linkset
                        _parentPrim.RemoveChildFromLinkset(this);
                    }
                    else
                    {
                        // asking to reparent a prim should not happen
                        MainConsole.Instance.ErrorFormat("{0}: Reparenting a prim. ", LogHeader);
                    }
                }
            }
            return;
        }

        // delink me from my linkset
        public override void delink()
        {
            // TODO: decide if this parent checking needs to happen at taint time
            // Race condition here: if link() and delink() in same simulation tick, the delink will not happen
            // MainConsole.Instance.DebugFormat("{0}: delink {1}/{2}", LogHeader, _avName, _localID);
            if (_parentPrim != null)
            {
                _parentPrim.RemoveChildFromLinkset(this);
            }
            return;
        }

        // I am the root of a linkset and a new child is being added
        public void AddChildToLinkset(BSPrim pchild)
        {
            BSPrim child = pchild;
            _scene.TaintedObject(delegate
                                     {
                                         if (!_childrenPrims.Contains(child))
                                         {
                                             _childrenPrims.Add(child);
                                             child.ParentPrim = this; // the child has gained a parent
                                             RecreateGeomAndObject(); // rebuild my shape with the new child added
                                         }
                                     });
            return;
        }

        // I am the root of a linkset and one of my children is being removed.
        // Safe to call even if the child is not really in my linkset.
        public void RemoveChildFromLinkset(BSPrim pchild)
        {
            BSPrim child = pchild;
            _scene.TaintedObject(delegate
                                     {
                                         if (_childrenPrims.Contains(child))
                                         {
                                             BulletSimAPI.RemoveConstraint(_scene.WorldID, child.LocalID, this.LocalID);
                                             _childrenPrims.Remove(child);
                                             child.ParentPrim = null; // the child has lost its parent
                                             RecreateGeomAndObject(); // rebuild my shape with the child removed
                                         }
                                         else
                                         {
                                             MainConsole.Instance.ErrorFormat(
                                                 "{0}: Asked to remove child from linkset that was not in linkset");
                                         }
                                     });
            return;
        }

        private void ZeroMotion()
        {
            Velocity = Vector3.Zero;
            _acceleration = Vector3.Zero;
            RotationalVelocity = Vector3.Zero;
            base.RequestPhysicsterseUpdate();
        }

        public override void LockAngularMotion(Vector3 axis)
        {
            return;
        }

        private void SetObjectDynamic()
        {
            // non-physical things work best with a mass of zero
            _mass = IsStatic ? 0f : CalculateMass();
            BulletSimAPI.SetObjectProperties(_scene.WorldID, LocalID, IsStatic, IsSolid, SubscribedEvents(), _mass);
            //Gotta reset this too if we're changing the staticness of the prim
            Buoyancy = _buoyancy;
            // MainConsole.Instance.DebugFormat("{0}: ID={1}, SetObjectDynamic: IsStatic={2}, IsSolid={3}, mass={4}", LogHeader, _localID, IsStatic, IsSolid, _mass);
        }

        public override void AddForce(Vector3 force, bool pushforce)
        {
            if (force.IsFinite())
            {
                if (pushforce)
                {
                    _force.X = force.X;
                    _force.Y = force.Y;
                    _force.Z = force.Z;
                }
                else
                {
                    _force.X += force.X;
                    _force.Y += force.Y;
                    _force.Z += force.Z;
                }
            }
            else
            {
                MainConsole.Instance.WarnFormat("{0}: Got a NaN force applied to a Prim", LogHeader);
            }
#if (!ISWIN)
            _scene.TaintedObject(delegate()
            {
                BulletSimAPI.SetObjectForce(_scene.WorldID, _localID, _force);
            });
#else
            _scene.TaintedObject(() => BulletSimAPI.SetObjectForce(_scene.WorldID, _localID, _force));
#endif
        }

        public override void AddAngularForce(Vector3 force, bool pushforce)
        {
            // MainConsole.Instance.DebugFormat("{0}: AddAngularForce. f={1}, push={2}", LogHeader, force, pushforce);
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

        // Create the geometry information in Bullet for later use
        // No locking here because this is done when we know physics is not simulating
        private void CreateGeom()
        {
            // Since we're recreating new, get rid of any previously generated shape
            if (_hullKey != 0)
            {
                // MainConsole.Instance.DebugFormat("{0}: CreateGeom: deleting old hull. Key={1}", LogHeader, _hullKey);
                BulletSimAPI.DestroyHull(_scene.WorldID, _hullKey);
                _hullKey = 0;
                _hulls.Clear();
            }

            if (_mesh == null)
            {
                // the mesher thought this was too simple to mesh. Use a native Bullet collision shape.
                if (_pbs.ProfileShape == ProfileShape.HalfCircle && _pbs.PathCurve == (byte) Extrusion.Curve1)
                {
                    if (_size.X == _size.Y && _size.Y == _size.Z && _size.X == _size.Z)
                    {
                        // MainConsole.Instance.DebugFormat("{0}: CreateGeom: mesh null. Defaulting to sphere of size {1}", LogHeader, _size);
                        _shapeType = ShapeData.PhysicsShapeType.SHAPE_SPHERE;
                        // Bullet native objects are scaled by the Bullet engine so pass the size in
                        _scale = _size;
                    }
                }
                else
                {
                    // MainConsole.Instance.DebugFormat("{0}: CreateGeom: mesh null. Defaulting to box. lid={1}, size={2}", LogHeader, LocalID,  _size);
                    _shapeType = ShapeData.PhysicsShapeType.SHAPE_BOX;
                    _scale = _size;
                }
            }
            else
            {
                GenerateHullMesh();
            }
            return;
        }

        private void GenerateHullMesh()
        {
            float[] convHulls;
            _hullKey = (ulong) _pbs.GetHashCode();
            if (_hullMeshDictionary.TryGetValue(_hullKey, out convHulls))
            {
                // create the hull definition in Bullet
                // MainConsole.Instance.DebugFormat("{0}: CreateGeom: calling CreateHull. lid={1}, key={2}, hulls={3}", LogHeader, _localID, _hullKey, hullCount);
                BulletSimAPI.CreateHull(_scene.WorldID, _hullKey, (int) convHulls[0], convHulls);
                _shapeType = ShapeData.PhysicsShapeType.SHAPE_HULL;
                // Let the object be scaled by Bullet (the mesh was created as a unit mesh)
                _scale = _size;
                return;
            }
            int[] indices = _mesh.getIndexListAsInt();
            List<Vector3> vertices = _mesh.getVertexList();

            //format conversion from IMesh format to DecompDesc format
            List<int> convIndices = new List<int>();
            for (int ii = 0; ii < indices.GetLength(0); ii++)
            {
                convIndices.Add(indices[ii]);
            }
#if (!ISWIN)
            List<float3> convVertices = new List<float3>();
            foreach (Vector3 vv in vertices)
                convVertices.Add(new float3(vv.X, vv.Y, vv.Z));
#else
            List<float3> convVertices = vertices.Select(vv => new float3(vv.X, vv.Y, vv.Z)).ToList();
#endif

            // setup and do convex hull conversion
            _hulls = new List<ConvexResult>();
            DecompDesc dcomp = new DecompDesc {mIndices = convIndices, mVertices = convVertices};
            ConvexBuilder convexBuilder = new ConvexBuilder(HullReturn);
            // create the hull into the _hulls variable
            convexBuilder.process(dcomp);

            // Convert the vertices and indices for passing to unmanaged
            // The hull information is passed as a large floating point array. 
            // The format is:
            //  convHulls[0] = number of hulls
            //  convHulls[1] = number of vertices in first hull
            //  convHulls[2] = hull centroid X coordinate
            //  convHulls[3] = hull centroid Y coordinate
            //  convHulls[4] = hull centroid Z coordinate
            //  convHulls[5] = first hull vertex X
            //  convHulls[6] = first hull vertex Y
            //  convHulls[7] = first hull vertex Z
            //  convHulls[8] = second hull vertex X
            //  ...
            //  convHulls[n] = number of vertices in second hull
            //  convHulls[n+1] = second hull centroid X coordinate
            //  ...
            //
            // TODO: is is very inefficient. Someday change the convex hull generator to return
            //   data structures that do not need to be converted in order to pass to Bullet.
            //   And maybe put the values directly into pinned memory rather than marshaling.
            int hullCount = _hulls.Count;
            int totalVertices = 1; // include one for the count of the hulls
            foreach (ConvexResult cr in _hulls)
            {
                totalVertices += 4; // add four for the vertex count and centroid
                totalVertices += cr.HullIndices.Count*3; // we pass just triangles
            }
            convHulls = new float[totalVertices];

            convHulls[0] = hullCount;
            int jj = 1;
            foreach (ConvexResult cr in _hulls)
            {
                // copy vertices for index access
                float3[] verts = new float3[cr.HullVertices.Count];
                int kk = 0;
                foreach (float3 ff in cr.HullVertices)
                {
                    verts[kk++] = ff;
                }

                // add to the array one hull's worth of data
                convHulls[jj++] = cr.HullIndices.Count;
                convHulls[jj++] = 0f; // centroid x,y,z
                convHulls[jj++] = 0f;
                convHulls[jj++] = 0f;
                foreach (int ind in cr.HullIndices)
                {
                    convHulls[jj++] = verts[ind].x;
                    convHulls[jj++] = verts[ind].y;
                    convHulls[jj++] = verts[ind].z;
                }
            }

            // create the hull definition in Bullet
            // MainConsole.Instance.DebugFormat("{0}: CreateGeom: calling CreateHull. lid={1}, key={2}, hulls={3}", LogHeader, _localID, _hullKey, hullCount);
            BulletSimAPI.CreateHull(_scene.WorldID, _hullKey, hullCount, convHulls);
            _shapeType = ShapeData.PhysicsShapeType.SHAPE_HULL;
            // Let the object be scaled by Bullet (the mesh was created as a unit mesh)
            _scale = _size;
        }

        // Callback from convex hull creater with a newly created hull.
        // Just add it to the collection of hulls for this shape.
        private void HullReturn(ConvexResult result)
        {
            _hulls.Add(result);
            return;
        }

        // Create an object in Bullet
        // No locking here because this is done when the physics engine is not simulating
        private void CreateObject()
        {
            if (IsRootOfLinkset)
            {
                // Create a linkset around this object
                // CreateLinksetWithCompoundHull();
                CreateLinksetWithConstraints();
            }
            else
            {
                // simple object
                ShapeData shape;
                FillShapeInfo(out shape);
                BulletSimAPI.CreateObject(_scene.WorldID, shape);
            }
        }

        // Create a linkset by creating a compound hull at the root prim that consists of all
        // the children.
        // NOTE: This does not allow proper collisions with the children prims so it is not a workable solution
        private void CreateLinksetWithCompoundHull()
        {
            // If I am the root prim of a linkset, replace my physical shape with all the
            // pieces of the children.
            // All of the children should have called CreateGeom so they have a hull
            // in the physics engine already. Here we pull together all of those hulls
            // into one shape.
            int totalPrimsInLinkset = _childrenPrims.Count + 1;
            // MainConsole.Instance.DebugFormat("{0}: CreateLinkset. Root prim={1}, prims={2}", LogHeader, LocalID, totalPrimsInLinkset);
            ShapeData[] shapes = new ShapeData[totalPrimsInLinkset];
            FillShapeInfo(out shapes[0]);
            int ii = 1;
            foreach (BSPrim prim in _childrenPrims)
            {
                // MainConsole.Instance.DebugFormat("{0}: CreateLinkset: adding prim {1}", LogHeader, prim.LocalID);
                prim.FillShapeInfo(out shapes[ii]);
                ii++;
            }
            BulletSimAPI.CreateLinkset(_scene.WorldID, totalPrimsInLinkset, shapes);
        }

        // Copy prim's info into the BulletSim shape description structure
        public void FillShapeInfo(out ShapeData shape)
        {
            shape.ID = _localID;
            shape.Type = _shapeType;
            shape.Position = _position;
            shape.Rotation = _orientation;
            shape.Velocity = _velocity;
            shape.Scale = _scale;
            shape.Mass = _isPhysical ? _mass : 0f;
            shape.Buoyancy = _buoyancy;
            shape.MeshKey = _hullKey;
            shape.Friction = _parent_entity.Friction;
            shape.Restitution = _parent_entity.Restitution;
            shape.Collidable = (!IsPhantom) ? ShapeData.numericTrue : ShapeData.numericFalse;
            shape.Static = _isPhysical ? ShapeData.numericFalse : ShapeData.numericTrue;
        }

        // Create the linkset by putting constraints between the objects of the set so they cannot move
        // relative to each other.
        // TODO: make this more effeicient: a large linkset gets rebuilt over and over and prims are added
        private void CreateLinksetWithConstraints()
        {
            // MainConsole.Instance.DebugFormat("{0}: CreateLinkset. Root prim={1}, prims={2}", LogHeader, LocalID, _childrenPrims.Count+1);

            // remove any constraints that might be in place
            foreach (BSPrim prim in _childrenPrims)
            {
                // MainConsole.Instance.DebugFormat("{0}: CreateObject: RemoveConstraint between root prim {1} and child prim {2}", LogHeader, LocalID, prim.LocalID);
                BulletSimAPI.RemoveConstraint(_scene.WorldID, LocalID, prim.LocalID);
            }
            // create constraints between the root prim and each of the children
            foreach (BSPrim prim in _childrenPrims)
            {
                // MainConsole.Instance.DebugFormat("{0}: CreateObject: AddConstraint between root prim {1} and child prim {2}", LogHeader, LocalID, prim.LocalID);

                // Zero motion for children so they don't interpolate
                prim.ZeroMotion();

                // relative position normalized to the root prim
                Vector3 childRelativePosition = (prim._position - this._position)*Quaternion.Inverse(this._orientation);

                // relative rotation of the child to the parent
                Quaternion relativeRotation = Quaternion.Inverse(prim._orientation)*this._orientation;

                // this is a constraint that allows no freedom of movement between the two objects
                // http://bulletphysics.org/Bullet/phpBB3/viewtopic.php?t=4818
                BulletSimAPI.AddConstraint(_scene.WorldID, LocalID, prim.LocalID,
                                           childRelativePosition,
                                           relativeRotation,
                                           Vector3.Zero,
                                           Quaternion.Identity,
                                           Vector3.Zero, Vector3.Zero,
                                           Vector3.Zero, Vector3.Zero);
            }
        }

        // Rebuild the geometry and object.
        // This is called when the shape changes so we need to recreate the mesh/hull.
        // No locking here because this is done when the physics engine is not simulating
        private void RecreateGeomAndObject()
        {
            // If this object is complex or we are the root of a linkset, build a mesh.
            // The root of a linkset must be a mesh so we can create the linked compound object.
            // if (_scene.NeedsMeshing(_pbs) || IsRootOfLinkset )
            if (_scene.NeedsMeshing(_pbs)) // linksets with constraints don't need a root mesh
            {
                // MainConsole.Instance.DebugFormat("{0}: RecreateGeomAndObject: creating mesh", LogHeader);
                // Make the mesh scaled to 1 and use Bullet's scaling feature to scale it in world
                Vector3 scaleFactor = new Vector3(1.0f, 1.0f, 1.0f);
                _mesh = _scene.mesher.CreateMesh(_avName, _pbs, scaleFactor, _scene.MeshLOD, _isPhysical);
            }
            else
            {
                // implement the shape with a Bullet native shape.
                _mesh = null;
            }
            CreateGeom();
            CreateObject();
            return;
        }

        // The physics engine says that properties have updated. Update same and inform
        // the world that things have changed.
        // TODO: do we really need to check for changed? Maybe just copy values and call RequestPhysicsterseUpdate()

        public void UpdateProperties(EntityProperties entprop)
        {
            UpdatedProperties changed = 0;
            if (SHOULD_DAMP_UPDATES && !IsStatic) //Only physical prims get updated tyvm
            {
                // assign to the local variables so the normal set action does not happen
                // if (_position != entprop.Position)
                if (!_position.ApproxEquals(entprop.Position, POSITION_TOLERANCE))
                {
                    _position = entprop.Position;
                    // MainConsole.Instance.DebugFormat("{0}: UpdateProperties: id={1}, pos = {2}", LogHeader, LocalID, _position);
                    //changed |= UpdatedProperties.Position;
                }
                // if (_orientation != entprop.Rotation)
                if (!_orientation.ApproxEquals(entprop.Rotation, ROTATION_TOLERANCE))
                {
                    _orientation = entprop.Rotation;
                    // MainConsole.Instance.DebugFormat("{0}: UpdateProperties: id={1}, rot = {2}", LogHeader, LocalID, _orientation);
                    //changed |= UpdatedProperties.Rotation;
                }
                // if (_velocity != entprop.Velocity)
                if (!_velocity.ApproxEquals(entprop.Velocity, VELOCITY_TOLERANCE))
                {
                    if (!_lastVelocity.ApproxEquals(entprop.Velocity, 0.1f))
                    {
                        _lastVelocity = entprop.Velocity;
                        changed |= UpdatedProperties.Velocity;
                    }
                    _velocity = entprop.Velocity;
                    // MainConsole.Instance.DebugFormat("{0}: UpdateProperties: velocity = {1}", LogHeader, _velocity);
                }
                // if (_acceleration != entprop.Acceleration)
                if (!_acceleration.ApproxEquals(entprop.Acceleration, ACCELERATION_TOLERANCE))
                {
                    _acceleration = entprop.Acceleration;
                    // MainConsole.Instance.DebugFormat("{0}: UpdateProperties: acceleration = {1}", LogHeader, _acceleration);
                    //changed |= UpdatedProperties.Acceleration;
                }
                // if (_rotationalVelocity != entprop.RotationalVelocity)
                if (!_rotationalVelocity.ApproxEquals(entprop.RotationalVelocity, ROTATIONAL_VELOCITY_TOLERANCE))
                {
                    if (!_lastRotationalVelocity.ApproxEquals(entprop.RotationalVelocity, 0.1f))
                    {
                        _lastRotationalVelocity = entprop.RotationalVelocity;
                        changed |= UpdatedProperties.RotationalVel;
                    }
                    _rotationalVelocity = entprop.RotationalVelocity;
                    // MainConsole.Instance.DebugFormat("{0}: UpdateProperties: rotationalVelocity = {1}", LogHeader, _rotationalVelocity);
                }
                if (changed != 0)
                {
                    // MainConsole.Instance.DebugFormat("{0}: UpdateProperties: id={1}, c={2}, pos={3}, rot={4}", LogHeader, LocalID, changed, _position, _orientation);
                    // Only update the position of single objects and linkset roots
                    if (this._parentPrim == null && _limitedUpdatesCount > 7)
                    {
                        _limitedUpdatesCount = 0;
                        // MainConsole.Instance.DebugFormat("{0}: RequestTerseUpdate. id={1}, ch={2}, pos={3}, rot={4}", LogHeader, LocalID, changed, _position, _orientation);
                        base.RequestPhysicsterseUpdate();
                    }
                    else
                        _limitedUpdatesCount++;
                }
            }
            /*else
        {
            // Don't check for damping here -- it's done in BulletSim and SceneObjectPart.
            if (this._parentPrim == null)
            {
                // Assign to the local variables so the normal set action does not happen
                _position = entprop.Position;
                _orientation = entprop.Rotation;
                _velocity = entprop.Velocity;
                _acceleration = entprop.Acceleration;
                _rotationalVelocity = entprop.RotationalVelocity;
                // MainConsole.Instance.DebugFormat("{0}: RequestTerseUpdate. id={1}, ch={2}, pos={3}, rot={4}", LogHeader, LocalID, changed, _position, _orientation);
                base.RequestPhysicsterseUpdate();
            }
        }*/
        }

        public override void ClearVelocity()
        {
            _velocity = Vector3.Zero;
            Velocity = Vector3.Zero;
        }

        public override void ForceSetPosition(Vector3 position)
        {
            _position = position;
            Position = position;
        }

        public override void ForceSetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
            Velocity = velocity;
        }

        public override void SetMaterial(int material, bool forceMaterialSettings)
        {
            Material oldMaterial = (Material) _material;
            _material = material;
            Material m = (Material) material;
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
            //Rebuild now please
            _scene.TaintedObject(RecreateGeomAndObject);
        }

        #region Collisions

        private readonly CollisionEventUpdate args = new CollisionEventUpdate();

        // I've collided with something
        public void Collide(uint collidingWith, ActorTypes type, Vector3 contactPoint, Vector3 contactNormal,
                            float pentrationDepth)
        {
            // MainConsole.Instance.DebugFormat("{0}: Collide: ms={1}, id={2}, with={3}", LogHeader, _subscribedEventsMs, LocalID, collidingWith);

            // The following lines make IsColliding() and IsCollidingGround() work
            _collidingStep = _scene.SimulationStep;
            if (collidingWith == BSScene.TERRAIN_ID || collidingWith == BSScene.GROUNDPLANE_ID)
            {
                _collidingGroundStep = _scene.SimulationStep;
            }

            if (_subscribedEventsMs == 0) return; // nothing in the object is waiting for collision events
            // throttle the collisions to the number of milliseconds specified in the subscription
            int nowTime = _scene.SimulationNowTime;
            if (nowTime < (_lastCollisionTime + _subscribedEventsMs)) return;
            _lastCollisionTime = nowTime;

            // create the event for the collision
            Dictionary<uint, ContactPoint> contactPoints = new Dictionary<uint, ContactPoint>();
            AddCollisionEvent(collidingWith, new ContactPoint(contactPoint, contactNormal, pentrationDepth, type));
        }

        public override void SendCollisions()
        {
            if (!args.Cleared)
            {
                base.SendCollisionUpdate(args.Copy());
                args.Clear();
            }
        }

        public override void AddCollisionEvent(uint localID, ContactPoint contact)
        {
            args.addCollider(localID, contact);
        }

        #endregion

        #region Mass Calculation

        private float CalculateMass()
        {
            float volume = _size.X*_size.Y*_size.Z; // default
            float tmp;

            float returnMass = 0;
            float hollowAmount = _pbs.ProfileHollow*2.0e-5f;
            float hollowVolume = hollowAmount*hollowAmount;

            switch (_pbs.ProfileShape)
            {
                case ProfileShape.Square:
                    // default box

                    if (_pbs.PathCurve == (byte) Extrusion.Straight)
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

                                    hollowVolume *= (0.5f*.5f);
                                    break;

                                default:
                                    hollowVolume = 0;
                                    break;
                            }
                            volume *= (1.0f - hollowVolume);
                        }
                    }

                    else if (_pbs.PathCurve == (byte) Extrusion.Curve1)
                    {
                        //a tube 

                        volume *= 0.78539816339e-2f*(200 - _pbs.PathScaleX);
                        tmp = 1.0f - 2.0e-2f*(200 - _pbs.PathScaleY);
                        volume -= volume*tmp*tmp;

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
                                    hollowVolume *= 0.5f*0.5f;
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

                    if (_pbs.PathCurve == (byte) Extrusion.Straight)
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
                                    hollowVolume *= 0.5f*2.5984480504799f;
                                    break;

                                case HollowShape.Triangle:
                                    hollowVolume *= .5f*1.27323954473516f;
                                    break;

                                default:
                                    hollowVolume = 0;
                                    break;
                            }
                            volume *= (1.0f - hollowVolume);
                        }
                    }

                    else if (_pbs.PathCurve == (byte) Extrusion.Curve1)
                    {
                        volume *= 0.61685027506808491367715568749226e-2f*(200 - _pbs.PathScaleX);
                        tmp = 1.0f - .02f*(200 - _pbs.PathScaleY);
                        volume *= (1.0f - tmp*tmp);

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
                                    hollowVolume *= 0.5f*2.5984480504799f;
                                    break;

                                case HollowShape.Triangle:
                                    hollowVolume *= .5f*1.27323954473516f;
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
                    if (_pbs.PathCurve == (byte) Extrusion.Curve1)
                    {
                        volume *= 0.52359877559829887307710723054658f;
                    }
                    break;

                case ProfileShape.EquilateralTriangle:

                    if (_pbs.PathCurve == (byte) Extrusion.Straight)
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
                                    hollowVolume *= 0.499849f*3.07920140172638f;
                                    break;

                                case HollowShape.Circle:
                                    // Hollow shape is a perfect cyllinder in respect to the cube's scale
                                    // Cyllinder hollow volume calculation

                                    hollowVolume *= 0.1963495f*3.07920140172638f;
                                    break;

                                default:
                                    hollowVolume = 0;
                                    break;
                            }
                            volume *= (1.0f - hollowVolume);
                        }
                    }
                    else if (_pbs.PathCurve == (byte) Extrusion.Curve1)
                    {
                        volume *= 0.32475953f;
                        volume *= 0.01f*(200 - _pbs.PathScaleX);
                        tmp = 1.0f - .02f*(200 - _pbs.PathScaleY);
                        volume *= (1.0f - tmp*tmp);

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
                                    hollowVolume *= 0.499849f*3.07920140172638f;
                                    break;

                                case HollowShape.Circle:

                                    hollowVolume *= 0.1963495f*3.07920140172638f;
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

            if (_pbs.PathCurve == (byte) Extrusion.Straight || _pbs.PathCurve == (byte) Extrusion.Flexible)
            {
                taperX1 = _pbs.PathScaleX*0.01f;
                if (taperX1 > 1.0f)
                    taperX1 = 2.0f - taperX1;
                taperX = 1.0f - taperX1;

                taperY1 = _pbs.PathScaleY*0.01f;
                if (taperY1 > 1.0f)
                    taperY1 = 2.0f - taperY1;
                taperY = 1.0f - taperY1;
            }
            else
            {
                taperX = _pbs.PathTaperX*0.01f;
                if (taperX < 0.0f)
                    taperX = -taperX;
                taperX1 = 1.0f - taperX;

                taperY = _pbs.PathTaperY*0.01f;
                if (taperY < 0.0f)
                    taperY = -taperY;
                taperY1 = 1.0f - taperY;
            }


            volume *= (taperX1*taperY1 + 0.5f*(taperX1*taperY + taperX*taperY1) + 0.3333333333f*taperX*taperY);

            pathBegin = _pbs.PathBegin*2.0e-5f;
            pathEnd = 1.0f - _pbs.PathEnd*2.0e-5f;
            volume *= (pathEnd - pathBegin);

            // this is crude aproximation
            profileBegin = _pbs.ProfileBegin*2.0e-5f;
            profileEnd = 1.0f - _pbs.ProfileEnd*2.0e-5f;
            volume *= (profileEnd - profileBegin);

            returnMass = _parent_entity.Density*volume;

            if (IsRootOfLinkset)
            {
#if (!ISWIN)
                float sum = 0;
                foreach (BSPrim prim in _childrenPrims)
                    sum += prim.CalculateMass();
                returnMass += sum;
#else
                returnMass += _childrenPrims.Sum(prim => prim.CalculateMass());
#endif
            }

            if (returnMass <= 0)
                returnMass = 0.0001f;

            if (returnMass > _scene.MaximumObjectMass)
                returnMass = _scene.MaximumObjectMass;

            return returnMass;
        }

// end CalculateMass

        #endregion Mass Calculation

        #region Nested type: UpdatedProperties

        private enum UpdatedProperties
        {
            Position = 1 << 0,
            Rotation = 1 << 1,
            Velocity = 1 << 2,
            Acceleration = 1 << 3,
            RotationalVel = 1 << 4
        }

        #endregion
    }
}