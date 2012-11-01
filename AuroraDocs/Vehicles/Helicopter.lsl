//This one works out of the box with the new Aurora-Sim SL Feeling Physics Engine
// Script Name: Helicopter.lsl
// Author: Encog Dod
//Helicopter

// Downloaded from : http://www.free-lsl-scripts.com/cgi/freescripts.plx?ID=124

// This program is free software; you can redistribute it and/or modify it.
// Additional Licenes may apply that prevent you from selling this code
// and these licenses may require you to publish any changes you make on request.
//
// There are literally thousands of hours of work in these scripts. Please respect
// the creators wishes and Copyright law and follow their license requirements.
//
// This header and License information included herein must be included in any script you give out or use.
// Licenses may also be included in the script or comments by the original author, in which case
// the authors license must be followed, and  their licenses override any licenses outlined in this header.
//
// You cannot attach a license to any of these scripts to make any license more or less restrictive.
//
// All scripts by avatar Ferd Frederix, unless stated otherwise in the script, are licensed as Creative Commons By Attribution and Non-Commercial.
// Commerical use is NOT allowed for resale of my scripts.  See http://creativecommons.org/licenses/by-nc/3.0/ for more details and the actual license agreement.
// This means you cannot sell my  scripts unless they are FREE.  
// Scripts by Ferd Frederix may be reused and sold when included with a build that actually uses these scripts. Putting my script in a prim and reselling it does not constitute a build.
// I reserve the right to detemine, at my sole discretion, what constitutes a build. My script source code must
// always be freely available, which means it must be MOD, COPY and TRANSFER.
// For any reuse or distribution, you must make clear to others the license terms of my works.
//
// A GNU license, if attached by the author, also means the original code must be Open Source.   If you modify it, it must be
// freely available to others.   You may not impose a license fee, royalty, or other charge for exercise of rights granted under this License
// Modifications can be made and products sold with the scripts in them.
// The script itself must be marked as MOD/COPY/XFER or otherwise made available by you as Open Source.
// You cannot attach a license to make a GNU License more restrictive.
// see http://www.gnu.org/copyleft/gpl.html

// Creative Commons licenses apply to all scripts from the Second Life - Commerical use is allowed.
// The scripts are Copyrighted by Linden Lab, and licensed under the Creative Commons Attribution-Share Alike 3.0 License.
// See http://www.creativecommons.org/licenses/by-sa/3.0/
// Notice — For any reuse or distribution, you must make clear to others the license terms of this work.
// The best way to do this is with a link to this web page. 

// You must leave any author credits and any following headers intact in any script you use or publish.
///////////////////////////////////////////////////////////////////////////////////////////////////
// If you don't like these restrictions and licenses, then don't use these scripts.
//////////////////////// ORIGINAL AUTHORS CODE BEGINS ////////////////////////////////////////////

// From the book:
//
// Scripting Recipes for Second Life
// by Jeff Heaton (Encog Dod in SL)
// ISBN: 160439000X
// Copyright 2007 by Heaton Research, Inc.
//
// This script may be freely copied and modified so long as this header
// remains unmodified.
//
// For more information about this book visit the following web site:
//
// http://www.heatonresearch.com/articles/series/22/

float forward_power = 15; //Power used to go forward (1 to 30)
float reverse_power = -15; //Power ued to go reverse (-1 to -30)
float turning_ratio = 2.0; //How sharply the vehicle turns. Less is more sharply. (.1 to 10)
string sit_message = "Ride"; //Sit message
string not_owner_message = "You are not the owner of this vehicle ..."; //Not owner message
float VERTICAL_THRUST = 7;
float ROTATION_RATE = 2.0;      //  Rate of turning  

resetY()
{
    rotation rot = llGetRot();
    llSetRot(rot);
}

