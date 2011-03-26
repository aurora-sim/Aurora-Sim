// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osShutDown.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    High
// Script Source:   
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// 
// ================================================================
// C# Source Line:      public void    osShutDown()
// Inworld Script Line: osShutDown(); 
//
// Example of osShutDown
//
//
default
{
    state_entry()
    {
        llSay(0,"Touch to see osShutDown work.\n!!! WARNING !!! this is identical to issuing the SHUTDOWN command on the Console\nIt will QUIT Aurora !");
    }
    touch_end(integer num)
    {
        llOwnerSay("Issuing the osShutDown command.  This will terminate Aurora-Sim application in 30 Seconds");
        llSleep(30.0);
        osShutDown();
    }
}