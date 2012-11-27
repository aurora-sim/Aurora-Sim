// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osDrawLine.lsl
// Script Author:
// Threat Level:    None
// Script Source:   REFERENCE http://opensimulator.org/wiki/osDrawLine
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// POLYMORPHIC function has two possible usages.
//
// Inworld Script Line:
//    osDrawLine( string drawList, integer startX, integer startY, integer endX, integer endY );
//    osDrawLine( string drawList, integer endX, integer endY ); 
//
// Example of osDrawLine
//
integer Touched = FALSE;
default
{
    state_entry()
    {
         llSay(0,"Touch to see osDrawLine to draw a series of lines across the faces of the Prim showing different angles that can be used"); 
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
            DrawList = osSetPenSize( DrawList, 5);              // Set the pen width to 3 pixels
            DrawList = osSetPenColor( DrawList, "Red");        // Set the pen color to red
            DrawList = osDrawLine( DrawList, 0,0,128,256);      // 1st Line diagonal(long form)
            DrawList = osSetPenColor( DrawList, "Blue");       // Set the pen color to green
            DrawList = osDrawLine( DrawList, 256,0);            // 2nd Line diagonal (short form)
            DrawList = osSetPenColor( DrawList, "Green");      // Set the pen color to green
            DrawList = osDrawLine( DrawList, 128,256,128,0);    // 3rd Line straight across
            DrawList = osSetPenColor( DrawList, "Purple");     // Set the pen color to Purple
            DrawList = osDrawLine( DrawList, 256,128,-256,128); // 4th Line straight across
            // Draw the lines
            // Coordinate mapping to draw lines is set to touch Edge to Edge based on settings below
            // correct if you are using Higher Resoltution
            //
            osSetDynamicTextureData( "", "vector", DrawList, "width:256,height:256", 0 );
            llSay(0,"Touch again to revert to Default Plywood texture");
        }
    }
}