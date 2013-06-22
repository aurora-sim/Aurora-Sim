using System.Collections.Generic;
using Nini.Config;

namespace Aurora.Framework.SceneInfo
{

    #region Delegates

    public delegate void NewScene(IScene scene);

    public delegate void NoParam();

    #endregion

    public interface ISceneManager
    {
        /// <summary>
        ///     Starts the region
        /// </summary>
        /// <param name="newRegion"></param>
        void StartRegions(out bool newRegion);

        /// <summary>
        ///     Shuts down the given region
        /// </summary>
        /// <param name="shutdownType"></param>
        /// <param name="p"></param>
        void CloseRegion(IScene scene, ShutdownType shutdownType, int p);

        /// <summary>
        ///     Removes and resets terrain and objects from the database
        /// </summary>
        void ResetRegion(IScene scene);

        /// <summary>
        ///     Restart the given region
        /// </summary>
        void RestartRegion(IScene scene);

        void HandleStartupComplete(List<string> data);

        IConfigSource ConfigSource { get; }

        List<IScene> Scenes { get; }

        event NewScene OnCloseScene;
        event NewScene OnAddedScene;
        event NewScene OnFinishedAddingScene;
    }
}