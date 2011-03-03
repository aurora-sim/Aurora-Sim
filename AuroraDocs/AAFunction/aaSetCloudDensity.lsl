default
{
    state_entry()
    {
        //Set the cloud density to 1
        aaSetCloudDensity(1.0);
    } 
    touch_start(integer number)
    {
        //Set the cloud density to half
        aaSetCloudDensity(0.5); 
    }
}