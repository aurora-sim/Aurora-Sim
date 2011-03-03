// How much to heal the client
float healing = 10;
// How much to damage the client
float damage = 10;

// If we are the object that kills the avatar, we
//  can decide where to put the avatar where we want.
//  Set these to put the avatar somewhere.
string regionName = "";
vector positionToPutDeadAvatar = <128,128,128>;
default
{
    state_entry()
    {
    }
    touch_start(integer num)
    {
        // Heal the client for touching us
        osCauseHealing(llDetectedKey(0), healing);
    }
    collision_start(integer num)
    {
        // Someone bumped into us, lets damage them
        // If the regionName is "", lets let them go home
        if(regionName == "") //No position setting
            osCauseDamage(llDetectedKey(0), damage);
        else //Put them where we want them
            osCauseDamage(llDetectedKey(0), damage, 
                regionName, positionToPutDeadAvatar, <0,0,0>);
    }
}