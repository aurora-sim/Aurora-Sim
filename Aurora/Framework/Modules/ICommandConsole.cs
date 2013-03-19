/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using System.Collections.Generic;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.SceneInfo;
using Nini.Config;
using log4net.Core;

namespace Aurora.Framework.Modules
{
    /// <summary>
    ///     The console interface
    ///     This deals with all things that happen on the console or GUI
    /// </summary>
    public interface ICommandConsole
    {
        /// <summary>
        ///     Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        ///     All commands that are enabled on this console
        /// </summary>
        Commands Commands { get; set; }

        /// <summary>
        ///     The log level required to write onto the console
        /// </summary>
        Level MaxLogLevel { get; set; }

        /// <summary>
        ///     The text behind the blinking cursor on the console
        /// </summary>
        string DefaultPrompt { get; set; }

        /// <summary>
        ///     The current scene the console is set to use (can be null)
        /// </summary>
        IScene ConsoleScene { get; set; }

        bool HasProcessedCurrentCommand { get; set; }

        /// <summary>
        ///     Set up this console
        /// </summary>
        /// <param name="source"></param>
        /// <param name="baseOpenSim"></param>
        void Initialize(IConfigSource source, ISimulationBase baseOpenSim);

        /// <summary>
        ///     Locks other threads from inserting text onto the console until the other threads are done
        /// </summary>
        void LockOutput();

        /// <summary>
        ///     All finished with inserting text onto the console, let other threads go through
        /// </summary>
        void UnlockOutput();

        /// <summary>
        ///     This is only to be used by the OpenSimAppender, do NOT use unless you have a valid reason!
        /// </summary>
        /// <param name="text">The text that will be shown on the console</param>
        /// <param name="level">The level of output that this text is (determines the color)</param>
        void Output(string text, string level);

        /// <summary>
        ///     This is only to be used by the OpenSimAppender, do NOT use unless you have a valid reason!
        /// </summary>
        /// <param name="text">The text that will be shown on the console</param>
        void Output(string text);

        /// <summary>
        ///     Read a line of text from the console, can return ""
        /// </summary>
        /// <param name="prompt">The message that will be displayed to the console before a prompt is started</param>
        /// <returns></returns>
        string Prompt(string prompt);

        /// <summary>
        ///     Reads a line of text from the console, and if the user presses enter with no text, defaultReturn is returned for this method
        /// </summary>
        /// <param name="prompt">The message that will be displayed to the console before a prompt is started</param>
        /// <param name="defaultReturn">The default response to return if "" is given</param>
        /// <returns></returns>
        string Prompt(string prompt, string defaultReturn);

        /// <summary>
        ///     Reads a line of text from the console, excluding the given characters and will return defaultResponse if enter is pressed with ""
        /// </summary>
        /// <param name="prompt">The message that will be displayed to the console before a prompt is started</param>
        /// <param name="defaultResponse">The default response to return if "" is given</param>
        /// <param name="excludedCharacters">The characters that cannot be in the response</param>
        /// <returns></returns>
        string Prompt(string prompt, string defaultResponse, List<char> excludedCharacters);

        /// <summary>
        ///     Reads a line of text from the console, returning defaultResponse if no response is given, only the given options are valid entries for the user to use
        /// </summary>
        /// <param name="prompt">The message that will be displayed to the console before a prompt is started</param>
        /// <param name="defaultresponse">The default response to return if "" is given</param>
        /// <param name="options">The options that the user has to select from</param>
        /// <returns></returns>
        string Prompt(string prompt, string defaultresponse, List<string> options);

        /// <summary>
        ///     Sets up a prompt for secure information (hides the user text and disallows viewing of the text typed later)
        /// </summary>
        /// <param name="prompt">The message that will be displayed to the console before a prompt is started</param>
        /// <returns></returns>
        string PasswordPrompt(string prompt);

        /// <summary>
        ///     Run the given command (acts like it was typed from the console)
        /// </summary>
        /// <param name="cmd"></param>
        void RunCommand(string cmd);

        /// <summary>
        ///     Starts the prompt for the console. This will never stop until the region is closed.
        /// </summary>
        void ReadConsole();

        /// <summary>
        ///     Check to see whether level A is lower or equal to levelB
        /// </summary>
        /// <param name="levelA"></param>
        /// <param name="levelB"></param>
        /// <returns></returns>
        bool CompareLogLevels(string levelA, string levelB);


        bool IsDebugEnabled { get; }
        bool IsErrorEnabled { get; }
        bool IsFatalEnabled { get; }
        bool IsInfoEnabled { get; }
        bool IsWarnEnabled { get; }

        void Debug(object message);
        void Debug(object message, Exception exception);
        void DebugFormat(string format, object arg0);
        void DebugFormat(string format, params object[] args);
        void DebugFormat(IFormatProvider provider, string format, params object[] args);
        void DebugFormat(string format, object arg0, object arg1);
        void DebugFormat(string format, object arg0, object arg1, object arg2);
        void Error(object message);
        void Error(object message, Exception exception);
        void ErrorFormat(string format, object arg0);
        void ErrorFormat(string format, params object[] args);
        void ErrorFormat(IFormatProvider provider, string format, params object[] args);
        void ErrorFormat(string format, object arg0, object arg1);
        void ErrorFormat(string format, object arg0, object arg1, object arg2);
        void Fatal(object message);
        void Fatal(object message, Exception exception);
        void FatalFormat(string format, object arg0);
        void FatalFormat(string format, params object[] args);
        void FatalFormat(IFormatProvider provider, string format, params object[] args);
        void FatalFormat(string format, object arg0, object arg1);
        void FatalFormat(string format, object arg0, object arg1, object arg2);
        void Format(Level level, string format, object arg0);
        void Format(Level level, string format, params object[] args);
        void Format(Level level, IFormatProvider provider, string format, params object[] args);
        void Format(Level level, string format, object arg0, object arg1);
        void Format(Level level, string format, object arg0, object arg1, object arg2);
        void Info(object message);
        void Info(object message, Exception exception);
        void InfoFormat(string format, object arg0);
        void InfoFormat(string format, params object[] args);
        void InfoFormat(IFormatProvider provider, string format, params object[] args);
        void InfoFormat(string format, object arg0, object arg1);
        void InfoFormat(string format, object arg0, object arg1, object arg2);
        bool IsEnabled(Level level);
        void Log(Level level, object message);
        void Log(Level level, object message, Exception exception);
        void Trace(object message);
        void Trace(object message, Exception exception);
        void TraceFormat(string format, object arg0);
        void TraceFormat(string format, params object[] args);
        void TraceFormat(IFormatProvider provider, string format, params object[] args);
        void TraceFormat(string format, object arg0, object arg1);
        void TraceFormat(string format, object arg0, object arg1, object arg2);
        void Warn(object message);
        void Warn(object message, Exception exception);
        void WarnFormat(string format, object arg0);
        void WarnFormat(string format, params object[] args);
        void WarnFormat(IFormatProvider provider, string format, params object[] args);
        void WarnFormat(string format, object arg0, object arg1);
        void WarnFormat(string format, object arg0, object arg1, object arg2);
    }
}