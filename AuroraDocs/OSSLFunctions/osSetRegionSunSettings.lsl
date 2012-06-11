// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetRegionSunSettings.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    Nuisance
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetRegionSunSettings
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// 
// Changes the Region Sun Settings, then Triggers a Sun Update
// param name="useEstateSun" True to use Estate Sun instead of Region Sun
// param name="sunFixed" True to keep the sun stationary
// param name="sunHour" The "Sun Hour" that is desired, 0...24, with 0 just after SunRise
// ================================================================
// C# Source Line:      public void osSetRegionSunSettings(bool useEstateSun, bool sunFixed, double sunHour)
// Inworld Script Line: osSetRegionSunSettings(integer useEstateSun, integer sunFixed, float sunHour); 
//
// Example of osSetRegionSunSettings
//
integer useEstateSun = TRUE; // TRUE to use Estate Sun instead of Region Sun
integer sunFixed = TRUE;     // TRUE to keep the sun stationary
float sunHour = 1.0;         // The "Sun Hour" that is desired, 0...24, with 0 just after SunRise

default
{
    state_entry()
    {
        llSay(0,"Touch to see osSetRegionSunSettings work.");
    }
    touch_end(integer num)
    {
        if(sunHour < 24.0) sunHour = sunHour+2.0;
        else sunHour = 1.0;
        osSetRegionSunSettings(useEstateSun, sunFixed, sunHour);
    }
}