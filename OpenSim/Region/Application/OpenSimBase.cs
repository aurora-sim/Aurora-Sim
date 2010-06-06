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
using OpenSim.Framework.Communications;

using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Statistics;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;

namespace OpenSim
{
	/// <summary>
	/// Common OpenSimulator simulator code
	/// </summary>
    public class OpenSimBase : IOpenSimBase
	{
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected string m_startupCommandsFile;
		protected string m_shutdownCommandsFile;
        private string m_TimerScriptFileName = "disabled";
        protected ConfigurationLoader m_configLoader;
        protected ICommandConsole m_console;
        protected OpenSimAppender m_consoleAppender;
        protected IAppender m_logFileAppender = null;
        protected NetworkServersInfo m_networkServersInfo;
        protected DiagnosticsManager m_DiagnosticsManager = null;
        protected BaseHttpServer m_BaseHTTPServer;

        
        protected ConfigSettings m_configSettings;
		public ConfigSettings ConfigurationSettings 
        {
			get { return m_configSettings; }
			set { m_configSettings = value; }
		}

		/// <value>
		/// The config information passed into the OpenSimulator region server.
		/// </value>
        protected IConfigSource m_config;
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

        protected SceneManager m_sceneManager = null;
        public SceneManager SceneManager
        {
            get { return m_sceneManager; }
        }

        /// <summary>
        /// Time at which this server was started
        /// </summary>
        protected DateTime m_StartupTime;
        public DateTime StartupTime
        {
            get { return m_StartupTime; }
        }

        /// <summary>
        /// Holds the non-viewer statistics collection object for this service/server
        /// </summary>
        protected IStatsCollector m_stats;
        public IStatsCollector Stats
        {
            get { return m_stats; }
        }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="configSource"></param>
		public OpenSimBase(IConfigSource configSource)
		{
            m_StartupTime = DateTime.Now;
            m_version = VersionInfo.Version;

            // This thread will go on to become the console listening thread
            System.Threading.Thread.CurrentThread.Name = "ConsoleThread";

            Configuration(configSource);

            SetUpConsole();
            
            RegisterConsoleCommands();
		}

        private void SetUpConsole()
        {
            List<ICommandConsole> Consoles = Aurora.Framework.AuroraModuleLoader.LoadPlugins<ICommandConsole>("/OpenSim/Startup", new ConsolePluginInitialiser("Region", ConfigSource, this));
            m_console = m_applicationRegistry.Get<ICommandConsole>();

            ILoggerRepository repository = LogManager.GetRepository();
            IAppender[] appenders = repository.GetAppenders();
            OpenSimAppender m_consoleAppender = null;
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
            }
            if (m_console == null)
                m_console = new LocalConsole();
            MainConsole.Instance = m_console;
        }

        private void Configuration(IConfigSource configSource)
        {
            m_configLoader = new ConfigurationLoader();
            m_config = m_configLoader.LoadConfigSettings(configSource, out m_configSettings, out m_networkServersInfo);

            IConfig startupConfig = m_config.Configs["Startup"];

            int stpMaxThreads = 15;

            if (startupConfig != null)
            {
                m_startupCommandsFile = startupConfig.GetString("startup_console_commands_file", "startup_commands.txt");
                m_shutdownCommandsFile = startupConfig.GetString("shutdown_console_commands_file", "shutdown_commands.txt");

                m_TimerScriptFileName = startupConfig.GetString("timer_Script", "disabled");
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

                string asyncCallMethodStr = startupConfig.GetString("async_call_method", String.Empty);
                FireAndForgetMethod asyncCallMethod;
                if (!String.IsNullOrEmpty(asyncCallMethodStr) && Utils.EnumTryParse<FireAndForgetMethod>(asyncCallMethodStr, out asyncCallMethod))
                    Util.FireAndForgetMethod = asyncCallMethod;

                stpMaxThreads = startupConfig.GetInt("MaxPoolThreads", 15);
            }

            if (Util.FireAndForgetMethod == FireAndForgetMethod.SmartThreadPool)
                Util.InitThreadPool(stpMaxThreads);
        }

