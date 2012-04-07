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
using System.Linq;
using System.Reflection;
using Nini.Config;
using log4net.Core;
using System.Threading;
#if NET_4_0
using System.Threading.Tasks;
#endif

namespace Aurora.Framework
{
    public class Commands
    {
        public static bool _ConsoleIsCaseSensitive = true;

        /// <value>
        ///   Commands organized by keyword in a tree
        /// </value>
        private readonly CommandSet tree = new CommandSet();

        /// <summary>
        ///   Get help for the given help string
        /// </summary>
        /// <param name = "helpParts">Parsed parts of the help string.  If empty then general help is returned.</param>
        /// <returns></returns>
        public List<string> GetHelp(string[] cmd)
        {
            return tree.GetHelp(new List<string>(0));
        }

        /// <summary>
        ///   Add a command to those which can be invoked from the console.
        /// </summary>
        /// <param name = "command">The string that will make the command execute</param>
        /// <param name = "commandHelp">The message that will show the user how to use the command</param>
        /// <param name = "info">Any information about how the command works or what it does</param>
        /// <param name = "fn"></param>
        public void AddCommand(string command, string commandHelp, string infomessage, CommandDelegate fn)
        {
            CommandInfo info = new CommandInfo{
                command = command,
                commandHelp = commandHelp,
                info = infomessage,
                fn = new List<CommandDelegate> {fn}
            };
            tree.AddCommand(info);
        }

        public string[] FindNextOption(string[] cmd)
        {
            return tree.FindCommands(cmd);
        }

        public string[] Resolve(string[] cmd)
        {
            return tree.ExecuteCommand(cmd);
        }

        #region Nested type: CommandInfo

        /// <summary>
        ///   Encapsulates a command that can be invoked from the console
        /// </summary>
        private class CommandInfo
        {
            /// <summary>
            ///   The command for this commandinfo
            /// </summary>
            public string command;

            /// <summary>
            ///   The help info for how to use this command
            /// </summary>
            public string commandHelp;

            /// <value>
            ///   The method to invoke for this command
            /// </value>
            public List<CommandDelegate> fn;

            /// <summary>
            ///   Any info about this command
            /// </summary>
            public string info;
        }

        #endregion

        #region Nested type: CommandSet

        private class CommandSet
        {
            private readonly Dictionary<string, CommandInfo> commands = new Dictionary<string, CommandInfo>();
            private readonly Dictionary<string, CommandSet> commandsets = new Dictionary<string, CommandSet>();
            public string Path = "";
            private bool m_allowSubSets = true;
            private string ourPath = "";

            public void Initialize(string path, bool allowSubSets)
            {
                m_allowSubSets = allowSubSets;
                ourPath = path;
                string[] paths = path.Split(' ');
                if (paths.Length != 0)
                {
                    Path = paths[paths.Length - 1];
                }
            }

            public void AddCommand(CommandInfo info)
            {
                if (!_ConsoleIsCaseSensitive) //Force to all lowercase
                {
                    info.command = info.command.ToLower();
                }

                //If our path is "", we can't replace, otherwise we just get ""
                string innerPath = info.command;
                if (ourPath != "")
                {
                    innerPath = info.command.Replace(ourPath, "");
                }
                if (innerPath.StartsWith(" "))
                {
                    innerPath = innerPath.Remove(0, 1);
                }
                string[] commandPath = innerPath.Split(new string[1] {" "}, StringSplitOptions.RemoveEmptyEntries);
                if (commandPath.Length == 1 || !m_allowSubSets)
                {
                    //Only one command after our path, its ours

                    //Add commands together if there is more than one event hooked to one command
                    if (commands.ContainsKey(info.command))
                    {
                        commands[info.command].fn.AddRange(info.fn);
                    }
                    else
                    {
                        commands[info.command] = info;
                    }
                }
                else
                {
                    //Its down the tree somewhere
                    CommandSet downTheTree;
                    if (!commandsets.TryGetValue(commandPath[0], out downTheTree))
                    {
                        //Need to add it to the tree then
                        downTheTree = new CommandSet();
                        downTheTree.Initialize((ourPath == "" ? "" : ourPath + " ") + commandPath[0], false);
                        commandsets.Add(commandPath[0], downTheTree);
                    }
                    downTheTree.AddCommand(info);
                }
            }

