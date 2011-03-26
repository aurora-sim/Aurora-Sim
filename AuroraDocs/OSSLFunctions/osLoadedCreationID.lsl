// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osLoadedCreationID.lsl
// Script Author:
// Threat Level:    Low
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osLoadedCreationID
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    string osLoadedCreationID();
//
// Example of osLoadedCreationID()
// This function returns a string containing the UUID that a sim was created with.  
default
{
    state_entry() // display @ start
    {
        llSay(0, "Touch to see Example osLoadedCreationID()");
    }
    touch_end(integer num)
    {
        llSay(0,"osLoadedCreationID: " + osLoadedCreationID());
    }
}
