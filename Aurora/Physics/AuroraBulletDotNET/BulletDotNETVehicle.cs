using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;
using BulletDotNET;
using OpenMetaverse;

namespace Aurora.Physics.BulletDotNETPlugin
{
    public class BulletDotNETVehicle
    {
        private int frcount = 0;                                        // Used to limit dynamics debug output to
        // every 100th frame

        // Vehicle properties
        private Vehicle m_type = Vehicle.TYPE_NONE;                     // If a 'VEHICLE', and what kind
        // private Quaternion m_referenceFrame = Quaternion.Identity;   // Axis modifier
        private VehicleFlag m_flags = (VehicleFlag)0;                  // Boolean settings:
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
        private btVector3 m_lastPositionVector = new btVector3(0,0,0);
        // private bool m_LinearMotorSetLastFrame = false;
        // private Vector3 m_linearMotorOffset = Vector3.Zero;

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
        // private float m_angularDeflectionEfficiency = 0;
        // private float m_angularDeflectionTimescale = 0;
        // private float m_linearDeflectionEfficiency = 0;
        // private float m_linearDeflectionTimescale = 0;

        //Banking properties
        // private float m_bankingEfficiency = 0;
        // private float m_bankingMix = 0;
        // private float m_bankingTimescale = 0;

        //Hover and Buoyancy properties
        private float m_VhoverHeight = 0f;
        //        private float m_VhoverEfficiency = 0f;
        private float m_VhoverTimescale = 0f;
        private float m_VhoverTargetHeight = -1.0f;     // if <0 then no hover, else its the current target height
        private float m_VehicleBuoyancy = 0f;           //KF: m_VehicleBuoyancy is set by VEHICLE_BUOYANCY for a vehicle.
        // Modifies gravity. Slider between -1 (double-gravity) and 1 (full anti-gravity)
        // KF: So far I have found no good method to combine a script-requested .Z velocity and gravity.
        // Therefore only m_VehicleBuoyancy=1 (0g) will use the script-requested .Z velocity.

        //Attractor properties
        private float m_verticalAttractionEfficiency = 1.0f;        // damped
        private float m_verticalAttractionTimescale = 500f;         // Timescale > 300  means no vert attractor.          
        private btRigidBody m_body = null;
        BulletDotNETScene parent_scene;
        BulletDotNETPrim m_prim;
        public Vehicle Type
        {
            get { return m_type; }
        }

        internal void Step(float timestep, BulletDotNETScene _parent_scene)
        {
            parent_scene = _parent_scene;
            if (m_body == null || m_type == Vehicle.TYPE_NONE)
                return;
            if (m_prim.m_force.X == 0.0f && m_prim.m_force.Y == 0.0f && m_prim.m_force.Z == 0.0f && m_prim.IsPhysical)
            {
                //  keep track of where we stopped.  No more slippin' & slidin'
                if (!m_prim._zeroFlag)
                {
                    m_prim._zeroFlag = true;
                    m_prim.m_zeroPosition.X = m_prim.m_taintposition.X;
                    m_prim.m_zeroPosition.Y = m_prim.m_taintposition.Y;
                    m_prim.m_zeroPosition.Z = m_prim.m_taintposition.Z;
                    m_prim.m_velocity = Vector3.Zero;
                    m_prim.Body.setLinearVelocity(new btVector3(0, 0, 0));
                }
            }

            MoveLinear(timestep);
            MoveAngular(timestep);
            LimitRotation(timestep);
        }

