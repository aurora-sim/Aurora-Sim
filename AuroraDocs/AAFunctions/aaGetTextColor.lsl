default
{
    state_entry()
    {
        llSetText("This is our floating text!", <0.0, 1.0, 1.0>, 0.5);
    } 
    touch_start(integer number)
    { 
        // aaGetTextColor allows you to retrieve the color of the text on the object
		Rotation color = aaGetTextColor();
		float r = color.X;
		float g = color.Y;
		float b = color.Z;
		float a = color.Q;
        llSay(0,r + "," + g + "," + b + "," + a); 
    }
}