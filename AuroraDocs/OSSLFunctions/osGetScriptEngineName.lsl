// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetScriptEngineName.lsl
// Script Author:   WSM
// Threat Level:    High
// Script Source:   http://opensimulator.org/wiki/OsGetScriptEngineName
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    string osGetScriptEngineName();
//
// Example of osGetScriptEngineName()
//
default
{
    state_entry()
    {
        llSay(0,"Touch to get Script Engine Name");
    }
    touch_end(integer total_num)
    {
        llSay(0,"The Script Engine Name is: "+osGetScriptEngineName());
    }
}