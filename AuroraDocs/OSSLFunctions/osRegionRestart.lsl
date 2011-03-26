// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osRegionRestart.lsl
// Script Author:
// Threat Level:    High
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osRegionRestart
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public int osRegionRestart(double seconds)
// Inworld Script Line: osRegionRestart(float seconds);
//
// Example of osRegionRestart
// SPECIAL NOTES:
//    Minimum Value is 15 seconds
//    Possibly Unstable Function due to ongoing OpenSim Changes.
//
default
{
    state_entry()
    {
        llSay(0,"Touch to Restart this region");
    }
    touch_start(integer total_num)
    {
        float seconds = 60.0;
        string message = "This Region is restarting in "+(string)seconds+" Seconds";
        llSay(0,message);
        osRegionRestart(seconds);
    }
}
