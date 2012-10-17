//ascript
#useLSLAPI
#include Aurora.BotManager;

string first = "Zombie";
string last = "Bot";
string userToDuplicate;
string botID;
bool resetNext = false;

public void state_entry()
{
    userToDuplicate = llGetOwner();
    botID = botCreateBot(first, last, userToDuplicate, llGetPos());
    botFollowAvatar(botID, llGetOwner(), 2.5, 2.5);
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