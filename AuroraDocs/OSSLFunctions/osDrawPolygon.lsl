// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osDrawPolygon.lsl
// Script Author:
// Threat Level:    None
// Script Source:   REFERENCE http://opensimulator.org/wiki/osDrawPolygon
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line: osDrawPolygon( string drawList, list xpoints, list ypoints );
//
// Example of osDrawPolygon
//
integer Touched = FALSE;
default
{
    state_entry()
    {
         llSay(0,"Touch to see osDrawPolygon create a Polygon Image on the prim"); 
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
            DrawList = osSetPenSize( DrawList, 3 );                               // Set the pen width to 3 pixels
            DrawList = osSetPenColor( DrawList, "Purple" );                      // Set the pen color to blue
            DrawList = osDrawPolygon( DrawList, [25,50,75], ["50",100.0,150] );   // integer, float or string are usable
            // Draw the polygon
            osSetDynamicTextureData( "", "vector", DrawList, "", 0 );
            llSay(0,"Touch again to revert to Default Plywood texture");
        }
    }
}