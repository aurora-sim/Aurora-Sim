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

        internal enum enumCompileType
        {
            lsl = 0,
            cs = 1,
            vb = 2,
            js = 3,
            yp = 4
        }

        /// <summary>
        /// This contains number of lines WE use for header when compiling script. User will get error in line x-LinesToRemoveOnError when error occurs.
        /// </summary>
        // Not used anymore?
        public int LinesToRemoveOnError = 3;

        private enumCompileType DefaultCompileLanguage;
        private bool WriteScriptSourceToDebugFile;
        private bool CompileWithDebugInformation;
        private Dictionary<string, bool> AllowedCompilers = new Dictionary<string, bool>(StringComparer.CurrentCultureIgnoreCase);
        private Dictionary<string, enumCompileType> LanguageMapping = new Dictionary<string, enumCompileType>(StringComparer.CurrentCultureIgnoreCase);

        public bool firstStartup = true;
        private string FilePrefix;
        private string ScriptEnginesPath = "ScriptEngines";
        // mapping between LSL and C# line/column numbers
        private ICodeConverter LSL_Converter;

        private List<string> m_warnings = new List<string>();

        // private object m_syncy = new object();

        private static CSharpCodeProvider CScodeProvider = new CSharpCodeProvider();
        private static VBCodeProvider VBcodeProvider = new VBCodeProvider();