        /// <summary>
        /// Performs initialisation of the scene, such as loading configuration from disk.
        /// </summary>
        public virtual void Startup()
        {
            m_log.Warn("====================================================================");
            m_log.Warn("========================= STARTING AURORA =========================");
            m_log.Warn("====================================================================");
            m_log.Warn("[AURORASTARTUP]: Version: " + Version + "\n");
            m_log.Info("[AURORADATA]: Setting up the data service");

            SetUpHTTPServer();

            //Move out!
            m_stats = StatsManager.StartCollectingSimExtraStats();
            m_DiagnosticsManager = new DiagnosticsManager(m_stats, this);

            //Lets start this after the http server, but before application plugins.
            //Note: this should be moved out.
            m_sceneManager = new SceneManager(this, m_configSettings.StorageDll, m_configSettings.StorageConnectionString, m_configSettings.EstateConnectionString);
            
            ApplicationPluginInitialiser ApplicationPluginManager = new ApplicationPluginInitialiser(this);
            Aurora.Framework.AuroraModuleLoader.LoadPlugins<IApplicationPlugin>("/OpenSim/Startup", ApplicationPluginManager);
            ApplicationPluginManager.PostInitialise();
            
            //Has to be after Scene Manager startup
			AddPluginCommands();

            RunStartupCommands();

            FinishStartUp();

            //Start the prompt
            MainConsole.Instance.ReadConsole();
		}

        private void RunStartupCommands()
        {
            //Run Startup Commands
            if (!String.IsNullOrEmpty(m_startupCommandsFile))
                RunCommandScript(m_startupCommandsFile);

            // Start timer script (run a script every xx seconds)
            if (m_TimerScriptFileName != "disabled")
            {
                Timer m_TimerScriptTimer = new Timer();
                m_TimerScriptTimer.Enabled = true;
                m_TimerScriptTimer.Interval = 1200 * 1000;
                m_TimerScriptTimer.Elapsed += RunAutoTimerScript;
            }
        }

        private void FinishStartUp()
        {
            PrintFileToConsole("startuplogo.txt");

            // For now, start at the 'root' level by default
            if (m_sceneManager.Scenes.Count == 1)
            {
                // If there is only one region, select it
                ChangeSelectedRegion(m_sceneManager.Scenes[0].RegionInfo.RegionName);
            }
            else
            {
                ChangeSelectedRegion("root");
            }

            TimeSpan timeTaken = DateTime.Now - m_StartupTime;

            m_log.InfoFormat("[STARTUP]: Startup is complete and took {0}m {1}s", timeTaken.Minutes, timeTaken.Seconds);
        }

