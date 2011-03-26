// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetNotecard.lsl
// Script Author:
// Threat Level:    VeryHigh
// Script Source:   http://opensimulator.org/wiki/osGetNotecard
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    string osGetNotecard(string name);
//
// Example of osGetNotecard(name) 
// 
default
{
    state_entry()
    {
         llSay(0,"Touch to see osGetNotecard read in a notecard and display the text retrieved"); 
    }
    touch_end(integer num)
    {
        // get the first notecard in inventory (default max is 255 lines and would not show in std chat)
        // each Line Return is reflected in output
        string name = llGetInventoryName(INVENTORY_NOTECARD,0);
        if(name == "") 
        {
            llSay(0,"There is no notecard in prim inventory.  Please place a notecard with some text in the prim to display it's contents"); 
            return;
        }
        else
        {
            string text = osGetNotecard(name);
            llOwnerSay("NoteCard Name is: "+name);
            llSay(0,text);
        }

    }
}
