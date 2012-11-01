//ascript
#useLSLAPI

public void state_entry()
{
}

public void touch_start(LSL_Types.LSLInteger a)
{
	//Get the terrain channel (the terrain)
	ITerrainChannel terrainModule = Scene.RequestModuleInterface<ITerrainChannel>();
	if(terrainModule != null) //Ask it for its height
		llSay(0, "The terrain height at the given position is " + terrainModule[(int)llGetPos().x, (int)llGetPos().y]);
}