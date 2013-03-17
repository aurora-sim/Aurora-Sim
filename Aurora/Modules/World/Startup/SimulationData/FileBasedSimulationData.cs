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
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Windows.Forms;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Serialization;
using Timer = System.Timers.Timer;
using Aurora.Management;
using ProtoBuf.Meta;
using ProtoBuf;
using Aurora.Region;

namespace Aurora.Modules
{
    /// <summary>
    ///   FileBased DataStore, do not store anything in any databases, instead save .abackup files for it
    /// </summary>
    public class FileBasedSimulationData : ISimulationDataStore
    {
        protected Timer m_backupSaveTimer;

        protected string m_fileName = "";
        protected bool m_loaded = false;
        protected bool m_keepOldSave = true;
        protected string m_oldSaveDirectory = "Backups";
        protected bool m_oldSaveHasBeenSaved;
        protected bool m_requiresSave = true;
        protected bool m_displayNotSavingNotice = true;
        protected bool m_saveBackupChanges = true;
        protected bool m_saveBackups;
        protected bool m_saveChanges = true;
        protected string m_storeDirectory = "";
        protected Timer m_saveTimer;
        protected IScene m_scene;
        protected int m_timeBetweenBackupSaves = 1440; //One day
        protected int m_timeBetweenSaves = 5;
        protected bool m_shutdown = false;
        protected IRegionDataLoader _regionLoader;
        protected IRegionDataLoader _oldRegionLoader;
        protected RegionData _regionData;
        protected Object m_saveLock = new Object();

        #region ISimulationDataStore Members

        public bool MapTileNeedsGenerated
        {
            get;
            set;
        }

        public virtual string Name
        {
            get { return "FileBasedDatabase"; }
        }

        public bool SaveBackups
        {
            get { return m_saveBackups; }
            set { m_saveBackups = value; }
        }

        public virtual void CacheDispose()
        {
            _regionData.Dispose();
            _regionData = null;
        }

        public virtual void Initialise()
        {
            _regionLoader = new ProtobufRegionDataLoader();
            _oldRegionLoader = new TarRegionDataLoader();
        }

        public virtual RegionInfo LoadRegionInfo(ISimulationBase simBase, out bool newRegion)
        {
            newRegion = false;
            ReadConfig(simBase);
            ReadBackup();
            RegionInfo info;

            bool noGui = false;
            IConfig startupconfig = simBase.ConfigSource.Configs["Startup"];
            if (startupconfig != null)
                noGui = startupconfig.GetBoolean("NoGUI", false);

            if (_regionData == null || _regionData.RegionInfo == null)
            {
            retry:
                if (noGui)
                {
                    info = ReadRegionInfoFromFile(ref _regionData.RegionInfo, out newRegion);
                }
                else
                {
                    try
                    {
                        info = Aurora.Management.RegionManagerHelper.StartSynchronously(true,
                            Management.RegionManagerPage.CreateRegion,
                            simBase.ConfigSource, simBase.ApplicationRegistry.RequestModuleInterface<IRegionManagement>(), null);
                    }
                    catch
                    {
                        noGui = true;
                        goto retry;
                    }
                }
                if (info == null)
                    goto retry;
                newRegion = true;
            }
            else
            {
                if (noGui)
                    info = ReadRegionInfoFromFile(ref _regionData.RegionInfo, out newRegion);
                else
                    info = _regionData.RegionInfo;
            }
            return info;
        }

