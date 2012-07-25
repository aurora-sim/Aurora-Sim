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
using System.Linq;
using OdeAPI;
using OpenMetaverse;
using Aurora.Framework.Physics;
//using Ode.NET;

namespace Aurora.Physics.AuroraOpenDynamicsEngine
{
    public class AuroraODEDynamics
    {
        public float Mass;
        private int frcount; // Used to limit dynamics debug output to
        // every 100th frame

        // private OdeScene m_parentScene = null;
        private Vector3 m_BlockingEndPoint = Vector3.Zero;
        private Quaternion m_RollreferenceFrame = Quaternion.Identity;
        private float m_VehicleBuoyancy; //KF: m_VehicleBuoyancy is set by VEHICLE_BUOYANCY for a vehicle.
        private float m_VhoverEfficiency;
        private float m_VhoverHeight;
        private float m_VhoverTargetHeight = -1.0f; // if <0 then no hover, else its the current target height
        private float m_VhoverTimescale;
        // Linear properties
        //       private Vector3 m_lastVertAttractor = Vector3.Zero;             // what VA was last applied to body

        //Deflection properties
        private float m_angularDeflectionEfficiency;
        private float m_angularDeflectionTimescale;
        private Vector3 m_angularFrictionTimescale = Vector3.Zero; // body angular velocity  decay rate
        
        private float m_angularMotorDecayTimescale; // motor angular velocity decay rate
        private Vector3 m_angularMotorDirection = Vector3.Zero; // angular velocity requested by LSL motor
        private float m_angularMotorTimescale; // motor angular velocity ramp up rate
        private Vector3 m_angularMotorVelocity = Vector3.Zero; // current angular motor velocity
        

        //Banking properties
        private float m_bankingEfficiency;
        private float m_bankingMix;
        private float m_bankingTimescale;
        private IntPtr m_body = IntPtr.Zero;
        private bool m_enabled;
        private VehicleFlag m_flags = 0; // Boolean settings:
        private List<Vector3> m_forcelist = new List<Vector3>();
        private Vector3 m_lastAngVelocity = Vector3.Zero;
        private Vector3 m_lastAngularVelocity = Vector3.Zero; // what was last applied to body
        private Vector3 m_lastLinearVelocityVector = Vector3.Zero;
        private Vector3 m_lastPositionVector = Vector3.Zero;
        private Vector3 m_lastVelocity = Vector3.Zero;
        private Vector3 m_lastposChange = Vector3.Zero;
        private float m_linearDeflectionEfficiency;
        private float m_linearDeflectionTimescale;
        private Vector3 m_linearFrictionTimescale = Vector3.Zero;
        private float m_linearMotorDecayTimescale;
        private Vector3 m_linearMotorDirection = Vector3.Zero; // velocity requested by LSL, decayed by time
        private Vector3 m_linearMotorDirectionLASTSET = Vector3.Zero; // velocity requested by LSL
        private Vector3 m_linearMotorOffset = Vector3.Zero;
        private float m_linearMotorTimescale;
        //private bool m_linearZeroFlag;
        private Vector3 m_newVelocity = Vector3.Zero; // velocity applied to body
        private Quaternion m_referenceFrame = Quaternion.Identity; // Axis modifier
        private Vehicle m_type = Vehicle.TYPE_NONE; // If a 'VEHICLE', and what kind
        private Quaternion m_userLookAt = Quaternion.Identity;

        //Hover and Buoyancy properties
        // Modifies gravity. Slider between -1 (double-gravity) and 1 (full anti-gravity)
        // KF: So far I have found no good method to combine a script-requested .Z velocity and gravity.
        // Therefore only m_VehicleBuoyancy=1 (0g) will use the script-requested .Z velocity.

        //Attractor properties
        private float m_verticalAttractionEfficiency = 1.0f; // damped
        private float m_verticalAttractionTimescale = 500f; // Timescale > 300  means no vert attractor.
        
       
        

        public Vehicle Type
        {
            get { return m_type; }
        }

        public IntPtr Body
        {
            get { return m_body; }
        }

