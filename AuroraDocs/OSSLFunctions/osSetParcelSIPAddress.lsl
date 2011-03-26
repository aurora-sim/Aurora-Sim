// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetParcelSIPAddress.lsl
// Script Author:
// Threat Level:    VeryLow
// Script Source:   SUPPLEMENTAL http://opensimulator.org/wiki/osSetParcelSIPAddress
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://opensimulator.org/wiki/Threat_level
// ================================================================
// C# Source Line:      public void osSetParcelSIPAddress(string SIPAddress)
// Inworld Script Line: osSetParcelSIPAddress(string sSIPAddress);
//
// Example of osSetParcelSIPAddress
//
/// Set the SIP url to be used by a parcel, this will allow manual setting of a SIP address
/// for a particular piece of land, allowing region owners to use preconfigured SIP conference channels.
/// This is used by osSetParcelSIPAddress
// --- SEE freeswitch_sip_proxy = ip.address.of.freeswitch.server:5060
// --- SEE vivox_sip_uri = foobar.vivox.com

string sSIPAddress = "ip.address.of.freeswitch.server:5060"; //The SIP address we are setting
//
default
{
    state_entry()
    {
        llSay(0, "Touch to see how osSetParcelSIPAddress works");
    }
    touch_start(integer num)
    {
        llSay(0,"SIP Address being set :"+sSIPAddress);
        osSetParcelSIPAddress( sSIPAddress);
    }
}