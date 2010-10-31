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
using OpenMetaverse;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools
{
    public class YPConverter : IScriptConverter
    {
        private CSharpCodeProvider YPcodeProvider = new CSharpCodeProvider(); // YP is translated into CSharp
        private YP2CSConverter YP_Converter = new YP2CSConverter();
        Compiler m_compiler;
        public void Initialise(Compiler compiler)
        {
            m_compiler = compiler;
        }

        public void Convert(string Script, out string CompiledScript, out string[] Warnings, out Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> PositionMap)
        {
            Warnings = new List<string>().ToArray();
            CompiledScript = YP_Converter.Convert(Script);
            CompiledScript = CreateCompilerScript(Script);
            PositionMap = null;
        }

        public string Name
        {
            get { return "yp"; }
        }
        public void Dispose()
        {
        }
        private string CreateCompilerScript(string compileScript)
        {
            compileScript = String.Empty +
                   "using OpenSim.Region.ScriptEngine.Shared.YieldProlog; " +
                    "using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime; using Aurora.ScriptEngine.AuroraDotNetEngine; using System.Collections.Generic;\r\n" +
                    String.Empty + "namespace Script { " +
                    String.Empty + "public class ScriptClass : Aurora.ScriptEngine.AuroraDotNetEngine.Runtime.ScriptBaseClass  { \r\n" +
                //@"public Script() { } " +
                    @"static OpenSim.Region.ScriptEngine.Shared.YieldProlog.YP YP=null; " +
                    @"public Script() {  YP= new OpenSim.Region.ScriptEngine.Shared.YieldProlog.YP(); } " +

                    compileScript +
                    "} }\r\n";
            return compileScript;
        }

        public CompilerResults Compile(CompilerParameters parameters, string Script)
        {
            bool complete = false;
            bool retried = false;
            CompilerResults results;
            do
            {
                lock (YPcodeProvider)
                {
                    results = YPcodeProvider.CompileAssemblyFromSource(
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
