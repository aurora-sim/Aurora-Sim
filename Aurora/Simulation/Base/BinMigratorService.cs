using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Nini.Ini;
using Nini.Config;

namespace Aurora.Simulation.Base
{
    public class BinMigratorService
    {
        private const int _currentBinVersion = 8;
        public void MigrateBin()
        {
            int currentVersion = GetBinVersion();
            if (currentVersion != _currentBinVersion)
            {
                UpgradeToTarget(currentVersion);
                SetBinVersion(_currentBinVersion);
            }
        }

        public int GetBinVersion()
        {
            if (!File.Exists("Aurora.version"))
                return 0;
            string file = File.ReadAllText("Aurora.version");
            return int.Parse(file);
        }

        public void SetBinVersion(int version)
        {
            File.WriteAllText("Aurora.version", version.ToString());
        }

        public bool UpgradeToTarget(int currentVersion)
        {
            try
            {
                while (currentVersion != _currentBinVersion)
                {
                    MethodInfo info = GetType().GetMethod("RunMigration" + ++currentVersion);
                    if (info != null)
                        info.Invoke(this, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error running bin migration " + currentVersion + ", " + ex.ToString());
                return false;
            }
            return true;
        }

        public void RunMigration1()
        {
            if (File.Exists("Physics//OpenSim.Region.Physics.BasicPhysicsPlugin.dll"))
                File.Delete("Physics//OpenSim.Region.Physics.BasicPhysicsPlugin.dll");
            if (File.Exists("Physics//OpenSim.Region.Physics.Meshing.dll"))
                File.Delete("Physics//OpenSim.Region.Physics.Meshing.dll");
            if (File.Exists("OpenSim.Framework.dll"))
                File.Delete("OpenSim.Framework.dll");
            if (File.Exists("OpenSim.Region.CoreModules.dll"))
                File.Delete("OpenSim.Region.CoreModules.dll");
            //rsmythe: djphil, this line is for you!
            if (File.Exists("Aurora.Protection.dll"))
                File.Delete("Aurora.Protection.dll");

            foreach(string path in Directory.GetDirectories("ScriptEngines//"))
            {
                Directory.Delete(path, true);
            }
        }

        public void RunMigration2()
        {
            ///Asset format changed, broke existing cached assets
            if (!Directory.Exists("assetcache//")) return;
            foreach (string path in Directory.GetDirectories("assetcache//"))
            {
                Directory.Delete(path, true);
            }
        }

        public void RunMigration3()
        {
            IniMigrator.UpdateIniFile("Configuration/Standalone/Standalone.ini", "AuroraConnectors",
                new[] { "DoRemoteCalls", "AllowRemoteCalls" }, new[] { "False", "False" },
                new[] { MigratorAction.Add, MigratorAction.Add });

            IniMigrator.UpdateIniFile("Configuration/Standalone/StandaloneIWC.ini", "AuroraConnectors",
                new[] { "DoRemoteCalls", "AllowRemoteCalls" }, new[] { "False", "False" },
                new[] { MigratorAction.Add, MigratorAction.Add });

            IniMigrator.UpdateIniFile("Configuration/Grid/Grid.ini", "AuroraConnectors",
                new[] { "DoRemoteCalls", "AllowRemoteCalls" }, new[] { "True", "False" },
                new[] { MigratorAction.Add, MigratorAction.Add });

            IniMigrator.UpdateIniFile("AuroraServerConfiguration/Main.ini", "AuroraConnectors",
                new[] { "DoRemoteCalls", "AllowRemoteCalls" }, new[] { "False", "True" },
                new[] { MigratorAction.Add, MigratorAction.Add });
        }

        public void RunMigration4()
        {
            IniMigrator.UpdateIniFile("Configuration/Grid/Grid.ini", "AuroraConnectors",
                new[] { "EstateConnector" }, new[] { "LocalConnector" },
                new[] { MigratorAction.Add, MigratorAction.Add });
            IniMigrator.UpdateIniFile("AuroraServerConfiguration/Main.ini", "RegionPermissions",
                new[] { "DefaultRegionThreatLevel" }, new[] { "High" },
                new[] { MigratorAction.Add, MigratorAction.Add });
        }

        public void RunMigration5()
        {
            IniMigrator.UpdateIniFile("AuroraServerConfiguration/Main.ini", "RegionPermissions",
                new[] { "Threat_Level_None", "Threat_Level_Low", "Threat_Level_Medium", "Threat_Level_High","Threat_Level_Full" },
                new[] { "", "", "", "", ""},
                new[] { MigratorAction.Add, MigratorAction.Add, MigratorAction.Add, MigratorAction.Add, MigratorAction.Add });
        }

        public void RunMigration6()
        {
            ///Asset format changed to protobuf, broke existing cached assets
            if (!Directory.Exists("assetcache//")) return;
            foreach (string path in Directory.GetDirectories("assetcache//"))
            {
                Directory.Delete(path, true);
            }
        }

        public void RunMigration7()
        {
            ///Asset type was wrong, need to nuke
            if (!Directory.Exists("assetcache//")) return;
            foreach (string path in Directory.GetDirectories("assetcache//"))
            {
                Directory.Delete(path, true);
            }
        }

        public void RunMigration8()
        {
            if (!File.Exists("AuroraServer.ini")) return;
            try
            {
                File.Move("AuroraServer.ini", "Aurora.Server.ini");
            }
            catch
            {
            }
        }
    }

    public enum MigratorAction
    {
        Add,
        Remove
    }

    public class IniMigrator
    {
        public static void UpdateIniFile(string fileName, string handler, string[] names, string[] values, MigratorAction[] actions)
        {
            if (File.Exists(fileName + ".example"))//Update the .example files too if people haven't
                UpdateIniFile(fileName + ".example", handler, names, values, actions);
            if (File.Exists(fileName))
            {
                IniConfigSource doc = new IniConfigSource(fileName, IniFileType.AuroraStyle);
                IConfig section = doc.Configs[handler];
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i];
                    string value = values[i];
                    MigratorAction action = actions[i];
                    if (action == MigratorAction.Add)
                        section.Set(name, value);
                    else
                        section.Remove(name);
                }
                doc.Save();
            }
        }
    }
}
