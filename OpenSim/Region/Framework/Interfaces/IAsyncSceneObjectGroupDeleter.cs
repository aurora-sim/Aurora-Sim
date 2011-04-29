using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IAsyncSceneObjectGroupDeleter
    {
        /// <summary>
        /// Deletes the given groups to the given user's inventory in the given folderID
        /// </summary>
        /// <param name="action">The reason these objects are being sent to inventory</param>
        /// <param name="folderID">The folder the objects will be added into, if you want the default folder, set this to UUID.Zero</param>
        /// <param name="objectGroups">The groups to send to inventory</param>
        /// <param name="AgentId">The agent who is deleting the given groups (not the owner of the objects necessarily)</param>
        /// <param name="permissionToDelete">If true, the objects will be deleted from the sim as well</param>
 	/// <param name="permissionToTake">If true, the objects will be added to the user's inventory as well</param>
 	void DeleteToInventory(DeRezAction action, UUID folderID,
                List<ISceneEntity> objectGroups, UUID AgentId,
                bool permissionToDelete, bool permissionToTake);
    }
}
