// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetGridName.lsl
// Script Author:
// Threat Level:    Moderate
// Script Source:   REFERENCE http://opensimulator.org/wiki/osGetGridName
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:  string osGetGridName();
//
// Example of osGetGridName()
// returns the value of GridName from GridInfo
//
default
{
    state_entry()
    {
         llSay(0,"Touch to see osGetGridName return the value set for the Grid Name "); 
    }
    touch_end(integer num)
    {
        llSay(0, "Grid Name = "+osGetGridName());
    }
}