        private void LimitRotation(float timestep)
        {
            btQuaternion rot = m_body.getWorldTransform().getRotation();
            bool changed = false;
            if (m_RollreferenceFrame != Quaternion.Identity)
            {
                if (rot.getX() >= m_RollreferenceFrame.X)
                    rot = new btQuaternion(rot.getX() - (m_RollreferenceFrame.X / 2), rot.getY(), rot.getZ(), rot.getW());
                if (rot.getX() <= -m_RollreferenceFrame.X)
                    rot = new btQuaternion(rot.getX() + (m_RollreferenceFrame.X / 2), rot.getY(), rot.getZ(), rot.getW());

                if (rot.getY() >= m_RollreferenceFrame.Y)
                    rot = new btQuaternion(rot.getX(), rot.getY() - (m_RollreferenceFrame.Y / 2), rot.getZ(), rot.getW());
                if (rot.getY() <= -m_RollreferenceFrame.Y)
                    rot = new btQuaternion(rot.getX(), rot.getY() + (m_RollreferenceFrame.Y / 2), rot.getZ(), rot.getW());
                
                changed = true;
            }
            if ((m_flags & VehicleFlag.LOCK_ROTATION) != 0)
            {
                rot = new btQuaternion(0,0,rot.getZ(),rot.getW());
                changed = true;
            }

            if (changed)
            {
                btTransform trans = m_body.getWorldTransform();
                trans.setRotation(rot);
                m_body.setWorldTransform(trans);
            }
        }

        private void MoveAngular(float timestep)
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
            btVector3 angularVelocity = m_body.getInterpolationAngularVelocity();
            //         Vector3 angularVelocity = Vector3.Zero;

            if (m_angularMotorApply > 0)
            {
                // ramp up to new value
                //   current velocity  +=                         error                       /    (time to get there / step interval)
                //                               requested speed            -  last motor speed
                m_angularMotorVelocity.X += (m_angularMotorDirection.X - m_angularMotorVelocity.X) / (m_angularMotorTimescale / timestep);
                m_angularMotorVelocity.Y += (m_angularMotorDirection.Y - m_angularMotorVelocity.Y) / (m_angularMotorTimescale / timestep);
                m_angularMotorVelocity.Z += (m_angularMotorDirection.Z - m_angularMotorVelocity.Z) / (m_angularMotorTimescale / timestep);

                m_angularMotorApply--;        // This is done so that if script request rate is less than phys frame rate the expected
                // velocity may still be acheived.
            }
            else
            {
                // no motor recently applied, keep the body velocity
                /*        m_angularMotorVelocity.X = angularVelocity.X;
                        m_angularMotorVelocity.Y = angularVelocity.Y;
                        m_angularMotorVelocity.Z = angularVelocity.Z; */

                // and decay the velocity
                m_angularMotorVelocity -= m_angularMotorVelocity / (m_angularMotorDecayTimescale / timestep);
            } // end motor section

            // Vertical attractor section
            Vector3 vertattr = Vector3.Zero;

            if (m_verticalAttractionTimescale < 300)
            {
                float VAservo = 0.2f / (m_verticalAttractionTimescale * timestep);
                // get present body rotation
                btQuaternion rot = m_body.getWorldTransform().getRotation();
                Quaternion rotq = new Quaternion(rot.getX(), rot.getY(), rot.getZ(), rot.getW());
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
                vertattr.X += bounce * angularVelocity.getX();
                vertattr.Y += bounce * angularVelocity.getY();

            } // else vertical attractor is off

            //        m_lastVertAttractor = vertattr;

            // Bank section tba
            // Deflection section tba

            // Sum velocities
            m_lastAngularVelocity = m_angularMotorVelocity + vertattr; // + bank + deflection

            if ((m_flags & (VehicleFlag.NO_DEFLECTION_UP)) != 0)
            {
                m_lastAngularVelocity.X = 0;
                m_lastAngularVelocity.Y = 0;
            }

            // apply friction
            Vector3 decayamount = Vector3.One / (m_angularFrictionTimescale / timestep);
            m_lastAngularVelocity -= m_lastAngularVelocity * decayamount;

            // Apply to the body
            m_body.setAngularVelocity(new btVector3(m_lastAngularVelocity.X, m_lastAngularVelocity.Y, m_lastAngularVelocity.Z));
        }

