// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osKickAvatar.lsl
// Script Author:
// Threat Level:    Severe
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osKickAvatar
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    osKickAvatar(string FirstName,string SurName,string AlertMsg);
//
// Example of osKickAvatar(string FirstName, string SurName, string AlertMsg)
//
default
{
    state_entry() // display @ start
    {
        llSay(0, "Touch to see osKickAvatar kick you off the region with a message (You can relog after)");
    }
    touch_end(integer num)
    {
        key kAvatar = llDetectedKey(0);
        list lName = llParseString2List(osKey2Name(kAvatar), [" "],[]);
        string FirstName = llList2String(lName,0);
        string LastName = llList2String(lName,1);
        llInstantMessage(kAvatar, "Sorry, "+FirstName+" "+LastName+" This was an example osKickAvatar ");
        osKickAvatar(FirstName,LastName,"You have been test kicked!");
    }
}
