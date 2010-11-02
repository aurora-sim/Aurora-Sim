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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;
using Nini.Config;

namespace OpenSim.Framework
{
    public interface ICommandConsole: IPlugin
    {
        Commands Commands { get; set; }
        void Initialize(string defaultPrompt, IConfigSource source, IOpenSimBase baseOpenSim);
        string DefaultPrompt { get; set; }
        void LockOutput();
        void UnlockOutput();
        void Output(string text, string level);
        void Output(string text);
        string CmdPrompt(string p);
        string CmdPrompt(string p, string def);
        string CmdPrompt(string p, List<char> excludedCharacters);
        string CmdPrompt(string p, string def, List<char> excludedCharacters);
        string CmdPrompt(string prompt, string defaultresponse, List<string> options);
        string PasswdPrompt(string p);
        string ReadLine(string p, bool isCommand, bool e);
        void RunCommand(string cmd);
        object ConsoleScene { get; set; }
        void Prompt();
        /// <summary>
        /// Starts the prompt for the console. This will never stop until the region is closed.
        /// </summary>
        void ReadConsole();

        void EndConsoleProcessing();
    }


    public class ConsolePluginInitialiser : PluginInitialiserBase
    {
        private IConfigSource m_source;
        private string m_defaultPrompt;
        private IOpenSimBase m_baseOpenSim;
        public ConsolePluginInitialiser(string defaultPrompt, IConfigSource source, IOpenSimBase baseOpenSim)
        {
            m_baseOpenSim = baseOpenSim;
            m_source = source;
            m_defaultPrompt = defaultPrompt;
        }

        public override void Initialise(IPlugin plugin)
        {
            ICommandConsole console = plugin as ICommandConsole;
            if (console == null)
                return;
            console.Initialize(m_defaultPrompt, m_source, m_baseOpenSim);
        }
    }
}