            public string[] ExecuteCommand(string[] command)
            {
                if (command.Length != 0)
                {
                    string innerPath = string.Join(" ", command);
                    if (!_ConsoleIsCaseSensitive)
                    {
                        innerPath = innerPath.ToLower();
                    }
                    if (ourPath != "")
                    {
                        innerPath = innerPath.Replace(ourPath, "");
                    }
                    if (innerPath.StartsWith(" "))
                    {
                        innerPath = innerPath.Remove(0, 1);
                    }
                    string[] commandPath = innerPath.Split(new string[1] {" "}, StringSplitOptions.RemoveEmptyEntries);
                    List<string> commandPathList = new List<string>(commandPath);
                    List<string> commandOptions = new List<string>();
                    int i;
                    for (i = commandPath.Length - 1; i >= 0; --i)
                    {
                        if (commandPath[i].Length > 1 && commandPath[i].Substring(0, 2) == "--")
                        {
                            commandOptions.Add(commandPath[i]);
                            commandPathList.RemoveAt(i);
                        }
                        else
                        {
                            break;
                        }
                    }
                    commandOptions.Reverse();
                    commandPath = commandPathList.ToArray();
                    if(commandOptions.Count > 0)
                        MainConsole.Instance.Info("Options: " + string.Join(", ", commandOptions.ToArray()));
                    List<string> cmdList;
                    if (commandPath.Length == 1 || !m_allowSubSets)
                    {
                        for (i = 1; i <= command.Length; i++)
                        {
                            string[] comm = new string[i];
                            Array.Copy(command, comm, i);
                            string com = string.Join(" ", comm);
                            //Only one command after our path, its ours
                            if (commands.ContainsKey(com))
                            {
                                MainConsole.Instance.HasProcessedCurrentCommand = false;
#if (!ISWIN)
                                foreach (CommandDelegate fn in commands[com].fn)
                                {
                                    if (fn != null)
                                    {
                                        cmdList = new List<string>(command);
                                        cmdList.AddRange(commandOptions);
                                        fn(cmdList.ToArray());
                                    }
                                }
#else
                                foreach (CommandDelegate fn in commands[com].fn.Where(fn => fn != null))
                                {
                                    cmdList = new List<string>(command);
                                    cmdList.AddRange(commandOptions);
                                    fn(cmdList.ToArray());
                                }
#endif
                                return new string[0];
                            }
                            else if (commandPath[0] == "help")
                            {
                                List<string> help = GetHelp(commandOptions);

                                foreach (string s in help)
                                {
                                    MainConsole.Instance.Output(s, "Severe");
                                }
                                return new string[0];
                            }
                            else
                            {
#if (!ISWIN)
                                foreach (KeyValuePair<string, CommandInfo> cmd in commands)
                                {
                                    string[] cmdSplit = cmd.Key.Split(' ');
                                    if (cmdSplit.Length == command.Length)
                                    {
                                        bool any = false;
                                        for (int k = 0; k < command.Length; k++)
                                            if (!cmdSplit[k].StartsWith(command[k]))
                                            {
                                                any = true;
                                                break;
                                            }
                                        bool same = !any;
                                        if (same)
                                        {
                                            foreach (CommandDelegate fn in cmd.Value.fn)
                                            {
                                                if (fn != null)
                                                {
                                                    fn(command);
                                                }
                                            }
                                            return new string[0];
                                        }
                                    }
                                }
#else
                                foreach (KeyValuePair<string, CommandInfo> cmd in from cmd in commands let cmdSplit = cmd.Key.Split(' ') where cmdSplit.Length == command.Length let same = !command.Where((t, k) => !cmdSplit[k].StartsWith(t)).Any() where same select cmd)
                                {
                                    foreach (CommandDelegate fn in cmd.Value.fn.Where(fn => fn != null))
                                    {
                                        cmdList = new List<string>(command);
                                        cmdList.AddRange(commandOptions);
                                        fn(cmdList.ToArray());
                                    }
                                    return new string[0];
                                }
#endif
                            }
                        }
                    }
                    else
                    {
                        string cmdToExecute = commandPath[0];
                        if (cmdToExecute == "help")
                        {
                            cmdToExecute = commandPath[1];
                        }
                        if (!_ConsoleIsCaseSensitive)
                        {
                            cmdToExecute = cmdToExecute.ToLower();
                        }
                        //Its down the tree somewhere
                        CommandSet downTheTree;
                        if (commandsets.TryGetValue(cmdToExecute, out downTheTree))
                        {
                            cmdList = new List<string>(commandPath);
                            cmdList.AddRange(commandOptions);
                            return downTheTree.ExecuteCommand(cmdList.ToArray());
                        }
                        else
                        {
                            //See if this is part of a word, and if it is part of a word, execute it
#if (!ISWIN)
                            foreach (KeyValuePair<string, CommandSet> cmd in commandsets)
                            {
                                if (cmd.Key.StartsWith(commandPath[0]))
                                {
                                    cmdList = new List<string>(commandPath);
                                    cmdList.AddRange(commandOptions);
                                    return cmd.Value.ExecuteCommand(cmdList.ToArray());
                                }
                            }
#else
                            foreach (KeyValuePair<string, CommandSet> cmd in commandsets.Where(cmd => cmd.Key.StartsWith(commandPath[0])))
                            {
                                cmdList = new List<string>(commandPath);
                                cmdList.AddRange(commandOptions);
                                return cmd.Value.ExecuteCommand(cmdList.ToArray());
                            }
#endif
                            if (commands.ContainsKey(cmdToExecute))
                            {
#if (!ISWIN)
                                foreach (CommandDelegate fn in commands[cmdToExecute].fn)
                                {
                                    if (fn != null)
                                    {
                                        cmdList = new List<string>(command);
                                        cmdList.AddRange(commandOptions);
                                        fn(cmdList.ToArray());
                                    }
                                }
#else
                                foreach (CommandDelegate fn in commands[cmdToExecute].fn.Where(fn => fn != null))
                                {
                                    cmdList = new List<string>(command);
                                    cmdList.AddRange(commandOptions);
                                    fn(cmdList.ToArray());
                                }
#endif
                                return new string[0];
                            }
                        }
                    }
                }

                return new string[0];
            }

