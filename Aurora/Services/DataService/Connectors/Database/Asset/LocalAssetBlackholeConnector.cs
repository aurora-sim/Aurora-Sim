using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Aurora.Framework;
using log4net;
using OpenMetaverse;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using System.Security.Cryptography;

namespace Aurora.Services.DataService.Connectors.Database.Asset
{
    public class LocalAssetBlackholeConnector : IAssetDataPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IGenericData m_Gd;
        private bool m_Enabled;
        private bool needsConversion;
        private readonly List<char> m_InvalidChars = new List<char>();
        private string m_CacheDirectory = "./BlackHoleAssets";
        private string m_CacheDirectoryBackup = "./BlackHoleBackup";
        private const int m_CacheDirectoryTiers = 3;
        private const int m_CacheDirectoryTierLen = 1;
        private readonly System.Timers.Timer taskTimer = new System.Timers.Timer();

        private AuroraThreadPool m_CmdThreadpool;

        private int convertCount;
        private int convertCountDupe;
        private int convertCountParentFix;
        private int migrationTaskCount;
        private int migrationTaskCountOn;
        private int displayCount;
        Stopwatch sw = new Stopwatch();

        #region Implementation of IAuroraDataPlugin

        public string Name
        {
            get { return "IAssetDataPlugin"; }
        }

        public void Initialize(IGenericData genericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AssetConnector", "LocalConnector") != "LocalConnectorBackHole")
                return;
            m_Gd = genericData;
            m_Enabled = true;

            if (source.Configs["Handlers"].GetString("AssetHandler", "") != "AssetService")
                return;

            m_CacheDirectory = source.Configs["BlackHole"].GetString("CacheDirector", m_CacheDirectory);
            m_CacheDirectoryBackup = source.Configs["BlackHole"].GetString("BackupCacheDirector", m_CacheDirectoryBackup);

            m_Enabled = true;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);
            genericData.ConnectToDatabase(defaultConnectionString, "BlackholeAsset", true);

            m_InvalidChars.AddRange(Path.GetInvalidPathChars());
            m_InvalidChars.AddRange(Path.GetInvalidFileNameChars());

