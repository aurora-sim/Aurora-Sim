//Simple boat that works with the new SLFeeling Physics Engine (mod from encog)

float forward_power = 25; 
float reverse_power = -15;
float turning_ratio = 5.0; 
string sit_message = "Ride"; 
string not_owner_message = "You are not the owner of this vehicle ..."; 

default
{
    state_entry()
    {
        llSetSitText(sit_message);
        llSitTarget(<0.2,0,0.45>, ZERO_ROTATION );
        
        llSetCameraEyeOffset(<-12, 0.0, 5.0>);
        llSetCameraAtOffset(<1.0, 0.0, 2.0>);
        
        llSetVehicleFlags(0);
        llSetVehicleType(VEHICLE_TYPE_BOAT);
        llSetVehicleFlags(VEHICLE_FLAG_HOVER_UP_ONLY | VEHICLE_FLAG_HOVER_WATER_ONLY);
        llSetVehicleVectorParam( VEHICLE_LINEAR_FRICTION_TIMESCALE, <1, 1, 1> );
        llSetVehicleFloatParam( VEHICLE_ANGULAR_FRICTION_TIMESCALE, 2 );
        
    llSetVehicleVectorParam(VEHICLE_LINEAR_MOTOR_DIRECTION, <0, 0, 0>);
    llSetVehicleFloatParam(VEHICLE_LINEAR_MOTOR_TIMESCALE, 1);
    llSetVehicleFloatParam(VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE, 0.05);
        
        llSetVehicleFloatParam( VEHICLE_ANGULAR_MOTOR_TIMESCALE, 1 );
        llSetVehicleFloatParam( VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE, 1 );
        llSetVehicleFloatParam( VEHICLE_HOVER_HEIGHT, 0.5);
        llSetVehicleFloatParam( VEHICLE_HOVER_EFFICIENCY,1 );
        llSetVehicleFloatParam( VEHICLE_HOVER_TIMESCALE, 2.0 );
        llSetVehicleFloatParam( VEHICLE_BUOYANCY, 1 );
        llSetVehicleFloatParam( VEHICLE_LINEAR_DEFLECTION_EFFICIENCY, 0.5 );
        llSetVehicleFloatParam( VEHICLE_LINEAR_DEFLECTION_TIMESCALE, 3 );
        llSetVehicleFloatParam( VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY, 0.5 );
        llSetVehicleFloatParam( VEHICLE_ANGULAR_DEFLECTION_TIMESCALE, 10 );
        llSetVehicleFloatParam( VEHICLE_VERTICAL_ATTRACTION_TIMESCALE, 4 );
        llSetVehicleFloatParam( VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY, 0.5 );
        llSetVehicleFloatParam( VEHICLE_BANKING_EFFICIENCY, 0.9 );
        llSetVehicleFloatParam( VEHICLE_BANKING_MIX, 1 );
        llSetVehicleFloatParam( VEHICLE_BANKING_TIMESCALE, 7 );
        llSetVehicleRotationParam( VEHICLE_REFERENCE_FRAME, ZERO_ROTATION );
    }
    
    changed(integer change)
    {
        
        
        if (change & CHANGED_LINK)
        {
            
            key agent = llAvatarOnSitTarget();
            if (agent)
            {                
                if (agent != llGetOwner())
                {
                    llSay(0, not_owner_message);
                    llUnSit(agent);
                    llPushObject(agent, <0,0,50>, ZERO_VECTOR, FALSE);
                }
                else
                {
                                        
                    llMessageLinked(LINK_ALL_CHILDREN , 0, "start", NULL_KEY);
                    llSleep(.4);
                    llSetStatus(STATUS_PHYSICS, TRUE);
                    llSleep(.1);
                    llRequestPermissions(agent, PERMISSION_TRIGGER_ANIMATION | PERMISSION_TAKE_CONTROLS);

                }
            }
            else
            {
                llSetStatus(STATUS_PHYSICS, FALSE);
                llSleep(.1);
                llMessageLinked(LINK_ALL_CHILDREN , 0, "stop", NULL_KEY);
                llSleep(.4);
                llReleaseControls();
                llTargetOmega(<0,0,0>,PI,0);
                
                llResetScript();
            }
        }
        
    }
    
    run_time_permissions(integer perm)
    {
        if (perm)
        {
            llTakeControls(CONTROL_FWD | CONTROL_BACK | CONTROL_DOWN | CONTROL_UP | CONTROL_RIGHT | 
                            CONTROL_LEFT | CONTROL_ROT_RIGHT | CONTROL_ROT_LEFT, TRUE, FALSE);
        }
    }
    
    control(key id, integer level, integer edge)
    {
        integer reverse=1;
        vector angular_motor;
        
        vector vel = llGetVel();
        float speed = llVecMag(vel);

        if(level & CONTROL_FWD)
        {
            llSetVehicleVectorParam(VEHICLE_LINEAR_MOTOR_DIRECTION, <forward_power,0,0>);
            reverse=1;
        }
        if(level & CONTROL_BACK)
        {
            llSetVehicleVectorParam(VEHICLE_LINEAR_MOTOR_DIRECTION, <reverse_power,0,0>);
            reverse = -1;
        }

        if(level & (CONTROL_RIGHT|CONTROL_ROT_RIGHT))
        {
            angular_motor.z -= speed / turning_ratio * reverse;
            angular_motor.x += 20;
        }
        
        if(level & (CONTROL_LEFT|CONTROL_ROT_LEFT))
        {
            angular_motor.z += speed / turning_ratio * reverse;
            angular_motor.x -= 20;
        }

        llSetVehicleVectorParam(VEHICLE_ANGULAR_MOTOR_DIRECTION, angular_motor);

    }   
    
} 