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

using System;
using System.Collections;
using System.Collections.Generic;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using OpenMetaverse;

namespace Aurora.Framework.Modules
{
    public interface IScriptModule
    {
        /// <summary>
        ///     Should we be able to run currently?
        /// </summary>
        bool Disabled { get; set; }

        /// <summary>
        ///     The name of our script engine
        /// </summary>
        string ScriptEngineName { get; }

        /// <summary>
        ///     Adds an event to one script with the given parameters
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="primID"></param>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        bool PostScriptEvent(UUID itemID, UUID primID, string name, Object[] args);

        /// <summary>
        ///     Posts an event to the entire given object
        /// </summary>
        /// <param name="primID"></param>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        bool PostObjectEvent(UUID primID, string name, Object[] args);

        // Suspend ALL scripts in a given scene object. The item ID
        // is the UUID of a SOG, and the method acts on all contained
        // scripts. This is different from the suspend/resume that
        // can be issued by a client.
        //
        void SuspendScript(UUID itemID);
        void ResumeScript(UUID itemID);

        /// <summary>
        ///     Gets all script errors for the given itemID
        /// </summary>
        /// <param name="itemID"></param>
        /// <returns></returns>
        ArrayList GetScriptErrors(UUID itemID);

        /// <summary>
        ///     Updates the given script with the options given
        /// </summary>
        /// <param name="partID"></param>
        /// <param name="itemID"></param>
        /// <param name="script"></param>
        /// <param name="startParam"></param>
        /// <param name="postOnRez"></param>
        /// <param name="stateSource"></param>
        void UpdateScript(UUID partID, UUID itemID, string script, int startParam, bool postOnRez,
                          StateSource stateSource);

        /// <summary>
        ///     Stops all scripts that the ScriptEngine is running
        /// </summary>
        void StopAllScripts();

        /// <summary>
        ///     Attempt to compile a script from the given assetID and return any compile errors
        /// </summary>
        /// <param name="assetID"></param>
        /// <param name="itemID"></param>
        /// <returns></returns>
        string TestCompileScript(UUID assetID, UUID itemID);

        /// <summary>
        ///     Changes script references from an old Item/Prim to a new one
        /// </summary>
        /// <param name="olditemID"></param>
        /// <param name="newItem"></param>
        /// <param name="newPart"></param>
        void UpdateScriptToNewObject(UUID olditemID, TaskInventoryItem newItem, ISceneChildEntity newPart);

        /// <summary>
        ///     Force a state save for the given script
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="primID"></param>
        void SaveStateSave(UUID itemID, UUID primID);

        /// <summary>
        ///     Get a list of all script function names in the Apis
        /// </summary>
        /// <returns></returns>
        List<string> GetAllFunctionNames();

        /// <summary>
        ///     Get the number of active (running) scripts in the given entity
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        int GetActiveScripts(IEntity obj);

        /// <summary>
        ///     Get the number of scripts in the given entity
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        int GetTotalScripts(IEntity obj);

        /// <summary>
        ///     Get the number of active (running) scripts that this engine is controlling
        /// </summary>
        /// <returns></returns>
        int GetActiveScripts();

        /// <summary>
        ///     Get the number of events fired per second currently
        /// </summary>
        /// <returns></returns>
        int GetScriptEPS();

        /// <summary>
        ///     Get the top scripts in the Script Engine
        /// </summary>
        /// <param name="RegionID"></param>
        /// <returns></returns>
        Dictionary<uint, float> GetTopScripts(UUID RegionID);

        /// <summary>
        ///     This makes sure that the cmd handler (for long running events like timers/listeners) is running
        /// </summary>
        /// <param name="itemID">Item causing the poke</param>
        void PokeThreads(UUID itemID);

        /// <summary>
        ///     Save states for all scripts that require it
        /// </summary>
        void SaveStateSaves();

        /// <summary>
        ///     Get the script time for the given script
        /// </summary>
        /// <param name="itemID"></param>
        /// <returns></returns>
        int GetScriptTime(UUID itemID);
    }

    [Flags]
    public enum scriptEvents : long
    {
        None = 0,
        attach = 1,
        collision = 16,
        collision_end = 32,
        collision_start = 64,
        control = 128,
        dataserver = 256,
        email = 512,
        http_response = 1024,
        land_collision = 2048,
        land_collision_end = 4096,
        land_collision_start = 8192,
        at_target = 16384,
        at_rot_target = 16777216,
        listen = 32768,
        money = 65536,
        moving_end = 131072,
        moving_start = 262144,
        not_at_rot_target = 524288,
        not_at_target = 1048576,
        remote_data = 8388608,
        run_time_permissions = 268435456,
        state_entry = 1073741824,
        state_exit = 2,
        timer = 4,
        touch = 8,
        touch_end = 536870912,
        touch_start = 2097152,
        object_rez = 4194304,
        changed = 2147483648,
        link_message = 4294967296,
        no_sensor = 8589934592,
        on_rez = 17179869184,
        sensor = 34359738368,
        http_request = 68719476736
    }
}