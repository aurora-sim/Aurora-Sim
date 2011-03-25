using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;
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
        /// Should we load prims for this region?
        /// </summary>
        bool LoadPrims { get; set; }

        /// <summary>
        /// Should we load parcels for this region?
        /// </summary>
        bool LoadParcels { get; set; }

        /// <summary>
        /// Loads all parcels from storage (database)
        /// Sets up the parcel interfaces and modules
        /// </summary>
        void LoadAllLandObjectsFromStorage ();

        /// <summary>
        /// Loads all prims from storage (database)
        /// This is normally called during startup, but can be called later if not called during startup
        /// </summary>
        void LoadPrimsFromStorage ();

        /// <summary>
        /// Creates script instances in all objects that have scripts in them
        /// This is normally called during startup, but can be called later if not called during startup
        /// </summary>
        void CreateScriptInstances ();

        /// <summary>
        /// Add a backup taint to the prim.
        /// </summary>
        /// <param name="sceneObjectGroup"></param>
        void AddPrimBackupTaint (ISceneEntity sceneObjectGroup);

        /// <summary>
        /// This is the new backup processor, it only deals with prims that 
        /// have been 'tainted' so that it does not waste time
        /// running through as large of a backup loop.
        /// </summary>
        void ProcessPrimBackupTaints(bool forced, bool backupAll);

        /// <summary>
        /// Queue the prim to be deleted from the simulation service.
        /// </summary>
        /// <param name="uuid"></param>
        void DeleteFromStorage(UUID uuid);

        /// <summary>
        /// Remove all objects from the given region.
        /// </summary>
        void DeleteAllSceneObjects();

        /// <summary>
        /// Synchronously delete the objects from the scene.
        /// This does send kill object updates and resets the parcel prim counts.
        /// </summary>
        /// <param name="groups"></param>
        /// <param name="DeleteScripts"></param>
        /// <returns></returns>
        bool DeleteSceneObjects(ISceneEntity[] groups, bool DeleteScripts);

        /// <summary>
        /// Removes all current objects from the scene, but not from the database
        /// </summary>
        void ResetRegionToStartupDefault ();
    }
}
