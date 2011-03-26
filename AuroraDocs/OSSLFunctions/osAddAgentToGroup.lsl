// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osAddAgentToGroup.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    None
// Script Source:   Aurora-Sim osFunction
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
//================================================================
// Inworld Script Line:  osAddAgentToGroup(key AgentID, key GroupID, key RequestedRoleID);
//
// PREREQUISITES 
// - The Group must be created
// - You must have the Group UUID
// - Roles within the group must be defined (default has Everyone & Owners)
//
key GroupID = NULL_KEY;
key RequestedRoleID = NULL_KEY;
//
integer Touched = FALSE;

default
{
    state_entry()
    {
        llSay(0,"Touch to use osAddAgentToGroup to add yourself to a group"); 
    }
    
    touch_end(integer num)
    {
        key AgentID = llDetectedKey(0);
        //
        GroupID = llList2Key(llGetObjectDetails(llGetKey(), [OBJECT_GROUP]),0);
        if(GroupID == "" || GroupID == NULL_KEY)
        {
            llSay(0,"ERROR: GroupID for this object could not be determined.\n\tPlease ensure that Object is set to a valid Group and try again");
            return;
        }
        // how to get next line ?
        //  RequestedRoleID = "";
        //
        osAddAgentToGroup(AgentID, GroupID, RequestedRoleID);
        
    }
}