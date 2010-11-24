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
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools
{
    public class LSLConverter : IScriptConverter
    {
        private CSharpCodeProvider CScodeProvider = new CSharpCodeProvider();
        private Compiler m_compiler;
        private ICSCodeGenerator LSL_Converter;

        public void Initialise(Compiler compiler)
        {
            m_compiler = compiler;
            if (m_compiler.m_XEngineLSLCompatabilityModule)
                LSL_Converter = new LegacyCSCodeGenerator();
            else
                LSL_Converter = new CSCodeGenerator(m_compiler.m_SLCompatabilityMode, null);
        }

        public void Convert(string Script, out string CompiledScript, out string[] Warnings, out Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> PositionMap)
        {
            // Its LSL, convert it to C#
            CompiledScript = LSL_Converter.Convert(Script);
            Warnings = LSL_Converter.GetWarnings();
            PositionMap = LSL_Converter.PositionMap;

            LSL_Converter.Dispose(); //Resets it for next time
        }

        public string Name
        {
            get { return "lsl"; }
        }

        public void Dispose()
        {
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
            }
            while (!complete);
            return results;
        }
    }
}
