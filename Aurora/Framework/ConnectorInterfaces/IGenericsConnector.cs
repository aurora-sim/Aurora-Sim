using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
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
    public interface IGenericsConnector
	{
        /// <summary>
        /// Gets a Generic type as set by the ownerID, Type, and Key
        /// </summary>
        /// <typeparam name="T">return value of type IDataTransferable</typeparam>
        /// <param name="OwnerID"></param>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        /// <param name="data">a default T to copy all data into</param>
        /// <returns></returns>
        T GetGeneric<T>(UUID OwnerID, string Type, string Key, T data) where T : IDataTransferable;
        
        /// <summary>
        /// Gets a list of generic T's from the database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="OwnerID"></param>
        /// <param name="Type"></param>
        /// <param name="data">a default T</param>
        /// <returns></returns>
        List<T> GetGenerics<T>(UUID OwnerID, string Type, T data) where T : IDataTransferable;
        
        /// <summary>
        /// Adds a generic IDataTransferable into the database
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        void AddGeneric(UUID AgentID, string Type, string Key, OSDMap Value);
        
        /// <summary>
        /// Removes a generic IDataTransferable from the database
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        void RemoveGeneric(UUID AgentID, string Type, string Key);
        
        /// <summary>
        /// Removes a generic IDataTransferable from the database
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="Type"></param>
        void RemoveGeneric(UUID AgentID, string Type);
	}
}
