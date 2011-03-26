// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osGetSimulatorMemory.lsl
// Script Author:   WSM
// Threat Level:    Moderate
// Script Source:   http://opensimulator.org/wiki/osGetSimulatorMemory
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// Inworld Script Line:    integer osGetSimulatorMemory();
//
// Example of osGetSimulatorMemory()
//
// Simple formatted Output
// shows either MB or GB as applicable
//
// ==== GET Memory Integer and Format for Display ====
GenStats()
{
    // Get Memory and format it
    string TotalMem;
    //string TotMemUsed;
    string Mem1;
    string Mem2;
    string TxtTail =" used by Simulator Instance";

    integer TotMemUsed = osGetSimulatorMemory();
    integer Len = llStringLength((string)TotMemUsed);

    if(Len == 8) // ##.### MB
    {
        Mem1 = llGetSubString((string)TotMemUsed,0,1);
        Mem2 = llGetSubString((string)TotMemUsed,2,4);
        TotalMem = Mem1+"."+Mem2+" Mb"+TxtTail;
    }
    else if(Len == 9) //###.### MB
    {
        Mem1 = llGetSubString((string)TotMemUsed,0,2);
        Mem2 = llGetSubString((string)TotMemUsed,3,5);
        TotalMem = Mem1+"."+Mem2+" Mb"+TxtTail;
    }
    else if(Len == 10) //#.### GB
    {
        Mem1 = llGetSubString((string)TotMemUsed,0,0);
        Mem2 = llGetSubString((string)TotMemUsed,1,3);
        TotalMem = Mem1+"."+Mem2+" Gb"+TxtTail;
    }
    else if(Len == 11) //##.### GB
    {
        Mem1 = llGetSubString((string)TotMemUsed,0,1);
        Mem2 = llGetSubString((string)TotMemUsed,2,4);
        TotalMem = Mem1+"."+ Mem2+" Gb"+TxtTail;
    }
    // Uncomment next line ot have Text Display above prim
    //llSetText(TotalMem, <0.0,1.0,0.0>, 1.0 );
    llSay(0,"Total Memory Used "+TotalMem);
}

default
{
    state_entry() // display @ start
    {
        GenStats();
    }

    touch_end(integer num) // refresh on touch
    {
        GenStats();
    }
}