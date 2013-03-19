/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using Nini.Config;

namespace Aurora.Framework
{
    /// <summary>
    ///     Loads regions from all IRegionLoaderPlugins and returns them to the SceneManager (normally)
    /// </summary>
    public interface IRegionLoader
    {
        /// <summary>
        ///     Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        ///     This determines whether this plugin will be loaded
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        ///     This determines whether this plugin will be used for dealing with creating regions and other things
        /// </summary>
        bool Default { get; }

        /// <summary>
        ///     Starts up the module and loads configs
        /// </summary>
        /// <param name="configSource"></param>
        /// <param name="openSim"></param>
        void Initialise(IConfigSource configSource, ISimulationBase openSim);

        /// <summary>
        ///     Loads the region from all enabled plugins
        /// </summary>
        /// <returns>All regionInfos loaded</returns>
        RegionInfo LoadRegion();

        /// <summary>
        ///     This updates a Regions info given by the param 'oldName' to the new region info given
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="regionInfo"></param>
        void UpdateRegionInfo(string oldName, RegionInfo regionInfo);

        /// <summary>
        ///     Delete the given region from the loader
        /// </summary>
        /// <param name="regionInfo"></param>
        void DeleteRegion(RegionInfo regionInfo);

        /// <summary>
        ///     The region loader failed to start this loader's regions, deal with the side effects
        /// </summary>
        /// <returns></returns>
        bool FailedToStartRegions(string reason);

        /// <summary>
        ///     Create a new region from the user's input
        /// </summary>
        void CreateRegion();
    }

    public interface ISceneLoader
    {
        /// <summary>
        ///     Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        ///     Create a basic IScene reference with the given RegionInfo
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <returns></returns>
        IScene CreateScene(RegionInfo regionInfo);
    }
}