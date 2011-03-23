default
{
    state_entry()
    {
        // With this function, you can get the UUID of
        //  an avatar by their full name, not just by 
        //  their first and last.
        // In this example, we'll find the user
        //  "Revolution Smythe"'s UUID.
        
        key id = aaAvatarFullName2Key("Revolution Smythe");
        llSay(0, id);
    } 
    touch_start(integer number)
    { 
        llSay(0,"Touched."); 
    }
}