        private void SetUpHTTPServer()
        {
            m_BaseHTTPServer = new BaseHttpServer(
                    m_networkServersInfo.HttpListenerPort, m_networkServersInfo.HttpUsesSSL, m_networkServersInfo.httpSSLPort,
                    m_networkServersInfo.HttpSSLCN);

            if (m_networkServersInfo.HttpUsesSSL && (m_networkServersInfo.HttpListenerPort == m_networkServersInfo.httpSSLPort))
            {
                m_log.Error("[HTTPSERVER]: HTTP Server config failed.   HTTP Server and HTTPS server must be on different ports");
            }

            m_log.InfoFormat("[HTTPSERVER]: Starting HTTP server on port {0}", m_networkServersInfo.HttpListenerPort);
            m_BaseHTTPServer.Start();

            MainServer.Instance = m_BaseHTTPServer;
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
        private void RegisterConsoleCommands()
        {
            //m_console.Commands.AddCommand("region", false, "clear assets", "clear assets", "Clear the asset cache", HandleClearAssets);

            m_console.Commands.AddCommand("region", false, "force update", "force update", "Force the update of all objects on clients", HandleForceUpdate);

            m_console.Commands.AddCommand("region", false, "debug packet", "debug packet <level>", "Turn on packet debugging", Debug);

            m_console.Commands.AddCommand("region", false, "debug scene", "debug scene <cripting> <collisions> <physics>", "Turn on scene debugging", Debug);

            m_console.Commands.AddCommand("region", false, "change region", "change region <region name>", "Change current console region", ChangeSelectedRegion);

            m_console.Commands.AddCommand("region", false, "load xml2", "load xml2", "Load a region's data from XML2 format", LoadXml2);

            m_console.Commands.AddCommand("region", false, "load oar", "load oar [--merge] [--skip-assets] <oar name>", "Load a region's data from OAR archive.  --merge will merge the oar with the existing scene.  --skip-assets will load the oar but ignore the assets it contains", LoadOar);

            m_console.Commands.AddCommand("region", false, "save oar", "save oar <oar name>", "Save a region's data to an OAR archive", "More information on forthcoming options here soon", SaveOar);

            m_console.Commands.AddCommand("region", false, "kick user", "kick user <first> <last> [message]", "Kick a user off the simulator", KickUserCommand);

            m_console.Commands.AddCommand("region", false, "backup", "backup", "Persist objects to the database now", RunCommand);

            m_console.Commands.AddCommand("region", false, "create region", "create region", "Create a new region.", HandleCreateRegion);

            m_console.Commands.AddCommand("region", false, "restart", "restart", "Restart all sims in this instance", RunCommand);

            m_console.Commands.AddCommand("region", false, "command-script", "command-script <script>", "Run a command script from file", RunCommand);

            m_console.Commands.AddCommand("region", false, "remove-region", "remove-region <name>", "Remove a region from this simulator", RunCommand);

            m_console.Commands.AddCommand("region", false, "delete-region", "delete-region <name>", "Delete a region from disk", RunCommand);

            m_console.Commands.AddCommand("region", false, "modules", "modules help", "Info about simulator modules", HandleModules);

            m_console.Commands.AddCommand("region", false, "kill uuid", "kill uuid <UUID>", "Kill an object by UUID", KillUUID);

            m_console.Commands.AddCommand("region", false, "quit", "quit", "Quit the application", HandleQuit);

            m_console.Commands.AddCommand("region", false, "shutdown", "shutdown", "Quit the application", HandleQuit);

            m_console.Commands.AddCommand("region", false, "set log level", "set log level <level>", "Set the console logging level", HandleLogLevel);

            m_console.Commands.AddCommand("region", false, "show", "show", "Shows information about this simulator", HandleShow);
        }

        protected virtual List<string> GetHelpTopics()
        {
            List<string> topics = new List<string>();
            Scene s = SceneManager.CurrentOrFirstScene;
            if (s != null && s.GetCommanders() != null)
                topics.AddRange(s.GetCommanders().Keys);

            return topics;
        }

        private void HandleQuit(string module, string[] args)
        {
            Shutdown();
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

            //Notice(String.Format("Console log level is {0}", m_consoleAppender.Threshold));
        }

		/// <summary>
		/// Kicks users off the region
		/// </summary>
		/// <param name="module"></param>
		/// <param name="cmdparams">name of avatar to kick</param>
		private void KickUserCommand(string module, string[] cmdparams)
		{
            string alert = null;
            IList agents = m_sceneManager.GetCurrentSceneAvatars();

            if (cmdparams.Length < 4)
            {
                if (cmdparams.Length < 3)
                    return;
                if (cmdparams[2] == "all")
                {
                    foreach (ScenePresence presence in agents)
                    {
                        RegionInfo regionInfo = presence.Scene.RegionInfo;

                        MainConsole.Instance.Output(String.Format("Kicking user: {0,-16}{1,-16}{2,-37} in region: {3,-16}", presence.Firstname, presence.Lastname, presence.UUID, regionInfo.RegionName));

                            // kick client...
                            if (alert != null)
                                presence.ControllingClient.Kick(alert);
                            else
                                presence.ControllingClient.Kick("\nThe OpenSim manager kicked you out.\n");

                            // ...and close on our side
                            presence.Scene.IncomingCloseAgent(presence.UUID);
                    }
                }
            }

            if (cmdparams.Length > 4)
                alert = String.Format("\n{0}\n", String.Join(" ", cmdparams, 4, cmdparams.Length - 4));

            foreach (ScenePresence presence in agents)
            {
				RegionInfo regionInfo = presence.Scene.RegionInfo;

				if (presence.Firstname.ToLower().Contains(cmdparams[2].ToLower()) && presence.Lastname.ToLower().Contains(cmdparams[3].ToLower())) {
					MainConsole.Instance.Output(String.Format("Kicking user: {0,-16}{1,-16}{2,-37} in region: {3,-16}", presence.Firstname, presence.Lastname, presence.UUID, regionInfo.RegionName));

					// kick client...
					if (alert != null)
						presence.ControllingClient.Kick(alert);
					else
						presence.ControllingClient.Kick("\nThe OpenSim manager kicked you out.\n");

					// ...and close on our side
					presence.Scene.IncomingCloseAgent(presence.UUID);
				}
			}
			MainConsole.Instance.Output("");
		}

		/// <summary>
		/// Run an optional startup list of commands
		/// </summary>
		/// <param name="fileName"></param>
		private void RunCommandScript(string fileName)
		{
			if (File.Exists(fileName)) {
				m_log.Info("[COMMANDFILE]: Running " + fileName);

				using (StreamReader readFile = File.OpenText(fileName)) {
					string currentCommand;
					while ((currentCommand = readFile.ReadLine()) != null) {
						if (currentCommand != String.Empty) {
							m_log.Info("[COMMANDFILE]: Running '" + currentCommand + "'");
							m_console.RunCommand(currentCommand);
						}
					}
				}
			}
		}

		/// <summary>
		/// Opens a file and uses it as input to the console command parser.
		/// </summary>
		/// <param name="fileName">name of file to use as input to the console</param>
		private static void PrintFileToConsole(string fileName)
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

		private void HandleClearAssets(string module, string[] args)
		{
			MainConsole.Instance.Output("Not implemented.");
		}

		/// <summary>
		/// Force resending of all updates to all clients in active region(s)
		/// </summary>
		/// <param name="module"></param>
		/// <param name="args"></param>
		private void HandleForceUpdate(string module, string[] args)
		{
			MainConsole.Instance.Output("Updating all clients");
			m_sceneManager.ForceCurrentSceneClientUpdate();
		}

		/// <summary>
		/// Creates a new region based on the parameters specified.   This will ask the user questions on the console
		/// </summary>
		/// <param name="module"></param>
		/// <param name="cmd">0,1,region name, region XML file</param>
		private void HandleCreateRegion(string module, string[] cmd)
		{
			RegionLoaderPluginInitialiser RegionLoaderPluginInitialiser = new RegionLoaderPluginInitialiser();
            List<IRegionLoader> regionLoaders = Aurora.Framework.AuroraModuleLoader.LoadPlugins<IRegionLoader>("/OpenSim/Startup", RegionLoaderPluginInitialiser);
            foreach (IRegionLoader loader in regionLoaders)
            {
				loader.AddRegion(this);
			}
		}

		/// <summary>
		/// Load, Unload, and list Region modules in use
		/// </summary>
		/// <param name="module"></param>
		/// <param name="cmd"></param>
		private void HandleModules(string module, string[] cmd)
		{
            List<string> args = new List<string>(cmd);
			args.RemoveAt(0);
			string[] cmdparams = args.ToArray();

            IRegionModulesController controller = m_applicationRegistry.Get<IRegionModulesController>();
            if (cmdparams.Length > 0)
            {
				switch (cmdparams[0].ToLower()) 
                {
                    case "help":
                        MainConsole.Instance.Output("modules list - List modules");
                        MainConsole.Instance.Output("modules unload - Unload a module");
                        break;
					case "list":
                        foreach (IRegionModuleBase irm in controller.AllModules)
                        {
                            if(irm is ISharedRegionModule)
							    MainConsole.Instance.Output(String.Format("Shared region module: {0}", irm.Name));
                            else if (irm is INonSharedRegionModule)
                                MainConsole.Instance.Output(String.Format("Nonshared region module: {0}", irm.Name));
                            else
                                MainConsole.Instance.Output(String.Format("Unknown type " + irm.GetType().ToString() + " region module: {0}", irm.Name));
						}

						break;
					case "unload":
						if (cmdparams.Length > 1)
                        {
                            foreach (IRegionModuleBase irm in controller.AllModules)
                            {
                                if (irm.Name.ToLower() == cmdparams[1].ToLower())
                                {
									MainConsole.Instance.Output(String.Format("Unloading module: {0}", irm.Name));
                                    foreach (Scene scene in m_sceneManager.Scenes)
                                        irm.RemoveRegion(scene);
                                    irm.Close();
								}
							}
						}
						break;
				}
			}
		}

		/// <summary>
		/// Runs commands issued by the server console from the operator
		/// </summary>
		/// <param name="command">The first argument of the parameter (the command)</param>
		/// <param name="cmdparams">Additional arguments passed to the command</param>
		public void RunCommand(string module, string[] cmdparams)
		{
			List<string> args = new List<string>(cmdparams);
			if (args.Count < 1)
				return;

			string command = args[0];
			args.RemoveAt(0);

			cmdparams = args.ToArray();

			switch (command) {
				case "command-script":
					if (cmdparams.Length > 0) {
						RunCommandScript(cmdparams[0]);
					}
					break;

				case "backup":
					m_sceneManager.BackupCurrentScene();
					break;

				case "remove-region":
					string regRemoveName = CombineParams(cmdparams, 0);

					Scene removeScene;
					if (m_sceneManager.TryGetScene(regRemoveName, out removeScene))
                        m_sceneManager.RemoveRegion(removeScene, false);
					else
						MainConsole.Instance.Output("no region with that name");
					break;

				case "delete-region":
					string regDeleteName = CombineParams(cmdparams, 0);

					Scene killScene;
					if (m_sceneManager.TryGetScene(regDeleteName, out killScene))
                        m_sceneManager.RemoveRegion(killScene, true);
					else
						MainConsole.Instance.Output("no region with that name");
					break;

				case "restart":
					m_sceneManager.RestartCurrentScene();
					break;

			}
		}

		/// <summary>
		/// Change the currently selected region.  The selected region is that operated upon by single region commands.
		/// </summary>
		/// <param name="cmdParams"></param>
		protected void ChangeSelectedRegion(string module, string[] cmdparams)
		{
			if (cmdparams.Length > 2)
            {
                string newRegionName = CombineParams(cmdparams, 2);
                ChangeSelectedRegion(newRegionName);
			} 
            else 
            {
				MainConsole.Instance.Output("Usage: change region <region name>");
			}
		}

        protected void ChangeSelectedRegion(string newRegionName)
        {
            if (!m_sceneManager.TrySetCurrentScene(newRegionName))
            {
                MainConsole.Instance.Output(String.Format("Couldn't select region {0}", newRegionName));
                return;
            }
            string regionName = (m_sceneManager.CurrentScene == null ? "root" : m_sceneManager.CurrentScene.RegionInfo.RegionName);
			//MainConsole.Instance.Output(String.Format("Currently selected region is {0}", regionName));
			m_console.DefaultPrompt = String.Format("Region ({0}) ", regionName);
			m_console.ConsoleScene = m_sceneManager.CurrentScene;
        }

		/// <summary>
		/// Turn on some debugging values for OpenSim.
		/// </summary>
		/// <param name="args"></param>
		protected void Debug(string module, string[] args)
		{
			if (args.Length == 1)
				return;

			switch (args[1]) {
				case "packet":
					if (args.Length > 2) {
						int newDebug;
						if (int.TryParse(args[2], out newDebug)) {
							m_sceneManager.SetDebugPacketLevelOnCurrentScene(newDebug);
						} else {
							MainConsole.Instance.Output("packet debug should be 0..255");
						}
						MainConsole.Instance.Output(String.Format("New packet debug: {0}", newDebug));
					}

					break;

				case "scene":
					if (args.Length == 5) {
						if (m_sceneManager.CurrentScene == null) {
							MainConsole.Instance.Output("Please use 'change region <regioname>' first");
						} else {
							bool scriptingOn = !Convert.ToBoolean(args[2]);
							bool collisionsOn = !Convert.ToBoolean(args[3]);
							bool physicsOn = !Convert.ToBoolean(args[4]);
							m_sceneManager.CurrentScene.SetSceneCoreDebug(scriptingOn, collisionsOn, physicsOn);

							MainConsole.Instance.Output(String.Format("Set debug scene scripting = {0}, collisions = {1}, physics = {2}", !scriptingOn, !collisionsOn, !physicsOn));
						}
					} else {
						MainConsole.Instance.Output("debug scene <scripting> <collisions> <physics> (where inside <> is true/false)");
					}

					break;
				default:

					MainConsole.Instance.Output("Unknown debug");
					break;
			}
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
            }
			List<string> args = new List<string>(cmd);
			args.RemoveAt(0);
			string[] showParams = args.ToArray();
            switch (showParams[0]) 
            {
                case "help":
                    MainConsole.Instance.Output("show info - Shows general information about the simulator");
                    MainConsole.Instance.Output("show stats - Show statistics");
                    MainConsole.Instance.Output("show threads - Show thread status");
                    MainConsole.Instance.Output("show uptime - Show server uptime");
                    MainConsole.Instance.Output("show version - Show server version");
                    MainConsole.Instance.Output("show assets - Show asset information");
                    MainConsole.Instance.Output("show connections - Show connection data");
                    MainConsole.Instance.Output("show users - Show all users connected");
                    MainConsole.Instance.Output("show users full - Show all users connected, including child agents");
                    MainConsole.Instance.Output("show regions - Show all regions");
                    MainConsole.Instance.Output("show queues - Show queue info");
                    MainConsole.Instance.Output("show maturity - Show region maturity levels");
                    MainConsole.Instance.Output("set log level [level] - Change the console logging level only.  For example, off or debug.");
                    MainConsole.Instance.Output("show info - Show server information (e.g. startup path).");
                    if (m_stats != null)
                        MainConsole.Instance.Output("show stats - Show statistical information for this server");
                    MainConsole.Instance.Output("show threads - List tracked threads");
                    MainConsole.Instance.Output("show uptime - Show server startup time and uptime.");
                    MainConsole.Instance.Output("show version - Show server version.");
                    break;

                case "info":
                    m_console.Output(("Version: " + m_version));
                    m_console.Output(("Startup directory: " + Environment.CurrentDirectory));
                    break;

                case "stats":
                    if (m_stats != null)
                        m_console.Output((m_stats.Report()));
                    break;

                case "threads":
                    m_console.Output(m_DiagnosticsManager.GetThreadsReport());
                    break;

                case "uptime":
                    m_console.Output(m_DiagnosticsManager.GetUptimeReport());
                    break;

                case "version":
                    m_console.Output((
                        String.Format(
                            "Version: {0} (interface version {1})", m_version, VersionInfo.MajorInterfaceVersion)));
                    break;

				case "assets":
					MainConsole.Instance.Output("Not implemented.");
					break;

				case "users":
					IList agents;
					if (showParams.Length > 1 && showParams[1] == "full") {
						agents = m_sceneManager.GetCurrentScenePresences();
					} else {
						agents = m_sceneManager.GetCurrentSceneAvatars();
					}

					MainConsole.Instance.Output(String.Format("\nAgents connected: {0}\n", agents.Count));

					MainConsole.Instance.Output(String.Format("{0,-16}{1,-16}{2,-37}{3,-11}{4,-16}{5,-30}", "Firstname", "Lastname", "Agent ID", "Root/Child", "Region", "Position"));

					foreach (ScenePresence presence in agents) {
						RegionInfo regionInfo = presence.Scene.RegionInfo;
						string regionName;

						if (regionInfo == null) {
							regionName = "Unresolvable";
						} else {
							regionName = regionInfo.RegionName;
						}

						MainConsole.Instance.Output(String.Format("{0,-16}{1,-16}{2,-37}{3,-11}{4,-16}{5,-30}", presence.Firstname, presence.Lastname, presence.UUID, presence.IsChildAgent ? "Child" : "Root", regionName, presence.AbsolutePosition.ToString()));
					}


					MainConsole.Instance.Output(String.Empty);
					break;

				case "connections":
					System.Text.StringBuilder connections = new System.Text.StringBuilder("Connections:\n");
					m_sceneManager.ForEachScene(delegate(Scene scene) { scene.ForEachClient(delegate(IClientAPI client) { connections.AppendFormat("{0}: {1} ({2}) from {3} on circuit {4}\n", scene.RegionInfo.RegionName, client.Name, client.AgentId, client.RemoteEndPoint, client.CircuitCode); }); });

					MainConsole.Instance.Output(connections.ToString());
					break;

				case "regions":
					m_sceneManager.ForEachScene(delegate(Scene scene) { MainConsole.Instance.Output(String.Format("Region Name: {0}, Region XLoc: {1}, Region YLoc: {2}, Region Port: {3}", scene.RegionInfo.RegionName, scene.RegionInfo.RegionLocX, scene.RegionInfo.RegionLocY, scene.RegionInfo.InternalEndPoint.Port)); });
					break;

				case "queues":
					m_console.Output((GetQueuesReport()));
					break;

				case "maturity":
					m_sceneManager.ForEachScene(delegate(Scene scene) {
						string rating = "";
						if (scene.RegionInfo.RegionSettings.Maturity == 1) {
							rating = "MATURE";
						} else if (scene.RegionInfo.RegionSettings.Maturity == 2) {
							rating = "ADULT";
						} else {
							rating = "PG";
						}
						MainConsole.Instance.Output(String.Format("Region Name: {0}, Region Rating {1}", scene.RegionInfo.RegionName, rating));
					});
					break;
			}
		}

