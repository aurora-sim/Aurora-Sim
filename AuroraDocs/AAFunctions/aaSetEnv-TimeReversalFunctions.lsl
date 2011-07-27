integer isReverting = true;
default
{
    state_entry()
    {
        llSay(0, "Collecting reversal data...");
        aaSetEnv(START_TIME_REVERSAL_SAVING, []);
        llSetTimerEvent(10);
    }
    touch_start(integer number)
    {
        if(isReverting)
        {
            //Stop reversing time
            llSetTimerEvent(0);
            llSay(0, "Stopping the reversal...");
            aaSetEnv(STOP_TIME_REVERSAL, []);
        }
        else
        {
            llResetScript();
        }
        isReverting = !isReverting;
    }
    timer()
    {
        //Start reversing the last 10s
        llSay(0, "Reversing the last 10 seconds...");
        isReverting = FALSE;
        llSetTimerEvent(0);
        aaSetEnv(START_TIME_REVERSAL, []);
    }
}
