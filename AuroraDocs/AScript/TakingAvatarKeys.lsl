//ascript
#useLSLAPI

public void state_entry()
{
	IScenePresence SP = Scene.GetScenePresence(UUID.Parse(llGetOwner()));
	if(SP != null)
	{
		SP.ControllingClient.OnAgentUpdate += AgentUpdate;
	}
}

//MUST be here so that if we are deleted, we don't keep going with the event
public void exit()
{
	IScenePresence SP = Scene.GetScenePresence(UUID.Parse(llGetOwner()));
	if(SP != null)
	{
		SP.ControllingClient.OnAgentUpdate -= AgentUpdate;
	}
}

public void AgentUpdate(IClientAPI remoteClient, AgentUpdateArgs agentData)
{
	if (agentData.ControlFlags == (int)AgentManager.ControlFlags.AGENT_CONTROL_LBUTTON_DOWN)
	{
		llSay(0,"Left Mouse Down!");
	}
}