		/// <summary>
		/// print UDP Queue data for each client
		/// </summary>
		/// <returns></returns>
		private string GetQueuesReport()
		{
			string report = String.Empty;
			m_sceneManager.ForEachScene(delegate(Scene scene) { scene.ForEachClient(delegate(IClientAPI client) {if (client is IStatsCollector) {report = report + client.FirstName + " " + client.LastName;IStatsCollector stats = (IStatsCollector)client;report = report + string.Format("{0,7} {1,7} {2,7} {3,7} {4,7} {5,7} {6,7} {7,7} {8,7} {9,7}\n", "Send", "In", "Out", "Resend", "Land", "Wind", "Cloud", "Task", "Texture","Asset");report = report + stats.Report() + "\n";}}); });

			return report;
		}

		/// <summary>
		/// Load region data from Xml2Format
		/// </summary>
		/// <param name="module"></param>
		/// <param name="cmdparams"></param>
		protected void LoadXml2(string module, string[] cmdparams)
		{
			if (cmdparams.Length > 2) {
				try {
					m_sceneManager.LoadCurrentSceneFromXml2(cmdparams[2]);
				} catch (FileNotFoundException) {
					MainConsole.Instance.Output("Specified xml not found. Usage: load xml2 <filename>");
				}
			} else {
				m_log.Warn("Not enough parameters!");
			}
		}

