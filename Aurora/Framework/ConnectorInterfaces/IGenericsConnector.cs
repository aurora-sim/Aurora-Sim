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
        T GetGeneric<T>(UUID OwnerID, string Type, string Key, T data) where T : IDataTransferable;
        List<T> GetGenerics<T>(UUID OwnerID, string Type, T data) where T : IDataTransferable;
        void AddGeneric(UUID AgentID, string Type, string Key, OSDMap Value);
        void RemoveGeneric(UUID AgentID, string Type, string Key);
        void RemoveGeneric(UUID AgentID, string Type);
	}
}
