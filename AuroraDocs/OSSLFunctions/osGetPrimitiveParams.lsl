// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetPrimitiveParams.lsl
// Script Author:
// Threat Level:    VeryHigh
// Script Source:   http://opensimulator.org/wiki/osGetPrimitiveParams
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:     public LSL_List osGetPrimitiveParams(LSL_Key prim, LSL_List rules)
// Inworld Script Line:    list osGetPrimitiveParams(key prim, list rules);
//
// Example of osGetPrimitiveParams(key prim, list rules)
// Refer to http://wiki.secondlife.com/wiki/LlGetPrimitiveParams for Param List
default
{
    state_entry() 
    {
        key kPrimKey = llGetKey();
        list lMyParams;
        list lPrimParams = [
            PRIM_NAME,
            PRIM_DESC,
            PRIM_TYPE,
            PRIM_MATERIAL,
            PRIM_PHYSICS,
            PRIM_TEMP_ON_REZ,
            PRIM_PHANTOM,
            PRIM_POSITION,
            PRIM_ROTATION,
            PRIM_ROT_LOCAL,
            PRIM_SIZE,
            PRIM_TEXTURE, ALL_SIDES,
            PRIM_COLOR, ALL_SIDES,
            PRIM_BUMP_SHINY, ALL_SIDES,
            PRIM_FULLBRIGHT, ALL_SIDES,
            PRIM_FLEXIBLE,
            PRIM_TEXGEN, ALL_SIDES,
            PRIM_POINT_LIGHT,
            PRIM_GLOW, ALL_SIDES];
        
        lMyParams = osGetPrimitiveParams(kPrimKey, lPrimParams); 


        llSay(0, llList2String(lMyParams,0));
    }
}