            public string[] FindCommands(string[] command)
            {
                List<string> values = new List<string>();
                if (command.Length != 0)
                {
                    string innerPath = string.Join(" ", command);
                    if (!_ConsoleIsCaseSensitive)
                    {
                        innerPath = innerPath.ToLower();
                    }
                    if (ourPath != "")
                    {
                        innerPath = innerPath.Replace(ourPath, "");
                    }
                    if (innerPath.StartsWith(" "))
                    {
                        innerPath = innerPath.Remove(0, 1);
                    }
                    string[] commandPath = innerPath.Split(new string[1] {" "}, StringSplitOptions.RemoveEmptyEntries);
                    if ((commandPath.Length == 1 || !m_allowSubSets))
                    {
                        string fullcommand = string.Join(" ", command, 0, 2 > command.Length ? command.Length : 2);
                        values.AddRange(from cmd in commands where cmd.Key.StartsWith(fullcommand) select cmd.Value.commandHelp);
                        if (commandPath.Length != 0)
                        {
                            string cmdToExecute = commandPath[0];
                            if (cmdToExecute == "help")
                            {
                                cmdToExecute = commandPath[1];
                            }
                            if (!_ConsoleIsCaseSensitive)
                            {
                                cmdToExecute = cmdToExecute.ToLower();
                            }
                            CommandSet downTheTree;
                            if (commandsets.TryGetValue(cmdToExecute, out downTheTree))
                            {
                                values.AddRange(downTheTree.FindCommands(commandPath));
                            }
                            else
                            {
                                //See if this is part of a word, and if it is part of a word, execute it
#if (!ISWIN)
                                foreach (KeyValuePair<string, CommandSet> cmd in commandsets)
                                {
                                    if (cmd.Key.StartsWith(cmdToExecute))
                                    {
                                        values.AddRange(cmd.Value.FindCommands(commandPath));
                                    }
                                }
#else
                                foreach (KeyValuePair<string, CommandSet> cmd in commandsets.Where(cmd => cmd.Key.StartsWith(cmdToExecute)))
                                {
                                    values.AddRange(cmd.Value.FindCommands(commandPath));
                                }
#endif
                            }
                        }
                    }
                    else if (commandPath.Length != 0)
                    {
                        string cmdToExecute = commandPath[0];
                        if (cmdToExecute == "help")
                        {
                            cmdToExecute = commandPath[1];
                        }
                        if (!_ConsoleIsCaseSensitive)
                        {
                            cmdToExecute = cmdToExecute.ToLower();
                        }
                        //Its down the tree somewhere
                        CommandSet downTheTree;
                        if (commandsets.TryGetValue(cmdToExecute, out downTheTree))
                        {
                            return downTheTree.FindCommands(commandPath);
                        }
                        else
                        {
                            //See if this is part of a word, and if it is part of a word, execute it
#if (!ISWIN)
                            foreach (KeyValuePair<string, CommandSet> cmd in commandsets)
                            {
                                if (cmd.Key.StartsWith(cmdToExecute))
                                {
                                    return cmd.Value.FindCommands(commandPath);
                                }
                            }
#else
                            foreach (KeyValuePair<string, CommandSet> cmd in commandsets.Where(cmd => cmd.Key.StartsWith(cmdToExecute)))
                            {
                                return cmd.Value.FindCommands(commandPath);
                            }
#endif
                        }
                    }
                }

                return values.ToArray();
            }

