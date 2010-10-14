/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
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

/* Revised Aug, Sept 2009 by Kitto Flora. ODEDynamics.cs replaces
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
using System.Reflection;
using System.Runtime.InteropServices;
using log4net;
using OpenMetaverse;
using Ode.NET;
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;

namespace Aurora.Physics.AuroraOpenDynamicsEngine
{
    public class AuroraODEDynamics
    {
        public Vehicle Type
        {
            get { return m_type; }
        }

        public IntPtr Body
        {
            get { return m_body; }
        }

        private int frcount = 0;                                        // Used to limit dynamics debug output to
                                                                        // every 100th frame

        // private OdeScene m_parentScene = null;
        private IntPtr m_body = IntPtr.Zero;
//        private IntPtr m_jointGroup = IntPtr.Zero;
//        private IntPtr m_aMotor = IntPtr.Zero;


        // Vehicle properties
        private Vehicle m_type = Vehicle.TYPE_NONE;                     // If a 'VEHICLE', and what kind
        private Quaternion m_referenceFrame = Quaternion.Identity;   // Axis modifier
        private VehicleFlag m_flags = (VehicleFlag) 0;                  // Boolean settings:
                                                                        // HOVER_TERRAIN_ONLY
                                                                        // HOVER_GLOBAL_HEIGHT
                                                                        // NO_DEFLECTION_UP
                                                                        // HOVER_WATER_ONLY
                                                                        // HOVER_UP_ONLY
                                                                        // LIMIT_MOTOR_UP
                                                                        // LIMIT_ROLL_ONLY
        private VehicleFlag m_Hoverflags = (VehicleFlag)0;
        private Vector3 m_BlockingEndPoint = Vector3.Zero;
        private Quaternion m_RollreferenceFrame = Quaternion.Identity;
        // Linear properties
        private Vector3 m_linearMotorDirection = Vector3.Zero;          // velocity requested by LSL, decayed by time
        private Vector3 m_linearMotorDirectionLASTSET = Vector3.Zero;   // velocity requested by LSL
        private Vector3 m_dir = Vector3.Zero;                           // velocity applied to body
        private Vector3 m_linearFrictionTimescale = Vector3.Zero;
        private float m_linearMotorDecayTimescale = 0;
        private float m_linearMotorTimescale = 0;
        private Vector3 m_lastLinearVelocityVector = Vector3.Zero;
        private d.Vector3 m_lastPositionVector = new d.Vector3();
        //private bool m_LinearMotorSetLastFrame = false;
        private Vector3 m_linearMotorOffset = Vector3.Zero;

        //Angular properties
        private Vector3 m_angularMotorDirection = Vector3.Zero;         // angular velocity requested by LSL motor
        private int m_angularMotorApply = 0;                            // application frame counter
        private Vector3 m_angularMotorVelocity = Vector3.Zero;          // current angular motor velocity
        private float m_angularMotorTimescale = 0;                      // motor angular velocity ramp up rate
        private float m_angularMotorDecayTimescale = 0;                 // motor angular velocity decay rate
        private Vector3 m_angularFrictionTimescale = Vector3.Zero;      // body angular velocity  decay rate
        private Vector3 m_lastAngularVelocity = Vector3.Zero;           // what was last applied to body
 //       private Vector3 m_lastVertAttractor = Vector3.Zero;             // what VA was last applied to body

        //Deflection properties
        private float m_angularDeflectionEfficiency = 0;
        private float m_angularDeflectionTimescale = 0;
        private float m_linearDeflectionEfficiency = 0;
        private float m_linearDeflectionTimescale = 0;

        //Banking properties
        private float m_bankingEfficiency = 0;
        private float m_bankingMix = 0;
        private float m_bankingTimescale = 0;

        //Hover and Buoyancy properties
        private float m_VhoverHeight = 0f;
        private float m_VhoverEfficiency = 0f;
        private float m_VhoverTimescale = 0f;
        private float m_VhoverTargetHeight = -1.0f;     // if <0 then no hover, else its the current target height
        private float m_VehicleBuoyancy = 0f;           //KF: m_VehicleBuoyancy is set by VEHICLE_BUOYANCY for a vehicle.
                    // Modifies gravity. Slider between -1 (double-gravity) and 1 (full anti-gravity)
                    // KF: So far I have found no good method to combine a script-requested .Z velocity and gravity.
                    // Therefore only m_VehicleBuoyancy=1 (0g) will use the script-requested .Z velocity.

        //Attractor properties
        private float m_verticalAttractionEfficiency = 1.0f;        // damped
        private float m_verticalAttractionTimescale = 500f;         // Timescale > 300  means no vert attractor.
        public float Mass;
        private bool m_enabled = false;

        internal void ProcessFloatVehicleParam(Vehicle pParam, float pValue)
        {
            switch (pParam)
            {
                case Vehicle.ANGULAR_DEFLECTION_EFFICIENCY:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_angularDeflectionEfficiency = pValue;
                    break;
                case Vehicle.ANGULAR_DEFLECTION_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_angularDeflectionTimescale = pValue;
                    break;
                case Vehicle.ANGULAR_MOTOR_DECAY_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_angularMotorDecayTimescale = pValue;
                    break;
                case Vehicle.ANGULAR_MOTOR_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_angularMotorTimescale = pValue;
                    break;
                case Vehicle.BANKING_EFFICIENCY:
                    if (pValue < -1f) pValue = -1f;
                    if (pValue > 1f) pValue = 1f;
                    m_bankingEfficiency = pValue;
                    break;
                case Vehicle.BANKING_MIX:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_bankingMix = pValue;
                    break;
                case Vehicle.BANKING_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_bankingTimescale = pValue;
                    break;
                case Vehicle.BUOYANCY:
                    if (pValue < -1f) pValue = -1f;
                    if (pValue > 1f) pValue = 1f;
                    m_VehicleBuoyancy = pValue;
                    break;
                case Vehicle.HOVER_EFFICIENCY:
                    if (pValue < 0f) pValue = 0f;
                    if (pValue > 1f) pValue = 1f;
                    m_VhoverEfficiency = pValue;
                    break;
                case Vehicle.HOVER_HEIGHT:
                    m_VhoverHeight = pValue;
                    break;
                case Vehicle.HOVER_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_VhoverTimescale = pValue;
                    break;
                case Vehicle.LINEAR_DEFLECTION_EFFICIENCY:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_linearDeflectionEfficiency = pValue;
                    break;
                case Vehicle.LINEAR_DEFLECTION_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_linearDeflectionTimescale = pValue;
                    break;
                case Vehicle.LINEAR_MOTOR_DECAY_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_linearMotorDecayTimescale = pValue;
                    break;
                case Vehicle.LINEAR_MOTOR_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_linearMotorTimescale = pValue;
                    break;
                case Vehicle.VERTICAL_ATTRACTION_EFFICIENCY:
                    if (pValue < 0.1f) pValue = 0.1f;    // Less goes unstable
                    if (pValue > 1.0f) pValue = 1.0f;
                    m_verticalAttractionEfficiency = pValue;
                    break;
                case Vehicle.VERTICAL_ATTRACTION_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_verticalAttractionTimescale = pValue;
                    break;

                // These are vector properties but the engine lets you use a single float value to
                // set all of the components to the same value
                case Vehicle.ANGULAR_FRICTION_TIMESCALE:
                    m_angularFrictionTimescale = new Vector3(pValue, pValue, pValue);
                    break;
                case Vehicle.ANGULAR_MOTOR_DIRECTION:
                    m_angularMotorDirection = new Vector3(pValue, pValue, pValue);
                    m_angularMotorApply = 10;
                    break;
                case Vehicle.LINEAR_FRICTION_TIMESCALE:
                    m_linearFrictionTimescale = new Vector3(pValue, pValue, pValue);
                    break;
                case Vehicle.LINEAR_MOTOR_DIRECTION:
                    m_linearMotorDirection = new Vector3(pValue, pValue, pValue);
                    m_linearMotorDirectionLASTSET = new Vector3(pValue, pValue, pValue);
                    break;
                case Vehicle.LINEAR_MOTOR_OFFSET:
                    m_linearMotorOffset = new Vector3(pValue, pValue, pValue);
                    break;

            }
        }//end ProcessFloatVehicleParam

        //All parts hooked up
        internal void ProcessVectorVehicleParam(Vehicle pParam, Vector3 pValue)
        {
            switch (pParam)
            {
                case Vehicle.ANGULAR_FRICTION_TIMESCALE:
                    m_angularFrictionTimescale = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.ANGULAR_MOTOR_DIRECTION:
                    m_angularMotorDirection = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    // Limit requested angular speed to 2 rps= 4 pi rads/sec
                    if (m_angularMotorDirection.X > 12.56f) m_angularMotorDirection.X = 12.56f;
                    if (m_angularMotorDirection.X < - 12.56f) m_angularMotorDirection.X = - 12.56f;
                    if (m_angularMotorDirection.Y > 12.56f) m_angularMotorDirection.Y = 12.56f;
                    if (m_angularMotorDirection.Y < - 12.56f) m_angularMotorDirection.Y = - 12.56f;
                    if (m_angularMotorDirection.Z > 12.56f) m_angularMotorDirection.Z = 12.56f;
                    if (m_angularMotorDirection.Z < - 12.56f) m_angularMotorDirection.Z = - 12.56f;
                    m_angularMotorApply = 10;
                    break;
                case Vehicle.LINEAR_FRICTION_TIMESCALE:
                    m_linearFrictionTimescale = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.LINEAR_MOTOR_DIRECTION:
                    m_linearMotorDirection = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    m_linearMotorDirectionLASTSET = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.LINEAR_MOTOR_OFFSET:
                    m_linearMotorOffset = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.BLOCK_EXIT:
                    m_BlockingEndPoint = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
            }
        }//end ProcessVectorVehicleParam

        //All parts hooked up
        internal void ProcessRotationVehicleParam(Vehicle pParam, Quaternion pValue)
        {
            switch (pParam)
            {
                case Vehicle.REFERENCE_FRAME:
                    m_referenceFrame = pValue;
                    break;
                case Vehicle.ROLL_FRAME:
                    m_RollreferenceFrame = pValue;
                    break;
            }
        }//end ProcessRotationVehicleParam

        internal void ProcessVehicleFlags(int pParam, bool remove)
        {
            if (remove)
            {
                if (pParam == -1)
                {
                    m_flags = (VehicleFlag)0;
                    m_Hoverflags = (VehicleFlag)0;
                    return;
                }
                if ((pParam & (int)VehicleFlag.HOVER_GLOBAL_HEIGHT) == (int)VehicleFlag.HOVER_GLOBAL_HEIGHT)
                {
                    if ((m_Hoverflags & VehicleFlag.HOVER_GLOBAL_HEIGHT) != (VehicleFlag)0)
                        m_Hoverflags &= ~(VehicleFlag.HOVER_GLOBAL_HEIGHT);
                }
                if ((pParam & (int)VehicleFlag.HOVER_TERRAIN_ONLY) == (int)VehicleFlag.HOVER_TERRAIN_ONLY)
                {
                    if ((m_Hoverflags & VehicleFlag.HOVER_TERRAIN_ONLY) != (VehicleFlag)0)
                        m_Hoverflags &= ~(VehicleFlag.HOVER_TERRAIN_ONLY);
                }
                if ((pParam & (int)VehicleFlag.HOVER_UP_ONLY) == (int)VehicleFlag.HOVER_UP_ONLY)
                {
                    if ((m_Hoverflags & VehicleFlag.HOVER_UP_ONLY) != (VehicleFlag)0)
                        m_Hoverflags &= ~(VehicleFlag.HOVER_UP_ONLY);
                }
                if ((pParam & (int)VehicleFlag.HOVER_WATER_ONLY) == (int)VehicleFlag.HOVER_WATER_ONLY)
                {
                    if ((m_Hoverflags & VehicleFlag.HOVER_WATER_ONLY) != (VehicleFlag)0)
                        m_Hoverflags &= ~(VehicleFlag.HOVER_WATER_ONLY);
                }
                if ((pParam & (int)VehicleFlag.LIMIT_MOTOR_UP) == (int)VehicleFlag.LIMIT_MOTOR_UP)
                {
                    if ((m_flags & VehicleFlag.LIMIT_MOTOR_UP) != (VehicleFlag)0)
                        m_flags &= ~(VehicleFlag.LIMIT_MOTOR_UP);
                }
                if ((pParam & (int)VehicleFlag.LIMIT_ROLL_ONLY) == (int)VehicleFlag.LIMIT_ROLL_ONLY)
                {
                    if ((m_flags & VehicleFlag.LIMIT_ROLL_ONLY) != (VehicleFlag)0)
                        m_flags &= ~(VehicleFlag.LIMIT_ROLL_ONLY);
                }
                if ((pParam & (int)VehicleFlag.MOUSELOOK_BANK) == (int)VehicleFlag.MOUSELOOK_BANK)
                {
                    if ((m_flags & VehicleFlag.MOUSELOOK_BANK) != (VehicleFlag)0)
                        m_flags &= ~(VehicleFlag.MOUSELOOK_BANK);
                }
                if ((pParam & (int)VehicleFlag.MOUSELOOK_STEER) == (int)VehicleFlag.MOUSELOOK_STEER)
                {
                    if ((m_flags & VehicleFlag.MOUSELOOK_STEER) != (VehicleFlag)0)
                        m_flags &= ~(VehicleFlag.MOUSELOOK_STEER);
                }
                if ((pParam & (int)VehicleFlag.NO_DEFLECTION_UP) == (int)VehicleFlag.NO_DEFLECTION_UP)
                {
                    if ((m_flags & VehicleFlag.NO_DEFLECTION_UP) != (VehicleFlag)0)
                        m_flags &= ~(VehicleFlag.NO_DEFLECTION_UP);
                }
                if ((pParam & (int)VehicleFlag.CAMERA_DECOUPLED) == (int)VehicleFlag.CAMERA_DECOUPLED)
                {
                    if ((m_flags & VehicleFlag.CAMERA_DECOUPLED) != (VehicleFlag)0)
                        m_flags &= ~(VehicleFlag.CAMERA_DECOUPLED);
                }
                if ((pParam & (int)VehicleFlag.NO_X) == (int)VehicleFlag.NO_X)
                {
                    if ((m_flags & VehicleFlag.NO_X) != (VehicleFlag)0)
                        m_flags &= ~(VehicleFlag.NO_X);
                }
                if ((pParam & (int)VehicleFlag.NO_Y) == (int)VehicleFlag.NO_Y)
                {
                    if ((m_flags & VehicleFlag.NO_Y) != (VehicleFlag)0)
                        m_flags &= ~(VehicleFlag.NO_Y);
                }
                if ((pParam & (int)VehicleFlag.NO_Z) == (int)VehicleFlag.NO_Z)
                {
                    if ((m_flags & VehicleFlag.NO_Z) != (VehicleFlag)0)
                        m_flags &= ~(VehicleFlag.NO_Z);
                }
                if ((pParam & (int)VehicleFlag.LOCK_HOVER_HEIGHT) == (int)VehicleFlag.LOCK_HOVER_HEIGHT)
                {
                    if ((m_Hoverflags & VehicleFlag.LOCK_HOVER_HEIGHT) != (VehicleFlag)0)
                        m_Hoverflags &= ~(VehicleFlag.LOCK_HOVER_HEIGHT);
                }
                if ((pParam & (int)VehicleFlag.NO_DEFLECTION) == (int)VehicleFlag.NO_DEFLECTION)
                {
                    if ((m_flags & VehicleFlag.NO_DEFLECTION) != (VehicleFlag)0)
                        m_flags &= ~(VehicleFlag.NO_DEFLECTION);
                }
                if ((pParam & (int)VehicleFlag.LOCK_ROTATION) == (int)VehicleFlag.LOCK_ROTATION)
                {
                    if ((m_flags & VehicleFlag.LOCK_ROTATION) != (VehicleFlag)0)
                        m_flags &= ~(VehicleFlag.LOCK_ROTATION);
                }
            }
            else
            {
                if ((pParam & (int)VehicleFlag.HOVER_GLOBAL_HEIGHT) == (int)VehicleFlag.HOVER_GLOBAL_HEIGHT)
                {
                    m_Hoverflags |= (VehicleFlag.HOVER_GLOBAL_HEIGHT | m_flags);
                }
                if ((pParam & (int)VehicleFlag.HOVER_TERRAIN_ONLY) == (int)VehicleFlag.HOVER_TERRAIN_ONLY)
                {
                    m_Hoverflags |= (VehicleFlag.HOVER_TERRAIN_ONLY | m_flags);
                }
                if ((pParam & (int)VehicleFlag.HOVER_UP_ONLY) == (int)VehicleFlag.HOVER_UP_ONLY)
                {
                    m_Hoverflags |= (VehicleFlag.HOVER_UP_ONLY | m_flags);
                }
                if ((pParam & (int)VehicleFlag.HOVER_WATER_ONLY) == (int)VehicleFlag.HOVER_WATER_ONLY)
                {
                    m_Hoverflags |= (VehicleFlag.HOVER_WATER_ONLY | m_flags);
                }
                if ((pParam & (int)VehicleFlag.LIMIT_MOTOR_UP) == (int)VehicleFlag.LIMIT_MOTOR_UP)
                {
                    m_flags |= (VehicleFlag.LIMIT_MOTOR_UP | m_flags);
                }
                if ((pParam & (int)VehicleFlag.MOUSELOOK_BANK) == (int)VehicleFlag.MOUSELOOK_BANK)
                {
                    m_flags |= (VehicleFlag.MOUSELOOK_BANK | m_flags);
                }
                if ((pParam & (int)VehicleFlag.MOUSELOOK_STEER) == (int)VehicleFlag.MOUSELOOK_STEER)
                {
                    m_flags |= (VehicleFlag.MOUSELOOK_STEER | m_flags);
                }
                if ((pParam & (int)VehicleFlag.NO_DEFLECTION_UP) == (int)VehicleFlag.NO_DEFLECTION_UP)
                {
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | m_flags);
                }
                if ((pParam & (int)VehicleFlag.CAMERA_DECOUPLED) == (int)VehicleFlag.CAMERA_DECOUPLED)
                {
                    m_flags |= (VehicleFlag.CAMERA_DECOUPLED | m_flags);
                }
                if ((pParam & (int)VehicleFlag.NO_X) == (int)VehicleFlag.NO_X)
                {
                    m_flags |= (VehicleFlag.NO_X);
                }
                if ((pParam & (int)VehicleFlag.NO_Y) == (int)VehicleFlag.NO_Y)
                {
                    m_flags |= (VehicleFlag.NO_Y);
                }
                if ((pParam & (int)VehicleFlag.NO_Z) == (int)VehicleFlag.NO_Z)
                {
                    m_flags |= (VehicleFlag.NO_Z);
                }
                if ((pParam & (int)VehicleFlag.LOCK_HOVER_HEIGHT) == (int)VehicleFlag.LOCK_HOVER_HEIGHT)
                {
                    m_Hoverflags |= (VehicleFlag.LOCK_HOVER_HEIGHT);
                }
                if ((pParam & (int)VehicleFlag.NO_DEFLECTION) == (int)VehicleFlag.NO_DEFLECTION)
                {
                    m_flags |= (VehicleFlag.NO_DEFLECTION);
                }
                if ((pParam & (int)VehicleFlag.LOCK_ROTATION) == (int)VehicleFlag.LOCK_ROTATION)
                {
                    m_flags |= (VehicleFlag.LOCK_ROTATION);
                }
            }
        }//end ProcessVehicleFlags

        internal void ProcessTypeChange(Vehicle pType)
        {
            // Set Defaults For Type
            m_type = pType;
            switch (pType)
            {
                    case Vehicle.TYPE_NONE:
                    m_linearFrictionTimescale = new Vector3(0, 0, 0);
                    m_angularFrictionTimescale = new Vector3(0, 0, 0);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 0;
                    m_linearMotorDecayTimescale = 0;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 0;
                    m_angularMotorDecayTimescale = 0;
                    m_VhoverHeight = 0;
                    m_VhoverTimescale = 0;
                    m_VehicleBuoyancy = 0;
                    m_flags = (VehicleFlag)0;
                    m_referenceFrame = Quaternion.Identity;
                    break;

                case Vehicle.TYPE_SLED:
                    m_linearFrictionTimescale = new Vector3(30, 1, 1000);
                    m_angularFrictionTimescale = new Vector3(1000, 1000, 1000);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 1000;
                    m_linearMotorDecayTimescale = 120;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 1000;
                    m_angularMotorDecayTimescale = 120;
                    m_VhoverHeight = 0;
                    m_VhoverEfficiency = 1;
                    m_VhoverTimescale = 10;
                    m_VehicleBuoyancy = 0;
                    m_linearDeflectionEfficiency = 1;
                    m_linearDeflectionTimescale = 1;
                    m_angularDeflectionEfficiency = 1;
                    m_angularDeflectionTimescale = 1000;
                    m_bankingEfficiency = 0;
                    m_bankingMix = 1;
                    m_bankingTimescale = 10;
                    m_referenceFrame = Quaternion.Identity;
                    m_Hoverflags &=
                         ~(VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                           VehicleFlag.HOVER_GLOBAL_HEIGHT | VehicleFlag.HOVER_UP_ONLY);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_ROLL_ONLY | VehicleFlag.LIMIT_MOTOR_UP);
                    break;
                case Vehicle.TYPE_CAR:
                    m_linearFrictionTimescale = new Vector3(100, 2, 1000);
                    m_angularFrictionTimescale = new Vector3(1000, 1000, 1000);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 1;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 1;
                    m_angularMotorDecayTimescale = 0.8f;
                    m_VhoverHeight = 0;
                    m_VhoverEfficiency = 0;
                    m_VhoverTimescale = 1000;
                    m_VehicleBuoyancy = 0;
                    m_linearDeflectionEfficiency = 1;
                    m_linearDeflectionTimescale = 2;
                    m_angularDeflectionEfficiency = 0;
                    m_angularDeflectionTimescale = 10;
                    m_verticalAttractionEfficiency = 1f;
                    m_verticalAttractionTimescale = 10f;
                    m_bankingEfficiency = -0.2f;
                    m_bankingMix = 1;
                    m_bankingTimescale = 1;
                    m_referenceFrame = Quaternion.Identity;
                    m_Hoverflags &= ~(VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY | VehicleFlag.HOVER_GLOBAL_HEIGHT);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_ROLL_ONLY |
                                VehicleFlag.LIMIT_MOTOR_UP);
                    m_Hoverflags |= (VehicleFlag.HOVER_UP_ONLY);
                    break;
                case Vehicle.TYPE_BOAT:
                    m_linearFrictionTimescale = new Vector3(10, 3, 2);
                    m_angularFrictionTimescale = new Vector3(10,10,10);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 5;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 4;
                    m_angularMotorDecayTimescale = 4;
                    m_VhoverHeight = 0;
                    m_VhoverEfficiency = 0.5f;
                    m_VhoverTimescale = 2;
                    m_VehicleBuoyancy = 1;
                    m_linearDeflectionEfficiency = 0.5f;
                    m_linearDeflectionTimescale = 3;
                    m_angularDeflectionEfficiency = 0.5f;
                    m_angularDeflectionTimescale = 5;
                    m_verticalAttractionEfficiency = 0.5f;
                    m_verticalAttractionTimescale = 5f;
                    m_bankingEfficiency = -0.3f;
                    m_bankingMix = 0.8f;
                    m_bankingTimescale = 1;
                    m_referenceFrame = Quaternion.Identity;
                    m_Hoverflags &= ~(VehicleFlag.HOVER_TERRAIN_ONLY |
                            VehicleFlag.HOVER_GLOBAL_HEIGHT | VehicleFlag.HOVER_UP_ONLY);
                    m_flags &= ~(VehicleFlag.LIMIT_ROLL_ONLY);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP |
                                VehicleFlag.LIMIT_MOTOR_UP);
                    m_Hoverflags |= (VehicleFlag.HOVER_WATER_ONLY);
                    break;
                case Vehicle.TYPE_AIRPLANE:
                    m_linearFrictionTimescale = new Vector3(200, 10, 5);
                    m_angularFrictionTimescale = new Vector3(20, 20, 20);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 2;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 4;
                    m_angularMotorDecayTimescale = 4;
                    m_VhoverHeight = 0;
                    m_VhoverEfficiency = 0.5f;
                    m_VhoverTimescale = 1000;
                    m_VehicleBuoyancy = 0;
                    m_linearDeflectionEfficiency = 0.5f;
                    m_linearDeflectionTimescale = 3;
                    m_angularDeflectionEfficiency = 1;
                    m_angularDeflectionTimescale = 2;
                    m_verticalAttractionEfficiency = 0.9f;
                    m_verticalAttractionTimescale = 2f;
                    m_bankingEfficiency = 1;
                    m_bankingMix = 0.7f;
                    m_bankingTimescale = 2;
                    m_referenceFrame = Quaternion.Identity;
                    m_Hoverflags &= ~(VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                        VehicleFlag.HOVER_GLOBAL_HEIGHT | VehicleFlag.HOVER_UP_ONLY);
                    m_flags &= ~(VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_MOTOR_UP);
                    m_flags |= (VehicleFlag.LIMIT_ROLL_ONLY);
                    break;
                case Vehicle.TYPE_BALLOON:
                    m_linearFrictionTimescale = new Vector3(5, 5, 5);
                    m_angularFrictionTimescale = new Vector3(10, 10, 10);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 5;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 6;
                    m_angularMotorDecayTimescale = 10;
                    m_VhoverHeight = 5;
                    m_VhoverEfficiency = 0.8f;
                    m_VhoverTimescale = 10;
                    m_VehicleBuoyancy = 1;
                    m_linearDeflectionEfficiency = 0;
                    m_linearDeflectionTimescale = 5;
                    m_angularDeflectionEfficiency = 0;
                    m_angularDeflectionTimescale = 5;
                    m_verticalAttractionEfficiency = 1f;
                    m_verticalAttractionTimescale = 100f;
                    m_bankingEfficiency = 0;
                    m_bankingMix = 0.7f;
                    m_bankingTimescale = 5;
                    m_referenceFrame = Quaternion.Identity;
                    m_Hoverflags &= ~(VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                        VehicleFlag.HOVER_UP_ONLY);
                    m_flags &= ~(VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_MOTOR_UP);
                    m_flags |= (VehicleFlag.LIMIT_ROLL_ONLY);
                    m_Hoverflags |= (VehicleFlag.HOVER_GLOBAL_HEIGHT);
                    break;
            }
        }//end SetDefaultsForType

        internal void Enable(IntPtr pBody, AuroraODEPrim parent, AuroraODEPhysicsScene pParentScene)
        {
            if (m_enabled)
                return;
            parent.ThrottleUpdates = false;
            m_body = pBody;
            if (pBody == IntPtr.Zero || m_type == Vehicle.TYPE_NONE)
                return;

            d.Mass mass;
            d.BodyGetMass(pBody, out mass);
            
            Mass = mass.mass;
            Mass *= 2;
        }

        internal void Disable(AuroraODEPrim parent)
        {
            if (!m_enabled)
                return;
            parent.ThrottleUpdates = true;
            //d.BodyDisable(Body);
            m_linearMotorDirection = Vector3.Zero;
            m_linearMotorDirectionLASTSET = Vector3.Zero;
            m_angularMotorDirection = Vector3.Zero;
        }

        internal void Step(IntPtr pBody, float pTimestep, AuroraODEPhysicsScene pParentScene, AuroraODEPrim parent)
        {
            m_body = pBody;
            if (pBody == IntPtr.Zero || m_type == Vehicle.TYPE_NONE)
                return;
            if (!d.BodyIsEnabled(Body))
                d.BodyEnable(Body);

            frcount++;  // used to limit debug comment output
            if (frcount > 100)
                frcount = 0;

            MoveLinear(pTimestep, pParentScene);
            MoveAngular(pTimestep, pParentScene);
            LimitRotation(pTimestep);
            
            // WE deal with updates
            parent.RequestPhysicsterseUpdate();

        }   // end Step

        private void MoveLinear(float pTimestep, AuroraODEPhysicsScene _pParentScene)
        {
            d.Vector3 pos = d.BodyGetPosition(Body);
            d.Vector3 oldPos = pos;
            m_lastPositionVector = d.BodyGetPosition(Body);

            if (m_lastPositionVector.X != pos.X ||
                m_lastPositionVector.Y != pos.Y ||
                m_lastPositionVector.Z != pos.Z)
            {
                m_lastPositionVector = d.BodyGetPosition(Body);
                m_lastAngularVelocity = new Vector3(d.BodyGetAngularVel(Body).X,d.BodyGetAngularVel(Body).Y, d.BodyGetAngularVel(Body).Z);
            }

            if (!m_linearMotorDirection.ApproxEquals(Vector3.Zero, 0.01f))  // requested m_linearMotorDirection is significant
            {
                // add drive to body
                Vector3 addAmount = (m_linearMotorDirection / (m_linearMotorTimescale / (pTimestep * pTimestep * 5)));
                addAmount.Z = (m_linearMotorDirection.Z / (m_linearMotorTimescale / (pTimestep * pTimestep)));//^ Z appears to be differently handled in SL? Go figure...
                m_lastLinearVelocityVector += addAmount;

                // This will work temporarily, but we really need to compare speed on an axis
                // KF: Limit body velocity to applied velocity?
                if (Math.Abs(m_lastLinearVelocityVector.X) > Math.Abs(m_linearMotorDirectionLASTSET.X))
                    m_lastLinearVelocityVector.X = m_linearMotorDirectionLASTSET.X;
                if (Math.Abs(m_lastLinearVelocityVector.Y) > Math.Abs(m_linearMotorDirectionLASTSET.Y))
                    m_lastLinearVelocityVector.Y = m_linearMotorDirectionLASTSET.Y;
                if (Math.Abs(m_lastLinearVelocityVector.Z) > Math.Abs(m_linearMotorDirectionLASTSET.Z))
                    m_lastLinearVelocityVector.Z = m_linearMotorDirectionLASTSET.Z;
            }
            else
            {        // requested is not significant
                    // if what remains of applied is small, zero it.
                if (m_lastLinearVelocityVector.ApproxEquals(Vector3.Zero, 0.01f))
                    m_lastLinearVelocityVector = Vector3.Zero;
            }
            m_linearMotorDirection = Vector3.Zero;

            // convert requested object velocity to world-referenced vector
            m_dir = m_lastLinearVelocityVector;

            d.Quaternion rot = d.BodyGetQuaternion(Body);
            Quaternion rotq = new Quaternion(rot.X,
                rot.Y,
                rot.Z,
                rot.W);    // rotq = rotation of object

            m_dir *= rotq;   // apply obj rotation to velocity vector

            // Preserve the current Z velocity
            d.Vector3 vel_now = d.BodyGetLinearVel(Body);
            m_dir.Z += vel_now.Z;        // Preserve the accumulated falling velocity

            #region Blocking End Points

            //This makes sure that the vehicle doesn't leave the defined limits of position
            if (m_BlockingEndPoint != Vector3.Zero)
            {
                Vector3 posChange = new Vector3();
                posChange.X = pos.X - m_lastPositionVector.X;
                posChange.Y = pos.Y - m_lastPositionVector.Y;
                posChange.Z = pos.Z - m_lastPositionVector.Z;

                if (pos.X >= (m_BlockingEndPoint.X - (float)1))
                    pos.X -= posChange.X + 1;

                if (pos.Y >= (m_BlockingEndPoint.Y - (float)1))
                    pos.Y -= posChange.Y + 1;

                if (pos.Z >= (m_BlockingEndPoint.Z - (float)1))
                    pos.Z -= posChange.Z + 1;

                if (pos.X <= 0)
                    pos.X += posChange.X + 1;

                if (pos.Y <= 0)
                    pos.Y += posChange.Y + 1;
            }

            #endregion

            #region Hover

            // Check if hovering
            if ((m_Hoverflags & (VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY | VehicleFlag.HOVER_GLOBAL_HEIGHT)) != 0)
            {
                // We should hover, get the target height
                if ((m_Hoverflags & VehicleFlag.HOVER_WATER_ONLY) != 0)
                {
                    m_VhoverTargetHeight = _pParentScene.GetWaterLevel() + m_VhoverHeight;
                }
                if ((m_Hoverflags & VehicleFlag.HOVER_TERRAIN_ONLY) != 0)
                {
                    m_VhoverTargetHeight = _pParentScene.GetTerrainHeightAtXY(pos.X, pos.Y) + m_VhoverHeight;
                }
                if ((m_Hoverflags & VehicleFlag.HOVER_GLOBAL_HEIGHT) != 0)
                {
                    m_VhoverTargetHeight = m_VhoverHeight;
                }

                if ((m_Hoverflags & VehicleFlag.HOVER_UP_ONLY) != 0)
                {
                    // If body is already heigher, use its height as target height
                    if (pos.Z > m_VhoverTargetHeight)
                        m_VhoverTargetHeight = pos.Z;
                }

                if ((m_Hoverflags & VehicleFlag.LOCK_HOVER_HEIGHT) != 0)
                {
                    if ((pos.Z - m_VhoverTargetHeight) > .2 || (pos.Z - m_VhoverTargetHeight) < -.2)
                    {
                        if ((pos.Z - (pos.Z - m_VhoverTargetHeight)) >= _pParentScene.GetTerrainHeightAtXY(pos.X, pos.Y))
                            pos.Z = m_VhoverTargetHeight;
                    }
                }
                else
                {
                    // m_VhoverEfficiency - 0=boucy, 1=Crit.damped
                    // m_VhoverTimescale - time to acheive height
                    float herr0 = pos.Z - m_VhoverTargetHeight;
                    // Replace Vertical speed with correction figure if significant
                    if (Math.Abs(herr0) > 0.01f)
                    {
                        //Note: we use 1.05 because it doesn't disappear completely, only very critically damped
                        m_dir.Z = (float)((-((herr0 * pTimestep * 50.0f) / m_VhoverTimescale) ) * (1.05 - m_VhoverEfficiency)); 
                    }
                    else
                        //Too small, zero it.
                        m_dir.Z = 0f;
                }
            }

            #endregion

            #region Check for tainted forces

            // KF: So far I have found no good method to combine a script-requested
            // .Z velocity and gravity. Therefore only 0g will used script-requested
            // .Z velocity. >0g (m_VehicleBuoyancy < 1) will used modified gravity only.
            // m_VehicleBuoyancy: -1=2g; 0=1g; 1=0g;
            Vector3 TaintedForce = new Vector3();
            if (m_forcelist.Count != 0)
            {
                try
                {
                    for (int i = 0; i < m_forcelist.Count; i++)
                    {
                        TaintedForce = TaintedForce + (m_forcelist[i] * 100);
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    TaintedForce = Vector3.Zero;
                }
                catch (ArgumentOutOfRangeException)
                {
                    TaintedForce = Vector3.Zero;
                }
                m_forcelist = new List<Vector3>();
            }

            #endregion

            #region Linear Deflection

            Vector3 DirectionWeShouldBeHeadingToward = new Vector3(1, 0, 0);
            DirectionWeShouldBeHeadingToward *= m_referenceFrame;

            //We multiply the direction we are headed
            m_dir -= (DirectionWeShouldBeHeadingToward * m_linearDeflectionEfficiency) / (m_linearDeflectionTimescale / pTimestep);

            #endregion

            #region Check Mass

            if (Mass == 0)
            {
                d.Mass mass;
                d.BodyGetMass(m_body, out mass);

                Mass = mass.mass;
                Mass *= 2;
            }

            #endregion

            #region No X,Y,Z

            if ((m_flags & (VehicleFlag.NO_X)) != 0)
                m_dir.X = 0;
            if ((m_flags & (VehicleFlag.NO_Y)) != 0)
                m_dir.Y = 0;
            if ((m_flags & (VehicleFlag.NO_Z)) != 0)
                m_dir.Z = 0;

            #endregion

            m_dir += TaintedForce;

            //Uses the square to make bouyancy more effective as in SL, as it seems to effect gravity more the higher the value is
            //This check helps keep things from being pushed into the ground and the consequence of being shoved back out
            if (Math.Abs(m_dir.Z) > 0.1)
                m_dir.Z += ((_pParentScene.gravityz) * ((((1 - m_VehicleBuoyancy) * (1 - m_VehicleBuoyancy) * (1 - m_VehicleBuoyancy))) * pTimestep));

            if (m_dir.Z > 10)
                m_dir.Z = 10;
            else if (m_dir.Z < -10)
                m_dir.Z = -10;

            //Set the velocity
            d.BodySetLinearVel(Body, m_dir.X, m_dir.Y, m_dir.Z);

            //Check for changes and only set it once
            if (pos.X != oldPos.X || pos.Y != oldPos.Y || pos.Z != oldPos.Z)
                d.BodySetPosition(Body, pos.X, pos.Y, pos.Z);

            // apply friction
            // note: seems more effective with how SL does this with the square
            Vector3 decayamount = Vector3.One / (m_linearFrictionTimescale / (pTimestep * pTimestep));
            m_lastLinearVelocityVector -= m_lastLinearVelocityVector * decayamount;
        } // end MoveLinear()

        private float lastDrift = 0;

        private void MoveAngular(float pTimestep, AuroraODEPhysicsScene _pParentScene)
        {
            /*
            private Vector3 m_angularMotorDirection = Vector3.Zero;            // angular velocity requested by LSL motor
            private int m_angularMotorApply = 0;                            // application frame counter
             private float m_angularMotorVelocity = 0;                        // current angular motor velocity (ramps up and down)
            private float m_angularMotorTimescale = 0;                        // motor angular velocity ramp up rate
            private float m_angularMotorDecayTimescale = 0;                    // motor angular velocity decay rate
            private Vector3 m_angularFrictionTimescale = Vector3.Zero;        // body angular velocity  decay rate
            private Vector3 m_lastAngularVelocity = Vector3.Zero;            // what was last applied to body
            */

            // Get what the body is doing, this includes 'external' influences
            d.Vector3 angularVelocity = d.BodyGetAngularVel(Body);
            d.Quaternion rot = d.BodyGetQuaternion(Body);

            Vector3 vertattr = Vector3.Zero;
            Vector3 bank = Vector3.Zero;
            Vector3 deflection = Vector3.Zero;

            #region Mouselook

            if ((m_flags & VehicleFlag.MOUSELOOK_STEER) == VehicleFlag.MOUSELOOK_STEER)
            {
                if (m_userLookAt != Vector3.Zero)
                {
                    /*m_lastCameraRotation = llRotBetween(new Vector3(d.BodyGetPosition(m_body).X, d.BodyGetPosition(m_body).Y, d.BodyGetPosition(m_body).Z), m_userLookAt);
                    m_lastCameraRotation *= 10;
                    Vector3 move = ToEuler(m_lastCameraRotation);
                    //move.Z *= (-1);
                    //move *= new Quaternion(d.BodyGetQuaternion(Body).X, d.BodyGetQuaternion(Body).Y, d.BodyGetQuaternion(Body).Z, d.BodyGetQuaternion(Body).W);
                    move.Z *= (float)(-2 * Math.PI);
                    move.Y = 0;
                    move.X = 0;
                    m_angularMotorVelocity += move / pTimestep;*/
                    m_userLookAt.Z = m_userLookAt.X * 10;
                    m_userLookAt.X = 0;
                    m_userLookAt.Y = 0;
                    m_angularMotorVelocity += m_userLookAt;
                    Console.WriteLine(m_userLookAt.Z);
                    //Console.WriteLine(move.Z);
                }
            }

            #endregion

            #region Add angular motor

            if (m_angularMotorApply > 0)
            {
                // ramp up to new value
                //   current velocity  +=                         error                       /    (time to get there / step interval)
                //                               requested speed            -  last motor speed
                m_angularMotorVelocity.X += (m_angularMotorDirection.X - m_angularMotorVelocity.X) / (m_angularMotorTimescale / (pTimestep * pTimestep * pTimestep * pTimestep * 10));
                m_angularMotorVelocity.Y += (m_angularMotorDirection.Y - m_angularMotorVelocity.Y) / (m_angularMotorTimescale / (pTimestep * pTimestep * pTimestep * pTimestep * 10));
                m_angularMotorVelocity.Z += (m_angularMotorDirection.Z - m_angularMotorVelocity.Z) / (m_angularMotorTimescale / (pTimestep * pTimestep * pTimestep * pTimestep * 10));
                m_angularMotorApply--;        // This is done so that if script request rate is less than phys frame rate the expected
                // velocity may still be acheived.
            }
            else
            {
                m_angularMotorVelocity -= m_angularMotorVelocity / (m_angularMotorDecayTimescale / pTimestep);
            }

            #endregion

            #region Vertical attractor section

            if (m_verticalAttractionTimescale < 300)
            {
                Quaternion rotqq = new Quaternion(rot.X + m_referenceFrame.X,
                    rot.Y + m_referenceFrame.Y,
                    rot.Z + m_referenceFrame.Z,
                    rot.W);    // rotq = rotation of object

                m_angularMotorVelocity *= rotqq;
                float VAservo = 0.2f / (m_verticalAttractionTimescale * pTimestep);
                // get present body rotation
                Quaternion rotq = new Quaternion(rot.X, rot.Y, rot.Z, rot.W);
                // make a vector pointing up
                Vector3 verterr = Vector3.Zero;
                verterr.Z = 1.0f;
                // rotate it to Body Angle
                verterr = verterr * rotq;
                // verterr.X and .Y are the World error ammounts. They are 0 when there is no error (Vehicle Body is 'vertical'), and .Z will be 1.
                // As the body leans to its side |.X| will increase to 1 and .Z fall to 0. As body inverts |.X| will fall and .Z will go
                // negative. Similar for tilt and |.Y|. .X and .Y must be modulated to prevent a stable inverted body.
                if (verterr.Z < 0.0f)
                {
                    verterr.X = 2.0f - verterr.X;
                    verterr.Y = 2.0f - verterr.Y;
                }
                // Error is 0 (no error) to +/- 2 (max error)
                // scale it by VAservo
                verterr = verterr * VAservo;

                // As the body rotates around the X axis, then verterr.Y increases; Rotated around Y then .X increases, so
                // Change  Body angular velocity  X based on Y, and Y based on X. Z is not changed.
                vertattr.X =    verterr.Y;
                vertattr.Y =  - verterr.X;
                vertattr.Z = 0f;

                // scaling appears better using square-law
                float bounce = 1.0f - (m_verticalAttractionEfficiency * m_verticalAttractionEfficiency);
                vertattr.X += bounce * angularVelocity.X;
                vertattr.Y += bounce * angularVelocity.Y;

                #region banking

                /*
                 
                 VEHICLE_BANKING_EFFICIENCY, the angle of the roll rotation,
                 * and sometimes the vehicle's velocity along its preferred axis
                 * of motion. 

                The VEHICLE_BANKING_EFFICIENCY can vary between -1 and +1.
                 * When it is positive then any positive rotation
                 * (by the right-hand rule) about the roll-axis will effect
                 * a (negative) torque around the yaw-axis, making it turn to
                 * the right--that is the vehicle will lean into the turn,
                 * which is how real airplanes and motorcycle's work. 
                 * Negating the banking coefficient will make it so that the
                 * vehicle leans to the outside of the turn (not very "physical"
                 * but might allow interesting vehicles so why not?). 

                The VEHICLE_BANKING_MIX is a fake (i.e. non-physical) parameter
                 * that is useful for making banking vehicles do what you want
                 * rather than what the laws of physics allow. For example,
                 * consider a real motorcycle...it must be moving forward in order
                 * for it to turn while banking, however video-game motorcycles
                 * are often configured to turn in place when at a dead
                 * stop--because they are often easier to control that way
                 * using the limited interface of the keyboard or game controller.
                 * The VEHICLE_BANKING_MIX enables combinations of both realistic
                 * and non-realistic banking by functioning as a slider
                 * between a banking that is correspondingly totally static (0.0)
                 * and totally dynamic (1.0). By "static" we mean that the banking
                 * effect depends only on the vehicle's rotation about its roll-axis
                 * compared to "dynamic" where the banking is also proportional
                 * to its velocity along its roll-axis.
                 * Finding the best value of the "mixture" will probably
                 * require trial and error. 
                 * 
                */

                float oldZAngVel = m_angularMotorVelocity.Z;

                //-1 is to send a negative torque when efficiency is > 0 to make it bank inward

                float addAmount = m_angularMotorVelocity.X;

                m_angularMotorVelocity.Z = (m_angularMotorVelocity.Z * (1 - m_bankingMix)) * ((-1) * m_bankingEfficiency * addAmount) / ((m_bankingTimescale / pTimestep) *5);

                m_angularMotorVelocity.X += oldZAngVel - m_angularMotorVelocity.Z;

                #endregion

            } // else vertical attractor is off

            #endregion

            #region Deflection

            Quaternion rotation = new Quaternion(rot.X, rot.Y, rot.Z, rot.W);

            //Our ang direction minus the direction we need to be going
            // Then times the efficiency and timestep
            deflection = ToEuler((rotation - m_referenceFrame)) * m_angularDeflectionEfficiency / (m_angularDeflectionTimescale / pTimestep);

            #endregion

            // Sum of velocities
            m_lastAngularVelocity = m_angularMotorVelocity + vertattr + bank + deflection;
            
            #region Limit Motor Up
            double Zchange = d.BodyGetLinearVel(Body).Z;

            if ((m_flags & (VehicleFlag.LIMIT_MOTOR_UP)) != 0)
            {
                Vector3 Change = Vector3.One;
                //Start Experimental Values
                if (Zchange < -1)
                {
                    m_lastAngularVelocity.X += 1.25f;
                }
                else if (Zchange < -.75)
                {
                    m_lastAngularVelocity.X += 1f;
                }
                else if (Zchange < -.5)
                {
                    m_lastAngularVelocity.X += 0.75f;
                }
                else if (Zchange < -.25)
                {
                    m_lastAngularVelocity.X += .5f;
                }
                else if (Zchange < -.05)
                {
                    m_lastAngularVelocity.X += .25f;
                }

                //End Experimental Values

                if (Change != Vector3.One)
                {
                    //Using the ref frame because this requires the use of the idea of UP
                    Quaternion rotq = new Quaternion(rot.X + m_referenceFrame.X,
                        rot.Y + m_referenceFrame.Y,
                        rot.Z + m_referenceFrame.Z,
                        rot.W);    // rotq = rotation of object
                    rotq.Normalize();
                    Change *= rotq;
                    Change.Z = 0;
                    m_lastAngularVelocity += Change;
                }
            }

            #endregion

            #region Vertical stabilizer

            if (rot.Y > .01 + m_referenceFrame.Y) //Add the reference frame because this requires the use of the idea of UP
            {
                m_lastAngularVelocity.Y -= (m_lastAngularVelocity.Y) * (pTimestep);
            }
            if (rot.Y < -.01 + m_referenceFrame.Y)
            {
                m_lastAngularVelocity.Y += (m_lastAngularVelocity.Y) * (pTimestep);
            }

            #endregion

            #region Drift

            //Slide us around a bit
            lastDrift += m_lastAngularVelocity.Z / (7f / pTimestep);
            
            if (Math.Abs(lastDrift) < 0.1)
                lastDrift = 0;
            
            lastDrift -= lastDrift / (2f / pTimestep);
            
            m_lastAngularVelocity.Z += lastDrift;

            #endregion

            #region Block X,Y,Z rotation

            //Block off X,Y,Z rotation as requested
            if ((m_flags & (VehicleFlag.NO_X)) != 0)
                m_lastAngularVelocity.X = 0;
            if ((m_flags & (VehicleFlag.NO_Y)) != 0)
                m_lastAngularVelocity.Y = 0;
            if ((m_flags & (VehicleFlag.NO_Z)) != 0)
                m_lastAngularVelocity.Z = 0;

            #endregion

            #region Decay

            if (m_lastAngularVelocity.ApproxEquals(Vector3.Zero, 0.01f))
                m_lastAngularVelocity = Vector3.Zero; // Reduce small value to zero.

            // apply friction
            Vector3 decayamount = Vector3.One / (m_angularFrictionTimescale / pTimestep);
            m_lastAngularVelocity -= m_lastAngularVelocity * decayamount;

            #endregion

            #region Linear Motor Offset

            if (m_linearMotorOffset != Vector3.Zero)
            {
                //Offset of linear velocity doesn't change the linear velocity,
                //   but causes a torque to be applied, for example...
                //
                //      IIIII     >>>   IIIII
                //      IIIII     >>>    IIIII
                //      IIIII     >>>     IIIII
                //          ^
                //          |  Applying a force at the arrow will cause the object to move forward, but also rotate
                //
                //
                // The torque created is the linear velocity crossed with the offset

                //Note: we use the motor, otherwise you will just spin around and we divide by 10 since otherwise we go crazy
                Vector3 torqueFromOffset = (m_linearMotorDirectionLASTSET % m_linearMotorOffset) / 10;
                d.BodyAddTorque(Body, torqueFromOffset.X, torqueFromOffset.Y, torqueFromOffset.Z);
            }

            #endregion

            // Apply to the body
            //
            d.BodySetAngularVel (Body, m_lastAngularVelocity.X, m_lastAngularVelocity.Y, m_lastAngularVelocity.Z);
        }

        private Vector3 ToEuler(Quaternion m_lastCameraRotation)
        {
            Quaternion t = new Quaternion(m_lastCameraRotation.X * m_lastCameraRotation.X, m_lastCameraRotation.Y * m_lastCameraRotation.Y, m_lastCameraRotation.Z * m_lastCameraRotation.Z, m_lastCameraRotation.W * m_lastCameraRotation.W);
            double m = (m_lastCameraRotation.X + m_lastCameraRotation.Y + m_lastCameraRotation.Z + m_lastCameraRotation.W);
            if (m == 0) return Vector3.Zero;
            double n = 2 * (m_lastCameraRotation.Y * m_lastCameraRotation.W + m_lastCameraRotation.X * m_lastCameraRotation.Y);
            double p = m * m - n * n;
            if (p > 0)
                return new Vector3((float)NormalizeAngle(Math.Atan2(2.0 * (m_lastCameraRotation.X * m_lastCameraRotation.W - m_lastCameraRotation.Y * m_lastCameraRotation.Z), (-t.X - t.Y + t.Z + t.W))),
                                             (float)NormalizeAngle(Math.Atan2(n, Math.Sqrt(p))),
                                             (float)NormalizeAngle(Math.Atan2(2.0 * (m_lastCameraRotation.Z * m_lastCameraRotation.W - m_lastCameraRotation.X * m_lastCameraRotation.Y), (t.X - t.Y - t.Z + t.W))));
            else if (n > 0)
                return new Vector3(0, (float)(Math.PI * 0.5), (float)NormalizeAngle(Math.Atan2((m_lastCameraRotation.Z * m_lastCameraRotation.W + m_lastCameraRotation.X * m_lastCameraRotation.Y), 0.5 - t.X - t.Z)));
            else
                return new Vector3(0, (float)(-Math.PI * 0.5), (float)NormalizeAngle(Math.Atan2((m_lastCameraRotation.Z * m_lastCameraRotation.W + m_lastCameraRotation.X * m_lastCameraRotation.Y), 0.5 - t.X - t.Z)));
        }

        protected double NormalizeAngle(double angle)
        {
            if (angle > -Math.PI && angle < Math.PI)
                return angle;

            int numPis = (int)(Math.PI / angle);
            double remainder = angle - Math.PI * numPis;
            if (numPis % 2 == 1)
                return Math.PI - angle;
            return remainder;
        }
        
        //end MoveAngular

        internal void LimitRotation(float timestep)
        {
            if (m_RollreferenceFrame != Quaternion.Identity || (m_flags & VehicleFlag.LOCK_ROTATION) != 0)
            {
                d.Quaternion rot = d.BodyGetQuaternion(Body);
                d.Quaternion m_rot = rot;
                if (rot.X >= m_RollreferenceFrame.X)
                    m_rot.X = rot.X - (m_RollreferenceFrame.X / 2);

                if (rot.Y >= m_RollreferenceFrame.Y)
                    m_rot.Y = rot.Y - (m_RollreferenceFrame.Y / 2);

                if (rot.X <= -m_RollreferenceFrame.X)
                    m_rot.X = rot.X + (m_RollreferenceFrame.X / 2);

                if (rot.Y <= -m_RollreferenceFrame.Y)
                    m_rot.Y = rot.Y + (m_RollreferenceFrame.Y / 2);

                if ((m_flags & VehicleFlag.LOCK_ROTATION) != 0)
                {
                    m_rot.X = 0;
                    m_rot.Y = 0;
                }

                if (m_rot.X != rot.X || m_rot.Y != rot.Y || m_rot.Z != rot.Z)
                    d.BodySetQuaternion(Body, ref m_rot);
            }
        }

        private List<Vector3> m_forcelist = new List<Vector3>();
        Quaternion m_lastCameraRotation = Quaternion.Identity;
        private Vector3 m_userLookAt = Vector3.Zero;
        internal void ProcessSetCameraPos(Vector3 CameraRotation)
        {
            //m_referenceFrame -= m_lastCameraRotation;
            //m_referenceFrame += CameraRotation;
            m_userLookAt = CameraRotation;
        }

        internal void ProcessForceTaint(List<Vector3> forcelist)
        {
            m_forcelist = forcelist;
        }

        public Quaternion llRotBetween(Vector3 a, Vector3 b)
        {
            Quaternion rotBetween;
            // Check for zero vectors. If either is zero, return zero rotation. Otherwise,
            // continue calculation.
            if (a == Vector3.Zero || b == Vector3.Zero)
            {
                rotBetween = Quaternion.Identity;
            }
            else
            {
                a.Normalize();
                b.Normalize();
                double dotProduct = (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
                // There are two degenerate cases possible. These are for vectors 180 or
                // 0 degrees apart. These have to be detected and handled individually.
                //
                // Check for vectors 180 degrees apart.
                // A dot product of -1 would mean the angle between vectors is 180 degrees.
                if (dotProduct < -0.9999999f)
                {
                    // First assume X axis is orthogonal to the vectors.
                    Vector3 orthoVector = new Vector3(1.0f, 0.0f, 0.0f);
                    orthoVector = orthoVector - a * (a.X / (a.X * a.X) + (a.Y * a.Y) + (a.Z * a.Z));
                    // Check for near zero vector. A very small non-zero number here will create
                    // a rotation in an undesired direction.
                    if (Math.Sqrt(orthoVector.X * orthoVector.X + orthoVector.Y * orthoVector.Y + orthoVector.Z * orthoVector.Z) > 0.0001)
                    {
                        rotBetween = new Quaternion(orthoVector.X, orthoVector.Y, orthoVector.Z, 0.0f);
                    }
                    // If the magnitude of the vector was near zero, then assume the X axis is not
                    // orthogonal and use the Z axis instead.
                    else
                    {
                        // Set 180 z rotation.
                        rotBetween = new Quaternion(0.0f, 0.0f, 1.0f, 0.0f);
                    }
                }
                // Check for parallel vectors.
                // A dot product of 1 would mean the angle between vectors is 0 degrees.
                else if (dotProduct > 0.9999999f)
                {
                    // Set zero rotation.
                    rotBetween = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
                }
                else
                {
                    // All special checks have been performed so get the axis of rotation.
                    Vector3 crossProduct = new Vector3
                    (
                    a.Y * b.Z - a.Z * b.Y,
                    a.Z * b.X - a.X * b.Z,
                    a.X * b.Y - a.Y * b.X
                    );
                    // Quarternion s value is the length of the unit vector + dot product.
                    double qs = 1.0 + dotProduct;
                    rotBetween = new Quaternion(crossProduct.X, crossProduct.Y, crossProduct.Z, (float)qs);
                    // Normalize the rotation.
                    double mag = Math.Sqrt(rotBetween.X * rotBetween.X + rotBetween.Y * rotBetween.Y + rotBetween.Z * rotBetween.Z + rotBetween.W * rotBetween.W);
                    // We shouldn't have to worry about a divide by zero here. The qs value will be
                    // non-zero because we already know if we're here, then the dotProduct is not -1 so
                    // qs will not be zero. Also, we've already handled the input vectors being zero so the
                    // crossProduct vector should also not be zero.
                    rotBetween.X = (float)(rotBetween.X / mag);
                    rotBetween.Y = (float)(rotBetween.Y / mag);
                    rotBetween.Z = (float)(rotBetween.Z / mag);
                    rotBetween.W = (float)(rotBetween.W / mag);
                    // Check for undefined values and set zero rotation if any found. This code might not actually be required
                    // any longer since zero vectors are checked for at the top.
                    if (Double.IsNaN(rotBetween.X) || Double.IsNaN(rotBetween.Y) || Double.IsNaN(rotBetween.Y) || Double.IsNaN(rotBetween.W))
                    {
                        rotBetween = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
                    }
                }
            }
            return rotBetween;
        }
    }
}
