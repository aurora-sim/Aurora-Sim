// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetPenSize.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    None
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetPenSize
//                  http://opensimulator.org/wiki/Drawing_commands
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public string osSetPenSize(string drawList, int penSize)
// Inworld Script Line: osSetPenSize(string sDrawList, integer iPenSize);
//
// Example of osSetPenSize
//
default
{
    state_entry()
    {
        llSay(0,"Touch to see osPenSize work.  Shows Lines in Point Size 1 to 20");
    }
    touch_end(integer num)
    {
        string sDrawList = "";                                                       // Storage for our drawing commands
        integer iPenSize;                                                            // Define the Integer for iPenSize
        sDrawList = osSetPenColor(sDrawList, "Blue");                               // Set the pen color to Blue
 
        for (iPenSize = 1; iPenSize < 21; ++iPenSize)
        {
            sDrawList = osSetPenSize(sDrawList, iPenSize);                           // Set the pen size
            sDrawList = osDrawLine(sDrawList, 12, iPenSize*24, 384, iPenSize*24);    // Draw a horizontal line from startY=12 to endY=384 (leaving room for text)
            // Setup fontsize & text for Point Size Display
            sDrawList = osSetFontSize(sDrawList, 8);                                  // Set our font size to be readable
            sDrawList = osDrawText(sDrawList, "--Size-["+(string)iPenSize+"] points");// Set the text to show Point Size
        }
        // Now draw the lines
        osSetDynamicTextureData("", "vector", sDrawList, "width:512,height:512", 0);  // Texture = 512x512
    }
}