            public List<string> GetHelp(List<string> options)
            {
                MainConsole.Instance.Info("HTML mode: " + options.Contains("--html"));
                List<string> help = new List<string>();
                if (commandsets.Count != 0)
                {
                    help.Add("");
                    help.Add("------- Help Sets (type the name and help to get more info about that set) -------");
                    help.Add("");
                }
                List<string> paths = new List<string>();
#if (!ISWIN)
                foreach (CommandSet set in commandsets.Values)
                {
                    paths.Add(string.Format("-- Help Set: {0}", set.Path));
                }
#else
                paths.AddRange(commandsets.Values.Select(set => string.Format("-- Help Set: {0}", set.Path)));
#endif
                help.AddRange(StringUtils.AlphanumericSort(paths));
                if (help.Count != 0)
                {
                    help.Add("");
                    help.Add("------- Help options -------");
                    help.Add("");
                }
                paths.Clear();
#if (!ISWIN)
                foreach (CommandInfo command in commands.Values)
                {
                    paths.Add(string.Format("-- {0}  [{1}]:   {2}", command.command, command.commandHelp, command.info));
                }
#else
                paths.AddRange(commands.Values.Select(command => string.Format("-- {0}  [{1}]:   {2}", command.command, command.commandHelp, command.info)));
#endif
                help.AddRange(StringUtils.AlphanumericSort(paths));
                return help;
            }
        }

        #endregion
    }

    public delegate void CommandDelegate(string[] cmd);

    public class Parser
    {
        public static string[] Parse(string text)
        {
            List<string> result = new List<string>();

            int index;

            string[] unquoted = text.Split(new[] {'"'});

            for (index = 0; index < unquoted.Length; index++)
            {
                if (index%2 == 0)
                {
                    string[] words = unquoted[index].Split(new[] {' '});
#if (!ISWIN)
                    foreach (string w in words)
                    {
                        if (w != String.Empty)
                        {
                            result.Add(w);
                        }
                    }
#else
                    result.AddRange(words.Where(w => w != String.Empty));
#endif
                }
                else
                {
                    result.Add(unquoted[index]);
                }
            }

            return result.ToArray();
        }
    }

    /// <summary>
    ///   A console that processes commands internally
    /// </summary>
    public class CommandConsole : BaseConsole, ICommandConsole
    {
        public bool m_isPrompting;
        public int m_lastSetPromptOption;
        public List<string> m_promptOptions = new List<string>();

        public virtual void Initialize(IConfigSource source, ISimulationBase baseOpenSim)
        {
            if (source.Configs["Console"] == null || source.Configs["Console"].GetString("Console", String.Empty) != Name)
            {
                return;
            }

            baseOpenSim.ApplicationRegistry.RegisterModuleInterface<ICommandConsole>(this);
            MainConsole.Instance = this;

            m_Commands.AddCommand("help", "help", "Get a general command list", Help);
        }

        public void Help(string[] cmd)
        {
            List<string> help = m_Commands.GetHelp(cmd);

            foreach (string s in help)
            {
                Output(s, "Severe");
            }
        }

