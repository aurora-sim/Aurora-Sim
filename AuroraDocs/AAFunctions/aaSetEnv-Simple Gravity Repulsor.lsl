default
{
    state_entry()
    {
        //Disable normal gravity
        aaSetEnv(ENABLE_GRAVITY, [0]);
        
        float xPower = 0;
        float yPower = 0;
        float zPower = 150;
        float Radius = 2;
        integer identifer = 1;
        
        //Forces things upwards
        aaSetEnv(ADD_GRAVITY_FORCE, [llGetPos(), xPower, yPower, zPower, Radius, identifer]);
    }
}