        private RegionInfo ReadRegionInfoFromFile(ref RegionInfo info, out bool newRegion)
        {
            if (!File.Exists(Path.Combine("Regions", "RegionConfig.ini")))
            {
                newRegion = true;
                return info = CreateRegionFromConsole();
            }
            newRegion = false;
            try
            {
                Nini.Ini.IniDocument doc = new Nini.Ini.IniDocument(Path.Combine("Regions", "RegionConfig.ini"), Nini.Ini.IniFileType.AuroraStyle);
                if(info == null)
                    info = new RegionInfo();
                Nini.Config.IniConfigSource source = new IniConfigSource(doc);
                IConfig config = source.Configs["Region"];

                info.RegionID = UUID.Parse(config.GetString("RegionID"));
                info.RegionName = config.GetString("RegionName");
                info.RegionLocX = config.GetInt("RegionLocX");
                info.RegionLocY = config.GetInt("RegionLocY");
                info.RegionSizeX = config.GetInt("RegionSizeX");
                info.RegionSizeY = config.GetInt("RegionSizeY");

                System.Net.IPAddress intAdd = System.Net.IPAddress.Parse(config.GetString("InternalAddress", "0.0.0.0"));
                int intPort = config.GetInt("InternalPort", 9000);
                info.InternalEndPoint = new System.Net.IPEndPoint(intAdd, intPort);
                info.UDPPorts.Add(info.InternalEndPoint.Port);

                info.ObjectCapacity = config.GetInt("MaxPrims", info.ObjectCapacity);
                info.RegionType = config.GetString("RegionType", "");
                info.AccessLevel = Util.ConvertMaturityToAccessLevel(config.GetUInt("MaturityLevel", info.AccessLevel));
                info.InfiniteRegion = config.GetBoolean("InfiniteRegion", info.InfiniteRegion);
                info.Startup = config.GetString("StartupType", "Normal") == "Normal" ? StartupType.Normal : StartupType.Medium;
                info.ScopeID = UUID.Parse(config.GetString("ScopeID", UUID.Zero.ToString()));
                info.SeeIntoThisSimFromNeighbor = config.GetBoolean("SeeIntoThisSimFromNeighbor", info.SeeIntoThisSimFromNeighbor);
                ReadOpenRegionSettings(config, ref info);
                return info;
            }
            catch {  }
            return null;
        }

        private RegionInfo CreateRegionFromConsole()
        {
            if (!File.Exists(Path.Combine("Regions", "RegionConfig.ini.example")))
                File.Create(Path.Combine("Regions", "RegionConfig.ini")).Close();
            else
                File.Copy(Path.Combine("Regions", "RegionConfig.ini.example"), Path.Combine("Regions", "RegionConfig.ini"));
            Nini.Ini.IniDocument doc = new Nini.Ini.IniDocument(Path.Combine("Regions", "RegionConfig.ini"), Nini.Ini.IniFileType.AuroraStyle);
            Nini.Config.IniConfigSource source = new IniConfigSource(doc);
            IConfig section = source.Configs["Region"] != null ? source.Configs["Region"] : source.AddConfig("Region");

            RegionInfo info = new RegionInfo();
            info.RegionID = UUID.Random();
            section.Set("RegionID", info.RegionID.ToString());
            info.RegionName = MainConsole.Instance.Prompt("Region Name: ");
            section.Set("RegionName", info.RegionName);

            info.RegionLocX = int.Parse(MainConsole.Instance.Prompt("Region Location X: "));
            info.RegionLocY = int.Parse(MainConsole.Instance.Prompt("Region location Y: "));
            section.Set("RegionLocX", info.RegionLocX.ToString());
            section.Set("RegionLocY", info.RegionLocY.ToString());

            info.RegionSizeX = int.Parse(MainConsole.Instance.Prompt("Region size X: ", "256"));
            info.RegionSizeY = int.Parse(MainConsole.Instance.Prompt("Region size Y: ", "256"));
            section.Set("RegionSizeX", info.RegionSizeX.ToString());
            section.Set("RegionSizeY", info.RegionSizeY.ToString());

            System.Net.IPAddress intAdd = System.Net.IPAddress.Parse(MainConsole.Instance.Prompt("Internal IP: ", "0.0.0.0"));
            int intPort = int.Parse(MainConsole.Instance.Prompt("Internal port: ", "9000"));
            info.InternalEndPoint = new System.Net.IPEndPoint(intAdd, intPort);
            section.Set("InternalPort", intPort.ToString());
            section.Set("InternalAddress", intAdd.ToString());

            info.RegionType = MainConsole.Instance.Prompt("Region Type: ", "Mainland");
            section.Set("RegionType", info.RegionType);

            info.SeeIntoThisSimFromNeighbor = bool.Parse(MainConsole.Instance.Prompt("See into this sim from neighbors: ", "true", new List<string>() { "true", "false" }).ToLower());
            section.Set("SeeIntoThisSimFromNeighbor", info.SeeIntoThisSimFromNeighbor);

            info.ObjectCapacity = int.Parse(MainConsole.Instance.Prompt("Object capacity: ", "50000"));
            section.Set("MaxPrims", info.ObjectCapacity);

            source.Save(Path.Combine("Regions", "RegionConfig.ini"));

            return info;
        }

