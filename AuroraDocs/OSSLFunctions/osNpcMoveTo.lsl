// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osNpcMoveTo.lsl
// Script Author:
// Threat Level:    High
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osNpcMoveTo
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public void osNpcMoveTo(LSL_Key npc, LSL_Vector position)
// Inworld Script Line:     osNpcMoveTo(key npc, vector position);
//
// Example of osNpcMoveTo
// This function moves the NPC(Non Player Character) clone positions.
//
default
{
    state_entry()
    {
        llSay(0,"Touch to create A Non Player Character Bot Clone of yourself using osNpcCreate\nthen have it move +2m on X using osNpcMoveTo");
    }
    touch_start()
    {
        key npc;
        key cloneFrom = llDetectedKey(0);
        vector position = llGetPos();
        float posY = (position.y + 1.0); //placement + 1m on y
        // Create the NPC
        npc = osNpcCreate("NPC-Char", "Bot", <position.x, posY, position.z>, cloneFrom);
        // setup position.x + 2m, and move the NPC 
        float posX = (position.x + 2.0); //placement + 2m on x
        osNpcMoveTo(npc, <posX, posY,position.z>);
    }
}
