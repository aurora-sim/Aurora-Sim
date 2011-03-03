default
{
    state_entry()
    {
        llSetText("This is our floating text!", <1.0, 1.0, 1.0>, 1.0);
    } 
    touch_start(integer number)
    { 
        // aaGetText allows you to retrieve the text of the object
        llSay(0,aaGetText()); 
    }
}