//        private static JScriptCodeProvider JScodeProvider = new JScriptCodeProvider();
        private static CSharpCodeProvider YPcodeProvider = new CSharpCodeProvider(); // YP is translated into CSharp
        private static YP2CSConverter YP_Converter = new YP2CSConverter();

        // private static int instanceID = new Random().Next(0, int.MaxValue);                 // Unique number to use on our compiled files
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

            //m_log.Debug("[Compiler]: Allowed languages: " + allowComp);

            foreach (string strl in allowComp.Split(','))
            {
                string strlan = strl.Trim(" \t".ToCharArray()).ToLower();
                if (!LanguageMapping.ContainsKey(strlan))
                {
                    m_log.Error("[Compiler]: Config error. Compiler is unable to recognize language type \"" + strlan + "\" specified in \"AllowedCompilers\".");
                }
                AllowedCompilers.Add(strlan, true);
            }
            if (AllowedCompilers.Count == 0)
                m_log.Error("[Compiler]: Config error. Compiler could not recognize any language in \"AllowedCompilers\". Scripts will not be executed!");

            // Default language
            string defaultCompileLanguage = m_scriptEngine.Config.GetString("DefaultCompileLanguage", "lsl").ToLower();

            // Is this language recognized at all?
            if (!LanguageMapping.ContainsKey(defaultCompileLanguage))
            {
                m_log.Error("[Compiler]: " +
                                            "Config error. Default language \"" + defaultCompileLanguage + "\" specified in \"DefaultCompileLanguage\" is not recognized as a valid language. Changing default to: \"lsl\".");
                defaultCompileLanguage = "lsl";
            }

            // Is this language in allow-list?
            if (!AllowedCompilers.ContainsKey(defaultCompileLanguage))
            {
                m_log.Error("[Compiler]: " +
                            "Config error. Default language \"" + defaultCompileLanguage + "\"specified in \"DefaultCompileLanguage\" is not in list of \"AllowedCompilers\". Scripts may not be executed!");
            }
            else
            {
                // LANGUAGE IS IN ALLOW-LIST
                DefaultCompileLanguage = LanguageMapping[defaultCompileLanguage];
            }

            // We now have an allow-list, a mapping list, and a default language
        }

        public void MapCompilers()
        {
            // Map name and enum type of our supported languages
            LanguageMapping.Add(enumCompileType.cs.ToString(), enumCompileType.cs);
            LanguageMapping.Add(enumCompileType.vb.ToString(), enumCompileType.vb);
            LanguageMapping.Add(enumCompileType.lsl.ToString(), enumCompileType.lsl);
            LanguageMapping.Add(enumCompileType.js.ToString(), enumCompileType.js);
            LanguageMapping.Add(enumCompileType.yp.ToString(), enumCompileType.yp);
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

            enumCompileType language;
            string compileScript;

            CheckLanguageAndConvert(Script, ownerUUID, out language, out compileScript);

			assembly = CompileFromDotNetText(compileScript, language, assembly);
        }

        private void CheckLanguageAndConvert(string Script,UUID ownerID, out enumCompileType language, out string compileScript)
        {
            language = DefaultCompileLanguage;

            if (Script.StartsWith("//c#", true, CultureInfo.InvariantCulture))
                language = enumCompileType.cs;
            if (Script.StartsWith("//vb", true, CultureInfo.InvariantCulture))
            {
                language = enumCompileType.vb;
                // We need to remove //vb, it won't compile with that

                Script = Script.Substring(4, Script.Length - 4);
            }
            if (Script.StartsWith("//lsl", true, CultureInfo.InvariantCulture))
                language = enumCompileType.lsl;

            if (Script.StartsWith("//js", true, CultureInfo.InvariantCulture))
                language = enumCompileType.js;

            if (Script.StartsWith("//yp", true, CultureInfo.InvariantCulture))
                language = enumCompileType.yp;

            if (!AllowedCompilers.ContainsKey(language.ToString()))
            {
                // Not allowed to compile to this language!
                string errtext = String.Empty;
                errtext += "The compiler for language \"" + language.ToString() + "\" is not in list of allowed compilers. Script will not be executed!";
                throw new Exception(errtext);
            }

            if (m_scriptEngine.World.Permissions.CanCompileScript(ownerID, (int)language) == false)
            {
                // Not allowed to compile to this language!
                string errtext = String.Empty;
                errtext += ownerID + " is not in list of allowed users for this scripting language. Script will not be executed!";
                throw new Exception(errtext);
            }

            compileScript = Script;
            if (language == enumCompileType.lsl)
            {
                // Its LSL, convert it to C#
                LSL_Converter = (ICodeConverter)new CSCodeGenerator();
                compileScript = LSL_Converter.Convert(Script);

                // copy converter warnings into our warnings.
                foreach (string warning in LSL_Converter.GetWarnings())
                {
                    AddWarning(warning);
                }
            }

            if (language == enumCompileType.yp)
            {
                // Its YP, convert it to C#
                compileScript = YP_Converter.Convert(Script);
            }

            switch (language)
            {
                case enumCompileType.cs:
                case enumCompileType.lsl:
                    compileScript = CreateCSCompilerScript(compileScript);
                    break;
                case enumCompileType.vb:
                    compileScript = CreateVBCompilerScript(compileScript);
                    break;
                //                case enumCompileType.js:
                //                    compileScript = CreateJSCompilerScript(compileScript);
                //                    break;
                case enumCompileType.yp:
                    compileScript = CreateYPCompilerScript(compileScript);
                    break;
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

//        private static string CreateJSCompilerScript(string compileScript)
//        {
//            compileScript = String.Empty +
//                "import OpenSim.Region.ScriptEngine.Shared; import System.Collections.Generic;\r\n" +
//                "package SecondLife {\r\n" +
//                "class Script extends OpenSim.Region.ScriptEngine.Shared.ScriptBase.ScriptBaseClass { \r\n" +
//                compileScript +
//                "} }\r\n";
//            return compileScript;
//        }

        /*private static string CreateCSCompilerScript(string compileScript, string identifier, string InheritedClasses, UUID itemID, uint localID, UUID ownerID, IScriptProtectionModule ScriptProtection, object InstanceData)
        {             
        	string compiledScript = "";
            compiledScript = String.Empty +
                "using OpenSim.Region.ScriptEngine.Shared;" +
            	"\nusing System;" +
            	"\nusing System.Collections.Generic;" +
            	"\nusing System.Collections;\n" +
                "using System.Timers;\n" +
                "namespace Script\n{\n";
            string TempClassScript = "";
            TempClassScript = String.Empty + "[Serializable]\n public class " + identifier + " : OpenSim.Region.ScriptEngine.Shared.ScriptBase.ScriptBaseClass, IDisposable";
            if (InheritedClasses != "")
                TempClassScript += "," + InheritedClasses;
            TempClassScript += "\n{\n" +
                     "List<IEnumerator> parts = new List<IEnumerator>();\n";
            TempClassScript += "System.Timers.Timer aTimer = new System.Timers.Timer(250);\n";
            TempClassScript += "public " + identifier + "()\n{\n";
            TempClassScript += "aTimer.Elapsed += new System.Timers.ElapsedEventHandler(Timer);\n";
            TempClassScript += "aTimer.Enabled = true;\n";
            TempClassScript += "aTimer.Start();\n";
            TempClassScript += "}\n";
            TempClassScript += "~" + identifier + "()\n{\n";
            TempClassScript += "aTimer.Stop();\n";
            TempClassScript += "aTimer.Dispose();\n";
            TempClassScript += "}\n";
            TempClassScript += "public void Dispose()\n";
            TempClassScript += "{\n";
            TempClassScript += "aTimer.Stop();\n";
            TempClassScript += "aTimer.Dispose();\n";
            TempClassScript += "}\n";
        
            TempClassScript += "public void Timer(object source, System.Timers.ElapsedEventArgs e)\n{\n";
            TempClassScript += "lock (parts)\n";
            TempClassScript += "{\n";
            TempClassScript += "int i = 0;\n";
            TempClassScript += "if(parts.Count == 0)\n";
            TempClassScript += "return;";
            TempClassScript += "while (parts.Count > 0 && i < 1000)\n";
            TempClassScript += "{\n";
            TempClassScript += "i++;\n";

            TempClassScript += "bool running = false;\n";
            TempClassScript += "try\n";
            TempClassScript += "{\n";
            TempClassScript += "running = parts[i % parts.Count].MoveNext();\n";
            TempClassScript += "}\n";
            TempClassScript += "catch (Exception ex)\n";
            TempClassScript += "{\n";
            TempClassScript += "}\n";

            TempClassScript += "if (!running)\n { \n";
            TempClassScript += "parts.Remove(parts[i % parts.Count]);\n } \n";
            TempClassScript += "else\n { } \n";
            TempClassScript += "}\n";
            TempClassScript += "}\n";
            TempClassScript += "}\n";

            TempClassScript +=
                     compileScript +
                     "\n}";

            ScriptProtection.AddNewClassSource(identifier, TempClassScript, InstanceData);
            TempClassScript += ScriptProtection.GetSRC(itemID, localID, ownerID);
            compiledScript += TempClassScript +
                 "\n}";

            return compiledScript;
        }*/

        private static string CreateCSCompilerScript(string compileScript)
        {
            string compiledScript = "";
            compiledScript = String.Empty +
                "using OpenSim.Region.ScriptEngine.Shared;" +
                "\nusing System;" +
                "\nusing System.Collections.Generic;" +
                "\nusing System.Collections;\n" +
                "using System.Timers;\n" +
                "namespace Script\n{\n";

            compiledScript += "[Serializable]\n public class LSL : OpenSim.Region.ScriptEngine.Shared.ScriptBase.ScriptBaseClass, IDisposable";
            compiledScript += "\n{\n" +
                     "List<IEnumerator> parts = new List<IEnumerator>();\n";
            compiledScript += "System.Timers.Timer aTimer = new System.Timers.Timer(250);\n";
            compiledScript += "public LSL()\n{\n";
            compiledScript += "aTimer.Elapsed += new System.Timers.ElapsedEventHandler(Timer);\n";
            compiledScript += "aTimer.Enabled = true;\n";
            compiledScript += "aTimer.Start();\n";
            compiledScript += "}\n";
            compiledScript += "~LSL()\n{\n";
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

            compiledScript +=
                     compileScript +
                     "\n}"; // Close Class

            compiledScript += "\n}"; // Close Namespace

            return compiledScript;
        }

        private static string CreateYPCompilerScript(string compileScript)
        {
            compileScript = String.Empty +
                       "using OpenSim.Region.ScriptEngine.Shared.YieldProlog; " +
                        "using OpenSim.Region.ScriptEngine.Shared; using System.Collections.Generic;\r\n" +
                        String.Empty + "namespace SecondLife { " +
                        String.Empty + "public class Script : OpenSim.Region.ScriptEngine.Shared.ScriptBase.ScriptBaseClass  { \r\n" +
                        //@"public Script() { } " +
                        @"static OpenSim.Region.ScriptEngine.Shared.YieldProlog.YP YP=null; " +
                        @"public Script() {  YP= new OpenSim.Region.ScriptEngine.Shared.YieldProlog.YP(); } " +

                        compileScript +
                        "} }\r\n";
            return compileScript;
        }

        private static string CreateVBCompilerScript(string compileScript)
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

        /// <summary>
        /// Compile .NET script to .Net assembly (.dll)
        /// </summary>
        /// <param name="Script">CS script</param>
        /// <returns>Filename to .dll assembly</returns>
        internal string CompileFromDotNetText(string Script, enumCompileType lang, string assembly)
        {
            string ext = "." + lang.ToString();

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
            
            if (lang == enumCompileType.yp)
            {
                parameters.ReferencedAssemblies.Add(Path.Combine(rootPath,
                        "OpenSim.Region.ScriptEngine.Shared.YieldProlog.dll"));
            }

            parameters.GenerateExecutable = false;
            parameters.OutputAssembly = assembly;
            parameters.IncludeDebugInformation = CompileWithDebugInformation;
            //parameters.WarningLevel = 1; // Should be 4?
            parameters.TreatWarningsAsErrors = false;

            CompilerResults results;
            switch (lang)
            {
                case enumCompileType.vb:
                    results = VBcodeProvider.CompileAssemblyFromSource(
                            parameters, Script);
                    break;
                case enumCompileType.cs:
                case enumCompileType.lsl:
                    bool complete = false;
                    bool retried = false;
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
                    break;
//                case enumCompileType.js:
//                    results = JScodeProvider.CompileAssemblyFromSource(
//                        parameters, Script);
//                    break;
                case enumCompileType.yp:
                    results = YPcodeProvider.CompileAssemblyFromSource(
                        parameters, Script);
                    break;
                default:
                    throw new Exception("Compiler is not able to recongnize " +
                                        "language type \"" + lang.ToString() + "\"");
            }

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
                        lslPos = FindErrorPosition(CompErr.Line, CompErr.Column, ((CSCodeGenerator)LSL_Converter).PositionMap);
                        string text = CompErr.ErrorText;

                        // Use LSL type names
                        if (lang == enumCompileType.lsl)
                            text = ReplaceTypes(CompErr.ErrorText);

                        // The Second Life viewer's script editor begins
                        // countingn lines and columns at 0, so we subtract 1.
                        errtext += String.Format("({0},{1}): {4} {2}: {3}\n",
                                lslPos.Key -1 , lslPos.Value -1 ,
                                CompErr.ErrorNumber, text, severity);
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

            //            m_log.DebugFormat("[Compiler] Compiled new assembly "+
            //                    "for {0}", asset);

            // Because windows likes to perform exclusive locks, we simply
            // write out a textual representation of the file here
            //
            // Read the binary file into a buffer
            //
            /*FileInfo fi = new FileInfo(assembly);

            if (fi == null)
            {
                errtext = String.Empty;
                errtext += "No compile error. But not able to start file.";
                throw new Exception(errtext);
            }

            Byte[] data = new Byte[fi.Length];

            try
            {
                FileStream fs = File.Open(assembly, FileMode.Open, FileAccess.Read);
                fs.Read(data, 0, data.Length);
                fs.Close();
            }
            catch (Exception)
            {
                errtext = String.Empty;
                errtext += "No compile error. But not able to open file.";
                throw new Exception(errtext);
            }

            // Convert to base64
            //
            AssemblyText = System.Convert.ToBase64String(data);

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();

            Byte[] buf = enc.GetBytes(AssemblyText);

            FileStream sfs = File.Create(assembly + ".text");
            sfs.Write(buf, 0, buf.Length);
            sfs.Close();*/

            return assembly;
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

        #region LineMaps

        private static void WriteMapFile(string filename, Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> linemap)
        {
            string mapstring = String.Empty;
            foreach (KeyValuePair<KeyValuePair<int, int>, KeyValuePair<int, int>> kvp in linemap)
            {
                KeyValuePair<int, int> k = kvp.Key;
                KeyValuePair<int, int> v = kvp.Value;
                mapstring += String.Format("{0},{1},{2},{3}\n", k.Key, k.Value, v.Key, v.Value);
            }

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            Byte[] mapbytes = enc.GetBytes(mapstring);
            FileStream mfs = File.Create(filename);
            mfs.Write(mapbytes, 0, mapbytes.Length);
            mfs.Close();
        }


        public static Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> ReadMapFile(string filename)
        {
            Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> linemap;
            try
            {
                StreamReader r = File.OpenText(filename);
                linemap = new Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>>();

                string line;
                while ((line = r.ReadLine()) != null)
                {
                    String[] parts = line.Split(new Char[] { ',' });
                    int kk = System.Convert.ToInt32(parts[0]);
                    int kv = System.Convert.ToInt32(parts[1]);
                    int vk = System.Convert.ToInt32(parts[2]);
                    int vv = System.Convert.ToInt32(parts[3]);

                    KeyValuePair<int, int> k = new KeyValuePair<int, int>(kk, kv);
                    KeyValuePair<int, int> v = new KeyValuePair<int, int>(vk, vv);

                    linemap[k] = v;
                }
            }
            catch
            {
                linemap = new Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>>();
            }
            return linemap;
        }

        public static Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> ReadMapFileFromString(string text)
        {
            Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> linemap;
            try
            {
                linemap = new Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>>();

                string line;
                int i = 0;
                while ((line = text.Split(';')[i]) != null)
                {
                    if (line == "")
                        break;
                    String[] parts = line.Split(',');
                    int kk = System.Convert.ToInt32(parts[0]);
                    int kv = System.Convert.ToInt32(parts[1]);
                    int vk = System.Convert.ToInt32(parts[2]);
                    int vv = System.Convert.ToInt32(parts[3]);

                    KeyValuePair<int, int> k = new KeyValuePair<int, int>(kk, kv);
                    KeyValuePair<int, int> v = new KeyValuePair<int, int>(vk, vv);

                    linemap[k] = v;
                    i++;
                }
            }
            catch
            {
                linemap = new Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>>();
            }
            return linemap;
        }

        #endregion

        /// <summary>
        /// Generates a random string with the given length
        /// </summary>
        /// <param name="size">Size of the string</param>
        /// <param name="lowerCase">If true, generate lowercase string</param>
        /// <returns>Random string</returns>
        private string RandomString(int size, bool lowerCase)
        {
        	StringBuilder builder = new StringBuilder();
        	Random random = new Random();
        	char ch ;
        	for(int i=0; i<size; i++)
        	{
                ch = System.Convert.ToChar(System.Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
        		builder.Append(ch);
        	}
        	if(lowerCase)
        		return builder.ToString().ToLower();
        	return builder.ToString();
        }
        #endregion
    }
}
