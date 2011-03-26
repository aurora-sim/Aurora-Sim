// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osMessageObject.lsl
// Script Author:
// Threat Level:    Low
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osMessageObject
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    osMessageObject(key objectUUID, string message);
//
// Example of osMessageObject
//
// SPECIAL NOTE
// send a message to to object identified by the given UUID,
// a script in the Receiving object must implement the dataserver function
// the dataserver function is passed the ID of the calling function and a string message
// 
//
default
{
    state_entry()
    {
        llSay(0, "Touch me to use osMessageObject to message an object");
    }
    touch_end(integer total_num)
    {
        key kTargetObj = "UUID"; //INSERT A VALID Object UUID here
        string sSentence = "This message sent from a Sending object using osMessageObject";
        osMessageObject(kTargetObj,sSentence);
    }
}
// ==== SAMPLE Receiver Script (commented out)
// Place this script in the Receiver prim and un-comment following lines
//default
//{
//    state_entry()
//    {
//        llSay(0, "osMessageObject Receiver Ready\nPlease replace UUID in osMessageObject Script (line 31) kTargetObj = "+(string)llGetKey());
//    }
//    dataserver(key query_id, string data)
//    {
//        llSay(0, "RECEIVER: The message received.\n\t query_id = "+(string)query_id+"\n\t msg = "+data);
//    }
//}
