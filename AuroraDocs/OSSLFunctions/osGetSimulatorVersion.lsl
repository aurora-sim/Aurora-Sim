// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetSimulatorVersion.lsl
// Script Author:   
// Threat Level:    High
// Script Source:   http://opensimulator.org/wiki/OsGetSimulatorVersion
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    string osGetSimulatorVersion();
//
// Example of osGetSimulatorVersion()
//
default
{
    state_entry() // display @ start
    {
        llSay(0, "Touch me to get the Simulator Version Information using osGetSimulatorVersion");
    }
    touch_end(integer num) // Tell toucher our version
    {
        llInstantMessage(llDetectedKey(0), "Simulator Version: "+osGetSimulatorVersion());
    }
}