        internal void ProcessFloatVehicleParam(Vehicle pParam, float pValue, float timestep)
        {
            switch (pParam)
            {
                case Vehicle.ANGULAR_DEFLECTION_EFFICIENCY:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_angularDeflectionEfficiency = pValue;
                    break;
                case Vehicle.ANGULAR_DEFLECTION_TIMESCALE:
                    if (pValue <= timestep)
                        m_angularDeflectionTimescale = timestep;
                    else
                        m_angularDeflectionTimescale = pValue/timestep;
                    break;
                case Vehicle.ANGULAR_MOTOR_DECAY_TIMESCALE:
                    if (pValue <= timestep)
                        m_angularMotorDecayTimescale = timestep;
                    else
                        m_angularMotorDecayTimescale = pValue/timestep;
                    break;
                case Vehicle.ANGULAR_MOTOR_TIMESCALE:
                    if (pValue <= timestep)
                        m_angularMotorTimescale = timestep;
                    else
                        m_angularMotorTimescale = pValue/timestep;
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
                    if (pValue <= timestep)
                        m_bankingTimescale = timestep;
                    else
                        m_bankingTimescale = pValue/timestep;
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
                    if (pValue <= timestep)
                        m_VhoverTimescale = timestep;
                    else
                        m_VhoverTimescale = pValue/timestep;
                    break;
                case Vehicle.LINEAR_DEFLECTION_EFFICIENCY:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_linearDeflectionEfficiency = pValue;
                    break;
                case Vehicle.LINEAR_DEFLECTION_TIMESCALE:
                    if (pValue <= timestep)
                        m_linearDeflectionTimescale = timestep;
                    else
                        m_linearDeflectionTimescale = pValue/timestep;
                    break;
                case Vehicle.LINEAR_MOTOR_DECAY_TIMESCALE:
                    if (pValue > 120)
                        m_linearMotorDecayTimescale = 120/timestep;
                    else if (pValue <= timestep)
                        m_linearMotorDecayTimescale = timestep;
                    else
                        m_linearMotorDecayTimescale = pValue/timestep;
                    break;
                case Vehicle.LINEAR_MOTOR_TIMESCALE:
                    if (pValue <= timestep)
                        m_linearMotorTimescale = timestep;
                    else
                        m_linearMotorTimescale = pValue/timestep;
                    break;
                case Vehicle.VERTICAL_ATTRACTION_EFFICIENCY:
                    if (pValue < 0.1f) pValue = 0.1f; // Less goes unstable
                    if (pValue > 1.0f) pValue = 1.0f;
                    m_verticalAttractionEfficiency = pValue;
                    break;
                case Vehicle.VERTICAL_ATTRACTION_TIMESCALE:
                    if (pValue <= timestep)
                        m_verticalAttractionTimescale = timestep;
                    else
                        m_verticalAttractionTimescale = pValue/timestep;
                    break;

                    // These are vector properties but the engine lets you use a single float value to
                    // set all of the components to the same value
                case Vehicle.ANGULAR_FRICTION_TIMESCALE:
                    if (pValue <= timestep)
                        pValue = timestep;
                    m_angularFrictionTimescale = new Vector3(pValue, pValue, pValue);
                    break;
                case Vehicle.ANGULAR_MOTOR_DIRECTION:
                    m_angularMotorDirection = new Vector3(pValue, pValue, pValue);
                    m_angularMotorApply = 100;
                    break;
                case Vehicle.LINEAR_FRICTION_TIMESCALE:
                    if (pValue <= timestep)
                        pValue = timestep;
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
        }

//end ProcessFloatVehicleParam

        //All parts hooked up
        internal void ProcessVectorVehicleParam(Vehicle pParam, Vector3 pValue, float timestep)
        {
            switch (pParam)
            {
                case Vehicle.ANGULAR_FRICTION_TIMESCALE:
                    if (pValue.X < timestep)
                        pValue.X = timestep;
                    if (pValue.Y < timestep)
                        pValue.Y = timestep;
                    if (pValue.Z < timestep)
                        pValue.Z = timestep;
                    m_angularFrictionTimescale = new Vector3(pValue.X/timestep, pValue.Y/timestep, pValue.Z/timestep);
                    break;
                case Vehicle.ANGULAR_MOTOR_DIRECTION:
                    m_angularMotorDirection = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    // Limit requested angular speed to 2 rps= 4 pi rads/sec
                    if (m_angularMotorDirection.X > 12.56f) m_angularMotorDirection.X = 12.56f;
                    else if (m_angularMotorDirection.X < -12.56f) m_angularMotorDirection.X = -12.56f;
                    if (m_angularMotorDirection.Y > 12.56f) m_angularMotorDirection.Y = 12.56f;
                    else if (m_angularMotorDirection.Y < -12.56f) m_angularMotorDirection.Y = -12.56f;
                    if (m_angularMotorDirection.Z > 12.56f) m_angularMotorDirection.Z = 12.56f;
                    else if (m_angularMotorDirection.Z < -12.56f) m_angularMotorDirection.Z = -12.56f;

                    break;
                case Vehicle.LINEAR_FRICTION_TIMESCALE:
                    if (pValue.X < timestep)
                        pValue.X = timestep;
                    if (pValue.Y < timestep)
                        pValue.Y = timestep;
                    if (pValue.Z < timestep)
                        pValue.Z = timestep;
                    m_linearFrictionTimescale = new Vector3(pValue.X/timestep, pValue.Y/timestep, pValue.Z/timestep);
                    break;
                case Vehicle.LINEAR_MOTOR_DIRECTION:
                    m_linearMotorDirection = pValue;
                    m_linearMotorDirectionLASTSET = m_linearMotorDirection;
                    break;
                case Vehicle.LINEAR_MOTOR_OFFSET:
                    m_linearMotorOffset = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.BLOCK_EXIT:
                    m_BlockingEndPoint = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
            }
        }

//end ProcessVectorVehicleParam

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
        }

//end ProcessRotationVehicleParam

        internal void ProcessVehicleFlags(int pParam, bool remove)
        {
            VehicleFlag param = (VehicleFlag) pParam;
            if (remove)
            {
                if (pParam == -1)
                    m_flags = 0;
                else
                    m_flags &= ~param;
            }
            else if (pParam == -1)
                m_flags = 0;
            else
                m_flags |= param;
        }

//end ProcessVehicleFlags

