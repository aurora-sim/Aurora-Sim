using System;
using System.Runtime.InteropServices;
using System.Security;
using Aurora.Framework.ConsoleFramework;

namespace Aurora.Physics.AuroraOpenDynamicsEngine
{
#if dDOUBLE
	using dReal = System.Double;
#else
    using dReal = Single;
    using Aurora.Framework.Modules;

#endif

    public static class d
    {
        #region Declares

        public static Single Infinity = Single.MaxValue;
        public static object m_odeSetupLock = new object();

        #endregion

        #region Flags and Enumerations

        [Flags]
        public enum AllocateODEDataFlags : uint
        {
            BasicData = 0,
            CollisionData = 0x00000001,
            All = ~0u
        }

        [Flags]
        public enum IniteODEFlags : uint
        {
            dInitFlagManualThreadCleanup = 0x00000001
        }

        [Flags]
        public enum ContactFlags : int
        {
            Mu2 = 0x001,
            FDir1 = 0x002,
            Bounce = 0x004,
            SoftERP = 0x008,
            SoftCFM = 0x010,
            Motion1 = 0x020,
            Motion2 = 0x040,
            MotionN = 0x080,
            Slip1 = 0x100,
            Slip2 = 0x200,
            Approx0 = 0x0000,
            Approx1_1 = 0x1000,
            Approx1_2 = 0x2000,
            Approx1 = 0x3000
        }

        public enum GeomClassID : int
        {
            SphereClass,
            BoxClass,
            CapsuleClass,
            CylinderClass,
            PlaneClass,
            RayClass,
            ConvexClass,
            GeomTransformClass,
            TriMeshClass,
            HeightfieldClass,
            FirstSpaceClass,
            SimpleSpaceClass = FirstSpaceClass,
            HashSpaceClass,
            QuadTreeSpaceClass,
            LastSpaceClass = QuadTreeSpaceClass,
            FirstUserClass,
            LastUserClass = FirstUserClass + MaxUserClasses - 1,
            NumClasses,
            MaxUserClasses = 4
        }

        public enum JointType : int
        {
            None,
            Ball,
            Hinge,
            Slider,
            Contact,
            Universal,
            Hinge2,
            Fixed,
            Null,
            AMotor,
            LMotor,
            Plane2D
        }

        public enum JointParam : int
        {
            LoStop,
            HiStop,
            Vel,
            FMax,
            FudgeFactor,
            Bounce,
            CFM,
            StopERP,
            StopCFM,
            SuspensionERP,
            SuspensionCFM,
            LoStop2 = 256,
            HiStop2,
            Vel2,
            FMax2,
            FudgeFactor2,
            Bounce2,
            CFM2,
            StopERP2,
            StopCFM2,
            SuspensionERP2,
            SuspensionCFM2,
            LoStop3 = 512,
            HiStop3,
            Vel3,
            FMax3,
            FudgeFactor3,
            Bounce3,
            CFM3,
            StopERP3,
            StopCFM3,
            SuspensionERP3,
            SuspensionCFM3
        }

        public enum dSweepAndPruneAxis : int
        {
            XYZ = ((0) | (1 << 2) | (2 << 4)),
            XZY = ((0) | (2 << 2) | (1 << 4)),
            YXZ = ((1) | (0 << 2) | (2 << 4)),
            YZX = ((1) | (2 << 2) | (0 << 4)),
            ZXY = ((2) | (0 << 2) | (1 << 4)),
            ZYX = ((2) | (1 << 2) | (0 << 4))
        }

        #endregion

