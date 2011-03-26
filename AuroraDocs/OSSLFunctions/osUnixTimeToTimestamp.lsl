// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osUnixTimeToTimestamp.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    VeryLow
// Script Source:
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
//
// ================================================================
// C# Source Lines:    public LSL_String    osUnixTimeToTimestamp(long time)
// Inworld Script Lines:  osUnixTimeToTimestamp(long time);
//
// Example osUnixTimeToTimestamp Script
// Returned format = "yyyy-MM-ddTHH:mm:ss.fffffffZ"
// ========================================================================================
//
default
{
    on_rez(integer start_param)
    {
        llResetScript();
    }

    state_entry()
    {
        llWhisper(0, "Touch to use osUnixTimeToTimestamp\nNOTE: this will convert Unix Time to a readable time");
    }

    touch_end(integer num_detected)
    {
        key avatar = llDetectedKey(0);
        llInstantMessage(avatar, "Unix Time To Timestamp = [ "+osUnixTimeToTimestamp((integer)llGetUnixTime())+" ]");
    }
}