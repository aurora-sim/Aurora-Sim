// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osAvatarStopAnimation.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    VeryHigh
// Script Source:   
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
//================================================================
// Inworld Script Line:    osAvatarStopAnimation(key targetuuid, string anim);
//
// NOTE:  anim can be the Name (if contained in prim) or UUID of the animation
//
default
{
    state_entry()
    {
        llSay(0, "Touch to have Avatar STOP using the contained animation with osAvatarStopAnimation ");
    }
   
    touch_end(integer num)
    {
        string anim = llGetInventoryName(INVENTORY_ANIMATION, 0);
        if(anim == "") 
        {
            llOwnerSay("ERROR: Animation Missing. Please drop an animation in the prim with this script");
            return;
        }
        else
        {
            llOwnerSay("Now Playing "+anim+" animation");
            osAvatarStopAnimation(llDetectedKey(0), anim);
        }
    }
}