string first = "Test";
string last = "Bot";
key userToDuplicate;
string botID;
string tag = "removeMe";

default
{
    state_entry()
    {
        //On startup, we'll generate a new bot, then make it move when we touch it
        //Create the bot with the given first/last name and the user whose appearance it will duplicate
        userToDuplicate = llGetOwner();
		vector startPos = llGetPos();
        botID = botCreateBot(first + "1", last, userToDuplicate, startPos);
		botAddTag(botID, tag);
        botID = botCreateBot(first + "2", last, userToDuplicate, startPos);
		botAddTag(botID, tag);
        botID = botCreateBot(first + "3", last, userToDuplicate, startPos);
		botAddTag(botID, tag);
    }
    touch_start(integer number)
    {
	    //BOT_TAG_FIND_ALL returns all bots that are currently running in the sim
	    llSay(0, "All bots: " + (string)botGetBotsWithTag(BOT_TAG_FIND_ALL));
		//Now remove our tagged ones
	    llSay(0, "Found these bots with the tag: " + (string)botGetBotsWithTag(tag));
		botRemoveBotsWithTag(tag);
    }
}