		/// <summary>
		/// Load a whole region from an opensimulator archive.
		/// </summary>
		/// <param name="cmdparams"></param>
		protected void LoadOar(string module, string[] cmdparams)
		{
			try {
				m_sceneManager.LoadArchiveToCurrentScene(cmdparams);
			} catch (Exception e) {
				MainConsole.Instance.Output(e.Message);
			}
		}

		/// <summary>
		/// Save a region to a file, including all the assets needed to restore it.
		/// </summary>
		/// <param name="cmdparams"></param>
		protected void SaveOar(string module, string[] cmdparams)
		{
			m_sceneManager.SaveCurrentSceneToArchive(cmdparams);
		}

		private static string CombineParams(string[] commandParams, int pos)
		{
			string result = String.Empty;
			for (int i = pos; i < commandParams.Length; i++) {
				result += commandParams[i] + " ";
			}
			result = result.TrimEnd(' ');
			return result;
		}

		/// <summary>
		/// Kill an object given its UUID.
		/// </summary>
		/// <param name="cmdparams"></param>
		protected void KillUUID(string module, string[] cmdparams)
		{
			if (cmdparams.Length > 2) {
				UUID id = UUID.Zero;
				SceneObjectGroup grp = null;
				Scene sc = null;

				if (!UUID.TryParse(cmdparams[2], out id)) {
					MainConsole.Instance.Output("[KillUUID]: Error bad UUID format!");
					return;
				}

				m_sceneManager.ForEachScene(delegate(Scene scene) {
					SceneObjectPart part = scene.GetSceneObjectPart(id);
					if (part == null)
						return;

					grp = part.ParentGroup;
					sc = scene;
				});

				if (grp == null) {
					MainConsole.Instance.Output(String.Format("[KillUUID]: Given UUID {0} not found!", id));
				} else {
					MainConsole.Instance.Output(String.Format("[KillUUID]: Found UUID {0} in scene {1}", id, sc.RegionInfo.RegionName));
					try {
						sc.DeleteSceneObject(grp, false);
					} catch (Exception e) {
						m_log.ErrorFormat("[KillUUID]: Error while removing objects from scene: " + e);
					}
				}
			} else {
				MainConsole.Instance.Output("[KillUUID]: Usage: kill uuid <UUID>");
			}
		}

