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
using OpenMetaverse;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public interface IScriptApi
    {
        //
        // Each API has an identifier, which is used to load the
        // proper runtime assembly at load time.
        //

        /// <summary>
        ///   Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        ///   The name of the interface that is used to implement the functions
        /// </summary>
        string InterfaceName { get; }

        /// <summary>
        ///   Any assemblies that may need referenced to implement your Api.
        ///   If you are adding an Api, you will need to have the path to your assembly in this 
        ///   (along with any other assemblies you may need). You can use this code to add the current assembly 
        ///   to this list:
        ///   "this.GetType().Assembly.Location"
        ///   as shown in the Bot_API.cs in Aurora.BotManager.
        /// </summary>
        string[] ReferencedAssemblies { get; }

        /// <summary>
        ///   If you do not use the standard namespaces for your API module, you will need to add them here 
        ///   As shown in the Bot_API.cs in Aurora.BotManager.
        /// </summary>
        string[] NamespaceAdditions { get; }

        void Initialize(IScriptModulePlugin engine, ISceneChildEntity part, uint localID, UUID item,
                        ScriptProtectionModule module);

        /// <summary>
        ///   Make a copy of the api so that it can be used again
        /// </summary>
        /// <returns></returns>
        IScriptApi Copy();
    }
}