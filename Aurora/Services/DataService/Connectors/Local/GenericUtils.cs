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
            data.FromKVP(Util.OSDToDictionary(map));
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