        private void ReadOpenRegionSettings(IConfig instanceSettings, ref RegionInfo region)
        {
            region.OpenRegionSettings.MaxDragDistance = instanceSettings.GetFloat("MaxDragDistance", region.OpenRegionSettings.MaxDragDistance);
            region.OpenRegionSettings.DefaultDrawDistance = instanceSettings.GetFloat("DefaultDrawDistance", region.OpenRegionSettings.DefaultDrawDistance);


            region.OpenRegionSettings.MaximumPrimScale = instanceSettings.GetFloat("MaximumPrimScale", region.OpenRegionSettings.MaximumPrimScale);
            region.OpenRegionSettings.MinimumPrimScale = instanceSettings.GetFloat("MinimumPrimScale", region.OpenRegionSettings.MinimumPrimScale);
            region.OpenRegionSettings.MaximumPhysPrimScale = instanceSettings.GetFloat("MaximumPhysPrimScale", region.OpenRegionSettings.MaximumPhysPrimScale);


            region.OpenRegionSettings.MaximumHollowSize = instanceSettings.GetFloat("MaximumHollowSize", region.OpenRegionSettings.MaximumHollowSize);
            region.OpenRegionSettings.MinimumHoleSize = instanceSettings.GetFloat("MinimumHoleSize", region.OpenRegionSettings.MinimumHoleSize);


            region.OpenRegionSettings.MaximumLinkCount = instanceSettings.GetInt("MaximumLinkCount", region.OpenRegionSettings.MaximumLinkCount);
            region.OpenRegionSettings.MaximumLinkCountPhys = instanceSettings.GetInt("MaximumLinkCountPhys", region.OpenRegionSettings.MaximumLinkCountPhys);


            region.OpenRegionSettings.RenderWater = instanceSettings.GetBoolean("RenderWater", region.OpenRegionSettings.RenderWater);
            region.OpenRegionSettings.MaximumInventoryItemsTransfer = instanceSettings.GetInt("MaximumInventoryItemsTransfer",
                                                                               region.OpenRegionSettings.MaximumInventoryItemsTransfer);
            region.OpenRegionSettings.DisplayMinimap = instanceSettings.GetBoolean("DisplayMinimap", region.OpenRegionSettings.DisplayMinimap);
            region.OpenRegionSettings.AllowPhysicalPrims = instanceSettings.GetBoolean("AllowPhysicalPrims", region.OpenRegionSettings.AllowPhysicalPrims);
            region.OpenRegionSettings.ForceDrawDistance = instanceSettings.GetBoolean("ForceDrawDistance", region.OpenRegionSettings.ForceDrawDistance);

            string offset = instanceSettings.GetString("OffsetOfUTC", region.OpenRegionSettings.OffsetOfUTC.ToString());
            int off;
            if (!int.TryParse(offset, out off))
            {
                if (offset == "SLT" || offset == "PST" || offset == "PDT")
                    off = -8;
                else if (offset == "UTC" || offset == "GMT")
                    off = 0;
            }
            region.OpenRegionSettings.OffsetOfUTC = off;
            region.OpenRegionSettings.OffsetOfUTCDST = instanceSettings.GetBoolean("OffsetOfUTCDST", region.OpenRegionSettings.OffsetOfUTCDST);
            region.OpenRegionSettings.EnableTeenMode = instanceSettings.GetBoolean("EnableTeenMode", region.OpenRegionSettings.EnableTeenMode);
            region.OpenRegionSettings.ShowTags = instanceSettings.GetInt("ShowTags", region.OpenRegionSettings.ShowTags);
            region.OpenRegionSettings.MaxGroups = instanceSettings.GetInt("MaxGroups", region.OpenRegionSettings.MaxGroups);

            string defaultunderpants = instanceSettings.GetString("DefaultUnderpants",
                                                                  region.OpenRegionSettings.DefaultUnderpants.ToString());
            UUID.TryParse(defaultunderpants, out region.OpenRegionSettings.m_DefaultUnderpants);
            string defaultundershirt = instanceSettings.GetString("DefaultUndershirt",
                                                                  region.OpenRegionSettings.DefaultUndershirt.ToString());
            UUID.TryParse(defaultundershirt, out region.OpenRegionSettings.m_DefaultUndershirt);
        }

