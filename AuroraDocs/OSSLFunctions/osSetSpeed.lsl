// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetSpeed.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    Moderate
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetSpeed
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// 
// ================================================================
// C# Source Line:      public void    osSetSpeed(string UUID, float SpeedModifier)
// Inworld Script Line: osSetSpeed(string UUID, float SpeedModifier); 
//
// Example of osSetSpeed
// SpeedModifier values = 1.0 Normal, 1.5 = 1 1/2 times, 2.0 double normal, 3.0 triple
// Object must be worn as attachment
//
float SpeedModifier = 1.0;          // default is 1.0 normal speed
key UUID;
default
{
    state_entry()
    {
        llSay(0,"Wear & Touch to see osSetSpeed work.");
    }
    touch_end(integer num)
    {
        if(SpeedModifier < 7.5) SpeedModifier = SpeedModifier+0.5;
        else SpeedModifier = 1.0;
        llOwnerSay("Speed Modifier = ["+(string)SpeedModifier+"]");
        UUID = llGetOwner();
        osSetSpeed(UUID, SpeedModifier);
    }
}