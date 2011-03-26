// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osNpcCreate.lsl
// Script Author:
// Threat Level:    High
// Script Source:   http://opensimulator.org/wiki/osNpcCreate
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public LSL_Key osNpcCreate(string firstname, string lastname, LSL_Vector position, LSL_Key cloneFrom)
// Inworld Script Line:     key osNpcCreate(string firstname, string lastname, vector position, key cloneFrom);
//
// Example of osNpcCreate
// This function creates a NPC(Non Player Character) clone from an already existing avatar UUID Key.
//
default
{
    state_entry()
    {
        llSay(0,"Touch to create A Non Player Character Bot Clone of yourself using osNpcCreate");
    }
    touch_start()
    {
        key npc;
        key cloneFrom = llDetectedKey(0);
        vector position = llGetPos();
        float posY = (position.y + 1.0); //placement + 1m on y
        npc = osNpcCreate("NPC-Char", "Bot", <position.x, posY, position.z>, cloneFrom);
    }
}
