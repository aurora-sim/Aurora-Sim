// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osCauseDamage_osCauseHealing.lsl
// Script Author:   WhiteStar Magic 
// Threat Level:    High
// Script Source:   
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
//================================================================
// Inworld Script Line:  osCauseDamage(key targetuuid, integer hitpoints);
// Inworld Script Line:  osCauseHealing(key targetuuid, integer healing);
//
// hit points: 0 = none, 100 = dead
// also allows for damage on collision or touch
//
integer Touched = FALSE;
default
{
    state_entry()
    {
        llSay(0, "Touch to test osCauseDamage & osCauseHealing ");
    }
    touch_end(integer num)
    {
        if(Touched)
        {
            Touched = FALSE;
            llWhisper(0, "You are Healing 50 Hit Points from damage\nTouch again to lose 50 Hit Points");
            osCauseHealing(llDetectedKey(0), 50);
        }
        else
        {
            Touched = TRUE;
            llWhisper(0, "You are taking 50 Hit Points in damage\nTouch again to Heal");
            osCauseDamage(llDetectedKey(0), 50);
        }
    }
}
