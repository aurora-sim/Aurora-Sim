// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetPenColour.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    None
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetPenColour
//                  http://opensimulator.org/wiki/Drawing_commands
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public string osSetPenColour(string drawList, string colour)
// Inworld Script Line: osSetPenColour(string sCommandList, string sColour);
//
// Example of osSetPenColour
//
list lColours =
    ["AliceBlue", "AntiqueWhite", "Aqua", "Aquamarine", "Azure", "Beige", "Bisque",
    "Black", "BlanchedAlmond", "Blue", "BlueViolet", "Brown", "BurlyWood","CadetBlue",
    "Chartreuse", "Chocolate", "Coral", "CornflowerBlue", "Cornsilk", "Crimson", "Cyan",
    "DarkBlue", "DarkCyan", "DarkGoldenrod", "DarkGrey", "DarkGreen", "DarkKhaki", "DarkMagenta",
    "DarkOliveGreen", "DarkOrange", "DarkOrchid", "DarkRed", "DarkSalmon", "DarkSeaGreen", "DarkSlateBlue",
    "DarkSlateGrey", "DarkTurquoise", "DarkViolet", "DeepPink", "DeepSkyBlue","DimGrey", "DodgerBlue",
    "FireBrick", "FloralWhite", "ForestGreen", "Fuchsia", "Gainsboro", "GhostWhite", "Gold",
    "Goldenrod", "Grey", "Green", "GreenYellow","Honeydew", "HotPink", "IndianRed", "Indigo",
    "Ivory", "Khaki", "Lavender", "LavenderBlush", "LawnGreen", "LemonChiffon", "LightBlue",
    "LightCoral", "LightCyan", "LightGoldenrodYellow", "LightGreen", "LightGrey", "LightPink", "LightSalmon",
    "LightSeaGreen", "LightSkyBlue", "LightSlateGrey", "LightSteelBlue", "LightYellow", "Lime", "LimeGreen",
    "Linen", "Magenta", "Maroon", "MediumAquamarine", "MediumBlue", "MediumOrchid", "MediumPurple",
    "MediumSeaGreen", "MediumSlateBlue", "MediumSpringGreen", "MediumTurquoise", "MediumVioletRed", "MidnightBlue", "MintCream",
    "MistyRose", "Moccasin", "NavajoWhite", "Navy", "OldLace", "Olive", "OliveDrab",
    "Orange", "OrangeRed", "Orchid", "PaleGoldenrod", "PaleGreen", "PaleTurquoise", "PaleVioletRed",
    "PapayaWhip", "PeachPuff", "Peru", "Pink", "Plum", "PowderBlue", "Purple",
    "Red", "RosyBrown", "RoyalBlue", "SaddleBrown", "Salmon", "SandyBrown","SeaGreen",
    "Seashell", "Sienna", "Silver", "SkyBlue", "SlateBlue", "SlateGrey","Snow",
    "SpringGreen", "SteelBlue", "Tan", "Teal", "Thistle", "Tomato", "Turquoise",
    "Violet", "Wheat", "White", "WhiteSmoke", "Yellow", "YellowGreen" ];
//
default
{
    state_entry()
    {
        llSetScale(<10.0,0.010,10.0>);    // Set Scale of Prim to show Colours & Colour Names correctly
        llSay(0,"Touch to see the Full Colour List with Names as usable with osDraw Functions using osSetPenColour");
    }
    touch_end(integer num)
    {
        integer iLstLen = llGetListLength(lColours);    // Get the Colours List Length
        string sCommandList = "";                       // Storage for our drawing commands
        sCommandList = osSetPenSize( sCommandList, 5 ); // Set the pen width to 5 pixels
        integer i;

        llSay(0,"Colour List has "+(string)iLstLen+" Colours usable with osDraw Functions");
        // draw each named color as a single horizontal line
        // append the Colour Name after the coloured line
        for (i = 0; i < iLstLen; ++i)
        {
            string sColour = llList2String(lColours, i);            //Get the colour from the List
            sCommandList = osSetPenColor(sCommandList, sColour);   // Set PenColour to the colour
            // Setup fontsize & text for names
            sCommandList = osSetFontSize(sCommandList, 7);          // Set our font size to be readable
            sCommandList = osDrawText(sCommandList, "___"+sColour); // Set the Colour Name prefixed with ___
            // Setup the DrawLine
            integer iY = (i*7)+5;                                   //Correct for positioning
            sCommandList = osDrawLine(sCommandList, 0, iY, 768, iY);// DrawLine ends @ 768 leaving roon for appended Colour Name
        }
        // Draw the data to the prim
        osSetDynamicTextureData( "", "vector", sCommandList, "width:1024,height:1024", 0 ); //Texture Resolution is 1024x1024
    }
}