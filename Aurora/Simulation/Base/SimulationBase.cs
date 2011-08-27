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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Timers;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;
using log4net.Config;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using Aurora.Framework;

namespace Aurora.Simulation.Base
{
    public class SimulationBase : ISimulationBase
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected string m_startupCommandsFile;
        protected string m_shutdownCommandsFile;
        protected string m_TimerScriptFileName = "disabled";
        protected int m_TimerScriptTime = 20;
        protected ICommandConsole m_console;
        protected OpenSimAppender m_consoleAppender;
        protected IAppender m_logFileAppender = null;
        protected IHttpServer m_BaseHTTPServer;
        protected Timer m_TimerScriptTimer;
        protected ConfigurationLoader m_configurationLoader;

        /// <value>
        /// The config information passed into the Aurora server.
        /// </value>
        protected IConfigSource m_config;
        protected IConfigSource m_original_config;
        public IConfigSource ConfigSource
        {
            get { return m_config; }
            set { m_config = value; }
        }

        /// <summary>
        /// Server version information.  Usually VersionInfo + information about git commit, operating system, etc.
        /// </summary>
        protected string m_version;
        public string Version
        {
            get { return m_version; }
        }

        protected IRegistryCore m_applicationRegistry = new RegistryCore();
        public IRegistryCore ApplicationRegistry
        {
            get { return m_applicationRegistry; }
        }

        protected AuroraEventManager m_eventManager = new AuroraEventManager();
        public AuroraEventManager EventManager
        {
            get { return m_eventManager; }
        }

        /// <summary>
        /// Time at which this server was started
        /// </summary>
        protected DateTime m_StartupTime;
        public DateTime StartupTime
        {
            get { return m_StartupTime; }
        }

        protected List<IApplicationPlugin> m_applicationPlugins = new List<IApplicationPlugin>();

        public IHttpServer HttpServer
        {
            get { return m_BaseHTTPServer; }
        }

        protected Dictionary<uint, BaseHttpServer> m_Servers =
            new Dictionary<uint, BaseHttpServer>();

        protected uint m_Port;
        public uint DefaultPort
        {
            get { return m_Port; }
        }

        protected string[] m_commandLineParameters = null;
        public string[] CommandLineParameters
        {
             get { return m_commandLineParameters; }
        }

        protected string m_pidFile = String.Empty;

        /// <summary>
        /// Do the initial setup for the application
        /// </summary>
        /// <param name="originalConfig"></param>
        /// <param name="configSource"></param>
        public virtual void Initialize(IConfigSource originalConfig, IConfigSource configSource, string[] cmdParams, ConfigurationLoader configLoader)
        {
            m_commandLineParameters = cmdParams;
            m_StartupTime = DateTime.Now;
            m_version = VersionInfo.Version + " (" + Util.GetRuntimeInformation() + ")";
            m_original_config = originalConfig;
            m_config = configSource;
            m_configurationLoader = configLoader;

            // This thread will go on to become the console listening thread
            if (System.Threading.Thread.CurrentThread.Name != "ConsoleThread")
                System.Threading.Thread.CurrentThread.Name = "ConsoleThread";
            //Register the interface
            ApplicationRegistry.RegisterModuleInterface<ISimulationBase>(this);

            Configuration(configSource);

            SetUpConsole();

            RegisterConsoleCommands();
        }

