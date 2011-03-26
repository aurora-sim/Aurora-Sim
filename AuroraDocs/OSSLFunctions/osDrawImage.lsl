// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osDrawImage.lsl
// Script Author:
// Threat Level:    None
// Script Source:   REFERENCE http://opensimulator.org/wiki/osDrawImage
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line:  osDrawImage( string drawList, integer width, integer height, string imageUrl);
//
// Example of osDrawImage
integer Touched = FALSE;
default
{
    state_entry()
    {
         llSay(0,"Touch to see osDrawImage place an image on a prim from a Web Source"); 
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
            string ImageURL = "http://grid.aurora-sim.org/splash/Aurora-Login.gif";
            DrawList = osMovePen( DrawList, 0, 0 );                // Upper left corner at <0,0>
            DrawList = osDrawImage( DrawList, 256, 54, ImageURL ); // 200 pixels by 100 pixels
            // Draw the image
            osSetDynamicTextureData( "", "vector", DrawList, "width:256,height:256", 0 );
        }
    }
}