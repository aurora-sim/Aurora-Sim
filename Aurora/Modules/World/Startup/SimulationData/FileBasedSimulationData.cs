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

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Timers;
using System.Reflection;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Serialization;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using Nini.Config;
using log4net;
using Aurora.Framework;

namespace Aurora.Modules.FileBasedSimulationData
{
    /// <summary>
    /// FileBased DataStore, do not store anything in any databases, instead save .abackup files for it
    /// </summary>
    public class FileBasedSimulationData : ISimulationDataStore
    {
        private static readonly ILog m_log =
           LogManager.GetLogger (MethodBase.GetCurrentMethod ().DeclaringType);
        
        protected string m_fileName = "";
        protected string m_saveAppenedFileName = "";
        protected string m_loadAppenedFileName = "";
        protected int m_timeBetweenSaves = 5;
        protected int m_timeBetweenBackupSaves = 1440;//One day
        protected bool m_saveChanges = true;
        protected bool m_saveBackupChanges = true;
        protected List<ISceneEntity> m_groups = new List<ISceneEntity> ();
        protected byte[] m_terrain;
        protected byte[] m_revertTerrain;
        //For backwards compat
        protected short[] m_shortterrain;
        //For backwards compat
        protected short[] m_shortrevertTerrain;
        //For backwards compat
        protected List<LandData> m_parcels = new List<LandData> ();
        protected byte[] m_water;
        protected byte[] m_revertWater;
        protected bool m_loaded = false;
        protected Timer m_saveTimer = null;
        protected Timer m_backupSaveTimer = null;
        protected IScene m_scene;
        protected bool m_keepOldSave = true;
        protected bool m_oldSaveHasBeenSaved = false;
        protected string m_oldSaveDirectory = "Backups";
        protected string m_saveDirectory = "";
        protected string m_loadDirectory = "";
        protected bool m_requiresSave = true;
        protected bool m_hasShownFileBasedWarning = false;
        protected bool m_saveBackups = false;

        public virtual string Name
        {
            get
            {
                return "FileBasedDatabase";
            }
        }

        public bool SaveBackups
        {
            get
            {
                return m_saveBackups;
            }
            set
            {
                m_saveBackups = value;
            }
        }

        public virtual ISimulationDataStore Copy ()
        {
            return new FileBasedSimulationData ();
        }

        public virtual void Initialise ()
        {
        }

        /// <summary>
        /// Read the config for the data loader
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="config"></param>
        protected virtual void ReadConfig (IScene scene, IConfig config)
        {
            if (config != null)
            {
                m_loadAppenedFileName = config.GetString ("ApendedLoadFileName", m_loadAppenedFileName);
                m_saveAppenedFileName = config.GetString ("ApendedSaveFileName", m_saveAppenedFileName);
                m_saveChanges = config.GetBoolean ("SaveChanges", m_saveChanges);
                m_timeBetweenSaves = config.GetInt ("TimeBetweenSaves", m_timeBetweenSaves);
                m_keepOldSave = config.GetBoolean ("SavePreviousBackup", m_keepOldSave);
                m_oldSaveDirectory = config.GetString ("PreviousBackupDirectory", m_oldSaveDirectory);
                m_loadDirectory = config.GetString ("LoadBackupDirectory", m_loadDirectory);
                m_saveDirectory = config.GetString ("SaveBackupDirectory", m_saveDirectory);
                m_saveBackupChanges = config.GetBoolean ("SaveTimedPreviousBackup", m_keepOldSave);
                m_timeBetweenBackupSaves = config.GetInt ("TimeBetweenBackupSaves", m_timeBetweenBackupSaves);
            }

            if (m_saveChanges && m_timeBetweenSaves != 0)
            {
                m_saveTimer = new Timer (m_timeBetweenSaves * 60 * 1000);
                m_saveTimer.Elapsed += m_saveTimer_Elapsed;
                m_saveTimer.Start ();
            }

            if (m_saveChanges && m_timeBetweenBackupSaves != 0)
            {
                m_backupSaveTimer = new Timer (m_timeBetweenBackupSaves * 60 * 1000);
                m_backupSaveTimer.Elapsed += m_saveTimer_Elapsed;
                m_backupSaveTimer.Start ();
            }

            scene.AuroraEventManager.RegisterEventHandler("Backup", AuroraEventManager_OnGenericEvent);

            m_scene = scene;
            m_fileName = scene.RegionInfo.RegionName + m_loadAppenedFileName + ".abackup";
        }

