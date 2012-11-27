// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetDynamicTextureDataBlendFace.lsl
// Script Author:
// Threat Level:    VeryLow
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetDynamicTextureDataBlendFace
//                  Above Supplemental gives a good Example numbers all iFaces of any given prim.
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public string osSetDynamicTextureDataBlendFace(string sDynamicID, string sContentType, string sData, string sExtraParams, bool iBlend, int iDisp, int timer, int alpha, int iFace)
// Inworld Script Line: osSetDynamicTextureDataBlendFace(string sDynamicID, string sContentType, string sData, string sExtraParams, integer iBlend, integer iDisp, integer timer, integer alpha, integer iFace;
//
// Example of osSetDynamicTextureDataBlendFace
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
        llSay(0,"Touch to see osSetDynamicTextureDataBlend used to render custom drawings on a prim");
    }
    touch_start(integer total_num)
    {
        string sDynamicID = "";                          // not implemented yet
        string sContentType = "vector";                  // vector = text/lines,etc.  image = texture only
        string sData = "";                               // Storage for our drawing commands
        string sExtraParams = "width:512,height:512";    // optional parameters in the following format: [param]:[value],[param]:[value]
        integer iBlend = TRUE;                           // TRUE = the newly generated texture is iBlended with the appropriate existing ones on the prim
        integer iDisp = 2;                               // 1 = expire deletes the old texture.  2 = temp means that it is not saved to the sDatabase. 
        integer iTimer = 0;                              // timer is not implemented yet, leave @ 0
        integer iAlpha = 255;                            // 0 = 100% Alpha, 255 = 100% Solid
        integer iFace = ALL_SIDES;                       // Faces of the prim, Select the Face you want
        //
        // sData (drawing commands) used in the example.
        sData = osMovePen(sData, 20, 40);                // place pen @ X,Y coordinates 
        sData = osSetFontName(sData, "Arial");           // Set the Fontname to use
        sData = osSetFontSize(sData, 10);                // Set the Font Size in pixels
        sData = osSetPenColor( sData, "Green" );        // Set the pen color to Green
        sData = osDrawText(sData, "Written Text to display on\nALL_SIDES ");  // The text to write
        // Now draw it out
        osSetDynamicTextureDataBlendFace( sDynamicID, sContentType, sData, sExtraParams, iBlend, iDisp, iTimer, iAlpha, iFace );
    }
}
