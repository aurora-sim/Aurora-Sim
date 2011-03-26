// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetGridLoginURI.lsl
// Script Author:
// Threat Level:    Moderate
// Script Source:   REFERENCE http://opensimulator.org/wiki/osGetGridLoginURI
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:  string osGetGridLoginURI();
//
// Example of osGetGridLoginURI()
// returns the value of loginuri = "http://GridDomain_OR_IP:PortNum" in GridCommon.ini under [GridInfo] section
//
default
{
    state_entry()
    {
         llSay(0,"Touch to see osGetGridLoginURI return the value set for the loginuri "); 
    }
    touch_end(integer num)
    {
        llSay(0, "Grid Login Uri = "+osGetGridLoginURI());
    }
}
