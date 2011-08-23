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

            m_Log.Warn("Asset Database is using " + ((IDataConnector)genericData).Identifier);

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            genericData.ConnectToDatabase(defaultConnectionString, "Asset", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));
            DataManager.DataManager.RegisterPlugin(Name, this);
        }

        #endregion

        #region Implementation of IAssetDataPlugin

        public AssetBase GetAsset(UUID uuid)
        {
            IDataReader dr = null;
            try
            {
                dr = m_Gd.QueryData("where id = '" + uuid + "'", "assets", "id, name, description, assetType, local, temporary, asset_flags, creatorID, data");
                while(dr != null && dr.Read())
                {
                    return LoadAssetFromDataRead(dr);
                }
                m_Log.Warn("[LocalAssetDatabase] GetAsset(" + uuid + ") - Asset " + uuid + " was not found.");
            }
            catch (Exception e)
            {
                m_Log.Error("[LocalAssetDatabase]: Failed to fetch asset " + uuid + ", " + e.ToString());
            }
            finally
            {
                if (dr != null)
                    dr.Close();
            }
            return null;
        }

        public AssetBase GetMeta(UUID uuid)
        {
            IDataReader dr = null;
            try
            {
                dr = m_Gd.QueryData("where id = '" + uuid + "' LIMIT 1", "assets", "id, name, description, assetType, local, temporary, asset_flags, creatorID");
                while (dr.Read())
                {
                    return LoadAssetFromDataRead(dr);
                }
                m_Log.Warn("[LocalAssetDatabase] GetMeta(" + uuid + ") - Asset " + uuid + " was not found.");
            }
            catch (Exception e)
            {
                m_Log.Error("[LocalAssetDatabase]: Failed to fetch asset " + uuid + ", " + e.ToString());
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
                m_Log.Warn("[LocalAssetDatabase] GetData(" + uuid + ") - Asset " + uuid + " was not found.");
            }
            catch (Exception e)
            {
                m_Log.Error("[LocalAssetDatabase]: Failed to fetch asset " + uuid + ", " + e.ToString());
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
                if(asset.Name.Length > 64)
                    asset.Name = asset.Name.Substring(0, 64);
                if(asset.Description.Length > 128)
                    asset.Description = asset.Description.Substring(0, 128);
                int now = (int)Utils.DateTimeToUnixTime(DateTime.UtcNow);
                if(ExistsAsset(asset.ID))
                {
                    m_Log.Warn("[LocalAssetDatabase]: Asset already exists in the db - " + asset.ID);
                    Delete(asset.ID);
                    m_Gd.Insert("assets", new[] { "id", "name", "description", "assetType", "local", "temporary", "create_time", "access_time", "asset_flags", "creatorID", "data" },
                        new object[] { asset.ID, asset.Name, asset.Description, (sbyte)asset.TypeAsset, (asset.Flags & AssetFlags.Local) == AssetFlags.Local, (asset.Flags & AssetFlags.Temperary) == AssetFlags.Temperary, now, now, (int)asset.Flags, asset.CreatorID, asset.Data });
                }
                else
                {
                    m_Gd.Insert("assets", new[] { "id", "name", "description", "assetType", "local", "temporary", "create_time", "access_time", "asset_flags", "creatorID", "data" },
                        new object[] { asset.ID, asset.Name, asset.Description, (sbyte)asset.TypeAsset, (asset.Flags & AssetFlags.Local) == AssetFlags.Local, (asset.Flags & AssetFlags.Temperary) == AssetFlags.Temperary, now, now, (int)asset.Flags, asset.CreatorID, asset.Data });
                }
            }
            catch(Exception e)
            {
                m_Log.ErrorFormat("[LocalAssetDatabase]: Failure creating asset {0} with name \"{1}\". Error: {2}",
                    asset.ID, asset.Name, e.ToString());
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
                m_Log.Error("[LocalAssetDatabase] UpdateContent(" + id + ") - Errored, " + e.ToString());
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
                    "[LocalAssetDatabase]: Failure fetching asset {0}" + Environment.NewLine + e.ToString(), uuid);
            }
            return false;
        }

        public bool Delete(UUID id)
        {
            try
            {
                m_Gd.Delete("assets", "id = '" + id + "'");
            }
            catch (Exception e)
            {
                m_Log.Error("[LocalAssetDatabase] Error while deleting asset " + e.ToString());
            }
            return true;
        }

        private static AssetBase LoadAssetFromDataRead(IDataRecord dr)
        {
            AssetBase asset = new AssetBase(dr["id"].ToString())
            {
                Name = dr["name"].ToString(),
                Description = dr["description"].ToString()
            };
            string Flags = dr["asset_flags"].ToString();
            if(Flags != "")
                asset.Flags = (AssetFlags)int.Parse(Flags);
            string type = dr["assetType"].ToString();
            asset.TypeAsset = (AssetType)int.Parse(type);
            UUID creator;

            if(UUID.TryParse(dr["creatorID"].ToString(), out creator))
                asset.CreatorID = creator;
            try
            {
                object d = dr["data"];
                if ((d != null) && (d.ToString() != ""))
                {
                    asset.Data = (Byte[]) d;
                    asset.MetaOnly = false;
                }
                else
                {
                    asset.MetaOnly = true;
                    asset.Data = new byte[0];
                }
            }
            catch (Exception ex)
            {
                asset.MetaOnly = true;
                asset.Data = new byte[0];
                m_Log.Error("[LocalAssetDatabase]: Failed to cast data for " + asset.ID + ", " + ex.ToString());
            }
            
            if (dr["local"].ToString().Equals("1") || dr["local"].ToString().Equals("true", StringComparison.InvariantCultureIgnoreCase))
                asset.Flags |= AssetFlags.Local;
            string temp = dr["temporary"].ToString();
            if(temp != "")
            {
                bool tempbool = false;
                int tempint = 0;
                if(bool.TryParse(temp, out tempbool))
                {
                    if(tempbool)
                        asset.Flags |= AssetFlags.Temperary;
                }
                else if(int.TryParse(temp, out tempint))
                {
                    if(tempint == 1)
                        asset.Flags |= AssetFlags.Temperary;
                }
            }
            return asset;
        }

        #endregion
    }
}