        internal void ProcessTypeChange(AuroraODEPrim parent, Vehicle pType, float timestep)
        {
            // Set Defaults For Type
            m_type = pType;
            switch (pType)
            {
                case Vehicle.TYPE_NONE:
                    m_linearFrictionTimescale = new Vector3(1000/timestep, 1000/timestep, 1000/timestep);
                    m_angularFrictionTimescale = new Vector3(1000/timestep, 1000/timestep, 1000/timestep);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 1000/timestep;
                    m_linearMotorDecayTimescale = 1000/timestep;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 1000/timestep;
                    m_angularMotorDecayTimescale = 120/timestep;
                    m_VhoverHeight = 0;
                    m_VhoverTimescale = 1000/timestep;
                    m_VehicleBuoyancy = 0;

                    m_linearDeflectionEfficiency = 1;
                    m_linearDeflectionTimescale = 1/timestep;

                    m_angularDeflectionEfficiency = 0;
                    m_angularDeflectionTimescale = 1000/timestep;

                    m_bankingEfficiency = 0;
                    m_bankingMix = 1;
                    m_bankingTimescale = 1000/timestep;

                    m_flags = 0;
                    m_referenceFrame = Quaternion.Identity;
                    break;

                case Vehicle.TYPE_SLED:
                    m_linearFrictionTimescale = new Vector3(30/timestep, 1/timestep, 1000/timestep);

                    m_angularFrictionTimescale = new Vector3(1000/timestep, 1000/timestep, 1000/timestep);

                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 1000/timestep;
                    m_linearMotorDecayTimescale = 120/timestep;

                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 1000/timestep;
                    m_angularMotorDecayTimescale = 120/timestep;

                    m_VhoverHeight = 0;
                    m_VhoverEfficiency = 10;
                    m_VhoverTimescale = 10/timestep;
                    m_VehicleBuoyancy = 0;

                    m_linearDeflectionEfficiency = 1;
                    m_linearDeflectionTimescale = 1/timestep;

                    m_angularDeflectionEfficiency = 0;
                    m_angularDeflectionTimescale = 1000/timestep;

                    m_bankingEfficiency = 0;
                    m_bankingMix = 1;
                    m_bankingTimescale = 10/timestep;

                    m_referenceFrame = Quaternion.Identity;
                    m_flags &=
                        ~(VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                          VehicleFlag.HOVER_GLOBAL_HEIGHT | VehicleFlag.HOVER_UP_ONLY);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_ROLL_ONLY | VehicleFlag.LIMIT_MOTOR_UP);
                    break;
                case Vehicle.TYPE_CAR:
                    m_linearFrictionTimescale = new Vector3(100/timestep, 2/timestep, 1000/timestep);
                    m_angularFrictionTimescale = new Vector3(1000/timestep, 1000/timestep, 1000/timestep);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 1/timestep;
                    m_linearMotorDecayTimescale = 60/timestep;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 1/timestep;
                    m_angularMotorDecayTimescale = 0.8f/timestep;
                    m_VhoverHeight = 0;
                    m_VhoverEfficiency = 0;
                    m_VhoverTimescale = 1000/timestep;
                    m_VehicleBuoyancy = 0;
                    m_linearDeflectionEfficiency = 1;
                    m_linearDeflectionTimescale = 2/timestep;
                    m_angularDeflectionEfficiency = 0;
                    m_angularDeflectionTimescale = 10/timestep;
                    m_verticalAttractionEfficiency = 1f;
                    m_verticalAttractionTimescale = 10f/timestep;
                    m_bankingEfficiency = -0.2f;
                    m_bankingMix = 1;
                    m_bankingTimescale = 1/timestep;
                    m_referenceFrame = Quaternion.Identity;
                    m_flags &=
                        ~(VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                          VehicleFlag.HOVER_GLOBAL_HEIGHT);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_ROLL_ONLY |
                                VehicleFlag.LIMIT_MOTOR_UP | VehicleFlag.HOVER_UP_ONLY);
                    break;
                case Vehicle.TYPE_BOAT:
                    m_linearFrictionTimescale = new Vector3(10/timestep, 3/timestep, 2/timestep);
                    m_angularFrictionTimescale = new Vector3(10/timestep, 10/timestep, 10/timestep);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 5/timestep;
                    m_linearMotorDecayTimescale = 60/timestep;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 4/timestep;
                    m_angularMotorDecayTimescale = 4/timestep;
                    m_VhoverHeight = 0;
                    m_VhoverEfficiency = 0.5f;
                    m_VhoverTimescale = 2/timestep;
                    m_VehicleBuoyancy = 1;
                    m_linearDeflectionEfficiency = 0.5f;
                    m_linearDeflectionTimescale = 3/timestep;
                    m_angularDeflectionEfficiency = 0.5f;
                    m_angularDeflectionTimescale = 5/timestep;
                    m_verticalAttractionEfficiency = 0.5f;
                    m_verticalAttractionTimescale = 5f/timestep;
                    m_bankingEfficiency = -0.3f;
                    m_bankingMix = 0.8f;
                    m_bankingTimescale = 1/timestep;
                    m_referenceFrame = Quaternion.Identity;
                    m_flags &= ~(VehicleFlag.LIMIT_ROLL_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                                 VehicleFlag.HOVER_GLOBAL_HEIGHT | VehicleFlag.HOVER_UP_ONLY);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP |
                                VehicleFlag.LIMIT_MOTOR_UP |
                                VehicleFlag.HOVER_WATER_ONLY);
                    break;
                case Vehicle.TYPE_AIRPLANE:
                    m_linearFrictionTimescale = new Vector3(200/timestep, 10/timestep, 5/timestep);
                    m_angularFrictionTimescale = new Vector3(20/timestep, 20/timestep, 20/timestep);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 2/timestep;
                    m_linearMotorDecayTimescale = 60/timestep;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 4/timestep;
                    m_angularMotorDecayTimescale = 4/timestep;
                    m_VhoverHeight = 0;
                    m_VhoverEfficiency = 0.5f;
                    m_VhoverTimescale = 1000/timestep;
                    m_VehicleBuoyancy = 0;
                    m_linearDeflectionEfficiency = 0.5f;
                    m_linearDeflectionTimescale = 3/timestep;
                    m_angularDeflectionEfficiency = 1;
                    m_angularDeflectionTimescale = 2/timestep;
                    m_verticalAttractionEfficiency = 0.9f;
                    m_verticalAttractionTimescale = 2f/timestep;
                    m_bankingEfficiency = 1;
                    m_bankingMix = 0.7f;
                    m_bankingTimescale = 2;
                    m_referenceFrame = Quaternion.Identity;
                    m_flags &= ~(VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_MOTOR_UP |
                                 VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                                 VehicleFlag.HOVER_GLOBAL_HEIGHT | VehicleFlag.HOVER_UP_ONLY);
                    m_flags |= (VehicleFlag.LIMIT_ROLL_ONLY);
                    break;
                case Vehicle.TYPE_BALLOON:
                    m_linearFrictionTimescale = new Vector3(5/timestep, 5/timestep, 5/timestep);
                    m_angularFrictionTimescale = new Vector3(10/timestep, 10/timestep, 10/timestep);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 5/timestep;
                    m_linearMotorDecayTimescale = 60/timestep;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 6/timestep;
                    m_angularMotorDecayTimescale = 10/timestep;
                    m_VhoverHeight = 5;
                    m_VhoverEfficiency = 0.8f;
                    m_VhoverTimescale = 10/timestep;
                    m_VehicleBuoyancy = 1;
                    m_linearDeflectionEfficiency = 0;
                    m_linearDeflectionTimescale = 5/timestep;
                    m_angularDeflectionEfficiency = 0;
                    m_angularDeflectionTimescale = 5/timestep;
                    m_verticalAttractionEfficiency = 1f;
                    m_verticalAttractionTimescale = 100f/timestep;
                    m_bankingEfficiency = 0;
                    m_bankingMix = 0.7f;
                    m_bankingTimescale = 5/timestep;
                    m_referenceFrame = Quaternion.Identity;
                    m_flags &= ~(VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_MOTOR_UP |
                                 VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                                 VehicleFlag.HOVER_UP_ONLY);
                    m_flags |= (VehicleFlag.LIMIT_ROLL_ONLY | VehicleFlag.HOVER_GLOBAL_HEIGHT);
                    break;
            }
        }

