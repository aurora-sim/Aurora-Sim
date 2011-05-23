// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osParcelSubdivide.lsl
// Script Author:
// Threat Level:    High
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osParcelSubdivide
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public void osParcelSubdivide(LSL_Vector pos1, LSL_Vector pos2)
// Inworld Script Line:     osParcelSubdivide(vector pos1, vector pos2);
//
// Example of osParcelSubdivide
// This function allows subdivision of parcels programmatically.
// Subdivides( start.x,start.y _to_ end.x,end.y ) Z is ignored but must exist in syntax
default
{
    state_entry()
    {
        llSay(0,"Touch to subdivide adjacent Parcels using osParcelSubdivide");
    }
    touch_start(integer test)
    {
        vector start = <0.0, 0.0, 0.0>; //top corner
        vector end = <100.0, 100.0, 0.0>;
        osParcelSubdivide(start, end);
    }
}
