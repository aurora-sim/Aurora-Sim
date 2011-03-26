// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osParcelJoin.lsl
// Script Author:
// Threat Level:    High
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osParcelJoin
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:     osParcelJoin(vector start, vector end);
//
// Example of osParcelJoin
// This function allows for creating and managing parcels programmatically.
// Joins( start.x,start.y _to_ end.x,end.y ) Z is ignored but must exist in syntax
default
{
    state_entry()
    {
        llSay(0,"Touch to Join adjacent Parcels");
    }
    touch_start()
    {
        vector start = <0.0, 0.0, 0.0>; //top corner
        vector end = <100.0, 100.0, 0.0>;
        osParcelJoin(start, end);
    }
}
