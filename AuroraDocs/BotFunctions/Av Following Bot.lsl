string first = "Test";
string last = "Bot";
key userToDuplicate;
string botID;
default
{
    state_entry()
    {
        //On startup, we'll generate a new bot, then make it move when we touch it
        //Create the bot with the given first/last name and the user whose appearance it will duplicate
        userToDuplicate = llGetOwner();
		vector startPos = llGetPos();
        botID = botCreateBot(first, last, userToDuplicate, startPos);
        //You can either put an avatar's name or UUID as the second parameter, and then the last parameter is how close it should get to the avatar
        //The third and fourth parameters have to do with how far away the avatar can be before it follows
		// The third is how far away the avatar can be before it begins to follow them, and the fourth is how far away the avatar has to be before it stops attempting to follow the avatar
		botFollowAvatar(botID, llGetOwner(), 2, 2);
    }
    touch_start(integer a)
    {
	    //Stop following the avatar now
        botStopFollowAvatar (botID);
    }
}