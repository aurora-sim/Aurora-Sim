// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osCauseHealing.lsl
// Script Author:
// Threat Level:    High
// Script Source:   http://opensimulator.org/wiki/osCauseHealing
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
//================================================================
// C# Source Line:    public void osCauseHealing(string avatar, double healing)
// Inworld Script Line:    osCauseHealing(key targetuuid, integer healing);
//
default
{
    state_entry()
    {
        // healing: 0 = none, 100 = full health
        osCauseHealing(llGetOwner(), 50);
    }
}
