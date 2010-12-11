using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools
{
    public interface IScriptConverter : OpenSim.Framework.IPlugin
    {
        void Initialise(Compiler compiler);
        void Convert(string Script, out string CompiledScript, out Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> PositionMap);
        CompilerResults Compile(CompilerParameters parameters, string Script);
    }
}
