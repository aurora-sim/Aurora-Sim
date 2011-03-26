// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// **** UNTESTED ****
//
// Script Title:    osParseJSON.lsl
// Script Author:
// Threat Level:    None
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osParseJSON
//                  see http://www.json.org/ for more details on JSON
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public Hashtable osParseJSON(string JSON)
// Inworld Script Line:     hashtable osParseJSON(string JSON);
//
// Example of osParseJSON
//
// === SCRIPT START HERE ===
string sJSON = "";  //test JSON String here

default
{
    state_entry()
    {
        llSay(0,"Touch to parse test JSON String using osParseJSON");
    }
    touch_start(integer total_num)
    {
        llInstantMessage(llGetOwner(), (string)osParseJSON(sJSON));
    }
}