        #region Callbacks

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int AABBTestFn(IntPtr o1, IntPtr o2, ref AABB aabb);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ColliderFn(IntPtr o1, IntPtr o2, int flags, out ContactGeom contact, int skip);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GetAABBFn(IntPtr geom, out AABB aabb);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate ColliderFn GetColliderFnFn(int num);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GeomDtorFn(IntPtr o);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate Single HeightfieldGetHeight(IntPtr p_user_data, int x, int z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NearCallback(IntPtr data, IntPtr geom1, IntPtr geom2);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int TriCallback(IntPtr trimesh, IntPtr refObject, int triangleIndex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int TriArrayCallback(IntPtr trimesh, IntPtr refObject, int[] triangleIndex, int triCount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int TriRayCallback(IntPtr trimesh, IntPtr ray, int triangleIndex, Single u, Single v);

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct AABB
        {
            public Single MinX, MaxX;
            public Single MinY, MaxY;
            public Single MinZ, MaxZ;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Contact
        {
            public SurfaceParameters surface;
            public ContactGeom geom;
            public Vector3 fdir1;
            public static readonly int unmanagedSizeOf = Marshal.SizeOf(typeof (ContactGeom));
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct ContactGeom
        {
            public Vector3 pos;
            public Vector3 normal;
            public Single depth;
            public IntPtr g1;
            public IntPtr g2;
            public int side1;
            public int side2;
            public static readonly int unmanagedSizeOf = Marshal.SizeOf(typeof (ContactGeom));
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GeomClass
        {
            public int bytes;
            public GetColliderFnFn collider;
            public GetAABBFn aabb;
            public AABBTestFn aabb_test;
            public GeomDtorFn dtor;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct JointFeedback
        {
            public Vector3 f1;
            public Vector3 t1;
            public Vector3 f2;
            public Vector3 t2;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Mass
        {
            public Single mass;
            public Vector4 c;
            public Matrix3 I;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Matrix3
        {
            public Matrix3(Single m00, Single m10, Single m20, Single m01, Single m11, Single m21, Single m02, Single m12,
                           Single m22)
            {
                M00 = m00;
                M10 = m10;
                M20 = m20;
                _m30 = 0.0f;
                M01 = m01;
                M11 = m11;
                M21 = m21;
                _m31 = 0.0f;
                M02 = m02;
                M12 = m12;
                M22 = m22;
                _m32 = 0.0f;
            }

            public Single M00, M10, M20;
            private Single _m30;
            public Single M01, M11, M21;
            private Single _m31;
            public Single M02, M12, M22;
            private Single _m32;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Matrix4
        {
            public Matrix4(Single m00, Single m10, Single m20, Single m30,
                           Single m01, Single m11, Single m21, Single m31,
                           Single m02, Single m12, Single m22, Single m32,
                           Single m03, Single m13, Single m23, Single m33)
            {
                M00 = m00;
                M10 = m10;
                M20 = m20;
                M30 = m30;
                M01 = m01;
                M11 = m11;
                M21 = m21;
                M31 = m31;
                M02 = m02;
                M12 = m12;
                M22 = m22;
                M32 = m32;
                M03 = m03;
                M13 = m13;
                M23 = m23;
                M33 = m33;
            }

            public Single M00, M10, M20, M30;
            public Single M01, M11, M21, M31;
            public Single M02, M12, M22, M32;
            public Single M03, M13, M23, M33;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Quaternion
        {
            public Single W, X, Y, Z;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct SurfaceParameters
        {
            public ContactFlags mode;
            public Single mu;
            public Single mu2;
            public Single bounce;
            public Single bounce_vel;
            public Single soft_erp;
            public Single soft_cfm;
            public Single motion1;
            public Single motion2;
            public Single motionN;
            public Single slip1;
            public Single slip2;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Vector3
        {
            public Vector3(Single x, Single y, Single z)
            {
                X = x;
                Y = y;
                Z = z;
                _w = 0.0f;
            }

            public Single X, Y, Z;
            private Single _w;

            public OpenMetaverse.Vector3 ToVector3()
            {
                return new OpenMetaverse.Vector3((float) X, (float) Y, (float) Z);
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Vector4
        {
            public Vector4(Single x, Single y, Single z, Single w)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
            }

            public Single X, Y, Z, W;
        }

        #endregion

        #region Methods

        [DllImport("ode", EntryPoint = "dAllocateODEDataForThread"), SuppressUnmanagedCodeSecurity]
        internal static extern int AllocateODEDataForThread(uint ODEInitFlags);

        [DllImport("ode", EntryPoint = "dAreConnected"), SuppressUnmanagedCodeSecurity]
        internal static extern bool AreConnected(IntPtr b1, IntPtr b2);

        [DllImport("ode", EntryPoint = "dAreConnectedExcluding"), SuppressUnmanagedCodeSecurity]
        internal static extern bool AreConnectedExcluding(IntPtr b1, IntPtr b2, JointType joint_type);

        [DllImport("ode", EntryPoint = "dBodyAddForce"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyAddForce(IntPtr body, Single fx, Single fy, Single fz);

        [DllImport("ode", EntryPoint = "dBodyAddForceAtPos"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyAddForceAtPos(IntPtr body, Single fx, Single fy, Single fz, Single px, Single py,
                                                    Single pz);

        [DllImport("ode", EntryPoint = "dBodyAddForceAtRelPos"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyAddForceAtRelPos(IntPtr body, Single fx, Single fy, Single fz, Single px, Single py,
                                                       Single pz);

        [DllImport("ode", EntryPoint = "dBodyAddRelForce"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyAddRelForce(IntPtr body, Single fx, Single fy, Single fz);

        [DllImport("ode", EntryPoint = "dBodyAddRelForceAtPos"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyAddRelForceAtPos(IntPtr body, Single fx, Single fy, Single fz, Single px, Single py,
                                                       Single pz);

        [DllImport("ode", EntryPoint = "dBodyAddRelForceAtRelPos"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyAddRelForceAtRelPos(IntPtr body, Single fx, Single fy, Single fz, Single px, Single py,
                                                          Single pz);

        [DllImport("ode", EntryPoint = "dBodyAddRelTorque"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyAddRelTorque(IntPtr body, Single fx, Single fy, Single fz);

        [DllImport("ode", EntryPoint = "dBodyAddTorque"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyAddTorque(IntPtr body, Single fx, Single fy, Single fz);

        [DllImport("ode", EntryPoint = "dBodyCopyPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyCopyPosition(IntPtr body, out Vector3 pos);

        [DllImport("ode", EntryPoint = "dBodyCopyPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyCopyPosition(IntPtr body, out Single X);

        [DllImport("ode", EntryPoint = "dBodyCopyQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyCopyQuaternion(IntPtr body, out Quaternion quat);

        [DllImport("ode", EntryPoint = "dBodyCopyQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyCopyQuaternion(IntPtr body, out Single X);

        [DllImport("ode", EntryPoint = "dBodyCopyRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyCopyRotation(IntPtr body, out Matrix3 R);

        [DllImport("ode", EntryPoint = "dBodyCopyRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyCopyRotation(IntPtr body, out Single M00);

        [DllImport("ode", EntryPoint = "dBodyCreate"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr BodyCreate(IntPtr world);

        [DllImport("ode", EntryPoint = "dBodyDestroy"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyDestroy(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyDisable"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyDisable(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyEnable"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyEnable(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetAutoDisableAngularThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern Single BodyGetAutoDisableAngularThreshold(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetAutoDisableFlag"), SuppressUnmanagedCodeSecurity]
        internal static extern bool BodyGetAutoDisableFlag(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetAutoDisableDefaults"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyGetAutoDisableDefaults(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetAutoDisableLinearThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern Single BodyGetAutoDisableLinearThreshold(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetAutoDisableSteps"), SuppressUnmanagedCodeSecurity]
        internal static extern int BodyGetAutoDisableSteps(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetAutoDisableTime"), SuppressUnmanagedCodeSecurity]
        internal static extern Single BodyGetAutoDisableTime(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetAngularVel"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe Vector3* BodyGetAngularVelUnsafe(IntPtr body);

        internal static Vector3 BodyGetAngularVel(IntPtr body)
        {
            unsafe
            {
                return *(BodyGetAngularVelUnsafe(body));
            }
        }

        [DllImport("ode", EntryPoint = "dBodyGetData"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr BodyGetData(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetFiniteRotationMode"), SuppressUnmanagedCodeSecurity]
        internal static extern int BodyGetFiniteRotationMode(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetFiniteRotationAxis"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyGetFiniteRotationAxis(IntPtr body, out Vector3 result);

        [DllImport("ode", EntryPoint = "dBodyGetForce"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe Vector3* BodyGetForceUnsafe(IntPtr body);

        internal static Vector3 BodyGetForce(IntPtr body)
        {
            unsafe
            {
                return *(BodyGetForceUnsafe(body));
            }
        }

        [DllImport("ode", EntryPoint = "dBodyGetGravityMode"), SuppressUnmanagedCodeSecurity]
        internal static extern bool BodyGetGravityMode(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetGyroscopicMode"), SuppressUnmanagedCodeSecurity]
        internal static extern int BodyGetGyroscopicMode(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetJoint"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr BodyGetJoint(IntPtr body, int index);

        [DllImport("ode", EntryPoint = "dBodyGetLinearVel"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe Vector3* BodyGetLinearVelUnsafe(IntPtr body);

        internal static Vector3 BodyGetLinearVel(IntPtr body)
        {
            unsafe
            {
                return *(BodyGetLinearVelUnsafe(body));
            }
        }

        [DllImport("ode", EntryPoint = "dBodyGetMass"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyGetMass(IntPtr body, out Mass mass);

        [DllImport("ode", EntryPoint = "dBodyGetNumJoints"), SuppressUnmanagedCodeSecurity]
        internal static extern int BodyGetNumJoints(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetPointVel"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyGetPointVel(IntPtr body, Single px, Single py, Single pz, out Vector3 result);

        [DllImport("ode", EntryPoint = "dBodyGetPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe Vector3* BodyGetPositionUnsafe(IntPtr body);

        internal static Vector3 BodyGetPosition(IntPtr body)
        {
            unsafe
            {
                return *(BodyGetPositionUnsafe(body));
            }
        }

        [DllImport("ode", EntryPoint = "dBodyGetPosRelPoint"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyGetPosRelPoint(IntPtr body, Single px, Single py, Single pz, out Vector3 result);

        [DllImport("ode", EntryPoint = "dBodyGetQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe Quaternion* BodyGetQuaternionUnsafe(IntPtr body);

        internal static Quaternion BodyGetQuaternion(IntPtr body)
        {
            unsafe
            {
                return *(BodyGetQuaternionUnsafe(body));
            }
        }

        [DllImport("ode", EntryPoint = "dBodyGetRelPointPos"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyGetRelPointPos(IntPtr body, Single px, Single py, Single pz, out Vector3 result);

        [DllImport("ode", EntryPoint = "dBodyGetRelPointVel"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyGetRelPointVel(IntPtr body, Single px, Single py, Single pz, out Vector3 result);

        [DllImport("ode", EntryPoint = "dBodyGetRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe Matrix3* BodyGetRotationUnsafe(IntPtr body);

        internal static Matrix3 BodyGetRotation(IntPtr body)
        {
            unsafe
            {
                return *(BodyGetRotationUnsafe(body));
            }
        }

        [DllImport("ode", EntryPoint = "dBodyGetTorque"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe Vector3* BodyGetTorqueUnsafe(IntPtr body);

        internal static Vector3 BodyGetTorque(IntPtr body)
        {
            unsafe
            {
                return *(BodyGetTorqueUnsafe(body));
            }
        }

        [DllImport("ode", EntryPoint = "dBodyGetWorld"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr BodyGetWorld(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyIsEnabled"), SuppressUnmanagedCodeSecurity]
        internal static extern bool BodyIsEnabled(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodySetAngularVel"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetAngularVel(IntPtr body, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dBodySetAutoDisableAngularThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetAutoDisableAngularThreshold(IntPtr body, Single angular_threshold);

        [DllImport("ode", EntryPoint = "dBodySetAutoDisableDefaults"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetAutoDisableDefaults(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodySetAutoDisableFlag"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetAutoDisableFlag(IntPtr body, bool do_auto_disable);

        [DllImport("ode", EntryPoint = "dBodySetAutoDisableLinearThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetAutoDisableLinearThreshold(IntPtr body, Single linear_threshold);

        [DllImport("ode", EntryPoint = "dBodySetAutoDisableSteps"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetAutoDisableSteps(IntPtr body, int steps);

        [DllImport("ode", EntryPoint = "dBodySetAutoDisableTime"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetAutoDisableTime(IntPtr body, Single time);

        [DllImport("ode", EntryPoint = "dBodySetData"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetData(IntPtr body, IntPtr data);

        [DllImport("ode", EntryPoint = "dBodySetFiniteRotationMode"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetFiniteRotationMode(IntPtr body, int mode);

        [DllImport("ode", EntryPoint = "dBodySetFiniteRotationAxis"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetFiniteRotationAxis(IntPtr body, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dBodySetLinearDamping"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetLinearDamping(IntPtr body, Single scale);

        [DllImport("ode", EntryPoint = "dBodySetAngularDamping"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetAngularDamping(IntPtr body, Single scale);

        [DllImport("ode", EntryPoint = "dBodyGetLinearDamping"), SuppressUnmanagedCodeSecurity]
        internal static extern Single BodyGetLinearDamping(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetAngularDamping"), SuppressUnmanagedCodeSecurity]
        internal static extern Single BodyGetAngularDamping(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodySetAngularDamping"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetDamping(IntPtr body, Single linear_scale, Single angular_scale);

        [DllImport("ode", EntryPoint = "dBodySetAngularDampingThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetAngularDampingThreshold(IntPtr body, Single threshold);

        [DllImport("ode", EntryPoint = "dBodySetLinearDampingThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetLinearDampingThreshold(IntPtr body, Single threshold);

        [DllImport("ode", EntryPoint = "dBodyGetLinearDampingThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern Single BodyGetLinearDampingThreshold(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodyGetAngularDampingThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern Single BodyGetAngularDampingThreshold(IntPtr body);

        [DllImport("ode", EntryPoint = "dBodySetForce"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetForce(IntPtr body, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dBodySetGravityMode"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetGravityMode(IntPtr body, bool mode);

        /// <summary>
        ///     Sets the Gyroscopic term status on the body specified.
        /// </summary>
        /// <param name="body">Pointer to body</param>
        /// <param name="enabled">NonZero enabled, Zero disabled</param>
        [DllImport("ode", EntryPoint = "dBodySetGyroscopicMode"), SuppressUnmanagedCodeSecurity]
        internal static extern void dBodySetGyroscopicMode(IntPtr body, int enabled);

        [DllImport("ode", EntryPoint = "dBodySetLinearVel"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetLinearVel(IntPtr body, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dBodySetMass"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetMass(IntPtr body, ref Mass mass);

        [DllImport("ode", EntryPoint = "dBodySetPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetPosition(IntPtr body, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dBodySetQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetQuaternion(IntPtr body, ref Quaternion q);

        [DllImport("ode", EntryPoint = "dBodySetQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetQuaternion(IntPtr body, ref Single w);

        [DllImport("ode", EntryPoint = "dBodySetRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetRotation(IntPtr body, ref Matrix3 R);

        [DllImport("ode", EntryPoint = "dBodySetRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetRotation(IntPtr body, ref Single M00);

        [DllImport("ode", EntryPoint = "dBodySetTorque"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodySetTorque(IntPtr body, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dBodyVectorFromWorld"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyVectorFromWorld(IntPtr body, Single px, Single py, Single pz, out Vector3 result);

        [DllImport("ode", EntryPoint = "dBodyVectorToWorld"), SuppressUnmanagedCodeSecurity]
        internal static extern void BodyVectorToWorld(IntPtr body, Single px, Single py, Single pz, out Vector3 result);

        [DllImport("ode", EntryPoint = "dBoxBox"), SuppressUnmanagedCodeSecurity]
        internal static extern void BoxBox(ref Vector3 p1, ref Matrix3 R1,
                                         ref Vector3 side1, ref Vector3 p2,
                                         ref Matrix3 R2, ref Vector3 side2,
                                         ref Vector3 normal, out Single depth, out int return_code,
                                         int maxc, out ContactGeom contact, int skip);

        [DllImport("ode", EntryPoint = "dBoxTouchesBox"), SuppressUnmanagedCodeSecurity]
        internal static extern void BoxTouchesBox(ref Vector3 _p1, ref Matrix3 R1,
                                                ref Vector3 side1, ref Vector3 _p2,
                                                ref Matrix3 R2, ref Vector3 side2);

        [DllImport("ode", EntryPoint = "dCleanupODEAllDataForThread"), SuppressUnmanagedCodeSecurity]
        internal static extern void CleanupODEAllDataForThread();

        [DllImport("ode", EntryPoint = "dClosestLineSegmentPoints"), SuppressUnmanagedCodeSecurity]
        internal static extern void ClosestLineSegmentPoints(ref Vector3 a1, ref Vector3 a2,
                                                           ref Vector3 b1, ref Vector3 b2,
                                                           ref Vector3 cp1, ref Vector3 cp2);

        [DllImport("ode", EntryPoint = "dCloseODE"), SuppressUnmanagedCodeSecurity]
        internal static extern void CloseODE();

        [DllImport("ode", EntryPoint = "dCollide"), SuppressUnmanagedCodeSecurity]
        internal static extern int Collide(IntPtr o1, IntPtr o2, int flags,
                                         [In, Out] ContactGeom[] contactcontactgeomarray, int skip);

        [DllImport("ode", EntryPoint = "dCollide"), SuppressUnmanagedCodeSecurity]
        internal static extern int CollidePtr([In] IntPtr o1, [In] IntPtr o2, [In] int flags, [In] IntPtr contactgeomarray,
                                            [In] int skip);

        [DllImport("ode", EntryPoint = "dConnectingJoint"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr ConnectingJoint([In] IntPtr j1, [In] IntPtr j2);

        [DllImport("ode", EntryPoint = "dCreateBox"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr CreateBox([In] IntPtr space, [In] Single lx, [In] Single ly, [In] Single lz);

        [DllImport("ode", EntryPoint = "dCreateCapsule"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr CreateCapsule([In] IntPtr space, [In] Single radius, [In] Single length);

        [DllImport("ode", EntryPoint = "dCreateConvex"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr CreateConvex([In] IntPtr space, [In] Single[] planes, [In] int planeCount,
                                                 [In] Single[] points, [In] int pointCount, [In] int[] polygons);

        [DllImport("ode", EntryPoint = "dCreateCylinder"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr CreateCylinder(IntPtr space, Single radius, Single length);

        [DllImport("ode", EntryPoint = "dCreateHeightfield"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr CreateHeightfield(IntPtr space, IntPtr data, int bPlaceable);

        [DllImport("ode", EntryPoint = "dCreateGeom"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr CreateGeom(int classnum);

        [DllImport("ode", EntryPoint = "dCreateGeomClass"), SuppressUnmanagedCodeSecurity]
        internal static extern int CreateGeomClass(ref GeomClass classptr);

        [DllImport("ode", EntryPoint = "dCreateGeomTransform"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr CreateGeomTransform(IntPtr space);

        [DllImport("ode", EntryPoint = "dCreatePlane"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr CreatePlane(IntPtr space, Single a, Single b, Single c, Single d);

        [DllImport("ode", EntryPoint = "dCreateRay"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr CreateRay(IntPtr space, Single length);

        [DllImport("ode", EntryPoint = "dCreateSphere"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr CreateSphere(IntPtr space, Single radius);

        [DllImport("ode", EntryPoint = "dCreateTriMesh"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr CreateTriMesh(IntPtr space, IntPtr data,
                                                  TriCallback callback, TriArrayCallback arrayCallback,
                                                  TriRayCallback rayCallback);

        [DllImport("ode", EntryPoint = "dDot"), SuppressUnmanagedCodeSecurity]
        internal static extern Single Dot(ref Single X0, ref Single X1, int n);

        [DllImport("ode", EntryPoint = "dDQfromW"), SuppressUnmanagedCodeSecurity]
        internal static extern void DQfromW(Single[] dq, ref Vector3 w, ref Quaternion q);

        [DllImport("ode", EntryPoint = "dFactorCholesky"), SuppressUnmanagedCodeSecurity]
        internal static extern int FactorCholesky(ref Single A00, int n);

        [DllImport("ode", EntryPoint = "dFactorLDLT"), SuppressUnmanagedCodeSecurity]
        internal static extern void FactorLDLT(ref Single A, out Single d, int n, int nskip);

        [DllImport("ode", EntryPoint = "dGeomBoxGetLengths"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomBoxGetLengths(IntPtr geom, out Vector3 len);

        [DllImport("ode", EntryPoint = "dGeomBoxGetLengths"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomBoxGetLengths(IntPtr geom, out Single x);

        [DllImport("ode", EntryPoint = "dGeomBoxPointDepth"), SuppressUnmanagedCodeSecurity]
        internal static extern Single GeomBoxPointDepth(IntPtr geom, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dGeomBoxSetLengths"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomBoxSetLengths(IntPtr geom, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dGeomCapsuleGetParams"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomCapsuleGetParams(IntPtr geom, out Single radius, out Single length);

        [DllImport("ode", EntryPoint = "dGeomCapsulePointDepth"), SuppressUnmanagedCodeSecurity]
        internal static extern Single GeomCapsulePointDepth(IntPtr geom, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dGeomCapsuleSetParams"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomCapsuleSetParams(IntPtr geom, Single radius, Single length);

        [DllImport("ode", EntryPoint = "dGeomClearOffset"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomClearOffset(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomCopyOffsetPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomCopyOffsetPosition(IntPtr geom, ref Vector3 pos);

        [DllImport("ode", EntryPoint = "dGeomCopyOffsetPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomCopyOffsetPosition(IntPtr geom, ref Single X);

        [DllImport("ode", EntryPoint = "dGeomGetOffsetQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomCopyOffsetQuaternion(IntPtr geom, ref Quaternion Q);

        [DllImport("ode", EntryPoint = "dGeomGetOffsetQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomCopyOffsetQuaternion(IntPtr geom, ref Single X);

        [DllImport("ode", EntryPoint = "dGeomCopyOffsetRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomCopyOffsetRotation(IntPtr geom, ref Matrix3 R);

        [DllImport("ode", EntryPoint = "dGeomCopyOffsetRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomCopyOffsetRotation(IntPtr geom, ref Single M00);

        [DllImport("ode", EntryPoint = "dGeomCopyPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomCopyPosition(IntPtr geom, out Vector3 pos);

        [DllImport("ode", EntryPoint = "dGeomCopyPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomCopyPosition(IntPtr geom, out Single X);

        [DllImport("ode", EntryPoint = "dGeomCopyRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomCopyRotation(IntPtr geom, out Matrix3 R);

        [DllImport("ode", EntryPoint = "dGeomCopyRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomCopyRotation(IntPtr geom, out Single M00);

        [DllImport("ode", EntryPoint = "dGeomCylinderGetParams"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomCylinderGetParams(IntPtr geom, out Single radius, out Single length);

        [DllImport("ode", EntryPoint = "dGeomCylinderSetParams"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomCylinderSetParams(IntPtr geom, Single radius, Single length);

        [DllImport("ode", EntryPoint = "dGeomDestroy"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomDestroy(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomDisable"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomDisable(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomEnable"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomEnable(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomGetAABB"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomGetAABB(IntPtr geom, out AABB aabb);

        [DllImport("ode", EntryPoint = "dGeomGetAABB"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomGetAABB(IntPtr geom, out Single minX);

        [DllImport("ode", EntryPoint = "dGeomGetBody"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomGetBody(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomGetCategoryBits"), SuppressUnmanagedCodeSecurity]
        internal static extern int GeomGetCategoryBits(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomGetClassData"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomGetClassData(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomGetCollideBits"), SuppressUnmanagedCodeSecurity]
        internal static extern int GeomGetCollideBits(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomGetClass"), SuppressUnmanagedCodeSecurity]
        internal static extern GeomClassID GeomGetClass(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomGetData"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomGetData(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomGetOffsetPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe Vector3* GeomGetOffsetPositionUnsafe(IntPtr geom);

        internal static Vector3 GeomGetOffsetPosition(IntPtr geom)
        {
            unsafe
            {
                return *(GeomGetOffsetPositionUnsafe(geom));
            }
        }

        [DllImport("ode", EntryPoint = "dGeomGetOffsetRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe Matrix3* GeomGetOffsetRotationUnsafe(IntPtr geom);

        internal static Matrix3 GeomGetOffsetRotation(IntPtr geom)
        {
            unsafe
            {
                return *(GeomGetOffsetRotationUnsafe(geom));
            }
        }

        [DllImport("ode", EntryPoint = "dGeomGetPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe Vector3* GeomGetPositionUnsafe(IntPtr geom);

        internal static Vector3 GeomGetPosition(IntPtr geom)
        {
            unsafe
            {
                return *(GeomGetPositionUnsafe(geom));
            }
        }

        [DllImport("ode", EntryPoint = "dGeomGetQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomCopyQuaternion(IntPtr geom, out Quaternion q);

        [DllImport("ode", EntryPoint = "dGeomGetQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomCopyQuaternion(IntPtr geom, out Single X);

        [DllImport("ode", EntryPoint = "dGeomGetRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe Matrix3* GeomGetRotationUnsafe(IntPtr geom);

        internal static Matrix3 GeomGetRotation(IntPtr geom)
        {
            unsafe
            {
                return *(GeomGetRotationUnsafe(geom));
            }
        }

        [DllImport("ode", EntryPoint = "dGeomGetSpace"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomGetSpace(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataBuildByte"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldDataBuildByte(IntPtr d, byte[] pHeightData, int bCopyHeightData,
                                                               Single width, Single depth, int widthSamples,
                                                               int depthSamples,
                                                               Single scale, Single offset, Single thickness, int bWrap);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataBuildByte"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldDataBuildByte(IntPtr d, IntPtr pHeightData, int bCopyHeightData,
                                                               Single width, Single depth, int widthSamples,
                                                               int depthSamples,
                                                               Single scale, Single offset, Single thickness, int bWrap);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataBuildCallback"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldDataBuildCallback(IntPtr d, IntPtr pUserData,
                                                                   HeightfieldGetHeight pCallback,
                                                                   Single width, Single depth, int widthSamples,
                                                                   int depthSamples,
                                                                   Single scale, Single offset, Single thickness, int bWrap);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataBuildShort"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldDataBuildShort(IntPtr d, ushort[] pHeightData, int bCopyHeightData,
                                                                Single width, Single depth, int widthSamples,
                                                                int depthSamples,
                                                                Single scale, Single offset, Single thickness, int bWrap);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataBuildShort"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldDataBuildShort(IntPtr d, short[] pHeightData, int bCopyHeightData,
                                                                Single width, Single depth, int widthSamples,
                                                                int depthSamples,
                                                                Single scale, Single offset, Single thickness, int bWrap);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataBuildShort"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldDataBuildShort(IntPtr d, IntPtr pHeightData, int bCopyHeightData,
                                                                Single width, Single depth, int widthSamples,
                                                                int depthSamples,
                                                                Single scale, Single offset, Single thickness, int bWrap);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataBuildSingle"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldDataBuildSingle(IntPtr d, float[] pHeightData, int bCopyHeightData,
                                                                 Single width, Single depth, int widthSamples,
                                                                 int depthSamples,
                                                                 Single scale, Single offset, Single thickness, int bWrap);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataBuildSingle"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldDataBuildSingle(IntPtr d, IntPtr pHeightData, int bCopyHeightData,
                                                                 Single width, Single depth, int widthSamples,
                                                                 int depthSamples,
                                                                 Single scale, Single offset, Single thickness, int bWrap);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataBuildDouble"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldDataBuildDouble(IntPtr d, double[] pHeightData, int bCopyHeightData,
                                                                 Single width, Single depth, int widthSamples,
                                                                 int depthSamples,
                                                                 Single scale, Single offset, Single thickness, int bWrap);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataBuildDouble"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldDataBuildDouble(IntPtr d, IntPtr pHeightData, int bCopyHeightData,
                                                                 Single width, Single depth, int widthSamples,
                                                                 int depthSamples,
                                                                 Single scale, Single offset, Single thickness, int bWrap);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataCreate"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomHeightfieldDataCreate();

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataDestroy"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldDataDestroy(IntPtr d);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldDataSetBounds"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldDataSetBounds(IntPtr d, Single minHeight, Single maxHeight);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldGetHeightfieldData"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomHeightfieldGetHeightfieldData(IntPtr g);

        [DllImport("ode", EntryPoint = "dGeomHeightfieldSetHeightfieldData"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomHeightfieldSetHeightfieldData(IntPtr g, IntPtr d);

        [DllImport("ode", EntryPoint = "dGeomIsEnabled"), SuppressUnmanagedCodeSecurity]
        internal static extern bool GeomIsEnabled(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomIsOffset"), SuppressUnmanagedCodeSecurity]
        internal static extern bool GeomIsOffset(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomIsSpace"), SuppressUnmanagedCodeSecurity]
        internal static extern bool GeomIsSpace(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomPlaneGetParams"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomPlaneGetParams(IntPtr geom, ref Vector4 result);

        [DllImport("ode", EntryPoint = "dGeomPlaneGetParams"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomPlaneGetParams(IntPtr geom, ref Single A);

        [DllImport("ode", EntryPoint = "dGeomPlanePointDepth"), SuppressUnmanagedCodeSecurity]
        internal static extern Single GeomPlanePointDepth(IntPtr geom, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dGeomPlaneSetParams"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomPlaneSetParams(IntPtr plane, Single a, Single b, Single c, Single d);

        [DllImport("ode", EntryPoint = "dGeomRayGet"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomRayGet(IntPtr ray, ref Vector3 start, ref Vector3 dir);

        [DllImport("ode", EntryPoint = "dGeomRayGet"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomRayGet(IntPtr ray, ref Single startX, ref Single dirX);

        [DllImport("ode", EntryPoint = "dGeomRayGetClosestHit"), SuppressUnmanagedCodeSecurity]
        internal static extern int GeomRayGetClosestHit(IntPtr ray);

        [DllImport("ode", EntryPoint = "dGeomRayGetLength"), SuppressUnmanagedCodeSecurity]
        internal static extern Single GeomRayGetLength(IntPtr ray);

        [DllImport("ode", EntryPoint = "dGeomRayGetParams"), SuppressUnmanagedCodeSecurity]
        internal static extern Single GeomRayGetParams(IntPtr g, out int firstContact, out int backfaceCull);

        [DllImport("ode", EntryPoint = "dGeomRaySet"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomRaySet(IntPtr ray, Single px, Single py, Single pz, Single dx, Single dy, Single dz);

        [DllImport("ode", EntryPoint = "dGeomRaySetClosestHit"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomRaySetClosestHit(IntPtr ray, int closestHit);

        [DllImport("ode", EntryPoint = "dGeomRaySetLength"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomRaySetLength(IntPtr ray, Single length);

        [DllImport("ode", EntryPoint = "dGeomRaySetParams"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomRaySetParams(IntPtr ray, int firstContact, int backfaceCull);

        [DllImport("ode", EntryPoint = "dGeomSetBody"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetBody(IntPtr geom, IntPtr body);

        [DllImport("ode", EntryPoint = "dGeomSetCategoryBits"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetCategoryBits(IntPtr geom, int bits);

        [DllImport("ode", EntryPoint = "dGeomSetCollideBits"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetCollideBits(IntPtr geom, int bits);

        [DllImport("ode", EntryPoint = "dGeomSetConvex"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomSetConvex(IntPtr geom, Single[] planes, int planeCount, Single[] points,
                                                  int pointCount, int[] polygons);

        [DllImport("ode", EntryPoint = "dGeomSetData"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetData(IntPtr geom, IntPtr data);

        [DllImport("ode", EntryPoint = "dGeomSetOffsetPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetOffsetPosition(IntPtr geom, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dGeomSetOffsetQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetOffsetQuaternion(IntPtr geom, ref Quaternion Q);

        [DllImport("ode", EntryPoint = "dGeomSetOffsetQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetOffsetQuaternion(IntPtr geom, ref Single X);

        [DllImport("ode", EntryPoint = "dGeomSetOffsetRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetOffsetRotation(IntPtr geom, ref Matrix3 R);

        [DllImport("ode", EntryPoint = "dGeomSetOffsetRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetOffsetRotation(IntPtr geom, ref Single M00);

        [DllImport("ode", EntryPoint = "dGeomSetOffsetWorldPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetOffsetWorldPosition(IntPtr geom, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dGeomSetOffsetWorldQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetOffsetWorldQuaternion(IntPtr geom, ref Quaternion Q);

        [DllImport("ode", EntryPoint = "dGeomSetOffsetWorldQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetOffsetWorldQuaternion(IntPtr geom, ref Single X);

        [DllImport("ode", EntryPoint = "dGeomSetOffsetWorldRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetOffsetWorldRotation(IntPtr geom, ref Matrix3 R);

        [DllImport("ode", EntryPoint = "dGeomSetOffsetWorldRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetOffsetWorldRotation(IntPtr geom, ref Single M00);

        [DllImport("ode", EntryPoint = "dGeomSetPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetPosition(IntPtr geom, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dGeomSetQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetQuaternion(IntPtr geom, ref Quaternion quat);

        [DllImport("ode", EntryPoint = "dGeomSetQuaternion"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetQuaternion(IntPtr geom, ref Single w);

        [DllImport("ode", EntryPoint = "dGeomSetRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetRotation(IntPtr geom, ref Matrix3 R);

        [DllImport("ode", EntryPoint = "dGeomSetRotation"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSetRotation(IntPtr geom, ref Single M00);

        [DllImport("ode", EntryPoint = "dGeomSphereGetRadius"), SuppressUnmanagedCodeSecurity]
        internal static extern Single GeomSphereGetRadius(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomSpherePointDepth"), SuppressUnmanagedCodeSecurity]
        internal static extern Single GeomSpherePointDepth(IntPtr geom, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dGeomSphereSetRadius"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomSphereSetRadius(IntPtr geom, Single radius);

        [DllImport("ode", EntryPoint = "dGeomTransformGetCleanup"), SuppressUnmanagedCodeSecurity]
        internal static extern int GeomTransformGetCleanup(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomTransformGetGeom"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomTransformGetGeom(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomTransformGetInfo"), SuppressUnmanagedCodeSecurity]
        internal static extern int GeomTransformGetInfo(IntPtr geom);

        [DllImport("ode", EntryPoint = "dGeomTransformSetCleanup"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTransformSetCleanup(IntPtr geom, int mode);

        [DllImport("ode", EntryPoint = "dGeomTransformSetGeom"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTransformSetGeom(IntPtr geom, IntPtr obj);

        [DllImport("ode", EntryPoint = "dGeomTransformSetInfo"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTransformSetInfo(IntPtr geom, int info);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataBuildDouble"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataBuildDouble(IntPtr d,
                                                             double[] vertices, int vertexStride, int vertexCount,
                                                             int[] indices, int indexCount, int triStride);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataBuildDouble"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataBuildDouble(IntPtr d,
                                                             IntPtr vertices, int vertexStride, int vertexCount,
                                                             IntPtr indices, int indexCount, int triStride);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataBuildDouble1"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataBuildDouble1(IntPtr d,
                                                              double[] vertices, int vertexStride, int vertexCount,
                                                              int[] indices, int indexCount, int triStride,
                                                              double[] normals);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataBuildDouble1"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataBuildDouble(IntPtr d,
                                                             IntPtr vertices, int vertexStride, int vertexCount,
                                                             IntPtr indices, int indexCount, int triStride,
                                                             IntPtr normals);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataBuildSimple"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataBuildSingle(IntPtr d,
                                                             Single[] vertices, int vertexStride, int vertexCount,
                                                             int[] indices, int indexCount, int triStride);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataBuildSimple"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataBuildSingle(IntPtr d,
                                                             IntPtr vertices, int vertexStride, int vertexCount,
                                                             IntPtr indices, int indexCount, int triStride);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataBuildSimple1"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataBuildSingle1(IntPtr d,
                                                              Single[] vertices, int vertexStride, int vertexCount,
                                                              int[] indices, int indexCount, int triStride,
                                                              Single[] normals);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataBuildSimple1"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataBuildSingle1(IntPtr d,
                                                              IntPtr vertices, int vertexStride, int vertexCount,
                                                              IntPtr indices, int indexCount, int triStride,
                                                              IntPtr normals);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataBuildSingle"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataBuildSimple(IntPtr d,
                                                             float[] vertices, int vertexStride, int vertexCount,
                                                             int[] indices, int indexCount, int triStride);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataBuildSingle"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataBuildSimple(IntPtr d,
                                                             IntPtr vertices, int vertexStride, int vertexCount,
                                                             IntPtr indices, int indexCount, int triStride);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataBuildSingle1"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataBuildSimple1(IntPtr d,
                                                              float[] vertices, int vertexStride, int vertexCount,
                                                              int[] indices, int indexCount, int triStride,
                                                              float[] normals);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataBuildSingle1"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataBuildSimple1(IntPtr d,
                                                              IntPtr vertices, int vertexStride, int vertexCount,
                                                              IntPtr indices, int indexCount, int triStride,
                                                              IntPtr normals);

        [DllImport("ode", EntryPoint = "dGeomTriMeshClearTCCache"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshClearTCCache(IntPtr g);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataCreate"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomTriMeshDataCreate();

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataDestroy"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataDestroy(IntPtr d);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataGet"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomTriMeshDataGet(IntPtr d, int data_id);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataPreprocess"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataPreprocess(IntPtr d);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataSet"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataSet(IntPtr d, int data_id, IntPtr in_data);

        [DllImport("ode", EntryPoint = "dGeomTriMeshDataUpdate"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshDataUpdate(IntPtr d);

        [DllImport("ode", EntryPoint = "dGeomTriMeshEnableTC"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshEnableTC(IntPtr g, int geomClass, bool enable);

        [DllImport("ode", EntryPoint = "dGeomTriMeshGetArrayCallback"), SuppressUnmanagedCodeSecurity]
        internal static extern TriArrayCallback GeomTriMeshGetArrayCallback(IntPtr g);

        [DllImport("ode", EntryPoint = "dGeomTriMeshGetCallback"), SuppressUnmanagedCodeSecurity]
        internal static extern TriCallback GeomTriMeshGetCallback(IntPtr g);

        [DllImport("ode", EntryPoint = "dGeomTriMeshGetData"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomTriMeshGetData(IntPtr g);

        [DllImport("ode", EntryPoint = "dGeomTriMeshGetLastTransform"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe Matrix4* GeomTriMeshGetLastTransformUnsafe(IntPtr geom);

        internal static Matrix4 GeomTriMeshGetLastTransform(IntPtr geom)
        {
            unsafe
            {
                return *(GeomTriMeshGetLastTransformUnsafe(geom));
            }
        }

        [DllImport("ode", EntryPoint = "dGeomTriMeshGetPoint"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshGetPoint(IntPtr g, int index, Single u, Single v, ref Vector3 outVec);

        [DllImport("ode", EntryPoint = "dGeomTriMeshGetRayCallback"), SuppressUnmanagedCodeSecurity]
        internal static extern TriRayCallback GeomTriMeshGetRayCallback(IntPtr g);

        [DllImport("ode", EntryPoint = "dGeomTriMeshGetTriangle"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshGetTriangle(IntPtr g, int index, ref Vector3 v0, ref Vector3 v1,
                                                         ref Vector3 v2);

        [DllImport("ode", EntryPoint = "dGeomTriMeshGetTriangleCount"), SuppressUnmanagedCodeSecurity]
        internal static extern int GeomTriMeshGetTriangleCount(IntPtr g);

        [DllImport("ode", EntryPoint = "dGeomTriMeshGetTriMeshDataID"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GeomTriMeshGetTriMeshDataID(IntPtr g);

        [DllImport("ode", EntryPoint = "dGeomTriMeshIsTCEnabled"), SuppressUnmanagedCodeSecurity]
        internal static extern bool GeomTriMeshIsTCEnabled(IntPtr g, int geomClass);

        [DllImport("ode", EntryPoint = "dGeomTriMeshSetArrayCallback"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshSetArrayCallback(IntPtr g, TriArrayCallback arrayCallback);

        [DllImport("ode", EntryPoint = "dGeomTriMeshSetCallback"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshSetCallback(IntPtr g, TriCallback callback);

        [DllImport("ode", EntryPoint = "dGeomTriMeshSetData"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshSetData(IntPtr g, IntPtr data);

        [DllImport("ode", EntryPoint = "dGeomTriMeshSetLastTransform"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshSetLastTransform(IntPtr g, ref Matrix4 last_trans);

        [DllImport("ode", EntryPoint = "dGeomTriMeshSetLastTransform"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshSetLastTransform(IntPtr g, ref Single M00);

        [DllImport("ode", EntryPoint = "dGeomTriMeshSetRayCallback"), SuppressUnmanagedCodeSecurity]
        internal static extern void GeomTriMeshSetRayCallback(IntPtr g, TriRayCallback callback);

        [DllImport("ode", EntryPoint = "dHashSpaceCreate"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr HashSpaceCreate(IntPtr space);

        [DllImport("ode", EntryPoint = "dHashSpaceGetLevels"), SuppressUnmanagedCodeSecurity]
        internal static extern void HashSpaceGetLevels(IntPtr space, out int minlevel, out int maxlevel);

        [DllImport("ode", EntryPoint = "dHashSpaceSetLevels"), SuppressUnmanagedCodeSecurity]
        internal static extern void HashSpaceSetLevels(IntPtr space, int minlevel, int maxlevel);

        [DllImport("ode", EntryPoint = "dInfiniteAABB"), SuppressUnmanagedCodeSecurity]
        internal static extern void InfiniteAABB(IntPtr geom, out AABB aabb);

        [DllImport("ode", EntryPoint = "dInitODE"), SuppressUnmanagedCodeSecurity]
        internal static extern void InitODE();

        [DllImport("ode", EntryPoint = "dInitODE2"), SuppressUnmanagedCodeSecurity]
        internal static extern int InitODE2(uint ODEInitFlags);

        [DllImport("ode", EntryPoint = "dIsPositiveDefinite"), SuppressUnmanagedCodeSecurity]
        internal static extern int IsPositiveDefinite(ref Single A, int n);

        [DllImport("ode", EntryPoint = "dInvertPDMatrix"), SuppressUnmanagedCodeSecurity]
        internal static extern int InvertPDMatrix(ref Single A, out Single Ainv, int n);

        [DllImport("ode", EntryPoint = "dJointAddAMotorTorques"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointAddAMotorTorques(IntPtr joint, Single torque1, Single torque2, Single torque3);

        [DllImport("ode", EntryPoint = "dJointAddHingeTorque"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointAddHingeTorque(IntPtr joint, Single torque);

        [DllImport("ode", EntryPoint = "dJointAddHinge2Torque"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointAddHinge2Torques(IntPtr joint, Single torque1, Single torque2);

        [DllImport("ode", EntryPoint = "dJointAddPRTorque"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointAddPRTorque(IntPtr joint, Single torque);

        [DllImport("ode", EntryPoint = "dJointAddUniversalTorque"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointAddUniversalTorques(IntPtr joint, Single torque1, Single torque2);

        [DllImport("ode", EntryPoint = "dJointAddSliderForce"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointAddSliderForce(IntPtr joint, Single force);

        [DllImport("ode", EntryPoint = "dJointAttach"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointAttach(IntPtr joint, IntPtr body1, IntPtr body2);

        [DllImport("ode", EntryPoint = "dJointCreateAMotor"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreateAMotor(IntPtr world, IntPtr group);

        [DllImport("ode", EntryPoint = "dJointCreateBall"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreateBall(IntPtr world, IntPtr group);

        [DllImport("ode", EntryPoint = "dJointCreateContact"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreateContact(IntPtr world, IntPtr group, ref Contact contact);

        [DllImport("ode", EntryPoint = "dJointCreateContact"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreateContactPtr(IntPtr world, IntPtr group, IntPtr contact);

        [DllImport("ode", EntryPoint = "dJointCreateFixed"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreateFixed(IntPtr world, IntPtr group);

        [DllImport("ode", EntryPoint = "dJointCreateHinge"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreateHinge(IntPtr world, IntPtr group);

        [DllImport("ode", EntryPoint = "dJointCreateHinge2"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreateHinge2(IntPtr world, IntPtr group);

        [DllImport("ode", EntryPoint = "dJointCreateLMotor"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreateLMotor(IntPtr world, IntPtr group);

        [DllImport("ode", EntryPoint = "dJointCreateNull"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreateNull(IntPtr world, IntPtr group);

        [DllImport("ode", EntryPoint = "dJointCreatePR"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreatePR(IntPtr world, IntPtr group);

        [DllImport("ode", EntryPoint = "dJointCreatePlane2D"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreatePlane2D(IntPtr world, IntPtr group);

        [DllImport("ode", EntryPoint = "dJointCreateSlider"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreateSlider(IntPtr world, IntPtr group);

        [DllImport("ode", EntryPoint = "dJointCreateUniversal"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointCreateUniversal(IntPtr world, IntPtr group);

        [DllImport("ode", EntryPoint = "dJointDestroy"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointDestroy(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetAMotorAngle"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetAMotorAngle(IntPtr j, int anum);

        [DllImport("ode", EntryPoint = "dJointGetAMotorAngleRate"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetAMotorAngleRate(IntPtr j, int anum);

        [DllImport("ode", EntryPoint = "dJointGetAMotorAxis"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetAMotorAxis(IntPtr j, int anum, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetAMotorAxisRel"), SuppressUnmanagedCodeSecurity]
        internal static extern int JointGetAMotorAxisRel(IntPtr j, int anum);

        [DllImport("ode", EntryPoint = "dJointGetAMotorMode"), SuppressUnmanagedCodeSecurity]
        internal static extern int JointGetAMotorMode(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetAMotorNumAxes"), SuppressUnmanagedCodeSecurity]
        internal static extern int JointGetAMotorNumAxes(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetAMotorParam"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetAMotorParam(IntPtr j, int parameter);

        [DllImport("ode", EntryPoint = "dJointGetBallAnchor"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetBallAnchor(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetBallAnchor2"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetBallAnchor2(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetBody"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointGetBody(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetData"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointGetData(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetFeedback"), SuppressUnmanagedCodeSecurity]
        internal static extern unsafe JointFeedback* JointGetFeedbackUnsafe(IntPtr j);

        internal static JointFeedback JointGetFeedback(IntPtr j)
        {
            unsafe
            {
                return *(JointGetFeedbackUnsafe(j));
            }
        }

        [DllImport("ode", EntryPoint = "dJointGetHingeAnchor"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetHingeAnchor(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetHingeAngle"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetHingeAngle(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetHingeAngleRate"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetHingeAngleRate(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetHingeAxis"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetHingeAxis(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetHingeParam"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetHingeParam(IntPtr j, int parameter);

        [DllImport("ode", EntryPoint = "dJointGetHinge2Angle1"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetHinge2Angle1(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetHinge2Angle1Rate"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetHinge2Angle1Rate(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetHinge2Angle2Rate"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetHinge2Angle2Rate(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetHingeAnchor2"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetHingeAnchor2(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetHinge2Anchor"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetHinge2Anchor(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetHinge2Anchor2"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetHinge2Anchor2(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetHinge2Axis1"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetHinge2Axis1(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetHinge2Axis2"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetHinge2Axis2(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetHinge2Param"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetHinge2Param(IntPtr j, int parameter);

        [DllImport("ode", EntryPoint = "dJointGetLMotorAxis"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetLMotorAxis(IntPtr j, int anum, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetLMotorNumAxes"), SuppressUnmanagedCodeSecurity]
        internal static extern int JointGetLMotorNumAxes(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetLMotorParam"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetLMotorParam(IntPtr j, int parameter);

        [DllImport("ode", EntryPoint = "dJointGetPRAnchor"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetPRAnchor(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetPRAxis1"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetPRAxis1(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetPRAxis2"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetPRAxis2(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetPRParam"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetPRParam(IntPtr j, int parameter);

        [DllImport("ode", EntryPoint = "dJointGetPRPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetPRPosition(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetPRPositionRate"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetPRPositionRate(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetSliderAxis"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetSliderAxis(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetSliderParam"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetSliderParam(IntPtr j, int parameter);

        [DllImport("ode", EntryPoint = "dJointGetSliderPosition"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetSliderPosition(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetSliderPositionRate"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetSliderPositionRate(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetType"), SuppressUnmanagedCodeSecurity]
        internal static extern JointType JointGetType(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetUniversalAnchor"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetUniversalAnchor(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetUniversalAnchor2"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetUniversalAnchor2(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetUniversalAngle1"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetUniversalAngle1(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetUniversalAngle1Rate"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetUniversalAngle1Rate(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetUniversalAngle2"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetUniversalAngle2(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetUniversalAngle2Rate"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetUniversalAngle2Rate(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointGetUniversalAngles"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetUniversalAngles(IntPtr j, out Single angle1, out Single angle2);

        [DllImport("ode", EntryPoint = "dJointGetUniversalAxis1"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetUniversalAxis1(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetUniversalAxis2"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGetUniversalAxis2(IntPtr j, out Vector3 result);

        [DllImport("ode", EntryPoint = "dJointGetUniversalParam"), SuppressUnmanagedCodeSecurity]
        internal static extern Single JointGetUniversalParam(IntPtr j, int parameter);

        [DllImport("ode", EntryPoint = "dJointGroupCreate"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr JointGroupCreate(int max_size);

        [DllImport("ode", EntryPoint = "dJointGroupDestroy"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGroupDestroy(IntPtr group);

        [DllImport("ode", EntryPoint = "dJointGroupEmpty"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointGroupEmpty(IntPtr group);

        [DllImport("ode", EntryPoint = "dJointSetAMotorAngle"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetAMotorAngle(IntPtr j, int anum, Single angle);

        [DllImport("ode", EntryPoint = "dJointSetAMotorAxis"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetAMotorAxis(IntPtr j, int anum, int rel, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetAMotorMode"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetAMotorMode(IntPtr j, int mode);

        [DllImport("ode", EntryPoint = "dJointSetAMotorNumAxes"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetAMotorNumAxes(IntPtr group, int num);

        [DllImport("ode", EntryPoint = "dJointSetAMotorParam"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetAMotorParam(IntPtr group, int parameter, Single value);

        [DllImport("ode", EntryPoint = "dJointSetBallAnchor"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetBallAnchor(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetBallAnchor2"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetBallAnchor2(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetData"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetData(IntPtr j, IntPtr data);

        [DllImport("ode", EntryPoint = "dJointSetFeedback"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetFeedback(IntPtr j, out JointFeedback feedback);

        [DllImport("ode", EntryPoint = "dJointSetFixed"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetFixed(IntPtr j);

        [DllImport("ode", EntryPoint = "dJointSetHingeAnchor"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetHingeAnchor(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetHingeAnchorDelta"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetHingeAnchorDelta(IntPtr j, Single x, Single y, Single z, Single ax, Single ay,
                                                           Single az);

        [DllImport("ode", EntryPoint = "dJointSetHingeAxis"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetHingeAxis(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetHingeParam"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetHingeParam(IntPtr j, int parameter, Single value);

        [DllImport("ode", EntryPoint = "dJointSetHinge2Anchor"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetHinge2Anchor(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetHinge2Axis1"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetHinge2Axis1(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetHinge2Axis2"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetHinge2Axis2(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetHinge2Param"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetHinge2Param(IntPtr j, int parameter, Single value);

        [DllImport("ode", EntryPoint = "dJointSetLMotorAxis"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetLMotorAxis(IntPtr j, int anum, int rel, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetLMotorNumAxes"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetLMotorNumAxes(IntPtr j, int num);

        [DllImport("ode", EntryPoint = "dJointSetLMotorParam"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetLMotorParam(IntPtr j, int parameter, Single value);

        [DllImport("ode", EntryPoint = "dJointSetPlane2DAngleParam"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetPlane2DAngleParam(IntPtr j, int parameter, Single value);

        [DllImport("ode", EntryPoint = "dJointSetPlane2DXParam"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetPlane2DXParam(IntPtr j, int parameter, Single value);

        [DllImport("ode", EntryPoint = "dJointSetPlane2DYParam"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetPlane2DYParam(IntPtr j, int parameter, Single value);

        [DllImport("ode", EntryPoint = "dJointSetPRAnchor"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetPRAnchor(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetPRAxis1"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetPRAxis1(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetPRAxis2"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetPRAxis2(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetPRParam"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetPRParam(IntPtr j, int parameter, Single value);

        [DllImport("ode", EntryPoint = "dJointSetSliderAxis"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetSliderAxis(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetSliderAxisDelta"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetSliderAxisDelta(IntPtr j, Single x, Single y, Single z, Single ax, Single ay,
                                                          Single az);

        [DllImport("ode", EntryPoint = "dJointSetSliderParam"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetSliderParam(IntPtr j, int parameter, Single value);

        [DllImport("ode", EntryPoint = "dJointSetUniversalAnchor"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetUniversalAnchor(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetUniversalAxis1"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetUniversalAxis1(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetUniversalAxis2"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetUniversalAxis2(IntPtr j, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dJointSetUniversalParam"), SuppressUnmanagedCodeSecurity]
        internal static extern void JointSetUniversalParam(IntPtr j, int parameter, Single value);

        [DllImport("ode", EntryPoint = "dLDLTAddTL"), SuppressUnmanagedCodeSecurity]
        internal static extern void LDLTAddTL(ref Single L, ref Single d, ref Single a, int n, int nskip);

        [DllImport("ode", EntryPoint = "dMassAdd"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassAdd(ref Mass a, ref Mass b);

        [DllImport("ode", EntryPoint = "dMassAdjust"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassAdjust(ref Mass m, Single newmass);

        [DllImport("ode", EntryPoint = "dMassCheck"), SuppressUnmanagedCodeSecurity]
        internal static extern bool MassCheck(ref Mass m);

        [DllImport("ode", EntryPoint = "dMassRotate"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassRotate(ref Mass mass, ref Matrix3 R);

        [DllImport("ode", EntryPoint = "dMassRotate"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassRotate(ref Mass mass, ref Single M00);

        [DllImport("ode", EntryPoint = "dMassSetBox"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassSetBox(out Mass mass, Single density, Single lx, Single ly, Single lz);

        [DllImport("ode", EntryPoint = "dMassSetBoxTotal"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassSetBoxTotal(out Mass mass, Single total_mass, Single lx, Single ly, Single lz);

        [DllImport("ode", EntryPoint = "dMassSetCapsule"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassSetCapsule(out Mass mass, Single density, int direction, Single radius, Single length);

        [DllImport("ode", EntryPoint = "dMassSetCapsuleTotal"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassSetCapsuleTotal(out Mass mass, Single total_mass, int direction, Single radius,
                                                      Single length);

        [DllImport("ode", EntryPoint = "dMassSetCylinder"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassSetCylinder(out Mass mass, Single density, int direction, Single radius,
                                                  Single length);

        [DllImport("ode", EntryPoint = "dMassSetCylinderTotal"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassSetCylinderTotal(out Mass mass, Single total_mass, int direction, Single radius,
                                                       Single length);

        [DllImport("ode", EntryPoint = "dMassSetParameters"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassSetParameters(out Mass mass, Single themass,
                                                    Single cgx, Single cgy, Single cgz,
                                                    Single i11, Single i22, Single i33,
                                                    Single i12, Single i13, Single i23);

        [DllImport("ode", EntryPoint = "dMassSetSphere"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassSetSphere(out Mass mass, Single density, Single radius);

        [DllImport("ode", EntryPoint = "dMassSetSphereTotal"), SuppressUnmanagedCodeSecurity]
        internal static extern void dMassSetSphereTotal(out Mass mass, Single total_mass, Single radius);

        [DllImport("ode", EntryPoint = "dMassSetTrimesh"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassSetTrimesh(out Mass mass, Single density, IntPtr g);

        [DllImport("ode", EntryPoint = "dMassSetZero"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassSetZero(out Mass mass);

        [DllImport("ode", EntryPoint = "dMassTranslate"), SuppressUnmanagedCodeSecurity]
        internal static extern void MassTranslate(ref Mass mass, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dMultiply0"), SuppressUnmanagedCodeSecurity]
        internal static extern void Multiply0(out Single A00, ref Single B00, ref Single C00, int p, int q, int r);

        [DllImport("ode", EntryPoint = "dMultiply1"), SuppressUnmanagedCodeSecurity]
        internal static extern void Multiply1(out Single A00, ref Single B00, ref Single C00, int p, int q, int r);

        [DllImport("ode", EntryPoint = "dMultiply2"), SuppressUnmanagedCodeSecurity]
        internal static extern void Multiply2(out Single A00, ref Single B00, ref Single C00, int p, int q, int r);

        [DllImport("ode", EntryPoint = "dQFromAxisAndAngle"), SuppressUnmanagedCodeSecurity]
        internal static extern void QFromAxisAndAngle(out Quaternion q, Single ax, Single ay, Single az, Single angle);

        [DllImport("ode", EntryPoint = "dQfromR"), SuppressUnmanagedCodeSecurity]
        internal static extern void QfromR(out Quaternion q, ref Matrix3 R);

        [DllImport("ode", EntryPoint = "dQMultiply0"), SuppressUnmanagedCodeSecurity]
        internal static extern void QMultiply0(out Quaternion qa, ref Quaternion qb, ref Quaternion qc);

        [DllImport("ode", EntryPoint = "dQMultiply1"), SuppressUnmanagedCodeSecurity]
        internal static extern void QMultiply1(out Quaternion qa, ref Quaternion qb, ref Quaternion qc);

        [DllImport("ode", EntryPoint = "dQMultiply2"), SuppressUnmanagedCodeSecurity]
        internal static extern void QMultiply2(out Quaternion qa, ref Quaternion qb, ref Quaternion qc);

        [DllImport("ode", EntryPoint = "dQMultiply3"), SuppressUnmanagedCodeSecurity]
        internal static extern void QMultiply3(out Quaternion qa, ref Quaternion qb, ref Quaternion qc);

        [DllImport("ode", EntryPoint = "dQSetIdentity"), SuppressUnmanagedCodeSecurity]
        internal static extern void QSetIdentity(out Quaternion q);

        [DllImport("ode", EntryPoint = "dQuadTreeSpaceCreate"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr QuadTreeSpaceCreate(IntPtr space, ref Vector3 center, ref Vector3 extents, int depth);

        [DllImport("ode", EntryPoint = "dQuadTreeSpaceCreate"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr QuadTreeSpaceCreate(IntPtr space, ref Single centerX, ref Single extentsX, int depth);

        [DllImport("ode", EntryPoint = "dRandReal"), SuppressUnmanagedCodeSecurity]
        internal static extern Single RandReal();

        [DllImport("ode", EntryPoint = "dRFrom2Axes"), SuppressUnmanagedCodeSecurity]
        internal static extern void RFrom2Axes(out Matrix3 R, Single ax, Single ay, Single az, Single bx, Single by, Single bz);

        [DllImport("ode", EntryPoint = "dRFromAxisAndAngle"), SuppressUnmanagedCodeSecurity]
        internal static extern void RFromAxisAndAngle(out Matrix3 R, Single x, Single y, Single z, Single angle);

        [DllImport("ode", EntryPoint = "dRFromEulerAngles"), SuppressUnmanagedCodeSecurity]
        internal static extern void RFromEulerAngles(out Matrix3 R, Single phi, Single theta, Single psi);

        [DllImport("ode", EntryPoint = "dRfromQ"), SuppressUnmanagedCodeSecurity]
        internal static extern void RfromQ(out Matrix3 R, ref Quaternion q);

        [DllImport("ode", EntryPoint = "dRFromZAxis"), SuppressUnmanagedCodeSecurity]
        internal static extern void RFromZAxis(out Matrix3 R, Single ax, Single ay, Single az);

        [DllImport("ode", EntryPoint = "dRSetIdentity"), SuppressUnmanagedCodeSecurity]
        internal static extern void RSetIdentity(out Matrix3 R);

        [DllImport("ode", EntryPoint = "dSetValue"), SuppressUnmanagedCodeSecurity]
        internal static extern void SetValue(out Single a, int n);

        [DllImport("ode", EntryPoint = "dSetZero"), SuppressUnmanagedCodeSecurity]
        internal static extern void SetZero(out Single a, int n);

        [DllImport("ode", EntryPoint = "dSimpleSpaceCreate"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr SimpleSpaceCreate(IntPtr space);

        [DllImport("ode", EntryPoint = "dSolveCholesky"), SuppressUnmanagedCodeSecurity]
        internal static extern void SolveCholesky(ref Single L, out Single b, int n);

        [DllImport("ode", EntryPoint = "dSolveL1"), SuppressUnmanagedCodeSecurity]
        internal static extern void SolveL1(ref Single L, out Single b, int n, int nskip);

        [DllImport("ode", EntryPoint = "dSolveL1T"), SuppressUnmanagedCodeSecurity]
        internal static extern void SolveL1T(ref Single L, out Single b, int n, int nskip);

        [DllImport("ode", EntryPoint = "dSolveLDLT"), SuppressUnmanagedCodeSecurity]
        internal static extern void SolveLDLT(ref Single L, ref Single d, out Single b, int n, int nskip);

        [DllImport("ode", EntryPoint = "dSpaceAdd"), SuppressUnmanagedCodeSecurity]
        internal static extern void SpaceAdd(IntPtr space, IntPtr geom);

        [DllImport("ode", EntryPoint = "dSpaceLockQuery"), SuppressUnmanagedCodeSecurity]
        internal static extern bool SpaceLockQuery(IntPtr space);

        [DllImport("ode", EntryPoint = "dSpaceClean"), SuppressUnmanagedCodeSecurity]
        internal static extern void SpaceClean(IntPtr space);

        [DllImport("ode", EntryPoint = "dSpaceCollide"), SuppressUnmanagedCodeSecurity]
        internal static extern void SpaceCollide(IntPtr space, IntPtr data, NearCallback callback);

        [DllImport("ode", EntryPoint = "dSpaceCollide2"), SuppressUnmanagedCodeSecurity]
        internal static extern void SpaceCollide2(IntPtr space1, IntPtr space2, IntPtr data, NearCallback callback);

        [DllImport("ode", EntryPoint = "dSpaceDestroy"), SuppressUnmanagedCodeSecurity]
        internal static extern void SpaceDestroy(IntPtr space);

        [DllImport("ode", EntryPoint = "dSpaceGetCleanup"), SuppressUnmanagedCodeSecurity]
        internal static extern bool SpaceGetCleanup(IntPtr space);

        [DllImport("ode", EntryPoint = "dSpaceGetNumGeoms"), SuppressUnmanagedCodeSecurity]
        internal static extern int SpaceGetNumGeoms(IntPtr space);

        [DllImport("ode", EntryPoint = "dSpaceGetGeom"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr SpaceGetGeom(IntPtr space, int i);

        [DllImport("ode", EntryPoint = "dSpaceGetSublevel"), SuppressUnmanagedCodeSecurity]
        internal static extern int SpaceGetSublevel(IntPtr space);

        [DllImport("ode", EntryPoint = "dSpaceQuery"), SuppressUnmanagedCodeSecurity]
        internal static extern bool SpaceQuery(IntPtr space, IntPtr geom);

        [DllImport("ode", EntryPoint = "dSpaceRemove"), SuppressUnmanagedCodeSecurity]
        internal static extern void SpaceRemove(IntPtr space, IntPtr geom);

        [DllImport("ode", EntryPoint = "dSpaceSetCleanup"), SuppressUnmanagedCodeSecurity]
        internal static extern void SpaceSetCleanup(IntPtr space, bool mode);

        [DllImport("ode", EntryPoint = "dSpaceSetSublevel"), SuppressUnmanagedCodeSecurity]
        internal static extern void SpaceSetSublevel(IntPtr space, int sublevel);

        [DllImport("ode", EntryPoint = "dSweepAndPruneSpaceCreate"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr SweepAndPruneSpaceCreate(IntPtr space, int AxisOrder);

        [DllImport("ode", EntryPoint = "dVectorScale"), SuppressUnmanagedCodeSecurity]
        internal static extern void VectorScale(out Single a, ref Single d, int n);

        [DllImport("ode", EntryPoint = "dWorldCreate"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr WorldCreate();

        [DllImport("ode", EntryPoint = "dWorldDestroy"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldDestroy(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetAutoDisableAverageSamplesCount"), SuppressUnmanagedCodeSecurity]
        internal static extern int WorldGetAutoDisableAverageSamplesCount(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetAutoDisableAngularThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetAutoDisableAngularThreshold(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetAutoDisableFlag"), SuppressUnmanagedCodeSecurity]
        internal static extern bool WorldGetAutoDisableFlag(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetAutoDisableLinearThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetAutoDisableLinearThreshold(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetAutoDisableSteps"), SuppressUnmanagedCodeSecurity]
        internal static extern int WorldGetAutoDisableSteps(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetAutoDisableTime"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetAutoDisableTime(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetAutoEnableDepthSF1"), SuppressUnmanagedCodeSecurity]
        internal static extern int WorldGetAutoEnableDepthSF1(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetCFM"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetCFM(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetERP"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetERP(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetGravity"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldGetGravity(IntPtr world, out Vector3 gravity);

        [DllImport("ode", EntryPoint = "dWorldGetGravity"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldGetGravity(IntPtr world, out Single X);

        [DllImport("ode", EntryPoint = "dWorldGetContactMaxCorrectingVel"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetContactMaxCorrectingVel(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetContactSurfaceLayer"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetContactSurfaceLayer(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetAngularDamping"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetAngularDamping(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetAngularDampingThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetAngularDampingThreshold(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetLinearDamping"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetLinearDamping(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetLinearDampingThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetLinearDampingThreshold(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetQuickStepNumIterations"), SuppressUnmanagedCodeSecurity]
        internal static extern int WorldGetQuickStepNumIterations(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetQuickStepW"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetQuickStepW(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldGetMaxAngularSpeed"), SuppressUnmanagedCodeSecurity]
        internal static extern Single WorldGetMaxAngularSpeed(IntPtr world);

        [DllImport("ode", EntryPoint = "dWorldImpulseToForce"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldImpulseToForce(IntPtr world, Single stepsize, Single ix, Single iy, Single iz,
                                                      out Vector3 force);

        [DllImport("ode", EntryPoint = "dWorldImpulseToForce"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldImpulseToForce(IntPtr world, Single stepsize, Single ix, Single iy, Single iz,
                                                      out Single forceX);

        [DllImport("ode", EntryPoint = "dWorldQuickStep"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldQuickStep(IntPtr world, Single stepsize);

        [DllImport("ode", EntryPoint = "dWorldSetAngularDamping"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetAngularDamping(IntPtr world, Single scale);

        [DllImport("ode", EntryPoint = "dWorldSetAngularDampingThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetAngularDampingThreshold(IntPtr world, Single threshold);

        [DllImport("ode", EntryPoint = "dWorldSetAutoDisableAngularThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetAutoDisableAngularThreshold(IntPtr world, Single angular_threshold);

        [DllImport("ode", EntryPoint = "dWorldSetAutoDisableAverageSamplesCount"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetAutoDisableAverageSamplesCount(IntPtr world, int average_samples_count);

        [DllImport("ode", EntryPoint = "dWorldSetAutoDisableFlag"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetAutoDisableFlag(IntPtr world, bool do_auto_disable);

        [DllImport("ode", EntryPoint = "dWorldSetAutoDisableLinearThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetAutoDisableLinearThreshold(IntPtr world, Single linear_threshold);

        [DllImport("ode", EntryPoint = "dWorldSetAutoDisableSteps"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetAutoDisableSteps(IntPtr world, int steps);

        [DllImport("ode", EntryPoint = "dWorldSetAutoDisableTime"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetAutoDisableTime(IntPtr world, Single time);

        [DllImport("ode", EntryPoint = "dWorldSetAutoEnableDepthSF1"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetAutoEnableDepthSF1(IntPtr world, int autoEnableDepth);

        [DllImport("ode", EntryPoint = "dWorldSetCFM"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetCFM(IntPtr world, Single cfm);

        [DllImport("ode", EntryPoint = "dWorldSetContactMaxCorrectingVel"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetContactMaxCorrectingVel(IntPtr world, Single vel);

        [DllImport("ode", EntryPoint = "dWorldSetContactSurfaceLayer"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetContactSurfaceLayer(IntPtr world, Single depth);

        [DllImport("ode", EntryPoint = "dWorldSetDamping"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetDamping(IntPtr world, Single linear_scale, Single angular_scale);

        [DllImport("ode", EntryPoint = "dWorldSetERP"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetERP(IntPtr world, Single erp);

        [DllImport("ode", EntryPoint = "dWorldSetGravity"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetGravity(IntPtr world, Single x, Single y, Single z);

        [DllImport("ode", EntryPoint = "dWorldSetLinearDamping"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetLinearDamping(IntPtr world, Single scale);

        [DllImport("ode", EntryPoint = "dWorldSetLinearDampingThreshold"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetLinearDampingThreshold(IntPtr world, Single threshold);

        [DllImport("ode", EntryPoint = "dWorldSetQuickStepNumIterations"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetQuickStepNumIterations(IntPtr world, int num);

        [DllImport("ode", EntryPoint = "dWorldSetQuickStepW"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetQuickStepW(IntPtr world, Single over_relaxation);

        [DllImport("ode", EntryPoint = "dWorldSetMaxAngularSpeed"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldSetMaxAngularSpeed(IntPtr world, Single max_speed);

        [DllImport("ode", EntryPoint = "dWorldStep"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldStep(IntPtr world, Single stepsize);

        [DllImport("ode", EntryPoint = "dWorldStepFast1"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldStepFast1(IntPtr world, Single stepsize, int maxiterations);

        [DllImport("ode", EntryPoint = "dWorldExportDIF"), SuppressUnmanagedCodeSecurity]
        internal static extern void WorldExportDIF(IntPtr world, string filename, bool append, string prefix);

        #endregion

        #region Setup

        static d()
        {
            try
            {
                if (System.IO.File.Exists("ode.dll"))
                    System.IO.File.Delete("ode.dll");
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Format(Level.Off, "[ODE]: Failed to copy ODE dll file, may have issues with physics! (Can be caused by running multiple instances in the same bin, if so, ignore this warning) " +
                    ex.ToString());
            }
            try
            {
                if (!System.IO.File.Exists("ode.dll"))
                {
                    string fileName = System.IntPtr.Size == 4 ? "odex86.dll" : "odex64.dll";
                    System.IO.File.Copy(fileName, "ode.dll");
                }
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Format(Level.Off, "[ODE]: Failed to copy ODE dll file, may have issues with physics! (Can be caused by running multiple instances in the same bin, if so, ignore this warning) " +
                    ex.ToString());
            }
        }

        #endregion
    }
}