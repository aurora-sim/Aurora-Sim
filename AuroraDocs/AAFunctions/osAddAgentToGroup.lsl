//The group name
string group_name = "Test Group";
//The role to add users into
string role_name = "Everyone";
default
{
    touch_start(integer number)
    { 
        // With osAddAgentToGroup, you can add a person to a group of your choise easily.
        // Syntax: osAddAgentToGroup (LSL_Key AgentID, LSL_String GroupName, LSL_String RequestedRole)
        // In this test script, it will send an invite for "Test Group" to the person 
        // who clicked on this prim
        osAddAgentToGroup(llDetectedKey(0), group_name, role_name);
    }
}