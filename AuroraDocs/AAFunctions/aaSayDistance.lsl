default
{
    state_entry()
    {
        // With aaSayDistance you can customize the length
        //  your message will be heard
        // In this test scenario, we will have a message
        //  on channel 0 that can be heard from 1 meter
        // away, then 200.
        aaSayDistance(0, 1.0, "Test 1 meter");
        aaSayDistance(0, 200.0, "Test 200 meters");
    }
}