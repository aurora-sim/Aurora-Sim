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

namespace OpenSim.Region.ScriptEngine.Shared.CodeTools
{
    [Extension(Path = "/OpenSim/ScriptConverter", NodeName = "ScriptConverter")]
    public class VBConverter : IScriptConverter
    {
        private VBCodeProvider VBcodeProvider = new VBCodeProvider();
        Compiler m_compiler;
        public void Initialise(Compiler compiler)
        {
            m_compiler = compiler;
        }

        public void Convert(string Script, out string CompiledScript, out string[] Warnings)
        {
            Warnings = new List<string>().ToArray();
            CompiledScript = CreateCompilerScript(Script);
        }

        public string Name
        {
            get { return "vb"; }
        }
        public void Dispose()
        {
        }
        private static string CreateCompilerScript(string compileScript)
        {
            compileScript = String.Empty +
            "Imports OpenSim.Region.ScriptEngine.Shared: Imports System.Collections.Generic: " +
            String.Empty + "NameSpace SecondLife:" +
            String.Empty + "Public Class Script: Inherits OpenSim.Region.ScriptEngine.Shared.ScriptBase.ScriptBaseClass: " +
            "\r\nPublic Sub New()\r\nEnd Sub: " +
            compileScript +
            ":End Class :End Namespace\r\n";
            return compileScript;
        }

        public CompilerResults Compile(CompilerParameters parameters, string Script)
        {
            bool complete = false;
            bool retried = false;
            CompilerResults results;
            do
            {
                lock (VBcodeProvider)
                {
                    results = VBcodeProvider.CompileAssemblyFromSource(
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
