using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.Services.DataService
{
    /// <summary>
    /// Some background to this class
    /// 
    /// This class saves any class that implements the IDataTransferable interface.
    ///   When implementing the IDataTransferable interface, it is heavily recommending to implement ToOSD and FromOSD first, then use the Utility methods to convert OSDMaps into Dictionarys, as shown in the LandData class.
    /// 
    /// This method of saving uses 4 columns in the database, OwnerID, Type, Key, and Value
    /// 
    ///   - OwnerID : This is a way to be able to save Agent or Region or anything with a UUID into the database and have it be set to that UUID only.
    ///   - Type : What made this data? This just tells what module created the given row in the database.
    ///   - Key : Another identifying setting so that you can store more than one row under an OwnerID and Type
    ///   - Value : The value of the row
    /// 
    /// This class deals with the Getting/Setting/Removing of these generic interfaces.
    /// 
    /// </summary>
    public class GenericUtils
    {
        /// <summary>
        /// Gets a Generic type as set by T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="OwnerID"></param>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        /// <param name="GD"></param>
        /// <param name="data">a default T to copy all data into</param>
        /// <returns></returns>
        public static T GetGeneric<T>(UUID OwnerID, string Type, string Key, IGenericData GD, T data) where T : IDataTransferable
        {
            List<string> retVal = GD.Query(new string[] { "OwnerID", "Type", "`Key`" }, new object[] { OwnerID, Type, Key }, "generics", "`value`");
            
            if (retVal.Count == 0)
                return null;

            OSDMap map = (OSDMap)OSDParser.DeserializeJson(retVal[0]);
            data.FromOSD(map);
            return data;
        }

        /// <summary>
        /// Gets a list of generic T's from the database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="OwnerID"></param>
        /// <param name="Type"></param>
        /// <param name="GD"></param>
        /// <param name="data">a default T</param>
        /// <returns></returns>
        public static List<T> GetGenerics<T>(UUID OwnerID, string Type, IGenericData GD, T data) where T : IDataTransferable
        {
            List<T> Values = new List<T>();
            List<string> retVal = GD.Query(new string[] { "OwnerID", "Type" }, new object[] { OwnerID, Type }, "generics", "`value`");
            foreach (string ret in retVal)
            {
                OSDMap map = (OSDMap)OSDParser.DeserializeJson(ret);
                data.FromOSD(map);
                T dataCopy = (T)data.Duplicate();
                Values.Add(dataCopy);
            }
            return Values;
        }

        /// <summary>
        /// Adds a generic into the database
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <param name="GD"></param>
        public static void AddGeneric(UUID AgentID, string Type, string Key, OSDMap Value, IGenericData GD)
        {
            GD.Replace("generics", new string[] { "OwnerID", "Type", "`Key`", "`Value`" }, new object[] { AgentID, Type, Key, OSDParser.SerializeJsonString(Value) });
        }

        /// <summary>
        /// Removes a generic from the database
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        /// <param name="GD"></param>
        public static void RemoveGeneric(UUID AgentID, string Type, string Key, IGenericData GD)
        {
            GD.Delete("generics", new string[] { "OwnerID", "Type", "`Key`" }, new object[] { AgentID, Type, Key });
        }

        /// <summary>
        /// Removes a generic from the database
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="Type"></param>
        /// <param name="GD"></param>
        public static void RemoveGeneric(UUID AgentID, string Type, IGenericData GD)
        {
            GD.Delete("generics", new string[] { "OwnerID", "Type" }, new object[] { AgentID, Type });
        }
    }
}
