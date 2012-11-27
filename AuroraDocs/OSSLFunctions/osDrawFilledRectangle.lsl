// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osDrawFilledRectangle.lsl
// Script Author:
// Threat Level:    None
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osDrawFilledRectangle
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line:  osDrawFilledRectangle( string drawList, integer width, integer height );
//
// Example of osDrawFilledRectangle
integer Touched = FALSE;
default
{
    state_entry()
    {
         llSay(0,"Touch to see osDrawFilledRectangle create a Red Rectangle figure on the prim"); 
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
            DrawList = osSetPenSize( DrawList, 3 );                 // Set the pen width to 3 pixels
            DrawList = osSetPenColor( DrawList, "Red" );           // Set the pen color to red
            DrawList = osMovePen( DrawList, 28, 78 );               // Upper left corner at <28,78>
            DrawList = osDrawFilledRectangle( DrawList, 200, 100 ); // 200 pixels by 100 pixels
            // Now to draw image
            osSetDynamicTextureData( "", "vector", DrawList, "width:256,height:256", 0 );
        }
    }
}