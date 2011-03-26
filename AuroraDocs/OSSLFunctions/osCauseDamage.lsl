// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osCauseDamage.lsl
// Script Author:
// Threat Level:    High
// Script Source:   http://opensimulator.org/wiki/osCauseDamage
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
//================================================================
// C# Source Line:    public void osCauseDamage(string avatar, double damage)
// Inworld Script Line:    osCauseDamage(key targetuuid, integer hitpoints);
//
default
{
    state_entry()
    {
        // hit points: 0 = none, 100 = dead
        // also allows for damage on collision or touch
        osCauseDamage(llGetOwner(), 50);
    }
}
