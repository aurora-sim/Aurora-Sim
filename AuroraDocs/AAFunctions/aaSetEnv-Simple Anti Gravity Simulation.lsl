float currentGrav = -9.8;
default
{
    state_entry()
    {
        llSay(0, "Script running.");
    } 
    touch_start(integer number)
    {
        llSetTimerEvent(0.25);
    }
    timer()
    {
        currentGrav += 1;
        if(currentGrav == -0.8)
            currentGrav = 0;
        if(currentGrav >= 2)
            llSetTimerEvent(0);
        aaSetEnv(GRAVITY_FORCE_Z, [currentGrav]);
    }
}