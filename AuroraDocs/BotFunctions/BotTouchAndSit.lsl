string first = "Test";
string last = "Bot";
key userToDuplicate;
string botID;

integer currentlySitting = 0;
default
{
    state_entry()
    {
        //On startup, we'll generate a new bot, then make it move when we touch it
        //Create the bot with the given first/last name and the user whose appearance it will duplicate
        userToDuplicate = llGetOwner();
		vector startPos = llGetPos();
        botID = botCreateBot(first, last, userToDuplicate, startPos);
    }
    touch_start(integer number)
    {
	    if(currentlySitting == 0)
	        botSitObject(botID, llGetKey(), ZERO_VECTOR); //Sit on this object with the default sitting position
	    if(currentlySitting == 1)
	        botStandUp(botID);//Now stand up off this object
	    if(currentlySitting == 2)
		    botTouchObject(botID, llGetKey());//Now touch this object
		if(currentlySitting == 3)
		    currentlySitting = -1;//Reset after the bot touches the box (#2)
		currentlySitting++;
    }
}