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
// Inworld Script Line: osSetProjectionParams(integer projection, key texture, float FieldOfVision float focus, float ambience); 
//
// Example of osSetProjectionParams
//
string ProjParams = "TRUE, <UUID>, 10.0, 5.0, 7.5";
default
{
    state_entry()
    {
        llSay(0,"Touch to see osSetProjectionParams work.");
    }
    touch_end(integer num)
    {
        osSetProjectionParams(ProjParams);
    }
}