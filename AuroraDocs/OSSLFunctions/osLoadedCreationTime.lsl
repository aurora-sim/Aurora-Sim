// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osLoadedCreationTime.lsl
// Script Author:
// Threat Level:    Low
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osLoadedCreationTime
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    string osLoadedCreationTime();
//
// Example of osLoadedCreationTime() 
// Returns a string containing the time that Region was first created. Example "6:16:31 AM".
default
{
    state_entry() // display @ start
    {
        llSay(0, "Example osLoadedCreationTime()");
    }
    touch_end(integer num)
    {
        llSay(0,"osLoadedCreationTime is: "+osLoadedCreationTime());
    }
}
