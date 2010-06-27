using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Text;
using Microsoft.CSharp;
//using Microsoft.JScript;
using Microsoft.VisualBasic;
using log4net;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenMetaverse;
using Mono.Addins;

namespace OpenSim.Region.ScriptEngine.Shared.CodeTools.Converters
{
    [Extension(Path = "/OpenSim/ScriptConverter", NodeName = "ScriptConverter")]
    public class CSConverter : IScriptConverter
    {
        private CSharpCodeProvider CScodeProvider = new CSharpCodeProvider();
        Compiler m_compiler;
        public void Initialise(Compiler compiler)
        {
            m_compiler = compiler;
        }

        public void Convert(string Script, out string CompiledScript, out string[] Warnings, out Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> PositionMap)
        {
            Warnings = new List<string>().ToArray();
            CompiledScript = CreateCompilerScript(Script);
            PositionMap = null;
        }

        public string Name
        {
            get { return "cs"; }
        }
        public void Dispose()
        {
        }
        private string CreateCompilerScript(string compileScript)
        {
            compileScript = compileScript.Replace("string", 
                    "OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLString");

            compileScript = compileScript.Replace("integer", 
                    "OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLInteger");

            compileScript = compileScript.Replace("float", 
                    "OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLFloat");

            compileScript = compileScript.Replace("list",
                    "OpenSim.Region.ScriptEngine.Shared.LSL_Types.list");

            compileScript = compileScript.Replace("rotation",
                    "OpenSim.Region.ScriptEngine.Shared.LSL_Types.Quaternion");

            compileScript = compileScript.Replace("vector",
                    "OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3");
            string compiledScript = "";
            compiledScript = String.Empty +
                "using OpenSim.Region.ScriptEngine.Shared;\n" +
                "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Collections;\n" +
                "namespace Script\n" +
                "{\n";

            compiledScript += "public class ScriptClass : OpenSim.Region.ScriptEngine.Shared.ScriptBase.ScriptBaseClass, IDisposable\n";
            compiledScript += "{\n";
            compiledScript +=
                     compileScript;

            compiledScript += "\n}"; // Close Class

            compiledScript += "\n}"; // Close Namespace

            return compiledScript;
        }

        public CompilerResults Compile(CompilerParameters parameters, string Script)
        {
            bool complete = false;
            bool retried = false;
            CompilerResults results;
            do
            {
                lock (CScodeProvider)
                {
                    results = CScodeProvider.CompileAssemblyFromSource(
                        parameters, Script);
                }
                // Deal with an occasional segv in the compiler.
                // Rarely, if ever, occurs twice in succession.
                // Line # == 0 and no file name are indications that
                // this is a native stack trace rather than a normal
                // error log.
                if (results.Errors.Count > 0)
                {
                    if (!retried && (results.Errors[0].FileName == null || results.Errors[0].FileName == String.Empty) &&
                        results.Errors[0].Line == 0)
                    {
                        // System.Console.WriteLine("retrying failed compilation");
                        retried = true;
                    }
                    else
                    {
                        complete = true;
                    }
                }
                else
                {
                    complete = true;
                }
            } while (!complete);
            return results;
        }
    }
}
