// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetMapTexture.lsl
// Script Author:   
// Threat Level:    None
// Script Source:   http://opensimulator.org/wiki/osGetMapTexture
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:  Key osGetMapTexture();
//
// Example of osGetMapTexture()
//
integer Touched = FALSE;
default
{
    state_entry()
    {
         llSay(0,"Touch to see osGetMapTexture Place the current region map texture on the Prim Faces"); 
    }
    touch_end(integer num)
    {
        if(Touched)
        {
            Touched = FALSE;
            llSetTexture(TEXTURE_PLYWOOD, ALL_SIDES);
            llSay(0,"Touch again to show map texture again");
        }
        else
        {
            Touched = TRUE;    
            llSetTexture(osGetMapTexture(),ALL_SIDES);
            llSay(0,"Touch again to return to Plywood Texture");
        }
    }
}