        /// <summary>
        ///   Display a command prompt on the console and wait for user input
        /// </summary>
        public void Prompt()
        {
            // Set this culture for the thread 
            // to en-US to avoid number parsing issues
            Culture.SetCurrentCulture();
            string line = ReadLine(m_defaultPrompt + "# ", true, true);

            if (line != String.Empty && line.Replace(" ", "") != String.Empty) //If there is a space, its fine
            {
                MainConsole.Instance.Info("[CONSOLE] Invalid command");
            }
        }

        public void RunCommand(string cmd)
        {
            string[] parts = Parser.Parse(cmd);
            m_Commands.Resolve(parts);
            Output("");
        }

        public virtual string ReadLine(string p, bool isCommand, bool e)
        {
            string oldDefaultPrompt = m_defaultPrompt;
            m_defaultPrompt = p;
            Console.Write("{0}", p);
            string cmdinput = Console.ReadLine();

            if (isCommand)
            {
                string[] cmd = m_Commands.Resolve(Parser.Parse(cmdinput));

                if (cmd.Length != 0)
                {
                    int i;

                    for (i = 0; i < cmd.Length; i++)
                    {
                        if (cmd[i].Contains(" "))
                        {
                            cmd[i] = "\"" + cmd[i] + "\"";
                        }
                    }
                    return String.Empty;
                }
            }
            m_defaultPrompt = oldDefaultPrompt;
            return cmdinput;
        }

        public string Prompt(string prompt)
        {
            return Prompt(prompt, "");
        }

        public string Prompt(string prompt, string defaultResponse)
        {
            return Prompt(prompt, defaultResponse, new List<string>());
        }

        public string Prompt(string prompt, string defaultResponse, List<char> excludedCharacters)
        {
            return Prompt(prompt, defaultResponse, new List<string>(), excludedCharacters);
        }

        public string Prompt(string prompt, string defaultresponse, List<string> options)
        {
            return Prompt(prompt, defaultresponse, options, new List<char>());
        }

