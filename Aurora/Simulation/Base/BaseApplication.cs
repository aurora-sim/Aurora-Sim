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

//#define BlockUnsupportedVersions

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Aurora.Framework.Servers.HttpServer;
using Nini.Config;
using Aurora.Framework;
using log4net.Config;
using System.Net;
using System.Text;

namespace Aurora.Simulation.Base
{
    /// <summary>
    ///   Starting class for the Aurora Server
    /// </summary>
    public class BaseApplication
    {
        /// <summary>
        ///   Save Crashes in the bin/crashes folder.  Configurable with m_crashDir
        /// </summary>
        public static bool m_saveCrashDumps;

        /// <summary>
        ///   Should we send an error report?
        /// </summary>
        public static bool m_sendErrorReport;

        /// <summary>
        ///   Where to post errors
        /// </summary>
        public static string m_urlToPostErrors = "http://aurora-sim.org/CrashReports/crashreports.php";

        /// <summary>
        ///   Loader of configuration files
        /// </summary>
        private static readonly ConfigurationLoader m_configLoader = new ConfigurationLoader();

        /// <summary>
        ///   Directory to save crash reports to.  Relative to bin/
        /// </summary>
        public static string m_crashDir = "crashes";

        private static bool _IsHandlingException; // Make sure we don't go recursive on ourself

        //could move our main function into OpenSimMain and kill this class
        public static void BaseMain(string[] args, string defaultIniFile, ISimulationBase simBase)
        {
            // First line, hook the appdomain to the crash reporter
            AppDomain.CurrentDomain.UnhandledException +=
                CurrentDomain_UnhandledException;

            // Add the arguments supplied when running the application to the configuration
            ArgvConfigSource configSource = new ArgvConfigSource(args);

            if (!args.Contains("-skipconfig"))
                Configure(false);

            // Configure Log4Net
            configSource.AddSwitch("Startup", "logconfig");
            string logConfigFile = configSource.Configs["Startup"].GetString("logconfig", String.Empty);
            if (logConfigFile != String.Empty)
            {
                XmlConfigurator.Configure(new FileInfo(logConfigFile));
                //MainConsole.Instance.InfoFormat("[OPENSIM MAIN]: configured log4net using \"{0}\" as configuration file",
                //                 logConfigFile);
            }
            else
            {
                XmlConfigurator.Configure();
                //MainConsole.Instance.Info("[OPENSIM MAIN]: configured log4net using default OpenSim.exe.config");
            }

            // Increase the number of IOCP threads available. Mono defaults to a tragically low number
            int workerThreads, iocpThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out iocpThreads);
            //MainConsole.Instance.InfoFormat("[OPENSIM MAIN]: Runtime gave us {0} worker threads and {1} IOCP threads", workerThreads, iocpThreads);
            if (workerThreads < 500 || iocpThreads < 1000)
            {
                workerThreads = 500;
                iocpThreads = 1000;
                //MainConsole.Instance.Info("[OPENSIM MAIN]: Bumping up to 500 worker threads and 1000 IOCP threads");
                ThreadPool.SetMaxThreads(workerThreads, iocpThreads);
            }

            // Check if the system is compatible with OpenSimulator.
            // Ensures that the minimum system requirements are met
            //MainConsole.Instance.Info("[Setup]: Performing compatibility checks... \n");
            string supported = String.Empty;
            if (Util.IsEnvironmentSupported(ref supported))
            {
                int minWorker, minIOC;
                // Get the current settings.
                ThreadPool.GetMinThreads(out minWorker, out minIOC);

                //MainConsole.Instance.InfoFormat("[Setup]: Environment is compatible. Thread Workers: {0}, IO Workers {1}\n", minWorker, minIOC);
            }
            else
            {
                MainConsole.Instance.Warn("[Setup]: Environment is unsupported (" + supported + ")\n");
#if BlockUnsupportedVersions
                    Thread.Sleep(10000); //Sleep 10 seconds
                    return;
#endif
            }

            BinMigratorService service = new BinMigratorService();
            service.MigrateBin();
            // Configure nIni aliases and localles
            Culture.SetCurrentCulture();
            configSource.Alias.AddAlias("On", true);
            configSource.Alias.AddAlias("Off", false);
            configSource.Alias.AddAlias("True", true);
            configSource.Alias.AddAlias("False", false);

