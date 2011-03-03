default
{
    state_entry()
    {
        // If you have the Center Of Gravity option enabled
        //  in your sim, you can use this function to change
        //  where the center is while the sim is running
        //  (The option is in AuroraOpenDynamicsEngine.ini)
        // In this example, we will be setting the new
        //  center of gravity to 128,128,128, so everything
        //  will go toward there
        aaSetCenterOfGravity(<128,128,128>);
    } 
    touch_start(integer number)
    { 
        // You also can do gravity on just certain axi,
        //  such as the X and Z by setting one of the axis
        //  values to 0.
        // In this case, gravity will pull objects to the
        // +X (256) axis and the -Z (-1) axis
        aaSetCenterOfGravity(<256, 0, -1>);
    }
}