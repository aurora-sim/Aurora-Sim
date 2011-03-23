on_error(string message)
{
    // We errored out somewhere in the script, now we need 
    //  to report the error.
    llSay(0,"We errored out, error: " + message);
}

default
{
    touch_start(integer number)
    { 
        // aaRaiseError allows you to have errors that end
        //  the execution of the event and fire another
        //  event in the script.
        aaRaiseError("We errored out on touch!");
    }
}