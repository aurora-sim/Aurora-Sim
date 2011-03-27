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
        //You can either put an avatar's name or UUID here
        botFollowAvatar(botID, llGetOwner());
    }
    touch_start(integer a)
    {
        botRemoveBot(botID);
    }
}