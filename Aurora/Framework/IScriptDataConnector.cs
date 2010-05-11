using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.Framework
{
	public interface IScriptDataConnector
	{
		StateSave GetStateSave(UUID itemID, UUID UserInventoryItemID);
		void SaveStateSave(StateSave state);
        void DeleteStateSave(UUID ItemID);
        void DeleteStateSave(string AssemblyName);
	}
}
