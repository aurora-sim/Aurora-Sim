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
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Timers;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Communications;

using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Statistics;
using OpenSim.Region.ClientStack;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;

namespace OpenSim
{
    /// <summary>
    /// Common OpenSimulator simulator code
    /// </summary>
    public class OpenSimBase : RegionApplicationBase
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // These are the names of the plugin-points extended by this
        // class during system startup.

        private const string PLUGIN_ASSET_CACHE = "/OpenSim/AssetCache";
        private const string PLUGIN_ASSET_SERVER_CLIENT = "/OpenSim/AssetClient";

        protected string proxyUrl;
        protected int proxyOffset = 0;

        protected string m_startupCommandsFile;
        protected string m_shutdownCommandsFile;
        protected string m_consoleType = "LocalConsole";
        protected uint m_consolePort = 0;

        private string m_timedScript = "disabled";
        private Timer m_scriptTimer;
        
        public string userStatsURI = String.Empty;

        protected bool m_autoCreateClientStack = true;

        /// <value>
        /// The file used to load and save prim backup xml if no filename has been specified
        /// </value>
        protected const string DEFAULT_PRIM_BACKUP_FILENAME = "prim-backup.xml";

        public ConfigSettings ConfigurationSettings
        {
            get { return m_configSettings; }
            set { m_configSettings = value; }
        }

        protected ConfigSettings m_configSettings;

        protected ConfigurationLoader m_configLoader;

        public ConsoleCommand CreateAccount = null;

        protected List<IApplicationPlugin> m_plugins = new List<IApplicationPlugin>();

        /// <value>
        /// The config information passed into the OpenSimulator region server.
        /// </value>
        public OpenSimConfigSource ConfigSource
        {
            get { return m_config; }
            set { m_config = value; }
        }

        protected OpenSimConfigSource m_config;

        public List<IClientNetworkServer> ClientServers
        {
            get { return m_clientServers; }
        }

        protected List<IClientNetworkServer> m_clientServers = new List<IClientNetworkServer>();
       
        public uint HttpServerPort
        {
            get { return m_httpServerPort; }
        }

        public ModuleLoader ModuleLoader
        {
            get { return m_moduleLoader; }
            set { m_moduleLoader = value; }
        }

        protected ModuleLoader m_moduleLoader;

        protected IRegistryCore m_applicationRegistry = new RegistryCore();

