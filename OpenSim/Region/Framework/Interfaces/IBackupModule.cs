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
