string first = "Test";
string last = "Bot";
key userToDuplicate;
string botID;
string message = "Hi avatar!";

// sayType 0 = Whisper
// sayType 1 = Say
// sayType 2 = Shout
integer sayType = 1;

//Channel to talk on
integer channel = 0;
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
        //Say the message to users
        botSendChatMessage (botID, message, channel, sayType);
        botSendIM (botID, llGetOwner(), message);
    }
}