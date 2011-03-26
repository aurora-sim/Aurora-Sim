// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetPrimFloatOnWater.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    VeryLow
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetPrimFloatOnWater
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public void osSetPrimFloatOnWater(int floatYN)
// Inworld Script Line: osSetPrimFloatOnWater(integer floatYN);
//
// Example of osSetPrimFloatOnWater
//
integer iFloatYN = FALSE;
default
{
    state_entry()
    {
        llSay(0,"Touch to see osSetPrimFloatOnWater work.  Sets TRUE / FALSE");
    }
    touch_end(integer num)
    {
        if(iFloatYN)
        {
            iFloatYN = FALSE;
            llSay(0,"osSetPrimFloatOnWater = FALSE");
            osSetPrimFloatOnWater(iFloatYN);
        }
        else
        {
            iFloatYN = TRUE;
            llSay(0,"osSetPrimFloatOnWater = TRUE");
            osSetPrimFloatOnWater(iFloatYN);
        }
    }
}