        /// <summary>
        /// Read the configuration
        /// </summary>
        /// <param name="configSource"></param>
        public virtual void Configuration(IConfigSource configSource)
        {
            IConfig startupConfig = m_config.Configs["Startup"];

            int stpMaxThreads = 15;

            if (startupConfig != null)
            {
                m_startupCommandsFile = startupConfig.GetString("startup_console_commands_file", "startup_commands.txt");
                m_shutdownCommandsFile = startupConfig.GetString("shutdown_console_commands_file", "shutdown_commands.txt");

                m_TimerScriptFileName = startupConfig.GetString("timer_Script", "disabled");
                m_TimerScriptTime = startupConfig.GetInt("timer_time", m_TimerScriptTime);

                string pidFile = startupConfig.GetString("PIDFile", String.Empty);
                if (pidFile != String.Empty)
                    CreatePIDFile(pidFile);
            }

            IConfig SystemConfig = m_config.Configs["System"];
            if (SystemConfig != null)
            {
                string asyncCallMethodStr = SystemConfig.GetString("AsyncCallMethod", String.Empty);
                FireAndForgetMethod asyncCallMethod;
                if (!String.IsNullOrEmpty(asyncCallMethodStr) && Utils.EnumTryParse<FireAndForgetMethod>(asyncCallMethodStr, out asyncCallMethod))
                    Util.FireAndForgetMethod = asyncCallMethod;

                stpMaxThreads = SystemConfig.GetInt("MaxPoolThreads", 15);
            }

            if (Util.FireAndForgetMethod == FireAndForgetMethod.SmartThreadPool)
                Util.InitThreadPool(stpMaxThreads);
        }

        /// <summary>
        /// Performs initialisation of the application, such as loading the HTTP server and modules
        /// </summary>
        public virtual void Startup()
        {
            m_log.Warn("====================================================================");
            m_log.Warn("========================= STARTING AURORA =========================");
            m_log.Warn("====================================================================");
            m_log.Warn("[AuroraStartup]: Version: " + Version + "\n");

            SetUpHTTPServer();

            StartModules();

            //Has to be after Scene Manager startup
            AddPluginCommands();
        }

        public virtual ISimulationBase Copy()
        {
            return new SimulationBase();
        }

        /// <summary>
        /// Run the console now that we are all done with startup
        /// </summary>
        public virtual void Run()
        {
            while (true)
            {
                try
                {
                    //Start the prompt
                    if (MainConsole.Instance != null)
                    {
                        MainConsole.Instance.ReadConsole();
                    }
                }
                catch (Exception ex)
                {
                    //Only error that ever could occur is the restart one
                    m_log.InfoFormat("[Console]: Exception {0}", ex.Message);
                    m_log.InfoFormat("[Console]: App {0}", ex.Source);
                    m_log.InfoFormat("[Console]: tgt {0}", ex.TargetSite);
                    Shutdown(true);
                    throw ex;
                }
            }
        }

        public virtual void AddPluginCommands()
        {
        }

        /// <summary>
        /// Find the console plugin and initialize the logger for it
        /// </summary>
        public virtual void SetUpConsole()
        {
            List<ICommandConsole> Plugins = AuroraModuleLoader.PickupModules<ICommandConsole>();
            foreach (ICommandConsole plugin in Plugins)
            {
                plugin.Initialize("Region", ConfigSource, this);
            }

            m_console = m_applicationRegistry.RequestModuleInterface<ICommandConsole>();
            if (m_console == null)
                m_log.Info("[Console]: No Console located");
            ILoggerRepository repository = LogManager.GetRepository();
            IAppender[] appenders = repository.GetAppenders();
            foreach (IAppender appender in appenders)
            {
                if (appender.Name == "Console")
                {
                    m_consoleAppender = (OpenSimAppender)appender;
                    break;
                }
            }

            foreach (IAppender appender in appenders)
            {
                if (appender.Name == "LogFileAppender")
                {
                    m_logFileAppender = appender;
                }
            }

            if (null != m_consoleAppender)
            {
                m_consoleAppender.Console = m_console;
                // If there is no threshold set then the threshold is effectively everything.
                if (null == m_consoleAppender.Threshold)
                    m_consoleAppender.Threshold = Level.All;
                repository.Threshold = m_consoleAppender.Threshold;
                foreach (ILogger log in repository.GetCurrentLoggers())
                {
                    log.Level = m_consoleAppender.Threshold;
                }
                m_log.Fatal (String.Format ("[Console]: Console log level is {0}", m_consoleAppender.Threshold));
            }

            IConfig startupConfig = m_config.Configs["Startup"];
            if (m_logFileAppender != null)
            {
                if (m_logFileAppender is log4net.Appender.FileAppender)
                {
                    log4net.Appender.FileAppender appender = (log4net.Appender.FileAppender)m_logFileAppender;
                    string fileName = startupConfig.GetString("LogFile", String.Empty);
                    if (fileName != String.Empty)
                    {
                        appender.File = fileName;
                        appender.ActivateOptions();
                    }
                }
            }

            MainConsole.Instance = m_console;
        }

