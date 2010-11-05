using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Ode.NET
{
#if dDOUBLE
	using dReal = System.Double;
#else
	using dReal = System.Single;
#endif

	public static class d
	{
		public static dReal Infinity = dReal.MaxValue;

		#region Flags and Enumerations

#if !dNO_UNSAFE_CODE
        [CLSCompliant(false)]
        [Flags]
        public enum AllocateODEDataFlags : uint
        {
            BasicData = 0,
            CollisionData = 0x00000001,
            All = ~0u
        }

        [CLSCompliant(false)]
        [Flags]
        public enum IniteODEFlags : uint
        {
            dInitFlagManualThreadCleanup = 0x00000001
        }
#endif

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
            XYZ = ((0)|(1<<2)|(2<<4)),
            XZY = ((0)|(2<<2)|(1<<4)),
            YXZ = ((1)|(0<<2)|(2<<4)),
            YZX = ((1)|(2<<2)|(0<<4)),
            ZXY = ((2)|(0<<2)|(1<<4)),
            ZYX = ((2)|(1<<2)|(0<<4))
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
		public delegate dReal HeightfieldGetHeight(IntPtr p_user_data, int x, int z);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NearCallback(IntPtr data, IntPtr geom1, IntPtr geom2);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int TriCallback(IntPtr trimesh, IntPtr refObject, int triangleIndex);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int TriArrayCallback(IntPtr trimesh, IntPtr refObject, int[] triangleIndex, int triCount);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int TriRayCallback(IntPtr trimesh, IntPtr ray, int triangleIndex, dReal u, dReal v);

		#endregion

		#region Structs

		[StructLayout(LayoutKind.Sequential)]
		public struct AABB
		{
			public dReal MinX, MaxX;
			public dReal MinY, MaxY;
			public dReal MinZ, MaxZ;
		}


		[StructLayout(LayoutKind.Sequential)]
		public struct Contact
		{
			public SurfaceParameters surface;
			public ContactGeom geom;
			public Vector3 fdir1;
		}


		[StructLayout(LayoutKind.Sequential)]
		public struct ContactGeom
		{
			public static readonly int SizeOf = Marshal.SizeOf(typeof(ContactGeom));

			public Vector3 pos;
			public Vector3 normal;
			public dReal depth;
			public IntPtr g1;
			public IntPtr g2;
			public int side1;
			public int side2;
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
			public dReal mass;
			public Vector4 c;
			public Matrix3 I;
		}


		[StructLayout(LayoutKind.Sequential)]
		public struct Matrix3
		{
			public Matrix3(dReal m00, dReal m10, dReal m20, dReal m01, dReal m11, dReal m21, dReal m02, dReal m12, dReal m22)
			{
				M00 = m00;  M10 = m10;  M20 = m20;  _m30 = 0.0f;
				M01 = m01;  M11 = m11;  M21 = m21;  _m31 = 0.0f;
				M02 = m02;  M12 = m12;  M22 = m22;  _m32 = 0.0f;
			}
			public dReal M00, M10, M20;
			private dReal _m30;
			public dReal M01, M11, M21;
			private dReal _m31;
			public dReal M02, M12, M22;
			private dReal _m32;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Matrix4
		{
			public Matrix4(dReal m00, dReal m10, dReal m20, dReal m30,
				dReal m01, dReal m11, dReal m21, dReal m31,
				dReal m02, dReal m12, dReal m22, dReal m32,
				dReal m03, dReal m13, dReal m23, dReal m33)
			{
				M00 = m00; M10 = m10; M20 = m20; M30 = m30;
				M01 = m01; M11 = m11; M21 = m21; M31 = m31;
				M02 = m02; M12 = m12; M22 = m22; M32 = m32;
				M03 = m03; M13 = m13; M23 = m23; M33 = m33;
			}
			public dReal M00, M10, M20, M30;
			public dReal M01, M11, M21, M31;
			public dReal M02, M12, M22, M32;
			public dReal M03, M13, M23, M33;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Quaternion
		{
			public dReal W, X, Y, Z;
		}


		[StructLayout(LayoutKind.Sequential)]
		public struct SurfaceParameters
		{
			public ContactFlags mode;
			public dReal mu;
			public dReal mu2;
			public dReal bounce;
			public dReal bounce_vel;
			public dReal soft_erp;
			public dReal soft_cfm;
			public dReal motion1;
			public dReal motion2;
            public dReal motionN;
			public dReal slip1;
			public dReal slip2;
		}


		[StructLayout(LayoutKind.Sequential)]
		public struct Vector3
		{
			public Vector3(dReal x, dReal y, dReal z)
			{
				X = x;  Y = y;  Z = z;  _w = 0.0f;
			}
			public dReal X, Y, Z;
			private dReal _w;
		}


		[StructLayout(LayoutKind.Sequential)]
		public struct Vector4
		{
			public Vector4(dReal x, dReal y, dReal z, dReal w)
			{
				X = x;  Y = y;  Z = z;  W = w;
			}
			public dReal X, Y, Z, W;
		}

		#endregion

#if !dNO_UNSAFE_CODE
        [CLSCompliant(false)]
        [DllImport("odesingle", EntryPoint = "dAllocateODEDataForThread"), SuppressUnmanagedCodeSecurity]
        public static extern int AllocateODEDataForThread(uint ODEInitFlags);
#endif

        [DllImport("odesingle", EntryPoint = "dAreConnected"), SuppressUnmanagedCodeSecurity]
		public static extern bool AreConnected(IntPtr b1, IntPtr b2);

		[DllImport("odesingle", EntryPoint = "dAreConnectedExcluding"), SuppressUnmanagedCodeSecurity]
		public static extern bool AreConnectedExcluding(IntPtr b1, IntPtr b2, JointType joint_type);

		[DllImport("odesingle", EntryPoint = "dBodyAddForce"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyAddForce(IntPtr body, dReal fx, dReal fy, dReal fz);

		[DllImport("odesingle", EntryPoint = "dBodyAddForceAtPos"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyAddForceAtPos(IntPtr body, dReal fx, dReal fy, dReal fz, dReal px, dReal py, dReal pz);

		[DllImport("odesingle", EntryPoint = "dBodyAddForceAtRelPos"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyAddForceAtRelPos(IntPtr body, dReal fx, dReal fy, dReal fz, dReal px, dReal py, dReal pz);

		[DllImport("odesingle", EntryPoint = "dBodyAddRelForce"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyAddRelForce(IntPtr body, dReal fx, dReal fy, dReal fz);

		[DllImport("odesingle", EntryPoint = "dBodyAddRelForceAtPos"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyAddRelForceAtPos(IntPtr body, dReal fx, dReal fy, dReal fz, dReal px, dReal py, dReal pz);

		[DllImport("odesingle", EntryPoint = "dBodyAddRelForceAtRelPos"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyAddRelForceAtRelPos(IntPtr body, dReal fx, dReal fy, dReal fz, dReal px, dReal py, dReal pz);

		[DllImport("odesingle", EntryPoint = "dBodyAddRelTorque"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyAddRelTorque(IntPtr body, dReal fx, dReal fy, dReal fz);

		[DllImport("odesingle", EntryPoint = "dBodyAddTorque"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyAddTorque(IntPtr body, dReal fx, dReal fy, dReal fz);

		[DllImport("odesingle", EntryPoint = "dBodyCopyPosition"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyCopyPosition(IntPtr body, out Vector3 pos);

		[DllImport("odesingle", EntryPoint = "dBodyCopyPosition"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyCopyPosition(IntPtr body, out dReal X);

		[DllImport("odesingle", EntryPoint = "dBodyCopyQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyCopyQuaternion(IntPtr body, out Quaternion quat);

		[DllImport("odesingle", EntryPoint = "dBodyCopyQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyCopyQuaternion(IntPtr body, out dReal X);

		[DllImport("odesingle", EntryPoint = "dBodyCopyRotation"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyCopyRotation(IntPtr body, out Matrix3 R);

		[DllImport("odesingle", EntryPoint = "dBodyCopyRotation"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyCopyRotation(IntPtr body, out dReal M00);

		[DllImport("odesingle", EntryPoint = "dBodyCreate"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr BodyCreate(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dBodyDestroy"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyDestroy(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodyDisable"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyDisable(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodyEnable"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyEnable(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodyGetAutoDisableAngularThreshold"), SuppressUnmanagedCodeSecurity]
		public static extern dReal BodyGetAutoDisableAngularThreshold(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodyGetAutoDisableFlag"), SuppressUnmanagedCodeSecurity]
		public static extern bool BodyGetAutoDisableFlag(IntPtr body);

        [DllImport("odesingle", EntryPoint = "dBodyGetAutoDisableDefaults"), SuppressUnmanagedCodeSecurity]
        public static extern void BodyGetAutoDisableDefaults(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodyGetAutoDisableLinearThreshold"), SuppressUnmanagedCodeSecurity]
		public static extern dReal BodyGetAutoDisableLinearThreshold(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodyGetAutoDisableSteps"), SuppressUnmanagedCodeSecurity]
		public static extern int BodyGetAutoDisableSteps(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodyGetAutoDisableTime"), SuppressUnmanagedCodeSecurity]
		public static extern dReal BodyGetAutoDisableTime(IntPtr body);

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dBodyGetAngularVel"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static Vector3* BodyGetAngularVelUnsafe(IntPtr body);
		public static Vector3 BodyGetAngularVel(IntPtr body)
		{
			unsafe { return *(BodyGetAngularVelUnsafe(body)); }
		}
#endif

		[DllImport("odesingle", EntryPoint = "dBodyGetData"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr BodyGetData(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodyGetFiniteRotationMode"), SuppressUnmanagedCodeSecurity]
		public static extern int BodyGetFiniteRotationMode(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodyGetFiniteRotationAxis"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyGetFiniteRotationAxis(IntPtr body, out Vector3 result);

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dBodyGetForce"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static Vector3* BodyGetForceUnsafe(IntPtr body);
		public static Vector3 BodyGetForce(IntPtr body)
		{
			unsafe { return *(BodyGetForceUnsafe(body)); }
		}
#endif

		[DllImport("odesingle", EntryPoint = "dBodyGetGravityMode"), SuppressUnmanagedCodeSecurity]
		public static extern bool BodyGetGravityMode(IntPtr body);

        [DllImport("odesingle", EntryPoint = "dBodyGetGyroscopicMode"), SuppressUnmanagedCodeSecurity]
        public static extern int BodyGetGyroscopicMode(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodyGetJoint"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr BodyGetJoint(IntPtr body, int index);

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dBodyGetLinearVel"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static Vector3* BodyGetLinearVelUnsafe(IntPtr body);
		public static Vector3 BodyGetLinearVel(IntPtr body)
		{
			unsafe { return *(BodyGetLinearVelUnsafe(body)); }
		}
#endif

		[DllImport("odesingle", EntryPoint = "dBodyGetMass"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyGetMass(IntPtr body, out Mass mass);

		[DllImport("odesingle", EntryPoint = "dBodyGetNumJoints"), SuppressUnmanagedCodeSecurity]
		public static extern int BodyGetNumJoints(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodyGetPointVel"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyGetPointVel(IntPtr body, dReal px, dReal py, dReal pz, out Vector3 result);

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dBodyGetPosition"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static Vector3* BodyGetPositionUnsafe(IntPtr body);
		public static Vector3 BodyGetPosition(IntPtr body)
		{
			unsafe { return *(BodyGetPositionUnsafe(body)); }
		}
#endif

		[DllImport("odesingle", EntryPoint = "dBodyGetPosRelPoint"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyGetPosRelPoint(IntPtr body, dReal px, dReal py, dReal pz, out Vector3 result);

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dBodyGetQuaternion"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static Quaternion* BodyGetQuaternionUnsafe(IntPtr body);
		public static Quaternion BodyGetQuaternion(IntPtr body)
		{
			unsafe { return *(BodyGetQuaternionUnsafe(body)); }
		}
#endif

		[DllImport("odesingle", EntryPoint = "dBodyGetRelPointPos"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyGetRelPointPos(IntPtr body, dReal px, dReal py, dReal pz, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dBodyGetRelPointVel"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyGetRelPointVel(IntPtr body, dReal px, dReal py, dReal pz, out Vector3 result);

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dBodyGetRotation"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static Matrix3* BodyGetRotationUnsafe(IntPtr body);
		public static Matrix3 BodyGetRotation(IntPtr body)
		{
			unsafe { return *(BodyGetRotationUnsafe(body)); }
		}
#endif

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dBodyGetTorque"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static Vector3* BodyGetTorqueUnsafe(IntPtr body);
		public static Vector3 BodyGetTorque(IntPtr body)
		{
			unsafe { return *(BodyGetTorqueUnsafe(body)); }
		}
#endif

        [DllImport("odesingle", EntryPoint = "dBodyGetWorld"), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr BodyGetWorld(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodyIsEnabled"), SuppressUnmanagedCodeSecurity]
		public static extern bool BodyIsEnabled(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodySetAngularVel"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetAngularVel(IntPtr body, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dBodySetAutoDisableAngularThreshold"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetAutoDisableAngularThreshold(IntPtr body, dReal angular_threshold);

		[DllImport("odesingle", EntryPoint = "dBodySetAutoDisableDefaults"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetAutoDisableDefaults(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodySetAutoDisableFlag"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetAutoDisableFlag(IntPtr body, bool do_auto_disable);

		[DllImport("odesingle", EntryPoint = "dBodySetAutoDisableLinearThreshold"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetAutoDisableLinearThreshold(IntPtr body, dReal linear_threshold);

		[DllImport("odesingle", EntryPoint = "dBodySetAutoDisableSteps"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetAutoDisableSteps(IntPtr body, int steps);

		[DllImport("odesingle", EntryPoint = "dBodySetAutoDisableTime"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetAutoDisableTime(IntPtr body, dReal time);

		[DllImport("odesingle", EntryPoint = "dBodySetData"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetData(IntPtr body, IntPtr data);

		[DllImport("odesingle", EntryPoint = "dBodySetFiniteRotationMode"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetFiniteRotationMode(IntPtr body, int mode);

		[DllImport("odesingle", EntryPoint = "dBodySetFiniteRotationAxis"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetFiniteRotationAxis(IntPtr body, dReal x, dReal y, dReal z);

        [DllImport("odesingle", EntryPoint = "dBodySetLinearDamping"), SuppressUnmanagedCodeSecurity]
        public static extern void BodySetLinearDamping(IntPtr body, dReal scale);

        [DllImport("odesingle", EntryPoint = "dBodySetAngularDamping"), SuppressUnmanagedCodeSecurity]
        public static extern void BodySetAngularDamping(IntPtr body, dReal scale);

        [DllImport("odesingle", EntryPoint = "dBodyGetLinearDamping"), SuppressUnmanagedCodeSecurity]
        public static extern dReal BodyGetLinearDamping(IntPtr body);

        [DllImport("odesingle", EntryPoint = "dBodyGetAngularDamping"), SuppressUnmanagedCodeSecurity]
        public static extern dReal BodyGetAngularDamping(IntPtr body);

        [DllImport("odesingle", EntryPoint = "dBodySetAngularDamping"), SuppressUnmanagedCodeSecurity]
        public static extern void BodySetDamping(IntPtr body, dReal linear_scale, dReal angular_scale);

        [DllImport("odesingle", EntryPoint = "dBodySetAngularDampingThreshold"), SuppressUnmanagedCodeSecurity]
        public static extern void BodySetAngularDampingThreshold(IntPtr body, dReal threshold);

        [DllImport("odesingle", EntryPoint = "dBodySetLinearDampingThreshold"), SuppressUnmanagedCodeSecurity]
        public static extern void BodySetLinearDampingThreshold(IntPtr body, dReal threshold);

        [DllImport("odesingle", EntryPoint = "dBodyGetLinearDampingThreshold"), SuppressUnmanagedCodeSecurity]
        public static extern dReal BodyGetLinearDampingThreshold(IntPtr body);

        [DllImport("odesingle", EntryPoint = "dBodyGetAngularDampingThreshold"), SuppressUnmanagedCodeSecurity]
        public static extern dReal BodyGetAngularDampingThreshold(IntPtr body);

		[DllImport("odesingle", EntryPoint = "dBodySetForce"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetForce(IntPtr body, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dBodySetGravityMode"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetGravityMode(IntPtr body, bool mode);

        /// <summary>
        /// Sets the Gyroscopic term status on the body specified.
        /// </summary>
        /// <param name="body">Pointer to body</param>
        /// <param name="enabled">NonZero enabled, Zero disabled</param>
        [DllImport("odesingle", EntryPoint = "dBodySetGyroscopicMode"), SuppressUnmanagedCodeSecurity]
        public static extern void dBodySetGyroscopicMode(IntPtr body, int enabled);

		[DllImport("odesingle", EntryPoint = "dBodySetLinearVel"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetLinearVel(IntPtr body, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dBodySetMass"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetMass(IntPtr body, ref Mass mass);

		[DllImport("odesingle", EntryPoint = "dBodySetPosition"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetPosition(IntPtr body, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dBodySetQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetQuaternion(IntPtr body, ref Quaternion q);

		[DllImport("odesingle", EntryPoint = "dBodySetQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetQuaternion(IntPtr body, ref dReal w);

		[DllImport("odesingle", EntryPoint = "dBodySetRotation"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetRotation(IntPtr body, ref Matrix3 R);

		[DllImport("odesingle", EntryPoint = "dBodySetRotation"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetRotation(IntPtr body, ref dReal M00);

		[DllImport("odesingle", EntryPoint = "dBodySetTorque"), SuppressUnmanagedCodeSecurity]
		public static extern void BodySetTorque(IntPtr body, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dBodyVectorFromWorld"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyVectorFromWorld(IntPtr body, dReal px, dReal py, dReal pz, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dBodyVectorToWorld"), SuppressUnmanagedCodeSecurity]
		public static extern void BodyVectorToWorld(IntPtr body, dReal px, dReal py, dReal pz, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dBoxBox"), SuppressUnmanagedCodeSecurity]
		public static extern void BoxBox(ref Vector3 p1, ref Matrix3 R1,
			ref Vector3 side1, ref Vector3 p2,
			ref Matrix3 R2, ref Vector3 side2,
			ref Vector3 normal, out dReal depth, out int return_code,
			int maxc, out ContactGeom contact, int skip);

		[DllImport("odesingle", EntryPoint = "dBoxTouchesBox"), SuppressUnmanagedCodeSecurity]
		public static extern void BoxTouchesBox(ref Vector3 _p1, ref Matrix3 R1,
			ref Vector3 side1, ref Vector3 _p2,
			ref Matrix3 R2, ref Vector3 side2);

        [DllImport("odesingle", EntryPoint = "dCleanupODEAllDataForThread"), SuppressUnmanagedCodeSecurity]
		public static extern void CleanupODEAllDataForThread();
        
		[DllImport("odesingle", EntryPoint = "dClosestLineSegmentPoints"), SuppressUnmanagedCodeSecurity]
		public static extern void ClosestLineSegmentPoints(ref Vector3 a1, ref Vector3 a2, 
			ref Vector3 b1, ref Vector3 b2, 
			ref Vector3 cp1, ref Vector3 cp2);

		[DllImport("odesingle", EntryPoint = "dCloseODE"), SuppressUnmanagedCodeSecurity]
		public static extern void CloseODE();

		[DllImport("odesingle", EntryPoint = "dCollide"), SuppressUnmanagedCodeSecurity]
		public static extern int Collide(IntPtr o1, IntPtr o2, int flags, [In, Out] ContactGeom[] contact, int skip);

		[DllImport("odesingle", EntryPoint = "dConnectingJoint"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr ConnectingJoint(IntPtr j1, IntPtr j2);

		[DllImport("odesingle", EntryPoint = "dCreateBox"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr CreateBox(IntPtr space, dReal lx, dReal ly, dReal lz);

		[DllImport("odesingle", EntryPoint = "dCreateCapsule"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr CreateCapsule(IntPtr space, dReal radius, dReal length);

		[DllImport("odesingle", EntryPoint = "dCreateConvex"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr CreateConvex(IntPtr space, dReal[] planes, int planeCount, dReal[] points, int pointCount, int[] polygons);

		[DllImport("odesingle", EntryPoint = "dCreateCylinder"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr CreateCylinder(IntPtr space, dReal radius, dReal length);

		[DllImport("odesingle", EntryPoint = "dCreateHeightfield"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr CreateHeightfield(IntPtr space, IntPtr data, int bPlaceable);

		[DllImport("odesingle", EntryPoint = "dCreateGeom"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr CreateGeom(int classnum);

		[DllImport("odesingle", EntryPoint = "dCreateGeomClass"), SuppressUnmanagedCodeSecurity]
		public static extern int CreateGeomClass(ref GeomClass classptr);

		[DllImport("odesingle", EntryPoint = "dCreateGeomTransform"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr CreateGeomTransform(IntPtr space);

		[DllImport("odesingle", EntryPoint = "dCreatePlane"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr CreatePlane(IntPtr space, dReal a, dReal b, dReal c, dReal d);

		[DllImport("odesingle", EntryPoint = "dCreateRay"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr CreateRay(IntPtr space, dReal length);

		[DllImport("odesingle", EntryPoint = "dCreateSphere"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr CreateSphere(IntPtr space, dReal radius);

		[DllImport("odesingle", EntryPoint = "dCreateTriMesh"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr CreateTriMesh(IntPtr space, IntPtr data, 
			TriCallback callback, TriArrayCallback arrayCallback, TriRayCallback rayCallback);

		[DllImport("odesingle", EntryPoint = "dDot"), SuppressUnmanagedCodeSecurity]
		public static extern dReal Dot(ref dReal X0, ref dReal X1, int n);

		[DllImport("odesingle", EntryPoint = "dDQfromW"), SuppressUnmanagedCodeSecurity]
		public static extern void DQfromW(dReal[] dq, ref Vector3 w, ref Quaternion q);

		[DllImport("odesingle", EntryPoint = "dFactorCholesky"), SuppressUnmanagedCodeSecurity]
		public static extern int FactorCholesky(ref dReal A00, int n);

		[DllImport("odesingle", EntryPoint = "dFactorLDLT"), SuppressUnmanagedCodeSecurity]
		public static extern void FactorLDLT(ref dReal A, out dReal d, int n, int nskip);

		[DllImport("odesingle", EntryPoint = "dGeomBoxGetLengths"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomBoxGetLengths(IntPtr geom, out Vector3 len);

		[DllImport("odesingle", EntryPoint = "dGeomBoxGetLengths"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomBoxGetLengths(IntPtr geom, out dReal x);

		[DllImport("odesingle", EntryPoint = "dGeomBoxPointDepth"), SuppressUnmanagedCodeSecurity]
		public static extern dReal GeomBoxPointDepth(IntPtr geom, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dGeomBoxSetLengths"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomBoxSetLengths(IntPtr geom, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dGeomCapsuleGetParams"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomCapsuleGetParams(IntPtr geom, out dReal radius, out dReal length);

		[DllImport("odesingle", EntryPoint = "dGeomCapsulePointDepth"), SuppressUnmanagedCodeSecurity]
		public static extern dReal GeomCapsulePointDepth(IntPtr geom, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dGeomCapsuleSetParams"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomCapsuleSetParams(IntPtr geom, dReal radius, dReal length);

		[DllImport("odesingle", EntryPoint = "dGeomClearOffset"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomClearOffset(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomCopyOffsetPosition"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomCopyOffsetPosition(IntPtr geom, ref Vector3 pos);

		[DllImport("odesingle", EntryPoint = "dGeomCopyOffsetPosition"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomCopyOffsetPosition(IntPtr geom, ref dReal X);

		[DllImport("odesingle", EntryPoint = "dGeomGetOffsetQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomCopyOffsetQuaternion(IntPtr geom, ref Quaternion Q);

		[DllImport("odesingle", EntryPoint = "dGeomGetOffsetQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomCopyOffsetQuaternion(IntPtr geom, ref dReal X);

		[DllImport("odesingle", EntryPoint = "dGeomCopyOffsetRotation"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomCopyOffsetRotation(IntPtr geom, ref Matrix3 R);

		[DllImport("odesingle", EntryPoint = "dGeomCopyOffsetRotation"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomCopyOffsetRotation(IntPtr geom, ref dReal M00);

		[DllImport("odesingle", EntryPoint = "dGeomCopyPosition"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomCopyPosition(IntPtr geom, out Vector3 pos);

		[DllImport("odesingle", EntryPoint = "dGeomCopyPosition"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomCopyPosition(IntPtr geom, out dReal X);

		[DllImport("odesingle", EntryPoint = "dGeomCopyRotation"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomCopyRotation(IntPtr geom, out Matrix3 R);

		[DllImport("odesingle", EntryPoint = "dGeomCopyRotation"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomCopyRotation(IntPtr geom, out dReal M00);

		[DllImport("odesingle", EntryPoint = "dGeomCylinderGetParams"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomCylinderGetParams(IntPtr geom, out dReal radius, out dReal length);

		[DllImport("odesingle", EntryPoint = "dGeomCylinderSetParams"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomCylinderSetParams(IntPtr geom, dReal radius, dReal length);

		[DllImport("odesingle", EntryPoint = "dGeomDestroy"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomDestroy(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomDisable"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomDisable(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomEnable"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomEnable(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomGetAABB"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomGetAABB(IntPtr geom, out AABB aabb);

		[DllImport("odesingle", EntryPoint = "dGeomGetAABB"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomGetAABB(IntPtr geom, out dReal minX);

		[DllImport("odesingle", EntryPoint = "dGeomGetBody"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomGetBody(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomGetCategoryBits"), SuppressUnmanagedCodeSecurity]
		public static extern int GeomGetCategoryBits(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomGetClassData"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomGetClassData(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomGetCollideBits"), SuppressUnmanagedCodeSecurity]
		public static extern int GeomGetCollideBits(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomGetClass"), SuppressUnmanagedCodeSecurity]
		public static extern GeomClassID GeomGetClass(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomGetData"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomGetData(IntPtr geom);

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dGeomGetOffsetPosition"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static Vector3* GeomGetOffsetPositionUnsafe(IntPtr geom);
		public static Vector3 GeomGetOffsetPosition(IntPtr geom)
		{
			unsafe { return *(GeomGetOffsetPositionUnsafe(geom)); }
		}
#endif

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dGeomGetOffsetRotation"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static Matrix3* GeomGetOffsetRotationUnsafe(IntPtr geom);
		public static Matrix3 GeomGetOffsetRotation(IntPtr geom)
		{
			unsafe { return *(GeomGetOffsetRotationUnsafe(geom)); }
		}
#endif

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dGeomGetPosition"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static Vector3* GeomGetPositionUnsafe(IntPtr geom);
		public static Vector3 GeomGetPosition(IntPtr geom)
		{
			unsafe { return *(GeomGetPositionUnsafe(geom)); }
		}
#endif

		[DllImport("odesingle", EntryPoint = "dGeomGetQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomCopyQuaternion(IntPtr geom, out Quaternion q);

		[DllImport("odesingle", EntryPoint = "dGeomGetQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomCopyQuaternion(IntPtr geom, out dReal X);

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dGeomGetRotation"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static Matrix3* GeomGetRotationUnsafe(IntPtr geom);
		public static Matrix3 GeomGetRotation(IntPtr geom)
		{
			unsafe { return *(GeomGetRotationUnsafe(geom)); }
		}
#endif

		[DllImport("odesingle", EntryPoint = "dGeomGetSpace"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomGetSpace(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataBuildByte"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldDataBuildByte(IntPtr d, byte[] pHeightData, int bCopyHeightData,
				dReal width, dReal depth, int widthSamples, int depthSamples,
				dReal scale, dReal offset, dReal thickness, int bWrap);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataBuildByte"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldDataBuildByte(IntPtr d, IntPtr pHeightData, int bCopyHeightData,
				dReal width, dReal depth, int widthSamples, int depthSamples,
				dReal scale, dReal offset, dReal thickness,	int bWrap);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataBuildCallback"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldDataBuildCallback(IntPtr d, IntPtr pUserData, HeightfieldGetHeight pCallback,
				dReal width, dReal depth, int widthSamples, int depthSamples,
				dReal scale, dReal offset, dReal thickness, int bWrap);

		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataBuildShort"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldDataBuildShort(IntPtr d, ushort[] pHeightData, int bCopyHeightData,
				dReal width, dReal depth, int widthSamples, int depthSamples,
				dReal scale, dReal offset, dReal thickness, int bWrap);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataBuildShort"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldDataBuildShort(IntPtr d, short[] pHeightData, int bCopyHeightData,
				dReal width, dReal depth, int widthSamples, int depthSamples,
				dReal scale, dReal offset, dReal thickness, int bWrap);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataBuildShort"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldDataBuildShort(IntPtr d, IntPtr pHeightData, int bCopyHeightData,
				dReal width, dReal depth, int widthSamples, int depthSamples,
				dReal scale, dReal offset, dReal thickness, int bWrap);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataBuildSingle"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldDataBuildSingle(IntPtr d, float[] pHeightData, int bCopyHeightData,
				dReal width, dReal depth, int widthSamples, int depthSamples,
				dReal scale, dReal offset, dReal thickness, int bWrap);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataBuildSingle"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldDataBuildSingle(IntPtr d, IntPtr pHeightData, int bCopyHeightData,
				dReal width, dReal depth, int widthSamples, int depthSamples,
				dReal scale, dReal offset, dReal thickness, int bWrap);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataBuildDouble"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldDataBuildDouble(IntPtr d, double[] pHeightData, int bCopyHeightData,
				dReal width, dReal depth, int widthSamples, int depthSamples,
				dReal scale, dReal offset, dReal thickness, int bWrap);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataBuildDouble"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldDataBuildDouble(IntPtr d, IntPtr pHeightData, int bCopyHeightData,
				dReal width, dReal depth, int widthSamples, int depthSamples,
				dReal scale, dReal offset, dReal thickness, int bWrap);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataCreate"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomHeightfieldDataCreate();

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataDestroy"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldDataDestroy(IntPtr d);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldDataSetBounds"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldDataSetBounds(IntPtr d, dReal minHeight, dReal maxHeight);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldGetHeightfieldData"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomHeightfieldGetHeightfieldData(IntPtr g);

		[DllImport("odesingle", EntryPoint = "dGeomHeightfieldSetHeightfieldData"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomHeightfieldSetHeightfieldData(IntPtr g, IntPtr d);

		[DllImport("odesingle", EntryPoint = "dGeomIsEnabled"), SuppressUnmanagedCodeSecurity]
		public static extern bool GeomIsEnabled(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomIsOffset"), SuppressUnmanagedCodeSecurity]
		public static extern bool GeomIsOffset(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomIsSpace"), SuppressUnmanagedCodeSecurity]
		public static extern bool GeomIsSpace(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomPlaneGetParams"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomPlaneGetParams(IntPtr geom, ref Vector4 result);

		[DllImport("odesingle", EntryPoint = "dGeomPlaneGetParams"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomPlaneGetParams(IntPtr geom, ref dReal A);

		[DllImport("odesingle", EntryPoint = "dGeomPlanePointDepth"), SuppressUnmanagedCodeSecurity]
		public static extern dReal GeomPlanePointDepth(IntPtr geom, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dGeomPlaneSetParams"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomPlaneSetParams(IntPtr plane, dReal a, dReal b, dReal c, dReal d);

		[DllImport("odesingle", EntryPoint = "dGeomRayGet"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomRayGet(IntPtr ray, ref Vector3 start, ref Vector3 dir);

		[DllImport("odesingle", EntryPoint = "dGeomRayGet"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomRayGet(IntPtr ray, ref dReal startX, ref dReal dirX);

		[DllImport("odesingle", EntryPoint = "dGeomRayGetClosestHit"), SuppressUnmanagedCodeSecurity]
		public static extern int GeomRayGetClosestHit(IntPtr ray);

		[DllImport("odesingle", EntryPoint = "dGeomRayGetLength"), SuppressUnmanagedCodeSecurity]
		public static extern dReal GeomRayGetLength(IntPtr ray);

		[DllImport("odesingle", EntryPoint = "dGeomRayGetParams"), SuppressUnmanagedCodeSecurity]
		public static extern dReal GeomRayGetParams(IntPtr g, out int firstContact, out int backfaceCull);

		[DllImport("odesingle", EntryPoint = "dGeomRaySet"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomRaySet(IntPtr ray, dReal px, dReal py, dReal pz, dReal dx, dReal dy, dReal dz);

		[DllImport("odesingle", EntryPoint = "dGeomRaySetClosestHit"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomRaySetClosestHit(IntPtr ray, int closestHit);

		[DllImport("odesingle", EntryPoint = "dGeomRaySetLength"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomRaySetLength(IntPtr ray, dReal length);

		[DllImport("odesingle", EntryPoint = "dGeomRaySetParams"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomRaySetParams(IntPtr ray, int firstContact, int backfaceCull);

		[DllImport("odesingle", EntryPoint = "dGeomSetBody"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetBody(IntPtr geom, IntPtr body);

		[DllImport("odesingle", EntryPoint = "dGeomSetCategoryBits"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetCategoryBits(IntPtr geom, int bits);

		[DllImport("odesingle", EntryPoint = "dGeomSetCollideBits"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetCollideBits(IntPtr geom, int bits);

		[DllImport("odesingle", EntryPoint = "dGeomSetConvex"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomSetConvex(IntPtr geom, dReal[] planes, int planeCount, dReal[] points, int pointCount, int[] polygons);

		[DllImport("odesingle", EntryPoint = "dGeomSetData"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetData(IntPtr geom, IntPtr data);

		[DllImport("odesingle", EntryPoint = "dGeomSetOffsetPosition"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetOffsetPosition(IntPtr geom, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dGeomSetOffsetQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetOffsetQuaternion(IntPtr geom, ref Quaternion Q);

		[DllImport("odesingle", EntryPoint = "dGeomSetOffsetQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetOffsetQuaternion(IntPtr geom, ref dReal X);

		[DllImport("odesingle", EntryPoint = "dGeomSetOffsetRotation"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetOffsetRotation(IntPtr geom, ref Matrix3 R);

		[DllImport("odesingle", EntryPoint = "dGeomSetOffsetRotation"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetOffsetRotation(IntPtr geom, ref dReal M00);

		[DllImport("odesingle", EntryPoint = "dGeomSetOffsetWorldPosition"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetOffsetWorldPosition(IntPtr geom, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dGeomSetOffsetWorldQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetOffsetWorldQuaternion(IntPtr geom, ref Quaternion Q);

		[DllImport("odesingle", EntryPoint = "dGeomSetOffsetWorldQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetOffsetWorldQuaternion(IntPtr geom, ref dReal X);

		[DllImport("odesingle", EntryPoint = "dGeomSetOffsetWorldRotation"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetOffsetWorldRotation(IntPtr geom, ref Matrix3 R);

		[DllImport("odesingle", EntryPoint = "dGeomSetOffsetWorldRotation"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetOffsetWorldRotation(IntPtr geom, ref dReal M00);

		[DllImport("odesingle", EntryPoint = "dGeomSetPosition"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetPosition(IntPtr geom, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dGeomSetQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetQuaternion(IntPtr geom, ref Quaternion quat);

		[DllImport("odesingle", EntryPoint = "dGeomSetQuaternion"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetQuaternion(IntPtr geom, ref dReal w);

		[DllImport("odesingle", EntryPoint = "dGeomSetRotation"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetRotation(IntPtr geom, ref Matrix3 R);

		[DllImport("odesingle", EntryPoint = "dGeomSetRotation"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSetRotation(IntPtr geom, ref dReal M00);

		[DllImport("odesingle", EntryPoint = "dGeomSphereGetRadius"), SuppressUnmanagedCodeSecurity]
		public static extern dReal GeomSphereGetRadius(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomSpherePointDepth"), SuppressUnmanagedCodeSecurity]
		public static extern dReal GeomSpherePointDepth(IntPtr geom, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dGeomSphereSetRadius"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomSphereSetRadius(IntPtr geom, dReal radius);

		[DllImport("odesingle", EntryPoint = "dGeomTransformGetCleanup"), SuppressUnmanagedCodeSecurity]
		public static extern int GeomTransformGetCleanup(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomTransformGetGeom"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomTransformGetGeom(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomTransformGetInfo"), SuppressUnmanagedCodeSecurity]
		public static extern int GeomTransformGetInfo(IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dGeomTransformSetCleanup"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTransformSetCleanup(IntPtr geom, int mode);

		[DllImport("odesingle", EntryPoint = "dGeomTransformSetGeom"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTransformSetGeom(IntPtr geom, IntPtr obj);

		[DllImport("odesingle", EntryPoint = "dGeomTransformSetInfo"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTransformSetInfo(IntPtr geom, int info);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataBuildDouble"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataBuildDouble(IntPtr d,
			double[] vertices, int vertexStride, int vertexCount,
			int[] indices, int indexCount, int triStride);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataBuildDouble"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataBuildDouble(IntPtr d,
			IntPtr vertices, int vertexStride, int vertexCount,
			IntPtr indices, int indexCount, int triStride);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataBuildDouble1"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataBuildDouble1(IntPtr d,
			double[] vertices, int vertexStride, int vertexCount,
			int[] indices, int indexCount, int triStride,
			double[] normals);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataBuildDouble1"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataBuildDouble(IntPtr d,
			IntPtr vertices, int vertexStride, int vertexCount,
			IntPtr indices, int indexCount, int triStride,
			IntPtr normals);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataBuildSimple"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataBuildSingle(IntPtr d,
			dReal[] vertices, int vertexStride, int vertexCount,
			int[] indices, int indexCount, int triStride);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataBuildSimple"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataBuildSingle(IntPtr d,
			IntPtr vertices, int vertexStride, int vertexCount,
			IntPtr indices, int indexCount, int triStride);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataBuildSimple1"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataBuildSingle1(IntPtr d,
			dReal[] vertices, int vertexStride, int vertexCount,
			int[] indices, int indexCount, int triStride,
			dReal[] normals);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataBuildSimple1"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataBuildSingle1(IntPtr d,
			IntPtr vertices, int vertexStride, int vertexCount,
			IntPtr indices, int indexCount, int triStride,
			IntPtr normals);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataBuildSingle"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataBuildSimple(IntPtr d, 
			float[] vertices, int vertexStride, int vertexCount,
			int[] indices, int indexCount, int triStride);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataBuildSingle"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataBuildSimple(IntPtr d,
			IntPtr vertices, int vertexStride, int vertexCount,
			IntPtr indices, int indexCount, int triStride);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataBuildSingle1"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataBuildSimple1(IntPtr d, 
			float[] vertices, int vertexStride, int vertexCount,
			int[] indices, int indexCount, int triStride,
			float[] normals);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataBuildSingle1"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataBuildSimple1(IntPtr d,
			IntPtr vertices, int vertexStride, int vertexCount,
			IntPtr indices, int indexCount, int triStride,
			IntPtr normals);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshClearTCCache"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshClearTCCache(IntPtr g);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataCreate"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomTriMeshDataCreate();

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataDestroy"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataDestroy(IntPtr d);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataGet"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomTriMeshDataGet(IntPtr d, int data_id);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataPreprocess"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataPreprocess(IntPtr d);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataSet"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataSet(IntPtr d, int data_id, IntPtr in_data);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshDataUpdate"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshDataUpdate(IntPtr d);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshEnableTC"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshEnableTC(IntPtr g, int geomClass, bool enable);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshGetArrayCallback"), SuppressUnmanagedCodeSecurity]
		public static extern TriArrayCallback GeomTriMeshGetArrayCallback(IntPtr g);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshGetCallback"), SuppressUnmanagedCodeSecurity]
		public static extern TriCallback GeomTriMeshGetCallback(IntPtr g);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshGetData"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomTriMeshGetData(IntPtr g);

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dGeomTriMeshGetLastTransform"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static Matrix4* GeomTriMeshGetLastTransformUnsafe(IntPtr geom);
		public static Matrix4 GeomTriMeshGetLastTransform(IntPtr geom)
		{
			unsafe { return *(GeomTriMeshGetLastTransformUnsafe(geom)); }
		}
#endif

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshGetPoint"), SuppressUnmanagedCodeSecurity]
		public extern static void GeomTriMeshGetPoint(IntPtr g, int index, dReal u, dReal v, ref Vector3 outVec);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshGetRayCallback"), SuppressUnmanagedCodeSecurity]
		public static extern TriRayCallback GeomTriMeshGetRayCallback(IntPtr g);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshGetTriangle"), SuppressUnmanagedCodeSecurity]
		public extern static void GeomTriMeshGetTriangle(IntPtr g, int index, ref Vector3 v0, ref Vector3 v1, ref Vector3 v2);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshGetTriangleCount"), SuppressUnmanagedCodeSecurity]
		public extern static int GeomTriMeshGetTriangleCount(IntPtr g);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshGetTriMeshDataID"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GeomTriMeshGetTriMeshDataID(IntPtr g);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshIsTCEnabled"), SuppressUnmanagedCodeSecurity]
		public static extern bool GeomTriMeshIsTCEnabled(IntPtr g, int geomClass);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshSetArrayCallback"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshSetArrayCallback(IntPtr g, TriArrayCallback arrayCallback);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshSetCallback"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshSetCallback(IntPtr g, TriCallback callback);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshSetData"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshSetData(IntPtr g, IntPtr data);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshSetLastTransform"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshSetLastTransform(IntPtr g, ref Matrix4 last_trans);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshSetLastTransform"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshSetLastTransform(IntPtr g, ref dReal M00);

		[DllImport("odesingle", EntryPoint = "dGeomTriMeshSetRayCallback"), SuppressUnmanagedCodeSecurity]
		public static extern void GeomTriMeshSetRayCallback(IntPtr g, TriRayCallback callback);

		[DllImport("odesingle", EntryPoint = "dHashSpaceCreate"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr HashSpaceCreate(IntPtr space);

		[DllImport("odesingle", EntryPoint = "dHashSpaceGetLevels"), SuppressUnmanagedCodeSecurity]
		public static extern void HashSpaceGetLevels(IntPtr space, out int minlevel, out int maxlevel);

		[DllImport("odesingle", EntryPoint = "dHashSpaceSetLevels"), SuppressUnmanagedCodeSecurity]
		public static extern void HashSpaceSetLevels(IntPtr space, int minlevel, int maxlevel);

		[DllImport("odesingle", EntryPoint = "dInfiniteAABB"), SuppressUnmanagedCodeSecurity]
		public static extern void InfiniteAABB(IntPtr geom, out AABB aabb);

		[DllImport("odesingle", EntryPoint = "dInitODE"), SuppressUnmanagedCodeSecurity]
		public static extern void InitODE();

#if !dNO_UNSAFE_CODE
        [CLSCompliant(false)]
        [DllImport("odesingle", EntryPoint = "dInitODE2"), SuppressUnmanagedCodeSecurity]
        public static extern int InitODE2(uint ODEInitFlags);
#endif
		[DllImport("odesingle", EntryPoint = "dIsPositiveDefinite"), SuppressUnmanagedCodeSecurity]
		public static extern int IsPositiveDefinite(ref dReal A, int n);

		[DllImport("odesingle", EntryPoint = "dInvertPDMatrix"), SuppressUnmanagedCodeSecurity]
		public static extern int InvertPDMatrix(ref dReal A, out dReal Ainv, int n);

		[DllImport("odesingle", EntryPoint = "dJointAddAMotorTorques"), SuppressUnmanagedCodeSecurity]
		public static extern void JointAddAMotorTorques(IntPtr joint, dReal torque1, dReal torque2, dReal torque3);

		[DllImport("odesingle", EntryPoint = "dJointAddHingeTorque"), SuppressUnmanagedCodeSecurity]
		public static extern void JointAddHingeTorque(IntPtr joint, dReal torque);

		[DllImport("odesingle", EntryPoint = "dJointAddHinge2Torque"), SuppressUnmanagedCodeSecurity]
		public static extern void JointAddHinge2Torques(IntPtr joint, dReal torque1, dReal torque2);

		[DllImport("odesingle", EntryPoint = "dJointAddPRTorque"), SuppressUnmanagedCodeSecurity]
		public static extern void JointAddPRTorque(IntPtr joint, dReal torque);

		[DllImport("odesingle", EntryPoint = "dJointAddUniversalTorque"), SuppressUnmanagedCodeSecurity]
		public static extern void JointAddUniversalTorques(IntPtr joint, dReal torque1, dReal torque2);

		[DllImport("odesingle", EntryPoint = "dJointAddSliderForce"), SuppressUnmanagedCodeSecurity]
		public static extern void JointAddSliderForce(IntPtr joint, dReal force);

		[DllImport("odesingle", EntryPoint = "dJointAttach"), SuppressUnmanagedCodeSecurity]
		public static extern void JointAttach(IntPtr joint, IntPtr body1, IntPtr body2);

		[DllImport("odesingle", EntryPoint = "dJointCreateAMotor"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointCreateAMotor(IntPtr world, IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointCreateBall"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointCreateBall(IntPtr world, IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointCreateContact"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointCreateContact(IntPtr world, IntPtr group, ref Contact contact);

		[DllImport("odesingle", EntryPoint = "dJointCreateFixed"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointCreateFixed(IntPtr world, IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointCreateHinge"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointCreateHinge(IntPtr world, IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointCreateHinge2"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointCreateHinge2(IntPtr world, IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointCreateLMotor"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointCreateLMotor(IntPtr world, IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointCreateNull"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointCreateNull(IntPtr world, IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointCreatePR"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointCreatePR(IntPtr world, IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointCreatePlane2D"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointCreatePlane2D(IntPtr world, IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointCreateSlider"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointCreateSlider(IntPtr world, IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointCreateUniversal"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointCreateUniversal(IntPtr world, IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointDestroy"), SuppressUnmanagedCodeSecurity]
		public static extern void JointDestroy(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetAMotorAngle"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetAMotorAngle(IntPtr j, int anum);

		[DllImport("odesingle", EntryPoint = "dJointGetAMotorAngleRate"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetAMotorAngleRate(IntPtr j, int anum);

		[DllImport("odesingle", EntryPoint = "dJointGetAMotorAxis"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetAMotorAxis(IntPtr j, int anum, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetAMotorAxisRel"), SuppressUnmanagedCodeSecurity]
		public static extern int JointGetAMotorAxisRel(IntPtr j, int anum);

		[DllImport("odesingle", EntryPoint = "dJointGetAMotorMode"), SuppressUnmanagedCodeSecurity]
		public static extern int JointGetAMotorMode(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetAMotorNumAxes"), SuppressUnmanagedCodeSecurity]
		public static extern int JointGetAMotorNumAxes(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetAMotorParam"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetAMotorParam(IntPtr j, int parameter);

		[DllImport("odesingle", EntryPoint = "dJointGetBallAnchor"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetBallAnchor(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetBallAnchor2"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetBallAnchor2(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetBody"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointGetBody(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetData"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointGetData(IntPtr j);

#if !dNO_UNSAFE_CODE
		[CLSCompliant(false)]
		[DllImport("odesingle", EntryPoint = "dJointGetFeedback"), SuppressUnmanagedCodeSecurity]
		public extern unsafe static JointFeedback* JointGetFeedbackUnsafe(IntPtr j);
		public static JointFeedback JointGetFeedback(IntPtr j)
		{
			unsafe { return *(JointGetFeedbackUnsafe(j)); }
		}
#endif

		[DllImport("odesingle", EntryPoint = "dJointGetHingeAnchor"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetHingeAnchor(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetHingeAngle"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetHingeAngle(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetHingeAngleRate"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetHingeAngleRate(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetHingeAxis"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetHingeAxis(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetHingeParam"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetHingeParam(IntPtr j, int parameter);

		[DllImport("odesingle", EntryPoint = "dJointGetHinge2Angle1"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetHinge2Angle1(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetHinge2Angle1Rate"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetHinge2Angle1Rate(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetHinge2Angle2Rate"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetHinge2Angle2Rate(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetHingeAnchor2"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetHingeAnchor2(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetHinge2Anchor"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetHinge2Anchor(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetHinge2Anchor2"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetHinge2Anchor2(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetHinge2Axis1"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetHinge2Axis1(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetHinge2Axis2"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetHinge2Axis2(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetHinge2Param"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetHinge2Param(IntPtr j, int parameter);

		[DllImport("odesingle", EntryPoint = "dJointGetLMotorAxis"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetLMotorAxis(IntPtr j, int anum, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetLMotorNumAxes"), SuppressUnmanagedCodeSecurity]
		public static extern int JointGetLMotorNumAxes(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetLMotorParam"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetLMotorParam(IntPtr j, int parameter);

		[DllImport("odesingle", EntryPoint = "dJointGetPRAnchor"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetPRAnchor(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetPRAxis1"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetPRAxis1(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetPRAxis2"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetPRAxis2(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetPRParam"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetPRParam(IntPtr j, int parameter);

		[DllImport("odesingle", EntryPoint = "dJointGetPRPosition"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetPRPosition(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetPRPositionRate"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetPRPositionRate(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetSliderAxis"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetSliderAxis(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetSliderParam"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetSliderParam(IntPtr j, int parameter);

		[DllImport("odesingle", EntryPoint = "dJointGetSliderPosition"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetSliderPosition(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetSliderPositionRate"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetSliderPositionRate(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetType"), SuppressUnmanagedCodeSecurity]
		public static extern JointType JointGetType(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetUniversalAnchor"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetUniversalAnchor(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetUniversalAnchor2"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetUniversalAnchor2(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetUniversalAngle1"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetUniversalAngle1(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetUniversalAngle1Rate"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetUniversalAngle1Rate(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetUniversalAngle2"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetUniversalAngle2(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetUniversalAngle2Rate"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetUniversalAngle2Rate(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointGetUniversalAngles"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetUniversalAngles(IntPtr j, out dReal angle1, out dReal angle2);

		[DllImport("odesingle", EntryPoint = "dJointGetUniversalAxis1"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetUniversalAxis1(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetUniversalAxis2"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGetUniversalAxis2(IntPtr j, out Vector3 result);

		[DllImport("odesingle", EntryPoint = "dJointGetUniversalParam"), SuppressUnmanagedCodeSecurity]
		public static extern dReal JointGetUniversalParam(IntPtr j, int parameter);

		[DllImport("odesingle", EntryPoint = "dJointGroupCreate"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr JointGroupCreate(int max_size);

		[DllImport("odesingle", EntryPoint = "dJointGroupDestroy"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGroupDestroy(IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointGroupEmpty"), SuppressUnmanagedCodeSecurity]
		public static extern void JointGroupEmpty(IntPtr group);

		[DllImport("odesingle", EntryPoint = "dJointSetAMotorAngle"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetAMotorAngle(IntPtr j, int anum, dReal angle);

		[DllImport("odesingle", EntryPoint = "dJointSetAMotorAxis"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetAMotorAxis(IntPtr j, int anum, int rel, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetAMotorMode"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetAMotorMode(IntPtr j, int mode);

		[DllImport("odesingle", EntryPoint = "dJointSetAMotorNumAxes"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetAMotorNumAxes(IntPtr group, int num);

		[DllImport("odesingle", EntryPoint = "dJointSetAMotorParam"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetAMotorParam(IntPtr group, int parameter, dReal value);

		[DllImport("odesingle", EntryPoint = "dJointSetBallAnchor"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetBallAnchor(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetBallAnchor2"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetBallAnchor2(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetData"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetData(IntPtr j, IntPtr data);

		[DllImport("odesingle", EntryPoint = "dJointSetFeedback"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetFeedback(IntPtr j, out JointFeedback feedback);

		[DllImport("odesingle", EntryPoint = "dJointSetFixed"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetFixed(IntPtr j);

		[DllImport("odesingle", EntryPoint = "dJointSetHingeAnchor"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetHingeAnchor(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetHingeAnchorDelta"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetHingeAnchorDelta(IntPtr j, dReal x, dReal y, dReal z, dReal ax, dReal ay, dReal az);

		[DllImport("odesingle", EntryPoint = "dJointSetHingeAxis"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetHingeAxis(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetHingeParam"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetHingeParam(IntPtr j, int parameter, dReal value);

		[DllImport("odesingle", EntryPoint = "dJointSetHinge2Anchor"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetHinge2Anchor(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetHinge2Axis1"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetHinge2Axis1(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetHinge2Axis2"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetHinge2Axis2(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetHinge2Param"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetHinge2Param(IntPtr j, int parameter, dReal value);

		[DllImport("odesingle", EntryPoint = "dJointSetLMotorAxis"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetLMotorAxis(IntPtr j, int anum, int rel, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetLMotorNumAxes"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetLMotorNumAxes(IntPtr j, int num);

		[DllImport("odesingle", EntryPoint = "dJointSetLMotorParam"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetLMotorParam(IntPtr j, int parameter, dReal value);

		[DllImport("odesingle", EntryPoint = "dJointSetPlane2DAngleParam"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetPlane2DAngleParam(IntPtr j, int parameter, dReal value);

		[DllImport("odesingle", EntryPoint = "dJointSetPlane2DXParam"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetPlane2DXParam(IntPtr j, int parameter, dReal value);

		[DllImport("odesingle", EntryPoint = "dJointSetPlane2DYParam"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetPlane2DYParam(IntPtr j, int parameter, dReal value);

		[DllImport("odesingle", EntryPoint = "dJointSetPRAnchor"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetPRAnchor(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetPRAxis1"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetPRAxis1(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetPRAxis2"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetPRAxis2(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetPRParam"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetPRParam(IntPtr j, int parameter, dReal value);

		[DllImport("odesingle", EntryPoint = "dJointSetSliderAxis"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetSliderAxis(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetSliderAxisDelta"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetSliderAxisDelta(IntPtr j, dReal x, dReal y, dReal z, dReal ax, dReal ay, dReal az);

		[DllImport("odesingle", EntryPoint = "dJointSetSliderParam"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetSliderParam(IntPtr j, int parameter, dReal value);

		[DllImport("odesingle", EntryPoint = "dJointSetUniversalAnchor"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetUniversalAnchor(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetUniversalAxis1"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetUniversalAxis1(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetUniversalAxis2"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetUniversalAxis2(IntPtr j, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dJointSetUniversalParam"), SuppressUnmanagedCodeSecurity]
		public static extern void JointSetUniversalParam(IntPtr j, int parameter, dReal value);

		[DllImport("odesingle", EntryPoint = "dLDLTAddTL"), SuppressUnmanagedCodeSecurity]
		public static extern void LDLTAddTL(ref dReal L, ref dReal d, ref dReal a, int n, int nskip);

		[DllImport("odesingle", EntryPoint = "dMassAdd"), SuppressUnmanagedCodeSecurity]
		public static extern void MassAdd(ref Mass a, ref Mass b);

		[DllImport("odesingle", EntryPoint = "dMassAdjust"), SuppressUnmanagedCodeSecurity]
		public static extern void MassAdjust(ref Mass m, dReal newmass);

		[DllImport("odesingle", EntryPoint = "dMassCheck"), SuppressUnmanagedCodeSecurity]
		public static extern bool MassCheck(ref Mass m);

		[DllImport("odesingle", EntryPoint = "dMassRotate"), SuppressUnmanagedCodeSecurity]
		public static extern void MassRotate(ref Mass mass, ref Matrix3 R);

		[DllImport("odesingle", EntryPoint = "dMassRotate"), SuppressUnmanagedCodeSecurity]
		public static extern void MassRotate(ref Mass mass, ref dReal M00);

		[DllImport("odesingle", EntryPoint = "dMassSetBox"), SuppressUnmanagedCodeSecurity]
		public static extern void MassSetBox(out Mass mass, dReal density, dReal lx, dReal ly, dReal lz);

		[DllImport("odesingle", EntryPoint = "dMassSetBoxTotal"), SuppressUnmanagedCodeSecurity]
		public static extern void MassSetBoxTotal(out Mass mass, dReal total_mass, dReal lx, dReal ly, dReal lz);

		[DllImport("odesingle", EntryPoint = "dMassSetCapsule"), SuppressUnmanagedCodeSecurity]
		public static extern void MassSetCapsule(out Mass mass, dReal density, int direction, dReal radius, dReal length);

		[DllImport("odesingle", EntryPoint = "dMassSetCapsuleTotal"), SuppressUnmanagedCodeSecurity]
		public static extern void MassSetCapsuleTotal(out Mass mass, dReal total_mass, int direction, dReal radius, dReal length);

		[DllImport("odesingle", EntryPoint = "dMassSetCylinder"), SuppressUnmanagedCodeSecurity]
		public static extern void MassSetCylinder(out Mass mass, dReal density, int direction, dReal radius, dReal length);

		[DllImport("odesingle", EntryPoint = "dMassSetCylinderTotal"), SuppressUnmanagedCodeSecurity]
		public static extern void MassSetCylinderTotal(out Mass mass, dReal total_mass, int direction, dReal radius, dReal length);

		[DllImport("odesingle", EntryPoint = "dMassSetParameters"), SuppressUnmanagedCodeSecurity]
		public static extern void MassSetParameters(out Mass mass, dReal themass,
			 dReal cgx, dReal cgy, dReal cgz,
			 dReal i11, dReal i22, dReal i33,
			 dReal i12, dReal i13, dReal i23);

		[DllImport("odesingle", EntryPoint = "dMassSetSphere"), SuppressUnmanagedCodeSecurity]
		public static extern void MassSetSphere(out Mass mass, dReal density, dReal radius);

		[DllImport("odesingle", EntryPoint = "dMassSetSphereTotal"), SuppressUnmanagedCodeSecurity]
		public static extern void dMassSetSphereTotal(out Mass mass, dReal total_mass, dReal radius);

		[DllImport("odesingle", EntryPoint = "dMassSetTrimesh"), SuppressUnmanagedCodeSecurity]
		public static extern void MassSetTrimesh(out Mass mass, dReal density, IntPtr g);

		[DllImport("odesingle", EntryPoint = "dMassSetZero"), SuppressUnmanagedCodeSecurity]
		public static extern void MassSetZero(out Mass mass);

		[DllImport("odesingle", EntryPoint = "dMassTranslate"), SuppressUnmanagedCodeSecurity]
		public static extern void MassTranslate(ref Mass mass, dReal x, dReal y, dReal z);

		[DllImport("odesingle", EntryPoint = "dMultiply0"), SuppressUnmanagedCodeSecurity]
		public static extern void Multiply0(out dReal A00, ref dReal B00, ref dReal C00, int p, int q, int r);

		[DllImport("odesingle", EntryPoint = "dMultiply1"), SuppressUnmanagedCodeSecurity]
		public static extern void Multiply1(out dReal A00, ref dReal B00, ref dReal C00, int p, int q, int r);

		[DllImport("odesingle", EntryPoint = "dMultiply2"), SuppressUnmanagedCodeSecurity]
		public static extern void Multiply2(out dReal A00, ref dReal B00, ref dReal C00, int p, int q, int r);

		[DllImport("odesingle", EntryPoint = "dQFromAxisAndAngle"), SuppressUnmanagedCodeSecurity]
		public static extern void QFromAxisAndAngle(out Quaternion q, dReal ax, dReal ay, dReal az, dReal angle);

		[DllImport("odesingle", EntryPoint = "dQfromR"), SuppressUnmanagedCodeSecurity]
		public static extern void QfromR(out Quaternion q, ref Matrix3 R);

		[DllImport("odesingle", EntryPoint = "dQMultiply0"), SuppressUnmanagedCodeSecurity]
		public static extern void QMultiply0(out Quaternion qa, ref Quaternion qb, ref Quaternion qc);

		[DllImport("odesingle", EntryPoint = "dQMultiply1"), SuppressUnmanagedCodeSecurity]
		public static extern void QMultiply1(out Quaternion qa, ref Quaternion qb, ref Quaternion qc);

		[DllImport("odesingle", EntryPoint = "dQMultiply2"), SuppressUnmanagedCodeSecurity]
		public static extern void QMultiply2(out Quaternion qa, ref Quaternion qb, ref Quaternion qc);

		[DllImport("odesingle", EntryPoint = "dQMultiply3"), SuppressUnmanagedCodeSecurity]
		public static extern void QMultiply3(out Quaternion qa, ref Quaternion qb, ref Quaternion qc);

		[DllImport("odesingle", EntryPoint = "dQSetIdentity"), SuppressUnmanagedCodeSecurity]
		public static extern void QSetIdentity(out Quaternion q);

		[DllImport("odesingle", EntryPoint = "dQuadTreeSpaceCreate"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr QuadTreeSpaceCreate(IntPtr space, ref Vector3 center, ref Vector3 extents, int depth);

		[DllImport("odesingle", EntryPoint = "dQuadTreeSpaceCreate"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr QuadTreeSpaceCreate(IntPtr space, ref dReal centerX, ref dReal extentsX, int depth);

		[DllImport("odesingle", EntryPoint = "dRandReal"), SuppressUnmanagedCodeSecurity]
		public static extern dReal RandReal();

		[DllImport("odesingle", EntryPoint = "dRFrom2Axes"), SuppressUnmanagedCodeSecurity]
		public static extern void RFrom2Axes(out Matrix3 R, dReal ax, dReal ay, dReal az, dReal bx, dReal by, dReal bz);

		[DllImport("odesingle", EntryPoint = "dRFromAxisAndAngle"), SuppressUnmanagedCodeSecurity]
		public static extern void RFromAxisAndAngle(out Matrix3 R, dReal x, dReal y, dReal z, dReal angle);

		[DllImport("odesingle", EntryPoint = "dRFromEulerAngles"), SuppressUnmanagedCodeSecurity]
		public static extern void RFromEulerAngles(out Matrix3 R, dReal phi, dReal theta, dReal psi);

		[DllImport("odesingle", EntryPoint = "dRfromQ"), SuppressUnmanagedCodeSecurity]
		public static extern void RfromQ(out Matrix3 R, ref Quaternion q);

		[DllImport("odesingle", EntryPoint = "dRFromZAxis"), SuppressUnmanagedCodeSecurity]
		public static extern void RFromZAxis(out Matrix3 R, dReal ax, dReal ay, dReal az);

		[DllImport("odesingle", EntryPoint = "dRSetIdentity"), SuppressUnmanagedCodeSecurity]
		public static extern void RSetIdentity(out Matrix3 R);

		[DllImport("odesingle", EntryPoint = "dSetValue"), SuppressUnmanagedCodeSecurity]
		public static extern void SetValue(out dReal a, int n);

		[DllImport("odesingle", EntryPoint = "dSetZero"), SuppressUnmanagedCodeSecurity]
		public static extern void SetZero(out dReal a, int n);

		[DllImport("odesingle", EntryPoint = "dSimpleSpaceCreate"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr SimpleSpaceCreate(IntPtr space);

		[DllImport("odesingle", EntryPoint = "dSolveCholesky"), SuppressUnmanagedCodeSecurity]
		public static extern void SolveCholesky(ref dReal L, out dReal b, int n);

		[DllImport("odesingle", EntryPoint = "dSolveL1"), SuppressUnmanagedCodeSecurity]
		public static extern void SolveL1(ref dReal L, out dReal b, int n, int nskip);

		[DllImport("odesingle", EntryPoint = "dSolveL1T"), SuppressUnmanagedCodeSecurity]
		public static extern void SolveL1T(ref dReal L, out dReal b, int n, int nskip);

		[DllImport("odesingle", EntryPoint = "dSolveLDLT"), SuppressUnmanagedCodeSecurity]
		public static extern void SolveLDLT(ref dReal L, ref dReal d, out dReal b, int n, int nskip);

		[DllImport("odesingle", EntryPoint = "dSpaceAdd"), SuppressUnmanagedCodeSecurity]
		public static extern void SpaceAdd(IntPtr space, IntPtr geom);

        [DllImport("odesingle", EntryPoint = "dSpaceLockQuery"), SuppressUnmanagedCodeSecurity]
        public static extern bool SpaceLockQuery(IntPtr space);

		[DllImport("odesingle", EntryPoint = "dSpaceClean"), SuppressUnmanagedCodeSecurity]
		public static extern void SpaceClean(IntPtr space);

		[DllImport("odesingle", EntryPoint = "dSpaceCollide"), SuppressUnmanagedCodeSecurity]
		public static extern void SpaceCollide(IntPtr space, IntPtr data, NearCallback callback);

		[DllImport("odesingle", EntryPoint = "dSpaceCollide2"), SuppressUnmanagedCodeSecurity]
		public static extern void SpaceCollide2(IntPtr space1, IntPtr space2, IntPtr data, NearCallback callback);

		[DllImport("odesingle", EntryPoint = "dSpaceDestroy"), SuppressUnmanagedCodeSecurity]
		public static extern void SpaceDestroy(IntPtr space);

		[DllImport("odesingle", EntryPoint = "dSpaceGetCleanup"), SuppressUnmanagedCodeSecurity]
		public static extern bool SpaceGetCleanup(IntPtr space);

		[DllImport("odesingle", EntryPoint = "dSpaceGetNumGeoms"), SuppressUnmanagedCodeSecurity]
		public static extern int SpaceGetNumGeoms(IntPtr space);

		[DllImport("odesingle", EntryPoint = "dSpaceGetGeom"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr SpaceGetGeom(IntPtr space, int i);

        [DllImport("odesingle", EntryPoint = "dSpaceGetSublevel"), SuppressUnmanagedCodeSecurity]
        public static extern int SpaceGetSublevel(IntPtr space);

		[DllImport("odesingle", EntryPoint = "dSpaceQuery"), SuppressUnmanagedCodeSecurity]
		public static extern bool SpaceQuery(IntPtr space, IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dSpaceRemove"), SuppressUnmanagedCodeSecurity]
		public static extern void SpaceRemove(IntPtr space, IntPtr geom);

		[DllImport("odesingle", EntryPoint = "dSpaceSetCleanup"), SuppressUnmanagedCodeSecurity]
		public static extern void SpaceSetCleanup(IntPtr space, bool mode);

        [DllImport("odesingle", EntryPoint = "dSpaceSetSublevel"), SuppressUnmanagedCodeSecurity]
        public static extern void SpaceSetSublevel(IntPtr space, int sublevel);

        [DllImport("odesingle", EntryPoint = "dSweepAndPruneSpaceCreate"), SuppressUnmanagedCodeSecurity]
        public static extern IntPtr SweepAndPruneSpaceCreate(IntPtr space, int AxisOrder);

		[DllImport("odesingle", EntryPoint = "dVectorScale"), SuppressUnmanagedCodeSecurity]
		public static extern void VectorScale(out dReal a, ref dReal d, int n);

		[DllImport("odesingle", EntryPoint = "dWorldCreate"), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr WorldCreate();

		[DllImport("odesingle", EntryPoint = "dWorldDestroy"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldDestroy(IntPtr world);

        [DllImport("odesingle", EntryPoint = "dWorldGetAutoDisableAverageSamplesCount"), SuppressUnmanagedCodeSecurity]
        public static extern int WorldGetAutoDisableAverageSamplesCount(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldGetAutoDisableAngularThreshold"), SuppressUnmanagedCodeSecurity]
		public static extern dReal WorldGetAutoDisableAngularThreshold(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldGetAutoDisableFlag"), SuppressUnmanagedCodeSecurity]
		public static extern bool WorldGetAutoDisableFlag(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldGetAutoDisableLinearThreshold"), SuppressUnmanagedCodeSecurity]
		public static extern dReal WorldGetAutoDisableLinearThreshold(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldGetAutoDisableSteps"), SuppressUnmanagedCodeSecurity]
		public static extern int WorldGetAutoDisableSteps(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldGetAutoDisableTime"), SuppressUnmanagedCodeSecurity]
		public static extern dReal WorldGetAutoDisableTime(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldGetAutoEnableDepthSF1"), SuppressUnmanagedCodeSecurity]
		public static extern int WorldGetAutoEnableDepthSF1(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldGetCFM"), SuppressUnmanagedCodeSecurity]
		public static extern dReal WorldGetCFM(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldGetERP"), SuppressUnmanagedCodeSecurity]
		public static extern dReal WorldGetERP(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldGetGravity"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldGetGravity(IntPtr world, out Vector3 gravity);

		[DllImport("odesingle", EntryPoint = "dWorldGetGravity"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldGetGravity(IntPtr world, out dReal X);

		[DllImport("odesingle", EntryPoint = "dWorldGetContactMaxCorrectingVel"), SuppressUnmanagedCodeSecurity]
		public static extern dReal WorldGetContactMaxCorrectingVel(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldGetContactSurfaceLayer"), SuppressUnmanagedCodeSecurity]
		public static extern dReal WorldGetContactSurfaceLayer(IntPtr world);

        [DllImport("odesingle", EntryPoint = "dWorldGetAngularDamping"), SuppressUnmanagedCodeSecurity]
        public static extern dReal WorldGetAngularDamping(IntPtr world);

        [DllImport("odesingle", EntryPoint = "dWorldGetAngularDampingThreshold"), SuppressUnmanagedCodeSecurity]
        public static extern dReal WorldGetAngularDampingThreshold(IntPtr world);

        [DllImport("odesingle", EntryPoint = "dWorldGetLinearDamping"), SuppressUnmanagedCodeSecurity]
        public static extern dReal WorldGetLinearDamping(IntPtr world);

        [DllImport("odesingle", EntryPoint = "dWorldGetLinearDampingThreshold"), SuppressUnmanagedCodeSecurity]
        public static extern dReal WorldGetLinearDampingThreshold(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldGetQuickStepNumIterations"), SuppressUnmanagedCodeSecurity]
		public static extern int WorldGetQuickStepNumIterations(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldGetQuickStepW"), SuppressUnmanagedCodeSecurity]
		public static extern dReal WorldGetQuickStepW(IntPtr world);

        [DllImport("odesingle", EntryPoint = "dWorldGetMaxAngularSpeed"), SuppressUnmanagedCodeSecurity]
        public static extern dReal WorldGetMaxAngularSpeed(IntPtr world);

		[DllImport("odesingle", EntryPoint = "dWorldImpulseToForce"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldImpulseToForce(IntPtr world, dReal stepsize, dReal ix, dReal iy, dReal iz, out Vector3 force);

		[DllImport("odesingle", EntryPoint = "dWorldImpulseToForce"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldImpulseToForce(IntPtr world, dReal stepsize, dReal ix, dReal iy, dReal iz, out dReal forceX);

		[DllImport("odesingle", EntryPoint = "dWorldQuickStep"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldQuickStep(IntPtr world, dReal stepsize);

        [DllImport("odesingle", EntryPoint = "dWorldSetAngularDamping"), SuppressUnmanagedCodeSecurity]
        public static extern void WorldSetAngularDamping(IntPtr world, dReal scale);

        [DllImport("odesingle", EntryPoint = "dWorldSetAngularDampingThreshold"), SuppressUnmanagedCodeSecurity]
        public static extern void WorldSetAngularDampingThreshold(IntPtr world, dReal threshold);

		[DllImport("odesingle", EntryPoint = "dWorldSetAutoDisableAngularThreshold"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetAutoDisableAngularThreshold(IntPtr world, dReal angular_threshold);

        [DllImport("odesingle", EntryPoint = "dWorldSetAutoDisableAverageSamplesCount"), SuppressUnmanagedCodeSecurity]
        public static extern void WorldSetAutoDisableAverageSamplesCount(IntPtr world, int average_samples_count);

		[DllImport("odesingle", EntryPoint = "dWorldSetAutoDisableFlag"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetAutoDisableFlag(IntPtr world, bool do_auto_disable);

		[DllImport("odesingle", EntryPoint = "dWorldSetAutoDisableLinearThreshold"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetAutoDisableLinearThreshold(IntPtr world, dReal linear_threshold);

		[DllImport("odesingle", EntryPoint = "dWorldSetAutoDisableSteps"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetAutoDisableSteps(IntPtr world, int steps);

		[DllImport("odesingle", EntryPoint = "dWorldSetAutoDisableTime"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetAutoDisableTime(IntPtr world, dReal time);

		[DllImport("odesingle", EntryPoint = "dWorldSetAutoEnableDepthSF1"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetAutoEnableDepthSF1(IntPtr world, int autoEnableDepth);

		[DllImport("odesingle", EntryPoint = "dWorldSetCFM"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetCFM(IntPtr world, dReal cfm);

		[DllImport("odesingle", EntryPoint = "dWorldSetContactMaxCorrectingVel"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetContactMaxCorrectingVel(IntPtr world, dReal vel);

		[DllImport("odesingle", EntryPoint = "dWorldSetContactSurfaceLayer"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetContactSurfaceLayer(IntPtr world, dReal depth);

        [DllImport("odesingle", EntryPoint = "dWorldSetDamping"), SuppressUnmanagedCodeSecurity]
        public static extern void WorldSetDamping(IntPtr world, dReal linear_scale, dReal angular_scale);

		[DllImport("odesingle", EntryPoint = "dWorldSetERP"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetERP(IntPtr world, dReal erp);

		[DllImport("odesingle", EntryPoint = "dWorldSetGravity"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetGravity(IntPtr world, dReal x, dReal y, dReal z);
        
        [DllImport("odesingle", EntryPoint = "dWorldSetLinearDamping"), SuppressUnmanagedCodeSecurity]
        public static extern void WorldSetLinearDamping(IntPtr world, dReal scale);

        [DllImport("odesingle", EntryPoint = "dWorldSetLinearDampingThreshold"), SuppressUnmanagedCodeSecurity]
        public static extern void WorldSetLinearDampingThreshold(IntPtr world, dReal threshold);

		[DllImport("odesingle", EntryPoint = "dWorldSetQuickStepNumIterations"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetQuickStepNumIterations(IntPtr world, int num);

		[DllImport("odesingle", EntryPoint = "dWorldSetQuickStepW"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldSetQuickStepW(IntPtr world, dReal over_relaxation);

        [DllImport("odesingle", EntryPoint = "dWorldSetMaxAngularSpeed"), SuppressUnmanagedCodeSecurity]
        public static extern void WorldSetMaxAngularSpeed(IntPtr world, dReal max_speed);

		[DllImport("odesingle", EntryPoint = "dWorldStep"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldStep(IntPtr world, dReal stepsize);

		[DllImport("odesingle", EntryPoint = "dWorldStepFast1"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldStepFast1(IntPtr world, dReal stepsize, int maxiterations);

		[DllImport("odesingle", EntryPoint = "dWorldExportDIF"), SuppressUnmanagedCodeSecurity]
		public static extern void WorldExportDIF(IntPtr world, string filename, bool append, string prefix);


	}
}
