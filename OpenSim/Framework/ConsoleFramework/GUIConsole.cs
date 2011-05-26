using System;
using System.Linq;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
#if NET_4_0
using System.Threading.Tasks;
#endif
using log4net;
using Nini.Config;
using log4net.Core;

namespace OpenSim.Framework
{
    /// <summary>
    /// This is a special class designed to take over control of the command console prompt of
    /// the server instance to allow for the input and output to the server to be redirected
    /// by an external application, in this case a GUI based application on Windows.
    /// </summary>
    public class GUIConsole : ICommandConsole
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public bool m_isPrompting = false;
        public int m_lastSetPromptOption = 0;
        public List<string> m_promptOptions = new List<string>();

        public virtual void Initialize(string defaultPrompt, IConfigSource source, ISimulationBase baseOpenSim)
        {
            if (source.Configs["Console"] != null)
            {
                if (source.Configs["Console"].GetString("Console", String.Empty) != Name)
                    return;
            }
            else
                return;

            baseOpenSim.ApplicationRegistry.RegisterModuleInterface<ICommandConsole>(this);

            m_Commands.AddCommand("help", "help",
                    "Get a general command list", Help);

            m_log.Info("[GUIConsole] initialised.");
        }

        public void Help(string module, string[] cmd)
        {
            List<string> help = m_Commands.GetHelp(cmd);

            foreach (string s in help)
                Output(s, Level.Severe);
        }

        /// <summary>
        /// Display a command prompt on the console and wait for user input
        /// </summary>
        public void Prompt()
        {
            // Set this culture for the thread 
            // to en-US to avoid number parsing issues
            OpenSim.Framework.Culture.SetCurrentCulture();
            string line = ReadLine(m_defaultPrompt + "# ", true, true);

            result.AsyncWaitHandle.WaitOne(-1);

//            if (line != String.Empty && line.Replace(" ", "") != String.Empty) //If there is a space, its fine
//            {
//                m_log.Info("[GUICONSOLE] Invalid command");
//            }
        }

        public void RunCommand(string cmd)
        {
            string[] parts = Parser.Parse(cmd);
            m_Commands.Resolve(parts);
            Output("");
        }

        /// <summary>
        /// Method that reads a line of text from the user.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="isCommand"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public virtual string ReadLine(string p, bool isCommand, bool e)
        {
            string oldDefaultPrompt = m_defaultPrompt;
            m_defaultPrompt = p;
//            System.Console.Write("{0}", p);
            string cmdinput = System.Console.ReadLine();

            if (isCommand)
            {
                string[] cmd = m_Commands.Resolve(Parser.Parse(cmdinput));

                if (cmd.Length != 0)
                {
                    int i;

                    for (i = 0; i < cmd.Length; i++)
                    {
                        if (cmd[i].Contains(" "))
                            cmd[i] = "\"" + cmd[i] + "\"";
                    }
                    return String.Empty;
                }
            }
            m_defaultPrompt = oldDefaultPrompt;
            return cmdinput;
        }

        public string CmdPrompt(string p)
        {
            m_isPrompting = true;
            string line = ReadLine(String.Format("{0}: ", p), false, true);
            m_isPrompting = false;
            return line;
        }

        public string CmdPrompt(string p, string def)
        {
            m_isPrompting = true;
            string ret = ReadLine(String.Format("{0} [{1}]: ", p, def), false, true);
            if (ret == String.Empty)
                ret = def;

            m_isPrompting = false;
            return ret;
        }

        public string CmdPrompt(string p, List<char> excludedCharacters)
        {
            m_isPrompting = true;

            bool itisdone = false;
            string ret = String.Empty;
            while (!itisdone)
            {
                itisdone = true;
                ret = CmdPrompt(p);

                foreach (char c in excludedCharacters)
                {
                    if (ret.Contains(c.ToString()))
                    {
                        System.Console.WriteLine("The character \"" + c.ToString() + "\" is not permitted.");
                        itisdone = false;
                    }
                }
            }

            m_isPrompting = false;
            return ret;
        }

        public string CmdPrompt(string p, string def, List<char> excludedCharacters)
        {
            m_isPrompting = true;
            bool itisdone = false;
            string ret = String.Empty;
            while (!itisdone)
            {
                itisdone = true;
                ret = CmdPrompt(p, def);

                if (ret == String.Empty)
                {
                    ret = def;
                }
                else
                {
                    foreach (char c in excludedCharacters)
                    {
                        if (ret.Contains(c.ToString()))
                        {
                            System.Console.WriteLine("The character \"" + c.ToString() + "\" is not permitted.");
                            itisdone = false;
                        }
                    }
                }
            }
            m_isPrompting = false;

            return ret;
        }

