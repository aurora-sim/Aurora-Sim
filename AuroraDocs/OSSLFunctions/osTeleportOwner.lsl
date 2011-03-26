// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osTeleportOwner.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    None
// Script Source:
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
//
// ================================================================
// NOTE:  This is a Polymorphic function.  It has different command strings depending on usage.
//
// C# Source Lines:
//    DateTime osTeleportOwner(int regionX, int regionY, LSL_Types.Vector3 position, LSL_Types.Vector3 lookat);
//    DateTime osTeleportOwner(string regionName, LSL_Types.Vector3 position, LSL_Types.Vector3 lookat);
//    DateTime osTeleportOwner(LSL_Types.Vector3 position, LSL_Types.Vector3 lookat);
//
// Inworld Script Lines:
//    osTeleportOwner(int regionX, int regionY, vector position, vector lookat);
//    osTeleportOwner(string regionName, vector position, vector lookat);
//    osTeleportOwner(vector position, vector lookat);
//    osTeleportOwner(string Ip_or_DNS:Port:RegionName, vector lookat);
//
// Example osTeleportOwner Script
//
// Set Destination as described below, There are a Few Options depending on Application:
//      Destination = "1000,1000";                 = In-Grid Map XXXX,YYYY coordinates
//      Destination = "RegionName";                = In-Grid-TP to RegionName
//      Destination = <2560100.0, 2560100.0, 50.0>;= In-Grid to X,Y,Z Vector coordinates
//
//      Destination = "TcpIpAddr:Port:RegionName"; = HyperGrid-TP method
//      Destination = "DNSname:Port:RegionName";   = HyperGrid-TP method
//
// Note: RegionName is Optionally Specified to deliver Avatar to specific region in an instance.
//
// ========================================================================================
// === SET DESTINATION INFO HERE ===
//
string Destination = "";                  // your target destination here (SEE NEXT LINES) Can Be
vector LandingPoint = <100,100,50>;       // X,Y,Z landing point for avatar to arrive at
vector LookAt = <1,1,1>;                  // which way they look at when arriving
//
default
{
    on_rez(integer start_param)
    {
        llResetScript();
    }

    state_entry()
    {
        Destination = llGetRegionName();
        llWhisper(0, "Touch to osTeleportOwner to destination "+Destination+" @ Landing Point "+(string)LandingPoint);
    }

    changed(integer change) // something changed, take action
    {
        if(change & CHANGED_OWNER)
        {
            llResetScript();
        }
        else if(change & 1024) // that bit is set during a region restart
        {
            llResetScript();
        }
    }

    touch_end(integer num_detected)
    {
        key avatar = llDetectedKey(0);
        llInstantMessage(avatar, "Teleporting you to : "+Destination+" @ Landing Point "+(string)LandingPoint);
        osTeleportOwner(Destination, LandingPoint, LookAt);
    }
}