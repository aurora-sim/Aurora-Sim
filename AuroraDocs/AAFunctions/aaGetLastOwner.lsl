default
{
    touch_start(integer number)
    { 
        // Say the last owner of the prim
        llSay(0,"The last owner of this prim was " + aaGetLastOwner() + ".");

        // You can also pass it a UUID of a prim to find
        //  it's last owner as well
        
        // This code doesn't work, as the av who touched 
        //   it doesn't have a last owner, but it 
        //   demonstrates how to do it.
        llSay(0,"The last owner of this avatar who touched me was " + aaGetLastOwner(llDetectedKey(number)) + ".");
    }
}