            if (m_Enabled)
            {
                DataManager.DataManager.RegisterPlugin(Name, this);
                needsConversion = (m_Gd.Query(" 1 = 1 LIMIT 1 ", "assets", "id").Count >= 1);
                convertCount = 0;
                taskTimer.Interval = 60000;
                taskTimer.Elapsed += t_Elapsed;
                taskTimer.Start();

                if (needsConversion)
                {
                    AuroraThreadPoolStartInfo info = new AuroraThreadPoolStartInfo
                    {
                        priority = ThreadPriority.Normal,
                        Threads = 40,
                        MaxSleepTime = 100,
                        SleepIncrementTime = 1,
                        Name = "Asset conversion thread"
                    };
                    m_CmdThreadpool = new AuroraThreadPool(info);
                }

            }
        }



        #endregion

        #region Implementation of IAssetDataPlugin

        #region GetAsset

        public AssetBase GetAsset(UUID uuid)
        {
            return GetAsset(uuid, false);
        }

        public AssetBase GetMeta(UUID uuid)
        {
            return GetAsset(uuid, true);
        }

        public AssetBase GetAsset(UUID uuid, bool metaOnly)
        {
            ResetTimer(1);
            string databaseTable = "auroraassets_" + uuid.ToString().Substring(0, 1);
            IDataReader dr = null;
            AssetBase asset;
            try
            {
                dr = m_Gd.QueryData("WHERE id = '" + uuid + "' LIMIT 1", databaseTable,
                                    "id, hash_code, parent_id, creator_id, name, description, asset_type, create_time, access_time, asset_flags, owner_id, host_uri");
                asset = LoadAssetFromDR(dr);
                if (asset == null)
                {
                    databaseTable = "auroraassets_old";
                    dr = m_Gd.QueryData("WHERE id = '" + uuid + "' LIMIT 1", databaseTable,
                                    "id, hash_code, parent_id, creator_id, name, description, asset_type, create_time, access_time, asset_flags, owner_id, host_uri");
                    asset = LoadAssetFromDR(dr);
                    if (asset != null) StoreAsset(asset);
                }
                if (asset == null)
                {
                    if (needsConversion)
                    {
                        asset = Convert2BH(uuid);
                        if (asset == null)
                            m_Log.Warn("[LocalAssetBlackholeConnector] GetAsset(" + uuid + "); Unable to find asset " + uuid);
                        else
                        {
                            if (metaOnly) asset.Data = new byte[] { };
                            asset.MetaOnly = metaOnly;
                        }
                    }
                    else
                        m_Log.Warn("[LocalAssetBlackholeConnector] GetAsset(" + uuid + "); Unable to find asset " + uuid);
                }
                else if (!metaOnly)
                {
                    asset.Data = LoadFile(asset.HashCode);
                    asset.MetaOnly = false;
                }
                else asset.MetaOnly = true;
                m_Gd.Update(databaseTable, new object[] { Util.ToUnixTime(DateTime.UtcNow) }, new[] { "access_time" }, new[] { "id" }, new object[] { uuid });
            }
            catch (Exception e)
            {
                m_Log.Error("[LocalAssetBlackholeConnector] GetAsset(" + uuid + "); Error ", e);
                throw;
            }
            finally
            {
                if (dr != null) dr.Close();
            }
            return asset;
        }

        private AssetBase LoadAssetFromDR(IDataReader dr)
        {
            try
            {
                if (dr != null)
                {
                    while (dr.Read())
                    {
                        return new AssetBase(UUID.Parse(dr["id"].ToString()), dr["name"].ToString(),
                                              (AssetType)int.Parse(dr["asset_type"].ToString()),
                                              UUID.Parse(dr["creator_id"].ToString()))
                        {
                            CreationDate = UnixTimeStampToDateTime(int.Parse(dr["create_time"].ToString())),
                            DatabaseTable = "auroraassets_" + dr["id"].ToString().Substring(0, 1),
                            Description = dr["description"].ToString(),
                            Flags = (AssetFlags)int.Parse(dr["asset_flags"].ToString()),
                            HashCode = dr["hash_code"].ToString(),
                            HostUri = dr["host_uri"].ToString(),
                            LastAccessed = DateTime.UtcNow,
                            OwnerID = UUID.Parse(dr["owner_id"].ToString()),
                            ParentID = UUID.Parse(dr["parent_id"].ToString())
                        };
                    }
                }
            }
            catch (Exception e)
            {
                m_Log.Error("[LocalAssetBlackholeConnector] LoadAssetFromDR(); Error Loading", e);

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
        public bool StoreAsset(AssetBase asset)
        {
            try
            {
                string database = "auroraassets_" + asset.ID.ToString().Substring(0, 1);
                if (asset.Name.Length > 63) asset.Name = asset.Name.Substring(0, 63);
                if (asset.Description.Length > 128) asset.Description = asset.Description.Substring(0, 128);
                string newHash = WriteFile(asset.ID, asset.Data);
                if (asset.HashCode != newHash)
                {
                    m_Gd.Insert("auroraassets_tasks", new[] { "id", "task_type", "task_values" }, new object[] { UUID.Random(), "HASHCHECK", asset.HashCode });
                }
                asset.HashCode = newHash;
                Delete(asset.ID);
                m_Gd.Insert(database,
                            new[]
                                {
                                    "id", "hash_code", "parent_id", "creator_id", "name", "description", "asset_type",
                                    "create_time", "access_time", "asset_flags",
                                    "owner_id", "host_uri"
                                },
                            new object[]
                                {
                                    asset.ID, asset.HashCode,
                                    (asset.ID == asset.ParentID) ? ""
                                        : (UUID.Zero == asset.ParentID) ? "" : asset.ParentID.ToString(),
                                    (asset.CreatorID == UUID.Zero) ? "" : asset.CreatorID.ToString(), asset.Name,
                                    asset.Description, (int) asset.TypeAsset,
                                    Util.ToUnixTime(asset.CreationDate), Util.ToUnixTime(DateTime.UtcNow)
                                    , (int) asset.Flags, (asset.OwnerID == UUID.Zero) ? "" : asset.OwnerID.ToString(),
                                    asset.HostUri
                                });

                // Double checked its saved. Just for debug
                if (needsConversion)
                {
                    if (m_Gd.Query("id", asset.ID, "auroraassets_" + asset.ID.ToString().Substring(0, 1), "id").Count ==
                        0)
                    {
                        m_Log.Error("[AssetDataPlugin] Asset did not saver propery: " + asset.ID);
                        return false;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                m_Log.Error("[AssetDataPlugin]: StoreAsset(" + asset.ID + ")", e);
            }
            return false;
        }

        public void UpdateContent(UUID id, byte[] assetdata)
        {
            ResetTimer(1);
            string newHash = WriteFile(id, assetdata);
            List<string> hashCodeCheck = m_Gd.Query("id", id, "auroraassets_" + id.ToString().ToCharArray()[0],
                                                    "hash_code");
            if (hashCodeCheck.Count >= 1)
            {
                if (hashCodeCheck[0] != newHash)
                {
                    m_Gd.Insert("auroraassets_tasks", new[] { "id", "task_type", "task_values" },
                                new object[] { UUID.Random(), "HASHCHECK", hashCodeCheck[0] });
                    m_Gd.Update("auroraassets_" + id.ToString().ToCharArray()[0], new object[] { newHash },
                        new[] { "hash_code" }, new[] { "id" }, new object[] { id });
                }
            }
        }
        #endregion

        #region asset exists

        public bool ExistsAsset(UUID uuid)
        {
            ResetTimer(1);
            try
            {
                bool result = m_Gd.Query("id", uuid, "auroraassets_" + uuid.ToString().Substring(0, 1), "id").Count > 0;
                if (!result)
                    result = m_Gd.Query("id", uuid, "auroraassets_old", "id").Count > 0;
                if (!result)
                {
                    AssetBase a = Convert2BH(uuid);
                    return a != null;
                }
                return result;
            }
            catch (Exception e)
            {
                m_Log.ErrorFormat(
                    "[ASSETS DB]: MySql failure fetching asset {0}" + Environment.NewLine + e, uuid);
            }
            return false;
        }

        #endregion

        public void Initialise(string connect)
        {

        }

        public bool Delete(UUID id)
        {
            ResetTimer(1);
            try
            {
                if (!m_Gd.Delete("auroraassets_" + id.ToString().Substring(0, 1), "id = '" + id + "'"))
                    m_Gd.Delete("auroraassets_old", "id = '" + id + "'");
                return true;
            }
            catch (Exception e)
            {
                m_Log.Error("[AssetDataPlugin] Delete - Error", e);
                return false;
            }
        }

        #endregion

        #region util functions

        public static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public bool ByteArraysEqual(byte[] b1, byte[] b2)
        {
            if (b1 == null || b2 == null) return false;
            if (b1.Length != b2.Length) return false;
            return (b1.SequenceEqual(b2));
        }

        private AssetBase Convert2BH(UUID uuid)
        {
            IDataReader dr = m_Gd.QueryData("WHERE id = '" + uuid + "' LIMIT 1", "assets", "id, name, description, assetType, local, temporary, asset_flags, CreatorID, create_time, data");
            AssetBase asset = null;
            try
            {
                if (dr != null)
                {
                    while (dr.Read())
                    {
                        asset = new AssetBase(dr["id"].ToString(), dr["name"].ToString(), (AssetType)int.Parse(dr["assetType"].ToString()), UUID.Parse(dr["CreatorID"].ToString()))
                        {
                            CreatorID = UUID.Parse(dr["CreatorID"].ToString()),
                            Flags = (AssetFlags)int.Parse(dr["asset_flags"].ToString()),
                            Data = (Byte[])dr["data"],
                            Description = dr["description"].ToString(),
                            CreationDate = UnixTimeStampToDateTime(int.Parse(dr["create_time"].ToString())),
                            LastAccessed = DateTime.Now,
                            DatabaseTable = "auroraassets_" + dr["id"].ToString().Substring(0, 1)
                        };

                        if (dr["local"].ToString().Equals("1") || dr["local"].ToString().Equals("true", StringComparison.InvariantCultureIgnoreCase))
                            asset.Flags |= AssetFlags.Local;
                        if (bool.Parse(dr["temporary"].ToString())) asset.Flags |= AssetFlags.Temperary;

                        if (File.Exists(GetFileName(Convert.ToBase64String(new SHA256Managed().ComputeHash(asset.Data)) + asset.Data.Length, false)))
                            convertCountDupe++;

                        asset.HashCode = WriteFile(asset.ID, asset.Data);

                        List<string> check1 = m_Gd.Query(
                            "hash_code = '" + asset.HashCode + "' and creator_id = '" + asset.CreatorID +
                            "'", "auroraassets_temp", "id");
                        if ((check1 != null) && (check1.Count == 0))
                        {
                            m_Gd.Insert("auroraassets_temp", new[] { "id", "hash_code", "creator_id" },
                                        new object[] { asset.ID, asset.HashCode, asset.CreatorID });
                        }
                        else if ((check1 != null) && (check1[0] != asset.ID.ToString()))
                        {
                            convertCountParentFix++;
                            asset.ParentID = new UUID(check1[0]);

                            m_Gd.Update("inventoryitems", new object[] { asset.ParentID }, new[] { "assetID" },
                                new[] { "assetID" }, new object[] { asset.ID });
                        }
                        else asset.ParentID = asset.ID;

                        if (StoreAsset(asset)) m_Gd.Delete("assets", "id = '" + asset.ID + "'");
                        convertCount++;
                        migrationTaskCountOn++;
                    }
                    dr.Close();
                    dr = null;
                }
            }
            catch (Exception e)
            {
                m_Log.Error("[LocalAssetBlackholeManualMigration] Migrate Error", e);
            }
            finally
            {
                if (dr != null) dr.Close();
            }
            return asset;
        }

        #endregion

        #region File Management

        public string WriteFile(UUID assetid, byte[] data, int tryCount)
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
                    catch (System.IO.IOException e)
                    {
                        if (stream != null) stream.Close();
                        stream = null;
                        if (tryCount <= 2)
                        {
                            Thread.Sleep(4000);
                            WriteFile(assetid, data, tryCount++);
                        }
                        else
                        {
                            m_Log.Error("[AssetDataPlugin] Error writing Asset File " + assetid, e);
                        }
                    }

                    stream = null;
                    string filenameForBackup = GetFileName(hashCode, true) + ".7z";
                    directory = Path.GetDirectoryName(filenameForBackup);
                    if (directory != null && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
                    if (!File.Exists(filenameForBackup))
                        Util.Compress7ZipFile(filename, filenameForBackup);
                }
            }
            catch (Exception e)
            {
                m_Log.Error("[AssetDataPlugin]: WriteFile(" + assetid + ")", e);
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return hashCode;
        }

        public string WriteFile(UUID assetid, byte[] data)
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
            string filename = GetFileName(hashCode, false);
            try
            {
                stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                return (Byte[])bformatter.Deserialize(stream);
            }
            catch
            {
                if (stream != null) stream.Close();
                stream = null;
                if (!waserror)
                {
                    RestoreBackup(hashCode);
                    return LoadFile(hashCode, true);
                }
                return null;
            }
            finally
            {
                if (stream != null) stream.Close();
            }
        }

        private void RestoreBackup(string hashCode)
        {
            string backupfile = GetFileName(hashCode, true);
            string file = GetFileName(hashCode, false);
            if (File.Exists(backupfile))
            {
                File.Move(file, file + ".corrupt");
                Util.UnCompress7ZipFile(backupfile + ".7z", Path.GetDirectoryName(file));
                m_Log.Info("[AssetDataPlugin]: Restored backup asset file " + file);
            }
        }

        /// <summary>
        /// Determines the filename for an AssetID stored in the file cache
        /// </summary>
        /// <param name="id"></param>
        /// <param name="backup"></param>
        /// <returns></returns>
        private string GetFileName(string id, bool backup)
        {
            // Would it be faster to just hash the darn thing?
            id = m_InvalidChars.Aggregate(id, (current, c) => current.Replace(c, '_'));

            string path = (backup) ? m_CacheDirectoryBackup : m_CacheDirectory;
            for (int p = 1; p <= m_CacheDirectoryTiers; p++)
            {
                string pathPart = id.Substring(0, m_CacheDirectoryTierLen);
                path = Path.Combine(path, pathPart);
                id = id.Substring(1);
            }

            return Path.Combine(path, id + ".ass");
        }

        #endregion

        #region Timer

        private void ResetTimer(int typeOfReset)
        {
            taskTimer.Stop();
            if (typeOfReset == 1)
                taskTimer.Interval = 60000;
            else if (typeOfReset == 2)
                taskTimer.Interval = 50;
            else
                taskTimer.Interval = 2000;
            taskTimer.Start();
        }

        private void t_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            taskTimer.Stop();
            if (needsConversion)
            {
                if (!sw.IsRunning) sw.Start();
                displayCount++;
                List<string> toConvert = m_Gd.Query(" 1 = 1 LIMIT 5 ", "assets", "id");
                if (toConvert.Count >= 1)
                {
                    foreach (string assetkey in toConvert)
                        Convert2BH(UUID.Parse(assetkey));
                }
                if (displayCount == 100)
                {
                    sw.Stop();
                    m_Log.Info("[Blackhole Assets] Converted:" + convertCount + " DupeContent:" + convertCountDupe +
                               " Dupe4Creator:" + convertCountParentFix);
                    m_Log.Info("[Blackhole Assets] 500 in " + sw.Elapsed.Minutes + ":" + sw.Elapsed.Seconds);
                    displayCount = 0;
                    sw.Reset();
                    sw.Start();
                }

                ResetTimer(2);
                return;
            }
            List<string> taskCheck = m_Gd.Query(" 1 = 1 LIMIT 1 ", "auroraassets_tasks",
                                                "id, task_type, task_values");
            if (taskCheck.Count == 1)
            {
                string task_id = taskCheck[0];
                string task_type = taskCheck[1];
                string task_value = taskCheck[2];

                if (task_type == "HASHCHECK")
                {
                    int result =
                        m_Gd.Query("hash_code", task_value, "auroraassets_old", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_9", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_8", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_7", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_6", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_5", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_4", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_3", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_2", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_1", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_0", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_f", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_e", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_d", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_c", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_b", "id").Count +
                        m_Gd.Query("hash_code", task_value, "auroraassets_a", "id").Count;
                    if (result == 0)
                    {
                        m_Log.Info("[AssetDataPlugin] Deleteing old asset files");
                        if (File.Exists(GetFileName(task_value, false)))
                            File.Delete(GetFileName(task_value, false));
                        if (File.Exists(GetFileName(task_value, true))) File.Delete(GetFileName(task_value, true));
                    }
                }
                m_Gd.Delete("auroraassets_tasks", new[] { "id" }, new object[] { task_id });
            }
            else
            {
                List<string> findOld2 = m_Gd.Query(" access_time < " + Util.ToUnixTime(DateTime.UtcNow.AddDays(-30)) + " LIMIT 1 ", "auroraassets_" + UUID.Random().ToString().ToCharArray()[0],
                                                   "id");
                foreach (string ass in findOld2)
                {
                    List<string> findOld = m_Gd.Query(" id = '" + ass + "'", "auroraassets_" + ass.ToCharArray()[0], "id,hash_code,name,description,asset_type,create_time,access_time,asset_flags,creator_id,owner_id,host_uri,parent_id");
                    m_Gd.Insert("auroraassets_old", new object[] { findOld[0], findOld[1], findOld[2], findOld[3], findOld[4], findOld[5], findOld[6], findOld[7], findOld[8], findOld[9], findOld[10], findOld[11] });
                    if (m_Gd.Query("id", ass, "auroraassets_old", "id").Count > 0)
                        m_Gd.Delete("auroraassets_" + ass.ToCharArray()[0], new[] { "id" }, new object[] { ass });
                }
            }
            ResetTimer(0);
        }

        #endregion
    }
}
