// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetRegionMapTexture.lsl
// Script Author:
// Threat Level:    High
// Script Source:   http://opensimulator.org/wiki/osGetRegionMapTexture
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:    public LSL_Key osGetRegionMapTexture(string regionName)
// Inworld Script Line:  Key osGetRegionMapTexture(string regionName);
//
// Example of osGetRegionMapTexture()
//
integer listener;
string RegionName;
default
{
    state_entry()
    {
         llSay(0,"Touch to see osGetRegionMapTexture Display a the Region Map on a prim"); 
    }
    
    touch_end(integer num)
    {
        RegionName = llGetRegionName();
        llListenRemove(listener);  // remove listener as a safety, just in case.
        // for future use llTextBox is implemented in V2 Viewers & Phoenix and will also be available in SL as that function is now deployed.
        // commented the next 3 lines below, but if you want to try, go for it.
        //
        // === textbox command below ===
        // integer channel = ~(integer)llFrand(1000.0);
        // listener = llListen(channel,"","","");
        // llTextBox(llDetectedKey(0),"Please enter the name of the Region Map you want to display\nCurrent Region Name = "+RegionName,channel);
        // === end of textbox commands ===
        //
        // if Using llTextBox then comment out the next 3 lines after uncommenting the llTextBox lines above.
        // === regular chat input lines ===
        integer channel = 0;
        listener = llListen(channel,"","","");
        llSay(0,"Please enter the name of the Region Map you want to display in your Local Chat line on channel 0\nCurrent Region Name = "+RegionName);
        // === end of standard chat input commands ===
    }
    
    listen( integer channel, string name, key id, string message )
    {
        llListenRemove(listener);
        llSay(0,"You responded with: " + (string)message);
        if(message != "")
        {
            key map = osGetRegionMapTexture(message);
            llWhisper(0,"map key = "+(string)map);
            if(map == NULL_KEY)
            {
                llSay(0,"Region Name provided invalid.  Using current Region Name");
                map = osGetRegionMapTexture(RegionName);
                llSetTexture(map, ALL_SIDES);
            }
            else 
            {
                RegionName = message;
                map = osGetRegionMapTexture(RegionName);
                llSetTexture(map, ALL_SIDES);
            }
        }
    }
}