        // Displays a command prompt and returns a default value, user may only enter 1 of 2 options
        public string Prompt(string prompt, string defaultresponse, List<string> options, List<char> excludedCharacters)
        {
            m_isPrompting = true;
            m_promptOptions = new List<string>(options);

            bool itisdone = false;
#if (!ISWIN)
            string optstr = String.Empty;
            foreach (string option in options)
            {
                optstr = optstr + (" " + option);
            }
#else
            string optstr = options.Aggregate(String.Empty, (current, s) => current + (" " + s));
#endif

            string temp = InternalPrompt(prompt, defaultresponse, options);
            while (itisdone == false && options.Count > 0)
            {
                if (options.Contains(temp))
                {
                    itisdone = true;
                }
                else
                {
                    Console.WriteLine("Valid options are" + optstr);
                    temp = InternalPrompt(prompt, defaultresponse, options);
                }
            }
            itisdone = false;
            while (!itisdone && excludedCharacters.Count > 0)
            {
                #if (!ISWIN)
                    bool found = false;
                    foreach (char c in excludedCharacters)
                    {
                        if (temp.Contains(c.ToString()))
                        {
                            Console.WriteLine("The character \"" + c.ToString() + "\" is not permitted.");
                            itisdone = false;
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        itisdone = true;
                    }
                    else
                    {
                        temp = InternalPrompt(prompt, defaultresponse, options);
                    }
#else
                    foreach (char c in excludedCharacters.Where(c => temp.Contains(c.ToString())))
                    {
                        Console.WriteLine("The character \"" + c.ToString() + "\" is not permitted.");
                        itisdone = false;
                    }
#endif
            }
            m_isPrompting = false;
            m_promptOptions.Clear();
            return temp;
        }

        private string InternalPrompt(string prompt, string defaultresponse, List<string> options)
        {
            m_reading = Thread.CurrentThread != m_consoleReadingThread;
            string ret = ReadLine(String.Format("{0}{2} [{1}]: ",
                prompt,
                defaultresponse,
                options.Count == 0 ? "" : ", Options are [" + string.Join(", ", options.ToArray()) + "]"
            ), false, true);
            m_reading = false;
            if (ret == String.Empty)
            {
                ret = defaultresponse;
            }

            return ret;
        }

        // Displays a prompt and waits for the user to enter a string, then returns that string
        // (Done with no echo and suitable for passwords)
        public string PasswordPrompt(string p)
        {
            m_isPrompting = true;
            string line = ReadLine(p + ": ", false, false);
            m_isPrompting = false;
            return line;
        }

        public virtual void Output(string text, string lvl)
        {
            MainConsole.TriggerLog(lvl, text);
            Console.WriteLine(text);
        }

        public virtual void Output(string text)
        {
            Output(text, "Debug");
        }

        public virtual void LockOutput()
        {
        }

        public virtual void UnlockOutput()
        {
        }

        public virtual bool CompareLogLevels(string a, string b)
        {
            Level aa = GetLevel(a);
            Level bb = GetLevel(b);
            return aa <= bb;
        }

        public Level GetLevel(string lvl)
        {
            switch (lvl.ToLower())
            {
                case "alert":
                    return Level.Alert;
                case "all":
                    return Level.All;
                case "critical":
                    return Level.Critical;
                case "debug":
                    return Level.Debug;
                case "emergency":
                    return Level.Emergency;
                case "error":
                    return Level.Error;
                case "fatal":
                    return Level.Fatal;
                case "fine":
                    return Level.Fine;
                case "finer":
                    return Level.Finer;
                case "finest":
                    return Level.Finest;
                case "info":
                    return Level.Info;
                case "notice":
                    return Level.Notice;
                case "none":
                    return new Level(0, "None");
                case "off":
                    return Level.Off;
                case "severe":
                    return Level.Severe;
                case "trace":
                    return Level.Trace;
                case "verbose":
                    return Level.Verbose;
                case "warn":
                    return Level.Warn;
            }
            return null;
        }

        /// <summary>
        ///   The default prompt text.
        /// </summary>
        public virtual string DefaultPrompt
        {
            set { m_defaultPrompt = value; }
            get { return m_defaultPrompt; }
        }

        protected string m_defaultPrompt;

        public virtual string Name
        {
            get { return "CommandConsole"; }
        }

        public Commands m_Commands = new Commands();

        public Commands Commands
        {
            get { return m_Commands; }
            set { m_Commands = value; }
        }

        public IScene ConsoleScene
        {
            get { return m_ConsoleScene; }
            set { m_ConsoleScene = value; }
        }

        public bool HasProcessedCurrentCommand { get; set; }

        public IScene m_ConsoleScene;

        public void Dispose()
        {
        }


        public void EndConsoleProcessing()
        {
            Processing = false;
        }

        public bool Processing = true;
#if !NET_4_0
        private delegate void PromptEvent();

        private IAsyncResult result;
        private PromptEvent action;
        private readonly Object m_consoleLock = new Object();
        private bool m_calledEndInvoke;
        protected static bool m_reading;
        private Thread m_consoleReadingThread;
#endif
        private Thread StartReadingThread()
        {
            Thread t = new Thread(delegate()
            {
                Prompt();
            });
            t.Start();
            return t;
        }

        /// <summary>
        ///   Starts the prompt for the console. This will never stop until the region is closed.
        /// </summary>
        public void ReadConsole()
        {
            while (true)
            {
                if (!Processing)
                {
                    throw new Exception("Restart");
                }
                lock (m_consoleLock)
                {
#if !NET_4_0
                    if(m_consoleReadingThread == null)
                        m_consoleReadingThread = StartReadingThread();
                    try
                    {
                        if (m_reading)
                        {
                            if (m_consoleReadingThread.ThreadState == ThreadState.Running)
                                m_consoleReadingThread.Suspend();
                            continue;
                        }
                        else if (m_consoleReadingThread.ThreadState == ThreadState.Stopped)
                        {
                            m_consoleReadingThread = null;
                            continue;
                        }
                        if (m_consoleReadingThread.Join(1000))
                        {
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        //Eat the exception and go on
                        Output("[Console]: Failed to execute command: " + ex);
                        action = null;
                        result = null;
                    }
#else
                    Task prompt = TaskEx.Run(() => { Prompt(); });
                    while (!Task.WaitAll(new Task[1] { prompt }, 1000))
                    {
                        if (!Processing)
                        {
                            throw new Exception("Restart");
                        }
                    }
#endif
                }
            }
        }
    }
}