        private void MoveLinear(float timestep)
        {
            if (!m_linearMotorDirection.ApproxEquals(Vector3.Zero, 0.01f))  // requested m_linearMotorDirection is significant
            {
                
                // add drive to body
                Vector3 addAmount = m_linearMotorDirection / (m_linearMotorTimescale / timestep);
                m_lastLinearVelocityVector += (addAmount * 10);  // lastLinearVelocityVector is the current body velocity vector?

                // This will work temporarily, but we really need to compare speed on an axis
                // KF: Limit body velocity to applied velocity?
                if (Math.Abs(m_lastLinearVelocityVector.X) > Math.Abs(m_linearMotorDirectionLASTSET.X))
                    m_lastLinearVelocityVector.X = m_linearMotorDirectionLASTSET.X;
                if (Math.Abs(m_lastLinearVelocityVector.Y) > Math.Abs(m_linearMotorDirectionLASTSET.Y))
                    m_lastLinearVelocityVector.Y = m_linearMotorDirectionLASTSET.Y;
                if (Math.Abs(m_lastLinearVelocityVector.Z) > Math.Abs(m_linearMotorDirectionLASTSET.Z))
                    m_lastLinearVelocityVector.Z = m_linearMotorDirectionLASTSET.Z;

                // decay applied velocity
                Vector3 decayfraction = ((Vector3.One / (m_linearMotorDecayTimescale / timestep)));
                //Console.WriteLine("decay: " + decayfraction);
                m_linearMotorDirection -= m_linearMotorDirection * decayfraction * 0.5f;
                //Console.WriteLine("actual: " + m_linearMotorDirection);
            }
            else
            {        // requested is not significant
                // if what remains of applied is small, zero it.
                if (m_lastLinearVelocityVector.ApproxEquals(Vector3.Zero, 0.01f))
                    m_lastLinearVelocityVector = Vector3.Zero;
            }

            // convert requested object velocity to world-referenced vector
            m_dir = m_lastLinearVelocityVector;
            btQuaternion rot = m_body.getWorldTransform().getRotation();
            Quaternion rotq = new Quaternion(rot.getX(), rot.getY(), rot.getZ(), rot.getW());    // rotq = rotation of object
            m_dir *= rotq;                            // apply obj rotation to velocity vector

            // add Gravity andBuoyancy
            // KF: So far I have found no good method to combine a script-requested
            // .Z velocity and gravity. Therefore only 0g will used script-requested
            // .Z velocity. >0g (m_VehicleBuoyancy < 1) will used modified gravity only.
            Vector3 grav = Vector3.Zero;
            // There is some gravity, make a gravity force vector
            // that is applied after object velocity.
            float objMass =  m_prim.CalculateMass();
            // m_VehicleBuoyancy: -1=2g; 0=1g; 1=0g;
            //Rev: bullet does gravity internally
            grav.Z = -parent_scene.gravityz * objMass * m_VehicleBuoyancy; //parent_scene.gravityz/* * objMass*/ * (1f - m_VehicleBuoyancy);
            
            // Preserve the current Z velocity
            btVector3 pos = m_body.getWorldTransform().getOrigin();

            btVector3 newpos = pos;

            btVector3 vel_now = m_body.getVelocityInLocalPoint(newpos);
            m_dir.Z = vel_now.getZ();        // Preserve the accumulated falling velocity

            Vector3 accel = new Vector3(-(m_dir.X - m_lastLinearVelocityVector.X / 0.1f), -(m_dir.Y - m_lastLinearVelocityVector.Y / 0.1f), m_dir.Z - m_lastLinearVelocityVector.Z / 0.1f);
            Vector3 posChange = new Vector3();
            posChange.X = newpos.getX() - m_lastPositionVector.getX();
            posChange.Y = newpos.getY() - m_lastPositionVector.getY();
            posChange.Z = newpos.getZ() - m_lastPositionVector.getZ();
            double Zchange = Math.Abs(posChange.Z);
            btQuaternion Orientation2 = m_body.getWorldTransform().getRotation();
            if (m_BlockingEndPoint != Vector3.Zero)
            {
                if (newpos.getX() >= (m_BlockingEndPoint.X - (float)1))
                    newpos.setX(newpos.getX() - (posChange.X + 1));
                if (newpos.getY() >= (m_BlockingEndPoint.Y - (float)1))
                    newpos.setY(newpos.getY() - (posChange.Y + 1));
                if (newpos.getZ() >= (m_BlockingEndPoint.Z - (float)1))
                    newpos.setZ(newpos.getZ() - (posChange.Z + 1));
                if (newpos.getX() <= 0)
                    newpos.setX(newpos.getX() + (posChange.X + 1));
                if (newpos.getY() <= 0)
                    newpos.setY(newpos.getY() + (posChange.Y + 1));
            }

            if (newpos.getZ() < parent_scene.GetTerrainHeightAtXY(newpos.getX(), newpos.getY()))
                newpos.setZ(parent_scene.GetTerrainHeightAtXY(newpos.getX(), newpos.getY()) + 2);

            // Check if hovering
            if ((m_Hoverflags & (VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY | VehicleFlag.HOVER_GLOBAL_HEIGHT)) != 0)
            {
                float diff = (newpos.getZ() - m_VhoverTargetHeight);
                // We should hover, get the target height
                if ((m_Hoverflags & VehicleFlag.HOVER_WATER_ONLY) != 0)
                    m_VhoverTargetHeight = parent_scene.GetWaterLevel() + m_VhoverHeight;
                if ((m_Hoverflags & VehicleFlag.HOVER_TERRAIN_ONLY) != 0)
                    m_VhoverTargetHeight = parent_scene.GetTerrainHeightAtXY(pos.getX(), pos.getY()) + m_VhoverHeight;
                if ((m_Hoverflags & VehicleFlag.HOVER_GLOBAL_HEIGHT) != 0)
                    m_VhoverTargetHeight = m_VhoverHeight;

                if ((m_Hoverflags & VehicleFlag.HOVER_UP_ONLY) != 0)
                {
                    // If body is aready heigher, use its height as target height
                    if (newpos.getZ() > m_VhoverTargetHeight) m_VhoverTargetHeight = newpos.getZ();
                }
                if ((m_Hoverflags & VehicleFlag.LOCK_HOVER_HEIGHT) != 0)
                {
                    if (diff > .2 || diff < -.2)
                    {
                        newpos.setValue(newpos.getX(), newpos.getY(), m_VhoverTargetHeight);
                        btTransform trans = new btTransform(Orientation2, newpos);
                        m_body.setWorldTransform(trans);
                    }
                }
                else
                {
                    // Replace Vertical speed with correction figure if significant
                    if (Math.Abs(diff) > 0.01f)
                        m_dir.Z = -((diff * timestep * 50.0f) / m_VhoverTimescale);
                    else
                        m_dir.Z = 0f;
                }
            }

            if ((m_flags & (VehicleFlag.LIMIT_MOTOR_UP)) != 0)
            {
                //Start Experimental Values
                if (Zchange > .3)
                    grav.Z = (float)(grav.Z * 3);
                if (Zchange > .15)
                    grav.Z = (float)(grav.Z * 2);
                if (Zchange > .75)
                    grav.Z = (float)(grav.Z * 1.5);
                if (Zchange > .05)
                    grav.Z = (float)(grav.Z * 1.25);
                if (Zchange > .025)
                    grav.Z = (float)(grav.Z * 1.125);
                
                float terraintemp = parent_scene.GetTerrainHeightAtXY(pos.getX(), pos.getY());
                float postemp = (pos.getZ() - terraintemp);
                
                if (postemp > 2.5f)
                    grav.Z = (float)(grav.Z * 1.037125);
                //End Experimental Values
            }

            if ((m_flags & (VehicleFlag.NO_X)) != 0)
                m_dir.X = 0;
            if ((m_flags & (VehicleFlag.NO_Y)) != 0)
                m_dir.Y = 0;
            if ((m_flags & (VehicleFlag.NO_Z)) != 0)
                m_dir.Z = 0;

            m_lastPositionVector = new btVector3(m_prim.Position.X,m_prim.Position.Y,m_prim.Position.Z);
            // Apply velocity
            //if(m_dir != Vector3.Zero)
            //    m_body.setLinearVelocity(new btVector3(m_dir.X, m_dir.Y, m_dir.Z));
            m_body.applyCentralImpulse(new btVector3(m_dir.X, m_dir.Y, m_dir.Z));
            // apply gravity force
            //m_body.applyCentralImpulse(new btVector3(0, 0, 9.8f));

            Vector3 newpos2 = new Vector3(newpos.getX(), newpos.getY(), newpos.getZ());
            if (newpos2.X != m_prim.Position.X || newpos2.Y != m_prim.Position.Y || newpos2.Z != m_prim.Position.Z)
            {
                btTransform trans = new btTransform(Orientation2, newpos);
                m_body.setWorldTransform(trans);
            }
            m_prim.RequestPhysicsterseUpdate();

            // apply friction
            Vector3 decayamount = Vector3.One / (m_linearFrictionTimescale / timestep);
            m_lastLinearVelocityVector -= m_lastLinearVelocityVector * decayamount;
        }

