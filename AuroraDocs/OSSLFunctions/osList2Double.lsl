// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osList2Double.lsl
// Script Author:
// Threat Level:    None
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osList2Double
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    float osList2Double(SrcList, integer index);
//
// Example of osList2Double
//
// Special Notes:
// Some Functions as shown use "double" as a Value instead of "float" these vary for purposes of accuracy as shown Below.
// Float is short for "floating point", and just means a number with a point something on the end.
// The difference between the two is in the size of the numbers that they can hold.
// For float, you can have up to 7 digits in your number.
// For doubles, you can have up to 16 digits. To be more precise, here's the official size: ( float: 1.5 × 10-45 to 3.4 × 1038 ) ( double: 5.0 × 10-324 to 1.7 × 10308 )
//
// for the example below, the return value is being dumped to string.
//
default
{
    state_entry() // display @ start
    {
        llSay(0, "Touch to see osList2Double convert double values to float");
    }
    touch_end(integer num)
    {
        list lDoubles = (["-4.42330604244772E-305","14009.349609375","0.100000001"]);
        llSay(0,"Values are: "+(string)osList2Double(lDoubles, 0)+" "+(string)osList2Double(lDoubles, 1)+" "+(string)osList2Double(lDoubles, 2));
    }
}
