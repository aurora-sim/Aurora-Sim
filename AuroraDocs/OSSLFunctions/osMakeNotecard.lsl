// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osMakeNotecard.lsl
// Script Author:
// Threat Level:    High
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osMakeNotecard
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    osMakeNotecard(string notecardName, list contents);
//
// Example of osMakeNotecard
//
default
{
    state_entry()
    {
        llSay(0, "Touch me to create a Notecard with your Avatar Name, containing: Name,Key,Position,Rotation" );
    }
    touch_end(integer total_num)
    {
        key kAvatar = llDetectedKey(0);
        string sName = llDetectedName(0);
        list lContents = [];              //to contain the values
        lContents += "Name: "+sName;
        lContents += "Key: "+(string)kAvatar;
        lContents += "Position: "+(string)llDetectedPos(0);
        lContents += "Rotation: "+(string)llDetectedRot(0);
        osMakeNotecard(sName, lContents); //Makes the notecard. Avatar Name = NoteCard Name
        llGiveInventory(kAvatar, sName);  //Gives the notecard to the person.
    }
}