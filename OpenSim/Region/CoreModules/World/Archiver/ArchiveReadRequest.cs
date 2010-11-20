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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Serialization;
using OpenSim.Framework.Serialization.External;
using OpenSim.Region.CoreModules.World.Terrain;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

namespace OpenSim.Region.CoreModules.World.Archiver
{
    /// <summary>
    /// Handles an individual archive read request
    /// </summary>
    public class ArchiveReadRequest
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected static ASCIIEncoding m_asciiEncoding = new ASCIIEncoding();
        protected static UTF8Encoding m_utf8Encoding = new UTF8Encoding();

        protected Scene m_scene;
        protected Stream m_loadStream;
        protected Guid m_requestId;
        protected string m_errorMessage;
        protected HashSet<AssetBase> AssetsToAdd = new HashSet<AssetBase>();
        protected bool AssetSaverIsRunning = false;
        protected bool m_useAsync = false;

        /// <value>
        /// Should the archive being loaded be merged with what is already on the region?
        /// </value>
        protected bool m_merge;

        protected Aurora.Framework.AuroraThreadPool m_threadpool;

        /// <value>
        /// Should we ignore any assets when reloading the archive?
        /// </value>
        protected bool m_skipAssets;

        /// <summary>
        /// Used to cache lookups for valid uuids.
        /// </summary>
        private IDictionary<UUID, bool> m_validUserUuids = new Dictionary<UUID, bool>();

        public ArchiveReadRequest(Scene scene, string loadPath, bool merge, bool skipAssets, Guid requestId)
        {
            m_scene = scene;

            try
            {
                m_loadStream = new GZipStream(ArchiveHelpers.GetStream(loadPath), CompressionMode.Decompress);
            }
            catch (EntryPointNotFoundException e)
            {
                m_log.ErrorFormat(
                    "[ARCHIVER]: Mismatch between Mono and zlib1g library version when trying to create compression stream."
                        + "If you've manually installed Mono, have you appropriately updated zlib1g as well?");
                m_log.Error(e);
            }

            m_errorMessage = String.Empty;
            m_merge = merge;
            m_skipAssets = skipAssets;
            m_requestId = requestId;
        }

        public ArchiveReadRequest(Scene scene, Stream loadStream, bool merge, bool skipAssets, Guid requestId)
        {
            m_scene = scene;
            m_loadStream = loadStream;
            m_merge = merge;
            m_skipAssets = skipAssets;
            m_requestId = requestId;
        }

        /// <summary>
        /// Dearchive the region embodied in this request.
        /// </summary>
        public void DearchiveRegion()
        {
            // The same code can handle dearchiving 0.1 and 0.2 OpenSim Archive versions
            DearchiveRegion0DotStar();
        }