        /// <summary>
        /// Look for the backup event, and if it is there, trigger the backup of the sim
        /// </summary>
        /// <param name="FunctionName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        object AuroraEventManager_OnGenericEvent (string FunctionName, object parameters)
        {
            if (FunctionName == "Backup")
            {
                m_saveTimer.Stop ();
                m_requiresSave = false;
                SaveBackup (m_saveDirectory + "/");
                m_saveTimer.Start (); //Restart it as we just did a backup
            }
            return null;
        }

        /// <summary>
        /// Save a backup on the timer event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_saveTimer_Elapsed (object sender, ElapsedEventArgs e)
        {
            if (m_requiresSave)
            {
                m_saveTimer.Stop ();
                SaveBackup (m_saveDirectory + "/");
                m_requiresSave = false;
                m_saveTimer.Start (); //Restart it as we just did a backup
            }
            else
                m_log.Info ("[FileBasedSimulationData]: Not saving backup, not required");
        }

        /// <summary>
        /// Save a backup into the oldSaveDirectory on the timer event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_backupSaveTimer_Elapsed (object sender, ElapsedEventArgs e)
        {
            SaveBackup (m_oldSaveDirectory + "/");
        }

        /// <summary>
        /// Save a backup of the sim
        /// </summary>
        /// <param name="appendedFilePath">The file path where the backup will be saved</param>
        protected virtual void SaveBackup (string appendedFilePath)
        {
            if (!m_saveChanges || !m_saveBackups)
                return;
            if (appendedFilePath == "/")
                appendedFilePath = "";
            IBackupModule backupModule = m_scene.RequestModuleInterface<IBackupModule> ();
            if (backupModule != null && backupModule.LoadingPrims) //Something is changing lots of prims
            {
                m_log.Info ("[Backup]: Not saving backup because the backup module is loading prims");
                return;
            }

            //Save any script state saves that might be around
            IScriptModule[] engines = m_scene.RequestModuleInterfaces<IScriptModule> ();
            try
            {
                if (engines != null)
                {
                    foreach (IScriptModule engine in engines)
                    {
                        if (engine != null)
                        {
                            engine.SaveStateSaves ();
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                m_log.WarnFormat ("[Backup]: Exception caught: {0}", ex.ToString());
            }

            ISceneEntity[] entities = m_scene.Entities.GetEntities ();
            try
            {
                foreach (ISceneEntity entity in entities)
                {
                    if (entity.HasGroupChanged)
                        entity.HasGroupChanged = false;
                }
            }
            catch(Exception ex)
            {
                m_log.WarnFormat ("[Backup]: Exception caught: {0}", ex.ToString());
            }

            m_log.Info ("[FileBasedSimulationData]: Saving Backup for region " + m_scene.RegionInfo.RegionName);
            string fileName = appendedFilePath + m_scene.RegionInfo.RegionName + m_saveAppenedFileName + ".abackup";
            //Add the .temp since we might need to make a backup and so that if something goes wrong, we don't corrupt the main backup
            GZipStream m_saveStream = new GZipStream (new FileStream (fileName + ".tmp", FileMode.Create), CompressionMode.Compress);
            TarArchiveWriter writer = new TarArchiveWriter (m_saveStream);
            IAuroraBackupArchiver archiver = m_scene.RequestModuleInterface<IAuroraBackupArchiver> ();
            
            //Turn off prompting so that we don't ask the user questions every time we need to save the backup
            archiver.AllowPrompting = false;
            archiver.SaveRegionBackup (writer, m_scene);
            archiver.AllowPrompting = true;

            //If we got this far, we assume that everything went well, so now we move the stuff around
            if(File.Exists(fileName))
            {
                //If keepOldSave is enabled, the user wants us to move the first backup that we originally loaded from into the oldSaveDirectory
                if (m_keepOldSave && !m_oldSaveHasBeenSaved)
                {
                    //Havn't moved it yet, so make sure the directory exists, then move it
                    m_oldSaveHasBeenSaved = true;
                    if (!Directory.Exists (m_oldSaveDirectory))
                        Directory.CreateDirectory (m_oldSaveDirectory);
                    File.Move (fileName, m_oldSaveDirectory + "/" + m_scene.RegionInfo.RegionName + SerializeDateTime() + m_saveAppenedFileName + ".abackup");
                }
                else //Just remove the file
                    File.Delete (fileName);
            }
            //Now make it the full file again
            File.Move (fileName + ".tmp", fileName);
        }

        protected virtual string SerializeDateTime ()
        {
            return "--" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Hour + "-" + DateTime.Now.Minute;
        }

        protected virtual void ReadBackup (IScene scene)
        {
            List<uint> foundLocalIDs = new List<uint> ();
            GZipStream m_loadStream;
            try
            {
                m_loadStream = new GZipStream (ArchiveHelpers.GetStream (m_loadDirectory + m_fileName), CompressionMode.Decompress);
            }
            catch
            {
                CheckForOldDataBase ();
                return;
            }
            TarArchiveReader reader = new TarArchiveReader (m_loadStream);

            byte[] data;
            string filePath;
            TarArchiveReader.TarEntryType entryType;
            //Load the archive data that we need
            while ((data = reader.ReadEntry (out filePath, out entryType)) != null)
            {
                if (TarArchiveReader.TarEntryType.TYPE_DIRECTORY == entryType)
                    continue;

                if (filePath.StartsWith ("parcels/"))
                {
                    //Only use if we are not merging
                    LandData parcel = new LandData ();
                    OSD parcelData = OSDParser.DeserializeLLSDBinary (data);
                    parcel.FromOSD ((OSDMap)parcelData);
                    m_parcels.Add (parcel);
                }
                else if (filePath.StartsWith ("terrain/"))
                {
                    m_terrain = data;
                }
                else if (filePath.StartsWith ("revertterrain/"))
                {
                    m_revertTerrain = data;
                }
                else if (filePath.StartsWith ("water/"))
                {
                    m_water = data;
                }
                else if (filePath.StartsWith ("revertwater/"))
                {
                    m_revertWater = data;
                }
                else if (filePath.StartsWith ("entities/"))
                {
                    MemoryStream ms = new MemoryStream (data);
                    SceneObjectGroup sceneObject = SceneObjectSerializer.FromXml2Format (ms, scene);
                    ms.Close ();
                    ms = null;
                    data = null;
                    foreach (ISceneChildEntity part in sceneObject.ChildrenEntities ())
                    {
                        if (!foundLocalIDs.Contains (part.LocalId))
                            foundLocalIDs.Add (part.LocalId);
                        else
                            part.LocalId = 0; //Reset it! Only use it once!
                    }
                    m_groups.Add (sceneObject);
                }
            }
            m_loadStream.Close ();
            m_loadStream = null;
            foundLocalIDs.Clear ();
            GC.Collect ();
        }

        protected virtual void CheckForOldDataBase ()
        {
            string connString = "";
            string name = "";
            // Try reading the [DatabaseService] section, if it exists
            IConfig dbConfig = m_scene.Config.Configs["DatabaseService"];
            if (dbConfig != null)
                connString = dbConfig.GetString ("ConnectionString", String.Empty);

            // Try reading the [SimulationDataStore] section
            IConfig simConfig = m_scene.Config.Configs["SimulationDataStore"];
            if (simConfig != null)
            {
                name = simConfig.GetString ("LegacyDatabaseLoaderName", "FileBasedDatabase");
                connString = simConfig.GetString ("ConnectionString", connString);
            }

            ILegacySimulationDataStore simStore = null;
            ILegacySimulationDataStore[] stores = AuroraModuleLoader.PickupModules<ILegacySimulationDataStore> ().ToArray ();
            foreach (ILegacySimulationDataStore store in stores)
            {
                if (store.Name == name)
                {
                    simStore = store;
                    break;
                }
            }
            if (simStore == null)
                return;

            try
            {
                if (!m_hasShownFileBasedWarning)
                {
                    m_hasShownFileBasedWarning = true;
                    System.Windows.Forms.MessageBox.Show (@"Your sim has been updated to use the FileBased Simulation Service.
Your sim is now saved in a .abackup file in the bin/ directory with the same name as your region.
More configuration options and info can be found in the Configuration/Data/FileBased.ini file.", "WARNING");
                }
            }
            catch
            {
                //Some people don't have winforms, which is fine
                m_log.Error ("---------------------");
                m_log.Error ("---------------------");
                m_log.Error ("---------------------");
                m_log.Error ("---------------------");
                m_log.Error ("---------------------");
                m_log.Error ("Your sim has been updated to use the FileBased Simulation Service.");
                m_log.Error ("Your sim is now saved in a .abackup file in the bin/ directory with the same name as your region.");
                m_log.Error ("More configuration options and info can be found in the Configuration/Data/FileBased.ini file.");
                m_log.Error ("---------------------");
                m_log.Error ("---------------------");
                m_log.Error ("---------------------");
                m_log.Error ("---------------------");
                m_log.Error ("---------------------");
            }

            simStore.Initialise (connString);

            IParcelServiceConnector conn = DataManager.DataManager.RequestPlugin<IParcelServiceConnector> ();
            m_parcels = simStore.LoadLandObjects (m_scene.RegionInfo.RegionID);
            m_parcels.AddRange(conn.LoadLandObjects(m_scene.RegionInfo.RegionID));
            m_groups = simStore.LoadObjects (m_scene.RegionInfo.RegionID, m_scene);
            m_shortterrain = simStore.LoadTerrain (m_scene, false, m_scene.RegionInfo.RegionSizeX, m_scene.RegionInfo.RegionSizeY);
            m_shortrevertTerrain = simStore.LoadTerrain (m_scene, true, m_scene.RegionInfo.RegionSizeX, m_scene.RegionInfo.RegionSizeY);
        }

        public virtual List<ISceneEntity> LoadObjects (IScene scene)
        {
            return m_groups;
        }

        public virtual short[] LoadTerrain (IScene scene, bool RevertMap, int RegionSizeX, int RegionSizeY)
        {
            if (!m_loaded)
            {
                m_loaded = true;
                ReadConfig (scene, scene.Config.Configs["FileBasedSimulationData"]);
                ReadBackup (scene);
            }
            ITerrainModule terrainModule = scene.RequestModuleInterface<ITerrainModule> ();
            if (RevertMap)
            {
                ITerrainChannel channel = new TerrainChannel (false, scene);
                if (m_revertTerrain == null)
                {
                    if (m_shortrevertTerrain != null)
                        terrainModule.TerrainRevertMap = new TerrainChannel (m_shortterrain, scene);
                    return null;
                }
                MemoryStream ms = new MemoryStream (m_revertTerrain);
                if (terrainModule != null)
                    terrainModule.LoadRevertMapFromStream (".r32", ms, 0, 0);
                m_revertTerrain = null;
                return null;
            }
            else
            {
                if (m_terrain == null)
                {
                    if (m_shortterrain != null)
                        terrainModule.TerrainMap = new TerrainChannel (m_shortterrain, scene);
                    return null;
                }
                ITerrainChannel channel = new TerrainChannel (false, scene);
                MemoryStream ms = new MemoryStream (m_terrain);
                if (terrainModule != null)
                    terrainModule.LoadFromStream (".r32", ms, 0, 0);
                m_terrain = null;
                return null;
            }
        }

        public virtual short[] LoadWater (IScene scene, bool RevertMap, int RegionSizeX, int RegionSizeY)
        {
            ITerrainModule terrainModule = scene.RequestModuleInterface<ITerrainModule> ();
            if (RevertMap)
            {
                if (m_revertWater == null)
                    return null;
                ITerrainChannel channel = new TerrainChannel (false, scene);
                MemoryStream ms = new MemoryStream (m_revertWater);
                if (terrainModule != null)
                    terrainModule.LoadWaterRevertMapFromStream (".r32", ms, 0, 0);
                m_revertWater = null;
                return null;
            }
            else
            {
                if (m_water == null)
                    return null;
                ITerrainChannel channel = new TerrainChannel (false, scene);
                MemoryStream ms = new MemoryStream (m_water);
                if (terrainModule != null)
                    terrainModule.LoadWaterFromStream (".r32", ms, 0, 0);
                m_water = null;
                return null;
            }
        }

        public virtual void Shutdown ()
        {
            //The sim is shutting down, we need to save one last backup
            SaveBackup (m_saveDirectory + "/");
        }

        public virtual void Tainted ()
        {
            m_requiresSave = true;
        }

        public virtual void RemoveRegion (UUID regionUUID)
        {
            //Remove the file so that the region is gone
            File.Delete (m_loadDirectory + m_fileName);
        }

        /// <summary>
        /// Around for legacy things
        /// </summary>
        /// <param name="regionUUID"></param>
        /// <returns></returns>
        public virtual List<LandData> LoadLandObjects (UUID regionUUID)
        {
            return m_parcels;
        }
    }
}
