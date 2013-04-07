using Nini.Config;
using Nini.Ini;
using System;
using System.IO;
using System.Reflection;

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

        //Next: 9

        public void RunMigration9()
        {
            if (File.Exists("Aurora.UserServer.exe"))
                File.Delete("Aurora.UserServer.exe");
        }
    }

    public enum MigratorAction
    {
        Add,
        Remove
    }

    public class IniMigrator
    {
        public static void UpdateIniFile(string fileName, string handler, string[] names, string[] values,
                                         MigratorAction[] actions)
        {
            if (File.Exists(fileName + ".example")) //Update the .example files too if people haven't
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