string first = "Test";
string last = "Bot";
key userToDuplicate;
string botID;
integer paused = FALSE;
default
{
    state_entry()
    {
        //On startup, we'll generate a new bot, then make it move when we touch it
        //Create the bot with the given first/last name and the user whose appearance it will duplicate
        userToDuplicate = llGetOwner();
        botID = botCreateBot(first, last, userToDuplicate);
        
        //Now give it a list of positions to go around
        list positions = [llGetPos(), llGetPos() + <0, 20, 20>, llGetPos() + <20, 0, 20>];
        //Now tell it how it will get there
        //0 - Walk to the next target
        //1 - Fly to the next target
        list types = [1,1,1];
        //Now tell the bot what to do
		//The last parameter is the Flags parameter
		//You can pass through BOT_FOLLOW_FLAG_INDEFINITELY to make the bot follow indefinitely and continue to loop through all the positions
		// Or you can pass through BOT_FOLLOW_FLAG_NONE to make the bot stop after going through all the positions given
        botSetMap(botID, positions, types, BOT_FOLLOW_FLAG_INDEFINITELY);
    } 
    touch_start(integer number)
    {
        if(paused)
           botResumeMovement(botID);
        else if(!paused)
           botPauseMovement(botID);
        else
        {
            botRemoveBot(botID);
            paused = FALSE;
        }
        paused++;
    }
}