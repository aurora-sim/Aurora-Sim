//This is the data we want to put into the database in
// a key/value pair with lists
list dataKeys = ["Key"];
list dataValues = ["TestData"];
//We need a key to identify it when we pull it out
string Key = "TestKey";
//Finally, we need some password so that other prims 
// and users cannot access this info
string Token = "oiasjdf";

default
{
    state_entry()
    {
        //First, change the lists into an xml string so that it can go into the database easily
        string xmlDataToPutIntoTheDatabase = aaSerializeXML(dataKeys, dataValues);
        //Put the info in the database now
        aaUpdateDatabase(Key, xmlDataToPutIntoTheDatabase, Token);
    } 
    touch_start(integer number)
    { 
        //Now we need to retrieve the info from the database
        list queryData = aaQueryDatabase(Key, Token);
        //Now that we have the info, we need to pull the keys and values back out of the third value in the database.
        //The first value is the token, the second the Key, and the third, the value
        string retrievedXML = llList2String(queryData,2);
        //Pull the keys out of the xml
        list retrievedKeys = aaDeserializeXMLKeys(retrievedXML);
        //Pull the values out of the xml
        list retrievedValues = aaDeserializeXMLValues(retrievedXML);
        
        llSay(0,"Keys: " + retrievedKeys + ", Values: " + retrievedValues);
    }
}