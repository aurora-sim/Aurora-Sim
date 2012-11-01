//ascript
#useLSLAPI
#include Aurora.BotManager;

string first = "Zombie";
string last = "Bot";
string userToDuplicate;
string botID;
bool resetNext = false;
Bot ourBot = null;

public void state_entry()
{
    userToDuplicate = llGetOwner();
    botID = botCreateBot(first, last, userToDuplicate, llGetPos());
    botFollowAvatar(botID, llGetOwner(), 2.5, 2.5);
    
    IScenePresence sp = Scene.GetScenePresence(UUID.Parse(botID));
    if(sp != null)
    {
        ourBot = sp.ControllingClient as Bot;
        /// There are several events added so far,
        /// Update - called every 0.1s, allows for updating of the position of where the avatar is supposed to be goign
        /// Move - called every 10ms, allows for subtle changes and fast callbacks before the avatar moves toward its next location
        /// ToAvatar - a following event, called when the bot is within range of the avatar (range = m_followCloseToPoint)
        /// LostAvatar - a following event, called when the bot is out of the maximum range to look for its avatar (range = m_followLoseAvatarDistance)
        /// HereEvent - Triggered when a script passes TRIGGER_HERE_EVENT via botSetMap
        /// ChangedState = Triggered when the state of a bot changes
        ourBot.EventManager.RegisterEventHandler("HereEvent", HereEvent);
        ourBot.EventManager.RegisterEventHandler("ChangedState", ChangedState);
        ourBot.EventManager.RegisterEventHandler("Update", Update);
        ourBot.EventManager.RegisterEventHandler("Move", Move);
        ourBot.EventManager.RegisterEventHandler("ToAvatar", ToAvatar);
        ourBot.EventManager.RegisterEventHandler("LostAvatar", LostAvatar);
    }
}

public void exit()
{
    if(ourBot != null)
    {
        ourBot.EventManager.UnregisterEventHandler("HereEvent", HereEvent);
        ourBot.EventManager.UnregisterEventHandler("ChangedState", ChangedState);
        ourBot.EventManager.UnregisterEventHandler("Update", Update);
        ourBot.EventManager.UnregisterEventHandler("Move", Move);
        ourBot.EventManager.UnregisterEventHandler("ToAvatar", ToAvatar);
        ourBot.EventManager.UnregisterEventHandler("LostAvatar", LostAvatar);
    }
}

private object HereEvent(string funct, object param)
{
    llSay(0,"Bot has made it to the position!");
    return null;
}

private object Update(string funct, object param)
{
    return null;
}

private object Move(string funct, object param)
{
    return null;
}

private object ToAvatar(string funct, object param)
{
    llSay(0,"Bot has made it to the avatar it is following!");
    return null;
}

private object LostAvatar(string funct, object param)
{
    llSay(0,"Bot has lost the avatar it is following!");
    return null;
}

private object ChangedState(string funct, object param)
{
    llSay(0,"Bot has changed state to " + ourBot.State + "!");
    return null;
}

public void touch_start(LSL_Types.LSLInteger a)
{
    if(botID != "")
    {
        botRemoveBot(botID);
        botID = "";
    }
    else
    {
        llResetScript();
    }
}