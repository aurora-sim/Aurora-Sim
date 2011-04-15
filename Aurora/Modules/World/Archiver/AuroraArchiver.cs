using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Reflection;
using OpenSim.Framework;
using OpenSim.Framework.Serialization;
using OpenSim.Framework.Serialization.External;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;
using log4net;

namespace Aurora.Modules
{
    public class AuroraArchiver : ISharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Initialise(IConfigSource source)
        {
            MainConsole.Instance.Commands.AddCommand("save archive", "save archive", "Saves an Aurora Archive", SaveAuroraArchive);
            MainConsole.Instance.Commands.AddCommand("load archive", "load archive", "Loads an Aurora Archive", LoadAuroraArchive);
        }

        public void PostInitialise()
        {
        }

        public void AddRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "AuroraArchiver"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        private void LoadAuroraArchive(string mod, string[] cmd)
        {
            IScene scene = MainConsole.Instance.ConsoleScene;
            if (scene == null)
            {
                m_log.Warn("Select a region first.");
                return;
            }

            string fileName = MainConsole.Instance.CmdPrompt("What file name should we load?", scene.RegionInfo.RegionName + ".backup");

            GZipStream m_loadStream = new GZipStream(ArchiveHelpers.GetStream(fileName), CompressionMode.Decompress);
            TarArchiveReader reader = new TarArchiveReader(m_loadStream);

            LoadRegionBackup(reader, scene);
        }

        private void SaveAuroraArchive(string mod, string[] cmd)
        {
            IScene scene = MainConsole.Instance.ConsoleScene;
            if (scene == null)
            {
                m_log.Warn("Select a region first.");
                return;
            }

            string fileName = MainConsole.Instance.CmdPrompt("What file name will this be saved as?", scene.RegionInfo.RegionName + ".backup");
            
            GZipStream m_saveStream = new GZipStream(new FileStream(fileName, FileMode.Create), CompressionMode.Compress);
            TarArchiveWriter writer = new TarArchiveWriter(m_saveStream);
            
            SaveRegionBackup(writer, scene);
        }

        public void SaveRegionBackup(TarArchiveWriter writer, IScene scene)
        {
            writer.WriteDir("assets"); //Used by many, create it by default

            IAuroraBackupModule[] modules = scene.RequestModuleInterfaces<IAuroraBackupModule>();
            foreach (IAuroraBackupModule module in modules)
                module.SaveModuleToArchive(writer, scene);

            foreach (IAuroraBackupModule module in modules)
            {
                while (module.IsArchiving) //Wait until all are done
                    System.Threading.Thread.Sleep(100);
            }

            writer.Close();
        }

        public void LoadRegionBackup(TarArchiveReader reader, IScene scene)
        {
            IAuroraBackupModule[] modules = scene.RequestModuleInterfaces<IAuroraBackupModule>();

            byte[] data;
            string filePath;
            TarArchiveReader.TarEntryType entryType;

            while ((data = reader.ReadEntry(out filePath, out entryType)) != null)
            {
                if (TarArchiveReader.TarEntryType.TYPE_DIRECTORY == entryType)
                    continue;
                foreach (IAuroraBackupModule module in modules)
                    module.LoadModuleFromArchive(data, filePath, entryType, scene);
            }

            reader.Close();
        }
    }
}
