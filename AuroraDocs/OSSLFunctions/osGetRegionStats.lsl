// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetRegionStats.lsl
// Script Author:
// Threat Level:    Moderate
// Script Source:   http://opensimulator.org/wiki/osGetRegionStats
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    list osGetRegionStats();
//
// Example of llGetRegionStats()
//
// Displays certain region statistics in hovertext above the prim containing the script.
//
// VALUES "Constant" = "Integer Value" 
//    STATS_TIME_DILATION = 0  
//    STATS_SIM_FPS = 1  
//    STATS_PHYSICS_FPS = 2  
//    STATS_AGENT_UPDATES = 3  
//    STATS_ROOT_AGENTS = 4  
//    STATS_CHILD_AGENTS = 5  
//    STATS_TOTAL_PRIMS = 6  
//    STATS_ACTIVE_PRIMS = 7  
//    STATS_FRAME_MS = 8  
//    STATS_NET_MS = 9  
//    STATS_PHYSICS_MS = 10   
//    STATS_IMAGE_MS = 11  
//    STATS_OTHER_MS = 12  
//    STATS_IN_PACKETS_PER_SECOND = 13  
//    STATS_OUT_PACKETS_PER_SECOND = 14
//    STATS_UNACKED_BYTES = 15
//    STATS_AGENT_MS = 16
//    STATS_PENDING_DOWNLOADS = 17
//    STATS_PENDING_UPLOADS = 18  
//    STATS_ACTIVE_SCRIPTS = 19  
//    STATS_SCRIPT_LPS = 20
//
list Stats;
//
GatherStats()
{
    Stats = [];
    Stats = osGetRegionStats();
    string s = "Sim FPS:       " + (string) llList2Float( Stats, STATS_SIM_FPS ) + "\n";
    s += "Physics FPS:   " + (string) llList2Float( Stats, STATS_PHYSICS_FPS ) + "\n";
    s += "Time Dilation: " + (string) llList2Float( Stats, STATS_TIME_DILATION ) + "\n";
    s += "Root Agents:   " + (string) llList2Integer( Stats, STATS_ROOT_AGENTS ) + "\n";
    s += "Child Agents:  " + (string) llList2Integer( Stats, STATS_CHILD_AGENTS ) + "\n";
    s += "Total Prims:   " + (string) llList2Integer( Stats, STATS_TOTAL_PRIMS ) + "\n";
    s += "Active Scripts:" + (string) llList2Integer( Stats, STATS_ACTIVE_SCRIPTS ) + "\n";
    s += "Script LPS:    " + (string) llList2Float( Stats, STATS_SCRIPT_LPS );
    llSetText( s, <0.0,1.0,0.0>, 1.0 );
}
default
{
    state_entry()
    {
        // we set timer to update every 5 seconds
        llSetTimerEvent( 5.0 );
    }
    touch_end(integer num)
    {
        //
        GatherStats();
        // uncomment below if you want to see all values in the Stats List
        // llInstantMessage(llDetectedKey(0), (string)llList2CSV(Stats));
    }
    timer()
    {
        GatherStats();
    }
}