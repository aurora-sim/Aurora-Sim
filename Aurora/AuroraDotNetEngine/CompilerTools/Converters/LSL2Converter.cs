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
using System.Text.RegularExpressions;
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
        private string m_functionRegex = "";
        private Dictionary<string, IScriptApi> m_scriptApis;

        #region Listings

        private List<string> ProtectedCalls = new List<string>(new[]
            {
                "for",
                "while",
                "do"
            });
        private List<string> CalledBeforeProtectedCalls = new List<string>(new[]
            {
                "goto"
            });

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
                "path_update",
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
                        if (!DTFunctions.Contains(info.Name))
                            DTFunctions.Add(info.Name);
            }
            m_scriptApis = m_compiler.ScriptEngine.GetAllFunctionNamesAPIs();
            List<string> functionKeys = new List<string>(m_scriptApis.Keys);
            functionKeys = Aurora.Framework.StringUtils.SizeSort(functionKeys, false);
            foreach (string function in m_scriptApis.Keys)
                m_functionRegex += function + "|";
            m_functionRegex = m_functionRegex.Remove(m_functionRegex.Length - 1);
            //bool success = RunTest1();
        }

        #region Tests

        public bool RunTest1()
        {
            //Test Compile

            string script = @"//lsl2
a() { llSay(0, ""a - success""); }
string b()
{
    vector a = <0,0,0>;
    float b = a.x ;
    return ""b - success"";
}
default { state_entry() { vector a = <0,0,0.04>; vector b; llSay(0, ""Script running.  ""); } 
    touch_start(integer number)
    { 
test:
// doing some testing.
        llSay(0, ""doing more testing //. "");
        /* testing. */
        llSay(0, ""testing some shit /*.*/"");
        for(number = 0; number < 10; number++)
        {
        }
        for(number = 0; number < 10; number++)
            llSay(0,""kill me!"");
        llSay(0,""Touched.""); 
        llMessageLinked(-1, 0, ""c"", ""d"");
goto test;
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
        llSay(0,""testing time..."");
        state default;
    }
}";
            m_compiler.ClearErrors();
            string compiledScript;
            object map;
            Convert(script, out compiledScript, out map);
            if (m_compiler.GetErrors().Length > 0)
                return false;
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
                                                                 "Aurora.Framework.dll"));
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

        #endregion

        public void Convert(string Script, out string CompiledScript,
                            out object PositionMap)
        {
            // Its LSL, convert it to C#
            Dictionary<int, int> map = new Dictionary<int, int>();
            List<string> csClass = new List<string>();
            Script = Script.Replace("\n", "\r\n");

            List<string> GlobalFunctions;
            string[] lineSplit = ConvertLSLTypes(Script, out GlobalFunctions);
            if (m_compiler.GetErrors().Length > 0)
            {
                CompiledScript = "";
                PositionMap = "";
                return;
            }
            Dictionary<string, string[]> EnumerableFunctionInfos = new Dictionary<string, string[]>();
            foreach (string function in GlobalFunctions)
            {
                string[] func = GetInfo(function);
                EnumerableFunctionInfos[func[1]] = func;
            }
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
            List<int> ProtectedBracketLoops = new List<int>();
            bool AddBracketAfterNextCommand = false;
            foreach (string wword in split)
            {
                if (i < skipUntil)
                {
                    i++;
                    continue;
                }
                string word = wword.Replace("\r", "");
                if (word.StartsWith("//"))
                    GetUntilBreak(split, breaksplit, i, out skipUntil);
                else if (word.StartsWith("/*"))
                    GetUntil(split, "*/", i, out skipUntil);
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
                        AddToClass(csClass, GenerateEvent(currentState, split, breaksplit, i, out skipUntil, ref GlobalFunctions),
                            split, breaksplit, lineSplit, i, ref map);
                        inMethod = true;
                    }
                }
                else if (word == "{" || word == "}")
                {
                    bool addToClass = !(word == "{" && bracketInsideMethod == 0 && InState) &&
                        !(word == "}" && bracketInsideMethod == 1 && InState);
                    if (word == "{")
                        bracketInsideMethod++;
                    else
                    {
                        bracketInsideMethod--;
                        if (ProtectedBracketLoops.Contains(bracketInsideMethod))
                        {
                            ProtectedBracketLoops.Remove(bracketInsideMethod);
                            AddToClass(csClass, GenerateTimeCheck("", true), split, breaksplit, lineSplit, i, ref map);
                        }
                        if (bracketInsideMethod == (InState ? 1 : 0))
                        {
                            //We're inside an enumerable function, add the yield return/break
                            AddToClass(csClass, GenerateReturn(""), split, breaksplit, lineSplit, i, ref map);
                            inMethod = false;
                        }
                        if (bracketInsideMethod == 0)
                            InState = false;
                    }
                    if (addToClass)
                        AddToClass(csClass, word,
                            split, breaksplit, lineSplit, i, ref map);
                }
                else if (inMethod)
                {
                    bool wasSemicolan = false;
                    string csLine = split[i].StartsWith("for") ?
                        GetUntilBreak(split, breaksplit, i, out skipUntil)
                        :
                        GetUntilSemicolanOrBreak(split, breaksplit, i, out skipUntil, out wasSemicolan);
                    if (wasSemicolan && csLine.StartsWith("return"))
                        csLine = GenerateReturn(csLine);
                    AddDefaultInitializers(ref csLine);
                    if (AddBracketAfterNextCommand)
                    {
                        AddBracketAfterNextCommand = false;
                        csLine += "\n }";
                    }

                    foreach (string call in ProtectedCalls)
                        if (csLine.StartsWith(call))
                        {
                            if (!GetNextWord(split, skipUntil - 1).StartsWith("{"))//Someone is trying to do while(TRUE) X();
                            {
                                csLine += " { ";
                                csLine = GenerateTimeCheck(csLine, false);
                                AddBracketAfterNextCommand = true;
                            }
                            else
                                ProtectedBracketLoops.Add(bracketInsideMethod);//Make sure no loops are created as well
                        }
                    foreach (string call in CalledBeforeProtectedCalls)
                        if (csLine.StartsWith(call))
                            csLine = GenerateTimeCheck(csLine, true);//Make sure no other loops are created as well

                    string noSpacedLine = csLine.Replace(" ", "");
                    Match match;
                    foreach (string[] globFunc in EnumerableFunctionInfos.Values)
                        if (RegexContains(csLine, GenerateRegex(globFunc[1], int.Parse(globFunc[3])), out match))
                            csLine = GenerateNewFunction(csLine, globFunc, match);

                    AddToClass(csClass, csLine,
                            split, breaksplit, lineSplit, i, ref map);
                }
                else if (!InState)
                {
                    bool wasSemicolan;
                    string line = GetUntil(split, ";", ")", i, out skipUntil, out wasSemicolan);
                    if (!wasSemicolan)
                        GenerateGlobalFunction(ref inMethod, ref line);
                    else
                        AddDefaultInitializers(ref line);

                    AddToClass(csClass, line,
                            split, breaksplit, lineSplit, i, ref map);
                }
                else
                {
                    AddToClass(csClass, "fake", split, breaksplit, lineSplit, i, ref map);
                    m_compiler.AddError(String.Format("({0},{1}): {3}: {2}\n",
                                             map[csClass.Count - 1], 1, "Invalid expression term '" + word + "'", "Error"));
                    CompiledScript = "";
                    PositionMap = null;
                    return;
                }
                i++;
            }

            PositionMap = map;
            CompiledScript = CSCodeGenerator.CreateCompilerScript(m_compiler, new List<string>(), string.Join("\n", csClass.ToArray()));
        }

        private string GenerateRegex(string function, int paramCount)
        {
            string regex = function + "(| )";
            regex = GenerateParametersRegex(paramCount, regex);
            return regex;
        }

        private static string GenerateParametersRegex(int paramCount, string regex)
        {
            regex += "\\(";
            for (int i = 0; i < paramCount; i++)
                regex += ".*,";
            if (paramCount > 0)
                regex = regex.Remove(regex.Length - 1);
            regex += "\\)";
            return regex;
        }

        private bool RegexContains(string line, string x, out Match match)
        {
            /*^a(|.)\(\);*/
            /*b(|.)\(.*,.*,.*\);*/
            match = Regex.Match(line, x);
            return match.Success;
        }

        private string[] GetInfo(string function)
        {
            List<string> sp = new List<string>(function.Substring(0, function.IndexOf('(')).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            int spaceCount = sp.Count - 1;
            string retType, functionName, param;
            if (spaceCount == 0)
                retType = "void";
            else
            {
                retType = sp[0];
                sp.RemoveAt(0);
            }
            functionName = sp[0];
            int firstPos = function.IndexOf('(');
            param = function.Substring(firstPos, function.IndexOf(')') - firstPos) + ")";
            return new[]
            {
                retType,
                functionName,
                param,
                (param.Split(',').Length - 1).ToString()
            };
        }

        private string GenerateNewFunction(string line, string[] globalFunctionDef, Match match)
        {
            string retType = globalFunctionDef[0];
            string functionName = globalFunctionDef[1];
            string functionParams = globalFunctionDef[2];
            int ParamCount = int.Parse(globalFunctionDef[3]);

            Match paramMatch;
            RegexContains(match.Value, GenerateParametersRegex(ParamCount, ""), out paramMatch);

            string parameters = paramMatch.Value;

            string Mname = Aurora.Framework.StringUtils.RandomString(10, true);
            string Exname = Aurora.Framework.StringUtils.RandomString(10, true);

            string newLine = "string " + Exname + " =  \"\";" +
                                                  "System.Collections.IEnumerator " + Mname + " = " +
                                                  functionName + parameters +
                                                  ";" +
                                                  "while (true) {" +
                                                  " try {" +
                                                  "  if(!" + Mname + ".MoveNext())" +
                                                  "   break;" +
                                                  "  }" +
                                                  " catch(Exception ex) " +
                                                  "  {" +
                                                  "  " + Exname + " = ex.Message;" +
                                                  "  }" +
                                                  " if(" + Exname + " != \"\")" +
                                                  "   yield return " + Exname + ";" +
                                                  " else if(" + Mname + ".Current == null || " + Mname +
                                                           ".Current is DateTime)" +
                                                  "   yield return " + Mname + ".Current;" +
                                                  " else break;" +
                                                  " }";
            if (retType != "void")
                newLine += "\n" + line.Replace(functionName + parameters, "(" + retType + ")" + Mname + ".Current");
            return newLine;
        }

        private string GenerateTimeCheck(string csLine, bool before)
        {
            string check = "if (CheckSlice()) yield return null; ";
            return before ? check + csLine : csLine + check;
        }

        private void GenerateGlobalFunction(ref bool inMethod, ref string line)
        {
            string[] info = GetInfo(line);
            inMethod = true;
            line = "public System.Collections.IEnumerator " + info[1] + info[2];
        }

        private string GenerateReturn(string csLine)
        {
            List<string> split = new List<string>(csLine == "" ? new string[0] :
                csLine.Trim().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries));
            if (split.Count < 2)
                return "yield break;";

            split.RemoveAt(0);
            return "yield return " + string.Join(" ", split.ToArray());
        }

        private void AddDefaultInitializers(ref string csLine)
        {
            csLine = RemoveComments(csLine.Trim());
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

        private string[] ConvertLSLTypes(string Script, out List<string> GlobalFunctions)
        {
            Match vectorMatches;
            RegexContains(Script, "<.*,.*,.*>", out vectorMatches);
            while (vectorMatches.Success)
            {
                if (vectorMatches.Value != "")
                    Script = Script.Replace(vectorMatches.Value, "new vector(" + vectorMatches.Value.Substring(1, vectorMatches.Value.Length - 2) + ")");
                vectorMatches = vectorMatches.NextMatch();
            }
            RegexContains(Script, "<.*,.*,.*,.*>", out vectorMatches);
            while (vectorMatches.Success)
            {
                if (vectorMatches.Value != "")
                    Script = Script.Replace(vectorMatches.Value, "new rotation(" + vectorMatches.Value.Substring(1, vectorMatches.Value.Length - 2) + ")");
                vectorMatches = vectorMatches.NextMatch();
            }
            RegexContains(Script, "[*]", out vectorMatches);
            while (vectorMatches.Success)
            {
                if (vectorMatches.Value != "" && vectorMatches.Value != "*")
                    Script = Script.Replace(vectorMatches.Value, "new list(" + vectorMatches.Value.Substring(1, vectorMatches.Value.Length - 2) + ")");
                vectorMatches = vectorMatches.NextMatch();
            }

            RegexContains(Script, ".*\\..*", out vectorMatches);
            while (vectorMatches.Success)
            {
                if (vectorMatches.Value != "")
                {
                    //TODO: check for C# syntax
                    int startValue = 0, index;
                    while ((index = vectorMatches.Value.IndexOf(".", startValue)) != -1)
                    {
                        char c = vectorMatches.Value[index + 1];
                        char d = vectorMatches.Value[index - 1];
                        if (!char.IsNumber(c) && !char.IsNumber(d))//Eliminates float values, such as 5.05
                        {
                            bool mustHaveCommentOrWillFail = false;
                            char nxtChar = c;
                            int i = index + 1;
                            int length = vectorMatches.Value.Length;
                            while (char.IsWhiteSpace(nxtChar))
                            {
                                if (i == length)
                                {
                                    //Blocks things like File.
                                    //   Delete("Testing");
                                    mustHaveCommentOrWillFail = false;
                                    break;
                                }
                                nxtChar = vectorMatches.Value[i++];//NO putting spaces before other functions
                            }
                            if (!(!mustHaveCommentOrWillFail && (nxtChar == 'x' || nxtChar == 'y' || nxtChar == 'z' || nxtChar == 'w') && !char.IsLetterOrDigit(vectorMatches.Value[i + 1])))//Eliminate vector.x, etc
                            {
                                //Check whether it is inside ""
                                bool inside = false;
                                bool insideComment = false;
                                int pos = 0;
                                foreach (char chr in vectorMatches.Value)
                                {
                                    if (chr == '"')
                                        inside = !inside;
                                    if (chr == '/' && vectorMatches.Value[pos + 1] == '*' && !inside)
                                        insideComment = true;
                                    if (chr == '*' && vectorMatches.Value[pos + 1] == '/')
                                        insideComment = false;
                                    if (chr == '/' && vectorMatches.Value[pos + 1] == '/')
                                    {
                                        if (!inside)
                                            insideComment = true;//Goes for the entire line if its not inside a "" already
                                        break;
                                    }
                                    if (pos++ == index)
                                        break;
                                }
                                if (!inside && !insideComment)
                                {
                                    m_compiler.AddError("Failed to find valid expression containing .");
                                    GlobalFunctions = new List<string>();
                                    return new string[0];
                                }
                                else
                                {
                                    //Inside "" or comment
                                }
                            }
                            else
                            {
                                //vector.x, vector.y, etc
                            }
                        }
                        else
                        {
                            //Float, 0.05, valid
                        }
                        startValue = index + 1;
                    }
                }
                vectorMatches = vectorMatches.NextMatch();
            }
            RegexContains(Script, m_functionRegex, out vectorMatches);
            List<string> replacedFunctions = new List<string>();
            while (vectorMatches.Success)
            {
                if (vectorMatches.Value != "" && !replacedFunctions.Contains(vectorMatches.Value))
                {
                    IScriptApi api = m_scriptApis[vectorMatches.Value];
                    string formatedFunction = String.Format("{3}(({0})m_apis[\"{1}\"]).{2}",
                                              api.InterfaceName,
                                              api.Name, vectorMatches.Value,
                                              DTFunctions.Contains(vectorMatches.Value) ? "yield return " : "");
                    Script = Script.Replace(vectorMatches.Value, formatedFunction);
                    replacedFunctions.Add(vectorMatches.Value);
                }
                vectorMatches = vectorMatches.NextMatch();
            }
            Script = Script.Replace("integer", "LSL_Types.LSLInteger");
            Script = Script.Replace("float", "LSL_Types.LSLFloat");
            Script = Script.Replace("key", "LSL_Types.LSLString");
            Script = Script.Replace("string", "LSL_Types.LSLString");
            Script = Script.Replace("vector", "LSL_Types.Vector3");
            Script = Script.Replace("rotation", "LSL_Types.Quaternion");
            Script = Script.Replace("list", "LSL_Types.list");
            string[] split = Script.Split('\n');
            bool beforeStates = true;
            int bracketCount = 0;
            GlobalFunctions = new List<string>();
            for (int i = 0; i < split.Length; i++)
            {
                if (split[i].StartsWith("default") || split[i].StartsWith("state "))
                    beforeStates = false;
                else if (split[i].StartsWith("{"))
                    bracketCount++;
                else if (split[i].StartsWith("}"))
                    bracketCount--;
                else if (split[i].StartsWith("/*"))
                    bracketCount++;
                else if (split[i].StartsWith("*/"))
                    bracketCount--;
                else if (beforeStates && bracketCount == 0)
                {
                    string splitTrim = RemoveComments(split[i].Trim());
                    string splitTrimReplaced = splitTrim.Replace(" ", "");
                    if (splitTrim != "" &&
                        !splitTrim.EndsWith(";") &&
                        !splitTrim.StartsWith("//") &&
                        !splitTrim.StartsWith("/*") &&
                        !splitTrimReplaced.EndsWith(";"))
                    {
                        if (splitTrimReplaced.EndsWith("{"))
                            bracketCount++;
                        else if (splitTrimReplaced.EndsWith("}"))
                            bracketCount--;
                        GlobalFunctions.Add(splitTrim);
                    }
                }
            }
            return split;
        }

        private string RemoveComments(string p)
        {
            string n = "";
            bool insideComment = false;
            int pos = 0;
            foreach (char chr in p)
            {
                if (chr == '/' && (pos + 1 != p.Length) && p[pos + 1] == '*')
                    insideComment = true;
                if (chr == '*' && (pos + 1 != p.Length) && p[pos + 1] == '/')
                    insideComment = false;
                if (chr == '/' && (pos + 1 != p.Length) && p[pos + 1] == '/')
                    insideComment = true;//Goes for the entire line if its not inside a "" already
                if (!insideComment)
                    n += chr;
                if (pos++ == p.Length)
                    break;
            }
            return n;
        }

        private bool RemoveR(string r)
        {
            return r.Replace("\r", "") == "";
        }

        private string GenerateEvent(string currentState, string[] split, string[] breaksplit, int i, out int skipUntil, ref List<string> GlobalFunctions)
        {
            string eventName = GetUntil(split, ")", i, out skipUntil);
            GlobalFunctions.Add(eventName);
            return "public System.Collections.IEnumerator " + currentState + "_event_" + eventName;
        }

        private string GetNextWord(string[] split, int i)
        {
            return split[i + 1];
        }

        #region GetUntil*

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
                {
                    end = int.MaxValue;
                    return resp;
                }
            }
            end = ++i;
            return resp;
        }

        private string GetUntil(string[] splita, string[] splitb, string a, string b, int i, out int end, out bool wasA)
        {
            int aEnd, bEnd;
            string aa = "", bb = "";
            aa = GetUntil(splita, a, i, out aEnd);
            bb = GetUntil(splitb, b, i, out bEnd);
            end = aEnd <= bEnd ? aEnd : bEnd;
            wasA = aEnd <= bEnd;
            return aEnd <= bEnd ? aa : bb;
        }

        private string GetUntil(string[] split, string a, string b, int i, out int end, out bool wasA)
        {
            return GetUntil(split, split, a, b, i, out end, out wasA);
        }

        private string GetUntilSemicolan(string[] split, int i, out int end)
        {
            return GetUntil(split, ";", i, out end);
        }

        private string GetUntilSemicolanOrBreak(string[] split, string[] breaksplit, int i, out int end, out bool wasSemicolan)
        {
            return GetUntil(split, breaksplit, ";", "\r", i, out end, out wasSemicolan);
        }

        #endregion

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
            LineN = CompErr.Line;
            if (CompErr.Line > CSCodeGenerator.GetHeaderCount(m_compiler))
                LineN = CompErr.Line - CSCodeGenerator.GetHeaderCount(m_compiler) - 1;
            CharN = 1;
            LineN = PositionMapp[LineN];//LSL is zero based, so subtract one
        }

        #endregion

        public void Dispose()
        {
        }
    }
}