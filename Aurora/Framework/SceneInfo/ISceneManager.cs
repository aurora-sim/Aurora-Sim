using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using Nini.Config;

namespace Aurora.Framework
{
    #region Delegates

    public delegate void NewScene(IScene scene);
    public delegate void NoParam();

    #endregion

    public interface ISceneManager
    {
        /// <summary>
        /// Attempts to find a running region
        /// </summary>
        bool TryGetScene(string regionName, out IScene scene);
        bool TryGetScene(int LocX, int LocY, out IScene scene);
        bool TryGetScene(UUID regionID, out IScene scene);

        /// <summary>
        /// The number of regions in the instance
        /// </summary>
        int AllRegions { get; set; }

        /// <summary>
        /// Starts a region
        /// </summary>
        /// <param name="region"></param>
        IScene StartNewRegion(RegionInfo region);

        /// <summary>
        /// Shuts down the given region
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="shutdownType"></param>
        /// <param name="p"></param>
        void CloseRegion(IScene scene, ShutdownType shutdownType, int p);

        /// <summary>
        /// Removes and resets terrain and objects from the database
        /// </summary>
        /// <param name="scene"></param>
        void ResetRegion(IScene scene);

        /// <summary>
        /// Deletes a region's objects from the object database
        /// </summary>
        /// <param name="regionID"></param>
        void DeleteRegion(UUID regionID);

        /// <summary>
        /// Get all currently running scenes
        /// </summary>
        /// <returns></returns>
        List<IScene> GetAllScenes();

        /// <summary>
        /// Restart the given region
        /// </summary>
        /// <param name="m_scene"></param>
        void RestartRegion(IScene scene);

        IScene GetCurrentOrFirstScene();

        void HandleStartupComplete(IScene scene, List<string> data);

        ISimulationDataStore GetNewSimulationDataStore();

        IConfigSource ConfigSource { get; }

        void RemoveRegion(IScene scene, bool cleanup);

        bool ChangeConsoleRegion(string regionName);

        event NewScene OnCloseScene;
        event NewScene OnAddedScene;
        
        void ForEachCurrentScene(Action<IScene> func);

        void ForEachScene(Action<IScene> func);
    }
}
