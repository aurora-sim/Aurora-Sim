// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetTerrainHeight_osGetTerrainHeight.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    High
// Script Source:   
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// 
// ================================================================
// C# Source Line:      public LSL_Integer    osSetTerrainHeight(int x, int y, double val)
// Inworld Script Line: osSetTerrainHeight(int X, int Y, float value); 
//
// C# Source Line:      public LSL_Float    osGetTerrainHeight(int x, int y)
// Inworld Script Line: osGetTerrainHeight(int x, int y);
//
// Example of osSetTerrainHeight & osGetTerrainHeight
//
integer touched = FALSE;
integer iX = 128;
integer iY = 128;
float CurValue;
//
default
{
    state_entry()
    {
        llSay(0,"Touch to see osSetTerrainHeight & osGetTerrainHeight work.");
        CurValue = osGetTerrainHeight(iX, iY);
    }
    touch_end(integer num)
    {
        if(touched)
        {
            touched = FALSE;
            osSetTerrainHeight(iX, iY, CurValue);
            llOwnerSay("osSetTerrainHeight @ coordinates X-["+(string)iX+"] Y-["+(string)iY+"] set to ["+(string)osGetTerrainHeight(iX, iY)+"]");
        }
        else
        {
            touched = TRUE;
            float NewValue = CurValue + 4.5;
            osSetTerrainHeight(iX, iY, NewValue);
            llOwnerSay("osSetTerrainHeight @ coordinates X-["+(string)iX+"] Y-["+(string)iY+"] set to ["+(string)osGetTerrainHeight(iX, iY)+"]\nTouch to restore to Original Height");
        }
    }
}