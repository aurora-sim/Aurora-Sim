// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetThreatlevel.lsl
// Script Author:
// Threat Level:    High
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:     public string osGetThreatlevel(string key)
// Inworld Script Line:    string osGetThreatlevel(string key);
//
// Example of osGetThreatlevel(string key)
//
default
{
    state_entry()
    {
        llSay(0, "FunctionThreatLevel = " + osGetThreatLevel("ThreatLevel"));
    }
}