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
        botSetMap(botID, positions, types);
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