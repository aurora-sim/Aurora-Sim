// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osLoadedCreationDate.lsl
// Script Author:
// Threat Level:    Low
// Script Source:   http://opensimulator.org/wiki/osLoadedCreationDate
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    string osLoadedCreationDate();
//
// Example of osLoadedCreationDate()
// Returns a string containing the date that a sim was first created such as "Monday, December 07, 2009". 
default
{
    state_entry() // display @ start
    {
        llSay(0, "Touch to see Example osLoadedCreationDate()");
    }
    touch_end(integer num)
    {
        llSay(0,"osLoadedCreationDate is: "+osLoadedCreationDate());
    }
}
