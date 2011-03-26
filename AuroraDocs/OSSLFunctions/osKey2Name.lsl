// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osKey2Name.lsl
// Script Author:
// Threat Level:    Low
// Script Source:   http://opensimulator.org/wiki/osKey2Name
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:     public string osKey2Name(string id)
// Inworld Script Line:    string osKey2Name(key id);
//
// Example of osKey2Name(id)
//
default
{
    state_entry() // display @ start
    {
        llSay(0, "UUID/Key 2 Name Display Ready");
    }
    touch_end(integer num) // Tell toucher Their UUID & Name
    {
        key avatar = llDetectedKey(0);
        llInstantMessage(avatar, "Your Key is: "+(string)avatar+" Name is: "+osKey2Name(avatar));
    }
}
