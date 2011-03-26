// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osFormatString.lsl
// Script Author:   
// Threat Level:    Low
// Script Source:   Reference http://opensimulator.org/wiki/osFormatString
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line:    osFormatString(string to_format, list strings);
//
// Example osFormatString
//
default
{
    state_entry()
    {
         llSay(0,"Touch to see osFormatString chat formatted text"); 
    }
    touch_end(integer num)
    {
        string to_format = "My name is {0} and I am located in {1} Region. The avatar who just touched me is {2}.";
        list format = [llGetObjectName(),llGetRegionName(),llKey2Name(llDetectedKey(0))];
        llOwnerSay(osFormatString(to_format, format));
    }
}
