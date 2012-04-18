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
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;
using Timer = System.Timers.Timer;

namespace Aurora.Services.DataService.Connectors.Database.Asset
{
    public class LocalAssetBlackholeConnector : IAssetDataPlugin
    {
        #region Variables

        private const int m_CacheDirectoryTiers = 3;
        private const int m_CacheDirectoryTierLen = 1;
        private const bool disableTimer = false;
        private readonly List<char> m_InvalidChars = new List<char>();
        private readonly List<Blank> m_genericTasks = new List<Blank>();
        private readonly Stopwatch sw = new Stopwatch();
        private readonly Timer taskTimer = new Timer();
        private int NumberOfDaysForOldAssets = -30;

        private int convertCount;
        private int convertCountDupe;
        private int convertCountParentFix;
        private int displayCount;
        private string m_CacheDirectory = "./BlackHoleAssets";
        private string m_CacheDirectoryBackup = "./BlackHoleBackup";
        private bool m_Enabled;
        private IGenericData m_Gd;
        private bool m_pointInventory2ParentAssets = true;
        private bool needsConversion;
        private readonly List<string> lastNotFound = new List<string>();


        private delegate void Blank();

        #endregion

        #region Implementation of IAuroraDataPlugin

        /// <summary>
        /// Gets interface name
        /// </summary>
        public string Name
        {
            get { return "IAssetDataPlugin"; }
        }

        /// <summary>
        /// Part of the Iservice
        /// </summary>
        /// <param name="genericData"></param>
        /// <param name="source"></param>
        /// <param name="simBase"></param>
        /// <param name="defaultConnectionString"></param>
        public void Initialize(IGenericData genericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AssetConnector", "LocalConnector") !=
                "LocalConnectorBlackHole")
                return;
            m_Gd = genericData;
            m_Enabled = true;

            if (source.Configs["Handlers"].GetString("AssetHandler", "") != "AssetService")
                return;

            m_CacheDirectory = source.Configs["BlackHole"].GetString("CacheDirector", m_CacheDirectory);
            m_CacheDirectoryBackup = source.Configs["BlackHole"].GetString("BackupCacheDirector", m_CacheDirectoryBackup);
            NumberOfDaysForOldAssets = source.Configs["BlackHole"].GetInt("AssetsAreOldAfterHowManyDays", 30) * -1;
            m_Enabled = true;

            m_pointInventory2ParentAssets = source.Configs["BlackHole"].GetBoolean("PointInventoryToParentAssets", true);


            if (!Directory.Exists(m_CacheDirectoryBackup))
                Directory.CreateDirectory(m_CacheDirectoryBackup);
            if (!Directory.Exists(m_CacheDirectoryBackup))
            {
                MainConsole.Instance.Error(
                    "Check your Main.ini and ensure your backup directory is set! under [BlackHole] BackupCacheDirector");
                m_Enabled = false;
                return;
            }

            if (!Directory.Exists(m_CacheDirectory))
                Directory.CreateDirectory(m_CacheDirectory);
            if (!Directory.Exists(m_CacheDirectory))
            {
                MainConsole.Instance.Error(
                    "Check your Main.ini and ensure your cache directory is set! under [BlackHole] m_CacheDirectory");
                m_Enabled = false;
                return;
            }

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);
            genericData.ConnectToDatabase(defaultConnectionString, "BlackholeAsset",
                                          source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            m_InvalidChars.AddRange(Path.GetInvalidPathChars());
            m_InvalidChars.AddRange(Path.GetInvalidFileNameChars());

            if (m_Enabled)
            {
                MainConsole.Instance.Error("[BlackholeAssets]: Blackhole assets enabled");
                DataManager.DataManager.RegisterPlugin(this);
                try
                {
                    needsConversion = (m_Gd.Query(new string[1] { "id" }, "assets", null, null, 0, 1).Count >= 1);
                }
                catch
                {
                    // the assets table might not exist if they next used it.. 
                    needsConversion = false;
                }
                convertCount = 0;
                taskTimer.Interval = 60000;
                taskTimer.Elapsed += t_Elapsed;
                taskTimer.Start();
            }
        }

        #endregion

        #region Implementation of IAssetDataPlugin

        #region GetAsset

        /// <summary>
        ///   Get a asset
        /// </summary>
        /// <param name = "uuid">UUID of the asset requesting</param>
        /// <returns>AssetBase</returns>
        public AssetBase GetAsset(UUID uuid)
        {
            return GetAsset(uuid, false, true);
        }

        /// <summary>
        ///   Get a asset without the actual data. You can always use MetaOnly Property to deterine if its there
        /// </summary>
        /// <param name = "uuid">UUID of the asset requesting</param>
        /// <returns>AssetBase without the actual asset data</returns>
        public AssetBase GetMeta(UUID uuid)
        {
            return GetAsset(uuid, true, true);
        }

