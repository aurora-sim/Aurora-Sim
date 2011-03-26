// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetPenCap.lsl
// Script Author:
// Threat Level:    None
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetPenCap
//                  http://opensimulator.org/wiki/Drawing_commands
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public string osSetPenCap(string drawList, string direction, string type)
// Inworld Script Line: osSetPenCap(string drawList, string direction, string type);
//
// Example of osSetPenCap
//
default
{
    state_entry()
    {
        llSay(0, "Touch to see how osSetPenCap works");
    }
    touch_start(integer num)
    {
        string drawList = "";                            // Storage for our drawing commands
        list lType = ["arrow","diamond","round","flat"]; // Types of PenShapes
        list lDir = ["both","start","end"];              // Direction of PenShape
        // setup first line
        drawList = osSetPenSize( drawList, 5 );                                          // Set the pen width to 5 pixels. With 1 pixel, arrow is very hard to see
        drawList = osSetPenCap(drawList, llList2String(lDir,0), llList2String(lType,0)); // Sets shape to arrow, Both Directions
        drawList = osMovePen(drawList,10,100);                                           // Moves pen to 10,100
        drawList += "LineTo 50,150;";                                                    // Draws line from 10,100 to 50,150
        // setup second line
        drawList = osSetPenCap(drawList, llList2String(lDir,1), llList2String(lType,1)); // Sets shape to Diamond, Start direction
        drawList = osMovePen(drawList,75,150);                                           // Moves pen to 75,150
        drawList += "LineTo 75,200;";                                                    // Draws line from 75,150 to 75,200
        // draw the lines out 
        osSetDynamicTextureData( "", "vector", drawList, "", 0 );
    }
} 