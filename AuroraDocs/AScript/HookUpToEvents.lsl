//ascript
#useLSLAPI

public void state_entry()
{
    //Attach to the event
    Scene.EventManager.OnSignificantClientMovement += SignificantClientMovement;
}

public void exit()
{
    //REMOVE THE EVENT (VERY IMPORTANT, ALWAYS REMOVE ANY EVENTS YOU ADD, OTHERWISE THEY KEEP GOING EVEN WHEN THE SCRIPT IS DELETED)
    Scene.EventManager.OnSignificantClientMovement -= SignificantClientMovement;
}

public void SignificantClientMovement(IClientAPI remote_client)
{
    //A client moved in the scene!
    llShout(0, remote_client.Name + " moved!");
}