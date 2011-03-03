default
{
    touch_start(integer number)
    { 
        // With aaSayTo you can do the same functionality 
        //  of llOwnerSay, but to any user instead of just
        //  the owner.
        // In this case, we will tell the person who
        //  touched us that they touched us.
        aaSayTo(llDetectedKey(number), "You touched me!");
    }
}