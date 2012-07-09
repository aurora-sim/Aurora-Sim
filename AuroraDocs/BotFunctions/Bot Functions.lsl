string first = "Test";
string last = "Bot";
key userToDuplicate;
string botID;
integer paused = 0;
default
{
    state_entry()
    {
        //On startup, we'll generate a new bot, then make it move when we touch it
        //Create the bot with the given first/last name and the user whose appearance it will duplicate
        userToDuplicate = llGetOwner();
        vector startPos = llGetPos();
        botID = botCreateBot(first, last, userToDuplicate, startPos);
        
        //Tell the bot how it will get there
        //BOT_FOLLOW_WALK - Walk to the next target
        //BOT_FOLLOW_RUN - Run to the next target
        //BOT_FOLLOW_FLY - Fly to the next target
        //BOT_FOLLOW_TELEPORT - Teleports to the next target
        //BOT_FOLLOW_WAIT - Waits for the given amount of time
        //BOT_FOLLOW_TRIGGER_HERE_EVENT - Trigger the HereEvent for AScripts (advanced)
        
        //Flys to the first 3, then waits for 3 seconds (set below in the position code)
        list types = [BOT_FOLLOW_FLY, 
        BOT_FOLLOW_FLY,
        BOT_FOLLOW_FLY,
        BOT_FOLLOW_WAIT];
        
        //Now give it a list of positions to go around, the last is the special Wait time function
        list positions = [llGetPos() + <0, 0, 1>, //Don't put the bot in the ground
						  llGetPos() + <0, 20, 20>, 
						  llGetPos() + <20, 0, 20>, 
						  botGetWaitingTime(3)];
        
        //Now tell the bot what to do
        //The last parameter is the Flags parameter
        //You can pass through BOT_FOLLOW_FLAG_INDEFINITELY to make the bot follow indefinitely and continue to loop through all the positions
        // Or you can pass through BOT_FOLLOW_FLAG_NONE to make the bot stop after going through all the positions given
        botSetMap(botID, positions, types, BOT_FOLLOW_FLAG_INDEFINITELY);
    } 
    touch_start(integer number)
    {
        if(paused == 0)
           botPauseMovement(botID);
        else if(paused == 1)
           botResumeMovement(botID);
        else
        {
            botRemoveBot(botID);
            paused = 0;
        }
        paused++;
    }
}