        public virtual void SetRegion(IScene scene)
        {
            if (!m_loaded)
            {
                m_loaded = true;
                scene.AuroraEventManager.RegisterEventHandler("Backup", AuroraEventManager_OnGenericEvent);
                m_scene = scene;
            }
        }

        public virtual List<ISceneEntity> LoadObjects()
        {
            return _regionData.Groups.ConvertAll<ISceneEntity>(o=>o);
        }

        public virtual void LoadTerrain(bool RevertMap, int RegionSizeX, int RegionSizeY)
        {
            ITerrainModule terrainModule = m_scene.RequestModuleInterface<ITerrainModule>();
            if (RevertMap)
            {
                terrainModule.TerrainRevertMap = ReadFromData(_regionData.RevertTerrain, m_scene);
                //Make sure the size is right!
                if (terrainModule.TerrainRevertMap != null &&
                    terrainModule.TerrainRevertMap.Height != m_scene.RegionInfo.RegionSizeX)
                    terrainModule.TerrainRevertMap = null;
            }
            else
            {
                terrainModule.TerrainMap = ReadFromData(_regionData.Terrain, m_scene);
                //Make sure the size is right!
                if (terrainModule.TerrainMap != null &&
                    terrainModule.TerrainMap.Height != m_scene.RegionInfo.RegionSizeX)
                    terrainModule.TerrainMap = null;
            }
        }

        public virtual void LoadWater(bool RevertMap, int RegionSizeX, int RegionSizeY)
        {
            ITerrainModule terrainModule = m_scene.RequestModuleInterface<ITerrainModule>();
            if (RevertMap)
            {
                terrainModule.TerrainWaterRevertMap = ReadFromData(_regionData.RevertWater, m_scene);
                //Make sure the size is right!
                if (terrainModule.TerrainWaterRevertMap.Height != m_scene.RegionInfo.RegionSizeX)
                    terrainModule.TerrainWaterRevertMap = null;
            }
            else
            {
                terrainModule.TerrainWaterMap = ReadFromData(_regionData.Water, m_scene);
                //Make sure the size is right!
                if (terrainModule.TerrainWaterMap.Height != m_scene.RegionInfo.RegionSizeX)
                    terrainModule.TerrainWaterMap = null;
            }
        }

        public virtual void Shutdown()
        {
            //The sim is shutting down, we need to save one last backup
            try
            {
                lock (m_saveLock)
                {
                    m_shutdown = true;
                    if (!m_saveChanges || !m_saveBackups)
                        return;
                    SaveBackup(false);
                }
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Error("[FileBasedSimulationData]: Failed to save backup, exception occured " + ex);
            }
        }

