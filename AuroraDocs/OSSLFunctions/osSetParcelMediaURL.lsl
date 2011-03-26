// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetParcelMediaURL.lsl
// Script Author:
// Threat Level:    VeryLow
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetParcelMediaURL
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public void osSetParcelMediaURL(string url)
// Inworld Script Line: osSetParcelMediaURL(string url);
//
// Example of osSetParcelMediaURL
//
string sURL = "http://www.archive.org/download/CncdVsFairlightCeasefire/ceasefire_all_fall_down.stream.mp4"; //The URL we are setting to the parcel.
//
default
{
    state_entry()
    {
        llSay(0, "Touch to see how osSetParcelMediaURL works");
    }
    touch_start(integer num)
    {
        llSay(0,"Media URL being set to :"+sURL);
        osSetParcelMediaURL( sURL);
    }
}