        /// <summary>
        /// Get an HTTPServer on the given port. It will create one if one does not exist
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public IHttpServer GetHttpServer(uint port)
        {
            return GetHttpServer(port, false);
        }

        public IHttpServer GetHttpServer (uint port, bool secure)
        {
            if ((port == m_Port || port == 0) && HttpServer != null)
                return HttpServer;

            BaseHttpServer server;
            if(m_Servers.TryGetValue(port, out server) && server.Secure == secure)
                return server;

            string hostName =
                m_config.Configs["Network"].GetString("HostName", "http" + (secure ? "s" : "") + "://127.0.0.1");
            //Clean it up a bit
            if (!hostName.StartsWith ("http://") && !hostName.StartsWith ("https://"))
                hostName = (secure ? "https://" : "http://") + hostName;
            if (hostName.EndsWith ("/"))
                hostName = hostName.Remove (hostName.Length - 1, 1);

            server = new BaseHttpServer(port, hostName, secure);

            try
            {
                server.Start();
            }
            catch(Exception ex)
            {
                //Remove the server from the list
                m_Servers.Remove (port);
                //Then pass the exception upwards
                throw ex;
            }

            return (m_Servers[port] = server);
        }

        /// <summary>
        /// Set up the base HTTP server 
        /// </summary>
        public virtual void SetUpHTTPServer()
        {
            m_Port = m_config.Configs["Network"].GetUInt("http_listener_port", 9000);
            bool useHTTPS = m_config.Configs["Network"].GetBoolean("use_https", false);
            m_BaseHTTPServer = GetHttpServer(m_Port, useHTTPS);
            MainServer.Instance = m_BaseHTTPServer;
        }

        /// <summary>
        /// Start the application modules
        /// </summary>
        public virtual void StartModules()
        {
            m_applicationPlugins = AuroraModuleLoader.PickupModules<IApplicationPlugin>();
            foreach (IApplicationPlugin plugin in m_applicationPlugins)
            {
                plugin.Initialize(this);
            }

            foreach (IApplicationPlugin plugin in m_applicationPlugins)
            {
                plugin.PostInitialise();
            }

            foreach (IApplicationPlugin plugin in m_applicationPlugins)
            {
                plugin.Start();
            }

            foreach (IApplicationPlugin plugin in m_applicationPlugins)
            {
                plugin.PostStart();
            }
        }

        /// <summary>
        /// Close all the Application Plugins
        /// </summary>
        public virtual void CloseModules()
        {
            foreach (IApplicationPlugin plugin in m_applicationPlugins)
            {
                plugin.Close();
            }
        }

        /// <summary>
        /// Run the commands given now that startup is complete
        /// </summary>
        public void RunStartupCommands()
        {
            //Draw the file on the console
            PrintFileToConsole("startuplogo.txt");
            //Run Startup Commands
            if (!String.IsNullOrEmpty(m_startupCommandsFile))
                RunCommandScript(m_startupCommandsFile);

            // Start timer script (run a script every xx seconds)
            if (m_TimerScriptFileName != "disabled")
            {
                Timer m_TimerScriptTimer = new Timer();
                m_TimerScriptTimer.Enabled = true;
                m_TimerScriptTimer.Interval = m_TimerScriptTime * 60 * 1000;
                m_TimerScriptTimer.Elapsed += RunAutoTimerScript;
            }
        }

