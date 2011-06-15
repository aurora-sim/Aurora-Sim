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
	    llSay(0, "Found these bots with the tag: " + (string)botGetBotsWithTag(tag));
		botRemoveBotsWithTag(tag);
    }
}