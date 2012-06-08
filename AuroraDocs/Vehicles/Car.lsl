//Simple car that works with the new SLFeeling Physics Engine (mod from encog)

float forward_power = 15; 
float reverse_power = -15; 
float turning_ratio = 2.0; 
string sit_message = "Ride"; 
string not_owner_message = "You are not the owner of this vehicle ..."; 

default
{
    state_entry()
    {
        llSetSitText(sit_message);
        llSitTarget(<0.2,0,0.45>, ZERO_ROTATION );
        
        llSetCameraEyeOffset(<-8, 0.0, 5.0>);
        llSetCameraAtOffset(<1.0, 0.0, 2.0>);
              
        
        llSetVehicleType(VEHICLE_TYPE_CAR);
        llSetVehicleFloatParam(VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY, 0.1);
        llSetVehicleFloatParam(VEHICLE_LINEAR_DEFLECTION_EFFICIENCY, 0.2);
        llSetVehicleFloatParam(VEHICLE_ANGULAR_DEFLECTION_TIMESCALE, 0.2);
        llSetVehicleFloatParam(VEHICLE_LINEAR_DEFLECTION_TIMESCALE, 0.10);
        llSetVehicleFloatParam(VEHICLE_LINEAR_MOTOR_TIMESCALE, 0.2);
        llSetVehicleFloatParam(VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE, 0.2);
        llSetVehicleFloatParam(VEHICLE_ANGULAR_MOTOR_TIMESCALE, 0.1);
        llSetVehicleFloatParam(VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE, 0.5);
        
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
                                
                    
                    llMessageLinked(LINK_ALL_CHILDREN , 0, "WHEEL_DRIVING", NULL_KEY);
                    llSleep(.4);
                    llSetStatus(STATUS_PHYSICS, TRUE);
                    llSleep(.1);
                    llRequestPermissions(agent, PERMISSION_TRIGGER_ANIMATION | PERMISSION_TAKE_CONTROLS);

                    
                }
            }
            else
            {
                llStopSound();
                
                llSetStatus(STATUS_PHYSICS, FALSE);
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
        }
        
        if(level & (CONTROL_LEFT|CONTROL_ROT_LEFT))
        {
            angular_motor.z += speed / turning_ratio * reverse;
        }

        llSetVehicleVectorParam(VEHICLE_ANGULAR_MOTOR_DIRECTION, angular_motor);

    }    
    
    
} 