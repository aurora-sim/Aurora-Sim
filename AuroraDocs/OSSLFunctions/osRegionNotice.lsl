// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osRegionNotice.lsl
// Script Author:
// Threat Level:    VeryHigh
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osRegionNotice
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public void osRegionNotice(string msg)
// Inworld Script Line:     osRegionNotice(string message);
//
// Example of osRegionNotice
//
default
{
    state_entry()
    {
        llSay(0,"Touch to send a Notice to the region");
    }
    touch_start(integer total_num)
    {
        string message = "This is a test Notice to this region using osRegionNotice";
        osRegionNotice(message);
    }
}
