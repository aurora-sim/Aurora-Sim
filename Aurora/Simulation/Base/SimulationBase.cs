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
using OpenSim.Framework.Console;
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
        protected ConfigurationLoader m_configLoader;
        protected ICommandConsole m_console;
        protected OpenSimAppender m_consoleAppender;
        protected IAppender m_logFileAppender = null;
        protected BaseHttpServer m_BaseHTTPServer;

        /// <value>
        /// The config information passed into the OpenSimulator region server.
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

        protected string m_pidFile = String.Empty;

        /// <summary>
        /// Do the initial setup for the application
        /// </summary>
        /// <param name="originalConfig"></param>
        /// <param name="configSource"></param>
        public virtual void Initialize(IConfigSource originalConfig, IConfigSource configSource)
        {
            m_StartupTime = DateTime.Now;
            m_version = VersionInfo.Version + " (" + Util.GetRuntimeInformation() + ")";
            m_original_config = configSource;
            m_config = configSource;

            // This thread will go on to become the console listening thread
            if (System.Threading.Thread.CurrentThread.Name != "ConsoleThread")
                System.Threading.Thread.CurrentThread.Name = "ConsoleThread";
            //Register the interface
            ApplicationRegistry.RegisterInterface<ISimulationBase>(this);

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
                if (m_TimerScriptTime < 5) //Limit for things like backup and etc...
                    m_TimerScriptTime = 5;

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
            m_log.Warn("[AURORASTARTUP]: Version: " + Version + "\n");

            SetUpHTTPServer();

            StartModules();

            //Has to be after Scene Manager startup
            AddPluginCommands();
        }

        /// <summary>
        /// Run the console now that we are all done with startup
        /// </summary>
        public virtual void Run()
        {
            try
            {
                //Start the prompt
                MainConsole.Instance.ReadConsole();
            }
            catch (Exception ex)
            {
                //Only error that ever could occur is the restart one
                Shutdown(false);
                throw ex;
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

            m_console = m_applicationRegistry.Get<ICommandConsole>();
            if (m_console == null)
                m_console = new LocalConsole();
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
                m_console.Output(String.Format("[Console]: Console log level is {0}", m_consoleAppender.Threshold));
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
            m_log.DebugFormat("[SERVER]: Requested port {0}", port);
            if (port == m_Port)
                return HttpServer;
            if (port == 0)
                return HttpServer;

            if (m_Servers.ContainsKey(port))
                return m_Servers[port];

            m_Servers[port] = new BaseHttpServer(port);

            m_log.InfoFormat("[SERVER]: Starting new HTTP server on port {0}", port);
            m_Servers[port].Start();

            return m_Servers[port];
        }

        /// <summary>
        /// Set up the base HTTP server 
        /// </summary>
        public virtual void SetUpHTTPServer()
        {
            m_Port =
                (uint)m_config.Configs["Network"].GetInt("http_listener_port", (int)9000);
            uint httpSSLPort = 9001;
            bool HttpUsesSSL = false;
            string HttpSSLCN = "localhost";
            try
            {
                if (m_config.Configs["SSLConfig"] != null)
                {
                    httpSSLPort =
                        (uint)m_config.Configs["SSLConfig"].GetInt("http_listener_sslport", (int)9001);
                    HttpUsesSSL = m_config.Configs["SSLConfig"].GetBoolean("http_listener_ssl", false);
                    HttpSSLCN = m_config.Configs["SSLConfig"].GetString("http_listener_cn", "localhost");
                }
            }
            catch
            {
            }
            m_BaseHTTPServer = new BaseHttpServer(
                    m_Port, HttpUsesSSL, httpSSLPort,
                    HttpSSLCN);

            if (HttpUsesSSL && (m_Port == httpSSLPort))
            {
                m_log.Error("[HTTPSERVER]: HTTP Server config failed.   HTTP Server and HTTPS server must be on different ports");
            }

            m_log.InfoFormat("[HTTPSERVER]: Starting HTTP server on port {0}", m_Port);
            m_BaseHTTPServer.Start();

            MainServer.Instance = m_BaseHTTPServer;
            m_Servers[m_Port] = m_BaseHTTPServer;
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
            m_console.Commands.AddCommand("region", false, "quit", "quit", "Quit the application", HandleQuit);

            m_console.Commands.AddCommand("region", false, "shutdown", "shutdown", "Quit the application", HandleQuit);

            m_console.Commands.AddCommand("region", false, "set log level", "set log level <level>", "Set the console logging level", HandleLogLevel);

            m_console.Commands.AddCommand("region", false, "show", "show", "Shows information about this simulator", HandleShow);

            m_console.Commands.AddCommand("region", false, "reload config", "reload config", "Reloads .ini file configuration", HandleConfigRefresh);

            m_console.Commands.AddCommand("region", false, "set timer script interval", "set timer script interval", "Set the interval for the timer script (in minutes).", HandleTimerScriptTime);
        }

        private void HandleQuit(string module, string[] args)
        {
            Shutdown(true);
        }

        private void HandleLogLevel(string module, string[] cmd)
        {
            if (null == m_consoleAppender)
            {
                m_console.Output("No appender named Console found (see the log4net config file for this executable)!");
                return;
            }

            string rawLevel = cmd[3];

            ILoggerRepository repository = LogManager.GetRepository();
            Level consoleLevel = repository.LevelMap[rawLevel];

            if (consoleLevel != null)
                m_consoleAppender.Threshold = consoleLevel;
            else
                m_console.Output((
                    String.Format(
                        "{0} is not a valid logging level.  Valid logging levels are ALL, DEBUG, INFO, WARN, ERROR, FATAL, OFF",
                        rawLevel)));

            m_console.Output(String.Format("Console log level is {0}", m_consoleAppender.Threshold));
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

        public virtual void HandleTimerScriptTime(string mod, string[] cmd)
        {
            if (cmd.Length != 5)
            {
                m_log.Warn("[CONSOLE]: Timer Interval command did not have enough parameters.");
                return;
            }
            m_log.Warn("[CONSOLE]: Set Timer Interval to " + cmd[4]);
            m_TimerScriptTime = int.Parse(cmd[4]);
        }

        public virtual void HandleConfigRefresh(string mod, string[] cmd)
        {
            //Rebuild the configs
            ConfigurationLoader loader = new ConfigurationLoader();
            m_config = loader.LoadConfigSettings(m_original_config);
            MainConsole.Instance.Output("Finished reloading configuration.");
        }

        /// <summary>
        /// Many commands list objects for debugging.  Some of the types are listed  here
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="cmd"></param>
        public virtual void HandleShow(string mod, string[] cmd)
        {
            if (cmd.Length == 1)
            {
                m_log.Warn("Incorrect number of parameters!");
                return;
            }
            List<string> args = new List<string>(cmd);
            args.RemoveAt(0);
            string[] showParams = args.ToArray();
            switch (showParams[0])
            {
                case "help":
                    MainConsole.Instance.Output("set log level [level] - Change the console logging level only.  For example, off or debug.");
                    MainConsole.Instance.Output("show info - Show server information (e.g. startup path).");
                    MainConsole.Instance.Output("show threads - List tracked threads");
                    MainConsole.Instance.Output("show uptime - Show server startup time and uptime.");
                    MainConsole.Instance.Output("show version - Show server version.");
                    break;

                case "info":
                    m_console.Output(("Version: " + m_version));
                    m_console.Output(("Startup directory: " + Environment.CurrentDirectory));
                    break;

                case "version":
                    m_console.Output((
                        String.Format(
                            "Version: {0} (interface version {1})", m_version, VersionInfo.MajorInterfaceVersion)));
                    break;
            }
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