        /// <summary>
        /// Opens a file and uses it as input to the console command parser.
        /// </summary>
        /// <param name="fileName">name of file to use as input to the console</param>
        private void PrintFileToConsole(string fileName)
        {
            if (File.Exists(fileName))
            {
                StreamReader readFile = File.OpenText(fileName);
                string currentLine;
                while ((currentLine = readFile.ReadLine()) != null)
                {
                    m_log.Info("[!]" + currentLine);
                }
            }
        }

        /// <summary>
        /// Timer to run a specific text file as console commands.
        /// Configured in in the main .ini file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunAutoTimerScript(object sender, EventArgs e)
        {
            RunCommandScript(m_TimerScriptFileName);
        }

        #region Console Commands

        /// <summary>
        /// Register standard set of region console commands
        /// </summary>
        public virtual void RegisterConsoleCommands()
        {
            if (m_console == null)
                return;
            m_console.Commands.AddCommand ("quit", "quit", "Quit the application", HandleQuit);

            m_console.Commands.AddCommand ("shutdown", "shutdown", "Quit the application", HandleQuit);

            m_console.Commands.AddCommand ("set log level", "set log level [level]", "Set the console logging level", HandleLogLevel);

            m_console.Commands.AddCommand ("show info", "show info", "Show server information (e.g. startup path)", HandleShowInfo);
            m_console.Commands.AddCommand ("show version", "show version", "Show server version", HandleShowVersion);

            m_console.Commands.AddCommand ("reload config", "reload config", "Reloads .ini file configuration", HandleConfigRefresh);

            m_console.Commands.AddCommand ("set timer script interval", "set timer script interval", "Set the interval for the timer script (in minutes).", HandleTimerScriptTime);

            m_console.Commands.AddCommand ("force GC", "force GC", "Forces garbage collection.", HandleForceGC);
        }

        private void HandleQuit(string[] args)
        {
            Shutdown(true);
        }

        private void HandleLogLevel(string[] cmd)
        {
            if (null == m_consoleAppender)
            {
                m_log.Fatal ("No appender named Console found (see the log4net config file for this executable)!");
                return;
            }

            string rawLevel = cmd[3];

            ILoggerRepository repository = LogManager.GetRepository();
            Level consoleLevel = repository.LevelMap[rawLevel];
            if (consoleLevel != null)
            {
                m_consoleAppender.Threshold = consoleLevel;
                repository.Threshold = consoleLevel;
                foreach (ILogger log in repository.GetCurrentLoggers())
                {
                    log.Level = consoleLevel;
                }
            }
            else
            {
                string forms = "";
                for (int i = 0; i < repository.LevelMap.AllLevels.Count; i++)
                {
                    forms += repository.LevelMap.AllLevels[i].Name;
                    if (i + 1 != repository.LevelMap.AllLevels.Count)
                        forms += ", ";
                }
                m_log.Fatal(
                    String.Format(
                        "{0} is not a valid logging level.  Valid logging levels are " + forms,
                        rawLevel));
            }
            m_log.Fatal (String.Format ("Console log level is {0}", m_consoleAppender.Threshold));
        }

        /// <summary>
        /// Run an optional startup list of commands
        /// </summary>
        /// <param name="fileName"></param>
        public virtual void RunCommandScript(string fileName)
        {
            if (File.Exists(fileName))
            {
                m_log.Info("[COMMANDFILE]: Running " + fileName);
                List<string> commands = new List<string>();
                using (StreamReader readFile = File.OpenText(fileName))
                {
                    string currentCommand;
                    while ((currentCommand = readFile.ReadLine()) != null)
                    {
                        if (currentCommand != String.Empty)
                        {
                            commands.Add(currentCommand);
                        }
                    }
                }
                foreach (string currentCommand in commands)
                {
                    m_log.Info("[COMMANDFILE]: Running '" + currentCommand + "'");
                    m_console.RunCommand(currentCommand);
                }
            }
        }

