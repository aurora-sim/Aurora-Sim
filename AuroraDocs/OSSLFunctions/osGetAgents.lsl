// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetAgents.lsl
// Script Author:   WSM
// Threat Level:    None
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osGetAgents
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// C# Source Line:    public LSL_List osGetAgents()
// Inworld Script Line:  list osGetAgents();
//
// Example of osGetAgents
//
default
{
    state_entry()
    {
        llSay(0, "Touch to get a List of Avatars on this Region using osGetAgents");
    }
    touch_start(integer num)
    {
        llSay(0, "The Avatars located here are: "+ llList2CSV(osGetAgents()));
    }
}
