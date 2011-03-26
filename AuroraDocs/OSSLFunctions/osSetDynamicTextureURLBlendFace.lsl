// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetDynamicTextureURLBlendFace.lsl
// Script Author:
// Threat Level:    VeryLow
// Script Source:   http://opensimulator.org/wiki/osSetDynamicTextureURLBlendFace
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:     public string osSetDynamicTextureURLBlendFace(string dynamicID, string contentType, string url, string extraParams, int blend, int timer, int alpha, int face)
// Inworld Script Line: osSetDynamicTextureURLBlendFace(string sDynamicID, string sContentType, string sURL, string sExtraParams, integer iBlend, integer iTimer, integer iAlpha, integer iFace);
//
// Example of osSetDynamicTextureURLBlendFace
//
// ExtraParams Values:
//    width - width of the dynamic texture in pixels (example: width:256) 
//    height - height of the dynamic texture in pixels (example: height:256) 
//    alpha - alpha (transparency) component of the dynamic texture. Values are from 0-clear to 255-solid, and false to turn off the alpha layer completely (example: alpha:255) 
//    bgcolour - specifies the background color of the texture (example: bgcolour:Red) 
//    setalpha 
//    integer value - any integer value is treated like specifing alpha component 

default
{
    state_entry()
    {
        llSay(0,"Touch to see osSetDynamicTextureURLBlendFace used to render Web Based Image/Texture on a prim");
    }
    
    touch_start(integer total_num)
    {
        string sDynamicID = "";                          // not implemented yet
        string sContentType = "image";                   // vector = text/lines,etc.  image = texture only
        string sURL = "http://www.goes.noaa.gov/FULLDISK/GMVS.JPG"; // URL for WebImage (Earth Shown)
        string sExtraParams = "width:512,height:512";    // optional parameters in the following format: [param]:[value],[param]:[value]
        integer iBlend = TRUE;                           // TRUE = the newly generated texture is iBlended with the appropriate existing ones on the prim
        integer iDisp = 2;                               // 1 = expire deletes the old texture.  2 = temp means that it is not saved to the sDatabase. 
        integer iTimer = 0;                              // timer is not implemented yet, leave @ 0
        integer iAlpha = 255;                            // 0 = 100% Alpha, 255 = 100% Solid
        integer iFace = 0;                       // Faces of the prim, Select the Face you want
        // Set the prepared texture to the Prim
        osSetDynamicTextureURLBlendFace( sDynamicID, sContentType, sURL, sExtraParams, iBlend, iDisp, iTimer, iAlpha, iFace );
    }
}

