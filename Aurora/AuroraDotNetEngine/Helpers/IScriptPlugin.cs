/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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

using Aurora.Framework;
using Aurora.Framework.SceneInfo;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    /// <summary>
    ///     These plugins provide for the ability to hook up onto scripts for events and are called each iteration by the script engine event thread
    /// </summary>
    public interface IScriptPlugin
    {
        /// <summary>
        ///     Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        ///     Whether or not the plugin should remove itself from scripts on state changes
        /// </summary>
        bool RemoveOnStateChange { get; }

        /// <summary>
        ///     Start the plugin
        /// </summary>
        /// <param name="engine"></param>
        void Initialize(ScriptEngine engine);

        /// <summary>
        ///     Add the given region to the plugin that it will be serving
        /// </summary>
        /// <param name="scene"></param>
        void AddRegion(IScene scene);

        /// <summary>
        ///     Check the module and all scripts that it may be being used by
        ///     This is called every iteration by the event thread
        /// </summary>
        /// <returns></returns>
        bool Check();

        /// <summary>
        ///     Serialize any data that we may have for the given script
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="primID"></param>
        /// <returns></returns>
        OSD GetSerializationData(UUID itemID, UUID primID);

        /// <summary>
        ///     Create from the information that we serialized earlier the info that we have for the given script and add it back to the script
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="objectID"></param>
        /// <param name="data"></param>
        void CreateFromData(UUID itemID, UUID objectID, OSD data);

        /// <summary>
        ///     Remove the given script from the plugin
        /// </summary>
        /// <param name="primID"></param>
        /// <param name="itemID"></param>
        void RemoveScript(UUID primID, UUID itemID);
    }
}