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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CSharp;
//using Microsoft.JScript;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools
{
    public class ParseException : Exception
    {
        public ParseException(string message) :
            base(message)
        {
        }
    }

    public class LSL2Converter : IScriptConverter
    {
        private readonly CSharpCodeProvider CScodeProvider = new CSharpCodeProvider();
        private Compiler m_compiler;
        private List<string> DTFunctions;

        #region Event Listing

        private List<string> Events = new List<string>(new[]
            {
                "at_rot_target",
                "at_target",
                "attach",
                "changed",
                "collision",
                "collision_end",
                "collision_start",
                "control",
                "dataserver",
                "email",
                "http_request",
                "http_response",
                "land_collision",
                "land_collision_end",
                "land_collision_start",
                "link_message",
                "listen",
                "money",
                "moving_end",
                "moving_start",
                "no_sensor",
                "not_at_rot_target",
                "not_at_target",
                "object_rez",
                "on_rez",
                "remote_data",
                "run_time_permissions",
                "sensor",
                "state_entry",
                "state_exit",
                "timer",
                "touch",
                "touch_start",
                "touch_end",
                "transaction_result"
            });

        #endregion

        #region IScriptConverter Members

        public string DefaultState
        {
            get { return "default"; }
        }

        public void Initialise(Compiler compiler)
        {
            m_compiler = compiler;
            DTFunctions = new List<string>();
            foreach (IScriptApi api in m_compiler.ScriptEngine.GetAPIs())
            {
                MethodInfo[] members = api.GetType().GetMethods();
                foreach (MethodInfo info in members)
                    if (info.ReturnType == typeof(DateTime))
                        if(!DTFunctions.Contains(info.Name))
                            DTFunctions.Add(info.Name);
            }
            
            bool success = RunTest1();
        }

        public bool RunTest1()
        {
            //Test Compile

            string script = @"//lsl2
a() { llSay(0, ""a - success""); }
string b()
{
    return ""b - success"";
}
default { state_entry() { vector a = <0,0,0>; vector b; llSay(0, ""Script running.""); } 
    touch_start(integer number)
    { 
        for(number = 0; number < 10; number++)
        {
        }
        llSay(0,""Touched.""); 
        llMessageLinked(-1, 0, ""c"", ""d"");
    }
    link_message(integer num, integer num2, string c, string d)
    {
        llSay(0, (string)num2 + "" "" + c + "" "" + d);
        a();
        llSay(0, b());
    }
}
state testing
{
    state_entry()
    {
        llSay(0, ""Script Start - Testing"");
    }
    touch_start(integer a)
    {
        llSay(0,""testing time!"");
        state default;
    }
}";
            string compiledScript;
            object map;
            Convert(script, out compiledScript, out map);
            CompilerParameters parameters = new CompilerParameters { IncludeDebugInformation = true };


            string rootPath =
                System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

            if (rootPath != null)
                parameters.ReferencedAssemblies.Add(System.IO.Path.Combine(rootPath,
                                                                 "Aurora.ScriptEngine.AuroraDotNetEngine.dll"));
            parameters.ReferencedAssemblies.Add("System.dll");
            if (rootPath != null)
            {
                parameters.ReferencedAssemblies.Add(System.IO.Path.Combine(rootPath,
                                                                 "OpenSim.Framework.dll"));
                parameters.ReferencedAssemblies.Add(System.IO.Path.Combine(rootPath,
                                                                 "OpenMetaverseTypes.dll"));
            }
            IScriptApi[] apis = m_compiler.ScriptEngine.GetAPIs();
            //Now we need to pull the files they will need to access from them
            foreach (IScriptApi api in apis)
                parameters.ReferencedAssemblies.AddRange(api.ReferencedAssemblies);
            CompilerResults results = Compile(parameters, false, compiledScript);
            if (results.Errors.Count > 0)
            {
                int LineN, CharN;
                FindErrorLine(results.Errors[0], map, script, out LineN, out CharN);
                return false;
            }
            return true;
        }

        public void Convert(string Script, out string CompiledScript,
                            out object PositionMap)
        {
            // Its LSL, convert it to C#
            Dictionary<int, int>  map = new Dictionary<int, int>();
            List<string> csClass = new List<string>();
            Script = Script.Replace("\n", "\r\n");

            string[] lineSplit = ConvertLSLTypes(Script);
            List<string> splits = new List<string>();
            List<string> breaksplits = new List<string>();
            foreach (string s in lineSplit)
            {
                splits.AddRange(s.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                breaksplits.AddRange(s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
            breaksplits.RemoveAll(RemoveR);
            string[] split = splits.ToArray();
            string[] breaksplit = breaksplits.ToArray();
            int i = 0;
            string currentState = "";
            bool InState = false;
            bool inMethod = false;
            int skipUntil = 0;
            int bracketInsideMethod = 0;
            foreach (string wword in split)
            {
                if (i < skipUntil)
                {
                    i++;
                    continue;
                }
                string word = wword.Replace("\r", "");
                if (word.StartsWith("//"))
                {
                    GetUntilBreak(split, breaksplit, i, out skipUntil);
                    i++;
                    continue;
                }
                else if (word == "default")
                {
                    currentState = "default";
                    InState = true;
                }
                else if (word == "state")
                {
                    if (inMethod)
                    {
                        currentState = GetUntilBreak(split, breaksplit, i, out skipUntil).Replace(";", "").Trim();
                        AddToClass(csClass, string.Format(
                            "((ILSL_Api)m_apis[\"ll\"]).state(\"{0}\");",
                            currentState), split, breaksplit, lineSplit, i, ref map); 
                    }
                    else
                    {
                        currentState = GetNextWord(split, i);
                        skipUntil = i + 2;
                        InState = true;
                    }
                }
                else if (word.IndexOf("(") != -1 &&
                    Events.Contains(word.Substring(0, word.IndexOf("("))))
                {
                    if (!InState)
                        throw new ParseException("Event is not in a state");
                    else
                    {
                        AddToClass(csClass, GenerateEvent(currentState, split, breaksplit, i, out skipUntil),
                            split, breaksplit, lineSplit, i, ref map);
                        inMethod = true;
                    }
                }
                else if (word == "{" || word == "}")
                {
                    if(!(word == "{" && bracketInsideMethod == 0 && InState) &&
                        !(word == "}" && bracketInsideMethod == 1 && InState))
                        AddToClass(csClass, word,
                            split, breaksplit, lineSplit, i, ref map);
                    if (word == "{")
                        bracketInsideMethod++;
                    else
                    {
                        bracketInsideMethod--;
                        if (bracketInsideMethod == (InState ? 1 : 0))
                            inMethod = false;
                        if (bracketInsideMethod == 0)
                            InState = false;
                    }
                }
                else if (inMethod)
                {
                    bool wasSemicolan = false;
                    string csLine = split[i].StartsWith("for") ?
                        GetUntilBreak(split, breaksplit, i, out skipUntil)
                        :
                        GetUntilSemicolanOrBreak(split, breaksplit, i, out skipUntil, out wasSemicolan);
                    AddDefaultInitializers(ref csLine);
                    AddToClass(csClass, csLine,
                            split, breaksplit, lineSplit, i, ref map);
                }
                else if (!InState)
                {
                    bool wasSemicolan;
                    string line = GetUntil(split, ";", ")", i, out skipUntil, out wasSemicolan);
                    if (!wasSemicolan)
                    {
                        int spaceCount = line.Substring(0, line.IndexOf('(')).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length - 1;
                        if (spaceCount == 0)
                            line = "public void " + line;
                        else
                            line = "public " + line;
                        inMethod = true;
                    }
                    else
                        AddDefaultInitializers(ref line);

                    AddToClass(csClass, line,
                            split, breaksplit, lineSplit, i, ref map);
                }
                else
                {
                    AddToClass(csClass, "fake", split, breaksplit, lineSplit, i, ref map);
                    m_compiler.AddError(String.Format("({0},{1}): {3}: {2}\n",
                                             map[csClass.Count-1], 1, "Invalid expression term '" + word + "'", "Error"));
                    CompiledScript = "";
                    PositionMap = null;
                    return;
                }
                i++;
            }

            PositionMap = map;
            CompiledScript = CSCodeGenerator.CreateCompilerScript(m_compiler, new List<string>(), string.Join("\n", csClass.ToArray()));
        }

        private void AddDefaultInitializers(ref string csLine)
        {
            if (csLine.StartsWith("LSL_Types.LSLInteger"))
            {
                if (csLine.IndexOf('=') == -1)
                    csLine = csLine.Remove(csLine.Length - 1) + " = 0;";
            }
            else if (csLine.StartsWith("LSL_Types.LSLFloat"))
            {
                if (csLine.IndexOf('=') == -1)
                    csLine = csLine.Remove(csLine.Length - 1) + " = 0.0;";
            }
            else if (csLine.StartsWith("LSL_Types.LSLString"))
            {
                if (csLine.IndexOf('=') == -1)
                    csLine = csLine.Remove(csLine.Length - 1) + " = \"\";";
            }
            else if (csLine.StartsWith("LSL_Types.Vector3"))
            {
                if (csLine.IndexOf('=') == -1)
                    csLine = csLine.Remove(csLine.Length - 2) + " = new LSL_Types.Vector3(0.0,0.0,0.0);";
            }
            else if (csLine.StartsWith("LSL_Types.Quaternion"))
            {
                if (csLine.IndexOf('=') == -1)
                    csLine = csLine.Remove(csLine.Length - 1) + " = new LSL_Types.Quaternion(0.0,0.0,0.0,1.0);";
            }
            else if (csLine.StartsWith("LSL_Types.list"))
            {
                if (csLine.IndexOf('=') == -1)
                    csLine = csLine.Remove(csLine.Length - 1) + " = new LSL_Types.list();";
            }
        }

        private void AddToClass(List<string> csline, string line, string[] split, 
            string[] breaksplit, string[] lineSplit, int i, ref Dictionary<int, int> PositionMap)
        {
            if (line != "{" && line != "}")
            {
                int end;
                bool wasSemi;
                int LSLLineNum = 0;
                string currentLSLLine = split[i] + GetUntilSemicolanOrBreak(split, breaksplit, i, out end, out wasSemi);
                currentLSLLine = currentLSLLine.Remove(currentLSLLine.Length - 1);//Trailing space
                for (int lineN = 0; lineN < lineSplit.Length; lineN++)
                {
                    string trimmed = lineSplit[lineN].Trim();
                    if (lineSplit[lineN].Trim().StartsWith(currentLSLLine))
                    {
                        LSLLineNum = lineN;
                        break;
                    }
                }
                PositionMap[csline.Count] = LSLLineNum;
            }
            csline.Add(line);
        }

        private string[] ConvertLSLTypes(string Script)
        {
            Script = Script.Replace("integer", "LSL_Types.LSLInteger");
            Script = Script.Replace("float", "LSL_Types.LSLFloat");
            Script = Script.Replace("key", "LSL_Types.LSLString");
            Script = Script.Replace("string", "LSL_Types.LSLString");
            Script = Script.Replace("vector", "LSL_Types.Vector3");
            Script = Script.Replace("rotation", "LSL_Types.Quaternion");
            Script = Script.Replace("list", "LSL_Types.list");
            string[] split = Script.Split('\n');
            Dictionary<string, IScriptApi> apiFunctions = m_compiler.ScriptEngine.GetAllFunctionNamesAPIs();
            for (int i = 0; i < split.Length; i++)
            {
                int subStr = 0;
                int startIndex = 0;
            checkAgain:
                if ((subStr = split[i].IndexOf("<", startIndex)) != -1)
                {
                    int endSubStr = split[i].IndexOf(">", startIndex);
                    if (endSubStr != -1)
                    {
                        string subString = split[i].Substring(subStr + 1, endSubStr - subStr - 1);
                        string[] values = subString.Split(',');
                        int commaCount = values.Length;
                        if (commaCount == 3)//Vector
                            split[i] = split[i].Replace("<" + subString + ">", "new LSL_Types.Vector3(" + subString + ")");
                        else if (commaCount == 4)//Rotation
                            split[i] = split[i].Replace("<" + subString + ">", "new LSL_Typers.Quaternion(" + subString + ")");
                        startIndex = endSubStr;
                        goto checkAgain;
                    }
                }
                if ((subStr = split[i].IndexOf("[", startIndex)) != -1)
                {
                    int endSubStr = split[i].IndexOf("]", startIndex);
                    string subString = split[i].Substring(subStr + 1, endSubStr - subStr - 1);
                    split[i] = split[i].Replace("[" + subString + "]", "new LSL_Types.list(" + subString + ")");
                    startIndex = endSubStr;
                    goto checkAgain;
                }
                
                foreach (KeyValuePair<string, IScriptApi> function in apiFunctions)
                {
                    string old = split[i];
                    if ((split[i] = split[i].Replace(function.Key,
                        String.Format("(({0})m_apis[\"{1}\"]).{2}",
                                          function.Value.InterfaceName,
                                          function.Value.Name, function.Key))) != old)
                        break;
                }
            }
            return split;
        }

        private bool RemoveR(string r)
        {
            return r.Replace("\r","") == "";
        }

        private string GenerateEvent(string currentState, string[] split, string[] breaksplit, int i, out int skipUntil)
        {
            return "public void " + currentState + "_event_" + GetUntil(split, ")", i, out skipUntil);
        }

        private string GetNextWord(string[] split, int i)
        {
            return split[i + 1];
        }

        private string GetUntilBreak(string[] split, string[] breaksplit, int i, out int end)
        {
            return GetUntil(breaksplit, "\r", i, out end);
        }

        private string GetUntil(string[] split, string character, int i, out int end)
        {
            string resp = "";
            while (true)
            {
                resp += split[i] + " ";
                if (split[i].EndsWith(character))
                    break;
                i++;
                if (i == split.Length)
                    throw new ParseException("Missing ';'");
            }
            end = ++i;
            return resp;
        }

        private string GetUntil(string[] split, string a, string b, int i, out int end, out bool wasA)
        {
            int aEnd = int.MaxValue, bEnd = int.MaxValue;
            string aa = "", bb = "";
            try
            {
                aa = GetUntil(split, a, i, out aEnd);
            }
            catch { }
            try
            {
                bb = GetUntil(split, b, i, out bEnd);
            }
            catch { }
            end = aEnd < bEnd ? aEnd : bEnd;
            wasA = aEnd < bEnd;
            return aEnd < bEnd ? aa : bb;
        }

        private string GetUntilSemicolan(string[] split, int i, out int end)
        {
            return GetUntil(split, ";", i, out end);
        }

        private string GetUntilSemicolanOrBreak(string[] split, string[] breaksplit, int i, out int end, out bool wasSemicolan)
        {
            int semiEnd = int.MaxValue, breakEnd = int.MaxValue;
            string semi = "", brk = "";
            try
            {
                brk = GetUntilBreak(split, breaksplit, i, out breakEnd);
            }
            catch { }
            try
            {
                semi = GetUntilSemicolan(split, i, out semiEnd);
            }
            catch { }
            end = semiEnd < breakEnd ? semiEnd : breakEnd;
            wasSemicolan = semiEnd < breakEnd;
            return semiEnd < breakEnd ? semi : brk;
        }

        public string Name
        {
            get { return "lsl2"; }
        }

        public CompilerResults Compile(CompilerParameters parameters, bool isFile, string Script)
        {
            bool complete = false;
            bool retried = false;
            CompilerResults results;
            do
            {
                lock (CScodeProvider)
                {
                    if (isFile)
                        results = CScodeProvider.CompileAssemblyFromFile(
                            parameters, Script);
                    else
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
                    if (!retried && string.IsNullOrEmpty(results.Errors[0].FileName) &&
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

        public void FinishCompile(IScriptModulePlugin plugin, ScriptData data, IScript Script)
        {
        }

        public void FindErrorLine(CompilerError CompErr, object PositionMap, string script, out int LineN, out int CharN)
        {
            Dictionary<int, int> PositionMapp = (Dictionary<int, int>)PositionMap;
            LineN = CompErr.Line - CSCodeGenerator.GetHeaderCount(m_compiler);
            CharN = 1;
            LineN = PositionMapp[LineN - 1];//LSL is zero based, so subtract one
        }

        #endregion

        public void Dispose()
        {
        }
    }
}