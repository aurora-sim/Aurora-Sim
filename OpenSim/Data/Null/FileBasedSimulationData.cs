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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Timers;
using System.Reflection;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Serialization;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;
using log4net;

namespace OpenSim.Data.Null
{
    /// <summary>
    /// NULL DataStore, do not store anything
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
        protected List<SceneObjectGroup> m_groups = new List<SceneObjectGroup> ();
        protected byte[] m_terrain;
        protected byte[] m_revertTerrain;
        protected bool m_loaded = false;
        protected Timer m_saveTimer = null;
        protected Timer m_backupSaveTimer = null;
        protected IScene m_scene;
        protected bool m_keepOldSave = true;
        protected bool m_oldSaveHasBeenSaved = false;
        protected string m_oldSaveDirectory = "Backups";
        protected string m_loadDirectory = "";
        protected bool m_requiresSave = true;

        public string Name
        {
            get
            {
                return "FileBasedDatabase";
            }
        }

        public ISimulationDataStore Copy ()
        {
            return new FileBasedSimulationData ();
        }

        public void Initialise(string dbfile)
        {
        }

        /// <summary>
        /// Read the config for the data loader
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="config"></param>
        protected void ReadConfig (IScene scene, IConfig config)
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

            scene.AuroraEventManager.OnGenericEvent += AuroraEventManager_OnGenericEvent;

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
                SaveBackup ("");
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
                SaveBackup ("");
                m_requiresSave = false;
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
        protected void SaveBackup (string appendedFilePath)
        {
            if (!m_saveChanges)
                return;
            IBackupModule backupModule = m_scene.RequestModuleInterface<IBackupModule> ();
            if (backupModule != null && backupModule.LoadingPrims) //Something is changing lots of prims
                return;
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

        private string SerializeDateTime ()
        {
            return "--" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Hour + "-" + DateTime.Now.Minute;
        }

        protected void ReadBackup (IScene scene)
        {
            List<uint> foundLocalIDs = new List<uint> ();
            GZipStream m_loadStream;
            try
            {
                m_loadStream = new GZipStream (ArchiveHelpers.GetStream (m_loadDirectory + m_fileName), CompressionMode.Decompress);
            }
            catch
            {
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

                if (filePath.StartsWith ("terrain/"))
                {
                    m_terrain = data;
                }
                else if (filePath.StartsWith ("revertterrain/"))
                {
                    m_revertTerrain = data;
                }
                else if (filePath.StartsWith ("entities/"))
                {
                    MemoryStream ms = new MemoryStream (data);
                    SceneObjectGroup sceneObject = OpenSim.Region.Framework.Scenes.Serialization.SceneObjectSerializer.FromXml2Format (ms, (Scene)scene);
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
        }

        public List<SceneObjectGroup> LoadObjects (UUID regionUUID, Scene scene)
        {
            return m_groups;
        }

        public short[] LoadTerrain (IScene scene, bool RevertMap, int RegionSizeX, int RegionSizeY)
        {
            if (!m_loaded)
            {
                m_loaded = true;
                ReadConfig (scene, scene.Config.Configs["FileBasedSimulationData"]);
                ReadBackup (scene);
                //Disable the backup module so that it doesn't run at all
                IBackupModule backupModule = scene.RequestModuleInterface<IBackupModule> ();
                if (backupModule != null)
                {
                    //No saving of prims as we do that, and we don't need any of the other threads running
                    backupModule.SavePrims = false;
                }
            }
            ITerrainModule terrainModule = scene.RequestModuleInterface<ITerrainModule> ();
            if (RevertMap)
            {
                if (m_revertTerrain == null)
                    return null;
                ITerrainChannel channel = new TerrainChannel (false, scene);
                MemoryStream ms = new MemoryStream (m_revertTerrain);
                if (terrainModule != null)
                    terrainModule.LoadRevertMapFromStream (".r32", ms, 0, 0);
                ms.Close ();
                m_revertTerrain = null;
                if (terrainModule.TerrainRevertMap == null)
                    return null;
                return terrainModule.TerrainRevertMap.GetSerialised (scene);
            }
            else
            {
                if (m_terrain == null)
                    return null;
                ITerrainChannel channel = new TerrainChannel (false, scene);
                MemoryStream ms = new MemoryStream (m_terrain);
                if (terrainModule != null)
                    terrainModule.LoadFromStream (".r32", ms, 0, 0);
                ms.Close ();
                m_terrain = null;
                if (terrainModule.TerrainMap == null)
                    return null;
                return terrainModule.TerrainMap.GetSerialised (scene);
            }
        }

        public short[] LoadWater (UUID regionID, bool RevertMap, int RegionSizeX, int RegionSizeY)
        {
            return null;
        }

        public void Shutdown ()
        {
            //The sim is shutting down, we need to save one last backup
            SaveBackup ("");
        }


        //
        // We don't implement any of these, as they arn't needed by our implementation
        // We do all the saving at once, so we don't need to save the objects every few mins
        //


        public void Dispose()
        {
        }

        public void Tainted ()
        {
            m_requiresSave = true;
        }

        public void StoreObject(SceneObjectGroup obj, UUID regionUUID)
        {
            m_requiresSave = true;
        }

        public void RemoveObject(UUID obj, UUID regionUUID)
        {
            m_requiresSave = true;
        }

        public void StorePrimInventory(UUID primID, ICollection<TaskInventoryItem> items)
        {
            m_requiresSave = true;
        }

        public void StoreTerrain(double[,] ter, UUID regionID)
        {
            m_requiresSave = true;
        }

        public void RemoveObjects(List<UUID> objGroups)
        {
            m_requiresSave = true;
        }

        public void StoreTerrain(short[] terrain, UUID regionID, bool Revert)
        {
            m_requiresSave = true;
        }

        public void StoreWater(short[] water, UUID regionID, bool Revert)
        {
            m_requiresSave = true;
        }

        public void RemoveRegion(UUID regionUUID)
        {
            m_requiresSave = true;
        }

        public void StoreLandObject (ILandObject land)
        {
        }

        public void StoreLandObject (LandData args)
        {
        }

        public void RemoveLandObject (UUID RegionID, UUID ParcelID)
        {
        }

        public void RemoveLandObject (UUID globalID)
        {
        }

        public List<LandData> LoadLandObjects (UUID regionUUID)
        {
            return new List<LandData> ();
        }
    }
}
