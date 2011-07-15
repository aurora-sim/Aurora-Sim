using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
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
        private readonly List<char> m_InvalidChars = new List<char>();
        private string m_CacheDirectory = "./BlackHoleAssets";
        private string m_CacheDirectoryBackup = "./BlackHoleBackup";
        private const int m_CacheDirectoryTiers = 3;
        private const int m_CacheDirectoryTierLen = 1;

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
            genericData.ConnectToDatabase(defaultConnectionString, "AuroraAsset", true);

            m_InvalidChars.AddRange(Path.GetInvalidPathChars());
            m_InvalidChars.AddRange(Path.GetInvalidFileNameChars());

            List<string> currentAssestVersion = genericData.Query("", "auroraassets_manual_migration", "version");

            if (currentAssestVersion.Count == 0)
            {
                new LocalAssetBlackholeManualMigration(genericData, m_CacheDirectory, m_CacheDirectoryBackup).Run();
            }
            if (m_Enabled)
                DataManager.DataManager.RegisterPlugin(Name, this);
        }

        #endregion

        #region Implementation of IAssetDataPlugin

        public AssetBase GetAsset(UUID uuid)
        {
            string databaseTable = "auroraassets_" + uuid.ToString().Substring(0, 1);
            IDataReader dr = null;
            AssetBase asset = null;
            try
            {
                dr = m_Gd.QueryData("WHERE id = '" + uuid + "'", databaseTable,
                                    "id, hash_code, parent_id, creator_id, name, description, assetType, create_time, access_time, asset_flags, owner_id, host_uri");
                if (dr != null)
                {
                    while (dr.Read())
                    {
                        asset = new AssetBase(dr["id"].ToString(), dr["name"].ToString(),
                                              (AssetType)int.Parse(dr["asset_type"].ToString()),
                                              UUID.Parse(dr["creator_id"].ToString()))
                        {
                            CreationDate = UnixTimeStampToDateTime(int.Parse(dr["create_time"].ToString())),
                            DatabaseTable = databaseTable,
                            Description = dr["description"].ToString(),
                            Flags = (AssetFlags)int.Parse(dr["asset_flags"].ToString()),
                            HashCode = dr["hash_code"].ToString(),
                            HostUri = dr["host_uri"].ToString(),
                            LastAccessed = UnixTimeStampToDateTime(int.Parse(dr["access_time"].ToString())),
                            OwnerID = UUID.Parse(dr["owner_id"].ToString()),
                            ParentID = UUID.Parse(dr["parent_id"].ToString()),
                            MetaOnly = false,
                            Data = LoadFile(dr["hash_code"].ToString())
                        };
                    }
                    dr.Close();
                    dr = null;
                }
                else
                {
                    m_Log.Warn("[LocalAssetBlackholeConnector] GetAsset(" + uuid + "); Unable to find asset " + uuid);
                }
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

        public AssetBase GetMeta(UUID uuid)
        {
            string databaseTable = "auroraassets_" + uuid.ToString().Substring(0, 1);
            IDataReader dr = null;
            AssetBase asset = null;
            try
            {
                dr = m_Gd.QueryData("WHERE id = '" + uuid + "'", databaseTable,
                                    "id, hash_code, parent_id, creator_id, name, description, assetType, create_time, access_time, asset_flags, owner_id, host_uri");
                if (dr != null)
                {
                    while (dr.Read())
                    {
                        asset = new AssetBase(dr["id"].ToString(), dr["name"].ToString(),
                                              (AssetType)int.Parse(dr["asset_type"].ToString()),
                                              UUID.Parse(dr["creator_id"].ToString()))
                        {
                            CreationDate = UnixTimeStampToDateTime(int.Parse(dr["create_time"].ToString())),
                            DatabaseTable = databaseTable,
                            Description = dr["description"].ToString(),
                            Flags = (AssetFlags)int.Parse(dr["asset_flags"].ToString()),
                            HashCode = dr["hash_code"].ToString(),
                            HostUri = dr["host_uri"].ToString(),
                            LastAccessed = UnixTimeStampToDateTime(int.Parse(dr["access_time"].ToString())),
                            OwnerID = UUID.Parse(dr["owner_id"].ToString()),
                            ParentID = UUID.Parse(dr["parent_id"].ToString()),
                            MetaOnly = true
                        };
                    }
                    dr.Close();
                    dr = null;
                }
                else
                {
                    m_Log.Warn("[LocalAssetBlackholeConnector] GetAsset(" + uuid + "); Unable to find asset " + uuid);
                }
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

        public void StoreAsset(AssetBase asset)
        {
            try
            {
                string database = "auroraassets_" + asset.ID.ToString().Substring(0, 1);
                if (asset.Name.Length > 63) asset.Name = asset.Name.Substring(0, 63);
                if (asset.Description.Length > 128) asset.Description = asset.Description.Substring(0, 128);
                asset = WriteFile(asset);
                Delete(asset.ID);
                m_Gd.Insert(database,
                             new[]
                                 {
                                     "id", "hash_code", "parent_id", "creator_id", "name", "description", "assetType",
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
            }
            catch (Exception e)
            {
                m_Log.Error("[AssetDataPlugin]: StoreAsset(" + asset.ID + ")", e);
            }
        }

        public void UpdateContent(UUID id, byte[] assetdata)
        {
            m_Gd.Update("auroraassets_" + id.ToString().ToCharArray()[0], new object[] {WriteFile(id, assetdata, 0)},
                        new[] {"hash_code"}, new[] {"id"}, new object[] {id});
        }

        public bool ExistsAsset(UUID uuid)
        {
            try
            {
                return m_Gd.Query("id", uuid, "assets", "id").Count > 0;
            }
            catch (Exception e)
            {
                m_Log.ErrorFormat(
                    "[ASSETS DB]: MySql failure fetching asset {0}" + Environment.NewLine + e, uuid);
            }
            return false;
        }

        public void Initialise(string connect)
        {

        }

        public bool Delete(UUID id)
        {
            try
            {
                return m_Gd.Delete("assets", "id = '" + id + "'");
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

        #region File Management

        public AssetBase WriteFile(AssetBase asset)
        {
            asset.HashCode = WriteFile(asset.ID, asset.Data, 0);
            return asset;
        }

        public string WriteFile(UUID assetid, byte[] data, int loop)
        {
            string hashCode;
            if (loop == 0)
                hashCode = Convert.ToBase64String(new SHA256Managed().ComputeHash(data)).Substring(0, 36);
            else
                hashCode =
                    Convert.ToBase64String(
                        new SHA256Managed().ComputeHash(Combine(new[] {data, new[] {byte.Parse(loop.ToString())}}))).
                        Substring(0, 36);

            string filename = GetFileName(hashCode, false);
            string directory = Path.GetDirectoryName(filename) + "";
            bool alreadyWriten = false;
            Stream stream = null;
            try
            {
                BinaryFormatter bformatter = new BinaryFormatter();
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                if ((File.Exists(filename)) && (!ByteArraysEqual(LoadFile(hashCode), data)))
                    return WriteFile(assetid, data, loop + 1);
                else if (File.Exists(filename))
                    alreadyWriten = true;
                
                if (!alreadyWriten)
                {
                    stream = File.Open(filename, FileMode.Create);
                    bformatter.Serialize(stream, data);
                    stream.Close();
                    stream = null;
                    string filenameForBackup = GetFileName(hashCode, true);
                    directory = Path.GetDirectoryName(filenameForBackup) + "";
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                    if (!File.Exists(filenameForBackup))
                    {
                        stream = File.Open(filenameForBackup, FileMode.Create);
                        bformatter.Serialize(stream, data);
                        stream.Close();
                        stream = null;
                    }
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
    }
}