            //Command line switches
            configSource.AddSwitch("Startup", "inifile");
            configSource.AddSwitch("Startup", "inimaster");
            configSource.AddSwitch("Startup", "inigrid");
            configSource.AddSwitch("Startup", "inisim");
            configSource.AddSwitch("Startup", "inidirectory");
            configSource.AddSwitch("Startup", "oldoptions");
            configSource.AddSwitch("Startup", "inishowfileloading");
            configSource.AddSwitch("Startup", "mainIniDirectory");
            configSource.AddSwitch("Startup", "mainIniFileName");
            configSource.AddSwitch("Startup", "secondaryIniFileName");

            configSource.AddConfig("Network");

            IConfigSource m_configSource = Configuration(configSource, defaultIniFile);

            // Check if we're saving crashes
            m_saveCrashDumps = m_configSource.Configs["Startup"].GetBoolean("save_crashes", m_saveCrashDumps);

            // load Crash directory config
            m_crashDir = m_configSource.Configs["Startup"].GetString("crash_dir", m_crashDir);

            // check auto restart
            bool AutoRestart = m_configSource.Configs["Startup"].GetBoolean("AutoRestartOnCrash", true);

            //Set up the error reporting
            if (m_configSource.Configs["ErrorReporting"] != null)
            {
                m_sendErrorReport = m_configSource.Configs["ErrorReporting"].GetBoolean("SendErrorReports", true);
                m_urlToPostErrors = m_configSource.Configs["ErrorReporting"].GetString("ErrorReportingURL",
                                                                                       m_urlToPostErrors);
            }

            bool Running = true;
            //If auto restart is set, then we always run.
            // otherwise, just run the first time that Running == true
            while (AutoRestart || Running)
            {
                //Always run once, then disable this
                Running = false;
                //Initialize the sim base now
                Startup(configSource, m_configSource, simBase.Copy(), args);
            }
        }

