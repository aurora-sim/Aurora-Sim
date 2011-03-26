// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetFontName_osSetFontSize.lsl
// Script Author:
// Threat Level:    None
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetFontName
//                  SUPPLEMENTAL http://opensimulator.org/wiki/osSetFontSize
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public string osSetFontName(string drawList, string fontName)
// C# Source Line:      public string osSetFontSize(string drawList, int fontSize)
// Inworld Script Line: osSetFontName(string sCommandList, string sFontName);
// Inworld Script Line: osSetFontSize(string sCommandList, integer iFontSize);
//
// Example of osSetFontName & osSetFontSize
//
// Example of osDrawText - Highlighting osSetFontName & osSetFontSize
// For font families which can be used SEE:  http://www.w3schools.com/css/css_websafe_fonts.asp
//
integer iFlag = TRUE;
string sCommandList = "";   // Storage for our drawing commands
string sFontName = "Arial"; // Arial is the default font used, if unspecified
integer iFontSize = 14;     // default to 24 point for sample
integer iX = 10;            // used for osMovePen (X coord) from Top Left In
integer iY = 10;            // used for osMovePen (Y coord) from Top Left Down
string sText;
//
DrawText()
{
    sCommandList = osSetFontName(sCommandList, sFontName);
    sCommandList = osSetFontSize(sCommandList, iFontSize);
    sCommandList = osMovePen( sCommandList, iX, iY );       // Upper left corner at <pixels in, pixels down>
    sCommandList = osDrawText( sCommandList, sText);        // The Text to Display
    // Now draw the image
    llWhisper(0,"FontName = "+sFontName+" FontSize = "+(string)iFontSize);
    osSetDynamicTextureData( "", "vector", sCommandList, "width:512,height:512", 0 );
}
default
{
    state_entry()
    {
        llSay(0, "Touch to see how changing osSetFontName & osSetFontName work");
        sText = "FontName = "+sFontName+"\nFontSize = "+(string)iFontSize;
        DrawText();
    }
    touch_start(integer num)
    {
        if(iFlag)
        {
            iX = 10;
            iY = 50;
            iFlag = FALSE;
            sFontName = "Times";
            iFontSize = 18;
            sText = "FontName = "+sFontName+"\nFontSize = "+(string)iFontSize;
            DrawText();
        }
        else
        {
            iX = 10;
            iY = 100;
            iFlag = TRUE;
            sFontName = "Courier";
            iFontSize = 22;
            sText = "FontName = "+sFontName+"\nFontSize = "+(string)iFontSize;
            DrawText();
        }
    }
}