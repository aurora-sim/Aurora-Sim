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

using System.Collections.Generic;
using Tools;
using Aurora.ScriptEngineParser;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools
{
    public class LSL2CSCodeTransformer
    {
        private static Dictionary<string, string> m_datatypeLSL2OpenSim;
        private readonly SYMBOL m_astRoot;
        private readonly Dictionary<string, string> m_globalVariableValues = new Dictionary<string, string>();
        private readonly Dictionary<string, SYMBOL> m_duplicatedGlobalVariableValues = new Dictionary<string, SYMBOL>();
        private Dictionary<string, Dictionary<string, SYMBOL>> m_localVariableValues = new Dictionary<string, Dictionary<string, SYMBOL>>();
        private Dictionary<string, Dictionary<string, string>> m_localVariableValuesStr = new Dictionary<string, Dictionary<string, string>>();
        private readonly Dictionary<string, Dictionary<string, SYMBOL>> m_duplicatedLocalVariableValues = new Dictionary<string, Dictionary<string, SYMBOL>>();
        private string m_currentEvent = "";
        private string m_currentState = "";

        public Dictionary<string, SYMBOL> DuplicatedGlobalVars
        {
            get { return m_duplicatedGlobalVariableValues; }
        }
        public Dictionary<string, string> GlobalVars
        {
            get { return m_globalVariableValues; }
        }

        public Dictionary<string, Dictionary<string, SYMBOL>> DuplicatedLocalVars
        {
            get { return m_duplicatedLocalVariableValues; }
        }
        public Dictionary<string, Dictionary<string, SYMBOL>> LocalVars
        {
            get { return m_localVariableValues; }
        }

        /// <summary>
        ///   Pass the new CodeTranformer an abstract syntax tree.
        /// </summary>
        /// <param name = "astRoot">The root node of the AST.</param>
        public LSL2CSCodeTransformer(SYMBOL astRoot)
        {
            m_astRoot = astRoot;

            // let's populate the dictionary
            if (null == m_datatypeLSL2OpenSim)
            {
                m_datatypeLSL2OpenSim = new Dictionary<string, string>
                                            {
                                                {"integer", "LSL_Types.LSLInteger"},
                                                {"float", "LSL_Types.LSLFloat"},
                                                {"key", "LSL_Types.LSLString"},
                                                {"string", "LSL_Types.LSLString"},
                                                {"vector", "LSL_Types.Vector3"},
                                                {"rotation", "LSL_Types.Quaternion"},
                                                {"list", "LSL_Types.list"}
                                            };
            }
        }

        /// <summary>
        ///   Transform the code in the AST we have.
        /// </summary>
        /// <returns>The root node of the transformed AST</returns>
        public SYMBOL Transform()
        {
            return Transform(null, null);
        }

        public SYMBOL Transform(Dictionary<string, string> GlobalMethods,
                                Dictionary<string, ObjectList> MethodArguements)
        {
            foreach (SYMBOL s in m_astRoot.kids)
                TransformNode(s, GlobalMethods, MethodArguements);

            return m_astRoot;
        }

        /// <summary>
        ///   Recursively called to transform each type of node. Will transform this
        ///   node, then all it's children.
        /// </summary>
        /// <param name = "s">The current node to transform.</param>
        private void TransformNode(SYMBOL s, Dictionary<string, string> GlobalMethods,
                                   Dictionary<string, ObjectList> MethodArguements)
        {
            // make sure to put type lower in the inheritance hierarchy first
            // ie: since IdentConstant and StringConstant inherit from Constant,
            // put IdentConstant and StringConstant before Constant
            if (s is Declaration)
            {
                Declaration dec = (Declaration)s;
                dec.Datatype = m_datatypeLSL2OpenSim[dec.Datatype];
            }
            else if (s is Constant)
                ((Constant)s).Type = m_datatypeLSL2OpenSim[((Constant)s).Type];
            else if (s is TypecastExpression)
                ((TypecastExpression)s).TypecastType = m_datatypeLSL2OpenSim[((TypecastExpression)s).TypecastType];
            else if (s is GlobalFunctionDefinition)
            {
                GlobalFunctionDefinition fun = (GlobalFunctionDefinition)s;
                if ("void" == fun.ReturnType) // we don't need to translate "void"
                {
                    if (GlobalMethods != null && !GlobalMethods.ContainsKey(fun.Name))
                        GlobalMethods.Add(fun.Name, "void");
                }
                else
                {
                    fun.ReturnType =
                        m_datatypeLSL2OpenSim[fun.ReturnType];
                    if (GlobalMethods != null && !GlobalMethods.ContainsKey(fun.Name))
                    {
                        GlobalMethods.Add(fun.Name, fun.ReturnType);
                        MethodArguements.Add(fun.Name, (s).kids);
                    }
                }
                //Reset the variables, we changed events
                m_currentEvent = fun.Name;
                m_localVariableValues.Add("global_function_" + fun.Name, new Dictionary<string, SYMBOL>());
                m_localVariableValuesStr.Add("global_function_" + fun.Name, new Dictionary<string, string>());
                m_duplicatedLocalVariableValues.Add("global_function_" + fun.Name, new Dictionary<string, SYMBOL>());
            }
            else if (s is State)
            {
                //Reset the variables, we changed events
                State evt = (State)s;
                m_currentState = evt.Name;
            }
            else if (s is StateEvent)
            {
                //Reset the variables, we changed events
                StateEvent evt = (StateEvent)s;
                m_currentEvent = evt.Name;
                m_localVariableValues.Add(m_currentState + "_" + evt.Name, new Dictionary<string, SYMBOL>());
                m_localVariableValuesStr.Add(m_currentState + "_" + evt.Name, new Dictionary<string, string>());
                m_duplicatedLocalVariableValues.Add(m_currentState + "_" + evt.Name, new Dictionary<string, SYMBOL>());
            }
            else if (s is GlobalVariableDeclaration)
            {
                GlobalVariableDeclaration gvd = (GlobalVariableDeclaration)s;
                foreach (SYMBOL child in gvd.kids)
                {
                    if (child is Assignment)
                    {
                        bool isDeclaration = false;
                        string decID = "";
                        foreach (SYMBOL assignmentChild in child.kids)
                        {
                            if (assignmentChild is Declaration)
                            {
                                Declaration d = (Declaration)assignmentChild;
                                decID = d.Id;
                                isDeclaration = true;
                            }
                            else if (assignmentChild is IdentExpression)
                            {
                                IdentExpression identEx = (IdentExpression)assignmentChild;
                                if (isDeclaration)
                                {
                                    if (m_globalVariableValues.ContainsKey(decID))
                                        m_duplicatedGlobalVariableValues[decID] = identEx;
                                    m_globalVariableValues[decID] = identEx.Name;
                                }
                            }
                            else if (assignmentChild is ListConstant)
                            {
                                ListConstant listConst = (ListConstant)assignmentChild;
                                foreach (SYMBOL listChild in listConst.kids)
                                {
                                    if (listChild is ArgumentList)
                                    {
                                        ArgumentList argList = (ArgumentList)listChild;
                                        int i = 0;
                                        bool changed = false;
                                        object[] p = new object[argList.kids.Count];
                                        foreach (SYMBOL objChild in argList.kids)
                                        {
                                            p[i] = objChild;
                                            if (objChild is IdentExpression)
                                            {
                                                IdentExpression identEx = (IdentExpression)objChild;
                                                if (m_globalVariableValues.ContainsKey(identEx.Name))
                                                {
                                                    changed = true;
                                                    p[i] = new IdentExpression(identEx.yyps,
                                                                               m_globalVariableValues[identEx.Name])
                                                    {
                                                        pos = objChild.pos,
                                                        m_dollar = objChild.m_dollar
                                                    };
                                                }
                                            }
                                            i++;
                                        }
                                        if (changed)
                                        {
                                            argList.kids = new ObjectList();
                                            foreach (object o in p)
                                                argList.kids.Add(o);
                                        }
                                        if (isDeclaration)
                                        {
                                            if (m_globalVariableValues.ContainsKey(decID))
                                                m_duplicatedGlobalVariableValues[decID] = listConst;
                                            m_globalVariableValues[decID] = listConst.Value;
                                        }
                                    }
                                }
                            }
                            else if (assignmentChild is VectorConstant || assignmentChild is RotationConstant)
                            {
                                Constant listConst = (Constant)assignmentChild;
                                int i = 0;
                                bool changed = false;
                                object[] p = new object[listConst.kids.Count];
                                foreach (SYMBOL objChild in listConst.kids)
                                {
                                    p[i] = objChild;
                                    if (objChild is IdentExpression)
                                    {
                                        IdentExpression identEx = (IdentExpression)objChild;
                                        if (m_globalVariableValues.ContainsKey(identEx.Name))
                                        {
                                            changed = true;
                                            p[i] = new IdentExpression(identEx.yyps,
                                                                       m_globalVariableValues[identEx.Name])
                                            {
                                                pos = objChild.pos,
                                                m_dollar = objChild.m_dollar
                                            };
                                        }
                                    }
                                    i++;
                                }
                                if (changed)
                                {
                                    listConst.kids = new ObjectList();
                                    foreach (object o in p)
                                        listConst.kids.Add(o);
                                }
                                if (isDeclaration)
                                {
                                    if (m_globalVariableValues.ContainsKey(decID))
                                        m_duplicatedGlobalVariableValues[decID] = listConst;
                                    m_globalVariableValues[decID] = listConst.Value;
                                }
                            }
                            else if (assignmentChild is Constant)
                            {
                                Constant identEx = (Constant)assignmentChild;
                                if (isDeclaration)
                                {
                                    if (m_globalVariableValues.ContainsKey(decID))
                                        m_duplicatedGlobalVariableValues[decID] = identEx;
                                    m_globalVariableValues[decID] = identEx.Value;
                                }
                            }
                        }
                    }
                }
            }
            else if (s is Assignment && m_currentEvent != "")
            {
                Assignment ass = (Assignment)s;
                bool isDeclaration = false;
                string decID = "";
                foreach (SYMBOL assignmentChild in ass.kids)
                {
                    if (assignmentChild is Declaration)
                    {
                        Declaration d = (Declaration)assignmentChild;
                        decID = d.Id;
                        isDeclaration = true;
                    }
                    else if (assignmentChild is IdentExpression)
                    {
                        IdentExpression identEx = (IdentExpression)assignmentChild;
                        if (isDeclaration)
                        {
                            if (m_localVariableValues[GetLocalVariableDictionaryKey()].ContainsKey(decID) && 
                                !m_duplicatedLocalVariableValues[GetLocalVariableDictionaryKey()].ContainsKey(decID))
                                m_duplicatedLocalVariableValues[GetLocalVariableDictionaryKey()][decID] = m_localVariableValues[GetLocalVariableDictionaryKey()][decID];
                            m_localVariableValues[GetLocalVariableDictionaryKey()][decID] = identEx;
                            m_localVariableValuesStr[GetLocalVariableDictionaryKey()][decID] = identEx.Name;
                        }
                    }
                    else if (assignmentChild is ListConstant)
                    {
                        ListConstant listConst = (ListConstant)assignmentChild;
                        foreach (SYMBOL listChild in listConst.kids)
                        {
                            if (listChild is ArgumentList)
                            {
                                ArgumentList argList = (ArgumentList)listChild;
                                int i = 0;
                                bool changed = false;
                                object[] p = new object[argList.kids.Count];
                                foreach (SYMBOL objChild in argList.kids)
                                {
                                    p[i] = objChild;
                                    if (objChild is IdentExpression)
                                    {
                                        IdentExpression identEx = (IdentExpression)objChild;
                                        if (m_localVariableValues[GetLocalVariableDictionaryKey()].ContainsKey(identEx.Name))
                                        {
                                            changed = true;
                                            p[i] = new IdentExpression(identEx.yyps,
                                                                        m_localVariableValuesStr[GetLocalVariableDictionaryKey()][identEx.Name])
                                            {
                                                pos = objChild.pos,
                                                m_dollar = objChild.m_dollar
                                            };
                                        }
                                    }
                                    i++;
                                }
                                if (changed)
                                {
                                    argList.kids = new ObjectList();
                                    foreach (object o in p)
                                        argList.kids.Add(o);
                                }
                                if (isDeclaration)
                                {
                                    if (m_localVariableValues[GetLocalVariableDictionaryKey()].ContainsKey(decID) &&
                                        !m_duplicatedLocalVariableValues[GetLocalVariableDictionaryKey()].ContainsKey(decID))
                                        m_duplicatedLocalVariableValues[GetLocalVariableDictionaryKey()][decID] = m_localVariableValues[GetLocalVariableDictionaryKey()][decID];
                                    m_localVariableValues[GetLocalVariableDictionaryKey()][decID] = listConst;
                                    m_localVariableValuesStr[GetLocalVariableDictionaryKey()][decID] = listConst.Value;
                                }
                            }
                        }
                    }
                    else if (assignmentChild is VectorConstant || assignmentChild is RotationConstant)
                    {
                        Constant listConst = (Constant)assignmentChild;
                        int i = 0;
                        bool changed = false;
                        object[] p = new object[listConst.kids.Count];
                        foreach (SYMBOL objChild in listConst.kids)
                        {
                            p[i] = objChild;
                            if (objChild is IdentExpression)
                            {
                                IdentExpression identEx = (IdentExpression)objChild;
                                if (m_localVariableValues[GetLocalVariableDictionaryKey()].ContainsKey(identEx.Name))
                                {
                                    changed = true;
                                    p[i] = new IdentExpression(identEx.yyps,
                                                                m_localVariableValuesStr[GetLocalVariableDictionaryKey()][identEx.Name])
                                    {
                                        pos = objChild.pos,
                                        m_dollar = objChild.m_dollar
                                    };
                                }
                            }
                            i++;
                        }
                        if (changed)
                        {
                            listConst.kids = new ObjectList();
                            foreach (object o in p)
                                listConst.kids.Add(o);
                        }
                        if (isDeclaration)
                        {
                            if (m_localVariableValues[GetLocalVariableDictionaryKey()].ContainsKey(decID) &&
                                !m_duplicatedLocalVariableValues[GetLocalVariableDictionaryKey()].ContainsKey(decID))
                                m_duplicatedLocalVariableValues[GetLocalVariableDictionaryKey()][decID] = m_localVariableValues[GetLocalVariableDictionaryKey()][decID];
                            m_localVariableValues[GetLocalVariableDictionaryKey()][decID] = listConst;
                            m_localVariableValuesStr[GetLocalVariableDictionaryKey()][decID] = listConst.Value;
                        }
                    }
                    else if (assignmentChild is Constant)
                    {
                        Constant identEx = (Constant)assignmentChild;
                        if (isDeclaration)
                        {
                            if (m_localVariableValues[GetLocalVariableDictionaryKey()].ContainsKey(decID) &&
                                !m_duplicatedLocalVariableValues[GetLocalVariableDictionaryKey()].ContainsKey(decID))
                                m_duplicatedLocalVariableValues[GetLocalVariableDictionaryKey()][decID] = m_localVariableValues[GetLocalVariableDictionaryKey()][decID];
                            m_localVariableValues[GetLocalVariableDictionaryKey()][decID] = identEx;
                            m_localVariableValuesStr[GetLocalVariableDictionaryKey()][decID] = identEx.Value;
                        }
                    }
                }
            }
            /*if(s is Statement)
            {
                if(s.kids.Count == 1 && s.kids[0] is Assignment)
                {
                    Assignment assignment = (Assignment)s.kids[0];
                    object[] p = new object[assignment.kids.Count];
                    int i = 0;
                    int toRemove = -1;
                    foreach(SYMBOL assignmentChild in assignment.kids)
                    {
                        p[i] = assignmentChild;
                        if(assignmentChild is Declaration)
                        {
                            Declaration d = (Declaration)assignmentChild;
                            if(m_allVariableValues.Contains(d.Id))
                                toRemove = i;
                            else
                                m_allVariableValues.Add(d.Id);
                        }
                        i++;
                    }
                    if(toRemove != -1)
                    {
                        List<object> ps = new List<object>();
                        foreach(object obj in p)
                            ps.Add(obj);
                        ps[toRemove] = new IDENT(null)
                        {
                            kids = new ObjectList(),
                            pos = ((SYMBOL)ps[toRemove]).pos,
                            m_dollar = ((SYMBOL)ps[toRemove]).m_dollar,
                            yylval = ((SYMBOL)ps[toRemove]).yylval,
                            yylx = ((SYMBOL)ps[toRemove]).yylx,
                            yyps = ((SYMBOL)ps[toRemove]).yyps,
                            yytext = ps[toRemove] is Declaration ?
                            ((Declaration)ps[toRemove]).Id
                            : ((SYMBOL)ps[toRemove]).yyname,
                        };
                        ((SYMBOL)s.kids[0]).kids = new ObjectList();
                        foreach(object obj in ps)
                            if(obj != null)
                                ((SYMBOL)s.kids[0]).kids.Add(obj);
                    }
                }
            }*/

            for (int i = 0; i < s.kids.Count; i++)
            {
                // It's possible that a child is null, for instance when the
                // assignment part in a for-loop is left out, ie:
                //
                //     for (; i < 10; i++)
                //     {
                //         ...
                //     }
                //
                // We need to check for that here.

                if (null != s.kids[i])
                {
                    if (!(s is Assignment || s is ArgumentDeclarationList) && s.kids[i] is Declaration)
                        AddImplicitInitialization(s, i);

                    TransformNode((SYMBOL)s.kids[i], null, null);
                }
            }
        }

        private string GetLocalVariableDictionaryKey()
        {
            if (m_currentState == "")
                return "global_function_" + m_currentEvent;
            return m_currentState + "_" + m_currentEvent;
        }

        /// <summary>
        ///   Replaces an instance of the node at s.kids[didx] with an assignment
        ///   node. The assignment node has the Declaration node on the left hand
        ///   side and a default initializer on the right hand side.
        /// </summary>
        /// <param name = "s">
        ///   The node containing the Declaration node that needs replacing.
        /// </param>
        /// <param name = "didx">Index of the Declaration node to replace.</param>
        private void AddImplicitInitialization(SYMBOL s, int didx)
        {
            // We take the kids for a while to play with them.
            int sKidSize = s.kids.Count;
            object[] sKids = new object[sKidSize];
            for (int i = 0; i < sKidSize; i++)
                sKids[i] = s.kids.Pop();

            // The child to be changed.
            Declaration currentDeclaration = (Declaration)sKids[didx];

            // We need an assignment node.
            Assignment newAssignment = new Assignment(currentDeclaration.yyps,
                                                      currentDeclaration,
                                                      GetZeroConstant(currentDeclaration.yyps,
                                                                      currentDeclaration.Datatype),
                                                      "=");
            sKids[didx] = newAssignment;

            // Put the kids back where they belong.
            for (int i = 0; i < sKidSize; i++)
                s.kids.Add(sKids[i]);
        }

        /// <summary>
        ///   Generates the node structure required to generate a default
        ///   initialization.
        /// </summary>
        /// <param name = "p">
        ///   Tools.Parser instance to use when instantiating nodes.
        /// </param>
        /// <param name = "constantType">String describing the datatype.</param>
        /// <returns>
        ///   A SYMBOL node conaining the appropriate structure for intializing a
        ///   constantType.
        /// </returns>
        private SYMBOL GetZeroConstant(Parser p, string constantType)
        {
            switch (constantType)
            {
                case "integer":
                    return new Constant(p, constantType, "0");
                case "float":
                    return new Constant(p, constantType, "0.0");
                case "string":
                case "key":
                    return new Constant(p, constantType, "");
                case "list":
                    ArgumentList al = new ArgumentList(p);
                    return new ListConstant(p, al);
                case "vector":
                    Constant vca = new Constant(p, "float", "0.0");
                    Constant vcb = new Constant(p, "float", "0.0");
                    Constant vcc = new Constant(p, "float", "0.0");
                    ConstantExpression vcea = new ConstantExpression(p, vca);
                    ConstantExpression vceb = new ConstantExpression(p, vcb);
                    ConstantExpression vcec = new ConstantExpression(p, vcc);
                    return new VectorConstant(p, vcea, vceb, vcec);
                case "rotation":
                    Constant rca = new Constant(p, "float", "0.0");
                    Constant rcb = new Constant(p, "float", "0.0");
                    Constant rcc = new Constant(p, "float", "0.0");
                    Constant rcd = new Constant(p, "float", "0.0");
                    ConstantExpression rcea = new ConstantExpression(p, rca);
                    ConstantExpression rceb = new ConstantExpression(p, rcb);
                    ConstantExpression rcec = new ConstantExpression(p, rcc);
                    ConstantExpression rced = new ConstantExpression(p, rcd);
                    return new RotationConstant(p, rcea, rceb, rcec, rced);
                default:
                    return null; // this will probably break stuff
            }
        }
    }
}