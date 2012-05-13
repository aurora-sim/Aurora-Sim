/*
 * Copyright (c) Contributors, http://world.4d-web.eu , http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the name of Nova Project nor the
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
using System.IO;
using System.Net;
using System.Text;
using System.Diagnostics;
using Aurora.Framework;

namespace Aurora.Configuration
{
    public class Configure
    {
        private static string dbSource = "localhost";
        private static string dbPasswd = "aurora";
        private static string dbSchema = "aurora";
        private static string dbUser = "aurora";
        private static string ipAddress = Framework.Utilities.GetExternalIp();
        private static bool auroraReconfig;
        private static string platform = "1";
        private static string mode = "1";
        private static string dbregion = "1";
        private static string worldName = "Aurora-Sim";
        private static string regionFlag = "Aurora";

        private static void CheckAuroraConfigHostName()
        {
            if (!File.Exists("Aurora.ini")) return;
            try
            {
                File.Move("Aurora.ini", "Aurora.ini.old");
            }
            catch
            {
            }
            auroraReconfig = true;
        }

        private static void CheckAuroraConfigData()
        {
            if (!File.Exists("Configuration/Data/Data.ini")) return;
            try
            {
                File.Move("Configuration/Data/Data.ini", "Configuration/Data/Data.ini.old");
            }
            catch
            {
            }
            auroraReconfig = true;
        }

        private static void CheckAuroraServerConfigData()
        {
            if (!File.Exists("AuroraServerConfiguration/Data/Data.ini")) return;
            try
            {
                File.Move("AuroraServerConfiguration/Data/Data.ini", "AuroraServerConfiguration/Data/Data.ini.old");
            }
            catch
            {
            }
            auroraReconfig = true;
        }

        private static void CheckAuroraConfigMySql()
        {
            if (!File.Exists("Configuration/Data/MySQL.ini")) return;
            try
            {
                File.Move("Configuration/Data/MySQL.ini", "Configuration/Data/MySQL.ini.old");
            }
            catch
            {
            }
            auroraReconfig = true;
        }

        private static void CheckAuroraServerMySQL()
        {
            if (!File.Exists("AuroraServerConfiguration/Data/MySQL.ini")) return;
            try
            {
                File.Move("AuroraServerConfiguration/Data/MySQL.ini", "AuroraServerConfiguration/Data/MySQL.ini.old");
            }
            catch
            {
            }
            auroraReconfig = true;
        }

        private static void CheckAuroraConfigMain()
        {
            if (!File.Exists("Configuration/Main.ini")) return;
            try
            {
                File.Move("Configuration/Main.ini", "Configuration/Main.ini.old");
            }
            catch
            {
            }
            auroraReconfig = true;
        }

        private static void CheckAuroraConfigCommon()
        {
            if (!File.Exists("Configuration/Standalone/StandaloneCommon.ini")) return;
            try
            {
                File.Move("Configuration/Standalone/StandaloneCommon.ini", "Configuration/Standalone/StandaloneCommon.ini.old");
            }
            catch
            {
            }
            auroraReconfig = true;
        }

        private static void CheckAuroraGridCommon()
        {
            if (!File.Exists("Configuration/Grid/AuroraGridCommon.ini")) return;
            try
            {
                File.Move("Configuration/Grid/AuroraGridCommon.ini", "Configuration/Grid/AuroraGridCommon.ini.old");
            }
            catch
            {
            }
            auroraReconfig = true;
        }

        private static void CheckAuroraLogin()
        {
            if (!File.Exists("AuroraServerConfiguration/Login.ini")) return;
            try
            {
                File.Move("AuroraServerConfiguration/Login.ini", "AuroraServerConfiguration/Login.ini.old");
            }
            catch
            {
            }
            auroraReconfig = true;
        }

        private static void CheckAuroraGridInfoService()
        {
            if (!File.Exists("AuroraServerConfiguration/GridInfoService.ini")) return;
            try
            {
                File.Move("AuroraServerConfiguration/GridInfoService.ini", "AuroraServerConfiguration/GridInfoService.ini.old");
            }
            catch
            {
            }
            auroraReconfig = true;
        }

        private static void CheckAuroraServer()
        {
            if (!File.Exists("Aurora.Server.ini")) return;
            try
            {
                File.Move("Aurora.Server.ini", "Aurora.Server.ini.old");
            }
            catch
            {
            }
            auroraReconfig = true;
        }

        private static void ConfigureAuroraini()
        {
            CheckAuroraConfigHostName();
            string str = string.Format("Define-<HostName> = \"{0}\"", ipAddress);
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
                                str2 = str;
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
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error configuring Aurora.ini " + exception.Message);
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your Aurora.ini has been successfully configured");
        }

        private static void ConfigureAuroraServer()
        {
            CheckAuroraServer();
            string str = string.Format("Define-<HostName> = \"{0}\"", ipAddress);
            try
            {
                using (TextReader reader = new StreamReader("Aurora.Server.ini.example"))
                {
                    using (TextWriter writer = new StreamWriter("Aurora.Server.ini"))
                    {
                        string str2;
                        while ((str2 = reader.ReadLine()) != null)
                        {
                            if (str2.Contains("Define-<HostName>"))
                            {
                                str2 = str;
                            }
                            if (str2.Contains("127.0.0.1"))
                            {
                                str2 = str2.Replace("127.0.0.1", ipAddress);
                            }
                            if (str2.Contains("Region_RegionName ="))
                            {
                                str2 = str2.Replace("Region_RegionName =", "Region_" + regionFlag.Replace(' ', '_') + " =");
                            }
                            writer.WriteLine(str2);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error configuring Aurora.Server.ini " + exception.Message);
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your Aurora.Server.ini has been successfully configured");
        }

        private static void ConfigureAuroraData()
        {
            CheckAuroraConfigData();
            try
            {
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
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error configuring Data.ini " + exception.Message);
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your Data.ini has been successfully configured");
        }

        private static void ConfigureAuroraServerData()
        {
            CheckAuroraServerConfigData();
            try
            {
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
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error configuring AuroraServer Data.ini " + exception.Message);
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your AuroraServer Data.ini has been successfully configured");
        }

        private static void ConfigureAuroraMain()
        {
            CheckAuroraConfigMain();
            try
            {
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
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error configuring Main.ini " + exception.Message);
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your Main.ini has been successfully configured");
        }

        private static void ConfigureAuroraCommon()
        {
            CheckAuroraConfigCommon();
            try
            {
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
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error configuring StandaloneCommon.ini " + exception.Message);
                Console.ResetColor();
                return;
            }
            Console.WriteLine("Your StandaloneCommon.ini has been successfully configured");
        }

        private static void ConfigureAuroraGridCommon()
        {
            CheckAuroraGridCommon();
            try
            {
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
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error configuring AuroraGridCommon.ini " + exception.Message);
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your AuroraGridCommon.ini has been successfully configured");
        }

        private static void ConfigureAuroraMySql()
        {
            CheckAuroraConfigMySql();
            try
            {
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
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error configuring MySQL.ini " + exception.Message);
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your MySQL.ini has been successfully configured");
        }

        private static void ConfigureAuroraServerMySQL()
        {
            CheckAuroraServerMySQL();
            try
            {
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
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error configuring AuroraServer MySQL.ini " + exception.Message);
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your AuroraServer MySQL.ini has been successfully configured");
        }

        private static void ConfigureAuroraLogin()
        {
            CheckAuroraLogin();
            try
            {
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
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error configuring Login.ini " + exception.Message);
                Console.ResetColor();
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your Login.ini has been successfully configured");
        }

        private static void ConfigureAuroraGridInfoService()
        {
            CheckAuroraGridInfoService();
            try
            {
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
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error configuring GridInfoService.ini " + exception.Message);
                Console.ResetColor();
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Your GridInfoService.ini has been successfully configured");
        }

        private static void DisplayInfo()
        {
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
                Console.WriteLine("\nNow press any key to start AuroraServer.exe \nthen press Enter key to start Aurora.exe.\nUse this name for your Welcome Land: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(regionFlag);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(auroraReconfig
                                      ? "\nNOTE: Aurora-Sim has been reconfigured as Grid Mode.\nPrevious configurations are marked *.old.\nPlease revise the new configurations.\n"
                                      : "Your Aurora-Sim's configuration is complete.\nPlease revise it.");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("====================================================================\n");
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("<Aurora-Sim Configurator v.0.2 by Rico - You can now start Aurora.Server then Aurora.exe>");
                    Console.ReadLine();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("<Aurora-Sim Configurator v.0.2 by Rico - Press Enter key to start your Aurora.Server>");
                    Console.ReadLine();
                    Process AuroraServer = new Process();
                    Process Aurora = new Process();

                    AuroraServer.StartInfo.FileName = "Aurora.Server.exe";
                    Aurora.StartInfo.FileName = "Aurora.exe";

                    AuroraServer.Start();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Press Enter key to start the region...");
                    Console.Read();
                    Aurora.Start();
                }

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
                Console.WriteLine("\nNow press any key to start Aurora.exe.\nUse this name for your Welcome Land: ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(regionFlag);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(auroraReconfig
                                      ? "\nNOTE: Aurora-Sim has been reconfigured as Standalone Mode.\nPrevious configurations are marked *.old.\nPlease revise the new configurations.\n"
                                      : "Your Aurora-Sim's configuration is complete.\nPlease revise it.");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("====================================================================\n");
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("<Aurora-Sim Configurator v.0.2 by Rico - You can now start Aurora.exe>");
                    Console.ReadLine();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("<Aurora-Sim Configurator v.0.2 by Rico - Press Enter key to start your Aurora>");
                    Console.ReadLine();
                    Process Aurora = new Process { StartInfo = { FileName = "Aurora.exe" } };
                    Aurora.Start();
                }
            }

        }

        private static void GetUserInput()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
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

                string str = Console.ReadLine();
                if (str != string.Empty)
                {
                    dbSchema = str;
                }

                Console.ResetColor();
                Console.Write("MySql database IP: [localhost]");
                Console.ForegroundColor = ConsoleColor.Green;

                str = Console.ReadLine();
                if (str != string.Empty)
                {
                    dbSource = str;
                }
                Console.ResetColor();
                Console.Write("MySql database user account: [aurora]");
                Console.ForegroundColor = ConsoleColor.Green;

                str = Console.ReadLine();
                if (str != string.Empty)
                {
                    dbUser = str;
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

                string str = Console.ReadLine();
                if (str != string.Empty)
                {
                    dbSchema = str;
                }

                Console.ResetColor();
                Console.Write("MySql database IP: [localhost]");
                Console.ForegroundColor = ConsoleColor.Green;

                str = Console.ReadLine();
                if (str != string.Empty)
                {
                    dbSource = str;
                }
                Console.ResetColor();
                Console.Write("MySql database user account: [aurora]");
                Console.ForegroundColor = ConsoleColor.Green;

                str = Console.ReadLine();
                if (str != string.Empty)
                {
                    dbUser = str;
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
        }

        public static void Main(string[] args)
        {
            GetUserInput();
            ConfigureAuroraini();
            ConfigureAuroraServer();
            ConfigureAuroraData();
            ConfigureAuroraMySql();
            ConfigureAuroraMain();
            ConfigureAuroraGridCommon();
            ConfigureAuroraServerData();
            ConfigureAuroraServerMySQL();
            ConfigureAuroraLogin();
            ConfigureAuroraGridInfoService();
            ConfigureAuroraCommon();
            DisplayInfo();

        }


    }
}