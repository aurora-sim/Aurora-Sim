/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.CodeDom.Compiler;
using System.Collections;
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

namespace OpenSim.Region.ScriptEngine.Shared.CodeTools
{
    public class Compiler : ICompiler
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // * Uses "LSL2Converter" to convert LSL to C# if necessary.
        // * Compiles C#-code into an assembly
        // * Returns assembly name ready for AppDomain load.
        //
        // Assembly is compiled using LSL_BaseClass as base. Look at debug C# code file created when LSL script is compiled for full details.
        //

        /// <summary>
        /// This contains number of lines WE use for header when compiling script. User will get error in line x-LinesToRemoveOnError when error occurs.
        /// </summary>
        public static int LinesToRemoveOnError = 9;

        private string DefaultCompileLanguage;
        private bool WriteScriptSourceToDebugFile;
        private bool CompileWithDebugInformation;
        public Dictionary<string, IScriptConverter> AllowedCompilers = new Dictionary<string, IScriptConverter>(StringComparer.CurrentCultureIgnoreCase);
        List<IScriptConverter> converters = new List<IScriptConverter>();
        private Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> PositionMap;

        public bool firstStartup = true;
        private string FilePrefix;
        private string ScriptEnginesPath = "ScriptEngines";

        private List<string> m_warnings = new List<string>();

        private static UInt64 scriptCompileCounter = 0;                                     // And a counter

        public IScriptEngine m_scriptEngine;

        #endregion

        #region Setup

        public Compiler(IScriptEngine scriptEngine)
        {
            m_scriptEngine = scriptEngine;
            ReadConfig();
        }

        public void ReadConfig()
        {
            // Get some config
            WriteScriptSourceToDebugFile = m_scriptEngine.Config.GetBoolean("WriteScriptSourceToDebugFile", false);
            CompileWithDebugInformation = m_scriptEngine.Config.GetBoolean("CompileWithDebugInformation", true);
            
            MakeFilePrefixSafe();
            //Set up the compilers
            MapCompilers();
            //Find the default compiler
            FindDefaultCompiler();
        }

