// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osTerrainFlush.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    VeryLow
// Script Source:
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
//
// ================================================================
// C# Source Lines:    public void osTerrainFlush()
// Inworld Script Lines:  osTerrainFlush();
//
// Example osTerrainFlush Script
//
// Note: Ths function to be used when Changes to terrain have been made and you want to force an 
//       update to the Database.  Use in conjunction with osSetTerrainHeight or after modifying 
//       the terrain manually
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
        llWhisper(0, "Touch to use osTerrainFlush\nNOTE: this will not show anything in-world as it only forces a Terrain Update to e commited to the Database");
    }

    touch_end(integer num_detected)
    {
        key avatar = llDetectedKey(0);
        llInstantMessage(avatar, "Flushing Terrain data to Database now");
        osTerrainFlush();
    }
}