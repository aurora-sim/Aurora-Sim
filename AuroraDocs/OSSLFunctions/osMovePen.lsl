// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osMovePen.lsl
// Script Author:
// Threat Level:    None
// Script Source:   http://opensimulator.org/wiki/osMovePen
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    string osMovePen(string drawList, integer x, integer y);
//
// Example of osMovePen
//
// SPECIAL NOTES
// Appends a MoveTo drawing command to the string provided in drawList and returns the result.
// This moves the pen's location to the coordinates specified by the x and y parameters, without drawing anything.
//
integer Touched = FALSE;
default
{
    state_entry()
    {
         llSay(0,"Touch to see osMovePen do it's tricks"); 
    }
    touch_end(integer num)
    {
        if(Touched)
        {
            Touched = FALSE;
            llSetTexture(TEXTURE_PLYWOOD, ALL_SIDES);
        }
        else
        {
            Touched = TRUE;
            string DrawList = "";                               // Storage for our drawing commands
            integer i;
            DrawList = osSetPenSize( DrawList, 3 );          // Set the pen width to 3 pixels
            DrawList = osSetPenColor( DrawList, "Blue" );   // Set the pen color to blue
            for (i = 0; i < 256; i += 20)
            {
                DrawList = osMovePen( DrawList, 255, i );    // Move to the right side
                DrawList = osDrawLine( DrawList, 0, i+20 );  // Draw left and slightly down
            }
            // Now draw the lines
            osSetDynamicTextureData( "", "vector", DrawList, "width:256,height:256", 0 );
            llSay(0,"Touch again to revert back to plywood texture");
        }
    }
}
