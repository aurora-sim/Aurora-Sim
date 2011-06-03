default
{
    state_entry()
    {
        llSetText("This is our floating text!", <0.0, 1.0, 1.0>, 0.5);
    } 
    touch_start(integer number)
    { 
        // aaGetTextColor allows you to retrieve the color of the text on the object
        rotation color = aaGetTextColor();
        float r = color.x;
        float g = color.y;
        float b = color.z;
        float a = color.s;
        llSay(0,r + "," + g + "," + b + "," + a); 
    }
}