        // Displays a command prompt and returns a default value, user may only enter 1 of 2 options
        public string CmdPrompt(string prompt, string defaultresponse, List<string> options)
        {
            m_isPrompting = true;
            m_promptOptions = new List<string>(options);

            bool itisdone = false;
            string optstr = String.Empty;
            foreach (string s in options)
                optstr += " " + s;

            string temp = CmdPrompt(prompt, defaultresponse);
            while (itisdone == false)
            {
                if (options.Contains(temp))
                {
                    itisdone = true;
                }
                else
                {
                    System.Console.WriteLine("Valid options are" + optstr);
                    temp = CmdPrompt(prompt, defaultresponse);
                }
            }
            m_isPrompting = false;
            m_promptOptions.Clear();
            return temp;
        }

        // Displays a prompt and waits for the user to enter a string, then returns that string
        // (Done with no echo and suitable for passwords)
        public string PasswdPrompt(string p)
        {
            m_isPrompting = true;
            string line = ReadLine(p + ": ", false, false);
            m_isPrompting = false;
            return line;
        }

        public virtual void Output(string text, Level level)
        {
            Output(text);
        }

        public virtual void Output(string text)
        {
            Log(text);
            System.Console.WriteLine(text);
        }

        public virtual void Log(string text)
        {

        }

        public virtual void LockOutput()
        {
        }

        public virtual void UnlockOutput()
        {
        }

        /// <summary>
        /// The default prompt text.
        /// </summary>
        public string DefaultPrompt
        {
            set { m_defaultPrompt = value; }
            get { return m_defaultPrompt; }
        }
        protected string m_defaultPrompt;

        public virtual string Name
        {
            get { return "GUIConsole"; }
        }

        public Commands m_Commands = new Commands();

        public Commands Commands
        {
            get
            {
                return m_Commands;
            }
            set
            {
                m_Commands = value;
            }
        }

        public IScene ConsoleScene
        {
            get
            {
                return m_ConsoleScene;
            }
            set
            {
                m_ConsoleScene = value;
            }
        }
        public IScene m_ConsoleScene = null;

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
        private IAsyncResult result = null;
        private PromptEvent action = null;
        private Object m_consoleLock = new Object();
        private bool m_calledEndInvoke = false;
#endif

        /// <summary>
        /// Starts the prompt for the console. This will never stop until the region is closed.
        /// </summary>
        public void ReadConsole()
        {
            Process curProc = Process.GetCurrentProcess();
            WaitHandle[] wHandles = new WaitHandle[0];
            wHandles[0] = result.AsyncWaitHandle;
            m_log.Info("[GUIConsole]: ReadConsole .");
            m_log.InfoFormat("[CUIConsole]: {0}", curProc.MainModule.ModuleName);
            while (true)
            {
#if !NET_4_0
                if (!Processing)
                {
                    throw new Exception("Restart");
                }
                lock (m_consoleLock)
                {
                    if (action == null)
                    {
                        action = Prompt;
                        result = action.BeginInvoke(null, null);
                        m_calledEndInvoke = false;
                    }

                    try
                    {
                        if ((!result.IsCompleted) &&
                            (!result.AsyncWaitHandle.WaitOne(1000) || !result.IsCompleted))
                        {
                            m_log.Info("[GUIConsole]: Sleeping 60 secs");
                            System.Threading.WaitHandle.WaitAny(wHandles);
                            m_log.Info("[GUIConsole]: Yawn");
                        }
                        else if (action != null && !result.CompletedSynchronously && 
                            result.IsCompleted && !m_calledEndInvoke)
                        {
                            m_calledEndInvoke = true;
                            action.EndInvoke(result);
                            action = null;
                            result = null;
                            m_log.Info("[GUIConsole]: acting.");
                        }
                    }
                    catch (Exception ex)
                    {
                        //Eat the exception and go on
                        Output("[GUIConsole]: Failed to execute command: " + ex.ToString());
                        action = null;
                        result = null;
                    }
                }
#else
                Task prompt = TaskEx.Run(() => { Prompt(); });
                if (!Processing)
                    throw new Exception("Restart");
                while (!Task.WaitAll(new Task[1] { prompt }, 1000))
                {
                    if (!Processing)
                        throw new Exception("Restart");
                }
#endif
            }
        }
    }
}
