// ----------------------------------------------------------------
// Example / Sample Script to show function use.
//
// Script Title:    osSetParcelDetails.lsl
// Script Author:   WhiteStar Magic
// Threat Level:    High
// Script Source:   SUPPLEMENTAL http://wiki.aurora-sim.org/index.php?title=OsSetParcelDetails
//
// Notes: See Script Source reference for more detailed information
// This sample is full opensource and available to use as you see fit and desire.
// Threat Levels only apply to OSSL & AA Functions
// See http://wiki.aurora-sim.org/index.php?title=Threat_Levels
// ================================================================
// C# Source Line:      public void osSetParcelDetails(LSL_Vector pos, LSL_List rules)
// Inworld Script Line:     osSetParcelDetails(vector pos, list rules);
//
// Example of osSetParcelDetails
// This function allows for setting parcels information programmatically.
// -- constants for osSetParcelDetails
//    PARCEL_DETAILS_NAME = 0;
//    PARCEL_DETAILS_DESC = 1;
//    PARCEL_DETAILS_OWNER = 2;
//    PARCEL_DETAILS_GROUP = 3;
//
default
{
    state_entry()
    {
        llSay(0,"Touch to use osSetParcelDetails Parcels");
    }
    touch_start(integer total_num)
    {
        vector position = <128.0, 128.0, 0.0>;       //Parcel Location: centre of region
        string name = "My New Land ";                //Parcel Name to set
        string descript = "My New Land Description"; //Parcel Description text
        key owner = llGetOwner();                    //Parcel Owners UUID
        key group = NULL_KEY;                        //Parcel Group UUID
        // setup the Rules List with the above values 
        list rules =[
            PARCEL_DETAILS_NAME, name,
            PARCEL_DETAILS_DESC, descript,
            PARCEL_DETAILS_OWNER, owner,
            PARCEL_DETAILS_GROUP, group];
        osSetParcelDetails(position, rules);
    }
}
