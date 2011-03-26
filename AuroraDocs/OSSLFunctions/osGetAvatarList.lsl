// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetAvatarList.lsl
// Script Author:   WSM
// Threat Level:    None
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osGetAvatarList
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line:  list osGetAvatarList();
//
// Example of osGetAvatarList.
//
default
{
    state_entry()
    {
         llSay(0,"Touch to see result of osGetAvatarList which returns a list of (Avatar-UUID, Postion & Avatar Name) on a region Excluding the Owner of the script"); 
    }
    touch_start(integer total_number)
    {
        list avatars = osGetAvatarList(); //creates a Strided List (3 strides)
        llSay(0, "UUID, Position, AvatarName, on this Region (without the owner):\n" + llList2CSV(avatars));
    }
}