        protected virtual void AddPluginCommands()
        {
            // If console exists add plugin commands.
            if (m_console != null)
            {
                List<string> topics = GetHelpTopics();

                foreach (string topic in topics)
                {
                    m_console.Commands.AddCommand("plugin", false, "help " + topic, "help " + topic, "Get help on plugin command '" + topic + "'", HandleCommanderHelp);

                    m_console.Commands.AddCommand("plugin", false, topic, topic, "Execute subcommand for plugin '" + topic + "'", null);

                    ICommander commander = null;

                    Scene s = SceneManager.CurrentOrFirstScene;

                    if (s != null && s.GetCommanders() != null)
                    {
                        if (s.GetCommanders().ContainsKey(topic))
                            commander = s.GetCommanders()[topic];
                    }

                    if (commander == null)
                        continue;

                    foreach (string command in commander.Commands.Keys)
                    {
                        m_console.Commands.AddCommand(topic, false, topic + " " + command, topic + " " + commander.Commands[command].ShortHelp(), String.Empty, HandleCommanderCommand);
                    }
                }
            }
        }

        private void HandleCommanderCommand(string module, string[] cmd)
        {
            m_sceneManager.SendCommandToPluginModules(cmd);
        }

        private void HandleCommanderHelp(string module, string[] cmd)
        {
            // Only safe for the interactive console, since it won't
            // let us come here unless both scene and commander exist
            //
            ICommander moduleCommander = SceneManager.CurrentOrFirstScene.GetCommander(cmd[1]);
            if (moduleCommander != null)
                m_console.Output(moduleCommander.Help);
        }