default
{
    state_entry()
    {
        llSetSitText(sit_message);
        // forward-back,left-right,updown
        llSitTarget(<0.2,0,0.45>, ZERO_ROTATION );
        
        llSetCameraEyeOffset(<-8, 0.0, 5.0>);
        llSetCameraAtOffset(<1.0, 0.0, 2.0>);
        
        llPreloadSound("helicopter_run");
        
        //car
       llSetVehicleType(VEHICLE_TYPE_AIRPLANE);

       llSetVehicleFloatParam(VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY, 0.1);
       llSetVehicleFloatParam(VEHICLE_LINEAR_DEFLECTION_EFFICIENCY, 0.1);
       llSetVehicleFloatParam(VEHICLE_ANGULAR_DEFLECTION_TIMESCALE, 10);
       llSetVehicleFloatParam(VEHICLE_LINEAR_DEFLECTION_TIMESCALE, 10);

       llSetVehicleFloatParam(VEHICLE_LINEAR_MOTOR_TIMESCALE, 0.2);
       llSetVehicleFloatParam(VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE, 10);
       llSetVehicleFloatParam(VEHICLE_ANGULAR_MOTOR_TIMESCALE, 0.2);
       llSetVehicleFloatParam(VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE, 0.1);

       llSetVehicleVectorParam(VEHICLE_LINEAR_FRICTION_TIMESCALE, <1,1,1>);
       llSetVehicleVectorParam(VEHICLE_ANGULAR_FRICTION_TIMESCALE, <1,1000,1000>);

       llSetVehicleFloatParam(VEHICLE_BUOYANCY, 0.9);

        llSetVehicleFloatParam( VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY, 1 );
        llSetVehicleFloatParam( VEHICLE_VERTICAL_ATTRACTION_TIMESCALE, 2 );

        llSetVehicleFloatParam( VEHICLE_BANKING_EFFICIENCY, 1 );
        llSetVehicleFloatParam( VEHICLE_BANKING_MIX, 0.5 );
        llSetVehicleFloatParam( VEHICLE_BANKING_TIMESCALE, .5 );
        
        
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
                    llSetStatus(STATUS_ROTATE_Y,TRUE);
                    llSleep(.1);
                    llRequestPermissions(agent, PERMISSION_TRIGGER_ANIMATION | PERMISSION_TAKE_CONTROLS);

                    llLoopSound("helicopter_run",1);
                }
            }
            else
            {
                llStopSound();
                llMessageLinked(LINK_ALL_CHILDREN , 0, "stop", NULL_KEY);
                
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
        if (perm) {
            llTakeControls(CONTROL_FWD | CONTROL_BACK | CONTROL_RIGHT | CONTROL_LEFT | CONTROL_ROT_RIGHT | CONTROL_ROT_LEFT | CONTROL_UP | CONTROL_DOWN, TRUE, FALSE);
        }
    }
    
    control(key id, integer level, integer edge)
    {
        vector angular_motor;
        

        // going forward, or stop going forward
        if(level & CONTROL_FWD)
        {
            llSetVehicleVectorParam(VEHICLE_LINEAR_MOTOR_DIRECTION, <forward_power,0,0>);
        } else if(edge & CONTROL_FWD)
        {
            llSetVehicleVectorParam(VEHICLE_LINEAR_MOTOR_DIRECTION, <0,0,0>);
        }
        
        
        // going back, or stop going back
        if(level & CONTROL_BACK)
        {
            llSetVehicleVectorParam(VEHICLE_LINEAR_MOTOR_DIRECTION, <reverse_power,0,0>);
        }
        else if(edge & CONTROL_BACK)
        {
            llSetVehicleVectorParam(VEHICLE_LINEAR_MOTOR_DIRECTION, <0,0,0>);
        }
        
        // turning
        if(level & (CONTROL_RIGHT|CONTROL_ROT_RIGHT))
        {
            angular_motor.x += 25;
        }
        
        if(level & (CONTROL_LEFT|CONTROL_ROT_LEFT))
        {
            angular_motor.x -= 25;
        }
        
        
        // going up or stop going up
        if(level & CONTROL_UP) {
            llSetVehicleVectorParam(VEHICLE_LINEAR_MOTOR_DIRECTION, <0,0,VERTICAL_THRUST>);
        } else if (edge & CONTROL_UP) {
            llSetVehicleVectorParam(VEHICLE_LINEAR_MOTOR_DIRECTION, <0,0,0>);
        }
        
        // going down or stop going down
        
        if(level & CONTROL_DOWN) {
            llSetVehicleVectorParam(VEHICLE_LINEAR_MOTOR_DIRECTION, <0,0,-VERTICAL_THRUST>);
        } else if (edge & CONTROL_DOWN) {
            llSetVehicleVectorParam(VEHICLE_LINEAR_MOTOR_DIRECTION, <0,0,0>);
        }

        angular_motor.y = 0;
        llSetVehicleVectorParam(VEHICLE_ANGULAR_MOTOR_DIRECTION, angular_motor);
        


    } //end control   
    
    
    
} //end default


