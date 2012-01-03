using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nini.Ini;
using Nini.Config;

namespace Aurora.Simulation.Base
{
    public class BinMigratorService
    {
        private const int _currentBinVersion = 2;
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
                    if (currentVersion == 0)
                        RunMigration1();
                    if (currentVersion == 1)
                        RunMigration2();
                    currentVersion++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error running bin migration " + currentVersion + ", " + ex.ToString());
                return false;
            }
            return true;
        }

        private void RunMigration2()
        {
			if (!Directory.Exists("assetcache//")) return;
            foreach (string path in Directory.GetDirectories("assetcache//"))
            {
                Directory.Delete(path, true);
            }
        }

        private void RunMigration1()
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
    }

    public enum MigratorAction
    {
        Add,
        Remove
    }

    public class IniMigrator
    {
        public void UpdateIniFile(string fileName, string handler, string[] names, string[] values, MigratorAction[] actions)
        {
            IniConfigSource doc = new IniConfigSource(fileName, IniFileType.AuroraStyle);
            IConfig section = doc.Configs[handler];
            for(int i = 0; i < names.Length; i++)
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