        private AssetBase GetAsset(UUID uuid, bool metaOnly, bool displayMessages)
        {
            ResetTimer(15000);
            if (lastNotFound.Contains(uuid.ToString())) return null;
            string databaseTable = "auroraassets_" + uuid.ToString().Substring(0, 1);
            IDataReader dr = null;
            AssetBase asset = null;
            try
            {
                // get the asset
                dr = m_Gd.QueryData("WHERE id = '" + uuid + "' LIMIT 1", databaseTable,
                                    "id, hash_code, parent_id, creator_id, name, description, asset_type, create_time, access_time, asset_flags, host_uri");
                asset = LoadAssetFromDR(dr);

                if ((asset == null) && (needsConversion))
                {
                    // check to see if it needs converted
                    asset = Convert2BH(uuid);
                    if (asset != null)
                    {
                        if (metaOnly) asset.Data = new byte[] { };
                        asset.MetaOnly = metaOnly;
                    }
                }

                if (asset == null)
                {
                    // check the old table
                    databaseTable = "auroraassets_old";
                    dr = m_Gd.QueryData("WHERE id = '" + uuid + "' LIMIT 1", databaseTable,
                                        "id, hash_code, parent_id, creator_id, name, description, asset_type, create_time, access_time, asset_flags, host_uri");
                    asset = LoadAssetFromDR(dr);
                    if (asset != null)
                    {
                        bool results = false;
                        AssetBase asset2 = StoreAsset(asset, out results, true);
                        if (results) asset = asset2;
                    }
                }


                if ((asset == null) && (displayMessages))
                {
                    // oh well.. we tried
                    MainConsole.Instance.Warn("[LocalAssetBlackholeConnector] GetAsset(" + uuid +
                                              "); Unable to find asset " + uuid);
                    lastNotFound.Add(uuid.ToString());
                }
                if (asset == null) return null;

                if (!metaOnly)
                {
                    // load all the data
                    asset.Data = LoadFile(asset.HashCode);
                }
                asset.MetaOnly = metaOnly;
                Util.FireAndForget(delegate
                {
                    updateAccessTime(databaseTable, asset.ID);
                });
            }
            catch (Exception e)
            {
                if (displayMessages)
                    MainConsole.Instance.Error("[LocalAssetBlackholeConnector] GetAsset(" + uuid + "); Error ", e);
            }
            finally
            {
                if (dr != null) dr.Close();
            }
            return asset;
        }


        private void updateAccessTime(string databaseTable, UUID assetID)
        {
            Dictionary<string, object> values = new Dictionary<string, object>(1);
            values["access_time"] = Util.ToUnixTime(DateTime.UtcNow);

            QueryFilter filter = new QueryFilter();
            filter.andFilters["id"] = assetID;

            // save down last time updated
            m_Gd.Update(databaseTable, values, null, filter, null, null);
        }