        public static void Configure(bool requested)
        {
            bool Aurora_log = (File.Exists(Path.Combine(Util.configDir(), "Aurora.log")));
            bool Aurora_Server_log = (File.Exists(Path.Combine(Util.configDir(), "AuroraServer.log")));
            bool isAuroraExe = System.AppDomain.CurrentDomain.FriendlyName == "Aurora.exe" ||
                System.AppDomain.CurrentDomain.FriendlyName == "Aurora.vshost.exe";

            if (requested || !(isAuroraExe ?
                Aurora_log : Aurora_Server_log))
            {
                string resp = "no";
                if (!requested)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n\n*************Required Configuration files not found.*************");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n\n   This is your first time running Aurora, if not and you already configured your " +
                        "*ini.example files, please ignore this warning and press enter; " +
                        "Otherwise type yes and Aurora will guide you trough configuration files.\n\nRemember, " +
                        "these file names are Case Sensitive in Linux and Proper Cased.\n1. ./Aurora.ini\nand\n2. " +
                        "./Configuration/Standalone/StandaloneCommon.ini \nor\n3. ./Configuration/Grid/GridCommon.ini\n" +
                        "\nAlso, you will want to examine these files in great detail because only the basic system will " +
                        "load by default. Aurora can do a LOT more if you spend a little time going through these files.\n\n");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("This will overwrite any existing configuration files for your sim");
                    Console.ResetColor();
                    resp = ReadLine("Do you want to configure Aurora now", resp);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("This will overwrite any existing configuration files for your sim");
                    Console.ResetColor();
                    resp = ReadLine("Do you want to configure Aurora now", resp);
                }
                if (resp == "yes")
                {
                    string dbSource = "localhost";
                    string dbPasswd = "aurora";
                    string dbSchema = "aurora";
                    string dbUser = "aurora";
                    string ipAddress = Utilities.GetExternalIp(),
                        gridIPAddress = Utilities.GetExternalIp();
                    string mode = "1";
                    string dbType = "1";
                    string gridName = "Aurora-Sim Grid";
                    string welcomeMessage = "";
                    string allowAnonLogin = "true";
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("====================================================================");
                    Console.WriteLine("========================= AURORA CONFIGURATOR ======================");
                    Console.WriteLine("====================================================================");
                    Console.ResetColor();

                    if (isAuroraExe)
                    {
                        Console.WriteLine("This installation is going to run in");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("[1] Standalone Mode \n[2] Grid Mode");
                        Console.ResetColor();
                        mode = ReadLine("Choose 1 or 2", mode);
                    }

                    Console.WriteLine("Which database do you want to use?");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("[1] SQLite \n[2] MySQL");
                    Console.ResetColor();
                    dbType = ReadLine("Choose 1 or 2", dbType);
                    if (dbType == "2")
                    {
                        Console.WriteLine("Note: this setup does not automatically create a MySQL installation for you.\n" +
                            "You must install MySQL on your own");

                        dbSchema = ReadLine("MySQL database name for your region", dbSchema);
                        dbSource = ReadLine("MySQL database IP", dbSource);
                        dbUser = ReadLine("MySQL database user account", dbUser);

                        Console.Write("MySQL database password for that account: ");
                        dbPasswd = Console.ReadLine();
                    }

                    if (mode == "1")
                    {
                        gridName = ReadLine("Name of your Aurora-Sim Grid", gridName);

                        welcomeMessage = "Welcome to " + gridName + ", <USERNAME>!";
                        welcomeMessage = ReadLine("Welcome Message to show during login (putting <USERNAME> into the " + 
                            "welcome message will insert the user's name)", welcomeMessage);

                        allowAnonLogin = ReadLine("Create accounts automatically when users log in " +
                            "(you don't have to create all accounts manually or have a web interface then)", allowAnonLogin);
                    }

                    ipAddress = ReadLine("Your external domain name or IP address", ipAddress);
                    if (ipAddress.StartsWith("http://"))//IP only!
                        ipAddress = ipAddress.Remove(0, 7);

                    if (mode == "2")
                        gridIPAddress = ReadLine("The external domain name or IP address of the grid server you wish to connect to",
                            ipAddress);

                    if (isAuroraExe)
                    {
                        MakeSureExists("Aurora.ini");
                        IniConfigSource aurora_ini = new IniConfigSource("Aurora.ini", Nini.Ini.IniFileType.AuroraStyle);
                        IniConfigSource aurora_ini_example = new IniConfigSource("Aurora.ini.example", Nini.Ini.IniFileType.AuroraStyle);

                        bool setHostName = false;
                        foreach (IConfig config in aurora_ini_example.Configs)
                        {
                            IConfig newConfig = aurora_ini.AddConfig(config.Name);
                            foreach (string key in config.GetKeys())
                            {
                                if (key == "HostName")
                                {
                                    setHostName = true;
                                    newConfig.Set(key, ipAddress);
                                }
                                //No GUI for mono
                                else if (key == "NoGUI" &&
                                    Util.GetRuntimeEnvironment() == Framework.RuntimeEnvironment.Mono)
                                    newConfig.Set(key, true);
                                //Next 3: Set file loader to the default for mono
                                else if (config.Name == "RegionStartup" && key == "Default" &&
                                    Util.GetRuntimeEnvironment() == Framework.RuntimeEnvironment.Mono)
                                    newConfig.Set(key, "RegionLoaderFileSystem");
                                else if (key == "RegionLoaderFileSystem_Enabled" &&
                                    Util.GetRuntimeEnvironment() == Framework.RuntimeEnvironment.Mono)
                                    newConfig.Set(key, true);
                                else if (key == "RegionLoaderDataBaseSystem_Enabled" &&
                                    Util.GetRuntimeEnvironment() == Framework.RuntimeEnvironment.Mono)
                                    newConfig.Set(key, false);
                                else
                                    newConfig.Set(key, config.Get(key));
                            }
                        }
                        if (!setHostName)
                            aurora_ini.Configs["Network"].Set("HostName", ipAddress);

                        aurora_ini.Save();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your Aurora.ini has been successfully configured");
                        Console.ResetColor();
                    }
                    else
                    {
                        MakeSureExists("Aurora.Server.ini");
                        IniConfigSource aurora_ini = new IniConfigSource("Aurora.Server.ini", Nini.Ini.IniFileType.AuroraStyle);
                        IniConfigSource aurora_ini_example = new IniConfigSource("Aurora.Server.ini.example", Nini.Ini.IniFileType.AuroraStyle);

                        bool setHostName = false;
                        foreach (IConfig config in aurora_ini_example.Configs)
                        {
                            IConfig newConfig = aurora_ini.AddConfig(config.Name);
                            foreach (string key in config.GetKeys())
                            {
                                if (key == "HostName")
                                {
                                    setHostName = true;
                                    newConfig.Set(key, ipAddress);
                                }
                                else
                                    newConfig.Set(key, config.Get(key));
                            }
                        }
                        if (!setHostName)
                            aurora_ini.Configs["Network"].Set("HostName", ipAddress);

                        aurora_ini.Save();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your Aurora.Server.ini has been successfully configured");
                        Console.ResetColor();
                    }

                    //Data.ini setup
                    {
                        string folder = isAuroraExe ? "Configuration/" : "AuroraServerConfiguration/";
                        MakeSureExists(folder + "Data/Data.ini");
                        IniConfigSource data_ini = new IniConfigSource(folder + "Data/Data.ini", Nini.Ini.IniFileType.AuroraStyle);
                        IConfig conf = data_ini.AddConfig("DataFile");
                        if (dbType == "1")
                            conf.Set("Include-SQLite", folder + "Data/SQLite.ini");
                        else
                            conf.Set("Include-MySQL", folder + "Data/MySQL.ini");

                        if(isAuroraExe)
                            conf.Set("Include-FileBased", "Configuration/Data/FileBased.ini");

                        conf = data_ini.AddConfig("AuroraConnectors");
                        conf.Set("ValidateTables", true);

                        data_ini.Save();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your Data.ini has been successfully configured");
                        Console.ResetColor();

                        if (dbType == "2")//MySQL setup
                        {
                            MakeSureExists(folder + "Data/MySQL.ini");
                            IniConfigSource mysql_ini = new IniConfigSource(folder + "Data/MySQL.ini", Nini.Ini.IniFileType.AuroraStyle);
                            IniConfigSource mysql_ini_example = new IniConfigSource(folder + "Data/MySQL.ini.example", Nini.Ini.IniFileType.AuroraStyle);
                            foreach (IConfig config in mysql_ini_example.Configs)
                            {
                                IConfig newConfig = mysql_ini.AddConfig(config.Name);
                                foreach (string key in config.GetKeys())
                                {
                                    if (key == "ConnectionString")
                                        newConfig.Set(key, string.Format("\"Data Source={0};Port=3306;Database={1};User ID={2};Password={3};\"",
                                            dbSource, dbSchema, dbUser, dbPasswd));
                                    else
                                        newConfig.Set(key, config.Get(key));
                                }
                            }
                            mysql_ini.Save();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Your MySQL.ini has been successfully configured");
                            Console.ResetColor();
                        }
                    }

                    if (isAuroraExe)
                    {
                        MakeSureExists("Configuration/Main.ini");
                        IniConfigSource main_ini = new IniConfigSource("Configuration/Main.ini", Nini.Ini.IniFileType.AuroraStyle);

                        IConfig conf = main_ini.AddConfig("Architecture");
                        conf.Set("Include-Includes", "Configuration/Includes.ini");
                        if (mode == "1")
                            conf.Set("Include-Standalone", "Configuration/Standalone/StandaloneCommon.ini");
                        else
                            conf.Set("Include-Grid", "Configuration/Grid/AuroraGridCommon.ini");

                        main_ini.Save();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your Main.ini has been successfully configured");
                        Console.ResetColor();

                        if (mode == "1")
                        {
                            MakeSureExists("Configuration/Standalone/StandaloneCommon.ini");
                            IniConfigSource standalone_ini = new IniConfigSource("Configuration/Standalone/StandaloneCommon.ini", Nini.Ini.IniFileType.AuroraStyle);
                            IniConfigSource standalone_ini_example = new IniConfigSource("Configuration/Standalone/StandaloneCommon.ini.example", Nini.Ini.IniFileType.AuroraStyle);
                            foreach (IConfig config in standalone_ini_example.Configs)
                            {
                                IConfig newConfig = standalone_ini.AddConfig(config.Name);
                                if (newConfig.Name == "GridInfoService")
                                {
                                    newConfig.Set("GridInfoInHandlerPort", 0);
                                    newConfig.Set("login", "http://" + ipAddress + ":9000/");
                                    newConfig.Set("gridname", gridName);
                                    newConfig.Set("gridnick", gridName);
                                }
                                else
                                {
                                    foreach (string key in config.GetKeys())
                                    {
                                        if (key == "WelcomeMessage")
                                            newConfig.Set(key, welcomeMessage);
                                        else if (key == "AllowAnonymousLogin")
                                            newConfig.Set(key, allowAnonLogin);
                                        else
                                            newConfig.Set(key, config.Get(key));
                                    }
                                }
                            }

                            standalone_ini.Save();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Your StandaloneCommon.ini has been successfully configured");
                            Console.ResetColor();
                        }
                        else
                        {
                            MakeSureExists("Configuration/Grid/AuroraGridCommon.ini");
                            IniConfigSource grid_ini = new IniConfigSource("Configuration/Grid/AuroraGridCommon.ini", Nini.Ini.IniFileType.AuroraStyle);

                            conf = grid_ini.AddConfig("Includes");
                            conf.Set("Include-Grid", "Configuration/Grid/Grid.ini");
                            conf = grid_ini.AddConfig("Configuration");
                            conf.Set("RegistrationURI", "http://" + gridIPAddress + ":8003");

                            grid_ini.Save();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Your Grid.ini has been successfully configured");
                            Console.ResetColor();
                        }
                    }
                    if (!isAuroraExe)
                    {
                        MakeSureExists("AuroraServerConfiguration/Login.ini");
                        IniConfigSource login_ini = new IniConfigSource("AuroraServerConfiguration/Login.ini", Nini.Ini.IniFileType.AuroraStyle);
                        IniConfigSource login_ini_example = new IniConfigSource("AuroraServerConfiguration/Login.ini.example", Nini.Ini.IniFileType.AuroraStyle);
                        foreach (IConfig config in login_ini_example.Configs)
                        {
                            IConfig newConfig = login_ini.AddConfig(config.Name);
                            foreach (string key in config.GetKeys())
                            {
                                if (key == "WelcomeMessage")
                                    newConfig.Set(key, welcomeMessage);
                                else if (key == "AllowAnonymousLogin")
                                    newConfig.Set(key, allowAnonLogin);
                                else
                                    newConfig.Set(key, config.Get(key));
                            }
                        }
                        login_ini.Save();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your Login.ini has been successfully configured");
                        Console.ResetColor();

                        MakeSureExists("AuroraServerConfiguration/GridInfoService.ini");
                        IniConfigSource grid_info_ini = new IniConfigSource("AuroraServerConfiguration/GridInfoService.ini", Nini.Ini.IniFileType.AuroraStyle);
                        IConfig conf = grid_info_ini.AddConfig("GridInfoService");
                        conf.Set("GridInfoInHandlerPort", 8002);
                        conf.Set("login", "http://" + ipAddress + ":8002");
                        conf.Set("gridname", gridName);
                        conf.Set("gridnick", gridName);

                        grid_info_ini.Save();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your GridInfoService.ini has been successfully configured");
                        Console.ResetColor();
                    }

                    Console.WriteLine("\n====================================================================\n");
                    Console.ResetColor();
                    Console.WriteLine("Your grid name is ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(gridName);
                    Console.ResetColor();
                    Console.WriteLine("\nYour loginuri is ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("http://" + ipAddress + (isAuroraExe ? ":9000/" : ":8002/"));
                    Console.ResetColor();
                    if (mode == "2")
                    {
                        Console.WriteLine("\nConnected Grid URL: ");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("http://" + ipAddress + ":8003/");
                        Console.ResetColor();
                    }
                    Console.WriteLine("\n====================================================================\n");
                    Console.WriteLine("If you ever want to rerun this configurator, you can type \"run configurator\" into the console to bring this prompt back up.");
                }
            }
        }

