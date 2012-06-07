// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetProjectionParams.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    high
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetProjectionParams
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// 
// 
// ================================================================
// C# Source Line:      public void osSetProjectionParams(bool projection, LSL_Key texture, double fov, double focus, double amb)
// C# Source Line:      public void osSetProjectionParams(LSL_Key prim, bool projection, LSL_Key texture, double fov, double focus, double amb)
// Inworld Script Line: osSetProjectionParams(integer projection, key texture, float FieldOfVision, float focus, float ambience); 
//
// Example of osSetProjectionParams
//
integer project = TRUE;
key textureID = "3d7a5b98-5b6c-46ff-9fd5-2c1ccc8d703c";
float fov = 1.0;
float focus = 5.0;
float ambience = 1.0;

default
{
    state_entry()
    {
        llSay(0,"Touch to see osSetProjectionParams work.");
    } 
    touch_start(integer number)
    { 
        llSay(0,"On");
        llSetPos(llGetPos() + <0,0,1>);
        llSetPrimitiveParams([PRIM_POINT_LIGHT,TRUE, <1,1,1>,1.0,10.0,0.75 ]);
        osSetProjectionParams(project, textureID, fov, focus, ambience);
    }
}