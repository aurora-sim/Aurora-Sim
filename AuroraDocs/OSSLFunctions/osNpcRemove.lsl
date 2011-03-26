// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osNpcRemove.lsl
// Script Author:
// Threat Level:    High
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osNpcRemove
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public void osNpcRemove(LSL_Key npc)
// Inworld Script Line:     osNpcRemove(key npc);
//
// Example of osNpcRemove
// This function removes a NPC(Non Player Character) clone from the Region.
//
default
{
    state_entry()
    {
        llSay(0,"Touch remove Non Player Character Bot created with osNpcCreate");
    }
    touch_start()
    {
        key npc = "00000000-0000-0000-0000-000000000000";
        // Use the UUID of the Clone you made with osNpcCreate or osNpcMove
        if(npc != "00000000-0000-0000-0000-000000000000") osNpcRemove(npc);
        else llSay(0,"Use the UUID of the Clone you made with osNpcCreate or osNpcMove");
    }
}