        private AssetBase LoadAssetFromDR(IDataReader dr)
        {
            try
            {
                if (dr != null)
                {
                    while (dr.Read())
                    {
                        return new AssetBase()
                        {
                            ID = UUID.Parse(dr["id"].ToString()),
                            Name = dr["name"].ToString(),
                            TypeAsset = (AssetType)int.Parse(dr["asset_type"].ToString()),
                            CreatorID = UUID.Parse(dr["creator_id"].ToString()),
                            CreationDate = UnixTimeStampToDateTime(int.Parse(dr["create_time"].ToString())),
                            DatabaseTable = "auroraassets_" + dr["id"].ToString().Substring(0, 1),
                            Description = dr["description"].ToString(),
                            Flags = (AssetFlags)int.Parse(dr["asset_flags"].ToString()),
                            HashCode = dr["hash_code"].ToString(),
                            HostUri = dr["host_uri"].ToString(),
                            LastAccessed = DateTime.UtcNow,
                            ParentID =
                                (dr["parent_id"].ToString() == "")
                                    ? UUID.Parse(dr["id"].ToString())
                                    : UUID.Parse(dr["parent_id"].ToString())
                        };
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[LocalAssetBlackholeConnector] LoadAssetFromDR(); Error Loading", e);
            }
            finally
            {
                if (dr != null)
                {
                    dr.Close();
                }
            }
            return null;
        }

        #endregion

        #region Store Asset

        /// <summary>
        ///   Stores the Asset in the database
        /// </summary>
        /// <param name = "asset">Asset you wish to store</param>
        /// <returns></returns>
        public UUID Store(AssetBase asset)
        {
            bool successful;
            asset = StoreAsset(asset, out successful, false);
            return asset.ID;
        }

        /// <summary>
        ///   Stores the Asset in the database
        /// </summary>
        /// <param name = "asset">Asset you wish to store</param>
        /// <returns></returns>
        public bool StoreAsset(AssetBase asset)
        {
            bool successful;
            StoreAsset(asset, out successful, false);
            return successful;
        }

        /// <summary>
        /// Update just the content of the asset, will return a UUID.Zero if the asset does not exist. So check that afterwards
        /// </summary>
        /// <param name="id">UUID of asset you want to change</param>
        /// <param name="assetdata"></param>
        /// <param name="newID"></param>
        public void UpdateContent(UUID id, byte[] assetdata, out UUID newID)
        {
            try
            {
                AssetBase asset = GetMeta(id);
                if (asset != null)
                {
                    asset.Data = assetdata;

                    bool success;
                    AssetBase newasset = StoreAsset(asset, out success, false);
                    if (success)
                    {
                        newID = newasset.ID;
                        return;
                    }
                    MainConsole.Instance.ErrorFormat("[LocalAssetBlackholeConnector] - UpdateContent {0} - Failed to save ", id);
                }
                else
                    MainConsole.Instance.ErrorFormat("[LocalAssetBlackholeConnector] - Updating asset content to a asset that does not exisxt {0}", id);
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[LocalAssetBlackholeConnector] UpdateContent", e);
            }
            newID = UUID.Zero;

        }

        private AssetBase StoreAsset(AssetBase asset, out bool successful, bool justMovingDatabase)
        {
            ResetTimer(15000);
            try
            {
                Dictionary<string, object> row;
                bool assetDoesExist = false;
                // this was causing problems with convering the first asset which.. is a zero id.. 
                if (!justMovingDatabase)
                {
                    assetDoesExist = ExistsAsset(asset.ID);
                    if (assetDoesExist)
                    {
                        Dictionary<string, object> where = new Dictionary<string, object>(1);
                        where["id"] = asset.ID;
                        QueryFilter filter = new QueryFilter
                        {
                            andFilters = where
                        };

                        string databaseTable = "auroraassets_" + asset.ID.ToString().Substring(0, 1);
                        List<string> results = m_Gd.Query(new string[] { "asset_flags" }, databaseTable, filter, null, null, null);
                        AssetFlags thisassetflag = AssetFlags.Rewritable;
                        if ((results != null) && (results.Count >= 1))
                        {
                            thisassetflag = (AssetFlags)int.Parse(results[0]);
                        }
                        else
                        {
                            databaseTable = "auroraassets_old";
                            results = m_Gd.Query(new string[] { "asset_flags" }, databaseTable, filter, null, null, null);
                            if ((results != null) && (results.Count >= 1))
                                thisassetflag = (AssetFlags)int.Parse(results[0]);
                        }

                        if (((thisassetflag & AssetFlags.Rewritable) != AssetFlags.Rewritable))
                        {
                            asset.ID = UUID.Random();
                            asset.CreationDate = DateTime.UtcNow;
                            assetDoesExist = false;
                        }
                    }

                    if (asset.Name.Length > 64) asset.Name = asset.Name.Substring(0, 64);
                    if (asset.Description.Length > 128) asset.Description = asset.Description.Substring(0, 128);

                    // Get the new hashcode if this is not MataOnly Data
                    if ((!asset.MetaOnly) || ((asset.Data != null) && (asset.Data.Length >= 1)))
                        asset.HashCode = WriteFile(asset.ID, asset.Data);

                    if ((!asset.MetaOnly) && ((asset.HashCode != asset.LastHashCode) || (!assetDoesExist)))
                    {
                        row = new Dictionary<string, object>(3);
                        if (asset.HashCode != asset.LastHashCode)
                        {
                            // check if that hash is being used anywhere later
                            row["id"] = UUID.Random();
                            row["task_type"] = "HASHCHECK";
                            row["task_values"] = asset.LastHashCode;
                            m_Gd.Insert("auroraassets_tasks", row);
                        }
                        row = new Dictionary<string, object>(3);
                        QueryFilter filter = new QueryFilter();
                        filter.andFilters["hash_code"] = asset.HashCode;
                        filter.andFilters["creator_id"] = asset.CreatorID;
                        // check to see if this hash/creator combo already exist
                        List<string> check1 = m_Gd.Query(new string[1] { "id" }, "auroraassets_temp", filter, null, null, null);
                        if ((check1 != null) && (check1.Count >= 1) && (asset.CreatorID != new UUID("11111111-1111-0000-0000-000100bba000")))
                        {
                            successful = true;
                            AssetBase abtemp = GetAsset(UUID.Parse(check1[0]));
                            // not going to save it... 
                            // use existing asset instead
                            if (abtemp != null) return abtemp;

                            // that asset returned nothing.. so.. 
                            // do some checks on it later
                            row["id"] = UUID.Random();
                            row["task_type"] = "PARENTCHECK";
                            row["task_values"] = check1[0] + "|" + asset.ID;
                            m_Gd.Insert("auroraassets_tasks", row);
                            asset.ParentID = asset.ID;
                        }
                        else if (asset.CreatorID != new UUID("11111111-1111-0000-0000-000100bba000"))
                        {
                            // was not found so insert it
                            row["id"] = asset.ID;
                            row["hash_code"] = asset.HashCode;
                            row["creator_id"] = asset.CreatorID;
                            m_Gd.Insert("auroraassets_temp", row);
                            asset.ParentID = asset.ID;
                        }
                    }
                }
                else
                {
                    assetDoesExist = true;
                }


                // Ensure some data is correct



                string database = "auroraassets_" + asset.ID.ToString().Substring(0, 1);
                // Delete and save the asset
                if (assetDoesExist)
                {
                    Delete(asset.ID, false, true, asset);
                }
                row = new Dictionary<string, object>(11);
                row["id"] = asset.ID;
                row["hash_code"] = asset.HashCode;
                row["parent_id"] = (asset.ID == asset.ParentID) ? "" : (UUID.Zero == asset.ParentID) ? "" : asset.ParentID.ToString();
                row["creator_id"] = (asset.CreatorID == UUID.Zero) ? "" : asset.CreatorID.ToString();
                row["name"] = asset.Name.MySqlEscape(64);
                row["description"] = asset.Description.MySqlEscape(128);
                row["asset_type"] = (int)asset.TypeAsset;
                row["create_time"] = Util.ToUnixTime(asset.CreationDate);
                row["access_time"] = Util.ToUnixTime(DateTime.UtcNow);
                row["asset_flags"] = (int)asset.Flags;
                row["host_uri"] = asset.HostUri;
                row["owner_id"] = "";
                m_Gd.Insert(database, row);
                if (lastNotFound.Contains(asset.ID.ToString()))
                {
                    lastNotFound.Remove(asset.ID.ToString());
                }
                // Double checked its saved. Just for debug
                if (needsConversion)
                {
                    Dictionary<string, object> where = new Dictionary<string, object>(1);
                    where["id"] = asset.ID;
                    if(m_Gd.Query(new string[]{ "id" }, "auroraassets_" + asset.ID.ToString().Substring(0, 1), new QueryFilter{
                        andFilters = where
                    }, null, null, null).Count == 0)
                    {
                        MainConsole.Instance.Error("[AssetDataPlugin] Asset did not saver propery: " + asset.ID);
                        successful = false;
                        return asset;
                    }
                }
                successful = true;
                return asset;
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[AssetDataPlugin]: StoreAsset(" + asset.ID + ")", e);
            }
            successful = false;
            return asset;
        }

        #endregion

        #region asset exists

        /// <summary>
        ///   Check to see if a asset exists
        /// </summary>
        /// <param name = "uuid">UUID of the asset you want to check</param>
        /// <returns></returns>
        public bool ExistsAsset(UUID uuid)
        {
            ResetTimer(15000);
            try
            {
                QueryFilter filter = new QueryFilter();
                filter.andFilters["id"] = uuid;

                bool result = m_Gd.Query(new string[] { "id" }, "auroraassets_" + uuid.ToString().Substring(0, 1), filter, null, null, null).Count >= 1;
                if (!result)
                {
                    result = m_Gd.Query(new string[] { "id" }, "auroraassets_old", filter, null, null, null).Count >= 1;
                }
                if ((!result) && (needsConversion))
                {
                    AssetBase a = Convert2BH(uuid);
                    return a != null;
                }
                return result;
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat(
                    "[ASSETS DB]: MySql failure fetching asset {0}" + Environment.NewLine + e, uuid);
            }
            return false;
        }

        #endregion

        #region Delete Asset

        /// <summary>
        ///   Delete the asset from the database and file system
        /// </summary>
        /// <param name = "id">UUID of the asset you wish to delete</param>
        /// <returns></returns>
        public bool Delete(UUID id)
        {
            return Delete(id, true, false);
        }

        /// <summary>
        ///   Delete the asset from the database and file system and ignores the asset flags
        /// </summary>
        /// <param name = "id">UUID of the asset you wish to delete</param>
        /// <returns></returns>
        public bool Delete(UUID id, bool ignoreFlags)
        {
            return Delete(id, true, ignoreFlags);
        }

        private bool Delete(UUID id, bool assignHashCodeCheckTask, bool ignoreFlags)
        {
            AssetBase asset = GetAsset(id, true, false);
            if (asset == null) return false;
            return Delete(id, assignHashCodeCheckTask, ignoreFlags, asset);
        }

        private bool Delete(UUID id, bool assignHashCodeCheckTask, bool ignoreFlags, AssetBase asset)
        {
            ResetTimer(15000);
            string tableName = "auroraassets_" + id.ToString().Substring(0, 1);
            try
            {
                // assign a task to see if the hash code is being used anywhere else
                if (assignHashCodeCheckTask)
                {
                    Dictionary<string, object> row = new Dictionary<string, object>(3);
                    row["id"] = UUID.Random();
                    row["task_type"] = "HASHCHECK";
                    row["task_values"] = asset.HashCode;
                    m_Gd.Insert("auroraassets_tasks", row);
                }

                // check deleteability of this asset.. if needed
                if (!ignoreFlags)
                {
                    if ((int)(asset.Flags & AssetFlags.Maptile) != 0 || //Depriated, use Deletable instead
                        (int)(asset.Flags & AssetFlags.Deletable) != 0)
                        ignoreFlags = true;
                }

                if (ignoreFlags)
                {
                    QueryFilter filter = new QueryFilter();
                    filter.andFilters["id"] = id;
                    // delete the asset
                    m_Gd.Delete(tableName, filter);
                    // just for safe measure check here as well
                    m_Gd.Delete("auroraassets_old", filter);
                }
                return ignoreFlags;
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[AssetDataPlugin] Delete - Error for asset ID " + id, e);
                return false;
            }
        }

        #endregion

        #endregion

        #region util functions

        private static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        #endregion

        #region File Management

        private string WriteFile(UUID assetid, byte[] data, int tryCount)
        {
            bool alreadyWriten = false;
            Stream stream = null;
            BinaryFormatter bformatter = new BinaryFormatter();
            string hashCode = Convert.ToBase64String(new SHA256Managed().ComputeHash(data)) + data.Length;
            try
            {
                string filename = GetFileName(hashCode, false);
                string directory = Path.GetDirectoryName(filename);
                if (directory != null && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
                if (File.Exists(filename)) alreadyWriten = true;

                if (!alreadyWriten)
                {
                    try
                    {
                        stream = File.Open(filename, FileMode.Create);
                        bformatter.Serialize(stream, data);
                        stream.Close();
                        stream = null;
                    }
                    catch (IOException e)
                    {
                        if (stream != null) stream.Close();
                        stream = null;
                        if (tryCount <= 1)
                        {
                            Thread.Sleep(500);
                            tryCount = tryCount + 1;
                            WriteFile(assetid, data, tryCount);
                        }
                        else
                        {
                            MainConsole.Instance.Error("[AssetDataPlugin] Error writing Asset File " + assetid, e);
                        }
                    }
                    string filenameForBackup = GetFileName(hashCode, true) + ".7z";
                    directory = Path.GetDirectoryName(filenameForBackup);
                    if (directory != null && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
                    if (!File.Exists(filenameForBackup))
                        Util.Compress7ZipFile(filename, filenameForBackup);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[AssetDataPlugin]: WriteFile(" + assetid + ")", e);
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return hashCode;
        }

        private string WriteFile(UUID assetid, byte[] data)
        {
            return WriteFile(assetid, data, 0);
        }

        private Byte[] LoadFile(string hashCode)
        {
            return LoadFile(hashCode, false);
        }

        private Byte[] LoadFile(string hashCode, bool waserror)
        {
            Stream stream = null;
            BinaryFormatter bformatter = new BinaryFormatter();
            byte[] results = new byte[] { };
            string filename = GetFileName(hashCode, false);
            bool wasErrorLoading = false;
            try
            {
                if (!File.Exists(filename))
                {
                    if (!RestoreBackup(hashCode))
                        return new byte[] { };
                }
                stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                results = (Byte[])bformatter.Deserialize(stream);
            }
            catch
            {
                wasErrorLoading = true;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

            // we have to do this stuff after the try catch final to ensure stream is closed
            if ((wasErrorLoading) && (!waserror))
                return RestoreBackup(hashCode) ? LoadFile(hashCode, true) : null;
            if (wasErrorLoading)
                return null;

            Util.FireAndForget(delegate
            {
                FileCheck(hashCode, results, waserror);
            });
            return results;
        }

        private void FileCheck(string hashCode, byte[] results, bool waserror)
        {
            // check the files results with hash.. see if they match
            if (hashCode != Convert.ToBase64String(new SHA256Managed().ComputeHash(results)) + results.Length)
            {
                // seen this happen a couple times.. recovery seems to work good..
                if (!waserror)
                {
                    if (RestoreBackup(hashCode))
                        LoadFile(hashCode, true);
                    else
                        MainConsole.Instance.Error("[AssetDataPlugin]: Resulting files didn't match hash. Failed recovery");
                }
                else
                    MainConsole.Instance.Error("[AssetDataPlugin]: Resulting files didn't match hash. Failed recovery 2");
            }
            else if (waserror)
                MainConsole.Instance.Error("[AssetDataPlugin]: Asset recovery successfully");
        }

        private bool RestoreBackup(string hashCode)
        {
            return RestoreBackup(hashCode, 0);
        }

        private bool RestoreBackup(string hashCode, int trycount)
        {
            string backupfile = GetFileName(hashCode, true) + ".7z";
            string file = GetFileName(hashCode, false);
            // ever now and then getting system io exceptions because the file already exist
            try
            {
                if (File.Exists(backupfile))
                {
                    if (File.Exists(file))
                    {
                        if (File.Exists(file + ".corrupt"))
                            File.Delete(file + ".corrupt");
                        File.Move(file, file + ".corrupt");
                    }
                    Util.UnCompress7ZipFile(backupfile, Path.GetDirectoryName(file));
                    MainConsole.Instance.Info("[AssetDataPlugin] Restored backup asset file " + file);
                    return true;
                }
            }
            catch (IOException e)
            {
                if (trycount <= 1)
                {
                    Thread.Sleep(500);
                    trycount = trycount + 1;
                    return RestoreBackup(hashCode, trycount);
                }
                MainConsole.Instance.Error("[AssetDataPlugin] Restore back error:", e);
            }
            return false;
        }

        private string GetFileName(string id, bool backup)
        {
            string path = (backup) ? m_CacheDirectoryBackup : m_CacheDirectory;
            try
            {
#if (!ISWIN)
                foreach (char invalidChar in m_InvalidChars)
                    id = id.Replace(invalidChar, '_');
#else
                id = m_InvalidChars.Aggregate(id, (current, c) => current.Replace(c, '_'));
#endif
                for (int p = 1; p <= m_CacheDirectoryTiers; p++)
                {
                    string pathPart = id.Substring(0, m_CacheDirectoryTierLen);
                    path = Path.Combine(path, pathPart);
                    id = id.Substring(1);
                }
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Error("[] Error while getting filename", ex);
            }
            return Path.Combine(path, id + ".ass");
        }

        #endregion

        #region Old Asset Migration To BlackHole

        private readonly Dictionary<UUID, AssetBase> m_convertingAssets = new Dictionary<UUID, AssetBase>();

        private void StartMigration()
        {
            if (!sw.IsRunning)
            {
                sw.Start();
            }
            displayCount++;
            List<string> toConvert = m_Gd.Query(new string[1] { "id" }, "assets", null, null, 0, 5);
            if (toConvert.Count >= 1)
            {
                foreach (string assetkey in toConvert)
                {
                    Convert2BH(UUID.Parse(assetkey));
                }
            }
            else
            {
                needsConversion = false; //ALL DONE!
            }
            if (displayCount == 100)
            {
                sw.Stop();
                MainConsole.Instance.Info("[Blackhole Assets] Converted:" + convertCount + " DupeContent:" + convertCountDupe + " Dupe4Creator:" + convertCountParentFix);
                MainConsole.Instance.Info("[Blackhole Assets] 500 in " + sw.Elapsed.Minutes + ":" + sw.Elapsed.Seconds);
                displayCount = 0;
                sw.Reset();
                sw.Start();
            }
        }

        private AssetBase Convert2BH(UUID uuid)
        {
            AssetBase asset = null;
            IDataReader dr = null;
            try
            {
                if (m_convertingAssets.TryGetValue(uuid, out asset))
                    return asset;
                dr = m_Gd.QueryData("WHERE id = '" + uuid + "' LIMIT 1", "assets",
                                                "id, name, description, assetType, local, temporary, asset_flags, CreatorID, create_time, data");
                if (dr != null)
                {
                    while (dr != null && dr.Read())
                    {
                        asset = new AssetBase()
                        {
                            ID = UUID.Parse(dr["id"].ToString()),
                            Name = dr["name"].ToString(),
                            TypeAsset = (AssetType)int.Parse(dr["assetType"].ToString()),
                            CreatorID = UUID.Parse(dr["CreatorID"].ToString()),
                            Flags = (AssetFlags)int.Parse(dr["asset_flags"].ToString()),
                            Data = (Byte[])dr["data"],
                            Description = dr["description"].ToString(),
                            CreationDate = UnixTimeStampToDateTime(int.Parse(dr["create_time"].ToString())),
                            LastAccessed = DateTime.Now,
                            DatabaseTable = "auroraassets_" + dr["id"].ToString().Substring(0, 1),
                            MetaOnly = false,
                            ParentID = UUID.Parse(dr["id"].ToString())
                        };

                        // set the flags
                        if (dr["local"].ToString().Equals("1") ||
                            dr["local"].ToString().Equals("true", StringComparison.InvariantCultureIgnoreCase))
                            asset.Flags |= AssetFlags.Local;
                        if (bool.Parse(dr["temporary"].ToString()))
                            asset.Flags |= AssetFlags.Temporary;
                        dr.Close();
                        dr = null;
                        m_convertingAssets[uuid] = asset;

                        ResetTimer(1000); //Fire the timer in 1s to finish conversion
                        lock (m_genericTasks)
                        {
                            AssetBase asset1 = asset;
                            m_genericTasks.Add(delegate
                            {
                                // go through this asset and change all the guids to the parent IDs
                                if (!asset1.IsBinaryAsset)
                                {
                                    const string sPattern =
                                        @"(\{{0,1}([0-9a-fA-F]){8}-([0-9a-f]){4}-([0-9a-f]){4}-([0-9a-f]){4}-([0-9a-f]){12}\}{0,1})";
                                    string stringData = Utils.BytesToString(asset1.Data);
                                    bool changed = false;
                                    MatchCollection mc = Regex.Matches(stringData, sPattern);
                                    if (mc.Count >= 1)
                                    {
                                        foreach (Match match in mc)
                                        {
                                            try
                                            {
                                                UUID theMatch = UUID.Parse(match.Value);
                                                if (theMatch != UUID.Zero)
                                                {
                                                    AssetBase mightBeAsset = GetAsset(theMatch,
                                                                                      true, false);
                                                    if ((mightBeAsset != null) &&
                                                        (mightBeAsset.ParentID != UUID.Zero) &&
                                                        (mightBeAsset.ParentID != mightBeAsset.ID))
                                                    {
                                                        stringData =
                                                            stringData.Replace(match.Value,
                                                                               mightBeAsset.
                                                                                   ParentID.
                                                                                   ToString());
                                                        changed = true;
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                MainConsole.Instance.Error("Errored", e);
                                            }
                                        }
                                    }
                                    if (changed)
                                    {
                                        asset1.Data = Utils.StringToBytes(stringData);
                                        // so it doesn't try to find the old file
                                        asset1.LastHashCode = asset1.HashCode;
                                    }
                                }
                                if (
                                    File.Exists(
                                        GetFileName(
                                            Convert.ToBase64String(
                                                new SHA256Managed().ComputeHash(asset1.Data)) +
                                            asset1.Data.Length, false)))
                                {
                                    convertCountDupe++;
                                }

                                QueryFilter filter = new QueryFilter();
                                filter.andFilters["hash_code"] = asset1.HashCode;
                                filter.andFilters["creator_id"] = asset1.CreatorID;

                                // check to see if this asset should have a parent ID
                                List<string> check1 = m_Gd.Query(new string[1] { "id" }, "auroraassets_temp", filter, null, null, null);

                                bool update = false;
                                bool insert = false;
                                if ((check1 != null) && (check1.Count == 0))
                                {
                                    asset1.ParentID = asset1.ID;
                                    insert = true;
                                }
                                else if ((check1 != null) && (check1[0] != asset1.ID.ToString()))
                                {
                                    convertCountParentFix++;
                                    asset1.ParentID = new UUID(check1[0]);

                                    update = true;
                                }
                                else
                                {
                                    asset1.ParentID = asset1.ID;
                                }

                                bool wassuccessful;
                                asset1.HashCode = WriteFile(asset1.ID, asset1.Data, 0);
                                StoreAsset(asset1, out wassuccessful, true);
                                if (wassuccessful)
                                {
                                    filter = new QueryFilter();
                                    filter.andFilters["id"] = asset1.ID;
                                    m_Gd.Delete("assets", filter);
                                }

                                try
                                {
                                    if (insert)
                                    {
                                        Dictionary<string, object> row = new Dictionary<string, object>(3);
                                        row["id"] = asset1.ID;
                                        row["hash_code"] = asset1.HashCode;
                                        row["creator_id"] = asset1.CreatorID;
                                        m_Gd.Insert("auroraassets_temp", row);
                                    }
                                    else if ((update) && (m_pointInventory2ParentAssets))
                                    {
                                        Dictionary<string, object> values = new Dictionary<string, object>(1);
                                        values["assetID"] = asset1.ParentID;

                                        filter = new QueryFilter();
                                        filter.andFilters["assetID"] = asset1.ID;

                                        m_Gd.Update("inventoryitems", values, null, filter, null, null);
                                    }
                                }
                                catch (Exception e)
                                {
                                    MainConsole.Instance.Error("[LocalAssetBlackholeManualMigration] Error on update/insert", e);
                                }
                                convertCount++;
                                m_convertingAssets.Remove(uuid);
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[LocalAssetBlackholeManualMigration] Migrate Error", e);
            }
            finally
            {
                if (dr != null) dr.Close();
            }
            return asset;
        }

        #endregion

        #region Timer

        private void ResetTimer(int howLong)
        {
            taskTimer.Stop();
            taskTimer.Interval = howLong;
            if ((m_Enabled) && (!disableTimer))
                taskTimer.Start();
        }

        /// <summary>
        ///   Timer runs tasks in the background when not busy
        /// </summary>
        /// <param name = "sender"></param>
        /// <param name = "e"></param>
        private void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            taskTimer.Stop();
            if (m_genericTasks.Count > 0)
            {
                List<Blank> tasks;
                lock (m_genericTasks)
                {
                    tasks = new List<Blank>(m_genericTasks);
                    m_genericTasks.Clear();
                }
                foreach (Blank b in tasks)
                {
                    b();
                }
                ResetTimer(1000);
                return;
            }
            if (needsConversion)
            {
                StartMigration();
                ResetTimer(1000);
                return;
            }

            // check for task in the auroraassets_task table
            List<string> taskCheck = m_Gd.Query(new string[3]{
                "id",
                "task_type",
                "task_values"
            }, "auroraassets_tasks", null, null, 0, 1);

            Dictionary<string, object> row;

            if (taskCheck.Count == 3)
            {
                string task_id = taskCheck[0];
                string task_type = taskCheck[1];
                string task_value = taskCheck[2];

                try
                {
                    //check if this hash file is still used anywhere
                    if (task_type == "HASHCHECK")
                    {
                        if (File.Exists(GetFileName(task_value, false)))
                        {
                            int result = TaskGetHashCodeUseCount(task_value);
                            if (result == 0)
                            {
                                MainConsole.Instance.Info("[AssetDataPlugin] Deleteing old unused asset file");
                                File.Delete(GetFileName(task_value, false));
                                if (File.Exists(GetFileName(task_value, true)))
                                    File.Delete(GetFileName(task_value, true));
                            }
                        }
                    }
                    else if ((task_type == "PARENTCHECK") && (task_value.Split('|').Count() > 1))
                    {
                        UUID uuid1 = UUID.Parse(task_value.Split('|')[0]);

                        UUID uuid2 = UUID.Parse(task_value.Split('|')[1]);

                        // double check this asset does not exist 
                        AssetBase abtemp = GetAsset(uuid1);
                        AssetBase actemp = GetAsset(uuid2);
                        if ((abtemp == null) && (actemp != null))
                        {
                            QueryFilter dfilter = new QueryFilter();
                            dfilter.andFilters["id"] = uuid1;
                            m_Gd.Delete("auroraassets_temp", dfilter);
                            row = new Dictionary<string, object>(3);
                            row["id"] = actemp.ID;
                            row["hash_code"] = actemp.HashCode;
                            row["creator_id"] = actemp.CreatorID;
                            m_Gd.Insert("auroraassets_temp", row);
                            // I admit this might be a bit over kill.. 

                            Dictionary<string, object> values = new Dictionary<string, object>(1);
                            values["parent_id"] = uuid2;

                            QueryFilter filter = new QueryFilter();
                            filter.andFilters["parent_id"] = uuid1;

                            string[] tables = new string[17]{
                                "auroraassets_a",
                                "auroraassets_b",
                                "auroraassets_c",
                                "auroraassets_d",
                                "auroraassets_e",
                                "auroraassets_f",
                                "auroraassets_0",
                                "auroraassets_1",
                                "auroraassets_2",
                                "auroraassets_3",
                                "auroraassets_4",
                                "auroraassets_5",
                                "auroraassets_6",
                                "auroraassets_7",
                                "auroraassets_8",
                                "auroraassets_9",
                                "auroraassets_old"
                            };
                            foreach (string table in tables)
                            {
                                m_Gd.Update(table, values, null, filter, null, null);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.Error("[AssetDataPlugin] Background task error. Task " + task_type, ex);
                }
                finally
                {
                    QueryFilter filter = new QueryFilter();
                    filter.andFilters["id"] = task_id;
                    m_Gd.Delete("auroraassets_tasks", filter);
                    ResetTimer(500);
                }
            }
            else
            {
                // check for old assets that have not been access for over 30 days
                try
                {
                    QueryFilter filter = new QueryFilter();
                    filter.andLessThanFilters["access_time"] = Util.ToUnixTime(DateTime.UtcNow.AddDays(NumberOfDaysForOldAssets));
                    List<string> findOld2 = m_Gd.Query(new string[1] { "id" }, "auroraassets_" + UUID.Random().ToString().ToCharArray()[0], filter, null, 0, 1);
                    if (findOld2.Count >= 1)
                    {
                        filter = new QueryFilter();
                        foreach (string ass in findOld2)
                        {
                            filter.andFilters["id"] = ass;
                            List<string> findOld = m_Gd.Query(new string[11]{
                                "id",
                                "hash_code",
                                "name",
                                "description",
                                "asset_type",
                                "create_time",
                                "access_time",
                                "asset_flags",
                                "creator_id",
                                "host_uri",
                                "parent_id"
                            }, "auroraassets_" + ass.ToCharArray()[0], filter, null, null, null);

                            if (m_Gd.Query(new string[] { "id" }, "auroraassets_old", filter, null, null, null).Count == 0)
                            {
                                row = new Dictionary<string, object>(12);
                                row["id"] = findOld[0];
                                row["hash_code"] = findOld[1];
                                row["name"] = findOld[2];
                                row["description"] = findOld[3];
                                row["asset_type"] = findOld[4];
                                row["create_time"] = findOld[5];
                                row["access_time"] = findOld[6];
                                row["asset_flags"] = findOld[7];
                                row["creator_id"] = findOld[8];
                                row["host_uri"] = findOld[9];
                                row["parent_id"] = findOld[10];
                                row["owner_id"] = "";
                                m_Gd.Insert("auroraassets_old", row);
                            }
                            if (m_Gd.Query(new string[] { "id" }, "auroraassets_old", filter, null, null, null).Count > 0)
                            {
                                m_Gd.Delete("auroraassets_" + ass.ToCharArray()[0], filter);
                            }
                        }
                        ResetTimer(100);
                        return;
                    }
                }
                catch (Exception exx)
                {
                    MainConsole.Instance.Error("[AssetDataPlugin] Background task retiring asset", exx);
                }
            }
            ResetTimer(15000);
        }

        private int TaskGetHashCodeUseCount(string hash_code)
        {
            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where["hash_code"] = hash_code;
            QueryFilter filter = new QueryFilter
            {
                andFilters = where
            };

            string[] tables = {
                "auroraassets_1",
                "auroraassets_2",
                "auroraassets_3",
                "auroraassets_4",
                "auroraassets_5",
                "auroraassets_6",
                "auroraassets_7",
                "auroraassets_8",
                "auroraassets_9",
                "auroraassets_0",
                "auroraassets_a",
                "auroraassets_b",
                "auroraassets_c",
                "auroraassets_d",
                "auroraassets_e",
                "auroraassets_f",
                "auroraassets_old"
            };

            try
            {
                foreach (string table in tables)
                {
                    if (m_Gd.Query(new string[] { "id" }, table, filter, null, null, null).Count >= 1)
                    {
                        return 1;
                    }
                }
                return 0;
            }
            catch
            {
                // because this function checks to see if a asset file is being used, and deletes it if not,
                // I am saying it is being used if there is a error
                return 1;
            }
        }

        #endregion
    }
}