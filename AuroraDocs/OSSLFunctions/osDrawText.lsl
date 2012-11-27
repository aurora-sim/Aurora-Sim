// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osDrawText.lsl
// Script Author:
// Threat Level:    None
// Script Source:   REFERENCE http://opensimulator.org/wiki/osDrawText
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line:    osDrawText(string drawList, string text);
//
// Example of osDrawText
//
integer Touched = FALSE;
default
{
    state_entry()
    {
         llSay(0,"Touch to see osDrawText to write some text on the Prim Faces"); 
    }
    touch_end(integer num)
    {
        string AvatarName = llKey2Name(llDetectedKey(0));
        
        if(Touched)
        {
            Touched = FALSE;
            llSetTexture(TEXTURE_PLYWOOD, ALL_SIDES);
        }
        else
        {
            Touched = TRUE;    
            string DrawList = ""; 
            DrawList = osMovePen( DrawList, 10, 10 );           // Upper left corner at <10,10>
            DrawList = osSetPenColor(DrawList, "Green");       // Set the pen color to green
            DrawList = osDrawText( DrawList, "Hello "+AvatarName+"\nThis message is a Test"); // Place some text
            // Draw the text
            osSetDynamicTextureData( "", "vector", DrawList, "width:256,height:256", 0 );
            llSay(0,"Touch again to revert to Default Plywood texture");
        }
    }
}