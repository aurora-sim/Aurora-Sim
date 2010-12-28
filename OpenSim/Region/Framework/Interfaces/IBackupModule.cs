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
        /// <summary>
        /// Are we currently loading prims?
        /// </summary>
        bool LoadingPrims { get; set; }

        /// <summary>
        /// Add a backup taint to the prim
        /// </summary>
        /// <param name="sceneObjectGroup"></param>
        void AddPrimBackupTaint(EntityBase sceneObjectGroup);

        /// <summary>
        /// This is the new backup processor, it only deals with prims that 
        /// have been 'tainted' so that it does not waste time
        /// running through as large of a backup loop
        /// </summary>
        void ProcessPrimBackupTaints(bool forced, bool backupAll);

        /// <summary>
        /// Queue the prim to be deleted from the simulation service
        /// </summary>
        /// <param name="uuid"></param>
        void DeleteFromStorage(UUID uUID);

        void DeleteAllSceneObjects();
    }
}