        private void DearchiveRegion0DotStar()
        {
            int successfulAssetRestores = 0;
            int failedAssetRestores = 0;
            //List<string> serialisedSceneObjects = new List<string>();
            List<string> serialisedParcels = new List<string>();
            string filePath = "NONE";
            DateTime start = DateTime.Now;

            TarArchiveReader archive = new TarArchiveReader(m_loadStream);
            byte[] data;
            TarArchiveReader.TarEntryType entryType;

            if (!m_skipAssets)
                m_threadpool = new Aurora.Framework.AuroraThreadPool(new Aurora.Framework.AuroraThreadPoolStartInfo()
                    {
                        Threads = 1,
                        priority = System.Threading.ThreadPriority.BelowNormal
                    });

            if (!m_merge)
            {
                DateTime before = DateTime.Now;
                m_log.Info("[ARCHIVER]: Clearing all existing scene objects");
                m_scene.DeleteAllSceneObjects();
                m_log.Info("[ARCHIVER]: Cleared all existing scene objects in " + (DateTime.Now - before).Minutes + ":" + (DateTime.Now - before).Seconds);
            }

            IScriptModule[] modules = m_scene.RequestModuleInterfaces<IScriptModule>();
            //Disable the script engine so that it doesn't load in the background and kill OAR loading
            foreach (IScriptModule module in modules)
            {
                module.Disabled = true;
            }
            //Disable backup for now as well
            m_scene.LoadingPrims = true;

            IRegionSerialiserModule serialiser = m_scene.RequestModuleInterface<IRegionSerialiserModule>();
            int sceneObjectsLoadedCount = 0;

            //We save the groups so that we can back them up later
            List<SceneObjectGroup> groupsToBackup = new List<SceneObjectGroup>();

            try
            {
                while ((data = archive.ReadEntry(out filePath, out entryType)) != null)
                {
                    //m_log.DebugFormat(
                    //    "[ARCHIVER]: Successfully read {0} ({1} bytes)", filePath, data.Length);

                    if (TarArchiveReader.TarEntryType.TYPE_DIRECTORY == entryType)
                        continue;

                    if (filePath.StartsWith(ArchiveConstants.OBJECTS_PATH))
                    {
                        string sogdata = m_utf8Encoding.GetString(data);
                        //serialisedSceneObjects.Add(m_utf8Encoding.GetString(data));
                        /*
                m_log.DebugFormat("[ARCHIVER]: Loading xml with raw size {0}", serialisedSceneObject.Length);

                // Really large xml files (multi megabyte) appear to cause
                // memory problems
                // when loading the xml.  But don't enable this check yet
                
                if (serialisedSceneObject.Length > 5000000)
                {
                    m_log.Error("[ARCHIVER]: Ignoring xml since size > 5000000);");
                    continue;
                }
                */

                        string serialisedSceneObject = sogdata;
                        SceneObjectGroup sceneObject = serialiser.DeserializeGroupFromXml2(serialisedSceneObject, m_scene);

                        if (sceneObject == null)
                        {
                            //! big error!
                            m_log.Error("Error reading SOP XML (Please mantis this!): " + serialisedSceneObject);
                            continue;
                        }
                        
                        foreach (SceneObjectPart part in sceneObject.ChildrenList)
                        {
                            if (!ResolveUserUuid(part.CreatorID))
                                part.CreatorID = m_scene.RegionInfo.EstateSettings.EstateOwner;

                            if (!ResolveUserUuid(part.OwnerID))
                                part.OwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;

                            if (!ResolveUserUuid(part.LastOwnerID))
                                part.LastOwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;

                            // And zap any troublesome sit target information
                            part.SitTargetOrientation = new Quaternion(0, 0, 0, 1);
                            part.SitTargetPosition = new Vector3(0, 0, 0);

                            // Fix ownership/creator of inventory items
                            // Not doing so results in inventory items
                            // being no copy/no mod for everyone
                            lock (part.TaskInventory)
                            {
                                TaskInventoryDictionary inv = part.TaskInventory;
                                foreach (KeyValuePair<UUID, TaskInventoryItem> kvp in inv)
                                {
                                    if (!ResolveUserUuid(kvp.Value.OwnerID))
                                    {
                                        kvp.Value.OwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;
                                    }
                                    if (!ResolveUserUuid(kvp.Value.CreatorID))
                                    {
                                        kvp.Value.CreatorID = m_scene.RegionInfo.EstateSettings.EstateOwner;
                                    }
                                }
                            }
                        }

                        if (m_scene.AddPrimToScene(sceneObject))
                        {
                            groupsToBackup.Add(sceneObject);
                            sceneObject.ScheduleGroupForFullUpdate(PrimUpdateFlags.FullUpdate);
                            sceneObjectsLoadedCount++;
                            sceneObject.CreateScriptInstances(0, false, m_scene.DefaultScriptEngine, 0, UUID.Zero);
                            sceneObject.ResumeScripts();
                        }
                        sceneObjectsLoadedCount++;
                        if (sceneObjectsLoadedCount % 250 == 0)
                            m_log.Debug("[ARCHIVER]: Loaded " + sceneObjectsLoadedCount + " objects...");
                    }
                    else if (filePath.StartsWith(ArchiveConstants.ASSETS_PATH))
                    {
                        if (!m_skipAssets)
                        {
                            if (LoadAsset(filePath, data))
                                successfulAssetRestores++;
                            else
                                failedAssetRestores++;

                            if ((successfulAssetRestores + failedAssetRestores) % 250 == 0)
                                m_log.Debug("[ARCHIVER]: Loaded " + successfulAssetRestores + " assets and failed to load " + failedAssetRestores + " assets...");
                        }
                    }
                    else if (!m_merge && filePath.StartsWith(ArchiveConstants.TERRAINS_PATH))
                    {
                        LoadTerrain(filePath, data);
                    }
                    else if (!m_merge && filePath.StartsWith(ArchiveConstants.SETTINGS_PATH))
                    {
                        LoadRegionSettings(filePath, data);
                    }
                    else if (!m_merge && filePath.StartsWith(ArchiveConstants.LANDDATA_PATH))
                    {
                        serialisedParcels.Add(m_utf8Encoding.GetString(data));
                    }
                    else if (filePath == ArchiveConstants.CONTROL_FILE_PATH)
                    {
                        LoadControlFile(filePath, data);
                    }
                    else
                    {
                        m_log.Debug("[ARCHIVER]:UNKNOWN PATH: " + filePath);
                    }
                }

                //m_log.Debug("[ARCHIVER]: Reached end of archive");
            }
            catch (Exception e)
            {
                m_log.ErrorFormat(
                    "[ARCHIVER]: Aborting load with error in archive file {0}.  {1}", filePath, e);
                m_errorMessage += e.ToString();
                m_scene.EventManager.TriggerOarFileLoaded(m_requestId, m_errorMessage);
                return;
            }
            finally
            {
                archive.Close();
                m_loadStream.Close();
                m_loadStream.Dispose();
            }
            //Reeanble now that we are done
            foreach (IScriptModule module in modules)
            {
                module.Disabled = false;
            }
            m_scene.LoadingPrims = false;

            //Now back up the prims
            foreach (SceneObjectGroup grp in groupsToBackup)
            {
                //Backup!
                grp.HasGroupChanged = true;
            }

            if (!m_skipAssets)
            {
                if (m_useAsync && !AssetSaverIsRunning)
                    m_threadpool.QueueEvent(SaveAssets, 0);
                else if (!AssetSaverIsRunning)
                    SaveAssets();
            }

            if (!m_skipAssets)
            {
                m_log.InfoFormat("[ARCHIVER]: Restored {0} assets", successfulAssetRestores);

                if (failedAssetRestores > 0)
                {
                    m_log.ErrorFormat("[ARCHIVER]: Failed to load {0} assets", failedAssetRestores);
                    m_errorMessage += String.Format("Failed to load {0} assets", failedAssetRestores);
                }
            }

            // Try to retain the original creator/owner/lastowner if their uuid is present on this grid
            // otherwise, use the master avatar uuid instead

            // Reload serialized parcels
            m_log.InfoFormat("[ARCHIVER]: Loading {0} parcels.  Please wait.", serialisedParcels.Count);
            List<LandData> landData = new List<LandData>();
            foreach (string serialisedParcel in serialisedParcels)
            {
                LandData parcel = LandDataSerializer.Deserialize(serialisedParcel);
                if (!ResolveUserUuid(parcel.OwnerID))
                    parcel.OwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;
                landData.Add(parcel);
            }
            m_scene.EventManager.TriggerIncomingLandDataFromStorage(landData);
            m_log.InfoFormat("[ARCHIVER]: Restored {0} parcels.", landData.Count);

            //Clean it out
            landData.Clear();
            serialisedParcels.Clear();

            // Reload serialized prims
            //m_log.InfoFormat("[ARCHIVER]: Loading {0} scene objects.  Please wait.", serialisedSceneObjects.Count);

            //m_log.InfoFormat("[ARCHIVER]: Restored {0} scene objects to the scene", sceneObjectsLoadedCount);

            //int ignoredObjects = serialisedSceneObjects.Count - sceneObjectsLoadedCount;

            //if (ignoredObjects > 0)
            //    m_log.WarnFormat("[ARCHIVER]: Ignored {0} scene objects that already existed in the scene", ignoredObjects);

            m_log.InfoFormat("[ARCHIVER]: Successfully loaded archive in " + (DateTime.Now - start).Minutes + ":" + (DateTime.Now - start).Seconds);

            m_validUserUuids.Clear();
            m_scene.EventManager.TriggerOarFileLoaded(m_requestId, m_errorMessage);
        }