        private static void MakeSureExists(string file)
        {
            if (File.Exists(file))
                File.Delete(file);
            File.Create(file).Close();
        }

        private static string ReadLine(string log, string defaultReturn)
        {
            Console.WriteLine(log + ": [" + defaultReturn + "]");
            string mode = Console.ReadLine();
            if (mode == string.Empty)
                mode = defaultReturn;
            if (mode != null)
                mode = mode.Trim();
            return mode;
        }

        public static void Startup(ArgvConfigSource originalConfigSource, IConfigSource configSource,
                                   ISimulationBase simBase, string[] cmdParameters)
        {
            //Get it ready to run
            simBase.Initialize(originalConfigSource, configSource, cmdParameters, m_configLoader);
            try
            {
                //Start it. This starts ALL modules and completes the startup of the application
                simBase.Startup();
                //Run the console now that we are done
                simBase.Run();
            }
            catch (Exception ex)
            {
                if (ex.Message != "Restart") //Internal needs a restart message
                {
                    UnhandledException(false, ex);
                    //Just clean it out as good as we can
                    simBase.Shutdown(false);
                    IRegionLoader[] regionLoaders = simBase.ApplicationRegistry.RequestModuleInterfaces<IRegionLoader>();
#if (!ISWIN)
                    foreach (IRegionLoader loader in regionLoaders)
                    {
                        if (loader != null && loader.Default)
                        {
                            loader.FailedToStartRegions(ex.Message);
                        }
                    }
#else
                    foreach (IRegionLoader loader in regionLoaders.Where(loader => loader != null && loader.Default))
                    {
                        loader.FailedToStartRegions(ex.Message);
                    }
#endif
                }
                //Then let it restart if it needs by sending it back up to 'while (AutoRestart || Running)' above
                return;
            }
            //If it didn't throw an error, it wants to quit
            Environment.Exit(0);
        }