        public virtual void HandleForceGC(string[] cmd)
        {
            GC.Collect();
            m_log.Warn("Garbage collection finished");
        }

        public virtual void HandleTimerScriptTime(string[] cmd)
        {
            if (cmd.Length != 5)
            {
                m_log.Warn("[CONSOLE]: Timer Interval command did not have enough parameters.");
                return;
            }
            m_log.Warn("[CONSOLE]: Set Timer Interval to " + cmd[4]);
            m_TimerScriptTime = int.Parse(cmd[4]);
            m_TimerScriptTimer.Enabled = false;
            m_TimerScriptTimer.Interval = m_TimerScriptTime * 60 * 1000;
            m_TimerScriptTimer.Enabled = true;
        }

        public virtual void HandleConfigRefresh(string[] cmd)
        {
            //Rebuild the configs
            m_config = m_configurationLoader.LoadConfigSettings (m_original_config);
            foreach (IApplicationPlugin plugin in m_applicationPlugins)
            {
                plugin.ReloadConfiguration(m_config);
            }
            string hostName =
                m_config.Configs["Network"].GetString("HostName", "http://127.0.0.1");
            //Clean it up a bit
            hostName.Replace("http://", "");
            hostName.Replace("https://", "");
            if(hostName.EndsWith("/"))
                hostName = hostName.Remove(hostName.Length - 1, 1);
            foreach(IHttpServer server in m_Servers.Values)
            {
                server.HostName = (server.Secure ? "https://" : "http://") + hostName;
            }
            m_log.Info ("Finished reloading configuration.");
        }

        public virtual void HandleShowInfo (string[] cmd)
        {
            m_log.Info ("Version: " + m_version);
            m_log.Info ("Startup directory: " + Environment.CurrentDirectory);
        }

        public virtual void HandleShowVersion (string[] cmd)
        {
            m_log.Info (
                String.Format (
                    "Version: {0} (interface version {1})", m_version, VersionInfo.MajorInterfaceVersion));
        }

        #endregion

        /// <summary>
        /// Should be overriden and referenced by descendents if they need to perform extra shutdown processing
        /// Performs any last-minute sanity checking and shuts down the region server
        /// </summary>
        public virtual void Shutdown(bool close)
        {
            try
            {
                try
                {
                    RemovePIDFile();
                    if (m_shutdownCommandsFile != String.Empty)
                    {
                        RunCommandScript(m_shutdownCommandsFile);
                    }
                }
                catch
                {
                    //It doesn't matter, just shut down
                }
                try
                {
                    //Close out all the modules
                    CloseModules();
                }
                catch
                {
                    //Just shut down already
                }
                try
                {
                    //Close the thread pool
                    Util.CloseThreadPool();
                }
                catch
                {
                    //Just shut down already
                }
                try
                {
                    //Stop the HTTP server(s)
                    foreach (BaseHttpServer server in m_Servers.Values)
                    {
                        server.Stop();
                    }
                }
                catch
                {
                    //Again, just shut down
                }

                if (close)
                    m_log.Info("[SHUTDOWN]: Terminating");

                m_log.Info("[SHUTDOWN]: Shutdown processing on main thread complete. " + (close ? " Exiting..." : ""));

                if (close)
                    Environment.Exit(0);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Write the PID file to the hard drive
        /// </summary>
        /// <param name="path"></param>
        protected void CreatePIDFile(string path)
        {
            try
            {
                string pidstring = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
                FileStream fs = File.Create(path);
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                Byte[] buf = enc.GetBytes(pidstring);
                fs.Write(buf, 0, buf.Length);
                fs.Close();
                m_pidFile = path;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Delete the PID file now that we are done running
        /// </summary>
        protected void RemovePIDFile()
        {
            if (m_pidFile != String.Empty)
            {
                try
                {
                    File.Delete(m_pidFile);
                    m_pidFile = String.Empty;
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
