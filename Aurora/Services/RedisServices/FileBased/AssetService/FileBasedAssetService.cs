using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using System;
using System.IO;

namespace Aurora.FileBasedServices.AssetService
{
    public class AssetService : ConnectorBase, IAssetService, IService
    {
        #region Declares

        protected bool doDatabaseCaching = false;
        protected string m_connectionDNS = "localhost", m_connectionPassword = null;

        protected bool m_doConversion = false;
        protected IAssetDataPlugin m_assetService;
        protected string m_assetsDirectory = "C://assets/";
        protected bool m_enabled = false;

        #endregion

        #region IService Members

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AssetHandler", "") != "FileBased" + Name)
                return;
            m_enabled = true;
            Configure(config, registry);
            Init(registry, Name, serverPath: "/asset/");

            SetUpFileBase(Path.GetPathRoot(Environment.CurrentDirectory));

            IConfig assetConfig = config.Configs["AssetService"];
            if (assetConfig != null)
                m_doConversion = assetConfig.GetBoolean("DoConversion", true);
        }

        public virtual void Configure(IConfigSource config, IRegistryCore registry)
        {
            if (!m_enabled)
                return;
            m_registry = registry;

            registry.RegisterModuleInterface<IAssetService>(this);

            IConfig handlers = config.Configs["Handlers"];
            if (handlers != null)
                doDatabaseCaching = handlers.GetBoolean("AssetHandlerUseCache", false);

            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand("show digest",
                                                         "show digest <ID>",
                                                         "Show asset digest", HandleShowDigest);

                MainConsole.Instance.Commands.AddCommand("delete asset",
                                                         "delete asset <ID>",
                                                         "Delete asset from database", HandleDeleteAsset);

                MainConsole.Instance.Commands.AddCommand("get asset",
                                                         "get asset <ID>",
                                                         "Gets info about asset from database", HandleGetAsset);
            }
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_doConversion)
                m_assetService = Aurora.DataManager.DataManager.RequestPlugin<IAssetDataPlugin>();
        }

        public virtual void FinishedStartup()
        {
        }

        #endregion

        #region IAssetService Members

        public IAssetService InnerService
        {
            get { return this; }
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual AssetBase Get(string id)
        {
            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (doDatabaseCaching && cache != null)
            {
                bool found;
                AssetBase cachedAsset = cache.Get(id, out found);
                if (found && (cachedAsset == null || cachedAsset.Data.Length != 0))
                    return cachedAsset;
            }

            object remoteValue = DoRemoteByURL("AssetServerURI", id);
            if (remoteValue != null || m_doRemoteOnly)
            {
                if (doDatabaseCaching && cache != null)
                    cache.Cache(id, (AssetBase) remoteValue);
                return (AssetBase) remoteValue;
            }

            AssetBase asset = FileGetAsset(id);
            if (doDatabaseCaching && cache != null)
                cache.Cache(id, asset);
            return asset;
        }

        public virtual AssetBase GetMesh(string id)
        {
            return Get(id);
        }

        public virtual AssetBase GetCached(string id)
        {
            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (doDatabaseCaching && cache != null)
                return cache.Get(id);
            return null;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual byte[] GetData(string id)
        {
            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (doDatabaseCaching && cache != null)
            {
                bool found;
                byte[] cachedAsset = cache.GetData(id, out found);
                if (found)
                    return cachedAsset;
            }

            object remoteValue = DoRemoteByURL("AssetServerURI", id);
            if (remoteValue != null || m_doRemoteOnly)
            {
                byte[] data = (byte[]) remoteValue;
                if (doDatabaseCaching && cache != null)
                    cache.CacheData(id, data);
                return data;
            }

            AssetBase asset = FileGetAsset(id);
            if (doDatabaseCaching && cache != null)
                cache.Cache(id, asset);
            if (asset != null) return asset.Data;
            return new byte[0];
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual bool GetExists(string id)
        {
            object remoteValue = DoRemoteByURL("AssetServerURI", id);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool) remoteValue;

            return FileExistsAsset(id);
        }

        public virtual void Get(String id, Object sender, AssetRetrieved handler)
        {
            Util.FireAndForget((o) => { handler(id, sender, Get(id)); });
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual UUID Store(AssetBase asset)
        {
            if (asset == null)
                return UUID.Zero;

            object remoteValue = DoRemoteByURL("AssetServerURI", asset);
            if (remoteValue != null || m_doRemoteOnly)
            {
                if (remoteValue == null)
                    return UUID.Zero;
                asset.ID = (UUID) remoteValue;
            }
            else
                FileSetAsset(asset);

            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (doDatabaseCaching && cache != null && asset != null && asset.Data != null && asset.Data.Length != 0)
            {
                cache.Expire(asset.ID.ToString());
                cache.Cache(asset.ID.ToString(), asset);
            }

            return asset != null ? asset.ID : UUID.Zero;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual UUID UpdateContent(UUID id, byte[] data)
        {
            object remoteValue = DoRemoteByURL("AssetServerURI", id, data);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? UUID.Zero : (UUID) remoteValue;

            AssetBase asset = FileGetAsset(id.ToString());
            if (asset == null)
                return UUID.Zero;
            UUID newID = asset.ID = UUID.Random();
            asset.Data = data;
            bool success = FileSetAsset(asset);
            if (!success)
                return UUID.Zero; //We weren't able to update the asset
            return newID;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual bool Delete(UUID id)
        {
            object remoteValue = DoRemoteByURL("AssetServerURI", id);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool) remoteValue;

            FileDeleteAsset(id.ToString());
            return true;
        }

        #endregion

        private void SetUpFileBase(string path)
        {
            m_assetsDirectory = Path.Combine(path, "assets");
            if (!Directory.Exists(m_assetsDirectory))
                Directory.CreateDirectory(m_assetsDirectory);
            if (!Directory.Exists(Path.Combine(m_assetsDirectory, "data")))
                Directory.CreateDirectory(Path.Combine(m_assetsDirectory, "data"));

            MainConsole.Instance.InfoFormat("[FILE BASED ASSET SERVICE]: Set up File Based Assets in {0}.",
                                            m_assetsDirectory);
        }

        private string GetPathForID(string id)
        {
            string fileName = MakeValidFileName(id);
            string baseStr = m_assetsDirectory;
            for (int i = 0; i < 4; i++)
            {
                baseStr = Path.Combine(baseStr, fileName.Substring(i*2, 2));
                if (!Directory.Exists(baseStr))
                    Directory.CreateDirectory(baseStr);
            }
            return Path.Combine(baseStr, fileName + ".asset");
        }

        private string GetDataPathForID(string hashCode)
        {
            string fileName = MakeValidFileName(hashCode);
            string baseStr = Path.Combine(m_assetsDirectory, "data");
            for (int i = 0; i < 4; i++)
            {
                baseStr = Path.Combine(baseStr, fileName.Substring(i*2, 2));
                if (!Directory.Exists(baseStr))
                    Directory.CreateDirectory(baseStr);
            }
            return Path.Combine(baseStr, fileName + ".data");
        }

        private static string MakeValidFileName(string name)
        {
            string invalidChars =
                System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars())) + "+=";
            string invalidReStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return System.Text.RegularExpressions.Regex.Replace(name, invalidReStr, "");
        }

        private object _lock = new object();

        public AssetBase FileGetAsset(string id)
        {
            AssetBase asset = null;

#if DEBUG
            long startTime = System.Diagnostics.Stopwatch.GetTimestamp();
#endif
            try
            {
                if (!File.Exists(GetPathForID(id)))
                    return CheckForConversion(id);

                lock (_lock)
                {
                    FileStream openStream = File.OpenRead(GetPathForID(id));
                    asset = ProtoBuf.Serializer.Deserialize<AssetBase>(openStream);
                    openStream.Close();
                    asset.Data = File.ReadAllBytes(GetDataPathForID(asset.HashCode));
                }
            }
            catch
            {
            }
            finally
            {
#if DEBUG
                long endTime = System.Diagnostics.Stopwatch.GetTimestamp();
                if (MainConsole.Instance != null && asset != null)
                    MainConsole.Instance.Warn("[FILE BASED ASSET SERVICE]: Took " + (endTime - startTime)/10000 +
                                              " to get asset " + id + " sized " + asset.Data.Length/(1024) + "kbs");
#endif
            }
            return asset;
        }

        private AssetBase CheckForConversion(string id)
        {
            if (!m_doConversion)
                return null;

            AssetBase asset;
            asset = m_assetService.GetAsset(UUID.Parse(id));

            if (asset == null)
                return null;

            //Delete first, then restore it with the new local flag attached, so that we know we've converted it
            //m_assetService.Delete(asset.ID, true);
            //asset.Flags = AssetFlags.Local;
            //m_assetService.StoreAsset(asset);

            //Now store in Redis
            FileSetAsset(asset);

            return asset;
        }

        public bool FileExistsAsset(string id)
        {
            bool success = File.Exists(GetPathForID(id));
            if (!success)
                success = m_assetService.ExistsAsset(UUID.Parse(id));
            return success;
        }

        public bool FileSetAsset(AssetBase asset)
        {
            FileStream assetStream = null;
            try
            {
                string hash = asset.HashCode;
                bool duplicate = File.Exists(GetDataPathForID(hash));

                byte[] data = asset.Data;
                asset.Data = new byte[0];
                lock (_lock)
                {
                    assetStream = File.OpenWrite(GetPathForID(asset.IDString));
                    ProtoBuf.Serializer.Serialize<AssetBase>(assetStream, asset);
                    assetStream.Close();
                    asset.Data = data;

                    //Deduplication...
                    if (duplicate)
                    {
                        //Only set id --> asset, and not the hashcode --> data to deduplicate
                        return true;
                    }

                    File.WriteAllBytes(GetDataPathForID(hash), data);
                }
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                //if (assetStream != null)
                //    assetStream.Close();
            }
        }

        public void FileDeleteAsset(string id)
        {
            AssetBase asset = FileGetAsset(id);
            if (asset == null)
                return;
            File.Delete(GetPathForID(id));
            File.Delete(GetDataPathForID(asset.HashCode));
        }

        #region Console Commands

        private void HandleShowDigest(string[] args)
        {
            if (args.Length < 3)
            {
                MainConsole.Instance.Info("Syntax: show digest <ID>");
                return;
            }

            AssetBase asset = Get(args[2]);

            if (asset == null || asset.Data.Length == 0)
            {
                MainConsole.Instance.Info("Asset not found");
                return;
            }

            int i;

            MainConsole.Instance.Info(String.Format("Name: {0}", asset.Name));
            MainConsole.Instance.Info(String.Format("Description: {0}", asset.Description));
            MainConsole.Instance.Info(String.Format("Type: {0}", asset.TypeAsset));
            MainConsole.Instance.Info(String.Format("Content-type: {0}", asset.TypeAsset.ToString()));
            MainConsole.Instance.Info(String.Format("Flags: {0}", asset.Flags));

            for (i = 0; i < 5; i++)
            {
                int off = i*16;
                if (asset.Data.Length <= off)
                    break;
                int len = 16;
                if (asset.Data.Length < off + len)
                    len = asset.Data.Length - off;

                byte[] line = new byte[len];
                Array.Copy(asset.Data, off, line, 0, len);

                string text = BitConverter.ToString(line);
                MainConsole.Instance.Info(String.Format("{0:x4}: {1}", off, text));
            }
        }

        private void HandleDeleteAsset(string[] args)
        {
            if (args.Length < 3)
            {
                MainConsole.Instance.Info("Syntax: delete asset <ID>");
                return;
            }

            AssetBase asset = Get(args[2]);

            if (asset == null || asset.Data.Length == 0)
            {
                MainConsole.Instance.Info("Asset not found");
                return;
            }

            Delete(UUID.Parse(args[2]));

            MainConsole.Instance.Info("Asset deleted");
        }

        private void HandleGetAsset(string[] args)
        {
            if (args.Length < 3)
            {
                MainConsole.Instance.Info("Syntax: get asset <ID>");
                return;
            }

            AssetBase asset = FileGetAsset(args[2]);
            if (asset == null)
                asset = FileGetAsset(args[2]);

            if (asset == null || asset.Data.Length == 0)
            {
                MainConsole.Instance.Info("Asset not found");
                return;
            }

            MainConsole.Instance.Info("Asset found");
        }

        #endregion
    }
}