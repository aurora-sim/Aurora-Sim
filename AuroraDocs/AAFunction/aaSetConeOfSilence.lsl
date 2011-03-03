default
{
    state_entry()
    {
        //This creates a 'Cone of Silence' a radius
        // 10 meters away from this object's current position.
        // This creates an area that sounds that are in it can only be 
        // heard by the people in it and not by the people outside and visa-versa, 
        // a little seperate sound region seperate from the rest of the region.
        aaSetConeOfSilence(10);
    }
}