        /// <summary>
        /// Look up the given user id to check whether it's one that is valid for this grid.
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        private bool ResolveUserUuid(UUID uuid)
        {
            bool v;
            if (!m_validUserUuids.TryGetValue(uuid, out v))
            {
                UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.ScopeID, uuid);
                if (account != null)
                {
                    m_validUserUuids.Add(uuid, true);
                    return true;
                }
                else
                {
                    m_validUserUuids.Add(uuid, false);
                    return false;
                }
            }

            return v;
        }

        /// <summary>
        /// Load an asset
        /// </summary>
        /// <param name="assetFilename"></param>
        /// <param name="data"></param>
        /// <returns>true if asset was successfully loaded, false otherwise</returns>
        private bool LoadAsset(string assetPath, byte[] data)
        {
            // Right now we're nastily obtaining the UUID from the filename
            string filename = assetPath.Remove(0, ArchiveConstants.ASSETS_PATH.Length);
            int i = filename.LastIndexOf(ArchiveConstants.ASSET_EXTENSION_SEPARATOR);

            if (i == -1)
            {
                m_log.ErrorFormat(
                    "[ARCHIVER]: Could not find extension information in asset path {0} since it's missing the separator {1}.  Skipping",
                    assetPath, ArchiveConstants.ASSET_EXTENSION_SEPARATOR);

                return false;
            }

            string extension = filename.Substring(i);
            string uuid = filename.Remove(filename.Length - extension.Length);

            if (ArchiveConstants.EXTENSION_TO_ASSET_TYPE.ContainsKey(extension))
            {
                sbyte assetType = ArchiveConstants.EXTENSION_TO_ASSET_TYPE[extension];

                if (assetType == (sbyte)AssetType.Unknown)
                    m_log.WarnFormat("[ARCHIVER]: Importing {0} byte asset {1} with unknown type", data.Length, uuid);

                //m_log.DebugFormat("[ARCHIVER]: Importing asset {0}, type {1}", uuid, assetType);
                AssetBase asset = new AssetBase(new UUID(uuid), String.Empty, assetType, UUID.Zero.ToString());
                asset.Data = data;

                // We're relying on the asset service to do the sensible thing and not store the asset if it already
                // exists.
                if (m_useAsync)
                {
                    lock (AssetsToAdd)
                        AssetsToAdd.Add(asset);
                }
                else
                    m_scene.AssetService.Store(asset);

                /**
                 * Create layers on decode for image assets.  This is likely to significantly increase the time to load archives so
                 * it might be best done when dearchive takes place on a separate thread
                if (asset.Type=AssetType.Texture)
                {
                    IJ2KDecoder cacheLayerDecode = scene.RequestModuleInterface<IJ2KDecoder>();
                    if (cacheLayerDecode != null)
                        cacheLayerDecode.syncdecode(asset.FullID, asset.Data);
                }
                */

                return true;
            }
            else
            {
                m_log.ErrorFormat(
                    "[ARCHIVER]: Tried to dearchive data with path {0} with an unknown type extension {1}",
                    assetPath, extension);

                return false;
            }
        }

        private bool SaveAssets()
        {
            AssetSaverIsRunning = true;
            lock (AssetsToAdd)
            {
                foreach (AssetBase asset in AssetsToAdd)
                {
                    m_scene.AssetService.Store(asset);
                }
            }
            AssetsToAdd.Clear();
            AssetSaverIsRunning = false;
            return false;
        }

        /// <summary>
        /// Load region settings data
        /// </summary>
        /// <param name="settingsPath"></param>
        /// <param name="data"></param>
        /// <returns>
        /// true if settings were loaded successfully, false otherwise
        /// </returns>
        private bool LoadRegionSettings(string settingsPath, byte[] data)
        {
            RegionSettings loadedRegionSettings;

            try
            {
                loadedRegionSettings = RegionSettingsSerializer.Deserialize(data);
            }
            catch (Exception e)
            {
                m_log.ErrorFormat(
                    "[ARCHIVER]: Could not parse region settings file {0}.  Ignoring.  Exception was {1}",
                    settingsPath, e);
                return false;
            }

            RegionSettings currentRegionSettings = m_scene.RegionInfo.RegionSettings;

            currentRegionSettings.AgentLimit = loadedRegionSettings.AgentLimit;
            currentRegionSettings.AllowDamage = loadedRegionSettings.AllowDamage;
            currentRegionSettings.AllowLandJoinDivide = loadedRegionSettings.AllowLandJoinDivide;
            currentRegionSettings.AllowLandResell = loadedRegionSettings.AllowLandResell;
            currentRegionSettings.BlockFly = loadedRegionSettings.BlockFly;
            currentRegionSettings.BlockShowInSearch = loadedRegionSettings.BlockShowInSearch;
            currentRegionSettings.BlockTerraform = loadedRegionSettings.BlockTerraform;
            currentRegionSettings.DisableCollisions = loadedRegionSettings.DisableCollisions;
            currentRegionSettings.DisablePhysics = loadedRegionSettings.DisablePhysics;
            currentRegionSettings.DisableScripts = loadedRegionSettings.DisableScripts;
            currentRegionSettings.Elevation1NE = loadedRegionSettings.Elevation1NE;
            currentRegionSettings.Elevation1NW = loadedRegionSettings.Elevation1NW;
            currentRegionSettings.Elevation1SE = loadedRegionSettings.Elevation1SE;
            currentRegionSettings.Elevation1SW = loadedRegionSettings.Elevation1SW;
            currentRegionSettings.Elevation2NE = loadedRegionSettings.Elevation2NE;
            currentRegionSettings.Elevation2NW = loadedRegionSettings.Elevation2NW;
            currentRegionSettings.Elevation2SE = loadedRegionSettings.Elevation2SE;
            currentRegionSettings.Elevation2SW = loadedRegionSettings.Elevation2SW;
            currentRegionSettings.FixedSun = loadedRegionSettings.FixedSun;
            currentRegionSettings.ObjectBonus = loadedRegionSettings.ObjectBonus;
            currentRegionSettings.RestrictPushing = loadedRegionSettings.RestrictPushing;
            currentRegionSettings.TerrainLowerLimit = loadedRegionSettings.TerrainLowerLimit;
            currentRegionSettings.TerrainRaiseLimit = loadedRegionSettings.TerrainRaiseLimit;
            currentRegionSettings.TerrainTexture1 = loadedRegionSettings.TerrainTexture1;
            currentRegionSettings.TerrainTexture2 = loadedRegionSettings.TerrainTexture2;
            currentRegionSettings.TerrainTexture3 = loadedRegionSettings.TerrainTexture3;
            currentRegionSettings.TerrainTexture4 = loadedRegionSettings.TerrainTexture4;
            currentRegionSettings.UseEstateSun = loadedRegionSettings.UseEstateSun;
            currentRegionSettings.WaterHeight = loadedRegionSettings.WaterHeight;

            currentRegionSettings.Save();

            IEstateModule estateModule = m_scene.RequestModuleInterface<IEstateModule>();

            if (estateModule != null)
                estateModule.sendRegionHandshakeToAll();

            return true;
        }

        /// <summary>
        /// Load terrain data
        /// </summary>
        /// <param name="terrainPath"></param>
        /// <param name="data"></param>
        /// <returns>
        /// true if terrain was resolved successfully, false otherwise.
        /// </returns>
        private bool LoadTerrain(string terrainPath, byte[] data)
        {
            ITerrainModule terrainModule = m_scene.RequestModuleInterface<ITerrainModule>();

            MemoryStream ms = new MemoryStream(data);
            terrainModule.LoadFromStream(terrainPath, ms);
            ms.Close();

            m_log.DebugFormat("[ARCHIVER]: Restored terrain {0}", terrainPath);

            return true;
        }

        /// <summary>
        /// Load oar control file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        private void LoadControlFile(string path, byte[] data)
        {
            //Create the XmlNamespaceManager.
            NameTable nt = new NameTable();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);

            // Create the XmlParserContext.
            XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);

            XmlTextReader xtr
                = new XmlTextReader(m_asciiEncoding.GetString(data), XmlNodeType.Document, context);

            RegionSettings currentRegionSettings = m_scene.RegionInfo.RegionSettings;

            // Loaded metadata will empty if no information exists in the archive
            currentRegionSettings.LoadedCreationDateTime = 0;
            currentRegionSettings.LoadedCreationID = "";

            while (xtr.Read())
            {
                if (xtr.NodeType == XmlNodeType.Element)
                {
                    if (xtr.Name.ToString() == "datetime")
                    {
                        int value;
                        if (Int32.TryParse(xtr.ReadElementContentAsString(), out value))
                            currentRegionSettings.LoadedCreationDateTime = value;
                    }
                    else if (xtr.Name.ToString() == "id")
                    {
                        currentRegionSettings.LoadedCreationID = xtr.ReadElementContentAsString();
                    }
                }

            }

            currentRegionSettings.Save();
        }
    }
}
