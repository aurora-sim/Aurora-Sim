// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetGridNick.lsl
// Script Author:
// Threat Level:    Moderate
// Script Source:   REFERENCE http://opensimulator.org/wiki/osGetGridNick
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:  string osGetGridNick();
//
// Example of osGetGridNick()
// returns the value of GridNick from GridInfo
//
//
default
{
    state_entry()
    {
         llSay(0,"Touch to see osGetGridNick return the value set for the gridnick "); 
    }
    touch_end(integer num)
    {
        llSay(0, "Grid Nick = "+osGetGridNick());
    }
}
