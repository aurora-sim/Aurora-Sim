// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetAgentIP.lsl
// Script Author:   
// Threat Level:    High
// Script Source:   Reference http://opensimulator.org/wiki/osGetAgentIP
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line:    osGetAgentIP(key agent);
//
// Example of osGetAgentIP
//
default
{
    state_entry()
    {
        llSay(0, "Touch to get see osGetAgentIP tell you your IP Address");
    }
    touch_start(integer total_number)
    {
        key Agent = llDetectedKey(0);
        llInstantMessage(Agent, "Your IP is : "+ osGetAgentIP(Agent));
    }
}
