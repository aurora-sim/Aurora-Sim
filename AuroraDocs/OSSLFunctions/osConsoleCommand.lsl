// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osConsoleCommand.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    Severe
// Script Source:
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line:    osConsoleCommand(string command);
//
// This is ONLY EFFECTIVE if you are running Aurora-Sim yourself and have access to the console screen 
// This Fuction MUST BE RESTRICTED to using only Authorized UUID as can be defined in
//    \bin\Configuration\Scripting\AuroraDotNetEngine.ini
//
default
{
    state_entry()
    {
        llSay(0,"Touch to use osConsoleCommand to show the Region Names on your console ");
    }
    
    touch_end(integer num)
    {
        string command = "show regions";
        osConsoleCommand(command);
    }
}