//end SetDefaultsForType

        internal void Enable(IntPtr pBody, AuroraODEPrim parent, AuroraODEPhysicsScene pParentScene)
        {
            if (m_enabled)
                return;
            if (pBody == IntPtr.Zero || m_type == Vehicle.TYPE_NONE)
                return;
            m_body = pBody;
            d.BodySetGravityMode(Body, true);
            m_enabled = true;
            m_lastLinearVelocityVector = parent.Velocity;
            m_lastPositionVector = parent.Position;
            m_lastAngularVelocity = parent.RotationalVelocity;
            parent.ThrottleUpdates = false;
            GetMass(pBody);
        }

        internal void Disable(AuroraODEPrim parent)
        {
            if (!m_enabled || m_type == Vehicle.TYPE_NONE)
                return;
            m_enabled = false;
            parent.ThrottleUpdates = true;
            parent.ForceSetVelocity(Vector3.Zero);
            parent.ForceSetRotVelocity(Vector3.Zero);
            parent.ForceSetPosition(parent.Position);
            d.BodySetGravityMode(Body, false);
            m_body = IntPtr.Zero;
            m_linearMotorDirection = Vector3.Zero;
            m_linearMotorDirectionLASTSET = Vector3.Zero;
            m_angularMotorDirection = Vector3.Zero;
        }

        internal void GetMass(IntPtr pBody)
        {
            d.Mass mass;
            d.BodyGetMass(pBody, out mass);

            Mass = mass.mass;
        }


        internal void Step(IntPtr pBody, float pTimestep, AuroraODEPhysicsScene pParentScene, AuroraODEPrim parent)
        {
            m_body = pBody;
            if (pBody == IntPtr.Zero || m_type == Vehicle.TYPE_NONE)
                return;
            if (Mass == 0)
                GetMass(pBody);
            if (Mass == 0)
                return; //No noMass vehicles...
            if (!d.BodyIsEnabled(Body))
                d.BodyEnable(Body);

            frcount++; // used to limit debug comment output
            if (frcount > 100)
                frcount = 0;

            // scale time so parameters work as before
            // until we scale then acording to ode step time

            MoveLinear(pTimestep, pParentScene, parent);
            MoveAngular(pTimestep, pParentScene, parent);
            LimitRotation(pTimestep);
        }

        // end Step

        private void MoveLinear(float pTimestep, AuroraODEPhysicsScene _pParentScene, AuroraODEPrim parent)
        {
            if (m_linearMotorDirection.LengthSquared() < 0.0001f)
            {
                m_linearMotorDirection = Vector3.Zero;
                m_newVelocity = Vector3.Zero;
            }
            else
            {
                Vector3 addAmount = (m_linearMotorDirection - m_lastLinearVelocityVector)/(m_linearMotorTimescale);
                m_lastLinearVelocityVector += (addAmount);

                m_linearMotorDirection *= (1.0f - 1.0f/m_linearMotorDecayTimescale);

                // convert requested object velocity to world-referenced vector
                d.Quaternion rot = d.BodyGetQuaternion(Body);
                Quaternion rotq = new Quaternion(rot.X, rot.Y, rot.Z, rot.W); // rotq = rotation of object
                //Vector3 oldVelocity = m_newVelocity;
                m_newVelocity = m_lastLinearVelocityVector * rotq; // apply obj rotation to velocity vector
                //if (oldVelocity.Z == 0 && (Type != Vehicle.TYPE_AIRPLANE && Type != Vehicle.TYPE_BALLOON))
                //    m_newVelocity.Z += dvel_now.Z; // Preserve the accumulated falling velocity
            }

            //if (m_newVelocity.Z == 0 && (Type != Vehicle.TYPE_AIRPLANE && Type != Vehicle.TYPE_BALLOON))
            //    m_newVelocity.Z += dvel_now.Z; // Preserve the accumulated falling velocity

            d.Vector3 dpos = d.BodyGetPosition(Body);
            Vector3 pos = new Vector3(dpos.X, dpos.Y, dpos.Z);

            if (!(m_lastPositionVector.X == 0 &&
                  m_lastPositionVector.Y == 0 &&
                  m_lastPositionVector.Z == 0))
            {
                ///Only do this if we have a last position
                m_lastposChange.X = pos.X - m_lastPositionVector.X;
                m_lastposChange.Y = pos.Y - m_lastPositionVector.Y;
                m_lastposChange.Z = pos.Z - m_lastPositionVector.Z;
            }

            #region Blocking Change

            if (m_BlockingEndPoint != Vector3.Zero)
            {
                bool needUpdateBody = false;
                if (pos.X >= (m_BlockingEndPoint.X - 1))
                {
                    pos.X -= m_lastposChange.X + 1;
                    needUpdateBody = true;
                }
                if (pos.Y >= (m_BlockingEndPoint.Y - 1))
                {
                    pos.Y -= m_lastposChange.Y + 1;
                    needUpdateBody = true;
                }
                if (pos.Z >= (m_BlockingEndPoint.Z - 1))
                {
                    pos.Z -= m_lastposChange.Z + 1;
                    needUpdateBody = true;
                }
                if (pos.X <= 0)
                {
                    pos.X += m_lastposChange.X + 1;
                    needUpdateBody = true;
                }
                if (pos.Y <= 0)
                {
                    pos.Y += m_lastposChange.Y + 1;
                    needUpdateBody = true;
                }
                if (needUpdateBody)
                    d.BodySetPosition(Body, pos.X, pos.Y, pos.Z);
            }

            #endregion

            #region Terrain checks

            float terrainHeight = _pParentScene.GetTerrainHeightAtXY(pos.X, pos.Y);
            if (pos.Z < terrainHeight - 5)
            {
                pos.Z = terrainHeight + 2;
                m_lastPositionVector = pos;
                    //Make sure that we don't have an explosion the next frame with the posChange
                d.BodySetPosition(Body, pos.X, pos.Y, pos.Z);
            }
            else if (pos.Z < terrainHeight)
                m_newVelocity.Z += 1;

            #endregion

            #region Hover

            // Check if hovering
            if ((m_flags &
                 (VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY | VehicleFlag.HOVER_GLOBAL_HEIGHT)) != 0)
            {
                // We should hover, get the target height
                if ((m_flags & VehicleFlag.HOVER_WATER_ONLY) != 0)
                {
                    m_VhoverTargetHeight = (float) _pParentScene.GetWaterLevel(pos.X, pos.Y) + m_VhoverHeight;
                }
                if ((m_flags & VehicleFlag.HOVER_TERRAIN_ONLY) != 0)
                {
                    m_VhoverTargetHeight = _pParentScene.GetTerrainHeightAtXY(pos.X, pos.Y) + m_VhoverHeight;
                }
                if ((m_flags & VehicleFlag.HOVER_GLOBAL_HEIGHT) != 0)
                {
                    m_VhoverTargetHeight = m_VhoverHeight;
                }

                float tempHoverHeight = m_VhoverTargetHeight;
                if ((m_flags & VehicleFlag.HOVER_UP_ONLY) != 0)
                {
                    // If body is aready heigher, use its height as target height
                    if (pos.Z > tempHoverHeight)
                        tempHoverHeight = pos.Z;
                }
                if ((m_flags & VehicleFlag.LOCK_HOVER_HEIGHT) != 0)
                {
                    if ((pos.Z - tempHoverHeight) > .2 || (pos.Z - tempHoverHeight) < -.2)
                    {
                        float h = tempHoverHeight;
                        float groundHeight = _pParentScene.GetTerrainHeightAtXY(pos.X, pos.Y);
                        if (groundHeight >= tempHoverHeight)
                            h = groundHeight;

                        d.BodySetPosition(Body, pos.X, pos.Y, h);
                    }
                }
                else
                {
                    float herr0 = pos.Z - tempHoverHeight;
                    // Replace Vertical speed with correction figure if significant
                    if (herr0 > 0.01f)
                    {
                        m_newVelocity.Z = -((herr0*50.0f)/m_VhoverTimescale);
                        //KF: m_VhoverEfficiency is not yet implemented
                    }
                    else if (herr0 < -0.01f)
                    {
                        m_newVelocity.Z = -((herr0*50f)/m_VhoverTimescale);
                    }
                    else
                    {
                        m_newVelocity.Z = 0f;
                    }
                }

                //                m_VhoverEfficiency = 0f;    // 0=boucy, 1=Crit.damped
                //                m_VhoverTimescale = 0f;        // time to acheive height
                //                pTimestep  is time since last frame,in secs
            }

            #endregion

            #region No X,Y,Z

            if ((m_flags & (VehicleFlag.NO_X)) != 0)
                m_newVelocity.X = 0;
            if ((m_flags & (VehicleFlag.NO_Y)) != 0)
                m_newVelocity.Y = 0;
            if ((m_flags & (VehicleFlag.NO_Z)) != 0)
                m_newVelocity.Z = 0;

            #endregion

            #region Deal with tainted forces

            // KF: So far I have found no good method to combine a script-requested
            // .Z velocity and gravity. Therefore only 0g will used script-requested
            // .Z velocity. >0g (m_VehicleBuoyancy < 1) will used modified gravity only.
            // m_VehicleBuoyancy: -1=2g; 0=1g; 1=0g;
            Vector3 TaintedForce = new Vector3();
            if (m_forcelist.Count != 0)
            {
                try
                {
#if (!ISWIN)
                    foreach (Vector3 vector3 in m_forcelist)
                        TaintedForce = TaintedForce + (vector3);
#else
                    TaintedForce = m_forcelist.Aggregate(TaintedForce, (current, t) => current + (t));
#endif
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

            // force to deltaV
            m_newVelocity += TaintedForce*(pTimestep/Mass);

            #endregion

            #region Deflection

            //Forward is the prefered direction
            /*Vector3 deflectionamount = m_newVelocity / (m_linearDeflectionTimescale / pTimestep);
            //deflectionamount *= m_linearDeflectionEfficiency;
            if (deflectionamount != Vector3.Zero)
            {
            }
            Vector3 deflection = Vector3.One / deflectionamount;
            m_newVelocity /= deflection;*/

            #endregion

            #region limitations

            if (m_newVelocity.LengthSquared() > 1e6f)
            {
                m_newVelocity /= m_newVelocity.Length();
                m_newVelocity *= 1000f;
            }
            else if (m_newVelocity.LengthSquared() < 1e-6f)
                m_newVelocity = Vector3.Zero;

            #endregion

            m_lastPositionVector = parent.Position;

            float grav = -1 * Mass * pTimestep;

            // Apply velocity
            if (m_newVelocity != Vector3.Zero)
            {
                if ((Type == Vehicle.TYPE_CAR || Type == Vehicle.TYPE_SLED) && !parent.LinkSetIsColliding)
                {
                    //Force ODE gravity here!!!
                }
                else
                    d.BodySetLinearVel(Body, m_newVelocity.X, m_newVelocity.Y, m_newVelocity.Z + grav);
            }
            // apply friction
            m_lastLinearVelocityVector.X *= (1.0f - 1/m_linearFrictionTimescale.X);
            m_lastLinearVelocityVector.Y *= (1.0f - 1/m_linearFrictionTimescale.Y);
            m_lastLinearVelocityVector.Z *= (1.0f - 1/m_linearFrictionTimescale.Z);
        }

        // end MoveLinear()

        private void MoveAngular(float pTimestep, AuroraODEPhysicsScene _pParentScene, AuroraODEPrim parent)
        {
            d.Vector3 angularVelocity = d.BodyGetAngularVel(Body);
            d.Quaternion rot = d.BodyGetQuaternion(Body);
            Quaternion rotq = new Quaternion(rot.X, rot.Y, rot.Z, rot.W);
            //         Vector3 angularVelocity = Vector3.Zero;

            /*if ((m_flags & VehicleFlag.MOUSELOOK_STEER) == VehicleFlag.MOUSELOOK_STEER)
            {
                if (m_userLookAt != Quaternion.Identity)
                {
                    Quaternion camrot = Quaternion.Subtract (m_userLookAt, rotq);
                    camrot.Normalize ();
                    m_angularMotorVelocity += Vector3.One * camrot;
                    Console.WriteLine (Vector3.One * camrot);
                }
            }*/

            if (m_angularMotorDirection.LengthSquared() > 1e-6f)
            {
                m_angularMotorVelocity.X = (m_angularMotorDirection.X - angularVelocity.X)/(m_angularMotorTimescale);
                m_angularMotorVelocity.Y = (m_angularMotorDirection.Y - angularVelocity.Y)/(m_angularMotorTimescale);
                m_angularMotorVelocity.Z = (m_angularMotorDirection.Z - angularVelocity.Z)/(m_angularMotorTimescale);
                m_angularMotorDirection *= (1.0f - 1.0f/m_angularMotorDecayTimescale);
            }
            else
            {
                m_angularMotorVelocity = Vector3.Zero;
            } // end motor section


            // Vertical attractor section
            Vector3 vertattr = Vector3.Zero;
            Vector3 deflection = Vector3.Zero;
            Vector3 banking = Vector3.Zero;

            if (m_verticalAttractionTimescale < 300 && m_lastAngularVelocity != Vector3.Zero)
            {
                float VAservo = 0;
                if (Type == Vehicle.TYPE_BOAT)
                {
                    VAservo = 0.2f / (m_verticalAttractionTimescale);
                    VAservo *= (m_verticalAttractionEfficiency * m_verticalAttractionEfficiency);
                }
                else
                {
                    if (parent.LinkSetIsColliding)
                        VAservo = 0.05f / (m_verticalAttractionTimescale);
                    else
                        VAservo = 0.2f / (m_verticalAttractionTimescale);
                    VAservo *= (m_verticalAttractionEfficiency * m_verticalAttractionEfficiency);
                }
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
                //if (frcount == 0) Console.WriteLine("VAerr=" + verterr);

                // As the body rotates around the X axis, then verterr.Y increases; Rotated around Y then .X increases, so
                // Change  Body angular velocity  X based on Y, and Y based on X. Z is not changed.
                vertattr.X = verterr.Y;
                vertattr.Y = -verterr.X;
                vertattr.Z = 0f;

                // scaling appears better usingsquare-law
                float bounce = 1.0f - (m_verticalAttractionEfficiency * m_verticalAttractionEfficiency);
                vertattr.X += bounce * angularVelocity.X;
                vertattr.Y += bounce * angularVelocity.Y;
            } // else vertical attractor is off

            #region Deflection

            //Forward is the prefered direction, but if the reference frame has changed, we need to take this into account as well
            Vector3 PreferredAxisOfMotion =
                new Vector3((10*(m_angularDeflectionEfficiency/m_angularDeflectionTimescale)), 0, 0);
            PreferredAxisOfMotion *= Quaternion.Add(rotq, m_referenceFrame);

            //Multiply it so that it scales linearly
            //deflection = PreferredAxisOfMotion;

            //deflection = ((PreferredAxisOfMotion * m_angularDeflectionEfficiency) / (m_angularDeflectionTimescale / pTimestep));

            #endregion

            #region Banking

            if (m_bankingEfficiency != 0)
            {
                Vector3 dir = Vector3.One*rotq;
                float mult = (m_bankingMix*m_bankingMix)*-1*(m_bankingMix < 0 ? -1 : 1);
                    //Changes which way it banks in and out of turns

                //Use the square of the efficiency, as it looks much more how SL banking works
                float effSquared = (m_bankingEfficiency*m_bankingEfficiency);
                if (m_bankingEfficiency < 0)
                    effSquared *= -1; //Keep the negative!

                float mix = Math.Abs(m_bankingMix);
                if (m_angularMotorVelocity.X == 0)
                {
                    /*if (!parent.Orientation.ApproxEquals(this.m_referenceFrame, 0.25f))
                    {
                        Vector3 axisAngle;
                        float angle;
                        parent.Orientation.GetAxisAngle(out axisAngle, out angle);
                        Vector3 rotatedVel = parent.Velocity * parent.Orientation;
                        if ((rotatedVel.X < 0 && axisAngle.Y > 0) || (rotatedVel.X > 0 && axisAngle.Y < 0))
                            m_angularMotorVelocity.X += (effSquared * (mult * mix)) * (1f) * 10;
                        else
                            m_angularMotorVelocity.X += (effSquared * (mult * mix)) * (-1f) * 10;
                    }*/
                }
                else
                    banking.Z += (effSquared*(mult*mix))*(m_angularMotorVelocity.X) * 4;
                if (!parent.LinkSetIsColliding && Math.Abs(m_angularMotorVelocity.X) > mix)
                    //If they are colliding, we probably shouldn't shove the prim around... probably
                {
                    float angVelZ = m_angularMotorVelocity.X*-1;
                    /*if(angVelZ > mix)
                        angVelZ = mix;
                    else if(angVelZ < -mix)
                        angVelZ = -mix;*/
                    //This controls how fast and how far the banking occurs
                    Vector3 bankingRot = new Vector3(angVelZ*(effSquared*mult), 0, 0);
                    if (bankingRot.X > 3)
                        bankingRot.X = 3;
                    else if (bankingRot.X < -3)
                        bankingRot.X = -3;
                    bankingRot *= rotq;
                    banking += bankingRot;
                }
                m_angularMotorVelocity.X *= m_bankingEfficiency == 1 ? 0.0f : 1 - m_bankingEfficiency;
            }

            #endregion

            #region Downward Force

            Vector3 downForce = Vector3.Zero;

            double Zchange = m_lastposChange.Z;
            if ((m_flags & (VehicleFlag.LIMIT_MOTOR_UP)) != 0) //if it isn't going up, don't apply the limiting force
            {
                if (Zchange < -0.1f)
                {
                    if (Zchange < -0.3f)
                        Zchange = -0.3f;
                    //Requires idea of 'up', so use reference frame to rotate it
                    //Add to the X, because that will normally tilt the vehicle downward (if its rotated, it'll be rotated by the ref. frame
                    downForce = (new Vector3(0, ((float) Math.Abs(Zchange)*(pTimestep*_pParentScene.PID_P/4)), 0));
                    downForce *= rotq;
                }
            }

            #endregion

            // Sum velocities
            m_lastAngularVelocity = m_angularMotorVelocity + vertattr + deflection + banking + downForce;

            #region Linear Motor Offset

            //Offset section
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
                Vector3 torqueFromOffset = (m_linearMotorDirectionLASTSET/m_linearMotorOffset);
                if (float.IsNaN(torqueFromOffset.X))
                    torqueFromOffset.X = 0;
                if (float.IsNaN(torqueFromOffset.Y))
                    torqueFromOffset.Y = 0;
                if (float.IsNaN(torqueFromOffset.Z))
                    torqueFromOffset.Z = 0;
                d.BodyAddTorque(Body, torqueFromOffset.X, torqueFromOffset.Y, torqueFromOffset.Z);
            }

            #endregion

            /*if ((m_flags & (VehicleFlag.NO_DEFLECTION_UP)) != 0)
            {
                m_lastAngularVelocity.X = 0;
                m_lastAngularVelocity.Y = 0;
            }*/

            // apply friction

            // Apply to the body

            if (m_lastAngularVelocity.LengthSquared() < 0.0001f)
            {
                m_lastAngularVelocity = Vector3.Zero;
                d.BodySetAngularVel(Body, 0, 0, 0);
                m_angularZeroFlag = true;
            }
            else
            {
                if (!d.BodyIsEnabled(Body))
                    d.BodyEnable(Body);
                d.BodySetAngularVel(Body, m_lastAngularVelocity.X, m_lastAngularVelocity.Y, m_lastAngularVelocity.Z);
                m_angularZeroFlag = false;

                m_lastAngularVelocity.X *= (1.0f - 1.0f/m_angularFrictionTimescale.X);
                m_lastAngularVelocity.Y *= (1.0f - 1.0f/m_angularFrictionTimescale.Y);
                m_lastAngularVelocity.Z *= (1.0f - 1.0f/m_angularFrictionTimescale.Z);
            }
        }


        private Vector3 ToEuler(Quaternion m_lastCameraRotation)
        {
            Quaternion t = new Quaternion(m_lastCameraRotation.X*m_lastCameraRotation.X,
                                          m_lastCameraRotation.Y*m_lastCameraRotation.Y,
                                          m_lastCameraRotation.Z*m_lastCameraRotation.Z,
                                          m_lastCameraRotation.W*m_lastCameraRotation.W);
            double m = (m_lastCameraRotation.X + m_lastCameraRotation.Y + m_lastCameraRotation.Z +
                        m_lastCameraRotation.W);
            if (m == 0) return Vector3.Zero;
            double n = 2*(m_lastCameraRotation.Y*m_lastCameraRotation.W + m_lastCameraRotation.X*m_lastCameraRotation.Y);
            double p = m*m - n*n;
            if (p > 0)
                return
                    new Vector3(
                        (float)
                        NormalizeAngle(
                            Math.Atan2(
                                2.0*
                                (m_lastCameraRotation.X*m_lastCameraRotation.W -
                                 m_lastCameraRotation.Y*m_lastCameraRotation.Z), (-t.X - t.Y + t.Z + t.W))),
                        (float) NormalizeAngle(Math.Atan2(n, Math.Sqrt(p))),
                        (float)
                        NormalizeAngle(
                            Math.Atan2(
                                2.0*
                                (m_lastCameraRotation.Z*m_lastCameraRotation.W -
                                 m_lastCameraRotation.X*m_lastCameraRotation.Y), (t.X - t.Y - t.Z + t.W))));
            else if (n > 0)
                return new Vector3(0, (float) (Math.PI*0.5),
                                   (float)
                                   NormalizeAngle(
                                       Math.Atan2(
                                           (m_lastCameraRotation.Z*m_lastCameraRotation.W +
                                            m_lastCameraRotation.X*m_lastCameraRotation.Y), 0.5 - t.X - t.Z)));
            else
                return new Vector3(0, (float) (-Math.PI*0.5),
                                   (float)
                                   NormalizeAngle(
                                       Math.Atan2(
                                           (m_lastCameraRotation.Z*m_lastCameraRotation.W +
                                            m_lastCameraRotation.X*m_lastCameraRotation.Y), 0.5 - t.X - t.Z)));
        }

        protected double NormalizeAngle(double angle)
        {
            if (angle > -Math.PI && angle < Math.PI)
                return angle;

            int numPis = (int) (Math.PI/angle);
            double remainder = angle - Math.PI*numPis;
            if (numPis%2 == 1)
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
                    m_rot.X = rot.X - (m_RollreferenceFrame.X/2);

                if (rot.Y >= m_RollreferenceFrame.Y)
                    m_rot.Y = rot.Y - (m_RollreferenceFrame.Y/2);

                if (rot.X <= -m_RollreferenceFrame.X)
                    m_rot.X = rot.X + (m_RollreferenceFrame.X/2);

                if (rot.Y <= -m_RollreferenceFrame.Y)
                    m_rot.Y = rot.Y + (m_RollreferenceFrame.Y/2);

                if ((m_flags & VehicleFlag.LOCK_ROTATION) != 0)
                {
                    m_rot.X = 0;
                    m_rot.Y = 0;
                }

                if (m_rot.X != rot.X || m_rot.Y != rot.Y || m_rot.Z != rot.Z)
                    d.BodySetQuaternion(Body, ref m_rot);
            }
        }

        internal void ProcessSetCameraPos(Quaternion CameraRotation)
        {
            //m_referenceFrame -= m_lastCameraRotation;
            //m_referenceFrame += CameraRotation;
            m_userLookAt = CameraRotation;
        }

        internal void ProcessForceTaint(Vector3 force)
        {
            m_forcelist.Add(force);
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
                double dotProduct = (a.X*b.X) + (a.Y*b.Y) + (a.Z*b.Z);
                // There are two degenerate cases possible. These are for vectors 180 or
                // 0 degrees apart. These have to be detected and handled individually.
                //
                // Check for vectors 180 degrees apart.
                // A dot product of -1 would mean the angle between vectors is 180 degrees.
                if (dotProduct < -0.9999999f)
                {
                    // First assume X axis is orthogonal to the vectors.
                    Vector3 orthoVector = new Vector3(1.0f, 0.0f, 0.0f);
                    orthoVector = orthoVector - a*(a.X/(a.X*a.X) + (a.Y*a.Y) + (a.Z*a.Z));
                    // Check for near zero vector. A very small non-zero number here will create
                    // a rotation in an undesired direction.
                    rotBetween = Math.Sqrt(orthoVector.X*orthoVector.X + orthoVector.Y*orthoVector.Y +
                                           orthoVector.Z*orthoVector.Z) > 0.0001 ? new Quaternion(orthoVector.X, orthoVector.Y, orthoVector.Z, 0.0f) : new Quaternion(0.0f, 0.0f, 1.0f, 0.0f);
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
                        a.Y*b.Z - a.Z*b.Y,
                        a.Z*b.X - a.X*b.Z,
                        a.X*b.Y - a.Y*b.X
                        );
                    // Quarternion s value is the length of the unit vector + dot product.
                    double qs = 1.0 + dotProduct;
                    rotBetween = new Quaternion(crossProduct.X, crossProduct.Y, crossProduct.Z, (float) qs);
                    // Normalize the rotation.
                    double mag =
                        Math.Sqrt(rotBetween.X*rotBetween.X + rotBetween.Y*rotBetween.Y + rotBetween.Z*rotBetween.Z +
                                  rotBetween.W*rotBetween.W);
                    // We shouldn't have to worry about a divide by zero here. The qs value will be
                    // non-zero because we already know if we're here, then the dotProduct is not -1 so
                    // qs will not be zero. Also, we've already handled the input vectors being zero so the
                    // crossProduct vector should also not be zero.
                    rotBetween.X = (float) (rotBetween.X/mag);
                    rotBetween.Y = (float) (rotBetween.Y/mag);
                    rotBetween.Z = (float) (rotBetween.Z/mag);
                    rotBetween.W = (float) (rotBetween.W/mag);
                    // Check for undefined values and set zero rotation if any found. This code might not actually be required
                    // any longer since zero vectors are checked for at the top.
                    if (Double.IsNaN(rotBetween.X) || Double.IsNaN(rotBetween.Y) || Double.IsNaN(rotBetween.Y) ||
                        Double.IsNaN(rotBetween.W))
                    {
                        rotBetween = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
                    }
                }
            }
            return rotBetween;
        }



        public int m_angularMotorApply { get; set; }

        public bool m_angularZeroFlag { get; set; }
    }
}