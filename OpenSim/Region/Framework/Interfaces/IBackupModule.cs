using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IBackupModule
    {
        void AddPrimBackupTaint(EntityBase sceneObjectGroup);
        void ProcessPrimBackupTaints(bool forced, bool backupAll);
        void DeleteFromStorage(UUID uUID);
    }
}
