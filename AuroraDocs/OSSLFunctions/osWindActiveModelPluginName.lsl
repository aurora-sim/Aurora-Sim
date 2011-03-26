// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osWindActiveModelPluginName.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    None
// Script Source:
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
//
// ================================================================
// Inworld Script Lines:  osWindActiveModelPluginName();
//
// Example osWindActiveModelPluginName Script
// 
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
        llWhisper(0, "Touch to use osWindActiveModelPluginName ");
    }

    touch_end(integer num_detected)
    {
        key avatar = llDetectedKey(0);
        llInstantMessage(avatar, "Active Wind Module = [ "+osWindActiveModelPluginName()+" ]");
    }
}