using System;
using System.Data;
using System.Reflection;
using Aurora.Framework;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService.Connectors.Database.Asset
{
    public class LocalAssetMainConnector : IAssetDataPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IGenericData m_Gd;
        #region Implementation of IAuroraDataPlugin

        public string Name
        {
            get { return "IAssetDataPlugin"; }
        }

        public void Initialize(IGenericData genericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AssetConnector", "LocalConnector") != "LocalConnector")
                return;
            m_Gd = genericData;

            if (source.Configs["Handlers"].GetString("AssetHandler", "") != "AssetService")
                return;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            genericData.ConnectToDatabase(defaultConnectionString, "Asset", true);
            DataManager.DataManager.RegisterPlugin(Name, this);

        }

        #endregion

        #region Implementation of IAssetDataPlugin

        public AssetBase GetAsset(UUID uuid)
        {
            IDataReader dr = null;
            try
            {
                dr = m_Gd.QueryData("where id = '" + uuid + "' LIMIT 1", "assets", "id, name, description, assetType, local, temporary, asset_flags, CreatorID, data");
                if (dr == null)
                {
                    m_Log.Warn("[LocalAssetMainConnector] GetAsset(" + uuid + ") - Asset " + uuid + " was not found.");
                    return null;
                }
                while (dr.Read())
                {
                    return LoadAssetFromDataRead(dr);
                }
            }
            catch (Exception e)
            {
                m_Log.Error("[ASSETS DB]: MySql failure fetching asset " + uuid, e);
            }
            finally
            {
                if (dr != null) dr.Close();
            }
            return null;
        }

        public AssetBase GetMeta(UUID uuid)
        {
            IDataReader dr = null;
            try
            {
                dr = m_Gd.QueryData("where id = '" + uuid + "' LIMIT 1", "assets", "id, name, description, assetType, local, temporary, asset_flags, CreatorID");
                if (dr == null)
                {
                    m_Log.Warn("[LocalAssetMainConnector] GetMeta(" + uuid + ") - Asset " + uuid + " was not found.");
                    return null;
                }
                while (dr.Read())
                {
                    return LoadAssetFromDataRead(dr);
                }
            }
            catch (Exception e)
            {
                m_Log.Error("[ASSETS DB]: MySql failure fetching asset " + uuid, e);
            }
            finally
            {
                if (dr != null) dr.Close();
            }
            return null;
        }

        public Byte[] GetData(UUID uuid)
        {
            IDataReader dr = null;
            try
            {
                dr = m_Gd.QueryData("where id = '" + uuid + "' LIMIT 1", "assets", "data");
                if (dr != null)
                    return (byte[])dr["data"];
                m_Log.Warn("[LocalAssetMainConnector] GetData(" + uuid + ") - Asset " + uuid + " was not found.");
            }
            catch (Exception e)
            {
                m_Log.Error("[ASSETS DB]: MySql failure fetching asset " + uuid, e);
            }
            finally
            {
                if (dr != null) dr.Close();
            }
            return null;
        }

        public UUID Store(AssetBase asset)
        {
            StoreAsset(asset);
            return asset.ID;
        }

        public bool StoreAsset(AssetBase asset)
        {
            try
            {
                if (asset.Name.Length > 64) asset.Name = asset.Name.Substring(0, 64);
                if (asset.Description.Length > 128) asset.Description = asset.Description.Substring(0, 128);
                int now = (int)Utils.DateTimeToUnixTime(DateTime.UtcNow);
                Delete(asset.ID);
                m_Gd.Insert("assets", new[] { "id", "name", "description", "assetType", "local", "temporary", "create_time", "access_time", "asset_flags", "CreatorID", "data" },
                    new object[] { asset.ID, asset.Name, asset.Description, (sbyte)asset.TypeAsset, (asset.Flags & AssetFlags.Local) == AssetFlags.Local, (asset.Flags & AssetFlags.Temperary) == AssetFlags.Temperary, now, now, (int)asset.Flags, asset.CreatorID, asset.Data });
            }
            catch (Exception e)
            {
                m_Log.ErrorFormat("[ASSET DB]: MySQL failure creating asset {0} with name \"{1}\". Error: {2}",
                    asset.ID, asset.Name, e.Message);
            }
            return true;
        }

        public void UpdateContent(UUID id, byte[] asset)
        {
            try
            {
                m_Gd.Update("assets", new object[] { asset }, new[] { "data" }, new[] { "id" }, new object[] { id });
            }
            catch (Exception e)
            {
                m_Log.Error("[ASSETS DB] UpdateContent(" + id + ") - Errored", e);
            }
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
                m_Gd.Delete("assets", "id = '" + id + "'");
            }
            catch (Exception e)
            {
                m_Log.Error("[ASSETS DB] Error while deleting asset", e);
            }
            return true;
        }

        private static AssetBase LoadAssetFromDataRead(IDataRecord dr)
        {
            AssetBase asset = new AssetBase(dr["id"].ToString())
            {
                Name = dr["name"].ToString(),
                TypeAsset = (AssetType)int.Parse(dr["assetType"].ToString()),
                CreatorID = UUID.Parse(dr["CreatorID"].ToString()),
                Description = dr["description"].ToString(),
                Flags = (AssetFlags)int.Parse(dr["asset_flags"].ToString())
            };
            try
            {
                if ((dr["data"] != null) && (dr["data"].ToString() != ""))
                {
                    asset.Data = (Byte[]) dr["data"];
                    asset.MetaOnly = false;
                }
                else
                {
                    asset.MetaOnly = true;
                    asset.Data = new byte[0];
                }
            }
            catch (Exception)
            {
                asset.MetaOnly = true;
                asset.Data = new byte[0];
            }
            
            if (dr["local"].ToString().Equals("1") || dr["local"].ToString().Equals("true", StringComparison.InvariantCultureIgnoreCase))
                asset.Flags |= AssetFlags.Local;
            if (bool.Parse(dr["temporary"].ToString())) asset.Flags |= AssetFlags.Temperary;
            return asset;
        }

        #endregion
    }
}
