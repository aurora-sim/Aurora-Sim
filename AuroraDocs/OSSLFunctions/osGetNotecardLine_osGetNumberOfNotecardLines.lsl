// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetNotecardLine_osGetNumberOfNotecardLines.lsl
// Script Author:
// Threat Level:    VeryHigh
// Script Source:   http://opensimulator.org/wiki/osGetNotecardLine
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    
//    string osGetNotecardLine(string name, integer line);
//    integer osGetNumberOfNotecardLines(string name);
//
// Example of osGetNotecardLine & osGetNumberOfNotecardLines
//
default
{
    state_entry()
    {
         llSay(0,"Touch to see osGetNotecardLine read in a notecard line by line and display the text retrieved"); 
    }
    touch_end(integer num)
    {
        string name = llGetInventoryName(INVENTORY_NOTECARD,0);
        if(name == "") 
        {
            llSay(0,"There is no notecard in prim inventory.  Please place a notecard with some text in the prim to display it's contents"); 
            return;
        }
        else
        {
            integer lines;
            lines = osGetNumberOfNotecardLines(name);
            integer i;
            for(i=1; i<=lines; i++)
            {
                llSay(0, osGetNotecardLine(name, i));
            }
        }
    }
}