        public virtual void Tainted()
        {
            m_requiresSave = true;
        }

        public virtual void ForceBackup()
        {
            if (m_saveTimer != null)
                m_saveTimer.Stop();
            try
            {
                lock (m_saveLock)
                {
                    if (!m_shutdown)
                        SaveBackup(false);
                    m_requiresSave = false;
                }
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Error("[FileBasedSimulationData]: Failed to save backup, exception occured " + ex);
            }
            if (m_saveTimer != null)
                m_saveTimer.Start(); //Restart it as we just did a backup
        }

        public virtual void RemoveRegion()
        {
            //Remove the file so that the region is gone
            File.Delete(BuildSaveFileName());
        }

        /// <summary>
        ///   Around for legacy things
        /// </summary>
        /// <param name = "regionUUID"></param>
        /// <returns></returns>
        public virtual List<LandData> LoadLandObjects()
        {
            return _regionData.Parcels;
        }

        #endregion

        /// <summary>
        ///   Read the config for the data loader
        /// </summary>
        /// <param name = "scene"></param>
        /// <param name = "config"></param>
        protected virtual void ReadConfig(ISimulationBase simBase)
        {
            IConfig config = simBase.ConfigSource.Configs["FileBasedSimulationData"];
            if (config != null)
            {
                m_saveChanges = config.GetBoolean("SaveChanges", m_saveChanges);
                m_timeBetweenSaves = config.GetInt("TimeBetweenSaves", m_timeBetweenSaves);
                m_keepOldSave = config.GetBoolean("SavePreviousBackup", m_keepOldSave);
                m_oldSaveDirectory = PathHelpers.ComputeFullPath(config.GetString("PreviousBackupDirectory", m_oldSaveDirectory));
                m_storeDirectory = PathHelpers.ComputeFullPath(config.GetString("StoreBackupDirectory", m_storeDirectory));
                m_saveBackupChanges = config.GetBoolean("SaveTimedPreviousBackup", m_keepOldSave);
                m_timeBetweenBackupSaves = config.GetInt("TimeBetweenBackupSaves", m_timeBetweenBackupSaves);
            }

            if (m_saveChanges && m_timeBetweenSaves != 0)
            {
                m_saveTimer = new Timer(m_timeBetweenSaves*60*1000);
                m_saveTimer.Elapsed += m_saveTimer_Elapsed;
                m_saveTimer.Start();
            }

            if (m_saveChanges && m_timeBetweenBackupSaves != 0)
            {
                m_backupSaveTimer = new Timer(m_timeBetweenBackupSaves*60*1000);
                m_backupSaveTimer.Elapsed += m_backupSaveTimer_Elapsed;
                m_backupSaveTimer.Start();
            }

            m_fileName = "sim";
        }

