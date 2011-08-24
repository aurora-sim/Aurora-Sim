default
{
    touch_start(integer number)
    {
	    //BOT_TAG_FIND_ALL returns all bots that are currently running in the sim
	    llSay(0, "Removing all of these bots: " + (string)botGetBotsWithTag(BOT_TAG_FIND_ALL));
		//Remove them all!
		botRemoveBotsWithTag(BOT_TAG_FIND_ALL);
    }
}