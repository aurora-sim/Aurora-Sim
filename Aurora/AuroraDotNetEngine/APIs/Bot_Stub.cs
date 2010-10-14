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
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using vector = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Vector3;
using rotation = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Quaternion;
using key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_List = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.list;
using LSL_String = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_Float = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLFloat;
using LSL_Integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using Aurora.ScriptEngine.AuroraDotNetEngine;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.Runtime
{
    public partial class ScriptBaseClass : MarshalByRefObject
    {
        public IBot_Api m_Bot_Functions;

        public void ApiTypebot(IScriptApi api)
        {
            if (!(api is IBot_Api))
                return;

            m_Bot_Functions = (IBot_Api)api;
        }

        public string botCreateBot(string FirstName, string LastName, string appearanceToClone)
        {
            return m_Bot_Functions.botCreateBot(FirstName, LastName, appearanceToClone);
        }

        public void botSetMap(string keyOfBot, LSL_List positions, LSL_List movementType)
        {
            m_Bot_Functions.botSetMap(keyOfBot, positions, movementType);
        }

        public void botPause(string bot)
        {
            m_Bot_Functions.botPause(bot);
        }

        public void botUnPause(string bot)
        {
            m_Bot_Functions.botUnPause(bot);
        }

        public void botStop(string bot)
        {
            m_Bot_Functions.botStop(bot);
        }

        public void botStart(string bot)
        {
            m_Bot_Functions.botStart(bot);
        }
    }
}
