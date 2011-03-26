// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetDynamicTextureURLBlend.lsl
// Script Author:
// Threat Level:    VeryLow
// Script Source:   http://opensimulator.org/wiki/osSetDynamicTextureURLBlend
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:     public string osSetDynamicTextureURLBlend(string dynamicID, string contentType, string url, string extraParams, int timer, int alpha)
// Inworld Script Line: osSetDynamicTextureURLBlend(string sDynamicID, string sContentType, string sURL, string sExtraParams, integer iTimer, integer alpha);
//
// Example of osSetDynamicTextureURLBlend
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
        llSay(0,"Touch to see osSetDynamicTextureURLBlend used to render Web Based Image/Texture on a prim");
    }
    
    touch_start(integer total_num)
    {
        string sDynamicID = "";                          // not implemented yet
        string sContentType = "image";                   // vector = text/lines,etc.  image = texture only
        string sURL = "http://www.goes.noaa.gov/FULLDISK/GEVS.JPG"; // URL for WebImage (Earth Shown)
        string sExtraParams = "width:256,height:256";    // optional parameters in the following format: [param]:[value],[param]:[value]
        integer iTimer = 0;                              // timer is not implemented yet, leave @ 0
        integer iAlpha = 255;                            // 0 = 100% Alpha, 255 = 100% Solid
        // Set the prepared texture info to a string
        string sTexture = osSetDynamicTextureURLBlend( sDynamicID, sContentType, sURL, sExtraParams, iTimer, iAlpha);
        if (llStringLength(sTexture)>0) 
        {
            llSay(0,"URL of Texture = "+sURL);
            llSetTexture(sTexture, ALL_SIDES);
        }
    }
}

