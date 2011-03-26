// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetDrawStringSize.lsl
// Script Author:
// Threat Level:    VeryLow
// Script Source:   
// REFERENCES: http://opensimulator.org/wiki/osGetDrawStringSize
//             http://opensimulator.org/wiki/Drawing_commands
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line:  vector osGetDrawStringSize( string contentType, string text, string fontName, integer fontSize);
//
// Example of osGetDrawStringSize
//
// SPECIAL NOTES  See References for breakdown of values
//
string contentType = "vector";
string text = "Hello";
string fontName = "Arial";  // Valid Fonts that are usable See:  http://www.w3schools.com/css/css_websafe_fonts.asp
integer fontSize = 14;      
//
integer Touched = FALSE;
default
{
    state_entry()
    {
         llSay(0,"Touch to see osGetDrawStringSize to write some text on the Prim Faces"); 
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
            string TextToDraw = text+" "+AvatarName;        // text to display

            vector Extents = osGetDrawStringSize( contentType, TextToDraw, fontName, fontSize );

            integer xpos = 128 - ((integer) Extents.x >> 1);    // Center the text horizontally
            integer ypos = 128 - ((integer) Extents.y >> 1);    //   and vertically
            DrawList = osMovePen( DrawList, xpos, ypos );       // Position the text
            DrawList = osDrawText( DrawList, TextToDraw );      // Place the text
            // Now draw the text
            osSetDynamicTextureData( "", "vector", DrawList, "width:256,height:256", 0 );
        }
    }
}
