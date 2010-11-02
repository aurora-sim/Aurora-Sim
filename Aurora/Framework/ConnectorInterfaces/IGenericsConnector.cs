using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
	public interface IGenericsConnector
	{
        /// <summary>
        /// Gets a Generic type as set by T
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
        /// Adds a generic into the database
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        void AddGeneric(UUID AgentID, string Type, string Key, OSDMap Value);
        
        /// <summary>
        /// Removes a generic from the database
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        void RemoveGeneric(UUID AgentID, string Type, string Key);
        
        /// <summary>
        /// Removes a generic from the database
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="Type"></param>
        void RemoveGeneric(UUID AgentID, string Type);
	}
}
