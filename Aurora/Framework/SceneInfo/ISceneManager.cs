using System.Collections.Generic;
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
        void CloseRegion(ShutdownType shutdownType, int p);

        /// <summary>
        /// Removes and resets terrain and objects from the database
        /// </summary>
        /// <param name="scene"></param>
        void ResetRegion();

        /// <summary>
        /// Restart the given region
        /// </summary>
        /// <param name="m_scene"></param>
        void RestartRegion();

        void HandleStartupComplete(List<string> data);

        ISimulationDataStore GetSimulationDataStore();

        IConfigSource ConfigSource { get; }

        void RemoveRegion(bool cleanup);

        IScene Scene { get; }

        event NewScene OnCloseScene;
        event NewScene OnAddedScene;
    }
}
