// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetRegionWaterHeight.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    High
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetRegionWaterHeight
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// 
// ================================================================
// C# Source Line:      public void    osSetRegionWaterHeight(double height)
// Inworld Script Line: osSetRegionWaterHeight(float height); 
//
// Example of osSetRegionWaterHeight
//
float height = 20.0;          // default is 20.0 m
default
{
    state_entry()
    {
        llSay(0,"Touch to see osSetRegionWaterHeight work.");
    }
    touch_end(integer num)
    {
        if(height < 30.0) height = height+2.0;
        else height = 20.0;
        llOwnerSay("Water Height = ["+(string)height+"]");
        osSetRegionWaterHeight(height);
    }
}