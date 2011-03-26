// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osMatchString.lsl
// Script Author:
// Threat Level:    High
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osMatchString
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    list osMatchString(string src, string pattern, integer start);
//
// Example of osMatchString
//
default
{
    state_entry()
    {
        llSay(0, "Touch me to show Matched Strings");
    }
    touch_end(integer total_num)
    {
        key kAvatar = llDetectedKey(0);
        string sSentence = "today we do this all day long and all night long";
        list lMatches = [];
        lMatches = osMatchString(sSentence, "all", 0);
        llSay(0,"Matched String :\n"+llDumpList2String(lMatches, " @ "));  
    }
}