        /// <summary>
        ///   Look for the backup event, and if it is there, trigger the backup of the sim
        /// </summary>
        /// <param name = "FunctionName"></param>
        /// <param name = "parameters"></param>
        /// <returns></returns>
        private object AuroraEventManager_OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "Backup")
            {
                ForceBackup();
            }
            return null;
        }

        /// <summary>
        ///   Save a backup on the timer event
        /// </summary>
        /// <param name = "sender"></param>
        /// <param name = "e"></param>
        private void m_saveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (m_requiresSave)
            {
                m_displayNotSavingNotice = true;
                m_requiresSave = false;
                m_saveTimer.Stop();
                try
                {
                    lock(m_saveLock)
                    {
                        if (m_saveChanges && m_saveBackups && !m_shutdown)
                        {
                            SaveBackup(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.Error("[FileBasedSimulationData]: Failed to save backup, exception occured " + ex);
                }
                m_saveTimer.Start(); //Restart it as we just did a backup
            }
            else if (m_displayNotSavingNotice)
            {
                m_displayNotSavingNotice = false;
                MainConsole.Instance.Info("[FileBasedSimulationData]: Not saving backup, not required");
            }
        }

        /// <summary>
        ///   Save a backup into the oldSaveDirectory on the timer event
        /// </summary>
        /// <param name = "sender"></param>
        /// <param name = "e"></param>
        private void m_backupSaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock(m_saveLock)
                {
                    if(!m_shutdown)
                    {
                        SaveBackup(true);
                    }
                }
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Error("[FileBasedSimulationData]: Failed to save backup, exception occured " + ex);
            }
        }

        /// <summary>
        ///   Save a backup of the sim
        /// </summary>
        /// <param name = "appendedFilePath">The file path where the backup will be saved</param>
        protected virtual void SaveBackup(bool isOldSave)
        {
            if (m_scene.RegionInfo.HasBeenDeleted)
                return;
            IBackupModule backupModule = m_scene.RequestModuleInterface<IBackupModule>();
            if (backupModule != null && backupModule.LoadingPrims) //Something is changing lots of prims
            {
                MainConsole.Instance.Info("[Backup]: Not saving backup because the backup module is loading prims");
                return;
            }

            //Save any script state saves that might be around
            IScriptModule[] engines = m_scene.RequestModuleInterfaces<IScriptModule>();
            try
            {
                if (engines != null)
                {
                    foreach (IScriptModule engine in engines.Where(engine => engine != null))
                    {
                        engine.SaveStateSaves();
                    }
                }
            }
            catch (Exception ex)
            {
                MainConsole.Instance.WarnFormat("[Backup]: Exception caught: {0}", ex);
            }

            MainConsole.Instance.Info("[FileBasedSimulationData]: Saving backup for region " + m_scene.RegionInfo.RegionName);
            
            RegionData regiondata = new RegionData();
            regiondata.Init();

            regiondata.RegionInfo = m_scene.RegionInfo;
            IParcelManagementModule module = m_scene.RequestModuleInterface<IParcelManagementModule>();
            if (module != null)
            {
                List<ILandObject> landObject = module.AllParcels();
                foreach (ILandObject parcel in landObject)
                    regiondata.Parcels.Add(parcel.LandData);
            }

            ITerrainModule tModule = m_scene.RequestModuleInterface<ITerrainModule>();
            if (tModule != null)
            {
                try
                {
                    regiondata.Terrain = WriteTerrainToStream(tModule.TerrainMap);
                    regiondata.RevertTerrain = WriteTerrainToStream(tModule.TerrainRevertMap);

                    if (tModule.TerrainWaterMap != null)
                    {
                        regiondata.Water = WriteTerrainToStream(tModule.TerrainWaterMap);
                        regiondata.RevertWater = WriteTerrainToStream(tModule.TerrainWaterRevertMap);
                    }
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.WarnFormat("[Backup]: Exception caught: {0}", ex);
                }
            }

            ISceneEntity[] entities = m_scene.Entities.GetEntities();
            regiondata.Groups = new List<SceneObjectGroup>(entities.Cast<SceneObjectGroup>().Where((entity)=>
                {
                    return entity.IsAttachment ||
                            ((entity.RootChild.Flags & PrimFlags.Temporary) == PrimFlags.Temporary)
                            || ((entity.RootChild.Flags & PrimFlags.TemporaryOnRez) == PrimFlags.TemporaryOnRez);
                }));
            try
            {
                foreach (ISceneEntity entity in regiondata.Groups.Where(ent => ent.HasGroupChanged))
                    entity.HasGroupChanged = false;
            }
            catch (Exception ex)
            {
                MainConsole.Instance.WarnFormat("[Backup]: Exception caught: {0}", ex);
            }
            string filename = isOldSave ? BuildOldSaveFileName() : BuildSaveFileName();

            if (File.Exists(filename + (isOldSave ? "" : ".tmp")))
                File.Delete(filename + (isOldSave ? "" : ".tmp"));//Remove old tmp files
            _regionLoader.SaveBackup(filename + (isOldSave ? "" : ".tmp"), regiondata);

            //RegionData data = _regionLoader.LoadBackup(filename + ".tmp");
            if(!isOldSave)
            {
                if (File.Exists(filename))
                    File.Delete(filename);
                File.Move(filename + ".tmp", filename);

                if (m_keepOldSave && !m_oldSaveHasBeenSaved)
                {
                    //Havn't moved it yet, so make sure the directory exists, then move it
                    m_oldSaveHasBeenSaved = true;
                    if (!Directory.Exists(m_oldSaveDirectory))
                        Directory.CreateDirectory(m_oldSaveDirectory);
                    File.Copy(filename, BuildOldSaveFileName());
                }
            }
            regiondata.Dispose();
            //Now make it the full file again
            MapTileNeedsGenerated = true;
            MainConsole.Instance.Info("[FileBasedSimulationData]: Saved Backup for region " + m_scene.RegionInfo.RegionName);
        }

        private string BuildOldSaveFileName()
        {
            return Path.Combine(m_oldSaveDirectory,
                                           m_scene.RegionInfo.RegionName + SerializeDateTime() +
                                           _regionLoader.FileType);
        }

        private string BuildSaveFileName()
        {
            return (m_storeDirectory == "" || m_storeDirectory == "/")
                                                      ? m_fileName + _regionLoader.FileType
                                                      : Path.Combine(m_storeDirectory, m_fileName + _regionLoader.FileType);
        }

        private byte[] WriteTerrainToStream(ITerrainChannel tModule)
        {
            int tMapSize = tModule.Height*tModule.Height;
            byte[] sdata = new byte[tMapSize*2];
            Buffer.BlockCopy(tModule.GetSerialised(tModule.Scene), 0, sdata, 0, sdata.Length);
            return sdata;
        }

        protected virtual string SerializeDateTime()
        {
            return String.Format("--{0:yyyy-MM-dd-HH-mm}", DateTime.Now);
            //return "--" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Hour + "-" + DateTime.Now.Minute;
        }

        protected virtual void ReadBackup()
        {
            MainConsole.Instance.Info("[FileBasedSimulationData]: Restoring sim backup...");
            _regionData = _regionLoader.LoadBackup(BuildSaveFileName());
            if (_regionData == null)
                _regionData = _oldRegionLoader.LoadBackup(Path.ChangeExtension(BuildSaveFileName(), _oldRegionLoader.FileType));
            if (_regionData == null)
            {
                _regionData = new RegionData();
                _regionData.Init();
            }
            GC.Collect();
        }

        private ITerrainChannel ReadFromData(byte[] data, IScene scene)
        {
            if (data == null) return null;
            short[] sdata = new short[data.Length/2];
            Buffer.BlockCopy(data, 0, sdata, 0, data.Length);
            return new TerrainChannel(sdata, scene);
        }
    }

    public interface IRegionDataLoader
    {
        string FileType { get; }

        RegionData LoadBackup(string file);

        bool SaveBackup(string m_fileName, RegionData regiondata);
    }

    [Serializable, ProtoBuf.ProtoContract()]
    public class RegionData
    {
        [ProtoMember(1)]
        public List<SceneObjectGroup> Groups;
        [ProtoMember(2)]
        public RegionInfo RegionInfo;
        [ProtoMember(3)]
        public byte[] Terrain;
        [ProtoMember(4)]
        public byte[] RevertTerrain;
        [ProtoMember(5)]
        public byte[] Water;
        [ProtoMember(6)]
        public byte[] RevertWater;
        [ProtoMember(7)]
        public List<LandData> Parcels;

        public void Init()
        {
            Groups = new List<SceneObjectGroup>();
            Parcels = new List<LandData>();
        }

        public void Dispose()
        {
            Groups = null;
            Parcels = null;
            Water = null;
            RevertWater = null;
            Terrain = null;
            RevertTerrain = null;
            RegionInfo = null;
        }
    }
}