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
    public class LSLConverter : IScriptConverter
    {
        private CSharpCodeProvider CScodeProvider = new CSharpCodeProvider();
        Compiler m_compiler;
        public void Initialise(Compiler compiler)
        {
            m_compiler = compiler;
        }

        public void Convert(string Script, out string CompiledScript, out string[] Warnings)
        {
            // Its LSL, convert it to C#
            ICodeConverter LSL_Converter = (ICodeConverter)new CSCodeGenerator();
            CompiledScript = LSL_Converter.Convert(Script);
            Warnings = LSL_Converter.GetWarnings();
            CompiledScript = CreateCompilerScript(CompiledScript);
        }

        public string Name
        {
            get { return "lsl"; }
        }
        public void Dispose()
        {
        }
        private string CreateCompilerScript(string compileScript)
        {
            string compiledScript = "";
            compiledScript = String.Empty +
                "using OpenSim.Region.ScriptEngine.Shared;\n" +
                "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Collections;\n" +
                "using System.Reflection;\n" +
                "using System.Timers;\n" +
                "namespace Script\n" +
                "{\n";

            compiledScript += "[Serializable]\n public class ScriptClass : OpenSim.Region.ScriptEngine.Shared.ScriptBase.ScriptBaseClass, IDisposable, OpenSim.Region.ScriptEngine.Shared.ScriptBase.IRemoteInterface\n";
            compiledScript += "{\n";
            compiledScript +=
                     compileScript;


            compiledScript += "List<IEnumerator> parts = new List<IEnumerator>();\n";
            compiledScript += "System.Timers.Timer aTimer = new System.Timers.Timer(250);\n";
            compiledScript += "public ScriptClass()\n{\n";
            compiledScript += "aTimer.Elapsed += new System.Timers.ElapsedEventHandler(Timer);\n";
            compiledScript += "aTimer.Enabled = true;\n";
            compiledScript += "aTimer.Start();\n";
            compiledScript += "}\n";
            compiledScript += "~ScriptClass()\n{\n";
            compiledScript += "aTimer.Stop();\n";
            compiledScript += "aTimer.Dispose();\n";
            compiledScript += "}\n";
            compiledScript += "public void Dispose()\n";
            compiledScript += "{\n";
            compiledScript += "aTimer.Stop();\n";
            compiledScript += "aTimer.Dispose();\n";
            compiledScript += "}\n";

            compiledScript += "public void Timer(object source, System.Timers.ElapsedEventArgs e)\n{\n";
            compiledScript += "lock (parts)\n";
            compiledScript += "{\n";
            compiledScript += "int i = 0;\n";
            compiledScript += "if(parts.Count == 0)\n";
            compiledScript += "return;";
            compiledScript += "while (parts.Count > 0 && i < 1000)\n";
            compiledScript += "{\n";
            compiledScript += "i++;\n";
            compiledScript += "bool running = false;\n";
            compiledScript += "try\n";
            compiledScript += "{\n";
            compiledScript += "running = parts[i % parts.Count].MoveNext();\n";
            compiledScript += "}\n";
            compiledScript += "catch (Exception ex)\n";
            compiledScript += "{\n";
            compiledScript += "}\n";
            compiledScript += "if (!running)\n { \n";
            compiledScript += "parts.Remove(parts[i % parts.Count]);\n } \n";
            compiledScript += "else\n { } \n";
            compiledScript += "}\n";
            compiledScript += "}\n";
            compiledScript += "}\n";

            compiledScript += "public object Invoke(string lcMethod,object[] Parameters)\n {\n";
            compiledScript += "return this.GetType().InvokeMember(lcMethod, BindingFlags.InvokeMethod,null,this,Parameters);\n";
            compiledScript += "}\n";
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
