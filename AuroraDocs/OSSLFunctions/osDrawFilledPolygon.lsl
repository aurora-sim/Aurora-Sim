// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osDrawFilledPolygon.lsl
// Script Author:
// Threat Level:    None
// Script Source:   REFERENCE http://opensimulator.org/wiki/OsDrawFilledPolygon
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line:  osDrawFilledPolygon( string drawList, list xpoints, list ypoints );
//
integer Touched = FALSE;

default
{
    state_entry()
    {
        llSay(0,"Touch to see osDrawFilledPolygon create a filled blue polygon figure on the prim"); 
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
            string DrawList = ""; 
            DrawList = osSetPenSize( DrawList, 5 );         // Set the pen width to 3 pixels
            DrawList = osSetPenColor( DrawList, "Blue" );  // Set the pen color to blue
            DrawList = osDrawFilledPolygon( DrawList, [50,100,150], ["50",100,150.0] ); // You can use either integer, float or string 
            // Now draw the polygon
            osSetDynamicTextureData( "", "vector", DrawList, "", 0 );
            llSay(0,"Touch again to revert back to plywood texture");
        }
    }
}