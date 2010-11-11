using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public interface IScriptDataConnector : IAuroraDataPlugin
	{
        /// <summary>
        /// Gets a script state save by user Inventory Item ID, if UUID.Zero, then loads by prim Inventory Item ID
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="UserInventoryItemID"></param>
        /// <returns></returns>
		StateSave GetStateSave(UUID itemID, UUID UserInventoryItemID);

        /// <summary>
        /// Saves a new state save for the given script
        /// </summary>
        /// <param name="state"></param>
		void SaveStateSave(StateSave state);

        /// <summary>
        /// Deletes a state save by Inventory ItemID
        /// </summary>
        /// <param name="ItemID"></param>
        void DeleteStateSave(UUID ItemID);

        /// <summary>
        /// Deletes a state save by assembly name.
        /// </summary>
        /// <param name="AssemblyName"></param>
        void DeleteStateSave(string AssemblyName);
	}
}
