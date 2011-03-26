// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetPrimitiveParams.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    high
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetPrimitiveParams
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// REFERENCEL http://wiki.secondlife.com/wiki/LlSetPrimitiveParams
// ================================================================
// C# Source Line:      public void osSetPrimitiveParams(LSL_Key prim, LSL_List rules)
// Inworld Script Line: osSetPrimitiveParams(key prim_uuid, list rules); 
//
// Example of osSetPrimitiveParams
//
list PrimParams = [
    PRIM_NAME = "examplePrimName",
    PRIM_SIZE = <1.0,2.0,3.0>];
//    
// Standard Prim Values to set
// Constant NAME  #Val  Description                             Example
// PRIM_NAME 	    27 	Sets the prim's name.        	        [ PRIM_NAME, string name ]
// PRIM_DESC 	    28 	Sets the prim's description. 	        [ PRIM_DESC, string description ]
// PRIM_TYPE 	    9 	Sets the prim's shape.       	        [ PRIM_TYPE, integer flag ] + flag_parameters
// PRIM_MATERIAL 	2 	Sets the prim's material. 	            [ PRIM_MATERIAL, integer flag ]
// PRIM_PHYSICS 	3 	Sets the object's physics status.       [ PRIM_PHYSICS, integer boolean ]
// PRIM_TEMP_ON_REZ 4 	Sets the object's temporary attribute. 	[ PRIM_TEMP_ON_REZ, integer boolean ]
// PRIM_PHANTOM 	5 	Sets the object's phantom status. 	    [ PRIM_PHANTOM, integer boolean ]
// PRIM_POSITION 	6 	Sets the prim's position. 	            [ PRIM_POSITION, vector position ]
// PRIM_ROTATION 	8 	Sets the prim's global rotation. 	    [ PRIM_ROTATION, rotation rot ]
// PRIM_ROT_LOCAL 	29 	Sets the prim's local rotation. 	    [ PRIM_ROT_LOCAL, rotation rot ]
// PRIM_SIZE 	    7 	Sets the prim's size. 	                [ PRIM_SIZE, vector size ]
// PRIM_TEXTURE 	17 	Sets the prim's texture attributes. 	[ PRIM_TEXTURE, integer face, string texture, vector repeats, vector offsets, float rotation_in_radians ]
// PRIM_TEXT 	    26 	Sets the prim's floating text. 	        [ PRIM_TEXT, string text, vector color, float alpha ]
// PRIM_COLOR 	    18 	Sets the face's color. 	                [ PRIM_COLOR, integer face, vector color, float alpha ]
// PRIM_BUMP_SHINY 19 	Sets the face's shiny & bump. 	        [ PRIM_BUMP_SHINY, integer face, integer shiny, integer bump ]
// PRIM_POINT_LIGHT 23 Sets the prim as a point light. 	        [ PRIM_POINT_LIGHT, integer boolean, vector color, float intensity, float radius, float falloff ]
// PRIM_FULLBRIGHT 20 	Sets the face's full bright flag. 	    [ PRIM_FULLBRIGHT, integer face, integer boolean ]
// PRIM_FLEXIBLE 	21 	Sets the prim as flexible. 	            [ PRIM_FLEXIBLE, integer boolean, integer softness, float gravity, float friction, float wind, float tension, vector force ]
// PRIM_TEXGEN 	22 	Sets the face's texture mode. 	            [ PRIM_TEXGEN, integer face, integer type ]
// PRIM_GLOW 	    25 	Sets the face's glow attribute. 	    [ PRIM_GLOW, integer face, float intensity ] 
//
default
{
    state_entry()
    {
        llSay(0,"Touch to see osSetPrimitiveParams work.");
    }
    touch_end(integer num)
    {
        osSetPrimitiveParams(PrimParams);
    }
}