        internal void Enable(btRigidBody pBody, BulletDotNETPrim prim)
        {
            m_prim = prim;
            m_body = pBody;
        }

        #region Parameter setting

        internal void ProcessFloatVehicleParam(Vehicle pParam, float pValue)
        {
            switch (pParam)
            {
                case Vehicle.ANGULAR_DEFLECTION_EFFICIENCY:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_angularDeflectionEfficiency = pValue;
                    break;
                case Vehicle.ANGULAR_DEFLECTION_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_angularDeflectionTimescale = pValue;
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
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_bankingEfficiency = pValue;
                    break;
                case Vehicle.BANKING_MIX:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_bankingMix = pValue;
                    break;
                case Vehicle.BANKING_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_bankingTimescale = pValue;
                    break;
                case Vehicle.BUOYANCY:
                    //if (pValue < -1f) pValue = -1f;
                    //if (pValue > 1f) pValue = 1f;
                    m_VehicleBuoyancy = pValue;
                    break;
                //                case Vehicle.HOVER_EFFICIENCY:
                //                    if (pValue < 0f) pValue = 0f;
                //                    if (pValue > 1f) pValue = 1f;
                //                    m_VhoverEfficiency = pValue;
                //                    break;
                case Vehicle.HOVER_HEIGHT:
                    m_VhoverHeight = pValue;
                    break;
                case Vehicle.HOVER_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    m_VhoverTimescale = pValue;
                    break;
                case Vehicle.LINEAR_DEFLECTION_EFFICIENCY:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_linearDeflectionEfficiency = pValue;
                    break;
                case Vehicle.LINEAR_DEFLECTION_TIMESCALE:
                    if (pValue < 0.01f) pValue = 0.01f;
                    // m_linearDeflectionTimescale = pValue;
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
                    // m_linearMotorOffset = new Vector3(pValue, pValue, pValue);
                    break;

            }
        }//end ProcessFloatVehicleParam

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
                    if (m_angularMotorDirection.X < -12.56f) m_angularMotorDirection.X = -12.56f;
                    if (m_angularMotorDirection.Y > 12.56f) m_angularMotorDirection.Y = 12.56f;
                    if (m_angularMotorDirection.Y < -12.56f) m_angularMotorDirection.Y = -12.56f;
                    if (m_angularMotorDirection.Z > 12.56f) m_angularMotorDirection.Z = 12.56f;
                    if (m_angularMotorDirection.Z < -12.56f) m_angularMotorDirection.Z = -12.56f;
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
                    // m_linearMotorOffset = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
                case Vehicle.BLOCK_EXIT:
                    m_BlockingEndPoint = new Vector3(pValue.X, pValue.Y, pValue.Z);
                    break;
            }
        }//end ProcessVectorVehicleParam

        internal void ProcessRotationVehicleParam(Vehicle pParam, Quaternion pValue)
        {
            switch (pParam)
            {
                case Vehicle.REFERENCE_FRAME:
                    // m_referenceFrame = pValue;
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
                    m_Hoverflags |= (VehicleFlag.HOVER_GLOBAL_HEIGHT | m_flags);
                if ((pParam & (int)VehicleFlag.HOVER_TERRAIN_ONLY) == (int)VehicleFlag.HOVER_TERRAIN_ONLY)
                    m_Hoverflags |= (VehicleFlag.HOVER_TERRAIN_ONLY | m_flags);
                if ((pParam & (int)VehicleFlag.HOVER_UP_ONLY) == (int)VehicleFlag.HOVER_UP_ONLY)
                    m_Hoverflags |= (VehicleFlag.HOVER_UP_ONLY | m_flags);
                if ((pParam & (int)VehicleFlag.HOVER_WATER_ONLY) == (int)VehicleFlag.HOVER_WATER_ONLY)
                    m_Hoverflags |= (VehicleFlag.HOVER_WATER_ONLY | m_flags);
                if ((pParam & (int)VehicleFlag.LIMIT_MOTOR_UP) == (int)VehicleFlag.LIMIT_MOTOR_UP)
                    m_flags |= (VehicleFlag.LIMIT_MOTOR_UP | m_flags);
                if ((pParam & (int)VehicleFlag.MOUSELOOK_BANK) == (int)VehicleFlag.MOUSELOOK_BANK)
                    m_flags |= (VehicleFlag.MOUSELOOK_BANK | m_flags);
                if ((pParam & (int)VehicleFlag.MOUSELOOK_STEER) == (int)VehicleFlag.MOUSELOOK_STEER)
                    m_flags |= (VehicleFlag.MOUSELOOK_STEER | m_flags);
                if ((pParam & (int)VehicleFlag.NO_DEFLECTION_UP) == (int)VehicleFlag.NO_DEFLECTION_UP)
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | m_flags);
                if ((pParam & (int)VehicleFlag.CAMERA_DECOUPLED) == (int)VehicleFlag.CAMERA_DECOUPLED)
                    m_flags |= (VehicleFlag.CAMERA_DECOUPLED | m_flags);
                if ((pParam & (int)VehicleFlag.NO_X) == (int)VehicleFlag.NO_X)
                    m_flags |= (VehicleFlag.NO_X);
                if ((pParam & (int)VehicleFlag.NO_Y) == (int)VehicleFlag.NO_Y)
                    m_flags |= (VehicleFlag.NO_Y);
                if ((pParam & (int)VehicleFlag.NO_Z) == (int)VehicleFlag.NO_Z)
                    m_flags |= (VehicleFlag.NO_Z);
                if ((pParam & (int)VehicleFlag.LOCK_HOVER_HEIGHT) == (int)VehicleFlag.LOCK_HOVER_HEIGHT)
                    m_Hoverflags |= (VehicleFlag.LOCK_HOVER_HEIGHT);
                if ((pParam & (int)VehicleFlag.NO_DEFLECTION) == (int)VehicleFlag.NO_DEFLECTION)
                    m_flags |= (VehicleFlag.NO_DEFLECTION);
                if ((pParam & (int)VehicleFlag.LOCK_ROTATION) == (int)VehicleFlag.LOCK_ROTATION)
                    m_flags |= (VehicleFlag.LOCK_ROTATION);
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
                    //                    m_VhoverEfficiency = 1;
                    m_VhoverTimescale = 10;
                    m_VehicleBuoyancy = 0;
                    // m_linearDeflectionEfficiency = 1;
                    // m_linearDeflectionTimescale = 1;
                    // m_angularDeflectionEfficiency = 1;
                    // m_angularDeflectionTimescale = 1000;
                    // m_bankingEfficiency = 0;
                    // m_bankingMix = 1;
                    // m_bankingTimescale = 10;
                    // m_referenceFrame = Quaternion.Identity;
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
                    //                    m_VhoverEfficiency = 0;
                    m_VhoverTimescale = 1000;
                    m_VehicleBuoyancy = 0;
                    // // m_linearDeflectionEfficiency = 1;
                    // // m_linearDeflectionTimescale = 2;
                    // // m_angularDeflectionEfficiency = 0;
                    // m_angularDeflectionTimescale = 10;
                    m_verticalAttractionEfficiency = 1f;
                    m_verticalAttractionTimescale = 10f;
                    // m_bankingEfficiency = -0.2f;
                    // m_bankingMix = 1;
                    // m_bankingTimescale = 1;
                    // m_referenceFrame = Quaternion.Identity;
                    m_Hoverflags &= ~(VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY | VehicleFlag.HOVER_GLOBAL_HEIGHT);
                    m_flags |= (VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_ROLL_ONLY |
                                VehicleFlag.LIMIT_MOTOR_UP);
                    m_Hoverflags |= (VehicleFlag.HOVER_UP_ONLY);
                    break;
                case Vehicle.TYPE_BOAT:
                    m_linearFrictionTimescale = new Vector3(10, 3, 2);
                    m_angularFrictionTimescale = new Vector3(10, 10, 10);
                    m_linearMotorDirection = Vector3.Zero;
                    m_linearMotorTimescale = 5;
                    m_linearMotorDecayTimescale = 60;
                    m_angularMotorDirection = Vector3.Zero;
                    m_angularMotorTimescale = 4;
                    m_angularMotorDecayTimescale = 4;
                    m_VhoverHeight = 0;
                    //                    m_VhoverEfficiency = 0.5f;
                    m_VhoverTimescale = 2;
                    m_VehicleBuoyancy = 1;
                    // m_linearDeflectionEfficiency = 0.5f;
                    // m_linearDeflectionTimescale = 3;
                    // m_angularDeflectionEfficiency = 0.5f;
                    // m_angularDeflectionTimescale = 5;
                    m_verticalAttractionEfficiency = 0.5f;
                    m_verticalAttractionTimescale = 5f;
                    // m_bankingEfficiency = -0.3f;
                    // m_bankingMix = 0.8f;
                    // m_bankingTimescale = 1;
                    // m_referenceFrame = Quaternion.Identity;
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
                    //                    m_VhoverEfficiency = 0.5f;
                    m_VhoverTimescale = 1000;
                    m_VehicleBuoyancy = 0;
                    // m_linearDeflectionEfficiency = 0.5f;
                    // m_linearDeflectionTimescale = 3;
                    // m_angularDeflectionEfficiency = 1;
                    // m_angularDeflectionTimescale = 2;
                    m_verticalAttractionEfficiency = 0.9f;
                    m_verticalAttractionTimescale = 2f;
                    // m_bankingEfficiency = 1;
                    // m_bankingMix = 0.7f;
                    // m_bankingTimescale = 2;
                    // m_referenceFrame = Quaternion.Identity;
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
                    //                    m_VhoverEfficiency = 0.8f;
                    m_VhoverTimescale = 10;
                    m_VehicleBuoyancy = 1;
                    // m_linearDeflectionEfficiency = 0;
                    // m_linearDeflectionTimescale = 5;
                    // m_angularDeflectionEfficiency = 0;
                    // m_angularDeflectionTimescale = 5;
                    m_verticalAttractionEfficiency = 1f;
                    m_verticalAttractionTimescale = 100f;
                    // m_bankingEfficiency = 0;
                    // m_bankingMix = 0.7f;
                    // m_bankingTimescale = 5;
                    // m_referenceFrame = Quaternion.Identity;
                    m_Hoverflags &= ~(VehicleFlag.HOVER_WATER_ONLY | VehicleFlag.HOVER_TERRAIN_ONLY |
                        VehicleFlag.HOVER_UP_ONLY);
                    m_flags &= ~(VehicleFlag.NO_DEFLECTION_UP | VehicleFlag.LIMIT_MOTOR_UP);
                    m_flags |= (VehicleFlag.LIMIT_ROLL_ONLY);
                    m_Hoverflags |= (VehicleFlag.HOVER_GLOBAL_HEIGHT);
                    break;

            }
        }//end SetDefaultsForType
        #endregion
    }
}
