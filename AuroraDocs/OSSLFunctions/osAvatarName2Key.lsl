// ----------------------------------------------------------------
// Example / Sample Script to show function use.
// 
// Script Title:    osAvatarName2Key.lsl
// Author:          WhiteStar Magic
// Threat Level OSSL/AA: LOW
// Script Source:
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
//================================================================
// Inworld Script Line:  osAvatarName2Key(string FirstName,string LastName);
//
// 
default
{
   state_entry()
   {
       llSay(0, "Touch to convert an Avatar Name to KEY");
   }
   
   touch_end(integer num)
   {
       list nameparts   = llParseString2List(llKey2Name(llDetectedKey(0)), [" "], [" "]);
       string FirstName = llList2String(nameparts,0);
       string LastName  = llList2String(nameparts,1);
       //
       key kAvatarKey = osAvatarName2Key(FirstName, LastName);
       llInstantMessage(kAvatarKey, "The user key is: "+(string)kAvatarKey);    
   }
}
