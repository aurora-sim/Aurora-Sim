// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetEstateSunSettings.lsl
// Script Author:
// Threat Level:    Nuisance
// Script Source:   http://opensimulator.org/wiki/osSetEstateSunSettings
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public void osSetEstateSunSettings(bool sunFixed, double sunHour)
// Inworld Script Line: osSetEstateSunSettings(integer iSunFixed, float fSunHour);
//
// Example of osSetEstateSunSettings
//
integer iTest = TRUE;
integer iSunFixed;
float fSunHour;

default
{
    state_entry()
    {
        llSay(0,"Touch to see osSetEstateSunSettings used to change Sun Position ");
    }

    touch_start(integer total_num)
    {
        if(iTest)
        {
            iTest = FALSE;
            iSunFixed = TRUE; // TRUE = Sun stationary, FALSE = use global time & move
            fSunHour = 19.00;   // The "Sun Hour" that is desired, 0...24, with 0 just after SunRise
            // Set the prepared texture to the Prim
            osSetEstateSunSettings(iSunFixed, fSunHour);
        }
        else
        {
            iTest = TRUE;
            iSunFixed = FALSE;
            fSunHour = 10.00;
            osSetEstateSunSettings(iSunFixed, fSunHour);
        }
        llSay(0,"osSetEstateSunSettings : SunFixed = ["+iSunFixed+"], SunHour = ["+fSunHour+"]");
    }
}