        public void MakeFilePrefixSafe()
        {
            // Get file prefix from scriptengine name and make it file system safe:
            FilePrefix = "CommonCompiler";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                FilePrefix = FilePrefix.Replace(c, '_');
            }
        }

        public void FindDefaultCompiler()
        {
            // Allowed compilers
            string allowComp = m_scriptEngine.Config.GetString("AllowedCompilers", "lsl");
            AllowedCompilers.Clear();

            // Default language
            DefaultCompileLanguage = m_scriptEngine.Config.GetString("DefaultCompileLanguage", "lsl").ToLower();

            bool found = false;
            foreach (string strl in allowComp.Split(','))
            {
                string strlan = strl.Trim(" \t".ToCharArray()).ToLower();
                foreach (IScriptConverter converter in converters)
                {
                    if (converter.Name == strlan)
                    {
                        AllowedCompilers.Add(strlan, converter);
                        if (converter.Name == DefaultCompileLanguage)
                        {
                            found = true;
                        }
                    }
                }
            }
            if (AllowedCompilers.Count == 0)
                m_log.Error("[Compiler]: Config error. Compiler could not recognize any language in \"AllowedCompilers\". Scripts will not be executed!");
            
            if (!found)
                m_log.Error("[Compiler]: " +
                                            "Config error. Default language \"" + DefaultCompileLanguage + "\" specified in \"DefaultCompileLanguage\" is not recognized as a valid language. Changing default to: \"lsl\".");

            // Is this language in allow-list?
            if (!AllowedCompilers.ContainsKey(DefaultCompileLanguage))
            {
                m_log.Error("[Compiler]: " +
                            "Config error. Default language \"" + DefaultCompileLanguage + "\"specified in \"DefaultCompileLanguage\" is not in list of \"AllowedCompilers\". Scripts may not be executed!");
            }

            // We now have an allow-list, a mapping list, and a default language
        }
        public class ScriptConverterInitialiser : OpenSim.Framework.PluginInitialiserBase
        {
            Compiler m_compiler;
            public ScriptConverterInitialiser(Compiler compiler)
            {
                m_compiler = compiler;
            }

            public override void Initialise(OpenSim.Framework.IPlugin plugin)
            {
                IScriptConverter convert = (IScriptConverter)plugin;
                convert.Initialise(m_compiler);
            }
        }

        public void MapCompilers()
        {
            converters = Aurora.Framework.AuroraModuleLoader.LoadPlugins<IScriptConverter>("/OpenSim/ScriptConverter", new ScriptConverterInitialiser(this));
        }

        #endregion

        #region Compile script

        /// <summary>
        /// Converts script from LSL to CS and calls CompileFromCSText
        /// </summary>
        /// <param name="Script">LSL script</param>
        /// <returns>Filename to .dll assembly</returns>
        public void PerformScriptCompile(string Script, UUID itemID, UUID ownerUUID, out string assembly)
        {
            if (Script == String.Empty)
            {
                throw new Exception("No script text present");
            }
        	
            m_warnings.Clear();

            assembly = CheckDirectories(Path.Combine("ScriptEngines", Path.Combine(
                        "Script",
                        FilePrefix + "_compiled_" + itemID.ToString() + ".dll")));

            IScriptConverter converter;
            string compileScript;

            CheckLanguageAndConvert(Script, ownerUUID, out converter, out compileScript);

            CompileFromDotNetText(compileScript, converter, assembly);
        }

        private void CheckLanguageAndConvert(string Script,UUID ownerID, out IScriptConverter converter, out string compileScript)
        {
            string language = DefaultCompileLanguage;

            foreach (IScriptConverter convert in converters)
            {
                if (Script.StartsWith("//" + convert.Name, true, CultureInfo.InvariantCulture))
                    language = convert.Name;
            }

            if (!AllowedCompilers.ContainsKey(language))
            {
                // Not allowed to compile to this language!
                string errtext = String.Empty;
                errtext += "The compiler for language \"" + language.ToString() + "\" is not in list of allowed compilers. Script will not be executed!";
                throw new Exception(errtext);
            }

            if (((OpenSim.Region.Framework.Scenes.Scene)m_scriptEngine.Worlds[0]).Permissions.CanCompileScript(ownerID, language) == false)
            {
                // Not allowed to compile to this language!
                string errtext = String.Empty;
                errtext += ownerID + " is not in list of allowed users for this scripting language. Script will not be executed!";
                throw new Exception(errtext);
            }

            compileScript = Script;

            string[] Warnings;
            AllowedCompilers.TryGetValue(language, out converter);

            converter.Convert(Script, out compileScript, out Warnings, out PositionMap);

            // copy converter warnings into our warnings.
            foreach (string warning in Warnings)
            {
                AddWarning(warning);
            }
        }

        private string CheckDirectories(string assembly)
        {
            if (!Directory.Exists(ScriptEnginesPath))
            {
                try
                {
                    Directory.CreateDirectory(ScriptEnginesPath);
                }
                catch (Exception)
                {
                }
            }

            string[] testDir = assembly.Split('\\');

            if (!Directory.Exists(testDir[0] + "\\" + testDir[1]))
            {
                try
                {
                    Directory.CreateDirectory(testDir[0] + "\\" + testDir[1]);
                }
                catch (Exception)
                {
                }
            }
            assembly = CheckAssembly(assembly, 0);
            return assembly;
        }

        private string CheckAssembly(string assembly, int i)
        {
            if (File.Exists(assembly) || File.Exists(assembly.Remove(assembly.Length - 4) + ".pdb"))
            {
                try
                {
                    File.Delete(assembly);
                    File.Delete(assembly.Remove(assembly.Length - 4) + ".pdb");
                }
                catch (Exception e) // NOTLEGIT - Should be just FileIOException
                {
                    assembly = assembly.Remove(assembly.Length - 4);
                    assembly += "A.dll";
                    i++;
                    return CheckAssembly(assembly, i);
                }
            }
            return assembly;
        }

        public string[] GetWarnings()
        {
            return m_warnings.ToArray();
        }

        private void AddWarning(string warning)
        {
            if (!m_warnings.Contains(warning))
            {
                m_warnings.Add(warning);
            }
        }

        /// <summary>
        /// Compile .NET script to .Net assembly (.dll)
        /// </summary>
        /// <param name="Script">CS script</param>
        /// <returns>Filename to .dll assembly</returns>
        internal void CompileFromDotNetText(string Script, IScriptConverter converter, string assembly)
        {
            string ext = "." + converter.Name;

            // Output assembly name
            scriptCompileCounter++;
            try
            {
                File.Delete(assembly);
            }
            catch (Exception e) // NOTLEGIT - Should be just FileIOException
            {
                throw new Exception("Unable to delete old existing " +
                        "script-file before writing new. Compile aborted: " +
                        e.ToString());
            }

            // DEBUG - write source to disk
            if (WriteScriptSourceToDebugFile)
            {
                string srcFileName = FilePrefix + "_source_" +
                        Path.GetFileNameWithoutExtension(assembly) + ext;
                try
                {
                    File.WriteAllText(Path.Combine(Path.Combine(
                        ScriptEnginesPath,
                        "Script"),
                        srcFileName), Script);
                }
                catch (Exception ex) //NOTLEGIT - Should be just FileIOException
                {
                    m_log.Error("[Compiler]: Exception while " +
                                "trying to write script source to file \"" +
                                srcFileName + "\": " + ex.ToString());
                }
            }

            // Do actual compile
            CompilerParameters parameters = new CompilerParameters();

            parameters.IncludeDebugInformation = true;

            string rootPath =
                Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

            parameters.ReferencedAssemblies.Add(Path.Combine(rootPath,
                    "OpenSim.Region.ScriptEngine.Shared.dll"));
            parameters.ReferencedAssemblies.Add(Path.Combine(rootPath,
                    "OpenSim.Region.ScriptEngine.Shared.Api.Runtime.dll"));
            parameters.ReferencedAssemblies.Add("System.dll");

            if (converter.Name == "yp")
            {
                parameters.ReferencedAssemblies.Add(Path.Combine(rootPath,
                        "OpenSim.Region.ScriptEngine.Shared.YieldProlog.dll"));
            }

            parameters.GenerateExecutable = false;
            parameters.OutputAssembly = assembly;
            parameters.IncludeDebugInformation = CompileWithDebugInformation;
            //parameters.WarningLevel = 1; // Should be 4?
            parameters.TreatWarningsAsErrors = false;

            CompilerResults results = converter.Compile(parameters, Script);

            // Check result
            // Go through errors

            //
            // WARNINGS AND ERRORS
            //
            bool hadErrors = false;
            string errtext = String.Empty;

            if (results.Errors.Count > 0)
            {
                foreach (CompilerError CompErr in results.Errors)
                {
                    string severity = CompErr.IsWarning ? "Warning" : "Error";
                
                    KeyValuePair<int, int> lslPos;

                    // Show 5 errors max, but check entire list for errors

                    if (severity == "Error")
                    {
                        string text = CompErr.ErrorText;
                        int LineN = 0;
                        int CharN = 0;
                        // Use LSL type names
                        if (converter.Name == "lsl")
                        {
                            text = ReplaceTypes(CompErr.ErrorText);
                            text = CleanError(text);
                            lslPos = FindErrorPosition(CompErr.Line, CompErr.Column, PositionMap);
                            LineN = lslPos.Key - 1;
                            CharN = lslPos.Value - 1;
                        }
                        else
                        {
                            LineN = CompErr.Line;
                            CharN = CompErr.Column;
                        }

                        // The Second Life viewer's script editor begins
                        // countingn lines and columns at 0, so we subtract 1.
                        errtext += String.Format("({0},{1}): {3}: {2}\n",
                                LineN, CharN, text, severity);
                        hadErrors = true;
                    }
                }
            }

            if (hadErrors)
            {
                throw new Exception(errtext);
            }

            //  On today's highly asynchronous systems, the result of
            //  the compile may not be immediately apparent. Wait a 
            //  reasonable amount of time before giving up on it.

            if (!File.Exists(assembly))
            {
                for (int i = 0; i < 50 && !File.Exists(assembly); i++)
                {
                    System.Threading.Thread.Sleep(100);
                }
                // One final chance...
                if (!File.Exists(assembly))
                {
                    errtext = String.Empty;
                    errtext += "No compile error. But not able to locate compiled file.";
                    throw new Exception(errtext);
                }
            }
        }

        private class kvpSorter : IComparer<KeyValuePair<int, int>>
        {
            public int Compare(KeyValuePair<int, int> a,
                    KeyValuePair<int, int> b)
            {
                return a.Key.CompareTo(b.Key);
            }
        }

        public static KeyValuePair<int, int> FindErrorPosition(int line,
                int col, Dictionary<KeyValuePair<int, int>,
                KeyValuePair<int, int>> positionMap)
        {
            if (positionMap == null || positionMap.Count == 0)
                return new KeyValuePair<int, int>(line, col);

            KeyValuePair<int, int> ret = new KeyValuePair<int, int>();
            line -= LinesToRemoveOnError;

            if (positionMap.TryGetValue(new KeyValuePair<int, int>(line, col),
                    out ret))
                return ret;

            List<KeyValuePair<int, int>> sorted =
                    new List<KeyValuePair<int, int>>(positionMap.Keys);

            sorted.Sort(new kvpSorter());

            int l = 1;
            int c = 1;

            foreach (KeyValuePair<int, int> cspos in sorted)
            {
                if (cspos.Key >= line)
                {
                    if (cspos.Key > line)
                        return new KeyValuePair<int, int>(l, c);
                    if (cspos.Value > col)
                        return new KeyValuePair<int, int>(l, c);
                    c = cspos.Value;
                    if (c == 0)
                        c++;
                }
                else
                {
                    l = cspos.Key;
                }
            }
            return new KeyValuePair<int, int>(l, c);
        }

        string ReplaceTypes(string message)
        {
            message = message.Replace(
                    "OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLString",
                    "string");

            message = message.Replace(
                    "OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLInteger",
                    "integer");

            message = message.Replace(
                    "OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLFloat",
                    "float");

            message = message.Replace(
                    "OpenSim.Region.ScriptEngine.Shared.LSL_Types.list",
                    "list");

            return message;
        }

        string CleanError(string message)
        {
            //Remove these long strings
            message = message.Replace(
                    "OpenSim.Region.ScriptEngine.Shared.ScriptBase.ScriptBaseClass",
                    "");
            if (message.Contains("The best overloaded method match for"))
            {
                string[] messageSplit = message.Split('\'');
                string Function = messageSplit[1];
                Function = Function.Remove(0, 1);
                string[] FunctionSplit = Function.Split('(');
                string FunctionName = FunctionSplit[0];
                string Arguments = FunctionSplit[1].Split(')')[0];
                message = "Incorrect argument in " + FunctionName + ", arguments should be " + Arguments + "\n";
            }
            if (message == "Unexpected EOF")
            {
                message = "Missing one or more }." + "\n";
            }
            return message;
        }
        #endregion
    }
}