        /// <summary>
        ///   Load the configuration for the Application
        /// </summary>
        /// <param name = "configSource"></param>
        /// <param name = "defaultIniFile"></param>
        /// <returns></returns>
        private static IConfigSource Configuration(IConfigSource configSource, string defaultIniFile)
        {
            if (defaultIniFile != "")
                m_configLoader.defaultIniFile = defaultIniFile;
            return m_configLoader.LoadConfigSettings(configSource);
        }

        /// <summary>
        ///   Global exception handler -- all unhandlet exceptions end up here :)
        /// </summary>
        /// <param name = "sender"></param>
        /// <param name = "e"></param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (_IsHandlingException)
                return;

            _IsHandlingException = true;
            Exception ex = (Exception) e.ExceptionObject;

            UnhandledException(e.IsTerminating, ex);

            _IsHandlingException = false;
        }

        private static void UnhandledException(bool isTerminating, Exception ex)
        {
            string msg = String.Empty;
            msg += "\r\n";
            msg += "APPLICATION EXCEPTION DETECTED" + "\r\n";
            msg += "\r\n";

            msg += "Exception: " + ex + "\r\n";
            if (ex.InnerException != null)
            {
                msg += "InnerException: " + ex.InnerException + "\r\n";
            }

            msg += "\r\n";
            msg += "Application is terminating: " + isTerminating.ToString(CultureInfo.InvariantCulture) + "\r\n";

            MainConsole.Instance.ErrorFormat("[APPLICATION]: {0}", msg);

            handleException(msg, ex);
        }

        /// <summary>
        ///   Deal with sending the error to the error reporting service and saving the dump to the harddrive if needed
        /// </summary>
        /// <param name = "msg"></param>
        /// <param name = "ex"></param>
        public static void handleException(string msg, Exception ex)
        {
            if (m_saveCrashDumps)
            {
                // Log exception to disk
                try
                {
                    if (!Directory.Exists(m_crashDir))
                        Directory.CreateDirectory(m_crashDir);

                    string log = Path.Combine(m_crashDir, Util.GetUniqueFilename("crashDump" +
                                                                                 DateTime.Now.Day + DateTime.Now.Month +
                                                                                 DateTime.Now.Year + ".mdmp"));
                    using (FileStream fs = new FileStream(log, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
                    {
                        MiniDump.Write(fs.SafeFileHandle,
                                       MiniDump.Option.WithThreadInfo | MiniDump.Option.WithProcessThreadData |
                                       MiniDump.Option.WithUnloadedModules | MiniDump.Option.WithHandleData |
                                       MiniDump.Option.WithDataSegs | MiniDump.Option.WithCodeSegs,
                                       MiniDump.ExceptionInfo.Present);
                    }
                }
                catch (Exception e2)
                {
                    MainConsole.Instance.ErrorFormat("[CRASH LOGGER CRASHED]: {0}", e2);
                }
            }

            if (m_sendErrorReport)
            {
                Hashtable param = new Hashtable
                                      {
                                          {"Version", VersionInfo.Version},
                                          {"Message", msg},
                                          {"Platform", Environment.OSVersion.Platform.ToString()}
                                      };
                IList parameters = new ArrayList();
                parameters.Add(param);
                ConfigurableKeepAliveXmlRpcRequest req = new ConfigurableKeepAliveXmlRpcRequest("SendErrorReport", parameters, true);
                try
                {
                    req.Send(m_urlToPostErrors, 10000);
                }
                catch
                {
                }
            }
        }
    }

    public static class MiniDump
    {
        // Taken almost verbatim from http://blog.kalmbach-software.de/2008/12/13/writing-minidumps-in-c/ 

        #region ExceptionInfo enum

        public enum ExceptionInfo
        {
            None,
            Present
        }

        #endregion

        #region Option enum

        [Flags]
        public enum Option : uint
        {
            // From dbghelp.h: 
            Normal = 0x00000000,
            WithDataSegs = 0x00000001,
            WithFullMemory = 0x00000002,
            WithHandleData = 0x00000004,
            FilterMemory = 0x00000008,
            ScanMemory = 0x00000010,
            WithUnloadedModules = 0x00000020,
            WithIndirectlyReferencedMemory = 0x00000040,
            FilterModulePaths = 0x00000080,
            WithProcessThreadData = 0x00000100,
            WithPrivateReadWriteMemory = 0x00000200,
            WithoutOptionalData = 0x00000400,
            WithFullMemoryInfo = 0x00000800,
            WithThreadInfo = 0x00001000,
            WithCodeSegs = 0x00002000,
            WithoutAuxiliaryState = 0x00004000,
            WithFullAuxiliaryState = 0x00008000,
            WithPrivateWriteCopyMemory = 0x00010000,
            IgnoreInaccessibleMemory = 0x00020000,
            ValidTypeFlags = 0x0003ffff,
        };

        #endregion

        //typedef struct _MINIDUMP_EXCEPTION_INFORMATION { 
        //    DWORD ThreadId; 
        //    PEXCEPTION_POINTERS ExceptionPointers; 
        //    BOOL ClientPointers; 
        //} MINIDUMP_EXCEPTION_INFORMATION, *PMINIDUMP_EXCEPTION_INFORMATION; 

        //BOOL 
        //WINAPI 
        //MiniDumpWriteDump( 
        //    __in HANDLE hProcess, 
        //    __in DWORD ProcessId, 
        //    __in HANDLE hFile, 
        //    __in MINIDUMP_TYPE DumpType, 
        //    __in_opt PMINIDUMP_EXCEPTION_INFORMATION ExceptionParam, 
        //    __in_opt PMINIDUMP_USER_STREAM_INFORMATION UserStreamParam, 
        //    __in_opt PMINIDUMP_CALLBACK_INFORMATION CallbackParam 
        //    ); 

        // Overload requiring MiniDumpExceptionInformation 
        [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeHandle hFile, uint dumpType,
                                                     ref MiniDumpExceptionInformation expParam, IntPtr userStreamParam,
                                                     IntPtr callbackParam);

        // Overload supporting MiniDumpExceptionInformation == NULL 
        [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeHandle hFile, uint dumpType,
                                                     IntPtr expParam, IntPtr userStreamParam, IntPtr callbackParam);

        [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
        private static extern uint GetCurrentThreadId();

        public static bool Write(SafeHandle fileHandle, Option options, ExceptionInfo exceptionInfo)
        {
            Process currentProcess = Process.GetCurrentProcess();
            IntPtr currentProcessHandle = currentProcess.Handle;
            uint currentProcessId = (uint) currentProcess.Id;
            MiniDumpExceptionInformation exp;
            exp.ThreadId = GetCurrentThreadId();
            exp.ClientPointers = false;
            exp.ExceptionPointers = IntPtr.Zero;
            if (exceptionInfo == ExceptionInfo.Present)
            {
                exp.ExceptionPointers = Marshal.GetExceptionPointers();
            }
            bool bRet = false;
            if (exp.ExceptionPointers == IntPtr.Zero)
            {
                bRet = MiniDumpWriteDump(currentProcessHandle, currentProcessId, fileHandle, (uint) options, IntPtr.Zero,
                                         IntPtr.Zero, IntPtr.Zero);
            }
            else
            {
                bRet = MiniDumpWriteDump(currentProcessHandle, currentProcessId, fileHandle, (uint) options, ref exp,
                                         IntPtr.Zero, IntPtr.Zero);
            }
            return bRet;
        }

        public static bool Write(SafeHandle fileHandle, Option dumpType)
        {
            return Write(fileHandle, dumpType, ExceptionInfo.None);
        }

        #region Nested type: MiniDumpExceptionInformation

        [StructLayout(LayoutKind.Sequential, Pack = 4)] // Pack=4 is important! So it works also for x64! 
        public struct MiniDumpExceptionInformation
        {
            public uint ThreadId;
            public IntPtr ExceptionPointers;
            [MarshalAs(UnmanagedType.Bool)] public bool ClientPointers;
        }

        #endregion
    }
}