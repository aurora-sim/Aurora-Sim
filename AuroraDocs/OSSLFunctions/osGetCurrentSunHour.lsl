// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetCurrentSunHour.lsl
// Script Author:   WSM
// Threat Level:    None
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osGetCurrentSunHour
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line:  float osGetCurrentSunHour();
//
// Example of osGetCurrentSunHour.
//
default
{
    state_entry()
    {
         llSay(0,"Touch to see osGetCurrentSunHour return the current sun hour setting"); 
    }
    touch_start(integer total_number)
    {
        llSay(0, "Current sun hour: ["+(string)osGetCurrentSunHour()+"]");
    }
}