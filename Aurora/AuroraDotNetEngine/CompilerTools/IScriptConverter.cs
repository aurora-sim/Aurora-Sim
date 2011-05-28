using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools
{
    public interface IScriptConverter
    {
        /// <summary>
        /// Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        /// Starts the converter module and gives the ref to the Compiler itself
        /// </summary>
        /// <param name="compiler"></param>
        void Initialise(Compiler compiler);
        
        /// <summary>
        /// Convert the given script
        /// </summary>
        /// <param name="Script">The script to convert</param>
        /// <param name="CompiledScript">The converted script</param>
        /// <param name="PositionMap">LSL only</param>
        void Convert(string Script, out string CompiledScript, out Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> PositionMap);
        
        /// <summary>
        /// Compile the given script that was converted with this module
        /// </summary>
        /// <param name="parameters">The parameters that have been set by the Compiler</param>
        /// <param name="isFilePath">Whether the script should be compiled from a file or in memory</param>
        /// <param name="Script">The converted script to compile</param>
        /// <returns></returns>
        CompilerResults Compile(CompilerParameters parameters, bool isFilePath, string Script);

        /// <summary>
        /// The default state for this script
        /// LSL is 'default', all others are ""
        /// </summary>
        string DefaultState { get; }

        /// <summary>
        /// Provides for any functionality that needs to occur after the APIs have been set up and the script has been compiled
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="data"></param>
        /// <param name="Script"></param>
        void FinishCompile (IScriptModulePlugin plugin, ScriptData data, IScript Script);
    }
}
