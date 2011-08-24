// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetDynamicTextureData.lsl
// Script Author:
// Threat Level:    VeryLow
// Script Source:   http://opensimulator.org/wiki/osSetDynamicTextureData
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public string osSetDynamicTextureData(string dynamicID, string contentType, string data, string extraParams, int timer)
// Inworld Script Line: osSetDynamicTextureData(string sDynamicID, string sContentType, string sData, string sExtraParams, integer iTimer);
//
// Example of osSetDynamicTextureData
//
// ExtraParams Values:
//    width - width of the dynamic texture in pixels (example: width:256) 
//    height - height of the dynamic texture in pixels (example: height:256) 
//    alpha - alpha (transparency) component of the dynamic texture. Values are from 0-clear to 255-solid, and false to turn off the alpha layer completely (example: alpha:255) 
//    bgcolour - specifies the background color of the texture (example: bgcolour:Red) 
//    setalpha 
//    integer value - any integer value is treated like specifing alpha component 
//
default
{
    state_entry()
    {
        llSay(0,"Touch to see osSetDynamicTextureData used to render custom drawings on a prim");
    }
    
    touch_start(integer total_num)
    {
        string sDynamicID = "";                          // not implemented yet
        string sContentType = "vector";                  // vector = text/lines,etc.  image = texture only
        string sData = "";                               // Storage for our drawing commands
        string sExtraParams = "width:256,height:256";    // optional parameters in the following format: [param]:[value],[param]:[value]
        integer iTimer = 0;                              // timer is not implemented yet, leave @ 0
        //
        // sData (drawing commands) used in the example.
        // draw a filled rectangle
        sData = osSetPenSize(sData, 3);                   // Set the pen width to 3 pixels
        sData = osSetPenColor(sData, "Red");             // Set the pen color to red
        sData = osMovePen(sData, 28, 78);                 // Upper left corner at <28,78>
        sData = osDrawFilledRectangle(sData, 200, 100);   // 200 pixels by 100 pixels
        // setup text to go in the drawn box
        sData = osMovePen(sData, 30, 80);                 // place pen @ X,Y coordinates 
        sData = osSetFontName(sData, "Arial");            // Set the Fontname to use
        sData = osSetFontSize(sData, 10);                 // Set the Font Size in pixels
        sData = osSetPenColor(sData, "Black");           // Set the pen color to Green
        sData = osDrawText(sData, "osSetDynamicTextureData\nSample\nfor: "+llDetectedName(0)); // The text to write
        // Now draw it out
        osSetDynamicTextureData( sDynamicID, sContentType, sData, sExtraParams, iTimer);
    }
}

