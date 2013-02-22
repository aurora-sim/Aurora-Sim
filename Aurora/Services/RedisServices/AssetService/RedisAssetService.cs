﻿using System;
using System.Reflection;
using Aurora.DataManager;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Services.Interfaces;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Sider;
using Aurora.RedisServices.ConnectionHelpers;

namespace Aurora.RedisServices.AssetService
{
    public class AssetService : ConnectorBase, IAssetService, IService
    {
        #region Declares

        protected const string DATA_PREFIX = "DATA";
        protected bool doDatabaseCaching = false;
        protected string m_connectionDNS = "localhost", m_connectionPassword = null;
        protected Pool<RedisClient<byte[]>> m_connectionPool;
        protected int m_connectionPort = 6379;

        protected bool m_doConversion = false;
        protected IAssetDataPlugin m_assetService;

        #endregion

        #region IService Members

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AssetHandler", "") != "Redis" + Name)
                return;
            Configure(config, registry);
            Init(registry, Name, serverPath: "/asset/");
        }

        public virtual void Configure(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;

            registry.RegisterModuleInterface<IAssetService>(this);

            IConfig handlers = config.Configs["Handlers"];
            if (handlers != null)
                doDatabaseCaching = handlers.GetBoolean("AssetHandlerUseCache", false);

            IConfig redisConnection = config.Configs["RedisConnection"];
            if (redisConnection != null)
            {
                string connString = redisConnection.Get("ConnectionString", "localhost:6379");
                m_connectionDNS = connString.Split(':')[0];
                m_connectionPort = int.Parse(connString.Split(':')[1]);
                m_connectionPassword = redisConnection.Get("ConnectionPassword", null);
                m_doConversion = redisConnection.GetBoolean("DoConversion", false);
            }
            m_connectionPool = new Pool<RedisClient<byte[]>>(() => new RedisClient<byte[]>(m_connectionDNS, m_connectionPort));

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

                MainConsole.Instance.Info("[REDIS ASSET SERVICE]: Redis asset service enabled");
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

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
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

            object remoteValue = DoRemote(id);
            if (remoteValue != null || m_doRemoteOnly)
            {
                if (doDatabaseCaching && cache != null)
                    cache.Cache(id, (AssetBase)remoteValue);
                return (AssetBase)remoteValue;
            }

            AssetBase asset = RedisGetAsset(id);
            if (doDatabaseCaching && cache != null)
                cache.Cache(id, asset);
            return asset;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual AssetBase GetMesh(string id)
        {
            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (doDatabaseCaching && cache != null)
            {
                bool found;
                AssetBase cachedAsset = cache.Get(id, out found);
                if (found && (cachedAsset == null || cachedAsset.Data.Length != 0))
                    return cachedAsset;
            }

            object remoteValue = DoRemote(id);
            if (remoteValue != null || m_doRemoteOnly)
            {
                if (doDatabaseCaching && cache != null)
                    cache.Cache(id, (AssetBase)remoteValue);
                return (AssetBase)remoteValue;
            }

            AssetBase asset = RedisGetAsset(id);
            if (doDatabaseCaching && cache != null)
                cache.Cache(id, asset);
            return asset;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual AssetBase GetCached(string id)
        {
            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (doDatabaseCaching && cache != null)
                return cache.Get(id);
            return null;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual byte[] GetData(string id)
        {
            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (doDatabaseCaching && cache != null)
            {
                bool found;
                AssetBase cachedAsset = cache.Get(id, out found);
                if (found && (cachedAsset == null || cachedAsset.Data.Length != 0))
                    return cachedAsset.Data;
            }

            object remoteValue = DoRemote(id);
            if (remoteValue != null || m_doRemoteOnly)
                return (byte[])remoteValue;

            AssetBase asset = RedisGetAsset(id);
            if (doDatabaseCaching && cache != null)
                cache.Cache(id, asset);
            if (asset != null) return asset.Data;
            return new byte[0];
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual bool GetExists(string id)
        {
            object remoteValue = DoRemote(id);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            return RedisExistsAsset(id);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual void Get(String id, Object sender, AssetRetrieved handler)
        {
            Util.FireAndForget((o) =>
            {
                handler(id, sender, Get(id));
            });
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual UUID Store(AssetBase asset)
        {
            if (asset == null)
                return UUID.Zero;

            object remoteValue = DoRemote(asset);
            if (remoteValue != null || m_doRemoteOnly)
            {
                if (remoteValue == null)
                    return UUID.Zero;
                asset.ID = (UUID)remoteValue;
            }
            else
                RedisSetAsset(asset);

            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (doDatabaseCaching && cache != null && asset != null && asset.Data != null && asset.Data.Length != 0)
            {
                cache.Expire(asset.ID.ToString());
                cache.Cache(asset.ID.ToString(), asset);
            }

            return asset != null ? asset.ID : UUID.Zero;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual UUID UpdateContent(UUID id, byte[] data)
        {
            object remoteValue = DoRemote(id, data);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? UUID.Zero : (UUID)remoteValue;

            RedisUpdateAsset(id.ToString(), data);
            IImprovedAssetCache cache = m_registry.RequestModuleInterface<IImprovedAssetCache>();
            if (doDatabaseCaching && cache != null)
                cache.Expire(id.ToString());
            return id;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual bool Delete(UUID id)
        {
            object remoteValue = DoRemote(id);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            RedisDeleteAsset(id.ToString());
            return true;
        }

        #endregion

        private T RedisEnsureConnection<T>(Func<RedisClient<byte[]>, T> func)
        {
            RedisClient<byte[]> client = null;
            try
            {
                client = m_connectionPool.GetFreeItem();
                if (func == null)
                    return default(T);//Checking whether the connection is alive
                return func(client);
            }
            catch (Exception)
            {
                try { client.Dispose(); } catch { }
                m_connectionPool.DestroyItem(client);
                client = null;
            }
            finally
            {
                if (client != null)
                    m_connectionPool.FlagFreeItem(client);
            }
            return default(T);
        }

        public AssetBase RedisGetAsset(string id)
        {
            AssetBase asset = null;
            int msCount = Environment.TickCount;
            string lookupID = id;

            byte[] data = RedisEnsureConnection((conn) => conn.Get(lookupID));
            if (data == null)
                return CheckForConversion(id);

            // Deduplication...

            if (data.Length == 16)
            {
                //It's a reference to another asset
                Guid reference = new Guid(data);
                asset = RedisGetAsset(reference.ToString());
                if(asset != null)
                    asset.IDString = id;//Fix the ID too
                return asset;
            }

            // End deduplication

            byte[] assetdata = RedisEnsureConnection((conn) => conn.Get(DATA_PREFIX + lookupID));
            MemoryStream memStream = new MemoryStream(data);
            asset = ProtoBuf.Serializer.Deserialize<AssetBase>(memStream);
            memStream.Close();
            asset.Data = assetdata;

            int elapsed = Environment.TickCount - msCount;
            if (MainConsole.Instance != null && elapsed > 50)
                MainConsole.Instance.Debug("[REDIS ASSET SERVICE]: Took " + elapsed + " to get asset " + id + " sized " + asset.Data.Length / (1024) + "kbs");
            return asset;
        }

        private object m_conversionlock = new object();
        private AssetBase CheckForConversion(string id)
        {
            if (!m_doConversion)
                return null;

            AssetBase asset;
            lock (m_conversionlock)
            {
                asset = m_assetService.GetAsset(UUID.Parse(id));

                if (asset == null)
                    return null;

                //Delete first, then restore it with the new local flag attached, so that we know we've converted it
                m_assetService.Delete(asset.ID, true);
                asset.Flags = AssetFlags.Local;
                m_assetService.StoreAsset(asset);
            }

            //Now store in Redis
            RedisSetAsset(asset);

            return asset;
        }

        public bool RedisExistsAsset(string id)
        {
            bool success = RedisEnsureConnection((conn) => conn.Exists(id));
            if (!success)
                success = m_assetService.ExistsAsset(UUID.Parse(id));
            return success;
        }

        public bool RedisSetAsset(AssetBase asset)
        {
            //Deduplication...

            //Database+2 holds hashcodes --> UUID, so check it to see whether a hashcode for this object already exists
            int msCount = Environment.TickCount;
            byte[] trueAsset = RedisEnsureConnection((conn) => conn.Get(asset.HashCode));
            int elapsed1 = Environment.TickCount - msCount;
            if (trueAsset != null)
            {
                Guid trueAssetID = new Guid(trueAsset);
                if(MainConsole.Instance != null)
                    MainConsole.Instance.Debug("[REDIS ASSET SERVICE]: Found duplicate asset " + asset.IDString + " for " + trueAssetID);
                RedisEnsureConnection((conn) => conn.Set(asset.IDString, trueAssetID.ToByteArray()));
                return true;
            }

            int msCount2 = Environment.TickCount;
            MemoryStream memStream = new MemoryStream();
            byte[] data = asset.Data;
            asset.Data = new byte[0];
            ProtoBuf.Serializer.Serialize<AssetBase>(memStream, asset);
            asset.Data = data;
            int elapsed2 = Environment.TickCount - msCount2;

            int msCount3 = Environment.TickCount;
            RedisEnsureConnection((conn) => 
                {
                    conn.Pipeline((c) =>
                    {
                        c.Set(asset.IDString, memStream.ToArray());
                        c.Set(DATA_PREFIX + asset.IDString, data);
                        //Add us to the hashcode --> UUID database
                        c.Set(asset.HashCode, new Guid(asset.IDString).ToByteArray());
                    });
                    return true;
                });
            memStream.Close();
            int elapsed3 = Environment.TickCount - msCount3;
            /*if ((elapsed1 + elapsed2 + elapsed3) > 10)
                Console.WriteLine("[REDIS ASSET SERVICE]: Took " + (elapsed1 + elapsed2 + elapsed3) + " (" + elapsed1 + ", " + elapsed2 + ", " + elapsed3 + 
                    ") to store asset " + asset.IDString + " sized " + asset.Data.Length / (1024) + "kbs");*/
            return false;
        }

        public void RedisUpdateAsset(string id, byte[] data)
        {
            RedisEnsureConnection((conn) => conn.Set(DATA_PREFIX + id, data));
        }

        public void RedisDeleteAsset(string id)
        {
            AssetBase asset = RedisGetAsset(id);
            if (asset == null)
                asset = RedisGetAsset(id);
            RedisEnsureConnection((conn) => conn.Del(id) == 1);
            RedisEnsureConnection((conn) => conn.Del(DATA_PREFIX + id) == 1);
            RedisEnsureConnection((conn) => conn.Del(asset.HashCode) == 1);
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
                int off = i * 16;
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

            AssetBase asset = RedisGetAsset(args[2]);
            if(asset == null)
                asset = RedisGetAsset(args[2]);

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