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

using System.CodeDom.Compiler;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools
{
    /// <summary>
    ///     This allows for scripts to be converted and compiled for different languages
    /// </summary>
    public interface IScriptConverter
    {
        /// <summary>
        ///     Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        ///     The default state for this script
        ///     LSL is 'default', all others are ""
        /// </summary>
        string DefaultState { get; }

        /// <summary>
        ///     Starts the converter module and gives the ref to the Compiler itself
        /// </summary>
        /// <param name="compiler"></param>
        void Initialise(Compiler compiler);

        /// <summary>
        ///     Convert the given script
        /// </summary>
        /// <param name="Script">The script to convert</param>
        /// <param name="CompiledScript">The converted script</param>
        /// <param name="PositionMap">LSL only</param>
        void Convert(string Script, out string CompiledScript,
                     out object PositionMap);

        /// <summary>
        ///     Compile the given script that was converted with this module
        /// </summary>
        /// <param name="parameters">The parameters that have been set by the Compiler</param>
        /// <param name="isFilePath">Whether the script should be compiled from a file or in memory</param>
        /// <param name="Script">The converted script to compile</param>
        /// <returns></returns>
        CompilerResults Compile(CompilerParameters parameters, bool isFilePath, string Script);

        /// <summary>
        ///     Provides for any functionality that needs to occur after the APIs have been set up and the script has been compiled
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="data"></param>
        /// <param name="Script"></param>
        void FinishCompile(IScriptModulePlugin plugin, ScriptData data, IScript Script);

        /// <summary>
        ///     Find the line that is erroring out in the script
        /// </summary>
        /// <param name="CompErr"></param>
        /// <param name="PositionMap"></param>
        /// <param name="script"></param>
        /// <param name="LineN"></param>
        /// <param name="CharN"></param>
        void FindErrorLine(CompilerError CompErr, object PositionMap, string script, out int LineN, out int CharN);
    }
}