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
            Configure(args);
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

        public static void Configure(string[] args)
        {
            bool Aurora_log = (File.Exists(Path.Combine(Util.configDir(), "Aurora.log")));
            bool Aurora_Server_log = (File.Exists(Path.Combine(Util.configDir(), "AuroraServer.log")));
            
            Process sProcessName = Process.GetCurrentProcess();
            string sCompare = sProcessName.ToString();

            if ((args.Contains("-skipconfig") || ((Process.GetCurrentProcess().MainModule.ModuleName == "Aurora.exe" ||
                Process.GetCurrentProcess().MainModule.ModuleName == "Aurora.vshost.exe")
                && ((Aurora_log) && (new FileInfo("Aurora.log").Length > 0)))
                || ((Process.GetCurrentProcess().MainModule.ModuleName == "Aurora.Server.exe" ||
                Process.GetCurrentProcess().MainModule.ModuleName == "Aurora.Server.vshost.exe")
                && ((Aurora_Server_log) && (new FileInfo("AuroraServer.log").Length > 0)))))
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Required Configuration Files Found\n");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n\n*************Required Configuration files not found.*************");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n\n   This is your first time running Aurora, if not and you already configured your *ini.example files, please ignore this warning and press enter; Otherwise type yes and Aurora will guide you trough configuration files.\n\nRemember, these file names are Case Sensitive in Linux and Proper Cased.\n1. ./Aurora.ini\nand\n2. ./Configuration/Standalone/StandaloneCommon.ini \nor\n3. ./Configuration/Grid/GridCommon.ini\n\nAlso, you will want to examine these files in great detail because only the basic system will load by default. Aurora can do a LOT more if you spend a little time going through these files.\n\n");
                Console.ForegroundColor = ConsoleColor.Green;
                string resp = "no";
                Console.WriteLine("Do you want to configure Aurora now? [no] : ");
                resp = Console.ReadLine();

                if (resp == "yes")
                {
                    string dbSource = "localhost";
                    string dbPasswd = "aurora";
                    string dbSchema = "aurora";
                    string dbUser = "aurora";
                    string ipAddress = Framework.Utilities.GetExternalIp();
                    string platform = "1";
                    string mode = "1";
                    string dbregion = "1";
                    string worldName = "Aurora-Sim";
                    string regionFlag = "Aurora";
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("====================================================================\n");
            Console.WriteLine("========================= AURORA CONFIGURATOR ======================\n");
            Console.WriteLine("====================================================================\n");
            Console.ResetColor();

            Console.Write("This installation is going to run in \n");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[1] Standalone Mode \n[2] Grid Mode");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\nChoose 1 or 2 [1]: ");
            Console.ForegroundColor = ConsoleColor.Green;
            mode = Console.ReadLine();
            if (mode == string.Empty)
            {
                mode = "1";
            }
            if (mode != null) mode = mode.Trim();
            Console.ResetColor();
            Console.Write("Which database do you want to use for the region ? \n(this will not affect Aurora.Server)");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\n[1] MySQL \n[2] SQLite");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\nChoose 1 or 2 [1]: ");
            Console.ForegroundColor = ConsoleColor.Green;
            dbregion = Console.ReadLine();
            if (dbregion == string.Empty)
            {
                dbregion = "1";
            }
            if (dbregion != null) dbregion = dbregion.Trim();
            Console.ResetColor();
            Console.Write("Name of your Aurora-Sim: ");
            Console.ForegroundColor = ConsoleColor.Green;

            worldName = Console.ReadLine();
            if (worldName != null) worldName = worldName == string.Empty ? "My Aurora" : worldName.Trim();
            Console.ResetColor();
            if (dbregion != null && dbregion.Equals("1"))
            {
                Console.Write("MySql database name for your region: [aurora]");
                Console.ForegroundColor = ConsoleColor.Green;

                string str2 = Console.ReadLine();
                if (str2 != string.Empty)
                {
                    dbSchema = str2;
                }

                Console.ResetColor();
                Console.Write("MySql database IP: [localhost]");
                Console.ForegroundColor = ConsoleColor.Green;

                string str3 = Console.ReadLine();
                if (str3 != string.Empty)
                {
                    dbSource = str3;
                }
                Console.ResetColor();
                Console.Write("MySql database user account: [aurora]");
                Console.ForegroundColor = ConsoleColor.Green;

                string str4 = Console.ReadLine();
                if (str4 != string.Empty)
                {
                    dbUser = str4;
                }
                Console.ResetColor();
                Console.Write("MySql database password for that account: ");
                Console.ForegroundColor = ConsoleColor.Green;

                dbPasswd = Console.ReadLine();
            }
            if (mode != null && mode.Equals("2"))
            {
                Console.ResetColor();
                Console.Write("MySql database name for Aurora.Server: [aurora]");
                Console.ForegroundColor = ConsoleColor.Green;

                string str5 = Console.ReadLine();
                if (str5 != string.Empty)
                {
                    dbSchema = str5;
                }

                Console.ResetColor();
                Console.Write("MySql database IP: [localhost]");
                Console.ForegroundColor = ConsoleColor.Green;

                string str6 = Console.ReadLine();
                if (str6 != string.Empty)
                {
                    dbSource = str6;
                }
                Console.ResetColor();
                Console.Write("MySql database user account: [aurora]");
                Console.ForegroundColor = ConsoleColor.Green;

                string str7 = Console.ReadLine();
                if (str7 != string.Empty)
                {
                    dbUser = str7;
                }
                Console.ResetColor();
                Console.Write("MySql database password for that account: ");
                Console.ForegroundColor = ConsoleColor.Green;

                dbPasswd = Console.ReadLine();
            }
            Console.ResetColor();
            Console.Write("Your external domain name (preferred) or IP address: [" + Framework.Utilities.GetExternalIp() + "]");
            Console.ForegroundColor = ConsoleColor.Green;

            ipAddress = Console.ReadLine();
            if (ipAddress == string.Empty)
            {
                ipAddress = Framework.Utilities.GetExternalIp();
            }
            Console.ResetColor();
            Console.Write("The name you will use for your Welcome Land: ");
            Console.ForegroundColor = ConsoleColor.Green;

            regionFlag = Console.ReadLine();
            if (regionFlag == string.Empty)
            {
                regionFlag = "Aurora";
            }
            Console.ResetColor();
            Console.Write("This installation is going to run on");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\n[1] .NET/Windows \n[2] *ix/Mono");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\nChoose 1 or 2 [1]: ");
            Console.ForegroundColor = ConsoleColor.Green;

            platform = Console.ReadLine();
            if (platform == string.Empty)
            {
                platform = "1";
            }
            if (platform != null) platform = platform.Trim();
            
            string str8 = string.Format("Define-<HostName> = \"{0}\"", ipAddress);
            try
            {
                using (TextReader reader = new StreamReader("Aurora.ini.example"))
                {
                    using (TextWriter writer = new StreamWriter("Aurora.ini"))
                    {
                        string str2;
                        while ((str2 = reader.ReadLine()) != null)
                        {
                            if (str2.Contains("Define-<HostName>"))
                            {
                                str2 = str8;
                            }
                            if (str2.Contains("127.0.0.1"))
                            {
                                str2 = str2.Replace("127.0.0.1", ipAddress);
                            }
                            if (str2.Contains("NoGUI = false") && platform.Equals("2"))
                            {
                                str2 = str2.Replace("NoGUI = false", "NoGUI = true");
                            }
                            if (str2.Contains("Default = RegionLoaderDataBaseSystem") && platform.Equals("2"))
                            {
                                str2 = str2.Replace("Default = RegionLoaderDataBaseSystem", "Default = RegionLoaderFileSystem");
                            }
                            if (str2.Contains("RegionLoaderDataBaseSystem_Enabled = true") && platform.Equals("2"))
                            {
                                str2 = str2.Replace("RegionLoaderDataBaseSystem_Enabled = true", "RegionLoaderDataBaseSystem_Enabled = false");
                            }
                            if (str2.Contains("RegionLoaderFileSystem_Enabled = false") && platform.Equals("2"))
                            {
                                str2 = str2.Replace("RegionLoaderFileSystem_Enabled = false", "RegionLoaderFileSystem_Enabled = true");
                            }
                            if (str2.Contains("RegionLoaderWebServer_Enabled = true") && platform.Equals("2"))
                            {
                                str2 = str2.Replace("RegionLoaderWebServer_Enabled = true", "RegionLoaderWebServer_Enabled = false");
                            }
                            writer.WriteLine(str2);
                        }
                    }
                }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your Aurora.ini has been successfully configured");
            
        
            string str9 = string.Format("Define-<HostName> = \"{0}\"", ipAddress);
            
                using (TextReader reader = new StreamReader("AuroraServer.ini.example"))
                {
                    using (TextWriter writer = new StreamWriter("AuroraServer.ini"))
                    {
                        string str4;
                        while ((str4 = reader.ReadLine()) != null)
                        {
                            if (str4.Contains("Define-<HostName>"))
                            {
                                str4 = str9;
                            }
                            if (str4.Contains("127.0.0.1"))
                            {
                                str4 = str4.Replace("127.0.0.1", ipAddress);
                            }
                            if (str4.Contains("Region_RegionName ="))
                            {
                                str4 = str4.Replace("Region_RegionName =", "Region_" + regionFlag.Replace(' ', '_') + " =");
                            }
                            writer.WriteLine(str4);
                        }
                    }
                }
            
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your AuroraServer.ini has been successfully configured");
        
            using (TextReader reader = new StreamReader("Configuration/Data/Data.ini.example"))
                {
                    using (TextWriter writer = new StreamWriter("Configuration/Data/Data.ini"))
                    {
                        string str2;
                        while ((str2 = reader.ReadLine()) != null)
                        {
                            if (str2.Contains("Include-SQLite = Configuration/Data/SQLite.ini") && dbregion.Equals("1"))
                            {
                                str2 = str2.Replace("Include-SQLite = Configuration/Data/SQLite.ini", ";Include-SQLite = Configuration/Data/SQLite.ini");
                            }
                            if (str2.Contains(";Include-MySQL = Configuration/Data/MySQL.ini") && dbregion.Equals("1"))
                            {
                                str2 = str2.Replace(";Include-MySQL = Configuration/Data/MySQL.ini", "Include-MySQL = Configuration/Data/MySQL.ini");
                            }

                            writer.WriteLine(str2);
                        }
                    }
                }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your Data.ini has been successfully configured");
        
            using (TextReader reader = new StreamReader("AuroraServerConfiguration/Data/Data.ini.example"))
                {
                    using (TextWriter writer = new StreamWriter("AuroraServerConfiguration/Data/Data.ini"))
                    {
                        string str2;
                        while ((str2 = reader.ReadLine()) != null)
                        {
                            if (str2.Contains("Include-SQLite = AuroraServerConfiguration/Data/SQLite.ini"))
                            {
                                str2 = str2.Replace("Include-SQLite = AuroraServerConfiguration/Data/SQLite.ini", ";Include-SQLite = AuroraServerConfiguration/Data/SQLite.ini");
                            }
                            if (str2.Contains(";Include-MySQL = AuroraServerConfiguration/Data/MySQL.ini"))
                            {
                                str2 = str2.Replace(";Include-MySQL = AuroraServerConfiguration/Data/MySQL.ini", "Include-MySQL = AuroraServerConfiguration/Data/MySQL.ini");
                            }

                            writer.WriteLine(str2);
                        }
                    }
                }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your AuroraServer Data.ini has been successfully configured");
    
            using (TextReader reader = new StreamReader("Configuration/Main.ini.example"))
                {
                    using (TextWriter writer = new StreamWriter("Configuration/Main.ini"))
                    {
                        string str2;
                        while ((str2 = reader.ReadLine()) != null)
                        {
                            if (str2.Contains("Include-Standalone = Configuration/Standalone/StandaloneCommon.ini") && (mode.Equals("2")))
                            {
                                str2 = str2.Replace("Include-Standalone = Configuration/Standalone/StandaloneCommon.ini", ";Include-Standalone = Configuration/Standalone/StandaloneCommon.ini");
                            }
                            if (str2.Contains(";Include-Grid = Configuration/Grid/AuroraGridCommon.ini") && (mode.Equals("2")))
                            {
                                str2 = str2.Replace(";Include-Grid = Configuration/Grid/AuroraGridCommon.ini", "Include-Grid = Configuration/Grid/AuroraGridCommon.ini");
                            }
                            writer.WriteLine(str2);
                        }
                    }
                }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your Main.ini has been successfully configured");
        
            using (TextReader reader = new StreamReader("Configuration/Standalone/StandaloneCommon.ini.example"))
                {
                    using (TextWriter writer = new StreamWriter("Configuration/Standalone/StandaloneCommon.ini"))
                    {
                        string str2;
                        while ((str2 = reader.ReadLine()) != null)
                        {
                            if (str2.Contains("Region_Aurora ="))
                            {
                                str2 = str2.Replace("Region_Aurora =", "Region_" + regionFlag.Replace(' ', '_') + " =");
                            }
                            if (str2.Contains("127.0.0.1"))
                            {
                                str2 = str2.Replace("127.0.0.1", ipAddress);
                            }
                            if (str2.Contains("My Aurora Simulator"))
                            {
                                str2 = str2.Replace("My Aurora Simulator", worldName);
                            }
                            if (str2.Contains("AuroraSim"))
                            {
                                str2 = str2.Replace("AuroraSim", worldName);
                            }
                            if (str2.Contains("Welcome to Aurora Simulator"))
                            {
                                str2 = str2.Replace("Welcome to Aurora Simulator", "Welcome to " + worldName);
                            }
                            if (str2.Contains("AllowAnonymousLogin = false"))
                            {
                                str2 = str2.Replace("AllowAnonymousLogin = false", "AllowAnonymousLogin = true");
                            }
                            if (str2.Contains("DefaultHomeRegion = "))
                            {
                                str2 = str2.Replace("DefaultHomeRegion = \"\"", "DefaultHomeRegion = \"" + regionFlag + "\"");
                            }
                            writer.WriteLine(str2);
                        }
                    }
                }
            
            Console.WriteLine("Your StandaloneCommon.ini has been successfully configured");
   
            using (TextReader reader = new StreamReader("Configuration/Grid/AuroraGridCommon.ini.example"))
                {
                    using (TextWriter writer = new StreamWriter("Configuration/Grid/AuroraGridCommon.ini"))
                    {
                        string str2;
                        while ((str2 = reader.ReadLine()) != null)
                        {
                            if (str2.Contains("127.0.0.1"))
                            {
                                str2 = str2.Replace("127.0.0.1", ipAddress);
                            }

                            writer.WriteLine(str2);
                        }
                    }
                }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your AuroraGridCommon.ini has been successfully configured");
        

            using (TextReader reader = new StreamReader("Configuration/Data/MySQL.ini.example"))
                {
                    using (TextWriter writer = new StreamWriter("Configuration/Data/MySQL.ini"))
                    {
                        string str2;
                        while ((str2 = reader.ReadLine()) != null)
                        {
                            if (str2.Contains("Database=opensim;User ID=opensim;Password=***;"))
                            {
                                str2 = str2.Replace("Database=opensim;User ID=opensim;Password=***;", "Database=" + dbSchema + ";User ID=" + dbUser + ";Password=" + dbPasswd + ";");
                            }
                            if (str2.Contains("Data Source=localhost"))
                            {
                                str2 = str2.Replace("Data Source=localhost", "Data Source=" + dbSource);
                            }

                            writer.WriteLine(str2);
                        }
                    }
                }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your MySQL.ini has been successfully configured");
        
            using (TextReader reader = new StreamReader("AuroraServerConfiguration/Data/MySQL.ini.example"))
                {
                    using (TextWriter writer = new StreamWriter("AuroraServerConfiguration/Data/MySQL.ini"))
                    {
                        string str2;
                        while ((str2 = reader.ReadLine()) != null)
                        {
                            if (str2.Contains("Database=opensim;User ID=opensim;Password=***;"))
                            {
                                str2 = str2.Replace("Database=opensim;User ID=opensim;Password=***;", "Database=" + dbSchema + ";User ID=" + dbUser + ";Password=" + dbPasswd + ";");
                            }
                            if (str2.Contains("Data Source=localhost"))
                            {
                                str2 = str2.Replace("Data Source=localhost", "Data Source=" + dbSource);
                            }

                            writer.WriteLine(str2);
                        }
                    }
                }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your AuroraServer MySQL.ini has been successfully configured");
        
                using (TextReader reader = new StreamReader("AuroraServerConfiguration/Login.ini.example"))
                {
                    using (TextWriter writer = new StreamWriter("AuroraServerConfiguration/Login.ini"))
                    {
                        string str2;
                        while ((str2 = reader.ReadLine()) != null)
                        {
                            if (str2.Contains("Welcome to Aurora Simulator"))
                            {
                                str2 = str2.Replace("Welcome to Aurora Simulator", "Welcome to " + worldName);
                            }
                            if (str2.Contains("AllowAnonymousLogin = false"))
                            {
                                str2 = str2.Replace("AllowAnonymousLogin = false", "AllowAnonymousLogin = true");
                            }
                            if (str2.Contains("DefaultHomeRegion = "))
                            {
                                str2 = str2.Replace("DefaultHomeRegion = \"\"", "DefaultHomeRegion = \"" + regionFlag + "\"");
                            }
                            writer.WriteLine(str2);
                        }
                    }
                }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your Login.ini has been successfully configured");
        
            using (TextReader reader = new StreamReader("AuroraServerConfiguration/GridInfoService.ini.example"))
                {
                    using (TextWriter writer = new StreamWriter("AuroraServerConfiguration/GridInfoService.ini"))
                    {
                        string str2;
                        while ((str2 = reader.ReadLine()) != null)
                        {
                            if (str2.Contains("127.0.0.1"))
                            {
                                str2 = str2.Replace("127.0.0.1", ipAddress);
                            }
                            if (str2.Contains("the lost continent of hippo"))
                            {
                                str2 = str2.Replace("the lost continent of hippo", worldName);
                            }
                            if (str2.Contains("hippogrid"))
                            {
                                str2 = str2.Replace("hippogrid", worldName);
                            }
                            writer.WriteLine(str2);
                        }
                    }
                }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your GridInfoService.ini has been successfully configured");
        
            if (mode.Equals("2"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("====================================================================\n");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Your world is ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(worldName);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\nYour loginuri is ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("http://" + ipAddress + ":8002/");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\nThis is the Registration URL: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("http://" + ipAddress + ":8003/");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\nNow AuroraServer.exe will start \nthen, please, start Aurora.exe.\nUse this name for your Welcome Land: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(regionFlag);
                Console.ForegroundColor = ConsoleColor.White;
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("====================================================================\n");
                

            }
            else if (mode.Equals("1"))
            {
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("====================================================================\n");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Your world is ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(worldName);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\nYour loginuri is ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("http://" + ipAddress + ":9000/");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\nNow Aurora.exe will start.\nPlease : use this name for your Welcome Land: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(regionFlag);
                Console.ForegroundColor = ConsoleColor.White;
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("====================================================================\n");
                
                }

             }
                catch
                {
                }
            }
                
          }
            
        }

        public static void runConfigurator()
        {
            
            Process sProcessName = Process.GetCurrentProcess();
            string sCompare = sProcessName.ToString();

            MainConsole.Instance = new LocalConsole();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n\n*************Running Aurora.Configurator*************");
                
                    string dbSource = "localhost";
                    string dbPasswd = "aurora";
                    string dbSchema = "aurora";
                    string dbUser = "aurora";
                    string ipAddress = Framework.Utilities.GetExternalIp();
                    string platform = "1";
                    string mode = "1";
                    string dbregion = "1";
                    string worldName = "Aurora-Sim";
                    string regionFlag = "Aurora";
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("====================================================================\n");
                    Console.WriteLine("========================= AURORA CONFIGURATOR ======================\n");
                    Console.WriteLine("====================================================================\n");
                    Console.ResetColor();

                    Console.Write("This installation is going to run in \n");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[1] Standalone Mode \n[2] Grid Mode");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("\nChoose 1 or 2 [1]: ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    mode = Console.ReadLine();
                    if (mode == string.Empty)
                    {
                        mode = "1";
                    }
                    if (mode != null) mode = mode.Trim();
                    Console.ResetColor();
                    Console.Write("Which database do you want to use for the region ? \n(this will not affect Aurora.Server)");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("\n[1] MySQL \n[2] SQLite");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("\nChoose 1 or 2 [1]: ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    dbregion = Console.ReadLine();
                    if (dbregion == string.Empty)
                    {
                        dbregion = "1";
                    }
                    if (dbregion != null) dbregion = dbregion.Trim();
                    Console.ResetColor();
                    Console.Write("Name of your Aurora-Sim: ");
                    Console.ForegroundColor = ConsoleColor.Green;

                    worldName = Console.ReadLine();
                    if (worldName != null) worldName = worldName == string.Empty ? "My Aurora" : worldName.Trim();
                    Console.ResetColor();
                    if (dbregion != null && dbregion.Equals("1"))
                    {
                        Console.Write("MySql database name for your region: [aurora]");
                        Console.ForegroundColor = ConsoleColor.Green;

                        string str2 = Console.ReadLine();
                        if (str2 != string.Empty)
                        {
                            dbSchema = str2;
                        }

                        Console.ResetColor();
                        Console.Write("MySql database IP: [localhost]");
                        Console.ForegroundColor = ConsoleColor.Green;

                        string str3 = Console.ReadLine();
                        if (str3 != string.Empty)
                        {
                            dbSource = str3;
                        }
                        Console.ResetColor();
                        Console.Write("MySql database user account: [aurora]");
                        Console.ForegroundColor = ConsoleColor.Green;

                        string str4 = Console.ReadLine();
                        if (str4 != string.Empty)
                        {
                            dbUser = str4;
                        }
                        Console.ResetColor();
                        Console.Write("MySql database password for that account: ");
                        Console.ForegroundColor = ConsoleColor.Green;

                        dbPasswd = Console.ReadLine();
                    }
                    if (mode != null && mode.Equals("2"))
                    {
                        Console.ResetColor();
                        Console.Write("MySql database name for Aurora.Server: [aurora]");
                        Console.ForegroundColor = ConsoleColor.Green;

                        string str5 = Console.ReadLine();
                        if (str5 != string.Empty)
                        {
                            dbSchema = str5;
                        }

                        Console.ResetColor();
                        Console.Write("MySql database IP: [localhost]");
                        Console.ForegroundColor = ConsoleColor.Green;

                        string str6 = Console.ReadLine();
                        if (str6 != string.Empty)
                        {
                            dbSource = str6;
                        }
                        Console.ResetColor();
                        Console.Write("MySql database user account: [aurora]");
                        Console.ForegroundColor = ConsoleColor.Green;

                        string str7 = Console.ReadLine();
                        if (str7 != string.Empty)
                        {
                            dbUser = str7;
                        }
                        Console.ResetColor();
                        Console.Write("MySql database password for that account: ");
                        Console.ForegroundColor = ConsoleColor.Green;

                        dbPasswd = Console.ReadLine();
                    }
                    Console.ResetColor();
                    Console.Write("Your external domain name (preferred) or IP address: [" + Framework.Utilities.GetExternalIp() + "]");
                    Console.ForegroundColor = ConsoleColor.Green;

                    ipAddress = Console.ReadLine();
                    if (ipAddress == string.Empty)
                    {
                        ipAddress = Framework.Utilities.GetExternalIp();
                    }
                    Console.ResetColor();
                    Console.Write("The name you will use for your Welcome Land: ");
                    Console.ForegroundColor = ConsoleColor.Green;

                    regionFlag = Console.ReadLine();
                    if (regionFlag == string.Empty)
                    {
                        regionFlag = "Aurora";
                    }
                    Console.ResetColor();
                    Console.Write("This installation is going to run on");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("\n[1] .NET/Windows \n[2] *ix/Mono");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("\nChoose 1 or 2 [1]: ");
                    Console.ForegroundColor = ConsoleColor.Green;

                    platform = Console.ReadLine();
                    if (platform == string.Empty)
                    {
                        platform = "1";
                    }
                    if (platform != null) platform = platform.Trim();

                    string str8 = string.Format("Define-<HostName> = \"{0}\"", ipAddress);
                    try
                    {
                        using (TextReader reader = new StreamReader("Aurora.ini.example"))
                        {
                            using (TextWriter writer = new StreamWriter("Aurora.ini"))
                            {
                                string str2;
                                while ((str2 = reader.ReadLine()) != null)
                                {
                                    if (str2.Contains("Define-<HostName>"))
                                    {
                                        str2 = str8;
                                    }
                                    if (str2.Contains("127.0.0.1"))
                                    {
                                        str2 = str2.Replace("127.0.0.1", ipAddress);
                                    }
                                    if (str2.Contains("NoGUI = false") && platform.Equals("2"))
                                    {
                                        str2 = str2.Replace("NoGUI = false", "NoGUI = true");
                                    }
                                    if (str2.Contains("Default = RegionLoaderDataBaseSystem") && platform.Equals("2"))
                                    {
                                        str2 = str2.Replace("Default = RegionLoaderDataBaseSystem", "Default = RegionLoaderFileSystem");
                                    }
                                    if (str2.Contains("RegionLoaderDataBaseSystem_Enabled = true") && platform.Equals("2"))
                                    {
                                        str2 = str2.Replace("RegionLoaderDataBaseSystem_Enabled = true", "RegionLoaderDataBaseSystem_Enabled = false");
                                    }
                                    if (str2.Contains("RegionLoaderFileSystem_Enabled = false") && platform.Equals("2"))
                                    {
                                        str2 = str2.Replace("RegionLoaderFileSystem_Enabled = false", "RegionLoaderFileSystem_Enabled = true");
                                    }
                                    if (str2.Contains("RegionLoaderWebServer_Enabled = true") && platform.Equals("2"))
                                    {
                                        str2 = str2.Replace("RegionLoaderWebServer_Enabled = true", "RegionLoaderWebServer_Enabled = false");
                                    }
                                    writer.WriteLine(str2);
                                }
                            }
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your Aurora.ini has been successfully configured");


                        string str9 = string.Format("Define-<HostName> = \"{0}\"", ipAddress);

                        using (TextReader reader = new StreamReader("AuroraServer.ini.example"))
                        {
                            using (TextWriter writer = new StreamWriter("AuroraServer.ini"))
                            {
                                string str4;
                                while ((str4 = reader.ReadLine()) != null)
                                {
                                    if (str4.Contains("Define-<HostName>"))
                                    {
                                        str4 = str9;
                                    }
                                    if (str4.Contains("127.0.0.1"))
                                    {
                                        str4 = str4.Replace("127.0.0.1", ipAddress);
                                    }
                                    if (str4.Contains("Region_RegionName ="))
                                    {
                                        str4 = str4.Replace("Region_RegionName =", "Region_" + regionFlag.Replace(' ', '_') + " =");
                                    }
                                    writer.WriteLine(str4);
                                }
                            }
                        }


                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your AuroraServer.ini has been successfully configured");

                        using (TextReader reader = new StreamReader("Configuration/Data/Data.ini.example"))
                        {
                            using (TextWriter writer = new StreamWriter("Configuration/Data/Data.ini"))
                            {
                                string str2;
                                while ((str2 = reader.ReadLine()) != null)
                                {
                                    if (str2.Contains("Include-SQLite = Configuration/Data/SQLite.ini") && dbregion.Equals("1"))
                                    {
                                        str2 = str2.Replace("Include-SQLite = Configuration/Data/SQLite.ini", ";Include-SQLite = Configuration/Data/SQLite.ini");
                                    }
                                    if (str2.Contains(";Include-MySQL = Configuration/Data/MySQL.ini") && dbregion.Equals("1"))
                                    {
                                        str2 = str2.Replace(";Include-MySQL = Configuration/Data/MySQL.ini", "Include-MySQL = Configuration/Data/MySQL.ini");
                                    }

                                    writer.WriteLine(str2);
                                }
                            }
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your Data.ini has been successfully configured");

                        using (TextReader reader = new StreamReader("AuroraServerConfiguration/Data/Data.ini.example"))
                        {
                            using (TextWriter writer = new StreamWriter("AuroraServerConfiguration/Data/Data.ini"))
                            {
                                string str2;
                                while ((str2 = reader.ReadLine()) != null)
                                {
                                    if (str2.Contains("Include-SQLite = AuroraServerConfiguration/Data/SQLite.ini"))
                                    {
                                        str2 = str2.Replace("Include-SQLite = AuroraServerConfiguration/Data/SQLite.ini", ";Include-SQLite = AuroraServerConfiguration/Data/SQLite.ini");
                                    }
                                    if (str2.Contains(";Include-MySQL = AuroraServerConfiguration/Data/MySQL.ini"))
                                    {
                                        str2 = str2.Replace(";Include-MySQL = AuroraServerConfiguration/Data/MySQL.ini", "Include-MySQL = AuroraServerConfiguration/Data/MySQL.ini");
                                    }

                                    writer.WriteLine(str2);
                                }
                            }
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your AuroraServer Data.ini has been successfully configured");

                        using (TextReader reader = new StreamReader("Configuration/Main.ini.example"))
                        {
                            using (TextWriter writer = new StreamWriter("Configuration/Main.ini"))
                            {
                                string str2;
                                while ((str2 = reader.ReadLine()) != null)
                                {
                                    if (str2.Contains("Include-Standalone = Configuration/Standalone/StandaloneCommon.ini") && (mode.Equals("2")))
                                    {
                                        str2 = str2.Replace("Include-Standalone = Configuration/Standalone/StandaloneCommon.ini", ";Include-Standalone = Configuration/Standalone/StandaloneCommon.ini");
                                    }
                                    if (str2.Contains(";Include-Grid = Configuration/Grid/AuroraGridCommon.ini") && (mode.Equals("2")))
                                    {
                                        str2 = str2.Replace(";Include-Grid = Configuration/Grid/AuroraGridCommon.ini", "Include-Grid = Configuration/Grid/AuroraGridCommon.ini");
                                    }
                                    writer.WriteLine(str2);
                                }
                            }
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your Main.ini has been successfully configured");

                        using (TextReader reader = new StreamReader("Configuration/Standalone/StandaloneCommon.ini.example"))
                        {
                            using (TextWriter writer = new StreamWriter("Configuration/Standalone/StandaloneCommon.ini"))
                            {
                                string str2;
                                while ((str2 = reader.ReadLine()) != null)
                                {
                                    if (str2.Contains("Region_Aurora ="))
                                    {
                                        str2 = str2.Replace("Region_Aurora =", "Region_" + regionFlag.Replace(' ', '_') + " =");
                                    }
                                    if (str2.Contains("127.0.0.1"))
                                    {
                                        str2 = str2.Replace("127.0.0.1", ipAddress);
                                    }
                                    if (str2.Contains("My Aurora Simulator"))
                                    {
                                        str2 = str2.Replace("My Aurora Simulator", worldName);
                                    }
                                    if (str2.Contains("AuroraSim"))
                                    {
                                        str2 = str2.Replace("AuroraSim", worldName);
                                    }
                                    if (str2.Contains("Welcome to Aurora Simulator"))
                                    {
                                        str2 = str2.Replace("Welcome to Aurora Simulator", "Welcome to " + worldName);
                                    }
                                    if (str2.Contains("AllowAnonymousLogin = false"))
                                    {
                                        str2 = str2.Replace("AllowAnonymousLogin = false", "AllowAnonymousLogin = true");
                                    }
                                    if (str2.Contains("DefaultHomeRegion = "))
                                    {
                                        str2 = str2.Replace("DefaultHomeRegion = \"\"", "DefaultHomeRegion = \"" + regionFlag + "\"");
                                    }
                                    writer.WriteLine(str2);
                                }
                            }
                        }

                        Console.WriteLine("Your StandaloneCommon.ini has been successfully configured");

                        using (TextReader reader = new StreamReader("Configuration/Grid/AuroraGridCommon.ini.example"))
                        {
                            using (TextWriter writer = new StreamWriter("Configuration/Grid/AuroraGridCommon.ini"))
                            {
                                string str2;
                                while ((str2 = reader.ReadLine()) != null)
                                {
                                    if (str2.Contains("127.0.0.1"))
                                    {
                                        str2 = str2.Replace("127.0.0.1", ipAddress);
                                    }

                                    writer.WriteLine(str2);
                                }
                            }
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your AuroraGridCommon.ini has been successfully configured");


                        using (TextReader reader = new StreamReader("Configuration/Data/MySQL.ini.example"))
                        {
                            using (TextWriter writer = new StreamWriter("Configuration/Data/MySQL.ini"))
                            {
                                string str2;
                                while ((str2 = reader.ReadLine()) != null)
                                {
                                    if (str2.Contains("Database=opensim;User ID=opensim;Password=***;"))
                                    {
                                        str2 = str2.Replace("Database=opensim;User ID=opensim;Password=***;", "Database=" + dbSchema + ";User ID=" + dbUser + ";Password=" + dbPasswd + ";");
                                    }
                                    if (str2.Contains("Data Source=localhost"))
                                    {
                                        str2 = str2.Replace("Data Source=localhost", "Data Source=" + dbSource);
                                    }

                                    writer.WriteLine(str2);
                                }
                            }
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your MySQL.ini has been successfully configured");

                        using (TextReader reader = new StreamReader("AuroraServerConfiguration/Data/MySQL.ini.example"))
                        {
                            using (TextWriter writer = new StreamWriter("AuroraServerConfiguration/Data/MySQL.ini"))
                            {
                                string str2;
                                while ((str2 = reader.ReadLine()) != null)
                                {
                                    if (str2.Contains("Database=opensim;User ID=opensim;Password=***;"))
                                    {
                                        str2 = str2.Replace("Database=opensim;User ID=opensim;Password=***;", "Database=" + dbSchema + ";User ID=" + dbUser + ";Password=" + dbPasswd + ";");
                                    }
                                    if (str2.Contains("Data Source=localhost"))
                                    {
                                        str2 = str2.Replace("Data Source=localhost", "Data Source=" + dbSource);
                                    }

                                    writer.WriteLine(str2);
                                }
                            }
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your AuroraServer MySQL.ini has been successfully configured");

                        using (TextReader reader = new StreamReader("AuroraServerConfiguration/Login.ini.example"))
                        {
                            using (TextWriter writer = new StreamWriter("AuroraServerConfiguration/Login.ini"))
                            {
                                string str2;
                                while ((str2 = reader.ReadLine()) != null)
                                {
                                    if (str2.Contains("Welcome to Aurora Simulator"))
                                    {
                                        str2 = str2.Replace("Welcome to Aurora Simulator", "Welcome to " + worldName);
                                    }
                                    if (str2.Contains("AllowAnonymousLogin = false"))
                                    {
                                        str2 = str2.Replace("AllowAnonymousLogin = false", "AllowAnonymousLogin = true");
                                    }
                                    if (str2.Contains("DefaultHomeRegion = "))
                                    {
                                        str2 = str2.Replace("DefaultHomeRegion = \"\"", "DefaultHomeRegion = \"" + regionFlag + "\"");
                                    }
                                    writer.WriteLine(str2);
                                }
                            }
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your Login.ini has been successfully configured");

                        using (TextReader reader = new StreamReader("AuroraServerConfiguration/GridInfoService.ini.example"))
                        {
                            using (TextWriter writer = new StreamWriter("AuroraServerConfiguration/GridInfoService.ini"))
                            {
                                string str2;
                                while ((str2 = reader.ReadLine()) != null)
                                {
                                    if (str2.Contains("127.0.0.1"))
                                    {
                                        str2 = str2.Replace("127.0.0.1", ipAddress);
                                    }
                                    if (str2.Contains("the lost continent of hippo"))
                                    {
                                        str2 = str2.Replace("the lost continent of hippo", worldName);
                                    }
                                    if (str2.Contains("hippogrid"))
                                    {
                                        str2 = str2.Replace("hippogrid", worldName);
                                    }
                                    writer.WriteLine(str2);
                                }
                            }
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Your GridInfoService.ini has been successfully configured");

                        if (mode.Equals("2"))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("====================================================================\n");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("Your world is ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(worldName);
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("\nYour loginuri is ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("http://" + ipAddress + ":8002/");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("\nThis is the Registration URL: ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("http://" + ipAddress + ":8003/");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("\nReloading configs for Aurora.Server... ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("Done !");
                            Console.ForegroundColor = ConsoleColor.White;

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("====================================================================\n");
                            Console.ForegroundColor = ConsoleColor.White;

                        }
                        else if (mode.Equals("1"))
                        {

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("====================================================================\n");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("Your world is ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(worldName);
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("\nYour loginuri is ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("http://" + ipAddress + ":9000/");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("\nReloading Configs for Aurora.... ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("Done !!!");
                            Console.ForegroundColor = ConsoleColor.White;

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("====================================================================\n");
                            Console.ForegroundColor = ConsoleColor.White;
                        }

                    }
                    catch
                    {
                    }
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