        #endregion

        /// <summary>
        /// Should be overriden and referenced by descendents if they need to perform extra shutdown processing
        /// Performs any last-minute sanity checking and shuts down the region server
        /// </summary>
        public virtual void Shutdown()
        {
            if (m_shutdownCommandsFile != String.Empty)
            {
                RunCommandScript(m_shutdownCommandsFile);
            }

            m_log.Info("[SHUTDOWN]: Terminating");

            try
            {
                m_sceneManager.Close();
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[SHUTDOWN]: Ignoring failure during shutdown - {0}", e);
            }

            m_log.Info("[SHUTDOWN]: Shutdown processing on main thread complete.  Exiting...");

            Environment.Exit(0);
        }
    }

    public class DiagnosticsManager
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        IStatsCollector m_stats;
        OpenSimBase m_OpenSimBase;
        
        public DiagnosticsManager(IStatsCollector stats, OpenSimBase baseOpenSim)
        {
            m_OpenSimBase = baseOpenSim;
            m_stats = stats;
            Timer PeriodicDiagnosticsTimer = new Timer(60 * 60 * 1000); // One hour
            PeriodicDiagnosticsTimer.Elapsed += LogDiagnostics;
            PeriodicDiagnosticsTimer.Enabled = true;
            PeriodicDiagnosticsTimer.Start();
        }

        /// <summary>
        /// Print statistics to the logfile, if they are active
        /// </summary>
        private void LogDiagnostics(object source, ElapsedEventArgs e)
        {
            m_log.Debug(LogDiagnostics());
        }

        public string LogDiagnostics()
        {
            StringBuilder sb = new StringBuilder("DIAGNOSTICS\n\n");
            sb.Append(GetUptimeReport());

            if (m_stats != null)
                sb.Append(m_stats.Report());

            sb.Append(Environment.NewLine);
            sb.Append(GetThreadsReport());

            return sb.ToString();
        }

        /// <summary>
        /// Get a report about the registered threads in this server.
        /// </summary>
        public string GetThreadsReport()
        {
            StringBuilder sb = new StringBuilder();

            ProcessThreadCollection threads = ThreadTracker.GetThreads();
            if (threads == null)
            {
                sb.Append("OpenSim thread tracking is only enabled in DEBUG mode.");
            }
            else
            {
                sb.Append(threads.Count + " threads are being tracked:" + Environment.NewLine);
                foreach (ProcessThread t in threads)
                {
                    sb.Append("ID: " + t.Id + ", TotalProcessorTime: " + t.TotalProcessorTime + ", TimeRunning: " +
                        (DateTime.Now - t.StartTime) + ", Pri: " + t.CurrentPriority + ", State: " + t.ThreadState);
                    if (t.ThreadState == System.Diagnostics.ThreadState.Wait)
                        sb.Append(", Reason: " + t.WaitReason + Environment.NewLine);
                    else
                        sb.Append(Environment.NewLine);

                }
            }
            int workers = 0, ports = 0, maxWorkers = 0, maxPorts = 0;
            System.Threading.ThreadPool.GetAvailableThreads(out workers, out ports);
            System.Threading.ThreadPool.GetMaxThreads(out maxWorkers, out maxPorts);

            sb.Append(Environment.NewLine + "*** ThreadPool threads ***" + Environment.NewLine);
            sb.Append("workers: " + (maxWorkers - workers) + " (" + maxWorkers + "); ports: " + (maxPorts - ports) + " (" + maxPorts + ")" + Environment.NewLine);

            return sb.ToString();
        }

        /// <summary>
        /// Return a report about the uptime of this server
        /// </summary>
        /// <returns></returns>
        public string GetUptimeReport()
        {
            StringBuilder sb = new StringBuilder(String.Format("Time now is {0}\n", DateTime.Now));
            sb.Append(String.Format("Server has been running since {0}, {1}\n", m_OpenSimBase.StartupTime.DayOfWeek, m_OpenSimBase.StartupTime));
            sb.Append(String.Format("That is an elapsed time of {0}\n", DateTime.Now - m_OpenSimBase.StartupTime));

            return sb.ToString();
        }
    }
}
