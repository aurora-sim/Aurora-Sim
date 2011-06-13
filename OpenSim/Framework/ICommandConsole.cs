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
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;
using Nini.Config;
using log4net.Core;

namespace OpenSim.Framework
{
    /// <summary>
    /// The console interface
    /// This deals with all things that happen on the console or GUI
    /// </summary>
    public interface ICommandConsole
    {
        /// <summary>
        /// Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        /// All commands that are enabled on this console
        /// </summary>
        Commands Commands { get; set; }

        /// <summary>
        /// Set up this console
        /// </summary>
        /// <param name="defaultPrompt"></param>
        /// <param name="source"></param>
        /// <param name="baseOpenSim"></param>
        void Initialize(string defaultPrompt, IConfigSource source, ISimulationBase baseOpenSim);

        /// <summary>
        /// The text behind the blinking cursor on the console
        /// </summary>
        string DefaultPrompt { get; set; }

        /// <summary>
        /// Locks other threads from inserting text onto the console until the other threads are done
        /// </summary>
        void LockOutput();

        /// <summary>
        /// All finished with inserting text onto the console, let other threads go through
        /// </summary>
        void UnlockOutput ();

        /// <summary>
        /// This is only to be used by the OpenSimAppender, do NOT use unless you have a valid reason!
        /// </summary>
        /// <param name="text">The text that will be shown on the console</param>
        /// <param name="level">The level of output that this text is (determines the color)</param>
        void Output (string text, Level level);

        /// <summary>
        /// This is only to be used by the OpenSimAppender, do NOT use unless you have a valid reason!
        /// </summary>
        /// <param name="text">The text that will be shown on the console</param>
        void Output (string text);

        /// <summary>
        /// Read a line of text from the console, can return ""
        /// </summary>
        /// <param name="prompt">The message that will be displayed to the console before a prompt is started</param>
        /// <returns></returns>
        string CmdPrompt (string prompt);

        /// <summary>
        /// Reads a line of text from the console, and if the user presses enter with no text, defaultReturn is returned for this method
        /// </summary>
        /// <param name="prompt">The message that will be displayed to the console before a prompt is started</param>
        /// <param name="defaultReturn">The default response to return if "" is given</param>
        /// <returns></returns>
        string CmdPrompt(string prompt, string defaultReturn);

        /// <summary>
        /// Reads a line of text from the console, and refuses to read if the line contains any of the excluded characters
        /// </summary>
        /// <param name="prompt">The message that will be displayed to the console before a prompt is started</param>
        /// <param name="excludedCharacters">The characters that cannot be in the response</param>
        /// <returns></returns>
        string CmdPrompt (string prompt, List<char> excludedCharacters);

        /// <summary>
        /// Reads a line of text from the console, excluding the given characters and will return defaultResponse if enter is pressed with ""
        /// </summary>
        /// <param name="prompt">The message that will be displayed to the console before a prompt is started</param>
        /// <param name="defaultResponse">The default response to return if "" is given</param>
        /// <param name="excludedCharacters">The characters that cannot be in the response</param>
        /// <returns></returns>
        string CmdPrompt (string prompt, string defaultResponse, List<char> excludedCharacters);

        /// <summary>
        /// Reads a line of text from the console, returning defaultResponse if no response is given, only the given options are valid entries for the user to use
        /// </summary>
        /// <param name="prompt">The message that will be displayed to the console before a prompt is started</param>
        /// <param name="defaultresponse">The default response to return if "" is given</param>
        /// <param name="options">The options that the user has to select from</param>
        /// <returns></returns>
        string CmdPrompt(string prompt, string defaultresponse, List<string> options);

        /// <summary>
        /// Sets up a prompt for secure information (hides the user text and disallows viewing of the text typed later)
        /// </summary>
        /// <param name="prompt">The message that will be displayed to the console before a prompt is started</param>
        /// <returns></returns>
        string PasswdPrompt (string prompt);

        /// <summary>
        /// Read a line from the console
        /// </summary>
        /// <param name="prompt">The message that will be displayed to the console before a prompt is started</param>
        /// <param name="isCommand">Should this execute a command if it matches a command (runs RunCommand method)</param>
        /// <param name="echo">Should what is typed for this line be able to be seen? (hides what the user types for things like passwords)</param>
        /// <returns></returns>
        string ReadLine (string prompt, bool isCommand, bool echo);

        /// <summary>
        /// Run the given command (acts like it was typed from the console)
        /// </summary>
        /// <param name="cmd"></param>
        void RunCommand(string cmd);

        /// <summary>
        /// The current scene the console is set to use (can be null)
        /// </summary>
        IScene ConsoleScene { get; set; }

        /// <summary>
        /// Starts the prompt for the console. This will never stop until the region is closed.
        /// </summary>
        void ReadConsole();

        /// <summary>
        /// Stops the reading of the console and closes/restarts the console thread 
        /// </summary>
        void EndConsoleProcessing();
    }
}
