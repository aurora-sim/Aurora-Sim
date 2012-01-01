/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Serialization;
using Aurora.Framework.Serialization.External;
using Aurora.Modules.Terrain;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Archivers
{
    /// <summary>
    /// Handles an individual archive read request
    /// </summary>
    public class ArchiveReadRequest
    {
        protected static ASCIIEncoding m_asciiEncoding = new ASCIIEncoding();
        protected static UTF8Encoding m_utf8Encoding = new UTF8Encoding();

        protected IScene m_scene;
        protected Stream m_loadStream;
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
        private IDictionary<UUID, UUID> m_validUserUuids = new Dictionary<UUID, UUID>();

        private int m_offsetX = 0;
        private int m_offsetY = 0;
        private int m_offsetZ = 0;
        private bool m_flipX = false;
        private bool m_flipY = false;
        private bool m_useParcelOwnership = false;
        private bool m_checkOwnership = false;

        public ArchiveReadRequest (IScene scene, string loadPath, bool merge, bool skipAssets,
            int offsetX, int offsetY, int offsetZ, bool flipX, bool flipY, bool useParcelOwnership, bool checkOwnership)
        {
            try
            {
                m_loadStream = new GZipStream(ArchiveHelpers.GetStream(loadPath), CompressionMode.Decompress);
            }
            catch (EntryPointNotFoundException e)
            {
                MainConsole.Instance.ErrorFormat(
                    "[ARCHIVER]: Mismatch between Mono and zlib1g library version when trying to create compression stream."
                        + "If you've manually installed Mono, have you appropriately updated zlib1g as well?");
                MainConsole.Instance.Error(e);
            }
            Init(scene, m_loadStream, merge, skipAssets, offsetX, offsetY, offsetZ, flipX, flipY, useParcelOwnership, checkOwnership);
        }

        public void Init(IScene scene, Stream stream, bool merge, bool skipAssets,
            int offsetX, int offsetY, int offsetZ, bool flipX, bool flipY, bool useParcelOwnership, bool checkOwnership)
        {
            m_loadStream = stream;
            m_offsetX = offsetX;
            m_offsetY = offsetY;
            m_offsetZ = offsetZ;
            m_flipX = flipX;
            m_flipY = flipY;
            m_useParcelOwnership = useParcelOwnership;
            m_checkOwnership = checkOwnership;
            m_scene = scene;
            m_errorMessage = String.Empty;
            m_merge = merge;
            m_skipAssets = skipAssets;
        }

        public ArchiveReadRequest(IScene scene, Stream stream, bool merge, bool skipAssets,
            int offsetX, int offsetY, int offsetZ, bool flipX, bool flipY, bool useParcelOwnership, bool checkOwnership)
        {
            Init(scene, stream, merge, skipAssets, offsetX, offsetY, offsetZ, flipX, flipY, useParcelOwnership, checkOwnership);
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

            IBackupModule backup = m_scene.RequestModuleInterface<IBackupModule>();
            if (!m_merge)
            {
                DateTime before = DateTime.Now;
                MainConsole.Instance.Info("[ARCHIVER]: Clearing all existing scene objects");
                if (backup != null)
                    backup.DeleteAllSceneObjects();
                MainConsole.Instance.Info("[ARCHIVER]: Cleared all existing scene objects in " + (DateTime.Now - before).Minutes + ":" + (DateTime.Now - before).Seconds);
            }

            IScriptModule[] modules = m_scene.RequestModuleInterfaces<IScriptModule>();
            //Disable the script engine so that it doesn't load in the background and kill OAR loading
            foreach (IScriptModule module in modules)
            {
                module.Disabled = true;
            }
            //Disable backup for now as well
            if (backup != null)
                backup.LoadingPrims = true;

            IRegionSerialiserModule serialiser = m_scene.RequestModuleInterface<IRegionSerialiserModule>();
            int sceneObjectsLoadedCount = 0;

            //We save the groups so that we can back them up later
            List<SceneObjectGroup> groupsToBackup = new List<SceneObjectGroup>();
            List<LandData> landData = new List<LandData>();
            IUserManagement UserManager = m_scene.RequestModuleInterface<IUserManagement>();
            try
            {
                while ((data = archive.ReadEntry(out filePath, out entryType)) != null)
                {
                    //MainConsole.Instance.DebugFormat(
                    //    "[ARCHIVER]: Successfully read {0} ({1} bytes)", filePath, data.Length);

                    if (TarArchiveReader.TarEntryType.TYPE_DIRECTORY == entryType)
                        continue;

                    if (filePath.StartsWith(ArchiveConstants.OBJECTS_PATH))
                    {
                        /*
                        MainConsole.Instance.DebugFormat("[ARCHIVER]: Loading xml with raw size {0}", serialisedSceneObject.Length);

                        // Really large xml files (multi megabyte) appear to cause
                        // memory problems
                        // when loading the xml.  But don't enable this check yet
                
                        if (serialisedSceneObject.Length > 5000000)
                        {
                            MainConsole.Instance.Error("[ARCHIVER]: Ignoring xml since size > 5000000);");
                            continue;
                        }
                        */

                        SceneObjectGroup sceneObject = (SceneObjectGroup)serialiser.DeserializeGroupFromXml2(data, m_scene);

                        if (sceneObject == null)
                        {
                            //! big error!
                            MainConsole.Instance.Error("Error reading SOP XML (Please mantis this!): " + m_asciiEncoding.GetString(data));
                            continue;
                        }

                        foreach (SceneObjectPart part in sceneObject.ChildrenList)
                        {
                            if (part.CreatorData == null || part.CreatorData == string.Empty)
                                part.CreatorID = ResolveUserUuid(part.CreatorID, part.CreatorID, part.CreatorData, part.AbsolutePosition, landData);

                            if (UserManager != null)
                                UserManager.AddUser(part.CreatorID, part.CreatorData);

                            part.OwnerID = ResolveUserUuid(part.OwnerID, part.CreatorID, part.CreatorData, part.AbsolutePosition, landData);

                            part.LastOwnerID = ResolveUserUuid(part.LastOwnerID, part.CreatorID, part.CreatorData, part.AbsolutePosition, landData);

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
                                    kvp.Value.OwnerID = ResolveUserUuid(kvp.Value.OwnerID, kvp.Value.CreatorID, kvp.Value.CreatorData, part.AbsolutePosition, landData);
                                    if (kvp.Value.CreatorData == null || kvp.Value.CreatorData == string.Empty)
                                        kvp.Value.CreatorID = ResolveUserUuid(kvp.Value.CreatorID, kvp.Value.CreatorID, kvp.Value.CreatorData, part.AbsolutePosition, landData);
                                    if (UserManager != null)
                                        UserManager.AddUser(kvp.Value.CreatorID, kvp.Value.CreatorData);
                                }
                            }
                        }

                        //Add the offsets of the region
                        Vector3 newPos = new Vector3(sceneObject.AbsolutePosition.X + m_offsetX,
                            sceneObject.AbsolutePosition.Y + m_offsetY,
                            sceneObject.AbsolutePosition.Z + m_offsetZ);
                        if (m_flipX)
                            newPos.X = m_scene.RegionInfo.RegionSizeX - newPos.X;
                        if (m_flipY)
                            newPos.Y = m_scene.RegionInfo.RegionSizeY - newPos.Y;
                        sceneObject.SetAbsolutePosition(false, newPos);

                        if (m_scene.SceneGraph.AddPrimToScene(sceneObject))
                        {
                            groupsToBackup.Add(sceneObject);
                            sceneObject.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
                            sceneObjectsLoadedCount++;
                            sceneObject.CreateScriptInstances(0, false, StateSource.RegionStart, UUID.Zero);
                        }
                        sceneObjectsLoadedCount++;
                        if (sceneObjectsLoadedCount % 250 == 0)
                            MainConsole.Instance.Info("[ARCHIVER]: Loaded " + sceneObjectsLoadedCount + " objects...");
                    }
                    else if (!m_skipAssets && filePath.StartsWith(ArchiveConstants.ASSETS_PATH))
                    {
                        if (LoadAsset(filePath, data))
                            successfulAssetRestores++;
                        else
                            failedAssetRestores++;

                        if ((successfulAssetRestores + failedAssetRestores) % 250 == 0)
                            MainConsole.Instance.Info("[ARCHIVER]: Loaded " + successfulAssetRestores + " assets and failed to load " + failedAssetRestores + " assets...");
                    }
                    else if (filePath.StartsWith(ArchiveConstants.TERRAINS_PATH))
                    {
                        LoadTerrain(filePath, data);
                    }
                    else if (!m_merge && filePath.StartsWith(ArchiveConstants.SETTINGS_PATH))
                    {
                        LoadRegionSettings(filePath, data);
                    }
                    else if (filePath.StartsWith(ArchiveConstants.LANDDATA_PATH))
                    {
                        LandData parcel = LandDataSerializer.Deserialize(m_utf8Encoding.GetString(data));
                        parcel.OwnerID = ResolveUserUuid(parcel.OwnerID, UUID.Zero, "", Vector3.Zero, null);
                        landData.Add(parcel);
                    }
                    else if (filePath == ArchiveConstants.CONTROL_FILE_PATH)
                    {
                        LoadControlFile(filePath, data);
                    }
                }

                //MainConsole.Instance.Debug("[ARCHIVER]: Reached end of archive");
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat(
                    "[ARCHIVER]: Aborting load with error in archive file {0}.  {1}", filePath, e);
                m_errorMessage += e.ToString();
                m_scene.EventManager.TriggerOarFileLoaded(UUID.Zero.Guid, m_errorMessage);
                return;
            }
            finally
            {
                archive.Close();
                m_loadStream.Close();
                m_loadStream.Dispose();

                //Reeanble now that we are done
                foreach (IScriptModule module in modules)
                {
                    module.Disabled = false;
                }
                //Reset backup too
                if (backup != null)
                    backup.LoadingPrims = false;
            }

            //Now back up the prims
            foreach (SceneObjectGroup grp in groupsToBackup)
            {
                //Backup!
                grp.HasGroupChanged = true;
            }

            if (!m_skipAssets && m_useAsync && !AssetSaverIsRunning)
                    m_threadpool.QueueEvent(SaveAssets, 0);

            if (!m_skipAssets)
            {
                MainConsole.Instance.InfoFormat("[ARCHIVER]: Restored {0} assets", successfulAssetRestores);

                if (failedAssetRestores > 0)
                {
                    MainConsole.Instance.ErrorFormat("[ARCHIVER]: Failed to load {0} assets", failedAssetRestores);
                    m_errorMessage += String.Format("Failed to load {0} assets", failedAssetRestores);
                }
            }

            // Try to retain the original creator/owner/lastowner if their uuid is present on this grid
            // otherwise, use the master avatar uuid instead

            // Reload serialized parcels
            MainConsole.Instance.InfoFormat("[ARCHIVER]: Loading {0} parcels.  Please wait.", landData.Count);

            IParcelManagementModule parcelManagementModule = m_scene.RequestModuleInterface<IParcelManagementModule>();
            if (!m_merge && parcelManagementModule != null)
                parcelManagementModule.ClearAllParcels();
            if (landData.Count > 0)
            {
                m_scene.EventManager.TriggerIncomingLandDataFromStorage(landData, new Vector2(m_offsetX, m_offsetY));
                //Update the database as well!
                if (parcelManagementModule != null)
                {
                    foreach (LandData parcel in landData)
                    {
                        parcelManagementModule.UpdateLandObject(parcelManagementModule.GetLandObject(parcel.LocalID));
                    }
                }
            }
            else if (parcelManagementModule != null)
                parcelManagementModule.ResetSimLandObjects();

            MainConsole.Instance.InfoFormat("[ARCHIVER]: Restored {0} parcels.", landData.Count);

            //Clean it out
            landData.Clear();

            MainConsole.Instance.InfoFormat("[ARCHIVER]: Successfully loaded archive in " + (DateTime.Now - start).Minutes + ":" + (DateTime.Now - start).Seconds);

            m_validUserUuids.Clear();
            m_scene.EventManager.TriggerOarFileLoaded(UUID.Zero.Guid, m_errorMessage);
        }

        /// <summary>
        /// Look up the given user id to check whether it's one that is valid for this grid.
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="creatorID"></param>
        /// <param name="creatorData"></param>
        /// <param name="location"></param>
        /// <param name="parcels"></param>
        /// <returns></returns>
        private UUID ResolveUserUuid(UUID uuid, UUID creatorID, string creatorData, Vector3 location, List<LandData> parcels)
        {
            UUID u;
            if (!m_validUserUuids.TryGetValue(uuid, out u))
            {
                UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.ScopeID, uuid);
                if (account != null)
                {
                    m_validUserUuids.Add(uuid, uuid);
                    return uuid;
                }
                if(uuid == creatorID)
                {
                    UUID hid;
                    string first, last, url, secret;
                    if(HGUtil.ParseUniversalUserIdentifier(creatorData, out hid, out url, out first, out last, out secret))
                    {
                        account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.ScopeID, first, last);
                        if(account != null)
                        {
                            m_validUserUuids.Add(uuid, account.PrincipalID);
                            return account.PrincipalID;//Fix the UUID
                        }
                    }
                }
                IUserManagement uf = m_scene.RequestModuleInterface<IUserManagement> ();
                if (uf != null)
                    if (uf.GetUserExists (uuid))//Foreign user, don't remove their info
                    {
                        m_validUserUuids.Add(uuid, uuid);
                        return uuid;
                    }
                UUID id = UUID.Zero;
                if(m_checkOwnership || (m_useParcelOwnership && parcels == null))//parcels == null is a parcel owner, ask for it if useparcel is on
                {
                    tryAgain:
                    string ownerName = MainConsole.Instance.Prompt(string.Format("User Name to use instead of UUID '{0}'", uuid), "");
                    account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.ScopeID, ownerName);
                    if(account != null)
                        id = account.PrincipalID;
                    else if(ownerName != "")
                        if((ownerName = MainConsole.Instance.Prompt("User was not found, do you want to try again?", "no", new List<string>(new[] { "no", "yes" }))) == "yes")
                            goto tryAgain;
                }
                if(m_useParcelOwnership && id == UUID.Zero && location != Vector3.Zero && parcels != null)
                {
                    foreach (LandData data in parcels)
                    {
                        if (ContainsPoint(data, (int)location.X + m_offsetX, (int)location.Y + m_offsetY))
                            if (uuid != data.OwnerID)
                                id = data.OwnerID;
                    }
                }
                if(id == UUID.Zero)
                    id = m_scene.RegionInfo.EstateSettings.EstateOwner;
                m_validUserUuids.Add(uuid, id);

                return m_validUserUuids[uuid];
            }

            return u;
        }

        private bool ContainsPoint(LandData data, int checkx, int checky)
        {
            int x = 0, y = 0, i = 0;
            for (i = 0; i < data.Bitmap.Length; i++)
            {
                byte tempByte = 0;
                if (i < data.Bitmap.Length)
                    tempByte = data.Bitmap[i];
                else
                    break;//All the rest are false then
                int bitNum = 0;
                for (bitNum = 0; bitNum < 8; bitNum++)
                {
                    if (x == checkx / 4 && y == checky / 4)
                        return Convert.ToBoolean(Convert.ToByte(tempByte >> bitNum) & 1);
                    x++;
                    //Remove the offset so that we get a calc from the beginning of the array, not the offset array
                    if (x > ((m_scene.RegionInfo.RegionSizeX / 4) - 1))
                    {
                        x = 0;//Back to the beginning
                        y++;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Load an asset
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="data"></param>
        /// <returns>true if asset was successfully loaded, false otherwise</returns>
        private bool LoadAsset(string assetPath, byte[] data)
        {
            // Right now we're nastily obtaining the UUID from the filename
            string filename = assetPath.Remove(0, ArchiveConstants.ASSETS_PATH.Length);
            int i = filename.LastIndexOf(ArchiveConstants.ASSET_EXTENSION_SEPARATOR);

            if (i == -1)
            {
                MainConsole.Instance.ErrorFormat(
                    "[ARCHIVER]: Could not find extension information in asset path {0} since it's missing the separator {1}.  Skipping",
                    assetPath, ArchiveConstants.ASSET_EXTENSION_SEPARATOR);

                return false;
            }

            string extension = filename.Substring(i);
            string uuid = filename.Remove(filename.Length - extension.Length);

            if (ArchiveConstants.EXTENSION_TO_ASSET_TYPE.ContainsKey(extension))
            {
                AssetType assetType = ArchiveConstants.EXTENSION_TO_ASSET_TYPE[extension];

                if (assetType == AssetType.Unknown)
                    MainConsole.Instance.WarnFormat("[ARCHIVER]: Importing {0} byte asset {1} with unknown type", data.Length, uuid);

                //MainConsole.Instance.DebugFormat("[ARCHIVER]: Importing asset {0}, type {1}", uuid, assetType);
                AssetBase asset = new AssetBase(UUID.Parse(uuid), String.Empty, assetType, UUID.Zero)
                                      {Data = data};

                // We're relying on the asset service to do the sensible thing and not store the asset if it already
                // exists.
                if (m_useAsync)
                    lock (AssetsToAdd)
                        AssetsToAdd.Add(asset);
                else
                    asset.ID = m_scene.AssetService.Store(asset);

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
            MainConsole.Instance.ErrorFormat(
                "[ARCHIVER]: Tried to dearchive data with path {0} with an unknown type extension {1}",
                assetPath, extension);

            return false;
        }

        private void SaveAssets()
        {
            AssetSaverIsRunning = true;
            lock (AssetsToAdd)
            {
                foreach (AssetBase asset in AssetsToAdd)
                {
                    asset.ID = m_scene.AssetService.Store(asset);
                }
            }
            AssetsToAdd.Clear();
            AssetSaverIsRunning = false;
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
                MainConsole.Instance.ErrorFormat(
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
            terrainModule.LoadFromStream(terrainPath, ms, m_offsetX, m_offsetY);
            ms.Close();

            MainConsole.Instance.DebugFormat("[ARCHIVER]: Restored terrain {0}", terrainPath);

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
                    if (xtr.Name == "datetime")
                    {
                        int value;
                        if (Int32.TryParse(xtr.ReadElementContentAsString(), out value))
                            currentRegionSettings.LoadedCreationDateTime = value;
                    }
                    else if (xtr.Name == "id")
                    {
                        currentRegionSettings.LoadedCreationID = xtr.ReadElementContentAsString();
                    }
                }

            }

            currentRegionSettings.Save();
        }
    }
}
