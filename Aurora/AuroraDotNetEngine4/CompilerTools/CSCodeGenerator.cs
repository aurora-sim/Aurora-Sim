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

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Tools;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools
{
    public static class Extension
    {
        public static bool CompareWildcards(this string WildString, string Mask, bool IgnoreCase)
        {
            int i = 0;

            if (String.IsNullOrEmpty(Mask))
                return false;
            if (Mask == "*")
                return true;

            while (i != Mask.Length)
            {
                if (CompareWildcard(WildString, Mask.Substring(i), IgnoreCase))
                    return true;

                while (i != Mask.Length && Mask[i] != ';')
                    i += 1;

                if (i != Mask.Length && Mask[i] == ';')
                {
                    i += 1;

                    while (i != Mask.Length && Mask[i] == ' ')
                        i += 1;
                }
            }

            return false;
        }

        public static bool CompareWildcard(this string WildString, string Mask, bool IgnoreCase)
        {
            int i = 0, k = 0;

            while (k != WildString.Length)
            {
                if ((i + 1) > Mask.Length)
                    return true;
                switch (Mask[i])
                {
                    case '*':

                        if ((i + 1) >= Mask.Length)
                            return true;

                        while (k != WildString.Length)
                        {
                            if (CompareWildcard(WildString.Substring(k + 1), Mask.Substring(i + 1), IgnoreCase))
                                return true;

                            k += 1;
                        }

                        return false;

                    case '?':

                        break;

                    default:

                        if (IgnoreCase == false && WildString[k] != Mask[i])
                            return false;
                        if (IgnoreCase && Char.ToLower(WildString[k]) != Char.ToLower(Mask[i]))
                            return false;

                        break;
                }

                i += 1;
                k += 1;
            }

            if (k == WildString.Length)
            {
                if (i == Mask.Length || Mask[i] == ';' || Mask[i] == '*')
                    return true;
            }

            return false;
        }
    }

    public class CSCodeGenerator : IDisposable
    {
        private SYMBOL m_astRoot = null;
        private Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> m_positionMap;
        private int m_indentWidth = 4;  // for indentation
        private int m_braceCount;       // for indentation
        private int m_CSharpLine;       // the current line of generated C# code
        private int m_CSharpCol;        // the current column of generated C# code
        private string OriginalScript = "";
        private Parser p = null;
        private Random random = new Random();
        private bool IsaGlobalVar = false;

        private class GlobalVar
        {
            public string Type;
            public string Value;
        }
        private HashSet<string> DTFunctions = new HashSet<string>();
        //        public Dictionary<string, string> IenFunctions = new Dictionary<string, string>();
        public Dictionary<string, string> LocalMethods = new Dictionary<string, string>();
        public Dictionary<string, ObjectList> LocalMethodArguements = new Dictionary<string, ObjectList>();
        private Dictionary<string, GlobalVar> GlobalVariables = new Dictionary<string, GlobalVar>();

        /// <summary>
        /// This saves the variables in methods so that we can make sure multiple variables do not have the same name, and if they do, rename/assign them to the correct variable name
        /// </summary>
        private Dictionary<string, GlobalVar> MethodVariables = new Dictionary<string, GlobalVar>();

        private class VarRename
        {
            //public string NewVarName;
            //public bool HasBeenAssigned;
            //public string OldVarName;
        }
        /// <summary>
        /// This contains a list of variables that we need to rename because of some constraint
        /// </summary>
        private Dictionary<string, VarRename> VariablesToRename = new Dictionary<string, VarRename>();
        private List<string> FuncCalls = new List<string>();
        private List<string> MethodsToAdd = new List<string>();
        /// <summary>
        /// Param 1 - the API function name, Param 2 - the API name
        /// </summary>
        private Dictionary<string, IScriptApi> m_apiFunctions = new Dictionary<string, IScriptApi>();
        private Compiler m_compiler;

        /// <summary>
        /// Creates an 'empty' CSCodeGenerator instance.
        /// </summary>
        public CSCodeGenerator(Compiler compiler)
        {
            ResetCounters();
            m_compiler = compiler;

            IScriptApi[] apis = compiler.ScriptEngine.GetAPIs();

            foreach (IScriptApi api in apis)
            {
                List<string> FunctionNames = compiler.ScriptEngine.GetFunctionNames(api);
                foreach (string functionName in FunctionNames)
                {
                    if (!m_apiFunctions.ContainsKey(functionName))
                        m_apiFunctions.Add(functionName, api);
                }
            }
        }

        /// <summary>
        /// Get the mapping between LSL and C# line/column number.
        /// </summary>
        /// <returns>Dictionary\<KeyValuePair\<int, int\>, KeyValuePair\<int, int\>\>.</returns>
        public Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> PositionMap
        {
            get { return m_positionMap; }
        }

        /// <summary>
        /// Get the mapping between LSL and C# line/column number.
        /// </summary>
        /// <returns>SYMBOL pointing to root of the abstract syntax tree.</returns>
        public SYMBOL ASTRoot
        {
            get { return m_astRoot; }
        }

        /// <summary>
        /// Resets various counters and metadata.
        /// </summary>
        private void ResetCounters()
        {
            //NOTE: This takes a VERY long time to rebuild. Ideally, this should be reset, but interesting errors are happening when it is reset..
            p = new LSLSyntax(new yyLSLSyntax(), new ErrorHandler(true));
            MethodVariables.Clear();
            VariablesToRename.Clear();
            GlobalVariables.Clear();
            m_braceCount = 0;
            m_CSharpLine = 15;
            m_CSharpCol = 1;
            m_positionMap = new Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>>();
            LocalMethods.Clear();          
            m_astRoot = null;
            OriginalScript = "";
            lock (FuncCalls)
                FuncCalls.Clear();
            IsaGlobalVar = false;
            MethodsToAdd.Clear();
            LocalMethodArguements.Clear();
        }

        private string CreateCompilerScript(string ScriptClass)
        {
            string compiledScript = "";
            compiledScript += "using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;\n";
            compiledScript += "using Aurora.ScriptEngine.AuroraDotNetEngine;\n";
            compiledScript += "using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;\n";
            compiledScript += "using System;\n";
            compiledScript += "using System.Collections.Generic;\n";
            compiledScript += "using System.Collections;\n";
            compiledScript += "using System.Reflection;\n";
            compiledScript += "using System.Timers;\n";
            foreach (IScriptApi api in m_compiler.ScriptEngine.GetAPIs())
            {
                foreach (string nameSpace in api.NamespaceAdditions)
                {
                    compiledScript += "using " + nameSpace + ";\n";
                }
            }

            compiledScript += "namespace Script\n";
            compiledScript += "{\n";

            compiledScript += "[Serializable]\n";
            compiledScript += "public class ScriptClass : Aurora.ScriptEngine.AuroraDotNetEngine.Runtime.ScriptBaseClass\n";
            compiledScript += "{\n";

            compiledScript += ScriptClass;
            foreach(string method in MethodsToAdd)
            {
                compiledScript += method;
            }

            compiledScript += "}\n"; // Close Class

            compiledScript += "}\n"; // Close Namespace

            return compiledScript.ToString();
        }

        /// <summary>
        /// Generate the code from the AST we have.
        /// </summary>
        /// <param name="script">The LSL source as a string.</param>
        /// <returns>String containing the generated C# code.</returns>
        public string Convert(string script)
        {
            ResetCounters();

            LSL2CSCodeTransformer codeTransformer;
            try
            {
 //               lock (p)
                {
                    codeTransformer = new LSL2CSCodeTransformer(p.Parse(script));
//                    p.m_lexer.Reset();
                }
            }
            catch (CSToolsException e)
            {
                string message;

                // LL start numbering lines at 0 - geeks!
                // Also need to subtract one line we prepend!
                //
                string emessage = e.Message;
                string slinfo = e.slInfo.ToString();

                // Remove wrong line number info
                //
                if (emessage.StartsWith(slinfo + ": "))
                    emessage = emessage.Substring(slinfo.Length + 2);

                if (e.slInfo.lineNumber - 1 <= 0)
                    e.slInfo.lineNumber = 2;
                if (e.slInfo.charPosition - 1 <= 0)
                    e.slInfo.charPosition = 2;

                message = String.Format("({0},{1}) {2}",
                        e.slInfo.lineNumber - 1,
                        e.slInfo.charPosition - 1, emessage);

                m_compiler.AddError(message);
//                p.m_lexer.Reset();
                ResetCounters();
                return "Error parsing the script. " + message;
            }

            m_astRoot = codeTransformer.Transform(LocalMethods, LocalMethodArguements);
            OriginalScript = script;
            string returnstring = "";

            // line number
            //m_CSharpLine += 3;

            // here's the payload
            returnstring += GenerateLine();
            foreach (SYMBOL s in m_astRoot.kids)
                returnstring += GenerateNode(s);

            // Removes all carriage return characters which may be generated in Windows platform. 
            //Is there a cleaner way of doing this?
            returnstring = returnstring.Replace("\r", "");

            CheckEventCasts(returnstring);
            return CreateCompilerScript(returnstring);
        }

        private string CheckFloatExponent(string script)
        {
            string[] SplitScript = script.Split('\n');
            List<string> ReconstructableScript = new List<string>();

            foreach (string line in SplitScript)
            {
                string AddLine = line;
                if (AddLine.Replace(" ", "") == "default")
                    break; //We only check above default

                if (AddLine.Contains("float"))
                {
                    if (line.Contains("e") && !line.Contains("(") && !line.Contains(")")) // Looking for exponents, but not for functions that have ()
                    {
                        //Should have this - float *** = 151e9;
                        string[] SplitBeforeE = AddLine.Split('='); // Split at the e so we can look at the syntax
                        if (SplitBeforeE.Length != 1)
                        {
                            string[] SplitBeforeESpace = SplitBeforeE[1].Split(';');
                            //Should have something like this now - 151
                            if (SplitBeforeESpace[0].Contains("e"))
                            {
                                if (!SplitBeforeESpace[0].Contains("."))
                                {
                                    //Needs one then
                                    string[] Split = SplitBeforeESpace[0].Split('e'); // Split at the e so we can look at the syntax
                                    Split[0] += ".";
                                    AddLine = "";
                                    string TempString = "";
                                    foreach (string tempLine in Split)
                                    {
                                        TempString += tempLine + "e";
                                    }
                                    TempString = TempString.Remove(TempString.Length - 1, 1);
                                    SplitBeforeESpace[0] = TempString;
                                    TempString = "";
                                    foreach (string tempLine in SplitBeforeESpace)
                                    {
                                        TempString += tempLine + ";";
                                    }
                                    //Remove the last ;
                                    TempString = TempString.Remove(TempString.Length - 1, 1);
                                    SplitBeforeE[1] = TempString;
                                    foreach (string tempLine in SplitBeforeE)
                                    {
                                        AddLine += tempLine + "=";
                                    }
                                    //Remove the last e
                                    AddLine = AddLine.Remove(AddLine.Length - 1, 1);
                                }
                            }
                        }
                    }
                }
                ReconstructableScript.Add(AddLine);
            }
            string RetVal = "";
            foreach (string line in ReconstructableScript)
            {
                RetVal += line + "\n";
            }
            return RetVal;
        }

        private string CheckForInlineVectors(string script)
        {
            string[] SplitScript = script.Split('\n');
            List<string> ReconstructableScript = new List<string>();
            foreach (string line in SplitScript)
            {
                string AddLine = line;
                if (AddLine.Contains("<") && AddLine.Contains(">"))
                {
                    if (AddLine.Contains("\"")) // if it contains ", we need to be more careful
                    {
                        string[] SplitByParLine = AddLine.Split('\"');
                        int lineNumber = 0;
                        List<string> ReconstructableLine = new List<string>();
                        foreach (string parline in SplitByParLine)
                        {
                            string AddInsideLine = parline;
                            //throw out all odd numbered lines as they are inside ""
                            if (lineNumber % 2 != 1)
                            {
                                string[] SplitLineA = AddLine.Split('<');
                                if (SplitLineA.Length > 1)
                                {
                                    string SplitLineB = SplitLineA[1].Split('>')[0];
                                    if (SplitLineB.CompareWildcard("*,*,*", true))
                                    {
                                        AddInsideLine = AddInsideLine.Replace("<", "(<");
                                        AddInsideLine = AddInsideLine.Replace(">", ">)");
                                    }
                                }
                            }
                            ReconstructableLine.Add(AddInsideLine);
                            lineNumber++;
                        }
                        AddLine = "";
                        foreach (string insideline in ReconstructableLine)
                        {
                            AddLine += insideline + "\"";
                        }
                        AddLine = AddLine.Remove(AddLine.Length - 1, 1);
                    }
                    else
                    {
                        string[] SplitLineA = AddLine.Split('<');
                        if (SplitLineA.Length > 1)
                        {
                            string SplitLineB = SplitLineA[1].Split('>')[0];
                            if (SplitLineB.CompareWildcard("*,*,*", true))
                            {
                                AddLine = AddLine.Replace("<", "(<");
                                AddLine = AddLine.Replace(">", ">)");
                            }
                        }
                    }
                }
                ReconstructableScript.Add(AddLine);
            }
            string RetVal = "";
            foreach (string line in ReconstructableScript)
            {
                RetVal += line + "\n";
            }
            return RetVal;
        }

        /// <summary>
        /// Checks the C# script for the correct casts in events
        /// This stops errors from misformed events ex. 'touch(vector3 position)' instead of 'touch(int touch)'
        /// </summary>
        /// <param name="script"></param>
        private void CheckEventCasts(string script)
        {
            CheckEvent(script, "default");
            string[] States = OriginalScript.Split(new string[] { "state " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string state in States)
            {
                string stateName = state.Split(' ')[0];
                stateName = state.Split('\n')[0];
                if (!stateName.Contains("default"))
                    CheckEvent(script, stateName);
            }
        }

        private void CheckEvent(string script, string state)
        {
            if (script.Contains(state + "_event_state_entry("))
            {
                string Valid = state + "_event_state_entry()";
                int charNum = script.IndexOf(state + "_event_state_entry(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);
                if (splitScript != Valid)
                {
                    FindLineNumbers("state_entry", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_touch_start("))
            {
                //Valid : default_event_touch_start(LSL_Types.LSLInteger number)
                int charNum = script.IndexOf(state + "_event_touch_start(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                    {
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("touch_start", "Invalid argument");
                    }
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("touch_start", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_at_rot_target("))
            {
                //Valid : default_event_at_rot_target(LSL_Types.LSLInteger tnum, LSL_Types.Quaternion targetrot, LSL_Types.Quaternion ourrot)
                int charNum = script.IndexOf(state + "_event_at_rot_target(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("at_rot_target", "Invalid argument");
                    if (i == 1 || i == 2)
                        if (!argument.Contains("LSL_Types.Quaternion"))
                            FindLineNumbers("at_rot_target", "Invalid argument");
                    i++;
                }
                if (i != 3)
                {
                    FindLineNumbers("at_rot_target", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_at_target("))
            {
                //Valid : default_event_at_rot_target(LSL_Types.LSLInteger tnum, LSL_Types.Quaternion targetrot, LSL_Types.Quaternion ourrot)
                int charNum = script.IndexOf(state + "_event_at_target(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("at_target", "Invalid argument");
                    if (i == 1 || i == 2)
                        if (!argument.Contains("LSL_Types.Vector3"))
                            FindLineNumbers("at_target", "Invalid argument");
                    i++;
                }
                if (i != 3)
                {
                    FindLineNumbers("at_target", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_not_at_target("))
            {
                int charNum = script.IndexOf(state + "_event_not_at_target(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string Valid = state + "_event_not_at_target()";
                if (splitScript != Valid)
                {
                    FindLineNumbers("not_at_target", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_attach("))
            {
                int charNum = script.IndexOf(state + "_event_attach(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                    {
                        if (!argument.Contains("LSL_Types.LSLString"))
                            FindLineNumbers("attach", "Invalid argument");
                    }
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("attach", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_changed("))
            {
                int charNum = script.IndexOf(state + "_event_changed(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                    {
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("changed", "Invalid argument");
                    }
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("changed", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_collision("))
            {
                int charNum = script.IndexOf(state + "_event_collision(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                    {
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("collision", "Invalid argument");
                    }
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("collision", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_collision_end("))
            {
                int charNum = script.IndexOf(state + "_event_collision_end(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                    {
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("collision_end", "Invalid argument");
                    }
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("collision_end", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_collision_start("))
            {
                int charNum = script.IndexOf(state + "_event_collision_start(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                    {
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("collision_start", "Invalid argument");
                    }
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("collision_start", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_run_time_permissions("))
            {
                int charNum = script.IndexOf(state + "_event_run_time_permissions(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                    {
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("run_time_permissions", "Invalid argument");
                    }
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("run_time_permissions", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_control("))
            {
                int charNum = script.IndexOf(state + "_event_control(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.LSLString"))
                            FindLineNumbers("control", "Invalid argument");
                    if (i == 1 || i == 2)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("control", "Invalid argument");
                    i++;
                }
                if (i != 3)
                {
                    FindLineNumbers("control", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_dataserver("))
            {
                int charNum = script.IndexOf(state + "_event_dataserver(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0 || i == 1)
                        if (!argument.Contains("LSL_Types.LSLString"))
                            FindLineNumbers("dataserver", "Invalid argument");
                    i++;
                }
                if (i != 2)
                {
                    FindLineNumbers("dataserver", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_timer("))
            {
                int charNum = script.IndexOf(state + "_event_timer(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string Valid = state + "_event_timer()";
                if (splitScript != Valid)
                {
                    FindLineNumbers("timer", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_email("))
            {
                int charNum = script.IndexOf(state + "_event_email(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0 || i == 1 || i == 2 || i == 3)
                        if (!argument.Contains("LSL_Types.LSLString"))
                            FindLineNumbers("email", "Invalid argument");
                    if (i == 4)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("email", "Invalid argument");
                    i++;
                }
                if (i != 5)
                {
                    FindLineNumbers("email", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_http_request("))
            {
                int charNum = script.IndexOf(state + "_event_http_request(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0 || i == 1 || i == 2)
                        if (!argument.Contains("LSL_Types.LSLString"))
                            FindLineNumbers("http_request", "Invalid argument");
                    i++;
                }
                if (i != 3)
                {
                    FindLineNumbers("http_request", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_http_response("))
            {
                int charNum = script.IndexOf(state + "_event_http_response(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0 || i == 3)
                        if (!argument.Contains("LSL_Types.LSLString"))
                            FindLineNumbers("http_response", "Invalid argument");
                    if (i == 1)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("http_response", "Invalid argument");
                    if (i == 2)
                        if (!argument.Contains("LSL_Types.list"))
                            FindLineNumbers("http_response", "Invalid argument");
                    i++;
                }
                if (i != 4)
                {
                    FindLineNumbers("http_response", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_land_collision_end("))
            {
                int charNum = script.IndexOf(state + "_event_land_collision_end(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.Vector3"))
                            FindLineNumbers("land_collision_end", "Invalid argument");
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("land_collision_end", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_land_collision_start("))
            {
                int charNum = script.IndexOf(state + "_event_land_collision_start(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.Vector3"))
                            FindLineNumbers("land_collision_start", "Invalid argument");
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("land_collision_start", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_link_message("))
            {
                int charNum = script.IndexOf(state + "_event_link_message(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0 || i == 1)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("link_message", "Invalid argument");
                    if (i == 2 || i == 3)
                        if (!argument.Contains("LSL_Types.LSLString"))
                            FindLineNumbers("link_message", "Invalid argument");
                    i++;
                }
                if (i != 4)
                {
                    FindLineNumbers("link_message", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_listen("))
            {
                int charNum = script.IndexOf(state + "_event_listen(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("listen", "Invalid argument");
                    if (i == 1 || i == 2 || i == 3)
                        if (!argument.Contains("LSL_Types.LSLString"))
                            FindLineNumbers("listen", "Invalid argument");
                    i++;
                }
                if (i != 4)
                {
                    FindLineNumbers("listen", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_on_rez("))
            {
                int charNum = script.IndexOf(state + "_event_on_rez(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("on_rez", "Invalid argument");
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("on_rez", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_money("))
            {
                int charNum = script.IndexOf(state + "_event_money(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.LSLString"))
                            FindLineNumbers("money", "Invalid argument");
                    if (i == 1)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("money", "Invalid argument");
                    i++;
                }
                if (i != 2)
                {
                    FindLineNumbers("money", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_moving_end("))
            {
                int charNum = script.IndexOf(state + "_event_moving_end(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string Valid = state + "_event_moving_end()";
                if (splitScript != Valid)
                {
                    FindLineNumbers("moving_end", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_moving_start("))
            {
                int charNum = script.IndexOf(state + "_event_moving_start(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string Valid = state + "_event_moving_start()";
                if (splitScript != Valid)
                {
                    FindLineNumbers("moving_start", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_no_sensor("))
            {
                int charNum = script.IndexOf(state + "_event_no_sensor(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string Valid = state + "_event_no_sensor()";
                if (splitScript != Valid)
                {
                    FindLineNumbers("no_sensor", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_not_at_rot_target("))
            {
                int charNum = script.IndexOf(state + "_event_not_at_rot_target(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string Valid = state + "_event_not_at_rot_target()";
                if (splitScript != Valid)
                {
                    FindLineNumbers("not_at_rot_target", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_object_rez("))
            {
                int charNum = script.IndexOf(state + "_event_object_rez(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.LSLString"))
                            FindLineNumbers("object_rez", "Invalid argument");
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("object_rez", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_on_error("))
            {
                int charNum = script.IndexOf(state + "_event_on_error(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.LSLString"))
                            FindLineNumbers("on_error", "Invalid argument");
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("on_error", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_remote_data("))
            {
                int charNum = script.IndexOf(state + "_event_remote_data(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0 || i == 4)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("remote_data", "Invalid argument");
                    if (i == 1 || i == 2 || i == 3 || i == 5)
                        if (!argument.Contains("LSL_Types.LSLString"))
                            FindLineNumbers("remote_data", "Invalid argument");
                    i++;
                }
                if (i != 6)
                {
                    FindLineNumbers("remote_data", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_sensor("))
            {
                int charNum = script.IndexOf(state + "_event_sensor(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("sensor", "Invalid argument");
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("sensor", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_state_exit("))
            {
                int charNum = script.IndexOf(state + "_event_state_exit(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string Valid = state + "_event_state_exit()";
                if (splitScript != Valid)
                {
                    FindLineNumbers("state_exit", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_touch("))
            {
                int charNum = script.IndexOf(state + "_event_touch(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("touch", "Invalid argument");
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("touch", "Too many arguments");
                }
            }
            if (script.Contains(state + "_event_touch_end("))
            {
                int charNum = script.IndexOf(state + "_event_touch_end(");
                string splitScript = script.Remove(0, charNum);
                charNum = splitScript.IndexOf('\n');
                splitScript = splitScript.Remove(charNum, splitScript.Length - charNum);

                string arguments = splitScript.Split('(')[1];
                arguments = arguments.Split(')')[0];

                string[] AllArguments = arguments.Split(',');

                int i = 0;
                foreach (string argument in AllArguments)
                {
                    if (i == 0)
                        if (!argument.Contains("LSL_Types.LSLInteger"))
                            FindLineNumbers("touch_end", "Invalid argument");
                    i++;
                }
                if (i != 1)
                {
                    FindLineNumbers("touch_end", "Too many arguments");
                }
            }
        }

        void FindLineNumbers(string EventName, string Problem)
        {
            //string testScript = OriginalScript.Replace(" ", "");
            int lineNumber = 0;
            int charNumber = 0;
            int i = 0;
            foreach (string str in OriginalScript.Split('\n'))
            {
                if (str.Contains(EventName + "("))
                {
                    lineNumber = i;
                    charNumber = str.IndexOf(EventName);
                    break;
                }
                i++;
            }
            throw new Exception(String.Format("({0},{1}) {2}",
                lineNumber,
                charNumber, Problem + " in '" + EventName + "'\n"));
        }

        private void AddWarning(string warning)
        {
            m_compiler.AddWarning(warning);
        }

        /// <summary>
        /// Recursively called to generate each type of node. Will generate this
        /// node, then all it's children.
        /// </summary>
        /// <param name="s">The current node to generate code for.</param>
        /// <returns>String containing C# code for SYMBOL s.</returns>
        private string GenerateNode(SYMBOL s)
        {
            string fullretstr = "";

            // make sure to put type lower in the inheritance hierarchy first
            // ie: since IdentArgument and ExpressionArgument inherit from
            // Argument, put IdentArgument and ExpressionArgument before Argument
            if (s is GlobalFunctionDefinition)
            {
                fullretstr += GenerateGlobalFunctionDefinition((GlobalFunctionDefinition)s);
            }
            else if (s is GlobalVariableDeclaration)
            {
                fullretstr += GenerateGlobalVariableDeclaration((GlobalVariableDeclaration)s);
            }
            else if (s is State)
            {
                fullretstr += GenerateState((State)s);
            }
            else if (s is CompoundStatement)
            {
                fullretstr += GenerateCompoundStatement((CompoundStatement)s);
            }
            else if (s is Declaration)
            {
                fullretstr += GenerateDeclaration((Declaration)s);
            }
            else if (s is Statement)
            {
                fullretstr += GenerateStatement((Statement)s);
            }
            else if (s is ReturnStatement)
            {
                fullretstr += GenerateReturnStatement((ReturnStatement)s);
            }
            else if (s is JumpLabel)
            {
                fullretstr += GenerateJumpLabel((JumpLabel)s);
            }
            else if (s is JumpStatement)
            {
                fullretstr += GenerateJumpStatement((JumpStatement)s);
            }
            else if (s is StateChange)
            {
                fullretstr += GenerateStateChange((StateChange)s);
            }
            else if (s is IfStatement)
            {
                fullretstr += GenerateIfStatement((IfStatement)s);
            }
            else if (s is WhileStatement)
            {
                fullretstr += GenerateWhileStatement((WhileStatement)s);
            }
            else if (s is DoWhileStatement)
            {
                fullretstr += GenerateDoWhileStatement((DoWhileStatement)s);
            }
            else if (s is ForLoop)
            {
                fullretstr += GenerateForLoop((ForLoop)s);
            }
            else if (s is ArgumentList)
            {
                fullretstr += GenerateArgumentList((ArgumentList)s);
            }
            else if (s is Assignment)
            {
                fullretstr += GenerateAssignment((Assignment)s);
            }
            else if (s is BinaryExpression)
            {
                fullretstr += GenerateBinaryExpression((BinaryExpression)s);
            }
            else if (s is ParenthesisExpression)
            {
                fullretstr += GenerateParenthesisExpression((ParenthesisExpression)s);
            }
            else if (s is UnaryExpression)
            {
                fullretstr += GenerateUnaryExpression((UnaryExpression)s);
            }
            else if (s is IncrementDecrementExpression)
            {
                fullretstr += GenerateIncrementDecrementExpression((IncrementDecrementExpression)s);
            }
            else if (s is TypecastExpression)
            {
                fullretstr += GenerateTypecastExpression((TypecastExpression)s);
            }
            else if (s is FunctionCall)
            {
                fullretstr += GenerateFunctionCall((FunctionCall)s,true);
            }
            else if (s is VectorConstant)
            {
                fullretstr += GenerateVectorConstant((VectorConstant)s);
            }
            else if (s is RotationConstant)
            {
                fullretstr += GenerateRotationConstant((RotationConstant)s);
            }
            else if (s is ListConstant)
            {
                fullretstr += GenerateListConstant((ListConstant)s);
            }
            else if (s is Constant)
            {
                fullretstr += GenerateConstant((Constant)s);
            }
            else if (s is IdentDotExpression)
            {
                fullretstr += Generate(CheckName(((IdentDotExpression)s).Name) + "." + ((IdentDotExpression)s).Member, s);
            }
            else if (s is IdentExpression)
            {
                fullretstr += Generate(CheckName(((IdentExpression)s).Name), s);
            }
            else if (s is IDENT)
            {
                fullretstr += Generate(CheckName(((TOKEN)s).yytext), s);
            }
            else
            {
                foreach (SYMBOL kid in s.kids)
                {
                    fullretstr += GenerateNode(kid);
                }
            }
            return fullretstr.ToString();
        }

        /// <summary>
        /// Generates the code for a GlobalFunctionDefinition node.
        /// </summary>
        /// <param name="gf">The GlobalFunctionDefinition node.</param>
        /// <returns>String containing C# code for GlobalFunctionDefinition gf.</returns>
        private string GenerateGlobalFunctionDefinition(GlobalFunctionDefinition gf)
        {
            MethodVariables.Clear();
            VariablesToRename.Clear();
            StringBuilder retstr = new StringBuilder();

            // we need to separate the argument declaration list from other kids
            List<SYMBOL> argumentDeclarationListKids = new List<SYMBOL>();
            List<SYMBOL> remainingKids = new List<SYMBOL>();


            foreach (SYMBOL kid in gf.kids)
                if (kid is ArgumentDeclarationList)
                    argumentDeclarationListKids.Add(kid);
                else
                    remainingKids.Add(kid);

            retstr.Append(GenerateIndented(String.Format("public {1} {0}(", CheckName(gf.Name), gf.ReturnType), gf));

            // print the state arguments, if any
            foreach (SYMBOL kid in argumentDeclarationListKids)
            {
                ArgumentDeclarationList ADL = (ArgumentDeclarationList)kid;
                retstr.Append(GenerateArgumentDeclarationList(ADL));
            }

            retstr.Append(GenerateLine(")"));

            retstr.Append(GenerateLine("{"));
            retstr.Append(GenerateLine("CheckSleep();"));

            foreach (SYMBOL kid in remainingKids)
                retstr.Append(GenerateNode(kid));

            retstr.Append(GenerateLine("}"));

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a GlobalVariableDeclaration node.
        /// </summary>
        /// <param name="gv">The GlobalVariableDeclaration node.</param>
        /// <returns>String containing C# code for GlobalVariableDeclaration gv.</returns>
        private string GenerateGlobalVariableDeclaration(GlobalVariableDeclaration gv)
        {
            string retstr = "";

            foreach (SYMBOL s in gv.kids)
            {
                retstr += Indent();
                retstr += "public ";
                if (s is Assignment)
                {
                    string innerretstr = "";

                    Assignment a = s as Assignment;
                    List<string> identifiers = new List<string>();

                    checkForMultipleAssignments(identifiers, a);

                    IsaGlobalVar = true;
                    string VarName = GenerateNode((SYMBOL)a.kids.Pop());
                    innerretstr += VarName;
                    IsaGlobalVar = false;

                    #region Find the var name and type

                    string[] vars = VarName.Split(' ');
                    string type = vars[0];
                    string varName = vars[1];
                    string globalVarValue = "";

                    #endregion

                    innerretstr += Generate(String.Format(" {0} ", a.AssignmentType), a);
                    foreach (SYMBOL kid in a.kids)
                    {
                        if (kid is Constant)
                        {
                            Constant c = kid as Constant;
                            // Supprt LSL's weird acceptance of floats with no trailing digits
                            // after the period. Turn float x = 10.; into float x = 10.0;
                            if ("LSL_Types.LSLFloat" == c.Type)
                            {
                                int dotIndex = c.Value.IndexOf('.') + 1;
                                if (0 < dotIndex && (dotIndex == c.Value.Length || !Char.IsDigit(c.Value[dotIndex])))
                                    globalVarValue = c.Value.Insert(dotIndex, "0");
                                globalVarValue = "new LSL_Types.LSLFloat(" + c.Value + ") ";
                            }
                            else if ("LSL_Types.LSLInteger" == c.Type)
                            {
                                globalVarValue = "new LSL_Types.LSLInteger(" + c.Value + ") ";
                            }
                            else if ("LSL_Types.LSLString" == c.Type)
                            {
                                globalVarValue = "new LSL_Types.LSLString(\"" + c.Value + "\") ";
                            }
                            if(globalVarValue == "")
                                globalVarValue = c.Value;

                            if (globalVarValue == null)
                                globalVarValue = GenerateNode(c);
                            if (GlobalVariables.ContainsKey(globalVarValue))
                            {
                                //Its an assignment to another global var!
                                //reset the global value to the other's value
                                GlobalVar var;
                                GlobalVariables.TryGetValue(globalVarValue, out var);
                                //Do one last additional test before we set it.
                                if (type == var.Type)
                                {
                                    globalVarValue = var.Value;
                                }
                                c.Value = globalVarValue;
                            }
                            innerretstr += globalVarValue;
                        }
                        else if (kid is IdentExpression)
                        {

                            IdentExpression c = kid as IdentExpression;
                            globalVarValue = c.Name;

                            if (GlobalVariables.ContainsKey(globalVarValue))
                            {
                                //Its an assignment to another global var!
                                //reset the global value to the other's value
                                GlobalVar var;
                                GlobalVariables.TryGetValue(globalVarValue, out var);
                                //Do one last additional test before we set it.
                                if (type == var.Type)
                                {
                                    globalVarValue = var.Value;
                                }
                            }

                            innerretstr += globalVarValue;
 
                        }
                        else
                            innerretstr += GenerateNode(kid);
                    }
                    GlobalVariables.Add(varName, new GlobalVar()
                        {
                            Type = type,
                            Value = globalVarValue
                        });

                    retstr += innerretstr.ToString();
                }
                else
                    retstr += GenerateNode(s);

                retstr += GenerateLine(";");
            }

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a State node.
        /// </summary>
        /// <param name="s">The State node.</param>
        /// <returns>String containing C# code for State s.</returns>
        private string GenerateState(State s)
        {
            string retstr = "";

            foreach (SYMBOL kid in s.kids)
                if (kid is StateEvent)
                    retstr += GenerateStateEvent((StateEvent)kid, s.Name);

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a StateEvent node.
        /// </summary>
        /// <param name="se">The StateEvent node.</param>
        /// <param name="parentStateName">The name of the parent state.</param>
        /// <returns>String containing C# code for StateEvent se.</returns>
        private string GenerateStateEvent(StateEvent se, string parentStateName)
        {
            StringBuilder retstr = new System.Text.StringBuilder();

            // we need to separate the argument declaration list from other kids
            List<SYMBOL> argumentDeclarationListKids = new List<SYMBOL>();
            List<SYMBOL> remainingKids = new List<SYMBOL>();

            MethodVariables.Clear();
            VariablesToRename.Clear();
            
            foreach (SYMBOL kid in se.kids)
                if (kid is ArgumentDeclarationList)
                    argumentDeclarationListKids.Add(kid);
                else
                    remainingKids.Add(kid);

            // "state" (function) declaration
            retstr.Append(GenerateIndented(String.Format("public void {0}_event_{1}(", parentStateName, se.Name), se));

            // print the state arguments, if any
            foreach (SYMBOL kid in argumentDeclarationListKids)
                retstr.Append(GenerateArgumentDeclarationList((ArgumentDeclarationList)kid));

            retstr.Append(GenerateLine(")"));

            foreach (SYMBOL kid in remainingKids)
                retstr.Append(GenerateNode(kid));

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for an ArgumentDeclarationList node.
        /// </summary>
        /// <param name="adl">The ArgumentDeclarationList node.</param>
        /// <returns>String containing C# code for ArgumentDeclarationList adl.</returns>
        private string GenerateArgumentDeclarationList(ArgumentDeclarationList adl)
        {
            string retstr = "";

            int comma = adl.kids.Count - 1; // tells us whether to print a comma

            foreach (Declaration d in adl.kids)
            {
                retstr += GenerateDeclaration(d);
//                retstr += Generate(String.Format("{0} {1}", d.Datatype, CheckName(d.Id)), d);
                if (0 < comma--)
                    retstr += Generate(", ");
            }

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for an ArgumentList node.
        /// </summary>
        /// <param name="al">The ArgumentList node.</param>
        /// <returns>String containing C# code for ArgumentList al.</returns>
        private string GenerateArgumentList(ArgumentList al)
        {
            string retstr = "";

            int comma = al.kids.Count - 1;  // tells us whether to print a comma

            foreach (SYMBOL s in al.kids)
            {
                retstr += GenerateNode(s);
                if (0 < comma--)
                    retstr += Generate(", ");
            }

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a CompoundStatement node.
        /// </summary>
        /// <param name="cs">The CompoundStatement node.</param>
        /// <returns>String containing C# code for CompoundStatement cs.</returns>
        private string GenerateCompoundStatement(CompoundStatement cs)
        {
            string  retstr = "";

            // opening brace
            retstr += GenerateIndentedLine("{");
            m_braceCount++;

            foreach (SYMBOL kid in cs.kids)
                retstr += GenerateNode(kid);

            // closing brace
            m_braceCount--;

            retstr += GenerateIndentedLine("}");

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a Declaration node.
        /// </summary>
        /// <param name="d">The Declaration node.</param>
        /// <returns>String containing C# code for Declaration d.</returns>
        private string GenerateDeclaration(Declaration d)
        {

//        return Generate(String.Format("{0} {1}", d.Datatype, CheckName(d.Id)), d);

        GlobalVar var;
        if(IsaGlobalVar)
            return Generate(String.Format("{0} {1}", d.Datatype, CheckName(d.Id)), d);

        if (GlobalVariables.TryGetValue(d.Id, out var))
            return Generate(String.Format("{0} {1}", d.Datatype, CheckName(d.Id)), d);

            //Commented out because we can't handle the same var name in different if/else statements
        /*if (MethodVariables.TryGetValue(d.Id, out var))
            {
            if (var.Type != d.Datatype)
                {
                Console.WriteLine("[CSCodeGenerator]: found var needing renamed!");
                string NewVariableName = RandomString(10, true);
                VarRename r = new VarRename();
                r.OldVarName = d.Id;
                r.HasBeenAssigned = false;
                r.NewVarName = NewVariableName;
                VariablesToRename.Add(d.Id, r);
                d.Id = NewVariableName;
                MethodVariables.Add(d.Id, new GlobalVar() { Type = d.Datatype, Value = "" });
                return Generate(String.Format("{0} {1}", d.Datatype, CheckName(d.Id)), d);
                }
            else
                return Generate(String.Format("{0} {1}", d.Datatype, CheckName(d.Id)), d);
            }
        else
            {
            MethodVariables[d.Id] = new GlobalVar() { Type = d.Datatype, Value = "" };
            return Generate(String.Format("{0} {1}", d.Datatype, CheckName(d.Id)), d);
            }  
         * */

            return Generate(String.Format("{0} {1}", d.Datatype, CheckName(d.Id)), d);
        }

        /// <summary>
        /// Generates the code for a Statement node.
        /// </summary>
        /// <param name="s">The Statement node.</param>
        /// <returns>String containing C# code for Statement s.</returns>
        private string GenerateStatement(Statement s)
        {
            string retstr = "";

            bool printSemicolon = true;

            retstr += Indent();

            if (0 < s.kids.Count)
            {
                // Jump label prints its own colon, we don't need a semicolon.
                printSemicolon = !(s.kids.Top is JumpLabel);

                // If we encounter a lone Ident, we skip it, since that's a C#
                // (MONO) error.
                // If we encounter a lone Ident, we skip it, since that's a C#
                // (MONO) error.
                if (!(s.kids.Top is IdentExpression && 1 == s.kids.Count))
                    foreach (SYMBOL kid in s.kids)
                        retstr += GenerateNode(kid);
            }

            if (printSemicolon)
                retstr += GenerateLine(";");

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for an Assignment node.
        /// </summary>
        /// <param name="a">The Assignment node.</param>
        /// <returns>String containing C# code for Assignment a.</returns>
        private string GenerateAssignment(Assignment a)
        {
            string retstr = "";
            List<string> identifiers = new List<string>();

            checkForMultipleAssignments(identifiers, a);


            retstr += GenerateNode((SYMBOL)a.kids.Pop());
            retstr += Generate(String.Format(" {0} ", a.AssignmentType), a);
            foreach (SYMBOL kid in a.kids)
                retstr += GenerateNode(kid);

            return retstr.ToString();
        }

        // This code checks for LSL of the following forms, and generates a
        // warning if it finds them.
        //
        // list l = [ "foo" ]; 
        // l = (l=[]) + l + ["bar"];
        // (produces l=["foo","bar"] in SL but l=["bar"] in OS)
        //
        // integer i;
        // integer j;
        // i = (j = 3) + (j = 4) + (j = 5);
        // (produces j=3 in SL but j=5 in OS)
        //
        // Without this check, that code passes compilation, but does not do what
        // the end user expects, because LSL in SL evaluates right to left instead
        // of left to right.
        //
        // The theory here is that producing an error and alerting the end user that
        // something needs to change is better than silently generating incorrect code.
        private void checkForMultipleAssignments(List<string> identifiers, SYMBOL s)
        {
            if (s is Assignment)
            {
                Assignment a = (Assignment)s;
                string newident = null;

                if (a.kids[0] is Declaration)
                {
                    newident = ((Declaration)a.kids[0]).Id;
                }
                else if (a.kids[0] is IDENT)
                {
                    newident = ((IDENT)a.kids[0]).yytext;
                }
                else if (a.kids[0] is IdentDotExpression)
                {
                    newident = ((IdentDotExpression)a.kids[0]).Name; // +"." + ((IdentDotExpression)a.kids[0]).Member;
                }
                else
                {
                    AddWarning(String.Format("Multiple assignments checker internal error '{0}' at line {1} column {2}.", a.kids[0].GetType(), ((SYMBOL)a.kids[0]).Line - 1, ((SYMBOL)a.kids[0]).Position));
                }

                if (identifiers.Contains(newident))
                {
                    AddWarning(String.Format("Multiple assignments to '{0}' at line {1} column {2}; results may differ between LSL and OSSL.", newident, ((SYMBOL)a.kids[0]).Line - 1, ((SYMBOL)a.kids[0]).Position));
                }
                identifiers.Add(newident);
            }

            int index;
            for (index = 0; index < s.kids.Count; index++)
            {
                checkForMultipleAssignments(identifiers, (SYMBOL)s.kids[index]);
            }
        }

        /// <summary>
        /// Generates the code for a ReturnStatement node.
        /// </summary>
        /// <param name="rs">The ReturnStatement node.</param>
        /// <returns>String containing C# code for ReturnStatement rs.</returns>
        private string GenerateReturnStatement(ReturnStatement rs)
        {
            string retstr = "";

            retstr += Generate("return ", rs);

            foreach (SYMBOL kid in rs.kids)
                retstr += GenerateNode(kid);

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a JumpLabel node.
        /// </summary>
        /// <param name="jl">The JumpLabel node.</param>
        /// <returns>String containing C# code for JumpLabel jl.</returns>
        private string GenerateJumpLabel(JumpLabel jl)
        {
            return GenerateLine(Generate(String.Format("{0}:", CheckName(jl.LabelName)), jl) + " NoOp();");
        }

        /// <summary>
        /// Generates the code for a JumpStatement node.
        /// </summary>
        /// <param name="js">The JumpStatement node.</param>
        /// <returns>String containing C# code for JumpStatement js.</returns>
        private string GenerateJumpStatement(JumpStatement js)
        {
            return Generate(String.Format("goto {0}", CheckName(js.TargetName)), js);
        }

        /// <summary>
        /// Generates the code for an IfStatement node.
        /// </summary>
        /// <param name="ifs">The IfStatement node.</param>
        /// <returns>String containing C# code for IfStatement ifs.</returns>
        private string GenerateIfStatement(IfStatement ifs)
        {
            /*
             * Test script that was used to make sure that if statements do not fail
              integer a = 0;
integer test()
{
    a++;
    return a;
}
default
{
    state_entry()
    {
        if(test() == 0) //1 gets returned here
        {
            llSay(0, "Script running. 0");
        }
        else if(test() == 1) //2 gets returned here
        {
            llSay(0, "Script running. 2");
        }
        else
        {
            // 3 gets returned here
            if(test() == 4)
            {
                llSay(0, "Script running. 4");
            }
            else if(test() == 4)
            {
                // It should hit this path
                llSay(0, "Script running. 2 4");
            }
            else
            {
              // 5 would be returned here
                llSay(0, "Script running. else " + test());
            }
        }
    }
}*/
            string retstr = "";
            string tmpstr = "";
            bool DoBrace = false;
            tmpstr += GenerateIndented("if (", ifs);
            tmpstr += GenerateNode((SYMBOL)ifs.kids.Pop());
            tmpstr += GenerateLine(")");

            retstr += tmpstr.ToString();

            // CompoundStatement handles indentation itself but we need to do it
            // otherwise.

            // bool indentHere = ifs.kids.Top is Statement;
            // if (indentHere) m_braceCount++;
            DoBrace = !(ifs.kids.Top is CompoundStatement);
            if (DoBrace)
                retstr += GenerateLine("{");

            retstr += GenerateNode((SYMBOL)ifs.kids.Pop());
            // if (indentHere) m_braceCount--;
            if (DoBrace)
                retstr += GenerateLine("}");


            if (0 < ifs.kids.Count) // do it again for an else
            {
                retstr += GenerateIndentedLine("else", ifs);

                // indentHere = ifs.kids.Top is Statement;
                // if (indentHere) m_braceCount++;
                DoBrace = !(ifs.kids.Top is CompoundStatement);
                if (DoBrace)
                    retstr += GenerateLine("{");

                retstr += GenerateNode((SYMBOL)ifs.kids.Pop());

                if (DoBrace)
                    retstr += GenerateLine("}");


                // if (indentHere) m_braceCount--;
            }

            return retstr.ToString();
        }

        private string GenerateInnerIfStatement(SYMBOL child)
        {
            string tmpstr = "";
            if (child is CompoundStatement)
            {
                CompoundStatement cs = child as CompoundStatement;
                string innerretstr = "";

                // opening brace
                innerretstr += GenerateIndentedLine("{");
                //            if (IsParentEnumerable)
                //                retstr += GenerateLine("if (CheckSlice()) yield return null;");
                m_braceCount++;

                foreach (SYMBOL kid in cs.kids)
                {
                    if (kid is Statement)
                    {
                        Statement state = kid as Statement;
                        bool printSemicolon = true;

                        innerretstr += Indent();

                        if (0 < state.kids.Count)
                        {
                            // Jump label prints its own colon, we don't need a semicolon.
                            printSemicolon = !(state.kids.Top is JumpLabel);

                            // If we encounter a lone Ident, we skip it, since that's a C#
                            // (MONO) error.
                            if (!(state.kids.Top is IdentExpression && 1 == state.kids.Count))
                            {
                                foreach (SYMBOL stateKid in state.kids)
                                {
                                    if (stateKid is FunctionCallExpression)
                                    {
                                        foreach (SYMBOL akid in stateKid.kids)
                                        {
                                            if (akid is FunctionCall)
                                            {
                                                string innerinnerretStr = "";
                                                innerinnerretStr += GenerateFunctionCall(akid as FunctionCall, false);
                                                innerretstr += innerinnerretStr;
                                            }
                                            else
                                                innerretstr += GenerateNode(akid);
                                        }
                                    }
                                    else
                                        innerretstr += GenerateNode(stateKid);
                                }
                            }
                        }


                        //Nasty hack to fix if statements with yield return and yield break;
                        if (innerretstr[innerretstr.Length - 1] == '}')
                            printSemicolon = false;

                        if (printSemicolon)
                            innerretstr += GenerateLine(";");
                    }
                    else
                        innerretstr += GenerateNode(kid);
                }

                // closing brace
                m_braceCount--;

                innerretstr += GenerateIndentedLine("}");
                tmpstr += innerretstr;
            }
            else
                tmpstr += GenerateNode(child);
            return tmpstr;
        }

        /// <summary>
        /// Generates the code for a StateChange node.
        /// </summary>
        /// <param name="sc">The StateChange node.</param>
        /// <returns>String containing C# code for StateChange sc.</returns>
        private string GenerateStateChange(StateChange sc)
        {
            //State is in the LSL_Api because it requires a ref to the ScriptEngine, which we can't have in the ScriptBase
            string retStr = GenerateLine("try", null);
            retStr += GenerateLine("{", null);
            retStr += Generate(
                 String.Format("((ILSL_Api)m_apis[\"ll\"]).state(\"{0}\");", sc.NewState)
                 , sc);
            retStr += GenerateLine("}", null);
            retStr += GenerateLine("catch", null);
            retStr += GenerateLine("{", null);
            retStr += GenerateLine("}", null);
            return retStr;
        }

        /// <summary>
        /// Generates the code for a WhileStatement node.
        /// </summary>
        /// <param name="ws">The WhileStatement node.</param>
        /// <returns>String containing C# code for WhileStatement ws.</returns>
        private string GenerateWhileStatement(WhileStatement ws)
        {
            string retstr = "";

            retstr += GenerateIndented("while (", ws);
            retstr += GenerateNode((SYMBOL)ws.kids.Pop());
            retstr += GenerateLine(")");

            retstr += GenerateLine("{");
            retstr += GenerateLine("CheckSleep();");

            // CompoundStatement handles indentation itself but we need to do it
            // otherwise.
            bool indentHere = ws.kids.Top is Statement;
            if (indentHere) m_braceCount++;
                retstr += GenerateNode((SYMBOL)ws.kids.Pop());
            if (indentHere) m_braceCount--;

            retstr += GenerateLine("}");

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a DoWhileStatement node.
        /// </summary>
        /// <param name="dws">The DoWhileStatement node.</param>
        /// <returns>String containing C# code for DoWhileStatement dws.</returns>
        private string GenerateDoWhileStatement(DoWhileStatement dws)
        {
            string retstr = "";
            string tmpstr = "";

            tmpstr += GenerateIndentedLine("do", dws);

            retstr += GenerateLine("{");
            retstr += GenerateLine("CheckSleep();");
            // CompoundStatement handles indentation itself but we need to do it
            // otherwise.
            bool indentHere = dws.kids.Top is Statement;
            if (indentHere) m_braceCount++;
            tmpstr += GenerateNode((SYMBOL)dws.kids.Pop());
            if (indentHere) m_braceCount--;

            retstr += GenerateLine("}");

            //Forces all functions to use MoveNext() instead of .Current, as it never changes otherwise, and the loop runs infinitely

            tmpstr += GenerateIndented("while (", dws);
            tmpstr += GenerateNode((SYMBOL)dws.kids.Pop());
            tmpstr += GenerateLine(");");

            retstr += tmpstr.ToString();

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a ForLoop node.
        /// </summary>
        /// <param name="fl">The ForLoop node.</param>
        /// <returns>String containing C# code for ForLoop fl.</returns>
        private string GenerateForLoop(ForLoop fl)
        {
            string retstr = "";
            string tmpstr = "";

            tmpstr += GenerateIndented("for (", fl);

            // It's possible that we don't have an assignment, in which case
            // the child will be null and we only print the semicolon.
            // for (x = 0; x < 10; x++)
            //      ^^^^^
            ForLoopStatement s = (ForLoopStatement)fl.kids.Pop();
            if (null != s)
            {
                tmpstr += GenerateForLoopStatement(s);
            }
            tmpstr += Generate("; ");
            // for (x = 0; x < 10; x++)
            //             ^^^^^^
            tmpstr += GenerateNode((SYMBOL)fl.kids.Pop());
            tmpstr += Generate("; ");
            // for (x = 0; x < 10; x++)
            //                     ^^^
            tmpstr += GenerateForLoopStatement((ForLoopStatement)fl.kids.Pop());
            tmpstr += GenerateLine(")");

            retstr += tmpstr.ToString();

            // CompoundStatement handles indentation itself but we need to do it
            // otherwise.
            bool indentHere = fl.kids.Top is Statement;
            if (indentHere) m_braceCount++;
            retstr += GenerateNode((SYMBOL)fl.kids.Pop());
            if (indentHere) m_braceCount--;

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a ForLoopStatement node.
        /// </summary>
        /// <param name="fls">The ForLoopStatement node.</param>
        /// <returns>String containing C# code for ForLoopStatement fls.</returns>
        private string GenerateForLoopStatement(ForLoopStatement fls)
        {
            string retstr = "";

            int comma = fls.kids.Count - 1;  // tells us whether to print a comma

            // It's possible that all we have is an empty Ident, for example:
            //
            //     for (x; x < 10; x++) { ... }
            //
            // Which is illegal in C# (MONO). We'll skip it.
            if (fls.kids.Top is IdentExpression && 1 == fls.kids.Count)
                return retstr.ToString();

            for (int i = 0; i < fls.kids.Count; i++)
            {
                SYMBOL s = (SYMBOL)fls.kids[i];

                // Statements surrounded by parentheses in for loops
                //
                // e.g.  for ((i = 0), (j = 7); (i < 10); (++i))
                //
                // are legal in LSL but not in C# so we need to discard the parentheses
                //
                // The following, however, does not appear to be legal in LLS
                //
                // for ((i = 0, j = 7); (i < 10); (++i))
                //
                // As of Friday 20th November 2009, the Linden Lab simulators appear simply never to compile or run this
                // script but with no debug or warnings at all!  Therefore, we won't deal with this yet (which looks
                // like it would be considerably more complicated to handle).
                while (s is ParenthesisExpression)
                    s = (SYMBOL)s.kids.Pop();

                retstr += GenerateNode(s);
                if (0 < comma--)
                    retstr += Generate(", ");
            }

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a BinaryExpression node.
        /// </summary>
        /// <param name="be">The BinaryExpression node.</param>
        /// <returns>String containing C# code for BinaryExpression be.</returns>
        private string GenerateBinaryExpression(BinaryExpression be)
        {
            string retstr = "";

            if (be.ExpressionSymbol.Equals("&&") || be.ExpressionSymbol.Equals("||"))
            {
                // special case handling for logical and/or, see Mantis 3174
                retstr += "((bool)(";
                retstr += GenerateNode((SYMBOL)be.kids.Pop());
                retstr += "))";
                retstr += Generate(String.Format(" {0} ", be.ExpressionSymbol.Substring(0, 1)), be);
                retstr += "((bool)(";
                foreach (SYMBOL kid in be.kids)
                    retstr += GenerateNode(kid);
                retstr += "))";
            }
            else
            {
                retstr += GenerateNode((SYMBOL)be.kids.Pop());
                retstr += Generate(String.Format(" {0} ", be.ExpressionSymbol), be);
                foreach (SYMBOL kid in be.kids)
                    retstr += GenerateNode(kid);
            }

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a UnaryExpression node.
        /// </summary>
        /// <param name="ue">The UnaryExpression node.</param>
        /// <returns>String containing C# code for UnaryExpression ue.</returns>
        private string GenerateUnaryExpression(UnaryExpression ue)
        {
            string retstr = "";

            retstr += Generate(ue.UnarySymbol, ue);
            retstr += GenerateNode((SYMBOL)ue.kids.Pop());

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a ParenthesisExpression node.
        /// </summary>
        /// <param name="pe">The ParenthesisExpression node.</param>
        /// <returns>String containing C# code for ParenthesisExpression pe.</returns>
        private string GenerateParenthesisExpression(ParenthesisExpression pe)
        {
            string retstr = "";

            retstr += Generate("(");
            foreach (SYMBOL kid in pe.kids)
                retstr += GenerateNode(kid);
            retstr += Generate(")");

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a IncrementDecrementExpression node.
        /// </summary>
        /// <param name="ide">The IncrementDecrementExpression node.</param>
        /// <returns>String containing C# code for IncrementDecrementExpression ide.</returns>
        private string GenerateIncrementDecrementExpression(IncrementDecrementExpression ide)
        {
            string retstr = "";

            if (0 < ide.kids.Count)
            {
                IdentDotExpression dot = (IdentDotExpression)ide.kids.Top;
                retstr += Generate(String.Format("{0}", ide.PostOperation ? CheckName(dot.Name) + "." + dot.Member + ide.Operation : ide.Operation + CheckName(dot.Name) + "." + dot.Member), ide);
            }
            else
                retstr += Generate(String.Format("{0}", ide.PostOperation ? CheckName(ide.Name) + ide.Operation : ide.Operation + CheckName(ide.Name)), ide);

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a TypecastExpression node.
        /// </summary>
        /// <param name="te">The TypecastExpression node.</param>
        /// <returns>String containing C# code for TypecastExpression te.</returns>
        private string GenerateTypecastExpression(TypecastExpression te)
        {
            string retstr = "";

            // we wrap all typecasted statements in parentheses
            retstr += Generate(String.Format("({0}) (", te.TypecastType), te);
            retstr += GenerateNode((SYMBOL)te.kids.Pop());
            retstr += Generate(")");

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a FunctionCall node.
        /// </summary>
        /// <param name="fc">The FunctionCall node.</param>
        /// <returns>String containing C# code for FunctionCall fc.</returns>
        private string GenerateFunctionCall(FunctionCall fc, bool NeedRetVal)
        {
            string retstr = String.Empty;
            //Check whether this function is an API function
            if (m_apiFunctions.ContainsKey(CheckName(fc.Id)))
            {
                //Add the m_apis link
                fc.Id = String.Format("(({0})m_apis[\"{1}\"]).{2}",
                    m_apiFunctions[CheckName(fc.Id)].InterfaceName,
                    m_apiFunctions[CheckName(fc.Id)].Name, fc.Id);
            }
            retstr += Generate(String.Format("{0}(", CheckName(fc.Id)), fc);

            foreach (SYMBOL kid in fc.kids)
                retstr += GenerateNode(kid);

            retstr += Generate(")");

            return retstr;
        }

        /// <summary>
        /// Generates the code for a Constant node.
        /// </summary>
        /// <param name="c">The Constant node.</param>
        /// <returns>String containing C# code for Constant c.</returns>
        private string GenerateConstant(Constant c)
        {
            string retstr = "";

            // Supprt LSL's weird acceptance of floats with no trailing digits
            // after the period. Turn float x = 10.; into float x = 10.0;
            if ("LSL_Types.LSLFloat" == c.Type)
            {
                int dotIndex = c.Value.IndexOf('.') + 1;
                if (0 < dotIndex && (dotIndex == c.Value.Length || !Char.IsDigit(c.Value[dotIndex])))
                    c.Value = c.Value.Insert(dotIndex, "0");
                c.Value = "new LSL_Types.LSLFloat(" + c.Value + ")";
            }
            else if ("LSL_Types.LSLInteger" == c.Type)
            {
                c.Value = "new LSL_Types.LSLInteger(" + c.Value + ")";
            }
            else if ("LSL_Types.LSLString" == c.Type)
            {
                c.Value = "new LSL_Types.LSLString(\"" + c.Value + "\")";
            }

            retstr += Generate(c.Value, c);

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a VectorConstant node.
        /// </summary>
        /// <param name="vc">The VectorConstant node.</param>
        /// <returns>String containing C# code for VectorConstant vc.</returns>
        private string GenerateVectorConstant(VectorConstant vc)
        {
            string retstr = "";

            retstr += Generate(String.Format("new {0}(", vc.Type), vc);
            retstr += GenerateNode((SYMBOL)vc.kids.Pop());
            retstr += Generate(", ");
            retstr += GenerateNode((SYMBOL)vc.kids.Pop());
            retstr += Generate(", ");
            retstr += GenerateNode((SYMBOL)vc.kids.Pop());
            retstr += Generate(")");

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a RotationConstant node.
        /// </summary>
        /// <param name="rc">The RotationConstant node.</param>
        /// <returns>String containing C# code for RotationConstant rc.</returns>
        private string GenerateRotationConstant(RotationConstant rc)
        {
            string retstr = "";

            retstr += Generate(String.Format("new {0}(", rc.Type), rc);
            retstr += GenerateNode((SYMBOL)rc.kids.Pop());
            retstr += Generate(", ");
            retstr += GenerateNode((SYMBOL)rc.kids.Pop());
            retstr += Generate(", ");
            retstr += GenerateNode((SYMBOL)rc.kids.Pop());
            retstr += Generate(", ");
            retstr += GenerateNode((SYMBOL)rc.kids.Pop());
            retstr += Generate(")");

            return retstr.ToString();
        }

        /// <summary>
        /// Generates the code for a ListConstant node.
        /// </summary>
        /// <param name="lc">The ListConstant node.</param>
        /// <returns>String containing C# code for ListConstant lc.</returns>
        private string GenerateListConstant(ListConstant lc)
        {
            string retstr = "";

            retstr += Generate(String.Format("new {0}(", lc.Type), lc);

            foreach (SYMBOL kid in lc.kids)
                retstr += GenerateNode(kid);

            retstr += Generate(")");

            return retstr.ToString();
        }

        /// <summary>
        /// Prints a newline.
        /// </summary>
        /// <returns>A newline.</returns>
        private string GenerateLine()
        {
            return GenerateLine("");
        }

        /// <summary>
        /// Prints text, followed by a newline.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <returns>String s followed by newline.</returns>
        private string GenerateLine(string s)
        {
            return GenerateLine(s, null);
        }

        /// <summary>
        /// Prints text, followed by a newline.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <param name="sym">Symbol being generated to extract original line
        /// number and column from.</param>
        /// <returns>String s followed by newline.</returns>
        private string GenerateLine(string s, SYMBOL sym)
        {
            string retstr = Generate(s, sym) + "\n";

            m_CSharpLine++;
            m_CSharpCol = 1;

            return retstr;
        }

        /// <summary>
        /// Prints text.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <returns>String s.</returns>
        private string Generate(string s)
        {
            return Generate(s, null);
        }

        /// <summary>
        /// Prints text.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <param name="sym">Symbol being generated to extract original line
        /// number and column from.</param>
        /// <returns>String s.</returns>
        private string Generate(string s, SYMBOL sym)
        {
            if (null != sym)
                m_positionMap.Add(new KeyValuePair<int, int>(m_CSharpLine, m_CSharpCol), new KeyValuePair<int, int>(sym.Line, sym.Position));

            m_CSharpCol += s.Length;

            return s;
        }

        /// <summary>
        /// Prints text correctly indented, followed by a newline.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <returns>Properly indented string s followed by newline.</returns>
        private string GenerateIndentedLine(string s)
        {
            return GenerateIndentedLine(s, null);
        }

        /// <summary>
        /// Prints text correctly indented, followed by a newline.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <param name="sym">Symbol being generated to extract original line
        /// number and column from.</param>
        /// <returns>Properly indented string s followed by newline.</returns>
        private string GenerateIndentedLine(string s, SYMBOL sym)
        {
            string retstr = GenerateIndented(s, sym) + "\n";

            m_CSharpLine++;
            m_CSharpCol = 1;

            return retstr;
        }

        /// <summary>
        /// Prints text correctly indented.
        /// </summary>
        /// <param name="s">String of text to print.</param>
        /// <param name="sym">Symbol being generated to extract original line
        /// number and column from.</param>
        /// <returns>Properly indented string s.</returns>
        private string GenerateIndented(string s, SYMBOL sym)
        {
            string retstr = Indent() + s;

            if (null != sym)
                m_positionMap.Add(new KeyValuePair<int, int>(m_CSharpLine, m_CSharpCol), new KeyValuePair<int, int>(sym.Line, sym.Position));

            m_CSharpCol += s.Length;

            return retstr;
        }

        /// <summary>
        /// Prints correct indentation.
        /// </summary>
        /// <returns>Indentation based on brace count.</returns>
        private string Indent()
        {
            string retstr = String.Empty;

            for (int i = 0; i < m_braceCount; i++)
                for (int j = 0; j < m_indentWidth; j++)
                {
                    retstr += " ";
                    m_CSharpCol++;
                }

            return retstr;
        }

        /// <summary>
        /// Returns the passed name with an underscore prepended if that name is a reserved word in C#
        /// and not resevered in LSL otherwise it just returns the passed name.
        ///
        /// This makes no attempt to cache the results to minimise future lookups. For a non trivial
        /// scripts the number of unique identifiers could easily grow to the size of the reserved word
        /// list so maintaining a list or dictionary and doing the lookup there firstwould probably not
        /// give any real speed advantage.
        ///
        /// I believe there is a class Microsoft.CSharp.CSharpCodeProvider that has a function
        /// CreateValidIdentifier(str) that will return either the value of str if it is not a C#
        /// key word or "_"+str if it is. But availability under Mono?
        /// </summary>
        private string CheckName(string s)
        {
            if (CSReservedWords.IsReservedWord(s))
                return "@" + s;
            else
            {
                /*VarRename var;
                if(VariablesToRename.TryGetValue(s, out var))
                {
                    Console.WriteLine("[CSCodeGenerator]: found var needing renamed!");
                    if (var.HasBeenAssigned)
                        s = var.NewVarName;
                    else
                    {
                        s = var.OldVarName;
                        var.HasBeenAssigned = true;
                        VariablesToRename[s] = var;
                    }
                }*/
                return s;
            }
        }

        /// From http://www.c-sharpcorner.com/UploadFile/mahesh/RandomNumber11232005010428AM/RandomNumber.aspx
        /// <summary>
        /// Generates a random string with the given length
        /// </summary>
        /// <param name="size">Size of the string</param>
        /// <param name="lowerCase">If true, generate lowercase string</param>
        /// <returns>Random string</returns>
        private string RandomString(int size, bool lowerCase)
        {
            string builder = "t";
            int off = lowerCase ? 'a' : 'A';
            int j;
            for (int i = 0; i < size; i++)
            {
                j = random.Next(25);
                builder += (char)(j + off);
            }

            return builder;
        }

        #region IDisposable Members

        public void Dispose()
        {
            ResetCounters();
        }

        #endregion
    }
}
