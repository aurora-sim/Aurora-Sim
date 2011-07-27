default
{
    state_entry()
    {
        //Disable normal gravity
        aaSetEnv(ENABLE_GRAVITY, [0]);
        
        float GravPower = 1;
        float Radius = 20;
        integer identifer = 1;
        
        //Add two points, 20m apart
        aaSetEnv(ADD_GRAVITY_POINT, [llGetPos(), GravPower, Radius, identifer++]);
        aaSetEnv(ADD_GRAVITY_POINT, [llGetPos() + <0, 0, 20>, GravPower, Radius, identifer]);
    } 
}