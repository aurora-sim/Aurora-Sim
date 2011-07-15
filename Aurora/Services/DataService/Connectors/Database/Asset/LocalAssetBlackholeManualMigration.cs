using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading;
using Aurora.Framework;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.Services.DataService.Connectors.Database.Asset
{
    class LocalAssetBlackholeManualMigration
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private AuroraThreadPool m_CmdThreadpool;
        readonly Stopwatch m_StartTime = new Stopwatch();
        private readonly IGenericData m_Gd;

        private readonly List<char> m_InvalidChars = new List<char>();
        private const int m_CacheDirectoryTiers = 3;
        private const int m_CacheDirectoryTierLen = 1;

        private readonly string m_CacheDirectory = "C:\\blackhole";
        private readonly string m_CacheDirectoryBackup = "C:\\blackhole\\backup";

        private static long _mSavedSize;
        private static int _mSavedFiles;
        private int m_TotalFiles;
        private int m_TotalFilesDone;

        public LocalAssetBlackholeManualMigration(IGenericData genericData, string cacheDirectory, string cacheDirectoryBackup)
        {
            m_CacheDirectory = cacheDirectory;
            m_CacheDirectoryBackup = cacheDirectoryBackup;
            m_Gd = genericData;
            m_InvalidChars.AddRange(Path.GetInvalidPathChars());
            m_InvalidChars.AddRange(Path.GetInvalidFileNameChars());
        }

        public void Run()
        {
            m_Log.Warn("Starting Conversion of for BlackHole Assests, this can take a long time.");
            AuroraThreadPoolStartInfo info = new AuroraThreadPoolStartInfo
            {
                priority = ThreadPriority.Normal,
                Threads = 40,
                MaxSleepTime = 100,
                SleepIncrementTime = 1,
                Name = "Asset conversion thread"
            };
            m_CmdThreadpool = new AuroraThreadPool(info);
            m_StartTime.Start();
            Migrate();
        }

        private void Migrate()
        {

            int count;
            bool hitend = false;
            for (int i = 0; i <= 500; i++)
            {
                System.Data.IDataReader dr = m_Gd.QueryData("LIMIT " + i + ",1", "assets", "id, name, description, assetType, local, temporary, asset_flags, CreatorID, create_time, data");
                try
                {
                    if (dr != null)
                    {
                        while (dr.Read())
                        {
                            AssetBase asset = new AssetBase(dr["id"].ToString(), dr["name"].ToString(), (AssetType)int.Parse(dr["assetType"].ToString()), UUID.Parse(dr["CreatorID"].ToString()))
                            {
                                CreatorID = UUID.Parse(dr["CreatorID"].ToString()),
                                Flags = (AssetFlags)int.Parse(dr["asset_flags"].ToString()),
                                Data = (Byte[])dr["data"],
                                HashCode = Convert.ToBase64String(new SHA256Managed().ComputeHash((Byte[])dr["data"])).Substring(0, 36),
                                Description = dr["description"].ToString(),
                                CreationDate = UnixTimeStampToDateTime(int.Parse(dr["create_time"].ToString())),
                                LastAccessed = UnixTimeStampToDateTime(int.Parse(dr["create_time"].ToString())),
                                DatabaseTable = "auroraassets_" + dr["id"].ToString().Substring(0, 1)
                            };

                            asset = WriteFile(asset, 0);
                            //MigrationSaveAsset(asset);
                            m_CmdThreadpool.QueueEvent(() => MigrationSaveAsset(asset), 1);

                            m_TotalFiles++;
                        }
                        dr.Close();
                        dr = null;
                    }
                    else
                    {
                        if (i == 0) hitend = true;
                        i = 1001;
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


                count = m_TotalFiles - m_TotalFilesDone;
                while (80 <= count)
                {
                    Thread.Sleep(1000);
                    count = m_TotalFiles - m_TotalFilesDone;
                    DisplayMigrationInfo();
                }
            }

            // Wait for the threads to finish if we are done.
            if (hitend)
            {
                int thisloopcount = 0;
                count = m_TotalFiles - m_TotalFilesDone;
                while (1 <= count)
                {
                    thisloopcount++;
                    Thread.Sleep(1000);
                    count = m_TotalFiles - m_TotalFilesDone;
                    if (thisloopcount == 300)
                    {
                        //Its been 5min.. and not down to 0.. not sure why.. 
                        count = 0;
                    }
                    DisplayMigrationInfo();
                }
                List<string> currentAssestVersion = m_Gd.Query("", "assets", "id");
                if (currentAssestVersion.Count >= 1)
                    hitend = false;
                else
                    m_Gd.Insert("auroraassets_manual_migration", new object[] { 1 });
            }

            if (!hitend) Migrate();
        }

        private void MigrationSaveAsset(AssetBase asset)
        {
            try
            {
                List<string> check1 = m_Gd.Query(
                    "hash_code = '" + asset.HashCode + "' and creator_id = '" + asset.CreatorID + "' and id != '" +
                    asset.ID + "'", "auroraassets_temp", "id");
                if ((check1 != null) && (check1.Count == 0))
                {
                    asset.ParentID = asset.ID;
                    m_Gd.Insert("auroraassets_temp", new[] { "id", "hash_code", "creator_id" },
                                new object[] { asset.ID, asset.HashCode, asset.CreatorID });
                }
                else if (check1 != null) asset.ParentID = new UUID(check1[0]);

                m_Gd.Replace(asset.DatabaseTable,
                             new[]
                                 {
                                     "id", "hash_code", "parent_id", "creator_id", "name", "description", "asset_type",
                                     "create_time", "access_time", "asset_flags",
                                     "owner_id", "host_uri"
                                 },
                             new object[]
                                 {
                                     asset.ID, asset.HashCode, (asset.ID == asset.ParentID) ? "":asset.ParentID.ToString(), asset.CreatorID, asset.Name,
                                     asset.Description, (int) asset.TypeAsset,
                                     Util.ToUnixTime(asset.CreationDate), asset.LastAccessed
                                     , (int) asset.Flags, asset.OwnerID, asset.HostUri
                                 });

                if (asset.ParentID != asset.ID)
                    m_Gd.Update("inventoryitems", new object[] { asset.ParentID }, new[] { "assetID" },
                                new[] { "assetID" }, new object[] { asset.ID });

                m_Gd.Delete("assets", "id = '" + asset.ID + "'");
                m_TotalFilesDone++;

            }
            catch (Exception e)
            {
                m_Log.Error("MigrationSaveAsset", e);
            }
        }

        #region File Management

        public AssetBase WriteFile(AssetBase asset)
        {
            return WriteFile(asset, 0);
        }

        public AssetBase WriteFile(AssetBase asset, int loop)
        {
            string filename = GetFileName(asset.HashCode, false);
            string directory = Path.GetDirectoryName(filename) + "";
            bool alreadyWriten = false;
            Stream stream = null;
            try
            {
                BinaryFormatter bformatter = new BinaryFormatter();
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                if (File.Exists(filename))
                {
                    if (!ByteArraysEqual(LoadFile(asset.HashCode), asset.Data))
                    {
                        asset.HashCode = Convert.ToBase64String(
                            new SHA256Managed().ComputeHash(
                                Combine(new[] { asset.Data, new[] { byte.Parse(loop.ToString()) } }))).Substring(0, 36);
                        return WriteFile(asset, loop + 1);
                    }
                    _mSavedSize += asset.Data.Length;
                    _mSavedFiles++;
                    alreadyWriten = true;
                }
                if (!alreadyWriten)
                {
                    stream = File.Open(filename, FileMode.Create);
                    bformatter.Serialize(stream, asset.Data);
                    stream.Close();
                    stream = null;
                    string filenameForBackup = GetFileName(asset.HashCode, true);
                    directory = Path.GetDirectoryName(filenameForBackup) + "";
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                    if (!File.Exists(filenameForBackup))
                    {
                        stream = File.Open(filenameForBackup, FileMode.Create);
                        bformatter.Serialize(stream, asset.Data);
                        stream.Close();
                        stream = null;
                    }
                }
            }
            catch (Exception e)
            {
                m_Log.Error("[AssetDataPlugin]: WriteFile(" + asset.ID + ")", e);
                if (stream != null)
                    stream.Close();
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return asset;
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
                File.Move(backupfile, file);
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
                string pathPart = id.Substring((p - 1) * m_CacheDirectoryTierLen, m_CacheDirectoryTierLen);
                path = Path.Combine(path, pathPart);
            }

            return Path.Combine(path, id + ".ass");
        }

        #endregion

        #region util functions

        private void DisplayMigrationInfo()
        {
            try
            {
                Console.Clear();
                Console.Write("Black Hole Asset Conversion\n" + ((m_TotalFilesDone >= 1) ? m_TotalFilesDone / m_StartTime.Elapsed.TotalSeconds : 0) +
                              " per second is being converte\nThis Group " + m_TotalFilesDone + " of " +
                              m_TotalFiles + "\nDupes Found " + _mSavedFiles + "\nSized Saved " +
                              ((_mSavedSize >= 1) ? ((_mSavedSize / 1024f) / 1024f) : 0) + " MB");
            }
            catch (Exception e)
            {
                m_Log.Error("Error", e);
            }
        }

        public static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        private static byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        public bool ByteArraysEqual(byte[] b1, byte[] b2)
        {
            if (b1 == null || b2 == null) return false;
            if (b1.Length != b2.Length) return false;
            return (b1.SequenceEqual(b2));
        }

        #endregion
    }


}
