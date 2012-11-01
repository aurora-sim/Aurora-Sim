//ascript
#useLSLAPI

public void state_entry()
{
	//First get the IScenePresence from the Scene.
	IScenePresence SP = Scene.GetScenePresence(UUID.Parse(llGetOwner()));
	llSay(0, SP.Name); //Say their name, then teleport them
	SP.Teleport(new Vector3((float)llGetPos().x,(float)llGetPos().y,(float)llGetPos().z));
}
