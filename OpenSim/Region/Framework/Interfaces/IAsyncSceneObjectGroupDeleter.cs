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
        void DeleteToInventory(DeRezAction action, UUID folderID,
                List<ISceneEntity> objectGroups, UUID AgentId,
                bool permissionToDelete, bool permissionToTake);
    }
}
