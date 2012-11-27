// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osDrawEllipse.lsl
// Script Author:   
// Threat Level:    None
// Script Source:   Reference http://opensimulator.org/wiki/osDrawEllipse
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line:    osDrawEllipse( string drawList, integer width, integer height );
//
integer Touched = FALSE;

default
{
    state_entry()
    {
        llSay(0,"Touch to see osDrawElipse create a Blue Elliptical figure on the prim"); 
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
            string DrawList = "";                           // Storage for drawing commands
            DrawList = osSetPenSize( DrawList, 4 );         // Set the pen width to 3 pixels
            DrawList = osSetPenColor( DrawList, "Blue" );  // Set the pen color to blue
            DrawList = osMovePen( DrawList, 28, 78 );       // Upper left corner at <28,78>
            DrawList = osDrawEllipse( DrawList, 200, 100 ); // 200 pixels by 100 pixels
            // Now draw the ellipse
            osSetDynamicTextureData( "", "vector", DrawList, "width:256,height:256", 0 );
            llSay(0,"Touch again to revert back to plywood texture");
        }
    }
}