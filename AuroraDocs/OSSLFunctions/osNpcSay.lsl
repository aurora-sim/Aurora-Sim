// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osNpcSay.lsl
// Script Author:
// Threat Level:    High
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osNpcSay
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public void osParcelJoin(LSL_Vector pos1, LSL_Vector pos2)
// Inworld Script Line:     osNpcSay(key npc, string message);
//
// Example of osNpcSay
// This function allows an NPC(Non Player Character) to say something.
//
default
{
    state_entry()
    {
        llSay(0,"Touch to have the Non Player Character Bot created with osNpcCreate, say something");
    }
    touch_start()
    {
        key npc = "00000000-0000-0000-0000-000000000000";
        // Use the UUID of the Clone you made with osNpcCreate or osNpcMove
        if(npc != "00000000-0000-0000-0000-000000000000") osNpcSay(npc, "I'm an NPC Bot, Hello "+llDetectedName(0);
        else llSay(0,"Use the UUID of the Clone you made with osNpcCreate or osNpcMove");
    }
}