        public IRegistryCore ApplicationRegistry
        {
            get { return m_applicationRegistry; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configSource"></param>
        public OpenSimBase(IConfigSource configSource) : base()
        {
            m_configLoader = new ConfigurationLoader();
            m_config = m_configLoader.LoadConfigSettings(configSource, out m_configSettings, out m_networkServersInfo);
            IConfig networkConfig = m_config.Source.Configs["Network"];
            if (networkConfig != null)
            {
                proxyUrl = networkConfig.GetString("proxy_url", "");
                proxyOffset = Int32.Parse(networkConfig.GetString("proxy_offset", "0"));
            }
            IConfig startupConfig = m_config.Source.Configs["Startup"];
            
            int stpMaxThreads = 15;

            if (startupConfig != null)
            {
                m_startupCommandsFile = startupConfig.GetString("startup_console_commands_file", "startup_commands.txt");
                m_shutdownCommandsFile = startupConfig.GetString("shutdown_console_commands_file", "shutdown_commands.txt");

                if (startupConfig.GetString("console", String.Empty) != String.Empty)
                    m_consoleType = startupConfig.GetString("console", String.Empty);

                if (networkConfig != null)
                    m_consolePort = (uint)networkConfig.GetInt("console_port", 0);
                m_timedScript = startupConfig.GetString("timer_Script", "disabled");
                if (m_logFileAppender != null)
                {
                    if (m_logFileAppender is log4net.Appender.FileAppender)
                    {
                        log4net.Appender.FileAppender appender =
                                (log4net.Appender.FileAppender)m_logFileAppender;
                        string fileName = startupConfig.GetString("LogFile", String.Empty);
                        if (fileName != String.Empty)
                        {
                            appender.File = fileName;
                            appender.ActivateOptions();
                        }
                        m_log.InfoFormat("[LOGGING]: Logging started to file {0}", appender.File);
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

            m_log.Info("[OPENSIM MAIN]: Using async_call_method " + Util.FireAndForgetMethod);
        }

        protected virtual void LoadPlugins()
        {
            using (PluginLoader<IApplicationPlugin> loader = new PluginLoader<IApplicationPlugin>(new ApplicationPluginInitialiser(this)))
            {
                loader.Load("/OpenSim/Startup");
                m_plugins = loader.Plugins;
            }
        }

        protected override List<string> GetHelpTopics()
        {
            List<string> topics = base.GetHelpTopics();
            Scene s = SceneManager.CurrentOrFirstScene;
            if (s != null && s.GetCommanders() != null)
                topics.AddRange(s.GetCommanders().Keys);

            return topics;
        }

        /// <summary>
        /// Performs startup specific to the region server, including initialization of the scene 
        /// such as loading configuration from disk.
        /// </summary>
        protected override void StartupSpecific()
        {
            m_log.Info("====================================================================");
            m_log.Info("========================= STARTING OPENSIM =========================");
            m_log.Info("====================================================================");
            m_log.InfoFormat("[OPENSIM MAIN]: Running ");
            //m_log.InfoFormat("[OPENSIM MAIN]: GC Is Server GC: {0}", GCSettings.IsServerGC.ToString());
            // http://msdn.microsoft.com/en-us/library/bb384202.aspx
            //GCSettings.LatencyMode = GCLatencyMode.Batch;
            //m_log.InfoFormat("[OPENSIM MAIN]: GC Latency Mode: {0}", GCSettings.LatencyMode.ToString());

            List<ICommandConsole> ConsoleModules = Aurora.Framework.AuroraModuleLoader.PickupModules<ICommandConsole>(Environment.CurrentDirectory, "ICommandConsole");
            foreach(ICommandConsole consolemod in ConsoleModules)
            {
                if(consolemod.Name == m_consoleType)
                {
                    consolemod.Initialise("Region");
                    //REFACTOR ISSUE: this shouldn't occur... but the interface needs extended before this can be fixed.
                    m_console = (CommandConsole)consolemod;
                }
            }

            MainConsole.Instance = m_console;

            RegisterConsoleCommands();
            IConfig startupConfig = m_config.Source.Configs["Startup"];
            if (startupConfig != null)
            {
                string pidFile = startupConfig.GetString("PIDFile", String.Empty);
                if (pidFile != String.Empty)
                    CreatePIDFile(pidFile);
                
                userStatsURI = startupConfig.GetString("Stats_URI", String.Empty);
            }

            base.StartupSpecific();

            m_stats = StatsManager.StartCollectingSimExtraStats();

            // Create a ModuleLoader instance
            m_moduleLoader = new ModuleLoader(m_config.Source);

            LoadPlugins();
            foreach (IApplicationPlugin plugin in m_plugins)
            {
                plugin.PostInitialise();
            }

            AddPluginCommands();

            MainServer.Instance.AddStreamHandler(new SimStatusHandler());
            MainServer.Instance.AddStreamHandler(new XSimStatusHandler(this));
            if (userStatsURI != String.Empty)
                MainServer.Instance.AddStreamHandler(new UXSimStatusHandler(this));

            if (m_console is RemoteConsole)
            {
                if (m_consolePort == 0)
                {
                    ((RemoteConsole)m_console).SetServer(m_httpServer);
                }
                else
                {
                    ((RemoteConsole)m_console).SetServer(MainServer.GetHttpServer(m_consolePort));
                }
            }

            //Run Startup Commands
            if (String.IsNullOrEmpty(m_startupCommandsFile))
            {
                m_log.Info("[STARTUP]: No startup command script specified. Moving on...");
            }
            else
            {
                RunCommandScript(m_startupCommandsFile);
            }

            // Start timer script (run a script every xx seconds)
            if (m_timedScript != "disabled")
            {
                m_scriptTimer = new Timer();
                m_scriptTimer.Enabled = true;
                m_scriptTimer.Interval = 1200 * 1000;
                m_scriptTimer.Elapsed += RunAutoTimerScript;
            }

            // Hook up to the watchdog timer
            Watchdog.OnWatchdogTimeout += WatchdogTimeoutHandler;

            PrintFileToConsole("startuplogo.txt");

            m_log.InfoFormat("[NETWORK]: Using {0} as SYSTEMIP", Util.GetLocalHost().ToString());

            // For now, start at the 'root' level by default
            if (m_sceneManager.Scenes.Count == 1) // If there is only one region, select it
                ChangeSelectedRegion("region",
                                     new string[] { "change", "region", m_sceneManager.Scenes[0].RegionInfo.RegionName });
            else
                ChangeSelectedRegion("region", new string[] { "change", "region", "root" });
        }

        /// <summary>
        /// Register standard set of region console commands
        /// </summary>
        private void RegisterConsoleCommands()
        {
            m_console.Commands.AddCommand("region", false, "clear assets",
                                          "clear assets",
                                          "Clear the asset cache", HandleClearAssets);

            m_console.Commands.AddCommand("region", false, "force update",
                                          "force update",
                                          "Force the update of all objects on clients",
                                          HandleForceUpdate);

            m_console.Commands.AddCommand("region", false, "debug packet",
                                          "debug packet <level>",
                                          "Turn on packet debugging", Debug);

            m_console.Commands.AddCommand("region", false, "debug scene",
                                          "debug scene <cripting> <collisions> <physics>",
                                          "Turn on scene debugging", Debug);

            m_console.Commands.AddCommand("region", false, "change region",
                                          "change region <region name>",
                                          "Change current console region", ChangeSelectedRegion);

            m_console.Commands.AddCommand("region", false, "save xml",
                                          "save xml",
                                          "Save a region's data in XML format", SaveXml);

            m_console.Commands.AddCommand("region", false, "save xml2",
                                          "save xml2",
                                          "Save a region's data in XML2 format", SaveXml2);

            m_console.Commands.AddCommand("region", false, "load xml",
                                          "load xml [-newIDs [<x> <y> <z>]]",
                                          "Load a region's data from XML format", LoadXml);

            m_console.Commands.AddCommand("region", false, "load xml2",
                                          "load xml2",
                                          "Load a region's data from XML2 format", LoadXml2);

            m_console.Commands.AddCommand("region", false, "save prims xml2",
                                          "save prims xml2 [<prim name> <file name>]",
                                          "Save named prim to XML2", SavePrimsXml2);

            m_console.Commands.AddCommand("region", false, "load oar",
                                          "load oar [--merge] [--skip-assets] <oar name>",
                                          "Load a region's data from OAR archive.  --merge will merge the oar with the existing scene.  --skip-assets will load the oar but ignore the assets it contains",
                                          LoadOar);

            m_console.Commands.AddCommand("region", false, "save oar",
                                          "save oar <oar name>",
                                          "Save a region's data to an OAR archive",
                                          "More information on forthcoming options here soon", SaveOar);

            m_console.Commands.AddCommand("region", false, "edit scale",
                                          "edit scale <name> <x> <y> <z>",
                                          "Change the scale of a named prim", HandleEditScale);

            m_console.Commands.AddCommand("region", false, "kick user",
                                          "kick user <first> <last> [message]",
                                          "Kick a user off the simulator", KickUserCommand);

            m_console.Commands.AddCommand("region", false, "show assets",
                                          "show assets",
                                          "Show asset data", HandleShow);

            m_console.Commands.AddCommand("region", false, "show users",
                                          "show users [full]",
                                          "Show user data", HandleShow);

            m_console.Commands.AddCommand("region", false, "show connections",
                                          "show connections",
                                          "Show connection data", HandleShow);

            m_console.Commands.AddCommand("region", false, "show users full",
                                          "show users full",
                                          String.Empty, HandleShow);

            m_console.Commands.AddCommand("region", false, "show modules",
                                          "show modules",
                                          "Show module data", HandleShow);

            m_console.Commands.AddCommand("region", false, "show regions",
                                          "show regions",
                                          "Show region data", HandleShow);

            m_console.Commands.AddCommand("region", false, "show queues",
                                          "show queues",
                                          "Show queue data", HandleShow);
            m_console.Commands.AddCommand("region", false, "show ratings",
                                          "show ratings",
                                          "Show rating data", HandleShow);

            m_console.Commands.AddCommand("region", false, "backup",
                                          "backup",
                                          "Persist objects to the database now", RunCommand);

            m_console.Commands.AddCommand("region", false, "create region",
                                          "create region",
                                          "Create a new region Ex. create region <filename.ini>", HandleCreateRegion);

            m_console.Commands.AddCommand("region", false, "restart",
                                          "restart",
                                          "Restart all sims in this instance", RunCommand);

            m_console.Commands.AddCommand("region", false, "config set",
                                          "config set <section> <field> <value>",
                                          "Set a config option", HandleConfig);

            m_console.Commands.AddCommand("region", false, "config get",
                                          "config get <section> <field>",
                                          "Read a config option", HandleConfig);

            m_console.Commands.AddCommand("region", false, "config save",
                                          "config save",
                                          "Save current configuration", HandleConfig);

            m_console.Commands.AddCommand("region", false, "command-script",
                                          "command-script <script>",
                                          "Run a command script from file", RunCommand);

            m_console.Commands.AddCommand("region", false, "remove-region",
                                          "remove-region <name>",
                                          "Remove a region from this simulator", RunCommand);

            m_console.Commands.AddCommand("region", false, "delete-region",
                                          "delete-region <name>",
                                          "Delete a region from disk", RunCommand);

            m_console.Commands.AddCommand("region", false, "modules list",
                                          "modules list",
                                          "List modules", HandleModules);

            m_console.Commands.AddCommand("region", false, "modules load",
                                          "modules load <name>",
                                          "Load a module", HandleModules);

            m_console.Commands.AddCommand("region", false, "modules unload",
                                          "modules unload <name>",
                                          "Unload a module", HandleModules);

            m_console.Commands.AddCommand("region", false, "Add-InventoryHost",
                                          "Add-InventoryHost <host>",
                                          String.Empty, RunCommand);

            m_console.Commands.AddCommand("region", false, "kill uuid",
                                          "kill uuid <UUID>",
                                          "Kill an object by UUID", KillUUID);

        }

        /// <summary>
        /// Timer to run a specific text file as console commands.  Configured in in the main ini file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunAutoTimerScript(object sender, EventArgs e)
        {
            if (m_timedScript != "disabled")
            {
                RunCommandScript(m_timedScript);
            }
        }

        private void WatchdogTimeoutHandler(System.Threading.Thread thread, int lastTick)
        {
            int now = Environment.TickCount & Int32.MaxValue;

            m_log.ErrorFormat("[WATCHDOG]: Timeout detected for thread \"{0}\". ThreadState={1}. Last tick was {2}ms ago",
                thread.Name, thread.ThreadState, now - lastTick);
        }

        #region Console Commands

        /// <summary>
        /// Kicks users off the region
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmdparams">name of avatar to kick</param>
        private void KickUserCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length < 4)
                return;

            string alert = null;
            if (cmdparams.Length > 4)
                alert = String.Format("\n{0}\n", String.Join(" ", cmdparams, 4, cmdparams.Length - 4));

            IList agents = m_sceneManager.GetCurrentSceneAvatars();

            foreach (ScenePresence presence in agents)
            {
                RegionInfo regionInfo = presence.Scene.RegionInfo;

                if (presence.Firstname.ToLower().Contains(cmdparams[2].ToLower()) &&
                    presence.Lastname.ToLower().Contains(cmdparams[3].ToLower()))
                {
                    MainConsole.Instance.Output(
                        String.Format(
                            "Kicking user: {0,-16}{1,-16}{2,-37} in region: {3,-16}",
                            presence.Firstname, presence.Lastname, presence.UUID, regionInfo.RegionName));

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
            if (File.Exists(fileName))
            {
                m_log.Info("[COMMANDFILE]: Running " + fileName);

                using (StreamReader readFile = File.OpenText(fileName))
                {
                    string currentCommand;
                    while ((currentCommand = readFile.ReadLine()) != null)
                    {
                        if (currentCommand != String.Empty)
                        {
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
        /// Edits the scale of a primative with the name specified
        /// </summary>
        /// <param name="module"></param>
        /// <param name="args">0,1, name, x, y, z</param>
        private void HandleEditScale(string module, string[] args)
        {
            if (args.Length == 6)
            {
                m_sceneManager.HandleEditCommandOnCurrentScene(args);
            }
            else
            {
                MainConsole.Instance.Output("Argument error: edit scale <prim name> <x> <y> <z>");
            }
        }

        /// <summary>
        /// Creates a new region based on the parameters specified.   This will ask the user questions on the console
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmd">0,1,region name, region XML file</param>
        private void HandleCreateRegion(string module, string[] cmd)
        {
            if (cmd.Length < 3)
            {
                MainConsole.Instance.Output("Usage: create region <region_file.ini>");
                return;
            }
            //Decapriates .xml files --Revolution
            /*if (cmd[3].EndsWith(".xml"))
            {
                string regionsDir = ConfigSource.Source.Configs["Startup"].GetString("regionload_regionsdir", "Regions").Trim();
                string regionFile = String.Format("{0}/{1}", regionsDir, cmd[3]);
                // Allow absolute and relative specifiers
                if (cmd[3].StartsWith("/") || cmd[3].StartsWith("\\") || cmd[3].StartsWith(".."))
                    regionFile = cmd[3];

                IScene scene;
                CreateRegion(new RegionInfo("", regionFile, false, ConfigSource.Source), true, out scene);
            }
            else */if (cmd[2].EndsWith(".ini"))
            {
                string regionsDir = ConfigSource.Source.Configs["Startup"].GetString("regionload_regionsdir", "Regions").Trim();
                string regionFile = String.Format("{0}/{1}", regionsDir, cmd[2]);
                // Allow absolute and relative specifiers
                if (cmd[2].StartsWith("/") || cmd[2].StartsWith("\\") || cmd[2].StartsWith(".."))
                    regionFile = cmd[2];

                IScene scene;
                CreateRegion(new RegionInfo("", regionFile, false, ConfigSource.Source, ""), true, out scene);
            }
            else
            {
                MainConsole.Instance.Output("Usage: create region <region_file.ini>");
                return;
            }
        }

        /// <summary>
        /// Change and load configuration file data.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmd"></param>
        private void HandleConfig(string module, string[] cmd)
        {
            List<string> args = new List<string>(cmd);
            args.RemoveAt(0);
            string[] cmdparams = args.ToArray();
            string n = "CONFIG";

            if (cmdparams.Length > 0)
            {
                switch (cmdparams[0].ToLower())
                {
                    case "set":
                        if (cmdparams.Length < 4)
                        {
                            MainConsole.Instance.Output(String.Format("SYNTAX: {0} SET SECTION KEY VALUE", n));
                            MainConsole.Instance.Output(String.Format("EXAMPLE: {0} SET ScriptEngine.DotNetEngine NumberOfScriptThreads 5", n));
                        }
                        else
                        {
                            IConfig c;
                            IConfigSource source = new IniConfigSource();
                            c = source.AddConfig(cmdparams[1]);
                            if (c != null)
                            {
                                string _value = String.Join(" ", cmdparams, 3, cmdparams.Length - 3);
                                c.Set(cmdparams[2], _value);
                                m_config.Source.Merge(source);

                                MainConsole.Instance.Output(String.Format("{0} {0} {1} {2} {3}", n, cmdparams[1], cmdparams[2], _value));
                            }
                        }
                        break;

                    case "get":
                        if (cmdparams.Length < 3)
                        {
                            MainConsole.Instance.Output(String.Format("SYNTAX: {0} GET SECTION KEY", n));
                            MainConsole.Instance.Output(String.Format("EXAMPLE: {0} GET ScriptEngine.DotNetEngine NumberOfScriptThreads", n));
                        }
                        else
                        {
                            IConfig c = m_config.Source.Configs[cmdparams[1]];
                            if (c == null)
                            {
                                MainConsole.Instance.Output(String.Format("Section \"{0}\" does not exist.", cmdparams[1]));
                                break;
                            }
                            else
                            {
                                MainConsole.Instance.Output(String.Format("{0} GET {1} {2} : {3}", n, cmdparams[1], cmdparams[2],
                                                     c.GetString(cmdparams[2])));
                            }
                        }

                        break;

                    case "save":
                        if (cmdparams.Length < 2)
                        {
                            MainConsole.Instance.Output("SYNTAX: " + n + " SAVE FILE");
                            return;
                        }

                        if (Application.iniFilePath == cmdparams[1])
                        {
                            MainConsole.Instance.Output("FILE can not be " + Application.iniFilePath);
                            return;
                        }

                        MainConsole.Instance.Output("Saving configuration file: " + cmdparams[1]);
                        m_config.Save(cmdparams[1]);
                        break;
                }
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

            if (cmdparams.Length > 0)
            {
                switch (cmdparams[0].ToLower())
                {
                    case "list":
                        foreach (IRegionModule irm in m_moduleLoader.GetLoadedSharedModules)
                        {
                            MainConsole.Instance.Output(String.Format("Shared region module: {0}", irm.Name));
                        }
                        break;
                    case "unload":
                        if (cmdparams.Length > 1)
                        {
                            foreach (IRegionModule rm in new ArrayList(m_moduleLoader.GetLoadedSharedModules))
                            {
                                if (rm.Name.ToLower() == cmdparams[1].ToLower())
                                {
                                    MainConsole.Instance.Output(String.Format("Unloading module: {0}", rm.Name));
                                    m_moduleLoader.UnloadModule(rm);
                                }
                            }
                        }
                        break;
                    case "load":
                        if (cmdparams.Length > 1)
                        {
                            foreach (Scene s in new ArrayList(m_sceneManager.Scenes))
                            {
                                MainConsole.Instance.Output(String.Format("Loading module: {0}", cmdparams[1]));
                                m_moduleLoader.LoadRegionModules(cmdparams[1], s);
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

            switch (command)
            {
                case "command-script":
                    if (cmdparams.Length > 0)
                    {
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
                        RemoveRegion(removeScene, false);
                    else
                        MainConsole.Instance.Output("no region with that name");
                    break;

                case "delete-region":
                    string regDeleteName = CombineParams(cmdparams, 0);

                    Scene killScene;
                    if (m_sceneManager.TryGetScene(regDeleteName, out killScene))
                        RemoveRegion(killScene, true);
                    else
                        MainConsole.Instance.Output("no region with that name");
                    break;

                case "restart":
                    m_sceneManager.RestartCurrentScene();
                    break;

                case "Add-InventoryHost":
                    if (cmdparams.Length > 0)
                    {
                        MainConsole.Instance.Output("Not implemented.");
                    }
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

                if (!m_sceneManager.TrySetCurrentScene(newRegionName))
                    MainConsole.Instance.Output(String.Format("Couldn't select region {0}", newRegionName));
            }
            else
            {
                MainConsole.Instance.Output("Usage: change region <region name>");
            }

            string regionName = (m_sceneManager.CurrentScene == null ? "root" : m_sceneManager.CurrentScene.RegionInfo.RegionName);
            MainConsole.Instance.Output(String.Format("Currently selected region is {0}", regionName));
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

            switch (args[1])
            {
                case "packet":
                    if (args.Length > 2)
                    {
                        int newDebug;
                        if (int.TryParse(args[2], out newDebug))
                        {
                            m_sceneManager.SetDebugPacketLevelOnCurrentScene(newDebug);
                        }
                        else
                        {
                            MainConsole.Instance.Output("packet debug should be 0..255");
                        }
                        MainConsole.Instance.Output(String.Format("New packet debug: {0}", newDebug));
                    }

                    break;

                case "scene":
                    if (args.Length == 5)
                    {
                        if (m_sceneManager.CurrentScene == null)
                        {
                            MainConsole.Instance.Output("Please use 'change region <regioname>' first");
                        }
                        else
                        {
                            bool scriptingOn = !Convert.ToBoolean(args[2]);
                            bool collisionsOn = !Convert.ToBoolean(args[3]);
                            bool physicsOn = !Convert.ToBoolean(args[4]);
                            m_sceneManager.CurrentScene.SetSceneCoreDebug(scriptingOn, collisionsOn, physicsOn);

                            MainConsole.Instance.Output(
                                String.Format(
                                    "Set debug scene scripting = {0}, collisions = {1}, physics = {2}",
                                    !scriptingOn, !collisionsOn, !physicsOn));
                        }
                    }
                    else
                    {
                        MainConsole.Instance.Output("debug scene <scripting> <collisions> <physics> (where inside <> is true/false)");
                    }

                    break;

                default:
                    MainConsole.Instance.Output("Unknown debug");
                    break;
            }
        }

        // see BaseOpenSimServer
        /// <summary>
        /// Many commands list objects for debugging.  Some of the types are listed  here
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="cmd"></param>
        public override void HandleShow(string mod, string[] cmd)
        {
            base.HandleShow(mod, cmd);

            List<string> args = new List<string>(cmd);
            args.RemoveAt(0);
            string[] showParams = args.ToArray();

            switch (showParams[0])
            {
                case "assets":
                    MainConsole.Instance.Output("Not implemented.");
                    break;

                case "users":
                    IList agents;
                    if (showParams.Length > 1 && showParams[1] == "full")
                    {
                        agents = m_sceneManager.GetCurrentScenePresences();
                    }
                    else
                    {
                        agents = m_sceneManager.GetCurrentSceneAvatars();
                    }

                    MainConsole.Instance.Output(String.Format("\nAgents connected: {0}\n", agents.Count));

                    MainConsole.Instance.Output(
                        String.Format("{0,-16}{1,-16}{2,-37}{3,-11}{4,-16}{5,-30}", "Firstname", "Lastname",
                                      "Agent ID", "Root/Child", "Region", "Position"));

                    foreach (ScenePresence presence in agents)
                    {
                        RegionInfo regionInfo = presence.Scene.RegionInfo;
                        string regionName;

                        if (regionInfo == null)
                        {
                            regionName = "Unresolvable";
                        }
                        else
                        {
                            regionName = regionInfo.RegionName;
                        }

                        MainConsole.Instance.Output(
                            String.Format(
                                "{0,-16}{1,-16}{2,-37}{3,-11}{4,-16}{5,-30}",
                                presence.Firstname,
                                presence.Lastname,
                                presence.UUID,
                                presence.IsChildAgent ? "Child" : "Root",
                                regionName,
                                presence.AbsolutePosition.ToString()));
                    }

                    MainConsole.Instance.Output(String.Empty);
                    break;

                case "connections":
                    System.Text.StringBuilder connections = new System.Text.StringBuilder("Connections:\n");
                    m_sceneManager.ForEachScene(
                        delegate(Scene scene)
                        {
                            scene.ForEachClient(
                                delegate(IClientAPI client)
                                {
                                    connections.AppendFormat("{0}: {1} ({2}) from {3} on circuit {4}\n",
                                        scene.RegionInfo.RegionName, client.Name, client.AgentId, client.RemoteEndPoint, client.CircuitCode);
                                }
                            );
                        }
                    );

                    MainConsole.Instance.Output(connections.ToString());
                    break;

                case "modules":
                    MainConsole.Instance.Output("The currently loaded shared modules are:");
                    foreach (IRegionModule module in m_moduleLoader.GetLoadedSharedModules)
                    {
                        MainConsole.Instance.Output("Shared Module: " + module.Name);
                    }

                    MainConsole.Instance.Output("");
                    break;

                case "regions":
                    m_sceneManager.ForEachScene(
                        delegate(Scene scene)
                        {
                            MainConsole.Instance.Output(String.Format(
                                       "Region Name: {0}, Region XLoc: {1}, Region YLoc: {2}, Region Port: {3}",
                                       scene.RegionInfo.RegionName,
                                       scene.RegionInfo.RegionLocX,
                                       scene.RegionInfo.RegionLocY,
                                       scene.RegionInfo.InternalEndPoint.Port));
                        });
                    break;

                case "queues":
                    Notice(GetQueuesReport());
                    break;

                case "ratings":
                    m_sceneManager.ForEachScene(
                    delegate(Scene scene)
                    {
                        string rating = "";
                        if (scene.RegionInfo.RegionSettings.Maturity == 1)
                        {
                            rating = "MATURE";
                        }
                        else if (scene.RegionInfo.RegionSettings.Maturity == 2)
                        {
                            rating = "ADULT";
                        }
                        else
                        {
                            rating = "PG";
                        }
                        MainConsole.Instance.Output(String.Format(
                                   "Region Name: {0}, Region Rating {1}",
                                   scene.RegionInfo.RegionName,
                                   rating));
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

            m_sceneManager.ForEachScene(delegate(Scene scene)
            {
                scene.ForEachClient(delegate(IClientAPI client)
                {
                    if (client is IStatsCollector)
                    {
                        report = report + client.FirstName +
                                 " " + client.LastName;

                        IStatsCollector stats =
                            (IStatsCollector)client;

                        report = report + string.Format("{0,7} {1,7} {2,7} {3,7} {4,7} {5,7} {6,7} {7,7} {8,7} {9,7}\n",
                                     "Send",
                                     "In",
                                     "Out",
                                     "Resend",
                                     "Land",
                                     "Wind",
                                     "Cloud",
                                     "Task",
                                     "Texture",
                                     "Asset");
                        report = report + stats.Report() +
                                 "\n";
                    }
                });
            });

            return report;
        }

        /// <summary>
        /// Use XML2 format to serialize data to a file
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmdparams"></param>
        protected void SavePrimsXml2(string module, string[] cmdparams)
        {
            if (cmdparams.Length > 5)
            {
                m_sceneManager.SaveNamedPrimsToXml2(cmdparams[3], cmdparams[4]);
            }
            else
            {
                m_sceneManager.SaveNamedPrimsToXml2("Primitive", DEFAULT_PRIM_BACKUP_FILENAME);
            }
        }

        /// <summary>
        /// Use XML format to serialize data to a file
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmdparams"></param>
        protected void SaveXml(string module, string[] cmdparams)
        {
            MainConsole.Instance.Output("PLEASE NOTE, save-xml is DEPRECATED and may be REMOVED soon.  If you are using this and there is some reason you can't use save-xml2, please file a mantis detailing the reason.");

            if (cmdparams.Length > 0)
            {
                m_sceneManager.SaveCurrentSceneToXml(cmdparams[2]);
            }
            else
            {
                m_sceneManager.SaveCurrentSceneToXml(DEFAULT_PRIM_BACKUP_FILENAME);
            }
        }

        /// <summary>
        /// Loads data and region objects from XML format.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmdparams"></param>
        protected void LoadXml(string module, string[] cmdparams)
        {
            MainConsole.Instance.Output("PLEASE NOTE, load-xml is DEPRECATED and may be REMOVED soon.  If you are using this and there is some reason you can't use load-xml2, please file a mantis detailing the reason.");

            Vector3 loadOffset = new Vector3(0, 0, 0);
            if (cmdparams.Length > 2)
            {
                bool generateNewIDS = false;
                if (cmdparams.Length > 3)
                {
                    if (cmdparams[3] == "-newUID")
                    {
                        generateNewIDS = true;
                    }
                    if (cmdparams.Length > 4)
                    {
                        loadOffset.X = (float)Convert.ToDecimal(cmdparams[4], Culture.NumberFormatInfo);
                        if (cmdparams.Length > 5)
                        {
                            loadOffset.Y = (float)Convert.ToDecimal(cmdparams[5], Culture.NumberFormatInfo);
                        }
                        if (cmdparams.Length > 6)
                        {
                            loadOffset.Z = (float)Convert.ToDecimal(cmdparams[6], Culture.NumberFormatInfo);
                        }
                        MainConsole.Instance.Output(String.Format("loadOffsets <X,Y,Z> = <{0},{1},{2}>", loadOffset.X, loadOffset.Y, loadOffset.Z));
                    }
                }
                m_sceneManager.LoadCurrentSceneFromXml(cmdparams[0], generateNewIDS, loadOffset);
            }
            else
            {
                try
                {
                    m_sceneManager.LoadCurrentSceneFromXml(DEFAULT_PRIM_BACKUP_FILENAME, false, loadOffset);
                }
                catch (FileNotFoundException)
                {
                    MainConsole.Instance.Output("Default xml not found. Usage: load-xml <filename>");
                }
            }
        }
        /// <summary>
        /// Serialize region data to XML2Format
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmdparams"></param>
        protected void SaveXml2(string module, string[] cmdparams)
        {
            if (cmdparams.Length > 2)
            {
                m_sceneManager.SaveCurrentSceneToXml2(cmdparams[2]);
            }
            else
            {
                m_sceneManager.SaveCurrentSceneToXml2(DEFAULT_PRIM_BACKUP_FILENAME);
            }
        }

        /// <summary>
        /// Load region data from Xml2Format
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmdparams"></param>
        protected void LoadXml2(string module, string[] cmdparams)
        {
            if (cmdparams.Length > 2)
            {
                try
                {
                    m_sceneManager.LoadCurrentSceneFromXml2(cmdparams[2]);
                }
                catch (FileNotFoundException)
                {
                    MainConsole.Instance.Output("Specified xml not found. Usage: load xml2 <filename>");
                }
            }
            else
            {
                try
                {
                    m_sceneManager.LoadCurrentSceneFromXml2(DEFAULT_PRIM_BACKUP_FILENAME);
                }
                catch (FileNotFoundException)
                {
                    MainConsole.Instance.Output("Default xml not found. Usage: load xml2 <filename>");
                }
            }
        }

        /// <summary>
        /// Load a whole region from an opensimulator archive.
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void LoadOar(string module, string[] cmdparams)
        {
            try
            {
                m_sceneManager.LoadArchiveToCurrentScene(cmdparams);
            }
            catch (Exception e)
            {
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
            for (int i = pos; i < commandParams.Length; i++)
            {
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
            if (cmdparams.Length > 2)
            {
                UUID id = UUID.Zero;
                SceneObjectGroup grp = null;
                Scene sc = null;

                if (!UUID.TryParse(cmdparams[2], out id))
                {
                    MainConsole.Instance.Output("[KillUUID]: Error bad UUID format!");
                    return;
                }

                m_sceneManager.ForEachScene(
                    delegate(Scene scene)
                    {
                        SceneObjectPart part = scene.GetSceneObjectPart(id);
                        if (part == null)
                            return;

                        grp = part.ParentGroup;
                        sc = scene;
                    });

                if (grp == null)
                {
                    MainConsole.Instance.Output(String.Format("[KillUUID]: Given UUID {0} not found!", id));
                }
                else
                {
                    MainConsole.Instance.Output(String.Format("[KillUUID]: Found UUID {0} in scene {1}", id, sc.RegionInfo.RegionName));
                    try
                    {
                        sc.DeleteSceneObject(grp, false);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat("[KillUUID]: Error while removing objects from scene: " + e);
                    }
                }
            }
            else
            {
                MainConsole.Instance.Output("[KillUUID]: Usage: kill uuid <UUID>");
            }
        }

        #endregion

        protected virtual void AddPluginCommands()
        {
            // If console exists add plugin commands.
            if (m_console != null)
            {
                List<string> topics = GetHelpTopics();

                foreach (string topic in topics)
                {
                    m_console.Commands.AddCommand("plugin", false, "help " + topic,
                                                  "help " + topic,
                                                  "Get help on plugin command '" + topic + "'",
                                                  HandleCommanderHelp);

                    m_console.Commands.AddCommand("plugin", false, topic,
                                                  topic,
                                                  "Execute subcommand for plugin '" + topic + "'",
                                                  null);

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
                        m_console.Commands.AddCommand(topic, false,
                                                      topic + " " + command,
                                                      topic + " " + commander.Commands[command].ShortHelp(),
                                                      String.Empty, HandleCommanderCommand);
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

        protected override void Initialize()
        {
            // Called from base.StartUp()

            m_httpServerPort = m_networkServersInfo.HttpListenerPort;
            m_sceneManager.OnRestartSim += handleRestartRegion;
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="portadd_flag"></param>
        /// <returns></returns>
        public IClientNetworkServer CreateRegion(RegionInfo regionInfo, bool portadd_flag, out IScene scene)
        {
            return CreateRegion(regionInfo, portadd_flag, false, out scene);
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <returns></returns>
        public IClientNetworkServer CreateRegion(RegionInfo regionInfo, out IScene scene)
        {
            return CreateRegion(regionInfo, false, true, out scene);
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="portadd_flag"></param>
        /// <param name="do_post_init"></param>
        /// <returns></returns>
        public IClientNetworkServer CreateRegion(RegionInfo regionInfo, bool portadd_flag, bool do_post_init, out IScene mscene)
        {
            int port = regionInfo.InternalEndPoint.Port;

            // set initial RegionID to originRegionID in RegionInfo. (it needs for loding prims)
            // Commented this out because otherwise regions can't register with
            // the grid as there is already another region with the same UUID
            // at those coordinates. This is required for the load balancer to work.
            // --Mike, 2009.02.25
            //regionInfo.originRegionID = regionInfo.RegionID;

            // set initial ServerURI
            regionInfo.ServerURI = "http://" + regionInfo.ExternalHostName + ":" + regionInfo.InternalEndPoint.Port;
            regionInfo.HttpPort = m_httpServerPort;
            
            regionInfo.osSecret = m_osSecret;
            
            if ((proxyUrl.Length > 0) && (portadd_flag))
            {
                // set proxy url to RegionInfo
                regionInfo.proxyUrl = proxyUrl;
                regionInfo.ProxyOffset = proxyOffset;
                Util.XmlRpcCommand(proxyUrl, "AddPort", port, port + proxyOffset, regionInfo.ExternalHostName);
            }

            IClientNetworkServer clientServer;
            Scene scene = SetupScene(regionInfo, proxyOffset, m_config.Source, out clientServer);

            m_log.Info("[MODULES]: Loading Region's modules (old style)");

            List<IRegionModule> modules = m_moduleLoader.PickupModules(scene, ".");

            // This needs to be ahead of the script engine load, so the
            // script module can pick up events exposed by a module
            m_moduleLoader.InitialiseSharedModules(scene);

            // Use this in the future, the line above will be deprecated soon
            m_log.Info("[MODULES]: Loading Region's modules (new style)");
            IRegionModulesController controller;
            if (ApplicationRegistry.TryGet(out controller))
            {
                controller.AddRegionToModules(scene);
            }
            else m_log.Error("[MODULES]: The new RegionModulesController is missing...");

            scene.SetModuleInterfaces();

            // Prims have to be loaded after module configuration since some modules may be invoked during the load
            scene.LoadPrimsFromStorage(regionInfo.originRegionID);
            
            // moved these here as the terrain texture has to be created after the modules are initialized
            // and has to happen before the region is registered with the grid.
            scene.CreateTerrainTexture();
            
            // TODO : Try setting resource for region xstats here on scene
            MainServer.Instance.AddStreamHandler(new Region.Framework.Scenes.RegionStatsHandler(regionInfo)); 
            
            try
            {
                scene.RegisterRegionWithGrid();
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[STARTUP]: Registration of region with grid failed, aborting startup - {0} - {1}", e.Message, e.StackTrace);

                // Carrying on now causes a lot of confusion down the
                // line - we need to get the user's attention
                throw e;
            }

            // We need to do this after we've initialized the
            // scripting engines.
            scene.CreateScriptInstances();

            scene.loadAllLandObjectsFromStorage(regionInfo.originRegionID);
            scene.EventManager.TriggerParcelPrimCountUpdate();

            m_sceneManager.Add(scene);

            if (m_autoCreateClientStack)
            {
                m_clientServers.Add(clientServer);
                clientServer.Start();
            }

            if (do_post_init)
            {
                foreach (IRegionModule module in modules)
                {
                    module.PostInitialise();
                }
            }
            scene.EventManager.OnShutdown += delegate() { ShutdownRegion(scene); };

            mscene = scene;

            scene.StartTimer();

            return clientServer;
        }

        private void ShutdownRegion(Scene scene)
        {
            m_log.DebugFormat("[SHUTDOWN]: Shutting down region {0}", scene.RegionInfo.RegionName);
            IRegionModulesController controller;
            if (ApplicationRegistry.TryGet<IRegionModulesController>(out controller))
            {
                controller.RemoveRegionFromModules(scene);
            }
        }

        public void RemoveRegion(Scene scene, bool cleanup)
        {
            // only need to check this if we are not at the
            // root level
            if ((m_sceneManager.CurrentScene != null) &&
                (m_sceneManager.CurrentScene.RegionInfo.RegionID == scene.RegionInfo.RegionID))
            {
                m_sceneManager.TrySetCurrentScene("..");
            }

            scene.DeleteAllSceneObjects();
            m_sceneManager.CloseScene(scene);
            ShutdownClientServer(scene.RegionInfo);
            
            if (!cleanup)
                return;

            if (!String.IsNullOrEmpty(scene.RegionInfo.RegionFile))
            {
                if (scene.RegionInfo.RegionFile.ToLower().EndsWith(".xml"))
                {
                    File.Delete(scene.RegionInfo.RegionFile);
                    m_log.InfoFormat("[OPENSIM]: deleting region file \"{0}\"", scene.RegionInfo.RegionFile);
                }
                if (scene.RegionInfo.RegionFile.ToLower().EndsWith(".ini"))
                {
                    try
                    {
                        IniConfigSource source = new IniConfigSource(scene.RegionInfo.RegionFile);
                        if (source.Configs[scene.RegionInfo.RegionName] != null)
                        {
                            source.Configs.Remove(scene.RegionInfo.RegionName);

                            if (source.Configs.Count == 0)
                            {
                                File.Delete(scene.RegionInfo.RegionFile);
                            }
                            else
                            {
                                source.Save(scene.RegionInfo.RegionFile);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public void RemoveRegion(string name, bool cleanUp)
        {
            Scene target;
            if (m_sceneManager.TryGetScene(name, out target))
                RemoveRegion(target, cleanUp);
        }

        /// <summary>
        /// Remove a region from the simulator without deleting it permanently.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public void CloseRegion(Scene scene)
        {
            // only need to check this if we are not at the
            // root level
            if ((m_sceneManager.CurrentScene != null) &&
                (m_sceneManager.CurrentScene.RegionInfo.RegionID == scene.RegionInfo.RegionID))
            {
                m_sceneManager.TrySetCurrentScene("..");
            }

            m_sceneManager.CloseScene(scene);
            ShutdownClientServer(scene.RegionInfo);
        }
        
        /// <summary>
        /// Remove a region from the simulator without deleting it permanently.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void CloseRegion(string name)
        {
            Scene target;
            if (m_sceneManager.TryGetScene(name, out target))
                CloseRegion(target);
        }
        
        /// <summary>
        /// Create a scene and its initial base structures.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="clientServer"> </param>
        /// <returns></returns>
        protected Scene SetupScene(RegionInfo regionInfo, out IClientNetworkServer clientServer)
        {
            return SetupScene(regionInfo, 0, null, out clientServer);
        }

        /// <summary>
        /// Create a scene and its initial base structures.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="proxyOffset"></param>
        /// <param name="configSource"></param>
        /// <param name="clientServer"> </param>
        /// <returns></returns>
        protected Scene SetupScene(
            RegionInfo regionInfo, int proxyOffset, IConfigSource configSource, out IClientNetworkServer clientServer)
        {
            AgentCircuitManager circuitManager = new AgentCircuitManager();
            IPAddress listenIP = regionInfo.InternalEndPoint.Address;
            //if (!IPAddress.TryParse(regionInfo.InternalEndPoint, out listenIP))
            //    listenIP = IPAddress.Parse("0.0.0.0");

            uint port = (uint) regionInfo.InternalEndPoint.Port;

            if (m_autoCreateClientStack)
            {
                clientServer
                    = m_clientStackManager.CreateServer(
                        listenIP, ref port, proxyOffset, regionInfo.m_allow_alternate_ports, configSource,
                        circuitManager);
            }
            else
            {
                clientServer = null;
            }

            regionInfo.InternalEndPoint.Port = (int) port;

            Scene scene = CreateScene(regionInfo, m_storageManager, circuitManager);

            if (m_autoCreateClientStack)
            {
                clientServer.AddScene(scene);
            }

            scene.LoadWorldMap();

            scene.PhysicsScene = GetPhysicsScene(scene.RegionInfo.RegionName);
            scene.PhysicsScene.SetTerrain(scene.Heightmap.GetFloatsSerialised());
            scene.PhysicsScene.SetWaterLevel((float) regionInfo.RegionSettings.WaterHeight);

            return scene;
        }

        protected override StorageManager CreateStorageManager()
        {
            return
                CreateStorageManager(m_configSettings.StorageConnectionString, m_configSettings.EstateConnectionString);
        }

        protected StorageManager CreateStorageManager(string connectionstring, string estateconnectionstring)
        {
            return new StorageManager(m_configSettings.StorageDll, connectionstring, estateconnectionstring);
        }

        protected override ClientStackManager CreateClientStackManager()
        {
            return new ClientStackManager(m_configSettings.ClientstackDll);
        }

        protected override Scene CreateScene(RegionInfo regionInfo, StorageManager storageManager,
                                             AgentCircuitManager circuitManager)
        {
            SceneCommunicationService sceneGridService = new SceneCommunicationService();

            return new Scene(
                regionInfo, circuitManager, sceneGridService,
                storageManager, m_moduleLoader, false, m_configSettings.PhysicalPrim,
                m_configSettings.See_into_region_from_neighbor, m_config.Source, m_version);
        }
        
        protected void ShutdownClientServer(RegionInfo whichRegion)
        {
            // Close and remove the clientserver for a region
            bool foundClientServer = false;
            int clientServerElement = 0;
            Location location = new Location(whichRegion.RegionHandle);

            for (int i = 0; i < m_clientServers.Count; i++)
            {
                if (m_clientServers[i].HandlesRegion(location))
                {
                    clientServerElement = i;
                    foundClientServer = true;
                    break;
                }
            }

            if (foundClientServer)
            {
                m_clientServers[clientServerElement].NetworkStop();
                m_clientServers.RemoveAt(clientServerElement);
            }
        }
        
        public void handleRestartRegion(RegionInfo whichRegion)
        {
            m_log.Info("[OPENSIM]: Got restart signal from SceneManager");

            ShutdownClientServer(whichRegion);
            IScene scene;
            CreateRegion(whichRegion, true, out scene);
        }

        # region Setup methods

        protected override PhysicsScene GetPhysicsScene(string osSceneIdentifier)
        {
            return GetPhysicsScene(
                m_configSettings.PhysicsEngine, m_configSettings.MeshEngineName, m_config.Source, osSceneIdentifier);
        }

        /// <summary>
        /// Handler to supply the current status of this sim
        /// </summary>
        /// Currently this is always OK if the simulator is still listening for connections on its HTTP service
        public class SimStatusHandler : IStreamedRequestHandler
        {
            public byte[] Handle(string path, Stream request,
                                 OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return Util.UTF8.GetBytes("OK");
            }

            public string ContentType
            {
                get { return "text/plain"; }
            }

            public string HttpMethod
            {
                get { return "GET"; }
            }

            public string Path
            {
                get { return "/simstatus/"; }
            }
        }

        /// <summary>
        /// Handler to supply the current extended status of this sim
        /// Sends the statistical data in a json serialization 
        /// </summary>
        public class XSimStatusHandler : IStreamedRequestHandler
        {
            OpenSimBase m_opensim;
            string osXStatsURI = String.Empty;
        
            public XSimStatusHandler(OpenSimBase sim)
            {
                m_opensim = sim;
                osXStatsURI = Util.SHA1Hash(sim.osSecret);
            }
            
            public byte[] Handle(string path, Stream request,
                                 OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return Util.UTF8.GetBytes(m_opensim.StatReport(httpRequest));
            }

            public string ContentType
            {
                get { return "text/plain"; }
            }

            public string HttpMethod
            {
                get { return "GET"; }
            }

            public string Path
            {
                // This is for the OpenSimulator instance and is the osSecret hashed
                get { return "/" + osXStatsURI + "/"; }
            }
        }

        /// <summary>
        /// Handler to supply the current extended status of this sim to a user configured URI
        /// Sends the statistical data in a json serialization 
        /// If the request contains a key, "callback" the response will be wrappend in the 
        /// associated value for jsonp used with ajax/javascript
        /// </summary>
        public class UXSimStatusHandler : IStreamedRequestHandler
        {
            OpenSimBase m_opensim;
            string osUXStatsURI = String.Empty;
        
            public UXSimStatusHandler(OpenSimBase sim)
            {
                m_opensim = sim;
                osUXStatsURI = sim.userStatsURI;
                
            }
            
            public byte[] Handle(string path, Stream request,
                                 OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return Util.UTF8.GetBytes(m_opensim.StatReport(httpRequest));
            }

            public string ContentType
            {
                get { return "text/plain"; }
            }

            public string HttpMethod
            {
                get { return "GET"; }
            }

            public string Path
            {
                // This is for the OpenSimulator instance and is the user provided URI 
                get { return "/" + osUXStatsURI + "/"; }
            }
        }

        #endregion

        /// <summary>
        /// Performs any last-minute sanity checking and shuts down the region server
        /// </summary>
        public override void ShutdownSpecific()
        {
            if (m_shutdownCommandsFile != String.Empty)
            {
                RunCommandScript(m_shutdownCommandsFile);
            }
            if (proxyUrl.Length > 0)
            {
                Util.XmlRpcCommand(proxyUrl, "Stop");
            }

            m_log.Info("[SHUTDOWN]: Closing all threads");
            m_log.Info("[SHUTDOWN]: Killing listener thread");
            m_log.Info("[SHUTDOWN]: Killing clients");
            // TODO: implement this
            m_log.Info("[SHUTDOWN]: Closing console and terminating");

            try
            {
                m_sceneManager.Close();
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[SHUTDOWN]: Ignoring failure during shutdown - {0}", e);
            }
        }

        /// <summary>
        /// Get the start time and up time of Region server
        /// </summary>
        /// <param name="starttime">The first out parameter describing when the Region server started</param>
        /// <param name="uptime">The second out parameter describing how long the Region server has run</param>
        public void GetRunTime(out string starttime, out string uptime)
        {
            starttime = m_startuptime.ToString();
            uptime = (DateTime.Now - m_startuptime).ToString();
        }

        /// <summary>
        /// Get the number of the avatars in the Region server
        /// </summary>
        /// <param name="usernum">The first out parameter describing the number of all the avatars in the Region server</param>
        public void GetAvatarNumber(out int usernum)
        {
            usernum = m_sceneManager.GetCurrentSceneAvatars().Count;
        }

        /// <summary>
        /// Get the number of regions
        /// </summary>
        /// <param name="regionnum">The first out parameter describing the number of regions</param>
        public void GetRegionNumber(out int regionnum)
        {
            regionnum = m_sceneManager.Scenes.Count;
        }
    }

    
    public class OpenSimConfigSource
    {
        public IConfigSource Source;

        public void Save(string path)
        {
            if (Source is IniConfigSource)
            {
                IniConfigSource iniCon = (IniConfigSource) Source;
                iniCon.Save(path);
            }
            else if (Source is XmlConfigSource)
            {
                XmlConfigSource xmlCon = (XmlConfigSource) Source;
                xmlCon.Save(path);
            }
        }
    }
}
