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
        botID = botCreateBot(first, last, userToDuplicate);
        
        llListen( 0, "", NULL_KEY, "" ); 
    } 
    touch_start(integer number)
    {
        //Now give it a list of positions to go around
        list positions = [llGetPos(), llGetPos() + <0, 20, 20>, llGetPos() + <20, 0, 20>];
        //Now tell it how it will get there
        //0 - Walk to the next target
        //1 - Fly to the next target
        list types = [1,1,1];
        //Now tell the bot what to do
        botSetMap(botID, positions, types);
    }
    listen( integer channel, string name, key id, string message )
    {
        if ( id == llGetOwner() )
        {
            if(message == "pause")
            {
                //This disables the bots movement, however, the bot will warp to its next location once the alloted time runs out for movement
                botPause(botID);
            }
            if(message == "resume")
            {
                //This reenables movement for the bot and does not turn on the movement timer
                botResume(botID);
            }
            if(message == "stop")
            {
                //This disables the bots movement, as well as the auto warp that will occur if the bot does not get to its position in the alloted period of time
                botStop(botID);
            }
            if(message == "start")
            {
                //This reenables movement for the bot and does turn on the movement timer
                botStart(botID);
            }
        }
    }
}