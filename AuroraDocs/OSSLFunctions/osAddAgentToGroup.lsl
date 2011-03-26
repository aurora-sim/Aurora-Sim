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
// Inworld Script Line:  osAddAgentToGroup(key AgentID, string GroupName, string RequestedRole);
//
// PREREQUISITES 
// - The Group must be created
// - You must have the Group UUID
// - Roles within the group must be defined (default has Everyone & Owners)
//
//
string GroupToJoin = "Test Group";
string RoleToJoin = "Everyone";

default
{
    state_entry()
    {
        llSay(0,"Touch to use osAddAgentToGroup to add yourself to a group"); 
    }
    
    touch_end(integer num)
    {
        key AgentID = llDetectedKey(0);
        osAddAgentToGroup(AgentID, GroupToJoin, RoleToJoin);
    }
}